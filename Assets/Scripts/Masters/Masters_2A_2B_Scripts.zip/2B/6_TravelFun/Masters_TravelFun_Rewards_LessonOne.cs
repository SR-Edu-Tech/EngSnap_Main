using UnityEngine;
using TMPro;

namespace EngSnap.Masters.Unit6 {

    /// <summary>
    /// Subclass for Book 2B Unit 6 (Travel Fun) Rewards Lesson One.
    /// Inherits star pop celebrations, topic completion sequence, and animations.
    /// </summary>
    public class Masters_TravelFun_Rewards_LessonOne : Masters_PolishedCommunication_Rewards_LessonOne {

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
            if (string.IsNullOrEmpty(masterText) || masterText.Contains("Polished")) {
                masterText = "Travel Fun Mastered!";
            }

            TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
            foreach (var tmp in allTMPs) {
                if (tmp == null) continue;
                string lowerName = tmp.name.ToLower();
                string textVal = tmp.text ?? "";
                if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Unit") || textVal.Contains("Polished") || textVal.Contains("Rewards") || textVal.Contains("Mastered")) {
                    tmp.gameObject.SetActive(true);
                    tmp.text = "Unit 6 Complete!";
                }
                if (lowerName.Contains("heading") || textVal.Contains("REWARDS") || textVal.Contains("BRANCH") || textVal.Contains("CELEBRATION")) {
                    tmp.text = "REWARDS BRANCH (Celebration Badge)";
                }
            }
        }
    }
}
