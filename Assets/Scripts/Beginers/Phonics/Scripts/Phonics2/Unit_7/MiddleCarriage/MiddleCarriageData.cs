using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit7
{
    [Serializable]
    public class MiddleCarriageItem
    {
        public string fullWord = "fan";
        public string middleVowel = "a";
        public string firstLetter = "f";
        public string lastLetter = "n";
        public Sprite wordPictureSprite;
        public AudioClip normalWordAudio;         // "fan"
        public AudioClip middleStretchedWordAudio; // "faaaaan"
        public AudioClip segmentedWordAudio;       // "f - a - n"
        public AudioClip successVoiceClip;        // "Yes! Faaaaan. That is /a/ — Apple's house!"
    }

    [Serializable]
    public class OddOneOutVowelItem
    {
        public string[] wordChoices = new string[] { "cat", "jug", "fan" };
        public Sprite[] wordSprites = new Sprite[3];
        public AudioClip[] wordClips = new AudioClip[3];
        public string[] middleVowels = new string[] { "a", "u", "a" };
        public int oddChoiceIndex = 1; // "jug" has /u/, others have /a/
        public string explanationText = "'jug' has /u/, while 'cat' and 'fan' have /a/!";
    }

    [CreateAssetMenu(fileName = "MiddleCarriageData_Unit7", menuName = "EngSnap/Phonics2/Unit7/Middle Carriage Data")]
    public class MiddleCarriageData : ScriptableObject
    {
        [Header("Leo & Momo Voice Scripts")]
        public AudioClip leoIntroClip;          // "Now the hardest one — the MIDDLE. The middle sound likes to hide!"
        public AudioClip demonstrationClip;     // "Fan. f - a - n. Faaaaan. Hear it in the middle? /a/!"
        public AudioClip instructionClip;       // "Which vowel is hiding in the middle? Look at the five houses!"
        public AudioClip retryClip;             // "Let me stretch it for you..."
        public AudioClip momoStrategyClip;      // "Try each vowel and see which sounds right!"
        public AudioClip oddOneOutIntroClip;    // "Two of them have the same middle sound. Which is the odd one?"
        public AudioClip leoClosingClip;        // "The middle sound is the tricky one — and you found it every time!"

        [Header("10 Middle Sound Rounds")]
        public MiddleCarriageItem[] middleItems = new MiddleCarriageItem[10];

        [Header("3 Odd One Out Rounds")]
        public OddOneOutVowelItem[] oddOneOutItems = new OddOneOutVowelItem[3];

        [Header("Visual House Sprites (a, e, i, o, u)")]
        public Sprite[] vowelHouseSprites = new Sprite[5];

        [Header("Audio SFX")]
        public AudioClip trainChugSfx;
        public AudioClip trainWhistleSfx;
        public AudioClip tileSnapSfx;
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip starPopSfx;
    }
}
