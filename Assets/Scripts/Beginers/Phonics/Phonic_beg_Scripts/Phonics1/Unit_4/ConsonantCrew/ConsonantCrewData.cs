using UnityEngine;

namespace EngSnap.Unit4
{
    [CreateAssetMenu(fileName = "NewConsonantCrewData", menuName = "EngSnap/Unit 4/Consonant Crew Data")]
    public class ConsonantCrewData : ScriptableObject
    {
        [Header("Consonant Info")]
        [Tooltip("The consonant letter string (B, C, D, F, G, H, J, K, L, M, N, P, Q, R, S, T, V, W, X, Y, Z).")]
        public string letter = "B";

        [Tooltip("Phoneme pronunciation text e.g. 'buh', 'kuh', 'duh', 'fuh', 'guh'.")]
        public string phonemeText = "buh";

        [Tooltip("Example word for the consonant (e.g. bat, cat, dog, fish, girl, etc.).")]
        public string exampleWord = "bat";

        [Header("Visual Assets")]
        [Tooltip("Picture image representing the example word.")]
        public Sprite wordSprite;

        [Header("Audio Clips")]
        [Tooltip("Pure phoneme sound clip (reused from Unit 3).")]
        public AudioClip purePhonemeClip;

        [Tooltip("Example word spoken audio clip.")]
        public AudioClip wordAudioClip;

        [Tooltip("Combined sound clip: pure sound then picture word (e.g. 'buh - bat').")]
        public AudioClip soundAndWordClip;
    }
}
