using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.ConfusedWords;

/// <summary>
/// Unit 7 Reading Lesson One (R01 — Case File Context Reading).
/// Students read contextual sentences and choose which confused word belongs in the blank.
/// </summary>
public class Masters_ConfusedEnglishWords_Reading_LessonOne : Masters_Lesson
{
    [Serializable]
    public class ReadingContextItem
    {
        public string sentenceWithGap;
        public string wordA;
        public string wordB;
        public int correctIndex; // 0 for A, 1 for B
        public string fullCorrectSentence;
        public string explanation;
    }

    [Header("UI References")]
    [SerializeField] private TMP_Text caseHeaderTMP;
    [SerializeField] private TMP_Text sentenceTMP;
    [SerializeField] private TMP_Text feedbackTMP;
    [SerializeField] private Button optionAButton;
    [SerializeField] private Button optionBButton;
    [SerializeField] private TMP_Text optionAText;
    [SerializeField] private TMP_Text optionBText;
    [SerializeField] private Image optionAImage;
    [SerializeField] private Image optionBImage;
    [SerializeField] private Button backButton;
    [SerializeField] private Color defaultCardColor = new Color(0.95f, 0.92f, 0.85f, 1f);
    [SerializeField] private Color correctCardColor = new Color(0.4f, 0.85f, 0.4f, 1f);
    [SerializeField] private Color incorrectCardColor = new Color(0.95f, 0.4f, 0.4f, 1f);

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Content")]

    [SerializeField] private List<ReadingContextItem> items = new List<ReadingContextItem>();

    private int currentIndex = 0;
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
    }

    protected override void Start()
    {
        base.Start();
        currentIndex = 0;
        LoadCurrentItem();
    }

    private void InitializeDefaultItems()
    {
        items = new List<ReadingContextItem>
        {
            new ReadingContextItem
            {
                sentenceWithGap = "The library is very ____.",
                wordA = "QUIET",
                wordB = "QUITE",
                correctIndex = 0,
                fullCorrectSentence = "The library is very quiet.",
                explanation = "QUIET means silent / free of noise."
            },
            new ReadingContextItem
            {
                sentenceWithGap = "I am ____ sure about the answer.",
                wordA = "QUIET",
                wordB = "QUITE",
                correctIndex = 1,
                fullCorrectSentence = "I am quite sure about the answer.",
                explanation = "QUITE means not exactly / to a certain degree."
            },
            new ReadingContextItem
            {
                sentenceWithGap = "I write in my ____ every night.",
                wordA = "DIARY",
                wordB = "DAIRY",
                correctIndex = 0,
                fullCorrectSentence = "I write in my diary every night.",
                explanation = "DIARY is a book for recording daily experiences."
            },
            new ReadingContextItem
            {
                sentenceWithGap = "Butter and cheese are ____ products.",
                wordA = "DIARY",
                wordB = "DAIRY",
                correctIndex = 1,
                fullCorrectSentence = "Butter and cheese are dairy products.",
                explanation = "DAIRY products come from animal milk."
            },
            new ReadingContextItem
            {
                sentenceWithGap = "____ arrives first will get the prize.",
                wordA = "WHOEVER",
                wordB = "WHOMEVER",
                correctIndex = 0,
                fullCorrectSentence = "Whoever arrives first will get the prize.",
                explanation = "WHOEVER stands in the subject position (he/she arrives)."
            },
            new ReadingContextItem
            {
                sentenceWithGap = "Give the ticket to ____ you choose.",
                wordA = "WHOEVER",
                wordB = "WHOMEVER",
                correctIndex = 1,
                fullCorrectSentence = "Give the ticket to whomever you choose.",
                explanation = "WHOMEVER stands in the object position (you choose him/her)."
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
        var item = items[currentIndex];

        if (caseHeaderTMP != null) caseHeaderTMP.text = $"Case File Evidence: {currentIndex + 1}/{items.Count}";
        if (sentenceTMP != null) sentenceTMP.text = item.sentenceWithGap;
        if (optionAText != null) optionAText.text = item.wordA;
        if (optionBText != null) optionBText.text = item.wordB;

        ResetCardStyles();

        if (feedbackTMP != null) feedbackTMP.text = "Read the context sentence and select the correct word.";
        if (nextButton != null) nextButton.gameObject.SetActive(false);
    }

    private void ResetCardStyles()
    {
        if (optionAImage != null) optionAImage.color = defaultCardColor;
        if (optionBImage != null) optionBImage.color = defaultCardColor;
        if (optionAButton != null) optionAButton.interactable = true;
        if (optionBButton != null) optionBButton.interactable = true;
    }

    private void OnOptionSelected(int selectedIndex)
    {
        if (isAnswered) return;
        isAnswered = true;

        var item = items[currentIndex];
        bool isCorrect = (selectedIndex == item.correctIndex);

        if (selectedIndex == 0 && optionAImage != null) optionAImage.color = isCorrect ? correctCardColor : incorrectCardColor;
        if (selectedIndex == 1 && optionBImage != null) optionBImage.color = isCorrect ? correctCardColor : incorrectCardColor;

        if (!isCorrect)
        {
            if (item.correctIndex == 0 && optionAImage != null) optionAImage.color = correctCardColor;
            if (item.correctIndex == 1 && optionBImage != null) optionBImage.color = correctCardColor;
        }

        if (optionAButton != null) optionAButton.interactable = false;
        if (optionBButton != null) optionBButton.interactable = false;

        if (sentenceTMP != null) sentenceTMP.text = item.fullCorrectSentence;

        if (Masters_AudioManager.Instance != null)
        {
            if (!M3A_U7_HubProgress.IsIntroComplete())
            {
                Debug.LogWarning("[U7 AUDIO BLOCKED] Reading_LessonOne attempted audio before Intro completion");
            }
            else
            {
                Masters_AudioManager.Instance.PlaySoundEffect(isCorrect ? Masters_SFX.Correct : Masters_SFX.Incorrect);
            }
        }


        if (feedbackTMP != null)
        {
            feedbackTMP.text = isCorrect ? $"Correct! {item.explanation}" : $"Evidence: {item.explanation}";
        }

        StartCoroutine(AdvanceAfterDelay(1.8f));
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
        if (feedbackTMP != null) feedbackTMP.text = "R01 Context Analysis Complete!";
        M3A_U7_HubProgress.MarkR01Complete();

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }
    }

    protected override void OnNextButtonClicked()
    {
        M3A_U7_HubProgress.MarkR01Complete();

        if (nextLessonSO != null && Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        }
        else if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ShowReading(2);
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
