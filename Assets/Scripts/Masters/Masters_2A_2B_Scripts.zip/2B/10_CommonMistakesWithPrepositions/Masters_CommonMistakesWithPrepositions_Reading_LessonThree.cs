using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit10 {

    

    /// <summary>
    /// Unit 10: Common Mistakes with Prepositions — Reading Lesson Three
    /// (R03 Preposition Fitting)
    /// 12 Rounds across pp.42-45 with 5 option chips: OF, IN, ON, TO, NOTHING NEEDED.
    /// Student fits the correct preposition into the sentence slot.
    /// Pass mark: 10 of 12 (or 8 of 10).
    /// </summary>
    public class Masters_CommonMistakesWithPrepositions_Reading_LessonThree : Masters_Lesson {

[System.Serializable]
    public class CommonMistakes_ReadingR03RoundData {
        public string sentenceWithBlank;
        public string correctPreposition; // "OF", "IN", "ON", "TO", "NOTHING NEEDED"
        public string completedSentence;
        public AudioClip readoutAudio;
    }
    
        [Header("12 Preposition Fitting Rounds")]
        [SerializeField] private CommonMistakes_ReadingR03RoundData[] rounds;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI scoreTMP;
        [SerializeField] private TextMeshProUGUI sentenceTMP;
        [SerializeField] private TextMeshProUGUI speechBubbleTMP;

        [Header("5 Preposition Option Buttons (OF, IN, ON, TO, NOTHING NEEDED)")]
        [SerializeField] private Button[] optionButtons;

        [Header("Audio Controls")]
        [SerializeField] private Button replayAudioBtn;
        [SerializeField] private Toggle repeatThisToggle;
        [SerializeField] private Toggle slowToggle;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;

        [Header("Colors & Visuals")]
        [SerializeField] private Color defaultChipColor = new Color(0.14f, 0.38f, 0.58f, 1f);
        [SerializeField] private Color correctColor = new Color(0.14f, 0.65f, 0.28f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.85f, 0.22f, 0.22f, 1f);

        private readonly string[] standardOptions = new string[] { "OF", "IN", "ON", "TO", "NOTHING NEEDED" };

        private int currentRoundIndex = 0;
        private int score = 0;
        private bool isProcessingInput = false;
        private bool currentRoundHasRetried = false;
        private bool isSlowMode = false;
        private bool isRepeatMode = false;

        private Image[] optionImages;
        private TextMeshProUGUI[] optionTMPs;

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Reading;

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/10_CommonMistakesWithPrepositions/Reading/cm_r03_full_intro.mp3");
#endif
            }

            PurgeLegacyChildren();
            AutoBindReferences();
            EnsureHeaderAndTitle();
            EnsureNextAndBackButtonWired();

            if (rounds == null || rounds.Length == 0 || rounds[0] == null || rounds[0].readoutAudio == null) {
                PopulateDefaultRounds();
            }

            InitOptionButtons();
            WireEventListeners();

            if (resultPanel != null) resultPanel.SetActive(false);
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Reading;
            EnsureNextAndBackButtonWired();
            EnsureAspectRatiosAndAnchorsPreserved();
            EnsureHeaderAndTitle();

            currentRoundIndex = 0;
            score = 0;
            isProcessingInput = false;

            if (nextButton != null) {
                nextButton.gameObject.SetActive(false);
            }

            StartCoroutine(InitializeLessonRoutine());
        }

        private IEnumerator InitializeLessonRoutine() {
            yield return new WaitForSeconds(0.3f);
            DisplayRound(0);
        }

        private void EnsureHeaderAndTitle() {
            if (headerTMP == null) {
                Transform hTrans = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("UnitHeading/UnitTMP") ?? transform.Find("UnitHeading") ?? transform.Find("Branch");
                if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
            }
            if (headerTMP != null) {
                headerTMP.text = "COMMON MISTAKES WITH PREPOSITIONS 🔍";
            }

            if (titleTMP == null) {
                Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle");
                if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
            }
            if (titleTMP != null) {
                titleTMP.text = "R03 Preposition Fitting";
                titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
                titleTMP.enableAutoSizing = false;
                titleTMP.fontSize = 38f;
                titleTMP.fontStyle = FontStyles.Bold;
                titleTMP.alignment = TextAlignmentOptions.Center;
            }
        }

        private void PurgeLegacyChildren() {
            string[] legacyNames = new string[] {
                "Cloud", "Cloud (1)", "Cloud (2)", "Cloud (3)", "Cloud (4)", "Cloud (5)",
                "SpawnArea", "GamePlayGameObject"
            };

            foreach (string lName in legacyNames) {
                Transform lTrans = transform.Find(lName);
                if (lTrans != null) {
                    lTrans.gameObject.SetActive(false);
                    if (Application.isPlaying) Destroy(lTrans.gameObject);
                    else DestroyImmediate(lTrans.gameObject);
                }
            }
        }

        private void AutoBindReferences() {
            // Find header & title
            if (headerTMP == null) {
                Transform hTr = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("UnitHeading/UnitTMP") ?? transform.Find("Branch");
                if (hTr != null) headerTMP = hTr.GetComponent<TextMeshProUGUI>();
            }

            if (titleTMP == null) {
                Transform tTr = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle");
                if (tTr != null) titleTMP = tTr.GetComponent<TextMeshProUGUI>();
            }

            // Find sentence text (e.g. WindowSignTMP or SentenceTMP)
            if (sentenceTMP == null) {
                Transform sTr = transform.Find("WindowSignContainer/WindowSignTMP")
                             ?? transform.Find("WindowSignTMP")
                             ?? transform.Find("SentenceContainer/SentenceTMP") 
                             ?? transform.Find("SentenceTMP") 
                             ?? transform.Find("PromptTMP") 
                             ?? transform.Find("PromptSentenceTMP")
                             ?? transform.Find("QuestionPanel/QuestionTMP");
                if (sTr != null) sentenceTMP = sTr.GetComponent<TextMeshProUGUI>();
            }

            // Find speech bubble text
            if (speechBubbleTMP == null) {
                Transform spTr = transform.Find("SpeechBubble/Text") 
                              ?? transform.Find("SpeechBubbleTMP") 
                              ?? transform.Find("FeedbackTMP");
                if (spTr != null) speechBubbleTMP = spTr.GetComponent<TextMeshProUGUI>();
            }

            // Find progress / score TMP
            if (progressTMP == null) {
                Transform pTr = transform.Find("HeaderContainer/ProgressTMP") 
                             ?? transform.Find("ProgressCountTMP") 
                             ?? transform.Find("ExpressionCountTMP") 
                             ?? transform.Find("ProgressTMP");
                if (pTr != null) {
                    progressTMP = pTr.GetComponent<TextMeshProUGUI>();
                } else {
                    TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var t in tmps) {
                        string tn = t.name.ToLower();
                        if (tn.Contains("progress") || tn.Contains("expressioncount")) { progressTMP = t; break; }
                    }
                }
            }

            if (scoreTMP == null) {
                Transform scTr = transform.Find("HeaderContainer/ScoreTMP") 
                              ?? transform.Find("ScoreTMP") 
                              ?? transform.Find("CorrectlyFilledTMP");
                if (scTr != null) {
                    scoreTMP = scTr.GetComponent<TextMeshProUGUI>();
                } else {
                    TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                    foreach (var t in tmps) {
                        string tn = t.name.ToLower();
                        if ((tn.Contains("score") || tn.Contains("correctlyfilled")) && !tn.Contains("result")) { scoreTMP = t; break; }
                    }
                }
            }

            // Find option buttons
            if (optionButtons == null || optionButtons.Length == 0) {
                Transform optContainer = transform.Find("OptionButtonsContainer")
                                      ?? transform.Find("OptionsContainer") 
                                      ?? transform.Find("OptionButtons") 
                                      ?? transform.Find("ButtonsContainer")
                                      ?? transform.Find("ChipsContainer");
                if (optContainer != null) {
                    Button[] btns = optContainer.GetComponentsInChildren<Button>(true);
                    if (btns != null && btns.Length > 0) {
                        optionButtons = btns;
                    }
                }
            }

            if (optionButtons == null || optionButtons.Length == 0) {
                List<Button> bList = new List<Button>();
                for (int i = 1; i <= 5; i++) {
                    Transform bTr = transform.Find($"OptionButton_0{i}") ?? transform.Find($"Option_{i}") ?? transform.Find($"Button_{i}");
                    if (bTr != null) {
                        Button b = bTr.GetComponent<Button>();
                        if (b != null) bList.Add(b);
                    }
                }
                if (bList.Count > 0) optionButtons = bList.ToArray();
            }

            // Find Audio controls
            Transform controlsTr = transform.Find("Controls") ?? transform.Find("AudioControls");
            if (controlsTr != null) {
                Button[] cBtns = controlsTr.GetComponentsInChildren<Button>(true);
                foreach (var b in cBtns) {
                    string bn = b.name.ToLower();
                    if (bn.Contains("repeat") || bn.Contains("replay") || bn.Contains("audio") || bn.Contains("sound")) replayAudioBtn = b;
                }
                Toggle[] cTogs = controlsTr.GetComponentsInChildren<Toggle>(true);
                foreach (var t in cTogs) {
                    string tn = t.name.ToLower();
                    if (tn.Contains("repeat") || tn.Contains("replay")) repeatThisToggle = t;
                    if (tn.Contains("slow")) slowToggle = t;
                }
            }

            // Find Result panel
            if (resultPanel == null) {
                Transform rp = transform.Find("ResultPanel") ?? transform.Find("CompletedPanel") ?? transform.Find("ResultsPanel");
                if (rp != null) resultPanel = rp.gameObject;
            }
            if (resultPanel != null) {
                Transform rSc = resultPanel.transform.Find("ScoreTMP") ?? resultPanel.transform.Find("ResultScoreTMP");
                if (rSc != null) resultScoreTMP = rSc.GetComponent<TextMeshProUGUI>();

                Transform rSt = resultPanel.transform.Find("StatusTMP") ?? resultPanel.transform.Find("ResultStatusTMP");
                if (rSt != null) resultStatusTMP = rSt.GetComponent<TextMeshProUGUI>();

                Transform rBtn = resultPanel.transform.Find("RetryButton") ?? resultPanel.transform.Find("RetryBtn");
                if (rBtn != null) retryBtn = rBtn.GetComponent<Button>();
            }
        }

        private void InitOptionButtons() {
            if (optionButtons == null || optionButtons.Length == 0) return;

            optionImages = new Image[optionButtons.Length];
            optionTMPs = new TextMeshProUGUI[optionButtons.Length];

            for (int i = 0; i < optionButtons.Length; i++) {
                int index = i;
                Button btn = optionButtons[i];
                if (btn != null) {
                    optionImages[i] = btn.GetComponent<Image>();
                    optionTMPs[i] = btn.GetComponentInChildren<TextMeshProUGUI>(true);

                    // Set label if in standard options
                    if (i < standardOptions.Length && optionTMPs[i] != null) {
                        optionTMPs[i].text = standardOptions[i];
                    }

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnOptionButtonClicked(index));
                }
            }
        }

        private void DisplayRound(int roundIdx) {
            if (rounds == null || roundIdx < 0 || roundIdx >= rounds.Length) return;

            currentRoundIndex = roundIdx;
            currentRoundHasRetried = false;
            isProcessingInput = false;

            CommonMistakes_ReadingR03RoundData r = rounds[roundIdx];

            if (sentenceTMP != null) {
                sentenceTMP.text = r.sentenceWithBlank;
            }

            if (speechBubbleTMP != null) {
                speechBubbleTMP.text = "Fit the correct preposition into the empty slot!";
            }

            if (progressTMP != null) {
                progressTMP.text = $"Sentence {roundIdx + 1}/{rounds.Length}";
            }
            if (scoreTMP != null) {
                scoreTMP.text = $"Score: {score}/{rounds.Length}";
            }

            // Reset option button colors and interactability
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    optionButtons[i].interactable = true;
                    if (optionImages[i] != null) optionImages[i].color = defaultChipColor;
                    if (i < standardOptions.Length && optionTMPs[i] != null) {
                        optionTMPs[i].text = standardOptions[i];
                    }
                }
            }
        }

        private void OnOptionButtonClicked(int optionIdx) {
            if (isProcessingInput) return;
            if (currentRoundIndex < 0 || currentRoundIndex >= rounds.Length) return;

            string selectedOption = (optionIdx < standardOptions.Length) ? standardOptions[optionIdx] : "";
            if (optionTMPs != null && optionIdx < optionTMPs.Length && optionTMPs[optionIdx] != null) {
                selectedOption = optionTMPs[optionIdx].text.Trim().ToUpper();
            }

            CommonMistakes_ReadingR03RoundData r = rounds[currentRoundIndex];
            string correctOption = r.correctPreposition.Trim().ToUpper();

            bool isCorrect = selectedOption.Equals(correctOption, System.StringComparison.OrdinalIgnoreCase);

            StartCoroutine(HandleOptionAnswerRoutine(isCorrect, optionIdx, r));
        }

        private IEnumerator HandleOptionAnswerRoutine(bool isCorrect, int optionIdx, CommonMistakes_ReadingR03RoundData r) {
            isProcessingInput = true;

            Button btn = (optionIdx >= 0 && optionIdx < optionButtons.Length) ? optionButtons[optionIdx] : null;
            Image img = (optionIdx >= 0 && optionIdx < optionImages.Length) ? optionImages[optionIdx] : null;

            if (isCorrect) {
                if (img != null) img.color = correctColor;
                if (btn != null) btn.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 6, 0.5f);

                if (!currentRoundHasRetried) score++;
                if (scoreTMP != null) scoreTMP.text = $"Score: {score}/{rounds.Length}";

                if (sentenceTMP != null) {
                    sentenceTMP.text = r.completedSentence;
                }

                if (speechBubbleTMP != null) {
                    speechBubbleTMP.text = $"✅ \"{r.completedSentence}\"";
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                if (r.readoutAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(r.readoutAudio);
                    yield return new WaitForSeconds(r.readoutAudio.length + 0.3f);
                } else {
                    yield return new WaitForSeconds(0.8f);
                }

                // Advance to next round or finish
                if (currentRoundIndex + 1 < rounds.Length) {
                    DisplayRound(currentRoundIndex + 1);
                } else {
                    ShowFinalResults();
                }
            } else {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                if (img != null) {
                    img.DOColor(wrongColor, 0.2f).OnComplete(() => {
                        if (img != null) img.DOColor(defaultChipColor, 0.3f);
                    });
                }
                if (btn != null) {
                    btn.transform.DOShakePosition(0.35f, new Vector3(8f, 0f, 0f), 10, 90f);
                }

                currentRoundHasRetried = true;

                if (speechBubbleTMP != null) {
                    speechBubbleTMP.text = "⚠️ Not quite! Try fitting another option.";
                }

                yield return new WaitForSeconds(0.8f);
                isProcessingInput = false;
            }
        }

        private void ShowFinalResults() {
            if (resultPanel != null) {
                resultPanel.SetActive(true);
            }

            int totalRounds = (rounds != null) ? rounds.Length : 12;
            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Score: {score} / {totalRounds}";
            }

            bool passed = score >= (totalRounds >= 12 ? 10 : 8);
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "Great Job! Unit 10 Reading R03 Completed!" : "Keep practicing! Try again!";
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(passed ? Masters_SFX.Correct : Masters_SFX.Incorrect);
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                NextButtonAnimation();
            }

            if (passed) {
                topic = Masters_Topic.Reading;
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnLessonComplete(topic);
                }
            }
        }

        public void OnReplayAudioClicked() {
            if (rounds != null && currentRoundIndex >= 0 && currentRoundIndex < rounds.Length) {
                var r = rounds[currentRoundIndex];
                if (r.readoutAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(r.readoutAudio);
                }
            }
        }

        public void RestartLesson() {
            currentRoundIndex = 0;
            score = 0;
            isProcessingInput = false;

            if (resultPanel != null) resultPanel.SetActive(false);

            DisplayRound(0);
        }

        private void WireEventListeners() {
            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
            }

            if (repeatThisToggle != null) {
                Button togBtn = repeatThisToggle.GetComponent<Button>();
                if (togBtn != null) {
                    togBtn.onClick.RemoveAllListeners();
                    togBtn.onClick.AddListener(OnReplayAudioClicked);
                }

                repeatThisToggle.onValueChanged.RemoveAllListeners();
                repeatThisToggle.onValueChanged.AddListener((isOn) => {
                    isRepeatMode = isOn;
                    OnReplayAudioClicked();
                });
            }

            if (slowToggle != null) {
                slowToggle.onValueChanged.RemoveAllListeners();
                slowToggle.onValueChanged.AddListener((isOn) => {
                    isSlowMode = isOn;
                    OnReplayAudioClicked();
                });
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RestartLesson);
            }
        }

        public void PopulateDefaultRounds() {
            string audioDir = "Assets/Audio/2B/10_CommonMistakesWithPrepositions/Reading/";

            rounds = new CommonMistakes_ReadingR03RoundData[] {
                new CommonMistakes_ReadingR03RoundData {
                    sentenceWithBlank = "The dessert consisted ______ fruit and cream.",
                    correctPreposition = "OF",
                    completedSentence = "The dessert consisted of fruit and cream.",
#if UNITY_EDITOR
                    readoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r03_r01.mp3")
#endif
                },
                new CommonMistakes_ReadingR03RoundData {
                    sentenceWithBlank = "There are flowers ______ the picture.",
                    correctPreposition = "IN",
                    completedSentence = "There are flowers in the picture.",
#if UNITY_EDITOR
                    readoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r03_r02.mp3")
#endif
                },
                new CommonMistakes_ReadingR03RoundData {
                    sentenceWithBlank = "It depends ______ you.",
                    correctPreposition = "ON",
                    completedSentence = "It depends on you.",
#if UNITY_EDITOR
                    readoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r03_r03.mp3")
#endif
                },
                new CommonMistakes_ReadingR03RoundData {
                    sentenceWithBlank = "Who is ______ the phone?",
                    correctPreposition = "ON",
                    completedSentence = "Who is on the phone?",
#if UNITY_EDITOR
                    readoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r03_r04.mp3")
#endif
                },
                new CommonMistakes_ReadingR03RoundData {
                    sentenceWithBlank = "We went ______ the mall.",
                    correctPreposition = "TO",
                    completedSentence = "We went to the mall.",
#if UNITY_EDITOR
                    readoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r03_r05.mp3")
#endif
                },
                new CommonMistakes_ReadingR03RoundData {
                    sentenceWithBlank = "I am going ______ home.",
                    correctPreposition = "NOTHING NEEDED",
                    completedSentence = "I am going home.",
#if UNITY_EDITOR
                    readoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r03_r06.mp3")
#endif
                },
                new CommonMistakes_ReadingR03RoundData {
                    sentenceWithBlank = "Where is my phone ______?",
                    correctPreposition = "NOTHING NEEDED",
                    completedSentence = "Where is my phone?",
#if UNITY_EDITOR
                    readoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r03_r07.mp3")
#endif
                },
                new CommonMistakes_ReadingR03RoundData {
                    sentenceWithBlank = "Have you been ______ London?",
                    correctPreposition = "TO",
                    completedSentence = "Have you been to London?",
#if UNITY_EDITOR
                    readoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r03_r08.mp3")
#endif
                },
                new CommonMistakes_ReadingR03RoundData {
                    sentenceWithBlank = "He spoke ______ my behalf.",
                    correctPreposition = "ON",
                    completedSentence = "He spoke on my behalf.",
#if UNITY_EDITOR
                    readoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r03_r09.mp3")
#endif
                },
                new CommonMistakes_ReadingR03RoundData {
                    sentenceWithBlank = "The office is ______ the first floor.",
                    correctPreposition = "ON",
                    completedSentence = "The office is on the first floor.",
#if UNITY_EDITOR
                    readoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r03_r10.mp3")
#endif
                },
                new CommonMistakes_ReadingR03RoundData {
                    sentenceWithBlank = "My birthday is ______ January.",
                    correctPreposition = "IN",
                    completedSentence = "My birthday is in January.",
#if UNITY_EDITOR
                    readoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r03_r11.mp3")
#endif
                },
                new CommonMistakes_ReadingR03RoundData {
                    sentenceWithBlank = "I'm going to ______ bed.",
                    correctPreposition = "NOTHING NEEDED",
                    completedSentence = "I'm going to bed.",
#if UNITY_EDITOR
                    readoutAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r03_r12.mp3")
#endif
                }
            };
        }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
}
