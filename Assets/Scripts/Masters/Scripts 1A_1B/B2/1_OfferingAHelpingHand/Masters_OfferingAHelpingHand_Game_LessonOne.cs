using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_OfferingAHelpingHand_Game_LessonOne : Masters_Lesson {

    [Header("Game Settings")]
    [SerializeField] private float gameDuration = 60f;
    [SerializeField] private int maxHP = 3;
    [SerializeField] private float spawnInterval = 2f;
    
    [Header("Spawning Area")]
    [SerializeField] private RectTransform spawnArea; 
    [SerializeField] private Masters_FallingItem fallingItemPrefab;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI hpTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private Masters_BasketController basketController;
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

    private int currentHP;
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
        currentHP = maxHP;
        timeRemaining = gameDuration;
        score = 0;
        isGameActive = true;
        
        basketController.Setup(this);
        UpdateUI();
        
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(true);
    }

    private void RetryGame() {
        if (starImageArray != null) {
            foreach (Image image in starImageArray) {
                if (image != null) image.color = Color.white;
            }
        }
        
        // Clear any falling items left on screen
        if (spawnArea != null) {
            foreach (Transform child in spawnArea) {
                Destroy(child.gameObject);
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
            GameWon();
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
        }

        // Instantiate
        Masters_FallingItem newItem = Instantiate(fallingItemPrefab, spawnArea);
        
        // Random horizontal position within spawn area
        float randomX = Random.Range(spawnArea.rect.xMin, spawnArea.rect.xMax);
        newItem.GetComponent<RectTransform>().anchoredPosition = new Vector2(randomX, 0);

        newItem.Initialize(textToSpawn, spawnCorrect, this);
    }

    public void HandleItemCaught(bool isCorrect) {
        if (!isGameActive) return;

        if (isCorrect) {
            score++;
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        } else {
            currentHP--;
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            if (currentHP <= 0) {
                currentHP = 0;
                GameOver();
            }
        }
        UpdateUI();
    }

    private void UpdateUI() {
        if (timerTMP != null) timerTMP.text = Mathf.CeilToInt(timeRemaining).ToString() + "s";
        if (hpTMP != null) hpTMP.text = "HP: " + currentHP;
        if (scoreTMP != null) scoreTMP.text = "Score: " + score;
    }

    private void ShowQuizCompleteScreen() {
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(true);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(false);

        // Light up stars based on remaining HP (Max 3 stars)
        if (starImageArray != null) {
            for (int i = 0; i < currentHP; i++) {
                if (i < starImageArray.Length && starImageArray[i] != null) {
                    starImageArray[i].color = goldStarColor;
                }
            }
        }

        if (retryButton != null) {
            retryButton.gameObject.SetActive(true);
        }

        if (closeButton != null) {
            closeButton.gameObject.SetActive(true);
        }

        if (currentHP > 0) {
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
        }
    }

    private void GameWon() {
        isGameActive = false;
        ShowQuizCompleteScreen();
    }

    private void GameOver() {
        isGameActive = false;
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect); 
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
