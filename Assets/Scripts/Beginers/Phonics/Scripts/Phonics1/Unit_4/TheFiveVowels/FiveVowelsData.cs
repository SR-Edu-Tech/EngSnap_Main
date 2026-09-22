using UnityEngine;

namespace EngSnap.Unit4
{
    [CreateAssetMenu(fileName = "NewFiveVowelsData", menuName = "EngSnap/Unit 4/Five Vowels Data")]
    public class FiveVowelsData : ScriptableObject
    {
        [Header("Vowel Info")]
        [Tooltip("The vowel letter string (A, E, I, O, U).")]
        public string vowelLetter = "A";

        [Tooltip("Phoneme pronunciation text e.g. 'ae', 'eh', 'ah-eei', 'ah', 'yoo'.")]
        public string phonemeText = "ae";

        [Tooltip("Example word for the vowel (e.g. apple, elephant, igloo, octopus, umbrella).")]
        public string exampleWord = "apple";

        [Header("Visual Assets")]
        [Tooltip("Picture image representing the example word.")]
        public Sprite wordSprite;

        [Tooltip("Color tint for the balloon image.")]
        public Color balloonColor = Color.red;

        [Header("Audio Clips")]
        [Tooltip("Pure phoneme sound clip (reused from Unit 3).")]
        public AudioClip purePhonemeClip;

        [Tooltip("Example word spoken audio clip.")]
        public AudioClip wordAudioClip;

        [Tooltip("Combined phoneme sound + picture word clip (e.g. 'ae - apple').")]
        public AudioClip soundAndWordClip;
    }
}
