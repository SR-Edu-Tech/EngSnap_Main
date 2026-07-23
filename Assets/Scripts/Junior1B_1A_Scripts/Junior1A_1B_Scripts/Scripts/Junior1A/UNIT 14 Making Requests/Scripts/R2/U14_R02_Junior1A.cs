using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U14_R02_Junior1A_TabModule
{
    [Header("Tab UI Hierarchy Bindings")]
    public Button NavigationTabButton;
    public GameObject TabPagePanel;

    [Header("Tab Audio Clips")]
    public AudioClip TabIntroClip;

    [Header("Manual Chat Bubble Card Assignments")]
    [Tooltip("Drag your speech bubble GameObjects here in the EXACT reading order (top-to-bottom)")]
    public GameObject[] ClickableBubbleButtons; 

    [Header("Chat Bubble Audio Files")]
    [Tooltip("Assign the audio clips matching the precise index order of your Clickable Bubble Buttons array")]
    public AudioClip[] Clips;

    [HideInInspector] public int CurrentAudioIndex = 0;
    [HideInInspector] public HashSet<int> CompletedAudioIndices = new HashSet<int>(); 
}

public class U14_R02_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [Header("Global Audio Settings")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _masterIntroClip;

    [Header("Dynamic Tab Hierarchy Tracking")]
    [SerializeField] private U14_R02_Junior1A_TabModule[] _tabModules; // Fixed Type mismatch name here

    [Header("Visual Highlighting Colors")]
    [SerializeField] private Color _activeHighlightColor = new Color(1f, 0.9565453f, 0.4386792f, 1.0f);
    [SerializeField] private Color _selectedTabColor = Color.white;
    [SerializeField] private Color _unselectedTabColor = new Color(0.7f, 0.7f, 0.7f, 1.0f);

    [Header("Optional Score UI Display")]
    [Tooltip("Optional text field to show current clicked bubbles score progress (e.g., '0 / 8')")]
    [SerializeField] private TextMeshProUGUI _scoreProgressText;

    [Header("State Tracking")]
    [SerializeField] private bool _isViewed = false;
    [SerializeField] private bool _isSlowed = false;

    private int _activeTabIndex = 0;
    private Coroutine _introRoutine;
    private Coroutine _buttonRoutine;
    private Coroutine _tabAudioRoutine;

    private int _lastSpokenTabTrack = 0;
    private int _lastSpokenIndexTrack = 0;
    private bool _lastAudioWasTabIntro = false;

    // Internal score counting fields
    private int _totalRequiredScore = 0;
    private int _currentScore = 0;

    public bool IsViewed => _isViewed;

    private void Awake()
    {
        for (int i = 0; i < _tabModules.Length; i++)
        {
            int capturedIndex = i;
            if (_tabModules[i].NavigationTabButton != null)
            {
                _tabModules[i].NavigationTabButton.onClick.RemoveAllListeners();
                _tabModules[i].NavigationTabButton.onClick.AddListener(() => OnTabNavigationClicked(capturedIndex));
            }
        }
    }

    private void OnEnable()
    {
        KillAllAudioRoutines();

        _isViewed = false;
        _isSlowed = false;
        _activeTabIndex = 0;
        _lastSpokenTabTrack = 0;
        _lastSpokenIndexTrack = 0;
        _lastAudioWasTabIntro = false;
        _currentScore = 0;
        _totalRequiredScore = 0;

        if (_audioSource != null) _audioSource.pitch = 1.0f;

        _introRoutine = StartCoroutine(Starter());
    }

    private void OnDisable()
    {
        KillAllAudioRoutines();
    }

    private IEnumerator Starter()
    {
        // 🔒 Lock next button immediately on startup
        if (GameManager_Junior1A.Instance != null)
        {
            GameManager_Junior1A.Instance.Next(false);
        }

        // Calculate the maximum required score based on total inspector assignments
        foreach (var tab in _tabModules)
        {
            if (tab.ClickableBubbleButtons != null)
            {
                _totalRequiredScore += tab.ClickableBubbleButtons.Length;
            }
        }

        // Prep, clear click states, and wire up all manually assigned bubble buttons across tabs
        for (int t = 0; t < _tabModules.Length; t++)
        {
            var tab = _tabModules[t];
            tab.CurrentAudioIndex = 0;
            tab.CompletedAudioIndices.Clear();

            if (tab.ClickableBubbleButtons == null) continue;

            for (int i = 0; i < tab.ClickableBubbleButtons.Length; i++)
            {
                GameObject bubbleObj = tab.ClickableBubbleButtons[i];
                if (bubbleObj == null) continue;

                bubbleObj.SetActive(true);
                ClearCardHighlightVisuals(bubbleObj.transform);

                Button bubbleBtn = bubbleObj.GetComponentInChildren<Button>(true);
                if (bubbleBtn != null)
                {
                    int capturedTab = t;
                    int capturedIndex = i;
                    bubbleBtn.onClick.RemoveAllListeners();
                    bubbleBtn.onClick.AddListener(() => PlayAudio(capturedTab, capturedIndex));
                }
                else
                {
                    Debug.LogWarning($"[U14_R02] GameObject '{bubbleObj.name}' in Tab {t} is missing a Button component!");
                }
            }
        }

        UpdateScoreUI();
        _audioSource.pitch = _isSlowed ? 0.75f : 1.0f;
        SwitchActiveTabPanelVisuals(0);

        if (_masterIntroClip != null && _audioSource != null)
        {
            _audioSource.clip = _masterIntroClip;
            _audioSource.Play();
            
            float currentPitch = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            yield return new WaitForSeconds(_masterIntroClip.length / currentPitch);
        }

        if (_tabModules.Length > 0 && _tabModules[0].TabIntroClip != null)
        {
            _tabAudioRoutine = StartCoroutine(PlayTabIntroAudioSequence(_tabModules[0]));
        }

        _introRoutine = null;
    }

    public void PlayAudio(int tabIndex, int buttonIndex)
    {
        if (tabIndex != _activeTabIndex) return;

        KillAllAudioRoutines();
        U14_R02_Junior1A_TabModule activeTab = _tabModules[tabIndex];

        if (activeTab.CurrentAudioIndex < activeTab.ClickableBubbleButtons.Length)
        {
            GameObject prevObj = activeTab.ClickableBubbleButtons[activeTab.CurrentAudioIndex];
            if (prevObj != null) ClearCardHighlightVisuals(prevObj.transform);
        }

        _lastSpokenTabTrack = tabIndex;
        _lastSpokenIndexTrack = buttonIndex;
        _lastAudioWasTabIntro = false;
        activeTab.CurrentAudioIndex = buttonIndex;

        _buttonRoutine = StartCoroutine(StartButtonAudio(activeTab, buttonIndex));
    }

    private IEnumerator StartButtonAudio(U14_R02_Junior1A_TabModule tab, int index)
    {
        if (index >= tab.ClickableBubbleButtons.Length || tab.ClickableBubbleButtons[index] == null) yield break;

        Transform currentCard = tab.ClickableBubbleButtons[index].transform;

        if (currentCard.TryGetComponent(out Image cardImg)) cardImg.color = _activeHighlightColor;
        
        if (currentCard.childCount > 0 && currentCard.GetChild(0).childCount > 0)
        {
            if (currentCard.GetChild(0).GetChild(0).TryGetComponent(out Image structuralFrame))
            {
                structuralFrame.enabled = true;
            }
        }

        if (currentCard.TryGetComponent(out PopEffect_Junior1A pop))
        {
            pop.enabled = false;
            pop.enabled = true;
        }

        if (index < tab.Clips.Length && tab.Clips[index] != null && _audioSource != null)
        {
            _audioSource.clip = tab.Clips[index];
            _audioSource.Play();

            float currentPitch = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float waitDuration = tab.Clips[index].length / currentPitch;
            yield return new WaitForSeconds(waitDuration);
        }

        ClearCardHighlightVisuals(currentCard);

        // Score handling logic: Trigger only on the first unique click discovery
        if (!tab.CompletedAudioIndices.Contains(index))
        {
            tab.CompletedAudioIndices.Add(index);
            _currentScore++;
            UpdateScoreUI();
            CheckGlobalWinCondition();
        }

        _buttonRoutine = null;
    }

    private void OnTabNavigationClicked(int tabIndex)
    {
        KillAllAudioRoutines();

        U14_R02_Junior1A_TabModule formerTab = _tabModules[_activeTabIndex];
        if (formerTab.CurrentAudioIndex < formerTab.ClickableBubbleButtons.Length)
        {
            GameObject prevObj = formerTab.ClickableBubbleButtons[formerTab.CurrentAudioIndex];
            if (prevObj != null) ClearCardHighlightVisuals(prevObj.transform);
        }

        SwitchActiveTabPanelVisuals(tabIndex);

        _lastSpokenTabTrack = tabIndex;
        _lastAudioWasTabIntro = true;

        U14_R02_Junior1A_TabModule activeTab = _tabModules[tabIndex];
        if (activeTab.TabIntroClip != null)
        {
            _tabAudioRoutine = StartCoroutine(PlayTabIntroAudioSequence(activeTab));
        }
    }

    private IEnumerator PlayTabIntroAudioSequence(U14_R02_Junior1A_TabModule tab)
    {
        if (_audioSource == null || tab.TabIntroClip == null) yield break;

        _audioSource.clip = tab.TabIntroClip;
        _audioSource.Play();

        float currentPitch = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
        yield return new WaitForSeconds(tab.TabIntroClip.length / currentPitch);

        _tabAudioRoutine = null;
    }

    private void SwitchActiveTabPanelVisuals(int targetTabIndex)
    {
        _activeTabIndex = targetTabIndex;

        for (int i = 0; i < _tabModules.Length; i++)
        {
            if (_tabModules[i].TabPagePanel != null)
            {
                _tabModules[i].TabPagePanel.SetActive(i == _activeTabIndex);
            }

            if (_tabModules[i].NavigationTabButton != null)
            {
                if (_tabModules[i].NavigationTabButton.TryGetComponent(out Image tabImage))
                {
                    tabImage.color = (i == _activeTabIndex) ? _selectedTabColor : _unselectedTabColor;
                }
            }
        }
    }

    private void ClearCardHighlightVisuals(Transform cardTransform)
    {
        if (cardTransform == null) return;

        if (cardTransform.TryGetComponent(out Image cardImg)) cardImg.color = Color.white;
        
        if (cardTransform.childCount > 0 && cardTransform.GetChild(0).childCount > 0)
        {
            if (cardTransform.GetChild(0).GetChild(0).TryGetComponent(out Image structuralFrame))
            {
                structuralFrame.enabled = false;
            }
        }
    }

    private void UpdateScoreUI()
    {
        if (_scoreProgressText != null)
        {
            _scoreProgressText.text = $"{_currentScore} / {_totalRequiredScore}";
        }
    }

    private void CheckGlobalWinCondition()
    {
        if (_isViewed) return;

        // Condition check: Has the score matched the total registered buttons array sizes?
        if (_currentScore >= _totalRequiredScore)
        {
            _isViewed = true;
            if (GameManager_Junior1A.Instance != null)
            {
                GameManager_Junior1A.Instance.Next(true);
            }
        }
    }

    private void KillAllAudioRoutines()
    {
        if (_buttonRoutine != null) StopCoroutine(_buttonRoutine);
        if (_introRoutine != null) StopCoroutine(_introRoutine);
        if (_tabAudioRoutine != null) StopCoroutine(_tabAudioRoutine);
        if (_audioSource != null) _audioSource.Stop();
        
        _buttonRoutine = null;
        _introRoutine = null;
        _tabAudioRoutine = null;
    }

    public void Repeat()
    {
        KillAllAudioRoutines();

        U14_R02_Junior1A_TabModule targetedTab = _tabModules[_lastSpokenTabTrack];
        
        if (targetedTab.CurrentAudioIndex < targetedTab.ClickableBubbleButtons.Length)
        {
            GameObject currentObj = targetedTab.ClickableBubbleButtons[targetedTab.CurrentAudioIndex];
            if (currentObj != null) ClearCardHighlightVisuals(currentObj.transform);
        }

        if (_activeTabIndex != _lastSpokenTabTrack)
        {
            SwitchActiveTabPanelVisuals(_lastSpokenTabTrack);
        }

        if (_lastAudioWasTabIntro)
        {
            if (targetedTab.TabIntroClip != null)
            {
                _tabAudioRoutine = StartCoroutine(PlayTabIntroAudioSequence(targetedTab));
            }
        }
        else
        {
            targetedTab.CurrentAudioIndex = _lastSpokenIndexTrack;
            _buttonRoutine = StartCoroutine(StartButtonAudio(targetedTab, _lastSpokenIndexTrack));
        }
    }

    public void Slow(TextMeshProUGUI text)
    {
        _isSlowed = !_isSlowed;
        if (text != null) text.text = _isSlowed ? "    FAST" : "    SLOW";
        if (_audioSource != null) _audioSource.pitch = _isSlowed ? 0.75f : 1f;
    }
}