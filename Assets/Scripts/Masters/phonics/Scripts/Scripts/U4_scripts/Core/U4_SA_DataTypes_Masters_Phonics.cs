using System;
using UnityEngine;

namespace MastersPhonics
{
    public enum SyllableTypeCategory
    {
        Closed,         // v c (cat, napkin) -> Unit 5
        Open,           // v (tiger, paper, baby) -> Unit 5
        MagicE,         // v c e (bake, bone, pine) -> Unit 6
        VowelTeam,      // v v (team, float, seed) -> Unit 7
        RControlled,    // v r (car, bird, fort) -> Unit 8
        Diphthong,      // (v v) (boil, cloud) -> Unit 7
        ConsonantLe     // c + le (bubble, staple, circle) -> Unit 9
    }

    [Serializable]
    public class SyllableTypeDoorItem
    {
        public string word;
        public SyllableTypeCategory category;
        public string patternNotation;
        public int roundIndex; // 1 or 2
        public AudioClip wordAudio;
    }

    [Serializable]
    public class StrongBeatItem
    {
        public string word;
        public string[] syllables;      // e.g. ["RAB", "bit"] or ["a", "GO"]
        public int stressedIndex;       // 0 for 1st syllable, 1 for 2nd syllable
        public string weakVowelNote;    // e.g. "i -> schwa-ish", "a -> schwa"
        public AudioClip naturalAudio;
        public AudioClip normAudio;     // light separation
        public AudioClip exagAudio;     // exaggerated loud stressed beat
    }

    [Serializable]
    public class SchwaHuntItem
    {
        public string word;
        public int[] schwaLetterIndices; // 0-based indices in the word where schwa occurs
        public string respelling;        // e.g. "camul", "uh-go", "bacun"
        public string clueRule;          // "Vowel before final L" or "Lazy unaccented vowel"
        public int roundIndex;           // 1 = Final L, 2 = a/e/i, 3 = o/u/y
        public AudioClip wordAudio;
        public AudioClip schwaAudio;     // U04_SCH_{word}
    }

    [Serializable]
    public class ChallengeQuestionItem
    {
        public string prompt;
        public string[] options;
        public int correctIndex;
        public string explanation;
        public AudioClip audioClip;
    }
}
