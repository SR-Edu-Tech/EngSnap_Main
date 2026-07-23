using UnityEngine;

/// <summary>
/// Reading Lesson 1 for Unit 15 Presentation Pointers.
/// Subclasses Unit 14 Reading Lesson 1 (Vertical Slate Ordering) but bypasses Scene Selection UI so students directly order the 5 presentation steps.
/// </summary>
public class Masters_PresentationPointers_Reading_LessonOne : Masters_RealLifeInteractions_Reading_LessonOne {

    protected override void Start() {
        base.Start();

        // Hide scene selector and directly show ordering panel for Scene 0
        if (sceneSelectionPanel != null) sceneSelectionPanel.SetActive(false);
        if (orderingPanel != null) orderingPanel.SetActive(true);

        if (scenes != null && scenes.Length > 0) {
            OnSceneSelected(0);
        }
    }

    protected override void LoadCurrentPage() {
        if (scenes == null || activeSceneIndex < 0 || activeSceneIndex >= scenes.Length) return;
        SceneData currentScene = scenes[activeSceneIndex];

        if (activePageIndex >= currentScene.pages.Length) {
            // Lesson completed directly! Bypass scene selection screen and trigger Next Button.
            currentScene.isCompleted = true;
            if (orderingPanel != null) orderingPanel.SetActive(false);
            if (sceneSelectionPanel != null) sceneSelectionPanel.SetActive(false);

            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            return;
        }

        base.LoadCurrentPage();
        if (sceneSelectionPanel != null) sceneSelectionPanel.SetActive(false);
    }
}
