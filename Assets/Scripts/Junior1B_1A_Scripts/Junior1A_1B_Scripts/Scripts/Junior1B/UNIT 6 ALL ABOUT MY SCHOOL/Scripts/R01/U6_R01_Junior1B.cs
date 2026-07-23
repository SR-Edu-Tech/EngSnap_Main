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
    
    private Coroutine _audioTrackingCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        StartCoroutine(StarterTab1());
    }

    IEnumerator StarterTab1()
    {
        _clickCheckIndex.Clear();
        _currentAudioIndex = 0;
        _clickedIndexText.text = "0/" + _clips.Length;
        _isSlowed = false;
        _isViewed = false;
        if (_audioSource != null) _audioSource.pitch = 1f;

        // Reset all school buttons to initial state and keep them temporarily interactive=false during intro
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

        // Intro finished! Turn the buttons ON so the user can freely click them
        UnlockButtonsForPlayer();
    }

    private void UnlockButtonsForPlayer()
    {
        foreach (Transform schoolButton in _cardParent)
        {
            if (schoolButton.TryGetComponent(out Button btn)) btn.interactable = true;
        }
    }

    /// <summary>
    /// Put this function directly on the OnClick() event of your individual card buttons!
    /// Pass 0 for Card 1, 1 for Card 2, etc.
    /// </summary>
    public void PlayAudio(int index)
    {
        if (index >= _clips.Length || index >= _cardParent.childCount || _audioSource == null) return;

        // Reset any previously highlighted "yellow/playing" card back to finished/green if it was already clicked
        ResetCardVisualStates();

        _currentAudioIndex = index;

        // Track distinct card views
        if (!_clickCheckIndex.Contains(index))
        {
            _clickCheckIndex.Add(index);
            _clickedIndexText.text = _clickCheckIndex.Count.ToString() + "/" + _clips.Length;
        }

        // 1. WHILE PLAYING STATE ➡️ Highlight current clicked button yellow
        Transform currentButton = _cardParent.GetChild(_currentAudioIndex);
        if (currentButton.TryGetComponent(out Image buttonBg)) buttonBg.color = _playingColor;

        // Execute audio clip playback
        _audioSource.Stop();
        _audioSource.clip = _clips[_currentAudioIndex];
        _audioSource.pitch = _isSlowed ? 0.7f : 1.0f;
        _audioSource.Play();

        // Monitor completion tracker loop cleanly
        if (_audioTrackingCoroutine != null) StopCoroutine(_audioTrackingCoroutine);
        _audioTrackingCoroutine = StartCoroutine(TrackAudioCompletion(currentButton));
    }

    IEnumerator TrackAudioCompletion(Transform activeCard)
    {
        while (_audioSource.isPlaying)
        {
            yield return null;
        }

        // 2. FINISHED PLAYING STATE ➡️ Switch active button color to solid green
        if (activeCard.TryGetComponent(out Image completeBg)) completeBg.color = _finishedColor;

        // 3. CHECK COMPLETE CRITERIA ➡️ Trigger scene progression next scene if all unique cards were clicked
        if (_clickCheckIndex.Count >= _clips.Length && !_isViewed)
        {
            _isViewed = true;
            if (GameManager_Junior1B.Instance != null)
            {
                GameManager_Junior1B.Instance.Next(true);
            }
        }
    }

    private void ResetCardVisualStates()
    {
        for (int i = 0; i < _cardParent.childCount; i++)
        {
            Transform card = _cardParent.GetChild(i);
            if (card.TryGetComponent(out Image img))
            {
                // If it has been viewed in the past, keep it green. Otherwise leave white.
                if (_clickCheckIndex.Contains(i))
                {
                    img.color = _finishedColor;
                }
                else
                {
                    img.color = _defaultColor;
                }
            }
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

        // Reinforce yellow color state if repeat is targeted mid-activity
        Transform currentButton = _cardParent.GetChild(_currentAudioIndex);
        if (currentButton.TryGetComponent(out Image buttonBg)) buttonBg.color = _playingColor;

        if (_audioTrackingCoroutine != null) StopCoroutine(_audioTrackingCoroutine);
        _audioTrackingCoroutine = StartCoroutine(TrackAudioCompletion(currentButton));
    }
}