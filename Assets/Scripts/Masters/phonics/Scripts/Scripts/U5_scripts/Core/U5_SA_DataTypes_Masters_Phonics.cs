using System;
using System.Collections.Generic;
using UnityEngine;

namespace MastersPhonics
{
    public enum SyllableDoorType
    {
        Open,
        Closed
    }

    public enum VowelSoundCategory
    {
        ShortA, // /æ/ (cat, got)
        LongA,  // /eɪ/ (bacon, say)
        ShortE, // /e/ (bed, met)
        LongE,  // /iː/ (me, began)
        ShortI, // /ɪ/ (hip, sit)
        LongI,  // /aɪ/ (hi, silent)
        ShortO, // /ɒ/ (got, not)
        LongO,  // /əʊ/ (go, so)
        ShortU, // /ʌ/ (club, duck)
        LongU   // /juː/ (flu, human, tulip)
    }

    // -------------------------------------------------------------
    // Activity 1 (Door or Gate - GM-03s Swipe)
    // -------------------------------------------------------------
    [Serializable]
    public class DoorOrGateItem
    {
        public string word;
        public SyllableDoorType doorType;
        public bool isYVowel;
        public AudioClip wordAudio;
        public string vowelSoundText; // e.g. "Long o: 'oh'" or "Short o: 'ah'"
    }

    // -------------------------------------------------------------
    // Activity 2 (Long or Short? - GM-04 Listen & Choose)
    // -------------------------------------------------------------
    [Serializable]
    public class LongOrShortItem
    {
        public string word;
        public VowelSoundCategory correctVowel;
        public string vowelLabel;       // e.g. "long o (/oh/)"
        public string[] optionLabels;   // 3-4 choices displayed on buttons
        public int correctOptionIndex;
        public SyllableDoorType doorType;
        public string minimalPairWord;  // e.g. "got" paired with "go"
        public AudioClip wordAudio;
    }

    // -------------------------------------------------------------
    // Activity 3 (The First Door - GM-03 2-Bin Drag)
    // -------------------------------------------------------------
    [Serializable]
    public class FirstDoorItem
    {
        public string word;
        public string syllableDivision; // e.g. "ba-con", "bas-ket"
        public string firstSyllable;     // e.g. "ba", "bas"
        public SyllableDoorType firstDoorType;
        public string vowelRuleNote;    // e.g. "Long a (door is open)" vs "Short a (door is closed by s)"
        public AudioClip wordAudio;
    }

    // -------------------------------------------------------------
    // Activity 4 (Closed-Word Hunt - GM-07 Word Grid)
    // -------------------------------------------------------------
    [Serializable]
    public class ClosedWordSearchItem
    {
        public string word;
        public bool isIrregularSightWord; // true for "said" and "was"
        public string irregularNote;      // "That one's a rule-breaker — you just have to know it."
        public bool isFound;
        public AudioClip wordAudio;
    }

    // -------------------------------------------------------------
    // Activity 5 (Big Word Reader - GM-05 Syllable Tile Builder)
    // -------------------------------------------------------------
    [Serializable]
    public class BigWordChunkItem
    {
        public string word;
        public string[] chunks;          // e.g. ["di", "no", "saur"]
        public int roundIndex;           // 1 for 2-syllables, 2 for 3-syllables
        public SyllableDoorType firstDoorType;
        public AudioClip wholeWordAudio;
        public AudioClip[] chunkAudios;
    }

    // -------------------------------------------------------------
    // Unit Challenge (10 Mixed Questions)
    // -------------------------------------------------------------
    public enum UnitChallengeType
    {
        TextMCQ,
        AudioVowelSound,
        DoorSwipe,
        FirstDoorDrag,
        ChunkBuilder,
        TransferQuestion
    }

    [Serializable]
    public class Unit5ChallengeQuestion
    {
        public string prompt;
        public UnitChallengeType questionType;
        public string[] options;
        public int correctIndex;
        public string explanation;
        public string audioClipName;
        public AudioClip customAudio;
    }
}
