using UnityEngine;

/// <summary>
/// Subclass for Unit 8: Source of Inspiration - Speaking Lesson Two (SP02: Say It Fast - The Speed Ladder).
/// Inherits 100% of the speech recognition, coroutine, phrase card spawning, and UI logic
/// directly from the gold-standard base class `Masters_PolishedCommunication_Speaking_LessonOne`.
/// </summary>
public class Masters_SourceOfInspiration_Speaking_LessonTwo : Masters_PolishedCommunication_Speaking_LessonOne {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Speaking;
    }
}
