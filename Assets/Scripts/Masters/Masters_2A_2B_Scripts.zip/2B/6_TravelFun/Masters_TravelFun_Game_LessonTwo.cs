using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



public class Masters_TravelFun_Game_LessonTwo : Masters_Lesson {

[System.Serializable]
public class TravelFun_MemoryPair {
    public int pairId;
    public string phraseText;       // Card 1
    public string meaningText;      // Card 2
    public AudioClip pairAudio;     // Full pair audio readback
}

    [Header("G02 Memory Game Settings")]
    [SerializeField]
    private TravelFun_MemoryPair[] pairs = new TravelFun_MemoryPair[] {
        new TravelFun_MemoryPair { pairId = 1, phraseText = "set off", meaningText = "Start a journey" },
        new TravelFun_MemoryPair { pairId = 2, phraseText = "see off", meaningText = "Go to the station to say goodbye" },
        new TravelFun_MemoryPair { pairId = 3, phraseText = "hold up", meaningText = "Delay when travelling" },
        new TravelFun_MemoryPair { pairId = 4, phraseText = "check in", meaningText = "Arrive & register at a hotel" },
        new TravelFun_MemoryPair { pairId = 5, phraseText = "check out", meaningText = "Pay the bill & leave hotel" },
        new TravelFun_MemoryPair { pairId = 6, phraseText = "get on", meaningText = "Enter a bus or train" },
        new TravelFun_MemoryPair { pairId = 7, phraseText = "get off", meaningText = "Leave a bus or train" },
        new TravelFun_MemoryPair { pairId = 8, phraseText = "take off", meaningText = "Leave ground & fly" },
        new TravelFun_MemoryPair { pairId = 9, phraseText = "touch down", meaningText = "Land on the ground" }
    };

    [Header("UI References")]
    [SerializeField]
    private TextMeshProUGUI branchTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;

    [Header("18 Cards Grid Container")]
    [SerializeField]
    private GameObject cardsGridContainer;
    [SerializeField]
    private Button[] cardButtons = new Button[0]; // 18 buttons
    [SerializeField]
    private Image[] cardImages = new Image[0];
    [SerializeField]
    private TextMeshProUGUI[] cardTexts = new TextMeshProUGUI[0];

    [Header("Results Panel")]
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

    [Header("Colors & Styling")]
    [SerializeField]
    private Color faceDownColor = new Color(0.12f, 0.28f, 0.48f, 1f);
    [SerializeField]
    private Color faceUpColor = new Color(0.12f, 0.52f, 0.72f, 1f);
    [SerializeField]
    private Color matchedColor = new Color(0.12f, 0.72f, 0.35f, 1f);
    [SerializeField]
    private Color mismatchColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    private class CardItem {
        public int cardIndex;
        public int pairId;
        public string text;
        public bool isPhrase; // true = phrase, false = meaning
        public bool isMatched;
        public bool isFaceUp;
    }

    private List<CardItem> activeCards = new List<CardItem>();
    private CardItem firstSelected = null;
    private CardItem secondSelected = null;
    private bool isCheckingMatch = false;
    private int matchedPairsCount = 0;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;

        AutoBindReferences();

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(OnRetryButtonClicked);
        }

        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(HandleNextButtonClicked);
            nextButton.gameObject.SetActive(false);
        }

        if (resultPanel != null) resultPanel.SetActive(false);
    }

    protected override void Start() {
        base.Start();
        AutoBindReferences();
        StartCoroutine(StartWithIntroRoutine());
    }

    private void AutoBindReferences() {
        if (branchTMP == null) {
            Transform b = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header");
            if (b != null) branchTMP = b.GetComponent<TextMeshProUGUI>();
        }
        if (branchTMP != null) branchTMP.text = "GAME BRANCH (Boarding Gate Wall)";

        if (titleTMP == null) {
            Transform t = transform.Find("HeaderContainer/Title") ?? transform.Find("LessonTitle");
            if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) titleTMP.text = "G02 Travel Pairs — Memory Match";

        if (progressTMP == null) {
            Transform p = transform.Find("HeaderContainer/Progress") ?? transform.Find("Progress");
            if (p != null) progressTMP = p.GetComponent<TextMeshProUGUI>();
        }

        if (cardsGridContainer == null) {
            Transform cg = transform.Find("CardsGrid") ?? transform.Find("GridContainer") ?? transform.Find("Gameelemetes");
            if (cg != null) cardsGridContainer = cg.gameObject;
        }

        if (cardsGridContainer != null) {
            List<Button> bList = new List<Button>();
            List<Image> iList = new List<Image>();
            List<TextMeshProUGUI> tList = new List<TextMeshProUGUI>();

            for (int i = 0; i < cardsGridContainer.transform.childCount; i++) {
                Transform cTr = cardsGridContainer.transform.GetChild(i);
                Button btn = cTr.GetComponent<Button>();
                if (btn != null) {
                    int idx = bList.Count;
                    bList.Add(btn);
                    iList.Add(cTr.GetComponent<Image>());
                    TextMeshProUGUI txt = cTr.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null) {
                        txt.enableAutoSizing = true;
                        txt.fontSizeMin = 12;
                        txt.fontSizeMax = 18;
                    }
                    tList.Add(txt);

                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnCardClicked(idx));
                }
            }

            cardButtons = bList.ToArray();
            cardImages = iList.ToArray();
            cardTexts = tList.ToArray();
        }
    }

    private IEnumerator StartWithIntroRoutine() {
        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            float delay = narratorSpeech.length > 0 ? narratorSpeech.length : 3.0f;
            yield return new WaitForSeconds(delay);
        } else {
            yield return new WaitForSeconds(0.4f);
        }

        StartMemoryGame();
    }

    private void StartMemoryGame() {
        matchedPairsCount = 0;
        firstSelected = null;
        secondSelected = null;
        isCheckingMatch = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        // Build and shuffle 18 cards
        activeCards.Clear();
        for (int i = 0; i < pairs.Length; i++) {
            var p = pairs[i];
            activeCards.Add(new CardItem { pairId = p.pairId, text = p.phraseText, isPhrase = true });
            activeCards.Add(new CardItem { pairId = p.pairId, text = p.meaningText, isPhrase = false });
        }

        // Fisher-Yates Shuffle
        for (int i = 0; i < activeCards.Count; i++) {
            int rnd = Random.Range(i, activeCards.Count);
            var temp = activeCards[i];
            activeCards[i] = activeCards[rnd];
            activeCards[rnd] = temp;
        }

        // Setup 18 Card visual slots
        for (int i = 0; i < activeCards.Count && i < cardButtons.Length; i++) {
            activeCards[i].cardIndex = i;
            activeCards[i].isMatched = false;
            activeCards[i].isFaceUp = false;

            if (cardButtons[i] != null) {
                cardButtons[i].interactable = true;
                cardButtons[i].gameObject.SetActive(true);
            }
            if (cardImages[i] != null) {
                cardImages[i].color = faceDownColor;
            }
            if (cardTexts[i] != null) {
                cardTexts[i].text = "✈️";
                cardTexts[i].color = new Color(0.7f, 0.85f, 1f);
            }
        }

        UpdateProgressDisplay();
    }

    private void UpdateProgressDisplay() {
        if (progressTMP != null) {
            progressTMP.text = $"Matched: {matchedPairsCount} / {pairs.Length} Pairs";
        }
    }

    private void OnCardClicked(int cardIndex) {
        if (isCheckingMatch || cardIndex < 0 || cardIndex >= activeCards.Count) return;

        CardItem clicked = activeCards[cardIndex];
        if (clicked.isMatched || clicked.isFaceUp) return;

        // Flip Face-Up
        FlipCardVisual(clicked, true);

        if (firstSelected == null) {
            firstSelected = clicked;
        } else if (secondSelected == null) {
            secondSelected = clicked;
            StartCoroutine(CheckMatchRoutine(firstSelected, secondSelected));
        }
    }

    private void FlipCardVisual(CardItem card, bool faceUp) {
        card.isFaceUp = faceUp;
        int idx = card.cardIndex;

        if (cardButtons[idx] != null) {
            cardButtons[idx].transform.DORotate(new Vector3(0f, 90f, 0f), 0.15f).OnComplete(() => {
                if (faceUp) {
                    if (cardImages[idx] != null) cardImages[idx].color = faceUpColor;
                    if (cardTexts[idx] != null) {
                        cardTexts[idx].text = card.isPhrase ? $"<b><color=#FFD700>[ {card.text} ]</color></b>" : card.text;
                        cardTexts[idx].color = Color.white;
                    }
                } else {
                    if (cardImages[idx] != null) cardImages[idx].color = faceDownColor;
                    if (cardTexts[idx] != null) {
                        cardTexts[idx].text = "✈️";
                        cardTexts[idx].color = new Color(0.7f, 0.85f, 1f);
                    }
                }
                cardButtons[idx].transform.DORotate(Vector3.zero, 0.15f);
            });
        }
    }

    private IEnumerator CheckMatchRoutine(CardItem c1, CardItem c2) {
        isCheckingMatch = true;

        bool isMatch = (c1.pairId == c2.pairId && c1.isPhrase != c2.isPhrase);

        yield return new WaitForSeconds(0.4f);

        if (isMatch) {
            // Match found!
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            c1.isMatched = true;
            c2.isMatched = true;
            matchedPairsCount++;
            UpdateProgressDisplay();

            // Lock green and punch scale
            if (cardImages[c1.cardIndex] != null) cardImages[c1.cardIndex].color = matchedColor;
            if (cardImages[c2.cardIndex] != null) cardImages[c2.cardIndex].color = matchedColor;

            if (cardButtons[c1.cardIndex] != null) {
                cardButtons[c1.cardIndex].transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.3f, 5, 0.5f);
                cardButtons[c1.cardIndex].interactable = false;
            }
            if (cardButtons[c2.cardIndex] != null) {
                cardButtons[c2.cardIndex].transform.DOPunchScale(new Vector3(0.12f, 0.12f, 0f), 0.3f, 5, 0.5f);
                cardButtons[c2.cardIndex].interactable = false;
            }

            // Play Pair Readback Audio if available
            int pIdx = c1.pairId - 1;
            if (pIdx >= 0 && pIdx < pairs.Length && pairs[pIdx].pairAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(pairs[pIdx].pairAudio);
                float delay = pairs[pIdx].pairAudio.length > 0 ? pairs[pIdx].pairAudio.length : 2.5f;
                yield return new WaitForSeconds(delay + 0.2f);
            } else {
                yield return new WaitForSeconds(0.5f);
            }

            firstSelected = null;
            secondSelected = null;
            isCheckingMatch = false;

            if (matchedPairsCount >= pairs.Length) {
                ShowGameComplete();
            }
        } else {
            // Mismatch!
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (cardImages[c1.cardIndex] != null) cardImages[c1.cardIndex].color = mismatchColor;
            if (cardImages[c2.cardIndex] != null) cardImages[c2.cardIndex].color = mismatchColor;

            if (cardButtons[c1.cardIndex] != null) cardButtons[c1.cardIndex].transform.DOShakePosition(0.3f, new Vector3(8f, 0f, 0f), 10, 90f);
            if (cardButtons[c2.cardIndex] != null) cardButtons[c2.cardIndex].transform.DOShakePosition(0.3f, new Vector3(8f, 0f, 0f), 10, 90f);

            yield return new WaitForSeconds(0.8f);

            FlipCardVisual(c1, false);
            FlipCardVisual(c2, false);

            yield return new WaitForSeconds(0.3f);

            firstSelected = null;
            secondSelected = null;
            isCheckingMatch = false;
        }
    }

    private void ShowGameComplete() {
        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = "ALL PAIRS MATCHED! 🌟";
            resultTitleTMP.color = new Color(0.2f, 0.95f, 0.4f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Score: {matchedPairsCount} / {pairs.Length} Pairs Found";
            resultScoreTMP.color = new Color(1f, 0.9f, 0.2f);
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = "“Outstanding memory! You paired all 9 travel phrases with their definitions!”";
            resultStatusTMP.color = Color.white;
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
        }

        if (retryBtn != null) {
            retryBtn.gameObject.SetActive(false);
        }
    }

    private void OnRetryButtonClicked() {
        if (resultPanel != null) resultPanel.SetActive(false);
        StartMemoryGame();
    }

    private void HandleNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}

