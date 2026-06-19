using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Game Manager for Tricky Three - Quiz Lesson 1.
/// Simulates a standard 10-question multiple-choice quiz format.
/// </summary>
public class Masters_TrickyThree_Quiz_LessonOne : Masters_Lesson {

    private const string SET_QUIZ = "SetQuiz";

    [System.Serializable]
    public class Quiz {
        [Tooltip("The question text displayed at the top.")]
        public string question;
        [Tooltip("The multiple choice options displayed on the buttons.")]
        public string[] options;
        [Tooltip("The index (0-based) of the correct option.")]
        public int correctOptionIndex;
    }

    [Header("Quiz Data")]
    [SerializeField] private Quiz[] quizArray;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI questionTMP;
    [SerializeField] private Masters_QuizButton[] quizButtonArray;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TextMeshProUGUI quizCountTMP;
    
    [Header("Colors")]
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color defaultColor;
    [SerializeField] private Color correctColor;
    [SerializeField] private Color incorrectColor;
    
    [Header("Timing & Animation")]
    [SerializeField] private float timeBetweenEachQuizQuestion;
    [SerializeField] private float timeBetweenEachAnimation;
    [SerializeField] private float animationSpeed;

    private int currentQuizIndex;
    private Quiz currentQuiz;
    private Masters_QuizButton currentlyPressedQuizButton;
    private bool canClickCheckButton;
    private bool canClickOptionButton;

    protected override void Awake() {
        base.Awake();

        if (confirmButton != null) {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
    }

    protected override void Start() {
        base.Start();

        SetQuiz();

        // Bind quiz buttons
        for (int i = 0; i < quizButtonArray.Length; i++) {
            int buttonIndex = i;
            if (quizButtonArray[i] != null && quizButtonArray[i].GetButton() != null) {
                quizButtonArray[i].GetButton().onClick.AddListener(() => {
                    OnQuizButtonClicked(quizButtonArray[buttonIndex], buttonIndex);
                });
            }
        }
    }

    /// <summary>
    /// Validates the player's selected option when they click the Check/Confirm button.
    /// </summary>
    private void OnConfirmButtonClicked() {
        if (!canClickCheckButton || currentlyPressedQuizButton == null) {
            return;
        }

        if (currentQuiz.correctOptionIndex == currentlyPressedQuizButton.GetButtonIndex()) {
            // Correct Answer
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            currentlyPressedQuizButton.GetButtonImage().color = correctColor;
            
            if (quizCountTMP != null) {
                quizCountTMP.text = $"{++currentQuizIndex}/{quizArray.Length}";
            } else {
                currentQuizIndex++;
            }

            canClickCheckButton = false;
            canClickOptionButton = false;

            Invoke(SET_QUIZ, timeBetweenEachQuizQuestion);
        } else {
            // Incorrect Answer
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            currentlyPressedQuizButton.GetButton().interactable = false;
            currentlyPressedQuizButton.GetButtonImage().color = incorrectColor;
            currentlyPressedQuizButton = null;

            canClickOptionButton = true;
            canClickCheckButton = false;
        }
    }

    /// <summary>
    /// Triggered when the player taps one of the multiple choice buttons.
    /// Marks it as selected, changing its color, but does not confirm the answer yet.
    /// </summary>
    private void OnQuizButtonClicked(Masters_QuizButton quizButton, int buttonIndex) {
        if (!canClickOptionButton) {
            return;
        }

        // Reset previous selection
        if (currentlyPressedQuizButton != null) {
            currentlyPressedQuizButton.GetButtonImage().color = defaultColor;
        }

        canClickCheckButton = true;

        quizButton.SetButtonIndex(buttonIndex);
        currentlyPressedQuizButton = quizButton;

        currentlyPressedQuizButton.GetButtonImage().color = selectedColor;
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    /// <summary>
    /// Prepares the UI and state for the next quiz question. Ends the lesson if all questions are answered.
    /// </summary>
    private void SetQuiz() {
        if (currentQuizIndex >= quizArray.Length) {
            // Lesson Over
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            return;
        }

        // Reset Buttons
        foreach (Masters_QuizButton quizButton in quizButtonArray) {
            if (quizButton != null && quizButton.GetButton() != null) {
                quizButton.GetButton().interactable = true;
                quizButton.GetButtonImage().color = defaultColor;
            }
        }

        canClickCheckButton = false;
        canClickOptionButton = true;
        currentQuiz = quizArray[currentQuizIndex];

        StartCoroutine(AnimationCoroutine());
    }

    /// <summary>
    /// Smoothly pops in the question text via TypeWriter effect, followed by popping in each button.
    /// </summary>
    private IEnumerator AnimationCoroutine() {
        // Hide all elements initially
        foreach (Masters_QuizButton quizButton in quizButtonArray) {
            if (quizButton != null) {
                quizButton.gameObject.SetActive(false);
                RectTransform quizButtonRectTransform = quizButton.GetComponent<RectTransform>();
                if (quizButtonRectTransform != null) quizButtonRectTransform.localScale = Vector3.zero;
            }
        }

        if (questionTMP != null) questionTMP.gameObject.SetActive(false);

        yield return new WaitForSeconds(timeBetweenEachAnimation);
        
        if (questionTMP != null) {
            questionTMP.text = currentQuiz.question;
            questionTMP.gameObject.SetActive(true);
            Masters_TextTypeWriter questionTextTypeWriter = questionTMP.GetComponent<Masters_TextTypeWriter>();
            if (questionTextTypeWriter != null) {
                questionTextTypeWriter.TriggerAnimation(questionTMP.text.Length);
            }
        }

        // Reveal buttons one by one
        for (int i = 0; i < currentQuiz.options.Length; i++) {
            if (i >= quizButtonArray.Length || quizButtonArray[i] == null) break;
            
            quizButtonArray[i].gameObject.SetActive(true);
            yield return new WaitForSeconds(timeBetweenEachAnimation);
            
            quizButtonArray[i].SetText(currentQuiz.options[i]);
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            
            RectTransform quizButtonRectTransform = quizButtonArray[i].GetComponent<RectTransform>();
            if (quizButtonRectTransform != null) {
                quizButtonRectTransform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutExpo);
            }
        }
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) {
            Debug.Log($"Topic not set for {this.name}!");
            return;
        }
        Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}
