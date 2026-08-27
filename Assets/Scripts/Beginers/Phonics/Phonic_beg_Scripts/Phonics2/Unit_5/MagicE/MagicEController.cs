using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit5
{
    public class MagicEController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit5";
        [SerializeField] private string topicName = "MagicE";

        [Header("Data Asset")]
        [SerializeField] private MagicEData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Header / Dialogue UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Phase 1: Wand Transformation Workshop UI")]
        [SerializeField] private GameObject transformPanel;
        [SerializeField] private TMP_Text transformWordText;
        [SerializeField] private Image transformWordImage;
        [SerializeField] private MagicEWand wandScript;
        [SerializeField] private RectTransform silentELandingTarget;
        [SerializeField] private RectTransform vowelPositionTarget;
        [SerializeField] private GameObject emptySlotObject;
        [SerializeField] private GameObject emptySlotGlow;
        [SerializeField] private GameObject silentEGraphic;
        [SerializeField] private TMP_Text silentEText;
        [SerializeField] private GameObject backwardsSparkleTrail;
        [SerializeField] private RectTransform sparkleTrailRect;
        [SerializeField] private GameObject vowelStandUpGlow;
        [SerializeField] private TMP_Text vowelStandingText;
        [SerializeField] private Button transformTapWandPromptButton;

        [Header("Phase 2: Which One? Choice UI")]
        [SerializeField] private GameObject whichOnePanel;
        [SerializeField] private TMP_Text whichOnePromptText;
        [SerializeField] private Button choiceButtonA;
        [SerializeField] private TMP_Text choiceTextA;
        [SerializeField] private Image choiceImageA;
        [SerializeField] private Button choiceButtonB;
        [SerializeField] private TMP_Text choiceTextB;
        [SerializeField] private Image choiceImageB;
        [SerializeField] private Button replayWhichOneAudioButton;

        [Header("Phase 3: Word Wall UI")]
        [SerializeField] private GameObject wordWallPanel;
        [SerializeField] private Button[] wordWallButtons;
        [SerializeField] private TMP_Text[] wordWallTexts;
        [SerializeField] private Image[] wordWallImages;
        [SerializeField] private MagicEWordCard[] wordWallCards;
        [SerializeField] private Button finishWordWallButton;
        [SerializeField] private Button startWordWallButton;

        [Header("Progress & Mascot UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Image starMeterFillImage;
        [SerializeField] private TMP_Text starCountText;
        [SerializeField] private RectTransform starMeterRect;
        [SerializeField] private GameObject momoMascotObject;
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject momoHintObject;

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

        [Tooltip("The current panel to hide when Continue is clicked. (Assign this GameObject or its parent panel)")]
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        // Internal State
        private int currentPhase = 1; // 1 = Transform (8 rounds), 2 = Which One (6 rounds), 3 = Word Wall, 4 = Rewards
        private int currentTransformIndex = 0;
        private int currentWhichOneIndex = 0;
        private int totalStarsEarned = 0;
        private bool isTransitioning = false;
        private bool isActivityCompleted = false;
        private HashSet<int> exploredWordWallIndices = new HashSet<int>();

        public string UnitID => unitID;
        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
            EnsureDataAssigned();
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
            sfxAudioSource.loop = false;

            if (voiceAudioSource == null)
            {
                voiceAudioSource = gameObject.AddComponent<AudioSource>();
            }
            voiceAudioSource.spatialBlend = 0f;
            voiceAudioSource.volume = 1f;
            voiceAudioSource.loop = false;
        }

        private void EnsureDataAssigned()
        {
            if (activityData == null)
            {
                activityData = Resources.Load<MagicEData>("Phonics2/Unit5/MagicEData_Unit5");
            }

            if (activityData == null)
            {
                // Fallback runtime initialization
                activityData = ScriptableObject.CreateInstance<MagicEData>();

                activityData.transformPairs = new MagicETransformPair[8];
                string[] shorts = new string[] { "cap", "kit", "tub", "mad", "pin", "cub", "tap", "hop" };
                string[] longs = new string[] { "cape", "kite", "tube", "made", "pine", "cube", "tape", "hope" };
                int[] vIndices = new int[] { 1, 1, 1, 1, 1, 1, 1, 1 };

                for (int i = 0; i < 8; i++)
                {
                    activityData.transformPairs[i] = new MagicETransformPair
                    {
                        shortWord = shorts[i],
                        longWord = longs[i],
                        vowelCharIndex = vIndices[i]
                    };
                }

                activityData.whichOneChoices = new MagicEWhichOneChoice[6];
                activityData.whichOneChoices[0] = new MagicEWhichOneChoice { wordA = "cap", wordB = "cape", correctIndex = 1 };
                activityData.whichOneChoices[1] = new MagicEWhichOneChoice { wordA = "pin", wordB = "pine", correctIndex = 1 };
                activityData.whichOneChoices[2] = new MagicEWhichOneChoice { wordA = "tub", wordB = "tube", correctIndex = 1 };
                activityData.whichOneChoices[3] = new MagicEWhichOneChoice { wordA = "kit", wordB = "kite", correctIndex = 1 };
                activityData.whichOneChoices[4] = new MagicEWhichOneChoice { wordA = "mad", wordB = "made", correctIndex = 1 };
                activityData.whichOneChoices[5] = new MagicEWhichOneChoice { wordA = "hop", wordB = "hope", correctIndex = 1 };

                activityData.magicEWordWallList = new string[]
                {
                    "cake", "take", "bake", "make", "game", "same", "fame",
                    "tape", "safe", "case", "vase", "bike", "like", "hike",
                    "line", "mine", "dime", "lime", "side", "hide", "ride",
                    "tube", "cube", "June", "rule", "tune"
                };
            }
        }

        private void Start()
        {
            EnsureAudioSources();
            EnsureDataAssigned();
            SetupButtonListeners();
            ResetActivity();
        }

        private void OnEnable()
        {
            EnsureAudioSources();
            EnsureDataAssigned();
            SetupButtonListeners();
            ResetActivity();
            StartCoroutine(StartIntroSequence());
        }

        private void OnDisable()
        {
            DeactivateMascots();
        }

        public void DeactivateMascots()
        {
            if (momoMascotObject != null) momoMascotObject.SetActive(false);
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
        }

        private void SetupButtonListeners()
        {
            if (choiceButtonA != null)
            {
                choiceButtonA.onClick.RemoveAllListeners();
                choiceButtonA.onClick.AddListener(() => OnWhichOneChoiceSelected(0));
            }

            if (choiceButtonB != null)
            {
                choiceButtonB.onClick.RemoveAllListeners();
                choiceButtonB.onClick.AddListener(() => OnWhichOneChoiceSelected(1));
            }

            if (replayWhichOneAudioButton != null)
            {
                replayWhichOneAudioButton.onClick.RemoveAllListeners();
                replayWhichOneAudioButton.onClick.AddListener(ReplayWhichOneAudio);
            }

            if (transformTapWandPromptButton != null)
            {
                transformTapWandPromptButton.onClick.RemoveAllListeners();
                transformTapWandPromptButton.onClick.AddListener(CastMagicEWand);
            }

            if (startWordWallButton != null)
            {
                startWordWallButton.onClick.RemoveAllListeners();
                startWordWallButton.onClick.AddListener(StartWordWallPhase);
            }

            if (finishWordWallButton != null)
            {
                finishWordWallButton.onClick.RemoveAllListeners();
                finishWordWallButton.onClick.AddListener(CompleteStop2);
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

            if (wordWallButtons != null)
            {
                for (int i = 0; i < wordWallButtons.Length; i++)
                {
                    int index = i;
                    if (wordWallButtons[i] != null)
                    {
                        wordWallButtons[i].onClick.RemoveAllListeners();
                        wordWallButtons[i].onClick.AddListener(() => OnWordWallCardClicked(index));
                    }
                }
            }
        }

        public void ResetActivity()
        {
            EnsureAudioSources();
            EnsureDataAssigned();
            StopAllCoroutines();

            currentPhase = 1;
            currentTransformIndex = 0;
            currentWhichOneIndex = 0;
            totalStarsEarned = 0;
            isTransitioning = false;
            isActivityCompleted = false;
            exploredWordWallIndices.Clear();

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (silentEGraphic != null) silentEGraphic.SetActive(false);
            if (emptySlotObject != null) emptySlotObject.SetActive(true);
            if (emptySlotGlow != null) emptySlotGlow.SetActive(false);
            if (backwardsSparkleTrail != null) backwardsSparkleTrail.SetActive(false);
            if (vowelStandUpGlow != null) vowelStandUpGlow.SetActive(false);

            if (wandScript != null)
            {
                wandScript.SetupWand(this);
            }

            UpdateProgressUI(0f);
            UpdateStarMeterUI(0, 8);
        }

        private IEnumerator StartIntroSequence()
        {
            isTransitioning = true;
            if (momoMascotObject != null) momoMascotObject.SetActive(true);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);

            ShowTransformPhaseUI();

            // Momo Opening Narration
            SetDialogue("Momo: I have a magic wand — and it is shaped like an e! Watch what it does.");
            if (activityData != null && activityData.momoIntroClip != null)
            {
                yield return PlayVoiceClip(activityData.momoIntroClip);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            isTransitioning = false;
            LoadTransformRound(0);
        }

        private void ShowTransformPhaseUI()
        {
            currentPhase = 1;
            if (transformPanel != null) transformPanel.SetActive(true);
            if (whichOnePanel != null) whichOnePanel.SetActive(false);
            if (wordWallPanel != null) wordWallPanel.SetActive(false);
        }

        private void LoadTransformRound(int index)
        {
            if (activityData == null || activityData.transformPairs == null || index >= activityData.transformPairs.Length)
            {
                StartWhichOnePhase();
                return;
            }

            currentTransformIndex = index;
            MagicETransformPair pair = activityData.transformPairs[index];

            if (transformWordText != null)
            {
                transformWordText.text = pair.shortWord;
                transformWordText.transform.localScale = Vector3.one;
            }

            if (transformWordImage != null)
            {
                if (pair.shortWordSprite != null)
                {
                    transformWordImage.sprite = pair.shortWordSprite;
                    transformWordImage.gameObject.SetActive(true);
                }
                else
                {
                    transformWordImage.gameObject.SetActive(false);
                }
                transformWordImage.transform.localScale = Vector3.one;
            }

            if (emptySlotObject != null) emptySlotObject.SetActive(true);
            if (emptySlotGlow != null) emptySlotGlow.SetActive(false);
            if (silentEGraphic != null) silentEGraphic.SetActive(false);
            if (backwardsSparkleTrail != null) backwardsSparkleTrail.SetActive(false);
            if (vowelStandUpGlow != null) vowelStandUpGlow.SetActive(false);

            if (index == 0)
            {
                // Round 1 Teacher demonstration
                SetDialogue("Leo: This word says 'cap'. Drag the magic e wand to the empty slot!");
                if (activityData != null && activityData.leoSetupClip != null)
                {
                    PlayVoiceClipNonBlocking(activityData.leoSetupClip);
                }
            }
            else
            {
                SetDialogue($"Leo: This word says '{pair.shortWord.ToUpper()}'. Drag the magic e wand to the end!");
                if (activityData != null && activityData.leoTapWandClip != null)
                {
                    PlayVoiceClipNonBlocking(activityData.leoTapWandClip);
                }
                else if (pair.shortWordClip != null)
                {
                    PlayVoiceClipNonBlocking(pair.shortWordClip);
                }
            }

            float progress = (index / 8f) * 0.45f;
            UpdateProgressUI(progress);
            UpdateStarMeterUI(index, 8);
        }

        #region Wand Drag and Drop Evaluation

        /// <summary>
        /// Called continuously while the child drags Momo's wand.
        /// Activates empty slot glow when wand approaches target landing area.
        /// </summary>
        public void OnWandDragUpdate(Vector3 wandWorldPos, float snapDist)
        {
            if (isTransitioning || currentPhase != 1) return;

            Vector3 targetPos = GetLandingTargetPosition();
            float dist = Vector3.Distance(wandWorldPos, targetPos);

            bool isNear = dist <= snapDist;
            if (emptySlotGlow != null && emptySlotGlow.activeSelf != isNear)
            {
                emptySlotGlow.SetActive(isNear);
            }
        }

        /// <summary>
        /// Evaluates whether the wand was dropped close enough to the empty target slot.
        /// </summary>
        public bool EvaluateWandDrop(Vector3 wandWorldPos, float snapDist)
        {
            if (isTransitioning || currentPhase != 1 || activityData == null || currentTransformIndex >= activityData.transformPairs.Length)
            {
                if (emptySlotGlow != null) emptySlotGlow.SetActive(false);
                return false;
            }

            Vector3 targetPos = GetLandingTargetPosition();
            float dist = Vector3.Distance(wandWorldPos, targetPos);

            if (dist <= snapDist)
            {
                if (emptySlotGlow != null) emptySlotGlow.SetActive(false);
                MagicETransformPair pair = activityData.transformPairs[currentTransformIndex];
                StartCoroutine(ExecuteWandSlideInSequence(pair, wandWorldPos, targetPos));
                return true;
            }

            if (emptySlotGlow != null) emptySlotGlow.SetActive(false);
            return false;
        }

        private Vector3 GetLandingTargetPosition()
        {
            if (silentELandingTarget != null)
            {
                return silentELandingTarget.position;
            }
            if (emptySlotObject != null)
            {
                return emptySlotObject.transform.position;
            }
            if (transformWordText != null)
            {
                return transformWordText.transform.position + new Vector3(80f, 0, 0);
            }
            return transform.position;
        }

        public void CastMagicEWand()
        {
            if (isTransitioning || currentPhase != 1 || activityData == null || currentTransformIndex >= activityData.transformPairs.Length)
            {
                return;
            }

            MagicETransformPair pair = activityData.transformPairs[currentTransformIndex];
            Vector3 targetPos = GetLandingTargetPosition();
            Vector3 startPos = wandScript != null ? wandScript.transform.position : transform.position;
            StartCoroutine(ExecuteWandSlideInSequence(pair, startPos, targetPos));
        }

        /// <summary>
        /// When wand hits the empty slot: 'e' slides in, backwards sparkle travels to vowel,
        /// vowel stands up and says its name, word changes (cap -> cape), and audio plays!
        /// </summary>
        private IEnumerator ExecuteWandSlideInSequence(MagicETransformPair pair, Vector3 fromPos, Vector3 targetLandingPos)
        {
            isTransitioning = true;

            // 1. Play Sparkle SFX
            PlaySFX(activityData.wandSparkleSfx);

            // Hide empty dashed outline slot as 'e' arrives
            if (emptySlotObject != null) emptySlotObject.SetActive(false);
            if (emptySlotGlow != null) emptySlotGlow.SetActive(false);

            // 2. 'e' SLIDES IN smoothly into the target landing position
            if (silentEGraphic != null)
            {
                silentEGraphic.SetActive(true);
                Vector3 slideStartPos = fromPos;
                float slideElapsed = 0f;
                float slideDuration = 0.28f;

                while (slideElapsed < slideDuration)
                {
                    slideElapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(slideElapsed / slideDuration);
                    float smoothT = Mathf.SmoothStep(0f, 1f, t);
                    silentEGraphic.transform.position = Vector3.Lerp(slideStartPos, targetLandingPos, smoothT);
                    yield return null;
                }

                silentEGraphic.transform.position = targetLandingPos;
                StartCoroutine(PopScaleRoutine(silentEGraphic.transform, 1.35f, 0.22f));
            }

            // 3. Silent E Lands Silently ("shhh" gesture, no sound of its own)
            PlaySFX(activityData.shhhSilentSfx);

            // Show visual word with gold silent e
            if (transformWordText != null)
            {
                transformWordText.text = $"{pair.shortWord}<color=#FFD54F><b>e</b></color>";
            }

            yield return new WaitForSeconds(0.25f);

            // 4. Sparkle travels BACKWARDS from the 'e' to the root vowel
            if (backwardsSparkleTrail != null)
            {
                backwardsSparkleTrail.SetActive(true);
                Vector3 vowelPos = transformWordText != null ? transformWordText.transform.position : targetLandingPos - new Vector3(80f, 0, 0);
                if (vowelPositionTarget != null) vowelPos = vowelPositionTarget.position;

                yield return StartCoroutine(AnimateBackwardsSparkle(targetLandingPos, vowelPos, 0.42f));
            }
            else
            {
                yield return new WaitForSeconds(0.35f);
            }

            // 5. The Vowel stands up and says its NAME
            PlaySFX(activityData.vowelStandUpSfx != null ? activityData.vowelStandUpSfx : activityData.wandSparkleSfx);
            if (vowelStandUpGlow != null) vowelStandUpGlow.SetActive(true);

            // Vowel stand-up animation: Rich formatted word with colored vowel + gold silent e, big scale pop
            if (transformWordText != null)
            {
                transformWordText.text = MagicEData.FormatMagicEWord(pair.longWord);
                StartCoroutine(PopScaleRoutine(transformWordText.transform, 1.45f, 0.35f));
            }

            // Transform picture to Long Word picture (e.g. cape)
            if (transformWordImage != null && pair.longWordSprite != null)
            {
                transformWordImage.sprite = pair.longWordSprite;
                transformWordImage.gameObject.SetActive(true);
                StartCoroutine(PopScaleRoutine(transformWordImage.transform, 1.25f, 0.35f));
            }

            PlaySFX(activityData.correctChimeSfx);
            TriggerWiggleStarMeter();
            totalStarsEarned++;
            UpdateStarMeterUI(totalStarsEarned, 8);

            // Return wand smoothly back to rest position
            if (wandScript != null)
            {
                wandScript.ReturnToStartPosition();
            }

            // 6. Voice Rule Dialogue / Full Spoken Word Audio (cap -> cape!)
            if (currentTransformIndex == 0)
            {
                SetDialogue("Momo: The e says nothing at all — it is silent! But it makes the a say its NAME. Caaape! Cape!");
                if (activityData != null && activityData.momoRuleExplanationClip != null)
                {
                    yield return PlayVoiceClip(activityData.momoRuleExplanationClip);
                }
                else
                {
                    yield return new WaitForSeconds(1.5f);
                }
            }
            else
            {
                SetDialogue($"Magic E power: '{pair.shortWord.ToUpper()}' becomes '{pair.longWord.ToUpper()}'!");
                if (pair.pairAudioClip != null)
                {
                    yield return PlayVoiceClip(pair.pairAudioClip);
                }
                else if (pair.longWordClip != null)
                {
                    yield return PlayVoiceClip(pair.longWordClip);
                }
                else
                {
                    yield return new WaitForSeconds(1.2f);
                }
            }

            if (backwardsSparkleTrail != null) backwardsSparkleTrail.SetActive(false);
            if (vowelStandUpGlow != null) vowelStandUpGlow.SetActive(false);

            currentTransformIndex++;
            isTransitioning = false;

            if (currentTransformIndex < 8)
            {
                LoadTransformRound(currentTransformIndex);
            }
            else
            {
                StartWhichOnePhase();
            }
        }

        #endregion

        private IEnumerator AnimateBackwardsSparkle(Vector3 fromPos, Vector3 toPos, float duration)
        {
            float elapsed = 0f;
            if (sparkleTrailRect != null)
            {
                sparkleTrailRect.position = fromPos;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                if (sparkleTrailRect != null)
                {
                    sparkleTrailRect.position = Vector3.Lerp(fromPos, toPos, smoothT);
                }
                yield return null;
            }
        }

        private void StartWhichOnePhase()
        {
            currentPhase = 2;
            if (transformPanel != null) transformPanel.SetActive(false);
            if (whichOnePanel != null) whichOnePanel.SetActive(true);
            if (wordWallPanel != null) wordWallPanel.SetActive(false);

            LoadWhichOneRound(0);
        }

        private void LoadWhichOneRound(int index)
        {
            if (activityData == null || activityData.whichOneChoices == null || index >= activityData.whichOneChoices.Length)
            {
                StartWordWallPhase();
                return;
            }

            currentWhichOneIndex = index;
            MagicEWhichOneChoice choice = activityData.whichOneChoices[index];

            if (choiceTextA != null)
            {
                choiceTextA.text = (choice.correctIndex == 0) ? MagicEData.FormatMagicEWord(choice.wordA) : choice.wordA;
            }
            if (choiceImageA != null)
            {
                if (choice.spriteA != null)
                {
                    choiceImageA.sprite = choice.spriteA;
                    choiceImageA.gameObject.SetActive(true);
                }
                else
                {
                    choiceImageA.gameObject.SetActive(false);
                }
            }

            if (choiceTextB != null)
            {
                choiceTextB.text = (choice.correctIndex == 1) ? MagicEData.FormatMagicEWord(choice.wordB) : choice.wordB;
            }
            if (choiceImageB != null)
            {
                if (choice.spriteB != null)
                {
                    choiceImageB.sprite = choice.spriteB;
                    choiceImageB.gameObject.SetActive(true);
                }
                else
                {
                    choiceImageB.gameObject.SetActive(false);
                }
            }

            string targetWord = (choice.correctIndex == 0) ? choice.wordA : choice.wordB;
            SetDialogue($"Leo: Which word is this? Listen… '{targetWord.ToUpper()}'");

            if (choice.spokenQuestionClip != null)
            {
                PlayVoiceClipNonBlocking(choice.spokenQuestionClip);
            }

            float progress = 0.45f + (index / 6f) * 0.35f;
            UpdateProgressUI(progress);
        }

        public void ReplayWhichOneAudio()
        {
            if (isTransitioning || currentPhase != 2 || activityData == null) return;
            if (currentWhichOneIndex >= activityData.whichOneChoices.Length) return;

            MagicEWhichOneChoice choice = activityData.whichOneChoices[currentWhichOneIndex];
            if (choice.spokenQuestionClip != null)
            {
                PlayVoiceClipNonBlocking(choice.spokenQuestionClip);
            }
            else
            {
                string targetWord = (choice.correctIndex == 0) ? choice.wordA : choice.wordB;
                SetDialogue($"Listen carefully… which one says '{targetWord.ToUpper()}'?");
            }
        }

        private void OnWhichOneChoiceSelected(int selectedIndex)
        {
            if (isTransitioning || currentPhase != 2 || activityData == null || currentWhichOneIndex >= activityData.whichOneChoices.Length)
            {
                return;
            }

            MagicEWhichOneChoice choice = activityData.whichOneChoices[currentWhichOneIndex];
            bool isCorrect = (selectedIndex == choice.correctIndex);

            if (isCorrect)
            {
                StartCoroutine(HandleWhichOneCorrect(choice, selectedIndex));
            }
            else
            {
                StartCoroutine(HandleWhichOneWrong(choice, selectedIndex));
            }
        }

        private IEnumerator HandleWhichOneCorrect(MagicEWhichOneChoice choice, int selectedIndex)
        {
            isTransitioning = true;
            PlaySFX(activityData.correctChimeSfx);
            TriggerWiggleStarMeter();

            Button selectedButton = (selectedIndex == 0) ? choiceButtonA : choiceButtonB;
            if (selectedButton != null)
            {
                StartCoroutine(PopScaleRoutine(selectedButton.transform, 1.18f, 0.3f));
            }

            string targetWord = (choice.correctIndex == 0) ? choice.wordA : choice.wordB;
            SetDialogue($"Leo: Yes! '{targetWord.ToUpper()}' is correct!");
            yield return new WaitForSeconds(0.9f);

            currentWhichOneIndex++;
            isTransitioning = false;
            LoadWhichOneRound(currentWhichOneIndex);
        }

        private IEnumerator HandleWhichOneWrong(MagicEWhichOneChoice choice, int selectedIndex)
        {
            isTransitioning = true;
            PlaySFX(activityData.retryGentleSfx);

            if (momoHintObject != null) momoHintObject.SetActive(true);

            Button selectedButton = (selectedIndex == 0) ? choiceButtonA : choiceButtonB;
            if (selectedButton != null)
            {
                StartCoroutine(WiggleRoutine(selectedButton.GetComponent<RectTransform>()));
            }

            SetDialogue("Momo: Look at the end of the word. Is there a magic e hiding there?");
            if (activityData != null && activityData.momoHintClip != null)
            {
                yield return PlayVoiceClip(activityData.momoHintClip);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            if (momoHintObject != null) momoHintObject.SetActive(false);
            isTransitioning = false;
        }

        private void StartWordWallPhase()
        {
            currentPhase = 3;
            if (transformPanel != null) transformPanel.SetActive(false);
            if (whichOnePanel != null) whichOnePanel.SetActive(false);
            if (wordWallPanel != null) wordWallPanel.SetActive(true);
            if (finishWordWallButton != null) finishWordWallButton.gameObject.SetActive(true);

            SetDialogue("Leo: Tap any card on the Magic E Word Wall to hear its long vowel sound!");

            // Populate word wall cards with rich colors for vowels and silent e
            if (activityData != null && activityData.magicEWordWallList != null)
            {
                string[] words = activityData.magicEWordWallList;

                if (wordWallCards != null && wordWallCards.Length > 0)
                {
                    for (int i = 0; i < wordWallCards.Length; i++)
                    {
                        if (i < words.Length && wordWallCards[i] != null)
                        {
                            int idx = i;
                            Sprite spr = (activityData.magicEWordWallSprites != null && i < activityData.magicEWordWallSprites.Length) ? activityData.magicEWordWallSprites[i] : null;
                            AudioClip clip = (activityData.magicEWordWallClips != null && i < activityData.magicEWordWallClips.Length) ? activityData.magicEWordWallClips[i] : null;
                            wordWallCards[i].SetupCard(words[i], spr, clip, (card) => OnWordWallCardClicked(idx));
                            wordWallCards[i].gameObject.SetActive(true);
                        }
                    }
                }
                else if (wordWallTexts != null)
                {
                    for (int i = 0; i < wordWallTexts.Length; i++)
                    {
                        if (i < words.Length && wordWallTexts[i] != null)
                        {
                            wordWallTexts[i].text = MagicEData.FormatMagicEWord(words[i]);
                            if (wordWallButtons != null && i < wordWallButtons.Length && wordWallButtons[i] != null)
                            {
                                wordWallButtons[i].gameObject.SetActive(true);
                            }
                        }
                    }
                }
            }

            UpdateProgressUI(0.92f);
        }

        private void OnWordWallCardClicked(int index)
        {
            if (activityData == null || activityData.magicEWordWallList == null) return;
            if (index < 0 || index >= activityData.magicEWordWallList.Length) return;

            exploredWordWallIndices.Add(index);
            PlaySFX(activityData.starPopSfx);

            if (wordWallButtons != null && index < wordWallButtons.Length && wordWallButtons[index] != null)
            {
                StartCoroutine(PopScaleRoutine(wordWallButtons[index].transform, 1.15f, 0.25f));
            }

            string word = activityData.magicEWordWallList[index];
            SetDialogue($"Magic E power: '{word.ToUpper()}'!");

            if (activityData.magicEWordWallClips != null && index < activityData.magicEWordWallClips.Length && activityData.magicEWordWallClips[index] != null)
            {
                PlayVoiceClipNonBlocking(activityData.magicEWordWallClips[index]);
            }
        }

        public void CompleteStop2()
        {
            if (isActivityCompleted) return;
            StartCoroutine(CompleteStop2Sequence());
        }

        private IEnumerator CompleteStop2Sequence()
        {
            isActivityCompleted = true;
            isTransitioning = true;
            PlaySFX(activityData.correctChimeSfx);
            UpdateProgressUI(1.0f);
            UpdateStarMeterUI(8, 8);

            SetDialogue("Leo: One little silent e — and the whole word changes. That is magic!");
            if (activityData != null && activityData.leoClosingClip != null)
            {
                yield return PlayVoiceClip(activityData.leoClosingClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            isTransitioning = false;
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
                yield return new WaitForSeconds(clip.length + 0.15f);
            }
        }

        private void UpdateProgressUI(float fillAmount)
        {
            if (progressRingFillImage != null) progressRingFillImage.fillAmount = fillAmount;
            if (progressText != null) progressText.text = $"{Mathf.RoundToInt(fillAmount * 100)}%";
        }

        private void UpdateStarMeterUI(int stars, int total)
        {
            if (starMeterFillImage != null)
            {
                starMeterFillImage.fillAmount = (float)stars / Mathf.Max(1, total);
            }
            if (starCountText != null)
            {
                starCountText.text = $"{stars} / {total} Stars";
            }
        }

        private void TriggerWiggleStarMeter()
        {
            if (starMeterRect != null) StartCoroutine(WiggleRoutine(starMeterRect));
        }

        private IEnumerator PopScaleRoutine(Transform target, float maxScaleMultiplier, float duration)
        {
            if (target == null) yield break;
            Vector3 originalScale = target.localScale;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / duration;
                float scale = 1f + Mathf.Sin(progress * Mathf.PI) * (maxScaleMultiplier - 1f);
                target.localScale = originalScale * scale;
                yield return null;
            }

            target.localScale = originalScale;
        }

        private IEnumerator WiggleRoutine(RectTransform target)
        {
            if (target == null) yield break;
            float elapsed = 0f;
            float duration = 0.35f;
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

        public void GoToNextPanel()
        {
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            TopicProgressUI.HideTopicCompletePanel();
            DeactivateMascots();

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
    }
}

