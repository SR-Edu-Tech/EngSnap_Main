using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.ConfusedWords;

/// <summary>
/// Unit 7 Writing Lesson Two (W02 — Meaning / Definition Writing Matching).
/// Students type the exact authoritative confused English word that matches the given definition.
/// Completing W02 (with W01) marks the Writing branch complete (3/6).
/// </summary>
public class Masters_ConfusedEnglishWords_Writing_LessonTwo : Masters_Lesson
{
    [Serializable]
    public class DefinitionWriteItem
    {
        public string definitionPrompt;
        public string expectedWord;
        public string pairHint;
    }

    [Header("UI References")]
    [SerializeField] private TMP_Text taskHeaderTMP;
    [SerializeField] private TMP_Text definitionPromptTMP;
    [SerializeField] private TMP_InputField answerInputField;
    [SerializeField] private Button submitButton;
    [SerializeField] private TMP_Text feedbackTMP;
    [SerializeField] private Button backButton;

    [Header("Visual Feedback")]
    [SerializeField] private Image inputFieldBackground;
    [SerializeField] private Color defaultInputColor = Color.white;
    [SerializeField] private Color correctInputColor = new Color(0.75f, 0.95f, 0.75f, 1f);
    [SerializeField] private Color incorrectInputColor = new Color(0.95f, 0.75f, 0.75f, 1f);

    [Header("Content")]
    [SerializeField] private List<DefinitionWriteItem> items = new List<DefinitionWriteItem>();

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
        items = new List<DefinitionWriteItem>
        {
            new DefinitionWriteItem { definitionPrompt = "Type the word that means 'no noise, silent':", expectedWord = "QUIET", pairHint = "Pair: QUIET / QUITE" },
            new DefinitionWriteItem { definitionPrompt = "Type the word that means 'not exactly, not perfectly':", expectedWord = "QUITE", pairHint = "Pair: QUIET / QUITE" },
            new DefinitionWriteItem { definitionPrompt = "Type the word for 'a book that you write daily events, records, experiences, etc.':", expectedWord = "DIARY", pairHint = "Pair: DIARY / DAIRY" },
            new DefinitionWriteItem { definitionPrompt = "Type the word for 'A product that comes from animal milk':", expectedWord = "DAIRY", pairHint = "Pair: DIARY / DAIRY" },
            new DefinitionWriteItem { definitionPrompt = "Type the pronoun that 'stands in the position of subject':", expectedWord = "WHOEVER", pairHint = "Pair: WHOEVER / WHOMEVER (he/she fits)" },
            new DefinitionWriteItem { definitionPrompt = "Type the pronoun that 'stands in the position of an object':", expectedWord = "WHOMEVER", pairHint = "Pair: WHOEVER / WHOMEVER (him/her fits)" }
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

        if (taskHeaderTMP != null) taskHeaderTMP.text = $"Definition Case Task: {currentIndex + 1}/{items.Count}";
        if (definitionPromptTMP != null) definitionPromptTMP.text = item.definitionPrompt;

        if (answerInputField != null)
        {
            answerInputField.text = "";
            answerInputField.interactable = true;
            answerInputField.ActivateInputField();
        }

        if (inputFieldBackground != null) inputFieldBackground.color = defaultInputColor;
        if (feedbackTMP != null) feedbackTMP.text = item.pairHint;
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
            if (feedbackTMP != null) feedbackTMP.text = "Please type an answer!";
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
                Debug.LogWarning("[U7 AUDIO BLOCKED] Writing_LessonTwo attempted audio before Intro completion");
            }
            else
            {
                Masters_AudioManager.Instance.PlaySoundEffect(isCorrect ? Masters_SFX.Correct : Masters_SFX.Incorrect);
            }
        }


        if (feedbackTMP != null)
        {
            feedbackTMP.text = isCorrect ? $"Correct! {item.expectedWord}" : $"Authoritative Word: {item.expectedWord}";
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
        if (feedbackTMP != null) feedbackTMP.text = "W02 Definition Writing Complete! Writing Branch Solved (3/6).";
        M3A_U7_HubProgress.MarkW02Complete();

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }
    }

    protected override void OnNextButtonClicked()
    {
        M3A_U7_HubProgress.MarkW02Complete();

        if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ReturnToHub();
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
