using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// SPEAKING GAME CONTROLLER
///
/// Orchestrates the full speaking test flow:
///   - Loads each question (text + audio) into SpeechRecognitionTest
///   - Feeds the target word/phrase into WordMatchEvaluator
///   - Listens for pass threshold → enables Next button
///   - Advances through all questions
///   - Shows CompletedPanel when all are done
///
/// HIERARCHY SETUP:
/// Canvas
/// └── SpeakingGameController       ← this script
///     ├── SpeechRecognitionTest    ← existing script (same or child GO)
///     ├── WordMatchEvaluator       ← existing script (same or child GO)
///     ├── QuestionText (TMP)       ← shows current target word/phrase
///     ├── QuestionNumberText (TMP) ← "Question 1 / 5" (optional)
///     ├── NextButton               ← shared Next button (disabled until pass)
///     └── CompletedPanel           ← hidden by default
///         └── RestartButton
/// </summary>
public class SpeakingGameController : MonoBehaviour
{
    // ── Question Data ──────────────────────────────────────────────────────────

    [System.Serializable]
    public class SpeakingQuestion
    {
        [Tooltip("The word or phrase the player must say")]
        public string targetText;

        [Tooltip("Audio clip that reads the target text aloud")]
        public AudioClip questionAudio;
    }
    public UnitPanelController_BB1 unitPanel;
    public UnitButton_BB1 unitButton;
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("── Questions ───────────────────")]
    public SpeakingQuestion[] questions;

    [Header("── Script References ──────────")]
    public SpeechRecognitionTest speechRecognition;
    public WordMatchEvaluator    wordMatchEvaluator;

    [Header("── UI ──────────────────────────")]
    [Tooltip("Large label that shows the target word/phrase to the player")]
    public TextMeshProUGUI questionText;

    [Tooltip("Optional — shows 'Question X / Y'")]
    public TextMeshProUGUI questionNumberText;

    [Tooltip("Next button — enabled by WordMatchEvaluator when score passes threshold")]
    public Button nextButton;

    [Header("── Completion ─────────────────")]
    public GameObject completedPanel;
    public Button     FinishButton;

    // ── Runtime ────────────────────────────────────────────────────────────────

    private int currentQuestion = 0;

    // ── Unity Lifecycle ────────────────────────────────────────────────────────

    void Start()
    {
        completedPanel.SetActive(false);

        nextButton.interactable = false;
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(OnNextClicked);

        FinishButton.onClick.RemoveAllListeners();
        FinishButton.onClick.AddListener(Restart);

        // WordMatchEvaluator drives the Next button — wire it to our nextButton
        if (wordMatchEvaluator != null)
            wordMatchEvaluator.nextButton = nextButton;

        LoadQuestion(0);
    }

    // ── Question Loading ───────────────────────────────────────────────────────

    void LoadQuestion(int index)
{
    currentQuestion = index;
    var q = questions[index];

    
    if (wordMatchEvaluator != null)
        wordMatchEvaluator.ResetAllUI();

    // Show target text
    if (questionText)
        questionText.text = q.targetText;

    if (questionNumberText)
        questionNumberText.text = $"Question {index + 1} / {questions.Length}";

    if (wordMatchEvaluator != null && wordMatchEvaluator.targetWordLabel != null)
        wordMatchEvaluator.targetWordLabel.text = q.targetText;

    nextButton.interactable = false;

    if (speechRecognition != null)
        speechRecognition.LoadQuestion(q.targetText, q.questionAudio);
}

    // ── Next ───────────────────────────────────────────────────────────────────

    public void OnNextClicked()
    {
        nextButton.interactable = false;

        int next = currentQuestion + 1;

        if (next >= questions.Length)
        {
            ShowCompleted();
            return;
        }

        LoadQuestion(next);
    }

    // ── Completion ─────────────────────────────────────────────────────────────

    void ShowCompleted()
    {
        completedPanel.SetActive(true);

        // Optionally fade in
        var cg = completedPanel.GetComponent<CanvasGroup>();
        if (cg) StartCoroutine(FadeIn(cg));
    }

    IEnumerator FadeIn(CanvasGroup cg)
    {
        cg.alpha = 0f;
        float t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / 0.5f);
            yield return null;
        }
        cg.alpha = 1f;
    }

    // ── Restart ────────────────────────────────────────────────────────────────

  void Restart()
{
    completedPanel.SetActive(false);

    
    if (unitPanel != null && unitButton != null)
    {
        unitPanel.UnitFinished(unitButton);
    }

    
    currentQuestion = 0;

    if (wordMatchEvaluator != null)
        wordMatchEvaluator.ResetAllUI();
}


public void ResetGame()
{
    currentQuestion = 0;

    if (wordMatchEvaluator != null)
        wordMatchEvaluator.ResetAllUI();

    LoadQuestion(0);
}
}
