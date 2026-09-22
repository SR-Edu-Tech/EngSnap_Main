using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit11 {

    /// <summary>
    /// Unit 11: Art of Politeness — Reading Lesson One
    /// (R01 Which One Would You Say Here?)
    /// Student reads the situation card and taps the sentence that best fits the place and the person.
    /// 10 rounds rotating the pairs across different social settings (formal, casual, family).
    /// Pass threshold: 8 / 10 situations.
    /// </summary>
    public class Masters_ArtOfPoliteness_Reading_LessonOne : Masters_Lesson {

[System.Serializable]
    public class ArtOfPoliteness_ReadingR01RoundData {
        public string situationText;            // Scenario description (e.g. "You are ordering at a restaurant with your family.")
        public string[] options;                // Options to choose from (2 options)
        public int correctOptionIndex;          // Index of better choice (0 or 1)
        public bool isEitherAllowed;            // For rounds 9 & 10 where both choices are acceptable
        public string explanationText;          // ARIA explanation of why it fits
        public AudioClip situationAudio;        // Voiceover of the situation
        public AudioClip feedbackAudio;         // Voiceover of the explanation
    }
    
        [Header("10 Situation-Based Reading Rounds")]
        [SerializeField] private ArtOfPoliteness_ReadingR01RoundData[] rounds;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI situationTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;

        [Header("Option Buttons (2-3 Choice Chips)")]
        [SerializeField] private Button[] optionButtons;
        [SerializeField] private TextMeshProUGUI[] optionTexts;
        [SerializeField] private Image[] optionImages;
        [SerializeField] private Button[] extraButtons;

        [Header("Audio Controls")]
        [SerializeField] private Button replayAudioBtn;
        [SerializeField] private Toggle repeatThisToggle;
        [SerializeField] private Toggle slowToggle;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;

        [Header("Colors (Preserved from Scene)")]
        [SerializeField] private Color defaultOptionColor = Color.white;
        [SerializeField] private Color correctColor = new Color(0.2f, 0.8f, 0.35f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.9f, 0.25f, 0.25f, 1f);

        private int currentRoundIndex = 0;
        private int score = 0;
        private bool isProcessingInput = false;

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Reading;

            PurgeSlideAnimations();
            PurgeLegacyChildren();

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/11_ArtOfPoliteness/Reading/artofpoliteness_r01_full_intro.mp3");
#endif
            }

            AutoBindReferences();

            if (optionImages != null && optionImages.Length > 0 && optionImages[0] != null) {
                defaultOptionColor = optionImages[0].color;
            }

            EnsureHeaderAndTitle();
            EnsureNextAndBackButtonWired();

            if (rounds == null || rounds.Length == 0) {
                PopulateDefaultRounds();
            }

            InitOptionButtons();
            WireEventListeners();
        }

        protected override void Start() {
            topic = Masters_Topic.Reading;

            PurgeSlideAnimations();
            EnsureHeaderAndTitle();
            ResetGameState();
            StartIntroAndGame();
        }

        private void PurgeSlideAnimations() {
            var slideAnims = GetComponentsInChildren<Masters_SlideAnimation>(true);
            foreach (var sa in slideAnims) {
                if (sa != null) {
                    sa.enabled = false;
                    if (Application.isPlaying) Destroy(sa);
                    else DestroyImmediate(sa);
                }
            }

            // Also disable any FillInTheBlank container if leftover from old template
            Transform fitb = transform.Find("SequenceYourThoughts_Reading_FillInTheBlank") ?? transform.Find("FillInTheBlank");
            if (fitb != null) {
                fitb.gameObject.SetActive(false);
            }
        }

        private void EnsureHeaderAndTitle() {
            AutoBindReferences();

            if (headerTMP == null) {
                Transform hTrans = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("UnitHeading/UnitTMP") ?? transform.Find("UnitHeading");
                if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
            }
            if (headerTMP != null) {
                headerTMP.text = "THE ART OF POLITENESS 🎩";
            }

            if (titleTMP == null) {
                Transform tTrans = transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle") ?? transform.Find("HeaderContainer/Title") ?? transform.Find("Title");
                if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
            }
            if (titleTMP != null) {
                titleTMP.text = "R01 Which One Would You Say Here?";
            }

            ResetOptionVisuals();
        }

        private void PurgeLegacyChildren() {
            string[] legacyNames = new string[] {
                "Cloud", "Cloud (1)", "Cloud (2)", "Cloud (3)", "Cloud (4)", "Cloud (5)", "Cloud (6)", "Cloud (7)",
                "cloud grid", "Objects scroll view",
                "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
                "SequenceYourThoughts_Reading_FillInTheBlank", "FillInTheBlank"
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
            List<Button> btnList = new List<Button>();
            List<TextMeshProUGUI> txtList = new List<TextMeshProUGUI>();
            List<Image> imgList = new List<Image>();
            List<Button> extraList = new List<Button>();

            // 1. Look for OptionButton chips (OptionButton, OptionButton (1), OptionButton (2), OptionButton (3))
            string[] optionNames = new string[] { "OptionButton", "OptionButton (1)", "OptionButton (2)", "OptionButton (3)", "Option 1", "Option 2", "Option 3", "Bin 1", "Bin 2", "Bin 3", "Button 1", "Button 2", "Button 3" };
            for (int i = 0; i < optionNames.Length; i++) {
                Transform tr = transform.Find(optionNames[i]);
                if (tr != null) {
                    Button b = tr.GetComponentInChildren<Button>(true) ?? tr.GetComponent<Button>();
                    TextMeshProUGUI tmp = tr.GetComponentInChildren<TextMeshProUGUI>(true);
                    Image img = tr.GetComponent<Image>() ?? tr.GetComponentInChildren<Image>(true);

                    if (b != null && !btnList.Contains(b)) {
                        btnList.Add(b);
                        if (tmp != null) txtList.Add(tmp);
                        if (img != null) imgList.Add(img);
                    }
                }
            }

            // Fallback to all button children with "option"
            if (btnList.Count == 0) {
                Button[] allBtns = GetComponentsInChildren<Button>(true);
                foreach (var b in allBtns) {
                    if (b != nextButton && b != retryBtn && b != replayAudioBtn && !b.name.ToLower().Contains("back") && !b.name.ToLower().Contains("speaker")) {
                        btnList.Add(b);
                        txtList.Add(b.GetComponentInChildren<TextMeshProUGUI>(true));
                        imgList.Add(b.GetComponent<Image>() ?? b.GetComponentInChildren<Image>(true));
                    }
                }
            }

            if (btnList.Count > 0) {
                optionButtons = btnList.ToArray();
                optionTexts = txtList.ToArray();
                optionImages = imgList.ToArray();
            }

            // Bind Progress TMP
            if (progressTMP == null) {
                Transform pTrans = transform.Find("PuzzleCountTMP") ?? transform.Find("ExpressionCountTMP") ?? transform.Find("Progress") ?? transform.Find("Counter");
                if (pTrans != null) progressTMP = pTrans.GetComponent<TextMeshProUGUI>();
            }

            // Bind Situation / Prompt TMP
            if (situationTMP == null) {
                Transform sTrans = transform.Find("sentence/TMP") ?? transform.Find("sentence/Text (TMP)") ?? transform.Find("sentence") ?? transform.Find("SituationCard/Text (TMP)") ?? transform.Find("PhraseCard/Text (TMP)") ?? transform.Find("Prompt");
                if (sTrans != null) situationTMP = sTrans.GetComponent<TextMeshProUGUI>() ?? sTrans.GetComponentInChildren<TextMeshProUGUI>(true);
            }

            // Bind Title and Header
            if (titleTMP == null) {
                Transform tTrans = transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle") ?? transform.Find("HeaderContainer/Title") ?? transform.Find("Title");
                if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
            }

            if (headerTMP == null) {
                Transform hTrans = transform.Find("UnitHeading/UnitTMP") ?? transform.Find("UnitHeading") ?? transform.Find("HeaderContainer/Header") ?? transform.Find("Header");
                if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
            }

            if (feedbackTMP == null) {
                Transform fTrans = transform.Find("FeedbackTMP") ?? transform.Find("ExplanationTMP") ?? transform.Find("StatusTMP");
                if (fTrans != null) feedbackTMP = fTrans.GetComponent<TextMeshProUGUI>();
            }

            // Bind Speaker Replay Button
            if (replayAudioBtn == null) {
                Transform spkTr = transform.Find("sentence/SpeakerIcon") ?? transform.Find("SpeakerIcon") ?? transform.Find("SituationCard/SpeakerImage") ?? transform.Find("PhraseCard/SpeakerImage");
                if (spkTr != null) {
                    replayAudioBtn = spkTr.GetComponent<Button>() ?? spkTr.GetComponentInChildren<Button>(true);
                    if (replayAudioBtn == null) {
                        replayAudioBtn = spkTr.gameObject.AddComponent<Button>();
                    }
                }
            }

            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons) {
                string n = btn.gameObject.name.ToLower();
                if (replayAudioBtn == null && (n.Contains("speaker") || n.Contains("replay"))) replayAudioBtn = btn;
                else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
                else if (nextButton == null && n == "nextbutton") nextButton = btn;
            }

            if (resultPanel == null) {
                Transform rTrans = transform.Find("ResultPanel") ?? transform.Find("Results") ?? transform.Find("ScorePanel") ?? transform.Find("CompletedPanel");
                if (rTrans != null) resultPanel = rTrans.gameObject;
            }
        }

        private void InitOptionButtons() {
            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    int capturedIndex = i;
                    var btn = optionButtons[i];
                    if (btn != null) {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnOptionSelected(capturedIndex));
                    }
                }
            }
        }

        private void WireEventListeners() {
            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentAudio);
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RetryLesson);
            }
        }

        private void ResetGameState() {
            currentRoundIndex = 0;
            score = 0;
            isProcessingInput = false;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (feedbackTMP != null) feedbackTMP.text = "";

            LoadRound(0, false);
        }

        private void StartIntroAndGame() {
            StartCoroutine(IntroAndFirstRoundCoroutine());
        }

        private IEnumerator IntroAndFirstRoundCoroutine() {
            isProcessingInput = true;
            LoadRound(0, false);

            if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
                yield return new WaitForSeconds(narratorSpeech.length + 0.2f);
            } else {
                yield return new WaitForSeconds(0.3f);
            }

            isProcessingInput = false;
            if (rounds != null && rounds.Length > 0) {
                PlayRoundAudio(rounds[0]);
            }
        }

        public void LoadRound(int roundIdx, bool playAudio = true) {
            if (rounds == null || rounds.Length == 0) return;

            currentRoundIndex = Mathf.Clamp(roundIdx, 0, rounds.Length - 1);
            var round = rounds[currentRoundIndex];
            isProcessingInput = false;

            if (progressTMP != null) {
                progressTMP.text = $"{score}/8";
            }

            if (situationTMP != null) {
                situationTMP.text = round.situationText;
            }

            if (feedbackTMP != null) {
                feedbackTMP.text = "";
            }

            // Populate option texts and toggle active state
            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (optionButtons[i] != null) {
                        if (round.options != null && i < round.options.Length) {
                            optionButtons[i].gameObject.SetActive(true);
                            if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) {
                                optionTexts[i].text = round.options[i];
                            }
                        } else {
                            optionButtons[i].gameObject.SetActive(false);
                        }
                    }
                }
            }

            ResetOptionVisuals();
            if (playAudio) {
                PlayRoundAudio(round);
            }
        }

        private void ResetOptionVisuals() {
            if (optionImages != null) {
                for (int i = 0; i < optionImages.Length; i++) {
                    if (optionImages[i] != null) {
                        optionImages[i].color = defaultOptionColor;
                    }
                }
            }

            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (optionButtons[i] != null) {
                        optionButtons[i].interactable = true;
                        optionButtons[i].transform.localScale = Vector3.one;
                    }
                }
            }
        }

        private void PlayRoundAudio(ArtOfPoliteness_ReadingR01RoundData round) {
            if (round == null) return;
            if (round.situationAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(round.situationAudio);
            }
        }

        public void ReplayCurrentAudio() {
            if (rounds != null && currentRoundIndex >= 0 && currentRoundIndex < rounds.Length) {
                PlayRoundAudio(rounds[currentRoundIndex]);
            }
        }

        public void OnOptionSelected(int selectedIndex) {
            if (isProcessingInput) return;
            if (rounds == null || currentRoundIndex >= rounds.Length) return;

            var round = rounds[currentRoundIndex];
            bool isCorrect = round.isEitherAllowed || (selectedIndex == round.correctOptionIndex);

            StartCoroutine(HandleAnswerCoroutine(round, selectedIndex, isCorrect));
        }

        private IEnumerator HandleAnswerCoroutine(ArtOfPoliteness_ReadingR01RoundData round, int selectedIndex, bool isCorrect) {
            isProcessingInput = true;

            if (isCorrect) {
                score++;
                if (progressTMP != null) {
                    progressTMP.text = $"{score}/8";
                }

                if (optionImages != null && selectedIndex < optionImages.Length && optionImages[selectedIndex] != null) {
                    optionImages[selectedIndex].DOColor(correctColor, 0.25f);
                }
                if (optionButtons != null && selectedIndex < optionButtons.Length && optionButtons[selectedIndex] != null) {
                    optionButtons[selectedIndex].transform.DOScale(1.08f, 0.25f).SetLoops(2, LoopType.Yoyo);
                }

                if (feedbackTMP != null) {
                    feedbackTMP.text = $"<color=#4CAF50><b>Polished Choice!</b></color> {round.explanationText}";
                }

                if (round.feedbackAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(round.feedbackAudio);
                    yield return new WaitForSeconds(round.feedbackAudio.length + 0.3f);
                } else {
                    yield return new WaitForSeconds(1.4f);
                }

                AdvanceToNextRound();

            } else {
                if (optionImages != null && selectedIndex < optionImages.Length && optionImages[selectedIndex] != null) {
                    optionImages[selectedIndex].DOColor(wrongColor, 0.25f);
                }

                if (feedbackTMP != null) {
                    feedbackTMP.text = $"<color=#F44336><b>Consider the setting!</b></color> {round.explanationText}";
                }

                yield return new WaitForSeconds(1.6f);
                ResetOptionVisuals();
                isProcessingInput = false;
            }
        }

        private void AdvanceToNextRound() {
            if (currentRoundIndex < rounds.Length - 1) {
                LoadRound(currentRoundIndex + 1, true);
            } else {
                ShowResults();
            }
        }

        private void ShowResults() {
            if (resultPanel != null) {
                resultPanel.SetActive(true);
            }

            bool passed = score >= 8; // Success condition: at least 8 of 10

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Score: {score} / {rounds.Length}";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ?
                    "<color=#4CAF50><b>Workshop Mastered!</b></color>\nYou chose the right words for every situation!" :
                    "<color=#FFC107><b>Keep Practising!</b></color>\nScore at least 8 of 10 to advance.";
            }

            if (passed) {
                topic = Masters_Topic.Reading;
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnLessonComplete(topic);
                }
                if (nextButton != null) {
                    nextButton.gameObject.SetActive(true);
                }
            }
        }

        public void RetryLesson() {
            ResetGameState();
            LoadRound(0, true);
        }

        public void PopulateDefaultRounds() {
            string audioDir = "Assets/Audio/2B/11_ArtOfPoliteness/Reading/";

            rounds = new ArtOfPoliteness_ReadingR01RoundData[] {
                // Round 1: Restaurant with family
                new ArtOfPoliteness_ReadingR01RoundData {
                    situationText = "You are ordering at a restaurant with your family.",
                    options = new string[] {
                        "I'll have a pizza, please.",
                        "I want a pizza."
                    },
                    correctOptionIndex = 0,
                    isEitherAllowed = false,
                    explanationText = "In a restaurant with family or staff, 'I'll have... please' is polite and courteous.",
#if UNITY_EDITOR
                    situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r01_situation.mp3"),
                    feedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r01_feedback.mp3")
#endif
                },
                // Round 2: Classmate asks for help during test
                new ArtOfPoliteness_ReadingR01RoundData {
                    situationText = "A classmate asks for help while you are finishing a test.",
                    options = new string[] {
                        "I'm busy.",
                        "Sorry – I'm a bit busy right now."
                    },
                    correctOptionIndex = 1,
                    isEitherAllowed = false,
                    explanationText = "'Sorry – I'm a bit busy right now' softens the refusal kindly.",
#if UNITY_EDITOR
                    situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r02_situation.mp3"),
                    feedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r02_feedback.mp3")
#endif
                },
                // Round 3: Teammate you don't know well
                new ArtOfPoliteness_ReadingR01RoundData {
                    situationText = "You need a report from a teammate you don't know well.",
                    options = new string[] {
                        "Could you send me the report?",
                        "Send me the report."
                    },
                    correctOptionIndex = 0,
                    isEitherAllowed = false,
                    explanationText = "'Could you send me the report?' makes a courteous request to a teammate.",
#if UNITY_EDITOR
                    situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r03_situation.mp3"),
                    feedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r03_feedback.mp3")
#endif
                },
                // Round 4: Teacher free time
                new ArtOfPoliteness_ReadingR01RoundData {
                    situationText = "You want to know when your teacher is free.",
                    options = new string[] {
                        "When are you free?",
                        "Let me know when you're available."
                    },
                    correctOptionIndex = 1,
                    isEitherAllowed = false,
                    explanationText = "'Let me know when you're available' shows respect to a teacher.",
#if UNITY_EDITOR
                    situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r04_situation.mp3"),
                    feedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r04_feedback.mp3")
#endif
                },
                // Round 5: Friend wrong date
                new ArtOfPoliteness_ReadingR01RoundData {
                    situationText = "A friend has the wrong date for the exam.",
                    options = new string[] {
                        "You're wrong.",
                        "I think you might be mistaken."
                    },
                    correctOptionIndex = 1,
                    isEitherAllowed = false,
                    explanationText = "'I think you might be mistaken' avoids direct confrontation.",
#if UNITY_EDITOR
                    situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r05_situation.mp3"),
                    feedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r05_feedback.mp3")
#endif
                },
                // Round 6: Group suggests unworkable plan
                new ArtOfPoliteness_ReadingR01RoundData {
                    situationText = "Your group suggests a plan you don't think will work.",
                    options = new string[] {
                        "I'm not so sure that's a good idea.",
                        "That's a terrible idea."
                    },
                    correctOptionIndex = 0,
                    isEitherAllowed = false,
                    explanationText = "'I'm not so sure that's a good idea' offers gentle disagreement.",
#if UNITY_EDITOR
                    situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r06_situation.mp3"),
                    feedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r06_feedback.mp3")
#endif
                },
                // Round 7: Feedback on classmate project
                new ArtOfPoliteness_ReadingR01RoundData {
                    situationText = "You are giving feedback on a classmate's project.",
                    options = new string[] {
                        "I don't like this.",
                        "I'm not quite satisfied with this work."
                    },
                    correctOptionIndex = 1,
                    isEitherAllowed = false,
                    explanationText = "'I'm not quite satisfied with this work' gives constructive feedback.",
#if UNITY_EDITOR
                    situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r07_situation.mp3"),
                    feedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r07_feedback.mp3")
#endif
                },
                // Round 8: Reviewing design
                new ArtOfPoliteness_ReadingR01RoundData {
                    situationText = "You are reviewing a design and want different colours.",
                    options = new string[] {
                        "I'd prefer to use different colours in this design.",
                        "Change the colours."
                    },
                    correctOptionIndex = 0,
                    isEitherAllowed = false,
                    explanationText = "'I'd prefer to use different colours' expresses your preference politely.",
#if UNITY_EDITOR
                    situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r08_situation.mp3"),
                    feedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r08_feedback.mp3")
#endif
                },
                // Round 9: Brother at room door (Home)
                new ArtOfPoliteness_ReadingR01RoundData {
                    situationText = "Your little brother is at the door of your room and you're studying. (home)",
                    options = new string[] {
                        "Close the door.",
                        "Could you close the door, please?"
                    },
                    correctOptionIndex = 0,
                    isEitherAllowed = true, // Either works at home
                    explanationText = "Both work! Plain speech at home with family is completely natural.",
#if UNITY_EDITOR
                    situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r09_situation.mp3"),
                    feedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r09_feedback.mp3")
#endif
                },
                // Round 10: Best friend asking what to eat (Close friend)
                new ArtOfPoliteness_ReadingR01RoundData {
                    situationText = "Your best friend asks what you want to eat. (close friend)",
                    options = new string[] {
                        "I want a pizza.",
                        "I'll have a pizza, please."
                    },
                    correctOptionIndex = 0,
                    isEitherAllowed = true, // Either works with close friend
                    explanationText = "Both work! With close friends, casual direct speech is friendly and normal.",
#if UNITY_EDITOR
                    situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r10_situation.mp3"),
                    feedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_r01_r10_feedback.mp3")
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
