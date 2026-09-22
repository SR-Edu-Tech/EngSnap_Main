using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit10 {

    

    /// <summary>
    /// RP01 On Stage — Helping Your Cousin Practise
    /// Guided multi-step correction dialogue for Book 2B Unit 10 (Common Mistakes with Prepositions).
    /// A theatre stage set as a living room.
    /// LEO's younger cousin is practising English before her school test and keeps saying sentences with one loose part.
    /// LEO helps her kindly by giving the right sentence rather than just calling the mistake out.
    /// Response chips offer the mended sentence plus a blunt correction as the distractor.
    /// Success condition: Student chooses the helpful reply in at least 4 of 5 steps.
    /// </summary>
    public class Masters_CommonMistakesWithPrepositions_Roleplay_LessonOne : Masters_Lesson {

[System.Serializable]
    public class CommonMistakes_RoleplayRP01Step {
        public int stepId;
        public string stepTitle;              // e.g. "Step 1: Preposition Fix"
        public string npcSpeakerName;         // "Cousin"
        public string npcOpeningLine;         // e.g. "Have you been in London?"
        public string studentFittingAnswer;   // e.g. "Have you been to London? — yes, last year!"
        public string studentDistractorLine;  // e.g. "That's wrong."
        public int correctOptionIndex;        // 0 or 1
        public string[] optionChoices;        // 2 option chips
        public AudioClip npcAudio;
        public AudioClip studentAudio;
    }
    
        [Header("RP01 5 Helping Cousin Practise Steps")]
        [SerializeField] private CommonMistakes_RoleplayRP01Step[] steps;

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
        [SerializeField] private Button[] optionButtons; // 2 or 3 Buttons
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
        private int helpfulStepsCount = 0;
        private int attemptsOnCurrentStep = 0;
        private bool isProcessingTurn = false;

        private readonly Color defaultOptionColor = new Color(0.12f, 0.42f, 0.78f, 0.95f);
        private readonly Color correctOptionColor = new Color(0.15f, 0.75f, 0.35f, 1f);
        private readonly Color wrongOptionColor = new Color(0.85f, 0.25f, 0.25f, 1f);

        protected override void Awake() {
            topic = Masters_Topic.Roleplay;
            if (narratorSpeech != null) {
                if (introAudio == null) introAudio = narratorSpeech;
                narratorSpeech = null;
            }
            base.Awake();

            AutoBindReferences();
            InitStepsIfEmpty();
            WireEventListeners();
        }

        protected override void Start() {
            topic = Masters_Topic.Roleplay;
            base.Start();
            AutoBindReferences();
            InitStepsIfEmpty();
            WireEventListeners();
            EnsureNextAndBackButtonWired();
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
                if (headerTMP == null) {
                    Transform h = headerTr.Find("Header") ?? headerTr.Find("Branch");
                    if (h != null) headerTMP = h.GetComponent<TextMeshProUGUI>();
                }
                if (titleTMP == null) {
                    Transform t = headerTr.Find("LessonTitle") ?? headerTr.Find("Title");
                    if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
                }
                if (subtitleTMP == null) {
                    Transform s = headerTr.Find("Subtitle") ?? headerTr.Find("InstructionTMP");
                    if (s != null) subtitleTMP = s.GetComponent<TextMeshProUGUI>();
                }
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
                    bList.Add(b);
                    iList.Add(b.GetComponent<Image>());
                    tList.Add(b.GetComponentInChildren<TextMeshProUGUI>(true));
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

        private void WireEventListeners() {
            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    int optIdx = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionChosen(optIdx));
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
                returnHubBtn.onClick.AddListener(OnReturnToHub);
            }
        }

        public void RestartRoleplay() {
            currentStepIndex = 0;
            helpfulStepsCount = 0;
            attemptsOnCurrentStep = 0;
            isProcessingTurn = false;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            UpdateHeaderTexts();
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

        private void UpdateHeaderTexts() {
            if (headerTMP != null) headerTMP.text = "COMMON MISTAKES WITH PREPOSITIONS";
            if (titleTMP != null) {
                titleTMP.text = "RP01 On Stage — Helping Your Cousin Practise";
                titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
            }
            if (subtitleTMP != null) subtitleTMP.text = "Choose the helpful reply that gives the right sentence rather than just calling out the mistake.";
        }

        private void UpdateStatsUI() {
            int totalSteps = (steps != null) ? steps.Length : 5;
            if (progressTMP != null) {
                progressTMP.text = $"Step: {Mathf.Min(currentStepIndex + 1, totalSteps)} / {totalSteps}";
            }
            if (scoreTMP != null) {
                scoreTMP.text = $"Helpful: {helpfulStepsCount} / {totalSteps}";
            }
        }

        private void DisplayStep(int stepIndex) {
            if (steps == null || stepIndex < 0 || stepIndex >= steps.Length) {
                EndRoleplay();
                return;
            }

            CommonMistakes_RoleplayRP01Step step = steps[stepIndex];
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
                        }
                        if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                            optionImages[i].color = defaultOptionColor;
                        }
                    } else {
                        optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        private void OnOptionChosen(int optionIndex) {
            if (isProcessingTurn) return;
            if (steps == null || currentStepIndex >= steps.Length) return;

            CommonMistakes_RoleplayRP01Step step = steps[currentStepIndex];
            isProcessingTurn = true;
            attemptsOnCurrentStep++;

            // Disable buttons immediately so player cannot trigger next turn while audio is playing
            if (optionButtons != null) {
                foreach (var btn in optionButtons) {
                    if (btn != null) btn.interactable = false;
                }
            }

            bool isHelpful = (optionIndex == step.correctOptionIndex);

            if (isHelpful) {
                StartCoroutine(HandleHelpfulAnswerCoroutine(step, optionIndex));
            } else {
                StartCoroutine(HandleBluntAnswerCoroutine(step, optionIndex));
            }
        }

        private IEnumerator HandleHelpfulAnswerCoroutine(CommonMistakes_RoleplayRP01Step step, int optionIndex) {
            // Visual highlight green
            if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = correctOptionColor;
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // Show student cloud with helpful mended sentence
            if (studentCloud != null) studentCloud.SetActive(true);
            if (studentDialogueTMP != null) {
                studentDialogueTMP.text = $"<b>LEO:</b> \"{step.studentFittingAnswer}\"";
            }

            // Play LEO voice clip
            if (step.studentAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(step.studentAudio);
            }

            // Show positive feedback banner
            if (feedbackBanner != null) {
                feedbackBanner.SetActive(true);
                if (feedbackTextTMP != null) {
                    feedbackTextTMP.text = "<color=#4CAF50><b>Helpful & Kind!</b></color> Giving the right sentence encourages your cousin!";
                }
            }

            if (attemptsOnCurrentStep == 1) {
                helpfulStepsCount++;
            }
            UpdateStatsUI();

            // Wait for student audio to finish completely before loading the next dialogue turn!
            float studentAudioDuration = (step.studentAudio != null && step.studentAudio.length > 0.1f) 
                ? step.studentAudio.length + 0.8f 
                : 3.5f;
            yield return new WaitForSeconds(studentAudioDuration);

            if (feedbackBanner != null) feedbackBanner.SetActive(false);

            currentStepIndex++;
            if (currentStepIndex < steps.Length) {
                DisplayStep(currentStepIndex);
            } else {
                EndRoleplay();
            }
        }

        private IEnumerator HandleBluntAnswerCoroutine(CommonMistakes_RoleplayRP01Step step, int optionIndex) {
            // Visual highlight red
            if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = wrongOptionColor;
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            // Show feedback banner
            if (feedbackBanner != null) {
                feedbackBanner.SetActive(true);
                if (feedbackTextTMP != null) {
                    feedbackTextTMP.text = "<color=#F44336><b>Blunt Distractor!</b></color> Just saying 'That's wrong' discourages her. Show her the mended sentence!";
                }
            }

            if (wrongFeedbackAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(wrongFeedbackAudio);
            }

            float wrongAudioDuration = (wrongFeedbackAudio != null && wrongFeedbackAudio.length > 0.1f) 
                ? wrongFeedbackAudio.length + 0.6f 
                : 3.2f;
            yield return new WaitForSeconds(wrongAudioDuration);

            if (feedbackBanner != null) feedbackBanner.SetActive(false);

            // Reset buttons for retry
            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (optionButtons[i] != null) optionButtons[i].interactable = true;
                    if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                        optionImages[i].color = defaultOptionColor;
                    }
                }
            }
            isProcessingTurn = false;
        }

        private void ReplayCurrentNpcAudio() {
            if (steps != null && currentStepIndex >= 0 && currentStepIndex < steps.Length) {
                AudioClip clip = steps[currentStepIndex].npcAudio;
                if (clip != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(clip);
                }
            }
        }

        private void EndRoleplay() {
            int totalSteps = (steps != null) ? steps.Length : 5;
            bool passed = (helpfulStepsCount >= 4);

            if (optionsContainer != null) optionsContainer.SetActive(false);
            if (npcCloud != null) npcCloud.SetActive(false);
            if (studentCloud != null) studentCloud.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);

            if (resultPanel != null) {
                resultPanel.SetActive(true);
                if (resultTitleTMP != null) {
                    resultTitleTMP.text = passed ? "STAGE CLEARED!" : "PRACTISE AGAIN!";
                }
                if (resultScoreTMP != null) {
                    resultScoreTMP.text = $"Helpful Replies: {helpfulStepsCount} / {totalSteps}";
                }
                if (resultStatusTMP != null) {
                    resultStatusTMP.text = passed 
                        ? "Excellent roleplay! You helped your cousin practise with kind, supportive guidance!" 
                        : "Almost there! Try choosing the kind mended sentence at each step!";
                }
            }

            if (passed) {
                if (celebrationAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(celebrationAudio);
                }
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }
                if (nextButton != null) {
                    nextButton.gameObject.SetActive(true);
                    nextButton.interactable = true;
                }
            } else {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }
            }
        }

        protected override void OnNextButtonClicked() {
            if (topic == Masters_Topic.None) return;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                Masters_AudioManager.Instance.StopVoiceOver();
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

        private void InitStepsIfEmpty() {
            if (steps != null && steps.Length > 0) return;

            steps = new CommonMistakes_RoleplayRP01Step[] {
                new CommonMistakes_RoleplayRP01Step {
                    stepId = 1,
                    stepTitle = "Step 1: Preposition Fix",
                    npcSpeakerName = "Cousin",
                    npcOpeningLine = "Have you been in London?",
                    studentFittingAnswer = "Have you been to London? — yes, last year!",
                    studentDistractorLine = "That's wrong.",
                    correctOptionIndex = 0,
                    optionChoices = new string[] {
                        "Have you been to London? — yes, last year!",
                        "That's wrong."
                    }
                },
                new CommonMistakes_RoleplayRP01Step {
                    stepId = 2,
                    stepTitle = "Step 2: Verb Form Fix",
                    npcSpeakerName = "Cousin",
                    npcOpeningLine = "Does she drinks milk?",
                    studentFittingAnswer = "Does she drink milk? Yes, every morning.",
                    studentDistractorLine = "No, that's not right.",
                    correctOptionIndex = 1,
                    optionChoices = new string[] {
                        "No, that's not right.",
                        "Does she drink milk? Yes, every morning."
                    }
                },
                new CommonMistakes_RoleplayRP01Step {
                    stepId = 3,
                    stepTitle = "Step 3: Word Choice Fix",
                    npcSpeakerName = "Cousin",
                    npcOpeningLine = "I don't use a watch.",
                    studentFittingAnswer = "I don't wear a watch — nor do I!",
                    studentDistractorLine = "You said it wrong.",
                    correctOptionIndex = 0,
                    optionChoices = new string[] {
                        "I don't wear a watch — nor do I!",
                        "You said it wrong."
                    }
                },
                new CommonMistakes_RoleplayRP01Step {
                    stepId = 4,
                    stepTitle = "Step 4: Word Order Fix",
                    npcSpeakerName = "Cousin",
                    npcOpeningLine = "Where has gone Sunil?",
                    studentFittingAnswer = "Where has Sunil gone? He's outside.",
                    studentDistractorLine = "Wrong order.",
                    correctOptionIndex = 1,
                    optionChoices = new string[] {
                        "Wrong order.",
                        "Where has Sunil gone? He's outside."
                    }
                },
                new CommonMistakes_RoleplayRP01Step {
                    stepId = 5,
                    stepTitle = "Step 5: Expression / Age Fix",
                    npcSpeakerName = "Cousin",
                    npcOpeningLine = "I have 26 years.",
                    studentFittingAnswer = "I'm 26 years old — that's how we say it.",
                    studentDistractorLine = "That's wrong.",
                    correctOptionIndex = 0,
                    optionChoices = new string[] {
                        "I'm 26 years old — that's how we say it.",
                        "That's wrong."
                    }
                }
            };
        }
    }
}
