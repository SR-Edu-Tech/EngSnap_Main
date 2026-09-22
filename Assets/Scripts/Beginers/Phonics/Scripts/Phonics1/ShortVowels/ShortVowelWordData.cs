using UnityEngine;

namespace EngSnap.Common.ShortVowels
{
    [CreateAssetMenu(fileName = "NewShortVowelWordData", menuName = "EngSnap/Short Vowels/Word Data")]
    public class ShortVowelWordData : ScriptableObject
    {
        [Header("Word Config")]
        [Tooltip("CVC word string e.g. cat, pen, pin, dog, sun.")]
        public string word = "cat";

        [Tooltip("Target short vowel letter e.g. a, e, i, o, u.")]
        public string targetVowel = "a";

        [Header("Visual & Audio Assets")]
        [Tooltip("Illustration sprite for the CVC word.")]
        public Sprite wordSprite;

        [Tooltip("Word pronunciation audio clip (e.g. 'cat').")]
        public AudioClip wordAudioClip;

        [Tooltip("Phonetic short vowel audio clip (e.g. /æ/).")]
        public AudioClip vowelAudioClip;

        [Tooltip("Optional mascot reinforcement audio clip e.g. 'Hear it? c - a - t, cat! The short a sound!'")]
        public AudioClip reinforcementAudioClip;
    }
}
