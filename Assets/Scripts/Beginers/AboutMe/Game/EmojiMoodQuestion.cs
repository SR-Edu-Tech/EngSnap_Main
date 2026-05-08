using UnityEngine;

/// <summary>
/// Data for one round of the Emoji Mood matching game.
///
/// INSPECTOR SETUP PER QUESTION:
///   moodText       — e.g. "I am happy!"
///   emojiSprites   — exactly 4 sprites (the emoji images, in any order)
///   correctIndex   — which slot (0-3) holds the correct emoji
///                    NOTE: positions are randomised at runtime, so this is
///                    the index inside emojiSprites[], not a screen position.
///   questionAudio  — clip that plays when the round starts (the mood phrase)
///   correctAudio   — clip played on a correct tap
///   wrongAudio     — clip played on a wrong tap
/// </summary>
[System.Serializable]
public class EmojiMoodQuestion
{
    [Header("Question Display")]
    [Tooltip("Mood phrase shown on the word card, e.g. 'I am happy!'")]
    public string moodText;

    [Header("Emoji Options")]
    [Tooltip("Exactly 4 emoji sprites. Positions are randomised each round.")]
    public Sprite[] emojiSprites = new Sprite[4];   // size must be 4

    [Tooltip("Index into emojiSprites[] that is the correct answer (0-3).")]
    public int correctIndex;

    [Header("Audio")]
    [Tooltip("Plays when the round loads (the mood phrase audio).")]
    public AudioClip questionAudio;
    [Tooltip("Plays on a correct tap.")]
    public AudioClip correctAudio;
    [Tooltip("Plays on a wrong tap.")]
    public AudioClip wrongAudio;
}
