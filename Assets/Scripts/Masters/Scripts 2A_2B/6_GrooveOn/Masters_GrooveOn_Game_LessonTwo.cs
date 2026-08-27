using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum Masters_CardType {
    Festival,
    Greeting
}

/// <summary>
/// Data structure for a G02 Memory Card.
/// </summary>
[System.Serializable]
public class Masters_MemoryCardData {
    public int pairId;
    public string cardText;
    public Masters_CardType cardType;
    public AudioClip cardAudio;
}

/// <summary>
/// Subclass / Game Controller for Unit 6 (Groove On) G02: FESTIVAL MATCH — Festival <-> Greeting Memory Game.
/// - 16 face-down cards in a 4x4 responsive grid (8 Festival Cards + 8 Greeting Cards)
/// - 8 Verbatim Pairs: Diwali, Christmas, Easter, Eid, New Year, Independence Day, Gandhi Jayanti, Guru Nanak Jayanti
/// - Smooth 3D flip animation (Face-Down <-> Face-Up)
/// - Input-blocked pair checking logic
/// - Audio, Match Chime, Gold Sparkle Glow, Score/Time tracking, Result Popup, Retry & Return to Hub.
/// </summary>
[ExecuteAlways]
public class Masters_GrooveOn_Game_LessonTwo : Masters_Lesson {

    public enum GameState {
        Waiting,
        OneCardFlipped,
        CheckingPair,
        Completed
    }

    /// <summary>
    /// Helper component for individual memory card UI & DOTween flip animations.
    /// </summary>
    public class MemoryCardUI : MonoBehaviour {
        public int index;
        public Masters_MemoryCardData data;
        public bool isFaceUp = false;
        public bool isMatched = false;

        public Image cardImage;
        public TextMeshProUGUI cardTMP;
        public Button button;

        private Masters_GrooveOn_Game_LessonTwo controller;

        public void Init(Masters_GrooveOn_Game_LessonTwo mainController, int cardIndex) {
            controller = mainController;
            index = cardIndex;

            button = GetComponent<Button>();
            if (button == null) button = gameObject.AddComponent<Button>();

            cardImage = GetComponent<Image>();
            if (cardImage == null) cardImage = gameObject.AddComponent<Image>();
            cardImage.raycastTarget = true;

            cardTMP = GetComponentInChildren<TextMeshProUGUI>(true);
            if (cardTMP == null) {
                GameObject tObj = new GameObject("CardTMP");
                tObj.transform.SetParent(transform, false);
                RectTransform tRect = tObj.AddComponent<RectTransform>();
                tRect.anchorMin = Vector2.zero;
                tRect.anchorMax = Vector2.one;
                tRect.offsetMin = new Vector2(8f, 8f);
                tRect.offsetMax = new Vector2(-8f, -8f);
                cardTMP = tObj.AddComponent<TextMeshProUGUI>();
            }

            cardTMP.raycastTarget = false;
            cardTMP.enableWordWrapping = true;
            cardTMP.enableAutoSizing = true;
            cardTMP.fontSizeMin = 11;
            cardTMP.fontSizeMax = 20;
            cardTMP.alignment = TextAlignmentOptions.Center;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnCardClicked);
        }

        public void SetupData(Masters_MemoryCardData cardData) {
            data = cardData;
            isFaceUp = false;
            isMatched = false;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            ApplyFaceDownStyle();
        }

        private void ApplyFaceDownStyle() {
            if (cardImage != null) {
                cardImage.color = new Color(0.12f, 0.25f, 0.48f, 1f); // Deep Royal Blue face-down
            }
            if (cardTMP != null) {
                cardTMP.text = "?";
                cardTMP.color = new Color(1f, 0.85f, 0.3f, 0.8f);
            }
        }

        private void ApplyFaceUpStyle() {
            if (data == null) return;

            if (cardImage != null) {
                if (data.cardType == Masters_CardType.Festival) {
                    cardImage.color = new Color(0.85f, 0.47f, 0.02f, 1f); // Amber / Gold for Festival
                } else {
                    cardImage.color = new Color(0.49f, 0.23f, 0.93f, 1f); // Vibrant Purple for Greeting
                }
            }

            if (cardTMP != null) {
                cardTMP.text = data.cardText;
                cardTMP.color = Color.white;
            }
        }

        public void FlipToFaceUp(System.Action onComplete = null) {
            if (isFaceUp) return;
            isFaceUp = true;

            transform.DOKill();
            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DORotate(new Vector3(0, 90, 0), 0.15f).SetEase(Ease.InQuad));
            seq.AppendCallback(() => {
                ApplyFaceUpStyle();
            });
            seq.Append(transform.DORotate(new Vector3(0, 0, 0), 0.15f).SetEase(Ease.OutQuad));
            seq.OnComplete(() => {
                onComplete?.Invoke();
            });
        }

        public void FlipToFaceDown(System.Action onComplete = null) {
            if (!isFaceUp || isMatched) return;
            isFaceUp = false;

            transform.DOKill();
            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DORotate(new Vector3(0, 90, 0), 0.15f).SetEase(Ease.InQuad));
            seq.AppendCallback(() => {
                ApplyFaceDownStyle();
            });
            seq.Append(transform.DORotate(new Vector3(0, 0, 0), 0.15f).SetEase(Ease.OutQuad));
            seq.OnComplete(() => {
                onComplete?.Invoke();
            });
        }

        public void AnimateMatch() {
            isMatched = true;
            isFaceUp = true;
            transform.DOKill();

            if (cardImage != null) {
                cardImage.DOColor(new Color(1f, 0.92f, 0.35f, 1f), 0.25f); // Sparkle Gold Highlight
            }
            transform.DOPunchScale(Vector3.one * 0.2f, 0.35f);
        }

        public void AnimateMismatch() {
            transform.DOKill();
            transform.DOShakePosition(0.35f, new Vector3(12f, 0, 0));
        }

        private void OnCardClicked() {
            if (controller != null) {
                controller.OnCardSelected(this);
            }
        }
    }

    [Header("G02 Memory Game Configuration")]
    [SerializeField] private int totalPairs = 8;
    [SerializeField] private Transform gridContainer;

    [Header("UI Text Indicators")]
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI matchesTMP;
    [SerializeField] private TextMeshProUGUI attemptsTMP;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private GameObject resultPopup;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultStatusTMP;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button returnHubButton;

    private List<Masters_MemoryCardData> cardDeck = new List<Masters_MemoryCardData>();
    private List<MemoryCardUI> cardUIList = new List<MemoryCardUI>();

    private MemoryCardUI firstFlippedCard = null;
    private MemoryCardUI secondFlippedCard = null;

    private GameState currentState = GameState.Waiting;
    private int matchedPairsCount = 0;
    private int attemptCount = 0;
    private float elapsedTime = 0f;
    private bool isTimerRunning = false;

    private void OnValidate() {
        if (Application.isPlaying) {
            CleanOrphanedSubMeshes();
        }
    }

    private void OnEnable() {
        if (!Application.isPlaying) {
            // Preserved for user manual inspector & scene editing - do not auto-overwrite!
        }
    }

    private void CleanOrphanedSubMeshes() {
        TMP_SubMeshUI[] subMeshes = GetComponentsInChildren<TMP_SubMeshUI>(true);
        foreach (var subMesh in subMeshes) {
            if (subMesh != null) {
                if (subMesh.sharedMaterial == null || subMesh.canvasRenderer == null || subMesh.fontAsset == null || subMesh.GetComponentInParent<TMP_Text>() == null) {
                    try {
                        GameObject go = subMesh.gameObject;
                        if (Application.isPlaying) {
                            Destroy(subMesh);
                            if (go != null && go != gameObject && go.transform.childCount == 0) Destroy(go);
                        } else {
                            DestroyImmediate(subMesh);
                            if (go != null && go != gameObject && go.transform.childCount == 0) DestroyImmediate(go);
                        }
                    } catch { }
                }
            }
        }
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;
        FixCanvasAndEventSystem();
        CleanOrphanedSubMeshes();
        CleanPastGameElements();
        EnsureBackButtonActive();
        EnsureUIElementsInitialized();
        EnsureMemoryGridCreated();
        InitializeCardDeckData();
        UpdateTitleAndUIComponents();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Game;
        FixCanvasAndEventSystem();
        CleanPastGameElements();
        EnsureBackButtonActive();
        EnsureUIElementsInitialized();
        EnsureMemoryGridCreated();
        InitializeCardDeckData();
        UpdateTitleAndUIComponents();

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        if (resultPopup != null) {
            resultPopup.SetActive(false);
        }

        SetupResultScreenButtons();
        StartNewGame();
    }

    private void CleanPastGameElements() {
        foreach (Transform child in transform) {
            if (child == null) continue;
            string childName = child.name;

            if (childName.Equals("MemoryGridContainer", System.StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("TopBarContainer", System.StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("BackButton", System.StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("LessonTitle", System.StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("LessonTitleText", System.StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("TitleText", System.StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("ResultPopup", System.StringComparison.OrdinalIgnoreCase)) {
                child.gameObject.SetActive(true);
            }
            else if (childName.Contains("SelectionPanel") || childName.Contains("PuzzleCount") || childName.Contains("Polished") || childName.Contains("SortPhrase") || childName.Contains("RestPoint")) {
                child.gameObject.SetActive(false);
            }
        }
    }

    private void Update() {
        if (isTimerRunning && currentState != GameState.Completed) {
            elapsedTime += Time.deltaTime;
            UpdateUI();
        }
    }

    private void FixCanvasAndEventSystem() {
        if (UnityEngine.EventSystems.EventSystem.current == null) {
            GameObject es = GameObject.Find("EventSystem");
            if (es == null) {
                es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        Canvas c = GetComponentInParent<Canvas>();
        if (c != null && c.GetComponent<GraphicRaycaster>() == null) {
            c.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void EnsureBackButtonActive() {
        GameObject backBtnObj = GameObject.Find("BackButton");
        if (backBtnObj == null) {
            Transform t = transform.Find("BackButton") ?? transform.Find("Header/BackButton") ?? transform.Find("Canvas/BackButton") ?? transform.Find("TopBar/BackButton");
            if (t != null) backBtnObj = t.gameObject;
        }

        if (backBtnObj != null) {
            backBtnObj.SetActive(true);
            Image backImg = backBtnObj.GetComponent<Image>();
            if (backImg != null) {
                backImg.enabled = true;
                backImg.raycastTarget = true;
                backImg.color = Color.white;
            }

            Button b = backBtnObj.GetComponent<Button>();
            if (b == null) b = backBtnObj.AddComponent<Button>();
            b.interactable = true;

            Masters_BackButton mbb = backBtnObj.GetComponent<Masters_BackButton>();
            if (mbb == null) mbb = backBtnObj.AddComponent<Masters_BackButton>();

            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => {
                Debug.Log("[G02] Back clicked");
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                    Masters_AudioManager.Instance.StopVoiceOver();
                }
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnBackButtonClicked();
                }
            });
        }
    }

    private void EnsureUIElementsInitialized() {
        if (scoreTMP == null) scoreTMP = FindTMPByName("Score") ?? FindTMPByName("SCORE");
        if (timerTMP == null) timerTMP = FindTMPByName("Timer") ?? FindTMPByName("Time") ?? FindTMPByName("TIME");
        if (matchesTMP == null) matchesTMP = FindTMPByName("Pairs") ?? FindTMPByName("Matches");
        if (attemptsTMP == null) attemptsTMP = FindTMPByName("Flips") ?? FindTMPByName("Attempts");

        Transform topBarTrans = transform.Find("TopBarContainer");
        if (topBarTrans == null) {
            GameObject tbGo = new GameObject("TopBarContainer");
            tbGo.transform.SetParent(transform, false);
            RectTransform tbRect = tbGo.AddComponent<RectTransform>();
            tbRect.anchorMin = new Vector2(0.5f, 1f);
            tbRect.anchorMax = new Vector2(0.5f, 1f);
            tbRect.pivot = new Vector2(0.5f, 1f);
            tbRect.anchoredPosition = new Vector2(0f, -115f);
            tbRect.sizeDelta = new Vector2(1000f, 50f);

            Image bg = tbGo.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.15f, 0.28f, 0.85f);
            bg.raycastTarget = false;

            HorizontalLayoutGroup hlg = tbGo.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.spacing = 30f;
            hlg.padding = new RectOffset(20, 20, 4, 4);

            topBarTrans = tbGo.transform;
        } else {
            RectTransform tbRect = topBarTrans.GetComponent<RectTransform>();
            if (tbRect != null) {
                tbRect.anchorMin = new Vector2(0.5f, 1f);
                tbRect.anchorMax = new Vector2(0.5f, 1f);
                tbRect.pivot = new Vector2(0.5f, 1f);
                tbRect.anchoredPosition = new Vector2(0f, -115f);
                tbRect.sizeDelta = new Vector2(1000f, 50f);
            }
        }

        if (scoreTMP == null) scoreTMP = CreateTopBarTMP(topBarTrans, "ScoreTMP", "SCORE: 0", new Color(1f, 0.85f, 0.2f, 1f));
        if (timerTMP == null) timerTMP = CreateTopBarTMP(topBarTrans, "TimerTMP", "TIME: 0s", new Color(0.3f, 0.9f, 1f, 1f));
        if (matchesTMP == null) matchesTMP = CreateTopBarTMP(topBarTrans, "MatchesTMP", "PAIRS: 0/8", Color.white);
        if (attemptsTMP == null) attemptsTMP = CreateTopBarTMP(topBarTrans, "AttemptsTMP", "FLIPS: 0", new Color(0.9f, 0.9f, 0.9f, 1f));
    }

    private TextMeshProUGUI CreateTopBarTMP(Transform parent, string name, string defaultText, Color textColor) {
        Transform child = parent.Find(name);
        GameObject go;
        if (child == null) {
            go = new GameObject(name);
            go.transform.SetParent(parent, false);
        } else {
            go = child.gameObject;
        }

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = go.AddComponent<TextMeshProUGUI>();

        tmp.text = defaultText;
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private TextMeshProUGUI FindTMPByName(string nameSubstring) {
        TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in tmps) {
            if (tmp == null) continue;
            if (tmp.name.IndexOf(nameSubstring, System.StringComparison.OrdinalIgnoreCase) >= 0 || (tmp.text != null && tmp.text.IndexOf(nameSubstring, System.StringComparison.OrdinalIgnoreCase) >= 0)) {
                return tmp as TextMeshProUGUI;
            }
        }
        return null;
    }

    private void EnsureMemoryGridCreated() {
        if (gridContainer == null) {
            Transform gTrans = transform.Find("MemoryGridContainer") ?? transform.Find("GridContainer") ?? transform.Find("Grid") ?? transform.Find("SelectionPanel");
            if (gTrans != null) gridContainer = gTrans;
        }

        if (gridContainer == null) {
            GameObject gridGo = new GameObject("MemoryGridContainer");
            gridGo.transform.SetParent(transform, false);
            RectTransform gRect = gridGo.AddComponent<RectTransform>();
            gRect.anchorMin = new Vector2(0.5f, 0.45f);
            gRect.anchorMax = new Vector2(0.5f, 0.45f);
            gRect.pivot = new Vector2(0.5f, 0.5f);
            gRect.anchoredPosition = new Vector2(0f, -20f);
            gRect.sizeDelta = new Vector2(760f, 480f);

            GridLayoutGroup glg = gridGo.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(165f, 105f);
            glg.spacing = new Vector2(14f, 14f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 4;
            glg.childAlignment = TextAnchor.MiddleCenter;

            gridContainer = gridGo.transform;
        }

        cardUIList.Clear();
        for (int i = 0; i < 16; i++) {
            Transform cardChild = gridContainer.Find($"Card_{i}");
            GameObject cardObj;
            if (cardChild == null) {
                cardObj = new GameObject($"Card_{i}");
                cardObj.transform.SetParent(gridContainer, false);
            } else {
                cardObj = cardChild.gameObject;
            }

            cardObj.SetActive(true);
            MemoryCardUI cardUI = cardObj.GetComponent<MemoryCardUI>();
            if (cardUI == null) cardUI = cardObj.AddComponent<MemoryCardUI>();
            cardUI.Init(this, i);
            cardUIList.Add(cardUI);
        }
    }

    private void InitializeCardDeckData() {
        cardDeck.Clear();
        string audioDir = "Assets/Audio/2A/6_GrooveOn/Listening/";

        var pairs = new (string festival, string greeting, string audioFile)[] {
            ("Diwali", "Wish you a Happy Diwali", "Wish you a Happy Diwali.mp3"),
            ("Christmas", "Merry Christmas to you!", "Merry Christmas.mp3"),
            ("Easter", "Happy Easter to you!", "Happy Easter to you.mp3"),
            ("Eid", "Eid Mubarak!", "Eid Mubarak.mp3"),
            ("New Year", "Happy New Year!", "Happy New Year.mp3"),
            ("Independence Day", "Happy Independence Day!", "Happy Independence Day.mp3"),
            ("Gandhi Jayanti", "Happy Gandhi Jayanti!", "Happy Gandhi Jayanti.mp3"),
            ("Guru Nanak Jayanti", "Happy Gurupurab! Guru Nanak Jayanti!", "Happy Gurpurab Guru Nanak Jayanti.mp3")
        };

        for (int i = 0; i < pairs.Length; i++) {
            // 1. Festival Card
            Masters_MemoryCardData festData = new Masters_MemoryCardData();
            festData.pairId = i;
            festData.cardText = pairs[i].festival;
            festData.cardType = Masters_CardType.Festival;

            // 2. Greeting Card
            Masters_MemoryCardData greetData = new Masters_MemoryCardData();
            greetData.pairId = i;
            greetData.cardText = pairs[i].greeting;
            greetData.cardType = Masters_CardType.Greeting;

AudioClip clip = null;
#if UNITY_EDITOR
        clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + pairs[i].audioFile);
#endif
            festData.cardAudio = clip;
            greetData.cardAudio = clip;

            cardDeck.Add(festData);
            cardDeck.Add(greetData);
        }
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            if (tmp.GetComponentInParent<Button>() != null) continue;
            if (tmp.GetComponentInParent<MemoryCardUI>() != null) continue;

            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";

            if (lowerName.Equals("lessontitletext") || lowerName.Equals("lessontitle") || lowerName.Equals("titletext") ||
                textVal.Contains("Twin Match") || textVal.Contains("TWIN") || textVal.Contains("Formal") || textVal.Contains("Rapid Fire") || textVal.Contains("Celebration") || textVal.Contains("Festival Match")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "G02 Festival Match — Festival ↔ Greeting Memory";

                RectTransform tRect = tmp.GetComponent<RectTransform>();
                if (tRect != null) {
                    tRect.anchorMin = new Vector2(0.5f, 1f);
                    tRect.anchorMax = new Vector2(0.5f, 1f);
                    tRect.pivot = new Vector2(0.5f, 1f);
                    tRect.anchoredPosition = new Vector2(0f, -40f);
                }
            }
            else if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("GAME") || textVal.Contains("BRANCH")) {
                tmp.text = "G02 Festival Match — Festival ↔ Greeting Memory";
            }
        }
    }

    private void SetupResultScreenButtons() {
        if (resultPopup != null) {
            Transform retryT = FindChildRecursive(resultPopup.transform, "RetryButton") ?? FindChildRecursive(resultPopup.transform, "Retry");
            if (retryT != null) retryButton = retryT.GetComponent<Button>();

            Transform hubT = FindChildRecursive(resultPopup.transform, "ReturnHubButton") ?? FindChildRecursive(resultPopup.transform, "HubButton") ?? FindChildRecursive(resultPopup.transform, "HomeButton");
            if (hubT != null) returnHubButton = hubT.GetComponent<Button>();
        }

        if (retryButton != null) {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(() => {
                Debug.Log("[G02] Retry clicked");
                if (resultPopup != null) resultPopup.SetActive(false);
                StartNewGame();
            });
        }

        if (returnHubButton != null) {
            returnHubButton.onClick.RemoveAllListeners();
            returnHubButton.onClick.AddListener(() => {
                Debug.Log("[G02] Return to Hub clicked");
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnBackButtonClicked();
                }
            });
        }
    }

    private void StartNewGame() {
        matchedPairsCount = 0;
        attemptCount = 0;
        elapsedTime = 0f;
        firstFlippedCard = null;
        secondFlippedCard = null;
        currentState = GameState.Waiting;
        isTimerRunning = true;

        UpdateUI();

        // Shuffle Deck
        List<Masters_MemoryCardData> shuffledDeck = new List<Masters_MemoryCardData>(cardDeck);
        ShuffleList(shuffledDeck);

        for (int i = 0; i < cardUIList.Count && i < shuffledDeck.Count; i++) {
            cardUIList[i].SetupData(shuffledDeck[i]);
        }
    }

    public void OnCardSelected(MemoryCardUI card) {
        if (currentState == GameState.CheckingPair || currentState == GameState.Completed) return;
        if (card == null || card.isMatched || card.isFaceUp) return;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }

        if (currentState == GameState.Waiting) {
            firstFlippedCard = card;
            currentState = GameState.OneCardFlipped;

            if (card.data != null && card.data.cardAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(card.data.cardAudio);
            }

            card.FlipToFaceUp();
        }
        else if (currentState == GameState.OneCardFlipped) {
            if (card == firstFlippedCard) return;

            secondFlippedCard = card;
            currentState = GameState.CheckingPair;
            attemptCount++;

            if (card.data != null && card.data.cardAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(card.data.cardAudio);
            }

            card.FlipToFaceUp(() => {
                StartCoroutine(CheckMatchRoutine());
            });
        }
    }

    private IEnumerator CheckMatchRoutine() {
        yield return new WaitForSeconds(0.4f);

        if (firstFlippedCard != null && secondFlippedCard != null && firstFlippedCard.data != null && secondFlippedCard.data != null) {
            bool isMatch = (firstFlippedCard.data.pairId == secondFlippedCard.data.pairId) &&
                           (firstFlippedCard.data.cardType != secondFlippedCard.data.cardType);

            if (isMatch) {
                // Correct Match!
                matchedPairsCount++;
                firstFlippedCard.AnimateMatch();
                secondFlippedCard.AnimateMatch();

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                }

                firstFlippedCard = null;
                secondFlippedCard = null;
                UpdateUI();

                if (matchedPairsCount >= totalPairs) {
                    OnAllPairsMatched();
                } else {
                    currentState = GameState.Waiting;
                }
            } else {
                // Mismatch
                firstFlippedCard.AnimateMismatch();
                secondFlippedCard.AnimateMismatch();

                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                yield return new WaitForSeconds(0.7f);

                firstFlippedCard.FlipToFaceDown();
                secondFlippedCard.FlipToFaceDown();

                yield return new WaitForSeconds(0.35f);

                firstFlippedCard = null;
                secondFlippedCard = null;
                currentState = GameState.Waiting;
                UpdateUI();
            }
        } else {
            firstFlippedCard = null;
            secondFlippedCard = null;
            currentState = GameState.Waiting;
        }
    }

    private void OnAllPairsMatched() {
        currentState = GameState.Completed;
        isTimerRunning = false;

        if (resultPopup != null) {
            resultPopup.SetActive(true);
            resultPopup.transform.localScale = Vector3.zero;
            resultPopup.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = "GREAT JOB!";
            resultTitleTMP.color = Color.green;
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = "You matched all 8 festival & greeting pairs!";
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"TIME: {Mathf.CeilToInt(elapsedTime)}s  |  PAIRS: {matchedPairsCount}/{totalPairs}  |  ATTEMPTS: {attemptCount}";
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            NextButtonAnimation();
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    private void UpdateUI() {
        int scorePoints = (matchedPairsCount * 100) - (attemptCount * 10);
        if (scorePoints < 0) scorePoints = 0;

        if (scoreTMP != null) scoreTMP.text = $"SCORE: {scorePoints}";
        if (timerTMP != null) timerTMP.text = $"TIME: {Mathf.CeilToInt(elapsedTime)}s";
        if (matchesTMP != null) matchesTMP.text = $"PAIRS: {matchedPairsCount}/{totalPairs}";
        if (attemptsTMP != null) attemptsTMP.text = $"FLIPS: {attemptCount}";
    }

    protected override void OnNextButtonClicked() {
        Debug.Log($"G02 Festival Match Completed in {Mathf.CeilToInt(elapsedTime)}s with {attemptCount} attempts.");
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    private void ShuffleList<T>(List<T> list) {
        for (int i = list.Count - 1; i > 0; i--) {
            int rand = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[rand];
            list[rand] = temp;
        }
    }

    private Transform FindChildRecursive(Transform parent, string childName) {
        if (parent == null) return null;
        foreach (Transform child in parent) {
            if (child == null) continue;
            if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase)) return child;
            Transform result = FindChildRecursive(child, childName);
            if (result != null) return result;
        }
        return null;
    }

}