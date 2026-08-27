using UnityEngine;

namespace EngSnap.Phonics2.Unit3
{
    [System.Serializable]
    public class VowelVoiceToyItem
    {
        public string vowelLetter = "A";
        public Sprite vowelShortSprite;
        public Sprite vowelLongSprite;
        public AudioClip nameVoiceClip;  // "My name is ay... ay! Acorn!"
        public AudioClip soundVoiceClip; // "My sound is /a/... /a/! Cat!"
        public Sprite shortPictureSprite; // Cat
        public Sprite longPictureSprite;  // Acorn
    }

    [System.Serializable]
    public class WhichVoiceItem
    {
        public string wordName = "cat";
        public char vowelChar = 'a';
        public bool isLongName = false; // false = short sound (breve), true = long name (macron)
        public Sprite pictureSprite;
        public AudioClip wordAudioClip;
        public AudioClip explanationClip;
    }

    [CreateAssetMenu(fileName = "NewTwoVoicesData", menuName = "EngSnap/Phonics2/Unit 3/Two Voices Data")]
    public class TwoVoicesData : ScriptableObject
    {
        [Header("Leo & Momo Voice Clips")]
        public AudioClip introVoiceClip; // "Every singer here has TWO voices. Watch this switch!"
        public AudioClip ruleExplanationClip; // "Long vowels say their NAME. Short vowels say their SOUND."
        public AudioClip freePlayInstructionClip; // "Flip the switches. Try them all!"
        public AudioClip quizInstructionClip; // "Listen: cake. Did the A say its NAME, or its SOUND?"
        public AudioClip momoMarkingHintClip; // "The curvy mark means short. The straight mark means long!"
        public AudioClip closingVoiceClip; // "You can tell short and long vowels apart! Fantastic!"

        [Header("5 Vowel Toy Switch Items")]
        public VowelVoiceToyItem[] vowelToyItems = new VowelVoiceToyItem[5];

        [Header("Which Voice Quiz Items (8 rounds)")]
        public WhichVoiceItem[] quizItems = new WhichVoiceItem[8];

        [Header("Mark It Items (4 rounds)")]
        public WhichVoiceItem[] markItems = new WhichVoiceItem[4];
    }
}
