using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Core controller for Unit 1: Polished Communication - Game Lesson Two (G02: Twin Match / Formal-Informal Memory).
/// Standalone implementation independent of older book scripts.
/// Handles spawning tiles, tracking matches, scoring, and end-game flow.
/// </summary>
public class Masters_PolishedCommunication_Game_LessonTwo : Masters_Lesson {

    [Header("Game Settings")]
    [Tooltip("How long to show the two selected tiles before flipping them back over (if incorrect).")]
    [SerializeField]
    protected float hideDelay = 1f;

    [Header("Memory Game Area")]
    [Tooltip("The parent container with a Grid/Layout group where the tiles spawn.")]
    [SerializeField]
    protected Transform tilesContainer;

    [Tooltip("The memory tile prefab that gets instantiated for every pair item.")]
    [SerializeField]
    protected Masters_PolishedCommunication_MemoryTile memoryTilePrefab;

    [Header("UI Elements")]
    [SerializeField]
    protected TextMeshProUGUI scoreTMP;
    [SerializeField]
    protected Masters_LessonSO nextLessonSO;

    [System.Serializable]
    public class MemoryMatchPair {
        public string offerText;
        public string responseText;
    }

    [Header("Tile Pairs Data")]
    [Tooltip("The list of pairs. Each element spawns two tiles (one for offer, one for response).")]
    [SerializeField]
    protected List<MemoryMatchPair> matchPairs;

    [Header("Game Over / Quiz Complete Settings")]
    [SerializeField]
    protected GameObject quizCompleteGameObject;
    [SerializeField]
    protected GameObject gamePlayGameObject;
    [SerializeField]
    protected Button retryButton;
    [SerializeField]
    protected Button closeButton;

    // Internal State
    protected int score;
    protected bool isGameActive;

    protected List<Masters_PolishedCommunication_MemoryTile> allTiles = new List<Masters_PolishedCommunication_MemoryTile>();
    protected Masters_PolishedCommunication_MemoryTile firstSelectedTile;
    protected Masters_PolishedCommunication_MemoryTile secondSelectedTile;

    protected int matchedPairsCount = 0;
    protected bool isCheckingMatch = false;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;
    }

    protected override void Start() {
        base.Start();

        if (retryButton != null) {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(RetryGame);
        }
        if (closeButton != null) {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseGame);
        }

        StartGame();
    }

    protected virtual void StartGame() {
        score = 0;
        isGameActive = true;
        matchedPairsCount = 0;
        isCheckingMatch = false;
        firstSelectedTile = null;
        secondSelectedTile = null;

        UpdateUI();

        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(true);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(false);

        SetupTiles();
    }

    protected virtual void SetupTiles() {
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
            string matchId = "Pair_" + i;
            itemsToSpawn.Add((matchId, matchPairs[i].offerText));
            itemsToSpawn.Add((matchId, matchPairs[i].responseText));
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

    protected void ShuffleList<T>(List<T> list) {
        if (list == null) return;
        for (int i = 0; i < list.Count; i++) {
            var temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    protected virtual void RetryGame() {
        StartGame();
    }

    protected virtual void CloseGame() {
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(true);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(false);
    }

    public virtual bool CanSelectTile() {
        return isGameActive && !isCheckingMatch;
    }

    public virtual void TileSelected(Masters_PolishedCommunication_MemoryTile selectedTile) {
        if (firstSelectedTile == null) {
            firstSelectedTile = selectedTile;
        } else if (secondSelectedTile == null && selectedTile != firstSelectedTile) {
            secondSelectedTile = selectedTile;
            isCheckingMatch = true;
            StartCoroutine(CheckMatchRoutine());
        }
    }

    protected virtual IEnumerator CheckMatchRoutine() {
        yield return new WaitForSeconds(hideDelay);

        if (firstSelectedTile != null && secondSelectedTile != null && firstSelectedTile.GetMatchId() == secondSelectedTile.GetMatchId()) {
            firstSelectedTile.SetMatched();
            secondSelectedTile.SetMatched();
            score++;
            matchedPairsCount++;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
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

    protected void UpdateUI() {
        if (scoreTMP != null) scoreTMP.text = "Score: " + score;
    }

    protected void ShowQuizCompleteScreen() {
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(true);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(false);

        if (retryButton != null) retryButton.gameObject.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(true);

        if (nextButton != null) {
            nextButton.interactable = true;
            NextButtonAnimation();
        }
    }

    protected virtual void GameWon() {
        isGameActive = false;
        ShowQuizCompleteScreen();
    }

    public bool IsGameActive() {
        return isGameActive;
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) return;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}
