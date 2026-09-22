using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit10 {

    

    /// <summary>
    /// Unit 10: Common Mistakes with Prepositions — Listening Lesson One
    /// (L01 Hear It — Which One Runs Smoothly?)
    /// 10 audio A/B discrimination rounds on the Listening Bench.
    /// Student listens to Version A and Version B and taps the version that runs smoothly (the grammatically correct sentence).
    /// Pass threshold: 8 / 10 rounds.
    /// </summary>
    public class Masters_CommonMistakesWithPrepositions_Listening_LessonOne : Masters_Lesson {

[System.Serializable]
    public class CommonMistakes_ListeningL01RoundData {
        public string sentenceAText;
        public string sentenceBText;
        public bool isOptionACorrect;
        public string differingWord;
        public string explanationText;
        public AudioClip promptAudio;
        public AudioClip slowPromptAudio;
        public AudioClip audioA;
        public AudioClip audioB;
        public AudioClip confirmationAudio;
        public AudioClip wrongFeedbackAudio;
    }
    
        [Header("10 A/B Discrimination Rounds")]
        [SerializeField] private CommonMistakes_ListeningL01RoundData[] rounds;

        [Header("UI Display References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI speechBubbleTMP;
        [SerializeField] private TextMeshProUGUI feedbackTMP;

        [Header("Two Sentence Parts A & B (Workbench Buttons)")]
        [SerializeField] private Button optionABtn;
        [SerializeField] private Button optionBBtn;
        [SerializeField] private Button[] extraOptionButtons; // For prefabs that have 4 buttons in a grid

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
        [SerializeField] private Color defaultCardColor = new Color(0.14f, 0.38f, 0.58f, 1f);
        [SerializeField] private Color correctColor = new Color(0.14f, 0.65f, 0.28f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.85f, 0.22f, 0.22f, 1f);

        private int currentRoundIndex = 0;
        private int score = 0;
        private bool isProcessingInput = false;
        private bool isSlowMode = false;
        private bool isRepeatMode = false;

        private Image optionAImg;
        private Image optionBImg;
        private TextMeshProUGUI optionATMP;
        private TextMeshProUGUI optionBTMP;

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Listening;

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/10_CommonMistakesWithPrepositions/Listening/cm_l01_full_intro.mp3");
#endif
            }

            PurgeLegacyChildren();
            AutoBindReferences();
            EnsureHeaderAndTitle();
            EnsureNextAndBackButtonWired();

            if (rounds == null || rounds.Length == 0) {
                PopulateDefaultRounds();
            }

            InitOptionButtons();
            WireEventListeners();
        }

        protected override void Start() {
            // Note: We deliberately avoid calling base.Start() to prevent duplicate immediate playback of narratorSpeech.
            EnsureNextAndBackButtonWired();
            EnsureAspectRatiosAndAnchorsPreserved();
            topic = Masters_Topic.Listening;

            currentRoundIndex = 0;
            score = 0;
            isProcessingInput = false;

            if (resultPanel != null) resultPanel.SetActive(false);

            StartCoroutine(InitializeLessonRoutine());
        }

        private IEnumerator InitializeLessonRoutine() {
            isProcessingInput = true;
            SetButtonsInteractable(false);

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/10_CommonMistakesWithPrepositions/Listening/cm_l01_full_intro.mp3");
#endif
            }

            // 1. Play full Introduction Audio before Round 1 starts
            if (speechBubbleTMP != null) {
                speechBubbleTMP.text = "Listen carefully — which one runs smoothly?";
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

            // 2. Start Round 1
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
                titleTMP.text = "L01 Hear It — Which One Runs Smoothly?";
                titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
                titleTMP.enableAutoSizing = false;
                titleTMP.fontSize = 40f;
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

        private void AutoBindReferences() {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var tmp in tmps) {
                string n = tmp.gameObject.name.ToLower();
                if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading") || n.Contains("unitheading"))) headerTMP = tmp;
                else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("unittmp") || n == "title")) titleTMP = tmp;
                else if (progressTMP == null && (n.Contains("progress") || n.Contains("counter") || n.Contains("expressioncount") || n.Contains("roundcount"))) progressTMP = tmp;
                else if (speechBubbleTMP == null && (n.Contains("bubble") || n.Contains("speech") || n.Contains("prompt") || n.Contains("shopper"))) speechBubbleTMP = tmp;
                else if (feedbackTMP == null && (n.Contains("feedback") || n.Contains("status") || n.Contains("explanation"))) feedbackTMP = tmp;
            }

            Button[] btns = GetComponentsInChildren<Button>(true);
            List<Button> optionList = new List<Button>();

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
                    if (btn != nextButton && !n.Contains("back") && !n.Contains("hub") && !n.Contains("option") && !n.Contains("card")) {
                        replayAudioBtn = btn;
                    }
                } else if (retryBtn == null && n.Contains("retry")) {
                    retryBtn = btn;
                } else if (n.Contains("option") || n.Contains("button") || n.Contains("card") || n.Contains("part")) {
                    if (btn != nextButton && !n.Contains("back") && !n.Contains("hub") && !n.Contains("sound") && !n.Contains("repeat") && !n.Contains("slow") && !n.Contains("replay")) {
                        optionList.Add(btn);
                    }
                }
            }

            if (optionList.Count >= 2) {
                optionABtn = optionList[0];
                optionBBtn = optionList[1];

                // Deactivate extra option buttons if any (from 4-button templates)
                if (optionList.Count > 2) {
                    for (int i = 2; i < optionList.Count; i++) {
                        optionList[i].gameObject.SetActive(false);
                    }
                }
            }

            Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
            foreach (var t in toggles) {
                string n = t.gameObject.name.ToLower();
                if (slowToggle == null && n.Contains("slow")) slowToggle = t;
                else if (repeatThisToggle == null && n.Contains("repeat")) repeatThisToggle = t;
            }
        }

        private void InitOptionButtons() {
            if (optionABtn != null) {
                optionAImg = optionABtn.GetComponent<Image>();
                optionATMP = optionABtn.GetComponentInChildren<TextMeshProUGUI>(true);
                optionABtn.onClick.RemoveAllListeners();
                optionABtn.onClick.AddListener(() => OnOptionChosen(true));
            }

            if (optionBBtn != null) {
                optionBImg = optionBBtn.GetComponent<Image>();
                optionBTMP = optionBBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                optionBBtn.onClick.RemoveAllListeners();
                optionBBtn.onClick.AddListener(() => OnOptionChosen(false));
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

            // Explicitly ensure all BackButtons in hierarchy route through OnBackButtonClicked
            Button[] allButtons = GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons) {
                if (b != null && b.name.ToLower().Contains("back")) {
                    b.gameObject.SetActive(true);
                    b.interactable = true;
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(OnBackButtonClicked);
                }
            }

            // Wire NextButton
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
                progressTMP.text = $"Part {currentRoundIndex + 1}/{rounds.Length}";
            }

            if (speechBubbleTMP != null) {
                speechBubbleTMP.text = "Listen carefully — which one runs smoothly?";
            }

            if (feedbackTMP != null) {
                feedbackTMP.text = "";
            }

            // Set button texts
            if (optionATMP != null) optionATMP.text = $"<b>A:</b> {r.sentenceAText}";
            if (optionBTMP != null) optionBTMP.text = $"<b>B:</b> {r.sentenceBText}";

            // Reset button colors and interactive states
            ResetButtonVisuals();
            SetButtonsInteractable(true);

            // Play prompt audio
            AudioClip clipToPlay = (isSlowMode && r.slowPromptAudio != null) ? r.slowPromptAudio : r.promptAudio;
            if (clipToPlay != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            }
        }

        private void ResetButtonVisuals() {
            if (optionAImg != null) {
                optionAImg.color = defaultCardColor;
                optionAImg.transform.localScale = Vector3.one;
            }
            if (optionBImg != null) {
                optionBImg.color = defaultCardColor;
                optionBImg.transform.localScale = Vector3.one;
            }
        }

        private void SetButtonsInteractable(bool state) {
            if (optionABtn != null) optionABtn.interactable = state;
            if (optionBBtn != null) optionBBtn.interactable = state;
        }

        private void OnOptionChosen(bool isOptionA) {
            if (isProcessingInput || currentRoundIndex >= rounds.Length) return;

            isProcessingInput = true;
            SetButtonsInteractable(false);

            var r = rounds[currentRoundIndex];
            bool isCorrect = (isOptionA == r.isOptionACorrect);

            Button selectedBtn = isOptionA ? optionABtn : optionBBtn;
            Image selectedImg = isOptionA ? optionAImg : optionBImg;
            Button otherBtn = isOptionA ? optionBBtn : optionABtn;
            Image otherImg = isOptionA ? optionBImg : optionAImg;

            StartCoroutine(HandleAnswerFeedback(isCorrect, selectedBtn, selectedImg, otherBtn, otherImg, r));
        }

        private IEnumerator HandleAnswerFeedback(bool isCorrect, Button selectedBtn, Image selectedImg, Button otherBtn, Image otherImg, CommonMistakes_ListeningL01RoundData r) {
            if (isCorrect) {
                score++;

                // Play Positive Correct SFX
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                if (selectedImg != null) {
                    selectedImg.DOColor(correctColor, 0.2f);
                    selectedImg.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f, 8, 0.5f);
                }

                if (speechBubbleTMP != null) {
                    speechBubbleTMP.text = $"✅ {r.explanationText}";
                }

                if (r.confirmationAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(r.confirmationAudio);
                    yield return new WaitForSeconds(r.confirmationAudio.length + 0.3f);
                } else {
                    yield return new WaitForSeconds(1.5f);
                }
            } else {
                // Play Negative Incorrect SFX
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                if (selectedImg != null) {
                    selectedImg.DOColor(wrongColor, 0.2f);
                    selectedImg.transform.DOShakePosition(0.5f, new Vector3(12f, 0f, 0f), 15, 90f);
                }

                if (otherImg != null) {
                    otherImg.DOColor(correctColor, 0.3f);
                }

                if (speechBubbleTMP != null) {
                    speechBubbleTMP.text = $"Fix: {r.differingWord}\n{r.explanationText}";
                }

                // Play dedicated WRONG feedback audio
                AudioClip wrongClip = (r.wrongFeedbackAudio != null) ? r.wrongFeedbackAudio : r.confirmationAudio;
                if (wrongClip != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(wrongClip);
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
                string rName = $"cm_l01_r{(currentRoundIndex + 1):00}";
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

            bool passed = score >= 8;
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "Great Job! Unit 10 Listening Completed!" : "Keep practicing! Try again!";
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

            rounds = new CommonMistakes_ListeningL01RoundData[] {
                // Round 1 (at -> to)
                new CommonMistakes_ListeningL01RoundData {
                    sentenceAText = "We went at the mall.",
                    sentenceBText = "We went to the mall.",
                    isOptionACorrect = false,
                    differingWord = "at ➔ to",
                    explanationText = "Use 'to' for movement towards a place: We went to the mall.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r01_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l01_r01_prompt.mp3"),
                    audioA = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r01_opt_a.mp3"),
                    audioB = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r01_opt_b.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r01_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r01_wrong.mp3")
#endif
                },
                // Round 2 (from -> on)
                new CommonMistakes_ListeningL01RoundData {
                    sentenceAText = "It depends on the weather.",
                    sentenceBText = "It depends from the weather.",
                    isOptionACorrect = true,
                    differingWord = "from ➔ on",
                    explanationText = "In English, we always say 'depends on': It depends on the weather.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r02_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l01_r02_prompt.mp3"),
                    audioA = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r02_opt_a.mp3"),
                    audioB = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r02_opt_b.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r02_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r02_wrong.mp3")
#endif
                },
                // Round 3 (in -> at)
                new CommonMistakes_ListeningL01RoundData {
                    sentenceAText = "She is good in English.",
                    sentenceBText = "She is good at English.",
                    isOptionACorrect = false,
                    differingWord = "in ➔ at",
                    explanationText = "For abilities, we say 'good at': She is good at English.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r03_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l01_r03_prompt.mp3"),
                    audioA = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r03_opt_a.mp3"),
                    audioB = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r03_opt_b.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r03_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r03_wrong.mp3")
#endif
                },
                // Round 4 (listening -> listening to)
                new CommonMistakes_ListeningL01RoundData {
                    sentenceAText = "I am listening to the music.",
                    sentenceBText = "I am listening the music.",
                    isOptionACorrect = true,
                    differingWord = "listening ➔ listening to",
                    explanationText = "'Listen' requires 'to': I am listening to the music.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r04_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l01_r04_prompt.mp3"),
                    audioA = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r04_opt_a.mp3"),
                    audioB = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r04_opt_b.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r04_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r04_wrong.mp3")
#endif
                },
                // Round 5 (to -> at)
                new CommonMistakes_ListeningL01RoundData {
                    sentenceAText = "He arrived to the station on time.",
                    sentenceBText = "He arrived at the station on time.",
                    isOptionACorrect = false,
                    differingWord = "to ➔ at",
                    explanationText = "For stations or places, we say 'arrived at': He arrived at the station on time.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r05_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l01_r05_prompt.mp3"),
                    audioA = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r05_opt_a.mp3"),
                    audioB = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r05_opt_b.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r05_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r05_wrong.mp3")
#endif
                },
                // Round 6 (to -> with)
                new CommonMistakes_ListeningL01RoundData {
                    sentenceAText = "I agree with you.",
                    sentenceBText = "I agree to you.",
                    isOptionACorrect = true,
                    differingWord = "to ➔ with",
                    explanationText = "When agreeing with a person, say 'agree with': I agree with you.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r06_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l01_r06_prompt.mp3"),
                    audioA = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r06_opt_a.mp3"),
                    audioB = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r06_opt_b.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r06_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r06_wrong.mp3")
#endif
                },
                // Round 7 (with -> to)
                new CommonMistakes_ListeningL01RoundData {
                    sentenceAText = "She is married with a doctor.",
                    sentenceBText = "She is married to a doctor.",
                    isOptionACorrect = false,
                    differingWord = "with ➔ to",
                    explanationText = "In English, we say 'married to': She is married to a doctor.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r07_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l01_r07_prompt.mp3"),
                    audioA = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r07_opt_a.mp3"),
                    audioB = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r07_opt_b.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r07_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r07_wrong.mp3")
#endif
                },
                // Round 8 (on -> in)
                new CommonMistakes_ListeningL01RoundData {
                    sentenceAText = "I am interested in this project.",
                    sentenceBText = "I am interested on this project.",
                    isOptionACorrect = true,
                    differingWord = "on ➔ in",
                    explanationText = "Say 'interested in': I am interested in this project.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r08_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l01_r08_prompt.mp3"),
                    audioA = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r08_opt_a.mp3"),
                    audioB = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r08_opt_b.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r08_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r08_wrong.mp3")
#endif
                },
                // Round 9 (from -> of)
                new CommonMistakes_ListeningL01RoundData {
                    sentenceAText = "He is afraid from spiders.",
                    sentenceBText = "He is afraid of spiders.",
                    isOptionACorrect = false,
                    differingWord = "from ➔ of",
                    explanationText = "We say 'afraid of': He is afraid of spiders.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r09_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l01_r09_prompt.mp3"),
                    audioA = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r09_opt_a.mp3"),
                    audioB = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r09_opt_b.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r09_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r09_wrong.mp3")
#endif
                },
                // Round 10 (for -> on)
                new CommonMistakes_ListeningL01RoundData {
                    sentenceAText = "They congratulated him on his success.",
                    sentenceBText = "They congratulated him for his success.",
                    isOptionACorrect = true,
                    differingWord = "for ➔ on",
                    explanationText = "We congratulate someone 'on' their success: They congratulated him on his success.",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r10_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "cm_l01_r10_prompt.mp3"),
                    audioA = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r10_opt_a.mp3"),
                    audioB = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r10_opt_b.mp3"),
                    confirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r10_confirm.mp3"),
                    wrongFeedbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_l01_r10_wrong.mp3")
#endif
                }
            };
        }
    }
}
