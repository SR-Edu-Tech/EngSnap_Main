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
    }
}
