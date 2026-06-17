using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Refactored lesson script for Unit 12 – Dialogue (L02).
/// * No direct panel activation.
/// * Local next button only advances internal dialogue.
/// * Global GameManager arrow is hidden during the lesson and shown when finished.
/// * Listener handling is simplified by resetting the ButtonClickedEvent.
/// </summary>
public class U12_L02_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    #region Data structures
    [Serializable]
    public struct DialogueExchange
    {
        [TextArea(2, 5)] public string TeacherText;
        [TextArea(2, 5)] public string SamText;
        public AudioClip TeacherClip;
        public AudioClip SamClip;
    }
    #endregion

    #region UI refs
    [Header("UI Text References")]
    [SerializeField] private TextMeshProUGUI _teacherTextBox;
    [SerializeField] private TextMeshProUGUI _samTextBox;
    [Header("Bubbles")]
    [SerializeField] private GameObject _teacherBubbleObj;
    [SerializeField] private GameObject _samBubbleObj;
    [Header("Navigation")]
    [SerializeField] private Button _localNextButton; // local next button only
    #endregion

    #region Audio
    [Header("Audio")]
    [SerializeField] private AudioSource _audioSource;
    #endregion

    #region Dialogue Data
    [Header("Dialogues (6 items)")]
    [SerializeField] private DialogueExchange[] _dialogues;
    #endregion

    private int _currentStep = 0;
    private bool _waitingForClick = false;
    private bool _isViewed = false;
    private Coroutine _timelineCoroutine;

    public bool IsViewed => _isViewed;

    #region State machine enum (internal)
    private enum LessonState { Idle, Running, Waiting, Completed }
    private LessonState _state = LessonState.Idle;
    #endregion

    private void Awake()
    {
        // Ensure a clean listener set (clears any persistent listeners).
        if (_localNextButton != null)
        {
            _localNextButton.onClick = new Button.ButtonClickedEvent();
            _localNextButton.onClick.AddListener(OnLocalNextClicked);
            _localNextButton.gameObject.SetActive(false);
        }
        // Hide the global next arrow while this lesson is active.
        GameManager_Junior1A.Instance?.Next(false);
    }

    private void Start()
    {
        _state = LessonState.Running;
        _timelineCoroutine = StartCoroutine(RunTimeline());
    }

    private IEnumerator RunTimeline()
    {
        if (_dialogues == null || _dialogues.Length == 0)
        {
            Debug.LogError("❌ No dialogue entries assigned.");
            yield break;
        }

        while (_currentStep < _dialogues.Length)
        {
            DialogueExchange cur = _dialogues[_currentStep];

            // Teacher line
            _samTextBox.text = string.Empty;
            if (_samBubbleObj) _samBubbleObj.SetActive(false);
            _teacherTextBox.text = cur.TeacherText;
            if (_teacherBubbleObj) _teacherBubbleObj.SetActive(true);
            if (_audioSource && cur.TeacherClip)
            {
                _audioSource.clip = cur.TeacherClip;
                _audioSource.Play();
                yield return new WaitForSeconds(cur.TeacherClip.length);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }
            yield return new WaitForSeconds(0.4f);

            // Sam line
            _samTextBox.text = cur.SamText;
            if (_samBubbleObj) _samBubbleObj.SetActive(true);
            if (_audioSource && cur.SamClip)
            {
                _audioSource.clip = cur.SamClip;
                _audioSource.Play();
                yield return new WaitForSeconds(cur.SamClip.length);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            // Wait for player to press local next.
            yield return WaitForPlayerClick();
            _currentStep++;
        }

        // Lesson finished – tell the GameManager to show its Next arrow and advance.
        _isViewed = true;
        _state = LessonState.Completed;
        GameManager_Junior1A.Instance?.Next(true);
    }

    private IEnumerator WaitForPlayerClick()
    {
        _waitingForClick = true;
        _state = LessonState.Waiting;
        if (_localNextButton) _localNextButton.gameObject.SetActive(true);
        while (_waitingForClick) yield return null;
        if (_localNextButton) _localNextButton.gameObject.SetActive(false);
        _state = LessonState.Running;
    }

    private void OnLocalNextClicked()
    {
        _waitingForClick = false;
    }

    // PUBLIC API -----------------------------------------------------------
    public void Repeat()
    {
        // Stop current routine and reset.
        if (_timelineCoroutine != null) StopCoroutine(_timelineCoroutine);
        _currentStep = 0;
        _isViewed = false;
        GameManager_Junior1A.Instance?.Next(false);
        _timelineCoroutine = StartCoroutine(RunTimeline());
    }

    public void PlayAudio(int index) { /* not used in this lesson */ }
    public void Slow(TextMeshProUGUI text) { /* optional speed toggle */ }
}