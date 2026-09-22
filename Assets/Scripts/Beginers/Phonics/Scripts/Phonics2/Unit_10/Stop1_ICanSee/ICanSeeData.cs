using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit10
{
    [Serializable]
    public class FluencySentenceLine
    {
        public string lineText = "I can see Ben.";
        public string[] wordTokens = new string[] { "I", "can", "see", "Ben." };
        public Sprite lineIllustration;
        public AudioClip lineAudioClip;
    }

    [Serializable]
    public class FluencySentenceSet
    {
        public string setTitle = "Ben & Pen";
        public FluencySentenceLine[] lines = new FluencySentenceLine[3];
    }

    [Serializable]
    public class MakeYourOwnChoiceItem
    {
        public string word = "cat";
        public Sprite pictureSprite;
        public AudioClip wordAudioClip;
    }

    [CreateAssetMenu(fileName = "ICanSeeData_Unit10", menuName = "EngSnap/Phonics2/Unit10/I Can See Data")]
    public class ICanSeeData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip openingIntroClip;         // "Today we read lots of lines — and we read them SMOOTHLY."
        public AudioClip fluencyPraiseClip;        // "'I can see' — you did not even have to sound that out, did you? You just READ it!"
        public AudioClip rereadInvitationClip;     // "Let us read that set again. See if it feels easier this time."
        public AudioClip fasterPraiseClip;         // "Faster! You are reading like a reader."
        public AudioClip makeYourOwnIntroClip;     // "Now make your own. Pick a picture and read your sentence out loud!"
        public AudioClip makeYourOwnSuccessClip;   // "You wrote that sentence yourself — and then you read it!"

        [Header("4 Fluency Sentence Sets (pp. 48)")]
        public FluencySentenceSet[] sentenceSets = new FluencySentenceSet[4];

        [Header("Make Your Own Choices (cat, dog, pig, bug)")]
        public MakeYourOwnChoiceItem[] makeYourOwnChoices = new MakeYourOwnChoiceItem[4];

        [Header("Audio SFX")]
        public AudioClip lineTickSfx;
        public AudioClip smoothMeterFillSfx;
        public AudioClip wordTapSfx;
        public AudioClip correctChimeSfx;
        public AudioClip setCompleteFanfareSfx;
        public AudioClip retryGentleSfx;

        private void Reset()
        {
            PopulateDefaultData();
        }

        [ContextMenu("Populate Default Data")]
        public void PopulateDefaultData()
        {
            sentenceSets = new FluencySentenceSet[4];

            // Set 1: Ben & Pen
            sentenceSets[0] = new FluencySentenceSet
            {
                setTitle = "Ben with a pen",
                lines = new FluencySentenceLine[]
                {
                    new FluencySentenceLine { lineText = "I can see Ben.", wordTokens = new string[] { "I", "can", "see", "Ben." } },
                    new FluencySentenceLine { lineText = "I can see a pen.", wordTokens = new string[] { "I", "can", "see", "a", "pen." } },
                    new FluencySentenceLine { lineText = "I can see Ben with a pen.", wordTokens = new string[] { "I", "can", "see", "Ben", "with", "a", "pen." } }
                }
            };

            // Set 2: Hen & Chick
            sentenceSets[1] = new FluencySentenceSet
            {
                setTitle = "A hen and a chick",
                lines = new FluencySentenceLine[]
                {
                    new FluencySentenceLine { lineText = "I can see a hen.", wordTokens = new string[] { "I", "can", "see", "a", "hen." } },
                    new FluencySentenceLine { lineText = "I can see a chick.", wordTokens = new string[] { "I", "can", "see", "a", "chick." } },
                    new FluencySentenceLine { lineText = "I can see a hen and a chick.", wordTokens = new string[] { "I", "can", "see", "a", "hen", "and", "a", "chick." } }
                }
            };

            // Set 3: Desk & Musk Melon
            sentenceSets[2] = new FluencySentenceSet
            {
                setTitle = "A musk melon on a desk",
                lines = new FluencySentenceLine[]
                {
                    new FluencySentenceLine { lineText = "I can see a desk.", wordTokens = new string[] { "I", "can", "see", "a", "desk." } },
                    new FluencySentenceLine { lineText = "I can see a musk melon.", wordTokens = new string[] { "I", "can", "see", "a", "musk", "melon." } },
                    new FluencySentenceLine { lineText = "I can see a musk melon on a desk.", wordTokens = new string[] { "I", "can", "see", "a", "musk", "melon", "on", "a", "desk." } }
                }
            };

            // Set 4: Ship & Fish
            sentenceSets[3] = new FluencySentenceSet
            {
                setTitle = "A fish in a ship",
                lines = new FluencySentenceLine[]
                {
                    new FluencySentenceLine { lineText = "I can see a ship.", wordTokens = new string[] { "I", "can", "see", "a", "ship." } },
                    new FluencySentenceLine { lineText = "I can see a fish.", wordTokens = new string[] { "I", "can", "see", "a", "fish." } },
                    new FluencySentenceLine { lineText = "I can see a fish in a ship.", wordTokens = new string[] { "I", "can", "see", "a", "fish", "in", "a", "ship." } }
                }
            };

            // Make Your Own Choices
            makeYourOwnChoices = new MakeYourOwnChoiceItem[]
            {
                new MakeYourOwnChoiceItem { word = "cat" },
                new MakeYourOwnChoiceItem { word = "dog" },
                new MakeYourOwnChoiceItem { word = "pig" },
                new MakeYourOwnChoiceItem { word = "bug" }
            };
        }
    }
}
