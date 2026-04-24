using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Displays one quiz question at a time.
/// Called by QuizManager_BB1 to show each question, collect the player's answer,
/// then report back via the onAnswered callback.
///
/// WIRING (Inspector):
///   questionImageHolder  → GameObject that contains the question Image (hidden for audio-only types)
///   questionImage        → Image component showing the scene sprite
///   questionTypeLabel    → TextMeshProUGUI showing the question type string (optional, can be null)
///   optionButtons        → Array of exactly 3 QuizOptionButton_BB1 (A, B, C)
///   feedbackPanel        → GameObject shown after an answer (contains feedbackText)
///   feedbackText         → TextMeshProUGUI for "Correct!" or "Not quite..." text
///   feedbackIcon         → Image used for checkmark/cross icon in feedback panel
///   starParticles        → ParticleSystem played on correct answer (optional)
///   replayAudioButton    → Button to re-play the question audio (optional)
///   audioController      → QuizAudioController_BB1 on the same prefab or parent
///
/// Flow per question:
///   1. ShowQuestion() called by manager
///   2. Image shown (if applicable), options locked
///   3. Question VO plays automatically
///   4. Secondary audio plays if present (ListenAndPick)
///   5. Options unlocked
///   6. Player taps an option → EvaluateAnswer()
///   7. Feedback shown, onAnswered(isCorrect) called after delay
/// </summary>
public class QuizQuestionUI_BB1 : MonoBehaviour
{
    [Header("Question Display")]
    public GameObject          questionImageHolder;
    public Image               questionImage;
    public TextMeshProUGUI     questionTypeLabel;

    [Header("Option Buttons (exactly 3: A, B, C)")]
    public QuizOptionButton_BB1[] optionButtons; // length must be 3

    [Header("Feedback Panel")]
    public GameObject          feedbackPanel;
    public TextMeshProUGUI     feedbackText;
    public Image               feedbackIcon;
    public Sprite              feedbackCorrectSprite;
    public Sprite              feedbackWrongSprite;
    public Color               feedbackCorrectColor = new Color(0.2f, 0.75f, 0.3f, 1f);
    public Color               feedbackWrongColor   = new Color(0.85f, 0.2f, 0.2f, 1f);

    [Header("Star / Celebration")]
    public ParticleSystem      starParticles;

    [Header("Replay Button")]
    public Button              replayAudioButton;

    [Header("Audio Controller")]
    public QuizAudioController_BB1 audioController;

    // ── Internal state ────────────────────────────────────────────────────
    private QuizData_BB1.QuizQuestion currentQuestion;
    private Action<bool>              onAnswered;   // bool = wasCorrect
    private bool                      answered       = false;
    private Coroutine                 flowCoroutine  = null;

    // Delay after showing feedback before calling onAnswered
    private const float FEEDBACK_DISPLAY_DURATION = 2.2f;

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Display a question. onAnswered is called with (wasCorrect) after feedback is shown.
    /// </summary>
    public void ShowQuestion(QuizData_BB1.QuizQuestion question, Action<bool> onAnsweredCallback)
    {
        currentQuestion = question;
        onAnswered      = onAnsweredCallback;
        answered        = false;

        if (flowCoroutine != null) StopCoroutine(flowCoroutine);
        flowCoroutine = StartCoroutine(QuestionFlow());
    }

    // ── Flow ─────────────────────────────────────────────────────────────

    IEnumerator QuestionFlow()
    {
        // --- Reset UI ---
        HideFeedback();
        SetOptionsLocked(true);

        // --- Question type label ---
        if (questionTypeLabel != null)
            questionTypeLabel.text = GetTypeDisplayName(currentQuestion.questionType);

        // --- Image ---
        bool showImage = currentQuestion.questionImage != null;
        if (questionImageHolder != null) questionImageHolder.SetActive(showImage);
        if (showImage && questionImage != null)
            questionImage.sprite = currentQuestion.questionImage;

        // --- Populate options ---
        var opts = new[]
        {
            currentQuestion.optionA,
            currentQuestion.optionB,
            currentQuestion.optionC
        };

        for (int i = 0; i < optionButtons.Length; i++)
        {
            int capturedIndex = i;
            optionButtons[i].Initialise(
                i,
                opts[i].optionText,
                opts[i].optionSprite,
                (pickedIndex) => OnOptionPicked(pickedIndex)
            );
        }

        // --- Replay button ---
        if (replayAudioButton != null)
        {
            replayAudioButton.onClick.RemoveAllListeners();
            replayAudioButton.onClick.AddListener(ReplayQuestionAudio);
            replayAudioButton.gameObject.SetActive(true);
        }

        // --- Play question VO ---
        bool audioDone = false;

        if (currentQuestion.secondaryAudio != null)
        {
            // Chain primary + secondary (ListenAndPick type)
            audioController.PlayVOChained(
                currentQuestion.questionAudio,
                currentQuestion.secondaryAudio,
                () => audioDone = true
            );
        }
        else
        {
            audioController.PlayVO(currentQuestion.questionAudio, () => audioDone = true);
        }

        // Wait for all audio to finish before unlocking options
        yield return new WaitUntil(() => audioDone);
        yield return new WaitForSeconds(0.2f); // tiny pause for polish

        // --- Unlock options ---
        SetOptionsLocked(false);
        flowCoroutine = null;
    }

    // Simple heap-allocated bool so lambdas and coroutines can share it
    // without needing ref/out (which C# forbids inside iterators and lambdas).
    private class BoolBox { public bool value; }

    void ReplayQuestionAudio()
    {
        if (answered) return;
        SetOptionsLocked(true);

        BoolBox audioDone = new BoolBox();
        if (currentQuestion.secondaryAudio != null)
            audioController.PlayVOChained(currentQuestion.questionAudio, currentQuestion.secondaryAudio, () => audioDone.value = true);
        else
            audioController.PlayVO(currentQuestion.questionAudio, () => audioDone.value = true);

        StartCoroutine(WaitThenUnlock(audioDone));
    }

    IEnumerator WaitThenUnlock(BoolBox audioDone)
    {
        yield return new WaitUntil(() => audioDone.value);
        yield return new WaitForSeconds(0.2f);
        if (!answered) SetOptionsLocked(false);
    }

    // ── Answer evaluation ─────────────────────────────────────────────────

    void OnOptionPicked(int pickedIndex)
    {
        if (answered) return;
        answered = true;
        SetOptionsLocked(true);

        bool isCorrect = (pickedIndex == currentQuestion.correctOptionIndex);
        StartCoroutine(ShowAnswerFeedback(pickedIndex, isCorrect));
    }

    IEnumerator ShowAnswerFeedback(int pickedIndex, bool isCorrect)
    {
        if (isCorrect)
        {
            // Correct
            optionButtons[pickedIndex].ShowCorrect();
            audioController.PlayFX(null); // FX played by manager via correctVO
            ShowFeedback(true, "Correct! Well done!");

            if (starParticles != null)
                starParticles.Play();
        }
        else
        {
            // Wrong — shake picked button, reveal correct button
            optionButtons[pickedIndex].ShowWrong();

            // Short pause before revealing correct
            yield return new WaitForSeconds(0.5f);
            optionButtons[currentQuestion.correctOptionIndex].ShowRevealCorrect();

            string revealText = string.IsNullOrEmpty(currentQuestion.wrongAnswerRevealText)
                ? "Not quite! The correct answer is highlighted."
                : "Not quite! " + currentQuestion.wrongAnswerRevealText;
            ShowFeedback(false, revealText);
        }

        // Let player read feedback
        yield return new WaitForSeconds(FEEDBACK_DISPLAY_DURATION);

        HideFeedback();
        onAnswered?.Invoke(isCorrect);
    }

    // ── Feedback panel helpers ────────────────────────────────────────────

    void ShowFeedback(bool correct, string message)
    {
        if (feedbackPanel != null) feedbackPanel.SetActive(true);
        if (feedbackText  != null) feedbackText.text = message;

        if (feedbackIcon != null)
        {
            feedbackIcon.sprite = correct ? feedbackCorrectSprite : feedbackWrongSprite;
            feedbackIcon.color  = correct ? feedbackCorrectColor  : feedbackWrongColor;
        }

        if (feedbackText != null)
            feedbackText.color = correct ? feedbackCorrectColor : feedbackWrongColor;
    }

    void HideFeedback()
    {
        if (feedbackPanel != null) feedbackPanel.SetActive(false);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    void SetOptionsLocked(bool locked)
    {
        foreach (var btn in optionButtons)
            btn.SetLocked(locked);
    }

    string GetTypeDisplayName(QuizData_BB1.QuestionType type)
    {
        switch (type)
        {
            case QuizData_BB1.QuestionType.ImageAudioChoice:  return "Image + Audio Choice";
            case QuizData_BB1.QuestionType.ListenAndPick:     return "Listen and Pick";
            case QuizData_BB1.QuestionType.FillInResponse:    return "Fill in the Response";
            case QuizData_BB1.QuestionType.SceneMatch:        return "Scene Match";
            case QuizData_BB1.QuestionType.DialogueComplete:  return "Dialogue Complete";
            default: return "";
        }
    }
}