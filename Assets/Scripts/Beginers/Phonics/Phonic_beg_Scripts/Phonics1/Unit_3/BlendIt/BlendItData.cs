using UnityEngine;

[CreateAssetMenu(fileName = "NewBlendItData", menuName = "EngSnap/Unit 3/Blend It Data")]
public class BlendItData : ScriptableObject
{
    [Header("Word Config")]
    [Tooltip("The full blended target word, e.g. 'cat', 'pin', 'dog', 'sun'.")]
    public string targetWord = "cat";

    [Tooltip("The 3 separate letter sounds, e.g. ['c', 'a', 't'].")]
    public string[] phonemeLetters = new string[] { "c", "a", "t" };

    [Tooltip("Displayed phoneme text for boxes, e.g. ['kuh', 'ae', 'tuh'].")]
    public string[] phonemeDisplayTexts = new string[] { "kuh", "ae", "tuh" };

    [Header("Audio Assets")]
    [Tooltip("3 separate pure phoneme audio clips for each box.")]
    public AudioClip[] phonemeClips;

    [Tooltip("Full blended word audio clip (e.g. 'cat'). Label: blend_word_cat.mp3")]
    public AudioClip blendedWordClip;

    [Tooltip("Mascot celebration line audio clip, e.g. 'kuh-ae-tuh... cat! You made a word!'")]
    public AudioClip mascotCelebrationClip;

    [Header("Art Assets")]
    [Tooltip("Keyword picture sprite for the blended word.")]
    public Sprite wordSprite;
}
