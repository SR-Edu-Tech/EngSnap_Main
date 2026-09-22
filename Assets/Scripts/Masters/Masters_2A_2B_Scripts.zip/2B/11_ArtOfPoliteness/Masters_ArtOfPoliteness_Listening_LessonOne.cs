using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit11 {


    /// <summary>
    /// Unit 11: Art of Politeness — Listening Lesson One (L01 Hear It — Blunt or Polished?)
    /// 10 rounds of Audio-to-Category recognition.
    /// Student listens to a spoken sentence and taps either BLUNT (Impolite) or POLISHED (Polite).
    /// If correct and blunt: the sentence lands on the tray and Aria plays its polished twin.
    /// If wrong: gentle chime + replay option.
    /// Pass threshold: 8 / 10 rounds.
    /// </summary>
    public class Masters_ArtOfPoliteness_Listening_LessonOne : Masters_Lesson {

public enum PolitenessCategory {
        Blunt,      // Impolite / direct command
        Polished    // Polite / courteous question or request
    }

    [System.Serializable]
    public class ArtOfPoliteness_ListeningL01RoundData {
        public string sentenceText;              // Spoken sentence text
        public PolitenessCategory category;      // Blunt or Polished
        public string polishedTwinText;          // Polished alternative if blunt
        public string explanationText;          // Educational explanation
        public AudioClip promptAudio;           // Audio of the spoken sentence
        public AudioClip slowPromptAudio;       // Slow speed audio of the spoken sentence
        public AudioClip polishedTwinAudio;     // Audio of polished twin (played when blunt is chosen)
    }
    
        [Header("10 Audio-to-Category Rounds")]
        [SerializeField] private ArtOfPoliteness_ListeningL01RoundData[] rounds;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI instructionTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;
        [SerializeField] private TextMeshProUGUI spokenSentenceTMP;

        [Header("Two Trays (Blunt & Polished)")]
        [SerializeField] private Button bluntTrayBtn;
        [SerializeField] private Button polishedTrayBtn;
        [SerializeField] private TextMeshProUGUI bluntLabelTMP;
        [SerializeField] private TextMeshProUGUI polishedLabelTMP;
        [SerializeField] private Image bluntTrayBg;
        [SerializeField] private Image polishedTrayBg;
        [SerializeField] private Button[] extraButtons; // In case of 4-button base prefab

        [Header("Audio Controls")]
        [SerializeField] private Button replayAudioBtn;
        [SerializeField] private Toggle repeatThisToggle;
        [SerializeField] private Toggle slowToggle;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;

        [Header("Theme Colors (Captured from Scene)")]
        [SerializeField] private Color bluntDefaultColor = new Color(0.14f, 0.45f, 0.72f, 1f);
        [SerializeField] private Color polishedDefaultColor = new Color(0.14f, 0.45f, 0.72f, 1f);
        [SerializeField] private Color correctColor = new Color(0.14f, 0.68f, 0.32f, 1f);          // Green
        [SerializeField] private Color wrongColor = new Color(0.88f, 0.24f, 0.24f, 1f);            // Red

        private int currentRoundIndex = 0;
        private int score = 0;
        private bool isProcessingInput = false;
        private bool isSlowMode = false;
        private bool isRepeatMode = false;

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Listening;

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/11_ArtOfPoliteness/Listening/artofpoliteness_l01_full_intro.mp3");
#endif
            }

            PurgeLegacyChildren();
            AutoBindReferences();

            // Capture exact button colors from the Scene/Inspector
            if (bluntTrayBg != null) bluntDefaultColor = bluntTrayBg.color;
            if (polishedTrayBg != null) polishedDefaultColor = polishedTrayBg.color;

            EnsureHeaderAndTitle();
            EnsureNextAndBackButtonWired();

            if (rounds == null || rounds.Length == 0) {
                PopulateDefaultRounds();
            }

            InitTrayButtons();
            WireEventListeners();
        }

        protected override void Start() {
            topic = Masters_Topic.Listening;

            EnsureHeaderAndTitle();
            ResetGameState();
            StartIntroAndGame();
        }

        private void EnsureHeaderAndTitle() {
            AutoBindReferences();

            if (headerTMP == null) {
                Transform hTrans = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("UnitHeading/UnitTMP") ?? transform.Find("UnitHeading");
                if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
            }

            if (titleTMP == null) {
                Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle");
                if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
            }

            ResetTrayVisuals();
        }

        private void PurgeLegacyChildren() {
            string[] legacyNames = new string[] {
                "Cloud", "Cloud (1)", "Cloud (2)", "Cloud (3)", "Cloud (4)", "Cloud (5)", "Cloud (6)", "Cloud (7)",
                "cloud grid", "Objects scroll view",
                "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
                "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer"
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
            Transform grid = transform.Find("PhraseCardsGrid") ?? transform.Find("CardsGrid") ?? transform.Find("Options");
            if (grid != null) {
                if (grid.childCount > 0) {
                    bluntTrayBtn = grid.GetChild(0).GetComponent<Button>();
                    if (bluntTrayBtn != null) {
                        bluntTrayBg = bluntTrayBtn.GetComponent<Image>();
                        bluntLabelTMP = bluntTrayBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                    }
                }
                if (grid.childCount > 1) {
                    polishedTrayBtn = grid.GetChild(1).GetComponent<Button>();
                    if (polishedTrayBtn != null) {
                        polishedTrayBg = polishedTrayBtn.GetComponent<Image>();
                        polishedLabelTMP = polishedTrayBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                    }
                }
                List<Button> extras = new List<Button>();
                for (int i = 2; i < grid.childCount; i++) {
                    var childBtn = grid.GetChild(i).GetComponent<Button>();
                    if (childBtn != null) {
                        extras.Add(childBtn);
                        childBtn.gameObject.SetActive(false);
                    }
                }
                extraButtons = extras.ToArray();
            }

            if (progressTMP == null) {
                Transform pTrans = transform.Find("ExpressionCountTMP") ?? transform.Find("Progress") ?? transform.Find("Counter");
                if (pTrans != null) progressTMP = pTrans.GetComponent<TextMeshProUGUI>();
            }

            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps) {
                string n = tmp.gameObject.name.ToLower();
                if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading"))) headerTMP = tmp;
                else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("unittmp") || n == "title" || n == "tmp")) titleTMP = tmp;
                else if (progressTMP == null && (n.Contains("progress") || n.Contains("counter") || n.Contains("expressioncount"))) progressTMP = tmp;
                else if (instructionTMP == null && (n.Contains("instruction") || n.Contains("subtitle"))) instructionTMP = tmp;
                else if (feedbackTMP == null && (n.Contains("feedback") || n.Contains("explanation") || n.Contains("resulttext"))) feedbackTMP = tmp;
                else if (spokenSentenceTMP == null && (n.Contains("spoken") || n.Contains("sentence") || n.Contains("question"))) spokenSentenceTMP = tmp;
            }

            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons) {
                string n = btn.gameObject.name.ToLower();
                if (replayAudioBtn == null && n.Contains("replay")) {
                    replayAudioBtn = btn;
                } else if (retryBtn == null && n.Contains("retry")) {
                    retryBtn = btn;
                } else if (nextButton == null && n == "nextbutton") {
                    nextButton = btn;
                }
            }

            // Controls container search
            Transform controlsTr = transform.Find("Controls") ?? transform.Find("PhraseCardsGrid/Controls") ?? transform.Find("AudioControls");
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

            if (bluntTrayBtn != null && bluntTrayBg == null) bluntTrayBg = bluntTrayBtn.GetComponent<Image>();
            if (polishedTrayBtn != null && polishedTrayBg == null) polishedTrayBg = polishedTrayBtn.GetComponent<Image>();
        }

        private void InitTrayButtons() {
            if (bluntTrayBtn != null) {
                bluntTrayBtn.onClick.RemoveAllListeners();
                bluntTrayBtn.onClick.AddListener(() => OnTraySelected(PolitenessCategory.Blunt));
            }

            if (polishedTrayBtn != null) {
                polishedTrayBtn.onClick.RemoveAllListeners();
                polishedTrayBtn.onClick.AddListener(() => OnTraySelected(PolitenessCategory.Polished));
            }

            if (extraButtons != null) {
                foreach (var extra in extraButtons) {
                    if (extra != null && extra != bluntTrayBtn && extra != polishedTrayBtn && extra != replayAudioBtn && extra != retryBtn && extra != nextButton && !extra.name.ToLower().Contains("back")) {
                        extra.gameObject.SetActive(false);
                    }
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

            // Controls container direct listeners
            Transform controlsTr = transform.Find("Controls") ?? transform.Find("PhraseCardsGrid/Controls") ?? transform.Find("AudioControls");
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
            if (spokenSentenceTMP != null) spokenSentenceTMP.text = "??? (Listen)";
        }

        private void StartIntroAndGame() {
            StartCoroutine(IntroAndFirstRoundCoroutine());
        }

        private IEnumerator IntroAndFirstRoundCoroutine() {
            isProcessingInput = true;
            if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
                yield return new WaitForSeconds(narratorSpeech.length + 0.3f);
            } else {
                yield return new WaitForSeconds(0.5f);
            }
            isProcessingInput = false;
            LoadRound(0);
        }

        public void LoadRound(int roundIdx) {
            if (rounds == null || rounds.Length == 0) return;

            currentRoundIndex = Mathf.Clamp(roundIdx, 0, rounds.Length - 1);
            var round = rounds[currentRoundIndex];
            isProcessingInput = false;

            if (progressTMP != null) {
                progressTMP.text = $"{score}/8";
            }

            if (spokenSentenceTMP != null) {
                spokenSentenceTMP.text = "<color=#90CAF9>🎧 Listen closely...</color>";
            }

            if (feedbackTMP != null) {
                feedbackTMP.text = "";
            }

            ResetTrayVisuals();
            PlayRoundAudio(round);
        }

        private void ResetTrayVisuals() {
            // Revert back to the exact scene colors without forcing grey
            if (bluntTrayBg != null) bluntTrayBg.color = bluntDefaultColor;
            if (polishedTrayBg != null) polishedTrayBg.color = polishedDefaultColor;

            if (bluntTrayBtn != null) {
                bluntTrayBtn.interactable = true;
                bluntTrayBtn.transform.localScale = Vector3.one;
            }
            if (polishedTrayBtn != null) {
                polishedTrayBtn.interactable = true;
                polishedTrayBtn.transform.localScale = Vector3.one;
            }
        }

        private void PlayRoundAudio(ArtOfPoliteness_ListeningL01RoundData round) {
            if (round == null) return;
            AudioClip clip = (isSlowMode && round.slowPromptAudio != null) ? round.slowPromptAudio : round.promptAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }

        public void ReplayCurrentAudio() {
            if (rounds != null && currentRoundIndex >= 0 && currentRoundIndex < rounds.Length) {
                PlayRoundAudio(rounds[currentRoundIndex]);
            }
        }

        public void OnTraySelected(PolitenessCategory chosenCategory) {
            if (isProcessingInput) return;
            if (rounds == null || currentRoundIndex >= rounds.Length) return;

            var round = rounds[currentRoundIndex];
            bool isCorrect = (chosenCategory == round.category);

            StartCoroutine(HandleAnswerCoroutine(round, chosenCategory, isCorrect));
        }

        private IEnumerator HandleAnswerCoroutine(ArtOfPoliteness_ListeningL01RoundData round, PolitenessCategory chosenCategory, bool isCorrect) {
            isProcessingInput = true;

            // Reveal the spoken sentence on the tray
            if (spokenSentenceTMP != null) {
                spokenSentenceTMP.text = $"\"{round.sentenceText}\"";
            }

            if (isCorrect) {
                score++;
                if (chosenCategory == PolitenessCategory.Blunt) {
                    if (bluntTrayBg != null) bluntTrayBg.DOColor(correctColor, 0.25f);
                    if (bluntTrayBtn != null) bluntTrayBtn.transform.DOScale(1.08f, 0.25f).SetLoops(2, LoopType.Yoyo);
                } else {
                    if (polishedTrayBg != null) polishedTrayBg.DOColor(correctColor, 0.25f);
                    if (polishedTrayBtn != null) polishedTrayBtn.transform.DOScale(1.08f, 0.25f).SetLoops(2, LoopType.Yoyo);
                }

                if (feedbackTMP != null) {
                    feedbackTMP.text = $"<color=#4CAF50><b>Correct!</b></color> {round.explanationText}";
                }

                // If it was blunt, ARIA plays its polished twin straight after
                if (round.category == PolitenessCategory.Blunt && round.polishedTwinAudio != null) {
                    yield return new WaitForSeconds(0.8f);
                    if (feedbackTMP != null && !string.IsNullOrEmpty(round.polishedTwinText)) {
                        feedbackTMP.text = $"<color=#4CAF50><b>Correct!</b></color>\n✨ Polished Twin: <i>\"{round.polishedTwinText}\"</i>";
                    }
                    if (Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(round.polishedTwinAudio);
                    }
                    yield return new WaitForSeconds(round.polishedTwinAudio.length + 0.5f);
                } else {
                    yield return new WaitForSeconds(1.2f);
                }

                if (isRepeatMode) {
                    LoadRound(currentRoundIndex);
                } else {
                    AdvanceToNextRound();
                }

            } else {
                // Wrong selection
                if (chosenCategory == PolitenessCategory.Blunt) {
                    if (bluntTrayBg != null) bluntTrayBg.DOColor(wrongColor, 0.25f);
                } else {
                    if (polishedTrayBg != null) polishedTrayBg.DOColor(wrongColor, 0.25f);
                }

                if (feedbackTMP != null) {
                    feedbackTMP.text = $"<color=#F44336><b>Not quite!</b></color> {round.explanationText}\nTap Replay to listen again.";
                }

                yield return new WaitForSeconds(1.8f);
                ResetTrayVisuals();
                isProcessingInput = false;
            }
        }

        private void AdvanceToNextRound() {
            if (currentRoundIndex < rounds.Length - 1) {
                LoadRound(currentRoundIndex + 1);
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
                    "<color=#4CAF50><b>Workshop Mastered!</b></color>\nYou have a keen ear for polite polishing!" :
                    "<color=#FFC107><b>Keep Practising!</b></color>\nScore at least 8 to advance.";
            }

            if (passed) {
                topic = Masters_Topic.Listening;
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnLessonComplete(topic);
                }
            }
        }

        public void RetryLesson() {
            ResetGameState();
            LoadRound(0);
        }

        public void PopulateDefaultRounds() {
            string audioDir = "Assets/Audio/2B/11_ArtOfPoliteness/Listening/";
            string slowDir = "Assets/Audio/2B/11_ArtOfPoliteness/Listening/Slow/";

            rounds = new ArtOfPoliteness_ListeningL01RoundData[] {
                // Round 1 (Blunt)
                new ArtOfPoliteness_ListeningL01RoundData {
                    sentenceText = "Send me the report.",
                    category = PolitenessCategory.Blunt,
                    polishedTwinText = "Could you send me the report, please?",
                    explanationText = "'Send me the report' is a direct command. Polished: 'Could you send me the report, please?'",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l01_r01_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l01_r01_slow.mp3"),
                    polishedTwinAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l01_r01_polished_twin.mp3")
#endif
                },
                // Round 2 (Polished)
                new ArtOfPoliteness_ListeningL01RoundData {
                    sentenceText = "Would you mind closing the window?",
                    category = PolitenessCategory.Polished,
                    polishedTwinText = "",
                    explanationText = "'Would you mind closing the window?' uses polite softening. It belongs on the Polished tray!",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l01_r02_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l01_r02_slow.mp3")
#endif
                },
                // Round 3 (Blunt)
                new ArtOfPoliteness_ListeningL01RoundData {
                    sentenceText = "You're wrong.",
                    category = PolitenessCategory.Blunt,
                    polishedTwinText = "I think you might be mistaken.",
                    explanationText = "'You're wrong' is blunt and confrontational. Polished: 'I think you might be mistaken.'",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l01_r03_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l01_r03_slow.mp3"),
                    polishedTwinAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l01_r03_polished_twin.mp3")
#endif
                },
                // Round 4 (Polished)
                new ArtOfPoliteness_ListeningL01RoundData {
                    sentenceText = "I'll have a coffee, please.",
                    category = PolitenessCategory.Polished,
                    polishedTwinText = "",
                    explanationText = "'I'll have a coffee, please' is courteous and polite. Polished tray!",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l01_r04_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l01_r04_slow.mp3")
#endif
                },
                // Round 5 (Blunt)
                new ArtOfPoliteness_ListeningL01RoundData {
                    sentenceText = "Give me that book.",
                    category = PolitenessCategory.Blunt,
                    polishedTwinText = "Could I borrow that book for a moment?",
                    explanationText = "'Give me that book' sounds demanding. Polished: 'Could I borrow that book for a moment?'",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l01_r05_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l01_r05_slow.mp3"),
                    polishedTwinAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l01_r05_polished_twin.mp3")
#endif
                },
                // Round 6 (Polished)
                new ArtOfPoliteness_ListeningL01RoundData {
                    sentenceText = "May I ask where the meeting is?",
                    category = PolitenessCategory.Polished,
                    polishedTwinText = "",
                    explanationText = "'May I ask...' softens the inquiry politely. Polished tray!",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l01_r06_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l01_r06_slow.mp3")
#endif
                },
                // Round 7 (Blunt)
                new ArtOfPoliteness_ListeningL01RoundData {
                    sentenceText = "That's a terrible idea.",
                    category = PolitenessCategory.Blunt,
                    polishedTwinText = "I'm not sure that would be the best approach.",
                    explanationText = "'That's a terrible idea' is harsh. Polished: 'I'm not sure that would be the best approach.'",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l01_r07_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l01_r07_slow.mp3"),
                    polishedTwinAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l01_r07_polished_twin.mp3")
#endif
                },
                // Round 8 (Polished)
                new ArtOfPoliteness_ListeningL01RoundData {
                    sentenceText = "Could you possibly help me with this?",
                    category = PolitenessCategory.Polished,
                    polishedTwinText = "",
                    explanationText = "'Could you possibly help me...' is gentle and respectful. Polished tray!",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l01_r08_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l01_r08_slow.mp3")
#endif
                },
                // Round 9 (Blunt)
                new ArtOfPoliteness_ListeningL01RoundData {
                    sentenceText = "Tell me what happened.",
                    category = PolitenessCategory.Blunt,
                    polishedTwinText = "Could you tell me a bit more about what happened?",
                    explanationText = "'Tell me what happened' is blunt. Polished: 'Could you tell me a bit more about what happened?'",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l01_r09_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l01_r09_slow.mp3"),
                    polishedTwinAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l01_r09_polished_twin.mp3")
#endif
                },
                // Round 10 (Polished)
                new ArtOfPoliteness_ListeningL01RoundData {
                    sentenceText = "I would appreciate your feedback on this.",
                    category = PolitenessCategory.Polished,
                    polishedTwinText = "",
                    explanationText = "'I would appreciate your feedback' shows high courtesy. Polished tray!",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_l01_r10_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "artofpoliteness_l01_r10_slow.mp3")
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
