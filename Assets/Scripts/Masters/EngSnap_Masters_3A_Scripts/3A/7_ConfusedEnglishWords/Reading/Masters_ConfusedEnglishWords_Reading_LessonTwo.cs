using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.ConfusedWords;

/// <summary>
/// Unit 7 Reading Lesson Two (R02 — The Book's Own Test: He/She vs Him/Her).
/// Students test the blank using the book's substitution rule:
/// if 'he/she' fits -> subject position -> WHOEVER
/// if 'him/her' fits -> object position -> WHOMEVER
/// </summary>
public class Masters_ConfusedEnglishWords_Reading_LessonTwo : Masters_Lesson
{
    [Serializable]
    public class SubstitutionTestItem
    {
        public string sentenceWithBlank;
        public string subjectSubstitution; // e.g. "he/she wants to join"
        public string objectSubstitution;  // e.g. "you invite him/her"
        public bool isSubject;              // true -> Subject (Whoever), false -> Object (Whomever)
        public string correctWord;          // "WHOEVER" or "WHOMEVER"
        public string testExplanation;
    }

    [Header("UI References")]
    [SerializeField] private TMP_Text titleTMP;
    [SerializeField] private TMP_Text ruleSummaryTMP;
    [SerializeField] private TMP_Text sentenceTMP;
    [SerializeField] private TMP_Text testPromptTMP;
    [SerializeField] private TMP_Text feedbackTMP;

    [Header("Substitution Choice Buttons (Step 1: Test pronoun)")]
    [SerializeField] private Button heSheButton;
    [SerializeField] private Button himHerButton;
    [SerializeField] private TMP_Text heSheText;
    [SerializeField] private TMP_Text himHerText;
    [SerializeField] private Image heSheImage;
    [SerializeField] private Image himHerImage;

    [Header("Final Word Choice Buttons (Step 2: Whoever vs Whomever)")]
    [SerializeField] private GameObject wordChoiceContainer;
    [SerializeField] private Button whoeverButton;
    [SerializeField] private Button whomeverButton;
    [SerializeField] private Image whoeverImage;
    [SerializeField] private Image whomeverImage;

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;
    [SerializeField] private Button backButton;

    [SerializeField] private Color defaultCardColor = new Color(0.95f, 0.92f, 0.85f, 1f);
    [SerializeField] private Color correctCardColor = new Color(0.4f, 0.85f, 0.4f, 1f);
    [SerializeField] private Color incorrectCardColor = new Color(0.95f, 0.4f, 0.4f, 1f);

    [Header("Content Items")]
    [SerializeField] private List<SubstitutionTestItem> items = new List<SubstitutionTestItem>();

    private int currentIndex = 0;
    private bool stepOneCompleted = false;
    private bool isAnswered = false;

    protected override void Awake()
    {
        topic = Masters_Topic.Reading;

        if (items == null || items.Count == 0)
        {
            InitializeDefaultItems();
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

        if (heSheButton != null)
        {
            heSheButton.onClick.RemoveAllListeners();
            heSheButton.onClick.AddListener(() => OnPronounTestSelected(true));
        }

        if (himHerButton != null)
        {
            himHerButton.onClick.RemoveAllListeners();
            himHerButton.onClick.AddListener(() => OnPronounTestSelected(false));
        }

        if (whoeverButton != null)
        {
            whoeverButton.onClick.RemoveAllListeners();
            whoeverButton.onClick.AddListener(() => OnFinalWordSelected("WHOEVER"));
        }

        if (whomeverButton != null)
        {
            whomeverButton.onClick.RemoveAllListeners();
            whomeverButton.onClick.AddListener(() => OnFinalWordSelected("WHOMEVER"));
        }
    }

    protected override void Start()
    {
        base.Start();
        currentIndex = 0;
        LoadCurrentItem();
    }

    private void InitializeDefaultItems()
    {
        items = new List<SubstitutionTestItem>
        {
            new SubstitutionTestItem
            {
                sentenceWithBlank = "____ wants to join the detective team is welcome.",
                subjectSubstitution = "HE/SHE wants to join",
                objectSubstitution = "HIM/HER wants to join",
                isSubject = true,
                correctWord = "WHOEVER",
                testExplanation = "'He/She wants to join' fits -> Subject position -> WHOEVER"
            },
            new SubstitutionTestItem
            {
                sentenceWithBlank = "You may invite ____ you like to the case briefing.",
                subjectSubstitution = "You like HE/SHE",
                objectSubstitution = "You like HIM/HER",
                isSubject = false,
                correctWord = "WHOMEVER",
                testExplanation = "'You like him/her' fits -> Object position -> WHOMEVER"
            },
            new SubstitutionTestItem
            {
                sentenceWithBlank = "____ solves the cipher first will get the magnifying glass.",
                subjectSubstitution = "HE/SHE solves the cipher",
                objectSubstitution = "HIM/HER solves the cipher",
                isSubject = true,
                correctWord = "WHOEVER",
                testExplanation = "'He/She solves' fits -> Subject position -> WHOEVER"
            },
            new SubstitutionTestItem
            {
                sentenceWithBlank = "The Chief will question ____ the junior detective arrested.",
                subjectSubstitution = "The detective arrested HE/SHE",
                objectSubstitution = "The detective arrested HIM/HER",
                isSubject = false,
                correctWord = "WHOMEVER",
                testExplanation = "'The detective arrested him/her' fits -> Object position -> WHOMEVER"
            }
        };
    }

    private void LoadCurrentItem()
    {
        if (currentIndex >= items.Count)
        {
            OnAllItemsComplete();
            return;
        }

        isAnswered = false;
        stepOneCompleted = false;
        var item = items[currentIndex];

        if (titleTMP != null) titleTMP.text = $"The Book's Substitution Test: Item {currentIndex + 1}/{items.Count}";
        if (ruleSummaryTMP != null) ruleSummaryTMP.text = "Rule: he/she fits -> WHOEVER | him/her fits -> WHOMEVER";
        if (sentenceTMP != null) sentenceTMP.text = item.sentenceWithBlank;
        if (testPromptTMP != null) testPromptTMP.text = "Step 1: Test the blank. Does 'he/she' or 'him/her' fit naturally?";

        ResetStyles();

        if (wordChoiceContainer != null) wordChoiceContainer.SetActive(false);
        if (feedbackTMP != null) feedbackTMP.text = "Select the fitting pronoun test.";
        if (nextButton != null) nextButton.gameObject.SetActive(false);
    }

    private void ResetStyles()
    {
        if (heSheImage != null) heSheImage.color = defaultCardColor;
        if (himHerImage != null) himHerImage.color = defaultCardColor;
        if (whoeverImage != null) whoeverImage.color = defaultCardColor;
        if (whomeverImage != null) whomeverImage.color = defaultCardColor;

        if (heSheButton != null) heSheButton.interactable = true;
        if (himHerButton != null) himHerButton.interactable = true;
        if (whoeverButton != null) whoeverButton.interactable = true;
        if (whomeverButton != null) whomeverButton.interactable = true;
    }

    private void OnPronounTestSelected(bool selectedHeShe)
    {
        if (stepOneCompleted) return;

        var item = items[currentIndex];
        bool isCorrect = (selectedHeShe == item.isSubject);

        if (selectedHeShe && heSheImage != null) heSheImage.color = isCorrect ? correctCardColor : incorrectCardColor;
        if (!selectedHeShe && himHerImage != null) himHerImage.color = isCorrect ? correctCardColor : incorrectCardColor;

        if (heSheButton != null) heSheButton.interactable = false;
        if (himHerButton != null) himHerButton.interactable = false;

        if (Masters_AudioManager.Instance != null)
        {
            if (!M3A_U7_HubProgress.IsIntroComplete())
            {
                Debug.LogWarning("[U7 AUDIO BLOCKED] Reading_LessonTwo attempted audio before Intro completion");
            }
            else
            {
                Masters_AudioManager.Instance.PlaySoundEffect(isCorrect ? Masters_SFX.Correct : Masters_SFX.Incorrect);
            }
        }

        stepOneCompleted = true;

        if (testPromptTMP != null) testPromptTMP.text = $"Step 2: Based on the test, choose WHOEVER (Subject) or WHOMEVER (Object):";
        if (feedbackTMP != null) feedbackTMP.text = isCorrect ? "Test fits! Now choose the correct pronoun." : $"Notice: {item.testExplanation}";

        if (wordChoiceContainer != null) wordChoiceContainer.SetActive(true);
    }

    private void OnFinalWordSelected(string selectedWord)
    {
        if (isAnswered) return;
        isAnswered = true;

        var item = items[currentIndex];
        bool isCorrect = selectedWord.Equals(item.correctWord, StringComparison.OrdinalIgnoreCase);

        if (selectedWord == "WHOEVER" && whoeverImage != null) whoeverImage.color = isCorrect ? correctCardColor : incorrectCardColor;
        if (selectedWord == "WHOMEVER" && whomeverImage != null) whomeverImage.color = isCorrect ? correctCardColor : incorrectCardColor;

        if (whoeverButton != null) whoeverButton.interactable = false;
        if (whomeverButton != null) whomeverButton.interactable = false;

        if (Masters_AudioManager.Instance != null)
        {
            if (!M3A_U7_HubProgress.IsIntroComplete())
            {
                Debug.LogWarning("[U7 AUDIO BLOCKED] Reading_LessonTwo attempted audio before Intro completion");
            }
            else
            {
                Masters_AudioManager.Instance.PlaySoundEffect(isCorrect ? Masters_SFX.Correct : Masters_SFX.Incorrect);
            }
        }


        if (feedbackTMP != null)
        {
            feedbackTMP.text = isCorrect ? $"Correct! {item.testExplanation}" : $"Rule: {item.testExplanation}";
        }

        StartCoroutine(AdvanceAfterDelay(2f));
    }

    private IEnumerator AdvanceAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        currentIndex++;
        if (currentIndex < items.Count)
        {
            LoadCurrentItem();
        }
        else
        {
            OnAllItemsComplete();
        }
    }

    private void OnAllItemsComplete()
    {
        if (feedbackTMP != null) feedbackTMP.text = "R02 The Book's Test Mastered!";
        M3A_U7_HubProgress.MarkR02Complete();

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }
    }

    protected override void OnNextButtonClicked()
    {
        M3A_U7_HubProgress.MarkR02Complete();

        if (nextLessonSO != null && Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        }
        else if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ShowReading(3);
        }
        else if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Reading);
        }
        else
        {
            gameObject.SetActive(false);
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
            Masters_LevelManager.Instance.OnBackButtonClicked();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
