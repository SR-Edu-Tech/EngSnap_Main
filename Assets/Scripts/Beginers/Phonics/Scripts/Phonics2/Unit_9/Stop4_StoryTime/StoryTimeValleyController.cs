using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;
using EngSnap.Phonics2.Unit8;

namespace EngSnap.Phonics2.Unit9
{
    public class StoryTimeValleyController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit9";
        [SerializeField] private string topicName = "StoryTime";

        [Header("Story Selection")]
        [Tooltip("0 = Story i (The Big Pig), 1 = Story o (Tom's Dog), 2 = Story u (The Bug)")]
        [SerializeField] private int activeStoryIndex = 0;

        [Header("Data Asset")]
        [SerializeField] private StoryTimeValleyData activityData;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource voiceAudioSource;
        [SerializeField] private AudioSource sfxAudioSource;

        [Header("Dialogue / Subtitle UI")]
        [SerializeField] private TMP_Text dialogueText;
        [SerializeField] private CanvasGroup dialogueCanvasGroup;

        [Header("Phase 1: Story Panel UI")]
        [SerializeField] private GameObject storyPanel;
        [SerializeField] private TMP_Text storyTitleTMP;
        [SerializeField] private Image storyIllustrationImage;
        [SerializeField] private TMP_Text currentLineTMP;
        [SerializeField] private Button[] wordButtonsInLine = new Button[10];
        [SerializeField] private TMP_Text[] wordTextsInLine = new TMP_Text[10];
        [SerializeField] private Image[] wordHighlightsInLine = new Image[10];
        [SerializeField] private Button readLineSpeakerButton;
        [SerializeField] private Button nextLineTickButton;

        [Header("Phase 2: Picture Comprehension Questions")]
        [SerializeField] private GameObject comprehensionPanel;
        [SerializeField] private TMP_Text questionPromptTMP;
        [SerializeField] private Button[] questionChoiceButtons = new Button[3];
        [SerializeField] private TMP_Text[] questionChoiceTexts = new TMP_Text[3];
        [SerializeField] private Image[] questionChoiceImages = new Image[3];

        [Header("Phase 3: Tara Star Round UI (6 Challenges)")]
        [SerializeField] private GameObject starRoundPanel;
        [SerializeField] private TMP_Text starChallengePromptTMP;
        [SerializeField] private Button[] starChoiceButtons = new Button[3];
        [SerializeField] private TMP_Text[] starChoiceTexts = new TMP_Text[3];
        [SerializeField] private Button replayStarAudioButton;
        [SerializeField] private Button startStarRoundButton;

        [Header("Progress & Mascot UI")]
        [SerializeField] private Image progressRingFillImage;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private RectTransform starMeterRect;
        [SerializeField] private GameObject leoMascotObject;
        [SerializeField] private GameObject taraMascotObject;

        [Header("Rewards & Unit 10 Unlock")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;
        [SerializeField] private GameObject badgePopup;
        [SerializeField] private GameObject unit10UnlockPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentLineIndex = 0;
        private int currentQuestionIndex = 0;
        private int currentStarChallengeIndex = 0;
        private bool isTransitioning = false;
        private bool hasInitialized = false;

        private StoryContentItem currentStory;
        private StoryQuestionItem currentQuestion;
        private Unit8StarChallengeItem currentStarChallenge;

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
                activityData = Resources.Load<StoryTimeValleyData>("Phonics2/Unit9/StoryTimeValleyData_Unit9");
            }
            if (activityData == null)
            {
                activityData = ScriptableObject.CreateInstance<StoryTimeValleyData>();
                activityData.PopulateDefaultData();
            }
        }

        private void SetupButtonListeners()
        {
            if (readLineSpeakerButton != null)
            {
                readLineSpeakerButton.onClick.RemoveAllListeners();
                readLineSpeakerButton.onClick.AddListener(OnReadLineSpeakerClicked);
            }

            if (nextLineTickButton != null)
            {
                nextLineTickButton.onClick.RemoveAllListeners();
                nextLineTickButton.onClick.AddListener(OnNextLineTickClicked);
            }

            if (startStarRoundButton != null)
            {
                startStarRoundButton.onClick.RemoveAllListeners();
                startStarRoundButton.onClick.AddListener(StartStarRoundPhase);
            }

            if (replayStarAudioButton != null)
            {
                replayStarAudioButton.onClick.RemoveAllListeners();
                replayStarAudioButton.onClick.AddListener(ReplayStarAudioPrompt);
            }

            for (int i = 0; i < wordButtonsInLine.Length; i++)
            {
                int index = i;
                if (wordButtonsInLine[i] != null)
                {
                    wordButtonsInLine[i].onClick.RemoveAllListeners();
                    wordButtonsInLine[i].onClick.AddListener(() => OnWordInLineClicked(index));
                }
            }

            for (int i = 0; i < questionChoiceButtons.Length; i++)
            {
                int index = i;
                if (questionChoiceButtons[i] != null)
                {
                    questionChoiceButtons[i].onClick.RemoveAllListeners();
                    questionChoiceButtons[i].onClick.AddListener(() => OnQuestionChoiceSelected(index));
                }
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

        public void ResetLevel()
        {
            StartActivity();
        }

        public void StartActivity()
        {
            StopAllCoroutines();
            currentLineIndex = 0;
            currentQuestionIndex = 0;
            currentStarChallengeIndex = 0;
            isTransitioning = false;

            currentStory = (activeStoryIndex == 0) ? activityData.storyI : (activeStoryIndex == 1) ? activityData.storyO : activityData.storyU;

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (badgePopup != null) badgePopup.SetActive(false);
            if (unit10UnlockPopup != null) unit10UnlockPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (comprehensionPanel != null) comprehensionPanel.SetActive(false);
            if (starRoundPanel != null) starRoundPanel.SetActive(false);
            if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(false);

            if (storyPanel != null) storyPanel.SetActive(true);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (taraMascotObject != null) taraMascotObject.SetActive(false);

            StartCoroutine(RunStoryIntroRoutine());
        }

        private IEnumerator RunStoryIntroRoutine()
        {
            isTransitioning = true;
            if (storyTitleTMP != null && currentStory != null) storyTitleTMP.text = currentStory.storyTitle;

            if (storyIllustrationImage != null && currentStory != null && currentStory.storyIllustrationSprite != null)
            {
                storyIllustrationImage.sprite = currentStory.storyIllustrationSprite;
                storyIllustrationImage.gameObject.SetActive(true);
            }

            SetDialogue("Three stories this time. Try each line by yourself first — I am right here if you need me.");
            if (activityData != null && activityData.leoIntroClip != null)
            {
                yield return PlayVoiceClip(activityData.leoIntroClip);
            }
            else
            {
                yield return new WaitForSeconds(2.0f);
            }

            isTransitioning = false;
            LoadStoryLine(0);
        }

        #region Phase 1: Line by Line

        private void LoadStoryLine(int index)
        {
            if (currentStory == null || currentStory.lines == null || index >= currentStory.lines.Length)
            {
                OnStoryFirstPassFinished();
                return;
            }

            currentLineIndex = index;
            var lineItem = currentStory.lines[index];

            if (currentLineTMP != null) currentLineTMP.text = lineItem.lineText;

            for (int i = 0; i < wordButtonsInLine.Length; i++)
            {
                if (wordButtonsInLine[i] != null)
                {
                    bool active = lineItem.wordTokens != null && i < lineItem.wordTokens.Length;
                    wordButtonsInLine[i].gameObject.SetActive(active);

                    if (active && wordTextsInLine != null && i < wordTextsInLine.Length && wordTextsInLine[i] != null)
                    {
                        wordTextsInLine[i].text = lineItem.wordTokens[i];
                    }
                    if (wordHighlightsInLine != null && i < wordHighlightsInLine.Length && wordHighlightsInLine[i] != null)
                    {
                        wordHighlightsInLine[i].gameObject.SetActive(false);
                    }
                }
            }

            SetDialogue($"Line {index + 1}: Try reading this line! Tap the tick when done.");
            UpdateProgressUI((float)index / (currentStory.lines.Length * 2f));
        }

        private void OnWordInLineClicked(int wordIndex)
        {
            if (currentStory == null || currentStory.lines == null || currentLineIndex >= currentStory.lines.Length) return;

            var lineItem = currentStory.lines[currentLineIndex];
            if (lineItem.wordTokens != null && wordIndex >= 0 && wordIndex < lineItem.wordTokens.Length)
            {
                string word = lineItem.wordTokens[wordIndex];
                SetDialogue(word);

                if (wordHighlightsInLine != null && wordIndex < wordHighlightsInLine.Length && wordHighlightsInLine[wordIndex] != null)
                {
                    wordHighlightsInLine[wordIndex].gameObject.SetActive(true);
                }

                if (wordButtonsInLine != null && wordIndex < wordButtonsInLine.Length && wordButtonsInLine[wordIndex] != null)
                {
                    StartCoroutine(PopScaleRoutine(wordButtonsInLine[wordIndex].transform, 1.2f, 0.25f));
                }
            }
        }

        private void OnReadLineSpeakerClicked()
        {
            if (currentStory == null || currentStory.lines == null || currentLineIndex >= currentStory.lines.Length) return;

            var lineItem = currentStory.lines[currentLineIndex];
            SetDialogue(lineItem.lineText);

            if (lineItem.lineAudioClip != null)
            {
                PlayVoiceClipNonBlocking(lineItem.lineAudioClip);
            }
        }

        private void OnNextLineTickClicked()
        {
            if (isTransitioning) return;

            PlaySFX(activityData != null ? activityData.lineCompleteTickSfx : null);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            TriggerWiggleStarMeter();

            currentLineIndex++;
            LoadStoryLine(currentLineIndex);
        }

        private void OnStoryFirstPassFinished()
        {
            StartCoroutine(RunSecondPassRoutine());
        }

        private IEnumerator RunSecondPassRoutine()
        {
            isTransitioning = true;
            SetDialogue("You read that whole line without any help. Did you notice? Let us read the whole story together!");
            if (activityData != null && activityData.unaidedLinePraiseClip != null)
            {
                yield return PlayVoiceClip(activityData.unaidedLinePraiseClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            if (currentStory != null && currentStory.lines != null)
            {
                for (int l = 0; l < currentStory.lines.Length; l++)
                {
                    LoadStoryLine(l);
                    var line = currentStory.lines[l];
                    if (line.lineAudioClip != null) PlayVoiceClipNonBlocking(line.lineAudioClip);

                    if (line.wordTokens != null)
                    {
                        for (int w = 0; w < line.wordTokens.Length; w++)
                        {
                            if (wordHighlightsInLine != null && w < wordHighlightsInLine.Length && wordHighlightsInLine[w] != null)
                                wordHighlightsInLine[w].gameObject.SetActive(true);

                            yield return new WaitForSeconds(0.45f);

                            if (wordHighlightsInLine != null && w < wordHighlightsInLine.Length && wordHighlightsInLine[w] != null)
                                wordHighlightsInLine[w].gameObject.SetActive(false);
                        }
                    }
                    else
                    {
                        yield return new WaitForSeconds(1.2f);
                    }
                }
            }

            isTransitioning = false;
            StartComprehensionPhase();
        }

        #endregion

        #region Phase 2: Comprehension

        private void StartComprehensionPhase()
        {
            if (storyPanel != null) storyPanel.SetActive(false);
            if (comprehensionPanel != null) comprehensionPanel.SetActive(true);

            currentQuestionIndex = 0;
            LoadQuestion(0);
        }

        private void LoadQuestion(int index)
        {
            if (activityData == null || activityData.comprehensionQuestions == null || index >= activityData.comprehensionQuestions.Length)
            {
                CompleteStoryReadingPhase();
                return;
            }

            currentQuestionIndex = index;
            currentQuestion = activityData.comprehensionQuestions[index];

            if (questionPromptTMP != null) questionPromptTMP.text = currentQuestion.questionPrompt;
            SetDialogue(currentQuestion.questionPrompt);

            for (int i = 0; i < questionChoiceButtons.Length; i++)
            {
                if (questionChoiceButtons[i] != null)
                {
                    bool active = currentQuestion.choiceLabels != null && i < currentQuestion.choiceLabels.Length;
                    questionChoiceButtons[i].gameObject.SetActive(active);

                    if (active && questionChoiceTexts != null && i < questionChoiceTexts.Length && questionChoiceTexts[i] != null)
                    {
                        questionChoiceTexts[i].text = currentQuestion.choiceLabels[i];
                    }
                    if (active && questionChoiceImages != null && i < questionChoiceImages.Length && questionChoiceImages[i] != null)
                    {
                        bool hasSprite = currentQuestion.choiceSprites != null && i < currentQuestion.choiceSprites.Length && currentQuestion.choiceSprites[i] != null;
                        questionChoiceImages[i].gameObject.SetActive(hasSprite);
                        if (hasSprite) questionChoiceImages[i].sprite = currentQuestion.choiceSprites[i];
                    }
                }
            }
        }

        private void OnQuestionChoiceSelected(int index)
        {
            if (isTransitioning || currentQuestion == null) return;
            StartCoroutine(CheckQuestionAnswerRoutine(index));
        }

        private IEnumerator CheckQuestionAnswerRoutine(int index)
        {
            isTransitioning = true;
            bool isCorrect = (index == currentQuestion.correctChoiceIndex);

            if (isCorrect)
            {
                PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
                TriggerWiggleStarMeter();
                SetDialogue("Spot on!");
                yield return new WaitForSeconds(1.0f);

                currentQuestionIndex++;
                isTransitioning = false;
                LoadQuestion(currentQuestionIndex);
            }
            else
            {
                PlaySFX(activityData != null ? activityData.retryGentleSfx : null);
                SetDialogue("Think back to the story!");
                yield return new WaitForSeconds(1.0f);
                isTransitioning = false;
            }
        }

        private void CompleteStoryReadingPhase()
        {
            if (comprehensionPanel != null) comprehensionPanel.SetActive(false);

            if (startStarRoundButton != null)
            {
                startStarRoundButton.gameObject.SetActive(true);
                SetDialogue("Stories completed! Tap Start for Tara's Star Round!");
            }
            else
            {
                StartStarRoundPhase();
            }
        }

        #endregion

        #region Phase 3: Star Round with Tara

        public void StartStarRoundPhase()
        {
            if (comprehensionPanel != null) comprehensionPanel.SetActive(false);
            if (storyPanel != null) storyPanel.SetActive(false);
            if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(false);
            if (starRoundPanel != null) starRoundPanel.SetActive(true);

            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (taraMascotObject != null) taraMascotObject.SetActive(true);

            currentStarChallengeIndex = 0;

            SetDialogue("My turn! Six quick challenges. Ready? Roar!");
            if (activityData != null && activityData.taraStarRoundIntroClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.taraStarRoundIntroClip);
            }

            LoadStarChallenge(0);
        }

        private void LoadStarChallenge(int index)
        {
            if (activityData == null || activityData.starChallenges == null || index >= activityData.starChallenges.Length)
            {
                CompleteStop4();
                return;
            }

            currentStarChallengeIndex = index;
            currentStarChallenge = activityData.starChallenges[index];

            if (starChallengePromptTMP != null) starChallengePromptTMP.text = currentStarChallenge.promptText;
            SetDialogue(currentStarChallenge.promptText);

            if (currentStarChallenge.promptClip != null)
            {
                PlayVoiceClipNonBlocking(currentStarChallenge.promptClip);
            }

            for (int i = 0; i < starChoiceButtons.Length; i++)
            {
                if (starChoiceButtons[i] != null)
                {
                    bool active = currentStarChallenge.choices != null && i < currentStarChallenge.choices.Length;
                    starChoiceButtons[i].gameObject.SetActive(active);

                    if (active && starChoiceTexts != null && i < starChoiceTexts.Length && starChoiceTexts[i] != null)
                    {
                        starChoiceTexts[i].text = currentStarChallenge.choices[i];
                    }
                }
            }

            UpdateProgressUI(0.5f + ((float)index / 6f) * 0.5f);
        }

        private void ReplayStarAudioPrompt()
        {
            if (currentStarChallenge != null && currentStarChallenge.promptClip != null)
            {
                PlayVoiceClipNonBlocking(currentStarChallenge.promptClip);
            }
        }

        private void OnStarChoiceSelected(int index)
        {
            if (isTransitioning || currentStarChallenge == null) return;
            StartCoroutine(CheckStarAnswerRoutine(index));
        }

        private IEnumerator CheckStarAnswerRoutine(int index)
        {
            isTransitioning = true;
            bool isCorrect = (index == currentStarChallenge.correctIndex);

            if (isCorrect)
            {
                PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
                TriggerWiggleStarMeter();

                SetDialogue("Roar! Spot on!");
                yield return new WaitForSeconds(0.8f);

                currentStarChallengeIndex++;
                isTransitioning = false;
                LoadStarChallenge(currentStarChallengeIndex);
            }
            else
            {
                PlaySFX(activityData != null ? activityData.retryGentleSfx : null);
                SetDialogue("Listen to Tara! Give it another try!");
                yield return new WaitForSeconds(1.0f);
                isTransitioning = false;
            }
        }

        #endregion

        #region Final Rewards & Unit 10 Unlock

        private void CompleteStop4()
        {
            StartCoroutine(CompleteStop4Sequence());
        }

        private IEnumerator CompleteStop4Sequence()
        {
            isTransitioning = true;
            PlaySFX(activityData != null ? activityData.badgeUnlockSfx : null);
            PlaySFX(activityData != null ? activityData.correctChimeSfx : null);
            UpdateProgressUI(1.0f);

            SetDialogue("Every vowel, every word. You are a BLENDING CHAMPION!");
            if (activityData != null && activityData.blendingChampionBadgeClip != null)
            {
                yield return PlayVoiceClip(activityData.blendingChampionBadgeClip);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (badgePopup != null) badgePopup.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);

            yield return new WaitForSeconds(1.5f);

            SetDialogue("Unit Ten is open — the last one. Next time you read a real story, all the way through, by yourself.");
            if (activityData != null && activityData.unit10UnlockVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.unit10UnlockVoiceClip);
            }

            if (unit10UnlockPopup != null) unit10UnlockPopup.SetActive(true);
            if (continueButton != null) continueButton.SetActive(true);

            TopicProgressUI.MarkTopicComplete(unitID, topicName, GoToNextPanel);
            TopicProgressUI.ShowTopicCompletePanel(topicName, GoToNextPanel);

            yield return new WaitForSeconds(1.0f);
            isTransitioning = false;
        }

        public void DeactivateMascots()
        {
            if (leoMascotObject != null) leoMascotObject.SetActive(false);
            if (taraMascotObject != null) taraMascotObject.SetActive(false);
        }

        public void GoToNextPanel()
        {
            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (badgePopup != null) badgePopup.SetActive(false);
            if (unit10UnlockPopup != null) unit10UnlockPopup.SetActive(false);
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

        #endregion
    }
}
