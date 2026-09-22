using UnityEngine;

namespace EngSnap.Phonics2.Unit4
{
    [System.Serializable]
    public class ShortVowelFriendItem
    {
        public string vowelChar = "a"; // "a", "e", "i", "o", "u"
        public string wordName = "cat"; // "cat", "elephant", "igloo", "octopus", "gum"
        public string actionDescription = "Stroke the cat";
        public Sprite vowelSprite;
        public Sprite pictureSprite;
        public Sprite actionSprite; // Action pose sprite (e.g. cat stroking pose, trunk pose, shiver pose)
        public AudioClip vowelSoundClip;
        public AudioClip actionPromptClip;
    }

    [System.Serializable]
    public class MiddleSpotlightWord
    {
        public string wordName = "cat";
        public string vowelChar = "a";
        public Sprite wordSprite;
        public AudioClip wordNormalClip;
        public AudioClip wordMiddleStretchedClip;
    }

    [System.Serializable]
    public class WhichHouseQuizRound
    {
        public string questionPrompt = "Which friend do you hear in the middle?";
        public string targetWord = "bed";
        public int correctVowelIndex = 1; // 0:a, 1:e, 2:i, 3:o, 4:u
        public Sprite promptSprite;
        public AudioClip wordNormalClip;
        public AudioClip wordMiddleStretchedClip;
    }

    [CreateAssetMenu(fileName = "NewFiveShortFriendsData", menuName = "EngSnap/Phonics2/Unit 4/Five Short Friends Data")]
    public class FiveShortFriendsData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip introVoiceClip; // "Welcome to Short Vowel Street! Five friends live here..."
        public AudioClip middleSpotlightIntroClip; // "Listen to the MIDDLE of the word..."
        public AudioClip quizInstructionClip; // "Which friend do you hear in the middle? Tap their house!"
        public AudioClip quizSuccessClip; // "Yes! That is their house!"
        public AudioClip quizRetryClip; // "Listen again — which one is that?"

        [Header("5 Vowel Friends (p. 21)")]
        public ShortVowelFriendItem[] vowelFriends = new ShortVowelFriendItem[5];

        [Header("15 Middle-Sound Spotlight Words (3 per vowel)")]
        public MiddleSpotlightWord[] spotlightWords = new MiddleSpotlightWord[15];

        [Header("10 Which House Quiz Rounds")]
        public WhichHouseQuizRound[] quizRounds = new WhichHouseQuizRound[10];
    }
}
