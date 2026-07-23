using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unit 4: Code of Conduct - Game Lesson Two (G02: Thank-You Match — Thanks ↔ Welcome).
/// Subclasses Unit 1's memory tile matching controller.
/// Overrides SetupTiles and CheckMatchRoutine to support cross-family matching:
/// any THANK YOU card paired with any YOU'RE WELCOME card locks as a valid match.
/// </summary>
public class Masters_CodeOfConduct_Game_LessonTwo : Masters_PolishedCommunication_Game_LessonTwo {

    public void SetMatchPairsData(List<MemoryMatchPair> data) {
        matchPairs = data;
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;
    }

    protected override void SetupTiles() {
        if (tilesContainer != null) {
            foreach (Transform child in tilesContainer) {
                if (child != null && child.gameObject != null) Destroy(child.gameObject);
            }
        }
        allTiles.Clear();

        if (matchPairs == null || memoryTilePrefab == null || tilesContainer == null) return;

        List<(string matchId, string displayText)> itemsToSpawn = new List<(string, string)>();
        for (int i = 0; i < matchPairs.Count; i++) {
            if (matchPairs[i] == null) continue;
            // Tag all offers as ThankYou and all responses as Welcome
            itemsToSpawn.Add(("ThankYou", matchPairs[i].offerText));
            itemsToSpawn.Add(("Welcome", matchPairs[i].responseText));
        }

        ShuffleList(itemsToSpawn);

        foreach (var item in itemsToSpawn) {
            Masters_PolishedCommunication_MemoryTile newTile = Instantiate(memoryTilePrefab, tilesContainer);
            if (newTile != null) {
                newTile.Setup(item.matchId, item.displayText, this);
                allTiles.Add(newTile);
            }
        }
    }

    protected override IEnumerator CheckMatchRoutine() {
        yield return new WaitForSeconds(hideDelay);

        if (firstSelectedTile != null && secondSelectedTile != null) {
            string id1 = firstSelectedTile.GetMatchId();
            string id2 = secondSelectedTile.GetMatchId();

            bool isValidPair = (id1 == "ThankYou" && id2 == "Welcome") || (id1 == "Welcome" && id2 == "ThankYou");

            if (isValidPair) {
                firstSelectedTile.SetMatched();
                secondSelectedTile.SetMatched();
                score++;
                matchedPairsCount += 2; // 2 cards locked per match

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                // Total pairs is matchPairs.Count (8 pairs = 16 cards total = matchedPairsCount >= 16)
                if (matchPairs != null && matchedPairsCount >= matchPairs.Count * 2) {
                    GameWon();
                }
            } else {
                if (firstSelectedTile != null) firstSelectedTile.CloseTile();
                if (secondSelectedTile != null) secondSelectedTile.CloseTile();

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }
            }
        }

        firstSelectedTile = null;
        secondSelectedTile = null;
        isCheckingMatch = false;

        UpdateUI();
    }
}
