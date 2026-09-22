using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Core Controller for Unit 2 (Courtesy Call) Reading Lesson Two:
/// R02 Match — The Reason and the Courtesy.
/// Implements full interactive line-drag matching with introduction audio playback.
/// </summary>
public class Masters_CourtesyCall_Reading_LessonTwo : Masters_PolishedCommunication_Reading_LessonTwo {

    protected override void Awake() {
        topic = Masters_Topic.Reading;
        useUniqueRightCards = false;
        pairsPerSet = 5;
        InitializePuzzlesIfEmpty();
        base.Awake();
    }

    protected override void Start() {
        topic = Masters_Topic.Reading;
        useUniqueRightCards = false;
        pairsPerSet = 5;
        InitializePuzzlesIfEmpty();

        if (nextButton != null) {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }

        totalPairs = puzzles != null ? puzzles.Length : 0;
        correctCount = 0;
        currentSetIndex = 0;
        if (progressCountTMP != null) progressCountTMP.text = $"0/{totalPairs}";

        if (leftContainer != null) {
            foreach (Transform child in leftContainer) Destroy(child.gameObject);
        }
        if (rightContainer != null) {
            foreach (Transform child in rightContainer) Destroy(child.gameObject);
        }
        if (puzzleContainerRectTransform != null) {
            puzzleContainerRectTransform.DOKill();
            puzzleContainerRectTransform.localScale = Vector3.zero;
        }

#if UNITY_EDITOR
        if (narratorSpeech == null) {
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R02_ARIA.mp3");
        }
#endif

        StartCoroutine(StartWithIntroAudioRoutine());
    }

    private IEnumerator StartWithIntroAudioRoutine() {
        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            yield return new WaitForSeconds(Mathf.Max(2.5f, narratorSpeech.length));
        } else {
            yield return new WaitForSeconds(0.5f);
        }

        LoadNextSet();
    }

    public new void InitializePuzzlesIfEmpty() {
        if (puzzles != null && puzzles.Length > 0) return;

        puzzles = new MatchPuzzle[] {
            new MatchPuzzle { leftPhrase = "...for the birthday gift", rightPhrase = "Thank you so much for the birthday gift." },
            new MatchPuzzle { leftPhrase = "...for the lift home", rightPhrase = "Thank you so much for driving me home." },
            new MatchPuzzle { leftPhrase = "...for help before the maths test", rightPhrase = "Thanks so much. I really appreciate you helping me out with math test." },
            new MatchPuzzle { leftPhrase = "...you need to know the time", rightPhrase = "Excuse me, do you know what time it is?" },
            new MatchPuzzle { leftPhrase = "...a man dropped something", rightPhrase = "Excuse me sir, you dropped your wallet." }
        };
    }
}
