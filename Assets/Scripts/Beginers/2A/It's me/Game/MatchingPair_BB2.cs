using UnityEngine;

/// <summary>
/// Represents a single word + illustration pair.
/// </summary>
[System.Serializable]
public class MatchingPair_BB2
{
    [Tooltip("The word shown on the left.")]
    public string wordLabel;

    [Tooltip("The correct illustration for this word.")]
    public Sprite correctIllustrationSprite;

    [Tooltip("Optional audio clip played when the word is tapped.")]
    public AudioClip wordAudioClip;
}