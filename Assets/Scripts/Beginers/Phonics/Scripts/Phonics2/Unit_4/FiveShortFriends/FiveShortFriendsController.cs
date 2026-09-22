using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit4
{
    public class FiveShortFriendsController : MonoBehaviour
    {
        [Header("Unit & Topic Identity")]
        [SerializeField] private string unitID = "Unit4";
        [SerializeField] private string topicName = "FiveShortFriends";

        [Header("Data Reference")]
        [SerializeField] private FiveShortFriendsData activityData;

        [Header("UI Text & Dialogue")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Phase 1 & 2: Street & Vowel Houses UI")]
        [SerializeField] private GameObject streetPanel;
        [SerializeField] private Button[] vowelHouseButtons = new Button[5]; // ă, ĕ, ĭ, ŏ, ŭ
        [SerializeField] private TMP_Text[] vowelHouseTexts = new TMP_Text[5];
        [SerializeField] private Image displayFriendImage;
        [SerializeField] private TMP_Text displayWordText;
        [SerializeField] private Button startQuizButton;

        [Header("Phase 3: Which House Quiz UI")]
        [SerializeField] private GameObject quizPanel;
        [SerializeField] private TMP_Text quizPromptText;
        [SerializeField] private Image quizPromptImage;
        [SerializeField] private Button[] quizHouseButtons = new Button[5];
        [SerializeField] private TMP_Text[] quizHouseTexts = new TMP_Text[5];

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

        [Header("SFX Feedback Clips")]
        [SerializeField] private AudioClip correctChimeSfx;
        [SerializeField] private AudioClip retryGentleSfx;
        [SerializeField] private AudioClip starPopSfx;

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentQuizIndex = 0;
        private int totalQuizRounds = 10;
        private bool isQuizActive = false;
        private bool isTransitioning = false;
        private WhichHouseQuizRound currentQuizRound;
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
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
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
            if (startQuizButton != null) startQuizButton.onClick.AddListener(StartQuizRound);

            for (int i = 0; i < vowelHouseButtons.Length; i++)
            {
                int index = i;
                if (vowelHouseButtons[i] != null)
                    vowelHouseButtons[i].onClick.AddListener(() => OnVowelHouseTapped(index));
            }

            for (int i = 0; i < quizHouseButtons.Length; i++)
            {
                int index = i;
                if (quizHouseButtons[i] != null)
                    quizHouseButtons[i].onClick.AddListener(() => OnQuizHouseSelected(index));
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
            currentQuizIndex = 0;
            isQuizActive = false;
            isTransitioning = false;

            if (streetPanel != null) streetPanel.SetActive(true);
            if (quizPanel != null) quizPanel.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (continueButton != null) continueButton.SetActive(false);

            SetupStreetHouses();
            UpdateProgressUI(0f);
            StartCoroutine(PlayIntroSequence());
        }

        private IEnumerator PlayIntroSequence()
        {
            isTransitioning = true;
            SetDialogue("Welcome to Short Vowel Street! Five friends live here, and each one has ONE short sound.");

            if (activityData != null && activityData.introVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.introVoiceClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            if (startQuizButton != null) startQuizButton.gameObject.SetActive(true);
            isTransitioning = false;
        }

        private void SetupStreetHouses()
        {
            if (activityData == null || activityData.vowelFriends == null) return;

            string[] defaultLabels = new string[] { "ă", "ĕ", "ĭ", "ŏ", "ŭ" };
            for (int i = 0; i < vowelHouseButtons.Length; i++)
            {
                if (i < activityData.vowelFriends.Length)
                {
                    vowelHouseButtons[i].gameObject.SetActive(true);
                    ShortVowelFriendItem friend = activityData.vowelFriends[i];

                    if (vowelHouseTexts[i] != null)
                        vowelHouseTexts[i].text = !string.IsNullOrEmpty(friend.vowelChar) ? friend.vowelChar : defaultLabels[i];
                }
            }
        }

        private void OnVowelHouseTapped(int index)
        {
            if (isTransitioning || IsAudioPlaying() || activityData == null || index >= activityData.vowelFriends.Length) return;

            ShortVowelFriendItem friend = activityData.vowelFriends[index];
            PlaySFX(correctChimeSfx);
            SetDialogue($"Short Vowel: /{friend.vowelChar}/ - {friend.wordName.ToUpper()}! ({friend.actionDescription})");

            if (displayFriendImage != null)
            {
                displayFriendImage.gameObject.SetActive(true);
                if (friend.pictureSprite != null) displayFriendImage.sprite = friend.pictureSprite;
            }

            if (displayWordText != null)
            {
                displayWordText.gameObject.SetActive(true);
                displayWordText.text = friend.wordName.ToUpper();
            }

            if (friend.vowelSoundClip != null)
            {
                PlayVoiceClipNonBlocking(friend.vowelSoundClip);
            }

            StartCoroutine(SwapToActionSpriteSequence(friend));
        }

        private IEnumerator SwapToActionSpriteSequence(ShortVowelFriendItem friend)
        {
            yield return new WaitForSeconds(0.8f);
            if (displayFriendImage != null && friend != null)
            {
                if (friend.actionSprite != null)
                {
                    displayFriendImage.sprite = friend.actionSprite;
                    TriggerWiggle(displayFriendImage.rectTransform);
                }
            }
        }

        private void StartQuizRound()
        {
            isQuizActive = true;
            currentQuizIndex = 0;

            if (streetPanel != null) streetPanel.SetActive(false);
            if (quizPanel != null) quizPanel.SetActive(true);

            LoadQuizRound(0);
        }

        private void LoadQuizRound(int index)
        {
            if (activityData == null || activityData.quizRounds == null || index >= activityData.quizRounds.Length)
            {
                StartCoroutine(CompleteQuizSequence());
                return;
            }

            currentQuizIndex = index;
            failAttempts = 0;
            StopMomoPulse();
            if (momoHintObject != null) momoHintObject.SetActive(false);

            currentQuizRound = activityData.quizRounds[index];

            if (quizPromptText != null) quizPromptText.text = $"Word: {currentQuizRound.targetWord.ToUpper()}";
            if (quizPromptImage != null && currentQuizRound.promptSprite != null)
                quizPromptImage.sprite = currentQuizRound.promptSprite;

            SetupQuizHouses();
            UpdateProgressUI((float)index / totalQuizRounds);

            StartCoroutine(PlayQuizRoundAudioSequence());
        }

        private IEnumerator PlayQuizRoundAudioSequence()
        {
            isTransitioning = true;
            SetDialogue("Which friend do you hear in the middle? Tap their house!");

            if (activityData != null && activityData.quizInstructionClip != null)
            {
                yield return PlayVoiceClip(activityData.quizInstructionClip);
            }

            if (currentQuizRound != null && currentQuizRound.wordNormalClip != null)
            {
                SetDialogue($"Listen: '{currentQuizRound.targetWord.ToUpper()}'! Which friend is in the middle?");
                yield return PlayVoiceClip(currentQuizRound.wordNormalClip);
            }
            else
            {
                yield return new WaitForSeconds(0.5f);
            }

            isTransitioning = false;
        }

        private void SetupQuizHouses()
        {
            string[] labels = new string[] { "ă", "ĕ", "ĭ", "ŏ", "ŭ" };
            for (int i = 0; i < quizHouseButtons.Length; i++)
            {
                quizHouseButtons[i].gameObject.SetActive(true);
                quizHouseButtons[i].transform.localScale = Vector3.one;

                Image btnImg = quizHouseButtons[i].GetComponent<Image>();
                if (btnImg != null) btnImg.color = Color.white;

                if (quizHouseTexts[i] != null) quizHouseTexts[i].text = labels[i];
            }
        }

        private void OnQuizHouseSelected(int houseIndex)
        {
            if (isTransitioning || IsAudioPlaying() || currentQuizRound == null) return;

            bool isCorrect = (houseIndex == currentQuizRound.correctVowelIndex);
            Button tappedBtn = (houseIndex >= 0 && houseIndex < quizHouseButtons.Length) ? quizHouseButtons[houseIndex] : null;

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
                StartCoroutine(HandleQuizCorrect());
            }
            else
            {
                failAttempts++;
                if (failAttempts >= 2 && momoHintObject != null)
                {
                    momoHintObject.SetActive(true);
                    SetDialogue("Momo says: Tap the glowing house!");
                    TriggerMomoCorrectAnswerPulse();
                }
                StartCoroutine(HandleQuizWrong());
            }
        }

        private IEnumerator ResetButtonColor(Image targetImage, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (targetImage != null) targetImage.color = Color.white;
        }

        private IEnumerator HandleQuizCorrect()
        {
            isTransitioning = true;
            PlaySFX(correctChimeSfx);
            TriggerWiggleStarMeter();

            SetDialogue($"Yes! You found the right house for '{currentQuizRound.targetWord}'!");
            yield return new WaitForSeconds(0.8f);

            currentQuizIndex++;
            isTransitioning = false;

            if (currentQuizIndex < totalQuizRounds && currentQuizIndex < activityData.quizRounds.Length)
            {
                LoadQuizRound(currentQuizIndex);
            }
            else
            {
                StartCoroutine(CompleteQuizSequence());
            }
        }

        private IEnumerator HandleQuizWrong()
        {
            PlaySFX(retryGentleSfx);
            if (currentQuizRound != null && currentQuizRound.wordMiddleStretchedClip != null)
            {
                PlayVoiceClipNonBlocking(currentQuizRound.wordMiddleStretchedClip);
            }
            SetDialogue("Listen again! Which vowel is in the middle?");
            yield return new WaitForSeconds(0.6f);
        }

        private IEnumerator CompleteQuizSequence()
        {
            if (quizPanel != null) quizPanel.SetActive(false);
            UpdateProgressUI(1f);

            SetDialogue("You know all five short friends!");
            if (activityData != null && activityData.quizSuccessClip != null)
            {
                yield return PlayVoiceClip(activityData.quizSuccessClip);
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
        }

        private void TriggerMomoCorrectAnswerPulse()
        {
            StopMomoPulse();
            if (currentQuizRound != null && currentQuizRound.correctVowelIndex >= 0 && currentQuizRound.correctVowelIndex < quizHouseButtons.Length)
            {
                Button correctBtn = quizHouseButtons[currentQuizRound.correctVowelIndex];
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

            if (quizHouseButtons != null)
            {
                for (int i = 0; i < quizHouseButtons.Length; i++)
                {
                    if (quizHouseButtons[i] != null)
                        quizHouseButtons[i].transform.localScale = Vector3.one;
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
