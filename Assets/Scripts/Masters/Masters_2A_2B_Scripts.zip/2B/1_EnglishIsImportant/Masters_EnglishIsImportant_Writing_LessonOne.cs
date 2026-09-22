using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class Masters_EnglishIsImportant_Writing_LessonOne : Masters_Lesson {

[System.Serializable]
public class WritingW01ItemData {
    public string sentenceWithBlank;   // e.g. "Would/Could you _______ what you said, please? [ask to repeat]"
    public string acceptedAnswer;      // e.g. "repeat"
    public string[] distractorAnswers; // 3 distractor choices
}

    [Header("W01 12 Verbatim Cloze Items")]
    [SerializeField]
    private WritingW01ItemData[] items;

    [Header("UI References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI sentenceTextTMP;
    [SerializeField]
    private TextMeshProUGUI hintTextTMP;

    [Header("Input Controls")]
    [SerializeField]
    private TMP_InputField answerInputField;
    [SerializeField]
    private Button submitBtn;

    [Header("4 Option Choice Buttons")]
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
    [Range(0, 9)]
    public int editorPreviewItem = 0;

    [Header("Colors & Styling")]
    [SerializeField]
    private Color defaultChipColor = new Color(0.14f, 0.38f, 0.58f, 1f); // Medium Blue #235E8A
    [SerializeField]
    private Color correctColor = new Color(0.14f, 0.53f, 0.22f, 1f);     // Emerald Green #248838
    [SerializeField]
    private Color wrongColor = new Color(0.71f, 0.15f, 0.15f, 1f);       // Crimson Red #B52626

    private int currentItemIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;
    private bool currentItemHasRetried = false;

    private Image[] optionImages;
    private Color[] originalOptionColors;
    private string[] currentItemShuffledChoices;
    private int currentItemCorrectChoiceIndex;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Writing;

        PurgeLegacyChildren();
        AutoBindReferences();

        if (submitBtn != null) {
            submitBtn.onClick.RemoveAllListeners();
            submitBtn.onClick.AddListener(OnSubmitButtonClicked);
        }

        if (answerInputField != null) {
            answerInputField.onSubmit.RemoveAllListeners();
            answerInputField.onSubmit.AddListener((val) => OnSubmitButtonClicked());
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

        PurgeLegacyChildren();
        EnsureHeaderAndTitle();

        if (items == null || items.Length == 0) {
            PopulateFailsafeItems();
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

        if (items == null || items.Length == 0) {
            PopulateFailsafeItems();
        }

        if (items == null || items.Length == 0) return;

        int idx = Mathf.Clamp(editorPreviewItem, 0, Mathf.Min(9, items.Length - 1));
        WritingW01ItemData item = items[idx];

        if (progressTMP != null) progressTMP.text = $"Item {idx + 1}/10";
        if (sentenceTextTMP != null) sentenceTextTMP.text = item.sentenceWithBlank;
        if (titleTMP != null) titleTMP.text = "W01 Complete the Phrase";
        if (headerTMP != null) headerTMP.text = "✍️ WRITING BRANCH (Post Office)";

        if (optionButtons != null && optionButtons.Length >= 4) {
            string[] previewChoices = new string[4];
            previewChoices[0] = item.acceptedAnswer;
            previewChoices[1] = (item.distractorAnswers != null && item.distractorAnswers.Length > 0) ? item.distractorAnswers[0] : "louder";
            previewChoices[2] = (item.distractorAnswers != null && item.distractorAnswers.Length > 1) ? item.distractorAnswers[1] : "understand";
            previewChoices[3] = (item.distractorAnswers != null && item.distractorAnswers.Length > 2) ? item.distractorAnswers[2] : "explain";

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
            headerTMP.text = "✍️ WRITING BRANCH (Post Office)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "W01 Complete the Phrase";
        }
    }

    private void AutoBindReferences() {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> chipBtns = new List<Button>();

        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("optionbutton") || n.Contains("optionchip") || n.Contains("chip") || n.Contains("word")) {
                chipBtns.Add(btn);
            } else if (submitBtn == null && (n.Contains("submit") || n.Contains("check"))) {
                submitBtn = btn;
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

        if (answerInputField == null) {
            answerInputField = GetComponentInChildren<TMP_InputField>(true);
        }

        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (progressTMP == null && (n.Contains("progresstext") || n.Contains("progress") || n.Contains("counter"))) progressTMP = tmp;
            else if (sentenceTextTMP == null && (n.Contains("sentencetext") || n.Contains("sentence") || n.Contains("phrase") || n.Contains("question"))) sentenceTextTMP = tmp;
            else if (hintTextTMP == null && n.Contains("hint")) hintTextTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
        }
    }

    private void PopulateFailsafeItems() {
        (string sentence, string answer, string[] distractors)[] sampleDefs = new (string, string, string[])[] {
            ("Would/Could you _______ what you said, please? [ask to repeat]", "repeat", new string[] { "louder", "understand", "explain" }),
            ("Can you speak _______, please? [ask to repeat]", "louder", new string[] { "faster", "softer", "repeat" }),
            ("I'm not sure I _______ what you mean. [ask to repeat]", "understand", new string[] { "repeat", "know", "explain" }),
            ("Can you _______ it again, please? [ask to repeat]", "explain", new string[] { "repeat", "say", "speak" }),
            ("Do you know what I _______ ? [check them]", "mean", new string[] { "say", "words", "myself" }),
            ("Do I make _______ clear? [check them]", "myself", new string[] { "mean", "words", "clear" }),
            ("Does that _______......? [check me]", "mean", new string[] { "make", "words", "clear" }),
            ("In other _______...... [another way]", "words", new string[] { "ways", "things", "mean" }),
            ("Let me make it _______. What I was saying is............ [another way]", "clear", new string[] { "words", "mean", "myself" }),
            ("Cannot = _______ [contraction]", "can't", new string[] { "willn't", "wont", "cannot" }),
            ("They have = _______ [contraction]", "they've", new string[] { "Ive", "they'll", "they're" }),
            ("End a phone call = _______ [p.6 pair]", "hang up", new string[] { "look out", "put on", "look forward" })
        };

        items = new WritingW01ItemData[sampleDefs.Length];
        for (int i = 0; i < sampleDefs.Length; i++) {
            items[i] = new WritingW01ItemData {
                sentenceWithBlank = sampleDefs[i].sentence,
                acceptedAnswer = sampleDefs[i].answer,
                distractorAnswers = sampleDefs[i].distractors
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
        currentItemIndex = 0;
        score = 0;
        isProcessingInput = false;

        if (resultPanel != null) resultPanel.SetActive(false);

        PlayCurrentItem();
    }

    private void PlayCurrentItem() {
        if (items == null || items.Length == 0) {
            Debug.LogWarning("[W01 Writing] items array is empty.");
            return;
        }

        if (currentItemIndex >= 10 || currentItemIndex >= items.Length) {
            CompleteLesson();
            return;
        }

        isProcessingInput = false;
        currentItemHasRetried = false;
        EnsureHeaderAndTitle();
        ResetOptionVisuals();

        WritingW01ItemData item = items[currentItemIndex];

        if (progressTMP != null) {
            progressTMP.text = $"Item {currentItemIndex + 1}/10";
        }

        if (sentenceTextTMP != null) {
            sentenceTextTMP.text = item.sentenceWithBlank;
        }

        if (hintTextTMP != null) {
            hintTextTMP.text = "";
        }

        if (answerInputField != null) {
            answerInputField.text = "";
            answerInputField.interactable = true;
        }

        // Build 4 choices (1 correct + 3 distractors)
        List<string> choices = new List<string>();
        choices.Add(item.acceptedAnswer);
        if (item.distractorAnswers != null) {
            foreach (var d in item.distractorAnswers) {
                if (!string.IsNullOrEmpty(d)) choices.Add(d);
            }
        }

        while (choices.Count < 4) {
            choices.Add("Option");
        }

        // Randomize option positions (A, B, C, D) deterministically per item
        int targetCorrectIndex = currentItemIndex % 4;
        string correctVal = choices[0];

        System.Random rng = new System.Random(currentItemIndex + 83);
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

        currentItemShuffledChoices = choices.ToArray();
        currentItemCorrectChoiceIndex = targetCorrectIndex;

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    if (i < currentItemShuffledChoices.Length) {
                        optionButtons[i].gameObject.SetActive(true);
                        SetButtonText(optionButtons[i], currentItemShuffledChoices[i]);
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

        string selectedText = currentItemShuffledChoices != null && index < currentItemShuffledChoices.Length ? currentItemShuffledChoices[index] : "";
        if (answerInputField != null) {
            answerInputField.text = selectedText;
        }

        EvaluateAnswer(selectedText, index);
    }

    private void OnSubmitButtonClicked() {
        if (isProcessingInput) return;
        string typed = (answerInputField != null) ? answerInputField.text.Trim() : "";
        EvaluateAnswer(typed, -1);
    }

    private void EvaluateAnswer(string inputAnswer, int optionBtnIndex) {
        if (isProcessingInput || currentItemIndex >= items.Length) return;

        WritingW01ItemData item = items[currentItemIndex];
        string cleanUser = inputAnswer.Trim().ToLower();
        string cleanTarget = item.acceptedAnswer.Trim().ToLower();

        bool isCorrect = (cleanUser == cleanTarget);

        if (isCorrect) {
            isProcessingInput = true;
            score++;

            if (optionBtnIndex >= 0 && optionImages != null && optionBtnIndex < optionImages.Length && optionImages[optionBtnIndex] != null) {
                optionImages[optionBtnIndex].color = correctColor;
            }

            if (sentenceTextTMP != null) {
                sentenceTextTMP.text = item.sentenceWithBlank.Replace("_______", $"<b><color=#4CD964>{item.acceptedAnswer}</color></b>");
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            StartCoroutine(NextItemRoutine());
        } else {
            if (optionBtnIndex >= 0 && optionImages != null && optionBtnIndex < optionImages.Length && optionImages[optionBtnIndex] != null) {
                optionImages[optionBtnIndex].color = wrongColor;
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (!currentItemHasRetried) {
                currentItemHasRetried = true;
                if (hintTextTMP != null && !string.IsNullOrEmpty(item.acceptedAnswer)) {
                    hintTextTMP.text = $"Hint: starts with '{item.acceptedAnswer[0]}'...";
                }
            } else {
                if (sentenceTextTMP != null) {
                    sentenceTextTMP.text = item.sentenceWithBlank.Replace("_______", $"<b><color=#FF3B30>{item.acceptedAnswer}</color></b>");
                }
                isProcessingInput = true;
                StartCoroutine(NextItemRoutine());
            }
        }
    }

    private IEnumerator NextItemRoutine() {
        yield return new WaitForSeconds(1.2f);
        currentItemIndex++;
        PlayCurrentItem();
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
            resultScoreTMP.text = $"{score}/10 Items";
        }

        bool passed = score >= 8; // 8/10 pass threshold

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed ? "EXCELLENT! WRITING COMPLETE!" : "TRY AGAIN TO PASS (8/10 REQUIRED)";
        }

        if (retryBtn != null) {
            retryBtn.gameObject.SetActive(!passed);
        }

        if (passed) {
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            Debug.Log($"[W01 Writing] Passed with score {score}/10!");
        } else {
            Debug.Log($"[W01 Writing] Failed with score {score}/10. Retry available.");
        }
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}
