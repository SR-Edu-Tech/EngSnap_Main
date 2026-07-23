using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Game Manager for Chatting Bees - Writing Lesson 1 (W01: Reply to the Message).
/// Simulates a phone screen where the user types a free-text reply.
/// Validates using required keywords/regex rather than just exact string matching,
/// and provides a custom hint if the user fails twice.
/// </summary>
public class Masters_ChattingBees_Writing_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class WritingQuestion {
        [TextArea]
        [Tooltip("The incoming message the student is replying to, e.g., 'Can't wait for tonight!'")]
        public string incomingMessageText;
        
        [Tooltip("Keywords or phrases that MUST be present in the user's answer for it to be marked correct. (Case insensitive)")]
        public string[] requiredKeywords;

        [Tooltip("Optional: Acceptable exact sentences. If the user types any of these exactly, it's correct regardless of keywords.")]
        public string[] acceptableExactMatches;

        [TextArea]
        [Tooltip("The custom hint to show after 2 failed attempts. e.g., 'Try sounding a bit more excited.'")]
        public string hintText;
    }

    [SerializeField]
    private WritingQuestion[] questions;

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
        
        if (promptTMP != null) promptTMP.text = question.incomingMessageText;
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

        bool isCorrect = false;
        string normalizedInput = string.Join(" ", ExtractWords(input));

        // 1. Check Exact Matches
        if (currentQ.acceptableExactMatches != null && currentQ.acceptableExactMatches.Length > 0) {
            foreach (string correctVariation in currentQ.acceptableExactMatches) {
                if (string.IsNullOrEmpty(correctVariation)) continue;
                
                string normalizedCorrect = string.Join(" ", ExtractWords(correctVariation));
                if (normalizedInput == normalizedCorrect) {
                    isCorrect = true;
                    break;
                }
            }
        }

        // 2. Check Keyword Validation (If not already correct via exact match)
        if (!isCorrect && currentQ.requiredKeywords != null && currentQ.requiredKeywords.Length > 0) {
            // If the user's raw input contains ANY of the required keywords/phrases, we mark it correct.
            // (Depending on design, you might want to require ALL keywords, but usually ANY keyword from a list of synonyms is enough)
            string lowerInput = input.ToLowerInvariant();
            foreach (string keyword in currentQ.requiredKeywords) {
                if (string.IsNullOrEmpty(keyword)) continue;

                if (lowerInput.Contains(keyword.ToLowerInvariant())) {
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

    private void WrongAnswer() {
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
        }
    }

    private void ShowHint() {
        if (hintPanel != null) hintPanel.SetActive(true);
        if (hintTMP != null) {
            WritingQuestion currentQ = questions[currentQuestionIndex];
            
            // Show the custom hint text. If empty, fallback to showing the first exact match.
            if (!string.IsNullOrEmpty(currentQ.hintText)) {
                hintTMP.text = currentQ.hintText;
            } else if (currentQ.acceptableExactMatches != null && currentQ.acceptableExactMatches.Length > 0) {
                hintTMP.text = currentQ.acceptableExactMatches[0];
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
