using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;
using EngSnap.Phonics2.Unit8;

namespace EngSnap.Phonics2.Unit9
{
    public class BlendItAgainController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit9";
        [SerializeField] private string topicName = "BlendItAgain";

        [Header("Session Selection")]
        [Tooltip("0 = Short i, 1 = Short o, 2 = Short u, 3 = Mixed 5-Vowels")]
        [SerializeField] private int activeSessionIndex = 0;

        [Header("Data Asset")]
        [SerializeField] private BlendItAgainData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Dialogue / Subtitle UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Sound Boxes & Slider")]
        [SerializeField] private GameObject blendingPanel;
        [SerializeField] private RectTransform[] soundBoxRects = new RectTransform[3];
        [SerializeField] private TMP_Text[] soundBoxTexts = new TMP_Text[3];
        [SerializeField] private Image[] soundBoxGlowImages = new Image[3];
        [SerializeField] private BlendSlideBar blendSlideBar;
        [SerializeField] private Slider unityBlendSlider;
        [SerializeField] private GameObject slideBarContainer;
        [SerializeField] private Button helpSliderButton;
        [SerializeField] private Button directReadButton;

        [Header("5 Vowel Houses (Session 4 Trackside UI)")]
        [SerializeField] private GameObject fiveVowelHousesContainer;
        [SerializeField] private Button[] vowelHouseButtons = new Button[5]; // a, e, i, o, u
        [SerializeField] private Image[] vowelHouseHighlightImages = new Image[5];

        [Header("Word Picture Reveal")]
        [SerializeField] private GameObject pictureRevealPanel;
        [SerializeField] private Image wordPictureImage;
        [SerializeField] private TMP_Text wholeWordTMP;

        [Header("Vowel Triples Discrimination UI")]
        [SerializeField] private GameObject vowelTriplesPanel;
        [SerializeField] private TMP_Text triplePromptTMP;
        [SerializeField] private Button[] tripleChoiceButtons = new Button[3];
        [SerializeField] private TMP_Text[] tripleChoiceTexts = new TMP_Text[3];
        [SerializeField] private Image[] tripleChoiceImages = new Image[3];
        [SerializeField] private Button replayTriplePromptButton;

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
        private int currentTripleIndex = 0;
        private bool isTransitioning = false;
        private bool hasInitialized = false;
        private bool isTriplePhase = false;
        private float hesitationTimer = 0f;
        private bool isWaitingForUserAction = false;
        private Vector3[] initialBoxPositions = new Vector3[3];
        private BlendWordItem currentWord;
        private VowelTripleItem currentTriple;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
            EnsureDataAssigned();

            for (int i = 0; i < soundBoxRects.Length; i++)
            {
                if (soundBoxRects[i] != null) initialBoxPositions[i] = soundBoxRects[i].localPosition;
            }

            if (blendSlideBar != null)
            {
                blendSlideBar.OnLetterStopReached += OnSlideBarLetterStop;
                blendSlideBar.OnBlendCompleted += OnSlideBarBlendCompleted;
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
            }
        }

        private void Update()
        {
            if (activeSessionIndex < 3 && isWaitingForUserAction && slideBarContainer != null && !slideBarContainer.activeSelf)
            {
                hesitationTimer += Time.deltaTime;
                if (hesitationTimer >= 4.0f)
                {
                    hesitationTimer = 0f;
                    slideBarContainer.SetActive(true);
                    if (blendSlideBar != null) blendSlideBar.ResetSlider();
                }
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
                activityData = Resources.Load<BlendItAgainData>("Phonics2/Unit9/BlendItAgainData_Unit9");
            }
            if (activityData == null)
            {
                activityData = ScriptableObject.CreateInstance<BlendItAgainData>();
                activityData.PopulateDefaultData();
            }
        }

        private void SetupButtonListeners()
        {
            if (helpSliderButton != null)
            {
                helpSliderButton.onClick.RemoveAllListeners();
                helpSliderButton.onClick.AddListener(() =>
                {
                    if (slideBarContainer != null) slideBarContainer.SetActive(true);
                    if (blendSlideBar != null) blendSlideBar.ResetSlider();
                });
            }

            if (directReadButton != null)
            {
                directReadButton.onClick.RemoveAllListeners();
                directReadButton.onClick.AddListener(OnDirectReadClicked);
            }

            if (replayTriplePromptButton != null)
            {
                replayTriplePromptButton.onClick.RemoveAllListeners();
                replayTriplePromptButton.onClick.AddListener(ReplayTriplePromptAudio);
            }

            // Vowel House buttons (Session 4)
            for (int i = 0; i < vowelHouseButtons.Length; i++)
            {
                int index = i;
                if (vowelHouseButtons[i] != null)
                {
                    vowelHouseButtons[i].onClick.RemoveAllListeners();
                    vowelHouseButtons[i].onClick.AddListener(() => OnVowelHouseClicked(index));
                }
            }

            // Vowel Triples choices
            for (int i = 0; i < tripleChoiceButtons.Length; i++)
            {
                int index = i;
                if (tripleChoiceButtons[i] != null)
                {
                    tripleChoiceButtons[i].onClick.RemoveAllListeners();
                    tripleChoiceButtons[i].onClick.AddListener(() => OnTripleChoiceSelected(index));
                }
            }

            // Setup click listeners on sound boxes
            for (int i = 0; i < soundBoxRects.Length; i++)
            {
                int index = i;
                if (soundBoxRects[i] != null)
                {
                    Button boxBtn = soundBoxRects[i].GetComponent<Button>();
                    if (boxBtn == null) boxBtn = soundBoxRects[i].gameObject.AddComponent<Button>();
                    boxBtn.onClick.RemoveAllListeners();
                    boxBtn.onClick.AddListener(() => OnSoundBoxClicked(index));
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

        public void ResetLevel()
        {
            StartActivity();
        }

        public void StartActivity()
        {
            StopAllCoroutines();
            activeSessionIndex = 0;
            currentWordIndex = 0;
            currentTripleIndex = 0;
            isTriplePhase = false;
            isTransitioning = false;
            hesitationTimer = 0f;
            isWaitingForUserAction = false;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (pictureRevealPanel != null) pictureRevealPanel.SetActive(false);
            if (vowelTriplesPanel != null) vowelTriplesPanel.SetActive(false);

            if (blendingPanel != null) blendingPanel.SetActive(true);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);

            StartCoroutine(RunActivityIntroRoutine());
        }

        private IEnumerator RunActivityIntroRoutine()
        {
            isTransitioning = true;

            if (activeSessionIndex < 3)
            {
                if (fiveVowelHousesContainer != null) fiveVowelHousesContainer.SetActive(false);
                SetDialogue("You know the slide. Now let us use it on some new vowels!");
                if (activityData != null && activityData.leoIntroClip != null)
                {
                    yield return PlayVoiceClip(activityData.leoIntroClip);
                }
                else
                {
                    yield return new WaitForSeconds(1.8f);
                }
            }
            else
            {
                // Session 4 Mixed / Vowels Round
                if (fiveVowelHousesContainer != null) fiveVowelHousesContainer.SetActive(true);
                SetDialogue("Now the vowel round! This word could have ANY vowel in the middle. Just tap the middle vowel!");
                if (activityData != null && activityData.mixedSessionIntroClip != null)
                {
                    yield return PlayVoiceClip(activityData.mixedSessionIntroClip);
                }
                else
                {
                    yield return new WaitForSeconds(2.0f);
                }
            }

            isTransitioning = false;
            LoadWordRound(0);
        }

        private BlendWordItem[] GetActiveWordSet()
        {
            if (activityData == null) return null;
            if (activeSessionIndex == 0) return activityData.shortIWords;
            if (activeSessionIndex == 1) return activityData.shortOWords;
            if (activeSessionIndex == 2) return activityData.shortUWords;
            return activityData.mixedWords;
        }

        private void LoadWordRound(int index)
        {
            BlendWordItem[] wordSet = GetActiveWordSet();
            if (wordSet == null || index >= wordSet.Length)
            {
                if (activeSessionIndex < 3)
                {
                    StartCoroutine(TransitionToNextSessionRoutine());
                    return;
                }
                else if (activeSessionIndex == 3 && !isTriplePhase)
                {
                    StartVowelTriplesPhase();
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

        private IEnumerator TransitionToNextSessionRoutine()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            activeSessionIndex++;
            string nextVowelName = (activeSessionIndex == 1) ? "short 'o'" : (activeSessionIndex == 2) ? "short 'u'" : "all five vowels";
            SetDialogue($"Awesome blending! Now let's try {nextVowelName}!");

            yield return new WaitForSeconds(1.8f);

            if (activeSessionIndex == 3 && fiveVowelHousesContainer != null)
            {
                fiveVowelHousesContainer.SetActive(true);
            }

            currentWordIndex = 0;
            isTransitioning = false;
            LoadWordRound(0);
        }

        private IEnumerator LoadWordRoundRoutine(int index, BlendWordItem item)
        {
            isTransitioning = true;
            currentWordIndex = index;
            currentWord = item;
            hesitationTimer = 0f;
            isWaitingForUserAction = true;

            if (blendingPanel != null) blendingPanel.SetActive(true);
            if (pictureRevealPanel != null) pictureRevealPanel.SetActive(false);

            ResetSoundBoxVisuals();

            if (soundBoxTexts.Length >= 3 && currentWord != null)
            {
                if (soundBoxTexts[0] != null) soundBoxTexts[0].text = currentWord.firstLetter;
                if (soundBoxTexts[1] != null) soundBoxTexts[1].text = currentWord.middleLetter;
                if (soundBoxTexts[2] != null) soundBoxTexts[2].text = currentWord.lastLetter;
            }

            if (activeSessionIndex < 3)
            {
                // Sessions 1-3: Same as Unit 8 Blend It (slide bar active by default)
                if (slideBarContainer != null) slideBarContainer.SetActive(true);
                if (fiveVowelHousesContainer != null) fiveVowelHousesContainer.SetActive(false);
                if (blendSlideBar != null)
                {
                    blendSlideBar.SetInteractable(true);
                    blendSlideBar.ResetSlider();
                }
                SetDialogue($"Slide your finger: /{currentWord.firstLetter}/ … /{currentWord.middleLetter}/ … /{currentWord.lastLetter}/ … and blend!");
            }
            else
            {
                // Session 4 (Vowels Round): Remove slider, just tap middle vowel or vowel house!
                if (slideBarContainer != null) slideBarContainer.SetActive(false);
                if (fiveVowelHousesContainer != null) fiveVowelHousesContainer.SetActive(true);
                SetDialogue($"Vowel Round! Tap the middle vowel '{currentWord.middleLetter}' or its vowel house to blend!");
            }

            float totalCompleted = (activeSessionIndex * 8f) + index;
            UpdateProgressUI(totalCompleted / 38f);

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

            if (vowelHouseHighlightImages != null)
            {
                for (int i = 0; i < vowelHouseHighlightImages.Length; i++)
                {
                    if (vowelHouseHighlightImages[i] != null) vowelHouseHighlightImages[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnSoundBoxClicked(int index)
        {
            if (isTransitioning || currentWord == null || index < 0 || index >= 3) return;

            PlaySFX(activityData != null ? activityData.soundBoxGlowSfx : null);
            if (soundBoxGlowImages != null && index < soundBoxGlowImages.Length && soundBoxGlowImages[index] != null)
            {
                soundBoxGlowImages[index].gameObject.SetActive(true);
            }
            if (soundBoxRects != null && index < soundBoxRects.Length && soundBoxRects[index] != null)
            {
                StartCoroutine(PopScaleRoutine(soundBoxRects[index], 1.25f, 0.2f));
            }

            AudioClip clip = (index == 0) ? currentWord.firstSoundAudio : (index == 1) ? currentWord.middleSoundAudio : currentWord.lastSoundAudio;
            if (clip != null)
            {
                PlayVoiceClipNonBlocking(clip);
            }

            // In Session 4 (Vowels round), tapping the middle vowel directly blends the word!
            if (index == 1 && activeSessionIndex >= 3)
            {
                StartCoroutine(HandleBlendSuccessRoutine());
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

        private void OnDirectReadClicked()
        {
            if (isTransitioning || currentWord == null) return;
            if (directReadButton != null)
            {
                StartCoroutine(PopScaleRoutine(directReadButton.transform, 1.15f, 0.2f));
            }
            StartCoroutine(HandleBlendSuccessRoutine());
        }

        private void OnVowelHouseClicked(int vowelIndex)
        {
            if (isTransitioning || currentWord == null) return;

            char[] vowels = new char[] { 'a', 'e', 'i', 'o', 'u' };
            if (vowelIndex < 0 || vowelIndex >= vowels.Length) return;

            char clickedVowel = vowels[vowelIndex];
            char targetVowel = (!string.IsNullOrEmpty(currentWord.middleLetter))
                ? currentWord.middleLetter.ToLower()[0]
                : ' ';

            // Highlight the clicked vowel house
            if (vowelHouseHighlightImages != null && vowelIndex < vowelHouseHighlightImages.Length && vowelHouseHighlightImages[vowelIndex] != null)
            {
                vowelHouseHighlightImages[vowelIndex].gameObject.SetActive(true);
            }

            if (clickedVowel == targetVowel)
            {
                PlaySFX(activityData != null ? activityData.vowelHouseSnapSfx : null);
                if (soundBoxTexts != null && soundBoxTexts.Length > 1 && soundBoxTexts[1] != null)
                {
                    soundBoxTexts[1].text = clickedVowel.ToString();
                }
                if (soundBoxGlowImages != null && soundBoxGlowImages.Length > 1 && soundBoxGlowImages[1] != null)
                {
                    soundBoxGlowImages[1].gameObject.SetActive(true);
                }
                if (soundBoxRects != null && soundBoxRects.Length > 1 && soundBoxRects[1] != null)
                {
                    StartCoroutine(PopScaleRoutine(soundBoxRects[1], 1.3f, 0.2f));
                }

                SetDialogue($"Yes! The '{clickedVowel}' is in the middle!");
                StartCoroutine(HandleBlendSuccessRoutine());
            }
            else
            {
                PlaySFX(activityData != null ? activityData.retryGentleSfx : null);
                SetDialogue($"Try each house — {currentWord.firstLetter}-{clickedVowel}-{currentWord.lastLetter}? Which one sounds right?");
                if (activityData != null && activityData.momoVowelHouseHintClip != null)
                {
                    PlayVoiceClipNonBlocking(activityData.momoVowelHouseHintClip);
                }
            }
        }

        private IEnumerator HandleBlendSuccessRoutine()
        {
            isTransitioning = true;
            isWaitingForUserAction = false;
            if (blendSlideBar != null) blendSlideBar.SetInteractable(false);

            string displayWord = currentWord.fullWord.ToUpper();
            SetDialogue($"{displayWord}!");

            // 1. Play blended word sound AND squash SFX SIMULTANEOUSLY while wobbling!
            PlaySFX(activityData != null ? activityData.blendSquashSfx : null);
            if (currentWord.blendedWordAudio != null)
            {
                PlayVoiceClipNonBlocking(currentWord.blendedWordAudio);
            }

            // 2. Sound boxes glow and wobble excitedly while audio plays
            yield return StartCoroutine(WobbleBoxesRoutine(0.55f));

            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            // 3. Word Picture Reveal Panel
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

            if (currentWordIndex == 1 && activityData != null && activityData.noSliderPraiseClip != null)
            {
                SetDialogue($"{displayWord}! Straight away, with no slider. You are getting fast!");
                yield return PlayVoiceClip(activityData.noSliderPraiseClip);
            }

            yield return new WaitForSeconds(0.6f);

            ResetSliderToZero();
            if (pictureRevealPanel != null) pictureRevealPanel.SetActive(false);

            currentWordIndex++;
            isTransitioning = false;
            LoadWordRound(currentWordIndex);
        }

        private void ResetSliderToZero()
        {
            if (blendSlideBar != null)
            {
                blendSlideBar.SetInteractable(true);
                blendSlideBar.ResetSlider();
            }
            if (unityBlendSlider != null)
            {
                unityBlendSlider.value = unityBlendSlider.minValue;
                unityBlendSlider.interactable = true;
            }
            else if (slideBarContainer != null)
            {
                Slider s = slideBarContainer.GetComponentInChildren<Slider>(true);
                if (s != null)
                {
                    s.value = s.minValue;
                    s.interactable = true;
                }
            }
        }

        private IEnumerator WobbleBoxesRoutine(float duration)
        {
            if (soundBoxRects == null) yield break;

            // Light up all three sound boxes during blend wobble
            if (soundBoxGlowImages != null)
            {
                for (int i = 0; i < soundBoxGlowImages.Length; i++)
                {
                    if (soundBoxGlowImages[i] != null) soundBoxGlowImages[i].gameObject.SetActive(true);
                }
            }

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
                        float angle = Mathf.Sin((elapsed * 32f) + phaseOffset) * 12f * decay;
                        float scalePop = 1f + (Mathf.Sin((elapsed * 26f) + phaseOffset) * 0.22f * decay);
                        float yBump = Mathf.Sin((elapsed * 22f) + phaseOffset) * 10f * decay;

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

        #region Vowel Triples Phase (Mixed Session Only)

        private void StartVowelTriplesPhase()
        {
            isTriplePhase = true;
            if (blendingPanel != null) blendingPanel.SetActive(false);
            if (pictureRevealPanel != null) pictureRevealPanel.SetActive(false);
            if (vowelTriplesPanel != null) vowelTriplesPanel.SetActive(true);

            currentTripleIndex = 0;
            LoadTriple(0);
        }

        private void LoadTriple(int index)
        {
            if (activityData == null || activityData.vowelTriples == null || index >= activityData.vowelTriples.Length)
            {
                CompleteStop1();
                return;
            }

            currentTripleIndex = index;
            currentTriple = activityData.vowelTriples[index];

            if (triplePromptTMP != null) triplePromptTMP.text = currentTriple.promptText;
            SetDialogue(currentTriple.promptText);

            if (currentTriple.tripleAudioClip != null)
            {
                PlayVoiceClipNonBlocking(currentTriple.tripleAudioClip);
            }

            for (int i = 0; i < tripleChoiceButtons.Length; i++)
            {
                if (tripleChoiceButtons[i] != null)
                {
                    bool active = currentTriple.tripleWords != null && i < currentTriple.tripleWords.Length;
                    tripleChoiceButtons[i].gameObject.SetActive(active);

                    if (active && tripleChoiceTexts != null && i < tripleChoiceTexts.Length && tripleChoiceTexts[i] != null)
                    {
                        tripleChoiceTexts[i].text = currentTriple.tripleWords[i];
                    }
                }
            }

            UpdateProgressUI((34f + index) / 38f);
        }

        private void ReplayTriplePromptAudio()
        {
            if (currentTriple != null && currentTriple.tripleAudioClip != null)
            {
                PlayVoiceClipNonBlocking(currentTriple.tripleAudioClip);
            }
        }

        private void OnTripleChoiceSelected(int index)
        {
            if (isTransitioning || currentTriple == null) return;
            StartCoroutine(CheckTripleChoiceRoutine(index));
        }

        private IEnumerator CheckTripleChoiceRoutine(int index)
        {
            isTransitioning = true;
            bool isCorrect = (index == currentTriple.correctIndex);

            if (isCorrect)
            {
                PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
                TriggerWiggleStarMeter();

                string correctWord = currentTriple.tripleWords[index];
                char vowelChar = correctWord.Length > 1 ? correctWord[1] : 'o';
                SetDialogue($"Yes! {correctWord.ToUpper()}. The {vowelChar} was in the middle.");

                if (activityData != null && activityData.vowelTripleSuccessClip != null)
                {
                    yield return PlayVoiceClip(activityData.vowelTripleSuccessClip);
                }
                else
                {
                    yield return new WaitForSeconds(1.0f);
                }

                currentTripleIndex++;
                isTransitioning = false;
                LoadTriple(currentTripleIndex);
            }
            else
            {
                PlaySFX(activityData != null ? activityData.retryGentleSfx : null);
                SetDialogue("Listen to the middle sound carefully!");
                yield return new WaitForSeconds(1.0f);
                isTransitioning = false;
            }
        }

        #endregion

        private void CompleteStop1()
        {
            StartCoroutine(CompleteStop1Sequence());
        }

        private IEnumerator CompleteStop1Sequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            UpdateProgressUI(1.0f);

            SetDialogue("You can blend every vowel with ease! Tap continue to build new families!");
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
