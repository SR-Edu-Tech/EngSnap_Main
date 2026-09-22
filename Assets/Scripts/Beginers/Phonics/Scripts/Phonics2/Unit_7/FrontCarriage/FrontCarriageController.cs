using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;
using EngSnap.Phonics2.Unit2;

namespace EngSnap.Phonics2.Unit7
{
    public class FrontCarriageController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit7";
        [SerializeField] private string topicName = "FrontCarriage";

        [Header("Data Asset")]
        [SerializeField] private FrontCarriageData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Header / Dialogue UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Sound Train Rig UI (3 Carriages)")]
        [SerializeField] private RectTransform trainRigRect;
        [SerializeField] private RectTransform frontCarriageSlot;       // Lit & Active
        [SerializeField] private GameObject frontCarriageGlow;
        [SerializeField] private TMP_Text frontCarriageLetterTMP;
        [SerializeField] private Image frontCarriagePictureImage;
        [SerializeField] private GameObject middleCarriageCoverObject;  // Covered with sheet
        [SerializeField] private GameObject backCarriageCoverObject;    // Covered with sheet
        [SerializeField] private Button replayAudioButton;

        [Header("Train Motion & Speed Settings")]
        [Tooltip("Speed/Duration in seconds for the train to drive in from right to center.")]
        [Range(0.2f, 4f)] [SerializeField] private float trainEntranceSpeed = 0.85f;

        [Tooltip("Speed/Duration in seconds for the train to drive off to the left upon completion.")]
        [Range(0.2f, 4f)] [SerializeField] private float trainDepartureSpeed = 0.9f;

        [Tooltip("Off-screen X horizontal travel distance in UI units.")]
        [SerializeField] private float trainTravelDistanceX = 1400f;

        [Tooltip("Optional custom animation curve for entrance easing.")]
        [SerializeField] private AnimationCurve entranceEasingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Optional custom animation curve for departure easing.")]
        [SerializeField] private AnimationCurve departureEasingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Tooltip("Enable subtle vertical wheel bump while the train is moving.")]
        [SerializeField] private bool enableWheelBumping = true;
        [Range(0f, 20f)] [SerializeField] private float wheelBumpIntensity = 6f;
        [Range(1f, 30f)] [SerializeField] private float wheelBumpFrequency = 14f;

        [Header("Letter Tile Choices UI")]
        [SerializeField] private GameObject tileChoicesPanel;
        [SerializeField] private Button[] tileButtons = new Button[4];
        [SerializeField] private TMP_Text[] tileTexts = new TMP_Text[4];

        [Header("Finger Tracing UI (Unit 2 System)")]
        [SerializeField] private GameObject tracingPanel;
        [SerializeField] private Image pictureImage;
        [SerializeField] private TMP_Text wordDisplayTMP;
        [SerializeField] private Image wordGlowHighlight;
        [SerializeField] private LetterTracingComponent letterTracingComponent;
        [SerializeField] private GameObject momoHintObject;
        [SerializeField] private Button skipTracingButton;

        [Header("Progress & Mascot UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;
        [SerializeField] private GameObject leoMascotObject;

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private readonly Color correctGreenColor = new Color(0.298f, 0.686f, 0.314f, 1f); // #4CAF50
        private readonly Color wrongRedColor = new Color(0.937f, 0.325f, 0.314f, 1f);     // #EF5350

        private int currentRoundIndex = 0;
        private int tracingFailAttempts = 0;
        private bool isTransitioning = false;
        private bool isWaitingForTracing = false;
        private bool isSkipTracingTriggered = false;
        private FrontCarriageItem currentItem;
        private Vector2 defaultTrainAnchoredPos;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            if (trainRigRect != null)
            {
                defaultTrainAnchoredPos = trainRigRect.anchoredPosition;
            }
            EnsureAudioSources();
            EnsureDataAssigned();
        }

        private void Start()
        {
            SetupButtonListeners();
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

        private void OnDestroy()
        {
            if (letterTracingComponent != null)
            {
                letterTracingComponent.OnTracingCompleted -= HandleTracingCompleted;
                letterTracingComponent.OnTracingFailedAttempt -= HandleTracingFailedAttempt;
            }
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
                activityData = Resources.Load<FrontCarriageData>("Phonics2/Unit7/FrontCarriageData_Unit7");
            }
        }

        private void StopAllAudio()
        {
            if (voiceAudioSource != null && voiceAudioSource.isPlaying) voiceAudioSource.Stop();
            if (sfxAudioSource != null && sfxAudioSource.isPlaying) sfxAudioSource.Stop();
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
        }

        private void SetupButtonListeners()
        {
            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(ReplayCurrentAudio);
            }

            if (skipTracingButton != null)
            {
                skipTracingButton.onClick.RemoveAllListeners();
                skipTracingButton.onClick.AddListener(() => { isSkipTracingTriggered = true; });
            }

            if (letterTracingComponent != null)
            {
                letterTracingComponent.OnTracingCompleted -= HandleTracingCompleted;
                letterTracingComponent.OnTracingCompleted += HandleTracingCompleted;
                letterTracingComponent.OnTracingFailedAttempt -= HandleTracingFailedAttempt;
                letterTracingComponent.OnTracingFailedAttempt += HandleTracingFailedAttempt;
            }

            for (int i = 0; i < tileButtons.Length; i++)
            {
                int index = i;
                if (tileButtons[i] != null)
                {
                    tileButtons[i].onClick.RemoveAllListeners();
                    tileButtons[i].onClick.AddListener(() => OnTileSelected(index));
                }
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

        public void StartActivity()
        {
            currentRoundIndex = 0;
            isTransitioning = true;
            isSkipTracingTriggered = false;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (tracingPanel != null) tracingPanel.SetActive(false);
            if (tileChoicesPanel != null) tileChoicesPanel.SetActive(false);

            if (middleCarriageCoverObject != null) middleCarriageCoverObject.SetActive(true);
            if (backCarriageCoverObject != null) backCarriageCoverObject.SetActive(true);
            if (frontCarriageGlow != null) frontCarriageGlow.SetActive(true);

            StartCoroutine(StartActivityIntroSequence());
        }

        private IEnumerator StartActivityIntroSequence()
        {
            isTransitioning = true;
            if (tileChoicesPanel != null) tileChoicesPanel.SetActive(false);

            // Animate train sliding into screen from right to left using configured entrance speed
            if (trainRigRect != null)
            {
                yield return StartCoroutine(SlideTrainInFromRight(trainEntranceSpeed));
            }

            SetDialogue("All aboard the Sound Train! Today we find where sounds sit inside a word.");
            PlaySFX(activityData != null ? activityData.trainWhistleSfx : null);

            if (activityData != null && activityData.leoIntroClip != null)
            {
                yield return PlayVoiceClip(activityData.leoIntroClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            SetDialogue("This is the FRONT carriage. The first sound rides here.");
            if (activityData != null && activityData.frontCarriageTeachClip != null)
            {
                yield return PlayVoiceClip(activityData.frontCarriageTeachClip);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            // Wait a brief moment before activating the choices panel
            yield return new WaitForSeconds(0.2f);

            isTransitioning = false;
            LoadRound(0);
        }

        private void LoadRound(int index)
        {
            if (activityData == null || activityData.beginningItems == null || index >= activityData.beginningItems.Length)
            {
                CompleteStop1();
                return;
            }

            currentRoundIndex = index;
            isTransitioning = false;
            currentItem = activityData.beginningItems[index];

            if (frontCarriageLetterTMP != null) frontCarriageLetterTMP.text = "_";
            if (frontCarriagePictureImage != null && currentItem.wordPictureSprite != null)
            {
                frontCarriagePictureImage.sprite = currentItem.wordPictureSprite;
                frontCarriagePictureImage.gameObject.SetActive(true);
            }

            if (tileChoicesPanel != null) tileChoicesPanel.SetActive(true);
            if (tracingPanel != null) tracingPanel.SetActive(false);

            for (int i = 0; i < tileButtons.Length; i++)
            {
                if (tileButtons[i] != null && i < currentItem.tileOptions.Length)
                {
                    tileButtons[i].gameObject.SetActive(true);
                    if (tileTexts != null && i < tileTexts.Length && tileTexts[i] != null)
                    {
                        tileTexts[i].text = currentItem.tileOptions[i];
                    }
                }
                else if (tileButtons[i] != null)
                {
                    tileButtons[i].gameObject.SetActive(false);
                }
            }

            SetDialogue($"Which letter goes in the front? Listen — {currentItem.fullWord}!");
            if (currentItem.stretchedWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentItem.stretchedWordAudio);
            }
            else if (currentItem.normalWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentItem.normalWordAudio);
            }

            UpdateProgressUI(index / 10f);
        }

        private void ReplayCurrentAudio()
        {
            if (currentItem == null) return;
            if (currentItem.stretchedWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentItem.stretchedWordAudio);
            }
            else if (currentItem.normalWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentItem.normalWordAudio);
            }
        }

        private void OnTileSelected(int tileIndex)
        {
            if (isTransitioning || currentItem == null || tileIndex >= currentItem.tileOptions.Length) return;

            string selectedLetter = currentItem.tileOptions[tileIndex];
            bool isCorrect = string.Equals(selectedLetter, currentItem.firstLetter, System.StringComparison.OrdinalIgnoreCase);
            Button chosenBtn = tileButtons[tileIndex];

            if (chosenBtn != null)
            {
                StartCoroutine(FlashButtonFeedback(chosenBtn, isCorrect, 0.6f));
            }

            if (isCorrect)
            {
                StartCoroutine(HandleTileCorrect(selectedLetter));
            }
            else
            {
                StartCoroutine(HandleTileWrong());
            }
        }

        private IEnumerator HandleTileCorrect(string letter)
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.tileSnapSfx : null);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            if (frontCarriageLetterTMP != null)
            {
                frontCarriageLetterTMP.text = $"<color=#4CAF50><b>{letter}</b></color>";
                StartCoroutine(PopScaleRoutine(frontCarriageLetterTMP.transform, 1.35f, 0.35f));
            }

            SetDialogue(string.IsNullOrEmpty(currentItem.successVoiceClip ? "" : "Yes!") ? $"Yes! '{currentItem.fullWord.ToUpper()}' starts with /{letter}/!" : $"Yes! '{currentItem.fullWord.ToUpper()}' starts with /{letter}/!");
            if (currentItem.successVoiceClip != null)
            {
                yield return PlayVoiceClip(currentItem.successVoiceClip);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            // Check if this round includes finger tracing
            if (currentItem.isTracingRound && letterTracingComponent != null)
            {
                yield return StartCoroutine(RunTracingRound(letter));
            }

            currentRoundIndex++;
            isTransitioning = false;
            LoadRound(currentRoundIndex);
        }

        private IEnumerator HandleTileWrong()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.retryGentleSfx : null);

            SetDialogue($"Listen to just the start — {currentItem.firstLetter}{currentItem.firstLetter}{currentItem.firstLetter}. Which letter is that?");
            if (activityData != null && activityData.retryClip != null)
            {
                yield return PlayVoiceClip(activityData.retryClip);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            isTransitioning = false;
        }

        private IEnumerator RunTracingRound(string letter)
        {
            if (tileChoicesPanel != null) tileChoicesPanel.SetActive(false);
            if (tracingPanel != null) tracingPanel.SetActive(true);
            if (momoHintObject != null) momoHintObject.SetActive(false);

            tracingFailAttempts = 0;
            isWaitingForTracing = true;
            isSkipTracingTriggered = false;

            // Unit 2 Tracing Visual Setup
            if (pictureImage != null && currentItem != null && currentItem.wordPictureSprite != null)
            {
                pictureImage.sprite = currentItem.wordPictureSprite;
                pictureImage.gameObject.SetActive(true);
            }

            if (wordDisplayTMP != null && currentItem != null)
            {
                wordDisplayTMP.text = currentItem.displayGapWord;
                wordDisplayTMP.gameObject.SetActive(true);
            }

            if (wordGlowHighlight != null)
            {
                wordGlowHighlight.gameObject.SetActive(false);
            }

            SetDialogue($"Trace the letter '{letter.ToUpper()}' — and say the sound as you go.");
            if (activityData != null && activityData.tracePromptClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.tracePromptClip);
            }

            if (letterTracingComponent != null)
            {
                letterTracingComponent.gameObject.SetActive(true);
                AudioClip traceAudio = currentItem.stretchedWordAudio != null ? currentItem.stretchedWordAudio : currentItem.normalWordAudio;
                char traceChar = !string.IsNullOrEmpty(letter) ? letter[0] : 'a';

                letterTracingComponent.SetupTracing(
                    currentItem.tracingOutlineSprite,
                    traceAudio,
                    traceChar,
                    currentItem.filledLetterSprite,
                    currentItem.checkpointPositions
                );
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
                isWaitingForTracing = false;
            }

            // Wait for Unit 2 Event OnTracingCompleted
            float timeout = 35f;
            float elapsed = 0f;
            while (isWaitingForTracing && !isSkipTracingTriggered && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (tracingPanel != null) tracingPanel.SetActive(false);
            if (tileChoicesPanel != null) tileChoicesPanel.SetActive(true);
            if (momoHintObject != null) momoHintObject.SetActive(false);
        }

        private void HandleTracingFailedAttempt()
        {
            if (!isWaitingForTracing) return;

            tracingFailAttempts++;
            PlaySFX(activityData != null ? activityData.retryGentleSfx : null);

            if (tracingFailAttempts >= 2)
            {
                if (momoHintObject != null) momoHintObject.SetActive(true);
                SetDialogue("Follow Momo! Start at the glowing checkpoint and draw smooth.");
                if (letterTracingComponent != null) letterTracingComponent.PlayGhostFingerGuide();
            }
        }

        private void HandleTracingCompleted()
        {
            if (!isWaitingForTracing || currentItem == null) return;
            StartCoroutine(HandleTracingCompletedSequence());
        }

        private IEnumerator HandleTracingCompletedSequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.tileSnapSfx : null);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            if (wordDisplayTMP != null && currentItem != null)
            {
                wordDisplayTMP.text = currentItem.fullWord.ToUpper();
            }

            if (wordGlowHighlight != null)
            {
                wordGlowHighlight.gameObject.SetActive(true);
            }

            SetDialogue($"Awesome! You traced '{currentItem.firstLetter.ToUpper()}' for '{currentItem.fullWord.ToUpper()}'!");

            if (currentItem != null && currentItem.normalWordAudio != null)
            {
                yield return PlayVoiceClip(currentItem.normalWordAudio);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            isWaitingForTracing = false;
            isTransitioning = false;
        }

        public void CompleteStop1()
        {
            StartCoroutine(CompleteStop1Sequence());
        }

        private IEnumerator CompleteStop1Sequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.trainWhistleSfx : null);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            UpdateProgressUI(1.0f);

            SetDialogue("You found every starting sound! The front carriage is rolling!");

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.gameObject.SetActive(true);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            // Animate train driving off smoothly to the left using configured departure speed
            if (trainRigRect != null)
            {
                yield return StartCoroutine(SlideTrainOffToLeft(trainDepartureSpeed));
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            isTransitioning = false;
        }

        private IEnumerator SlideTrainInFromRight(float duration)
        {
            if (trainRigRect == null) yield break;

            PlaySFX(activityData != null ? activityData.trainChugSfx : null);

            Vector2 offscreenRightPos = defaultTrainAnchoredPos + new Vector2(trainTravelDistanceX, 0f);
            trainRigRect.anchoredPosition = offscreenRightPos;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Use easing curve if available, otherwise smooth ease-out
                float curveT = entranceEasingCurve != null ? entranceEasingCurve.Evaluate(t) : (1f - Mathf.Pow(1f - t, 3f));
                Vector2 targetPos = Vector2.Lerp(offscreenRightPos, defaultTrainAnchoredPos, curveT);

                // Subtle rhythmic train wheel bump
                if (enableWheelBumping)
                {
                    float bump = Mathf.Sin(elapsed * wheelBumpFrequency) * wheelBumpIntensity * (1f - t);
                    targetPos.y += bump;
                }

                trainRigRect.anchoredPosition = targetPos;
                yield return null;
            }
            trainRigRect.anchoredPosition = defaultTrainAnchoredPos;
        }

        private IEnumerator SlideTrainOffToLeft(float duration)
        {
            if (trainRigRect == null) yield break;

            PlaySFX(activityData != null ? activityData.trainChugSfx : null);

            Vector2 offscreenLeftPos = defaultTrainAnchoredPos + new Vector2(-trainTravelDistanceX, 0f);
            Vector2 startPos = trainRigRect.anchoredPosition;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Use easing curve if available, otherwise smooth ease-in
                float curveT = departureEasingCurve != null ? departureEasingCurve.Evaluate(t) : (t * t);
                Vector2 targetPos = Vector2.Lerp(startPos, offscreenLeftPos, curveT);

                // Subtle rhythmic train wheel bump
                if (enableWheelBumping)
                {
                    float bump = Mathf.Sin(elapsed * wheelBumpFrequency) * wheelBumpIntensity * t;
                    targetPos.y += bump;
                }

                trainRigRect.anchoredPosition = targetPos;
                yield return null;
            }
            trainRigRect.anchoredPosition = offscreenLeftPos;
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
            if (starMeterRect != null) StartCoroutine(WiggleAnimation(starMeterRect));
        }

        private IEnumerator FlashButtonFeedback(Button btn, bool isCorrect, float duration = 0.5f)
        {
            if (btn == null) yield break;
            Image img = btn.GetComponent<Image>();
            Color originalColor = (img != null) ? img.color : Color.white;
            RectTransform rt = btn.GetComponent<RectTransform>();

            if (img != null)
            {
                img.color = isCorrect ? correctGreenColor : wrongRedColor;
            }

            if (rt != null)
            {
                if (isCorrect)
                {
                    StartCoroutine(WiggleAnimation(rt));
                }
                else
                {
                    StartCoroutine(ShakeAnimation(rt));
                }
            }

            yield return new WaitForSeconds(duration);

            if (img != null)
            {
                img.color = originalColor;
            }
        }

        private IEnumerator WiggleAnimation(RectTransform target)
        {
            float elapsed = 0f;
            float duration = 0.3f;
            Vector3 startScale = target.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = 1f + Mathf.Sin(elapsed * 25f) * 0.12f;
                target.localScale = startScale * scale;
                yield return null;
            }
            target.localScale = startScale;
        }

        private IEnumerator ShakeAnimation(RectTransform target)
        {
            if (target == null) yield break;
            Vector3 startPos = target.localPosition;
            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float offset = Mathf.Sin(elapsed * 40f) * 12f * (1f - elapsed / duration);
                target.localPosition = startPos + new Vector3(offset, 0f, 0f);
                yield return null;
            }
            target.localPosition = startPos;
        }

        private IEnumerator PopScaleRoutine(Transform target, float targetScale, float duration)
        {
            if (target == null) yield break;
            Vector3 startScale = target.localScale;
            Vector3 maxScale = startScale * targetScale;
            float halfDuration = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                target.localScale = Vector3.Lerp(startScale, maxScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < halfDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / halfDuration);
                target.localScale = Vector3.Lerp(maxScale, startScale, t);
                yield return null;
            }

            target.localScale = startScale;
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
                unitContentPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            TopicProgressUI.RefreshAllTicks();
        }
    }
}
