using UnityEngine;

/// <summary>
/// Subclass for Unit 7: Question Tags - Speaking Lesson One (SP01: Call It Across the Valley).
/// Inherits 100% of the speech recognition, coroutines, phrase card spawning, and UI logic
/// directly from the gold-standard base class `Masters_PolishedCommunication_Speaking_LessonOne`.
/// </summary>
public class Masters_QuestionTags_Speaking_LessonOne : Masters_PolishedCommunication_Speaking_LessonOne {

    public void SetSpeechToTextData(SpeechToText[] data) {
        speechToTextArray = data;
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Speaking;
    }
}
