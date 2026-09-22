using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit6
{
    public class SortingCaveController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit6";
        [SerializeField] private string topicName = "SortingCave";

        [Header("Data Asset")]
        [SerializeField] private SortingCaveData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Header / Dialogue UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Phase 1: Crystal Bins Sorting UI (12 Rounds)")]
        [SerializeField] private GameObject crystalSortingPanel;
        [SerializeField] private RectTransform buzzCrystalSlot;       // Glowing Buzz Crystal drop target
        [SerializeField] private RectTransform whisperCrystalSlot;    // Pale Whisper Crystal drop target
        [SerializeField] private GameObject buzzCrystalGlowEffect;
        [SerializeField] private GameObject whisperCrystalGlowEffect;
        [SerializeField] private SortingCaveCard activeCardUI;        // The sound card to drag
        [SerializeField] private Button replaySoundCardButton;
        [SerializeField] private Button startStarRoundButton;         // Appears only after 12 rounds

        [Header("Phase 2: Tara Star Round UI (6 Challenges)")]
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

        [Header("Rewards & Progression")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentCardIndex = 0;
        private int starChallengeIndex = 0;
        private bool isStarRoundActive = false;
        private bool isTransitioning = false;
        private Camera mainCamera;
        private ConsonantSoundCardItem currentCardItem;
        private StarRoundUnit6Challenge currentStarChallenge;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            mainCamera = Camera.main;
            EnsureAudioSources();
            EnsureDataAssigned();
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
                activityData = Resources.Load<SortingCaveData>("Phonics2/Unit6/SortingCaveData_Unit6");
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

        private void SetupButtonListeners()
        {
            if (replaySoundCardButton != null) replaySoundCardButton.onClick.AddListener(ReplayActiveSound);
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
            currentCardIndex = 0;
            starChallengeIndex = 0;
            isStarRoundActive = false;
            isTransitioning = false;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.gameObject.SetActive(false);
            if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(false);
            if (momoHintObject != null) momoHintObject.SetActive(false);

            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (taraMascotObject != null) taraMascotObject.SetActive(false);

            if (crystalSortingPanel != null) crystalSortingPanel.SetActive(true);
            if (starRoundPanel != null) starRoundPanel.SetActive(false);

            StartCoroutine(StartActivityIntroSequence());
        }

        private IEnumerator StartActivityIntroSequence()
        {
            isTransitioning = true;
            SetDialogue("Hand on your throat — then put each sound in the right crystal!");
            if (activityData != null && activityData.leoIntroClip != null)
            {
                yield return PlayVoiceClip(activityData.leoIntroClip);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            isTransitioning = false;
            LoadSortingCard(0);
        }

        #region Phase 1: Crystal Bins Sorting (12 Rounds)

        private void LoadSortingCard(int index)
        {
            if (activityData == null || activityData.sessionCards == null || index >= activityData.sessionCards.Length)
            {
                CompleteCrystalSortingPhase();
                return;
            }

            currentCardIndex = index;
            isTransitioning = false;
            if (momoHintObject != null) momoHintObject.SetActive(false);

            currentCardItem = activityData.sessionCards[index];

            if (activeCardUI != null)
            {
                activeCardUI.SetupCard(currentCardItem, this);
                activeCardUI.gameObject.SetActive(true);
            }

            SetDialogue($"Listen: {currentCardItem.soundSymbol} {currentCardItem.wordName}. Buzz crystal, or whisper crystal?");
            if (currentCardItem.soundClip != null)
            {
                PlayVoiceClipNonBlocking(currentCardItem.soundClip);
            }

            UpdateProgressUI((index / 12f) * 0.5f);
        }

        private void ReplayActiveSound()
        {
            if (currentCardItem != null && currentCardItem.soundClip != null)
            {
                PlayVoiceClipNonBlocking(currentCardItem.soundClip);
            }
        }

        public void EvaluateCardDrop(SortingCaveCard card, PointerEventData eventData)
        {
            if (isTransitioning || card == null || activityData == null || currentCardIndex >= activityData.sessionCards.Length)
            {
                if (card != null) card.ReturnToStartPosition();
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera cam = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : mainCamera;

            bool hitBuzz = false;
            bool hitWhisper = false;

            if (buzzCrystalSlot != null)
            {
                hitBuzz = RectTransformUtility.RectangleContainsScreenPoint(buzzCrystalSlot, eventData.position, cam);
            }

            if (whisperCrystalSlot != null)
            {
                hitWhisper = RectTransformUtility.RectangleContainsScreenPoint(whisperCrystalSlot, eventData.position, cam);
            }

            if (!hitBuzz && !hitWhisper)
            {
                card.ReturnToStartPosition();
                return;
            }

            bool chosenIsBuzzer = hitBuzz;
            bool isCorrect = (chosenIsBuzzer == currentCardItem.isBuzzer);

            if (isCorrect)
            {
                RectTransform targetSlot = chosenIsBuzzer ? buzzCrystalSlot : whisperCrystalSlot;
                StartCoroutine(HandleCardSortCorrect(card, chosenIsBuzzer, targetSlot));
            }
            else
            {
                card.ReturnToStartPosition();
                StartCoroutine(HandleCardSortWrong());
            }
        }

        private IEnumerator HandleCardSortCorrect(SortingCaveCard card, bool isBuzz, RectTransform targetSlot)
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.cardSnapSfx : null);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            if (card != null)
            {
                card.AnimateAbsorbIntoCrystal(targetSlot, null);
            }

            if (isBuzz && buzzCrystalGlowEffect != null) StartCoroutine(PopScaleRoutine(buzzCrystalGlowEffect.transform, 1.3f, 0.35f));
            if (!isBuzz && whisperCrystalGlowEffect != null) StartCoroutine(PopScaleRoutine(whisperCrystalGlowEffect.transform, 1.3f, 0.35f));

            if (currentCardItem.isBuzzer)
            {
                SetDialogue($"Yes! {currentCardItem.soundSymbol} buzzes — {currentCardItem.wordName}!");
            }
            else
            {
                SetDialogue($"Yes! {currentCardItem.soundSymbol} is all air — {currentCardItem.wordName}! Same mouth, no buzz.");
            }

            if (currentCardItem.successClip != null)
            {
                yield return PlayVoiceClip(currentCardItem.successClip);
            }
            else if (currentCardItem.wordClip != null)
            {
                yield return PlayVoiceClip(currentCardItem.wordClip);
            }
            else
            {
                yield return new WaitForSeconds(0.9f);
            }

            if (card != null) card.HideCard();

            currentCardIndex++;
            isTransitioning = false;
            LoadSortingCard(currentCardIndex);
        }

        private IEnumerator HandleCardSortWrong()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.retryGentleSfx : null);

            SetDialogue($"Try it again with your hand on your throat. {currentCardItem.soundSymbol} {currentCardItem.wordName}…");
            if (activityData != null && activityData.floatBackRetryClip != null)
            {
                yield return PlayVoiceClip(activityData.floatBackRetryClip);
            }
            else
            {
                yield return new WaitForSeconds(1.2f);
            }

            isTransitioning = false;
        }

        private void CompleteCrystalSortingPhase()
        {
            if (startStarRoundButton != null)
            {
                startStarRoundButton.gameObject.SetActive(true);
            }

            SetDialogue("Crystal cave sorted! Tap 'Start Star Round' to continue with Tara! ⭐");
        }

        #endregion

        #region Phase 2: Tara Star Round (6 Challenges)

        private void StartStarRound()
        {
            if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(false);
            if (crystalSortingPanel != null) crystalSortingPanel.SetActive(false);
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
            isTransitioning = false;
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

        private readonly Color correctGreenColor = new Color(0.298f, 0.686f, 0.314f, 1f); // #4CAF50
        private readonly Color wrongRedColor = new Color(0.937f, 0.325f, 0.314f, 1f);     // #EF5350

        private void OnStarChoiceSelected(int choiceIndex)
        {
            if (isTransitioning || currentStarChallenge == null) return;

            bool isCorrect = (choiceIndex == currentStarChallenge.correctChoiceIndex);
            if (starChoiceButtons != null && choiceIndex < starChoiceButtons.Length && starChoiceButtons[choiceIndex] != null)
            {
                StartCoroutine(FlashButtonFeedback(starChoiceButtons[choiceIndex], isCorrect, 0.6f));
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

            SetDialogue("Roar! You got it right!");
            yield return new WaitForSeconds(0.8f);

            starChallengeIndex++;
            isTransitioning = false;
            LoadStarChallenge(starChallengeIndex);
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

        private IEnumerator CompleteStarRoundSequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            UpdateProgressUI(1.0f);

            SetDialogue("You can feel every sound you make. You are a SOUND BUZZER!");
            if (activityData != null && activityData.badgeVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.badgeVoiceClip);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);

            yield return new WaitForSeconds(1.5f);

            SetDialogue("Unit Seven is open! Next time — the beginning, the middle and the END of every word.");
            if (activityData != null && activityData.unit7UnlockVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.unit7UnlockVoiceClip);
            }

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            if (continueButton != null) continueButton.gameObject.SetActive(true);
            isTransitioning = false;
        }

        #endregion

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
