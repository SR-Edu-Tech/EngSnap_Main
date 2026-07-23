using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unit5.R01.Junior1B
{
    [Serializable]
    public class DemonstrativeSlideData
    {
        public string SentenceText;
        public Sprite DisplaySprite;
        public AudioClip SentenceAudio;
    }

    [Serializable]
    public class PronounGroup
    {
        public string PronounLabel;
        [TextArea(2, 4)]
        [Tooltip("The text that will display in the center text box when this pronoun category is clicked.")]
        public string IndividualIntroText; 
        public Button CategoryButton;
        [Tooltip("Spoken audio clip played automatically upon opening this pronoun group category.")]
        public AudioClip ButtonIntroAudio;
        public List<DemonstrativeSlideData> Slides;
    }

    public class U5_R01_Junior1B : MonoBehaviour, Interfaces_Junior1B
    {
        [Header("=== Central Display Components ===")]
        [SerializeField] private Image _centerDisplayImage;
        [SerializeField] private TextMeshProUGUI _centerDisplayText; // Displays the Individual Intro Text permanently until next click
        [SerializeField] private TextMeshProUGUI _subtitleText;      // Reads individual slide sentence text

        [Header("=== Scalable Category Groups ===")]
        [SerializeField] private List<PronounGroup> _pronounGroups;

        [Header("=== Audio Components ===")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _levelIntroClip;

        [Header("=== Independent Control Buttons ===")]
        // [SerializeField] private Button _repeatBtnObject;
        [SerializeField] private Button _slowBtnObject;

        [Header("=== State Trackers ===")]
        [SerializeField] private bool _isViewed = false;
        
        private int _activeClickingIndex = -1;
        private bool _isSlowed = false; 
        private bool _isAudioRoutineRunning = false;
        private Coroutine _audioPlaybackCoroutine;
        private TextMeshProUGUI _slowButtonLabel;

        // Tracks which indices have completed their entire audio playback sequence
        private HashSet<int> _completedGroupIndices = new HashSet<int>();

        public bool IsViewed => _isViewed;

        void Start() => InitializeLessonState();
        void OnEnable() => InitializeLessonState();

        private void InitializeLessonState()
        {
            _activeClickingIndex = -1;
            _isSlowed = false;
            _isAudioRoutineRunning = false;
            _completedGroupIndices.Clear();

            if (_audioSource != null) _audioSource.pitch = 1.0f;

            if (_slowBtnObject != null && _slowButtonLabel == null)
            {
                _slowButtonLabel = _slowBtnObject.GetComponentInChildren<TextMeshProUGUI>();
            }
            if (_slowButtonLabel != null) _slowButtonLabel.text = "    SLOW";

            if (_centerDisplayText != null) _centerDisplayText.text = "";
            if (_subtitleText != null) _subtitleText.text = "";
            if (_centerDisplayImage != null) _centerDisplayImage.gameObject.SetActive(false);
            
            SetupUtilityControlListeners();
            SetupPronounButtonListeners();
            ResetAllButtonVisualStates();
            UpdatePronounButtonsInteractableState(); 

            if (_audioPlaybackCoroutine != null) StopCoroutine(_audioPlaybackCoroutine);
            _audioPlaybackCoroutine = StartCoroutine(PlayLevelIntroOnly());
        }

        private void SetupUtilityControlListeners()
        {
            // if (_repeatBtnObject != null)
            // {
            //     _repeatBtnObject.interactable = true;
            //     _repeatBtnObject.onClick.RemoveAllListeners();
            //     _repeatBtnObject.onClick.AddListener(Repeat);
            // }

            if (_slowBtnObject != null)
            {
                _slowBtnObject.interactable = true;
                _slowBtnObject.onClick.RemoveAllListeners();
                _slowBtnObject.onClick.AddListener(() => Slow(_slowButtonLabel));
            }
        }

        private void SetupPronounButtonListeners()
        {
            for (int i = 0; i < _pronounGroups.Count; i++)
            {
                int index = i;
                if (_pronounGroups[i].CategoryButton != null)
                {
                    _pronounGroups[i].CategoryButton.onClick.RemoveAllListeners();
                    _pronounGroups[i].CategoryButton.onClick.AddListener(() => OnPronounGroupClicked(index));
                }
            }
        }

        private void ResetAllButtonVisualStates()
        {
            for (int i = 0; i < _pronounGroups.Count; i++)
            {
                if (_pronounGroups[i].CategoryButton != null)
                {
                    ColorBlock cb = _pronounGroups[i].CategoryButton.colors;
                    
                    if (i == _activeClickingIndex)
                    {
                        cb.normalColor = Color.yellow;
                        cb.disabledColor = Color.yellow;
                    }
                    else if (_completedGroupIndices.Contains(i))
                    {
                        cb.normalColor = Color.green;
                        cb.disabledColor = Color.green;
                    }
                    else
                    {
                        cb.normalColor = Color.white;
                        cb.disabledColor = Color.white;
                    }

                    _pronounGroups[i].CategoryButton.colors = cb;
                }
            }
        }

        private void UpdatePronounButtonsInteractableState()
        {
            for (int i = 0; i < _pronounGroups.Count; i++)
            {
                if (_pronounGroups[i].CategoryButton != null)
                {
                    _pronounGroups[i].CategoryButton.interactable = !_isAudioRoutineRunning;
                }
            }
        }

        IEnumerator PlayLevelIntroOnly()
        {
            if (_audioSource != null && _levelIntroClip != null)
            {
                _audioSource.clip = _levelIntroClip;
                _audioSource.Play();
                
                while (_audioSource.isPlaying)
                {
                    yield return null;
                }
                yield return new WaitForSeconds(0.2f);
            }
        }

        private void OnPronounGroupClicked(int groupIndex)
        {
            if (_isAudioRoutineRunning) return;

            if (_audioPlaybackCoroutine != null) StopCoroutine(_audioPlaybackCoroutine);
            _audioPlaybackCoroutine = StartCoroutine(ManualClickAudioFlow(groupIndex));
        }

        IEnumerator ManualClickAudioFlow(int groupIndex)
        {
            _isAudioRoutineRunning = true;
            _activeClickingIndex = groupIndex;
            
            ResetAllButtonVisualStates();
            UpdatePronounButtonsInteractableState();

            PronounGroup currentGroup = _pronounGroups[groupIndex];

            // 1. Instantly display your custom Inspector text into the center field box
            if (_centerDisplayText != null)
            {
                _centerDisplayText.gameObject.SetActive(false);
                _centerDisplayText.text = currentGroup.IndividualIntroText; // <--- Uses the custom box data now
                yield return new WaitForEndOfFrame();
                _centerDisplayText.gameObject.SetActive(true);
            }

            if (_subtitleText != null) _subtitleText.text = "";

            if (currentGroup.CategoryButton != null)
            {
                Popeffect_Junior1B buttonPopper = currentGroup.CategoryButton.GetComponent<Popeffect_Junior1B>();
                if (buttonPopper != null)
                {
                    buttonPopper.enabled = false;
                    buttonPopper.enabled = true;
                }
            }

            // Play Category Intro Audio
            if (_audioSource != null && currentGroup.ButtonIntroAudio != null)
            {
                _audioSource.clip = currentGroup.ButtonIntroAudio;
                _audioSource.Play();
                
                while (_audioSource.isPlaying) yield return null;
                yield return new WaitForSeconds(0.3f);
            }

            // 2. Cycle through sub-slides sequential view elements
            if (currentGroup.Slides != null && currentGroup.Slides.Count > 0)
            {
                for (int i = 0; i < currentGroup.Slides.Count; i++)
                {
                    DemonstrativeSlideData activeSlide = currentGroup.Slides[i];

                    if (_centerDisplayImage != null && activeSlide.DisplaySprite != null)
                    {
                        _centerDisplayImage.gameObject.SetActive(false);
                        Popeffect_Junior1B imagePopper = _centerDisplayImage.GetComponent<Popeffect_Junior1B>();
                        if (imagePopper == null) imagePopper = _centerDisplayImage.gameObject.AddComponent<Popeffect_Junior1B>();
                        imagePopper.enabled = false;

                        _centerDisplayImage.sprite = activeSlide.DisplaySprite;
                        _centerDisplayImage.gameObject.SetActive(true);
                        imagePopper.enabled = true;
                    }

                    if (_subtitleText != null)
                    {
                        _subtitleText.gameObject.SetActive(false);
                        _subtitleText.text = activeSlide.SentenceText;
                        yield return new WaitForEndOfFrame();
                        _subtitleText.gameObject.SetActive(true);
                    }

                    if (_audioSource != null && activeSlide.SentenceAudio != null)
                    {
                        _audioSource.clip = activeSlide.SentenceAudio;
                        _audioSource.Play();

                        while (_audioSource.isPlaying) yield return null;
                        yield return new WaitForSeconds(0.4f);
                    }
                    else
                    {
                        float dynamicWait = 2.0f / (_audioSource != null ? _audioSource.pitch : 1f);
                        yield return new WaitForSeconds(dynamicWait);
                    }
                }
            }

            if (_subtitleText != null) _subtitleText.text = "";

            _completedGroupIndices.Add(groupIndex);
            _activeClickingIndex = -1;
            _isAudioRoutineRunning = false;

            ResetAllButtonVisualStates();
            UpdatePronounButtonsInteractableState();

            CheckOverallLessonCompletionStatus();
        }

        private void CheckOverallLessonCompletionStatus()
        {
            if (_completedGroupIndices.Count >= _pronounGroups.Count)
            {
                EndLessonFlow();
            }
        }

        // ==========================================
        // 🛠️ UTILITY CONTROLS
        // ==========================================

        // public void Repeat()
        // {
        //     if (_audioPlaybackCoroutine != null) StopCoroutine(_audioPlaybackCoroutine);
        //     if (_audioSource != null) _audioSource.Stop();

        //     InitializeLessonState();
        // }

        public void Slow(TextMeshProUGUI slowButtonText)
        {
            if (_audioSource == null) return;

            _isSlowed = !_isSlowed;
            _audioSource.pitch = _isSlowed ? 0.7f : 1.0f;

            if (slowButtonText != null)
            {
                slowButtonText.text = _isSlowed ? "    FAST" : "    SLOW";
            }

            if (_audioSource.isPlaying)
            {
                float currentPlaybackTime = _audioSource.time;
                _audioSource.Stop();
                _audioSource.Play();
                _audioSource.time = currentPlaybackTime; 
            }
        }

        private void EndLessonFlow()
        {
            _isViewed = true;
            ResetAllButtonVisualStates();
            if (_centerDisplayText != null) _centerDisplayText.text = "EXCELLENT JOB!";
            if (_subtitleText != null) _subtitleText.text = "";
            if (GameManager_Junior1B.Instance != null) GameManager_Junior1B.Instance.Next(true);
        }
    }
}