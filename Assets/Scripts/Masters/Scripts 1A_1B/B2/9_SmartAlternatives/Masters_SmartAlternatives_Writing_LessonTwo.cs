using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_SmartAlternatives_Writing_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class LineRewriteQuestion {
        [Tooltip("Acceptable rewrites for this specific line. Punctuation and case are ignored.")]
        public string[] acceptableRewrites;
        
        [Tooltip("The default text shown on the checklist item before completion.")]
        public string checklistText;

        [Tooltip("The popup hint text shown after 2 failed attempts.")]
        public string hintText;
    }

    [TextArea(3, 6)]
    [SerializeField] private string fullSourceMessage;

    [SerializeField] private LineRewriteQuestion[] questions;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI sourceMessageTMP;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button checkButton;
    [SerializeField] private TextMeshProUGUI progressCountTMP;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Hint UI Elements")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TextMeshProUGUI hintTMP;

    [Header("Checklist UI Elements")]
    [SerializeField] private TextMeshProUGUI line1CheckTMP;
    [SerializeField] private TextMeshProUGUI line2CheckTMP;
    [SerializeField] private TextMeshProUGUI line3CheckTMP;
    [SerializeField] private Color checklistDefaultColor = Color.white;
    [SerializeField] private Color checklistCorrectColor = Color.green;
    [SerializeField] private Color checklistIncorrectColor = Color.red;

    [Header("Animation & Feedback")]
    [SerializeField] private float timeBetweenQuestions = 1.2f;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private Color defaultInputFieldColor = Color.white;
    [SerializeField] private Image inputFieldBackground;

    private int currentQuestionIndex = 0;
    private int failedAttempts = 0;
    private bool canCheck = false;

    protected override void Awake() {
        base.Awake();
        if (checkButton != null) checkButton.onClick.AddListener(OnCheckButtonClicked);
    }

    protected override void Start() {
        base.Start();
        if (sourceMessageTMP != null) sourceMessageTMP.text = fullSourceMessage;
        
        if (questions != null && questions.Length >= 3) {
            if (line1CheckTMP != null) line1CheckTMP.text = questions[0].checklistText;
            if (line2CheckTMP != null) line2CheckTMP.text = questions[1].checklistText;
            if (line3CheckTMP != null) line3CheckTMP.text = questions[2].checklistText;
        }

        ResetChecklistColors();
        LoadQuestion(0);
    }

    private void ResetChecklistColors() {
        if (line1CheckTMP != null) line1CheckTMP.color = checklistDefaultColor;
        if (line2CheckTMP != null) line2CheckTMP.color = checklistDefaultColor;
        if (line3CheckTMP != null) line3CheckTMP.color = checklistDefaultColor;
    }

    private TextMeshProUGUI GetCurrentCheckTMP() {
        if (currentQuestionIndex == 0) return line1CheckTMP;
        if (currentQuestionIndex == 1) return line2CheckTMP;
        if (currentQuestionIndex == 2) return line3CheckTMP;
        return null;
    }

    private void LoadQuestion(int index) {
        if (index >= questions.Length) {
            if (inputField != null) inputField.gameObject.SetActive(false);
            if (checkButton != null) checkButton.gameObject.SetActive(false);
            if (hintPanel != null) hintPanel.SetActive(false);

            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            return;
        }

        currentQuestionIndex = index;
        failedAttempts = 0;

        if (hintPanel != null) hintPanel.SetActive(false);

        if (progressCountTMP != null) progressCountTMP.text = $"{currentQuestionIndex + 1}/{questions.Length}";

        if (inputField != null) inputField.text = "";
        if (inputFieldBackground != null) inputFieldBackground.color = defaultInputFieldColor;

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

        LineRewriteQuestion currentQ = questions[currentQuestionIndex];
        string normalizedInput = string.Join(" ", ExtractWords(input));

        bool isCorrect = false;
        if (currentQ.acceptableRewrites != null) {
            foreach (string variation in currentQ.acceptableRewrites) {
                if (string.IsNullOrEmpty(variation)) continue;
                if (normalizedInput == string.Join(" ", ExtractWords(variation))) {
                    isCorrect = true;
                    break;
                }
            }
        }

        TextMeshProUGUI currentCheckTMP = GetCurrentCheckTMP();

        if (isCorrect) {
            canCheck = false;
            if (checkButton != null) checkButton.interactable = false;
            if (hintPanel != null) hintPanel.SetActive(false);
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

            if (inputFieldBackground != null) inputFieldBackground.color = correctColor;
            if (currentCheckTMP != null) currentCheckTMP.color = checklistCorrectColor;

            Invoke(nameof(NextQuestion), timeBetweenQuestions);
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            failedAttempts++;

            if (inputFieldBackground != null) {
                inputFieldBackground.DOKill();
                inputFieldBackground.DOColor(incorrectColor, 0.2f).OnComplete(() => {
                    inputFieldBackground.DOColor(defaultInputFieldColor, 0.3f);
                });
            }
            inputField.transform.DOPunchPosition(Vector3.right * 10f, 0.3f, 10, 1f);

            if (currentCheckTMP != null) {
                currentCheckTMP.transform.DOPunchPosition(Vector3.right * 5f, 0.3f, 10, 1f);
            }

            // Trigger popup hint after 2 failed attempts
            if (failedAttempts >= 2 && !string.IsNullOrEmpty(currentQ.hintText)) {
                if (hintPanel != null) {
                    hintPanel.SetActive(true);
                    hintPanel.transform.DOKill();
                    hintPanel.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f);
                }
                if (hintTMP != null) hintTMP.text = currentQ.hintText;
            }
        }
    }

    private List<string> ExtractWords(string sentence) {
        string clean = sentence.Replace(",", "").Replace(".", "").Replace("?", "").Replace("!", "").Replace(";", "").Replace(":", "").ToLower();
        return clean.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries).ToList();
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
