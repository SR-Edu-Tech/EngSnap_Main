using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// G02 Window Pairs — Memory Match
/// Timed concentration / memory match game for Book 2B Unit 8 (Shopping Time).
/// An arcade memory wall of 18 face-down window cards: 9 shop signs and their 9 plain-English meanings.
/// Flips two cards at a time to pair each sign with what it means.
/// If matched, cards lock face-up and audio reads the sign.
/// Success condition: Student finds all 9 sign-meaning pairs.
/// </summary>
public class Masters_ShoppingTime_Game_LessonTwo : Masters_Lesson {

[System.Serializable]
public class ShoppingTime_GameG02MemoryPair {
    public int pairId;
    public string signText;
    public string meaningText;
    public AudioClip pairAudio;
}

    [Header("G02 9 Shop Sign - Meaning Pairs")]
    [SerializeField] private ShoppingTime_GameG02MemoryPair[] memoryPairs;

    [Header("UI Display References")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI flipsTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI timerTMP;

    [Header("18 Card Grid Container")]
    [SerializeField] private Transform tilesContainer;
    [SerializeField] private Button[] cardButtons; // 18 Card Buttons

    [Header("Feedback Banner")]
    [SerializeField] private GameObject feedbackBanner;
    [SerializeField] private TextMeshProUGUI feedbackTextTMP;

    [Header("Results & Retry Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private TextMeshProUGUI resultStatusTMP;
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button returnHubBtn;

    [Header("Audio References")]
    [SerializeField] private AudioClip introAudio;
    [SerializeField] private AudioClip recapAudio;

    private int totalFlips = 0;
    private int matchedPairsCount = 0;
    private int score = 0;
    private float gameTime = 0f;
    private bool isGameActive = false;
    private bool isProcessingTurn = false;

    private int firstFlippedIndex = -1;
    private int secondFlippedIndex = -1;

    private int[] cardPairIds;
    private string[] cardDisplayTexts;
    private bool[] cardIsFaceUp;
    private bool[] cardIsMatched;

    private Image[] cardImages;
    private TextMeshProUGUI[] cardTexts;

    private readonly Color cardBackBgColor = new Color(0.12f, 0.24f, 0.45f, 0.95f);
    private readonly Color cardFrontBgColor = new Color(0.18f, 0.42f, 0.72f, 1f);
    private readonly Color matchedBgColor = new Color(0.15f, 0.75f, 0.35f, 1f);
    private readonly Color wrongBgColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Game;
        base.Awake();

        AutoBindReferences();
        InitMemoryPairsIfEmpty();
        WireEventListeners();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Game;
        AutoBindReferences();
        WireEventListeners();
        EnsureNextAndBackButtonWired();

        if (introAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(introAudio);
        }

        float introDuration = (introAudio != null && introAudio.length > 0) ? introAudio.length + 0.3f : 1.0f;
        StartCoroutine(StartGameAfterIntro(introDuration));
    }

    private IEnumerator StartGameAfterIntro(float delay) {
        isGameActive = false;
        SetupBoard();
        UpdateHUD();

        if (headerTMP != null) headerTMP.gameObject.SetActive(true);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        yield return new WaitForSeconds(delay);

        isGameActive = true;
    }

    private void Update() {
        if (!isGameActive) return;

        gameTime += Time.deltaTime;
        if (timerTMP != null) {
            int minutes = Mathf.FloorToInt(gameTime / 60f);
            int seconds = Mathf.FloorToInt(gameTime % 60f);
            timerTMP.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }

    private void WireEventListeners() {
        if (cardButtons != null) {
            for (int i = 0; i < cardButtons.Length; i++) {
                int cardIndex = i;
                if (cardButtons[i] != null) {
                    cardButtons[i].onClick.RemoveAllListeners();
                    cardButtons[i].onClick.AddListener(() => OnCardClicked(cardIndex));
                }
            }
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(RestartGame);
        }

        if (returnHubBtn != null) {
            returnHubBtn.onClick.RemoveAllListeners();
            returnHubBtn.onClick.AddListener(OnReturnHubClicked);
        }

        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }

    public void RestartGame() {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        SetupBoard();
        UpdateHUD();
        isGameActive = true;
    }

    private void SetupBoard() {
        totalFlips = 0;
        matchedPairsCount = 0;
        score = 0;
        gameTime = 0f;
        firstFlippedIndex = -1;
        secondFlippedIndex = -1;
        isProcessingTurn = false;

        int totalCards = 18;
        cardPairIds = new int[totalCards];
        cardDisplayTexts = new string[totalCards];
        cardIsFaceUp = new bool[totalCards];
        cardIsMatched = new bool[totalCards];

        List<int> slots = new List<int>();
        for (int i = 0; i < totalCards; i++) {
            slots.Add(i);
        }

        // Shuffle slot positions
        for (int i = 0; i < slots.Count; i++) {
            int temp = slots[i];
            int rand = Random.Range(i, slots.Count);
            slots[i] = slots[rand];
            slots[rand] = temp;
        }

        // Assign 9 pairs into shuffled slots
        for (int p = 0; p < memoryPairs.Length && p < 9; p++) {
            int slot1 = slots[p * 2];
            int slot2 = slots[p * 2 + 1];

            cardPairIds[slot1] = memoryPairs[p].pairId;
            cardDisplayTexts[slot1] = memoryPairs[p].signText;

            cardPairIds[slot2] = memoryPairs[p].pairId;
            cardDisplayTexts[slot2] = memoryPairs[p].meaningText;
        }

        // Initialize visual cards
        CacheCardComponents();

        for (int i = 0; i < totalCards; i++) {
            cardIsFaceUp[i] = false;
            cardIsMatched[i] = false;
            SetCardVisual(i, false, false);
            if (i < cardButtons.Length && cardButtons[i] != null) {
                cardButtons[i].interactable = true;
            }
        }
    }

    private void CacheCardComponents() {
        if (tilesContainer == null) {
            tilesContainer = transform.Find("CardsGrid") ?? transform.Find("TilesContainer") ?? transform.Find("OptionsContainer");
        }

        if (tilesContainer != null && (cardButtons == null || cardButtons.Length != 18 || cardButtons[0] == null)) {
            List<Button> btns = new List<Button>();
            for (int i = 0; i < 18; i++) {
                Transform c = tilesContainer.Find($"MemoryCard_{i}") ?? tilesContainer.Find($"Card_{i}") ?? ((i < tilesContainer.childCount) ? tilesContainer.GetChild(i) : null);
                if (c != null) {
                    Button b = c.GetComponent<Button>();
                    if (b != null) btns.Add(b);
                }
            }
            if (btns.Count == 18) cardButtons = btns.ToArray();
        }

        if (cardButtons != null && cardButtons.Length == 18) {
            cardImages = new Image[18];
            cardTexts = new TextMeshProUGUI[18];

            for (int i = 0; i < 18; i++) {
                if (cardButtons[i] != null) {
                    cardImages[i] = cardButtons[i].GetComponent<Image>();
                    cardTexts[i] = cardButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
        }
    }

    private void SetCardVisual(int cardIndex, bool faceUp, bool matched) {
        if (cardIndex < 0 || cardIndex >= 18) return;

        if (cardTexts != null && cardIndex < cardTexts.Length && cardTexts[cardIndex] != null) {
            cardTexts[cardIndex].text = faceUp ? cardDisplayTexts[cardIndex] : "?";
            cardTexts[cardIndex].fontSize = faceUp ? 18f : 28f;
        }

        if (cardImages != null && cardIndex < cardImages.Length && cardImages[cardIndex] != null) {
            if (matched) {
                cardImages[cardIndex].color = matchedBgColor;
            } else if (faceUp) {
                cardImages[cardIndex].color = cardFrontBgColor;
            } else {
                cardImages[cardIndex].color = cardBackBgColor;
            }
        }
    }

    private void OnCardClicked(int cardIndex) {
        if (!isGameActive || isProcessingTurn) return;
        if (cardIndex < 0 || cardIndex >= 18) return;
        if (cardIsMatched[cardIndex] || cardIsFaceUp[cardIndex]) return;

        StartCoroutine(FlipCard(cardIndex));
    }

    private IEnumerator FlipCard(int cardIndex) {
        isProcessingTurn = true;
        totalFlips++;
        UpdateHUD();

        // Flip animation
        Transform cardTr = (cardButtons != null && cardIndex < cardButtons.Length) ? cardButtons[cardIndex].transform : null;
        if (cardTr != null) {
            yield return cardTr.DOScaleX(0f, 0.15f).WaitForCompletion();
        }

        cardIsFaceUp[cardIndex] = true;
        SetCardVisual(cardIndex, true, false);

        if (cardTr != null) {
            yield return cardTr.DOScaleX(1f, 0.15f).WaitForCompletion();
        }

        if (firstFlippedIndex == -1) {
            // First card flipped
            firstFlippedIndex = cardIndex;
            isProcessingTurn = false;
        } else {
            // Second card flipped
            secondFlippedIndex = cardIndex;
            yield return StartCoroutine(CheckMatch());
        }
    }

    private IEnumerator CheckMatch() {
        int idx1 = firstFlippedIndex;
        int idx2 = secondFlippedIndex;

        bool isMatch = (cardPairIds[idx1] == cardPairIds[idx2]);

        if (isMatch) {
            cardIsMatched[idx1] = true;
            cardIsMatched[idx2] = true;
            matchedPairsCount++;
            score += 100;
            UpdateHUD();

            SetCardVisual(idx1, true, true);
            SetCardVisual(idx2, true, true);

            if (cardButtons != null) {
                if (idx1 < cardButtons.Length && cardButtons[idx1] != null) cardButtons[idx1].interactable = false;
                if (idx2 < cardButtons.Length && cardButtons[idx2] != null) cardButtons[idx2].interactable = false;
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // Play pair voiceover
            int pairId = cardPairIds[idx1];
            ShoppingTime_GameG02MemoryPair pair = GetPairById(pairId);
            if (pair != null && pair.pairAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(pair.pairAudio);
            }

            yield return new WaitForSeconds(0.6f);

            firstFlippedIndex = -1;
            secondFlippedIndex = -1;
            isProcessingTurn = false;

            if (matchedPairsCount >= 9) {
                EndGame();
            }
        } else {
            // Mismatch
            if (cardImages != null) {
                if (idx1 < cardImages.Length && cardImages[idx1] != null) cardImages[idx1].color = wrongBgColor;
                if (idx2 < cardImages.Length && cardImages[idx2] != null) cardImages[idx2].color = wrongBgColor;
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            yield return new WaitForSeconds(0.85f);

            // Flip both cards back face-down
            Transform t1 = (cardButtons != null && idx1 < cardButtons.Length) ? cardButtons[idx1].transform : null;
            Transform t2 = (cardButtons != null && idx2 < cardButtons.Length) ? cardButtons[idx2].transform : null;

            if (t1 != null) t1.DOScaleX(0f, 0.15f);
            if (t2 != null) yield return t2.DOScaleX(0f, 0.15f).WaitForCompletion();

            cardIsFaceUp[idx1] = false;
            cardIsFaceUp[idx2] = false;

            SetCardVisual(idx1, false, false);
            SetCardVisual(idx2, false, false);

            if (t1 != null) t1.DOScaleX(1f, 0.15f);
            if (t2 != null) yield return t2.DOScaleX(1f, 0.15f).WaitForCompletion();

            firstFlippedIndex = -1;
            secondFlippedIndex = -1;
            isProcessingTurn = false;
        }
    }

    private ShoppingTime_GameG02MemoryPair GetPairById(int id) {
        if (memoryPairs == null) return null;
        for (int i = 0; i < memoryPairs.Length; i++) {
            if (memoryPairs[i].pairId == id) return memoryPairs[i];
        }
        return null;
    }

    private void EndGame() {
        isGameActive = false;
        isProcessingTurn = false;

        if (tilesContainer != null) tilesContainer.gameObject.SetActive(false);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

            if (resultTitleTMP != null) {
                resultTitleTMP.text = "WINDOW PAIRS COMPLETE!";
                resultTitleTMP.color = new Color(0.2f, 0.85f, 0.35f, 1f);
            }

            if (resultScoreTMP != null) {
                int minutes = Mathf.FloorToInt(gameTime / 60f);
                int seconds = Mathf.FloorToInt(gameTime % 60f);
                resultScoreTMP.text = $"Pairs: 9/9 | Flips: {totalFlips} | Time: {minutes:00}:{seconds:00} | Score: {score}";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = "Fantastic! You matched all 9 shop window signs with their plain-English meanings!";
            }

            if (returnHubBtn != null) returnHubBtn.gameObject.SetActive(true);
            if (retryBtn != null) retryBtn.gameObject.SetActive(true);
        }

        if (recapAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(recapAudio);
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    public void OnReturnHubClicked() {
        topic = Masters_Topic.Game;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Game;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    private void UpdateHUD() {
        if (progressTMP != null) progressTMP.text = $"Pairs: {matchedPairsCount}/9";
        if (flipsTMP != null) flipsTMP.text = $"Flips: {totalFlips}";
        if (scoreTMP != null) scoreTMP.text = $"Score: {score}";
    }

    public void InitMemoryPairsIfEmpty() {
        if (memoryPairs != null && memoryPairs.Length > 0) return;

        memoryPairs = new ShoppingTime_GameG02MemoryPair[] {
            new ShoppingTime_GameG02MemoryPair { pairId = 1, signText = "Open 24 hrs a day", meaningText = "Open all day and night" },
            new ShoppingTime_GameG02MemoryPair { pairId = 2, signText = "Clearance sale", meaningText = "Selling off stock cheaply" },
            new ShoppingTime_GameG02MemoryPair { pairId = 3, signText = "Closing down sale", meaningText = "Closing down permanently" },
            new ShoppingTime_GameG02MemoryPair { pairId = 4, signText = "Half price sale", meaningText = "Everything costs 50% less" },
            new ShoppingTime_GameG02MemoryPair { pairId = 5, signText = "Buy 1 Get 1 Free", meaningText = "Buy one get another free" },
            new ShoppingTime_GameG02MemoryPair { pairId = 6, signText = "Fixed price", meaningText = "Prices cannot be bargained" },
            new ShoppingTime_GameG02MemoryPair { pairId = 7, signText = "No bargains", meaningText = "The shop will not lower prices" },
            new ShoppingTime_GameG02MemoryPair { pairId = 8, signText = "Cash on delivery", meaningText = "Pay when goods arrive" },
            new ShoppingTime_GameG02MemoryPair { pairId = 9, signText = "Credit cards accepted", meaningText = "You may pay with card" }
        };
    }

    public void AutoBindReferences() {
        if (titleTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("Title") ?? transform.Find("HeaderContainer/LessonTitle") ?? transform.Find("CommonHUD/LessonTitle");
            if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP == null) {
            Transform t = transform.Find("Subtitle") ?? transform.Find("Instruction") ?? transform.Find("HeaderContainer/Subtitle") ?? transform.Find("CommonHUD/Subtitle");
            if (t != null) subtitleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP == null) {
            Transform t = transform.Find("Header") ?? transform.Find("BranchHeader") ?? transform.Find("HeaderContainer/Header");
            if (t != null) headerTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (timerTMP == null) {
            Transform t = transform.Find("TimerTMP") ?? transform.Find("Timer") ?? transform.Find("HeaderContainer/TimerTMP") ?? transform.Find("CommonHUD/TimerTMP");
            if (t != null) timerTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (progressTMP == null) {
            Transform t = transform.Find("ProgressTMP") ?? transform.Find("progression count") ?? transform.Find("Progress") ?? transform.Find("HeaderContainer/ProgressTMP") ?? transform.Find("CommonHUD/ProgressTMP");
            if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (flipsTMP == null) {
            Transform t = transform.Find("FlipsTMP") ?? transform.Find("Flips") ?? transform.Find("HeaderContainer/FlipsTMP") ?? transform.Find("CommonHUD/FlipsTMP");
            if (t != null) flipsTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (scoreTMP == null) {
            Transform t = transform.Find("ScoreTMP") ?? transform.Find("Score") ?? transform.Find("HeaderContainer/ScoreTMP") ?? transform.Find("CommonHUD/ScoreTMP");
            if (t != null) scoreTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (tilesContainer == null) {
            tilesContainer = transform.Find("CardsGrid") ?? transform.Find("TilesContainer") ?? transform.Find("OptionsContainer");
        }

        CacheCardComponents();

        // Feedback Banner
        if (feedbackBanner == null) {
            Transform t = transform.Find("FeedbackBanner") ?? transform.Find("Feedback");
            if (t != null) feedbackBanner = t.gameObject;
        }
        if (feedbackTextTMP == null && feedbackBanner != null) {
            feedbackTextTMP = feedbackBanner.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        // Result Panel
        if (resultPanel == null) {
            Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
            if (t != null) resultPanel = t.gameObject;
        }
        if (resultTitleTMP == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("ResultTitle") ?? resultPanel.transform.Find("Title");
            if (t != null) resultTitleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (resultScoreTMP == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("ResultScore") ?? resultPanel.transform.Find("Score");
            if (t != null) resultScoreTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (resultStatusTMP == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("ResultStatus") ?? resultPanel.transform.Find("Status");
            if (t != null) resultStatusTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (retryBtn == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("RetryButton") ?? resultPanel.transform.Find("RetryBtn");
            if (t != null) retryBtn = t.GetComponent<Button>();
        }
        if (returnHubBtn == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("ReturnHubButton") ?? resultPanel.transform.Find("ReturnHubBtn") ?? resultPanel.transform.Find("NextButton");
            if (t != null) returnHubBtn = t.GetComponent<Button>();
        }
    }
}
