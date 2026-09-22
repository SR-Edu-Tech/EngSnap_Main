using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit11 {


    /// <summary>
    /// RP01 On Stage — The Group Project
    /// Guided multi-step dialogue choice for Unit 11: The Art of Politeness.
    /// A theatre stage set as a classroom table where four students are finishing a group project.
    /// LEO must respond politely at each step without upsetting teammates.
    /// Response chips offer the verbatim polite line plus its blunt twin.
    /// Success condition: Student chooses the polished line in at least 5 of 6 steps.
    /// </summary>
    public class Masters_ArtOfPoliteness_Roleplay_LessonOne : Masters_Lesson {

[System.Serializable]
    public class ArtOfPoliteness_RoleplayRP01Step {
        public int stepId;
        public string stepTitle;              // e.g. "Step 1: Deadline Disagreement"
        public string npcSpeakerName;         // "Maya", "Rohan", etc.
        public string npcOpeningLine;         // e.g. "I'm sure the deadline is on Friday."
        public string studentPolishedLine;    // e.g. "I think you might be mistaken."
        public string studentBluntLine;       // e.g. "You're wrong."
        public int correctOptionIndex;        // 0 or 1
        public string[] optionChoices;        // 2 option chips
        public string feedbackExplanation;    // ARIA's polite tip
        public AudioClip npcAudio;
        public AudioClip studentAudio;
    }
    
        [Header("RP01 6 Group Project Steps")]
        [SerializeField] private ArtOfPoliteness_RoleplayRP01Step[] steps;

        [Header("UI Display References")]
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;

        [Header("Characters & Speech Clouds")]
        [SerializeField] private GameObject npcAndStudentGameObject;
        [SerializeField] private GameObject npcCloud;
        [SerializeField] private TextMeshProUGUI npcDialogueTMP;
        [SerializeField] private GameObject studentCloud;
        [SerializeField] private TextMeshProUGUI studentDialogueTMP;

        [Header("Dialogue Option Buttons")]
        [SerializeField] private GameObject optionsContainer;
        [SerializeField] private Button[] optionButtons; // 2 option chips
        [SerializeField] private Image[] optionImages;
        [SerializeField] private TextMeshProUGUI[] optionTexts;

        [Header("Audio Controls")]
        [SerializeField] private Button replayAudioBtn;

        [Header("Audio References")]
        [SerializeField] private AudioClip introAudio;
        [SerializeField] private AudioClip celebrationAudio;
        [SerializeField] private AudioClip wrongFeedbackAudio;

        [Header("Feedback Banner")]
        [SerializeField] private GameObject feedbackBanner;
        [SerializeField] private TextMeshProUGUI feedbackTextTMP;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleTMP;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;
        [SerializeField] private Button returnHubBtn;

        private int currentStepIndex = 0;
        private int politeStepsCount = 0;
        private int attemptsOnCurrentStep = 0;
        private bool isProcessingTurn = false;

        private readonly Color defaultOptionColor = new Color(0.18f, 0.42f, 0.72f, 1f); // Clean blue
        private readonly Color correctOptionColor = new Color(0.18f, 0.80f, 0.44f, 1f); // Green
        private readonly Color wrongOptionColor = new Color(0.91f, 0.30f, 0.24f, 1f);   // Red

        protected override void Awake() {
            topic = Masters_Topic.Roleplay;
            if (narratorSpeech != null) {
                if (introAudio == null) introAudio = narratorSpeech;
                narratorSpeech = null;
            }
            base.Awake();

            AutoBindReferences();

            if (steps == null || steps.Length == 0) {
                PopulateDefaultSteps();
            }

            WireEventListeners();

            if (resultPanel != null) resultPanel.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);
        }

        protected override void Start() {
            topic = Masters_Topic.Roleplay;
            base.Start();
            AutoBindReferences();
            EnsureHeaderAndTitle();
            WireEventListeners();
            EnsureNextAndBackButtonWired();

            if (nextButton != null) {
                nextButton.interactable = false;
                nextButton.gameObject.SetActive(false);
            }

            RestartRoleplay();
        }

        public override void EnsureNextAndBackButtonWired() {
            base.EnsureNextAndBackButtonWired();
            if (nextButton != null) {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
            }
            if (backButton != null) {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        private void AutoBindReferences() {
            EnsureHeaderAndTitle();

            Transform hud = transform.Find("CommonHUD") ?? transform;
            if (backButton == null) {
                Transform bTr = hud.Find("BackButton") ?? transform.Find("BackButton");
                if (bTr != null) backButton = bTr.GetComponent<Button>();
            }
            if (nextButton == null) {
                Transform nTr = hud.Find("NextButton") ?? transform.Find("NextButton");
                if (nTr != null) nextButton = nTr.GetComponent<Button>();
            }

            Transform headerTr = transform.Find("HeaderContainer");
            if (headerTr != null) {
                if (progressTMP == null) {
                    Transform p = headerTr.Find("ProgressTMP") ?? headerTr.Find("Progress");
                    if (p != null) progressTMP = p.GetComponent<TextMeshProUGUI>();
                }
                if (scoreTMP == null) {
                    Transform sc = headerTr.Find("ScoreTMP") ?? headerTr.Find("Score");
                    if (sc != null) scoreTMP = sc.GetComponent<TextMeshProUGUI>();
                }
            }

            if (npcAndStudentGameObject == null) {
                Transform ns = transform.Find("Characters") ?? transform.Find("Actors") ?? transform.Find("DialogueStage");
                if (ns != null) npcAndStudentGameObject = ns.gameObject;
            }

            if (npcDialogueTMP == null) {
                Transform nTr = transform.Find("NpcCloud/Text") ?? transform.Find("Characters/NpcCloud/Text") ?? transform.Find("DialogueStage/NpcCloud/Text");
                if (nTr != null) npcDialogueTMP = nTr.GetComponent<TextMeshProUGUI>();
            }
            if (npcCloud == null && npcDialogueTMP != null) {
                npcCloud = npcDialogueTMP.transform.parent.gameObject;
            }

            if (studentDialogueTMP == null) {
                Transform sTr = transform.Find("StudentCloud/Text") ?? transform.Find("Characters/StudentCloud/Text") ?? transform.Find("DialogueStage/StudentCloud/Text");
                if (sTr != null) studentDialogueTMP = sTr.GetComponent<TextMeshProUGUI>();
            }
            if (studentCloud == null && studentDialogueTMP != null) {
                studentCloud = studentDialogueTMP.transform.parent.gameObject;
            }

            if (optionsContainer == null) {
                Transform oc = transform.Find("OptionsContainer") ?? transform.Find("ChipsContainer") ?? transform.Find("RepliesContainer");
                if (oc != null) optionsContainer = oc.gameObject;
            }

            if (optionsContainer != null && (optionButtons == null || optionButtons.Length == 0)) {
                List<Button> bList = new List<Button>();
                List<Image> iList = new List<Image>();
                List<TextMeshProUGUI> tList = new List<TextMeshProUGUI>();

                Button[] allBtns = optionsContainer.GetComponentsInChildren<Button>(true);
                foreach (var b in allBtns) {
                    if (b != null && b != backButton && b != nextButton && b != retryBtn && b != returnHubBtn && b != replayAudioBtn) {
                        bList.Add(b);
                        iList.Add(b.GetComponent<Image>());
                        tList.Add(b.GetComponentInChildren<TextMeshProUGUI>(true));
                    }
                }

                optionButtons = bList.ToArray();
                optionImages = iList.ToArray();
                optionTexts = tList.ToArray();
            }

            if (replayAudioBtn == null) {
                Transform rTr = transform.Find("AudioReplayButton") ?? transform.Find("ReplayButton") ?? transform.Find("HeaderContainer/ReplayBtn");
                if (rTr != null) replayAudioBtn = rTr.GetComponent<Button>();
            }

            if (feedbackBanner == null) {
                Transform fb = transform.Find("FeedbackBanner") ?? transform.Find("InstructionBanner");
                if (fb != null) {
                    feedbackBanner = fb.gameObject;
                    feedbackTextTMP = fb.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }

            if (resultPanel == null) {
                Transform rp = transform.Find("ResultPanel") ?? transform.Find("CompletedPanel") ?? transform.Find("ResultsPanel");
                if (rp != null) {
                    resultPanel = rp.gameObject;
                    Transform rTitle = rp.Find("ResultTitle") ?? rp.Find("Title");
                    if (rTitle != null) resultTitleTMP = rTitle.GetComponent<TextMeshProUGUI>();

                    Transform rSc = rp.Find("ResultScore") ?? rp.Find("ScoreTMP");
                    if (rSc != null) resultScoreTMP = rSc.GetComponent<TextMeshProUGUI>();

                    Transform rSt = rp.Find("ResultStatus") ?? rp.Find("StatusTMP");
                    if (rSt != null) resultStatusTMP = rSt.GetComponent<TextMeshProUGUI>();

                    Transform rBtn = rp.Find("RetryButton") ?? rp.Find("RetryBtn");
                    if (rBtn != null) retryBtn = rBtn.GetComponent<Button>();

                    Transform hBtn = rp.Find("ReturnHubButton") ?? rp.Find("NextButton");
                    if (hBtn != null) returnHubBtn = hBtn.GetComponent<Button>();
                }
            }
        }

        private void EnsureHeaderAndTitle() {
            if (headerTMP == null) {
                Transform hTr = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("Branch");
                if (hTr != null) headerTMP = hTr.GetComponent<TextMeshProUGUI>();
            }
            if (headerTMP != null) {
                headerTMP.text = "THE ART OF POLITENESS";
            }

            if (titleTMP == null) {
                Transform tTr = transform.Find("HeaderContainer/LessonTitle") ?? transform.Find("LessonTitle") ?? transform.Find("HeaderContainer/Title") ?? transform.Find("Title");
                if (tTr != null) titleTMP = tTr.GetComponent<TextMeshProUGUI>();
            }
            if (titleTMP != null) {
                titleTMP.text = "RP01 On Stage — The Group Project";
                titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
                titleTMP.fontStyle = FontStyles.Bold;
                titleTMP.alignment = TextAlignmentOptions.Center;
            }

            if (subtitleTMP == null) {
                Transform sTr = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Subtitle") ?? transform.Find("Instruction");
                if (sTr != null) subtitleTMP = sTr.GetComponent<TextMeshProUGUI>();
            }
            if (subtitleTMP != null) {
                subtitleTMP.text = "Choose the polished line at each step so the group keeps working happily together!";
            }
        }

        public void PopulateDefaultSteps() {
            steps = new ArtOfPoliteness_RoleplayRP01Step[] {
                new ArtOfPoliteness_RoleplayRP01Step {
                    stepId = 1,
                    stepTitle = "Step 1: Deadline Disagreement",
                    npcSpeakerName = "Maya",
                    npcOpeningLine = "I'm sure the deadline is on Friday.",
                    studentPolishedLine = "I think you might be mistaken.",
                    studentBluntLine = "You're wrong.",
                    correctOptionIndex = 0,
                    optionChoices = new string[] { "I think you might be mistaken.", "You're wrong." },
                    feedbackExplanation = "Saying 'You're wrong' is harsh. 'I think you might be mistaken' is a gentle, polite way to disagree."
                },
                new ArtOfPoliteness_RoleplayRP01Step {
                    stepId = 2,
                    stepTitle = "Step 2: Poster Colour Idea",
                    npcSpeakerName = "Maya",
                    npcOpeningLine = "Let's do the whole poster in bright red.",
                    studentPolishedLine = "I'm not so sure that's a good idea.",
                    studentBluntLine = "That's a bad idea.",
                    correctOptionIndex = 1,
                    optionChoices = new string[] { "That's a bad idea.", "I'm not so sure that's a good idea." },
                    feedbackExplanation = "Calling it 'a bad idea' directly hurts feelings. 'I'm not so sure that's a good idea' softens your opinion."
                },
                new ArtOfPoliteness_RoleplayRP01Step {
                    stepId = 3,
                    stepTitle = "Step 3: Section Review",
                    npcSpeakerName = "Maya",
                    npcOpeningLine = "Here's my part of the writing — done!",
                    studentPolishedLine = "I'm not quite satisfied with this work.",
                    studentBluntLine = "Your work isn't good.",
                    correctOptionIndex = 0,
                    optionChoices = new string[] { "I'm not quite satisfied with this work.", "Your work isn't good." },
                    feedbackExplanation = "Saying 'Your work isn't good' sounds like an attack. Saying 'I'm not quite satisfied' focuses on personal standards."
                },
                new ArtOfPoliteness_RoleplayRP01Step {
                    stepId = 4,
                    stepTitle = "Step 4: Border Design Preference",
                    npcSpeakerName = "Maya",
                    npcOpeningLine = "I've picked these colours for the border.",
                    studentPolishedLine = "I'd prefer to use different colours in this design.",
                    studentBluntLine = "I don't like the colors in this design.",
                    correctOptionIndex = 1,
                    optionChoices = new string[] { "I don't like the colors in this design.", "I'd prefer to use different colours in this design." },
                    feedbackExplanation = "Saying 'I don't like...' is blunt. Expressing a polite preference with 'I'd prefer...' is much smoother."
                },
                new ArtOfPoliteness_RoleplayRP01Step {
                    stepId = 5,
                    stepTitle = "Step 5: Requesting the Summary",
                    npcSpeakerName = "Maya",
                    npcOpeningLine = "I'll finish the summary tonight.",
                    studentPolishedLine = "Could you send me the report?",
                    studentBluntLine = "Send me the report.",
                    correctOptionIndex = 0,
                    optionChoices = new string[] { "Could you send me the report?", "Send me the report." },
                    feedbackExplanation = "A direct command sounds like an order. Asking 'Could you send me...?' is polite and cooperative."
                },
                new ArtOfPoliteness_RoleplayRP01Step {
                    stepId = 6,
                    stepTitle = "Step 6: Scheduling Practice",
                    npcSpeakerName = "Maya",
                    npcOpeningLine = "When shall we meet to practise?",
                    studentPolishedLine = "Let me know when you're available.",
                    studentBluntLine = "Tell me when you're available.",
                    correctOptionIndex = 1,
                    optionChoices = new string[] { "Tell me when you're available.", "Let me know when you're available." },
                    feedbackExplanation = "Telling someone 'Tell me' sounds demanding. 'Let me know' is warm and inviting."
                }
            };
        }

        private void WireEventListeners() {
            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    int optIdx = i;
                    if (optionButtons[i] != null) {
                        optionButtons[i].onClick.RemoveAllListeners();
                        optionButtons[i].onClick.AddListener(() => OnOptionChosen(optIdx));
                    }
                }
            }

            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentNpcAudio);
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RestartRoleplay);
            }

            if (returnHubBtn != null) {
                returnHubBtn.onClick.RemoveAllListeners();
                returnHubBtn.onClick.AddListener(OnNextButtonClicked);
            }
        }

        public void RestartRoleplay() {
            currentStepIndex = 0;
            politeStepsCount = 0;
            attemptsOnCurrentStep = 0;
            isProcessingTurn = false;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            UpdateStatsUI();

            StopAllCoroutines();
            StartCoroutine(PlayIntroThenStartCoroutine());
        }

        private IEnumerator PlayIntroThenStartCoroutine() {
            if (npcCloud != null) npcCloud.SetActive(false);
            if (studentCloud != null) studentCloud.SetActive(false);
            if (optionsContainer != null) optionsContainer.SetActive(false);

            AudioClip introClip = (introAudio != null) ? introAudio : narratorSpeech;
            if (introClip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(introClip);
                yield return new WaitForSeconds(introClip.length + 0.3f);
            } else {
                yield return new WaitForSeconds(0.4f);
            }

            DisplayStep(0);
        }

        private void UpdateStatsUI() {
            int totalSteps = (steps != null) ? steps.Length : 6;
            if (progressTMP != null) {
                progressTMP.text = $"Step: {Mathf.Min(currentStepIndex + 1, totalSteps)}/{totalSteps}";
            }
            if (scoreTMP != null) {
                scoreTMP.text = $"Polite: {politeStepsCount}/{totalSteps}";
            }
        }

        private void DisplayStep(int stepIndex) {
            if (steps == null || stepIndex < 0 || stepIndex >= steps.Length) {
                EndRoleplay();
                return;
            }

            ArtOfPoliteness_RoleplayRP01Step step = steps[stepIndex];
            attemptsOnCurrentStep = 0;
            isProcessingTurn = false;

            UpdateStatsUI();

            // Display NPC Line in NPC speech cloud
            if (npcCloud != null) npcCloud.SetActive(true);
            if (npcDialogueTMP != null) {
                npcDialogueTMP.text = $"<b>{step.npcSpeakerName}:</b> \"{step.npcOpeningLine}\"";
            }

            // Hide student cloud until chosen
            if (studentCloud != null) studentCloud.SetActive(false);
            if (studentDialogueTMP != null) studentDialogueTMP.text = "";

            // Play NPC Audio
            if (step.npcAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(step.npcAudio);
            }

            // Populate option chips
            if (optionsContainer != null) optionsContainer.SetActive(true);
            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (step.optionChoices != null && i < step.optionChoices.Length) {
                        optionButtons[i].gameObject.SetActive(true);
                        optionButtons[i].interactable = true;

                        if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) {
                            optionTexts[i].text = step.optionChoices[i];
                            optionTexts[i].color = Color.white;
                        }
                        if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                            optionImages[i].color = defaultOptionColor;
                        }
                    } else if (optionButtons[i] != null) {
                        optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        public void OnOptionChosen(int chosenIndex) {
            if (isProcessingTurn || steps == null || currentStepIndex >= steps.Length) return;

            ArtOfPoliteness_RoleplayRP01Step step = steps[currentStepIndex];
            attemptsOnCurrentStep++;

            bool isCorrect = (chosenIndex == step.correctOptionIndex);
            StartCoroutine(ProcessChoiceRoutine(chosenIndex, isCorrect, step));
        }

        private IEnumerator ProcessChoiceRoutine(int chosenIndex, bool isCorrect, ArtOfPoliteness_RoleplayRP01Step step) {
            isProcessingTurn = true;
            SetOptionsInteractable(false);

            if (isCorrect) {
                if (attemptsOnCurrentStep == 1) {
                    politeStepsCount++;
                }

                // Highlight chosen button green
                if (optionImages != null && chosenIndex < optionImages.Length && optionImages[chosenIndex] != null) {
                    optionImages[chosenIndex].color = correctOptionColor;
                    optionImages[chosenIndex].transform.DOPunchScale(Vector3.one * 0.1f, 0.25f);
                }

                // Show student speech cloud
                if (studentCloud != null) studentCloud.SetActive(true);
                if (studentDialogueTMP != null) {
                    studentDialogueTMP.text = $"<b>LEO:</b> \"{step.studentPolishedLine}\"";
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                // Play student audio
                if (step.studentAudio != null && Masters_AudioManager.Instance != null) {
                    yield return new WaitForSeconds(0.4f);
                    Masters_AudioManager.Instance.PlayVoiceOver(step.studentAudio);
                    yield return new WaitForSeconds(step.studentAudio.length + 0.3f);
                } else {
                    yield return new WaitForSeconds(1.5f);
                }

                currentStepIndex++;
                UpdateStatsUI();

                if (currentStepIndex >= steps.Length) {
                    EndRoleplay();
                } else {
                    DisplayStep(currentStepIndex);
                }
            } else {
                // Highlight chosen button red
                if (optionImages != null && chosenIndex < optionImages.Length && optionImages[chosenIndex] != null) {
                    optionImages[chosenIndex].color = wrongOptionColor;
                    optionImages[chosenIndex].transform.DOShakePosition(0.35f, 10f);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                // Show ARIA feedback banner
                if (feedbackBanner != null && feedbackTextTMP != null) {
                    feedbackBanner.SetActive(true);
                    feedbackTextTMP.text = $"<color=#FFD27F><b>ARIA Tip:</b></color> {step.feedbackExplanation}";
                }

                yield return new WaitForSeconds(2.5f);

                if (feedbackBanner != null) feedbackBanner.SetActive(false);

                // Reset button colors for retry
                if (optionImages != null) {
                    for (int i = 0; i < optionImages.Length; i++) {
                        if (optionImages[i] != null) optionImages[i].color = defaultOptionColor;
                    }
                }

                SetOptionsInteractable(true);
                isProcessingTurn = false;
            }
        }

        private void SetOptionsInteractable(bool interactable) {
            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (optionButtons[i] != null) optionButtons[i].interactable = interactable;
                }
            }
        }

        public void ReplayCurrentNpcAudio() {
            if (steps != null && currentStepIndex < steps.Length && steps[currentStepIndex].npcAudio != null) {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(steps[currentStepIndex].npcAudio);
                }
            }
        }

        private void EndRoleplay() {
            if (optionsContainer != null) optionsContainer.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);

            if (resultPanel != null) {
                resultPanel.SetActive(true);
            }

            bool passed = politeStepsCount >= 5;

            if (resultTitleTMP != null) {
                resultTitleTMP.text = passed ? "Project Succeeded!" : "Keep Practicing!";
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Polite Choices: {politeStepsCount} / {steps.Length}";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed
                    ? "<color=#55FF88>Awesome! By choosing polished lines, you kept your team happy and finished the project smoothly!</color>"
                    : "<color=#FFD27F>Try to use more polished lines next time so your teammates feel comfortable and supported.</color>";
            }

            if (passed) {
                if (nextButton != null) {
                    nextButton.gameObject.SetActive(true);
                    nextButton.interactable = true;
                    NextButtonAnimation();
                }
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }
            } else {
                if (retryBtn != null) retryBtn.gameObject.SetActive(true);
            }
        }

        protected override void OnNextButtonClicked() {
            if (topic == Masters_Topic.None) return;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
            }

            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }

        private void OnBackButtonClicked() {
            OnReturnToHub();
        }

        private void OnReturnToHub() {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                Masters_AudioManager.Instance.StopVoiceOver();
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnBackButtonClicked();
            }
        }
    }
}
