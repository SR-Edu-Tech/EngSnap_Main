using Junior2A;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Junior2A
{
    public class U9_R01_Junior2A : MonoBehaviour, Interfaces_Junior2A
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

        [Header("Audio Components")]
        [SerializeField] private AudioSource _audioSource;
        [Tooltip("Intro clip that plays right when the screen loads before interaction begins.")]
        [SerializeField] private AudioClip _introAudio;

        [Header("Contraction Configurations")]
        [Tooltip("Set this array size to match your cards and fill in the corresponding texts and audio clips.")]
        [SerializeField] private ContractionData[] _contractionRules;

        [Header("Shake Animation Settings")]
        [SerializeField] private float _shakeDuration = 0.5f;
        [SerializeField] private float _shakeMagnitude = 15f;

        [Header("UI Component Links")]
        [Tooltip("Drag the parent container holding your cards here.")]
        [SerializeField] private Transform _cardParent;

        [Tooltip("Drag the Repeat button here so it can be locked/unlocked automatically.")]
        [SerializeField] private Button _repeatButton;

        [Tooltip("Drag the Slow button here so it can be locked/unlocked automatically.")]
        [SerializeField] private Button _slowButton;

        [Header("Visual Feedback (Colors)")]
        [SerializeField] private Color _normalColor = Color.white;
        [SerializeField] private Color _activeColor = new Color(1f, 0.9565453f, 0.4386792f, 1.0f);
        [SerializeField] private Color _completedColor = Color.green;

        [Header("Runtime Tracker")]
        [SerializeField] private bool _isViewed = false;

        private bool _isSlowed = false;
        private bool _isProcessingCard = false;
        private Coroutine _cardSequenceCoroutine;
        private Coroutine _introCoroutine;

        // Tracks unique cards completed by the player
        private HashSet<int> _completedCardIndices = new HashSet<int>();

        // Interface implementation for GameManager progression tracking
        public bool IsViewed => _isViewed;

        private void OnEnable()
        {
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();

            _completedCardIndices.Clear();
            _isViewed = false;
            _isProcessingCard = false;

            InitializeButtonLayout();

            if (_slowButton != null) _slowButton.interactable = true;

            // Start the intro sequence before enabling interactions
            if (_introCoroutine != null) StopCoroutine(_introCoroutine);
            _introCoroutine = StartCoroutine(PlayIntroRoutine());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (_audioSource != null) _audioSource.Stop();
        }

        private IEnumerator PlayIntroRoutine()
        {
            _isProcessingCard = true;
            SetInteractableState(false);

            if (_introAudio != null && _audioSource != null)
            {
                _audioSource.clip = _introAudio;
                _audioSource.Play();
                yield return new WaitForSeconds(GetScaledAudioLength(_introAudio) + 0.2f);
            }

            _isProcessingCard = false;
            SetInteractableState(true);
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
                    btn.interactable = true;
                    btn.onClick.RemoveAllListeners();

                    int cardIndex = i; // Closure copy for lambda listener binding
                    btn.onClick.AddListener(() => OnClickCard(cardIndex));
                }

                buttonTransform.gameObject.SetActive(true);
            }
        }

        private void SetInteractableState(bool interactable)
        {
            if (_repeatButton != null) _repeatButton.interactable = interactable;

            if (_cardParent != null)
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

        public void OnClickCard(int index)
        {
            // Prevent spamming clicks while an animation sequence is running
            if (_isProcessingCard) return;

            if (_cardSequenceCoroutine != null) StopCoroutine(_cardSequenceCoroutine);
            _cardSequenceCoroutine = StartCoroutine(PlayInteractiveContractionRoutine(index));
        }

        private IEnumerator PlayInteractiveContractionRoutine(int index)
        {
            if (_cardParent == null || index < 0 || index >= _contractionRules.Length || index >= _cardParent.childCount) yield break;

            _isProcessingCard = true;
            SetInteractableState(false);

            Transform buttonTransform = _cardParent.GetChild(index);
            ContractionData data = _contractionRules[index];
            TMP_Text textMesh = buttonTransform.GetComponentInChildren<TMP_Text>();

            // Visual setup for active state
            if (buttonTransform.TryGetComponent(out Image img)) img.color = _activeColor;
            SetNestedHighlightActive(buttonTransform, true);

            // Step A: Play Long Form Audio
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

            // Step C: Text Morphing to Short Form
            if (textMesh != null) textMesh.text = data.ShortFormText;

            // Step D: Play Short Form Audio
            if (data.ShortFormAudio != null && _audioSource != null)
            {
                _audioSource.clip = data.ShortFormAudio;
                _audioSource.Play();

                float elapsedAudio = 0f;
                while (elapsedAudio < GetScaledAudioLength(data.ShortFormAudio) + 0.3f)
                {
                    elapsedAudio += Time.deltaTime;
                    yield return null;
                }
            }

            // Mark visually completed
            if (buttonTransform.TryGetComponent(out Image finalImg)) finalImg.color = _completedColor;
            SetNestedHighlightActive(buttonTransform, false);

            // Track score & completion
            _completedCardIndices.Add(index);

            int totalItems = Mathf.Min(_cardParent.childCount, _contractionRules.Length);
            if (!_isViewed && _completedCardIndices.Count >= totalItems)
            {
                _isViewed = true;
                if (GameManager_Junior2A.Instance != null)
                {
                    GameManager_Junior2A.Instance.Next(true);
                }
            }

            _isProcessingCard = false;
            SetInteractableState(true);
        }

        public void Repeat()
        {
            if (_isProcessingCard || _cardParent == null) return;

            if (_cardSequenceCoroutine != null) StopCoroutine(_cardSequenceCoroutine);
            if (_introCoroutine != null) StopCoroutine(_introCoroutine);
            if (_audioSource != null) _audioSource.Stop();

            // Reset all card visuals and text back to original initial states
            InitializeButtonLayout();
            _completedCardIndices.Clear();
            _isViewed = false;

            // Re-play intro audio on repeat
            _introCoroutine = StartCoroutine(PlayIntroRoutine());
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