using UnityEngine;
using UnityEngine.UI;
using TMPro;

// TopicItemUI — One row in the topic selection list.
//
// PREFAB SETUP:
//   Root: Button + Image (white/light background)
//     ├── TopicNameText   : Text  (left-aligned)
//     ├── DoneLabel       : Text  ("done" in gray italic — hidden by default)
//     └── ResetButton     : Button (circular arrow icon — hidden by default)
//
public class TopicItemUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI  topicNameText;
    public TextMeshProUGUI   doneLabel;        // shows "done" when topic is completed
    public Button resetButton;      // circular-arrow button (visible when done)
    public Button selectButton;     // the row itself (whole button)

    private WordTopicData data;
    private TopicSelectionManager manager;

    public void Init(WordTopicData topicData, TopicSelectionManager mgr)
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
        bool isDone = TopicSelectionManager.IsTopicDone(data);

        if (doneLabel  != null) doneLabel.gameObject.SetActive(isDone);
        if (resetButton != null) resetButton.gameObject.SetActive(isDone);
    }

    public WordTopicData Data => data;
}
