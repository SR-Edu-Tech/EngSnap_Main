using UnityEngine;

/// <summary>
/// Core Speaking 1 controller for Unit 6: Travel Fun (Book 2B).
/// SP01 Do You Know the Meaning? — 6 Speaking Prompts.
/// Inherits 100% of speech recognition, phrase card spawning, Levenshtein similarity, and UI flow
/// directly from the gold-standard base class `Masters_PolishedCommunication_Speaking_LessonOne`.
/// </summary>
public class Masters_TravelFun_Speaking_LessonOne : Masters_PolishedCommunication_Speaking_LessonOne {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Speaking;
    }
}
