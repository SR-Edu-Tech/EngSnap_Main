using UnityEngine;

/// <summary>
/// Core Reading 1 controller for Unit 5: Over The Phone Call (Book 2A).
/// R01 Pick the Right Phone Phrase (in-context): Displays a noticeboard situation + register hint across 12 rounds.
/// Inherits core setup, option shuffling, and validation mechanics from `Masters_CodeOfConduct_Reading_LessonOne`.
/// </summary>
public class Masters_OverThePhoneCall_Reading_LessonOne : Masters_CodeOfConduct_Reading_LessonOne {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;
    }
}
