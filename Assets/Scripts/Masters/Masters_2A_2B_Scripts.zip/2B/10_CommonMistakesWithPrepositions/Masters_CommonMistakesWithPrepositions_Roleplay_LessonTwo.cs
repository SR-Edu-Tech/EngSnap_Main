using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace EngSnap.Masters.Unit10 {

    /// <summary>
    /// Unit 10: Common Mistakes with Prepositions - Roleplay Lesson Two (RP02: Free Scene — Error-Free Talk).
    /// Subclasses Masters_PolishedCommunication_Roleplay_LessonTwo.
    /// Manages 3 Everyday Sets:
    /// Card A: A phone call (Turn 1: 'Who is on the phone?', Turn 2: 'I am going home now.')
    /// Card B: A shop counter (Turn 1: 'How much does it come to?', Turn 2: 'We have no money left.')
    /// Card C: A classroom (Turn 1: 'I must learn English. I like July more than May.', Turn 2: 'I'm 26 years old. My birthday is in January.')
    /// </summary>
    public class Masters_CommonMistakesWithPrepositions_Roleplay_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonTwo {

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
                "A — A phone call",
                "B — A shop counter",
                "C — A classroom"
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
                if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("FREE SCENE") || textVal.Contains("Free Scene") || textVal.Contains("TELL THE SAME") || textVal.Contains("Tell")) {
                    tmp.text = "FREE SCENE — ERROR-FREE TALK";
                }
                if (lowerName.Contains("header") || lowerName.Contains("branch") || textVal.Contains("COMMON MISTAKES")) {
                    tmp.text = "COMMON MISTAKES WITH PREPOSITIONS";
                }
            }
        }
    }
}
