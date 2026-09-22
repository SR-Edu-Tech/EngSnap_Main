using UnityEngine;

namespace EngSnap.Phonics2.Unit3
{
    [System.Serializable]
    public class VowelSingerItem
    {
        public string vowelLetter = "A";
        public Sprite vowelSprite;
        public Sprite vowelSingingSprite;
        public AudioClip sungVowelClip;
        public Color vowelColor = Color.yellow;
    }

    [System.Serializable]
    public class WordSingerItem
    {
        public string fullWord = "cat";
        public char vowelChar = 'a';
        public string gapWord = "c _ t";
        public Sprite pictureSprite;
        public AudioClip wordAudioClip;
        public AudioClip vowelSoundClip;
    }

    [CreateAssetMenu(fileName = "NewFiveSingersData", menuName = "EngSnap/Phonics2/Unit 3/Five Singers Data")]
    public class FiveSingersData : ScriptableObject
    {
        [Header("Leo & Momo Voice Clips")]
        public AudioClip introVoiceClip; // "Welcome to Vowel Valley! Five letters live here, and all five of them can SING."
        public AudioClip feelTheBuzzClip; // "Put your hand on your throat and sing with me — aaaaa. Feel the buzz?"
        public AudioClip whisperConsonantClip; // "Now whisper /t/. No buzz! Vowels always turn the buzzer on."
        public AudioClip findTheSingerPromptClip; // "Find the singer in this word. Tap the vowel!"
        public AudioClip wordBrokeDemoClip; // "Uh-oh — I took the vowel away. c … t … Can you read it?"
        public AudioClip putSingerBackClip; // "Put the singer back! c - a - t. Cat! Every word needs a vowel."
        public AudioClip closingVoiceClip; // "Every word needs a singer! You found them all!"

        [Header("AEIOU Song Section")]
        [Tooltip("Audio clip for the A-E-I-O-U Vowel Song in the Stage Panel.")]
        public AudioClip vowelSongAudioClip; // "A E I O U Song Audio Clip"

        [Header("5 Vowel Singer Items")]
        public VowelSingerItem[] vowelSingers = new VowelSingerItem[5];

        [Header("Find The Singer Round Items (6 rounds)")]
        public WordSingerItem[] findSingerItems = new WordSingerItem[6];

        [Header("Broken Word Round Items (3 rounds)")]
        public WordSingerItem[] brokenWordItems = new WordSingerItem[3];
    }
}
