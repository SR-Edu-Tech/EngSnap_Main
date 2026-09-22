using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit5
{
    [Serializable]
    public class VowelTeamItem
    {
        public string teamName = "ee"; // ee, ea, oa, ai
        public string teamSound = "ee";
        public AudioClip teamVoiceClip;
        public String[] pictureWordNames = new string[4];
        public Sprite[] pictureWordSprites = new Sprite[4];
        public AudioClip[] pictureWordAudioClips = new AudioClip[4];
    }

    [Serializable]
    public class VowelTeamSpottingWord
    {
        public string wordText = "sheep";
        public Sprite wordSprite;
        public string correctTeamLetters = "ee";
        public int teamStartIndex = 2; // e.g. "sh[ee]p" -> index 2
        public int teamLength = 2;
        public AudioClip wordAudioClip;
    }

    [Serializable]
    public class VowelTeamWordFamily
    {
        public string familyName = "-eat / -eak";
        public string familyDescription = "Long E team: -eat and -eak words!";
        public string[] words = new string[] { "eat", "beat", "beak", "leak", "weak" };
        public Sprite[] sprites;
        public AudioClip[] audioClips;
    }

    [CreateAssetMenu(fileName = "VowelTeamsData_Unit5", menuName = "EngSnap/Phonics2/Unit5/Vowel Teams Data")]
    public class VowelTeamsData : ScriptableObject
    {
        [Header("Intro Voice Clips")]
        public AudioClip leoIntroClip; // "Sometimes two vowels walk together — and only the first one talks!"
        public AudioClip leoClosingClip; // "Magic e, or a vowel team — both make the vowel say its name!"

        [Header("4 Vowel Teams (ee, ea, oa, ai)")]
        public VowelTeamItem[] vowelTeams = new VowelTeamItem[4];

        [Header("6 Team Spotting Rounds")]
        public VowelTeamSpottingWord[] spottingWords = new VowelTeamSpottingWord[6];

        [Header("Word Wall Families (Book pp. 31, 33)")]
        public VowelTeamWordFamily[] wordWallFamilies = new VowelTeamWordFamily[]
        {
            new VowelTeamWordFamily
            {
                familyName = "-eat / -eak",
                familyDescription = "Long E team: -eat and -eak words!",
                words = new string[] { "eat", "beat", "beak", "leak", "weak" }
            },
            new VowelTeamWordFamily
            {
                familyName = "-ee- / -ea-",
                familyDescription = "Long E team: ee and ea words!",
                words = new string[] { "sheep", "feet", "seat", "meat", "mean", "bean", "seal", "meal", "leaf" }
            },
            new VowelTeamWordFamily
            {
                familyName = "-oa-",
                familyDescription = "Long O team: oa words!",
                words = new string[] { "boat", "coat", "toast", "road" }
            },
            new VowelTeamWordFamily
            {
                familyName = "Magic E Reminder",
                familyDescription = "Magic E reminder: Long O words can also use magic e!",
                words = new string[] { "rose", "cone", "rope", "stone", "phone", "alone" }
            }
        };

        [Header("Word Wall Lists (pp. 31, 33)")]
        public string[] vowelTeamsWordWallList = new string[]
        {
            "eat", "beat", "beak", "leak", "weak",
            "sheep", "feet", "seat", "meat", "mean", "bean", "seal", "meal", "leaf",
            "boat", "coat", "toast", "road",
            "rose", "cone", "rope", "stone", "phone", "alone"
        };
        public Sprite[] vowelTeamsWordWallSprites;
        public AudioClip[] vowelTeamsWordWallClips;

        [Header("Feedback Audio")]
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip handHoldLinkSfx;
        public AudioClip starPopSfx;
    }
}
