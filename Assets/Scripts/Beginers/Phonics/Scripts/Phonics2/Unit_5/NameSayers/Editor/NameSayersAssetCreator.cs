#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit5.Editor
{
    public static class NameSayersAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create Name Sayers Data (Unit 5)", false, 51)]
        public static void CreateNameSayersAsset()
        {
            NameSayersData asset = ScriptableObject.CreateInstance<NameSayersData>();

            // Setup 5 Long Vowels
            asset.longVowels = new LongVowelItem[5];
            string[] symbols = new string[] { "ā", "ē", "ī", "ō", "ū" };
            string[] names = new string[] { "ay", "ee", "eye", "oh", "you" };
            string[][] picWords = new string[][]
            {
                new string[] { "acorn", "baby", "alien", "paper" },
                new string[] { "bee", "beach", "eagle", "leaf" },
                new string[] { "bicycle", "kite", "ride", "smile" },
                new string[] { "rose", "hose", "toad", "boat" },
                new string[] { "tube", "computer", "tune", "unicorn" }
            };

            for (int i = 0; i < 5; i++)
            {
                asset.longVowels[i] = new LongVowelItem
                {
                    vowelSymbol = symbols[i],
                    vowelName = names[i],
                    pictureWordNames = picWords[i]
                };
            }

            // Setup 10 Short vs Long Contrast Pairs
            asset.contrastPairs = new ShortLongContrastPair[10];
            string[] shorts = new string[] { "cat", "bed", "pig", "dog", "cup", "cap", "pin", "tub", "hop", "mad" };
            string[] longs = new string[] { "cake", "bee", "bike", "boat", "cube", "cape", "pine", "tube", "hope", "made" };

            for (int i = 0; i < 10; i++)
            {
                asset.contrastPairs[i] = new ShortLongContrastPair
                {
                    shortWord = shorts[i],
                    longWord = longs[i],
                    isLongCorrect = true
                };
            }

            // Setup 5 Hat Swap Rounds
            asset.hatSwapRounds = new HatSwapRound[5];
            asset.hatSwapRounds[0] = new HatSwapRound { wordText = "cat", requiresMacron = false };
            asset.hatSwapRounds[1] = new HatSwapRound { wordText = "cake", requiresMacron = true };
            asset.hatSwapRounds[2] = new HatSwapRound { wordText = "bed", requiresMacron = false };
            asset.hatSwapRounds[3] = new HatSwapRound { wordText = "bee", requiresMacron = true };
            asset.hatSwapRounds[4] = new HatSwapRound { wordText = "bike", requiresMacron = true };

            string folderPath = "Assets/Resources/Phonics2/Unit5";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(folderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit5");

            string assetPath = $"{folderPath}/NameSayersData_Unit5.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created NameSayersData asset at: {assetPath}");
        }
    }
}
#endif
