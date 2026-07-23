using UnityEngine;

/// <summary>
/// Game Lesson 1 for Unit 14 Real Life Interactions.
/// Implements Scene Scramble (Conveyor Sequence Scramble) where conversation lines drift across an arcade conveyor
/// and the player taps them in chronological dialogue order.
/// </summary>
public class Masters_RealLifeInteractions_Game_LessonOne : Masters_JumbledWords_Game_LessonOne {

    [Header("Unit 14 Audio")]
    [SerializeField] private AudioClip narratorAudio;

    protected override void Start() {
        base.Start();
        if (narratorAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorAudio);
        }
    }
}
