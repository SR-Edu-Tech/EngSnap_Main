using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject that holds all question data for one Quiz screen.
///
/// CREATE: Right-click in Project → Create → BB1 → Quiz Data
///
/// QUESTION TYPES:
///   ImageAudioChoice  — shows an image, plays audio, pick from text options
///   ListenAndPick     — plays audio only (no image), pick from text options
///   FillInResponse    — plays a scenario audio, pick the correct reply
///   SceneMatch        — shows an image, pick the matching greeting
///   DialogueComplete  — plays a dialogue audio, pick what the character should say
///
/// All types share the same Option structure; only image/audio usage differs per type.
/// </summary>
[CreateAssetMenu(fileName = "QuizData_BB1", menuName = "BB1/Quiz Data")]
public class QuizData_BB1 : ScriptableObject
{
    [System.Serializable]
    public enum QuestionType
    {
        ImageAudioChoice,   // Q1 style: image + audio cue, pick text answer
        ListenAndPick,      // Q2 style: audio only, pick text answer
        FillInResponse,     // Q3 style: audio scenario, pick correct reply
        SceneMatch,         // Q4 style: image only, pick matching phrase
        DialogueComplete    // Q5 style: audio dialogue, pick character's response
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
        [Tooltip("VO clip that reads the question aloud. Played automatically when the question appears.")]
        public AudioClip questionAudio;

        [Tooltip("Optional secondary audio (e.g. the 'Leo says Good afternoon!' clip for ListenAndPick). " +
                 "Played after questionAudio finishes, before options are unlocked.")]
        public AudioClip secondaryAudio;

        [Header("Question Image")]
        [Tooltip("Scene/image shown for ImageAudioChoice and SceneMatch types. Leave null for audio-only types.")]
        public Sprite questionImage;

        [Header("Options (always 3)")]
        public QuizOption optionA;
        public QuizOption optionB;
        public QuizOption optionC;

        [Header("Correct Answer")]
        [Tooltip("0 = A, 1 = B, 2 = C")]
        [Range(0, 2)]
        public int correctOptionIndex;

        [Header("Wrong Answer Explanation")]
        [Tooltip("Text appended to the wrong-answer VO, e.g. '...the answer is: Good morning!'")]
        public string wrongAnswerRevealText;
    }

    [Header("Questions (add in order)")]
    public List<QuizQuestion> questions = new List<QuizQuestion>();

    [Header("Global Audio Clips")]
    [Tooltip("Looping background music during the quiz.")]
    public AudioClip bgmClip;

    [Tooltip("Intro VO: 'Quiz time! Let us see what you remember...'")]
    public AudioClip introVO;

    [Tooltip("Correct feedback VO: 'Correct! Well done!'")]
    public AudioClip correctVO;

    [Tooltip("Wrong feedback VO: 'Not quite — the answer is...'")]
    public AudioClip wrongVO;

    [Tooltip("End VO: 'Quiz complete! You are a Greetings Champion!'")]
    public AudioClip endVO;
}
