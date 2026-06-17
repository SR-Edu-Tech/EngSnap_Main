using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_SelfIntroduction_Listening_LessonTwo : Masters_Lesson {


    [System.Serializable]
    public class DialogueSet {

        public Button button;
        public GameObject gameObject;
        public AudioClip[] dialogueAudioClipArray;

    }


    [SerializeField]
    private DialogueSet[] dialogueSetArray;
    [SerializeField]
    private float timeBetweenDialogues;
    [SerializeField]
    private RectTransform topicTabsRectTransform;
    [SerializeField]
    private float animationSpeed;
    [SerializeField]
    private CanvasGroup fillCanvasGroup;
    [SerializeField]
    private CanvasGroup borderCanvasGroup;
    [SerializeField]
    private TextMeshProUGUI dialogueCountTMP;


    private HashSet<DialogueSet> dialogueSetHashSet = new HashSet<DialogueSet>();
    private bool doOnce;
    private Coroutine highlightCoroutine;


    protected override void Awake() {
        base.Awake();

        for (int i = 0; i < dialogueSetArray.Length; i++) {
            DialogueSet dialogueSet = dialogueSetArray[i];
            RectTransform dialogueButtonRectTransform = dialogueSetArray[i].button.GetComponent<RectTransform>();
            dialogueSetArray[i].button.onClick.AddListener(() => {
                OnDialogueSetButtonClicked(dialogueButtonRectTransform, dialogueSet);
            });
        }
    }

    private void StartDialogueBoxAnimation(DialogueSet dialogueSet) {
        topicTabsRectTransform.DOAnchorPos(Vector3.zero, animationSpeed).SetEase(Ease.OutExpo).OnComplete(() => {
            PlayDialogueLineByLine(dialogueSet);
        });

        fillCanvasGroup.DOFade(1f, animationSpeed);
        borderCanvasGroup.DOFade(1f, animationSpeed);
    }

    private void PlayDialogueLineByLine(DialogueSet dialogueSet) {
        if (!dialogueSetHashSet.Contains(dialogueSet)) {
            // New
            dialogueSetHashSet.Add(dialogueSet);
            dialogueCountTMP.text = $"{dialogueSetHashSet.Count}/3";
            if (dialogueSetHashSet.Count == 3) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
        }

        Masters_AudioManager.Instance.StopVoiceOver();

        if (highlightCoroutine != null) {
            StopCoroutine(highlightCoroutine);
            highlightCoroutine = null;
        }

        for (int i = 0; i < dialogueSetArray.Length; i++) {
            if (dialogueSet == dialogueSetArray[i]) {
                dialogueSet.gameObject.SetActive(true);
                Masters_AudioManager.Instance.PlayAudioClipsArray(dialogueSet.dialogueAudioClipArray, timeBetweenDialogues);
                
                // Start coroutine to highlight the next button
                if (i + 1 < dialogueSetArray.Length) {
                    highlightCoroutine = StartCoroutine(HighlightNextButton(dialogueSet.dialogueAudioClipArray, dialogueSetArray[i + 1].button));
                }

                continue;
            }
            dialogueSetArray[i].gameObject.SetActive(false);
        }
    }

    private IEnumerator HighlightNextButton(AudioClip[] audioClipArray, Button nextButton) {
        float totalWaitTime = 0f;
        if (audioClipArray != null) {
            for (int j = 0; j < audioClipArray.Length; j++) {
                if (audioClipArray[j] != null) {
                    totalWaitTime += audioClipArray[j].length;
                }
            }
            totalWaitTime += timeBetweenDialogues * Mathf.Max(0, audioClipArray.Length - 1);
        }
        
        yield return new WaitForSeconds(totalWaitTime);
        
        if (nextButton != null) {
            RectTransform nextBtnRect = nextButton.GetComponent<RectTransform>();
            // Subtle but noticeable expanding and contracting
            nextBtnRect.DOScale(1.05f, 0.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }

    private void OnDialogueSetButtonClicked(RectTransform rectTransform, DialogueSet dialogueSet) {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        // Stop any looping animation on all buttons and reset scale
        foreach (DialogueSet ds in dialogueSetArray) {
            if (ds.button != null) {
                RectTransform rt = ds.button.GetComponent<RectTransform>();
                rt.DOKill(true);
                rt.localScale = Vector3.one;
            }
        }

        rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);

        if (!doOnce) {
            doOnce = true;

            StartDialogueBoxAnimation(dialogueSet);
            return;
        }

        PlayDialogueLineByLine(dialogueSet);
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
