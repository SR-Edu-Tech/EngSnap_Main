using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


/// <summary>
/// L02 Positive Tag or Negative Tag?
/// Listening Lesson 2 controller for Book 2B Unit 7 (Question Tags).
/// Echo Point with Two Arches: POSITIVE TAG on the left, NEGATIVE TAG on the right.
/// Student listens to complete sentence and selects which tag category it belongs under.
/// Success condition: Student correctly classifies at least 6 of 8 rounds.
/// </summary>
public class Masters_QuestionTags_Listening_LessonTwo : Masters_Lesson {

public enum QuestionTagCategory {
    PositiveTag = 0, // e.g., "...are you?", "...do you?", "...were they?", "...will you?"
    NegativeTag = 1  // e.g., "...aren't you?", "...isn't he?", "...doesn't he?", "...haven't you?"
}

[System.Serializable]
public class QuestionTags_ListeningL02Round {
    public int roundId;
    public string fullSentenceText;    // "You are a student, aren't you?"
    public string tagPart;             // "aren't you?"
    public QuestionTagCategory correctCategory; // PositiveTag or NegativeTag
    public AudioClip sentenceAudio;
    public AudioClip slowSentenceAudio;
}

    [Header("L02 8 Question Tag Rounds")]
    [SerializeField]
    private QuestionTags_ListeningL02Round[] rounds;

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

    [Header("Sentence Card & Audio Controls")]
    [SerializeField]
    private GameObject sentenceCardObject;
    [SerializeField]
    private Image sentenceCardBg;
    [SerializeField]
    private TextMeshProUGUI sentencePromptTMP;
    [SerializeField]
    private TextMeshProUGUI sentenceTextTMP;
    [SerializeField]
    private Button replayAudioBtn;
    [SerializeField]
    private Toggle slowToggle;

    [Header("Two Arch Category Buttons")]
    [SerializeField]
    private Button positiveTagBtn;     // Left Arch (Index 0)
    [SerializeField]
    private Button negativeTagBtn;     // Right Arch (Index 1)
    [SerializeField]
    private Image positiveTagImg;
    [SerializeField]
    private Image negativeTagImg;

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

    [Header("Navigation")]
    [SerializeField]
    private Masters_LessonSO nextLessonSO;

    [Header("Pass Threshold")]
    [SerializeField]
    private int passThreshold = 6;

    private int currentRoundIndex = 0;
    private int score = 0;
    private bool isHandlingAnswer = false;
    private bool isSlowAudio = false;

    private Color defaultPositiveColor = new Color(0.12f, 0.45f, 0.85f, 0.95f);
    private Color defaultNegativeColor = new Color(0.75f, 0.32f, 0.12f, 0.95f);
    private Color correctChipColor = new Color(0.15f, 0.75f, 0.35f, 1f);
    private Color wrongChipColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Listening;
        base.Awake();

        PurgeLegacyChildren();
        AutoBindReferences();

        if (positiveTagBtn != null) {
            positiveTagBtn.onClick.RemoveAllListeners();
            positiveTagBtn.onClick.AddListener(() => OnCategorySelected(QuestionTagCategory.PositiveTag));
        }

        if (negativeTagBtn != null) {
            negativeTagBtn.onClick.RemoveAllListeners();
            negativeTagBtn.onClick.AddListener(() => OnCategorySelected(QuestionTagCategory.NegativeTag));
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(PlayCurrentSentenceAudio);
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
        if (positiveTagBtn != null) positiveTagBtn.interactable = false;
        if (negativeTagBtn != null) negativeTagBtn.interactable = false;

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
            headerTMP.text = "LISTENING 2 (Positive Tag or Negative Tag?)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "L02 Positive Tag or Negative Tag?";
        }

        if (subtitleTMP == null) {
            Transform sTrans = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Instruction") ?? transform.Find("Subtitle");
            if (sTrans != null) subtitleTMP = sTrans.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP != null) {
            subtitleTMP.text = "Listen to the complete sentence and tap the arch it belongs under.";
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
            else if (sentenceTextTMP == null && (n.Contains("statement") || n.Contains("question") || n.Contains("prompttext") || n.Contains("sentencetext"))) sentenceTextTMP = tmp;
            else if (sentencePromptTMP == null && n.Contains("prompt")) sentencePromptTMP = tmp;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (positiveTagBtn == null && (n.Contains("positive") || n.Contains("leftarch") || n.Contains("opt_0"))) {
                positiveTagBtn = btn;
                positiveTagImg = btn.GetComponent<Image>();
            } else if (negativeTagBtn == null && (n.Contains("negative") || n.Contains("rightarch") || n.Contains("opt_1"))) {
                negativeTagBtn = btn;
                negativeTagImg = btn.GetComponent<Image>();
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

        if (slowToggle == null) {
            slowToggle = GetComponentInChildren<Toggle>(true);
        }

        if (sentenceCardObject == null) {
            Transform cardTrans = transform.Find("SentenceCard") ?? transform.Find("StatementCard") ?? transform.Find("CenterCard");
            if (cardTrans != null) {
                sentenceCardObject = cardTrans.gameObject;
                sentenceCardBg = cardTrans.GetComponent<Image>();
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
        rounds = new QuestionTags_ListeningL02Round[] {
            // Round 1: Negative Tag
            new QuestionTags_ListeningL02Round {
                roundId = 1,
                fullSentenceText = "You are a student, aren't you?",
                tagPart = "aren't you?",
                correctCategory = QuestionTagCategory.NegativeTag
            },
            // Round 2: Positive Tag
            new QuestionTags_ListeningL02Round {
                roundId = 2,
                fullSentenceText = "You aren't a teacher, are you?",
                tagPart = "are you?",
                correctCategory = QuestionTagCategory.PositiveTag
            },
            // Round 3: Negative Tag
            new QuestionTags_ListeningL02Round {
                roundId = 3,
                fullSentenceText = "He is very busy, isn't he?",
                tagPart = "isn't he?",
                correctCategory = QuestionTagCategory.NegativeTag
            },
            // Round 4: Positive Tag
            new QuestionTags_ListeningL02Round {
                roundId = 4,
                fullSentenceText = "They weren't late, were they?",
                tagPart = "were they?",
                correctCategory = QuestionTagCategory.PositiveTag
            },
            // Round 5: Negative Tag
            new QuestionTags_ListeningL02Round {
                roundId = 5,
                fullSentenceText = "He studies French, doesn't he?",
                tagPart = "doesn't he?",
                correctCategory = QuestionTagCategory.NegativeTag
            },
            // Round 6: Positive Tag
            new QuestionTags_ListeningL02Round {
                roundId = 6,
                fullSentenceText = "You don't speak French, do you?",
                tagPart = "do you?",
                correctCategory = QuestionTagCategory.PositiveTag
            },
            // Round 7: Negative Tag
            new QuestionTags_ListeningL02Round {
                roundId = 7,
                fullSentenceText = "You have studied all week, haven't you?",
                tagPart = "haven't you?",
                correctCategory = QuestionTagCategory.NegativeTag
            },
            // Round 8: Positive Tag
            new QuestionTags_ListeningL02Round {
                roundId = 8,
                fullSentenceText = "You won't fail the exam, will you?",
                tagPart = "will you?",
                correctCategory = QuestionTagCategory.PositiveTag
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

        if (sentenceTextTMP != null) {
            sentenceTextTMP.text = $"\"{round.fullSentenceText}\"";
        }

        if (sentenceCardObject != null) {
            sentenceCardObject.transform.DOKill();
            sentenceCardObject.transform.localScale = Vector3.one * 0.95f;
            sentenceCardObject.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }

        // Reset Arch button colors
        if (positiveTagImg != null) positiveTagImg.color = defaultPositiveColor;
        if (negativeTagImg != null) negativeTagImg.color = defaultNegativeColor;

        if (positiveTagBtn != null) {
            positiveTagBtn.interactable = true;
            positiveTagBtn.transform.DOKill();
            positiveTagBtn.transform.localScale = Vector3.one;
        }

        if (negativeTagBtn != null) {
            negativeTagBtn.interactable = true;
            negativeTagBtn.transform.DOKill();
            negativeTagBtn.transform.localScale = Vector3.one;
        }

        PlayCurrentSentenceAudio();
    }

    public void PlayCurrentSentenceAudio() {
        if (rounds == null || currentRoundIndex >= rounds.Length) return;
        var round = rounds[currentRoundIndex];

        AudioClip clipToPlay = isSlowAudio && round.slowSentenceAudio != null ? round.slowSentenceAudio : round.sentenceAudio;

        if (clipToPlay != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
        }
    }

    public void OnSlowToggleChanged(bool slow) {
        isSlowAudio = slow;
        PlayCurrentSentenceAudio();
    }

    public void OnCategorySelected(QuestionTagCategory selectedCategory) {
        if (isHandlingAnswer || currentRoundIndex >= rounds.Length) return;
        isHandlingAnswer = true;

        var round = rounds[currentRoundIndex];
        bool isCorrect = (selectedCategory == round.correctCategory);

        // Lock both arch buttons
        if (positiveTagBtn != null) positiveTagBtn.interactable = false;
        if (negativeTagBtn != null) negativeTagBtn.interactable = false;

        Image selectedImg = (selectedCategory == QuestionTagCategory.PositiveTag) ? positiveTagImg : negativeTagImg;
        Image correctImg = (round.correctCategory == QuestionTagCategory.PositiveTag) ? positiveTagImg : negativeTagImg;

        if (selectedImg != null) {
            selectedImg.color = isCorrect ? correctChipColor : wrongChipColor;
        }
        if (!isCorrect && correctImg != null) {
            correctImg.color = correctChipColor;
        }

        if (isCorrect) {
            score++;
            UpdateScoreDisplay();

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            StartCoroutine(PlayCorrectAndAdvanceRoutine(round));
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (sentenceCardObject != null) {
                sentenceCardObject.transform.DOShakePosition(0.4f, 15f, 20);
            }

            StartCoroutine(NextRoundRoutine(2.0f));
        }
    }

    private IEnumerator PlayCorrectAndAdvanceRoutine(QuestionTags_ListeningL02Round round) {
        yield return new WaitForSeconds(0.35f);

        if (round.sentenceAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(round.sentenceAudio);
            yield return new WaitForSeconds(round.sentenceAudio.length + 0.5f);
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
            resultTitleTMP.text = passed ? "ECHO POINT ARCHES MASTERED!" : "KEEP PRACTICING!";
            resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f) : new Color(1f, 0.65f, 0.2f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Your Score: {score} / {rounds.Length}";
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed
                ? "You correctly classified Positive and Negative Tag echoes across the valley!"
                : $"You need at least {passThreshold} correct to master this lesson. Tap Retry to try again!";
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
