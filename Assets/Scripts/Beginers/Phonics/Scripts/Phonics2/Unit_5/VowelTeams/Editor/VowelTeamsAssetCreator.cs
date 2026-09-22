#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit5.Editor
{
    public static class VowelTeamsAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create Vowel Teams Data (Unit 5)", false, 53)]
        public static void CreateVowelTeamsAsset()
        {
            VowelTeamsData asset = ScriptableObject.CreateInstance<VowelTeamsData>();

            // Setup 4 Vowel Teams
            asset.vowelTeams = new VowelTeamItem[4];
            string[] names = new string[] { "ee", "ea", "oa", "ai" };
            string[] sounds = new string[] { "ee", "ee", "oh", "ay" };
            string[][] picWords = new string[][]
            {
                new string[] { "bee", "feet", "sheep", "seat" },
                new string[] { "leaf", "meat", "seal", "beach" },
                new string[] { "boat", "coat", "toast", "road" },
                new string[] { "rain", "train", "snail", "paint" }
            };

            for (int i = 0; i < 4; i++)
            {
                asset.vowelTeams[i] = new VowelTeamItem
                {
                    teamName = names[i],
                    teamSound = sounds[i],
                    pictureWordNames = picWords[i]
                };
            }

            // Setup 6 Team Spotting Words
            asset.spottingWords = new VowelTeamSpottingWord[6];
            asset.spottingWords[0] = new VowelTeamSpottingWord { wordText = "sheep", correctTeamLetters = "ee", teamStartIndex = 2, teamLength = 2 };
            asset.spottingWords[1] = new VowelTeamSpottingWord { wordText = "leaf", correctTeamLetters = "ea", teamStartIndex = 1, teamLength = 2 };
            asset.spottingWords[2] = new VowelTeamSpottingWord { wordText = "boat", correctTeamLetters = "oa", teamStartIndex = 1, teamLength = 2 };
            asset.spottingWords[3] = new VowelTeamSpottingWord { wordText = "rain", correctTeamLetters = "ai", teamStartIndex = 1, teamLength = 2 };
            asset.spottingWords[4] = new VowelTeamSpottingWord { wordText = "feet", correctTeamLetters = "ee", teamStartIndex = 1, teamLength = 2 };
            asset.spottingWords[5] = new VowelTeamSpottingWord { wordText = "coat", correctTeamLetters = "oa", teamStartIndex = 1, teamLength = 2 };

            // Setup Word Wall Families (Book pp. 31, 33)
            asset.wordWallFamilies = new VowelTeamWordFamily[]
            {
                new VowelTeamWordFamily
                {
                    familyName = "-eat / -eak",
                    familyDescription = "Long E team: -eat and -eak words!",
                    words = new string[] { "eat", "beat", "beak", "leak", "weak" }
                },
                new VowelTeamWordFamily
                {
                    familyName = "-ee- / -ea-",
                    familyDescription = "Long E team: ee and ea words!",
                    words = new string[] { "sheep", "feet", "seat", "meat", "mean", "bean", "seal", "meal", "leaf" }
                },
                new VowelTeamWordFamily
                {
                    familyName = "-oa-",
                    familyDescription = "Long O team: oa words!",
                    words = new string[] { "boat", "coat", "toast", "road" }
                },
                new VowelTeamWordFamily
                {
                    familyName = "Magic E Reminder",
                    familyDescription = "Magic E reminder: Long O words can also use magic e!",
                    words = new string[] { "rose", "cone", "rope", "stone", "phone", "alone" }
                }
            };

            // Setup Flat Word Wall List
            asset.vowelTeamsWordWallList = new string[]
            {
                "eat", "beat", "beak", "leak", "weak",
                "sheep", "feet", "seat", "meat", "mean", "bean", "seal", "meal", "leaf",
                "boat", "coat", "toast", "road",
                "rose", "cone", "rope", "stone", "phone", "alone"
            };

            string resourcesFolderPath = "Assets/Resources/Phonics2/Unit5";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(resourcesFolderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit5");

            string resourcesAssetPath = $"{resourcesFolderPath}/VowelTeamsData_Unit5.asset";
            AssetDatabase.CreateAsset(asset, resourcesAssetPath);

            // Also ensure copy in ScriptableObj folder
            string scriptableFolderPath = "Assets/ScriptableObj/Phonics 2/Unit 5/Vowel Teams";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 5/Vowel Teams"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj")) AssetDatabase.CreateFolder("Assets", "ScriptableObj");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 5")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2/Unit 5");
                if (!AssetDatabase.IsValidFolder(scriptableFolderPath)) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2/Unit 5", "Vowel Teams");
            }

            string scriptableAssetPath = $"{scriptableFolderPath}/VowelTeamsData_Unit5.asset";
            AssetDatabase.CopyAsset(resourcesAssetPath, scriptableAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created VowelTeamsData assets at:\n- {resourcesAssetPath}\n- {scriptableAssetPath}");
        }
    }
}
#endif
