using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.ConfusedWords;

/// <summary>
/// Unit 7 Listening Lesson One (L01 — Evidence Audio Examination).
/// Students listen to spoken confused words and identify the matching word card.
/// </summary>
public class Masters_ConfusedEnglishWords_Listening_LessonOne : Masters_Lesson
{
    [Serializable]
    public class ListeningItem
    {
        public string promptText;
        public AudioClip spokenAudio;
        public string wordA;
        public string wordB;
        public int correctIndex; // 0 for A, 1 for B
        public string hintText;
    }

    [Header("UI Components")]
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private TMP_Text questionCountText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button playAudioButton;
    [SerializeField] private Button replayAudioButton;
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

    [Header("Content Items")]

    [SerializeField] private List<ListeningItem> items = new List<ListeningItem>();

    private int currentIndex = 0;
    private bool isAnswered = false;

    protected override void Awake()
    {
        topic = Masters_Topic.Listening;

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

        if (playAudioButton != null)
        {
            playAudioButton.onClick.RemoveAllListeners();
            playAudioButton.onClick.AddListener(PlayCurrentAudio);
        }

        if (replayAudioButton != null)
        {
            replayAudioButton.onClick.RemoveAllListeners();
            replayAudioButton.onClick.AddListener(PlayCurrentAudio);
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
        Debug.Log($"[U7 L01 AUDIO DEBUG] Start. IntroComplete: {M3A_U7_HubProgress.IsIntroComplete()}, Items: {(items != null ? items.Count : 0)}, NarratorSpeech: {(narratorSpeech != null ? narratorSpeech.name : "null")}");
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
        items = new List<ListeningItem>
        {
            new ListeningItem { promptText = "Listen to the word and select the card:", wordA = "QUIET", wordB = "QUITE", correctIndex = 0, hintText = "QUIET means silent / no noise." },
            new ListeningItem { promptText = "Listen to the word and select the card:", wordA = "QUIET", wordB = "QUITE", correctIndex = 1, hintText = "QUITE means not exactly / not perfectly." },
            new ListeningItem { promptText = "Listen to the word and select the card:", wordA = "DIARY", wordB = "DAIRY", correctIndex = 0, hintText = "DIARY is a book for daily events." },
            new ListeningItem { promptText = "Listen to the word and select the card:", wordA = "DIARY", wordB = "DAIRY", correctIndex = 1, hintText = "DAIRY refers to products from animal milk." },
            new ListeningItem { promptText = "Listen to the word and select the card:", wordA = "WHOEVER", wordB = "WHOMEVER", correctIndex = 0, hintText = "WHOEVER stands in the subject position." },
            new ListeningItem { promptText = "Listen to the word and select the card:", wordA = "WHOEVER", wordB = "WHOMEVER", correctIndex = 1, hintText = "WHOMEVER stands in the object position." }
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

        if (questionCountText != null) questionCountText.text = $"Case Item: {currentIndex + 1}/{items.Count}";
        if (instructionText != null) instructionText.text = item.promptText;
        if (optionAText != null) optionAText.text = item.wordA;
        if (optionBText != null) optionBText.text = item.wordB;

        ResetCardStyles();

        if (feedbackText != null) feedbackText.text = "Tap the audio button to listen.";
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        PlayCurrentAudio();
    }

    private void ResetCardStyles()
    {
        if (optionAImage != null) optionAImage.color = defaultCardColor;
        if (optionBImage != null) optionBImage.color = defaultCardColor;
        if (optionAButton != null) optionAButton.interactable = true;
        if (optionBButton != null) optionBButton.interactable = true;
    }

    private void PlayCurrentAudio()
    {
        Debug.Log($"[U7 L01 AUDIO DEBUG] PlayCurrentAudio ENTERED. CurrentIndex: {currentIndex}, IntroComplete: {M3A_U7_HubProgress.IsIntroComplete()}, AudioManager exists: {Masters_AudioManager.Instance != null}");

        if (!M3A_U7_HubProgress.IsIntroComplete())
        {
            Debug.LogWarning("[U7 AUDIO BLOCKED] Listening_LessonOne attempted audio before Intro completion");
            return;
        }

        if (currentIndex < items.Count)
        {
            var item = items[currentIndex];
            Debug.Log($"[U7 L01 AUDIO DEBUG] Item[{currentIndex}] exists: true, SpokenAudio: {(item.spokenAudio != null ? item.spokenAudio.name : "null")}");

            if (item.spokenAudio != null && Masters_AudioManager.Instance != null)
            {
                Debug.Log($"[U7 L01 AUDIO DEBUG] PlayVoiceOver CALLED with clip: {item.spokenAudio.name}");
                Masters_AudioManager.Instance.PlayVoiceOver(item.spokenAudio);
            }
        }
    }



    private void OnOptionSelected(int selectedIndex)
    {
        if (isAnswered) return;
        isAnswered = true;

        var item = items[currentIndex];
        bool isCorrect = (selectedIndex == item.correctIndex);

        if (selectedIndex == 0 && optionAImage != null)
        {
            optionAImage.color = isCorrect ? correctCardColor : incorrectCardColor;
        }
        else if (selectedIndex == 1 && optionBImage != null)
        {
            optionBImage.color = isCorrect ? correctCardColor : incorrectCardColor;
        }

        // Highlight correct option if incorrect
        if (!isCorrect)
        {
            if (item.correctIndex == 0 && optionAImage != null) optionAImage.color = correctCardColor;
            if (item.correctIndex == 1 && optionBImage != null) optionBImage.color = correctCardColor;
        }

        if (optionAButton != null) optionAButton.interactable = false;
        if (optionBButton != null) optionBButton.interactable = false;

        if (Masters_AudioManager.Instance != null)
        {
            Masters_AudioManager.Instance.PlaySoundEffect(isCorrect ? Masters_SFX.Correct : Masters_SFX.Incorrect);
        }

        if (feedbackText != null)
        {
            feedbackText.text = isCorrect ? $"Correct! {item.hintText}" : $"Notice: {item.hintText}";
        }

        StartCoroutine(AdvanceAfterDelay(1.5f));
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
        if (feedbackText != null) feedbackText.text = "L01 Evidence Examined Successfully!";
        M3A_U7_HubProgress.MarkL01Complete();

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }
    }

    protected override void OnNextButtonClicked()
    {
        M3A_U7_HubProgress.MarkL01Complete();

        if (nextLessonSO != null && Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        }
        else if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ShowListening(2);
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
