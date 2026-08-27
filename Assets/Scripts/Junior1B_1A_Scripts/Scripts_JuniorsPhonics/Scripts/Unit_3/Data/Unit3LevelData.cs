    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "New Unit 3 Level", menuName = "Phonics/Unit 3/Level Data")]
    public class Unit3LevelData : ScriptableObject
    {
        public string vowelName; // "Short a"
        public Sprite vowelBadge;

        [Header("Activity 1: Blend & Read")]
        public List<CVCWordData> blendReadWords = new List<CVCWordData>(); // 6-8 words

        [Header("Activity 2: Word Hunt")]
        public WordHuntGridData huntGrid;

        [Header("Activity 3: Spell the Picture")]
        public List<CVCWordData> spellPictureWords = new List<CVCWordData>(); // 6 words
    }