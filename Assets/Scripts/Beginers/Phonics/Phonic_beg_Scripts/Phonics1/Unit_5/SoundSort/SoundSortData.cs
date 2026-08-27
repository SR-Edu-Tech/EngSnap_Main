using UnityEngine;

namespace EngSnap.Unit5
{
    [CreateAssetMenu(fileName = "NewSoundSortData", menuName = "EngSnap/Unit 5/Sound Sort Data")]
    public class SoundSortData : ScriptableObject
    {
        [Header("Word Sort Config")]
        [Tooltip("Word string e.g. fan, pet, pin, dog, sun, ant, rat, egg, bed, ink, sit, ox, pot, jug.")]
        public string word = "fan";

        [Tooltip("Target middle vowel letter e.g. a, e, i, o, u.")]
        public string targetVowel = "a";

        [Header("Visual & Audio Assets")]
        [Tooltip("Picture-word card sprite illustration.")]
        public Sprite wordSprite;

        [Tooltip("Word pronunciation audio clip.")]
        public AudioClip wordAudioClip;
    }
}
