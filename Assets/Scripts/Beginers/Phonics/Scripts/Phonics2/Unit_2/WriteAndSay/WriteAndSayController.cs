using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit2
{
    public class WriteAndSayController : MonoBehaviour
    {
        [Header("Unit & Topic Identity")]
        [SerializeField] private string unitID = "Unit2";
        [SerializeField] private string topicName = "WriteAndSay";

        [Header("Data Reference")]
        [SerializeField] private WriteAndSayData activityData;

        [Header("UI Text & Dialogue")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Part A: Tracing UI")]
        [SerializeField] private GameObject tracingPanel;
        [SerializeField] private Image pictureImage;
        [SerializeField] private TMP_Text wordDisplayTMP;
        [SerializeField] private Image wordGlowHighlight;
        [SerializeField] private LetterTracingComponent letterTracingComponent;

        [Header("Part B: Tara Star Round UI")]
        [SerializeField] private GameObject starRoundPanel;
        [SerializeField] private TMP_Text starQuestionPromptTMP;
        [SerializeField] private Image starQuestionImage;
        [SerializeField] private Button[] starChoiceButtons;
        [SerializeField] private TMP_Text[] starChoiceTexts;
        [SerializeField] private GameObject vowelStripContainer;
        [SerializeField] private Button[] vowelStripButtons; // 5 Vowels for Challenge 6
        [SerializeField] private AudioClip[] vowelSoundClips = new AudioClip[5]; // Audio clips for A, E, I, O, U

        [Header("Button Feedback Colors")]
        [SerializeField] private Color correctColor = new Color(0.3f, 0.69f, 0.31f, 1f); // #4CAF50 Green
        [SerializeField] private Color wrongColor = new Color(0.96f, 0.26f, 0.21f, 1f); // #F44336 Red


        [Header("Progress Ring UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;

        [Header("Mascots")]
        [SerializeField] private GameObject momoHintObject;
        [SerializeField] private GameObject taraMascotObject;

        [Header("SFX Feedback Clips")]
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip retryGentleSfx;
        [SerializeField] private AudioClip starPopSfx;
        [SerializeField] private AudioClip wordSnapSfx;
        [SerializeField] private AudioClip badgeAwardFanfareSfx;

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

        private int tracingRoundIndex = 0;
        private int totalTracingRounds = 8;
        private int starChallengeIndex = 0;
        private int totalStarChallenges = 6;
        private int failAttempts = 0;
        private bool isStarRoundActive = false;
        private bool isTransitioning = false;
        private WriteAndSayItem currentTracingItem;
        private StarRoundUnit2Challenge currentStarChallenge;
        private HashSet<int> tappedVowelsInStrip = new HashSet<int>();
        private Coroutine _momoHintPulseCoroutine;

        public string UnitID => unitID;
        public string TopicName => topicName;

        private void Awake()
        {
            EnsureAudioSources();
            SetupButtonListeners();
        }

        private void Start()
        {
            StartActivity();
        }

        private void OnEnable()
        {
            if (taraMascotObject != null) taraMascotObject.SetActive(true);
            StartActivity();
        }

        private void OnDisable()
        {
            StopAllAudio();
            StopAllCoroutines();
            DeactivateMascots();
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

        private void SetupButtonListeners()
        {
            if (continueButton != null)
            {
                Button btn = continueButton.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(GoToNextPanel);
                }
            }

            if (letterTracingComponent != null)
            {
                letterTracingComponent.OnTracingCompleted += HandleTracingCompleted;
                letterTracingComponent.OnTracingFailedAttempt += HandleTracingFailedAttempt;
            }

            for (int i = 0; i < starChoiceButtons.Length; i++)
            {
                int index = i;
                if (starChoiceButtons[i] != null)
                    starChoiceButtons[i].onClick.AddListener(() => OnStarChoiceSelected(index));
            }

            if (vowelStripButtons != null)
            {
                for (int i = 0; i < vowelStripButtons.Length; i++)
                {
                    int index = i;
                    vowelStripButtons[i].onClick.AddListener(() => OnVowelStripButtonTapped(index));
                }
            }
        }

        private void OnDestroy()
        {
            if (letterTracingComponent != null)
            {
                letterTracingComponent.OnTracingCompleted -= HandleTracingCompleted;
                letterTracingComponent.OnTracingFailedAttempt -= HandleTracingFailedAttempt;
            }
        }

        public void StartActivity()
        {
            tracingRoundIndex = 0;
            starChallengeIndex = 0;
            failAttempts = 0;
            isStarRoundActive = false;
            isTransitioning = false;

            if (tracingPanel != null) tracingPanel.SetActive(true);
            if (starRoundPanel != null) starRoundPanel.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (taraMascotObject != null) taraMascotObject.SetActive(true);
            if (continueButton != null) continueButton.SetActive(false);

            UpdateProgressUI(0f);
            StartCoroutine(StartIntroSequence());
        }

        private IEnumerator StartIntroSequence()
        {
            isTransitioning = true;
            SetDialogue("Look at the picture. What letter is missing?");

            if (activityData != null && activityData.introVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.introVoiceClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            isTransitioning = false;
            LoadTracingRound(0);
        }

        private void LoadTracingRound(int roundIndex)
        {
            if (activityData == null || activityData.tracingItems == null || roundIndex >= activityData.tracingItems.Length)
            {
                StartStarRound();
                return;
            }

            tracingRoundIndex = roundIndex;
            failAttempts = 0;
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (taraMascotObject != null) taraMascotObject.SetActive(true);

            currentTracingItem = activityData.tracingItems[roundIndex];

            if (pictureImage != null && currentTracingItem.pictureSprite != null)
                pictureImage.sprite = currentTracingItem.pictureSprite;
            if (wordDisplayTMP != null)
                wordDisplayTMP.text = currentTracingItem.displayGapText;
            if (wordGlowHighlight != null)
                wordGlowHighlight.gameObject.SetActive(false);

            if (letterTracingComponent != null)
            {
                letterTracingComponent.SetupTracing(
                    currentTracingItem.tracingOutlineSprite,
                    currentTracingItem.letterSoundClip,
                    currentTracingItem.missingLetter,
                    currentTracingItem.filledLetterSprite,
                    currentTracingItem.checkpointPositions
                );
            }

            SetDialogue($"Trace the letter {currentTracingItem.missingLetter} — and say the sound as you go.");
            UpdateProgressUI((float)tracingRoundIndex / 14f);
        }

        private void HandleTracingFailedAttempt()
        {
            failAttempts++;
            PlaySFX(retryGentleSfx);

            if (failAttempts >= 2)
            {
                if (momoHintObject != null) momoHintObject.SetActive(true);
                SetDialogue("Follow Momo! Start at the glowing checkpoint and draw smooth.");
                if (letterTracingComponent != null) letterTracingComponent.PlayGhostFingerGuide();
                if (activityData != null && activityData.momoGhostFingerClip != null)
                {
                    PlayVoiceClipNonBlocking(activityData.momoGhostFingerClip);
                }
            }
        }

        private void HandleTracingCompleted()
        {
            if (isTransitioning || currentTracingItem == null) return;
            StartCoroutine(HandleTracingCompletedSequence());
        }

        private IEnumerator HandleTracingCompletedSequence()
        {
            isTransitioning = true;
            PlaySFX(wordSnapSfx);
            PlaySFX(correctChimeSfx);
            TriggerWiggleStarMeter();

            if (wordDisplayTMP != null)
                wordDisplayTMP.text = currentTracingItem.wordName;
            if (wordGlowHighlight != null)
                wordGlowHighlight.gameObject.SetActive(true);

            SetDialogue($"{currentTracingItem.missingLetter} - {currentTracingItem.wordName.Substring(1)}. {currentTracingItem.wordName.ToUpper()}! You wrote it and you said it!");

            if (currentTracingItem.wordAudioClip != null)
            {
                yield return PlayVoiceClip(currentTracingItem.wordAudioClip);
            }
            else
            {
                yield return new WaitForSeconds(0.8f);
            }

            tracingRoundIndex++;
            isTransitioning = false;

            if (tracingRoundIndex < totalTracingRounds && tracingRoundIndex < activityData.tracingItems.Length)
            {
                LoadTracingRound(tracingRoundIndex);
            }
            else
            {
                StartStarRound();
            }
        }

        private void StartStarRound()
        {
            isStarRoundActive = true;
            starChallengeIndex = 0;

            if (tracingPanel != null) tracingPanel.SetActive(false);
            if (starRoundPanel != null) starRoundPanel.SetActive(true);
            if (taraMascotObject != null) taraMascotObject.SetActive(true);

            SetDialogue("My turn! Six quick challenges. Ready? Roar!");
            if (activityData != null && activityData.taraStarRoundOpenerClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.taraStarRoundOpenerClip);
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
            failAttempts = 0;
            if (momoHintObject != null) momoHintObject.SetActive(false);
            StopHintPulse();

            currentStarChallenge = activityData.starChallenges[index];

            if (starQuestionPromptTMP != null) starQuestionPromptTMP.text = currentStarChallenge.questionPrompt;
            if (starQuestionImage != null && currentStarChallenge.promptSprite != null)
                starQuestionImage.sprite = currentStarChallenge.promptSprite;

            SetDialogue(currentStarChallenge.questionPrompt);
            if (currentStarChallenge.promptClip != null)
            {
                PlayVoiceClipNonBlocking(currentStarChallenge.promptClip);
            }

            if (currentStarChallenge.isVowelStripChallenge)
            {
                SetupVowelStrip();
            }
            else
            {
                SetupStarChoices();
            }

            UpdateProgressUI((8f + index) / 14f);
        }

        private void SetupStarChoices()
        {
            if (vowelStripContainer != null) vowelStripContainer.SetActive(false);
            StopHintPulse();

            for (int i = 0; i < starChoiceButtons.Length; i++)
            {
                if (i < currentStarChallenge.choices.Length)
                {
                    starChoiceButtons[i].gameObject.SetActive(true);
                    starChoiceButtons[i].transform.localScale = Vector3.one;

                    Image btnImg = starChoiceButtons[i].GetComponent<Image>();
                    if (btnImg != null) btnImg.color = Color.white;

                    if (starChoiceTexts[i] != null) starChoiceTexts[i].text = currentStarChallenge.choices[i];
                }
                else
                {
                    starChoiceButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void SetupVowelStrip()
        {
            for (int i = 0; i < starChoiceButtons.Length; i++) starChoiceButtons[i].gameObject.SetActive(false);
            if (vowelStripContainer != null) vowelStripContainer.SetActive(true);
            tappedVowelsInStrip.Clear();
        }

        private bool IsAudioPlaying()
        {
            return voiceAudioSource != null && voiceAudioSource.isPlaying;
        }

        private void OnStarChoiceSelected(int choiceIndex)
        {
            if (isTransitioning || IsAudioPlaying() || currentStarChallenge == null) return;

            bool isCorrect = (choiceIndex == currentStarChallenge.correctChoiceIndex);
            Button tappedBtn = (choiceIndex >= 0 && choiceIndex < starChoiceButtons.Length) ? starChoiceButtons[choiceIndex] : null;

            if (tappedBtn != null)
            {
                RectTransform rt = tappedBtn.GetComponent<RectTransform>();
                WiggleButton(rt);

                Image btnImg = tappedBtn.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = isCorrect ? correctColor : wrongColor;
                    if (!isCorrect)
                    {
                        // Reset wrong button red color back to white after 0.8s feedback
                        StartCoroutine(ResetButtonColor(btnImg, 0.8f));
                    }
                }
            }

            if (isCorrect)
            {
                StopHintPulse();
                if (momoHintObject != null) momoHintObject.SetActive(false);
                StartCoroutine(HandleStarChoiceCorrect());
            }
            else
            {
                failAttempts++;
                if (failAttempts >= 2)
                {
                    TriggerMomoHintForQuiz();
                }
                StartCoroutine(HandleStarChoiceWrong());
            }
        }

        private IEnumerator ResetButtonColor(Image targetImage, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (targetImage != null)
            {
                targetImage.color = Color.white;
            }
        }

        private void TriggerMomoHintForQuiz()
        {
            if (momoHintObject != null) momoHintObject.SetActive(true);
            SetDialogue("Follow Momo! Tap the pop-up answer!");

            if (currentStarChallenge != null && currentStarChallenge.correctChoiceIndex >= 0 && currentStarChallenge.correctChoiceIndex < starChoiceButtons.Length)
            {
                Button correctBtn = starChoiceButtons[currentStarChallenge.correctChoiceIndex];
                if (correctBtn != null)
                {
                    RectTransform rt = correctBtn.GetComponent<RectTransform>();
                    StopHintPulse();
                    _momoHintPulseCoroutine = StartCoroutine(LoopPulseScaleRect(rt));
                }
            }
        }

        private IEnumerator LoopPulseScaleRect(RectTransform targetRect)
        {
            if (targetRect == null) yield break;
            Vector3 baseScale = Vector3.one;
            Vector3 pulseScale = baseScale * 1.25f;

            while (true)
            {
                float elapsed = 0f;
                float duration = 0.5f;

                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    targetRect.localScale = Vector3.Lerp(baseScale, pulseScale, elapsed / duration);
                    yield return null;
                }

                elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    targetRect.localScale = Vector3.Lerp(pulseScale, baseScale, elapsed / duration);
                    yield return null;
                }

                yield return new WaitForSeconds(0.1f);
            }
        }

        private void StopHintPulse()
        {
            if (_momoHintPulseCoroutine != null)
            {
                StopCoroutine(_momoHintPulseCoroutine);
                _momoHintPulseCoroutine = null;
            }
            if (starChoiceButtons != null)
            {
                for (int i = 0; i < starChoiceButtons.Length; i++)
                {
                    if (starChoiceButtons[i] != null)
                    {
                        starChoiceButtons[i].transform.localScale = Vector3.one;
                    }
                }
            }
        }

        private void WiggleButton(RectTransform targetRect)
        {
            if (targetRect == null) return;
            StartCoroutine(WiggleButtonSequence(targetRect));
        }

        private IEnumerator WiggleButtonSequence(RectTransform targetRect)
        {
            Vector3 originalScale = Vector3.one;
            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float scale = 1f + Mathf.Sin(elapsed / duration * Mathf.PI) * 0.15f;
                targetRect.localScale = originalScale * scale;
                yield return null;
            }
            targetRect.localScale = originalScale;
        }

        private string[] vowelNames = new string[] { "A", "E", "I", "O", "U" };
        private string[] vowelPhonics = new string[] { "a", "e", "i", "o", "u" };

        private void OnVowelStripButtonTapped(int index)
        {
            if (isTransitioning || IsAudioPlaying()) return;

            if (!tappedVowelsInStrip.Contains(index))
            {
                tappedVowelsInStrip.Add(index);
            }

            string letterName = (index >= 0 && index < vowelNames.Length) ? vowelNames[index] : "vowel";
            string soundText = (index >= 0 && index < vowelPhonics.Length) ? vowelPhonics[index] : "sound";

            SetDialogue($"{letterName} says /{soundText}/! You found {tappedVowelsInStrip.Count} of 5 vowels!");

            PlaySFX(correctChimeSfx);

            if (vowelSoundClips != null && index >= 0 && index < vowelSoundClips.Length && vowelSoundClips[index] != null)
            {
                PlayVoiceClipNonBlocking(vowelSoundClips[index]);
            }

            if (tappedVowelsInStrip.Count >= 5)
            {
                StartCoroutine(HandleStarChoiceCorrect());
            }
        }

        private IEnumerator HandleStarChoiceCorrect()
        {
            isTransitioning = true;
            PlaySFX(correctChimeSfx);
            TriggerWiggleStarMeter();

            SetDialogue("Roar! You got it right!");
            yield return new WaitForSeconds(0.8f);

            starChallengeIndex++;
            isTransitioning = false;

            if (starChallengeIndex < totalStarChallenges && starChallengeIndex < activityData.starChallenges.Length)
            {
                LoadStarChallenge(starChallengeIndex);
            }
            else
            {
                StartCoroutine(CompleteStarRoundSequence());
            }
        }

        private IEnumerator HandleStarChoiceWrong()
        {
            PlaySFX(retryGentleSfx);
            SetDialogue("Almost! Try again.");
            yield return new WaitForSeconds(0.6f);
        }

        private IEnumerator CompleteStarRoundSequence()
        {
            if (starRoundPanel != null) starRoundPanel.SetActive(false);
            UpdateProgressUI(1f);

            SetDialogue("You know every letter and every sound. You are a LETTER MASTER!");
            if (activityData != null && activityData.badgeVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.badgeVoiceClip);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            PlaySFX(starPopSfx != null ? starPopSfx : badgeAwardFanfareSfx);
            yield return new WaitForSeconds(0.5f);

            if (currentPanel != null) TopicProgressUI.MarkTopicComplete(currentPanel);
            else TopicProgressUI.MarkTopicComplete(gameObject);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            if (continueButton != null) continueButton.SetActive(true);
            isTransitioning = false;
        }

        public void DeactivateMascots()
        {
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (taraMascotObject != null) taraMascotObject.SetActive(false);
        }

        public void GoToNextPanel()
        {
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
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

            if (currentPanel != null)  {
                 currentPanel.SetActive(false);
                unitContentPanel.SetActive(false);
            }
            gameObject.SetActive(false);
        }

        public void GoToPreviousPanel()
        {
            DeactivateMascots();
            if (currentPanel != null) currentPanel.SetActive(false);
            if (unitContentPanel != null) unitContentPanel.SetActive(true);
        }

        private void SetDialogue(string text)
        {
            DialogueBoxAutoHider.SetDialogue(dialogueText, text, dialogueCanvasGroup);
        }

        private void UpdateProgressUI(float fillRatio)
        {
            fillRatio = Mathf.Clamp01(fillRatio);
            if (progressRingFillImage != null) progressRingFillImage.fillAmount = fillRatio;
            if (progressText != null) progressText.text = $"{Mathf.RoundToInt(fillRatio * 100)}%";
        }

        private void TriggerWiggleStarMeter()
        {
            if (starMeterRect != null)
            {
                WiggleButton(starMeterRect);
            }
        }

        private IEnumerator PlayVoiceClip(AudioClip clip)
        {
            if (voiceAudioSource != null && clip != null)
            {
                voiceAudioSource.Stop();
                voiceAudioSource.clip = clip;
                voiceAudioSource.Play();
                yield return new WaitForSeconds(clip.length + 0.1f);
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

        private void PlaySFX(AudioClip clip)
        {
            if (sfxAudioSource != null && clip != null)
            {
                sfxAudioSource.PlayOneShot(clip);
            }
        }
    }
}
