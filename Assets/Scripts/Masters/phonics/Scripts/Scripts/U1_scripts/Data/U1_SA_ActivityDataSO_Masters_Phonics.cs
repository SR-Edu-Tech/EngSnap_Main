using System.Collections.Generic;
using UnityEngine;

namespace MastersPhonics
{
    public enum GameModeType
    {
        ConceptCard,
        WordSplitter,
        SortingBins,
        BeatPad,
        WordBuilder
    }

    [CreateAssetMenu(fileName = "NewActivityData_Masters_Phonics", menuName = "Masters Phonics/Data/Activity Data")]
    public class U1_SA_ActivityDataSO_Masters_Phonics : ScriptableObject
    {
        [Header("Activity Info")]
        public string activityTitle;
        public GameModeType gameMode;

        [Header("Voice A - Mascot Audio")]
        [Tooltip("Instructions or narration for this activity")]
        public AudioClip instructionAudioClip;
        public AudioClip successFeedbackAudioClip;
        public AudioClip failFeedbackAudioClip;

        [Header("Activity Content")]
        public List<U1_SA_WordDataSO_Masters_Phonics> targetWords;
    }
}
