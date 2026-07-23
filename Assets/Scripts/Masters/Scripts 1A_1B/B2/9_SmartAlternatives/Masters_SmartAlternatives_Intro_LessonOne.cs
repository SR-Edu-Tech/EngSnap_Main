using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_SmartAlternatives_Intro_LessonOne : Masters_Lesson {

    private const string END_LEVEL = "EndLevel";

    [Header("Smart Alternatives Intro Settings")]
    [SerializeField]
    private float timeToShowNextButton = 5f;

    [Header("Sequential Object Activation")]
    [SerializeField]
    private GameObject[] objectsToActivateInSequence;
    
    [SerializeField]
    private float initialActivationDelay = 1f;
    
    [SerializeField]
    private float delayBetweenObjects = 0.5f;

    [SerializeField]
    private float popUpDuration = 0.4f;

    [Header("Audio")]
    [SerializeField]
    private AudioClip introVoiceOver;

    protected override void Awake() {
        base.Awake();

        // Auto end level after a long time just in case
        float autoEndLevelTime = 25f;
        Invoke(END_LEVEL, autoEndLevelTime);

        // Show the next button after the specified time
        StartCoroutine(NextButtonAnimationCoroutine());
        
        // Hide all objects immediately by setting scale to zero before the sequence starts
        if (objectsToActivateInSequence != null) {
            foreach(GameObject obj in objectsToActivateInSequence) {
                if (obj != null) {
                    obj.transform.localScale = Vector3.zero;
                    obj.SetActive(false);
                }
            }
        }

        // Start the sequential object activation
        if (objectsToActivateInSequence != null && objectsToActivateInSequence.Length > 0) {
            StartCoroutine(ActivateObjectsSequentially());
        }

        if (introVoiceOver != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(introVoiceOver);
        }
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

    private IEnumerator ActivateObjectsSequentially() {
        // Wait for the initial delay before starting the sequence
        yield return new WaitForSeconds(initialActivationDelay);
        
        for (int i = 0; i < objectsToActivateInSequence.Length; i++) {
            GameObject obj = objectsToActivateInSequence[i];
            if (obj != null) {
                obj.SetActive(true);

                // Bouncy pop-up animation. We removed the floating animation because it fights with the Grid Layout Group.
                obj.transform.DOScale(Vector3.one, popUpDuration).SetEase(Ease.OutBack);
            }
            // Wait before activating the next object
            yield return new WaitForSeconds(delayBetweenObjects);
        }
    }
}
