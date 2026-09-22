using System.Collections.Generic;
using UnityEngine;

namespace EngSnap.ConfusedWords
{
    [CreateAssetMenu(fileName = "ConfusedEnglishWords_Unit7_Content", menuName = "EngSnap/Unit 7/Content Data")]
    public class M3A_U7_ContentData : ScriptableObject
    {
        [Header("Unit Metadata")]
        [SerializeField] private string unitId = M3A_U7_ConfusedWordsData.UnitId;
        [SerializeField] private string unitName = M3A_U7_ConfusedWordsData.UnitName;
        [SerializeField] private string unitTheme = M3A_U7_ConfusedWordsData.UnitTheme;
        [SerializeField] private string badgeId = M3A_U7_ConfusedWordsData.BadgeId;
        [SerializeField] private string badgeDisplayName = M3A_U7_ConfusedWordsData.BadgeDisplayName;

        [Header("Curriculum Bank")]
        [SerializeField] private List<U7_WordEntry> wordEntries = new List<U7_WordEntry>(M3A_U7_ConfusedWordsData.WordBank);
        [SerializeField] private List<U7_SentenceEntry> sentenceEntries = new List<U7_SentenceEntry>(M3A_U7_ConfusedWordsData.SentenceBank);
        [SerializeField] private List<U7_SubjectObjectTestEntry> testEntries = new List<U7_SubjectObjectTestEntry>(M3A_U7_ConfusedWordsData.SubjectObjectTestBank);

        [Header("Audio Registry")]
        [SerializeField] private M3A_U7_AudioRegistry audioRegistry = new M3A_U7_AudioRegistry();

        public string UnitId => unitId;
        public string UnitName => unitName;
        public string UnitTheme => unitTheme;
        public string BadgeId => badgeId;
        public string BadgeDisplayName => badgeDisplayName;
        public IReadOnlyList<U7_WordEntry> WordEntries => wordEntries;
        public IReadOnlyList<U7_SentenceEntry> SentenceEntries => sentenceEntries;
        public IReadOnlyList<U7_SubjectObjectTestEntry> TestEntries => testEntries;
        public M3A_U7_AudioRegistry AudioRegistry => audioRegistry;


        public void ResetToAuthoritativeDefaults()
        {
            unitId = M3A_U7_ConfusedWordsData.UnitId;
            unitName = M3A_U7_ConfusedWordsData.UnitName;
            unitTheme = M3A_U7_ConfusedWordsData.UnitTheme;
            badgeId = M3A_U7_ConfusedWordsData.BadgeId;
            badgeDisplayName = M3A_U7_ConfusedWordsData.BadgeDisplayName;
            wordEntries = new List<U7_WordEntry>(M3A_U7_ConfusedWordsData.WordBank);
            sentenceEntries = new List<U7_SentenceEntry>(M3A_U7_ConfusedWordsData.SentenceBank);
            testEntries = new List<U7_SubjectObjectTestEntry>(M3A_U7_ConfusedWordsData.SubjectObjectTestBank);
        }
    }
}
