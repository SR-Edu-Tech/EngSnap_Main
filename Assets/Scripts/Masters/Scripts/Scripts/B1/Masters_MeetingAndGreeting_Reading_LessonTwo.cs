using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Masters_MeetingAndGreeting_Reading_LessonTwo : Masters_Lesson, IDragHandler {


    [SerializeField]
    private RectTransform mapRectTransform;
    [SerializeField]
    private Slider mapZoomSlider;
    [SerializeField]
    private float maxMapZoom;
    [SerializeField]
    private ScrollRect scrollRect;
    [SerializeField]
    private Button[] expressionButtonArray;
    [SerializeField]
    private TextMeshProUGUI expressionsDiscoveredCountTMP;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;
    [SerializeField]
    private float expressionRevealThreshold;
    [SerializeField]
    private RectTransform expressionsDiscoveredRectTransform;
    [SerializeField]
    private RectTransform mapZoomSliderRectTransform;
    [SerializeField]
    private float animationSpeed;


    private HashSet<Button> expressionButtonHashSet = new HashSet<Button>();
    private bool revealExpressions;


    protected override void Awake() {
        base.Awake();

        mapZoomSlider.onValueChanged.AddListener(OnMapZoomSliderChanged);

        foreach(Button expressionButton in expressionButtonArray) {
            expressionButton.onClick.AddListener(() => {
                OnExpressionButtonClicked(expressionButton);
            });
        }
    }

    private void OnEnable() {
        expressionsDiscoveredRectTransform.anchoredPosition = new Vector3(-500f, 0f);
        mapZoomSliderRectTransform.anchoredPosition = new Vector3(1000f, -50f);
        expressionsDiscoveredRectTransform.DOAnchorPos(new Vector3(100f, 25f), animationSpeed).SetEase(Ease.OutExpo);
        mapZoomSliderRectTransform.DOAnchorPos(new Vector3(550f, -50f), animationSpeed).SetEase(Ease.OutExpo);
    }

    private void OnExpressionButtonClicked(Button button) {
        if (!expressionButtonHashSet.Contains(button)) {
            expressionButtonHashSet.Add(button);

            int numberOfUniqueButtonsClicked = expressionButtonHashSet.Count;
            int totalNumberOfButtons = expressionButtonArray.Length;

            expressionsDiscoveredCountTMP.text = $"{numberOfUniqueButtonsClicked}/{totalNumberOfButtons}";

            if (numberOfUniqueButtonsClicked == totalNumberOfButtons) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
        }
    }

    private void ShowExpressions() {
        foreach(Button expressionButton in expressionButtonArray) {
            expressionButton.gameObject.SetActive(true);
        }
    }

    private void HideExpressions() {
        foreach(Button expressionButton in expressionButtonArray) {
            expressionButton.gameObject.SetActive(false);
        }
    }

    private void OnMapZoomSliderChanged(float value) {
        if(value > expressionRevealThreshold) {
            ShowExpressions();
        } else {
            HideExpressions();
        }

        float scale = Mathf.Lerp(1.15f, maxMapZoom, value);
        mapRectTransform.localScale = new Vector3(scale, scale, scale);

        // Force ScrollRect to update bounds
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(mapRectTransform);

        // Reassign to trigger internal recalculation
        scrollRect.content = null;
        scrollRect.content = mapRectTransform;
    }

    public void OnDrag(PointerEventData eventData) {
        mapRectTransform.anchoredPosition += eventData.delta;
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
    }


}
