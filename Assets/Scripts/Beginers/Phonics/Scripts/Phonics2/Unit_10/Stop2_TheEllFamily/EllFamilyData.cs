using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit10
{
    [Serializable]
    public class EllFamilyWordItem
    {
        public string onset = "w";
        public string rime = "ell";
        public string fullWord = "well";
        public Sprite wordPicture;
        public AudioClip onsetAudioClip;
        public AudioClip rimeAudioClip;
        public AudioClip fullWordAudioClip;
    }

    [CreateAssetMenu(fileName = "EllFamilyData_Unit10", menuName = "EngSnap/Phonics2/Unit10/The Ell Family Data")]
    public class EllFamilyData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip openingCallbackClip;       // "Remember s-c-oo-p, when two letters held hands? Look — two l's, doing exactly the same thing!"
        public AudioClip blendDemonstrationClip;    // "w … e … ll. Well! The two l's make one sound."
        public AudioClip meetDellClip;              // "This is Dell. It is somebody's name, so it wears a big D — but it reads just like bell."
        public AudioClip familyReadPromptClip;      // "Read them all to me: well, bell, fell, tell, yell."
        public AudioClip storyBridgeClip;           // "Now you know every word in the story. Let us go and read it!"

        [Header("6 -ell Family Words")]
        public EllFamilyWordItem[] ellWords = new EllFamilyWordItem[6];

        [Header("Story Words Preview (Dell, Dad, ran)")]
        public EllFamilyWordItem dellCharacterItem;
        public EllFamilyWordItem dadWordItem;
        public EllFamilyWordItem ranWordItem;

        [Header("Audio SFX")]
        public AudioClip tileSnapSfx;
        public AudioClip wordBlendSfx;
        public AudioClip correctChimeSfx;
        public AudioClip starPopSfx;
        public AudioClip retryGentleSfx;

        private void Reset()
        {
            PopulateDefaultData();
        }

        [ContextMenu("Populate Default Data")]
        public void PopulateDefaultData()
        {
            string[] onsets = new string[] { "w", "b", "f", "t", "y", "s" };
            string[] words = new string[] { "well", "bell", "fell", "tell", "yell", "sell" };

            ellWords = new EllFamilyWordItem[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                ellWords[i] = new EllFamilyWordItem
                {
                    onset = onsets[i],
                    rime = "ell",
                    fullWord = words[i]
                };
            }

            dellCharacterItem = new EllFamilyWordItem
            {
                onset = "D",
                rime = "ell",
                fullWord = "Dell"
            };

            dadWordItem = new EllFamilyWordItem
            {
                onset = "d",
                rime = "ad",
                fullWord = "Dad"
            };

            ranWordItem = new EllFamilyWordItem
            {
                onset = "r",
                rime = "an",
                fullWord = "ran"
            };
        }
    }
}
