
    using UnityEngine;

    [CreateAssetMenu(fileName = "U2_VowelPairData", menuName = "Phonics/Unit 2/Vowel Pair Data")]
    public class U2_VowelPairData_Phonics_Junior : ScriptableObject
    {
        public string vowelLetter; // "A", "E", "I", "O", "U"

        [Header("Short Vowel (Breve ˘)")]
        public string shortSymbol = "˘";
        public string shortWord = "crab";
        public Sprite shortImage;
        public AudioClip shortSoundAudio; // Explanation Audio: "Short a... /a/... crab."
        public AudioClip shortQuizAudio;  // Quiz Audio: "/a/... crab" or "crab" (no "Short a" prefix)

        [Header("Long Vowel (Macron ¯)")]
        public string longSymbol = "¯";
        public string longWord = "gate";
       public Sprite longImage;
        public AudioClip longSoundAudio; // Explanation Audio: "Long a... /ay/... gate."
       public AudioClip longQuizAudio;  // Quiz Audio: "/ay/... gate" or "gate" (no "Long a" prefix)
    }