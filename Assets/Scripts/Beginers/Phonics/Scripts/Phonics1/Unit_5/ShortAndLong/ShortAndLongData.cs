using UnityEngine;

namespace EngSnap.Unit5
{
    [CreateAssetMenu(fileName = "NewShortAndLongData", menuName = "EngSnap/Unit 5/Short And Long Data")]
    public class ShortAndLongData : ScriptableObject
    {
        [Header("Vowel Header")]
        [Tooltip("Vowel letter string e.g. A, E, I, O, U.")]
        public string vowelLetter = "A";

        [Header("Short Vowel Details")]
        [Tooltip("Short vowel audio clip e.g. a_short.mp3 (æ - apple).")]
        public AudioClip shortSoundClip;
        [Tooltip("Picture sprite for short vowel word (e.g. apple, elephant, igloo, octopus, umbrella).")]
        public Sprite shortPictureSprite;
        [Tooltip("Text label for short word (e.g. apple, elephant, igloo).")]
        public string shortWordLabel = "apple";

        [Header("Long Vowel Details")]
        [Tooltip("Long vowel audio clip e.g. a_long.mp3 (ay - grapes).")]
        public AudioClip longSoundClip;
        [Tooltip("Picture sprite for long vowel word (e.g. grapes, feet, kite, boat, glue).")]
        public Sprite longPictureSprite;
        [Tooltip("Text label for long word (e.g. grapes, feet, kite).")]
        public string longWordLabel = "grapes";

        [Header("Reinforcement Dialogue")]
        [Tooltip("Reinforcement voice clip e.g. 'Short a is apple, long a is grapes. Clever!'")]
        public AudioClip reinforcementClip;
    }
}
