using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_ChangeVoiceAndSoundSmart_Intro_LessonOne : Masters_Lesson {

    private const string END_LEVEL = "EndLevel";

    [SerializeField]
    private float timeToShowNextButton = 5f;

    protected override void Awake() {
        base.Awake();

        // Auto end level after a long time just in case, similar to previous lessons
        float autoEndLevelTime = 20f;
        Invoke(END_LEVEL, autoEndLevelTime);

        // Show the next button after the specified time
        StartCoroutine(NextButtonAnimationCoroutine());
    }

    private void OnDisable() {
        StopAllCoroutines();
    }

    protected override void OnNextButtonClicked() {
        EndLevel();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    private void EndLevel() {
        if(topic == Masters_Topic.None) {
            Debug.Log($"Topic not set for {this.name}!");
            return;
        }
        Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }

    private IEnumerator NextButtonAnimationCoroutine() {
        yield return new WaitForSeconds(timeToShowNextButton);
        NextButtonAnimation();
    }
}
