using UnityEngine;

namespace EngSnap.Phonics2.Unit3
{
    [System.Serializable]
    public class DiphthongSlideItem
    {
        public string wordName = "boy";
        public Sprite pictureSprite;
        public AudioClip slideAudioClip;
    }

    [System.Serializable]
    public class StarRoundUnit3Challenge
    {
        [TextArea(1, 3)]
        public string questionPrompt = "Tap the vowel in 'sun'.";
        public AudioClip promptClip;
        public Sprite promptSprite;
        public string[] choices = new string[] { "S", "U", "N" };
        public int correctChoiceIndex = 1;
        public bool isBreveMacronChallenge = false;
    }

    [CreateAssetMenu(fileName = "NewSlidingSoundsData", menuName = "EngSnap/Phonics2/Unit 3/Sliding Sounds Data")]
    public class SlidingSoundsData : ScriptableObject
    {
        [Header("Leo & Tara Voice Scripts")]
        public AudioClip introVoiceClip; // "Some sounds do not stay still. They SLIDE! Watch."
        public AudioClip slideDemoClip; // "ooo … iii … oy! Boy!"
        public AudioClip copyAlongClip; // "Say it with me — your mouth slides too!"
        public AudioClip slideMatchPromptClip; // "Which one did you hear? Tap the picture."
        public AudioClip taraStarRoundOpenerClip; // "My turn! Six quick challenges. Ready? Roar!"
        public AudioClip badgeVoiceClip; // "You know both voices of every vowel. You are a VOWEL VOICE!"
        public AudioClip unit4UnlockVoiceClip; // "Unit Four is open! Next time we go deep into the SHORT vowels — and you will start reading real words."

        [Header("Visual Assets")]
        public Sprite vowelVoiceBadgeSprite;

        [Header("8 Diphthong Sliding Items (p. 20)")]
        public DiphthongSlideItem[] diphthongItems = new DiphthongSlideItem[8];

        [Header("Tara 6 Star Round Challenges")]
        public StarRoundUnit3Challenge[] starChallenges = new StarRoundUnit3Challenge[6];
    }
}
