using UnityEngine;
using TMPro;

/// <summary>
/// Subclass for Book 2B Unit 5 (Doctor Need Your Help) Rewards Lesson One.
/// Replicates the proven reward celebration panel layout with Unit 5 branding.
/// </summary>
public class Masters_DoctorNeedYourHelp_Rewards_LessonOne : Masters_PolishedCommunication_Rewards_LessonOne {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Rewards;
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Rewards;
        UpdateTitleAndUIComponents();
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Unit") || textVal.Contains("Polished") || textVal.Contains("Rewards") || textVal.Contains("Mastered")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "Unit 5 Complete!";
            }
            if (lowerName.Contains("heading") || textVal.Contains("REWARDS") || textVal.Contains("CELEBRATION")) {
                tmp.text = "REWARDS BRANCH (Celebration Badge)";
            }
        }
    }
}
