using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_CorrectPronunciation_Reading_LessonOne : Masters_Lesson {


    [System.Serializable]
    public class TabSet {

        public Button button;
        public GameObject gameObject;

    }


    [SerializeField]
    private TabSet[] tabSetArray;
    [SerializeField]
    private float timeBetweenDialogues;
    [SerializeField]
    private RectTransform tabsRectTransform;
    [SerializeField]
    private float animationSpeed;
    [SerializeField]
    private TextMeshProUGUI tabCountTMP;
    [SerializeField]
    private Button[] buttonArray;


    private HashSet<Button> buttonHashSet = new HashSet<Button>();
    private bool doOnce;
    private GameObject previousGameObject;


    protected override void Awake() {
        base.Awake();

        for (int i = 0; i < tabSetArray.Length; i++) {
            TabSet dialogueSet = tabSetArray[i];
            RectTransform dialogueButtonRectTransform = tabSetArray[i].button.GetComponent<RectTransform>();
            tabSetArray[i].button.onClick.AddListener(() => {
                OnDialogueSetButtonClicked(dialogueButtonRectTransform, dialogueSet);
            });
        }

        foreach (Button button in buttonArray) {
            Button phraseButton = button;
            button.onClick.AddListener(() => {
                OnPhraseButtonClicked(phraseButton);
            });
        }
    }

    private void OnPhraseButtonClicked(Button button) {
        if (!buttonHashSet.Contains(button)) {
            buttonHashSet.Add(button);

            tabCountTMP.text = $"{buttonHashSet.Count}/12";

            if (buttonHashSet.Count == buttonArray.Length) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
        }
    }

    private void StartDialogueBoxAnimation() {
        tabsRectTransform.DOAnchorPos(Vector3.zero, animationSpeed).SetEase(Ease.OutExpo);
    }

    private void OnDialogueSetButtonClicked(RectTransform rectTransform, TabSet dialogueSet) {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        rectTransform.DOKill(true);
        rectTransform.localScale = Vector3.one;

        rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);

        if (previousGameObject) {
            previousGameObject.SetActive(false);
        }

        dialogueSet.gameObject.SetActive(true);
        previousGameObject = dialogueSet.gameObject;

        if (!doOnce) {
            doOnce = true;

            StartDialogueBoxAnimation();
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
