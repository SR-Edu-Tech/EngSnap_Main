using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_SelfIntroduction_Reading_LessonTwo : Masters_Lesson {


    [System.Serializable]
    public enum ConversationBy {
        Left,
        Right
    }


    [System.Serializable]
    public class Conversation {

        public string conversationText;
        public ConversationBy conversationBy;
        public AudioClip conversationAudioClip;

    }


    [SerializeField]
    private TextMeshProUGUI progressCounterTMP;
    [SerializeField]
    private float animationSpeed;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;
    [SerializeField]
    private Conversation[] conversationArray;
    [SerializeField]
    private TextMeshProUGUI leftTMP, rightTMP;
    [SerializeField]
    private GameObject leftCloudGameObject, rightCloudGameObject;
    [SerializeField]
    private float timeBetweenEachConversation;
    [SerializeField]
    private GameObject npcGameObject;
    [SerializeField]
    private Button continueDialogueButton;


    private int conversationIndex;


    protected override void Awake() {
        base.Awake();

        continueDialogueButton.onClick.AddListener(OnContinueDialogueButtonClicked);
    }

    private void OnContinueDialogueButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        StopAllCoroutines();
        leftCloudGameObject.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo);
        rightCloudGameObject.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo).OnComplete(() => {
            leftCloudGameObject.SetActive(false);
            rightCloudGameObject.SetActive(false);
            DialogueSequence();
        });
    }

    protected override void Start() {
        base.Start();

        leftCloudGameObject.SetActive(false);
        rightCloudGameObject.SetActive(false);

        StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(DialogueSequence));
    }

    private void DialogueSequence() {
        if (conversationIndex == conversationArray.Length) {
            // Over

            continueDialogueButton.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo);
            npcGameObject.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo).OnComplete(() => {
                npcGameObject.SetActive(false);
                npcGameObject.transform.localScale = Vector2.one;
                continueDialogueButton.gameObject.SetActive(false);
                continueDialogueButton.transform.localScale = Vector2.one;
            });

            nextButton.interactable = true;
            NextButtonAnimation();
            return;
        }

        Conversation conversation = conversationArray[conversationIndex++];
        progressCounterTMP.text = $"{conversationIndex}/18";

        if(conversation.conversationBy == ConversationBy.Left) {
            // Left
            leftTMP.text = conversation.conversationText;
            leftCloudGameObject.SetActive(true);
        } else {
            // Right
            rightTMP.text = conversation.conversationText;
            rightCloudGameObject.SetActive(true);
        }
        Masters_AudioManager.Instance.PlayVoiceOver(conversation.conversationAudioClip);
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
    }


}
