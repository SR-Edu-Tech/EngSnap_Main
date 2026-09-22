using UnityEngine;

/// <summary>
/// Unit 5: Doctor Need Your Help - Roleplay Lesson Two (RP02: Free Scene — Three Ways to Ask for Help).
/// Directly subclasses 2A Unit 1 Polished Communication Roleplay Lesson Two controller
/// and provides clean data setters for the 3 scene cards without modifying any 2A code.
/// </summary>
public class Masters_DoctorNeedYourHelp_Roleplay_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonTwo {

    public void SetScenes(SceneData[] sceneData) {
        scenes = sceneData;
    }

    public SceneData[] GetScenes() {
        return scenes;
    }
}
