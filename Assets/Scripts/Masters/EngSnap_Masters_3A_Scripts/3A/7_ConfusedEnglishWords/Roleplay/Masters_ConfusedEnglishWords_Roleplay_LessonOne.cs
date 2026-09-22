using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using EngSnap.ConfusedWords;

/// <summary>
/// Unit 7 Roleplay Lesson One (RP01 — On Stage: The Case of the Mixed-Up Words).
/// Student corrects witness NPC statements politely, identifying the right confused word.
/// </summary>
public class Masters_ConfusedEnglishWords_Roleplay_LessonOne : Masters_Lesson
{
    [Serializable]
    public class RoleplayItem
    {
        public string sceneHeader;
        public string witnessLine;
        public AudioClip witnessAudio;
        public string optionA;
        public string optionB;
        public int correctOptionIndex;
        public AudioClip responseAudio;
        public string explanationNote;
    }


    [Header("UI References")]
    [SerializeField] private TMP_Text sceneHeaderTMP;
    [SerializeField] private TMP_Text witnessTMP;
    [SerializeField] private TMP_Text instructionTMP;
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

    [Header("Roleplay Items")]
    [SerializeField] private List<RoleplayItem> items = new List<RoleplayItem>();

    private int currentIndex = 0;
    private int correctCount = 0;
    private bool isAnswered = false;

    protected override void Awake()
    {
        topic = Masters_Topic.Roleplay;

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
        correctCount = 0;
        LoadCurrentScene();
    }

    private void InitializeDefaultItems()
    {
        items = new List<RoleplayItem>
        {
            new RoleplayItem
            {
                sceneHeader = "Scene 1/4: Study Hall Witness",
                witnessLine = "Witness: \"Please be quite — I'm trying to study.\"",
                optionA = "You mean quiet — no noise.",
                optionB = "You mean quite — not exactly.",
                correctOptionIndex = 0,
                explanationNote = "QUIET means silent / no noise."
            },
            new RoleplayItem
            {
                sceneHeader = "Scene 2/4: Diary Mystery",
                witnessLine = "Witness: \"I wrote it in my dairy last night.\"",
                optionA = "You mean dairy — products from milk.",
                optionB = "You mean diary — the book you write in.",
                correctOptionIndex = 1,
                explanationNote = "DIARY is a book for writing daily records."
            },
            new RoleplayItem
            {
                sceneHeader = "Scene 3/4: Kitchen Report",
                witnessLine = "Witness: \"I love eating diary products like cheese.\"",
                optionA = "You mean dairy — products from milk.",
                optionB = "You mean diary — a book you write in.",
                correctOptionIndex = 0,
                explanationNote = "DAIRY refers to animal milk foods."
            },
            new RoleplayItem
            {
                sceneHeader = "Scene 4/4: Detective Plan",
                witnessLine = "Witness: \"This is not quiet the best idea.\"",
                optionA = "You mean quiet — no noise.",
                optionB = "You mean quite — not exactly.",
                correctOptionIndex = 1,
                explanationNote = "QUITE means rather / not perfectly."
            }
        };
    }

    private void LoadCurrentScene()
    {
        if (currentIndex >= items.Count)
        {
            OnAllScenesComplete();
            return;
        }

        isAnswered = false;
        var current = items[currentIndex];

        if (sceneHeaderTMP != null) sceneHeaderTMP.text = current.sceneHeader;
        if (witnessTMP != null) witnessTMP.text = current.witnessLine;
        if (instructionTMP != null) instructionTMP.text = "Choose the polite detective correction:";
        if (optionAText != null) optionAText.text = current.optionA;
        if (optionBText != null) optionBText.text = current.optionB;

        ResetOptionColors();

        if (feedbackTMP != null) feedbackTMP.text = "Help the witness by correcting the mixed-up word.";
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        PlayWitnessAudio();
    }

    private void PlayWitnessAudio()
    {
        if (!M3A_U7_HubProgress.IsIntroComplete())
        {
            Debug.LogWarning("[U7 AUDIO BLOCKED] Roleplay_LessonOne attempted audio before Intro completion");
            return;
        }

        if (currentIndex < items.Count && items[currentIndex].witnessAudio != null)
        {
            if (Masters_AudioManager.Instance != null)
            {
                Debug.Log("[U7 FLOW] RP01 NPC audio started");
                Masters_AudioManager.Instance.PlayVoiceOver(items[currentIndex].witnessAudio);
            }
        }
    }


    private void ResetOptionColors()
    {
        if (optionAImage != null) optionAImage.color = defaultCardColor;
        if (optionBImage != null) optionBImage.color = defaultCardColor;
    }

    public void OnOptionSelected(int optionIndex)
    {
        if (isAnswered || currentIndex >= items.Count) return;

        isAnswered = true;
        var current = items[currentIndex];
        bool isCorrect = (optionIndex == currentCorrectOption(current));

        if (isCorrect)
        {
            correctCount++;
            if (optionIndex == 0 && optionAImage != null) optionAImage.color = correctCardColor;
            if (optionIndex == 1 && optionBImage != null) optionBImage.color = correctCardColor;

            if (feedbackTMP != null) feedbackTMP.text = $"<color=#50AA5A>SOLVED! Witness: \"Thank you! {current.explanationNote}\"</color>";
        }
        else
        {
            if (optionIndex == 0 && optionAImage != null) optionAImage.color = incorrectCardColor;
            if (optionIndex == 1 && optionBImage != null) optionBImage.color = incorrectCardColor;

            int correctIdx = currentCorrectOption(current);
            if (correctIdx == 0 && optionAImage != null) optionAImage.color = correctCardColor;
            if (correctIdx == 1 && optionBImage != null) optionBImage.color = correctCardColor;

            if (feedbackTMP != null) feedbackTMP.text = $"<color=#E05050>Witness looks puzzled. {current.explanationNote}</color>";
        }

        StartCoroutine(AutoAdvance());
    }

    private int currentCorrectOption(RoleplayItem item)
    {
        return item.correctOptionIndex;
    }

    private IEnumerator AutoAdvance()
    {
        yield return new WaitForSeconds(1.6f);
        currentIndex++;
        LoadCurrentScene();
    }

    private void OnAllScenesComplete()
    {
        M3A_U7_HubProgress.MarkRP01Complete();

        if (witnessTMP != null) witnessTMP.text = "WITNESS INTERVIEW CONCLUDED!";
        if (feedbackTMP != null) feedbackTMP.text = $"<color=#FFD750>Case progress updated! Corrected: {correctCount}/{items.Count}</color>";

        if (nextButton != null) nextButton.gameObject.SetActive(true);
    }

    protected override void OnNextButtonClicked()
    {
        M3A_U7_HubProgress.MarkRP01Complete();

        if (nextLessonSO != null && Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        }
        else if (M3A_U7_ScreenController.Instance != null)
        {
            M3A_U7_ScreenController.Instance.ShowRolePlay02();
        }
        else if (Masters_LevelManager.Instance != null)
        {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Roleplay);
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
