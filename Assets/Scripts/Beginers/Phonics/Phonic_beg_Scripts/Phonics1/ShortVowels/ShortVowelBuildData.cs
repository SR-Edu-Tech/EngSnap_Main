using UnityEngine;

namespace EngSnap.Common.ShortVowels
{
    [System.Serializable]
    public class LetterSwapStep
    {
        [Tooltip("The replacement letter e.g. 'b' when changing 'c' -> 'b'.")]
        public string newLetter = "b";

        [Tooltip("Position of tile to swap: 0 = initial consonant, 1 = middle vowel, 2 = final consonant.")]
        public int swapPosition = 0;

        [Tooltip("Audio clip for the new swapped letter sound e.g. /b/ or /d/.")]
        public AudioClip newLetterSoundClip;

        [Tooltip("The resulting blended word e.g. 'bat'.")]
        public string resultingWord = "bat";

        [Tooltip("Illustration sprite for the resulting word.")]
        public Sprite wordSprite;

        [Tooltip("Blended audio clip for the resulting word.")]
        public AudioClip blendedWordClip;

        [Tooltip("Mascot voice clip e.g. 'Change c to b - now it says bat!'")]
        public AudioClip reinforcementVoiceClip;
    }

    [CreateAssetMenu(fileName = "NewShortVowelBuildData", menuName = "EngSnap/Short Vowels/Build Data")]
    public class ShortVowelBuildData : ScriptableObject
    {
        [Header("Initial Word Config")]
        public string initialWord = "cat";
        public string sound1 = "c";
        public string sound2 = "a";
        public string sound3 = "t";

        [Header("Audio & Visual Assets")]
        public AudioClip sound1Clip;
        public AudioClip sound2Clip;
        public AudioClip sound3Clip;
        public AudioClip initialBlendedClip;
        public Sprite initialWordSprite;

        [Header("Letter Swap Steps (Cat -> Bat -> Hat)")]
        public LetterSwapStep[] swapSteps;
    }
}
