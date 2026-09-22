using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit6
{
    public enum TeamPositionType
    {
        StartsWith,
        EndsWith
    }

    [Serializable]
    public class ConsonantTeamItem
    {
        public string teamLetters = "sh";   // sh, ch, th, ng
        public string teamSound = "/sh/";
        public string hookPhrase = "Finger on your lips — shhhhh!";
        public Sprite hookSprite;
        public AudioClip hookVoiceClip;
        public string exampleWord = "ship";
        public Sprite exampleWordSprite;
        public AudioClip exampleWordAudio;
    }

    [Serializable]
    public class PositionSortWordItem
    {
        public string wordText = "sing";
        public string teamLetters = "ng";
        public TeamPositionType positionType = TeamPositionType.EndsWith;
        public Sprite wordSprite;
        public AudioClip wordAudioClip;
    }

    [Serializable]
    public class TeamHuntWordItem
    {
        public string fullWord = "chips";
        public string teamLetters = "ch";
        public int teamStartIndex = 0; // index of the team in the word
        public Sprite wordSprite;
        public AudioClip wordAudioClip;
    }

    [Serializable]
    public class ThBuzzCheckItem
    {
        public string wordText = "thin";
        public bool isBuzzer = false; // false for "thin" (whisper), true for "this" (buzz)
        public Sprite wordSprite;
        public AudioClip wordAudioClip;
        public AudioClip throatPromptClip;
    }

    [CreateAssetMenu(fileName = "ConsonantTeamsData_Unit6", menuName = "EngSnap/Phonics2/Unit6/Consonant Teams Data")]
    public class ConsonantTeamsData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip leoIntroClip;          // "Remember when two letters held hands and made one sound? These four do it too — and now they have names!"
        public AudioClip teamHuntPromptClip;    // "Which two letters are holding hands here? Tap them!"
        public AudioClip thCompareIntroClip;    // "Here is a funny one. 'Thin' — whisper. 'This' — buzz! Hand on your throat and try both."
        public AudioClip leoClosingClip;        // "Four teams, four sounds. You will see these everywhere now!"

        [Header("4 Consonant Teams (sh, ch, th, ng)")]
        public ConsonantTeamItem[] teams = new ConsonantTeamItem[4];

        [Header("6 Position Sorting Words")]
        public PositionSortWordItem[] positionWords = new PositionSortWordItem[6];

        [Header("8 Team Hunt Words")]
        public TeamHuntWordItem[] teamHuntWords = new TeamHuntWordItem[8];

        [Header("2 TH Buzz Check Words (thin vs this)")]
        public ThBuzzCheckItem[] thCheckWords = new ThBuzzCheckItem[2];

        [Header("Audio SFX")]
        public AudioClip handHoldLinkSfx;
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip starPopSfx;
    }
}
