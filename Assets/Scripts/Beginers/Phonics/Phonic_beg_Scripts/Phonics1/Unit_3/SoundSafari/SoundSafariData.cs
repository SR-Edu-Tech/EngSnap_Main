using UnityEngine;

[CreateAssetMenu(fileName = "NewSoundSafariData", menuName = "EngSnap/Unit 3/Sound Safari Data")]
public class SoundSafariData : ScriptableObject
{
    [Header("Letter & Keyword Config")]
    [Tooltip("The letter string, e.g. 'A', 'B', 'C'...")]
    public string letter = "A";

    [Tooltip("The pure phoneme text, e.g. 'ae', 'buh', 'kuh'...")]
    public string phonemeText = "ae";

    [Tooltip("The keyword word string, e.g. 'apple', 'bike', 'cake'...")]
    public string keyword = "apple";

    [Header("Art & Audio Assets")]
    [Tooltip("Sprite illustration of the keyword picture.")]
    public Sprite keywordSprite;

    [Tooltip("Audio clip for sound + word (e.g. 'buh - bike'). Label: b_snd_word.mp3")]
    public AudioClip soundAndWordClip;

    [Tooltip("Optional pure phoneme audio clip if needed separately.")]
    public AudioClip purePhonemeClip;
}
