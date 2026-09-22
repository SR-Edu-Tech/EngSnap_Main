using System;
using UnityEngine;

/// <summary>
/// Clean serializable data container for Unit 6 Listening L02 "Real or Literal? — Don't Be Fooled".
/// Holds sentence text, literal art, real meaning representation, VO audio triggers, and hint content.
/// </summary>
[Serializable]
public class RealOrLiteralRoundData
{
    [Header("Audio Identifiers")]
    [Tooltip("Unique VO trigger ID for sentence audio (e.g. VO_L02_SENT_1)")]
    public string audioId;

    [Tooltip("Direct AudioClip reference for sentence voiceover (fallback if audioId not in Audio Bank)")]
    public AudioClip sentenceAudio;

    [Tooltip("Direct AudioClip reference for reveal voiceover (ARIA explaining the idiom)")]
    public AudioClip revealAudio;

    [Header("Idiom Text")]
    [Tooltip("The spoken idiom sentence")]
    public string sentenceText;

    [Header("Interpretations")]
    [Tooltip("Sprite reference for the funny/wrong literal option (LEFT Easel)")]
    public Sprite literalArt;

    [Tooltip("Key or description for the correct figurative meaning (RIGHT Easel)")]
    public string realMeaningKey;

    [Tooltip("Sprite reference for the real figurative meaning (RIGHT Easel)")]
    public Sprite realMeaningArt;

    [Header("Hint")]
    [Tooltip("Hint text displayed when player selects LITERAL on first attempt")]
    public string hintText;
}
