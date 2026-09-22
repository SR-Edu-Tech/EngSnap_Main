using UnityEngine;

/// <summary>
/// Core Reading 2 controller for Unit 4: Colour Your Speech (Book 2B).
/// R02 Match — Idiom and Meaning: 10 verbatim idioms on left, verbatim book meanings on right.
/// Drag lines from idioms to their exact meaning across 2 sets (5 pairs each).
/// Inherits full line-drag and node matching architecture from `Masters_ClearConfusion_Reading_LessonTwo`.
/// </summary>
public class Masters_2B_ColourYourSpeech_Reading_LessonTwo : Masters_ClearConfusion_Reading_LessonTwo {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;
    }
}
