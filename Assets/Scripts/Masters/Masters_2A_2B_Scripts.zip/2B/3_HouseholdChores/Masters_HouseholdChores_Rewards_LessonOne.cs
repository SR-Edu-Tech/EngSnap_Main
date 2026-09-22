using UnityEngine;

/// <summary>
/// Unit 3 (Household Chores): Rewards Lesson One.
/// Subclasses Masters_PolishedCommunication_Rewards_LessonOne.
/// </summary>
public class Masters_HouseholdChores_Rewards_LessonOne : Masters_PolishedCommunication_Rewards_LessonOne {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Rewards;
        masterText = "Household Chores Mastered!";
    }
}
