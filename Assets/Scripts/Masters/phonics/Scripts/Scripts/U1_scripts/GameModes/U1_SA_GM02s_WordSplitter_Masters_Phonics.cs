using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

namespace MastersPhonics
{
    public class U1_SA_GM02s_WordSplitter_Masters_Phonics : MonoBehaviour
    {
        [Header("Data")]
        public U1_SA_WordDataSO_Masters_Phonics currentWordData;

        [Header("Events")]
        public UnityEvent OnSplitSuccess;
        public UnityEvent OnSplitFail;

        private string currentWord;
        private List<int> correctSplitIndices = new List<int>();

        public void LoadWord(U1_SA_WordDataSO_Masters_Phonics wordData)
        {
            currentWordData = wordData;
            currentWord = wordData.fullWord;
            CalculateCorrectSplits();
            // In a full implementation, you would spawn UI text elements here
            // and interleave them with invisible/visible 'gap' buttons.
        }

        private void CalculateCorrectSplits()
        {
            correctSplitIndices.Clear();
            int currentIndex = 0;
            
            // If the word is "jumping" and components are "jump", "ing"
            // The split index is after the 'p' (index 4).
            for (int i = 0; i < currentWordData.wordComponents.Count - 1; i++)
            {
                currentIndex += currentWordData.wordComponents[i].Length;
                correctSplitIndices.Add(currentIndex);
            }
        }

        /// <summary>
        /// Called by a UI Button placed between characters.
        /// 'splitIndex' represents the character index after which the split occurs.
        /// </summary>
        public void AttemptSplit(int splitIndex)
        {
            if (correctSplitIndices.Contains(splitIndex))
            {
                Debug.Log($"Correct split at index {splitIndex}");
                OnSplitSuccess?.Invoke();
                // Play success audio
                if (U1_SA_AudioManager_Masters_Phonics.Instance != null && currentWordData.componentAudioClips.Count > 0)
                {
                    // Basic logic: Play the first component audio on success, can be expanded
                    U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceB(currentWordData.componentAudioClips[0]);
                }
            }
            else
            {
                Debug.Log($"Incorrect split at index {splitIndex}");
                OnSplitFail?.Invoke();
            }
        }
    }
}
