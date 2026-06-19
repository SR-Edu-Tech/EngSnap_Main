using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_TrickyThree_Writing_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class WritingQuestion {
        [TextArea]
        [Tooltip("The question, e.g., 'Is she?'")]
        public string promptText;
        
        [Tooltip("List all acceptable grammatically correct Yes answers.")]
        public string[] acceptableYesSentences;

        [Tooltip("List all acceptable grammatically correct No answers.")]
        public string[] acceptableNoSentences;
    }

    [SerializeField]
    private WritingQuestion[] questions;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI promptTMP;
    [SerializeField] private TMP_InputField yesInputField;
    [SerializeField] private TMP_InputField noInputField;
    [SerializeField] private Button checkButton;
    [SerializeField] private TextMeshProUGUI progressCountTMP;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Animation & Feedback")]
    [SerializeField] private float timeBetweenQuestions = 1.5f;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private Color defaultInputFieldColor = Color.white;
    [SerializeField] private Image yesInputFieldBackground;
    [SerializeField] private Image noInputFieldBackground;

    private int currentQuestionIndex = 0;
    private bool canCheck = false;

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
            yesInputField.gameObject.SetActive(false);
            noInputField.gameObject.SetActive(false);
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
        yesInputField.text = "";
        noInputField.text = "";
        
        if (yesInputFieldBackground != null) yesInputFieldBackground.color = defaultInputFieldColor;
        if (noInputFieldBackground != null) noInputFieldBackground.color = defaultInputFieldColor;

        canCheck = true;
        checkButton.interactable = true;
    }

    private void OnCheckButtonClicked() {
        if (!canCheck) return;

        string yesInput = yesInputField.text.Trim();
        string noInput = noInputField.text.Trim();
        
        bool isEmpty = false;
        if (string.IsNullOrEmpty(yesInput)) {
            yesInputField.transform.DOPunchPosition(Vector3.right * 5f, 0.3f, 10, 1f);
            isEmpty = true;
        }
        if (string.IsNullOrEmpty(noInput)) {
            noInputField.transform.DOPunchPosition(Vector3.right * 5f, 0.3f, 10, 1f);
            isEmpty = true;
        }
        
        if (isEmpty) return;

        WritingQuestion currentQ = questions[currentQuestionIndex];

        bool isYesCorrect = ValidateInput(yesInput, currentQ.acceptableYesSentences);
        bool isNoCorrect = ValidateInput(noInput, currentQ.acceptableNoSentences);

        if (isYesCorrect && isNoCorrect) {
            CorrectAnswer();
        } else {
            WrongAnswer(!isYesCorrect, !isNoCorrect);
        }
    }

    private bool ValidateInput(string input, string[] acceptableSentences) {
        string normalizedInput = string.Join(" ", ExtractWords(input));

        if (acceptableSentences != null && acceptableSentences.Length > 0) {
            foreach (string correctVariation in acceptableSentences) {
                if (string.IsNullOrEmpty(correctVariation)) continue;
                
                string normalizedCorrect = string.Join(" ", ExtractWords(correctVariation));
                if (normalizedInput == normalizedCorrect) {
                    return true;
                }
            }
        }
        return false;
    }

    private List<string> ExtractWords(string sentence) {
        // Remove common terminal/separating punctuation
        string clean = sentence.Replace(",", "").Replace(".", "").Replace("?", "").Replace("!", "").Replace(";", "").Replace(":", "").ToLower();
        return clean.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
    }

    private void CorrectAnswer() {
        canCheck = false;
        checkButton.interactable = false;
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

        if (yesInputFieldBackground != null) yesInputFieldBackground.color = correctColor;
        if (noInputFieldBackground != null) noInputFieldBackground.color = correctColor;

        Invoke(nameof(NextQuestion), timeBetweenQuestions);
    }

    private void WrongAnswer(bool yesIsWrong, bool noIsWrong) {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        
        if (yesIsWrong) {
            if (yesInputFieldBackground != null) {
                yesInputFieldBackground.DOKill();
                yesInputFieldBackground.DOColor(incorrectColor, 0.2f).OnComplete(() => {
                    yesInputFieldBackground.DOColor(defaultInputFieldColor, 0.3f);
                });
            }
            yesInputField.transform.DOPunchPosition(Vector3.right * 10f, 0.3f, 10, 1f);
        }

        if (noIsWrong) {
            if (noInputFieldBackground != null) {
                noInputFieldBackground.DOKill();
                noInputFieldBackground.DOColor(incorrectColor, 0.2f).OnComplete(() => {
                    noInputFieldBackground.DOColor(defaultInputFieldColor, 0.3f);
                });
            }
            noInputField.transform.DOPunchPosition(Vector3.right * 10f, 0.3f, 10, 1f);
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
