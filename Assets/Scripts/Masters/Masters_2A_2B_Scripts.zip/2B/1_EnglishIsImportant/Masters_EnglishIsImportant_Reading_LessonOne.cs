using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class Masters_EnglishIsImportant_Reading_LessonOne : Masters_Lesson {


[System.Serializable]
public class ReadingR01RoundData {
    public string situationText;
    public string correctReason;
    public string[] distractorReasons; // 3 distractor choices
    public AudioClip situationAudioClip;
}

    [Header("R01 12 Verbatim Rounds")]
    [SerializeField]
    private ReadingR01RoundData[] roundDataArray;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI speechBubbleTMP; // SituationText inside SituationPanel
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI headerTMP;

    [Header("4 Answer Option Buttons (Pre-existing in Scene/Prefab)")]
    [SerializeField]
    private Button[] optionButtons; // OptionButton_01, OptionButton_02, OptionButton_03, OptionButton_04

    [Header("Navigation Buttons")]
    [SerializeField]
    private Button backButton;

    [Header("Audio Controls")]
    [SerializeField]
    private Button replayAudioBtn;

    [Header("Feedback / Hint Panel References")]
    [SerializeField]
    private GameObject hintPanel; // FeedbackPanel
    [SerializeField]
    private TextMeshProUGUI hintCorrectAnswerTMP; // FeedbackText
    [SerializeField]
    private Button gotItBtn;

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
    private Color defaultChipColor = new Color(0.14f, 0.38f, 0.58f, 1f);
    [SerializeField]
    private Color correctColor = new Color(0.14f, 0.53f, 0.22f, 1f);
    [SerializeField]
    private Color wrongColor = new Color(0.71f, 0.15f, 0.15f, 1f);

    private int currentRoundIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;
    private bool currentRoundHasRetried = false;

    private Image[] optionImages;
    private Color[] originalOptionColors;
    private string[] currentRoundShuffledChoices;
    private int currentRoundCorrectChoiceIndex;

    [System.Serializable]
    private struct OptionButtonState {
        public Vector2 anchoredPosition;
        public Vector2 sizeDelta;
        public Vector2 anchorMin;
        public Vector2 anchorMax;
        public Vector2 pivot;
        public Vector3 localScale;
    }

    private OptionButtonState[] initialButtonStates;

    protected override void Awake() {
        topic = Masters_Topic.Reading;
        narratorSpeech = null; // Prevent duplicate audio clash with situation voiceover

        base.Awake();

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

        if (gotItBtn != null) {
            gotItBtn.onClick.RemoveAllListeners();
            gotItBtn.onClick.AddListener(OnGotItButtonClicked);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(OnRetryButtonClicked);
        }

        if (nextButton != null) {
            nextButton.interactable = false;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        if (backButton != null) {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        if (hintPanel != null) {
            hintPanel.SetActive(false);
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }

        CacheOptionButtonStates();
    }

    protected override void Start() {
        // We handle voiceover explicitly to avoid duplicate audio playback
        EnsureNextAndBackButtonWired();
        EnsureAspectRatiosAndAnchorsPreserved();
        EnsureHeaderAndTitle();

        PurgeLegacyChildren();

        if (roundDataArray == null || roundDataArray.Length == 0) {
            PopulateFailsafeRounds();
        }

        StartLesson();
    }

    private void CacheOptionButtonStates() {
        if (optionButtons != null && optionButtons.Length > 0) {
            initialButtonStates = new OptionButtonState[optionButtons.Length];
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    RectTransform rt = optionButtons[i].GetComponent<RectTransform>();
                    if (rt != null) {
                        initialButtonStates[i] = new OptionButtonState {
                            anchoredPosition = rt.anchoredPosition,
                            sizeDelta = rt.sizeDelta,
                            anchorMin = rt.anchorMin,
                            anchorMax = rt.anchorMax,
                            pivot = rt.pivot,
                            localScale = rt.localScale
                        };
                    }
                }
            }
        }
    }

    private void OnValidate() {
#if UNITY_EDITOR
        if (!Application.isPlaying) {
            UnityEditor.EditorApplication.delayCall -= UpdateEditorPreview;
            UnityEditor.EditorApplication.delayCall += UpdateEditorPreview;
        }
#endif
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        if (this == null) return;
        AutoBindReferences();

        if (roundDataArray == null || roundDataArray.Length == 0) {
            PopulateFailsafeRounds();
        }

        if (roundDataArray == null || roundDataArray.Length == 0) return;

        int idx = Mathf.Clamp(editorPreviewRound, 0, roundDataArray.Length - 1);
        ReadingR01RoundData round = roundDataArray[idx];

        if (progressTMP != null) progressTMP.text = $"{idx + 1}/{roundDataArray.Length}";
        if (speechBubbleTMP != null) speechBubbleTMP.text = round.situationText;
        if (titleTMP != null) titleTMP.text = "WHY DO WE LEARN ENGLISH?";
        if (headerTMP != null) headerTMP.text = "READING BRANCH (Library)";

        if (optionButtons != null && optionButtons.Length >= 4) {
            string[] previewChoices = new string[4];
            previewChoices[0] = round.correctReason;
            previewChoices[1] = (round.distractorReasons != null && round.distractorReasons.Length > 0) ? round.distractorReasons[0] : "It is a part of a curriculum.";
            previewChoices[2] = (round.distractorReasons != null && round.distractorReasons.Length > 1) ? round.distractorReasons[1] : "To communicate with friends.";
            previewChoices[3] = (round.distractorReasons != null && round.distractorReasons.Length > 2) ? round.distractorReasons[2] : "To find a job abroad/in India.";

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
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer",
            "OptionChip_0", "OptionChip_1", "OptionChip_2", "OptionChip_3"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName) ?? transform.Find("OptionsContainer/" + lName);
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
            headerTMP.text = "READING BRANCH (Library Noticeboard)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "R01 Why Do We Learn English? (in-context)";
        }
    }

    public void AutoBindReferences() {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> chipBtns = new List<Button>();

        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("optionbutton") || n.Contains("optionchip") || n.Contains("chip")) {
                chipBtns.Add(btn);
            } else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("repeat") || n.Contains("audio"))) {
                replayAudioBtn = btn;
            } else if (retryBtn == null && n.Contains("retry")) {
                retryBtn = btn;
            } else if (gotItBtn == null && (n.Contains("gotit") || n.Contains("got_it"))) {
                gotItBtn = btn;
            } else if (nextButton == null && n.Contains("next")) {
                nextButton = btn;
            } else if (backButton == null && n.Contains("back")) {
                backButton = btn;
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
            } else if (speechBubbleTMP == null && (n.Contains("situationtext") || n.Contains("situation") || n.Contains("expression") || n.Contains("bubble") || n.Contains("phrase"))) {
                speechBubbleTMP = tmp;
            } else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) {
                titleTMP = tmp;
            } else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) {
                headerTMP = tmp;
            }
        }

        if (hintPanel == null) {
            Transform hTrans = transform.Find("FeedbackPanel") ?? transform.Find("HintPanel");
            if (hTrans != null) hintPanel = hTrans.gameObject;
        }

        if (hintPanel != null) {
            if (hintCorrectAnswerTMP == null) {
                hintCorrectAnswerTMP = hintPanel.transform.Find("FeedbackText")?.GetComponent<TextMeshProUGUI>() ?? hintPanel.transform.Find("CorrectAnswerText")?.GetComponent<TextMeshProUGUI>();
            }
            if (gotItBtn == null) {
                gotItBtn = hintPanel.transform.Find("GotItButton")?.GetComponent<Button>();
            }
        }

        if (resultPanel == null) {
            Transform rTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel");
            if (rTrans != null) resultPanel = rTrans.gameObject;
        }
    }

    public void PopulateFailsafeRounds() {
        string baseAudio = "Assets/Audio/2B/1_EnglishIsImportant/Reading/";

        (string sit, string correct, string[] distractors, string audioFile)[] samples = new (string, string, string[], string)[] {
            ("You want a good job when you grow up.", "English is the most important language for a better career.", new string[] { "It is a part of a curriculum.", "To communicate with friends.", "To find a job abroad/in India." }, "VO_R01_P1.mp3"),
            ("English is one of your school subjects.", "It is a part of a curriculum.", new string[] { "English is the most important language for a better career.", "To read English books.", "To surf the net." }, "VO_R01_P2.mp3"),
            ("You want to chat and play with friends.", "To communicate with friends.", new string[] { "To study in an English-speaking country.", "To travel to other countries.", "English is a global language." }, "VO_R01_P3.mp3"),
            ("You dream of working in another country.", "To find a job abroad/in India.", new string[] { "To understand computer software.", "I want to learn English for a bright future.", "To read English books." }, "VO_R01_P4.mp3"),
            ("You love story books written in English.", "To read English books.", new string[] { "To surf the net.", "To travel to other countries.", "It is a part of a curriculum." }, "VO_R01_P5.mp3"),
            ("You want to join a college overseas.", "To study in an English-speaking country.", new string[] { "To find a job abroad/in India.", "To communicate with friends.", "English is the most important language for a better career." }, "VO_R01_P6.mp3"),
            ("You want to use the internet easily.", "To surf the net.", new string[] { "To understand computer software.", "To read English books.", "English is a global language." }, "VO_R01_P7.mp3"),
            ("You are planning a holiday abroad.", "To travel to other countries.", new string[] { "To study in an English-speaking country.", "To communicate with friends.", "I want to learn English for a bright future." }, "VO_R01_P8.mp3"),
            ("You want to enjoy English films and music.", "To understand my favourite English movies and songs.", new string[] { "To surf the net.", "It is a part of a curriculum.", "English is the most important language for a better career." }, "VO_R01_P9.mp3"),
            ("You want to use apps and computers well.", "To understand computer software.", new string[] { "To surf the net.", "To read English books.", "To find a job abroad/in India." }, "VO_R01_P10.mp3"),
            ("You want one language that works everywhere.", "English is a global language.", new string[] { "To travel to other countries.", "To study in an English-speaking country.", "I want to learn English for a bright future." }, "VO_R01_P11.mp3"),
            ("You are thinking about your future.", "I want to learn English for a bright future.", new string[] { "English is the most important language for a better career.", "English is a global language.", "To understand my favourite English movies and songs." }, "VO_R01_P12.mp3")
        };

        roundDataArray = new ReadingR01RoundData[samples.Length];
        for (int i = 0; i < samples.Length; i++) {
            roundDataArray[i] = new ReadingR01RoundData {
                situationText = samples[i].sit,
                correctReason = samples[i].correct,
                distractorReasons = samples[i].distractors,
                situationAudioClip = Resources.Load<AudioClip>(baseAudio + samples[i].audioFile)
            };
        }
    }

    public void StartLesson() {
        currentRoundIndex = 0;
        score = 0;
        isProcessingInput = false;

        if (hintPanel != null) hintPanel.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        PlayCurrentRound();
    }

    private void PlayCurrentRound() {
        if (roundDataArray == null || roundDataArray.Length == 0) {
            Debug.LogWarning("[R01 Reading] roundDataArray is empty.");
            return;
        }

        if (currentRoundIndex >= roundDataArray.Length) {
            CompleteLesson();
            return;
        }

        isProcessingInput = false;
        currentRoundHasRetried = false;
        EnsureHeaderAndTitle();
        ResetOptionVisuals();

        ReadingR01RoundData round = roundDataArray[currentRoundIndex];

        if (progressTMP != null) {
            progressTMP.text = $"{currentRoundIndex + 1}/{roundDataArray.Length}";
        }

        if (speechBubbleTMP != null) {
            speechBubbleTMP.text = round.situationText;
        }

        // Build 4 choices (1 correct + 3 distractors)
        List<string> choices = new List<string>();
        choices.Add(round.correctReason);
        if (round.distractorReasons != null) {
            foreach (var d in round.distractorReasons) {
                if (!string.IsNullOrEmpty(d)) choices.Add(d);
            }
        }

        while (choices.Count < 4) {
            choices.Add("English is a global language.");
        }

        // Randomize option positions (A, B, C, D) deterministically per round
        int targetCorrectIndex = currentRoundIndex % 4;
        string correctVal = choices[0];

        System.Random rng = new System.Random(currentRoundIndex + 17);
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

        // Play single situation audio for current round
        if (round.situationAudioClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(round.situationAudioClip);
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
            tmp.fontSizeMin = 16f;
            tmp.fontSizeMax = 22f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.enableWordWrapping = true;
            tmp.color = Color.white;
            RectTransform trt = tmp.GetComponent<RectTransform>();
            if (trt != null) {
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(30f, 4f);
                trt.offsetMax = new Vector2(-20f, -4f);
            }
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

                    if (initialButtonStates != null && i < initialButtonStates.Length) {
                        RectTransform rt = optionButtons[i].GetComponent<RectTransform>();
                        if (rt != null) {
                            rt.anchorMin = initialButtonStates[i].anchorMin;
                            rt.anchorMax = initialButtonStates[i].anchorMax;
                            rt.pivot = initialButtonStates[i].pivot;
                            rt.sizeDelta = initialButtonStates[i].sizeDelta;
                            rt.anchoredPosition = initialButtonStates[i].anchoredPosition;
                            rt.localScale = initialButtonStates[i].localScale;
                        }
                    }

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
                ShowHintPanel();
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

    private void ShowHintPanel() {
        if (hintPanel != null) {
            hintPanel.SetActive(true);

            if (hintCorrectAnswerTMP != null && roundDataArray != null && currentRoundIndex < roundDataArray.Length) {
                hintCorrectAnswerTMP.text = roundDataArray[currentRoundIndex].correctReason;
            }
        }
    }

    private void OnGotItButtonClicked() {
        if (hintPanel != null) {
            hintPanel.SetActive(false);
        }

        currentRoundHasRetried = true;

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    if (optionImages[i] != null && optionImages[i].color == wrongColor) {
                        optionImages[i].color = defaultChipColor;
                        optionButtons[i].interactable = false;
                    }
                }
            }
        }
    }

    private IEnumerator NextRoundRoutine() {
        yield return new WaitForSeconds(1.2f);
        currentRoundIndex++;
        PlayCurrentRound();
    }

    private void OnReplayAudioClicked() {
        if (roundDataArray != null && currentRoundIndex < roundDataArray.Length) {
            AudioClip clip = roundDataArray[currentRoundIndex].situationAudioClip;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
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
            resultScoreTMP.text = $"{score}/{roundDataArray.Length}";
        }

        bool passed = score >= 10;

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed ? "GREAT JOB! LESSON COMPLETED!" : "TRY AGAIN TO PASS (10/12 REQUIRED)";
        }

        if (retryBtn != null) {
            retryBtn.gameObject.SetActive(!passed);
        }

        if (passed) {
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            Debug.Log($"[R01 Reading] Passed with score {score}/{roundDataArray.Length}!");
        } else {
            Debug.Log($"[R01 Reading] Failed with score {score}/{roundDataArray.Length}. Retry available.");
        }
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }

    protected virtual void OnBackButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnBackButtonClicked();
        }
    }
}
