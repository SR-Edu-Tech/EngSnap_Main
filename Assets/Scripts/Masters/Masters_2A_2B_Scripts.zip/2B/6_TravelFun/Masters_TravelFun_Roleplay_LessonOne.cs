using UnityEngine;

/// <summary>
/// Unit 6: Travel Fun - Roleplay Lesson One (RP01: On Stage — The Delayed Journey).
/// Subclasses Polished Communication's proven Book 2A roleplay controller and provides clean data setters.
/// </summary>
public class Masters_TravelFun_Roleplay_LessonOne : Masters_PolishedCommunication_Roleplay_LessonOne {

    public void SetRoleplayTurns(RoleplayTurn[] turns) {
        roleplayTurns = turns;
    }

    public RoleplayTurn[] GetRoleplayTurns() {
        return roleplayTurns;
    }
}
