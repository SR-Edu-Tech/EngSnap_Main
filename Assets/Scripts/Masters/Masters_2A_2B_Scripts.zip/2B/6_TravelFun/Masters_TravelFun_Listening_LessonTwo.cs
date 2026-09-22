using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



public class Masters_TravelFun_Listening_LessonTwo : Masters_Lesson {

[System.Serializable]
public class TravelFunL02RoundData {
    public string clozePromptText;
    public string revealedSentenceText;
    public string correctPhrase;
    public string[] optionChoices; // 5 phrase options
    public AudioClip clozePromptClip;
    public AudioClip revealedSentenceClip;
    public AudioClip slowClozePromptClip;
}

    [Header("L02 8 Announcement Cloze Rounds")]
    [SerializeField]
    private TravelFunL02RoundData[] roundDataArray;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI announcementBoardTMP;
    [SerializeField]
    private Transform announcementBoardCard;

    [Header("5 Answer Option Chips")]
    [SerializeField]
    private Button[] optionButtons; // 5 Option Chips

    [Header("Audio Controls")]
    [SerializeField]
    private Button replayAudioBtn;
    [SerializeField]
    private Toggle slowToggle;

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
    private Color correctColor = new Color(0.14f, 0.65f, 0.28f, 1f);
    [SerializeField]
    private Color wrongColor = new Color(0.80f, 0.20f, 0.20f, 1f);

    [Header("Editor Preview")]
    [Range(0, 7)]
    public int editorPreviewRound = 0;

    private int currentRoundIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;
    private bool currentRoundHasRetried = false;

    private Image[] optionImages;
    private TextMeshProUGUI[] optionTexts;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Listening;

        PurgeLegacyChildren();
        EnsureUIElementsExist();
        AutoBindReferences();

        if (optionButtons != null && optionButtons.Length > 0) {
            optionImages = new Image[optionButtons.Length];
            optionTexts = new TextMeshProUGUI[optionButtons.Length];
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    int index = i;
                    optionImages[i] = optionButtons[i].GetComponent<Image>();
                    optionTexts[i] = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
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

        if (headerTMP != null) headerTMP.text = "LISTENING BRANCH (Travel Fun)";
        if (titleTMP != null) titleTMP.text = "L02 Finish the Announcement";

        if (roundDataArray == null || roundDataArray.Length == 0) return;

        int idx = Mathf.Clamp(editorPreviewRound, 0, roundDataArray.Length - 1);
        TravelFunL02RoundData r = roundDataArray[idx];

        if (progressTMP != null) progressTMP.text = $"Round {idx + 1}/{roundDataArray.Length}";
        if (announcementBoardTMP != null) announcementBoardTMP.text = r.clozePromptText;

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
            headerTMP.text = "LISTENING BRANCH (Travel Fun)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "L02 Finish the Announcement";
        }
    }

    private void AutoBindReferences() {
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
            if (progressTMP == null && (n.Contains("progresstext") || n.Contains("progress") || n.Contains("counter") || n.Contains("expressioncount"))) progressTMP = tmp;
            else if (announcementBoardTMP == null && (n.Contains("speech") || n.Contains("bubble") || n.Contains("prompt") || n.Contains("statement") || n.Contains("board"))) announcementBoardTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
        }

        if (announcementBoardCard == null) {
            Transform boardTr = transform.Find("AnnouncementBoard") ?? transform.Find("SpeechBubble") ?? transform.Find("PromptCard") ?? transform.Find("QuestionPanel");
            if (boardTr != null) announcementBoardCard = boardTr;
            else if (announcementBoardTMP != null) announcementBoardCard = announcementBoardTMP.transform.parent;
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    private IEnumerator StartWithIntroAudioRoutine() {
        isProcessingInput = true;

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            yield return new WaitForSeconds(narratorSpeech.length + 0.3f);
        } else {
            yield return new WaitForSeconds(0.5f);
        }

        currentRoundIndex = 0;
        score = 0;
        LoadRound(currentRoundIndex);
    }

    private void LoadRound(int roundIndex) {
        if (roundDataArray == null || roundIndex < 0 || roundIndex >= roundDataArray.Length) {
            ShowResults();
            return;
        }

        currentRoundIndex = roundIndex;
        currentRoundHasRetried = false;
        isProcessingInput = false;

        TravelFunL02RoundData data = roundDataArray[currentRoundIndex];

        if (progressTMP != null) {
            progressTMP.text = $"Round {currentRoundIndex + 1}/{roundDataArray.Length}";
        }

        if (announcementBoardTMP != null) {
            announcementBoardTMP.text = data.clozePromptText;
            announcementBoardTMP.color = Color.white;
        }

        if (announcementBoardCard != null) {
            announcementBoardCard.DOKill();
            announcementBoardCard.localScale = Vector3.one;
            announcementBoardCard.DOPunchScale(new Vector3(0.05f, 0.05f, 0f), 0.3f, 5, 0.5f);
        }

        // Setup Option Chips
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    bool hasOption = (data.optionChoices != null && i < data.optionChoices.Length);
                    optionButtons[i].gameObject.SetActive(hasOption);
                    optionButtons[i].interactable = true;

                    if (hasOption) {
                        if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) {
                            optionTexts[i].text = data.optionChoices[i];
                        }
                        if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                            optionImages[i].color = defaultChipColor;
                        }
                        optionButtons[i].transform.DOKill();
                        optionButtons[i].transform.localScale = Vector3.one;
                    }
                }
            }
        }

        PlayPromptAudio();
    }

    private void PlayPromptAudio() {
        if (currentRoundIndex >= 0 && currentRoundIndex < roundDataArray.Length) {
            var data = roundDataArray[currentRoundIndex];
            bool isSlow = (slowToggle != null && slowToggle.isOn);
            AudioClip clipToPlay = (isSlow && data.slowClozePromptClip != null) ? data.slowClozePromptClip : data.clozePromptClip;

            if (clipToPlay != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            }
        }
    }

    private void OnOptionSelected(int optionIndex) {
        if (isProcessingInput || currentRoundIndex >= roundDataArray.Length) return;

        var data = roundDataArray[currentRoundIndex];
        if (data.optionChoices == null || optionIndex < 0 || optionIndex >= data.optionChoices.Length) return;

        string selectedText = data.optionChoices[optionIndex].Trim();
        bool isCorrect = string.Equals(selectedText, data.correctPhrase.Trim(), System.StringComparison.OrdinalIgnoreCase);

        StartCoroutine(ProcessAnswerRoutine(optionIndex, isCorrect));
    }

    private IEnumerator ProcessAnswerRoutine(int optionIndex, bool isCorrect) {
        isProcessingInput = true;
        var data = roundDataArray[currentRoundIndex];

        if (isCorrect) {
            // Correct Answer!
            if (!currentRoundHasRetried) {
                score++;
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // Green highlight on clicked option
            if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = correctColor;
            }
            if (optionButtons != null && optionIndex < optionButtons.Length && optionButtons[optionIndex] != null) {
                optionButtons[optionIndex].transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.35f, 6, 0.5f);
            }

            // Flip Announcement Board to show revealed sentence
            yield return FlipAnnouncementBoardRoutine(data.revealedSentenceText);

            // Play Revealed Full Audio
            if (data.revealedSentenceClip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(data.revealedSentenceClip);
                yield return new WaitForSeconds(data.revealedSentenceClip.length + 0.4f);
            } else {
                yield return new WaitForSeconds(1.2f);
            }

            // Advance to next round
            LoadRound(currentRoundIndex + 1);
        } else {
            // Wrong Answer!
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            // Red highlight & shake
            if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = wrongColor;
            }
            if (optionButtons != null && optionIndex < optionButtons.Length && optionButtons[optionIndex] != null) {
                optionButtons[optionIndex].transform.DOShakePosition(0.4f, new Vector3(15f, 0f, 0f), 10, 90f);
            }

            yield return new WaitForSeconds(0.6f);

            if (!currentRoundHasRetried) {
                // One Retry Allowed!
                currentRoundHasRetried = true;
                if (optionButtons != null && optionIndex < optionButtons.Length && optionButtons[optionIndex] != null) {
                    optionButtons[optionIndex].interactable = false;
                }
                isProcessingInput = false;
            } else {
                // Second Wrong - Reveal Correct Answer & Proceed
                HighlightCorrectOption(data.correctPhrase);

                yield return FlipAnnouncementBoardRoutine(data.revealedSentenceText);

                if (data.revealedSentenceClip != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(data.revealedSentenceClip);
                    yield return new WaitForSeconds(data.revealedSentenceClip.length + 0.4f);
                } else {
                    yield return new WaitForSeconds(1.2f);
                }

                LoadRound(currentRoundIndex + 1);
            }
        }
    }

    private IEnumerator FlipAnnouncementBoardRoutine(string targetText) {
        if (announcementBoardCard != null) {
            // Flip board animation (Scale X to 0, swap text, scale X to 1)
            announcementBoardCard.DOScaleX(0f, 0.2f).SetEase(Ease.InQuad);
            yield return new WaitForSeconds(0.2f);

            if (announcementBoardTMP != null) {
                announcementBoardTMP.text = targetText;
                announcementBoardTMP.color = new Color(1f, 0.95f, 0.4f, 1f); // Golden Highlight
            }

            announcementBoardCard.DOScaleX(1f, 0.25f).SetEase(Ease.OutBack);
            yield return new WaitForSeconds(0.25f);
        } else if (announcementBoardTMP != null) {
            announcementBoardTMP.text = targetText;
            announcementBoardTMP.color = new Color(1f, 0.95f, 0.4f, 1f);
            yield return new WaitForSeconds(0.3f);
        }
    }

    private void HighlightCorrectOption(string correctPhrase) {
        var data = roundDataArray[currentRoundIndex];
        if (data.optionChoices == null || optionButtons == null) return;

        for (int i = 0; i < data.optionChoices.Length && i < optionButtons.Length; i++) {
            if (string.Equals(data.optionChoices[i].Trim(), correctPhrase.Trim(), System.StringComparison.OrdinalIgnoreCase)) {
                if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                    optionImages[i].color = correctColor;
                }
                if (optionButtons[i] != null) {
                    optionButtons[i].transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.3f, 5, 0.5f);
                }
            }
        }
    }

    private void OnReplayAudioClicked() {
        if (isProcessingInput) return;
        PlayPromptAudio();
    }

    private void ShowResults() {
        isProcessingInput = true;

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        int total = (roundDataArray != null) ? roundDataArray.Length : 8;
        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Score: {score} / {total}";
        }

        bool passed = score >= 6; // Success condition: at least 6 of 8

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed ?
                "“Outstanding! You finished the announcements like a pro!”" :
                "“Nice effort! Give it another try to finish all announcements!”";
            resultStatusTMP.color = passed ? new Color(0.2f, 0.9f, 0.4f) : new Color(1f, 0.6f, 0.2f);
        }

        if (passed) {
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }
            if (retryBtn != null) {
                retryBtn.gameObject.SetActive(false);
            }
        } else {
            if (retryBtn != null) {
                retryBtn.gameObject.SetActive(true);
                retryBtn.interactable = true;
            }
            if (nextButton != null) {
                nextButton.gameObject.SetActive(false);
            }
        }
    }

    private void OnRetryButtonClicked() {
        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
        currentRoundIndex = 0;
        score = 0;
        LoadRound(0);
    }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}

