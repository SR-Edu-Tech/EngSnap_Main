using System.Collections.Generic;
using UnityEngine;

namespace EngSnap.Unit4
{
    [CreateAssetMenu(fileName = "NewCatchTheVowelData", menuName = "EngSnap/Unit 4/Catch The Vowel Data")]
    public class CatchTheVowelData : ScriptableObject
    {
        [System.Serializable]
        public class LetterTileItem
        {
            [Tooltip("Letter string e.g. A, B, E, F, I, M, O, U, Z.")]
            public string letter = "A";

            [Tooltip("True if this letter is a vowel (A, E, I, O, U).")]
            public bool isVowel = true;

            [Tooltip("Local UI Anchored Position offset on the shape container.")]
            public Vector2 localPosition;

            [Tooltip("Phoneme audio clip played when this vowel is caught.")]
            public AudioClip phonemeSoundClip;
        }

        [Header("Shape Game Config")]
        [Tooltip("Shape name string (e.g. Apple, Dolphin).")]
        public string shapeName = "Apple";

        [Tooltip("Background shape image (e.g. Apple or Dolphin graphic from the book).")]
        public Sprite shapeBackgroundSprite;

        [Tooltip("Optional audio clip played when this shape section is completed (e.g. You completed the Apple section!).")]
        public AudioClip sectionCompletedClip;

        [Tooltip("List of letter tiles scattered across this shape.")]
        public List<LetterTileItem> scatteredLetters = new List<LetterTileItem>();
    }
}
