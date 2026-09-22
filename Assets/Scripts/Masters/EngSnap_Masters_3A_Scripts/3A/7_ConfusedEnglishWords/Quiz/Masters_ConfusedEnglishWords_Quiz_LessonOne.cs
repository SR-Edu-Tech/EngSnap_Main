using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.ConfusedWords;

/// <summary>
/// Unit 7 Quiz Lesson One (Q01 — Case-Closed Final Quiz).
/// Comprehensive 12-question mixed assessment across all three confused word pairs.
/// Pass mark is 9/12. Question 11 features dual acceptance (WHOEVER and WHOMEVER).
/// </summary>
public class Masters_ConfusedEnglishWords_Quiz_LessonOne : Masters_Lesson
{
    [Serializable]
    public class QuizQuestion
    {
        public string questionNumber;
        public string promptText;
        public string optionA;
        public string optionB;
        public string optionC;
        public string optionD;
        public int optionCount; // 2, 3, or 4
        public int correctOptionIndex;
        public bool isDualAcceptance;
        public string explanation;
    }

    [Header("UI References")]
    [SerializeField] private TMP_Text headerTMP;
    [SerializeField] private TMP_Text progressTMP;
    [SerializeField] private TMP_Text scoreTMP;
    [SerializeField] private TMP_Text questionTMP;
    [SerializeField] private TMP_Text feedbackTMP;

    [Header("Answer Options")]
    [SerializeField] private Button optionAButton;
    [SerializeField] private Button optionBButton;
    [SerializeField] private Button optionCButton;
    [SerializeField] private Button optionDButton;
    [SerializeField] private TMP_Text optionAText;
    [SerializeField] private TMP_Text optionBText;
    [SerializeField] private TMP_Text optionCText;
    [SerializeField] private TMP_Text optionDText;
    [SerializeField] private Image optionAImage;
    [SerializeField] private Image optionBImage;
    [SerializeField] private Image optionCImage;
    [SerializeField] private Image optionDImage;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text resultTitleTMP;
    [SerializeField] private TMP_Text finalScoreTMP;
    [SerializeField] private TMP_Text resultFeedbackTMP;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button backButton;

    [Header("Colors")]
    [SerializeField] private Color defaultCardColor = new Color(0.95f, 0.92f, 0.85f, 1f);
    [SerializeField] private Color correctCardColor = new Color(0.4f, 0.85f, 0.4f, 1f);
    [SerializeField] private Color incorrectCardColor = new Color(0.95f, 0.4f, 0.4f, 1f);

    [Header("Questions Data")]
    [SerializeField] private List<QuizQuestion> questions = new List<QuizQuestion>();

    private int currentIndex = 0;
    private int score = 0;
    private bool isAnswered = false;
    private const int PassThreshold = 9;

    protected override void Awake()
    {
        topic = Masters_Topic.Quiz;

        if (questions == null || questions.Count == 0)
        {
            InitializeDefaultQuestions();
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
            nextButton.gameObject.SetActive(false);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(RetryQuiz);
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        if (optionAButton != null)
        {
            optionAButton.onClick.RemoveAllListeners();
            optionAButton.onClick.AddListener(() => OnOptionSelected(0));
        }

        if (optionBButton != null)
        {
            optionBButton.onClick.RemoveAllListeners();
            optionBButton.onClick.AddListener(() => OnOptionSelected(1));
        }

        if (optionCButton != null)
        {
            optionCButton.onClick.RemoveAllListeners();
            optionCButton.onClick.AddListener(() => OnOptionSelected(2));
        }

        if (optionDButton != null)
        {
            optionDButton.onClick.RemoveAllListeners();
            optionDButton.onClick.AddListener(() => OnOptionSelected(3));
        }
    }

    protected override void Start()
    {
        base.Start();
        currentIndex = 0;
        score = 0;
        if (resultPanel != null) resultPanel.SetActive(false);
        LoadCurrentQuestion();
    }

    private void InitializeDefaultQuestions()
    {
        questions = new List<QuizQuestion>
        {
            // Q1
            new QuizQuestion
            {
                questionNumber = "Question 1/12",
                promptText = "Please be ______ while I study.",
                optionA = "QUIET",
                optionB = "QUITE",
                optionCount = 2,
                correctOptionIndex = 0,
                isDualAcceptance = false,
                explanation = "QUIET = no noise, silent."
            },
            // Q2
            new QuizQuestion
            {
                questionNumber = "Question 2/12",
                promptText = "This is not ______ the best idea.",
                optionA = "QUIET",
                optionB = "QUITE",
                optionCount = 2,
                correctOptionIndex = 1,
                isDualAcceptance = false,
                explanation = "QUITE = not exactly, fairly."
            },
            // Q3
            new QuizQuestion
            {
                questionNumber = "Question 3/12",
                promptText = "What word means 'no noise, silent'?",
                optionA = "QUIET",
                optionB = "QUITE",
                optionC = "DAIRY",
                optionCount = 3,
                correctOptionIndex = 0,
                isDualAcceptance = false,
                explanation = "QUIET = no noise, silent."
            },
            // Q4
            new QuizQuestion
            {
                questionNumber = "Question 4/12",
                promptText = "What word means 'not exactly, not perfectly'?",
                optionA = "QUIET",
                optionB = "QUITE",
                optionC = "DIARY",
                optionCount = 3,
                correctOptionIndex = 1,
                isDualAcceptance = false,
                explanation = "QUITE = not exactly, fairly."
            },
            // Q5
            new QuizQuestion
            {
                questionNumber = "Question 5/12",
                promptText = "She will not let me read her ______.",
                optionA = "DIARY",
                optionB = "DAIRY",
                optionCount = 2,
                correctOptionIndex = 0,
                isDualAcceptance = false,
                explanation = "DIARY = a personal record book."
            },
            // Q6
            new QuizQuestion
            {
                questionNumber = "Question 6/12",
                promptText = "I like to eat ______ products such as cheese and butter.",
                optionA = "DIARY",
                optionB = "DAIRY",
                optionCount = 2,
                correctOptionIndex = 1,
                isDualAcceptance = false,
                explanation = "DAIRY = products made from animal milk."
            },
            // Q7
            new QuizQuestion
            {
                questionNumber = "Question 7/12",
                promptText = "We are going to keep an online ______.",
                optionA = "DIARY",
                optionB = "DAIRY",
                optionCount = 2,
                correctOptionIndex = 0,
                isDualAcceptance = false,
                explanation = "DIARY = daily written entries/journal."
            },
            // Q8
            new QuizQuestion
            {
                questionNumber = "Question 8/12",
                promptText = "Give the letter to ______ opens the door. (He/she opens the door)",
                optionA = "WHOEVER",
                optionB = "WHOMEVER",
                optionCount = 2,
                correctOptionIndex = 0,
                isDualAcceptance = false,
                explanation = "He/she fits naturally as subject -> WHOEVER."
            },
            // Q9
            new QuizQuestion
            {
                questionNumber = "Question 9/12",
                promptText = "Give the letter to ______ you see first. (You see him/her first)",
                optionA = "WHOEVER",
                optionB = "WHOMEVER",
                optionCount = 2,
                correctOptionIndex = 1,
                isDualAcceptance = false,
                explanation = "Him/her fits naturally as object -> WHOMEVER."
            },
            // Q10
            new QuizQuestion
            {
                questionNumber = "Question 10/12",
                promptText = "WHOEVER stands in the position of:",
                optionA = "Subject (he/she)",
                optionB = "Object (him/her)",
                optionCount = 2,
                correctOptionIndex = 0,
                isDualAcceptance = false,
                explanation = "WHOEVER is a subject pronoun."
            },
            // Q11 (MANDATORY DUAL ACCEPTANCE)
            new QuizQuestion
            {
                questionNumber = "Question 11/12",
                promptText = "She always smiles at ______ she meets.",
                optionA = "WHOEVER",
                optionB = "WHOMEVER",
                optionCount = 2,
                correctOptionIndex = 0,
                isDualAcceptance = true,
                explanation = "Special Book Rule: Both WHOEVER and WHOMEVER are accepted!"
            },
            // Q12
            new QuizQuestion
            {
                questionNumber = "Question 12/12",
                promptText = "True or False: 'quiet' and 'quite' mean the same thing.",
                optionA = "TRUE",
                optionB = "FALSE",
                optionCount = 2,
                correctOptionIndex = 1,
                isDualAcceptance = false,
                explanation = "FALSE: 'quiet' means silent; 'quite' means fairly/not exactly."
            }
        };
    }

    private void LoadCurrentQuestion()
    {
        if (currentIndex >= questions.Count)
        {
            ShowResults();
            return;
        }

        isAnswered = false;
        var q = questions[currentIndex];

        if (headerTMP != null) headerTMP.text = "CASE FILE #713: CASE-CLOSED FINAL QUIZ";
        if (progressTMP != null) progressTMP.text = q.questionNumber;
        if (scoreTMP != null) scoreTMP.text = $"Score: {score}/{questions.Count}";
        if (questionTMP != null) questionTMP.text = q.promptText;

        if (optionAText != null) optionAText.text = q.optionA;
        if (optionBText != null) optionBText.text = q.optionB;
        if (optionCText != null) optionCText.text = q.optionC ?? "";
        if (optionDText != null) optionDText.text = q.optionD ?? "";

        SetOptionsInteractable(true);

        if (optionAButton != null) optionAButton.gameObject.SetActive(true);
        if (optionBButton != null) optionBButton.gameObject.SetActive(true);
        if (optionCButton != null) optionCButton.gameObject.SetActive(q.optionCount >= 3);
        if (optionDButton != null) optionDButton.gameObject.SetActive(q.optionCount >= 4);

        ResetOptionColors();

        if (feedbackTMP != null) feedbackTMP.text = "Answer carefully to solve the final unit case.";
        if (nextButton != null)
        {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }
    }

    private void SetOptionsInteractable(bool interactable)
    {
        if (optionAButton != null) optionAButton.interactable = interactable;
        if (optionBButton != null) optionBButton.interactable = interactable;
        if (optionCButton != null) optionCButton.interactable = interactable;
        if (optionDButton != null) optionDButton.interactable = interactable;
    }

    private void ResetOptionColors()
    {
        if (optionAImage != null) optionAImage.color = defaultCardColor;
        if (optionBImage != null) optionBImage.color = defaultCardColor;
        if (optionCImage != null) optionCImage.color = defaultCardColor;
        if (optionDImage != null) optionDImage.color = defaultCardColor;
    }

    public void OnOptionSelected(int optionIndex)
    {
        if (isAnswered || currentIndex >= questions.Count) return;

        isAnswered = true;
        SetOptionsInteractable(false);

        var q = questions[currentIndex];
        bool isCorrect = q.isDualAcceptance || (optionIndex == q.correctOptionIndex);

        if (isCorrect)
        {
            score++;
            SetButtonColor(optionIndex, correctCardColor);
            if (feedbackTMP != null) feedbackTMP.text = $"<color=#50AA5A>CORRECT! {q.explanation}</color>";
        }
        else
        {
            SetButtonColor(optionIndex, incorrectCardColor);
            SetButtonColor(q.correctOptionIndex, correctCardColor);
            if (feedbackTMP != null) feedbackTMP.text = $"<color=#E05050>INCORRECT! {q.explanation}</color>";
        }

        if (Masters_AudioManager.Instance != null)
        {
            if (!M3A_U7_HubProgress.IsIntroComplete())
            {
                Debug.LogWarning("[U7 AUDIO BLOCKED] Quiz_LessonOne attempted audio before Intro completion");
            }
            else
            {
                Masters_AudioManager.Instance.PlaySoundEffect(isCorrect ? Masters_SFX.Correct : Masters_SFX.Incorrect);
            }
        }

        if (scoreTMP != null) scoreTMP.text = $"Score: {score}/{questions.Count}";

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }
    }

    private void SetButtonColor(int index, Color color)
    {
        if (index == 0 && optionAImage != null) optionAImage.color = color;
        if (index == 1 && optionBImage != null) optionBImage.color = color;
        if (index == 2 && optionCImage != null) optionCImage.color = color;
        if (index == 3 && optionDImage != null) optionDImage.color = color;
    }

    private void ShowResults()
    {
        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(true);

        bool passed = score >= PassThreshold;

        if (passed)
        {
            M3A_U7_HubProgress.MarkQ01Complete(score, PassThreshold);

            if (resultTitleTMP != null) resultTitleTMP.text = "<color=#50AA5A>CASE CLOSED — PASSED!</color>";
            if (finalScoreTMP != null) finalScoreTMP.text = $"FINAL SCORE: {score}/{questions.Count}";
            if (resultFeedbackTMP != null) resultFeedbackTMP.text = "Outstanding detective work! You have mastered all confused word pairs. The Word Detective Badge is now unlocked!";

            if (retryButton != null) retryButton.gameObject.SetActive(false);
            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(true);
                continueButton.interactable = true;
            }
        }
        else
        {
            if (resultTitleTMP != null) resultTitleTMP.text = "<color=#E05050>CASE OPEN — TRY AGAIN</color>";
            if (finalScoreTMP != null) finalScoreTMP.text = $"FINAL SCORE: {score}/{questions.Count}";
            if (resultFeedbackTMP != null) resultFeedbackTMP.text = $"You scored {score}/{questions.Count}. A score of {PassThreshold}/{questions.Count} is required to close the case. Try again!";

            if (retryButton != null)
            {
                retryButton.gameObject.SetActive(true);
                retryButton.interactable = true;
            }
            if (continueButton != null) continueButton.gameObject.SetActive(false);
        }
    }

    public void RetryQuiz()
    {
        score = 0;
        currentIndex = 0;
        isAnswered = false;
        if (resultPanel != null) resultPanel.SetActive(false);
        LoadCurrentQuestion();
    }

    private void OnContinueClicked()
    {
        if (continueButton != null) continueButton.interactable = false;

        if (score >= PassThreshold)
        {
            M3A_U7_HubProgress.MarkQ01Complete(score, PassThreshold);
        }

        if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Quiz);
        }
        else if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ReturnToHub();
        }
    }

    protected override void OnNextButtonClicked()
    {
        if (!isAnswered) return;

        if (nextButton != null)
        {
            nextButton.interactable = false;
        }

        currentIndex++;
        if (currentIndex < questions.Count)
        {
            LoadCurrentQuestion();
        }
        else
        {
            ShowResults();
        }
    }

    private void OnBackClicked()
    {
        if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ReturnToHub();
        }
        else if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Quiz);
        }
    }
}
