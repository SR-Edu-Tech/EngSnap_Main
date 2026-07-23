
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U6_L02_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [Header("=== State Trackers ===")]
    [SerializeField] bool _isViewed = false;
    [SerializeField] bool _containerOpened = false;
    private bool _waitingForPlayerInput = false;

    [Header("=== UI Component Layout ===")]
    [Tooltip("The parent object holding the entire Scroll View setup (Tab_1Data).")]
    [SerializeField] GameObject _dialogueContainerParent;
    [Tooltip("The actual Content GameObject inside the Viewport that holds StudentD1, TeacherD1, etc.")]
    [SerializeField] Transform _contentGrid;
    [SerializeField] Image _currentSpeakerIcon;

    [Header("=== Input Field Box Elements ===")]
    [Tooltip("The shared UI Panel overlay holding your input elements.")]
    [SerializeField] GameObject _inputBoxParent;
    [Tooltip("The TMP Input Field component where the player types.")]
    [SerializeField] TMP_InputField _schoolInputField;
    [Tooltip("The submit/confirmation button next to the input field.")]
    [SerializeField] Button _inputSubmitButton;

    [Header("=== Audio Elements ===")]
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _introClip;
    [SerializeField] AudioClip[] _dialogueAudioClips;
    [SerializeField] int _currentAudioClipIndex;

    private Coroutine _audioCoroutine;
    private Coroutine _sequenceCoroutine;

    public static Dictionary<int, string> SavedInputs { get; private set; } = new Dictionary<int, string>();
    private int _activeInputIndex = -1;

    public bool IsViewed => _isViewed;

    void OnEnable()
    {
        _containerOpened = false;
        _waitingForPlayerInput = false;
        SavedInputs.Clear();
        
        if (_inputBoxParent != null) _inputBoxParent.SetActive(false);
        if (_inputSubmitButton != null) _inputSubmitButton.onClick.AddListener(OnSubmitPlayerInput);
        
        if (_dialogueContainerParent != null)
        {
            _dialogueContainerParent.SetActive(false);
        }

        _sequenceCoroutine = StartCoroutine(StartAutomaticContainerFlow());
    }

    void OnDisable()
    {
        if (_inputSubmitButton != null) _inputSubmitButton.onClick.RemoveListener(OnSubmitPlayerInput);
    }

    IEnumerator StartAutomaticContainerFlow()
    {
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

        if (_dialogueContainerParent != null)
        {
            _dialogueContainerParent.SetActive(true);
            
            if (_dialogueContainerParent.TryGetComponent(out CanvasGroup cg))
            {
                cg.alpha = 1f;
            }
        }

        if (_contentGrid != null)
        {
            foreach (Transform dialogueRow in _contentGrid) 
            {
                dialogueRow.gameObject.SetActive(true);
                if (dialogueRow.TryGetComponent(out Button btn)) btn.interactable = false;
            }
            
            _contentGrid.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }

        yield return new WaitForSeconds(0.3f);
        
        _sequenceCoroutine = StartCoroutine(AutoRunAudios());
    }

    IEnumerator AutoRunAudios()
    {     
        if (_contentGrid == null) yield break;

        foreach (Transform child in _contentGrid)
        {
            if (child.TryGetComponent(out Button btn)) btn.interactable = false;
        }

        int currentLoopIndex = 0;
        int totalDialogueCount = _contentGrid.childCount;

        foreach (Transform child in _contentGrid)
        {
            if (child.TryGetComponent(out Button btn))
            {
                btn.onClick.Invoke();
                
                float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
                float waitTime = 0.5f;
                
                if (_audioSource != null && _audioSource.clip != null)
                {
                    waitTime = (_audioSource.clip.length / pV1) + 0.4f;
                }
                
                yield return new WaitForSeconds(waitTime);

                // FIXED FLOW: Only register input updates if it is NOT the very last element in the grid
                bool isLastRow = (currentLoopIndex == totalDialogueCount - 1);

                if (!isLastRow && (child.name.Contains("Teacher") || currentLoopIndex % 2 != 0))
                {
                    OpenPlayerInputInterface(currentLoopIndex);
                    
                    while (_waitingForPlayerInput)
                    {
                        yield return null;
                    }
                }
            }
            currentLoopIndex++;
        }

        foreach (Transform child in _contentGrid)
        {
            if (child.TryGetComponent(out Button btn)) btn.interactable = true;
        }

        _containerOpened = true;

        if (_containerOpened && !_isViewed)
        {
            _isViewed = true;
            if (GameManager_Junior1B.Instance != null)
            {
                GameManager_Junior1B.Instance.Next(true);
            }
        }
    }

    private void OpenPlayerInputInterface(int dialogueIndex)
    {
        _waitingForPlayerInput = true;
        _activeInputIndex = dialogueIndex;
        
        if (_inputBoxParent != null) _inputBoxParent.SetActive(true);
        
        if (_schoolInputField != null)
        {
            _schoolInputField.text = "";
            _schoolInputField.ActivateInputField();
        }
    }

    public void OnSubmitPlayerInput()
    {
        if (_schoolInputField == null || string.IsNullOrEmpty(_schoolInputField.text)) return;

        if (SavedInputs.ContainsKey(_activeInputIndex))
            SavedInputs[_activeInputIndex] = _schoolInputField.text;
        else
            SavedInputs.Add(_activeInputIndex, _schoolInputField.text);

        if (_inputBoxParent != null) _inputBoxParent.SetActive(false);
        _waitingForPlayerInput = false;
    }

    public void OnSpeaker(Image speakerIcon)
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        _currentSpeakerIcon = speakerIcon;
        _currentSpeakerIcon.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
    }

    public void PlayAudio(int index)
    {
        if (_contentGrid == null) return;

        _currentAudioClipIndex = index;
        Transform targetButton = _contentGrid.GetChild(index);
        Sprite btnSprite = null;
        
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

        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        _audioCoroutine = StartCoroutine(PlayAudioIndex());
    }

    IEnumerator PlayAudioIndex()
    {
        if (_dialogueAudioClips == null || _currentAudioClipIndex >= _dialogueAudioClips.Length) yield break;

        _audioSource.clip = _dialogueAudioClips[_currentAudioClipIndex];
        _audioSource.Play();
        
        float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
        float aL1 = _audioSource.clip.length / pV1;
        
        yield return new WaitForSeconds(aL1);
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
    }
}

