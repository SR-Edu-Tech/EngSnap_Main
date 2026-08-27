 using UnityEngine;

    [CreateAssetMenu(fileName = "New CVC Word", menuName = "Phonics/Unit 3/CVC Word Data")]
    public class CVCWordData : ScriptableObject
    {
        public string word; // e.g. "cat"
        public Sprite wordPicture; // Image for Activity 1 & 3

        [Header("Audio Clips")]
        public AudioClip letter1Sound; // /c/
        public AudioClip letter2Sound; // /a/
        public AudioClip letter3Sound; // /t/
        public AudioClip fullWordAudio; // "cat!"

        public char Letter1 => word.Length >= 1 ? char.ToLower(word[0]) : ' ';
        public char Letter2 => word.Length >= 2 ? char.ToLower(word[1]) : ' ';
        public char Letter3 => word.Length >= 3 ? char.ToLower(word[2]) : ' ';
    }
