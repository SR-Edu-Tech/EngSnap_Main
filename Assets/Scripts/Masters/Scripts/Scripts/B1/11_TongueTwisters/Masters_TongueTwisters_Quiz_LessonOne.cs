using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_TongueTwisters_Quiz_LessonOne : Masters_Lesson {


    private const string SET_QUIZ = "SetQuiz";


    [System.Serializable]
    public class Quiz {

        public string question;
        public string[] fourOptions = new string[4];
        [Range(0, 3)]
        public int correctOptionIndex;

    }


    [SerializeField]
    private Quiz[] quizArray;
    [SerializeField]
    private TextMeshProUGUI questionTMP;
    [SerializeField]
    private Masters_QuizButton[] quizButtonArray;
    [SerializeField]
    private Button confirmButton;
    [SerializeField]
    private Color selectedColor;
    [SerializeField]
    private Color defaultColor;
    [SerializeField]
    private Color correctColor;
    [SerializeField]
    private Color incorrectColor;
    [SerializeField]
    private float timeBetweenEachQuizQuestion;
    [SerializeField]
    private TextMeshProUGUI quizCountTMP;
    [SerializeField]
    private float timeBetweenEachAnimation, animationSpeed;


    private int currentQuizIndex;
    private Quiz currentQuiz;
    private Masters_QuizButton currentlyPressedQuizButton;
    private bool canClickCheckButton;
    private bool canClickOptionButton;


    protected override void Awake() {
        base.Awake();

        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
    }

    protected override void Start() {
        base.Start();

        SetQuiz();

        for (int i = 0; i < quizButtonArray.Length; i++) {
            int buttonIndex = i;

            quizButtonArray[i].GetButton().onClick.AddListener(() => {
                OnQuizButtonClicked(quizButtonArray[buttonIndex], buttonIndex);
            });
        }
    }

    private void OnConfirmButtonClicked() {
        if (!canClickCheckButton) {
            return;
        }

        if (currentQuiz.correctOptionIndex == currentlyPressedQuizButton.GetButtonIndex()) {
            // Correct
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            currentlyPressedQuizButton.GetButtonImage().color = correctColor;
            quizCountTMP.text = $"{++currentQuizIndex}/6";

            canClickCheckButton = false;
            canClickOptionButton = false;

            Invoke(SET_QUIZ, timeBetweenEachQuizQuestion);
        } else {
            // Incorrect
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            currentlyPressedQuizButton.GetButton().interactable = false;
            currentlyPressedQuizButton.GetButtonImage().color = incorrectColor;
            currentlyPressedQuizButton = null;

            canClickOptionButton = true;
            canClickCheckButton = false;
        }
    }

    private void OnQuizButtonClicked(Masters_QuizButton quizButton, int buttonIndex) {
        if (!canClickOptionButton) {
            return;
        }

        if (currentlyPressedQuizButton != null) {
            //currentlyPressedQuizButton.GetButton().transition = Selectable.Transition.ColorTint;
            currentlyPressedQuizButton.GetButtonImage().color = defaultColor;
        }

        quizButton.SetButtonIndex(buttonIndex);
        currentlyPressedQuizButton = quizButton;
        canClickCheckButton = true;

        //currentlyPressedQuizButton.GetButton().transition = Selectable.Transition.None;
        currentlyPressedQuizButton.GetButtonImage().color = selectedColor;
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    private void SetQuiz() {
        if (currentQuizIndex == quizArray.Length) {
            // Over
            nextButton.interactable = true;
            NextButtonAnimation();
            return;
        }

        foreach (Masters_QuizButton quizButton in quizButtonArray) {
            //quizButton.GetButton().transition = Selectable.Transition.ColorTint;
            quizButton.GetButton().interactable = true;
            quizButton.GetButtonImage().color = defaultColor;
        }

        canClickCheckButton = true;
        canClickOptionButton = true;
        currentQuiz = quizArray[currentQuizIndex];

        StartCoroutine(AnimationCoroutine());
    }

    private IEnumerator AnimationCoroutine() {
        foreach (Masters_QuizButton quizButton in quizButtonArray) {
            RectTransform quizButtonRectTransform = quizButton.GetComponent<RectTransform>();
            quizButtonRectTransform.localScale = Vector3.zero;
        }

        questionTMP.gameObject.SetActive(false);

        yield return new WaitForSeconds(timeBetweenEachAnimation);
        questionTMP.text = currentQuiz.question;
        Masters_TextTypeWriter questionTextTypeWriter = questionTMP.GetComponent<Masters_TextTypeWriter>();
        questionTMP.gameObject.SetActive(true);
        questionTextTypeWriter.TriggerAnimation(questionTMP.text.Length);

        for (int i = 0; i < currentQuiz.fourOptions.Length; i++) {
            yield return new WaitForSeconds(timeBetweenEachAnimation);
            quizButtonArray[i].SetText(currentQuiz.fourOptions[i]);
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            RectTransform quizButtonRectTransform = quizButtonArray[i].GetComponent<RectTransform>();
            quizButtonRectTransform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutExpo);
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
