using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_ConditionalClause_Game_LessonOne : Masters_Lesson {
    
    [System.Serializable]
    public class SignCatcherPair {
        public string ifClause;
        public string willClause;
    }

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private int pairsToWin = 8;
    [SerializeField] private float dropPenaltySeconds = 5f;
    
    [Header("Spawning Settings")]
    [SerializeField] private float initialSpawnInterval = 3f;
    [SerializeField] private float minimumSpawnInterval = 1f;
    [SerializeField] private float initialFallSpeed = 150f;
    [SerializeField] private float dropThresholdY = -800f;

    [Header("UI References")]
    [SerializeField] private RectTransform leftSpawnArea;
    [SerializeField] private RectTransform rightSpawnArea;
    [SerializeField] private Masters_3A_SignCatcherTile tilePrefab;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Pair Data")]
    [SerializeField] private List<SignCatcherPair> matchPairs;

    [Header("Game Over Settings")]
    [SerializeField] private GameObject quizCompleteGameObject;
    [SerializeField] private GameObject gamePlayGameObject;
    [SerializeField] private Image[] starImageArray;
    [SerializeField] private Color goldStarColor = Color.yellow;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button closeButton;

    private float timeRemaining;
    private int score;
    private bool isGameActive;
    
    private float currentSpawnInterval;
    private float spawnTimer;

    private Masters_3A_SignCatcherTile currentlySelectedTile;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;
    }

    protected override void Start() {
        base.Start();
        
        if (retryButton != null) retryButton.onClick.AddListener(RetryGame);
        if (closeButton != null) closeButton.onClick.AddListener(CloseGame);
        
        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        }

        StartGame();
    }

    private void StartGame() {
        timeRemaining = gameDuration;
        score = 0;
        currentSpawnInterval = initialSpawnInterval;
        spawnTimer = currentSpawnInterval;
        isGameActive = true;
        currentlySelectedTile = null;
        
        UpdateUI();
        
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(true);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(false);
        if (nextButton != null) nextButton.interactable = false;

        // Initial spawn
        SpawnPair();
    }

    private void RetryGame() {
        ClearAllTiles();
        
        if (starImageArray != null) {
            foreach (Image image in starImageArray) {
                if (image != null) image.color = Color.white;
            }
        }
        
        StartGame();
    }

    private void CloseGame() {
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(true);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(false);
        
        if (starImageArray != null) {
            foreach (Image image in starImageArray) {
                if (image != null) image.color = Color.white;
            }
        }
    }

    private void ClearAllTiles() {
        if (leftSpawnArea != null) {
            foreach (Transform child in leftSpawnArea) Destroy(child.gameObject);
        }
        if (rightSpawnArea != null) {
            foreach (Transform child in rightSpawnArea) Destroy(child.gameObject);
        }
    }

    private void Update() {
        if (!isGameActive) return;

        timeRemaining -= Time.deltaTime;
        
        if (timeRemaining <= 0) {
            timeRemaining = 0;
            EndGame(score >= pairsToWin);
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0) {
            SpawnPair();
            
            // Gradually increase difficulty (spawn faster)
            currentSpawnInterval = Mathf.Max(minimumSpawnInterval, currentSpawnInterval - 0.1f);
            spawnTimer = currentSpawnInterval;
        }

        UpdateUI();
    }

    private void SpawnPair() {
        if (matchPairs == null || matchPairs.Count == 0) return;

        int randomPairId = Random.Range(0, matchPairs.Count);
        SignCatcherPair pair = matchPairs[randomPairId];

        bool spawnLeftFirst = Random.value > 0.5f;
        float desyncDelay = Random.Range(0.5f, 2.5f); // Random delay between the two halves

        StartCoroutine(SpawnDesyncedPairCoroutine(pair, randomPairId, spawnLeftFirst, desyncDelay));
    }

    private System.Collections.IEnumerator SpawnDesyncedPairCoroutine(SignCatcherPair pair, int pairId, bool spawnLeftFirst, float delay) {
        if (!isGameActive) yield break;

        if (spawnLeftFirst) {
            SpawnSingleTile(leftSpawnArea, pair.ifClause, pairId, true);
        } else {
            SpawnSingleTile(rightSpawnArea, pair.willClause, pairId, false);
        }

        yield return new WaitForSeconds(delay);
        
        if (!isGameActive) yield break;

        if (spawnLeftFirst) {
            SpawnSingleTile(rightSpawnArea, pair.willClause, pairId, false);
        } else {
            SpawnSingleTile(leftSpawnArea, pair.ifClause, pairId, true);
        }
    }

    private void SpawnSingleTile(RectTransform area, string text, int pairId, bool isAbbr) {
        if (area == null || tilePrefab == null) return;
        
        Masters_3A_SignCatcherTile newTile = Instantiate(tilePrefab, area);
        
        // Random horizontal position within area bounds
        float randomX = Random.Range(area.rect.xMin, area.rect.xMax);
        newTile.GetComponent<RectTransform>().anchoredPosition = new Vector2(randomX, 0);
        
        newTile.Setup(text, pairId, isAbbr, initialFallSpeed, dropThresholdY, this);
    }

    public void OnTileClicked(Masters_3A_SignCatcherTile clickedTile) {
        if (!isGameActive) return;

        if (currentlySelectedTile == null) {
            // First selection
            currentlySelectedTile = clickedTile;
            currentlySelectedTile.SetSelectedState(true);
        } 
        else if (currentlySelectedTile == clickedTile) {
            // Deselect
            currentlySelectedTile.SetSelectedState(false);
            currentlySelectedTile = null;
        } 
        else {
            // Second selection: Check match
            if (currentlySelectedTile.pairId == clickedTile.pairId && currentlySelectedTile.isAbbreviation != clickedTile.isAbbreviation) {
                // Correct Match
                score++;
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }
                
                currentlySelectedTile.LockAndDestroy();
                clickedTile.LockAndDestroy();
                currentlySelectedTile = null;
            } 
            else {
                // Incorrect Match
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }
                currentlySelectedTile.SetSelectedState(false);
                currentlySelectedTile = null;
            }
        }
        UpdateUI();
    }

    public void OnTileDropped(Masters_3A_SignCatcherTile droppedTile) {
        if (!isGameActive) return;

        // Apply time penalty
        timeRemaining -= dropPenaltySeconds;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect); 
        }
        
        // Deselect if it was the selected one dropping
        if (currentlySelectedTile == droppedTile) {
            currentlySelectedTile = null;
        }
    }

    private void UpdateUI() {
        if (timerTMP != null) timerTMP.text = Mathf.CeilToInt(timeRemaining).ToString() + "s";
        if (scoreTMP != null) scoreTMP.text = score.ToString();
    }

    private void EndGame(bool won) {
        isGameActive = false;
        
        if (Masters_AudioManager.Instance != null) {
            if (won) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            else Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
        
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(true);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(false);

        int starsEarned = won ? 3 : 0; // Simple win/loss logic for stars
        
        if (starImageArray != null) {
            for (int i = 0; i < starsEarned; i++) {
                if (i < starImageArray.Length && starImageArray[i] != null) {
                    starImageArray[i].color = goldStarColor;
                }
            }
        }

        if (retryButton != null) retryButton.gameObject.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(true);

        if (won && nextButton != null) {
            nextButton.interactable = true;
            NextButtonAnimation();
        }
    }

    public bool IsGameActive() {
        return isGameActive;
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
