using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unit 2: Clear Confusion - Game Lesson Two (G02: Question Memory — Phrase ↔ Job).
/// Subclasses Unit 1's memory tile matching controller.
/// Same mechanic: flip two cards at a time to match each verbatim phrase with its correct job tag.
/// </summary>
public class Masters_ClearConfusion_Game_LessonTwo : Masters_PolishedCommunication_Game_LessonTwo {

    public void SetMatchPairsData(List<MemoryMatchPair> data) {
        matchPairs = data;
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;
    }
}
