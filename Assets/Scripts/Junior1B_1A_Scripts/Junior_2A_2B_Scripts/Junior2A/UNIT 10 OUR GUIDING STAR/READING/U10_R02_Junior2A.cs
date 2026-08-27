using Junior2A;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Junior2A
{
    public class U10_R02_Junior2A : MonoBehaviour, Interfaces_Junior2A
    {
        [Header("Status Tracking")]
        [SerializeField] private bool _isViewed = false;

        [Header("UI Containers")]
        [SerializeField] private GameObject _tab;

        [Header("Audio Components")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _introClip;
        [SerializeField] private AudioClip[] _tab2AudioClips;

        private int _currentAudioClipIndex;
        private Coroutine _audioCoroutine;

        // Tracks unique buttons clicked by the player
        private HashSet<int> _clickedButtonIndices = new HashSet<int>();

        public bool IsViewed => _isViewed;

        private void OnEnable()
        {
            _isViewed = false;
            _clickedButtonIndices.Clear();
            StartCoroutine(Starter());
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (_audioSource != null) _audioSource.Stop();
        }

        private IEnumerator Starter()
        {
            // Lock buttons during intro audio
            SetButtonsInteractable(false);

            if (_introClip != null && _audioSource != null)
            {
                _audioSource.clip = _introClip;
                _audioSource.Play();

                float pitchVal = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
                yield return new WaitForSeconds(_introClip.length / pitchVal);
            }

            // Enable manual clicking after intro ends
            SetButtonsInteractable(true);
        }

        private void SetButtonsInteractable(bool interactable)
        {
            if (_tab == null || _tab.transform.childCount == 0) return;

            foreach (Transform child in _tab.transform.GetChild(0))
            {
                if (child.TryGetComponent(out Button btn))
                {
                    btn.interactable = interactable;
                }
            }
        }

        public void PlayAudio(int index)
        {
            if (_tab2AudioClips == null || index < 0 || index >= _tab2AudioClips.Length) return;

            _currentAudioClipIndex = index;
            Transform currentTabP = _tab.transform;

            // Extract sprite and trigger pop effect on container 1 or 2
            Sprite btnSprite = currentTabP.GetChild(0).GetChild(index).GetChild(0).GetChild(0).GetComponent<Image>().sprite;

            if (index % 2 == 0)
            {
                Transform container = currentTabP.GetChild(1);
                container.GetComponent<Image>().sprite = btnSprite;

                if (container.TryGetComponent(out PopEffect_Junior2A pop))
                {
                    pop.enabled = false;
                    pop.enabled = true;
                }
            }
            else
            {
                Transform container = currentTabP.GetChild(2);
                container.GetComponent<Image>().sprite = btnSprite;

                if (container.TryGetComponent(out PopEffect_Junior2A pop))
                {
                    pop.enabled = false;
                    pop.enabled = true;
                }
            }

            // Track clicked buttons for level completion
            TrackButtonCompletion(index);

            // Play audio clip
            if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
            _audioCoroutine = StartCoroutine(PlayAudioIndex());
        }

        private IEnumerator PlayAudioIndex()
        {
            if (_audioSource == null) yield break;

            _audioSource.Stop();
            _audioSource.clip = _tab2AudioClips[_currentAudioClipIndex];
            _audioSource.Play();

            float pV1 = Mathf.Abs(_audioSource.pitch) > 0 ? Mathf.Abs(_audioSource.pitch) : 1f;
            float aL1 = _audioSource.clip.length / pV1;
            yield return new WaitForSeconds(aL1);
        }

        private void TrackButtonCompletion(int index)
        {
            _clickedButtonIndices.Add(index);

            if (_tab == null || _tab.transform.childCount == 0) return;

            int totalButtons = 0;
            foreach (Transform child in _tab.transform.GetChild(0))
            {
                if (child.GetComponent<Button>()) totalButtons++;
            }

            if (!_isViewed && _clickedButtonIndices.Count >= totalButtons)
            {
                _isViewed = true;
                if (GameManager_Junior2A.Instance != null)
                {
                    GameManager_Junior2A.Instance.Next(true);
                }
            }
        }
    }
}