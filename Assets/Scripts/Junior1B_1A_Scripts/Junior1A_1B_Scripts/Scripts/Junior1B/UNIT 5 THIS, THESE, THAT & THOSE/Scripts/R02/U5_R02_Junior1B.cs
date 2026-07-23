using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Unit5.R02.Junior1B
{
    public class U5_R02_Junior1B : MonoBehaviour, Interfaces_Junior1B
    {
        [SerializeField] bool _isViewed = false, _slided = false, _tab1Opened = false, _tab2Opened = false;
        [SerializeField] RectTransform _tab1, _tab2;
        [SerializeField] GameObject _tab1P, _tab2P;
        
        [Header("Scroll Content References")]
        [SerializeField] private Transform _tab1Content;
        [SerializeField] private Transform _tab2Content;

        [Header("=== Audio Components ===")]
        [SerializeField] AudioSource _audioSource;
        [SerializeField] AudioClip[] _audioClips;
        
        [Tooltip("The introductory audio that plays once when the lesson layout is opened.")]
        [SerializeField] private AudioClip _masterIntroClip;
        
        [Tooltip("Audio clip played immediately upon sliding up/opening Tab 1 (Index 0).")]
        [SerializeField] private AudioClip _tab1IntroClip;
        
        [Tooltip("Audio clip played immediately upon sliding up/opening Tab 2 (Index 1).")]
        [SerializeField] private AudioClip _tab2IntroClip;

        [Header("=== State & Tracking ===")]
        [SerializeField] Image _currentSpeakerIcon;
        [SerializeField] int _currentAudioClipIndex, _currentTabIndex;
        [SerializeField] List<int> _clickCheckIndex = new List<int>();
        [SerializeField] TextMeshProUGUI _clickedIndexText;
        
        private bool _isIntroPlaying = false;
        Coroutine _audioCoroutine, _tabChange;

        public bool IsViewed => _isViewed;

        void OnEnable()
        {
            _tab1.anchoredPosition = Vector3.left * 200;
            _tab2.anchoredPosition = Vector3.right * 200;
            
            _tab1.anchorMin = new Vector2(.5f, .5f);
            _tab1.anchorMax = new Vector2(.5f, .5f);
            _tab2.anchorMin = new Vector2(.5f, .5f);
            _tab2.anchorMax = new Vector2(.5f, .5f);
            
            _tab1.GetComponent<Image>().color = _tab2.GetComponent<Image>().color = Color.white;
            _tab1.GetChild(0).GetComponent<TextMeshProUGUI>().color = _tab2.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
            
            if (_tab1P != null && _tab2P != null)
            {
                if (_currentTabIndex == 0) _tab1P.GetComponent<CanvasGroup>().alpha = 0;
                else _tab2P.GetComponent<CanvasGroup>().alpha = 0;
            }

            _tab1Opened = _tab2Opened = _slided = false;
            _clickCheckIndex.Clear();
            
            if (_clickedIndexText) _clickedIndexText.text = $"0/{_audioClips.Length}";

            // Trigger the Master Level Intro Sequence on Startup
            if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
            _audioCoroutine = StartCoroutine(PlayMasterIntroSequence());
        }

        void OnDisable()
        {
            ResetButtonColorsOnDisable();
            _clickCheckIndex.Clear();
            _isIntroPlaying = false;
        }

        private IEnumerator PlayMasterIntroSequence()
        {
            _isIntroPlaying = true;
            SetTabsInteractable(false);

            if (_audioSource != null && _masterIntroClip != null)
            {
                _audioSource.clip = _masterIntroClip;
                _audioSource.Play();
                while (_audioSource.isPlaying) yield return null;
                yield return new WaitForSeconds(0.2f);
            }

            _isIntroPlaying = false;
            SetTabsInteractable(true);
        }

        private void SetTabsInteractable(bool interactable)
        {
            if (_tab1 != null && _tab1.TryGetComponent(out Button btn1)) btn1.interactable = interactable;
            if (_tab2 != null && _tab2.TryGetComponent(out Button btn2)) btn2.interactable = interactable;
        }

        private void ResetButtonColorsOnDisable()
        {
            if (_tab1Content == null || _tab2Content == null) return;

            foreach (int index in _clickCheckIndex)
            {
                Transform btnTrans = FindButtonByIndex(index);
                if (btnTrans != null)
                {
                    Image img = GetButtonImage(btnTrans);
                    if (img != null)
                    {
                        Color c = img.color;
                        c.r = Mathf.Clamp01(c.r / 0.85f);
                        c.g = Mathf.Clamp01(c.g / 0.85f);
                        c.b = Mathf.Clamp01(c.b / 0.85f);
                        img.color = c;
                    }
                }
            }
        }

        private Transform FindButtonByIndex(int index)
        {
            if (_tab1Content == null || _tab2Content == null) return null;

            int tab1ButtonCount = _tab1Content.childCount;
            if (index < tab1ButtonCount)
            {
                return (index >= 0 && index < tab1ButtonCount) ? _tab1Content.GetChild(index) : null;
            }
            else
            {
                int localIndex = index - tab1ButtonCount;
                return (localIndex >= 0 && localIndex < _tab2Content.childCount) ? _tab2Content.GetChild(localIndex) : null;
            }
        }

        Image GetButtonImage(Transform buttonTrans)
        {
            if (buttonTrans == null) return null;
            if (buttonTrans.childCount > 1)
            {
                Image img = buttonTrans.GetChild(1).GetComponent<Image>();
                if (img != null) return img;
            }
            return buttonTrans.GetComponent<Image>();
        }

        public void TabSlideUp(int index)
        {
            // Protect layout switching if a core hardware sequence loop is running
            if (_isIntroPlaying) return;

            if (index == 0)
            {
                _tab1Opened = true;
                _tab1.GetComponent<Image>().color = new Color(0.09411766f, 0.6745098f, 0.8784314f, 1);
                _tab2.GetComponent<Image>().color = Color.white;
                _tab1.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
                _tab2.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
            }
            else
            {
                _tab2Opened = true;
                _tab1.GetComponent<Image>().color = Color.white;
                _tab2.GetComponent<Image>().color = new Color(0.09411766f, 0.6745098f, 0.8784314f, 1);
                _tab1.GetChild(0).GetComponent<TextMeshProUGUI>().color = new Color(0.1960784f, 0.1960784f, 0.1960784f, 1);
                _tab2.GetChild(0).GetComponent<TextMeshProUGUI>().color = Color.white;
            }
            
            if (_audioSource != null) _audioSource.Stop();
            if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
            
            if (_slided)
            {
                if (index == _currentTabIndex) return;
                _currentTabIndex = index;
                
                ToggleContentVisibility(_currentTabIndex == 0 ? _tab1Content : _tab2Content, false);
                
                if (_tabChange != null) StopCoroutine(_tabChange);
                _tabChange = StartCoroutine(OnTab());
                return;
            }
            
            _slided = true;
            _currentTabIndex = index;
            StartCoroutine(SlideTabUp());
        }

        IEnumerator SlideTabUp()
        {
            _slided = true;

            ToggleContentVisibility(_tab1Content, false);
            ToggleContentVisibility(_tab2Content, false);

            Vector3 worldPos1 = _tab1.position;
            Vector3 worldPos2 = _tab2.position;

            _tab1.anchorMin = new Vector2(.5f, 1);
            _tab1.anchorMax = new Vector2(.5f, 1);
            _tab2.anchorMin = new Vector2(.5f, 1);
            _tab2.anchorMax = new Vector2(.5f, 1);

            _tab1.position = worldPos1;
            _tab2.position = worldPos2;

            Vector2 startPos1 = _tab1.anchoredPosition;
            Vector2 startPos2 = _tab2.anchoredPosition;
            Vector2 targetPos1 = new Vector2(startPos1.x, -280f);
            Vector2 targetPos2 = new Vector2(startPos2.x, -280f);

            CanvasGroup cg1 = _tab1P.GetComponent<CanvasGroup>();
            CanvasGroup cg2 = _tab2P.GetComponent<CanvasGroup>();

            float slideSpeed = 2.5f;
            float t = 0f;

            while (t < 1f)
            {
                t += Time.deltaTime * slideSpeed;
                float easedT = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
                _tab1.anchoredPosition = Vector2.LerpUnclamped(startPos1, targetPos1, easedT);
                _tab2.anchoredPosition = Vector2.LerpUnclamped(startPos2, targetPos2, easedT);
                cg1.alpha = easedT;
                cg2.alpha = easedT;

                yield return null;
            }

            _tab1.anchoredPosition = targetPos1;
            _tab2.anchoredPosition = targetPos2;

            cg1.alpha = 1;
            cg2.alpha = 1;

            _tabChange = StartCoroutine(OnTab());
        }

        IEnumerator OnTab()
        {
            if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
            
            // 1. Evaluate selection and fire off the specific tab's introductory audio
            AudioClip activeTabIntro = (_currentTabIndex == 0) ? _tab1IntroClip : _tab2IntroClip;
            if (_audioSource != null && activeTabIntro != null)
            {
                _audioSource.clip = activeTabIntro;
                _audioSource.Play();
                while (_audioSource.isPlaying) yield return null;
                yield return new WaitForSeconds(0.25f);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            Transform activeContent = (_currentTabIndex == 0) ? _tab1Content : _tab2Content;

            if (activeContent != null)
            {
                // 2. Perform smooth stagger-reveal layout processing down the hierarchy
                for (int i = 0; i < activeContent.childCount; i++)
                {
                    Transform child = activeContent.GetChild(i);
                    child.gameObject.SetActive(true);
                    if (child.TryGetComponent(out Button btn)) btn.interactable = false;

                    yield return new WaitForSeconds(.25f);
                }
                
                yield return new WaitForSeconds(.25f);
                
                for (int i = 0; i < activeContent.childCount; i++)
                {
                    if (activeContent.GetChild(i).TryGetComponent(out Button btn)) btn.interactable = true;
                }
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
            if (_isIntroPlaying) return;

            _currentAudioClipIndex = index;

            if (!_clickCheckIndex.Contains(index))
            {
                _clickCheckIndex.Add(index);
                if (_clickedIndexText) _clickedIndexText.text = $"{_clickCheckIndex.Count}/{_audioClips.Length}";

                Transform btnTrans = FindButtonByIndex(index);

                if (btnTrans != null)
                {
                    Image img = GetButtonImage(btnTrans);
                    if (img != null)
                    {
                        Color c = img.color;
                        c.r *= 0.85f;
                        c.g *= 0.85f;
                        c.b *= 0.85f;
                        img.color = c;
                    }
                }

                if (_clickCheckIndex.Count == _audioClips.Length && !_isViewed)
                {
                    _isViewed = true;
                    if (GameManager_Junior1B.Instance != null)
                    {
                        GameManager_Junior1B.Instance.Next(true);
                    }
                }
            }

            if (_audioCoroutine != null) StopCoroutine(_audioCoroutine);
            _audioCoroutine = StartCoroutine(PlayAudioIndex());
        }

        IEnumerator PlayAudioIndex()
        {
            if (_audioSource != null && _audioClips != null && _currentAudioClipIndex < _audioClips.Length)
            {
                _audioSource.clip = _audioClips[_currentAudioClipIndex];
                _audioSource.Play();
                yield return new WaitForSeconds(_audioSource.clip.length);
            }
            if (_currentSpeakerIcon) _currentSpeakerIcon.color = Color.white;
        }

        private void ToggleContentVisibility(Transform contentRoot, bool state)
        {
            if (contentRoot == null) return;
            foreach (Transform child in contentRoot)
            {
                child.gameObject.SetActive(state);
            }
        }
    }
}