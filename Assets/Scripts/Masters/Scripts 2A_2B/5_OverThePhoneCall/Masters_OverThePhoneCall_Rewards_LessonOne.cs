using UnityEngine;

/// <summary>
/// Subclass for Unit 5: Over The Phone Call - Rewards Lesson One (R01: Over The Phone Call Master Badge).
/// Inherits 100% of the celebration star animations and announcement logic from Book 2A verified base class.
/// </summary>
public class Masters_OverThePhoneCall_Rewards_LessonOne : Masters_PolishedCommunication_Rewards_LessonOne {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Rewards;
    }
}
