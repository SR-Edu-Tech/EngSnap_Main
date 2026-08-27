using UnityEngine;
using UnityEngine.UI;

public class TopicToUnitPanel : MonoBehaviour
{
    [Header("Panels")]
    [Tooltip("The panel containing topics, which will be hidden when clicked.")]
    [SerializeField] private GameObject topicPanel;

    [Tooltip("The panel containing units, which will be shown when clicked.")]
    [SerializeField] private GameObject unitsmainPanel;
    [SerializeField] private GameObject unitsPanel;

    private Button button;

    private void Start()
    {
        // Automatically try to find the Button component on this GameObject
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(TransitionToUnitsPanel);
        }
    }

    /// <summary>
    /// Transitions from the topic panel to the units panel.
    /// </summary>
    public void TransitionToUnitsPanel()
    {
        if (topicPanel == null && unitsmainPanel == null && unitsPanel == null)
        {
            Debug.LogWarning("TopicToUnitPanel: Both topicPanel and unitsPanel references are missing!", this);
            return;
        }

        if (topicPanel != null)
        {
            topicPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("TopicToUnitPanel: Topic Panel reference is not assigned.", this);
        }

        if (unitsPanel != null && unitsmainPanel != null)
        {
            unitsmainPanel.SetActive(true);
            unitsPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("TopicToUnitPanel: Units Panel reference is not assigned.", this);
        }
    }
}

