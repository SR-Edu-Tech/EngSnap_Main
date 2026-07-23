using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Unit 4: Code of Conduct - Game Lesson One (G01: Kindness Dash — Sort Expressions Fast).
/// Subclasses Unit 1's falling sort controller and overrides ConfigureBins() to enable all 5 bins
/// for the 5 expression families: THANK YOU, YOU'RE WELCOME, SAYING SORRY, GOOD JOB, BEAUTIFUL.
/// Uses the inherited sortPuzzleArray directly with unit4SortType.
/// </summary>
public class Masters_CodeOfConduct_Game_LessonOne : Masters_PolishedCommunication_Game_LessonOne {

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;
    }

    /// <summary>
    /// Override ConfigureBins to activate all 5 bins and assign Unit 4 categories.
    /// </summary>
    protected override void ConfigureBins() {
        Masters_FallingSortBin[] allBins = GetComponentsInChildren<Masters_FallingSortBin>(true);
        if (allBins == null || allBins.Length == 0) return;

        Masters_Unit4_FallingSortCategory[] categories = new Masters_Unit4_FallingSortCategory[] {
            Masters_Unit4_FallingSortCategory.ThankYou,
            Masters_Unit4_FallingSortCategory.YoureWelcome,
            Masters_Unit4_FallingSortCategory.SayingSorry,
            Masters_Unit4_FallingSortCategory.GoodJob,
            Masters_Unit4_FallingSortCategory.Beautiful
        };

        List<Masters_FallingSortBin> activeBins = new List<Masters_FallingSortBin>();
        for (int i = 0; i < allBins.Length && i < categories.Length; i++) {
            if (allBins[i] != null) {
                allBins[i].gameObject.SetActive(true);
                allBins[i].SetUnit4Category(categories[i]);
                activeBins.Add(allBins[i]);
            }
        }

        // Deactivate any extra bins beyond what we need
        for (int i = categories.Length; i < allBins.Length; i++) {
            if (allBins[i] != null) allBins[i].gameObject.SetActive(false);
        }

        sortBinArray = activeBins.ToArray();
    }

    /// <summary>
    /// Override SpawnRandomCard to use inherited sortPuzzleArray data.
    /// </summary>
    protected override void SpawnRandomCard() {
        if (sortPuzzleArray == null || sortPuzzleArray.Length == 0) return;

        SortPuzzle selectedPuzzle = sortPuzzleArray[Random.Range(0, sortPuzzleArray.Length)];
        if (selectedPuzzle == null) return;

        if (phraseCardPrefab != null && topSpawnPoint != null) {
            Masters_FallingSortPhraseCard newCard = Instantiate(phraseCardPrefab, topSpawnPoint.parent);
            newCard.SetExpression(selectedPuzzle.expression);

            RectTransform cardRect = newCard.GetComponent<RectTransform>();
            if (cardRect != null) {
                cardRect.position = topSpawnPoint.position;
                cardRect.localScale = Vector3.one;
            }
            newCard.gameObject.SetActive(true);

            newCard.OnDragEnded += HandleCardDragEnded;
            activeCards.Add(newCard);
        }
    }

    /// <summary>
    /// Override EvaluateDrop to match against Unit 4 categories (MatchesUnit4) using unit4SortType.
    /// </summary>
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

        if (matchedPuzzle != null && bin.MatchesUnit4(matchedPuzzle.unit4SortType)) {
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
