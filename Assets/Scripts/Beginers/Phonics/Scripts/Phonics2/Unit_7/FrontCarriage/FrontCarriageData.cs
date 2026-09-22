using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit7
{
    [Serializable]
    public class FrontCarriageItem
    {
        public string fullWord = "web";
        public string firstLetter = "w";
        public string displayGapWord = "_eb";
        public Sprite wordPictureSprite;
        public AudioClip stretchedWordAudio; // "wwwweb"
        public AudioClip normalWordAudio;    // "web"
        public string[] tileOptions = new string[] { "w", "v", "b", "m" };
        public bool isTracingRound = false;  // Every 4th word triggers finger tracing
        public Sprite tracingOutlineSprite;
        public Sprite filledLetterSprite;
        public Vector2[] checkpointPositions;
        public AudioClip successVoiceClip;   // "Yes! Web starts with /w/. Round lips — /w/!"
    }

    [CreateAssetMenu(fileName = "FrontCarriageData_Unit7", menuName = "EngSnap/Phonics2/Unit7/Front Carriage Data")]
    public class FrontCarriageData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip leoIntroClip;          // "All aboard the Sound Train! Today we find where sounds sit inside a word."
        public AudioClip frontCarriageTeachClip;// "This is the FRONT carriage. The first sound rides here."
        public AudioClip instructionClip;       // "Which letter goes in the front? Listen..."
        public AudioClip retryClip;             // "Listen to just the start. Which letter is that?"
        public AudioClip tracePromptClip;       // "Now trace it and say it!"

        [Header("10 Beginning Sound Rounds")]
        public FrontCarriageItem[] beginningItems = new FrontCarriageItem[10];

        [Header("Audio SFX")]
        public AudioClip trainChugSfx;
        public AudioClip trainWhistleSfx;
        public AudioClip tileSnapSfx;
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip starPopSfx;
    }
}
