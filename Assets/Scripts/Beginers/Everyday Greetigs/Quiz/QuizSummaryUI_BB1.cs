using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Summary screen shown after all quiz questions are answered.
///
/// Shows:
///   • Score text: "4 out of 5 correct!"
///   • Star row (one star per correct answer)
///   • Review grid: one button per question; tap to replay that question
///   • "Finish" button → calls onFinish
///   • "Replay All" button (optional) → calls onReplayAll
///
/// WIRING (Inspector):
///   scoreLabel        → TextMeshProUGUI for "X out of Y correct!"
///   starsContainer    → Transform; star icons are instantiated here
///   starFilledPrefab  → GameObject prefab for a filled star
///   starEmptyPrefab   → GameObject prefab for an empty/grey star
///   reviewContainer   → Transform; review buttons are instantiated here
///   reviewButtonPrefab→ GameObject prefab with a Button + TextMeshProUGUI child
///   finishButton      → "Continue" / "Finish" button
///   replayAllButton   → optional "Replay Quiz" button
/// </summary>
public class QuizSummaryUI_BB1 : MonoBehaviour
{
    [Header("Score Display")]
    public TextMeshProUGUI scoreLabel;
    public TextMeshProUGUI encouragementLabel; // "Great job!" etc.

    [Header("Stars")]
    public Transform  starsContainer;
    public GameObject starFilledPrefab;
    public GameObject starEmptyPrefab;

    [Header("Review Buttons")]
    public Transform  reviewContainer;
    public GameObject reviewButtonPrefab; // needs Button + TextMeshProUGUI child named "Label"

    [Header("Action Buttons")]
    public Button finishButton;
    public Button replayAllButton;

    // ── Callbacks ─────────────────────────────────────────────────────────
    private Action         onFinish;
    private Action<int>    onReviewQuestion; // int = questionIndex
    private Action         onReplayAll;

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>
    /// Populate and show the summary screen.
    /// </summary>
    /// <param name="correctCount">Number of correct answers.</param>
    /// <param name="totalCount">Total number of questions.</param>
    /// <param name="questionLabels">Short label for each question's review button (e.g. "Q1", "Q2"…).</param>
    /// <param name="onFinishCallback">Called when the player taps Finish.</param>
    /// <param name="onReviewCallback">Called with question index when player taps a review button.</param>
    /// <param name="onReplayAllCallback">Called when player taps Replay All (may be null).</param>
    public void Show(
        int                correctCount,
        int                totalCount,
        List<string>       questionLabels,
        Action             onFinishCallback,
        Action<int>        onReviewCallback,
        Action             onReplayAllCallback = null)
    {
        onFinish         = onFinishCallback;
        onReviewQuestion = onReviewCallback;
        onReplayAll      = onReplayAllCallback;

        gameObject.SetActive(true);

        BuildScore(correctCount, totalCount);
        BuildStars(correctCount, totalCount);
        BuildReviewButtons(questionLabels, totalCount);
        WireActionButtons();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    // ── Build helpers ─────────────────────────────────────────────────────

    void BuildScore(int correct, int total)
    {
        if (scoreLabel != null)
            scoreLabel.text = $"{correct} out of {total} correct!";

        if (encouragementLabel != null)
        {
            float pct = total > 0 ? (float)correct / total : 0f;
            if      (pct >= 1.0f) encouragementLabel.text = "Perfect score! Outstanding!";
            else if (pct >= 0.8f) encouragementLabel.text = "Great job! Almost perfect!";
            else if (pct >= 0.6f) encouragementLabel.text = "Well done! Keep practising!";
            else if (pct >= 0.4f) encouragementLabel.text = "Good effort! You are improving!";
            else                  encouragementLabel.text = "Keep going! Practice makes perfect!";
        }
    }

    void BuildStars(int correct, int total)
    {
        if (starsContainer == null) return;

        // Clear existing
        foreach (Transform child in starsContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < total; i++)
        {
            bool filled = i < correct;
            GameObject prefab = filled ? starFilledPrefab : starEmptyPrefab;
            if (prefab != null)
                Instantiate(prefab, starsContainer);
        }
    }

    void BuildReviewButtons(List<string> labels, int total)
    {
        if (reviewContainer == null || reviewButtonPrefab == null) return;

        // Clear existing
        foreach (Transform child in reviewContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < total; i++)
        {
            int capturedIndex = i;
            GameObject obj    = Instantiate(reviewButtonPrefab, reviewContainer);

            // Set label — look for a TextMeshProUGUI named "Label" or just the first one
            TextMeshProUGUI lbl = obj.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            if (lbl == null) lbl = obj.GetComponentInChildren<TextMeshProUGUI>();
            if (lbl != null)
                lbl.text = (labels != null && i < labels.Count) ? labels[i] : $"Q{i + 1}";

            Button btn = obj.GetComponent<Button>();
            if (btn == null) btn = obj.AddComponent<Button>();
            btn.onClick.AddListener(() => onReviewQuestion?.Invoke(capturedIndex));
        }
    }

    void WireActionButtons()
    {
        if (finishButton != null)
        {
            finishButton.onClick.RemoveAllListeners();
            finishButton.onClick.AddListener(() => onFinish?.Invoke());
        }

        if (replayAllButton != null)
        {
            replayAllButton.onClick.RemoveAllListeners();
            if (onReplayAll != null)
            {
                replayAllButton.gameObject.SetActive(true);
                replayAllButton.onClick.AddListener(() => onReplayAll?.Invoke());
            }
            else
            {
                replayAllButton.gameObject.SetActive(false);
            }
        }
    }
}
