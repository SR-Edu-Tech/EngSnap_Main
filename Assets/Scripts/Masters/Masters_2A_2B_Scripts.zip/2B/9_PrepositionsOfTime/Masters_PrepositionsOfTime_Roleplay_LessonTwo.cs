using UnityEngine;

namespace EngSnap.Masters.Unit9 {

    /// <summary>
    /// Unit 9: Prepositions of Time - Roleplay Lesson Two (RP02: Free Scene — Give Me a Situation).
    /// Subclasses Masters_PolishedCommunication_Roleplay_LessonTwo and manages the 3 time idiom cards:
    /// Card A: In the nick of time
    /// Card B: It's high time
    /// Card C: Make up for lost time
    /// Each card has two required player turns: Turn 1 (Meaning), Turn 2 (Situation sentence).
    /// </summary>
    public class Masters_PrepositionsOfTime_Roleplay_LessonTwo : Masters_PolishedCommunication_Roleplay_LessonTwo {

        public void SetScenes(SceneData[] sceneData) {
            scenes = sceneData;
        }

        public SceneData[] GetScenes() {
            return scenes;
        }
    }
}
