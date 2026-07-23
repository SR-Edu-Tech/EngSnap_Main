using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Game Lesson 2 for Unit 14 Real Life Interactions.
/// Memory Match game where character names match any of their spoken dialogue lines.
/// Also plays narrator audio at lesson start.
/// </summary>
public class Masters_RealLifeInteractions_Game_LessonTwo : Masters_SequenceYourThoughts_Game_LessonTwo {

    [Header("Unit 14 Audio")]
    [SerializeField] private AudioClip narratorAudio;

    protected override void Start() {
        base.Start();
        if (narratorAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorAudio);
        }
    }

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
            // Match ID is offerText (Character Name) so any card for that character matches any of their lines!
            string characterMatchId = matchPairs[i].offerText; 
            itemsToSpawn.Add(new SpawnItem { matchId = characterMatchId, displayText = matchPairs[i].offerText, isResponse = false });
            itemsToSpawn.Add(new SpawnItem { matchId = characterMatchId, displayText = matchPairs[i].responseText, isResponse = true });
        }

        ShuffleList(itemsToSpawn);

        foreach (var item in itemsToSpawn) {
            Masters_ChattingBees_MemoryTile newTile = Instantiate(memoryTilePrefab, tilesContainer);
            newTile.Setup(item.matchId, item.displayText, this);
            newTile.SetIsResponseTile(item.isResponse);
            allTiles.Add(newTile);
        }
    }
}
