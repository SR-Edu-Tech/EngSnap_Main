using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.ConfusedWords;

/// <summary>
/// Unit 7 Reading Lesson Three (R03 — Case File Error Spotting & Special Book Acceptance).
/// Students identify correct vs erroneous sentences and handle the special book acceptance rule:
/// "She always smiles at whoever she meets." and "She always smiles at whomever she meets." are BOTH accepted!
/// </summary>
public class Masters_ConfusedEnglishWords_Reading_LessonThree : Masters_Lesson
{
    [Serializable]
    public class ErrorSpottingItem
    {
        public string caseFileDossier;
        public string sentenceText;
        public bool isCorrectAsWritten;
        public bool acceptsEitherWhoeverOrWhomever;
        public string correctionNote;
    }

    [Header("UI References")]
    [SerializeField] private TMP_Text dossierHeaderTMP;
    [SerializeField] private TMP_Text sentenceTMP;
    [SerializeField] private TMP_Text feedbackTMP;
    [SerializeField] private Button correctButton;
    [SerializeField] private Button hasErrorButton;
    [SerializeField] private Image correctButtonImage;
    [SerializeField] private Image hasErrorButtonImage;
    [SerializeField] private Button backButton;

    [Header("Colors")]
    [SerializeField] private Color correctButtonDefaultColor = new Color(0.345f, 0.725f, 0.341f, 1f);
    [SerializeField] private Color hasErrorButtonDefaultColor = new Color(0.898f, 0.420f, 0.310f, 1f);
    [SerializeField] private Color passColor = new Color(0.25f, 0.9f, 0.25f, 1f);
    [SerializeField] private Color failColor = new Color(0.95f, 0.25f, 0.25f, 1f);

    [Header("Dossier Items")]
    [SerializeField] private List<ErrorSpottingItem> items = new List<ErrorSpottingItem>();

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

        if (correctButton != null)
        {
            correctButton.onClick.RemoveAllListeners();
            correctButton.onClick.AddListener(() => OnChoiceSelected(true));
        }

        if (hasErrorButton != null)
        {
            hasErrorButton.onClick.RemoveAllListeners();
            hasErrorButton.onClick.AddListener(() => OnChoiceSelected(false));
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
        items = new List<ErrorSpottingItem>
        {
            new ErrorSpottingItem
            {
                caseFileDossier = "Dossier #1: Library Report",
                sentenceText = "The student was asked to be quite in the examination hall.",
                isCorrectAsWritten = false,
                acceptsEitherWhoeverOrWhomever = false,
                correctionNote = "Error spotted! It should be QUIET (no noise), not QUITE."
            },
            new ErrorSpottingItem
            {
                caseFileDossier = "Dossier #2: Grocery Inventory",
                sentenceText = "We bought fresh dairy milk and cheese from the local store.",
                isCorrectAsWritten = true,
                acceptsEitherWhoeverOrWhomever = false,
                correctionNote = "Correct as written! DAIRY refers to animal milk products."
            },
            new ErrorSpottingItem
            {
                caseFileDossier = "Dossier #3: Special Book Evidence",
                sentenceText = "She always smiles at whoever she meets.",
                isCorrectAsWritten = true,
                acceptsEitherWhoeverOrWhomever = true,
                correctionNote = "Special Book Rule: 'She always smiles at whoever she meets' is accepted as correct."
            },
            new ErrorSpottingItem
            {
                caseFileDossier = "Dossier #4: Special Book Evidence Alternative",
                sentenceText = "She always smiles at whomever she meets.",
                isCorrectAsWritten = true,
                acceptsEitherWhoeverOrWhomever = true,
                correctionNote = "Special Book Rule: 'She always smiles at whomever she meets' is ALSO accepted as correct."
            },
            new ErrorSpottingItem
            {
                caseFileDossier = "Dossier #5: Personal Notebook",
                sentenceText = "I forgot to write my thoughts in my dairy today.",
                isCorrectAsWritten = false,
                acceptsEitherWhoeverOrWhomever = false,
                correctionNote = "Error spotted! A personal record book is a DIARY, not DAIRY."
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

        if (dossierHeaderTMP != null) dossierHeaderTMP.text = $"{item.caseFileDossier} ({currentIndex + 1}/{items.Count})";
        if (sentenceTMP != null) sentenceTMP.text = $"\"{item.sentenceText}\"";

        ResetStyles();

        if (feedbackTMP != null) feedbackTMP.text = "Inspect the case evidence. Is this sentence correct or does it contain a confused word error?";
        if (nextButton != null) nextButton.gameObject.SetActive(false);
    }

    private void ResetStyles()
    {
        if (correctButtonImage != null) correctButtonImage.color = correctButtonDefaultColor;
        if (hasErrorButtonImage != null) hasErrorButtonImage.color = hasErrorButtonDefaultColor;
        if (correctButton != null) correctButton.interactable = true;
        if (hasErrorButton != null) hasErrorButton.interactable = true;
    }

    private void OnChoiceSelected(bool selectedCorrect)
    {
        if (isAnswered) return;
        isAnswered = true;

        var item = items[currentIndex];
        bool userIsCorrect = (selectedCorrect == item.isCorrectAsWritten);

        if (selectedCorrect && correctButtonImage != null) correctButtonImage.color = userIsCorrect ? passColor : failColor;
        if (!selectedCorrect && hasErrorButtonImage != null) hasErrorButtonImage.color = userIsCorrect ? passColor : failColor;

        if (correctButton != null) correctButton.interactable = false;
        if (hasErrorButton != null) hasErrorButton.interactable = false;

        if (Masters_AudioManager.Instance != null)
        {
            if (!M3A_U7_HubProgress.IsIntroComplete())
            {
                Debug.LogWarning("[U7 AUDIO BLOCKED] Reading_LessonThree attempted audio before Intro completion");
            }
            else
            {
                Masters_AudioManager.Instance.PlaySoundEffect(userIsCorrect ? Masters_SFX.Correct : Masters_SFX.Incorrect);
            }
        }


        if (feedbackTMP != null)
        {
            feedbackTMP.text = userIsCorrect ? $"Verified! {item.correctionNote}" : $"Detective Note: {item.correctionNote}";
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
        if (feedbackTMP != null) feedbackTMP.text = "R03 Dossier Audit Complete! Reading Branch Solved (2/6).";
        M3A_U7_HubProgress.MarkR03Complete();

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }
    }

    protected override void OnNextButtonClicked()
    {
        M3A_U7_HubProgress.MarkR03Complete();

        if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ReturnToHub();
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
