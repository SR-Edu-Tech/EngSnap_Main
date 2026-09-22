using System;
using UnityEngine;

[Serializable]
public class SoundAndLetterChoice
{
    [Tooltip("The letter string or word displayed on the card (e.g. 'm', 'sun').")]
    public string letter;

    [Tooltip("Optional picture sprite if this choice displays an image (e.g. sun image).")]
    public Sprite imageSprite;

    [Tooltip("True if this choice displays a picture image instead of letter text.")]
    public bool isPictureCard = false;

    [Tooltip("Audio clip for this letter sound or word.")]
    public AudioClip soundClip;
}

[CreateAssetMenu(menuName = "Phonics/Sound And Letter Round Data", fileName = "NewSoundAndLetterRound")]
public class SoundAndLetterData : ScriptableObject
{
    [Header("Round Configuration")]
    public string roundName = "Round 1";

    [Tooltip("Mascot prompt text (e.g. 'Tap the letter that says... aaa!' or 'Which picture starts with... sss?').")]
    public string promptText = "Tap the letter that says...";

    [Tooltip("Prompt voice audio clip.")]
    public AudioClip promptAudioClip;

    [Tooltip("The target letter character or word the child needs to find (e.g. 'm').")]
    public string targetLetter = "m";

    [Tooltip("The target phonetic sound audio clip played to the child (e.g. 'mmm').")]
    public AudioClip targetSoundClip;

    [Tooltip("The 3 choice cards shown on screen for this round.")]
    public SoundAndLetterChoice[] choices;

    [Tooltip("Optional custom praise clip for this specific round.")]
    public AudioClip roundPraiseClip;
}
