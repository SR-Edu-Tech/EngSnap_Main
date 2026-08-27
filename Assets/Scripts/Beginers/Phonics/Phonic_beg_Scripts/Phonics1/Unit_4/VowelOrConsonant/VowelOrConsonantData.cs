using UnityEngine;

namespace EngSnap.Unit4
{
    [CreateAssetMenu(fileName = "NewVowelOrConsonantData", menuName = "EngSnap/Unit 4/Vowel or Consonant Data")]
    public class VowelOrConsonantData : ScriptableObject
    {
        [Header("Letter Tile Data")]
        [Tooltip("The letter string (e.g. A, B, C, E, F, I, O, U, etc.).")]
        public string letter = "E";

        [Tooltip("True if this letter is a vowel (A, E, I, O, U); false if it is a consonant.")]
        public bool isVowel = true;

        [Header("Visual Assets")]
        [Tooltip("Optional tile background or sprite icon.")]
        public Sprite letterSprite;

        [Header("Audio Clips")]
        [Tooltip("Letter phoneme audio clip played when dropped or tapped.")]
        public AudioClip letterSoundClip;
    }
}
