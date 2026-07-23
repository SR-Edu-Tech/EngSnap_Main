using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_ChangeVoiceAndSoundSmart_Writing_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class WritingQuestion {
        [TextArea]
        [Tooltip("The question and hint, e.g., 'The postman delivers the letter.'")]
        public string promptText;
        
        [Tooltip("List all acceptable grammatically correct sentences. (Punctuation and case are ignored)")]
        public string[] acceptableCorrectSentences;
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

        bool isCorrect = false;
        string normalizedInput = string.Join(" ", ExtractWords(input));

        if (currentQ.acceptableCorrectSentences != null && currentQ.acceptableCorrectSentences.Length > 0) {
            foreach (string correctVariation in currentQ.acceptableCorrectSentences) {
                if (string.IsNullOrEmpty(correctVariation)) continue;
                
                string normalizedCorrect = string.Join(" ", ExtractWords(correctVariation));
                if (normalizedInput == normalizedCorrect) {
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
            if (currentQ.acceptableCorrectSentences != null && currentQ.acceptableCorrectSentences.Length > 0) {
                hintTMP.text = currentQ.acceptableCorrectSentences[0];
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
