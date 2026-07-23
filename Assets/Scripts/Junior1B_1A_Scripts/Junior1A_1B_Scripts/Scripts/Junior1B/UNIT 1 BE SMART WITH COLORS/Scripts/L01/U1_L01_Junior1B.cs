using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U1_L01_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _introClip;
    [SerializeField] private AudioClip[] _audioClips;
    [SerializeField] private Transform _buttonParent;
    [SerializeField] private Transform _optionParent;
    [SerializeField] private int _currentAudioIndex = 0;
    [SerializeField] private bool _isViewed;
    [SerializeField] private bool _isSlowed;
    
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
        }
    }

    void OnEnable()
    {
        StartCoroutine(Starter());
    }

    IEnumerator Starter()
    {
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

        _coroutine = StartCoroutine(AutoStart());
    }

    IEnumerator SetText(int index)
    {
        // Turn off the old active highlight image overlay frame
        SetHighlightImageState(_currentAudioIndex, false);
        
        _currentAudioIndex = index;
        
        // Turn on the new highlight frame overlay
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

    IEnumerator AutoStart()
    {
        _currentAudioIndex = 0;
        yield return new WaitForSeconds(1f);

        if (_audioClips != null)
        {
            for (int i = 0; i < _audioClips.Length; i++)
            {
                _currentAudioIndex = i;
                AudioClip clip = _audioClips[i];

                SetHighlightImageState(_currentAudioIndex, true);

                if (_audioSource != null && clip != null)
                {
                    _audioSource.clip = clip;
                    _audioSource.Play();
                    yield return new WaitForSeconds(clip.length + 0.5f);
                }
                else
                {
                    yield return new WaitForSeconds(1f);
                }

                SetHighlightImageState(_currentAudioIndex, false);

                // Assuming last clip completion activates the next section
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
        if (_coroutine != null) StopCoroutine(_coroutine);
        if (_audioSource != null) _audioSource.Stop();

        SetHighlightImageState(_currentAudioIndex, false);

        if (_buttonParent != null)
        {
            foreach (Transform child in _buttonParent)
            {
                var btn = child.GetComponent<Button>();
                if (btn != null) btn.interactable = true;
            }
        }

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

    /// <summary>
    /// Safely handles turning the selection highlight image inside a button on or off.
    /// </summary>
    private void SetHighlightImageState(int buttonIndex, bool targetState)
    {
        if (_buttonParent == null || buttonIndex >= _buttonParent.childCount) return;

        Transform buttonTransform = _buttonParent.GetChild(buttonIndex);
        
        // Target the separate overlay graphic object inside the button structure.
        // If your highlight frame is the FIRST child object underneath the button, childCount layout index 0 handles it:
        if (buttonTransform.childCount > 0)
        {
            Image highlightImage = buttonTransform.GetChild(0).GetComponent<Image>();
            if (highlightImage != null)
            {
                highlightImage.enabled = targetState;
            }
        }
    }
}