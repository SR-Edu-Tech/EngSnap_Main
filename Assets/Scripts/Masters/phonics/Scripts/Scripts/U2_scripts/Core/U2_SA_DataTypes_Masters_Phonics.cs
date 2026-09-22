using System;
using System.Collections.Generic;
using UnityEngine;

namespace MastersPhonics
{
    public enum InflectionalEndingType
    {
        Plural_S,        // -s (dogs)
        Plural_ES,       // -es (boxes)
        Possessive_S,    // 's (dad's)
        ThirdPerson_S,   // -s (sings)
        Progressive_ING, // -ing (skating, hopping)
        Past_ED,         // -ed (walked, slipped, cried)
        Participle_EN,   // -en (fallen)
        Comparative_ER,  // -er (taller, nicer)
        Superlative_EST  // -est (tallest, nicest)
    }

    public enum SpellingRuleType
    {
        AsItIs,    // No change (talk -> talked)
        DoubleIt,  // 1-1-1 Rule (hop -> hopping, bat -> batted)
        DropTheE,  // Silent e before vowel ending (like -> liked, nice -> nicer)
        YToI,      // Consonant + y -> i (cry -> cried, story -> stories)
        KeepY      // Consonant + y before -ing or vowel + y (cry -> crying, boy -> boys)
    }

    [System.Serializable]
    public class U2_EndingItem
    {
        public string baseWord;
        public string ending;
        public string fullWord;
        public SpellingRuleType rule;
        public InflectionalEndingType endingType;
        public AudioClip wordAudio;
        public AudioClip sentenceAudio;
    }
}
