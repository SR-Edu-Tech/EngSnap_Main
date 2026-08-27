using UnityEngine;

namespace EngSnap.Phonics2.Unit4
{
    [System.Serializable]
    public class GalleryPictureItem
    {
        public string wordName = "apple";
        public Sprite pictureSprite;
        public AudioClip wordNormalClip;
        public AudioClip wordVowelStretchedClip;
    }

    [System.Serializable]
    public class RhymeFamilyGroup
    {
        public string familyName = "at family"; // e.g. "at family" (cat, fat, rat, mat, hat)
        public string[] words = new string[] { "cat", "fat", "rat", "mat", "hat" };
        public AudioClip familyRunClip;
        public AudioClip[] wordClips = new AudioClip[5];
    }

    [System.Serializable]
    public class GalleryRoomData
    {
        public string vowelChar = "a";
        public string roomTitle = "The /a/ Room";
        public Color roomThemeColor = new Color(0.9f, 0.4f, 0.4f, 1f);
        public AudioClip roomWelcomeClip; // "Welcome to the /a/ room!"
        public GalleryPictureItem[] pictureWallItems = new GalleryPictureItem[8];
        public RhymeFamilyGroup[] rhymeFamilies = new RhymeFamilyGroup[4];
    }

    [CreateAssetMenu(fileName = "NewSayItAloudGalleryData", menuName = "EngSnap/Phonics2/Unit 4/Say It Aloud Gallery Data")]
    public class SayItAloudGalleryData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip introVoiceClip; // "Welcome to the Say It Aloud Gallery!"
        public AudioClip echoRoundIntroClip; // "My turn, then your turn. Ready?"
        public AudioClip echoPraiseClip; // "Nice loud voice! You said it just right."
        public AudioClip roomCompleteClip; // "This room is glowing now. Four more rooms to explore!"

        [Header("5 Gallery Rooms (ă, ĕ, ĭ, ŏ, ŭ)")]
        public GalleryRoomData[] galleryRooms = new GalleryRoomData[5];
    }
}
