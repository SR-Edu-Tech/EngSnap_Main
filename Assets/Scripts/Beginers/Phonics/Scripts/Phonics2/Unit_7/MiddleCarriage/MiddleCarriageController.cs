using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit7
{
    public class MiddleCarriageController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit7";
        [SerializeField] private string topicName = "MiddleCarriage";

        [Header("Data Asset")]
        [SerializeField] private MiddleCarriageData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Header / Dialogue UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Sound Train Rig UI (Middle Carriage Active)")]
        [SerializeField] private RectTransform trainRigRect;
        [SerializeField] private RectTransform middleCarriageSlot;     // Lit & Active Drop Target
        [SerializeField] private GameObject middleCarriageGlow;
        [SerializeField] private TMP_Text frontCarriageLetterTMP;      // Displays first letter
        [SerializeField] private TMP_Text middleCarriageLetterTMP;     // Displays "_" -> vowel
        [SerializeField] private TMP_Text backCarriageLetterTMP;       // Displays last letter
        [SerializeField] private Image middleCarriagePictureImage;
        [SerializeField] private Button replayAudioButton;
        [SerializeField] private Button helpStretchAudioButton;        // "Help / Stretch Word" button

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

        [Header("Phase 1: 5 Trackside Vowel Houses (a, e, i, o, u)")]
        [SerializeField] private GameObject vowelHousesPanel;
        [SerializeField] private Button[] vowelHouseButtons = new Button[5];
        [SerializeField] private Image[] vowelHouseImages = new Image[5];
        [SerializeField] private TMP_Text[] vowelHouseTexts = new TMP_Text[5];
        [SerializeField] private MiddleCarriageVowelHouseTile[] vowelHouseTiles = new MiddleCarriageVowelHouseTile[5];

        [Header("Phase 2: Odd One Out UI (3 Rounds)")]
        [SerializeField] private GameObject oddOneOutPanel;
        [SerializeField] private TMP_Text oddPromptTMP;
        [SerializeField] private Button[] oddChoiceButtons = new Button[3];
        [SerializeField] private TMP_Text[] oddChoiceTexts = new TMP_Text[3];
        [SerializeField] private Image[] oddChoiceImages = new Image[3];

        [Header("Progress & Mascot UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject momoHintObject;

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

        private int currentMiddleIndex = 0;
        private int currentOddIndex = 0;
        private int retryAttempts = 0;
        private bool isTransitioning = false;
        private MiddleCarriageItem currentItem;
        private OddOneOutVowelItem currentOddItem;
        private Vector2 defaultTrainAnchoredPos;

        private readonly string[] vowels = new string[] { "a", "e", "i", "o", "u" };

        public bool IsTransitioning => isTransitioning;
        public RectTransform MiddleCarriageSlotRect => middleCarriageSlot;

        private void Awake()
        {
            EnsureAudioSources();
            EnsureDataAssigned();
            if (trainRigRect != null) defaultTrainAnchoredPos = trainRigRect.anchoredPosition;
        }

        private void Start()
        {
            SetupButtonListeners();
            SetupVowelHouseVisuals();
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
                activityData = Resources.Load<MiddleCarriageData>("Phonics2/Unit7/MiddleCarriageData_Unit7");
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

        private void SetupVowelHouseVisuals()
        {
            for (int i = 0; i < vowelHouseButtons.Length && i < vowels.Length; i++)
            {
                if (vowelHouseTexts != null && i < vowelHouseTexts.Length && vowelHouseTexts[i] != null)
                {
                    vowelHouseTexts[i].text = vowels[i];
                }

                if (vowelHouseImages != null && i < vowelHouseImages.Length && vowelHouseImages[i] != null)
                {
                    if (activityData != null && activityData.vowelHouseSprites != null && i < activityData.vowelHouseSprites.Length && activityData.vowelHouseSprites[i] != null)
                    {
                        vowelHouseImages[i].sprite = activityData.vowelHouseSprites[i];
                        vowelHouseImages[i].gameObject.SetActive(true);
                    }
                }

                // Attach/configure drag-and-drop tile component
                if (vowelHouseButtons[i] != null)
                {
                    MiddleCarriageVowelHouseTile tile = vowelHouseButtons[i].GetComponent<MiddleCarriageVowelHouseTile>();
                    if (tile == null)
                    {
                        tile = vowelHouseButtons[i].gameObject.AddComponent<MiddleCarriageVowelHouseTile>();
                    }
                    tile.Setup(i, vowels[i], this);
                    if (i < vowelHouseTiles.Length)
                    {
                        vowelHouseTiles[i] = tile;
                    }
                }
            }
        }

        private void SetupButtonListeners()
        {
            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(ReplayCurrentAudio);
            }

            if (helpStretchAudioButton != null)
            {
                helpStretchAudioButton.onClick.RemoveAllListeners();
                helpStretchAudioButton.onClick.AddListener(PlayStretchedAudioHelp);
            }

            for (int i = 0; i < vowelHouseButtons.Length; i++)
            {
                int index = i;
                if (vowelHouseButtons[i] != null)
                {
                    vowelHouseButtons[i].onClick.RemoveAllListeners();
                    vowelHouseButtons[i].onClick.AddListener(() => OnVowelHouseSelected(index));
                }
            }

            for (int i = 0; i < oddChoiceButtons.Length; i++)
            {
                int index = i;
                if (oddChoiceButtons[i] != null)
                {
                    oddChoiceButtons[i].onClick.RemoveAllListeners();
                    oddChoiceButtons[i].onClick.AddListener(() => OnOddChoiceSelected(index));
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
            currentMiddleIndex = 0;
            currentOddIndex = 0;
            retryAttempts = 0;
            isTransitioning = true;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);

            if (middleCarriageGlow != null) middleCarriageGlow.SetActive(true);

            if (vowelHousesPanel != null) vowelHousesPanel.SetActive(false);
            if (oddOneOutPanel != null) oddOneOutPanel.SetActive(false);

            StartCoroutine(StartActivityIntroSequence());
        }

        private IEnumerator StartActivityIntroSequence()
        {
            isTransitioning = true;
            if (vowelHousesPanel != null) vowelHousesPanel.SetActive(false);
            if (oddOneOutPanel != null) oddOneOutPanel.SetActive(false);

            // Animate train driving into view
            if (trainRigRect != null)
            {
                yield return StartCoroutine(SlideTrainInFromRight(trainEntranceSpeed));
            }

            SetDialogue("Now the hardest one — the MIDDLE. The middle sound likes to hide!");
            PlaySFX(activityData != null ? activityData.trainWhistleSfx : null);

            if (activityData != null && activityData.leoIntroClip != null)
            {
                yield return PlayVoiceClip(activityData.leoIntroClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            // Demonstration: "Fan. f - a - n. Faaaaan. Hear it in the middle? /a/!"
            SetDialogue("Fan. f - a - n. Faaaaan. Hear it in the middle? /a/!");

            if (middleCarriageSlot != null)
            {
                StartCoroutine(PopScaleRoutine(middleCarriageSlot, 1.15f, 0.8f));
            }

            if (middleCarriageGlow != null)
            {
                middleCarriageGlow.SetActive(true);
            }

            if (activityData != null && activityData.demonstrationClip != null)
            {
                yield return PlayVoiceClip(activityData.demonstrationClip);
            }
            else
            {
                yield return new WaitForSeconds(1.8f);
            }

            yield return new WaitForSeconds(0.2f);

            isTransitioning = false;
            LoadMiddleRound(0);
        }

        #region Phase 1: Middle Vowel Rounds (10 Rounds)

        private void LoadMiddleRound(int index)
        {
            if (activityData == null || activityData.middleItems == null || index >= activityData.middleItems.Length)
            {
                StartOddOneOutPhase();
                return;
            }

            if (vowelHousesPanel != null) vowelHousesPanel.SetActive(true);
            if (oddOneOutPanel != null) oddOneOutPanel.SetActive(false);

            currentMiddleIndex = index;
            retryAttempts = 0;
            isTransitioning = false;
            if (momoHintObject != null) momoHintObject.SetActive(false);

            currentItem = activityData.middleItems[index];

            ResetAllVowelTiles();

            if (frontCarriageLetterTMP != null) frontCarriageLetterTMP.text = currentItem.firstLetter;
            if (middleCarriageLetterTMP != null) middleCarriageLetterTMP.text = "_";
            if (backCarriageLetterTMP != null) backCarriageLetterTMP.text = currentItem.lastLetter;

            if (middleCarriagePictureImage != null && currentItem.wordPictureSprite != null)
            {
                middleCarriagePictureImage.sprite = currentItem.wordPictureSprite;
                middleCarriagePictureImage.gameObject.SetActive(true);
            }

            // Fading Support System:
            // Rounds 1-4 (indices 0..3): Auto-play middle-stretched version
            // Rounds 5-8 (indices 4..7): Play normal word, show Help / Stretch button
            // Rounds 9-10 (indices 8..9): Play normal word only, hide Help button
            if (helpStretchAudioButton != null)
            {
                helpStretchAudioButton.gameObject.SetActive(index >= 4 && index <= 7);
            }

            SetDialogue($"Which vowel is hiding in the middle of '{currentItem.fullWord.ToUpper()}'? Look at the five houses!");

            if (index <= 3 && currentItem.middleStretchedWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentItem.middleStretchedWordAudio);
            }
            else if (currentItem.normalWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentItem.normalWordAudio);
            }

            UpdateProgressUI(0.05f + (index / 10f) * 0.65f);
        }

        private void ResetAllVowelTiles()
        {
            for (int i = 0; i < vowelHouseTiles.Length; i++)
            {
                if (vowelHouseTiles[i] != null)
                {
                    vowelHouseTiles[i].ResetToHome(true);
                }
            }
        }

        private void ReplayCurrentAudio()
        {
            if (currentItem == null) return;
            if (currentMiddleIndex <= 3 && currentItem.middleStretchedWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentItem.middleStretchedWordAudio);
            }
            else if (currentItem.normalWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentItem.normalWordAudio);
            }
        }

        private void PlayStretchedAudioHelp()
        {
            if (currentItem != null && currentItem.middleStretchedWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentItem.middleStretchedWordAudio);
                SetDialogue($"Let me stretch it for you — {currentItem.fullWord.ToUpper()}!");
            }
        }

        public void OnVowelHouseDropped(int vowelIndex, MiddleCarriageVowelHouseTile tile = null)
        {
            if (isTransitioning || currentItem == null || vowelIndex >= vowels.Length)
            {
                tile?.ReturnToHomePosition();
                return;
            }

            string selectedVowel = vowels[vowelIndex];
            bool isCorrect = string.Equals(selectedVowel, currentItem.middleVowel, System.StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                tile?.OnDropSuccess();
                StartCoroutine(HandleVowelCorrect(selectedVowel));
            }
            else
            {
                tile?.OnDropFailed();
                StartCoroutine(HandleVowelWrong());
            }
        }

        public void OnVowelHouseSelected(int vowelIndex)
        {
            if (isTransitioning || currentItem == null || vowelIndex >= vowels.Length) return;

            string selectedVowel = vowels[vowelIndex];
            bool isCorrect = string.Equals(selectedVowel, currentItem.middleVowel, System.StringComparison.OrdinalIgnoreCase);
            Button chosenBtn = vowelHouseButtons[vowelIndex];

            if (chosenBtn != null)
            {
                StartCoroutine(FlashButtonFeedback(chosenBtn, isCorrect, 0.6f));
            }

            if (isCorrect)
            {
                StartCoroutine(HandleVowelCorrect(selectedVowel));
            }
            else
            {
                StartCoroutine(HandleVowelWrong());
            }
        }

        private IEnumerator HandleVowelCorrect(string vowel)
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.tileSnapSfx : null);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            if (middleCarriageLetterTMP != null)
            {
                middleCarriageLetterTMP.text = $"<color=#4CAF50><b>{vowel}</b></color>";
                StartCoroutine(PopScaleRoutine(middleCarriageLetterTMP.transform, 1.35f, 0.35f));
            }

            if (middleCarriageGlow != null)
            {
                StartCoroutine(PopScaleRoutine(middleCarriageGlow.transform, 1.25f, 0.35f));
            }

            SetDialogue($"Yes! {currentItem.firstLetter}-{vowel}-{currentItem.lastLetter} — {currentItem.fullWord.ToUpper()}! /{vowel}/ in the middle!");
            if (currentItem.successVoiceClip != null)
            {
                yield return PlayVoiceClip(currentItem.successVoiceClip);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            currentMiddleIndex++;
            isTransitioning = false;
            LoadMiddleRound(currentMiddleIndex);
        }

        private IEnumerator HandleVowelWrong()
        {
            isTransitioning = true;
            retryAttempts++;
            PlaySFX(activityData != null ? activityData.retryGentleSfx : null);

            if (retryAttempts >= 2 && momoHintObject != null)
            {
                momoHintObject.SetActive(true);
                SetDialogue("Try each vowel and see which sounds right!");
                if (activityData != null && activityData.momoStrategyClip != null)
                {
                    yield return PlayVoiceClip(activityData.momoStrategyClip);
                }
            }
            else
            {
                SetDialogue($"Let me stretch it for you — {currentItem.fullWord.ToUpper()}!");
                if (currentItem.middleStretchedWordAudio != null)
                {
                    yield return PlayVoiceClip(currentItem.middleStretchedWordAudio);
                }
                else
                {
                    yield return new WaitForSeconds(1.2f);
                }
            }

            isTransitioning = false;
        }

        #endregion

        #region Phase 2: Odd One Out (3 Rounds)

        private void StartOddOneOutPhase()
        {
            if (vowelHousesPanel != null) vowelHousesPanel.SetActive(false);
            if (oddOneOutPanel != null) oddOneOutPanel.SetActive(true);

            currentOddIndex = 0;
            SetDialogue("Two of them have the same middle sound. Which is the odd one?");
            if (activityData != null && activityData.oddOneOutIntroClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.oddOneOutIntroClip);
            }

            LoadOddRound(0);
        }

        private void LoadOddRound(int index)
        {
            if (activityData == null || activityData.oddOneOutItems == null || index >= activityData.oddOneOutItems.Length)
            {
                CompleteStop3();
                return;
            }

            currentOddIndex = index;
            isTransitioning = false;
            currentOddItem = activityData.oddOneOutItems[index];

            if (oddPromptTMP != null)
            {
                oddPromptTMP.text = $"{string.Join(" … ", currentOddItem.wordChoices)}. Which one is different?";
            }

            for (int i = 0; i < oddChoiceButtons.Length; i++)
            {
                if (oddChoiceButtons[i] != null && i < currentOddItem.wordChoices.Length)
                {
                    oddChoiceButtons[i].gameObject.SetActive(true);
                    if (oddChoiceTexts != null && i < oddChoiceTexts.Length && oddChoiceTexts[i] != null)
                    {
                        oddChoiceTexts[i].text = currentOddItem.wordChoices[i];
                    }
                    if (oddChoiceImages != null && i < oddChoiceImages.Length && oddChoiceImages[i] != null)
                    {
                        if (currentOddItem.wordSprites != null && i < currentOddItem.wordSprites.Length && currentOddItem.wordSprites[i] != null)
                        {
                            oddChoiceImages[i].sprite = currentOddItem.wordSprites[i];
                            oddChoiceImages[i].gameObject.SetActive(true);
                        }
                        else
                        {
                            oddChoiceImages[i].gameObject.SetActive(false);
                        }
                    }
                }
                else if (oddChoiceButtons[i] != null)
                {
                    oddChoiceButtons[i].gameObject.SetActive(false);
                }
            }

            UpdateProgressUI(0.75f + (index / 3f) * 0.22f);
        }

        private void OnOddChoiceSelected(int choiceIndex)
        {
            if (isTransitioning || currentOddItem == null) return;
            StartCoroutine(HandleOddChoiceRoutine(choiceIndex));
        }

        private IEnumerator HandleOddChoiceRoutine(int choiceIndex)
        {
            isTransitioning = true;
            bool isCorrect = (choiceIndex == currentOddItem.oddChoiceIndex);
            Button chosenBtn = (choiceIndex < oddChoiceButtons.Length) ? oddChoiceButtons[choiceIndex] : null;

            if (chosenBtn != null)
            {
                StartCoroutine(FlashButtonFeedback(chosenBtn, isCorrect, 0.6f));
            }

            if (isCorrect)
            {
                PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
                TriggerWiggleStarMeter();
                SetDialogue($"Correct! {currentOddItem.explanationText}");
                yield return new WaitForSeconds(1.2f);
                currentOddIndex++;
                isTransitioning = false;
                LoadOddRound(currentOddIndex);
            }
            else
            {
                PlaySFX(activityData != null ? activityData.retryGentleSfx : null);
                SetDialogue("Listen to the middle sound of each word again!");
                yield return new WaitForSeconds(1.0f);
                isTransitioning = false;
            }
        }

        #endregion

        public void CompleteStop3()
        {
            StartCoroutine(CompleteStop3Sequence());
        }

        private IEnumerator CompleteStop3Sequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.trainWhistleSfx : null);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            UpdateProgressUI(1.0f);

            SetDialogue("The middle sound is the tricky one — and you found it every time!");
            if (activityData != null && activityData.leoClosingClip != null)
            {
                yield return PlayVoiceClip(activityData.leoClosingClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            // Train departs to the left
            if (trainRigRect != null)
            {
                yield return StartCoroutine(SlideTrainOutToLeft(trainDepartureSpeed));
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.gameObject.SetActive(true);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            yield return new WaitForSeconds(1.0f);
            isTransitioning = false;
        }

        private IEnumerator SlideTrainInFromRight(float duration)
        {
            if (trainRigRect == null) yield break;

            Vector2 offscreenRightPos = defaultTrainAnchoredPos + new Vector2(trainTravelDistanceX, 0f);
            trainRigRect.anchoredPosition = offscreenRightPos;

            PlaySFX(activityData != null ? activityData.trainChugSfx : null);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float curveT = entranceEasingCurve != null ? entranceEasingCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);
                Vector2 targetPos = Vector2.Lerp(offscreenRightPos, defaultTrainAnchoredPos, curveT);

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

        private IEnumerator SlideTrainOutToLeft(float duration)
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

                float curveT = departureEasingCurve != null ? departureEasingCurve.Evaluate(t) : (t * t);
                Vector2 targetPos = Vector2.Lerp(startPos, offscreenLeftPos, curveT);

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
