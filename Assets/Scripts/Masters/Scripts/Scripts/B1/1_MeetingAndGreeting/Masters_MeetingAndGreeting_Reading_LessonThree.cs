using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_MeetingAndGreeting_Reading_LessonThree : Masters_Lesson {


    private const string SET_NEXT_QUESTION = "SetNextQuestion";


    [System.Serializable]
    public struct MultipleChoiceQuestion {

        public string statement;
        public string question;
        public string[] options;
        [Range(0, 3)]
        public int validOption;

    }


    [SerializeField]
    private MultipleChoiceQuestion[] multipleChoiceQuestionArray;
    [SerializeField]
    private TextMeshProUGUI statementTMP;
    [SerializeField]
    private TextMeshProUGUI questionTMP;
    [SerializeField]
    private Button[] optionButtonArray;
    [SerializeField]
    private Color selectedColor;
    [SerializeField]
    private Color defaultColor;
    [SerializeField]
    private Color correctColor;
    [SerializeField]
    private Color incorrectColor;
    [SerializeField]
    private float timeToLoadNextQuestion;
    [SerializeField]
    private Button confirmButton;
    [SerializeField]
    private float animationSpeed, timeBetweenEachAnimation;
    [SerializeField]
    private TextMeshProUGUI progressCountTMP;


    private MultipleChoiceQuestion currentQuestion;
    private int currentQuestionIndex;
    private bool canSelectButton;
    private Button currentSelectedButton;
    private int currentSelectedIndex;
    private bool doOnce;


    protected override void Awake() {
        base.Awake();

        for(int i = 0; i < optionButtonArray.Length; i++) {
            int index = i;
            Button button = optionButtonArray[index];

            button.onClick.AddListener(() => {
                OnOptionButtonClicked(button, index);
            });
        }

        confirmButton.onClick.AddListener(OnConfirmButtonClicked);

        SetNextQuestion();
    }

    private void OnConfirmButtonClicked() {
        if (currentSelectedIndex == currentQuestion.validOption) {
            // Correct
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

            progressCountTMP.text = $"{currentQuestionIndex}/4";

            Image buttonImage = currentSelectedButton.GetComponent<Image>();
            buttonImage.color = correctColor;

            Invoke(SET_NEXT_QUESTION, timeToLoadNextQuestion);

            canSelectButton = false;
        } else {
            // Wrong
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);

            Image buttonImage = currentSelectedButton.GetComponent<Image>();
            buttonImage.color = incorrectColor;

            currentSelectedButton.interactable = false;
            currentSelectedButton = null;
        }
    }

    private void OnOptionButtonClicked(Button button, int index) {
        if (!canSelectButton) {
            return;
        }

        if (currentSelectedButton) {
            Image image = currentSelectedButton.GetComponent<Image>();
            image.color = defaultColor;
        }

        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        currentSelectedButton = button;
        currentSelectedIndex = index;

        Image buttonImage = button.GetComponent<Image>();
        buttonImage.color = selectedColor;
    }

    private void SetNextQuestion() {
        if(currentQuestionIndex + 1 > multipleChoiceQuestionArray.Length) {
            // Level over
            nextButton.interactable = true;
            NextButtonAnimation();
            canSelectButton = false;
            return;
        }

        foreach(Button button in optionButtonArray) {
            Image buttonImage = button.GetComponent<Image>();
            buttonImage.color = defaultColor;
            button.interactable = true;
        }

        currentQuestion = multipleChoiceQuestionArray[currentQuestionIndex++];

        statementTMP.text = currentQuestion.statement;
        questionTMP.text = currentQuestion.question;

        for (int i = 0; i < optionButtonArray.Length; i++) {
            TextMeshProUGUI optionButtonTMP = optionButtonArray[i].GetComponentInChildren<TextMeshProUGUI>();
            optionButtonTMP.text = currentQuestion.options[i];
        }

        canSelectButton = true;

        if (!doOnce) {
            doOnce = true;
            return;
        }

        Masters_TextTypeWriter statementTextTypeWriter = statementTMP.GetComponent<Masters_TextTypeWriter>();
        Masters_TextTypeWriter questionTextTypeWriter = questionTMP.GetComponent<Masters_TextTypeWriter>();

        statementTextTypeWriter.TriggerAnimation(currentQuestion.statement.Length);
        questionTextTypeWriter.TriggerAnimation(currentQuestion.question.Length);

        StartCoroutine(OptionButtonAnimations());
    }

    private IEnumerator OptionButtonAnimations() {
        foreach (Button button in optionButtonArray) {
            RectTransform buttonRectTransform = button.GetComponent<RectTransform>();
            TextMeshProUGUI optionTMP = button.GetComponentInChildren<TextMeshProUGUI>();
            optionTMP.maxVisibleCharacters = 0;
            buttonRectTransform.localScale = Vector3.zero;
        }

        foreach (Button button in optionButtonArray) {
            RectTransform buttonRectTransform = button.GetComponent<RectTransform>();
            Masters_TextTypeWriter optionTextTypeWriter = button.GetComponent<Masters_TextTypeWriter>();
            TextMeshProUGUI optionTMP = button.GetComponentInChildren<TextMeshProUGUI>();
            optionTextTypeWriter.TriggerAnimation(optionTMP.text.Length);
            yield return new WaitForSeconds(timeBetweenEachAnimation);
            buttonRectTransform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutExpo);
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
