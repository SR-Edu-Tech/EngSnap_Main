using UnityEngine;

/// <summary>
/// Unit 3: Beyond The Horizon - Roleplay Lesson One (RP01: On Stage — Help a Traveller Find the Way).
/// Subclasses Unit 1's roleplay controller and provides clean data setters.
/// </summary>
public class Masters_BeyondTheHorizon_Roleplay_LessonOne : Masters_PolishedCommunication_Roleplay_LessonOne {

    public void SetRoleplayTurns(RoleplayTurn[] turns) {
        roleplayTurns = turns;
    }

    public RoleplayTurn[] GetRoleplayTurns() {
        return roleplayTurns;
    }
}
