using System;
using System.Text;
using System.Collections;
using System.Collections.Generic; // ⬅️ Restored to fix the HashSet compiler error
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class U6_R02_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [Header("=== State Trackers ===")]
    [SerializeField] bool _isViewed = false;
    private HashSet<int> _completedIndices = new HashSet<int>();

    [Header("=== UI Component Layout ===")]
    [Tooltip("The parent object holding the entire Scroll View setup (Tab_1Data).")]
    [SerializeField] GameObject _dialogueContainerParent;
    [Tooltip("The actual Content GameObject inside the Viewport that holds StudentD1, TeacherD1, etc.")]
    [SerializeField] Transform _contentGrid;
    [SerializeField] Image _currentSpeakerIcon;

    [Header("=== Audio Elements ===")]
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _dialogueAudioClips;
    [SerializeField] int _currentAudioClipIndex;

    private Coroutine _audioCoroutine;
    private Coroutine _introCoroutine;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        _completedIndices.Clear();
        
        // Keep the container hidden on startup while intro plays
        if (_dialogueContainerParent != null)
        {
            _dialogueContainerParent.SetActive(false);
        }

        // Initialize button lines
        if (_contentGrid != null)
        {
            foreach (Transform dialogueRow in _contentGrid) 
            {
                dialogueRow.gameObject.SetActive(true);
                if (dialogueRow.TryGetComponent(out Button btn)) btn.interactable = false;
            }
        }

        _introCoroutine = StartCoroutine(PlayIntroThenEnableInteraction());
    }

    IEnumerator PlayIntroThenEnableInteraction()
    {
        // 1. Play introduction voice track first while screen is clean
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

        // 2. Intro finished! Turn on dialogue containers completely
        if (_dialogueContainerParent != null)
        {
            _dialogueContainerParent.SetActive(true);
            
            if (_dialogueContainerParent.TryGetComponent(out CanvasGroup cg))
            {
                cg.alpha = 1f;
            }
        }

        // 3. Make buttons interactive for manual clicking and position layout correctly
        if (_contentGrid != null)
        {
            foreach (Transform dialogueRow in _contentGrid) 
            {
                if (dialogueRow.TryGetComponent(out Button btn)) btn.interactable = true;
            }
            _contentGrid.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }
    }

    public void OnSpeaker(Image speakerIcon)
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        _currentSpeakerIcon = speakerIcon;
        _currentSpeakerIcon.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
    }

    public void PlayAudio(int index)
    {
        if (_contentGrid == null || index >= _contentGrid.childCount) return;

        _currentAudioClipIndex = index;
        Transform targetButton = _contentGrid.GetChild(index);
        Sprite btnSprite = null;
        
        // --- HANDLE PORTRAIT AVATAR CHANGES ---
        Transform iconTransform = targetButton.Find("Mask/Icon") ?? targetButton.Find("StudentD/Mask/Icon") ?? targetButton.Find("TeacherD/Mask/Icon");
        if (iconTransform == null && targetButton.childCount > 0) 
            iconTransform = targetButton.GetChild(0).Find("Icon") ?? targetButton.GetChild(0).GetChild(0);

        if (iconTransform != null && iconTransform.TryGetComponent(out Image img))
        {
            btnSprite = img.sprite;
        }

        int visualTargetIndex = (index % 2 == 0) ? 1 : 2;
        if (_dialogueContainerParent != null && _dialogueContainerParent.transform.childCount > visualTargetIndex)
        {
            Transform targetVisualContainer = _dialogueContainerParent.transform.GetChild(visualTargetIndex);
            
            if (btnSprite != null && targetVisualContainer.TryGetComponent(out Image targetImg))
            {
                targetImg.sprite = btnSprite;
            }
            
            if (targetVisualContainer.TryGetComponent(out Popeffect_Junior1B pop))
            {
                pop.enabled = false;
                pop.enabled = true;
            }
        }

        // --- FIRING AUDIO CLIPS ---
        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        _audioCoroutine = StartCoroutine(PlayAudioIndex(index));
    }

    IEnumerator PlayAudioIndex(int index)
    {
        if (_dialogueAudioClips == null || index >= _dialogueAudioClips.Length) yield break;

        _audioSource.clip = _dialogueAudioClips[index];
        _audioSource.Play();
        
        float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
        float aL1 = _audioSource.clip.length / pV1;
        
        yield return new WaitForSeconds(aL1);
        
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        
        _completedIndices.Add(index);

        // Check if all dialogues inside the grid have been played at least once to unlock Next button
        if (_contentGrid != null && _completedIndices.Count >= _contentGrid.childCount && !_isViewed)
        {
            _isViewed = true;
            if (GameManager_Junior1B.Instance != null)
            {
                GameManager_Junior1B.Instance.Next(true);
            }
        }
    }
}