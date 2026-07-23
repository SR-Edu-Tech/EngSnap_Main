using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_StartingConversationWithAStranger_Game_LessonTwo : Masters_Lesson {

    [Header("Game Settings")]
    [SerializeField] private float hideDelay = 1f;
    
    [Header("Memory Game Area")]
    [SerializeField] private Transform tilesContainer; 
    [SerializeField] private Masters_StartingConversationWithAStranger_MemoryTile memoryTilePrefab;
    
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [System.Serializable]
    public class MemoryMatchPair {
        public string offerText;
        public string responseText;
    }

    [Header("Tile Pairs Data")]
    [SerializeField] private List<MemoryMatchPair> matchPairs;

    [Header("Game Over / Quiz Complete Settings")]
    [SerializeField] private GameObject quizCompleteGameObject;
    [SerializeField] private GameObject gamePlayGameObject;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button closeButton;

    private int score;
    private bool isGameActive;
    
    private List<Masters_StartingConversationWithAStranger_MemoryTile> allTiles = new List<Masters_StartingConversationWithAStranger_MemoryTile>();
    private Masters_StartingConversationWithAStranger_MemoryTile firstSelectedTile;
    private Masters_StartingConversationWithAStranger_MemoryTile secondSelectedTile;
    
    private int matchedPairsCount = 0;
    private bool isCheckingMatch = false;

    protected override void Start() {
        base.Start();
        
        if (retryButton != null) {
            retryButton.onClick.AddListener(RetryGame);
        }
        
        if (closeButton != null) {
            closeButton.onClick.AddListener(CloseGame);
        }
        
        StartGame();
    }

    private void StartGame() {
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

    private void SetupTiles() {
        // Clear old tiles
        foreach (Transform child in tilesContainer) {
            Destroy(child.gameObject);
        }
        allTiles.Clear();
        
        // Create pairs
        List<(string matchId, string displayText)> itemsToSpawn = new List<(string, string)>();
        for (int i = 0; i < matchPairs.Count; i++) {
            string matchId = "Pair_" + i;
            itemsToSpawn.Add((matchId, matchPairs[i].offerText));
            itemsToSpawn.Add((matchId, matchPairs[i].responseText));
        }
        
        // Shuffle
        ShuffleList(itemsToSpawn);
        
        // Instantiate
        foreach (var item in itemsToSpawn) {
            Masters_StartingConversationWithAStranger_MemoryTile newTile = Instantiate(memoryTilePrefab, tilesContainer);
            newTile.Setup(item.matchId, item.displayText, this);
            allTiles.Add(newTile);
        }
    }

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



    public bool CanSelectTile() {
        return isGameActive && !isCheckingMatch;
    }

    public void TileSelected(Masters_StartingConversationWithAStranger_MemoryTile selectedTile) {
        if (firstSelectedTile == null) {
            firstSelectedTile = selectedTile;
        } else if (secondSelectedTile == null && selectedTile != firstSelectedTile) {
            secondSelectedTile = selectedTile;
            isCheckingMatch = true;
            StartCoroutine(CheckMatchRoutine());
        }
    }

    private IEnumerator CheckMatchRoutine() {
        yield return new WaitForSeconds(hideDelay);
        
        if (firstSelectedTile.GetMatchId() == secondSelectedTile.GetMatchId()) {
            // Match found
            firstSelectedTile.SetMatched();
            secondSelectedTile.SetMatched();
            score++;
            matchedPairsCount++;
            
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            
            if (matchedPairsCount >= matchPairs.Count) {
                GameWon();
            }
        } else {
            // No match
            firstSelectedTile.CloseTile();
            secondSelectedTile.CloseTile();
            
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
        
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

        if (retryButton != null) {
            retryButton.gameObject.SetActive(true);
        }

        if (closeButton != null) {
            closeButton.gameObject.SetActive(true);
        }

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


