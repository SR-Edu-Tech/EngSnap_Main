using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Unit 4 (Colour Your Speech): Roleplay Lesson Two (RP02: Free Scene — Introduce and Perform).
/// Subclasses Masters_PolishedCommunication_Roleplay_LessonTwo.
/// 3 Scene Cards:
/// - Card A: Piece of cake
/// - Card B: Miss the boat
/// - Card C: Bury the hatchet
/// </summary>
public class Masters_2B_ColourYourSpeech_Roleplay_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonTwo {

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

        // Lock card buttons during introduction audio so game starts playing only after intro ends
        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            SetSceneButtonsInteractable(false);
            StartCoroutine(EnableSceneButtonsAfterIntro(narratorSpeech.length));
        } else {
            SetSceneButtonsInteractable(true);
        }
    }

    private IEnumerator EnableSceneButtonsAfterIntro(float duration) {
        yield return new WaitForSeconds(duration);
        SetSceneButtonsInteractable(true);
    }

    private void SetSceneButtonsInteractable(bool interactable) {
        if (scenes != null) {
            foreach (var s in scenes) {
                if (s.sceneButton != null) {
                    s.sceneButton.interactable = interactable;
                }
            }
        }
    }

    private void UpdateTitleAndHeader() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Free Scene") || textVal.Contains("FREE SCENE") || textVal.Contains("Polished") || textVal.Contains("Tell") || textVal.Contains("Share Out") || textVal.Contains("STAGE") || textVal.Contains("SHOPPING") || textVal.Contains("Introduce")) {
                tmp.text = "RP02 FREE SCENE — INTRODUCE AND PERFORM";
            }
            if (lowerName.Contains("heading") || lowerName.Contains("branch") || textVal.Contains("ROLEPLAY") || textVal.Contains("BRANCH")) {
                tmp.text = "ROLEPLAY BRANCH (Stage)";
            }
            if (lowerName.Contains("subtitle") || textVal.Contains("Select a scene") || textVal.Contains("Select a shop") || textVal.Contains("Select a card")) {
                tmp.text = "Select an idiom card to introduce and perform the scene on stage.";
            }

            // Correct Card Titles
            if (textVal.Contains("Reception") || textVal.Contains("Boutique") || textVal.Contains("festival") || textVal.Contains("Piece") || (tmp.transform.parent != null && tmp.transform.parent.name.Contains("Scene1"))) {
                tmp.text = "Card A — Piece of cake";
            }
            if (textVal.Contains("Consulting") || textVal.Contains("Grocery") || textVal.Contains("Splitting") || textVal.Contains("boat") || (tmp.transform.parent != null && tmp.transform.parent.name.Contains("Scene2"))) {
                tmp.text = "Card B — Miss the boat";
            }
            if (textVal.Contains("At home") || textVal.Contains("Pet Shop") || textVal.Contains("Reporting") || textVal.Contains("hatchet") || (tmp.transform.parent != null && tmp.transform.parent.name.Contains("Scene3"))) {
                tmp.text = "Card C — Bury the hatchet";
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
