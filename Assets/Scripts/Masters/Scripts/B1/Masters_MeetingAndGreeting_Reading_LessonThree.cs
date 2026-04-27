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
    private ColorBlock defaultColorBlock;
    [SerializeField]
    private ColorBlock correctColorBlock;
    [SerializeField]
    private ColorBlock wrongColorBlock;
    [SerializeField]
    private float timeToLoadNextQuestion;


    private MultipleChoiceQuestion currentQuestion;
    private int currentQuestionIndex;
    private bool canSelectButton;


    protected override void Awake() {
        base.Awake();

        for(int i = 0; i < optionButtonArray.Length; i++) {
            int index = i;

            optionButtonArray[index].onClick.AddListener(() => {
                OnOptionButtonClicked(index);
            });
        }

        SetNextQuestion();
    }

    private void OnOptionButtonClicked(int index) {
        if (!canSelectButton) {
            return;
        }

        if (currentQuestion.validOption == index) {
            // Correct
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            optionButtonArray[index].colors = correctColorBlock;
            Invoke(SET_NEXT_QUESTION, timeToLoadNextQuestion);

            canSelectButton = false;
        } else {
            // Wrong
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            optionButtonArray[index].colors = wrongColorBlock;
        }
    }

    private void SetNextQuestion() {
        if(currentQuestionIndex + 1 > multipleChoiceQuestionArray.Length) {
            // Level over
            nextButton.interactable = true;
            canSelectButton = false;
            return;
        }

        foreach(Button button in optionButtonArray) {
            button.colors = defaultColorBlock;
        }

        currentQuestion = multipleChoiceQuestionArray[currentQuestionIndex++];

        statementTMP.text = currentQuestion.statement;
        questionTMP.text = currentQuestion.question;

        for(int i = 0; i < optionButtonArray.Length; i++) {
            TextMeshProUGUI optionButtonTMP = optionButtonArray[i].GetComponentInChildren<TextMeshProUGUI>();
            optionButtonTMP.text = currentQuestion.options[i];
        }

        canSelectButton = true;
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
