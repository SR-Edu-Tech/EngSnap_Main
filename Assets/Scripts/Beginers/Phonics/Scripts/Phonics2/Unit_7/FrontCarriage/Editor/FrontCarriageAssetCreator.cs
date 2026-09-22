#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit7.Editor
{
    public static class FrontCarriageAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create Front Carriage Data (Unit 7 Stop 1)", false, 71)]
        public static void CreateFrontCarriageAsset()
        {
            FrontCarriageData asset = ScriptableObject.CreateInstance<FrontCarriageData>();

            // Setup 10 Beginning Sound Words from book p. 38
            // web, drum, flag, rose, goat, top, bed, hen, cake, kite
            asset.beginningItems = new FrontCarriageItem[10];

            string[] words = new string[] { "web", "drum", "flag", "rose", "goat", "top", "bed", "hen", "cake", "kite" };
            string[] firstL = new string[] { "w", "d", "f", "r", "g", "t", "b", "h", "c", "k" };
            string[] gaps = new string[] { "_eb", "_rum", "_lag", "_ose", "_oat", "_op", "_ed", "_en", "_ake", "_ite" };
            string[][] options = new string[][]
            {
                new string[] { "w", "v", "b", "m" },
                new string[] { "d", "b", "p", "t" },
                new string[] { "f", "v", "s", "p" },
                new string[] { "r", "l", "w", "n" },
                new string[] { "g", "c", "k", "j" },
                new string[] { "t", "d", "p", "b" },
                new string[] { "b", "d", "p", "g" },
                new string[] { "h", "n", "m", "w" },
                new string[] { "c", "k", "s", "g" },
                new string[] { "k", "c", "t", "p" }
            };

            bool[] isTracing = new bool[] { false, false, false, true, false, false, false, true, false, false };

            for (int i = 0; i < 10; i++)
            {
                asset.beginningItems[i] = new FrontCarriageItem
                {
                    fullWord = words[i],
                    firstLetter = firstL[i],
                    displayGapWord = gaps[i],
                    tileOptions = options[i],
                    isTracingRound = isTracing[i],
                    checkpointPositions = new Vector2[]
                    {
                        new Vector2(0f, 40f),
                        new Vector2(0f, 0f),
                        new Vector2(0f, -40f)
                    }
                };
            }

            string resourcesFolderPath = "Assets/Resources/Phonics2/Unit7";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(resourcesFolderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit7");

            string resourcesAssetPath = $"{resourcesFolderPath}/FrontCarriageData_Unit7.asset";
            AssetDatabase.CreateAsset(asset, resourcesAssetPath);

            string scriptableFolderPath = "Assets/ScriptableObj/Phonics 2/Unit 7/Front Carriage";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 7/Front Carriage"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj")) AssetDatabase.CreateFolder("Assets", "ScriptableObj");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 7")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2/Unit 7");
                if (!AssetDatabase.IsValidFolder(scriptableFolderPath)) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2/Unit 7", "Front Carriage");
            }

            string scriptableAssetPath = $"{scriptableFolderPath}/FrontCarriageData_Unit7.asset";
            AssetDatabase.CopyAsset(resourcesAssetPath, scriptableAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created FrontCarriageData assets at:\n- {resourcesAssetPath}\n- {scriptableAssetPath}");
        }
    }
}
#endif
