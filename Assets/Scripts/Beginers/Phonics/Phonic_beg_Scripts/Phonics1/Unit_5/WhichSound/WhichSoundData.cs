using UnityEngine;

namespace EngSnap.Unit5
{
    [CreateAssetMenu(fileName = "NewWhichSoundData", menuName = "EngSnap/Unit 5/Which Sound Data")]
    public class WhichSoundData : ScriptableObject
    {
        [Header("Word Game Config")]
        [Tooltip("Word string e.g. cat, cake, pin, kite, sun, bed, feet, boat, glue.")]
        public string word = "cat";

        [Tooltip("True if this word has a long vowel sound; false if short vowel sound.")]
        public bool isLongVowel = false;

        [Tooltip("Vowel letter e.g. a, e, i, o, u.")]
        public string targetVowel = "a";

        [Header("Visual & Audio Assets")]
        [Tooltip("Picture sprite for word.")]
        public Sprite wordSprite;

        [Tooltip("Word pronunciation audio clip.")]
        public AudioClip wordAudioClip;

        [Tooltip("Praise clip e.g. 'Yes! cat is the short a sound!'")]
        public AudioClip praiseAudioClip;
    }
}
