using UnityEngine;

namespace EngSnap.Phonics2.Unit1
{
    [System.Serializable]
    public class AlphabetCardItem
    {
        public char letterChar = 'A';
        public string letterName = "ay";
        public string letterSound = "/a/";
        public string pictureWord = "apple";
        public Sprite letterSprite;
        public Sprite pictureWordSprite;
        public AudioClip cardAudioClip; // 3-part clip: Name + Sound + Picture word
        public AudioClip soundOnlyClip; // Clipped sound clip for quiz
        public bool isVowel = false;
    }

    [CreateAssetMenu(fileName = "NewAlphabetParadeData", menuName = "EngSnap/Phonics2/Unit 1/Alphabet Parade Data")]
    public class AlphabetParadeData : ScriptableObject
    {
        [Header("Leo & Momo Voice Script Clips")]
        public AudioClip introVoiceClip; // "Here comes the Alphabet Parade! Twenty-six letters, all in a line."
        public AudioClip alphabetSongClip; // Alphabet Song (~25 s)
        public AudioClip keyTeachingVoiceClip; // "Every letter has TWO things: a name, and a sound. Listen."
        public AudioClip freeExploreInstructionClip; // "Tap any letter you like!"
        public AudioClip quizInstructionClip; // "Which letter says...?"
        public AudioClip spellReadVoiceClip; // "Names help us spell. Sounds help us READ. We will use the sounds."
        public AudioClip vowelPeekVoiceClip; // "Look — these five letters are glowing. A, E, I, O, U. They are special. We will meet them next time!"

        [Header("26 Letter Card Items (Aa to Zz)")]
        public AlphabetCardItem[] alphabetCards;

        [Header("Quiz Targets (/s/, /m/, /a/, /t/, /b/)")]
        public int[] quizLetterIndices = new int[] { 18, 12, 0, 19, 1 }; // S, M, A, T, B
    }
}
