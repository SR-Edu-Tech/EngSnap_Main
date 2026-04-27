using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuizQuestionUI_BB1 : MonoBehaviour
{
    [Header("Question Display")]
    public GameObject          questionImageHolder;
    public Image               questionImage;
    public TextMeshProUGUI     questionTypeLabel;

    [Header("Option Buttons (exactly 3: A, B, C)")]
    public QuizOptionButton_BB1[] optionButtons;

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

    // Set by QuizManager_BB1 — no extra Inspector wiring needed
    [HideInInspector] public AudioClip correctFX;
    [HideInInspector] public AudioClip wrongFX;

    private QuizData_BB1.QuizQuestion currentQuestion;
    private Action<bool>              onAnswered;
    private bool                      answered      = false;
    private Coroutine                 flowCoroutine = null;

    private const float FEEDBACK_DISPLAY_DURATION = 2.2f;

    // ── Public API ────────────────────────────────────────────────────────

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
        HideFeedback();
        SetOptionsLocked(true);

        if (questionTypeLabel != null)
            questionTypeLabel.text = GetTypeDisplayName(currentQuestion.questionType);

        bool showImage = currentQuestion.questionImage != null;
        if (questionImageHolder != null) questionImageHolder.SetActive(showImage);
        if (showImage && questionImage != null)
            questionImage.sprite = currentQuestion.questionImage;

        var opts = new[]
        {
            currentQuestion.optionA,
            currentQuestion.optionB,
            currentQuestion.optionC
        };

        for (int i = 0; i < optionButtons.Length; i++)
        {
            optionButtons[i].Initialise(
                i,
                opts[i].optionText,
                opts[i].optionSprite,
                (pickedIndex) => OnOptionPicked(pickedIndex)
            );
        }

        if (replayAudioButton != null)
        {
            replayAudioButton.onClick.RemoveAllListeners();
            replayAudioButton.onClick.AddListener(ReplayQuestionAudio);
            replayAudioButton.gameObject.SetActive(true);
        }

        bool audioDone = false;

        if (currentQuestion.secondaryAudio != null)
            audioController.PlayVOChained(currentQuestion.questionAudio, currentQuestion.secondaryAudio, () => audioDone = true);
        else
            audioController.PlayVO(currentQuestion.questionAudio, () => audioDone = true);

        yield return new WaitUntil(() => audioDone);
        yield return new WaitForSeconds(0.2f);

        SetOptionsLocked(false);
        flowCoroutine = null;
    }

    private class BoolBox { public bool value; }

    void ReplayQuestionAudio()
    {
        if (answered) return;
        SetOptionsLocked(true);

        BoolBox done = new BoolBox();
        if (currentQuestion.secondaryAudio != null)
            audioController.PlayVOChained(currentQuestion.questionAudio, currentQuestion.secondaryAudio, () => done.value = true);
        else
            audioController.PlayVO(currentQuestion.questionAudio, () => done.value = true);

        StartCoroutine(WaitThenUnlock(done));
    }

    IEnumerator WaitThenUnlock(BoolBox done)
    {
        yield return new WaitUntil(() => done.value);
        yield return new WaitForSeconds(0.2f);
        if (!answered) SetOptionsLocked(false);
    }

    // ── Answer evaluation ─────────────────────────────────────────────────

    void OnOptionPicked(int pickedIndex)
    {
        if (answered) return;

        // ══ PLAY FX FIRST — before anything else, zero delay ══
        bool isCorrect = (pickedIndex == currentQuestion.correctOptionIndex);
        audioController.PlayFX(isCorrect ? correctFX : wrongFX);

        // Now do the rest
        answered = true;
        SetOptionsLocked(true);
        audioController.StopVO(); // stop question VO if still playing

        StartCoroutine(ShowAnswerFeedback(pickedIndex, isCorrect));
    }

    IEnumerator ShowAnswerFeedback(int pickedIndex, bool isCorrect)
    {
        if (isCorrect)
        {
            optionButtons[pickedIndex].ShowCorrect();
            ShowFeedback(true, "Correct! Well done!");
            if (starParticles != null) starParticles.Play();
        }
        else
        {
            optionButtons[pickedIndex].ShowWrong();
            yield return new WaitForSeconds(0.5f);
            optionButtons[currentQuestion.correctOptionIndex].ShowRevealCorrect();

            string revealText = string.IsNullOrEmpty(currentQuestion.wrongAnswerRevealText)
                ? "Not quite! The correct answer is highlighted."
                : "Not quite! " + currentQuestion.wrongAnswerRevealText;
            ShowFeedback(false, revealText);
        }

        yield return new WaitForSeconds(FEEDBACK_DISPLAY_DURATION);
        HideFeedback();
        onAnswered?.Invoke(isCorrect);
    }

    // ── Feedback helpers ──────────────────────────────────────────────────

    void ShowFeedback(bool correct, string message)
    {
        if (feedbackPanel != null) feedbackPanel.SetActive(true);
        if (feedbackText  != null) feedbackText.text  = message;

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