using UnityEngine;

/// <summary>
/// Core Intro controller for Book 3A Unit 9: Let's Talk About Future Plans.
/// Inherits from Masters_BoostSomeoneUp_Intro_LessonOne.
/// </summary>
public class Masters_LetsTalkAboutFuturePlans_Intro_LessonOne : Masters_BoostSomeoneUp_Intro_LessonOne {
    protected override void Awake() {
        topic = Masters_Topic.Intro;
        
        formalSentences = new string[] {
            "I'm going to take computer classes.",
            "I'm planning to join a music club.",
            "I will go to the science fair.",
            "I'm starting to learn coding.",
            "I'll visit my grandma tomorrow.",
            "I'll start my project tonight."
        };

        informalSentences = new string[] {
            "I hope to become a doctor.",
            "I really want to learn to dance.",
            "I'd really like to travel one day.",
            "I'd like to try painting.",
            "I'd like to start guitar classes.",
            "I'd like to begin music lessons."
        };

        base.Awake();
    }
}
