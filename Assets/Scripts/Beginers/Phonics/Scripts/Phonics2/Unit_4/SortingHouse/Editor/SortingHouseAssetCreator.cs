#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit4.Editor
{
    public static class SortingHouseAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create Sorting House Data", false, 44)]
        public static void CreateSortingHouseDataAsset()
        {
            SortingHouseData asset = ScriptableObject.CreateInstance<SortingHouseData>();

            // Setup 25 Sorting Cards (p. 28 list + distractors)
            asset.sortingCards = new SortingWordCardItem[]
            {
                // ă (box 0)
                new SortingWordCardItem { wordName = "cat", correctBoxIndex = 0, isDistractor = false },
                new SortingWordCardItem { wordName = "jam", correctBoxIndex = 0, isDistractor = false },
                new SortingWordCardItem { wordName = "hand", correctBoxIndex = 0, isDistractor = false },

                // ĕ (box 1)
                new SortingWordCardItem { wordName = "best", correctBoxIndex = 1, isDistractor = false },
                new SortingWordCardItem { wordName = "red", correctBoxIndex = 1, isDistractor = false },
                new SortingWordCardItem { wordName = "gem", correctBoxIndex = 1, isDistractor = false },
                new SortingWordCardItem { wordName = "fetch", correctBoxIndex = 1, isDistractor = false },
                new SortingWordCardItem { wordName = "leg", correctBoxIndex = 1, isDistractor = false },

                // ĭ (box 2)
                new SortingWordCardItem { wordName = "fish", correctBoxIndex = 2, isDistractor = false },
                new SortingWordCardItem { wordName = "big", correctBoxIndex = 2, isDistractor = false },
                new SortingWordCardItem { wordName = "six", correctBoxIndex = 2, isDistractor = false },
                new SortingWordCardItem { wordName = "sit", correctBoxIndex = 2, isDistractor = false },
                new SortingWordCardItem { wordName = "ring", correctBoxIndex = 2, isDistractor = false },

                // ŏ (box 3)
                new SortingWordCardItem { wordName = "toss", correctBoxIndex = 3, isDistractor = false },
                new SortingWordCardItem { wordName = "job", correctBoxIndex = 3, isDistractor = false },
                new SortingWordCardItem { wordName = "lost", correctBoxIndex = 3, isDistractor = false },
                new SortingWordCardItem { wordName = "box", correctBoxIndex = 3, isDistractor = false },
                new SortingWordCardItem { wordName = "dog", correctBoxIndex = 3, isDistractor = false },

                // ŭ (box 4)
                new SortingWordCardItem { wordName = "bud", correctBoxIndex = 4, isDistractor = false },
                new SortingWordCardItem { wordName = "hug", correctBoxIndex = 4, isDistractor = false },
                new SortingWordCardItem { wordName = "duck", correctBoxIndex = 4, isDistractor = false },
                new SortingWordCardItem { wordName = "cub", correctBoxIndex = 4, isDistractor = false },

                // Distractors -> "Not today!" (box 5)
                new SortingWordCardItem { wordName = "car", correctBoxIndex = 5, isDistractor = true },
                new SortingWordCardItem { wordName = "ball", correctBoxIndex = 5, isDistractor = true },
                new SortingWordCardItem { wordName = "turn", correctBoxIndex = 5, isDistractor = true }
            };

            // Setup 6 Star Round Challenges with Tara
            asset.starChallenges = new StarRoundUnit4Challenge[]
            {
                // Round 1: Vowel in middle of "sun"?
                new StarRoundUnit4Challenge
                {
                    challengeType = StarChallengeType.VowelMiddleChoice,
                    questionPrompt = "Which vowel is in the middle of 'sun'?",
                    targetWord = "sun",
                    choices = new string[] { "a", "u", "i" },
                    correctChoiceIndex = 1 // 'u'
                },
                // Round 2: Machine turn: p_n -> make "pen"
                new StarRoundUnit4Challenge
                {
                    challengeType = StarChallengeType.WordMachineFill,
                    questionPrompt = "Turn the machine: p_n → make 'pen'!",
                    targetWord = "pen",
                    choices = new string[] { "e", "a", "o" },
                    correctChoiceIndex = 0 // 'e'
                },
                // Round 3: Tap picture for "big"
                new StarRoundUnit4Challenge
                {
                    challengeType = StarChallengeType.PictureTap,
                    questionPrompt = "Tap the picture for 'big'!",
                    targetWord = "big",
                    choices = new string[] { "big", "bag", "bug" },
                    correctChoiceIndex = 0
                },
                // Round 4: Which of these is a real word — mig or mug?
                new StarRoundUnit4Challenge
                {
                    challengeType = StarChallengeType.RealVsSillyChoice,
                    questionPrompt = "Which of these is a real word — mig or mug?",
                    targetWord = "mug",
                    choices = new string[] { "mig", "mug" },
                    correctChoiceIndex = 1 // 'mug'
                },
                // Round 5: Sort three quick cards
                new StarRoundUnit4Challenge
                {
                    challengeType = StarChallengeType.QuickSortDrag,
                    questionPrompt = "Sort three quick cards into their letterboxes!",
                    choices = new string[] { "cat", "red", "pig" },
                    quickDragCards = new SortingWordCardItem[]
                    {
                        new SortingWordCardItem { wordName = "cat", correctBoxIndex = 0 },
                        new SortingWordCardItem { wordName = "red", correctBoxIndex = 1 },
                        new SortingWordCardItem { wordName = "pig", correctBoxIndex = 2 }
                    }
                },
                // Round 6: Sing five short vowels
                new StarRoundUnit4Challenge
                {
                    challengeType = StarChallengeType.VowelSongRecap,
                    questionPrompt = "Sing the five short vowels with Tara! ă, ĕ, ĭ, ŏ, ŭ!",
                    choices = new string[] { "ă", "ĕ", "ĭ", "ŏ", "ŭ" },
                    correctChoiceIndex = 0
                }
            };

            string path = "Assets/Resources/Phonics2/Unit4/SortingHouseData_Unit4.asset";
            System.IO.Directory.CreateDirectory("Assets/Resources/Phonics2/Unit4");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"Created Sorting House Data Asset at {path}");
        }
    }
}
#endif
