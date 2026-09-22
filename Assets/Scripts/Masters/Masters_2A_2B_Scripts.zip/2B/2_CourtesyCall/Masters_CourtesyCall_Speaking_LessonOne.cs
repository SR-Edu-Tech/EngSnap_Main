using System.Collections;
using UnityEngine;

/// <summary>
/// Core Speaking 1 controller for Unit 2: Courtesy Call (Book 2B).
/// SP01 Say It Kindly — 6 Speaking Prompts.
/// Inherits 100% of speech recognition, phrase card spawning, Levenshtein similarity, and UI flow
/// directly from the gold-standard base class `Masters_PolishedCommunication_Speaking_LessonOne`.
/// </summary>
public class Masters_CourtesyCall_Speaking_LessonOne : Masters_PolishedCommunication_Speaking_LessonOne {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Speaking;
    }

    protected override void Start() {
#if UNITY_EDITOR
        if (narratorSpeech == null) {
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Speaking/VO_SP01_ARIA.mp3");
        }
#endif
        base.Start();
        StartCoroutine(StartWithIntroAudioRoutine());
    }

    private IEnumerator StartWithIntroAudioRoutine() {
        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            yield return new WaitForSeconds(Mathf.Max(2.5f, narratorSpeech.length));
        }
    }
}
