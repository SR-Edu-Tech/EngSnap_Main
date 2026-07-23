using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Game Manager for Tricky Three - Game Lesson 2 (Memory Match Pairs).
/// This script handles spawning tiles, tracking matches, scoring, and end-game flow.
/// Designed cleanly so future developers can easily adapt the pair-matching logic.
/// </summary>
public class Masters_TrickyThree_Game_LessonTwo : Masters_Lesson {

    [Header("Game Settings")]
    [Tooltip("How long to show the two selected tiles before flipping them back over (if incorrect).")]
    [SerializeField] private float hideDelay = 1f;
    
    [Header("Memory Game Area")]
    [Tooltip("The parent container with a Grid/Layout group where the tiles spawn.")]
    [SerializeField] private Transform tilesContainer; 
    
    [Tooltip("The memory tile prefab that gets instantiated for every pair item.")]
    [SerializeField] private Masters_TrickyThree_MemoryTile memoryTilePrefab;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    /// <summary>
    /// Struct defining a matching pair of strings (e.g., Question and Answer).
    /// </summary>
    [System.Serializable]
    public class MemoryMatchPair {
        public string offerText;
        public string responseText;
    }

    [Header("Tile Pairs Data")]
    [Tooltip("The list of pairs. Each element spawns two tiles (one for offer, one for response).")]
    [SerializeField] private List<MemoryMatchPair> matchPairs;

    [Header("Game Over / Quiz Complete Settings")]
    [SerializeField] private GameObject quizCompleteGameObject;
    [SerializeField] private GameObject gamePlayGameObject;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button closeButton;

    // Internal State
    private int score;
    private bool isGameActive;
    
    // Tracking active tiles and selections
    private List<Masters_TrickyThree_MemoryTile> allTiles = new List<Masters_TrickyThree_MemoryTile>();
    private Masters_TrickyThree_MemoryTile firstSelectedTile;
    private Masters_TrickyThree_MemoryTile secondSelectedTile;
    
    private int matchedPairsCount = 0;
    private bool isCheckingMatch = false; // Prevents clicking other tiles while waiting for flip animation

    protected override void Start() {
        base.Start();
        
        // Setup end-screen buttons
        if (retryButton != null) retryButton.onClick.AddListener(RetryGame);
        if (closeButton != null) closeButton.onClick.AddListener(CloseGame);
        
        StartGame();
    }

    /// <summary>
    /// Initializes all game variables and triggers the tile spawning phase.
    /// </summary>
    private void StartGame() {
        score = 0;
        isGameActive = true;
        matchedPairsCount = 0;
        isCheckingMatch = false;
        firstSelectedTile = null;
        secondSelectedTile = null;
        
        UpdateUI();
        
        // Swap UI panels
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(true);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(false);
        
        SetupTiles();
    }

    /// <summary>
    /// Cleans up old tiles, builds a list of items to spawn based on the pairs array,
    /// shuffles them randomly, and instantiates the tile prefabs.
    /// </summary>
    private void SetupTiles() {
        // Clear any leftover tiles from previous rounds
        foreach (Transform child in tilesContainer) {
            Destroy(child.gameObject);
        }
        allTiles.Clear();
        
        // Break down pairs into a flat list of individual tiles to spawn
        List<(string matchId, string displayText)> itemsToSpawn = new List<(string, string)>();
        for (int i = 0; i < matchPairs.Count; i++) {
            string matchId = "Pair_" + i; // Used later to check if two tiles belong together
            itemsToSpawn.Add((matchId, matchPairs[i].offerText));
            itemsToSpawn.Add((matchId, matchPairs[i].responseText));
        }
        
        ShuffleList(itemsToSpawn);
        
        // Instantiate prefabs and inject their data
        foreach (var item in itemsToSpawn) {
            Masters_TrickyThree_MemoryTile newTile = Instantiate(memoryTilePrefab, tilesContainer);
            newTile.Setup(item.matchId, item.displayText, this);
            allTiles.Add(newTile);
        }
    }

    /// <summary>
    /// Standard Fisher-Yates shuffle to randomize the board layout.
    /// </summary>
    private void ShuffleList(List<(string matchId, string displayText)> list) {
        for (int i = 0; i < list.Count; i++) {
            var temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void RetryGame() {
        StartGame();
    }

    private void CloseGame() {
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(true);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(false);
    }

    /// <summary>
    /// Checks if the board is locked (e.g. game over, or waiting for wrong tiles to flip back).
    /// Called by individual tiles before they flip over.
    /// </summary>
    public bool CanSelectTile() {
        return isGameActive && !isCheckingMatch;
    }

    /// <summary>
    /// Fired by a tile when clicked. Caches the first and second selections and kicks off the validation routine.
    /// </summary>
    public void TileSelected(Masters_TrickyThree_MemoryTile selectedTile) {
        if (firstSelectedTile == null) {
            firstSelectedTile = selectedTile;
        } else if (secondSelectedTile == null && selectedTile != firstSelectedTile) {
            secondSelectedTile = selectedTile;
            isCheckingMatch = true; // Lock the board
            StartCoroutine(CheckMatchRoutine());
        }
    }

    /// <summary>
    /// Waits briefly so the user sees both tiles, then compares their hidden Match IDs.
    /// Resolves UI logic and scoring based on the match result.
    /// </summary>
    private IEnumerator CheckMatchRoutine() {
        yield return new WaitForSeconds(hideDelay);
        
        if (firstSelectedTile.GetMatchId() == secondSelectedTile.GetMatchId()) {
            // Match found!
            firstSelectedTile.SetMatched();
            secondSelectedTile.SetMatched();
            score++;
            matchedPairsCount++;
            
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            
            // Check win condition
            if (matchedPairsCount >= matchPairs.Count) {
                GameWon();
            }
        } else {
            // No match - Flip back over
            firstSelectedTile.CloseTile();
            secondSelectedTile.CloseTile();
            
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
        
        // Reset selections and unlock board
        firstSelectedTile = null;
        secondSelectedTile = null;
        isCheckingMatch = false;
        
        UpdateUI();
    }

    private void UpdateUI() {
        if (scoreTMP != null) scoreTMP.text = "Score: " + score;
    }

    private void ShowQuizCompleteScreen() {
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(true);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(false);

        if (retryButton != null) retryButton.gameObject.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(true);

        if (nextButton != null) {
            nextButton.interactable = true;
            NextButtonAnimation();
        }
    }

    private void GameWon() {
        isGameActive = false;
        ShowQuizCompleteScreen();
    }

    public bool IsGameActive() {
        return isGameActive;
    }

    /// <summary>
    /// Hook for the Next Button click at the end of the lesson. Loads the next lesson or ends topic.
    /// </summary>
    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        
        if (nextLessonSO != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            if (topic != Masters_Topic.None) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
