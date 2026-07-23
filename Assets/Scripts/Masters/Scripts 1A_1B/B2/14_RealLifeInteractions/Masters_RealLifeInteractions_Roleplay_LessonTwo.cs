using UnityEngine;

/// <summary>
/// Roleplay Lesson 2 for Unit 14 Real Life Interactions.
/// Implements Free Scene conversation using input textfield and heuristic dictionary validation.
/// Subclasses OfferingAHelpingHand_Writing_LessonTwo which uses Masters_SentenceValidator against words.txt.
/// </summary>
public class Masters_RealLifeInteractions_Roleplay_LessonTwo : Masters_OfferingAHelpingHand_Writing_LessonTwo {

    [Header("Unit 14 Audio")]
    [SerializeField] private AudioClip narratorAudio;

    protected override void Start() {
        base.Start();
        if (narratorAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorAudio);
        }
    }
}
