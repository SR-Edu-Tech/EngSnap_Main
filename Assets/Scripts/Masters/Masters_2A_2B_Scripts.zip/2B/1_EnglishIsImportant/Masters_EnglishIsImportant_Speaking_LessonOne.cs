using UnityEngine;

/// <summary>
/// Subclass for Unit 1: English Is Important - Speaking Lesson One (SP01: Say It Out Loud).
/// Inherits 100% of the speech recognition, coroutine, phrase card spawning, and UI logic
/// directly from the gold-standard base class `Masters_PolishedCommunication_Speaking_LessonOne`.
/// </summary>
public class Masters_EnglishIsImportant_Speaking_LessonOne : Masters_PolishedCommunication_Speaking_LessonOne {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Speaking;
    }
}
