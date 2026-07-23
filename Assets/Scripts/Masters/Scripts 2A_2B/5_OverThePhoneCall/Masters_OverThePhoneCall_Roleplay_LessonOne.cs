using UnityEngine;

/// <summary>
/// Unit 5: Over The Phone Call - Roleplay Lesson One (RP01: On Stage — Answer the Call).
/// Subclasses Unit 1's stage-based roleplay controller directly.
/// </summary>
public class Masters_OverThePhoneCall_Roleplay_LessonOne : Masters_PolishedCommunication_Roleplay_LessonOne {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Roleplay;
    }
}
