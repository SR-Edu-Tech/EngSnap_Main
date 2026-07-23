using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U13_L01_Junior1A_QuestionData
{
    [Tooltip("The text item displayed in the Scroll View list row")]
    public string ScrollText;

    [Tooltip("The answer text that appears in the Character's speech bubble")]
    public string CharacterBubbleText;

    [Tooltip("Audio clip for the question sentence line")]
    public AudioClip QuestionAudio;

    [Tooltip("Audio clip for the answer bubble line")]
    public AudioClip AnswerAudio;

    [HideInInspector] public bool IsCompleted = false;
    [HideInInspector] public Color OriginalColor = Color.white; 
}

[Serializable]
public class U13_L01_Junior1A_TabModule
{
    [Header("Tab UI Bindings")]
    [Tooltip("The core navigation button used to click and open this page")]
    public Button NavigationTabButton;

    [Tooltip("The entire full-screen panel page container for this tab")]
    public GameObject TabPagePanel;
    
    [Tooltip("The Content parent transform holding all row items inside this tab's Scroll View")]
    public Transform ScrollContentParent;

    [Tooltip("The TextMeshPro target inside this tab's specific character bubble frame")]
    public TextMeshProUGUI CharacterBubbleTMP;

    [Header("Tab Data Sets")]
    public U13_L01_Junior1A_QuestionData[] Questions; 

    [HideInInspector] public int LastClickedIndex = -1;
    [HideInInspector] public Color OriginalTabButtonColor = Color.gray; // Saves your native custom layout gray palette
}

public class U13_L01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [Header("Global Audio Tracks")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;

    [Header("Dynamic Tab Management")]
    [Tooltip("Add as many tabs here as you want! Upgradable framework.")]
    [SerializeField] private U13_L01_Junior1A_TabModule[] _tabModules; 

    [Header("Visual Highlighting Colors")]
    [SerializeField] private Color _activeHighlightColor = Color.yellow;
    [SerializeField] private Color _completedRowColor = Color.green;

    [Header("Global Tracking")]
    [SerializeField] private TextMeshProUGUI _globalProgressText;

    private int _activeTabIndex = 0;
    private bool _isViewed = false;
    private Coroutine _autoPlayMasterRoutine;

    public bool IsViewed => _isViewed;

    private void Awake()
    {
        for (int i = 0; i < _tabModules.Length; i++)
        {
            int capturedIndex = i;
            if (_tabModules[i].NavigationTabButton != null)
            {
                // 🎨 Cache the default grey/unselected designer color assigned in the scene context inspector
                if (_tabModules[i].NavigationTabButton.targetGraphic != null)
                {
                    _tabModules[i].OriginalTabButtonColor = _tabModules[i].NavigationTabButton.targetGraphic.color;
                }

                _tabModules[i].NavigationTabButton.onClick.RemoveAllListeners();
                _tabModules[i].NavigationTabButton.onClick.AddListener(() => OnTabNavigationButtonClicked(capturedIndex));
            }
        }
    }

    private void OnEnable()
    {
        StartPlaybackFromTab(0);
    }

    private void StartPlaybackFromTab(int startingTabIndex)
    {
        if (_autoPlayMasterRoutine != null)
        {
            StopCoroutine(_autoPlayMasterRoutine);
        }
        if (_audioSource != null)
        {
            _audioSource.Stop();
        }

        _autoPlayMasterRoutine = StartCoroutine(AutoPlayRoutine(startingTabIndex));
    }

    private IEnumerator AutoPlayRoutine(int startingTabIndex)
    {
        _isViewed = false;
        GameManager_Junior1A.Instance?.Next(false);

        for (int t = 0; t < _tabModules.Length; t++)
        {
            var tab = _tabModules[t];
            tab.LastClickedIndex = -1;
            if (tab.CharacterBubbleTMP != null) tab.CharacterBubbleTMP.text = "";

            SaveOriginalRowColors(tab);
            PopulateRowTextOnly(tab); 
        }

        UpdateGlobalProgressUI();

        if (startingTabIndex == 0 && _introClip != null && _audioSource != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length);
        }

        for (int t = startingTabIndex; t < _tabModules.Length; t++)
        {
            SwitchActiveTabVisuals(t);
            U13_L01_Junior1A_TabModule activeTab = _tabModules[t];

            for (int q = 0; q < activeTab.Questions.Length; q++)
            {
                U13_L01_Junior1A_QuestionData data = activeTab.Questions[q];

                if (activeTab.LastClickedIndex != -1 && activeTab.LastClickedIndex != q)
                {
                    var prevQuestion = activeTab.Questions[activeTab.LastClickedIndex];
                    Color previousColor = prevQuestion.IsCompleted ? _completedRowColor : prevQuestion.OriginalColor;
                    SetRowColor(activeTab, activeTab.LastClickedIndex, previousColor);
                }

                activeTab.LastClickedIndex = q;

                SetRowColor(activeTab, q, _activeHighlightColor);
                if (activeTab.CharacterBubbleTMP != null) activeTab.CharacterBubbleTMP.text = "";

                if (q < activeTab.ScrollContentParent.childCount)
                {
                    if (activeTab.ScrollContentParent.GetChild(q).TryGetComponent(out PopEffect_Junior1A rowPop))
                    {
                        rowPop.enabled = false;
                        rowPop.enabled = true;
                    }
                }

                if (data.QuestionAudio != null && _audioSource != null)
                {
                    _audioSource.clip = data.QuestionAudio;
                    _audioSource.Play();
                    yield return new WaitForSeconds(data.QuestionAudio.length);
                }

                if (activeTab.CharacterBubbleTMP != null)
                {
                    activeTab.CharacterBubbleTMP.text = data.CharacterBubbleText;
                    if (activeTab.CharacterBubbleTMP.TryGetComponent(out TextPopEffect_Junior1A textPop))
                    {
                        textPop.enabled = false;
                        textPop.enabled = true;
                    }
                }

                if (data.AnswerAudio != null && _audioSource != null)
                {
                    _audioSource.clip = data.AnswerAudio;
                    _audioSource.Play();
                    yield return new WaitForSeconds(data.AnswerAudio.length);
                }

                data.IsCompleted = true;
                SetRowColor(activeTab, q, _completedRowColor);
                UpdateGlobalProgressUI();
            }
        }

        _isViewed = true;
        GameManager_Junior1A.Instance?.Next(true);
        _autoPlayMasterRoutine = null;
    }

    private void OnTabNavigationButtonClicked(int tabIndex)
    {
        if (!_isViewed) return;

        ResetAllCompletionData();
        StartPlaybackFromTab(tabIndex);
    }

    private void ResetAllCompletionData()
    {
        foreach (var tab in _tabModules)
        {
            tab.LastClickedIndex = -1;
            foreach (var q in tab.Questions)
            {
                q.IsCompleted = false;
            }
        }
    }

    private void SwitchActiveTabVisuals(int targetTabIndex)
    {
        _activeTabIndex = targetTabIndex;

        for (int i = 0; i < _tabModules.Length; i++)
        {
            var tab = _tabModules[i];
            
            // Toggle Page Panels
            if (tab.TabPagePanel != null)
            {
                tab.TabPagePanel.SetActive(i == _activeTabIndex);
            }

            // 🎨 Manage Tab Button Highlights dynamically
            if (tab.NavigationTabButton != null && tab.NavigationTabButton.targetGraphic != null)
            {
                // Active selection shifts to pure White (#FFFFFF), unselected returns to your designer gray state
                tab.NavigationTabButton.targetGraphic.color = (i == _activeTabIndex) ? Color.white : tab.OriginalTabButtonColor;
            }
        }
    }

    private void SaveOriginalRowColors(U13_L01_Junior1A_TabModule tab)
    {
        if (tab.ScrollContentParent == null) return;

        for (int i = 0; i < tab.Questions.Length; i++)
        {
            if (i < tab.ScrollContentParent.childCount)
            {
                Transform rowRoot = tab.ScrollContentParent.GetChild(i);
                Image targetImage = GetBGGirlDImage(rowRoot);

                if (targetImage != null)
                {
                    tab.Questions[i].OriginalColor = targetImage.color;
                }
            }
        }
    }

    private void PopulateRowTextOnly(U13_L01_Junior1A_TabModule tab)
    {
        if (tab.ScrollContentParent == null) return;

        for (int i = 0; i < tab.Questions.Length; i++)
        {
            if (i < tab.ScrollContentParent.childCount)
            {
                Transform rowItem = tab.ScrollContentParent.GetChild(i);
                
                TextMeshProUGUI rowTMP = rowItem.GetComponentInChildren<TextMeshProUGUI>();
                if (rowTMP != null) rowTMP.text = tab.Questions[i].ScrollText;

                Button rowButton = rowItem.GetComponent<Button>();
                if (rowButton != null)
                {
                    rowButton.onClick.RemoveAllListeners();
                }
            }
        }
    }

    private void SetRowColor(U13_L01_Junior1A_TabModule tab, int index, Color targetColor)
    {
        if (tab.ScrollContentParent == null || index >= tab.ScrollContentParent.childCount) return;
        
        Transform rowRoot = tab.ScrollContentParent.GetChild(index);
        Image targetImage = GetBGGirlDImage(rowRoot);

        if (targetImage != null)
        {
            targetImage.color = targetColor;
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

        int totalQuestions = 0;
        int completedQuestions = 0;

        foreach (var tab in _tabModules)
        {
            totalQuestions += tab.Questions.Length;
            foreach (var q in tab.Questions)
            {
                if (q.IsCompleted) completedQuestions++;
            }
        }

        _globalProgressText.text = $"{completedQuestions}/{totalQuestions}";
    }
}