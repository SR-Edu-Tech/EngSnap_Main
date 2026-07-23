using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_LetsChoose_Game_LessonOne : Masters_Lesson {

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private float spawnInterval = 1.5f;
    [SerializeField] private float itemLifetime = 4f;
    
    [Header("Scoring Rules")]
    [SerializeField] private int pointsPerCorrect = 10;
    [SerializeField] private int pointsPenaltyPerWrong = 5;
    [SerializeField] private float timePenaltyForMiss = 5f;
    
    [Header("Star Rating Thresholds")]
    [SerializeField] private int scoreForOneStar = 50;
    [SerializeField] private int scoreForTwoStars = 100;
    [SerializeField] private int scoreForThreeStars = 150;
    
    [Header("Spawning Area")]
    [SerializeField] private RectTransform spawnArea; 
    [SerializeField] private Masters_PopupItem_LetsChoose popupItemPrefab;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Answers List")]
    [SerializeField] private List<string> correctAnswers;
    [SerializeField] private List<string> incorrectAnswers;

    [Header("Game Over / Quiz Complete Settings")]
    [SerializeField] private GameObject quizCompleteGameObject;
    [SerializeField] private GameObject gamePlayGameObject;
    [SerializeField] private Image[] starImageArray;
    [SerializeField] private Color goldStarColor = Color.yellow;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button closeButton;

    private float timeRemaining;
    private int score;
    private bool isGameActive;
    private float spawnTimer;

    protected override void Start() {
        base.Start();
        
        if (retryButton != null) {
            retryButton.onClick.AddListener(RetryGame);
        }
        
        if (closeButton != null) {
            closeButton.onClick.AddListener(CloseGame);
        }
        
        StartGame();
    }

    private void StartGame() {
        timeRemaining = gameDuration;
        score = 0;
        isGameActive = true;
        
        UpdateUI();
        
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(true);
    }

    private void RetryGame() {
        // Clear any popup items left on screen
        if (spawnArea != null) {
            foreach (Transform child in spawnArea) {
                Destroy(child.gameObject);
            }
        }
        
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

    private void Update() {
        if (!isGameActive) return;

        // Timer Logic
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0) {
            timeRemaining = 0;
            GameOver();
        }
        
        // Spawn Logic
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0) {
            SpawnItem();
            spawnTimer = spawnInterval;
        }

        UpdateUI();
    }

    private void SpawnItem() {
        if (correctAnswers.Count == 0 && incorrectAnswers.Count == 0) return;

        // Decide if we spawn a correct or incorrect item (50/50 chance)
        bool spawnCorrect = Random.value > 0.5f;
        string textToSpawn = "";
        
        if (spawnCorrect && correctAnswers.Count > 0) {
            textToSpawn = correctAnswers[Random.Range(0, correctAnswers.Count)];
        } else if (incorrectAnswers.Count > 0) {
            textToSpawn = incorrectAnswers[Random.Range(0, incorrectAnswers.Count)];
            spawnCorrect = false;
        } else if (correctAnswers.Count > 0) {
             textToSpawn = correctAnswers[Random.Range(0, correctAnswers.Count)];
             spawnCorrect = true;
        }

        // Instantiate
        Masters_PopupItem_LetsChoose newItem = Instantiate(popupItemPrefab, spawnArea);
        
        // Random position within spawn area
        float randomX = Random.Range(spawnArea.rect.xMin, spawnArea.rect.xMax);
        float randomY = Random.Range(spawnArea.rect.yMin, spawnArea.rect.yMax);
        newItem.GetComponent<RectTransform>().anchoredPosition = new Vector2(randomX, randomY);

        newItem.Initialize(textToSpawn, spawnCorrect, itemLifetime, this);
    }

    public void HandleItemClicked(bool isCorrect) {
        if (!isGameActive) return;

        if (isCorrect) {
            score += pointsPerCorrect;
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        } else {
            score -= pointsPenaltyPerWrong;
            // Prevent negative score if desired, or allow it
            if (score < 0) score = 0;
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
        UpdateUI();
    }

    public void HandleCorrectItemMissed() {
        if (!isGameActive) return;

        timeRemaining -= timePenaltyForMiss;
        if (timeRemaining <= 0) {
            timeRemaining = 0;
            GameOver();
        }
        
        // Play an incorrect sound or specific "missed" sound
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        
        UpdateUI();
    }

    private void UpdateUI() {
        if (timerTMP != null) timerTMP.text = Mathf.CeilToInt(timeRemaining).ToString() + "s";
        if (scoreTMP != null) scoreTMP.text = "Score: " + score;
    }

    private void ShowQuizCompleteScreen() {
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(true);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(false);

        // Light up stars based on score
        int starsEarned = 0;
        if (score >= scoreForThreeStars) {
            starsEarned = 3;
        } else if (score >= scoreForTwoStars) {
            starsEarned = 2;
        } else if (score >= scoreForOneStar) {
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
        ShowQuizCompleteScreen();
    }

    public bool IsGameActive() {
        return isGameActive;
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
