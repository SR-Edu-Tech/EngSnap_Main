using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EngSnap.Masters.Unit9 {

    /// <summary>
    /// Core Reading 2 controller for Unit 9: Prepositions of Time (Book 2B).
    /// R02 Match — Time Idiom and Meaning (10 verbatim pairs from GDD p.38).
    /// Inherits directly from Masters_PolishedCommunication_Reading_LessonTwo.
    /// Supports line drag and tap-tap pairing, 2 sets of 5 pairs, score tracking, and audio feedback.
    /// Pass mark: 8 of 10 pairs.
    /// </summary>
    public class Masters_PrepositionsOfTime_Reading_LessonTwo : Masters_PolishedCommunication_Reading_LessonTwo {

        protected override void Awake() {
            base.Awake();
            topic = Masters_Topic.Reading;
            useUniqueRightCards = false; // 1-to-1 matching: distinct meanings on the right per page
            pairsPerSet = 5;             // 5 pairs per page (2 pages total)
            InitializePrepositionsOfTimePuzzles();
        }

#if UNITY_EDITOR
        private void Reset() {
            InitializePrepositionsOfTimePuzzles();
        }
#endif

        public void InitializePrepositionsOfTimePuzzles() {
            useUniqueRightCards = false;
            pairsPerSet = 5;

            puzzles = new MatchPuzzle[] {
                // Set 1 (5 Pairs)
                new MatchPuzzle { leftPhrase = "Time flies", rightPhrase = "Time passes quickly." },
                new MatchPuzzle { leftPhrase = "It's high time", rightPhrase = "It's the right time to do something, or past the appropriate time." },
                new MatchPuzzle { leftPhrase = "Third time's a charm", rightPhrase = "The third time you do something that will finally work." },
                new MatchPuzzle { leftPhrase = "Beat the clock", rightPhrase = "Finish something before time is up, before a deadline." },
                new MatchPuzzle { leftPhrase = "Better late than never", rightPhrase = "Doing something late is better than not doing it at all." },

                // Set 2 (5 Pairs)
                new MatchPuzzle { leftPhrase = "At the eleventh hour", rightPhrase = "Almost too late or at the last possible moment." },
                new MatchPuzzle { leftPhrase = "In the long run", rightPhrase = "In the long term, over a long period of time." },
                new MatchPuzzle { leftPhrase = "Make up for lost time", rightPhrase = "To catch up, to do something intensely to make up for lost time." },
                new MatchPuzzle { leftPhrase = "Ship has sailed", rightPhrase = "A lost opportunity, missed shot." },
                new MatchPuzzle { leftPhrase = "Around the clock", rightPhrase = "For 24 hours, without stopping." }
            };
        }
    }
}
