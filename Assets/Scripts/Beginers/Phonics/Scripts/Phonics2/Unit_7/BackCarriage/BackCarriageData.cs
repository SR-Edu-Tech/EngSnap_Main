using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit7
{
    public enum CarriagePositionTarget
    {
        Front,
        Back
    }

    [Serializable]
    public class BackCarriageItem
    {
        public string fullWord = "van";
        public string lastLetter = "n";
        public string displayGapWord = "va_";
        public Sprite wordPictureSprite;
        public AudioClip heldEndingWordAudio; // "vannnn"
        public AudioClip normalWordAudio;     // "van"
        public string[] tileOptions = new string[] { "n", "m", "t", "d" };
        public AudioClip successVoiceClip;    // "Yes! /n/ at the end. Van!"
    }

    [Serializable]
    public class FrontOrBackQuizItem
    {
        public string fullWord = "hat";
        public string testSound = "/t/";
        public CarriagePositionTarget correctPosition = CarriagePositionTarget.Back;
        public Sprite wordPictureSprite;
        public AudioClip promptAudioClip;     // "Is /t/ at the FRONT of 'hat', or at the BACK?"
    }

    [Serializable]
    public class TrickyEndingBonusItem
    {
        public string fullWord = "nest";
        public string endingTwoSounds = "st";
        public Sprite wordPictureSprite;
        public AudioClip wordAudioClip;       // "n - e - s - t. Nest!"
        public string explanationText = "TWO sounds at the end: s - t!";
    }

    [CreateAssetMenu(fileName = "BackCarriageData_Unit7", menuName = "EngSnap/Phonics2/Unit7/Back Carriage Data")]
    public class BackCarriageData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip leoIntroClip;          // "The LAST sound rides at the back. It is the one people forget — so listen hard!"
        public AudioClip instructionClip;       // "What is the last sound? Listen..."
        public AudioClip retryClip;             // "Say it slowly with me. Hear it at the end?"
        public AudioClip trickyIntroClip;       // "This one is tricky — nest. Listen: n-e-s-t. TWO sounds at the end! We will learn these properly later."
        public AudioClip leoClosingClip;        // "You caught every last sound. Most people miss those!"

        [Header("8 Clean Ending Sound Rounds")]
        public BackCarriageItem[] cleanEndingItems = new BackCarriageItem[8];

        [Header("4 Front or Back Position Rounds")]
        public FrontOrBackQuizItem[] frontOrBackItems = new FrontOrBackQuizItem[4];

        [Header("3 Tricky Ending Bonus Rounds")]
        public TrickyEndingBonusItem[] trickyBonusItems = new TrickyEndingBonusItem[3];

        [Header("Audio SFX")]
        public AudioClip trainChugSfx;
        public AudioClip trainWhistleSfx;
        public AudioClip tileSnapSfx;
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip starPopSfx;
    }
}
