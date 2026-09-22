using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.ConfusedWords;

/// <summary>
/// Unit 7 Listening Lesson Two (L02 — Clue Audio & Definition Investigation).
/// Students listen to context clues and match them with the authoritative confused word.
/// Completing L02 (with L01) marks the Listening branch complete (1/6).
/// </summary>
public class Masters_ConfusedEnglishWords_Listening_LessonTwo : Masters_Lesson
{
    [Serializable]
    public class DefinitionClueItem
    {
        public string clueTitle;
        public string clueSpokenText;
        public AudioClip clueAudio;
        public AudioClip revealAudio;
        public string[] options; // 3 or 4 options
        public int correctOptionIndex;
        public string definitionSummary;
    }


    [Header("UI Components")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text clueNumberText;
    [SerializeField] private TMP_Text clueTranscriptText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button playClueAudioButton;
    [SerializeField] private Button replayClueAudioButton;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TMP_Text[] optionTexts;
    [SerializeField] private Image[] optionImages;
    [SerializeField] private Button backButton;
    [SerializeField] private Color defaultCardColor = new Color(0.95f, 0.92f, 0.85f, 1f);
    [SerializeField] private Color correctCardColor = new Color(0.4f, 0.85f, 0.4f, 1f);
    [SerializeField] private Color incorrectCardColor = new Color(0.95f, 0.4f, 0.4f, 1f);

    [Header("Clue Items")]
    [SerializeField] private List<DefinitionClueItem> items = new List<DefinitionClueItem>();

    private int currentIndex = 0;
    private bool isAnswered = false;

    protected override void Awake()
    {
        topic = Masters_Topic.Listening;
        Debug.Log($"[U7 L02 AUDIO DEBUG] Awake. Serialized items count: {(items != null ? items.Count : 0)}");

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

        if (playClueAudioButton != null)
        {
            playClueAudioButton.onClick.RemoveAllListeners();
            playClueAudioButton.onClick.AddListener(PlayCurrentAudio);
        }

        if (replayClueAudioButton != null)
        {
            replayClueAudioButton.onClick.RemoveAllListeners();
            replayClueAudioButton.onClick.AddListener(PlayCurrentAudio);
        }

        if (optionButtons != null)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                int index = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
            }
        }
    }

    protected override void Start()
    {
        base.Start();
        Debug.Log($"[U7 L02 AUDIO DEBUG] Start. IntroComplete: {M3A_U7_HubProgress.IsIntroComplete()}, Items: {(items != null ? items.Count : 0)}, NarratorSpeech: {(narratorSpeech != null ? narratorSpeech.name : "null")}");
        currentIndex = 0;
        StartCoroutine(InitializeLessonRoutine());
    }

    private IEnumerator InitializeLessonRoutine()
    {
        if (narratorSpeech != null && Masters_AudioManager.Instance != null)
        {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        }
        LoadCurrentItem();
    }


    private void InitializeDefaultItems()
    {
        items = new List<DefinitionClueItem>
        {
            new DefinitionClueItem
            {
                clueTitle = "Clue #1: Sound Level",
                clueSpokenText = "Clue: This word means making no noise and being completely silent.",
                options = new[] { "QUIET", "QUITE", "DAIRY" },
                correctOptionIndex = 0,
                definitionSummary = "QUIET = no noise, silent"
            },
            new DefinitionClueItem
            {
                clueTitle = "Clue #2: Degree / Certainty",
                clueSpokenText = "Clue: This word means not exactly, or not perfectly.",
                options = new[] { "QUIET", "QUITE", "DIARY" },
                correctOptionIndex = 1,
                definitionSummary = "QUITE = not exactly, not perfectly"
            },
            new DefinitionClueItem
            {
                clueTitle = "Clue #3: Daily Records",
                clueSpokenText = "Clue: A book in which you write daily events, records, and experiences.",
                options = new[] { "DAIRY", "DIARY", "WHOEVER" },
                correctOptionIndex = 1,
                definitionSummary = "DIARY = a book to write daily events and experiences"
            },
            new DefinitionClueItem
            {
                clueTitle = "Clue #4: Farm Production",
                clueSpokenText = "Clue: Food or product that comes directly from animal milk.",
                options = new[] { "DIARY", "DAIRY", "QUITE" },
                correctOptionIndex = 1,
                definitionSummary = "DAIRY = a product that comes from animal milk"
            },
            new DefinitionClueItem
            {
                clueTitle = "Clue #5: Subject Position",
                clueSpokenText = "Clue: This pronoun stands in the position of a subject.",
                options = new[] { "WHOEVER", "WHOMEVER", "QUIET" },
                correctOptionIndex = 0,
                definitionSummary = "WHOEVER = stands in the position of subject"
            },
            new DefinitionClueItem
            {
                clueTitle = "Clue #6: Object Position",
                clueSpokenText = "Clue: This pronoun stands in the position of an object.",
                options = new[] { "WHOEVER", "WHOMEVER", "DIARY" },
                correctOptionIndex = 1,
                definitionSummary = "WHOMEVER = stands in the position of an object"
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

        if (clueNumberText != null) clueNumberText.text = $"Case File Clue: {currentIndex + 1}/{items.Count}";
        if (instructionText != null) instructionText.text = item.clueTitle;
        if (clueTranscriptText != null) clueTranscriptText.text = item.clueSpokenText;

        ResetOptionStyles();

        if (optionButtons != null && optionTexts != null)
        {
            for (int i = 0; i < optionButtons.Length; i++)
            {
                if (i < item.options.Length)
                {
                    optionButtons[i].gameObject.SetActive(true);
                    optionTexts[i].text = item.options[i];
                }
                else
                {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }

        if (feedbackText != null) feedbackText.text = "Listen to the clue and select the matching word card.";
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        PlayCurrentAudio();
    }

    private void ResetOptionStyles()
    {
        if (optionImages != null)
        {
            foreach (var img in optionImages)
            {
                if (img != null) img.color = defaultCardColor;
            }
        }

        if (optionButtons != null)
        {
            foreach (var btn in optionButtons)
            {
                if (btn != null) btn.interactable = true;
            }
        }
    }

    private void PlayCurrentAudio()
    {
        Debug.Log($"[U7 L02 AUDIO DEBUG] PlayCurrentAudio ENTERED. CurrentIndex: {currentIndex}, IntroComplete: {M3A_U7_HubProgress.IsIntroComplete()}, AudioManager exists: {Masters_AudioManager.Instance != null}");

        if (!M3A_U7_HubProgress.IsIntroComplete())
        {
            Debug.LogWarning("[U7 AUDIO BLOCKED] Listening_LessonTwo attempted audio before Intro completion");
            return;
        }

        if (currentIndex < items.Count)
        {
            var item = items[currentIndex];
            Debug.Log($"[U7 L02 AUDIO DEBUG] Item[{currentIndex}] exists: true, ClueAudio: {(item.clueAudio != null ? item.clueAudio.name : "null")}, RevealAudio: {(item.revealAudio != null ? item.revealAudio.name : "null")}");

            if (item.clueAudio != null && Masters_AudioManager.Instance != null)
            {
                Debug.Log($"[U7 L02 AUDIO DEBUG] PlayVoiceOver CALLED with clip: {item.clueAudio.name}");
                Masters_AudioManager.Instance.PlayVoiceOver(item.clueAudio);
            }
            else
            {
                Debug.LogWarning($"[U7 L02 AUDIO DEBUG] Cannot play audio! clueAudio is null: {item.clueAudio == null}, AudioManager is null: {Masters_AudioManager.Instance == null}");
            }
        }
        else
        {
            Debug.LogWarning($"[U7 L02 AUDIO DEBUG] currentIndex {currentIndex} >= items.Count {items.Count}");
        }
    }



    private void OnOptionSelected(int selectedIndex)
    {
        if (isAnswered) return;
        isAnswered = true;

        var item = items[currentIndex];
        bool isCorrect = (selectedIndex == item.correctOptionIndex);

        if (optionImages != null && selectedIndex < optionImages.Length && optionImages[selectedIndex] != null)
        {
            optionImages[selectedIndex].color = isCorrect ? correctCardColor : incorrectCardColor;
        }

        if (!isCorrect && optionImages != null && item.correctOptionIndex < optionImages.Length && optionImages[item.correctOptionIndex] != null)
        {
            optionImages[item.correctOptionIndex].color = correctCardColor;
        }

        if (optionButtons != null)
        {
            foreach (var btn in optionButtons)
            {
                if (btn != null) btn.interactable = false;
            }
        }

        if (Masters_AudioManager.Instance != null)
        {
            if (isCorrect && item.revealAudio != null)
            {
                Masters_AudioManager.Instance.PlayVoiceOver(item.revealAudio);
            }
            else
            {
                Masters_AudioManager.Instance.PlaySoundEffect(isCorrect ? Masters_SFX.Correct : Masters_SFX.Incorrect);
            }
        }


        if (feedbackText != null)
        {
            feedbackText.text = isCorrect ? $"Correct! {item.definitionSummary}" : $"Definition: {item.definitionSummary}";
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
        if (feedbackText != null) feedbackText.text = "L02 Investigation Completed! Listening Branch Solved (1/6).";
        M3A_U7_HubProgress.MarkL02Complete();

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }
    }

    protected override void OnNextButtonClicked()
    {
        M3A_U7_HubProgress.MarkL02Complete();

        if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ReturnToHub();
        }
        else if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Listening);
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
