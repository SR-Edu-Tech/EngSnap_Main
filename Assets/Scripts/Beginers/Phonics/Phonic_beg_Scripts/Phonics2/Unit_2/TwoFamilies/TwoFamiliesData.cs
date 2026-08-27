using UnityEngine;

namespace EngSnap.Phonics2.Unit2
{
    public enum MouthType
    {
        OpenVowel,
        LipsTogether, // b, m, p
        TongueTap,     // t, d, n
        TeethOnLip,    // f, v
        RoundedLips    // w
    }

    [System.Serializable]
    public class SortingLetterItem
    {
        public char letterChar = 'A';
        public string letterName = "ay";
        public string letterSound = "/a/";
        public bool isVowel = true;
        public MouthType mouthType = MouthType.OpenVowel;
        public Sprite letterSprite;
        public Sprite mouthCloseUpSprite;
        public AudioClip letterQuestionClip; // e.g. "Hello! I am E. Where do I live?"
        public AudioClip letterSoundClip;    // Clipped sound clip
        public AudioClip successVoiceClip;   // e.g. "Yes! E is a vowel. E lives in a gold house. eeeee!"
    }

    [CreateAssetMenu(fileName = "NewTwoFamiliesData", menuName = "EngSnap/Phonics2/Unit 2/Two Families Data")]
    public class TwoFamiliesData : ScriptableObject
    {
        [Header("Leo & Momo Voice Clips")]
        public AudioClip introVoiceClip; // "Welcome to Alphabet Town! Twenty-six letters live here — but in two different places."
        public AudioClip vowelDemoVoiceClip; // "Open your mouth and sing with me… aaaaa. Nothing gets in the way! That is a vowel."
        public AudioClip consonantDemoVoiceClip; // "Now say /b/. Feel it? Your lips shut! And /t/ — your tongue taps. Those are consonants."
        public AudioClip momoHintVoiceClip; // "Try singing it. If it sings with an open mouth, it is a vowel!"
        public AudioClip sungVowelsVoiceClip; // "aaa … eee … iii … ooo … uuu"
        public AudioClip bridgeToStop2VoiceClip; // "Five singers, twenty-one helpers. Now let's meet every letter properly!"

        [Header("Visual Assets")]
        public Sprite openVowelMouthSprite;
        public Sprite lipsTogetherMouthSprite;
        public Sprite tongueTapMouthSprite;
        public Sprite teethOnLipMouthSprite;
        public Sprite roundedLipsMouthSprite;

        [Header("Sorting Letter Items (10 Rounds)")]
        public SortingLetterItem[] sortingLetters;
    }
}
