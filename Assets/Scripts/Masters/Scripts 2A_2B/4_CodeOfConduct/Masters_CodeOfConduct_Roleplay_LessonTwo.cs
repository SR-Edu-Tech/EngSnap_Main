using UnityEngine;

/// <summary>
/// Unit 4: Code of Conduct - Roleplay Lesson Two (RP02: Free Scene — Thank, Apologise, Praise).
/// Subclasses Unit 1's roleplay controller and provides clean data setters for the 3 situation cards.
/// </summary>
public class Masters_CodeOfConduct_Roleplay_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonTwo {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Roleplay;
    }

    public void SetScenes(SceneData[] sceneData) {
        scenes = sceneData;
    }

    public SceneData[] GetScenes() {
        return scenes;
    }
}
