using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit5 {


    /// <summary>
    /// L01 Hear It — Where Does It Hurt?
    /// Listening lesson controller for Book 2B Unit 5 (Doctor Need Your Help).
    /// Student listens to spoken symptom sentences and selects the corresponding body part region.
    /// Success condition: Student places at least 8 of 10 spoken symptoms on the correct body part.
    /// </summary>
    public class Masters_DoctorNeedYourHelp_Listening_LessonOne : Masters_Lesson {

[System.Serializable]
    public class DoctorListeningL01Round {
        public int roundId;
        public string symptomSentence;
        public string correctBodyPart;
        public string[] optionBodyParts;
        public int correctOptionIndex;
        public AudioClip symptomAudio;
    }
    
        [Header("10 Symptom Body Part Rounds")]
        [SerializeField] private DoctorListeningL01Round[] rounds;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;

        [Header("Radio Corner & Symptom Display")]
        [SerializeField] private GameObject symptomCardObject;
        [SerializeField] private Image symptomCardBg;
        [SerializeField] private TextMeshProUGUI symptomHeaderTMP;
        [SerializeField] private TextMeshProUGUI symptomTextTMP;
        [SerializeField] private Button replayAudioBtn;

        [Header("4 Body Part Option Chips")]
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
            topic = Masters_Topic.Listening;
            base.Awake();

            AutoBindReferences();
            InitRoundsIfEmpty();

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

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Listening;
            EnsureNextAndBackButtonWired();
            AutoBindReferences();

            if (rounds == null || rounds.Length == 0) {
                InitRoundsIfEmpty();
            }

            SetControlsInteractable(false);
            StartCoroutine(BeginLessonRoutine());
        }

        private IEnumerator BeginLessonRoutine() {
            isIntroPhase = true;
            if (progressTMP != null) progressTMP.text = "Story 1/10";
            if (scoreTMP != null) scoreTMP.text = "Score: 0/10";
            if (headerTMP != null) headerTMP.text = "RADIO CORNER 📻";
            if (titleTMP != null) {
                titleTMP.text = "Hear It — Where Does It Hurt?";
                titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
            }
            if (subtitleTMP != null) subtitleTMP.text = "Listen to the symptom and tap the part of the body it belongs to.";

            if (rounds != null && rounds.Length > 0) {
                if (symptomHeaderTMP != null) symptomHeaderTMP.text = "RADIO CORNER";
                if (symptomTextTMP != null) symptomTextTMP.text = rounds[0].symptomSentence;
                if (optionTexts != null && optionButtons != null) {
                    for (int i = 0; i < optionButtons.Length; i++) {
                        if (i < rounds[0].optionBodyParts.Length && optionTexts[i] != null) {
                            optionTexts[i].text = rounds[0].optionBodyParts[i];
                        }
                    }
                }
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
                foreach (var btn in optionButtons) {
                    if (btn != null) btn.interactable = interactable;
                }
            }
            if (replayAudioBtn != null) replayAudioBtn.interactable = interactable;
        }

        public void InitRoundsIfEmpty() {
            string audioDir = "Assets/Audio/2B/5_DoctorNeedYourHelp/Listening/";

            rounds = new DoctorListeningL01Round[] {
                new DoctorListeningL01Round {
                    roundId = 1,
                    symptomSentence = "\"My head hurts and I feel very dizzy today.\"",
                    correctBodyPart = "Head & Face",
                    optionBodyParts = new string[] { "Head & Face", "Chest & Body", "Arms & Hands", "Legs & Feet" },
                    correctOptionIndex = 0,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l01_round1_head.mp3")
#endif
                },
                new DoctorListeningL01Round {
                    roundId = 2,
                    symptomSentence = "\"My throat is very sore and it hurts when I swallow.\"",
                    correctBodyPart = "Throat & Neck",
                    optionBodyParts = new string[] { "Stomach", "Throat & Neck", "Legs & Feet", "Arms & Hands" },
                    correctOptionIndex = 1,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l01_round2_throat.mp3")
#endif
                },
                new DoctorListeningL01Round {
                    roundId = 3,
                    symptomSentence = "\"I have a sharp pain in my chest when I breathe deeply.\"",
                    correctBodyPart = "Chest & Breathing",
                    optionBodyParts = new string[] { "Head & Face", "Legs & Feet", "Chest & Breathing", "Arms & Hands" },
                    correctOptionIndex = 2,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l01_round3_chest.mp3")
#endif
                },
                new DoctorListeningL01Round {
                    roundId = 4,
                    symptomSentence = "\"My stomach hurts terribly after having lunch.\"",
                    correctBodyPart = "Stomach & Tummy",
                    optionBodyParts = new string[] { "Head & Face", "Arms & Hands", "Legs & Feet", "Stomach & Tummy" },
                    correctOptionIndex = 3,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l01_round4_stomach.mp3")
#endif
                },
                new DoctorListeningL01Round {
                    roundId = 5,
                    symptomSentence = "\"I hurt my left arm and wrist while playing basketball.\"",
                    correctBodyPart = "Arms & Hands",
                    optionBodyParts = new string[] { "Arms & Hands", "Legs & Feet", "Chest & Body", "Head & Face" },
                    correctOptionIndex = 0,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l01_round5_arm.mp3")
#endif
                },
                new DoctorListeningL01Round {
                    roundId = 6,
                    symptomSentence = "\"My right knee is swollen and painful to walk on.\"",
                    correctBodyPart = "Legs & Knee",
                    optionBodyParts = new string[] { "Arms & Hands", "Legs & Knee", "Head & Face", "Chest & Body" },
                    correctOptionIndex = 1,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l01_round6_knee.mp3")
#endif
                },
                new DoctorListeningL01Round {
                    roundId = 7,
                    symptomSentence = "\"There is a sharp pain inside my left ear since yesterday.\"",
                    correctBodyPart = "Ears & Hearing",
                    optionBodyParts = new string[] { "Stomach", "Chest & Body", "Ears & Hearing", "Legs & Feet" },
                    correctOptionIndex = 2,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l01_round7_ear.mp3")
#endif
                },
                new DoctorListeningL01Round {
                    roundId = 8,
                    symptomSentence = "\"I have a terrible toothache in my lower jaw.\"",
                    correctBodyPart = "Teeth & Mouth",
                    optionBodyParts = new string[] { "Legs & Feet", "Arms & Hands", "Chest & Body", "Teeth & Mouth" },
                    correctOptionIndex = 3,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l01_round8_teeth.mp3")
#endif
                },
                new DoctorListeningL01Round {
                    roundId = 9,
                    symptomSentence = "\"My lower back aches whenever I bend down to pick up books.\"",
                    correctBodyPart = "Back & Spine",
                    optionBodyParts = new string[] { "Back & Spine", "Head & Face", "Ears & Hearing", "Arms & Hands" },
                    correctOptionIndex = 0,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l01_round9_back.mp3")
#endif
                },
                new DoctorListeningL01Round {
                    roundId = 10,
                    symptomSentence = "\"I twisted my ankle and now my foot hurts to stand on.\"",
                    correctBodyPart = "Foot & Ankle",
                    optionBodyParts = new string[] { "Head & Face", "Foot & Ankle", "Chest & Body", "Stomach" },
                    correctOptionIndex = 1,
#if UNITY_EDITOR
                    symptomAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "dnyh_l01_round10_ankle.mp3")
#endif
                }
            };
        }

        public void RestartLesson() {
            currentRoundIndex = 0;
            score = 0;
            isHandlingAnswer = false;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (symptomCardObject != null) symptomCardObject.SetActive(true);

            UpdateScoreUI();
            ShowRound(0);
        }

        public void ShowRound(int roundIdx) {
            if (rounds == null || rounds.Length == 0) return;
            currentRoundIndex = Mathf.Clamp(roundIdx, 0, rounds.Length - 1);
            DoctorListeningL01Round round = rounds[currentRoundIndex];

            if (progressTMP != null) progressTMP.text = $"Story {currentRoundIndex + 1}/{rounds.Length}";
            if (headerTMP != null) headerTMP.text = "RADIO CORNER 📻";
            if (titleTMP != null) {
                titleTMP.text = "Hear It — Where Does It Hurt?";
                titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
            }
            if (subtitleTMP != null) subtitleTMP.text = "Listen to the symptom and tap the part of the body it belongs to.";

            if (symptomHeaderTMP != null) symptomHeaderTMP.text = "RADIO CORNER";
            if (symptomTextTMP != null) symptomTextTMP.text = round.symptomSentence;

            // Setup Options
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] == null) continue;
                optionButtons[i].interactable = true;
                optionButtons[i].transform.localScale = Vector3.one;

                if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                    optionImages[i].color = defaultChipColor;
                }

                if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) {
                    if (round.optionBodyParts != null && i < round.optionBodyParts.Length) {
                        optionTexts[i].text = round.optionBodyParts[i];
                    }
                }
            }

            SetControlsInteractable(true);
            PlayCurrentAudio();
        }

        public void PlayCurrentAudio() {
            if (rounds == null || currentRoundIndex >= rounds.Length) return;
            AudioClip clip = rounds[currentRoundIndex].symptomAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }

        public void ReplayCurrentAudio() {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            PlayCurrentAudio();
        }

        public void OnOptionClicked(int optionIndex) {
            if (isHandlingAnswer || isIntroPhase) return;
            if (rounds == null || currentRoundIndex >= rounds.Length) return;

            DoctorListeningL01Round round = rounds[currentRoundIndex];
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
                    optionButtons[optionIndex].transform.DOScale(1.1f, 0.2f).SetLoops(2, LoopType.Yoyo);
                }

                if (sfxCorrect != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(sfxCorrect);
                } else if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
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

                if (sfxWrong != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(sfxWrong);
                } else if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
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
            if (symptomCardObject != null) symptomCardObject.SetActive(false);
            if (resultPanel != null) {
                resultPanel.SetActive(true);
                bool passed = (score >= 8);

                if (resultTitleTMP != null) {
                    resultTitleTMP.text = passed ? "GREAT JOB! 🩺" : "KEEP PRACTICING!";
                    resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
                }

                if (resultScoreTMP != null) {
                    resultScoreTMP.text = $"You identified {score} of {rounds.Length} body parts correctly!";
                }

                if (resultStatusTMP != null) {
                    resultStatusTMP.text = passed ? "Success! You passed the symptom listening challenge." : "You need at least 8/10 to complete this topic.";
                }

                if (returnHubBtn != null) {
                    returnHubBtn.gameObject.SetActive(passed);
                }

                if (retryBtn != null) {
                    retryBtn.gameObject.SetActive(!passed || score < rounds.Length);
                }

                if (passed && recapAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.StopVoiceOver();
                    Masters_AudioManager.Instance.PlayVoiceOver(recapAudio);
                } else if (passed && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                }
            }
        }

        protected override void OnNextButtonClicked() {
            topic = Masters_Topic.Listening;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }

        private void AutoBindReferences() {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps) {
                string n = tmp.gameObject.name.ToLower();
                if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
                else if (progressTMP == null && n.Contains("progress")) progressTMP = tmp;
                else if (scoreTMP == null && n.Contains("score")) scoreTMP = tmp;
                else if (headerTMP == null && (n.Contains("branch") || n.Contains("header"))) headerTMP = tmp;
                else if (subtitleTMP == null && n.Contains("subtitle")) subtitleTMP = tmp;
                else if (symptomHeaderTMP == null && (n.Contains("radioheadertmp") || n.Contains("symptomheader"))) symptomHeaderTMP = tmp;
                else if (symptomTextTMP == null && (n.Contains("storytexttmp") || n.Contains("symptomtext"))) symptomTextTMP = tmp;
            }

            if (symptomCardObject == null) {
                Transform t = transform.Find("RadioCard") ?? transform.Find("SymptomCard") ?? transform.Find("CenterCard");
                if (t != null) symptomCardObject = t.gameObject;
            }

            if (replayAudioBtn == null) {
                Transform t = transform.Find("RadioCard/ReplayAudioBtn") ?? transform.Find("ReplayAudioBtn") ?? transform.Find("SpeakerBtn") ?? transform.Find("AudioBtn");
                if (t != null) replayAudioBtn = t.GetComponent<Button>();
            }

            if (resultPanel == null) {
                Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
                if (t != null) resultPanel = t.gameObject;
            }

            if (resultPanel != null) {
                if (retryBtn == null) {
                    Transform t = resultPanel.transform.Find("RetryButton") ?? resultPanel.transform.Find("RetryBtn");
                    if (t != null) retryBtn = t.GetComponent<Button>();
                }
                if (returnHubBtn == null) {
                    Transform t = resultPanel.transform.Find("ReturnHubButton") ?? resultPanel.transform.Find("ReturnHubBtn") ?? resultPanel.transform.Find("NextBtn");
                    if (t != null) returnHubBtn = t.GetComponent<Button>();
                }
                if (resultTitleTMP == null) {
                    Transform t = resultPanel.transform.Find("ResultTitle");
                    if (t != null) resultTitleTMP = t.GetComponent<TextMeshProUGUI>();
                }
                if (resultScoreTMP == null) {
                    Transform t = resultPanel.transform.Find("ResultScore");
                    if (t != null) resultScoreTMP = t.GetComponent<TextMeshProUGUI>();
                }
                if (resultStatusTMP == null) {
                    Transform t = resultPanel.transform.Find("ResultStatus");
                    if (t != null) resultStatusTMP = t.GetComponent<TextMeshProUGUI>();
                }
            }

            // Find 4 options
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
                            txts.Add(b.GetComponentInChildren<TextMeshProUGUI>(true));
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
