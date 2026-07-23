using UnityEngine;

/// <summary>
/// Unit 3: Beyond The Horizon - Roleplay Lesson Two (RP02: Free Scene — Guide Your Blindfolded Friend).
/// Subclasses Unit 1's roleplay controller and provides clean data setters.
/// </summary>
public class Masters_BeyondTheHorizon_Roleplay_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonTwo {

    public void SetScenes(SceneData[] sceneData) {
        scenes = sceneData;
    }

    public SceneData[] GetScenes() {
        return scenes;
    }
}
