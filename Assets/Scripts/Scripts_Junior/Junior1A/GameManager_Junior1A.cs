using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum Topics
{
    Intro,
    LISTENING,
    READING,
    WRITING,
    SPEAKING,
    GAME,
    ROLEPLAY,
    QUIZ
}
[Serializable]
public class TopicData
{
    public Topics TopicType;
    public GameObject[] Slides;
    public bool IsCompleted;
}
[Serializable]
public class LessonData
{
    public int LessonNumber;
    public TopicData[] Topics;
    public GameObject Reward;
}

public class GameManager_Junior1A : MonoBehaviour
{
    public static GameManager_Junior1A Instance;

    [Header("All 15 Lessons Data Configuration")]
    [SerializeField] LessonData[] _lessons;

    [Header("Current Tracker State")]
    [SerializeField] int _selectedLessonIndex = -1;
    [SerializeField] Topics _selectedTopicType;
    [SerializeField] int _currentSlideIndex;
    [SerializeField] bool _isLesssonOpen = true;
    [SerializeField] TopicData _currentTopicData;
    [SerializeField] GameObject _next, _topicParent, _lessonParent, _wordSearch;

    [SerializeField] AudioSource _audioSource, _audioSourceSelection;
    [SerializeField] AudioClip _popClip, _wooshClip, _selectUnit, _selectTopic;
    void Awake() => Instance = this;
    void Start()
    {
        Application.targetFrameRate = 120;
        _isLesssonOpen = true;
        _selectedLessonIndex = _currentSlideIndex = -1;
        UnitAndTopicSelectionAudio(true);
    }
    public void SelectLesson(int lessonIndex)
    {
        _isLesssonOpen = false;
        _selectedLessonIndex = lessonIndex;
        _topicParent.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>().anchoredPosition = Vector3.zero;

        if (_selectedLessonIndex >= 0 && _selectedLessonIndex < _lessons.Length)
        {
            int _currentTopicIndex = 0;
            foreach (TopicData topic in _lessons[_selectedLessonIndex].Topics)
            {
                if (topic.IsCompleted)
                {
                    _topicParent.transform.GetChild(0).GetChild(0).GetChild(_currentTopicIndex).GetChild(1).GetComponent<Image>().enabled = true;
                    Debug.Log($"Completed Topic: {topic.TopicType}");
                }
                else _topicParent.transform.GetChild(0).GetChild(0).GetChild(_currentTopicIndex).GetChild(1).GetComponent<Image>().enabled = false;
                _currentTopicIndex++;
            }
        }
    }
    public void DebugMethod(string MSG) => Debug.Log(MSG);
    public void SelectTopic(int topicEnumIndex)
    {
        _selectedTopicType = (Topics)topicEnumIndex;
        _currentTopicData = GetTopicDataForCurrentLesson(_selectedTopicType);
        if (_currentTopicData != null && _currentTopicData.Slides.Length > 0)
        {
            _currentSlideIndex = 0;

            for (int i = 0; i < _currentTopicData.Slides.Length; i++)
            {
                Interfaces_Junior1A slideInterface = _currentTopicData.Slides[i].GetComponent<Interfaces_Junior1A>();
                if (slideInterface != null && slideInterface.IsViewed) _currentSlideIndex = i + 1;
                else break;
            }

            if (_currentSlideIndex >= _currentTopicData.Slides.Length) _currentSlideIndex = 0;
            ShowCurrentSlideOnly();
        }
    }

    public void NextSlide()
    {
        if (_currentSlideIndex > _currentTopicData.Slides.Length) return;
        _currentTopicData.Slides[_currentSlideIndex].SetActive(false);
        _currentSlideIndex++;
        if (_currentSlideIndex >= _currentTopicData.Slides.Length) CompleteCurrentTopic();
        else ShowCurrentSlideOnly();
    }
    public void Back()
    {
        if (_wordSearch.activeInHierarchy) _wordSearch.SetActive(false);
        else if (_isLesssonOpen)
        {
            Resources.UnloadUnusedAssets();
            SceneManager.LoadSceneAsync("mainScene");
        }
        if (_currentSlideIndex < 0)
        {
            _topicParent.SetActive(false);
            UnitAndTopicSelectionAudio(true);
            _lessonParent.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
            for (int i = 0; i < 15; i++)
            {
                _lessonParent.transform.GetChild(_lessonParent.transform.childCount - (1 + i)).GetComponent<PopEffect_Junior1A>().enabled = false;
                _lessonParent.transform.GetChild(_lessonParent.transform.childCount - (1 + i)).GetComponent<PopEffect_Junior1A>().enabled = true;
            }
            _isLesssonOpen = true;
        }
        else
        {
            UnitAndTopicSelectionAudio(false);
            _topicParent.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
            if (_currentTopicData.Slides.Length > 0) _currentTopicData.Slides[_currentSlideIndex].SetActive(false);
            _currentSlideIndex--;
            Next(false);
        }
    }
    public void Next(bool value)
    {
        _next.SetActive(value);
        _next.GetComponent<PopEffect_Junior1A>().enabled = true;
    }

    void ShowCurrentSlideOnly()
    {
        for (int i = 0; i < _currentTopicData.Slides.Length; i++)
        {
            _currentTopicData.Slides[i].SetActive(i == _currentSlideIndex);
        }
    }
    public void UnitAndTopicSelectionAudio(bool isUnit)
    {
        _audioSourceSelection.clip = isUnit ? _selectUnit : _selectTopic;
        _audioSourceSelection.Play();
    }
    TopicData GetTopicDataForCurrentLesson(Topics expectedTopic)
    {
        foreach (TopicData topic in _lessons[_selectedLessonIndex].Topics)
        {
            if (topic.TopicType == expectedTopic) return topic;
        }
        return null;
    }

    void CompleteCurrentTopic()
    {
        _currentTopicData.IsCompleted = true;
        int _currentTopicIndex = 0;
        bool _allDone = true;
        foreach (TopicData topic in _lessons[_selectedLessonIndex].Topics)
        {
            if (topic.IsCompleted) _topicParent.transform.GetChild(0).GetChild(0).GetChild(_currentTopicIndex).GetChild(1).GetComponent<Image>().enabled = true;
            else
            {
                _topicParent.transform.GetChild(0).GetChild(0).GetChild(_currentTopicIndex).GetChild(1).GetComponent<Image>().enabled = false;
                _allDone = false;
            }
            _currentTopicIndex++;
        }
        _currentSlideIndex = -1;
        if (_allDone) _lessons[_selectedLessonIndex].Reward.SetActive(true);
        Debug.Log($"Topic {_selectedTopicType} in Lesson {_selectedLessonIndex + 1} is completely viewed!");
    }

    public void Pop()
    {
        _audioSource.clip = _popClip;
        _audioSource.Play();
    }
    public void Woosh()
    {
        _audioSource.clip = _wooshClip;
        _audioSource.Play();
    }



    [SerializeField] TextMeshProUGUI consoleText;
    [SerializeField] int maxLines = 20;

    StringBuilder logBuilder = new StringBuilder();

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        logBuilder.AppendLine($"[{type}] {logString}");

        var lines = logBuilder.ToString().Split('\n');
        if (lines.Length > maxLines)
        {
            logBuilder.Clear();
            for (int i = lines.Length - maxLines; i < lines.Length; i++)
                logBuilder.AppendLine(lines[i]);
        }

        consoleText.text = logBuilder.ToString();
    }
}
