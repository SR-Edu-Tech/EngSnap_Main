#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit9.Editor
{
    public static class SentenceStreetAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create Sentence Street Data (Unit 9 Stop 3)", false, 93)]
        public static void CreateSentenceStreetAsset()
        {
            SentenceStreetData asset = ScriptableObject.CreateInstance<SentenceStreetData>();
            asset.PopulateDefaultData();

            string resourcesFolderPath = "Assets/Resources/Phonics2/Unit9";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(resourcesFolderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit9");

            string resourcesAssetPath = $"{resourcesFolderPath}/SentenceStreetData_Unit9.asset";
            AssetDatabase.CreateAsset(asset, resourcesAssetPath);

            string scriptableFolderPath = "Assets/ScriptableObj/Phonics 2/Unit 9/Sentence Street";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 9/Sentence Street"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj")) AssetDatabase.CreateFolder("Assets", "ScriptableObj");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 9")) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2", "Unit 9");
                if (!AssetDatabase.IsValidFolder(scriptableFolderPath)) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2/Unit 9", "Sentence Street");
            }

            string scriptableAssetPath = $"{scriptableFolderPath}/SentenceStreetData_Unit9.asset";
            AssetDatabase.CopyAsset(resourcesAssetPath, scriptableAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created SentenceStreetData assets at:\n- {resourcesAssetPath}\n- {scriptableAssetPath}");
        }
    }
}
#endif
