using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unit 2: Clear Confusion - Rewards Lesson One (R01).
/// Inherits from Unit 1's Rewards controller (Masters_PolishedCommunication_Rewards_LessonOne).
/// Supports 8 topic completed announcements for Unit 2: Intro, Listening, Reading, Writing, Speaking, Game, Roleplay, Quiz.
/// </summary>
public class Masters_ClearConfusion_Rewards_LessonOne : Masters_PolishedCommunication_Rewards_LessonOne {

    public void SetRewardsData(string[] topicTexts, AudioClip[] topicClips, string masterCompletedText, AudioClip masterCompletedClip) {
        allTopicCompletedText = topicTexts;
        allTopicCompletedAudioClips = topicClips;
        masterText = masterCompletedText;
        masterAudioClip = masterCompletedClip;
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Rewards;
    }
}
