using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Core controller for Unit 2: Clear Confusion - Writing Lesson One (W01: Complete the Polite Question).
/// Inherits from Unit 1 Polished Communication Writing Lesson One (`Masters_PolishedCommunication_Writing_LessonOne`).
/// </summary>
public class Masters_ClearConfusion_Writing_LessonOne : Masters_PolishedCommunication_Writing_LessonOne {

    public void SetQuestions(WritingQuestion[] newQuestions) {
        questions = newQuestions;
    }

    protected override void Start() {
        base.Start();
        // Ensure next button is properly assigned and wired
        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }
}
