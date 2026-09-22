using System;
using System.Collections.Generic;
using UnityEngine;

namespace MastersPhonics
{
    // =========================================================================
    // Unit 8 Enums
    // =========================================================================

    public enum RControlledPattern
    {
        AR,
        OR,
        ER,
        IR,
        UR,
        ER_Weak // Unstressed schwa+r at the end of words
    }

    public enum RControlledSound
    {
        Sound_AR, // /ɑːr/ as in car
        Sound_OR, // /ɔːr/ as in corn
        Sound_ER  // /ɜːr/ or /ər/ as in bird, her, turn
    }

    public enum ErRuleCategory
    {
        End_Quiet,      // Word-final unaccented /ər/ -> spelled 'er' (e.g., tiger, spider)
        Middle_Stressed // Middle stressed /ɜːr/ -> spelled 'ir' or 'ur' (e.g., bird, turn)
    }

    public enum ChallengeQuestionType
    {
        MinimalPair_Audio,      // Cat vs Cart
        Text_MCQ,               // Multiple choice concept check
        SoundSort_3Bins,        // 3 cards to 3 sound bins
        SoundCompare_MCQ,       // Middle sound comparison
        Spelling_GapFill,       // Choose er/ir/ur for blanked word
        ErRule_YesNo,           // Is sound at end and quiet?
        RuleExplanation_MCQ,    // Why does 'a' in car change?
        TransferReasoning_MCQ   // Unseen word rule deduction (whisker)
    }

    // =========================================================================
    // Activity Data Models
    // =========================================================================

    [Serializable]
    public class BossyRMinimalPairItem
    {
        public string withoutRWord;
        public string withRWord;
        public RControlledPattern pattern;
        public bool targetIsWithR = true; // Which word is prompted
        public AudioClip withoutRAudio;
        public AudioClip withRAudio;

        public BossyRMinimalPairItem(string noR, string withR, RControlledPattern pat, bool targetWithR = true)
        {
            withoutRWord = noR;
            withRWord = withR;
            pattern = pat;
            targetIsWithR = targetWithR;
        }
    }

    [Serializable]
    public class ThreeSoundsItem
    {
        public string word;
        public RControlledSound targetSound;
        public RControlledPattern spellingPattern;
        public AudioClip wordAudio;

        public ThreeSoundsItem(string w, RControlledSound sound, RControlledPattern spelling)
        {
            word = w;
            targetSound = sound;
            spellingPattern = spelling;
        }
    }

    [Serializable]
    public class FiveFamiliesItem
    {
        public string word;
        public RControlledPattern family;
        public int round = 1; // 1 = ar/or/er, 2 = ir/ur/er
        public AudioClip wordAudio;

        public FiveFamiliesItem(string w, RControlledPattern fam, int r = 1)
        {
            word = w;
            family = fam;
            round = r;
        }
    }

    [Serializable]
    public class ErRuleItem
    {
        public string word;
        public ErRuleCategory ruleCategory;
        public string team; // "er", "ir", or "ur"
        public AudioClip wordAudio;

        public ErRuleItem(string w, ErRuleCategory cat, string tm = "er")
        {
            word = w;
            ruleCategory = cat;
            team = tm;
        }
    }

    [Serializable]
    public class SpellingLabItem
    {
        public string word;
        public string correctTeam; // "er", "ir", or "ur"
        public string displayPrompt; // e.g. "b __ __ d"
        public bool isRuleReward = false; // Items like tiger/ladder that follow Activity 4's rule
        public AudioClip wordAudio;

        public SpellingLabItem(string w, string team, string prompt, bool reward = false)
        {
            word = w;
            correctTeam = team;
            displayPrompt = prompt;
            isRuleReward = reward;
        }
    }

    [Serializable]
    public class U8ChallengeQuestion
    {
        public int questionNumber;
        public ChallengeQuestionType type;
        public string promptText;
        public List<string> options;
        public int correctIndex;
        public string targetWord;
        public AudioClip promptAudio;
        public string ruleTip;

        public U8ChallengeQuestion(int num, ChallengeQuestionType t, string prompt, List<string> opts, int correctIdx, string word = "", string tip = "")
        {
            questionNumber = num;
            type = t;
            promptText = prompt;
            options = opts;
            correctIndex = correctIdx;
            targetWord = word;
            ruleTip = tip;
        }
    }

    // =========================================================================
    // Static Default Word Banks & Generator Helpers
    // =========================================================================

    public static class U8_SA_DataTypes_Masters_Phonics
    {
        // ---------------------------------------------------------------------
        // Activity 1: Bossy R (12 Minimal Pairs)
        // ---------------------------------------------------------------------
        public static List<BossyRMinimalPairItem> GetDefaultActivity1Items()
        {
            return new List<BossyRMinimalPairItem>
            {
                new BossyRMinimalPairItem("cat", "cart", RControlledPattern.AR, true),
                new BossyRMinimalPairItem("bid", "bird", RControlledPattern.IR, true),
                new BossyRMinimalPairItem("ten", "term", RControlledPattern.ER, true),
                new BossyRMinimalPairItem("hut", "hurt", RControlledPattern.UR, true),
                new BossyRMinimalPairItem("cod", "cord", RControlledPattern.OR, true),
                new BossyRMinimalPairItem("bun", "burn", RControlledPattern.UR, true),
                new BossyRMinimalPairItem("bad", "bard", RControlledPattern.AR, true),
                new BossyRMinimalPairItem("shut", "shirt", RControlledPattern.IR, true),
                new BossyRMinimalPairItem("pot", "port", RControlledPattern.OR, true),
                new BossyRMinimalPairItem("hat", "heart", RControlledPattern.AR, true),
                new BossyRMinimalPairItem("fun", "fern", RControlledPattern.ER, true),
                new BossyRMinimalPairItem("cub", "curb", RControlledPattern.UR, true)
            };
        }

        // ---------------------------------------------------------------------
        // Activity 2: Three Sounds Word Pool (18 per run: 6 /ar/, 6 /or/, 6 /er/)
        // ---------------------------------------------------------------------
        public static List<ThreeSoundsItem> GetDefaultActivity2Items()
        {
            return new List<ThreeSoundsItem>
            {
                // /ar/ as in car (6 items)
                new ThreeSoundsItem("car", RControlledSound.Sound_AR, RControlledPattern.AR),
                new ThreeSoundsItem("star", RControlledSound.Sound_AR, RControlledPattern.AR),
                new ThreeSoundsItem("farm", RControlledSound.Sound_AR, RControlledPattern.AR),
                new ThreeSoundsItem("yard", RControlledSound.Sound_AR, RControlledPattern.AR),
                new ThreeSoundsItem("bark", RControlledSound.Sound_AR, RControlledPattern.AR),
                new ThreeSoundsItem("shark", RControlledSound.Sound_AR, RControlledPattern.AR),

                // /or/ as in corn (6 items)
                new ThreeSoundsItem("corn", RControlledSound.Sound_OR, RControlledPattern.OR),
                new ThreeSoundsItem("horn", RControlledSound.Sound_OR, RControlledPattern.OR),
                new ThreeSoundsItem("storm", RControlledSound.Sound_OR, RControlledPattern.OR),
                new ThreeSoundsItem("fork", RControlledSound.Sound_OR, RControlledPattern.OR),
                new ThreeSoundsItem("horse", RControlledSound.Sound_OR, RControlledPattern.OR),
                new ThreeSoundsItem("port", RControlledSound.Sound_OR, RControlledPattern.OR),

                // /er/ as in bird (6 items: balanced with er, ir, ur)
                new ThreeSoundsItem("bird", RControlledSound.Sound_ER, RControlledPattern.IR),
                new ThreeSoundsItem("girl", RControlledSound.Sound_ER, RControlledPattern.IR),
                new ThreeSoundsItem("turn", RControlledSound.Sound_ER, RControlledPattern.UR),
                new ThreeSoundsItem("purple", RControlledSound.Sound_ER, RControlledPattern.UR),
                new ThreeSoundsItem("her", RControlledSound.Sound_ER, RControlledPattern.ER),
                new ThreeSoundsItem("term", RControlledSound.Sound_ER, RControlledPattern.ER)
            };
        }

        // ---------------------------------------------------------------------
        // Activity 3: Five Families (2 Rounds of 12 items)
        // ---------------------------------------------------------------------
        public static List<FiveFamiliesItem> GetDefaultActivity3ItemsRoundA()
        {
            return new List<FiveFamiliesItem>
            {
                // ar family (4)
                new FiveFamiliesItem("harp", RControlledPattern.AR, 1),
                new FiveFamiliesItem("barn", RControlledPattern.AR, 1),
                new FiveFamiliesItem("cards", RControlledPattern.AR, 1),
                new FiveFamiliesItem("garden", RControlledPattern.AR, 1),

                // or family (4)
                new FiveFamiliesItem("horse", RControlledPattern.OR, 1),
                new FiveFamiliesItem("fork", RControlledPattern.OR, 1),
                new FiveFamiliesItem("cork", RControlledPattern.OR, 1),
                new FiveFamiliesItem("torch", RControlledPattern.OR, 1),

                // er family (4)
                new FiveFamiliesItem("her", RControlledPattern.ER, 1),
                new FiveFamiliesItem("spider", RControlledPattern.ER, 1),
                new FiveFamiliesItem("ladder", RControlledPattern.ER, 1),
                new FiveFamiliesItem("tiger", RControlledPattern.ER, 1)
            };
        }

        public static List<FiveFamiliesItem> GetDefaultActivity3ItemsRoundB()
        {
            return new List<FiveFamiliesItem>
            {
                // ir family (4)
                new FiveFamiliesItem("bird", RControlledPattern.IR, 2),
                new FiveFamiliesItem("skirt", RControlledPattern.IR, 2),
                new FiveFamiliesItem("swirl", RControlledPattern.IR, 2),
                new FiveFamiliesItem("circle", RControlledPattern.IR, 2),

                // ur family (4)
                new FiveFamiliesItem("fur", RControlledPattern.UR, 2),
                new FiveFamiliesItem("turn", RControlledPattern.UR, 2),
                new FiveFamiliesItem("purse", RControlledPattern.UR, 2),
                new FiveFamiliesItem("turkey", RControlledPattern.UR, 2),

                // er family (4)
                new FiveFamiliesItem("germ", RControlledPattern.ER, 2),
                new FiveFamiliesItem("term", RControlledPattern.ER, 2),
                new FiveFamiliesItem("clerk", RControlledPattern.ER, 2),
                new FiveFamiliesItem("ginger", RControlledPattern.ER, 2)
            };
        }

        // ---------------------------------------------------------------------
        // Activity 4: The -er Rule (16 items: 8 End/Quiet, 8 Middle/Stressed)
        // ---------------------------------------------------------------------
        public static List<ErRuleItem> GetDefaultActivity4Items()
        {
            return new List<ErRuleItem>
            {
                // END, quiet -> er (8 items)
                new ErRuleItem("tiger", ErRuleCategory.End_Quiet, "er"),
                new ErRuleItem("spider", ErRuleCategory.End_Quiet, "er"),
                new ErRuleItem("ladder", ErRuleCategory.End_Quiet, "er"),
                new ErRuleItem("ginger", ErRuleCategory.End_Quiet, "er"),
                new ErRuleItem("flower", ErRuleCategory.End_Quiet, "er"),
                new ErRuleItem("biker", ErRuleCategory.End_Quiet, "er"),
                new ErRuleItem("boxer", ErRuleCategory.End_Quiet, "er"),
                new ErRuleItem("buyer", ErRuleCategory.End_Quiet, "er"),

                // MIDDLE, stressed -> ir / ur (8 items)
                new ErRuleItem("bird", ErRuleCategory.Middle_Stressed, "ir"),
                new ErRuleItem("girl", ErRuleCategory.Middle_Stressed, "ir"),
                new ErRuleItem("shirt", ErRuleCategory.Middle_Stressed, "ir"),
                new ErRuleItem("skirt", ErRuleCategory.Middle_Stressed, "ir"),
                new ErRuleItem("turn", ErRuleCategory.Middle_Stressed, "ur"),
                new ErRuleItem("burn", ErRuleCategory.Middle_Stressed, "ur"),
                new ErRuleItem("purse", ErRuleCategory.Middle_Stressed, "ur"),
                new ErRuleItem("hurt", ErRuleCategory.Middle_Stressed, "ur")
            };
        }

        // ---------------------------------------------------------------------
        // Activity 5: Spelling Lab (15 items across 3 rounds)
        // ---------------------------------------------------------------------
        public static List<SpellingLabItem> GetDefaultActivity5Items()
        {
            return new List<SpellingLabItem>
            {
                // Round A: ir words (5)
                new SpellingLabItem("bird", "ir", "b <color=#0284C7>_ _</color> d"),
                new SpellingLabItem("girl", "ir", "g <color=#0284C7>_ _</color> l"),
                new SpellingLabItem("shirt", "ir", "sh <color=#0284C7>_ _</color> t"),
                new SpellingLabItem("third", "ir", "th <color=#0284C7>_ _</color> d"),
                new SpellingLabItem("dirt", "ir", "d <color=#0284C7>_ _</color> t"),

                // Round B: ur words (5)
                new SpellingLabItem("turn", "ur", "t <color=#0284C7>_ _</color> n"),
                new SpellingLabItem("burn", "ur", "b <color=#0284C7>_ _</color> n"),
                new SpellingLabItem("nurse", "ur", "n <color=#0284C7>_ _</color> se"),
                new SpellingLabItem("purse", "ur", "p <color=#0284C7>_ _</color> se"),
                new SpellingLabItem("hurt", "ur", "h <color=#0284C7>_ _</color> t"),

                // Round C: mixed including rule rewards (5)
                new SpellingLabItem("tiger", "er", "tig <color=#0284C7>_ _</color>", true),
                new SpellingLabItem("circle", "ir", "c <color=#0284C7>_ _</color> cle"),
                new SpellingLabItem("turkey", "ur", "t <color=#0284C7>_ _</color> key"),
                new SpellingLabItem("ladder", "er", "ladd <color=#0284C7>_ _</color>", true),
                new SpellingLabItem("thirty", "ir", "th <color=#0284C7>_ _</color> ty")
            };
        }

        // ---------------------------------------------------------------------
        // Unit Challenge (10 Mixed Questions)
        // ---------------------------------------------------------------------
        public static List<U8ChallengeQuestion> GetDefaultChallengeQuestions()
        {
            return new List<U8ChallengeQuestion>
            {
                new U8ChallengeQuestion(1, ChallengeQuestionType.MinimalPair_Audio, "Listen carefully. Which word has the Bossy R?", new List<string> { "cat", "cart" }, 1, "cart", "The 'r' controls the vowel sound in cart."),
                new U8ChallengeQuestion(2, ChallengeQuestionType.Text_MCQ, "Which two spellings make completely DIFFERENT sounds?", new List<string> { "ar and or", "er and ir", "ir and ur" }, 0, "", "ar (/ɑːr/) and or (/ɔːr/) make two distinct sounds."),
                new U8ChallengeQuestion(3, ChallengeQuestionType.SoundSort_3Bins, "Sort: What sound does 'purple' make?", new List<string> { "/ar/ as in car", "/or/ as in corn", "/er/ as in bird" }, 2, "purple", "ur in purple makes the /er/ sound."),
                new U8ChallengeQuestion(4, ChallengeQuestionType.SoundCompare_MCQ, "In her, bird, and turn, the middle vowel sound is...", new List<string> { "The same in all three", "Different in all three", "The same in only two" }, 0, "", "er, ir, and ur all share the identical /ɜːr/ sound."),
                new U8ChallengeQuestion(5, ChallengeQuestionType.Spelling_GapFill, "Spell 'nurse': n __ __ se", new List<string> { "er", "ir", "ur" }, 2, "nurse", "nurse is spelled with 'ur'."),
                new U8ChallengeQuestion(6, ChallengeQuestionType.ErRule_YesNo, "In flower, is the r-sound at the END and quiet?", new List<string> { "Yes (Spelled er)", "No (Spelled ir/ur)" }, 0, "flower", "The quiet ending /ər/ is spelled 'er'."),
                new U8ChallengeQuestion(7, ChallengeQuestionType.Spelling_GapFill, "Finish 'tiger': tig __ __", new List<string> { "er", "ir", "ur" }, 0, "tiger", "Unstressed word-final /ər/ takes 'er'."),
                new U8ChallengeQuestion(8, ChallengeQuestionType.RuleExplanation_MCQ, "In car, why doesn't the letter 'a' say its name?", new List<string> { "The Bossy R takes over", "It is a closed syllable", "It is a vowel team" }, 0, "car", "The Bossy R overrules the vowel completely."),
                new U8ChallengeQuestion(9, ChallengeQuestionType.SoundSort_3Bins, "Sort: What sound does 'torch' make?", new List<string> { "/ar/ as in car", "/or/ as in corn", "/er/ as in bird" }, 1, "torch", "or in torch makes the /or/ sound."),
                new U8ChallengeQuestion(10, ChallengeQuestionType.TransferReasoning_MCQ, "You have never seen 'whisker'. How is the quiet last sound spelled?", new List<string> { "er", "ir", "ur" }, 0, "whisker", "The quiet ending rule means it is spelled 'er'.")
            };
        }
    }
}
