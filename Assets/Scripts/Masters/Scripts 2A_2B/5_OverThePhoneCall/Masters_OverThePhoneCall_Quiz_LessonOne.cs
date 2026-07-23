using UnityEngine;

/// <summary>
/// Subclass for Unit 5: Over The Phone Call - Quiz Lesson One (Q01: 12-Question Phone Call Review).
/// Inherits 100% of the quiz UI, scoring, confirmation, and navigation logic from Book 2A verified base class.
/// </summary>
public class Masters_OverThePhoneCall_Quiz_LessonOne : Masters_PolishedCommunication_Quiz_LessonOne {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Quiz;
    }
}
