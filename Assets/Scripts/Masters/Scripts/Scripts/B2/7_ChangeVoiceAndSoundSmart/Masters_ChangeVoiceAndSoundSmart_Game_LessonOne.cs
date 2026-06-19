using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_ChangeVoiceAndSoundSmart_Game_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class CardData {
        [TextArea(2, 5)]
        [Tooltip("The text to display on the card (e.g. 'I was invited' or 'She invites me')")]
        public string cardText;
        [Tooltip("Check this box if the sentence is Active. Leave unchecked if it is Passive.")]
        public bool isActiveVoice;
    }

    [Header("Game Data")]
    [SerializeField] private List<CardData> cards;
    [SerializeField] private float roundDuration = 60f;
    [SerializeField] private int startingLives = 3;
    [SerializeField] private int scorePerCorrect = 1; 

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI cardTextTMP;
    [SerializeField] private RectTransform cardRect;
    [SerializeField] private Button activeButton;
    [SerializeField] private Button passiveButton;
    [SerializeField] private Button retryButton;
    
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private GameObject[] lifeIcons;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private int currentCardIndex = 0;
    private int currentScore = 0;
    private int currentLives;
    private float timeRemaining;
    private bool isGameActive = false;
    private bool canClick = false;

    private List<CardData> shuffledCards = new List<CardData>();

    protected override void Awake() {
        base.Awake();
        currentLives = startingLives;
        timeRemaining = roundDuration;
        
        if (activeButton != null) activeButton.onClick.AddListener(() => OnChoiceSelected(true));
        if (passiveButton != null) passiveButton.onClick.AddListener(() => OnChoiceSelected(false));
        if (retryButton != null) {
            retryButton.onClick.AddListener(RestartGame);
            retryButton.gameObject.SetActive(false);
        }

        UpdateHUD();
    }

    protected override void Start() {
        base.Start();
        
        // Hide card initially
        cardRect.localScale = Vector3.zero;
        
        StartGame();
    }

    private void StartGame() {
        if (cards == null || cards.Count == 0) {
            Debug.LogError("No cards assigned to the game!");
            return;
        }

        // Shuffle cards for random order
        shuffledCards = new List<CardData>(cards);
        ShuffleList(shuffledCards);

        if (retryButton != null) retryButton.gameObject.SetActive(false);

        isGameActive = true;
        LoadNextCard();
    }

    private void RestartGame() {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        
        currentLives = startingLives;
        timeRemaining = roundDuration;
        currentScore = 0;
        currentCardIndex = 0;

        foreach (var icon in lifeIcons) {
            if (icon != null) icon.SetActive(true);
        }

        UpdateHUD();

        if (retryButton != null) retryButton.gameObject.SetActive(false);
        if (activeButton != null) activeButton.interactable = true;
        if (passiveButton != null) passiveButton.interactable = true;
        
        if (nextButton != null) nextButton.interactable = false;

        cardRect.localScale = Vector3.zero;
        StartGame();
    }

    private void Update() {
        if (!isGameActive) return;

        if (timeRemaining > 0) {
            timeRemaining -= Time.deltaTime;
            timerTMP.text = Mathf.CeilToInt(timeRemaining).ToString() + "s";

            if (timeRemaining <= 0) {
                timeRemaining = 0;
                timerTMP.text = "0s";
                EndGame();
            }
        }
    }

    private void LoadNextCard() {
        if (currentLives <= 0 || timeRemaining <= 0) {
            EndGame();
            return;
        }

        // If we ran out of cards, reshuffle and keep going!
        if (currentCardIndex >= shuffledCards.Count) {
            ShuffleList(shuffledCards);
            currentCardIndex = 0;
        }

        CardData currentCard = shuffledCards[currentCardIndex];
        currentCardIndex++;

        cardTextTMP.text = currentCard.cardText;
        
        // Pop animation
        cardRect.localScale = Vector3.zero;
        cardRect.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).OnComplete(() => {
            canClick = true;
        });
    }

    private void OnChoiceSelected(bool selectedActive) {
        if (!isGameActive || !canClick) return;
        canClick = false;

        // The card we just showed is at index - 1
        CardData currentCard = shuffledCards[currentCardIndex - 1];

        if (selectedActive == currentCard.isActiveVoice) {
            // Correct
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            currentScore += scorePerCorrect;
            UpdateHUD();
            
            // Shrink out and load next
            cardRect.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => {
                LoadNextCard();
            });
        } else {
            // Wrong
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            cardRect.DOPunchRotation(new Vector3(0, 0, 15f), 0.4f, 10, 1f);
            LoseLife();
            
            // Wait a moment then load next
            Invoke(nameof(HideCardAndLoadNext), 0.5f);
        }
    }

    private void HideCardAndLoadNext() {
        cardRect.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => {
            LoadNextCard();
        });
    }

    private void LoseLife() {
        currentLives--;
        if (currentLives >= 0 && currentLives < lifeIcons.Length) {
            // Hide the life icon
            lifeIcons[currentLives].SetActive(false);
            
            // Shake the screen
            if (Camera.main != null) {
                Camera.main.transform.DOShakePosition(0.3f, 0.5f);
            }
        }

        if (currentLives <= 0) {
            EndGame();
        }
    }

    private void EndGame() {
        isGameActive = false;
        canClick = false;

        cardRect.DOScale(Vector3.zero, 0.3f).OnComplete(() => {
            if (activeButton != null) activeButton.interactable = false;
            if (passiveButton != null) passiveButton.interactable = false;
            if (retryButton != null) retryButton.gameObject.SetActive(true);

            // Score check (> 14 means 15 or more)
            if (currentScore > 14) {
                nextButton.interactable = true;
                NextButtonAnimation();
                cardTextTMP.text = $"You Won!\nScore: {currentScore}";
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            } else {
                cardTextTMP.text = $"Game Over!\nScore: {currentScore}\nNeed > 14";
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            cardRect.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBounce);
        });
    }

    private void UpdateHUD() {
        if (scoreTMP != null) scoreTMP.text = currentScore.ToString();
    }

    private void ShuffleList<T>(List<T> list) {
        for (int i = 0; i < list.Count; i++) {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        
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
