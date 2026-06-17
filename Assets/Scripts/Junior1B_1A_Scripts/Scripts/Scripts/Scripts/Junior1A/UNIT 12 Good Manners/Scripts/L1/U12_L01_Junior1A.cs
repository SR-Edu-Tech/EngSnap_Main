using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 

public class U12_L01_Junior1A : MonoBehaviour, Interfaces_Junior1A
{
    [Header("Audio Setup")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;          
    [SerializeField] private AudioClip _mannerIntroClip;    
    [SerializeField] private AudioClip _phraseIntroClip;    
    [SerializeField] private AudioClip[] _mannerWordClips;  
    [SerializeField] private AudioClip[] _phraseClips;      

    [Header("GameObject Phase Targets")]
    [SerializeField] private GameObject _introObj;
    [SerializeField] private GameObject _mannerWordObj;
    [SerializeField] private GameObject _phraseObj;

    [Header("New Navigation UI")]
    [SerializeField] private Button _localNextButton; 

    [Header("Runtime State Tracking")]
    [SerializeField] private int _currentPhase = 1; 
    [SerializeField] private int _currentAudioIndex = 0; 
    [SerializeField] private bool _isViewed = false;
    [SerializeField] private bool _isSlowed = false;

    private GameObject _currentActiveButtonParent;
    private AudioClip[] _currentActiveClips;

    private Coroutine _masterSequenceCoroutine;
    private Coroutine _audioPlayCoroutine;
    private Coroutine _cardsSequenceCoroutine;
    private bool _waitingForPlayerClick = false;
    private bool _isAutoPlayingSequence = false; 

    public bool IsViewed => _isViewed;

    private void Start()
    {
        if (_localNextButton != null)
        {
            _localNextButton.onClick.RemoveAllListeners();
            _localNextButton.onClick.AddListener(OnLocalNextClicked);
            _localNextButton.gameObject.SetActive(false);
        }

        _masterSequenceCoroutine = StartCoroutine(MasterTimelineSequence());
    }

    private void OnDisable()
    {
        if (_audioSource != null) _audioSource.Stop();
    }

    private IEnumerator MasterTimelineSequence()
    {
        if (_introObj == null || _mannerWordObj == null || _phraseObj == null)
        {
            Debug.LogError("❌ Phase GameObjects are not fully allocated in the Inspector!");
            yield break;
        }

        _introObj.SetActive(false);
        _mannerWordObj.SetActive(false);
        _phraseObj.SetActive(false);

        if (GameManager_Junior1A.Instance != null) GameManager_Junior1A.Instance.Next(false);

        // ==========================================
        // PHASE 1: INTRO
        // ==========================================
        _currentPhase = 1;
        _isAutoPlayingSequence = true;
        _introObj.SetActive(true);

        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length);
        }
        else
        {
            yield return new WaitForSeconds(1.0f);
        }

        _isAutoPlayingSequence = false;
        yield return WaitForPlayerProgress();

        // ==========================================
        // PHASE 2: FOUR MANNER OBJECTS
        // ==========================================
        _currentPhase = 2;
        _isAutoPlayingSequence = true;
        _introObj.SetActive(false);
        _mannerWordObj.SetActive(true);

        _currentActiveButtonParent = _mannerWordObj;
        _currentActiveClips = _mannerWordClips;

        SetCardsVisibility(_mannerWordObj, false);
        if (_audioSource != null && _mannerIntroClip != null)
        {
            _audioSource.clip = _mannerIntroClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_mannerIntroClip.length);
        }

        // Pass 'true' to play the pop/spawn animation the very first time
        _cardsSequenceCoroutine = StartCoroutine(PlayCardsSequence(_mannerWordObj, _mannerWordClips, true));
        yield return _cardsSequenceCoroutine;

        _isAutoPlayingSequence = false;
        yield return WaitForPlayerProgress();

        // ==========================================
        // PHASE 3: THANK YOU PHRASES
        // ==========================================
        _currentPhase = 3;
        _isAutoPlayingSequence = true;
        _mannerWordObj.SetActive(false);
        _phraseObj.SetActive(true);

        _currentActiveButtonParent = _phraseObj;
        _currentActiveClips = _phraseClips;

        SetCardsVisibility(_phraseObj, false);
        if (_audioSource != null && _phraseIntroClip != null)
        {
            _audioSource.clip = _phraseIntroClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_phraseIntroClip.length);
        }

        // Pass 'true' to play the pop/spawn animation the very first time
        _cardsSequenceCoroutine = StartCoroutine(PlayCardsSequence(_phraseObj, _phraseClips, true));
        yield return _cardsSequenceCoroutine;

        _isAutoPlayingSequence = false;
        _isViewed = true;

        if (_localNextButton != null) _localNextButton.gameObject.SetActive(false);
        GameManager_Junior1A.Instance?.Next(true);
    }

    // 🔧 UPDATED: Added an option to skip spawning animations on repeat requests
    private IEnumerator PlayCardsSequence(GameObject panelContainer, AudioClip[] clipsArray, bool playSpawnAnimation)
    {
        // 1. Only loop and scale up if requested (First time entrance)
        if (playSpawnAnimation)
        {
            foreach (Transform child in panelContainer.transform)
            {
                if (child.TryGetComponent(out Button btn))
                {
                    child.gameObject.SetActive(true);
                    btn.interactable = false;

                    if (child.TryGetComponent(out PopEffect_Junior1A pop))
                    {
                        pop.enabled = false;
                        pop.enabled = true;
                        yield return new WaitForSeconds(pop.PopDuration + 0.1f);
                    }
                    else
                    {
                        yield return new WaitForSeconds(0.4f);
                    }
                }
            }
        }
        else
        {
            // Just make sure everything is active and locked down out-of-the-gate
            foreach (Transform child in panelContainer.transform)
            {
                if (child.TryGetComponent(out Button btn))
                {
                    child.gameObject.SetActive(true);
                    btn.interactable = false;
                }
            }
        }

        // 2. Play audio for each card automatically and flash colors
        int cardCounter = 0;
        foreach (Transform child in panelContainer.transform)
        {
            if (child.GetComponent<Button>() != null)
            {
                _currentAudioIndex = cardCounter;
                SetCardActiveVisualState(cardCounter);

                if (clipsArray != null && cardCounter < clipsArray.Length && clipsArray[cardCounter] != null && _audioSource != null)
                {
                    _audioSource.clip = clipsArray[cardCounter];
                    _audioSource.Play();

                    float currentPitch = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
                    float waitTime = clipsArray[cardCounter].length / currentPitch;
                    yield return new WaitForSeconds(waitTime);
                }
                else
                {
                    yield return new WaitForSeconds(1.0f);
                }

                ResetCardVisualState(cardCounter);
                cardCounter++;
            }
        }

        // 3. Unlock buttons for player manual interactions
        foreach (Transform child in panelContainer.transform)
        {
            if (child.TryGetComponent(out Button btn)) btn.interactable = true;
        }
    }

    private void SetCardsVisibility(GameObject panelContainer, bool visible)
    {
        foreach (Transform child in panelContainer.transform)
        {
            if (child.GetComponent<Button>() != null)
            {
                child.gameObject.SetActive(visible);
            }
        }
    }

    private IEnumerator WaitForPlayerProgress()
    {
        if (_localNextButton != null) _localNextButton.gameObject.SetActive(true);
        _waitingForPlayerClick = true;
        
        while (_waitingForPlayerClick) yield return null;

        if (_localNextButton != null) _localNextButton.gameObject.SetActive(false);
    }

    private void OnLocalNextClicked()
    {
        if (_isAutoPlayingSequence) return;

        if (_audioSource != null) _audioSource.Stop();
        if (_audioPlayCoroutine != null) StopCoroutine(_audioPlayCoroutine);
        
        ResetCardVisualState(_currentAudioIndex);
        _waitingForPlayerClick = false; 
    }

    // 🔧 UPDATED: Cleaned up configuration mapping on repeat execution
    public void Repeat()
    {
        if (_audioSource != null) _audioSource.Stop();
        if (_audioPlayCoroutine != null) StopCoroutine(_audioPlayCoroutine);
        if (_cardsSequenceCoroutine != null) StopCoroutine(_cardsSequenceCoroutine);
        if (_masterSequenceCoroutine != null) StopCoroutine(_masterSequenceCoroutine);

        ResetCardVisualState(_currentAudioIndex);
        _waitingForPlayerClick = false;

        if (GameManager_Junior1A.Instance != null) GameManager_Junior1A.Instance.Next(false);

        if (_localNextButton != null)
        {
            _localNextButton.onClick.RemoveAllListeners();
            _localNextButton.onClick.AddListener(OnLocalNextClicked);
        }

        if (_currentPhase == 1)
        {
            _masterSequenceCoroutine = StartCoroutine(MasterTimelineSequence());
        }
        else if (_currentPhase == 2)
        {
            _phraseObj.SetActive(false);
            _introObj.SetActive(false);
            _mannerWordObj.SetActive(true);
            _masterSequenceCoroutine = StartCoroutine(RerunPhaseSequenceFromRepeat(2, _mannerWordObj, _mannerWordClips, _mannerIntroClip));
        }
        else if (_currentPhase == 3)
        {
            _introObj.SetActive(false);
            _mannerWordObj.SetActive(false);
            _phraseObj.SetActive(true);
            _masterSequenceCoroutine = StartCoroutine(RerunPhaseSequenceFromRepeat(3, _phraseObj, _phraseClips, _phraseIntroClip));
        }
    }

    // 🔧 UPDATED: Removed 'SetCardsVisibility(..., false)' to prevent elements from vanishing on repeat action
    private IEnumerator RerunPhaseSequenceFromRepeat(int phaseIndex, GameObject targetObj, AudioClip[] targetClips, AudioClip introClip)
    {
        _currentPhase = phaseIndex;
        _isAutoPlayingSequence = true;
        _currentActiveButtonParent = targetObj;
        _currentActiveClips = targetClips;

        if (_localNextButton != null) _localNextButton.gameObject.SetActive(false);

        if (_audioSource != null && introClip != null)
        {
            _audioSource.clip = introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(introClip.length);
        }

        // Passed 'false' here so it skips the pop sequence, keeping them visible
        _cardsSequenceCoroutine = StartCoroutine(PlayCardsSequence(targetObj, targetClips, false));
        yield return _cardsSequenceCoroutine;

        _isAutoPlayingSequence = false;
        yield return WaitForPlayerProgress();

        if (phaseIndex == 2)
        {
            _currentPhase = 3;
            _isAutoPlayingSequence = true;
            _mannerWordObj.SetActive(false);
            _phraseObj.SetActive(true);
            _currentActiveButtonParent = _phraseObj;
            _currentActiveClips = _phraseClips;

            if (_audioSource != null && _phraseIntroClip != null)
            {
                _audioSource.clip = _phraseIntroClip;
                _audioSource.Play();
                yield return new WaitForSeconds(_phraseIntroClip.length);
            }

            // Passed 'false' here as well
            _cardsSequenceCoroutine = StartCoroutine(PlayCardsSequence(_phraseObj, _phraseClips, false));
            yield return _cardsSequenceCoroutine;

            _isAutoPlayingSequence = false;
            yield return WaitForPlayerProgress();
        }

        _isViewed = true;
        if (_localNextButton != null) _localNextButton.gameObject.SetActive(false);
        GameManager_Junior1A.Instance?.Next(true);
    }

    public void PlayAudio(int index)
    {
        if (_isAutoPlayingSequence || _currentActiveButtonParent == null) return;
        
        if (_audioPlayCoroutine != null) StopCoroutine(_audioPlayCoroutine);
        
        ResetCardVisualState(_currentAudioIndex);
        
        int cleanCardIndex = ConvertChildIndexToCardIndex(_currentActiveButtonParent, index);
        _currentAudioIndex = cleanCardIndex != -1 ? cleanCardIndex : index;
        
        _audioPlayCoroutine = StartCoroutine(StartButtonAudio());
    }

    private IEnumerator StartButtonAudio()
    {
        SetCardActiveVisualState(_currentAudioIndex);

        if (_currentActiveClips != null && _currentAudioIndex < _currentActiveClips.Length && _currentActiveClips[_currentAudioIndex] != null && _audioSource != null)
        {
            _audioSource.clip = _currentActiveClips[_currentAudioIndex];
            _audioSource.Play();

            float currentPitch = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float waitTime = _currentActiveClips[_currentAudioIndex].length / currentPitch;
            yield return new WaitForSeconds(waitTime);
        }

        ResetCardVisualState(_currentAudioIndex);
    }

    public void Slow(TextMeshProUGUI text)
    {
        if (text != null) text.text = _isSlowed ? "    SLOW" : "    FAST";
        if (_audioSource != null) _audioSource.pitch = _isSlowed ? 1f : 0.75f;
        _isSlowed = !_isSlowed;
    }

    private void SetCardActiveVisualState(int cardIndex)
    {
        Transform targetCard = GetTransformOfCardAtIndex(_currentActiveButtonParent, cardIndex);
        if (targetCard == null) return;

        if (targetCard.TryGetComponent(out Image cardBg))
        {
            cardBg.color = new Color(1f, 0.9565453f, 0.4386792f, 1.0f);
        }

        if (targetCard.childCount > 0)
        {
            Transform innerContainer = targetCard.GetChild(0);
            if (innerContainer.childCount > 0)
            {
                Transform highlightObj = innerContainer.GetChild(0);
                if (highlightObj.TryGetComponent(out Image highlightImg)) highlightImg.enabled = true;
            }
        }
    }

    private void ResetCardVisualState(int cardIndex)
    {
        Transform targetCard = GetTransformOfCardAtIndex(_currentActiveButtonParent, cardIndex);
        if (targetCard == null) return;

        if (targetCard.TryGetComponent(out Image cardBg))
        {
            cardBg.color = Color.white;
        }

        if (targetCard.childCount > 0)
        {
            Transform innerContainer = targetCard.GetChild(0);
            if (innerContainer.childCount > 0)
            {
                Transform highlightObj = innerContainer.GetChild(0);
                if (highlightObj.TryGetComponent(out Image highlightImg)) highlightImg.enabled = false;
            }
        }
    }

    private Transform GetTransformOfCardAtIndex(GameObject panel, int cardIndex)
    {
        if (panel == null) return null;
        int currentCardCount = 0;
        foreach (Transform child in panel.transform)
        {
            if (child.GetComponent<Button>() != null)
            {
                if (currentCardCount == cardIndex) return child;
                currentCardCount++;
            }
        }
        return null;
    }

    private int ConvertChildIndexToCardIndex(GameObject panel, int rawChildIndex)
    {
        if (panel == null || rawChildIndex >= panel.transform.childCount) return -1;
        int cardCounter = 0;
        for (int i = 0; i < panel.transform.childCount; i++)
        {
            Transform child = panel.transform.GetChild(i);
            if (child.GetComponent<Button>() != null)
            {
                if (i == rawChildIndex) return cardCounter;
                cardCounter++;
            }
        }
        return -1;
    }
}