using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class Masters_EnglishIsImportant_Quiz_LessonOne : Masters_Lesson {


[System.Serializable]
public class QuizQ01QuestionData {
    public string questionText;        // e.g. "'Would/Could you say that again, please?' — what is the speaker doing?"
    public string correctOption;       // e.g. "ASKING SOMEONE TO REPEAT"
    public string[] distractorOptions; // 3 distractor choices
}

    [Header("Q01 12 Mixed-Format Questions")]
    [SerializeField]
    private QuizQ01QuestionData[] questions;

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

        PurgeLegacyChildren();
        EnsureHeaderAndTitle();

        if (questions == null || questions.Length == 0) {
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

        if (questions == null || questions.Length == 0) {
            PopulateFailsafeQuestions();
        }

        if (questions == null || questions.Length == 0) return;

        int idx = Mathf.Clamp(editorPreviewRound, 0, questions.Length - 1);
        QuizQ01QuestionData q = questions[idx];

        if (progressTMP != null) progressTMP.text = $"Question {idx + 1}/{questions.Length}";
        if (questionTextTMP != null) questionTextTMP.text = q.questionText;
        if (titleTMP != null) titleTMP.text = "Q01 Town Hall Quiz — English Is Important";
        if (headerTMP != null) headerTMP.text = "QUIZ BRANCH (Town Hall)";

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
            headerTMP.text = "QUIZ BRANCH (Town Hall)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "Q01 Town Hall Quiz — English Is Important";
        }
    }

    private void AutoBindReferences() {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> chipBtns = new List<Button>();

        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("optionbutton") || n.Contains("optionchip") || n.Contains("chip") || n.Contains("answer")) {
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
            if (progressTMP == null && (n.Contains("progresstext") || n.Contains("progress") || n.Contains("counter"))) {
                progressTMP = tmp;
            } else if (questionTextTMP == null && (n.Contains("questiontext") || n.Contains("question") || n.Contains("cardtext"))) {
                questionTextTMP = tmp;
            } else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) {
                titleTMP = tmp;
            } else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) {
                headerTMP = tmp;
            }
        }
    }

    private void PopulateFailsafeQuestions() {
        (string qText, string correct, string[] distractors)[] sampleDefs = new (string, string, string[])[] {
            ("'Would/Could you say that again, please?' — what is the speaker doing?", "ASKING SOMEONE TO REPEAT", new string[] { "CHECKING THAT YOU HAVE UNDERSTOOD", "SAYING IT ANOTHER WAY", "EXPLAINING A REASON" }),
            ("'Do you know what I mean?' — what is the speaker doing?", "CHECKING THAT YOU HAVE UNDERSTOOD THEM", new string[] { "ASKING SOMEONE TO REPEAT", "SAYING IT ANOTHER WAY", "MAKING AN OFFER" }),
            ("End a phone call → ?", "Hang up", new string[] { "Put on", "Look out", "Return" }),
            ("Be excited about the future → ?", "Look forward to", new string[] { "Go through", "Point out", "Bring up" }),
            ("Can you speak ________, please?", "louder", new string[] { "faster", "softer", "longer" }),
            ("In other ________ ......", "words", new string[] { "ways", "things", "meaning" }),
            ("Will not = ________", "won't", new string[] { "willn't", "wont", "wil'nt" }),
            ("I have = ________", "I've", new string[] { "Ive", "I'hve", "I'ha" }),
            ("Which of these is a reason for learning English from the book?", "To surf the net.", new string[] { "To play sports.", "To cook dinner.", "To sleep early." }),
            ("Not a way of asking someone to repeat: 'Pardon.' · 'I'm sorry, what did you say?' · 'Can you explain it again, please?' · 'Got it?'", "Got it?", new string[] { "Pardon.", "I'm sorry, what did you say?", "Can you explain it again, please?" }),
            ("Put the repair in order: ___ say it another way · ___ ask them to repeat · ___ check what they meant", "ask to repeat, check what they meant, say it another way", new string[] { "say it another way, ask to repeat, check what they meant", "check what they meant, say it another way, ask to repeat", "ask to repeat, say it another way, check what they meant" }),
            ("True or False: 'They will' becomes 'they'll'.", "True", new string[] { "False", "N/A", "Not applicable" })
        };

        questions = new QuizQ01QuestionData[sampleDefs.Length];
        for (int i = 0; i < sampleDefs.Length; i++) {
            questions[i] = new QuizQ01QuestionData {
                questionText = sampleDefs[i].qText,
                correctOption = sampleDefs[i].correct,
                distractorOptions = sampleDefs[i].distractors
            };
        }
    }

    private IEnumerator StartWithIntroAudioRoutine() {
        isProcessingInput = true;

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            float delay = narratorSpeech.length > 0 ? narratorSpeech.length : 2.5f;
            yield return new WaitForSeconds(delay);
        }

        StartLesson();
    }

    public void StartLesson() {
        currentRoundIndex = 0;
        score = 0;
        isProcessingInput = false;

        if (resultPanel != null) resultPanel.SetActive(false);

        PlayCurrentRound();
    }

    private void PlayCurrentRound() {
        if (questions == null || questions.Length == 0) {
            Debug.LogWarning("[Q01 Quiz] questions array is empty.");
            return;
        }

        if (currentRoundIndex >= questions.Length) {
            CompleteLesson();
            return;
        }

        isProcessingInput = false;
        currentRoundHasRetried = false;
        EnsureHeaderAndTitle();
        ResetOptionVisuals();

        QuizQ01QuestionData q = questions[currentRoundIndex];

        if (progressTMP != null) {
            progressTMP.text = $"Question {currentRoundIndex + 1}/{questions.Length}";
        }

        if (questionTextTMP != null) {
            questionTextTMP.text = q.questionText;
        }

        // Build 4 choices (1 correct + 3 distractors)
        List<string> choices = new List<string>();
        choices.Add(q.correctOption);
        if (q.distractorOptions != null) {
            foreach (var d in q.distractorOptions) {
                if (!string.IsNullOrEmpty(d)) choices.Add(d);
            }
        }

        while (choices.Count < 4) {
            choices.Add("Option");
        }

        // Randomize option positions (A, B, C, D) deterministically per round
        int targetCorrectIndex = currentRoundIndex % 4;
        string correctVal = choices[0];

        System.Random rng = new System.Random(currentRoundIndex + 37);
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
            tmp.alignment = TextAlignmentOptions.Left;
        } else {
            UnityEngine.UI.Text txt = btn.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (txt != null) {
                txt.text = textValue;
                txt.enabled = true;
                txt.gameObject.SetActive(true);
                txt.alignment = TextAnchor.MiddleLeft;
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

    private void OnRetryButtonClicked() {
        StartLesson();
    }

    private void CompleteLesson() {
        isProcessingInput = true;

        if (resultPanel != null) {
            resultPanel.SetActive(true);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"{score}/{questions.Length}";
        }

        bool passed = score >= 9; // 9/12 pass threshold

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed ? "CONGRATULATIONS! TOWN HALL QUIZ PASSED!" : "TRY AGAIN TO PASS (9/12 REQUIRED)";
        }

        if (retryBtn != null) {
            retryBtn.gameObject.SetActive(!passed);
        }

        if (passed) {
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            Debug.Log($"[Q01 Quiz] Passed with score {score}/{questions.Length}!");
        } else {
            Debug.Log($"[Q01 Quiz] Failed with score {score}/{questions.Length}. Retry available.");
        }
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}
