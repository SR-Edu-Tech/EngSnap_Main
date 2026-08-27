using UnityEngine;

namespace EngSnap.Phonics2.Unit2
{
    [System.Serializable]
    public class WriteAndSayItem
    {
        public string wordName = "ant";
        public char missingLetter = 'a';
        public string displayGapText = "_nt";
        public Sprite pictureSprite;
        public Sprite tracingOutlineSprite;
        public Sprite filledLetterSprite;
        public Vector2[] checkpointPositions = new Vector2[5];
        public AudioClip wordAudioClip;      // "a - n - t. ANT!"
        public AudioClip letterSoundClip;    // /a/
        public AudioClip completionVoiceClip;// "You wrote it and you said it!"
    }

    [System.Serializable]
    public class StarRoundUnit2Challenge
    {
        [TextArea(1, 3)]
        public string questionPrompt = "Is E a vowel or a consonant?";
        public AudioClip promptClip;
        public Sprite promptSprite;
        public string[] choices = new string[] { "Vowel", "Consonant" };
        public int correctChoiceIndex = 0;
        public bool isTracingChallenge = false;
        public bool isVowelStripChallenge = false;
    }

    [CreateAssetMenu(fileName = "NewWriteAndSayData", menuName = "EngSnap/Phonics2/Unit 2/Write And Say Data")]
    public class WriteAndSayData : ScriptableObject
    {
        [Header("Leo & Momo Voice Scripts")]
        public AudioClip introVoiceClip; // "Look at the picture. Ant! What letter is missing?"
        public AudioClip tracingInstructionClip; // "Trace it with your finger — and say the sound as you go."
        public AudioClip momoGhostFingerClip; // "Follow my finger — start here, and go this way."
        public AudioClip badgeVoiceClip; // "You know every letter and every sound. You are a LETTER MASTER!"
        public AudioClip unit3UnlockVoiceClip; // "Unit Three is open! Next time — the five singers and their two voices."

        [Header("Tara Star Round Voice Clips")]
        public AudioClip taraStarRoundOpenerClip; // "My turn! Six quick challenges. Ready? Roar!"

        [Header("Visual Assets")]
        public Sprite letterMasterBadgeSprite;

        [Header("8 Write & Say Tracing Items (p. 15)")]
        public WriteAndSayItem[] tracingItems;

        [Header("Tara 6 Star Round Challenges")]
        public StarRoundUnit2Challenge[] starChallenges;
    }
}
