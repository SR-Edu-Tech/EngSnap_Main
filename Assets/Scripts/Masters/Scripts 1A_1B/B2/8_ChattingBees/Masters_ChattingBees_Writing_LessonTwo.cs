using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Game Manager for Chatting Bees - Writing Lesson 2 (W02).
/// Simulates a free-text entry where the student must write a grammatically 
/// valid sentence starting with a specific master-list phrase.
/// Validates using Masters_SentenceValidator (dictionary check) and length pseudo-grammar check.
/// </summary>
public class Masters_ChattingBees_Writing_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class WritingQuestion {
        [TextArea]
        public string promptText;
        
        [Tooltip("Keywords or phrases that MUST be present in the user's answer (e.g. the master-list phrases).")]
        public string[] requiredKeywords;

        [TextArea]
        [Tooltip("The custom hint to show after 2 failed attempts.")]
        public string hintText;
    }

    [SerializeField]
    private WritingQuestion[] questions;

    [Header("Validation Rules")]
    [Tooltip("The minimum number of valid words the student must type AFTER the required keyword phrase to be considered a 'sentence'.")]
    [SerializeField] private int minExtraWordsRequired = 2;

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
        checkButton.onClick.AddListener(OnCheckButtonClicked);
    }

    protected override void Start() {
        base.Start();
        LoadQuestion(0);
    }

    private void LoadQuestion(int index) {
        if (index >= questions.Length) {
            // Lesson over
            inputField.gameObject.SetActive(false);
            if (promptTMP != null) promptTMP.gameObject.SetActive(false);
            checkButton.gameObject.SetActive(false);
            
            nextButton.interactable = true;
            NextButtonAnimation();
            return;
        }

        currentQuestionIndex = index;
        progressCountTMP.text = $"{currentQuestionIndex + 1}/{questions.Length}";

        WritingQuestion question = questions[currentQuestionIndex];
        
        if (promptTMP != null) promptTMP.text = question.promptText;
        inputField.text = "";
        
        if (inputFieldBackground != null) {
            inputFieldBackground.color = defaultInputFieldColor;
        }

        failedAttempts = 0;
        if (hintPanel != null) hintPanel.SetActive(false);

        canCheck = true;
        checkButton.interactable = true;
    }

    private void OnCheckButtonClicked() {
        if (!canCheck) return;

        string input = inputField.text.Trim();
        if (string.IsNullOrEmpty(input)) {
            // Give a little shake if empty
            inputField.transform.DOPunchPosition(Vector3.right * 5f, 0.3f, 10, 1f);
            return;
        }

        WritingQuestion currentQ = questions[currentQuestionIndex];

        // 1. Run the Dictionary Heuristic check
        if (!Masters_SentenceValidator.Validate(input, currentQ.requiredKeywords, out string feedback)) {
            // Failed either dictionary or keyword check
            Debug.Log($"Validation failed: {feedback}");
            WrongAnswer(feedback);
            return;
        }

        // 2. Length check (did they write more than just the phrase?)
        string lowerInput = input.ToLowerInvariant();
        string matchedKeyword = "";
        
        // Find which keyword they actually used
        foreach (string kw in currentQ.requiredKeywords) {
            if (lowerInput.Contains(kw.ToLowerInvariant())) {
                matchedKeyword = kw.ToLowerInvariant();
                break;
            }
        }

        // Strip the matched keyword out to see what's left
        string strippedInput = lowerInput.Replace(matchedKeyword, "").Trim();
        List<string> remainingWords = ExtractWords(strippedInput);

        if (remainingWords.Count < minExtraWordsRequired) {
            Debug.Log($"Validation failed: Needs at least {minExtraWordsRequired} more words after the phrase.");
            WrongAnswer($"You must write at least {minExtraWordsRequired} more words to complete the sentence!");
            return;
        }

        // Passed all checks!
        CorrectAnswer();
    }

    private List<string> ExtractWords(string sentence) {
        // Remove common terminal/separating punctuation, keep internal hyphens or apostrophes
        string clean = sentence.Replace(",", "").Replace(".", "").Replace("?", "").Replace("!", "").Replace(";", "").Replace(":", "").ToLower();
        return clean.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private void CorrectAnswer() {
        canCheck = false;
        checkButton.interactable = false;
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

        if (hintPanel != null) hintPanel.SetActive(false);

        if (inputFieldBackground != null) {
            inputFieldBackground.color = correctColor;
        }

        Invoke(nameof(NextQuestion), timeBetweenQuestions);
    }

    private void WrongAnswer(string optionalFeedbackMessage) {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        
        if (inputFieldBackground != null) {
            inputFieldBackground.DOKill();
            inputFieldBackground.DOColor(incorrectColor, 0.2f).OnComplete(() => {
                inputFieldBackground.DOColor(defaultInputFieldColor, 0.3f);
            });
        }
        
        inputField.transform.DOPunchPosition(Vector3.right * 10f, 0.3f, 10, 1f);

        failedAttempts++;
        if (failedAttempts >= 2) {
            ShowHint();
        } else if (!string.IsNullOrEmpty(optionalFeedbackMessage)) {
            // Optional: If we want to show immediate feedback on the first failure
            // we could flash the hint text briefly, but sticking to standard UI for now.
        }
    }

    private void ShowHint() {
        if (hintPanel != null) hintPanel.SetActive(true);
        if (hintTMP != null) {
            WritingQuestion currentQ = questions[currentQuestionIndex];
            
            if (!string.IsNullOrEmpty(currentQ.hintText)) {
                hintTMP.text = currentQ.hintText;
            } else if (currentQ.requiredKeywords != null && currentQ.requiredKeywords.Length > 0) {
                hintTMP.text = $"Try starting your sentence with: {currentQ.requiredKeywords[0]}";
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
