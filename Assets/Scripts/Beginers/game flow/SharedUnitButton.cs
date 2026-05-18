using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Replaces UnitButton_BB1.
/// Sits on each unit button inside the ONE shared Unit Panel.
/// No direct content GO reference needed — the panel resolves it from TopicData_BB2.
///
/// WIRING IN INSPECTOR:
///   unitType        → pick the type (Intro, Listening, Reading...)
///   button          → the Button component on this GO
///   tmpLabel        → TMP text (optional)
///   label           → Legacy text (optional)
///   completionBadge → tick/star child GO (optional)
///   displayName     → text shown on the button
/// </summary>
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

        if (completionBadge != null) completionBadge.SetActive(false);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);
    }

    /// <summary>
    /// Called by SharedUnitPanelController every time the panel opens for a topic.
    /// Wires the panel and refreshes the badge for the active topic.
    /// </summary>
    public void Initialise(SharedUnitPanelController panel, TopicData_BB2 topicData)
    {
        _panel = panel;
        RefreshBadge(topicData);
    }

    private void RefreshBadge(TopicData_BB2 topicData)
    {
        if (completionBadge == null || topicData == null) return;
        string key      = topicData.GetSaveKey(unitType);
        bool completed  = PlayerPrefs.GetInt(key, 0) == 1;
        completionBadge.SetActive(completed);
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

    private void OnClicked()
    {
        _panel?.StartUnit(this);
    }
}
