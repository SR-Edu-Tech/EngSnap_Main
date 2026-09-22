#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit6.Editor
{
    public static class SoundTwinsAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create Sound Twins Data (Unit 6 Stop 2)", false, 62)]
        public static void CreateSoundTwinsAsset()
        {
            SoundTwinsData asset = ScriptableObject.CreateInstance<SoundTwinsData>();

            // Setup 6 Twin Pairs (b/p, d/t, g/k, v/f, z/s, th/th)
            asset.twinPairs = new SoundTwinPairItem[6];
            string[] pairNames = new string[] { "b / p", "d / t", "g / k", "v / f", "z / s", "th / th" };
            string[] buzzers = new string[] { "b", "d", "g", "v", "z", "th (voiced)" };
            string[] whisperers = new string[] { "p", "t", "k", "f", "s", "th (unvoiced)" };
            string[] mouthDescs = new string[]
            {
                "Lips pressed together",
                "Tongue tap on roof",
                "Back of tongue against throat",
                "Top teeth on bottom lip",
                "Teeth together",
                "Tongue peeking between teeth"
            };

            for (int i = 0; i < 6; i++)
            {
                asset.twinPairs[i] = new SoundTwinPairItem
                {
                    pairName = pairNames[i],
                    buzzerLetter = buzzers[i],
                    whispererLetter = whisperers[i],
                    mouthDescription = mouthDescs[i]
                };
            }

            // Setup 8 Minimal Pairs (bear/pear, goat/coat, van/fan, zip/sip, dime/time, bat/pat, buzz/bus, thin/this)
            asset.minimalPairs = new MinimalPairQuizItem[8];
            string[] targets = new string[] { "pear", "goat", "fan", "zip", "time", "bat", "bus", "this" };
            bool[] isWhisper = new bool[] { true, false, true, false, true, false, true, false };
            string[] wordsA = new string[] { "bear", "goat", "van", "zip", "dime", "bat", "buzz", "thin" };
            string[] wordsB = new string[] { "pear", "coat", "fan", "sip", "time", "pat", "bus", "this" };
            int[] correctIndices = new int[] { 1, 0, 1, 0, 1, 0, 1, 1 };

            for (int i = 0; i < 8; i++)
            {
                asset.minimalPairs[i] = new MinimalPairQuizItem
                {
                    targetWord = targets[i],
                    targetIsWhisperer = isWhisper[i],
                    choiceWordA = wordsA[i],
                    choiceWordB = wordsB[i],
                    correctChoiceIndex = correctIndices[i]
                };
            }

            // Setup 4 Twin Trouble Gap Items
            asset.twinTroubleItems = new TwinTroubleGapItem[4];
            string[] gapDisplays = new string[] { "_ear", "_oat", "_an", "_ip" };
            string[] correctL = new string[] { "b", "g", "v", "z" };
            string[] distractorL = new string[] { "p", "k", "f", "s" };
            string[] fullW = new string[] { "bear", "goat", "van", "zip" };

            for (int i = 0; i < 4; i++)
            {
                asset.twinTroubleItems[i] = new TwinTroubleGapItem
                {
                    displayGapWord = gapDisplays[i],
                    correctLetter = correctL[i],
                    distractorLetter = distractorL[i],
                    fullWord = fullW[i]
                };
            }

            // Setup 4 V vs W Practice Items
            asset.vVsWItems = new VvsWItem[4];
            string[] wordsV = new string[] { "vet", "van", "vine", "vest" };
            string[] wordsW = new string[] { "wet", "wall", "wine", "west" };
            int[] targetsVW = new int[] { 0, 1, 0, 1 };

            for (int i = 0; i < 4; i++)
            {
                asset.vVsWItems[i] = new VvsWItem
                {
                    wordV = wordsV[i],
                    wordW = wordsW[i],
                    targetIndex = targetsVW[i]
                };
            }

            string resourcesFolderPath = "Assets/Resources/Phonics2/Unit6";
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder("Assets/Resources/Phonics2")) AssetDatabase.CreateFolder("Assets/Resources", "Phonics2");
            if (!AssetDatabase.IsValidFolder(resourcesFolderPath)) AssetDatabase.CreateFolder("Assets/Resources/Phonics2", "Unit6");

            string resourcesAssetPath = $"{resourcesFolderPath}/SoundTwinsData_Unit6.asset";
            AssetDatabase.CreateAsset(asset, resourcesAssetPath);

            string scriptableFolderPath = "Assets/ScriptableObj/Phonics 2/Unit 6/Sound Twins";
            if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 6/Sound Twins"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj")) AssetDatabase.CreateFolder("Assets", "ScriptableObj");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2");
                if (!AssetDatabase.IsValidFolder("Assets/ScriptableObj/Phonics 2/Unit 6")) AssetDatabase.CreateFolder("Assets/ScriptableObj", "Phonics 2/Unit 6");
                if (!AssetDatabase.IsValidFolder(scriptableFolderPath)) AssetDatabase.CreateFolder("Assets/ScriptableObj/Phonics 2/Unit 6", "Sound Twins");
            }

            string scriptableAssetPath = $"{scriptableFolderPath}/SoundTwinsData_Unit6.asset";
            AssetDatabase.CopyAsset(resourcesAssetPath, scriptableAssetPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
            Debug.Log($"[EngSnap] Successfully created SoundTwinsData assets at:\n- {resourcesAssetPath}\n- {scriptableAssetPath}");
        }
    }
}
#endif
