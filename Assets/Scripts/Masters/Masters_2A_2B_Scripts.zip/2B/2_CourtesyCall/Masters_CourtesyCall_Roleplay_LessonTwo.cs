using UnityEngine;
using TMPro;

/// <summary>
/// Unit 2 (Courtesy Call): Roleplay Lesson Two (RP02: Free Scene — Your Turn to Be Kind).
/// Subclasses Masters_PolishedCommunication_Roleplay_LessonTwo.
/// </summary>
public class Masters_CourtesyCall_Roleplay_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonTwo {

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
    }

    private void UpdateTitleAndHeader() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Free Scene") || textVal.Contains("Polished") || textVal.Contains("Tell")) {
                tmp.text = "RP02 Free Scene — Your Turn to Be Kind";
            }
            if (lowerName.Contains("heading") || lowerName.Contains("branch") || textVal.Contains("ROLEPLAY") || textVal.Contains("BRANCH")) {
                tmp.text = "ROLEPLAY BRANCH (Stage)";
            }
        }
    }
}
