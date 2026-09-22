using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit8
{
    public enum Unit8StarChallengeType
    {
        BlendWord,
        ChangeLetter,
        IdentifyWord,
        ReadSentence,
        PocketSightWord,
        BlendFinalWord
    }

    [Serializable]
    public class StoryLineItem
    {
        public string lineText = "I have a cat.";
        public string[] wordTokens = new string[] { "I", "have", "a", "cat." };
        public bool[] isSightWord = new bool[] { true, true, true, false };
        public AudioClip lineAudioClip;
    }

    [Serializable]
    public class StoryContentItem
    {
        public string storyTitle = "Pat The Cat";
        public Sprite storyIllustrationSprite;
        public StoryLineItem[] lines;
        public AudioClip fullStoryAudio;
    }

    [Serializable]
    public class StoryQuestionItem
    {
        public string questionPrompt = "Where does Pat sit?";
        public Sprite[] choiceSprites = new Sprite[3];
        public string[] choiceLabels = new string[] { "on a mat", "in a bed", "on a log" };
        public int correctChoiceIndex = 0;
        public AudioClip questionPromptClip;
    }

    [Serializable]
    public class Unit8StarChallengeItem
    {
        public Unit8StarChallengeType challengeType = Unit8StarChallengeType.BlendWord;
        public string promptText = "Blend it — m, a, p.";
        public string targetWord = "map";
        public string[] choices = new string[] { "map", "mop", "mat" };
        public int correctIndex = 0;
        public AudioClip promptClip;
    }

    [CreateAssetMenu(fileName = "StoryTimeData_Unit8", menuName = "EngSnap/Phonics2/Unit8/Story Time Data")]
    public class StoryTimeData : ScriptableObject
    {
        [Header("Leo & Tara Voice Scripts")]
        public AudioClip leoIntroClip;              // "You can read words. You can read sentences. Now — a whole STORY."
        public AudioClip leoPermissionClip;         // "Read it your way. Tap any word you want to hear."
        public AudioClip reReadInvitationClip;      // "You read the whole story! Shall we read it again, faster this time?"
        public AudioClip taraStarRoundIntroClip;    // "My turn! Six quick challenges. Ready? Roar!"
        public AudioClip wordReaderBadgeVoiceClip;  // "You are a WORD READER! Go and read that story to someone at home."
        public AudioClip unit9UnlockVoiceClip;      // "Unit Nine is open! Next time — i, o and u, and three more stories."

        [Header("Story A (Pat The Cat)")]
        public StoryContentItem storyA;

        [Header("Story B (Ben's Hen)")]
        public StoryContentItem storyB;

        [Header("Comprehension Questions")]
        public StoryQuestionItem[] comprehensionQuestions = new StoryQuestionItem[2];

        [Header("6 Tara Star Round Challenges")]
        public Unit8StarChallengeItem[] starChallenges = new Unit8StarChallengeItem[6];

        [Header("Audio SFX")]
        public AudioClip lineCompleteTickSfx;
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip badgeUnlockSfx;
        public AudioClip starPopSfx;

        private void Reset()
        {
            PopulateDefaultData();
        }

        [ContextMenu("Populate Default Data")]
        public void PopulateDefaultData()
        {
            // Story A: Pat The Cat
            storyA = new StoryContentItem
            {
                storyTitle = "Pat The Cat",
                lines = new StoryLineItem[]
                {
                    new StoryLineItem { lineText = "I have a cat.", wordTokens = new string[] { "I", "have", "a", "cat." } },
                    new StoryLineItem { lineText = "His name is Pat.", wordTokens = new string[] { "His", "name", "is", "Pat." } },
                    new StoryLineItem { lineText = "Pat will not play or run.", wordTokens = new string[] { "Pat", "will", "not", "play", "or", "run." } },
                    new StoryLineItem { lineText = "He sits on a mat.", wordTokens = new string[] { "He", "sits", "on", "a", "mat." } },
                    new StoryLineItem { lineText = "Pat the cat is too fat.", wordTokens = new string[] { "Pat", "the", "cat", "is", "too", "fat." } }
                }
            };

            // Story B: Ben's Hen
            storyB = new StoryContentItem
            {
                storyTitle = "Ben's Hen",
                lines = new StoryLineItem[]
                {
                    new StoryLineItem { lineText = "Ben got a new red hen.", wordTokens = new string[] { "Ben", "got", "a", "new", "red", "hen." } },
                    new StoryLineItem { lineText = "Her name is Jen.", wordTokens = new string[] { "Her", "name", "is", "Jen." } },
                    new StoryLineItem { lineText = "Jen, the hen, likes Ben.", wordTokens = new string[] { "Jen,", "the", "hen,", "likes", "Ben." } },
                    new StoryLineItem { lineText = "Jen had ten eggs for Ben.", wordTokens = new string[] { "Jen", "had", "ten", "eggs", "for", "Ben." } },
                    new StoryLineItem { lineText = "Ben is happy with Jen.", wordTokens = new string[] { "Ben", "is", "happy", "with", "Jen." } }
                }
            };

            // Comprehension Questions
            comprehensionQuestions = new StoryQuestionItem[2];
            comprehensionQuestions[0] = new StoryQuestionItem
            {
                questionPrompt = "Where does Pat sit?",
                choiceLabels = new string[] { "on a mat", "on a bed", "in a box" },
                correctChoiceIndex = 0
            };
            comprehensionQuestions[1] = new StoryQuestionItem
            {
                questionPrompt = "What is the hen's name?",
                choiceLabels = new string[] { "Jen", "Ben", "Pat" },
                correctChoiceIndex = 0
            };

            // 6 Star Round Challenges
            starChallenges = new Unit8StarChallengeItem[6];
            starChallenges[0] = new Unit8StarChallengeItem { challengeType = Unit8StarChallengeType.BlendWord, promptText = "Blend it — m, a, p.", targetWord = "map", choices = new string[] { "map", "mop", "mat" }, correctIndex = 0 };
            starChallenges[1] = new Unit8StarChallengeItem { challengeType = Unit8StarChallengeType.ChangeLetter, promptText = "Change 'cat' to 'bat'.", targetWord = "bat", choices = new string[] { "bat", "hat", "rat" }, correctIndex = 0 };
            starChallenges[2] = new Unit8StarChallengeItem { challengeType = Unit8StarChallengeType.IdentifyWord, promptText = "Which word says 'hat'?", targetWord = "hat", choices = new string[] { "hat", "hot", "hit" }, correctIndex = 0 };
            starChallenges[3] = new Unit8StarChallengeItem { challengeType = Unit8StarChallengeType.ReadSentence, promptText = "Read this: 'The rat ran.'", targetWord = "The rat ran.", choices = new string[] { "The rat ran.", "The cat ran.", "The bat ran." }, correctIndex = 0 };
            starChallenges[4] = new Unit8StarChallengeItem { challengeType = Unit8StarChallengeType.PocketSightWord, promptText = "Tap 'the' in your pocket.", targetWord = "the", choices = new string[] { "the", "and", "see" }, correctIndex = 0 };
            starChallenges[5] = new Unit8StarChallengeItem { challengeType = Unit8StarChallengeType.BlendFinalWord, promptText = "Blend it — b, e, d.", targetWord = "bed", choices = new string[] { "bed", "bad", "bid" }, correctIndex = 0 };
        }
    }
}
