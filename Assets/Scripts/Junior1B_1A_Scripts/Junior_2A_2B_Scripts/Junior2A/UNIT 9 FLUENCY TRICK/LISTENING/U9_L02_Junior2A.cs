using Junior2A;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Junior2A
{
    public class U9_L02_Junior2A : MonoBehaviour, Interfaces_Junior2A
    {
        [Header("Audio Configurations")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _introClip;
        [SerializeField] private AudioClip[] _clips;

        [Header("UI & Layout References")]
        [SerializeField] private Transform _cardParent;

        [Header("State Settings")]
        [SerializeField] private bool _isViewed = false;
        [SerializeField] private bool _isSlowed = false;

        private int _currentAudioIndex = 0;
        private Coroutine _starterCoroutine;
        private Coroutine _audioCoroutine;
        private Coroutine _repeatCoroutine;

        public bool IsViewed => _isViewed;

        private void OnEnable()
        {
            if (_audioSource == null) _audioSource = GetComponent<AudioSource>();

            if (_starterCoroutine != null) StopCoroutine(_starterCoroutine);
            _starterCoroutine = StartCoroutine(Starter());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (_audioSource != null) _audioSource.Stop();
        }

        private IEnumerator Starter()
        {
            if (_cardParent == null) yield break;

            // 1. Reset state and hide buttons initially
            _currentAudioIndex = 0;
            ResetAllCardHighlights();

            foreach (Transform button in _cardParent)
            {
                button.gameObject.SetActive(false);
            }

            // Safely disable bottom/next UI element if present
            SetFooterNextButtonActive(false);

            // 2. Play intro clip
            if (_introClip != null && _audioSource != null)
            {
                _audioSource.clip = _introClip;
                _audioSource.Play();
                yield return new WaitForSeconds(GetScaledAudioLength(_introClip));
            }

            // 3. Stagger-show card buttons
            foreach (Transform button in _cardParent)
            {
                button.gameObject.SetActive(true);
                if (button.TryGetComponent(out PopEffect_Junior2A pop))
                {
                    pop.enabled = true;
                }
                yield return new WaitForSeconds(0.1f);
            }

            // 4. Sequential audio auto-play loop
            int totalCards = Mathf.Min(_cardParent.childCount, _clips != null ? _clips.Length : 0);
            for (int i = 0; i < totalCards; i++)
            {
                _currentAudioIndex = i;

                if (_cardParent.GetChild(i).TryGetComponent(out Button btn))
                {
                    btn.onClick.Invoke();
                }

                if (_clips != null && i < _clips.Length && _clips[i] != null)
                {
                    yield return new WaitForSeconds(GetScaledAudioLength(_clips[i]));
                }
            }

            // 5. Enable interaction and notify GameManager
            foreach (Transform button in _cardParent)
            {
                if (button.TryGetComponent(out Button btn))
                {
                    btn.interactable = true;
                }
            }

            SetFooterNextButtonActive(true);

            _isViewed = true;
            if (GameManager_Junior2A.Instance != null)
            {
                GameManager_Junior2A.Instance.Next(true);
            }
        }

        public void PlayAudio(int index)
        {
            if (_cardParent == null || index < 0 || index >= _cardParent.childCount) return;

            if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);
            if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);

            ResetCardVisualState(_currentAudioIndex);
            _currentAudioIndex = index;

            _audioCoroutine = StartCoroutine(StartButtonAudio());
        }

        private IEnumerator StartButtonAudio()
        {
            if (_cardParent == null || _currentAudioIndex >= _cardParent.childCount) yield break;

            Transform targetCard = _cardParent.GetChild(_currentAudioIndex);
            SetCardHighlight(targetCard, true);

            if (_clips != null && _currentAudioIndex < _clips.Length && _clips[_currentAudioIndex] != null)
            {
                _audioSource.clip = _clips[_currentAudioIndex];
                _audioSource.Play();
                yield return new WaitForSeconds(GetScaledAudioLength(_clips[_currentAudioIndex]));
            }

            SetCardHighlight(targetCard, false);
        }

        public void Repeat()
        {
            if (_cardParent == null) return;

            if (_starterCoroutine != null) StopCoroutine(_starterCoroutine);
            if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
            if (_repeatCoroutine != null) StopCoroutine(_repeatCoroutine);

            if (_audioSource != null) _audioSource.Stop();

            ResetAllCardHighlights();
            _repeatCoroutine = StartCoroutine(RepeatAudio());
        }

        private IEnumerator RepeatAudio()
        {
            if (_cardParent == null || _clips == null) yield break;

            int totalCards = Mathf.Min(_cardParent.childCount, _clips.Length);

            for (int i = 0; i < totalCards; i++)
            {
                _currentAudioIndex = i;
                Transform button = _cardParent.GetChild(i);

                if (button.TryGetComponent(out PopEffect_Junior2A pop)) pop.enabled = true;
                SetCardHighlight(button, true);

                if (_clips[i] != null && _audioSource != null)
                {
                    _audioSource.clip = _clips[i];
                    _audioSource.Play();
                    yield return new WaitForSeconds(GetScaledAudioLength(_clips[i]));
                }

                SetCardHighlight(button, false);
            }

            _currentAudioIndex = 0;
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

        // Safe Helper Methods
        private void SetCardHighlight(Transform card, bool highlight)
        {
            if (card == null) return;

            if (card.TryGetComponent(out Image cardImg))
            {
                cardImg.color = highlight ? new Color(1f, 0.9565453f, 0.4386792f, 1.0f) : Color.white;
            }

            // Safe double-nested check: card -> child(0) -> child(0)
            if (card.childCount > 0)
            {
                Transform firstChild = card.GetChild(0);
                if (firstChild.childCount > 0)
                {
                    if (firstChild.GetChild(0).TryGetComponent(out Image indicatorImg))
                    {
                        indicatorImg.enabled = highlight;
                    }
                }
            }
        }

        private void ResetCardVisualState(int index)
        {
            if (_cardParent != null && index >= 0 && index < _cardParent.childCount)
            {
                SetCardHighlight(_cardParent.GetChild(index), false);
            }
        }

        private void ResetAllCardHighlights()
        {
            if (_cardParent == null) return;
            foreach (Transform card in _cardParent)
            {
                SetCardHighlight(card, false);
            }
        }

        private void SetFooterNextButtonActive(bool active)
        {
            if (transform.childCount > 0)
            {
                Transform lastChild = transform.GetChild(transform.childCount - 1);
                if (lastChild.childCount > 1)
                {
                    lastChild.GetChild(1).gameObject.SetActive(active);
                }
            }
        }

        private float GetScaledAudioLength(AudioClip clip)
        {
            if (clip == null) return 0f;
            float pitch = (_audioSource != null && Mathf.Abs(_audioSource.pitch) > 0f) ? Mathf.Abs(_audioSource.pitch) : 1f;
            return clip.length / pitch;
        }
    }
}