using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit3
{
    public class SingTheVowelsController : MonoBehaviour
    {
        [Header("Unit & Topic Identity")]
        [SerializeField] private string unitID = "Unit3";
        [SerializeField] private string topicName = "SingTheVowels";

        [Header("Data Reference")]
        [SerializeField] private SingTheVowelsData activityData;

        [Header("UI Text & Dialogue")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Verse Cards & UI Controls")]
        [SerializeField] private GameObject[] verseBlocks = new GameObject[3];
        [SerializeField] private Button nextVerseButton;
        [SerializeField] private ParticleSystem tapSparkleParticles;

        [Header("Karaoke & Timing Settings")]
        [Tooltip("If true, automatically divides the instrumental clip duration into 3 equal verse sections.")]
        [SerializeField] private bool autoCalculateKaraokeDelay = true;
        [Tooltip("Duration (in seconds) for each verse card in Karaoke mode when auto-calculate is false.")]
        [SerializeField] private float karaokeVerseDuration = 4.0f;
        [Tooltip("Extra delay (in seconds) between card transitions during Karaoke mode.")]
        [SerializeField] private float karaokeCardSlideDelay = 0.5f;

        [Header("Progress Ring UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;

        [Header("Mascots")]
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject momoHintObject;

        [Header("SFX Feedback Clips")]
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip retryGentleSfx;
        [SerializeField] private AudioClip starPopSfx;
        [SerializeField] private AudioClip applauseSfx;

        [Header("Rewards & Progression")]
        [Tooltip("Confetti particle system to play on completion.")]
        [SerializeField] private GameObject confettiParticles;

        [Tooltip("The sticker reward popup screen.")]
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;

        [Tooltip("The button to continue to the next activity.")]
        [SerializeField] private GameObject continueButton;

        [Tooltip("The next panel or activity to show when Continue is clicked.")]
        [SerializeField] private GameObject nextPanel;

        [Tooltip("The current panel to hide when Continue is clicked.")]
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentVerseIndex = 0;
        private bool isTapAlongActive = false;
        private bool isKaraokeActive = false;
        private bool isTransitioning = false;

        public string UnitID => unitID;
        public string TopicName => topicName;

        private void Awake()
        {
            EnsureAudioSources();
            SetupButtonListeners();
            SaveOriginalVersePositions();
        }

        private void Start()
        {
            StartActivity();
        }

        private void OnEnable()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            StartActivity();
        }

        private void OnDisable()
        {
            StopAllAudio();
            StopAllCoroutines();
            DeactivateMascots();
        }

        private void StopAllAudio()
        {
            if (voiceAudioSource != null && voiceAudioSource.isPlaying) voiceAudioSource.Stop();
            if (sfxAudioSource != null && sfxAudioSource.isPlaying) sfxAudioSource.Stop();
        }

        private void EnsureAudioSources()
        {
            if (voiceAudioSource == null) voiceAudioSource = gameObject.AddComponent<AudioSource>();
            if (sfxAudioSource == null) sfxAudioSource = gameObject.AddComponent<AudioSource>();
            voiceAudioSource.spatialBlend = 0f;
            sfxAudioSource.spatialBlend = 0f;
        }

        private void SetupButtonListeners()
        {
            if (nextVerseButton != null) nextVerseButton.onClick.AddListener(OnNextVerseButtonClicked);

            if (continueButton != null)
            {
                Button btn = continueButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(GoToNextPanel);
                }
            }
        }

        public void OnNextVerseButtonClicked()
        {
            if (isTransitioning) return;

            if (currentVerseIndex == 1)
            {
                PlayVerse(2);
            }
            else if (currentVerseIndex == 2)
            {
                PlayVerse(3);
            }
            else if (currentVerseIndex == 3)
            {
                StartKaraokeMode();
            }
            else if (isKaraokeActive || currentVerseIndex >= 4)
            {
                StartCoroutine(CompleteStopSequence());
            }
            else
            {
                PlayVerse(1);
            }
        }

        public void StartActivity()
        {
            currentVerseIndex = 0;
            isTapAlongActive = false;
            isKaraokeActive = false;
            isTransitioning = false;

            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (continueButton != null) continueButton.SetActive(false);
            if (nextVerseButton != null) nextVerseButton.gameObject.SetActive(false);

            HideAllVerseBlocks();
            UpdateProgressUI(0f);
            StartCoroutine(PlayIntroSequence());
        }

        private IEnumerator PlayIntroSequence()
        {
            isTransitioning = true;
            SetDialogue("Sing with me! Verse 1: Short Vowels!");

            if (activityData != null && activityData.introVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.introVoiceClip);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            isTransitioning = false;
            PlayVerse(1);
        }

        private void PlayVerse(int verseNumber)
        {
            if (isTransitioning) return;
            StartCoroutine(PlayVerseSequence(verseNumber));
        }

        private IEnumerator PlayVerseSequence(int verseNumber)
        {
            isTransitioning = true;
            currentVerseIndex = verseNumber;

            if (nextVerseButton != null) nextVerseButton.gameObject.SetActive(false);
            UpdateVerseBlocks(verseNumber);

            AudioClip verseClip = null;
            switch (verseNumber)
            {
                case 1:
                    SetDialogue("/a/, /e/, /i/, /o/, /u/ — are short vowels that we use!");
                    verseClip = activityData != null ? activityData.verse1ShortVowelsClip : null;
                    break;
                case 2:
                    SetDialogue("A vowel is in every word that we read or write!");
                    verseClip = activityData != null ? activityData.verse2EveryWordClip : null;
                    break;
                case 3:
                    SetDialogue("/ai/, /ee/, /ie/, /oa/, /ue/ — are long vowels that we use!");
                    verseClip = activityData != null ? activityData.verse3LongVowelsClip : null;
                    break;
            }

            if (verseClip != null)
            {
                yield return PlayVoiceClip(verseClip);
            }
            else
            {
                yield return new WaitForSeconds(3.0f);
            }

            UpdateProgressUI((float)verseNumber / 3f);
            isTransitioning = false;

            if (nextVerseButton != null) nextVerseButton.gameObject.SetActive(true);
        }

        public void PlayVowelAudioAndWiggle(Button target, int index)
        {
            if (target != null)
            {
                TriggerWiggle(target.GetComponent<RectTransform>());
            }
            PlayVowelAudio(index);
        }

        public void PlayVowelAudioAndWiggle(RectTransform target, int index)
        {
            if (target != null)
            {
                TriggerWiggle(target);
            }
            PlayVowelAudio(index);
        }

        public void PlayVowelAudioAndWiggle(int index)
        {
            PlayVowelAudio(index);
        }

        public void PlayVowelAudio(int index)
        {
            AudioClip clipToPlay = null;
            if (currentVerseIndex == 3)
            {
                if (activityData != null && activityData.longVowelAudioClips != null && index >= 0 && index < activityData.longVowelAudioClips.Length)
                {
                    clipToPlay = activityData.longVowelAudioClips[index];
                }
            }
            else
            {
                if (activityData != null && activityData.shortVowelAudioClips != null && index >= 0 && index < activityData.shortVowelAudioClips.Length)
                {
                    clipToPlay = activityData.shortVowelAudioClips[index];
                }
            }

            if (clipToPlay != null)
            {
                PlayVoiceClipNonBlocking(clipToPlay);
            }
            else
            {
                PlaySFX(correctChimeSfx);
            }

            if (tapSparkleParticles != null) tapSparkleParticles.Play();

            string currentText = GetVowelLabel(index);
            SetDialogue($"Sing! /{currentText.ToLower()}/!");
        }

        private string GetVowelLabel(int index)
        {
            if (activityData == null) return "Vowel";
            string[] labels = (currentVerseIndex == 3) ? activityData.longVowelLabels : activityData.shortVowelLabels;
            if (labels != null && index >= 0 && index < labels.Length) return labels[index];
            return "Vowel";
        }

        private void OnVowelTapped(int index)
        {
            PlayVowelAudioAndWiggle(index);
        }

        private void StartKaraokeMode()
        {
            if (isTransitioning) return;
            StartCoroutine(PlayKaraokeSequence());
        }

        private IEnumerator PlayKaraokeSequence()
        {
            isTransitioning = true;
            isKaraokeActive = true;
            HideAllVerseBlocks();

            SetDialogue("Now YOU sing it — I will be quiet!");
            if (activityData != null && activityData.karaokeCueClip != null)
            {
                yield return PlayVoiceClip(activityData.karaokeCueClip);
            }

            SetDialogue("Your turn to sing!");

            AudioClip instClip = activityData != null ? activityData.karaokeInstrumentalClip : null;

            if (instClip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = instClip;
                voiceAudioSource.Play();
            }

            float verseDuration = karaokeVerseDuration;
            if (autoCalculateKaraokeDelay && instClip != null && instClip.length > 3f)
            {
                verseDuration = Mathf.Max(1.0f, (instClip.length - (karaokeCardSlideDelay * 2f)) / 3f);
            }

            // Verse 1 during Karaoke
            currentVerseIndex = 1;
            SetDialogue("Karaoke: Verse 1 — Short Vowels!");
            UpdateVerseBlocks(1);
            yield return new WaitForSeconds(verseDuration);
            if (karaokeCardSlideDelay > 0f) yield return new WaitForSeconds(karaokeCardSlideDelay);

            // Verse 2 during Karaoke
            currentVerseIndex = 2;
            SetDialogue("Karaoke: Verse 2 — Every Word Rule!");
            UpdateVerseBlocks(2);
            yield return new WaitForSeconds(verseDuration);
            if (karaokeCardSlideDelay > 0f) yield return new WaitForSeconds(karaokeCardSlideDelay);

            // Verse 3 during Karaoke
            currentVerseIndex = 3;
            SetDialogue("Karaoke: Verse 3 — Long Vowels!");
            UpdateVerseBlocks(3);
            yield return new WaitForSeconds(verseDuration);
            if (karaokeCardSlideDelay > 0f) yield return new WaitForSeconds(karaokeCardSlideDelay);

            if (voiceAudioSource != null && voiceAudioSource.isPlaying)
            {
                voiceAudioSource.Stop();
            }

            HideAllVerseBlocks();
            SetDialogue("Hooray! What a singer!");
            PlaySFX(applauseSfx != null ? applauseSfx : (activityData != null && activityData.applauseStingerSfx != null ? activityData.applauseStingerSfx : correctChimeSfx));

            yield return new WaitForSeconds(1.0f);

            StartCoroutine(CompleteStopSequence());
        }

        private IEnumerator CompleteStopSequence()
        {
            HideAllVerseBlocks();
            UpdateProgressUI(1f);

            SetDialogue("You know the Vowel Song by heart! Bravo!");

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            PlaySFX(starPopSfx);
            yield return new WaitForSeconds(0.5f);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            if (continueButton != null) continueButton.SetActive(true);
            isTransitioning = false;
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
        }

        public void GoToNextPanel()
        {
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            TopicProgressUI.HideTopicCompletePanel();
            DeactivateMascots();

            if (nextPanel != null)
            {
                nextPanel.SetActive(true);
            }
            else if (unitContentPanel != null)
            {
                unitContentPanel.SetActive(true);
            }

            if (currentPanel != null)  {
                 currentPanel.SetActive(false);
                unitContentPanel.SetActive(false);
            }
            else gameObject.SetActive(false);

            TopicProgressUI.RefreshAllTicks();
        }

        public void GoToPreviousPanel()
        {
            DeactivateMascots();
            if (currentPanel != null) currentPanel.SetActive(false);
            if (unitContentPanel != null) unitContentPanel.SetActive(true);
        }

        private void TriggerWiggle(RectTransform target)
        {
            if (target == null) return;
            StartCoroutine(WiggleRect(target, 0.35f, 10f));
        }

        private IEnumerator WiggleRect(RectTransform target, float duration, float angle)
        {
            Quaternion originalRot = target.localRotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / duration;
                float currentAngle = Mathf.Sin(percent * Mathf.PI * 8f) * angle * (1f - percent);
                target.localRotation = originalRot * Quaternion.Euler(0f, 0f, currentAngle);
                yield return null;
            }

            target.localRotation = originalRot;
        }

        private void SetDialogue(string msg)
        {
            DialogueBoxAutoHider.SetDialogue(dialogueText, msg, dialogueCanvasGroup);
        }

        private void UpdateProgressUI(float fillPercent)
        {
            fillPercent = Mathf.Clamp01(fillPercent);
            if (progressRingFillImage != null) progressRingFillImage.fillAmount = fillPercent;
            if (progressText != null) progressText.text = $"{Mathf.RoundToInt(fillPercent * 100)}%";
        }

        private IEnumerator PlayVoiceClip(AudioClip clip)
        {
            if (clip == null || voiceAudioSource == null) yield break;
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(clip.length + 0.1f);
        }

        private void PlayVoiceClipNonBlocking(AudioClip clip)
        {
            if (clip == null || voiceAudioSource == null) return;
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
        }

        private void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxAudioSource == null) return;
            sfxAudioSource.PlayOneShot(clip);
        }

        private Vector2[] originalVerseBlockPositions;

        private void SaveOriginalVersePositions()
        {
            if (verseBlocks == null) return;
            if (originalVerseBlockPositions == null || originalVerseBlockPositions.Length != verseBlocks.Length)
            {
                originalVerseBlockPositions = new Vector2[verseBlocks.Length];
                for (int i = 0; i < verseBlocks.Length; i++)
                {
                    if (verseBlocks[i] != null)
                    {
                        RectTransform rt = verseBlocks[i].GetComponent<RectTransform>();
                        if (rt != null) originalVerseBlockPositions[i] = rt.anchoredPosition;
                    }
                }
            }
        }

        private void HideAllVerseBlocks()
        {
            if (verseBlocks == null) return;
            for (int i = 0; i < verseBlocks.Length; i++)
            {
                if (verseBlocks[i] != null) verseBlocks[i].SetActive(false);
            }
        }

        private void UpdateVerseBlocks(int verseNumber)
        {
            if (verseBlocks == null || verseBlocks.Length == 0) return;
            SaveOriginalVersePositions();

            for (int i = 0; i < verseBlocks.Length; i++)
            {
                if (verseBlocks[i] != null)
                {
                    bool isCurrent = (i == verseNumber - 1);
                    verseBlocks[i].SetActive(isCurrent);

                    if (isCurrent)
                    {
                        RectTransform rt = verseBlocks[i].GetComponent<RectTransform>();
                        if (rt != null && originalVerseBlockPositions != null && i < originalVerseBlockPositions.Length)
                        {
                            Vector2 targetPos = originalVerseBlockPositions[i];
                            StartCoroutine(SlideInRectTransform(rt, targetPos, 0.35f));
                        }
                    }
                }
            }
        }

        private IEnumerator SlideInRectTransform(RectTransform target, Vector2 targetAnchoredPos, float duration)
        {
            if (target == null) yield break;

            Vector2 startAnchoredPos = targetAnchoredPos + new Vector2(600f, 0f);
            target.anchoredPosition = startAnchoredPos;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                target.anchoredPosition = Vector2.Lerp(startAnchoredPos, targetAnchoredPos, smoothT);
                yield return null;
            }

            target.anchoredPosition = targetAnchoredPos;
        }
    }
}
