using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class Masters_EnglishIsImportant_Writing_LessonTwo : Masters_Lesson {

[System.Serializable]
public class WritingW02PostcardData {
    public string scenarioTitle;       // e.g. "Postcard 1: Pen-friend abroad (REASONS)"
    public string scenarioPrompt;      // e.g. "A pen-friend abroad asks why you are learning English — choose the THREE reasons from the book!"
    public string fittingAnswer;       // Correct verbatim response
    public string[] distractorAnswers; // 2 distractors
    public AudioClip promptAudioClip;  // Prompt narration
    public AudioClip ariaReadoutClip;  // ARIA readout on pass
}

    [Header("W02 3 Postcards")]
    [SerializeField]
    private WritingW02PostcardData[] postcards;

    [Header("UI References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI scenarioTextTMP;
    [SerializeField]
    private TextMeshProUGUI hintTextTMP;

    [Header("Input Controls")]
    [SerializeField]
    private TMP_InputField answerInputField;
    [SerializeField]
    private Button submitBtn;

    [Header("3 Option Choice Buttons")]
    [SerializeField]
    private Button[] optionButtons; // OptionButton_01 .. OptionButton_03

    [Header("Audio Controls")]
    [SerializeField]
    private Button replayAudioBtn;

    [Header("Navigation")]
    [SerializeField]
    private Button backButton;

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
    [Range(0, 2)]
    public int editorPreviewPostcard = 0;

    [Header("Colors & Styling")]
    [SerializeField]
    private Color defaultChipColor = new Color(0.14f, 0.38f, 0.58f, 1f); // Medium Blue #235E8A
    [SerializeField]
    private Color correctColor = new Color(0.14f, 0.53f, 0.22f, 1f);     // Emerald Green #248838
    [SerializeField]
    private Color wrongColor = new Color(0.71f, 0.15f, 0.15f, 1f);       // Crimson Red #B52626

    private int currentPostcardIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;
    private bool currentPostcardHasRetried = false;

    private Image[] optionImages;
    private Color[] originalOptionColors;
    private string[] currentPostcardShuffledChoices;
    private int currentPostcardCorrectChoiceIndex;

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

        if (backButton != null) {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
            nextButton.interactable = false;
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();

        PurgeLegacyChildren();
        EnsureHeaderAndTitle();

        if (postcards == null || postcards.Length == 0) {
            PopulateFailsafePostcards();
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

        if (postcards == null || postcards.Length == 0) {
            PopulateFailsafePostcards();
        }

        if (postcards == null || postcards.Length == 0) return;

        int idx = Mathf.Clamp(editorPreviewPostcard, 0, postcards.Length - 1);
        WritingW02PostcardData card = postcards[idx];

        if (progressTMP != null) progressTMP.text = $"Postcard {idx + 1}/3";
        if (scenarioTextTMP != null) scenarioTextTMP.text = $"<b>{card.scenarioTitle}</b>\n{card.scenarioPrompt}";
        if (titleTMP != null) titleTMP.text = "W02 Write It — Reasons and Repairs";
        if (headerTMP != null) headerTMP.text = "✍️ WRITING BRANCH (Post Office)";

        if (optionButtons != null && optionButtons.Length >= 3) {
            string[] previewChoices = new string[3];
            previewChoices[0] = card.fittingAnswer;
            previewChoices[1] = (card.distractorAnswers != null && card.distractorAnswers.Length > 0) ? card.distractorAnswers[0] : "Option B";
            previewChoices[2] = (card.distractorAnswers != null && card.distractorAnswers.Length > 1) ? card.distractorAnswers[1] : "Option C";

            for (int i = 0; i < 3; i++) {
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
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition"
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
            headerTMP.text = "✍️ WRITING BRANCH (Post Office)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "W02 Write It — Reasons and Repairs";
        }
    }

    private void AutoBindReferences() {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> chipBtns = new List<Button>();

        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("optionbutton") || n.Contains("optionchip") || n.Contains("chip") || n.Contains("postcard")) {
                chipBtns.Add(btn);
            } else if (submitBtn == null && (n.Contains("submit") || n.Contains("check"))) {
                submitBtn = btn;
            } else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("repeat") || n.Contains("audio"))) {
                replayAudioBtn = btn;
            } else if (retryBtn == null && n.Contains("retry")) {
                retryBtn = btn;
            } else if (nextButton == null && n.Contains("next")) {
                nextButton = btn;
            } else if (backButton == null && n.Contains("back")) {
                backButton = btn;
            }
        }

        if ((optionButtons == null || optionButtons.Length == 0 || System.Array.Exists(optionButtons, b => b == null)) && chipBtns.Count >= 3) {
            optionButtons = new Button[3];
            for (int i = 0; i < 3; i++) {
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
            else if (scenarioTextTMP == null && (n.Contains("scenariotext") || n.Contains("scenario") || n.Contains("prompt") || n.Contains("postcard"))) scenarioTextTMP = tmp;
            else if (hintTextTMP == null && n.Contains("hint")) hintTextTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
        }
    }

    private void PopulateFailsafePostcards() {
        postcards = new WritingW02PostcardData[] {
            // Postcard 1
            new WritingW02PostcardData {
                scenarioTitle = "Postcard 1: Pen-friend abroad (REASONS)",
                scenarioPrompt = "A pen-friend abroad asks why you are learning English — write THREE different reasons from the book.",
                fittingAnswer = "I want to learn English for a bright future. To read English books. English is a global language.",
                distractorAnswers = new string[] {
                    "I want to watch movies all day. To play games on my phone. English is easy.",
                    "I have no reason to learn English. I do not like reading books."
                }
            },
            // Postcard 2
            new WritingW02PostcardData {
                scenarioTitle = "Postcard 2: Didn't hear teacher (REPAIR)",
                scenarioPrompt = "You didn't hear your teacher's instruction — write ONE phrase asking her to repeat and ONE phrase checking that you understood.",
                fittingAnswer = "I'm sorry, I didn't hear what you said. Could you say it again, please? Do you mean...?",
                distractorAnswers = new string[] {
                    "Stop speaking so quietly. Are you talking to me or someone else?",
                    "I wasn't listening. What did you say?"
                }
            },
            // Postcard 3
            new WritingW02PostcardData {
                scenarioTitle = "Postcard 3: Short & Smart rewrite (REWRITE)",
                scenarioPrompt = "Short & Smart rewrite — rewrite 'I am going to end a phone call and I will return a phone call later.' using contractions and the p.6 phrasal verbs.",
                fittingAnswer = "I'm going to hang up and I'll call back later.",
                distractorAnswers = new string[] {
                    "I will stop talking on the telephone and I shall ring you again tomorrow.",
                    "I'm hanging on the line and I will look into it later."
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

        StartLesson();
    }

    public void StartLesson() {
        currentPostcardIndex = 0;
        score = 0;
        isProcessingInput = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (nextButton != null) nextButton.interactable = false;

        PlayCurrentPostcard();
    }

    private void PlayCurrentPostcard() {
        if (postcards == null || postcards.Length == 0) {
            Debug.LogWarning("[W02 Writing] postcards array is empty.");
            return;
        }

        if (currentPostcardIndex >= postcards.Length) {
            CompleteLesson();
            return;
        }

        isProcessingInput = false;
        currentPostcardHasRetried = false;
        EnsureHeaderAndTitle();
        ResetOptionVisuals();

        WritingW02PostcardData card = postcards[currentPostcardIndex];

        if (progressTMP != null) progressTMP.text = $"Postcard {currentPostcardIndex + 1}/3";
        if (scenarioTextTMP != null) scenarioTextTMP.text = $"<b>{card.scenarioTitle}</b>\n{card.scenarioPrompt}";
        if (hintTextTMP != null) hintTextTMP.text = "";

        if (answerInputField != null) {
            answerInputField.text = "";
            answerInputField.interactable = true;
        }

        // Build 3 choices (1 fitting answer + 2 distractors)
        List<string> choices = new List<string>();
        choices.Add(card.fittingAnswer);
        if (card.distractorAnswers != null) {
            foreach (var d in card.distractorAnswers) {
                if (!string.IsNullOrEmpty(d)) choices.Add(d);
            }
        }

        while (choices.Count < 3) {
            choices.Add("Option");
        }

        // Randomize option positions (A, B, C) deterministically per postcard
        int targetCorrectIndex = currentPostcardIndex % 3;
        string correctVal = choices[0];

        System.Random rng = new System.Random(currentPostcardIndex + 101);
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

        currentPostcardShuffledChoices = choices.ToArray();
        currentPostcardCorrectChoiceIndex = targetCorrectIndex;

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    if (i < currentPostcardShuffledChoices.Length) {
                        optionButtons[i].gameObject.SetActive(true);
                        SetButtonText(optionButtons[i], currentPostcardShuffledChoices[i]);
                    } else {
                        optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.gameObject.SetActive(true);
        }

        // Play prompt audio
        if (card.promptAudioClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(card.promptAudioClip);
        }
    }

    private void SetButtonText(Button btn, string textValue) {
        if (btn == null) return;
        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null) {
            tmp.text = textValue;
            tmp.enabled = true;
            tmp.gameObject.SetActive(true);
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 14f;
            tmp.fontSizeMax = 22f;
            tmp.enableWordWrapping = true;
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

        string selectedText = currentPostcardShuffledChoices != null && index < currentPostcardShuffledChoices.Length ? currentPostcardShuffledChoices[index] : "";
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
        if (isProcessingInput || currentPostcardIndex >= postcards.Length) return;

        WritingW02PostcardData card = postcards[currentPostcardIndex];
        bool isCorrect = (optionBtnIndex == currentPostcardCorrectChoiceIndex) || (inputAnswer.Trim().ToLower() == card.fittingAnswer.Trim().ToLower());

        if (isCorrect) {
            isProcessingInput = true;
            score++;

            if (optionBtnIndex >= 0 && optionImages != null && optionBtnIndex < optionImages.Length && optionImages[optionBtnIndex] != null) {
                optionImages[optionBtnIndex].color = correctColor;
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            StartCoroutine(PostcardPassRoutine(card));
        } else {
            if (optionBtnIndex >= 0 && optionImages != null && optionBtnIndex < optionImages.Length && optionImages[optionBtnIndex] != null) {
                optionImages[optionBtnIndex].color = wrongColor;
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (!currentPostcardHasRetried) {
                currentPostcardHasRetried = true;
                if (hintTextTMP != null) {
                    hintTextTMP.text = "Hint: Check the book's exact phrases and try once more!";
                }
            } else {
                if (currentPostcardCorrectChoiceIndex >= 0 && optionImages != null && currentPostcardCorrectChoiceIndex < optionImages.Length) {
                    if (optionImages[currentPostcardCorrectChoiceIndex] != null) {
                        optionImages[currentPostcardCorrectChoiceIndex].color = correctColor;
                    }
                }
                isProcessingInput = true;
                StartCoroutine(NextPostcardRoutine(1.8f));
            }
        }
    }

    private IEnumerator PostcardPassRoutine(WritingW02PostcardData card) {
        float delay = 1.5f;

        // ARIA reads the postcard back aloud
        if (card.ariaReadoutClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(card.ariaReadoutClip);
            delay = Mathf.Max(card.ariaReadoutClip.length + 0.5f, 2.0f);
        }

        yield return new WaitForSeconds(delay);
        currentPostcardIndex++;
        PlayCurrentPostcard();
    }

    private IEnumerator NextPostcardRoutine(float delay) {
        yield return new WaitForSeconds(delay);
        currentPostcardIndex++;
        PlayCurrentPostcard();
    }

    private void OnReplayAudioClicked() {
        if (currentPostcardIndex < postcards.Length) {
            WritingW02PostcardData card = postcards[currentPostcardIndex];
            if (card.promptAudioClip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(card.promptAudioClip);
                return;
            }
        }

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
            resultScoreTMP.text = $"{score}/3 Postcards";
        }

        bool passed = score >= 2; // 2/3 pass threshold

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed ? "GREAT JOB! WRITING POSTCARDS COMPLETE!" : "TRY AGAIN TO PASS (2/3 REQUIRED)";
        }

        if (retryBtn != null) {
            retryBtn.gameObject.SetActive(!passed);
        }

        if (passed) {
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            Debug.Log($"[W02 Writing] Passed with score {score}/3!");
        } else {
            Debug.Log($"[W02 Writing] Failed with score {score}/3. Retry available.");
        }
    }

    protected void OnBackButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}
