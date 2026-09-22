using System.Collections.Generic;
using UnityEngine;

namespace MastersPhonics
{
    [CreateAssetMenu(fileName = "NewWordData_Masters_Phonics", menuName = "Masters Phonics/Data/Word Data")]
    public class U1_SA_WordDataSO_Masters_Phonics : ScriptableObject
    {
        [Header("Word Properties")]
        public string fullWord;
        public List<string> wordComponents; // e.g., syllables, prefix/root/suffix

        [Header("Voice B - Word Model Audio")]
        [Tooltip("Clean pronunciation of the full word")]
        public AudioClip fullWordAudioClip;
        
        [Tooltip("Clean pronunciation of individual components in order")]
        public List<AudioClip> componentAudioClips;
    }
}
