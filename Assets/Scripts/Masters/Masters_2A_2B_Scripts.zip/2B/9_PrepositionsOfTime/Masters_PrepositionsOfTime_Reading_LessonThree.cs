using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EngSnap.Masters.Unit9 {

    /// <summary>
    /// Core Reading 3 controller for Unit 9: Prepositions of Time (Book 2B).
    /// R03 Match — Word and Meaning (revision from Unit 1).
    /// 12 verbatim pairs from GDD p.40 matching phrasal verbs with their definitions.
    /// Inherits directly from Masters_PolishedCommunication_Reading_LessonTwo.
    /// Supports line drag and tap-tap pairing, 2 sets of 6 pairs (12 pairs total), score tracking, and audio feedback.
    /// Pass mark: 9 of 12 pairs.
    /// </summary>
    public class Masters_PrepositionsOfTime_Reading_LessonThree : Masters_PolishedCommunication_Reading_LessonTwo {

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Reading;
            useUniqueRightCards = false; // 1-to-1 matching: distinct meanings on the right per page
            pairsPerSet = 6;             // 6 pairs per page (2 pages total = 12 pairs)
            InitializePrepositionsOfTimePuzzles();
        }

#if UNITY_EDITOR
        private void Reset() {
            InitializePrepositionsOfTimePuzzles();
        }
#endif

        public void InitializePrepositionsOfTimePuzzles() {
            useUniqueRightCards = false;
            pairsPerSet = 6;

            puzzles = new MatchPuzzle[] {
                // Set 1 (6 Pairs)
                new MatchPuzzle { leftPhrase = "Figure out", rightPhrase = "Solve something" },
                new MatchPuzzle { leftPhrase = "Find out", rightPhrase = "Discover" },
                new MatchPuzzle { leftPhrase = "Run out", rightPhrase = "Have none left" },
                new MatchPuzzle { leftPhrase = "Fill up", rightPhrase = "Fill to the brim" },
                new MatchPuzzle { leftPhrase = "Blow up", rightPhrase = "Explode" },
                new MatchPuzzle { leftPhrase = "Run off", rightPhrase = "Flee" },

                // Set 2 (6 Pairs)
                new MatchPuzzle { leftPhrase = "Clean up", rightPhrase = "Tidy" },
                new MatchPuzzle { leftPhrase = "Point out", rightPhrase = "Draw attention to" },
                new MatchPuzzle { leftPhrase = "Bring up", rightPhrase = "Start talking about a subject" },
                new MatchPuzzle { leftPhrase = "Cross out", rightPhrase = "Draw a line through" },
                new MatchPuzzle { leftPhrase = "Call back", rightPhrase = "Return a phone call" },
                new MatchPuzzle { leftPhrase = "Hang up", rightPhrase = "End a phone call" }
            };
        }
    }
}
