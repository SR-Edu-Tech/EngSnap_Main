using UnityEngine;

/// <summary>
/// Central coordinator. Lives on a persistent GameObject in the scene.
/// Holds references to every TopicPanel and drives show/hide logic.
///
/// HIERARCHY expected:
///   TopicRegistry (this script)
///   TopicSelectionPanel          ← always visible at start
///   Topic_Greetings_Panel        ← UnitPanelController on this
///       UnitButton_Intro         ← UnitButton_BB1 on this
///       UnitButton_Listening     ← UnitButton_BB1 on this
///       ...
///   Topic_Numbers_Panel          ← UnitPanelController on this
///       ...
/// </summary>
public class TopicRegistry_BB1 : MonoBehaviour
{
    [Header("Root Panels")]
    public GameObject topicSelectionPanel;   // The panel showing topic buttons

    // ── called by each topic button in the Topic Selection panel ──────────
    public void OpenTopic(UnitPanelController_BB1 panel)
    {
        topicSelectionPanel.SetActive(false);
        panel.Open();
    }

    public void BackToTopicSelection(UnitPanelController_BB1 panel)
    {
        panel.Close();
        topicSelectionPanel.SetActive(true);
    }
}
