using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// L01 Hear It — Finish the Echo
/// Listening Lesson 1 controller for Book 2B Unit 7 (Question Tags).
/// ARIA calls out a statement from the unit and stops before the tag; 4 tag chips appear below.
/// Student picks the correct tag that the echo should answer with.
/// Success condition: Student picks the correct tag in at least 8 of 10 rounds.
/// </summary>
public class Masters_QuestionTags_Listening_LessonOne : Masters_Lesson {

[System.Serializable]
public class QuestionTags_ListeningL01Round {
    public int roundId;
    public string statementText;       // "You are a student,"
    public string correctTag;          // "aren't you?"
    public string fullEchoSentence;    // "You are a student, aren't you?"
    public string[] optionTags;        // 4 tag option chips
    public int correctOptionIndex;     // Index 0..3
    public AudioClip statementAudio;
    public AudioClip slowStatementAudio;
    public AudioClip fullEchoAudio;
}

    [Header("L01 10 Question Tag Rounds")]
    [SerializeField]
    private QuestionTags_ListeningL01Round[] rounds;

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

    [Header("Echo Point Statement Display")]
    [SerializeField]
    private GameObject statementCardObject;
    [SerializeField]
    private Image statementCardBg;
    [SerializeField]
    private TextMeshProUGUI statementPromptTMP;
    [SerializeField]
    private TextMeshProUGUI statementTextTMP;
    [SerializeField]
    private Button replayAudioBtn;
    [SerializeField]
    private Toggle slowToggle;

    [Header("4 Answer Option Tag Chips")]
    [SerializeField]
    private Button[] optionButtons;       // 4 Option Buttons
    [SerializeField]
    private TextMeshProUGUI[] optionTexts;// 4 Option Texts
    [SerializeField]
    private Image[] optionImages;         // 4 Option Button Images

    [Header("Results & Completion Panel")]
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
    private AudioClip sfxCorrect;
    [SerializeField]
    private AudioClip sfxWrong;

    [Header("Pass Threshold")]
    [SerializeField]
    private int passThreshold = 8;

    private int currentRoundIndex = 0;
    private int score = 0;
    private bool isHandlingAnswer = false;
    private bool isSlowAudio = false;

    private Color defaultChipColor = new Color(0.14f, 0.45f, 0.75f, 0.95f);
    private Color correctChipColor = new Color(0.15f, 0.75f, 0.35f, 1f);
    private Color wrongChipColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Listening;
        base.Awake();

        PurgeLegacyChildren();
        AutoBindReferences();

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                int index = i;
                if (optionButtons[i] != null) {
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
                }
            }
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(PlayCurrentStatementAudio);
        }

        if (slowToggle != null) {
            slowToggle.onValueChanged.RemoveAllListeners();
            slowToggle.onValueChanged.AddListener(OnSlowToggleChanged);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(RestartLesson);
        }

        if (returnHubBtn != null) {
            returnHubBtn.onClick.RemoveAllListeners();
            returnHubBtn.onClick.AddListener(OnReturnHubClicked);
        }

        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }

    protected override void Start() {
        base.Start();

        PurgeLegacyChildren();
        EnsureHeaderAndTitle();

        if (rounds == null || rounds.Length == 0) {
            PopulateDefaultRounds();
        }

        if (resultPanel != null) resultPanel.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        StartCoroutine(InitializeLessonRoutine());
    }

    private IEnumerator InitializeLessonRoutine() {
        if (optionButtons != null) {
            foreach (var btn in optionButtons) {
                if (btn != null) btn.interactable = false;
            }
        }

        if (Masters_AudioManager.Instance != null) {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(1.5f);
        }

        yield return new WaitForSeconds(0.4f);
        StartLessonSequence();
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
            headerTMP.text = "LISTENING 1 (Hear It — Finish the Echo)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "L01 Hear It — Finish the Echo";
        }

        if (subtitleTMP == null) {
            Transform sTrans = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Instruction") ?? transform.Find("Subtitle");
            if (sTrans != null) subtitleTMP = sTrans.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP != null) {
            subtitleTMP.text = "Listen to the statement and tap the tag that the echo should answer with.";
        }
    }

    private void AutoBindReferences() {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading"))) headerTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (subtitleTMP == null && (n.Contains("subtitle") || n.Contains("instruction"))) subtitleTMP = tmp;
            else if (progressTMP == null && (n.Contains("progress") || n.Contains("counter") || n.Contains("roundcount"))) progressTMP = tmp;
            else if (scoreTMP == null && n.Contains("score")) scoreTMP = tmp;
            else if (statementTextTMP == null && (n.Contains("statement") || n.Contains("question") || n.Contains("prompttext"))) statementTextTMP = tmp;
            else if (statementPromptTMP == null && n.Contains("prompt")) statementPromptTMP = tmp;
        }

        List<Button> optBtns = new List<Button>();
        List<TextMeshProUGUI> optTexts = new List<TextMeshProUGUI>();
        List<Image> optImgs = new List<Image>();

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("option") || n.Contains("choice") || n.Contains("chip") || n.Contains("btn_tag")) {
                optBtns.Add(btn);
                var t = btn.GetComponentInChildren<TextMeshProUGUI>(true);
                if (t != null) optTexts.Add(t);
                var img = btn.GetComponent<Image>();
                if (img != null) optImgs.Add(img);
            } else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("audio") || n.Contains("speaker") || n.Contains("listen"))) {
                replayAudioBtn = btn;
            } else if (retryBtn == null && n.Contains("retry")) {
                retryBtn = btn;
            } else if (returnHubBtn == null && (n.Contains("hub") || n.Contains("done") || n.Contains("finish"))) {
                returnHubBtn = btn;
            } else if (nextButton == null && n == "nextbutton") {
                nextButton = btn;
            }
        }

        if (optBtns.Count > 0) {
            optionButtons = optBtns.ToArray();
            optionTexts = optTexts.ToArray();
            optionImages = optImgs.ToArray();
        }

        if (slowToggle == null) {
            slowToggle = GetComponentInChildren<Toggle>(true);
        }

        if (statementCardObject == null) {
            Transform cardTrans = transform.Find("StatementCard") ?? transform.Find("RadioCard") ?? transform.Find("CenterCard") ?? transform.Find("EchoCard");
            if (cardTrans != null) {
                statementCardObject = cardTrans.gameObject;
                statementCardBg = cardTrans.GetComponent<Image>();
            }
        }

        if (resultPanel == null) {
            Transform resTrans = transform.Find("ResultPanel") ?? transform.Find("Results") ?? transform.Find("GameOverPanel");
            if (resTrans != null) {
                resultPanel = resTrans.gameObject;
                TextMeshProUGUI[] resTmps = resTrans.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var rt in resTmps) {
                    string rn = rt.gameObject.name.ToLower();
                    if (resultTitleTMP == null && (rn.Contains("title") || rn.Contains("heading"))) resultTitleTMP = rt;
                    else if (resultScoreTMP == null && rn.Contains("score")) resultScoreTMP = rt;
                    else if (resultStatusTMP == null && (rn.Contains("status") || rn.Contains("desc") || rn.Contains("message"))) resultStatusTMP = rt;
                }
            }
        }
    }

    public void PopulateDefaultRounds() {
        rounds = new QuestionTags_ListeningL01Round[] {
            // Round 1
            new QuestionTags_ListeningL01Round {
                roundId = 1,
                statementText = "You are a student,",
                correctTag = "aren't you?",
                fullEchoSentence = "You are a student, aren't you?",
                optionTags = new string[] { "aren't you?", "are you?", "isn't he?", "don't you?" },
                correctOptionIndex = 0
            },
            // Round 2
            new QuestionTags_ListeningL01Round {
                roundId = 2,
                statementText = "You aren't a teacher,",
                correctTag = "are you?",
                fullEchoSentence = "You aren't a teacher, are you?",
                optionTags = new string[] { "aren't you?", "are you?", "do you?", "is he?" },
                correctOptionIndex = 1
            },
            // Round 3
            new QuestionTags_ListeningL01Round {
                roundId = 3,
                statementText = "He is very busy,",
                correctTag = "isn't he?",
                fullEchoSentence = "He is very busy, isn't he?",
                optionTags = new string[] { "is he?", "aren't you?", "isn't he?", "doesn't he?" },
                correctOptionIndex = 2
            },
            // Round 4
            new QuestionTags_ListeningL01Round {
                roundId = 4,
                statementText = "They weren't late,",
                correctTag = "were they?",
                fullEchoSentence = "They weren't late, were they?",
                optionTags = new string[] { "weren't they?", "was he?", "did they?", "were they?" },
                correctOptionIndex = 3
            },
            // Round 5
            new QuestionTags_ListeningL01Round {
                roundId = 5,
                statementText = "You don't speak French,",
                correctTag = "do you?",
                fullEchoSentence = "You don't speak French, do you?",
                optionTags = new string[] { "do you?", "don't you?", "are you?", "does he?" },
                correctOptionIndex = 0
            },
            // Round 6
            new QuestionTags_ListeningL01Round {
                roundId = 6,
                statementText = "He studies French,",
                correctTag = "doesn't he?",
                fullEchoSentence = "He studies French, doesn't he?",
                optionTags = new string[] { "does he?", "doesn't he?", "isn't he?", "didn't he?" },
                correctOptionIndex = 1
            },
            // Round 7
            new QuestionTags_ListeningL01Round {
                roundId = 7,
                statementText = "You have studied all week,",
                correctTag = "haven't you?",
                fullEchoSentence = "You have studied all week, haven't you?",
                optionTags = new string[] { "have you?", "hadn't you?", "haven't you?", "didn't you?" },
                correctOptionIndex = 2
            },
            // Round 8
            new QuestionTags_ListeningL01Round {
                roundId = 8,
                statementText = "You won't fail the exam,",
                correctTag = "will you?",
                fullEchoSentence = "You won't fail the exam, will you?",
                optionTags = new string[] { "won't you?", "do you?", "can you?", "will you?" },
                correctOptionIndex = 3
            },
            // Round 9
            new QuestionTags_ListeningL01Round {
                roundId = 9,
                statementText = "You can speak two languages,",
                correctTag = "can't you?",
                fullEchoSentence = "You can speak two languages, can't you?",
                optionTags = new string[] { "can't you?", "can you?", "couldn't you?", "don't you?" },
                correctOptionIndex = 0
            },
            // Round 10
            new QuestionTags_ListeningL01Round {
                roundId = 10,
                statementText = "We mustn't say anything,",
                correctTag = "must we?",
                fullEchoSentence = "We mustn't say anything, must we?",
                optionTags = new string[] { "mustn't we?", "must we?", "must you?", "should we?" },
                correctOptionIndex = 1
            }
        };
    }

    public void StartLessonSequence() {
        currentRoundIndex = 0;
        score = 0;
        isHandlingAnswer = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        UpdateScoreDisplay();
        LoadRound(currentRoundIndex);
    }

    public void LoadRound(int roundIdx) {
        if (rounds == null || rounds.Length == 0) return;
        if (roundIdx < 0 || roundIdx >= rounds.Length) {
            ShowResults();
            return;
        }

        currentRoundIndex = roundIdx;
        isHandlingAnswer = false;
        var round = rounds[currentRoundIndex];

        if (progressTMP != null) progressTMP.text = $"Round {roundIdx + 1}/{rounds.Length}";

        if (statementTextTMP != null) {
            statementTextTMP.text = $"\"{round.statementText} <color=#FFD700>___ ?</color>\"";
        }

        if (statementCardObject != null) {
            statementCardObject.transform.DOKill();
            statementCardObject.transform.localScale = Vector3.one * 0.95f;
            statementCardObject.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }

        // Setup Option Buttons
        if (optionButtons != null && round.optionTags != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (i < round.optionTags.Length && optionButtons[i] != null) {
                    optionButtons[i].gameObject.SetActive(true);
                    optionButtons[i].interactable = true;

                    if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) {
                        optionTexts[i].text = round.optionTags[i];
                    }

                    if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                        optionImages[i].color = defaultChipColor;
                    }

                    // Punch / scale anim
                    optionButtons[i].transform.DOKill();
                    optionButtons[i].transform.localScale = Vector3.zero;
                    optionButtons[i].transform.DOScale(Vector3.one, 0.25f + (i * 0.05f)).SetEase(Ease.OutBack);
                } else if (optionButtons[i] != null) {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }

        PlayCurrentStatementAudio();
    }

    public void PlayCurrentStatementAudio() {
        if (rounds == null || currentRoundIndex >= rounds.Length) return;
        var round = rounds[currentRoundIndex];

        AudioClip clipToPlay = isSlowAudio && round.slowStatementAudio != null ? round.slowStatementAudio : round.statementAudio;

        if (clipToPlay != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
        }
    }

    public void OnSlowToggleChanged(bool slow) {
        isSlowAudio = slow;
        PlayCurrentStatementAudio();
    }

    public void OnOptionSelected(int optionIndex) {
        if (isHandlingAnswer || currentRoundIndex >= rounds.Length) return;
        isHandlingAnswer = true;

        var round = rounds[currentRoundIndex];
        bool isCorrect = (optionIndex == round.correctOptionIndex);

        // Lock all buttons
        if (optionButtons != null) {
            foreach (var btn in optionButtons) {
                if (btn != null) btn.interactable = false;
            }
        }

        // Color selected option
        if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
            optionImages[optionIndex].color = isCorrect ? correctChipColor : wrongChipColor;
        }

        // Highlight correct option if student was wrong
        if (!isCorrect && optionImages != null && round.correctOptionIndex < optionImages.Length && optionImages[round.correctOptionIndex] != null) {
            optionImages[round.correctOptionIndex].color = correctChipColor;
        }

        if (isCorrect) {
            score++;
            UpdateScoreDisplay();

            if (statementTextTMP != null) {
                statementTextTMP.text = $"\"{round.fullEchoSentence}\"";
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            StartCoroutine(PlayCorrectAndAdvanceRoutine(round));
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (statementCardObject != null) {
                statementCardObject.transform.DOShakePosition(0.4f, 15f, 20);
            }

            StartCoroutine(NextRoundRoutine(2.0f));
        }
    }

    private IEnumerator PlayCorrectAndAdvanceRoutine(QuestionTags_ListeningL01Round round) {
        yield return new WaitForSeconds(0.35f);

        if (round.fullEchoAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(round.fullEchoAudio);
            yield return new WaitForSeconds(round.fullEchoAudio.length + 0.6f);
        } else {
            yield return new WaitForSeconds(1.8f);
        }

        if (currentRoundIndex + 1 < rounds.Length) {
            LoadRound(currentRoundIndex + 1);
        } else {
            ShowResults();
        }
    }

    private IEnumerator NextRoundRoutine(float delay) {
        yield return new WaitForSeconds(delay);

        if (currentRoundIndex + 1 < rounds.Length) {
            LoadRound(currentRoundIndex + 1);
        } else {
            ShowResults();
        }
    }

    private void UpdateScoreDisplay() {
        if (scoreTMP != null) scoreTMP.text = $"Score: {score}/{rounds.Length}";
    }

    private void ShowResults() {
        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        bool passed = (score >= passThreshold);

        if (resultTitleTMP != null) {
            resultTitleTMP.text = passed ? "ECHO POINT MASTERED!" : "KEEP PRACTICING!";
            resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f) : new Color(1f, 0.65f, 0.2f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Your Score: {score} / {rounds.Length}";
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed
                ? "You correctly matched the Question Tag echoes across the valley!"
                : "You need at least 8 correct to master this lesson. Tap Retry to try again!";
        }

        if (retryBtn != null) {
            retryBtn.gameObject.SetActive(!passed);
        }

        if (returnHubBtn != null) {
            returnHubBtn.gameObject.SetActive(true);
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(passed);
            if (passed) NextButtonAnimation();
        }

        if (passed) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Reading);
            }
        }
    }

    public void RestartLesson() {
        StartLessonSequence();
    }

    public void OnReturnHubClicked() {
        OnNextButtonClicked();
    }

    [Header("Navigation")]
    [SerializeField]
    private Masters_LessonSO nextLessonSO;

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Reading);
            }

            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}
