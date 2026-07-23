using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U13_R01_Junior1A_BubbleData
{
    [Tooltip("Audio clip associated with this specific conversational bubble line")]
    public AudioClip BubbleAudio;

    [HideInInspector] public bool IsCompleted = false;
    [HideInInspector] public Color OriginalColor = Color.white; 
}

[Serializable]
public class U13_R01_Junior1A_TabModule
{
    [Header("Tab Navigation")]
    [Tooltip("The tab button used to switch to this page")]
    public Button NavigationTabButton;

    [Tooltip("The entire full-screen panel page container for this tab")]
    public GameObject TabPagePanel;

    [Header("Manual Chat Bubble Assignments")]
    [Tooltip("Drag your Bubble GameObjects with Button components here in the EXACT order they should be read (top-to-bottom)")]
    public Button[] ClickableBubbleButtons; // 🔥 Assign manually in inspector to bypass hierarchy variations!

    [Header("Chat Bubble Audio Files")]
    [Tooltip("Match this size EXACTLY to the Clickable Bubble Buttons array above!")]
    public U13_R01_Junior1A_BubbleData[] ChatBubbles;

    [HideInInspector] public int LastClickedIndex = -1;
    [HideInInspector] public Color OriginalTabButtonColor = Color.gray; 
}

public class U13_R01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [Header("Global Audio Components")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;

    [Header("Scalable Chat Tabs")]
    [SerializeField] private U13_R01_Junior1A_TabModule[] _tabModules;

    [Header("Bubble Highlighting Colors")]
    [SerializeField] private Color _activeHighlightColor = Color.yellow;
    [SerializeField] private Color _completedBubbleColor = Color.green;

    [Header("Progress Tracking")]
    [SerializeField] private TextMeshProUGUI _globalProgressText;

    private int _activeTabIndex = 0;
    private bool _isViewed = false;
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
                _tabModules[i].NavigationTabButton.onClick.AddListener(() => SwitchActiveTab(capturedIndex));
            }
        }
    }

    private void OnEnable()
    {
        StartCoroutine(Starter());
    }

    private IEnumerator Starter()
    {
        _isViewed = false;
        GameManager_Junior1A.Instance?.Next(false);

        for (int t = 0; t < _tabModules.Length; t++)
        {
            var tab = _tabModules[t];
            tab.LastClickedIndex = -1;

            SaveOriginalBubbleColors(tab);
            WireChatBubblesDirectly(tab, t);
        }

        SwitchActiveTab(0);
        UpdateGlobalProgressUI();

        if (_introClip != null && _audioSource != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length);
        }
    }

    private void WireChatBubblesDirectly(U13_R01_Junior1A_TabModule tab, int tabModuleIndex)
    {
        if (tab.ClickableBubbleButtons == null) return;

        // Uses your clean manual array assignment length properties
        for (int i = 0; i < tab.ChatBubbles.Length; i++)
        {
            tab.ChatBubbles[i].IsCompleted = false;

            if (i < tab.ClickableBubbleButtons.Length && tab.ClickableBubbleButtons[i] != null)
            {
                Button bubbleButton = tab.ClickableBubbleButtons[i];
                int capturedBubbleIndex = i;

                bubbleButton.onClick.RemoveAllListeners();
                bubbleButton.onClick.AddListener(() => OnChatBubbleClicked(tabModuleIndex, capturedBubbleIndex));
            }
        }
    }

    private void OnChatBubbleClicked(int tabIndex, int bubbleIndex)
    {
        if (tabIndex != _activeTabIndex) return;

        if (_bubblePlaybackRoutine != null) StopCoroutine(_bubblePlaybackRoutine);
        if (_audioSource != null) _audioSource.Stop();

        _bubblePlaybackRoutine = StartCoroutine(PlayBubbleAudioSequence(tabIndex, bubbleIndex));
    }

    private IEnumerator PlayBubbleAudioSequence(int tabIndex, int bIndex)
    {
        U13_R01_Junior1A_TabModule activeTab = _tabModules[tabIndex];
        U13_R01_Junior1A_BubbleData data = activeTab.ChatBubbles[bIndex];

        if (activeTab.LastClickedIndex != -1 && activeTab.LastClickedIndex != bIndex)
        {
            var prevBubble = activeTab.ChatBubbles[activeTab.LastClickedIndex];
            Color fallbackColor = prevBubble.IsCompleted ? _completedBubbleColor : prevBubble.OriginalColor;
            SetBubbleColor(activeTab, activeTab.LastClickedIndex, fallbackColor);
        }

        activeTab.LastClickedIndex = bIndex;

        SetBubbleColor(activeTab, bIndex, _activeHighlightColor);

        // Safely triggers Pop animation components directly on your assigned target object row
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
        CheckGlobalWinCondition();
        _bubblePlaybackRoutine = null;
    }

    private void SwitchActiveTab(int targetTabIndex)
    {
        if (targetTabIndex < 0 || targetTabIndex >= _tabModules.Length) return;

        if (_bubblePlaybackRoutine != null)
        {
            StopCoroutine(_bubblePlaybackRoutine);
            _bubblePlaybackRoutine = null;
        }
        if (_audioSource != null) _audioSource.Stop();

        U13_R01_Junior1A_TabModule currentTab = _tabModules[_activeTabIndex];
        if (currentTab.LastClickedIndex != -1)
        {
            var prevBubble = currentTab.ChatBubbles[currentTab.LastClickedIndex];
            Color fallbackColor = prevBubble.IsCompleted ? _completedBubbleColor : prevBubble.OriginalColor;
            SetBubbleColor(currentTab, currentTab.LastClickedIndex, fallbackColor);
        }

        _activeTabIndex = targetTabIndex;

        for (int i = 0; i < _tabModules.Length; i++)
        {
            var tab = _tabModules[i];
            if (tab.TabPagePanel != null)
            {
                tab.TabPagePanel.SetActive(i == _activeTabIndex);
            }

            if (tab.NavigationTabButton != null && tab.NavigationTabButton.targetGraphic != null)
            {
                tab.NavigationTabButton.targetGraphic.color = (i == _activeTabIndex) ? Color.white : tab.OriginalTabButtonColor;
            }
        }
    }

    private void SaveOriginalBubbleColors(U13_R01_Junior1A_TabModule tab)
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

    private void SetBubbleColor(U13_R01_Junior1A_TabModule tab, int index, Color targetColor)
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

    private void CheckGlobalWinCondition()
    {
        foreach (var tab in _tabModules)
        {
            foreach (var b in tab.ChatBubbles)
            {
                if (!b.IsCompleted) return;
            }
        }

        _isViewed = true;
        GameManager_Junior1A.Instance?.Next(true);
    }
}