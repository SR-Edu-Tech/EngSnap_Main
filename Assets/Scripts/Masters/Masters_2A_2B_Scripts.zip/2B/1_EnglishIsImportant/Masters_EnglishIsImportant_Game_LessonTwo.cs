using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class Masters_EnglishIsImportant_Game_LessonTwo : Masters_Lesson {

[System.Serializable]
public class G02MemoryPair {
    public string fullForm;      // e.g. "Are not"
    public string contraction;   // e.g. "aren't"
}

    [Header("G02 9 Memory Match Pairs (p.7)")]
    [SerializeField]
    private G02MemoryPair[] matchPairs;

    [Header("UI References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI pairsCountTMP;
    [SerializeField]
    private TextMeshProUGUI movesTMP;

    [Header("18 Card Grid Buttons")]
    [SerializeField]
    private Button[] cardButtons; // Card_00 .. Card_17

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

    [Header("Editor Preview")]
    [Range(0, 8)]
    public int editorPreviewPair = 0;

    [Header("Card Visual Theme")]
    [SerializeField]
    private Color faceDownColor = new Color(0.14f, 0.38f, 0.58f, 1f);  // Medium Blue #235E8A
    [SerializeField]
    private Color faceUpColor = new Color(0.18f, 0.55f, 0.85f, 1f);    // Cyan Blue #2E8CD9
    [SerializeField]
    private Color matchedColor = new Color(0.14f, 0.53f, 0.22f, 1f);   // Emerald Green #248838
    [SerializeField]
    private Color wrongColor = new Color(0.71f, 0.15f, 0.15f, 1f);     // Crimson Red #B52626

    private int matchedPairsCount = 0;
    private int moveCount = 0;
    private bool isProcessingInput = false;

    private int[] cardPairIDs = new int[18];
    private string[] cardTexts = new string[18];
    private bool[] cardIsFaceUp = new bool[18];
    private bool[] cardIsMatched = new bool[18];

    private Image[] cardImages;
    private TextMeshProUGUI[] cardTextTMPs;

    private int firstFlippedIndex = -1;
    private int secondFlippedIndex = -1;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;

        PurgeLegacyChildren();
        AutoBindReferences();

        if (cardButtons != null && cardButtons.Length > 0) {
            cardImages = new Image[cardButtons.Length];
            cardTextTMPs = new TextMeshProUGUI[cardButtons.Length];

            for (int i = 0; i < cardButtons.Length; i++) {
                if (cardButtons[i] != null) {
                    int cardIdx = i;
                    cardImages[i] = cardButtons[i].GetComponent<Image>();
                    cardTextTMPs[i] = cardButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);

                    cardButtons[i].onClick.RemoveAllListeners();
                    cardButtons[i].onClick.AddListener(() => OnCardClicked(cardIdx));
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

        if (matchPairs == null || matchPairs.Length == 0) {
            PopulateFailsafePairs();
        }

        StartCoroutine(StartWithIntroAudioRoutine());
    }

    private void OnValidate() {
        if (!Application.isPlaying) {
            UpdateEditorPreview();
        }
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        AutoBindReferences();

        if (matchPairs == null || matchPairs.Length == 0) {
            PopulateFailsafePairs();
        }

        if (headerTMP != null) headerTMP.text = "🎮 GAME BRANCH (Arcade)";
        if (titleTMP != null) titleTMP.text = "G02 Contraction Pairs — Memory Match";
        if (pairsCountTMP != null) pairsCountTMP.text = "0/9 Pairs";
        if (movesTMP != null) movesTMP.text = "0 Moves";

        if (cardButtons != null && cardButtons.Length >= 18 && matchPairs != null && matchPairs.Length >= 9) {
            for (int i = 0; i < 9; i++) {
                int idxA = i * 2;
                int idxB = i * 2 + 1;

                if (cardButtons[idxA] != null) {
                    cardButtons[idxA].gameObject.SetActive(true);
                    SetButtonText(cardButtons[idxA], matchPairs[i].fullForm);
                }
                if (cardButtons[idxB] != null) {
                    cardButtons[idxB].gameObject.SetActive(true);
                    SetButtonText(cardButtons[idxB], matchPairs[i].contraction);
                }
            }
        }
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                Destroy(lTrans.gameObject);
            }
        }

        for (int i = transform.childCount - 1; i >= 0; i--) {
            Transform child = transform.GetChild(i);
            string cName = child.name.ToLower();
            if (cName.Contains("statement") || cName.Contains("words") || cName.Contains("fillin")) {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "🎮 GAME BRANCH (Arcade)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "G02 Contraction Pairs — Memory Match";
        }
    }

    private void AutoBindReferences() {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> cBtns = new List<Button>();

        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("card") || n.Contains("optionbutton") || n.Contains("chip")) {
                cBtns.Add(btn);
            } else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("repeat") || n.Contains("audio"))) {
                replayAudioBtn = btn;
            } else if (retryBtn == null && n.Contains("retry")) {
                retryBtn = btn;
            } else if (nextButton == null && n.Contains("next")) {
                nextButton = btn;
            }
        }

        if ((cardButtons == null || cardButtons.Length == 0 || System.Array.Exists(cardButtons, b => b == null)) && cBtns.Count >= 18) {
            cardButtons = new Button[18];
            for (int i = 0; i < 18; i++) {
                cardButtons[i] = cBtns[i];
            }
        }

        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (pairsCountTMP == null && (n.Contains("pair") || n.Contains("progress") || n.Contains("score"))) pairsCountTMP = tmp;
            else if (movesTMP == null && n.Contains("move")) movesTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
        }
    }

    private void PopulateFailsafePairs() {
        (string full, string contr)[] samplePairs = new (string, string)[] {
            ("Are not", "aren't"),
            ("Cannot", "can't"),
            ("Did not", "didn't"),
            ("I am", "I'm"),
            ("I have", "I've"),
            ("They are", "they're"),
            ("We are", "we're"),
            ("Will not", "won't"),
            ("You have", "you've")
        };

        matchPairs = new G02MemoryPair[samplePairs.Length];
        for (int i = 0; i < samplePairs.Length; i++) {
            matchPairs[i] = new G02MemoryPair {
                fullForm = samplePairs[i].full,
                contraction = samplePairs[i].contr
            };
        }
    }

    private IEnumerator StartWithIntroAudioRoutine() {
        isProcessingInput = true;

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            float delay = narratorSpeech.length > 0 ? narratorSpeech.length : 2.5f;
            yield return new WaitForSeconds(delay);
        }

        StartLesson();
    }

    public void StartLesson() {
        matchedPairsCount = 0;
        moveCount = 0;
        firstFlippedIndex = -1;
        secondFlippedIndex = -1;
        isProcessingInput = false;

        if (resultPanel != null) resultPanel.SetActive(false);

        UpdateUI();
        InitializeCardDeck();
    }

    private void InitializeCardDeck() {
        if (matchPairs == null || matchPairs.Length < 9) PopulateFailsafePairs();

        List<int> pIDs = new List<int>();
        List<string> pTexts = new List<string>();

        for (int i = 0; i < 9; i++) {
            pIDs.Add(i);
            pTexts.Add(matchPairs[i].fullForm);

            pIDs.Add(i);
            pTexts.Add(matchPairs[i].contraction);
        }

        // Randomize deck deterministically
        System.Random rng = new System.Random(42);
        for (int i = pIDs.Count - 1; i > 0; i--) {
            int k = rng.Next(i + 1);

            int tempID = pIDs[i];
            pIDs[i] = pIDs[k];
            pIDs[k] = tempID;

            string tempTxt = pTexts[i];
            pTexts[i] = pTexts[k];
            pTexts[k] = tempTxt;
        }

        for (int i = 0; i < 18; i++) {
            cardPairIDs[i] = pIDs[i];
            cardTexts[i] = pTexts[i];
            cardIsFaceUp[i] = false;
            cardIsMatched[i] = false;

            SetCardVisual(i, false, false, false);
        }
    }

    private void SetCardVisual(int index, bool faceUp, bool matched, bool wrong) {
        if (cardButtons == null || index >= cardButtons.Length || cardButtons[index] == null) return;

        Button btn = cardButtons[index];
        Image img = cardImages != null && index < cardImages.Length ? cardImages[index] : btn.GetComponent<Image>();
        TextMeshProUGUI tmp = cardTextTMPs != null && index < cardTextTMPs.Length ? cardTextTMPs[index] : btn.GetComponentInChildren<TextMeshProUGUI>(true);

        if (matched) {
            if (img != null) img.color = matchedColor;
            if (tmp != null) {
                tmp.text = cardTexts[index];
                tmp.color = Color.white;
            }
            btn.interactable = false;
        } else if (wrong) {
            if (img != null) img.color = wrongColor;
            if (tmp != null) {
                tmp.text = cardTexts[index];
                tmp.color = Color.white;
            }
        } else if (faceUp) {
            if (img != null) img.color = faceUpColor;
            if (tmp != null) {
                tmp.text = cardTexts[index];
                tmp.color = Color.white;
            }
            btn.interactable = false;
        } else {
            if (img != null) img.color = faceDownColor;
            if (tmp != null) {
                tmp.text = "?";
                tmp.color = Color.yellow;
            }
            btn.interactable = true;
        }
    }

    private void UpdateUI() {
        if (pairsCountTMP != null) pairsCountTMP.text = $"{matchedPairsCount}/9 Pairs";
        if (movesTMP != null) movesTMP.text = $"{moveCount} Moves";
    }

    private void OnCardClicked(int index) {
        if (isProcessingInput || index < 0 || index >= 18) return;
        if (cardIsMatched[index] || cardIsFaceUp[index]) return;

        if (firstFlippedIndex == -1) {
            // First card flip
            firstFlippedIndex = index;
            cardIsFaceUp[index] = true;
            SetCardVisual(index, true, false, false);

            if (cardButtons[index] != null) {
                cardButtons[index].transform.DOPunchScale(Vector3.one * 0.15f, 0.25f);
            }
        } else if (secondFlippedIndex == -1 && index != firstFlippedIndex) {
            // Second card flip
            secondFlippedIndex = index;
            cardIsFaceUp[index] = true;
            SetCardVisual(index, true, false, false);

            moveCount++;
            UpdateUI();

            if (cardButtons[index] != null) {
                cardButtons[index].transform.DOPunchScale(Vector3.one * 0.15f, 0.25f);
            }

            CheckCardMatch();
        }
    }

    private void CheckCardMatch() {
        isProcessingInput = true;

        int idA = cardPairIDs[firstFlippedIndex];
        int idB = cardPairIDs[secondFlippedIndex];

        if (idA == idB) {
            // Valid Match
            matchedPairsCount++;
            UpdateUI();

            cardIsMatched[firstFlippedIndex] = true;
            cardIsMatched[secondFlippedIndex] = true;

            SetCardVisual(firstFlippedIndex, true, true, false);
            SetCardVisual(secondFlippedIndex, true, true, false);

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            firstFlippedIndex = -1;
            secondFlippedIndex = -1;
            isProcessingInput = false;

            if (matchedPairsCount >= 9) {
                CompleteLesson();
            }
        } else {
            // Mismatch
            SetCardVisual(firstFlippedIndex, true, false, true);
            SetCardVisual(secondFlippedIndex, true, false, true);

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            StartCoroutine(FlipBackRoutine(firstFlippedIndex, secondFlippedIndex));
        }
    }

    private IEnumerator FlipBackRoutine(int idxA, int idxB) {
        yield return new WaitForSeconds(0.8f);

        cardIsFaceUp[idxA] = false;
        cardIsFaceUp[idxB] = false;

        SetCardVisual(idxA, false, false, false);
        SetCardVisual(idxB, false, false, false);

        firstFlippedIndex = -1;
        secondFlippedIndex = -1;
        isProcessingInput = false;
    }

    private void SetButtonText(Button btn, string textValue) {
        if (btn == null) return;
        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null) {
            tmp.text = textValue;
            tmp.enabled = true;
            tmp.gameObject.SetActive(true);
        }
    }

    private void OnReplayAudioClicked() {
        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        }
    }

    private void OnRetryButtonClicked() {
        StartLesson();
    }

    private void CompleteLesson() {
        isProcessingInput = true;

        if (resultPanel != null) {
            resultPanel.SetActive(true);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"9/9 Pairs ({moveCount} Moves)";
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = "EXCELLENT! ALL 9 MATCHES FOUND!";
        }

        if (nextButton != null) {
            nextButton.interactable = true;
            NextButtonAnimation();
        }

        Debug.Log($"[G02 Game] Completed all 9 pairs in {moveCount} moves!");
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}
