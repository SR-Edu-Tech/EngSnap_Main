using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Game Controller for Unit 6 (Groove On) G01: Greeting Dash — Sort by Occasion.
/// Timed arcade sorting game with auto-constructing gates and UI:
/// - 4 Occasion Gates: BIRTHDAY WISH (0), PARTY QUESTION (1), FESTIVAL GREETING (2), PREPARATION (3)
/// - 60s round timer, 3 initial lives, target 16 correct sorts to pass
/// - Accelerating falling phrase stream (4 speed tiers)
/// - Supports both Drag-and-Drop and Tap interactions
/// - Full Audio, Confetti burst, Score, Result Popup, Retry, and Return to Hub navigation
/// </summary>
[ExecuteAlways]
public class Masters_GrooveOn_Game_LessonOne : Masters_Lesson, IDragHandler, IBeginDragHandler, IEndDragHandler {

    [System.Serializable]
    public class GreetingTileData {
        public string phraseText;
        public AudioClip phraseAudio;
        public int categoryId; // 0 = BIRTHDAY WISH, 1 = PARTY QUESTION, 2 = FESTIVAL GREETING, 3 = PREPARATION
    }

    [Header("Greeting Dash Tile Data (24 Verbatim Phrases)")]
    [SerializeField] private GreetingTileData[] greetingTiles;

    [Header("UI Gate & Lane Containers")]
    [SerializeField] private Masters_UniversalSortBin[] gateArray; // 4 gates
    [SerializeField] private RectTransform activeTileRectTransform;
    [SerializeField] private TextMeshProUGUI phraseTMP;
    [SerializeField] private RectTransform laneTopSpawnPoint;
    [SerializeField] private RectTransform laneBottomFailPoint;

    [Header("Game State UI")]
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI livesTMP;
    [SerializeField] private TextMeshProUGUI sortedCounterTMP;
    [SerializeField] private GameObject resultPopup;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultStatusTMP;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button returnHubButton;

    [Header("Game Parameters")]
    [SerializeField] private int targetPassCount = 10;
    [SerializeField] private float roundDuration = 60f;
    [SerializeField] private int initialLives = 3;

    // Difficulty Speed Tiers (Configurable - Slower, relaxed fall speeds)
    [SerializeField] private float speedTier1 = 50f; // 0-15s
    [SerializeField] private float speedTier2 = 65f; // 15-30s
    [SerializeField] private float speedTier3 = 80f; // 30-45s
    [SerializeField] private float speedTier4 = 95f; // 45-60s

    private List<GreetingTileData> shuffledQueue = new List<GreetingTileData>();
    private int queueIndex = 0;
    private int currentLives = 3;
    private int correctSorts = 0;
    private int scorePoints = 0;
    private float timeRemaining = 60f;
    private bool isGameActive = false;
    private bool isTileActive = false;
    private bool isDragging = false;
    private GreetingTileData currentTile;
    private Coroutine tileFallCoroutine;
    private Vector2 originalTilePos;
    private Canvas parentCanvas;

    private void OnValidate() {
        if (Application.isPlaying) {
            CleanOrphanedSubMeshes();
        }
    }

    private void OnEnable() {
        if (!Application.isPlaying) {
            EnsureUIElementsInitialized();
            ConfigureGates();
            EnsurePhraseDataInitialized();
            UpdateTitleAndUIComponents();
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
        parentCanvas = GetComponentInParent<Canvas>();
        FixCanvasAndEventSystem();
        CleanOrphanedSubMeshes();
        EnsureBackButtonActive();
        EnsureUIElementsInitialized();
        CleanPastGameElements();
        ConfigureGates();
        EnsurePhraseDataInitialized();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Game;
        FixCanvasAndEventSystem();
        EnsureBackButtonActive();
        EnsureUIElementsInitialized();
        CleanPastGameElements();
        ConfigureGates();
        EnsurePhraseDataInitialized();
        UpdateTitleAndUIComponents();

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        if (resultPopup != null) {
            resultPopup.SetActive(false);
        }

        SetupResultScreenButtons();
        InitializeGame();
    }

    private void CleanPastGameElements() {
        foreach (Transform child in transform) {
            if (child == null) continue;
            string childName = child.name;

            if (childName.Equals("GatesContainer", System.StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("PhraseCard", System.StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("TopBarContainer", System.StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("BackButton", System.StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("LessonTitle", System.StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("LessonTitleText", System.StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("TitleText", System.StringComparison.OrdinalIgnoreCase) ||
                childName.Equals("ResultPopup", System.StringComparison.OrdinalIgnoreCase)) {
                child.gameObject.SetActive(true);
            }
            else if (childName.Contains("SelectionPanel") || childName.Contains("PuzzleCount") || childName.Contains("Polished") || childName.Contains("SortPhraseCard")) {
                child.gameObject.SetActive(false);
            }
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

        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.GetComponent<GraphicRaycaster>() == null) {
            parentCanvas.gameObject.AddComponent<GraphicRaycaster>();
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
                Debug.Log("[G01] Back clicked");
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
        if (activeTileRectTransform == null) {
            Transform cardT = transform.Find("PhraseCard") ?? transform.Find("ActiveTile") ?? transform.Find("Tile");
            if (cardT != null) activeTileRectTransform = cardT.GetComponent<RectTransform>();
        }

        if (phraseTMP == null && activeTileRectTransform != null) {
            phraseTMP = activeTileRectTransform.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (laneTopSpawnPoint == null) {
            Transform spawnT = transform.Find("PhraseCardRestPoint") ?? transform.Find("SpawnPoint");
            if (spawnT != null) laneTopSpawnPoint = spawnT.GetComponent<RectTransform>();
        }

        if (laneBottomFailPoint == null) {
            Transform failT = transform.Find("FailPoint") ?? transform.Find("BottomPoint");
            if (failT == null) {
                GameObject failGo = new GameObject("LaneBottomFailPoint");
                failGo.transform.SetParent(transform, false);
                RectTransform failRect = failGo.AddComponent<RectTransform>();
                failRect.anchorMin = new Vector2(0.5f, 0f);
                failRect.anchorMax = new Vector2(0.5f, 0f);
                failRect.pivot = new Vector2(0.5f, 0f);
                failRect.anchoredPosition = new Vector2(0f, 220f);
                laneBottomFailPoint = failRect;
            } else {
                laneBottomFailPoint = failT.GetComponent<RectTransform>();
            }
        }

        EnsureScoreBoardHUDCreated();
    }

    private void EnsureScoreBoardHUDCreated() {
        if (scoreTMP != null || sortedCounterTMP != null) {
            if (scoreTMP != null) scoreTMP.gameObject.SetActive(true);
            if (sortedCounterTMP != null) sortedCounterTMP.gameObject.SetActive(true);
            UpdateUI();
            return;
        }

        // Disable any old/inactive PuzzleCountTMP objects from legacy templates
        TMP_Text[] oldTmps = GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in oldTmps) {
            if (t != null && t.name.StartsWith("PuzzleCountTMP")) {
                t.gameObject.SetActive(false);
            }
        }

        Transform boardTrans = transform.Find("ScoreBoardHUD");
        GameObject boardGo;
        if (boardTrans == null) {
            boardGo = new GameObject("ScoreBoardHUD");
            boardGo.transform.SetParent(transform, false);
        } else {
            boardGo = boardTrans.gameObject;
        }

        boardGo.SetActive(true);
        boardGo.transform.SetAsLastSibling(); // Ensure Score Board renders on top of all canvas layers

        RectTransform rect = boardGo.GetComponent<RectTransform>();
        if (rect == null) rect = boardGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-30f, -25f);
        rect.sizeDelta = new Vector2(320f, 120f);

        Image bgImg = boardGo.GetComponent<Image>();
        if (bgImg != null) {
            bgImg.enabled = false;
        }

        Transform textTrans = boardGo.transform.Find("ScoreText");
        GameObject scoreGo;
        if (textTrans == null) {
            scoreGo = new GameObject("ScoreText");
            scoreGo.transform.SetParent(boardGo.transform, false);
        } else {
            scoreGo = textTrans.gameObject;
        }

        scoreGo.SetActive(true);
        RectTransform scoreRect = scoreGo.GetComponent<RectTransform>();
        if (scoreRect == null) scoreRect = scoreGo.AddComponent<RectTransform>();
        scoreRect.anchorMin = Vector2.zero;
        scoreRect.anchorMax = Vector2.one;
        scoreRect.offsetMin = Vector2.zero;
        scoreRect.offsetMax = Vector2.zero;

        TMP_FontAsset howdybunFont = null;
#if UNITY_EDITOR
        howdybunFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Howdybun SDF 1.asset");
#endif
        if (howdybunFont == null) {
            howdybunFont = Resources.Load<TMP_FontAsset>("Fonts/Howdybun SDF 1");
        }

        TextMeshProUGUI tmp = scoreGo.GetComponent<TextMeshProUGUI>();
        if (tmp == null) tmp = scoreGo.AddComponent<TextMeshProUGUI>();
        tmp.enabled = true;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        if (howdybunFont != null) {
            tmp.font = howdybunFont;
        }
        tmp.enableWordWrapping = true;
        tmp.enableAutoSizing = false;
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.TopRight;

        scoreTMP = tmp;
        sortedCounterTMP = tmp;
        timerTMP = tmp;
    }

    private void EnsurePhraseDataInitialized() {
        if (greetingTiles == null || greetingTiles.Length < 20) {
            var phrases = new (string text, string audio, int category)[] {
                // Category 0: BIRTHDAY WISH
                ("Have a wonderful birthday!", "Wish you a very happy birthday.mp3", 0),
                ("Wish you a very happy birthday!", "Wish you a very happy birthday.mp3", 0),
                ("Many happy returns of the day!", "Many happy returns of the day.mp3", 0),
                ("Wishing you a fantastic birthday!", "Wishing you a fantastic birthday.mp3", 0),
                ("May all your dreams come true!", "May all your dreams come true.mp3", 0),
                ("Hope your birthday is awesome!", "Hope your birthday is awesome.mp3", 0),

                // Category 1: PARTY QUESTION
                ("Would you like to join the party?", "Where is the party happening.mp3", 1),
                ("What time does the party start?", "What time does the party start.mp3", 1),
                ("Shall I bring some snacks?", "Shall I bring some snacks.mp3", 1),
                ("Who else is coming tonight?", "Who else is coming tonight.mp3", 1),
                ("Where is the party happening?", "Where is the party happening.mp3", 1),
                ("Would you like me to bring dessert?", "Would you like me to bring dessert.mp3", 1),

                // Category 2: FESTIVAL GREETING
                ("Wish you a Happy Diwali!", "Wish you a Happy Diwali.mp3", 2),
                ("Wishing you a joyful festival season!", "Wishing you a joyful festival season.mp3", 2),
                ("Happy Holidays to you and your family!", "Happy Holidays to you and your family.mp3", 2),
                ("May this festival bring peace and joy!", "May this festival bring peace and joy.mp3", 2),
                ("Warm wishes on this festive occasion!", "Warm wishes on this festive occasion.mp3", 2),
                ("Have a blessed and happy festival!", "Have a blessed and happy festival.mp3", 2),

                // Category 3: PREPARATION
                ("Please get the decorations ready.", "Let us decorate the living room.mp3", 3),
                ("Let us decorate the living room.", "Let us decorate the living room.mp3", 3),
                ("We need to bake the cake first.", "We need to bake the cake first.mp3", 3),
                ("Did you order the balloons?", "Did you order the balloons.mp3", 3),
                ("I will set up the music system.", "I will set up the music system.mp3", 3),
                ("Let us prepare the guest list.", "Let us prepare the guest list.mp3", 3)
            };

            greetingTiles = new GreetingTileData[phrases.Length];
            string audioDir = "Assets/Audio/2A/6_GrooveOn/Game/";

            for (int i = 0; i < phrases.Length; i++) {
                greetingTiles[i] = new GreetingTileData();
                greetingTiles[i].phraseText = phrases[i].text;
                greetingTiles[i].categoryId = phrases[i].category;
#if UNITY_EDITOR
                greetingTiles[i].phraseAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + phrases[i].audio);
#endif
            }
        }
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            if (tmp.GetComponentInParent<Button>() != null) continue;
            if (tmp.GetComponentInParent<Masters_UniversalSortBin>() != null) continue;

            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";

            if (lowerName.Equals("lessontitletext") || lowerName.Equals("lessontitle") || lowerName.Equals("titletext") || textVal.Contains("STREET DASH") || textVal.Contains("Street Dash") || textVal.Contains("Sort Expressions")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "G01 Greeting Dash — Sort by Occasion";
            }
            else if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("GAME") || textVal.Contains("BRANCH")) {
                tmp.text = "G01 Greeting Dash — Sort by Occasion";
            }
        }
    }

    private void ConfigureGates() {
        if (gateArray == null || gateArray.Length < 4) {
            gateArray = GetComponentsInChildren<Masters_UniversalSortBin>(true);
        }

        if (gateArray == null || gateArray.Length < 4) {
            EnsureGatesCreated();
        }

        string[] gateLabels = new string[] {
            "BIRTHDAY WISH",
            "PARTY QUESTION",
            "FESTIVAL GREETING",
            "PREPARATION"
        };

        Color[] gateColors = new Color[] {
            new Color(0.85f, 0.47f, 0.02f, 1f), // 0: Birthday Wish (Gold / Amber)
            new Color(0.15f, 0.39f, 0.92f, 1f), // 1: Party Question (Royal Blue)
            new Color(0.49f, 0.23f, 0.93f, 1f), // 2: Festival Greeting (Purple)
            new Color(0.02f, 0.59f, 0.41f, 1f)  // 3: Preparation (Emerald Green)
        };

        if (gateArray != null) {
            for (int i = 0; i < gateArray.Length && i < 4; i++) {
                if (gateArray[i] == null) continue;

                gateArray[i].gameObject.SetActive(true);
                gateArray[i].SetSortId(i);

                GameObject gateObj = gateArray[i].gameObject;

                Image img = gateObj.GetComponent<Image>();
                if (img == null) img = gateObj.GetComponentInChildren<Image>(true);
                if (img == null) img = gateObj.AddComponent<Image>();

                img.enabled = true;
                img.raycastTarget = true;
                img.color = gateColors[i];

                TMP_Text tmp = gateObj.GetComponentInChildren<TMP_Text>(true);
                if (tmp == null) {
                    GameObject textGo = new GameObject("GateText");
                    textGo.transform.SetParent(gateObj.transform, false);
                    RectTransform textRect = textGo.AddComponent<RectTransform>();
                    textRect.anchorMin = Vector2.zero;
                    textRect.anchorMax = Vector2.one;
                    textRect.offsetMin = Vector2.zero;
                    textRect.offsetMax = Vector2.zero;
                    tmp = textGo.AddComponent<TextMeshProUGUI>();
                }

                tmp.gameObject.SetActive(true);
                tmp.raycastTarget = false;
                tmp.color = Color.white;
                tmp.text = gateLabels[i];
                tmp.enableWordWrapping = true;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 13;
                tmp.fontSizeMax = 20;
                tmp.alignment = TextAlignmentOptions.Center;

                Button btn = gateObj.GetComponent<Button>();
                if (btn == null) btn = gateObj.GetComponentInChildren<Button>(true);
                if (btn == null) btn = gateObj.AddComponent<Button>();

                btn.interactable = true;
                btn.transition = Selectable.Transition.None;

                int gateIndex = i;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnGateClicked(gateIndex));
            }
        }
    }

    private void EnsureGatesCreated() {
        Transform containerTrans = transform.Find("GatesContainer");
        if (containerTrans == null) {
            GameObject containerGo = new GameObject("GatesContainer");
            containerGo.transform.SetParent(transform, false);
            RectTransform cRect = containerGo.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 0f);
            cRect.anchorMax = new Vector2(0.5f, 0f);
            cRect.pivot = new Vector2(0.5f, 0f);
            cRect.anchoredPosition = new Vector2(0f, 50f);
            cRect.sizeDelta = new Vector2(1100f, 130f);

            HorizontalLayoutGroup hlg = containerGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            containerTrans = containerGo.transform;
        }

        List<Masters_UniversalSortBin> createdBins = new List<Masters_UniversalSortBin>();
        for (int i = 0; i < 4; i++) {
            Transform gateChild = containerTrans.Find($"Gate_{i}");
            GameObject gObj;
            if (gateChild == null) {
                gObj = new GameObject($"Gate_{i}");
                gObj.transform.SetParent(containerTrans, false);
            } else {
                gObj = gateChild.gameObject;
            }

            gObj.SetActive(true);
            Masters_UniversalSortBin bin = gObj.GetComponent<Masters_UniversalSortBin>();
            if (bin == null) bin = gObj.AddComponent<Masters_UniversalSortBin>();
            createdBins.Add(bin);
        }

        gateArray = createdBins.ToArray();
    }

    private void InitializeGame() {
        currentLives = initialLives;
        correctSorts = 0;
        scorePoints = 0;
        timeRemaining = roundDuration;
        isGameActive = false;
        isTileActive = false;
        isDragging = false;

        UpdateUI();

        // Prepare shuffled tile stream
        shuffledQueue.Clear();
        if (greetingTiles != null && greetingTiles.Length > 0) {
            shuffledQueue.AddRange(greetingTiles);
            ShuffleList(shuffledQueue);
        }

        queueIndex = 0;
        StartCoroutine(StartGameRoutine());
    }

    private bool introPlayed = false;

    private IEnumerator StartGameRoutine() {
        if (!introPlayed) {
            introPlayed = true;
            if (narratorSpeech == null) {
#if UNITY_EDITOR
                narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Game/Welcome to Greeting Dash Sort the celebration tiles into the correct categories.mp3");
#endif
            }

            AudioClip introAudio = narratorSpeech;
            if (introAudio != null) {
                Debug.Log($"[G01 Intro Audio] Playing introduction audio first: {introAudio.name} ({introAudio.length}s)");
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(introAudio);
                }
                yield return new WaitForSeconds(introAudio.length + 0.3f);
            } else {
                yield return new WaitForSeconds(0.5f);
            }
        } else {
            yield return new WaitForSeconds(0.5f);
        }

        isGameActive = true;
        StartCoroutine(TimerCountdownRoutine());
        SpawnNextTile();
    }

    private IEnumerator TimerCountdownRoutine() {
        while (isGameActive && timeRemaining > 0f) {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining < 0f) timeRemaining = 0f;

            UpdateUI();
            yield return null;
        }

        if (isGameActive) {
            EndGame(correctSorts >= targetPassCount);
        }
    }

    private float GetCurrentFallSpeed() {
        float elapsed = roundDuration - timeRemaining;
        float speed = speedTier1;
        if (elapsed < 15f) speed = speedTier1;
        else if (elapsed < 30f) speed = speedTier2;
        else if (elapsed < 45f) speed = speedTier3;
        else speed = speedTier4;

        // If Inspector serialized fields contain legacy high values (> 100f), scale down to comfortable slow speed
        if (speed > 100f) speed = Mathf.Min(speed * 0.35f, 95f);
        return Mathf.Max(speed, 45f);
    }

    private void SpawnNextTile() {
        if (!isGameActive) return;

        if (shuffledQueue.Count == 0) return;

        if (queueIndex >= shuffledQueue.Count) {
            ShuffleList(shuffledQueue);
            queueIndex = 0;
        }

        currentTile = shuffledQueue[queueIndex++];

        if (phraseTMP != null) {
            phraseTMP.text = currentTile.phraseText;
            phraseTMP.raycastTarget = false;
        }

        if (activeTileRectTransform != null && laneTopSpawnPoint != null) {
            activeTileRectTransform.gameObject.SetActive(true);
            activeTileRectTransform.anchoredPosition = laneTopSpawnPoint.anchoredPosition;
            activeTileRectTransform.localScale = Vector3.one;

            Image tileImg = activeTileRectTransform.GetComponent<Image>();
            if (tileImg != null) tileImg.raycastTarget = true;
        }

        if (currentTile.phraseAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(currentTile.phraseAudio);
        }

        isTileActive = true;
        isDragging = false;

        if (tileFallCoroutine != null) StopCoroutine(tileFallCoroutine);
        tileFallCoroutine = StartCoroutine(TileFallRoutine());
    }

    private IEnumerator TileFallRoutine() {
        if (activeTileRectTransform == null || laneBottomFailPoint == null) yield break;

        Vector2 startPos = activeTileRectTransform.anchoredPosition;
        Vector2 targetPos = laneBottomFailPoint.anchoredPosition;

        float elapsedTime = 0f;
        float currentSpeed = GetCurrentFallSpeed();
        float totalDistance = Vector2.Distance(startPos, targetPos);
        float duration = totalDistance / currentSpeed;

        while (elapsedTime < duration && isTileActive && isGameActive) {
            if (!isDragging) {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / duration;
                activeTileRectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
            }
            yield return null;
        }

        if (isTileActive && isGameActive && !isDragging) {
            OnTileMissed();
        }
    }

    #region Drag & Drop Implementation
    public void OnBeginDrag(PointerEventData eventData) {
        if (!isGameActive || !isTileActive || activeTileRectTransform == null) return;
        isDragging = true;
        originalTilePos = activeTileRectTransform.anchoredPosition;
    }

    public void OnDrag(PointerEventData eventData) {
        if (!isGameActive || !isTileActive || activeTileRectTransform == null) return;
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            activeTileRectTransform.parent as RectTransform,
            eventData.position,
            parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? eventData.pressEventCamera : null,
            out localPoint
        );
        activeTileRectTransform.anchoredPosition = localPoint;
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (!isGameActive || !isTileActive) return;
        isDragging = false;

        int droppedGateIndex = DetectDroppedGate(eventData.position);
        if (droppedGateIndex >= 0) {
            OnGateClicked(droppedGateIndex);
        } else {
            activeTileRectTransform.DOAnchorPos(originalTilePos, 0.2f);
        }
    }

    private int DetectDroppedGate(Vector2 screenPos) {
        if (gateArray == null) return -1;
        for (int i = 0; i < gateArray.Length && i < 4; i++) {
            if (gateArray[i] == null) continue;
            RectTransform gateRect = gateArray[i].GetComponent<RectTransform>();
            if (gateRect != null && RectTransformUtility.RectangleContainsScreenPoint(gateRect, screenPos, parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? Camera.main : null)) {
                return i;
            }
        }
        return -1;
    }
    #endregion

    private void OnGateClicked(int gateIndex) {
        if (!isGameActive || !isTileActive || currentTile == null) return;

        isTileActive = false;
        if (tileFallCoroutine != null) StopCoroutine(tileFallCoroutine);

        bool isCorrect = (gateIndex == currentTile.categoryId);
        if (isCorrect) {
            // Correct Gate!
            correctSorts++;
            scorePoints += 100;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
            AnimateGateFeedback(gateIndex, true);
            AnimateTileToGate(gateIndex, true);
        } else {
            // Wrong Gate!
            currentLives--;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            AnimateGateFeedback(gateIndex, false);
            AnimateTileToGate(gateIndex, false);
        }

        UpdateUI();
    }

    private void AnimateGateFeedback(int gateIndex, bool isCorrect) {
        if (gateArray == null || gateIndex < 0 || gateIndex >= gateArray.Length || gateArray[gateIndex] == null) return;

        GameObject gateObj = gateArray[gateIndex].gameObject;
        gateObj.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f, 5, 0.5f);

        Image gateImage = gateObj.GetComponent<Image>();
        if (gateImage == null) gateImage = gateObj.GetComponentInChildren<Image>();

        if (gateImage != null) {
            Color[] gateColors = new Color[] {
                new Color(0.85f, 0.47f, 0.02f, 1f),
                new Color(0.15f, 0.39f, 0.92f, 1f),
                new Color(0.49f, 0.23f, 0.93f, 1f),
                new Color(0.02f, 0.59f, 0.41f, 1f)
            };

            Color originalColor = gateColors[gateIndex % gateColors.Length];
            Color feedbackColor = isCorrect ? new Color(1f, 0.92f, 0.35f, 1f) : new Color(0.93f, 0.26f, 0.26f, 1f);

            gateImage.DOColor(feedbackColor, 0.15f).OnComplete(() => {
                gateImage.DOColor(originalColor, 0.3f);
            });
        }
    }

    private void OnTileMissed() {
        isTileActive = false;
        currentLives--;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
        UpdateUI();

        if (activeTileRectTransform != null) {
            activeTileRectTransform.DOShakePosition(0.3f, 20f).OnComplete(() => {
                CheckGameStatusAndContinue();
            });
        } else {
            CheckGameStatusAndContinue();
        }
    }

    private void AnimateTileToGate(int gateIndex, bool isCorrect) {
        if (activeTileRectTransform == null || gateArray == null || gateIndex >= gateArray.Length || gateArray[gateIndex] == null) {
            CheckGameStatusAndContinue();
            return;
        }

        RectTransform gateRect = gateArray[gateIndex].GetPhraseTargetPointRectTransform();
        Vector2 targetPos = gateRect.anchoredPosition;

        Sequence seq = DOTween.Sequence();
        seq.Append(activeTileRectTransform.DOAnchorPos(targetPos, 0.25f).SetEase(Ease.InQuad));
        seq.Join(activeTileRectTransform.DOScale(isCorrect ? 0.2f : 1.1f, 0.25f));
        seq.OnComplete(() => {
            if (activeTileRectTransform != null) activeTileRectTransform.gameObject.SetActive(false);
            CheckGameStatusAndContinue();
        });
    }

    private void CheckGameStatusAndContinue() {
        if (currentLives <= 0) {
            EndGame(false);
        } else if (correctSorts >= targetPassCount && timeRemaining > 0f) {
            EndGame(true);
        } else if (isGameActive) {
            SpawnNextTile();
        }
    }

    private void SetupResultScreenButtons() {
        EnsureResultPopupAndRetryButton();
    }

    private void EnsureResultPopupAndRetryButton() {
        if (resultPopup == null) {
            Transform popTrans = transform.Find("CompletedPanel") ?? transform.Find("ResultPopup") ?? transform.Find("GameOverPanel");
            if (popTrans == null) {
                GameObject popGo = new GameObject("ResultPopup");
                popGo.transform.SetParent(transform, false);
                popTrans = popGo.transform;
            }
            resultPopup = popTrans.gameObject;
        }

        // Overlay backdrop
        Image popBg = resultPopup.GetComponent<Image>();
        if (popBg == null) popBg = resultPopup.AddComponent<Image>();
        popBg.enabled = true;
        popBg.color = new Color(0.05f, 0.08f, 0.15f, 0.92f);
        popBg.raycastTarget = true;

        RectTransform popRect = resultPopup.GetComponent<RectTransform>();
        if (popRect == null) popRect = resultPopup.AddComponent<RectTransform>();
        popRect.anchorMin = Vector2.zero;
        popRect.anchorMax = Vector2.one;
        popRect.offsetMin = Vector2.zero;
        popRect.offsetMax = Vector2.zero;

        // Result Card Container
        Transform cardTrans = resultPopup.transform.Find("ResultCard");
        GameObject cardGo;
        if (cardTrans == null) {
            cardGo = new GameObject("ResultCard");
            cardGo.transform.SetParent(resultPopup.transform, false);
            cardTrans = cardGo.transform;
        } else {
            cardGo = cardTrans.gameObject;
        }

        RectTransform cardRect = cardGo.GetComponent<RectTransform>();
        if (cardRect == null) cardRect = cardGo.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.anchoredPosition = Vector2.zero;
        cardRect.sizeDelta = new Vector2(560f, 320f);

        Image cardBg = cardGo.GetComponent<Image>();
        if (cardBg == null) cardBg = cardGo.AddComponent<Image>();
        cardBg.enabled = true;
        cardBg.color = new Color(0.1f, 0.18f, 0.35f, 1f);

        TMP_FontAsset howdybunFont = null;
#if UNITY_EDITOR
        howdybunFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Howdybun SDF 1.asset");
#endif
        if (howdybunFont == null) howdybunFont = Resources.Load<TMP_FontAsset>("Fonts/Howdybun SDF 1");

        // Title TMP
        Transform titleTrans = cardGo.transform.Find("ResultTitle");
        GameObject titleGo;
        if (titleTrans == null) {
            titleGo = new GameObject("ResultTitle");
            titleGo.transform.SetParent(cardGo.transform, false);
        } else {
            titleGo = titleTrans.gameObject;
        }

        RectTransform titleRect = titleGo.GetComponent<RectTransform>();
        if (titleRect == null) titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -25f);
        titleRect.sizeDelta = new Vector2(500f, 60f);

        TextMeshProUGUI titleTMP = titleGo.GetComponent<TextMeshProUGUI>();
        if (titleTMP == null) titleTMP = titleGo.AddComponent<TextMeshProUGUI>();
        titleTMP.alignment = TextAlignmentOptions.Center;
        titleTMP.fontSize = 36f;
        titleTMP.fontStyle = FontStyles.Bold;
        if (howdybunFont != null) titleTMP.font = howdybunFont;
        resultTitleTMP = titleTMP;

        // Score TMP
        Transform scoreTrans = cardGo.transform.Find("ResultScore");
        GameObject scoreGo;
        if (scoreTrans == null) {
            scoreGo = new GameObject("ResultScore");
            scoreGo.transform.SetParent(cardGo.transform, false);
        } else {
            scoreGo = scoreTrans.gameObject;
        }

        RectTransform scoreRect = scoreGo.GetComponent<RectTransform>();
        if (scoreRect == null) scoreRect = scoreGo.AddComponent<RectTransform>();
        scoreRect.anchorMin = new Vector2(0.5f, 0.5f);
        scoreRect.anchorMax = new Vector2(0.5f, 0.5f);
        scoreRect.pivot = new Vector2(0.5f, 0.5f);
        scoreRect.anchoredPosition = new Vector2(0f, 10f);
        scoreRect.sizeDelta = new Vector2(500f, 60f);

        TextMeshProUGUI scoreTextComp = scoreGo.GetComponent<TextMeshProUGUI>();
        if (scoreTextComp == null) scoreTextComp = scoreGo.AddComponent<TextMeshProUGUI>();
        scoreTextComp.alignment = TextAlignmentOptions.Center;
        scoreTextComp.fontSize = 24f;
        scoreTextComp.color = Color.white;
        if (howdybunFont != null) scoreTextComp.font = howdybunFont;
        resultScoreTMP = scoreTextComp;

        // Retry Button ("RETRY")
        Transform retryTrans = cardGo.transform.Find("RetryButton");
        GameObject retryGo;
        if (retryTrans == null) {
            retryGo = new GameObject("RetryButton");
            retryGo.transform.SetParent(cardGo.transform, false);
        } else {
            retryGo = retryTrans.gameObject;
        }

        RectTransform retryRect = retryGo.GetComponent<RectTransform>();
        if (retryRect == null) retryRect = retryGo.AddComponent<RectTransform>();
        retryRect.anchorMin = new Vector2(0.5f, 0f);
        retryRect.anchorMax = new Vector2(0.5f, 0f);
        retryRect.pivot = new Vector2(0.5f, 0f);
        retryRect.anchoredPosition = new Vector2(0f, 25f);
        retryRect.sizeDelta = new Vector2(200f, 60f);

        Image retryImg = retryGo.GetComponent<Image>();
        if (retryImg == null) retryImg = retryGo.AddComponent<Image>();
        retryImg.enabled = true;
        retryImg.color = new Color(0.9f, 0.4f, 0.2f, 1f);

        Button rBtn = retryGo.GetComponent<Button>();
        if (rBtn == null) rBtn = retryGo.AddComponent<Button>();
        rBtn.interactable = true;
        rBtn.transition = Selectable.Transition.None;
        retryButton = rBtn;

        // Retry Text
        Transform rTextTrans = retryGo.transform.Find("Text");
        GameObject rTextGo;
        if (rTextTrans == null) {
            rTextGo = new GameObject("Text");
            rTextGo.transform.SetParent(retryGo.transform, false);
        } else {
            rTextGo = rTextTrans.gameObject;
        }

        RectTransform rTextRect = rTextGo.GetComponent<RectTransform>();
        if (rTextRect == null) rTextRect = rTextGo.AddComponent<RectTransform>();
        rTextRect.anchorMin = Vector2.zero;
        rTextRect.anchorMax = Vector2.one;
        rTextRect.offsetMin = Vector2.zero;
        rTextRect.offsetMax = Vector2.zero;

        TextMeshProUGUI rTMP = rTextGo.GetComponent<TextMeshProUGUI>();
        if (rTMP == null) rTMP = rTextGo.AddComponent<TextMeshProUGUI>();
        rTMP.alignment = TextAlignmentOptions.Center;
        rTMP.text = "RETRY";
        rTMP.fontSize = 24f;
        rTMP.fontStyle = FontStyles.Bold;
        rTMP.color = Color.white;
        if (howdybunFont != null) rTMP.font = howdybunFont;

        retryButton.onClick.RemoveAllListeners();
        retryButton.onClick.AddListener(() => {
            Debug.Log("[G01] RETRY tapped. Restarting Greeting Dash...");
            if (resultPopup != null) resultPopup.SetActive(false);
            InitializeGame();
        });
    }

    private void EndGame(bool hasPassed) {
        isGameActive = false;
        isTileActive = false;
        if (tileFallCoroutine != null) StopCoroutine(tileFallCoroutine);

        if (activeTileRectTransform != null) {
            activeTileRectTransform.gameObject.SetActive(false);
        }

        EnsureResultPopupAndRetryButton();

        if (resultPopup != null) {
            resultPopup.SetActive(true);
            resultPopup.transform.localScale = Vector3.zero;
            resultPopup.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = hasPassed ? "GREAT JOB!" : "GAME OVER!";
            resultTitleTMP.color = hasPassed ? new Color(0.2f, 0.8f, 0.35f, 1f) : new Color(0.9f, 0.25f, 0.25f, 1f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"SCORE: {scorePoints}   |   SORTED: {correctSorts}/{targetPassCount}";
        }

        if (retryButton != null) {
            retryButton.gameObject.SetActive(!hasPassed);
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(hasPassed);
            if (hasPassed) NextButtonAnimation();
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(hasPassed ? Masters_SFX.SelectPositive : Masters_SFX.Incorrect);
        }
    }

    private void UpdateUI() {
        string displayStr = $"SCORE: {scorePoints}\nSORTED: {correctSorts}/{targetPassCount}\nTIME: {Mathf.CeilToInt(timeRemaining)}s";

        if (scoreTMP != null) {
            scoreTMP.text = displayStr;
            scoreTMP.alignment = TextAlignmentOptions.TopRight;
            scoreTMP.enableAutoSizing = false;
            scoreTMP.fontSize = 24f;
        }
        if (sortedCounterTMP != null && sortedCounterTMP != scoreTMP) sortedCounterTMP.text = $"{correctSorts}/{targetPassCount}";
        if (timerTMP != null && timerTMP != scoreTMP) timerTMP.text = $"TIME: {Mathf.CeilToInt(timeRemaining)}s";
        if (livesTMP != null && livesTMP != scoreTMP) {
            string hearts = "";
            for (int i = 0; i < currentLives; i++) hearts += "♥ ";
            livesTMP.text = $"LIVES: {hearts.Trim()}";
        }
    }

    protected override void OnNextButtonClicked() {
        Debug.Log($"G01 Greeting Dash Completed. Final Score: {scorePoints}, Sorted: {correctSorts}/{targetPassCount}");
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