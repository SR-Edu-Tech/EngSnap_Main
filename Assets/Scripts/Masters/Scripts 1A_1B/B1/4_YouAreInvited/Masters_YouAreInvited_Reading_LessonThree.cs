using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_YouAreInvited_Reading_LessonThree : Masters_Lesson {


    [SerializeField]
    private RectTransform topicTabsRectTransform;
    [SerializeField]
    private CanvasGroup fillCanvasGroup;
    [SerializeField]
    private float animationSpeed;
    [SerializeField]
    private Button excusesForBeingLateButton;
    [SerializeField]
    private Button sayingSorryButton;
    [SerializeField]
    private GameObject excusesForBeingLateContent;
    [SerializeField]
    private GameObject sayingSorryContent;
    [SerializeField]
    private Button[] clickToPlayAudioButtonArray;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private ScrollRect scrollRect;
    [SerializeField]
    private RectTransform excusesForBeingLateContentRectTransform, sayingSorryContentRectTransform;
    [SerializeField]
    private float excusesContentBottom, sorryContentBottom;


    private bool doOnce;
    private HashSet<Button> clickToPlayAudioButtonHashSet = new HashSet<Button>();


    protected override void Awake() {
        base.Awake();

        excusesForBeingLateButton.onClick.AddListener(OnExcusesForBeingLateButtonClicked);
        sayingSorryButton.onClick.AddListener(OnSayingSorryButtonClicked);

        excusesForBeingLateContent.SetActive(false);
        sayingSorryContent.SetActive(false);
        topicTabsRectTransform.anchoredPosition = new Vector2(0f, -225f);
        fillCanvasGroup.alpha = 0f;

        foreach(Button button in clickToPlayAudioButtonArray) {
            button.onClick.AddListener(() => {
                Button clickToPlayButton = button;
                OnClickToPlayAudioButtonClicked(clickToPlayButton);
            });
        }
    }

    private void OnClickToPlayAudioButtonClicked(Button button) {
        if (!clickToPlayAudioButtonHashSet.Contains(button)) {
            // First time clicked
            clickToPlayAudioButtonHashSet.Add(button);
            progressTMP.text = $"{clickToPlayAudioButtonHashSet.Count}/{clickToPlayAudioButtonArray.Length}";
        }

        if(clickToPlayAudioButtonHashSet.Count == clickToPlayAudioButtonArray.Length) {
            // All clicked at least once
            nextButton.interactable = true;
            NextButtonAnimation();
        }
    }

    private void OnExcusesForBeingLateButtonClicked() {
        if (!doOnce) {
            doOnce = true;
            StartingAnimation();
        }

        scrollRect.content = excusesForBeingLateContentRectTransform;
        excusesForBeingLateContentRectTransform.offsetMin = new Vector2(0f, excusesContentBottom);
        excusesForBeingLateContentRectTransform.offsetMax = new Vector2(0f, 0f);
        sayingSorryContent.SetActive(false);
        excusesForBeingLateContent.SetActive(true);
    }

    private void OnSayingSorryButtonClicked() {
        if (!doOnce) {
            doOnce = true;
            StartingAnimation();
        }

        scrollRect.content = sayingSorryContentRectTransform;
        sayingSorryContentRectTransform.offsetMin = new Vector2(0f, sorryContentBottom);
        sayingSorryContentRectTransform.offsetMax = new Vector2(0f, 0f);
        excusesForBeingLateContent.SetActive(false);
        sayingSorryContent.SetActive(true);
    }

    private void StartingAnimation() {
        Vector2 topicTabsTargetPosition = new Vector2(0f, 50f);
        topicTabsRectTransform.DOAnchorPos(topicTabsTargetPosition, animationSpeed).SetEase(Ease.OutExpo).OnComplete(() => {
            fillCanvasGroup.DOFade(1f, animationSpeed);
        });
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
