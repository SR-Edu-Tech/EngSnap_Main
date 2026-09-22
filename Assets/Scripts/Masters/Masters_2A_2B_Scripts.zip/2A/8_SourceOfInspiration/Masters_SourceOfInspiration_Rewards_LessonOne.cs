using UnityEngine;
using TMPro;

/// <summary>
/// Subclass for Unit 8 (Source of Inspiration) Rewards Lesson One.
/// Uses the clean Unit 6 (Groove On) Rewards layout reference base.
/// </summary>
public class Masters_SourceOfInspiration_Rewards_LessonOne : Masters_PolishedCommunication_Rewards_LessonOne {

    public static AudioClip SharedUserRecordingClip;

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
            string textVal = tmp.text != null ? tmp.text : "";
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Unit 6") || textVal.Contains("Unit 7") || textVal.Contains("Polished") || textVal.Contains("Rewards") || textVal.Contains("Mastered")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "Unit 8 Complete!";
            }
            if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("SOURCE") || textVal.Contains("REWARDS")) {
                tmp.text = "REWARDS BRANCH (Celebration Badge)";
            }
        }
    }
}
