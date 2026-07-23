using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Core Intro controller for Unit 3: Beyond the Horizon (Book 2A).
/// Inherits cinematic pop-up/floating sequence and audio management from Unit 1 Intro (`Masters_PolishedCommunication_Intro_LessonOne`).
/// </summary>
public class Masters_BeyondTheHorizon_Intro_LessonOne : Masters_PolishedCommunication_Intro_LessonOne {
    
    protected override void Start() {
        base.Start();

        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }
}
