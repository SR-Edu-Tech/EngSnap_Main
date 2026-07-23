using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_SequenceYourThoughts_Game_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class SortPuzzle {
        public string expression;
        public Masters_Unit12_FallingSortCategory sortType;
        public Masters_Unit13_FallingSortCategory unit13SortType;
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

    private List<SortPuzzle> shuffledBag = new List<SortPuzzle>();
    private SortPuzzle lastSpawnedPuzzle;

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
        foreach (var card in activeCards) {
            if (card != null) Destroy(card.gameObject);
        }
        activeCards.Clear();
        cardTargetBins.Clear();
        shuffledBag.Clear();
        lastSpawnedPuzzle = null;

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

    private IEnumerator InitialSpawnDelayCoroutine() {
        yield return new WaitForSeconds(initialSpawnDelay);
        isGameActive = true;
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

    private void UpdateUI() {
        if (timerTMP != null) timerTMP.text = Mathf.CeilToInt(timeRemaining).ToString() + "s";
        if (scoreTMP != null) scoreTMP.text = $"Score: {score}";
    }

    protected virtual void SpawnRandomCard() {
        if (sortPuzzleArray == null || sortPuzzleArray.Length == 0) return;

        if (shuffledBag.Count == 0) {
            shuffledBag.AddRange(sortPuzzleArray);
            for (int i = 0; i < shuffledBag.Count; i++) {
                SortPuzzle temp = shuffledBag[i];
                int randIndex = Random.Range(i, shuffledBag.Count);
                shuffledBag[i] = shuffledBag[randIndex];
                shuffledBag[randIndex] = temp;
            }
            if (lastSpawnedPuzzle != null && shuffledBag.Count > 1 && shuffledBag[0] == lastSpawnedPuzzle) {
                int swapIndex = Random.Range(1, shuffledBag.Count);
                SortPuzzle temp = shuffledBag[0];
                shuffledBag[0] = shuffledBag[swapIndex];
                shuffledBag[swapIndex] = temp;
            }
        }

        SortPuzzle randomPuzzle = shuffledBag[0];
        shuffledBag.RemoveAt(0);
        lastSpawnedPuzzle = randomPuzzle;
        
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
        if (!activeCards.Contains(card)) return;

        float minDistance = float.MaxValue;
        Masters_FallingSortBin closestBin = null;

        foreach (var bin in sortBinArray) {
            if (bin == null) continue;
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
                float minDistance = float.MaxValue;
                foreach (var bin in sortBinArray) {
                    if (bin == null) continue;
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

    protected virtual void EvaluateDrop(Masters_FallingSortPhraseCard card, Masters_FallingSortBin bin) {
        SortPuzzle matchedPuzzle = null;
        string cardText = card.GetComponentInChildren<TextMeshProUGUI>().text;
        foreach (var puzzle in sortPuzzleArray) {
            if (puzzle.expression == cardText) {
                matchedPuzzle = puzzle;
                break;
            }
        }

        if (matchedPuzzle != null && (bin.MatchesUnit12(matchedPuzzle.sortType) || bin.MatchesUnit13(matchedPuzzle.unit13SortType))) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            score++;
            UpdateUI();
        } else {
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
