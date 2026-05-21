using UnityEngine;

/// <summary>
/// Data containers for the Matching Game.
/// These are plain serializable classes — no MonoBehaviour needed.
/// </summary>

[System.Serializable]
public class MatchingPair
{
    [Tooltip("The action word shown on the left, e.g. 'sleep'")]
    public string wordLabel;

    [Tooltip("The sprite shown on the RIGHT side for this word's CORRECT illustration")]
    public Sprite correctIllustrationSprite;

    [Tooltip("Optional: VO clip to play when the word label is tapped")]
    public AudioClip wordAudioClip;
}

[System.Serializable]
public class MatchingRoundData
{
    [Tooltip("Round display name, e.g. 'Round 1'")]
    public string roundName = "Round 1";

    [Tooltip("5 word+illustration pairs for this round")]
    public MatchingPair[] pairs = new MatchingPair[5];
}
