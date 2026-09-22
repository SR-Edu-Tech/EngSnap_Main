using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit6
{
    public enum StarChallengeTypeUnit6
    {
        BuzzOrWhisperIdentify, // (1) does /g/ buzz or whisper?
        MinimalPairHear,        // (2) bear or pear — which did you hear?
        TeamSpottingInWord,     // (3) tap the team in "ship"
        TwinRoleIdentify,       // (4) is /f/ the buzzer or the whisperer?
        TeamPositionRule,       // (5) where does ng live — start or end?
        ThBuzzCheckChoice       // (6) hand on throat: is "this" buzz or whisper?
    }

    [Serializable]
    public class ConsonantSoundCardItem
    {
        public string soundSymbol = "/b/";
        public string wordName = "bat";
        public bool isBuzzer = true; // true = Voiced (Buzz), false = Unvoiced (Whisper)
        public Sprite wordSprite;
        public AudioClip soundClip;
        public AudioClip wordClip;
        public AudioClip successClip; // e.g. "Yes! /v/ buzzes — vvvvvan!"
    }

    [Serializable]
    public class StarRoundUnit6Challenge
    {
        public StarChallengeTypeUnit6 challengeType;
        [TextArea(2, 3)] public string questionPrompt;
        public Sprite promptSprite;
        public AudioClip promptClip;
        public string[] choices = new string[3];
        public Sprite[] choiceSprites = new Sprite[3];
        public int correctChoiceIndex = 0;
    }

    [CreateAssetMenu(fileName = "SortingCaveData_Unit6", menuName = "EngSnap/Phonics2/Unit6/Sorting Cave Data")]
    public class SortingCaveData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip leoIntroClip;          // "Hand on your throat — then put each sound in the right crystal!"
        public AudioClip floatBackRetryClip;    // "Try it again with your hand on your throat."
        public AudioClip badgeVoiceClip;        // "You can feel every sound you make. You are a SOUND BUZZER!"
        public AudioClip unit7UnlockVoiceClip;  // "Unit Seven is open! Next time — the beginning, the middle and the END of every word."

        [Header("Tara Star Round Voice Scripts")]
        public AudioClip taraOpenerClip;        // "My turn! Six quick challenges. Ready? Roar!"

        [Header("12 Session Sorting Sound Cards (from 24 Consonants)")]
        public ConsonantSoundCardItem[] sessionCards = new ConsonantSoundCardItem[12];

        [Header("6 Star Round Challenges with Tara")]
        public StarRoundUnit6Challenge[] starChallenges = new StarRoundUnit6Challenge[6];

        [Header("Feedback Audio")]
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip starPopSfx;
        public AudioClip cardSnapSfx;
    }
}
