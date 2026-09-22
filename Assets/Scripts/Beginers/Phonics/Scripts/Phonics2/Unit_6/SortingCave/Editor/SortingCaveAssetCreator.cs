#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit6.Editor
{
    public static class SortingCaveAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create Sorting Cave Data (Unit 6 Stop 4)", false, 64)]
        public static void CreateSortingCaveAsset()
        {
            SortingCaveData asset = ScriptableObject.CreateInstance<SortingCaveData>();

            // 12 Session Sorting Sound Cards (drawn evenly from the book's 24 consonants)
            asset.sessionCards = new ConsonantSoundCardItem[12];
            string[] symbols = new string[] { "/b/", "/p/", "/d/", "/t/", "/g/", "/k/", "/v/", "/f/", "/z/", "/s/", "/sh/", "/th/" };
            string[] words = new string[] { "bat", "pen", "dog", "tall", "girl", "cap", "van", "fan", "zoo", "sun", "shore", "this" };
            bool[] isBuzzer = new bool[] { true, false, true, false, true, false, true, false, true, false, false, true };

            for (int i = 0; i < 12; i++)
            {
                asset.sessionCards[i] = new ConsonantSoundCardItem
                {
                    soundSymbol = symbols[i],
                    wordName = words[i],
                    isBuzzer = isBuzzer[i]
                };
            }

            // Setup 6 Star Round Challenges with Tara
            asset.starChallenges = new StarRoundUnit6Challenge[6];

            // 1. BuzzOrWhisperIdentify
            asset.starChallenges[0] = new StarRoundUnit6Challenge
            {
                challengeType = StarChallengeTypeUnit6.BuzzOrWhisperIdentify,
                questionPrompt = "Does /g/ buzz, or whisper?",
                choices = new string[] { "Buzz", "Whisper", "Neither" },
                correctChoiceIndex = 0
            };

            // 2. MinimalPairHear
            asset.starChallenges[1] = new StarRoundUnit6Challenge
            {
                challengeType = StarChallengeTypeUnit6.MinimalPairHear,
                questionPrompt = "Bear, or pear — which did you hear?",
                choices = new string[] { "bear", "pear", "cap" },
                correctChoiceIndex = 1
            };

            // 3. TeamSpottingInWord
            asset.starChallenges[2] = new StarRoundUnit6Challenge
            {
                challengeType = StarChallengeTypeUnit6.TeamSpottingInWord,
                questionPrompt = "Tap the team in 'ship'!",
                choices = new string[] { "sh", "ip", "hi" },
                correctChoiceIndex = 0
            };

            // 4. TwinRoleIdentify
            asset.starChallenges[3] = new StarRoundUnit6Challenge
            {
                challengeType = StarChallengeTypeUnit6.TwinRoleIdentify,
                questionPrompt = "Is /f/ the buzzer twin or the whisper twin?",
                choices = new string[] { "Buzzer", "Whisperer", "Both" },
                correctChoiceIndex = 1
            };

            // 5. TeamPositionRule
            asset.starChallenges[4] = new StarRoundUnit6Challenge
            {
                challengeType = StarChallengeTypeUnit6.TeamPositionRule,
                questionPrompt = "Does ng live at the start or the end of words?",
                choices = new string[] { "Start", "End", "Middle" },
                correctChoiceIndex = 1
            };

            // 6. ThBuzzCheckChoice
            asset.starChallenges[5] = new StarRoundUnit6Challenge
            {
                challengeType = StarChallengeTypeUnit6.ThBuzzCheckChoice,
                questionPrompt = "Hand on your throat — is 'this' a buzz or a whisper?",
                choices = new string[] { "Buzz", "Whisper", "Neither" },
                correctChoiceIndex = 0
            };

            string resourcesFolderPath = "Assets/Resources/Phonics2/Unit6";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(resourcesFolderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit6");

            string resourcesAssetPath = $"{resourcesFolderPath}/SortingCaveData_Unit6.asset";
            AssetDatabase.CreateAsset(asset, resourcesAssetPath);

            string scriptableFolderPath = "Assets/ScriptableObj/Phonics 2/Unit 6/Sorting Cave";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 6/Sorting Cave"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj")) AssetDatabase.CreateFolder("Assets", "ScriptableObj");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 6")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2/Unit 6");
                if (!AssetDatabase.IsValidFolder(scriptableFolderPath)) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2/Unit 6", "Sorting Cave");
            }

            string scriptableAssetPath = $"{scriptableFolderPath}/SortingCaveData_Unit6.asset";
            AssetDatabase.CopyAsset(resourcesAssetPath, scriptableAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created SortingCaveData assets at:\n- {resourcesAssetPath}\n- {scriptableAssetPath}");
        }
    }
}
#endif
