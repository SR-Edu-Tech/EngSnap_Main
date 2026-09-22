using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Core Game Manager for Unit 6: Colour Your Speech - Q01 ("Gallery Wall Quiz").
/// Supports 7 distinct question formats: Match, MCQ, Group, Fill, Swap, Literal/Real, True/False.
/// Generates 12 randomized questions per attempt from a pool of 30+ candidates.
/// Single source of truth: each QuizQuestion object drives both displayed text and question audio.
/// Serialized startup audio flow: Intro plays once -> intro finishes -> First question loads & plays once.
/// Pass threshold: >= 9/12. Passing marks Q01 complete in M3A_U6_HubProgress and completes topic.
/// </summary>
public class Masters_ColourYourSpeech_Quiz_LessonOne : Masters_Lesson {

    public enum QuestionFormat {
        Match,
        MCQ,
        Group,
        Fill,
        Swap,
        LiteralReal,
        TrueFalse
    }

    [System.Serializable]
    public class QuizQuestion {
        public string questionId;
        public QuestionFormat format;
        public string idiomTag;
        public string familyTag;
        [TextArea(2, 4)] public string questionText;
        public AudioClip questionAudioClip;  // Direct, self-contained question voiceover
        public string[] options;
        public int correctOptionIndex;       // Used for single-choice / MCQ / Swap / Match / TF / Literal
        public int[] correctOptionIndices;   // Used for Group (multiple selection)
        public string fillCorrectAnswer;     // Used for Fill-in-the-blank
        public string[] fillAcceptableAnswers;
        [TextArea(1, 3)] public string explanation;
    }

    [Header("Header & Stage UI")]
    [SerializeField] private TextMeshProUGUI quizTitleTMP;
    [SerializeField] private TextMeshProUGUI questionCounterTMP;
    [SerializeField] private TextMeshProUGUI runningScoreTMP;
    [SerializeField] private Button backButton;

    [Header("Central Gallery & Painting")]
    [SerializeField] private GameObject galleryPaintingFrame;
    [SerializeField] private Image idiomPaintingImage;
    [SerializeField] private TextMeshProUGUI framedIdiomTMP;
    [SerializeField] private TextMeshProUGUI formatBadgeTMP;
    [SerializeField] private TextMeshProUGUI questionPromptTMP;

    [Header("Standard Options UI (MCQ / Match / Swap / LiteralReal / TrueFalse)")]
    [SerializeField] private GameObject standardOptionsContainer;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TextMeshProUGUI[] optionButtonTexts;
    [SerializeField] private Image[] optionBorders;

    [Header("Group Multi-Select UI")]
    [SerializeField] private GameObject groupOptionsContainer;
    [SerializeField] private Button[] groupOptionButtons;
    [SerializeField] private TextMeshProUGUI[] groupOptionTexts;
    [SerializeField] private Image[] groupOptionBorders;
    [SerializeField] private Button groupSubmitButton;

    [Header("Fill-In-The-Blank UI")]
    [SerializeField] private GameObject fillContainer;
    [SerializeField] private TMP_InputField fillInputField;
    [SerializeField] private Button fillSubmitButton;
    [SerializeField] private Image fillInputBorder;

    [Header("Feedback UI")]
    [SerializeField] private GameObject feedbackPanel;
    [SerializeField] private TextMeshProUGUI feedbackStatusTMP;
    [SerializeField] private TextMeshProUGUI feedbackExplanationTMP;

    [Header("Result / End Screen UI")]
    [SerializeField] private GameObject resultScreenPanel;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private TextMeshProUGUI resultFeedbackTMP;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button finishButton;

    [Header("Host Audio & SFX")]
    [SerializeField] private AudioClip ariaIntroClip;
    [SerializeField] private AudioClip ariaPassClip;
    [SerializeField] private AudioClip ariaFailClip;
    [SerializeField] private AudioClip sfxCorrect;
    [SerializeField] private AudioClip sfxWrong;
    [SerializeField] private AudioClip sfxResultPass;

    [Header("Candidate Question Pool (30+ Questions)")]
    [SerializeField] private List<QuizQuestion> questionPool = new List<QuizQuestion>();

    // Runtime state
    private List<QuizQuestion> activeQuestions = new List<QuizQuestion>();
    private int currentQuestionIndex = 0;
    private int currentScore = 0;
    private bool isAnsweringLocked = false;
    private HashSet<int> selectedGroupIndices = new HashSet<int>();
    private Coroutine startupCoroutine = null;
    private Coroutine advanceCoroutine = null;
    private bool startupAudioSequenceStarted = false;

    // Default UI Color Constants
    private static readonly Color ColorNormalBorder = new Color(0.36f, 0.49f, 0.73f, 1f);     // Soft Slate Blue #5C7CBA
    private static readonly Color ColorNormalBg = new Color(0.16f, 0.21f, 0.34f, 1f);         // Dark Slate #283556
    private static readonly Color ColorSelectedBorder = new Color(1f, 0.84f, 0.0f, 1f);       // Gold #FFD700
    private static readonly Color ColorSelectedBg = new Color(0.23f, 0.30f, 0.48f, 1f);       // Brighter Navy
    private static readonly Color ColorCorrectBorder = new Color(0.13f, 0.75f, 0.42f, 1f);     // Emerald Green #22C55E
    private static readonly Color ColorCorrectBg = new Color(0.08f, 0.35f, 0.20f, 1f);
    private static readonly Color ColorWrongBorder = new Color(0.94f, 0.27f, 0.27f, 1f);       // Coral Red #EF4444
    private static readonly Color ColorWrongBg = new Color(0.40f, 0.12f, 0.12f, 1f);

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Quiz;
        narratorSpeech = null; // Nullify base narrator speech to prevent unmanaged audio

        EnsureUIBindings();

        if (backButton != null) {
            var mb = backButton.GetComponent<Masters_BackButton>() ?? backButton.GetComponentInParent<Masters_BackButton>();
            if (mb == null) {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        if (retryButton != null) {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryButtonClicked);
        }

        if (finishButton != null) {
            finishButton.onClick.RemoveAllListeners();
            finishButton.onClick.AddListener(OnFinishButtonClicked);
        }

        if (fillSubmitButton != null) {
            fillSubmitButton.onClick.RemoveAllListeners();
            fillSubmitButton.onClick.AddListener(OnFillSubmitted);
        }

        if (fillInputField != null) {
            fillInputField.onSubmit.RemoveAllListeners();
            fillInputField.onSubmit.AddListener(_ => OnFillSubmitted());
        }

        if (groupSubmitButton != null) {
            groupSubmitButton.onClick.RemoveAllListeners();
            groupSubmitButton.onClick.AddListener(OnGroupSubmitted);
        }

        BindOptionButtons();
        InitializeQuestionPoolIfEmpty();
    }

    private void EnsureUIBindings() {
        var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var t in texts) {
            string n = t.gameObject.name.ToLower();
            if (quizTitleTMP == null && (n.Contains("title") || n.Contains("lessontitle"))) quizTitleTMP = t;
            else if (questionCounterTMP == null && (n.Contains("progress") || n.Contains("count") || n.Contains("quizcount"))) questionCounterTMP = t;
            else if (runningScoreTMP == null && n.Contains("score")) runningScoreTMP = t;
            else if (framedIdiomTMP == null && n.Contains("framedidiom")) framedIdiomTMP = t;
            else if (formatBadgeTMP == null && n.Contains("formatbadge")) formatBadgeTMP = t;
            else if (questionPromptTMP == null && (n.Contains("question") || n.Contains("prompt"))) questionPromptTMP = t;
        }

        if (optionButtons == null || optionButtons.Length == 0) {
            var btns = GetComponentsInChildren<Button>(true);
            List<Button> opts = new List<Button>();
            List<TextMeshProUGUI> optTexts = new List<TextMeshProUGUI>();
            List<Image> optBorders = new List<Image>();

            foreach (var b in btns) {
                string bn = b.gameObject.name.ToLower();
                if (bn.StartsWith("option_")) {
                    opts.Add(b);
                    var txt = b.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null) optTexts.Add(txt);
                    var border = b.transform.Find("Border")?.GetComponent<Image>() ?? b.GetComponent<Image>();
                    if (border != null) optBorders.Add(border);
                }
            }

            if (opts.Count > 0) {
                optionButtons = opts.ToArray();
                optionButtonTexts = optTexts.ToArray();
                optionBorders = optBorders.ToArray();
            }
        }
    }

    protected override void Start() {
        EnsureUIBindings();
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        narratorSpeech = null;

        if (!startupAudioSequenceStarted) {
            StartNewQuizAttempt();
        }
    }

    private void OnDisable() {
        if (startupCoroutine != null) {
            StopCoroutine(startupCoroutine);
            startupCoroutine = null;
        }
        if (advanceCoroutine != null) {
            StopCoroutine(advanceCoroutine);
            advanceCoroutine = null;
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
    }

    private void OnDestroy() {
        if (startupCoroutine != null) {
            StopCoroutine(startupCoroutine);
            startupCoroutine = null;
        }
        if (advanceCoroutine != null) {
            StopCoroutine(advanceCoroutine);
            advanceCoroutine = null;
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
    }

    private void BindOptionButtons() {
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                int idx = i;
                if (optionButtons[i] != null) {
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionButtonClicked(idx));
                }
            }
        }

        if (groupOptionButtons != null) {
            for (int i = 0; i < groupOptionButtons.Length; i++) {
                int idx = i;
                if (groupOptionButtons[i] != null) {
                    groupOptionButtons[i].onClick.RemoveAllListeners();
                    groupOptionButtons[i].onClick.AddListener(() => OnGroupOptionClicked(idx));
                }
            }
        }
    }

    public void StartNewQuizAttempt() {
        if (startupCoroutine != null) {
            StopCoroutine(startupCoroutine);
            startupCoroutine = null;
        }

        if (advanceCoroutine != null) {
            StopCoroutine(advanceCoroutine);
            advanceCoroutine = null;
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        currentQuestionIndex = 0;
        currentScore = 0;
        isAnsweringLocked = true;
        selectedGroupIndices.Clear();

        if (resultScreenPanel != null) resultScreenPanel.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);

        BuildActiveQuestionSet();

        // Launch serialized startup audio sequence (Intro -> Wait for finish -> First question)
        startupCoroutine = StartCoroutine(StartupAudioSequenceRoutine());
    }

    private IEnumerator StartupAudioSequenceRoutine() {
        startupAudioSequenceStarted = true;

        // STEP 2: Stop any currently playing voice-over before starting Q01 startup audio
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        // Welcome host stage displayed on Gallery Wall during intro
        if (quizTitleTMP != null) quizTitleTMP.text = "GALLERY WALL QUIZ";
        if (questionCounterTMP != null) questionCounterTMP.text = "Welcome!";
        if (runningScoreTMP != null) runningScoreTMP.text = "Score: 0/12";
        if (framedIdiomTMP != null) framedIdiomTMP.text = "COLOUR YOUR SPEECH";
        if (formatBadgeTMP != null) formatBadgeTMP.text = "[WELCOME]";
        if (questionPromptTMP != null) questionPromptTMP.text = "Welcome to the Gallery Wall Quiz! Show how well you know your idioms. Get at least 9 out of 12 right to complete the unit!";

        if (standardOptionsContainer != null) standardOptionsContainer.SetActive(false);
        if (groupOptionsContainer != null) groupOptionsContainer.SetActive(false);
        if (fillContainer != null) fillContainer.SetActive(false);
        if (feedbackPanel != null) feedbackPanel.SetActive(false);

        // STEP 3: Play the intended Q01 introductory audio ONCE
        if (ariaIntroClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroClip);
            // STEP 4: WAIT until the introductory audio has completely finished
            yield return new WaitForSeconds(ariaIntroClip.length + 0.25f);
        } else {
            yield return new WaitForSeconds(0.2f);
        }

        startupCoroutine = null;

        // STEP 5: Only after the intro has finished, load/display the first question
        // STEP 6: Play the FIRST QUESTION'S explicitly bound questionAudioClip ONCE
        LoadQuestion(0);
    }

    private void BuildActiveQuestionSet() {
        activeQuestions.Clear();

        List<QuizQuestion> candidates = new List<QuizQuestion>(questionPool);

        // Separate canonical 12 vs extra
        List<QuizQuestion> canonical12 = new List<QuizQuestion>();
        List<QuizQuestion> extras = new List<QuizQuestion>();

        foreach (var q in candidates) {
            if (q.questionId != null && (q.questionId.StartsWith("Q0") || q.questionId.StartsWith("Q10") || q.questionId.StartsWith("Q11") || q.questionId.StartsWith("Q12"))) {
                canonical12.Add(q);
            } else {
                extras.Add(q);
            }
        }

        // Shuffle canonical set
        if (canonical12.Count >= 12) {
            for (int i = 0; i < canonical12.Count; i++) {
                int r = UnityEngine.Random.Range(i, canonical12.Count);
                var temp = canonical12[i];
                canonical12[i] = canonical12[r];
                canonical12[r] = temp;
            }
            for (int i = 0; i < 12; i++) {
                activeQuestions.Add(canonical12[i]);
            }
        } else {
            for (int i = 0; i < candidates.Count; i++) {
                int r = UnityEngine.Random.Range(i, candidates.Count);
                var temp = candidates[i];
                candidates[i] = candidates[r];
                candidates[r] = temp;
            }
            int count = Mathf.Min(12, candidates.Count);
            for (int i = 0; i < count; i++) {
                activeQuestions.Add(candidates[i]);
            }
        }
    }

    private void LoadQuestion(int index) {
        if (advanceCoroutine != null) {
            StopCoroutine(advanceCoroutine);
            advanceCoroutine = null;
        }

        // Stop any currently playing audio immediately to prevent overlap
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (index >= activeQuestions.Count || index >= 12) {
            ShowResultScreen();
            return;
        }

        currentQuestionIndex = index;
        isAnsweringLocked = false;
        selectedGroupIndices.Clear();

        // Single authoritative source of truth for all visuals and audio
        QuizQuestion currentQuestion = activeQuestions[index];

        // 1. Header & Counter
        if (quizTitleTMP != null) quizTitleTMP.text = "GALLERY WALL QUIZ";
        if (questionCounterTMP != null) questionCounterTMP.text = $"Question {index + 1}/12";
        if (runningScoreTMP != null) runningScoreTMP.text = $"Score: {currentScore}/12";

        // 2. Painting Frame & Prompt
        if (framedIdiomTMP != null) framedIdiomTMP.text = !string.IsNullOrEmpty(currentQuestion.idiomTag) ? currentQuestion.idiomTag.ToUpper() : "COLOUR YOUR SPEECH";
        if (formatBadgeTMP != null) formatBadgeTMP.text = $"[{currentQuestion.format.ToString().ToUpper()}]";
        if (questionPromptTMP != null) questionPromptTMP.text = currentQuestion.questionText;

        // 3. Hide Feedback
        if (feedbackPanel != null) feedbackPanel.SetActive(false);

        // Reset Containers
        if (standardOptionsContainer != null) standardOptionsContainer.SetActive(false);
        if (groupOptionsContainer != null) groupOptionsContainer.SetActive(false);
        if (fillContainer != null) fillContainer.SetActive(false);

        // 4. Configure Answer Container by Format
        if (currentQuestion.format == QuestionFormat.Fill) {
            if (fillContainer != null) {
                fillContainer.SetActive(true);
                if (fillInputField != null) {
                    fillInputField.text = "";
                    fillInputField.interactable = true;
                    fillInputField.ActivateInputField();
                }
                if (fillInputBorder != null) fillInputBorder.color = ColorNormalBorder;
            }
        } else if (currentQuestion.format == QuestionFormat.Group) {
            GameObject targetContainer = groupOptionsContainer != null ? groupOptionsContainer : standardOptionsContainer;
            Button[] targetButtons = groupOptionButtons != null && groupOptionButtons.Length > 0 ? groupOptionButtons : optionButtons;
            TextMeshProUGUI[] targetTexts = groupOptionTexts != null && groupOptionTexts.Length > 0 ? groupOptionTexts : optionButtonTexts;
            Image[] targetBorders = groupOptionBorders != null && groupOptionBorders.Length > 0 ? groupOptionBorders : optionBorders;

            if (targetContainer != null) targetContainer.SetActive(true);
            if (groupSubmitButton != null) {
                groupSubmitButton.gameObject.SetActive(true);
                groupSubmitButton.interactable = false;
            }

            if (targetButtons != null && currentQuestion.options != null) {
                for (int i = 0; i < targetButtons.Length; i++) {
                    if (i < currentQuestion.options.Length) {
                        targetButtons[i].gameObject.SetActive(true);
                        targetButtons[i].interactable = true;
                        if (targetTexts != null && i < targetTexts.Length && targetTexts[i] != null) {
                            targetTexts[i].text = currentQuestion.options[i];
                        }
                        ResetButtonVisual(targetButtons[i], targetBorders != null && i < targetBorders.Length ? targetBorders[i] : null);
                    } else {
                        targetButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        } else {
            if (standardOptionsContainer != null) standardOptionsContainer.SetActive(true);

            if (optionButtons != null && currentQuestion.options != null) {
                for (int i = 0; i < optionButtons.Length; i++) {
                    if (i < currentQuestion.options.Length) {
                        optionButtons[i].gameObject.SetActive(true);
                        optionButtons[i].interactable = true;
                        if (optionButtonTexts != null && i < optionButtonTexts.Length && optionButtonTexts[i] != null) {
                            optionButtonTexts[i].text = currentQuestion.options[i];
                        }
                        ResetButtonVisual(optionButtons[i], optionBorders != null && i < optionBorders.Length ? optionBorders[i] : null);
                    } else {
                        optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        // 5. Play Question Audio DIRECTLY from the current question data object
        if (currentQuestion.questionAudioClip != null && Masters_AudioManager.Instance != null) {
            Debug.Log($"[Q01 AUDIO] Question ID: {currentQuestion.questionId} | Format: {currentQuestion.format} | Displayed Text: \"{currentQuestion.questionText}\" | Question VO: {currentQuestion.questionAudioClip.name}");
            Masters_AudioManager.Instance.PlayVoiceOver(currentQuestion.questionAudioClip);
        }
    }

    private void ResetButtonVisual(Button btn, Image border) {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = ColorNormalBg;
        if (border != null) border.color = ColorNormalBorder;
        btn.transform.localScale = Vector3.one;
    }

    private void OnOptionButtonClicked(int optionIdx) {
        if (isAnsweringLocked || currentQuestionIndex >= activeQuestions.Count) return;

        QuizQuestion q = activeQuestions[currentQuestionIndex];

        if (q.format == QuestionFormat.Group) {
            OnGroupOptionClicked(optionIdx);
            return;
        }

        isAnsweringLocked = true;
        bool isCorrect = (optionIdx == q.correctOptionIndex);

        if (optionButtons != null && optionIdx < optionButtons.Length) {
            var btn = optionButtons[optionIdx];
            var border = (optionBorders != null && optionIdx < optionBorders.Length) ? optionBorders[optionIdx] : null;
            var img = btn.GetComponent<Image>();

            if (isCorrect) {
                if (img != null) img.color = ColorCorrectBg;
                if (border != null) border.color = ColorCorrectBorder;
            } else {
                if (img != null) img.color = ColorWrongBg;
                if (border != null) border.color = ColorWrongBorder;

                if (q.correctOptionIndex >= 0 && q.correctOptionIndex < optionButtons.Length) {
                    var correctBtn = optionButtons[q.correctOptionIndex];
                    var correctBorder = (optionBorders != null && q.correctOptionIndex < optionBorders.Length) ? optionBorders[q.correctOptionIndex] : null;
                    var cImg = correctBtn.GetComponent<Image>();
                    if (cImg != null) cImg.color = ColorCorrectBg;
                    if (correctBorder != null) correctBorder.color = ColorCorrectBorder;
                }
            }
        }

        HandleAnswerEvaluated(isCorrect, q);
    }

    private void OnGroupOptionClicked(int optionIdx) {
        if (isAnsweringLocked || currentQuestionIndex >= activeQuestions.Count) return;

        QuizQuestion q = activeQuestions[currentQuestionIndex];

        Button[] targetButtons = groupOptionButtons != null && groupOptionButtons.Length > 0 ? groupOptionButtons : optionButtons;
        Image[] targetBorders = groupOptionBorders != null && groupOptionBorders.Length > 0 ? groupOptionBorders : optionBorders;

        if (selectedGroupIndices.Contains(optionIdx)) {
            selectedGroupIndices.Remove(optionIdx);
            if (targetButtons != null && optionIdx < targetButtons.Length) {
                var btn = targetButtons[optionIdx];
                var border = (targetBorders != null && optionIdx < targetBorders.Length) ? targetBorders[optionIdx] : null;
                ResetButtonVisual(btn, border);
            }
        } else {
            selectedGroupIndices.Add(optionIdx);
            if (targetButtons != null && optionIdx < targetButtons.Length) {
                var btn = targetButtons[optionIdx];
                var border = (targetBorders != null && optionIdx < targetBorders.Length) ? targetBorders[optionIdx] : null;
                var img = btn.GetComponent<Image>();
                if (img != null) img.color = ColorSelectedBg;
                if (border != null) border.color = ColorSelectedBorder;
            }
        }

        int requiredCount = (q.correctOptionIndices != null && q.correctOptionIndices.Length > 0) ? q.correctOptionIndices.Length : 2;

        if (groupSubmitButton != null) {
            groupSubmitButton.interactable = (selectedGroupIndices.Count == requiredCount);
        } else if (selectedGroupIndices.Count >= requiredCount) {
            OnGroupSubmitted();
        }
    }

    private void OnGroupSubmitted() {
        if (isAnsweringLocked || currentQuestionIndex >= activeQuestions.Count) return;

        QuizQuestion q = activeQuestions[currentQuestionIndex];
        isAnsweringLocked = true;

        bool isCorrect = EvaluateGroupAnswer(q);

        Button[] targetButtons = groupOptionButtons != null && groupOptionButtons.Length > 0 ? groupOptionButtons : optionButtons;
        Image[] targetBorders = groupOptionBorders != null && groupOptionBorders.Length > 0 ? groupOptionBorders : optionBorders;

        if (targetButtons != null) {
            for (int i = 0; i < targetButtons.Length; i++) {
                if (i < (q.options != null ? q.options.Length : 0)) {
                    bool shouldBeSelected = false;
                    if (q.correctOptionIndices != null) {
                        foreach (int idx in q.correctOptionIndices) {
                            if (idx == i) { shouldBeSelected = true; break; }
                        }
                    }

                    var img = targetButtons[i].GetComponent<Image>();
                    var border = (targetBorders != null && i < targetBorders.Length) ? targetBorders[i] : null;

                    if (shouldBeSelected) {
                        if (img != null) img.color = ColorCorrectBg;
                        if (border != null) border.color = ColorCorrectBorder;
                    } else if (selectedGroupIndices.Contains(i)) {
                        if (img != null) img.color = ColorWrongBg;
                        if (border != null) border.color = ColorWrongBorder;
                    }
                }
            }
        }

        HandleAnswerEvaluated(isCorrect, q);
    }

    private bool EvaluateGroupAnswer(QuizQuestion q) {
        if (q.correctOptionIndices == null || q.correctOptionIndices.Length == 0) return false;
        if (selectedGroupIndices.Count != q.correctOptionIndices.Length) return false;

        foreach (int req in q.correctOptionIndices) {
            if (!selectedGroupIndices.Contains(req)) return false;
        }
        return true;
    }

    private void OnFillSubmitted() {
        if (isAnsweringLocked || currentQuestionIndex >= activeQuestions.Count) return;

        QuizQuestion q = activeQuestions[currentQuestionIndex];
        string userText = fillInputField != null ? fillInputField.text.Trim().ToLowerInvariant() : "";

        bool isCorrect = false;
        if (!string.IsNullOrEmpty(q.fillCorrectAnswer) && userText.Contains(q.fillCorrectAnswer.ToLowerInvariant())) {
            isCorrect = true;
        } else if (q.fillAcceptableAnswers != null) {
            foreach (var acc in q.fillAcceptableAnswers) {
                if (!string.IsNullOrEmpty(acc) && userText.Contains(acc.ToLowerInvariant())) {
                    isCorrect = true;
                    break;
                }
            }
        }

        if (fillInputBorder != null) {
            fillInputBorder.color = isCorrect ? ColorCorrectBorder : ColorWrongBorder;
        }

        isAnsweringLocked = true;
        HandleAnswerEvaluated(isCorrect, q);
    }

    private void HandleAnswerEvaluated(bool isCorrect, QuizQuestion q) {
        // Stop question VO if it was still playing so feedback SFX is crisp
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (isCorrect) {
            currentScore++;
            PlaySound(Masters_SFX.Correct);
            if (sfxCorrect != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(sfxCorrect);
            }
        } else {
            PlaySound(Masters_SFX.Incorrect);
            if (sfxWrong != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(sfxWrong);
            }
        }

        if (runningScoreTMP != null) runningScoreTMP.text = $"Score: {currentScore}/12";

        if (feedbackPanel != null) {
            feedbackPanel.SetActive(true);
            if (feedbackStatusTMP != null) {
                feedbackStatusTMP.text = isCorrect ? "CORRECT! GREAT JOB!" : "NOT QUITE!";
                feedbackStatusTMP.color = isCorrect ? ColorCorrectBorder : ColorWrongBorder;
            }
            if (feedbackExplanationTMP != null) {
                feedbackExplanationTMP.text = q.explanation;
            }
        }

        LockControls();
        advanceCoroutine = StartCoroutine(AdvanceAfterDelay(1.8f));
    }

    private void LockControls() {
        if (optionButtons != null) {
            foreach (var btn in optionButtons) {
                if (btn != null) btn.interactable = false;
            }
        }
        if (groupOptionButtons != null) {
            foreach (var btn in groupOptionButtons) {
                if (btn != null) btn.interactable = false;
            }
        }
        if (groupSubmitButton != null) groupSubmitButton.interactable = false;
        if (fillInputField != null) fillInputField.interactable = false;
        if (fillSubmitButton != null) fillSubmitButton.interactable = false;
    }

    private IEnumerator AdvanceAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        LoadQuestion(currentQuestionIndex + 1);
    }

    private void ShowResultScreen() {
        if (startupCoroutine != null) {
            StopCoroutine(startupCoroutine);
            startupCoroutine = null;
        }
        if (advanceCoroutine != null) {
            StopCoroutine(advanceCoroutine);
            advanceCoroutine = null;
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (resultScreenPanel != null) resultScreenPanel.SetActive(true);

        bool isPass = (currentScore >= 9);

        if (isPass) {
            M3A_U6_HubProgress.MarkQ01Complete();
            if (sfxResultPass != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(sfxResultPass);
            } else {
                PlaySound(Masters_SFX.Correct);
            }

            if (ariaPassClip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(ariaPassClip);
            }

            if (resultTitleTMP != null) resultTitleTMP.text = "GALLERY WALL QUIZ PASSED!";
            if (resultScoreTMP != null) resultScoreTMP.text = $"Your Score: {currentScore}/12 (Pass Mark: 9/12)";
            if (resultFeedbackTMP != null) resultFeedbackTMP.text = "Incredible work! You've mastered all the idioms in Unit 6.";

            if (retryButton != null) retryButton.gameObject.SetActive(false);
            if (finishButton != null) finishButton.gameObject.SetActive(true);
        } else {
            if (ariaFailClip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(ariaFailClip);
            } else {
                PlaySound(Masters_SFX.Incorrect);
            }

            if (resultTitleTMP != null) resultTitleTMP.text = "ALMOST THERE!";
            if (resultScoreTMP != null) resultScoreTMP.text = $"Your Score: {currentScore}/12 (Pass Mark: 9/12)";
            if (resultFeedbackTMP != null) resultFeedbackTMP.text = "You need at least 9/12 to pass. Let's give it another shot!";

            if (retryButton != null) retryButton.gameObject.SetActive(true);
            if (finishButton != null) finishButton.gameObject.SetActive(false);
        }
    }

    private void OnRetryButtonClicked() {
        startupAudioSequenceStarted = false;
        StartNewQuizAttempt();
    }

    private void OnFinishButtonClicked() {
        if (startupCoroutine != null) {
            StopCoroutine(startupCoroutine);
            startupCoroutine = null;
        }
        if (advanceCoroutine != null) {
            StopCoroutine(advanceCoroutine);
            advanceCoroutine = null;
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (currentScore >= 9) {
            M3A_U6_HubProgress.MarkQ01Complete();
        }

        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    protected override void OnNextButtonClicked() {
        OnFinishButtonClicked();
    }

    private void OnBackButtonClicked() {
        if (startupCoroutine != null) {
            StopCoroutine(startupCoroutine);
            startupCoroutine = null;
        }
        if (advanceCoroutine != null) {
            StopCoroutine(advanceCoroutine);
            advanceCoroutine = null;
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnBackButtonClicked();
        }
    }

    private void PlaySound(Masters_SFX sfx) {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(sfx);
        }
    }

    private void InitializeQuestionPoolIfEmpty() {
        if (questionPool != null && questionPool.Count >= 12) return;

        questionPool = new List<QuizQuestion> {
            // Q1 — Match: 'cloud nine' -> In a state of happiness or extreme excitement
            new QuizQuestion {
                questionId = "Q01_Match_CloudNine",
                format = QuestionFormat.Match,
                idiomTag = "Cloud nine",
                familyTag = "Happiness",
                questionText = "Match 'cloud nine' with its correct meaning:",
                options = new[] { "In a state of happiness or extreme excitement", "Very easy", "Becoming crazy", "Really strange" },
                correctOptionIndex = 0,
                explanation = "'Cloud nine' means being in a state of happiness or extreme excitement."
            },
            // Q2 — Match: 'Fruitcake' -> Really strange or crazy
            new QuizQuestion {
                questionId = "Q02_Match_Fruitcake",
                format = QuestionFormat.Match,
                idiomTag = "Fruitcake",
                familyTag = "Crazy",
                questionText = "Match 'Fruitcake' with its correct meaning:",
                options = new[] { "Kind and sweet", "Really strange or crazy", "Very easy", "Saying something as a joke" },
                correctOptionIndex = 1,
                explanation = "A 'fruitcake' is someone who is really strange or crazy."
            },
            // Q3 — MCQ: 'The exam paper was a piece of cake.' -> Very easy
            new QuizQuestion {
                questionId = "Q03_MCQ_PieceOfCake",
                format = QuestionFormat.MCQ,
                idiomTag = "A piece of cake",
                familyTag = "Easy",
                questionText = "'The exam paper was a piece of cake.' What does this mean?",
                options = new[] { "The exam was very hard", "Very easy", "The exam was delicious", "The exam was postponed" },
                correctOptionIndex = 1,
                explanation = "'A piece of cake' means something is very easy."
            },
            // Q4 — Group: Which two also mean 'very easy'? -> Easy as pie, Giving candy to a baby
            new QuizQuestion {
                questionId = "Q04_Group_VeryEasy",
                format = QuestionFormat.Group,
                idiomTag = "Easy idioms",
                familyTag = "Easy",
                questionText = "Which two also mean 'very easy'?",
                options = new[] { "Easy as pie", "Going bananas", "Giving candy to a baby", "Nutty as a fruitcake" },
                correctOptionIndices = new[] { 0, 2 },
                explanation = "'Easy as pie' and 'Giving candy to a baby' both mean very easy."
            },
            // Q5 — Fill: He was on ______ when he heard the good news. -> cloud nine
            new QuizQuestion {
                questionId = "Q05_Fill_CloudNine",
                format = QuestionFormat.Fill,
                idiomTag = "Cloud nine",
                familyTag = "Happiness",
                questionText = "He was on ______ when he heard the good news.",
                fillCorrectAnswer = "cloud nine",
                fillAcceptableAnswers = new[] { "cloud nine", "cloud 9", "nine" },
                explanation = "The idiom is 'on cloud nine' (extreme happiness)."
            },
            // Q6 — Fill: If I'm late, my dad will go ______. -> bananas
            new QuizQuestion {
                questionId = "Q06_Fill_GoBananas",
                format = QuestionFormat.Fill,
                idiomTag = "Going bananas",
                familyTag = "Crazy",
                questionText = "If I'm late, my dad will go ______.",
                fillCorrectAnswer = "bananas",
                fillAcceptableAnswers = new[] { "bananas", "banana" },
                explanation = "The idiom is 'go bananas' (get very angry/crazy)."
            },
            // Q7 — MCQ: 'Sugar and spice' describes someone who is -> kind and sweet
            new QuizQuestion {
                questionId = "Q07_MCQ_SugarAndSpice",
                format = QuestionFormat.MCQ,
                idiomTag = "Sugar and spice",
                familyTag = "Kindness",
                questionText = "'Sugar and spice' describes someone who is",
                options = new[] { "kind and sweet", "Angry and mean", "Crazy and wild", "Strict and bossy" },
                correctOptionIndex = 0,
                explanation = "'Sugar and spice' describes someone who is kind and sweet."
            },
            // Q8 — Swap: Instead of 'on cloud nine' you can say -> over the moon / on seventh heaven
            new QuizQuestion {
                questionId = "Q08_Swap_CloudNine",
                format = QuestionFormat.Swap,
                idiomTag = "Cloud nine",
                familyTag = "Happiness",
                questionText = "Instead of 'on cloud nine' you can say",
                options = new[] { "over the moon", "going bananas", "a piece of cake", "my way or the highway" },
                correctOptionIndex = 0,
                explanation = "'Over the moon' and 'on seventh heaven' are synonyms for 'on cloud nine'."
            },
            // Q9 — Swap: Instead of 'tongue-in-cheek' you can say -> cheeky / playful
            new QuizQuestion {
                questionId = "Q09_Swap_TongueInCheek",
                format = QuestionFormat.Swap,
                idiomTag = "Tongue-in-cheek",
                familyTag = "Humor",
                questionText = "Instead of 'tongue-in-cheek' you can say",
                options = new[] { "serious and strict", "cheeky", "angry and rude", "sad and upset" },
                correctOptionIndex = 1,
                explanation = "'Cheeky' or 'playful' can replace 'tongue-in-cheek'."
            },
            // Q10 — Literal/Real: 'Mom will go bananas' — does she want fruit? -> No, she will get very cross
            new QuizQuestion {
                questionId = "Q10_LiteralReal_Bananas",
                format = QuestionFormat.LiteralReal,
                idiomTag = "Going bananas",
                familyTag = "Literal/Real",
                questionText = "'Mom will go bananas' — does she want fruit?",
                options = new[] { "Yes, she loves bananas", "No, she will get very cross", "Yes, she is making a fruit salad", "No, she wants apples" },
                correctOptionIndex = 1,
                explanation = "'Go bananas' is an idiom meaning to get very cross or angry, not literal fruit!"
            },
            // Q11 — MCQ: 'My way or the highway' means -> You have to listen to me
            new QuizQuestion {
                questionId = "Q11_MCQ_MyWay",
                format = QuestionFormat.MCQ,
                idiomTag = "My way or the highway",
                familyTag = "Strict",
                questionText = "'My way or the highway' means",
                options = new[] { "We should go on a road trip", "You have to listen to me", "Take whatever path you like", "Drive carefully" },
                correctOptionIndex = 1,
                explanation = "'My way or the highway' means you have to listen to me without argument."
            },
            // Q12 — True/False: 'tongue-in-cheek' means the person is being serious. -> False
            new QuizQuestion {
                questionId = "Q12_TF_TongueInCheek",
                format = QuestionFormat.TrueFalse,
                idiomTag = "Tongue-in-cheek",
                familyTag = "Humor",
                questionText = "True or False: 'tongue-in-cheek' means the person is being serious.",
                options = new[] { "True", "False" },
                correctOptionIndex = 1,
                explanation = "False! 'Tongue-in-cheek' means someone is speaking playfully or jokingly, not seriously."
            },
            // Pool expansion to 30+ candidate questions
            new QuizQuestion {
                questionId = "Q13_MCQ_Nutty",
                format = QuestionFormat.MCQ,
                idiomTag = "Nutty as a fruitcake",
                familyTag = "Crazy",
                questionText = "'He is nutty as a fruitcake.' What does this mean?",
                options = new[] { "He is eating nuts", "Really strange or crazy", "He is very sleepy", "He is very smart" },
                correctOptionIndex = 1,
                explanation = "'Nutty as a fruitcake' means really strange or crazy."
            },
            new QuizQuestion {
                questionId = "Q14_Fill_EasyAsPie",
                format = QuestionFormat.Fill,
                idiomTag = "Easy as pie",
                familyTag = "Easy",
                questionText = "Fill in the blank: Learning this rule was as easy as ______.",
                fillCorrectAnswer = "pie",
                fillAcceptableAnswers = new[] { "pie" },
                explanation = "The idiom is 'easy as pie'."
            },
            new QuizQuestion {
                questionId = "Q15_Swap_EasyAsPie",
                format = QuestionFormat.Swap,
                idiomTag = "Easy as pie",
                familyTag = "Easy",
                questionText = "Which idiom can replace 'as easy as pie'?",
                options = new[] { "A piece of cake", "Going bananas", "Tongue-in-cheek", "Sugar and spice" },
                correctOptionIndex = 0,
                explanation = "'A piece of cake' and 'Easy as pie' both mean very easy."
            },
            new QuizQuestion {
                questionId = "Q16_TF_CloudNine",
                format = QuestionFormat.TrueFalse,
                idiomTag = "Cloud nine",
                familyTag = "Happiness",
                questionText = "True or False: 'On cloud nine' means you are feeling very sad.",
                options = new[] { "True", "False" },
                correctOptionIndex = 1,
                explanation = "False! 'On cloud nine' means you are in a state of happiness or extreme excitement."
            },
            new QuizQuestion {
                questionId = "Q17_MCQ_GivingCandy",
                format = QuestionFormat.MCQ,
                idiomTag = "Giving candy to a baby",
                familyTag = "Easy",
                questionText = "'Winning that game was like giving candy to a baby.' What does this mean?",
                options = new[] { "Very easy", "It was very hard", "It was expensive", "It took all day" },
                correctOptionIndex = 0,
                explanation = "'Giving candy to a baby' means something is very easy."
            },
            new QuizQuestion {
                questionId = "Q18_Fill_PieceOfCake",
                format = QuestionFormat.Fill,
                idiomTag = "A piece of cake",
                familyTag = "Easy",
                questionText = "Fill in the blank: Don't worry about the test, it's a piece of ______.",
                fillCorrectAnswer = "cake",
                fillAcceptableAnswers = new[] { "cake" },
                explanation = "The idiom is 'a piece of cake'."
            },
            new QuizQuestion {
                questionId = "Q19_LiteralReal_PieceOfCake",
                format = QuestionFormat.LiteralReal,
                idiomTag = "A piece of cake",
                familyTag = "Literal/Real",
                questionText = "'This puzzle is a piece of cake' — is there actual cake to eat?",
                options = new[] { "Yes, chocolate cake", "No, it just means the puzzle is very easy", "Yes, a slice of birthday cake", "No, it means the puzzle is spicy" },
                correctOptionIndex = 1,
                explanation = "'A piece of cake' is an idiom meaning very easy, not literal food!"
            },
            new QuizQuestion {
                questionId = "Q20_TF_Fruitcake",
                format = QuestionFormat.TrueFalse,
                idiomTag = "Fruitcake",
                familyTag = "Crazy",
                questionText = "True or False: Calling someone a 'fruitcake' is an idiom for someone who is really strange or crazy.",
                options = new[] { "True", "False" },
                correctOptionIndex = 0,
                explanation = "True! 'Fruitcake' refers to someone who is really strange or crazy."
            },
            new QuizQuestion {
                questionId = "Q21_Match_Highway",
                format = QuestionFormat.Match,
                idiomTag = "My way or the highway",
                familyTag = "Strict",
                questionText = "Match 'My way or the highway' with its correct meaning:",
                options = new[] { "You have to listen to me", "Let's drive on the fast road", "Take your time deciding", "Everyone gets a vote" },
                correctOptionIndex = 0,
                explanation = "'My way or the highway' means you have to listen to me without argument."
            },
            new QuizQuestion {
                questionId = "Q22_Swap_Nutty",
                format = QuestionFormat.Swap,
                idiomTag = "Nutty as a fruitcake",
                familyTag = "Crazy",
                questionText = "Instead of 'nutty as a fruitcake', what idiom can you use?",
                options = new[] { "Going bananas", "Easy as pie", "Sugar and spice", "Giving candy to a baby" },
                correctOptionIndex = 0,
                explanation = "'Going bananas' and 'nutty as a fruitcake' both relate to acting crazy."
            },
            new QuizQuestion {
                questionId = "Q23_MCQ_SugarSpice2",
                format = QuestionFormat.MCQ,
                idiomTag = "Sugar and spice",
                familyTag = "Kindness",
                questionText = "When someone is 'all sugar and spice', they are being:",
                options = new[] { "kind and sweet", "Angry and frustrated", "Bored and sleepy", "Strict and demanding" },
                correctOptionIndex = 0,
                explanation = "'Sugar and spice' describes someone who is kind and sweet."
            },
            new QuizQuestion {
                questionId = "Q24_Fill_Highway",
                format = QuestionFormat.Fill,
                idiomTag = "My way or the highway",
                familyTag = "Strict",
                questionText = "Fill in the blank: The captain said it is my way or the ______.",
                fillCorrectAnswer = "highway",
                fillAcceptableAnswers = new[] { "highway", "high way" },
                explanation = "The idiom is 'my way or the highway'."
            },
            new QuizQuestion {
                questionId = "Q25_TF_GoBananas",
                format = QuestionFormat.TrueFalse,
                idiomTag = "Going bananas",
                familyTag = "Crazy",
                questionText = "True or False: 'Going bananas' means someone is feeling calm and relaxed.",
                options = new[] { "True", "False" },
                correctOptionIndex = 1,
                explanation = "False! 'Going bananas' means becoming crazy or very angry."
            },
            new QuizQuestion {
                questionId = "Q26_Group_CrazyIdioms",
                format = QuestionFormat.Group,
                idiomTag = "Crazy idioms",
                familyTag = "Crazy",
                questionText = "Which TWO idioms describe acting crazy or wild?",
                options = new[] { "Going bananas", "Sugar and spice", "Nutty as a fruitcake", "A piece of cake" },
                correctOptionIndices = new[] { 0, 2 },
                explanation = "'Going bananas' and 'Nutty as a fruitcake' both describe crazy behavior."
            },
            new QuizQuestion {
                questionId = "Q27_Match_CandyBaby",
                format = QuestionFormat.Match,
                idiomTag = "Giving candy to a baby",
                familyTag = "Easy",
                questionText = "Match 'Giving candy to a baby' with its meaning:",
                options = new[] { "Very easy", "Buying presents for toddlers", "Cooking healthy snacks", "Losing a competition" },
                correctOptionIndex = 0,
                explanation = "'Giving candy to a baby' means something is very easy."
            },
            new QuizQuestion {
                questionId = "Q28_LiteralReal_SugarSpice",
                format = QuestionFormat.LiteralReal,
                idiomTag = "Sugar and spice",
                familyTag = "Literal/Real",
                questionText = "'She is sugar and spice' — is she made of baking ingredients?",
                options = new[] { "No, she is kind and sweet", "Yes, she is baked like a cookie", "No, it means she is salty", "Yes, she works in a bakery" },
                correctOptionIndex = 0,
                explanation = "'Sugar and spice' is an idiom for kind and sweet, not actual food ingredients!"
            },
            new QuizQuestion {
                questionId = "Q29_Swap_CandyBaby",
                format = QuestionFormat.Swap,
                idiomTag = "Giving candy to a baby",
                familyTag = "Easy",
                questionText = "Which idiom is a synonym for 'like giving candy to a baby'?",
                options = new[] { "A piece of cake", "My way or the highway", "Going bananas", "Fruitcake" },
                correctOptionIndex = 0,
                explanation = "'A piece of cake' is a direct synonym for very easy."
            },
            new QuizQuestion {
                questionId = "Q30_TF_MyWay",
                format = QuestionFormat.TrueFalse,
                idiomTag = "My way or the highway",
                familyTag = "Strict",
                questionText = "True or False: 'My way or the highway' allows for teamwork and negotiation.",
                options = new[] { "True", "False" },
                correctOptionIndex = 1,
                explanation = "False! 'My way or the highway' means you have to listen to me without negotiation."
            }
        };
    }
}
