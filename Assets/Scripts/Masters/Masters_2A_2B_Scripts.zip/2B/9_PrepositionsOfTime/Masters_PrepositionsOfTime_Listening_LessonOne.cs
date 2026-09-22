using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit9 {

    

    /// <summary>
    /// Unit 9: Prepositions of Time — Listening Lesson One (L01: Hear It — IN, ON or AT?)
    /// 10 audio-to-preposition recognition rounds.
    /// Student listens to the time expression and taps the preposition (IN, ON, AT) that belongs with it.
    /// Pass mark = 8 / 10.
    /// </summary>
    public class Masters_PrepositionsOfTime_Listening_LessonOne : Masters_Lesson {

[System.Serializable]
    public class TimePrepositionRoundData {
        public string timeExpressionText;          // e.g. "January", "Tuesday", "4 o'clock"
        public string correctPreposition;          // "IN", "ON", or "AT"
        public AudioClip promptAudio;              // Prompt audio
        public AudioClip slowPromptAudio;          // Slow prompt audio
        public AudioClip ariaConfirmationAudio;    // "In January!", "On Tuesday!"
    }
    
        [Header("10 Time Expression Rounds")]
        [SerializeField]
        private TimePrepositionRoundData[] rounds;

        [Header("UI References")]
        [SerializeField]
        private TextMeshProUGUI headerTMP;
        [SerializeField]
        private TextMeshProUGUI titleTMP;
        [SerializeField]
        private TextMeshProUGUI progressTMP;
        [SerializeField]
        private TextMeshProUGUI promptTextTMP;

        [Header("3 Preposition Chip Buttons (IN, ON, AT)")]
        [SerializeField]
        private Button[] chipButtons; // 3 buttons for IN, ON, AT

        [Header("Audio Controls")]
        [SerializeField]
        private Button replayAudioBtn;
        [SerializeField]
        private Toggle repeatThisToggle;
        [SerializeField]
        private Toggle slowToggle;

        [Header("Results & Retry Panel")]
        [SerializeField]
        private GameObject resultPanel;
        [SerializeField]
        private TextMeshProUGUI resultScoreTMP;
        [SerializeField]
        private TextMeshProUGUI resultStatusTMP;
        [SerializeField]
        private Button retryBtn;
        [SerializeField]
        private Button returnHubBtn;

        [Header("Editor Preview")]
        [Range(0, 9)]
        public int editorPreviewRound = 0;

        [Header("Colors & Styling")]
        [SerializeField]
        private Color defaultChipColor = new Color(0.14f, 0.38f, 0.58f, 1f);
        [SerializeField]
        private Color correctColor = new Color(0.14f, 0.65f, 0.28f, 1f);
        [SerializeField]
        private Color wrongColor = new Color(0.85f, 0.22f, 0.22f, 1f);

        private int currentRoundIndex = 0;
        private int score = 0;
        private bool isProcessingInput = false;
        private bool isSlowMode = false;

        private Image[] chipImages;
        private TextMeshProUGUI[] chipTexts;
        private readonly string[] prepositions = new string[] { "IN", "ON", "AT" };

        protected override void Awake() {
            topic = Masters_Topic.Listening;
            base.Awake();

            PurgeLegacyChildren();
            AutoBindReferences();
            EnsureHeaderAndTitle();

            if (rounds == null || rounds.Length == 0) {
                PopulateDefaultRounds();
            }

            InitChipButtons();
            WireEventListeners();

            if (resultPanel != null) {
                resultPanel.SetActive(false);
            }
        }

        protected override void Start() {
            topic = Masters_Topic.Listening;
            base.Start();

            PurgeLegacyChildren();
            AutoBindReferences();
            EnsureHeaderAndTitle();

            if (rounds == null || rounds.Length == 0) {
                PopulateDefaultRounds();
            }

            currentRoundIndex = 0;
            score = 0;
            isProcessingInput = false;

            if (nextButton != null) {
                nextButton.gameObject.SetActive(false);
            }

            StartCoroutine(BeginFirstRoundAfterIntro());
        }

        private void OnValidate() {
            if (!Application.isPlaying) {
                UpdateEditorPreview();
            }
        }

        [ContextMenu("Update Editor Preview")]
        public void UpdateEditorPreview() {
            AutoBindReferences();
            EnsureHeaderAndTitle();

            if (rounds == null || rounds.Length == 0) {
                PopulateDefaultRounds();
            }

            if (rounds == null || rounds.Length == 0) return;

            int idx = Mathf.Clamp(editorPreviewRound, 0, rounds.Length - 1);
            TimePrepositionRoundData r = rounds[idx];

            if (progressTMP != null) {
                progressTMP.text = $"Round {idx + 1}/{rounds.Length}";
                progressTMP.enableAutoSizing = false;
                progressTMP.fontSize = 30f;
            }

            if (promptTextTMP != null) {
                promptTextTMP.text = $"\"{r.timeExpressionText}\"";
                promptTextTMP.color = Color.white;
                promptTextTMP.enableAutoSizing = false;
                promptTextTMP.fontSize = 32f;
                promptTextTMP.alignment = TextAlignmentOptions.Center;
            }

            if (chipButtons != null) {
                for (int i = 0; i < chipButtons.Length; i++) {
                    if (chipButtons[i] != null && i < prepositions.Length) {
                        var txt = chipButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                        if (txt != null) {
                            txt.text = prepositions[i];
                            txt.color = Color.white;
                            txt.enableAutoSizing = false;
                            txt.fontSize = 28f;
                            txt.fontStyle = FontStyles.Bold;
                            txt.alignment = TextAlignmentOptions.Center;
                        }
                    }
                }
            }
        }

        private void PurgeLegacyChildren() {
            string[] legacyNames = new string[] {
                "Cloud", "Cloud (1)", "Cloud (2)", "Cloud (3)", "Cloud (4)",
                "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
                "OptionButtonContainer"
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

        private void EnsureHeaderAndTitle() {
            if (headerTMP == null) {
                Transform hTrans = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("UnitHeading") ?? transform.Find("Branch");
                if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
            }
            if (headerTMP != null) {
                headerTMP.text = "PREPOSITIONS OF TIME ⏰";
            }

            if (titleTMP == null) {
                Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle");
                if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
            }
            if (titleTMP != null) {
                titleTMP.text = "L01 Hear It — IN, ON or AT?";
                titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
                titleTMP.enableAutoSizing = false;
                titleTMP.fontSize = 40f;
                titleTMP.fontStyle = FontStyles.Bold;
                titleTMP.alignment = TextAlignmentOptions.Center;
            }

            if (progressTMP == null) {
                Transform pTrans = transform.Find("HeaderContainer/Progress") ?? transform.Find("Progress");
                if (pTrans != null) progressTMP = pTrans.GetComponent<TextMeshProUGUI>();
            }
            if (progressTMP != null) {
                progressTMP.text = $"Round {currentRoundIndex + 1}/10";
                progressTMP.enableAutoSizing = false;
                progressTMP.fontSize = 30f;
            }
        }

        private void AutoBindReferences() {
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (TextMeshProUGUI tmp in tmps) {
                string n = tmp.gameObject.name.ToLower();
                if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading") || n.Contains("unitheading"))) headerTMP = tmp;
                else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("unittmp") || n == "title")) titleTMP = tmp;
                else if (progressTMP == null && (n.Contains("progress") || n.Contains("counter") || n.Contains("roundcount"))) progressTMP = tmp;
                else if (promptTextTMP == null && (n.Contains("prompt") || n.Contains("statement") || n.Contains("question") || n.Contains("word") || n.Contains("expression"))) promptTextTMP = tmp;
            }

            Button[] buttons = GetComponentsInChildren<Button>(true);
            List<Button> chips = new List<Button>();
            foreach (Button btn in buttons) {
                string n = btn.gameObject.name.ToLower();
                if (n.Contains("option") || n.Contains("chip") || n.Contains("button_0") || n.Contains("card")) {
                    chips.Add(btn);
                } else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("speaker") || n.Contains("audio"))) {
                    replayAudioBtn = btn;
                } else if (retryBtn == null && n.Contains("retry")) {
                    retryBtn = btn;
                } else if (returnHubBtn == null && (n.Contains("return") || n.Contains("hub"))) {
                    returnHubBtn = btn;
                } else if (nextButton == null && n == "nextbutton") {
                    nextButton = btn;
                }
            }

            if ((chipButtons == null || chipButtons.Length < 3) && chips.Count >= 3) {
                chipButtons = chips.GetRange(0, 3).ToArray();
            }

            Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
            foreach (Toggle tog in toggles) {
                string n = tog.gameObject.name.ToLower();
                if (repeatThisToggle == null && n.Contains("repeat")) repeatThisToggle = tog;
                else if (slowToggle == null && n.Contains("slow")) slowToggle = tog;
            }

            if (resultPanel == null) {
                Transform resTrans = transform.Find("ResultPanel") ?? transform.Find("CompletionPanel");
                if (resTrans != null) resultPanel = resTrans.gameObject;
            }
        }

        private void InitChipButtons() {
            if (chipButtons == null || chipButtons.Length == 0) return;

            chipImages = new Image[chipButtons.Length];
            chipTexts = new TextMeshProUGUI[chipButtons.Length];

            for (int i = 0; i < chipButtons.Length; i++) {
                if (chipButtons[i] != null) {
                    int chipIdx = i;
                    chipImages[i] = chipButtons[i].GetComponent<Image>();
                    chipTexts[i] = chipButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);

                    if (chipTexts[i] != null && i < prepositions.Length) {
                        chipTexts[i].text = prepositions[i];
                        chipTexts[i].color = Color.white;
                        chipTexts[i].enableAutoSizing = false;
                        chipTexts[i].fontSize = 28f;
                        chipTexts[i].fontStyle = FontStyles.Bold;
                        chipTexts[i].alignment = TextAlignmentOptions.Center;
                    }

                    chipButtons[i].onClick.RemoveAllListeners();
                    chipButtons[i].onClick.AddListener(() => OnPrepositionChipClicked(chipIdx));
                }
            }
        }

        private void WireEventListeners() {
            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
            }

            if (repeatThisToggle != null) {
                repeatThisToggle.onValueChanged.RemoveAllListeners();
                repeatThisToggle.onValueChanged.AddListener((isOn) => {
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

            if (returnHubBtn != null) {
                returnHubBtn.onClick.RemoveAllListeners();
                returnHubBtn.onClick.AddListener(OnReturnHubClicked);
            }

            if (nextButton != null) {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnReturnHubClicked);
            }
        }

        private IEnumerator BeginFirstRoundAfterIntro() {
            EnableChipButtons(false);

            AudioClip clip = narratorSpeech;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
                yield return new WaitForSeconds(clip.length + 0.3f);
            } else {
                yield return new WaitForSeconds(0.4f);
            }

            PlayCurrentRound();
        }

        public void PlayCurrentRound() {
            if (rounds == null || rounds.Length == 0) return;

            currentRoundIndex = Mathf.Clamp(currentRoundIndex, 0, rounds.Length - 1);
            isProcessingInput = false;

            EnsureHeaderAndTitle();
            ResetChipVisuals();
            EnableChipButtons(true);

            TimePrepositionRoundData r = rounds[currentRoundIndex];

            if (promptTextTMP != null) {
                promptTextTMP.text = $"\"{r.timeExpressionText}\"";
                promptTextTMP.color = Color.white;
            }

            PlayCurrentRoundAudio();
        }

        private void ResetChipVisuals() {
            if (chipImages != null) {
                for (int i = 0; i < chipImages.Length; i++) {
                    if (chipImages[i] != null) {
                        chipImages[i].color = defaultChipColor;
                        chipImages[i].transform.localScale = Vector3.one;
                    }
                }
            }
        }

        private void OnPrepositionChipClicked(int clickedIndex) {
            if (isProcessingInput) return;
            if (rounds == null || currentRoundIndex >= rounds.Length) return;
            if (clickedIndex < 0 || clickedIndex >= prepositions.Length) return;

            string selectedPrep = prepositions[clickedIndex];
            TimePrepositionRoundData r = rounds[currentRoundIndex];

            bool isCorrect = (selectedPrep.Trim().ToUpper() == r.correctPreposition.Trim().ToUpper());
            StartCoroutine(HandleRoundEvaluation(isCorrect, clickedIndex));
        }

        private IEnumerator HandleRoundEvaluation(bool isCorrect, int clickedIndex) {
            isProcessingInput = true;
            EnableChipButtons(false);

            TimePrepositionRoundData r = rounds[currentRoundIndex];

            if (isCorrect) {
                score++;
                EnsureHeaderAndTitle();

                if (chipImages != null && clickedIndex < chipImages.Length && chipImages[clickedIndex] != null) {
                    chipImages[clickedIndex].color = correctColor;
                    chipImages[clickedIndex].transform.DOPunchScale(Vector3.one * 0.15f, 0.25f);
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                if (r.ariaConfirmationAudio != null && Masters_AudioManager.Instance != null) {
                    yield return new WaitForSeconds(0.3f);
                    Masters_AudioManager.Instance.PlayVoiceOver(r.ariaConfirmationAudio);
                    yield return new WaitForSeconds(r.ariaConfirmationAudio.length + 0.3f);
                } else {
                    yield return new WaitForSeconds(1.3f);
                }

                if (currentRoundIndex + 1 < rounds.Length) {
                    currentRoundIndex++;
                    PlayCurrentRound();
                } else {
                    EndLesson();
                }
            } else {
                if (chipImages != null && clickedIndex < chipImages.Length && chipImages[clickedIndex] != null) {
                    chipImages[clickedIndex].color = wrongColor;
                    chipImages[clickedIndex].transform.DOShakePosition(0.4f, 8f, 15, 90, false, true);
                }

                // Highlight the correct preposition in green as teaching feedback
                int correctIdx = System.Array.IndexOf(prepositions, r.correctPreposition.Trim().ToUpper());
                if (correctIdx >= 0 && chipImages != null && correctIdx < chipImages.Length && chipImages[correctIdx] != null) {
                    chipImages[correctIdx].color = correctColor;
                }

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                yield return new WaitForSeconds(1.6f);

                if (currentRoundIndex + 1 < rounds.Length) {
                    currentRoundIndex++;
                    PlayCurrentRound();
                } else {
                    EndLesson();
                }
            }
        }

        public void PlayCurrentRoundAudio() {
            if (rounds == null || currentRoundIndex >= rounds.Length) return;
            TimePrepositionRoundData r = rounds[currentRoundIndex];

            AudioClip clip = (isSlowMode && r.slowPromptAudio != null) ? r.slowPromptAudio : r.promptAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }

        public void OnReplayAudioClicked() {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            PlayCurrentRoundAudio();
        }

        private void EnableChipButtons(bool enable) {
            if (chipButtons == null) return;
            for (int i = 0; i < chipButtons.Length; i++) {
                if (chipButtons[i] != null) chipButtons[i].interactable = enable;
            }
        }

        public void RestartLesson() {
            currentRoundIndex = 0;
            score = 0;
            isProcessingInput = false;

            if (resultPanel != null) resultPanel.SetActive(false);

            PlayCurrentRound();
        }

        private void EndLesson() {
            isProcessingInput = false;
            bool passed = (score >= 8);

            if (resultPanel != null) {
                resultPanel.SetActive(true);
                resultPanel.transform.DOKill();
                resultPanel.transform.localScale = Vector3.zero;
                resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

                if (resultScoreTMP != null) {
                    resultScoreTMP.text = $"You scored {score}/10! (Pass mark: 8/10)";
                }

                if (resultStatusTMP != null) {
                    resultStatusTMP.text = passed
                        ? "Outstanding! You mastered prepositions of time: IN for stretches, ON for days, AT for moments!"
                        : "Keep practicing! Listen carefully to the time expression and try again!";
                }

                if (returnHubBtn != null) returnHubBtn.gameObject.SetActive(passed);
                if (retryBtn != null) retryBtn.gameObject.SetActive(!passed || score < 10);
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(passed);
            }

            if (passed && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
        }

        public void OnReturnHubClicked() {
            topic = Masters_Topic.Listening;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }

        public void PopulateDefaultRounds() {
            string audioDir = "Assets/Audio/2B/9_PrepositionsOfTime/Listening/";

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_intro.mp3");
#endif
            }

            rounds = new TimePrepositionRoundData[] {
                // Round 1: January -> IN
                new TimePrepositionRoundData {
                    timeExpressionText = "January",
                    correctPreposition = "IN",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r01_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l01_r01_prompt.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r01_confirm.mp3")
#endif
                },
                // Round 2: Tuesday -> ON
                new TimePrepositionRoundData {
                    timeExpressionText = "Tuesday",
                    correctPreposition = "ON",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r02_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l01_r02_prompt.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r02_confirm.mp3")
#endif
                },
                // Round 3: Four o'clock -> AT
                new TimePrepositionRoundData {
                    timeExpressionText = "Four o'clock",
                    correctPreposition = "AT",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r03_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l01_r03_prompt.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r03_confirm.mp3")
#endif
                },
                // Round 4: Spring -> IN
                new TimePrepositionRoundData {
                    timeExpressionText = "Spring",
                    correctPreposition = "IN",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r04_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l01_r04_prompt.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r04_confirm.mp3")
#endif
                },
                // Round 5: Christmas Day -> ON
                new TimePrepositionRoundData {
                    timeExpressionText = "Christmas Day",
                    correctPreposition = "ON",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r05_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l01_r05_prompt.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r05_confirm.mp3")
#endif
                },
                // Round 6: Noon -> AT
                new TimePrepositionRoundData {
                    timeExpressionText = "Noon",
                    correctPreposition = "AT",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r06_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l01_r06_prompt.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r06_confirm.mp3")
#endif
                },
                // Round 7: Nineteen forty-seven -> IN
                new TimePrepositionRoundData {
                    timeExpressionText = "1947",
                    correctPreposition = "IN",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r07_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l01_r07_prompt.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r07_confirm.mp3")
#endif
                },
                // Round 8: The tenth of August -> ON
                new TimePrepositionRoundData {
                    timeExpressionText = "10th of August",
                    correctPreposition = "ON",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r08_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l01_r08_prompt.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r08_confirm.mp3")
#endif
                },
                // Round 9: Dinner time -> AT
                new TimePrepositionRoundData {
                    timeExpressionText = "Dinner time",
                    correctPreposition = "AT",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r09_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l01_r09_prompt.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r09_confirm.mp3")
#endif
                },
                // Round 10: The Fourth of July -> ON
                new TimePrepositionRoundData {
                    timeExpressionText = "Fourth of July",
                    correctPreposition = "ON",
#if UNITY_EDITOR
                    promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r10_prompt.mp3"),
                    slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/pot_l01_r10_prompt.mp3"),
                    ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "pot_l01_r10_confirm.mp3")
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
