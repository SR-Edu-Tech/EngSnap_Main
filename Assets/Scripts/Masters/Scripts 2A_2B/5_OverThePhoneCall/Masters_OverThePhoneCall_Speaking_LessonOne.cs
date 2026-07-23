using UnityEngine;

/// <summary>
/// Subclass for Unit 5: Over The Phone Call - Speaking Lesson One (SP01: On the Line — Speak Your Phone Lines).
/// Inherits 100% of the speech recognition, coroutine, and UI logic from Book 2A verified base class.
/// </summary>
public class Masters_OverThePhoneCall_Speaking_LessonOne : Masters_PolishedCommunication_Speaking_LessonOne {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Speaking;
    }
}
