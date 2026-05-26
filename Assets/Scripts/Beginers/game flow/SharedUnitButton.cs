using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SharedUnitButton : MonoBehaviour
{
    [Header("Unit Type")]
    public UnitType_BB1 unitType;

    [Header("UI References")]
    public Button     button;
    public Text       label;
    public TMP_Text   tmpLabel;
    public GameObject completionBadge;

    [Header("Config")]
    public string displayName;

    private SharedUnitPanelController _panel;

    void Awake()
    {
        if (label    != null) label.text    = displayName;
        if (tmpLabel != null) tmpLabel.text = displayName;

        // Badge always starts hidden — Initialise() will set correct state per topic
        if (completionBadge != null) completionBadge.SetActive(false);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);
    }

    /// <summary>
    /// Called every time the panel opens for a topic (including topic switches).
    /// Always resets badge first, then reads the correct save key for THIS topic.
    /// </summary>
    public void Initialise(SharedUnitPanelController panel, TopicData_BB2 topicData)
    {
        _panel = panel;

        // Always reset badge to hidden first — avoids bleed-over from a previous topic
        if (completionBadge != null) completionBadge.SetActive(false);

        // Only show badge if this unitType actually exists in the current topic
        // AND has been saved as complete for this specific topic
        if (topicData != null && TopicHasThisUnit(topicData))
        {
            string key     = topicData.GetSaveKey(unitType);
            bool completed = PlayerPrefs.GetInt(key, 0) == 1;
            if (completionBadge != null) completionBadge.SetActive(completed);
        }
    }

    /// <summary>Returns true if this button's unitType is present in the topic's entries.</summary>
    private bool TopicHasThisUnit(TopicData_BB2 topicData)
    {
        if (topicData.unitEntries == null) return false;
        foreach (var entry in topicData.unitEntries)
            if (entry.unitType == unitType) return true;
        return false;
    }

    /// <summary>Shows badge and saves completion for the given topic.</summary>
    public void MarkCompleted(TopicData_BB2 topicData)
    {
        if (completionBadge != null) completionBadge.SetActive(true);

        if (topicData != null)
        {
            PlayerPrefs.SetInt(topicData.GetSaveKey(unitType), 1);
            PlayerPrefs.Save();
        }
    }

    private void OnClicked() => _panel?.StartUnit(this);
}