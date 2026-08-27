using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit5
{
    [Serializable]
    public class LongVowelItem
    {
        public string vowelSymbol = "ā"; // ā, ē, ī, ō, ū
        public string vowelName = "ay";
        public AudioClip nameVoiceClip;
        public String[] pictureWordNames = new string[4]; // e.g. acorn, baby, alien, paper
        public Sprite[] pictureWordSprites = new Sprite[4];
        public AudioClip[] pictureWordAudioClips = new AudioClip[4];
    }

    [Serializable]
    public class ShortLongContrastPair
    {
        public string shortWord = "cat";
        public Sprite shortWordSprite;
        public AudioClip shortWordClip;

        public string longWord = "cake";
        public Sprite longWordSprite;
        public AudioClip longWordClip;

        public AudioClip contrastPairClip; // Spoken contrast pair ("cat ... cake")
        public bool isLongCorrect = true; // Expected answer for question "Did it say its name?"
    }

    [Serializable]
    public class HatSwapRound
    {
        public string wordText = "cake";
        public Sprite wordSprite;
        public int targetVowelIndex = 1; // Index of the vowel letter in the word
        public bool requiresMacron = true; // true = flat macron hat (long), false = curved breve hat (short)
        public AudioClip wordAudioClip;
    }

    [CreateAssetMenu(fileName = "NameSayersData_Unit5", menuName = "EngSnap/Phonics2/Unit5/Name Sayers Data")]
    public class NameSayersData : ScriptableObject
    {
        [Header("Intro Voice Clips")]
        public AudioClip leoIntroClip; // "Welcome to Long Vowel Lake! These five are the Name Sayers. They say their own names."
        public AudioClip hatExplanationClip; // "Remember the curvy hat? That was SHORT. This flat hat means LONG."
        public AudioClip leoClosingClip; // "Short says the sound. Long says the name. You have both now!"

        [Header("5 Long Vowel Characters (p.29)")]
        public LongVowelItem[] longVowels = new LongVowelItem[5]; // ā, ē, ī, ō, ū

        [Header("10 Short vs Long Contrast Rounds")]
        public ShortLongContrastPair[] contrastPairs = new ShortLongContrastPair[10];

        [Header("5 Hat Swap Drag Rounds")]
        public HatSwapRound[] hatSwapRounds = new HatSwapRound[5];

        [Header("Feedback Audio")]
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip starPopSfx;
    }
}
