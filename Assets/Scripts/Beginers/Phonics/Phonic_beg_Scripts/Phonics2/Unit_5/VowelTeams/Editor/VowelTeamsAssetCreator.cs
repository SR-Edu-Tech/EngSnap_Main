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

            // Setup Word Wall List
            asset.vowelTeamsWordWallList = new string[]
            {
                "eat", "beat", "beak", "leak", "weak", "sheep", "feet",
                "seat", "meat", "mean", "bean", "seal", "meal", "leaf",
                "boat", "coat", "toast", "road", "rose", "cone", "rope",
                "stone", "phone", "alone"
            };

            string folderPath = "Assets/Resources/Phonics2/Unit5";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(folderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit5");

            string assetPath = $"{folderPath}/VowelTeamsData_Unit5.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created VowelTeamsData asset at: {assetPath}");
        }
    }
}
#endif
