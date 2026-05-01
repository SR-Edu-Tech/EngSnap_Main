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
    private RectTransform[] starRectTransformArray;
    [SerializeField]
    private RectTransform[] targetRectTransformArray;


    private int currentTopicCompletedIndex;


    private void Start() {
        StartCoroutine(RewardCoroutine());
    }

    private IEnumerator RewardCoroutine() {
        while(currentTopicCompletedIndex != allTopicCompletedText.Length) {
            topicCompletedTMP.transform.localScale = Vector3.zero;
            topicCompletedTMP.text = allTopicCompletedText[currentTopicCompletedIndex];
            Tween textPopUp = topicCompletedTMP.transform.DOScale(Vector3.one, timeForTopicCompletedTextAnimation).
                SetEase(Ease.OutExpo);
            RectTransform starRectTransform = starRectTransformArray[currentTopicCompletedIndex];
            RectTransform targetRectTransform = targetRectTransformArray[currentTopicCompletedIndex++];
            starRectTransform.DOAnchorPos(targetRectTransform.anchoredPosition, timeForStarAnimation).
                SetEase(Ease.OutExpo).OnComplete(() => {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                });
            yield return textPopUp.WaitForCompletion();
            yield return new WaitForSeconds(timeBetweenEachTopicCompletedText);
        }
        topicCompletedTMP.transform.localScale = Vector3.zero;
        topicCompletedTMP.text = masterText;
        topicCompletedTMP.transform.DOScale(Vector3.one, timeForTopicCompletedTextAnimation).
            SetEase(Ease.OutExpo);
    }

    protected override void OnNextButtonClicked() {
        
    }


}
