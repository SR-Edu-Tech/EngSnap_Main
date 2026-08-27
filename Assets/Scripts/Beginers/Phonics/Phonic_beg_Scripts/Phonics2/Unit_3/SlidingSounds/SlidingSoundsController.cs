using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit3
{
    public class SlidingSoundsController : MonoBehaviour
    {
        [Header("Unit & Topic Identity")]
        [SerializeField] private string unitID = "Unit3";
        [SerializeField] private string topicName = "SlidingSounds";

        [Header("Data Reference")]
        [SerializeField] private SlidingSoundsData activityData;

        [Header("UI Text & Dialogue")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Phase 1 & 2: Playground Slide & Free Listening UI")]
        [SerializeField] private GameObject playgroundPanel;
        [SerializeField] private RectTransform slideCharacterRect;
        [SerializeField] private Image morphingMouthImage;
        [SerializeField] private Button[] diphthongTileButtons; // 8 items
        [SerializeField] private TMP_Text[] diphthongTileTexts;
        [SerializeField] private Image[] diphthongTileImages;
        [SerializeField] private Button startStarRoundButton;

        [Header("Phase 3: Tara Star Round UI")]
        [SerializeField] private GameObject starRoundPanel;
        [SerializeField] private TMP_Text starQuestionPromptTMP;
        [SerializeField] private Image starQuestionImage;
        [SerializeField] private Button[] starChoiceButtons;
        [SerializeField] private TMP_Text[] starChoiceTexts;

        [Header("Button Feedback Colors")]
        [SerializeField] private Color correctColor = new Color(0.3f, 0.69f, 0.31f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.96f, 0.26f, 0.21f, 1f);

        [Header("Progress Ring UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;

        [Header("Mascots")]
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject momoHintObject;
        [SerializeField] private GameObject taraMascotObject;

        [Header("SFX Feedback Clips")]
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip retryGentleSfx;
        [SerializeField] private AudioClip starPopSfx;
        [SerializeField] private AudioClip slideWhooshSfx;

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

        [Tooltip("The current panel to hide when Continue is clicked.")]
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentSlideIndex = 0;
        private int starChallengeIndex = 0;
        private int totalStarChallenges = 6;
        private bool isStarRoundActive = false;
        private bool isTransitioning = false;
        private StarRoundUnit3Challenge currentStarChallenge;
        private int failAttempts = 0;
        private Coroutine momoPulseCoroutine;

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
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
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
            if (startStarRoundButton != null) startStarRoundButton.onClick.AddListener(StartStarRound);

            if (diphthongTileButtons != null)
            {
                for (int i = 0; i < diphthongTileButtons.Length; i++)
                {
                    int index = i;
                    if (diphthongTileButtons[i] != null)
                        diphthongTileButtons[i].onClick.AddListener(() => OnDiphthongTileTapped(index));
                }
            }

            for (int i = 0; i < starChoiceButtons.Length; i++)
            {
                int index = i;
                if (starChoiceButtons[i] != null)
                    starChoiceButtons[i].onClick.AddListener(() => OnStarChoiceSelected(index));
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
            currentSlideIndex = 0;
            starChallengeIndex = 0;
            isStarRoundActive = false;
            isTransitioning = false;

            if (playgroundPanel != null) playgroundPanel.SetActive(true);
            if (starRoundPanel != null) starRoundPanel.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (taraMascotObject != null) taraMascotObject.SetActive(true);
            if (continueButton != null) continueButton.SetActive(false);

            SetupDiphthongGrid();
            UpdateProgressUI(0f);
            StartCoroutine(PlayIntroSequence());
        }

        private IEnumerator PlayIntroSequence()
        {
            isTransitioning = true;
            SetDialogue("Some sounds do not stay still. They SLIDE! Watch.");

            if (activityData != null && activityData.introVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.introVoiceClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            TriggerSlideAnimation();
            SetDialogue("ooo … iii … oy! Boy!");
            if (activityData != null && activityData.slideDemoClip != null)
            {
                yield return PlayVoiceClip(activityData.slideDemoClip);
            }

            SetDialogue("Say it with me — your mouth slides too! Tap any sliding sound!");
            if (activityData != null && activityData.copyAlongClip != null)
            {
                yield return PlayVoiceClip(activityData.copyAlongClip);
            }

            if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(true);
            isTransitioning = false;
        }

        private void SetupDiphthongGrid()
        {
            if (activityData == null || activityData.diphthongItems == null) return;

            for (int i = 0; i < diphthongTileButtons.Length; i++)
            {
                if (i < activityData.diphthongItems.Length)
                {
                    diphthongTileButtons[i].gameObject.SetActive(true);
                    DiphthongSlideItem item = activityData.diphthongItems[i];

                    if (diphthongTileTexts[i] != null) diphthongTileTexts[i].text = item.wordName;
                    if (diphthongTileImages[i] != null && item.pictureSprite != null)
                        diphthongTileImages[i].sprite = item.pictureSprite;
                }
                else
                {
                    diphthongTileButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnDiphthongTileTapped(int index)
        {
            if (isTransitioning || IsAudioPlaying() || activityData == null || index >= activityData.diphthongItems.Length) return;

            DiphthongSlideItem item = activityData.diphthongItems[index];
            TriggerSlideAnimation();
            PlaySFX(correctChimeSfx);

            SetDialogue($"Sliding Sound: {item.wordName.ToUpper()}!");

            if (item.slideAudioClip != null)
            {
                PlayVoiceClipNonBlocking(item.slideAudioClip);
            }
        }

        private void TriggerSlideAnimation()
        {
            PlaySFX(slideWhooshSfx);
            if (slideCharacterRect != null)
            {
                StartCoroutine(AnimateSlideCharacter());
            }
        }

        private IEnumerator AnimateSlideCharacter()
        {
            Vector3 startPos = new Vector3(-150f, 150f, 0f);
            Vector3 endPos = new Vector3(150f, -150f, 0f);
            float elapsed = 0f;
            float duration = 0.8f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                slideCharacterRect.anchoredPosition = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }

            slideCharacterRect.anchoredPosition = endPos;
        }

        private void StartStarRound()
        {
            isStarRoundActive = true;
            starChallengeIndex = 0;

            if (playgroundPanel != null) playgroundPanel.SetActive(false);
            if (starRoundPanel != null) starRoundPanel.SetActive(true);

            if (leoMascotObject != null) leoMascotObject.SetActive(false);
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
            StopMomoPulse();
            if (momoHintObject != null) momoHintObject.SetActive(false);

            currentStarChallenge = activityData.starChallenges[index];

            if (starQuestionPromptTMP != null) starQuestionPromptTMP.text = currentStarChallenge.questionPrompt;
            if (starQuestionImage != null && currentStarChallenge.promptSprite != null)
                starQuestionImage.sprite = currentStarChallenge.promptSprite;

            SetDialogue(currentStarChallenge.questionPrompt);
            if (currentStarChallenge.promptClip != null)
            {
                PlayVoiceClipNonBlocking(currentStarChallenge.promptClip);
            }

            SetupStarChoices();
            UpdateProgressUI((8f + index) / 14f);
        }

        private void SetupStarChoices()
        {
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

        private void OnStarChoiceSelected(int choiceIndex)
        {
            if (isTransitioning || IsAudioPlaying() || currentStarChallenge == null) return;

            bool isCorrect = (choiceIndex == currentStarChallenge.correctChoiceIndex);
            Button tappedBtn = (choiceIndex >= 0 && choiceIndex < starChoiceButtons.Length) ? starChoiceButtons[choiceIndex] : null;

            if (tappedBtn != null)
            {
                RectTransform rt = tappedBtn.GetComponent<RectTransform>();
                TriggerWiggle(rt);

                Image btnImg = tappedBtn.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = isCorrect ? correctColor : wrongColor;
                    if (!isCorrect) StartCoroutine(ResetButtonColor(btnImg, 0.8f));
                }
            }

            if (isCorrect)
            {
                StopMomoPulse();
                StartCoroutine(HandleStarChoiceCorrect());
            }
            else
            {
                failAttempts++;
                if (failAttempts >= 2 && momoHintObject != null)
                {
                    momoHintObject.SetActive(true);
                    SetDialogue("Momo says: Tap the glowing answer!");
                    TriggerMomoCorrectAnswerPulse();
                }
                StartCoroutine(HandleStarChoiceWrong());
            }
        }

        private IEnumerator ResetButtonColor(Image targetImage, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (targetImage != null) targetImage.color = Color.white;
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

            SetDialogue("You know both voices of every vowel. You are a VOWEL VOICE!");
            if (activityData != null && activityData.badgeVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.badgeVoiceClip);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            PlaySFX(starPopSfx);
            yield return new WaitForSeconds(0.5f);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            if (continueButton != null) continueButton.SetActive(true);
            isTransitioning = false;
        }

        public void DeactivateMascots()
        {
            StopMomoPulse();
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (taraMascotObject != null) taraMascotObject.SetActive(false);
        }

        private void TriggerMomoCorrectAnswerPulse()
        {
            StopMomoPulse();
            if (currentStarChallenge != null && currentStarChallenge.correctChoiceIndex >= 0 && currentStarChallenge.correctChoiceIndex < starChoiceButtons.Length)
            {
                Button correctBtn = starChoiceButtons[currentStarChallenge.correctChoiceIndex];
                if (correctBtn != null)
                {
                    RectTransform correctRect = correctBtn.GetComponent<RectTransform>();
                    momoPulseCoroutine = StartCoroutine(PulseCorrectAnswerLoop(correctRect));
                }
            }
        }

        private void StopMomoPulse()
        {
            if (momoPulseCoroutine != null)
            {
                StopCoroutine(momoPulseCoroutine);
                momoPulseCoroutine = null;
            }

            if (starChoiceButtons != null)
            {
                for (int i = 0; i < starChoiceButtons.Length; i++)
                {
                    if (starChoiceButtons[i] != null)
                        starChoiceButtons[i].transform.localScale = Vector3.one;
                }
            }
        }

        private IEnumerator PulseCorrectAnswerLoop(RectTransform targetRect)
        {
            if (targetRect == null) yield break;
            Vector3 baseScale = Vector3.one;
            Vector3 maxScale = Vector3.one * 1.15f;
            float pulseSpeed = 4f;

            while (true)
            {
                float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
                targetRect.localScale = Vector3.Lerp(baseScale, maxScale, t);
                yield return null;
            }
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
            else gameObject.SetActive(false);

            TopicProgressUI.RefreshAllTicks();
        }

        public void GoToPreviousPanel()
        {
            DeactivateMascots();
            if (currentPanel != null) currentPanel.SetActive(false);
            if (unitContentPanel != null) unitContentPanel.SetActive(true);
        }

        private void TriggerWiggle(RectTransform target)
        {
            if (target == null) return;
            StartCoroutine(WiggleRect(target, 0.35f, 10f));
        }

        private void TriggerWiggleStarMeter()
        {
            if (starMeterRect != null)
            {
                StartCoroutine(WiggleRect(starMeterRect, 0.45f, 12f));
            }
        }

        private IEnumerator WiggleRect(RectTransform target, float duration, float angle)
        {
            Quaternion originalRot = target.localRotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float percent = elapsed / duration;
                float currentAngle = Mathf.Sin(percent * Mathf.PI * 8f) * angle * (1f - percent);
                target.localRotation = originalRot * Quaternion.Euler(0f, 0f, currentAngle);
                yield return null;
            }

            target.localRotation = originalRot;
        }

        private bool IsAudioPlaying()
        {
            return voiceAudioSource != null && voiceAudioSource.isPlaying;
        }

        private void SetDialogue(string msg)
        {
            DialogueBoxAutoHider.SetDialogue(dialogueText, msg, dialogueCanvasGroup);
        }

        private void UpdateProgressUI(float fillPercent)
        {
            fillPercent = Mathf.Clamp01(fillPercent);
            if (progressRingFillImage != null) progressRingFillImage.fillAmount = fillPercent;
            if (progressText != null) progressText.text = $"{Mathf.RoundToInt(fillPercent * 100)}%";
        }

        private IEnumerator PlayVoiceClip(AudioClip clip)
        {
            if (clip == null || voiceAudioSource == null) yield break;
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
            yield return new WaitForSeconds(clip.length + 0.1f);
        }

        private void PlayVoiceClipNonBlocking(AudioClip clip)
        {
            if (clip == null || voiceAudioSource == null) return;
            voiceAudioSource.Stop();
            voiceAudioSource.clip = clip;
            voiceAudioSource.Play();
        }

        private void PlaySFX(AudioClip clip)
        {
            if (clip == null || sfxAudioSource == null) return;
            sfxAudioSource.PlayOneShot(clip);
        }
    }
}
