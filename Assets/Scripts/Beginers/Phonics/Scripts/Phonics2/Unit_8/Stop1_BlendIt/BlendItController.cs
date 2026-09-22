using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit8
{
    public class BlendItController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit8";
        [SerializeField] private string topicName = "BlendIt";

        [Header("Session Selection")]
        [Tooltip("0 = Session A (Short a), 1 = Session B (Short e)")]
        [SerializeField] private int activeSession = 0;

        [Header("Data Asset")]
        [SerializeField] private BlendItData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Dialogue / Subtitle UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Phase 0: Mascot Shoulder-Slide Demo")]
        [SerializeField] private GameObject actionDemoPanel;
        [SerializeField] private RectTransform mascotArmDemoRect;
        [SerializeField] private Image mascotDemoHandImage;
        [SerializeField] private Button skipDemoButton;

        [Header("Phase 1: 3 Sound Boxes & Slide Bar")]
        [SerializeField] private GameObject blendingPanel;
        [SerializeField] private RectTransform[] soundBoxRects = new RectTransform[3];
        [SerializeField] private TMP_Text[] soundBoxTexts = new TMP_Text[3];
        [SerializeField] private Image[] soundBoxGlowImages = new Image[3];
        [SerializeField] private BlendSlideBar blendSlideBar;
        [SerializeField] private Slider unityBlendSlider;
        [SerializeField] private GameObject slideBarContainer;

        [Header("Phase 2: Fade-out 'Say It' UI (Rounds 7 & 8)")]
        [SerializeField] private GameObject sayItButtonContainer;
        [SerializeField] private Button sayItButton;
        [SerializeField] private Button replayAudioButton;

        [Header("Word Picture Reveal")]
        [SerializeField] private GameObject pictureRevealPanel;
        [SerializeField] private Image wordPictureImage;
        [SerializeField] private TMP_Text wholeWordTMP;

        [Header("Progress & Mascot UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject momoMascotObject;

        [Header("Rewards & Navigation")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentWordIndex = 0;
        private int totalRounds = 8;
        private int attemptCount = 0;
        private bool isTransitioning = false;
        private bool hasInitialized = false;
        private bool hasCompletedDemo = false;
        private Vector3[] initialBoxPositions = new Vector3[3];
        private BlendWordItem currentWord;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
            EnsureDataAssigned();

            for (int i = 0; i < soundBoxRects.Length; i++)
            {
                if (soundBoxRects[i] != null)
                {
                    initialBoxPositions[i] = soundBoxRects[i].localPosition;
                }
            }

            if (blendSlideBar != null)
            {
                blendSlideBar.OnLetterStopReached += OnSlideBarLetterStop;
                blendSlideBar.OnBlendCompleted += OnSlideBarBlendCompleted;
                blendSlideBar.OnSlideCancelled += OnSlideBarCancelled;
            }
        }

        private void Start()
        {
            SetupButtonListeners();
            hasInitialized = true;
            StartActivity();
        }

        private void OnEnable()
        {
            if (hasInitialized)
            {
                StartActivity();
            }
        }

        private void OnDisable()
        {
            StopAllAudio();
            StopAllCoroutines();
            DeactivateMascots();
        }

        private void OnDestroy()
        {
            if (blendSlideBar != null)
            {
                blendSlideBar.OnLetterStopReached -= OnSlideBarLetterStop;
                blendSlideBar.OnBlendCompleted -= OnSlideBarBlendCompleted;
                blendSlideBar.OnSlideCancelled -= OnSlideBarCancelled;
            }
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

        private void EnsureDataAssigned()
        {
            if (activityData == null)
            {
                activityData = Resources.Load<BlendItData>("Phonics2/Unit8/BlendItData_Unit8");
            }
            if (activityData == null)
            {
                activityData = ScriptableObject.CreateInstance<BlendItData>();
                activityData.PopulateDefaultData();
            }
        }

        private void SetupButtonListeners()
        {
            if (sayItButton != null)
            {
                sayItButton.onClick.RemoveAllListeners();
                sayItButton.onClick.AddListener(OnSayItButtonClicked);
            }

            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(ReplayCurrentWordSounds);
            }

            if (skipDemoButton != null)
            {
                skipDemoButton.onClick.RemoveAllListeners();
                skipDemoButton.onClick.AddListener(() =>
                {
                    StopAllCoroutines();
                    hasCompletedDemo = true;
                    if (actionDemoPanel != null) actionDemoPanel.SetActive(false);
                    LoadWordRound(0);
                });
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
        }

        public void ResetLevel()
        {
            StartActivity();
        }

        public void StartActivity()
        {
            StopAllCoroutines();
            activeSession = 0;
            currentWordIndex = 0;
            attemptCount = 0;
            isTransitioning = false;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (pictureRevealPanel != null) pictureRevealPanel.SetActive(false);

            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (momoMascotObject != null) momoMascotObject.SetActive(false);

            if (!hasCompletedDemo)
            {
                StartCoroutine(RunShoulderSlideTutorial());
            }
            else
            {
                LoadWordRound(0);
            }
        }

        private IEnumerator RunShoulderSlideTutorial()
        {
            isTransitioning = true;
            if (actionDemoPanel != null) actionDemoPanel.SetActive(true);
            if (blendingPanel != null) blendingPanel.SetActive(false);

            SetDialogue("Today you are going to READ. Stand up — we need your arm!");
            if (activityData != null && activityData.leoIntroClip != null)
            {
                yield return PlayVoiceClip(activityData.leoIntroClip);
            }
            else
            {
                yield return new WaitForSeconds(2.0f);
            }

            SetDialogue("Touch your shoulder. Slide down… /c/ … /a/ … /t/ … and sweep back up — CAT!");
            if (activityData != null && activityData.leoActionDemoClip != null)
            {
                yield return PlayVoiceClip(activityData.leoActionDemoClip);
            }
            else
            {
                yield return new WaitForSeconds(3.0f);
            }

            hasCompletedDemo = true;
            if (actionDemoPanel != null) actionDemoPanel.SetActive(false);

            SetDialogue("Your turn. Slide with your finger!");
            if (activityData != null && activityData.leoYourTurnClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.leoYourTurnClip);
            }

            yield return new WaitForSeconds(0.5f);
            isTransitioning = false;
            LoadWordRound(0);
        }

        private void LoadWordRound(int index)
        {
            BlendWordItem[] wordSet = (activeSession == 0) ? activityData.shortAWords : activityData.shortEWords;
            if (wordSet == null || index >= wordSet.Length)
            {
                if (activeSession == 0)
                {
                    StartCoroutine(TransitionToShortESessionRoutine());
                    return;
                }
                else
                {
                    CompleteStop1();
                    return;
                }
            }

            StartCoroutine(LoadWordRoundRoutine(index, wordSet[index]));
        }

        private IEnumerator TransitionToShortESessionRoutine()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            SetDialogue("Great job with short 'a'! Now let us blend short 'e' words!");
            yield return new WaitForSeconds(1.8f);

            activeSession = 1;
            currentWordIndex = 0;
            isTransitioning = false;
            LoadWordRound(0);
        }

        private IEnumerator LoadWordRoundRoutine(int index, BlendWordItem item)
        {
            isTransitioning = true;
            currentWordIndex = index;
            currentWord = item;
            attemptCount = 0;

            if (blendingPanel != null) blendingPanel.SetActive(true);
            if (pictureRevealPanel != null) pictureRevealPanel.SetActive(false);

            // Reset sound boxes positions and texts
            ResetSoundBoxVisuals();

            if (soundBoxTexts.Length >= 3 && currentWord != null)
            {
                if (soundBoxTexts[0] != null) soundBoxTexts[0].text = currentWord.firstLetter;
                if (soundBoxTexts[1] != null) soundBoxTexts[1].text = currentWord.middleLetter;
                if (soundBoxTexts[2] != null) soundBoxTexts[2].text = currentWord.lastLetter;
            }

            bool isFadeOutRound = (index >= 6); // Last 2 rounds fade out slider
            if (slideBarContainer != null) slideBarContainer.SetActive(!isFadeOutRound);
            if (sayItButtonContainer != null) sayItButtonContainer.SetActive(isFadeOutRound);
            if (blendSlideBar != null)
            {
                blendSlideBar.SetInteractable(true);
                blendSlideBar.ResetSlider();
            }

            if (activeSession == 1 && index >= 4)
            {
                SetDialogue($"Try saying /{currentWord.firstLetter}/ … /{currentWord.middleLetter}/ … /{currentWord.lastLetter}/ straight away!");
                if (index == 4 && activityData != null && activityData.noSliderFadeOutClip != null)
                {
                    PlayVoiceClipNonBlocking(activityData.noSliderFadeOutClip);
                }
            }
            else
            {
                SetDialogue($"Slide your finger: /{currentWord.firstLetter}/ … /{currentWord.middleLetter}/ … /{currentWord.lastLetter}/ … and blend!");
            }

            float totalProgress = (activeSession * 8f + index) / 16f;
            UpdateProgressUI(totalProgress);
            isTransitioning = false;
            yield return null;
        }

        private void ResetSoundBoxVisuals()
        {
            for (int i = 0; i < soundBoxRects.Length; i++)
            {
                if (soundBoxRects[i] != null && i < initialBoxPositions.Length)
                {
                    soundBoxRects[i].localPosition = initialBoxPositions[i];
                    soundBoxRects[i].localRotation = Quaternion.identity;
                    soundBoxRects[i].localScale = Vector3.one;
                }
                if (soundBoxGlowImages != null && i < soundBoxGlowImages.Length && soundBoxGlowImages[i] != null)
                {
                    soundBoxGlowImages[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnSlideBarLetterStop(int letterIndex)
        {
            if (currentWord == null || letterIndex < 0 || letterIndex >= 3) return;

            PlaySFX(activityData != null ? activityData.soundBoxGlowSfx : null);

            if (soundBoxGlowImages != null && letterIndex < soundBoxGlowImages.Length && soundBoxGlowImages[letterIndex] != null)
            {
                soundBoxGlowImages[letterIndex].gameObject.SetActive(true);
            }

            if (soundBoxRects != null && letterIndex < soundBoxRects.Length && soundBoxRects[letterIndex] != null)
            {
                StartCoroutine(PopScaleRoutine(soundBoxRects[letterIndex], 1.25f, 0.2f));
            }

            AudioClip soundClip = (letterIndex == 0) ? currentWord.firstSoundAudio : (letterIndex == 1) ? currentWord.middleSoundAudio : currentWord.lastSoundAudio;
            if (soundClip != null)
            {
                PlayVoiceClipNonBlocking(soundClip);
            }
        }

        private void OnSlideBarBlendCompleted()
        {
            if (isTransitioning || currentWord == null) return;
            StartCoroutine(HandleBlendSuccessRoutine());
        }

        private void OnSlideBarCancelled()
        {
            if (isTransitioning) return;
            attemptCount++;
            ResetSoundBoxVisuals();

            if (attemptCount >= 2 && momoMascotObject != null)
            {
                momoMascotObject.SetActive(true);
                SetDialogue("Slide with me! Shoulder… elbow… hand… and up!");
                if (activityData != null && activityData.momoHelpClip != null)
                {
                    PlayVoiceClipNonBlocking(activityData.momoHelpClip);
                }
            }
            else
            {
                SetDialogue($"Slide again, a bit slower… /{currentWord.firstLetter}/ … /{currentWord.middleLetter}/ … /{currentWord.lastLetter}/ …");
                if (activityData != null && activityData.retrySlowSlideClip != null)
                {
                    PlayVoiceClipNonBlocking(activityData.retrySlowSlideClip);
                }
            }
        }

        private void OnSayItButtonClicked()
        {
            if (isTransitioning || currentWord == null) return;
            if (sayItButton != null)
            {
                StartCoroutine(PopScaleRoutine(sayItButton.transform, 1.15f, 0.2f));
            }
            StartCoroutine(HandleBlendSuccessRoutine());
        }

        private IEnumerator HandleBlendSuccessRoutine()
        {
            isTransitioning = true;
            if (blendSlideBar != null) blendSlideBar.SetInteractable(false);

            string displayWord = currentWord.fullWord.ToUpper();
            SetDialogue($"{displayWord}!");

            // 1. Play blended word sound AND squash SFX SIMULTANEOUSLY while wobbling!
            PlaySFX(activityData != null ? activityData.blendSquashSfx : null);
            if (currentWord.blendedWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentWord.blendedWordAudio);
            }

            // 2. Wobble sound boxes while word audio plays
            yield return StartCoroutine(WobbleBoxesRoutine(0.55f));

            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            // Word Picture Reveal
            if (pictureRevealPanel != null) pictureRevealPanel.SetActive(true);
            if (wholeWordTMP != null) wholeWordTMP.text = displayWord;
            if (wordPictureImage != null)
            {
                if (currentWord.wordPictureSprite != null)
                {
                    wordPictureImage.sprite = currentWord.wordPictureSprite;
                    wordPictureImage.gameObject.SetActive(true);
                }
                else
                {
                    wordPictureImage.gameObject.SetActive(false);
                }
            }

            float waitDuration = (currentWord.blendedWordAudio != null)
                ? Mathf.Max(0.6f, currentWord.blendedWordAudio.length - 0.4f)
                : 1.0f;
            yield return new WaitForSeconds(waitDuration);

            // Extra praise on first word
            if (currentWordIndex == 0)
            {
                SetDialogue($"{displayWord}! You read a word! You did that all by yourself.");
                if (activityData != null && activityData.firstSuccessPraiseClip != null)
                {
                    yield return PlayVoiceClip(activityData.firstSuccessPraiseClip);
                }
            }

            yield return new WaitForSeconds(0.8f);

            ResetSliderToZero();
            currentWordIndex++;
            isTransitioning = false;
            LoadWordRound(currentWordIndex);
        }

        private void ResetSliderToZero()
        {
            if (blendSlideBar != null)
            {
                blendSlideBar.ResetSlider();
            }
            if (unityBlendSlider != null)
            {
                unityBlendSlider.value = unityBlendSlider.minValue;
            }
            else if (slideBarContainer != null)
            {
                Slider s = slideBarContainer.GetComponentInChildren<Slider>(true);
                if (s != null) s.value = s.minValue;
            }
        }

        private IEnumerator WobbleBoxesRoutine(float duration)
        {
            if (soundBoxRects == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float decay = 1f - t;

                for (int i = 0; i < soundBoxRects.Length; i++)
                {
                    if (soundBoxRects[i] != null && i < initialBoxPositions.Length)
                    {
                        float phaseOffset = i * 1.2f;
                        float angle = Mathf.Sin((elapsed * 32f) + phaseOffset) * 11f * decay;
                        float scalePop = 1f + (Mathf.Sin((elapsed * 26f) + phaseOffset) * 0.2f * decay);
                        float yBump = Mathf.Sin((elapsed * 22f) + phaseOffset) * 9f * decay;

                        soundBoxRects[i].localRotation = Quaternion.Euler(0f, 0f, angle);
                        soundBoxRects[i].localScale = new Vector3(scalePop, scalePop, 1f);
                        soundBoxRects[i].localPosition = initialBoxPositions[i] + new Vector3(0f, yBump, 0f);
                    }
                }

                yield return null;
            }

            for (int i = 0; i < soundBoxRects.Length; i++)
            {
                if (soundBoxRects[i] != null && i < initialBoxPositions.Length)
                {
                    soundBoxRects[i].localRotation = Quaternion.identity;
                    soundBoxRects[i].localScale = Vector3.one;
                    soundBoxRects[i].localPosition = initialBoxPositions[i];
                }
            }
        }

        private void ReplayCurrentWordSounds()
        {
            if (currentWord == null) return;
            if (currentWord.blendedWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentWord.blendedWordAudio);
            }
        }

        private void CompleteStop1()
        {
            StartCoroutine(CompleteStop1Sequence());
        }

        private IEnumerator CompleteStop1Sequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            UpdateProgressUI(1.0f);

            SetDialogue("You blended every single sound! Tap continue to build words!");
            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            yield return new WaitForSeconds(1.0f);
            isTransitioning = false;
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (momoMascotObject != null) momoMascotObject.SetActive(false);
        }

        public void GoToNextPanel()
        {
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            TopicProgressUI.HideTopicCompletePanel();
            DeactivateMascots();

            if (nextPanel != null) nextPanel.SetActive(true);
            else if (unitContentPanel != null) unitContentPanel.SetActive(true);

            if (currentPanel != null)
            {
                currentPanel.SetActive(false);
                if (unitContentPanel != null) unitContentPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            TopicProgressUI.RefreshAllTicks();
        }

        private void SetDialogue(string message)
        {
            DialogueBoxAutoHider.SetDialogue(dialogueText, message, dialogueCanvasGroup);
        }

        private void PlaySFX(AudioClip clip)
        {
            if (sfxAudioSource != null && clip != null)
            {
                sfxAudioSource.PlayOneShot(clip);
            }
        }

        private void PlayVoiceClipNonBlocking(AudioClip clip)
        {
            if (voiceAudioSource != null && clip != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
            }
        }

        private IEnumerator PlayVoiceClip(AudioClip clip)
        {
            if (voiceAudioSource != null && clip != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(clip.length);
            }
        }

        private void UpdateProgressUI(float fillAmount)
        {
            if (progressRingFillImage != null) progressRingFillImage.fillAmount = fillAmount;
            if (progressText != null) progressText.text = $"{Mathf.RoundToInt(fillAmount * 100)}%";
        }

        private void TriggerWiggleStarMeter()
        {
            if (starMeterRect != null) StartCoroutine(PopScaleRoutine(starMeterRect, 1.15f, 0.3f));
        }

        private IEnumerator PopScaleRoutine(Transform target, float maxScaleMul, float duration)
        {
            if (target == null) yield break;
            Vector3 startScale = target.localScale;
            Vector3 maxScale = startScale * maxScaleMul;
            float half = duration * 0.5f;

            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(startScale, maxScale, elapsed / half);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                target.localScale = Vector3.Lerp(maxScale, startScale, elapsed / half);
                yield return null;
            }

            target.localScale = startScale;
        }
    }
}
