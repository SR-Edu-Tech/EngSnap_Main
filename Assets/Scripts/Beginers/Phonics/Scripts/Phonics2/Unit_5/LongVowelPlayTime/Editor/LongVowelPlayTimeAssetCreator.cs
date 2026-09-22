#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit5.Editor
{
    public static class LongVowelPlayTimeAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create Play Time Data (Unit 5)", false, 54)]
        public static void CreatePlayTimeAsset()
        {
            LongVowelPlayTimeData asset = ScriptableObject.CreateInstance<LongVowelPlayTimeData>();

            // Setup 9 Gap Worksheet Items (p.35)
            asset.worksheetItems = new PlayTimeWorksheetItem[9];
            string[] gaps = new string[] { "tr_ _", "l_ _f", "k_te", "t_ger", "b_ne", "pl_ne", "B_ _r", "wh_le", "l_ _n" };
            string[] fullWords = new string[] { "tree", "leaf", "kite", "tiger", "bone", "plane", "bear", "whale", "lion" };
            string[] correctTiles = new string[] { "ee", "ea", "i", "i", "o", "a", "ea", "a", "i" };
            string[][] tileOptions = new string[][]
            {
                new string[] { "ee", "ea", "i" },
                new string[] { "ea", "ee", "a" },
                new string[] { "i", "e", "a" },
                new string[] { "i", "y", "e" },
                new string[] { "o", "oa", "u" },
                new string[] { "a", "ai", "e" },
                new string[] { "ea", "ee", "ai" },
                new string[] { "a", "ai", "e" },
                new string[] { "i", "y", "ee" }
            };

            Vector2[][] allCheckpoints = new Vector2[][]
            {
                // 1. tree ("ee") - 8 checkpoints
                new Vector2[]
                {
                    new Vector2(-80f, 0f), new Vector2(-60f, 35f), new Vector2(-40f, 0f), new Vector2(-60f, -35f),
                    new Vector2( 20f, 0f), new Vector2( 40f, 35f), new Vector2( 60f, 0f), new Vector2( 40f, -35f)
                },
                // 2. leaf ("ea") - 8 checkpoints
                new Vector2[]
                {
                    new Vector2(-80f, 0f), new Vector2(-60f, 35f), new Vector2(-40f, 0f), new Vector2(-60f, -35f),
                    new Vector2( 40f, 35f), new Vector2( 20f, 0f), new Vector2( 40f, -35f), new Vector2( 60f, -35f)
                },
                // 3. kite ("i") - 3 checkpoints
                new Vector2[]
                {
                    new Vector2(0f, 40f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, -40f)
                },
                // 4. tiger ("i") - 3 checkpoints
                new Vector2[]
                {
                    new Vector2(0f, 40f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, -40f)
                },
                // 5. bone ("o") - 5 checkpoints
                new Vector2[]
                {
                    new Vector2(0f, 45f),
                    new Vector2(-45f, 0f),
                    new Vector2(0f, -45f),
                    new Vector2(45f, 0f),
                    new Vector2(0f, 45f)
                },
                // 6. plane ("a") - 5 checkpoints
                new Vector2[]
                {
                    new Vector2(30f, 40f),
                    new Vector2(-30f, 20f),
                    new Vector2(-30f, -30f),
                    new Vector2(30f, -30f),
                    new Vector2(30f, -50f)
                },
                // 7. bear ("ea") - 8 checkpoints
                new Vector2[]
                {
                    new Vector2(-80f, 0f), new Vector2(-60f, 35f), new Vector2(-40f, 0f), new Vector2(-60f, -35f),
                    new Vector2( 40f, 35f), new Vector2( 20f, 0f), new Vector2( 40f, -35f), new Vector2( 60f, -35f)
                },
                // 8. whale ("a") - 5 checkpoints
                new Vector2[]
                {
                    new Vector2(30f, 40f),
                    new Vector2(-30f, 20f),
                    new Vector2(-30f, -30f),
                    new Vector2(30f, -30f),
                    new Vector2(30f, -50f)
                },
                // 9. lion ("i") - 3 checkpoints
                new Vector2[]
                {
                    new Vector2(0f, 40f),
                    new Vector2(0f, 0f),
                    new Vector2(0f, -40f)
                }
            };

            for (int i = 0; i < 9; i++)
            {
                asset.worksheetItems[i] = new PlayTimeWorksheetItem
                {
                    wordWithGap = gaps[i],
                    fullWordText = fullWords[i],
                    correctSpellingTile = correctTiles[i],
                    tileOptions = tileOptions[i],
                    checkpointPositions = allCheckpoints[i]
                };
            }

            // Setup 6 Star Round Challenges
            asset.starChallenges = new StarRoundUnit5Challenge[6];

            // 1. NameSayersChoice
            asset.starChallenges[0] = new StarRoundUnit5Challenge
            {
                challengeType = StarChallengeTypeUnit5.NameSayersChoice,
                questionPrompt = "Cat, or cake — which one said its NAME?",
                choices = new string[] { "cat", "cake", "cap" },
                correctChoiceIndex = 1
            };

            // 2. MagicETransform
            asset.starChallenges[1] = new StarRoundUnit5Challenge
            {
                challengeType = StarChallengeTypeUnit5.MagicETransform,
                questionPrompt = "Cast magic e on 'tub' — what word does it make?",
                choices = new string[] { "tub", "tube", "tab" },
                correctChoiceIndex = 1
            };

            // 3. VowelTeamSpotting
            asset.starChallenges[2] = new StarRoundUnit5Challenge
            {
                challengeType = StarChallengeTypeUnit5.VowelTeamSpotting,
                questionPrompt = "Which two letters are a team in 'boat'?",
                choices = new string[] { "oa", "ee", "ai" },
                correctChoiceIndex = 0
            };

            // 4. HatSwapChoice
            asset.starChallenges[3] = new StarRoundUnit5Challenge
            {
                challengeType = StarChallengeTypeUnit5.HatSwapChoice,
                questionPrompt = "Put the right hat on the i in 'bike'. Is it flat or curved?",
                choices = new string[] { "Flat (macron)", "Curved (breve)" },
                correctChoiceIndex = 0
            };

            // 5. PictureTapChoice
            asset.starChallenges[4] = new StarRoundUnit5Challenge
            {
                challengeType = StarChallengeTypeUnit5.PictureTapChoice,
                questionPrompt = "Tap the picture for 'seat'!",
                choices = new string[] { "seat", "seal", "sheep" },
                correctChoiceIndex = 0
            };

            // 6. ShortVsLongIdentify
            asset.starChallenges[5] = new StarRoundUnit5Challenge
            {
                challengeType = StarChallengeTypeUnit5.ShortVsLongIdentify,
                questionPrompt = "Is 'hop' short or long?",
                choices = new string[] { "Short", "Long" },
                correctChoiceIndex = 0
            };

            string resourcesFolderPath = "Assets/Resources/Phonics2/Unit5";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(resourcesFolderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit5");

            string resourcesAssetPath = $"{resourcesFolderPath}/LongVowelPlayTimeData_Unit5.asset";
            AssetDatabase.CreateAsset(asset, resourcesAssetPath);

            // Also ensure copy in ScriptableObj folder
            string scriptableFolderPath = "Assets/ScriptableObj/Phonics 2/Unit 5/Play Time";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 5/Play Time"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj")) AssetDatabase.CreateFolder("Assets", "ScriptableObj");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 5")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2/Unit 5");
                if (!AssetDatabase.IsValidFolder(scriptableFolderPath)) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2/Unit 5", "Play Time");
            }

            string scriptableAssetPath = $"{scriptableFolderPath}/LongVowelPlayTimeData_Unit5.asset";
            AssetDatabase.CopyAsset(resourcesAssetPath, scriptableAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created LongVowelPlayTimeData assets at:\n- {resourcesAssetPath}\n- {scriptableAssetPath}");
        }
    }
}
#endif
