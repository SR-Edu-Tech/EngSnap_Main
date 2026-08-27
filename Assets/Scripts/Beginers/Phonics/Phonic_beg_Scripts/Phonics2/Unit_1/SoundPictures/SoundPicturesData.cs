using UnityEngine;

namespace EngSnap.Phonics2.Unit1
{
    [System.Serializable]
    public class SoundCameraItem
    {
        public string soundStr = "/s/";
        public char letterChar = 's';
        public Sprite letterPhotoSprite;
        public AudioClip soundAudioClip;
        public AudioClip voiceDescriptionClip;
    }

    [System.Serializable]
    public class ScoopWordItem
    {
        public string wordStr = "scoop";
        public string splitPhonemesStr = "s … c … oo … p";
        public int phonemeCount = 4;
        public int letterCount = 5;
        public string joinedLettersPair = "oo";
        public Sprite wordSprite;
        public AudioClip splitClip;
        public AudioClip wholeWordClip;
    }

    [System.Serializable]
    public class StarRoundChallenge
    {
        [TextArea(1, 3)]
        public string questionPrompt = "How many sounds in \"hat\"?";
        public AudioClip promptClip;
        public Sprite promptSprite;
        public Sprite[] choiceSprites;
        public string[] choiceWords;
        public int correctChoiceIndex = 0;
    }

    [CreateAssetMenu(fileName = "NewSoundPicturesData", menuName = "EngSnap/Phonics2/Unit 1/Sound Pictures Data")]
    public class SoundPicturesData : ScriptableObject
    {
        [Header("Momo & Leo Sound Camera Voice Clips")]
        public AudioClip cameraIntroClip; // "I have a Sound Camera! Watch what happens when Leo makes a sound."
        public AudioClip cameraFlashClickClip; // "Click! Look — a picture of the sound /s/. We call it the letter s!"
        public AudioClip doubleGraphemeClip; // "Same sound — but two different pictures! c and k. Sneaky!"

        [Header("Double Grapheme Demonstration Sprite (c & k)")]
        public Sprite doubleGraphemePhotoSprite; // Combined photo sprite for c & k demonstration

        [Header("Scoop Game Voice Clips")]
        public AudioClip scoopInstructionClip; // "Add one scoop of ice cream for every sound you hear!"
        public AudioClip scoopHoldingHandsClip; // "One, two, three, four. Four sounds! But look — s, c, o, o, p. Five letters! The two o's are holding hands and sharing ONE sound."

        [Header("Tara Star Round Voice Clips")]
        public AudioClip taraStarRoundOpenerClip; // "My turn! Six quick challenges. Ready? Roar!"
        public AudioClip taraRetryClip; // "Almost! Let's do that one together."
        public AudioClip completionBadgeVoiceClip; // "You listened. You found the sounds. You are a SOUND DETECTIVE! Unit Two is open!"

        [Header("Sound Camera Photo Items (/s/, /m/, /t/, /a/)")]
        public SoundCameraItem[] cameraPhotos;

        [Header("Scoop Game Word Items (scoop, fish, moon)")]
        public ScoopWordItem[] scoopWords;

        [Header("Tara 6 Star Round Challenges")]
        public StarRoundChallenge[] starChallenges;
    }
}
