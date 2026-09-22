using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EngSnap.Common;
using EngSnap.Phonics2.Unit2;

namespace EngSnap.Phonics2.Unit5
{
    public class LongVowelPlayTimeController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit5";
        [SerializeField] private string topicName = "LongVowelPlayTime";

        [Header("Data Asset")]
        [SerializeField] private LongVowelPlayTimeData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Header / Dialogue UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Phase 1: Gap-Fill Worksheet UI")]
        [SerializeField] private GameObject worksheetPanel;
        [SerializeField] private TMP_Text worksheetWordText;
        [SerializeField] private Image worksheetWordImage;
        [SerializeField] private Image wordGlowHighlight;
        [SerializeField] private RectTransform gapDropSlot;
        [SerializeField] private LongVowelPlayTimeTile[] tileUIs = new LongVowelPlayTimeTile[3];
        [SerializeField] private Button startStarRoundButton; // INACTIVE initially - activates ONLY after Phase 1 worksheet complete!

        [Header("Phase 1: Letter Tracing UI (Reusing Unit 2 Tracing Component)")]
        [SerializeField] private GameObject tracingPanel;
        [SerializeField] private LetterTracingComponent letterTracingComponent;
        [SerializeField] private TMP_Text tracingPromptTMP;
        [SerializeField] private Image tracingWordImage;

        [Header("Phase 2: Tara Star Round UI")]
        [SerializeField] private GameObject starRoundPanel;
        [SerializeField] private TMP_Text starPromptTMP;
        [SerializeField] private Image starPromptImage;
        [SerializeField] private Button[] starChoiceButtons = new Button[3];
        [SerializeField] private TMP_Text[] starChoiceTexts = new TMP_Text[3];
        [SerializeField] private Image[] starChoiceImages = new Image[3];

        [Header("Progress & Mascot UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject taraMascotObject;
        [SerializeField] private GameObject momoHintObject;

        [Header("SFX Feedback Clips")]
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip retryGentleSfx;
        [SerializeField] private AudioClip wordSnapSfx;
        [SerializeField] private AudioClip starPopSfx;

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

        private int worksheetIndex = 0;
        private int starChallengeIndex = 0;
        private int tracingFailAttempts = 0;
        private bool isStarRoundActive = false;
        private bool isTransitioning = false;
        private bool isWaitingForTracing = false;
        private Camera mainCamera;
        private PlayTimeWorksheetItem currentWorksheetItem;
        private StarRoundUnit5Challenge currentStarChallenge;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            mainCamera = Camera.main;
            EnsureAudioSources();
            EnsureDataAssigned();
            SetupTracingEventListeners();
        }

        private void Start()
        {
            SetupButtonListeners();
            StartActivity();
        }

        private void OnEnable()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (taraMascotObject != null) taraMascotObject.SetActive(false);
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
            RemoveTracingEventListeners();
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
                activityData = Resources.Load<LongVowelPlayTimeData>("Phonics2/Unit5/LongVowelPlayTimeData_Unit5");
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
            if (taraMascotObject != null) taraMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
        }

        private void SetupTracingEventListeners()
        {
            if (letterTracingComponent != null)
            {
                letterTracingComponent.OnTracingCompleted += HandleTracingCompleted;
                letterTracingComponent.OnTracingFailedAttempt += HandleTracingFailedAttempt;
            }
        }

        private void RemoveTracingEventListeners()
        {
            if (letterTracingComponent != null)
            {
                letterTracingComponent.OnTracingCompleted -= HandleTracingCompleted;
                letterTracingComponent.OnTracingFailedAttempt -= HandleTracingFailedAttempt;
            }
        }

        private void SetupButtonListeners()
        {
            if (startStarRoundButton != null)
            {
                startStarRoundButton.onClick.RemoveAllListeners();
                startStarRoundButton.onClick.AddListener(StartStarRound);
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
            worksheetIndex = 0;
            starChallengeIndex = 0;
            tracingFailAttempts = 0;
            isStarRoundActive = false;
            isTransitioning = false;
            isWaitingForTracing = false;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (wordGlowHighlight != null) wordGlowHighlight.gameObject.SetActive(false);
            if (tracingPanel != null) tracingPanel.SetActive(false);

            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (taraMascotObject != null) taraMascotObject.SetActive(false);

            if (worksheetPanel != null) worksheetPanel.SetActive(true);
            if (starRoundPanel != null) starRoundPanel.SetActive(false);

            SetDialogue("Look at the picture and listen. Which letters are missing?");
            if (activityData != null && activityData.leoIntroClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.leoIntroClip);
            }

            LoadWorksheetItem(0);
        }

        private void LoadWorksheetItem(int index)
        {
            if (activityData == null || activityData.worksheetItems == null || index >= activityData.worksheetItems.Length)
            {
                CompleteWorksheetPhase();
                return;
            }

            worksheetIndex = index;
            tracingFailAttempts = 0;
            isWaitingForTracing = false;
            isTransitioning = false;

            if (tracingPanel != null) tracingPanel.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (wordGlowHighlight != null) wordGlowHighlight.gameObject.SetActive(false);

            currentWorksheetItem = activityData.worksheetItems[index];

            if (worksheetWordText != null)
            {
                worksheetWordText.text = currentWorksheetItem.wordWithGap;
                worksheetWordText.transform.localScale = Vector3.one;
            }

            if (worksheetWordImage != null)
            {
                if (currentWorksheetItem.wordSprite != null)
                {
                    worksheetWordImage.sprite = currentWorksheetItem.wordSprite;
                    worksheetWordImage.gameObject.SetActive(true);
                }
                else
                {
                    worksheetWordImage.gameObject.SetActive(false);
                }
                worksheetWordImage.transform.localScale = Vector3.one;
            }

            for (int t = 0; t < tileUIs.Length; t++)
            {
                if (t < currentWorksheetItem.tileOptions.Length && tileUIs[t] != null)
                {
                    tileUIs[t].SetupTile(currentWorksheetItem.tileOptions[t], this);
                    tileUIs[t].gameObject.SetActive(true);
                }
                else if (tileUIs[t] != null)
                {
                    tileUIs[t].gameObject.SetActive(false);
                }
            }

            SetDialogue($"Listen: {currentWorksheetItem.fullWordText.ToUpper()}. Which letters make that sound?");
            if (currentWorksheetItem.missingSoundClip != null)
            {
                PlayVoiceClipNonBlocking(currentWorksheetItem.missingSoundClip);
            }

            UpdateProgressUI((index / 9f) * 0.5f);
        }

        public void EvaluateTileDrop(LongVowelPlayTimeTile tile, PointerEventData eventData)
        {
            if (isTransitioning || isWaitingForTracing || tile == null || activityData == null || worksheetIndex >= activityData.worksheetItems.Length)
            {
                if (tile != null) tile.ReturnToStartPosition();
                return;
            }

            PlayTimeWorksheetItem item = activityData.worksheetItems[worksheetIndex];
            bool targetHit = false;

            if (gapDropSlot != null && eventData != null)
            {
                Canvas canvas = GetComponentInParent<Canvas>();
                Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : mainCamera;
                targetHit = RectTransformUtility.RectangleContainsScreenPoint(gapDropSlot, eventData.position, cam);
            }

            if (targetHit)
            {
                bool isCorrect = (string.Equals(tile.TileSpelling, item.correctSpellingTile, System.StringComparison.OrdinalIgnoreCase));
                if (isCorrect)
                {
                    if (gapDropSlot != null)
                    {
                        tile.SnapToTarget(gapDropSlot.anchoredPosition);
                    }
                    StartCoroutine(HandleWorksheetCorrect(tile, item));
                }
                else
                {
                    tile.ReturnToStartPosition();
                    StartCoroutine(HandleWorksheetWrong(tile.TileSpelling, item));
                }
            }
            else
            {
                tile.ReturnToStartPosition();
            }
        }

        /// <summary>
        /// When the correct tile is placed: the completed word lights up with long vowel glowing,
        /// word is read aloud, then transitions into the Trace It phase where the child finger-traces
        /// the placed letters using Unit 2's LetterTracingComponent and says the word.
        /// </summary>
        private IEnumerator HandleWorksheetCorrect(LongVowelPlayTimeTile tile, PlayTimeWorksheetItem item)
        {
            isTransitioning = true;
            PlaySFX(activityData != null && activityData.correctChimeSfx != null ? activityData.correctChimeSfx : correctChimeSfx);
            TriggerWiggleStarMeter();

            // Word lights up with Glowing Long Vowel
            if (worksheetWordText != null)
            {
                worksheetWordText.text = LongVowelPlayTimeData.FormatGlowingWord(item.fullWordText, item.correctSpellingTile);
                StartCoroutine(PopScaleRoutine(worksheetWordText.transform, 1.3f, 0.35f));
            }

            if (wordGlowHighlight != null)
            {
                wordGlowHighlight.gameObject.SetActive(true);
                StartCoroutine(PopScaleRoutine(wordGlowHighlight.transform, 1.25f, 0.35f));
            }

            if (worksheetWordImage != null)
            {
                StartCoroutine(PopScaleRoutine(worksheetWordImage.transform, 1.15f, 0.35f));
            }

            // Word is read aloud
            SetDialogue($"Yes! {item.fullWordText.ToUpper()}! Look at the long vowel glow!");
            if (item.wordAudioClip != null)
            {
                yield return PlayVoiceClip(item.wordAudioClip);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            if (tile != null)
            {
                tile.HideTile();
            }

            isTransitioning = false;

            // Start Tracing Phase for the placed letters
            StartTracingPhase(item);
        }

        /// <summary>
        /// When child chooses the wrong spelling: the tile floats back and Leo says both options aloud:
        /// "we could write ee, or ea. This one is ee." Then the child places it.
        /// </summary>
        private IEnumerator HandleWorksheetWrong(string chosenSpelling, PlayTimeWorksheetItem item)
        {
            isTransitioning = true;
            PlaySFX(activityData != null && activityData.retryGentleSfx != null ? activityData.retryGentleSfx : retryGentleSfx);

            string leoPrompt = $"Leo: We could write {chosenSpelling}, or {item.correctSpellingTile}. This one is {item.correctSpellingTile}.";
            SetDialogue(leoPrompt);

            if (item.wrongFeedbackClip != null)
            {
                yield return PlayVoiceClip(item.wrongFeedbackClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            isTransitioning = false;
        }

        #region Tracing Phase (Reusing Unit 2 LetterTracingComponent)

        private void StartTracingPhase(PlayTimeWorksheetItem item)
        {
            if (item == null) return;

            isWaitingForTracing = true;
            tracingFailAttempts = 0;

            if (tracingPanel != null)
            {
                tracingPanel.SetActive(true);
            }

            if (tracingPromptTMP != null)
            {
                tracingPromptTMP.text = $"Trace: {item.correctSpellingTile}";
            }

            if (tracingWordImage != null && item.wordSprite != null)
            {
                tracingWordImage.sprite = item.wordSprite;
                tracingWordImage.gameObject.SetActive(true);
            }

            SetDialogue($"Now trace the letters '{item.correctSpellingTile}' and say the word — {item.fullWordText}!");

            if (item.tracingPromptClip != null)
            {
                PlayVoiceClipNonBlocking(item.tracingPromptClip);
            }

            if (letterTracingComponent != null)
            {
                letterTracingComponent.gameObject.SetActive(true);
                AudioClip traceClip = item.letterSoundClip != null ? item.letterSoundClip : item.wordAudioClip;
                letterTracingComponent.SetupTracing(
                    item.tracingOutlineSprite,
                    traceClip,
                    item.correctSpellingTile,
                    item.filledLetterSprite,
                    item.checkpointPositions
                );
            }
            else
            {
                // Graceful fallback if LetterTracingComponent is not assigned in the inspector
                StartCoroutine(HandleTracingCompletedSequence(item));
            }
        }

        private void HandleTracingFailedAttempt()
        {
            if (!isWaitingForTracing) return;

            tracingFailAttempts++;
            PlaySFX(activityData != null && activityData.retryGentleSfx != null ? activityData.retryGentleSfx : retryGentleSfx);

            if (tracingFailAttempts >= 2)
            {
                if (momoHintObject != null) momoHintObject.SetActive(true);
                SetDialogue("Follow Momo! Start at the glowing checkpoint and draw smooth.");
                if (letterTracingComponent != null) letterTracingComponent.PlayGhostFingerGuide();
            }
        }

        private void HandleTracingCompleted()
        {
            if (!isWaitingForTracing || isTransitioning || currentWorksheetItem == null) return;
            StartCoroutine(HandleTracingCompletedSequence(currentWorksheetItem));
        }

        private IEnumerator HandleTracingCompletedSequence(PlayTimeWorksheetItem item)
        {
            isWaitingForTracing = false;
            isTransitioning = true;

            PlaySFX(activityData != null && activityData.wordSnapSfx != null ? activityData.wordSnapSfx : wordSnapSfx);
            PlaySFX(activityData != null && activityData.correctChimeSfx != null ? activityData.correctChimeSfx : correctChimeSfx);
            TriggerWiggleStarMeter();

            if (momoHintObject != null) momoHintObject.SetActive(false);

            SetDialogue($"{item.fullWordText.ToUpper()}! You traced '{item.correctSpellingTile}' and said the word!");

            if (item.wordAudioClip != null)
            {
                yield return PlayVoiceClip(item.wordAudioClip);
            }
            else
            {
                yield return new WaitForSeconds(0.9f);
            }

            yield return new WaitForSeconds(0.4f);

            if (tracingPanel != null) tracingPanel.SetActive(false);
            if (wordGlowHighlight != null) wordGlowHighlight.gameObject.SetActive(false);

            worksheetIndex++;
            isTransitioning = false;

            if (worksheetIndex < 9 && activityData != null && worksheetIndex < activityData.worksheetItems.Length)
            {
                LoadWorksheetItem(worksheetIndex);
            }
            else
            {
                CompleteWorksheetPhase();
            }
        }

        #endregion

        private void CompleteWorksheetPhase()
        {
            if (worksheetWordText != null) worksheetWordText.text = "COMPLETE!";
            if (startStarRoundButton != null)
            {
                startStarRoundButton.gameObject.SetActive(true);
            }
            if (tracingPanel != null) tracingPanel.SetActive(false);

            SetDialogue("Great job placing and tracing all words! Tap 'Start Star Round' to continue with Tara! ⭐");
        }

        private void StartStarRound()
        {
            if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(false);
            if (worksheetPanel != null) worksheetPanel.SetActive(false);
            if (tracingPanel != null) tracingPanel.SetActive(false);
            if (starRoundPanel != null) starRoundPanel.SetActive(true);

            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (taraMascotObject != null) taraMascotObject.SetActive(true);

            isStarRoundActive = true;
            starChallengeIndex = 0;

            SetDialogue("Tara says: My turn! Six quick challenges. Ready? Roar!");
            if (activityData != null && activityData.taraOpenerClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.taraOpenerClip);
            }

            LoadStarChallenge(0);
        }

        private void LoadStarChallenge(int index)
        {
            if (activityData == null || activityData.starChallenges == null || index >= activityData.starChallenges.Length)
            {
                StartCoroutine(CompleteStarRoundSequence());
                return;
            }

            starChallengeIndex = index;
            currentStarChallenge = activityData.starChallenges[index];

            if (starPromptTMP != null) starPromptTMP.text = currentStarChallenge.questionPrompt;
            if (starPromptImage != null && currentStarChallenge.promptSprite != null)
            {
                starPromptImage.sprite = currentStarChallenge.promptSprite;
                starPromptImage.gameObject.SetActive(true);
            }
            else if (starPromptImage != null)
            {
                starPromptImage.gameObject.SetActive(false);
            }

            for (int c = 0; c < starChoiceButtons.Length; c++)
            {
                if (c < currentStarChallenge.choices.Length && starChoiceButtons[c] != null)
                {
                    starChoiceButtons[c].gameObject.SetActive(true);
                    if (starChoiceTexts != null && c < starChoiceTexts.Length && starChoiceTexts[c] != null)
                    {
                        starChoiceTexts[c].text = currentStarChallenge.choices[c];
                    }
                    if (starChoiceImages != null && c < starChoiceImages.Length && starChoiceImages[c] != null)
                    {
                        if (currentStarChallenge.choiceSprites != null && c < currentStarChallenge.choiceSprites.Length && currentStarChallenge.choiceSprites[c] != null)
                        {
                            starChoiceImages[c].sprite = currentStarChallenge.choiceSprites[c];
                            starChoiceImages[c].gameObject.SetActive(true);
                        }
                        else
                        {
                            starChoiceImages[c].gameObject.SetActive(false);
                        }
                    }
                }
                else if (starChoiceButtons[c] != null)
                {
                    starChoiceButtons[c].gameObject.SetActive(false);
                }
            }

            SetDialogue(currentStarChallenge.questionPrompt);
            if (currentStarChallenge.promptClip != null)
            {
                PlayVoiceClipNonBlocking(currentStarChallenge.promptClip);
            }

            UpdateProgressUI(0.5f + (index / 6f) * 0.5f);
        }

        private void OnStarChoiceSelected(int choiceIndex)
        {
            if (isTransitioning || currentStarChallenge == null) return;

            bool isCorrect = (choiceIndex == currentStarChallenge.correctChoiceIndex);
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
            PlaySFX(activityData != null && activityData.correctChimeSfx != null ? activityData.correctChimeSfx : correctChimeSfx);
            TriggerWiggleStarMeter();

            SetDialogue("Roar! You got it right!");
            yield return new WaitForSeconds(0.8f);

            starChallengeIndex++;
            isTransitioning = false;
            LoadStarChallenge(starChallengeIndex);
        }

        private IEnumerator HandleStarChoiceWrong()
        {
            isTransitioning = true;
            PlaySFX(activityData != null && activityData.retryGentleSfx != null ? activityData.retryGentleSfx : retryGentleSfx);

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

        private IEnumerator CompleteStarRoundSequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null && activityData.correctChimeSfx != null ? activityData.correctChimeSfx : correctChimeSfx);
            UpdateProgressUI(1.0f);

            SetDialogue("Short vowels, long vowels, magic e and teams. You are a LONG VOWEL HERO!");
            if (activityData != null && activityData.badgeVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.badgeVoiceClip);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);

            yield return new WaitForSeconds(1.5f);

            SetDialogue("Unit Six is open! Next time we find out which sounds BUZZ and which ones whisper!");
            if (activityData != null && activityData.unit6UnlockVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.unit6UnlockVoiceClip);
            }

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            if (continueButton != null) continueButton.gameObject.SetActive(true);
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
            if (starMeterRect != null) TriggerWiggle(starMeterRect);
        }

        private void TriggerWiggle(RectTransform target)
        {
            if (target != null) StartCoroutine(WiggleAnimation(target));
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

            if (nextPanel != null)
            {
                nextPanel.SetActive(true);
            }
            else if (unitContentPanel != null)
            {
                unitContentPanel.SetActive(true);
            }

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
