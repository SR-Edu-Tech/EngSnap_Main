using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U14_L02_Junior1A_BubbleData
{
    [Tooltip("Audio clip associated with this specific conversational bubble line")]
    public AudioClip BubbleAudio;

    [HideInInspector] public bool IsCompleted = false;
    [HideInInspector] public Color OriginalColor = Color.white; 
}

[Serializable]
public class U14_L02_Junior1A_TabModule
{
    [Header("Tab Navigation")]
    [Tooltip("The tab button used to switch to this page")]
    public Button NavigationTabButton;

    [Tooltip("The entire full-screen panel page container for this tab")]
    public GameObject TabPagePanel;

    [Header("Manual Chat Bubble Assignments")]
    [Tooltip("Drag your Bubble GameObjects with Button components here in the EXACT order they should be read (top-to-bottom)")]
    public Button[] ClickableBubbleButtons; 

    [Header("Chat Bubble Audio Files")]
    [Tooltip("Match this size EXACTLY to the Clickable Bubble Buttons array above!")]
    public U14_L02_Junior1A_BubbleData[] ChatBubbles;

    [HideInInspector] public int LastClickedIndex = -1;
    [HideInInspector] public Color OriginalTabButtonColor = Color.gray; 
    [HideInInspector] public bool TabSequenceCompletedFully = false; // 🔥 Tracks if this specific tab finished its linear auto-play
}

public class U14_L02_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [Header("Global Audio Components")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;

    [Header("Scalable Chat Tabs")]
    [SerializeField] private U14_L02_Junior1A_TabModule[] _tabModules;

    [Header("Visual Validation Tuning")]
    [SerializeField] private Color _activeHighlightColor = Color.yellow;
    [SerializeField] private Color _completedBubbleColor = Color.green;
    [Tooltip("The color applied to the top navigation tab icon button when it completes all its audio clips")]
    [SerializeField] private Color _completedTabIconColor = Color.green;

    [Header("Progress Tracking")]
    [SerializeField] private TextMeshProUGUI _globalProgressText;

    private int _activeTabIndex = 0;
    private bool _isViewed = false;
    
    // Independent audio state coroutines
    private Coroutine _masterAutoPlayRoutine;
    private Coroutine _bubblePlaybackRoutine;

    public bool IsViewed => _isViewed;

    private void Awake()
    {
        for (int i = 0; i < _tabModules.Length; i++)
        {
            int capturedIndex = i;
            if (_tabModules[i].NavigationTabButton != null)
            {
                if (_tabModules[i].NavigationTabButton.targetGraphic != null)
                {
                    _tabModules[i].OriginalTabButtonColor = _tabModules[i].NavigationTabButton.targetGraphic.color;
                }

                _tabModules[i].NavigationTabButton.onClick.RemoveAllListeners();
                _tabModules[i].NavigationTabButton.onClick.AddListener(() => TryManualTabSwitch(capturedIndex));
            }
        }
    }

    private void OnEnable()
    {
        KillAllActiveRoutines();
        _isViewed = false;
        _activeTabIndex = 0;

        _masterAutoPlayRoutine = StartCoroutine(Starter());
    }

    private void OnDisable()
    {
        KillAllActiveRoutines();
    }

    private IEnumerator Starter()
    {
        // ⏳ Delay checking GameManager until initialization safe-window holds firm
        yield return new WaitForEndOfFrame();

        if (GameManager_Junior1A.Instance != null)
        {
            GameManager_Junior1A.Instance.Next(false);
        }

        for (int t = 0; t < _tabModules.Length; t++)
        {
            var tab = _tabModules[t];
            tab.LastClickedIndex = -1;
            tab.TabSequenceCompletedFully = false;

            SaveOriginalBubbleColors(tab);
            
            // Clean out button logic arrays since execution is now automated
            if (tab.ClickableBubbleButtons != null)
            {
                foreach (var btn in tab.ClickableBubbleButtons)
                {
                    if (btn != null) btn.onClick.RemoveAllListeners();
                }
            }

            for (int i = 0; i < tab.ChatBubbles.Length; i++)
            {
                tab.ChatBubbles[i].IsCompleted = false;
            }
        }

        UpdateGlobalProgressUI();
        
        // Force display visually back down to tab page zero
        SwitchActiveTabVisualLayouts(0);

        // Play introduction audio asset logic
        if (_introClip != null && _audioSource != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length);
        }

        // 🔥 Begin the completely automated tab narrative progression run loop
        for (int t = 0; t < _tabModules.Length; t++)
        {
            if (t != _activeTabIndex)
            {
                SwitchActiveTabVisualLayouts(t);
            }

            var currentTab = _tabModules[t];

            // Run through every single speech bubble inside the current tab panel
            for (int b = 0; b < currentTab.ChatBubbles.Length; b++)
            {
                // Assign internal bubble playback routine handles dynamically
                _bubblePlaybackRoutine = StartCoroutine(PlayBubbleAudioSequence(t, b));
                yield return _bubblePlaybackRoutine;
                _bubblePlaybackRoutine = null;
            }

            // Tab processing finished completely! Lock completion checks and stamp it green
            currentTab.TabSequenceCompletedFully = true;
            UpdateNavigationTabIconColor(t);
        }

        // Everything finished flawlessly without player intervention! Trigger next button
        _isViewed = true;
        if (GameManager_Junior1A.Instance != null)
        {
            GameManager_Junior1A.Instance.Next(true);
        }
    }

    private IEnumerator PlayBubbleAudioSequence(int tabIndex, int bIndex)
    {
        U14_L02_Junior1A_TabModule activeTab = _tabModules[tabIndex];
        U14_L02_Junior1A_BubbleData data = activeTab.ChatBubbles[bIndex];

        if (activeTab.LastClickedIndex != -1 && activeTab.LastClickedIndex != bIndex)
        {
            var prevBubble = activeTab.ChatBubbles[activeTab.LastClickedIndex];
            Color fallbackColor = prevBubble.IsCompleted ? _completedBubbleColor : prevBubble.OriginalColor;
            SetBubbleColor(activeTab, activeTab.LastClickedIndex, fallbackColor);
        }

        activeTab.LastClickedIndex = bIndex;
        SetBubbleColor(activeTab, bIndex, _activeHighlightColor);

        if (bIndex < activeTab.ClickableBubbleButtons.Length && activeTab.ClickableBubbleButtons[bIndex] != null)
        {
            if (activeTab.ClickableBubbleButtons[bIndex].TryGetComponent(out PopEffect_Junior1A pop))
            {
                pop.enabled = false;
                pop.enabled = true;
            }
        }

        if (data.BubbleAudio != null && _audioSource != null)
        {
            _audioSource.clip = data.BubbleAudio;
            _audioSource.Play();
            yield return new WaitForSeconds(data.BubbleAudio.length);
        }

        data.IsCompleted = true;
        SetBubbleColor(activeTab, bIndex, _completedBubbleColor);

        UpdateGlobalProgressUI();
    }

    private void TryManualTabSwitch(int requestedTabIndex)
    {
        if (requestedTabIndex == _activeTabIndex) return;

        // 🛑 CRITICAL MECHANIC: If the CURRENT tab has not completed its initial auto-play runtime,
        // absolutely reject the player click block input entirely.
        if (!_tabModules[_activeTabIndex].TabSequenceCompletedFully)
        {
            Debug.Log($"[U14_L02] Interaction Locked! You must finish listening to this section before changing layouts.");
            return; 
        }

        // 🔓 UNLOCKED STATE: If current tab has already been played once through completely,
        // clear old audio lines out and let them switch pages instantly!
        if (_bubblePlaybackRoutine != null) StopCoroutine(_bubblePlaybackRoutine);
        if (_masterAutoPlayRoutine != null) StopCoroutine(_masterAutoPlayRoutine);
        if (_audioSource != null) _audioSource.Stop();

        _bubblePlaybackRoutine = null;
        _masterAutoPlayRoutine = null;

        // Clean visual highlighting bounds layout artifacts from former active index block row
        U14_L02_Junior1A_TabModule oldTab = _tabModules[_activeTabIndex];
        if (oldTab.LastClickedIndex != -1)
        {
            var prevBubble = oldTab.ChatBubbles[oldTab.LastClickedIndex];
            Color fallbackColor = prevBubble.IsCompleted ? _completedBubbleColor : prevBubble.OriginalColor;
            SetBubbleColor(oldTab, oldTab.LastClickedIndex, fallbackColor);
        }

        // Initialize target tab panel visuals
        SwitchActiveTabVisualLayouts(requestedTabIndex);

        // 🔄 REPLAY CLAUSE: Since this tab layout is already cleared/green, run back from the start loop
        // while allowing free-roam manual exits to remain completely unlocked!
        _masterAutoPlayRoutine = StartCoroutine(RunFreeRoamTabAudioReplaySequence(requestedTabIndex));
    }

    private IEnumerator RunFreeRoamTabAudioReplaySequence(int tabIndex)
    {
        var targetTab = _tabModules[tabIndex];
        targetTab.LastClickedIndex = -1;

        for (int b = 0; b < targetTab.ChatBubbles.Length; b++)
        {
            _bubblePlaybackRoutine = StartCoroutine(PlayBubbleAudioSequence(tabIndex, b));
            yield return _bubblePlaybackRoutine;
            _bubblePlaybackRoutine = null;
        }
    }

    private void SwitchActiveTabVisualLayouts(int targetTabIndex)
    {
        _activeTabIndex = targetTabIndex;

        for (int i = 0; i < _tabModules.Length; i++)
        {
            var tab = _tabModules[i];
            if (tab.TabPagePanel != null)
            {
                tab.TabPagePanel.SetActive(i == _activeTabIndex);
            }

            UpdateNavigationTabIconColor(i);
        }
    }

    private void UpdateNavigationTabIconColor(int index)
    {
        var tab = _tabModules[index];
        if (tab.NavigationTabButton == null || tab.NavigationTabButton.targetGraphic == null) return;

        if (tab.TabSequenceCompletedFully)
        {
            // 🟢 STAMP GREEN: Permanently set verification colors if processing sequence has resolved
            tab.NavigationTabButton.targetGraphic.color = _completedTabIconColor;
        }
        else
        {
            // Standard state visual accent mapping logic
            tab.NavigationTabButton.targetGraphic.color = (index == _activeTabIndex) ? Color.white : tab.OriginalTabButtonColor;
        }
    }

    private void SaveOriginalBubbleColors(U14_L02_Junior1A_TabModule tab)
    {
        if (tab.ClickableBubbleButtons == null) return;

        for (int i = 0; i < tab.ChatBubbles.Length; i++)
        {
            if (i < tab.ClickableBubbleButtons.Length && tab.ClickableBubbleButtons[i] != null)
            {
                Transform bubbleRow = tab.ClickableBubbleButtons[i].transform;
                Image targetImage = GetBGGirlDImage(bubbleRow);

                if (targetImage != null)
                {
                    tab.ChatBubbles[i].OriginalColor = targetImage.color;
                }
            }
        }
    }

    private void SetBubbleColor(U14_L02_Junior1A_TabModule tab, int index, Color targetColor)
    {
        if (tab.ClickableBubbleButtons == null || index >= tab.ClickableBubbleButtons.Length) return;

        if (tab.ClickableBubbleButtons[index] != null)
        {
            Transform bubbleRow = tab.ClickableBubbleButtons[index].transform;
            Image targetImage = GetBGGirlDImage(bubbleRow);

            if (targetImage != null)
            {
                targetImage.color = targetColor;
            }
        }
    }

    private Image GetBGGirlDImage(Transform rowRoot)
    {
        Image[] images = rowRoot.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img.gameObject.name == "BGGirlD") return img;
        }
        return rowRoot.GetComponent<Image>();
    }

    private void UpdateGlobalProgressUI()
    {
        if (_globalProgressText == null) return;

        int totalBubbles = 0;
        int completedBubbles = 0;

        foreach (var tab in _tabModules)
        {
            totalBubbles += tab.ChatBubbles.Length;
            foreach (var b in tab.ChatBubbles)
            {
                if (b.IsCompleted) completedBubbles++;
            }
        }

        _globalProgressText.text = $"{completedBubbles}/{totalBubbles}";
    }

    private void KillAllActiveRoutines()
    {
        if (_bubblePlaybackRoutine != null) StopCoroutine(_bubblePlaybackRoutine);
        if (_masterAutoPlayRoutine != null) StopCoroutine(_masterAutoPlayRoutine);
        if (_audioSource != null) _audioSource.Stop();

        _bubblePlaybackRoutine = null;
        _masterAutoPlayRoutine = null;
    }
}