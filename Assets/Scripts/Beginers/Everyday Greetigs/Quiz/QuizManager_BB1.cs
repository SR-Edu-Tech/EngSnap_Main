using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Master controller for the BB1 Quiz gameplay screen.
///
/// ═══════════════════════════════════════════════════════════════════════
/// SCENE SETUP
/// ═══════════════════════════════════════════════════════════════════════
/// Create a GameObject named "QuizManager" and attach this script.
/// In the same screen hierarchy you need:
///
///   [QuizManager]                     ← this script
///   [QuizAudioController_BB1]         ← on same GO or child
///   [QuizQuestionUI_BB1]              ← question panel prefab instance
///   [QuizSummaryUI_BB1]               ← summary panel (hidden until end)
///   [IntroPanel]                      ← (optional) shown during intro VO
///   [ProgressBar / QuestionCounter]   ← optional
///
/// ═══════════════════════════════════════════════════════════════════════
/// WIRING (Inspector)
/// ═══════════════════════════════════════════════════════════════════════
///   quizData          → QuizData_BB1 ScriptableObject
///   panel             → UnitPanelController_BB1 (parent panel)
///   unitButton        → UnitButton_BB1 that launched this screen
///   audioController   → QuizAudioController_BB1
///   questionUI        → QuizQuestionUI_BB1
///   summaryUI         → QuizSummaryUI_BB1
///   introPanel        → (optional) panel shown during intro VO
///   questionCounter   → (optional) TextMeshProUGUI "Question 2 of 5"
///   progressBar       → (optional) Slider 0..1
///   backButton        → (optional) back button
///
/// ═══════════════════════════════════════════════════════════════════════
/// SAVE / RESTORE LOGIC
/// ═══════════════════════════════════════════════════════════════════════
///   Key: unitID + "_quizProgress"  → current question index (0-based)
///   Key: unitID + "_quizScore"     → running correct count
///   Key: unitID + "_quizDone"      → 1 when quiz fully completed
///
///   On Open:
///     If _quizDone == 1  → start from Q0 (replay from beginning)
///     Else               → resume from saved _quizProgress index
///
///   On Back mid-quiz:
///     Save current index + score → resume next time
///
///   On Finish:
///     Write _quizDone = 1, clear progress+score
///     Call panel.UnitFinished(unitButton) → badge shown
/// </summary>
public class QuizManager_BB1 : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────────────
    [Header("Data")]
    public QuizData_BB1 quizData;

    [Header("Panel Integration")]
    public UnitPanelController_BB1 panel;
    public UnitButton_BB1          unitButton;

    [Header("Controllers / UI")]
    public QuizAudioController_BB1 audioController;
    public QuizQuestionUI_BB1      questionUI;
    public QuizSummaryUI_BB1       summaryUI;

    [Header("Intro Panel (optional)")]
    public GameObject introPanelGO;

    [Header("HUD (optional)")]
    public TextMeshProUGUI questionCounter;   // "Question 1 of 5"
    public Slider          progressBar;       // 0..1
    public TextMeshProUGUI liveScoreLabel;    // "Score: 3"

    [Header("Back Button (optional)")]
    public Button backButton;

    // ── Save keys ─────────────────────────────────────────────────────────
    private string saveKey_progress;
    private string saveKey_score;
    private string saveKey_done;

    // ── Runtime state ─────────────────────────────────────────────────────
    private int  currentQuestionIndex = 0;
    private int  correctCount         = 0;
    private bool quizCompleted        = false;
    private bool inReviewMode         = false; // true when replaying one question from summary

    // ── Unity lifecycle ───────────────────────────────────────────────────

    void OnEnable()
    {
        BeginQuiz();
    }

    void OnDisable()
    {
        if (audioController != null) audioController.StopAll();
        StopAllCoroutines();
    }

    // ── Entry Point ───────────────────────────────────────────────────────

    void BeginQuiz()
    {
        // Build save keys from unitButton's unitID
        string uid = (unitButton != null && !string.IsNullOrEmpty(unitButton.unitID))
                     ? unitButton.unitID
                     : "default";

        saveKey_progress = uid + "_quizProgress";
        saveKey_score    = uid + "_quizScore";
        saveKey_done     = uid + "_quizDone";

        // ── Decide where to start ──────────────────────────────────────
        bool wasCompleted = PlayerPrefs.GetInt(saveKey_done, 0) == 1;

        if (wasCompleted)
        {
            // Player already finished → start fresh (replay from Q1)
            currentQuestionIndex = 0;
            correctCount         = 0;
            ClearSaveData();
        }
        else
        {
            // Resume from last saved position
            currentQuestionIndex = PlayerPrefs.GetInt(saveKey_progress, 0);
            correctCount         = PlayerPrefs.GetInt(saveKey_score, 0);
        }

        quizCompleted = false;
        inReviewMode  = false;

        // Wire back button
        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        // Hide summary
        if (summaryUI != null) summaryUI.Hide();

        // Show intro panel (if first question)
        StartCoroutine(PlayIntroThenStart());
    }

    // ── Intro ─────────────────────────────────────────────────────────────

    IEnumerator PlayIntroThenStart()
    {
        // Start BGM
        if (audioController != null && quizData != null)
            audioController.PlayBGM(quizData.bgmClip);

        // Show intro panel during intro VO (only on first question; skip if resuming mid-quiz)
        bool showIntro = (currentQuestionIndex == 0);

        if (showIntro && introPanelGO != null)
            introPanelGO.SetActive(true);

        if (questionUI != null)
            questionUI.gameObject.SetActive(false);

        if (showIntro && quizData != null && quizData.introVO != null)
        {
            bool introDone = false;
            audioController.PlayVO(quizData.introVO, () => introDone = true);
            yield return new WaitUntil(() => introDone);
            yield return new WaitForSeconds(0.3f);
        }

        if (introPanelGO != null)
            introPanelGO.SetActive(false);

        if (questionUI != null)
            questionUI.gameObject.SetActive(true);

        ShowCurrentQuestion();
    }

    // ── Question Flow ─────────────────────────────────────────────────────

    void ShowCurrentQuestion()
    {
        if (quizData == null || quizData.questions == null) return;
        if (currentQuestionIndex >= quizData.questions.Count)
        {
            // All questions done
            FinishQuiz();
            return;
        }

        UpdateHUD();

        var question = quizData.questions[currentQuestionIndex];
        questionUI.ShowQuestion(question, OnQuestionAnswered);
    }

    void OnQuestionAnswered(bool wasCorrect)
    {
        if (wasCorrect)
        {
            correctCount++;
            // Play correct VO
            if (audioController != null && quizData != null)
                audioController.PlayVO(quizData.correctVO);
        }
        else
        {
            // Play wrong VO
            if (audioController != null && quizData != null)
                audioController.PlayVO(quizData.wrongVO);
        }

        if (inReviewMode)
        {
            // After reviewing a single question from summary, go back to summary
            StartCoroutine(DelayThen(1.5f, ReturnToSummary));
            return;
        }

        // Advance to next question
        currentQuestionIndex++;
        SaveProgress();

        StartCoroutine(DelayThen(1.5f, ShowCurrentQuestion));
    }

    // ── Summary / Finish ──────────────────────────────────────────────────

    void FinishQuiz()
    {
        quizCompleted = true;

        // Mark as done + clear mid-progress
        PlayerPrefs.SetInt(saveKey_done, 1);
        ClearSaveData(keepDone: true);

        // Play end VO
        if (audioController != null && quizData != null)
            audioController.PlayVO(quizData.endVO);

        // Build question labels for review
        var labels = new List<string>();
        if (quizData != null)
            for (int i = 0; i < quizData.questions.Count; i++)
                labels.Add($"Q{i + 1}");

        // Show summary
        if (summaryUI != null)
        {
            questionUI.gameObject.SetActive(false);
            summaryUI.Show(
                correctCount,
                quizData != null ? quizData.questions.Count : 0,
                labels,
                onFinishCallback:      OnSummaryFinish,
                onReviewCallback:      OnReviewQuestion,
                onReplayAllCallback:   OnReplayAll
            );
        }

        UpdateHUD();
    }

    void OnSummaryFinish()
    {
        // Stop audio
        if (audioController != null) audioController.StopAll();

        // Tell the panel this unit is done → badge enabled
        if (panel != null && unitButton != null)
            panel.UnitFinished(unitButton);
        else
            gameObject.SetActive(false);
    }

    void OnReviewQuestion(int questionIndex)
    {
        // Player wants to replay one specific question
        if (quizData == null || questionIndex < 0 || questionIndex >= quizData.questions.Count)
            return;

        inReviewMode         = true;
        currentQuestionIndex = questionIndex;

        summaryUI.Hide();
        questionUI.gameObject.SetActive(true);
        ShowCurrentQuestion();
    }

    void ReturnToSummary()
    {
        inReviewMode = false;
        questionUI.gameObject.SetActive(false);

        var labels = new List<string>();
        if (quizData != null)
            for (int i = 0; i < quizData.questions.Count; i++)
                labels.Add($"Q{i + 1}");

        summaryUI.Show(
            correctCount,
            quizData != null ? quizData.questions.Count : 0,
            labels,
            onFinishCallback:    OnSummaryFinish,
            onReviewCallback:    OnReviewQuestion,
            onReplayAllCallback: OnReplayAll
        );
    }

    void OnReplayAll()
    {
        // Full replay from Q1
        currentQuestionIndex = 0;
        correctCount         = 0;
        quizCompleted        = false;
        inReviewMode         = false;
        ClearSaveData();

        summaryUI.Hide();
        questionUI.gameObject.SetActive(true);

        if (introPanelGO != null) introPanelGO.SetActive(false);
        ShowCurrentQuestion();
    }

    // ── Back button ───────────────────────────────────────────────────────

    void OnBackClicked()
    {
        // Save progress so player can resume
        if (!quizCompleted)
            SaveProgress();

        if (audioController != null) audioController.StopAll();
        StopAllCoroutines();

        if (panel != null)
        {
            gameObject.SetActive(false);
            panel.gameObject.SetActive(true);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    // ── HUD ───────────────────────────────────────────────────────────────

    void UpdateHUD()
    {
        if (quizData == null) return;
        int total = quizData.questions.Count;

        if (questionCounter != null)
        {
            if (quizCompleted)
                questionCounter.text = "Quiz Complete!";
            else
                questionCounter.text = $"Question {currentQuestionIndex + 1} of {total}";
        }

        if (progressBar != null)
            progressBar.value = total > 0 ? (float)currentQuestionIndex / total : 0f;

        if (liveScoreLabel != null)
            liveScoreLabel.text = $"Score: {correctCount}";
    }

    // ── Save / Load helpers ───────────────────────────────────────────────

    void SaveProgress()
    {
        PlayerPrefs.SetInt(saveKey_progress, currentQuestionIndex);
        PlayerPrefs.SetInt(saveKey_score,    correctCount);
        PlayerPrefs.Save();
    }

    /// <param name="keepDone">If true, the _quizDone key is not deleted.</param>
    void ClearSaveData(bool keepDone = false)
    {
        PlayerPrefs.DeleteKey(saveKey_progress);
        PlayerPrefs.DeleteKey(saveKey_score);
        if (!keepDone) PlayerPrefs.DeleteKey(saveKey_done);
        PlayerPrefs.Save();
    }

    // ── Utility ───────────────────────────────────────────────────────────

    IEnumerator DelayThen(float seconds, System.Action action)
    {
        yield return new WaitForSeconds(seconds);
        action?.Invoke();
    }
}
