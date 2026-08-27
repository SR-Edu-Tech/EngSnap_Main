using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public enum Q01QuestionType {
    MCQ,
    FillBlank,
    TrueFalse,
    OddOneOut,
    Match,
    SentenceChoice
}

[System.Serializable]
public class Q01QuestionData {
    public string questionId;
    public Q01QuestionType questionType;
    public string questionText;
    public AudioClip questionAudioClip;
    public string[] options;
    public int correctOptionIndex;       // 0-based for option-based questions
    public string fillCorrectAnswer;      // Expected text for FillBlank questions
    public string correctHubName;        // "CATCH", "GET", "SAVE", "IDEA"
    public string explanation;
}

/// <summary>
/// Controller for Unit 7 (Collocations) Quiz Q01 (Main Console Quiz — Collocations).
/// Hosted by ARIA with 12 mixed-format questions.
/// Features:
/// - Data-driven 12 questions (MCQ, Fill, T/F, Odd One Out, Match, Sentence Choice)
/// - Prerequisite 6/6 branch completion check
/// - Dynamic UI format rendering between option grid & TMP_InputField
/// - Running score counter (Pass mark >= 9/12)
/// - 1 Retry logic without duplicate point allocation
/// - Immediate feedback banner with correct hub name
/// - Result popup with reward unlock integration
/// </summary>
public class Masters_Collocations_Quiz_LessonOne : Masters_Lesson {

    [Header("Q01 Question Data Bank (12 Questions)")]
    [SerializeField] private Q01QuestionData[] questions;

    [Header("UI Header & Progress")]
    [SerializeField] private TextMeshProUGUI mainTitleTMP;
    [SerializeField] private TextMeshProUGUI progressIndicatorTMP;
    [SerializeField] private TextMeshProUGUI runningScoreTMP;

    [Header("Question Prompt UI")]
    [SerializeField] private TextMeshProUGUI questionPromptTMP;
    [SerializeField] private TextMeshProUGUI questionTypeBadgeTMP;

    [Header("Answer Container — Option Buttons (4 or 2 Options)")]
    [SerializeField] private GameObject optionGridParent;
    [SerializeField] private Button[] optionButtons;            // 4 option buttons
    [SerializeField] private TextMeshProUGUI[] optionButtonTMPs;
    [SerializeField] private Image[] optionButtonImages;

    [Header("Answer Container — Fill in the Blank")]
    [SerializeField] private GameObject fillInputContainer;
    [SerializeField] private TMP_InputField fillInputField;
    [SerializeField] private Button fillSubmitButton;

    [Header("Feedback & Controls")]
    [SerializeField] private TextMeshProUGUI feedbackBannerTMP;
    [SerializeField] private Button continueNextButton;

    [Header("Result Popup")]
    [SerializeField] private GameObject resultPopup;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private Button retryQuizButton;
    [SerializeField] private Button returnHubButton;

    [Header("Audio References")]
    [SerializeField] private AudioClip ariaIntroAudio;
    [SerializeField] private AudioClip sfxCorrect;
    [SerializeField] private AudioClip sfxIncorrect;

    // Runtime state variables
    private int currentQuestionIndex = 0;
    private int totalScore = 0;
    private int currentQuestionAttemptCount = 0; // 0 = first attempt, 1 = retry
    private bool isAnsweringActive = false;
    private bool currentQuestionScored = false;

    private const int PASS_THRESHOLD = 9;
    private const int TOTAL_QUESTIONS = 12;

    protected virtual void OnEnable() {
        // Prevent STT subscriptions
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Quiz;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeQuestionsData();
        UpdateTitleAndUIComponents();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Quiz;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeQuestionsData();
        UpdateTitleAndUIComponents();
        SetupButtonListeners();

        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (resultPopup != null) resultPopup.SetActive(false);

        StartCoroutine(StartQ01QuizRoutine());
    }

    private void DeactivateObsoleteBaseUI() {
        Transform skipTrans = transform.Find("SkipButton");
        if (skipTrans != null) skipTrans.gameObject.SetActive(false);

        Transform contTrans = transform.Find("Continue");
        if (contTrans != null) contTrans.gameObject.SetActive(false);

        Transform heading = transform.Find("Heading") ?? transform.Find("Header");
        if (heading != null) heading.gameObject.SetActive(false);
    }

    public void InitializeQuestionsData() {
        string audioDir = "Assets/Audio/2A/7_Collocations/Quiz/Q01/";

        #if UNITY_EDITOR
        ariaIntroAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Welcome to the Main Console Quiz Answer 12 collocation questions covering CATCH GET SAVE and IDEA.mp3");
        #endif

        questions = new Q01QuestionData[] {
            // Q1 (MCQ)
            new Q01QuestionData {
                questionId = "Q1",
                questionType = Q01QuestionType.MCQ,
                questionText = "Which hub goes with 'a bus'?",
                #if UNITY_EDITOR
                questionAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Which hub goes with 'a bus'.mp3"),
                #endif
                options = new string[] { "CATCH", "GET", "SAVE", "IDEA" },
                correctOptionIndex = 0,
                correctHubName = "CATCH",
                explanation = "'catch a bus' belongs to the CATCH hub."
            },
            // Q2 (MCQ)
            new Q01QuestionData {
                questionId = "Q2",
                questionType = Q01QuestionType.MCQ,
                questionText = "Which hub goes with 'permission'?",
                #if UNITY_EDITOR
                questionAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Which hub goes with 'permission'.mp3"),
                #endif
                options = new string[] { "GET", "CATCH", "SAVE", "IDEA" },
                correctOptionIndex = 0,
                correctHubName = "GET",
                explanation = "'get permission' belongs to the GET hub."
            },
            // Q3 (MCQ)
            new Q01QuestionData {
                questionId = "Q3",
                questionType = Q01QuestionType.MCQ,
                questionText = "Which hub goes with 'electricity'?",
                #if UNITY_EDITOR
                questionAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Which hub goes with 'electricity'.mp3"),
                #endif
                options = new string[] { "SAVE", "GET", "CATCH", "IDEA" },
                correctOptionIndex = 0,
                correctHubName = "SAVE",
                explanation = "'save electricity' belongs to the SAVE hub."
            },
            // Q4 (MCQ)
            new Q01QuestionData {
                questionId = "Q4",
                questionType = Q01QuestionType.MCQ,
                questionText = "Which hub goes with 'outlandish'?",
                #if UNITY_EDITOR
                questionAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Which hub goes with 'outlandish'.mp3"),
                #endif
                options = new string[] { "IDEA", "CATCH", "GET", "SAVE" },
                correctOptionIndex = 0,
                correctHubName = "IDEA",
                explanation = "'outlandish idea' belongs to the IDEA hub."
            },
            // Q5 (Fill in the blank)
            new Q01QuestionData {
                questionId = "Q5",
                questionType = Q01QuestionType.FillBlank,
                questionText = "Wash your hands so you don't catch a ______.",
                #if UNITY_EDITOR
                questionAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Wash your hands so you don't catch a blank.mp3"),
                #endif
                fillCorrectAnswer = "cold",
                correctHubName = "CATCH",
                explanation = "Full collocation: catch a cold."
            },
            // Q6 (Fill in the blank)
            new Q01QuestionData {
                questionId = "Q6",
                questionType = Q01QuestionType.FillBlank,
                questionText = "Please ______ ready for school.",
                #if UNITY_EDITOR
                questionAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Please blank ready for school.mp3"),
                #endif
                fillCorrectAnswer = "get",
                correctHubName = "GET",
                explanation = "Full collocation: get ready."
            },
            // Q7 (Fill in the blank)
            new Q01QuestionData {
                questionId = "Q7",
                questionType = Q01QuestionType.FillBlank,
                questionText = "Turn off the tap and save ______ water.",
                #if UNITY_EDITOR
                questionAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Turn off the tap and save blank water.mp3"),
                #endif
                fillCorrectAnswer = "water",
                correctHubName = "SAVE",
                explanation = "Full collocation: save water."
            },
            // Q8 (True/False)
            new Q01QuestionData {
                questionId = "Q8",
                questionType = Q01QuestionType.TrueFalse,
                questionText = "True or False: 'catch your breath' is a real collocation.",
                #if UNITY_EDITOR
                questionAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "True or False 'catch your breath' is a real collocation.mp3"),
                #endif
                options = new string[] { "TRUE", "FALSE" },
                correctOptionIndex = 0,
                correctHubName = "CATCH",
                explanation = "'catch your breath' is a valid collocation!"
            },
            // Q9 (True/False)
            new Q01QuestionData {
                questionId = "Q9",
                questionType = Q01QuestionType.TrueFalse,
                questionText = "True or False: 'get a thief' is a real collocation.",
                #if UNITY_EDITOR
                questionAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "True or False 'get a thief' is a real collocation.mp3"),
                #endif
                options = new string[] { "TRUE", "FALSE" },
                correctOptionIndex = 1,
                correctHubName = "CATCH",
                explanation = "The correct collocation is 'catch a thief'."
            },
            // Q10 (Odd One Out)
            new Q01QuestionData {
                questionId = "Q10",
                questionType = Q01QuestionType.OddOneOut,
                questionText = "Which does NOT go with GET?",
                #if UNITY_EDITOR
                questionAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Which does NOT go with GET.mp3"),
                #endif
                options = new string[] { "ready", "married", "started", "train" },
                correctOptionIndex = 3,
                correctHubName = "GET",
                explanation = "'get ready', 'get married', and 'get started' are valid collocations. 'get a train' belongs to CATCH!"
            },
            // Q11 (Match)
            new Q01QuestionData {
                questionId = "Q11",
                questionType = Q01QuestionType.Match,
                questionText = "Match 'someone a seat' →",
                #if UNITY_EDITOR
                questionAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Match 'someone a seat'.mp3"),
                #endif
                options = new string[] { "SAVE", "GET", "CATCH", "IDEA" },
                correctOptionIndex = 0,
                correctHubName = "SAVE",
                explanation = "Correct collocation: 'save someone a seat'."
            },
            // Q12 (Sentence Choice)
            new Q01QuestionData {
                questionId = "Q12",
                questionType = Q01QuestionType.SentenceChoice,
                questionText = "Choose the correct sentence:",
                #if UNITY_EDITOR
                questionAudioClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Choose the correct sentence She had a bright idea or She had a bright catch.mp3"),
                #endif
                options = new string[] { "She had a bright idea.", "She had a bright catch." },
                correctOptionIndex = 0,
                correctHubName = "IDEA",
                explanation = "Correct collocation: 'bright idea'."
            }
        };
    }

    private IEnumerator StartQ01QuizRoutine() {
        currentQuestionIndex = 0;
        totalScore = 0;

        UpdateScoreDisplay();
        if (resultPopup != null) resultPopup.SetActive(false);

        // Play ARIA Intro Audio
        if (ariaIntroAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroAudio);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(0.5f);
        }

        yield return StartCoroutine(LoadQuestionRoutine(0));
    }

    private IEnumerator LoadQuestionRoutine(int index) {
        if (questions == null || index < 0 || index >= questions.Length) yield break;

        currentQuestionIndex = index;
        currentQuestionAttemptCount = 0;
        currentQuestionScored = false;
        isAnsweringActive = true;

        Q01QuestionData currentQ = questions[index];

        UpdateProgressDisplay();
        ShowFeedback("", false, false);
        if (continueNextButton != null) continueNextButton.gameObject.SetActive(false);

        // Set Question Prompt & Type Badge
        if (questionPromptTMP != null) questionPromptTMP.text = currentQ.questionText;
        if (questionTypeBadgeTMP != null) questionTypeBadgeTMP.text = GetFormatLabel(currentQ.questionType);

        // Render Answer UI based on Question Format
        SetupAnswerUIForQuestion(currentQ);

        // Play Question Audio
        if (currentQ.questionAudioClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(currentQ.questionAudioClip);
        }

        yield break;
    }

    private string GetFormatLabel(Q01QuestionType type) {
        switch (type) {
            case Q01QuestionType.MCQ: return "MULTIPLE CHOICE";
            case Q01QuestionType.FillBlank: return "FILL IN THE BLANK";
            case Q01QuestionType.TrueFalse: return "TRUE OR FALSE";
            case Q01QuestionType.OddOneOut: return "ODD ONE OUT";
            case Q01QuestionType.Match: return "MATCHING";
            case Q01QuestionType.SentenceChoice: return "SENTENCE CHOICE";
            default: return "QUIZ";
        }
    }

    private void SetupAnswerUIForQuestion(Q01QuestionData q) {
        // Toggle containers
        bool isFill = (q.questionType == Q01QuestionType.FillBlank);

        if (fillInputContainer != null) fillInputContainer.SetActive(isFill);
        if (optionGridParent != null) optionGridParent.SetActive(!isFill);

        if (isFill) {
            if (fillInputField != null) {
                fillInputField.text = "";
                fillInputField.interactable = true;
            }
            if (fillSubmitButton != null) fillSubmitButton.interactable = true;
        } else {
            // Setup Option Buttons (2 or 4 options)
            Color defaultBtnColor = new Color(0.12f, 0.40f, 0.85f, 1f); // Royal Blue

            int optionCount = q.options != null ? q.options.Length : 0;

            for (int i = 0; i < 4; i++) {
                if (i < optionButtons.Length && optionButtons[i] != null) {
                    bool active = (i < optionCount);
                    optionButtons[i].gameObject.SetActive(active);
                    optionButtons[i].interactable = true;

                    if (active) {
                        if (i < optionButtonTMPs.Length && optionButtonTMPs[i] != null) {
                            optionButtonTMPs[i].text = q.options[i];
                            optionButtonTMPs[i].color = Color.white;
                        }
                        if (i < optionButtonImages.Length && optionButtonImages[i] != null) {
                            optionButtonImages[i].color = defaultBtnColor;
                        }
                    }
                }
            }
        }
    }

    public void OnOptionButtonClicked(int selectedIndex) {
        if (!isAnsweringActive || questions == null || currentQuestionIndex >= questions.Length) return;

        Q01QuestionData currentQ = questions[currentQuestionIndex];
        bool isCorrect = (selectedIndex == currentQ.correctOptionIndex);

        EvaluateAnswer(currentQ, isCorrect, selectedIndex);
    }

    public void OnFillSubmitClicked() {
        if (!isAnsweringActive || questions == null || currentQuestionIndex >= questions.Length) return;

        Q01QuestionData currentQ = questions[currentQuestionIndex];
        string userText = fillInputField != null ? fillInputField.text : "";

        string cleanUser = NormalizeText(userText);
        string cleanCorrect = NormalizeText(currentQ.fillCorrectAnswer);

        bool isCorrect = (cleanUser == cleanCorrect);

        EvaluateAnswer(currentQ, isCorrect, -1);
    }

    private void EvaluateAnswer(Q01QuestionData q, bool isCorrect, int selectedIndex) {
        Color correctColor = new Color(0.13f, 0.77f, 0.36f, 1f); // Green
        Color incorrectColor = new Color(0.92f, 0.32f, 0.20f, 1f); // Red

        if (isCorrect) {
            // Correct Answer
            isAnsweringActive = false;

            if (!currentQuestionScored) {
                totalScore++;
                currentQuestionScored = true;
                UpdateScoreDisplay();
            }

            // Highlight button green
            if (selectedIndex >= 0 && selectedIndex < optionButtonImages.Length && optionButtonImages[selectedIndex] != null) {
                optionButtonImages[selectedIndex].color = correctColor;
                optionButtonImages[selectedIndex].transform.DOKill(true);
                optionButtonImages[selectedIndex].transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }

            if (fillInputField != null) fillInputField.interactable = false;
            if (fillSubmitButton != null) fillSubmitButton.interactable = false;

            if (sfxCorrect != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxCorrect);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            string msg = $"CORRECT! • Hub: {q.correctHubName}";
            if (!string.IsNullOrEmpty(q.explanation)) msg += $"\n{q.explanation}";
            ShowFeedback(msg, true, true);

            if (continueNextButton != null) {
                continueNextButton.gameObject.SetActive(true);
            } else {
                StartCoroutine(AdvanceAfterDelayRoutine(1.8f));
            }
        } else {
            // Incorrect Answer
            if (sfxIncorrect != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxIncorrect);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            // Highlight pressed button red
            if (selectedIndex >= 0 && selectedIndex < optionButtonImages.Length && optionButtonImages[selectedIndex] != null) {
                optionButtonImages[selectedIndex].color = incorrectColor;
                optionButtonImages[selectedIndex].transform.DOKill(true);
                optionButtonImages[selectedIndex].transform.DOShakePosition(0.4f, 10f, 10, 90f);
            }

            currentQuestionAttemptCount++;

            string msg = $"NOT QUITE! • Hub: {q.correctHubName}";
            if (!string.IsNullOrEmpty(q.explanation)) msg += $"\n{q.explanation}";

            if (currentQuestionAttemptCount == 1) {
                // 1 Retry allowed while preserving score state
                ShowFeedback($"{msg}\n(Try again!)", false, true);
            } else {
                // Second attempt failed - show correct answer and enable Next
                isAnsweringActive = false;
                ShowFeedback($"{msg}\nCorrect Answer: {(q.questionType == Q01QuestionType.FillBlank ? q.fillCorrectAnswer : (q.options != null && q.correctOptionIndex < q.options.Length ? q.options[q.correctOptionIndex] : ""))}", false, true);

                if (continueNextButton != null) {
                    continueNextButton.gameObject.SetActive(true);
                } else {
                    StartCoroutine(AdvanceAfterDelayRoutine(2.5f));
                }
            }
        }
    }

    public void OnContinueNextClicked() {
        if (continueNextButton != null) continueNextButton.gameObject.SetActive(false);

        int nextIndex = currentQuestionIndex + 1;
        if (nextIndex < TOTAL_QUESTIONS) {
            StartCoroutine(LoadQuestionRoutine(nextIndex));
        } else {
            EndQ01Quiz();
        }
    }

    private IEnumerator AdvanceAfterDelayRoutine(float delay) {
        yield return new WaitForSeconds(delay);
        OnContinueNextClicked();
    }

    private void EndQ01Quiz() {
        isAnsweringActive = false;
        bool passed = (totalScore >= PASS_THRESHOLD);

        if (passed) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Quiz);
            }
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }

        ShowResultPopup(passed);
    }

    private void ShowResultPopup(bool passed) {
        if (resultPopup != null) {
            resultPopup.SetActive(true);
            resultPopup.transform.DOKill();
            resultPopup.transform.localScale = Vector3.zero;
            resultPopup.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = passed ? "QUIZ COMPLETE!" : "TRY AGAIN!";
            resultTitleTMP.color = passed ? new Color(0.13f, 0.77f, 0.36f) : new Color(0.85f, 0.2f, 0.2f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Your Score: {totalScore}/{TOTAL_QUESTIONS}\n{(passed ? "Congratulations! Reward Unlocked!" : "Score at least 9/12 to pass!")}";
        }
    }

    private void ShowFeedback(string msg, bool isSuccess, bool show) {
        if (feedbackBannerTMP != null) {
            feedbackBannerTMP.gameObject.SetActive(show);
            feedbackBannerTMP.text = msg;
            feedbackBannerTMP.color = isSuccess ? new Color(0.9f, 0.98f, 0.9f) : new Color(1f, 0.4f, 0.4f);
        }
    }

    private void UpdateProgressDisplay() {
        if (progressIndicatorTMP != null) {
            progressIndicatorTMP.text = $"Question {currentQuestionIndex + 1}/{TOTAL_QUESTIONS}";
        }
    }

    private void UpdateScoreDisplay() {
        if (runningScoreTMP != null) {
            runningScoreTMP.text = $"Score: {totalScore}/{TOTAL_QUESTIONS}";
        }
    }

    private void UpdateTitleAndUIComponents() {
        if (mainTitleTMP != null) {
            mainTitleTMP.gameObject.SetActive(true);
            mainTitleTMP.text = "Main Console Quiz — Collocations";
            mainTitleTMP.color = Color.white;
            RectTransform rt = mainTitleTMP.GetComponent<RectTransform>();
            if (rt != null) {
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(1000f, 50f);
                rt.anchoredPosition = new Vector2(0f, -40f);
            }
        }
    }

    private void SetupButtonListeners() {
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                int btnIdx = i;
                if (optionButtons[i] != null) {
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionButtonClicked(btnIdx));
                }
            }
        }

        if (fillSubmitButton != null) {
            fillSubmitButton.onClick.RemoveAllListeners();
            fillSubmitButton.onClick.AddListener(OnFillSubmitClicked);
        }

        if (continueNextButton != null) {
            continueNextButton.onClick.RemoveAllListeners();
            continueNextButton.onClick.AddListener(OnContinueNextClicked);
        }

        if (retryQuizButton != null) {
            retryQuizButton.onClick.RemoveAllListeners();
            retryQuizButton.onClick.AddListener(() => StartCoroutine(StartQ01QuizRoutine()));
        }

        if (returnHubButton != null) {
            returnHubButton.onClick.RemoveAllListeners();
            returnHubButton.onClick.AddListener(ReturnToHub);
        }
    }

    protected override void OnNextButtonClicked() {
        ReturnToHub();
    }

    public void ReturnToHub() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Quiz);
        }
    }

    private string NormalizeText(string text) {
        if (string.IsNullOrEmpty(text)) return "";
        string lower = text.ToLowerInvariant().Trim();
        char[] chars = lower.ToCharArray();
        for (int i = 0; i < chars.Length; i++) {
            if (char.IsPunctuation(chars[i])) chars[i] = ' ';
        }
        string[] words = new string(chars).Split(new char[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", words);
    }

    private void AutoFindUIReferences() {
        if (mainTitleTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("Title");
            if (t != null) mainTitleTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (progressIndicatorTMP == null) {
            Transform t = transform.Find("RoundProgressText") ?? transform.Find("ProgressIndicator");
            if (t != null) progressIndicatorTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (runningScoreTMP == null) {
            Transform t = transform.Find("ScoreText") ?? transform.Find("RunningScoreText");
            if (t != null) runningScoreTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (questionPromptTMP == null) {
            Transform t = transform.Find("QuestionPromptText");
            if (t != null) questionPromptTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (questionTypeBadgeTMP == null) {
            Transform t = transform.Find("QuestionTypeBadge");
            if (t != null) questionTypeBadgeTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (optionGridParent == null) {
            Transform t = transform.Find("OptionGrid");
            if (t != null) optionGridParent = t.gameObject;
        }

        if (optionButtons == null || optionButtons.Length < 4) {
            optionButtons = new Button[4];
            optionButtonTMPs = new TextMeshProUGUI[4];
            optionButtonImages = new Image[4];

            if (optionGridParent != null) {
                for (int i = 0; i < 4; i++) {
                    Transform btnTrans = optionGridParent.transform.Find($"OptionButton_{i + 1}");
                    if (btnTrans != null) {
                        optionButtons[i] = btnTrans.GetComponent<Button>();
                        optionButtonTMPs[i] = btnTrans.GetComponentInChildren<TextMeshProUGUI>(true);
                        optionButtonImages[i] = btnTrans.GetComponent<Image>();
                    }
                }
            }
        }

        if (fillInputContainer == null) {
            Transform t = transform.Find("FillInputContainer");
            if (t != null) fillInputContainer = t.gameObject;
        }

        if (fillInputField == null && fillInputContainer != null) {
            fillInputField = fillInputContainer.GetComponentInChildren<TMP_InputField>(true);
        }

        if (fillSubmitButton == null && fillInputContainer != null) {
            fillSubmitButton = fillInputContainer.GetComponentInChildren<Button>(true);
        }

        if (fillInputField != null && fillInputField.textViewport == null) {
            RectTransform rt = fillInputField.GetComponent<RectTransform>();
            Transform ta = fillInputField.transform.Find("Text Area");
            if (ta != null) rt = ta.GetComponent<RectTransform>();
            fillInputField.textViewport = rt;
        }

        if (feedbackBannerTMP == null) {
            Transform t = transform.Find("FeedbackText");
            if (t != null) feedbackBannerTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (continueNextButton == null) {
            Transform t = transform.Find("ContinueNextButton");
            if (t != null) continueNextButton = t.GetComponent<Button>();
        }

        if (resultPopup == null) {
            Transform t = transform.Find("ResultPopup") ?? transform.Find("ResultPanel");
            if (t != null) resultPopup = t.gameObject;
        }

        if (resultPopup != null) {
            Button[] resBtns = resultPopup.GetComponentsInChildren<Button>(true);
            foreach (var b in resBtns) {
                if (b == null) continue;
                string bName = b.name.ToLower();
                if (retryQuizButton == null && (bName.Contains("retry") || bName.Contains("again"))) retryQuizButton = b;
                if (returnHubButton == null && (bName.Contains("hub") || bName.Contains("home") || bName.Contains("continue"))) returnHubButton = b;
            }
        }
    }
}