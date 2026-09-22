using System;
using System.Collections.Generic;
using UnityEngine;

namespace MastersPhonics
{
    public enum SyllableCategory
    {
        Monosyllabic = 1, // 1 beat
        Disyllabic = 2,   // 2 beats
        Trisyllabic = 3,  // 3 beats
        Polysyllabic = 4  // 4+ beats
    }

    public enum SyllableSplitRule
    {
        DoubleConsonants,  // but-ter, rab-bit, pil-low, sum-mer (Rule 2)
        CompoundWords,     // mail-box, dog-house, lap-top, sun-set (Rule 3)
        PrefixSuffix,      // pre-view, kind-ness, un-fold, care-less (Rule 6)
        VCCVTwoConsonants, // el-bow, doc-tor, har-vest, car-bon, mar-gin, bar-ber (Rule 4)
        ConsonantLE        // tur-tle, cra-dle, can-dle, spar-kle, ta-ble, ap-ple (Rule 5)
    }

    [System.Serializable]
    public class SyllableWordItem
    {
        public string word;
        public int syllableCount;
        public string hyphenatedSplit; // e.g. "car-pen-ter"
        public string[] chunks;        // e.g. ["car", "pen", "ter"]
        public int[] splitIndices;     // 1-based index after which dividers go, e.g. [3, 6] for "car|pen|ter"
        public SyllableCategory category;
        public SyllableSplitRule primaryRule;
        public AudioClip wordAudio;
        public AudioClip separatedAudio;
    }

    [System.Serializable]
    public class PatternItem
    {
        public string word;
        public string pattern; // e.g. "CVC", "CCVC", "CVCC"
        public string typeLabel; // e.g. "Type 4"
        public AudioClip wordAudio;
    }
}
