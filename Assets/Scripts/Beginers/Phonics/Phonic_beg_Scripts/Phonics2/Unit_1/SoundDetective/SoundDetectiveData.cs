using UnityEngine;

namespace EngSnap.Phonics2.Unit1
{
    [System.Serializable]
    public class PhonemeWordItem
    {
        public string wordStr = "cat";
        public Sprite wordSprite;
        public AudioClip wholeWordClip;
        public AudioClip splitWordClip; // e.g. "c ... a ... t" with clipped consonants
        public int phonemeCount = 3;
        public string[] phonemeSounds; // Individual letter sounds e.g. ["c", "a", "t"]
        public AudioClip[] phonemeAudioClips; // Individual phoneme audio clips e.g. /k/, /a/, /t/
    }

    [System.Serializable]
    public class OddOneOutItem
    {
        public string promptText = "sun … sock … ball. Which one does NOT start like the others?";
        public AudioClip promptClip;
        public Sprite[] choiceSprites;
        public string[] choiceWords;
        public int oddOneOutIndex = 2; // 0-based index of the word that doesn't start like the others
    }

    [System.Serializable]
    public class OralBlendItem
    {
        public string splitText = "b … a … t";
        public string wordText = "bat";
        public AudioClip splitClip;
        public AudioClip wholeWordClip;
        public Sprite wordSprite;
        public Sprite[] choiceSprites;
        public int correctChoiceIndex = 0;
    }

    [CreateAssetMenu(fileName = "NewSoundDetectiveData", menuName = "EngSnap/Phonics2/Unit 1/Sound Detective Data")]
    public class SoundDetectiveData : ScriptableObject
    {
        [Header("Leo Voice Lines & Intro Clips")]
        public AudioClip introVoiceClip; // "Words are made of tiny sounds. Let's catch them!"
        public AudioClip demoVoiceClip; // "One, two, three! Three sounds! c - a - t. Cat!"
        public AudioClip tapInstructionClip; // "Now you try. Tap once for every sound you hear."
        public AudioClip retryVoiceClip; // "Let's listen again — I will say it slowly."
        public AudioClip squashSuccessClip; // "BAT! You squashed the sounds into a word!"
        public AudioClip completionVoiceClip; // "You can hear the sounds inside words. You are a Sound Detective!"

        [Header("Tap Per Sound Word Items (cat, sun, pig, bus, up, egg)")]
        public PhonemeWordItem[] phonemeWords;

        [Header("Odd One Out Rounds (3 Rounds)")]
        public OddOneOutItem[] oddOneOutRounds;

        [Header("Squash The Word / Oral Blending Rounds (2 Rounds)")]
        public OralBlendItem[] oralBlendRounds;
    }
}
