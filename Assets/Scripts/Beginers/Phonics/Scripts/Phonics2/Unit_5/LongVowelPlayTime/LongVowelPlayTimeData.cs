using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit5
{
    public enum StarChallengeTypeUnit5
    {
        NameSayersChoice,     // (1) cat or cake — which said its name?
        MagicETransform,      // (2) cast magic e on "tub"
        VowelTeamSpotting,    // (3) which two letters are a team in "boat"?
        HatSwapChoice,        // (4) put the right hat on the i in "bike"
        PictureTapChoice,     // (5) tap the picture for "seat"
        ShortVsLongIdentify   // (6) is "hop" short or long?
    }

    [Serializable]
    public class PlayTimeWorksheetItem
    {
        public string wordWithGap = "tr_ _"; // tr_ _, l_ _f, k_te, t_ger, b_ne, pl_ne, B_ _r, wh_le, l_ _n
        public string fullWordText = "tree";
        public Sprite wordSprite;
        public string correctSpellingTile = "ee";
        public string[] tileOptions = new string[] { "ee", "ea", "i" };
        public AudioClip wordAudioClip;
        public AudioClip missingSoundClip;

        [Header("Tracing Settings (Unit 2 Tracing Component Reuse)")]
        public Sprite tracingOutlineSprite;
        public Sprite filledLetterSprite;
        public AudioClip letterSoundClip;
        public Vector2[] checkpointPositions;

        [Header("Audio Feedback Clips")]
        public AudioClip wrongFeedbackClip; // Audio for: "we could write ee, or ea. This one is ee."
        public AudioClip tracingPromptClip; // Audio for: "Trace it and say the word!"
    }

    [Serializable]
    public class StarRoundUnit5Challenge
    {
        public StarChallengeTypeUnit5 challengeType;
        [TextArea(2, 3)] public string questionPrompt;
        public Sprite promptSprite;
        public AudioClip promptClip;

        public string[] choices = new string[3];
        public Sprite[] choiceSprites = new Sprite[3];
        public int correctChoiceIndex = 0;
    }

    [CreateAssetMenu(fileName = "LongVowelPlayTimeData_Unit5", menuName = "EngSnap/Phonics2/Unit5/Long Vowel Play Time Data")]
    public class LongVowelPlayTimeData : ScriptableObject
    {
        [Header("Intro Voice Clips")]
        public AudioClip leoIntroClip; // "Look at the picture and listen. Which letters are missing?"
        public AudioClip taraOpenerClip; // "My turn! Six quick challenges. Ready? Roar!"
        public AudioClip badgeVoiceClip; // "Short vowels, long vowels, magic e and teams. You are a LONG VOWEL HERO!"
        public AudioClip unit6UnlockVoiceClip; // "Unit Six is open! Next time we find out which sounds BUZZ and which ones whisper!"

        [Header("9 Worksheet Gap Items (p.35)")]
        public PlayTimeWorksheetItem[] worksheetItems = new PlayTimeWorksheetItem[9];

        [Header("6 Star Round Challenges with Tara")]
        public StarRoundUnit5Challenge[] starChallenges = new StarRoundUnit5Challenge[6];

        [Header("Feedback Audio")]
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip starPopSfx;
        public AudioClip wordSnapSfx;
        public AudioClip tracingStrokeSfx;

        /// <summary>
        /// Formats full word with long vowel team / split vowel highlighted in luminous gold glowing color (#FFD54F).
        /// </summary>
        public static string FormatGlowingWord(string fullWord, string longVowelLetters)
        {
            if (string.IsNullOrEmpty(fullWord)) return "";
            if (string.IsNullOrEmpty(longVowelLetters)) return fullWord;

            // Direct substring match (e.g. "ee" in "tree", "ea" in "leaf", "ea" in "bear")
            int index = fullWord.IndexOf(longVowelLetters, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                string before = fullWord.Substring(0, index);
                string match = fullWord.Substring(index, longVowelLetters.Length);
                string after = fullWord.Substring(index + longVowelLetters.Length);
                return $"{before}<color=#FFD54F><b>{match}</b></color>{after}";
            }

            // Split magic-e digraph case (e.g. "kite", "bone", "plane", "whale" where vowel is 'i'/'o'/'a' and ending is 'e')
            if (longVowelLetters.Length == 1 && fullWord.EndsWith("e", StringComparison.OrdinalIgnoreCase) && fullWord.Length > 2)
            {
                int vIdx = fullWord.IndexOf(longVowelLetters, StringComparison.OrdinalIgnoreCase);
                if (vIdx >= 0 && vIdx < fullWord.Length - 1)
                {
                    string p1 = fullWord.Substring(0, vIdx);
                    string v = fullWord.Substring(vIdx, 1);
                    string mid = fullWord.Substring(vIdx + 1, fullWord.Length - vIdx - 2);
                    string e = fullWord.Substring(fullWord.Length - 1, 1);
                    return $"{p1}<color=#FFD54F><b>{v}</b></color>{mid}<color=#FFD54F><b>{e}</b></color>";
                }
            }

            // Fallback
            return $"<color=#FFD54F><b>{fullWord}</b></color>";
        }
    }
}
