using UnityEngine;
using TMPro;

/// <summary>
/// Unit 3 (Household Chores): Roleplay Lesson One (RP01: On Stage — Helping My Mom).
/// Subclasses Masters_PolishedCommunication_Roleplay_LessonOne.
/// Matches the standard theme, character layout, options layout, and workflow of Unit 2 Courtesy Call Roleplay.
/// </summary>
public class Masters_HouseholdChores_Roleplay_LessonOne : Masters_PolishedCommunication_Roleplay_LessonOne {

    public void SetRoleplayTurns(RoleplayTurn[] turns) {
        roleplayTurns = turns;
    }

    public RoleplayTurn[] GetRoleplayTurns() {
        return roleplayTurns;
    }

    protected override void Start() {
        base.Start();
        if (progressCountTMP != null && roleplayTurns != null) {
            progressCountTMP.text = $"0/{roleplayTurns.Length}";
        }
        UpdateTitleAndHeader();
    }

    private void UpdateTitleAndHeader() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("STAGE") || textVal.Contains("Polished") || textVal.Contains("Clarify") || textVal.Contains("Greet") || textVal.Contains("Noisy") || textVal.Contains("Jammy") || textVal.Contains("Helping")) {
                tmp.text = "RP01 On Stage — Helping My Mom";
            }
            if (lowerName.Contains("heading") || lowerName.Contains("branch") || textVal.Contains("ROLEPLAY") || textVal.Contains("BRANCH")) {
                tmp.text = "ROLEPLAY BRANCH (Stage)";
            }
        }
    }
}
