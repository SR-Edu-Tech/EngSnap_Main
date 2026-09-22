using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// Q01 Summit Quiz — Question Tags (Book 2B Unit 7).
/// 12 mixed-format questions covering tag polarity, helping verbs, pronouns, category lists, and error spotting.
/// Pass mark = 9 / 12.
/// Locked input during intro audio, enabling full gameplay upon audio completion.
/// </summary>
public class Masters_QuestionTags_Quiz_LessonOne : Masters_Lesson {

[System.Serializable]
public class QuestionTags_QuizQuestionData {
    public string questionText;        // Question prompt
    public string correctOption;       // Correct answer
    public string[] distractorOptions; // 3 distractor choices
    public AudioClip questionAudio;    // Spoken question audio
}

    [Header("Q01 12 Mixed-Format Questions")]
    [SerializeField]
    private QuestionTags_QuizQuestionData[] questions;

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

        if (questions == null || questions.Length == 0 || questions[0] == null || string.IsNullOrEmpty(questions[0].questionText) || !questions[0].questionText.Contains("busy")) {
            PopulateDefaultQuestions();
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

        if (questions == null || questions.Length == 0 || questions[0] == null || string.IsNullOrEmpty(questions[0].questionText) || !questions[0].questionText.Contains("busy")) {
            PopulateDefaultQuestions();
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

        if (questions == null || questions.Length == 0 || questions[0] == null || string.IsNullOrEmpty(questions[0].questionText) || !questions[0].questionText.Contains("busy")) {
            PopulateDefaultQuestions();
        }

        if (questions == null || questions.Length == 0) return;

        int idx = Mathf.Clamp(editorPreviewRound, 0, questions.Length - 1);
        QuestionTags_QuizQuestionData q = questions[idx];

        if (progressTMP != null) progressTMP.text = $"Question {idx + 1}/12";
        if (questionTextTMP != null) questionTextTMP.text = q.questionText;
        if (titleTMP != null) titleTMP.text = "Q01 Summit Quiz \u2014 Question Tags";
        if (headerTMP != null) headerTMP.text = "QUESTION TAGS";

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
                if (Application.isPlaying) Destroy(lTrans.gameObject);
                else DestroyImmediate(lTrans.gameObject);
            }
        }

        for (int i = transform.childCount - 1; i >= 0; i--) {
            Transform child = transform.GetChild(i);
            string cName = child.name.ToLower();
            if (cName.Contains("statement") || cName.Contains("words") || cName.Contains("fillin")) {
                child.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch") ?? transform.Find("UnitHeading");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "QUESTION TAGS";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "Q01 Summit Quiz \u2014 Question Tags";
        }
    }

    public void AutoBindReferences() {
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
            } else if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading"))) {
                headerTMP = tmp;
            }
        }
    }

    public void PopulateDefaultQuestions() {
        string audioDir = "Assets/Audio/2B/7_Question_Tags/Quiz/";

        questions = new QuestionTags_QuizQuestionData[] {
            // Q1 (Tag choice)
            new QuestionTags_QuizQuestionData {
                questionText = "He is very busy, _____",
                correctOption = "isn't he?",
                distractorOptions = new string[] { "is he?", "doesn't he?", "wasn't he?" },
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "qt_q01_he_is_busy.mp3")
#endif
            },
            // Q2 (Tag choice)
            new QuestionTags_QuizQuestionData {
                questionText = "They weren't late, _____",
                correctOption = "were they?",
                distractorOptions = new string[] { "weren't they?", "are they?", "did they?" },
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "qt_q02_they_werent_late.mp3")
#endif
            },
            // Q3 (Type the tag / MCQ)
            new QuestionTags_QuizQuestionData {
                questionText = "You speak English, _____",
                correctOption = "don't you?",
                distractorOptions = new string[] { "do you?", "aren't you?", "haven't you?" },
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "qt_q03_you_speak_english.mp3")
#endif
            },
            // Q4 (Type the tag / MCQ)
            new QuestionTags_QuizQuestionData {
                questionText = "You hadn't done it before, _____",
                correctOption = "had you?",
                distractorOptions = new string[] { "hadn't you?", "did you?", "have you?" },
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "qt_q04_you_hadnt_done_it.mp3")
#endif
            },
            // Q5 (Category)
            new QuestionTags_QuizQuestionData {
                questionText = "'You must be patient, mustn't you?' - Which of the book's two lists?",
                correctOption = "NEGATIVE TAGS (p.30)",
                distractorOptions = new string[] { "POSITIVE TAGS (p.29)", "Irregular Tags", "Imperative Tags" },
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "qt_q05_you_must_be_patient.mp3")
#endif
            },
            // Q6 (Category)
            new QuestionTags_QuizQuestionData {
                questionText = "'You shouldn't go there, should you?' - Which list?",
                correctOption = "POSITIVE TAGS (p.29)",
                distractorOptions = new string[] { "NEGATIVE TAGS (p.30)", "Modal Inversions", "Question Words" },
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "qt_q06_you_shouldnt_go.mp3")
#endif
            },
            // Q7 (Error)
            new QuestionTags_QuizQuestionData {
                questionText = "What is wrong with: 'You are a student, are you?'",
                correctOption = "The polarity - the tag should be negative: aren't you?",
                distractorOptions = new string[] {
                    "The pronoun - it should be aren't they?",
                    "The verb tense - it should be were you?",
                    "The punctuation - tags cannot follow a comma."
                },
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "qt_q07_error_polarity.mp3")
#endif
            },
            // Q8 (Error)
            new QuestionTags_QuizQuestionData {
                questionText = "What is wrong with: 'He studies French, isn't he?'",
                correctOption = "The helping verb - it should be doesn't he?",
                distractorOptions = new string[] {
                    "The pronoun - it should be isn't she?",
                    "The tag should be positive: is he?",
                    "The verb tense - it should be didn't he?"
                },
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "qt_q08_error_verb.mp3")
#endif
            },
            // Q9 (Error)
            new QuestionTags_QuizQuestionData {
                questionText = "What is wrong with: 'They were surprised, weren't you?'",
                correctOption = "The pronoun - it should be weren't they?",
                distractorOptions = new string[] {
                    "The helping verb - it should be didn't they?",
                    "The polarity - the tag should be were you?",
                    "The adjective - surprised cannot take a tag."
                },
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "qt_q09_error_pronoun.mp3")
#endif
            },
            // Q10 (MCQ)
            new QuestionTags_QuizQuestionData {
                questionText = "Which tag goes with: 'You couldn't do it for me, _____?'",
                correctOption = "could you?",
                distractorOptions = new string[] { "couldn't you?", "can you?", "would you?" },
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "qt_q10_you_couldnt_do_it.mp3")
#endif
            },
            // Q11 (Match)
            new QuestionTags_QuizQuestionData {
                questionText = "You have studied all week, _____",
                correctOption = "haven't you?",
                distractorOptions = new string[] { "have you?", "didn't you?", "don't you?" },
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "qt_q11_you_have_studied.mp3")
#endif
            },
            // Q12 (T/F)
            new QuestionTags_QuizQuestionData {
                questionText = "True or False: A negative statement takes a positive tag.",
                correctOption = "True",
                distractorOptions = new string[] { "False", "Only with modal verbs", "Only in formal English" },
#if UNITY_EDITOR
                questionAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "qt_q12_true_false.mp3")
#endif
            }
        };
    }

    private IEnumerator StartWithIntroAudioRoutine() {
        isProcessingInput = true;
        SetOptionButtonsInteractable(false);

        AudioClip introClip = narratorSpeech;
#if UNITY_EDITOR
        if (introClip == null) {
            introClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/7_Question_Tags/Quiz/qt_quiz_intro.mp3");
        }
#endif

        if (introClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(introClip);
            float delay = introClip.length > 0 ? introClip.length + 0.2f : 2.5f;
            yield return new WaitForSeconds(delay);
        } else {
            yield return new WaitForSeconds(1.0f);
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
            Debug.LogWarning("[QuestionTags Quiz] Questions array is empty.");
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

        QuestionTags_QuizQuestionData q = questions[currentRoundIndex];

        if (progressTMP != null) {
            progressTMP.text = $"Question {currentRoundIndex + 1}/{questions.Length}";
        }

        if (questionTextTMP != null) {
            questionTextTMP.text = q.questionText;
        }

        // Play question audio if available
        if (q.questionAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(q.questionAudio);
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

        System.Random rng = new System.Random(currentRoundIndex * 23 + 17);
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

        SetOptionButtonsInteractable(true);

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
            tmp.fontSizeMin = 14;
            tmp.fontSizeMax = 22;
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

    private void SetOptionButtonsInteractable(bool interactable) {
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    optionButtons[i].interactable = interactable;
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
        if (currentRoundIndex >= 0 && currentRoundIndex < questions.Length) {
            AudioClip clip = questions[currentRoundIndex].questionAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
                return;
            }
        }

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
                    ? "<color=#248838><b>BRILLIANT WORK!</b>\nYou mastered question tags and polarity rules!</color>"
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
