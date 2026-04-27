using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject that holds all question data for one Quiz screen.
///
/// CREATE: Right-click in Project → Create → BB1 → Quiz Data
/// </summary>
[CreateAssetMenu(fileName = "QuizData_BB1", menuName = "BB1/Quiz Data")]
public class QuizData_BB1 : ScriptableObject
{
    [System.Serializable]
    public enum QuestionType
    {
        ImageAudioChoice,
        ListenAndPick,
        FillInResponse,
        SceneMatch,
        DialogueComplete
    }

    [System.Serializable]
    public class QuizOption
    {
        [Tooltip("Text shown on the option button.")]
        public string optionText;

        [Tooltip("Optional: icon/image to show on this option button (leave null for text-only).")]
        public Sprite optionSprite;
    }

    [System.Serializable]
    public class QuizQuestion
    {
        [Header("Type")]
        public QuestionType questionType;

        [Header("Question Audio")]
        [Tooltip("VO clip that reads the question aloud.")]
        public AudioClip questionAudio;

        [Tooltip("Optional secondary audio (e.g. the 'Leo says Good afternoon!' clip).")]
        public AudioClip secondaryAudio;

        [Header("Question Image")]
        public Sprite questionImage;

        [Header("Options (always 3)")]
        public QuizOption optionA;
        public QuizOption optionB;
        public QuizOption optionC;

        [Header("Correct Answer")]
        [Range(0, 2)]
        public int correctOptionIndex;

        [Header("Wrong Answer Explanation")]
        public string wrongAnswerRevealText;
    }

    [Header("Questions (add in order)")]
    public List<QuizQuestion> questions = new List<QuizQuestion>();

    [Header("Global Audio Clips")]
    [Tooltip("Looping background music during the quiz.")]
    public AudioClip bgmClip;

    [Tooltip("Intro VO: 'Quiz time! Let us see what you remember...'")]
    public AudioClip introVO;

    [Tooltip("Correct feedback VO played by manager after question resolves (e.g. 'Correct! Well done!').")]
    public AudioClip correctVO;

    [Tooltip("Wrong feedback VO played by manager after question resolves (e.g. 'Not quite...').")]
    public AudioClip wrongVO;

    [Tooltip("End VO: 'Quiz complete! You are a Greetings Champion!'")]
    public AudioClip endVO;

    // ── NEW: Instant feedback sound effects ──────────────────────────────────

    [Header("Instant Feedback SFX")]
    [Tooltip("Short sound effect played IMMEDIATELY when the player taps the CORRECT option " +
             "(e.g. a chime, bell, or 'ding'). Plays before the correctVO.")]
    public AudioClip correctFX;

    [Tooltip("Short sound effect played IMMEDIATELY when the player taps a WRONG option " +
             "(e.g. a buzzer or 'thud'). Plays before the wrongVO.")]
    public AudioClip wrongFX;
}