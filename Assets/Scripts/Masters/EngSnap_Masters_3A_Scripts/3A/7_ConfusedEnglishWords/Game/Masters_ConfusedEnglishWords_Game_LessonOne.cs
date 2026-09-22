using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.ConfusedWords;

/// <summary>
/// Unit 7 Game Lesson One (G01 — Line-Up Rush: Catch the Right Twin).
/// Timed word-choice reaction activity testing the confused-word pairs in context.
/// </summary>
public class Masters_ConfusedEnglishWords_Game_LessonOne : Masters_Lesson
{
    [Serializable]
    public class GameItem
    {
        public string roundHeader;
        public string sentenceWithGap;
        public string optionA;
        public string optionB;
        public int correctOptionIndex; // 0 = A, 1 = B
        public bool acceptsEither;
        public string feedbackExplanation;
    }

    [Header("UI References")]
    [SerializeField] private TMP_Text headerTMP;
    [SerializeField] private TMP_Text scoreTMP;
    [SerializeField] private TMP_Text sentenceTMP;
    [SerializeField] private TMP_Text feedbackTMP;
    [SerializeField] private Button optionAButton;
    [SerializeField] private Button optionBButton;
    [SerializeField] private TMP_Text optionAText;
    [SerializeField] private TMP_Text optionBText;
    [SerializeField] private Image optionAImage;
    [SerializeField] private Image optionBImage;
    [SerializeField] private Button backButton;

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Colors")]

    [SerializeField] private Color defaultCardColor = new Color(0.95f, 0.92f, 0.85f, 1f);
    [SerializeField] private Color correctCardColor = new Color(0.4f, 0.85f, 0.4f, 1f);
    [SerializeField] private Color incorrectCardColor = new Color(0.95f, 0.4f, 0.4f, 1f);

    [Header("Game Data")]
    [SerializeField] private List<GameItem> gameItems = new List<GameItem>();

    private int currentIndex = 0;
    private int score = 0;
    private bool isAnswered = false;

    protected override void Awake()
    {
        topic = Masters_Topic.Game;

        if (gameItems == null || gameItems.Count == 0)
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
        score = 0;
        LoadCurrentItem();
    }

    private void InitializeDefaultItems()
    {
        gameItems = new List<GameItem>
        {
            new GameItem
            {
                roundHeader = "Round 1/8",
                sentenceWithGap = "Please be ______ while I study.",
                optionA = "QUIET",
                optionB = "QUITE",
                correctOptionIndex = 0,
                acceptsEither = false,
                feedbackExplanation = "QUIET = no noise, silent."
            },
            new GameItem
            {
                roundHeader = "Round 2/8",
                sentenceWithGap = "My sister is not ______ right.",
                optionA = "QUIET",
                optionB = "QUITE",
                correctOptionIndex = 1,
                acceptsEither = false,
                feedbackExplanation = "QUITE = not exactly, fairly."
            },
            new GameItem
            {
                roundHeader = "Round 3/8",
                sentenceWithGap = "She will not let me read her ______.",
                optionA = "DIARY",
                optionB = "DAIRY",
                correctOptionIndex = 0,
                acceptsEither = false,
                feedbackExplanation = "DIARY = personal daily records."
            },
            new GameItem
            {
                roundHeader = "Round 4/8",
                sentenceWithGap = "I like to eat ______ products.",
                optionA = "DIARY",
                optionB = "DAIRY",
                correctOptionIndex = 1,
                acceptsEither = false,
                feedbackExplanation = "DAIRY = products from milk."
            },
            new GameItem
            {
                roundHeader = "Round 5/8",
                sentenceWithGap = "______ leaves last should lock the door.",
                optionA = "WHOEVER",
                optionB = "WHOMEVER",
                correctOptionIndex = 0,
                acceptsEither = false,
                feedbackExplanation = "WHOEVER = subject pronoun (he/she)."
            },
            new GameItem
            {
                roundHeader = "Round 6/8",
                sentenceWithGap = "Give the letter to ______ you see first.",
                optionA = "WHOEVER",
                optionB = "WHOMEVER",
                correctOptionIndex = 1,
                acceptsEither = false,
                feedbackExplanation = "WHOMEVER = object pronoun (him/her)."
            },
            new GameItem
            {
                roundHeader = "Round 7/8",
                sentenceWithGap = "My teacher asked us to be ______.",
                optionA = "QUIET",
                optionB = "QUITE",
                correctOptionIndex = 0,
                acceptsEither = false,
                feedbackExplanation = "QUIET = no noise, silent."
            },
            new GameItem
            {
                roundHeader = "Round 8/8",
                sentenceWithGap = "We make many sweets using ______ products.",
                optionA = "DIARY",
                optionB = "DAIRY",
                correctOptionIndex = 1,
                acceptsEither = false,
                feedbackExplanation = "DAIRY = products from animal milk."
            }
        };
    }

    private void LoadCurrentItem()
    {
        if (currentIndex >= gameItems.Count)
        {
            OnAllItemsComplete();
            return;
        }

        isAnswered = false;
        var item = gameItems[currentIndex];

        if (headerTMP != null) headerTMP.text = item.roundHeader;
        if (scoreTMP != null) scoreTMP.text = $"Arrests: {score}/{gameItems.Count}";
        if (sentenceTMP != null) sentenceTMP.text = item.sentenceWithGap;
        if (optionAText != null) optionAText.text = item.optionA;
        if (optionBText != null) optionBText.text = item.optionB;

        ResetButtonColors();

        if (feedbackTMP != null) feedbackTMP.text = "Spot the right suspect word before it walks away — go!";
        if (nextButton != null) nextButton.gameObject.SetActive(false);
    }

    private void ResetButtonColors()
    {
        if (optionAImage != null) optionAImage.color = defaultCardColor;
        if (optionBImage != null) optionBImage.color = defaultCardColor;
    }

    public void OnOptionSelected(int optionIndex)
    {
        if (isAnswered || currentIndex >= gameItems.Count) return;

        isAnswered = true;
        var item = gameItems[currentIndex];
        bool isCorrect = item.acceptsEither || (optionIndex == item.correctOptionIndex);

        if (isCorrect)
        {
            score++;
            if (optionIndex == 0 && optionAImage != null) optionAImage.color = correctCardColor;
            if (optionIndex == 1 && optionBImage != null) optionBImage.color = correctCardColor;

            if (feedbackTMP != null) feedbackTMP.text = $"<color=#50AA5A>ARRESTED! {item.feedbackExplanation}</color>";
        }
        else
        {
            if (optionIndex == 0 && optionAImage != null) optionAImage.color = incorrectCardColor;
            if (optionIndex == 1 && optionBImage != null) optionBImage.color = incorrectCardColor;

            if (item.correctOptionIndex == 0 && optionAImage != null) optionAImage.color = correctCardColor;
            if (item.correctOptionIndex == 1 && optionBImage != null) optionBImage.color = correctCardColor;

            if (feedbackTMP != null) feedbackTMP.text = $"<color=#E05050>ESCAPED! {item.feedbackExplanation}</color>";
        }

        if (Masters_AudioManager.Instance != null)
        {
            if (!M3A_U7_HubProgress.IsIntroComplete())
            {
                Debug.LogWarning("[U7 AUDIO BLOCKED] Game_LessonOne attempted audio before Intro completion");
            }
            else
            {
                Masters_AudioManager.Instance.PlaySoundEffect(isCorrect ? Masters_SFX.Correct : Masters_SFX.Incorrect);
            }
        }

        if (scoreTMP != null) scoreTMP.text = $"Arrests: {score}/{gameItems.Count}";

        StartCoroutine(AutoAdvance());
    }


    private IEnumerator AutoAdvance()
    {
        yield return new WaitForSeconds(1.5f);
        currentIndex++;
        LoadCurrentItem();
    }

    private void OnAllItemsComplete()
    {
        M3A_U7_HubProgress.MarkG01Complete();

        if (sentenceTMP != null) sentenceTMP.text = "LINE-UP RUSH COMPLETE!";
        if (feedbackTMP != null) feedbackTMP.text = $"<color=#FFD750>Great detective work! Final Arrests: {score}/{gameItems.Count}</color>";

        if (nextButton != null) nextButton.gameObject.SetActive(true);
    }

    protected override void OnNextButtonClicked()
    {
        M3A_U7_HubProgress.MarkG01Complete();

        if (nextLessonSO != null && Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        }
        else if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ShowGame02();
        }
        else if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Game);
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
    }
}
