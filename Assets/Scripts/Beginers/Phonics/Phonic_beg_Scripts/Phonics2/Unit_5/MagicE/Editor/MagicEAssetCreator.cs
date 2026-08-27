#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit5.Editor
{
    public static class MagicEAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create Magic E Data (Unit 5)", false, 52)]
        public static void CreateMagicEAsset()
        {
            MagicEData asset = ScriptableObject.CreateInstance<MagicEData>();

            // Setup 8 Transformation Pairs (cap -> cape, kit -> kite, etc.)
            asset.transformPairs = new MagicETransformPair[8];
            string[] shorts = new string[] { "cap", "kit", "tub", "mad", "pin", "cub", "tap", "hop" };
            string[] longs = new string[] { "cape", "kite", "tube", "made", "pine", "cube", "tape", "hope" };
            int[] vIndices = new int[] { 1, 1, 1, 1, 1, 1, 1, 1 };
            string[] vSounds = new string[] { "ay", "eye", "you", "ay", "eye", "you", "ay", "oh" };

            for (int i = 0; i < 8; i++)
            {
                asset.transformPairs[i] = new MagicETransformPair
                {
                    shortWord = shorts[i],
                    longWord = longs[i],
                    vowelCharIndex = vIndices[i],
                    vowelSoundName = vSounds[i]
                };
            }

            // Setup 6 Which One Choices
            asset.whichOneChoices = new MagicEWhichOneChoice[6];
            asset.whichOneChoices[0] = new MagicEWhichOneChoice { wordA = "cap", wordB = "cape", correctIndex = 1 };
            asset.whichOneChoices[1] = new MagicEWhichOneChoice { wordA = "pin", wordB = "pine", correctIndex = 1 };
            asset.whichOneChoices[2] = new MagicEWhichOneChoice { wordA = "tub", wordB = "tube", correctIndex = 1 };
            asset.whichOneChoices[3] = new MagicEWhichOneChoice { wordA = "kit", wordB = "kite", correctIndex = 1 };
            asset.whichOneChoices[4] = new MagicEWhichOneChoice { wordA = "mad", wordB = "made", correctIndex = 1 };
            asset.whichOneChoices[5] = new MagicEWhichOneChoice { wordA = "hop", wordB = "hope", correctIndex = 1 };

            // Setup 26 Word Wall List (pp. 30, 32, 34)
            asset.magicEWordWallList = new string[]
            {
                "cake", "take", "bake", "make", "game", "same", "fame",
                "tape", "safe", "case", "vase", "bike", "like", "hike",
                "line", "mine", "dime", "lime", "side", "hide", "ride",
                "tube", "cube", "June", "rule", "tune"
            };

            // Save in Resources folder for runtime fallback loading
            string resourcesFolderPath = "Assets/Resources/Phonics2/Unit5";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(resourcesFolderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit5");

            string resourcesAssetPath = $"{resourcesFolderPath}/MagicEData_Unit5.asset";
            AssetDatabase.CreateAsset(asset, resourcesAssetPath);

            // Also ensure copy/asset in ScriptableObj hierarchy
            string scriptableFolderPath = "Assets/ScriptableObj/Phonics 2/Unit 5/Magic E";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 5/Magic E"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj")) AssetDatabase.CreateFolder("Assets", "ScriptableObj");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 5")) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2", "Unit 5");
                if (!AssetDatabase.IsValidFolder(scriptableFolderPath)) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2/Unit 5", "Magic E");
            }

            string scriptableAssetPath = $"{scriptableFolderPath}/MagicEData_Unit5.asset";
            AssetDatabase.CopyAsset(resourcesAssetPath, scriptableAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created MagicEData assets at:\n- {resourcesAssetPath}\n- {scriptableAssetPath}");
        }
    }
}
#endif
