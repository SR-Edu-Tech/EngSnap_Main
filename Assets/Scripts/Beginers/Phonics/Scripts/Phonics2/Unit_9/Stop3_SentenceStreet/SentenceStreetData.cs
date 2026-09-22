using System;
using UnityEngine;
using EngSnap.Phonics2.Unit8;

namespace EngSnap.Phonics2.Unit9
{
    [CreateAssetMenu(fileName = "SentenceStreetData_Unit9", menuName = "EngSnap/Phonics2/Unit9/Sentence Street Data")]
    public class SentenceStreetData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip pocketIntroClip;           // "Six more pocket words today. These ones you just know — no sounding out!"
        public AudioClip sentenceIntroClip;         // "Here comes your first sentence. Tap each word and read it!"
        public AudioClip sentenceSuccessClip;       // "You read a SENTENCE. A whole sentence, by yourself!"
        public AudioClip readWholeInvitationClip;   // "Now let us read it all together, nice and smooth."

        [Header("6 Sight Words (he, she, his, her, was, with)")]
        public SightWordCard[] sightWords = new SightWordCard[6];

        [Header("6 Book Sentences (pp. 43-45)")]
        public SentenceItem[] sentences = new SentenceItem[6];

        [Header("Audio SFX")]
        public AudioClip cardSlideInSfx;
        public AudioClip wordTapSfx;
        public AudioClip sentenceCompleteSfx;
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;

        private void Reset()
        {
            PopulateDefaultData();
        }

        [ContextMenu("Populate Default Data")]
        public void PopulateDefaultData()
        {
            // 6 Sight words for Unit 9
            string[] swList = new string[] { "he", "she", "his", "her", "was", "with" };
            sightWords = new SightWordCard[swList.Length];
            for (int i = 0; i < swList.Length; i++)
            {
                sightWords[i] = new SightWordCard
                {
                    word = swList[i]
                };
            }

            // 6 Sentences for Unit 9
            sentences = new SentenceItem[6];

            // 1: The fig is in the bin.
            sentences[0] = CreateSentence(
                "The fig is in the bin.",
                new string[] { "The", "fig", "is", "in", "the", "bin." },
                new bool[] { true, false, true, true, true, false },
                new bool[] { false, true, false, false, false, true }
            );

            // 2: The kid with a wig sat in a pit.
            sentences[1] = CreateSentence(
                "The kid with a wig sat in a pit.",
                new string[] { "The", "kid", "with", "a", "wig", "sat", "in", "a", "pit." },
                new bool[] { true, false, true, true, false, false, true, true, false },
                new bool[] { false, true, false, false, true, false, false, false, true }
            );

            // 3: This is a dog and its name is Tom.
            sentences[2] = CreateSentence(
                "This is a dog and its name is Tom.",
                new string[] { "This", "is", "a", "dog", "and", "its", "name", "is", "Tom." },
                new bool[] { true, true, true, false, true, true, true, true, false },
                new bool[] { false, false, false, true, false, false, false, false, true }
            );

            // 4: A fox sat on a log.
            sentences[3] = CreateSentence(
                "A fox sat on a log.",
                new string[] { "A", "fox", "sat", "on", "a", "log." },
                new bool[] { true, false, false, true, true, false },
                new bool[] { false, true, false, false, false, true }
            );

            // 5: The cub rubs the pup.
            sentences[4] = CreateSentence(
                "The cub rubs the pup.",
                new string[] { "The", "cub", "rubs", "the", "pup." },
                new bool[] { true, false, false, true, false },
                new bool[] { false, true, false, false, true }
            );

            // 6: The pup is in the tub.
            sentences[5] = CreateSentence(
                "The pup is in the tub.",
                new string[] { "The", "pup", "is", "in", "the", "tub." },
                new bool[] { true, false, true, true, true, false },
                new bool[] { false, true, false, false, false, true }
            );
        }

        private SentenceItem CreateSentence(string full, string[] words, bool[] isSight, bool[] isNoun)
        {
            SentenceItem item = new SentenceItem();
            item.fullSentence = full;
            item.tokens = new SentenceWordToken[words.Length];
            for (int i = 0; i < words.Length; i++)
            {
                item.tokens[i] = new SentenceWordToken
                {
                    wordText = words[i],
                    isSightWord = i < isSight.Length && isSight[i],
                    isNounWithPicture = i < isNoun.Length && isNoun[i]
                };
            }
            return item;
        }
    }
}
