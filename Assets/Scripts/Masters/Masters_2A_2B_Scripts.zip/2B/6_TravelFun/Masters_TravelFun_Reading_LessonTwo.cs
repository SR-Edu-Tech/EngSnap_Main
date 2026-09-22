using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core Reading 2 controller for Unit 6: Travel Fun (Book 2B).
/// Inherits directly from Masters_PolishedCommunication_Reading_LessonTwo (Book 2A Polished Communication template).
/// 10 Travel Phrase <-> Book Meaning pairs (2 sets of 5 options each).
/// </summary>
public class Masters_TravelFun_Reading_LessonTwo : Masters_PolishedCommunication_Reading_LessonTwo {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;
        useUniqueRightCards = false; // 1-to-1 matching: 5 distinct meanings on the right per page
        pairsPerSet = 5; // 5 pairs per level / page
        InitializeTravelFunPuzzles();
    }

#if UNITY_EDITOR
    private void Reset() {
        InitializeTravelFunPuzzles();
    }

    private void OnValidate() {
        InitializeTravelFunPuzzles();
    }
#endif

    public void InitializeTravelFunPuzzles() {
        useUniqueRightCards = false;
        pairsPerSet = 5;

        puzzles = new MatchPuzzle[] {
            // Level / Set 1 (5 Options)
            new MatchPuzzle { leftPhrase = "See off", rightPhrase = "Go to the airport or station to say good bye to someone" },
            new MatchPuzzle { leftPhrase = "Set off", rightPhrase = "Start a journey" },
            new MatchPuzzle { leftPhrase = "Get in", rightPhrase = "Arrive (train, plane)" },
            new MatchPuzzle { leftPhrase = "Hold up", rightPhrase = "Delay when travelling" },
            new MatchPuzzle { leftPhrase = "Check in", rightPhrase = "Arrive and register at a hotel or airport" },

            // Level / Set 2 (5 Options)
            new MatchPuzzle { leftPhrase = "Get off", rightPhrase = "Leave a bus, train, plane" },
            new MatchPuzzle { leftPhrase = "Check out", rightPhrase = "Leave the hotel after paying" },
            new MatchPuzzle { leftPhrase = "Get on", rightPhrase = "Enter a bus, train, plane, to climb on board" },
            new MatchPuzzle { leftPhrase = "Pick up", rightPhrase = "Let some one get into your vehicle and take them somewhere" },
            new MatchPuzzle { leftPhrase = "Touch down", rightPhrase = "Land (Planes)" }
        };
    }
}
