using UnityEngine;

/// <summary>
/// Core Intro controller for Unit 4: Code of Conduct (Book 2A).
/// Inherits cinematic fade-in / Street house pop-up sequences and audio management from Unit 1 Intro (`Masters_PolishedCommunication_Intro_LessonOne`).
/// </summary>
public class Masters_CodeOfConduct_Intro_LessonOne : Masters_PolishedCommunication_Intro_LessonOne {
    
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Intro;
    }

    protected override void Start() {
        base.Start();
        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }
}
