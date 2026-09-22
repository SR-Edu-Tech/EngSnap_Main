using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit9 {

    

    /// <summary>
    /// RP01 On Stage — Fixing the Class Trip
    /// Guided multi-step roleplay dialogue for Book 2B Unit 9 (Prepositions of Time).
    /// A theatre stage set as a classroom noticeboard covered in dates.
    /// The Teacher (NPC - Female) asks when trip events happen; LEO (Student - Male) answers with the correct preposition (IN, ON, AT).
    /// Success condition: Student chooses a fitting line in at least 5 of 6 steps.
    /// </summary>
    public class Masters_PrepositionsOfTime_Roleplay_LessonOne : Masters_Lesson {

[System.Serializable]
    public class PrepositionsOfTime_RoleplayRP01Step {
        public int stepId;
        public string stepTitle;              // e.g. "Step 1: Month of the Trip"
        public string npcSpeakerName;         // "Teacher"
        public string npcOpeningLine;         // e.g. "Which month is the trip in?"
        public string studentFittingAnswer;   // e.g. "In August."
        public string studentDistractorLine;  // e.g. "On August. / At August."
        public int correctOptionIndex;        // 0, 1, or 2
        public string[] optionChoices;        // 3 option chips
        public AudioClip npcAudio;
        public AudioClip studentAudio;
    }
    
        [Header("RP01 6 Class Trip Planning Steps")]
        [SerializeField] private PrepositionsOfTime_RoleplayRP01Step[] steps;

        [Header("UI Display References")]
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
        [SerializeField] private Button[] optionButtons; // 3 Buttons (option1, option2, option3)
        [SerializeField] private Image[] optionImages;
        [SerializeField] private TextMeshProUGUI[] optionTexts;

        [Header("Audio Controls")]
        [SerializeField] private Button replayAudioBtn;

        [Header("Audio References")]
        [SerializeField] private AudioClip introAudio;
        [SerializeField] private AudioClip recapAudio;

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
        private int correctStepsCount = 0;
        private int attemptsOnCurrentStep = 0;
        private bool isProcessingTurn = false;

        private readonly Color defaultOptionColor = new Color(0.12f, 0.42f, 0.78f, 0.95f);
        private readonly Color correctOptionColor = new Color(0.15f, 0.75f, 0.35f, 1f);
        private readonly Color wrongOptionColor = new Color(0.85f, 0.25f, 0.25f, 1f);

        protected override void Awake() {
            topic = Masters_Topic.Roleplay;
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

        private void WireEventListeners() {
            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayNpcLine);
            }

            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    int optIdx = i;
                    if (optionButtons[i] != null) {
                        optionButtons[i].onClick.RemoveAllListeners();
                        optionButtons[i].onClick.AddListener(() => OnOptionSelected(optIdx));
                    }
                }
            }

            if (npcCloud != null) {
                Button npcBtn = npcCloud.GetComponent<Button>();
                if (npcBtn != null) {
                    npcBtn.onClick.RemoveAllListeners();
                    npcBtn.onClick.AddListener(ReplayNpcLine);
                }
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RestartRoleplay);
            }

            if (returnHubBtn != null) {
                returnHubBtn.onClick.RemoveAllListeners();
                returnHubBtn.onClick.AddListener(OnReturnHubClicked);
            }

            if (nextButton != null) {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
            }
        }

        public void RestartRoleplay() {
            currentStepIndex = 0;
            correctStepsCount = 0;
            attemptsOnCurrentStep = 0;
            isProcessingTurn = false;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);

            if (headerTMP != null) headerTMP.gameObject.SetActive(true);
            if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);

            UpdateHUD();
            StartCoroutine(PlayIntroThenStart());
        }

        private IEnumerator PlayIntroThenStart() {
            if (npcCloud != null) npcCloud.SetActive(false);
            if (studentCloud != null) studentCloud.SetActive(false);
            if (optionsContainer != null) optionsContainer.SetActive(false);

            AudioClip introClip = introAudio != null ? introAudio : narratorSpeech;
            if (introClip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(introClip);
                yield return new WaitForSeconds(introClip.length + 0.3f);
            } else {
                yield return new WaitForSeconds(0.4f);
            }

            if (optionsContainer != null) optionsContainer.SetActive(true);
            ShowStep(0);
        }

        private void ShowStep(int stepIndex) {
            if (steps == null || steps.Length == 0) return;
            currentStepIndex = Mathf.Clamp(stepIndex, 0, steps.Length - 1);
            attemptsOnCurrentStep = 0;
            isProcessingTurn = false;

            PrepositionsOfTime_RoleplayRP01Step step = steps[currentStepIndex];

            if (titleTMP != null) titleTMP.text = "RP01 ON STAGE — FIXING THE CLASS TRIP";
            if (subtitleTMP != null) subtitleTMP.text = "Choose the answer with the correct preposition of time.";

            UpdateHUD();

            // Show NPC speech bubble
            if (npcCloud != null) {
                npcCloud.SetActive(true);
                npcCloud.transform.DOKill();
                npcCloud.transform.localScale = Vector3.zero;
                npcCloud.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            }
            if (npcDialogueTMP != null) {
                npcDialogueTMP.text = $"<color=#0B5394><b>Teacher:</b></color>\n\"{step.npcOpeningLine}\"";
                npcDialogueTMP.color = new Color(0.1f, 0.15f, 0.25f, 1f);
                npcDialogueTMP.enableAutoSizing = true;
                npcDialogueTMP.fontSizeMin = 16f;
                npcDialogueTMP.fontSizeMax = 24f;
                npcDialogueTMP.alignment = TextAlignmentOptions.Center;
                npcDialogueTMP.enableWordWrapping = true;
                npcDialogueTMP.ForceMeshUpdate();
            }

            // Hide student speech bubble initially
            if (studentCloud != null) {
                studentCloud.SetActive(false);
            }

            // Play NPC voiceover
            PlayNpcLine();

            // Populate and enable Option buttons
            SetupOptions(step);
        }

        private void SetupOptions(PrepositionsOfTime_RoleplayRP01Step step) {
            if (optionsContainer != null) {
                optionsContainer.SetActive(true);
            }

            if (optionButtons != null && step.optionChoices != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (optionButtons[i] != null) {
                        if (i < step.optionChoices.Length) {
                            optionButtons[i].gameObject.SetActive(true);
                            optionButtons[i].interactable = true;

                            if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) {
                                optionTexts[i].text = step.optionChoices[i];
                                optionTexts[i].color = Color.white;
                                optionTexts[i].enableAutoSizing = true;
                                optionTexts[i].fontSizeMin = 14f;
                                optionTexts[i].fontSizeMax = 22f;
                                optionTexts[i].alignment = TextAlignmentOptions.Center;
                                optionTexts[i].enableWordWrapping = true;
                                optionTexts[i].margin = new Vector4(12, 6, 12, 6);
                                optionTexts[i].ForceMeshUpdate();
                            }

                            if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                                optionImages[i].color = defaultOptionColor;
                                optionImages[i].transform.localScale = Vector3.one;
                            }
                        } else {
                            optionButtons[i].gameObject.SetActive(false);
                        }
                    }
                }
            }
        }

        public void OnOptionSelected(int optionIndex) {
            if (isProcessingTurn) return;
            if (steps == null || currentStepIndex < 0 || currentStepIndex >= steps.Length) return;

            PrepositionsOfTime_RoleplayRP01Step step = steps[currentStepIndex];
            attemptsOnCurrentStep++;

            bool isCorrect = (optionIndex == step.correctOptionIndex);
            StartCoroutine(HandleOptionSelectedCoroutine(isCorrect, optionIndex, step));
        }

        private IEnumerator HandleOptionSelectedCoroutine(bool isCorrect, int optionIndex, PrepositionsOfTime_RoleplayRP01Step step) {
            isProcessingTurn = true;
            EnableOptionButtons(false);

            if (isCorrect) {
                if (attemptsOnCurrentStep == 1) {
                    correctStepsCount++;
                }
                UpdateHUD();

                // Highlight chosen option green
                if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                    optionImages[optionIndex].color = correctOptionColor;
                    optionImages[optionIndex].transform.DOPunchScale(Vector3.one * 0.1f, 0.25f);
                }

                // Show Student speech bubble
                if (studentCloud != null) {
                    studentCloud.SetActive(true);
                    studentCloud.transform.DOKill();
                    studentCloud.transform.localScale = Vector3.zero;
                    studentCloud.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                }
                if (studentDialogueTMP != null) {
                    studentDialogueTMP.text = $"<color=#D35400><b>Leo:</b></color>\n\"{step.studentFittingAnswer}\"";
                    studentDialogueTMP.color = new Color(0.1f, 0.15f, 0.25f, 1f);
                    studentDialogueTMP.enableAutoSizing = true;
                    studentDialogueTMP.fontSizeMin = 16f;
                    studentDialogueTMP.fontSizeMax = 24f;
                    studentDialogueTMP.alignment = TextAlignmentOptions.Center;
                    studentDialogueTMP.enableWordWrapping = true;
                    studentDialogueTMP.ForceMeshUpdate();
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                // Play student voice line (Leo)
                PlayStudentLine();

                float waitDuration = (step.studentAudio != null && step.studentAudio.length > 0) ? step.studentAudio.length + 0.6f : 2.5f;
                yield return new WaitForSeconds(waitDuration);

                // Advance to next step or end
                if (currentStepIndex + 1 < steps.Length) {
                    ShowStep(currentStepIndex + 1);
                } else {
                    EndRoleplay();
                }
            } else {
                // Incorrect choice
                if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                    optionImages[optionIndex].color = wrongOptionColor;
                    optionImages[optionIndex].transform.DOShakePosition(0.4f, 8f, 15, 90, false, true);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                ShowFeedback("Incorrect preposition! Remember: IN for months/seasons, ON for days/dates, AT for exact times!", 2.0f);
                yield return new WaitForSeconds(1.8f);

                // Reset option colors and re-enable for retry
                ResetOptionColors();
                EnableOptionButtons(true);
                isProcessingTurn = false;
            }
        }

        public void PlayNpcLine() {
            if (steps == null || currentStepIndex < 0 || currentStepIndex >= steps.Length) return;
            AudioClip clip = steps[currentStepIndex].npcAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }

        public void PlayStudentLine() {
            if (steps == null || currentStepIndex < 0 || currentStepIndex >= steps.Length) return;
            AudioClip clip = steps[currentStepIndex].studentAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }

        public void ReplayNpcLine() {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            PlayNpcLine();
        }

        private void ResetOptionColors() {
            if (optionImages == null) return;
            for (int i = 0; i < optionImages.Length; i++) {
                if (optionImages[i] != null) {
                    optionImages[i].color = defaultOptionColor;
                    optionImages[i].transform.localScale = Vector3.one;
                }
            }
        }

        private void EnableOptionButtons(bool enable) {
            if (optionButtons == null) return;
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) optionButtons[i].interactable = enable;
            }
        }

        private void ShowFeedback(string msg, float duration) {
            if (feedbackBanner == null) return;
            feedbackBanner.SetActive(true);
            if (feedbackTextTMP != null) feedbackTextTMP.text = msg;

            feedbackBanner.transform.DOKill();
            feedbackBanner.transform.localScale = Vector3.zero;
            feedbackBanner.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);

            StartCoroutine(HideFeedbackCoroutine(duration));
        }

        private IEnumerator HideFeedbackCoroutine(float delay) {
            yield return new WaitForSeconds(delay);
            if (feedbackBanner != null) {
                feedbackBanner.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => feedbackBanner.SetActive(false));
            }
        }

        private void EndRoleplay() {
            isProcessingTurn = false;

            if (optionsContainer != null) optionsContainer.SetActive(false);
            if (feedbackBanner != null) feedbackBanner.SetActive(false);

            bool passed = (correctStepsCount >= 5);

            if (resultPanel != null) {
                resultPanel.SetActive(true);
                resultPanel.transform.DOKill();
                resultPanel.transform.localScale = Vector3.zero;
                resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

                if (resultTitleTMP != null) {
                    resultTitleTMP.text = passed ? "CLASS TRIP PLANNED!" : "ROLEPLAY COMPLETE!";
                    resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
                }

                if (resultScoreTMP != null) {
                    resultScoreTMP.text = $"You completed {correctStepsCount}/6 steps on first attempt! (Target: 5/6)";
                }

                if (resultStatusTMP != null) {
                    resultStatusTMP.text = passed 
                        ? "Outstanding! You used in, on, and at correctly to plan the entire trip!"
                        : "Try again to get at least 5 out of 6 fitting lines right on your first attempt!";
                }

                if (returnHubBtn != null) returnHubBtn.gameObject.SetActive(passed);
                if (retryBtn != null) retryBtn.gameObject.SetActive(!passed || correctStepsCount < 6);
            }

            if (recapAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(recapAudio);
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(passed);
            }

            if (passed && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
        }

        public void OnReturnHubClicked() {
            topic = Masters_Topic.Roleplay;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }

        protected override void OnNextButtonClicked() {
            topic = Masters_Topic.Roleplay;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }

        private void UpdateHUD() {
            if (progressTMP != null) progressTMP.text = $"Step {currentStepIndex + 1}/6";
            if (scoreTMP != null) scoreTMP.text = $"Score: {correctStepsCount}/6";
        }

        public void InitStepsIfEmpty() {
            string audioDir = "Assets/Audio/2B/9_PrepositionsOfTime/Roleplay/";

            if (introAudio == null) {
#if UNITY_EDITOR
                introAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_rp01_intro.mp3");
#endif
                if (narratorSpeech == null) narratorSpeech = introAudio;
            }

            if (recapAudio == null) {
#if UNITY_EDITOR
                recapAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_rp01_recap.mp3");
#endif
            }

            if (steps == null || steps.Length != 6) {
                steps = new PrepositionsOfTime_RoleplayRP01Step[] {
                    new PrepositionsOfTime_RoleplayRP01Step {
                        stepId = 1,
                        stepTitle = "Step 1: Month of the Trip",
                        npcSpeakerName = "Teacher",
                        npcOpeningLine = "Which month is the trip in?",
                        studentFittingAnswer = "In August.",
                        studentDistractorLine = "On August. / At August.",
                        correctOptionIndex = 0,
                        optionChoices = new string[] {
                            "In August.",
                            "On August.",
                            "At August."
                        },
#if UNITY_EDITOR
                        npcAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_rp01_s01_npc.mp3"),
                        studentAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_rp01_s01_student.mp3")
#endif
                    },
                    new PrepositionsOfTime_RoleplayRP01Step {
                        stepId = 2,
                        stepTitle = "Step 2: Season of the Trip",
                        npcSpeakerName = "Teacher",
                        npcOpeningLine = "And which season is that?",
                        studentFittingAnswer = "In Spring.",
                        studentDistractorLine = "On Spring. / At Spring.",
                        correctOptionIndex = 1,
                        optionChoices = new string[] {
                            "On Spring.",
                            "In Spring.",
                            "At Spring."
                        },
#if UNITY_EDITOR
                        npcAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_rp01_s02_npc.mp3"),
                        studentAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_rp01_s02_student.mp3")
#endif
                    },
                    new PrepositionsOfTime_RoleplayRP01Step {
                        stepId = 3,
                        stepTitle = "Step 3: Day of the Week",
                        npcSpeakerName = "Teacher",
                        npcOpeningLine = "Which day of the week shall we go?",
                        studentFittingAnswer = "On Tuesday.",
                        studentDistractorLine = "In Tuesday. / At Tuesday.",
                        correctOptionIndex = 2,
                        optionChoices = new string[] {
                            "In Tuesday.",
                            "At Tuesday.",
                            "On Tuesday."
                        },
#if UNITY_EDITOR
                        npcAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_rp01_s03_npc.mp3"),
                        studentAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_rp01_s03_student.mp3")
#endif
                    },
                    new PrepositionsOfTime_RoleplayRP01Step {
                        stepId = 4,
                        stepTitle = "Step 4: Full Date for Letter",
                        npcSpeakerName = "Teacher",
                        npcOpeningLine = "Can you give me the full date for the letter?",
                        studentFittingAnswer = "On 10th of August.",
                        studentDistractorLine = "In 10th of August. / At 10th of August.",
                        correctOptionIndex = 0,
                        optionChoices = new string[] {
                            "On 10th of August.",
                            "In 10th of August.",
                            "At 10th of August."
                        },
#if UNITY_EDITOR
                        npcAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_rp01_s04_npc.mp3"),
                        studentAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_rp01_s04_student.mp3")
#endif
                    },
                    new PrepositionsOfTime_RoleplayRP01Step {
                        stepId = 5,
                        stepTitle = "Step 5: Bus Departure Time",
                        npcSpeakerName = "Teacher",
                        npcOpeningLine = "What time does the bus leave?",
                        studentFittingAnswer = "At 4 o'clock.",
                        studentDistractorLine = "In 4 o'clock. / On 4 o'clock.",
                        correctOptionIndex = 1,
                        optionChoices = new string[] {
                            "In 4 o'clock.",
                            "At 4 o'clock.",
                            "On 4 o'clock."
                        },
#if UNITY_EDITOR
                        npcAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_rp01_s05_npc.mp3"),
                        studentAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_rp01_s05_student.mp3")
#endif
                    },
                    new PrepositionsOfTime_RoleplayRP01Step {
                        stepId = 6,
                        stepTitle = "Step 6: Lunch Time",
                        npcSpeakerName = "Teacher",
                        npcOpeningLine = "When will we eat?",
                        studentFittingAnswer = "At noon.",
                        studentDistractorLine = "In noon. / On noon.",
                        correctOptionIndex = 2,
                        optionChoices = new string[] {
                            "In noon.",
                            "On noon.",
                            "At noon."
                        },
#if UNITY_EDITOR
                        npcAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_rp01_s06_npc.mp3"),
                        studentAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_rp01_s06_student.mp3")
#endif
                    }
                };
            }
        }

        public void AutoBindReferences() {
            if (titleTMP == null) {
                Transform t = transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle") ?? transform.Find("Title") ?? transform.Find("HeaderContainer/LessonTitle") ?? transform.Find("CommonHUD/LessonTitle");
                if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (subtitleTMP == null) {
                Transform t = transform.Find("Select an appropriate response:") ?? transform.Find("Subtitle") ?? transform.Find("Instruction") ?? transform.Find("HeaderContainer/Subtitle") ?? transform.Find("CommonHUD/Subtitle");
                if (t != null) subtitleTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (headerTMP == null) {
                Transform t = transform.Find("Header") ?? transform.Find("BranchHeader") ?? transform.Find("HeaderContainer/Header");
                if (t != null) headerTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (progressTMP == null) {
                Transform t = transform.Find("ProgressCountTMP") ?? transform.Find("ProgressTMP") ?? transform.Find("progression count") ?? transform.Find("Progress") ?? transform.Find("HeaderContainer/ProgressTMP") ?? transform.Find("CommonHUD/ProgressTMP");
                if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (scoreTMP == null) {
                Transform t = transform.Find("ScoreTMP") ?? transform.Find("Score") ?? transform.Find("HeaderContainer/ScoreTMP") ?? transform.Find("CommonHUD/ScoreTMP");
                if (t != null) scoreTMP = t.GetComponent<TextMeshProUGUI>();
            }

            // Characters & speech clouds
            if (npcAndStudentGameObject == null) {
                Transform t = transform.Find("NPCAndStudent") ?? transform.Find("Characters") ?? transform.Find("StageSet");
                if (t != null) npcAndStudentGameObject = t.gameObject;
            }

            if (npcCloud == null) {
                Transform t = transform.Find("NPCCloud") ?? transform.Find("TeacherCloud") ?? transform.Find("DoctorCloud") ?? transform.Find("ShopkeeperCloud") ?? transform.Find("NPCAndStudent/NPCCloud");
                if (t != null) npcCloud = t.gameObject;
            }
            if (npcDialogueTMP == null && npcCloud != null) {
                npcDialogueTMP = npcCloud.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            if (studentCloud == null) {
                Transform t = transform.Find("StudentCloud") ?? transform.Find("LeoCloud") ?? transform.Find("HenryCloud") ?? transform.Find("JoyCloud") ?? transform.Find("NPCAndStudent/StudentCloud");
                if (t != null) studentCloud = t.gameObject;
            }
            if (studentDialogueTMP == null && studentCloud != null) {
                studentDialogueTMP = studentCloud.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            // Options Container
            if (optionsContainer == null) {
                Transform t = transform.Find("optionscontainer") ?? transform.Find("OptionsContainer") ?? transform.Find("OptionsGrid") ?? transform.Find("ResponsesContainer");
                if (t != null) optionsContainer = t.gameObject;
            }

            if (optionsContainer != null) {
                Transform opt1Tr = optionsContainer.transform.Find("option1");
                Transform opt2Tr = optionsContainer.transform.Find("option2");
                Transform opt3Tr = optionsContainer.transform.Find("option3");

                List<Button> btns = new List<Button>();
                List<Image> imgs = new List<Image>();
                List<TextMeshProUGUI> txts = new List<TextMeshProUGUI>();

                Transform[] optTrs = new Transform[] { opt1Tr, opt2Tr, opt3Tr };
                foreach (var optTr in optTrs) {
                    if (optTr != null) {
                        Button b = optTr.GetComponent<Button>();
                        Image im = optTr.GetComponent<Image>();
                        TextMeshProUGUI tx = optTr.GetComponentInChildren<TextMeshProUGUI>(true);
                        if (b != null) btns.Add(b);
                        if (im != null) imgs.Add(im);
                        if (tx != null) txts.Add(tx);
                    }
                }

                if (btns.Count > 0) optionButtons = btns.ToArray();
                if (imgs.Count > 0) optionImages = imgs.ToArray();
                if (txts.Count > 0) optionTexts = txts.ToArray();
            }

            // Replay Audio Button
            if (replayAudioBtn == null) {
                Transform t = transform.Find("ReplayAudioBtn") ?? transform.Find("SpeakerButton") ?? transform.Find("AudioButton");
                if (t != null) replayAudioBtn = t.GetComponent<Button>();
            }

            // Feedback Banner
            if (feedbackBanner == null) {
                Transform t = transform.Find("FeedbackBanner") ?? transform.Find("Feedback");
                if (t != null) feedbackBanner = t.gameObject;
            }
            if (feedbackTextTMP == null && feedbackBanner != null) {
                feedbackTextTMP = feedbackBanner.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            // Result Panel
            if (resultPanel == null) {
                Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
                if (t != null) resultPanel = t.gameObject;
            }
            if (resultTitleTMP == null && resultPanel != null) {
                Transform t = resultPanel.transform.Find("ResultTitle") ?? resultPanel.transform.Find("Title");
                if (t != null) resultTitleTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (resultScoreTMP == null && resultPanel != null) {
                Transform t = resultPanel.transform.Find("ResultScore") ?? resultPanel.transform.Find("Score");
                if (t != null) resultScoreTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (resultStatusTMP == null && resultPanel != null) {
                Transform t = resultPanel.transform.Find("ResultStatus") ?? resultPanel.transform.Find("Status");
                if (t != null) resultStatusTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (retryBtn == null && resultPanel != null) {
                Transform t = resultPanel.transform.Find("RetryButton") ?? resultPanel.transform.Find("RetryBtn");
                if (t != null) retryBtn = t.GetComponent<Button>();
            }
            if (returnHubBtn == null && resultPanel != null) {
                Transform t = resultPanel.transform.Find("ReturnHubButton") ?? resultPanel.transform.Find("ReturnHubBtn") ?? resultPanel.transform.Find("NextButton");
                if (t != null) returnHubBtn = t.GetComponent<Button>();
            }
        }
    }
}
