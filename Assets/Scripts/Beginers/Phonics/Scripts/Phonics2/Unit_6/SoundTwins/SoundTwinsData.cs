using System;
using UnityEngine;

namespace EngSnap.Phonics2.Unit6
{
    [Serializable]
    public class SoundTwinPairItem
    {
        public string pairName = "b / p";
        public string buzzerLetter = "b";
        public string whispererLetter = "p";
        public AudioClip buzzerSoundClip;
        public AudioClip whispererSoundClip;
        public Sprite sharedMouthSprite;
        public string mouthDescription = "Lips together";
    }

    [Serializable]
    public class MinimalPairQuizItem
    {
        public string targetWord = "pear";
        public bool targetIsWhisperer = true;
        public AudioClip targetWordAudio;
        public string choiceWordA = "bear";
        public Sprite choiceSpriteA;
        public string choiceWordB = "pear";
        public Sprite choiceSpriteB;
        public int correctChoiceIndex = 1; // 0 for A, 1 for B
        public AudioClip successFeedbackClip;
    }

    [Serializable]
    public class TwinTroubleGapItem
    {
        public string displayGapWord = "_ear";
        public string correctLetter = "b";
        public string distractorLetter = "p";
        public string fullWord = "bear";
        public Sprite wordSprite;
        public AudioClip wordAudioClip;
    }

    [Serializable]
    public class VvsWItem
    {
        public string wordV = "vet";
        public Sprite spriteV;
        public AudioClip audioV;
        public string wordW = "wet";
        public Sprite spriteW;
        public AudioClip audioW;
        public int targetIndex = 0; // 0 for V, 1 for W
    }

    [CreateAssetMenu(fileName = "SoundTwinsData_Unit6", menuName = "EngSnap/Phonics2/Unit6/Sound Twins Data")]
    public class SoundTwinsData : ScriptableObject
    {
        [Header("Leo Voice Scripts")]
        public AudioClip leoIntroClip;          // "These two are twins. Same lips, same tongue — but listen!"
        public AudioClip oneBuzzesOneWhispersClip; // "One buzzes, one whispers. That is the only difference!"
        public AudioClip minimalPairPromptClip; // "Listen carefully… Which picture is it?"
        public AudioClip vVsWIntroClip;         // "Now two that are NOT twins, but often get mixed up. /v/ — teeth on your lip. /w/ — round lips, like a kiss!"
        public AudioClip momoRetryClip;         // "Listen again: bear … pear … bear … pear."

        [Header("6 Twin Pairs (b/p, d/t, g/k, v/f, z/s, th/th)")]
        public SoundTwinPairItem[] twinPairs = new SoundTwinPairItem[6];

        [Header("8 Minimal Pairs")]
        public MinimalPairQuizItem[] minimalPairs = new MinimalPairQuizItem[8];

        [Header("4 Twin Trouble Gap Items")]
        public TwinTroubleGapItem[] twinTroubleItems = new TwinTroubleGapItem[4];

        [Header("4 V vs W Mouth Practice Items")]
        public VvsWItem[] vVsWItems = new VvsWItem[4];

        [Header("Audio SFX")]
        public AudioClip correctChimeSfx;
        public AudioClip retryGentleSfx;
        public AudioClip starPopSfx;
    }
}
