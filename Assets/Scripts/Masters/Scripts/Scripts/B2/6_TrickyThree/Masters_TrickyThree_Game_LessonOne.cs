using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_TrickyThree_Game_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class GameQuestion {
        [TextArea]
        [Tooltip("The question to flash, e.g., 'Is he?'")]
        public string promptText;
        public string correctAnswer;
        public string distractorOne;
        public string distractorTwo;
    }

    [Header("Game Data")]
    [SerializeField] private GameQuestion[] questions;
    [SerializeField] private float chipSpeed = 200f;
    [SerializeField] private float roundDuration = 60f;
    [SerializeField] private int startingLives = 3;
    [SerializeField] private int scorePerCorrect = 10;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI promptTMP;
    [SerializeField] private RectTransform boundaryArea;
    [SerializeField] private GameObject chipPrefab;
    
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private GameObject[] lifeIcons;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private int currentQuestionIndex = 0;
    private int currentScore = 0;
    private int currentLives;
    private float timeRemaining;
    private bool isGameActive = false;
    private bool isRoundActive = false;

    private List<Masters_ReplyRush_Chip> activeChips = new List<Masters_ReplyRush_Chip>();

    protected override void Awake() {
        base.Awake();
        currentLives = startingLives;
        timeRemaining = roundDuration;
        UpdateHUD();
    }

    protected override void Start() {
        base.Start();
        // Start game immediately
        StartGame();
    }

    private void StartGame() {
        isGameActive = true;
        LoadQuestion(0);
    }

    private void Update() {
        if (!isGameActive) return;

        if (timeRemaining > 0) {
            timeRemaining -= Time.deltaTime;
            timerTMP.text = Mathf.CeilToInt(timeRemaining).ToString() + "s";

            if (timeRemaining <= 0) {
                // Time ran out!
                timeRemaining = 0;
                timerTMP.text = "0s";
                EndGame();
            }
        }
    }

    private void LoadQuestion(int index) {
        if (index >= questions.Length || currentLives <= 0 || timeRemaining <= 0) {
            EndGame();
            return;
        }

        currentQuestionIndex = index;
        GameQuestion q = questions[currentQuestionIndex];

        // Flash Question
        promptTMP.text = q.promptText;
        promptTMP.transform.DOPunchScale(Vector3.one * 0.2f, 0.5f, 5, 1f);

        // Spawn Chips
        SpawnChipsForQuestion(q);
        
        isRoundActive = true;
    }

    private void SpawnChipsForQuestion(GameQuestion q) {
        ClearActiveChips();

        List<string> options = new List<string> { q.correctAnswer, q.distractorOne, q.distractorTwo };
        
        // Shuffle options
        for (int i = 0; i < options.Count; i++) {
            string temp = options[i];
            int randomIndex = Random.Range(i, options.Count);
            options[i] = options[randomIndex];
            options[randomIndex] = temp;
        }

        foreach (string option in options) {
            GameObject chipObj = Instantiate(chipPrefab, boundaryArea);
            Masters_ReplyRush_Chip chip = chipObj.GetComponent<Masters_ReplyRush_Chip>();
            
            bool isCorrect = (option == q.correctAnswer);
            
            // Randomize spawn position inside boundary
            RectTransform chipRect = chip.GetComponent<RectTransform>();
            float halfW = chipRect.rect.width / 2f;
            float halfH = chipRect.rect.height / 2f;
            
            float rx = Random.Range(boundaryArea.rect.xMin + halfW, boundaryArea.rect.xMax - halfW);
            float ry = Random.Range(boundaryArea.rect.yMin + halfH, boundaryArea.rect.yMax - halfH);
            
            chipRect.anchoredPosition = new Vector2(rx, ry);

            chip.Initialize(option, isCorrect, boundaryArea, chipSpeed, this);
            activeChips.Add(chip);
        }
    }

    public void HandleChipClicked(Masters_ReplyRush_Chip clickedChip, bool isCorrect) {
        if (!isRoundActive || !isGameActive) return;
        isRoundActive = false;

        if (isCorrect) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            currentScore += scorePerCorrect;
            UpdateHUD();
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            LoseLife();
        }

        foreach (var chip in activeChips) {
            if (chip != null) chip.ExplodeAndDestroy();
        }
        activeChips.Clear();

        if (isGameActive) {
            Invoke(nameof(LoadNextQuestion), 0.5f);
        }
    }

    private void LoseLife() {
        currentLives--;
        if (currentLives >= 0 && currentLives < lifeIcons.Length) {
            lifeIcons[currentLives].SetActive(false);
            
            // Shake screen effect
            Camera.main.transform.DOShakePosition(0.3f, 0.5f);
        }

        if (currentLives <= 0) {
            EndGame();
        }
    }

    private void LoadNextQuestion() {
        LoadQuestion(currentQuestionIndex + 1);
    }

    private void EndGame() {
        isGameActive = false;
        isRoundActive = false;
        ClearActiveChips();

        // Game over sequence
        promptTMP.text = $"Time's up!\nFinal Score: {currentScore}";
        promptTMP.transform.DOScale(Vector3.one * 1.2f, 0.5f);

        nextButton.interactable = true;
        NextButtonAnimation();
    }

    private void ClearActiveChips() {
        foreach (var chip in activeChips) {
            if (chip != null) {
                chip.DestroySilently();
            }
        }
        activeChips.Clear();
    }

    private void UpdateHUD() {
        if (scoreTMP != null) scoreTMP.text = currentScore.ToString();
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        
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
