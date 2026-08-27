using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

/// <summary>
/// Game Controller for Unit 7 (Collocations) G01: Magnet Dash — Sort Partners to Hubs.
/// Timed arcade sorting game featuring:
/// - 4 Glowing Hub Gates: GET (0), CATCH (1), IDEA (2), SAVE (3)
/// - Sequential single-tile falling stream (one tile at a time, comfortable slow speed)
/// - Object-pooled falling partner tiles derived strictly from Unit 7 collocation webs
/// - Drag-and-Drop and Tap sorting interactions
/// - Magnet Snap on correct sort, Magnet Repel on wrong sort
/// - Full collocation flash on correct sort (e.g. "GET READY", "CATCH A TRAIN")
/// - 60s round timer, 3 initial lives, target >= 16 correct sorts to pass
/// - Game-G01 sub-flag progression tracking and Return to Hub navigation
/// </summary>
public class Masters_Collocations_Game_LessonOne : Masters_Lesson {

    public enum HubType {
        GET = 0,
        CATCH = 1,
        IDEA = 2,
        SAVE = 3
    }

    [System.Serializable]
    public class G01TileData {
        public string partnerText;          // e.g. "ready", "permission", "a train", "a cold", "water", "electricity", "clever idea", "good idea"
        public HubType correctHub;         // GET, CATCH, IDEA, SAVE
        public string fullCollocationText; // e.g. "GET READY", "CATCH A TRAIN", "SAVE WATER", "CLEVER IDEA"
        public AudioClip tileAudio;
    }

    [Header("G01 Partner Data (Verbatim Unit 7 Collocations)")]
    [SerializeField] private G01TileData[] tileDataBank;

    [Header("UI Gate Containers (4 Hubs)")]
    [SerializeField] private RectTransform getHubRect;
    [SerializeField] private RectTransform catchHubRect;
    [SerializeField] private RectTransform ideaHubRect;
    [SerializeField] private RectTransform saveHubRect;

    [SerializeField] private TextMeshProUGUI getHubTMP;
    [SerializeField] private TextMeshProUGUI catchHubTMP;
    [SerializeField] private TextMeshProUGUI ideaHubTMP;
    [SerializeField] private TextMeshProUGUI saveHubTMP;

    [Header("Spawn & Play Area")]
    [SerializeField] private RectTransform playAreaContainer;
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private float topSpawnY = 300f;
    [SerializeField] private float bottomFailY = -120f;

    [Header("Game State UI")]
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI livesTMP;
    [SerializeField] private TextMeshProUGUI sortedCounterTMP;
    [SerializeField] private TextMeshProUGUI flashBannerTMP;

    [Header("Title & Instruction UI")]
    [SerializeField] private TextMeshProUGUI g01TitleTMP;
    [SerializeField] private TextMeshProUGUI g01HeaderTMP;
    [SerializeField] private TextMeshProUGUI g01InstructionTMP;

    [Header("Result Popup")]
    [SerializeField] private GameObject resultPopup;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button returnHubButton;

    [Header("Audio References")]
    [SerializeField] private AudioClip ariaIntroAudio;
    [SerializeField] private AudioClip sfxMagnetSnap;
    [SerializeField] private AudioClip sfxMagnetRepel;

    [Header("Difficulty Parameters")]
    [SerializeField] private float roundDuration = 60f;
    [SerializeField] private int initialLives = 3;
    [SerializeField] private int targetPassCount = 16;

    // Relaxed, slow falling speeds (gives student 7-14 seconds to sort each tile)
    [SerializeField] private float speedTier1 = 30f; // 0-15s
    [SerializeField] private float speedTier2 = 40f; // 15-30s
    [SerializeField] private float speedTier3 = 50f; // 30-45s
    [SerializeField] private float speedTier4 = 60f; // 45-60s

    // Runtime state variables
    private List<G01TileController> tilePool = new List<G01TileController>();
    private List<G01TileController> activeTiles = new List<G01TileController>();
    private List<G01TileData> shuffledQueue = new List<G01TileData>();
    private int queueIndex = 0;

    private int currentLives = 3;
    private int correctSorts = 0;
    private int currentScore = 0;
    private float timeRemaining = 60f;
    private bool isGameActive = false;
    private bool isSpawningTile = false;
    private Canvas parentCanvas;

    protected virtual void OnEnable() {
        // Prevent unwanted STT subscriptions
    }

    protected virtual void OnDisable() {
        // Cleanup if needed
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;
        narratorSpeech = null;
        CancelInvoke();

        // Enforce relaxed slow fall speeds (gives student 7-14 seconds to sort each tile)
        speedTier1 = 30f;
        speedTier2 = 40f;
        speedTier3 = 50f;
        speedTier4 = 60f;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        parentCanvas = GetComponentInParent<Canvas>();

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeTileDataBank();
        CreateTilePool(8);
        UpdateTitleAndUIComponents();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Game;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeTileDataBank();
        UpdateTitleAndUIComponents();
        SetupButtonListeners();

        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (resultPopup != null) resultPopup.SetActive(false);
        if (flashBannerTMP != null) flashBannerTMP.gameObject.SetActive(false);

        PlayIntroVoiceover();
        StartNewGame();
    }

    private void DeactivateObsoleteBaseUI() {
        Transform skipTrans = transform.Find("SkipButton");
        if (skipTrans != null) skipTrans.gameObject.SetActive(false);

        Transform contTrans = transform.Find("Continue");
        if (contTrans != null) contTrans.gameObject.SetActive(false);

        Transform debugTrans = transform.Find("DebugText");
        if (debugTrans != null) debugTrans.gameObject.SetActive(false);

        // Deactivate obsolete GrooveOn text components
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string txt = tmp.text ?? "";
            string gName = tmp.name.ToLower();

            if (txt.Contains("GREETING") || txt.Contains("OCCASION") || gName.Contains("greeting") || gName.Contains("occasion")) {
                tmp.gameObject.SetActive(false);
            }
            if (gName.Contains("puzzlecount") || gName.Contains("progresscount") || (txt.Contains("0/6") && !gName.Contains("sorted"))) {
                tmp.gameObject.SetActive(false);
            }
        }
    }

    private void Update() {
        if (!isGameActive) return;

        // Timer Countdown
        timeRemaining -= Time.deltaTime;
        if (timeRemaining <= 0f) {
            timeRemaining = 0f;
            UpdateTimerUI();
            EndGameRound(true);
            return;
        }
        UpdateTimerUI();

        // Sequential One-by-One Tile Spawning: Spawn next tile only if screen is empty
        if (activeTiles.Count == 0 && !isSpawningTile) {
            StartCoroutine(SpawnNextTileWithDelay(0.6f));
        }

        // Calculate Current Falling Speed (Comfortable Slow Motion)
        float currentSpeed = GetCurrentFallSpeed();

        // Move Active Tiles Downward
        for (int i = activeTiles.Count - 1; i >= 0; i--) {
            var tile = activeTiles[i];
            if (tile != null && tile.gameObject.activeSelf && !tile.IsBeingDragged) {
                RectTransform rt = tile.GetComponent<RectTransform>();
                if (rt != null) {
                    rt.anchoredPosition += Vector2.down * currentSpeed * Time.deltaTime;

                    if (rt.anchoredPosition.y <= bottomFailY) {
                        OnTileReachedBottom(tile);
                    }
                }
            }
        }
    }

    private float GetCurrentFallSpeed() {
        float elapsed = roundDuration - timeRemaining;
        if (elapsed < 15f) return speedTier1;
        if (elapsed < 30f) return speedTier2;
        if (elapsed < 45f) return speedTier3;
        return speedTier4;
    }

    private IEnumerator SpawnNextTileWithDelay(float delay) {
        isSpawningTile = true;
        yield return new WaitForSeconds(delay);
        if (isGameActive && activeTiles.Count == 0) {
            SpawnNextTile();
        }
        isSpawningTile = false;
    }

    public void InitializeTileDataBank() {
        string audioDir = "Assets/Audio/2A/7_Collocations/Speaking/SP01/";

        tileDataBank = new G01TileData[] {
            #if UNITY_EDITOR
            new G01TileData { partnerText = "ready", correctHub = HubType.GET, fullCollocationText = "GET READY", tileAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "get ready.mp3") },
            #endif
            #if UNITY_EDITOR
            new G01TileData { partnerText = "permission", correctHub = HubType.GET, fullCollocationText = "GET PERMISSION", tileAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "get permission.mp3") },
            #endif
            #if UNITY_EDITOR
            new G01TileData { partnerText = "a train", correctHub = HubType.CATCH, fullCollocationText = "CATCH A TRAIN", tileAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "catch a train.mp3") },
            #endif
            #if UNITY_EDITOR
            new G01TileData { partnerText = "a cold", correctHub = HubType.CATCH, fullCollocationText = "CATCH A COLD", tileAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "catch a cold.mp3") },
            #endif
            #if UNITY_EDITOR
            new G01TileData { partnerText = "water", correctHub = HubType.SAVE, fullCollocationText = "SAVE WATER", tileAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "save water.mp3") },
            #endif
            #if UNITY_EDITOR
            new G01TileData { partnerText = "electricity", correctHub = HubType.SAVE, fullCollocationText = "SAVE ELECTRICITY", tileAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Switch off the fan to save electricity.mp3") },
            #endif
            #if UNITY_EDITOR
            new G01TileData { partnerText = "clever idea", correctHub = HubType.IDEA, fullCollocationText = "CLEVER IDEA", tileAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "clever idea.mp3") },
            #endif
            #if UNITY_EDITOR
            new G01TileData { partnerText = "good idea", correctHub = HubType.IDEA, fullCollocationText = "GOOD IDEA", tileAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "That was a clever idea.mp3") }
            #endif
        };
    }

    private void PlayIntroVoiceover() {
        if (ariaIntroAudio == null) {
            #if UNITY_EDITOR
            ariaIntroAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/7_Collocations/Game/G01/Quick Drag or tap each falling partner tile into its correct hub gate.mp3");
            if (ariaIntroAudio == null) {
                ariaIntroAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/7_Collocations/Game/G01/Quick get catch idea or save.mp3");
            }
            #endif
        }
        if (ariaIntroAudio != null) {
            narratorSpeech = ariaIntroAudio;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroAudio);
            }
        }
    }

    private void CreateTilePool(int poolSize) {
        if (playAreaContainer == null) playAreaContainer = transform.GetComponent<RectTransform>();

        for (int i = 0; i < poolSize; i++) {
            GameObject obj = null;
            if (tilePrefab != null) {
                obj = Instantiate(tilePrefab, playAreaContainer, false);
            } else {
                obj = CreateFallbackTileObject();
            }

            obj.name = $"PooledTile_{i}";
            obj.SetActive(false);

            G01TileController tileCtrl = obj.GetComponent<G01TileController>();
            if (tileCtrl == null) tileCtrl = obj.AddComponent<G01TileController>();

            tileCtrl.Initialize(this, parentCanvas);
            tilePool.Add(tileCtrl);
        }
    }

    private GameObject CreateFallbackTileObject() {
        GameObject tileObj = new GameObject("FallingTile", typeof(RectTransform), typeof(Image), typeof(G01TileController));
        tileObj.transform.SetParent(playAreaContainer, false);

        RectTransform rt = tileObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(230f, 70f);

        Image img = tileObj.GetComponent<Image>();
        img.color = new Color(0.12f, 0.25f, 0.52f, 0.95f);

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(tileObj.transform, false);

        RectTransform tRect = textObj.GetComponent<RectTransform>();
        tRect.sizeDelta = new Vector2(220f, 65f);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 28;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return tileObj;
    }

    private void BuildShuffledQueue() {
        shuffledQueue.Clear();
        if (tileDataBank != null && tileDataBank.Length > 0) {
            List<G01TileData> list = new List<G01TileData>(tileDataBank);
            for (int r = 0; r < 5; r++) {
                for (int i = 0; i < list.Count; i++) {
                    int randomIndex = Random.Range(i, list.Count);
                    var temp = list[i];
                    list[i] = list[randomIndex];
                    list[randomIndex] = temp;
                }
                shuffledQueue.AddRange(list);
            }
        }
        queueIndex = 0;
    }

    public void StartNewGame() {
        isGameActive = true;
        isSpawningTile = false;
        currentLives = initialLives;
        correctSorts = 0;
        currentScore = 0;
        timeRemaining = roundDuration;

        ReturnAllTilesToPool();
        BuildShuffledQueue();

        UpdateScoreUI();
        UpdateLivesUI();
        UpdateTimerUI();

        if (resultPopup != null) resultPopup.SetActive(false);

        // Spawn first tile
        SpawnNextTile();
    }

    private void SpawnNextTile() {
        if (!isGameActive) return;
        if (shuffledQueue == null || shuffledQueue.Count == 0) return;

        if (queueIndex >= shuffledQueue.Count) {
            BuildShuffledQueue();
        }

        G01TileData data = shuffledQueue[queueIndex++];

        G01TileController tile = GetAvailablePooledTile();
        if (tile != null) {
            // Spawn tile in center-top lane
            float xPos = Random.Range(-180f, 180f);
            Vector2 spawnPos = new Vector2(xPos, topSpawnY);

            tile.SetupTile(data, spawnPos);
            activeTiles.Add(tile);
        }
    }

    private G01TileController GetAvailablePooledTile() {
        foreach (var tile in tilePool) {
            if (tile != null && !tile.gameObject.activeSelf) {
                return tile;
            }
        }
        GameObject obj = CreateFallbackTileObject();
        obj.SetActive(false);
        G01TileController ctrl = obj.GetComponent<G01TileController>();
        ctrl.Initialize(this, parentCanvas);
        tilePool.Add(ctrl);
        return ctrl;
    }

    public void OnTileDropped(G01TileController tile, Vector2 dropScreenPosition) {
        if (!isGameActive || tile == null) return;

        HubType? droppedHub = CheckWhichHubDropped(dropScreenPosition, tile.GetComponent<RectTransform>());

        if (droppedHub.HasValue) {
            if (droppedHub.Value == tile.Data.correctHub) {
                OnCorrectSort(tile, droppedHub.Value);
            } else {
                OnWrongSort(tile, droppedHub.Value);
            }
        } else {
            tile.ResetToFallingPosition();
        }
    }

    private HubType? CheckWhichHubDropped(Vector2 dropScreenPos, RectTransform tileRect) {
        RectTransform[] hubRects = new RectTransform[] { getHubRect, catchHubRect, ideaHubRect, saveHubRect };
        HubType[] types = new HubType[] { HubType.GET, HubType.CATCH, HubType.IDEA, HubType.SAVE };

        Camera cam = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? parentCanvas.worldCamera : null;

        for (int i = 0; i < hubRects.Length; i++) {
            if (hubRects[i] != null) {
                if (RectTransformUtility.RectangleContainsScreenPoint(hubRects[i], dropScreenPos, cam)) {
                    return types[i];
                }

                Vector2 hubScreenPos = RectTransformUtility.WorldToScreenPoint(cam, hubRects[i].position);
                if (Vector2.Distance(dropScreenPos, hubScreenPos) < 150f) {
                    return types[i];
                }
            }
        }
        return null;
    }

    private void OnCorrectSort(G01TileController tile, HubType hub) {
        correctSorts++;
        currentScore += 100;
        UpdateScoreUI();

        if (sfxMagnetSnap != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxMagnetSnap);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        FlashCollocationBanner(tile.Data.fullCollocationText);
        AnimateHubGlow(hub);

        RecycleTile(tile);
    }

    private void OnWrongSort(G01TileController tile, HubType hub) {
        currentLives--;
        if (currentLives < 0) currentLives = 0;
        UpdateLivesUI();

        if (sfxMagnetRepel != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxMagnetRepel);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        tile.AnimateRepel(RecycleTile);

        if (currentLives <= 0) {
            EndGameRound(false);
        }
    }

    private void OnTileReachedBottom(G01TileController tile) {
        currentLives--;
        if (currentLives < 0) currentLives = 0;
        UpdateLivesUI();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        RecycleTile(tile);

        if (currentLives <= 0) {
            EndGameRound(false);
        }
    }

    private void RecycleTile(G01TileController tile) {
        if (tile != null) {
            activeTiles.Remove(tile);
            tile.Recycle();
        }
    }

    private void FlashCollocationBanner(string fullText) {
        if (flashBannerTMP != null) {
            flashBannerTMP.gameObject.SetActive(true);
            flashBannerTMP.text = fullText;
            flashBannerTMP.transform.DOKill();
            flashBannerTMP.transform.localScale = Vector3.zero;
            flashBannerTMP.transform.DOScale(Vector3.one * 1.2f, 0.25f).SetEase(Ease.OutBack).OnComplete(() => {
                flashBannerTMP.transform.DOScale(Vector3.one, 0.15f);
                DOVirtual.DelayedCall(0.8f, () => {
                    if (flashBannerTMP != null) flashBannerTMP.gameObject.SetActive(false);
                });
            });
        }
    }

    private void AnimateHubGlow(HubType hub) {
        RectTransform targetRect = null;
        switch (hub) {
            case HubType.GET: targetRect = getHubRect; break;
            case HubType.CATCH: targetRect = catchHubRect; break;
            case HubType.IDEA: targetRect = ideaHubRect; break;
            case HubType.SAVE: targetRect = saveHubRect; break;
        }

        if (targetRect != null) {
            targetRect.DOKill();
            targetRect.DOScale(Vector3.one * 1.15f, 0.15f).SetLoops(2, LoopType.Yoyo).OnComplete(() => targetRect.localScale = Vector3.one);
        }
    }

    private void EndGameRound(bool isTimeout) {
        isGameActive = false;
        CancelInvoke();
        ReturnAllTilesToPool();

        bool passed = (correctSorts >= targetPassCount);

        if (passed) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Game);
            }
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }

        ShowResultPopup(passed, isTimeout);
    }

    private void ShowResultPopup(bool passed, bool isTimeout) {
        if (resultPopup != null) {
            resultPopup.SetActive(true);
            resultPopup.transform.DOKill();
            resultPopup.transform.localScale = Vector3.zero;
            resultPopup.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = passed ? "MAGNET DASH PASSED!" : (isTimeout ? "TIME IS UP!" : "GAME OVER!");
            resultTitleTMP.color = passed ? new Color(0.13f, 0.77f, 0.36f) : new Color(0.85f, 0.2f, 0.2f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Sorted: {correctSorts}/{targetPassCount}\nScore: {currentScore}\n{(passed ? "Sub-flag Game-G01 Unlocked!" : "Sort 16 or more to unlock!")}";
        }
    }

    private void ReturnAllTilesToPool() {
        for (int i = activeTiles.Count - 1; i >= 0; i--) {
            if (activeTiles[i] != null) activeTiles[i].Recycle();
        }
        activeTiles.Clear();
    }

    private void UpdateTimerUI() {
        int sec = Mathf.CeilToInt(timeRemaining);
        if (timerTMP != null) {
            timerTMP.text = $"TIME: {sec}s";
            timerTMP.color = sec <= 10 ? new Color(0.9f, 0.2f, 0.2f) : Color.white;
        }
    }

    private void UpdateScoreUI() {
        if (scoreTMP != null) scoreTMP.text = $"SCORE: {currentScore}";
        if (sortedCounterTMP != null) sortedCounterTMP.text = $"SORTED: {correctSorts}/{targetPassCount}";
    }

    private void UpdateLivesUI() {
        if (livesTMP != null) {
            string hearts = "";
            for (int i = 0; i < currentLives; i++) hearts += "♥ ";
            livesTMP.text = hearts.Trim();
        }
    }

    private void UpdateTitleAndUIComponents() {
        if (g01TitleTMP != null) {
            g01TitleTMP.gameObject.SetActive(true);
            g01TitleTMP.text = "G01 Magnet Dash — Sort Partners to Hubs";
            g01TitleTMP.color = Color.white;
        }
        if (g01HeaderTMP != null) {
            g01HeaderTMP.gameObject.SetActive(true);
            g01HeaderTMP.text = "GAME BRANCH (Game Bench)";
            g01HeaderTMP.color = new Color(0.13f, 0.77f, 0.36f);
        }
        if (g01InstructionTMP != null) {
            g01InstructionTMP.gameObject.SetActive(true);
            g01InstructionTMP.text = "Drag or tap each falling partner tile into its correct hub gate!";
            g01InstructionTMP.color = new Color(0.9f, 0.95f, 1f);
        }
    }

    private void SetupButtonListeners() {
        if (retryButton != null) {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(StartNewGame);
        }

        if (returnHubButton != null) {
            returnHubButton.onClick.RemoveAllListeners();
            returnHubButton.onClick.AddListener(ReturnToHub);
        }
    }

    protected override void OnNextButtonClicked() {
        ReturnToHub();
    }

    public void ReturnToHub() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Game);
        }
    }

    private void AutoFindUIReferences() {
        Transform getT = transform.Find("HubsContainer/GET") ?? transform.Find("GET");
        if (getT != null) {
            getHubRect = getT.GetComponent<RectTransform>();
            getHubTMP = getT.GetComponentInChildren<TextMeshProUGUI>();
        }

        Transform catchT = transform.Find("HubsContainer/CATCH") ?? transform.Find("CATCH");
        if (catchT != null) {
            catchHubRect = catchT.GetComponent<RectTransform>();
            catchHubTMP = catchT.GetComponentInChildren<TextMeshProUGUI>();
        }

        Transform ideaT = transform.Find("HubsContainer/IDEA") ?? transform.Find("IDEA");
        if (ideaT != null) {
            ideaHubRect = ideaT.GetComponent<RectTransform>();
            ideaHubTMP = ideaT.GetComponentInChildren<TextMeshProUGUI>();
        }

        Transform saveT = transform.Find("HubsContainer/SAVE") ?? transform.Find("SAVE");
        if (saveT != null) {
            saveHubRect = saveT.GetComponent<RectTransform>();
            saveHubTMP = saveT.GetComponentInChildren<TextMeshProUGUI>();
        }

        if (timerTMP == null) {
            Transform t = transform.Find("TimerText") ?? transform.Find("TopBar/TimerText");
            if (t != null) timerTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (scoreTMP == null) {
            Transform t = transform.Find("ScoreText") ?? transform.Find("TopBar/ScoreText");
            if (t != null) scoreTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (livesTMP == null) {
            Transform t = transform.Find("LivesText") ?? transform.Find("TopBar/LivesText");
            if (t != null) livesTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (sortedCounterTMP == null) {
            Transform t = transform.Find("SortedCounterText");
            if (t != null) sortedCounterTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (flashBannerTMP == null) {
            Transform t = transform.Find("FlashBannerText");
            if (t != null) flashBannerTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (g01TitleTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("Title");
            if (t != null) g01TitleTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (g01HeaderTMP == null) {
            Transform t = transform.Find("Heading") ?? transform.Find("Header");
            if (t != null) g01HeaderTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (g01InstructionTMP == null) {
            Transform t = transform.Find("InstructionText") ?? transform.Find("Instruction");
            if (t != null) g01InstructionTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (resultPopup == null) {
            Transform t = transform.Find("ResultPopup") ?? transform.Find("ResultPanel");
            if (t != null) resultPopup = t.gameObject;
        }

        if (resultPopup != null) {
            Button[] resBtns = resultPopup.GetComponentsInChildren<Button>(true);
            foreach (var b in resBtns) {
                if (b == null) continue;
                string bName = b.name.ToLower();
                if (retryButton == null && (bName.Contains("retry") || bName.Contains("again"))) retryButton = b;
                if (returnHubButton == null && (bName.Contains("hub") || bName.Contains("home") || bName.Contains("continue"))) returnHubButton = b;
            }
        }
    }
}

/// <summary>
/// Helper MonoBehaviour attached to each pooled tile GameObject for drag, drop, and repel animations.
/// </summary>
public class G01TileController : MonoBehaviour, IPointerDownHandler, IDragHandler, IEndDragHandler {

    private Masters_Collocations_Game_LessonOne manager;
    private Canvas parentCanvas;
    private RectTransform rectTransform;
    private TextMeshProUGUI textComponent;
    private Image bgImage;

    public Masters_Collocations_Game_LessonOne.G01TileData Data { get; private set; }
    public bool IsBeingDragged { get; private set; }
    private Vector2 originalPosition;

    public void Initialize(Masters_Collocations_Game_LessonOne mgr, Canvas canvas) {
        manager = mgr;
        parentCanvas = canvas;
        rectTransform = GetComponent<RectTransform>();
        textComponent = GetComponentInChildren<TextMeshProUGUI>(true);
        bgImage = GetComponent<Image>();
    }

    public void SetupTile(Masters_Collocations_Game_LessonOne.G01TileData tileData, Vector2 spawnAnchoredPosition) {
        Data = tileData;
        IsBeingDragged = false;

        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (textComponent == null) textComponent = GetComponentInChildren<TextMeshProUGUI>(true);

        rectTransform.anchoredPosition = spawnAnchoredPosition;
        originalPosition = spawnAnchoredPosition;

        if (textComponent != null) {
            textComponent.text = tileData.partnerText;
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void OnPointerDown(PointerEventData eventData) {
        IsBeingDragged = true;
        originalPosition = rectTransform.anchoredPosition;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData) {
        if (!IsBeingDragged || rectTransform == null) return;

        Camera cam = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? parentCanvas.worldCamera : null;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform.parent as RectTransform, eventData.position, cam, out localPoint)) {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData) {
        IsBeingDragged = false;
        if (manager != null) {
            manager.OnTileDropped(this, eventData.position);
        }
    }

    public void ResetToFallingPosition() {
        IsBeingDragged = false;
    }

    public void AnimateRepel(System.Action<G01TileController> onComplete) {
        if (rectTransform != null) {
            rectTransform.DOKill();
            rectTransform.DOAnchorPosY(rectTransform.anchoredPosition.y + 120f, 0.2f).SetEase(Ease.OutBounce).OnComplete(() => {
                onComplete?.Invoke(this);
            });
        } else {
            onComplete?.Invoke(this);
        }
    }

    public void Recycle() {
        IsBeingDragged = false;
        if (rectTransform != null) rectTransform.DOKill();
        gameObject.SetActive(false);
    }
}