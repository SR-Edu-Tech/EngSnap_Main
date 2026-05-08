using UnityEngine;

/// <summary>
/// Bridge — Phase 1 → Phase 2
///
/// Takes the four answers filled in IntroCard_BB1, builds four dynamic
/// SpeakingQuestion entries in Logic_BB1.questions, then switches panels.
///
/// NO changes to Logic_BB1 or CrossPlatformSpeechManager_BB1 are required.
///
/// ── Inspector wiring ────────────────────────────────────────────────────────
///   introCardPanel   — Panel shown during Phase 1 (fill-in-blanks)
///   speechGamePanel  — Panel shown during Phase 2 (the existing speech game)
///   logicBB1         — The Logic_BB1 component on the speech game panel
///
/// Sentence templates use {0} for the player's answer.
/// Customise templateLines[] in the Inspector if you want different sentences.
///
/// ★ questionAudioClips[] — one AudioClip per template line.
///   The clip is passed straight into SpeakingQuestion.questionAudio so
///   Logic_BB1 can auto-play it and the Replay button works as normal.
///   Leave any element null if you have no audio for that sentence.
/// </summary>
public class IntroCardToSpeech_BB1 : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────────────

    [Header("── Panels ───────────────────────────────────")]
    [Tooltip("The fill-in-blank card panel (Phase 1)")]
    public GameObject introCardPanel;

    [Tooltip("The speech game panel (Phase 2)")]
    public GameObject speechGamePanel;

    [Header("── Logic Reference ─────────────────────────")]
    [Tooltip("The Logic_BB1 component driving the speech game")]
    public Logic_BB1 logicBB1;

    [Header("── Sentence Templates ──────────────────────")]
    [Tooltip("Four templates — {0} is replaced by the player's answer.\n" +
             "Order must match: Name, Age, City, Like.")]
    public string[] templateLines = new string[]
    {
        "Hello! My name is {0}.",
        "I am {0} years old.",
        "I live in {0}.",
        "I like {0}."
    };

    [Header("★ Question Audio Clips ─────────────────────")]
    [Tooltip("One AudioClip per template line (same order: Name, Age, City, Like).\n" +
             "Passed into SpeakingQuestion.questionAudio so Logic_BB1 auto-plays it\n" +
             "and the Replay button works out of the box.\n" +
             "Leave any slot empty if you have no audio for that sentence.")]
    public AudioClip[] questionAudioClips = new AudioClip[4];

    // ── Public API ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by IntroCard_BB1's Proceed button.
    /// Populates Logic_BB1.questions with the four dynamic sentences (+ audio)
    /// then activates the speech game panel.
    /// </summary>
    public void BuildAndLaunch(string name, string age, string city, string like)
    {
        if (logicBB1 == null)
        {
            Debug.LogError("[IntroCardToSpeech_BB1] logicBB1 reference is missing!");
            return;
        }

        // Ensure we have exactly 4 templates
        if (templateLines == null || templateLines.Length < 4)
        {
            templateLines = new string[]
            {
                "Hello! My name is {0}.",
                "I am {0} years old.",
                "I live in {0}.",
                "I like {0}."
            };
        }

        // Ensure the audio array is at least length 4 (pad with nulls if designer
        // left it shorter — Logic_BB1 handles null questionAudio gracefully)
        if (questionAudioClips == null || questionAudioClips.Length < 4)
        {
            var padded = new AudioClip[4];
            if (questionAudioClips != null)
                for (int i = 0; i < questionAudioClips.Length && i < 4; i++)
                    padded[i] = questionAudioClips[i];
            questionAudioClips = padded;
        }

        string[] answers = { name, age, city, like };

        // Build 4 SpeakingQuestion objects with dynamic target text + audio
        var questions = new Logic_BB1.SpeakingQuestion[4];
        for (int i = 0; i < 4; i++)
        {
            questions[i] = new Logic_BB1.SpeakingQuestion
            {
                targetText    = string.Format(templateLines[i], answers[i]),
                questionAudio = questionAudioClips[i]   // ★ now populated
            };

            Debug.Log($"[IntroCardToSpeech_BB1] Q{i + 1}: \"{questions[i].targetText}\" " +
                      $"| audio={(questions[i].questionAudio != null ? questions[i].questionAudio.name : "none")}");
        }

        // Inject into Logic_BB1
        logicBB1.questions = questions;

        // Reset speech game to start from Q1 with the new sentences
        logicBB1.ResetGame();

        // Switch panels
        if (introCardPanel  != null) introCardPanel.SetActive(false);
        if (speechGamePanel != null) speechGamePanel.SetActive(true);
    }

    // ── Convenience overload (call from Inspector Button if needed) ─────────────

    /// <summary>
    /// Overload that reads answers from an IntroCard_BB1 on the same GameObject.
    /// Wire to a Button's OnClick if you don't use the auto-wire inside IntroCard_BB1.
    /// </summary>
    public void BuildAndLaunchFromCard()
    {
        var card = GetComponent<IntroCard_BB1>()
                ?? introCardPanel?.GetComponent<IntroCard_BB1>();

        if (card == null)
        {
            Debug.LogError("[IntroCardToSpeech_BB1] Cannot find IntroCard_BB1 component.");
            return;
        }

        var (n, a, c, l) = card.GetFilledAnswers();
        BuildAndLaunch(n, a, c, l);
    }
}