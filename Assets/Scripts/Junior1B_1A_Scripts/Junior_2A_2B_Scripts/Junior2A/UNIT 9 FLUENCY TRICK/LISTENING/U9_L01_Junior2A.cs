using Junior2A;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Junior2A
{
    [Serializable]
    public class ContractionData
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

    public class U9_L01_Junior2A : MonoBehaviour, Interfaces_Junior2A
    {
        [Header("Audio Components")]
        [SerializeField] private AudioSource _audioSource;
        [Tooltip("Intro clip that plays right when the lesson starts before buttons spawn.")]
        [SerializeField] private AudioClip _introAudio;

        [Header("Contraction Configurations")]
        [Tooltip("Set this array size to 8 and fill in the corresponding texts and audio clips.")]
        [SerializeField] private ContractionData[] _contractionRules;

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

        [Header("Runtime Tracker")]
        [SerializeField] private bool _isViewed = false;

        private int _currentAudioIndex = 0;
        private bool _isSlowed = false;
        private bool _isLessonRunning = false;

        private Coroutine _coroutine;
        private Coroutine _repeatCoroutine;
        private Coroutine _manualPlayCoroutine;

        // Interface implementation for GameManager progression tracking
        public bool IsViewed => _isViewed;

        private void OnEnable()
        {
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();

            InitializeButtonLayout();
            SetInteractableState(false);

            if (_slowButton != null) _slowButton.interactable = true;

            if (_coroutine != null) StopCoroutine(_coroutine);
            _coroutine = StartCoroutine(AutomatedLessonFlowRoutine());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (_audioSource != null) _audioSource.Stop();
        }

        private void InitializeButtonLayout()
        {
            if (_cardParent == null || _contractionRules == null) return;

            int totalItems = Mathf.Min(_cardParent.childCount, _contractionRules.Length);

            for (int i = 0; i < totalItems; i++)
            {
                Transform buttonTransform = _cardParent.GetChild(i);

                TMP_Text buttonText = buttonTransform.GetComponentInChildren<TMP_Text>();
                if (buttonText != null) buttonText.text = _contractionRules[i].LongFormText;

                if (buttonTransform.TryGetComponent(out Image img)) img.color = _normalColor;
                SetNestedHighlightActive(buttonTransform, false);

                if (buttonTransform.TryGetComponent(out Button btn))
                {
                    btn.interactable = false;
                    btn.onClick.RemoveAllListeners();

                    int cardIndex = i; // Closure copy for lambda listener binding
                    btn.onClick.AddListener(() => OnClickCard(cardIndex));
                }

                buttonTransform.gameObject.SetActive(false);
            }
        }

        private void SetInteractableState(bool interactable)
        {
            if (_repeatButton != null) _repeatButton.interactable = interactable;

            if (_cardParent != null && !_isLessonRunning)
            {
                int totalItems = Mathf.Min(_cardParent.childCount, _contractionRules.Length);
                for (int i = 0; i < totalItems; i++)
                {
                    if (_cardParent.GetChild(i).TryGetComponent(out Button btn))
                    {
                        btn.interactable = interactable;
                    }
                }
            }
        }

        private IEnumerator AutomatedLessonFlowRoutine()
        {
            _isLessonRunning = true;
            SetInteractableState(false);

            if (_cardParent == null) yield break;
            int totalItems = Mathf.Min(_cardParent.childCount, _contractionRules.Length);

            // 1. Play Intro Audio Track
            if (_introAudio != null && _audioSource != null)
            {
                _audioSource.clip = _introAudio;
                _audioSource.Play();
                yield return new WaitForSeconds(GetScaledAudioLength(_introAudio) + 0.5f);
            }

            // 2. Spawn buttons sequentially
            for (int i = 0; i < totalItems; i++)
            {
                _cardParent.GetChild(i).gameObject.SetActive(true);
                yield return new WaitForSeconds(_spawnStaggerDelay);
            }

            yield return new WaitForSeconds(0.5f);

            // 3. Sequential auto-play and contraction animation loop
            for (int i = 0; i < totalItems; i++)
            {
                _currentAudioIndex = i;
                yield return StartCoroutine(PlayContractionAnimationStep(i));
            }

            // 4. Complete and unlock UI interactions
            _isLessonRunning = false;
            _isViewed = true;

            SetInteractableState(true);

            if (GameManager_Junior2A.Instance != null)
            {
                GameManager_Junior2A.Instance.Next(true);
            }
        }

        private IEnumerator PlayContractionAnimationStep(int index)
        {
            if (index >= _cardParent.childCount || index >= _contractionRules.Length) yield break;

            Transform buttonTransform = _cardParent.GetChild(index);
            ContractionData data = _contractionRules[index];
            TMP_Text textMesh = buttonTransform.GetComponentInChildren<TMP_Text>();

            if (buttonTransform.TryGetComponent(out Image img)) img.color = _activeColor;
            SetNestedHighlightActive(buttonTransform, true);

            // Step A: Long Form Audio
            if (data.LongFormAudio != null && _audioSource != null)
            {
                _audioSource.clip = data.LongFormAudio;
                _audioSource.Play();

                float elapsedAudio = 0f;
                while (elapsedAudio < GetScaledAudioLength(data.LongFormAudio) + 0.1f)
                {
                    elapsedAudio += Time.deltaTime;
                    yield return null;
                }
            }

            // Step B: Shake Effect
            Vector3 originalPosition = buttonTransform.localPosition;
            float elapsed = 0f;
            while (elapsed < _shakeDuration)
            {
                elapsed += Time.deltaTime;
                float offsetX = UnityEngine.Random.Range(-1f, 1f) * _shakeMagnitude;
                float offsetY = UnityEngine.Random.Range(-1f, 1f) * _shakeMagnitude;
                buttonTransform.localPosition = new Vector3(originalPosition.x + offsetX, originalPosition.y + offsetY, originalPosition.z);
                yield return null;
            }
            buttonTransform.localPosition = originalPosition;

            // Step C: Text Morphing
            if (textMesh != null) textMesh.text = data.ShortFormText;

            // Step D: Short Form Audio
            if (data.ShortFormAudio != null && _audioSource != null)
            {
                _audioSource.clip = data.ShortFormAudio;
                _audioSource.Play();

                float elapsedAudio = 0f;
                while (elapsedAudio < GetScaledAudioLength(data.ShortFormAudio) + 0.4f)
                {
                    elapsedAudio += Time.deltaTime;
                    yield return null;
                }
            }

            if (buttonTransform.TryGetComponent(out Image finalImg)) finalImg.color = _completedColor;
            SetNestedHighlightActive(buttonTransform, false);
        }

        public void OnClickCard(int index)
        {
            if (_isLessonRunning) return;

            if (_coroutine != null) StopCoroutine(_coroutine);
            if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);
            if (_manualPlayCoroutine != null) StopCoroutine(_manualPlayCoroutine);

            _manualPlayCoroutine = StartCoroutine(PlaySingleCardAudio(index));
        }

        private IEnumerator PlaySingleCardAudio(int index)
        {
            if (index < 0 || index >= _contractionRules.Length || index >= _cardParent.childCount) yield break;

            Transform buttonTransform = _cardParent.GetChild(index);
            ContractionData data = _contractionRules[index];

            if (buttonTransform.TryGetComponent(out Image img)) img.color = _activeColor;
            SetNestedHighlightActive(buttonTransform, true);

            if (data.ShortFormAudio != null && _audioSource != null)
            {
                _audioSource.clip = data.ShortFormAudio;
                _audioSource.Play();

                float elapsedAudio = 0f;
                while (elapsedAudio < GetScaledAudioLength(data.ShortFormAudio) + 0.1f)
                {
                    elapsedAudio += Time.deltaTime;
                    yield return null;
                }
            }

            if (buttonTransform.TryGetComponent(out Image finalImg)) finalImg.color = _completedColor;
            SetNestedHighlightActive(buttonTransform, false);
        }

        public void Repeat()
        {
            if (_isLessonRunning || _cardParent == null) return;

            if (_coroutine != null) StopCoroutine(_coroutine);
            if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);
            if (_manualPlayCoroutine != null) StopCoroutine(_manualPlayCoroutine);

            if (_audioSource != null) _audioSource.Stop();

            foreach (Transform buttonTransform in _cardParent)
            {
                if (buttonTransform.TryGetComponent(out Image img)) img.color = _normalColor;
                SetNestedHighlightActive(buttonTransform, false);
            }

            SetInteractableState(false);
            _repeatCoroutine = StartCoroutine(RepeatAudio());
        }

        private IEnumerator RepeatAudio()
        {
            _currentAudioIndex = 0;
            int totalItems = Mathf.Min(_cardParent.childCount, _contractionRules.Length);

            for (int i = 0; i < totalItems; i++)
            {
                _currentAudioIndex = i;
                Transform buttonTransform = _cardParent.GetChild(i);
                ContractionData data = _contractionRules[i];

                TMP_Text buttonText = buttonTransform.GetComponentInChildren<TMP_Text>();
                if (buttonText != null) buttonText.text = data.ShortFormText;

                if (buttonTransform.TryGetComponent(out Image img)) img.color = _activeColor;
                SetNestedHighlightActive(buttonTransform, true);

                if (data.ShortFormAudio != null && _audioSource != null)
                {
                    _audioSource.clip = data.ShortFormAudio;
                    _audioSource.Play();

                    float elapsedAudio = 0f;
                    while (elapsedAudio < GetScaledAudioLength(data.ShortFormAudio) + 0.2f)
                    {
                        elapsedAudio += Time.deltaTime;
                        yield return null;
                    }
                }

                if (buttonTransform.TryGetComponent(out Image finalImg)) finalImg.color = _completedColor;
                SetNestedHighlightActive(buttonTransform, false);
            }

            _currentAudioIndex = 0;
            SetInteractableState(true);
        }

        public void Slow(TextMeshProUGUI text)
        {
            _isSlowed = !_isSlowed;

            if (text != null)
            {
                text.text = _isSlowed ? "    FAST" : "    SLOW";
            }

            if (_audioSource != null)
            {
                _audioSource.pitch = _isSlowed ? 0.75f : 1f;
            }
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
                    {
                        img.enabled = isEnabled;
                    }
                }
            }
        }

        private float GetScaledAudioLength(AudioClip clip)
        {
            if (clip == null) return 0f;
            float currentPitch = (_audioSource != null && Mathf.Abs(_audioSource.pitch) > 0f) ? Mathf.Abs(_audioSource.pitch) : 1f;
            return clip.length / currentPitch;
        }
    }
}