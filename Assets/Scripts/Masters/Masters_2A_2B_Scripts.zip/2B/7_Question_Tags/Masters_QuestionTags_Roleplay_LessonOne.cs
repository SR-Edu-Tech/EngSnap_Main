using UnityEngine;

/// <summary>
/// Unit 7: Question Tags - Roleplay Lesson One (RP01: On Stage — Checking with a New Friend).
/// Subclasses Unit 1's roleplay controller and provides clean data setters for the 5-step guided dialogue.
/// An NPC classmate speaks a line; the student chooses the fitting tag question rather than a blunt distractor.
/// </summary>
public class Masters_QuestionTags_Roleplay_LessonOne : Masters_PolishedCommunication_Roleplay_LessonOne {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Roleplay;
    }

    public void SetRoleplayTurns(RoleplayTurn[] turns) {
        roleplayTurns = turns;
    }

    public RoleplayTurn[] GetRoleplayTurns() {
        return roleplayTurns;
    }
}
