using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit7
{
    public class BackCarriageController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit7";
        [SerializeField] private string topicName = "BackCarriage";

        [Header("Data Asset")]
        [SerializeField] private BackCarriageData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Header / Dialogue UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Sound Train Rig UI")]
        [SerializeField] private RectTransform trainRigRect;
        [SerializeField] private RectTransform backCarriageSlot;        // Lit & Active
        [SerializeField] private GameObject backCarriageGlow;
        [SerializeField] private TMP_Text backCarriageLetterTMP;
        [SerializeField] private Image backCarriagePictureImage;
        [SerializeField] private GameObject middleCarriageCoverObject;  // Covered with sheet
        [SerializeField] private TMP_Text frontCarriageLetterTMP;       // Displays start letters (e.g. "va")
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

        [Header("Phase 1: Letter Tile Choices UI")]
        [SerializeField] private GameObject tileChoicesPanel;
        [SerializeField] private Button[] tileButtons = new Button[4];
        [SerializeField] private TMP_Text[] tileTexts = new TMP_Text[4];

        [Header("Phase 2: Front or Back Carriage Buttons")]
        [SerializeField] private GameObject positionChoicePanel;
        [SerializeField] private Button frontPositionButton;
        [SerializeField] private Button backPositionButton;
        [SerializeField] private TMP_Text positionPromptTMP;

        [Header("Phase 3: Tricky Endings Bonus Panel")]
        [SerializeField] private GameObject trickyBonusPanel;
        [SerializeField] private TMP_Text trickyWordTMP;
        [SerializeField] private Image trickyWordImage;
        [SerializeField] private TMP_Text trickyExplanationTMP;
        [SerializeField] private Button nextBonusWordButton;
        [SerializeField] private Button finishStop2Button;

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

        private int currentCleanIndex = 0;
        private int currentPosIndex = 0;
        private int currentTrickyIndex = 0;
        private bool isTransitioning = false;
        private BackCarriageItem currentCleanItem;
        private FrontOrBackQuizItem currentPosItem;
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
                activityData = Resources.Load<BackCarriageData>("Phonics2/Unit7/BackCarriageData_Unit7");
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
        }

        private void SetupButtonListeners()
        {
            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(ReplayCleanAudio);
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

            if (frontPositionButton != null)
            {
                frontPositionButton.onClick.RemoveAllListeners();
                frontPositionButton.onClick.AddListener(() => OnPositionSelected(CarriagePositionTarget.Front));
            }

            if (backPositionButton != null)
            {
                backPositionButton.onClick.RemoveAllListeners();
                backPositionButton.onClick.AddListener(() => OnPositionSelected(CarriagePositionTarget.Back));
            }

            if (nextBonusWordButton != null)
            {
                nextBonusWordButton.onClick.RemoveAllListeners();
                nextBonusWordButton.onClick.AddListener(OnNextBonusWordClicked);
            }

            if (finishStop2Button != null)
            {
                finishStop2Button.onClick.RemoveAllListeners();
                finishStop2Button.onClick.AddListener(CompleteStop2);
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
            currentCleanIndex = 0;
            currentPosIndex = 0;
            currentTrickyIndex = 0;
            isTransitioning = true;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(false);

            if (middleCarriageCoverObject != null) middleCarriageCoverObject.SetActive(true);
            if (backCarriageGlow != null) backCarriageGlow.SetActive(true);

            if (tileChoicesPanel != null) tileChoicesPanel.SetActive(false);
            if (positionChoicePanel != null) positionChoicePanel.SetActive(false);
            if (trickyBonusPanel != null) trickyBonusPanel.SetActive(false);

            StartCoroutine(StartActivityIntroSequence());
        }

        private IEnumerator StartActivityIntroSequence()
        {
            isTransitioning = true;
            if (tileChoicesPanel != null) tileChoicesPanel.SetActive(false);
            if (positionChoicePanel != null) positionChoicePanel.SetActive(false);
            if (trickyBonusPanel != null) trickyBonusPanel.SetActive(false);

            // Animate train sliding into screen from right to left using configured entrance speed
            if (trainRigRect != null)
            {
                yield return StartCoroutine(SlideTrainInFromRight(trainEntranceSpeed));
            }

            SetDialogue("The LAST sound rides at the back. It is the one people forget — so listen hard!");
            PlaySFX(activityData != null ? activityData.trainWhistleSfx : null);

            if (activityData != null && activityData.leoIntroClip != null)
            {
                yield return PlayVoiceClip(activityData.leoIntroClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            if (activityData != null && activityData.instructionClip != null)
            {
                SetDialogue("What is the last sound? Listen...");
                yield return PlayVoiceClip(activityData.instructionClip);
            }

            // Brief pause after intro completes
            yield return new WaitForSeconds(0.2f);

            isTransitioning = false;
            LoadCleanRound(0);
        }

        #region Phase 1: Clean Endings (8 Rounds)

        private void LoadCleanRound(int index)
        {
            if (activityData == null || activityData.cleanEndingItems == null || index >= activityData.cleanEndingItems.Length)
            {
                StartPositionPhase();
                return;
            }

            if (tileChoicesPanel != null) tileChoicesPanel.SetActive(true);
            if (positionChoicePanel != null) positionChoicePanel.SetActive(false);
            if (trickyBonusPanel != null) trickyBonusPanel.SetActive(false);

            currentCleanIndex = index;
            isTransitioning = false;
            currentCleanItem = activityData.cleanEndingItems[index];

            if (frontCarriageLetterTMP != null)
            {
                string leadLetters = currentCleanItem.fullWord.Substring(0, Mathf.Max(1, currentCleanItem.fullWord.Length - 1));
                frontCarriageLetterTMP.text = leadLetters;
            }

            if (backCarriageLetterTMP != null) backCarriageLetterTMP.text = "_";
            if (backCarriagePictureImage != null && currentCleanItem.wordPictureSprite != null)
            {
                backCarriagePictureImage.sprite = currentCleanItem.wordPictureSprite;
                backCarriagePictureImage.gameObject.SetActive(true);
            }

            for (int i = 0; i < tileButtons.Length; i++)
            {
                if (tileButtons[i] != null && i < currentCleanItem.tileOptions.Length)
                {
                    tileButtons[i].gameObject.SetActive(true);
                    if (tileTexts != null && i < tileTexts.Length && tileTexts[i] != null)
                    {
                        tileTexts[i].text = currentCleanItem.tileOptions[i];
                    }
                }
                else if (tileButtons[i] != null)
                {
                    tileButtons[i].gameObject.SetActive(false);
                }
            }

            SetDialogue($"What is the last sound in '{currentCleanItem.fullWord.ToUpper()}'? Listen!");
            if (currentCleanItem.heldEndingWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentCleanItem.heldEndingWordAudio);
            }
            else if (currentCleanItem.normalWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentCleanItem.normalWordAudio);
            }

            UpdateProgressUI(0.05f + (index / 8f) * 0.45f);
        }

        private void ReplayCleanAudio()
        {
            if (currentCleanItem == null) return;
            if (currentCleanItem.heldEndingWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentCleanItem.heldEndingWordAudio);
            }
            else if (currentCleanItem.normalWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentCleanItem.normalWordAudio);
            }
        }

        private void OnTileSelected(int tileIndex)
        {
            if (isTransitioning || currentCleanItem == null || tileIndex >= currentCleanItem.tileOptions.Length) return;

            string selectedLetter = currentCleanItem.tileOptions[tileIndex];
            bool isCorrect = string.Equals(selectedLetter, currentCleanItem.lastLetter, System.StringComparison.OrdinalIgnoreCase);
            Button chosenBtn = tileButtons[tileIndex];

            if (chosenBtn != null)
            {
                StartCoroutine(FlashButtonFeedback(chosenBtn, isCorrect, 0.6f));
            }

            if (isCorrect)
            {
                StartCoroutine(HandleCleanTileCorrect(selectedLetter));
            }
            else
            {
                StartCoroutine(HandleCleanTileWrong());
            }
        }

        private IEnumerator HandleCleanTileCorrect(string letter)
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.tileSnapSfx : null);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            if (backCarriageLetterTMP != null)
            {
                backCarriageLetterTMP.text = $"<color=#4CAF50><b>{letter}</b></color>";
                StartCoroutine(PopScaleRoutine(backCarriageLetterTMP.transform, 1.35f, 0.35f));
            }

            SetDialogue($"Yes! /{letter}/ at the end. {currentCleanItem.fullWord.ToUpper()}!");
            if (currentCleanItem.successVoiceClip != null)
            {
                yield return PlayVoiceClip(currentCleanItem.successVoiceClip);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            currentCleanIndex++;
            isTransitioning = false;
            LoadCleanRound(currentCleanIndex);
        }

        private IEnumerator HandleCleanTileWrong()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.retryGentleSfx : null);

            SetDialogue($"Say it slowly with me — {currentCleanItem.fullWord}. Hear /{currentCleanItem.lastLetter}/ at the end?");
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

        #endregion

        #region Phase 2: Front or Back Position (4 Rounds)

        private void StartPositionPhase()
        {
            if (tileChoicesPanel != null) tileChoicesPanel.SetActive(false);
            if (positionChoicePanel != null) positionChoicePanel.SetActive(true);
            if (trickyBonusPanel != null) trickyBonusPanel.SetActive(false);

            currentPosIndex = 0;
            LoadPositionRound(0);
        }

        private void LoadPositionRound(int index)
        {
            if (activityData == null || activityData.frontOrBackItems == null || index >= activityData.frontOrBackItems.Length)
            {
                StartTrickyBonusPhase();
                return;
            }

            currentPosIndex = index;
            isTransitioning = false;
            currentPosItem = activityData.frontOrBackItems[index];

            string prompt = $"Is {currentPosItem.testSound} at the FRONT of '{currentPosItem.fullWord.ToUpper()}', or at the BACK?";
            if (positionPromptTMP != null) positionPromptTMP.text = prompt;
            SetDialogue(prompt);

            if (currentPosItem.promptAudioClip != null)
            {
                PlayVoiceClipNonBlocking(currentPosItem.promptAudioClip);
            }

            UpdateProgressUI(0.55f + (index / 4f) * 0.25f);
        }

        private void OnPositionSelected(CarriagePositionTarget chosenTarget)
        {
            if (isTransitioning || currentPosItem == null) return;

            bool isCorrect = (chosenTarget == currentPosItem.correctPosition);
            Button chosenBtn = (chosenTarget == CarriagePositionTarget.Front) ? frontPositionButton : backPositionButton;

            if (chosenBtn != null)
            {
                StartCoroutine(FlashButtonFeedback(chosenBtn, isCorrect, 0.6f));
            }

            if (isCorrect)
            {
                PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
                TriggerWiggleStarMeter();
                SetDialogue($"Spot on! {currentPosItem.testSound} is at the {(chosenTarget == CarriagePositionTarget.Front ? "FRONT" : "BACK")} of {currentPosItem.fullWord.ToUpper()}!");
                currentPosIndex++;
                LoadPositionRound(currentPosIndex);
            }
            else
            {
                PlaySFX(activityData != null ? activityData.retryGentleSfx : null);
                SetDialogue($"Listen closely to '{currentPosItem.fullWord.ToUpper()}'. Front or back?");
            }
        }

        #endregion

        #region Phase 3: Tricky Endings Bonus Track (3 Rounds)

        private void StartTrickyBonusPhase()
        {
            if (positionChoicePanel != null) positionChoicePanel.SetActive(false);
            if (trickyBonusPanel != null) trickyBonusPanel.SetActive(true);

            currentTrickyIndex = 0;
            LoadTrickyRound(0);
        }

        private void LoadTrickyRound(int index)
        {
            if (activityData == null || activityData.trickyBonusItems == null || index >= activityData.trickyBonusItems.Length)
            {
                if (finishStop2Button != null) finishStop2Button.gameObject.SetActive(true);
                return;
            }

            currentTrickyIndex = index;
            isTransitioning = false;
            TrickyEndingBonusItem item = activityData.trickyBonusItems[index];

            if (trickyWordTMP != null) trickyWordTMP.text = item.fullWord.ToUpper();
            if (trickyWordImage != null && item.wordPictureSprite != null)
            {
                trickyWordImage.sprite = item.wordPictureSprite;
                trickyWordImage.gameObject.SetActive(true);
            }

            if (trickyExplanationTMP != null) trickyExplanationTMP.text = item.explanationText;

            SetDialogue($"Bonus Track: '{item.fullWord.ToUpper()}' has TWO sounds at the end ({item.endingTwoSounds})!");
            if (item.wordAudioClip != null)
            {
                PlayVoiceClipNonBlocking(item.wordAudioClip);
            }

            if (nextBonusWordButton != null)
            {
                nextBonusWordButton.gameObject.SetActive(index < activityData.trickyBonusItems.Length - 1);
            }
            if (finishStop2Button != null)
            {
                finishStop2Button.gameObject.SetActive(index >= activityData.trickyBonusItems.Length - 1);
            }

            UpdateProgressUI(0.85f + (index / 3f) * 0.12f);
        }

        private void OnNextBonusWordClicked()
        {
            currentTrickyIndex++;
            LoadTrickyRound(currentTrickyIndex);
        }

        #endregion

        public void CompleteStop2()
        {
            StartCoroutine(CompleteStop2Sequence());
        }

        private IEnumerator CompleteStop2Sequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.trainWhistleSfx : null);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            UpdateProgressUI(1.0f);

            SetDialogue("You caught every last sound. Most people miss those! Awesome work!");
            if (activityData != null && activityData.leoClosingClip != null)
            {
                yield return PlayVoiceClip(activityData.leoClosingClip);
            }

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
