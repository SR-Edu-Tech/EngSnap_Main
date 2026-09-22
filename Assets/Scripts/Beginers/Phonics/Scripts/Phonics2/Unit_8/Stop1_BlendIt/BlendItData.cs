using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit8
{
    [Serializable]
    public class BlendWordItem
    {
        public string fullWord = "cat";
        public string firstLetter = "c";
        public string middleLetter = "a";
        public string lastLetter = "t";
        public Sprite wordPictureSprite;
        public AudioClip firstSoundAudio;   // clipped /c/
        public AudioClip middleSoundAudio;  // clipped /a/
        public AudioClip lastSoundAudio;    // clipped /t/
        public AudioClip blendedWordAudio;  // "CAT!" spoken brightly
    }

    [CreateAssetMenu(fileName = "BlendItData_Unit8", menuName = "EngSnap/Phonics2/Unit8/Blend It Data")]
    public class BlendItData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip leoIntroClip;          // "Today you are going to READ. Stand up — we need your arm!"
        public AudioClip leoActionDemoClip;     // "Touch your shoulder. Slide down… /c/ … /a/ … /t/ … and sweep back up — CAT!"
        public AudioClip leoYourTurnClip;       // "Your turn. Slide with your finger!"
        public AudioClip firstSuccessPraiseClip;// "CAT! You read a word! You did that all by yourself."
        public AudioClip retrySlowSlideClip;    // "Slide again, a bit slower… /m/ … /a/ … /p/ …"
        public AudioClip momoHelpClip;          // "Slide with me! Shoulder… elbow… hand… and up!"
        public AudioClip noSliderFadeOutClip;   // "Now with no slider. Ready? Read it!"

        [Header("Short a Session (8 Words)")]
        public BlendWordItem[] shortAWords = new BlendWordItem[8];

        [Header("Short e Session (8 Words)")]
        public BlendWordItem[] shortEWords = new BlendWordItem[8];

        [Header("Audio SFX")]
        public AudioClip soundBoxGlowSfx;
        public AudioClip blendSquashSfx;
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip starPopSfx;

        private void Reset()
        {
            PopulateDefaultData();
        }

        [ContextMenu("Populate Default Data")]
        public void PopulateDefaultData()
        {
            // Short a: sat, cat, map, bat, ran, hat, cap, rat
            string[] wordsA = new string[] { "sat", "cat", "map", "bat", "ran", "hat", "cap", "rat" };
            shortAWords = new BlendWordItem[wordsA.Length];
            for (int i = 0; i < wordsA.Length; i++)
            {
                shortAWords[i] = new BlendWordItem
                {
                    fullWord = wordsA[i],
                    firstLetter = wordsA[i][0].ToString(),
                    middleLetter = wordsA[i][1].ToString(),
                    lastLetter = wordsA[i][2].ToString()
                };
            }

            // Short e: ten, bed, red, leg, pen, net, wet, pet
            string[] wordsE = new string[] { "ten", "bed", "red", "leg", "pen", "net", "wet", "pet" };
            shortEWords = new BlendWordItem[wordsE.Length];
            for (int i = 0; i < wordsE.Length; i++)
            {
                shortEWords[i] = new BlendWordItem
                {
                    fullWord = wordsE[i],
                    firstLetter = wordsE[i][0].ToString(),
                    middleLetter = wordsE[i][1].ToString(),
                    lastLetter = wordsE[i][2].ToString()
                };
            }
        }
    }
}
