using UnityEngine;
using TMPro;

/// <summary>
/// Unit 1 (English Is Important): Roleplay Lesson Two (RP02: Free Scene — Tell the World Why).
/// Subclasses Masters_PolishedCommunication_Roleplay_LessonTwo.
/// </summary>
public class Masters_EnglishIsImportant_Roleplay_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonTwo {

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
                tmp.text = "RP02 Free Scene — Tell the World Why";
            }
            if (lowerName.Contains("heading") || lowerName.Contains("branch") || textVal.Contains("ROLEPLAY") || textVal.Contains("BRANCH")) {
                tmp.text = "ROLEPLAY BRANCH (Stage)";
            }
        }
    }
}
