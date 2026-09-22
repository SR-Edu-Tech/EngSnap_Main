using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.ConfusedWords;

/// <summary>
/// Unit 7 Game Lesson Two (G02 — Case File Word Chase / Twin Word Memory Match).
/// Match each confused word with its verbatim book meaning.
/// </summary>
public class Masters_ConfusedEnglishWords_Game_LessonTwo : Masters_Lesson
{
    [Serializable]
    public class MatchPair
    {
        public string wordText;
        public string definitionText;
        public string feedbackNote;
    }

    [Header("UI References")]
    [SerializeField] private TMP_Text titleTMP;
    [SerializeField] private TMP_Text scoreTMP;
    [SerializeField] private TMP_Text promptTMP;
    [SerializeField] private TMP_Text feedbackTMP;
    [SerializeField] private Button optionAButton;
    [SerializeField] private Button optionBButton;
    [SerializeField] private Button optionCButton;
    [SerializeField] private TMP_Text optionAText;
    [SerializeField] private TMP_Text optionBText;
    [SerializeField] private TMP_Text optionCText;
    [SerializeField] private Image optionAImage;
    [SerializeField] private Image optionBImage;
    [SerializeField] private Image optionCImage;
    [SerializeField] private Button backButton;

    [Header("Colors")]
    [SerializeField] private Color defaultCardColor = new Color(0.95f, 0.92f, 0.85f, 1f);
    [SerializeField] private Color correctCardColor = new Color(0.4f, 0.85f, 0.4f, 1f);
    [SerializeField] private Color incorrectCardColor = new Color(0.95f, 0.4f, 0.4f, 1f);

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Match Data")]
    [SerializeField] private List<MatchPair> matchPairs = new List<MatchPair>();

    private int currentIndex = 0;
    private int matchedCount = 0;
    private bool isAnswered = false;
    private int currentCorrectOption = 0;

    protected override void Awake()
    {
        topic = Masters_Topic.Game;

        if (matchPairs == null || matchPairs.Count == 0)
        {
            InitializeDefaultPairs();
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
            optionAButton.onClick.AddListener(() => OnOptionClicked(0));
        }

        if (optionBButton != null)
        {
            optionBButton.onClick.RemoveAllListeners();
            optionBButton.onClick.AddListener(() => OnOptionClicked(1));
        }

        if (optionCButton != null)
        {
            optionCButton.onClick.RemoveAllListeners();
            optionCButton.onClick.AddListener(() => OnOptionClicked(2));
        }
    }

    protected override void Start()
    {
        base.Start();
        currentIndex = 0;
        matchedCount = 0;
        LoadCurrentPair();
    }

    private void InitializeDefaultPairs()
    {
        matchPairs = new List<MatchPair>
        {
            new MatchPair
            {
                wordText = "QUIET",
                definitionText = "no noise, silent",
                feedbackNote = "QUIET means no noise, silent."
            },
            new MatchPair
            {
                wordText = "QUITE",
                definitionText = "not exactly, not perfectly",
                feedbackNote = "QUITE means not exactly, fairly."
            },
            new MatchPair
            {
                wordText = "DIARY",
                definitionText = "a book that you write daily events in",
                feedbackNote = "DIARY is a personal record book."
            },
            new MatchPair
            {
                wordText = "DAIRY",
                definitionText = "a product that comes from animal milk",
                feedbackNote = "DAIRY refers to milk products."
            },
            new MatchPair
            {
                wordText = "WHOEVER",
                definitionText = "stands in the position of subject (he/she)",
                feedbackNote = "WHOEVER is used in subject position."
            },
            new MatchPair
            {
                wordText = "WHOMEVER",
                definitionText = "stands in the position of an object (him/her)",
                feedbackNote = "WHOMEVER is used in object position."
            }
        };
    }

    private void LoadCurrentPair()
    {
        if (currentIndex >= matchPairs.Count)
        {
            OnAllPairsMatched();
            return;
        }

        isAnswered = false;
        var current = matchPairs[currentIndex];

        if (scoreTMP != null) scoreTMP.text = $"Matched: {matchedCount}/{matchPairs.Count}";
        if (promptTMP != null) promptTMP.text = $"Definition: \"{current.definitionText}\"";

        // Setup 3 options: correct word + 2 distractors
        string target = current.wordText;
        List<string> options = new List<string> { target };
        
        // Add distinct distractors
        for (int i = 0; i < matchPairs.Count; i++)
        {
            if (options.Count >= 3) break;
            if (matchPairs[i].wordText != target && !options.Contains(matchPairs[i].wordText))
            {
                options.Add(matchPairs[i].wordText);
            }
        }

        // Deterministic distribution based on currentIndex
        currentCorrectOption = currentIndex % 3;
        if (currentCorrectOption != 0 && options.Count > currentCorrectOption)
        {
            string temp = options[0];
            options[0] = options[currentCorrectOption];
            options[currentCorrectOption] = temp;
        }

        if (optionAText != null) optionAText.text = options.Count > 0 ? options[0] : "QUIET";
        if (optionBText != null) optionBText.text = options.Count > 1 ? options[1] : "QUITE";
        if (optionCText != null) optionCText.text = options.Count > 2 ? options[2] : "DAIRY";

        ResetOptionColors();

        if (feedbackTMP != null) feedbackTMP.text = "Match each confused word to what it really means!";
        if (nextButton != null) nextButton.gameObject.SetActive(false);
    }

    private void ResetOptionColors()
    {
        if (optionAImage != null) optionAImage.color = defaultCardColor;
        if (optionBImage != null) optionBImage.color = defaultCardColor;
        if (optionCImage != null) optionCImage.color = defaultCardColor;
    }

    private void OnOptionClicked(int optionIndex)
    {
        if (isAnswered || currentIndex >= matchPairs.Count) return;

        isAnswered = true;
        var current = matchPairs[currentIndex];
        bool isCorrect = (optionIndex == currentCorrectOption);

        if (isCorrect)
        {
            matchedCount++;
            SetOptionColor(optionIndex, correctCardColor);
            if (feedbackTMP != null) feedbackTMP.text = $"<color=#50AA5A>SOLVED! {current.feedbackNote}</color>";
        }
        else
        {
            SetOptionColor(optionIndex, incorrectCardColor);
            SetOptionColor(currentCorrectOption, correctCardColor);
            if (feedbackTMP != null) feedbackTMP.text = $"<color=#E05050>MISMATCH! {current.feedbackNote}</color>";
        }

        if (Masters_AudioManager.Instance != null)
        {
            if (!M3A_U7_HubProgress.IsIntroComplete())
            {
                Debug.LogWarning("[U7 AUDIO BLOCKED] Game_LessonTwo attempted audio before Intro completion");
            }
            else
            {
                Masters_AudioManager.Instance.PlaySoundEffect(isCorrect ? Masters_SFX.Correct : Masters_SFX.Incorrect);
            }
        }

        if (scoreTMP != null) scoreTMP.text = $"Matched: {matchedCount}/{matchPairs.Count}";

        StartCoroutine(AutoAdvance());
    }


    private void SetOptionColor(int index, Color color)
    {
        if (index == 0 && optionAImage != null) optionAImage.color = color;
        if (index == 1 && optionBImage != null) optionBImage.color = color;
        if (index == 2 && optionCImage != null) optionCImage.color = color;
    }

    private IEnumerator AutoAdvance()
    {
        yield return new WaitForSeconds(1.5f);
        currentIndex++;
        LoadCurrentPair();
    }

    private void OnAllPairsMatched()
    {
        M3A_U7_HubProgress.MarkG02Complete();

        if (promptTMP != null) promptTMP.text = "CASE FILE WORD CHASE COMPLETE!";
        if (feedbackTMP != null) feedbackTMP.text = $"<color=#FFD750>Case Closed! All {matchedCount}/{matchPairs.Count} twin words correctly identified.</color>";

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }
    }

    protected override void OnNextButtonClicked()
    {
        M3A_U7_HubProgress.MarkG02Complete();

        if (nextLessonSO != null && Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        }
        else if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Game);
        }
        else if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ReturnToHub();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnBackClicked()
    {
        if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnBackButtonClicked();
        }
        else if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ReturnToHub();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
