using UnityEngine;

/// <summary>
/// Data for one Simon Says round.
/// Assign in the Inspector on SimonSaysController.
/// </summary>
[System.Serializable]
public class SimonRoundData
{
    [Tooltip("The command text Simon displays, e.g. 'Jump up high!'")]
    public string commandText;

    [Tooltip("Index into AudioManager.voCommands[] for the VO clip")]
    public int voCommandIndex = -1;

    [Tooltip("The action word that is the CORRECT answer, e.g. 'jump'")]
    public string correctActionWord;

    [Tooltip("Sprite for the correct answer card")]
    public Sprite correctSprite;

    [Tooltip("3 decoy action words")]
    public string[] decoyWords = new string[3];

    [Tooltip("Sprites matching decoyWords[0..2]")]
    public Sprite[] decoySprites = new Sprite[3];

    [Tooltip("Is this the speed round? (shows timer bar)")]
    public bool isSpeedRound = false;

    [Tooltip("Time limit in seconds for speed rounds")]
    public float speedRoundTime = 8f;
}
