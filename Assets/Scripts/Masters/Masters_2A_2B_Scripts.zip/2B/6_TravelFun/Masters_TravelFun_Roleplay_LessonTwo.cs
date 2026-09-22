using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Unit 6: Travel Fun - Roleplay Lesson Two (RP02: Free Scene — Tell Me About Your Trip).
/// Subclasses Polished Communication's proven Book 2A roleplay controller and provides clean data setters.
/// 3 Scene Cards:
/// - Card A: After the Holidays (with a friend)
/// - Card B: Hotel Booking (on the road)
/// - Card C: Seaside Weekend (with a cousin)
/// </summary>
public class Masters_TravelFun_Roleplay_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonTwo {

    public void SetScenes(SceneData[] sceneData) {
        scenes = sceneData;
    }

    public SceneData[] GetScenes() {
        return scenes;
    }

    protected override void Awake() {
        base.Awake();
        UpdateCardTitlesAndHeader();
    }

    protected override void Start() {
        base.Start();
        if (progressCountTMP != null && scenes != null) {
            progressCountTMP.text = $"{completedScenes.Count}/{scenes.Length}";
        }
        UpdateCardTitlesAndHeader();
    }

    public void UpdateCardTitlesAndHeader() {
        string[] cardTitles = new string[] {
            "Card A — After the Holidays",
            "Card B — Hotel Booking",
            "Card C — Seaside Weekend"
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

        // Update button texts from sceneSelectionPanel or children
        Transform sPanel = transform.Find("SceneSelectionPanel") ?? transform.Find("Scenes") ?? transform.Find("SceneSelector") ?? transform.Find("SceneSelection");
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

        // Search by button names Scene1button, Scene2button, Scene3button if any
        for (int i = 0; i < 3; i++) {
            Transform btnTr = transform.Find($"Scene{i+1}button") ?? transform.Find($"Scenes/Scene{i+1}button") ?? transform.Find($"SceneSelector/Scene{i+1}button");
            if (btnTr != null) {
                TextMeshProUGUI t = btnTr.GetComponentInChildren<TextMeshProUGUI>(true);
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
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("FREE SCENE") || textVal.Contains("Free Scene") || textVal.Contains("TELL THE SAME") || textVal.Contains("Tell") || textVal.Contains("YOUR TRIP") || textVal.Contains("TRIP")) {
                tmp.text = "FREE SCENE — TELL ME ABOUT YOUR TRIP";
            }
            if (lowerName.Contains("header") || lowerName.Contains("heading") || lowerName.Contains("branch") || textVal.Contains("TRAVEL FUN") || textVal.Contains("POLISHED")) {
                tmp.text = "TRAVEL FUN";
            }
        }
    }
}

