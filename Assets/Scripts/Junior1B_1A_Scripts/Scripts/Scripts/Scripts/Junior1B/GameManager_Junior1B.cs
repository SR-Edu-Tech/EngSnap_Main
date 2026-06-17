using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameManager_Junior1B : MonoBehaviour
{
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

    public static GameManager_Junior1B Instance;

    [Header("All 14 Lessons Data Configuration")]
    [SerializeField] private LessonData[] _lessons;

    [Header("Current Tracker State")]
    [SerializeField] private int _selectedLessonIndex = -1;
    [SerializeField] private Topics _selectedTopicType;
    [SerializeField] private int _currentSlideIndex;
    [SerializeField] private bool _isLesssonOpen = true;
    [SerializeField] private TopicData _currentTopicData;
    [SerializeField] private GameObject _next, _topicParent, _lessonParent, _globalBackButton, _globalMainBackButton;

    [SerializeField] private AudioSource _audioSource, _audioSourceSelection;
    [SerializeField] private AudioClip _popClip, _wooshClip, _selectUnit, _selectTopic;

    void Awake() => Instance = this;

    void Start()
    {
        Application.targetFrameRate = 120;
        _isLesssonOpen = true;
        _selectedLessonIndex = _currentSlideIndex = -1;
        UnitAndTopicSelectionAudio(true);
        _globalMainBackButton.SetActive(true);
        _globalBackButton.SetActive(false);
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
        _globalBackButton.SetActive(true);
        _globalMainBackButton.SetActive(false);
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
                Interfaces_Junior1B slideInterface = _currentTopicData.Slides[i].GetComponent<Interfaces_Junior1B>();
                if (slideInterface != null && slideInterface.IsViewed) _currentSlideIndex = i + 1;
                else break;
            }

            if (_currentSlideIndex >= _currentTopicData.Slides.Length) _currentSlideIndex = 0;
            ShowCurrentSlideOnly();
        }
        _globalBackButton.SetActive(true);
        _globalMainBackButton.SetActive(false);
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
        // If reward is showing, hide it and stop — don't fall through
        if (_selectedLessonIndex >= 0 && _selectedLessonIndex < _lessons.Length
            && _lessons[_selectedLessonIndex].Reward != null
            && _lessons[_selectedLessonIndex].Reward.activeInHierarchy)
        {
            _lessons[_selectedLessonIndex].Reward.SetActive(false);
            _globalBackButton.SetActive(true);
            return;
        }

        // No topic selected yet or slide index is before the start — go back to lesson list
        if (_currentSlideIndex < 0 || _currentTopicData == null)
        {
            _topicParent.SetActive(false);
            UnitAndTopicSelectionAudio(true);
            _lessonParent.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
            for (int i = 0; i < _lessonParent.transform.childCount; i++)
            {
                var pop = _lessonParent.transform.GetChild(_lessonParent.transform.childCount - (1 + i))
                              .GetComponent<Popeffect_Junior1B>();
                if (pop != null) { pop.enabled = false; pop.enabled = true; }
            }
            _isLesssonOpen = true;
            _globalMainBackButton.SetActive(true);
            _globalBackButton.SetActive(false);
        }
        else
        {
            UnitAndTopicSelectionAudio(false);
            _topicParent.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
            if (_currentTopicData.Slides.Length > 0)
                _currentTopicData.Slides[_currentSlideIndex].SetActive(false);
            _currentSlideIndex--;
            Next(false);
        }
    }

    public void Next(bool value)
    {
        _next.SetActive(value);
        _next.GetComponent<Popeffect_Junior1B>().enabled = true;
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
        if (_allDone)
        {
            _lessons[_selectedLessonIndex].Reward.SetActive(true);
            _globalBackButton.SetActive(false);
            _globalMainBackButton.SetActive(true);
        }
        else
        {
            UnitAndTopicSelectionAudio(false);
            _topicParent.transform.GetChild(0).GetChild(0).GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
        }
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

    [SerializeField] private TextMeshProUGUI consoleText;
    [SerializeField] private int maxLines = 20;

    private StringBuilder logBuilder = new StringBuilder();

    void OnEnable() => Application.logMessageReceived += HandleLog;
    void OnDisable() => Application.logMessageReceived -= HandleLog;

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

        if (consoleText != null) consoleText.text = logBuilder.ToString();
    }
}