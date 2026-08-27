using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit1
{
    public class SoundPicturesController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit1";
        [SerializeField] private string topicName = "SoundPictures";

        [Header("ScriptableObject Data")]
        [SerializeField] private SoundPicturesData picturesData;

        [Header("UI & Subtitles")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Sound Camera UI (Phase 1)")]
        [SerializeField] private GameObject cameraPanel;
        [SerializeField] private Image cameraPhotoFrameImage; // Single Photo Frame Image UI
        [SerializeField] private GameObject cameraFlashOverlay;
        [SerializeField] private Button cameraSnapButton; // Interactive camera shutter button

        [Header("The Scoop Game UI (Phase 2)")]
        [SerializeField] private GameObject scoopPanel;
        [SerializeField] private TMP_Text scoopWordText;
        [SerializeField] private Image scoopWordImage;
        [SerializeField] private Button addScoopButton;
        [SerializeField] private GameObject[] iceCreamScoops; // Up to 5 stackable scoops
        [SerializeField] private GameObject holdingHandsTileObject; // Visual graphic for joined letters (oo, sh)

        [Header("Star Round UI (Phase 3)")]
        [SerializeField] private GameObject starRoundPanel;
        [SerializeField] private TMP_Text starQuestionText;
        [SerializeField] private Image starPromptImage;
        [SerializeField] private Button[] starChoiceButtons;
        [SerializeField] private Image[] starChoiceImages;
        [SerializeField] private TMP_Text[] starChoiceTexts;
        [SerializeField] private Image starMeterFillImage;
        [SerializeField] private TMP_Text starMeterCountText;
        [SerializeField] private GameObject soundDetectiveBadgeObject;

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
        [SerializeField] private GameObject momoMascotObject;
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject taraMascotObject;
        [SerializeField] private GameObject momoHintObject;

        [Header("SFX Feedback Clips")]
        [SerializeField] private AudioClip cameraShutterSfx;
        [SerializeField] private AudioClip scoopAddSfx;
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip retryGentleSfx;
        [SerializeField] private AudioClip starJingleSfx;

        private int currentPhase = 1; // 1 = Camera, 2 = Scoop, 3 = Star Round
        private int currentCameraIndex = 0;
        private int currentScoopWordIndex = 0;
        private int currentScoopsAdded = 0;
        private int currentStarChallengeIndex = 0;
        private int totalStarChallenges = 6;
        private int correctStarAnswers = 0;
        private int attemptsCount = 0;

        private bool isTransitioning = false;
        private bool isActivityCompleted = false;

        private ScoopWordItem currentScoopItem;
        private StarRoundChallenge currentStarItem;

        public string UnitID => unitID;
        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
            if (taraMascotObject != null) taraMascotObject.SetActive(true);
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
        }

        private void OnEnable()
        {
            if (taraMascotObject != null) taraMascotObject.SetActive(true);
            EnsureAudioSources();
            SetupButtonListeners();
            ResetLevel();
            if (cameraPanel != null) cameraPanel.SetActive(true);
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
            if (taraMascotObject != null) taraMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
        }

        private bool hasSnappedCameraPhoto = false;

        private void OnCameraSnapClicked()
        {
            hasSnappedCameraPhoto = true;
        }

        private void SetupButtonListeners()
        {
            if (cameraSnapButton != null)
            {
                cameraSnapButton.onClick.RemoveAllListeners();
                cameraSnapButton.onClick.AddListener(OnCameraSnapClicked);
            }

            if (addScoopButton != null)
            {
                addScoopButton.onClick.RemoveAllListeners();
                addScoopButton.onClick.AddListener(OnAddScoopClicked);
            }

            if (starChoiceButtons != null)
            {
                for (int i = 0; i < starChoiceButtons.Length; i++)
                {
                    int sIdx = i;
                    if (starChoiceButtons[i] != null)
                    {
                        starChoiceButtons[i].onClick.RemoveAllListeners();
                        starChoiceButtons[i].onClick.AddListener(() => OnStarChoiceSelected(sIdx));
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
        }

        public void ResetLevel()
        {
            currentPhase = 1;
            currentCameraIndex = 0;
            currentScoopWordIndex = 0;
            currentScoopsAdded = 0;
            currentStarChallengeIndex = 0;
            correctStarAnswers = 0;
            attemptsCount = 0;
            hasSnappedCameraPhoto = false;

            isTransitioning = false;
            isActivityCompleted = false;

            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (soundDetectiveBadgeObject != null) soundDetectiveBadgeObject.SetActive(false);

            HideAllPanels();
            UpdateStarMeterUI();
        }

        private void HideAllPanels()
        {
            if (cameraPanel != null) cameraPanel.SetActive(false);
            if (scoopPanel != null) scoopPanel.SetActive(false);
            if (starRoundPanel != null) starRoundPanel.SetActive(false);
        }

        private IEnumerator StartIntroSequence()
        {
            isTransitioning = true;
            if (cameraPanel != null) cameraPanel.SetActive(true);

            SetSubtitles("I have a Sound Camera! Watch what happens when Leo makes a sound.");

            if (picturesData != null && picturesData.cameraIntroClip != null)
            {
                PlayVoice(picturesData.cameraIntroClip);
                yield return new WaitForSeconds(picturesData.cameraIntroClip.length + 0.3f);
            }
            else
            {
                yield return new WaitForSeconds(1.0f);
            }

            isTransitioning = false;
            StartCoroutine(RunSoundCameraPhase());
        }

        #region Phase 1: Sound Camera
        private IEnumerator RunSoundCameraPhase()
        {
            isTransitioning = true;
            currentPhase = 1;

            if (cameraPanel != null) cameraPanel.SetActive(true);

            if (picturesData == null || picturesData.cameraPhotos == null) yield break;

            if (cameraPhotoFrameImage != null) cameraPhotoFrameImage.gameObject.SetActive(false);

            for (int i = 0; i < picturesData.cameraPhotos.Length; i++)
            {
                SoundCameraItem photo = picturesData.cameraPhotos[i];
                if (photo == null) continue;

                currentCameraIndex = i;

                SetSubtitles($"Listen to the sound {photo.soundStr}! Tap the camera button to snap a photo!");
                if (photo.soundAudioClip != null)
                {
                    PlayVoice(photo.soundAudioClip);
                }

                // Enable camera button for kid interaction
                hasSnappedCameraPhoto = false;
                if (cameraSnapButton != null)
                {
                    cameraSnapButton.interactable = true;
                    StartHintPulseAnimation(cameraSnapButton.transform);
                }

                yield return new WaitUntil(() => hasSnappedCameraPhoto);

                if (cameraSnapButton != null)
                {
                    cameraSnapButton.interactable = false;
                    StopHintPulseAnimation();
                }

                // Flash camera shutter
                if (cameraShutterSfx != null) PlaySfx(cameraShutterSfx);
                if (cameraFlashOverlay != null)
                {
                    cameraFlashOverlay.SetActive(true);
                    yield return new WaitForSeconds(0.15f);
                    cameraFlashOverlay.SetActive(false);
                }

                // Show developed primary photo
                if (cameraPhotoFrameImage != null && photo.letterPhotoSprite != null)
                {
                    cameraPhotoFrameImage.sprite = photo.letterPhotoSprite;
                    cameraPhotoFrameImage.gameObject.SetActive(true);
                    PlayBounceAnimation(cameraPhotoFrameImage.transform);
                }

                SetSubtitles($"Click! Look — a picture of the sound {photo.soundStr}. We call it the letter {photo.letterChar}!");
                if (photo.voiceDescriptionClip != null)
                {
                    PlayVoice(photo.voiceDescriptionClip);
                    yield return new WaitForSeconds(photo.voiceDescriptionClip.length + 0.3f);
                }
                else
                {
                    yield return new WaitForSeconds(1.2f);
                }
            }

            // Double Grapheme (c & k) Demonstration
            SetSubtitles("Same sound — but two different pictures! c and k. Tap the camera to reveal!");
            hasSnappedCameraPhoto = false;
            if (cameraSnapButton != null)
            {
                cameraSnapButton.interactable = true;
                StartHintPulseAnimation(cameraSnapButton.transform);
            }

            yield return new WaitUntil(() => hasSnappedCameraPhoto);

            if (cameraSnapButton != null)
            {
                cameraSnapButton.interactable = false;
                StopHintPulseAnimation();
            }

            if (cameraShutterSfx != null) PlaySfx(cameraShutterSfx);
            if (cameraFlashOverlay != null)
            {
                cameraFlashOverlay.SetActive(true);
                yield return new WaitForSeconds(0.15f);
                cameraFlashOverlay.SetActive(false);
            }

            if (cameraPhotoFrameImage != null && picturesData != null && picturesData.doubleGraphemePhotoSprite != null)
            {
                cameraPhotoFrameImage.sprite = picturesData.doubleGraphemePhotoSprite;
                cameraPhotoFrameImage.gameObject.SetActive(true);
                PlayBounceAnimation(cameraPhotoFrameImage.transform);
            }

            SetSubtitles("Same sound — but two different pictures! c and k. Sneaky!");
            if (picturesData != null && picturesData.doubleGraphemeClip != null)
            {
                PlayVoice(picturesData.doubleGraphemeClip);
                yield return new WaitForSeconds(picturesData.doubleGraphemeClip.length + 0.4f);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            isTransitioning = false;
            StartScoopGamePhase();
        }
        #endregion

        #region Phase 2: The Scoop Game
        private void StartScoopGamePhase()
        {
            currentPhase = 2;
            currentScoopWordIndex = 0;
            HideAllPanels();
            if (scoopPanel != null) scoopPanel.SetActive(true);

            LoadScoopWord(0);
        }

        private void LoadScoopWord(int index)
        {
            currentScoopWordIndex = index;
            currentScoopsAdded = 0;

            if (picturesData == null || picturesData.scoopWords == null || index >= picturesData.scoopWords.Length)
            {
                StartStarRoundPhase();
                return;
            }

            currentScoopItem = picturesData.scoopWords[index];
            if (currentScoopItem == null) return;

            if (scoopWordText != null) scoopWordText.text = currentScoopItem.wordStr;
            if (scoopWordImage != null && currentScoopItem.wordSprite != null) scoopWordImage.sprite = currentScoopItem.wordSprite;
            if (holdingHandsTileObject != null) holdingHandsTileObject.SetActive(false);

            ResetScoopVisuals();

            SetSubtitles($"Listen: {currentScoopItem.splitPhonemesStr}. Add one scoop of ice cream for every sound!");
            StartCoroutine(PlayScoopWordSequence());
        }

        private IEnumerator PlayScoopWordSequence()
        {
            isTransitioning = true;
            if (addScoopButton != null) addScoopButton.interactable = false;

            if (currentScoopItem != null && currentScoopItem.splitClip != null)
            {
                PlayVoice(currentScoopItem.splitClip);
                yield return new WaitForSeconds(currentScoopItem.splitClip.length + 0.3f);
            }

            if (picturesData != null && picturesData.scoopInstructionClip != null && currentScoopWordIndex == 0)
            {
                PlayVoice(picturesData.scoopInstructionClip);
                yield return new WaitForSeconds(picturesData.scoopInstructionClip.length + 0.2f);
            }

            if (addScoopButton != null) addScoopButton.interactable = true;
            isTransitioning = false;
        }

        private bool IsAudioPlaying()
        {
            return isTransitioning || (voiceAudioSource != null && voiceAudioSource.isPlaying);
        }

        private void OnAddScoopClicked()
        {
            if (IsAudioPlaying() || currentScoopItem == null) return;

            if (currentScoopsAdded < currentScoopItem.phonemeCount)
            {
                if (iceCreamScoops != null && currentScoopsAdded < iceCreamScoops.Length && iceCreamScoops[currentScoopsAdded] != null)
                {
                    iceCreamScoops[currentScoopsAdded].SetActive(true);
                    PlayBounceAnimation(iceCreamScoops[currentScoopsAdded].transform);
                }

                currentScoopsAdded++;
                if (scoopAddSfx != null) PlaySfx(scoopAddSfx);

                if (currentScoopsAdded >= currentScoopItem.phonemeCount)
                {
                    StartCoroutine(ScoopWordCompleteSequence());
                }
            }
        }

        private IEnumerator ScoopWordCompleteSequence()
        {
            isTransitioning = true;
            if (addScoopButton != null) addScoopButton.interactable = false;

            if (holdingHandsTileObject != null) holdingHandsTileObject.SetActive(true);
            if (correctChimeSfx != null) PlaySfx(correctChimeSfx);

            if (currentScoopWordIndex == 0 && picturesData != null && picturesData.scoopHoldingHandsClip != null)
            {
                SetSubtitles($"Four sounds! But s, c, o, o, p has 5 letters! The two o's are holding hands and sharing ONE sound!");
                PlayVoice(picturesData.scoopHoldingHandsClip);
                yield return new WaitForSeconds(picturesData.scoopHoldingHandsClip.length + 0.3f);
            }
            else
            {
                SetSubtitles($"Great job! {currentScoopItem.wordStr} has {currentScoopItem.phonemeCount} sounds and {currentScoopItem.letterCount} letters!");
                yield return new WaitForSeconds(1.2f);
            }

            isTransitioning = false;
            LoadScoopWord(currentScoopWordIndex + 1);
        }

        private void ResetScoopVisuals()
        {
            if (iceCreamScoops == null) return;
            foreach (var scoop in iceCreamScoops)
            {
                if (scoop != null) scoop.SetActive(false);
            }
        }
        #endregion

        #region Phase 3: Star Round (Tara The Tiger)
        private void StartStarRoundPhase()
        {
            currentPhase = 3;
            currentStarChallengeIndex = 0;
            correctStarAnswers = 0;

            HideAllPanels();
            if (starRoundPanel != null) starRoundPanel.SetActive(true);

            StartCoroutine(StartStarRoundIntroSequence());
        }

        private IEnumerator StartStarRoundIntroSequence()
        {
            isTransitioning = true;
            SetSubtitles("My turn! Six quick challenges. Ready? Roar!");

            if (picturesData != null && picturesData.taraStarRoundOpenerClip != null)
            {
                PlayVoice(picturesData.taraStarRoundOpenerClip);
                yield return new WaitForSeconds(picturesData.taraStarRoundOpenerClip.length + 0.3f);
            }

            isTransitioning = false;
            LoadStarChallenge(0);
        }

        private void LoadStarChallenge(int cIdx)
        {
            currentStarChallengeIndex = cIdx;
            attemptsCount = 0;
            StopHintPulseAnimation();
            if (momoHintObject != null) momoHintObject.SetActive(false);

            UpdateStarMeterUI();

            if (picturesData == null || picturesData.starChallenges == null || cIdx >= picturesData.starChallenges.Length)
            {
                StartCoroutine(CompletionSequence());
                return;
            }

            currentStarItem = picturesData.starChallenges[cIdx];
            if (currentStarItem == null) return;

            if (starQuestionText != null) starQuestionText.text = currentStarItem.questionPrompt;
            if (starPromptImage != null)
            {
                if (currentStarItem.promptSprite != null)
                {
                    starPromptImage.sprite = currentStarItem.promptSprite;
                    starPromptImage.gameObject.SetActive(true);
                }
                else
                {
                    starPromptImage.gameObject.SetActive(false);
                }
            }

            for (int i = 0; i < starChoiceButtons.Length; i++)
            {
                if (starChoiceButtons[i] == null) continue;

                // Reset button background color to default
                Image btnImg = starChoiceButtons[i].GetComponent<Image>();
                if (btnImg != null) btnImg.color = Color.white;

                bool hasSprite = (currentStarItem.choiceSprites != null && i < currentStarItem.choiceSprites.Length && currentStarItem.choiceSprites[i] != null);
                bool hasWord = (currentStarItem.choiceWords != null && i < currentStarItem.choiceWords.Length && !string.IsNullOrEmpty(currentStarItem.choiceWords[i]));

                if (hasSprite || hasWord)
                {
                    starChoiceButtons[i].gameObject.SetActive(true);
                    starChoiceButtons[i].interactable = true;

                    if (starChoiceImages != null && i < starChoiceImages.Length && starChoiceImages[i] != null)
                    {
                        if (hasSprite)
                        {
                            starChoiceImages[i].sprite = currentStarItem.choiceSprites[i];
                            starChoiceImages[i].gameObject.SetActive(true);
                        }
                        else starChoiceImages[i].gameObject.SetActive(false);
                    }

                    if (starChoiceTexts != null && i < starChoiceTexts.Length && starChoiceTexts[i] != null)
                    {
                        if (hasWord)
                        {
                            starChoiceTexts[i].text = currentStarItem.choiceWords[i];
                            starChoiceTexts[i].gameObject.SetActive(true);
                        }
                        else starChoiceTexts[i].gameObject.SetActive(false);
                    }
                }
                else
                {
                    starChoiceButtons[i].gameObject.SetActive(false);
                }
            }

            SetSubtitles(currentStarItem.questionPrompt);
            StartCoroutine(PlayStarPromptSequence());
        }

        private IEnumerator PlayStarPromptSequence()
        {
            isTransitioning = true;
            SetStarChoicesInteractable(false);

            if (currentStarItem != null && currentStarItem.promptClip != null)
            {
                PlayVoice(currentStarItem.promptClip);
                yield return new WaitForSeconds(currentStarItem.promptClip.length + 0.2f);
            }

            SetStarChoicesInteractable(true);
            isTransitioning = false;
        }

        private void SetStarChoicesInteractable(bool interactable)
        {
            if (starChoiceButtons == null) return;
            foreach (var btn in starChoiceButtons)
            {
                if (btn != null) btn.interactable = interactable;
            }
        }

        private void OnStarChoiceSelected(int index)
        {
            if (IsAudioPlaying() || isActivityCompleted) return;

            attemptsCount++;
            bool isCorrect = (currentStarItem != null && index == currentStarItem.correctChoiceIndex);
            GameObject tappedObj = (index >= 0 && index < starChoiceButtons.Length && starChoiceButtons[index] != null) ? starChoiceButtons[index].gameObject : null;

            if (isCorrect)
            {
                StartCoroutine(CorrectStarSequence(tappedObj));
            }
            else
            {
                StartCoroutine(RetryStarSequence(tappedObj));
            }
        }

        private IEnumerator CorrectStarSequence(GameObject tappedObj)
        {
            isTransitioning = true;

            if (tappedObj != null)
            {
                Image tappedImg = tappedObj.GetComponent<Image>();
                if (tappedImg != null) tappedImg.color = new Color(0.35f, 0.88f, 0.35f, 1f); // Vibrant Green Highlight!
                PlayWiggleAnimation(tappedObj.transform); // Wiggle on click!
            }

            if (correctChimeSfx != null) PlaySfx(correctChimeSfx);

            correctStarAnswers++;
            UpdateStarMeterUI();

            SetSubtitles("Correct! Roar!");
            yield return new WaitForSeconds(0.8f);

            isTransitioning = false;
            LoadStarChallenge(currentStarChallengeIndex + 1);
        }

        private IEnumerator RetryStarSequence(GameObject tappedObj)
        {
            isTransitioning = true;

            if (tappedObj != null)
            {
                Image tappedImg = tappedObj.GetComponent<Image>();
                if (tappedImg != null) tappedImg.color = new Color(1f, 0.55f, 0.55f, 1f); // Soft Red for wrong choice
                PlayWiggleAnimation(tappedObj.transform); // Wiggle on click!
            }

            if (retryGentleSfx != null) PlaySfx(retryGentleSfx);

            if (attemptsCount >= 2)
            {
                if (momoHintObject != null) momoHintObject.SetActive(true);
                if (momoMascotObject != null) momoMascotObject.SetActive(true); // Activate Momo Mascot on hint!

                SetSubtitles("Almost! Let's do that one together.");

                GameObject correctObj = GetCurrentCorrectButton();
                if (correctObj != null)
                {
                    Image correctImg = correctObj.GetComponent<Image>();
                    if (correctImg != null) correctImg.color = new Color(0.35f, 0.88f, 0.35f, 1f); // Green highlight for correct answer!
                    StartHintPulseAnimation(correctObj.transform); // Pop up and down bounce without increasing size!
                }

                if (picturesData != null && picturesData.taraRetryClip != null)
                {
                    PlayVoice(picturesData.taraRetryClip);
                    yield return new WaitForSeconds(picturesData.taraRetryClip.length + 0.3f);
                }
            }
            else
            {
                SetSubtitles("Listen once more! Try again.");
                yield return new WaitForSeconds(0.6f);
            }

            isTransitioning = false;
        }

        private void UpdateStarMeterUI()
        {
            if (starMeterCountText != null)
            {
                starMeterCountText.text = $"{correctStarAnswers} / {totalStarChallenges}";
            }

            if (starMeterFillImage != null && totalStarChallenges > 0)
            {
                starMeterFillImage.fillAmount = (float)correctStarAnswers / totalStarChallenges;
            }
        }
        #endregion

        #region Completion & Navigation
        private IEnumerator CompletionSequence()
        {
            isTransitioning = true;
            isActivityCompleted = true;

            HideAllPanels();

            if (soundDetectiveBadgeObject != null) soundDetectiveBadgeObject.SetActive(true);
            if (starJingleSfx != null) PlaySfx(starJingleSfx);
            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            SetSubtitles("You listened. You found the sounds. You are a SOUND DETECTIVE! Unit Two is open!");

            if (picturesData != null && picturesData.completionBadgeVoiceClip != null)
            {
                PlayVoice(picturesData.completionBadgeVoiceClip);
                yield return new WaitForSeconds(picturesData.completionBadgeVoiceClip.length + 0.5f);
            }

            if (currentPanel != null) TopicProgressUI.MarkTopicComplete(currentPanel);
            else TopicProgressUI.MarkTopicComplete(gameObject);

            TopicProgressUI.MarkTopicComplete(unitID, topicName);

            isTransitioning = false;
        }

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
            if (tr == null) return;
            StartCoroutine(WiggleCoroutine(tr));
        }

        private IEnumerator WiggleCoroutine(Transform tr)
        {
            Vector3 origScale = tr.localScale;
            Quaternion origRot = tr.localRotation;
            float dur = 0.3f;
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dur);
                float rot = Mathf.Sin(t * Mathf.PI * 3f) * 6f * (1f - t);
                tr.localRotation = origRot * Quaternion.Euler(0, 0, rot);
                yield return null;
            }
            tr.localScale = origScale;
            tr.localRotation = origRot;
        }

        private Coroutine hintPulseCoroutine;
        private Transform hintPulsingTransform;
        private Vector3 hintOriginalScale = Vector3.one;
        private Vector3 hintOriginalPos = Vector3.zero;

        private void StartHintPulseAnimation(Transform tr)
        {
            StopHintPulseAnimation();
            if (tr == null || !tr.gameObject.activeInHierarchy) return;
            hintPulsingTransform = tr;
            hintOriginalScale = tr.localScale;
            hintOriginalPos = tr.localPosition;
            hintPulseCoroutine = StartCoroutine(HintPopUpDownCoroutine(tr, hintOriginalPos, hintOriginalScale));
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
                hintPulsingTransform.localPosition = hintOriginalPos;
                hintPulsingTransform = null;
            }
        }

        private IEnumerator HintPopUpDownCoroutine(Transform tr, Vector3 basePos, Vector3 baseScale)
        {
            float moveSpeed = 6.0f;
            float popHeight = 14.0f;

            while (tr != null && tr.gameObject.activeInHierarchy)
            {
                float bounceY = Mathf.Abs(Mathf.Sin(Time.time * moveSpeed)) * popHeight;
                tr.localPosition = basePos + new Vector3(0f, bounceY, 0f);
                tr.localScale = baseScale; // Never increase size beyond baseScale!
                yield return null;
            }

            if (tr != null)
            {
                tr.localPosition = basePos;
                tr.localScale = baseScale;
            }
            hintPulsingTransform = null;
        }

        private GameObject GetCurrentCorrectButton()
        {
            if (currentStarItem != null && starChoiceButtons != null && currentStarItem.correctChoiceIndex >= 0 && currentStarItem.correctChoiceIndex < starChoiceButtons.Length)
            {
                return starChoiceButtons[currentStarItem.correctChoiceIndex] != null ? starChoiceButtons[currentStarItem.correctChoiceIndex].gameObject : null;
            }
            return null;
        }
        #endregion
    }
}
