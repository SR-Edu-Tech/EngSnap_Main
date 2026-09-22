using UnityEngine;
using TMPro;

/// <summary>
/// Unit 2 (Courtesy Call): Roleplay Lesson One (RP01: On Stage — Jammy and Sam).
/// Subclasses Masters_PolishedCommunication_Roleplay_LessonOne.
/// </summary>
public class Masters_CourtesyCall_Roleplay_LessonOne : Masters_PolishedCommunication_Roleplay_LessonOne {

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
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("STAGE") || textVal.Contains("Polished") || textVal.Contains("Clarify") || textVal.Contains("Greet") || textVal.Contains("Noisy")) {
                tmp.text = "RP01 On Stage — Jammy and Sam";
            }
            if (lowerName.Contains("heading") || lowerName.Contains("branch") || textVal.Contains("ROLEPLAY") || textVal.Contains("BRANCH")) {
                tmp.text = "ROLEPLAY BRANCH (Stage)";
            }
        }
    }
}
