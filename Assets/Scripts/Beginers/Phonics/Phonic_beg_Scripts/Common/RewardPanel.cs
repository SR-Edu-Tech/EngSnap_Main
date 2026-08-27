using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace EngSnap.Common
{
    /// <summary>
    /// Dynamic, data-driven Reward Panel component that presents topic-specific completion data,
    /// animated star badges, fanfare audio, and navigation options.
    /// </summary>
    public class RewardPanel : MonoBehaviour
    {
        [Header("UI Text References")]
        [Tooltip("TextMeshPro title element for champion title (e.g. 'Alphabet Phonics Champion').")]
        [SerializeField] private TextMeshProUGUI championTitleTMP;

        [Tooltip("Fallback UI Text element for champion title if TextMeshPro is not used.")]
        [SerializeField] private Text championTitleText;

        [Tooltip("TextMeshPro subtitle element (e.g. 'UNIT COMPLETE').")]
        [SerializeField] private TextMeshProUGUI subtitleTMP;

        [Tooltip("Fallback UI Text element for subtitle if TextMeshPro is not used.")]
        [SerializeField] private Text subtitleText;

        [Tooltip("TextMeshPro element displaying joined learned words.")]
        [SerializeField] private TextMeshProUGUI learnedWordsTMP;

        [Tooltip("Fallback UI Text element for learned words.")]
        [SerializeField] private Text learnedWordsText;

        [Header("Container & Prefabs")]
        [Tooltip("CanvasGroup controlling overall reward panel fade-in.")]
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Tooltip("Container Transform with LayoutGroup to hold spawned star badges.")]
        [SerializeField] private Transform starsContainer;

        [Tooltip("Prefab instantiated into starsContainer for each star badge.")]
        [SerializeField] private GameObject starPrefab;

        [Tooltip("Confetti particle effect GameObject.")]
        [SerializeField] private GameObject confettiParticles;

        [Header("Buttons")]
        [Tooltip("Button to replay current topic/unit.")]
        [SerializeField] private Button replayButton;

        [Tooltip("Button to proceed to next topic or return to topic selection.")]
        [SerializeField] private Button nextButton;

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip fanfareSound;
        [SerializeField] private AudioClip starPopSound;
        [Tooltip("Dialogue voice audio clip played on reward completion (e.g. 'You got your Phonics Star!').")]
        [SerializeField] private AudioClip dialogueSound;

        [Header("Animation Settings")]
        [SerializeField] private float fadeInDuration = 0.35f;
        [SerializeField] private float starDelay = 0.22f;
        [SerializeField] private float popDuration = 0.25f;

        // Callbacks & State
        private TopicData _currentTopicData;
        private MonoBehaviour _controller;
        private Action _onReplayCallback;
        private Action _onNextCallback;
        private Coroutine _activeAnimationCoroutine;
        private List<Coroutine> _idleCoroutines = new List<Coroutine>();

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

            if (replayButton != null)
            {
                replayButton.onClick.AddListener(OnReplayClicked);
            }

            if (nextButton != null)
            {
                nextButton.onClick.AddListener(OnNextClicked);
            }
        }

        /// <summary>
        /// Displays the reward panel dynamically configured by TopicData.
        /// </summary>
        public void Show(TopicData topicData, MonoBehaviour controller = null, Action onReplay = null, Action onNext = null)
        {
            _currentTopicData = topicData;
            _controller = controller;
            _onReplayCallback = onReplay;
            _onNextCallback = onNext;

            gameObject.SetActive(true);

            // Populate Text Elements
            PopulateText(topicData);

            // Record in PlayerPrefs if topicData provided
            if (topicData != null)
            {
                PlayerPrefs.SetInt(topicData.RewardShownPrefKey, 1);
                PlayerPrefs.SetInt($"{topicData.topicID}_Completed", 1);
                PlayerPrefs.Save();
            }

            // Trigger Animation Sequence
            if (_activeAnimationCoroutine != null)
            {
                StopCoroutine(_activeAnimationCoroutine);
            }
            _activeAnimationCoroutine = StartCoroutine(AnimateSequenceCoroutine(topicData));
        }

        private void PopulateText(TopicData topicData)
        {
            string titleStr = topicData != null ? topicData.championTitle : "Unit Champion";
            string subtitleStr = topicData != null ? topicData.subtitle : "UNIT COMPLETE";
            string wordsStr = topicData != null && topicData.learnedWords != null
                ? string.Join("  ·  ", topicData.learnedWords)
                : "";

            SetTextValue(championTitleTMP, championTitleText, titleStr);
            SetTextValue(subtitleTMP, subtitleText, subtitleStr);
            SetTextValue(learnedWordsTMP, learnedWordsText, wordsStr);
        }

        private void SetTextValue(TextMeshProUGUI tmpGui, Text legacyText, string value)
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

        private IEnumerator AnimateSequenceCoroutine(TopicData topicData)
        {
            // Reset layout and idle routines
            StopIdleCoroutines();
            ClearStarsContainer();

            // Enable Confetti
            if (confettiParticles != null)
            {
                confettiParticles.SetActive(true);
            }

            // 1. Initial State: Panel CanvasGroup Alpha = 0
            panelCanvasGroup.alpha = 0f;

            // 2. Instantiate Stars at scale 1.0 with invisible alpha to allow LayoutGroup to establish bounds
            int starsToSpawn = topicData != null ? Mathf.Max(1, topicData.starCount) : 4;
            List<CanvasGroup> starCanvasGroups = new List<CanvasGroup>();
            List<RectTransform> starRects = new List<RectTransform>();

            if (starsContainer != null && starPrefab != null)
            {
                for (int i = 0; i < starsToSpawn; i++)
                {
                    GameObject starObj = Instantiate(starPrefab, starsContainer);
                    starObj.transform.localScale = Vector3.one;

                    RectTransform rt = starObj.GetComponent<RectTransform>();
                    if (rt != null) starRects.Add(rt);

                    CanvasGroup cg = starObj.GetComponent<CanvasGroup>();
                    if (cg == null) cg = starObj.AddComponent<CanvasGroup>();
                    cg.alpha = 0f;
                    starCanvasGroups.Add(cg);
                }
            }

            // Wait 1 frame for Unity Canvas / HorizontalLayoutGroup recalculation
            yield return null;

            // 3. Fade In Reward Panel
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }
            panelCanvasGroup.alpha = 1f;

            // 4. Play Fanfare SFX & Dialogue Clip
            if (fanfareSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(fanfareSound);
            }

            AudioClip voiceClip = (topicData != null && topicData.dialogueSound != null) ? topicData.dialogueSound : dialogueSound;
            if (voiceClip != null && audioSource != null)
            {
                StartCoroutine(PlayDelayedDialogueCoroutine(voiceClip, 0.35f));
            }

            // 5. Sequential Star Pop-In Animation
            for (int i = 0; i < starCanvasGroups.Count; i++)
            {
                yield return new WaitForSeconds(starDelay);

                if (starPopSound != null && audioSource != null)
                {
                    audioSource.PlayOneShot(starPopSound);
                }

                CanvasGroup cg = starCanvasGroups[i];
                RectTransform rt = starRects[i];

                if (cg != null && rt != null)
                {
                    StartCoroutine(PopSingleStarCoroutine(cg, rt));
                }
            }

            // Wait for last star pop to complete, then launch idle sine bounce loops
            yield return new WaitForSeconds(popDuration);
            foreach (RectTransform rt in starRects)
            {
                if (rt != null)
                {
                    Coroutine idleRoutine = StartCoroutine(IdleBounceCoroutine(rt));
                    _idleCoroutines.Add(idleRoutine);
                }
            }
        }

        private IEnumerator PlayDelayedDialogueCoroutine(AudioClip clip, float delay)
        {
            if (clip == null || audioSource == null) yield break;
            yield return new WaitForSeconds(delay);
            if (gameObject.activeInHierarchy)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        private IEnumerator PopSingleStarCoroutine(CanvasGroup cg, RectTransform rt)
        {
            float elapsed = 0f;
            Vector3 startScale = Vector3.one * 0.01f;
            Vector3 maxScale = Vector3.one * 1.35f;
            Vector3 finalScale = Vector3.one;

            cg.alpha = 1f;

            // Phase 1: Scale up to 1.35x overshoot
            float phase1Time = popDuration * 0.6f;
            while (elapsed < phase1Time)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / phase1Time);
                float easeT = Mathf.Sin(t * Mathf.PI * 0.5f); // EaseOutQuad
                rt.localScale = Vector3.Lerp(startScale, maxScale, easeT);
                yield return null;
            }

            // Phase 2: Settle from 1.35x down to 1.0x
            elapsed = 0f;
            float phase2Time = popDuration * 0.4f;
            while (elapsed < phase2Time)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / phase2Time);
                rt.localScale = Vector3.Lerp(maxScale, finalScale, t * t); // EaseInQuad
                yield return null;
            }

            rt.localScale = finalScale;
        }

        private IEnumerator IdleBounceCoroutine(RectTransform rt)
        {
            float timeOffset = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float baseScale = 1.0f;
            float bounceAmount = 0.04f;
            float bounceSpeed = 2.5f;

            while (rt != null && gameObject.activeInHierarchy)
            {
                float sine = Mathf.Sin((Time.time * bounceSpeed) + timeOffset);
                float currentScale = baseScale + (sine * bounceAmount);
                rt.localScale = new Vector3(currentScale, currentScale, 1f);
                yield return null;
            }
        }

        private void ClearStarsContainer()
        {
            if (starsContainer == null) return;
            foreach (Transform child in starsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        private void StopIdleCoroutines()
        {
            foreach (var routine in _idleCoroutines)
            {
                if (routine != null) StopCoroutine(routine);
            }
            _idleCoroutines.Clear();
        }

        public void OnReplayClicked()
        {
            gameObject.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            TopicProgressUI.HideTopicCompletePanel();

            if (_onReplayCallback != null)
            {
                _onReplayCallback.Invoke();
            }
            else if (_controller != null)
            {
                _controller.SendMessage("OnRewardReplay", SendMessageOptions.DontRequireReceiver);
            }
        }

        public void OnNextClicked()
        {
            gameObject.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            TopicProgressUI.HideTopicCompletePanel();

            if (_onNextCallback != null)
            {
                _onNextCallback.Invoke();
            }
            else if (_controller != null)
            {
                _controller.SendMessage("OnRewardNext", SendMessageOptions.DontRequireReceiver);
            }
        }

        private void OnDisable()
        {
            StopIdleCoroutines();
            if (_activeAnimationCoroutine != null) StopCoroutine(_activeAnimationCoroutine);
        }
    }
}
