using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit4 {

    /// <summary>
    /// Q01 Gallery Quiz — Colour Your Speech
    /// 12 mixed-format questions covering idioms, meanings, literal-vs-real and conversations.
    /// Pass mark: 9 / 12.
    /// </summary>
    public class Masters_2B_ColourYourSpeech_Quiz_LessonOne : Masters_Lesson {

[System.Serializable]
    public class ColourYourSpeechQuizQuestionData {
        public string questionText;
        public string correctOption;
        public string[] distractorOptions;
        public AudioClip questionAudio;
    }
    
        [Header("Q01 12 Mixed-Format Questions")]
        [SerializeField] private ColourYourSpeechQuizQuestionData[] questions;

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
        [SerializeField] private AudioClip sfxCorrect;
        [SerializeField] private AudioClip sfxWrong;

        [Header("Results & Retry Panel")]
        [SerializeField] private GameObject resultPanel;
        [SerializeField] private TextMeshProUGUI resultTitleTMP;
        [SerializeField] private TextMeshProUGUI resultScoreTMP;
        [SerializeField] private TextMeshProUGUI resultStatusTMP;
        [SerializeField] private Button retryBtn;
        [SerializeField] private Button returnHubBtn;

        [Header("Navigation")]
        [SerializeField] private Button backButton;
        [SerializeField] private Masters_LessonSO nextLessonSO;

        [Header("Colors & Styling")]
        [SerializeField] private Color defaultChipColor = new Color(0.12f, 0.22f, 0.42f, 1f);
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
            RestartQuiz();
        }

        public void SetQuestions(ColourYourSpeechQuizQuestionData[] newQuestions) {
            questions = newQuestions;
        }

        [ContextMenu("Update Editor Preview")]
        public void UpdateEditorPreview() {
            AutoBindReferences();
            InitQuestionsIfEmpty();
            EnsureHeaderAndTitle();

            if (questions == null || questions.Length == 0) return;

            ColourYourSpeechQuizQuestionData q = questions[0];

            if (progressTMP != null) {
                progressTMP.text = "Question 1/12";
                progressTMP.enableAutoSizing = false;
                progressTMP.fontSize = 24f;
            }

            if (questionTextTMP != null) {
                questionTextTMP.text = q.questionText;
                questionTextTMP.color = Color.white;
                questionTextTMP.enableAutoSizing = false;
                questionTextTMP.fontSize = 28f;
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
                            txt.fontSizeMin = 18f;
                            txt.fontSizeMax = 24f;
                            txt.alignment = TextAlignmentOptions.Center;
                            txt.enableWordWrapping = true;
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

            if (backButton == null) {
                Transform bTr = hud.Find("BackButton") ?? transform.Find("BackButton");
                if (bTr != null) backButton = bTr.GetComponent<Button>();
            }

            Transform headerTr = transform.Find("HeaderContainer");
            if (headerTr != null) {
                if (headerTMP == null) {
                    Transform h = headerTr.Find("Branch") ?? headerTr.Find("Header");
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
                Transform rTr = transform.Find("AudioReplayButton") ?? transform.Find("ReplayButton") ?? transform.Find("HeaderContainer/ReplayBtn") ?? transform.Find("Audio/ReplayAudioButton") ?? transform.Find("QuestionCard/ReplayAudioBtn");
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
                headerTMP.text = "COLOUR YOUR SPEECH";
                headerTMP.enableAutoSizing = false;
                headerTMP.fontSize = 24f;
            }
            if (titleTMP != null) {
                titleTMP.gameObject.SetActive(true);
                titleTMP.text = "Q01 Gallery Quiz - Colour Your Speech";
                titleTMP.color = new Color(1f, 0.88f, 0.25f, 1f);
                titleTMP.enableAutoSizing = false;
                titleTMP.fontSize = 32f;
                titleTMP.fontStyle = FontStyles.Bold;
                titleTMP.alignment = TextAlignmentOptions.Center;
                titleTMP.enableWordWrapping = false;
            }
            if (progressTMP != null) {
                progressTMP.gameObject.SetActive(true);
                int total = (questions != null && questions.Length > 0) ? questions.Length : 12;
                progressTMP.text = $"Question {Mathf.Min(currentQuestionIndex + 1, total)}/{total}";
                progressTMP.enableAutoSizing = false;
                progressTMP.fontSize = 24f;
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

            if (backButton != null) {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnBackButtonClicked);
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
            if (replayAudioBtn != null) replayAudioBtn.interactable = false;

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
            if (replayAudioBtn != null) replayAudioBtn.interactable = true;
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
                progressTMP.fontSize = 24f;
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

            ColourYourSpeechQuizQuestionData q = questions[currentQuestionIndex];

            if (questionTextTMP != null) {
                questionTextTMP.text = q.questionText;
                questionTextTMP.color = Color.white;
                questionTextTMP.enableAutoSizing = false;
                questionTextTMP.fontSize = 28f;
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

            // Determine correct index deterministically or based on question ID
            int correctIndex = (currentQuestionIndex * 3) % choices.Count;
            currentCorrectOptionIndex = correctIndex;

            // Place correct option at correctIndex
            string temp = choices[0];
            choices[0] = choices[correctIndex];
            choices[correctIndex] = temp;

            // Apply choices to buttons
            if (optionButtons != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (i < choices.Count) {
                        optionButtons[i].gameObject.SetActive(true);
                        optionButtons[i].interactable = true;
                        optionButtons[i].transform.DOKill();
                        optionButtons[i].transform.localScale = Vector3.one;

                        Image btnImg = optionButtons[i].GetComponent<Image>();
                        if (btnImg != null) btnImg.color = defaultChipColor;

                        var txt = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                        if (txt != null) {
                            txt.text = choices[i];
                            txt.color = Color.white;
                            txt.enableAutoSizing = true;
                            txt.fontSizeMin = 18f;
                            txt.fontSizeMax = 24f;
                            txt.alignment = TextAlignmentOptions.Center;
                            txt.enableWordWrapping = true;
                            txt.margin = new Vector4(10, 4, 10, 4);
                        }
                    } else {
                        optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }

            if (playAudio) {
                PlayCurrentQuestionAudio();
            }
        }

        private void PlayCurrentQuestionAudio() {
            if (questions != null && currentQuestionIndex >= 0 && currentQuestionIndex < questions.Length) {
                AudioClip qAudio = questions[currentQuestionIndex].questionAudio;
                if (qAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.StopVoiceOver();
                    Masters_AudioManager.Instance.PlayVoiceOver(qAudio);
                }
            }
        }

        private void ReplayCurrentQuestionAudio() {
            if (isAnswering) return;
            PlayCurrentQuestionAudio();
            if (replayAudioBtn != null) {
                replayAudioBtn.transform.DOKill();
                replayAudioBtn.transform.DOScale(Vector3.one * 1.1f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }
        }

        private void OnOptionSelected(int optionIndex) {
            if (isAnswering) return;
            isAnswering = true;
            EnableOptionButtons(false);
            if (replayAudioBtn != null) replayAudioBtn.interactable = false;

            bool isCorrect = (optionIndex == currentCorrectOptionIndex);

            if (isCorrect) {
                score++;
                if (sfxCorrect != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(sfxCorrect);
                } else if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                if (optionIndex < optionButtons.Length && optionButtons[optionIndex] != null) {
                    Image btnImg = optionButtons[optionIndex].GetComponent<Image>();
                    if (btnImg != null) btnImg.color = correctColor;
                    optionButtons[optionIndex].transform.DOScale(Vector3.one * 1.05f, 0.15f).SetLoops(2, LoopType.Yoyo);
                }
            } else {
                if (sfxWrong != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(sfxWrong);
                } else if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                if (optionIndex < optionButtons.Length && optionButtons[optionIndex] != null) {
                    Image btnImg = optionButtons[optionIndex].GetComponent<Image>();
                    if (btnImg != null) btnImg.color = wrongColor;
                    optionButtons[optionIndex].transform.DOShakePosition(0.25f, new Vector3(10f, 0f, 0f), 10, 90, false, true);
                }

                // Also highlight correct answer
                if (currentCorrectOptionIndex < optionButtons.Length && optionButtons[currentCorrectOptionIndex] != null) {
                    Image correctBtnImg = optionButtons[currentCorrectOptionIndex].GetComponent<Image>();
                    if (correctBtnImg != null) correctBtnImg.color = correctColor;
                }
            }

            StartCoroutine(AdvanceToNextQuestion(1.4f));
        }

        private IEnumerator AdvanceToNextQuestion(float delay) {
            yield return new WaitForSeconds(delay);
            currentQuestionIndex++;
            if (currentQuestionIndex < questions.Length) {
                ShowQuestion(currentQuestionIndex, playAudio: true);
                EnableOptionButtons(true);
                if (replayAudioBtn != null) replayAudioBtn.interactable = true;
            } else {
                EndQuiz();
            }
        }

        private void EndQuiz() {
            isAnswering = true;
            EnableOptionButtons(false);
            if (replayAudioBtn != null) replayAudioBtn.interactable = false;

            bool isPassed = (score >= PASS_MARK);

            if (resultPanel != null) {
                resultPanel.SetActive(true);
                resultPanel.transform.DOKill();
                resultPanel.transform.localScale = Vector3.zero;
                resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
            }

            if (resultTitleTMP != null) {
                resultTitleTMP.text = isPassed ? "GALLERY QUIZ COMPLETED!" : "QUIZ NOT PASSED";
                resultTitleTMP.color = isPassed ? new Color(0.15f, 0.85f, 0.4f) : new Color(0.95f, 0.35f, 0.35f);
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Score: {score} / 12 (Pass Mark: {PASS_MARK})";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = isPassed 
                    ? "Outstanding job! You mastered all the idiom paintings in the gallery!" 
                    : "Try again! You need at least 9 correct answers to complete the quiz!";
            }

            if (isPassed) {
                if (celebrationAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(celebrationAudio);
                }
                if (nextButton != null) {
                    nextButton.gameObject.SetActive(true);
                    nextButton.interactable = true;
                    NextButtonAnimation();
                }
                if (Masters_TopicSelectionManager.Instance != null) {
                    Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Rewards);
                }
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Quiz);
                }
            }
        }

        private void OnReturnToHub() {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnBackButtonClicked();
            }
        }

        protected override void OnNextButtonClicked() {
            if (topic == Masters_Topic.None) return;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
            }

            if (nextLessonSO != null) {
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
                }
            } else {
                if (Masters_TopicSelectionManager.Instance != null) {
                    Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Rewards);
                }
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnLessonComplete(topic);
                }
            }
        }

        protected virtual void OnBackButtonClicked() {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnBackButtonClicked();
            }
        }

        private void InitQuestionsIfEmpty() {
            if (questions == null || questions.Length == 0) {
                questions = new ColourYourSpeechQuizQuestionData[] {
                    new ColourYourSpeechQuizQuestionData {
                        questionText = "What does 'Miss the boat' mean?",
                        correctOption = "To miss a chance",
                        distractorOptions = new string[] { "To arrive late at the harbor", "To swim after a ship", "To buy a boat ticket" }
                    },
                    new ColourYourSpeechQuizQuestionData {
                        questionText = "What does 'A close shave' mean?",
                        correctOption = "Narrowly escape a disaster",
                        distractorOptions = new string[] { "Shaving quickly in the morning", "Having a clean beard", "Visiting the barber shop" }
                    },
                    new ColourYourSpeechQuizQuestionData {
                        questionText = "What does 'Bury the hatchet' mean?",
                        correctOption = "To make up after an argument",
                        distractorOptions = new string[] { "To hide an axe in the ground", "To buy new garden tools", "To cut firewood together" }
                    },
                    new ColourYourSpeechQuizQuestionData {
                        questionText = "What does 'You scratch my back I will scratch yours' mean?",
                        correctOption = "You help me and I will help you",
                        distractorOptions = new string[] { "Two people scratching backs", "Asking someone to check skin", "Sharing a back massage" }
                    },
                    new ColourYourSpeechQuizQuestionData {
                        questionText = "Complete the idiom: \"Raining cats and _______.\"",
                        correctOption = "dogs",
                        distractorOptions = new string[] { "birds", "frogs", "fish" }
                    },
                    new ColourYourSpeechQuizQuestionData {
                        questionText = "Complete the idiom: \"Bite off more than you can _______.\"",
                        correctOption = "chew",
                        distractorOptions = new string[] { "swallow", "eat", "cook" }
                    },
                    new ColourYourSpeechQuizQuestionData {
                        questionText = "Which represents what 'The cat is out of the bag' really means?",
                        correctOption = "A secret being revealed",
                        distractorOptions = new string[] { "A cat climbing out of a bag", "Buying a backpack for a cat", "Searching inside a lost bag" }
                    },
                    new ColourYourSpeechQuizQuestionData {
                        questionText = "Your friend finishes the puzzle in one minute. Which idiom fits?",
                        correctOption = "Piece of cake",
                        distractorOptions = new string[] { "Raining cats and dogs", "A close shave", "Bury the hatchet" }
                    },
                    new ColourYourSpeechQuizQuestionData {
                        questionText = "You promised more jobs than you can finish. Which idiom fits?",
                        correctOption = "Bite off more than you can chew",
                        distractorOptions = new string[] { "Miss the boat", "Have a blast", "The cat is out of the bag" }
                    },
                    new ColourYourSpeechQuizQuestionData {
                        questionText = "Who said: \"We had a blast, Granny!\"?",
                        correctOption = "Clintan",
                        distractorOptions = new string[] { "Dev", "Aria", "Grandma" }
                    },
                    new ColourYourSpeechQuizQuestionData {
                        questionText = "Which is NOT one of this unit's idioms?",
                        correctOption = "Break the ice",
                        distractorOptions = new string[] { "Have a blast", "Piece of cake", "A close shave" }
                    },
                    new ColourYourSpeechQuizQuestionData {
                        questionText = "True or False: An idiom means exactly what its separate words say.",
                        correctOption = "False",
                        distractorOptions = new string[] { "True" }
                    }
                };
            }
        }
    }
}
