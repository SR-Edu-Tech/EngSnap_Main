using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Sits on each unit button inside a topic panel.
/// Drag the matching hierarchy GameObject (Intro, Listening, etc.) into unitGameObject.
/// Call panel.UnitFinished(this) from the unit script when the activity ends.
/// </summary>
public class UnitButton_BB1 : MonoBehaviour
{
    [Header("Unit To Launch")]
    public GameObject unitGameObject;        // The screen GameObject to show (inactive by default)


    [Header("Save ID (UNIQUE)")]
    public string unitID;
    [Header("UI References")]
    public Button button;
    public Text label;                       // Legacy Text (optional)
    public TMP_Text tmpLabel;                // TMP Text (optional)
    public GameObject completionBadge;       // Tick/star shown after completion (optional)

    [Header("Config")]
    public string displayName;               // Set in Inspector; shown on the button label

    private UnitPanelController_BB1 panel;

    void Awake()
    {
        panel = GetComponentInParent<UnitPanelController_BB1>(true);

        if (label != null)    label.text    = displayName;
        if (tmpLabel != null) tmpLabel.text = displayName;

        if (completionBadge != null) completionBadge.SetActive(false);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClicked);


        if (!string.IsNullOrEmpty(unitID))
{
    int isCompleted = PlayerPrefs.GetInt(unitID, 0);

    if (isCompleted == 1 && completionBadge != null)
    {
        completionBadge.SetActive(true);
    }
}
    }

    private void OnClicked()
    {
        if (panel != null)
            panel.StartUnit(this);
    }

    /// <summary>Show the completion badge on this button.</summary>
    public void MarkCompleted()
    {
        if (completionBadge != null)
            completionBadge.SetActive(true);

    if (!string.IsNullOrEmpty(unitID))
    {
        PlayerPrefs.SetInt(unitID, 1);
        PlayerPrefs.Save();
    }
            
    }
}
