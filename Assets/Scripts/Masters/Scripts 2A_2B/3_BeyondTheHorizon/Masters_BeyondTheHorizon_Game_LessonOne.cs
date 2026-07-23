using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Core controller for Unit 3: Beyond the Horizon (Book 2A) - Game Lesson One (G01: Direction Dash — Sort Phrases Fast).
/// Subclasses PolishedCommunication_Game_LessonOne and configures 3 gates (ASK, MOVEMENT, POSITION).
/// Implements timed sorting reaction with 3 lives and a 60s round where sorting correctly bursts points and wrong gates cost lives.
/// </summary>
public class Masters_BeyondTheHorizon_Game_LessonOne : Masters_PolishedCommunication_Game_LessonOne {

    [Header("Lives & Arcade Rules")]
    [SerializeField] protected int maxLives = 3;
    [SerializeField] protected int currentLives = 3;
    [SerializeField] protected TextMeshProUGUI livesTMP;
    [SerializeField] protected int targetScore = 16;
    protected Masters_Unit3_FallingSortCategory lastSpawnedUnit3Category;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;
        InitializeUnit3Game();
        AutoFindLivesTMP();
    }

#if UNITY_EDITOR
    private void Reset() {
        InitializeUnit3Game();
    }

    private void OnValidate() {
        InitializeUnit3Game();
    }
#endif

    private void AutoFindLivesTMP() {
        if (livesTMP != null) return;
        Transform livesObj = transform.Find("LivesTMP");
        if (livesObj == null) livesObj = transform.Find("TopPanel/LivesTMP");
        if (livesObj == null) livesObj = FindChildRecursive(transform, "LivesTMP");
        if (livesObj == null) livesObj = FindChildRecursive(transform, "Lives");
        if (livesObj != null) livesTMP = livesObj.GetComponent<TextMeshProUGUI>();
    }

    private Transform FindChildRecursive(Transform parent, string name) {
        foreach (Transform child in parent) {
            if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase)) return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    protected override void ConfigureBins() {
        Masters_FallingSortBin[] allBins = GetComponentsInChildren<Masters_FallingSortBin>(true);
        if (allBins == null || allBins.Length == 0) return;

        // Sort left to right visually by X coordinate
        System.Array.Sort(allBins, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        Masters_Unit3_FallingSortCategory[] categories = new Masters_Unit3_FallingSortCategory[] {
            Masters_Unit3_FallingSortCategory.Ask,
            Masters_Unit3_FallingSortCategory.Movement,
            Masters_Unit3_FallingSortCategory.Position
        };

        List<Masters_FallingSortBin> activeBins = new List<Masters_FallingSortBin>();
        for (int i = 0; i < allBins.Length && i < categories.Length; i++) {
            if (allBins[i] != null) {
                allBins[i].gameObject.SetActive(true);
                allBins[i].SetUnit3Category(categories[i]);
                activeBins.Add(allBins[i]);
            }
        }

        // Deactivate any extra bins beyond the 3 gates
        for (int i = categories.Length; i < allBins.Length; i++) {
            if (allBins[i] != null) allBins[i].gameObject.SetActive(false);
        }

        sortBinArray = activeBins.ToArray();
    }

    protected override void Start() {
        base.Start();
        currentLives = maxLives;
        UpdateUI();
    }

    protected override void SpawnRandomCard() {
        if (sortPuzzleArray == null || sortPuzzleArray.Length == 0) return;

        SortPuzzle selectedPuzzle = null;
        int maxAttempts = 50;

        for (int attempt = 0; attempt < maxAttempts; attempt++) {
            SortPuzzle candidate = sortPuzzleArray[Random.Range(0, sortPuzzleArray.Length)];
            if (candidate == null) continue;

            // 1. Prevent spawning if the exact same phrase card is currently active/falling on screen
            bool isAlreadyActive = false;
            foreach (var activeCard in activeCards) {
                if (activeCard != null) {
                    TextMeshProUGUI tmp = activeCard.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null && tmp.text == candidate.expression) {
                        isAlreadyActive = true;
                        break;
                    }
                }
            }
            if (isAlreadyActive) continue;

            // 2. Prevent repeating exact same word recently
            if (recentlySpawnedExpressions.Contains(candidate.expression)) {
                continue;
            }

            // 3. Prevent more than 2 of the exact same gate category (Ask/Movement/Position) in a row
            if (consecutiveCategoryCount >= 2 && candidate.unit3SortType == lastSpawnedUnit3Category) {
                continue;
            }

            selectedPuzzle = candidate;
            break;
        }

        if (selectedPuzzle == null) {
            foreach (var candidate in sortPuzzleArray) {
                if (candidate == null) continue;
                bool isAlreadyActive = false;
                foreach (var activeCard in activeCards) {
                    if (activeCard != null) {
                        TextMeshProUGUI tmp = activeCard.GetComponentInChildren<TextMeshProUGUI>();
                        if (tmp != null && tmp.text == candidate.expression) {
                            isAlreadyActive = true;
                            break;
                        }
                    }
                }
                if (!isAlreadyActive) {
                    selectedPuzzle = candidate;
                    break;
                }
            }
            if (selectedPuzzle == null) {
                selectedPuzzle = sortPuzzleArray[Random.Range(0, sortPuzzleArray.Length)];
            }
        }

        recentlySpawnedExpressions.Add(selectedPuzzle.expression);
        int maxHistory = Mathf.Max(3, sortPuzzleArray.Length / 2);
        if (recentlySpawnedExpressions.Count > maxHistory) {
            recentlySpawnedExpressions.RemoveAt(0);
        }

        if (selectedPuzzle.unit3SortType == lastSpawnedUnit3Category) {
            consecutiveCategoryCount++;
        } else {
            lastSpawnedUnit3Category = selectedPuzzle.unit3SortType;
            consecutiveCategoryCount = 1;
        }

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

        if (matchedPuzzle != null && bin.MatchesUnit3(matchedPuzzle.unit3SortType)) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
            score++;
            bin.AnimateCatch(true);
            UpdateUI();
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            currentLives--;
            bin.AnimateCatch(false);
            UpdateUI();

            if (currentLives <= 0) {
                activeCards.Remove(card);
                if (cardTargetBins.ContainsKey(card)) cardTargetBins.Remove(card);
                if (card != null && card.gameObject != null) Destroy(card.gameObject);
                GameOver();
                return;
            }
        }

        activeCards.Remove(card);
        if (cardTargetBins.ContainsKey(card)) cardTargetBins.Remove(card);

        RectTransform cardRect = card.GetComponent<RectTransform>();
        if (cardRect != null) {
            cardRect.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() => {
                if (card != null && card.gameObject != null) Destroy(card.gameObject);
            });
        } else if (card != null && card.gameObject != null) {
            Destroy(card.gameObject);
        }
    }

    protected override void UpdateUI() {
        base.UpdateUI();
        if (livesTMP != null) {
            livesTMP.text = $"Lives: {currentLives}";
        }
    }

    private void InitializeUnit3Game() {
        if (sortPuzzleArray != null && sortPuzzleArray.Length > 0 && sortPuzzleArray[0] != null &&
            !string.IsNullOrEmpty(sortPuzzleArray[0].expression) && sortPuzzleArray[0].expression.Contains("restroom")) {
            return;
        }

        sortPuzzleArray = new SortPuzzle[] {
            // ASK Category (6 phrases)
            new SortPuzzle { expression = "Excuse me, where is the restroom?", unit3SortType = Masters_Unit3_FallingSortCategory.Ask },
            new SortPuzzle { expression = "Would you mind telling me the way to the Principal's office?", unit3SortType = Masters_Unit3_FallingSortCategory.Ask },
            new SortPuzzle { expression = "Could you please tell me how I can get to the admin office?", unit3SortType = Masters_Unit3_FallingSortCategory.Ask },
            new SortPuzzle { expression = "Where is the stationery store, please?", unit3SortType = Masters_Unit3_FallingSortCategory.Ask },
            new SortPuzzle { expression = "How do I get to the auditorium?", unit3SortType = Masters_Unit3_FallingSortCategory.Ask },
            new SortPuzzle { expression = "Can you tell me the directions to the library?", unit3SortType = Masters_Unit3_FallingSortCategory.Ask },

            // MOVEMENT Category (6 phrases)
            new SortPuzzle { expression = "Go straight...", unit3SortType = Masters_Unit3_FallingSortCategory.Movement },
            new SortPuzzle { expression = "Turn left / right from the junction.", unit3SortType = Masters_Unit3_FallingSortCategory.Movement },
            new SortPuzzle { expression = "Walk / Go along the corridor/road.", unit3SortType = Masters_Unit3_FallingSortCategory.Movement },
            new SortPuzzle { expression = "Go past the park.", unit3SortType = Masters_Unit3_FallingSortCategory.Movement },
            new SortPuzzle { expression = "Take the first / second right.", unit3SortType = Masters_Unit3_FallingSortCategory.Movement },
            new SortPuzzle { expression = "Turn left at the traffic lights.", unit3SortType = Masters_Unit3_FallingSortCategory.Movement },

            // POSITION Category (8 phrases)
            new SortPuzzle { expression = "It's opposite to...", unit3SortType = Masters_Unit3_FallingSortCategory.Position },
            new SortPuzzle { expression = "It is beside...", unit3SortType = Masters_Unit3_FallingSortCategory.Position },
            new SortPuzzle { expression = "It's in between... and......", unit3SortType = Masters_Unit3_FallingSortCategory.Position },
            new SortPuzzle { expression = "The…. is on your right/ left.", unit3SortType = Masters_Unit3_FallingSortCategory.Position },
            new SortPuzzle { expression = "It is on the ground / first floor.", unit3SortType = Masters_Unit3_FallingSortCategory.Position },
            new SortPuzzle { expression = "It sits behind the administration block.", unit3SortType = Masters_Unit3_FallingSortCategory.Position },
            new SortPuzzle { expression = "It is near the main entrance.", unit3SortType = Masters_Unit3_FallingSortCategory.Position },
            new SortPuzzle { expression = "It is across the street from the bank.", unit3SortType = Masters_Unit3_FallingSortCategory.Position }
        };
    }
}
