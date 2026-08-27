using UnityEngine;

[CreateAssetMenu(fileName = "NewBigAndSmallMatchData", menuName = "EngSnap/Unit 2/Big & Small Match Data")]
public class BigAndSmallMatchData : ScriptableObject
{
    [Header("Letter Pair")]
    [Tooltip("Capital letter, e.g., 'B'.")]
    public string capitalLetter = "B";

    [Tooltip("Small letter, e.g., 'b'.")]
    public string smallLetter = "b";

    [Header("Audio")]
    [Tooltip("Voice prompt for this capital letter, e.g. 'Find the small letter for... big B!'")]
    public AudioClip promptAudioClip;

    [Tooltip("Audio clip played when pair is correctly matched, e.g. 'Yes! Big B and small b are partners!'")]
    public AudioClip matchPraiseAudioClip;
}
