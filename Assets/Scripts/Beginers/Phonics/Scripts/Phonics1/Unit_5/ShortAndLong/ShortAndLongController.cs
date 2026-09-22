using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EngSnap.Unit5
{
    public class ShortAndLongController : MonoBehaviour
    {
        [Header("Header & Dialogue")]
        [SerializeField] private TMP_Text vowelTitleText;
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Short Door UI")]
        [SerializeField] private Button shortDoorButton;
        [SerializeField] private TMP_Text shortWordText;
        [SerializeField] private Image shortPictureImage;

        [Header("Long Door UI")]
        [SerializeField] private Button longDoorButton;
        [SerializeField] private TMP_Text longWordText;
        [SerializeField] private Image longPictureImage;

        [Header("Vowel Data Sets (a, e, i, o, u)")]
        [SerializeField] private ShortAndLongData[] vowelDataSets;

        [Header("Voice Script Audio Clips")]
        [SerializeField] private AudioClip introClip;             // "Vowels have two sounds - a short one and a long one. Listen!"
        [SerializeField] private AudioClip completionPraiseClip;  // "Awesome job learning short and long vowels!"
        [SerializeField] private AudioClip doorPopSfx;

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentVowelIndex = 0;
        private bool hasExploredShort = false;
        private bool hasExploredLong = false;
        private bool isTransitioning = false;
        private bool isActivityCompleted = false;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
        }

        private void EnsureAudioSources()
        {
            if (sfxAudioSource == null)
            {
                sfxAudioSource = GetComponent<AudioSource>();
                if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
            }
            sfxAudioSource.spatialBlend = 0f;
            sfxAudioSource.volume = 1f;

            if (voiceAudioSource == null) voiceAudioSource = sfxAudioSource;
            else voiceAudioSource.spatialBlend = 0f;
        }

        private void Start()
        {
            EnsureAudioSources();

            if (shortDoorButton != null)
            {
                shortDoorButton.onClick.RemoveAllListeners();
                shortDoorButton.onClick.AddListener(OnShortDoorClicked);
            }

            if (longDoorButton != null)
            {
                longDoorButton.onClick.RemoveAllListeners();
                longDoorButton.onClick.AddListener(OnLongDoorClicked);
            }

            if (continueButton != null)
            {
                Button btn = continueButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(GoToNextPanel);
                }
            }

            ResetLevel();
        }

        private void OnEnable()
        {
            ResetLevel();
            StartCoroutine(StartIntroOnNextFrame());
        }

        private IEnumerator StartIntroOnNextFrame()
        {
            yield return null;
            if (gameObject.activeInHierarchy && !isActivityCompleted)
            {
                yield return StartCoroutine(IntroSequence());
            }
        }

        public void ResetLevel()
        {
            currentVowelIndex = 0;
            hasExploredShort = false;
            hasExploredLong = false;
            isTransitioning = false;
            isActivityCompleted = false;

            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);

            LoadVowel(currentVowelIndex);
            SetSubtitles("Vowels have two sounds - a short one and a long one. Listen!");
        }

        private void LoadVowel(int index)
        {
            hasExploredShort = false;
            hasExploredLong = false;

            ShortAndLongData data = (vowelDataSets != null && index < vowelDataSets.Length) ? vowelDataSets[index] : null;
            if (data != null)
            {
                if (vowelTitleText != null) vowelTitleText.text = data.vowelLetter.ToUpper();

                if (shortWordText != null) shortWordText.text = $"SHORT\n{data.shortWordLabel}";
                if (shortPictureImage != null && data.shortPictureSprite != null) shortPictureImage.sprite = data.shortPictureSprite;

                if (longWordText != null) longWordText.text = $"LONG\n{data.longWordLabel}";
                if (longPictureImage != null && data.longPictureSprite != null) longPictureImage.sprite = data.longPictureSprite;
            }
            else
            {
                if (vowelTitleText != null) vowelTitleText.text = "Short & Long Vowels";
            }

            TriggerPopInDoors();
        }

        private void TriggerPopInDoors()
        {
            StartCoroutine(PopInDoorsSequence());
        }

        private IEnumerator PopInDoorsSequence()
        {
            Transform tShort = (shortDoorButton != null) ? shortDoorButton.transform : null;
            Transform tLong = (longDoorButton != null) ? longDoorButton.transform : null;

            if (tShort != null) tShort.localScale = Vector3.zero;
            if (tLong != null) tLong.localScale = Vector3.zero;

            if (tShort != null)
            {
                yield return StartCoroutine(PopOutTarget(tShort, 0.18f));
            }

            if (tLong != null)
            {
                yield return StartCoroutine(PopOutTarget(tLong, 0.18f));
            }
        }

        private IEnumerator PopOutTarget(Transform target, float duration)
        {
            if (target == null) yield break;

            float elapsed = 0f;
            Vector3 targetScale = Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float scaleValue = Mathf.Sin(t * Mathf.PI * 0.75f) * 1.15f;
                target.localScale = targetScale * Mathf.Clamp(scaleValue, 0f, 1.15f);
                yield return null;
            }

            elapsed = 0f;
            float settleDuration = 0.08f;
            Vector3 startScale = target.localScale;

            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(startScale, targetScale, elapsed / settleDuration);
                yield return null;
            }

            target.localScale = targetScale;
        }

        private IEnumerator WiggleTarget(Transform target, float duration = 0.4f)
        {
            if (target == null) yield break;

            float elapsed = 0f;
            Vector3 origScale = Vector3.one;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float damp = 1f - t;
                float zRot = Mathf.Sin(t * Mathf.PI * 4f) * 3.5f * damp;
                float scalePulse = 1f + (Mathf.Sin(t * Mathf.PI) * 0.05f);

                target.localRotation = Quaternion.Euler(0, 0, zRot);
                target.localScale = origScale * scalePulse;
                yield return null;
            }

            target.localRotation = Quaternion.identity;
            target.localScale = origScale;
        }

        private IEnumerator IntroSequence()
        {
            isTransitioning = true;
            SetSubtitles("Vowels have two sounds - a short one and a long one. Listen!");

            if (introClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = introClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(introClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            isTransitioning = false;
        }

        public void OnShortDoorClicked()
        {
            if (isTransitioning) return;
            StartCoroutine(ShortDoorSequence());
        }

        private IEnumerator ShortDoorSequence()
        {
            isTransitioning = true;
            hasExploredShort = true;

            if (shortDoorButton != null) StartCoroutine(WiggleTarget(shortDoorButton.transform));
            if (doorPopSfx != null && sfxAudioSource != null) sfxAudioSource.PlayOneShot(doorPopSfx);

            ShortAndLongData data = (vowelDataSets != null && currentVowelIndex < vowelDataSets.Length) ? vowelDataSets[currentVowelIndex] : null;
            string letter = (data != null) ? data.vowelLetter.ToLower() : "a";
            string word = (data != null) ? data.shortWordLabel : "apple";

            SetSubtitles($"Short '{letter}' sound: {word}");

            if (data != null && data.shortSoundClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = data.shortSoundClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(data.shortSoundClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            isTransitioning = false;
            CheckVowelExploredProgress();
        }

        public void OnLongDoorClicked()
        {
            if (isTransitioning) return;
            StartCoroutine(LongDoorSequence());
        }

        private IEnumerator LongDoorSequence()
        {
            isTransitioning = true;
            hasExploredLong = true;

            if (longDoorButton != null) StartCoroutine(WiggleTarget(longDoorButton.transform));
            if (doorPopSfx != null && sfxAudioSource != null) sfxAudioSource.PlayOneShot(doorPopSfx);

            ShortAndLongData data = (vowelDataSets != null && currentVowelIndex < vowelDataSets.Length) ? vowelDataSets[currentVowelIndex] : null;
            string letter = (data != null) ? data.vowelLetter.ToLower() : "a";
            string word = (data != null) ? data.longWordLabel : "grapes";

            SetSubtitles($"Long '{letter}' sound: {word}");

            if (data != null && data.longSoundClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = data.longSoundClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(data.longSoundClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            isTransitioning = false;
            CheckVowelExploredProgress();
        }

        private void CheckVowelExploredProgress()
        {
            if (hasExploredShort && hasExploredLong && !isTransitioning)
            {
                StartCoroutine(ReinforcementSequence());
            }
        }

        private IEnumerator ReinforcementSequence()
        {
            isTransitioning = true;

            ShortAndLongData data = (vowelDataSets != null && currentVowelIndex < vowelDataSets.Length) ? vowelDataSets[currentVowelIndex] : null;
            if (data != null)
            {
                SetSubtitles($"Short {data.vowelLetter} is '{data.shortWordLabel}', long {data.vowelLetter} is '{data.longWordLabel}'. Clever!");
                if (data.reinforcementClip != null && voiceAudioSource != null)
                {
                    voiceAudioSource.Stop();
                    voiceAudioSource.clip = data.reinforcementClip;
                    voiceAudioSource.Play();
                    yield return new WaitForSeconds(data.reinforcementClip.length + 0.2f);
                }
                else
                {
                    yield return new WaitForSeconds(1.0f);
                }
            }

            yield return new WaitForSeconds(0.3f);
            isTransitioning = false;

            currentVowelIndex++;
            if (vowelDataSets != null && currentVowelIndex >= vowelDataSets.Length && !isActivityCompleted)
            {
                yield return StartCoroutine(CompletionSequence());
            }
            else
            {
                LoadVowel(currentVowelIndex);
            }
        }

        public void GoToNextVowel()
        {
            if (isTransitioning) return;

            currentVowelIndex++;
            if (vowelDataSets != null && currentVowelIndex >= vowelDataSets.Length && !isActivityCompleted)
            {
                StartCoroutine(CompletionSequence());
            }
            else
            {
                LoadVowel(currentVowelIndex);
            }
        }

        private IEnumerator CompletionSequence()
        {
            isTransitioning = true;
            isActivityCompleted = true;

            SetSubtitles("Awesome job learning short and long vowels!");

            if (completionPraiseClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = completionPraiseClip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(completionPraiseClip.length + 0.3f);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            if (currentPanel != null) TopicProgressUI.MarkTopicComplete(currentPanel);
            else TopicProgressUI.MarkTopicComplete(gameObject);
            TopicProgressUI.MarkTopicComplete("Unit5", "ShortAndLong");

            isTransitioning = false;
        }

        public void GoToNextPanel()
        {
            if (isActivityCompleted)
            {
                TopicProgressUI.MarkTopicComplete(gameObject);
            }

            ResetLevel();

            if (nextPanel != null)
            {
                nextPanel.SetActive(true);
                if (unitContentPanel != null && nextPanel != unitContentPanel && !nextPanel.transform.IsChildOf(unitContentPanel.transform))
                {
                    unitContentPanel.SetActive(false);
                }
            }
            else if (unitContentPanel != null)
            {
                unitContentPanel.SetActive(true);
            }

            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            TopicProgressUI.RefreshAllTicks();
        }

        private void SetSubtitles(string text)
        {
            EngSnap.Common.DialogueBoxAutoHider.SetDialogue(dialogueText, text, dialogueCanvasGroup);
        }
    }
}
