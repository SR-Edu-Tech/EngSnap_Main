using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// R03 Spot the Wrong Echo Controller for Book 2B Unit 7 (Question Tags).
/// Displays a sentence with a question tag.
/// Student judges whether the tag is a RIGHT ECHO or a WRONG ECHO.
/// Success condition: Student correctly classifies at least 6 of 8 sentences.
/// Completing this lesson unlocks the Writing branch.
/// </summary>
public class Masters_QuestionTags_Reading_LessonThree : Masters_Lesson {

[System.Serializable]
public class QuestionTags_ReadingR03Question {
    public int roundId;
    public string sentenceText;          // e.g. "You aren't a teacher, aren't you?"
    public bool isRightEcho;             // True if correct, False if broken tag
    public string correctTag;             // e.g. "are you?"
    public string correctedSentenceText; // e.g. "You aren't a teacher, are you?"
    public string whatIsWrongExplanation;// e.g. "Polarity error: Negative statement ('aren't') needs positive tag ('are you?')"
    public AudioClip sentenceAudio;
    public AudioClip correctedEchoAudio;
}

    [Header("8 Question Tag Echo Questions")]
    [SerializeField]
    private QuestionTags_ReadingR03Question[] questions;

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

    [Header("Sentence Card")]
    [SerializeField]
    private GameObject sentenceCardObject;
    [SerializeField]
    private Image sentenceCardBg;
    [SerializeField]
    private TextMeshProUGUI sentencePromptTMP;
    [SerializeField]
    private TextMeshProUGUI sentenceTextTMP;
    [SerializeField]
    private TextMeshProUGUI explanationTMP;
    [SerializeField]
    private Button replayAudioBtn;

    [Header("Decision Buttons (Right Echo / Wrong Echo)")]
    [SerializeField]
    private GameObject decisionContainer;
    [SerializeField]
    private Button rightEchoBtn;
    [SerializeField]
    private Button wrongEchoBtn;
    [SerializeField]
    private Image rightEchoImg;
    [SerializeField]
    private Image wrongEchoImg;

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
    private int passThreshold = 6;

    private int currentQuestionIndex = 0;
    private int score = 0;
    private bool isHandlingAnswer = false;

    private readonly Color defaultDecisionRightColor = new Color(0.12f, 0.55f, 0.35f, 0.95f);
    private readonly Color defaultDecisionWrongColor = new Color(0.75f, 0.35f, 0.15f, 0.95f);
    private readonly Color correctColor = new Color(0.15f, 0.78f, 0.35f, 1f);
    private readonly Color wrongColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Reading;
        base.Awake();

        AutoBindReferences();

        if (rightEchoBtn != null) {
            rightEchoBtn.onClick.RemoveAllListeners();
            rightEchoBtn.onClick.AddListener(() => OnDecisionClicked(true));
        }

        if (wrongEchoBtn != null) {
            wrongEchoBtn.onClick.RemoveAllListeners();
            wrongEchoBtn.onClick.AddListener(() => OnDecisionClicked(false));
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(PlayCurrentSentenceAudio);
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
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        StartCoroutine(InitializeLessonRoutine());
    }

    private IEnumerator InitializeLessonRoutine() {
        SetInteractiveState(false);

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
        if (headerTMP != null) headerTMP.text = "READING BRANCH (Echo Spotting)";
        if (titleTMP != null) titleTMP.text = "R03 Spot the Wrong Echo";
        if (subtitleTMP != null) subtitleTMP.text = "Read the sentence, decide whether the echo is right or wrong, and tap your decision.";
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
            else if (sentenceTextTMP == null && (n.Contains("sentence") || n.Contains("statement"))) sentenceTextTMP = tmp;
            else if (sentencePromptTMP == null && n.Contains("prompt")) sentencePromptTMP = tmp;
            else if (explanationTMP == null && (n.Contains("explanation") || n.Contains("desc"))) explanationTMP = tmp;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("rightecho") || n == "rightbtn" || n == "btn_right") {
                rightEchoBtn = btn;
                rightEchoImg = btn.GetComponent<Image>();
            } else if (n.Contains("wrongecho") || n == "wrongbtn" || n == "btn_wrong") {
                wrongEchoBtn = btn;
                wrongEchoImg = btn.GetComponent<Image>();
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
        questions = new QuestionTags_ReadingR03Question[] {
            // Q1: Right
            new QuestionTags_ReadingR03Question {
                roundId = 1,
                sentenceText = "You are a student, aren't you?",
                isRightEcho = true,
                correctTag = "aren't you?",
                correctedSentenceText = "You are a student, aren't you?",
                whatIsWrongExplanation = "Correct! [+] Statement 'You are' takes [-] Tag 'aren't you?'"
            },
            // Q2: Wrong (Polarity)
            new QuestionTags_ReadingR03Question {
                roundId = 2,
                sentenceText = "You aren't a teacher, aren't you?",
                isRightEcho = false,
                correctTag = "are you?",
                correctedSentenceText = "You aren't a teacher, are you?",
                whatIsWrongExplanation = "Polarity error: [-] Statement 'aren't' needs [+] Tag 'are you?'"
            },
            // Q3: Wrong (Helping verb)
            new QuestionTags_ReadingR03Question {
                roundId = 3,
                sentenceText = "He is very busy, doesn't he?",
                isRightEcho = false,
                correctTag = "isn't he?",
                correctedSentenceText = "He is very busy, isn't he?",
                whatIsWrongExplanation = "Verb error: 'He is' uses verb 'is', so the tag must be 'isn't he?'"
            },
            // Q4: Right
            new QuestionTags_ReadingR03Question {
                roundId = 4,
                sentenceText = "They weren't late, were they?",
                isRightEcho = true,
                correctTag = "were they?",
                correctedSentenceText = "They weren't late, were they?",
                whatIsWrongExplanation = "Correct! [-] Statement 'weren't' takes [+] Tag 'were they?'"
            },
            // Q5: Wrong (Pronoun)
            new QuestionTags_ReadingR03Question {
                roundId = 5,
                sentenceText = "You speak English, don't they?",
                isRightEcho = false,
                correctTag = "don't you?",
                correctedSentenceText = "You speak English, don't you?",
                whatIsWrongExplanation = "Pronoun error: Subject is 'You', so the tag must be 'don't you?'"
            },
            // Q6: Right
            new QuestionTags_ReadingR03Question {
                roundId = 6,
                sentenceText = "You didn't study for the test, did you?",
                isRightEcho = true,
                correctTag = "did you?",
                correctedSentenceText = "You didn't study for the test, did you?",
                whatIsWrongExplanation = "Correct! [-] Statement 'didn't study' takes [+] Tag 'did you?'"
            },
            // Q7: Wrong (Polarity)
            new QuestionTags_ReadingR03Question {
                roundId = 7,
                sentenceText = "You will pass the exam, will you?",
                isRightEcho = false,
                correctTag = "won't you?",
                correctedSentenceText = "You will pass the exam, won't you?",
                whatIsWrongExplanation = "Polarity error: [+] Statement 'You will' needs [-] Tag 'won't you?'"
            },
            // Q8: Right
            new QuestionTags_ReadingR03Question {
                roundId = 8,
                sentenceText = "We mustn't say anything, must we?",
                isRightEcho = true,
                correctTag = "must we?",
                correctedSentenceText = "We mustn't say anything, must we?",
                whatIsWrongExplanation = "Correct! [-] Modal 'mustn't' takes [+] Tag 'must we?'"
            }
        };
    }

    public void StartLessonSequence() {
        currentQuestionIndex = 0;
        score = 0;
        isHandlingAnswer = false;

        if (resultPanel != null) resultPanel.SetActive(false);
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
        isHandlingAnswer = false;
        var q = questions[currentQuestionIndex];

        if (progressTMP != null) progressTMP.text = $"Round {qIdx + 1}/{questions.Length}";

        if (sentenceTextTMP != null) {
            sentenceTextTMP.text = $"\"{q.sentenceText}\"";
            sentenceTextTMP.color = Color.white;
        }

        if (explanationTMP != null) {
            explanationTMP.gameObject.SetActive(false);
        }

        if (sentenceCardObject != null) {
            sentenceCardObject.transform.DOKill();
            sentenceCardObject.transform.localScale = Vector3.one * 0.95f;
            sentenceCardObject.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (decisionContainer != null) {
            decisionContainer.SetActive(true);
            decisionContainer.transform.DOKill();
            decisionContainer.transform.localScale = Vector3.zero;
            decisionContainer.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (rightEchoBtn != null) rightEchoBtn.interactable = true;
        if (wrongEchoBtn != null) wrongEchoBtn.interactable = true;
        if (rightEchoImg != null) rightEchoImg.color = defaultDecisionRightColor;
        if (wrongEchoImg != null) wrongEchoImg.color = defaultDecisionWrongColor;

        PlayCurrentSentenceAudio();
    }

    public void PlayCurrentSentenceAudio() {
        if (questions == null || currentQuestionIndex >= questions.Length) return;
        var q = questions[currentQuestionIndex];

        if (q.sentenceAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(q.sentenceAudio);
        }
    }

    public void OnDecisionClicked(bool tappedRightEcho) {
        if (isHandlingAnswer || currentQuestionIndex >= questions.Length) return;
        isHandlingAnswer = true;

        var q = questions[currentQuestionIndex];
        bool isDecisionCorrect = (tappedRightEcho == q.isRightEcho);

        if (rightEchoBtn != null) rightEchoBtn.interactable = false;
        if (wrongEchoBtn != null) wrongEchoBtn.interactable = false;

        // Visual feedback on buttons
        if (tappedRightEcho) {
            if (rightEchoImg != null) rightEchoImg.color = isDecisionCorrect ? correctColor : wrongColor;
            if (!isDecisionCorrect && wrongEchoImg != null) wrongEchoImg.color = correctColor;
            rightEchoBtn.transform.DOPunchScale(Vector3.one * 0.1f, 0.25f);
        } else {
            if (wrongEchoImg != null) wrongEchoImg.color = isDecisionCorrect ? correctColor : wrongColor;
            if (!isDecisionCorrect && rightEchoImg != null) rightEchoImg.color = correctColor;
            wrongEchoBtn.transform.DOPunchScale(Vector3.one * 0.1f, 0.25f);
        }

        if (isDecisionCorrect) {
            score++;
            UpdateScoreDisplay();

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            ShowExplanation(q.whatIsWrongExplanation);

            if (!q.isRightEcho && sentenceTextTMP != null && !string.IsNullOrEmpty(q.correctedSentenceText)) {
                sentenceTextTMP.text = $"<color=#80FF80>\"{q.correctedSentenceText}\"</color>";
            }

            StartCoroutine(PlayAudioAndAdvanceRoutine(q, true));
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (sentenceCardObject != null) {
                sentenceCardObject.transform.DOShakePosition(0.4f, 15f, 20);
            }

            ShowExplanation(q.whatIsWrongExplanation);

            if (!q.isRightEcho && sentenceTextTMP != null && !string.IsNullOrEmpty(q.correctedSentenceText)) {
                sentenceTextTMP.text = $"<color=#80FF80>\"{q.correctedSentenceText}\"</color>";
            }

            StartCoroutine(PlayAudioAndAdvanceRoutine(q, false));
        }
    }

    private void ShowExplanation(string text) {
        if (explanationTMP != null && !string.IsNullOrEmpty(text)) {
            explanationTMP.text = text;
            explanationTMP.gameObject.SetActive(true);
            explanationTMP.transform.DOKill();
            explanationTMP.transform.localScale = Vector3.zero;
            explanationTMP.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }

    private IEnumerator PlayAudioAndAdvanceRoutine(QuestionTags_ReadingR03Question q, bool wasCorrect) {
        yield return new WaitForSeconds(0.3f);

        AudioClip clipToPlay = (!q.isRightEcho && q.correctedEchoAudio != null) ? q.correctedEchoAudio : q.sentenceAudio;

        if (clipToPlay != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            yield return new WaitForSeconds(clipToPlay.length + 0.6f);
        } else {
            yield return new WaitForSeconds(2.0f);
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

    private void SetInteractiveState(bool state) {
        if (rightEchoBtn != null) rightEchoBtn.interactable = state;
        if (wrongEchoBtn != null) wrongEchoBtn.interactable = state;
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
            resultTitleTMP.text = passed ? "ECHO SPOTTING MASTERED!" : "KEEP PRACTICING!";
            resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f) : new Color(1f, 0.65f, 0.2f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Your Score: {score} / {questions.Length}";
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed
                ? "You correctly spotted and verified all Question Tag echoes!"
                : $"You need at least {passThreshold} correct judgments to master this lesson. Tap Retry to try again!";
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
            // Unlock Writing Topic upon Reading 3 completion
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Writing);
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

        // Unlock Writing topic
        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Writing);
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
