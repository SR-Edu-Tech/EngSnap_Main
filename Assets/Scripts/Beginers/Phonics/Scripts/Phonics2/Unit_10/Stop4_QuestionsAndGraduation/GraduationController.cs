using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit10
{
    public class GraduationController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit10";
        [SerializeField] private string topicName = "Graduation";

        [Header("Data Asset")]
        [SerializeField] private GraduationData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Dialogue / Subtitle UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Phase 1: Pinned Story & Questions UI")]
        [SerializeField] private GameObject questionsPanel;
        [SerializeField] private GameObject pinnedStoryPanel;
        [SerializeField] private TMP_Text[] pinnedStoryLineTexts = new TMP_Text[8];
        [SerializeField] private Image[] pinnedStoryLineHighlights = new Image[8];
        [SerializeField] private TMP_Text questionPromptTMP;
        [SerializeField] private Button[] questionChoiceButtons = new Button[3];
        [SerializeField] private TMP_Text[] questionChoiceTexts = new TMP_Text[3];
        [SerializeField] private Image[] questionChoiceImages = new Image[3];

        [Header("Phase 2: Sound Island Graduation Ceremony UI")]
        [SerializeField] private GameObject graduationPanel;
        [SerializeField] private Image[] islandWorldGlowImages = new Image[10];
        [SerializeField] private GameObject allFourMascotsContainer;
        [SerializeField] private GameObject leoMascot;
        [SerializeField] private GameObject gigiMascot;
        [SerializeField] private GameObject taraMascot;
        [SerializeField] private GameObject momoMascot;
        [SerializeField] private GameObject fireworksParticleSystem;

        [Header("Phase 3: Final Certificate & Badge Modals")]
        [SerializeField] private GameObject phonicsChampionBadgePopup;
        [SerializeField] private GameObject certificateModalPopup;
        [SerializeField] private TMP_Text certificateChildNameTMP;
        [SerializeField] private TMP_Text certificateWordCountTMP;
        [SerializeField] private Button finishGameButton;

        [Header("Progress UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;

        [Header("Rewards & Navigation")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentQuestionIndex = 0;
        private bool isTransitioning = false;
        private bool hasInitialized = false;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            EnsureAudioSources();
            EnsureDataAssigned();
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
                activityData = Resources.Load<GraduationData>("Phonics2/Unit10/GraduationData_Unit10");
            }
            if (activityData == null)
            {
                activityData = ScriptableObject.CreateInstance<GraduationData>();
                activityData.PopulateDefaultData();
            }
        }

        private void SetupButtonListeners()
        {
            for (int i = 0; i < questionChoiceButtons.Length; i++)
            {
                int index = i;
                if (questionChoiceButtons[i] != null)
                {
                    questionChoiceButtons[i].onClick.RemoveAllListeners();
                    questionChoiceButtons[i].onClick.AddListener(() => OnQuestionChoiceClicked(index));
                }
            }

            if (finishGameButton != null)
            {
                finishGameButton.onClick.RemoveAllListeners();
                finishGameButton.onClick.AddListener(GoToNextPanel);
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
            currentQuestionIndex = 0;
            isTransitioning = false;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (graduationPanel != null) graduationPanel.SetActive(false);
            if (phonicsChampionBadgePopup != null) phonicsChampionBadgePopup.SetActive(false);
            if (certificateModalPopup != null) certificateModalPopup.SetActive(false);
            if (fireworksParticleSystem != null) fireworksParticleSystem.SetActive(false);

            if (questionsPanel != null) questionsPanel.SetActive(true);
            if (leoMascot != null) leoMascot.SetActive(true);

            PopulatePinnedStoryText();
            StartCoroutine(RunActivityIntroRoutine());
        }

        private void PopulatePinnedStoryText()
        {
            if (activityData == null || activityData.pinnedStoryLines == null) return;

            for (int i = 0; i < pinnedStoryLineTexts.Length; i++)
            {
                if (pinnedStoryLineTexts[i] != null)
                {
                    bool inStory = i < activityData.pinnedStoryLines.Length;
                    pinnedStoryLineTexts[i].gameObject.SetActive(inStory);
                    if (inStory) pinnedStoryLineTexts[i].text = activityData.pinnedStoryLines[i];
                }
                if (pinnedStoryLineHighlights != null && i < pinnedStoryLineHighlights.Length && pinnedStoryLineHighlights[i] != null)
                {
                    pinnedStoryLineHighlights[i].gameObject.SetActive(false);
                }
            }
        }

        private IEnumerator RunActivityIntroRoutine()
        {
            isTransitioning = true;
            SetDialogue("Now — did you understand it? The story is right there if you want to look again.");
            if (activityData != null && activityData.questionIntroClip != null)
            {
                yield return PlayVoiceClip(activityData.questionIntroClip);
            }
            else
            {
                yield return new WaitForSeconds(2.0f);
            }

            isTransitioning = false;
            LoadQuestion(0);
        }

        private void LoadQuestion(int index)
        {
            if (activityData == null || activityData.questions == null || index >= activityData.questions.Length)
            {
                StartGraduationCeremony();
                return;
            }

            currentQuestionIndex = index;
            var q = activityData.questions[index];

            ClearStoryHighlights();

            if (questionPromptTMP != null) questionPromptTMP.text = q.questionPrompt;
            SetDialogue(q.questionPrompt);

            if (q.questionAudioClip != null)
            {
                PlayVoiceClipNonBlocking(q.questionAudioClip);
            }

            for (int i = 0; i < questionChoiceButtons.Length; i++)
            {
                if (questionChoiceButtons[i] != null)
                {
                    bool active = q.choices != null && i < q.choices.Length;
                    questionChoiceButtons[i].gameObject.SetActive(active);

                    if (active && questionChoiceTexts != null && i < questionChoiceTexts.Length && questionChoiceTexts[i] != null)
                    {
                        questionChoiceTexts[i].text = q.choices[i];
                    }
                    if (active && questionChoiceImages != null && i < questionChoiceImages.Length && questionChoiceImages[i] != null)
                    {
                        bool hasSprite = q.choiceSprites != null && i < q.choiceSprites.Length && q.choiceSprites[i] != null;
                        questionChoiceImages[i].gameObject.SetActive(hasSprite);
                        if (hasSprite) questionChoiceImages[i].sprite = q.choiceSprites[i];
                    }
                }
            }

            UpdateProgressUI((float)index / 6f);
        }

        private void OnQuestionChoiceClicked(int index)
        {
            if (isTransitioning || activityData == null || currentQuestionIndex >= activityData.questions.Length) return;

            var q = activityData.questions[currentQuestionIndex];
            StartCoroutine(CheckQuestionRoutine(index, q));
        }

        private IEnumerator CheckQuestionRoutine(int index, GraduationQuestionItem q)
        {
            isTransitioning = true;
            bool isCorrect = (index == q.correctChoiceIndex);

            RectTransform choiceRect = null;
            Image choiceBgImage = null;
            if (questionChoiceButtons != null && index >= 0 && index < questionChoiceButtons.Length && questionChoiceButtons[index] != null)
            {
                choiceRect = questionChoiceButtons[index].GetComponent<RectTransform>();
                choiceBgImage = questionChoiceButtons[index].GetComponent<Image>();
            }

            if (isCorrect)
            {
                PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
                TriggerWiggleStarMeter();

                if (choiceRect != null)
                {
                    StartCoroutine(PopBounceChoiceRoutine(choiceRect, 0.35f, 1.18f, choiceBgImage, new Color(0.75f, 0.95f, 0.75f, 1f)));
                }

                SetDialogue($"Yes! {q.choices[q.correctChoiceIndex]}!");
                if (q.praiseAudioClip != null)
                {
                    yield return PlayVoiceClip(q.praiseAudioClip);
                }
                else
                {
                    yield return new WaitForSeconds(1.0f);
                }

                currentQuestionIndex++;
                isTransitioning = false;
                LoadQuestion(currentQuestionIndex);
            }
            else
            {
                PlaySFX(activityData != null ? activityData.retryGentleSfx : null);

                // Wiggle / horizontal shake wrong answer button with soft red flash!
                if (choiceRect != null)
                {
                    StartCoroutine(ShakeWiggleRoutine(choiceRect, 0.45f, 15f, choiceBgImage, new Color(1f, 0.7f, 0.7f, 1f)));
                }

                // Highlight and pulse the clue line in the pinned story!
                if (q.storyReferenceLineIndex >= 0 && q.storyReferenceLineIndex < pinnedStoryLineHighlights.Length && pinnedStoryLineHighlights[q.storyReferenceLineIndex] != null)
                {
                    pinnedStoryLineHighlights[q.storyReferenceLineIndex].gameObject.SetActive(true);
                    if (pinnedStoryLineTexts != null && q.storyReferenceLineIndex < pinnedStoryLineTexts.Length && pinnedStoryLineTexts[q.storyReferenceLineIndex] != null)
                    {
                        StartCoroutine(PopScaleRoutine(pinnedStoryLineTexts[q.storyReferenceLineIndex].transform, 1.12f, 0.35f));
                    }
                }

                SetDialogue("Let us look again — the answer is hiding in this line.");
                if (activityData != null && activityData.wrongAnswerClueClip != null)
                {
                    yield return PlayVoiceClip(activityData.wrongAnswerClueClip);
                }
                else
                {
                    yield return new WaitForSeconds(1.5f);
                }

                isTransitioning = false;
            }
        }

        private void ClearStoryHighlights()
        {
            if (pinnedStoryLineHighlights == null) return;
            for (int i = 0; i < pinnedStoryLineHighlights.Length; i++)
            {
                if (pinnedStoryLineHighlights[i] != null)
                    pinnedStoryLineHighlights[i].gameObject.SetActive(false);
            }
        }

        #region Phase 2: Grand Graduation Ceremony

        private void StartGraduationCeremony()
        {
            StartCoroutine(RunGraduationCeremonyRoutine());
        }

        private IEnumerator RunGraduationCeremonyRoutine()
        {
            isTransitioning = true;
            if (questionsPanel != null) questionsPanel.SetActive(false);
            if (graduationPanel != null) graduationPanel.SetActive(true);

            SetDialogue("Five out of five. You read it AND you understood it!");
            if (activityData != null && activityData.questionsCompleteClip != null)
            {
                yield return PlayVoiceClip(activityData.questionsCompleteClip);
            }

            // 1. All four mascots appear cheering!
            if (allFourMascotsContainer != null) allFourMascotsContainer.SetActive(true);
            if (leoMascot != null) leoMascot.SetActive(true);
            if (gigiMascot != null) gigiMascot.SetActive(true);
            if (taraMascot != null) taraMascot.SetActive(true);
            if (momoMascot != null) momoMascot.SetActive(true);

            SetDialogue("Hooray! Hooray!");
            if (activityData != null && activityData.allMascotsCheerClip != null)
            {
                yield return PlayVoiceClip(activityData.allMascotsCheerClip);
            }

            // 2. Light up Sound Island world by world (1 to 10)
            if (islandWorldGlowImages != null)
            {
                for (int w = 0; w < islandWorldGlowImages.Length; w++)
                {
                    if (islandWorldGlowImages[w] != null)
                    {
                        islandWorldGlowImages[w].gameObject.SetActive(true);
                        StartCoroutine(PopScaleRoutine(islandWorldGlowImages[w].transform, 1.3f, 0.25f));
                        PlaySFX(activityData != null ? activityData.islandWorldLightUpSfx : null);
                    }
                    yield return new WaitForSeconds(0.3f);
                }
            }

            // 3. Fireworks & Island Awake Dialogue
            if (fireworksParticleSystem != null) fireworksParticleSystem.SetActive(true);
            if (confettiParticles != null) confettiParticles.SetActive(true);
            PlaySFX(activityData != null ? activityData.fireworksBoomSfx : null);

            SetDialogue("Look at the island! Every sound is home. YOU woke up Sound Island.");
            if (activityData != null && activityData.islandAwakeClip != null)
            {
                yield return PlayVoiceClip(activityData.islandAwakeClip);
            }

            // 4. Phonics Champion Badge
            if (phonicsChampionBadgePopup != null) phonicsChampionBadgePopup.SetActive(true);
            SetDialogue("You started by listening. Now you can read. You are a PHONICS CHAMPION!");
            if (activityData != null && activityData.phonicsChampionBadgeClip != null)
            {
                yield return PlayVoiceClip(activityData.phonicsChampionBadgeClip);
            }

            yield return new WaitForSeconds(1.5f);

            // 5. Printable Certificate Modal
            if (certificateModalPopup != null)
            {
                certificateModalPopup.SetActive(true);
                if (certificateChildNameTMP != null) certificateChildNameTMP.text = "Star Reader";
                if (certificateWordCountTMP != null) certificateWordCountTMP.text = "100+ Words Mastered";
            }

            SetDialogue("Here is your certificate. Show somebody — and then let us read a story again, just for fun.");
            if (activityData != null && activityData.certificateFinalClip != null)
            {
                yield return PlayVoiceClip(activityData.certificateFinalClip);
            }

            PlaySFX(activityData != null ? activityData.finalCertificateFanfareSfx : null);
            UpdateProgressUI(1.0f);

            if (continueButton != null) continueButton.SetActive(true);
            if (finishGameButton != null) finishGameButton.gameObject.SetActive(true);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            isTransitioning = false;
        }

        #endregion

        public void DeactivateMascots()
        {
            if (leoMascot != null) leoMascot.SetActive(false);
            if (gigiMascot != null) gigiMascot.SetActive(false);
            if (taraMascot != null) taraMascot.SetActive(false);
            if (momoMascot != null) momoMascot.SetActive(false);
            if (allFourMascotsContainer != null) allFourMascotsContainer.SetActive(false);
        }

        public void GoToNextPanel()
        {
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (phonicsChampionBadgePopup != null) phonicsChampionBadgePopup.SetActive(false);
            if (certificateModalPopup != null) certificateModalPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (fireworksParticleSystem != null) fireworksParticleSystem.SetActive(false);
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

        private IEnumerator ShakeWiggleRoutine(RectTransform target, float duration, float shakeAmount, Image cardImage, Color flashColor)
        {
            if (target == null) yield break;

            Vector2 originalPos = target.anchoredPosition;
            Vector3 originalRot = target.localEulerAngles;
            Color originalColor = cardImage != null ? cardImage.color : Color.white;

            if (cardImage != null) cardImage.color = flashColor;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float decay = 1f - (elapsed / duration);
                float xOffset = Mathf.Sin(elapsed * 45f) * shakeAmount * decay;
                float zRot = Mathf.Sin(elapsed * 35f) * 4.5f * decay;

                target.anchoredPosition = originalPos + new Vector2(xOffset, 0f);
                target.localEulerAngles = new Vector3(0f, 0f, zRot);

                if (cardImage != null)
                {
                    cardImage.color = Color.Lerp(flashColor, originalColor, elapsed / duration);
                }

                yield return null;
            }

            target.anchoredPosition = originalPos;
            target.localEulerAngles = Vector3.zero;
            if (cardImage != null) cardImage.color = originalColor;
        }

        private IEnumerator PopBounceChoiceRoutine(RectTransform target, float duration, float maxScaleMul, Image cardImage, Color flashColor)
        {
            if (target == null) yield break;

            Vector3 startScale = target.localScale;
            Vector3 maxScale = startScale * maxScaleMul;
            Color originalColor = cardImage != null ? cardImage.color : Color.white;
            if (cardImage != null) cardImage.color = flashColor;

            float half = duration * 0.5f;
            float elapsed = 0f;

            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                target.localScale = Vector3.Lerp(startScale, maxScale, t);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / half;
                target.localScale = Vector3.Lerp(maxScale, startScale, t);
                if (cardImage != null)
                {
                    cardImage.color = Color.Lerp(flashColor, originalColor, t);
                }
                yield return null;
            }

            target.localScale = startScale;
            if (cardImage != null) cardImage.color = originalColor;
        }
    }
}
