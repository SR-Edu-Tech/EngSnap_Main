using System;
using UnityEngine;
using EngSnap.Phonics2.Unit8;

namespace EngSnap.Phonics2.Unit9
{
    [CreateAssetMenu(fileName = "StoryTimeValleyData_Unit9", menuName = "EngSnap/Phonics2/Unit9/Story Time Valley Data")]
    public class StoryTimeValleyData : ScriptableObject
    {
        [Header("Leo & Tara Voice Scripts")]
        public AudioClip leoIntroClip;                  // "Three stories this time. Try each line by yourself first — I am right here if you need me."
        public AudioClip unaidedLinePraiseClip;         // "You read that whole line without any help. Did you notice?"
        public AudioClip taraStarRoundIntroClip;        // "My turn! Six quick challenges. Ready? Roar!"
        public AudioClip blendingChampionBadgeClip;     // "Every vowel, every word. You are a BLENDING CHAMPION!"
        public AudioClip unit10UnlockVoiceClip;         // "Unit Ten is open — the last one. Next time you read a real story, all the way through, by yourself."

        [Header("Story i (The Big Pig)")]
        public StoryContentItem storyI;

        [Header("Story o (Tom's Dog)")]
        public StoryContentItem storyO;

        [Header("Story u (The Bug)")]
        public StoryContentItem storyU;

        [Header("Comprehension Questions (3 Questions)")]
        public StoryQuestionItem[] comprehensionQuestions = new StoryQuestionItem[3];

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
            // Story i: The Big Pig
            storyI = new StoryContentItem
            {
                storyTitle = "The Big Pig",
                lines = new StoryLineItem[]
                {
                    new StoryLineItem { lineText = "I have a pink pig.", wordTokens = new string[] { "I", "have", "a", "pink", "pig." } },
                    new StoryLineItem { lineText = "His name is Fig.", wordTokens = new string[] { "His", "name", "is", "Fig." } },
                    new StoryLineItem { lineText = "Fig the pig is big.", wordTokens = new string[] { "Fig", "the", "pig", "is", "big." } },
                    new StoryLineItem { lineText = "He likes to dig and do a jig.", wordTokens = new string[] { "He", "likes", "to", "dig", "and", "do", "a", "jig." } },
                    new StoryLineItem { lineText = "The big pig digs and jigs.", wordTokens = new string[] { "The", "big", "pig", "digs", "and", "jigs." } }
                }
            };

            // Story o: Tom's Dog
            storyO = new StoryContentItem
            {
                storyTitle = "Tom's Dog",
                lines = new StoryLineItem[]
                {
                    new StoryLineItem { lineText = "Tom has a dog.", wordTokens = new string[] { "Tom", "has", "a", "dog." } },
                    new StoryLineItem { lineText = "The dog sat on the log.", wordTokens = new string[] { "The", "dog", "sat", "on", "the", "log." } },
                    new StoryLineItem { lineText = "The sun is hot so Tom did not sit on the log.", wordTokens = new string[] { "The", "sun", "is", "hot", "so", "Tom", "did", "not", "sit", "on", "the", "log." } },
                    new StoryLineItem { lineText = "Tom did sob.", wordTokens = new string[] { "Tom", "did", "sob." } }
                }
            };

            // Story u: The Bug
            storyU = new StoryContentItem
            {
                storyTitle = "The Bug",
                lines = new StoryLineItem[]
                {
                    new StoryLineItem { lineText = "Tim has a funny bug.", wordTokens = new string[] { "Tim", "has", "a", "funny", "bug." } },
                    new StoryLineItem { lineText = "The bug likes to hug Tim and to sleep in a mug.", wordTokens = new string[] { "The", "bug", "likes", "to", "hug", "Tim", "and", "to", "sleep", "in", "a", "mug." } },
                    new StoryLineItem { lineText = "One day, the bug got stuck in a jug.", wordTokens = new string[] { "One", "day,", "the", "bug", "got", "stuck", "in", "a", "jug." } },
                    new StoryLineItem { lineText = "Tim dug the bug out of the jug.", wordTokens = new string[] { "Tim", "dug", "the", "bug", "out", "of", "the", "jug." } }
                }
            };

            // 3 Comprehension Questions
            comprehensionQuestions = new StoryQuestionItem[3];
            comprehensionQuestions[0] = new StoryQuestionItem
            {
                questionPrompt = "What is the pig's name?",
                choiceLabels = new string[] { "Fig", "Big", "Jig" },
                correctChoiceIndex = 0
            };
            comprehensionQuestions[1] = new StoryQuestionItem
            {
                questionPrompt = "Why did Tom not sit on the log?",
                choiceLabels = new string[] { "The sun is hot", "The dog was there", "The log was wet" },
                correctChoiceIndex = 0
            };
            comprehensionQuestions[2] = new StoryQuestionItem
            {
                questionPrompt = "Where did the bug get stuck?",
                choiceLabels = new string[] { "in a jug", "in a mug", "in a rug" },
                correctChoiceIndex = 0
            };

            // 6 Star Round Challenges
            starChallenges = new Unit8StarChallengeItem[6];
            starChallenges[0] = new Unit8StarChallengeItem { challengeType = Unit8StarChallengeType.BlendWord, promptText = "Blend it — f, o, x.", targetWord = "fox", choices = new string[] { "fox", "box", "fix" }, correctIndex = 0 };
            starChallenges[1] = new Unit8StarChallengeItem { challengeType = Unit8StarChallengeType.IdentifyWord, promptText = "Which is it — bag, big, or bug?", targetWord = "big", choices = new string[] { "bag", "big", "bug" }, correctIndex = 1 };
            starChallenges[2] = new Unit8StarChallengeItem { challengeType = Unit8StarChallengeType.ChangeLetter, promptText = "Change 'pig' to 'pit'.", targetWord = "pit", choices = new string[] { "pit", "pin", "pot" }, correctIndex = 0 };
            starChallenges[3] = new Unit8StarChallengeItem { challengeType = Unit8StarChallengeType.ReadSentence, promptText = "Read this: 'The pup is in the tub.'", targetWord = "The pup is in the tub.", choices = new string[] { "The pup is in the tub.", "The cub is in the tub.", "The pup is on the rug." }, correctIndex = 0 };
            starChallenges[4] = new Unit8StarChallengeItem { challengeType = Unit8StarChallengeType.PocketSightWord, promptText = "Tap 'with' in your pocket.", targetWord = "with", choices = new string[] { "with", "was", "she" }, correctIndex = 0 };
            starChallenges[5] = new Unit8StarChallengeItem { challengeType = Unit8StarChallengeType.ReadSentence, promptText = "Does this make sense — 'The log sat on the fox'?", targetWord = "Silly!", choices = new string[] { "Silly!", "Yes, makes sense" }, correctIndex = 0 };
        }
    }
}
