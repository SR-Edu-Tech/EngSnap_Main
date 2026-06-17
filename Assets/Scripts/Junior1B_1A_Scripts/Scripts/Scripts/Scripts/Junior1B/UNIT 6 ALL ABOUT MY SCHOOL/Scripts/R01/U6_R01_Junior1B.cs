using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U6_R01_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [Header("=== Audio Elements ===")]
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _clips;
    [SerializeField] int _currentAudioIndex = 0;

    [Header("=== UI Component Layout ===")]
    [Tooltip("The 'Button Parent' GameObject that holds all your SCHOOL buttons.")]
    [SerializeField] Transform _cardParent;
    [SerializeField] TextMeshProUGUI _clickedIndexText;

    [Header("=== State Colors ===")]
    [SerializeField] Color _defaultColor = Color.white;
    [SerializeField] Color _playingColor = new Color(1f, 0.92f, 0.016f, 1f); // Yellow
    [SerializeField] Color _finishedColor = new Color(0.133f, 0.694f, 0.298f, 1f); // Green

    [Header("=== Progress Trackers ===")]
    [SerializeField] List<int> _clickCheckIndex = new List<int>();
    [SerializeField] bool _isViewed = false;    
    [SerializeField] bool _isSlowed = false;
    
    private Coroutine _sequenceCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        _sequenceCoroutine = StartCoroutine(StarterTab1());
    }

    IEnumerator StarterTab1()
    {
        _clickCheckIndex.Clear();
        _currentAudioIndex = 0;
        _clickedIndexText.text = "0/" + _clips.Length;
        _isSlowed = false;
        if (_audioSource != null) _audioSource.pitch = 1f;

        // Reset all SCHOOL buttons to their default state
        foreach (Transform schoolButton in _cardParent)
        {
            if (schoolButton.TryGetComponent(out Popeffect_Junior1B pop)) pop.enabled = true;
            if (schoolButton.TryGetComponent(out Image buttonBg)) buttonBg.color = _defaultColor;
            if (schoolButton.TryGetComponent(out Button btn)) btn.interactable = false;
            
            schoolButton.gameObject.SetActive(false);
        }

        // Play introduction voice track
        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            
            yield return new WaitForSeconds(_introClip.length / 2f);
            foreach (Transform schoolButton in _cardParent) schoolButton.gameObject.SetActive(true);
            yield return new WaitForSeconds(_introClip.length / 2f);
        }
        else
        {
            foreach (Transform schoolButton in _cardParent) schoolButton.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.5f);
        }

        // Keep button clicks locked during automated presentation run
        foreach (Transform schoolButton in _cardParent)
        {
            if (schoolButton.TryGetComponent(out Button btn)) btn.interactable = false;
        }

        // Kick off the automated sequential autoplay chain
        yield return StartCoroutine(AutoPlaySequence());
    }

    IEnumerator AutoPlaySequence()
    {
        for (int i = 0; i < _clips.Length; i++)
        {
            if (i >= _cardParent.childCount) break;

            _currentAudioIndex = i;
            Transform currentButton = _cardParent.GetChild(_currentAudioIndex);

            // Update text tracking UI dynamically
            _clickedIndexText.text = (i + 1).ToString() + "/" + _clips.Length;
            if (!_clickCheckIndex.Contains(i)) _clickCheckIndex.Add(i);

            // 1. WHILE PLAYING STATE ➡️ Turn the SCHOOL button image yellow
            if (currentButton.TryGetComponent(out Image buttonBg)) buttonBg.color = _playingColor;

            // Load and configure active loop clip
            _audioSource.clip = _clips[_currentAudioIndex];
            _audioSource.pitch = _isSlowed ? 0.7f : 1.0f;
            _audioSource.Play();

            // Dynamic check frame waiting loop so pitch changes scale length properly on the fly
            while (_audioSource.isPlaying)
            {
                yield return null;
            }

            // 2. FINISHED PLAYING STATE ➡️ Turn the SCHOOL button image solid green
            if (currentButton.TryGetComponent(out Image completeBg)) completeBg.color = _finishedColor;

            // Small split-second breathing gap buffer between separate tracks
            yield return new WaitForSeconds(0.25f);
        }

        // 3. COMPLETE STATE ➡️ Re-enable standard interaction and fire module next scene callbacks
        foreach (Transform schoolButton in _cardParent)
        {
            if (schoolButton.TryGetComponent(out Button btn)) btn.interactable = true;
        }

        _isViewed = true;
        if (GameManager_Junior1B.Instance != null)
        {
            GameManager_Junior1B.Instance.Next(true);
        }
    }

    /// <summary>
    /// UI Click Event Hook for your Slow Button Panel.
    /// </summary>
    public void Slow(TextMeshProUGUI slowButtonText)
    {
        if (_audioSource == null) return;

        _isSlowed = !_isSlowed;
        _audioSource.pitch = _isSlowed ? 0.7f : 1.0f;

        if (slowButtonText != null)
        {
            slowButtonText.text = _isSlowed ? "    FAST" : "    SLOW";
        }

        if (_audioSource.isPlaying)
        {
            float currentPlaybackTime = _audioSource.time;
            _audioSource.Stop();
            _audioSource.Play();
            _audioSource.time = currentPlaybackTime; 
        }
    }

    /// <summary>
    /// UI Click Event Hook for your Repeat Button.
    /// </summary>
    public void Repeat()
    {
        if (_currentAudioIndex >= _clips.Length || _audioSource == null) return;

        _audioSource.Stop();
        _audioSource.pitch = _isSlowed ? 0.7f : 1.0f;
        _audioSource.clip = _clips[_currentAudioIndex];
        _audioSource.Play();
    }

    // Fallback manual click function if child buttons are pressed after autoplay finishes
    public void PlayAudio(int index)
    {
        if (index >= _clips.Length || index >= _cardParent.childCount) return;

        _currentAudioIndex = index;
        _audioSource.clip = _clips[_currentAudioIndex];
        _audioSource.pitch = _isSlowed ? 0.7f : 1.0f;
        _audioSource.Play();
    }
}