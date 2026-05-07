using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

// TopicSelectionManager — OPTIONAL in v5.
//
// The word search game now starts directly without topic selection.
// This component is kept for projects that STILL want a topic picker,
// but it is no longer required. If you do not need it, simply remove it
// from your scene — WordSearchManager works standalone.
//
// If you DO use it, it still works the same way:
//   • Assign topics in the Inspector.
//   • Tap a topic row → loads words into WordSearchManager → shows game.
//   • WordSearchManager handles its own progress/finish panel.
//   • The Continue button on the finish panel calls ReturnToTopicPanel here.
//
public class Masters_WordMatchTopicSelectionManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject topicPanel;
    public GameObject wordSearchPanel;

    [Header("Topics")]
    public List<Masters_WordTopicData> topics = new List<Masters_WordTopicData>();

    [Header("UI References")]
    public Transform       topicListParent;
    public Masters_TopicItemUI     topicItemPrefab;
    public TextMeshProUGUI headerText;

    [Header("Game")]
    public Masters_WordSearchManager wordSearchManager;

    private List<Masters_TopicItemUI> topicItems  = new List<Masters_TopicItemUI>();
    private Masters_WordTopicData     currentTopic;

    static string DoneKey(Masters_WordTopicData d) => "wsdone_" + d.topicName;

    void Awake()
    {
        if (wordSearchManager != null)
            wordSearchManager.OnContinueClicked = ReturnToTopicPanel;
    }

    void Start()
    {
        BuildTopicList();
        ShowTopicPanel();
    }

    void BuildTopicList()
    {
        foreach (Transform t in topicListParent) Destroy(t.gameObject);
        topicItems.Clear();

        foreach (Masters_WordTopicData topic in topics)
        {
            Masters_TopicItemUI item = Instantiate(topicItemPrefab, topicListParent);
            item.Init(topic, this);
            topicItems.Add(item);
        }
    }

    public void SelectTopic(Masters_WordTopicData topic)
    {
        if (topic == null || topic.words == null || topic.words.Count == 0)
        {
            Debug.LogWarning("[TopicSelection] Topic has no words: " + (topic != null ? topic.topicName : "null"));
            return;
        }

        currentTopic = topic;
        wordSearchManager.LoadTopic(topic);
        ShowWordSearchPanel();
    }

    public void ReturnToTopicPanel()
    {
        if (currentTopic != null) MarkTopicDone(currentTopic);

        foreach (var item in topicItems) item.Refresh();
        ShowTopicPanel();
    }

    public void ResetTopic(Masters_WordTopicData topic)
    {
        if (topic == null) return;
        PlayerPrefs.DeleteKey(DoneKey(topic));
        PlayerPrefs.Save();
    }

    public static bool IsTopicDone(Masters_WordTopicData topic)
        => topic != null && PlayerPrefs.GetInt(DoneKey(topic), 0) == 1;

    void MarkTopicDone(Masters_WordTopicData topic)
    {
        if (topic == null) return;
        PlayerPrefs.SetInt(DoneKey(topic), 1);
        PlayerPrefs.Save();
    }

    void ShowTopicPanel()
    {
        if (topicPanel      != null) topicPanel.SetActive(true);
        if (wordSearchPanel != null) wordSearchPanel.SetActive(false);
    }

    void ShowWordSearchPanel()
    {
        if (topicPanel      != null) topicPanel.SetActive(false);
        if (wordSearchPanel != null) wordSearchPanel.SetActive(true);
    }
}