using UnityEngine;

/// <summary>
/// Root controller for the Magic Words gameplay.
/// Attach to the root content GameObject.
/// Implements IUnitCompletable — SharedUnitPanelController calls OnUnitStart.
///
/// HIERARCHY:
///   MagicWords (this script + IUnitCompletable entry point)
///     ├── Screen1_MagicWordQuiz        (MagicWordQuiz_MagicWords_BB1)
///     └── Screen2_MagicWordConversation (MagicWordConversation_MagicWords_BB1)
///
/// FLOW:
///   OnUnitStart → passes panel+button to Screen1
///   Screen1 completes → activates Screen2 and passes panel+button
///   Screen2 completes → calls panel.UnitFinished(unitButton)
/// </summary>
public class MagicWordsManager_MagicWords_BB1 : MonoBehaviour, IUnitCompletable
{
    [Header("Sub Screens")]
    public MagicWordQuiz_MagicWords_BB1         screen1Quiz;
    public MagicWordConversation_MagicWords_BB1 screen2Conversation;

    // ── IUnitCompletable ──────────────────────────────────────────────────
    [HideInInspector] public SharedUnitPanelController panel;
    [HideInInspector] public SharedUnitButton          unitButton;

    public void OnUnitStart(SharedUnitPanelController sharedPanel, SharedUnitButton sharedButton)
    {
        panel      = sharedPanel;
        unitButton = sharedButton;
    }

    // ── Unity ─────────────────────────────────────────────────────────────
    void OnEnable()
    {
        // Always start fresh from Screen 1
        if (screen2Conversation != null) screen2Conversation.gameObject.SetActive(false);

        if (screen1Quiz != null)
        {
            // Pass references so Screen 1 → Screen 2 handoff works
            screen1Quiz.panel            = panel;
            screen1Quiz.unitButton       = unitButton;
            screen1Quiz.conversationScreen = screen2Conversation;
            screen1Quiz.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[MagicWordsManager] screen1Quiz not assigned!");
        }
    }

    void OnDisable()
    {
        if (screen1Quiz         != null) screen1Quiz.gameObject.SetActive(false);
        if (screen2Conversation != null) screen2Conversation.gameObject.SetActive(false);
    }

    public void UnitFinished(SharedUnitButton button)
    {
        // Pass through to panel, which handles next steps (unlocking next unit, etc.)
        if (panel != null)
            panel.UnitFinished(button);
        else
            Debug.LogWarning("[MagicWordsManager] UnitFinished called but panel reference is null!");
    }
}
