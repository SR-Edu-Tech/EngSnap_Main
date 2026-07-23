using UnityEngine;

/// <summary>
/// Unit 4: Code of Conduct - Roleplay Lesson One (RP01: On Stage — A Kind Exchange).
/// Subclasses Unit 1's roleplay controller and provides clean data setters for the 4-step guided dialogue.
/// </summary>
public class Masters_CodeOfConduct_Roleplay_LessonOne : Masters_PolishedCommunication_Roleplay_LessonOne {

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
