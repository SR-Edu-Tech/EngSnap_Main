using UnityEngine;

/// <summary>
/// Subclass for Unit 4: Code of Conduct - Rewards Lesson One (R01: Kindness Town Grand Celebration).
/// Inherits 100% of the celebration star animations and announcement logic from Book 2A verified base class.
/// </summary>
public class Masters_CodeOfConduct_Rewards_LessonOne : Masters_PolishedCommunication_Rewards_LessonOne {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Rewards;
    }
}
