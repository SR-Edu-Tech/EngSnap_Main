using UnityEngine;

/// <summary>
/// Unit 2: Clear Confusion - Roleplay Lesson One (RP01: On Stage — Clear a Doubt Step by Step).
/// Subclasses Unit 1's roleplay controller and provides clean data setters for the 4-step doubt clearing dialogue.
/// </summary>
public class Masters_ClearConfusion_Roleplay_LessonOne : Masters_PolishedCommunication_Roleplay_LessonOne {

    public void SetRoleplayTurns(RoleplayTurn[] turns) {
        roleplayTurns = turns;
    }

    public RoleplayTurn[] GetRoleplayTurns() {
        return roleplayTurns;
    }
}
