using UnityEngine;

[CreateAssetMenu(fileName = "NewNameVsSoundData", menuName = "EngSnap/Unit 3/Name vs Sound Data")]
public class NameVsSoundData : ScriptableObject
{
    [Header("Letter Config")]
    [Tooltip("The letter string, e.g. 'B', 'C', 'M', 'S'.")]
    public string letter = "B";

    [Tooltip("Displayed letter name string, e.g. 'bee', 'cee', 'em', 'ess'.")]
    public string letterNameText = "bee";

    [Tooltip("Displayed letter sound string, e.g. 'buh', 'kuh', 'mmm', 'sss'.")]
    public string letterSoundText = "buh";

    [Header("Audio Clips")]
    [Tooltip("Audio clip for the letter NAME (e.g., 'bee'). Reused from Unit 2.")]
    public AudioClip letterNameClip;

    [Tooltip("Audio clip for the pure letter SOUND (phoneme, e.g., 'buh').")]
    public AudioClip letterSoundClip;

    [Tooltip("Mascot commentary clip reinforcing the distinction, e.g. 'The name is bee, but the sound is buh!'")]
    public AudioClip mascotReinforcementClip;

    [Tooltip("Subtitles for mascot reinforcement.")]
    [TextArea(2, 4)]
    public string reinforcementSubtitles = "The name is 'bee', but the sound is 'buh'. Clever!";
}
