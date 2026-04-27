using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class Masters_MeetingAndGreeting_Listening_LessonTwo : Masters_Lesson {


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
    private RectTransform dialogueSetsRectTransform;
    [SerializeField]
    private float animationSpeed;
    [SerializeField]
    private CanvasGroup fillCanvasGroup;
    [SerializeField]
    private CanvasGroup borderCanvasGroup;


    private HashSet<DialogueSet> dialogueSetHashSet = new HashSet<DialogueSet>();
    private bool doOnce;


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
        dialogueSetsRectTransform.DOAnchorPos(Vector3.zero, animationSpeed).SetEase(Ease.OutExpo).OnComplete(() => {
            PlayDialogueLineByLine(dialogueSet);
        });

        fillCanvasGroup.DOFade(1f, animationSpeed).SetEase(Ease.OutExpo);
        borderCanvasGroup.DOFade(1f, animationSpeed).SetEase(Ease.OutExpo);
    }

    private void PlayDialogueLineByLine(DialogueSet dialogueSet) {
        if (!dialogueSetHashSet.Contains(dialogueSet)) {
            // New
            dialogueSetHashSet.Add(dialogueSet);
            if (dialogueSetHashSet.Count == 3) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
        }

        Masters_AudioManager.Instance.StopVoiceOver();

        for (int i = 0; i < dialogueSetArray.Length; i++) {
            if (dialogueSet == dialogueSetArray[i]) {
                dialogueSet.gameObject.SetActive(true);
                Masters_AudioManager.Instance.PlayAudioClipsArray(dialogueSet.dialogueAudioClipArray, timeBetweenDialogues);
                continue;
            }
            dialogueSetArray[i].gameObject.SetActive(false);
        }
    }

    private void OnDialogueSetButtonClicked(RectTransform rectTransform, DialogueSet dialogueSet) {
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
