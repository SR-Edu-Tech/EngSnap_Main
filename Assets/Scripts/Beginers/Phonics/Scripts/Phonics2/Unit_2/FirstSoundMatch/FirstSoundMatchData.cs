using UnityEngine;

namespace EngSnap.Phonics2.Unit2
{
    [System.Serializable]
    public class FirstSoundMatchItem
    {
        public string wordName = "mango";
        public char correctFirstLetter = 'M';
        public string stretchedWordText = "mmmmango";
        public Sprite pictureSprite;
        public AudioClip stretchedAudioClip; // Clip with stretched first sound ("mmmmango")
        public AudioClip wordNormalClip;     // Clip with normal pronunciation ("mango")
        public AudioClip successVoiceClip;   // e.g. "Yes! Mango starts with /m/. M!"
        public char[] distractorLetters = new char[] { 'N', 'L', 'S' };
    }

    [CreateAssetMenu(fileName = "NewFirstSoundMatchData", menuName = "EngSnap/Phonics2/Unit 2/First Sound Match Data")]
    public class FirstSoundMatchData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip introVoiceClip; // "Listen to the word. Which letter does it START with?"
        public AudioClip closingVoiceClip; // "You are matching sounds to letters. That is real reading!"

        [Header("12 Worksheet Match Items (pp. 14-15)")]
        public FirstSoundMatchItem[] matchItems;

        [Header("Per-Letter Wrong Tap Voice Clips (26 Clips)")]
        // Audio clips where letter speaks for itself: "I say /n/. Listen again..."
        public AudioClip[] letterWrongTapVoiceClips; // Array of 26 clips indexed 0..25 (A..Z)
    }
}
