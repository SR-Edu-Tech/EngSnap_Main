using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// W02 Write the Conversation
/// Comic strip writing desk for Book 2B Unit 4 (Colour Your Speech).
/// Each round presents a comic strip scenario and target idiom.
/// Student types a short conversation exchange finishing with the given idiom.
/// Includes a Word Bank rail for tapping to insert phrases directly into the text field.
/// Success condition: Student writes a valid conversation for at least 2 of 3 strips (one retry each).
/// </summary>
public class Masters_2B_ColourYourSpeech_Writing_LessonTwo : Masters_Lesson {

[System.Serializable]
public class ColourYourSpeech_WritingW02ComicStrip {
    public int stripId;
    public string situationTitle;          // e.g. "Strip 1: Holiday Fun"
    public string situationDescription;    // e.g. "A friend asks how your holiday was and it was wonderful."
    public string targetIdiom;             // e.g. "Have a blast"
    public string speakerAPrompt;          // e.g. "Friend: How was your holiday trip?"
    public string modelAnswer;             // e.g. "It was wonderful. We had a blast!"
    public string[] wordBankChips;         // 3 quick-insert chips
    public string[] requiredKeywords;      // Keywords that validate correct idiom usage
    public AudioClip stripAudio;
}

    [Header("W02 3 Comic Strip Scenarios")]
    [SerializeField]
    private ColourYourSpeech_WritingW02ComicStrip[] strips;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI subtitleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI scoreTMP;

    [Header("Comic Strip Stage Card")]
    [SerializeField]
    private GameObject stageCardObject;
    [SerializeField]
    private TextMeshProUGUI stripTitleTMP;
    [SerializeField]
    private TextMeshProUGUI situationDescTMP;
    [SerializeField]
    private TextMeshProUGUI targetIdiomBadgeTMP;
    [SerializeField]
    private TextMeshProUGUI speakerAPromptTMP;
    [SerializeField]
    private Button replayAudioBtn;

    [Header("Word Bank Rail")]
    [SerializeField]
    private Button[] wordBankChipButtons; // 3 Chips
    [SerializeField]
    private TextMeshProUGUI[] wordBankChipTexts;

    [Header("Input Field & Submit Button")]
    [SerializeField]
    private TMP_InputField inputField;
    [SerializeField]
    private Button submitBtn;

    [Header("Feedback / Explanation Banner")]
    [SerializeField]
    private GameObject feedbackBanner;
    [SerializeField]
    private TextMeshProUGUI feedbackTextTMP;

    [Header("Results & Retry Panel")]
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultTitleTMP;
    [SerializeField]
    private TextMeshProUGUI resultScoreTMP;
    [SerializeField]
    private TextMeshProUGUI resultStatusTMP;
    [SerializeField]
    private Button retryBtn;
    [SerializeField]
    private Button returnHubBtn;

    [Header("Audio References")]
    [SerializeField]
    private AudioClip introAudio;
    [SerializeField]
    private AudioClip recapAudio;
    [SerializeField]
    private AudioClip sfxCorrect;
    [SerializeField]
    private AudioClip sfxWrong;

    private int currentStripIndex = 0;
    private int score = 0;
    private int attemptCountOnCurrentStrip = 0;
    private bool isHandlingAnswer = false;
    private bool isIntroPhase = true;

    private Color normalInputBorderColor = new Color(0.2f, 0.45f, 0.75f, 1f);
    private Color correctInputBorderColor = new Color(0.13f, 0.65f, 0.32f, 1f);
    private Color wrongInputBorderColor = new Color(0.82f, 0.2f, 0.2f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Writing;
        base.Awake();

        PurgeLegacyChildren();
        AutoBindReferences();

        if (submitBtn != null) {
            submitBtn.onClick.RemoveAllListeners();
            submitBtn.onClick.AddListener(OnSubmitClicked);
        }

        if (inputField != null) {
            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener((val) => OnSubmitClicked());
        }

        if (wordBankChipButtons != null && wordBankChipButtons.Length > 0) {
            wordBankChipTexts = new TextMeshProUGUI[wordBankChipButtons.Length];
            for (int i = 0; i < wordBankChipButtons.Length; i++) {
                if (wordBankChipButtons[i] != null) {
                    int index = i;
                    wordBankChipTexts[i] = wordBankChipButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    wordBankChipButtons[i].onClick.RemoveAllListeners();
                    wordBankChipButtons[i].onClick.AddListener(() => OnWordBankChipClicked(index));
                }
            }
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(StartNewGame);
        }

        if (returnHubBtn != null) {
            returnHubBtn.onClick.RemoveAllListeners();
            returnHubBtn.onClick.AddListener(() => {
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Writing);
                }
            });
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }

        if (feedbackBanner != null) {
            feedbackBanner.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();

        PurgeLegacyChildren();
        EnsureHeaderAndTitle();

        if (strips == null || strips.Length == 0) {
            PopulateFailsafeStrips();
        }

        // Lock controls during introduction
        isIntroPhase = true;
        SetControlsInteractable(false);
        StartCoroutine(BeginLessonRoutine());
    }

    private IEnumerator BeginLessonRoutine() {
        if (progressTMP != null) progressTMP.text = "Introduction";

        if (strips != null && strips.Length > 0) {
            if (stripTitleTMP != null) stripTitleTMP.text = strips[0].situationTitle;
            if (situationDescTMP != null) situationDescTMP.text = strips[0].situationDescription;
            if (targetIdiomBadgeTMP != null) targetIdiomBadgeTMP.text = $"Target Idiom: {strips[0].targetIdiom}";
            if (speakerAPromptTMP != null) speakerAPromptTMP.text = strips[0].speakerAPrompt;
        }

        AudioClip clipToPlay = introAudio != null ? introAudio : narratorSpeech;
        if (clipToPlay != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            yield return new WaitForSeconds(clipToPlay.length + 0.3f);
        } else {
            yield return new WaitForSeconds(1.0f);
        }

        isIntroPhase = false;
        StartNewGame();
    }

    private void SetControlsInteractable(bool interactable) {
        if (inputField != null) inputField.interactable = interactable;
        if (submitBtn != null) submitBtn.interactable = interactable;
        if (replayAudioBtn != null) replayAudioBtn.interactable = interactable;

        if (wordBankChipButtons != null) {
            foreach (var btn in wordBankChipButtons) {
                if (btn != null) btn.interactable = interactable;
            }
        }
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        AutoBindReferences();

        if (strips == null || strips.Length == 0) {
            PopulateFailsafeStrips();
        }

        if (headerTMP != null) headerTMP.text = "WRITING BRANCH (Writing Desk)";
        if (titleTMP != null) titleTMP.text = "W02 Write the Conversation";
        if (subtitleTMP != null) subtitleTMP.text = "Write a short conversation for each comic strip, finishing with the given idiom!";
        if (progressTMP != null) progressTMP.text = "Strip 1/3";
        if (scoreTMP != null) scoreTMP.text = "Score: 0/3";

        if (strips != null && strips.Length > 0) {
            if (stripTitleTMP != null) stripTitleTMP.text = strips[0].situationTitle;
            if (situationDescTMP != null) situationDescTMP.text = strips[0].situationDescription;
            if (targetIdiomBadgeTMP != null) targetIdiomBadgeTMP.text = $"Target Idiom: {strips[0].targetIdiom}";
            if (speakerAPromptTMP != null) speakerAPromptTMP.text = strips[0].speakerAPrompt;
        }
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer",
            "SpawnArea", "GamePlayGameObject"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(lTrans.gameObject);
                else DestroyImmediate(lTrans.gameObject);
            }
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch") ?? transform.Find("Heading");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "WRITING BRANCH (Writing Desk)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "W02 Write the Conversation";
        }

        if (subtitleTMP == null) {
            Transform sTrans = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Instruction") ?? transform.Find("Subtitle");
            if (sTrans != null) subtitleTMP = sTrans.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP != null) {
            subtitleTMP.text = "Write a short conversation for each comic strip, finishing with the given idiom!";
        }
    }

    private void AutoBindReferences() {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading"))) headerTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (subtitleTMP == null && (n.Contains("subtitle") || n.Contains("instruction"))) subtitleTMP = tmp;
            else if (progressTMP == null && (n.Contains("progress") || n.Contains("count") || n.Contains("roundcount"))) progressTMP = tmp;
            else if (scoreTMP == null && n.Contains("score")) scoreTMP = tmp;
            else if (stripTitleTMP == null && n.Contains("striptitle")) stripTitleTMP = tmp;
            else if (situationDescTMP == null && n.Contains("situationdesc")) situationDescTMP = tmp;
            else if (targetIdiomBadgeTMP == null && n.Contains("targetidiom")) targetIdiomBadgeTMP = tmp;
            else if (speakerAPromptTMP == null && n.Contains("speakeraprompt")) speakerAPromptTMP = tmp;
            else if (feedbackTextTMP == null && n.Contains("feedback")) feedbackTextTMP = tmp;
        }

        if (inputField == null) {
            inputField = GetComponentInChildren<TMP_InputField>(true);
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> chipBtns = new List<Button>();
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("chip") || n.Contains("wordbank")) {
                chipBtns.Add(btn);
            } else if (submitBtn == null && (n.Contains("submit") || n.Contains("check") || n.Contains("enter"))) {
                submitBtn = btn;
            } else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("audio") || n.Contains("speaker"))) {
                replayAudioBtn = btn;
            } else if (retryBtn == null && n.Contains("retry")) {
                retryBtn = btn;
            } else if (returnHubBtn == null && (n.Contains("hub") || n.Contains("home"))) {
                returnHubBtn = btn;
            } else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) {
                nextButton = btn;
            }
        }

        if (chipBtns.Count > 0) {
            wordBankChipButtons = chipBtns.ToArray();
            wordBankChipTexts = new TextMeshProUGUI[wordBankChipButtons.Length];
            for (int i = 0; i < wordBankChipButtons.Length; i++) {
                wordBankChipTexts[i] = wordBankChipButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        if (stageCardObject == null) {
            Transform scTrans = transform.Find("ComicStageCard") ?? transform.Find("StageCard") ?? transform.Find("WritingStage");
            if (scTrans != null) stageCardObject = scTrans.gameObject;
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("ResultPopup");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    public void PopulateFailsafeStrips() {
        strips = new ColourYourSpeech_WritingW02ComicStrip[] {
            // Strip 1: Holiday Fun (Have a blast)
            new ColourYourSpeech_WritingW02ComicStrip {
                stripId = 1,
                situationTitle = "Strip 1: Holiday Fun",
                situationDescription = "A friend asks how your holiday was and it was wonderful.",
                targetIdiom = "Have a blast",
                speakerAPrompt = "Friend: \"How was your holiday trip?\"",
                modelAnswer = "It was wonderful. We had a blast!",
                wordBankChips = new string[] {
                    "We had a blast!",
                    "It was wonderful.",
                    "Have a blast"
                },
                requiredKeywords = new string[] { "blast", "have a blast", "had a blast" }
            },
            // Strip 2: Three Jobs (Bite off more than you can chew)
            new ColourYourSpeech_WritingW02ComicStrip {
                stripId = 2,
                situationTitle = "Strip 2: Too Many Jobs",
                situationDescription = "You took on three jobs at once and could not finish any of them.",
                targetIdiom = "Bite off more than you can chew",
                speakerAPrompt = "Friend: \"Did you finish all three jobs you took on?\"",
                modelAnswer = "I think I bit off more than I could chew.",
                wordBankChips = new string[] {
                    "I bit off more than I could chew.",
                    "Too many tasks.",
                    "Bite off more than you can chew"
                },
                requiredKeywords = new string[] { "chew", "bit off", "bite off" }
            },
            // Strip 3: Friends Again (Bury the hatchet)
            new ColourYourSpeech_WritingW02ComicStrip {
                stripId = 3,
                situationTitle = "Strip 3: Friends Again",
                situationDescription = "You and your friend argued last week and today you are friends again; your mother asks about it.",
                targetIdiom = "Bury the hatchet",
                speakerAPrompt = "Mother: \"Aren't you and your friend still upset with each other?\"",
                modelAnswer = "We buried the hatchet, mom. We are friends again.",
                wordBankChips = new string[] {
                    "We buried the hatchet, mom.",
                    "We are friends again.",
                    "Bury the hatchet"
                },
                requiredKeywords = new string[] { "hatchet", "buried", "bury" }
            }
        };
    }

    public void StartNewGame() {
        currentStripIndex = 0;
        score = 0;
        attemptCountOnCurrentStrip = 0;
        isHandlingAnswer = false;

        if (inputField == null || submitBtn == null) {
            AutoBindReferences();
        }

        if (resultPanel != null) resultPanel.SetActive(false);
        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        UpdateScoreUI();
        LoadStrip(currentStripIndex);
    }

    private void LoadStrip(int stripIdx) {
        if (strips == null || strips.Length == 0) return;

        if (stripIdx >= strips.Length || stripIdx >= 3) {
            EndGame();
            return;
        }

        var strip = strips[stripIdx];
        isHandlingAnswer = false;
        attemptCountOnCurrentStrip = 0;

        if (progressTMP != null) progressTMP.text = $"Strip {stripIdx + 1}/3";

        if (stripTitleTMP != null) stripTitleTMP.text = strip.situationTitle;
        if (situationDescTMP != null) situationDescTMP.text = strip.situationDescription;
        if (targetIdiomBadgeTMP != null) targetIdiomBadgeTMP.text = $"Target Idiom: {strip.targetIdiom}";

        if (speakerAPromptTMP != null) {
            speakerAPromptTMP.text = strip.speakerAPrompt;
            speakerAPromptTMP.transform.DOKill();
            speakerAPromptTMP.transform.localScale = Vector3.zero;
            speakerAPromptTMP.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }

        // Setup Word Bank Rail Chips
        for (int i = 0; i < wordBankChipButtons.Length; i++) {
            if (i < strip.wordBankChips.Length && wordBankChipButtons[i] != null) {
                wordBankChipButtons[i].gameObject.SetActive(true);
                wordBankChipButtons[i].interactable = true;
                if (wordBankChipTexts != null && i < wordBankChipTexts.Length && wordBankChipTexts[i] != null) {
                    wordBankChipTexts[i].text = strip.wordBankChips[i];
                }
            } else if (wordBankChipButtons[i] != null) {
                wordBankChipButtons[i].gameObject.SetActive(false);
            }
        }

        if (inputField != null) {
            inputField.text = "";
            inputField.interactable = true;
            inputField.ActivateInputField();

            Image inputBg = inputField.GetComponent<Image>();
            if (inputBg != null) inputBg.color = normalInputBorderColor;
        }

        if (submitBtn != null) {
            submitBtn.interactable = true;
            submitBtn.transform.DOKill();
            submitBtn.transform.localScale = Vector3.one;
        }

        SetControlsInteractable(true);

        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        PlayStripAudio(strip);
    }

    private void PlayStripAudio(ColourYourSpeech_WritingW02ComicStrip strip) {
        if (strip.stripAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(strip.stripAudio);
        }
    }

    private void OnWordBankChipClicked(int chipIndex) {
        if (isHandlingAnswer || isIntroPhase || currentStripIndex >= strips.Length) return;

        var strip = strips[currentStripIndex];
        if (chipIndex < strip.wordBankChips.Length && inputField != null) {
            string chipText = strip.wordBankChips[chipIndex];
            if (string.IsNullOrEmpty(inputField.text)) {
                inputField.text = chipText;
            } else {
                inputField.text = inputField.text.TrimEnd() + " " + chipText;
            }
            inputField.caretPosition = inputField.text.Length;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            }
        }
    }

    private void OnSubmitClicked() {
        if (isHandlingAnswer || isIntroPhase || currentStripIndex >= strips.Length) return;

        if (inputField == null) return;
        string userText = inputField.text.Trim();

        if (string.IsNullOrEmpty(userText)) {
            ShowFeedback("Please write your conversation reply in the field!", false);
            return;
        }

        var strip = strips[currentStripIndex];
        bool hasTargetIdiom = false;

        // Check if any of the required keywords are present
        foreach (string kw in strip.requiredKeywords) {
            if (userText.IndexOf(kw, System.StringComparison.OrdinalIgnoreCase) >= 0) {
                hasTargetIdiom = true;
                break;
            }
        }

        if (hasTargetIdiom) {
            isHandlingAnswer = true;
            score++;
            UpdateScoreUI();

            if (sfxCorrect != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxCorrect);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            Image inputBg = inputField.GetComponent<Image>();
            if (inputBg != null) inputBg.color = correctInputBorderColor;

            if (submitBtn != null) {
                submitBtn.transform.DOKill();
                submitBtn.transform.DOScale(Vector3.one * 1.08f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }

            ShowFeedback($"Great job! You finished the comic strip using '{strip.targetIdiom}'!", true);
            StartCoroutine(AdvanceAfterDelay(2.2f));
        } else {
            attemptCountOnCurrentStrip++;

            if (attemptCountOnCurrentStrip == 1) {
                // First retry
                if (sfxWrong != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(sfxWrong);
                } else if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                Image inputBg = inputField.GetComponent<Image>();
                if (inputBg != null) inputBg.color = wrongInputBorderColor;

                if (inputField != null) {
                    inputField.transform.DOShakePosition(0.25f, new Vector3(8f, 0f, 0f), 10, 90, false, true);
                }

                ShowFeedback($"Make sure your reply includes the target idiom: '{strip.targetIdiom}'! Try once more.", false);
            } else {
                // Second wrong - show model answer and proceed
                isHandlingAnswer = true;

                if (sfxWrong != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(sfxWrong);
                } else if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                Image inputBg = inputField.GetComponent<Image>();
                if (inputBg != null) inputBg.color = wrongInputBorderColor;

                if (inputField != null) {
                    inputField.text = strip.modelAnswer;
                }

                ShowFeedback($"Model closing line: \"{strip.modelAnswer}\" ({strip.targetIdiom})", false);
                StartCoroutine(AdvanceAfterDelay(2.5f));
            }
        }
    }

    private void ShowFeedback(string msg, bool isSuccess) {
        if (feedbackBanner != null) {
            feedbackBanner.SetActive(true);
            if (feedbackTextTMP != null) feedbackTextTMP.text = msg;
            feedbackBanner.transform.DOKill();
            feedbackBanner.transform.localScale = Vector3.zero;
            feedbackBanner.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        }
    }

    private void OnReplayAudioClicked() {
        if (isIntroPhase) return;
        if (currentStripIndex < strips.Length) {
            PlayStripAudio(strips[currentStripIndex]);
            if (stageCardObject != null) {
                stageCardObject.transform.DOKill();
                stageCardObject.transform.DOScale(Vector3.one * 1.03f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }
        }
    }

    private IEnumerator AdvanceAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        currentStripIndex++;
        LoadStrip(currentStripIndex);
    }

    private void EndGame() {
        bool isPassed = (score >= 2);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = isPassed ? "COMIC WRITING CHALLENGE COMPLETED!" : "CHALLENGE NOT PASSED";
            resultTitleTMP.color = isPassed ? new Color(0.15f, 0.85f, 0.4f) : new Color(0.95f, 0.35f, 0.35f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Completed: {score} / 3 Comic Strips (Target: 2)";
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = isPassed
                ? "Excellent! You finished the comic strip conversations using the proper idioms!"
                : "Try again! Complete at least 2 of 3 strips to pass!";
        }

        if (isPassed && recapAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(recapAudio);
        }

        if (isPassed && Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Writing);
        }
    }

    private void UpdateScoreUI() {
        if (scoreTMP != null) {
            scoreTMP.text = $"Score: {score}/3";
        }
    }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}

