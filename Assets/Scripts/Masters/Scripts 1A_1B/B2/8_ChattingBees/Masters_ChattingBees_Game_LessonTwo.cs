using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Game Manager for Chatting Bees - Game Lesson 2 (Memory Match Pairs).
/// This script handles spawning tiles, tracking matches, scoring, and end-game flow.
/// </summary>
public class Masters_ChattingBees_Game_LessonTwo : Masters_Lesson {

    [Header("Game Settings")]
    [Tooltip("How long to show the two selected tiles before flipping them back over (if incorrect).")]
    [SerializeField] protected float hideDelay = 1f;
    
    [Header("Memory Game Area")]
    [Tooltip("The parent container with a Grid/Layout group where the tiles spawn.")]
    [SerializeField] protected Transform tilesContainer; 
    
    [Tooltip("The memory tile prefab that gets instantiated for every pair item.")]
    [SerializeField] protected Masters_ChattingBees_MemoryTile memoryTilePrefab;
    
    [Header("UI Elements")]
    [SerializeField] protected TextMeshProUGUI scoreTMP;
    [SerializeField] protected Masters_LessonSO nextLessonSO;

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
    [SerializeField] protected List<MemoryMatchPair> matchPairs;

    [Header("Game Over / Quiz Complete Settings")]
    [SerializeField] protected GameObject quizCompleteGameObject;
    [SerializeField] protected GameObject gamePlayGameObject;
    [SerializeField] protected Button retryButton;
    [SerializeField] protected Button closeButton;

    // Internal State
    protected int score;
    protected bool isGameActive;
    
    // Tracking active tiles and selections
    protected List<Masters_ChattingBees_MemoryTile> allTiles = new List<Masters_ChattingBees_MemoryTile>();
    protected Masters_ChattingBees_MemoryTile firstSelectedTile;
    protected Masters_ChattingBees_MemoryTile secondSelectedTile;
    
    protected int matchedPairsCount = 0;
    protected bool isCheckingMatch = false; // Prevents clicking other tiles while waiting for flip animation

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
    protected virtual void SetupTiles() {
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
            Masters_ChattingBees_MemoryTile newTile = Instantiate(memoryTilePrefab, tilesContainer);
            newTile.Setup(item.matchId, item.displayText, this);
            allTiles.Add(newTile);
        }
    }

    /// <summary>
    /// Standard Fisher-Yates shuffle to randomize the board layout.
    /// </summary>
    protected void ShuffleList<T>(List<T> list) {
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
    public void TileSelected(Masters_ChattingBees_MemoryTile selectedTile) {
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
    protected virtual IEnumerator CheckMatchRoutine() {
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
