using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit5
{
    [Serializable]
    public class MagicETransformPair
    {
        [Tooltip("The base short vowel word e.g. 'cap'")]
        public string shortWord = "cap";
        public Sprite shortWordSprite;
        public AudioClip shortWordClip;

        [Tooltip("The transformed silent-e word e.g. 'cape'")]
        public string longWord = "cape";
        public Sprite longWordSprite;
        public AudioClip longWordClip;

        [Tooltip("Spoken audio clip with both words in one breath: 'cap ... cape!'")]
        public AudioClip pairAudioClip;

        [Tooltip("Index of the root vowel character in the word (e.g. 1 for 'a' in 'cap')")]
        public int vowelCharIndex = 1;

        [Tooltip("Target long vowel sound name e.g. 'ā' / 'ay'")]
        public string vowelSoundName = "ay";
    }

    [Serializable]
    public class MagicEWhichOneChoice
    {
        [Tooltip("Short word option")]
        public string wordA = "cap";
        public Sprite spriteA;
        public AudioClip clipA;

        [Tooltip("Long word option")]
        public string wordB = "cape";
        public Sprite spriteB;
        public AudioClip clipB;

        [Tooltip("0 for wordA, 1 for wordB")]
        public int correctIndex = 1;

        [Tooltip("Audio clip for prompt question e.g. 'Listen... pine!'")]
        public AudioClip spokenQuestionClip;
    }

    [Serializable]
    public class MagicEWordWallEntry
    {
        public string word = "cake";
        public string familyName = "-ake";
        public string vowel = "a";
        public Sprite sprite;
        public AudioClip audioClip;
    }

    [CreateAssetMenu(fileName = "MagicEData_Unit5", menuName = "EngSnap/Phonics2/Unit5/Magic E Data")]
    public class MagicEData : ScriptableObject
    {
        [Header("Momo & Leo Voice Script Clips")]
        [Tooltip("Momo Opening: 'I have a magic wand — and it is shaped like an e! Watch what it does.'")]
        public AudioClip momoIntroClip;

        [Tooltip("Leo Setup: 'This word says \"cap\". Ready? Magic e… go!'")]
        public AudioClip leoSetupClip;

        [Tooltip("Momo Rule: 'The e says nothing at all — it is silent! But it makes the a say its NAME. Caaape! Cape!'")]
        public AudioClip momoRuleExplanationClip;

        [Tooltip("Leo Prompt: 'Your turn! Tap the wand.'")]
        public AudioClip leoTapWandClip;

        [Tooltip("Leo Prompt: 'Which word is this? Listen…'")]
        public AudioClip leoWhichWordPromptClip;

        [Tooltip("Momo Hint on retry: 'Look at the end of the word. Is there a magic e hiding there?'")]
        public AudioClip momoHintClip;

        [Tooltip("Leo Closing: 'One little silent e — and the whole word changes. That is magic!'")]
        public AudioClip leoClosingClip;

        [Header("8 Transformation Pairs (Book pages 30, 32, 34)")]
        public MagicETransformPair[] transformPairs = new MagicETransformPair[8];

        [Header("6 'Which One?' Choice Rounds")]
        public MagicEWhichOneChoice[] whichOneChoices = new MagicEWhichOneChoice[6];

        [Header("Word Wall 26 Items & Families (Book pp. 30, 32, 34)")]
        public string[] magicEWordWallList = new string[]
        {
            "cake", "take", "bake", "make", "game", "same", "fame",
            "tape", "safe", "case", "vase", "bike", "like", "hike",
            "line", "mine", "dime", "lime", "side", "hide", "ride",
            "tube", "cube", "June", "rule", "tune"
        };
        public Sprite[] magicEWordWallSprites;
        public AudioClip[] magicEWordWallClips;

        [Header("Feedback SFX")]
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip wandSparkleSfx;
        public AudioClip starPopSfx;
        public AudioClip shhhSilentSfx;
        public AudioClip vowelStandUpSfx;
        public AudioClip whooshFlightSfx;

        /// <summary>
        /// Formats word with distinct colors for the root vowel and the silent e.
        /// e.g. "cape" -> "c<color=#FF7043><b>a</b></color>p<color=#FFD54F><b>e</b></color>"
        /// </summary>
        public static string FormatMagicEWord(string word, string vowelHex = "#FF7043", string silentEHex = "#FFD54F")
        {
            if (string.IsNullOrEmpty(word)) return string.Empty;
            string lower = word.ToLower();

            // Check if ends with silent e
            if (lower.EndsWith("e") && lower.Length > 2)
            {
                int vowelIndex = -1;
                for (int i = 0; i < lower.Length - 1; i++)
                {
                    char c = lower[i];
                    if (c == 'a' || c == 'e' || c == 'i' || c == 'o' || c == 'u')
                    {
                        vowelIndex = i;
                        break;
                    }
                }

                if (vowelIndex >= 0)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    for (int i = 0; i < word.Length; i++)
                    {
                        if (i == vowelIndex)
                        {
                            sb.Append($"<color={vowelHex}><b>{word[i]}</b></color>");
                        }
                        else if (i == word.Length - 1)
                        {
                            sb.Append($"<color={silentEHex}><b>{word[i]}</b></color>");
                        }
                        else
                        {
                            sb.Append(word[i]);
                        }
                    }
                    return sb.ToString();
                }
            }

            return word;
        }
    }
}
