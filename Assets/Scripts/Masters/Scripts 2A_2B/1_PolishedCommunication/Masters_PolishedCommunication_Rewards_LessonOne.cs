using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Core Game Manager for Unit 1: Polished Communication - Rewards Lesson One (R01).
/// Standalone Book 2A controller written from scratch.
/// Supports 8 topic completed announcements (Intro, Listening, Reading, Writing, Speaking, Game, Roleplay, Quiz)
/// with matching stars, voiceovers, and dynamic animations.
/// </summary>
public class Masters_PolishedCommunication_Rewards_LessonOne : Masters_Lesson {

    [Header("Rewards Data")]
    [SerializeField] protected string[] allTopicCompletedText;
    [SerializeField] protected AudioClip[] allTopicCompletedAudioClips;
    [SerializeField] protected string masterText;
    [SerializeField] protected AudioClip masterAudioClip;
    
    [Header("UI Elements")]
    [SerializeField] protected TextMeshProUGUI topicCompletedTMP;
    [SerializeField] protected Masters_PopUpEffect[] starPopEffectArray;
    
    [Header("Timing & Animation")]
    [SerializeField] protected float timeBetweenEachTopicCompletedText = 1.6f;
    [SerializeField] protected float timeForTopicCompletedTextAnimation = 0.5f;

    protected int currentTopicCompletedIndex;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Rewards;
    }

    protected override void Start() {
        base.Start();

        StartCoroutine(RewardCoroutine());
    }

    protected virtual IEnumerator RewardCoroutine() {
        if (allTopicCompletedText != null) {
            while (currentTopicCompletedIndex < allTopicCompletedText.Length) {
                if (topicCompletedTMP != null) {
                    topicCompletedTMP.transform.localScale = Vector3.zero;
                    topicCompletedTMP.text = allTopicCompletedText[currentTopicCompletedIndex];
                    topicCompletedTMP.transform.DOScale(Vector3.one, timeForTopicCompletedTextAnimation).SetEase(Ease.OutExpo);
                }

                if (starPopEffectArray != null && starPopEffectArray.Length > 0 && starPopEffectArray[currentTopicCompletedIndex % starPopEffectArray.Length] != null) {
                    starPopEffectArray[currentTopicCompletedIndex % starPopEffectArray.Length].Pop();
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                    if (allTopicCompletedAudioClips != null && currentTopicCompletedIndex < allTopicCompletedAudioClips.Length && allTopicCompletedAudioClips[currentTopicCompletedIndex] != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(allTopicCompletedAudioClips[currentTopicCompletedIndex]);
                    }
                }

                currentTopicCompletedIndex++;
                yield return new WaitForSeconds(timeBetweenEachTopicCompletedText);
            }
        }

        if (topicCompletedTMP != null) {
            topicCompletedTMP.transform.localScale = Vector3.zero;
            topicCompletedTMP.text = masterText;
            topicCompletedTMP.transform.DOScale(Vector3.one, timeForTopicCompletedTextAnimation).SetEase(Ease.OutExpo).OnComplete(() => {
                if (nextButton != null) {
                    nextButton.interactable = true;
                    NextButtonAnimation();
                }
            });
        }

        if (Masters_AudioManager.Instance != null && masterAudioClip != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(masterAudioClip);
        }
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Rewards;
        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
