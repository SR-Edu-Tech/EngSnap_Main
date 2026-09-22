using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core Reading 2 controller for Unit 8: Shopping Time (Book 2B).
/// R02 Match — Shop and What It Sells (10 verbatim pairs from GDD p.32).
/// Inherits directly from Masters_PolishedCommunication_Reading_LessonTwo (Book 2A Polished Communication template).
/// Supports line drag and tap-tap pairing, 2 sets of 5 pairs, score tracking, audio feedback, and smooth transitions.
/// </summary>
public class Masters_ShoppingTime_Reading_LessonTwo : Masters_PolishedCommunication_Reading_LessonTwo {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;
        useUniqueRightCards = false; // 1-to-1 matching: distinct goods on the right per page
        pairsPerSet = 5; // 5 pairs per page (2 pages total)
        InitializeShoppingTimePuzzles();
    }

#if UNITY_EDITOR
    private void Reset() {
        InitializeShoppingTimePuzzles();
    }

    private void OnValidate() {
        InitializeShoppingTimePuzzles();
    }
#endif

    public void InitializeShoppingTimePuzzles() {
        useUniqueRightCards = false;
        pairsPerSet = 5;

        puzzles = new MatchPuzzle[] {
            // Level / Set 1 (5 Pairs)
            new MatchPuzzle { leftPhrase = "Baker's / Bakery", rightPhrase = "bread and cakes" },
            new MatchPuzzle { leftPhrase = "Chemist / Pharmacy", rightPhrase = "medicines" },
            new MatchPuzzle { leftPhrase = "Florist", rightPhrase = "flowers" },
            new MatchPuzzle { leftPhrase = "Seafood store / Fishmonger's", rightPhrase = "fish" },
            new MatchPuzzle { leftPhrase = "Greengrocer's / Grocery store", rightPhrase = "fruit and vegetables" },

            // Level / Set 2 (5 Pairs)
            new MatchPuzzle { leftPhrase = "Toy shop / Toy store", rightPhrase = "toys and games" },
            new MatchPuzzle { leftPhrase = "Shoe shop", rightPhrase = "shoes" },
            new MatchPuzzle { leftPhrase = "Jeweller's / Jewellery store", rightPhrase = "rings and chains" },
            new MatchPuzzle { leftPhrase = "Record shop / Music store", rightPhrase = "music" },
            new MatchPuzzle { leftPhrase = "Tailor", rightPhrase = "clothes made to your size" }
        };
    }
}
