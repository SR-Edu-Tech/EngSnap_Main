using UnityEngine;

/// <summary>
/// Contains all matching pairs for a single round.
/// </summary>
[System.Serializable]
public class MatchingRoundData_BB2
{
    [Tooltip("Display name of this round.")]
    public string roundName = "Round 1";

    [Tooltip("Word and illustration pairs for this round.")]
    public MatchingPair_BB2[] pairs = new MatchingPair_BB2[5];
}