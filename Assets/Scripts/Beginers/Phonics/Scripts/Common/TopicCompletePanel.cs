using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EngSnap.Common
{
    /// <summary>
    /// Modular Topic Completion Panel component.
    /// Presents dynamic topic completion details: Completion Title, Topic Name,
    /// a Big Central Animated Star, celebration confetti, fanfare & voice audio, and navigation buttons.
    /// Modular and reusable for every topic across Unit 1, Unit 2, Unit 3, etc.
    /// </summary>
    public class TopicCompletePanel : MonoBehaviour
    {
        [Header("UI Text References")]
        [Tooltip("TextMeshPro element for completion title (e.g. 'TOPIC COMPLETED!', 'GREAT JOB!').")]
        [SerializeField] private TextMeshProUGUI titleTMP;

        [Tooltip("Fallback UI Text element for completion title.")]
        [SerializeField] private Text titleText;

        [Tooltip("TextMeshPro element for topic name (e.g. 'Meet Phonics', 'Sound Wall', 'Blend It').")]
        [SerializeField] private TextMeshProUGUI topicNameTMP;

        [Tooltip("Fallback UI Text element for topic name.")]
        [SerializeField] private Text topicNameText;

        [Header("Big Central Star Visuals")]
        [Tooltip("The Big Central Star RectTransform.")]
        [SerializeField] private RectTransform bigStarRect;

        [Tooltip("The Big Central Star Image.")]
        [SerializeField] private Image bigStarImage;

        [Tooltip("Optional glowing aura/halo behind the central star.")]
        [SerializeField] private Image starGlowImage;

        [Header("Container & Canvas")]
        [Tooltip("CanvasGroup controlling overall topic complete panel fade-in.")]
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Tooltip("Confetti particle effect GameObject.")]
        [SerializeField] private GameObject confettiParticles;

        [Header("Buttons")]
        [Tooltip("Button to continue to next topic or return to topic menu.")]
        [SerializeField] private Button continueButton;

        [Tooltip("Optional button to replay current topic.")]
        [SerializeField] private Button replayButton;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip fanfareSound;
        [SerializeField] private AudioClip starPopSound;
        [SerializeField] private AudioClip topicCompleteVoiceClip;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.35f;
        [SerializeField] private float starPopDuration = 0.45f;

        // Callbacks & State
        private Action _onContinueCallback;
        private Action _onReplayCallback;
        private Coroutine _animationCoroutine;
        private Coroutine _idleBounceCoroutine;
        private bool _isShowingExplicitly = false;

        private void Awake()
        {
            if (panelCanvasGroup == null)
            {
                panelCanvasGroup = GetComponent<CanvasGroup>();
                if (panelCanvasGroup == null)
                {
                    panelCanvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(OnContinueClicked);
            }

            if (replayButton != null)
            {
                replayButton.onClick.RemoveAllListeners();
                replayButton.onClick.AddListener(OnReplayClicked);
            }
        }

        private void OnEnable()
        {
            if (_isShowingExplicitly)
            {
                if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
                _animationCoroutine = StartCoroutine(AnimateSequenceCoroutine(topicCompleteVoiceClip));
            }
        }

        public void Hide()
        {
            _isShowingExplicitly = false;
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            if (_idleBounceCoroutine != null) StopCoroutine(_idleBounceCoroutine);

            gameObject.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
        }

        /// <summary>
        /// Displays the modular Topic Complete Panel.
        /// </summary>
        /// <param name="topicName">The name of the completed topic (e.g. 'Meet Phonics', 'Sound Wall').</param>
        /// <param name="completionTitle">Custom completion title (defaults to 'TOPIC COMPLETED!').</param>
        /// <param name="onContinue">Callback invoked when Continue button is clicked.</param>
        /// <param name="onReplay">Optional callback invoked when Replay button is clicked.</param>
        /// <param name="voiceClip">Optional custom voice audio clip to play.</param>
        public void Show(string topicName, string completionTitle = "TOPIC COMPLETED!", Action onContinue = null, Action onReplay = null, AudioClip voiceClip = null)
        {
            _isShowingExplicitly = true;
            _onContinueCallback = onContinue;
            _onReplayCallback = onReplay;

            gameObject.SetActive(true);

            // Populate Text
            SetText(titleTMP, titleText, string.IsNullOrEmpty(completionTitle) ? "TOPIC COMPLETED!" : completionTitle);
            SetText(topicNameTMP, topicNameText, topicName);

            // Trigger Animation Sequence
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
            }
            _animationCoroutine = StartCoroutine(AnimateSequenceCoroutine(voiceClip));
        }

        private void SetText(TextMeshProUGUI tmpGui, Text legacyText, string value)
        {
            if (tmpGui != null)
            {
                tmpGui.text = value;
            }
            if (legacyText != null)
            {
                legacyText.text = value;
            }
        }

        private IEnumerator AnimateSequenceCoroutine(AudioClip customVoiceClip)
        {
            // Reset state
            if (_idleBounceCoroutine != null) StopCoroutine(_idleBounceCoroutine);
            if (bigStarRect != null) bigStarRect.localScale = Vector3.zero;
            if (starGlowImage != null) starGlowImage.enabled = false;
            panelCanvasGroup.alpha = 0f;

            // Enable Confetti
            if (confettiParticles != null)
            {
                confettiParticles.SetActive(true);
            }

            // 1. Fade In Panel Canvas
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            panelCanvasGroup.alpha = 1f;

            // 2. Play Fanfare SFX
            if (fanfareSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(fanfareSound);
            }

            // 3. Elastic Spring Pop Animation for Central Big Star
            if (bigStarRect != null)
            {
                yield return new WaitForSeconds(0.15f);

                if (starPopSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(starPopSound);
                }

                elapsed = 0f;
                Vector3 targetScale = Vector3.one;

                while (elapsed < starPopDuration)
                {
                    elapsed += Time.deltaTime;
                    float percent = Mathf.Clamp01(elapsed / starPopDuration);

                    // Spring overshoot pop math: 0 -> 1.35x -> 1.0x
                    float scaleFactor;
                    if (percent < 0.6f)
                    {
                        float t = percent / 0.6f;
                        scaleFactor = Mathf.Lerp(0.01f, 1.35f, Mathf.Sin(t * Mathf.PI * 0.5f));
                    }
                    else
                    {
                        float t = (percent - 0.6f) / 0.4f;
                        scaleFactor = Mathf.Lerp(1.35f, 1.0f, t * t);
                    }

                    bigStarRect.localScale = targetScale * scaleFactor;
                    yield return null;
                }

                bigStarRect.localScale = targetScale;
                if (starGlowImage != null) starGlowImage.enabled = true;
            }

            // 4. Play Dialogue Voice Line after short delay
            AudioClip voice = customVoiceClip != null ? customVoiceClip : topicCompleteVoiceClip;
            if (voice != null && audioSource != null)
            {
                yield return new WaitForSeconds(0.2f);
                audioSource.PlayOneShot(voice);
            }

            // 5. Start Idle Sine Bounce for Central Big Star
            if (bigStarRect != null)
            {
                _idleBounceCoroutine = StartCoroutine(IdleBounceCoroutine());
            }
        }

        private IEnumerator IdleBounceCoroutine()
        {
            float baseScale = 1.0f;
            float bounceAmount = 0.06f;
            float bounceSpeed = 3.0f;

            while (bigStarRect != null && gameObject.activeInHierarchy)
            {
                float sine = Mathf.Sin(Time.time * bounceSpeed);
                float currentScale = baseScale + (sine * bounceAmount);
                bigStarRect.localScale = new Vector3(currentScale, currentScale, 1f);

                if (starGlowImage != null)
                {
                    Color c = starGlowImage.color;
                    c.a = 0.5f + (sine * 0.3f);
                    starGlowImage.color = c;
                }

                yield return null;
            }
        }

        public void OnContinueClicked()
        {
            Hide();
            Action callback = _onContinueCallback;
            _onContinueCallback = null;
            callback?.Invoke();
        }

        public void OnReplayClicked()
        {
            Hide();
            Action callback = _onReplayCallback;
            _onReplayCallback = null;
            callback?.Invoke();
        }

        private void OnDisable()
        {
            if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);
            if (_idleBounceCoroutine != null) StopCoroutine(_idleBounceCoroutine);
        }
    }
}
