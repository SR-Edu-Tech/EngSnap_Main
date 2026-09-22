using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit7
{
    public enum StarChallengeType
    {
        FirstSound,
        LastSound,
        MiddleVowel,
        FrontOrBackPosition,
        WholeWordFill,
        MissingLetterTrace
    }

    [Serializable]
    public class WholeTrainWordItem
    {
        public string fullWord = "cat";
        public string firstLetter = "c";
        public string middleLetter = "a";
        public string lastLetter = "t";
        public Sprite wordPictureSprite;
        public AudioClip normalWordAudio;    // "cat"
        public AudioClip segmentedWordAudio; // "c - a - t"
        public string[] frontOptions = new string[] { "c", "k", "b", "p" };
        public string[] middleOptions = new string[] { "a", "e", "i", "o", "u" };
        public string[] backOptions = new string[] { "t", "d", "p", "g" };
        public AudioClip departVoiceClip;    // "c... a... t... CAT! The train is full — off it goes!"
    }

    [Serializable]
    public class TrainStarChallengeItem
    {
        public StarChallengeType challengeType = StarChallengeType.FirstSound;
        public string questionPrompt = "First sound in 'goat'?";
        public string testWord = "goat";
        public Sprite wordSprite;
        public AudioClip promptClip;
        public string[] choiceTexts = new string[] { "g", "d", "b" };
        public int correctChoiceIndex = 0;
        public string correctLetter = "g";
        public Vector2[] traceCheckpoints;
    }

    [CreateAssetMenu(fileName = "WholeTrainData_Unit7", menuName = "EngSnap/Phonics2/Unit7/Whole Train Data")]
    public class WholeTrainData : ScriptableObject
    {
        [Header("Leo & Tara Voice Scripts")]
        public AudioClip leoIntroClip;          // "All three carriages are open! Can you fill the whole train?"
        public AudioClip spelledWordPraiseClip; // "Do you know what you just did? You SPELLED a word!"
        public AudioClip taraStarRoundOpenClip; // "My turn! Six quick challenges. Ready? Roar!"
        public AudioClip badgeVoiceClip;        // "Front, middle and back. You are a SOUND TRAIN DRIVER!"
        public AudioClip unit8UnlockClip;       // "Unit Eight is open — and this is the big one. Next time, you are going to READ whole words all by yourself!"

        [Header("8 Whole Train CVC Rounds")]
        public WholeTrainWordItem[] wholeTrainItems = new WholeTrainWordItem[8];

        [Header("6 Tara Star Round Challenges")]
        public TrainStarChallengeItem[] starChallenges = new TrainStarChallengeItem[6];

        [Header("Audio SFX")]
        public AudioClip trainChugSfx;
        public AudioClip trainWhistleSfx;
        public AudioClip tileSnapSfx;
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip starPopSfx;
    }
}
