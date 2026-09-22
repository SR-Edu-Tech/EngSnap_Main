using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit5 {

    /// <summary>
    /// R03 Book the Appointment — Put the Call in Order
    /// Reception desk conversation sequencing controller for Book 2B Unit 5 (Doctor Need Your Help).
    /// Orders the 10-line clinic phone call between Steve and the Receptionist.
    /// Success condition: Student places at least 8 of 10 lines in the correct order.
    /// </summary>
    public class Masters_DoctorNeedYourHelp_Reading_LessonThree : Masters_Lesson {

[System.Serializable]
    public class DoctorAppointmentCallLine {
        public int stepIndex;
        public string speakerName;
        public string lineText;
        public string[] optionChoices;     // 3 shuffled options for this step
        public int correctOptionIndex;     // 0..2
        public AudioClip lineAudio;
    }
    
        [Header("10 Appointment Call Steps")]
        [SerializeField] private DoctorAppointmentCallLine[] callSteps;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;

        [Header("Conversation Call Display")]
        [SerializeField] private GameObject conversationCardObject;
        [SerializeField] private TextMeshProUGUI stepPromptTMP;
        [SerializeField] private TextMeshProUGUI callTranscriptTMP;
        [SerializeField] private Button replayAudioBtn;

        [Header("3 Shuffled Speech Bubble Option Chips")]
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private TextMeshProUGUI[] optionTexts;
        [SerializeField] private Image[] optionImages;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleTMP;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;
        [SerializeField] private Button returnHubBtn;

        [Header("Audio References")]
        [SerializeField] private AudioClip introAudio;
        [SerializeField] private AudioClip recapAudio;
        [SerializeField] private AudioClip sfxCorrect;
        [SerializeField] private AudioClip sfxWrong;

        private int currentStepIndex = 0;
        private int score = 0;
        private bool isHandlingAnswer = false;
        private bool isIntroPhase = true;
        private List<string> completedTranscript = new List<string>();

        private Color defaultChipColor = new Color(0.14f, 0.45f, 0.75f, 0.95f);
        private Color correctChipColor = new Color(0.15f, 0.75f, 0.35f, 1f);
        private Color wrongChipColor = new Color(0.85f, 0.25f, 0.25f, 1f);

        protected override void Awake() {
            topic = Masters_Topic.Reading;
            base.Awake();

            AutoBindReferences();
            InitCallStepsIfEmpty();
            WireEventListeners();
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Reading;
            AutoBindReferences();
            WireEventListeners();
            EnsureNextAndBackButtonWired();

            if (callSteps == null || callSteps.Length == 0) {
                InitCallStepsIfEmpty();
            }

            SetControlsInteractable(false);
            StartCoroutine(BeginLessonRoutine());
        }

        private IEnumerator BeginLessonRoutine() {
            isIntroPhase = true;
            if (progressTMP != null) progressTMP.text = "1/10";
            if (scoreTMP != null) scoreTMP.text = "Score: 0/10";
            if (headerTMP != null) headerTMP.text = "RECEPTION DESK 📞";
            if (titleTMP != null) {
                titleTMP.text = "Book the Appointment — Put the Call in Order";
                titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
            }
            if (subtitleTMP != null) subtitleTMP.text = "Tap the next line in order to complete the appointment call.";

            if (callSteps != null && callSteps.Length > 0) {
                if (stepPromptTMP != null) stepPromptTMP.text = $"LINE #1 ({callSteps[0].speakerName})";
                if (callTranscriptTMP != null) callTranscriptTMP.text = "<i>(Phone rings... Call begins)</i>";
            }

            AudioClip clipToPlay = introAudio != null ? introAudio : narratorSpeech;
            if (clipToPlay != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
                yield return new WaitForSeconds(clipToPlay.length + 0.3f);
            } else {
                yield return new WaitForSeconds(1.0f);
            }

            isIntroPhase = false;
            RestartLesson();
        }

        private void SetControlsInteractable(bool interactable) {
            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (optionButtons[i] != null) optionButtons[i].interactable = interactable;
                }
            }
            if (replayAudioBtn != null) replayAudioBtn.interactable = interactable;
        }

        private void WireEventListeners() {
            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentAudio);
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RestartLesson);
            }

            if (returnHubBtn != null) {
                returnHubBtn.onClick.RemoveAllListeners();
                returnHubBtn.onClick.AddListener(OnNextButtonClicked);
            }

            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    int idx = i;
                    if (optionButtons[i] != null) {
                        optionButtons[i].onClick.RemoveAllListeners();
                        optionButtons[i].onClick.AddListener(() => OnOptionClicked(idx));
                    }
                }
            }
        }

        public void InitCallStepsIfEmpty() {
            string audioDir = "Assets/Audio/2B/5_DoctorNeedYourHelp/Reading/";

            callSteps = new DoctorAppointmentCallLine[] {
                new DoctorAppointmentCallLine {
                    stepIndex = 1,
                    speakerName = "Steve",
                    lineText = "I want to consult a doctor.",
                    optionChoices = new string[] {
                        "I want to consult a doctor.",
                        "Please be here with your mom.",
                        "Is it urgent?"
                    },
                    correctOptionIndex = 0,
#if UNITY_EDITOR
                    lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r03_line1_steve.mp3")
#endif
                },
                new DoctorAppointmentCallLine {
                    stepIndex = 2,
                    speakerName = "Receptionist",
                    lineText = "Do you have an appointment?",
                    optionChoices = new string[] {
                        "Yes, that will be fine.",
                        "Do you have an appointment?",
                        "Mrs. Susan John."
                    },
                    correctOptionIndex = 1,
#if UNITY_EDITOR
                    lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r03_line2_rec.mp3")
#endif
                },
                new DoctorAppointmentCallLine {
                    stepIndex = 3,
                    speakerName = "Steve",
                    lineText = "No, I don't. I'd like to take an appointment with Dr Gupta, for my mom, please.",
                    optionChoices = new string[] {
                        "Just a minute, I'll check.",
                        "I will. Thank you very much.",
                        "No, I don't. I'd like to take an appointment with Dr Gupta, for my mom, please."
                    },
                    correctOptionIndex = 2,
#if UNITY_EDITOR
                    lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r03_line3_steve.mp3")
#endif
                },
                new DoctorAppointmentCallLine {
                    stepIndex = 4,
                    speakerName = "Receptionist",
                    lineText = "Just a minute, I'll just check. Will tomorrow afternoon suit you?",
                    optionChoices = new string[] {
                        "Just a minute, I'll just check. Will tomorrow afternoon suit you?",
                        "No, my mom is in lot of pain.",
                        "I want to consult a doctor."
                    },
                    correctOptionIndex = 0,
#if UNITY_EDITOR
                    lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r03_line4_rec.mp3")
#endif
                },
                new DoctorAppointmentCallLine {
                    stepIndex = 5,
                    speakerName = "Steve",
                    lineText = "No, my mom is in lot of pain and she really needs a help.",
                    optionChoices = new string[] {
                        "May I know your mom's name?",
                        "No, my mom is in lot of pain and she really needs a help.",
                        "Will that be alright?"
                    },
                    correctOptionIndex = 1,
#if UNITY_EDITOR
                    lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r03_line5_steve.mp3")
#endif
                },
                new DoctorAppointmentCallLine {
                    stepIndex = 6,
                    speakerName = "Receptionist / Steve",
                    lineText = "Is it urgent? / Yes, I think so.",
                    optionChoices = new string[] {
                        "Please be here at 1:30 p.m.",
                        "Do you have an appointment?",
                        "Is it urgent? / Yes, I think so."
                    },
                    correctOptionIndex = 2,
#if UNITY_EDITOR
                    lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r03_line6_rec_steve.mp3")
#endif
                },
                new DoctorAppointmentCallLine {
                    stepIndex = 7,
                    speakerName = "Receptionist",
                    lineText = "Just a moment please. I think you can come at 1:30 this afternoon. Will that be alright?",
                    optionChoices = new string[] {
                        "Just a moment please. I think you can come at 1:30 this afternoon. Will that be alright?",
                        "I want to consult a doctor.",
                        "Mrs. Susan John."
                    },
                    correctOptionIndex = 0,
#if UNITY_EDITOR
                    lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r03_line7_rec.mp3")
#endif
                },
                new DoctorAppointmentCallLine {
                    stepIndex = 8,
                    speakerName = "Steve",
                    lineText = "Yes, that will be fine.",
                    optionChoices = new string[] {
                        "Will tomorrow afternoon suit you?",
                        "Yes, that will be fine.",
                        "No, I don't."
                    },
                    correctOptionIndex = 1,
#if UNITY_EDITOR
                    lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r03_line8_steve.mp3")
#endif
                },
                new DoctorAppointmentCallLine {
                    stepIndex = 9,
                    speakerName = "Receptionist / Steve",
                    lineText = "May I know your mom's name, please? / Mrs. Susan John.",
                    optionChoices = new string[] {
                        "Do you have an appointment?",
                        "Just a minute, I'll check.",
                        "May I know your mom's name, please? / Mrs. Susan John."
                    },
                    correctOptionIndex = 2,
#if UNITY_EDITOR
                    lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r03_line9_rec_steve.mp3")
#endif
                },
                new DoctorAppointmentCallLine {
                    stepIndex = 10,
                    speakerName = "Receptionist / Steve",
                    lineText = "Please be here with your mom at 1:30 p.m., Mr. Steve. / I will. Thank you very much.",
                    optionChoices = new string[] {
                        "Please be here with your mom at 1:30 p.m., Mr. Steve. / I will. Thank you very much.",
                        "I want to consult a doctor.",
                        "Yes, that will be fine."
                    },
                    correctOptionIndex = 0,
#if UNITY_EDITOR
                    lineAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r03_line10_rec_steve.mp3")
#endif
                }
            };
        }

        public void RestartLesson() {
            currentStepIndex = 0;
            score = 0;
            isHandlingAnswer = false;
            completedTranscript.Clear();

            if (headerTMP != null) headerTMP.gameObject.SetActive(true);
            if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (conversationCardObject != null) conversationCardObject.SetActive(true);

            Transform grid = transform.Find("OptionsContainer") ?? transform.Find("OptionChipsGrid") ?? transform.Find("OptionsGrid");
            if (grid != null) grid.gameObject.SetActive(true);

            UpdateScoreUI();
            ShowStep(0);
        }

        public void ShowStep(int stepIdx) {
            if (callSteps == null || callSteps.Length == 0) return;
            currentStepIndex = Mathf.Clamp(stepIdx, 0, callSteps.Length - 1);
            DoctorAppointmentCallLine step = callSteps[currentStepIndex];

            if (progressTMP != null) progressTMP.text = $"{currentStepIndex + 1}/{callSteps.Length}";
            if (headerTMP != null) headerTMP.text = "RECEPTION DESK 📞";
            if (titleTMP != null) {
                titleTMP.text = "Book the Appointment — Put the Call in Order";
                titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
            }
            if (subtitleTMP != null) subtitleTMP.text = "Tap the next line in order to complete the appointment call.";

            if (stepPromptTMP != null) stepPromptTMP.text = $"LINE #{currentStepIndex + 1} ({step.speakerName})";

            // Update transcript display
            UpdateTranscriptDisplay();

            // Setup Options
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] == null) continue;
                optionButtons[i].interactable = true;
                optionButtons[i].transform.localScale = Vector3.one;

                if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                    optionImages[i].color = defaultChipColor;
                }

                if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) {
                    if (step.optionChoices != null && i < step.optionChoices.Length) {
                        optionTexts[i].text = step.optionChoices[i];
                    }
                }
            }
        }

        private void UpdateTranscriptDisplay() {
            if (callTranscriptTMP == null) return;
            if (completedTranscript.Count == 0) {
                callTranscriptTMP.text = "<i>(Phone rings... Call begins)</i>";
            } else {
                string transcriptText = "";
                int start = Mathf.Max(0, completedTranscript.Count - 3);
                for (int i = start; i < completedTranscript.Count; i++) {
                    transcriptText += completedTranscript[i] + "\n";
                }
                callTranscriptTMP.text = transcriptText.TrimEnd();
            }
        }

        public void PlayCurrentAudio() {
            if (callSteps == null || currentStepIndex >= callSteps.Length) return;
            AudioClip clip = callSteps[currentStepIndex].lineAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }

        public void ReplayCurrentAudio() {
            if (isIntroPhase) return;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            PlayCurrentAudio();
        }

        public void OnOptionClicked(int optionIndex) {
            if (isHandlingAnswer || isIntroPhase) return;
            if (callSteps == null || currentStepIndex >= callSteps.Length) return;

            DoctorAppointmentCallLine step = callSteps[currentStepIndex];
            bool isCorrect = (optionIndex == step.correctOptionIndex);

            StartCoroutine(HandleAnswerCoroutine(optionIndex, isCorrect));
        }

        private IEnumerator HandleAnswerCoroutine(int optionIndex, bool isCorrect) {
            isHandlingAnswer = true;

            // Lock all options
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) optionButtons[i].interactable = false;
            }

            DoctorAppointmentCallLine step = callSteps[currentStepIndex];

            if (isCorrect) {
                score++;
                UpdateScoreUI();

                if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                    optionImages[optionIndex].color = correctChipColor;
                }

                if (optionButtons[optionIndex] != null) {
                    optionButtons[optionIndex].transform.DOScale(1.08f, 0.18f).SetLoops(2, LoopType.Yoyo);
                }

                // Add to transcript
                completedTranscript.Add($"<b>{step.speakerName}:</b> \"{step.lineText}\"");
                UpdateTranscriptDisplay();

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }
                if (sfxCorrect != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(sfxCorrect);
                }

                yield return new WaitForSeconds(1.5f);
            } else {
                if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                    optionImages[optionIndex].color = wrongChipColor;
                }

                int correctIdx = step.correctOptionIndex;
                if (optionImages != null && correctIdx < optionImages.Length && optionImages[correctIdx] != null) {
                    optionImages[correctIdx].color = correctChipColor;
                }

                if (optionButtons[optionIndex] != null) {
                    optionButtons[optionIndex].transform.DOShakePosition(0.4f, 12f, 14, 90, false, true);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }
                if (sfxWrong != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(sfxWrong);
                }

                yield return new WaitForSeconds(1.8f);
            }

            isHandlingAnswer = false;

            if (currentStepIndex < callSteps.Length - 1) {
                ShowStep(currentStepIndex + 1);
            } else {
                ShowResults();
            }
        }

        private void UpdateScoreUI() {
            if (scoreTMP != null) {
                scoreTMP.text = $"Score: {score}/{callSteps.Length}";
            }
        }

        private void ShowResults() {
            if (conversationCardObject != null) conversationCardObject.SetActive(false);
            
            Transform grid = transform.Find("OptionsContainer") ?? transform.Find("OptionChipsGrid") ?? transform.Find("OptionsGrid");
            if (grid != null) grid.gameObject.SetActive(false);

            if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);

            if (resultPanel == null) {
                Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
                if (t != null) resultPanel = t.gameObject;
            }

            bool passed = (score >= 8);

            if (resultPanel != null) {
                resultPanel.SetActive(true);
                resultPanel.transform.DOKill();
                resultPanel.transform.localScale = Vector3.zero;
                resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

                if (resultTitleTMP != null) {
                    resultTitleTMP.text = passed ? "APPOINTMENT BOOKED! 📞" : "KEEP PRACTICING!";
                    resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
                }

                if (resultScoreTMP != null) {
                    resultScoreTMP.text = $"You placed {score} of {callSteps.Length} conversation lines in the correct order!";
                }

                if (resultStatusTMP != null) {
                    resultStatusTMP.text = passed ? "Success! You booked the doctor appointment smoothly from start to finish." : "You need at least 8/10 lines in order to complete this lesson.";
                }

                if (returnHubBtn != null) {
                    returnHubBtn.gameObject.SetActive(passed);
                }

                if (retryBtn != null) {
                    retryBtn.gameObject.SetActive(!passed || score < callSteps.Length);
                }
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(passed);
            }

            if (passed && recapAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(recapAudio);
            } else if (passed && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
        }

        protected override void OnNextButtonClicked() {
            topic = Masters_Topic.Reading;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }

        private void AutoBindReferences() {
            if (titleTMP == null) {
                Transform t = transform.Find("LessonTitle") ?? transform.Find("Title") ?? transform.Find("Header/Title") ?? transform.Find("HeaderContainer/LessonTitle");
                if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (progressTMP == null) {
                Transform t = transform.Find("ProgressTMP") ?? transform.Find("Progress") ?? transform.Find("Header/Progress") ?? transform.Find("HeaderContainer/ProgressTMP");
                if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (scoreTMP == null) {
                Transform t = transform.Find("ScoreTMP") ?? transform.Find("Score") ?? transform.Find("Header/Score") ?? transform.Find("HeaderContainer/ScoreTMP");
                if (t != null) scoreTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (conversationCardObject == null) {
                Transform t = transform.Find("ConversationCard") ?? transform.Find("NoticeboardCard") ?? transform.Find("PromptCard") ?? transform.Find("CenterCard");
                if (t != null) conversationCardObject = t.gameObject;
            }
            if (stepPromptTMP == null && conversationCardObject != null) {
                Transform t = conversationCardObject.transform.Find("NoticeHeaderTMP") ?? conversationCardObject.transform.Find("PromptHeaderTMP") ?? conversationCardObject.transform.Find("Header");
                if (t != null) stepPromptTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (callTranscriptTMP == null && conversationCardObject != null) {
                Transform t = conversationCardObject.transform.Find("SituationTextTMP") ?? conversationCardObject.transform.Find("TranscriptTMP") ?? conversationCardObject.transform.Find("Text");
                if (t != null) callTranscriptTMP = t.GetComponent<TextMeshProUGUI>();
            }
            if (replayAudioBtn == null) {
                Transform t = transform.Find("ReplayAudioBtn") ?? transform.Find("ConversationCard/ReplayAudioBtn") ?? transform.Find("NoticeboardCard/ReplayAudioBtn") ?? transform.Find("SpeakerBtn");
                if (t != null) replayAudioBtn = t.GetComponent<Button>();
            }
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

            // Find 3 option buttons
            if (optionButtons == null || optionButtons.Length == 0) {
                Transform grid = transform.Find("OptionsContainer") ?? transform.Find("OptionChipsGrid") ?? transform.Find("SelectionPanel") ?? transform.Find("OptionsGrid");
                if (grid != null) {
                    List<Button> btns = new List<Button>();
                    List<TextMeshProUGUI> txts = new List<TextMeshProUGUI>();
                    List<Image> imgs = new List<Image>();

                    for (int i = 0; i < grid.childCount; i++) {
                        Button b = grid.GetChild(i).GetComponent<Button>();
                        if (b != null) {
                            btns.Add(b);
                            imgs.Add(b.GetComponent<Image>());
                            txts.Add(b.GetComponentInChildren<TextMeshProUGUI>());
                        }
                    }
                    optionButtons = btns.ToArray();
                    optionTexts = txts.ToArray();
                    optionImages = imgs.ToArray();
                }
            }
        }
    }
}
