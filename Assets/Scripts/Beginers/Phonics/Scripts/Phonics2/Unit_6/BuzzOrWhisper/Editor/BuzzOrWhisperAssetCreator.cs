#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit6.Editor
{
    public static class BuzzOrWhisperAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create Buzz Or Whisper Data (Unit 6 Stop 1)", false, 61)]
        public static void CreateBuzzOrWhisperAsset()
        {
            BuzzOrWhisperData asset = ScriptableObject.CreateInstance<BuzzOrWhisperData>();

            // Setup 10 Sorting Sounds (/b/, /p/, /d/, /t/, /g/, /k/, /v/, /f/, /z/, /s/)
            string[] symbols = new string[] { "/b/", "/p/", "/d/", "/t/", "/g/", "/k/", "/v/", "/f/", "/z/", "/s/" };
            string[] letters = new string[] { "B", "P", "D", "T", "G", "K", "V", "F", "Z", "S" };
            bool[] isBuzzer = new bool[] { true, false, true, false, true, false, true, false, true, false };

            asset.sortingSounds = new BuzzSoundItem[10];
            for (int i = 0; i < 10; i++)
            {
                asset.sortingSounds[i] = new BuzzSoundItem
                {
                    soundSymbol = symbols[i],
                    letterName = letters[i],
                    isBuzzer = isBuzzer[i]
                };
            }

            asset.freePlaySounds = new BuzzSoundItem[10];
            for (int i = 0; i < 10; i++)
            {
                asset.freePlaySounds[i] = new BuzzSoundItem
                {
                    soundSymbol = symbols[i],
                    letterName = letters[i],
                    isBuzzer = isBuzzer[i]
                };
            }

            string resourcesFolderPath = "Assets/Resources/Phonics2/Unit6";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(resourcesFolderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit6");

            string resourcesAssetPath = $"{resourcesFolderPath}/BuzzOrWhisperData_Unit6.asset";
            AssetDatabase.CreateAsset(asset, resourcesAssetPath);

            string scriptableFolderPath = "Assets/ScriptableObj/Phonics 2/Unit 6/Buzz Or Whisper";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 6/Buzz Or Whisper"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj")) AssetDatabase.CreateFolder("Assets", "ScriptableObj");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 6")) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2", "Unit 6");
                if (!AssetDatabase.IsValidFolder(scriptableFolderPath)) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2/Unit 6", "Buzz Or Whisper");
            }

            string scriptableAssetPath = $"{scriptableFolderPath}/BuzzOrWhisperData_Unit6.asset";
            AssetDatabase.CopyAsset(resourcesAssetPath, scriptableAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created BuzzOrWhisperData assets at:\n- {resourcesAssetPath}\n- {scriptableAssetPath}");
        }
    }
}
#endif
