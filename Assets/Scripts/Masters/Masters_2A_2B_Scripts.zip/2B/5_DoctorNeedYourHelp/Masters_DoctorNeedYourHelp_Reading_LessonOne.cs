using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit5 {

    /// <summary>
    /// R01 What Would You Tell the Doctor? (in-context)
    /// Waiting-room noticeboard reading lesson controller for Book 2B Unit 5 (Doctor Need Your Help).
    /// Displays 12 clinical situations and 4 symptom sentence answer chips.
    /// Success condition: Student picks the correct sentence for at least 10 of 12 situations.
    /// </summary>
    public class Masters_DoctorNeedYourHelp_Reading_LessonOne : Masters_Lesson {

[System.Serializable]
    public class DoctorReadingR01Round {
        public int roundId;
        public string situationText;
        public string correctSymptom;
        public string[] optionSymptoms;
        public int correctOptionIndex;
        public AudioClip symptomAudio;
    }
    
        [Header("R01 12 Situation Rounds")]
        [SerializeField] private DoctorReadingR01Round[] rounds;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;

        [Header("Noticeboard Situation Card")]
        [SerializeField] private GameObject noticeboardCardObject;
        [SerializeField] private Image noticeboardCardBg;
        [SerializeField] private TextMeshProUGUI noticeboardHeaderTMP;
        [SerializeField] private TextMeshProUGUI situationTextTMP;
        [SerializeField] private Button replayAudioBtn;

        [Header("4 Symptom Answer Option Chips")]
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

        private int currentRoundIndex = 0;
        private int score = 0;
        private bool isHandlingAnswer = false;
        private bool isIntroPhase = true;

        private Color defaultChipColor = new Color(0.14f, 0.45f, 0.75f, 0.95f);
        private Color correctChipColor = new Color(0.15f, 0.75f, 0.35f, 1f);
        private Color wrongChipColor = new Color(0.85f, 0.25f, 0.25f, 1f);

        protected override void Awake() {
            topic = Masters_Topic.Reading;
            base.Awake();

            AutoBindReferences();
            InitRoundsIfEmpty();
            WireEventListeners();
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Reading;
            AutoBindReferences();
            WireEventListeners();
            EnsureNextAndBackButtonWired();

            if (rounds == null || rounds.Length == 0) {
                InitRoundsIfEmpty();
            }

            SetControlsInteractable(false);
            StartCoroutine(BeginLessonRoutine());
        }

        private IEnumerator BeginLessonRoutine() {
            isIntroPhase = true;
            if (progressTMP != null) progressTMP.text = "1/12";
            if (scoreTMP != null) scoreTMP.text = "Score: 0/12";
            if (headerTMP != null) headerTMP.text = "NOTICEBOARD 📋";
            if (titleTMP != null) {
                titleTMP.text = "What Would You Tell the Doctor?";
                titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
            }
            if (subtitleTMP != null) subtitleTMP.text = "Read the situation and tap the symptom sentence that fits.";

            if (rounds != null && rounds.Length > 0) {
                if (noticeboardHeaderTMP != null) noticeboardHeaderTMP.text = "SITUATION #1";
                if (situationTextTMP != null) situationTextTMP.text = $"\"{rounds[0].situationText}\"";
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

        public void InitRoundsIfEmpty() {
            string audioDir = "Assets/Audio/2B/5_DoctorNeedYourHelp/Reading/";

            rounds = new DoctorReadingR01Round[] {
                new DoctorReadingR01Round {
                    roundId = 1,
                    situationText = "You have been sneezing all morning and reaching for tissues.",
                    correctSymptom = "My nose is runny.",
                    optionSymptoms = new string[] { "My nose is runny.", "My eyes are watery.", "My arm is hurt.", "My legs feel weak." },
                    correctOptionIndex = 0,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r01_round1_nose.mp3")
#endif
                },
                new DoctorReadingR01Round {
                    roundId = 2,
                    situationText = "Your eyes keep watering in the dusty room.",
                    correctSymptom = "My eyes are watery.",
                    optionSymptoms = new string[] { "My ears are itching!", "My eyes are watery.", "My nose is runny.", "I cut my tongue." },
                    correctOptionIndex = 1,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r01_round2_eyes.mp3")
#endif
                },
                new DoctorReadingR01Round {
                    roundId = 3,
                    situationText = "You have been coughing since last night and your throat feels dry.",
                    correctSymptom = "My throat is dry! I can't stop coughing.",
                    optionSymptoms = new string[] { "My stomach hurts.", "My head hurts! What's wrong with me?", "My throat is dry! I can't stop coughing.", "I twisted my ankle." },
                    correctOptionIndex = 2,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r01_round3_throat.mp3")
#endif
                },
                new DoctorReadingR01Round {
                    roundId = 4,
                    situationText = "A tooth has been aching since you ate something sweet.",
                    correctSymptom = "I have a toothache! I think I have a cavity.",
                    optionSymptoms = new string[] { "My chest feels tight! I can't breathe.", "My eyes are watery.", "My ears are itching!", "I have a toothache! I think I have a cavity." },
                    correctOptionIndex = 3,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r01_round4_tooth.mp3")
#endif
                },
                new DoctorReadingR01Round {
                    roundId = 5,
                    situationText = "Your head has been aching since you woke up.",
                    correctSymptom = "My head hurts! What's wrong with me?",
                    optionSymptoms = new string[] { "My head hurts! What's wrong with me?", "My nose is runny.", "My throat is dry! I can't stop coughing.", "My arm is hurt." },
                    correctOptionIndex = 0,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r01_round5_head.mp3")
#endif
                },
                new DoctorReadingR01Round {
                    roundId = 6,
                    situationText = "Your ears feel itchy and you keep rubbing them.",
                    correctSymptom = "My ears are itching!",
                    optionSymptoms = new string[] { "I cut my tongue.", "My ears are itching!", "My stomach hurts.", "My legs feel weak." },
                    correctOptionIndex = 1,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r01_round6_ears.mp3")
#endif
                },
                new DoctorReadingR01Round {
                    roundId = 7,
                    situationText = "You bit down while eating and it stings.",
                    correctSymptom = "I cut my tongue.",
                    optionSymptoms = new string[] { "I have a toothache! I think I have a cavity.", "My eyes are watery.", "I cut my tongue.", "My head hurts! What's wrong with me?" },
                    correctOptionIndex = 2,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r01_round7_tongue.mp3")
#endif
                },
                new DoctorReadingR01Round {
                    roundId = 8,
                    situationText = "You feel heavy in the chest and cannot take a full breath.",
                    correctSymptom = "My chest feels tight! I can't breathe.",
                    optionSymptoms = new string[] { "My throat is dry! I can't stop coughing.", "My nose is runny.", "I twisted my ankle.", "My chest feels tight! I can't breathe." },
                    correctOptionIndex = 3,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r01_round8_chest.mp3")
#endif
                },
                new DoctorReadingR01Round {
                    roundId = 9,
                    situationText = "Your tummy has been hurting since lunch.",
                    correctSymptom = "My stomach hurts.",
                    optionSymptoms = new string[] { "My stomach hurts.", "My ears are itching!", "My arm is hurt.", "My legs feel weak." },
                    correctOptionIndex = 0,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r01_round9_stomach.mp3")
#endif
                },
                new DoctorReadingR01Round {
                    roundId = 10,
                    situationText = "You fell during the match and your arm is sore.",
                    correctSymptom = "My arm is hurt.",
                    optionSymptoms = new string[] { "My stomach hurts.", "My arm is hurt.", "My nose is runny.", "I cut my tongue." },
                    correctOptionIndex = 1,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r01_round10_arm.mp3")
#endif
                },
                new DoctorReadingR01Round {
                    roundId = 11,
                    situationText = "You slipped on the step and your ankle turned over.",
                    correctSymptom = "I twisted my ankle.",
                    optionSymptoms = new string[] { "My head hurts! What's wrong with me?", "My eyes are watery.", "I twisted my ankle.", "My chest feels tight! I can't breathe." },
                    correctOptionIndex = 2,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r01_round11_ankle.mp3")
#endif
                },
                new DoctorReadingR01Round {
                    roundId = 12,
                    situationText = "Your legs feel wobbly after being ill for two days.",
                    correctSymptom = "My legs feel weak.",
                    optionSymptoms = new string[] { "My ears are itching!", "My throat is dry! I can't stop coughing.", "I have a toothache! I think I have a cavity.", "My legs feel weak." },
                    correctOptionIndex = 3,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_r01_round12_legs.mp3")
#endif
                }
            };
        }

        public void RestartLesson() {
            currentRoundIndex = 0;
            score = 0;
            isHandlingAnswer = false;

            if (headerTMP != null) headerTMP.gameObject.SetActive(true);
            if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);
            if (resultPanel != null) resultPanel.SetActive(false);
            if (noticeboardCardObject != null) noticeboardCardObject.SetActive(true);

            Transform grid = transform.Find("OptionsContainer") ?? transform.Find("OptionChipsGrid") ?? transform.Find("OptionsGrid");
            if (grid != null) grid.gameObject.SetActive(true);

            UpdateScoreUI();
            ShowRound(0);
        }

        public void ShowRound(int roundIdx) {
            if (rounds == null || rounds.Length == 0) return;
            currentRoundIndex = Mathf.Clamp(roundIdx, 0, rounds.Length - 1);
            DoctorReadingR01Round round = rounds[currentRoundIndex];

            if (progressTMP != null) progressTMP.text = $"{currentRoundIndex + 1}/{rounds.Length}";
            if (headerTMP != null) headerTMP.text = "NOTICEBOARD 📋";
            if (titleTMP != null) {
                titleTMP.text = "What Would You Tell the Doctor?";
                titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
            }
            if (subtitleTMP != null) subtitleTMP.text = "Read the situation and tap the symptom sentence that fits.";

            if (noticeboardHeaderTMP != null) noticeboardHeaderTMP.text = $"SITUATION #{currentRoundIndex + 1}";
            if (situationTextTMP != null) {
                situationTextTMP.text = $"\"{round.situationText}\"";
            }

            // Setup Options
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] == null) continue;
                optionButtons[i].interactable = true;
                optionButtons[i].transform.localScale = Vector3.one;

                if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                    optionImages[i].color = defaultChipColor;
                }

                if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) {
                    if (round.optionSymptoms != null && i < round.optionSymptoms.Length) {
                        optionTexts[i].text = round.optionSymptoms[i];
                    }
                }
            }
        }

        public void PlayCurrentAudio() {
            if (rounds == null || currentRoundIndex >= rounds.Length) return;
            AudioClip clip = rounds[currentRoundIndex].symptomAudio;
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
            if (rounds == null || currentRoundIndex >= rounds.Length) return;

            DoctorReadingR01Round round = rounds[currentRoundIndex];
            bool isCorrect = (optionIndex == round.correctOptionIndex);

            StartCoroutine(HandleAnswerCoroutine(optionIndex, isCorrect));
        }

        private IEnumerator HandleAnswerCoroutine(int optionIndex, bool isCorrect) {
            isHandlingAnswer = true;

            // Lock all options
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) optionButtons[i].interactable = false;
            }

            if (isCorrect) {
                score++;
                UpdateScoreUI();

                if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                    optionImages[optionIndex].color = correctChipColor;
                }

                if (optionButtons[optionIndex] != null) {
                    optionButtons[optionIndex].transform.DOScale(1.08f, 0.18f).SetLoops(2, LoopType.Yoyo);
                }

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

                // Show correct option in green
                int correctIdx = rounds[currentRoundIndex].correctOptionIndex;
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

            if (currentRoundIndex < rounds.Length - 1) {
                ShowRound(currentRoundIndex + 1);
            } else {
                ShowResults();
            }
        }

        private void UpdateScoreUI() {
            if (scoreTMP != null) {
                scoreTMP.text = $"Score: {score}/{rounds.Length}";
            }
        }

        private void ShowResults() {
            if (noticeboardCardObject != null) noticeboardCardObject.SetActive(false);
            
            Transform grid = transform.Find("OptionsContainer") ?? transform.Find("OptionChipsGrid") ?? transform.Find("OptionsGrid");
            if (grid != null) grid.gameObject.SetActive(false);

            if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);

            if (resultPanel == null) {
                Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
                if (t != null) resultPanel = t.gameObject;
            }

            bool passed = (score >= 10);

            if (resultPanel != null) {
                resultPanel.SetActive(true);
                resultPanel.transform.DOKill();
                resultPanel.transform.localScale = Vector3.zero;
                resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

                if (resultTitleTMP != null) {
                    resultTitleTMP.text = passed ? "EXCELLENT WORK! 📋" : "KEEP PRACTICING!";
                    resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
                }

                if (resultScoreTMP != null) {
                    resultScoreTMP.text = $"You matched {score} of {rounds.Length} situations correctly!";
                }

                if (resultStatusTMP != null) {
                    resultStatusTMP.text = passed ? "Success! You know exactly what to tell the doctor in each situation." : "You need at least 10/12 to complete this reading topic.";
                }

                if (returnHubBtn != null) {
                    returnHubBtn.gameObject.SetActive(passed);
                }

                if (retryBtn != null) {
                    retryBtn.gameObject.SetActive(!passed || score < rounds.Length);
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
            if (noticeboardCardObject == null) {
                Transform t = transform.Find("NoticeboardCard") ?? transform.Find("PromptCard") ?? transform.Find("RadioCard") ?? transform.Find("CenterCard");
                if (t != null) {
                    noticeboardCardObject = t.gameObject;
                    situationTextTMP = t.GetComponentInChildren<TextMeshProUGUI>();
                }
            }
            if (replayAudioBtn == null) {
                Transform t = transform.Find("ReplayAudioBtn") ?? transform.Find("NoticeboardCard/ReplayAudioBtn") ?? transform.Find("SpeakerBtn");
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

            // Find 4 option buttons
            if (optionButtons == null || optionButtons.Length == 0) {
                Transform grid = transform.Find("OptionsContainer") 
                              ?? transform.Find("OptionChipsGrid") 
                              ?? transform.Find("SelectionPanel") 
                              ?? transform.Find("OptionsGrid");
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
