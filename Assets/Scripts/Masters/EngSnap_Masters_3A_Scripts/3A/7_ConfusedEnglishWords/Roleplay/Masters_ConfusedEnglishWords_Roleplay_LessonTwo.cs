using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.ConfusedWords;

/// <summary>
/// Unit 7 Roleplay Lesson Two (RP02 — Free Scene: Report Your Own Case / Final Clues).
/// Student reports two-turn case files using both twin words of each pair correctly.
/// </summary>
public class Masters_ConfusedEnglishWords_Roleplay_LessonTwo : Masters_Lesson
{
    [Serializable]
    public class CaseReport
    {
        public string caseName;
        public AudioClip setupAudio;
        public string turnOnePrompt;
        public string turnOneCorrect;
        public string turnOneDistractor;
        public string turnTwoPrompt;
        public string turnTwoCorrect;
        public string turnTwoDistractor;
        public bool acceptsEitherOnTurnTwo;
        public AudioClip readbackAudio;
        public string summaryNote;
    }


    [Header("UI References")]
    [SerializeField] private TMP_Text caseHeaderTMP;
    [SerializeField] private TMP_Text turnPromptTMP;
    [SerializeField] private TMP_Text instructionTMP;
    [SerializeField] private TMP_Text feedbackTMP;
    [SerializeField] private Button choiceAButton;
    [SerializeField] private Button choiceBButton;
    [SerializeField] private TMP_Text choiceAText;
    [SerializeField] private TMP_Text choiceBText;
    [SerializeField] private Image choiceAImage;
    [SerializeField] private Image choiceBImage;
    [SerializeField] private Button backButton;

    [Header("Colors")]
    [SerializeField] private Color defaultCardColor = new Color(0.95f, 0.92f, 0.85f, 1f);
    [SerializeField] private Color correctCardColor = new Color(0.4f, 0.85f, 0.4f, 1f);
    [SerializeField] private Color incorrectCardColor = new Color(0.95f, 0.4f, 0.4f, 1f);

    [Header("Case Data")]
    [SerializeField] private List<CaseReport> cases = new List<CaseReport>();

    private int currentCaseIndex = 0;
    private int currentTurn = 1; // 1 or 2
    private int correctTurns = 0;
    private bool isAnswered = false;
    private int correctChoiceIndex = 0;

    protected override void Awake()
    {
        topic = Masters_Topic.Roleplay;

        if (cases == null || cases.Count == 0)
        {
            InitializeDefaultCases();
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

        if (choiceAButton != null)
        {
            choiceAButton.onClick.RemoveAllListeners();
            choiceAButton.onClick.AddListener(() => OnChoiceSelected(0));
        }

        if (choiceBButton != null)
        {
            choiceBButton.onClick.RemoveAllListeners();
            choiceBButton.onClick.AddListener(() => OnChoiceSelected(1));
        }
    }

    protected override void Start()
    {
        base.Start();
        currentCaseIndex = 0;
        currentTurn = 1;
        correctTurns = 0;
        LoadCurrentTurn();
    }

    private void InitializeDefaultCases()
    {
        cases = new List<CaseReport>
        {
            new CaseReport
            {
                caseName = "Case Card A: QUIET & QUITE",
                turnOnePrompt = "Turn 1: Report the silent scene:",
                turnOneCorrect = "The library was very quiet.",
                turnOneDistractor = "The library was very quite.",
                turnTwoPrompt = "Turn 2: Report the detective plan status:",
                turnTwoCorrect = "But the plan was not quite right.",
                turnTwoDistractor = "But the plan was not quiet right.",
                acceptsEitherOnTurnTwo = false,
                summaryNote = "QUIET = silent; QUITE = not perfectly."
            },
            new CaseReport
            {
                caseName = "Case Card B: DIARY & DAIRY",
                turnOnePrompt = "Turn 1: Report the journal record:",
                turnOneCorrect = "She will not let me read her diary.",
                turnOneDistractor = "She will not let me read her dairy.",
                turnTwoPrompt = "Turn 2: Report the milk products found in kitchen:",
                turnTwoCorrect = "We found dairy products in the kitchen.",
                turnTwoDistractor = "We found diary products in the kitchen.",
                acceptsEitherOnTurnTwo = false,
                summaryNote = "DIARY = record book; DAIRY = milk products."
            },
            new CaseReport
            {
                caseName = "Case Card C: WHOEVER & WHOMEVER",
                turnOnePrompt = "Turn 1: Report the subject rule:",
                turnOneCorrect = "Whoever leaves last should lock the door.",
                turnOneDistractor = "Whomever leaves last should lock the door.",
                turnTwoPrompt = "Turn 2: Report the letter recipient (or book acceptance):",
                turnTwoCorrect = "Give the letter to whomever you see first.",
                turnTwoDistractor = "She always smiles at whoever she meets.",
                acceptsEitherOnTurnTwo = true,
                summaryNote = "WHOEVER = subject; WHOMEVER = object (both accepted for 'smiles at whoever/whomever')."
            }
        };
    }

    private void LoadCurrentTurn()
    {
        if (currentCaseIndex >= cases.Count)
        {
            OnAllCasesReported();
            return;
        }

        isAnswered = false;
        var c = cases[currentCaseIndex];

        if (caseHeaderTMP != null) caseHeaderTMP.text = $"{c.caseName} (Step {currentTurn}/2)";
        if (turnPromptTMP != null) turnPromptTMP.text = (currentTurn == 1) ? c.turnOnePrompt : c.turnTwoPrompt;
        if (instructionTMP != null) instructionTMP.text = "Select the sentence with the correct twin word:";

        string correctStr = (currentTurn == 1) ? c.turnOneCorrect : c.turnTwoCorrect;
        string distractorStr = (currentTurn == 1) ? c.turnOneDistractor : c.turnTwoDistractor;

        // Alternate button positions
        correctChoiceIndex = (currentCaseIndex + currentTurn) % 2;
        if (correctChoiceIndex == 0)
        {
            if (choiceAText != null) choiceAText.text = correctStr;
            if (choiceBText != null) choiceBText.text = distractorStr;
        }
        else
        {
            if (choiceAText != null) choiceAText.text = distractorStr;
            if (choiceBText != null) choiceBText.text = correctStr;
        }

        ResetButtonColors();

        if (feedbackTMP != null) feedbackTMP.text = "File the case report turn using the right twin.";
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        PlaySetupAudio();
    }

    private void PlaySetupAudio()
    {
        if (!M3A_U7_HubProgress.IsIntroComplete())
        {
            Debug.LogWarning("[U7 AUDIO BLOCKED] Roleplay_LessonTwo attempted audio before Intro completion");
            return;
        }

        if (currentCaseIndex < cases.Count && cases[currentCaseIndex].setupAudio != null)
        {
            if (Masters_AudioManager.Instance != null)
            {
                Debug.Log("[U7 FLOW] RP02 setup audio started");
                Masters_AudioManager.Instance.PlayVoiceOver(cases[currentCaseIndex].setupAudio);
            }
        }
    }


    private void ResetButtonColors()
    {
        if (choiceAImage != null) choiceAImage.color = defaultCardColor;
        if (choiceBImage != null) choiceBImage.color = defaultCardColor;
    }

    public void OnChoiceSelected(int optionIndex)
    {
        if (isAnswered || currentCaseIndex >= cases.Count) return;

        isAnswered = true;
        var c = cases[currentCaseIndex];
        bool isCorrect = (optionIndex == correctChoiceIndex) || (currentTurn == 2 && c.acceptsEitherOnTurnTwo);

        if (isCorrect)
        {
            correctTurns++;
            if (optionIndex == 0 && choiceAImage != null) choiceAImage.color = correctCardColor;
            if (optionIndex == 1 && choiceBImage != null) choiceBImage.color = correctCardColor;

            if (feedbackTMP != null) feedbackTMP.text = $"<color=#50AA5A>REPORT ACCEPTED! {c.summaryNote}</color>";
        }
        else
        {
            if (optionIndex == 0 && choiceAImage != null) choiceAImage.color = incorrectCardColor;
            if (optionIndex == 1 && choiceBImage != null) choiceBImage.color = incorrectCardColor;

            if (correctChoiceIndex == 0 && choiceAImage != null) choiceAImage.color = correctCardColor;
            if (correctChoiceIndex == 1 && choiceBImage != null) choiceBImage.color = correctCardColor;

            if (feedbackTMP != null) feedbackTMP.text = $"<color=#E05050>REVISE REPORT: {c.summaryNote}</color>";
        }

        StartCoroutine(AutoAdvanceTurn());
    }

    private IEnumerator AutoAdvanceTurn()
    {
        yield return new WaitForSeconds(1.6f);
        if (currentTurn == 1)
        {
            currentTurn = 2;
            LoadCurrentTurn();
        }
        else
        {
            currentTurn = 1;
            currentCaseIndex++;
            LoadCurrentTurn();
        }
    }

    private void OnAllCasesReported()
    {
        M3A_U7_HubProgress.MarkRP02Complete();

        if (turnPromptTMP != null) turnPromptTMP.text = "ALL CASE REPORTS FILED!";
        if (feedbackTMP != null) feedbackTMP.text = $"<color=#FFD750>CASE CLOSED! Completed {correctTurns}/6 report turns successfully.</color>";

        if (nextButton != null) nextButton.gameObject.SetActive(true);
    }

    protected override void OnNextButtonClicked()
    {
        if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ReturnToHub();
        }
    }



    private void OnBackClicked()
    {
        if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ReturnToHub();
        }
    }
}
