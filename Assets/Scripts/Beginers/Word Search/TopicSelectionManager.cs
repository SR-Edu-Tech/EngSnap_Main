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
public class TopicSelectionManager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject topicPanel;
    public GameObject wordSearchPanel;

    [Header("Topics")]
    public List<WordTopicData> topics = new List<WordTopicData>();

    [Header("UI References")]
    public Transform       topicListParent;
    public TopicItemUI     topicItemPrefab;
    public TextMeshProUGUI headerText;

    [Header("Game")]
    public WordSearchManager wordSearchManager;

    private List<TopicItemUI> topicItems  = new List<TopicItemUI>();
    private WordTopicData     currentTopic;

    static string DoneKey(WordTopicData d) => "wsdone_" + d.topicName;

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

        foreach (WordTopicData topic in topics)
        {
            TopicItemUI item = Instantiate(topicItemPrefab, topicListParent);
            item.Init(topic, this);
            topicItems.Add(item);
        }
    }

    public void SelectTopic(WordTopicData topic)
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

    public void ResetTopic(WordTopicData topic)
    {
        if (topic == null) return;
        PlayerPrefs.DeleteKey(DoneKey(topic));
        PlayerPrefs.Save();
    }

    public static bool IsTopicDone(WordTopicData topic)
        => topic != null && PlayerPrefs.GetInt(DoneKey(topic), 0) == 1;

    void MarkTopicDone(WordTopicData topic)
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