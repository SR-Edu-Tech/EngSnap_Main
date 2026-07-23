using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core Rewards controller for Unit 3: Beyond the Horizon (Book 2A).
/// Subclasses PolishedCommunication_Rewards_LessonOne.
/// R01 — Topic Completed Rewards & Star Celebrations.
/// Keeps base functionality intact so fields can be freely customized in the Inspector.
/// </summary>
public class Masters_BeyondTheHorizon_Rewards_LessonOne : Masters_PolishedCommunication_Rewards_LessonOne {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Rewards;
    }
}
