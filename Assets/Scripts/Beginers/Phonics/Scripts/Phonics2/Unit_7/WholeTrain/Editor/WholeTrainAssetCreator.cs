#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit7.Editor
{
    public static class WholeTrainAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create Whole Train Data (Unit 7 Stop 4)", false, 74)]
        public static void CreateWholeTrainAsset()
        {
            WholeTrainData asset = ScriptableObject.CreateInstance<WholeTrainData>();

            // Setup 8 CVC Train Words: cat, fan, cup, net, bag, pig, dog, hat
            asset.wholeTrainItems = new WholeTrainWordItem[8];
            string[] words = new string[] { "cat", "fan", "cup", "net", "bag", "pig", "dog", "hat" };
            string[] firsts = new string[] { "c", "f", "c", "n", "b", "p", "d", "h" };
            string[] vowels = new string[] { "a", "a", "u", "e", "a", "i", "o", "a" };
            string[] lasts = new string[] { "t", "n", "p", "t", "g", "g", "g", "t" };

            string[][] frontOpts = new string[][]
            {
                new string[] { "c", "k", "b", "p" },
                new string[] { "f", "v", "s", "b" },
                new string[] { "c", "k", "t", "d" },
                new string[] { "n", "m", "h", "b" },
                new string[] { "b", "d", "p", "g" },
                new string[] { "p", "b", "t", "d" },
                new string[] { "d", "b", "p", "t" },
                new string[] { "h", "n", "m", "w" }
            };

            string[][] middleOpts = new string[][]
            {
                new string[] { "a", "e", "i", "o", "u" },
                new string[] { "a", "e", "i", "o", "u" },
                new string[] { "a", "e", "i", "o", "u" },
                new string[] { "a", "e", "i", "o", "u" },
                new string[] { "a", "e", "i", "o", "u" },
                new string[] { "a", "e", "i", "o", "u" },
                new string[] { "a", "e", "i", "o", "u" },
                new string[] { "a", "e", "i", "o", "u" }
            };

            string[][] backOpts = new string[][]
            {
                new string[] { "t", "d", "p", "g" },
                new string[] { "n", "m", "t", "d" },
                new string[] { "p", "b", "t", "d" },
                new string[] { "t", "d", "n", "m" },
                new string[] { "g", "k", "d", "t" },
                new string[] { "g", "k", "b", "p" },
                new string[] { "g", "k", "d", "t" },
                new string[] { "t", "d", "p", "b" }
            };

            for (int i = 0; i < 8; i++)
            {
                asset.wholeTrainItems[i] = new WholeTrainWordItem
                {
                    fullWord = words[i],
                    firstLetter = firsts[i],
                    middleLetter = vowels[i],
                    lastLetter = lasts[i],
                    frontOptions = frontOpts[i],
                    middleOptions = middleOpts[i],
                    backOptions = backOpts[i]
                };
            }

            // Setup 6 Tara Star Round Challenges
            asset.starChallenges = new TrainStarChallengeItem[6];

            // 1. First sound in "goat"?
            asset.starChallenges[0] = new TrainStarChallengeItem
            {
                challengeType = StarChallengeType.FirstSound,
                questionPrompt = "First sound in 'goat'?",
                testWord = "goat",
                choiceTexts = new string[] { "g", "d", "b" },
                correctChoiceIndex = 0,
                correctLetter = "g"
            };

            // 2. Last sound in "crab"?
            asset.starChallenges[1] = new TrainStarChallengeItem
            {
                challengeType = StarChallengeType.LastSound,
                questionPrompt = "Last sound in 'crab'?",
                testWord = "crab",
                choiceTexts = new string[] { "b", "p", "d" },
                correctChoiceIndex = 0,
                correctLetter = "b"
            };

            // 3. Middle sound in "lock"?
            asset.starChallenges[2] = new TrainStarChallengeItem
            {
                challengeType = StarChallengeType.MiddleVowel,
                questionPrompt = "Middle sound in 'lock'?",
                testWord = "lock",
                choiceTexts = new string[] { "o", "u", "a" },
                correctChoiceIndex = 0,
                correctLetter = "o"
            };

            // 4. Is /p/ at the front or back of "cup"?
            asset.starChallenges[3] = new TrainStarChallengeItem
            {
                challengeType = StarChallengeType.FrontOrBackPosition,
                questionPrompt = "Is /p/ at the front or back of 'cup'?",
                testWord = "cup",
                choiceTexts = new string[] { "Front", "Back" },
                correctChoiceIndex = 1,
                correctLetter = "Back"
            };

            // 5. Fill the whole train for "bed"
            asset.starChallenges[4] = new TrainStarChallengeItem
            {
                challengeType = StarChallengeType.WholeWordFill,
                questionPrompt = "Which word completes the train for 'b - e - d'?",
                testWord = "bed",
                choiceTexts = new string[] { "bed", "bad", "bid" },
                correctChoiceIndex = 0,
                correctLetter = "bed"
            };

            // 6. Trace the missing letter in "d _ g"
            asset.starChallenges[5] = new TrainStarChallengeItem
            {
                challengeType = StarChallengeType.MissingLetterTrace,
                questionPrompt = "Trace the missing letter in 'd _ g'!",
                testWord = "dog",
                choiceTexts = new string[] { "o" },
                correctChoiceIndex = 0,
                correctLetter = "o",
                traceCheckpoints = new Vector2[]
                {
                    new Vector2(0f, 40f),
                    new Vector2(-30f, 0f),
                    new Vector2(0f, -40f),
                    new Vector2(30f, 0f),
                    new Vector2(0f, 40f)
                }
            };

            string resourcesFolderPath = "Assets/Resources/Phonics2/Unit7";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(resourcesFolderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit7");

            string resourcesAssetPath = $"{resourcesFolderPath}/WholeTrainData_Unit7.asset";
            AssetDatabase.CreateAsset(asset, resourcesAssetPath);

            string scriptableFolderPath = "Assets/ScriptableObj/Phonics 2/Unit 7/Whole Train";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 7/Whole Train"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj")) AssetDatabase.CreateFolder("Assets", "ScriptableObj");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 7")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2/Unit 7");
                if (!AssetDatabase.IsValidFolder(scriptableFolderPath)) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2/Unit 7", "Whole Train");
            }

            string scriptableAssetPath = $"{scriptableFolderPath}/WholeTrainData_Unit7.asset";
            AssetDatabase.CopyAsset(resourcesAssetPath, scriptableAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created WholeTrainData assets at:\n- {resourcesAssetPath}\n- {scriptableAssetPath}");
        }
    }
}
#endif
