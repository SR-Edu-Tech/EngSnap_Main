using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Core controller for Unit 1: Polished Communication - Writing Lesson One (W01).
/// Standalone implementation independent of older book scripts.
/// Manages incoming message evaluation, keyword/exact match validation, hint system, and progression.
/// </summary>
public class Masters_PolishedCommunication_Writing_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class WritingQuestion {
        public string incomingMessageText;
        public string[] requiredKeywords;
        public string[] acceptableExactMatches;
        public string hintText;
    }

    [Header("Writing Lesson Data")]
    [SerializeField]
    protected WritingQuestion[] questions;

    [Header("UI Components")]
    [SerializeField]
    private TextMeshProUGUI promptTMP;
    [SerializeField]
    private TMP_InputField inputField;
    [SerializeField]
    private Button checkButton;
    [SerializeField]
    private TextMeshProUGUI progressCountTMP;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;

    [Header("Hint UI Components")]
    [SerializeField]
    private GameObject hintPanel;
    [SerializeField]
    private TextMeshProUGUI hintTMP;

    [Header("Settings & Visual Feedback")]
    [SerializeField]
    private float timeBetweenQuestions = 1.5f;
    [SerializeField]
    private Color correctColor = Color.green;
    [SerializeField]
    private Color incorrectColor = Color.red;
    [SerializeField]
    private Color defaultInputFieldColor = Color.white;
    [SerializeField]
    private Image inputFieldBackground;

    private int currentQuestionIndex;
    private WritingQuestion currentQuestion;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Writing;

        if (checkButton != null) {
            checkButton.onClick.RemoveAllListeners();
            checkButton.onClick.AddListener(OnCheckButtonClicked);
        }

        if (hintPanel != null) {
            hintPanel.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();
        currentQuestionIndex = 0;
        if (questions != null && questions.Length > 0) {
            LoadQuestion(currentQuestionIndex);
        } else {
            Debug.LogWarning($"[WritingLessonOne] No questions assigned on {gameObject.name}.");
        }
    }

    private void LoadQuestion(int index) {
        if (questions == null || index < 0 || index >= questions.Length) {
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            return;
        }

        currentQuestion = questions[index];

        if (promptTMP != null) promptTMP.text = currentQuestion.incomingMessageText;
        if (progressCountTMP != null) progressCountTMP.text = $"{index + 1}/{questions.Length}";

        if (inputField != null) {
            inputField.text = "";
            inputField.interactable = true;
            if (inputFieldBackground != null) inputFieldBackground.color = defaultInputFieldColor;
            inputField.Select();
            inputField.ActivateInputField();
        }

        if (checkButton != null) checkButton.interactable = true;
        if (hintPanel != null) hintPanel.SetActive(false);
    }

    private void OnCheckButtonClicked() {
        if (currentQuestion == null || inputField == null) return;

        string userInput = inputField.text.Trim();
        if (string.IsNullOrEmpty(userInput)) return;

        string normalizedInput = userInput.ToLowerInvariant();

        // Check exact matches first
        bool isCorrect = false;
        if (currentQuestion.acceptableExactMatches != null && currentQuestion.acceptableExactMatches.Length > 0) {
            foreach (string exact in currentQuestion.acceptableExactMatches) {
                if (!string.IsNullOrEmpty(exact) && normalizedInput.Contains(exact.ToLowerInvariant().Trim())) {
                    isCorrect = true;
                    break;
                }
            }
        }

        // If no exact match, check if all required keywords exist
        if (!isCorrect && currentQuestion.requiredKeywords != null && currentQuestion.requiredKeywords.Length > 0) {
            bool allKeywordsFound = true;
            HashSet<string> inputWords = ExtractWords(normalizedInput);

            foreach (string keyword in currentQuestion.requiredKeywords) {
                if (string.IsNullOrEmpty(keyword)) continue;
                string cleanKeyword = keyword.ToLowerInvariant().Trim();
                
                if (cleanKeyword.Contains(" ")) {
                    if (!normalizedInput.Contains(cleanKeyword)) {
                        allKeywordsFound = false;
                        break;
                    }
                } else {
                    if (!inputWords.Contains(cleanKeyword) && !normalizedInput.Contains(cleanKeyword)) {
                        allKeywordsFound = false;
                        break;
                    }
                }
            }
            isCorrect = allKeywordsFound;
        }

        if (isCorrect) {
            CorrectAnswer();
        } else {
            WrongAnswer();
        }
    }

    private HashSet<string> ExtractWords(string text) {
        HashSet<string> words = new HashSet<string>();
        string cleanText = System.Text.RegularExpressions.Regex.Replace(text, @"[^a-z0-9\s]", "");
        string[] split = cleanText.Split(new char[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        foreach (string w in split) {
            words.Add(w);
        }
        return words;
    }

    private void CorrectAnswer() {
        if (inputField != null) inputField.interactable = false;
        if (checkButton != null) checkButton.interactable = false;
        if (inputFieldBackground != null) inputFieldBackground.color = correctColor;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        Invoke(nameof(NextQuestion), timeBetweenQuestions);
    }

    private void WrongAnswer() {
        if (inputFieldBackground != null) inputFieldBackground.color = incorrectColor;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        ShowHint();
    }

    private void ShowHint() {
        if (hintPanel != null && currentQuestion != null && !string.IsNullOrEmpty(currentQuestion.hintText)) {
            if (hintTMP != null) hintTMP.text = currentQuestion.hintText;
            hintPanel.SetActive(true);
        }
    }

    private void NextQuestion() {
        currentQuestionIndex++;
        if (questions != null && currentQuestionIndex < questions.Length) {
            LoadQuestion(currentQuestionIndex);
        } else {
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
        }
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
