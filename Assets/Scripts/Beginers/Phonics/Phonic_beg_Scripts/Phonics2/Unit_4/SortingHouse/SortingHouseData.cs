using UnityEngine;

namespace EngSnap.Phonics2.Unit4
{
    public enum StarChallengeType
    {
        VowelMiddleChoice, // Round 1: Which vowel is in middle of "sun"?
        WordMachineFill,   // Round 2: p_n -> make "pen"
        PictureTap,        // Round 3: Tap picture for "big"
        RealVsSillyChoice, // Round 4: Which of these is a real word — mig or mug?
        QuickSortDrag,     // Round 5: Sort three quick cards
        VowelSongRecap     // Round 6: Sing the five short vowels
    }

    [System.Serializable]
    public class SortingWordCardItem
    {
        public string wordName = "cat";
        public Sprite wordSprite;
        public int correctBoxIndex = 0; // 0:ă, 1:ĕ, 2:ĭ, 3:ŏ, 4:ŭ, 5:Not today!
        public bool isDistractor = false;
        public AudioClip wordNormalClip;
        public AudioClip wordMiddleStretchedClip;
    }

    [System.Serializable]
    public class StarRoundUnit4Challenge
    {
        public StarChallengeType challengeType = StarChallengeType.VowelMiddleChoice;
        [TextArea(1, 3)]
        public string questionPrompt = "Which vowel is in the middle of 'sun'?";
        public AudioClip promptClip;
        public Sprite promptSprite;
        public string targetWord = "sun";
        public string[] choices = new string[] { "a", "u", "i" };
        public Sprite[] choiceSprites;
        public int correctChoiceIndex = 1;

        [Header("Quick Sort Drag Cards (Round 5)")]
        public SortingWordCardItem[] quickDragCards;
    }

    [CreateAssetMenu(fileName = "SortingHouseData_Unit4", menuName = "EngSnap/Phonics2/Unit 4/Sorting House Data")]
    public class SortingHouseData : ScriptableObject
    {
        [Header("Leo, Tara & Voice Scripts")]
        public AudioClip introVoiceClip; // "Post each word into the right letterbox. Listen to the MIDDLE sound!"
        public AudioClip distractorWarningClip; // "Careful! That word does not have any of our five short sounds. Put it in 'Not today'!"
        public AudioClip shortVowelStarBadgeVoiceClip; // "You can hear every short vowel. You are a SHORT VOWEL STAR!"
        public AudioClip unit5UnlockVoiceClip; // "Unit Five is open! Next time — the LONG vowels..."
        public AudioClip taraStarRoundOpenerClip; // "My turn! Six quick challenges. Ready? Roar!"
        public AudioClip vowelSongAudioClip; // Short vowel song clip ă ĕ ĭ ŏ ŭ

        [Header("Visual Assets")]
        public Sprite shortVowelStarBadgeSprite;

        [Header("25 Sorting Word Cards (15 Short Vowels + 3 Distractors + List - p. 28)")]
        public SortingWordCardItem[] sortingCards = new SortingWordCardItem[25];

        [Header("6 Star Round Challenges with Tara")]
        public StarRoundUnit4Challenge[] starChallenges = new StarRoundUnit4Challenge[6];
    }
}
