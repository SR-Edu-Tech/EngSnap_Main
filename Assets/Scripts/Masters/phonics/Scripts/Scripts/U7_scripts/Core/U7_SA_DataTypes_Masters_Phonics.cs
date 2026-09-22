using System;
using System.Collections.Generic;
using UnityEngine;

namespace MastersPhonics
{
    // =========================================================================
    // Enums for Unit 7: Vowel Teams & Diphthongs
    // =========================================================================

    public enum VowelTeamType
    {
        AI,
        OA,
        EE,
        I_E,    // Split team: ride, kite, bike, lime, etc.
        AY,
        EA,
        EI,
        EY,
        IE,
        IGH,
        OW,
        OE,
        OO,
        UE,
        UI,
        EW,
        AU,
        AW,
        OI,
        OY,
        OU
    }

    public enum SoundCategoryType
    {
        LongA_AY,      // /eɪ/
        LongE_EE,      // /iː/
        LongO_OH,      // /əʊ/
        LongI_EYE,     // /aɪ/
        LongU_OO,      // /uː/
        Diphthong_OY,  // /ɔɪ/
        Diphthong_OW,  // /aʊ/
        Other
    }

    public enum SoundMovementType
    {
        Glide,  // Diphthong: coin, brown, pie, boat, toy, house, light, day
        Hold    // Monophthong / steady vowel: see, moon, look, green, fruit, saw, drew, team
    }

    public enum DiphthongPositionRule
    {
        Middle, // oi, ou
        End     // oy, ow
    }

    // =========================================================================
    // Activity 1: Team Up (GM-04 Picture Gap Fill)
    // =========================================================================

    [Serializable]
    public class TeamUpItem
    {
        public string fullWord;
        public string gappedDisplay;      // "s n _ _ l" or "k _ t _" for split
        public VowelTeamType correctTeam;
        public bool isSplitTeam;          // true for i_e
        public Sprite pictureSprite;
        public AudioClip wordAudio;
        public VowelTeamType[] candidateTiles; // 4 choices

        public TeamUpItem(string word, string display, VowelTeamType team, bool split = false)
        {
            fullWord = word;
            gappedDisplay = display;
            correctTeam = team;
            isSplitTeam = split;
            candidateTiles = new VowelTeamType[] { VowelTeamType.AI, VowelTeamType.OA, VowelTeamType.EE, VowelTeamType.I_E };
        }
    }

    // =========================================================================
    // Activity 2: Word Families (GM-03 4-Bin Sorter)
    // =========================================================================

    [Serializable]
    public class WordFamilyItem
    {
        public string word;
        public VowelTeamType team;
        public AudioClip wordAudio;

        public WordFamilyItem(string w, VowelTeamType t)
        {
            word = w;
            team = t;
        }
    }

    [Serializable]
    public class WordFamilyRound
    {
        public string roundName;
        public VowelTeamType[] binTeams;
        public List<WordFamilyItem> items = new List<WordFamilyItem>();

        public WordFamilyRound(string name, VowelTeamType[] bins)
        {
            roundName = name;
            binTeams = bins;
        }
    }

    // =========================================================================
    // Activity 3: Same Sound (GM-03 3-Bin Sound Sorter)
    // =========================================================================

    [Serializable]
    public class SameSoundItem
    {
        public string word;
        public string spellingTeam;       // "ai", "ay", "ei", "ee", etc.
        public SoundCategoryType targetSound;
        public AudioClip wordAudio;

        public SameSoundItem(string w, string spelling, SoundCategoryType sound)
        {
            word = w;
            spellingTeam = spelling;
            targetSound = sound;
        }
    }

    // =========================================================================
    // Activity 4: Glide or Hold? (GM-04 Auditory Waveform Discrimination)
    // =========================================================================

    [Serializable]
    public class GlideOrHoldItem
    {
        public string word;
        public string ipaSymbol;
        public SoundMovementType movementType;
        public bool isKeyMonophthongTeam; // moon, look, fruit, saw, drew (separation test)
        public AudioClip naturalAudio;
        public AudioClip stretchedAudio;

        public GlideOrHoldItem(string w, string ipa, SoundMovementType type, bool isKey = false)
        {
            word = w;
            ipaSymbol = ipa;
            movementType = type;
            isKeyMonophthongTeam = isKey;
        }
    }

    // =========================================================================
    // Activity 5: Glide Families (GM-03s Positional Swipe Sorter)
    // =========================================================================

    [Serializable]
    public class GlideSwipeItem
    {
        public string word;
        public string team;               // "oi", "oy", "ou", "ow"
        public DiphthongPositionRule positionRule;
        public string phoneticSpelling;
        public AudioClip wordAudio;

        public GlideSwipeItem(string w, string t, DiphthongPositionRule pos, string phon = "")
        {
            word = w;
            team = t;
            positionRule = pos;
            phoneticSpelling = string.IsNullOrEmpty(phon) ? w : phon;
        }
    }

    // =========================================================================
    // Activity 6: Team Rush (GM-08 60s Speed Round)
    // =========================================================================

    [Serializable]
    public class TeamRushItem
    {
        public string word;
        public SoundCategoryType targetChute;
        public AudioClip wordAudio;

        public TeamRushItem(string w, SoundCategoryType chute)
        {
            word = w;
            targetChute = chute;
        }
    }

    // =========================================================================
    // Unit 7 Challenge Question
    // =========================================================================

    [Serializable]
    public class Unit7ChallengeQuestion
    {
        public string prompt;
        public string[] options;
        public int correctIndex;
        public string explanation;
        public string audioClipName;
        public AudioClip customAudio;

        public Unit7ChallengeQuestion(string p, string[] opts, int correct, string expl, string audio = "")
        {
            prompt = p;
            options = opts;
            correctIndex = correct;
            explanation = expl;
            audioClipName = audio;
        }
    }

    // =========================================================================
    // Static Fallback Data Providers
    // =========================================================================

    public static class U7_SA_DataTypes_Masters_Phonics
    {
        // ---------------------------------------------------------------------
        // Activity 1 Default Items (16 Items: 4 each for ai, oa, ee, i_e)
        // ---------------------------------------------------------------------
        public static List<TeamUpItem> GetDefaultActivity1Items()
        {
            return new List<TeamUpItem>
            {
                // ai
                new TeamUpItem("rain", "r _ _ n", VowelTeamType.AI),
                new TeamUpItem("snail", "s n _ _ l", VowelTeamType.AI),
                new TeamUpItem("train", "t r _ _ n", VowelTeamType.AI),
                new TeamUpItem("paint", "p _ _ n t", VowelTeamType.AI),

                // oa
                new TeamUpItem("boat", "b _ _ t", VowelTeamType.OA),
                new TeamUpItem("road", "r _ _ d", VowelTeamType.OA),
                new TeamUpItem("soap", "s _ _ p", VowelTeamType.OA),
                new TeamUpItem("goat", "g _ _ t", VowelTeamType.OA),

                // ee
                new TeamUpItem("bee", "b _ _", VowelTeamType.EE),
                new TeamUpItem("tree", "t r _ _", VowelTeamType.EE),
                new TeamUpItem("queen", "q u _ _ n", VowelTeamType.EE),
                new TeamUpItem("sweet", "s w _ _ t", VowelTeamType.EE),

                // i_e (split)
                new TeamUpItem("kite", "k _ t _", VowelTeamType.I_E, true),
                new TeamUpItem("bike", "b _ k _", VowelTeamType.I_E, true),
                new TeamUpItem("ride", "r _ d _", VowelTeamType.I_E, true),
                new TeamUpItem("lime", "l _ m _", VowelTeamType.I_E, true)
            };
        }

        // ---------------------------------------------------------------------
        // Activity 2 Default Rounds (3 Rounds of 4 bins)
        // ---------------------------------------------------------------------
        public static List<WordFamilyRound> GetDefaultActivity2Rounds()
        {
            // Round A: ai, ay, ea, ee
            var rA = new WordFamilyRound("Round A", new VowelTeamType[] { VowelTeamType.AI, VowelTeamType.AY, VowelTeamType.EA, VowelTeamType.EE });
            rA.items.Add(new WordFamilyItem("pain", VowelTeamType.AI));
            rA.items.Add(new WordFamilyItem("rain", VowelTeamType.AI));
            rA.items.Add(new WordFamilyItem("train", VowelTeamType.AI));
            rA.items.Add(new WordFamilyItem("day", VowelTeamType.AY));
            rA.items.Add(new WordFamilyItem("may", VowelTeamType.AY));
            rA.items.Add(new WordFamilyItem("say", VowelTeamType.AY));
            rA.items.Add(new WordFamilyItem("eat", VowelTeamType.EA));
            rA.items.Add(new WordFamilyItem("read", VowelTeamType.EA));
            rA.items.Add(new WordFamilyItem("team", VowelTeamType.EA));
            rA.items.Add(new WordFamilyItem("see", VowelTeamType.EE));
            rA.items.Add(new WordFamilyItem("feet", VowelTeamType.EE));
            rA.items.Add(new WordFamilyItem("green", VowelTeamType.EE));

            // Round B: oa, ow, ie, igh
            var rB = new WordFamilyRound("Round B", new VowelTeamType[] { VowelTeamType.OA, VowelTeamType.OW, VowelTeamType.IE, VowelTeamType.IGH });
            rB.items.Add(new WordFamilyItem("boat", VowelTeamType.OA));
            rB.items.Add(new WordFamilyItem("road", VowelTeamType.OA));
            rB.items.Add(new WordFamilyItem("soap", VowelTeamType.OA));
            rB.items.Add(new WordFamilyItem("row", VowelTeamType.OW));
            rB.items.Add(new WordFamilyItem("blow", VowelTeamType.OW));
            rB.items.Add(new WordFamilyItem("grow", VowelTeamType.OW));
            rB.items.Add(new WordFamilyItem("piece", VowelTeamType.IE));
            rB.items.Add(new WordFamilyItem("thief", VowelTeamType.IE));
            rB.items.Add(new WordFamilyItem("belief", VowelTeamType.IE));
            rB.items.Add(new WordFamilyItem("light", VowelTeamType.IGH));
            rB.items.Add(new WordFamilyItem("night", VowelTeamType.IGH));
            rB.items.Add(new WordFamilyItem("right", VowelTeamType.IGH));

            // Round C: oo, ue, ui, ew
            var rC = new WordFamilyRound("Round C", new VowelTeamType[] { VowelTeamType.OO, VowelTeamType.UE, VowelTeamType.UI, VowelTeamType.EW });
            rC.items.Add(new WordFamilyItem("moon", VowelTeamType.OO));
            rC.items.Add(new WordFamilyItem("noon", VowelTeamType.OO));
            rC.items.Add(new WordFamilyItem("spoon", VowelTeamType.OO));
            rC.items.Add(new WordFamilyItem("glue", VowelTeamType.UE));
            rC.items.Add(new WordFamilyItem("due", VowelTeamType.UE));
            rC.items.Add(new WordFamilyItem("true", VowelTeamType.UE));
            rC.items.Add(new WordFamilyItem("fruit", VowelTeamType.UI));
            rC.items.Add(new WordFamilyItem("suit", VowelTeamType.UI));
            rC.items.Add(new WordFamilyItem("juice", VowelTeamType.UI));
            rC.items.Add(new WordFamilyItem("few", VowelTeamType.EW));
            rC.items.Add(new WordFamilyItem("new", VowelTeamType.EW));
            rC.items.Add(new WordFamilyItem("drew", VowelTeamType.EW));

            return new List<WordFamilyRound> { rA, rB, rC };
        }

        // ---------------------------------------------------------------------
        // Activity 3 Default Items (18 Items: 6 per sound bin)
        // ---------------------------------------------------------------------
        public static List<SameSoundItem> GetDefaultActivity3Items()
        {
            return new List<SameSoundItem>
            {
                // /eɪ/ "ay"
                new SameSoundItem("rain", "ai", SoundCategoryType.LongA_AY),
                new SameSoundItem("train", "ai", SoundCategoryType.LongA_AY),
                new SameSoundItem("day", "ay", SoundCategoryType.LongA_AY),
                new SameSoundItem("say", "ay", SoundCategoryType.LongA_AY),
                new SameSoundItem("eight", "ei", SoundCategoryType.LongA_AY),
                new SameSoundItem("weight", "ei", SoundCategoryType.LongA_AY),

                // /iː/ "ee"
                new SameSoundItem("see", "ee", SoundCategoryType.LongE_EE),
                new SameSoundItem("green", "ee", SoundCategoryType.LongE_EE),
                new SameSoundItem("eat", "ea", SoundCategoryType.LongE_EE),
                new SameSoundItem("team", "ea", SoundCategoryType.LongE_EE),
                new SameSoundItem("key", "ey", SoundCategoryType.LongE_EE),
                new SameSoundItem("money", "ey", SoundCategoryType.LongE_EE),

                // /əʊ/ "oh"
                new SameSoundItem("boat", "oa", SoundCategoryType.LongO_OH),
                new SameSoundItem("road", "oa", SoundCategoryType.LongO_OH),
                new SameSoundItem("row", "ow", SoundCategoryType.LongO_OH),
                new SameSoundItem("grow", "ow", SoundCategoryType.LongO_OH),
                new SameSoundItem("toe", "oe", SoundCategoryType.LongO_OH),
                new SameSoundItem("goes", "oe", SoundCategoryType.LongO_OH)
            };
        }

        // ---------------------------------------------------------------------
        // Activity 4 Default Items (16 Items: 8 Glide / 8 Hold)
        // ---------------------------------------------------------------------
        public static List<GlideOrHoldItem> GetDefaultActivity4Items()
        {
            return new List<GlideOrHoldItem>
            {
                new GlideOrHoldItem("coin", "/ɔɪ/", SoundMovementType.Glide),
                new GlideOrHoldItem("see", "/iː/", SoundMovementType.Hold),
                new GlideOrHoldItem("brown", "/aʊ/", SoundMovementType.Glide),
                new GlideOrHoldItem("moon", "/uː/", SoundMovementType.Hold, true),
                new GlideOrHoldItem("pie", "/aɪ/", SoundMovementType.Glide),
                new GlideOrHoldItem("look", "/ʊ/", SoundMovementType.Hold, true),
                new GlideOrHoldItem("boat", "/əʊ/", SoundMovementType.Glide),
                new GlideOrHoldItem("green", "/iː/", SoundMovementType.Hold),
                new GlideOrHoldItem("toy", "/ɔɪ/", SoundMovementType.Glide),
                new GlideOrHoldItem("fruit", "/uː/", SoundMovementType.Hold, true),
                new GlideOrHoldItem("house", "/aʊ/", SoundMovementType.Glide),
                new GlideOrHoldItem("saw", "/ɔː/", SoundMovementType.Hold, true),
                new GlideOrHoldItem("light", "/aɪ/", SoundMovementType.Glide),
                new GlideOrHoldItem("drew", "/uː/", SoundMovementType.Hold, true),
                new GlideOrHoldItem("day", "/eɪ/", SoundMovementType.Glide),
                new GlideOrHoldItem("team", "/iː/", SoundMovementType.Hold)
            };
        }

        // ---------------------------------------------------------------------
        // Activity 5 Default Items (2 Rounds of 10 items)
        // ---------------------------------------------------------------------
        public static List<GlideSwipeItem> GetDefaultActivity5ItemsRoundA()
        {
            // oi (middle) vs oy (end)
            return new List<GlideSwipeItem>
            {
                new GlideSwipeItem("coin", "oi", DiphthongPositionRule.Middle),
                new GlideSwipeItem("boy", "oy", DiphthongPositionRule.End),
                new GlideSwipeItem("boil", "oi", DiphthongPositionRule.Middle),
                new GlideSwipeItem("toy", "oy", DiphthongPositionRule.End),
                new GlideSwipeItem("soil", "oi", DiphthongPositionRule.Middle),
                new GlideSwipeItem("joy", "oy", DiphthongPositionRule.End),
                new GlideSwipeItem("join", "oi", DiphthongPositionRule.Middle),
                new GlideSwipeItem("coy", "oy", DiphthongPositionRule.End),
                new GlideSwipeItem("oil", "oi", DiphthongPositionRule.Middle),
                new GlideSwipeItem("annoy", "oy", DiphthongPositionRule.End)
            };
        }

        public static List<GlideSwipeItem> GetDefaultActivity5ItemsRoundB()
        {
            // ou (middle) vs ow (end/before l,n)
            return new List<GlideSwipeItem>
            {
                new GlideSwipeItem("couch", "ou", DiphthongPositionRule.Middle),
                new GlideSwipeItem("owl", "ow", DiphthongPositionRule.End),
                new GlideSwipeItem("cloud", "ou", DiphthongPositionRule.Middle),
                new GlideSwipeItem("frown", "ow", DiphthongPositionRule.End),
                new GlideSwipeItem("found", "ou", DiphthongPositionRule.Middle),
                new GlideSwipeItem("crown", "ow", DiphthongPositionRule.End),
                new GlideSwipeItem("house", "ou", DiphthongPositionRule.Middle),
                new GlideSwipeItem("clown", "ow", DiphthongPositionRule.End),
                new GlideSwipeItem("mouse", "ou", DiphthongPositionRule.Middle),
                new GlideSwipeItem("brown", "ow", DiphthongPositionRule.End)
            };
        }

        // ---------------------------------------------------------------------
        // Unit 7 Challenge Default Questions (12 Questions)
        // ---------------------------------------------------------------------
        public static List<Unit7ChallengeQuestion> GetDefaultChallengeQuestions()
        {
            return new List<Unit7ChallengeQuestion>
            {
                new Unit7ChallengeQuestion(
                    "What is a vowel team?",
                    new string[] { "Two vowel letters making one sound", "A vowel followed by a consonant", "Any word with two syllables" },
                    0,
                    "A vowel team is two vowel letters working together to make one vowel sound!"
                ),
                new Unit7ChallengeQuestion(
                    "What makes a diphthong different from a steady vowel sound?",
                    new string[] { "It is always spelled with three letters", "The sound glides from one vowel to another", "It is only found at the end of a word" },
                    1,
                    "A diphthong glides from one vowel sound toward another inside a single syllable!"
                ),
                new Unit7ChallengeQuestion(
                    "Can a word be BOTH a vowel team and a diphthong?",
                    new string[] { "Yes (like 'boil' and 'cow')", "No, they are opposites", "Only if it has two syllables" },
                    0,
                    "Yes! Vowel team is the spelling (two letters), and diphthong is the sound (gliding)!"
                ),
                new Unit7ChallengeQuestion(
                    "Which word contains a STEADY vowel sound (Hold)?",
                    new string[] { "coin", "moon", "house" },
                    1,
                    "'moon' has the steady /uː/ sound; it does not glide!"
                ),
                new Unit7ChallengeQuestion(
                    "Which spelling is used for the /ɔɪ/ sound in the MIDDLE of a word?",
                    new string[] { "oy", "oi", "ay" },
                    1,
                    "'oi' is used in the middle of a word (like coin, boil, soil)!"
                ),
                new Unit7ChallengeQuestion(
                    "Which spelling is used for the /ɔɪ/ sound at the END of a word?",
                    new string[] { "oi", "oy", "ow" },
                    1,
                    "'oy' sits at the end of a word (like boy, toy, joy)!"
                ),
                new Unit7ChallengeQuestion(
                    "Which two words share the EXACT SAME vowel sound despite different spellings?",
                    new string[] { "rain and day", "see and coin", "boat and moon" },
                    0,
                    "'rain' (ai) and 'day' (ay) both make the /eɪ/ sound!"
                ),
                new Unit7ChallengeQuestion(
                    "Which of these words is a split vowel team?",
                    new string[] { "kite", "rain", "boat" },
                    0,
                    "'kite' has the split team i_e working together across the 't'!"
                ),
                new Unit7ChallengeQuestion(
                    "In 'they' and 'key', the spelling 'ey' makes:",
                    new string[] { "The exact same sound", "Two different sounds (/eɪ/ and /iː/)", "A silent letter" },
                    1,
                    "'they' makes /eɪ/ while 'key' makes /iː/ — one spelling can make different sounds!"
                ),
                new Unit7ChallengeQuestion(
                    "Where does 'ou' usually appear in a word?",
                    new string[] { "At the very end", "In the middle", "Only at the beginning" },
                    1,
                    "'ou' is usually found in the middle of a word (cloud, house, found)!"
                ),
                new Unit7ChallengeQuestion(
                    "Which word has a GLIDING vowel sound (Diphthong)?",
                    new string[] { "see", "look", "toy" },
                    2,
                    "'toy' has the /ɔɪ/ diphthong that glides from 'aw' to 'ee'!"
                ),
                new Unit7ChallengeQuestion(
                    "How many vowel sounds are mastered on your 7 Types Map now?",
                    new string[] { "5 of 7 Syllable Types", "3 of 7 Syllable Types", "All 7 Types" },
                    0,
                    "You have now mastered 5 of the 7 Syllable Types on the Map!"
                )
            };
        }
    }
}
