using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Unit 6 Colour Your Speech — Writing L02 Controller.
/// Activity: "W02 — Use the Idiom in Your Own Sentence"
/// 
/// Mechanics:
/// - Student writes ONE original sentence using the target idiom correctly for the given situation.
/// - 3 Prompts:
///   1. On Cloud Nine (Meaning: extremely happy)
///   2. Piece of Cake (Meaning: very easy)
///   3. Go Bananas (Meaning: become very angry/crazy)
/// - Flexible, robust idiom detection supporting natural inflections (e.g. go/goes/went/will go/going bananas).
/// - Case-insensitive and whitespace-safe validation without grading unrelated words.
/// - Correct state: green visual, tick icon, SFX_MATCH, ARIA readback VO.
/// - Retry state: gentle guidance on wrong, allows exactly ONE retry without score inflation.
/// - Pass condition: >= 2 of 3 prompts passed.
/// - Progress: Marks M3A_U6_HubProgress.MarkW02Complete() and evaluates Writing branch completion.
/// - Returns cleanly to the Unit 6 Hub upon completion (nextLessonSO = null).
/// </summary>
public class Masters_ColourYourSpeech_Writing_LessonTwo : Masters_Lesson
{
    [System.Serializable]
    public class WritingPromptData
    {
        public string targetIdiom;               // e.g. "On Cloud Nine"
        public string idiomMeaning;              // e.g. "Extremely happy"
        public string situationScaffold;         // e.g. "The entire team was on cloud nine after winning the championship."
        public string[] validIdiomKeywords;      // Accepted variations (e.g. "cloud nine", "on cloud nine", "cloud 9")
        public AudioClip meaningAudioClip;       // Idiom Meaning confirmation audio (e.g. "On cloud nine means extremely happy.")
        public AudioClip scaffoldAudioClip;      // Situation Audio / Reveal Audio (e.g. "The entire team was on cloud nine...")
        public AudioClip correctReadbackAudio;   // Fallback reference
    }

    [Header("Writing Prompts (3 Prompts)")]
    [SerializeField] protected WritingPromptData[] prompts;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("UI Components")]
    [SerializeField] private TextMeshProUGUI promptHeaderTMP;
    [SerializeField] private TextMeshProUGUI scaffoldTMP;
    [SerializeField] private TextMeshProUGUI wordBankRailTMP;
    [SerializeField] private TMP_InputField studentInputField;
    [SerializeField] private Image inputFieldBackground;
    [SerializeField] private Button submitButton;
    [SerializeField] private TextMeshProUGUI progressCountTMP;
    [SerializeField] private TextMeshProUGUI lessonTitleTMP;
    [SerializeField] private Button backButton;
    [SerializeField] private GameObject correctTickObject;

    [Header("Settings & Visual Feedback")]
    [SerializeField] private float timeBetweenPrompts = 1.5f;
    [SerializeField] private Color defaultInputColor = Color.white;
    [SerializeField] private Color correctInputColor = new Color(0.18f, 0.8f, 0.44f, 1f);
    [SerializeField] private Color incorrectInputColor = new Color(0.9f, 0.3f, 0.23f, 1f);
    [SerializeField] private int passThreshold = 2;

    [Header("Audio")]
    [SerializeField] private AudioClip voAriaIntro;

    // Runtime state
    private int currentPromptIndex = 0;
    private WritingPromptData currentPrompt;
    private bool isTransitioning = false;
    private bool isFirstAttempt = true;
    private int firstAttemptCorrectCount = 0;

    public int FirstAttemptScore => firstAttemptCorrectCount;
    public int CurrentPromptIndex => currentPromptIndex;
    public bool IsW02Passed => firstAttemptCorrectCount >= passThreshold;

    protected override void Awake()
    {
        base.Awake();
        topic = Masters_Topic.Writing;

        FindAndAutoAssignUI();

        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmitClicked);
        }

        if (studentInputField != null)
        {
            studentInputField.onSubmit.RemoveAllListeners();
            studentInputField.onSubmit.AddListener(SubmitSentence);
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

        currentPromptIndex = 0;
        firstAttemptCorrectCount = 0;
        isTransitioning = false;
        isFirstAttempt = true;

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
            lessonTitleTMP.text = "W02 — Use the Idiom in Your Own Sentence";
        }

        if (promptHeaderTMP == null)
        {
            Transform promptTrans = transform.Find("PromptHeader") ?? transform.Find("Prompt") ?? FindChildRecursive(transform, "Prompt");
            if (promptTrans != null)
            {
                promptHeaderTMP = promptTrans.GetComponent<TextMeshProUGUI>() ?? promptTrans.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (scaffoldTMP == null)
        {
            Transform scafTrans = transform.Find("ScaffoldText") ?? transform.Find("Cloud/Text") ?? FindChildRecursive(transform, "ScaffoldText");
            if (scafTrans != null)
            {
                scaffoldTMP = scafTrans.GetComponent<TextMeshProUGUI>() ?? scafTrans.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (wordBankRailTMP == null)
        {
            Transform railTrans = transform.Find("WordBankRail") ?? transform.Find("HintContainer/Hint text") ?? transform.Find("Hint text") ?? FindChildRecursive(transform, "WordBankRail");
            if (railTrans != null)
            {
                wordBankRailTMP = railTrans.GetComponent<TextMeshProUGUI>() ?? railTrans.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (studentInputField == null)
        {
            Transform inputTrans = transform.Find("Player inputfield") ?? transform.Find("StudentInputField") ?? FindChildRecursive(transform, "Player inputfield");
            if (inputTrans != null)
            {
                studentInputField = inputTrans.GetComponent<TMP_InputField>();
                if (inputFieldBackground == null) inputFieldBackground = inputTrans.GetComponent<Image>();
            }
        }

        if (submitButton == null)
        {
            Transform checkTrans = transform.Find("CheckButton") ?? transform.Find("SubmitButton") ?? FindChildRecursive(transform, "CheckButton");
            if (checkTrans != null) submitButton = checkTrans.GetComponent<Button>();
        }

        if (progressCountTMP == null)
        {
            Transform progTrans = transform.Find("progression count") ?? FindChildRecursive(transform, "progression count");
            if (progTrans != null)
            {
                progressCountTMP = progTrans.GetComponent<TextMeshProUGUI>() ?? progTrans.GetComponentInChildren<TextMeshProUGUI>();
            }
        }

        if (backButton == null)
        {
            Transform backTrans = transform.Find("BackButton") ?? FindChildRecursive(transform, "BackButton");
            if (backTrans != null) backButton = backTrans.GetComponent<Button>();
        }

        if (correctTickObject == null)
        {
            Transform tickTrans = transform.Find("Player inputfield/CorrectTick") ?? transform.Find("CorrectTick") ?? FindChildRecursive(transform, "CorrectTick");
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

        LoadPrompt(0);
    }

    public void LoadPrompt(int index)
    {
        if (prompts == null || index < 0 || index >= prompts.Length)
        {
            OnAllPromptsCompleted();
            return;
        }

        currentPromptIndex = index;
        currentPrompt = prompts[index];
        isTransitioning = false;
        isFirstAttempt = true;

        if (progressCountTMP != null) progressCountTMP.text = $"{index + 1}/{prompts.Length}";
        if (correctTickObject != null) correctTickObject.SetActive(false);

        // Header: Idiom & Meaning
        if (promptHeaderTMP != null)
        {
            promptHeaderTMP.text = $"Write a sentence with: <color=#F39C12><b>\"{currentPrompt.targetIdiom}\"</b></color>";
        }

        // Scaffold / Situation Example
        if (scaffoldTMP != null)
        {
            scaffoldTMP.text = $"<b>Meaning:</b> {currentPrompt.idiomMeaning}\n<size=80%><i>Example: \"{currentPrompt.situationScaffold}\"</i></size>";
        }

        // Word Bank Rail: Idiom + Meaning Assistance
        if (wordBankRailTMP != null)
        {
            wordBankRailTMP.text = $"<b>Idiom:</b> {currentPrompt.targetIdiom}   |   <b>Meaning:</b> {currentPrompt.idiomMeaning}";
            if (wordBankRailTMP.transform.parent != null)
            {
                wordBankRailTMP.transform.parent.gameObject.SetActive(true);
            }
        }

        if (studentInputField != null)
        {
            studentInputField.text = "";
            studentInputField.interactable = true;
            studentInputField.readOnly = false;
            if (inputFieldBackground != null) inputFieldBackground.color = defaultInputColor;
            studentInputField.Select();
            studentInputField.ActivateInputField();
        }

        if (submitButton != null) submitButton.interactable = true;
    }

    public void SubmitSentence(string inputAnswer)
    {
        if (currentPrompt == null || isTransitioning) return;

        string userInput = (inputAnswer ?? "").Trim();
        if (string.IsNullOrEmpty(userInput)) return;

        bool isCorrect = ValidateStudentSentence(userInput, currentPrompt, out string failReason);

        if (isCorrect)
        {
            if (isFirstAttempt)
            {
                firstAttemptCorrectCount++;
            }

            isTransitioning = true;
            if (inputFieldBackground != null) inputFieldBackground.color = correctInputColor;
            if (studentInputField != null) studentInputField.interactable = false;
            if (submitButton != null) submitButton.interactable = false;
            if (correctTickObject != null) correctTickObject.SetActive(true);

            if (Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                AudioClip clipToPlay = currentPrompt.meaningAudioClip != null ? currentPrompt.meaningAudioClip : currentPrompt.correctReadbackAudio;
                if (clipToPlay != null)
                {
                    Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
                }
            }

            StartCoroutine(NextPromptRoutine());
        }
        else
        {
            // Wrong submission
            if (inputFieldBackground != null) inputFieldBackground.color = incorrectInputColor;
            if (correctTickObject != null) correctTickObject.SetActive(false);
            if (Masters_AudioManager.Instance != null)
            {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (isFirstAttempt)
            {
                // Allow exactly ONE retry
                isFirstAttempt = false;
                StartCoroutine(RetryPromptRoutine(failReason));
            }
            else
            {
                // Second wrong: Reveal guidance scaffold and advance cleanly
                isTransitioning = true;
                if (studentInputField != null)
                {
                    studentInputField.text = currentPrompt.situationScaffold;
                    studentInputField.interactable = false;
                }
                if (submitButton != null) submitButton.interactable = false;

                if (Masters_AudioManager.Instance != null)
                {
                    AudioClip scaffoldClip = currentPrompt.scaffoldAudioClip != null ? currentPrompt.scaffoldAudioClip : currentPrompt.correctReadbackAudio;
                    if (scaffoldClip != null)
                    {
                        Masters_AudioManager.Instance.PlayVoiceOver(scaffoldClip);
                    }
                }

                StartCoroutine(NextPromptRoutine());
            }
        }
    }

    public bool ValidateStudentSentence(string userInput, WritingPromptData promptData, out string failReason)
    {
        failReason = "";
        if (string.IsNullOrEmpty(userInput) || promptData == null)
        {
            failReason = "Please write a sentence.";
            return false;
        }

        string normalized = userInput.ToLowerInvariant().Trim();
        string[] words = normalized.Split(new char[] { ' ', '.', ',', '!', '?', '\n', '\r', '\t', ';', ':', '-', '"' }, StringSplitOptions.RemoveEmptyEntries);

        // Check minimum sentence length
        if (words.Length < 3)
        {
            failReason = "Please write a complete sentence with at least 3 words.";
            return false;
        }

        // Validate that target idiom (or one of its inflection variations) is present
        bool containsIdiom = false;
        if (promptData.validIdiomKeywords != null && promptData.validIdiomKeywords.Length > 0)
        {
            foreach (string keyword in promptData.validIdiomKeywords)
            {
                if (!string.IsNullOrEmpty(keyword))
                {
                    string normKeyword = keyword.ToLowerInvariant().Trim();
                    if (normalized.Contains(normKeyword))
                    {
                        containsIdiom = true;
                        break;
                    }
                }
            }
        }
        else if (!string.IsNullOrEmpty(promptData.targetIdiom))
        {
            if (normalized.Contains(promptData.targetIdiom.ToLowerInvariant().Trim()))
            {
                containsIdiom = true;
            }
        }

        if (!containsIdiom)
        {
            failReason = $"Make sure your sentence includes the idiom '{promptData.targetIdiom}'.";
            return false;
        }

        return true;
    }

    private void OnSubmitClicked()
    {
        if (isTransitioning) return;
        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
        if (studentInputField == null) return;
        SubmitSentence(studentInputField.text);
    }

    private IEnumerator RetryPromptRoutine(string failReason)
    {
        yield return new WaitForSeconds(0.8f);
        if (inputFieldBackground != null) inputFieldBackground.color = defaultInputColor;
        if (studentInputField != null)
        {
            studentInputField.interactable = true;
            studentInputField.Select();
            studentInputField.ActivateInputField();
        }
        if (submitButton != null) submitButton.interactable = true;
    }

    private IEnumerator NextPromptRoutine()
    {
        if (currentPrompt != null && currentPrompt.correctReadbackAudio != null && Masters_AudioManager.Instance != null)
        {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        }
        else
        {
            yield return new WaitForSeconds(timeBetweenPrompts);
        }

        LoadPrompt(currentPromptIndex + 1);
    }

    private void OnAllPromptsCompleted()
    {
        // Pass condition: >= 2 of 3 prompts
        if (firstAttemptCorrectCount >= passThreshold)
        {
            M3A_U6_HubProgress.MarkW02Complete();
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
            M3A_U6_HubProgress.MarkW02Complete();
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
