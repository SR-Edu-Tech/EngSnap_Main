using System;
using UnityEngine;
using EngSnap.Phonics2.Unit8;

namespace EngSnap.Phonics2.Unit9
{
    [Serializable]
    public class FluencyRowItem
    {
        public string[] words = new string[6];
    }

    [CreateAssetMenu(fileName = "WordBuilderValleyData_Unit9", menuName = "EngSnap/Phonics2/Unit9/Word Builder Valley Data")]
    public class WordBuilderValleyData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip leoIntroClip;              // "More words for your wall! Turn the dial."
        public AudioClip familyRevealClip;          // "pig, big, dig, wig — they all end in 'ig'. A whole family!"
        public AudioClip wallWordCountClip;         // "Look at your wall. Every single one of those, you can read. Count them!"
        public AudioClip fluencyRowIntroClip;       // "Read this row to me, at your own speed. I will wait."
        public AudioClip fluencyPraiseClip;         // "Smooth and quick. That is called reading fluently!"

        [Header("Swap Steps (12 Rounds)")]
        // Front: pig->big->dig->wig, pot->hot->dot->cot, bug->hug->mug->rug
        // End: pig->pit->pin, cup->cub->cut
        // Middle: pig->pug, cat->cot, bag->bug, hat->hot, pin->pan
        public SwapStepItem[] swapSteps = new SwapStepItem[12];

        [Header("Rhyming Family Wall Groups (ig, it, ot, og, ug, up)")]
        public FamilyWallGroup[] familyGroups = new FamilyWallGroup[6];

        [Header("Fluency Reading Rows (2 Rounds)")]
        public FluencyRowItem[] fluencyRows = new FluencyRowItem[2];

        [Header("Audio SFX")]
        public AudioClip slotSpinSfx;
        public AudioClip slotLockSfx;
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip wallPopSfx;

        private void Reset()
        {
            PopulateDefaultData();
        }

        [ContextMenu("Populate Default Data")]
        public void PopulateDefaultData()
        {
            swapSteps = new SwapStepItem[12];

            // 0-3: Front swaps (pig -> big -> dig -> wig)
            swapSteps[0] = new SwapStepItem { position = SwapSlotPosition.Front, startingWord = "pig", targetLetter = "b", resultingWord = "big", isRealWord = true };
            swapSteps[1] = new SwapStepItem { position = SwapSlotPosition.Front, startingWord = "big", targetLetter = "d", resultingWord = "dig", isRealWord = true };
            swapSteps[2] = new SwapStepItem { position = SwapSlotPosition.Front, startingWord = "dig", targetLetter = "w", resultingWord = "wig", isRealWord = true };

            // 3-5: Front swaps (pot -> hot -> dot -> cot)
            swapSteps[3] = new SwapStepItem { position = SwapSlotPosition.Front, startingWord = "pot", targetLetter = "h", resultingWord = "hot", isRealWord = true };
            swapSteps[4] = new SwapStepItem { position = SwapSlotPosition.Front, startingWord = "hot", targetLetter = "d", resultingWord = "dot", isRealWord = true };
            swapSteps[5] = new SwapStepItem { position = SwapSlotPosition.Front, startingWord = "dot", targetLetter = "c", resultingWord = "cot", isRealWord = true };

            // 6-7: End swaps (pig -> pit -> pin, cup -> cub -> cut)
            swapSteps[6] = new SwapStepItem { position = SwapSlotPosition.End, startingWord = "pig", targetLetter = "t", resultingWord = "pit", isRealWord = true };
            swapSteps[7] = new SwapStepItem { position = SwapSlotPosition.End, startingWord = "pit", targetLetter = "n", resultingWord = "pin", isRealWord = true };
            swapSteps[8] = new SwapStepItem { position = SwapSlotPosition.End, startingWord = "cup", targetLetter = "b", resultingWord = "cub", isRealWord = true };

            // 9-11: Middle vowel swaps (pig -> pug, bag -> bug, pin -> pan)
            swapSteps[9] = new SwapStepItem { position = SwapSlotPosition.Middle, startingWord = "pig", targetLetter = "u", resultingWord = "pug", isRealWord = true };
            swapSteps[10] = new SwapStepItem { position = SwapSlotPosition.Middle, startingWord = "bag", targetLetter = "u", resultingWord = "bug", isRealWord = true };
            swapSteps[11] = new SwapStepItem { position = SwapSlotPosition.Middle, startingWord = "pin", targetLetter = "a", resultingWord = "pan", isRealWord = true };

            // Family groups
            familyGroups = new FamilyWallGroup[6];
            familyGroups[0] = new FamilyWallGroup { rimePattern = "-ig", words = new string[] { "pig", "big", "dig", "wig", "fig" } };
            familyGroups[1] = new FamilyWallGroup { rimePattern = "-it", words = new string[] { "pit", "sit", "hit", "bit", "fit" } };
            familyGroups[2] = new FamilyWallGroup { rimePattern = "-ot", words = new string[] { "pot", "hot", "dot", "cot", "lot" } };
            familyGroups[3] = new FamilyWallGroup { rimePattern = "-og", words = new string[] { "dog", "log", "fog", "jog", "bog" } };
            familyGroups[4] = new FamilyWallGroup { rimePattern = "-ug", words = new string[] { "bug", "hug", "mug", "rug", "jug" } };
            familyGroups[5] = new FamilyWallGroup { rimePattern = "-up", words = new string[] { "cup", "pup", "sup" } };

            // Fluency rows
            fluencyRows = new FluencyRowItem[2];
            fluencyRows[0] = new FluencyRowItem { words = new string[] { "pig", "pot", "bug", "pit", "dog", "cup" } };
            fluencyRows[1] = new FluencyRowItem { words = new string[] { "big", "hot", "hug", "pin", "log", "pup" } };
        }
    }
}
