using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit10 {


    /// <summary>
    /// Q01 Inspection Room Quiz — Common Mistakes
    /// 12 mixed-format questions covering prepositions, verb forms, word choice, word order, and small words.
    /// Pass mark: 9 / 12.
    /// </summary>
    public class Masters_CommonMistakesWithPrepositions_Quiz_LessonOne : Masters_Lesson {

[System.Serializable]
    public class CommonMistakesQuizQuestionData {
        public string questionText;
        public string correctOption;
        public string[] distractorOptions;
        public AudioClip questionAudio;
    }
    
        [Header("Q01 12 Mixed-Format Questions")]
        [SerializeField] private CommonMistakesQuizQuestionData[] questions;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI questionTextTMP;

        [Header("4 Option Buttons")]
        [SerializeField] private Button[] optionButtons; // Up to 4 buttons

        [Header("Audio Controls")]
        [SerializeField] private Button replayAudioBtn;

        [Header("Audio References")]
        [SerializeField] private AudioClip introAudio;
        [SerializeField] private AudioClip celebrationAudio;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleTMP;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;
        [SerializeField] private Button returnHubBtn;

        [Header("Editor Preview")]
        [Range(0, 11)]
        public int editorPreviewRound = 0;

        [Header("Colors & Styling")]
        [SerializeField] private Color defaultChipColor = new Color(0.14f, 0.38f, 0.58f, 1f);
        [SerializeField] private Color correctColor = new Color(0.14f, 0.65f, 0.28f, 1f);
        [SerializeField] private Color wrongColor = new Color(0.85f, 0.22f, 0.22f, 1f);

        private int currentQuestionIndex = 0;
        private int score = 0;
        private bool isAnswering = false;
        private int currentCorrectOptionIndex = 0;
        private const int PASS_MARK = 9;
        private Coroutine introCoroutine;

        protected override void Awake() {
            topic = Masters_Topic.Quiz;
            base.Awake();

            AutoBindReferences();
            InitQuestionsIfEmpty();
            EnsureHeaderAndTitle();
            WireEventListeners();
        }

        protected override void Start() {
            topic = Masters_Topic.Quiz;
            base.Start();
            AutoBindReferences();
            InitQuestionsIfEmpty();
            EnsureHeaderAndTitle();
            WireEventListeners();
            EnsureNextAndBackButtonWired();
            RestartQuiz();
        }

        private void OnValidate() {
            if (!Application.isPlaying) {
                UpdateEditorPreview();
            }
        }

        [ContextMenu("Update Editor Preview")]
        public void UpdateEditorPreview() {
            AutoBindReferences();
            InitQuestionsIfEmpty();
            EnsureHeaderAndTitle();

            if (questions == null || questions.Length == 0) return;

            int idx = Mathf.Clamp(editorPreviewRound, 0, questions.Length - 1);
            CommonMistakesQuizQuestionData q = questions[idx];

            if (progressTMP != null) {
                progressTMP.text = $"Question {idx + 1}/12";
                progressTMP.enableAutoSizing = false;
                progressTMP.fontSize = 30f;
            }

            if (questionTextTMP != null) {
                questionTextTMP.text = q.questionText;
                questionTextTMP.color = Color.white;
                questionTextTMP.enableAutoSizing = false;
                questionTextTMP.fontSize = 30f;
                questionTextTMP.alignment = TextAlignmentOptions.Center;
                questionTextTMP.enableWordWrapping = true;
            }

            if (optionButtons != null && optionButtons.Length >= 4) {
                string[] previewChoices = new string[4];
                previewChoices[0] = q.correctOption;
                previewChoices[1] = (q.distractorOptions != null && q.distractorOptions.Length > 0) ? q.distractorOptions[0] : "Option B";
                previewChoices[2] = (q.distractorOptions != null && q.distractorOptions.Length > 1) ? q.distractorOptions[1] : "Option C";
                previewChoices[3] = (q.distractorOptions != null && q.distractorOptions.Length > 2) ? q.distractorOptions[2] : "Option D";

                for (int i = 0; i < 4; i++) {
                    if (optionButtons[i] != null) {
                        optionButtons[i].gameObject.SetActive(true);
                        var txt = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                        if (txt != null) {
                            txt.text = previewChoices[i];
                            txt.color = Color.white;
                            txt.enableAutoSizing = true;
                            txt.fontSizeMin = 20f;
                            txt.fontSizeMax = 28f;
                            txt.alignment = TextAlignmentOptions.Center;
                            txt.enableWordWrapping = true;
                            txt.margin = new Vector4(12, 6, 12, 6);
                        }
                    }
                }
            }
        }

        private void AutoBindReferences() {
            Transform hud = transform.Find("CommonHUD") ?? transform;
            if (nextButton == null) {
                Transform nTr = hud.Find("NextButton") ?? transform.Find("NextButton");
                if (nTr != null) nextButton = nTr.GetComponent<Button>();
            }

            Transform headerTr = transform.Find("HeaderContainer");
            if (headerTr != null) {
                if (headerTMP == null) {
                    Transform h = headerTr.Find("Header") ?? headerTr.Find("Branch");
                    if (h != null) headerTMP = h.GetComponent<TextMeshProUGUI>();
                }
                if (titleTMP == null) {
                    Transform t = headerTr.Find("LessonTitle") ?? headerTr.Find("Title");
                    if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
                }
                if (progressTMP == null) {
                    Transform p = headerTr.Find("ProgressTMP") ?? headerTr.Find("Progress") ?? headerTr.Find("ScoreTMP");
                    if (p != null) progressTMP = p.GetComponent<TextMeshProUGUI>();
                }
            }

            if (questionTextTMP == null) {
                Transform qTr = transform.Find("QuestionCard/QuestionTMP") 
                             ?? transform.Find("QuestionCard/Text") 
                             ?? transform.Find("QuestionPanel/QuestionText")
                             ?? transform.Find("QuestionTMP");
                if (qTr != null) questionTextTMP = qTr.GetComponent<TextMeshProUGUI>();
            }

            if (optionButtons == null || optionButtons.Length == 0) {
                List<Button> bList = new List<Button>();
                Transform optContainer = transform.Find("OptionsContainer") 
                                      ?? transform.Find("OptionCardsContainer") 
                                      ?? transform.Find("ButtonsContainer")
                                      ?? transform.Find("QuizButtons");
                if (optContainer != null) {
                    Button[] all = optContainer.GetComponentsInChildren<Button>(true);
                    foreach (var b in all) {
                        string bn = b.name.ToLower();
                        if (!bn.Contains("next") && !bn.Contains("back") && !bn.Contains("replay")) {
                            bList.Add(b);
                        }
                    }
                }
                if (bList.Count > 0) optionButtons = bList.ToArray();
            }

            if (replayAudioBtn == null) {
                Transform rTr = transform.Find("AudioReplayButton") ?? transform.Find("ReplayButton") ?? transform.Find("HeaderContainer/ReplayBtn") ?? transform.Find("Audio/ReplayAudioButton");
                if (rTr != null) replayAudioBtn = rTr.GetComponent<Button>();
            }

            if (resultPanel == null) {
                Transform rp = transform.Find("ResultPanel") ?? transform.Find("CompletedPanel") ?? transform.Find("ResultsPanel");
                if (rp != null) {
                    resultPanel = rp.gameObject;
                    Transform rTitle = rp.Find("ResultTitle") ?? rp.Find("Title");
                    if (rTitle != null) resultTitleTMP = rTitle.GetComponent<TextMeshProUGUI>();

                    Transform rSc = rp.Find("ResultScore") ?? rp.Find("ScoreTMP");
                    if (rSc != null) resultScoreTMP = rSc.GetComponent<TextMeshProUGUI>();

                    Transform rSt = rp.Find("ResultStatus") ?? rp.Find("StatusTMP");
                    if (rSt != null) resultStatusTMP = rSt.GetComponent<TextMeshProUGUI>();

                    Transform rBtn = rp.Find("RetryButton") ?? rp.Find("RetryBtn");
                    if (rBtn != null) retryBtn = rBtn.GetComponent<Button>();

                    Transform hBtn = rp.Find("ReturnHubButton") ?? rp.Find("NextButton");
                    if (hBtn != null) returnHubBtn = hBtn.GetComponent<Button>();
                }
            }
        }

        private void EnsureHeaderAndTitle() {
            if (headerTMP != null) {
                headerTMP.gameObject.SetActive(true);
                headerTMP.text = "COMMON MISTAKES WITH PREPOSITIONS";
                headerTMP.enableAutoSizing = false;
                headerTMP.fontSize = 32f;
            }
            if (titleTMP != null) {
                titleTMP.gameObject.SetActive(true);
                titleTMP.text = "Q01 Inspection Room Quiz — Common Mistakes";
                titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
                titleTMP.enableAutoSizing = false;
                titleTMP.fontSize = 34f;
                titleTMP.fontStyle = FontStyles.Bold;
                titleTMP.alignment = TextAlignmentOptions.Center;
                titleTMP.enableWordWrapping = false;
                RectTransform rt = titleTMP.GetComponent<RectTransform>();
                if (rt != null) rt.sizeDelta = new Vector2(1000f, 60f);
            }
            if (progressTMP != null) {
                progressTMP.gameObject.SetActive(true);
                int total = (questions != null && questions.Length > 0) ? questions.Length : 12;
                progressTMP.text = $"Question {Mathf.Min(currentQuestionIndex + 1, total)}/{total}";
                progressTMP.enableAutoSizing = false;
                progressTMP.fontSize = 30f;
            }
        }

        private void WireEventListeners() {
            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    int optIdx = i;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(optIdx));
                }
            }

            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayCurrentQuestionAudio);
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RestartQuiz);
            }

            if (returnHubBtn != null) {
                returnHubBtn.onClick.RemoveAllListeners();
                returnHubBtn.onClick.AddListener(OnReturnToHub);
            }

            if (nextButton != null) {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
            }
        }

        public void RestartQuiz() {
            currentQuestionIndex = 0;
            score = 0;
            isAnswering = false;

            if (introCoroutine != null) StopCoroutine(introCoroutine);

            if (resultPanel != null) resultPanel.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            EnsureHeaderAndTitle();
            UpdateHUD();

            introCoroutine = StartCoroutine(PlayIntroThenStart());
        }

        private IEnumerator PlayIntroThenStart() {
            EnableOptionButtons(false);
            // Show Question 1 immediately on screen without playing question audio yet
            ShowQuestion(0, playAudio: false);

            AudioClip clip = introAudio != null ? introAudio : narratorSpeech;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
                yield return new WaitForSeconds(clip.length + 0.25f);
            } else {
                yield return new WaitForSeconds(0.4f);
            }

            EnableOptionButtons(true);
            PlayCurrentQuestionAudio();
        }

        private void EnableOptionButtons(bool enable) {
            if (optionButtons != null) {
                foreach (var b in optionButtons) {
                    if (b != null) b.interactable = enable;
                }
            }
        }

        private void UpdateHUD() {
            int total = (questions != null && questions.Length > 0) ? questions.Length : 12;
            if (progressTMP != null) {
                progressTMP.text = $"Question {Mathf.Min(currentQuestionIndex + 1, total)}/{total}";
                progressTMP.enableAutoSizing = false;
                progressTMP.fontSize = 30f;
            }
        }

        private void ShowQuestion(int index, bool playAudio = true) {
            if (questions == null || index < 0 || index >= questions.Length) {
                EndQuiz();
                return;
            }

            currentQuestionIndex = index;
            isAnswering = false;
            UpdateHUD();

            CommonMistakesQuizQuestionData q = questions[currentQuestionIndex];

            if (questionTextTMP != null) {
                questionTextTMP.text = q.questionText;
                questionTextTMP.color = Color.white;
                questionTextTMP.enableAutoSizing = false;
                questionTextTMP.fontSize = 30f;
                questionTextTMP.alignment = TextAlignmentOptions.Center;
                questionTextTMP.enableWordWrapping = true;
                questionTextTMP.ForceMeshUpdate();
            }

            // Build choices list
            List<string> choices = new List<string>();
            choices.Add(q.correctOption);
            if (q.distractorOptions != null) {
                foreach (var d in q.distractorOptions) {
                    if (!string.IsNullOrEmpty(d)) choices.Add(d);
                }
            }

            // Shuffle choices
            for (int i = choices.Count - 1; i > 0; i--) {
                int r = Random.Range(0, i + 1);
                string tmp = choices[i];
                choices[i] = choices[r];
                choices[r] = tmp;
            }

            currentCorrectOptionIndex = choices.IndexOf(q.correctOption);

            // Populate option buttons
            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (i < choices.Count) {
                        optionButtons[i].gameObject.SetActive(true);
                        optionButtons[i].interactable = true;

                        Image img = optionButtons[i].GetComponent<Image>();
                        if (img != null) {
                            img.color = defaultChipColor;
                            img.transform.localScale = Vector3.one;
                        }

                        TextMeshProUGUI t = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                        if (t != null) {
                            t.text = choices[i];
                            t.color = Color.white;
                            t.enableAutoSizing = true;
                            t.fontSizeMin = 20f;
                            t.fontSizeMax = 28f;
                            t.alignment = TextAlignmentOptions.Center;
                            t.enableWordWrapping = true;
                            t.margin = new Vector4(12, 6, 12, 6);
                            t.ForceMeshUpdate();
                        }
                    } else {
                        optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }

            // Play Question Audio if requested
            if (playAudio) {
                PlayCurrentQuestionAudio();
            }
        }

        private void PlayCurrentQuestionAudio() {
            if (questions != null && currentQuestionIndex >= 0 && currentQuestionIndex < questions.Length) {
                AudioClip clip = questions[currentQuestionIndex].questionAudio;
                if (clip != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(clip);
                }
            }
        }

        private void OnOptionSelected(int optionIndex) {
            if (isAnswering) return;
            isAnswering = true;

            EnableOptionButtons(false);

            StartCoroutine(HandleAnswerCoroutine(optionIndex));
        }

        private IEnumerator HandleAnswerCoroutine(int selectedIndex) {
            bool isCorrect = (selectedIndex == currentCorrectOptionIndex);

            if (isCorrect) {
                score++;
                UpdateHUD();

                if (optionButtons != null && selectedIndex < optionButtons.Length && optionButtons[selectedIndex] != null) {
                    Image img = optionButtons[selectedIndex].GetComponent<Image>();
                    if (img != null) {
                        img.color = correctColor;
                        img.transform.DOPunchScale(Vector3.one * 0.1f, 0.25f);
                    }
                }
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }
            } else {
                if (optionButtons != null && selectedIndex < optionButtons.Length && optionButtons[selectedIndex] != null) {
                    Image img = optionButtons[selectedIndex].GetComponent<Image>();
                    if (img != null) {
                        img.color = wrongColor;
                        img.transform.DOShakePosition(0.4f, 8f, 15, 90, false, true);
                    }
                }
                // Also highlight correct answer in green
                if (optionButtons != null && currentCorrectOptionIndex < optionButtons.Length && optionButtons[currentCorrectOptionIndex] != null) {
                    Image cImg = optionButtons[currentCorrectOptionIndex].GetComponent<Image>();
                    if (cImg != null) cImg.color = correctColor;
                }
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }
            }

            yield return new WaitForSeconds(1.6f);

            currentQuestionIndex++;
            if (currentQuestionIndex < questions.Length) {
                ShowQuestion(currentQuestionIndex, playAudio: true);
            } else {
                EndQuiz();
            }
        }

        public void ReplayCurrentQuestionAudio() {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            PlayCurrentQuestionAudio();
        }

        private void EndQuiz() {
            int total = (questions != null) ? questions.Length : 12;
            bool passed = (score >= PASS_MARK);

            if (optionButtons != null) {
                foreach (var b in optionButtons) if (b != null) b.gameObject.SetActive(false);
            }

            if (resultPanel != null) {
                resultPanel.SetActive(true);
                resultPanel.transform.DOKill();
                resultPanel.transform.localScale = Vector3.zero;
                resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

                if (resultTitleTMP != null) {
                    resultTitleTMP.text = passed ? "QUIZ CLEARED!" : "TRY AGAIN!";
                    resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
                }
                if (resultScoreTMP != null) {
                    resultScoreTMP.text = $"Score: {score} / {total}";
                }
                if (resultStatusTMP != null) {
                    resultStatusTMP.text = passed 
                        ? "Outstanding work! You passed the Inspection Room Quiz and unlocked the Reward!" 
                        : $"You scored {score}/{total}. Pass mark is {PASS_MARK}/12. Tap Retry to try again!";
                }
                if (returnHubBtn != null) returnHubBtn.gameObject.SetActive(passed);
                if (retryBtn != null) retryBtn.gameObject.SetActive(!passed || score < total);
            }

            if (passed) {
                if (celebrationAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(celebrationAudio);
                }
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }
                if (nextButton != null) {
                    nextButton.gameObject.SetActive(true);
                    nextButton.interactable = true;
                }
            } else {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }
            }
        }

        protected override void OnNextButtonClicked() {
            if (topic == Masters_Topic.None) topic = Masters_Topic.Quiz;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                Masters_AudioManager.Instance.StopVoiceOver();
            }

            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }

        private void OnReturnToHub() {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                Masters_AudioManager.Instance.StopVoiceOver();
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnBackButtonClicked();
            }
        }

        public void InitQuestionsIfEmpty() {
            string audioDir = "Assets/Audio/2B/10_CommonMistakesWithPrepositions/Quiz/";

            if (introAudio == null) {
#if UNITY_EDITOR
                introAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_q01_intro.mp3");
#endif
                if (narratorSpeech == null) narratorSpeech = introAudio;
            }

            if (celebrationAudio == null) {
#if UNITY_EDITOR
                celebrationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_q01_celebration.mp3");
#endif
            }

            if (questions != null && questions.Length == 12) return;

            questions = new CommonMistakesQuizQuestionData[] {
                new CommonMistakesQuizQuestionData {
                    questionText = "'It depends from you.' — which word is loose?",
                    correctOption = "from",
                    distractorOptions = new string[] { "depends", "you", "It" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_q01_q01.mp3")
#endif
                },
                new CommonMistakesQuizQuestionData {
                    questionText = "'The office is in the first floor.' — which word is loose?",
                    correctOption = "in",
                    distractorOptions = new string[] { "office", "first", "floor" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_q01_q02.mp3")
#endif
                },
                new CommonMistakesQuizQuestionData {
                    questionText = "Which runs smoothly?",
                    correctOption = "I am going home.",
                    distractorOptions = new string[] { "I am going to home.", "I go to home." },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_q01_q03.mp3")
#endif
                },
                new CommonMistakesQuizQuestionData {
                    questionText = "Which runs smoothly?",
                    correctOption = "Does she drink milk?",
                    distractorOptions = new string[] { "Does she drinks milk?", "Does she drank milk?" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_q01_q04.mp3")
#endif
                },
                new CommonMistakesQuizQuestionData {
                    questionText = "Have you been _____ London?",
                    correctOption = "to",
                    distractorOptions = new string[] { "in", "at", "on" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_q01_q05.mp3")
#endif
                },
                new CommonMistakesQuizQuestionData {
                    questionText = "My birthday is _____ January.",
                    correctOption = "in",
                    distractorOptions = new string[] { "on", "at", "to" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_q01_q06.mp3")
#endif
                },
                new CommonMistakesQuizQuestionData {
                    questionText = "Mend: 'He speaks English very good.'",
                    correctOption = "He speaks English very well.",
                    distractorOptions = new string[] { "He speaks English very good.", "He speak English very well." },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_q01_q07.mp3")
#endif
                },
                new CommonMistakesQuizQuestionData {
                    questionText = "Mend: 'I have 26 years'",
                    correctOption = "I'm 26 years old.",
                    distractorOptions = new string[] { "I have 26 years old.", "I am 26 years." },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_q01_q08.mp3")
#endif
                },
                new CommonMistakesQuizQuestionData {
                    questionText = "Put it right: 'Where has gone Sunil?'",
                    correctOption = "Where has Sunil gone?",
                    distractorOptions = new string[] { "Where has gone Sunil?", "Where Sunil has gone?" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_q01_q09.mp3")
#endif
                },
                new CommonMistakesQuizQuestionData {
                    questionText = "Which is right?",
                    correctOption = "I don't wear a watch.",
                    distractorOptions = new string[] { "I don't use a watch.", "I don't put a watch." },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_q01_q10.mp3")
#endif
                },
                new CommonMistakesQuizQuestionData {
                    questionText = "Mend: 'We have not money left.'",
                    correctOption = "We have no money left.",
                    distractorOptions = new string[] { "We have not money left.", "We no have money left." },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_q01_q11.mp3")
#endif
                },
                new CommonMistakesQuizQuestionData {
                    questionText = "True or False: 'She has left five years ago.' is correct.",
                    correctOption = "False — She left five years ago.",
                    distractorOptions = new string[] { "True — She has left five years ago." },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_q01_q12.mp3")
#endif
                }
            };
        }
    }
}

