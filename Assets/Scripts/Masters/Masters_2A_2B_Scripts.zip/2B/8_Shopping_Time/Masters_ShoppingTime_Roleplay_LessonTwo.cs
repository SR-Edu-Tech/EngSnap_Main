using UnityEngine;

/// <summary>
/// Unit 8: Shopping Time - Roleplay Lesson Two (RP02: Free Scene — Let's Go Shopping).
/// Directly subclasses 2A Unit 1 Polished Communication Roleplay Lesson Two controller
/// and provides clean data setters for the 3 shop cards (Boutique, Grocery Store, Pet Shop).
/// </summary>
public class Masters_ShoppingTime_Roleplay_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonTwo {

    public void SetScenes(SceneData[] sceneData) {
        scenes = sceneData;
    }

    public SceneData[] GetScenes() {
        return scenes;
    }
}
