using UnityEngine;

/// <summary>
/// Unit 2: Clear Confusion - Roleplay Lesson Two (RP02: Free Scene — Your Own Doubt, Politely).
/// Subclasses Unit 1's roleplay controller and provides clean data setters for the 3 situation cards.
/// </summary>
public class Masters_ClearConfusion_Roleplay_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonTwo {

    public void SetScenes(SceneData[] sceneData) {
        scenes = sceneData;
    }

    public SceneData[] GetScenes() {
        return scenes;
    }
}
