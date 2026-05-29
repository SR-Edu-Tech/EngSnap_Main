using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Masters_JustAMinuteSession_Game_LessonOne : Masters_Lesson {


    [SerializeField]
    private Masters_MatchExpression[] matchExpressionArray;
    [SerializeField]
    private Masters_MatchCard[] matchCardArray;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;
    [SerializeField]
    private Color selectedColor;
    [SerializeField]
    private Color defaultColor;


    private Masters_MatchExpression currentMatchExpression;
    private int currentIndexToMatch;
    private int correctlyMatched;
    private Image currentlySelectedButtonImage;


    protected override void Awake() {
        base.Awake();
    }

    protected override void Start() {
        base.Start();

        foreach (Masters_MatchCard matchCard in matchCardArray) {
            matchCard.GetButton().onClick.AddListener(() => {
                OnMatchCardClicked(matchCard);
            });
        }

        foreach (Masters_MatchExpression matchExpression in matchExpressionArray) {
            matchExpression.GetButton().onClick.AddListener(() => {
                OnMatchButtonClicked(matchExpression);
            });
        }
    }

    private void OnMatchCardClicked(Masters_MatchCard card) {
        if (currentIndexToMatch == 0) {
            // Haven't yet clicked an expression
            return;
        }

        if (currentIndexToMatch == card.GetCardIndex()) {
            // Correct
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            currentlySelectedButtonImage.color = defaultColor;
            card.CompleteCard();
            card.GetButton().interactable = false;
            currentMatchExpression.GetButton().interactable = false;
            currentIndexToMatch = 0;

            correctlyMatched++;
            if (correctlyMatched == 6) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            // Incorrect
        }
    }

    private void OnMatchButtonClicked(Masters_MatchExpression expression) {
        if (currentlySelectedButtonImage != null) {
            currentlySelectedButtonImage.color = defaultColor;
        }

        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        currentlySelectedButtonImage = expression.GetComponent<Image>();
        currentlySelectedButtonImage.color = selectedColor;

        currentMatchExpression = expression;
        currentIndexToMatch = expression.GetExpressionIndex();
    }


    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
    }


}
