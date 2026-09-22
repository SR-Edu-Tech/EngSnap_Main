#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace EngSnap.Phonics2.Unit6.Editor
{
    public static class Unit6MasterAssetCreator
    {
        [MenuItem("EngSnap/Phonics2/Create All Unit 6 Data Assets", false, 60)]
        public static void CreateAllUnit6Assets()
        {
            BuzzOrWhisperAssetCreator.CreateBuzzOrWhisperAsset();
            SoundTwinsAssetCreator.CreateSoundTwinsAsset();
            ConsonantTeamsAssetCreator.CreateConsonantTeamsAsset();
            SortingCaveAssetCreator.CreateSortingCaveAsset();
            Debug.Log("[EngSnap] All Unit 6 Data Assets successfully created!");
        }
    }
}
#endif
