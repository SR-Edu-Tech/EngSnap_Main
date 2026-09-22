using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit10 {



    /// <summary>
    /// Unit 10: Common Mistakes with Prepositions — Listening Lesson Two
    /// (L02 Which Drawer? — What Needs Swapping)
    /// 8 audio-to-error-type recognition rounds on the Listening Bench facing 4 drawers:
    /// [PREPOSITION] · [VERB FORM] · [WORD CHOICE] · [WORD ORDER].
    /// Pass threshold: 6 / 8 rounds.
    /// </summary>
    public class Masters_CommonMistakesWithPrepositions_Listening_LessonTwo : Masters_Lesson {

public enum CommonMistakes_DrawerCategory {
        Preposition = 0,
        VerbForm = 1,
        WordChoice = 2,
        WordOrder = 3
    }

    [System.Serializable]
    public class CommonMistakes_ListeningL02RoundData {
        public string wobblySentence;
        public CommonMistakes_DrawerCategory correctDrawer;
        public string repairedSentence;
        public string differingWord;
        public string explanationText;
        public AudioClip promptAudio;
        public AudioClip slowPromptAudio;
        public AudioClip confirmationAudio;
        public AudioClip wrongFeedbackAudio;
    }
    
        [Header("8 Drawer Error-Recognition Rounds")]
        [SerializeField] private CommonMistakes_ListeningL02RoundData[] rounds;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI promptSentenceTMP;
        [SerializeField] private TextMeshProUGUI speechBubbleTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;

        [Header("4 Drawer Option Buttons")]
        [SerializeField] private Button[] drawerButtons; // 0=Preposition, 1=Verb Form, 2=Word Choice, 3=Word Order

        [Header("Audio Controls")]
        [SerializeField] private Button replayAudioBtn;
        [SerializeField] private Toggle repeatThisToggle;
        [SerializeField] private Toggle slowToggle;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;

        [Header("Colors & Animation")]
        [SerializeField] private Color defaultDrawerColor = new Color(0.14f, 0.38f, 0.58f, 1f);
        [SerializeField] private Color correctColor = new Color(0.14f, 0.65f, 0.28f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.85f, 0.22f, 0.22f, 1f);

        private static readonly string[] DrawerNames = new string[] {
            "PREPOSITION",
            "VERB FORM",
            "WORD CHOICE",
            "WORD ORDER"
        };

        private int currentRoundIndex = 0;
        private int score = 0;
        private bool isProcessingInput = false;
        private bool isSlowMode = false;
        private bool isRepeatMode = false;

        private Image[] drawerImages;
        private TextMeshProUGUI[] drawerTexts;

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Listening;

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/10_CommonMistakesWithPrepositions/Listening/cm_l02_full_intro.mp3");
#endif
            }

            PurgeLegacyChildren();
            AutoBindReferences();
            EnsureHeaderAndTitle();
            EnsureNextAndBackButtonWired();

            if (rounds == null || rounds.Length == 0 || rounds[0] == null || rounds[0].promptAudio == null) {
                PopulateDefaultRounds();
            }

            InitDrawerButtons();
            EnsureDrawerLabels();
            WireEventListeners();

            if (resultPanel != null) resultPanel.SetActive(false);
        }

        protected override void Start() {
            EnsureNextAndBackButtonWired();
            EnsureAspectRatiosAndAnchorsPreserved();
            topic = Masters_Topic.Listening;

            currentRoundIndex = 0;
            score = 0;
            isProcessingInput = false;

            if (progressTMP != null) {
                progressTMP.text = $"Round 1/{(rounds != null ? rounds.Length : 8)}";
            }

            InitDrawerButtons();
            EnsureDrawerLabels();

            if (resultPanel != null) resultPanel.SetActive(false);

            StartCoroutine(InitializeLessonRoutine());
        }

        private IEnumerator InitializeLessonRoutine() {
            isProcessingInput = true;
            SetButtonsInteractable(false);

            if (progressTMP != null) {
                progressTMP.text = $"Round 1/{(rounds != null ? rounds.Length : 8)}";
            }

            InitDrawerButtons();
            EnsureDrawerLabels();

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/10_CommonMistakesWithPrepositions/Listening/cm_l02_full_intro.mp3");
#endif
            }

            if (promptSentenceTMP != null && rounds != null && rounds.Length > 0) {
                promptSentenceTMP.text = $"❌ \"{rounds[0].wobblySentence}\"";
                promptSentenceTMP.color = Color.white;
            }

            if (speechBubbleTMP != null && speechBubbleTMP != promptSentenceTMP) {
                speechBubbleTMP.text = "Look at the workshop drawers: Preposition, Verb Form, Word Choice, and Word Order.";
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
                yield return new WaitForSeconds(narratorSpeech.length + 0.3f);
            } else {
                yield return new WaitForSeconds(0.4f);
            }

            yield return StartCoroutine(StartRoundSequence(0));
        }

        private void EnsureHeaderAndTitle() {
            if (headerTMP == null) {
                Transform hTrans = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("UnitHeading/UnitTMP") ?? transform.Find("UnitHeading") ?? transform.Find("Branch");
                if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
            }
            if (headerTMP != null) {
                headerTMP.text = "COMMON MISTAKES WITH PREPOSITIONS 🔧";
            }

            if (titleTMP == null) {
                Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle");
                if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
            }
            if (titleTMP != null) {
                titleTMP.text = "L02 Which Drawer? — What Needs Swapping";
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
                "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
                "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition",
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

        private bool IsChildOfAnyButton(Transform t) {
            if (t == null) return false;
            Button[] allBtns = GetComponentsInChildren<Button>(true);
            foreach (var btn in allBtns) {
                if (btn != null && (t == btn.transform || t.IsChildOf(btn.transform))) return true;
            }
            return false;
        }

        private void AutoBindReferences() {
            Button[] btns = GetComponentsInChildren<Button>(true);
            List<Button> cardList = new List<Button>();

            // 1. Explicitly check for controls under Controls/ container
            Transform controlsTr = transform.Find("Controls") ?? transform.Find("PhraseCardsGrid/Controls") ?? transform.Find("AudioControls");
            if (controlsTr != null) {
                Button[] cBtns = controlsTr.GetComponentsInChildren<Button>(true);
                foreach (var b in cBtns) {
                    string bn = b.name.ToLower();
                    if (bn.Contains("repeat") || bn.Contains("replay") || bn.Contains("audio") || bn.Contains("sound")) {
                        replayAudioBtn = b;
                    }
                }
                Toggle[] cTogs = controlsTr.GetComponentsInChildren<Toggle>(true);
                foreach (var t in cTogs) {
                    string tn = t.name.ToLower();
                    if (tn.Contains("repeat") || tn.Contains("replay")) repeatThisToggle = t;
                    if (tn.Contains("slow")) slowToggle = t;
                }
            }

            foreach (var btn in btns) {
                string n = btn.gameObject.name.ToLower();
                if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("repeat") || n.Contains("audio") || n.Contains("speaker") || n.Contains("sound"))) {
                    if (btn != nextButton && !n.Contains("back") && !n.Contains("hub") && !n.Contains("option") && !n.Contains("card") && !n.Contains("drawer")) {
                        replayAudioBtn = btn;
                    }
                } else if (retryBtn == null && n.Contains("retry")) {
                    retryBtn = btn;
                } else if (n.Contains("option") || n.Contains("drawer") || n.Contains("chip") || n.Contains("card") || n.Contains("button_")) {
                    if (btn != nextButton && !n.Contains("back") && !n.Contains("hub") && !n.Contains("sound") && !n.Contains("repeat") && !n.Contains("slow") && !n.Contains("replay")) {
                        cardList.Add(btn);
                    }
                }
            }

            if (drawerButtons == null || drawerButtons.Length == 0) {
                if (cardList.Count >= 4) {
                    drawerButtons = cardList.GetRange(0, 4).ToArray();
                }
            }

            // If prompt or bubble was mistakenly wired to a button text, unbind it
            if (speechBubbleTMP != null && IsChildOfAnyButton(speechBubbleTMP.transform)) speechBubbleTMP = null;
            if (promptSentenceTMP != null && IsChildOfAnyButton(promptSentenceTMP.transform)) promptSentenceTMP = null;

            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps) {
                if (IsChildOfAnyButton(tmp.transform)) continue; // Never bind prompt or bubble to a button's text!

                string n = tmp.gameObject.name.ToLower();
                if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading") || n.Contains("unitheading"))) headerTMP = tmp;
                else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("unittmp") || n == "title")) titleTMP = tmp;
                else if (progressTMP == null && (n.Contains("progress") || n.Contains("counter") || n.Contains("expressioncount") || n.Contains("roundcount"))) progressTMP = tmp;
                else if (speechBubbleTMP == null && (n.Contains("bubble") || n.Contains("speech") || n.Contains("shopper") || n.Contains("prompt") || n.Contains("sentence") || n.Contains("statement"))) speechBubbleTMP = tmp;
                else if (promptSentenceTMP == null && (n.Contains("statement") || n.Contains("sentence") || n.Contains("question"))) promptSentenceTMP = tmp;
                else if (feedbackTMP == null && (n.Contains("feedback") || n.Contains("status") || n.Contains("explanation"))) feedbackTMP = tmp;
            }

            Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
            foreach (var t in toggles) {
                string n = t.gameObject.name.ToLower();
                if (slowToggle == null && n.Contains("slow")) slowToggle = t;
                else if (repeatThisToggle == null && n.Contains("repeat")) repeatThisToggle = t;
            }
        }

        private void InitDrawerButtons() {
            if (drawerButtons == null || drawerButtons.Length == 0) return;

            drawerImages = new Image[drawerButtons.Length];
            drawerTexts = new TextMeshProUGUI[drawerButtons.Length];

            for (int i = 0; i < drawerButtons.Length; i++) {
                if (drawerButtons[i] != null) {
                    int drawerIdx = i;
                    drawerButtons[i].gameObject.SetActive(true);
                    drawerImages[i] = drawerButtons[i].GetComponent<Image>();
                    drawerTexts[i] = drawerButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);

                    // Preserve exact Scene / Inspector layout (Do NOT override position, sizeDelta, or anchors in script!)

                    SetDrawerButtonLabel(i, (i < DrawerNames.Length) ? DrawerNames[i] : $"DRAWER {i + 1}");

                    drawerButtons[i].onClick.RemoveAllListeners();
                    drawerButtons[i].onClick.AddListener(() => OnDrawerChosen((CommonMistakes_DrawerCategory)drawerIdx, drawerIdx));
                }
            }
        }

        private void EnsureDrawerLabels() {
            if (drawerButtons == null) return;
            for (int i = 0; i < drawerButtons.Length; i++) {
                if (i < DrawerNames.Length) {
                    SetDrawerButtonLabel(i, DrawerNames[i]);
                }
            }
        }

        private void SetDrawerButtonLabel(int index, string labelText) {
            if (drawerButtons == null || index < 0 || index >= drawerButtons.Length || drawerButtons[index] == null) return;

            TextMeshProUGUI tmp = drawerButtons[index].GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null) {
                tmp.text = labelText;
                tmp.color = Color.white;
                tmp.enableAutoSizing = false;
                tmp.fontSize = 28f;
                tmp.fontStyle = FontStyles.Bold;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.enabled = true;
                tmp.gameObject.SetActive(true);
            }

            UnityEngine.UI.Text txt = drawerButtons[index].GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (txt != null) {
                txt.text = labelText;
                txt.color = Color.white;
                txt.fontSize = 24;
                txt.fontStyle = FontStyle.Bold;
                txt.alignment = TextAnchor.MiddleCenter;
                txt.enabled = true;
                txt.gameObject.SetActive(true);
            }
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

            // Wire all interactive elements inside Controls container directly
            Transform controlsTr = transform.Find("Controls") ?? transform.Find("PhraseCardsGrid/Controls") ?? transform.Find("AudioControls");
            if (controlsTr != null) {
                foreach (Transform child in controlsTr) {
                    string cn = child.name.ToLower();
                    if (cn.Contains("repeat") || cn.Contains("replay")) {
                        Button b = child.GetComponent<Button>() ?? child.GetComponentInChildren<Button>(true);
                        if (b != null) {
                            b.onClick.RemoveAllListeners();
                            b.onClick.AddListener(OnReplayAudioClicked);
                        }
                        Toggle t = child.GetComponent<Toggle>() ?? child.GetComponentInChildren<Toggle>(true);
                        if (t != null) {
                            t.onValueChanged.RemoveAllListeners();
                            t.onValueChanged.AddListener((isOn) => {
                                isRepeatMode = isOn;
                                OnReplayAudioClicked();
                            });
                        }
                    }
                    if (cn.Contains("slow")) {
                        Toggle t = child.GetComponent<Toggle>() ?? child.GetComponentInChildren<Toggle>(true);
                        if (t != null) {
                            t.onValueChanged.RemoveAllListeners();
                            t.onValueChanged.AddListener((isOn) => {
                                isSlowMode = isOn;
                                OnReplayAudioClicked();
                            });
                        }
                        Button b = child.GetComponent<Button>() ?? child.GetComponentInChildren<Button>(true);
                        if (b != null) {
                            b.onClick.RemoveAllListeners();
                            b.onClick.AddListener(() => {
                                isSlowMode = !isSlowMode;
                                OnReplayAudioClicked();
                            });
                        }
                    }
                }
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RestartLesson);
            }
        }

        public override void EnsureNextAndBackButtonWired() {
            base.EnsureNextAndBackButtonWired();

            Button[] allButtons = GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons) {
                if (b != null && b.name.ToLower().Contains("back")) {
                    b.gameObject.SetActive(true);
                    b.interactable = true;
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(OnBackButtonClicked);
                }
            }

            if (nextButton == null) {
                Transform t = transform.Find("NextButton") ?? transform.Find("CommonHUD/NextButton") ?? transform.Find("Next");
                if (t != null) nextButton = t.GetComponent<Button>() ?? t.GetComponentInChildren<Button>(true);
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
            }
        }

        protected override void OnNextButtonClicked() {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                Masters_AudioManager.Instance.StopVoiceOver();
            }

            if (currentRoundIndex < rounds.Length - 1) {
                StopAllCoroutines();
                StartCoroutine(StartRoundSequence(currentRoundIndex + 1));
            } else {
                topic = Masters_Topic.Listening;
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnLessonComplete(topic);
                }
            }
        }

        public void OnBackButtonClicked() {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                Masters_AudioManager.Instance.StopVoiceOver();
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnBackButtonClicked();
            }
        }

        private IEnumerator StartRoundSequence(int roundIndex) {
            if (rounds == null || roundIndex < 0 || roundIndex >= rounds.Length) yield break;

            currentRoundIndex = roundIndex;
            isProcessingInput = false;
            var r = rounds[currentRoundIndex];

            if (progressTMP != null) {
                progressTMP.text = $"Round {currentRoundIndex + 1}/{rounds.Length}";
            }

            if (promptSentenceTMP != null) {
                promptSentenceTMP.text = $"❌ \"{r.wobblySentence}\"";
                promptSentenceTMP.color = Color.white;
            } else if (speechBubbleTMP != null) {
                speechBubbleTMP.text = $"❌ \"{r.wobblySentence}\"";
                speechBubbleTMP.color = Color.white;
            }

            if (feedbackTMP != null) {
                feedbackTMP.text = "";
            }

            ResetDrawerVisuals();
            EnsureDrawerLabels();
            SetButtonsInteractable(true);

            // Play prompt audio
            AudioClip clipToPlay = (isSlowMode && r.slowPromptAudio != null) ? r.slowPromptAudio : r.promptAudio;
            if (clipToPlay != null) {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
                } else {
                    AudioSource src = GetComponent<AudioSource>();
                    if (src != null) {
                        src.clip = clipToPlay;
                        src.Play();
                    }
                }
            }
        }

        private void ResetDrawerVisuals() {
            if (drawerImages != null) {
                for (int i = 0; i < drawerImages.Length; i++) {
                    if (drawerImages[i] != null) {
                        drawerImages[i].color = defaultDrawerColor;
                        drawerImages[i].transform.localScale = Vector3.one;
                    }
                }
            }
        }

        private void SetButtonsInteractable(bool state) {
            if (drawerButtons != null) {
                for (int i = 0; i < drawerButtons.Length; i++) {
                    if (drawerButtons[i] != null) drawerButtons[i].interactable = state;
                }
            }
        }

        private void OnDrawerChosen(CommonMistakes_DrawerCategory chosenCategory, int buttonIndex) {
            if (isProcessingInput || currentRoundIndex >= rounds.Length) return;

            isProcessingInput = true;
            SetButtonsInteractable(false);

            var r = rounds[currentRoundIndex];
            bool isCorrect = (chosenCategory == r.correctDrawer);

            Image selectedImg = (drawerImages != null && buttonIndex < drawerImages.Length) ? drawerImages[buttonIndex] : null;
            int correctIndex = (int)r.correctDrawer;
            Image correctImg = (drawerImages != null && correctIndex < drawerImages.Length) ? drawerImages[correctIndex] : null;

            StartCoroutine(HandleAnswerFeedback(isCorrect, selectedImg, correctImg, r));
        }

        private IEnumerator HandleAnswerFeedback(bool isCorrect, Image selectedImg, Image correctImg, CommonMistakes_ListeningL02RoundData r) {
            if (isCorrect) {
                score++;

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                if (selectedImg != null) {
                    selectedImg.DOColor(correctColor, 0.2f);
                    selectedImg.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 8, 0.5f);
                }

                // Animate sentence repair on screen
                if (promptSentenceTMP != null) {
                    promptSentenceTMP.text = $"✅ \"{r.repairedSentence}\"";
                    promptSentenceTMP.color = correctColor;
                }

                if (speechBubbleTMP != null) {
                    speechBubbleTMP.text = $"✅ \"{r.repairedSentence}\"\n{r.explanationText}";
                }

                if (r.confirmationAudio != null) {
                    if (Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(r.confirmationAudio);
                    }
                    yield return new WaitForSeconds(r.confirmationAudio.length + 0.4f);
                } else {
                    yield return new WaitForSeconds(1.8f);
                }
            } else {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                if (selectedImg != null) {
                    selectedImg.DOColor(wrongColor, 0.2f);
                    selectedImg.transform.DOShakePosition(0.5f, new Vector3(12f, 0f, 0f), 15, 90f);
                }

                if (correctImg != null) {
                    correctImg.DOColor(correctColor, 0.3f);
                }

                if (speechBubbleTMP != null) {
                    speechBubbleTMP.text = $"Fix Drawer: {DrawerNames[(int)r.correctDrawer]}\n{r.explanationText}";
                }

                AudioClip wrongClip = (r.wrongFeedbackAudio != null) ? r.wrongFeedbackAudio : r.confirmationAudio;
                if (wrongClip != null) {
                    if (Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(wrongClip);
                    }
                    yield return new WaitForSeconds(wrongClip.length + 0.5f);
                } else {
                    yield return new WaitForSeconds(2.0f);
                }
            }

            if (isRepeatMode) {
                yield return StartCoroutine(StartRoundSequence(currentRoundIndex));
            } else {
                if (currentRoundIndex + 1 < rounds.Length) {
                    yield return StartCoroutine(StartRoundSequence(currentRoundIndex + 1));
                } else {
                    ShowResults();
                }
            }
        }

        public void OnReplayAudioClicked() {
            if (rounds == null || rounds.Length == 0 || currentRoundIndex < 0 || currentRoundIndex >= rounds.Length) return;

            var r = rounds[currentRoundIndex];
            AudioClip clipToPlay = (isSlowMode && r.slowPromptAudio != null) ? r.slowPromptAudio : r.promptAudio;

#if UNITY_EDITOR
            if (clipToPlay == null) {
                string audioDir = "Assets/Audio/2B/10_CommonMistakesWithPrepositions/Listening/";
                string slowDir = audioDir + "Slow/";
                string rName = $"cm_l02_r{(currentRoundIndex + 1):00}";
                clipToPlay = isSlowMode 
                    ? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + rName + "_prompt.mp3") 
                    : UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + rName + "_prompt.mp3");
            }
#endif

            if (clipToPlay != null) {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
                } else {
                    AudioSource src = GetComponent<AudioSource>();
                    if (src == null) src = gameObject.AddComponent<AudioSource>();
                    src.clip = clipToPlay;
                    src.Play();
                }
            }
        }

        private void ShowResults() {
            if (resultPanel != null) {
                resultPanel.SetActive(true);
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Score: {score} / {rounds.Length}";
            }

            bool passed = score >= 6;
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "Great Job! Unit 10 Listening L02 Completed!" : "Keep practicing! Try again!";
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
                topic = Masters_Topic.Listening;
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnLessonComplete(topic);
                }
            }
        }

        public void RestartLesson() {
            currentRoundIndex = 0;
            score = 0;
            if (resultPanel != null) resultPanel.SetActive(false);
            StartCoroutine(InitializeLessonRoutine());
        }

        public void PopulateDefaultRounds() {
            string audioDir = "Assets/Audio/2B/10_CommonMistakesWithPrepositions/Listening/";
            string slowDir = audioDir + "Slow/";

            rounds = new CommonMistakes_ListeningL02RoundData[] {
                // Round 1 (Preposition)
                new CommonMistakes_ListeningL02RoundData {
                    wobblySentence = "We arrived to the airport late.",
                    correctDrawer = CommonMistakes_DrawerCategory.Preposition,
                    repairedSentence = "We arrived at the airport late.",
                    differingWord = "to ➔ at",
                    explanationText = "Use 'at' for specific locations: arrived at the airport.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r01_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l02_r01_prompt.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r01_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r01_wrong.mp3")
#endif
                },
                // Round 2 (Verb Form)
                new CommonMistakes_ListeningL02RoundData {
                    wobblySentence = "She don't like spicy food.",
                    correctDrawer = CommonMistakes_DrawerCategory.VerbForm,
                    repairedSentence = "She doesn't like spicy food.",
                    differingWord = "don't ➔ doesn't",
                    explanationText = "Subject-verb agreement: with 'she', use 'doesn't'.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r02_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l02_r02_prompt.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r02_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r02_wrong.mp3")
#endif
                },
                // Round 3 (Preposition)
                new CommonMistakes_ListeningL02RoundData {
                    wobblySentence = "Can you explain me the rules?",
                    correctDrawer = CommonMistakes_DrawerCategory.Preposition,
                    repairedSentence = "Can you explain the rules to me?",
                    differingWord = "explain me ➔ explain to me",
                    explanationText = "'Explain' requires 'to someone': explain to me.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r03_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l02_r03_prompt.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r03_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r03_wrong.mp3")
#endif
                },
                // Round 4 (Word Order)
                new CommonMistakes_ListeningL02RoundData {
                    wobblySentence = "Why she is leaving early?",
                    correctDrawer = CommonMistakes_DrawerCategory.WordOrder,
                    repairedSentence = "Why is she leaving early?",
                    differingWord = "she is ➔ is she",
                    explanationText = "In questions, the auxiliary verb comes before the subject: 'Why is she...'.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r04_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l02_r04_prompt.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r04_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r04_wrong.mp3")
#endif
                },
                // Round 5 (Word Choice)
                new CommonMistakes_ListeningL02RoundData {
                    wobblySentence = "He did a big mistake yesterday.",
                    correctDrawer = CommonMistakes_DrawerCategory.WordChoice,
                    repairedSentence = "He made a big mistake yesterday.",
                    differingWord = "did ➔ made",
                    explanationText = "Collocation: we 'make a mistake', not 'do a mistake'.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r05_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l02_r05_prompt.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r05_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r05_wrong.mp3")
#endif
                },
                // Round 6 (Verb Form)
                new CommonMistakes_ListeningL02RoundData {
                    wobblySentence = "I look forward to meet you.",
                    correctDrawer = CommonMistakes_DrawerCategory.VerbForm,
                    repairedSentence = "I look forward to meeting you.",
                    differingWord = "meet ➔ meeting",
                    explanationText = "After 'look forward to', use the -ing verb form: 'meeting'.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r06_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l02_r06_prompt.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r06_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r06_wrong.mp3")
#endif
                },
                // Round 7 (Preposition)
                new CommonMistakes_ListeningL02RoundData {
                    wobblySentence = "She is married with a doctor.",
                    correctDrawer = CommonMistakes_DrawerCategory.Preposition,
                    repairedSentence = "She is married to a doctor.",
                    differingWord = "married with ➔ married to",
                    explanationText = "In English, we say 'married to someone', not 'married with'.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r07_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l02_r07_prompt.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r07_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r07_wrong.mp3")
#endif
                },
                // Round 8 (Word Order)
                new CommonMistakes_ListeningL02RoundData {
                    wobblySentence = "Where you bought that jacket?",
                    correctDrawer = CommonMistakes_DrawerCategory.WordOrder,
                    repairedSentence = "Where did you buy that jacket?",
                    differingWord = "you bought ➔ did you buy",
                    explanationText = "In past questions, use 'did + subject + base verb': 'Where did you buy...'.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r08_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l02_r08_prompt.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r08_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l02_r08_wrong.mp3")
#endif
                }
            };
        }
    }
}
