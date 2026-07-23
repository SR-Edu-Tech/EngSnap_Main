using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_ChattingBees_Game_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class SortPuzzle {
        public string expression;
        public Masters_Unit8_FallingSortCategory sortType;
        public AudioClip audioClip;
    }

    [Header("Game Data")]
    [SerializeField]
    private SortPuzzle[] sortPuzzleArray;
    [SerializeField]
    private Masters_FallingSortBin[] sortBinArray;
    [SerializeField]
    private Masters_FallingSortPhraseCard phraseCardPrefab;
    
    [Header("Game UI")]
    [SerializeField]
    private RectTransform topSpawnPoint;
    [SerializeField]
    private TextMeshProUGUI timerTMP;
    [SerializeField]
    private TextMeshProUGUI scoreTMP;
    [SerializeField]
    private GameObject completedPanel;
    [SerializeField]
    private TextMeshProUGUI completedTitleTMP;
    [SerializeField]
    private Button retryButton;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;

    [Header("Timer & Spawning Settings")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private float initialSpawnDelay = 3f;
    [SerializeField] private float initialSpawnInterval = 4f;
    [SerializeField] private float minSpawnInterval = 1.0f;
    [Tooltip("How much to reduce the spawn interval every second.")]
    [SerializeField] private float spawnIntervalDecreaseRate = 0.05f; 
    
    [Header("Falling Settings")]
    [SerializeField] private float initialFallSpeed = 200f; // Pixels per second
    [Tooltip("How much the falling speed increases every second.")]
    [SerializeField] private float fallSpeedIncreaseRate = 5f;
    [SerializeField] private float maxFallSpeed = 600f;
    [SerializeField] private float snapAnimationSpeed = 0.2f;

    private float timeRemaining;
    private int score;
    private bool isGameActive;
    private float currentSpawnInterval;
    private float spawnTimer;
    private float currentFallSpeed;

    private List<Masters_FallingSortPhraseCard> activeCards = new List<Masters_FallingSortPhraseCard>();
    private Dictionary<Masters_FallingSortPhraseCard, Masters_FallingSortBin> cardTargetBins = new Dictionary<Masters_FallingSortPhraseCard, Masters_FallingSortBin>();

    protected override void Awake() {
        base.Awake();
        if (phraseCardPrefab != null) {
            phraseCardPrefab.gameObject.SetActive(false);
        }
        if (retryButton != null) {
            retryButton.onClick.AddListener(RestartGame);
        }
    }

    protected override void Start() {
        base.Start();
        RestartGame();
    }

    private void RestartGame() {
        // Clear old cards
        foreach (var card in activeCards) {
            if (card != null) Destroy(card.gameObject);
        }
        activeCards.Clear();
        cardTargetBins.Clear();

        if (completedPanel != null) completedPanel.SetActive(false);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (nextButton != null) nextButton.interactable = false;

        score = 0;
        timeRemaining = gameDuration;
        currentSpawnInterval = initialSpawnInterval;
        currentFallSpeed = initialFallSpeed;
        spawnTimer = initialSpawnInterval; // Force immediate spawn after delay

        UpdateUI();
        
        isGameActive = false;
        StartCoroutine(InitialSpawnDelayCoroutine());
    }

    private IEnumerator InitialSpawnDelayCoroutine() {
        yield return new WaitForSeconds(initialSpawnDelay);
        isGameActive = true;
    }

    private void Update() {
        if (!isGameActive) return;

        timeRemaining -= Time.deltaTime;
        
        // Decrease spawn interval over time
        currentSpawnInterval -= spawnIntervalDecreaseRate * Time.deltaTime;
        if (currentSpawnInterval < minSpawnInterval) {
            currentSpawnInterval = minSpawnInterval;
        }

        // Increase fall speed over time
        currentFallSpeed += fallSpeedIncreaseRate * Time.deltaTime;
        if (currentFallSpeed > maxFallSpeed) {
            currentFallSpeed = maxFallSpeed;
        }

        // Spawn timer logic
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

    private void UpdateUI() {
        if (timerTMP != null) timerTMP.text = Mathf.CeilToInt(timeRemaining).ToString() + "s";
        if (scoreTMP != null) scoreTMP.text = $"Score: {score}";
    }

    private void SpawnRandomCard() {
        if (sortPuzzleArray == null || sortPuzzleArray.Length == 0) return;

        SortPuzzle randomPuzzle = sortPuzzleArray[Random.Range(0, sortPuzzleArray.Length)];
        
        Masters_FallingSortPhraseCard newCard = Instantiate(phraseCardPrefab, topSpawnPoint.parent);
        newCard.SetExpression(randomPuzzle.expression);
        
        // We store the sort type directly on the card logic by mapping it in a dictionary if needed, 
        // but it's cleaner to just attach data. Since card doesn't hold data, we use a simple wrapper or dictionary.
        // Wait, falling card doesn't store SortType. Let's add it dynamically or store it in a lookup.
        // I will add a dynamic property or use a parallel dictionary for the puzzle data.
        
        RectTransform cardRect = newCard.GetComponent<RectTransform>();
        cardRect.position = topSpawnPoint.position;
        cardRect.localScale = Vector3.one;
        newCard.gameObject.SetActive(true);

        newCard.OnDragEnded += HandleCardDragEnded;
        
        activeCards.Add(newCard);
        // We need to know which puzzle this card belongs to evaluate it later.
        // For simplicity, we can just look up the expression string to find the puzzle,
        // since expressions are unique in this game.
    }

    private void HandleCardDragEnded(Masters_FallingSortPhraseCard card) {
        if (!activeCards.Contains(card)) return;

        float minDistance = float.MaxValue;
        Masters_FallingSortBin closestBin = null;

        foreach (var bin in sortBinArray) {
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
            } else {
                // If they never dragged it, default to the bin it happens to be hovering over
                float minDistance = float.MaxValue;
                foreach (var bin in sortBinArray) {
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
        // Find puzzle by matching the expression (assuming expressions are unique)
        SortPuzzle matchedPuzzle = null;
        string cardText = card.GetComponentInChildren<TextMeshProUGUI>().text;
        foreach (var puzzle in sortPuzzleArray) {
            if (puzzle.expression == cardText) {
                matchedPuzzle = puzzle;
                break;
            }
        }

        if (matchedPuzzle != null && bin.MatchesUnit8(matchedPuzzle.sortType)) {
            // Correct
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            score++;
            UpdateUI();
        } else {
            // Wrong
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        activeCards.Remove(card);
        if (cardTargetBins.ContainsKey(card)) cardTargetBins.Remove(card);
        
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack).OnComplete(() => {
            if (card != null) Destroy(card.gameObject);
        });
    }

    private void GameOver() {
        timeRemaining = 0;
        isGameActive = false;
        UpdateUI();

        foreach (var card in activeCards) {
            if (card != null) {
                card.GetComponent<RectTransform>().DOScale(Vector3.zero, 0.3f).OnComplete(() => {
                    Destroy(card.gameObject);
                });
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
        Masters_AudioManager.Instance.StopVoiceOver();
        
        if (nextLessonSO != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            if (topic != Masters_Topic.None) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
