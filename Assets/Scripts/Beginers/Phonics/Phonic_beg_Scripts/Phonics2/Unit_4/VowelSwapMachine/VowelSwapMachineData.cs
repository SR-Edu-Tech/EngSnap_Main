using UnityEngine;

namespace EngSnap.Phonics2.Unit4
{
    [System.Serializable]
    public class SwapWordOption
    {
        public char vowelChar = 'a';
        public string fullWord = "bad";
        public bool isRealWord = true;
        public Sprite pictureSprite;
        public AudioClip wordAudioClip;
    }

    [System.Serializable]
    public class SwapMachineSet
    {
        public string patternLabel = "b_d";
        public string prefixLetter = "b";
        public string suffixLetter = "d";
        public SwapWordOption[] vowelOptions = new SwapWordOption[5]; // a, e, i, o, u
    }

    [System.Serializable]
    public class RealWordQuizRound
    {
        public string promptText = "Which of these are REAL words? Tap them!";
        public string patternLabel = "b_d";
        public string[] wordOptions = new string[5];
        public bool[] isRealWord = new bool[5];
        public Sprite[] optionSprites = new Sprite[5];
        public AudioClip[] optionAudioClips = new AudioClip[5];
        public int targetRealCount = 3;
    }

    [CreateAssetMenu(fileName = "NewVowelSwapMachineData", menuName = "EngSnap/Phonics2/Unit 4/Vowel Swap Machine Data")]
    public class VowelSwapMachineData : ScriptableObject
    {
        [Header("Leo & Mascot Voice Scripts")]
        public AudioClip introVoiceClip; // "This is the Vowel Swap Machine..."
        public AudioClip nonsenseMonsterRaspberrySfx; // Playful raspberry sound
        public AudioClip nonsenseMonsterVoiceClip; // "Bppppt! That is not a word — it is just a silly sound!"
        public AudioClip momoRealWordHintClip; // "Hmm — do you know a 'bod'? Nope! But a BED, yes!"
        public AudioClip swapSuccessClosingClip; // "You changed the middle and changed the word..."

        [Header("Silly Monster Assets")]
        public Sprite[] sillyMonsterSprites = new Sprite[5];

        [Header("6 Swap Machine Sets (p. 22)")]
        public SwapMachineSet[] swapSets = new SwapMachineSet[6];

        [Header("4 Real Word Quiz Rounds")]
        public RealWordQuizRound[] realWordRounds = new RealWordQuizRound[4];

        private void Reset()
        {
            PopulatePage22DefaultData();
        }

        [ContextMenu("Populate Page 22 Default Data")]
        public void PopulatePage22DefaultData()
        {
            swapSets = new SwapMachineSet[6];

            swapSets[0] = CreateSet("b_d", "b", "d", new string[] { "bad", "bed", "bid", "bod", "bud" }, new bool[] { true, true, true, false, true });
            swapSets[1] = CreateSet("p_n", "p", "n", new string[] { "pan", "pen", "pin", "pon", "pun" }, new bool[] { true, true, true, false, true });
            swapSets[2] = CreateSet("s_t", "s", "t", new string[] { "sat", "set", "sit", "sot", "sut" }, new bool[] { true, true, true, false, false });
            swapSets[3] = CreateSet("m_t", "m", "t", new string[] { "mat", "met", "mit", "mot", "mut" }, new bool[] { true, true, true, false, false });
            swapSets[4] = CreateSet("c_p", "c", "p", new string[] { "cap", "cep", "cip", "cop", "cup" }, new bool[] { true, false, false, true, true });
            swapSets[5] = CreateSet("b_g", "b", "g", new string[] { "bag", "beg", "big", "bog", "bug" }, new bool[] { true, true, true, true, true });

            realWordRounds = new RealWordQuizRound[4];
            realWordRounds[0] = CreateQuizRound("b_d", new string[] { "bad", "bed", "bid", "bod", "bud" }, new bool[] { true, true, true, false, true }, 4);
            realWordRounds[1] = CreateQuizRound("p_n", new string[] { "pan", "pen", "pin", "pon", "pun" }, new bool[] { true, true, true, false, true }, 4);
            realWordRounds[2] = CreateQuizRound("s_t", new string[] { "sat", "set", "sit", "sot", "sut" }, new bool[] { true, true, true, false, false }, 3);
            realWordRounds[3] = CreateQuizRound("c_p", new string[] { "cap", "cep", "cip", "cop", "cup" }, new bool[] { true, false, false, true, true }, 3);
        }

        private SwapMachineSet CreateSet(string label, string prefix, string suffix, string[] words, bool[] isReal)
        {
            SwapMachineSet set = new SwapMachineSet();
            set.patternLabel = label;
            set.prefixLetter = prefix;
            set.suffixLetter = suffix;
            set.vowelOptions = new SwapWordOption[5];
            char[] vowels = new char[] { 'a', 'e', 'i', 'o', 'u' };

            for (int i = 0; i < 5; i++)
            {
                set.vowelOptions[i] = new SwapWordOption
                {
                    vowelChar = vowels[i],
                    fullWord = words[i],
                    isRealWord = isReal[i]
                };
            }
            return set;
        }

        private RealWordQuizRound CreateQuizRound(string label, string[] words, bool[] isReal, int targetReal)
        {
            RealWordQuizRound round = new RealWordQuizRound();
            round.promptText = "Which of these are REAL words? Tap them!";
            round.patternLabel = label;
            round.wordOptions = words;
            round.isRealWord = isReal;
            round.targetRealCount = targetReal;
            round.optionSprites = new Sprite[5];
            round.optionAudioClips = new AudioClip[5];
            return round;
        }
    }
}
