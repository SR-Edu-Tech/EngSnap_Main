#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit7.Editor
{
    public static class MiddleCarriageAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create Middle Carriage Data (Unit 7 Stop 3)", false, 73)]
        public static void CreateMiddleCarriageAsset()
        {
            MiddleCarriageData asset = ScriptableObject.CreateInstance<MiddleCarriageData>();

            // Setup 10 Middle Sound Words from book p. 39
            // fan, bell, fist, cup, net, cat, jug, doll, lock, zip
            asset.middleItems = new MiddleCarriageItem[10];

            string[] words = new string[] { "fan", "bell", "fist", "cup", "net", "cat", "jug", "doll", "lock", "zip" };
            string[] firsts = new string[] { "f", "b", "f", "c", "n", "c", "j", "d", "l", "z" };
            string[] vowels = new string[] { "a", "e", "i", "u", "e", "a", "u", "o", "o", "i" };
            string[] lasts = new string[] { "n", "ll", "st", "p", "t", "t", "g", "ll", "ck", "p" };

            for (int i = 0; i < 10; i++)
            {
                asset.middleItems[i] = new MiddleCarriageItem
                {
                    fullWord = words[i],
                    firstLetter = firsts[i],
                    middleVowel = vowels[i],
                    lastLetter = lasts[i]
                };
            }

            // Setup 3 Odd One Out Items
            asset.oddOneOutItems = new OddOneOutVowelItem[3];
            asset.oddOneOutItems[0] = new OddOneOutVowelItem
            {
                wordChoices = new string[] { "cat", "jug", "fan" },
                middleVowels = new string[] { "a", "u", "a" },
                oddChoiceIndex = 1,
                explanationText = "'jug' has /u/, while 'cat' and 'fan' have /a/!"
            };
            asset.oddOneOutItems[1] = new OddOneOutVowelItem
            {
                wordChoices = new string[] { "net", "bell", "cup" },
                middleVowels = new string[] { "e", "e", "u" },
                oddChoiceIndex = 2,
                explanationText = "'cup' has /u/, while 'net' and 'bell' have /e/!"
            };
            asset.oddOneOutItems[2] = new OddOneOutVowelItem
            {
                wordChoices = new string[] { "zip", "doll", "fist" },
                middleVowels = new string[] { "i", "o", "i" },
                oddChoiceIndex = 1,
                explanationText = "'doll' has /o/, while 'zip' and 'fist' have /i/!"
            };

            string resourcesFolderPath = "Assets/Resources/Phonics2/Unit7";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(resourcesFolderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit7");

            string resourcesAssetPath = $"{resourcesFolderPath}/MiddleCarriageData_Unit7.asset";
            AssetDatabase.CreateAsset(asset, resourcesAssetPath);

            string scriptableFolderPath = "Assets/ScriptableObj/Phonics 2/Unit 7/Middle Carriage";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 7/Middle Carriage"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj")) AssetDatabase.CreateFolder("Assets", "ScriptableObj");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 7")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2/Unit 7");
                if (!AssetDatabase.IsValidFolder(scriptableFolderPath)) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2/Unit 7", "Middle Carriage");
            }

            string scriptableAssetPath = $"{scriptableFolderPath}/MiddleCarriageData_Unit7.asset";
            AssetDatabase.CopyAsset(resourcesAssetPath, scriptableAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created MiddleCarriageData assets at:\n- {resourcesAssetPath}\n- {scriptableAssetPath}");
        }
    }
}
#endif
