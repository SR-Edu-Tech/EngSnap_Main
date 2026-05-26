using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_IDoAndIMake_Reading_LessonOne : Masters_Lesson {


    [SerializeField]
    private RectTransform topicTabsRectTransform;
    [SerializeField]
    private CanvasGroup fillCanvasGroup;
    [SerializeField]
    private float animationSpeed;
    [SerializeField]
    private Button doStatementsButton;
    [SerializeField]
    private Button makeStatementsButton;
    [SerializeField]
    private GameObject doStatementsContent;
    [SerializeField]
    private GameObject makeStatementsContent;
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

        doStatementsButton.onClick.AddListener(OnExcusesForBeingLateButtonClicked);
        makeStatementsButton.onClick.AddListener(OnSayingSorryButtonClicked);

        doStatementsContent.SetActive(false);
        makeStatementsContent.SetActive(false);
        topicTabsRectTransform.anchoredPosition = new Vector2(0f, -225f);
        fillCanvasGroup.alpha = 0f;

        foreach (Button button in clickToPlayAudioButtonArray) {
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

        if (clickToPlayAudioButtonHashSet.Count == clickToPlayAudioButtonArray.Length) {
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
        makeStatementsContent.SetActive(false);
        doStatementsContent.SetActive(true);
    }

    private void OnSayingSorryButtonClicked() {
        if (!doOnce) {
            doOnce = true;
            StartingAnimation();
        }

        scrollRect.content = sayingSorryContentRectTransform;
        sayingSorryContentRectTransform.offsetMin = new Vector2(0f, sorryContentBottom);
        sayingSorryContentRectTransform.offsetMax = new Vector2(0f, 0f);
        doStatementsContent.SetActive(false);
        makeStatementsContent.SetActive(true);
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
