using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit11 {


    /// <summary>
    /// Unit 11: Art of Politeness — Listening Lesson Two
    /// (L02 Hear the Blunt One — Tap the Polish)
    /// Student listens to a blunt sentence and taps its matching polished twin from 3 option cards/buttons.
    /// Preserves user Inspector layout & positions (purges legacy slide animators that alter position).
    /// Pass threshold: 6 / 8 pairs.
    /// </summary>
    public class Masters_ArtOfPoliteness_Listening_LessonTwo : Masters_Lesson {

[System.Serializable]
    public class ArtOfPoliteness_ListeningL02RoundData {
        public string bluntSentence;            // Spoken impolite sentence
        public string correctPolishedTwin;      // The authentic polite version
        public string[] options;                // 3 polished options (1 real + 2 distractors)
        public int correctOptionIndex;          // Index (0, 1, or 2)
        public string softeningWords;           // Softening explanation
        public AudioClip bluntAudio;            // Spoken audio of the blunt sentence
        public AudioClip slowBluntAudio;        // Slow spoken audio
        public AudioClip polishedAudio;         // Spoken audio of the polished twin
    }
    
        [Header("8 Polished Matching Rounds")]
        [SerializeField] private ArtOfPoliteness_ListeningL02RoundData[] rounds;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI promptSentenceTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;

        [Header("3 Option Buttons (Polished Twin Options)")]
        [SerializeField] private Button[] optionButtons; // 3 option buttons
        [SerializeField] private TextMeshProUGUI[] optionTexts;
        [SerializeField] private Image[] optionImages;
        [SerializeField] private Button[] extraButtons; // In case of 4th button in base template

        [Header("Audio Controls")]
        [SerializeField] private Button replayAudioBtn;
        [SerializeField] private Toggle repeatThisToggle;
        [SerializeField] private Toggle slowToggle;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;

        [Header("Colors (Captured from Scene)")]
        [SerializeField] private Color defaultOptionColor = Color.white;
        [SerializeField] private Color correctColor = new Color(0.2f, 0.8f, 0.35f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.9f, 0.25f, 0.25f, 1f);

        private int currentRoundIndex = 0;
        private int score = 0;
        private bool isProcessingInput = false;
        private bool isSlowMode = false;
        private bool isRepeatMode = false;

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Listening;

            // Remove legacy slide animation components that forcibly change option button positions
            PurgeSlideAnimations();
            PurgeLegacyChildren();

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/11_ArtOfPoliteness/Listening/artofpoliteness_l02_full_intro.mp3");
#endif
            }

            AutoBindReferences();

            // Capture natural Inspector colors from the first option button
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
            topic = Masters_Topic.Listening;

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
                titleTMP.text = "L02 Hear the Blunt One — Tap the Polish";
            }

            ResetOptionVisuals();
        }

        private void PurgeLegacyChildren() {
            string[] legacyNames = new string[] {
                "Cloud", "Cloud (1)", "Cloud (2)", "Cloud (3)", "Cloud (4)", "Cloud (5)", "Cloud (6)", "Cloud (7)",
                "cloud grid", "Objects scroll view",
                "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words"
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

            // Direct check for Bin 1, Bin 2, Bin 3, Bin 4
            string[] binNames = new string[] { "Bin 1", "Bin 2", "Bin 3", "Bin 4" };
            for (int i = 0; i < binNames.Length; i++) {
                Transform binTr = transform.Find(binNames[i]);
                if (binTr != null) {
                    Button b = binTr.GetComponentInChildren<Button>(true) ?? binTr.GetComponent<Button>();
                    if (b == null) {
                        Transform imgTr = binTr.Find("BinImage") ?? binTr;
                        b = imgTr.gameObject.AddComponent<Button>();
                    }

                    TextMeshProUGUI tmp = binTr.Find("BinTMP")?.GetComponent<TextMeshProUGUI>() ?? binTr.GetComponentInChildren<TextMeshProUGUI>(true);
                    Image img = binTr.Find("BinImage")?.GetComponent<Image>() ?? binTr.GetComponentInChildren<Image>(true);

                    if (i < 3) {
                        binTr.gameObject.SetActive(true);
                        if (b != null) btnList.Add(b);
                        if (tmp != null) txtList.Add(tmp);
                        if (img != null) imgList.Add(img);
                    } else {
                        // Deactivate 4th bin (Bin 4)
                        binTr.gameObject.SetActive(false);
                        if (b != null) extraList.Add(b);
                    }
                }
            }

            // Fallback to container if Bins not direct children
            if (btnList.Count == 0) {
                Transform grid = transform.Find("PhraseCardsGrid") ?? transform.Find("CardsGrid") ?? transform.Find("Options") ?? transform.Find("Bins");
                if (grid != null) {
                    for (int i = 0; i < grid.childCount; i++) {
                        Transform child = grid.GetChild(i);
                        Button b = child.GetComponent<Button>() ?? child.GetComponentInChildren<Button>(true);
                        if (b != null) {
                            if (btnList.Count < 3) {
                                btnList.Add(b);
                                txtList.Add(b.GetComponentInChildren<TextMeshProUGUI>(true));
                                imgList.Add(b.GetComponent<Image>() ?? b.GetComponentInChildren<Image>(true));
                            } else {
                                extraList.Add(b);
                                b.gameObject.SetActive(false);
                            }
                        }
                    }
                }
            }

            if (btnList.Count > 0) {
                optionButtons = btnList.ToArray();
                optionTexts = txtList.ToArray();
                optionImages = imgList.ToArray();
                extraButtons = extraList.ToArray();
            }

            // Bind Progress TMP
            if (progressTMP == null) {
                Transform pTrans = transform.Find("PuzzleCountTMP") ?? transform.Find("ExpressionCountTMP") ?? transform.Find("Progress") ?? transform.Find("Counter");
                if (pTrans != null) progressTMP = pTrans.GetComponent<TextMeshProUGUI>();
            }

            // Bind Prompt Sentence TMP
            if (promptSentenceTMP == null) {
                Transform pTrans = transform.Find("PhraseCard/Text (TMP)") ?? transform.Find("PhraseCardRestPoint/PhraseCard/Text (TMP)") ?? transform.Find("PhraseCard/Text") ?? transform.Find("PhraseCardRestPoint/Text (TMP)") ?? transform.Find("Question") ?? transform.Find("Prompt");
                if (pTrans != null) promptSentenceTMP = pTrans.GetComponent<TextMeshProUGUI>();
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

            // Bind Audio Replay Button (Speaker on card or Replay button)
            if (replayAudioBtn == null) {
                Transform spkTr = transform.Find("PhraseCard/SpeakerImage") ?? transform.Find("PhraseCardRestPoint/PhraseCard/SpeakerImage") ?? transform.Find("PhraseCard/Speaker") ?? transform.Find("PhraseCard");
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
                if (replayAudioBtn == null && n.Contains("replay")) replayAudioBtn = btn;
                else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
                else if (nextButton == null && n == "nextbutton") nextButton = btn;
            }

            Transform controlsTr = transform.Find("Controls") ?? transform.Find("AudioControls");
            if (controlsTr != null) {
                foreach (Transform child in controlsTr) {
                    string cn = child.name.ToLower();
                    if (cn.Contains("repeat") || cn.Contains("replay")) {
                        if (repeatThisToggle == null) repeatThisToggle = child.GetComponent<Toggle>() ?? child.GetComponentInChildren<Toggle>(true);
                    } else if (cn.Contains("slow")) {
                        if (slowToggle == null) slowToggle = child.GetComponent<Toggle>() ?? child.GetComponentInChildren<Toggle>(true);
                    }
                }
            }

            Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
            foreach (var tog in toggles) {
                string n = tog.gameObject.name.ToLower();
                if (repeatThisToggle == null && n.Contains("repeat")) repeatThisToggle = tog;
                else if (slowToggle == null && n.Contains("slow")) slowToggle = tog;
            }

            if (resultPanel == null) {
                Transform rTrans = transform.Find("ResultPanel") ?? transform.Find("Results") ?? transform.Find("ScorePanel");
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

            if (extraButtons != null) {
                foreach (var extra in extraButtons) {
                    if (extra != null) extra.gameObject.SetActive(false);
                }
            }
        }

        private void WireEventListeners() {
            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentAudio);
            }

            if (slowToggle != null) {
                slowToggle.onValueChanged.RemoveAllListeners();
                slowToggle.onValueChanged.AddListener((val) => {
                    isSlowMode = val;
                    ReplayCurrentAudio();
                });
            }

            if (repeatThisToggle != null) {
                repeatThisToggle.onValueChanged.RemoveAllListeners();
                repeatThisToggle.onValueChanged.AddListener((val) => {
                    isRepeatMode = val;
                    ReplayCurrentAudio();
                });
            }

            Transform controlsTr = transform.Find("Controls") ?? transform.Find("AudioControls");
            if (controlsTr != null) {
                foreach (Transform child in controlsTr) {
                    string cn = child.name.ToLower();
                    if (cn.Contains("repeat") || cn.Contains("replay")) {
                        Button b = child.GetComponent<Button>() ?? child.GetComponentInChildren<Button>(true);
                        if (b != null) {
                            b.onClick.RemoveAllListeners();
                            b.onClick.AddListener(ReplayCurrentAudio);
                        }
                    } else if (cn.Contains("slow")) {
                        Button b = child.GetComponent<Button>() ?? child.GetComponentInChildren<Button>(true);
                        if (b != null) {
                            b.onClick.RemoveAllListeners();
                            b.onClick.AddListener(() => {
                                isSlowMode = !isSlowMode;
                                if (slowToggle != null) slowToggle.isOn = isSlowMode;
                                ReplayCurrentAudio();
                            });
                        }
                    }
                }
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

            // Immediately display round 0 data without waiting
            LoadRound(0, false);
        }

        private void StartIntroAndGame() {
            StartCoroutine(IntroAndFirstRoundCoroutine());
        }

        private IEnumerator IntroAndFirstRoundCoroutine() {
            isProcessingInput = true;
            LoadRound(0, false); // Populate text & options immediately

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
                progressTMP.text = $"{score}/6";
            }

            if (promptSentenceTMP != null) {
                promptSentenceTMP.text = $"\"{round.bluntSentence}\"";
            }

            if (feedbackTMP != null) {
                feedbackTMP.text = "";
            }

            // Populate 3 Option Texts on the Option Buttons / Cards
            if (optionTexts != null && round.options != null) {
                for (int i = 0; i < optionTexts.Length; i++) {
                    if (optionTexts[i] != null && i < round.options.Length) {
                        optionTexts[i].text = round.options[i];
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

        private void PlayRoundAudio(ArtOfPoliteness_ListeningL02RoundData round) {
            if (round == null) return;
            AudioClip clip = (isSlowMode && round.slowBluntAudio != null) ? round.slowBluntAudio : round.bluntAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
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
            bool isCorrect = (selectedIndex == round.correctOptionIndex);

            StartCoroutine(HandleAnswerCoroutine(round, selectedIndex, isCorrect));
        }

        private IEnumerator HandleAnswerCoroutine(ArtOfPoliteness_ListeningL02RoundData round, int selectedIndex, bool isCorrect) {
            isProcessingInput = true;

            if (isCorrect) {
                score++;
                if (progressTMP != null) {
                    progressTMP.text = $"{score}/6";
                }

                if (optionImages != null && selectedIndex < optionImages.Length && optionImages[selectedIndex] != null) {
                    optionImages[selectedIndex].DOColor(correctColor, 0.25f);
                }
                if (optionButtons != null && selectedIndex < optionButtons.Length && optionButtons[selectedIndex] != null) {
                    optionButtons[selectedIndex].transform.DOScale(1.08f, 0.25f).SetLoops(2, LoopType.Yoyo);
                }

                if (feedbackTMP != null) {
                    feedbackTMP.text = $"<color=#4CAF50><b>Polished!</b></color> {round.softeningWords}";
                }

                if (round.polishedAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(round.polishedAudio);
                    yield return new WaitForSeconds(round.polishedAudio.length + 0.4f);
                } else {
                    yield return new WaitForSeconds(1.2f);
                }

                if (isRepeatMode) {
                    LoadRound(currentRoundIndex, true);
                } else {
                    AdvanceToNextRound();
                }

            } else {
                if (optionImages != null && selectedIndex < optionImages.Length && optionImages[selectedIndex] != null) {
                    optionImages[selectedIndex].DOColor(wrongColor, 0.25f);
                }

                if (feedbackTMP != null) {
                    feedbackTMP.text = $"<color=#F44336><b>Not quite!</b></color> Listen again and tap the matching polished version.";
                }

                yield return new WaitForSeconds(1.4f);
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

            bool passed = score >= 6; // Success condition: at least 6 of 8

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Score: {score} / {rounds.Length}";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ?
                    "<color=#4CAF50><b>Workshop Mastered!</b></color>\nYou matched the polished twins perfectly!" :
                    "<color=#FFC107><b>Keep Practising!</b></color>\nMatch at least 6 pairs to advance.";
            }

            if (passed) {
                topic = Masters_Topic.Listening;
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
            string audioDir = "Assets/Audio/2B/11_ArtOfPoliteness/Listening/";
            string slowDir = "Assets/Audio/2B/11_ArtOfPoliteness/Listening/Slow/";

            rounds = new ArtOfPoliteness_ListeningL02RoundData[] {
                // Round 1: "Send me the report."
                new ArtOfPoliteness_ListeningL02RoundData {
                    bluntSentence = "Send me the report.",
                    correctPolishedTwin = "Could you send me the report, please?",
                    options = new string[] {
                        "Could you send me the report, please?",
                        "Would you mind closing the door?",
                        "May I have a glass of water?"
                    },
                    correctOptionIndex = 0,
                    softeningWords = "Softening with 'Could you... please?' turns a blunt demand into a polite request.",
#if UNITY_EDITOR
                    bluntAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l02_r01_blunt.mp3"),
                    slowBluntAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l02_r01_slow.mp3"),
                    polishedAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l02_r01_polished.mp3")
#endif
                },
                // Round 2: "You're wrong."
                new ArtOfPoliteness_ListeningL02RoundData {
                    bluntSentence = "You're wrong.",
                    correctPolishedTwin = "I think you might be mistaken.",
                    options = new string[] {
                        "I'll have a coffee, please.",
                        "I think you might be mistaken.",
                        "Could you possibly help me?"
                    },
                    correctOptionIndex = 1,
                    softeningWords = "'I think you might be mistaken' softens direct confrontation.",
#if UNITY_EDITOR
                    bluntAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l02_r02_blunt.mp3"),
                    slowBluntAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l02_r02_slow.mp3"),
                    polishedAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l02_r02_polished.mp3")
#endif
                },
                // Round 3: "I want a pizza."
                new ArtOfPoliteness_ListeningL02RoundData {
                    bluntSentence = "I want a pizza.",
                    correctPolishedTwin = "I'll have a pizza, please.",
                    options = new string[] {
                        "Would you mind waiting a moment?",
                        "Could I borrow your pen?",
                        "I'll have a pizza, please."
                    },
                    correctOptionIndex = 2,
                    softeningWords = "'I'll have... please' orders food with courteous grace.",
#if UNITY_EDITOR
                    bluntAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l02_r03_blunt.mp3"),
                    slowBluntAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l02_r03_slow.mp3"),
                    polishedAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l02_r03_polished.mp3")
#endif
                },
                // Round 4: "Give me that book."
                new ArtOfPoliteness_ListeningL02RoundData {
                    bluntSentence = "Give me that book.",
                    correctPolishedTwin = "Could I borrow that book for a moment?",
                    options = new string[] {
                        "Could I borrow that book for a moment?",
                        "May I ask where the office is?",
                        "I think you might be mistaken."
                    },
                    correctOptionIndex = 0,
                    softeningWords = "'Could I borrow... for a moment' politely asks instead of demanding.",
#if UNITY_EDITOR
                    bluntAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l02_r04_blunt.mp3"),
                    slowBluntAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l02_r04_slow.mp3"),
                    polishedAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l02_r04_polished.mp3")
#endif
                },
                // Round 5: "Where is the meeting?"
                new ArtOfPoliteness_ListeningL02RoundData {
                    bluntSentence = "Where is the meeting?",
                    correctPolishedTwin = "May I ask where the meeting is?",
                    options = new string[] {
                        "Could you send me the report, please?",
                        "May I ask where the meeting is?",
                        "I would appreciate your feedback."
                    },
                    correctOptionIndex = 1,
                    softeningWords = "'May I ask...' softens direct inquiries respectfully.",
#if UNITY_EDITOR
                    bluntAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l02_r05_blunt.mp3"),
                    slowBluntAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l02_r05_slow.mp3"),
                    polishedAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l02_r05_polished.mp3")
#endif
                },
                // Round 6: "That's a terrible idea."
                new ArtOfPoliteness_ListeningL02RoundData {
                    bluntSentence = "That's a terrible idea.",
                    correctPolishedTwin = "I'm not sure that would be the best approach.",
                    options = new string[] {
                        "I'll have a pizza, please.",
                        "Could I borrow that book?",
                        "I'm not sure that would be the best approach."
                    },
                    correctOptionIndex = 2,
                    softeningWords = "'I'm not sure that would be...' offers constructive disagreement without offending.",
#if UNITY_EDITOR
                    bluntAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l02_r06_blunt.mp3"),
                    slowBluntAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l02_r06_slow.mp3"),
                    polishedAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l02_r06_polished.mp3")
#endif
                },
                // Round 7: "Help me with this."
                new ArtOfPoliteness_ListeningL02RoundData {
                    bluntSentence = "Help me with this.",
                    correctPolishedTwin = "Could you possibly help me with this?",
                    options = new string[] {
                        "Could you possibly help me with this?",
                        "May I ask where the meeting is?",
                        "Would you mind closing the door?"
                    },
                    correctOptionIndex = 0,
                    softeningWords = "'Could you possibly...' asks for assistance with humility and politeness.",
#if UNITY_EDITOR
                    bluntAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l02_r07_blunt.mp3"),
                    slowBluntAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l02_r07_slow.mp3"),
                    polishedAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l02_r07_polished.mp3")
#endif
                },
                // Round 8: "Tell me what happened."
                new ArtOfPoliteness_ListeningL02RoundData {
                    bluntSentence = "Tell me what happened.",
                    correctPolishedTwin = "Could you tell me a bit more about what happened?",
                    options = new string[] {
                        "I think you might be mistaken.",
                        "Could you tell me a bit more about what happened?",
                        "Could you send me the report, please?"
                    },
                    correctOptionIndex = 1,
                    softeningWords = "'Could you tell me a bit more...' invites conversation kindly.",
#if UNITY_EDITOR
                    bluntAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l02_r08_blunt.mp3"),
                    slowBluntAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l02_r08_slow.mp3"),
                    polishedAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l02_r08_polished.mp3")
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
