using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_YouAreInvited_Reading_LessonOne : Masters_Lesson {


    [SerializeField]
    private Masters_InvitationsAcceptingRefusingButton[] invitationsAcceptingRefusingButtonArray;
    [SerializeField]
    private TextMeshProUGUI progressCounterTMP;
    [SerializeField]
    private RectTransform invitationsHeadingRectTransform, acceptingHeadingRectTransform, refusingHeadingRectTransform;
    [SerializeField]
    private float animationSpeed;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;


    private HashSet<Masters_InvitationsAcceptingRefusingButton> invitationsAcceptingRefusingButtonHashSet = 
        new HashSet<Masters_InvitationsAcceptingRefusingButton>();
    private Masters_InvitationsAcceptingRefusingButton latestInvitationsAcceptingRefusingButton;


    protected override void Awake() {
        base.Awake();

        foreach (Masters_InvitationsAcceptingRefusingButton invitationAcceptingRefusingButton in invitationsAcceptingRefusingButtonArray) {
            foreach (Button button in invitationAcceptingRefusingButton.GetInvitationsAcceptingRefusingButtonArray()) {
                Masters_InvitationsAcceptingRefusingButton invitationsAcceptingsRefusingButton = invitationAcceptingRefusingButton;
                button.onClick.AddListener(() => {
                    OnInvitationsAcceptingRefusingButtonClicked(invitationsAcceptingsRefusingButton);
                });
            }
        }

        Vector2 invitationsTargetPosition = new Vector2(-450f, 50f);
        Vector2 acceptingTargetPosition = new Vector2(0f, 50f);
        Vector2 refusingTargetPosition = new Vector2(450f, 50f);
        Vector2 invitationsStartingPosition = new Vector2(-450f, 500f);
        Vector2 acceptingStartingPosition = new Vector2(0f, 500f);
        Vector2 refusingStartingPosition = new Vector2(450f, 500f);

        invitationsHeadingRectTransform.anchoredPosition = invitationsStartingPosition;
        acceptingHeadingRectTransform.anchoredPosition = acceptingStartingPosition;
        refusingHeadingRectTransform.anchoredPosition = refusingStartingPosition;

        invitationsHeadingRectTransform.DOAnchorPos(invitationsTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        acceptingHeadingRectTransform.DOAnchorPos(acceptingTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        refusingHeadingRectTransform.DOAnchorPos(refusingTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
    }

    private void OnInvitationsAcceptingRefusingButtonClicked(Masters_InvitationsAcceptingRefusingButton 
        invitationsAcceptingRefusingButton) {
        if (latestInvitationsAcceptingRefusingButton != null) {
            latestInvitationsAcceptingRefusingButton.StopCoroutine();
        }
        latestInvitationsAcceptingRefusingButton = invitationsAcceptingRefusingButton;

        invitationsAcceptingRefusingButton.PlayInvitationsAcceptingRefusingAudioClip();

        if (!invitationsAcceptingRefusingButtonHashSet.Contains(invitationsAcceptingRefusingButton)) {
            // New one
            invitationsAcceptingRefusingButtonHashSet.Add(invitationsAcceptingRefusingButton);
            progressCounterTMP.text = $"{invitationsAcceptingRefusingButtonHashSet.Count}/10";

            if (invitationsAcceptingRefusingButtonHashSet.Count == 10) {
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
