using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;
using EngSnap.Phonics2.Unit2;

namespace EngSnap.Phonics2.Unit7
{
    public class WholeTrainController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit7";
        [SerializeField] private string topicName = "WholeTrain";

        [Header("Data Asset")]
        [SerializeField] private WholeTrainData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Header / Dialogue UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Phase 1: Sound Train Rig UI (All 3 Carriages Active)")]
        [SerializeField] private GameObject wholeTrainPanel;
        [SerializeField] private RectTransform trainRigRect;
        [SerializeField] private Image roofPictureImage;
        [SerializeField] private RectTransform frontCarriageSlot;
        [SerializeField] private RectTransform middleCarriageSlot;
        [SerializeField] private RectTransform backCarriageSlot;
        [SerializeField] private TMP_Text frontCarriageTMP;
        [SerializeField] private TMP_Text middleCarriageTMP;
        [SerializeField] private TMP_Text backCarriageTMP;
        [SerializeField] private GameObject frontCarriageGlow;
        [SerializeField] private GameObject middleCarriageGlow;
        [SerializeField] private GameObject backCarriageGlow;
        [SerializeField] private Button replayAudioButton;

        [Header("Train Motion & Speed Settings")]
        [Tooltip("Speed/Duration in seconds for the train to drive in from right to center.")]
        [Range(0.2f, 4f)] [SerializeField] private float trainEntranceSpeed = 0.85f;

        [Tooltip("Speed/Duration in seconds for the train to drive off to the right upon completion.")]
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

        [Header("Phase 1: Letter Choices UI (Drag & Drop)")]
        [SerializeField] private GameObject letterChoicesPanel;
        [SerializeField] private Button[] tileChoiceButtons = new Button[5];
        [SerializeField] private TMP_Text[] tileChoiceTexts = new TMP_Text[5];
        [SerializeField] private WholeTrainLetterTile[] tileChoiceTiles = new WholeTrainLetterTile[5];

        [Header("Phase 2: Tara Star Round UI")]
        [SerializeField] private GameObject starRoundPanel;
        [SerializeField] private TMP_Text starPromptTMP;
        [SerializeField] private Image starPromptImage;
        [SerializeField] private Button[] starChoiceButtons = new Button[3];
        [SerializeField] private TMP_Text[] starChoiceTexts = new TMP_Text[3];
        [SerializeField] private Button replayStarPromptButton;
        [SerializeField] private Button startStarRoundButton;

        [Header("Finger Tracing Overlay for Challenge 6")]
        [SerializeField] private GameObject starTracingPanel;
        [SerializeField] private LetterTracingComponent letterTracingComponent;
        [SerializeField] private TMP_Text starTracingPromptTMP;

        [Header("Progress & Mascot UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject taraMascotObject;

        [Header("Rewards & Unit 8 Unlock")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;
        [SerializeField] private GameObject badgePopup;
        [SerializeField] private GameObject unit8UnlockPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private readonly Color correctGreenColor = new Color(0.298f, 0.686f, 0.314f, 1f); // #4CAF50
        private readonly Color wrongRedColor = new Color(0.937f, 0.325f, 0.314f, 1f);     // #EF5350

        private int currentTrainIndex = 0;
        private int activeCarriageIndex = 0; // 0 = Front, 1 = Middle, 2 = Back
        private int currentStarChallengeIndex = 0;
        private bool isStarRoundActive = false;
        private bool isTransitioning = false;
        private bool hasSpokenFirstPraise = false;
        private bool hasInitialized = false;

        private WholeTrainWordItem currentTrainWord;
        private TrainStarChallengeItem currentStarChallenge;
        private Vector2 trainStartAnchoredPos;

        public bool IsTransitioning => isTransitioning;

        public RectTransform ActiveCarriageSlotRect
        {
            get
            {
                if (activeCarriageIndex == 0) return frontCarriageSlot != null ? frontCarriageSlot : (frontCarriageTMP != null ? frontCarriageTMP.rectTransform : null);
                if (activeCarriageIndex == 1) return middleCarriageSlot != null ? middleCarriageSlot : (middleCarriageTMP != null ? middleCarriageTMP.rectTransform : null);
                return backCarriageSlot != null ? backCarriageSlot : (backCarriageTMP != null ? backCarriageTMP.rectTransform : null);
            }
        }

        private void Awake()
        {
            EnsureAudioSources();
            EnsureDataAssigned();
            if (trainRigRect != null) trainStartAnchoredPos = trainRigRect.anchoredPosition;

            // Enforce initial panel states immediately on Awake
            EnforcePhase1PanelState();
        }

        private void Start()
        {
            SetupButtonListeners();
            SetupTileComponents();
            hasInitialized = true;
            StartActivity();
        }

        private void OnEnable()
        {
            if (trainRigRect != null) trainRigRect.anchoredPosition = trainStartAnchoredPos;
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

        private void EnforcePhase1PanelState()
        {
            if (wholeTrainPanel != null) wholeTrainPanel.SetActive(true);
            if (starRoundPanel != null) starRoundPanel.SetActive(false);
            if (starTracingPanel != null) starTracingPanel.SetActive(false);
            if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(false);
            if (taraMascotObject != null) taraMascotObject.SetActive(false);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
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
                activityData = Resources.Load<WholeTrainData>("Phonics2/Unit7/WholeTrainData_Unit7");
            }

            bool needsDefaultData = (activityData == null) ||
                                    (activityData.wholeTrainItems == null) ||
                                    (activityData.wholeTrainItems.Length == 0) ||
                                    (activityData.wholeTrainItems[0] == null) ||
                                    string.IsNullOrEmpty(activityData.wholeTrainItems[0].fullWord);

            if (needsDefaultData)
            {
                activityData = ScriptableObject.CreateInstance<WholeTrainData>();
                PopulateDefaultWholeTrainData(activityData);
            }
        }

        private void PopulateDefaultWholeTrainData(WholeTrainData asset)
        {
            asset.wholeTrainItems = new WholeTrainWordItem[8];
            string[] words = new string[] { "cat", "fan", "cup", "net", "bag", "pig", "dog", "hat" };
            string[] firsts = new string[] { "c", "f", "c", "n", "b", "p", "d", "h" };
            string[] vowels = new string[] { "a", "a", "u", "e", "a", "i", "o", "a" };
            string[] lasts = new string[] { "t", "n", "p", "t", "g", "g", "g", "t" };

            string[][] frontOpts = new string[][]
            {
                new string[] { "c", "k", "b", "p" },
                new string[] { "f", "v", "s", "b" },
                new string[] { "c", "k", "t", "d" },
                new string[] { "n", "m", "h", "b" },
                new string[] { "b", "d", "p", "g" },
                new string[] { "p", "b", "t", "d" },
                new string[] { "d", "b", "p", "t" },
                new string[] { "h", "n", "m", "w" }
            };

            string[][] middleOpts = new string[][]
            {
                new string[] { "a", "e", "i", "o", "u" },
                new string[] { "a", "e", "i", "o", "u" },
                new string[] { "a", "e", "i", "o", "u" },
                new string[] { "a", "e", "i", "o", "u" },
                new string[] { "a", "e", "i", "o", "u" },
                new string[] { "a", "e", "i", "o", "u" },
                new string[] { "a", "e", "i", "o", "u" },
                new string[] { "a", "e", "i", "o", "u" }
            };

            string[][] backOpts = new string[][]
            {
                new string[] { "t", "d", "p", "g" },
                new string[] { "n", "m", "t", "d" },
                new string[] { "p", "b", "t", "d" },
                new string[] { "t", "d", "n", "m" },
                new string[] { "g", "k", "d", "t" },
                new string[] { "g", "k", "b", "p" },
                new string[] { "g", "k", "d", "t" },
                new string[] { "t", "d", "p", "b" }
            };

            for (int i = 0; i < 8; i++)
            {
                asset.wholeTrainItems[i] = new WholeTrainWordItem
                {
                    fullWord = words[i],
                    firstLetter = firsts[i],
                    middleLetter = vowels[i],
                    lastLetter = lasts[i],
                    frontOptions = frontOpts[i],
                    middleOptions = middleOpts[i],
                    backOptions = backOpts[i]
                };
            }

            asset.starChallenges = new TrainStarChallengeItem[6];
            asset.starChallenges[0] = new TrainStarChallengeItem
            {
                challengeType = StarChallengeType.FirstSound,
                questionPrompt = "First sound in 'goat'?",
                testWord = "goat",
                choiceTexts = new string[] { "g", "d", "b" },
                correctChoiceIndex = 0,
                correctLetter = "g"
            };
            asset.starChallenges[1] = new TrainStarChallengeItem
            {
                challengeType = StarChallengeType.LastSound,
                questionPrompt = "Last sound in 'crab'?",
                testWord = "crab",
                choiceTexts = new string[] { "b", "p", "d" },
                correctChoiceIndex = 0,
                correctLetter = "b"
            };
            asset.starChallenges[2] = new TrainStarChallengeItem
            {
                challengeType = StarChallengeType.MiddleVowel,
                questionPrompt = "Middle sound in 'lock'?",
                testWord = "lock",
                choiceTexts = new string[] { "o", "u", "a" },
                correctChoiceIndex = 0,
                correctLetter = "o"
            };
            asset.starChallenges[3] = new TrainStarChallengeItem
            {
                challengeType = StarChallengeType.FrontOrBackPosition,
                questionPrompt = "Is /p/ at the front or back of 'cup'?",
                testWord = "cup",
                choiceTexts = new string[] { "Front", "Back" },
                correctChoiceIndex = 1,
                correctLetter = "Back"
            };
            asset.starChallenges[4] = new TrainStarChallengeItem
            {
                challengeType = StarChallengeType.WholeWordFill,
                questionPrompt = "Which word completes the train for 'b - e - d'?",
                testWord = "bed",
                choiceTexts = new string[] { "bed", "bad", "bid" },
                correctChoiceIndex = 0,
                correctLetter = "bed"
            };
            asset.starChallenges[5] = new TrainStarChallengeItem
            {
                challengeType = StarChallengeType.MissingLetterTrace,
                questionPrompt = "Trace the missing letter in 'd _ g'!",
                testWord = "dog",
                choiceTexts = new string[] { "o" },
                correctChoiceIndex = 0,
                correctLetter = "o",
                traceCheckpoints = new Vector2[]
                {
                    new Vector2(0f, 40f),
                    new Vector2(-30f, 0f),
                    new Vector2(0f, -40f),
                    new Vector2(30f, 0f),
                    new Vector2(0f, 40f)
                }
            };
        }

        private void StopAllAudio()
        {
            if (voiceAudioSource != null && voiceAudioSource.isPlaying) voiceAudioSource.Stop();
            if (sfxAudioSource != null && sfxAudioSource.isPlaying) sfxAudioSource.Stop();
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (taraMascotObject != null) taraMascotObject.SetActive(false);
        }

        private void SetupTileComponents()
        {
            for (int i = 0; i < tileChoiceButtons.Length; i++)
            {
                if (tileChoiceButtons[i] != null)
                {
                    WholeTrainLetterTile tile = tileChoiceButtons[i].GetComponent<WholeTrainLetterTile>();
                    if (tile == null)
                    {
                        tile = tileChoiceButtons[i].gameObject.AddComponent<WholeTrainLetterTile>();
                    }
                    if (i < tileChoiceTiles.Length)
                    {
                        tileChoiceTiles[i] = tile;
                    }
                }
            }
        }

        private void SetupButtonListeners()
        {
            if (replayAudioButton != null)
            {
                replayAudioButton.onClick.RemoveAllListeners();
                replayAudioButton.onClick.AddListener(ReplayActiveTrainAudio);
            }

            if (replayStarPromptButton != null)
            {
                replayStarPromptButton.onClick.RemoveAllListeners();
                replayStarPromptButton.onClick.AddListener(ReplayStarPromptAudio);
            }

            if (startStarRoundButton != null)
            {
                startStarRoundButton.onClick.RemoveAllListeners();
                startStarRoundButton.onClick.AddListener(StartStarRoundPhase);
            }

            for (int i = 0; i < tileChoiceButtons.Length; i++)
            {
                int index = i;
                if (tileChoiceButtons[i] != null)
                {
                    tileChoiceButtons[i].onClick.RemoveAllListeners();
                    tileChoiceButtons[i].onClick.AddListener(() => OnTileChoiceClicked(index));
                }
            }

            for (int i = 0; i < starChoiceButtons.Length; i++)
            {
                int index = i;
                if (starChoiceButtons[i] != null)
                {
                    starChoiceButtons[i].onClick.RemoveAllListeners();
                    starChoiceButtons[i].onClick.AddListener(() => OnStarChoiceSelected(index));
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
            StopAllCoroutines();
            currentTrainIndex = 0;
            currentStarChallengeIndex = 0;
            isStarRoundActive = false;
            isTransitioning = false;
            hasSpokenFirstPraise = false;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (badgePopup != null) badgePopup.SetActive(false);
            if (unit8UnlockPopup != null) unit8UnlockPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(false);

            EnforcePhase1PanelState();

            StartCoroutine(StartActivityIntroSequence());
        }

        private IEnumerator StartActivityIntroSequence()
        {
            isTransitioning = true;
            EnforcePhase1PanelState();
            if (letterChoicesPanel != null) letterChoicesPanel.SetActive(false);

            SetDialogue("All three carriages are open! Can you fill the whole train?");
            PlaySFX(activityData != null ? activityData.trainWhistleSfx : null);

            if (activityData != null && activityData.leoIntroClip != null)
            {
                yield return PlayVoiceClip(activityData.leoIntroClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            yield return new WaitForSeconds(0.2f);

            isTransitioning = false;
            LoadTrainRound(0);
        }

        #region Phase 1: Whole Train CVC Rounds (8 Rounds)

        private void LoadTrainRound(int index)
        {
            if (activityData == null || activityData.wholeTrainItems == null || index >= activityData.wholeTrainItems.Length)
            {
                CompleteWholeTrainPhase();
                return;
            }

            StartCoroutine(LoadTrainRoundRoutine(index));
        }

        private IEnumerator LoadTrainRoundRoutine(int index)
        {
            isTransitioning = true;
            currentTrainIndex = index;
            activeCarriageIndex = 0; // Start at Front carriage

            if (activityData == null || activityData.wholeTrainItems == null || index >= activityData.wholeTrainItems.Length)
            {
                CompleteWholeTrainPhase();
                yield break;
            }

            currentTrainWord = activityData.wholeTrainItems[index];
            if (currentTrainWord == null)
            {
                CompleteWholeTrainPhase();
                yield break;
            }

            // Ensure Whole Train panel is visible, Star Round is strictly disabled
            EnforcePhase1PanelState();
            if (letterChoicesPanel != null) letterChoicesPanel.SetActive(false);

            if (frontCarriageTMP != null) frontCarriageTMP.text = "_";
            if (middleCarriageTMP != null) middleCarriageTMP.text = "_";
            if (backCarriageTMP != null) backCarriageTMP.text = "_";

            if (roofPictureImage != null)
            {
                if (currentTrainWord.wordPictureSprite != null)
                {
                    roofPictureImage.sprite = currentTrainWord.wordPictureSprite;
                    roofPictureImage.gameObject.SetActive(true);
                }
                else
                {
                    roofPictureImage.gameObject.SetActive(false);
                }
            }

            UpdateCarriageGlows();

            // Train drives in from the right to center at the start of every round
            if (trainRigRect != null)
            {
                yield return StartCoroutine(SlideTrainInFromRight(trainEntranceSpeed));
            }

            if (letterChoicesPanel != null) letterChoicesPanel.SetActive(true);
            UpdateCarriageChoiceTiles();

            string displayWord = !string.IsNullOrEmpty(currentTrainWord.fullWord) ? currentTrainWord.fullWord.ToUpper() : "";
            SetDialogue($"Listen: {displayWord} … {currentTrainWord.firstLetter} - {currentTrainWord.middleLetter} - {currentTrainWord.lastLetter}. Front, middle, back!");

            if (currentTrainWord.segmentedWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentTrainWord.segmentedWordAudio);
            }
            else if (currentTrainWord.normalWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentTrainWord.normalWordAudio);
            }

            UpdateProgressUI((index / 8f) * 0.5f);
            isTransitioning = false;
        }

        private void UpdateCarriageGlows()
        {
            if (frontCarriageGlow != null) frontCarriageGlow.SetActive(activeCarriageIndex == 0);
            if (middleCarriageGlow != null) middleCarriageGlow.SetActive(activeCarriageIndex == 1);
            if (backCarriageGlow != null) backCarriageGlow.SetActive(activeCarriageIndex == 2);
        }

        private void UpdateCarriageChoiceTiles()
        {
            if (currentTrainWord == null) return;
            string[] options = (activeCarriageIndex == 0)
                ? currentTrainWord.frontOptions
                : (activeCarriageIndex == 1)
                ? currentTrainWord.middleOptions
                : currentTrainWord.backOptions;

            if (options == null || options.Length == 0)
            {
                if (activeCarriageIndex == 0) options = new string[] { currentTrainWord.firstLetter, "b", "p", "d" };
                else if (activeCarriageIndex == 1) options = new string[] { "a", "e", "i", "o", "u" };
                else options = new string[] { currentTrainWord.lastLetter, "t", "d", "g" };
            }

            for (int i = 0; i < tileChoiceButtons.Length; i++)
            {
                if (tileChoiceButtons[i] != null && i < options.Length)
                {
                    tileChoiceButtons[i].gameObject.SetActive(true);
                    if (tileChoiceTexts != null && i < tileChoiceTexts.Length && tileChoiceTexts[i] != null)
                    {
                        tileChoiceTexts[i].text = options[i];
                    }

                    if (tileChoiceTiles != null && i < tileChoiceTiles.Length && tileChoiceTiles[i] != null)
                    {
                        tileChoiceTiles[i].Setup(i, options[i], this);
                    }
                }
                else if (tileChoiceButtons[i] != null)
                {
                    tileChoiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void ReplayActiveTrainAudio()
        {
            if (currentTrainWord == null) return;
            if (currentTrainWord.segmentedWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentTrainWord.segmentedWordAudio);
            }
            else if (currentTrainWord.normalWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentTrainWord.normalWordAudio);
            }
        }

        public void OnTileDropped(int choiceIndex, WholeTrainLetterTile tile = null)
        {
            if (isTransitioning || currentTrainWord == null)
            {
                tile?.ReturnToHomePosition();
                return;
            }

            string[] options = (activeCarriageIndex == 0)
                ? currentTrainWord.frontOptions
                : (activeCarriageIndex == 1)
                ? currentTrainWord.middleOptions
                : currentTrainWord.backOptions;

            if (options == null || choiceIndex >= options.Length)
            {
                tile?.ReturnToHomePosition();
                return;
            }

            string selected = options[choiceIndex];
            string targetLetter = (activeCarriageIndex == 0)
                ? currentTrainWord.firstLetter
                : (activeCarriageIndex == 1)
                ? currentTrainWord.middleLetter
                : currentTrainWord.lastLetter;

            bool isCorrect = string.Equals(selected, targetLetter, System.StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                tile?.OnDropSuccess();
                StartCoroutine(HandleCarriageTileCorrect(selected));
            }
            else
            {
                tile?.OnDropFailed();
                StartCoroutine(HandleCarriageTileWrong(targetLetter));
            }
        }

        public void OnTileChoiceClicked(int choiceIndex)
        {
            if (isTransitioning || currentTrainWord == null) return;

            string[] options = (activeCarriageIndex == 0)
                ? currentTrainWord.frontOptions
                : (activeCarriageIndex == 1)
                ? currentTrainWord.middleOptions
                : currentTrainWord.backOptions;

            if (options == null || choiceIndex >= options.Length) return;

            string selected = options[choiceIndex];
            string targetLetter = (activeCarriageIndex == 0)
                ? currentTrainWord.firstLetter
                : (activeCarriageIndex == 1)
                ? currentTrainWord.middleLetter
                : currentTrainWord.lastLetter;

            bool isCorrect = string.Equals(selected, targetLetter, System.StringComparison.OrdinalIgnoreCase);
            Button chosenBtn = tileChoiceButtons[choiceIndex];

            if (chosenBtn != null)
            {
                StartCoroutine(FlashButtonFeedback(chosenBtn, isCorrect, 0.5f));
            }

            if (isCorrect)
            {
                if (tileChoiceTiles != null && choiceIndex < tileChoiceTiles.Length && tileChoiceTiles[choiceIndex] != null)
                {
                    tileChoiceTiles[choiceIndex].OnDropSuccess();
                }
                StartCoroutine(HandleCarriageTileCorrect(selected));
            }
            else
            {
                StartCoroutine(HandleCarriageTileWrong(targetLetter));
            }
        }

        private IEnumerator HandleCarriageTileCorrect(string letter)
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.tileSnapSfx : null);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            if (activeCarriageIndex == 0 && frontCarriageTMP != null)
            {
                frontCarriageTMP.text = $"<color=#4CAF50><b>{letter}</b></color>";
                StartCoroutine(PopScaleRoutine(frontCarriageTMP.transform, 1.3f, 0.3f));
            }
            else if (activeCarriageIndex == 1 && middleCarriageTMP != null)
            {
                middleCarriageTMP.text = $"<color=#4CAF50><b>{letter}</b></color>";
                StartCoroutine(PopScaleRoutine(middleCarriageTMP.transform, 1.3f, 0.3f));
            }
            else if (activeCarriageIndex == 2 && backCarriageTMP != null)
            {
                backCarriageTMP.text = $"<color=#4CAF50><b>{letter}</b></color>";
                StartCoroutine(PopScaleRoutine(backCarriageTMP.transform, 1.3f, 0.3f));
            }

            activeCarriageIndex++;

            if (activeCarriageIndex < 3)
            {
                // Advance to next carriage
                yield return new WaitForSeconds(0.25f);
                UpdateCarriageGlows();
                UpdateCarriageChoiceTiles();
                isTransitioning = false;
            }
            else
            {
                // Full word completed! Animate train whistle and depart offscreen to right!
                yield return StartCoroutine(PlayTrainDepartSequence());
                currentTrainIndex++;
                isTransitioning = false;
                LoadTrainRound(currentTrainIndex);
            }
        }

        private IEnumerator HandleCarriageTileWrong(string targetLetter)
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.retryGentleSfx : null);

            string posName = (activeCarriageIndex == 0) ? "front" : (activeCarriageIndex == 1) ? "middle" : "back";
            SetDialogue($"Nearly! Listen to the {posName} sound — /{targetLetter}/!");
            yield return new WaitForSeconds(1.0f);

            isTransitioning = false;
        }

        private IEnumerator PlayTrainDepartSequence()
        {
            if (letterChoicesPanel != null) letterChoicesPanel.SetActive(false);

            PlaySFX(activityData != null ? activityData.trainWhistleSfx : null);
            SetDialogue($"{currentTrainWord.firstLetter}… {currentTrainWord.middleLetter}… {currentTrainWord.lastLetter}… {currentTrainWord.fullWord.ToUpper()}! The train is full — off it goes!");

            if (currentTrainWord.departVoiceClip != null)
            {
                yield return PlayVoiceClip(currentTrainWord.departVoiceClip);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            // Praise on first completed word
            if (!hasSpokenFirstPraise)
            {
                hasSpokenFirstPraise = true;
                SetDialogue("Do you know what you just did? You SPELLED a word!");
                if (activityData != null && activityData.spelledWordPraiseClip != null)
                {
                    yield return PlayVoiceClip(activityData.spelledWordPraiseClip);
                }
            }

            // Train drives off screen to the right after every completed word
            if (trainRigRect != null)
            {
                yield return StartCoroutine(SlideTrainOutToRight(trainDepartureSpeed));
            }

            yield return new WaitForSeconds(0.2f);
        }

        private void CompleteWholeTrainPhase()
        {
            isTransitioning = true;
            if (letterChoicesPanel != null) letterChoicesPanel.SetActive(false);

            if (startStarRoundButton != null)
            {
                startStarRoundButton.gameObject.SetActive(true);
                SetDialogue("All 8 trains filled! Tap Start to take on Tara's Star Round!");
            }
            else
            {
                StartCoroutine(TransitionToStarRoundDelay(1.5f));
            }
        }

        private IEnumerator TransitionToStarRoundDelay(float delay)
        {
            SetDialogue("All 8 trains filled! Get ready for Tara's Star Round!");
            yield return new WaitForSeconds(delay);
            StartStarRoundPhase();
        }

        #endregion

        #region Phase 2: Tara Star Round (6 Challenges)

        public void StartStarRoundPhase()
        {
            isStarRoundActive = true;
            if (wholeTrainPanel != null) wholeTrainPanel.SetActive(false);
            if (starRoundPanel != null) starRoundPanel.SetActive(true);
            if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(false);

            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (taraMascotObject != null) taraMascotObject.SetActive(true);

            currentStarChallengeIndex = 0;

            SetDialogue("My turn! Six quick challenges. Ready? Roar!");
            if (activityData != null && activityData.taraStarRoundOpenClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.taraStarRoundOpenClip);
            }

            LoadStarChallenge(0);
        }

        private void LoadStarChallenge(int index)
        {
            if (activityData == null || activityData.starChallenges == null || index >= activityData.starChallenges.Length)
            {
                CompleteStop4();
                return;
            }

            currentStarChallengeIndex = index;
            isTransitioning = false;
            currentStarChallenge = activityData.starChallenges[index];

            if (currentStarChallenge.challengeType == StarChallengeType.MissingLetterTrace)
            {
                StartCoroutine(RunStarTracingChallenge());
                return;
            }

            if (starPromptTMP != null) starPromptTMP.text = currentStarChallenge.questionPrompt;
            if (starPromptImage != null && currentStarChallenge.wordSprite != null)
            {
                starPromptImage.sprite = currentStarChallenge.wordSprite;
                starPromptImage.gameObject.SetActive(true);
            }
            else if (starPromptImage != null)
            {
                starPromptImage.gameObject.SetActive(false);
            }

            for (int i = 0; i < starChoiceButtons.Length; i++)
            {
                if (starChoiceButtons[i] != null && i < currentStarChallenge.choiceTexts.Length)
                {
                    starChoiceButtons[i].gameObject.SetActive(true);
                    if (starChoiceTexts != null && i < starChoiceTexts.Length && starChoiceTexts[i] != null)
                    {
                        starChoiceTexts[i].text = currentStarChallenge.choiceTexts[i];
                    }
                }
                else if (starChoiceButtons[i] != null)
                {
                    starChoiceButtons[i].gameObject.SetActive(false);
                }
            }

            SetDialogue(currentStarChallenge.questionPrompt);
            if (currentStarChallenge.promptClip != null)
            {
                PlayVoiceClipNonBlocking(currentStarChallenge.promptClip);
            }

            UpdateProgressUI(0.5f + (index / 6f) * 0.5f);
        }

        private void ReplayStarPromptAudio()
        {
            if (currentStarChallenge != null && currentStarChallenge.promptClip != null)
            {
                PlayVoiceClipNonBlocking(currentStarChallenge.promptClip);
            }
        }

        private void OnStarChoiceSelected(int choiceIndex)
        {
            if (isTransitioning || currentStarChallenge == null) return;

            bool isCorrect = (choiceIndex == currentStarChallenge.correctChoiceIndex);
            Button chosenBtn = (choiceIndex < starChoiceButtons.Length) ? starChoiceButtons[choiceIndex] : null;

            if (chosenBtn != null)
            {
                StartCoroutine(FlashButtonFeedback(chosenBtn, isCorrect, 0.6f));
            }

            if (isCorrect)
            {
                StartCoroutine(HandleStarChoiceCorrect());
            }
            else
            {
                StartCoroutine(HandleStarChoiceWrong());
            }
        }

        private IEnumerator HandleStarChoiceCorrect()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            SetDialogue("Roar! Spot on!");
            yield return new WaitForSeconds(0.8f);

            currentStarChallengeIndex++;
            isTransitioning = false;
            LoadStarChallenge(currentStarChallengeIndex);
        }

        private IEnumerator HandleStarChoiceWrong()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.retryGentleSfx : null);

            SetDialogue("Give it another try! Listen carefully to Tara!");
            if (currentStarChallenge != null && currentStarChallenge.promptClip != null)
            {
                yield return PlayVoiceClip(currentStarChallenge.promptClip);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            isTransitioning = false;
        }

        private IEnumerator RunStarTracingChallenge()
        {
            if (starTracingPanel != null) starTracingPanel.SetActive(true);

            SetDialogue("Trace the missing middle letter in 'd _ g'!");
            if (currentStarChallenge.promptClip != null)
            {
                PlayVoiceClipNonBlocking(currentStarChallenge.promptClip);
            }

            bool tracingDone = false;
            if (letterTracingComponent != null)
            {
                letterTracingComponent.SetupTracing(
                    'o',
                    null,
                    null,
                    () => { tracingDone = true; },
                    null,
                    currentStarChallenge.traceCheckpoints
                );
            }
            else
            {
                tracingDone = true;
            }

            while (!tracingDone)
            {
                yield return null;
            }

            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            SetDialogue("Roar! You traced 'o' for DOG!");
            yield return new WaitForSeconds(0.8f);

            if (starTracingPanel != null) starTracingPanel.SetActive(false);

            currentStarChallengeIndex++;
            CompleteStop4();
        }

        #endregion

        public void CompleteStop4()
        {
            StartCoroutine(CompleteStop4Sequence());
        }

        private IEnumerator CompleteStop4Sequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.trainWhistleSfx : null);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            UpdateProgressUI(1.0f);

            SetDialogue("Front, middle and back. You are a SOUND TRAIN DRIVER!");
            if (activityData != null && activityData.badgeVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.badgeVoiceClip);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (badgePopup != null) badgePopup.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);

            yield return new WaitForSeconds(1.5f);

            SetDialogue("Unit Eight is open — and this is the big one. Next time, you are going to READ whole words all by yourself!");
            if (activityData != null && activityData.unit8UnlockClip != null)
            {
                yield return PlayVoiceClip(activityData.unit8UnlockClip);
            }

            if (unit8UnlockPopup != null) unit8UnlockPopup.SetActive(true);
            if (continueButton != null) continueButton.gameObject.SetActive(true);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            yield return new WaitForSeconds(1.0f);
            isTransitioning = false;
        }

        private IEnumerator SlideTrainInFromRight(float duration)
        {
            if (trainRigRect == null) yield break;

            Vector2 offscreenRightPos = trainStartAnchoredPos + new Vector2(trainTravelDistanceX, 0f);
            trainRigRect.anchoredPosition = offscreenRightPos;

            PlaySFX(activityData != null ? activityData.trainChugSfx : null);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float curveT = entranceEasingCurve != null ? entranceEasingCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);
                Vector2 targetPos = Vector2.Lerp(offscreenRightPos, trainStartAnchoredPos, curveT);

                if (enableWheelBumping)
                {
                    float bump = Mathf.Sin(elapsed * wheelBumpFrequency) * wheelBumpIntensity * (1f - t);
                    targetPos.y += bump;
                }

                trainRigRect.anchoredPosition = targetPos;
                yield return null;
            }
            trainRigRect.anchoredPosition = trainStartAnchoredPos;
        }

        private IEnumerator SlideTrainOutToRight(float duration)
        {
            if (trainRigRect == null) yield break;

            PlaySFX(activityData != null ? activityData.trainChugSfx : null);

            Vector2 offscreenRightPos = trainStartAnchoredPos + new Vector2(trainTravelDistanceX, 0f);
            Vector2 startPos = trainRigRect.anchoredPosition;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float curveT = departureEasingCurve != null ? departureEasingCurve.Evaluate(t) : (t * t);
                Vector2 targetPos = Vector2.Lerp(startPos, offscreenRightPos, curveT);

                if (enableWheelBumping)
                {
                    float bump = Mathf.Sin(elapsed * wheelBumpFrequency) * wheelBumpIntensity * t;
                    targetPos.y += bump;
                }

                trainRigRect.anchoredPosition = targetPos;
                yield return null;
            }
            trainRigRect.anchoredPosition = offscreenRightPos;
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
            if (badgePopup != null) badgePopup.SetActive(false);
            if (unit8UnlockPopup != null) unit8UnlockPopup.SetActive(false);
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
