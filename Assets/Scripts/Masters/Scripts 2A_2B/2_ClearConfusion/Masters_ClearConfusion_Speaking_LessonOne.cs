using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unit 2: Clear Confusion - Speaking Lesson One (SP01: Ask It Aloud — Clear Your Doubt).
/// Inherits from Unit 1's polished speaking controller (Masters_PolishedCommunication_Speaking_LessonOne).
/// Provides helpful setters for data injection and exact verification.
/// </summary>
public class Masters_ClearConfusion_Speaking_LessonOne : Masters_PolishedCommunication_Speaking_LessonOne {

    public void SetSpeechToTextData(SpeechToText[] data) {
        speechToTextArray = data;
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Speaking;
    }

    protected override void OnEnable() {
        base.OnEnable();
    }

    protected override void OnDisable() {
        base.OnDisable();
    }
}
