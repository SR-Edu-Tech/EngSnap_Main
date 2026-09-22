#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit7.Editor
{
    public static class BackCarriageAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create Back Carriage Data (Unit 7 Stop 2)", false, 72)]
        public static void CreateBackCarriageAsset()
        {
            BackCarriageData asset = ScriptableObject.CreateInstance<BackCarriageData>();

            // Setup 8 Clean Ending Sounds (van, man, hat, bat, ship, crab, drum, bag)
            asset.cleanEndingItems = new BackCarriageItem[8];
            string[] cleanWords = new string[] { "van", "man", "hat", "bat", "ship", "crab", "drum", "bag" };
            string[] cleanLast = new string[] { "n", "n", "t", "t", "p", "b", "m", "g" };
            string[] cleanGaps = new string[] { "va_", "ma_", "ha_", "ba_", "shi_", "cra_", "dru_", "ba_" };
            string[][] cleanOptions = new string[][]
            {
                new string[] { "n", "m", "t", "d" },
                new string[] { "n", "m", "p", "t" },
                new string[] { "t", "d", "p", "b" },
                new string[] { "t", "d", "k", "g" },
                new string[] { "p", "b", "t", "d" },
                new string[] { "b", "p", "d", "t" },
                new string[] { "m", "n", "b", "d" },
                new string[] { "g", "k", "d", "t" }
            };

            for (int i = 0; i < 8; i++)
            {
                asset.cleanEndingItems[i] = new BackCarriageItem
                {
                    fullWord = cleanWords[i],
                    lastLetter = cleanLast[i],
                    displayGapWord = cleanGaps[i],
                    tileOptions = cleanOptions[i]
                };
            }

            // Setup 4 Front or Back Position Quiz Items
            asset.frontOrBackItems = new FrontOrBackQuizItem[4];
            asset.frontOrBackItems[0] = new FrontOrBackQuizItem
            {
                fullWord = "hat",
                testSound = "/t/",
                correctPosition = CarriagePositionTarget.Back
            };
            asset.frontOrBackItems[1] = new FrontOrBackQuizItem
            {
                fullWord = "top",
                testSound = "/t/",
                correctPosition = CarriagePositionTarget.Front
            };
            asset.frontOrBackItems[2] = new FrontOrBackQuizItem
            {
                fullWord = "van",
                testSound = "/v/",
                correctPosition = CarriagePositionTarget.Front
            };
            asset.frontOrBackItems[3] = new FrontOrBackQuizItem
            {
                fullWord = "crab",
                testSound = "/b/",
                correctPosition = CarriagePositionTarget.Back
            };

            // Setup 3 Tricky Ending Bonus Items (nest, belt, milk)
            asset.trickyBonusItems = new TrickyEndingBonusItem[3];
            asset.trickyBonusItems[0] = new TrickyEndingBonusItem
            {
                fullWord = "nest",
                endingTwoSounds = "st",
                explanationText = "TWO sounds at the end: s - t!"
            };
            asset.trickyBonusItems[1] = new TrickyEndingBonusItem
            {
                fullWord = "belt",
                endingTwoSounds = "lt",
                explanationText = "TWO sounds at the end: l - t!"
            };
            asset.trickyBonusItems[2] = new TrickyEndingBonusItem
            {
                fullWord = "milk",
                endingTwoSounds = "lk",
                explanationText = "TWO sounds at the end: l - k!"
            };

            string resourcesFolderPath = "Assets/Resources/Phonics2/Unit7";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(resourcesFolderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit7");

            string resourcesAssetPath = $"{resourcesFolderPath}/BackCarriageData_Unit7.asset";
            AssetDatabase.CreateAsset(asset, resourcesAssetPath);

            string scriptableFolderPath = "Assets/ScriptableObj/Phonics 2/Unit 7/Back Carriage";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 7/Back Carriage"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj")) AssetDatabase.CreateFolder("Assets", "ScriptableObj");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 7")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2/Unit 7");
                if (!AssetDatabase.IsValidFolder(scriptableFolderPath)) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2/Unit 7", "Back Carriage");
            }

            string scriptableAssetPath = $"{scriptableFolderPath}/BackCarriageData_Unit7.asset";
            AssetDatabase.CopyAsset(resourcesAssetPath, scriptableAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created BackCarriageData assets at:\n- {resourcesAssetPath}\n- {scriptableAssetPath}");
        }
    }
}
#endif
