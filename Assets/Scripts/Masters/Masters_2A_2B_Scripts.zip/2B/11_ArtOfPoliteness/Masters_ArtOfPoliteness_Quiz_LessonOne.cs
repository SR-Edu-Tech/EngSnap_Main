using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit11 {


    /// <summary>
    /// Q01 Display Cabinet Quiz — Art of Politeness
    /// 12 mixed-format questions covering the polite pairs, softening words, polishing moves, and situation choices.
    /// Pass mark: 9 / 12.
    /// </summary>
    public class Masters_ArtOfPoliteness_Quiz_LessonOne : Masters_Lesson {

[System.Serializable]
    public class ArtOfPolitenessQuizQuestionData {
        public string questionText;
        public string correctOption;
        public string[] distractorOptions;
        public AudioClip questionAudio;
    }
    
        [Header("Q01 12 Mixed-Format Questions")]
        [SerializeField] private ArtOfPolitenessQuizQuestionData[] questions;

        [Header("UI References")]
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI progressTMP;
        [SerializeField] private TextMeshProUGUI questionTextTMP;

        [Header("Option Buttons")]
        [SerializeField] private Button[] optionButtons;

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

            if (questions != null && questions.Length > 0 && editorPreviewRound >= 0 && editorPreviewRound < questions.Length) {
                var q = questions[editorPreviewRound];
                if (questionTextTMP != null) questionTextTMP.text = q.questionText;
                if (progressTMP != null) progressTMP.text = $"Question: {editorPreviewRound + 1}/{questions.Length}";

                List<string> previewOpts = new List<string>();
                previewOpts.Add(q.correctOption);
                if (q.distractorOptions != null) previewOpts.AddRange(q.distractorOptions);

                if (optionButtons != null) {
                    for (int i = 0; i < optionButtons.Length; i++) {
                        if (optionButtons[i] != null) {
                            if (i < previewOpts.Count) {
                                optionButtons[i].gameObject.SetActive(true);
                                var tmp = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                                if (tmp != null) tmp.text = previewOpts[i];
                            } else {
                                optionButtons[i].gameObject.SetActive(false);
                            }
                        }
                    }
                }
            }
        }

        public override void EnsureNextAndBackButtonWired() {
            base.EnsureNextAndBackButtonWired();
            if (nextButton != null) {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
            }
            if (backButton != null) {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        private void AutoBindReferences() {
            EnsureHeaderAndTitle();

            Transform hud = transform.Find("CommonHUD") ?? transform;
            if (backButton == null) {
                Transform bTr = hud.Find("BackButton") ?? transform.Find("BackButton");
                if (bTr != null) backButton = bTr.GetComponent<Button>();
            }
            if (nextButton == null) {
                Transform nTr = hud.Find("NextButton") ?? transform.Find("NextButton");
                if (nTr != null) nextButton = nTr.GetComponent<Button>();
            }

            Transform headerTr = transform.Find("HeaderContainer");
            if (headerTr != null) {
                if (progressTMP == null) {
                    Transform p = headerTr.Find("ProgressTMP") ?? headerTr.Find("Progress");
                    if (p != null) progressTMP = p.GetComponent<TextMeshProUGUI>();
                }
            }

            if (questionTextTMP == null) {
                Transform qTr = transform.Find("QuestionContainer/QuestionText") ?? transform.Find("QuestionPanel/QuestionText") ?? transform.Find("QuestionText") ?? transform.Find("Question");
                if (qTr != null) questionTextTMP = qTr.GetComponent<TextMeshProUGUI>();
            }

            if (optionButtons == null || optionButtons.Length == 0) {
                Transform optPanel = transform.Find("OptionsContainer") ?? transform.Find("OptionButtons") ?? transform.Find("OptionsPanel");
                if (optPanel != null) {
                    Button[] btns = optPanel.GetComponentsInChildren<Button>(true);
                    List<Button> filtered = new List<Button>();
                    foreach (var b in btns) {
                        if (b != backButton && b != nextButton && b != retryBtn && b != returnHubBtn && b != replayAudioBtn) {
                            filtered.Add(b);
                        }
                    }
                    optionButtons = filtered.ToArray();
                }
            }

            if (replayAudioBtn == null) {
                Transform rTr = transform.Find("AudioReplayButton") ?? transform.Find("ReplayButton") ?? transform.Find("HeaderContainer/ReplayBtn");
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
            if (headerTMP == null) {
                Transform hTr = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("Branch");
                if (hTr != null) headerTMP = hTr.GetComponent<TextMeshProUGUI>();
            }
            if (headerTMP != null) {
                headerTMP.text = "THE ART OF POLITENESS";
            }

            if (titleTMP == null) {
                Transform tTr = transform.Find("HeaderContainer/LessonTitle") ?? transform.Find("LessonTitle") ?? transform.Find("HeaderContainer/Title") ?? transform.Find("Title");
                if (tTr != null) titleTMP = tTr.GetComponent<TextMeshProUGUI>();
            }
            if (titleTMP != null) {
                titleTMP.text = "Q01 Display Cabinet Quiz — Art of Politeness";
            }
        }

        private void WireEventListeners() {
            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    int optIdx = i;
                    if (optionButtons[i] != null) {
                        optionButtons[i].onClick.RemoveAllListeners();
                        optionButtons[i].onClick.AddListener(() => OnOptionChosen(optIdx));
                    }
                }
            }

            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(ReplayQuestionAudio);
            }

            if (retryBtn != null) {
                retryBtn.onClick.RemoveAllListeners();
                retryBtn.onClick.AddListener(RestartQuiz);
            }

            if (returnHubBtn != null) {
                returnHubBtn.onClick.RemoveAllListeners();
                returnHubBtn.onClick.AddListener(OnNextButtonClicked);
            }
        }

        public void RestartQuiz() {
            currentQuestionIndex = 0;
            score = 0;
            isAnswering = false;

            if (resultPanel != null) resultPanel.SetActive(false);
            if (nextButton != null) nextButton.gameObject.SetActive(false);

            if (introCoroutine != null) StopCoroutine(introCoroutine);
            introCoroutine = StartCoroutine(PlayIntroThenStartCoroutine());
        }

        private IEnumerator PlayIntroThenStartCoroutine() {
            SetOptionsInteractable(false);

            AudioClip introClip = (introAudio != null) ? introAudio : narratorSpeech;
            if (introClip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(introClip);
                yield return new WaitForSeconds(introClip.length + 0.3f);
            } else {
                yield return new WaitForSeconds(0.4f);
            }

            DisplayQuestion(0);
        }

        private void DisplayQuestion(int index) {
            if (questions == null || index < 0 || index >= questions.Length) {
                EndQuiz();
                return;
            }

            currentQuestionIndex = index;
            isAnswering = false;
            var q = questions[index];

            if (progressTMP != null) {
                progressTMP.text = $"Question: {index + 1}/{questions.Length}";
            }

            if (questionTextTMP != null) {
                questionTextTMP.text = q.questionText;
                questionTextTMP.transform.DOPunchScale(Vector3.one * 0.05f, 0.2f);
            }

            // Build options list
            List<string> optionsList = new List<string>();
            optionsList.Add(q.correctOption);
            if (q.distractorOptions != null) {
                optionsList.AddRange(q.distractorOptions);
            }

            // Shuffle options
            int n = optionsList.Count;
            int[] perm = new int[n];
            for (int i = 0; i < n; i++) perm[i] = i;
            for (int i = 0; i < n; i++) {
                int r = Random.Range(i, n);
                int temp = perm[i];
                perm[i] = perm[r];
                perm[r] = temp;
            }

            currentCorrectOptionIndex = -1;
            string[] shuffled = new string[n];
            for (int i = 0; i < n; i++) {
                shuffled[i] = optionsList[perm[i]];
                if (perm[i] == 0) currentCorrectOptionIndex = i;
            }

            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (optionButtons[i] != null) {
                        if (i < shuffled.Length) {
                            optionButtons[i].gameObject.SetActive(true);
                            optionButtons[i].interactable = true;

                            var img = optionButtons[i].GetComponent<Image>();
                            if (img != null) img.color = defaultChipColor;

                            var tmp = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                            if (tmp != null) {
                                tmp.text = shuffled[i];
                                tmp.color = Color.white;
                            }
                        } else {
                            optionButtons[i].gameObject.SetActive(false);
                        }
                    }
                }
            }

            if (q.questionAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(q.questionAudio);
            }
        }

        private void OnOptionChosen(int chosenIndex) {
            if (isAnswering || questions == null || currentQuestionIndex >= questions.Length) return;
            StartCoroutine(ProcessChoiceRoutine(chosenIndex));
        }

        private IEnumerator ProcessChoiceRoutine(int chosenIndex) {
            isAnswering = true;
            SetOptionsInteractable(false);

            bool isCorrect = (chosenIndex == currentCorrectOptionIndex);

            if (optionButtons != null && chosenIndex < optionButtons.Length && optionButtons[chosenIndex] != null) {
                var img = optionButtons[chosenIndex].GetComponent<Image>();
                if (img != null) {
                    img.color = isCorrect ? correctColor : wrongColor;
                }

                if (isCorrect) {
                    optionButtons[chosenIndex].transform.DOPunchScale(Vector3.one * 0.12f, 0.25f);
                } else {
                    optionButtons[chosenIndex].transform.DOShakePosition(0.35f, 10f);
                }
            }

            // Also highlight correct button if player was wrong
            if (!isCorrect && currentCorrectOptionIndex >= 0 && optionButtons != null && currentCorrectOptionIndex < optionButtons.Length) {
                var correctImg = optionButtons[currentCorrectOptionIndex].GetComponent<Image>();
                if (correctImg != null) correctImg.color = correctColor;
            }

            if (isCorrect) {
                score++;
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }
            } else {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }
            }

            yield return new WaitForSeconds(1.5f);

            currentQuestionIndex++;
            if (currentQuestionIndex >= questions.Length) {
                EndQuiz();
            } else {
                DisplayQuestion(currentQuestionIndex);
            }
        }

        private void SetOptionsInteractable(bool state) {
            if (optionButtons == null) return;
            foreach (var b in optionButtons) {
                if (b != null) b.interactable = state;
            }
        }

        public void ReplayQuestionAudio() {
            if (questions != null && currentQuestionIndex < questions.Length && questions[currentQuestionIndex].questionAudio != null) {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(questions[currentQuestionIndex].questionAudio);
                }
            }
        }

        private void EndQuiz() {
            if (resultPanel != null) {
                resultPanel.SetActive(true);
            }

            bool passed = score >= PASS_MARK;

            if (resultTitleTMP != null) {
                resultTitleTMP.text = passed ? "Quiz Completed!" : "Keep Practicing!";
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Score: {score} / {questions.Length}";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed
                    ? "<color=#55FF88>Congratulations! You mastered the Display Cabinet Quiz!</color>"
                    : $"<color=#FFD27F>You need at least {PASS_MARK} correct to pass. Give it another try!</color>";
            }

            if (passed) {
                if (nextButton != null) {
                    nextButton.gameObject.SetActive(true);
                    nextButton.interactable = true;
                    NextButtonAnimation();
                }

                if (celebrationAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(celebrationAudio);
                } else if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }
            } else {
                if (retryBtn != null) retryBtn.gameObject.SetActive(true);
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

        private void OnBackButtonClicked() {
            OnReturnToHub();
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
            string audioDir = "Assets/Audio/2B/11_ArtOfPoliteness/Quiz/";

            if (introAudio == null) {
#if UNITY_EDITOR
                introAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_q01_intro.mp3");
#endif
                if (narratorSpeech == null) narratorSpeech = introAudio;
            }

            if (celebrationAudio == null) {
#if UNITY_EDITOR
                celebrationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_q01_celebration.mp3");
#endif
            }

            if (questions != null && questions.Length == 12) return;

            questions = new ArtOfPolitenessQuizQuestionData[] {
                // Q1 (Match)
                new ArtOfPolitenessQuizQuestionData {
                    questionText = "Send me the report.",
                    correctOption = "Could you send me the report?",
                    distractorOptions = new string[] { "Send me the report now.", "You must send the report." },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_q01_q01.mp3")
#endif
                },
                // Q2 (Match)
                new ArtOfPolitenessQuizQuestionData {
                    questionText = "You're wrong.",
                    correctOption = "I think you might be mistaken.",
                    distractorOptions = new string[] { "You made a big error.", "That is totally wrong." },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_q01_q02.mp3")
#endif
                },
                // Q3 (MCQ)
                new ArtOfPolitenessQuizQuestionData {
                    questionText = "Which is the polite way to order?",
                    correctOption = "I'll have a pizza, please.",
                    distractorOptions = new string[] { "Give me pizza right now.", "Bring a pizza here." },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_q01_q03.mp3")
#endif
                },
                // Q4 (MCQ)
                new ArtOfPolitenessQuizQuestionData {
                    questionText = "Which is the polite way to say you are busy?",
                    correctOption = "Sorry — I'm a bit busy right now.",
                    distractorOptions = new string[] { "Don't talk to me.", "Go away, I'm working." },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_q01_q04.mp3")
#endif
                },
                // Q5 (Fill)
                new ArtOfPolitenessQuizQuestionData {
                    questionText = "I'm not _____ satisfied with this work.",
                    correctOption = "quite",
                    distractorOptions = new string[] { "never", "too", "very" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_q01_q05.mp3")
#endif
                },
                // Q6 (Fill)
                new ArtOfPolitenessQuizQuestionData {
                    questionText = "I think you _____ be mistaken.",
                    correctOption = "might",
                    distractorOptions = new string[] { "must", "can't", "always" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_q01_q06.mp3")
#endif
                },
                // Q7 (Fill)
                new ArtOfPolitenessQuizQuestionData {
                    questionText = "I'd _____ to use different colours in this design.",
                    correctOption = "prefer",
                    distractorOptions = new string[] { "want", "like", "force" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_q01_q07.mp3")
#endif
                },
                // Q8 (Move)
                new ArtOfPolitenessQuizQuestionData {
                    questionText = "'Could you send me the report?' — which move?",
                    correctOption = "ASK, DON'T ORDER",
                    distractorOptions = new string[] { "SAY IT ABOUT YOURSELF", "SOFTEN WITH 'MIGHT'" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_q01_q08.mp3")
#endif
                },
                // Q9 (Move)
                new ArtOfPolitenessQuizQuestionData {
                    questionText = "'I'm not quite satisfied with this work.' — which move?",
                    correctOption = "SAY IT ABOUT YOURSELF",
                    distractorOptions = new string[] { "ASK, DON'T ORDER", "GIVE A DIRECT ORDER" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_q01_q09.mp3")
#endif
                },
                // Q10 (Situation)
                new ArtOfPolitenessQuizQuestionData {
                    questionText = "You need to know when your teacher is free. What do you say?",
                    correctOption = "Let me know when you're available.",
                    distractorOptions = new string[] { "Tell me your schedule now.", "When are you free? Answer me." },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_q01_q10.mp3")
#endif
                },
                // Q11 (Odd one out)
                new ArtOfPolitenessQuizQuestionData {
                    questionText = "Which is NOT a softening word from this unit: please · might · sorry · never",
                    correctOption = "never",
                    distractorOptions = new string[] { "please", "might", "sorry" },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_q01_q11.mp3")
#endif
                },
                // Q12 (T/F)
                new ArtOfPolitenessQuizQuestionData {
                    questionText = "True or False: a polite sentence changes what you mean.",
                    correctOption = "False — it changes how it lands, not what you mean.",
                    distractorOptions = new string[] { "True — it changes the whole message." },
#if UNITY_EDITOR
                    questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "artofpoliteness_q01_q12.mp3")
#endif
                }
            };
        }
    }
}
