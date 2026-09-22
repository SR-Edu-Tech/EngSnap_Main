using System;
using System.Collections.Generic;
using UnityEngine;

namespace EngSnap.ConfusedWords
{
    /// <summary>
    /// Master Audio Registry for Unit 7 (Confused English Words).
    /// Holds strongly-typed, serialized references to all Unit 7 Music, SFX, and Voice-Over assets.
    /// </summary>
    [Serializable]
    public class M3A_U7_AudioRegistry
    {
        [Header("Music References")]
        [SerializeField] public AudioClip musUnitTheme;
        [SerializeField] public AudioClip musArcade;
        [SerializeField] public AudioClip musQuiz;
        [SerializeField] public AudioClip musReward;

        [Header("SFX References")]
        [SerializeField] public AudioClip sfxMagnify;
        [SerializeField] public AudioClip sfxClick;
        [SerializeField] public AudioClip sfxTap;
        [SerializeField] public AudioClip sfxBranchDone;
        [SerializeField] public AudioClip sfxUnlock;
        [SerializeField] public AudioClip sfxCorrect;
        [SerializeField] public AudioClip sfxWrong;
        [SerializeField] public AudioClip sfxStamp;
        [SerializeField] public AudioClip sfxKey;
        [SerializeField] public AudioClip sfxMatch;
        [SerializeField] public AudioClip sfxRecStart;
        [SerializeField] public AudioClip sfxRecStop;
        [SerializeField] public AudioClip sfxArrest;
        [SerializeField] public AudioClip sfxEscape;
        [SerializeField] public AudioClip sfxFlip;
        [SerializeField] public AudioClip sfxCurtain;
        [SerializeField] public AudioClip sfxResultPass;
        [SerializeField] public AudioClip sfxConfetti;
        [SerializeField] public AudioClip sfxBadge;

        [Header("Word Pronunciation Audio")]
        [SerializeField] public AudioClip wordQuiet;
        [SerializeField] public AudioClip wordQuite;
        [SerializeField] public AudioClip wordDiary;
        [SerializeField] public AudioClip wordDairy;
        [SerializeField] public AudioClip wordWhoever;
        [SerializeField] public AudioClip wordWhomever;

        [Header("Intro Audio References")]
        [SerializeField] public AudioClip voIntroAria;
        [SerializeField] public AudioClip voIntroLeo;
        [SerializeField] public AudioClip voIntroAriaGoal;

        [Header("Hub Audio References")]
        [SerializeField] public AudioClip voHubAria;

        [Header("Listening Audio References")]
        [SerializeField] public AudioClip voL01Aria;
        [SerializeField] public List<AudioClip> voL01Sentences = new List<AudioClip>();
        [SerializeField] public AudioClip voL02AriaPrompt;
        [SerializeField] public List<AudioClip> voL02Clues = new List<AudioClip>();
        [SerializeField] public List<AudioClip> voL02Reveals = new List<AudioClip>();

        [Header("Reading Audio References")]
        [SerializeField] public AudioClip voR01Aria;
        [SerializeField] public List<AudioClip> voR01Sentences = new List<AudioClip>();
        [SerializeField] public AudioClip voR02Aria;
        [SerializeField] public List<AudioClip> voR02Sentences = new List<AudioClip>();
        [SerializeField] public AudioClip voR03Aria;
        [SerializeField] public List<AudioClip> voR03Sentences = new List<AudioClip>();

        [Header("Writing Audio References")]
        [SerializeField] public AudioClip voW01Aria;
        [SerializeField] public List<AudioClip> voW01Readbacks = new List<AudioClip>();
        [SerializeField] public AudioClip voW02Aria;
        [SerializeField] public List<AudioClip> voW02Readbacks = new List<AudioClip>();

        [Header("Speaking Audio References")]
        [SerializeField] public List<AudioClip> voSp01Prompts = new List<AudioClip>();
        [SerializeField] public List<AudioClip> voSp01Models = new List<AudioClip>();

        [Header("Game Audio References")]
        [SerializeField] public AudioClip voG01Aria;
        [SerializeField] public AudioClip voG02Aria;

        [Header("Roleplay Audio References")]
        [SerializeField] public AudioClip voRp01Aria;
        [SerializeField] public List<AudioClip> voRp01Npc = new List<AudioClip>();
        [SerializeField] public List<AudioClip> voRp01Leo = new List<AudioClip>();
        [SerializeField] public AudioClip voRp02SetupA;
        [SerializeField] public AudioClip voRp02SetupB;
        [SerializeField] public AudioClip voRp02SetupC;
        [SerializeField] public AudioClip voRp02Reply;
        [SerializeField] public AudioClip voRp02ReadbackA;
        [SerializeField] public AudioClip voRp02ReadbackB;
        [SerializeField] public AudioClip voRp02ReadbackC;

        [Header("Quiz Audio References")]
        [SerializeField] public AudioClip voQ01AriaHost;
        [SerializeField] public AudioClip voQ01Pass;
        [SerializeField] public AudioClip voQ01Retry;

        [Header("Reward Audio References")]
        [SerializeField] public AudioClip voRwdAria;
        [SerializeField] public AudioClip voRwdSummary;
    }
}
