using System;
using System.Collections.Generic;
using UnityEngine;

namespace EngSnap.Phonics2.Unit8
{
    public enum SwapSlotPosition
    {
        Front,
        Middle,
        End
    }

    [Serializable]
    public class SwapStepItem
    {
        public SwapSlotPosition position = SwapSlotPosition.Front;
        public string startingWord = "cat";
        public string targetLetter = "b";
        public string resultingWord = "bat";
        public bool isRealWord = true;
        public Sprite wordPictureSprite;
        public AudioClip wordAudioClip;
        public AudioClip promptVoiceClip;
    }

    [Serializable]
    public class FamilyWallGroup
    {
        public string rimePattern = "at"; // e.g., at, an, ap, ed, en, et
        public string[] words = new string[] { "cat", "bat", "hat", "mat", "rat" };
    }

    [CreateAssetMenu(fileName = "WordBuilderData_Unit8", menuName = "EngSnap/Phonics2/Unit8/Word Builder Data")]
    public class WordBuilderData : ScriptableObject
    {
        [Header("Leo & Monster Voice Scripts")]
        public AudioClip leoIntroClip;              // "The machine is back — and now ALL THREE letters can turn!"
        public AudioClip frontSwapInstructionClip;  // "Change the c to a b. What does it say now? Slide and read!"
        public AudioClip wordSuccessClip;           // "BAT! One letter changed and it is a whole new word!"
        public AudioClip familyWallRevealClip;      // "Look — cat, bat, hat, mat. They all end the same. That is a word family!"
        public AudioClip nonsenseMonsterVoiceClip;  // "Bppppt! Not a word — just a silly sound!"
        public AudioClip closingWallCountClip;      // "Your wall is getting big. Every one of those, you can read!"

        [Header("Silly Monster Assets")]
        public Sprite[] sillyMonsterSprites = new Sprite[4];
        public AudioClip nonsenseRaspberrySfx;

        [Header("Swap Steps (12 Rounds)")]
        // Front (5): cat->bat->hat->mat->rat
        // End (4): can->cap->cat, bed->beg->bet
        // Middle (3): cat->cot->cut, bag->beg->big
        public SwapStepItem[] swapSteps = new SwapStepItem[12];

        [Header("Rhyming Family Wall Groups")]
        public FamilyWallGroup[] familyGroups = new FamilyWallGroup[6];

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

            // 1-5: Front swap (cat -> bat -> hat -> mat -> rat)
            swapSteps[0] = new SwapStepItem { position = SwapSlotPosition.Front, startingWord = "cat", targetLetter = "b", resultingWord = "bat", isRealWord = true };
            swapSteps[1] = new SwapStepItem { position = SwapSlotPosition.Front, startingWord = "bat", targetLetter = "h", resultingWord = "hat", isRealWord = true };
            swapSteps[2] = new SwapStepItem { position = SwapSlotPosition.Front, startingWord = "hat", targetLetter = "m", resultingWord = "mat", isRealWord = true };
            swapSteps[3] = new SwapStepItem { position = SwapSlotPosition.Front, startingWord = "mat", targetLetter = "r", resultingWord = "rat", isRealWord = true };
            swapSteps[4] = new SwapStepItem { position = SwapSlotPosition.Front, startingWord = "rat", targetLetter = "c", resultingWord = "cat", isRealWord = true };

            // 6-8: End swap short a (can -> cap -> cab)
            swapSteps[5] = new SwapStepItem { position = SwapSlotPosition.End, startingWord = "can", targetLetter = "p", resultingWord = "cap", isRealWord = true };
            swapSteps[6] = new SwapStepItem { position = SwapSlotPosition.End, startingWord = "cap", targetLetter = "b", resultingWord = "cab", isRealWord = true };
            // 9: End swap short e (bed -> beg -> bet)
            swapSteps[7] = new SwapStepItem { position = SwapSlotPosition.End, startingWord = "bed", targetLetter = "g", resultingWord = "beg", isRealWord = true };
            swapSteps[8] = new SwapStepItem { position = SwapSlotPosition.End, startingWord = "beg", targetLetter = "t", resultingWord = "bet", isRealWord = true };

            // 10-12: Middle vowel swaps (cat -> cot -> cut, bag -> beg -> big)
            swapSteps[9] = new SwapStepItem { position = SwapSlotPosition.Middle, startingWord = "cat", targetLetter = "o", resultingWord = "cot", isRealWord = true };
            swapSteps[10] = new SwapStepItem { position = SwapSlotPosition.Middle, startingWord = "bag", targetLetter = "e", resultingWord = "beg", isRealWord = true };
            swapSteps[11] = new SwapStepItem { position = SwapSlotPosition.Middle, startingWord = "beg", targetLetter = "i", resultingWord = "big", isRealWord = true };

            // Family Wall Groups (at, an, ap, ed, en, et)
            familyGroups = new FamilyWallGroup[6];
            familyGroups[0] = new FamilyWallGroup { rimePattern = "-at", words = new string[] { "cat", "bat", "hat", "mat", "rat" } };
            familyGroups[1] = new FamilyWallGroup { rimePattern = "-an", words = new string[] { "can", "fan", "man", "pan", "ran" } };
            familyGroups[2] = new FamilyWallGroup { rimePattern = "-ap", words = new string[] { "cap", "map", "tap", "nap", "lap" } };
            familyGroups[3] = new FamilyWallGroup { rimePattern = "-ed", words = new string[] { "bed", "red", "fed", "led", "wed" } };
            familyGroups[4] = new FamilyWallGroup { rimePattern = "-en", words = new string[] { "pen", "ten", "hen", "men", "den" } };
            familyGroups[5] = new FamilyWallGroup { rimePattern = "-et", words = new string[] { "net", "pet", "wet", "get", "let" } };
        }
    }
}
