using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Writing Lesson 1 for Unit 11 Is There a Difference?
/// Implements fill-in-the-blank typing with answer hint on failed attempts and grammar rule display upon correct answer.
/// </summary>
public class Masters_IsThereADifference_Writing_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class QuestionData {
        [TextArea]
        public string promptText;
        public string[] acceptedAnswers;
        public string displayAnswer;
        public string ruleText;
    }

    [SerializeField] protected QuestionData[] questions;

    [Header("UI Elements")]
    [SerializeField] protected TextMeshProUGUI promptTMP;
    [SerializeField] protected TMP_InputField inputField;
    [SerializeField] protected Button checkButton;
    [SerializeField] protected TextMeshProUGUI progressCountTMP;
    [SerializeField] protected Masters_LessonSO nextLessonSO;

    [Header("Hint System")]
    [SerializeField] protected GameObject hintPanel;
    [SerializeField] protected TextMeshProUGUI hintTMP;

    [Header("Animation & Feedback")]
    [SerializeField] protected float delayAfterCorrectAnswer = 3.0f;
    [SerializeField] protected Color correctColor = Color.green;
    [SerializeField] protected Color incorrectColor = Color.red;
    [SerializeField] protected Color defaultInputFieldColor = Color.white;
    [SerializeField] protected Image inputFieldBackground;

    protected int currentQuestionIndex = 0;
    protected bool canCheck = false;
    protected int failedAttempts = 0;

    protected override void Awake() {
        base.Awake();
        if (checkButton != null) {
            checkButton.onClick.AddListener(OnCheckButtonClicked);
        }
        if (inputField != null) {
            inputField.onSubmit.AddListener((_) => OnCheckButtonClicked());
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
            if (hintPanel != null) hintPanel.SetActive(false);
            
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

        QuestionData question = questions[currentQuestionIndex];
        
        if (promptTMP != null) promptTMP.text = question.promptText;
        if (inputField != null) {
            inputField.interactable = true;
            inputField.text = "";
            inputField.ActivateInputField();
        }
        
        if (inputFieldBackground != null) {
            inputFieldBackground.color = defaultInputFieldColor;
        }

        failedAttempts = 0;
        if (hintPanel != null) hintPanel.SetActive(false);

        canCheck = true;
        if (checkButton != null) checkButton.interactable = true;
    }

    protected virtual void OnCheckButtonClicked() {
        if (!canCheck || inputField == null) return;

        string input = inputField.text.Trim();
        if (string.IsNullOrEmpty(input)) {
            inputField.transform.DOPunchPosition(Vector3.right * 5f, 0.3f, 10, 1f);
            return;
        }

        QuestionData currentQ = questions[currentQuestionIndex];
        bool isCorrect = false;

        string cleanInput = input.ToLowerInvariant().Replace(".", "").Replace("!", "").Replace("?", "").Trim();

        if (currentQ.acceptedAnswers != null && currentQ.acceptedAnswers.Length > 0) {
            foreach (string ans in currentQ.acceptedAnswers) {
                if (string.IsNullOrEmpty(ans)) continue;
                if (cleanInput == ans.ToLowerInvariant().Trim()) {
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

    protected virtual void CorrectAnswer() {
        canCheck = false;
        if (checkButton != null) checkButton.interactable = false;
        if (inputField != null) inputField.interactable = false;
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

        if (inputFieldBackground != null) {
            inputFieldBackground.color = correctColor;
        }

        if (hintPanel != null) {
            hintPanel.SetActive(true);
            hintPanel.transform.localScale = Vector3.zero;
            hintPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }
        if (hintTMP != null) {
            QuestionData currentQ = questions[currentQuestionIndex];
            hintTMP.text = $"Rule: {currentQ.ruleText}";
        }

        StartCoroutine(WaitAndLoadNextQuestion());
    }

    private IEnumerator WaitAndLoadNextQuestion() {
        yield return new WaitForSeconds(delayAfterCorrectAnswer);
        NextQuestion();
    }

    protected virtual void WrongAnswer() {
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

    protected virtual void ShowHint() {
        if (hintPanel != null) hintPanel.SetActive(true);
        if (hintTMP != null && questions != null) {
            QuestionData currentQ = questions[currentQuestionIndex];
            hintTMP.text = $"Answer: {currentQ.displayAnswer}";
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
