using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class Masters_EnglishIsImportant_Reading_LessonThree : Masters_Lesson {

[System.Serializable]
public class ReadingR03ItemData {
    public string fullForm;            // e.g. "Will not"
    public string correctContraction; // e.g. "won't"
    public string[] distractorContractions; // 3 verbatim contraction distractors from p.7
    public string note;                // e.g. "irregular — highlight it"
    public AudioClip fullFormAudioClip;
    public AudioClip sentenceUsageClip;
}

    [Header("R03 14 Verbatim Items from Book p.7")]
    [SerializeField]
    private ReadingR03ItemData[] itemDataArray;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI fullFormTextTMP; // Card text in machine
    [SerializeField]
    private Transform fullFormCardTransform;  // Card transform for squeeze animation
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI headerTMP;

    [Header("4 Contraction Option Buttons")]
    [SerializeField]
    private Button[] optionButtons; // OptionButton_01 .. OptionButton_04

    [Header("Navigation Buttons")]
    [SerializeField]
    private Button backButton;

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
    [Range(0, 13)]
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
        topic = Masters_Topic.Reading;
        narratorSpeech = null;

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

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
    }

    protected override void Start() {
        EnsureNextAndBackButtonWired();
        EnsureAspectRatiosAndAnchorsPreserved();
        EnsureHeaderAndTitle();

        PurgeLegacyChildren();

        if (itemDataArray == null || itemDataArray.Length == 0) {
            PopulateFailsafeItems();
        }

        StartLesson();
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

        if (itemDataArray == null || itemDataArray.Length == 0) {
            PopulateFailsafeItems();
        }

        if (itemDataArray == null || itemDataArray.Length == 0) return;

        int idx = Mathf.Clamp(editorPreviewRound, 0, itemDataArray.Length - 1);
        ReadingR03ItemData item = itemDataArray[idx];

        if (progressTMP != null) progressTMP.text = $"{idx + 1}/{itemDataArray.Length}";
        if (fullFormTextTMP != null) fullFormTextTMP.text = item.fullForm;
        if (titleTMP != null) titleTMP.text = "R03 Shrink It — Full Form to Contraction";
        if (headerTMP != null) headerTMP.text = "READING BRANCH (Library)";

        if (optionButtons != null && optionButtons.Length >= 4) {
            string[] previewChoices = new string[4];
            previewChoices[0] = item.correctContraction;
            previewChoices[1] = (item.distractorContractions != null && item.distractorContractions.Length > 0) ? item.distractorContractions[0] : "don't";
            previewChoices[2] = (item.distractorContractions != null && item.distractorContractions.Length > 1) ? item.distractorContractions[1] : "won't";
            previewChoices[3] = (item.distractorContractions != null && item.distractorContractions.Length > 2) ? item.distractorContractions[2] : "doesn't";

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
            headerTMP.text = "READING BRANCH (Library Squeezing Machine)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "R03 Shrink It — Full Form to Contraction";
        }
    }

    public void AutoBindReferences() {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> chipBtns = new List<Button>();

        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("optionbutton") || n.Contains("optionchip") || n.Contains("chip") || n.Contains("contraction")) {
                chipBtns.Add(btn);
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
            } else if (fullFormTextTMP == null && (n.Contains("fullformtext") || n.Contains("fullform") || n.Contains("cardtext") || n.Contains("word") || n.Contains("speech"))) {
                fullFormTextTMP = tmp;
            } else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) {
                titleTMP = tmp;
            } else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) {
                headerTMP = tmp;
            }
        }

        if (fullFormCardTransform == null) {
            Transform cardTr = transform.Find("FullFormCard") ?? transform.Find("SqueezingMachine/FullFormCard") ?? transform.Find("SituationPanel") ?? transform.Find("PhraseCard");
            if (cardTr != null) fullFormCardTransform = cardTr;
        }

        if (resultPanel == null) {
            Transform rTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel");
            if (rTrans != null) resultPanel = rTrans.gameObject;
        }
    }

    public void PopulateFailsafeItems() {
        string baseAudio = "Assets/Audio/2B/1_EnglishIsImportant/Reading/";

        (string full, string correct, string[] distractors, string noteStr, string ffAudio, string sentAudio)[] samples = new (string, string, string[], string, string, string)[] {
            ("Cannot", "can't", new string[] { "don't", "won't", "doesn't" }, "", "eii_r03_ff_01.mp3", "eii_r03_sent_01.mp3"),
            ("Do not", "don't", new string[] { "doesn't", "can't", "won't" }, "", "eii_r03_ff_02.mp3", "eii_r03_sent_02.mp3"),
            ("Does not", "doesn't", new string[] { "don't", "wouldn't", "shouldn't" }, "", "eii_r03_ff_03.mp3", "eii_r03_sent_03.mp3"),
            ("Will not", "won't", new string[] { "wouldn't", "can't", "we'll" }, "irregular — highlight it", "eii_r03_ff_04.mp3", "eii_r03_sent_04.mp3"),
            ("Would not", "wouldn't", new string[] { "shouldn't", "won't", "doesn't" }, "", "eii_r03_ff_05.mp3", "eii_r03_sent_05.mp3"),
            ("Should not", "shouldn't", new string[] { "wouldn't", "don't", "can't" }, "", "eii_r03_ff_06.mp3", "eii_r03_sent_06.mp3"),
            ("I am", "I'm", new string[] { "I've", "I'd", "they're" }, "", "eii_r03_ff_07.mp3", "eii_r03_sent_07.mp3"),
            ("I have", "I've", new string[] { "I'm", "they've", "we'll" }, "", "eii_r03_ff_08.mp3", "eii_r03_sent_08.mp3"),
            ("They are", "they're", new string[] { "they've", "we'll", "he's" }, "", "eii_r03_ff_09.mp3", "eii_r03_sent_09.mp3"),
            ("They have", "they've", new string[] { "they're", "I've", "we'll" }, "", "eii_r03_ff_10.mp3", "eii_r03_sent_10.mp3"),
            ("He is", "he's", new string[] { "it's", "I'm", "they're" }, "He has also gives he's", "eii_r03_ff_11.mp3", "eii_r03_sent_11.mp3"),
            ("It is", "it's", new string[] { "he's", "I'd", "we'll" }, "It has also gives it's", "eii_r03_ff_12.mp3", "eii_r03_sent_12.mp3"),
            ("I would", "I'd", new string[] { "I've", "I'm", "he's" }, "I had also gives I'd", "eii_r03_ff_13.mp3", "eii_r03_sent_13.mp3"),
            ("We will", "we'll", new string[] { "they're", "I've", "won't" }, "", "eii_r03_ff_14.mp3", "eii_r03_sent_14.mp3")
        };

        itemDataArray = new ReadingR03ItemData[samples.Length];
        for (int i = 0; i < samples.Length; i++) {
            itemDataArray[i] = new ReadingR03ItemData {
                fullForm = samples[i].full,
                correctContraction = samples[i].correct,
                distractorContractions = samples[i].distractors,
                note = samples[i].noteStr,
                fullFormAudioClip = Resources.Load<AudioClip>(baseAudio + samples[i].ffAudio),
                sentenceUsageClip = Resources.Load<AudioClip>(baseAudio + samples[i].sentAudio)
            };
        }
    }

    public void StartLesson() {
        currentRoundIndex = 0;
        score = 0;
        isProcessingInput = false;

        if (resultPanel != null) resultPanel.SetActive(false);

        PlayCurrentRound();
    }

    private void PlayCurrentRound() {
        if (itemDataArray == null || itemDataArray.Length == 0) {
            Debug.LogWarning("[R03 Reading] itemDataArray is empty.");
            return;
        }

        if (currentRoundIndex >= itemDataArray.Length) {
            CompleteLesson();
            return;
        }

        isProcessingInput = false;
        currentRoundHasRetried = false;
        EnsureHeaderAndTitle();
        ResetOptionVisuals();

        ReadingR03ItemData item = itemDataArray[currentRoundIndex];

        if (progressTMP != null) {
            progressTMP.text = $"{currentRoundIndex + 1}/{itemDataArray.Length}";
        }

        if (fullFormTextTMP != null) {
            fullFormTextTMP.text = item.fullForm;
            fullFormTextTMP.color = Color.white;
            fullFormTextTMP.fontSize = 32f;
            fullFormTextTMP.fontStyle = FontStyles.Bold;
        }

        if (fullFormCardTransform != null) {
            fullFormCardTransform.localScale = Vector3.one;
        }

        // Build 4 distinct choices from book p.7 verbatim contractions
        List<string> choices = new List<string>();
        choices.Add(item.correctContraction);
        if (item.distractorContractions != null) {
            foreach (var d in item.distractorContractions) {
                if (!string.IsNullOrEmpty(d) && !choices.Contains(d)) choices.Add(d);
            }
        }

        string[] p7FallbackContractions = new string[] { "don't", "can't", "won't", "doesn't", "wouldn't", "shouldn't", "I'm", "I've", "they're", "they've", "he's", "it's", "I'd", "we'll" };
        int fallbackIdx = 0;
        while (choices.Count < 4 && fallbackIdx < p7FallbackContractions.Length) {
            string candidate = p7FallbackContractions[fallbackIdx++];
            if (!choices.Contains(candidate)) {
                choices.Add(candidate);
            }
        }

        // Randomize option positions (A, B, C, D) deterministically per round
        int targetCorrectIndex = currentRoundIndex % 4;
        string correctVal = choices[0];

        System.Random rng = new System.Random(currentRoundIndex + 19);
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

        if (item.fullFormAudioClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(item.fullFormAudioClip);
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
            tmp.fontSizeMin = 20f;
            tmp.fontSizeMax = 30f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            RectTransform trt = tmp.GetComponent<RectTransform>();
            if (trt != null) {
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = new Vector2(10f, 4f);
                trt.offsetMax = new Vector2(-10f, -4f);
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
        ReadingR03ItemData item = (itemDataArray != null && currentRoundIndex < itemDataArray.Length) ? itemDataArray[currentRoundIndex] : null;

        if (isCorrect) {
            isProcessingInput = true;
            score++;

            if (img != null) img.color = correctColor;
            if (btn != null) btn.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);

            // Squeezing machine effect: squeeze animation + update card to contraction
            if (fullFormCardTransform != null) {
                fullFormCardTransform.DOPunchScale(new Vector3(-0.25f, 0.25f, 0f), 0.5f).OnComplete(() => {
                    if (fullFormTextTMP != null && item != null) {
                        fullFormTextTMP.text = item.correctContraction;
                    }
                });
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                if (item != null && item.sentenceUsageClip != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(item.sentenceUsageClip);
                }
            }

            float waitTime = (item != null && item.sentenceUsageClip != null && item.sentenceUsageClip.length > 0) ? item.sentenceUsageClip.length + 0.3f : 1.5f;
            StartCoroutine(NextRoundRoutine(waitTime));
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
                StartCoroutine(NextRoundRoutine(1.8f));
            }
        }
    }

    private IEnumerator NextRoundRoutine(float delay) {
        yield return new WaitForSeconds(delay);
        currentRoundIndex++;
        PlayCurrentRound();
    }

    private void OnReplayAudioClicked() {
        if (itemDataArray != null && currentRoundIndex < itemDataArray.Length) {
            AudioClip clip = itemDataArray[currentRoundIndex].fullFormAudioClip;
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
            resultScoreTMP.text = $"{score}/{itemDataArray.Length}";
        }

        bool passed = score >= Mathf.CeilToInt(itemDataArray.Length * 0.75f); // 8/10 or 11/14 pass threshold

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed ? "GREAT JOB! LESSON COMPLETED!" : "TRY AGAIN TO PASS";
        }

        if (retryBtn != null) {
            retryBtn.gameObject.SetActive(!passed);
        }

        if (passed) {
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            Debug.Log($"[R03 Reading] Passed with score {score}/{itemDataArray.Length}!");
        } else {
            Debug.Log($"[R03 Reading] Failed with score {score}/{itemDataArray.Length}. Retry available.");
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
