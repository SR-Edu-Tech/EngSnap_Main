using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Core Intro controller for Unit 2: Clear Confusion (Book 2A).
/// Inherits cinematic pop-up/floating sequence and audio management from Unit 1 Intro (`Masters_PolishedCommunication_Intro_LessonOne`).
/// </summary>
public class Masters_ClearConfusion_Intro_LessonOne : Masters_PolishedCommunication_Intro_LessonOne {
    
    protected override void Start() {
        base.Start();

        // Ensure next button is correctly configured for Unit 2 Intro completion
        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }
}
