using UnityEngine;
using TMPro;

namespace EngSnap.Masters.Unit11 {

    /// <summary>
    /// Subclass for Book 2B Unit 11 (Art of Politeness) Rewards Lesson One.
    /// Replicates the proven reward celebration panel layout with Unit 11 branding.
    /// </summary>
    public class Masters_ArtOfPoliteness_Rewards_LessonOne : Masters_PolishedCommunication_Rewards_LessonOne {

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Rewards;
            masterText = "Art of Politeness Mastered!";
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Rewards;
            UpdateTitleAndUIComponents();
        }

        private void UpdateTitleAndUIComponents() {
            if (string.IsNullOrEmpty(masterText) || masterText.Contains("Sequence") || masterText.Contains("Polished")) {
                masterText = "Art of Politeness Mastered!";
            }

            TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
            foreach (var tmp in allTMPs) {
                if (tmp == null) continue;
                string lowerName = tmp.name.ToLower();
                string textVal = tmp.text ?? "";
                if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Unit") || textVal.Contains("Sequence") || textVal.Contains("Polished") || textVal.Contains("Rewards") || textVal.Contains("Mastered")) {
                    tmp.gameObject.SetActive(true);
                    tmp.text = "Unit 11 Complete!";
                }
                if (lowerName.Contains("heading") || textVal.Contains("REWARDS") || textVal.Contains("BRANCH") || textVal.Contains("CELEBRATION")) {
                    tmp.text = "REWARDS BRANCH (Celebration Badge)";
                }
            }
        }
    }
}
