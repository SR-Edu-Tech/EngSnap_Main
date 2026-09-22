using System;
using System.Collections.Generic;
using UnityEngine;

namespace EngSnap.ConfusedWords
{
    public enum U7_ConfusedPair
    {
        Quiet_Quite,
        Diary_Dairy,
        Whoever_Whomever
    }

    public enum U7_TargetWord
    {
        Quiet,
        Quite,
        Diary,
        Dairy,
        Whoever,
        Whomever
    }

    [Serializable]
    public struct U7_WordEntry
    {
        public U7_TargetWord targetWord;
        public U7_ConfusedPair pair;
        public string wordText;
        public string verbatimMeaning;
        public string phoneticHint;
        public bool bookVerbatim;

        public U7_WordEntry(
            U7_TargetWord targetWord,
            U7_ConfusedPair pair,
            string wordText,
            string verbatimMeaning,
            string phoneticHint = "",
            bool bookVerbatim = true)
        {
            this.targetWord = targetWord;
            this.pair = pair;
            this.wordText = wordText;
            this.verbatimMeaning = verbatimMeaning;
            this.phoneticHint = phoneticHint;
            this.bookVerbatim = bookVerbatim;
        }
    }

    [Serializable]
    public struct U7_SentenceEntry
    {
        public string fullSentence;
        public string gapSentence;
        public U7_TargetWord targetWord;
        public U7_ConfusedPair pair;
        public bool acceptsEitherPairWord;
        public bool bookVerbatim;

        public U7_SentenceEntry(
            string fullSentence,
            string gapSentence,
            U7_TargetWord targetWord,
            U7_ConfusedPair pair,
            bool acceptsEitherPairWord = false,
            bool bookVerbatim = true)
        {
            this.fullSentence = fullSentence;
            this.gapSentence = gapSentence;
            this.targetWord = targetWord;
            this.pair = pair;
            this.acceptsEitherPairWord = acceptsEitherPairWord;
            this.bookVerbatim = bookVerbatim;
        }
    }

    [Serializable]
    public struct U7_MeaningEntry
    {
        public string meaningText;
        public U7_TargetWord targetWord;
        public U7_ConfusedPair pair;
        public bool bookVerbatim;

        public U7_MeaningEntry(
            string meaningText,
            U7_TargetWord targetWord,
            U7_ConfusedPair pair,
            bool bookVerbatim = true)
        {
            this.meaningText = meaningText;
            this.targetWord = targetWord;
            this.pair = pair;
            this.bookVerbatim = bookVerbatim;
        }
    }

    [Serializable]
    public struct U7_SubjectObjectTestEntry
    {
        public string sentence;
        public string subjectTestOption; // "he/she"
        public string objectTestOption;  // "him/her"
        public U7_TargetWord expectedWord;
        public bool acceptsBoth;
        public string explanation;

        public U7_SubjectObjectTestEntry(
            string sentence,
            string subjectTestOption,
            string objectTestOption,
            U7_TargetWord expectedWord,
            bool acceptsBoth = false,
            string explanation = "")
        {
            this.sentence = sentence;
            this.subjectTestOption = subjectTestOption;
            this.objectTestOption = objectTestOption;
            this.expectedWord = expectedWord;
            this.acceptsBoth = acceptsBoth;
            this.explanation = explanation;
        }
    }

    public static class M3A_U7_ConfusedWordsData
    {
        public const string UnitId = "M3A_U7";
        public const string UnitName = "Confused English Words";
        public const string UnitTheme = "Word Detective Agency";
        public const string BadgeId = "M3A_U7_WordDetective";
        public const string BadgeDisplayName = "Word Detective";

        public static readonly List<U7_WordEntry> WordBank = new List<U7_WordEntry>
        {
            new U7_WordEntry(
                U7_TargetWord.Quiet,
                U7_ConfusedPair.Quiet_Quite,
                "QUIET",
                "no noise, silent"
            ),
            new U7_WordEntry(
                U7_TargetWord.Quite,
                U7_ConfusedPair.Quiet_Quite,
                "QUITE",
                "not exactly, not perfectly"
            ),
            new U7_WordEntry(
                U7_TargetWord.Diary,
                U7_ConfusedPair.Diary_Dairy,
                "DIARY",
                "is a book that you write daily events, records, experiences, etc."
            ),
            new U7_WordEntry(
                U7_TargetWord.Dairy,
                U7_ConfusedPair.Diary_Dairy,
                "DAIRY",
                "A product that comes from animal milk."
            ),
            new U7_WordEntry(
                U7_TargetWord.Whoever,
                U7_ConfusedPair.Whoever_Whomever,
                "WHOEVER",
                "stands in the position of subject"
            ),
            new U7_WordEntry(
                U7_TargetWord.Whomever,
                U7_ConfusedPair.Whoever_Whomever,
                "WHOMEVER",
                "stands in the position of an object"
            )
        };

        public static readonly List<U7_SentenceEntry> SentenceBank = new List<U7_SentenceEntry>
        {
            // Pair 1: Quiet / Quite
            new U7_SentenceEntry("Be quiet, please.", "Be ____, please.", U7_TargetWord.Quiet, U7_ConfusedPair.Quiet_Quite),
            new U7_SentenceEntry("The library is very quiet.", "The library is very ____.", U7_TargetWord.Quiet, U7_ConfusedPair.Quiet_Quite),
            new U7_SentenceEntry("I am quite sure about the answer.", "I am ____ sure about the answer.", U7_TargetWord.Quite, U7_ConfusedPair.Quiet_Quite),
            new U7_SentenceEntry("It is quite cold today.", "It is ____ cold today.", U7_TargetWord.Quite, U7_ConfusedPair.Quiet_Quite),

            // Pair 2: Diary / Dairy
            new U7_SentenceEntry("I write in my diary every night.", "I write in my ____ every night.", U7_TargetWord.Diary, U7_ConfusedPair.Diary_Dairy),
            new U7_SentenceEntry("She bought milk from the dairy farm.", "She bought milk from the ____ farm.", U7_TargetWord.Dairy, U7_ConfusedPair.Diary_Dairy),
            new U7_SentenceEntry("Dairy products are made from milk.", "____ products are made from milk.", U7_TargetWord.Dairy, U7_ConfusedPair.Diary_Dairy),

            // Pair 3: Whoever / Whomever
            new U7_SentenceEntry("Whoever arrives first will get the prize.", "____ arrives first will get the prize.", U7_TargetWord.Whoever, U7_ConfusedPair.Whoever_Whomever),
            new U7_SentenceEntry("Give the ticket to whomever you choose.", "Give the ticket to ____ you choose.", U7_TargetWord.Whomever, U7_ConfusedPair.Whoever_Whomever),
            
            // Special Acceptance Sentence: appears in book with both variants allowed
            new U7_SentenceEntry("She always smiles at whoever she meets.", "She always smiles at ____ she meets.", U7_TargetWord.Whoever, U7_ConfusedPair.Whoever_Whomever, true),
            new U7_SentenceEntry("She always smiles at whomever she meets.", "She always smiles at ____ she meets.", U7_TargetWord.Whomever, U7_ConfusedPair.Whoever_Whomever, true)
        };

        public static readonly List<U7_SubjectObjectTestEntry> SubjectObjectTestBank = new List<U7_SubjectObjectTestEntry>
        {
            new U7_SubjectObjectTestEntry(
                "____ wants to join the detective club is welcome.",
                "he/she",
                "him/her",
                U7_TargetWord.Whoever,
                false,
                "Subject position: 'he/she wants to join' fits -> WHOEVER"
            ),
            new U7_SubjectObjectTestEntry(
                "You may invite ____ you like to the case briefing.",
                "he/she",
                "him/her",
                U7_TargetWord.Whomever,
                false,
                "Object position: 'you like him/her' fits -> WHOMEVER"
            ),
            new U7_SubjectObjectTestEntry(
                "She always smiles at ____ she meets.",
                "he/she",
                "him/her",
                U7_TargetWord.Whoever,
                true,
                "Special book acceptance: both 'whoever' and 'whomever' are accepted."
            )
        };

        public static U7_WordEntry GetWordEntry(U7_TargetWord word)
        {
            return WordBank.Find(entry => entry.targetWord == word);
        }

        public static List<U7_WordEntry> GetWordsForPair(U7_ConfusedPair pair)
        {
            return WordBank.FindAll(entry => entry.pair == pair);
        }

        public static bool IsValidWordForSentence(string sentenceText, U7_TargetWord word)
        {
            var matched = SentenceBank.Find(s => s.fullSentence.Equals(sentenceText, StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(matched.fullSentence))
            {
                return false;
            }

            if (matched.acceptsEitherPairWord)
            {
                return matched.pair == U7_ConfusedPair.Whoever_Whomever &&
                       (word == U7_TargetWord.Whoever || word == U7_TargetWord.Whomever);
            }

            return matched.targetWord == word;
        }
    }
}
