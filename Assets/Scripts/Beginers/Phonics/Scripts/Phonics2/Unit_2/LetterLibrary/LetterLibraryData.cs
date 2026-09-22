using UnityEngine;

namespace EngSnap.Phonics2.Unit2
{
    [System.Serializable]
    public class LibraryLetterCard
    {
        public char letterChar = 'A';
        public string letterName = "ay";
        public string letterSound = "/a/";
        public string pictureWord = "apple";
        public Sprite letterSprite;
        public Sprite pictureWordSprite;
        public Sprite mouthCloseUpSprite;
        public AudioClip cardAudioClip; // 3-part: Name + Sound + Picture word ("This is B. Its name is bee. Its sound is /b/. /b/ bike!")
        public AudioClip soundOnlyClip; // Clipped sound clip for quiz
        public MouthType mouthType = MouthType.OpenVowel;
    }

    [System.Serializable]
    public class LibraryShelfGroup
    {
        public string shelfName = "Shelf 1: A-F";
        public int startLetterOffset = 0;
        public int letterCount = 6;
        public LibraryLetterCard[] shelfLetters;
        public int[] quizTargetIndices; // 5 check questions
    }

    [CreateAssetMenu(fileName = "NewLetterLibraryData", menuName = "EngSnap/Phonics2/Unit 2/Letter Library Data")]
    public class LetterLibraryData : ScriptableObject
    {
        [Header("Leo & Momo Voice Clips")]
        public AudioClip libraryIntroClip; // "This is the Letter Library. Every letter has a card. Tap one!"
        public AudioClip quizInstructionClip; // "Which letter says...?"
        public AudioClip shelfCompleteVoiceClip; // "Six letters done! Your shelf is glowing. Come back any time to hear them."
        public AudioClip momoQuizHintClip; // "This one! Say it with me — /g/!"

        [Header("4 Shelves (A-F, G-M, N-T, U-Z)")]
        public LibraryShelfGroup[] shelves;
    }
}
