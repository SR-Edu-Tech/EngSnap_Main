#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit10.Editor
{
    public static class EllFamilyAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create The Ell Family Data (Unit 10 Stop 2)", false, 102)]
        public static void CreateEllFamilyAsset()
        {
            EllFamilyData asset = ScriptableObject.CreateInstance<EllFamilyData>();
            asset.PopulateDefaultData();

            string resourcesFolderPath = "Assets/Resources/Phonics2/Unit10";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(resourcesFolderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit10");

            string resourcesAssetPath = $"{resourcesFolderPath}/EllFamilyData_Unit10.asset";
            AssetDatabase.CreateAsset(asset, resourcesAssetPath);

            string scriptableFolderPath = "Assets/ScriptableObj/Phonics 2/Unit 10/The Ell Family";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 10/The Ell Family"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj")) AssetDatabase.CreateFolder("Assets", "ScriptableObj");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 10")) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2", "Unit 10");
                if (!AssetDatabase.IsValidFolder(scriptableFolderPath)) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2/Unit 10", "The Ell Family");
            }

            string scriptableAssetPath = $"{scriptableFolderPath}/EllFamilyData_Unit10.asset";
            AssetDatabase.CopyAsset(resourcesAssetPath, scriptableAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created EllFamilyData assets at:\n- {resourcesAssetPath}\n- {scriptableAssetPath}");
        }
    }
}
#endif
