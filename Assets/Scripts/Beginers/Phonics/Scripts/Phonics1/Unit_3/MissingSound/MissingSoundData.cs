using UnityEngine;

[CreateAssetMenu(fileName = "NewMissingSoundData", menuName = "EngSnap/Unit 3/Missing Sound Data")]
public class MissingSoundData : ScriptableObject
{
    [Header("Word & Final Sound Config")]
    [Tooltip("Target completed word, e.g. 'web', 'tap', 'cat', 'box', 'bat', 'bed', 'mug', 'dog', 'rat'.")]
    public string completedWord = "web";

    [Tooltip("Displayed partial word missing the last letter, e.g. 'we_'.")]
    public string partialWord = "we_";

    [Tooltip("The correct final letter, e.g. 'b'.")]
    public string correctLetter = "b";

    [Tooltip("Choice options array (including correctLetter), e.g. ['b', 't', 'n'].")]
    public string[] choiceLetters = new string[] { "b", "t", "n" };

    [Header("Audio Clips")]
    [Tooltip("Mascot round prompt audio clip emphasising final sound, e.g. 'web... what is the last sound? buh!'")]
    public AudioClip roundPromptClip;

    [Tooltip("Audio clip for the pure correct final phoneme (e.g. 'buh').")]
    public AudioClip correctFinalPhonemeClip;

    [Tooltip("Full completed word audio clip (e.g. 'web'). Label: word_web.mp3")]
    public AudioClip completedWordClip;

    [Header("Art Assets")]
    [Tooltip("Keyword picture sprite for the missing sound target.")]
    public Sprite keywordSprite;
}
