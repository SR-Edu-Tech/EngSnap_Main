using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Masters_TrickyThree_Intro_LessonOne : Masters_Lesson {


    private const string END_LEVEL = "EndLevel";


    [SerializeField]
    private float timeToShowNextButton;

    [Header("Kiosk Animation Sequence")]
    [SerializeField] private Image[] kioskGlows;
    [SerializeField] private TextMeshProUGUI[] kioskTexts;
    [SerializeField] private float timeBetweenKiosks = 1.5f;




    protected override void Awake() {
        base.Awake();

        float autoEndLevelTime = 20f;
        Invoke(END_LEVEL, autoEndLevelTime);

        StartCoroutine(KioskSequenceCoroutine());
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

    private IEnumerator KioskSequenceCoroutine() {
        // Wait briefly for the scene fade-in and initial sounds
        yield return new WaitForSeconds(2.0f);

        // Sequence through each kiosk
        for (int i = 0; i < kioskGlows.Length; i++) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            
            if (kioskGlows[i] != null) {
                kioskGlows[i].DOFade(1f, 0.5f);
            }
            if (kioskTexts[i] != null) {
                kioskTexts[i].DOFade(1f, 0.5f).SetEase(Ease.OutQuad);
            }

            yield return new WaitForSeconds(timeBetweenKiosks);
        }

        // Show the START button
        NextButtonAnimation();
    }

}
