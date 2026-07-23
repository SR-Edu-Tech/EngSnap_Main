using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unit 2: Clear Confusion - Quiz Lesson One (Q01).
/// Inherits from Unit 1's Quiz controller (Masters_PolishedCommunication_Quiz_LessonOne).
/// Provides exact setter methods for Editor data injection.
/// </summary>
public class Masters_ClearConfusion_Quiz_LessonOne : Masters_PolishedCommunication_Quiz_LessonOne {

    public void SetQuizData(Quiz[] data) {
        quizArray = data;
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Quiz;
    }
}
