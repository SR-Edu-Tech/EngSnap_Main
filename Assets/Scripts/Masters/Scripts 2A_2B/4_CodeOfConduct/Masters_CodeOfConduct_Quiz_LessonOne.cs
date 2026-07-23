using UnityEngine;

/// <summary>
/// Subclass for Unit 4: Code of Conduct - Quiz Lesson One (Q01: 12-Question Etiquette Review).
/// Inherits 100% of the quiz UI, scoring, confirmation, and navigation logic from Book 2A verified base class.
/// </summary>
public class Masters_CodeOfConduct_Quiz_LessonOne : Masters_PolishedCommunication_Quiz_LessonOne {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Quiz;
    }
}
