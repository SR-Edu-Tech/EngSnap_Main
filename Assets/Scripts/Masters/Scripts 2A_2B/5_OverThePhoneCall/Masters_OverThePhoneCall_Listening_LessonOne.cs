using UnityEngine;

/// <summary>
/// Core Listening 1 controller for Unit 5: Over the Phone Call (Book 2A).
/// Hear It — Formal or Informal Call? Audio-to-register recognition across 10 rounds: student listens to a voiced verbatim phone phrase
/// and matches it to either FORMAL (Index 0) or INFORMAL (Index 1).
/// Inherits 100% of the UI, scoring, slow toggle, repeat toggle, and progression logic from Book 2A verified base class (`Masters_PolishedCommunication_Listening_LessonOne`).
/// </summary>
public class Masters_OverThePhoneCall_Listening_LessonOne : Masters_PolishedCommunication_Listening_LessonOne {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Listening;
    }
}
