using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_WordSwitch_Writing_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class WritingQuestion {
        [TextArea]
        public string incomingMessageText;
        public string[] requiredKeywords;
        public string[] acceptableExactMatches;
        [TextArea]
        public string hintText;
    }

    [SerializeField] private WritingQuestion[] questions;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI promptTMP;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button checkButton;
    [SerializeField] private TextMeshProUGUI progressCountTMP;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Hint System")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TextMeshProUGUI hintTMP;

    [Header("Animation & Feedback")]
    [SerializeField] private float timeBetweenQuestions = 1.5f;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private Color defaultInputFieldColor = Color.white;
    [SerializeField] private Image inputFieldBackground;

    private int currentQuestionIndex = 0;
    private bool canCheck = false;
    private int failedAttempts = 0;

    protected override void Awake() {
        base.Awake();
        if (checkButton != null) {
            checkButton.onClick.AddListener(OnCheckButtonClicked);
        }
        if (nextButton != null) {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();
        if (questions != null && questions.Length > 0) {
            LoadQuestion(0);
        }
    }

    private void LoadQuestion(int index) {
        if (questions == null || index >= questions.Length) {
            if (inputField != null) inputField.gameObject.SetActive(false);
            if (promptTMP != null) promptTMP.gameObject.SetActive(false);
            if (checkButton != null) checkButton.gameObject.SetActive(false);
            
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }
            NextButtonAnimation();
            return;
        }

        currentQuestionIndex = index;
        if (progressCountTMP != null) {
            progressCountTMP.text = $"{currentQuestionIndex + 1}/{questions.Length}";
        }

        WritingQuestion question = questions[currentQuestionIndex];
        
        if (promptTMP != null) promptTMP.text = question.incomingMessageText;
        if (inputField != null) inputField.text = "";
        
        if (inputFieldBackground != null) {
            inputFieldBackground.color = defaultInputFieldColor;
        }

        failedAttempts = 0;
        if (hintPanel != null) hintPanel.SetActive(false);

        canCheck = true;
        if (checkButton != null) checkButton.interactable = true;
    }

    private void OnCheckButtonClicked() {
        if (!canCheck || inputField == null) return;

        string input = inputField.text.Trim();
        if (string.IsNullOrEmpty(input)) {
            inputField.transform.DOPunchPosition(Vector3.right * 5f, 0.3f, 10, 1f);
            return;
        }

        WritingQuestion currentQ = questions[currentQuestionIndex];
        bool isCorrect = false;

        string cleanInput = input.ToLowerInvariant().Replace(".", "").Replace("!", "").Replace("?", "").Trim();

        // Check against required keywords (accepted 12 synonyms)
        if (currentQ.requiredKeywords != null && currentQ.requiredKeywords.Length > 0) {
            foreach (string keyword in currentQ.requiredKeywords) {
                if (string.IsNullOrEmpty(keyword)) continue;
                if (cleanInput == keyword.ToLowerInvariant().Trim()) {
                    isCorrect = true;
                    break;
                }
            }
        }

        if (!isCorrect) {
            WrongAnswer();
            return;
        }

        CorrectAnswer();
    }

    private void CorrectAnswer() {
        canCheck = false;
        if (checkButton != null) checkButton.interactable = false;
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

        if (hintPanel != null) hintPanel.SetActive(false);

        if (inputFieldBackground != null) {
            inputFieldBackground.color = correctColor;
        }

        Invoke(nameof(NextQuestion), timeBetweenQuestions);
    }

    private void WrongAnswer() {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        
        if (inputFieldBackground != null) {
            inputFieldBackground.DOKill();
            inputFieldBackground.DOColor(incorrectColor, 0.2f).OnComplete(() => {
                inputFieldBackground.DOColor(defaultInputFieldColor, 0.3f);
            });
        }
        
        if (inputField != null) {
            inputField.transform.DOPunchPosition(Vector3.right * 10f, 0.3f, 10, 1f);
        }

        failedAttempts++;
        if (failedAttempts >= 2) {
            ShowHint();
        }
    }

    private void ShowHint() {
        if (hintPanel != null) hintPanel.SetActive(true);
        if (hintTMP != null && questions != null) {
            WritingQuestion currentQ = questions[currentQuestionIndex];
            if (!string.IsNullOrEmpty(currentQ.hintText)) {
                hintTMP.text = currentQ.hintText;
            } else if (currentQ.requiredKeywords != null && currentQ.requiredKeywords.Length > 0) {
                hintTMP.text = $"Try using: {currentQ.requiredKeywords[0]}";
            }
        }
    }

    private void NextQuestion() {
        LoadQuestion(currentQuestionIndex + 1);
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
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
