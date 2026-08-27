using System;
using UnityEngine;

[Serializable]
public class WhichLetterChoice
{
    public string letterChoice; // e.g. "b"
    public Sprite choiceSprite;
}

[CreateAssetMenu(fileName = "NewWhichLetterData", menuName = "EngSnap/Unit 2/Which Letter Data")]
public class WhichLetterData : ScriptableObject
{
    [Header("Picture Prompt")]
    [Tooltip("Target word, e.g., 'ball', 'apple', 'dog'.")]
    public string keywordWord = "ball";

    [Tooltip("Picture sprite shown for this round.")]
    public Sprite keywordSprite;

    [Tooltip("Voice prompt for this round, e.g. 'Which letter does ball start with?'")]
    public AudioClip promptAudioClip;

    [Header("Answer Config")]
    [Tooltip("The correct starting letter, e.g., 'B' or 'b'.")]
    public string targetLetter = "b";

    [Tooltip("The 3 choice options for this round.")]
    public WhichLetterChoice[] choices;

    [Tooltip("Audio praise played on correct choice, e.g. 'Yes! Ball starts with B!'")]
    public AudioClip praiseAudioClip;
}
