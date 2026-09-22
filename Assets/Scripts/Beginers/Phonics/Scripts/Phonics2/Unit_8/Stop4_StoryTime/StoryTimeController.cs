using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.Common;

namespace EngSnap.Phonics2.Unit8
{
    public class StoryTimeController : MonoBehaviour
    {
        [Header("Unit Progress Settings")]
        [SerializeField] private string unitID = "Unit8";
        [SerializeField] private string topicName = "StoryTime";

        [Header("Story Selection")]
        [Tooltip("0 = Story A (Pat The Cat), 1 = Story B (Ben's Hen)")]
        [SerializeField] private int activeStoryIndex = 0;

        [Header("Data Asset")]
        [SerializeField] private StoryTimeData activityData;

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
        [SerializeField] private Button[] wordButtonsInLine = new Button[8];
        [SerializeField] private TMP_Text[] wordTextsInLine = new TMP_Text[8];
        [SerializeField] private Image[] wordHighlightsInLine = new Image[8];
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

        [Header("Rewards & Unit 9 Unlock")]
        [SerializeField] private GameObject confettiParticles;
        [SerializeField] private GameObject rewardPopup;
        [SerializeField] private GameObject stickerPopup;
        [SerializeField] private GameObject badgePopup;
        [SerializeField] private GameObject unit9UnlockPopup;
        [SerializeField] private GameObject continueButton;
        [SerializeField] private GameObject nextPanel;
        [SerializeField] private GameObject currentPanel;
        [SerializeField] private GameObject unitContentPanel;

        private int currentLineIndex = 0;
        private int currentQuestionIndex = 0;
        private int currentStarChallengeIndex = 0;
        private bool isSecondPassReadAlong = false;
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
                activityData = Resources.Load<StoryTimeData>("Phonics2/Unit8/StoryTimeData_Unit8");
            }
            if (activityData == null)
            {
                activityData = ScriptableObject.CreateInstance<StoryTimeData>();
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

            // Word button listeners in story line
            for (int i = 0; i < wordButtonsInLine.Length; i++)
            {
                int index = i;
                if (wordButtonsInLine[i] != null)
                {
                    wordButtonsInLine[i].onClick.RemoveAllListeners();
                    wordButtonsInLine[i].onClick.AddListener(() => OnWordInLineClicked(index));
                }
            }

            // Comprehension question choice buttons
            for (int i = 0; i < questionChoiceButtons.Length; i++)
            {
                int index = i;
                if (questionChoiceButtons[i] != null)
                {
                    questionChoiceButtons[i].onClick.RemoveAllListeners();
                    questionChoiceButtons[i].onClick.AddListener(() => OnQuestionChoiceSelected(index));
                }
            }

            // Star Round choice buttons
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

        public int ActiveStoryIndex => activeStoryIndex;

        public void SetActiveStoryIndex(int index)
        {
            activeStoryIndex = Mathf.Clamp(index, 0, 1);
            PlayerPrefs.SetInt("Unit8_ActiveStoryIndex", activeStoryIndex);
            PlayerPrefs.Save();
        }

        public void StartActivity()
        {
            StopAllCoroutines();
            currentLineIndex = 0;
            currentQuestionIndex = 0;
            currentStarChallengeIndex = 0;
            isSecondPassReadAlong = false;
            isTransitioning = false;

            if (PlayerPrefs.HasKey("Unit8_ActiveStoryIndex"))
            {
                activeStoryIndex = Mathf.Clamp(PlayerPrefs.GetInt("Unit8_ActiveStoryIndex", 0), 0, 1);
            }

            if (rewardPopup != null) rewardPopup.SetActive(false);
            if (stickerPopup != null) stickerPopup.SetActive(false);
            if (badgePopup != null) badgePopup.SetActive(false);
            if (unit9UnlockPopup != null) unit9UnlockPopup.SetActive(false);
            if (confettiParticles != null) confettiParticles.SetActive(false);
            if (continueButton != null) continueButton.SetActive(false);
            if (comprehensionPanel != null) comprehensionPanel.SetActive(false);
            if (starRoundPanel != null) starRoundPanel.SetActive(false);
            if (startStarRoundButton != null) startStarRoundButton.gameObject.SetActive(false);

            if (storyPanel != null) storyPanel.SetActive(true);
            if (leoMascotObject != null) leoMascotObject.SetActive(true);
            if (taraMascotObject != null) taraMascotObject.SetActive(false);

            LoadStory(activeStoryIndex);
        }

        public void LoadStory(int storyIdx)
        {
            activeStoryIndex = storyIdx;
            currentLineIndex = 0;
            isSecondPassReadAlong = false;

            if (activityData != null)
            {
                currentStory = (activeStoryIndex == 0) ? activityData.storyA : activityData.storyB;
            }

            if (comprehensionPanel != null) comprehensionPanel.SetActive(false);
            if (starRoundPanel != null) starRoundPanel.SetActive(false);
            if (storyPanel != null) storyPanel.SetActive(true);

            StartCoroutine(RunStoryIntroRoutine());
        }

        private IEnumerator RunStoryIntroRoutine()
        {
            isTransitioning = true;
            if (storyTitleTMP != null && currentStory != null)
            {
                storyTitleTMP.text = currentStory.storyTitle;
            }

            if (storyIllustrationImage != null && currentStory != null && currentStory.storyIllustrationSprite != null)
            {
                storyIllustrationImage.sprite = currentStory.storyIllustrationSprite;
                storyIllustrationImage.gameObject.SetActive(true);
            }

            string introText = (activeStoryIndex == 0)
                ? "You can read words. You can read sentences. Now — a whole STORY."
                : "Now for our next story: Ben's Hen! Let's read!";
            SetDialogue(introText);

            if (activeStoryIndex == 0 && activityData != null && activityData.leoIntroClip != null)
            {
                yield return PlayVoiceClip(activityData.leoIntroClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            SetDialogue("Read it your way. Tap any word you want to hear.");
            if (activityData != null && activityData.leoPermissionClip != null)
            {
                PlayVoiceClipNonBlocking(activityData.leoPermissionClip);
            }

            yield return new WaitForSeconds(0.5f);
            isTransitioning = false;
            LoadStoryLine(0);
        }

        #region Phase 1: Story Reading Lines

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

            // Populate word buttons
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

            SetDialogue($"Line {index + 1}: Read the line or tap any word to hear it. Tap the tick when done!");
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
            StartCoroutine(RunStorySecondPassInvitation());
        }

        private IEnumerator RunStorySecondPassInvitation()
        {
            isTransitioning = true;
            SetDialogue("You read the whole story! Shall we read it again, faster this time?");
            if (activityData != null && activityData.reReadInvitationClip != null)
            {
                yield return PlayVoiceClip(activityData.reReadInvitationClip);
            }
            else
            {
                yield return new WaitForSeconds(1.5f);
            }

            // Run natural-pace read-along highlight through all lines
            if (currentStory != null && currentStory.lines != null)
            {
                for (int l = 0; l < currentStory.lines.Length; l++)
                {
                    LoadStoryLine(l);
                    var line = currentStory.lines[l];
                    if (line.lineAudioClip != null) PlayVoiceClipNonBlocking(line.lineAudioClip);

                    // Animate word highlight
                    if (line.wordTokens != null)
                    {
                        for (int w = 0; w < line.wordTokens.Length; w++)
                        {
                            if (wordHighlightsInLine != null && w < wordHighlightsInLine.Length && wordHighlightsInLine[w] != null)
                            {
                                wordHighlightsInLine[w].gameObject.SetActive(true);
                            }
                            yield return new WaitForSeconds(0.45f);
                            if (wordHighlightsInLine != null && w < wordHighlightsInLine.Length && wordHighlightsInLine[w] != null)
                            {
                                wordHighlightsInLine[w].gameObject.SetActive(false);
                            }
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

        #region Phase 2: Comprehension Questions

        private void StartComprehensionPhase()
        {
            if (storyPanel != null) storyPanel.SetActive(false);
            if (comprehensionPanel != null) comprehensionPanel.SetActive(true);

            currentQuestionIndex = (activityData != null && activityData.comprehensionQuestions != null && activityData.comprehensionQuestions.Length > activeStoryIndex)
                ? activeStoryIndex
                : 0;

            LoadQuestion(currentQuestionIndex);
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

            if (currentQuestion.questionPromptClip != null)
            {
                PlayVoiceClipNonBlocking(currentQuestion.questionPromptClip);
            }

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
                SetDialogue("Spot on! You understood the story!");
                yield return new WaitForSeconds(1.2f);

                isTransitioning = false;

                // Automatically advance story index from Story A (0) to Story B (1), then to Star Round
                if (activeStoryIndex == 0)
                {
                    activeStoryIndex = 1;
                    PlayerPrefs.SetInt("Unit8_ActiveStoryIndex", 1);
                    PlayerPrefs.Save();
                    LoadStory(1);
                }
                else
                {
                    // Both stories complete, reset for next playthrough and start Star Round
                    PlayerPrefs.SetInt("Unit8_ActiveStoryIndex", 0);
                    PlayerPrefs.Save();
                    CompleteStoryReadingPhase();
                }
            }
            else
            {
                PlaySFX(activityData != null ? activityData.retryGentleSfx : null);
                SetDialogue("Think back to the story — let's try again!");
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
                SetDialogue("Story completed! Tap Start to take on Tara's Star Round!");
            }
            else
            {
                StartStarRoundPhase();
            }
        }

        #endregion

        #region Phase 3: Tara Star Round (6 Challenges)

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

                SetDialogue("Roar! Brilliant!");
                yield return new WaitForSeconds(0.8f);

                currentStarChallengeIndex++;
                isTransitioning = false;
                LoadStarChallenge(currentStarChallengeIndex);
            }
            else
            {
                PlaySFX(activityData != null ? activityData.retryGentleSfx : null);
                SetDialogue("Listen carefully to Tara! Give it another go!");
                yield return new WaitForSeconds(1.0f);
                isTransitioning = false;
            }
        }

        #endregion

        #region Final Rewards & Unit 9 Unlock

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

            SetDialogue("You are a WORD READER! Go and read that story to someone at home.");
            if (activityData != null && activityData.wordReaderBadgeVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.wordReaderBadgeVoiceClip);
            }

            if (confettiParticles != null) confettiParticles.SetActive(true);
            if (badgePopup != null) badgePopup.SetActive(true);
            if (rewardPopup != null) rewardPopup.SetActive(true);
            if (stickerPopup != null) stickerPopup.SetActive(true);

            yield return new WaitForSeconds(1.5f);

            SetDialogue("Unit Nine is open! Next time — i, o and u, and three more stories.");
            if (activityData != null && activityData.unit9UnlockVoiceClip != null)
            {
                yield return PlayVoiceClip(activityData.unit9UnlockVoiceClip);
            }

            if (unit9UnlockPopup != null) unit9UnlockPopup.SetActive(true);
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
            if (unit9UnlockPopup != null) unit9UnlockPopup.SetActive(false);
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
