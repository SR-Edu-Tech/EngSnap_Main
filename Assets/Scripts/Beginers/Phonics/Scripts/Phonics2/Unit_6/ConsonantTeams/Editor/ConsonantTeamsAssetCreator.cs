#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit6.Editor
{
    public static class ConsonantTeamsAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create Consonant Teams Data (Unit 6 Stop 3)", false, 63)]
        public static void CreateConsonantTeamsAsset()
        {
            ConsonantTeamsData asset = ScriptableObject.CreateInstance<ConsonantTeamsData>();

            // Setup 4 Consonant Teams (sh, ch, th, ng)
            asset.teams = new ConsonantTeamItem[4];
            string[] teamL = new string[] { "sh", "ch", "th", "ng" };
            string[] teamS = new string[] { "/sh/", "/ch/", "/th/", "/ng/" };
            string[] hooks = new string[]
            {
                "Finger on your lips — shhhhh!",
                "Like a train — ch, ch, ch!",
                "Poke your tongue out a little bit!",
                "Lives at the END of words — Siiing!"
            };
            string[] exWords = new string[] { "ship", "chips", "thin", "sing" };

            for (int i = 0; i < 4; i++)
            {
                asset.teams[i] = new ConsonantTeamItem
                {
                    teamLetters = teamL[i],
                    teamSound = teamS[i],
                    hookPhrase = hooks[i],
                    exampleWord = exWords[i]
                };
            }

            // Setup 6 Position Sorting Words
            asset.positionWords = new PositionSortWordItem[6];
            string[] pWords = new string[] { "ship", "sing", "chips", "ring", "thin", "long" };
            string[] pTeams = new string[] { "sh", "ng", "ch", "ng", "th", "ng" };
            TeamPositionType[] pTypes = new TeamPositionType[]
            {
                TeamPositionType.StartsWith,
                TeamPositionType.EndsWith,
                TeamPositionType.StartsWith,
                TeamPositionType.EndsWith,
                TeamPositionType.StartsWith,
                TeamPositionType.EndsWith
            };

            for (int i = 0; i < 6; i++)
            {
                asset.positionWords[i] = new PositionSortWordItem
                {
                    wordText = pWords[i],
                    teamLetters = pTeams[i],
                    positionType = pTypes[i]
                };
            }

            // Setup 8 Team Hunt Words
            asset.teamHuntWords = new TeamHuntWordItem[8];
            string[] huntWords = new string[] { "chips", "chair", "ship", "shore", "shell", "thin", "moth", "sing" };
            string[] huntTeams = new string[] { "ch", "ch", "sh", "sh", "sh", "th", "th", "ng" };
            int[] huntStarts = new int[] { 0, 0, 0, 0, 0, 0, 2, 1 }; // index where team begins in word

            for (int i = 0; i < 8; i++)
            {
                asset.teamHuntWords[i] = new TeamHuntWordItem
                {
                    fullWord = huntWords[i],
                    teamLetters = huntTeams[i],
                    teamStartIndex = huntStarts[i]
                };
            }

            // Setup 2 TH Check Words
            asset.thCheckWords = new ThBuzzCheckItem[2];
            asset.thCheckWords[0] = new ThBuzzCheckItem { wordText = "thin", isBuzzer = false };
            asset.thCheckWords[1] = new ThBuzzCheckItem { wordText = "this", isBuzzer = true };

            string resourcesFolderPath = "Assets/Resources/Phonics2/Unit6";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(resourcesFolderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit6");

            string resourcesAssetPath = $"{resourcesFolderPath}/ConsonantTeamsData_Unit6.asset";
            AssetDatabase.CreateAsset(asset, resourcesAssetPath);

            string scriptableFolderPath = "Assets/ScriptableObj/Phonics 2/Unit 6/Consonant Teams";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 6/Consonant Teams"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj")) AssetDatabase.CreateFolder("Assets", "ScriptableObj");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 6")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2/Unit 6");
                if (!AssetDatabase.IsValidFolder(scriptableFolderPath)) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2/Unit 6", "Consonant Teams");
            }

            string scriptableAssetPath = $"{scriptableFolderPath}/ConsonantTeamsData_Unit6.asset";
            AssetDatabase.CopyAsset(resourcesAssetPath, scriptableAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created ConsonantTeamsData assets at:\n- {resourcesAssetPath}\n- {scriptableAssetPath}");
        }
    }
}
#endif
