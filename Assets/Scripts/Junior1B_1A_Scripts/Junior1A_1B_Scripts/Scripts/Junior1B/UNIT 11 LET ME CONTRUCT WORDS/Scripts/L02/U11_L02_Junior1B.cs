using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class U11_L02_Junior1B : MonoBehaviour, Interfaces_Junior1B
{
    [Header("=== State Trackers ===")]
    [SerializeField] bool _isViewed = false;
    [SerializeField] bool _containerOpened = false;

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
        SavedInputs.Clear();

        if (_inputBoxParent != null) _inputBoxParent.SetActive(false);

        if (_inputSubmitButton != null)
        {
            _inputSubmitButton.onClick.RemoveListener(OnSubmitPlayerInput);
            _inputSubmitButton.onClick.AddListener(OnSubmitPlayerInput);
        }

        if (_dialogueContainerParent != null)
        {
            _dialogueContainerParent.SetActive(false);
        }

        _sequenceCoroutine = StartCoroutine(StartManualContainerFlow());
    }

    void OnDisable()
    {
        if (_inputSubmitButton != null) _inputSubmitButton.onClick.RemoveListener(OnSubmitPlayerInput);
    }

    IEnumerator StartManualContainerFlow()
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
                if (dialogueRow.TryGetComponent(out Button btn)) btn.interactable = true;
            }

            if (_contentGrid.TryGetComponent(out RectTransform rect))
            {
                rect.anchoredPosition = Vector2.zero;
            }
        }

        _containerOpened = true;
    }

    public void PlayAudio(int index)
    {
        if (_contentGrid == null || index < 0 || index >= _contentGrid.childCount) return;

        _currentAudioClipIndex = index;
        Transform targetButton = _contentGrid.GetChild(index);

        if (index % 2 != 0)
        {
            OpenPlayerInputInterface(index);
            return;
        }

        if (targetButton.TryGetComponent(out Image clickImg)) OnSpeaker(clickImg);
        else if (targetButton.childCount > 0 && targetButton.GetChild(0).TryGetComponent(out Image childImg)) OnSpeaker(childImg);

        int visualTargetIndex = 1;
        if (_dialogueContainerParent != null && _dialogueContainerParent.transform.childCount > visualTargetIndex)
        {
            Transform targetVisualContainer = _dialogueContainerParent.transform.GetChild(visualTargetIndex);

            if (targetVisualContainer.TryGetComponent(out Popeffect_Junior1B pop))
            {
                pop.enabled = false;
                pop.enabled = true;
            }
        }

        if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
        _audioCoroutine = StartCoroutine(PlayAudioIndex());
    }

    private void OpenPlayerInputInterface(int dialogueIndex)
    {
        _activeInputIndex = dialogueIndex;

        if (_inputBoxParent != null) _inputBoxParent.SetActive(true);

        if (_schoolInputField != null)
        {
            // If they already typed something here before, keep it so they can edit it, otherwise clear it
            _schoolInputField.text = SavedInputs.ContainsKey(dialogueIndex) ? SavedInputs[dialogueIndex] : "";
            _schoolInputField.ActivateInputField();
        }
    }

    public void OnSubmitPlayerInput()
    {
        if (_schoolInputField == null || string.IsNullOrEmpty(_schoolInputField.text)) return;
        if (_contentGrid == null || _activeInputIndex < 0 || _activeInputIndex >= _contentGrid.childCount) return;

        if (SavedInputs.ContainsKey(_activeInputIndex))
            SavedInputs[_activeInputIndex] = _schoolInputField.text;
        else
            SavedInputs.Add(_activeInputIndex, _schoolInputField.text);

        Transform targetRow = _contentGrid.GetChild(_activeInputIndex);
        TextMeshProUGUI rowText = targetRow.GetComponentInChildren<TextMeshProUGUI>();

        if (rowText != null)
        {
            rowText.text = _schoolInputField.text;
        }

        if (_inputBoxParent != null) _inputBoxParent.SetActive(false);

        CheckCompletionState();
    }

    private void CheckCompletionState()
    {
        if (_contentGrid == null || !_containerOpened || _isViewed) return;

        // Calculate how many odd (input text field) rows are inside your grid layout
        int expectedInputCount = 0;
        for (int i = 0; i < _contentGrid.childCount; i++)
        {
            if (i % 2 != 0) expectedInputCount++;
        }

        // FIXED: Only advance the Next screen manager condition once EVERY odd row is answered
        if (SavedInputs.Count >= expectedInputCount)
        {
            _isViewed = true;
            if (GameManager_Junior1B.Instance != null)
            {
                GameManager_Junior1B.Instance.Next(true);
            }
        }
    }

    public void OnSpeaker(Image speakerIcon)
    {
        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        _currentSpeakerIcon = speakerIcon;
        _currentSpeakerIcon.color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
    }

    IEnumerator PlayAudioIndex()
    {
        if (_dialogueAudioClips == null || _currentAudioClipIndex >= _dialogueAudioClips.Length || _currentAudioClipIndex < 0) yield break;

        if (_audioSource != null && _dialogueAudioClips[_currentAudioClipIndex] != null)
        {
            _audioSource.clip = _dialogueAudioClips[_currentAudioClipIndex];
            _audioSource.Play();

            float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float aL1 = _audioSource.clip.length / pV1;

            yield return new WaitForSeconds(aL1);
        }

        if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
    }
}