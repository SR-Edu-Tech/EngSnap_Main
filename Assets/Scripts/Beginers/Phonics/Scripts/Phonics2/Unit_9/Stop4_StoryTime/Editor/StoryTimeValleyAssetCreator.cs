#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit9.Editor
{
    public static class StoryTimeValleyAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create Story Time Valley Data (Unit 9 Stop 4)", false, 94)]
        public static void CreateStoryTimeValleyAsset()
        {
            StoryTimeValleyData asset = ScriptableObject.CreateInstance<StoryTimeValleyData>();
            asset.PopulateDefaultData();

            string resourcesFolderPath = "Assets/Resources/Phonics2/Unit9";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(resourcesFolderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit9");

            string resourcesAssetPath = $"{resourcesFolderPath}/StoryTimeValleyData_Unit9.asset";
            AssetDatabase.CreateAsset(asset, resourcesAssetPath);

            string scriptableFolderPath = "Assets/ScriptableObj/Phonics 2/Unit 9/Story Time";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 9/Story Time"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj")) AssetDatabase.CreateFolder("Assets", "ScriptableObj");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 9")) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2", "Unit 9");
                if (!AssetDatabase.IsValidFolder(scriptableFolderPath)) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2/Unit 9", "Story Time");
            }

            string scriptableAssetPath = $"{scriptableFolderPath}/StoryTimeValleyData_Unit9.asset";
            AssetDatabase.CopyAsset(resourcesAssetPath, scriptableAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created StoryTimeValleyData assets at:\n- {resourcesAssetPath}\n- {scriptableAssetPath}");
        }
    }
}
#endif
