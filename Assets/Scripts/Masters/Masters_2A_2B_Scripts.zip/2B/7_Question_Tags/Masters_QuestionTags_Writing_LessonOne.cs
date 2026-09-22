using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// W01 Write the Tag — Stone Tablet Cloze Controller for Book 2B Unit 7 (Question Tags).
/// Displays a statement on a stone tablet with an empty tag space.
/// Student types the correct tag.
/// When the student types a wrong answer, a dedicated Hint Box appears showing the correct answer & rule.
/// Success condition: Student types the correct tag in at least 7 of 10 items.
/// </summary>
public class Masters_QuestionTags_Writing_LessonOne : Masters_Lesson {

[System.Serializable]
public class QuestionTags_WritingW01Item {
    public int roundId;
    public string statementText;         // e.g. "You are a student,"
    public string primaryAcceptedTag;    // e.g. "aren't you?"
    public string[] alternativeTags;     // e.g. new string[] { "aren't you", "arent you", "arent you?" }
    public string fullEchoSentence;      // e.g. "You are a student, aren't you?"
    public string hintText;              // e.g. "Positive statement [+] 'You are' requires negative tag [-] 'aren't you?'"
    public AudioClip statementAudio;
    public AudioClip fullEchoAudio;
}

    [Header("10 Writing Tag Rounds")]
    [SerializeField]
    private QuestionTags_WritingW01Item[] questions;

    [Header("UI Headers & Counters")]
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
    [SerializeField]
    private TextMeshProUGUI hintTMP;

    [Header("Stone Tablet Card")]
    [SerializeField]
    private GameObject tabletCardObject;
    [SerializeField]
    private Image tabletCardBg;
    [SerializeField]
    private TextMeshProUGUI tabletPromptTMP;
    [SerializeField]
    private TextMeshProUGUI statementTextTMP;
    [SerializeField]
    private Button replayAudioBtn;

    [Header("Typing & Submission Controls")]
    [SerializeField]
    private TMP_InputField inputField;
    [SerializeField]
    private Image inputFieldBg;
    [SerializeField]
    private Button submitBtn;

    [Header("Hint Box (Shows Correct Answer & Rule on Wrong)")]
    [SerializeField]
    private GameObject hintBoxObject;
    [SerializeField]
    private TextMeshProUGUI hintTitleTMP;
    [SerializeField]
    private TextMeshProUGUI hintAnswerTMP;
    [SerializeField]
    private TextMeshProUGUI hintRuleTMP;

    [Header("Results Panel")]
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
    private int passThreshold = 7;

    private int currentQuestionIndex = 0;
    private int score = 0;
    private bool isHandlingSubmission = false;

    private readonly Color defaultInputColor = new Color(0.12f, 0.16f, 0.28f, 1f);
    private readonly Color correctColor = new Color(0.15f, 0.75f, 0.35f, 1f);
    private readonly Color wrongColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Writing;
        base.Awake();

        AutoBindReferences();

        if (submitBtn != null) {
            submitBtn.onClick.RemoveAllListeners();
            submitBtn.onClick.AddListener(OnSubmitClicked);
        }

        if (inputField != null) {
            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener((val) => OnSubmitClicked());
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(PlayCurrentStatementAudio);
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

        EnsureHeaderAndTitle();

        if (questions == null || questions.Length == 0) {
            PopulateDefaultQuestions();
        }

        if (resultPanel != null) resultPanel.SetActive(false);
        if (hintBoxObject != null) hintBoxObject.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        StartCoroutine(InitializeLessonRoutine());
    }

    private IEnumerator InitializeLessonRoutine() {
        if (inputField != null) inputField.interactable = false;
        if (submitBtn != null) submitBtn.interactable = false;

        if (Masters_AudioManager.Instance != null && introAudio != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(introAudio);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(1.5f);
        }

        yield return new WaitForSeconds(0.3f);
        StartLessonSequence();
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP != null) headerTMP.text = "WRITING BRANCH (Stone Tablet)";
        if (titleTMP != null) titleTMP.text = "W01 Write the Tag";
        if (subtitleTMP != null) subtitleTMP.text = "Type the correct tag for each statement.";
        if (hintTMP != null) hintTMP.text = "See-Saw Rule: [+] Statement -> [-] Tag | [-] Statement -> [+] Tag";
    }

    private void AutoBindReferences() {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (subtitleTMP == null && (n.Contains("subtitle") || n.Contains("instruction"))) subtitleTMP = tmp;
            else if (progressTMP == null && (n.Contains("progress") || n.Contains("counter"))) progressTMP = tmp;
            else if (scoreTMP == null && n.Contains("score")) scoreTMP = tmp;
            else if (hintTMP == null && (n.Contains("hint") || n.Contains("seesaw"))) hintTMP = tmp;
            else if (statementTextTMP == null && (n.Contains("statement") || n.Contains("prompttext"))) statementTextTMP = tmp;
            else if (tabletPromptTMP == null && n.Contains("prompt")) tabletPromptTMP = tmp;
            else if (hintTitleTMP == null && n.Contains("hinttitle")) hintTitleTMP = tmp;
            else if (hintAnswerTMP == null && n.Contains("hintanswer")) hintAnswerTMP = tmp;
            else if (hintRuleTMP == null && n.Contains("hintrule")) hintRuleTMP = tmp;
        }

        if (inputField == null) {
            inputField = GetComponentInChildren<TMP_InputField>(true);
            if (inputField != null) {
                inputFieldBg = inputField.GetComponent<Image>();
            }
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (submitBtn == null && (n.Contains("submit") || n.Contains("check") || n.Contains("enter"))) {
                submitBtn = btn;
            } else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("audio") || n.Contains("listen"))) {
                replayAudioBtn = btn;
            } else if (retryBtn == null && n.Contains("retry")) {
                retryBtn = btn;
            } else if (returnHubBtn == null && (n.Contains("hub") || n.Contains("complete") || n.Contains("done"))) {
                returnHubBtn = btn;
            } else if (nextButton == null && n == "nextbutton") {
                nextButton = btn;
            }
        }
    }

    public void PopulateDefaultQuestions() {
        questions = new QuestionTags_WritingW01Item[] {
            new QuestionTags_WritingW01Item {
                roundId = 1,
                statementText = "You are a student,",
                primaryAcceptedTag = "aren't you?",
                alternativeTags = new string[] { "aren't you", "arent you", "arent you?" },
                fullEchoSentence = "You are a student, aren't you?",
                hintText = "Positive statement [+] 'You are' takes negative tag [-] 'aren't you?'"
            },
            new QuestionTags_WritingW01Item {
                roundId = 2,
                statementText = "He is very busy,",
                primaryAcceptedTag = "isn't he?",
                alternativeTags = new string[] { "isn't he", "isnt he", "isnt he?" },
                fullEchoSentence = "He is very busy, isn't he?",
                hintText = "Positive statement [+] 'He is' takes negative tag [-] 'isn't he?'"
            },
            new QuestionTags_WritingW01Item {
                roundId = 3,
                statementText = "He was happy,",
                primaryAcceptedTag = "wasn't he?",
                alternativeTags = new string[] { "wasn't he", "wasnt he", "wasnt he?" },
                fullEchoSentence = "He was happy, wasn't he?",
                hintText = "Past tense [+] 'He was' takes negative tag [-] 'wasn't he?'"
            },
            new QuestionTags_WritingW01Item {
                roundId = 4,
                statementText = "You speak English,",
                primaryAcceptedTag = "don't you?",
                alternativeTags = new string[] { "don't you", "dont you", "dont you?" },
                fullEchoSentence = "You speak English, don't you?",
                hintText = "Present verb [+] 'speak' with subject 'you' takes 'don't you?'"
            },
            new QuestionTags_WritingW01Item {
                roundId = 5,
                statementText = "He studies French,",
                primaryAcceptedTag = "doesn't he?",
                alternativeTags = new string[] { "doesn't he", "doesnt he", "doesnt he?" },
                fullEchoSentence = "He studies French, doesn't he?",
                hintText = "Third person [+] 'studies' with subject 'he' takes 'doesn't he?'"
            },
            new QuestionTags_WritingW01Item {
                roundId = 6,
                statementText = "You will pass the exam,",
                primaryAcceptedTag = "won't you?",
                alternativeTags = new string[] { "won't you", "wont you", "wont you?" },
                fullEchoSentence = "You will pass the exam, won't you?",
                hintText = "Future modal [+] 'will' takes negative tag [-] 'won't you?'"
            },
            new QuestionTags_WritingW01Item {
                roundId = 7,
                statementText = "You must be patient,",
                primaryAcceptedTag = "mustn't you?",
                alternativeTags = new string[] { "mustn't you", "mustnt you", "mustnt you?" },
                fullEchoSentence = "You must be patient, mustn't you?",
                hintText = "Modal [+] 'must' takes negative tag [-] 'mustn't you?'"
            },
            new QuestionTags_WritingW01Item {
                roundId = 8,
                statementText = "He isn't crazy,",
                primaryAcceptedTag = "is he?",
                alternativeTags = new string[] { "is he", "is he?" },
                fullEchoSentence = "He isn't crazy, is he?",
                hintText = "Negative statement [-] 'isn't' takes positive tag [+] 'is he?'"
            },
            new QuestionTags_WritingW01Item {
                roundId = 9,
                statementText = "They weren't late,",
                primaryAcceptedTag = "were they?",
                alternativeTags = new string[] { "were they", "were they?" },
                fullEchoSentence = "They weren't late, were they?",
                hintText = "Negative past [-] 'weren't' takes positive tag [+] 'were they?'"
            },
            new QuestionTags_WritingW01Item {
                roundId = 10,
                statementText = "You don't speak French,",
                primaryAcceptedTag = "do you?",
                alternativeTags = new string[] { "do you", "do you?" },
                fullEchoSentence = "You don't speak French, do you?",
                hintText = "Negative present [-] 'don't' takes positive tag [+] 'do you?'"
            }
        };
    }

    public void StartLessonSequence() {
        currentQuestionIndex = 0;
        score = 0;
        isHandlingSubmission = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (hintBoxObject != null) hintBoxObject.SetActive(false);
        UpdateScoreDisplay();
        LoadQuestion(currentQuestionIndex);
    }

    public void LoadQuestion(int qIdx) {
        if (questions == null || questions.Length == 0) return;
        if (qIdx < 0 || qIdx >= questions.Length) {
            ShowResults();
            return;
        }

        currentQuestionIndex = qIdx;
        isHandlingSubmission = false;
        var q = questions[currentQuestionIndex];

        if (progressTMP != null) progressTMP.text = $"Question {qIdx + 1}/{questions.Length}";

        if (statementTextTMP != null) {
            statementTextTMP.text = $"\"{q.statementText} <color=#FFD700>_____ ?</color>\"";
            statementTextTMP.color = Color.white;
        }

        if (hintBoxObject != null) {
            hintBoxObject.SetActive(false);
        }

        if (inputField != null) {
            inputField.text = "";
            inputField.interactable = true;
            if (inputFieldBg != null) inputFieldBg.color = defaultInputColor;
            inputField.Select();
            inputField.ActivateInputField();
        }

        if (submitBtn != null) submitBtn.interactable = true;

        if (tabletCardObject != null) {
            tabletCardObject.transform.DOKill();
            tabletCardObject.transform.localScale = Vector3.one * 0.95f;
            tabletCardObject.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        PlayCurrentStatementAudio();
    }

    public void PlayCurrentStatementAudio() {
        if (questions == null || currentQuestionIndex >= questions.Length) return;
        var q = questions[currentQuestionIndex];

        if (q.statementAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(q.statementAudio);
        }
    }

    public void OnSubmitClicked() {
        if (isHandlingSubmission || currentQuestionIndex >= questions.Length || inputField == null) return;

        string rawInput = inputField.text.Trim();
        if (string.IsNullOrEmpty(rawInput)) return;

        isHandlingSubmission = true;
        var q = questions[currentQuestionIndex];

        bool isCorrect = ValidateTag(rawInput, q);

        if (inputField != null) inputField.interactable = false;
        if (submitBtn != null) submitBtn.interactable = false;

        if (isCorrect) {
            score++;
            UpdateScoreDisplay();

            if (inputFieldBg != null) inputFieldBg.color = correctColor;

            if (statementTextTMP != null) {
                statementTextTMP.text = $"<color=#80FF80>\"{q.fullEchoSentence}\"</color>";
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            StartCoroutine(PlayEchoAndAdvanceRoutine(q, 1.8f));
        } else {
            if (inputFieldBg != null) inputFieldBg.color = wrongColor;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (tabletCardObject != null) {
                tabletCardObject.transform.DOShakePosition(0.4f, 15f, 20);
            }

            // Show Hint Box with the Correct Answer & Rule
            ShowHintBox(q);

            if (statementTextTMP != null) {
                statementTextTMP.text = $"<color=#80FF80>\"{q.fullEchoSentence}\"</color>";
            }

            StartCoroutine(PlayEchoAndAdvanceRoutine(q, 3.2f));
        }
    }

    private void ShowHintBox(QuestionTags_WritingW01Item q) {
        if (hintBoxObject != null) {
            hintBoxObject.SetActive(true);
            hintBoxObject.transform.DOKill();
            hintBoxObject.transform.localScale = Vector3.zero;
            hintBoxObject.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (hintTitleTMP != null) {
            hintTitleTMP.text = "HINT & CORRECT ANSWER";
        }

        if (hintAnswerTMP != null) {
            hintAnswerTMP.text = $"Correct Tag: <color=#66FF88>\"{q.primaryAcceptedTag}\"</color>";
        }

        if (hintRuleTMP != null) {
            hintRuleTMP.text = q.hintText;
        }
    }

    private bool ValidateTag(string input, QuestionTags_WritingW01Item q) {
        string normalizedInput = NormalizeTag(input);
        string normalizedPrimary = NormalizeTag(q.primaryAcceptedTag);

        if (normalizedInput == normalizedPrimary) return true;

        if (q.alternativeTags != null) {
            foreach (var alt in q.alternativeTags) {
                if (normalizedInput == NormalizeTag(alt)) return true;
            }
        }

        return false;
    }

    private string NormalizeTag(string str) {
        if (string.IsNullOrEmpty(str)) return "";
        return str.Trim().ToLowerInvariant()
            .Replace("?", "")
            .Replace(".", "")
            .Replace("!", "")
            .Replace(",", "")
            .Replace("’", "'")
            .Replace("`", "'")
            .Replace(" ", "");
    }

    private IEnumerator PlayEchoAndAdvanceRoutine(QuestionTags_WritingW01Item q, float fallbackDelay) {
        yield return new WaitForSeconds(0.35f);

        if (q.fullEchoAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(q.fullEchoAudio);
            yield return new WaitForSeconds(q.fullEchoAudio.length + 0.6f);
        } else {
            yield return new WaitForSeconds(fallbackDelay);
        }

        AdvanceNext();
    }

    private void AdvanceNext() {
        if (currentQuestionIndex + 1 < questions.Length) {
            LoadQuestion(currentQuestionIndex + 1);
        } else {
            ShowResults();
        }
    }

    private void UpdateScoreDisplay() {
        if (scoreTMP != null) scoreTMP.text = $"Score: {score}/{questions.Length}";
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
            resultTitleTMP.text = passed ? "STONE TABLET WRITING MASTERED!" : "KEEP PRACTICING!";
            resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f) : new Color(1f, 0.65f, 0.2f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Your Score: {score} / {questions.Length}";
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed
                ? "You accurately typed the Question Tags onto the stone tablet!"
                : $"You need at least {passThreshold} correct tags to master this lesson. Tap Retry to try again!";
        }

        if (retryBtn != null) retryBtn.gameObject.SetActive(!passed);
        if (returnHubBtn != null) returnHubBtn.gameObject.SetActive(true);

        if (nextButton != null) {
            nextButton.gameObject.SetActive(passed);
            if (passed) NextButtonAnimation();
        }

        if (passed) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
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
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}
