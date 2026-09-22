using UnityEngine;

[CreateAssetMenu(fileName = "NewMeetLettersData", menuName = "EngSnap/Unit 2/Meet Letters Data")]
public class MeetLettersData : ScriptableObject
{
    [Header("Letter Display")]
    [Tooltip("The letter pair displayed on the card, e.g., 'Aa', 'Bb', 'Cc'.")]
    public string letterPair = "Aa";

    [Tooltip("The letter name, e.g., 'ay', 'bee', 'cee', 'dee'.")]
    public string letterName = "ay";

    [Header("Audio Clips")]
    [Tooltip("Audio clip pronouncing the letter NAME (e.g. 'ay', 'bee').")]
    public AudioClip letterNameAudio;

    [Tooltip("Audio clip for letter name + keyword (e.g. 'ay - apple').")]
    public AudioClip letterNameAndWordAudio;

    [Header("Keyword Picture & Word")]
    [Tooltip("Keyword word, e.g., 'apple', 'ball', 'cat'.")]
    public string keywordWord = "apple";

    [Tooltip("Sprite of the keyword picture.")]
    public Sprite keywordSprite;

    [Tooltip("Audio clip pronouncing the keyword word.")]
    public AudioClip keywordAudio;
}
