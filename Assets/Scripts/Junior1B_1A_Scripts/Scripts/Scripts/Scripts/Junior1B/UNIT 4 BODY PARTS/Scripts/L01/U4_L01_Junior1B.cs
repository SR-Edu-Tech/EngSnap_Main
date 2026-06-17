using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U4_L01_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [Header("=== Audio Setup ===")]
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;
    [SerializeField] private AudioClip[] _audioClips;

    [Header("=== Automation Containers ===")]
    [Tooltip("Drag the parent container containing all the audio trigger buttons here.")]
    [SerializeField] private Transform _buttonParent;
    [SerializeField] private Transform _optionParent;

    [Header("=== Visual Styling ===")]
    [SerializeField] private Color _playedCorrectColor = Color.green;
    [SerializeField] private float _scaleUpMultiplier = 1.15f; // Scales up to 115% size
    [SerializeField] private float _animationSpeed = 10f;

    [Header("=== State Variables ===")]
    [SerializeField] private int _currentAudioIndex = 0;
    [SerializeField] private bool _isViewed;
    [SerializeField] private bool _isSlowed;
    
    private List<GameObject> _dynamicButtons = new List<GameObject>();
    private Dictionary<int, Color> _originalButtonColors = new Dictionary<int, Color>();

    private Coroutine _coroutine;
    private Coroutine _setCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        GatherAndHookupButtons();
        StartCoroutine(Starter());
    }

    private void GatherAndHookupButtons()
    {
        _dynamicButtons.Clear();
        _originalButtonColors.Clear();

        if (_buttonParent == null) return;

        for (int i = 0; i < _buttonParent.childCount; i++)
        {
            GameObject buttonObj = _buttonParent.GetChild(i).gameObject;
            _dynamicButtons.Add(buttonObj);

            // Force button scale clean safety state at structural bootup frames
            buttonObj.transform.localScale = Vector3.one;

            if (buttonObj.TryGetComponent(out Image img))
            {
                _originalButtonColors[i] = img.color;
            }
            else
            {
                _originalButtonColors[i] = Color.white;
            }

            if (buttonObj.TryGetComponent(out Button btn))
            {
                int indexBackup = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => SetAudioClip(indexBackup));
            }
        }
    }

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
        }
    }

    IEnumerator Starter()
    {
        ResetAllButtonVisualStates();

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

        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
        }

        if (_optionParent != null && _optionParent.childCount > 1)
        {
            _optionParent.GetChild(1).gameObject.SetActive(false);
        }

        SetButtonsInteractableState(false);

        if (_introClip != null)
        {
            yield return new WaitForSeconds(_introClip.length);
        }
        else
        {
            yield return new WaitForSeconds(1f);
        }

        _coroutine = StartCoroutine(AutoStart());
    }

    IEnumerator SetText(int index)
    {
        ResetSingleButtonVisual(_currentAudioIndex);
        _currentAudioIndex = index;

        if (_audioClips != null && index < _audioClips.Length && _audioClips[index] != null)
        {
            yield return StartCoroutine(AnimateButtonVisualSequence(index, _audioClips[index].length));
        }
        else
        {
            yield return StartCoroutine(AnimateButtonVisualSequence(index, 1f));
        }
    }

    IEnumerator AutoStart()
    {
        _currentAudioIndex = 0;
        yield return new WaitForSeconds(0.5f);

        if (_audioClips != null)
        {
            for (int i = 0; i < _audioClips.Length; i++)
            {
                _currentAudioIndex = i;
                AudioClip clip = _audioClips[i];

                if (_audioSource != null && clip != null)
                {
                    _audioSource.clip = clip;
                    _audioSource.Play();
                    yield return StartCoroutine(AnimateButtonVisualSequence(i, clip.length));
                }
                else
                {
                    yield return StartCoroutine(AnimateButtonVisualSequence(i, 1f));
                }

                if (_currentAudioIndex == _audioClips.Length - 1)
                {
                    _isViewed = true;
                    if (GameManager_Junior1B.Instance != null)
                    {
                        GameManager_Junior1B.Instance.Next(true);
                    }
                }
            }
        }

        _currentAudioIndex = 0;
        
        if (_optionParent != null && _optionParent.childCount > 1)
        {
            _optionParent.GetChild(1).gameObject.SetActive(true);
        }

        SetButtonsInteractableState(true);
    }

    /// <summary>
    /// Handles the explicit clean scale-up to target size, then drops back to Vector3.one + changes color to green
    /// </summary>
    IEnumerator AnimateButtonVisualSequence(int targetIndex, float duration)
    {
        if (targetIndex >= _dynamicButtons.Count || _dynamicButtons[targetIndex] == null) yield break;

        Transform btnTransform = _dynamicButtons[targetIndex].transform;
        
        // 💡 FIX: Explicitly enforce Vector3.one baseline instead of reading container cache properties
        Vector3 defaultBaseScale = Vector3.one;
        Vector3 popTargetScale = defaultBaseScale * _scaleUpMultiplier;

        float elapsed = 0f;

        // 1. Smoothly Scale Up to 115% size
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            btnTransform.localScale = Vector3.Lerp(btnTransform.localScale, popTargetScale, Time.deltaTime * _animationSpeed);
            yield return null;
        }
        btnTransform.localScale = popTargetScale;

        // 2. Play out the rest of the audio clip track duration
        float remainingAudioTime = duration - 0.15f;
        if (remainingAudioTime > 0)
        {
            yield return new WaitForSeconds(remainingAudioTime);
        }

        // 3. Audio Completed -> Change background tint color to green
        if (_dynamicButtons[targetIndex].TryGetComponent(out Image img))
        {
            img.color = _playedCorrectColor;
        }

        // 4. Smoothly drop scale exactly back down to original size (1.0)
        elapsed = 0f;
        while (elapsed < 0.2f)
        {
            elapsed += Time.deltaTime;
            btnTransform.localScale = Vector3.Lerp(btnTransform.localScale, defaultBaseScale, Time.deltaTime * _animationSpeed);
            yield return null;
        }
        btnTransform.localScale = defaultBaseScale;
    }

    private void SetButtonsInteractableState(bool isInteractable)
    {
        foreach (GameObject btnObj in _dynamicButtons)
        {
            if (btnObj != null && btnObj.TryGetComponent(out Button btn))
            {
                btn.interactable = isInteractable;
            }
        }
    }

    private void ResetSingleButtonVisual(int index)
    {
        if (index >= _dynamicButtons.Count || _dynamicButtons[index] == null) return;

        _dynamicButtons[index].transform.localScale = Vector3.one;
        if (_dynamicButtons[index].TryGetComponent(out Image img) && _originalButtonColors.ContainsKey(index))
        {
            img.color = _originalButtonColors[index];
        }
    }

    private void ResetAllButtonVisualStates()
    {
        for (int i = 0; i < _dynamicButtons.Count; i++)
        {
            ResetSingleButtonVisual(i);
        }
    }

    public void Repeat()
    {
        if (_coroutine != null) StopCoroutine(_coroutine);
        if (_setCoroutine != null) StopCoroutine(_setCoroutine);
        if (_audioSource != null) _audioSource.Stop();

        ResetAllButtonVisualStates();
        SetButtonsInteractableState(true);

        _coroutine = StartCoroutine(AutoStart());
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
}