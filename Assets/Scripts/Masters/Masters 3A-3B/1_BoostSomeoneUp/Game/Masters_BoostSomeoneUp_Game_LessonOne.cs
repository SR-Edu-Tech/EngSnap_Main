using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class Masters_BoostSomeoneUp_Game_LessonOne : Masters_Lesson {
    [System.Serializable]
    public class SortPuzzle {
        public string expression;
        public Masters_3A_FallingSortCategory sortType;
        public AudioClip audioClip;
    }

    [Header("Game Data")]
    [SerializeField] protected SortPuzzle[] sortPuzzleArray;
    [SerializeField] protected Masters_3A_FallingSortBin[] sortBinArray;
    [SerializeField] protected Masters_3A_FallingSortPhraseCard phraseCardPrefab;

    [Header("Game UI")]
    [SerializeField] protected RectTransform topSpawnPoint;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private GameObject completedPanel;
    [SerializeField] private TextMeshProUGUI completedTitleTMP;
    [SerializeField] private Button retryButton;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Timer & Spawning Settings")]
    [SerializeField] protected float gameDuration = 60f;
    [SerializeField] protected float initialSpawnDelay = 3f;
    [SerializeField] protected float initialSpawnInterval = 4f;
    [SerializeField] protected float minSpawnInterval = 1.0f;
    [SerializeField] protected float spawnIntervalDecreaseRate = 0.05f;

    [Header("Falling Settings")]
    [SerializeField] protected float initialFallSpeed = 200f;
    [SerializeField] protected float fallSpeedIncreaseRate = 5f;
    [SerializeField] protected float maxFallSpeed = 600f;
    [SerializeField] protected float snapAnimationSpeed = 0.2f;

    protected float timeRemaining;
    protected int score;
    protected bool isGameActive;
    protected float currentSpawnInterval;
    protected float spawnTimer;
    protected float currentFallSpeed;

    protected List<Masters_3A_FallingSortPhraseCard> activeCards = new List<Masters_3A_FallingSortPhraseCard>();
    protected Dictionary<Masters_3A_FallingSortPhraseCard, Masters_3A_FallingSortBin> cardTargetBins = new Dictionary<Masters_3A_FallingSortPhraseCard, Masters_3A_FallingSortBin>();

    protected List<string> recentlySpawnedExpressions = new List<string>();
    protected Masters_3A_FallingSortCategory lastSpawnedCategory = (Masters_3A_FallingSortCategory)(-1);
    protected int consecutiveCategoryCount = 0;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;
        if (phraseCardPrefab != null) {
            phraseCardPrefab.gameObject.SetActive(false);
        }
        if (retryButton != null) {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(RestartGame);
        }
    }

    protected override void Start() {
        base.Start();
        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        }
        RestartGame();
    }

    protected virtual void RestartGame() {
        foreach (var card in activeCards) {
            if (card != null && card.gameObject != null) Destroy(card.gameObject);
        }
        activeCards.Clear();
        cardTargetBins.Clear();

        ConfigureBins();

        if (completedPanel != null) completedPanel.SetActive(false);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (nextButton != null) nextButton.interactable = false;

        score = 0;
        timeRemaining = gameDuration;
        currentSpawnInterval = initialSpawnInterval;
        currentFallSpeed = initialFallSpeed;
        spawnTimer = initialSpawnInterval;

        UpdateUI();

        isGameActive = false;
        StartCoroutine(InitialSpawnDelayCoroutine());
    }

    protected virtual IEnumerator InitialSpawnDelayCoroutine() {
        yield return new WaitForSeconds(initialSpawnDelay);
        isGameActive = true;
    }

    protected virtual void ConfigureBins() {
        if (sortBinArray == null) return;
        
        // Define the 3 required categories
        Masters_3A_FallingSortCategory[] categories = {
            Masters_3A_FallingSortCategory.Compliment,
            Masters_3A_FallingSortCategory.Encouragement,
            Masters_3A_FallingSortCategory.Team
        };
        string[] labels = { "Compliment", "Encouragement", "Team" };

        for (int i = 0; i < sortBinArray.Length; i++) {
            if (sortBinArray[i] == null) continue;
            
            if (i < categories.Length) {
                sortBinArray[i].gameObject.SetActive(true);
                sortBinArray[i].ConfigureBin(categories[i], labels[i]);
            } else {
                sortBinArray[i].gameObject.SetActive(false);
            }
        }
    }

    private void Update() {
        if (!isGameActive) return;

        timeRemaining -= Time.deltaTime;

        currentSpawnInterval -= spawnIntervalDecreaseRate * Time.deltaTime;
        if (currentSpawnInterval < minSpawnInterval) {
            currentSpawnInterval = minSpawnInterval;
        }

        currentFallSpeed += fallSpeedIncreaseRate * Time.deltaTime;
        if (currentFallSpeed > maxFallSpeed) {
            currentFallSpeed = maxFallSpeed;
        }

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= currentSpawnInterval) {
            spawnTimer = 0f;
            SpawnRandomCard();
        }

        UpdateUI();

        if (timeRemaining <= 0) {
            GameOver();
            return;
        }

        UpdateFallingCards();
    }

    protected virtual void UpdateUI() {
        if (timerTMP != null) timerTMP.text = Mathf.CeilToInt(timeRemaining).ToString() + "s";
        if (scoreTMP != null) scoreTMP.text = $"Score: {score}";
    }

    protected virtual void SpawnRandomCard() {
        if (sortPuzzleArray == null || sortPuzzleArray.Length == 0) return;

        SortPuzzle selectedPuzzle = null;
        int maxAttempts = 50;

        for (int attempt = 0; attempt < maxAttempts; attempt++) {
            SortPuzzle candidate = sortPuzzleArray[Random.Range(0, sortPuzzleArray.Length)];
            if (candidate == null) continue;

            bool isAlreadyActive = false;
            foreach (var activeCard in activeCards) {
                if (activeCard != null) {
                    TextMeshProUGUI tmp = activeCard.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null && tmp.text == candidate.expression) {
                        isAlreadyActive = true;
                        break;
                    }
                }
            }
            if (isAlreadyActive) continue;

            if (recentlySpawnedExpressions.Contains(candidate.expression)) {
                continue;
            }

            if (consecutiveCategoryCount >= 2 && candidate.sortType == lastSpawnedCategory) {
                continue;
            }

            selectedPuzzle = candidate;
            break;
        }

        if (selectedPuzzle == null) {
            selectedPuzzle = sortPuzzleArray[Random.Range(0, sortPuzzleArray.Length)];
        }

        recentlySpawnedExpressions.Add(selectedPuzzle.expression);
        int maxHistory = Mathf.Max(3, sortPuzzleArray.Length / 2);
        if (recentlySpawnedExpressions.Count > maxHistory) {
            recentlySpawnedExpressions.RemoveAt(0);
        }

        if (selectedPuzzle.sortType == lastSpawnedCategory) {
            consecutiveCategoryCount++;
        } else {
            lastSpawnedCategory = selectedPuzzle.sortType;
            consecutiveCategoryCount = 1;
        }

        if (phraseCardPrefab != null && topSpawnPoint != null) {
            Masters_3A_FallingSortPhraseCard newCard = Instantiate(phraseCardPrefab, topSpawnPoint.parent);
            newCard.SetExpression(selectedPuzzle.expression);

            RectTransform cardRect = newCard.GetComponent<RectTransform>();
            if (cardRect != null) {
                cardRect.position = topSpawnPoint.position;
                cardRect.localScale = Vector3.one;
            }
            newCard.gameObject.SetActive(true);

            newCard.OnDragEnded += HandleCardDragEnded;
            activeCards.Add(newCard);
        }
    }

    protected void HandleCardDragEnded(Masters_3A_FallingSortPhraseCard card) {
        if (!activeCards.Contains(card) || sortBinArray == null) return;

        float minDistance = float.MaxValue;
        Masters_3A_FallingSortBin closestBin = null;

        foreach (var bin in sortBinArray) {
            if (bin == null || bin.GetSnapPoint() == null) continue;
            float dist = Mathf.Abs(bin.GetSnapPoint().position.x - card.transform.position.x);
            if (dist < minDistance) {
                minDistance = dist;
                closestBin = bin;
            }
        }

        if (closestBin != null && closestBin.GetSnapPoint() != null) {
            cardTargetBins[card] = closestBin;
            RectTransform cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null) {
                cardRect.DOMoveX(closestBin.GetSnapPoint().position.x, snapAnimationSpeed).SetEase(Ease.OutQuad);
            }
        }
    }

    protected virtual void UpdateFallingCards() {
        float scaledFallSpeed = currentFallSpeed * (Screen.height / 1920f);

        for (int i = activeCards.Count - 1; i >= 0; i--) {
            var card = activeCards[i];
            if (card == null) {
                activeCards.RemoveAt(i);
                continue;
            }

            RectTransform cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null) {
                cardRect.anchoredPosition += Vector2.down * scaledFallSpeed * Time.deltaTime;
            }

            Masters_3A_FallingSortBin evaluationBin = null;
            if (cardTargetBins.ContainsKey(card)) {
                evaluationBin = cardTargetBins[card];
            } else if (sortBinArray != null) {
                float minDistance = float.MaxValue;
                foreach (var bin in sortBinArray) {
                    if (bin == null || bin.GetSnapPoint() == null) continue;
                    float dist = Mathf.Abs(bin.GetSnapPoint().position.x - card.transform.position.x);
                    if (dist < minDistance) {
                        minDistance = dist;
                        evaluationBin = bin;
                    }
                }
            }

            if (evaluationBin != null && cardRect != null && cardRect.position.y <= evaluationBin.GetDropThresholdY()) {
                EvaluateDrop(card, evaluationBin);
            }
        }
    }

    protected virtual void EvaluateDrop(Masters_3A_FallingSortPhraseCard card, Masters_3A_FallingSortBin bin) {
        SortPuzzle matchedPuzzle = null;
        TextMeshProUGUI tmp = card.GetComponentInChildren<TextMeshProUGUI>();
        string cardText = (tmp != null) ? tmp.text : "";

        if (sortPuzzleArray != null) {
            foreach (var puzzle in sortPuzzleArray) {
                if (puzzle != null && puzzle.expression == cardText) {
                    matchedPuzzle = puzzle;
                    break;
                }
            }
        }

        if (matchedPuzzle != null && bin.MatchesCategory(matchedPuzzle.sortType)) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
            score++;
            UpdateUI();
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }

        activeCards.Remove(card);
        if (cardTargetBins.ContainsKey(card)) cardTargetBins.Remove(card);

        RectTransform cardRect = card.GetComponent<RectTransform>();
        if (cardRect != null) {
            cardRect.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
                if (card != null && card.gameObject != null) Destroy(card.gameObject);
            });
        } else if (card != null && card.gameObject != null) {
            Destroy(card.gameObject);
        }
    }

    protected virtual void GameOver() {
        timeRemaining = 0;
        isGameActive = false;
        UpdateUI();

        foreach (var card in activeCards) {
            if (card != null && card.gameObject != null) {
                RectTransform rect = card.GetComponent<RectTransform>();
                if (rect != null) {
                    rect.DOScale(Vector3.zero, 0.3f).OnComplete(() => {
                        if (card != null && card.gameObject != null) Destroy(card.gameObject);
                    });
                } else {
                    Destroy(card.gameObject);
                }
            }
        }
        activeCards.Clear();
        cardTargetBins.Clear();

        if (completedPanel != null) {
            completedPanel.SetActive(true);
            if (completedTitleTMP != null) completedTitleTMP.text = score > 0 ? "Good Job!" : "Time's Up!";
            completedPanel.transform.localScale = Vector3.zero;
            completedPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutExpo);
        }

        if (retryButton != null) retryButton.gameObject.SetActive(true);
        if (nextButton != null) {
            nextButton.interactable = true;
            NextButtonAnimation();
        }
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) return;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}
