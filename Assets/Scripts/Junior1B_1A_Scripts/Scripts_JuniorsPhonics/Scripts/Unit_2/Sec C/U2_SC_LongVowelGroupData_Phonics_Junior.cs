  using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "LongVowelGroup_", menuName = "Phonics/Unit 2/Long Vowel Group Data")]
    public class U2_SC_LongVowelGroupData_Phonics_Junior : ScriptableObject
    {
        [Header("Vowel Header")]
        [Tooltip("Macron-marked symbol e.g., ā, ē, ī, ō, ū")]
        public string vowelSymbol;

        public string vowelLetter; // "a", "e", "i", "o", "u"
        public LongVowelType vowelType;

        [Header("Word List")]
        public List<U2_SC_LongVowelWordData_Phonics_Junior> words = new
  List<U2_SC_LongVowelWordData_Phonics_Junior>();
    }