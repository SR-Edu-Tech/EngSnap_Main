using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable] // Added so it displays perfectly in the Unity Inspector!
public class ContractionData2
{
    [Header("Text Configuration")]
    public string LongFormText;
    public string ShortFormText;

    [Header("Audio Configuration")]
    [Tooltip("Audio clip pronouncing the long form text (e.g., 'I am')")]
    public AudioClip LongFormAudio;
    [Tooltip("Audio clip pronouncing the short form text (e.g., 'I'm')")]
    public AudioClip ShortFormAudio;
}

public class U15_L02_Junior1A : MonoBehaviour
{
    [Header("Audio Components")]
    [SerializeField] private AudioSource _audioSource;
    [Tooltip("Intro clip that plays right when the lesson starts before buttons spawn.")]
    [SerializeField] private AudioClip _introAudio;

    [Header("Contraction Configurations")]
    [Tooltip("Set this array size to 8 and fill in the corresponding texts and audio clips.")]
    [SerializeField] private ContractionData2[] _contractionRules;

    [Header("Shake Animation Settings")]
    [SerializeField] private float _shakeDuration = 0.5f;
    [SerializeField] private float _shakeMagnitude = 15f;

    [Header("UI Component Links")]
    [Tooltip("Drag the parent container holding your 8 cards here.")]
    [SerializeField] private Transform _cardParent;

    [Tooltip("Drag the Repeat button here so it can be locked/unlocked automatically.")]
    [SerializeField] private Button _repeatButton;

    [Tooltip("Drag the Slow button here so it can be locked/unlocked automatically.")]
    [SerializeField] private Button _slowButton;

    [Header("Visual Feedback (Colors)")]
    [SerializeField] private Color _normalColor = Color.white;
    [SerializeField] private Color _activeColor = new Color(1f, 0.9565453f, 0.4386792f, 1.0f);
    [SerializeField] private Color _completedColor = Color.green;

    [Header("Timing / Speed Controls")]
    [Tooltip("Time delay between each button spawning in.")]
    [SerializeField] private float _spawnStaggerDelay = 0.8f;

    private int _currentAudioIndex = 0;
    private bool _isSlowed = false;
    private bool _isLessonRunning = false;

    private Coroutine _coroutine;
    private Coroutine _repeatCoroutine;

    private void Start()
    {
        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();

        InitializeButtonLayout();
        SetInteractableState(false);
        
        if (_slowButton != null) _slowButton.interactable = true;

        _coroutine = StartCoroutine(AutomatedLessonFlowRoutine());
    }

    private void InitializeButtonLayout()
    {
        if (_cardParent == null || _contractionRules == null) return;

        int totalItems = Mathf.Min(_cardParent.childCount, _contractionRules.Length);

        for (int i = 0; i < totalItems; i++)
        {
            Transform button = _cardParent.GetChild(i);

            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
            if (buttonText != null) buttonText.text = _contractionRules[i].LongFormText;

            button.GetComponent<Image>().color = _normalColor;
            SetNestedHighlightActive(button, false);

            if (button.TryGetComponent(out Button btn)) btn.interactable = false;

            button.gameObject.SetActive(false);
        }
    }

    private void SetInteractableState(bool interactable)
    {
        if (_repeatButton != null) _repeatButton.interactable = interactable;
    }

    private IEnumerator AutomatedLessonFlowRoutine()
    {
        _isLessonRunning = true;
        SetInteractableState(false);

        if (_cardParent == null) yield break;
        int totalItems = Mathf.Min(_cardParent.childCount, _contractionRules.Length);

        if (_introAudio != null && _audioSource != null)
        {
            _audioSource.clip = _introAudio;
            _audioSource.Play();
            
            float audioDuration = GetScaledAudioLength(_introAudio) + 0.5f;
            float elapsedAudio = 0f;
            while (elapsedAudio < audioDuration)
            {
                elapsedAudio += Time.deltaTime;
                audioDuration = GetScaledAudioLength(_introAudio) + 0.5f;
                yield return null;
            }
        }

        for (int i = 0; i < totalItems; i++)
        {
            _cardParent.GetChild(i).gameObject.SetActive(true);
            yield return new WaitForSeconds(_spawnStaggerDelay);
        }

        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < totalItems; i++)
        {
            _currentAudioIndex = i;
            yield return StartCoroutine(PlayContractionAnimationStep(i));
        }

        _isLessonRunning = false;
        SetInteractableState(true);
        GameManager_Junior1A.Instance.Next(true);
    }

    private IEnumerator PlayContractionAnimationStep(int index)
    {
        Transform button = _cardParent.GetChild(index);
        ContractionData2 data = _contractionRules[index]; // Fixed: Changed type from ContractionData to ContractionData2
        TMP_Text textMesh = button.GetComponentInChildren<TMP_Text>();

        button.GetComponent<Image>().color = _activeColor;
        SetNestedHighlightActive(button, true);

        if (data.LongFormAudio != null && _audioSource != null)
        {
            _audioSource.clip = data.LongFormAudio;
            _audioSource.Play();
            
            float audioDuration = GetScaledAudioLength(data.LongFormAudio) + 0.1f;
            float elapsedAudio = 0f;
            while (elapsedAudio < audioDuration)
            {
                elapsedAudio += Time.deltaTime;
                audioDuration = GetScaledAudioLength(data.LongFormAudio) + 0.1f;
                yield return null;
            }
        }

        Vector3 originalPosition = button.localPosition;
        float elapsed = 0f;
        while (elapsed < _shakeDuration)
        {
            elapsed += Time.deltaTime;
            float offsetX = UnityEngine.Random.Range(-1f, 1f) * _shakeMagnitude;
            float offsetY = UnityEngine.Random.Range(-1f, 1f) * _shakeMagnitude;
            button.localPosition = new Vector3(originalPosition.x + offsetX, originalPosition.y + offsetY, originalPosition.z);
            yield return null;
        }
        button.localPosition = originalPosition;

        if (textMesh != null) textMesh.text = data.ShortFormText;

        if (data.ShortFormAudio != null && _audioSource != null)
        {
            _audioSource.clip = data.ShortFormAudio;
            _audioSource.Play();
            
            float audioDuration = GetScaledAudioLength(data.ShortFormAudio) + 0.4f;
            float elapsedAudio = 0f;
            while (elapsedAudio < audioDuration)
            {
                elapsedAudio += Time.deltaTime;
                audioDuration = GetScaledAudioLength(data.ShortFormAudio) + 0.4f;
                yield return null;
            }
        }

        button.GetComponent<Image>().color = _completedColor;
        SetNestedHighlightActive(button, false);
    }

    public void Repeat()
    {
        if (_isLessonRunning) return;
        if (_cardParent == null) return;

        if (_coroutine != null) StopCoroutine(_coroutine);
        if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);
        if (_audioSource != null) _audioSource.Stop();

        foreach (Transform button in _cardParent)
        {
            button.GetComponent<Image>().color = _normalColor;
            SetNestedHighlightActive(button, false);
        }

        SetInteractableState(false);
        _repeatCoroutine = StartCoroutine(RepeatAudio());
    }

    private IEnumerator RepeatAudio()
    {
        _currentAudioIndex = 0;

        foreach (Transform button in _cardParent)
        {
            ContractionData2 data = _contractionRules[_currentAudioIndex]; // Fixed: Changed type from ContractionData to ContractionData2

            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();
            if (buttonText != null) buttonText.text = data.ShortFormText;

            button.GetComponent<Image>().color = _activeColor;
            SetNestedHighlightActive(button, true);

            if (data.ShortFormAudio != null && _audioSource != null)
            {
                _audioSource.clip = data.ShortFormAudio;
                _audioSource.Play();
                
                float audioDuration = GetScaledAudioLength(data.ShortFormAudio) + 0.2f;
                float elapsedAudio = 0f;
                while (elapsedAudio < audioDuration)
                {
                    elapsedAudio += Time.deltaTime;
                    audioDuration = GetScaledAudioLength(data.ShortFormAudio) + 0.2f;
                    yield return null;
                }
            }

            button.GetComponent<Image>().color = _completedColor;
            SetNestedHighlightActive(button, false);
            _currentAudioIndex++;
        }

        _currentAudioIndex = 0;
        SetInteractableState(true);
    }

    public void Slow(TextMeshProUGUI text)
    {
        _isSlowed = !_isSlowed;
        text.text = _isSlowed ? "    FAST" : "    SLOW";
        _audioSource.pitch = _isSlowed ? 0.75f : 1f;
    }

    private void SetNestedHighlightActive(Transform targetCard, bool isEnabled)
    {
        if (targetCard.childCount > 0)
        {
            Transform firstChild = targetCard.GetChild(0);
            if (firstChild.childCount > 0)
            {
                Transform nestedIndicator = firstChild.GetChild(0);
                if (nestedIndicator.TryGetComponent(out Image img))
                    img.enabled = isEnabled;
            }
        }
    }

    private float GetScaledAudioLength(AudioClip clip)
    {
        if (clip == null) return 0f;
        float currentPitch = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
        return clip.length / currentPitch;
    }
}