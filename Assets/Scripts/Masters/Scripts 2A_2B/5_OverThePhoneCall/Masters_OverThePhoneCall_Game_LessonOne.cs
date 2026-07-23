using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections;

/// <summary>
/// Unit 5: Over The Phone Call - Game Lesson One (G01: Call Sort — Formal or Informal, Fast).
/// Subclasses Unit 1's falling sort controller directly.
/// </summary>
public class Masters_OverThePhoneCall_Game_LessonOne : Masters_PolishedCommunication_Game_LessonOne {
    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;
    }

    protected override IEnumerator InitialSpawnDelayCoroutine() {
        float delay = (narratorSpeech != null) ? narratorSpeech.length + 0.5f : initialSpawnDelay;
        yield return new WaitForSeconds(delay);
        isGameActive = true;
    }

    protected override void EvaluateDrop(Masters_FallingSortPhraseCard card, Masters_FallingSortBin bin) {
        TextMeshProUGUI tmp = card.GetComponentInChildren<TextMeshProUGUI>();
        string cardText = (tmp != null) ? tmp.text : "";

        SortPuzzle matchedPuzzle = null;
        if (sortPuzzleArray != null) {
            foreach (var puzzle in sortPuzzleArray) {
                if (puzzle != null && puzzle.expression == cardText) {
                    matchedPuzzle = puzzle;
                    break;
                }
            }
        }

        if (matchedPuzzle != null && bin.MatchesUnit8(matchedPuzzle.sortType)) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
            score++;
            UpdateUI();
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }

        activeCards.Remove(card);
        if (cardTargetBins.ContainsKey(card)) cardTargetBins.Remove(card);

        RectTransform cardRect = card.GetComponent<RectTransform>();
        if (cardRect != null) {
            cardRect.DOScale(Vector3.zero, 0.3f).SetEase(DG.Tweening.Ease.InBack).OnComplete(() => {
                if (card != null && card.gameObject != null) Destroy(card.gameObject);
            });
        } else if (card != null && card.gameObject != null) {
            Destroy(card.gameObject);
        }
    }
}
