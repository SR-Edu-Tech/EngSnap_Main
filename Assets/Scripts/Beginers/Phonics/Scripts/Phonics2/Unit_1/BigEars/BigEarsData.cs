using UnityEngine;

namespace EngSnap.Phonics2.Unit1
{
    [System.Serializable]
    public class SoundLottoItem
    {
        public string soundName = "Dog";
        public AudioClip sfxClip;
        public Sprite pictureSprite;
        public AudioClip successVoiceClip;
    }

    [CreateAssetMenu(fileName = "NewBigEarsData", menuName = "EngSnap/Phonics2/Unit 1/Big Ears Data")]
    public class BigEarsData : ScriptableObject
    {
        [Header("Gigi Voice Lines - Intro & Prompts")]
        public AudioClip introVoiceClip; // "Shhh… Let's use our big listening ears. What can you hear?"
        public AudioClip tapPictureInstructionClip; // "Tap the picture that made that sound."
        public AudioClip retryVoiceClip; // "Nearly! Listen once more…"
        public AudioClip loudSoftInstructionClip; // Fallback / "This one is loud… and this one is soft. Tap the loud one!"
        public AudioClip tapLoudInstructionClip; // "This one is loud… and this one is soft. Tap the LOUD one!"
        public AudioClip tapSoftInstructionClip; // "This one is loud… and this one is soft. Tap the SOFT one!"
        public AudioClip whichFirstInstructionClip; // "Which sound came FIRST? Tap it."
        public AudioClip completionBridgeVoiceClip; // "Your ears are strong! Sounds are everywhere… and words have tiny sounds hiding inside them too. Let's catch them!"

        [Header("Sound Lotto Items (10 Environmental Sounds)")]
        public SoundLottoItem[] lottoItems;

        [Header("Loud & Soft Icons")]
        public Sprite elephantLoudSprite;
        public Sprite mouseSoftSprite;
    }
}
