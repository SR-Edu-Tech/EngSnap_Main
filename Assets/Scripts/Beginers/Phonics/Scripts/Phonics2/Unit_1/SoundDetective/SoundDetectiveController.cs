using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit1
{
    public class SoundDetectiveController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit1";
        [SerializeField] private string topicName = "SoundDetective";

        [Header("ScriptableObject Data")]
        [SerializeField] private SoundDetectiveData detectiveData;

        [Header("UI & Subtitles")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;
        [SerializeField] private Button tapToListenButton;

        [Header("Phoneme Bubbles UI (Phase 1)")]
        [SerializeField] private GameObject phonemePanel;
        [SerializeField] private Image targetWordImage;
        [SerializeField] private TMP_Text targetWordText;
        [SerializeField] private Button[] soundBubbles; // Up to 3 bubbles
        [SerializeField] private Image[] soundBubbleFills;
        [SerializeField] private TMP_Text[] soundBubbleTexts; // Optional text numbers (1, 2, 3) inside bubbles
        [Tooltip("Vertical height offset (Y position) from which phoneme bubbles fall down during slide-in animation.")]
        [SerializeField] private float bubbleFallStartHeightY = 600f;
        [Tooltip("Duration of bubble fall slide-in animation.")]
        [SerializeField] private float bubbleFallDuration = 0.4f;

        [Header("Odd One Out UI (Phase 2)")]
        [SerializeField] private GameObject oddOneOutPanel;
        [SerializeField] private Button[] oddChoiceButtons;
        [SerializeField] private Image[] oddChoiceImages;
        [SerializeField] private TMP_Text[] oddChoiceTexts;

        [Header("Squash The Word UI (Phase 3)")]
        [SerializeField] private GameObject squashPanel;
        [SerializeField] private Button[] squashChoiceButtons;
        [SerializeField] private Image[] squashChoiceImages;
        [SerializeField] private RectTransform[] squashBubblesToAnimate;

        [Header("Progress Ring UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;

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

        [Header("Mascots & Props")]
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject momoHintObject;
        [SerializeField] private GameObject magnifyingGlassProp;

        [Header("SFX Feedback Clips")]
        [SerializeField] private AudioClip bubblePopSfx;
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip retryGentleSfx;
        [SerializeField] private AudioClip squashSfx;

        private int currentRoundIndex = 0;
        private int totalRounds = 11; // 6 Phoneme + 3 OddOneOut + 2 Squash
        private int attemptsCount = 0;
        private int tappedBubblesCount = 0;
        private int targetPhonemeCount = 3;
        private bool isTransitioning = false;
        private bool isActivityCompleted = false;

        private PhonemeWordItem currentPhonemeItem;
        private OddOneOutItem currentOddItem;
        private OralBlendItem currentBlendItem;

        public string UnitID => unitID;
        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
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
            SetupButtonListeners();
            ResetLevel();
        }

        private void OnEnable()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            ResetLevel();
            StartCoroutine(StartIntroSequence());
        }

        private void OnDisable()
        {
            DeactivateMascots();
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
        }

        private void SetupButtonListeners()
        {
            if (soundBubbles != null)
            {
                for (int i = 0; i < soundBubbles.Length; i++)
                {
                    int bIdx = i;
                    if (soundBubbles[i] != null)
                    {
                        soundBubbles[i].onClick.RemoveAllListeners();
                        soundBubbles[i].onClick.AddListener(() => OnBubbleTapped(bIdx));
                    }
                }
            }

            if (oddChoiceButtons != null)
            {
                for (int i = 0; i < oddChoiceButtons.Length; i++)
                {
                    int cIdx = i;
                    if (oddChoiceButtons[i] != null)
                    {
                        oddChoiceButtons[i].onClick.RemoveAllListeners();
                        oddChoiceButtons[i].onClick.AddListener(() => OnOddChoiceSelected(cIdx));
                    }
                }
            }

            if (squashChoiceButtons != null)
            {
                for (int i = 0; i < squashChoiceButtons.Length; i++)
                {
                    int sIdx = i;
                    if (squashChoiceButtons[i] != null)
                    {
                        squashChoiceButtons[i].onClick.RemoveAllListeners();
                        squashChoiceButtons[i].onClick.AddListener(() => OnSquashChoiceSelected(sIdx));
                    }
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

            if (tapToListenButton != null)
            {
                tapToListenButton.onClick.RemoveAllListeners();
                tapToListenButton.onClick.AddListener(OnTapToListenClicked);
            }
        }

        private bool IsAudioPlaying()
        {
            return isTransitioning || (voiceAudioSource != null && voiceAudioSource.isPlaying);
        }

        public void OnTapToListenClicked()
        {
            if (IsAudioPlaying() || isActivityCompleted) return;

            if (tapToListenButton != null)
            {
                PlayWiggleAnimation(tapToListenButton.transform);
            }

            if (currentRoundIndex < 6)
            {
                StartCoroutine(PlayPhonemeAudioSequence());
            }
            else if (currentRoundIndex < 9)
            {
                StartCoroutine(PlayOddPromptSequence());
            }
            else
            {
                StartCoroutine(PlaySquashPromptSequence());
            }
        }

        public void ResetLevel()
        {
            currentRoundIndex = 0;
            attemptsCount = 0;
            tappedBubblesCount = 0;
            isTransitioning = false;
            isActivityCompleted = false;

            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);

            ResetButtonColors();
            HideAllPanels();
            UpdateProgressMeter();
        }

        private void HideAllPanels()
        {
            if (phonemePanel != null) phonemePanel.SetActive(false);
            if (oddOneOutPanel != null) oddOneOutPanel.SetActive(false);
            if (squashPanel != null) squashPanel.SetActive(false);
        }

        private IEnumerator StartIntroSequence()
        {
            isTransitioning = true;
            SetSubtitles("Words are made of tiny sounds. Let's catch them!");

            if (detectiveData != null && detectiveData.introVoiceClip != null)
            {
                PlayVoice(detectiveData.introVoiceClip);
                yield return new WaitForSeconds(detectiveData.introVoiceClip.length + 0.3f);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            isTransitioning = false;
            LoadRound(0);
        }

        private void LoadRound(int roundIdx)
        {
            currentRoundIndex = roundIdx;
            attemptsCount = 0;
            tappedBubblesCount = 0;
            StopHintPulseAnimation();
            if (momoHintObject != null) momoHintObject.SetActive(false);

            UpdateProgressMeter();

            if (roundIdx >= totalRounds)
            {
                StartCoroutine(CompletionSequence());
                return;
            }

            if (roundIdx < 6)
            {
                SetupPhonemeRound(roundIdx);
            }
            else if (roundIdx < 9)
            {
                SetupOddOneOutRound(roundIdx - 6);
            }
            else
            {
                SetupSquashRound(roundIdx - 9);
            }
        }

        private List<Vector3> bubbleOriginalLocalPositions = new List<Vector3>();

        private void CacheBubblePositions()
        {
            if (bubbleOriginalLocalPositions.Count == 0 && soundBubbles != null)
            {
                foreach (var b in soundBubbles)
                {
                    if (b != null) bubbleOriginalLocalPositions.Add(b.transform.localPosition);
                    else bubbleOriginalLocalPositions.Add(Vector3.zero);
                }
            }
        }

        #region Phase 1: Tap Per Sound (Rounds 0-5)
        private void SetupPhonemeRound(int pIdx)
        {
            HideAllPanels();
            if (phonemePanel != null) phonemePanel.SetActive(true);

            if (detectiveData != null && detectiveData.phonemeWords != null && pIdx < detectiveData.phonemeWords.Length)
            {
                currentPhonemeItem = detectiveData.phonemeWords[pIdx];
            }

            if (currentPhonemeItem == null) return;
            targetPhonemeCount = currentPhonemeItem.phonemeCount;

            if (targetWordImage != null) targetWordImage.sprite = currentPhonemeItem.wordSprite;
            if (targetWordText != null) targetWordText.text = currentPhonemeItem.wordStr;

            CacheBubblePositions();

            // Setup bubbles & position off-screen above initially for top slide-in
            if (soundBubbles != null)
            {
                for (int i = 0; i < soundBubbles.Length; i++)
                {
                    if (soundBubbles[i] == null) continue;

                    if (i < targetPhonemeCount)
                    {
                        soundBubbles[i].gameObject.SetActive(true);
                        Vector3 targetPos = (i < bubbleOriginalLocalPositions.Count) ? bubbleOriginalLocalPositions[i] : soundBubbles[i].transform.localPosition;
                        soundBubbles[i].transform.localPosition = targetPos + new Vector3(0f, bubbleFallStartHeightY, 0f);

                        SetButtonInteractableAndDim(soundBubbles[i], null, true);
                        SetButtonColor(soundBubbles[i].gameObject, Color.white);
                        if (soundBubbleFills != null && i < soundBubbleFills.Length && soundBubbleFills[i] != null)
                        {
                            soundBubbleFills[i].gameObject.SetActive(false);
                        }
                        if (soundBubbleTexts != null && i < soundBubbleTexts.Length && soundBubbleTexts[i] != null)
                        {
                            if (currentPhonemeItem != null && currentPhonemeItem.phonemeSounds != null && i < currentPhonemeItem.phonemeSounds.Length && !string.IsNullOrEmpty(currentPhonemeItem.phonemeSounds[i]))
                            {
                                soundBubbleTexts[i].text = currentPhonemeItem.phonemeSounds[i];
                            }
                            else
                            {
                                soundBubbleTexts[i].text = (i + 1).ToString();
                            }
                        }
                    }
                    else
                    {
                        soundBubbles[i].gameObject.SetActive(false);
                    }
                }
            }

            SetSubtitles($"Tap once for every sound you hear in '{currentPhonemeItem.wordStr}'.");
            StartCoroutine(PlayPhonemeAudioSequence());
        }

        private IEnumerator PlayPhonemeAudioSequence()
        {
            isTransitioning = true;
            SetBubblesInteractable(false);

            if (currentPhonemeItem != null && currentPhonemeItem.wholeWordClip != null)
            {
                PlayVoice(currentPhonemeItem.wholeWordClip);
                yield return new WaitForSeconds(currentPhonemeItem.wholeWordClip.length + 0.2f);
            }

            // Staggered top slide-in for each bubble as sounds are introduced
            if (soundBubbles != null)
            {
                for (int i = 0; i < targetPhonemeCount && i < soundBubbles.Length; i++)
                {
                    if (soundBubbles[i] == null) continue;
                    Vector3 targetPos = (i < bubbleOriginalLocalPositions.Count) ? bubbleOriginalLocalPositions[i] : Vector3.zero;
                    StartCoroutine(SlideInBubbleCoroutine(soundBubbles[i].transform, targetPos, bubbleFallDuration));
                    if (bubblePopSfx != null) PlaySfx(bubblePopSfx, 0.7f);
                    PlayWiggleAnimation(soundBubbles[i].transform);
                    yield return new WaitForSeconds(bubbleFallDuration);
                }
            }

            if (currentPhonemeItem != null && currentPhonemeItem.splitWordClip != null)
            {
                PlayVoice(currentPhonemeItem.splitWordClip);
                yield return new WaitForSeconds(currentPhonemeItem.splitWordClip.length + 0.3f);
            }

            // Leo counting wobble sequence matching targetPhonemeCount (2 vs 3 sounds)
            if (currentRoundIndex == 0 && detectiveData != null && detectiveData.demoVoiceClip != null)
            {
                SetSubtitles("One... two... three! Three sounds!");
                PlayVoice(detectiveData.demoVoiceClip);

                float clipLength = detectiveData.demoVoiceClip.length;
                float stepDelay = Mathf.Max(0.4f, clipLength / Mathf.Max(1, targetPhonemeCount));

                for (int i = 0; i < targetPhonemeCount && i < soundBubbles.Length; i++)
                {
                    if (soundBubbles[i] != null)
                    {
                        PlayWiggleAnimation(soundBubbles[i].transform);
                        PlayBounceAnimation(soundBubbles[i].transform);
                    }
                    yield return new WaitForSeconds(stepDelay);
                }
            }
            else
            {
                string countSubtitle = (targetPhonemeCount == 2) ? "One... two! Two sounds!" : "One... two... three! Three sounds!";
                SetSubtitles(countSubtitle);

                for (int i = 0; i < targetPhonemeCount && i < soundBubbles.Length; i++)
                {
                    if (soundBubbles[i] != null)
                    {
                        PlayWiggleAnimation(soundBubbles[i].transform);
                        PlayBounceAnimation(soundBubbles[i].transform);
                    }
                    yield return new WaitForSeconds(0.45f);
                }
            }

            SetBubblesInteractable(true);
            isTransitioning = false;
        }

        private IEnumerator SlideInBubbleCoroutine(Transform tr, Vector3 targetPos, float duration)
        {
            Vector3 startPos = tr.localPosition;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                tr.localPosition = Vector3.Lerp(startPos, targetPos, smoothT);
                yield return null;
            }
            tr.localPosition = targetPos;
        }

        private void OnBubbleTapped(int index)
        {
            if (IsAudioPlaying() || isActivityCompleted) return;

            if (index == tappedBubblesCount)
            {
                tappedBubblesCount++;

                // Play individual phoneme audio clip if assigned, else pop sfx
                if (currentPhonemeItem != null && currentPhonemeItem.phonemeAudioClips != null && index < currentPhonemeItem.phonemeAudioClips.Length && currentPhonemeItem.phonemeAudioClips[index] != null)
                {
                    PlayVoice(currentPhonemeItem.phonemeAudioClips[index]);
                }
                else if (bubblePopSfx != null)
                {
                    PlaySfx(bubblePopSfx);
                }

                // Wobble and bounce button on tap
                if (soundBubbles != null && index < soundBubbles.Length && soundBubbles[index] != null)
                {
                    PlayWiggleAnimation(soundBubbles[index].transform);
                    PlayBounceAnimation(soundBubbles[index].transform);
                    SetButtonColor(soundBubbles[index].gameObject, new Color(0.35f, 0.85f, 1f, 1f));
                }

                if (soundBubbleFills != null && index < soundBubbleFills.Length && soundBubbleFills[index] != null)
                {
                    soundBubbleFills[index].gameObject.SetActive(true);
                    soundBubbleFills[index].color = Color.white; // Ensure full opacity
                    PlayBounceAnimation(soundBubbleFills[index].transform);
                }

                if (tappedBubblesCount >= targetPhonemeCount)
                {
                    StartCoroutine(PhonemeRoundCompleteSequence());
                }
            }
        }

        private IEnumerator PhonemeRoundCompleteSequence()
        {
            isTransitioning = true;
            if (correctChimeSfx != null) PlaySfx(correctChimeSfx);

            string wordStr = currentPhonemeItem != null ? currentPhonemeItem.wordStr : "word";
            SetSubtitles($"You got it! {targetPhonemeCount} sounds in '{wordStr}'!");
            yield return new WaitForSeconds(1.0f);

            isTransitioning = false;
            LoadRound(currentRoundIndex + 1);
        }
        #endregion

        #region Phase 2: Odd One Out (Rounds 6-8)
        private void SetupOddOneOutRound(int oIdx)
        {
            HideAllPanels();
            if (oddOneOutPanel != null) oddOneOutPanel.SetActive(true);

            if (detectiveData != null && detectiveData.oddOneOutRounds != null && oIdx < detectiveData.oddOneOutRounds.Length)
            {
                currentOddItem = detectiveData.oddOneOutRounds[oIdx];
            }

            if (currentOddItem == null) return;

            int totalOddChoices = (currentOddItem.choiceWords != null && currentOddItem.choiceWords.Length > 0)
                ? currentOddItem.choiceWords.Length
                : (currentOddItem.choiceSprites != null && currentOddItem.choiceSprites.Length > 0 ? currentOddItem.choiceSprites.Length : oddChoiceButtons.Length);

            for (int i = 0; i < oddChoiceButtons.Length; i++)
            {
                if (oddChoiceButtons[i] == null) continue;

                if (i < totalOddChoices)
                {
                    oddChoiceButtons[i].gameObject.SetActive(true);
                    SetButtonInteractableAndDim(oddChoiceButtons[i], (oddChoiceImages != null && i < oddChoiceImages.Length) ? oddChoiceImages[i] : null, true);
                    SetButtonColor(oddChoiceButtons[i].gameObject, Color.white);

                    if (oddChoiceImages != null && i < oddChoiceImages.Length && oddChoiceImages[i] != null && currentOddItem.choiceSprites != null && i < currentOddItem.choiceSprites.Length && currentOddItem.choiceSprites[i] != null)
                    {
                        oddChoiceImages[i].sprite = currentOddItem.choiceSprites[i];
                    }
                    if (oddChoiceTexts != null && i < oddChoiceTexts.Length && oddChoiceTexts[i] != null && currentOddItem.choiceWords != null && i < currentOddItem.choiceWords.Length)
                    {
                        oddChoiceTexts[i].text = currentOddItem.choiceWords[i];
                    }
                }
                else
                {
                    oddChoiceButtons[i].gameObject.SetActive(false);
                }
            }

            SetSubtitles(currentOddItem.promptText);
            StartCoroutine(PlayOddPromptSequence());
        }

        private IEnumerator PlayOddPromptSequence()
        {
            isTransitioning = true;
            SetOddChoicesInteractable(false);

            if (currentOddItem != null && currentOddItem.promptClip != null)
            {
                PlayVoice(currentOddItem.promptClip);
                yield return new WaitForSeconds(currentOddItem.promptClip.length + 0.3f);
            }

            SetOddChoicesInteractable(true);
            isTransitioning = false;
        }

        private void OnOddChoiceSelected(int index)
        {
            if (IsAudioPlaying() || isActivityCompleted) return;

            attemptsCount++;
            bool isCorrect = (currentOddItem != null && index == currentOddItem.oddOneOutIndex);
            GameObject tappedObj = (index >= 0 && index < oddChoiceButtons.Length && oddChoiceButtons[index] != null) ? oddChoiceButtons[index].gameObject : null;

            if (tappedObj != null) PlayWiggleAnimation(tappedObj.transform);

            if (isCorrect)
            {
                StartCoroutine(CorrectAnswerSequence(tappedObj));
            }
            else
            {
                StartCoroutine(RetryAnswerSequence(tappedObj));
            }
        }
        #endregion

        #region Phase 3: Squash The Word / Oral Blending (Rounds 9-10)
        private void SetupSquashRound(int sIdx)
        {
            HideAllPanels();
            if (squashPanel != null) squashPanel.SetActive(true);

            if (detectiveData != null && detectiveData.oralBlendRounds != null && sIdx < detectiveData.oralBlendRounds.Length)
            {
                currentBlendItem = detectiveData.oralBlendRounds[sIdx];
            }

            if (currentBlendItem == null) return;

            int totalSquashChoices = (currentBlendItem.choiceSprites != null && currentBlendItem.choiceSprites.Length > 0)
                ? currentBlendItem.choiceSprites.Length
                : squashChoiceButtons.Length;

            for (int i = 0; i < squashChoiceButtons.Length; i++)
            {
                if (squashChoiceButtons[i] == null) continue;

                if (i < totalSquashChoices)
                {
                    squashChoiceButtons[i].gameObject.SetActive(true);
                    SetButtonInteractableAndDim(squashChoiceButtons[i], (squashChoiceImages != null && i < squashChoiceImages.Length) ? squashChoiceImages[i] : null, true);
                    SetButtonColor(squashChoiceButtons[i].gameObject, Color.white);

                    if (squashChoiceImages != null && i < squashChoiceImages.Length && squashChoiceImages[i] != null && currentBlendItem.choiceSprites != null && i < currentBlendItem.choiceSprites.Length && currentBlendItem.choiceSprites[i] != null)
                    {
                        squashChoiceImages[i].sprite = currentBlendItem.choiceSprites[i];
                    }
                }
                else
                {
                    squashChoiceButtons[i].gameObject.SetActive(false);
                }
            }

            SetSubtitles($"{currentBlendItem.splitText}. Squash them together! What is it?");
            StartCoroutine(PlaySquashPromptSequence());
        }

        private IEnumerator PlaySquashPromptSequence()
        {
            isTransitioning = true;
            SetSquashChoicesInteractable(false);

            if (currentBlendItem != null && currentBlendItem.splitClip != null)
            {
                PlayVoice(currentBlendItem.splitClip);
                yield return new WaitForSeconds(currentBlendItem.splitClip.length + 0.3f);
            }

            SetSquashChoicesInteractable(true);
            isTransitioning = false;
        }

        private void OnSquashChoiceSelected(int index)
        {
            if (IsAudioPlaying() || isActivityCompleted) return;

            attemptsCount++;
            bool isCorrect = (currentBlendItem != null && index == currentBlendItem.correctChoiceIndex);
            GameObject tappedObj = (index >= 0 && index < squashChoiceButtons.Length && squashChoiceButtons[index] != null) ? squashChoiceButtons[index].gameObject : null;

            if (tappedObj != null) PlayWiggleAnimation(tappedObj.transform);

            if (isCorrect)
            {
                StartCoroutine(SquashCorrectSequence(tappedObj));
            }
            else
            {
                StartCoroutine(RetryAnswerSequence(tappedObj));
            }
        }

        private IEnumerator SquashCorrectSequence(GameObject tappedObj)
        {
            isTransitioning = true;

            if (tappedObj != null) SetButtonColor(tappedObj, new Color(0.2f, 0.85f, 0.2f, 1f)); // Green for right!
            if (squashSfx != null) PlaySfx(squashSfx);
            if (tappedObj != null) PlayBounceAnimation(tappedObj.transform);

            // Animate bubbles squashing together
            yield return StartCoroutine(AnimateSquashBubbles());

            if (correctChimeSfx != null) PlaySfx(correctChimeSfx);

            if (detectiveData != null && detectiveData.squashSuccessClip != null)
            {
                SetSubtitles($"BAT! You squashed the sounds into a word!");
                PlayVoice(detectiveData.squashSuccessClip);
                yield return new WaitForSeconds(detectiveData.squashSuccessClip.length + 0.3f);
            }
            else
            {
                SetSubtitles($"You squashed the sounds into '{currentBlendItem.wordText}'!");
                yield return new WaitForSeconds(1.0f);
            }

            isTransitioning = false;
            LoadRound(currentRoundIndex + 1);
        }

        private IEnumerator AnimateSquashBubbles()
        {
            if (squashBubblesToAnimate == null || squashBubblesToAnimate.Length == 0) yield break;

            float duration = 0.45f;
            float elapsed = 0f;

            Vector3 centerPos = Vector3.zero;
            List<Vector3> startPositions = new List<Vector3>();

            foreach (var bubble in squashBubblesToAnimate)
            {
                if (bubble != null) startPositions.Add(bubble.localPosition);
                else startPositions.Add(Vector3.zero);
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                for (int i = 0; i < squashBubblesToAnimate.Length; i++)
                {
                    if (squashBubblesToAnimate[i] != null)
                    {
                        squashBubblesToAnimate[i].localPosition = Vector3.Lerp(startPositions[i], centerPos, smoothT);
                    }
                }
                yield return null;
            }
        }
        #endregion

        #region Feedback & Support Fading Sequences
        private IEnumerator CorrectAnswerSequence(GameObject buttonObj)
        {
            isTransitioning = true;

            if (buttonObj != null) SetButtonColor(buttonObj, new Color(0.2f, 0.85f, 0.2f, 1f)); // Green for right!
            if (correctChimeSfx != null) PlaySfx(correctChimeSfx);
            if (buttonObj != null) PlayBounceAnimation(buttonObj.transform);

            SetSubtitles("Awesome job! You found it!");
            yield return new WaitForSeconds(0.8f);

            isTransitioning = false;
            LoadRound(currentRoundIndex + 1);
        }

        private IEnumerator RetryAnswerSequence(GameObject buttonObj)
        {
            isTransitioning = true;

            if (buttonObj != null) SetButtonColor(buttonObj, new Color(0.95f, 0.25f, 0.25f, 1f)); // Red for wrong!
            if (retryGentleSfx != null) PlaySfx(retryGentleSfx);
            if (buttonObj != null) PlayWrongWobbleAnimation(buttonObj);

            if (attemptsCount == 2)
            {
                SetSubtitles("Let's listen again — I will say it slowly.");
                if (detectiveData != null && detectiveData.retryVoiceClip != null)
                {
                    PlayVoice(detectiveData.retryVoiceClip);
                    yield return new WaitForSeconds(detectiveData.retryVoiceClip.length + 0.2f);
                }
            }
            else if (attemptsCount >= 3)
            {
                if (momoHintObject != null) momoHintObject.SetActive(true);
                SetSubtitles("Psst! Tap this one with Leo!");

                GameObject correctObj = GetCurrentCorrectButton();
                if (correctObj != null)
                {
                    StartHintPulseAnimation(correctObj.transform);
                }

                yield return new WaitForSeconds(1.0f);
            }
            else
            {
                SetSubtitles("Try again! Listen carefully.");
                yield return new WaitForSeconds(0.6f);
            }

            if (buttonObj != null) SetButtonColor(buttonObj, Color.white); // Restore white on retry reset
            isTransitioning = false;
        }

        private IEnumerator CompletionSequence()
        {
            isTransitioning = true;
            isActivityCompleted = true;

            HideAllPanels();

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            SetSubtitles("You can hear the sounds inside words. You are a Sound Detective!");

            if (detectiveData != null && detectiveData.completionVoiceClip != null)
            {
                PlayVoice(detectiveData.completionVoiceClip);
                yield return new WaitForSeconds(detectiveData.completionVoiceClip.length + 0.5f);
            }

            if (currentPanel != null) TopicProgressUI.MarkTopicComplete(currentPanel);
            else TopicProgressUI.MarkTopicComplete(gameObject);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            if (continueButton != null) continueButton.SetActive(true);

            isTransitioning = false;
        }
        #endregion

        #region Navigation & Helpers
        public void GoToNextPanel()
        {
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            TopicProgressUI.HideTopicCompletePanel();

            if (isActivityCompleted)
            {
                TopicProgressUI.MarkTopicComplete(gameObject);
            }

            DeactivateMascots();
            ResetLevel();

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

        private void SetBubblesInteractable(bool interactable)
        {
            if (soundBubbles == null) return;
            foreach (var btn in soundBubbles)
            {
                if (btn != null) SetButtonInteractableAndDim(btn, null, interactable);
            }
        }

        private void SetOddChoicesInteractable(bool interactable)
        {
            if (oddChoiceButtons == null) return;
            for (int i = 0; i < oddChoiceButtons.Length; i++)
            {
                if (oddChoiceButtons[i] != null)
                {
                    Image img = (oddChoiceImages != null && i < oddChoiceImages.Length) ? oddChoiceImages[i] : null;
                    SetButtonInteractableAndDim(oddChoiceButtons[i], img, interactable);
                }
            }
        }

        private void SetSquashChoicesInteractable(bool interactable)
        {
            if (squashChoiceButtons == null) return;
            for (int i = 0; i < squashChoiceButtons.Length; i++)
            {
                if (squashChoiceButtons[i] != null)
                {
                    Image img = (squashChoiceImages != null && i < squashChoiceImages.Length) ? squashChoiceImages[i] : null;
                    SetButtonInteractableAndDim(squashChoiceButtons[i], img, interactable);
                }
            }
        }

        private void SetButtonInteractableAndDim(Button btn, Image img, bool interactable)
        {
            if (btn != null) btn.interactable = interactable;
            Image targetImg = img != null ? img : (btn != null ? btn.GetComponent<Image>() : null);
            if (targetImg != null)
            {
                targetImg.color = interactable ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
            }
        }

        private void SetButtonColor(GameObject btnObj, Color col)
        {
            if (btnObj == null) return;
            Image img = btnObj.GetComponent<Image>();
            if (img == null) img = btnObj.GetComponentInChildren<Image>();
            if (img != null)
            {
                img.color = col;
            }
        }

        private void ResetButtonColors()
        {
            if (soundBubbles != null)
            {
                foreach (var btn in soundBubbles) if (btn != null) SetButtonColor(btn.gameObject, Color.white);
            }
            if (oddChoiceButtons != null)
            {
                foreach (var btn in oddChoiceButtons) if (btn != null) SetButtonColor(btn.gameObject, Color.white);
            }
            if (squashChoiceButtons != null)
            {
                foreach (var btn in squashChoiceButtons) if (btn != null) SetButtonColor(btn.gameObject, Color.white);
            }
        }

        private void UpdateProgressMeter()
        {
            if (progressRingFillImage != null)
            {
                progressRingFillImage.fillAmount = (float)currentRoundIndex / totalRounds;
            }
            if (progressText != null)
            {
                progressText.text = $"{currentRoundIndex} / {totalRounds}";
            }
        }

        private void PlayVoice(AudioClip clip)
        {
            if (clip != null && voiceAudioSource != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
            }
        }

        private void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip != null && sfxAudioSource != null)
            {
                sfxAudioSource.PlayOneShot(clip, volume);
            }
        }

        private void SetSubtitles(string text)
        {
            DialogueBoxAutoHider.SetDialogue(dialogueText, text, dialogueCanvasGroup);
        }

        private void PlayBounceAnimation(Transform tr)
        {
            if (tr == null) return;
            StartCoroutine(BounceCoroutine(tr));
        }

        private IEnumerator BounceCoroutine(Transform tr)
        {
            Vector3 orig = tr.localScale;
            float dur = 0.35f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.2f;
                tr.localScale = orig * scale;
                yield return null;
            }
            tr.localScale = orig;
        }

        private void PlayWiggleAnimation(Transform tr)
        {
            if (tr == null || !tr.gameObject.activeInHierarchy) return;
            StartCoroutine(WiggleCoroutine(tr));
        }

        private IEnumerator WiggleCoroutine(Transform tr)
        {
            Vector3 origScale = tr.localScale;
            Quaternion origRot = tr.localRotation;
            float dur = 0.45f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float rot = Mathf.Sin(t * Mathf.PI * 5f) * 15f * (1f - t);
                float scalePulse = 1f + Mathf.Sin(t * Mathf.PI * 4f) * 0.15f * (1f - t);
                tr.localRotation = origRot * Quaternion.Euler(0, 0, rot);
                tr.localScale = origScale * scalePulse;
                yield return null;
            }
            tr.localScale = origScale;
            tr.localRotation = origRot;
        }

        private void PlayWrongWobbleAnimation(GameObject obj)
        {
            if (obj == null || !obj.activeInHierarchy) return;
            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                StartCoroutine(WrongWobbleCoroutine(rect));
            }
            else
            {
                StartCoroutine(WiggleCoroutine(obj.transform));
            }
        }

        private IEnumerator WrongWobbleCoroutine(RectTransform rect)
        {
            Vector2 originalPos = rect.anchoredPosition;
            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                // Dampened rapid horizontal shake/wobble for wrong answers
                float offset = Mathf.Sin(t * Mathf.PI * 10f) * 26f * (1f - t);
                rect.anchoredPosition = originalPos + new Vector2(offset, 0f);

                yield return null;
            }

            rect.anchoredPosition = originalPos;
        }

        private Coroutine hintPulseCoroutine;
        private Transform hintPulsingTransform;
        private Vector3 hintOriginalScale = Vector3.one;

        private void StartHintPulseAnimation(Transform tr)
        {
            StopHintPulseAnimation();
            if (tr == null || !tr.gameObject.activeInHierarchy) return;
            hintPulsingTransform = tr;
            hintOriginalScale = tr.localScale;
            hintPulseCoroutine = StartCoroutine(HintPulseCoroutine(tr, hintOriginalScale));
        }

        private void StopHintPulseAnimation()
        {
            if (hintPulseCoroutine != null)
            {
                StopCoroutine(hintPulseCoroutine);
                hintPulseCoroutine = null;
            }
            if (hintPulsingTransform != null)
            {
                hintPulsingTransform.localScale = hintOriginalScale;
                hintPulsingTransform = null;
            }
        }

        private IEnumerator HintPulseCoroutine(Transform tr, Vector3 baseScale)
        {
            float pulseSpeed = 4.0f;
            float pulseAmount = 0.22f; // Smooth pop up and down scale pulse between 1.0x and 1.22x

            while (tr != null && tr.gameObject.activeInHierarchy && momoHintObject != null && momoHintObject.activeInHierarchy)
            {
                float sine = Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed));
                tr.localScale = baseScale * (1f + sine * pulseAmount);
                yield return null;
            }

            if (tr != null) tr.localScale = baseScale;
            hintPulsingTransform = null;
        }

        private GameObject GetCurrentCorrectButton()
        {
            if (currentRoundIndex < 6)
            {
                if (soundBubbles != null && tappedBubblesCount >= 0 && tappedBubblesCount < soundBubbles.Length)
                {
                    return soundBubbles[tappedBubblesCount] != null ? soundBubbles[tappedBubblesCount].gameObject : null;
                }
            }
            else if (currentRoundIndex < 9)
            {
                if (currentOddItem != null && oddChoiceButtons != null && currentOddItem.oddOneOutIndex >= 0 && currentOddItem.oddOneOutIndex < oddChoiceButtons.Length)
                {
                    return oddChoiceButtons[currentOddItem.oddOneOutIndex] != null ? oddChoiceButtons[currentOddItem.oddOneOutIndex].gameObject : null;
                }
            }
            else
            {
                if (currentBlendItem != null && squashChoiceButtons != null && currentBlendItem.correctChoiceIndex >= 0 && currentBlendItem.correctChoiceIndex < squashChoiceButtons.Length)
                {
                    return squashChoiceButtons[currentBlendItem.correctChoiceIndex] != null ? squashChoiceButtons[currentBlendItem.correctChoiceIndex].gameObject : null;
                }
            }
            return null;
        }
        #endregion
    }
}
