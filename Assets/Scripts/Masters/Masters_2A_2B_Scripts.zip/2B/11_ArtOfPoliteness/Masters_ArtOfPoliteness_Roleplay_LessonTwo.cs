using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace EngSnap.Masters.Unit11 {

    /// <summary>
    /// Unit 11: Art of Politeness - Roleplay Lesson Two (RP02: Free Scene — Your Polished Voice).
    /// Subclasses Masters_PolishedCommunication_Roleplay_LessonTwo.
    /// Manages 3 Everyday Sets:
    /// Card A: At a restaurant (Turn 1: "I'll have a pizza, please.", Turn 2: "I'm not so sure that's what I'd like, thank you.")
    /// Card B: Busy at your desk (Turn 1: "Sorry — I'm a bit busy right now.", Turn 2: "Let me know when you're available and we'll talk then.")
    /// Card C: A design meeting (Turn 1: "Could you send me the report?", Turn 2: "I'd prefer to use different colours in this design.")
    /// </summary>
    public class Masters_ArtOfPoliteness_Roleplay_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonTwo {

        public void SetScenes(SceneData[] sceneData) {
            scenes = sceneData;
        }

        public SceneData[] GetScenes() {
            return scenes;
        }

        protected override void Start() {
            base.Start();
            UpdateCardTitlesAndHeader();
        }

        public void UpdateCardTitlesAndHeader() {
            string[] cardTitles = new string[] {
                "A — At a restaurant",
                "B — Busy at your desk",
                "C — A design meeting"
            };

            // Update button texts from scenes array
            if (scenes != null) {
                for (int i = 0; i < scenes.Length && i < cardTitles.Length; i++) {
                    if (scenes[i] != null && scenes[i].sceneButton != null) {
                        TextMeshProUGUI t = scenes[i].sceneButton.GetComponentInChildren<TextMeshProUGUI>(true);
                        if (t != null) {
                            t.text = cardTitles[i];
                            t.fontSize = 20f;
                            t.enableAutoSizing = true;
                            t.fontSizeMin = 14f;
                            t.fontSizeMax = 24f;
                            t.alignment = TextAlignmentOptions.Center;
                        }
                    }
                }
            }

            // Update button texts from sceneSelectionPanel
            Transform sPanel = transform.Find("SceneSelectionPanel") ?? transform.Find("Scenes") ?? transform.Find("SceneSelection");
            if (sPanel != null) {
                Button[] btns = sPanel.GetComponentsInChildren<Button>(true);
                for (int i = 0; i < btns.Length && i < cardTitles.Length; i++) {
                    if (btns[i] == null) continue;
                    TextMeshProUGUI t = btns[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    if (t != null) {
                        t.text = cardTitles[i];
                        t.fontSize = 20f;
                        t.enableAutoSizing = true;
                        t.fontSizeMin = 14f;
                        t.fontSizeMax = 24f;
                        t.alignment = TextAlignmentOptions.Center;
                    }
                }
            }

            // Update Header & Lesson Title
            TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
            foreach (var tmp in allTMPs) {
                if (tmp == null) continue;
                string lowerName = tmp.name.ToLower();
                string textVal = tmp.text ?? "";
                if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("FREE SCENE") || textVal.Contains("Free Scene") || textVal.Contains("YOUR POLISHED") || textVal.Contains("Your Polished") || textVal.Contains("Tell")) {
                    tmp.text = "FREE SCENE — YOUR POLISHED VOICE";
                }
                if (lowerName.Contains("header") || lowerName.Contains("branch") || textVal.Contains("THE ART OF POLITENESS") || textVal.Contains("ART OF POLITENESS")) {
                    tmp.text = "THE ART OF POLITENESS";
                }
            }
        }
    }
}
