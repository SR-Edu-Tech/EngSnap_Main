 using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "ShortVowelGroup_", menuName = "Phonics/Short Vowel Group Data")]
    public class U2_SB_ShortVowelGroupData_Phonics_Junior : ScriptableObject
    {
        [Header("Vowel Header")]
        [Tooltip("Breve-marked symbol e.g., ă, ĕ, ĭ, ŏ, ŭ")]
        public string vowelSymbol;

        public string vowelLetter; // "a", "e", "i", "o", "u"
        public ShortVowelType vowelType;

        [Header("Word List")]
        public List<U2_SB_ShortVowelWordData_Phonics_Junior> words = new List<U2_SB_ShortVowelWordData_Phonics_Junior>();
    }