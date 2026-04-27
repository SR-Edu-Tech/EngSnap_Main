using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Master controller for the BB1 Quiz gameplay screen.
/// Passes correctFX / wrongFX clips from QuizData to QuizQuestionUI_BB1
/// so they can be played instantly on button tap.
/// </summary>
public class QuizManager_BB1 : MonoBehaviour
{
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
    public TextMeshProUGUI questionCounter;
    public Slider          progressBar;
    public TextMeshProUGUI liveScoreLabel;

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
    private bool inReviewMode         = false;

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
        string uid = (unitButton != null && !string.IsNullOrEmpty(unitButton.unitID))
                     ? unitButton.unitID
                     : "default";

        saveKey_progress = uid + "_quizProgress";
        saveKey_score    = uid + "_quizScore";
        saveKey_done     = uid + "_quizDone";

        bool wasCompleted = PlayerPrefs.GetInt(saveKey_done, 0) == 1;

        if (wasCompleted)
        {
            currentQuestionIndex = 0;
            correctCount         = 0;
            ClearSaveData();
        }
        else
        {
            currentQuestionIndex = PlayerPrefs.GetInt(saveKey_progress, 0);
            correctCount         = PlayerPrefs.GetInt(saveKey_score, 0);
        }

        quizCompleted = false;
        inReviewMode  = false;

        // ── Pass FX clips to QuestionUI so it can play them instantly on tap ──
        if (questionUI != null && quizData != null)
        {
            questionUI.correctFX = quizData.correctFX;
            questionUI.wrongFX   = quizData.wrongFX;
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (summaryUI != null) summaryUI.Hide();

        StartCoroutine(PlayIntroThenStart());
    }

    // ── Intro ─────────────────────────────────────────────────────────────

    IEnumerator PlayIntroThenStart()
    {
        if (audioController != null && quizData != null)
            audioController.PlayBGM(quizData.bgmClip);

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
            // correctVO plays AFTER the instant correctFX — manager handles the VO
            if (audioController != null && quizData != null)
                audioController.PlayVO(quizData.correctVO);
        }
        else
        {
            // wrongVO plays AFTER the instant wrongFX
            if (audioController != null && quizData != null)
                audioController.PlayVO(quizData.wrongVO);
        }

        if (inReviewMode)
        {
            StartCoroutine(DelayThen(1.5f, ReturnToSummary));
            return;
        }

        currentQuestionIndex++;
        SaveProgress();
        StartCoroutine(DelayThen(1.5f, ShowCurrentQuestion));
    }

    // ── Summary / Finish ──────────────────────────────────────────────────

    void FinishQuiz()
    {
        quizCompleted = true;
        PlayerPrefs.SetInt(saveKey_done, 1);
        ClearSaveData(keepDone: true);

        if (audioController != null && quizData != null)
            audioController.PlayVO(quizData.endVO);

        var labels = new List<string>();
        if (quizData != null)
            for (int i = 0; i < quizData.questions.Count; i++)
                labels.Add($"Q{i + 1}");

        if (summaryUI != null)
        {
            questionUI.gameObject.SetActive(false);
            summaryUI.Show(
                correctCount,
                quizData != null ? quizData.questions.Count : 0,
                labels,
                onFinishCallback:    OnSummaryFinish,
                onReviewCallback:    OnReviewQuestion,
                onReplayAllCallback: OnReplayAll
            );
        }

        UpdateHUD();
    }

    void OnSummaryFinish()
    {
        if (audioController != null) audioController.StopAll();

        if (panel != null && unitButton != null)
            panel.UnitFinished(unitButton);
        else
            gameObject.SetActive(false);
    }

    void OnReviewQuestion(int questionIndex)
    {
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
        if (!quizCompleted) SaveProgress();

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
            questionCounter.text = quizCompleted
                ? "Quiz Complete!"
                : $"Question {currentQuestionIndex + 1} of {total}";

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