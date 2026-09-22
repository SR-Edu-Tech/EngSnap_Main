using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

namespace EngSnap.Masters.Unit10 {

    /// <summary>
    /// Core Reading 2 controller for Unit 10: Common Mistakes with Prepositions (Book 2B).
    /// R02 Match — Wobbly and Mended (10 verbatim pairs from GDD).
    /// Inherits directly from Masters_PolishedCommunication_Reading_LessonTwo.
    /// Supports line drag and tap-tap pairing, 2 sets of 5 pairs, score tracking, and voiceover audio feedback.
    /// Pass mark: 8 of 10 pairs.
    /// </summary>
    public class Masters_CommonMistakesWithPrepositions_Reading_LessonTwo : Masters_PolishedCommunication_Reading_LessonTwo {

        [Header("Pair Audio Readouts")]
        [SerializeField] private AudioClip[] pairAudios;

        [Header("Header & Title References")]
        [SerializeField] private TextMeshProUGUI headerTMP;
        [SerializeField] private TextMeshProUGUI titleTMP;
        [SerializeField] private TextMeshProUGUI subtitleTMP;

        [Header("Audio Controls")]
        [SerializeField] private Button replayAudioBtn;

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Reading;
            useUniqueRightCards = false; // 1-to-1 matching: distinct mended sentences on right
            pairsPerSet = 5;             // 5 pairs per page (2 pages total = 10 pairs)

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/10_CommonMistakesWithPrepositions/Reading/cm_r02_full_intro.mp3");
#endif
            }

            if (puzzles == null || puzzles.Length == 0) {
                InitializeCommonMistakesPuzzles();
            }

            EnsureHeaderAndTitle();
            WireReplayButton();
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Reading;
            EnsureHeaderAndTitle();
            WireReplayButton();
            if (progressCountTMP != null) progressCountTMP.text = $"0/{totalPairs}";
        }

        public void PlayIntroAudio() {
            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/10_CommonMistakesWithPrepositions/Reading/cm_r02_full_intro.mp3");
#endif
            }

            if (narratorSpeech != null) {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
                } else {
                    AudioSource src = GetComponent<AudioSource>();
                    if (src == null) src = gameObject.AddComponent<AudioSource>();
                    src.clip = narratorSpeech;
                    src.Play();
                }
            }
        }

        private void WireReplayButton() {
            if (replayAudioBtn == null) {
                Transform replayTr = transform.Find("Controls/ReplayButton") 
                                  ?? transform.Find("ReplayButton") 
                                  ?? transform.Find("RepeatButton")
                                  ?? transform.Find("HeaderContainer/ReplayButton");
                if (replayTr != null) replayAudioBtn = replayTr.GetComponent<Button>();
            }

            if (replayAudioBtn != null) {
                replayAudioBtn.onClick.RemoveAllListeners();
                replayAudioBtn.onClick.AddListener(PlayIntroAudio);
            }
        }

#if UNITY_EDITOR
        private void Reset() {
            InitializeCommonMistakesPuzzles();
            EnsureHeaderAndTitle();
        }

        private void OnValidate() {
            if (puzzles == null || puzzles.Length == 0) {
                InitializeCommonMistakesPuzzles();
            }
        }
#endif

        public void EnsureHeaderAndTitle() {
            if (headerTMP == null) {
                Transform hTrans = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("UnitHeading/UnitTMP") ?? transform.Find("UnitHeading") ?? transform.Find("Branch");
                if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
            }
            if (headerTMP != null) {
                headerTMP.text = "COMMON MISTAKES WITH PREPOSITIONS 🔍";
            }

            if (titleTMP == null) {
                Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle") ?? transform.Find("Match - Word <-> Meaning");
                if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>() ?? tTrans.GetComponentInChildren<TextMeshProUGUI>(true);
            }
            if (titleTMP != null) {
                titleTMP.text = "R02 Match — Wobbly and Mended";
                titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
                titleTMP.enableAutoSizing = false;
                titleTMP.fontSize = 38f;
                titleTMP.fontStyle = FontStyles.Bold;
                titleTMP.alignment = TextAlignmentOptions.Center;
            }

            if (subtitleTMP == null) {
                Transform subTrans = transform.Find("Subtitle") ?? transform.Find("Instruction") ?? transform.Find("HeaderContainer/Subtitle");
                if (subTrans != null) subtitleTMP = subTrans.GetComponent<TextMeshProUGUI>();
            }
            if (subtitleTMP != null) {
                subtitleTMP.text = "Pair each wobbly sentence with its mended version.";
            }

            // Also search all TMP texts in hierarchy
            TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps) {
                string n = t.gameObject.name.ToLower();
                Transform parent = t.transform.parent;
                string pn = parent != null ? parent.name.ToLower() : "";

                if (n.Contains("branch") || n.Contains("header") || pn.Contains("branch") || pn.Contains("header")) {
                    t.text = "COMMON MISTAKES WITH PREPOSITIONS 🔍";
                } else if (n.Contains("lessontitle") || pn.Contains("lessontitle") || (n == "tmp" && pn.Contains("title")) || n.Contains("match - word")) {
                    t.text = "R02 Match — Wobbly and Mended";
                    t.color = new Color(1f, 0.85f, 0.15f, 1f);
                }
            }
        }

        public void InitializeCommonMistakesPuzzles() {
            useUniqueRightCards = false;
            pairsPerSet = 5;

            puzzles = new MatchPuzzle[] {
                // Set 1 (5 Pairs)
                new MatchPuzzle { leftPhrase = "The dessert consisted from fruit and cream.", rightPhrase = "The dessert consisted of fruit and cream." },
                new MatchPuzzle { leftPhrase = "There are flowers on the picture.", rightPhrase = "There are flowers in the picture." },
                new MatchPuzzle { leftPhrase = "It depends from you.", rightPhrase = "It depends on you." },
                new MatchPuzzle { leftPhrase = "Who is in the phone?", rightPhrase = "Who is on the phone?" },
                new MatchPuzzle { leftPhrase = "I am going to home.", rightPhrase = "I am going home." },

                // Set 2 (5 Pairs)
                new MatchPuzzle { leftPhrase = "Where is my phone at?", rightPhrase = "Where is my phone?" },
                new MatchPuzzle { leftPhrase = "We went at the mall.", rightPhrase = "We went to the mall." },
                new MatchPuzzle { leftPhrase = "Have you been in London?", rightPhrase = "Have you been to London?" },
                new MatchPuzzle { leftPhrase = "The office is in the first floor.", rightPhrase = "The office is on the first floor." },
                new MatchPuzzle { leftPhrase = "My birthday is on January.", rightPhrase = "My birthday is in January." }
            };

#if UNITY_EDITOR
            string audioDir = "Assets/Audio/2B/10_CommonMistakesWithPrepositions/Reading/";
            pairAudios = new AudioClip[] {
                UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r02_p01.mp3"),
                UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r02_p02.mp3"),
                UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r02_p03.mp3"),
                UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r02_p04.mp3"),
                UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r02_p05.mp3"),
                UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r02_p06.mp3"),
                UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r02_p07.mp3"),
                UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r02_p08.mp3"),
                UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r02_p09.mp3"),
                UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r02_p10.mp3")
            };
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cm_r02_full_intro.mp3");
#endif
        }

        public override void OnEndDrag(PointerEventData eventData) {
            int previousCorrect = correctCount;
            base.OnEndDrag(eventData);

            // If a new pair was correctly matched, play voiceover readout for the matched pair
            if (correctCount > previousCorrect && pairAudios != null) {
                int matchedIndex = correctCount - 1;
                if (matchedIndex >= 0 && matchedIndex < pairAudios.Length && pairAudios[matchedIndex] != null) {
                    if (Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(pairAudios[matchedIndex]);
                    }
                }
            }
        }
    }
}
