using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Masters_TapTangledBubbleData {
    public string sentenceText;
    public bool isJumbled;
}

public class Masters_JumbledWords_Game_LessonTwo : Masters_Lesson {

    [Header("Game Data")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private int targetScore = 10;
    [SerializeField] private List<Masters_TapTangledBubbleData> bubblePool;
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private Button jumbledPaddleButton;
    [SerializeField] private Button okPaddleButton;
    [SerializeField] private Masters_LessonSO nextLessonSO;
    
    [Header("Spawning & Bubbles")]
    [SerializeField] private RectTransform spawnArea;
    [SerializeField] private Masters_JumbledWords_BubbleItem bubblePrefab;
    [SerializeField] private float delayBetweenSpawns = 2f;
    [SerializeField] private float initialDriftSpeed = 100f;
    [SerializeField] private float speedIncreasePerScore = 10f;
    
    [Header("Game Over / Quiz Complete Settings")]
    [SerializeField] private GameObject quizCompleteGameObject;
    [SerializeField] private GameObject gamePlayGameObject;
    [SerializeField] private TextMeshProUGUI finalScoreTMP;
    [SerializeField] private Image[] starImageArray;
    [SerializeField] private Color goldStarColor = Color.yellow;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button closeButton;

    private float timeRemaining;
    private int score;
    private bool isGameActive;
    private float spawnTimer;
    private int lastLaneIndex = -1;
    private float lastTapTime = 0f;
    private const float TAP_COOLDOWN = 0.1f;
    
    private List<Masters_JumbledWords_BubbleItem> activeBubbles = new List<Masters_JumbledWords_BubbleItem>();
    
    protected override void Start() {
        base.Start();
        if (retryButton != null) retryButton.onClick.AddListener(RetryGame);
        if (closeButton != null) closeButton.onClick.AddListener(CloseGame);
        
        // I am removing the AddListener here because if you assign the OnClick event 
        // in the Unity Inspector as well, the button will fire TWICE (or more) per tap!
        
        StartGame();
    }
    
    private void StartGame() {
        score = 0;
        timeRemaining = gameDuration;
        spawnTimer = delayBetweenSpawns;
        isGameActive = true;
        lastLaneIndex = -1;
        
        UpdateScoreUI();
        UpdateTimerUI();
        
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(true);
    }
    
    private void Update() {
        if (!isGameActive) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0) {
            timeRemaining = 0;
            GameOver();
        }
        UpdateTimerUI();
        
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0) {
            SpawnBubble();
            // Decrease interval slightly as game goes on to make it harder
            float newInterval = delayBetweenSpawns - (score * 0.05f);
            spawnTimer = Mathf.Max(0.5f, newInterval);
        }
    }
    
    private void SpawnBubble() {
        if (bubblePrefab == null || spawnArea == null || bubblePool.Count == 0) return;

        Masters_TapTangledBubbleData data = bubblePool[Random.Range(0, bubblePool.Count)];
        Masters_JumbledWords_BubbleItem bubble = Instantiate(bubblePrefab, spawnArea);
        
        // Use lanes to prevent horizontal overlap of consecutive bubbles
        int numLanes = 3;
        float laneWidth = spawnArea.rect.width / numLanes;
        int laneIndex = Random.Range(0, numLanes);
        
        // Make sure it doesn't spawn in the exact same lane as the last bubble
        if (laneIndex == lastLaneIndex) {
            laneIndex = (laneIndex + 1) % numLanes;
        }
        lastLaneIndex = laneIndex;

        float minX = spawnArea.rect.xMin + (laneIndex * laneWidth);
        float maxX = minX + laneWidth;

        float padding = bubble.GetComponent<RectTransform>().rect.width / 2f;
        float randomX = Random.Range(minX + padding, maxX - padding);
        
        float spawnY = spawnArea.rect.yMin - bubble.GetComponent<RectTransform>().rect.height;
        bubble.GetComponent<RectTransform>().anchoredPosition = new Vector2(randomX, spawnY);
        
        float currentSpeed = initialDriftSpeed + (score * speedIncreasePerScore);
        bubble.Initialize(data.sentenceText, data.isJumbled, currentSpeed, spawnArea, this);
        activeBubbles.Add(bubble);
    }

    public void OnJumbledPaddleTapped() {
        if (Time.time - lastTapTime < TAP_COOLDOWN) return;
        lastTapTime = Time.time;
        ClassifyLowestBubble(true);
    }

    public void OnOkPaddleTapped() {
        if (Time.time - lastTapTime < TAP_COOLDOWN) return;
        lastTapTime = Time.time;
        ClassifyLowestBubble(false);
    }
    
    private void ClassifyLowestBubble(bool tappedJumbled) {
        if (!isGameActive || activeBubbles.Count == 0) return;

        // Get the oldest active bubble (index 0)
        Masters_JumbledWords_BubbleItem lowestBubble = activeBubbles[0];
        
        if (lowestBubble.IsJumbled() == tappedJumbled) {
            // Correct
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            score++;
            lowestBubble.Pop();
        } else {
            // Incorrect
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            score = Mathf.Max(0, score - 1); // Deduct score
            lowestBubble.Buzz();
            // We'll leave the bubble to drift off or destroy it? Let's let it drift, so we just remove it from the 'active' list that we classify
        }
        
        activeBubbles.RemoveAt(0);
        UpdateScoreUI();
    }
    
    public void HandleBubbleEscaped(Masters_JumbledWords_BubbleItem bubble) {
        if (activeBubbles.Contains(bubble)) {
            activeBubbles.Remove(bubble);
        }
    }
    
    private void UpdateScoreUI() {
        if (scoreTMP != null) scoreTMP.text = "Score: " + score;
    }

    private void UpdateTimerUI() {
        if (timerTMP != null) timerTMP.text = Mathf.CeilToInt(timeRemaining).ToString() + "s";
    }

    private void ClearBubbles() {
        // Destroy all existing bubbles
        if (spawnArea != null) {
            foreach (Transform child in spawnArea) {
                Destroy(child.gameObject);
            }
        }
        activeBubbles.Clear();
    }

    private void RetryGame() {
        ClearBubbles();
        
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

        // Reset stars
        if (starImageArray != null) {
            foreach (Image image in starImageArray) {
                if (image != null) image.color = Color.white;
            }
        }
    }
    
    private void ShowQuizCompleteScreen() {
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(true);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(false);
        if (finalScoreTMP != null) finalScoreTMP.text = "Final Score: " + score;

        // Light up stars based on score (e.g. 1/3, 2/3, 3/3 of targetScore)
        int starsEarned = 0;
        if (score >= targetScore) {
            starsEarned = 3;
        } else if (score >= targetScore * 0.66f) {
            starsEarned = 2;
        } else if (score >= targetScore * 0.33f) {
            starsEarned = 1;
        }

        if (starImageArray != null) {
            for (int i = 0; i < starImageArray.Length; i++) {
                if (starImageArray[i] != null) {
                    starImageArray[i].color = (i < starsEarned) ? goldStarColor : Color.white;
                }
            }
        }

        if (retryButton != null) {
            retryButton.gameObject.SetActive(true);
        }

        if (closeButton != null) {
            closeButton.gameObject.SetActive(true);
        }

        if (nextButton != null) {
            nextButton.interactable = true;
            NextButtonAnimation();
        }
    }
    
    private void GameOver() {
        isGameActive = false;
        ClearBubbles();
        ShowQuizCompleteScreen();
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
