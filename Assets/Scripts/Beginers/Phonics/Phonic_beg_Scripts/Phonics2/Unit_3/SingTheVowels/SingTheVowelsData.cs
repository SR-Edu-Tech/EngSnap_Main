using UnityEngine;

namespace EngSnap.Phonics2.Unit3
{
    [CreateAssetMenu(fileName = "NewSingTheVowelsData", menuName = "EngSnap/Phonics2/Unit 3/Sing The Vowels Data")]
    public class SingTheVowelsData : ScriptableObject
    {
        [Header("Voice & Audio Clips")]
        public AudioClip introVoiceClip; // "Let's sing the Vowel Song!"
        public AudioClip verse1ShortVowelsClip; // "/a/, /e/, /i/, /o/, /u/ ... are short vowels that we use!"
        public AudioClip verse2EveryWordClip; // "A vowel is in every word that we read or write."
        public AudioClip verse3LongVowelsClip; // "/ai/, /ee/, /ie/, /oa/, /ue/ ... are long vowels that we use!"
        public AudioClip tapAlongInstructionClip; // "Tap each vowel when the ball lands on it!"
        public AudioClip karaokeCueClip; // "Now YOU sing it — I will be quiet!"
        public AudioClip applauseStingerSfx; // "Hooray! What a singer!"
        public AudioClip karaokeInstrumentalClip; // Instrumental track for karaoke

        [Header("Vowel Display Labels")]
        public string[] shortVowelLabels = new string[] { "a", "e", "i", "o", "u" };
        public string[] longVowelLabels = new string[] { "ay", "ee", "ie", "oa", "ue" };

        [Header("Individual Vowel Sound Clips")]
        public AudioClip[] shortVowelAudioClips = new AudioClip[5]; // /a/, /e/, /i/, /o/, /u/
        public AudioClip[] longVowelAudioClips = new AudioClip[5];  // /ay/, /ee/, /ie/, /oa/, /ue/
    }
}
