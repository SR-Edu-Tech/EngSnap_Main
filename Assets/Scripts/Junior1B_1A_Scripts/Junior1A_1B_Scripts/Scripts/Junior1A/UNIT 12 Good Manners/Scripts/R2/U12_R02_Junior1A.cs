using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U12_R02_Junior1A_QuestionData
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
    [HideInInspector] public Color OriginalColor = Color.white; // Saves each button's unique color
}

[Serializable]
public class U12_R02_Junior1A_TabModule
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
    public U12_R02_Junior1A_QuestionData[] Questions;

    [HideInInspector] public int LastClickedIndex = -1;
}

public class U12_R02_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [Header("Global Audio Tracks")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;

    [Header("Dynamic Tab Management")]
    [Tooltip("Add as many tabs here as you want! Upgradable framework.")]
    [SerializeField] private U12_R02_Junior1A_TabModule[] _tabModules;

    [Header("Visual Highlighting Colors")]
    [SerializeField] private Color _activeHighlightColor = Color.yellow;
    [SerializeField] private Color _completedRowColor = Color.green;

    [Header("Global Tracking")]
    [SerializeField] private TextMeshProUGUI _globalProgressText;

    private int _activeTabIndex = 0;
    private bool _isViewed = false;
    private Coroutine _questionPlaybackRoutine;

    public bool IsViewed => _isViewed;

    private void Awake()
    {
        for (int i = 0; i < _tabModules.Length; i++)
        {
            int capturedIndex = i;
            if (_tabModules[i].NavigationTabButton != null)
            {
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
            if (tab.CharacterBubbleTMP != null) tab.CharacterBubbleTMP.text = "";

            // 🎨 Save individual original colors first, then set up text & clicks
            SaveOriginalRowColors(tab);
            PopulateAndWireRowButtons(tab, t);
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

    private void SaveOriginalRowColors(U12_R02_Junior1A_TabModule tab)
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
                    // Remember whatever unique custom color this button had in the editor scene
                    tab.Questions[i].OriginalColor = targetImage.color;
                }
            }
        }
    }

    private void PopulateAndWireRowButtons(U12_R02_Junior1A_TabModule tab, int tabModuleIndex)
    {
        if (tab.ScrollContentParent == null) return;

        for (int i = 0; i < tab.Questions.Length; i++)
        {
            tab.Questions[i].IsCompleted = false;

            if (i < tab.ScrollContentParent.childCount)
            {
                Transform rowItem = tab.ScrollContentParent.GetChild(i);
                
                TextMeshProUGUI rowTMP = rowItem.GetComponentInChildren<TextMeshProUGUI>();
                if (rowTMP != null) rowTMP.text = tab.Questions[i].ScrollText;

                Button rowButton = rowItem.GetComponent<Button>();
                if (rowButton != null)
                {
                    int capturedQuestionIndex = i;
                    rowButton.onClick.RemoveAllListeners();
                    rowButton.onClick.AddListener(() => OnQuestionRowClicked(tabModuleIndex, capturedQuestionIndex));
                }
            }
        }
    }

    private void OnQuestionRowClicked(int tabIndex, int questionIndex)
    {
        if (tabIndex != _activeTabIndex) return;

        if (_questionPlaybackRoutine != null) StopCoroutine(_questionPlaybackRoutine);
        if (_audioSource != null) _audioSource.Stop();

        _questionPlaybackRoutine = StartCoroutine(PlaySpecificQuestionSequence(tabIndex, questionIndex));
    }

    private IEnumerator PlaySpecificQuestionSequence(int tabIndex, int qIndex)
    {
        U12_R02_Junior1A_TabModule activeTab = _tabModules[tabIndex];
        U12_R02_Junior1A_QuestionData data = activeTab.Questions[qIndex];

        // Reset visual state of the *previously* clicked row back to its native default or green completed color
        if (activeTab.LastClickedIndex != -1 && activeTab.LastClickedIndex != qIndex)
        {
            var prevQuestion = activeTab.Questions[activeTab.LastClickedIndex];
            Color previousColor = prevQuestion.IsCompleted ? _completedRowColor : prevQuestion.OriginalColor;
            SetRowColor(activeTab, activeTab.LastClickedIndex, previousColor);
        }

        activeTab.LastClickedIndex = qIndex;

        SetRowColor(activeTab, qIndex, _activeHighlightColor);
        if (activeTab.CharacterBubbleTMP != null) activeTab.CharacterBubbleTMP.text = "";

        if (qIndex < activeTab.ScrollContentParent.childCount)
        {
            if (activeTab.ScrollContentParent.GetChild(qIndex).TryGetComponent(out PopEffect_Junior1A rowPop))
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
        SetRowColor(activeTab, qIndex, _completedRowColor);

        UpdateGlobalProgressUI();
        CheckGlobalWinCondition();
        _questionPlaybackRoutine = null;
    }

    private void SwitchActiveTab(int targetTabIndex)
    {
        if (targetTabIndex < 0 || targetTabIndex >= _tabModules.Length) return;

        if (_questionPlaybackRoutine != null)
        {
            StopCoroutine(_questionPlaybackRoutine);
            _questionPlaybackRoutine = null;
        }
        if (_audioSource != null) _audioSource.Stop();

        U12_R02_Junior1A_TabModule currentTab = _tabModules[_activeTabIndex];
        if (currentTab.LastClickedIndex != -1)
        {
            var prevQuestion = currentTab.Questions[currentTab.LastClickedIndex];
            Color targetColor = prevQuestion.IsCompleted ? _completedRowColor : prevQuestion.OriginalColor;
            SetRowColor(currentTab, currentTab.LastClickedIndex, targetColor);
        }

        _activeTabIndex = targetTabIndex;

        for (int i = 0; i < _tabModules.Length; i++)
        {
            if (_tabModules[i].TabPagePanel != null)
            {
                _tabModules[i].TabPagePanel.SetActive(i == _activeTabIndex);
            }
        }
    }

    private void SetRowColor(U12_R02_Junior1A_TabModule tab, int index, Color targetColor)
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
            if (img.gameObject.name == "BGGirlD")
            {
                return img;
            }
        }
        
        // Fallback context: if BGGirlD isn't found, try returning the root button image
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

    private void CheckGlobalWinCondition()
    {
        foreach (var tab in _tabModules)
        {
            foreach (var q in tab.Questions)
            {
                if (!q.IsCompleted) return;
            }
        }

        _isViewed = true;
        GameManager_Junior1A.Instance?.Next(true);
    }
}