using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Unit 3 (Household Chores): Roleplay Lesson Two (RP02: Free Scene — Share Out the Work).
/// Subclasses Masters_PolishedCommunication_Roleplay_LessonTwo.
/// 3 Scene Cards:
/// - Card A: Before the festival (with Mom)
/// - Card B: Splitting jobs (with a sibling)
/// - Card C: Reporting back (with Dad)
/// </summary>
public class Masters_HouseholdChores_Roleplay_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonTwo {

    public void SetScenes(SceneData[] sceneData) {
        scenes = sceneData;
    }

    public SceneData[] GetScenes() {
        return scenes;
    }

    protected override void Start() {
        base.Start();
        if (progressCountTMP != null && scenes != null) {
            progressCountTMP.text = $"0/{scenes.Length}";
        }
        UpdateTitleAndHeader();
        DisableDialogueAutoSizing();
    }

    private void UpdateTitleAndHeader() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Free Scene") || textVal.Contains("FREE SCENE") || textVal.Contains("Polished") || textVal.Contains("Tell") || textVal.Contains("Share Out") || textVal.Contains("STAGE") || textVal.Contains("SHOPPING")) {
                tmp.text = "RP02 FREE SCENE — SHARE OUT THE WORK";
            }
            if (lowerName.Contains("heading") || lowerName.Contains("branch") || textVal.Contains("ROLEPLAY") || textVal.Contains("BRANCH")) {
                tmp.text = "ROLEPLAY BRANCH (Stage)";
            }
            if (lowerName.Contains("subtitle") || textVal.Contains("Select a scene") || textVal.Contains("Select a shop")) {
                tmp.text = "Select a scene card and share out the chores across two turns.";
            }
            if (textVal.Contains("Reception") || textVal.Contains("Boutique") || (tmp.transform.parent != null && tmp.transform.parent.name.Contains("Scene1"))) {
                tmp.text = "Before the festival (with Mom)";
            }
            if (textVal.Contains("Consulting") || textVal.Contains("Grocery") || (tmp.transform.parent != null && tmp.transform.parent.name.Contains("Scene2"))) {
                tmp.text = "Splitting jobs (with a sibling)";
            }
            if (textVal.Contains("At home") || textVal.Contains("Pet Shop") || (tmp.transform.parent != null && tmp.transform.parent.name.Contains("Scene3"))) {
                tmp.text = "Reporting back (with Dad)";
            }
        }
    }

    private void DisableDialogueAutoSizing() {
        if (npcDialogueTMP != null) {
            npcDialogueTMP.enableAutoSizing = false;
            npcDialogueTMP.fontSize = 22f;
            npcDialogueTMP.alignment = TextAlignmentOptions.Center;
            npcDialogueTMP.enableWordWrapping = true;
        }
        if (playerDialogueTMP != null) {
            playerDialogueTMP.enableAutoSizing = false;
            playerDialogueTMP.fontSize = 22f;
            playerDialogueTMP.alignment = TextAlignmentOptions.Center;
            playerDialogueTMP.enableWordWrapping = true;
        }
        if (slateSentenceTMP != null) {
            slateSentenceTMP.enableAutoSizing = false;
            slateSentenceTMP.fontSize = 22f;
            slateSentenceTMP.alignment = TextAlignmentOptions.Center;
            slateSentenceTMP.enableWordWrapping = true;
        }
    }

    protected override void StartNextTurn() {
        DisableDialogueAutoSizing();
        base.StartNextTurn();
        DisableDialogueAutoSizing();
    }
}
