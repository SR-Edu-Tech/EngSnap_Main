using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace EngSnap.Masters.Unit11 {

    /// <summary>
    /// Core Reading 2 controller for Unit 11: Art of Politeness (Book 2B).
    /// R02 Match — Blunt and Polished: 8 blunt sentences on left, 8 polished twins on right.
    /// Student draws lines between each blunt sentence and its polished twin.
    /// Supports pagination across 2 sets (4 pairs per set).
    /// Pass threshold: 7 of 8 pairs.
    /// </summary>
    public class Masters_ArtOfPoliteness_Reading_LessonTwo : Masters_PolishedCommunication_Reading_LessonTwo {

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Reading;

            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/11_ArtOfPoliteness/Reading/artofpoliteness_r02_full_intro.mp3");
#endif
            }

            EnsureArtOfPolitenessHeaderAndTitle();
            InitializeArtOfPolitenessPuzzles();
        }

        protected override void Start() {
            base.Start();
            topic = Masters_Topic.Reading;
            EnsureArtOfPolitenessHeaderAndTitle();
        }

        private void EnsureArtOfPolitenessHeaderAndTitle() {
            TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tmps) {
                if (t == null) continue;
                string n = t.gameObject.name.ToLower();
                if (n.Contains("lessontitle") || n.Contains("title") || t.text.Contains("Match") || t.text.Contains("R02")) {
                    t.text = "R02 Match — Blunt and Polished";
                } else if (n.Contains("header") || n.Contains("branch") || n.Contains("heading")) {
                    t.text = "THE ART OF POLITENESS 🎩";
                }
            }
        }

        public void InitializeArtOfPolitenessPuzzles() {
            useUniqueRightCards = false;
            pairsPerSet = 4;

            if (puzzles == null || puzzles.Length == 0) {
                puzzles = new MatchPuzzle[] {
                    // Set 1 (Pairs 1 - 4)
                    new MatchPuzzle {
                        leftPhrase = "Send me the report.",
                        rightPhrase = "Could you send me the report, please?"
                    },
                    new MatchPuzzle {
                        leftPhrase = "You're wrong.",
                        rightPhrase = "I think you might be mistaken."
                    },
                    new MatchPuzzle {
                        leftPhrase = "I want a pizza.",
                        rightPhrase = "I'll have a pizza, please."
                    },
                    new MatchPuzzle {
                        leftPhrase = "Give me that book.",
                        rightPhrase = "Could I borrow that book for a moment?"
                    },

                    // Set 2 (Pairs 5 - 8)
                    new MatchPuzzle {
                        leftPhrase = "Where is the meeting?",
                        rightPhrase = "May I ask where the meeting is?"
                    },
                    new MatchPuzzle {
                        leftPhrase = "That's a terrible idea.",
                        rightPhrase = "I'm not sure that would be the best approach."
                    },
                    new MatchPuzzle {
                        leftPhrase = "Help me with this.",
                        rightPhrase = "Could you possibly help me with this?"
                    },
                    new MatchPuzzle {
                        leftPhrase = "Tell me what happened.",
                        rightPhrase = "Could you tell me a bit more about what happened?"
                    }
                };
            }

            totalPairs = puzzles.Length;
        }
    }
}
