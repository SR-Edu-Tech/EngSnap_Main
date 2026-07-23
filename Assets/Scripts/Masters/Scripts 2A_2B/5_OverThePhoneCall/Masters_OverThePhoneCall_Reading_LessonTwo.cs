using UnityEngine;

/// <summary>
/// Core Reading 2 controller for Unit 5: Over The Phone Call (Book 2A).
/// R02 Match — Phrasal Verb <-> Meaning: One-to-one line dragging across 9 pairs.
/// Inherits core line matching mechanics and pagination from `Masters_CodeOfConduct_Reading_LessonTwo`.
/// </summary>
public class Masters_OverThePhoneCall_Reading_LessonTwo : Masters_CodeOfConduct_Reading_LessonTwo {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;
    }
}
