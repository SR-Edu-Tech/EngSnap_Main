using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Masters_WordTrainRound {
    public string firstWord;
    public List<string> remainingWordsInOrder;
    public AudioClip completedAudio;
}

public class Masters_JumbledWords_Game_LessonOne : Masters_Lesson {

    [Header("Game Data")]
    [SerializeField] private Masters_WordTrainRound[] rounds;
    [SerializeField] private float gameDuration = 120f; // e.g., 2 minutes to complete all rounds
    
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI engineWordText;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI completedRoundsTMP;
    [SerializeField] private Masters_LessonSO nextLessonSO;
    
    [Header("Spawning & Carriages")]
    [SerializeField] private RectTransform spawnArea;
    [SerializeField] private Masters_JumbledWords_CarriageItem carriagePrefab;
    [SerializeField] private float carriageDriftSpeed = 150f;
    [SerializeField] private float timeBetweenSpawns = 1.5f;
    
    [Header("Game Over / Quiz Complete Settings")]
    [SerializeField] private GameObject quizCompleteGameObject;
    [SerializeField] private GameObject gamePlayGameObject;
    [SerializeField] private Image[] starImageArray;
    [SerializeField] private Color goldStarColor = Color.yellow;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button closeButton;

    private int currentRoundIndex = 0;
    private int currentWordIndex = 0;
    
    private float timeRemaining;
    private bool isGameActive;
    private Coroutine spawnCoroutine;

    private List<Masters_JumbledWords_CarriageItem> activeCarriages = new List<Masters_JumbledWords_CarriageItem>();
    
    protected override void Start() {
        base.Start();
        if (retryButton != null) retryButton.onClick.AddListener(RetryGame);
        if (closeButton != null) closeButton.onClick.AddListener(CloseGame);
        StartGame();
    }

    private void Update() {
        if (!isGameActive) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0) {
            timeRemaining = 0;
            GameOver();
        }

        UpdateTimerUI();
    }
    
    private void StartGame() {
        currentRoundIndex = 0;
        timeRemaining = gameDuration;
        isGameActive = true;
        
        UpdateRoundsUI();
        UpdateTimerUI();
        
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(true);
        
        StartRound();
    }

    private void RetryGame() {
        ClearCarriages();
        
        if (starImageArray != null) {
            foreach (Image image in starImageArray) {
                if (image != null) image.color = Color.white;
            }
        }

        StartGame();
    }

    private void CloseGame() {
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(true);
        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (closeButton != null) closeButton.gameObject.SetActive(false);

        // Reset stars
        if (starImageArray != null) {
            foreach (Image image in starImageArray) {
                if (image != null) image.color = Color.white;
            }
        }
    }
    
    private void StartRound() {
        if (currentRoundIndex >= rounds.Length) {
            GameOver();
            return;
        }
        
        ClearCarriages();
        if (spawnCoroutine != null) {
            StopCoroutine(spawnCoroutine);
        }
        currentWordIndex = 0;
        
        Masters_WordTrainRound currentRound = rounds[currentRoundIndex];
        if (engineWordText != null) {
            engineWordText.text = currentRound.firstWord;
        }
        
        // Shuffle the words before spawning so they appear in a random order
        List<string> wordsToSpawn = new List<string>(currentRound.remainingWordsInOrder);
        for (int i = 0; i < wordsToSpawn.Count; i++) {
            string temp = wordsToSpawn[i];
            int randomIndex = Random.Range(i, wordsToSpawn.Count);
            wordsToSpawn[i] = wordsToSpawn[randomIndex];
            wordsToSpawn[randomIndex] = temp;
        }

        spawnCoroutine = StartCoroutine(SpawnCarriagesRoutine(wordsToSpawn));
    }

    private IEnumerator SpawnCarriagesRoutine(List<string> wordsToSpawn) {
        foreach (string word in wordsToSpawn) {
            // Wait briefly if game gets paused/ended, though coroutine stopping should handle it
            while (!isGameActive) {
                yield return null;
            }
            SpawnCarriage(word);
            yield return new WaitForSeconds(timeBetweenSpawns);
        }
    }
    
    private void SpawnCarriage(string word) {
        if (carriagePrefab == null || spawnArea == null) return;

        Masters_JumbledWords_CarriageItem carriage = Instantiate(carriagePrefab, spawnArea);
        
        // Spawn entirely on the right edge of the spawn area, slightly off-screen
        float spawnX = spawnArea.rect.xMax + carriage.GetComponent<RectTransform>().rect.width;
        float randomY = Random.Range(spawnArea.rect.yMin, spawnArea.rect.yMax);
        carriage.GetComponent<RectTransform>().anchoredPosition = new Vector2(spawnX, randomY);
        
        carriage.Initialize(word, carriageDriftSpeed, spawnArea, this);
        activeCarriages.Add(carriage);
    }
    
    public void HandleCarriageClicked(Masters_JumbledWords_CarriageItem carriage) {
        if (!isGameActive) return;

        Masters_WordTrainRound currentRound = rounds[currentRoundIndex];
        
        if (currentWordIndex < currentRound.remainingWordsInOrder.Count) {
            string expectedWord = currentRound.remainingWordsInOrder[currentWordIndex];
            
            if (carriage.GetWord() == expectedWord) {
                // Correct word
                carriage.CoupleAndStop();
                activeCarriages.Remove(carriage);
                
                // Add the word to the engine text
                if (engineWordText != null) {
                    engineWordText.text += " " + expectedWord;
                }
                
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                
                currentWordIndex++;
                
                // Check if round is complete
                if (currentWordIndex >= currentRound.remainingWordsInOrder.Count) {
                    StartCoroutine(RoundCompleteCoroutine());
                }
            } else {
                // Incorrect word
                carriage.BounceAndReject();
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                // Time penalty can be added here if desired: timeRemaining -= 5f;
            }
        }
    }
    
    private IEnumerator RoundCompleteCoroutine() {
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        isGameActive = false; // Pause timer and ignore input while audio plays
        
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        
        Masters_WordTrainRound currentRound = rounds[currentRoundIndex];
        if (currentRound.completedAudio != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(currentRound.completedAudio);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(2f);
        }
        
        currentRoundIndex++;
        UpdateRoundsUI();
        
        if (currentRoundIndex >= rounds.Length) {
            // Wait briefly before showing game over if all rounds are completed
            yield return new WaitForSeconds(1f);
            GameOver();
        } else {
            isGameActive = true; // Resume timer for the next round
            StartRound();
        }
    }
    
    private void ClearCarriages() {
        foreach (var c in activeCarriages) {
            if (c != null) Destroy(c.gameObject);
        }
        activeCarriages.Clear();
    }
    
    private void UpdateTimerUI() {
        if (timerTMP != null) {
            timerTMP.text = Mathf.CeilToInt(timeRemaining).ToString() + "s";
        }
    }

    private void UpdateRoundsUI() {
        if (completedRoundsTMP != null) {
            completedRoundsTMP.text = "Completed: " + currentRoundIndex + "/" + rounds.Length;
        }
    }

    private void ShowQuizCompleteScreen() {
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(true);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(false);

        // Light up stars based on number of completed rounds
        int starsEarned = currentRoundIndex;

        if (starImageArray != null) {
            for (int i = 0; i < starImageArray.Length; i++) {
                if (starImageArray[i] != null) {
                    starImageArray[i].color = (i < starsEarned) ? goldStarColor : Color.white;
                }
            }
        }

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
    
    private void GameOver() {
        isGameActive = false;
        if (spawnCoroutine != null) StopCoroutine(spawnCoroutine);
        ClearCarriages();
        ShowQuizCompleteScreen();
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
