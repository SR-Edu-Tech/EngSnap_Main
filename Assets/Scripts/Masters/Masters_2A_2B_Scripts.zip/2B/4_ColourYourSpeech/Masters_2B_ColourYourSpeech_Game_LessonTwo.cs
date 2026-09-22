using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// G02 Idiom Pairs — Memory Match
/// Arcade memory wall of 18 face-down paint pots (9 idioms and 9 book meanings).
/// Flips two pots at a time to pair each idiom with its meaning.
/// Success condition: Student finds all 9 idiom-meaning pairs.
/// </summary>
public class Masters_2B_ColourYourSpeech_Game_LessonTwo : Masters_Lesson {

[System.Serializable]
public class ColourYourSpeech_GameG02MemoryPair {
    public int pairId;
    public string idiomText;          // e.g. "Bite off more than you can chew"
    public string meaningText;        // e.g. "To take on a task that is too big"
    public AudioClip pairAudio;
}

    [Header("G02 9 Idiom-Meaning Pairs")]
    [SerializeField]
    private ColourYourSpeech_GameG02MemoryPair[] memoryPairs;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI subtitleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI flipsTMP;
    [SerializeField]
    private TextMeshProUGUI scoreTMP;

    [Header("18 Card Grid Container")]
    [SerializeField]
    private Transform tilesContainer;
    [SerializeField]
    private Button[] cardButtons; // 18 Card Buttons

    [Header("Feedback Banner")]
    [SerializeField]
    private GameObject feedbackBanner;
    [SerializeField]
    private TextMeshProUGUI feedbackTextTMP;

    [Header("Results & Retry Panel")]
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultTitleTMP;
    [SerializeField]
    private TextMeshProUGUI resultScoreTMP;
    [SerializeField]
    private TextMeshProUGUI resultStatusTMP;
    [SerializeField]
    private Button retryBtn;
    [SerializeField]
    private Button returnHubBtn;

    [Header("Audio References")]
    [SerializeField]
    private AudioClip sfxCardFlip;
    [SerializeField]
    private AudioClip sfxMatchSuccess;
    [SerializeField]
    private AudioClip sfxMatchFail;
    [SerializeField]
    private AudioClip sfxCelebration;

    private int totalFlips = 0;
    private int matchedPairsCount = 0;
    private int score = 0;
    private bool isProcessingTurn = false;

    private int firstFlippedIndex = -1;
    private int secondFlippedIndex = -1;

    private int[] cardPairIds;          // Maps card index (0..17) -> pairId (0..8)
    private string[] cardDisplayTexts;  // Maps card index -> text displayed when flipped
    private bool[] cardIsFaceUp;
    private bool[] cardIsMatched;

    private Image[] cardImages;
    private TextMeshProUGUI[] cardTexts;

    private Color cardBackBgColor = new Color(0.12f, 0.22f, 0.42f, 0.95f);
    private Color cardFrontBgColor = new Color(0.16f, 0.36f, 0.62f, 1f);
    private Color matchedBgColor = new Color(0.13f, 0.65f, 0.32f, 1f);
    private Color wrongBgColor = new Color(0.82f, 0.2f, 0.2f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Game;
        base.Awake();

        PurgeLegacyChildren();
        AutoBindReferences();

        if (cardButtons != null && cardButtons.Length > 0) {
            cardImages = new Image[cardButtons.Length];
            cardTexts = new TextMeshProUGUI[cardButtons.Length];
            for (int i = 0; i < cardButtons.Length; i++) {
                if (cardButtons[i] != null) {
                    int index = i;
                    cardImages[i] = cardButtons[i].GetComponent<Image>();
                    cardTexts[i] = cardButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    cardButtons[i].onClick.RemoveAllListeners();
                    cardButtons[i].onClick.AddListener(() => OnCardClicked(index));
                }
            }
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(StartNewGame);
        }

        if (returnHubBtn != null) {
            returnHubBtn.onClick.RemoveAllListeners();
            returnHubBtn.onClick.AddListener(() => {
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Game);
                }
            });
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }

        if (feedbackBanner != null) {
            feedbackBanner.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();

        PurgeLegacyChildren();
        EnsureHeaderAndTitle();

        if (memoryPairs == null || memoryPairs.Length == 0) {
            PopulateFailsafePairs();
        }

        StartNewGame();
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        AutoBindReferences();

        if (memoryPairs == null || memoryPairs.Length == 0) {
            PopulateFailsafePairs();
        }

        if (headerTMP != null) headerTMP.text = "GAME BRANCH (Memory Wall)";
        if (titleTMP != null) titleTMP.text = "G02 Idiom Pairs — Memory Match";
        if (subtitleTMP != null) subtitleTMP.text = "Flip two pots at a time to pair each idiom with its meaning!";
        if (progressTMP != null) progressTMP.text = "Pairs: 0/9";
        if (flipsTMP != null) flipsTMP.text = "Flips: 0";
        if (scoreTMP != null) scoreTMP.text = "Score: 0";
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer",
            "SpawnArea", "GamePlayGameObject"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(lTrans.gameObject);
                else DestroyImmediate(lTrans.gameObject);
            }
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch") ?? transform.Find("Heading");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "GAME BRANCH (Memory Wall)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "G02 Idiom Pairs — Memory Match";
        }

        if (subtitleTMP == null) {
            Transform sTrans = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Instruction") ?? transform.Find("Subtitle");
            if (sTrans != null) subtitleTMP = sTrans.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP != null) {
            subtitleTMP.text = "Flip two pots at a time to pair each idiom with its meaning!";
        }
    }

    private void AutoBindReferences() {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading"))) headerTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (subtitleTMP == null && (n.Contains("subtitle") || n.Contains("instruction"))) subtitleTMP = tmp;
            else if (progressTMP == null && (n.Contains("progress") || n.Contains("paircount"))) progressTMP = tmp;
            else if (flipsTMP == null && n.Contains("flip")) flipsTMP = tmp;
            else if (scoreTMP == null && n.Contains("score")) scoreTMP = tmp;
            else if (feedbackTextTMP == null && n.Contains("feedback")) feedbackTextTMP = tmp;
        }

        if (tilesContainer == null) {
            Transform tcTrans = transform.Find("TilesContainer") ?? transform.Find("GridContainer") ?? transform.Find("CardGrid");
            if (tcTrans != null) tilesContainer = tcTrans;
        }

        if (tilesContainer != null) {
            Button[] btns = tilesContainer.GetComponentsInChildren<Button>(true);
            if (btns != null && btns.Length > 0) {
                cardButtons = btns;
                cardImages = new Image[cardButtons.Length];
                cardTexts = new TextMeshProUGUI[cardButtons.Length];
                for (int i = 0; i < cardButtons.Length; i++) {
                    cardImages[i] = cardButtons[i].GetComponent<Image>();
                    cardTexts[i] = cardButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
            else if (returnHubBtn == null && (n.Contains("hub") || n.Contains("home"))) returnHubBtn = btn;
            else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) nextButton = btn;
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("ResultPopup");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    public void PopulateFailsafePairs() {
        memoryPairs = new ColourYourSpeech_GameG02MemoryPair[] {
            new ColourYourSpeech_GameG02MemoryPair {
                pairId = 1,
                idiomText = "Bite off more than you can chew",
                meaningText = "To take on a task that is too big"
            },
            new ColourYourSpeech_GameG02MemoryPair {
                pairId = 2,
                idiomText = "Piece of cake",
                meaningText = "A job task or activity that is easy or simple to do"
            },
            new ColourYourSpeech_GameG02MemoryPair {
                pairId = 3,
                idiomText = "Have a blast",
                meaningText = "To enjoy a lot"
            },
            new ColourYourSpeech_GameG02MemoryPair {
                pairId = 4,
                idiomText = "Miss the boat",
                meaningText = "To miss a chance"
            },
            new ColourYourSpeech_GameG02MemoryPair {
                pairId = 5,
                idiomText = "The cat is out of the bag",
                meaningText = "the secret is given away"
            },
            new ColourYourSpeech_GameG02MemoryPair {
                pairId = 6,
                idiomText = "Raining cats and dogs",
                meaningText = "raining heavily"
            },
            new ColourYourSpeech_GameG02MemoryPair {
                pairId = 7,
                idiomText = "Bury the hatchet",
                meaningText = "To make up with someone after an argument"
            },
            new ColourYourSpeech_GameG02MemoryPair {
                pairId = 8,
                idiomText = "A close shave",
                meaningText = "narrowly escape a disaster"
            },
            new ColourYourSpeech_GameG02MemoryPair {
                pairId = 9,
                idiomText = "You scratch my back I will scratch yours",
                meaningText = "You help me and I will help you"
            }
        };
    }

    public void StartNewGame() {
        totalFlips = 0;
        matchedPairsCount = 0;
        score = 0;
        isProcessingTurn = false;
        firstFlippedIndex = -1;
        secondFlippedIndex = -1;

        if (cardButtons == null || cardButtons.Length == 0) {
            AutoBindReferences();
        }

        if (resultPanel != null) resultPanel.SetActive(false);
        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        SetupAndShuffleCards();
        UpdateHUD();
    }

    private void SetupAndShuffleCards() {
        if (cardButtons == null || cardButtons.Length < 18 || memoryPairs == null || memoryPairs.Length < 9) return;

        int totalCards = 18;
        cardPairIds = new int[totalCards];
        cardDisplayTexts = new string[totalCards];
        cardIsFaceUp = new bool[totalCards];
        cardIsMatched = new bool[totalCards];

        // Create 18 card entries (9 idioms + 9 meanings)
        List<CardEntry> entries = new List<CardEntry>();
        for (int p = 0; p < 9; p++) {
            var pair = memoryPairs[p];
            entries.Add(new CardEntry { pairId = pair.pairId, text = pair.idiomText, isIdiom = true });
            entries.Add(new CardEntry { pairId = pair.pairId, text = pair.meaningText, isIdiom = false });
        }

        // Shuffle entries
        for (int i = entries.Count - 1; i > 0; i--) {
            int rand = Random.Range(0, i + 1);
            var temp = entries[i];
            entries[i] = entries[rand];
            entries[rand] = temp;
        }

        // Assign to cards
        for (int i = 0; i < totalCards; i++) {
            cardPairIds[i] = entries[i].pairId;
            cardDisplayTexts[i] = entries[i].text;
            cardIsFaceUp[i] = false;
            cardIsMatched[i] = false;

            if (cardButtons[i] != null) {
                cardButtons[i].interactable = true;
                cardButtons[i].transform.DOKill();
                cardButtons[i].transform.localScale = Vector3.one;

                if (cardImages != null && cardImages[i] != null) {
                    cardImages[i].color = cardBackBgColor;
                }

                if (cardTexts != null && cardTexts[i] != null) {
                    cardTexts[i].text = "?";
                    cardTexts[i].fontSize = 26;
                    cardTexts[i].color = new Color(0.8f, 0.9f, 1f, 0.8f);
                }
            }
        }
    }

    private struct CardEntry {
        public int pairId;
        public string text;
        public bool isIdiom;
    }

    private void OnCardClicked(int cardIndex) {
        if (isProcessingTurn || cardIndex < 0 || cardIndex >= cardIsFaceUp.Length) return;
        if (cardIsFaceUp[cardIndex] || cardIsMatched[cardIndex]) return;

        totalFlips++;
        UpdateHUD();

        FlipCardUp(cardIndex);

        if (firstFlippedIndex == -1) {
            firstFlippedIndex = cardIndex;
        } else if (secondFlippedIndex == -1) {
            secondFlippedIndex = cardIndex;
            isProcessingTurn = true;
            StartCoroutine(EvaluateTurn());
        }
    }

    private void FlipCardUp(int cardIndex) {
        cardIsFaceUp[cardIndex] = true;

        if (sfxCardFlip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxCardFlip);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }

        if (cardButtons[cardIndex] != null) {
            cardButtons[cardIndex].transform.DOKill();
            cardButtons[cardIndex].transform.DOScaleX(0f, 0.12f).OnComplete(() => {
                if (cardImages != null && cardImages[cardIndex] != null) {
                    cardImages[cardIndex].color = cardFrontBgColor;
                }
                if (cardTexts != null && cardTexts[cardIndex] != null) {
                    cardTexts[cardIndex].text = cardDisplayTexts[cardIndex];
                    cardTexts[cardIndex].fontSize = 17;
                    cardTexts[cardIndex].color = Color.white;
                }
                cardButtons[cardIndex].transform.DOScaleX(1f, 0.12f);
            });
        }
    }

    private void FlipCardDown(int cardIndex) {
        cardIsFaceUp[cardIndex] = false;

        if (cardButtons[cardIndex] != null) {
            cardButtons[cardIndex].transform.DOKill();
            cardButtons[cardIndex].transform.DOScaleX(0f, 0.12f).OnComplete(() => {
                if (cardImages != null && cardImages[cardIndex] != null) {
                    cardImages[cardIndex].color = cardBackBgColor;
                }
                if (cardTexts != null && cardTexts[cardIndex] != null) {
                    cardTexts[cardIndex].text = "?";
                    cardTexts[cardIndex].fontSize = 26;
                    cardTexts[cardIndex].color = new Color(0.8f, 0.9f, 1f, 0.8f);
                }
                cardButtons[cardIndex].transform.DOScaleX(1f, 0.12f);
            });
        }
    }

    private IEnumerator EvaluateTurn() {
        yield return new WaitForSeconds(0.45f);

        bool isMatch = (cardPairIds[firstFlippedIndex] == cardPairIds[secondFlippedIndex]);

        if (isMatch) {
            // MATCH FOUND!
            matchedPairsCount++;
            score += 150;
            UpdateHUD();

            cardIsMatched[firstFlippedIndex] = true;
            cardIsMatched[secondFlippedIndex] = true;

            if (sfxMatchSuccess != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxMatchSuccess);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (cardImages != null) {
                if (cardImages[firstFlippedIndex] != null) cardImages[firstFlippedIndex].color = matchedBgColor;
                if (cardImages[secondFlippedIndex] != null) cardImages[secondFlippedIndex].color = matchedBgColor;
            }

            if (cardButtons != null) {
                if (cardButtons[firstFlippedIndex] != null) {
                    cardButtons[firstFlippedIndex].transform.DOKill();
                    cardButtons[firstFlippedIndex].transform.DOScale(Vector3.one * 1.08f, 0.15f).SetLoops(2, LoopType.Yoyo);
                }
                if (cardButtons[secondFlippedIndex] != null) {
                    cardButtons[secondFlippedIndex].transform.DOKill();
                    cardButtons[secondFlippedIndex].transform.DOScale(Vector3.one * 1.08f, 0.15f).SetLoops(2, LoopType.Yoyo);
                }
            }

            ShowFeedback("MATCH FOUND! +150 PTS", true);

            firstFlippedIndex = -1;
            secondFlippedIndex = -1;
            isProcessingTurn = false;

            if (matchedPairsCount >= 9) {
                yield return new WaitForSeconds(1.0f);
                EndGame();
            }
        } else {
            // NOT A MATCH
            if (sfxMatchFail != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxMatchFail);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (cardImages != null) {
                if (cardImages[firstFlippedIndex] != null) cardImages[firstFlippedIndex].color = wrongBgColor;
                if (cardImages[secondFlippedIndex] != null) cardImages[secondFlippedIndex].color = wrongBgColor;
            }

            if (cardButtons != null) {
                if (cardButtons[firstFlippedIndex] != null) {
                    cardButtons[firstFlippedIndex].transform.DOShakePosition(0.2f, new Vector3(6f, 0f, 0f), 10, 90, false, true);
                }
                if (cardButtons[secondFlippedIndex] != null) {
                    cardButtons[secondFlippedIndex].transform.DOShakePosition(0.2f, new Vector3(6f, 0f, 0f), 10, 90, false, true);
                }
            }

            ShowFeedback("Not a match — remember their places!", false);

            yield return new WaitForSeconds(0.9f);

            FlipCardDown(firstFlippedIndex);
            FlipCardDown(secondFlippedIndex);

            firstFlippedIndex = -1;
            secondFlippedIndex = -1;
            isProcessingTurn = false;
        }
    }

    private void ShowFeedback(string msg, bool isSuccess) {
        if (feedbackBanner != null) {
            feedbackBanner.SetActive(true);
            if (feedbackTextTMP != null) {
                feedbackTextTMP.text = msg;
                feedbackTextTMP.color = isSuccess ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.35f, 0.35f);
            }
            feedbackBanner.transform.DOKill();
            feedbackBanner.transform.localScale = Vector3.zero;
            feedbackBanner.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);
        }
    }

    private void EndGame() {
        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = "MEMORY WALL CLEARED!";
            resultTitleTMP.color = new Color(0.15f, 0.85f, 0.4f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Found: 9 / 9 Pairs in {totalFlips} Flips | Score: {score} pts";
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = "Brilliant Memory! You successfully paired all 9 idioms with their book meanings!";
        }

        if (sfxCelebration != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxCelebration);
        }

        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Game);
        }
    }

    private void UpdateHUD() {
        if (progressTMP != null) progressTMP.text = $"Pairs: {matchedPairsCount}/9";
        if (flipsTMP != null) flipsTMP.text = $"Flips: {totalFlips}";
        if (scoreTMP != null) scoreTMP.text = $"Score: {score}";
    }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}

