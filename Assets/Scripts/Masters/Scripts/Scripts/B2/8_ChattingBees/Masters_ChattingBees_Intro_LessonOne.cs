using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_ChattingBees_Intro_LessonOne : Masters_Lesson {

    private const string END_LEVEL = "EndLevel";

    [Header("Chatting Bees Intro Settings")]
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

    [Header("Floating Animation")]
    [SerializeField]
    private float floatHeight = 15f;
    [SerializeField]
    private float floatDuration = 1.5f;

    protected override void Awake() {
        base.Awake();

        // Auto end level after a long time just in case
        float autoEndLevelTime = 20f;
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
        
        foreach (GameObject obj in objectsToActivateInSequence) {
            if (obj != null) {
                obj.SetActive(true);
                // Bouncy pop-up animation, followed by a continuous floating animation
                obj.transform.DOScale(Vector3.one, popUpDuration).SetEase(Ease.OutBack).OnComplete(() => {
                    if (obj != null) {
                        float startY = obj.transform.localPosition.y;
                        obj.transform.DOLocalMoveY(startY + floatHeight, floatDuration)
                            .SetEase(Ease.InOutSine)
                            .SetLoops(-1, LoopType.Yoyo);
                    }
                });
            }
            // Wait before activating the next object
            yield return new WaitForSeconds(delayBetweenObjects);
        }
    }
}
