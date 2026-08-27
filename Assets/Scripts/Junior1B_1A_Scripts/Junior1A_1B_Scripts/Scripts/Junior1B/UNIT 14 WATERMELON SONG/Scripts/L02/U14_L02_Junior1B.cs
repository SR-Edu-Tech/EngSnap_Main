using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U14_L02_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [SerializeField] bool _isViewed = false;

    [Header("UI Containers")]
    [SerializeField] GameObject _containerPanel;
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _audioClips;
    [SerializeField] Image _currentSpeakerIcon;
    [SerializeField] int _currentAudioClipIndex = 0;

    Coroutine _audioCoroutine, _initSequenceCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        if (_initSequenceCoroutine != null) StopCoroutine(_initSequenceCoroutine);
        _isViewed = false;

        if (_containerPanel != null && _containerPanel.TryGetComponent(out CanvasGroup cg))
            cg.alpha = 1f;

        _initSequenceCoroutine = StartCoroutine(RunSimpleSequence());
    }

    IEnumerator RunSimpleSequence()
    {
        float pitchFactor = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;

        // 1. Play the Intro Audio immediately
        if (_audioSource != null && _introClip != null)
        {
            _audioSource.clip = _introClip;
            _audioSource.Play();
            yield return new WaitForSeconds(_introClip.length / pitchFactor);
        }

        // 2. Play the Index Audio right after with zero delay
        PlayAudio(_currentAudioClipIndex);

        // Wait for index audio to complete before finishing the game step
        if (_audioClips != null && _currentAudioClipIndex < _audioClips.Length && _audioClips[_currentAudioClipIndex] != null)
        {
            yield return new WaitForSeconds(_audioClips[_currentAudioClipIndex].length / pitchFactor);
        }

        if (!_isViewed)
        {
            _isViewed = true;
            GameManager_Junior1B.Instance.Next(true);
        }
    }

    public void OnSpeaker(Image speakerIcon)
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        _currentSpeakerIcon = speakerIcon;
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
    }

    public void PlayAudio(int index)
    {
        _currentAudioClipIndex = index;

        // Safety check for audio array boundaries
        if (_audioClips == null || index < 0 || index >= _audioClips.Length || _audioClips[index] == null) return;

        // --- DIRECT AUDIO PLAYBACK (Bypasses UI dependency so sound ALWAYS plays) ---
        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        _audioCoroutine = StartCoroutine(PlayAudioIndex());

        // --- SAFE VISUALS PATH ---
        // If the UI hierarchy fails, the sound will still keep playing perfectly
        try
        {
            ScrollRect scrollComponent = _containerPanel.GetComponentInChildren<ScrollRect>();
            Transform contentParent = (scrollComponent != null) ? scrollComponent.content : _containerPanel.transform.GetChild(0);

            if (contentParent != null && index < contentParent.childCount)
            {
                Transform targetRow = contentParent.GetChild(index); // Tab1_Data
                Sprite btnSprite = null;

                if (targetRow.TryGetComponent(out Image rowImg))
                {
                    btnSprite = rowImg.sprite;
                }

                // Tab1_Data -> Text (0) -> Speaker icon (0)
                if (targetRow.childCount > 0 && targetRow.GetChild(0).childCount > 0)
                {
                    Transform speakerChild = targetRow.GetChild(0).GetChild(0);
                    if (speakerChild.TryGetComponent(out Image speakerImg))
                    {
                        OnSpeaker(speakerImg);
                    }
                }

                // Side Accent Banner Bounce
                int bounceTargetIndex = (index % 2 == 0) ? 1 : 2;
                if (_containerPanel.transform.childCount > bounceTargetIndex)
                {
                    Transform sidePanel = _containerPanel.transform.GetChild(bounceTargetIndex);
                    if (sidePanel != null)
                    {
                        sidePanel.localScale = Vector3.one;
                        if (btnSprite != null && sidePanel.TryGetComponent(out Image sideImg))
                            sideImg.sprite = btnSprite;

                        if (sidePanel.TryGetComponent(out Popeffect_Junior1B pop))
                        {
                            pop.enabled = false;
                            pop.enabled = true;
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Visual UI elements or side panels could not be resolved, but audio will continue. Error details: " + e.Message);
        }
    }

    IEnumerator PlayAudioIndex()
    {
        AudioClip activeClip = _audioClips[_currentAudioClipIndex];

        if (_audioSource != null && activeClip != null)
        {
            _audioSource.clip = activeClip;
            _audioSource.Play();
            float pitchFactor = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            yield return new WaitForSeconds(activeClip.length / pitchFactor);
        }

        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
    }
}