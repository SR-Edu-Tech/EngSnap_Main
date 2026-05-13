using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Masters_MeetingAndGreeting_Rewards_LessonOne : Masters_Lesson {


    [SerializeField]
    private string[] allTopicCompletedText;
    [SerializeField]
    private string masterText;
    [SerializeField]
    private TextMeshProUGUI topicCompletedTMP;
    [SerializeField]
    private float timeBetweenEachTopicCompletedText;
    [SerializeField]
    private float timeForStarAnimation;
    [SerializeField]
    private float timeForTopicCompletedTextAnimation;
    [SerializeField]
    private Masters_PopUpEffect[] starPopEffectArray;


    private int currentTopicCompletedIndex;


    protected override void Start() {
        base.Start();

        StartCoroutine(RewardCoroutine());
    }

    private IEnumerator RewardCoroutine() {
        while(currentTopicCompletedIndex != allTopicCompletedText.Length) {
            topicCompletedTMP.transform.localScale = Vector3.zero;
            topicCompletedTMP.text = allTopicCompletedText[currentTopicCompletedIndex];
            Tween textPopUp = topicCompletedTMP.transform.DOScale(Vector3.one, timeForTopicCompletedTextAnimation).
                SetEase(Ease.OutExpo);
            starPopEffectArray[currentTopicCompletedIndex++].Pop();
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            yield return textPopUp.WaitForCompletion();
            yield return new WaitForSeconds(timeBetweenEachTopicCompletedText);
        }
        topicCompletedTMP.transform.localScale = Vector3.zero;
        topicCompletedTMP.text = masterText;
        topicCompletedTMP.transform.DOScale(Vector3.one, timeForTopicCompletedTextAnimation).
            SetEase(Ease.OutExpo).OnComplete(() => {
                nextButton.interactable = true;
                NextButtonAnimation();
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
