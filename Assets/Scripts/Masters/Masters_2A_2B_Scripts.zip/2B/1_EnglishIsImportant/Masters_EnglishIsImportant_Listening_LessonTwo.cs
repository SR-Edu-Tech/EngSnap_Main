using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class Masters_EnglishIsImportant_Listening_LessonTwo : Masters_Lesson {

[System.Serializable]
public class ListeningL02RoundData {
    public AudioClip longPhraseClip;
    public string longPhraseText;
    public string correctShortPhrase;
    public string[] optionChoices; // 5 phrasal-verb options
    public AudioClip ariaPairClip;
    public string feedbackText;
}

    [Header("L02 10 Verbatim Rounds from Book p.6")]
    [SerializeField]
    private ListeningL02RoundData[] roundDataArray;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI speechBubbleTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI headerTMP;

    [Header("5 Answer Option Chips")]
    [SerializeField]
    private Button[] optionButtons; // 5 Option Chips

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

    [Header("Colors & Animation")]
    [SerializeField]
    private Color defaultChipColor = new Color(0.14f, 0.38f, 0.58f, 1f);
    [SerializeField]
    private Color correctColor = new Color(0.14f, 0.53f, 0.22f, 1f);
    [SerializeField]
    private Color wrongColor = new Color(0.71f, 0.15f, 0.15f, 1f);

    [Header("Editor Preview")]
    [Range(0, 9)]
    public int editorPreviewRound = 0;

    private int currentRoundIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;
    private bool currentRoundHasRetried = false;

    private Image[] optionImages;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Listening;

        PurgeLegacyChildren();
        EnsureUIElementsExist();
        AutoBindReferences();

        if (optionButtons != null && optionButtons.Length > 0) {
            optionImages = new Image[optionButtons.Length];
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    int index = i;
                    optionImages[i] = optionButtons[i].GetComponent<Image>();
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
        EnsureUIElementsExist();

        if (roundDataArray == null || roundDataArray.Length == 0) {
            PopulateFailsafeRounds();
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

        if (roundDataArray == null || roundDataArray.Length == 0) {
            PopulateFailsafeRounds();
        }

        if (roundDataArray == null || roundDataArray.Length == 0) return;

        int idx = Mathf.Clamp(editorPreviewRound, 0, roundDataArray.Length - 1);
        ListeningL02RoundData r = roundDataArray[idx];

        if (headerTMP != null) headerTMP.text = "LISTENING BRANCH (Radio Tower)";
        if (titleTMP != null) titleTMP.text = "L02 Say It Shorter — The Long Way, The Short Way";
        if (progressTMP != null) progressTMP.text = $"{idx + 1}/{roundDataArray.Length}";
        if (speechBubbleTMP != null) speechBubbleTMP.text = r.longPhraseText;

        if (optionButtons != null && r.optionChoices != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    bool hasOpt = (i < r.optionChoices.Length);
                    optionButtons[i].gameObject.SetActive(hasOpt);
                    if (hasOpt) {
                        TextMeshProUGUI tmp = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (tmp != null) tmp.text = r.optionChoices[i];
                    }
                }
            }
        }
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "Bin 1", "Bin 2", "Bin 3", "Bin 4", "OfferingBin", "RespondingBin",
            "SortBin_0", "SortBin_1", "SortBin_2", "SortBin_3", "StreetBins", "TrashBins",
            "SortBins", "DropTargets", "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                Destroy(lTrans.gameObject);
            }
        }
    }

    private void EnsureUIElementsExist() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "LISTENING BRANCH (Radio Tower)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "L02 Say It Shorter — The Long Way, The Short Way";
        }
    }

    public void AutoBindReferences() {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> opts = new List<Button>();
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("option") || n.Contains("chip")) opts.Add(btn);
            else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("repeat") || n.Contains("audio"))) replayAudioBtn = btn;
            else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
            else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) nextButton = btn;
        }

        if (opts.Count >= 5 && (optionButtons == null || optionButtons.Length == 0)) {
            optionButtons = opts.ToArray();
        }

        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (progressTMP == null && (n.Contains("progresstext") || n.Contains("progress") || n.Contains("counter"))) progressTMP = tmp;
            else if (speechBubbleTMP == null && (n.Contains("speech") || n.Contains("bubble") || n.Contains("phrase"))) speechBubbleTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    public void PopulateFailsafeRounds() {
        string baseAudio = "Assets/Audio/2B/1_EnglishIsImportant/Listening/";

        (string longP, string corrP, string[] choices, string audioFile, string pairFile)[] samples = new (string, string, string[], string, string)[] {
            ("Stop sleeping", "WAKE UP", new string[] { "WAKE UP", "HANG UP", "CHECK OUT", "POINT OUT", "CALL BACK" }, "VO_L02_P1.mp3", "eii_l02_p01_pair.mp3"),
            ("Solve something", "FIGURE OUT", new string[] { "LOOK OUT", "FIGURE OUT", "BRING UP", "GO THROUGH", "CALL BACK" }, "VO_L02_P2.mp3", "eii_l02_p02_pair.mp3"),
            ("Look at it carefully", "CHECK OUT", new string[] { "WAKE UP", "LOOK OUT", "CHECK OUT", "POINT OUT", "GO THROUGH" }, "VO_L02_P3.mp3", "eii_l02_p03_pair.mp3"),
            ("Be careful", "LOOK OUT", new string[] { "HANG UP", "LOOK FORWARD TO", "BRING UP", "LOOK OUT", "CHECK OUT" }, "VO_L02_P4.mp3", "eii_l02_p04_pair.mp3"),
            ("End a phone call", "HANG UP", new string[] { "CALL BACK", "POINT OUT", "WAKE UP", "FIGURE OUT", "HANG UP" }, "VO_L02_P5.mp3", "eii_l02_p05_pair.mp3"),
            ("Be excited about the future", "LOOK FORWARD TO", new string[] { "LOOK FORWARD TO", "LOOK OUT", "BRING UP", "CHECK OUT", "WAKE UP" }, "VO_L02_P6.mp3", "eii_l02_p06_pair.mp3"),
            ("Draw attention to", "POINT OUT", new string[] { "BRING UP", "POINT OUT", "FIGURE OUT", "HANG UP", "CALL BACK" }, "VO_L02_P7.mp3", "eii_l02_p07_pair.mp3"),
            ("Return a phone call", "CALL BACK", new string[] { "HANG UP", "POINT OUT", "CALL BACK", "GO THROUGH", "LOOK OUT" }, "VO_L02_P8.mp3", "eii_l02_p08_pair.mp3"),
            ("Start talking about a subject", "BRING UP", new string[] { "FIGURE OUT", "WAKE UP", "LOOK FORWARD TO", "BRING UP", "POINT OUT" }, "VO_L02_P9.mp3", "eii_l02_p09_pair.mp3"),
            ("Examine in detail", "GO THROUGH", new string[] { "LOOK OUT", "CHECK OUT", "CALL BACK", "HANG UP", "GO THROUGH" }, "VO_L02_P10.mp3", "eii_l02_p10_pair.mp3")
        };

        roundDataArray = new ListeningL02RoundData[samples.Length];
        for (int i = 0; i < samples.Length; i++) {
            roundDataArray[i] = new ListeningL02RoundData {
                longPhraseText = samples[i].longP,
                correctShortPhrase = samples[i].corrP,
                optionChoices = samples[i].choices,
                longPhraseClip = Resources.Load<AudioClip>(baseAudio + samples[i].audioFile),
                ariaPairClip = Resources.Load<AudioClip>(baseAudio + samples[i].pairFile)
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

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }

        PlayCurrentRound();
    }

    private void PlayCurrentRound() {
        if (roundDataArray == null || roundDataArray.Length == 0) return;

        if (currentRoundIndex >= roundDataArray.Length) {
            CompleteLesson();
            return;
        }

        isProcessingInput = false;
        currentRoundHasRetried = false;
        ListeningL02RoundData r = roundDataArray[currentRoundIndex];

        if (progressTMP != null) progressTMP.text = $"{currentRoundIndex + 1}/{roundDataArray.Length}";
        if (speechBubbleTMP != null) speechBubbleTMP.text = r.longPhraseText;

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    bool hasOpt = (r.optionChoices != null && i < r.optionChoices.Length);
                    optionButtons[i].gameObject.SetActive(hasOpt);
                    if (hasOpt) {
                        optionButtons[i].interactable = true;
                        if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                            optionImages[i].color = defaultChipColor;
                        }
                        TextMeshProUGUI tmp = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (tmp != null) tmp.text = r.optionChoices[i];
                    }
                }
            }
        }

        if (r.longPhraseClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(r.longPhraseClip);
        }
    }

    private void OnOptionSelected(int index) {
        if (isProcessingInput || roundDataArray == null || currentRoundIndex >= roundDataArray.Length) return;

        ListeningL02RoundData r = roundDataArray[currentRoundIndex];
        string selectedText = (r.optionChoices != null && index < r.optionChoices.Length) ? r.optionChoices[index] : "";
        bool isCorrect = (selectedText.Trim().ToUpper() == r.correctShortPhrase.Trim().ToUpper());

        if (isCorrect) {
            isProcessingInput = true;
            score++;

            if (optionImages != null && index < optionImages.Length && optionImages[index] != null) {
                optionImages[index].color = correctColor;
            }
            if (optionButtons != null && index < optionButtons.Length && optionButtons[index] != null) {
                optionButtons[index].transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                if (r.ariaPairClip != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(r.ariaPairClip);
                }
            }

            float waitTime = (r.ariaPairClip != null && r.ariaPairClip.length > 0) ? r.ariaPairClip.length + 0.3f : 1.5f;
            StartCoroutine(AdvanceRoundRoutine(waitTime));
        } else {
            if (optionImages != null && index < optionImages.Length && optionImages[index] != null) {
                optionImages[index].color = wrongColor;
            }
            if (optionButtons != null && index < optionButtons.Length && optionButtons[index] != null) {
                optionButtons[index].transform.DOShakePosition(0.4f, new Vector3(15f, 0f, 0f), 10, 90, false, true);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (!currentRoundHasRetried) {
                // Allow 1 retry per round
                currentRoundHasRetried = true;
            } else {
                // Already retried once, reveal correct chip and move on
                isProcessingInput = true;
                HighlightCorrectChip(r.correctShortPhrase);
                StartCoroutine(AdvanceRoundRoutine(1.8f));
            }
        }
    }

    private void HighlightCorrectChip(string correctPhrase) {
        if (optionButtons == null || optionImages == null) return;
        for (int i = 0; i < optionButtons.Length; i++) {
            if (optionButtons[i] != null && optionImages[i] != null) {
                TextMeshProUGUI tmp = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null && tmp.text.Trim().ToUpper() == correctPhrase.Trim().ToUpper()) {
                    optionImages[i].color = correctColor;
                }
            }
        }
    }

    private IEnumerator AdvanceRoundRoutine(float delay) {
        yield return new WaitForSeconds(delay);

        currentRoundIndex++;
        if (currentRoundIndex < roundDataArray.Length) {
            PlayCurrentRound();
        } else {
            CompleteLesson();
        }
    }

    private void CompleteLesson() {
        bool passed = (score >= 8 || (roundDataArray.Length <= 8 && score >= 6)); // Pass condition: at least 8 of 10 or 6 of 8

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            if (resultScoreTMP != null) resultScoreTMP.text = $"Final Score: {score}/{roundDataArray.Length}";
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "VICTORY! RADIO TOWER MATCH COMPLETE!" : "TRY AGAIN TO PASS (AT LEAST 80% REQUIRED)";
                resultStatusTMP.color = passed ? Color.green : Color.red;
            }
        }

        if (retryBtn != null) {
            retryBtn.gameObject.SetActive(!passed);
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
        if (roundDataArray != null && currentRoundIndex < roundDataArray.Length && roundDataArray[currentRoundIndex].longPhraseClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(roundDataArray[currentRoundIndex].longPhraseClip);
        }
    }

    private void OnRetryButtonClicked() {
        if (resultPanel != null) resultPanel.SetActive(false);
        StartLesson();
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}
