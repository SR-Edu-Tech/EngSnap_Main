using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class Masters_CourtesyCall_Game_LessonTwo : Masters_Lesson {

[System.Serializable]
public class GameG02MemoryPair {
    public int pairId;
    public string sentenceText;    // e.g. "I'm exhausted."
    public string faceIllustration; // e.g. "😩 Tired Face"
    public AudioClip sentenceAudio;
}

    [Header("G02 9 Feeling ↔ Face Pairs (p.12)")]
    [SerializeField]
    private GameG02MemoryPair[] memoryPairs;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI flipsTMP;

    [Header("18 Card Grid Container")]
    [SerializeField]
    private Transform tilesContainer;
    [SerializeField]
    private Button[] cardButtons; // 18 Card Buttons

    [Header("Audio Controls")]
    [SerializeField]
    private Button replayAudioBtn;

    [Header("Results & Retry Panel")]
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultScoreTMP;
    [SerializeField]
    private TextMeshProUGUI resultStatusTMP;
    [SerializeField]
    private Button retryBtn;

    private int totalFlips = 0;
    private int matchedPairsCount = 0;
    private bool isProcessingTurn = false;

    private int firstFlippedIndex = -1;
    private int secondFlippedIndex = -1;

    private int[] cardPairIds;       // Maps card index (0..17) -> pairId (0..8)
    private string[] cardDisplayTexts; // Maps card index -> text to display when flipped
    private bool[] cardIsFaceUp;
    private bool[] cardIsMatched;

    private Image[] cardImages;
    private TextMeshProUGUI[] cardTexts;

    private Color cardBackBgColor = new Color(0.12f, 0.16f, 0.28f, 1f);
    private Color cardFrontBgColor = new Color(0.14f, 0.38f, 0.58f, 1f);
    private Color matchedBgColor = new Color(0.14f, 0.53f, 0.22f, 1f);
    private Color wrongBgColor = new Color(0.71f, 0.15f, 0.15f, 1f);

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;

        PurgeLegacyChildren();
        AutoBindReferences();

        if (cardButtons != null && cardButtons.Length > 0) {
            cardImages = new Image[cardButtons.Length];
            cardTexts = new TextMeshProUGUI[cardButtons.Length];
            for (int i = 0; i < cardButtons.Length; i++) {
                if (cardButtons[i] != null) {
                    int index = i;
                    cardImages[i] = cardButtons[i].GetComponent<Image>();
                    cardTexts[i] = cardButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    cardButtons[i].onClick.RemoveAllListeners();
                    cardButtons[i].onClick.AddListener(() => OnCardClicked(index));
                }
            }
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(OnRetryButtonClicked);
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();

        PurgeLegacyChildren();
        EnsureHeaderAndTitle();

        if (memoryPairs == null || memoryPairs.Length == 0) {
            PopulateFailsafePairs();
        }

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        }

        StartNewMemoryBoard();
    }

    private void OnValidate() {
        if (!Application.isPlaying) {
            UpdateEditorPreview();
        }
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        AutoBindReferences();

        if (memoryPairs == null || memoryPairs.Length == 0) {
            PopulateFailsafePairs();
        }

        if (headerTMP != null) headerTMP.text = "GAME BRANCH (Arcade Memory Wall)";
        if (titleTMP != null) titleTMP.text = "G02 Feelings Faces — Memory Match";
        if (progressTMP != null) progressTMP.text = "0/9 Pairs";
        if (flipsTMP != null) flipsTMP.text = "Flips: 0";
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer",
            "QuizCompleteGameObject", "GamePlayGameObject"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                Destroy(lTrans.gameObject);
            }
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "GAME BRANCH (Arcade Memory Wall)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "G02 Feelings Faces — Memory Match";
        }
    }

    private void AutoBindReferences() {
        if (tilesContainer == null) {
            Transform tc = transform.Find("MemoryWall/GridContainer") ?? transform.Find("TilesContainer") ?? transform.Find("Grid");
            if (tc != null) tilesContainer = tc;
        }

        if (tilesContainer != null) {
            cardButtons = tilesContainer.GetComponentsInChildren<Button>(true);
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("repeat") || n.Contains("audio"))) replayAudioBtn = btn;
            else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
            else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) nextButton = btn;
        }

        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (progressTMP == null && (n.Contains("progresstext") || n.Contains("progress") || n.Contains("counter"))) progressTMP = tmp;
            else if (flipsTMP == null && (n.Contains("flip") || n.Contains("moves"))) flipsTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    private void PopulateFailsafePairs() {
        string baseAudio = "Assets/Audio/2B/2_CourtesyCall/Game/";
        AudioClip ariaClip = Resources.Load<AudioClip>(baseAudio + "VO_G02_ARIA.mp3");
        if (ariaClip != null) narratorSpeech = ariaClip;

        (int id, string sent, string face, string aName)[] samples = new (int, string, string, string)[] {
            (0, "I'm exhausted.", "😩 Tired Face", "VO_G02_P1.mp3"),
            (1, "I'm angry.", "😡 Cross Face", "VO_G02_P2.mp3"),
            (2, "I'm happy.", "😊 Smiling Face", "VO_G02_P3.mp3"),
            (3, "I'm sad.", "😢 Downcast Face", "VO_G02_P4.mp3"),
            (4, "I'm relaxed.", "😌 Calm Face", "VO_G02_P5.mp3"),
            (5, "I'm nervous.", "😰 Worried Face", "VO_G02_P6.mp3"),
            (6, "I'm surprised.", "😲 Wide-Eyed Face", "VO_G02_P7.mp3"),
            (7, "I'm frightened.", "😱 Scared Face", "VO_G02_P8.mp3"),
            (8, "I'm shy.", "🫣 Hiding Face", "VO_G02_P9.mp3")
        };

        memoryPairs = new GameG02MemoryPair[samples.Length];
        for (int i = 0; i < samples.Length; i++) {
            memoryPairs[i] = new GameG02MemoryPair {
                pairId = samples[i].id,
                sentenceText = samples[i].sent,
                faceIllustration = samples[i].face,
                sentenceAudio = Resources.Load<AudioClip>(baseAudio + samples[i].aName)
            };
        }
    }

    private void StartNewMemoryBoard() {
        if (memoryPairs == null || memoryPairs.Length == 0) return;
        int pairCount = memoryPairs.Length;
        int totalCards = pairCount * 2;

        totalFlips = 0;
        matchedPairsCount = 0;
        firstFlippedIndex = -1;
        secondFlippedIndex = -1;
        isProcessingTurn = false;

        cardPairIds = new int[totalCards];
        cardDisplayTexts = new string[totalCards];
        cardIsFaceUp = new bool[totalCards];
        cardIsMatched = new bool[totalCards];

        // Build list of 18 items (9 sentences + 9 face illustrations)
        (int pairId, string text)[] boardItems = new (int, string)[totalCards];
        for (int i = 0; i < pairCount; i++) {
            boardItems[i * 2] = (memoryPairs[i].pairId, memoryPairs[i].sentenceText);
            boardItems[i * 2 + 1] = (memoryPairs[i].pairId, memoryPairs[i].faceIllustration);
        }

        // Shuffle board
        for (int i = totalCards - 1; i > 0; i--) {
            int r = Random.Range(0, i + 1);
            var temp = boardItems[i];
            boardItems[i] = boardItems[r];
            boardItems[r] = temp;
        }

        for (int i = 0; i < totalCards; i++) {
            cardPairIds[i] = boardItems[i].pairId;
            cardDisplayTexts[i] = boardItems[i].text;
            cardIsFaceUp[i] = false;
            cardIsMatched[i] = false;

            if (cardButtons != null && i < cardButtons.Length && cardButtons[i] != null) {
                cardButtons[i].gameObject.SetActive(true);
                cardButtons[i].interactable = true;
                if (cardImages != null && i < cardImages.Length && cardImages[i] != null) {
                    cardImages[i].color = cardBackBgColor;
                }
                if (cardTexts != null && i < cardTexts.Length && cardTexts[i] != null) {
                    cardTexts[i].text = "?";
                    cardTexts[i].color = Color.yellow;
                }
            }
        }

        UpdateMemoryUI();
    }

    public bool CanSelectTile() {
        return !isProcessingTurn;
    }

    public void TileSelected(object tile) {
        // Legacy callback stub
    }

    private void OnCardClicked(int index) {
        if (isProcessingTurn || index < 0 || index >= cardIsFaceUp.Length) return;
        if (cardIsFaceUp[index] || cardIsMatched[index]) return;

        totalFlips++;
        cardIsFaceUp[index] = true;

        // Flip Card Animation & Display
        if (cardButtons != null && index < cardButtons.Length && cardButtons[index] != null) {
            cardButtons[index].transform.DOPunchScale(Vector3.one * 0.15f, 0.25f);
        }
        if (cardImages != null && index < cardImages.Length && cardImages[index] != null) {
            cardImages[index].color = cardFrontBgColor;
        }
        if (cardTexts != null && index < cardTexts.Length && cardTexts[index] != null) {
            cardTexts[index].text = cardDisplayTexts[index];
            cardTexts[index].color = Color.white;
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

            int pId = cardPairIds[index];
            if (memoryPairs != null && pId >= 0 && pId < memoryPairs.Length && memoryPairs[pId].sentenceAudio != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(memoryPairs[pId].sentenceAudio);
            }
        }

        if (firstFlippedIndex < 0) {
            firstFlippedIndex = index;
        } else {
            secondFlippedIndex = index;
            isProcessingTurn = true;
            CheckMatchRoutine();
        }

        UpdateMemoryUI();
    }

    private void CheckMatchRoutine() {
        int idx1 = firstFlippedIndex;
        int idx2 = secondFlippedIndex;

        bool isMatch = (cardPairIds[idx1] == cardPairIds[idx2]);

        if (isMatch) {
            cardIsMatched[idx1] = true;
            cardIsMatched[idx2] = true;
            matchedPairsCount++;

            if (cardImages != null && idx1 < cardImages.Length && cardImages[idx1] != null) cardImages[idx1].color = matchedBgColor;
            if (cardImages != null && idx2 < cardImages.Length && cardImages[idx2] != null) cardImages[idx2].color = matchedBgColor;

            if (cardButtons != null && idx1 < cardButtons.Length && cardButtons[idx1] != null) cardButtons[idx1].interactable = false;
            if (cardButtons != null && idx2 < cardButtons.Length && cardButtons[idx2] != null) cardButtons[idx2].interactable = false;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            firstFlippedIndex = -1;
            secondFlippedIndex = -1;
            isProcessingTurn = false;

            UpdateMemoryUI();

            if (matchedPairsCount >= memoryPairs.Length) {
                CompleteLesson();
            }
        } else {
            if (cardImages != null && idx1 < cardImages.Length && cardImages[idx1] != null) cardImages[idx1].color = wrongBgColor;
            if (cardImages != null && idx2 < cardImages.Length && cardImages[idx2] != null) cardImages[idx2].color = wrongBgColor;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            StartCoroutine(FlipBackUnmatchedRoutine(idx1, idx2));
        }
    }

    private IEnumerator FlipBackUnmatchedRoutine(int idx1, int idx2) {
        yield return new WaitForSeconds(0.8f);

        cardIsFaceUp[idx1] = false;
        cardIsFaceUp[idx2] = false;

        if (cardImages != null && idx1 < cardImages.Length && cardImages[idx1] != null) cardImages[idx1].color = cardBackBgColor;
        if (cardImages != null && idx2 < cardImages.Length && cardImages[idx2] != null) cardImages[idx2].color = cardBackBgColor;

        if (cardTexts != null && idx1 < cardTexts.Length && cardTexts[idx1] != null) {
            cardTexts[idx1].text = "?";
            cardTexts[idx1].color = Color.yellow;
        }
        if (cardTexts != null && idx2 < cardTexts.Length && cardTexts[idx2] != null) {
            cardTexts[idx2].text = "?";
            cardTexts[idx2].color = Color.yellow;
        }

        firstFlippedIndex = -1;
        secondFlippedIndex = -1;
        isProcessingTurn = false;
    }

    private void UpdateMemoryUI() {
        if (progressTMP != null) {
            progressTMP.text = $"{matchedPairsCount}/{memoryPairs.Length} Pairs";
        }

        if (flipsTMP != null) {
            flipsTMP.text = $"Flips: {totalFlips}";
        }
    }

    private void CompleteLesson() {
        bool passed = (matchedPairsCount >= memoryPairs.Length); // Pass condition: all 9 pairs

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            if (resultScoreTMP != null) resultScoreTMP.text = $"All 9 Pairs Found in {totalFlips} Flips!";
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "VICTORY! MEMORY MATCH COMPLETE!" : "TRY AGAIN!";
                resultStatusTMP.color = passed ? Color.green : Color.red;
            }
        }

        if (passed && nextButton != null) {
            nextButton.interactable = true;
            NextButtonAnimation();
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(passed ? Masters_SFX.Correct : Masters_SFX.Incorrect);
        }
    }

    private void OnReplayAudioClicked() {
        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        }
    }

    private void OnRetryButtonClicked() {
        if (resultPanel != null) resultPanel.SetActive(false);
        StartNewMemoryBoard();
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}
