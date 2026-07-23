using UnityEngine;

/// <summary>
/// Core Reading 3 controller for Unit 5: Over The Phone Call (Book 2A).
/// R03 Name That Phone Word (vocabulary): 8 clue cards + rail of verbatim phone nouns.
/// Inherits option shuffling and validation mechanics from `Masters_CodeOfConduct_Reading_LessonThree`.
/// </summary>
public class Masters_OverThePhoneCall_Reading_LessonThree : Masters_CodeOfConduct_Reading_LessonThree {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;
    }
}
