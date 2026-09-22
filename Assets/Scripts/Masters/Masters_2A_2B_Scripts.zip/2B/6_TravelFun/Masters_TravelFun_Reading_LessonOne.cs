using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class Masters_TravelFun_Reading_LessonOne : Masters_Lesson {

[System.Serializable]
public class TravelFunR01RoundData {
    public string situationText;
    public string correctPhrase;
    public string[] distractorPhrases; // 3 distractors
    public string feedbackDefinition;
    public AudioClip situationAudio;
    public AudioClip feedbackAudio;
    public AudioClip slowSituationAudio;
}

    [Header("R01 12 Noticeboard Situation Rounds")]
    [SerializeField]
    private TravelFunR01RoundData[] roundDataArray;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI situationCardTMP;
    [SerializeField]
    private Transform noticeboardCard;

    [Header("4 Travel Phrase Option Chips")]
    [SerializeField]
    private Button[] optionButtons; // 4 Option Chips

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
    [Range(0, 11)]
    public int editorPreviewRound = 0;

    private int currentRoundIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;
    private bool currentRoundHasRetried = false;

    private Image[] optionImages;
    private TextMeshProUGUI[] optionTexts;
    private string[] currentRoundChoices;
    private int currentRoundCorrectIndex;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;

        // Ensure non-zero alpha colors
        if (defaultChipColor.a < 0.1f) defaultChipColor = new Color(0.14f, 0.38f, 0.58f, 1f);
        if (correctColor.a < 0.1f) correctColor = new Color(0.14f, 0.65f, 0.28f, 1f);
        if (wrongColor.a < 0.1f) wrongColor = new Color(0.80f, 0.20f, 0.20f, 1f);

        PurgeLegacyChildren();
        EnsureUIElementsExist();
        AutoBindReferences();
        EnsureOptionArrays();

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

    private void EnsureOptionArrays() {
        if (optionButtons != null && optionButtons.Length > 0) {
            if (optionImages == null || optionImages.Length != optionButtons.Length) {
                optionImages = new Image[optionButtons.Length];
            }
            if (optionTexts == null || optionTexts.Length != optionButtons.Length) {
                optionTexts = new TextMeshProUGUI[optionButtons.Length];
            }
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    int index = i;
                    if (optionImages[i] == null) optionImages[i] = optionButtons[i].GetComponent<Image>();
                    if (optionTexts[i] == null) optionTexts[i] = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    if (optionTexts[i] != null) optionTexts[i].raycastTarget = false;
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
                }
            }
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

        if (headerTMP != null) headerTMP.text = "READING BRANCH (Travel Fun)";
        if (titleTMP != null) titleTMP.text = "R01 Which Travel Phrase? (in-context)";

        if (roundDataArray == null || roundDataArray.Length == 0) return;

        int idx = Mathf.Clamp(editorPreviewRound, 0, roundDataArray.Length - 1);
        TravelFunR01RoundData r = roundDataArray[idx];

        if (progressTMP != null) progressTMP.text = $"Round {idx + 1}/{roundDataArray.Length}";
        if (situationCardTMP != null) situationCardTMP.text = r.situationText;

        if (optionButtons != null) {
            List<string> sampleOpts = new List<string>();
            if (!string.IsNullOrEmpty(r.correctPhrase)) sampleOpts.Add(r.correctPhrase);
            if (r.distractorPhrases != null) sampleOpts.AddRange(r.distractorPhrases);

            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    bool hasOpt = (i < sampleOpts.Count);
                    optionButtons[i].gameObject.SetActive(hasOpt);
                    if (hasOpt) {
                        TextMeshProUGUI tmp = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (tmp != null) tmp.text = sampleOpts[i];
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
            headerTMP.text = "READING BRANCH (Travel Fun)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "R01 Which Travel Phrase? (in-context)";
        }
    }

    private void AutoBindReferences() {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> opts = new List<Button>();
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("option") || n.Contains("chip") || n.Contains("choice")) opts.Add(btn);
            else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("repeat") || n.Contains("audio"))) replayAudioBtn = btn;
            else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
            else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) nextButton = btn;
        }

        if (opts.Count >= 4 && (optionButtons == null || optionButtons.Length == 0)) {
            optionButtons = opts.GetRange(0, 4).ToArray();
        }

        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (progressTMP == null && (n.Contains("progresstext") || n.Contains("progress") || n.Contains("counter") || n.Contains("expressioncount"))) progressTMP = tmp;
            else if (situationCardTMP == null && (n.Contains("situation") || n.Contains("notice") || n.Contains("speech") || n.Contains("bubble") || n.Contains("prompt") || n.Contains("statement"))) situationCardTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
        }

        if (noticeboardCard == null) {
            Transform boardTr = transform.Find("NoticeboardCard") ?? transform.Find("SituationCard") ?? transform.Find("SpeechBubble") ?? transform.Find("PromptCard") ?? transform.Find("QuestionPanel");
            if (boardTr != null) noticeboardCard = boardTr;
            else if (situationCardTMP != null) noticeboardCard = situationCardTMP.transform.parent;
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

        TravelFunR01RoundData data = roundDataArray[currentRoundIndex];

        if (progressTMP != null) {
            progressTMP.text = $"Round {currentRoundIndex + 1}/{roundDataArray.Length}";
        }

        if (situationCardTMP != null) {
            situationCardTMP.text = data.situationText;
            situationCardTMP.color = Color.white;
        }

        if (noticeboardCard != null) {
            noticeboardCard.DOKill();
            noticeboardCard.localScale = Vector3.one;
            noticeboardCard.DOPunchScale(new Vector3(0.04f, 0.04f, 0f), 0.3f, 5, 0.5f);
        }

        // Prepare Choices (1 correct + distractors)
        List<string> choices = new List<string>();
        choices.Add(data.correctPhrase);
        if (data.distractorPhrases != null) {
            for (int i = 0; i < data.distractorPhrases.Length; i++) {
                if (!string.IsNullOrEmpty(data.distractorPhrases[i])) {
                    choices.Add(data.distractorPhrases[i]);
                }
            }
        }

        // Deterministic shuffle / distribution across 4 buttons
        ShuffleList(choices);
        currentRoundChoices = choices.ToArray();
        currentRoundCorrectIndex = choices.IndexOf(data.correctPhrase);

        // Setup Option Chips
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    bool hasOption = (i < currentRoundChoices.Length);
                    optionButtons[i].gameObject.SetActive(hasOption);
                    optionButtons[i].interactable = true;

                    if (hasOption) {
                        if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) {
                            optionTexts[i].text = currentRoundChoices[i];
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

        PlaySituationAudio();
    }

    private void ShuffleList<T>(IList<T> list) {
        int n = list.Count;
        while (n > 1) {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    private void PlaySituationAudio() {
        if (currentRoundIndex >= 0 && currentRoundIndex < roundDataArray.Length) {
            var data = roundDataArray[currentRoundIndex];
            bool isSlow = (slowToggle != null && slowToggle.isOn);
            AudioClip clipToPlay = (isSlow && data.slowSituationAudio != null) ? data.slowSituationAudio : data.situationAudio;

            if (clipToPlay != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            }
        }
    }

    private void OnOptionSelected(int optionIndex) {
        if (isProcessingInput || currentRoundIndex >= roundDataArray.Length) return;
        if (currentRoundChoices == null || optionIndex < 0 || optionIndex >= currentRoundChoices.Length) return;

        bool isCorrect = (optionIndex == currentRoundCorrectIndex);
        StartCoroutine(ProcessAnswerRoutine(optionIndex, isCorrect));
    }

    private IEnumerator ProcessAnswerRoutine(int optionIndex, bool isCorrect) {
        isProcessingInput = true;
        var data = roundDataArray[currentRoundIndex];

        if (isCorrect) {
            // Correct Selection
            if (!currentRoundHasRetried) {
                score++;
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // Green highlight & punch
            if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = correctColor;
            }
            if (optionButtons != null && optionIndex < optionButtons.Length && optionButtons[optionIndex] != null) {
                optionButtons[optionIndex].transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.35f, 6, 0.5f);
            }

            // Play Feedback / Definition Audio
            if (data.feedbackAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(data.feedbackAudio);
                yield return new WaitForSeconds(data.feedbackAudio.length + 0.3f);
            } else {
                yield return new WaitForSeconds(1.2f);
            }

            LoadRound(currentRoundIndex + 1);
        } else {
            // Wrong Selection
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
                // 1 Retry Allowed!
                currentRoundHasRetried = true;
                if (optionButtons != null && optionIndex < optionButtons.Length && optionButtons[optionIndex] != null) {
                    optionButtons[optionIndex].interactable = false;
                }
                isProcessingInput = false;
            } else {
                // Second Wrong - Reveal Correct Option & Play Feedback Audio
                if (optionImages != null && currentRoundCorrectIndex < optionImages.Length && optionImages[currentRoundCorrectIndex] != null) {
                    optionImages[currentRoundCorrectIndex].color = correctColor;
                }
                if (optionButtons != null && currentRoundCorrectIndex < optionButtons.Length && optionButtons[currentRoundCorrectIndex] != null) {
                    optionButtons[currentRoundCorrectIndex].transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.3f, 5, 0.5f);
                }

                if (data.feedbackAudio != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(data.feedbackAudio);
                    yield return new WaitForSeconds(data.feedbackAudio.length + 0.3f);
                } else {
                    yield return new WaitForSeconds(1.2f);
                }

                LoadRound(currentRoundIndex + 1);
            }
        }
    }

    private void OnReplayAudioClicked() {
        if (isProcessingInput) return;
        PlaySituationAudio();
    }

    private void ShowResults() {
        isProcessingInput = true;

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        int total = (roundDataArray != null) ? roundDataArray.Length : 12;
        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Score: {score} / {total}";
        }

        bool passed = score >= 10; // Success condition: at least 10 of 12 situations

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed ?
                "“Terrific! You know every travel phrase in context!”" :
                "“Good effort! Try once more to master at least 10 travel situations!”";
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

