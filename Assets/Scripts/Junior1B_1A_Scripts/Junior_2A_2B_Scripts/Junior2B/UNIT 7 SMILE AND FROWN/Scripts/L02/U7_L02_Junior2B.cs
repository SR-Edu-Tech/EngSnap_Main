using Junior2B;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U7_L02_Junior2B : MonoBehaviour, Interfaces_Junior2B
{
    [SerializeField] bool _isCurrentLeft = false, _isViewed = false;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] Transform _char1TextObj, _char2TextObj, _buttonParent;
    [SerializeField] int _currentAudioIndex = 0;
    
    Coroutine _coroutine, _buttonCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable() => _coroutine = StartCoroutine(AutoStart());
    
    IEnumerator AutoStart()
    {
        if (_buttonParent != null)
        {
            foreach (Transform button in _buttonParent) button.gameObject.SetActive(false);
        }
        
        if (_char2TextObj != null) _char2TextObj.gameObject.SetActive(false);
        if (_char1TextObj != null) _char1TextObj.gameObject.SetActive(false);
        
        _currentAudioIndex = 0;

        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        if (_buttonParent != null)
        {
            // 💡 FIX 1: Loop dynamically using a structured for-loop so the index timing matches perfectly
            for (int i = 0; i < _buttonParent.childCount; i++)
            {
                Transform button = _buttonParent.GetChild(i);
                if (button.gameObject.activeSelf == false)
                {
                    button.gameObject.SetActive(true);
                }

                if (button.TryGetComponent(out Button btn))
                {
                    btn.interactable = false;
                    
                    // Force the index tracker to change BEFORE invoking the click sequence
                    _currentAudioIndex = i; 
                    
                    // Determine side dynamically: Even index (0, 2, 4...) = Left, Odd index (1, 3, 5...) = Right
                    SetSide(i % 2 == 0);

                    btn.onClick.Invoke();
                }

                if (_audioClips != null && i < _audioClips.Length && _audioClips[i] != null)
                {
                    yield return new WaitForSeconds(_audioClips[i].length + 0.3f);
                }
                else
                {
                    yield return new WaitForSeconds(1.0f);
                }
            }
        }
        
        _isViewed = true;
        if (GameManager_Junior2B.Instance != null)
        {
            GameManager_Junior2B.Instance.Next(true);
        }
        
        _currentAudioIndex = 0;
        if (_buttonParent != null)
        {
            foreach (Transform button in _buttonParent)
            {
                if (button.TryGetComponent(out Button btn)) btn.interactable = true;
            }
        }
    }
    
    public void SetSide(bool isLeft) => _isCurrentLeft = isLeft;
    
    // 💡 FIX 2: Dynamic fallback for manual user clicks!
    // If a user clicks manually, this automatically figures out if the button is Left or Right based on its hierarchy position.
    public void PlayAudio(int index)
    {
        _currentAudioIndex = index;
        
        // Auto-detect side layout location dynamically from child array position
        SetSide(index % 2 == 0);

        if (_buttonCoroutine != null) StopCoroutine(_buttonCoroutine);
        _buttonCoroutine = StartCoroutine(StartButtonAudio());
    }
    
    IEnumerator StartButtonAudio()
    {
        yield return null;

        if (_buttonParent == null || _currentAudioIndex >= _buttonParent.childCount) yield break;
        Transform currentButton = _buttonParent.GetChild(_currentAudioIndex);

        var elements = ExtractButtonElements(currentButton);

        // Turn off the opposite dialogue panel completely so they alternate back and forth cleanly
        if (_isCurrentLeft)
        {
            if (_char2TextObj != null) _char2TextObj.gameObject.SetActive(false);
        }
        else
        {
            if (_char1TextObj != null) _char1TextObj.gameObject.SetActive(false);
        }

        Transform targetCharObj = _isCurrentLeft ? _char1TextObj : _char2TextObj;

        if (targetCharObj != null)
        {
            if (targetCharObj.TryGetComponent(out Popeffect_Junior2B pop))
            {
                pop.enabled = false;
                pop.enabled = true;
            }

            // Target Layout Mapping: Parent -> Image (Child 0) -> Text (TMP)
            if (targetCharObj.childCount > 0)
            {
                Transform imageChild = targetCharObj.GetChild(0);
                
                if (imageChild.TryGetComponent(out Image targetImg) && elements.ButtonIcon != null)
                {
                    targetImg.sprite = elements.ButtonIcon.sprite;
                }

                var txtComponent = imageChild.GetComponentInChildren<TextMeshProUGUI>();
                if (txtComponent != null && elements.ButtonText != null)
                {
                    txtComponent.text = elements.ButtonText.text;
                }
            }

            targetCharObj.gameObject.SetActive(true);
        }

        if (_audioSource != null && _audioClips != null && _currentAudioIndex < _audioClips.Length)
        {
            if (_audioClips[_currentAudioIndex] != null)
            {
                _audioSource.clip = _audioClips[_currentAudioIndex];
                _audioSource.Play();
            }
        }
    }

    private (TextMeshProUGUI ButtonText, Image ButtonIcon) ExtractButtonElements(Transform root)
    {
        TextMeshProUGUI text = root.GetComponentInChildren<TextMeshProUGUI>();
        Image finalIcon = null;

        Image[] images = root.GetComponentsInChildren<Image>();
        foreach (Image img in images)
        {
            if (img.transform != root) 
            {
                finalIcon = img; 
                break;
            }
        }

        return (text, finalIcon);
    }
}