using UnityEngine;
using TMPro;

/// <summary>
/// Subclass for Unit 6 (Groove On) Rewards Lesson One.
/// </summary>
public class Masters_GrooveOn_Rewards_LessonOne : Masters_PolishedCommunication_Rewards_LessonOne {
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
            string textVal = tmp.text;
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Polished") || textVal.Contains("Rewards") || textVal.Contains("Mastered")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "Unit 6 Rewards — Mastered!";
            }
            if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("REWARDS")) {
                tmp.text = "REWARDS BRANCH (Celebration Badge)";
            }
        }
    }
}