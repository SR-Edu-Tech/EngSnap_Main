using UnityEngine;
using UnityEngine.UI;
using TMPro;

// TopicItemUI_S1A — One row in the topic selection list.
//
// PREFAB SETUP:
//   Root: Button + Image (white/light background)
//     ├── TopicNameText   : Text  (left-aligned)
//     ├── DoneLabel       : Text  ("done" in gray italic — hidden by default)
//     └── ResetButton     : Button (circular arrow icon — hidden by default)
//
public class TopicItemUI_S1A : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI  topicNameText;
    public TextMeshProUGUI   doneLabel;        // shows "done" when topic is completed
    public Button resetButton;      // circular-arrow button (visible when done)
    public Button selectButton;     // the row itself (whole button)

    private WordTopicData_S1A data;
    private TopicSelectionManager_S1A manager;

    public void Init(WordTopicData_S1A topicData, TopicSelectionManager_S1A mgr)
    {
        data    = topicData;
        manager = mgr;

        topicNameText.text = topicData.topicName;

        Refresh();

        // Clicking the row opens the word search
        if (selectButton != null)
            selectButton.onClick.AddListener(() => manager.SelectTopic(data));

        // Clicking reset clears the saved progress for this topic
        if (resetButton != null)
            resetButton.onClick.AddListener(() =>
            {
                manager.ResetTopic(data);
                Refresh();
            });
    }

    // Call this to sync the "done / reset" state from PlayerPrefs
    public void Refresh()
    {
        bool isDone = TopicSelectionManager_S1A.IsTopicDone(data);

        if (doneLabel  != null) doneLabel.gameObject.SetActive(isDone);
        if (resetButton != null) resetButton.gameObject.SetActive(isDone);
    }

    public WordTopicData_S1A Data => data;
}
