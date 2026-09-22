using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


/// <summary>
/// Q01 Front Office Quiz — Doctor! I Need Your Help!
/// 12 mixed-format questions covering symptoms, body parts, consultation, appointment call, and home conversation.
/// Pass mark = 9 / 12.
/// </summary>
public class Masters_DoctorNeedYourHelp_Quiz_LessonOne : Masters_Lesson {

[System.Serializable]
public class DoctorQuizQuestionData {
    public string questionText;        // Question prompt
    public string correctOption;       // Correct answer
    public string[] distractorOptions; // 3 distractor choices
}

    [Header("Q01 12 Mixed-Format Questions")]
    [SerializeField]
    private DoctorQuizQuestionData[] questions;

    [Header("UI References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI questionTextTMP;

    [Header("4 Option Buttons")]
    [SerializeField]
    private Button[] optionButtons; // OptionButton_01 .. OptionButton_04

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
    public int editorPreviewRound = 0;

    [Header("Colors & Styling")]
    [SerializeField]
    private Color defaultChipColor = new Color(0.14f, 0.38f, 0.58f, 1f); // Medium Blue #235E8A
    [SerializeField]
    private Color correctColor = new Color(0.14f, 0.53f, 0.22f, 1f);     // Emerald Green #248838
    [SerializeField]
    private Color wrongColor = new Color(0.71f, 0.15f, 0.15f, 1f);       // Crimson Red #B52626

    private int currentRoundIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;
    private bool currentRoundHasRetried = false;

    private Image[] optionImages;
    private Color[] originalOptionColors;
    private string[] currentRoundShuffledChoices;
    private int currentRoundCorrectChoiceIndex;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Quiz;

        PurgeLegacyChildren();
        AutoBindReferences();

        if (questions == null || questions.Length == 0 || questions[0] == null || string.IsNullOrEmpty(questions[0].questionText) || !questions[0].questionText.Contains("ankle")) {
            PopulateFailsafeQuestions();
        }

        if (optionButtons != null && optionButtons.Length > 0) {
            optionImages = new Image[optionButtons.Length];
            originalOptionColors = new Color[optionButtons.Length];

            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    int index = i;
                    optionImages[i] = optionButtons[i].GetComponent<Image>();
                    if (optionImages[i] != null) {
                        originalOptionColors[i] = optionImages[i].color;
                    } else {
                        originalOptionColors[i] = defaultChipColor;
                    }

                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
                }
            }
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
        EnsureHeaderAndTitle();

        if (questions == null || questions.Length == 0 || questions[0] == null || string.IsNullOrEmpty(questions[0].questionText) || !questions[0].questionText.Contains("ankle")) {
            PopulateFailsafeQuestions();
        }

        StartCoroutine(StartWithIntroAudioRoutine());
    }

    private void OnValidate() {
        if (!Application.isPlaying) {
            UpdateEditorPreview();
        }
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        AutoBindReferences();

        if (questions == null || questions.Length == 0 || questions[0] == null || string.IsNullOrEmpty(questions[0].questionText) || !questions[0].questionText.Contains("ankle")) {
            PopulateFailsafeQuestions();
        }

        if (questions == null || questions.Length == 0) return;

        int idx = Mathf.Clamp(editorPreviewRound, 0, questions.Length - 1);
        DoctorQuizQuestionData q = questions[idx];

        if (progressTMP != null) progressTMP.text = $"Question {idx + 1}/12";
        if (questionTextTMP != null) questionTextTMP.text = q.questionText;
        if (titleTMP != null) titleTMP.text = "Q01 Front Office Quiz — Doctor! I Need Your Help!";
        if (headerTMP != null) headerTMP.text = "🎯 QUIZ (Front Office)";

        if (optionButtons != null && optionButtons.Length >= 4) {
            string[] previewChoices = new string[4];
            previewChoices[0] = q.correctOption;
            previewChoices[1] = (q.distractorOptions != null && q.distractorOptions.Length > 0) ? q.distractorOptions[0] : "Option B";
            previewChoices[2] = (q.distractorOptions != null && q.distractorOptions.Length > 1) ? q.distractorOptions[1] : "Option C";
            previewChoices[3] = (q.distractorOptions != null && q.distractorOptions.Length > 2) ? q.distractorOptions[2] : "Option D";

            for (int i = 0; i < 4; i++) {
                if (optionButtons[i] != null) {
                    optionButtons[i].gameObject.SetActive(true);
                    SetButtonText(optionButtons[i], previewChoices[i]);
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

        for (int i = transform.childCount - 1; i >= 0; i--) {
            Transform child = transform.GetChild(i);
            string cName = child.name.ToLower();
            if (cName.Contains("statement") || cName.Contains("words") || cName.Contains("fillin")) {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "🎯 QUIZ (Front Office)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "Q01 Front Office Quiz — Doctor! I Need Your Help!";
        }
    }

    private void AutoBindReferences() {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> chipBtns = new List<Button>();

        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("optionbutton") || n.Contains("optionchip") || n.Contains("chip") || n.Contains("option")) {
                chipBtns.Add(btn);
            } else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("repeat") || n.Contains("audio"))) {
                replayAudioBtn = btn;
            } else if (retryBtn == null && n.Contains("retry")) {
                retryBtn = btn;
            } else if (nextButton == null && n.Contains("next")) {
                nextButton = btn;
            }
        }

        if ((optionButtons == null || optionButtons.Length == 0 || System.Array.Exists(optionButtons, b => b == null)) && chipBtns.Count >= 4) {
            optionButtons = new Button[4];
            for (int i = 0; i < 4; i++) {
                optionButtons[i] = chipBtns[i];
            }
        }

        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (progressTMP == null && (n.Contains("progresstext") || n.Contains("progress") || n.Contains("count") || n.Contains("roundcount"))) {
                progressTMP = tmp;
            } else if (questionTextTMP == null && (n.Contains("question") || n.Contains("prompt") || n.Contains("statement") || n.Contains("dialogue"))) {
                questionTextTMP = tmp;
            } else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) {
                titleTMP = tmp;
            } else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) {
                headerTMP = tmp;
            }
        }
    }

    public void PopulateFailsafeQuestions() {
        questions = new DoctorQuizQuestionData[] {
            // Q1 (Body map)
            new DoctorQuizQuestionData {
                questionText = "“I twisted my ankle!” — Which part of the body was injured?",
                correctOption = "Ankle",
                distractorOptions = new string[] { "Wrist", "Elbow", "Knee" }
            },
            // Q2 (Body map)
            new DoctorQuizQuestionData {
                questionText = "“My throat is dry! I can't stop coughing.” — Which body part is dry?",
                correctOption = "Throat",
                distractorOptions = new string[] { "Chest", "Stomach", "Shoulder" }
            },
            // Q3 (Match)
            new DoctorQuizQuestionData {
                questionText = "Complete the symptom description: “My eyes are _____”",
                correctOption = "watery",
                distractorOptions = new string[] { "stiff", "dry", "twisted" }
            },
            // Q4 (Fill)
            new DoctorQuizQuestionData {
                questionText = "Complete the symptom description: “My knees keep _____”",
                correctOption = "locking",
                distractorOptions = new string[] { "coughing", "sneezing", "spinning" }
            },
            // Q5 (Fill)
            new DoctorQuizQuestionData {
                questionText = "Complete the symptom description: “I had trouble in _____”",
                correctOption = "breathing",
                distractorOptions = new string[] { "sleeping", "speaking", "walking" }
            },
            // Q6 (MCQ)
            new DoctorQuizQuestionData {
                questionText = "Which sentence tells the doctor when the trouble happens?",
                correctOption = "It happens a lot when I work out.",
                distractorOptions = new string[] {
                    "My throat is dry and coughing.",
                    "I want to consult Dr Gupta.",
                    "I am running a temperature."
                }
            },
            // Q7 (Who said it)
            new DoctorQuizQuestionData {
                questionText = "Who said: “Do you have an appointment?”",
                correctOption = "The Receptionist",
                distractorOptions = new string[] { "The Doctor", "Sandy", "Steve" }
            },
            // Q8 (Who said it)
            new DoctorQuizQuestionData {
                questionText = "Who said: “Do you have any allergies that you know of?”",
                correctOption = "The Doctor",
                distractorOptions = new string[] { "The Receptionist", "Mom", "The Patient" }
            },
            // Q9 (Order)
            new DoctorQuizQuestionData {
                questionText = "What is the correct order for making a clinic appointment call?",
                correctOption = "Ask to consult -> Say who patient is -> Agree the time",
                distractorOptions = new string[] {
                    "Agree the time -> Say who patient is -> Ask to consult",
                    "Say who patient is -> Agree the time -> Ask to consult",
                    "Agree the time -> Ask to consult -> Say who patient is"
                }
            },
            // Q10 (MCQ)
            new DoctorQuizQuestionData {
                questionText = "In the book, what appointment time does the receptionist offer Steve in the end?",
                correctOption = "1:30 this afternoon",
                distractorOptions = new string[] {
                    "10:00 tomorrow morning",
                    "2:30 this afternoon",
                    "9:00 tomorrow morning"
                }
            },
            // Q11 (MCQ)
            new DoctorQuizQuestionData {
                questionText = "Why does Sandy feel unwell in the home conversation?",
                correctOption = "He stayed up late watching a movie instead of sleeping",
                distractorOptions = new string[] {
                    "He caught a cold at school",
                    "He ate expired food for dinner",
                    "He twisted his ankle during exercise"
                }
            },
            // Q12 (T/F)
            new DoctorQuizQuestionData {
                questionText = "True or False: The lesson notes state that 'Asthma' is pronounced as 'Azma'.",
                correctOption = "True",
                distractorOptions = new string[] {
                    "False",
                    "Only in formal medical speech",
                    "Neither true nor false"
                }
            }
        };
    }

    private IEnumerator StartWithIntroAudioRoutine() {
        isProcessingInput = true;

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            float delay = narratorSpeech.length > 0 ? narratorSpeech.length : 2.5f;
            yield return new WaitForSeconds(delay);
        }

        StartQuiz();
    }

    public void StartQuiz() {
        currentRoundIndex = 0;
        score = 0;
        isProcessingInput = false;

        if (resultPanel != null) resultPanel.SetActive(false);

        PlayCurrentRound();
    }

    private void PlayCurrentRound() {
        if (questions == null || questions.Length == 0) {
            Debug.LogWarning("[Q01 Quiz] Questions array is empty.");
            return;
        }

        if (currentRoundIndex >= questions.Length) {
            CompleteQuiz();
            return;
        }

        isProcessingInput = false;
        currentRoundHasRetried = false;
        EnsureHeaderAndTitle();
        ResetOptionVisuals();

        DoctorQuizQuestionData q = questions[currentRoundIndex];

        if (progressTMP != null) {
            progressTMP.text = $"Question {currentRoundIndex + 1}/{questions.Length}";
        }

        if (questionTextTMP != null) {
            questionTextTMP.text = q.questionText;
        }

        // Build 4 choices (1 correct + 3 distractors)
        List<string> choices = new List<string> { q.correctOption };
        if (q.distractorOptions != null) {
            foreach (var d in q.distractorOptions) {
                if (!string.IsNullOrEmpty(d)) choices.Add(d);
            }
        }

        while (choices.Count < 4) {
            choices.Add("Option");
        }

        // Distribute correct option across buttons A, B, C, D (0, 1, 2, 3)
        int targetCorrectIndex = currentRoundIndex % 4;
        string correctVal = choices[0];

        System.Random rng = new System.Random(currentRoundIndex * 17 + 43);
        for (int i = choices.Count - 1; i > 0; i--) {
            int k = rng.Next(i + 1);
            string tmpStr = choices[i];
            choices[i] = choices[k];
            choices[k] = tmpStr;
        }

        int actualCorrectIdx = choices.IndexOf(correctVal);
        if (actualCorrectIdx != -1 && actualCorrectIdx != targetCorrectIndex) {
            string tmpStr = choices[targetCorrectIndex];
            choices[targetCorrectIndex] = correctVal;
            choices[actualCorrectIdx] = tmpStr;
        }

        currentRoundShuffledChoices = choices.ToArray();
        currentRoundCorrectChoiceIndex = targetCorrectIndex;

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    if (i < currentRoundShuffledChoices.Length) {
                        optionButtons[i].gameObject.SetActive(true);
                        SetButtonText(optionButtons[i], currentRoundShuffledChoices[i]);
                    } else {
                        optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.gameObject.SetActive(true);
        }
    }

    private void SetButtonText(Button btn, string textValue) {
        if (btn == null) return;
        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null) {
            tmp.text = textValue;
            tmp.enabled = true;
            tmp.gameObject.SetActive(true);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 16;
            tmp.fontSizeMax = 24;
        } else {
            UnityEngine.UI.Text txt = btn.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (txt != null) {
                txt.text = textValue;
                txt.enabled = true;
                txt.gameObject.SetActive(true);
                txt.alignment = TextAnchor.MiddleCenter;
            }
        }
    }

    private void ResetOptionVisuals() {
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    optionButtons[i].transform.DOKill(true);
                    optionButtons[i].interactable = true;

                    if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                        optionImages[i].color = defaultChipColor;
                    }
                }
            }
        }
    }

    private void OnOptionSelected(int index) {
        if (isProcessingInput) return;
        if (optionButtons == null || index >= optionButtons.Length || optionButtons[index] == null) return;

        bool isCorrect = (index == currentRoundCorrectChoiceIndex);
        Button btn = optionButtons[index];
        Image img = optionImages != null && index < optionImages.Length ? optionImages[index] : null;

        if (isCorrect) {
            isProcessingInput = true;
            score++;

            if (img != null) img.color = correctColor;
            if (btn != null) btn.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            StartCoroutine(NextRoundRoutine());
        } else {
            if (img != null) img.color = wrongColor;
            if (btn != null) {
                btn.transform.DOShakePosition(0.4f, new Vector3(15f, 0f, 0f), 10, 90, false, true);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (!currentRoundHasRetried) {
                currentRoundHasRetried = true;
                btn.interactable = false;
            } else {
                if (currentRoundCorrectChoiceIndex >= 0 && currentRoundCorrectChoiceIndex < optionImages.Length) {
                    if (optionImages[currentRoundCorrectChoiceIndex] != null) {
                        optionImages[currentRoundCorrectChoiceIndex].color = correctColor;
                    }
                }
                isProcessingInput = true;
                StartCoroutine(NextRoundRoutine());
            }
        }
    }

    private IEnumerator NextRoundRoutine() {
        yield return new WaitForSeconds(1.2f);
        currentRoundIndex++;
        PlayCurrentRound();
    }

    private void OnReplayAudioClicked() {
        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        }
    }

    private void CompleteQuiz() {
        isProcessingInput = true;

        bool passed = (score >= 9);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Score: {score} / {questions.Length}";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed
                    ? "<color=#248838><b>BRILLIANT WORK!</b>\nYou mastered the clinic consultation and symptom terms!</color>"
                    : "<color=#B52626><b>KEEP PRACTICING!</b>\nScore 9/12 to unlock your reward.</color>";
            }
        }

        if (passed) {
            if (nextButton != null) nextButton.gameObject.SetActive(true);
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }
    }

    private void OnRetryButtonClicked() {
        StartQuiz();
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Quiz;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
