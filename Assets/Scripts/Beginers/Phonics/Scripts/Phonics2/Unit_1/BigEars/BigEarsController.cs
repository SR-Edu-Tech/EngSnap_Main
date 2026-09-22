using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit1
{
    public class BigEarsController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit1";
        [SerializeField] private string topicName = "BigEars";

        [Header("ScriptableObject Data")]
        [SerializeField] private BigEarsData soundData;

        [Header("UI & Subtitles")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;
        [SerializeField] private Button tapToListenButton;

        [Header("Choice Buttons (Sound Lotto & Order)")]
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private Image[] choiceImages;

        [Header("Loud & Soft UI Panel")]
        [SerializeField] private GameObject loudSoftPanel;
        [SerializeField] private Button elephantLoudButton;
        [SerializeField] private Button mouseSoftButton;

        [Header("Which Came First Order Containers")]
        [SerializeField] private GameObject orderPanelContainer;
        [SerializeField] private Button firstOrderButton;
        [SerializeField] private Button secondOrderButton;
        [SerializeField] private Image firstOrderImage;
        [SerializeField] private Image secondOrderImage;

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

        [Header("Mascot & Hints")]
        [SerializeField] private GameObject gigiMascotObject;
        [SerializeField] private GameObject momoHintObject;

        [Header("SFX Feedback Clips")]
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip retryGentleSfx;
        [SerializeField] private AudioClip stickerPopSfx;

        private int currentRoundIndex = 0;
        private int totalRounds = 8;
        private int attemptsCount = 0;
        private bool isTransitioning = false;
        private bool isActivityCompleted = false;

        private SoundLottoItem currentTargetItem;
        private SoundLottoItem secondTargetItem;
        private int correctChoiceIndex = 0;
        private bool isLoudTarget = true;

        public string UnitID => unitID;
        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
            if (gigiMascotObject != null) gigiMascotObject.SetActive(true);
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
            if (gigiMascotObject != null) gigiMascotObject.SetActive(true);
            ResetLevel();
            StartCoroutine(StartIntroSequence());
        }

        private void OnDisable()
        {
            DeactivateMascots();
        }

        public void DeactivateMascots()
        {
            if (gigiMascotObject != null) gigiMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
        }

        private void SetupButtonListeners()
        {
            if (choiceButtons != null)
            {
                for (int i = 0; i < choiceButtons.Length; i++)
                {
                    int btnIdx = i;
                    if (choiceButtons[i] != null)
                    {
                        choiceButtons[i].onClick.RemoveAllListeners();
                        choiceButtons[i].onClick.AddListener(() => OnChoiceSelected(btnIdx));
                    }
                }
            }

            if (elephantLoudButton != null)
            {
                elephantLoudButton.onClick.RemoveAllListeners();
                elephantLoudButton.onClick.AddListener(() => OnLoudSoftSelected(true));
            }

            if (mouseSoftButton != null)
            {
                mouseSoftButton.onClick.RemoveAllListeners();
                mouseSoftButton.onClick.AddListener(() => OnLoudSoftSelected(false));
            }

            if (firstOrderButton != null)
            {
                firstOrderButton.onClick.RemoveAllListeners();
                firstOrderButton.onClick.AddListener(() => OnWhichFirstSelected(0));
            }

            if (secondOrderButton != null)
            {
                secondOrderButton.onClick.RemoveAllListeners();
                secondOrderButton.onClick.AddListener(() => OnWhichFirstSelected(1));
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

        public void ResetLevel()
        {
            currentRoundIndex = 0;
            attemptsCount = 0;
            isTransitioning = false;
            isActivityCompleted = false;

            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);

            HideAllRoundPanels();
            UpdateProgressMeter();
        }

        private void HideAllRoundPanels()
        {
            ResetButtonColors();

            if (choiceButtons != null)
            {
                foreach (var btn in choiceButtons)
                {
                    if (btn != null) btn.gameObject.SetActive(false);
                }
            }

            if (loudSoftPanel != null) loudSoftPanel.SetActive(false);
            if (orderPanelContainer != null) orderPanelContainer.SetActive(false);
        }

        private IEnumerator StartIntroSequence()
        {
            isTransitioning = true;
            SetSubtitles("Shhh… Let's use our big listening ears. What can you hear?");

            if (soundData != null && soundData.introVoiceClip != null)
            {
                PlayVoice(soundData.introVoiceClip);
                yield return new WaitForSeconds(soundData.introVoiceClip.length + 0.3f);
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
            StopHintPulseAnimation();
            if (momoHintObject != null) momoHintObject.SetActive(false);

            UpdateProgressMeter();

            if (roundIdx >= totalRounds)
            {
                StartCoroutine(CompletionSequence());
                return;
            }

            if (roundIdx < 4)
            {
                SetupSoundLottoRound(roundIdx);
            }
            else if (roundIdx < 6)
            {
                SetupLoudAndSoftRound(roundIdx - 4);
            }
            else
            {
                SetupWhichCameFirstRound(roundIdx - 6);
            }
        }

        #region Sound Lotto (Rounds 0-3)
        private void SetupSoundLottoRound(int lottoIdx)
        {
            HideAllRoundPanels();
            if (soundData == null || soundData.lottoItems == null || soundData.lottoItems.Length < 3) return;

            List<SoundLottoItem> pool = new List<SoundLottoItem>(soundData.lottoItems);
            currentTargetItem = pool[lottoIdx % pool.Count];
            pool.Remove(currentTargetItem);

            SoundLottoItem distractor1 = pool[Random.Range(0, pool.Count)];
            pool.Remove(distractor1);
            SoundLottoItem distractor2 = pool[Random.Range(0, pool.Count)];

            List<SoundLottoItem> choices = new List<SoundLottoItem> { currentTargetItem, distractor1, distractor2 };
            ShuffleList(choices);

            correctChoiceIndex = choices.IndexOf(currentTargetItem);

            for (int i = 0; i < choiceButtons.Length && i < choices.Count; i++)
            {
                if (choiceButtons[i] != null)
                {
                    choiceButtons[i].gameObject.SetActive(true);
                    choiceButtons[i].interactable = true;
                    if (choiceImages != null && i < choiceImages.Length && choiceImages[i] != null)
                    {
                        choiceImages[i].sprite = choices[i].pictureSprite;
                    }
                }
            }

            SetSubtitles("Tap the picture that made that sound.");
            StartCoroutine(PlayLottoSoundSequence());
        }

        private IEnumerator PlayLottoSoundSequence()
        {
            isTransitioning = true;
            SetChoiceButtonsInteractable(false);

            if (soundData != null && soundData.tapPictureInstructionClip != null)
            {
                PlayVoice(soundData.tapPictureInstructionClip);
                yield return new WaitForSeconds(soundData.tapPictureInstructionClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(0.4f);
            }

            if (currentTargetItem != null && currentTargetItem.sfxClip != null)
            {
                if (choiceButtons != null && correctChoiceIndex >= 0 && correctChoiceIndex < choiceButtons.Length && choiceButtons[correctChoiceIndex] != null)
                {
                    PlayWiggleAnimation(choiceButtons[correctChoiceIndex].transform);
                }
                PlaySfx(currentTargetItem.sfxClip);
                yield return new WaitForSeconds(currentTargetItem.sfxClip.length + 0.3f);
            }
            else
            {
                PlayProceduralTone(440f, 0.4f, 0.8f);
                yield return new WaitForSeconds(0.5f);
            }

            SetChoiceButtonsInteractable(true);
            isTransitioning = false;
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

            if (currentRoundIndex < 4)
            {
                StartCoroutine(ReplayLottoSound());
            }
            else if (currentRoundIndex < 6)
            {
                StartCoroutine(PlayLoudSoftSequence());
            }
            else
            {
                StartCoroutine(PlayWhichFirstSequence());
            }
        }

        private IEnumerator ReplayLottoSound()
        {
            isTransitioning = true;
            SetChoiceButtonsInteractable(false);

            if (currentTargetItem != null && currentTargetItem.sfxClip != null)
            {
                if (choiceButtons != null && correctChoiceIndex >= 0 && correctChoiceIndex < choiceButtons.Length && choiceButtons[correctChoiceIndex] != null)
                {
                    PlayWiggleAnimation(choiceButtons[correctChoiceIndex].transform);
                }
                PlaySfx(currentTargetItem.sfxClip);
                yield return new WaitForSeconds(currentTargetItem.sfxClip.length + 0.3f);
            }
            else
            {
                PlayProceduralTone(440f, 0.4f, 0.8f);
                yield return new WaitForSeconds(0.5f);
            }

            SetChoiceButtonsInteractable(true);
            isTransitioning = false;
        }

        private void OnChoiceSelected(int index)
        {
            if (IsAudioPlaying() || isActivityCompleted) return;
            StopHintPulseAnimation();

            GameObject tappedObj = (choiceButtons != null && index >= 0 && index < choiceButtons.Length && choiceButtons[index] != null)
                ? choiceButtons[index].gameObject
                : null;
            if (tappedObj != null) PlayWiggleAnimation(tappedObj.transform);

            PlayProceduralTone(523.25f, 0.25f, 0.7f);

            attemptsCount++;

            if (index == correctChoiceIndex)
            {
                StartCoroutine(CorrectAnswerSequence(tappedObj));
            }
            else
            {
                StartCoroutine(RetryAnswerSequence(tappedObj));
            }
        }
        #endregion

        #region Loud & Soft (Rounds 4-5)
        private void SetupLoudAndSoftRound(int subIdx)
        {
            HideAllRoundPanels();
            if (loudSoftPanel != null) loudSoftPanel.SetActive(true);

            isLoudTarget = (subIdx == 0);

            if (soundData != null && soundData.lottoItems != null && soundData.lottoItems.Length > 0)
            {
                currentTargetItem = soundData.lottoItems[Random.Range(0, soundData.lottoItems.Length)];
            }

            string targetText = isLoudTarget ? "Tap the LOUD one!" : "Tap the SOFT one!";
            SetSubtitles($"This one is loud… and this one is soft. {targetText}");

            StartCoroutine(PlayLoudSoftSequence());
        }

        private IEnumerator PlayLoudSoftSequence()
        {
            isTransitioning = true;
            SetButtonInteractableAndDim(elephantLoudButton, null, false);
            SetButtonInteractableAndDim(mouseSoftButton, null, false);

            AudioClip promptClip = null;
            if (soundData != null)
            {
                promptClip = isLoudTarget ? soundData.tapLoudInstructionClip : soundData.tapSoftInstructionClip;
                if (promptClip == null) promptClip = soundData.loudSoftInstructionClip;
            }

            if (promptClip != null)
            {
                PlayVoice(promptClip);
                yield return new WaitForSeconds(promptClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(0.4f);
            }

            // 1. Play LOUD sound & Wiggle Elephant (Loud Button)
            if (elephantLoudButton != null) PlayWiggleAnimation(elephantLoudButton.transform);
            if (currentTargetItem != null && currentTargetItem.sfxClip != null)
            {
                PlaySfx(currentTargetItem.sfxClip, 1.0f);
                yield return new WaitForSeconds(currentTargetItem.sfxClip.length + 0.4f);
            }
            else
            {
                // Fallback procedural tone: Play LOUD tone (1.0 vol)
                PlayProceduralTone(440f, 0.4f, 1.0f);
                yield return new WaitForSeconds(0.6f);
            }

            // 2. Play SOFT sound & Wiggle Mouse (Soft Button)
            if (mouseSoftButton != null) PlayWiggleAnimation(mouseSoftButton.transform);
            if (currentTargetItem != null && currentTargetItem.sfxClip != null)
            {
                PlaySfx(currentTargetItem.sfxClip, 0.3f);
                yield return new WaitForSeconds(currentTargetItem.sfxClip.length + 0.3f);
            }
            else
            {
                // Fallback procedural tone: Play SOFT tone (0.25 vol)
                PlayProceduralTone(440f, 0.4f, 0.25f);
                yield return new WaitForSeconds(0.5f);
            }

            SetButtonInteractableAndDim(elephantLoudButton, null, true);
            SetButtonInteractableAndDim(mouseSoftButton, null, true);
            isTransitioning = false;
        }

        private void OnLoudSoftSelected(bool isLoudTapped)
        {
            if (IsAudioPlaying() || isActivityCompleted) return;

            GameObject tappedObj = isLoudTapped ? elephantLoudButton.gameObject : mouseSoftButton.gameObject;
            if (tappedObj != null) PlayWiggleAnimation(tappedObj.transform);

            // Audio feedback on button tap
            if (isLoudTapped) PlayProceduralTone(523.25f, 0.35f, 1.0f);
            else PlayProceduralTone(523.25f, 0.35f, 0.25f);

            attemptsCount++;
            bool isCorrect = (isLoudTapped == isLoudTarget);

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

        #region Which Came First? (Rounds 6-7)
        private void SetupWhichCameFirstRound(int subIdx)
        {
            HideAllRoundPanels();
            if (orderPanelContainer != null) orderPanelContainer.SetActive(true);

            if (soundData != null && soundData.lottoItems != null && soundData.lottoItems.Length >= 2)
            {
                int r1 = Random.Range(0, soundData.lottoItems.Length);
                int r2 = (r1 + 1 + Random.Range(0, soundData.lottoItems.Length - 1)) % soundData.lottoItems.Length;

                currentTargetItem = soundData.lottoItems[r1];
                secondTargetItem = soundData.lottoItems[r2];
            }

            if (firstOrderImage != null && currentTargetItem != null) firstOrderImage.sprite = currentTargetItem.pictureSprite;
            if (secondOrderImage != null && secondTargetItem != null) secondOrderImage.sprite = secondTargetItem.pictureSprite;

            SetSubtitles("Which sound came FIRST? Tap it.");
            StartCoroutine(PlayWhichFirstSequence());
        }

        private IEnumerator PlayWhichFirstSequence()
        {
            isTransitioning = true;
            SetButtonInteractableAndDim(firstOrderButton, firstOrderImage, false);
            SetButtonInteractableAndDim(secondOrderButton, secondOrderImage, false);

            if (soundData != null && soundData.whichFirstInstructionClip != null)
            {
                PlayVoice(soundData.whichFirstInstructionClip);
                yield return new WaitForSeconds(soundData.whichFirstInstructionClip.length + 0.2f);
            }
            else
            {
                yield return new WaitForSeconds(0.4f);
            }

            if (currentTargetItem != null && currentTargetItem.sfxClip != null)
            {
                PlaySfx(currentTargetItem.sfxClip);
                yield return new WaitForSeconds(currentTargetItem.sfxClip.length + 0.5f);
            }
            else
            {
                PlayProceduralTone(440f, 0.4f, 0.8f);
                yield return new WaitForSeconds(0.5f);
            }

            if (secondTargetItem != null && secondTargetItem.sfxClip != null)
            {
                PlaySfx(secondTargetItem.sfxClip);
                yield return new WaitForSeconds(secondTargetItem.sfxClip.length + 0.3f);
            }
            else
            {
                PlayProceduralTone(659.25f, 0.4f, 0.8f);
                yield return new WaitForSeconds(0.5f);
            }

            SetButtonInteractableAndDim(firstOrderButton, firstOrderImage, true);
            SetButtonInteractableAndDim(secondOrderButton, secondOrderImage, true);
            isTransitioning = false;
        }

        private void OnWhichFirstSelected(int orderIndex)
        {
            if (IsAudioPlaying() || isActivityCompleted) return;

            GameObject tappedObj = (orderIndex == 0) ? firstOrderButton.gameObject : secondOrderButton.gameObject;
            if (tappedObj != null) PlayWiggleAnimation(tappedObj.transform);

            attemptsCount++;
            bool isCorrect = (orderIndex == 0);

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

        #region Feedback & Support Fading Sequences
        private IEnumerator CorrectAnswerSequence(GameObject buttonObj)
        {
            isTransitioning = true;

            if (buttonObj != null) SetButtonColor(buttonObj, new Color(0.2f, 0.85f, 0.2f, 1f)); // Right answer: GREEN

            if (correctChimeSfx != null) PlaySfx(correctChimeSfx);
            if (buttonObj != null) PlayBounceAnimation(buttonObj.transform);

            if (currentTargetItem != null && currentTargetItem.successVoiceClip != null)
            {
                SetSubtitles($"Yes! That was a {currentTargetItem.soundName}.");
                PlayVoice(currentTargetItem.successVoiceClip);
                yield return new WaitForSeconds(currentTargetItem.successVoiceClip.length + 0.3f);
            }
            else
            {
                SetSubtitles("Great listening! You got it!");
                yield return new WaitForSeconds(0.8f);
            }

            isTransitioning = false;
            LoadRound(currentRoundIndex + 1);
        }

        private IEnumerator RetryAnswerSequence(GameObject buttonObj)
        {
            isTransitioning = true;

            if (buttonObj != null) SetButtonColor(buttonObj, new Color(0.95f, 0.25f, 0.25f, 1f)); // Wrong answer: RED

            if (retryGentleSfx != null) PlaySfx(retryGentleSfx);
            if (buttonObj != null) PlayWrongWobbleAnimation(buttonObj);

            if (attemptsCount == 2)
            {
                SetSubtitles("Nearly! Listen once more…");
                if (soundData != null && soundData.retryVoiceClip != null)
                {
                    PlayVoice(soundData.retryVoiceClip);
                    yield return new WaitForSeconds(soundData.retryVoiceClip.length + 0.2f);
                }
            }
            else if (attemptsCount >= 3)
            {
                if (momoHintObject != null) momoHintObject.SetActive(true);
                SetSubtitles("Psst! Tap this one with Gigi!");

                GameObject correctObj = GetCurrentCorrectButton();
                if (correctObj != null)
                {
                    StartHintPulseAnimation(correctObj.transform);
                }

                yield return new WaitForSeconds(1.0f);
            }
            else
            {
                SetSubtitles("Listen carefully once more!");
                yield return new WaitForSeconds(0.6f);
            }

            if (buttonObj != null) SetButtonColor(buttonObj, Color.white); // Restore white on retry reset
            isTransitioning = false;
        }

        private IEnumerator CompletionSequence()
        {
            isTransitioning = true;
            isActivityCompleted = true;

            HideAllRoundPanels();

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);
            if (stickerPopSfx != null) PlaySfx(stickerPopSfx);

            SetSubtitles("Your ears are strong! Sounds are everywhere… and words have tiny sounds hiding inside them too. Let's catch them!");

            if (soundData != null && soundData.completionBridgeVoiceClip != null)
            {
                PlayVoice(soundData.completionBridgeVoiceClip);
                yield return new WaitForSeconds(soundData.completionBridgeVoiceClip.length + 0.5f);
            }

            if (currentPanel != null) TopicProgressUI.MarkTopicComplete(currentPanel);
            else TopicProgressUI.MarkTopicComplete(gameObject);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

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

        private void SetChoiceButtonsInteractable(bool interactable)
        {
            if (choiceButtons == null) return;
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                Image img = (choiceImages != null && i < choiceImages.Length) ? choiceImages[i] : null;
                SetButtonInteractableAndDim(choiceButtons[i], img, interactable);
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
            if (choiceButtons != null)
            {
                for (int i = 0; i < choiceButtons.Length; i++)
                {
                    if (choiceButtons[i] != null) SetButtonColor(choiceButtons[i].gameObject, Color.white);
                }
            }
            if (elephantLoudButton != null) SetButtonColor(elephantLoudButton.gameObject, Color.white);
            if (mouseSoftButton != null) SetButtonColor(mouseSoftButton.gameObject, Color.white);
            if (firstOrderButton != null) SetButtonColor(firstOrderButton.gameObject, Color.white);
            if (secondOrderButton != null) SetButtonColor(secondOrderButton.gameObject, Color.white);
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

        private void PlayProceduralTone(float frequency, float duration, float volume)
        {
            if (sfxAudioSource == null) return;
            int sampleRate = 44100;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Clamp01((duration - t) / duration);
                samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * envelope * volume;
            }

            AudioClip toneClip = AudioClip.Create("ProceduralTone", sampleCount, 1, sampleRate, false);
            toneClip.SetData(samples, 0);
            sfxAudioSource.PlayOneShot(toneClip, volume);
        }

        private void SetSubtitles(string text)
        {
            DialogueBoxAutoHider.SetDialogue(dialogueText, text, dialogueCanvasGroup);
        }

        private void ShuffleList<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int rnd = Random.Range(0, i + 1);
                T temp = list[i];
                list[i] = list[rnd];
                list[rnd] = temp;
            }
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
            if (currentRoundIndex < 4)
            {
                if (choiceButtons != null && correctChoiceIndex >= 0 && correctChoiceIndex < choiceButtons.Length)
                {
                    return choiceButtons[correctChoiceIndex] != null ? choiceButtons[correctChoiceIndex].gameObject : null;
                }
            }
            else if (currentRoundIndex < 6)
            {
                if (isLoudTarget && elephantLoudButton != null) return elephantLoudButton.gameObject;
                if (!isLoudTarget && mouseSoftButton != null) return mouseSoftButton.gameObject;
            }
            else
            {
                if (firstOrderButton != null) return firstOrderButton.gameObject;
            }
            return null;
        }
        #endregion
    }
}
