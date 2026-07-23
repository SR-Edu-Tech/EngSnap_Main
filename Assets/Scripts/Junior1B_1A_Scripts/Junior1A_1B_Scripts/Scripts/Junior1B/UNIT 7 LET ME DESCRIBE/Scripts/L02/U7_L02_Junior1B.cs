
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U7_L02_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [Header("Audio Engine Setup")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;
    [Tooltip("Intro clips for each individual button parent theme respectively.")]
    [SerializeField] private AudioClip[] _groupIntroClips;
    [SerializeField] private AudioClip[] _clips;

    [Header("Layout Target References")]
    [Tooltip("Assign your 4 distinct button theme groups here in sequential layout order.")]
    [SerializeField] private Transform[] _cardParents; 

    [Header("Runtime State Matrices")]
    [SerializeField] private int _activeGroupIndex = 0;
    [SerializeField] private int _currentAudioIndex = 0;
    [SerializeField] private bool _isViewed = false;
    [SerializeField] private bool _isSlowed = false;

    private Coroutine _coroutine;
    private Coroutine _repeatCoroutine;

    public bool IsViewed => _isViewed;

    private void OnEnable() => StartCoroutine(Starter());

    private IEnumerator Starter()
    {
        if (_cardParents == null || _cardParents.Length == 0)
        {
            Debug.LogError("❌ Card Parents array reference is completely missing or empty!");
            yield break;
        }

        // Hide the footer next button layer tracking setups on startup
        if (transform.childCount > 0)
        {
            Transform footer = transform.GetChild(transform.childCount - 1);
            if (footer.childCount > 1) footer.GetChild(1).gameObject.SetActive(false);
        }

        // 1. Master Intro plays over a clean initial screen state
        DeactivateAllParents();
        _activeGroupIndex = 0;
        _currentAudioIndex = 0;

        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length);
        }

        // 2. Run the main sequential flow loop
        yield return StartCoroutine(RunSequenceFlow());
    }

    private IEnumerator RunSequenceFlow()
    {
        // Iterate through each card parent group sequentially
        for (int g = _activeGroupIndex; g < _cardParents.Length; g++)
        {
            _activeGroupIndex = g;
            Transform activeParent = _cardParents[_activeGroupIndex];
            if (activeParent == null) continue;

            // Activate the parent container frame
            activeParent.gameObject.SetActive(true);

            // Play Group Intro if available
            if (_groupIntroClips != null && _activeGroupIndex < _groupIntroClips.Length && _groupIntroClips[_activeGroupIndex] != null)
            {
                _audioSource.clip = _groupIntroClips[_activeGroupIndex];
                _audioSource.Play();
                yield return new WaitForSeconds(_audioSource.clip.length);
            }
            else
            {
                yield return new WaitForSeconds(0.3f);
            }

            // Sequentially spawn and pop all card layout items in the active parent group
            foreach (Transform button in activeParent)
            {
                button.gameObject.SetActive(true);
                
                if (button.TryGetComponent(out Popeffect_Junior1B pop)) 
                {
                    pop.enabled = false; 
                    pop.enabled = true;
                    yield return new WaitForSeconds(pop.PopDuration + 0.15f);
                }
                else
                {
                    yield return new WaitForSeconds(0.5f);
                }
            }

            // Autoplay active group audios one by one automatically
            foreach (Transform button in activeParent)
            {
                if (button.TryGetComponent(out Button btn))
                {
                    btn.onClick.Invoke();
                }

                if (_clips != null && _currentAudioIndex < _clips.Length && _clips[_currentAudioIndex] != null)
                {
                    float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
                    float aL1 = _clips[_currentAudioIndex].length / pV1;
                    yield return new WaitForSeconds(aL1);
                }
                else
                {
                    yield return new WaitForSeconds(1.0f);
                }
            }

            // Enable interaction settings for the completed group
            foreach (Transform button in activeParent)
            {
                if (button.TryGetComponent(out Button btn)) btn.interactable = true;
            }

            // If it's not the final layout, hide this group to make room for the next one
            if (g < _cardParents.Length - 1)
            {
                activeParent.gameObject.SetActive(false);
            }
        }

        // Complete cycle, reveal navigation targets
        if (transform.childCount > 0)
        {
            Transform footer = transform.GetChild(transform.childCount - 1);
            if (footer.childCount > 1) footer.GetChild(1).gameObject.SetActive(true);
        }

        if (GameManager_Junior1B.Instance != null) GameManager_Junior1B.Instance.Next(true);
        _isViewed = true;
    }

    public void PlayAudio(int index)
    {
        if (_cardParents == null || _activeGroupIndex >= _cardParents.Length) return;

        if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);
        
        ResetCardVisualState(_currentAudioIndex);
        _currentAudioIndex = index;
        
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(StartButtonAudio());
    }

    private IEnumerator StartButtonAudio()
    {
        SetCardActiveVisualState(_currentAudioIndex);

        if (_clips != null && _currentAudioIndex < _clips.Length && _clips[_currentAudioIndex] != null && _audioSource != null)
        {
            _audioSource.clip = _clips[_currentAudioIndex];
            _audioSource.Play();

            float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float aL1 = _clips[_currentAudioIndex].length / pV1;
            yield return new WaitForSeconds(aL1);
        }

        ResetCardVisualState(_currentAudioIndex);
    }

    // FIXED: The Repeat function now stops all operations, resets back to Parent 0, and replays the sequence
    public void Repeat()
    {
        if (_cardParents == null || _cardParents.Length == 0) return;

        // Clean up any ongoing animations/audios immediately
        ResetCardVisualState(_currentAudioIndex);
        if (_coroutine != null) StopCoroutine(_coroutine);
        if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);

        _repeatCoroutine = StartCoroutine(RepeatFromBeginningFlow());
    }

    private IEnumerator RepeatFromBeginningFlow()
    {
        // 1. Wipe layout states back to baseline zero
        DeactivateAllParents();
        _activeGroupIndex = 0;
        _currentAudioIndex = 0;

        // 2. Hand control back over to the automated sequence framework loop
        yield return StartCoroutine(RunSequenceFlow());
    }

    public void Slow(TextMeshProUGUI text)
    {
        if (text != null) text.text = _isSlowed ? "    SLOW" : "    FAST";
        if (_audioSource != null) _audioSource.pitch = _isSlowed ? 1f : 0.75f;
        _isSlowed = !_isSlowed;
    }

    private void DeactivateAllParents()
    {
        foreach (Transform parent in _cardParents)
        {
            if (parent != null)
            {
                foreach (Transform button in parent) button.gameObject.SetActive(false);
                parent.gameObject.SetActive(false);
            }
        }
    }

    private void SetCardActiveVisualState(int index)
    {
        Transform targetCard = GetCardTransformFromIndex(index);
        if (targetCard == null) return;

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

    private void ResetCardVisualState(int index)
    {
        Transform targetCard = GetCardTransformFromIndex(index);
        if (targetCard == null) return;

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

    private Transform GetCardTransformFromIndex(int globalIndex)
    {
        int accumulatedCount = 0;
        for (int i = 0; i < _cardParents.Length; i++)
        {
            if (_cardParents[i] == null) continue;
            int countInThisParent = _cardParents[i].childCount;

            if (globalIndex >= accumulatedCount && globalIndex < accumulatedCount + countInThisParent)
            {
                int localIndex = globalIndex - accumulatedCount;
                return _cardParents[i].GetChild(localIndex);
            }
            accumulatedCount += countInThisParent;
        }
        return null;
    }
}

