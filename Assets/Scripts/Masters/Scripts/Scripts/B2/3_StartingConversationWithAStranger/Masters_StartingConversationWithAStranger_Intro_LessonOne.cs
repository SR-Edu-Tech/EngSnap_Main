using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Masters_StartingConversationWithAStranger_Intro_LessonOne : Masters_Lesson {


    private const string END_LEVEL = "EndLevel";


    [SerializeField]
    private float timeToShowNextButton;


    protected override void Awake() {
        base.Awake();

        float autoEndLevelTime = 20f;
        Invoke(END_LEVEL, autoEndLevelTime);

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

