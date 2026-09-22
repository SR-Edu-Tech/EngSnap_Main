using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit10
{
    [Serializable]
    public class WellStoryLineItem
    {
        public string lineText = "The well.";
        public string[] wordTokens = new string[] { "The", "well." };
        public AudioClip lineAudioClip;
        public int animationBeatIndex = 0; // 0=idle, 1=dog falls, 2=bell rings, 3=Dell yells, 4=Dad runs, 5=Dog rescued
    }

    [CreateAssetMenu(fileName = "DogInTheWellData_Unit10", menuName = "EngSnap/Phonics2/Unit10/Dog In The Well Data")]
    public class DogInTheWellData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip openingIntroClip;         // "This is the longest story yet. You know every word in it. Take your time — I will be quiet."
        public AudioClip midStoryPraiseClip;       // "You read that whole line by yourself."
        public AudioClip endingCelebrationClip;    // "The dog is out! You read the whole story!"
        public AudioClip recordOfferClip;          // "Want to hear yourself read it? Press the red button and read it again!"
        public AudioClip recordPlaybackPraiseClip; // "That is YOUR voice, reading a story."

        [Header("Story Lines (8 lines)")]
        public WellStoryLineItem[] storyLines = new WellStoryLineItem[8];

        [Header("Full Story Continuous Narration")]
        public AudioClip fullStoryNarrationClip;

        [Header("Story Scene Animation Sprites")]
        public Sprite[] storySceneBeatSprites = new Sprite[6];

        [Header("Audio SFX")]
        public AudioClip dogBarkSfx;
        public AudioClip bellRingSfx;
        public AudioClip splashSfx;
        public AudioClip dadRunningSfx;
        public AudioClip rescueCheerSfx;
        public AudioClip lineCompleteTickSfx;
        public AudioClip correctChimeSfx;

        private void Reset()
        {
            PopulateDefaultData();
        }

        [ContextMenu("Populate Default Data")]
        public void PopulateDefaultData()
        {
            storyLines = new WellStoryLineItem[8];

            storyLines[0] = new WellStoryLineItem
            {
                lineText = "The well.",
                wordTokens = new string[] { "The", "well." },
                animationBeatIndex = 0
            };

            storyLines[1] = new WellStoryLineItem
            {
                lineText = "The bell.",
                wordTokens = new string[] { "The", "bell." },
                animationBeatIndex = 0
            };

            storyLines[2] = new WellStoryLineItem
            {
                lineText = "The dog fell in the well.",
                wordTokens = new string[] { "The", "dog", "fell", "in", "the", "well." },
                animationBeatIndex = 1
            };

            storyLines[3] = new WellStoryLineItem
            {
                lineText = "Dell, tell Dad the dog fell in the well.",
                wordTokens = new string[] { "Dell,", "tell", "Dad", "the", "dog", "fell", "in", "the", "well." },
                animationBeatIndex = 2
            };

            storyLines[4] = new WellStoryLineItem
            {
                lineText = "Dell rang the bell to tell Dad the dog fell.",
                wordTokens = new string[] { "Dell", "rang", "the", "bell", "to", "tell", "Dad", "the", "dog", "fell." },
                animationBeatIndex = 2
            };

            storyLines[5] = new WellStoryLineItem
            {
                lineText = "Dell had to yell, \"The dog is in the well.\"",
                wordTokens = new string[] { "Dell", "had", "to", "yell,", "\"The", "dog", "is", "in", "the", "well.\"" },
                animationBeatIndex = 3
            };

            storyLines[6] = new WellStoryLineItem
            {
                lineText = "Dad ran to Dell at the well.",
                wordTokens = new string[] { "Dad", "ran", "to", "Dell", "at", "the", "well." },
                animationBeatIndex = 4
            };

            storyLines[7] = new WellStoryLineItem
            {
                lineText = "Dad got the dog out of the well.",
                wordTokens = new string[] { "Dad", "got", "the", "dog", "out", "of", "the", "well." },
                animationBeatIndex = 5
            };
        }
    }
}
