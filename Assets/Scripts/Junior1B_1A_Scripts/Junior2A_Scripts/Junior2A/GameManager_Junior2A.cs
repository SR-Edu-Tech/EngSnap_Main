using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

namespace Junior2A
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

    public class GameManager_Junior2A : MonoBehaviour
    {
        public static GameManager_Junior2A Instance;

        [Header("All 15 Lessons Data Configuration")]
        [SerializeField] LessonData[] _lessons;

        [Header("Current Tracker State")]
        [SerializeField] int _selectedLessonIndex = -1;
        [SerializeField] Topics _selectedTopicType;
        [SerializeField] int _currentSlideIndex = -1;
        [SerializeField] bool _isLesssonOpen = true;
        [SerializeField] TopicData _currentTopicData;
        [SerializeField] GameObject _next, _topicParent, _lessonParent, _globalBackButton, _globalMainBackButton;

        [Header("Audio Configurations")]
        [SerializeField] AudioSource _audioSource;
        [SerializeField] AudioSource _audioSourceSelection;
        [SerializeField] AudioClip _popClip, _wooshClip, _selectUnit, _selectTopic;

        void Awake() => Instance = this;

        void Start()
        {
            Application.targetFrameRate = 120;
            _isLesssonOpen = true;
            _selectedLessonIndex = _currentSlideIndex = -1;
            UnitAndTopicSelectionAudio(true);
            if (_globalMainBackButton != null) _globalMainBackButton.SetActive(true);
            if (_globalBackButton != null) _globalBackButton.SetActive(false);
        }

        public void SelectLesson(int lessonIndex)
        {
            _isLesssonOpen = false;
            _selectedLessonIndex = lessonIndex;

            if (_topicParent != null && _topicParent.transform.childCount > 0)
            {
                Transform container = _topicParent.transform.GetChild(0);
                if (container.childCount > 0 && container.GetChild(0).TryGetComponent(out RectTransform rt))
                {
                    rt.anchoredPosition = Vector3.zero;
                }
            }

            if (_selectedLessonIndex >= 0 && _selectedLessonIndex < _lessons.Length)
            {
                int _currentTopicIndex = 0;
                foreach (TopicData topic in _lessons[_selectedLessonIndex].Topics)
                {
                    Transform topicGrid = _topicParent.transform.GetChild(0).GetChild(0);
                    if (_currentTopicIndex < topicGrid.childCount)
                    {
                        Transform item = topicGrid.GetChild(_currentTopicIndex);
                        if (item.childCount > 1 && item.GetChild(1).TryGetComponent(out Image img))
                        {
                            img.enabled = topic.IsCompleted;
                        }
                    }
                    _currentTopicIndex++;
                }
            }
            if (_globalBackButton != null) _globalBackButton.SetActive(true);
            if (_globalMainBackButton != null) _globalMainBackButton.SetActive(false);
        }

        public void DebugMethod(string MSG) => Debug.Log(MSG);

        public void SelectTopic(int topicEnumIndex)
        {
            _selectedTopicType = (Topics)topicEnumIndex;
            _currentTopicData = GetTopicDataForCurrentLesson(_selectedTopicType);

            if (_currentTopicData != null && _currentTopicData.Slides != null && _currentTopicData.Slides.Length > 0)
            {
                _currentSlideIndex = 0;

                for (int i = 0; i < _currentTopicData.Slides.Length; i++)
                {
                    if (_currentTopicData.Slides[i] != null && _currentTopicData.Slides[i].TryGetComponent(out Interfaces_Junior2A slideInterface))
                    {
                        if (slideInterface.IsViewed) _currentSlideIndex = i + 1;
                        else break;
                    }
                    else break;
                }

                if (_currentSlideIndex >= _currentTopicData.Slides.Length) _currentSlideIndex = 0;
                ShowCurrentSlideOnly();
            }

            if (_globalBackButton != null) _globalBackButton.SetActive(true);
            if (_globalMainBackButton != null) _globalMainBackButton.SetActive(false);
        }

        public void NextSlide()
        {
            if (_currentTopicData == null || _currentTopicData.Slides == null) return;
            if (_currentSlideIndex >= _currentTopicData.Slides.Length) return;

            if (_currentSlideIndex >= 0 && _currentSlideIndex < _currentTopicData.Slides.Length)
            {
                if (_currentTopicData.Slides[_currentSlideIndex] != null)
                {
                    _currentTopicData.Slides[_currentSlideIndex].SetActive(false);
                }
            }

            _currentSlideIndex++;
            if (_currentSlideIndex >= _currentTopicData.Slides.Length) CompleteCurrentTopic();
            else ShowCurrentSlideOnly();
        }

        public void Back()
        {
            // 1. Hide Reward object if active
            if (_selectedLessonIndex >= 0 && _selectedLessonIndex < _lessons.Length)
            {
                if (_lessons[_selectedLessonIndex].Reward != null && _lessons[_selectedLessonIndex].Reward.activeInHierarchy)
                {
                    _lessons[_selectedLessonIndex].Reward.SetActive(false);
                    if (_globalBackButton != null) _globalBackButton.SetActive(true);
                }
            }

            // 2. Already at Lesson/Topic selection menu level
            if (_currentSlideIndex < 0)
            {
                if (_topicParent != null) _topicParent.SetActive(false);
                UnitAndTopicSelectionAudio(true);

                if (_lessonParent != null)
                {
                    if (_lessonParent.TryGetComponent(out RectTransform rt)) rt.anchoredPosition = Vector3.zero;

                    int totalChildren = _lessonParent.transform.childCount;
                    for (int i = 0; i < totalChildren; i++)
                    {
                        Transform child = _lessonParent.transform.GetChild(i);
                        if (child.TryGetComponent(out PopEffect_Junior2A popEffect))
                        {
                            popEffect.enabled = false;
                            popEffect.enabled = true;
                        }
                    }
                }

                _isLesssonOpen = true;
                if (_globalMainBackButton != null) _globalMainBackButton.SetActive(true);
                if (_globalBackButton != null) _globalBackButton.SetActive(false);
            }
            // 3. Inside a topic's active slides
            else
            {
                UnitAndTopicSelectionAudio(false);

                if (_topicParent != null && _topicParent.transform.childCount > 0)
                {
                    Transform container = _topicParent.transform.GetChild(0);
                    if (container.childCount > 0 && container.GetChild(0).TryGetComponent(out RectTransform rt))
                    {
                        rt.anchoredPosition = Vector3.zero;
                    }
                }

                // Turn off current slide safely if in range
                if (_currentTopicData != null && _currentTopicData.Slides != null)
                {
                    if (_currentSlideIndex >= 0 && _currentSlideIndex < _currentTopicData.Slides.Length)
                    {
                        if (_currentTopicData.Slides[_currentSlideIndex] != null)
                        {
                            _currentTopicData.Slides[_currentSlideIndex].SetActive(false);
                        }
                    }
                }

                _currentSlideIndex--;

                if (_currentSlideIndex >= 0)
                {
                    ShowCurrentSlideOnly();
                }
                else
                {
                    // Returned to topic menu from slide 0
                    Next(false);
                }
            }
        }

        public void Next(bool value)
        {
            if (_next != null)
            {
                _next.SetActive(value);
                if (value && _next.TryGetComponent(out PopEffect_Junior2A popEffect))
                {
                    popEffect.enabled = false;
                    popEffect.enabled = true;
                }
            }
        }

        void ShowCurrentSlideOnly()
        {
            if (_currentTopicData == null || _currentTopicData.Slides == null) return;

            for (int i = 0; i < _currentTopicData.Slides.Length; i++)
            {
                if (_currentTopicData.Slides[i] != null)
                {
                    _currentTopicData.Slides[i].SetActive(i == _currentSlideIndex);
                }
            }
        }

        public void UnitAndTopicSelectionAudio(bool isUnit)
        {
            if (_audioSourceSelection == null) return;
            _audioSourceSelection.clip = isUnit ? _selectUnit : _selectTopic;
            _audioSourceSelection.Play();
        }

        TopicData GetTopicDataForCurrentLesson(Topics expectedTopic)
        {
            if (_selectedLessonIndex < 0 || _selectedLessonIndex >= _lessons.Length) return null;

            foreach (TopicData topic in _lessons[_selectedLessonIndex].Topics)
            {
                if (topic.TopicType == expectedTopic) return topic;
            }
            return null;
        }

        void CompleteCurrentTopic()
        {
            if (_currentTopicData != null) _currentTopicData.IsCompleted = true;

            int _currentTopicIndex = 0;
            bool _allDone = true;

            if (_selectedLessonIndex >= 0 && _selectedLessonIndex < _lessons.Length)
            {
                foreach (TopicData topic in _lessons[_selectedLessonIndex].Topics)
                {
                    Transform topicGrid = _topicParent.transform.GetChild(0).GetChild(0);
                    if (_currentTopicIndex < topicGrid.childCount)
                    {
                        Transform item = topicGrid.GetChild(_currentTopicIndex);
                        if (item.childCount > 1 && item.GetChild(1).TryGetComponent(out Image img))
                        {
                            img.enabled = topic.IsCompleted;
                        }
                    }

                    if (!topic.IsCompleted) _allDone = false;
                    _currentTopicIndex++;
                }
            }

            _currentSlideIndex = -1;

            if (_allDone)
            {
                if (_selectedLessonIndex >= 0 && _selectedLessonIndex < _lessons.Length)
                {
                    if (_lessons[_selectedLessonIndex].Reward != null)
                    {
                        _lessons[_selectedLessonIndex].Reward.SetActive(true);
                    }
                }

                if (_globalBackButton != null) _globalBackButton.SetActive(false);
                if (_globalMainBackButton != null) _globalMainBackButton.SetActive(true);
            }
            else
            {
                UnitAndTopicSelectionAudio(false);
                if (_topicParent != null && _topicParent.transform.childCount > 0)
                {
                    Transform container = _topicParent.transform.GetChild(0);
                    if (container.childCount > 0 && container.GetChild(0).TryGetComponent(out RectTransform rt))
                    {
                        rt.anchoredPosition = Vector3.zero;
                    }
                }
            }
            Debug.Log($"Topic {_selectedTopicType} in Lesson {_selectedLessonIndex + 1} is completely viewed!");
        }

        public void Pop()
        {
            if (_audioSource == null || _popClip == null) return;
            _audioSource.clip = _popClip;
            _audioSource.Play();
        }

        public void Woosh()
        {
            if (_audioSource == null || _wooshClip == null) return;
            _audioSource.clip = _wooshClip;
            _audioSource.Play();
        }

        [Header("In-Game Custom Debug Console")]
        [SerializeField] TextMeshProUGUI consoleText;
        [SerializeField] int maxLines = 20;

        StringBuilder logBuilder = new StringBuilder();

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
}