using UnityEngine;

/// <summary>
/// Replaces TopicRegistry_BB1.
/// Central coordinator — lives on a persistent GameObject in the scene.
///
/// WIRING:
///   topicSelectionPanel → the panel showing all topic buttons
///   sharedUnitPanel     → the ONE shared SharedUnitPanelController in the scene
/// </summary>
public class TopicSelectorRegistry : MonoBehaviour
{
    [Header("Root Panels")]
    public GameObject              topicSelectionPanel;
    public SharedUnitPanelController sharedUnitPanel;

    /// <summary>Called by TopicSelectorButton when a topic button is clicked.</summary>
    public void OpenTopic(TopicData_BB2 topicData)
    {
        topicSelectionPanel.SetActive(false);
        sharedUnitPanel.Open(topicData);
    }

    /// <summary>Called by SharedUnitPanelController Back button.</summary>
    public void BackToTopicSelection()
    {
        sharedUnitPanel.Close();
        topicSelectionPanel.SetActive(true);
    }
}
