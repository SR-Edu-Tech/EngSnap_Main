using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Game Lesson 2 for Unit 12 Sequence Your Thoughts.
/// Overrides tile spawning and match evaluation so that ANY word belonging to a category (e.g., "similarly")
/// matches ANY response tile of that category (e.g., "COMPARISON").
/// </summary>
public class Masters_SequenceYourThoughts_Game_LessonTwo : Masters_WordSwitch_Game_LessonTwo {

    private struct SpawnItem {
        public string matchId;
        public string displayText;
        public bool isResponse;
    }

    protected override void SetupTiles() {
        foreach (Transform child in tilesContainer) {
            Destroy(child.gameObject);
        }
        allTiles.Clear();

        List<SpawnItem> itemsToSpawn = new List<SpawnItem>();
        for (int i = 0; i < matchPairs.Count; i++) {
            string categoryMatchId = matchPairs[i].responseText; // e.g. "COMPARISON"
            itemsToSpawn.Add(new SpawnItem { matchId = categoryMatchId, displayText = matchPairs[i].offerText, isResponse = false });
            itemsToSpawn.Add(new SpawnItem { matchId = categoryMatchId, displayText = matchPairs[i].responseText, isResponse = true });
        }

        ShuffleList(itemsToSpawn);

        foreach (var item in itemsToSpawn) {
            Masters_ChattingBees_MemoryTile newTile = Instantiate(memoryTilePrefab, tilesContainer);
            newTile.Setup(item.matchId, item.displayText, this);
            newTile.SetIsResponseTile(item.isResponse);
            allTiles.Add(newTile);
        }
    }

    protected override IEnumerator CheckMatchRoutine() {
        yield return new WaitForSeconds(hideDelay);

        bool isMatch = (firstSelectedTile != null && secondSelectedTile != null) &&
                       (firstSelectedTile.GetMatchId() == secondSelectedTile.GetMatchId()) &&
                       (firstSelectedTile.isResponseTile != secondSelectedTile.isResponseTile);

        if (isMatch) {
            firstSelectedTile.SetMatched();
            secondSelectedTile.SetMatched();
            score++;
            matchedPairsCount++;

            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

            if (matchedPairsCount >= matchPairs.Count) {
                GameWon();
            }
        } else {
            if (firstSelectedTile != null) firstSelectedTile.CloseTile();
            if (secondSelectedTile != null) secondSelectedTile.CloseTile();

            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        firstSelectedTile = null;
        secondSelectedTile = null;
        isCheckingMatch = false;

        UpdateUI();
    }
}
