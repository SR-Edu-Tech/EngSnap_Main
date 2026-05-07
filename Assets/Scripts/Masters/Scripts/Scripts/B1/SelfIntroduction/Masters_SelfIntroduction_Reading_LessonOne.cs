using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_SelfIntroduction_Reading_LessonOne : Masters_Lesson {


    [SerializeField]
    private Masters_QuestionAndAnswerButton[] questionAndAnswerButtonArray;
    [SerializeField]
    private TextMeshProUGUI progressCounterTMP;
    [SerializeField]
    private RectTransform questionsHeadingRectTransform, answersHeadingRectTransform;
    [SerializeField]
    private float animationSpeed;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;


    private HashSet<Masters_QuestionAndAnswerButton> questionAndAnswerButtonHashSet = new HashSet<Masters_QuestionAndAnswerButton>();
    private Masters_QuestionAndAnswerButton latestQuestionAndAnswerButton;


    protected override void Awake() {
        base.Awake();

        foreach(Masters_QuestionAndAnswerButton questionAndAnswerButton in questionAndAnswerButtonArray) {
            foreach(Button button in questionAndAnswerButton.GetQuestionAndAnswerButtonArray()) {
                Masters_QuestionAndAnswerButton questionAndAnswer = questionAndAnswerButton;
                button.onClick.AddListener(() => {
                    OnQuestionAndAnswerButtonClicked(questionAndAnswer);
                });
            }
        }

        Vector2 questionTargetPosition = new Vector2(-322.5f, 50f);
        Vector2 answerTargetPosition = new Vector2(322.5f, 50f);
        Vector2 questionStartingPosition = new Vector2(-322.5f, 500f);
        Vector2 answerStartingPosition = new Vector2(322.5f, 500f);

        questionsHeadingRectTransform.anchoredPosition = questionStartingPosition;
        answersHeadingRectTransform.anchoredPosition = answerStartingPosition;

        questionsHeadingRectTransform.DOAnchorPos(questionTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        answersHeadingRectTransform.DOAnchorPos(answerTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
    }

    private void OnQuestionAndAnswerButtonClicked(Masters_QuestionAndAnswerButton questionAndAnswerButton) {
        if(latestQuestionAndAnswerButton != null) {
            latestQuestionAndAnswerButton.StopCoroutine();
        }
        latestQuestionAndAnswerButton = questionAndAnswerButton;

        questionAndAnswerButton.PlayQuestionAndAnswerAudioClip();

        if (!questionAndAnswerButtonHashSet.Contains(questionAndAnswerButton)) {
            // New one
            questionAndAnswerButtonHashSet.Add(questionAndAnswerButton);
            progressCounterTMP.text = $"{questionAndAnswerButtonHashSet.Count}/12";

            if(questionAndAnswerButtonHashSet.Count == 12) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
        }
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
    }


}
