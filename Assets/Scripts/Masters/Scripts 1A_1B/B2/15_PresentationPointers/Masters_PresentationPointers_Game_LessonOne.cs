using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Game Lesson 1 for Unit 15 Presentation Pointers.
/// Speech Steps Dash: Arcade falling phrase cards sorted into 4 step gates with 3 lives and 60s timer.
/// </summary>
public class Masters_PresentationPointers_Game_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class SortPuzzle {
        public string expression;
        public Masters_Unit15_FallingSortCategory sortType;
        public AudioClip audioClip;
    }

    [Header("Game Data")]
    [SerializeField] private SortPuzzle[] sortPuzzleArray;
    [SerializeField] private Masters_FallingSortBin[] sortBinArray;
    [SerializeField] private Masters_FallingSortPhraseCard phraseCardPrefab;
    
    [Header("Game UI")]
    [SerializeField] private RectTransform topSpawnPoint;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private GameObject completedPanel;
    [SerializeField] private TextMeshProUGUI completedTitleTMP;
    [SerializeField] private Button retryButton;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Timer & Spawning Settings")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private float initialSpawnDelay = 2f;
    [SerializeField] private float initialSpawnInterval = 3.5f;
    [SerializeField] private float minSpawnInterval = 1.0f;
    [SerializeField] private float spawnIntervalDecreaseRate = 0.05f; 
    
    [Header("Falling Settings")]
    [SerializeField] private float initialFallSpeed = 200f;
    [SerializeField] private float fallSpeedIncreaseRate = 5f;
    [SerializeField] private float maxFallSpeed = 600f;
    [SerializeField] private float snapAnimationSpeed = 0.2f;

    private float timeRemaining;
    private float currentSpawnInterval;
    private float currentFallSpeed;
    private float spawnTimer;
    private int score;
    private int lives = 3;
    private bool isGameActive;

    private List<Masters_FallingSortPhraseCard> activeCards = new List<Masters_FallingSortPhraseCard>();
    private Dictionary<Masters_FallingSortPhraseCard, Masters_FallingSortBin> cardTargetBins = new Dictionary<Masters_FallingSortPhraseCard, Masters_FallingSortBin>();

    protected override void Awake() {
        base.Awake();
        if (retryButton != null) retryButton.onClick.AddListener(RestartGame);
        if (completedPanel != null) completedPanel.SetActive(false);
        if (nextButton != null) nextButton.interactable = false;
    }

    protected override void Start() {
        base.Start();
        StartGame();
    }

    private void StartGame() {
        timeRemaining = gameDuration;
        currentSpawnInterval = initialSpawnInterval;
        currentFallSpeed = initialFallSpeed;
        spawnTimer = currentSpawnInterval - initialSpawnDelay;
        score = 0;
        lives = 3;
        isGameActive = true;

        if (completedPanel != null) completedPanel.SetActive(false);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (nextButton != null) nextButton.interactable = false;

        UpdateUI();
    }

    private void RestartGame() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
        foreach (var card in activeCards) {
            if (card != null) Destroy(card.gameObject);
        }
        activeCards.Clear();
        cardTargetBins.Clear();
        StartGame();
    }

    private void Update() {
        if (!isGameActive) return;

        timeRemaining -= Time.deltaTime;
        
        currentSpawnInterval -= spawnIntervalDecreaseRate * Time.deltaTime;
        if (currentSpawnInterval < minSpawnInterval) currentSpawnInterval = minSpawnInterval;

        currentFallSpeed += fallSpeedIncreaseRate * Time.deltaTime;
        if (currentFallSpeed > maxFallSpeed) currentFallSpeed = maxFallSpeed;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= currentSpawnInterval) {
            spawnTimer = 0f;
            SpawnRandomCard();
        }

        UpdateUI();

        if (timeRemaining <= 0 || lives <= 0) {
            GameOver();
            return;
        }

        UpdateFallingCards();
    }

    private void UpdateUI() {
        if (timerTMP != null) timerTMP.text = Mathf.CeilToInt(timeRemaining).ToString() + "s";
        if (scoreTMP != null) scoreTMP.text = $"Score: {score} | Lives: {lives}";
    }

    private void SpawnRandomCard() {
        if (sortPuzzleArray == null || sortPuzzleArray.Length == 0 || phraseCardPrefab == null || topSpawnPoint == null) return;

        SortPuzzle randomPuzzle = sortPuzzleArray[Random.Range(0, sortPuzzleArray.Length)];
        
        Masters_FallingSortPhraseCard newCard = Instantiate(phraseCardPrefab, topSpawnPoint.parent);
        newCard.SetExpression(randomPuzzle.expression);
        
        RectTransform cardRect = newCard.GetComponent<RectTransform>();
        cardRect.position = topSpawnPoint.position;
        cardRect.localScale = Vector3.one;
        newCard.gameObject.SetActive(true);

        newCard.OnDragEnded += HandleCardDragEnded;
        
        activeCards.Add(newCard);
    }

    private void HandleCardDragEnded(Masters_FallingSortPhraseCard card) {
        if (!activeCards.Contains(card) || sortBinArray == null) return;

        float minDistance = float.MaxValue;
        Masters_FallingSortBin closestBin = null;

        foreach (var bin in sortBinArray) {
            if (bin == null || bin.GetSnapPoint() == null) continue;
            float dist = Mathf.Abs(bin.GetSnapPoint().position.x - card.transform.position.x);
            if (dist < minDistance) {
                minDistance = dist;
                closestBin = bin;
            }
        }

        if (closestBin != null) {
            cardTargetBins[card] = closestBin;
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.DOMoveX(closestBin.GetSnapPoint().position.x, snapAnimationSpeed).SetEase(Ease.OutQuad);
        }
    }

    private void UpdateFallingCards() {
        float scaledFallSpeed = currentFallSpeed * (Screen.height / 1920f); 
        if (scaledFallSpeed <= 0f) scaledFallSpeed = currentFallSpeed;
        
        for (int i = activeCards.Count - 1; i >= 0; i--) {
            var card = activeCards[i];
            if (card == null) {
                activeCards.RemoveAt(i);
                continue;
            }

            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchoredPosition += Vector2.down * scaledFallSpeed * Time.deltaTime;

            Masters_FallingSortBin evaluationBin = null;
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

            if (evaluationBin != null && cardRect.position.y <= evaluationBin.GetDropThresholdY()) {
                EvaluateDrop(card, evaluationBin);
            }
        }
    }

    private void EvaluateDrop(Masters_FallingSortPhraseCard card, Masters_FallingSortBin bin) {
        SortPuzzle matchedPuzzle = null;
        string cardText = "";
        TextMeshProUGUI tmp = card.GetComponentInChildren<TextMeshProUGUI>();
        if (tmp != null) cardText = tmp.text;

        if (sortPuzzleArray != null) {
            foreach (var puzzle in sortPuzzleArray) {
                if (puzzle.expression == cardText) {
                    matchedPuzzle = puzzle;
                    break;
                }
            }
        }

        if (matchedPuzzle != null && bin != null && bin.MatchesUnit15(matchedPuzzle.sortType)) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
            score++;
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            lives--;
        }

        UpdateUI();

        activeCards.Remove(card);
        if (cardTargetBins.ContainsKey(card)) cardTargetBins.Remove(card);
        
        RectTransform cardRect = card.GetComponent<RectTransform>();
        if (cardRect != null) {
            cardRect.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
                if (card != null) Destroy(card.gameObject);
            });
        } else if (card != null) {
            Destroy(card.gameObject);
        }
    }

    private void GameOver() {
        if (timeRemaining < 0) timeRemaining = 0;
        isGameActive = false;
        UpdateUI();

        foreach (var card in activeCards) {
            if (card != null) {
                RectTransform rt = card.GetComponent<RectTransform>();
                if (rt != null) {
                    rt.DOScale(Vector3.zero, 0.3f).OnComplete(() => {
                        if (card != null) Destroy(card.gameObject);
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
            if (completedTitleTMP != null) {
                completedTitleTMP.text = (score >= 16 || lives > 0) ? "Good Job!" : "Try Again!";
            }
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
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        
        if (nextLessonSO != null && Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else if (Masters_LevelManager.Instance != null) {
            if (topic != Masters_Topic.None && Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
