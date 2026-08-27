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

            for (int i = 0; i < 9; i++)
            {
                asset.worksheetItems[i] = new PlayTimeWorksheetItem
                {
                    wordWithGap = gaps[i],
                    fullWordText = fullWords[i],
                    correctSpellingTile = correctTiles[i],
                    tileOptions = tileOptions[i]
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

            string folderPath = "Assets/Resources/Phonics2/Unit5";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(folderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit5");

            string assetPath = $"{folderPath}/LongVowelPlayTimeData_Unit5.asset";
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created LongVowelPlayTimeData asset at: {assetPath}");
        }
    }
}
#endif
