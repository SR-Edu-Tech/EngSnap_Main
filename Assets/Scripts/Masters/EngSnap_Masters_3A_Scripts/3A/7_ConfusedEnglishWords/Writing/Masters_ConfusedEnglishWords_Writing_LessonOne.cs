using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.ConfusedWords;

/// <summary>
/// Unit 7 Writing Lesson One (W01 — Case Report Word Typing).
/// Students type the missing confused word into the input field with whitespace/case normalization.
/// </summary>
public class Masters_ConfusedEnglishWords_Writing_LessonOne : Masters_Lesson
{
    [Serializable]
    public class WritingFillItem
    {
        public string reportTitle;
        public string sentenceWithGap;
        public string expectedWord;
        public string hintText;
    }

    [Header("UI References")]
    [SerializeField] private TMP_Text reportHeaderTMP;
    [SerializeField] private TMP_Text sentenceTMP;
    [SerializeField] private TMP_InputField answerInputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_Text feedbackTMP;
    [SerializeField] private Button backButton;

    [Header("Visual Feedback")]
    [SerializeField] private Image inputFieldBackground;
    [SerializeField] private Color defaultInputColor = Color.white;
    [SerializeField] private Color correctInputColor = new Color(0.75f, 0.95f, 0.75f, 1f);
    [SerializeField] private Color incorrectInputColor = new Color(0.95f, 0.75f, 0.75f, 1f);

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Content")]

    [SerializeField] private List<WritingFillItem> items = new List<WritingFillItem>();

    private int currentIndex = 0;
    private bool isAnswered = false;

    protected override void Awake()
    {
        topic = Masters_Topic.Writing;

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

        if (submitButton != null)
        {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmitClicked);
        }

        if (answerInputField != null)
        {
            answerInputField.onSubmit.RemoveAllListeners();
            answerInputField.onSubmit.AddListener(_ => OnSubmitClicked());
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
        items = new List<WritingFillItem>
        {
            new WritingFillItem { reportTitle = "Report Entry #1", sentenceWithGap = "Be ____, please.", expectedWord = "QUIET", hintText = "Hint: 'no noise, silent'" },
            new WritingFillItem { reportTitle = "Report Entry #2", sentenceWithGap = "It is ____ cold today.", expectedWord = "QUITE", hintText = "Hint: 'not exactly, not perfectly'" },
            new WritingFillItem { reportTitle = "Report Entry #3", sentenceWithGap = "I write in my ____ every night.", expectedWord = "DIARY", hintText = "Hint: daily personal record book" },
            new WritingFillItem { reportTitle = "Report Entry #4", sentenceWithGap = "____ products are made from milk.", expectedWord = "DAIRY", hintText = "Hint: animal milk products" },
            new WritingFillItem { reportTitle = "Report Entry #5", sentenceWithGap = "____ arrives first will win the case.", expectedWord = "WHOEVER", hintText = "Hint: subject position (he/she fits)" }
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

        if (reportHeaderTMP != null) reportHeaderTMP.text = $"{item.reportTitle} ({currentIndex + 1}/{items.Count})";
        if (sentenceTMP != null) sentenceTMP.text = item.sentenceWithGap;

        if (answerInputField != null)
        {
            answerInputField.text = "";
            answerInputField.interactable = true;
            answerInputField.ActivateInputField();
        }

        if (inputFieldBackground != null) inputFieldBackground.color = defaultInputColor;
        if (feedbackTMP != null) feedbackTMP.text = item.hintText;
        if (submitButton != null) submitButton.interactable = true;
        if (nextButton != null) nextButton.gameObject.SetActive(false);
    }

    private void OnSubmitClicked()
    {
        if (isAnswered || answerInputField == null) return;

        string rawInput = answerInputField.text;
        string normalizedInput = rawInput.Trim().ToUpperInvariant();

        if (string.IsNullOrEmpty(normalizedInput))
        {
            if (feedbackTMP != null) feedbackTMP.text = "Please type a word before submitting!";
            return;
        }

        isAnswered = true;
        var item = items[currentIndex];
        bool isCorrect = normalizedInput.Equals(item.expectedWord, StringComparison.OrdinalIgnoreCase);

        if (inputFieldBackground != null)
        {
            inputFieldBackground.color = isCorrect ? correctInputColor : incorrectInputColor;
        }

        if (answerInputField != null) answerInputField.interactable = false;
        if (submitButton != null) submitButton.interactable = false;

        if (Masters_AudioManager.Instance != null)
        {
            if (!M3A_U7_HubProgress.IsIntroComplete())
            {
                Debug.LogWarning("[U7 AUDIO BLOCKED] Writing_LessonOne attempted audio before Intro completion");
            }
            else
            {
                Masters_AudioManager.Instance.PlaySoundEffect(isCorrect ? Masters_SFX.Correct : Masters_SFX.Incorrect);
            }
        }


        if (feedbackTMP != null)
        {
            feedbackTMP.text = isCorrect ? $"Correctly Typed: {item.expectedWord}!" : $"Filed correction: {item.expectedWord}";
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
        if (feedbackTMP != null) feedbackTMP.text = "W01 Word Typing Completed Successfully!";
        M3A_U7_HubProgress.MarkW01Complete();

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }
    }

    protected override void OnNextButtonClicked()
    {
        M3A_U7_HubProgress.MarkW01Complete();

        if (nextLessonSO != null && Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        }
        else if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ShowWriting(2);
        }
        else if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Writing);
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
