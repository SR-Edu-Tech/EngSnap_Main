using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit6
{
    [Serializable]
    public class BuzzSoundItem
    {
        public string soundSymbol;      // e.g. "/b/", "/p/", "/z/", "/s/"
        public string letterName;        // "B", "P", "Z", "S"
        public bool isBuzzer;           // true = Voiced (buzzes in throat), false = Whisperer (unvoiced puff)
        public AudioClip soundClip;     // Isolated clipped phoneme sound
        public Sprite mouthSprite;       // Mouth shape sprite
        public AudioClip voiceSuccessClip; // "Yes! /d/ buzzes. Hand on your throat — ddddd!"
    }

    [CreateAssetMenu(fileName = "BuzzOrWhisperData_Unit6", menuName = "EngSnap/Phonics2/Unit6/Buzz Or Whisper Data")]
    public class BuzzOrWhisperData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip leoWelcomeClip;        // "Welcome to the Buzz and Whisper Cave! Some sounds buzz… and some are just air."
        public AudioClip throatTestPromptClip;   // "Put your hand on your throat. Say zzzzzz. Feel that buzz?"
        public AudioClip throatTestContrastClip; // "Now say sssssss. No buzz! Just air. Same mouth — different sound!"
        public AudioClip puffTestPromptClip;     // "Watch the tissue. /p/ — big puff! /b/ — little puff!"
        public AudioClip sortingInstructionClip; // "Buzz tunnel, or whisper tunnel? Send it down!"
        public AudioClip momoHintClip;           // "Hand on your throat! Buzz, or no buzz?"
        public AudioClip freePlayInstructionClip;// "Tap any sound to feel the throat buzz!"

        [Header("10 Sorting Sounds (Round 1 to 10)")]
        public BuzzSoundItem[] sortingSounds = new BuzzSoundItem[10];

        [Header("Free Play Sounds Row")]
        public BuzzSoundItem[] freePlaySounds;

        [Header("Audio SFX")]
        public AudioClip buzzHumSfx;
        public AudioClip whisperBreezeSfx;
        public AudioClip tissueFlySfx;
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip starPopSfx;
    }
}
