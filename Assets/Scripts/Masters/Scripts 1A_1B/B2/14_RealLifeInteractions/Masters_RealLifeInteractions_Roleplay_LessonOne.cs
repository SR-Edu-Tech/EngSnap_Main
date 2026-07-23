using UnityEngine;

/// <summary>
/// Roleplay Lesson 1 for Unit 14 Real Life Interactions.
/// Interactive dialogue where student picks the verbatim next line from MCQ options.
/// Plays narrator instruction audio at the start of the lesson.
/// </summary>
public class Masters_RealLifeInteractions_Roleplay_LessonOne : Masters_OfferingAHelpingHand_Roleplay_LessonOne {

    [Header("Unit 14 Audio")]
    [SerializeField] private AudioClip narratorAudio;

    protected override void Start() {
        base.Start();
        if (narratorAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorAudio);
        }
    }
}
