using System;
using System.Collections.Generic;
using UnityEngine;

namespace MastersPhonics
{
    public enum Unit6ActivityState
    {
        SectionSelection = -1,
        LearnConceptCards = 0,
        Activity1MagicWand = 1,
        Activity2WhichLongVowel = 2,
        Activity3SoftOrHard = 3,
        Activity4WhyTheE = 4,
        SevenTypesMap = 5,
        UnitChallenge = 6,
        CompletePanel = 7
    }

    public enum MagicEJobType
    {
        MakesVowelLong = 1,
        SoftCandG = 2,
        HoldsAPlace = 3,
        NotAPlural = 4,
        CleVowel = 5,
        VoicedTh = 6,
        ClarifiesMeaning = 7
    }

    public enum VowelSoundType
    {
        ShortA, LongA,
        ShortE, LongE,
        ShortI, LongI,
        ShortO, LongO,
        ShortU, LongU
    }

    public enum SoftHardType
    {
        Hard,
        Soft
    }

    // -------------------------------------------------------------
    // Data Items for Activity 1 (Magic Wand Transformation)
    // -------------------------------------------------------------
    [Serializable]
    public class WandTransformationItem
    {
        public string beforeWord;
        public string beforePictureDesc;
        public Sprite beforeSprite;
        public string afterWord;
        public string afterPictureDesc;
        public Sprite afterSprite;
        public string vowelShiftDesc; // e.g. "short a -> long a"
        public VowelSoundType targetVowelSound;
        public AudioClip beforeAudio;
        public AudioClip afterAudio;
    }

    // -------------------------------------------------------------
    // Data Items for Activity 2 (Which Long Vowel?)
    // -------------------------------------------------------------
    [Serializable]
    public class LongVowelChoiceItem
    {
        public string word;
        public VowelSoundType targetLongVowel; // LongA, LongE, LongI, LongO, LongU
        public AudioClip wordAudio;
    }

    // -------------------------------------------------------------
    // Data Items for Activity 3 (Soft or Hard?)
    // -------------------------------------------------------------
    [Serializable]
    public class SoftHardSwipeItem
    {
        public string word;
        public string targetLetter; // "c" or "g"
        public int targetLetterIndex;
        public SoftHardType soundType; // Hard or Soft
        public string phonemeSound; // "/k/", "/s/", "/g/", "/j/"
        public AudioClip wordAudio;
    }

    // -------------------------------------------------------------
    // Data Items for Activity 4 (Why the E?)
    // -------------------------------------------------------------
    [Serializable]
    public class WhyTheEItem
    {
        public string word;
        public MagicEJobType jobType; // MakesVowelLong (1), HoldsAPlace (3), NotAPlural (4)
        public string clueLetter; // e.g. "v", "u", "i", "s"
        public string explanation;
        public AudioClip wordAudio;
    }

    // -------------------------------------------------------------
    // Data Items for Unit 6 Challenge
    // -------------------------------------------------------------
    [Serializable]
    public class Unit6ChallengeQuestion
    {
        public string prompt;
        public string[] options;
        public int correctIndex;
        public string explanation;
        public string audioClipName;
        public AudioClip customAudio;
    }

    // -------------------------------------------------------------
    // Static Pools & Data Providers
    // -------------------------------------------------------------
    public static class U6_SA_DataTypes_Masters_Phonics
    {
        public static List<WandTransformationItem> GetDefaultTransformationItems()
        {
            return new List<WandTransformationItem>
            {
                new WandTransformationItem { beforeWord = "hat", beforePictureDesc = "a hat", afterWord = "hate", afterPictureDesc = "two children arguing", vowelShiftDesc = "short a to long a", targetVowelSound = VowelSoundType.LongA },
                new WandTransformationItem { beforeWord = "mat", beforePictureDesc = "a doormat", afterWord = "mate", afterPictureDesc = "two friends", vowelShiftDesc = "short a to long a", targetVowelSound = VowelSoundType.LongA },
                new WandTransformationItem { beforeWord = "pet", beforePictureDesc = "a puppy", afterWord = "Pete", afterPictureDesc = "boy waving", vowelShiftDesc = "short e to long e", targetVowelSound = VowelSoundType.LongE },
                new WandTransformationItem { beforeWord = "kit", beforePictureDesc = "a first-aid kit", afterWord = "kite", afterPictureDesc = "a flying kite", vowelShiftDesc = "short i to long i", targetVowelSound = VowelSoundType.LongI },
                new WandTransformationItem { beforeWord = "tap", beforePictureDesc = "a dripping tap", afterWord = "tape", afterPictureDesc = "a roll of tape", vowelShiftDesc = "short a to long a", targetVowelSound = VowelSoundType.LongA },
                new WandTransformationItem { beforeWord = "can", beforePictureDesc = "a tin can", afterWord = "cane", afterPictureDesc = "a walking cane", vowelShiftDesc = "short a to long a", targetVowelSound = VowelSoundType.LongA },
                new WandTransformationItem { beforeWord = "plan", beforePictureDesc = "a written plan", afterWord = "plane", afterPictureDesc = "an aeroplane", vowelShiftDesc = "short a to long a", targetVowelSound = VowelSoundType.LongA },
                new WandTransformationItem { beforeWord = "pin", beforePictureDesc = "a safety pin", afterWord = "pine", afterPictureDesc = "a pine tree", vowelShiftDesc = "short i to long i", targetVowelSound = VowelSoundType.LongI },
                new WandTransformationItem { beforeWord = "not", beforePictureDesc = "a no sign", afterWord = "note", afterPictureDesc = "a written note", vowelShiftDesc = "short o to long o", targetVowelSound = VowelSoundType.LongO },
                new WandTransformationItem { beforeWord = "hop", beforePictureDesc = "a child hopping", afterWord = "hope", afterPictureDesc = "hope is a feeling", vowelShiftDesc = "short o to long o", targetVowelSound = VowelSoundType.LongO },
                new WandTransformationItem { beforeWord = "cut", beforePictureDesc = "scissors cutting", afterWord = "cute", afterPictureDesc = "a kitten", vowelShiftDesc = "short u to long u", targetVowelSound = VowelSoundType.LongU },
                new WandTransformationItem { beforeWord = "tub", beforePictureDesc = "a bathtub", afterWord = "tube", afterPictureDesc = "a test tube", vowelShiftDesc = "short u to long u", targetVowelSound = VowelSoundType.LongU },
                new WandTransformationItem { beforeWord = "cub", beforePictureDesc = "a bear cub", afterWord = "cube", afterPictureDesc = "an ice cube", vowelShiftDesc = "short u to long u", targetVowelSound = VowelSoundType.LongU }
            };
        }

        public static List<LongVowelChoiceItem> GetDefaultWhichLongVowelItems()
        {
            return new List<LongVowelChoiceItem>
            {
                new LongVowelChoiceItem { word = "cake", targetLongVowel = VowelSoundType.LongA },
                new LongVowelChoiceItem { word = "these", targetLongVowel = VowelSoundType.LongE },
                new LongVowelChoiceItem { word = "kite", targetLongVowel = VowelSoundType.LongI },
                new LongVowelChoiceItem { word = "home", targetLongVowel = VowelSoundType.LongO },
                new LongVowelChoiceItem { word = "mule", targetLongVowel = VowelSoundType.LongU },
                new LongVowelChoiceItem { word = "snake", targetLongVowel = VowelSoundType.LongA },
                new LongVowelChoiceItem { word = "theme", targetLongVowel = VowelSoundType.LongE },
                new LongVowelChoiceItem { word = "smile", targetLongVowel = VowelSoundType.LongI },
                new LongVowelChoiceItem { word = "stone", targetLongVowel = VowelSoundType.LongO },
                new LongVowelChoiceItem { word = "cube", targetLongVowel = VowelSoundType.LongU },
                new LongVowelChoiceItem { word = "cave", targetLongVowel = VowelSoundType.LongA },
                new LongVowelChoiceItem { word = "athlete", targetLongVowel = VowelSoundType.LongE },
                new LongVowelChoiceItem { word = "shine", targetLongVowel = VowelSoundType.LongI },
                new LongVowelChoiceItem { word = "phone", targetLongVowel = VowelSoundType.LongO },
                new LongVowelChoiceItem { word = "cute", targetLongVowel = VowelSoundType.LongU }
            };
        }

        public static List<SoftHardSwipeItem> GetDefaultSoftHardItems()
        {
            return new List<SoftHardSwipeItem>
            {
                new SoftHardSwipeItem { word = "cat", targetLetter = "c", targetLetterIndex = 0, soundType = SoftHardType.Hard, phonemeSound = "/k/" },
                new SoftHardSwipeItem { word = "ice", targetLetter = "c", targetLetterIndex = 1, soundType = SoftHardType.Soft, phonemeSound = "/s/" },
                new SoftHardSwipeItem { word = "cake", targetLetter = "c", targetLetterIndex = 0, soundType = SoftHardType.Hard, phonemeSound = "/k/" },
                new SoftHardSwipeItem { word = "cage", targetLetter = "c", targetLetterIndex = 0, soundType = SoftHardType.Hard, phonemeSound = "/k/" },
                new SoftHardSwipeItem { word = "cage", targetLetter = "g", targetLetterIndex = 2, soundType = SoftHardType.Soft, phonemeSound = "/j/" },
                new SoftHardSwipeItem { word = "orange", targetLetter = "g", targetLetterIndex = 4, soundType = SoftHardType.Soft, phonemeSound = "/j/" },
                new SoftHardSwipeItem { word = "cup", targetLetter = "c", targetLetterIndex = 0, soundType = SoftHardType.Hard, phonemeSound = "/k/" },
                new SoftHardSwipeItem { word = "race", targetLetter = "c", targetLetterIndex = 2, soundType = SoftHardType.Soft, phonemeSound = "/s/" },
                new SoftHardSwipeItem { word = "goat", targetLetter = "g", targetLetterIndex = 0, soundType = SoftHardType.Hard, phonemeSound = "/g/" },
                new SoftHardSwipeItem { word = "giant", targetLetter = "g", targetLetterIndex = 0, soundType = SoftHardType.Soft, phonemeSound = "/j/" },
                new SoftHardSwipeItem { word = "city", targetLetter = "c", targetLetterIndex = 0, soundType = SoftHardType.Soft, phonemeSound = "/s/" },
                new SoftHardSwipeItem { word = "cold", targetLetter = "c", targetLetterIndex = 0, soundType = SoftHardType.Hard, phonemeSound = "/k/" },
                new SoftHardSwipeItem { word = "gum", targetLetter = "g", targetLetterIndex = 0, soundType = SoftHardType.Hard, phonemeSound = "/g/" },
                new SoftHardSwipeItem { word = "gem", targetLetter = "g", targetLetterIndex = 0, soundType = SoftHardType.Soft, phonemeSound = "/j/" },
                new SoftHardSwipeItem { word = "cent", targetLetter = "c", targetLetterIndex = 0, soundType = SoftHardType.Soft, phonemeSound = "/s/" },
                new SoftHardSwipeItem { word = "game", targetLetter = "g", targetLetterIndex = 0, soundType = SoftHardType.Hard, phonemeSound = "/g/" },
                new SoftHardSwipeItem { word = "huge", targetLetter = "g", targetLetterIndex = 2, soundType = SoftHardType.Soft, phonemeSound = "/j/" },
                new SoftHardSwipeItem { word = "nice", targetLetter = "c", targetLetterIndex = 2, soundType = SoftHardType.Soft, phonemeSound = "/s/" }
            };
        }

        public static List<WhyTheEItem> GetDefaultWhyTheEItems()
        {
            return new List<WhyTheEItem>
            {
                // Bin 1: Makes the vowel long
                new WhyTheEItem { word = "cake", jobType = MagicEJobType.MakesVowelLong, clueLetter = "a", explanation = "Magic e makes the 'a' say its long sound!" },
                new WhyTheEItem { word = "hide", jobType = MagicEJobType.MakesVowelLong, clueLetter = "i", explanation = "Magic e makes the 'i' say its long sound!" },
                new WhyTheEItem { word = "cube", jobType = MagicEJobType.MakesVowelLong, clueLetter = "u", explanation = "Magic e makes the 'u' say its long sound!" },
                new WhyTheEItem { word = "note", jobType = MagicEJobType.MakesVowelLong, clueLetter = "o", explanation = "Magic e makes the 'o' say its long sound!" },
                new WhyTheEItem { word = "stone", jobType = MagicEJobType.MakesVowelLong, clueLetter = "o", explanation = "Magic e makes the 'o' say its long sound!" },
                new WhyTheEItem { word = "smile", jobType = MagicEJobType.MakesVowelLong, clueLetter = "i", explanation = "Magic e makes the 'i' say its long sound!" },

                // Bin 2: Holds a place (no i, u, v at end)
                new WhyTheEItem { word = "give", jobType = MagicEJobType.HoldsAPlace, clueLetter = "v", explanation = "English words never end in v - the e holds the place!" },
                new WhyTheEItem { word = "have", jobType = MagicEJobType.HoldsAPlace, clueLetter = "v", explanation = "English words never end in v - the e holds the place!" },
                new WhyTheEItem { word = "live", jobType = MagicEJobType.HoldsAPlace, clueLetter = "v", explanation = "English words never end in v - the e holds the place!" },
                new WhyTheEItem { word = "love", jobType = MagicEJobType.HoldsAPlace, clueLetter = "v", explanation = "English words never end in v - the e holds the place!" },
                new WhyTheEItem { word = "tie", jobType = MagicEJobType.HoldsAPlace, clueLetter = "i", explanation = "English words never end in i - the e holds the place!" },
                new WhyTheEItem { word = "blue", jobType = MagicEJobType.HoldsAPlace, clueLetter = "u", explanation = "English words never end in u - the e holds the place!" },

                // Bin 3: Shows word is not plural
                new WhyTheEItem { word = "house", jobType = MagicEJobType.NotAPlural, clueLetter = "s", explanation = "The e shows this is one house, not plural!" },
                new WhyTheEItem { word = "mouse", jobType = MagicEJobType.NotAPlural, clueLetter = "s", explanation = "The e shows this is one mouse, not plural!" },
                new WhyTheEItem { word = "dense", jobType = MagicEJobType.NotAPlural, clueLetter = "s", explanation = "The e shows this word does not end in a plural s!" },
                new WhyTheEItem { word = "horse", jobType = MagicEJobType.NotAPlural, clueLetter = "s", explanation = "The e prevents horse from looking like a plural!" },
                new WhyTheEItem { word = "purse", jobType = MagicEJobType.NotAPlural, clueLetter = "s", explanation = "The e prevents purse from looking like a plural!" },
                new WhyTheEItem { word = "nurse", jobType = MagicEJobType.NotAPlural, clueLetter = "s", explanation = "The e prevents nurse from looking like a plural!" }
            };
        }

        public static List<Unit6ChallengeQuestion> GetDefaultChallengeQuestions()
        {
            return new List<Unit6ChallengeQuestion>
            {
                new Unit6ChallengeQuestion
                {
                    prompt = "Add Magic 'e' to <b>rob</b>. What new word do you get?",
                    options = new string[] { "robe", "rope", "robb" },
                    correctIndex = 0,
                    explanation = "rob becomes robe! The vowel changes from short o to long o."
                },
                new Unit6ChallengeQuestion
                {
                    prompt = "Listen to the word <b>stone</b>. Which vowel sound does it make?",
                    options = new string[] { "Long o", "Short o", "Long u" },
                    correctIndex = 0,
                    explanation = "stone has a long o sound (/oh/) because of the silent e!"
                },
                new Unit6ChallengeQuestion
                {
                    prompt = "The letters <b>c</b> and <b>g</b> go soft before...",
                    options = new string[] { "e, i, y", "a, o, u", "any consonant" },
                    correctIndex = 0,
                    explanation = "c and g go soft before e, i, or y (like in ice, city, gym)!"
                },
                new Unit6ChallengeQuestion
                {
                    prompt = "In the word <b>giant</b>, does the 'g' make a hard or soft sound?",
                    options = new string[] { "Soft (/j/)", "Hard (/g/)" },
                    correctIndex = 0,
                    explanation = "The 'g' in giant comes before 'i', so it makes the soft /j/ sound!"
                },
                new Unit6ChallengeQuestion
                {
                    prompt = "In the word <b>nurse</b>, why is the silent 'e' at the end?",
                    options = new string[] { "Shows it is not plural", "Makes the vowel long", "Makes n soft" },
                    correctIndex = 0,
                    explanation = "The 'e' in nurse shows that the word is not a plural ending in -s!"
                },
                new Unit6ChallengeQuestion
                {
                    prompt = "In the word <b>have</b>, why is the silent 'e' at the end?",
                    options = new string[] { "Holds the place (no v at end)", "Makes 'a' long", "Makes 'h' silent" },
                    correctIndex = 0,
                    explanation = "English words never end in v, so the e is just holding the place!"
                },
                new Unit6ChallengeQuestion
                {
                    prompt = "Which of these words has a <b>short vowel sound</b>?",
                    options = new string[] { "give", "hive", "dive" },
                    correctIndex = 0,
                    explanation = "give has a short i sound! The e is only there because English words cannot end in v."
                },
                new Unit6ChallengeQuestion
                {
                    prompt = "The word <b>breathe</b> has an 'e' at the end. What job does it do?",
                    options = new string[] { "Makes 'th' voiced", "Makes 'b' soft", "Makes 'r' silent" },
                    correctIndex = 0,
                    explanation = "The final e in breathe makes 'th' say its voiced buzzing sound!"
                },
                new Unit6ChallengeQuestion
                {
                    prompt = "What is the difference between <b>or</b> and <b>ore</b>?",
                    options = new string[] { "They mean different things", "They sound completely different", "There is no difference" },
                    correctIndex = 0,
                    explanation = "or and ore sound the same, but the e changes the meaning to mineral rock!"
                },
                new Unit6ChallengeQuestion
                {
                    prompt = "If you see a new word <b>spate</b>, what sound will the 'a' make?",
                    options = new string[] { "Long a (/ay/)", "Short a (/a/)" },
                    correctIndex = 0,
                    explanation = "The Magic e at the end makes the 'a' say its long vowel name!"
                }
            };
        }
    }
}
