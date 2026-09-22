using UnityEngine;
using TMPro;

/// <summary>
/// Subclass for Unit 1 (English Is Important) Rewards Lesson One.
/// Inherits full star pop celebration, topic completion sequence, and animations from the Groove On / Polished Communication base.
/// </summary>
public class Masters_EnglishIsImportant_Rewards_LessonOne : Masters_PolishedCommunication_Rewards_LessonOne {

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
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Unit") || textVal.Contains("Groove") || textVal.Contains("Polished") || textVal.Contains("Rewards") || textVal.Contains("Mastered")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "Unit 1 Rewards — Mastered!";
            }
            if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("REWARDS") || textVal.Contains("BRANCH")) {
                tmp.text = "REWARDS BRANCH (Celebration Badge)";
            }
        }
    }
}
