using UnityEngine;

/// <summary>
/// Subclass for Unit 4: Code of Conduct - Speaking Lesson One (SP01: Say It Kindly — Five Moments).
/// Inherits 100% of the speech recognition, coroutine, and UI logic from Book 2A verified base class.
/// </summary>
public class Masters_CodeOfConduct_Speaking_LessonOne : Masters_PolishedCommunication_Speaking_LessonOne {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Speaking;
    }
}
