using System.Collections;
using System.Collections.Generic; // Added for HashSet tracking
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U2_R01_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [Header("Audio Engine Setup")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;
    [SerializeField] private AudioClip[] _clips;

    [Header("Layout Target References")]
    [SerializeField] private Transform _cardParent; // Linked to ButtonParent in Inspector
    
    [Header("UI Text Displays")]
    [SerializeField] private TextMeshProUGUI _scoreText; // 👈 Drag your Scene Score TextMeshPro here!

    [Header("Runtime State Matrices")]
    [SerializeField] private int _currentAudioIndex = 0;
    [SerializeField] private bool _isViewed = false;
    [SerializeField] private bool _isSlowed = false;

    [Header("Score Metrics Matrix")]
    [SerializeField] private int _currentScore = 0;
    private HashSet<int> _clickedCards = new HashSet<int>();

    private Coroutine _coroutine;
    private Coroutine _repeatCoroutine;

    public bool IsViewed => _isViewed;

    private void OnEnable() => StartCoroutine(Starter());

    private IEnumerator Starter()
    {
        if (_cardParent == null)
        {
            Debug.LogError("❌ Card Parent reference is completely missing in inspector allocation fields!");
            yield break;
        }

        // Reset runtime scoring matrices
        _clickedCards.Clear();
        _currentScore = 0;
        UpdateScoreUI();

        // Initially hide all card items during introduction
        foreach (Transform button in _cardParent) button.gameObject.SetActive(false);
        
        // Safety check to handle footer navigation layer activation sequences
        if (transform.childCount > 0)
        {
            Transform footer = transform.GetChild(transform.childCount - 1);
            if (footer.childCount > 1) footer.GetChild(1).gameObject.SetActive(false);
        }

        // Reset active visual selection indicators safely
        ResetCardVisualState(_currentAudioIndex);
        _currentAudioIndex = 0;

        // Play introduction narrative audio file sequence
        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length);
        }

        // Sequentially spawn all card layout wrappers with their pop mechanics
        foreach (Transform button in _cardParent)
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

        // 💡 AUTO-RUN OMISSION: Hand controls directly to user interactions instead of iterating clicks
        EnableUserInteraction();
    }

    public void PlayAudio(int index)
    {
        if (_cardParent == null) return;

        if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);
        
        // Clear active visual states on current index before switching focuses
        ResetCardVisualState(_currentAudioIndex);
        
        _currentAudioIndex = index;
        
        if (_coroutine != null) StopCoroutine(_coroutine);
        _coroutine = StartCoroutine(StartButtonAudio());

        // Process discrete item scoring metrics safely
        if (!_clickedCards.Contains(index))
        {
            _clickedCards.Add(index);
            _currentScore = _clickedCards.Count;
            UpdateScoreUI();
        }

        // Trigger step-completion sequences once all items are checked out
        if (_clips != null && _currentScore >= _clips.Length)
        {
            _isViewed = true;
            if (GameManager_Junior1B.Instance != null)
            {
                GameManager_Junior1B.Instance.Next(true);
            }
        }
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

    public void Repeat()
    {
        if (_cardParent == null) return;

        ResetCardVisualState(_currentAudioIndex);

        if (_coroutine != null) StopCoroutine(_coroutine);
        if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);

        // Reset score layouts when player starts anew via repeat button mechanisms
        _clickedCards.Clear();
        _currentScore = 0;
        UpdateScoreUI();
        
        PlayAudio(0);
    }

    public void Slow(TextMeshProUGUI text)
    {
        if (text != null) text.text = _isSlowed ? "    SLOW" : "    FAST";
        if (_audioSource != null) _audioSource.pitch = _isSlowed ? 1f : 0.75f;
        _isSlowed = !_isSlowed;
    }

    private void EnableUserInteraction()
    {
        foreach (Transform button in _cardParent)
        {
            if (button.TryGetComponent(out Button btn)) btn.interactable = true;
        }

        if (transform.childCount > 0)
        {
            Transform footer = transform.GetChild(transform.childCount - 1);
            if (footer.childCount > 1) footer.GetChild(1).gameObject.SetActive(true);
        }
    }

    private void UpdateScoreUI()
    {
        if (_scoreText != null)
        {
            int maxClips = (_clips != null) ? _clips.Length : 0;
            _scoreText.text = $"{_currentScore} / {maxClips}";
        }
    }

    // --- SAFE DEFENSIVE LAYERING UI METHODS ---

    private void SetCardActiveVisualState(int index)
    {
        if (_cardParent == null || index >= _cardParent.childCount) return;

        Transform targetCard = _cardParent.GetChild(index);

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
        if (_cardParent == null || index >= _cardParent.childCount) return;

        Transform targetCard = _cardParent.GetChild(index);

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
}