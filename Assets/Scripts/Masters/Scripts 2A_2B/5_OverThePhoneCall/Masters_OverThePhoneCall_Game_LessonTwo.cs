using UnityEngine;

/// <summary>
/// Unit 5: Over The Phone Call - Game Lesson Two (G02: Phrasal-Verb Match — Verb <-> Meaning Memory).
/// Subclasses Unit 1's memory tile matching controller directly.
/// </summary>
public class Masters_OverThePhoneCall_Game_LessonTwo : Masters_PolishedCommunication_Game_LessonTwo {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;
    }
}
