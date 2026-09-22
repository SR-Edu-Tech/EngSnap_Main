using UnityEngine;

namespace EngSnap.Common.ShortVowels
{
    public enum RecapQuestionType
    {
        TapPictureForWord = 0,
        FillMissingVowel = 1,
        PickRhymingWord = 2
    }

    [System.Serializable]
    public class RecapQuestionData
    {
        [Tooltip("Question mode: TapPictureForWord, FillMissingVowel, or PickRhymingWord.")]
        public RecapQuestionType questionType = RecapQuestionType.TapPictureForWord;

        [Tooltip("Question prompt text e.g. 'Which one is a cat?' or 'c _ t' or 'What rhymes with cat?'")]
        public string questionText = "Which one is a cat?";

        [Tooltip("Target word audio or question voice clip.")]
        public AudioClip questionAudioClip;

        [Tooltip("Optional prompt picture sprite (e.g. image of a cat for 'c _ t' or rhyming prompt).")]
        public Sprite promptSprite;

        [Tooltip("Choice picture sprites for options.")]
        public Sprite[] choiceSprites;

        [Tooltip("Choice labels for options e.g. ['cat', 'bat', 'hat'] or ['a', 'e', 'i'].")]
        public string[] choiceWords;

        [Tooltip("0-based index of the correct answer.")]
        public int correctChoiceIndex = 0;
    }

    [CreateAssetMenu(fileName = "NewShortVowelStoryData", menuName = "EngSnap/Short Vowels/Story Data")]
    public class ShortVowelStoryData : ScriptableObject
    {
        [Header("Story / Unit Config")]
        public string title = "Dad Has a Cat";
        public string targetVowel = "a";

        [Header("Recap Star Round Questions (3-4 Quick Rounds)")]
        public RecapQuestionData[] recapQuestions;
    }
}
