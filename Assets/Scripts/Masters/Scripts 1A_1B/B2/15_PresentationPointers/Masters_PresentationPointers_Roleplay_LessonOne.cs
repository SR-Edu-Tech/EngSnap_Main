using UnityEngine;

/// <summary>
/// Roleplay Lesson 1 for Unit 15 Presentation Pointers.
/// Solo presentation walkthrough guided by the narrator (Prabhat) where student (Riya, Neerja +10Hz) selects the correct speech step.
/// Hides NPC model and cloud since no NPC is needed for this roleplay.
/// </summary>
public class Masters_PresentationPointers_Roleplay_LessonOne : Masters_StartingConversationWithAStranger_Roleplay_LessonOne {

    protected override void Start() {
        base.Start();
        if (npcCloud != null) npcCloud.SetActive(false);
        Transform npcChar = transform.Find("NpcAndStudent/NPCCharacter");
        if (npcChar != null) npcChar.gameObject.SetActive(false);
        if (npcAndStudentGameObject != null) {
            Transform childChar = npcAndStudentGameObject.transform.Find("NPCCharacter");
            if (childChar != null) childChar.gameObject.SetActive(false);
        }
    }
}
