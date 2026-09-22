using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class Masters_CourtesyCall_Quiz_LessonOne : Masters_Lesson {

[System.Serializable]
public class QuizQ01Question {
    public string questionText;
    public string[] options;         // 4 options A, B, C, D
    public int correctOptionIndex;   // 0, 1, 2, 3
    public string explanationText;
    public AudioClip questionAudio;
}

    [Header("Q01 12 Verbatim Unit 2 Quiz Questions (p.11-14)")]
    [SerializeField]
    private QuizQ01Question[] questions;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI questionTextTMP;
    [SerializeField]
    private TextMeshProUGUI feedbackTextTMP;

    [Header("4 Choice Option Buttons (A, B, C, D)")]
    [SerializeField]
    private Button[] optionButtons; // 4 Buttons
    [SerializeField]
    private TextMeshProUGUI[] optionTexts;

    [Header("Confirm / Submit Action")]
    [SerializeField]
    private Button confirmButton;

    [Header("Audio Controls")]
    [SerializeField]
    private Button replayAudioBtn;

    [Header("Results & Retry Panel")]
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultScoreTMP;
    [SerializeField]
    private TextMeshProUGUI resultStatusTMP;
    [SerializeField]
    private Button retryBtn;

    [Header("Editor Preview")]
    [Range(0, 11)]
    public int editorPreviewQuestion = 0;

    private int currentQuestionIndex = 0;
    private int score = 0;
    private int selectedOptionIndex = -1;
    private bool isAnswered = false;

    private Color defaultBtnColor = new Color(0.14f, 0.38f, 0.58f, 1f);
    private Color selectedBtnColor = new Color(0.14f, 0.58f, 0.85f, 1f);
    private Color correctBtnColor = new Color(0.14f, 0.53f, 0.22f, 1f);
    private Color incorrectBtnColor = new Color(0.71f, 0.15f, 0.15f, 1f);

    private Image[] optionButtonImages;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Quiz;

        PurgeLegacyChildren();
        AutoBindReferences();

        if (optionButtons != null && optionButtons.Length > 0) {
            optionButtonImages = new Image[optionButtons.Length];
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    int index = i;
                    optionButtonImages[i] = optionButtons[i].GetComponent<Image>();
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionButtonClicked(index));
                }
            }
        }

        if (confirmButton != null) {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(OnRetryButtonClicked);
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Quiz;

        PurgeLegacyChildren();
        AutoBindReferences();
        EnsureHeaderAndTitle();

        if (questions == null || questions.Length == 0) {
            PopulateFailsafeQuestions();
        }

#if UNITY_EDITOR
        if (narratorSpeech == null) {
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Quiz/VO_Q01_ARIA.mp3");
        }
#endif

        StartCoroutine(StartWithIntroAudioRoutine());
    }

    private IEnumerator StartWithIntroAudioRoutine() {
        isAnswered = true;

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) optionButtons[i].interactable = false;
            }
        }
        if (confirmButton != null) confirmButton.interactable = false;

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            yield return new WaitForSeconds(Mathf.Max(2.5f, narratorSpeech.length));
        } else {
            yield return new WaitForSeconds(0.5f);
        }

        StartQuiz();
    }

    public void StartQuiz() {
        currentQuestionIndex = 0;
        score = 0;
        isAnswered = false;

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }

        LoadQuestion(currentQuestionIndex);
    }

    private void OnValidate() {
        if (!Application.isPlaying) {
            UpdateEditorPreview();
        }
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        AutoBindReferences();

        if (questions == null || questions.Length == 0) {
            PopulateFailsafeQuestions();
        }

        if (questions == null || questions.Length == 0) return;

        int idx = Mathf.Clamp(editorPreviewQuestion, 0, questions.Length - 1);
        QuizQ01Question q = questions[idx];

        if (headerTMP != null) headerTMP.text = "QUIZ BRANCH (Town Hall Podium)";
        if (titleTMP != null) titleTMP.text = "Q01 Town Hall Quiz — Courtesy Call";
        if (progressTMP != null) progressTMP.text = $"Question {idx + 1}/{questions.Length}";
        if (questionTextTMP != null) questionTextTMP.text = q.questionText;

        if (optionButtons != null && q.options != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    bool hasOpt = (i < q.options.Length);
                    optionButtons[i].gameObject.SetActive(hasOpt);
                    if (hasOpt) {
                        TextMeshProUGUI tmp = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (tmp != null) tmp.text = q.options[i];
                    }
                }
            }
        }
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                Destroy(lTrans.gameObject);
            }
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "QUIZ BRANCH (Town Hall Podium)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "Q01 Town Hall Quiz — Courtesy Call";
        }
    }

    private void AutoBindReferences() {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> opts = new List<Button>();
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("option") || n.Contains("quizbutton") || n.Contains("choice")) opts.Add(btn);
            else if (confirmButton == null && (n.Contains("confirm") || n.Contains("submit") || n.Contains("check"))) confirmButton = btn;
            else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("repeat") || n.Contains("audio"))) replayAudioBtn = btn;
            else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
            else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) nextButton = btn;
        }

        if (opts.Count >= 4 && (optionButtons == null || optionButtons.Length == 0)) {
            optionButtons = opts.ToArray();
        }

        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (progressTMP == null && (n.Contains("progresstext") || n.Contains("progress") || n.Contains("counter"))) progressTMP = tmp;
            else if (questionTextTMP == null && (n.Contains("question") || n.Contains("prompt"))) questionTextTMP = tmp;
            else if (feedbackTextTMP == null && (n.Contains("feedback") || n.Contains("explanation"))) feedbackTextTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    private void PopulateFailsafeQuestions() {
        questions = new QuizQ01Question[] {
            new QuizQ01Question {
                questionText = "'Excuse me sir, you dropped your wallet.' — is this THANK, EXCUSE ME or SORRY?",
                options = new string[] { "A) THANK YOU", "B) EXCUSE ME", "C) SORRY", "D) NONE" },
                correctOptionIndex = 1,
                explanationText = "'Excuse me' is used to politely grab someone's attention.",
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Quiz/VO_Q01_Q1.mp3")
#endif
            },
            new QuizQ01Question {
                questionText = "'I really appreciate your help.' — is this THANK, EXCUSE ME or SORRY?",
                options = new string[] { "A) THANK YOU", "B) EXCUSE ME", "C) SORRY", "D) NONE" },
                correctOptionIndex = 0,
                explanationText = "'Appreciate your help' is a full form of expressing thanks!",
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Quiz/VO_Q01_Q2.mp3")
#endif
            },
            new QuizQ01Question {
                questionText = "You arrived late — choose the correct courtesy line:",
                options = new string[] { "A) I'm happy.", "B) Excuse me sir.", "C) I'm sorry for being so late.", "D) Thanks a lot." },
                correctOptionIndex = 2,
                explanationText = "'I'm sorry for being so late' expresses an apology for tardiness.",
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Quiz/VO_Q01_Q3.mp3")
#endif
            },
            new QuizQ01Question {
                questionText = "Someone gave you a present — choose the correct line:",
                options = new string[] { "A) I'm sorry for the mess.", "B) Excuse me, do you know what time it is?", "C) I'm nervous.", "D) Thank you so much for the birthday gift." },
                correctOptionIndex = 3,
                explanationText = "Saying thank you for a birthday gift names the reason.",
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Quiz/VO_Q01_Q4.mp3")
#endif
            },
            new QuizQ01Question {
                questionText = "Complete the blank: 'Thanks a _____ .'",
                options = new string[] { "A) lot", "B) much", "C) late", "D) mess" },
                correctOptionIndex = 0,
                explanationText = "The standard phrase is 'Thanks a lot.'",
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Quiz/VO_Q01_Q5.mp3")
#endif
            },
            new QuizQ01Question {
                questionText = "Complete the blank: 'I'm sorry for the _____ .'",
                options = new string[] { "A) gift", "B) mess", "C) help", "D) time" },
                correctOptionIndex = 1,
                explanationText = "The standard phrase is 'I'm sorry for the mess.'",
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Quiz/VO_Q01_Q6.mp3")
#endif
            },
            new QuizQ01Question {
                questionText = "Your turn on stage is next. How do you feel?",
                options = new string[] { "A) I'm relaxed.", "B) I'm exhausted.", "C) I'm nervous.", "D) I'm angry." },
                correctOptionIndex = 2,
                explanationText = "Before going on stage, people feel nervous.",
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Quiz/VO_Q01_Q7.mp3")
#endif
            },
            new QuizQ01Question {
                questionText = "You have worked hard all day. How do you feel?",
                options = new string[] { "A) I'm happy.", "B) I'm surprised.", "C) I'm frightened.", "D) I'm exhausted." },
                correctOptionIndex = 3,
                explanationText = "Working hard all day leaves you exhausted.",
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Quiz/VO_Q01_Q8.mp3")
#endif
            },
            new QuizQ01Question {
                questionText = "In the book's dialogue, what is wrong with Sam?",
                options = new string[] { "A) He has a headache.", "B) He lost his wallet.", "C) He is late.", "D) He is angry." },
                correctOptionIndex = 0,
                explanationText = "On p.13, Sam says 'I have a headache.'",
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Quiz/VO_Q01_Q9.mp3")
#endif
            },
            new QuizQ01Question {
                questionText = "Which line is NOT a way of thanking?",
                options = new string[] { "A) Thanks a lot.", "B) I'm sorry for the mess.", "C) I really appreciate it.", "D) Thank you so much." },
                correctOptionIndex = 1,
                explanationText = "'I'm sorry for the mess' is an apology, not thanks.",
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Quiz/VO_Q01_Q10.mp3")
#endif
            },
            new QuizQ01Question {
                questionText = "Put Jammy's care in order:",
                options = new string[] {
                    "A) offer the pill ➔ ask how he is",
                    "B) ask what happened ➔ offer the pill",
                    "C) ask how he is ➔ ask what happened ➔ offer the pill",
                    "D) offer the pill ➔ say goodbye"
                },
                correctOptionIndex = 2,
                explanationText = "Jammy greets & asks how he is, asks what happened, then offers the pill.",
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Quiz/VO_Q01_Q11.mp3")
#endif
            },
            new QuizQ01Question {
                questionText = "True or False: 'Excuse me, do you know what time it is?' is a polite way to stop someone and ask.",
                options = new string[] { "A) False", "B) Not Sure", "C) Both", "D) True" },
                correctOptionIndex = 3,
                explanationText = "True! 'Excuse me' is the polite courtesy phrase.",
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Quiz/VO_Q01_Q12.mp3")
#endif
            }
        };

#if UNITY_EDITOR
        if (narratorSpeech == null) {
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Quiz/VO_Q01_ARIA.mp3");
        }
#endif
    }

    private void LoadQuestion(int qIndex) {
        if (questions == null || qIndex < 0 || qIndex >= questions.Length) return;

        isAnswered = false;
        selectedOptionIndex = -1;
        QuizQ01Question q = questions[qIndex];

        if (progressTMP != null) progressTMP.text = $"Question {qIndex + 1}/{questions.Length}";
        if (questionTextTMP != null) questionTextTMP.text = q.questionText;

        if (feedbackTextTMP != null) {
            feedbackTextTMP.text = "";
            feedbackTextTMP.gameObject.SetActive(false);
        }

        if (confirmButton != null) {
            confirmButton.interactable = false;
        }

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    bool hasOpt = (q.options != null && i < q.options.Length);
                    optionButtons[i].gameObject.SetActive(hasOpt);
                    if (hasOpt) {
                        optionButtons[i].interactable = true;
                        if (optionButtonImages != null && i < optionButtonImages.Length && optionButtonImages[i] != null) {
                            optionButtonImages[i].color = defaultBtnColor;
                        }
                        TextMeshProUGUI tmp = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (tmp != null) tmp.text = q.options[i];
                    }
                }
            }
        }

        PlayQuestionAudio();
    }

    private void PlayQuestionAudio() {
        if (questions != null && currentQuestionIndex >= 0 && currentQuestionIndex < questions.Length) {
            AudioClip clip = questions[currentQuestionIndex].questionAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }
    }

    private void OnOptionButtonClicked(int optIndex) {
        if (isAnswered || questions == null || currentQuestionIndex >= questions.Length) return;

        selectedOptionIndex = optIndex;

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null && optionButtonImages != null && i < optionButtonImages.Length && optionButtonImages[i] != null) {
                    optionButtonImages[i].color = (i == selectedOptionIndex) ? selectedBtnColor : defaultBtnColor;
                }
            }
        }

        if (confirmButton != null) {
            confirmButton.interactable = true;
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    private void OnConfirmButtonClicked() {
        if (isAnswered || selectedOptionIndex < 0 || questions == null || currentQuestionIndex >= questions.Length) return;

        isAnswered = true;
        QuizQ01Question q = questions[currentQuestionIndex];
        bool isCorrect = (selectedOptionIndex == q.correctOptionIndex);

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    optionButtons[i].interactable = false;
                    if (optionButtonImages != null && i < optionButtonImages.Length && optionButtonImages[i] != null) {
                        if (i == q.correctOptionIndex) {
                            optionButtonImages[i].color = correctBtnColor;
                        } else if (i == selectedOptionIndex) {
                            optionButtonImages[i].color = incorrectBtnColor;
                        }
                    }
                }
            }
        }

        if (isCorrect) {
            score++;
            if (feedbackTextTMP != null) {
                feedbackTextTMP.gameObject.SetActive(true);
                feedbackTextTMP.text = $"✓ CORRECT! {q.explanationText}";
                feedbackTextTMP.color = Color.green;
            }
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
        } else {
            if (feedbackTextTMP != null) {
                feedbackTextTMP.gameObject.SetActive(true);
                feedbackTextTMP.text = $"✗ INCORRECT. {q.explanationText}";
                feedbackTextTMP.color = Color.red;
            }
            if (optionButtons != null && selectedOptionIndex < optionButtons.Length && optionButtons[selectedOptionIndex] != null) {
                optionButtons[selectedOptionIndex].transform.DOShakePosition(0.4f, 8f);
            }
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }

        StartCoroutine(AdvanceQuestionRoutine());
    }

    private IEnumerator AdvanceQuestionRoutine() {
        yield return new WaitForSeconds(1.8f);

        currentQuestionIndex++;
        if (currentQuestionIndex < questions.Length) {
            LoadQuestion(currentQuestionIndex);
        } else {
            CompleteLesson();
        }
    }

    private void CompleteLesson() {
        bool passed = (score >= 9); // Pass condition: 9 of 12

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            if (resultScoreTMP != null) resultScoreTMP.text = $"Final Score: {score}/{questions.Length}";
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "VICTORY! TOWN HALL REWARD UNLOCKED!" : "TRY AGAIN TO PASS!";
                resultStatusTMP.color = passed ? Color.green : Color.red;
            }
        }

        if (passed && nextButton != null) {
            nextButton.interactable = true;
            NextButtonAnimation();
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(passed ? Masters_SFX.Correct : Masters_SFX.Incorrect);
        }
    }

    private void OnReplayAudioClicked() {
        PlayQuestionAudio();
    }

    private void OnRetryButtonClicked() {
        if (resultPanel != null) resultPanel.SetActive(false);
        currentQuestionIndex = 0;
        score = 0;
        LoadQuestion(currentQuestionIndex);
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}
