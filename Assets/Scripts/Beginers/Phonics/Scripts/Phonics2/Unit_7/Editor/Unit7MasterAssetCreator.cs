#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit7.Editor
{
    public static class Unit7MasterAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create All Unit 7 Data Assets", false, 70)]
        public static void CreateAllUnit7Assets()
        {
            Debug.Log("[EngSnap] Creating all ScriptableObject data assets for Unit 7 (Beginning, Middle, Ending Sounds)...");

            FrontCarriageAssetCreator.CreateFrontCarriageAsset();
            BackCarriageAssetCreator.CreateBackCarriageAsset();
            MiddleCarriageAssetCreator.CreateMiddleCarriageAsset();
            WholeTrainAssetCreator.CreateWholeTrainAsset();

            Debug.Log("[EngSnap] ✅ Successfully created all Unit 7 Data Assets (Stops 1–4)!");
        }
    }
}
#endif
