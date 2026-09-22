using System;
using UnityEngine;
using EngSnap.Phonics2.Unit8;

namespace EngSnap.Phonics2.Unit9
{
    [Serializable]
    public class VowelTripleItem
    {
        public string promptText = "Which one is it — cat, cot, or cut?";
        public string[] tripleWords = new string[] { "cat", "cot", "cut" };
        public int correctIndex = 1; // e.g. cot
        public Sprite[] tripleSprites = new Sprite[3];
        public AudioClip tripleAudioClip;
        public AudioClip targetWordAudioClip;
    }

    [CreateAssetMenu(fileName = "BlendItAgainData_Unit9", menuName = "EngSnap/Phonics2/Unit9/Blend It Again Data")]
    public class BlendItAgainData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip leoIntroClip;              // "You know the slide. Now let us use it on some new vowels!"
        public AudioClip noSliderPraiseClip;        // "PIG! Straight away, with no slider. You are getting fast!"
        public AudioClip mixedSessionIntroClip;     // "Now the tricky part. This word could have ANY vowel in the middle. Listen and look!"
        public AudioClip vowelTripleSuccessClip;    // "Yes! Cot. The o was in the middle."
        public AudioClip momoVowelHouseHintClip;    // "Try each house — c-a-t? c-o-t? c-u-t? Which one sounds right?"

        [Header("Session 1: Short i Words (8)")]
        public BlendWordItem[] shortIWords = new BlendWordItem[8];

        [Header("Session 2: Short o Words (8)")]
        public BlendWordItem[] shortOWords = new BlendWordItem[8];

        [Header("Session 3: Short u Words (8)")]
        public BlendWordItem[] shortUWords = new BlendWordItem[8];

        [Header("Session 4: Mixed 5-Vowel Words (10)")]
        public BlendWordItem[] mixedWords = new BlendWordItem[10];

        [Header("Vowel Triples Discrimination (4 Sets)")]
        public VowelTripleItem[] vowelTriples = new VowelTripleItem[4];

        [Header("Audio SFX")]
        public AudioClip soundBoxGlowSfx;
        public AudioClip blendSquashSfx;
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip vowelHouseSnapSfx;

        private void Reset()
        {
            PopulateDefaultData();
        }

        [ContextMenu("Populate Default Data")]
        public void PopulateDefaultData()
        {
            // Short i: fig, sit, pit, kid, big, pig, bin, wig
            string[] wordsI = new string[] { "fig", "sit", "pit", "kid", "big", "pig", "bin", "wig" };
            shortIWords = CreateWordSet(wordsI);

            // Short o: pot, fox, dog, log, hop, top, cot, box
            string[] wordsO = new string[] { "pot", "fox", "dog", "log", "hop", "top", "cot", "box" };
            shortOWords = CreateWordSet(wordsO);

            // Short u: cup, rug, mug, pup, cub, tub, bug, sun
            string[] wordsU = new string[] { "cup", "rug", "mug", "pup", "cub", "tub", "bug", "sun" };
            shortUWords = CreateWordSet(wordsU);

            // Mixed: cat, pig, dog, cup, bed, hat, sit, top, bug, net
            string[] wordsMixed = new string[] { "cat", "pig", "dog", "cup", "bed", "hat", "sit", "top", "bug", "net" };
            mixedWords = CreateWordSet(wordsMixed);

            // Vowel Triples
            vowelTriples = new VowelTripleItem[4];
            vowelTriples[0] = new VowelTripleItem
            {
                promptText = "Which one is it — cat, cot, or cut?",
                tripleWords = new string[] { "cat", "cot", "cut" },
                correctIndex = 1
            };
            vowelTriples[1] = new VowelTripleItem
            {
                promptText = "Which one is it — pin, pan, or pun?",
                tripleWords = new string[] { "pin", "pan", "pun" },
                correctIndex = 0
            };
            vowelTriples[2] = new VowelTripleItem
            {
                promptText = "Which one is it — bag, big, or bug?",
                tripleWords = new string[] { "bag", "big", "bug" },
                correctIndex = 2
            };
            vowelTriples[3] = new VowelTripleItem
            {
                promptText = "Which one is it — hot, hat, or hut?",
                tripleWords = new string[] { "hot", "hat", "hut" },
                correctIndex = 0
            };
        }

        private BlendWordItem[] CreateWordSet(string[] words)
        {
            BlendWordItem[] set = new BlendWordItem[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                set[i] = new BlendWordItem
                {
                    fullWord = words[i],
                    firstLetter = words[i][0].ToString(),
                    middleLetter = words[i][1].ToString(),
                    lastLetter = words[i][2].ToString()
                };
            }
            return set;
        }
    }
}
