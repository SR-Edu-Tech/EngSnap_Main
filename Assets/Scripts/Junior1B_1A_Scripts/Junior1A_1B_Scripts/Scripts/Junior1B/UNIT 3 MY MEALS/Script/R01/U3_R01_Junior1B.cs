using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U3_R01_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [Header("Audio Configurations")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;
    [SerializeField] private AudioClip[] _audioClips;

    [Header("Layout Target References")]
    [SerializeField] private Transform _buttonParent;
    [SerializeField] private Transform _optionParent;
    
    [Header("UI Text Displays")]
    [SerializeField] private TextMeshProUGUI _scoreText;

    [Header("Visual Configurations")]
    // 💡 NEW FEATURE: Customize your clicked color directly from the Inspector!
    [SerializeField] private Color _clickedColor = Color.green; 

    [Header("Runtime State Matrices")]
    [SerializeField] private int _currentAudioIndex = 0;
    [SerializeField] private bool _isViewed;
    [SerializeField] private bool _isSlowed;
    
    [Header("Score Tracking")]
    [SerializeField] private int _currentScore = 0; 
    private HashSet<int> _clickedButtons = new HashSet<int>();

    private Coroutine _coroutine;
    private Coroutine _setCoroutine;

    public bool IsViewed => _isViewed;

    public void SetAudioClip(int index)
    {
        if (_audioSource == null) return;

        _audioSource.Stop();
        if (index >= 0 && index < _audioClips.Length)
        {
            _audioSource.clip = _audioClips[index];
            _audioSource.Play();
            
            if (_setCoroutine != null) StopCoroutine(_setCoroutine);
            if (_coroutine != null) StopCoroutine(_coroutine);
            _setCoroutine = StartCoroutine(SetText(index));

            // 💡 NEW FEATURE: Turn the target button's image background green
            if (_buttonParent != null && index < _buttonParent.childCount)
            {
                if (_buttonParent.GetChild(index).TryGetComponent(out Image btnImg))
                {
                    btnImg.color = _clickedColor;
                }
            }

            // Track unique score metrics
            if (!_clickedButtons.Contains(index))
            {
                _clickedButtons.Add(index);
                _currentScore = _clickedButtons.Count;
                
                // Update the UI Text instantly
                UpdateScoreUI();
            }

            // Task completion triggers when all unique layout buttons have been listened to
            if (_currentScore >= _audioClips.Length)
            {
                _isViewed = true;
                if (GameManager_Junior1B.Instance != null)
                {
                    GameManager_Junior1B.Instance.Next(true);
                }
            }
        }
    }

    void OnEnable()
    {
        StartCoroutine(Starter());
    }

    IEnumerator Starter()
    {
        // Reset buttons to their base visual color configuration
        ResetButtonVisualColors();
        
        _clickedButtons.Clear();
        _currentScore = 0;
        UpdateScoreUI();

        // 1. Enable Pop Effects safely
        if (_buttonParent != null)
        {
            foreach (Transform button in _buttonParent)
            {
                var pop = button.GetComponent<Popeffect_Junior1B>();
                if (pop != null) pop.enabled = true;
            }
        }

        if (_optionParent != null && _optionParent.childCount > 0)
        {
            var optionPop = _optionParent.GetChild(0).GetComponent<Popeffect_Junior1B>();
            if (optionPop != null) optionPop.enabled = true;
        }

        // 2. Play Intro Audio Track
        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
        }

        // 3. Hide secondary option asset
        if (_optionParent != null && _optionParent.childCount > 1)
        {
            _optionParent.GetChild(1).gameObject.SetActive(false);
        }

        // 4. Lock button interactability and turn off highlight overlay during intro
        if (_buttonParent != null)
        {
            foreach (Transform child in _buttonParent)
            {
                var btn = child.GetComponent<Button>();
                if (btn != null) btn.interactable = false;
            }

            SetHighlightImageState(_currentAudioIndex, false);
        }

        // 5. Wait for intro track to complete
        if (_introClip != null)
        {
            yield return new WaitForSeconds(_introClip.length);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        // 6. Intro Complete: Enable user controls
        EnableUserInteraction();
    }

    IEnumerator SetText(int index)
    {
        SetHighlightImageState(_currentAudioIndex, false);
        _currentAudioIndex = index;
        SetHighlightImageState(index, true);

        if (_audioClips != null && index < _audioClips.Length && _audioClips[index] != null)
        {
            yield return new WaitForSeconds(_audioClips[index].length + 0.5f);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        SetHighlightImageState(index, false);
    }

    private void EnableUserInteraction()
    {
        _currentAudioIndex = 0;
        
        if (_optionParent != null && _optionParent.childCount > 1)
        {
            _optionParent.GetChild(1).gameObject.SetActive(true);
        }

        if (_buttonParent != null)
        {
            foreach (Transform child in _buttonParent)
            {
                var btn = child.GetComponent<Button>();
                if (btn != null) btn.interactable = true;
            }
        }
    }

    public void Repeat()
    {
        if (_setCoroutine != null) StopCoroutine(_setCoroutine);
        if (_coroutine != null) StopCoroutine(_coroutine);
        if (_audioSource != null) _audioSource.Stop();

        SetHighlightImageState(_currentAudioIndex, false);
        
        // 💡 Clear visual states on repeating game loops
        ResetButtonVisualColors();

        _clickedButtons.Clear();
        _currentScore = 0;
        UpdateScoreUI();

        SetAudioClip(0);
    }

    public void Slow(TextMeshProUGUI slowButtonText)
    {
        if (_audioSource == null) return;

        if (slowButtonText != null)
        {
            slowButtonText.text = _isSlowed ? "    SLOW" : "    FAST";
        }
        
        _audioSource.pitch = _isSlowed ? 1f : 0.75f;
        _isSlowed = !_isSlowed;
    }

    private void UpdateScoreUI()
    {
        if (_scoreText != null)
        {
            int totalClips = (_audioClips != null) ? _audioClips.Length : 0;
            _scoreText.text = $"{_currentScore} / {totalClips}";
        }
    }

    private void SetHighlightImageState(int buttonIndex, bool targetState)
    {
        if (_buttonParent == null || buttonIndex >= _buttonParent.childCount) return;

        Transform buttonTransform = _buttonParent.GetChild(buttonIndex);
        
        if (buttonTransform.childCount > 0)
        {
            Image highlightImage = buttonTransform.GetChild(0).GetComponent<Image>();
            if (highlightImage != null)
            {
                highlightImage.enabled = targetState;
            }
        }
    }

    // 💡 NEW HELPER METHOD: Sweeps and restores original white background base states cleanly
    private void ResetButtonVisualColors()
    {
        if (_buttonParent == null) return;
        foreach (Transform button in _buttonParent)
        {
            if (button.TryGetComponent(out Image img))
            {
                img.color = Color.white;
            }
        }
    }
}