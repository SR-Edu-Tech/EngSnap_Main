using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Replaces TopicButton_BB1.
/// Attach to each topic button in the Topic Selection Panel.
///
/// WIRING IN INSPECTOR:
///   topicData → drag the scene GameObject that has TopicData_BB2 on it
///               (e.g. drag "EverydayGreetings" from Hierarchy)
///   registry  → drag the GameObject that has TopicSelectorRegistry on it
/// </summary>
[RequireComponent(typeof(Button))]
public class TopicSelectorButton : MonoBehaviour
{
    [Header("Topic")]
    public TopicData_BB2      topicData;   // drag scene GO with TopicData_BB2 component

    [Header("Registry")]
    public TopicSelectorRegistry registry;

    void Awake()
    {
        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClicked);
    }

    private void OnClicked()
    {
        if (registry != null && topicData != null)
            registry.OpenTopic(topicData);
        else
            Debug.LogWarning($"TopicSelectorButton on [{gameObject.name}]: registry or topicData not assigned.");
    }
}
