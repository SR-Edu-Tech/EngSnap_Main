#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace EngSnap.Phonics2.Unit4.Editor
{
    public static class VowelSwapMachineAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Unit 4/Create Vowel Swap Machine Data Asset")]
        public static void CreateVowelSwapMachineDataAsset()
        {
            string folderPath = "Assets/ScriptableObjects/Phonics2/Unit4/VowelSwapMachine";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            string assetPath = Path.Combine(folderPath, "VowelSwapMachineData_Unit4.asset");

            VowelSwapMachineData asset = AssetDatabase.LoadAssetAtPath<VowelSwapMachineData>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<VowelSwapMachineData>();
                asset.PopulatePage22DefaultData();
                AssetDatabase.CreateAsset(asset, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[EngSnap] Created VowelSwapMachineData asset at {assetPath} with Page 22 default values.");
            }
            else
            {
                asset.PopulatePage22DefaultData();
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                Debug.Log($"[EngSnap] Updated existing VowelSwapMachineData asset at {assetPath} with Page 22 default values.");
            }

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
    }
}
#endif
