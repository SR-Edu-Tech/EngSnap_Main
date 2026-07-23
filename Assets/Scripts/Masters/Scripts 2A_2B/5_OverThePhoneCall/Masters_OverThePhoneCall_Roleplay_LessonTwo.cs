using UnityEngine;

/// <summary>
/// Unit 5: Over The Phone Call - Roleplay Lesson Two (RP02: Free Scene — Make Your Own Call).
/// Subclasses Unit 1's free scene roleplay controller directly.
/// </summary>
public class Masters_OverThePhoneCall_Roleplay_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonTwo {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Roleplay;
    }
}
