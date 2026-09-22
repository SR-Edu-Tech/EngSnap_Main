using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Unit 6 Colour Your Speech — Writing L01 Controller.
/// Activity: "W01 — Complete the Idiom (type it)"
/// 
/// Mechanics:
/// - Displays verbatim book sentence with a missing idiom word blank.
/// - Displays word-bank rail containing the missing words as assistance.
/// - Student types the missing word into the input field and presses Submit.
/// - Exact case-insensitive matching on the missing word only with trimmed whitespace.
/// - Correct answer: plays match sound, shows green feedback, ARIA readback VO, updates word-bank rail, advances.
/// - Incorrect answer: shows red feedback, plays incorrect sound, allows exactly ONE retry.
/// - Score tracking: First-attempt correct answers only (pass threshold: >= 6/8).
/// - Progress: Marks M3A_U6_HubProgress.MarkW01Complete() on pass and returns cleanly to Hub.
/// </summary>
public class Masters_ColourYourSpeech_Writing_LessonOne : Masters_Lesson
{
    [System.Serializable]
    public class WritingQuestion
    {
        public string incomingMessageText;       // Verbatim sentence with blank
        public string[] acceptableExactMatches;  // Verbatim missing word
        public string hintText;                  // Word-bank entry
        public AudioClip correctAudio;           // ARIA readback VO on correct
    }

    [Header("Writing Lesson Data")]
    [SerializeField] protected WritingQuestion[] questions;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI promptTMP;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button checkButton;
    [SerializeField] private TextMeshProUGUI progressCountTMP;
    [SerializeField] private TextMeshProUGUI hintTMP;          // Word-bank rail
    [SerializeField] private TextMeshProUGUI lessonTitleTMP;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject correctTickObject;

    [Header("Settings & Visual Feedback")]
    [SerializeField] private float timeBetweenQuestions = 1.5f;
    [SerializeField] private Color correctColor = new Color(0.18f, 0.8f, 0.44f, 1f);
    [SerializeField] private Color incorrectColor = new Color(0.9f, 0.3f, 0.23f, 1f);
    [SerializeField] private Color defaultInputFieldColor = Color.white;
    [SerializeField] private Image inputFieldBackground;
    [SerializeField] private int passThreshold = 6;

    [Header("Audio")]
    [SerializeField] private AudioClip voAriaIntro;

    // Runtime state
    private int currentQuestionIndex;
    private WritingQuestion currentQuestion;
    private bool isTransitioning = false;
    private bool isFirstAttempt = true;
    private int firstAttemptCorrectCount = 0;
    private List<string> remainingWordBank;

    public int FirstAttemptScore => firstAttemptCorrectCount;
    public int CurrentQuestionIndex => currentQuestionIndex;
    public bool IsW01Passed => firstAttemptCorrectCount >= passThreshold;

    protected override void Awake()
    {
        base.Awake();
        topic = Masters_Topic.Writing;

        FindAndAutoAssignUI();

        if (checkButton != null)
        {
            checkButton.onClick.RemoveAllListeners();
            checkButton.onClick.AddListener(OnCheckButtonClicked);
        }

        if (inputField != null)
        {
            inputField.onSubmit.RemoveAllListeners();
            inputField.onSubmit.AddListener(SubmitAnswer);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    protected override void Start()
    {
        base.Start();
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
            nextButton.interactable = false;
        }

        currentQuestionIndex = 0;
        firstAttemptCorrectCount = 0;
        isTransitioning = false;
        isFirstAttempt = true;

        // Initialize Word-Bank Rail
        remainingWordBank = new List<string>();
        if (questions != null)
        {
            foreach (var q in questions)
            {
                if (q.acceptableExactMatches != null && q.acceptableExactMatches.Length > 0 && !string.IsNullOrEmpty(q.acceptableExactMatches[0]))
                {
                    remainingWordBank.Add(q.acceptableExactMatches[0]);
                }
            }
        }

        StartCoroutine(InitializeLessonRoutine());
    }

    private void FindAndAutoAssignUI()
    {
        if (lessonTitleTMP == null)
        {
            Transform titleTrans = transform.Find("LessonTitle") ?? FindChildRecursive(transform, "LessonTitle");
            if (titleTrans != null)
            {
                lessonTitleTMP = titleTrans.GetComponent<TextMeshProUGUI>() ?? titleTrans.GetComponentInChildren<TextMeshProUGUI>();
            }
        }
        if (lessonTitleTMP != null)
        {
            lessonTitleTMP.text = "W01 — Complete the Idiom (type it)";
        }

        if (promptTMP == null)
        {
            Transform promptTrans = transform.Find("Prompt") ?? FindChildRecursive(transform, "Prompt");
            if (promptTrans != null)
            {
                promptTMP = promptTrans.GetComponent<TextMeshProUGUI>() ?? promptTrans.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (inputField == null)
        {
            Transform inputTrans = transform.Find("Player inputfield") ?? FindChildRecursive(transform, "Player inputfield");
            if (inputTrans != null)
            {
                inputField = inputTrans.GetComponent<TMP_InputField>();
                if (inputFieldBackground == null) inputFieldBackground = inputTrans.GetComponent<Image>();
            }
        }

        if (checkButton == null)
        {
            Transform checkTrans = transform.Find("CheckButton") ?? FindChildRecursive(transform, "CheckButton");
            if (checkTrans != null) checkButton = checkTrans.GetComponent<Button>();
        }

        if (progressCountTMP == null)
        {
            Transform progTrans = transform.Find("progression count") ?? FindChildRecursive(transform, "progression count");
            if (progTrans != null)
            {
                progressCountTMP = progTrans.GetComponent<TextMeshProUGUI>() ?? progTrans.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (hintTMP == null)
        {
            Transform hintTrans = transform.Find("HintContainer/Hint text") ?? transform.Find("Hint text") ?? FindChildRecursive(transform, "Hint text");
            if (hintTrans != null)
            {
                hintTMP = hintTrans.GetComponent<TextMeshProUGUI>() ?? hintTrans.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (backButton == null)
        {
            Transform backTrans = transform.Find("BackButton") ?? FindChildRecursive(transform, "BackButton");
            if (backTrans != null) backButton = backTrans.GetComponent<Button>();
        }

        if (correctTickObject == null)
        {
            Transform tickTrans = transform.Find("Tick") ?? transform.Find("Player inputfield/Tick") ?? FindChildRecursive(transform, "Tick");
            if (tickTrans != null) correctTickObject = tickTrans.gameObject;
        }
    }

    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName) return child;
            Transform found = FindChildRecursive(child, childName);
            if (found != null) return found;
        }
        return null;
    }

    private IEnumerator InitializeLessonRoutine()
    {
        // Play Intro VO
        AudioClip introClip = voAriaIntro != null ? voAriaIntro : narratorSpeech;
        if (introClip != null && Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlayVoiceOver(introClip);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
        }

        LoadQuestion(0);
    }

    public void LoadQuestion(int index)
    {
        if (questions == null || index < 0 || index >= questions.Length)
        {
            OnAllQuestionsCompleted();
            return;
        }

        currentQuestionIndex = index;
        currentQuestion = questions[index];
        isTransitioning = false;
        isFirstAttempt = true;

        if (promptTMP != null) promptTMP.text = currentQuestion.incomingMessageText;
        if (progressCountTMP != null) progressCountTMP.text = $"{index + 1}/{questions.Length}";
        if (correctTickObject != null) correctTickObject.SetActive(false);

        UpdateWordBankDisplay();

        if (inputField != null)
        {
            inputField.text = "";
            inputField.interactable = true;
            if (inputFieldBackground != null) inputFieldBackground.color = defaultInputFieldColor;
            inputField.Select();
            inputField.ActivateInputField();
        }

        if (checkButton != null) checkButton.interactable = true;
    }

    private void UpdateWordBankDisplay()
    {
        if (hintTMP != null)
        {
            if (remainingWordBank != null && remainingWordBank.Count > 0)
            {
                hintTMP.text = string.Join("   |   ", remainingWordBank);
            }
            else
            {
                hintTMP.text = "";
            }

            if (hintTMP.transform.parent != null)
            {
                hintTMP.transform.parent.gameObject.SetActive(true);
            }
        }
    }

    public void SubmitAnswer(string inputAnswer)
    {
        if (currentQuestion == null || isTransitioning) return;

        string userInput = (inputAnswer ?? "").Trim();
        if (string.IsNullOrEmpty(userInput)) return;

        string normalizedInput = userInput.ToLowerInvariant();
        bool isCorrect = false;

        if (currentQuestion.acceptableExactMatches != null && currentQuestion.acceptableExactMatches.Length > 0)
        {
            foreach (string exact in currentQuestion.acceptableExactMatches)
            {
                if (!string.IsNullOrEmpty(exact) && normalizedInput == exact.Trim().ToLowerInvariant())
                {
                    isCorrect = true;
                    break;
                }
            }
        }

        if (isCorrect)
        {
            // First-attempt scoring: retry correctness does NOT inflate score
            if (isFirstAttempt)
            {
                firstAttemptCorrectCount++;
            }

            isTransitioning = true;
            if (inputFieldBackground != null) inputFieldBackground.color = correctColor;
            if (inputField != null) inputField.interactable = false;
            if (checkButton != null) checkButton.interactable = false;
            if (correctTickObject != null) correctTickObject.SetActive(true);

            // Display completed sentence with filled-in word highlighted
            if (promptTMP != null && currentQuestion.acceptableExactMatches != null && currentQuestion.acceptableExactMatches.Length > 0)
            {
                string completedWord = currentQuestion.acceptableExactMatches[0];
                promptTMP.text = currentQuestion.incomingMessageText.Replace("_____", $"<color=#2ECC71><b>{completedWord}</b></color>");
            }

            // Remove used word from word bank rail
            if (currentQuestion.acceptableExactMatches != null && currentQuestion.acceptableExactMatches.Length > 0)
            {
                remainingWordBank.Remove(currentQuestion.acceptableExactMatches[0]);
                UpdateWordBankDisplay();
            }

            // Audio & Readback VO
            if (Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                if (currentQuestion.correctAudio != null)
                {
                    Masters_AudioManager.Instance.PlayVoiceOver(currentQuestion.correctAudio);
                }
            }

            StartCoroutine(NextQuestionRoutine());
        }
        else
        {
            // Wrong answer
            if (inputFieldBackground != null) inputFieldBackground.color = incorrectColor;
            if (correctTickObject != null) correctTickObject.SetActive(false);
            if (Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (isFirstAttempt)
            {
                // Allow exactly ONE retry
                isFirstAttempt = false;
                StartCoroutine(RetryPromptRoutine());
            }
            else
            {
                // Retry failed: reveal correct answer and advance
                isTransitioning = true;
                if (inputField != null)
                {
                    if (currentQuestion.acceptableExactMatches != null && currentQuestion.acceptableExactMatches.Length > 0)
                    {
                        inputField.text = currentQuestion.acceptableExactMatches[0];
                    }
                    inputField.interactable = false;
                }
                if (checkButton != null) checkButton.interactable = false;

                // Display completed sentence
                if (promptTMP != null && currentQuestion.acceptableExactMatches != null && currentQuestion.acceptableExactMatches.Length > 0)
                {
                    string completedWord = currentQuestion.acceptableExactMatches[0];
                    promptTMP.text = currentQuestion.incomingMessageText.Replace("_____", $"<color=#2ECC71><b>{completedWord}</b></color>");
                }

                // Remove word from bank since question is resolved
                if (currentQuestion.acceptableExactMatches != null && currentQuestion.acceptableExactMatches.Length > 0)
                {
                    remainingWordBank.Remove(currentQuestion.acceptableExactMatches[0]);
                    UpdateWordBankDisplay();
                }

                if (currentQuestion.correctAudio != null && Masters_AudioManager.Instance != null)
                {
                    Masters_AudioManager.Instance.PlayVoiceOver(currentQuestion.correctAudio);
                }

                StartCoroutine(NextQuestionRoutine());
            }
        }
    }

    private void OnCheckButtonClicked()
    {
        if (isTransitioning) return;
        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
        if (inputField == null) return;
        SubmitAnswer(inputField.text);
    }

    private IEnumerator RetryPromptRoutine()
    {
        yield return new WaitForSeconds(0.6f);
        if (inputFieldBackground != null) inputFieldBackground.color = defaultInputFieldColor;
        if (inputField != null)
        {
            inputField.text = "";
            inputField.interactable = true;
            inputField.Select();
            inputField.ActivateInputField();
        }
        if (checkButton != null) checkButton.interactable = true;
    }

    private IEnumerator NextQuestionRoutine()
    {
        if (currentQuestion != null && currentQuestion.correctAudio != null && Masters_AudioManager.Instance != null)
        {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        }
        else
        {
            yield return new WaitForSeconds(timeBetweenQuestions);
        }

        LoadQuestion(currentQuestionIndex + 1);
    }

    private void OnAllQuestionsCompleted()
    {
        // Evaluate pass threshold (>= 6/8)
        if (firstAttemptCorrectCount >= passThreshold)
        {
            M3A_U6_HubProgress.MarkW01Complete();
        }

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            NextButtonAnimation();
        }
    }

    protected override void OnNextButtonClicked()
    {
        if (firstAttemptCorrectCount >= passThreshold)
        {
            M3A_U6_HubProgress.MarkW01Complete();
        }

        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (nextLessonSO != null)
        {
            if (Masters_LevelManager.Instance != null)
            {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        }
        else
        {
            if (Masters_TopicSelectionManager.Instance != null)
            {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null)
            {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }

    private void OnBackClicked()
    {
        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        StopAllCoroutines();

        if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnBackButtonClicked();
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
    }
}
