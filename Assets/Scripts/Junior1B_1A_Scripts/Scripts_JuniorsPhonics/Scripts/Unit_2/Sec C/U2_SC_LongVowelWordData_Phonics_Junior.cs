  using UnityEngine;

    public enum LongVowelType
    {
        Long_A,
        Long_E,
        Long_I,
        Long_O,
        Long_U
    }

    [CreateAssetMenu(fileName = "LongVowelWord_", menuName = "Phonics/Unit 2/Long Vowel Word Data")]
    public class U2_SC_LongVowelWordData_Phonics_Junior : ScriptableObject
    {
        [Header("Word Info")]
        public string wordText;
        public LongVowelType vowelType;

        [Header("Audio Clips")]
        [Tooltip("Audio sounding the word slowly (optional / segmented)")]
        public AudioClip slowAudio;

        [Tooltip("Audio sounding the word naturally (full word)")]
        public AudioClip naturalAudio;

        [Header("Visual (Optional)")]
        public Sprite wordImage;
    }