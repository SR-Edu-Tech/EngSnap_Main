using Junior2A;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U6_G01_Junior2A : MonoBehaviour, Interfaces_Junior2A
{
    [Header("Audio Configurations")]
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip _correctClickClip;
    [SerializeField] AudioClip _incorrectClickClip;
    [SerializeField] AudioClip _finalRewardClip;

    [Header("UI & Structure")]
    [SerializeField] TextMeshProUGUI _progressText;
    [SerializeField] Color _defaultColor = Color.white;
    [SerializeField] Color _correctColor = Color.green;
    [SerializeField] Color _wrongColor = Color.red;

    [Header("State Tracking")]
    [SerializeField] int _expectedNextIndex = 0;
    [SerializeField] int _totalTargetCount = 5;
    [SerializeField] bool _isViewed = false;

    private List<Button> _clickedCorrectButtons = new List<Button>();
    private bool _isProcessingInput = false;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        ResetSequenceGame();

        if (_audioSource && _introClip)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
        }
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// Bind this function to EACH button's OnClick() listener in the Unity Inspector.
    /// Pass 0 for the first button, 1 for the second, etc.
    /// </summary>
    public void PlaceIndex(int index)
    {
        // Block inputs during a wrong-answer penalty freeze, after victory, or if clicking an already cleared step
        if (_isViewed || _isProcessingInput || index < _expectedNextIndex) return;

        // Grab the button component currently executing this event
        Button clickedButton = UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject?.GetComponent<Button>();
        if (clickedButton == null) return;

        if (index == _expectedNextIndex)
        {
            // Correct click: Turn green instantly
            if (clickedButton.TryGetComponent(out Image img))
            {
                img.color = _correctColor;
            }

            clickedButton.interactable = false;
            _clickedCorrectButtons.Add(clickedButton);

            _expectedNextIndex++;
            UpdateProgressUI();

            if (_audioSource && _correctClickClip)
            {
                _audioSource.PlayOneShot(_correctClickClip);
            }

            if (_expectedNextIndex >= _totalTargetCount)
            {
                StartCoroutine(CompleteSequenceSequence());
            }
        }
        else
        {
            // Wrong click sequence breaker
            StartCoroutine(HandleWrongClickFeedback(clickedButton));
        }
    }

    IEnumerator HandleWrongClickFeedback(Button failedButton)
    {
        _isProcessingInput = true;

        if (failedButton.TryGetComponent(out Image img))
        {
            img.color = _wrongColor;
        }

        if (_audioSource && _incorrectClickClip)
        {
            _audioSource.PlayOneShot(_incorrectClickClip);
        }

        // Freeze for visual feedback
        yield return new WaitForSeconds(0.6f);

        // Reset everything back to default parameters
        ResetSequenceGame();

        _isProcessingInput = false;
    }

    IEnumerator CompleteSequenceSequence()
    {
        _isViewed = true;

        if (_audioSource && _finalRewardClip)
        {
            _audioSource.clip = _finalRewardClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_finalRewardClip.length);
        }

        if (GameManager_Junior2A.Instance != null)
        {
            GameManager_Junior2A.Instance.Next(true);
        }
    }

    private void ResetSequenceGame()
    {
        _expectedNextIndex = 0;

        // Reset only the buttons that were modified/turned green back to active states
        foreach (var btn in _clickedCorrectButtons)
        {
            if (btn != null)
            {
                btn.interactable = true;
                if (btn.TryGetComponent(out Image img)) img.color = _defaultColor;
            }
        }
        _clickedCorrectButtons.Clear();

        // Find the button that was just flashed red inside the Hierarchy structure and fix its color too
        GameObject activeSelection = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
        if (activeSelection != null && activeSelection.TryGetComponent(out Image activeImg))
        {
            activeImg.color = _defaultColor;
        }

        UpdateProgressUI();
    }

    private void UpdateProgressUI()
    {
        if (_progressText != null)
        {
            _progressText.text = $"{_expectedNextIndex}/{_totalTargetCount}";
        }
    }
}