using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class U13_L02_Junior1A_TabModule
{
    [Header("Tab UI Hierarchy Bindings")]
    [Tooltip("The navigation button at the top used to open this page layout")]
    public Button NavigationTabButton;

    [Tooltip("The main container Game Object for this tab (e.g., Tab_1_Container)")]
    public GameObject TabPagePanel;

    [Tooltip("The 'OPTIONS' transform container holding all this page's child card rows directly")]
    public Transform OptionsContainer;

    [Header("Tab Audio Clips")]
    [Tooltip("The specific introduction audio clip for this unique tab panel")]
    public AudioClip TabIntroClip;

    [Tooltip("Assign the audio clips matching the precise index order of the children inside OPTIONS")]
    public AudioClip[] Clips;

    [HideInInspector] public int CurrentAudioIndex = 0;
}

public class U13_L02_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [Header("Global Audio Settings")]
    [SerializeField] private AudioSource _audioSource;
    [Tooltip("The master introduction clip that plays only once at the very start of the entire script")]
    [SerializeField] private AudioClip _masterIntroClip;

    [Header("Dynamic Tab Hierarchy Tracking")]
    [Tooltip("Add your tab design layouts here. Works cleanly with 3 or more modules!")]
    [SerializeField] private U13_L02_Junior1A_TabModule[] _tabModules;

    [Header("Visual Highlighting Colors")]
    [SerializeField] private Color _activeHighlightColor = new Color(1f, 0.9565453f, 0.4386792f, 1.0f); // Default yellow highlight
    [Tooltip("The color applied to the top navigation tab button when it is actively selected.")]
    [SerializeField] private Color _selectedTabColor = Color.white; // 🔥 FFFFFF
    [Tooltip("The color applied to the top navigation tab buttons when they are unselected.")]
    [SerializeField] private Color _unselectedTabColor = new Color(0.7f, 0.7f, 0.7f, 1.0f); // Adjust in Inspector as needed

    [Header("Animation Settings")]
    [Tooltip("Time delay spacing in seconds between each card popping up on the screen container layout.")]
    [SerializeField] private float _spawnDelay = 0.5f;

    [Header("State Tracking")]
    [SerializeField] private bool _isViewed = false;
    [SerializeField] private bool _isSlowed = false;

    private int _activeTabIndex = 0;
    private Coroutine _masterRoutine;
    private Coroutine _buttonRoutine;

    // Track the absolute last tab and item that was spoken for the specialized structural Repeat function
    private int _lastSpokenTabTrack = 0;
    private int _lastSpokenIndexTrack = 0;

    public bool IsViewed => _isViewed;

    private void Awake()
    {
        // Programmatically wire the navigation tabs to their click functions
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
        if (_masterRoutine != null) StopCoroutine(_masterRoutine);
        if (_buttonRoutine != null) StopCoroutine(_buttonRoutine);

        _isViewed = false;
        _isSlowed = false;
        _activeTabIndex = 0;
        _lastSpokenTabTrack = 0;
        _lastSpokenIndexTrack = 0;

        if (_audioSource != null) _audioSource.pitch = 1.0f;

        _masterRoutine = StartCoroutine(Starter());
    }

    private IEnumerator Starter()
    {
        GameManager_Junior1A.Instance?.Next(false);

        // Stage 1: Clean, reset, and turn off all nested buttons across all containers
        for (int t = 0; t < _tabModules.Length; t++)
        {
            var tab = _tabModules[t];
            tab.CurrentAudioIndex = 0;

            if (tab.OptionsContainer != null)
            {
                foreach (Transform card in tab.OptionsContainer)
                {
                    card.gameObject.SetActive(false);
                    card.GetComponent<Image>().color = Color.white;
                    
                    if (card.childCount > 0 && card.GetChild(0).childCount > 0)
                    {
                        card.GetChild(0).GetChild(0).GetComponent<Image>().enabled = false;
                    }
                }
            }
        }

        // Apply initial system pitch tracking configuration
        _audioSource.pitch = _isSlowed ? 0.75f : 1.0f;

        // Play the overall Master Introduction Audio Track once before anything else
        if (_masterIntroClip != null && _audioSource != null)
        {
            _audioSource.clip = _masterIntroClip;
            _audioSource.Play();
            
            float currentPitch = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            yield return new WaitForSeconds(_masterIntroClip.length / currentPitch);
        }

        // Stage 2: Master Autoplay Loop (Tab-by-Tab sequence tracking)
        for (int t = 0; t < _tabModules.Length; t++)
        {
            SwitchActiveTabPanelVisuals(t); // Updates panels and applies the FFFFFF color to the active tab button
            U13_L02_Junior1A_TabModule activeTab = _tabModules[t];

            // Play this specific tab's unique intro audio before displaying/playing its buttons
            if (activeTab.TabIntroClip != null && _audioSource != null)
            {
                _audioSource.clip = activeTab.TabIntroClip;
                _audioSource.Play();
                
                float currentPitch = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
                yield return new WaitForSeconds(activeTab.TabIntroClip.length / currentPitch);
            }

            if (activeTab.OptionsContainer == null) continue;

            // Sequential entry pop animation layout inside current OPTIONS folder
            foreach (Transform card in activeTab.OptionsContainer)
            {
                card.gameObject.SetActive(true);
                if (card.TryGetComponent(out PopEffect_Junior1A pop))
                {
                    pop.enabled = false;
                    pop.enabled = true;
                }
                yield return new WaitForSeconds(_spawnDelay);
            }

            // Loop and auto-play each card button one-by-one
            for (int i = 0; i < activeTab.Clips.Length; i++)
            {
                if (i >= activeTab.OptionsContainer.childCount) break;

                _lastSpokenTabTrack = t;
                _lastSpokenIndexTrack = i;

                activeTab.CurrentAudioIndex = i;
                Transform currentCard = activeTab.OptionsContainer.GetChild(i);

                // Apply Highlight Visual Color and Enable Focus Frame
                currentCard.GetComponent<Image>().color = _activeHighlightColor;
                if (currentCard.childCount > 0 && currentCard.GetChild(0).childCount > 0)
                {
                    currentCard.GetChild(0).GetChild(0).GetComponent<Image>().enabled = true;
                }

                _audioSource.clip = activeTab.Clips[i];
                _audioSource.Play();

                float currentPitch = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
                float waitDuration = activeTab.Clips[i].length / currentPitch;
                yield return new WaitForSeconds(waitDuration);

                // Clean-up Highlight Visual Color and Focus Frame
                currentCard.GetComponent<Image>().color = Color.white;
                if (currentCard.childCount > 0 && currentCard.GetChild(0).childCount > 0)
                {
                    currentCard.GetChild(0).GetChild(0).GetComponent<Image>().enabled = false;
                }
            }
        }

        // Stage 3: Process complete. Hand control over to standard player interaction inputs.
        _isViewed = true;
        GameManager_Junior1A.Instance?.Next(true);
        _masterRoutine = null;
    }

    public void PlayAudio(int tabIndex, int buttonIndex)
    {
        if (!_isViewed) return;

        if (_buttonRoutine != null) StopCoroutine(_buttonRoutine);
        if (_masterRoutine != null) StopCoroutine(_masterRoutine);

        U13_L02_Junior1A_TabModule activeTab = _tabModules[tabIndex];

        // Wipe visual highlight tracking states on previous element row index selection block
        Transform prevCard = activeTab.OptionsContainer.GetChild(activeTab.CurrentAudioIndex);
        prevCard.GetComponent<Image>().color = Color.white;
        if (prevCard.childCount > 0 && prevCard.GetChild(0).childCount > 0)
        {
            prevCard.GetChild(0).GetChild(0).GetComponent<Image>().enabled = false;
        }

        _lastSpokenTabTrack = tabIndex;
        _lastSpokenIndexTrack = buttonIndex;
        activeTab.CurrentAudioIndex = buttonIndex;

        _buttonRoutine = StartCoroutine(StartButtonAudio(activeTab, buttonIndex));
    }

    private IEnumerator StartButtonAudio(U13_L02_Junior1A_TabModule tab, int index)
    {
        Transform currentCard = tab.OptionsContainer.GetChild(index);

        currentCard.GetComponent<Image>().color = _activeHighlightColor;
        if (currentCard.childCount > 0 && currentCard.GetChild(0).childCount > 0)
        {
            currentCard.GetChild(0).GetChild(0).GetComponent<Image>().enabled = true;
        }

        _audioSource.clip = tab.Clips[index];
        _audioSource.Play();

        float currentPitch = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
        float waitDuration = tab.Clips[index].length / currentPitch;
        yield return new WaitForSeconds(waitDuration);

        currentCard.GetComponent<Image>().color = Color.white;
        if (currentCard.childCount > 0 && currentCard.GetChild(0).childCount > 0)
        {
            currentCard.GetChild(0).GetChild(0).GetComponent<Image>().enabled = false;
        }
    }

    private void OnTabNavigationClicked(int tabIndex)
    {
        if (!_isViewed) return;
        SwitchActiveTabPanelVisuals(tabIndex);
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

            // 🔥 Dynamically update the top tab button image color targets
            if (_tabModules[i].NavigationTabButton != null)
            {
                Image tabImage = _tabModules[i].NavigationTabButton.GetComponent<Image>();
                if (tabImage != null)
                {
                    tabImage.color = (i == _activeTabIndex) ? _selectedTabColor : _unselectedTabColor;
                }
            }
        }
    }

    public void Repeat()
    {
        if (_buttonRoutine != null) StopCoroutine(_buttonRoutine);
        if (_masterRoutine != null) StopCoroutine(_masterRoutine);

        U13_L02_Junior1A_TabModule targetedTab = _tabModules[_lastSpokenTabTrack];
        
        Transform currentCard = targetedTab.OptionsContainer.GetChild(targetedTab.CurrentAudioIndex);
        currentCard.GetComponent<Image>().color = Color.white;
        if (currentCard.childCount > 0 && currentCard.GetChild(0).childCount > 0)
        {
            currentCard.GetChild(0).GetChild(0).GetComponent<Image>().enabled = false;
        }

        _buttonRoutine = StartCoroutine(StartButtonAudio(targetedTab, _lastSpokenIndexTrack));
    }

    public void Slow(TextMeshProUGUI text)
    {
        _isSlowed = !_isSlowed;
        text.text = _isSlowed ? "    FAST" : "    SLOW";
        _audioSource.pitch = _isSlowed ? 0.75f : 1f;
    }
}