using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_AbbreviationsAndAcronyms_Game_LessonTwo : Masters_Lesson {
    
    [Header("Game Settings")]
    [SerializeField] private int totalRounds = 8;
    [SerializeField] private int roundsToWin = 6;
    [SerializeField] private int livesPerRound = 5;
    [SerializeField] private int totalOptionsCount = 8;
    
    [Header("Pair Data")]
    [SerializeField] private List<Masters_AbbreviationsAndAcronyms_Game_LessonOne.SignCatcherPair> matchPairs;

    [Header("UI References - Text")]
    [SerializeField] private TextMeshProUGUI fullFormTextTMP;
    [SerializeField] private TextMeshProUGUI roundCounterTMP;
    
    [Header("UI References - Containers & Prefabs")]
    [SerializeField] private RectTransform slotsContainer;
    [SerializeField] private Masters_HangmanBlankSlot blankSlotPrefab;
    
    [SerializeField] private RectTransform letterOptionsContainer;
    [SerializeField] private Masters_HangmanLetterOption letterOptionPrefab;
    
    [Header("Lives UI")]
    [SerializeField] private Image[] livesImageArray; // E.g., 5 heart icons

    [Header("Game Over Settings")]
    [SerializeField] private GameObject quizCompleteGameObject;
    [SerializeField] private GameObject gamePlayGameObject;
    [SerializeField] private Image[] starImageArray;
    [SerializeField] private Color goldStarColor = Color.yellow;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private int currentRoundIndex;
    private int roundsWon;
    private int currentLives;
    
    private List<Masters_HangmanBlankSlot> activeSlots = new List<Masters_HangmanBlankSlot>();
    private List<Masters_HangmanLetterOption> activeLetterOptions = new List<Masters_HangmanLetterOption>();

    private bool isInputEnabled;

    protected override void Start() {
        base.Start();
        
        if (retryButton != null) retryButton.onClick.AddListener(RetryGame);
        if (closeButton != null) closeButton.onClick.AddListener(CloseGame);
        
        StartGame();
    }

    private void StartGame() {
        currentRoundIndex = 0;
        roundsWon = 0;
        
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(true);
        
        StartRound();
    }

    private void RetryGame() {
        ClearRoundUI();
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
        
        if (starImageArray != null) {
            foreach (Image image in starImageArray) {
                if (image != null) image.color = Color.white;
            }
        }
    }

    private void StartRound() {
        if (currentRoundIndex >= totalRounds) {
            EndGame();
            return;
        }

        currentRoundIndex++;
        currentLives = livesPerRound;
        isInputEnabled = true;
        
        UpdateRoundUI();
        UpdateLivesUI();
        ClearRoundUI();

        // Pick random pair
        var pair = matchPairs[Random.Range(0, matchPairs.Count)];
        
        if (fullFormTextTMP != null) {
            fullFormTextTMP.text = pair.fullForm;
        }

        string targetAbbr = pair.abbreviation.Replace(" ", "").Replace(".", "").ToUpper();
        
        // Spawn slots
        foreach (char c in targetAbbr) {
            var slot = Instantiate(blankSlotPrefab, slotsContainer);
            slot.Setup(c);
            activeSlots.Add(slot);
        }

        // Prepare letter options (correct letters + distractors)
        List<char> optionsPool = new List<char>();
        foreach (char c in targetAbbr) {
            if (!optionsPool.Contains(c)) {
                optionsPool.Add(c);
            }
        }

        // Add distractors
        string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        int neededDistractors = Mathf.Max(0, totalOptionsCount - optionsPool.Count);
        int addedDistractors = 0;
        
        while (addedDistractors < neededDistractors) {
            char randomChar = alphabet[Random.Range(0, alphabet.Length)];
            if (!optionsPool.Contains(randomChar)) {
                optionsPool.Add(randomChar);
                addedDistractors++;
            }
        }

        // Shuffle options
        for (int i = 0; i < optionsPool.Count; i++) {
            char temp = optionsPool[i];
            int randomIndex = Random.Range(i, optionsPool.Count);
            optionsPool[i] = optionsPool[randomIndex];
            optionsPool[randomIndex] = temp;
        }

        // Spawn letter options
        foreach (char c in optionsPool) {
            var option = Instantiate(letterOptionPrefab, letterOptionsContainer);
            option.Setup(c, this);
            activeLetterOptions.Add(option);
        }
    }

    public void OnLetterOptionClicked(Masters_HangmanLetterOption option, char guessedLetter) {
        if (!isInputEnabled) return;

        bool foundMatch = false;

        foreach (var slot in activeSlots) {
            if (!slot.isRevealed && slot.GetTargetLetter() == guessedLetter) {
                slot.Reveal();
                foundMatch = true;
            }
        }

        if (foundMatch) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct); // Positive blip
            CheckRoundWin();
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect); // Soft thud
            currentLives--;
            UpdateLivesUI();
            
            if (currentLives <= 0) {
                StartCoroutine(RoundLostCoroutine());
            }
        }
    }

    private void CheckRoundWin() {
        bool allRevealed = true;
        foreach (var slot in activeSlots) {
            if (!slot.isRevealed) {
                allRevealed = false;
                break;
            }
        }

        if (allRevealed) {
            roundsWon++;
            StartCoroutine(RoundWonCoroutine());
        }
    }

    private IEnumerator RoundWonCoroutine() {
        isInputEnabled = false;
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct); // Play "SFX_WIN_ROUND" jingle if available
        yield return new WaitForSeconds(1.5f);
        StartRound();
    }

    private IEnumerator RoundLostCoroutine() {
        isInputEnabled = false;
        
        // Reveal the correct answer so they learn it
        foreach (var slot in activeSlots) {
            if (!slot.isRevealed) {
                slot.Reveal();
            }
        }
        
        yield return new WaitForSeconds(2.0f);
        StartRound();
    }

    private void ClearRoundUI() {
        foreach (var slot in activeSlots) {
            if (slot != null) Destroy(slot.gameObject);
        }
        activeSlots.Clear();

        foreach (var option in activeLetterOptions) {
            if (option != null) Destroy(option.gameObject);
        }
        activeLetterOptions.Clear();
    }

    private void UpdateRoundUI() {
        if (roundCounterTMP != null) {
            roundCounterTMP.text = "Round " + currentRoundIndex + " of " + totalRounds;
        }
    }

    private void UpdateLivesUI() {
        if (livesImageArray == null) return;
        
        for (int i = 0; i < livesImageArray.Length; i++) {
            if (livesImageArray[i] != null) {
                livesImageArray[i].enabled = (i < currentLives);
            }
        }
    }

    private void EndGame() {
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(true);
        if (gamePlayGameObject != null) gamePlayGameObject.SetActive(false);

        bool passed = roundsWon >= roundsToWin;
        
        if (passed) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        else Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);

        // Simple stars logic based on ratio of wins
        int starsEarned = passed ? 3 : 0; 
        
        if (starImageArray != null) {
            for (int i = 0; i < starsEarned; i++) {
                if (i < starImageArray.Length && starImageArray[i] != null) {
                    starImageArray[i].color = goldStarColor;
                }
            }
        }

        if (retryButton != null) retryButton.gameObject.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(true);

        if (passed && nextButton != null) {
            nextButton.interactable = true;
            NextButtonAnimation();
        }
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
