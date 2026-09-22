using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit10
{
    [Serializable]
    public class GraduationQuestionItem
    {
        public string questionPrompt = "Who rang the bell?";
        public string[] choices = new string[] { "Dell", "Dad", "the dog" };
        public Sprite[] choiceSprites = new Sprite[3];
        public int correctChoiceIndex = 0;
        public int storyReferenceLineIndex = 4; // 0-indexed line in well story containing the answer
        public AudioClip questionAudioClip;
        public AudioClip praiseAudioClip;
    }

    [CreateAssetMenu(fileName = "GraduationData_Unit10", menuName = "EngSnap/Phonics2/Unit10/Graduation Data")]
    public class GraduationData : ScriptableObject
    {
        [Header("Leo & Mascots Voice Scripts")]
        public AudioClip questionIntroClip;         // "Now — did you understand it? The story is right there if you want to look again."
        public AudioClip wrongAnswerClueClip;       // "Let us look again — the answer is hiding in this line."
        public AudioClip questionsCompleteClip;     // "Five out of five. You read it AND you understood it."
        public AudioClip allMascotsCheerClip;       // "Hooray! Hooray!" (All four mascots cheer)
        public AudioClip islandAwakeClip;           // "Look at the island! Every sound is home. YOU woke up Sound Island."
        public AudioClip phonicsChampionBadgeClip;  // "You started by listening. Now you can read. You are a PHONICS CHAMPION!"
        public AudioClip certificateFinalClip;      // "Here is your certificate. Show somebody — and then let us read a story again, just for fun."

        [Header("5 Book Comprehension Questions")]
        public GraduationQuestionItem[] questions = new GraduationQuestionItem[5];

        [Header("Pinned Story Text Lines (8 lines)")]
        public string[] pinnedStoryLines = new string[]
        {
            "The well.",
            "The bell.",
            "The dog fell in the well.",
            "Dell, tell Dad the dog fell in the well.",
            "Dell rang the bell to tell Dad the dog fell.",
            "Dell had to yell, \"The dog is in the well.\"",
            "Dad ran to Dell at the well.",
            "Dad got the dog out of the well."
        };

        [Header("Audio SFX")]
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip islandWorldLightUpSfx;
        public AudioClip fireworksBoomSfx;
        public AudioClip finalCertificateFanfareSfx;

        private void Reset()
        {
            PopulateDefaultData();
        }

        [ContextMenu("Populate Default Data")]
        public void PopulateDefaultData()
        {
            questions = new GraduationQuestionItem[5];

            // 1: Who rang the bell?
            questions[0] = new GraduationQuestionItem
            {
                questionPrompt = "Who rang the bell?",
                choices = new string[] { "Dell", "Dad", "the dog" },
                correctChoiceIndex = 0,
                storyReferenceLineIndex = 4
            };

            // 2: Who fell in the well?
            questions[1] = new GraduationQuestionItem
            {
                questionPrompt = "Who fell in the well?",
                choices = new string[] { "the dog", "Dell", "Dad" },
                correctChoiceIndex = 0,
                storyReferenceLineIndex = 2
            };

            // 3: What did Dell do first to get help?
            questions[2] = new GraduationQuestionItem
            {
                questionPrompt = "What did Dell do first to get help?",
                choices = new string[] { "rang the bell", "yelled", "ran to Dad" },
                correctChoiceIndex = 0,
                storyReferenceLineIndex = 4
            };

            // 4: Who did Dell tell?
            questions[3] = new GraduationQuestionItem
            {
                questionPrompt = "Who did Dell tell?",
                choices = new string[] { "Dad", "the dog", "the hen" },
                correctChoiceIndex = 0,
                storyReferenceLineIndex = 3
            };

            // 5: What did Dad do?
            questions[4] = new GraduationQuestionItem
            {
                questionPrompt = "What did Dad do?",
                choices = new string[] { "got the dog out", "rang the bell", "fell in the well" },
                correctChoiceIndex = 0,
                storyReferenceLineIndex = 7
            };
        }
    }
}
