using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Core controller for Unit 7: Question Tags - Game Lesson Two (G02: Tag Pairs — Memory Match).
/// Inherits from Masters_PolishedCommunication_Game_LessonTwo and manages 9 statement ↔ tag pairs (18 stones).
/// Provides introduction audio, stone flip voice-overs, correct/wrong sound effects, and full sentence audio echoes upon match.
/// </summary>
public class Masters_QuestionTags_Game_LessonTwo : Masters_PolishedCommunication_Game_LessonTwo {

    [System.Serializable]
    public class TagMatchPairData {
        public string statementText;
        public string tagText;
        public AudioClip statementAudio;
        public AudioClip tagAudio;
        public AudioClip echoAudio;
    }

    [Header("Question Tags G02 Settings")]
    [SerializeField]
    private AudioClip introAudio;

    [SerializeField]
    private List<TagMatchPairData> tagPairs = new List<TagMatchPairData>();

    public void SetTagPairsData(List<TagMatchPairData> data, AudioClip intro = null) {
        tagPairs = data;
        introAudio = intro;

        matchPairs = new List<MemoryMatchPair>();
        if (data != null) {
            foreach (var item in data) {
                if (item != null) {
                    matchPairs.Add(new MemoryMatchPair {
                        offerText = item.statementText,
                        responseText = item.tagText
                    });
                }
            }
        }
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;
    }

    protected override void Start() {
        base.Start();

        if (introAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(introAudio);
        }
    }

    public override void TileSelected(Masters_PolishedCommunication_MemoryTile selectedTile) {
        base.TileSelected(selectedTile);

        // Play individual tile voice-over on select
        if (selectedTile != null && tagPairs != null) {
            string matchId = selectedTile.GetMatchId();
            if (matchId.StartsWith("Pair_") && int.TryParse(matchId.Substring(5), out int idx)) {
                if (idx >= 0 && idx < tagPairs.Count && tagPairs[idx] != null) {
                    var pair = tagPairs[idx];
                    // Find if it was statement or tag
                    AudioClip clipToPlay = null;
                    if (pair.statementAudio != null && selectedTile.name.Contains(pair.statementText)) {
                        clipToPlay = pair.statementAudio;
                    } else if (pair.tagAudio != null && selectedTile.name.Contains(pair.tagText)) {
                        clipToPlay = pair.tagAudio;
                    } else {
                        // Fallback: if first selection or statement audio
                        clipToPlay = (selectedTile == firstSelectedTile) ? pair.statementAudio : pair.tagAudio;
                    }

                    if (clipToPlay != null && Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
                    }
                }
            }
        }
    }

    protected override IEnumerator CheckMatchRoutine() {
        yield return new WaitForSeconds(hideDelay);

        if (firstSelectedTile != null && secondSelectedTile != null && firstSelectedTile.GetMatchId() == secondSelectedTile.GetMatchId()) {
            firstSelectedTile.SetMatched();
            secondSelectedTile.SetMatched();
            score++;
            matchedPairsCount++;

            int pairIndex = -1;
            string matchId = firstSelectedTile.GetMatchId();
            if (matchId.StartsWith("Pair_") && int.TryParse(matchId.Substring(5), out int pIdx)) {
                pairIndex = pIdx;
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

                // Step 3 in GDD: "If the statement matches its tag, both lock face-up and the whole sentence echoes."
                if (pairIndex >= 0 && pairIndex < tagPairs.Count && tagPairs[pairIndex] != null && tagPairs[pairIndex].echoAudio != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(tagPairs[pairIndex].echoAudio);
                }
            }

            if (matchPairs != null && matchedPairsCount >= matchPairs.Count) {
                GameWon();
            }
        } else {
            if (firstSelectedTile != null) firstSelectedTile.CloseTile();
            if (secondSelectedTile != null) secondSelectedTile.CloseTile();

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }

        firstSelectedTile = null;
        secondSelectedTile = null;
        isCheckingMatch = false;

        UpdateUI();
    }
}
