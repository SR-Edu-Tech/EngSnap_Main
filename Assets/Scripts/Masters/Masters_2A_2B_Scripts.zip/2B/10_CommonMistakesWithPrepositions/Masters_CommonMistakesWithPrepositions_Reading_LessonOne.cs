using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit10 {


    /// <summary>
    /// Unit 10: Common Mistakes with Prepositions — Reading Lesson One
    /// (R01 Spot the Loose Part)
    /// Features interactive 12 Fill-In-The-Blanks statements across 2 sets (6 per set)
    /// with instant audio readout, visual error-correction feedback, and progress tracking.
    /// </summary>
    public class Masters_CommonMistakesWithPrepositions_Reading_LessonOne : Masters_Lesson {

[System.Serializable]
    public class CommonMistakes_ReadingR01RoundData {
        public string wobblySentence;
        public string[] words;
        public int wrongWordIndex;
        public string fixedWord;
        public bool isRemoval;
        public bool isSwap;
        public int swapWithIndex;
        public string repairedSentence;
        public string explanationText;
        public string fixCategory;
        public AudioClip promptAudio;
        public AudioClip confirmAudio;
        public AudioClip fixAudio;
    }
    
        [Header("12 Reading Error-Location Rounds")]
        [SerializeField] private CommonMistakes_ReadingR01RoundData[] rounds;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI speechBubbleTMP;
        [SerializeField] private TextMeshProUGUI promptSentenceTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;
        [SerializeField] private TextMeshProUGUI correctlyFilledTMP;

        [Header("Fill In The Blanks Hierarchies")]
        [SerializeField] private GameObject set1StatementsContainer;
        [SerializeField] private GameObject set1WordsContainer;
        [SerializeField] private GameObject set2StatementsContainer;
        [SerializeField] private GameObject set2WordsContainer;
        [SerializeField] private GameObject completedPanel;

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
        [SerializeField] private Color defaultWordColor = new Color(0.14f, 0.38f, 0.58f, 1f);
        [SerializeField] private Color correctWordColor = new Color(0.14f, 0.65f, 0.28f, 1f);
        [SerializeField] private Color wrongWordColor = new Color(0.85f, 0.22f, 0.22f, 1f);
        [SerializeField] private Color activeBlankColor = new Color(1f, 0.85f, 0.2f, 1f);

        private int currentSetIndex = 0; // 0 = Set 1 (rounds 1-6), 1 = Set 2 (rounds 7-12)
        private int set1CorrectCount = 0;
        private int set2CorrectCount = 0;
        private int totalScore = 0;
        private bool isProcessingInput = false;
        private bool isSlowMode = false;
        private bool isRepeatMode = false;

        private int selectedBlankIndex = -1;

        // Cached references for Set 1 (Statements in QuestionStatements (1), Words in WordsOne (1))
        private List<Button> set1BlankButtons = new List<Button>();
        private List<TextMeshProUGUI> set1BlankTexts = new List<TextMeshProUGUI>();
        private List<Button> set1WordButtons = new List<Button>();
        private List<string> set1CorrectAnswers = new List<string> { "OF", "IN", "ON", "ON", "HOME", "NOW" };
        private List<bool> set1FilledState = new List<bool>();

        // Cached references for Set 2 (Statements in QuestionStatements, Words in WordsOne)
        private List<Button> set2BlankButtons = new List<Button>();
        private List<TextMeshProUGUI> set2BlankTexts = new List<TextMeshProUGUI>();
        private List<Button> set2WordButtons = new List<Button>();
        private List<string> set2CorrectAnswers = new List<string> { "TO", "TO", "NOW", "DRINK", "IN", "ON" };
        private List<bool> set2FilledState = new List<bool>();

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Reading;

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/10_CommonMistakesWithPrepositions/Reading/cm_r01_full_intro.mp3");
#endif
            }

            PurgeLegacyChildren();
            AutoBindReferences();
            EnsureHeaderAndTitle();
            EnsureNextAndBackButtonWired();

            if (rounds == null || rounds.Length == 0 || rounds[0] == null || rounds[0].promptAudio == null) {
                PopulateDefaultRounds();
            }

            InitFillInTheBlanks();
            WireEventListeners();

            if (resultPanel != null) resultPanel.SetActive(false);
            if (completedPanel != null) completedPanel.SetActive(false);
        }

        protected override void Start() {
            base.Start();
            EnsureNextAndBackButtonWired();
            EnsureAspectRatiosAndAnchorsPreserved();
            topic = Masters_Topic.Reading;

            currentSetIndex = 0;
            set1CorrectCount = 0;
            set2CorrectCount = 0;
            totalScore = 0;
            selectedBlankIndex = -1;
            isProcessingInput = false;

            ActivateSet(0);
            UpdateProgressDisplay();
            StartCoroutine(InitializeLessonRoutine());
        }

        private IEnumerator InitializeLessonRoutine() {
            if (speechBubbleTMP != null && speechBubbleTMP != promptSentenceTMP) {
                speechBubbleTMP.text = "Spot the loose part! Read each sentence and choose the correct word to fix it.";
            }

            if (narratorSpeech != null) {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
                } else {
                    AudioSource src = GetComponent<AudioSource>();
                    if (src == null) src = gameObject.AddComponent<AudioSource>();
                    src.clip = narratorSpeech;
                    src.Play();
                }
            }

            yield return new WaitForSeconds(0.3f);
            isProcessingInput = false;
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
                titleTMP.text = "R01 Spot the Loose Part";
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
            if (correctlyFilledTMP == null) {
                Transform c = transform.Find("CorrectlyFilledTMP") ?? transform.Find("ExpressionCountTMP") ?? transform.Find("ProgressCountTMP");
                if (c != null) correctlyFilledTMP = c.GetComponent<TextMeshProUGUI>();
            }

            // Find Containers
            Transform fitbRoot = transform.Find("IsThereADifference_Reading_FillInTheBlank") 
                              ?? transform.Find("FillInTheBlanksPosition/IsThereADifference_Reading_FillInTheBlank")
                              ?? transform;

            // Set 1 is QuestionStatements (1) and WordsOne (1)
            if (set1StatementsContainer == null) {
                Transform t = fitbRoot.Find("QuestionStatements (1)") ?? fitbRoot.Find("QuestionStatements");
                if (t != null) set1StatementsContainer = t.gameObject;
            }

            if (set1WordsContainer == null) {
                Transform t = fitbRoot.Find("WordsOne (1)") ?? fitbRoot.Find("WordsOne");
                if (t != null) set1WordsContainer = t.gameObject;
            }

            // Set 2 is QuestionStatements and WordsOne
            if (set2StatementsContainer == null) {
                Transform t = fitbRoot.Find("QuestionStatements") ?? fitbRoot.Find("QuestionStatements (1)");
                if (t != null) set2StatementsContainer = t.gameObject;
            }

            if (set2WordsContainer == null) {
                Transform t = fitbRoot.Find("WordsOne") ?? fitbRoot.Find("WordsOne (1)");
                if (t != null) set2WordsContainer = t.gameObject;
            }

            if (completedPanel == null) {
                Transform t = fitbRoot.Find("CompletedPanel") ?? transform.Find("CompletedPanel");
                if (t != null) completedPanel = t.gameObject;
            }

            // Bind Audio controls
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
        }

        private void InitFillInTheBlanks() {
            AutoBindReferences();

            SetupSetContainer(
                set1StatementsContainer, 
                set1WordsContainer, 
                set1BlankButtons, 
                set1BlankTexts, 
                set1WordButtons, 
                set1FilledState, 
                0
            );

            SetupSetContainer(
                set2StatementsContainer, 
                set2WordsContainer, 
                set2BlankButtons, 
                set2BlankTexts, 
                set2WordButtons, 
                set2FilledState, 
                1
            );
        }

        private void SetupSetContainer(
            GameObject statementsContainer, 
            GameObject wordsContainer, 
            List<Button> blankButtons, 
            List<TextMeshProUGUI> blankTexts, 
            List<Button> wordButtons, 
            List<bool> filledState,
            int setIndex) 
        {
            blankButtons.Clear();
            blankTexts.Clear();
            wordButtons.Clear();
            filledState.Clear();

            if (statementsContainer != null) {
                Transform stRoot = statementsContainer.transform.Find("Statements") ?? statementsContainer.transform;
                for (int i = 0; i < stRoot.childCount; i++) {
                    Transform row = stRoot.GetChild(i);
                    if (row.name.ToLower().Contains("fill") || row.name.ToLower().Contains("border")) continue;

                    Button blankBtn = row.GetComponentInChildren<Button>(true);
                    if (blankBtn != null) {
                        int blankIdx = blankButtons.Count;
                        if (blankIdx >= 6) break; // 6 blanks per set

                        blankButtons.Add(blankBtn);
                        filledState.Add(false);

                        TextMeshProUGUI tmp = blankBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                        blankTexts.Add(tmp);

                        blankBtn.interactable = true;
                        blankBtn.onClick.RemoveAllListeners();
                        blankBtn.onClick.AddListener(() => OnBlankClicked(blankIdx));
                    }
                }
            }

            if (wordsContainer != null) {
                for (int i = 0; i < wordsContainer.transform.childCount; i++) {
                    Transform wChild = wordsContainer.transform.GetChild(i);
                    Button wBtn = wChild.GetComponent<Button>() ?? wChild.GetComponentInChildren<Button>(true);
                    if (wBtn != null) {
                        int wIdx = wordButtons.Count;
                        if (wIdx >= 6) break; // 6 words per set

                        wordButtons.Add(wBtn);
                        wBtn.interactable = true;

                        Image img = wBtn.GetComponent<Image>();
                        if (img != null) img.color = defaultWordColor;

                        wBtn.onClick.RemoveAllListeners();
                        wBtn.onClick.AddListener(() => OnWordOptionClicked(wIdx, wBtn));
                    }
                }
            }
        }

        private string GetButtonLabel(Button btn) {
            if (btn == null) return "";
            var tmps = btn.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps) {
                string s = t.text.Trim().ToUpper();
                if (!string.IsNullOrEmpty(s) && !s.Contains("[TAP HERE]") && !s.Contains("STATEMENT")) {
                    return s;
                }
            }
            var txts = btn.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            foreach (var t in txts) {
                string s = t.text.Trim().ToUpper();
                if (!string.IsNullOrEmpty(s)) return s;
            }
            return "";
        }

        private void ActivateSet(int setIndex) {
            currentSetIndex = setIndex;
            selectedBlankIndex = -1;

            if (setIndex == 0) {
                if (set1StatementsContainer != null) set1StatementsContainer.SetActive(true);
                if (set1WordsContainer != null) set1WordsContainer.SetActive(true);
                if (set2StatementsContainer != null) set2StatementsContainer.SetActive(false);
                if (set2WordsContainer != null) set2WordsContainer.SetActive(false);
            } else {
                if (set1StatementsContainer != null) set1StatementsContainer.SetActive(false);
                if (set1WordsContainer != null) set1WordsContainer.SetActive(false);
                if (set2StatementsContainer != null) set2StatementsContainer.SetActive(true);
                if (set2WordsContainer != null) set2WordsContainer.SetActive(true);
            }

            UpdateProgressDisplay();
        }

        private void UpdateProgressDisplay() {
            int currentDone = (currentSetIndex == 0) ? set1CorrectCount : set2CorrectCount;
            if (correctlyFilledTMP != null) {
                correctlyFilledTMP.text = $"{currentDone}/6";
            }
            if (progressTMP != null) {
                progressTMP.text = (currentSetIndex == 0) ? $"Set 1 (Rounds 1-6)" : $"Set 2 (Rounds 7-12)";
            }
        }

        private void OnBlankClicked(int blankIndex) {
            if (isProcessingInput) return;

            selectedBlankIndex = blankIndex;
            HighlightBlank(blankIndex);

            // Play the corresponding sentence prompt audio
            int roundIdx = (currentSetIndex == 0) ? blankIndex : (blankIndex + 6);
            if (rounds != null && roundIdx >= 0 && roundIdx < rounds.Length) {
                AudioClip clip = rounds[roundIdx].promptAudio;
                if (clip != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(clip);
                }
            }
        }

        private void HighlightBlank(int blankIndex) {
            var blankList = (currentSetIndex == 0) ? set1BlankButtons : set2BlankButtons;
            var filledState = (currentSetIndex == 0) ? set1FilledState : set2FilledState;

            for (int i = 0; i < blankList.Count; i++) {
                if (blankList[i] != null) {
                    Image img = blankList[i].GetComponent<Image>();
                    if (img != null) {
                        bool isFilled = (i < filledState.Count && filledState[i]);
                        if (!isFilled) {
                            img.color = (i == blankIndex) ? activeBlankColor : defaultWordColor;
                        }
                    }
                }
            }
        }

        private void OnWordOptionClicked(int wordIndex, Button wordBtn) {
            if (isProcessingInput) return;
            if (wordBtn == null) return;

            string wordText = GetButtonLabel(wordBtn);
            if (string.IsNullOrEmpty(wordText)) return;

            var correctAnswers = (currentSetIndex == 0) ? set1CorrectAnswers : set2CorrectAnswers;
            var blankButtons = (currentSetIndex == 0) ? set1BlankButtons : set2BlankButtons;
            var filledState = (currentSetIndex == 0) ? set1FilledState : set2FilledState;

            // 1. Determine target blank index
            int targetBlank = selectedBlankIndex;

            // If no valid unfilled blank selected, find first unfilled matching blank, or first unfilled blank
            if (targetBlank < 0 || targetBlank >= blankButtons.Count || (targetBlank < filledState.Count && filledState[targetBlank])) {
                targetBlank = -1;
                // Match first unfilled blank whose correct answer is wordText
                for (int i = 0; i < correctAnswers.Count; i++) {
                    if (i < filledState.Count && !filledState[i]) {
                        if (correctAnswers[i].Equals(wordText, System.StringComparison.OrdinalIgnoreCase)) {
                            targetBlank = i;
                            break;
                        }
                    }
                }
                // If not found, pick first unfilled blank
                if (targetBlank == -1) {
                    for (int i = 0; i < filledState.Count; i++) {
                        if (!filledState[i]) {
                            targetBlank = i;
                            break;
                        }
                    }
                }
            }

            if (targetBlank < 0 || targetBlank >= correctAnswers.Count) return;

            // Check correctness
            bool isCorrect = correctAnswers[targetBlank].Equals(wordText, System.StringComparison.OrdinalIgnoreCase);

            StartCoroutine(HandleAnswerEvaluation(isCorrect, targetBlank, wordText, wordBtn));
        }

        private IEnumerator HandleAnswerEvaluation(bool isCorrect, int blankIdx, string wordText, Button wordBtn) {
            isProcessingInput = true;

            var blankButtons = (currentSetIndex == 0) ? set1BlankButtons : set2BlankButtons;
            var blankTexts = (currentSetIndex == 0) ? set1BlankTexts : set2BlankTexts;
            var filledState = (currentSetIndex == 0) ? set1FilledState : set2FilledState;

            Button targetBlankBtn = (blankIdx >= 0 && blankIdx < blankButtons.Count) ? blankButtons[blankIdx] : null;
            TextMeshProUGUI targetBlankTMP = (blankIdx >= 0 && blankIdx < blankTexts.Count) ? blankTexts[blankIdx] : null;
            Image wordImg = (wordBtn != null) ? wordBtn.GetComponent<Image>() : null;
            Image blankImg = (targetBlankBtn != null) ? targetBlankBtn.GetComponent<Image>() : null;

            int roundIdx = (currentSetIndex == 0) ? blankIdx : (blankIdx + 6);
            CommonMistakes_ReadingR01RoundData r = (rounds != null && roundIdx < rounds.Length) ? rounds[roundIdx] : null;

            if (isCorrect) {
                if (blankIdx < filledState.Count) filledState[blankIdx] = true;

                if (targetBlankTMP != null) {
                    targetBlankTMP.text = wordText;
                    targetBlankTMP.color = Color.white;
                }

                if (blankImg != null) blankImg.color = correctWordColor;
                if (wordImg != null) wordImg.color = correctWordColor;

                if (targetBlankBtn != null) {
                    targetBlankBtn.interactable = false;
                    targetBlankBtn.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 6, 0.5f);
                }

                if (wordBtn != null) {
                    wordBtn.interactable = false;
                }

                if (currentSetIndex == 0) set1CorrectCount++;
                else set2CorrectCount++;
                totalScore++;

                UpdateProgressDisplay();
                selectedBlankIndex = -1;

                if (speechBubbleTMP != null) {
                    speechBubbleTMP.text = (r != null) ? $"✅ \"{r.repairedSentence}\"" : $"✅ Correct!";
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                AudioClip confirmClip = (r != null) ? r.confirmAudio : null;
                if (confirmClip != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(confirmClip);
                    yield return new WaitForSeconds(confirmClip.length + 0.2f);
                } else {
                    yield return new WaitForSeconds(0.6f);
                }

                // Check Set Completion
                int currentSetDone = (currentSetIndex == 0) ? set1CorrectCount : set2CorrectCount;
                if (currentSetDone >= 6) {
                    yield return StartCoroutine(HandleSetCompletionRoutine());
                }
            } else {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                if (wordImg != null) {
                    wordImg.DOColor(wrongWordColor, 0.2f).OnComplete(() => {
                        if (wordImg != null) wordImg.DOColor(defaultWordColor, 0.3f);
                    });
                }
                if (wordBtn != null) {
                    wordBtn.transform.DOShakePosition(0.35f, new Vector3(8f, 0f, 0f), 10, 90f);
                }

                if (speechBubbleTMP != null && r != null) {
                    speechBubbleTMP.text = $"⚠️ {r.explanationText}";
                }

                AudioClip fixClip = (r != null && r.fixAudio != null) ? r.fixAudio : null;
                if (fixClip != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(fixClip);
                    yield return new WaitForSeconds(fixClip.length + 0.2f);
                } else {
                    yield return new WaitForSeconds(0.8f);
                }
            }

            isProcessingInput = false;
        }

        private IEnumerator HandleSetCompletionRoutine() {
            if (currentSetIndex == 0) {
                if (speechBubbleTMP != null) {
                    speechBubbleTMP.text = "Great Job on Set 1! Loading Set 2...";
                }
                yield return new WaitForSeconds(1.0f);
                ActivateSet(1);
            } else {
                ShowFinalResults();
            }
        }

        private void ShowFinalResults() {
            if (completedPanel != null) {
                completedPanel.SetActive(true);
            }
            if (resultPanel != null) {
                resultPanel.SetActive(true);
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Score: {totalScore} / 12";
            }

            bool passed = totalScore >= 10;
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "Great Job! Unit 10 Reading R01 Completed!" : "Keep practicing! Try again!";
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
            int roundIdx = (currentSetIndex == 0) ? (selectedBlankIndex >= 0 ? selectedBlankIndex : 0) : (selectedBlankIndex >= 0 ? selectedBlankIndex + 6 : 6);
            if (rounds != null && roundIdx >= 0 && roundIdx < rounds.Length) {
                var r = rounds[roundIdx];
                AudioClip clipToPlay = r.promptAudio;
                if (clipToPlay != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
                }
            }
        }

        public void RestartLesson() {
            currentSetIndex = 0;
            set1CorrectCount = 0;
            set2CorrectCount = 0;
            totalScore = 0;
            selectedBlankIndex = -1;
            isProcessingInput = false;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (completedPanel != null) completedPanel.SetActive(false);

            InitFillInTheBlanks();
            ActivateSet(0);
            StartCoroutine(InitializeLessonRoutine());
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

            rounds = new CommonMistakes_ReadingR01RoundData[] {
                // Set 1 - Round 1
                new CommonMistakes_ReadingR01RoundData {
                    wobblySentence = "The dessert consisted from fruit and cream.",
                    words = new string[] { "The", "dessert", "consisted", "from", "fruit", "and", "cream." },
                    wrongWordIndex = 3,
                    fixedWord = "of",
                    isRemoval = false,
                    repairedSentence = "The dessert consisted of fruit and cream.",
                    explanationText = "Preposition fix: use 'of' after 'consisted'.",
                    fixCategory = "Preposition",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r01_prompt.mp3"),
                    confirmAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r01_confirm.mp3"),
                    fixAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r01_fix.mp3")
#endif
                },
                // Set 1 - Round 2
                new CommonMistakes_ReadingR01RoundData {
                    wobblySentence = "There are flowers on the picture.",
                    words = new string[] { "There", "are", "flowers", "on", "the", "picture." },
                    wrongWordIndex = 3,
                    fixedWord = "in",
                    isRemoval = false,
                    repairedSentence = "There are flowers in the picture.",
                    explanationText = "Preposition fix: use 'in' for contents inside a picture.",
                    fixCategory = "Preposition",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r02_prompt.mp3"),
                    confirmAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r02_confirm.mp3"),
                    fixAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r02_fix.mp3")
#endif
                },
                // Set 1 - Round 3
                new CommonMistakes_ReadingR01RoundData {
                    wobblySentence = "It depends from you.",
                    words = new string[] { "It", "depends", "from", "you." },
                    wrongWordIndex = 2,
                    fixedWord = "on",
                    isRemoval = false,
                    repairedSentence = "It depends on you.",
                    explanationText = "Preposition fix: always say 'depends on'.",
                    fixCategory = "Preposition",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r03_prompt.mp3"),
                    confirmAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r03_confirm.mp3"),
                    fixAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r03_fix.mp3")
#endif
                },
                // Set 1 - Round 4
                new CommonMistakes_ReadingR01RoundData {
                    wobblySentence = "Who is in the phone?",
                    words = new string[] { "Who", "is", "in", "the", "phone?" },
                    wrongWordIndex = 2,
                    fixedWord = "on",
                    isRemoval = false,
                    repairedSentence = "Who is on the phone?",
                    explanationText = "Preposition fix: use 'on the phone'.",
                    fixCategory = "Preposition",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r04_prompt.mp3"),
                    confirmAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r04_confirm.mp3"),
                    fixAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r04_fix.mp3")
#endif
                },
                // Set 1 - Round 5
                new CommonMistakes_ReadingR01RoundData {
                    wobblySentence = "I am going to home.",
                    words = new string[] { "I", "am", "going", "to", "home." },
                    wrongWordIndex = 3,
                    fixedWord = "",
                    isRemoval = true,
                    repairedSentence = "I am going home.",
                    explanationText = "Small word fix: remove 'to', say 'going home'.",
                    fixCategory = "Small Word",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r05_prompt.mp3"),
                    confirmAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r05_confirm.mp3"),
                    fixAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r05_fix.mp3")
#endif
                },
                // Set 1 - Round 6
                new CommonMistakes_ReadingR01RoundData {
                    wobblySentence = "Where is my phone at?",
                    words = new string[] { "Where", "is", "my", "phone", "at?" },
                    wrongWordIndex = 4,
                    fixedWord = "",
                    isRemoval = true,
                    repairedSentence = "Where is my phone?",
                    explanationText = "Small word fix: remove 'at' at the end of the question.",
                    fixCategory = "Small Word",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r06_prompt.mp3"),
                    confirmAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r06_confirm.mp3"),
                    fixAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r06_fix.mp3")
#endif
                },
                // Set 2 - Round 7
                new CommonMistakes_ReadingR01RoundData {
                    wobblySentence = "Have you been in London?",
                    words = new string[] { "Have", "you", "been", "in", "London?" },
                    wrongWordIndex = 3,
                    fixedWord = "to",
                    isRemoval = false,
                    repairedSentence = "Have you been to London?",
                    explanationText = "Preposition fix: use 'been to' when asking about visits.",
                    fixCategory = "Preposition",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r08_prompt.mp3"),
                    confirmAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r08_confirm.mp3"),
                    fixAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r08_fix.mp3")
#endif
                },
                // Set 2 - Round 8
                new CommonMistakes_ReadingR01RoundData {
                    wobblySentence = "We went at the mall.",
                    words = new string[] { "We", "went", "at", "the", "mall." },
                    wrongWordIndex = 2,
                    fixedWord = "to",
                    isRemoval = false,
                    repairedSentence = "We went to the mall.",
                    explanationText = "Preposition fix: use 'to' for destination.",
                    fixCategory = "Preposition",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r07_prompt.mp3"),
                    confirmAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r07_confirm.mp3"),
                    fixAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r07_fix.mp3")
#endif
                },
                // Set 2 - Round 9
                new CommonMistakes_ReadingR01RoundData {
                    wobblySentence = "I must to learn English.",
                    words = new string[] { "I", "must", "to", "learn", "English." },
                    wrongWordIndex = 2,
                    fixedWord = "",
                    isRemoval = true,
                    repairedSentence = "I must learn English.",
                    explanationText = "Small word fix: modal verb 'must' takes base verb directly without 'to'.",
                    fixCategory = "Small Word",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r11_prompt.mp3"),
                    confirmAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r11_confirm.mp3"),
                    fixAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r11_fix.mp3")
#endif
                },
                // Set 2 - Round 10
                new CommonMistakes_ReadingR01RoundData {
                    wobblySentence = "Does she drinks milk?",
                    words = new string[] { "Does", "she", "drinks", "milk?" },
                    wrongWordIndex = 2,
                    fixedWord = "drink",
                    isRemoval = false,
                    repairedSentence = "Does she drink milk?",
                    explanationText = "Verb fix: after 'Does', use base form 'drink'.",
                    fixCategory = "Verb Form",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r13_prompt.mp3"),
                    confirmAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r13_confirm.mp3"),
                    fixAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r13_fix.mp3")
#endif
                },
                // Set 2 - Round 11
                new CommonMistakes_ReadingR01RoundData {
                    wobblySentence = "My birthday is on January.",
                    words = new string[] { "My", "birthday", "is", "on", "January." },
                    wrongWordIndex = 3,
                    fixedWord = "in",
                    isRemoval = false,
                    repairedSentence = "My birthday is in January.",
                    explanationText = "Preposition fix: use 'in' for months without a specific date.",
                    fixCategory = "Preposition",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r10_prompt.mp3"),
                    confirmAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r10_confirm.mp3"),
                    fixAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r10_fix.mp3")
#endif
                },
                // Set 2 - Round 12
                new CommonMistakes_ReadingR01RoundData {
                    wobblySentence = "The office is in the first floor.",
                    words = new string[] { "The", "office", "is", "in", "the", "first", "floor." },
                    wrongWordIndex = 3,
                    fixedWord = "on",
                    isRemoval = false,
                    repairedSentence = "The office is on the first floor.",
                    explanationText = "Preposition fix: use 'on' for building floors.",
                    fixCategory = "Preposition",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r09_prompt.mp3"),
                    confirmAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r09_confirm.mp3"),
                    fixAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r01_r09_fix.mp3")
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
