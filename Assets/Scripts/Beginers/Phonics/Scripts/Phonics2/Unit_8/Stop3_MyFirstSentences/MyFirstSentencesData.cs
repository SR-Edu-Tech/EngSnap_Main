using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit8
{
    [Serializable]
    public class SightWordCard
    {
        public string word = "the";
        public AudioClip wordAudioClip;
        public AudioClip introScriptClip; // "This one says 'the'. Not /t/-/h/-/e/ — just 'the'!"
    }

    [Serializable]
    public class SentenceWordToken
    {
        public string wordText = "cap";
        public bool isSightWord = false;
        public bool isNounWithPicture = false;
        public Sprite nounSprite;
        public AudioClip wordAudioClip;
    }

    [Serializable]
    public class SentenceItem
    {
        public string fullSentence = "I see a cap and a map.";
        public SentenceWordToken[] tokens;
        public AudioClip wholeSentenceAudio; // Natural reading pace
        public AudioClip leoReadQuietlyAudio; // Leo reading quietly underneath
    }

    [CreateAssetMenu(fileName = "MyFirstSentencesData_Unit8", menuName = "EngSnap/Phonics2/Unit8/My First Sentences Data")]
    public class MyFirstSentencesData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip pocketIntroClip;           // "Some little words cannot be sounded out. They are just… themselves! Pop them in your pocket."
        public AudioClip sentenceIntroClip;         // "Here comes your first sentence. Tap each word and read it!"
        public AudioClip sentenceSuccessClip;       // "You read a SENTENCE. A whole sentence, by yourself!"
        public AudioClip readWholeInvitationClip;   // "Now let us read it all together, nice and smooth."

        [Header("6 Sight Words (a, I, the, is, see, and)")]
        public SightWordCard[] sightWords = new SightWordCard[6];

        [Header("4 Book Sentences (pp. 41-42)")]
        public SentenceItem[] sentences = new SentenceItem[4];

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
            // 6 Sight words
            string[] swList = new string[] { "a", "I", "the", "is", "see", "and" };
            sightWords = new SightWordCard[swList.Length];
            for (int i = 0; i < swList.Length; i++)
            {
                sightWords[i] = new SightWordCard
                {
                    word = swList[i]
                };
            }

            // 4 Sentences
            sentences = new SentenceItem[4];

            // 1: I see a cap and a map.
            sentences[0] = CreateSentence(
                "I see a cap and a map.",
                new string[] { "I", "see", "a", "cap", "and", "a", "map." },
                new bool[] { true, true, true, false, true, true, false },
                new bool[] { false, false, false, true, false, false, true }
            );

            // 2: The rat ran with a hat.
            sentences[1] = CreateSentence(
                "The rat ran with a hat.",
                new string[] { "The", "rat", "ran", "with", "a", "hat." },
                new bool[] { true, false, false, true, true, false },
                new bool[] { false, true, false, false, false, true }
            );

            // 3: Ben writes ten with a pen.
            sentences[2] = CreateSentence(
                "Ben writes ten with a pen.",
                new string[] { "Ben", "writes", "ten", "with", "a", "pen." },
                new bool[] { false, true, false, true, true, false },
                new bool[] { true, false, true, false, false, true }
            );

            // 4: Here is a pet and it is wet.
            sentences[3] = CreateSentence(
                "Here is a pet and it is wet.",
                new string[] { "Here", "is", "a", "pet", "and", "it", "is", "wet." },
                new bool[] { true, true, true, false, true, true, true, false },
                new bool[] { false, false, false, true, false, false, false, true }
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
