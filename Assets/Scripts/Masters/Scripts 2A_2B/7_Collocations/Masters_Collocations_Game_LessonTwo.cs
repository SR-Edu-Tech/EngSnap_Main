using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

/// <summary>
/// Controller for Unit 7 (Collocations) Game Branch - Stage G02: Two Halves to Make One.
/// Untimed matching puzzle where 10 Hub tiles and 10 Partner tiles are scattered on the floor.
/// Students tap or drag a Hub tile and its matching Partner tile to lock them into a full collocation phrase.
/// Features:
/// - 10 verbatim Unit 7 collocation pairs
/// - Tap-to-match & Drag-to-match interaction
/// - Pre-built editor hierarchy visibility for all 20 tiles in FloorContainer
/// - Magnet Snap on correct match, soft repel on wrong match (untimed, no lives lost)
/// - Full collocation flash on match (e.g. "GET READY", "CATCH A TRAIN")
/// - Completion readback of all 10 collocations
/// - Game-G02 sub-flag completion tracking and Return to Hub navigation
/// </summary>
public class Masters_Collocations_Game_LessonTwo : Masters_Lesson {

    public enum TileType {
        HubTile,
        PartnerTile
    }

    [System.Serializable]
    public class G02CollocationPair {
        public int pairId;
        public string firstHalf;        // e.g. "GET", "CATCH", "SAVE", "IDEA", "CLEVER", "GOOD"
        public string secondHalf;       // e.g. "ready", "permission", "a train", "a cold", "water", "electricity", "idea", "dressed", "money"
        public string fullCollocation;  // e.g. "GET READY", "CATCH A TRAIN", "SAVE WATER", "CLEVER IDEA"
        public AudioClip readbackAudio;
    }

    [Header("G02 Collocation Data (10 Pairs)")]
    [SerializeField] private G02CollocationPair[] collocationPairs;

    [Header("UI Grid / Floor Container")]
    [SerializeField] private RectTransform floorContainer;
    [SerializeField] private GameObject tilePrefab;

    [Header("Game State UI")]
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI feedbackBannerTMP;
    [SerializeField] private TextMeshProUGUI flashBannerTMP;

    [Header("Title & Instruction UI")]
    [SerializeField] private TextMeshProUGUI g02TitleTMP;
    [SerializeField] private TextMeshProUGUI g02HeaderTMP;
    [SerializeField] private TextMeshProUGUI g02InstructionTMP;

    [Header("Result Popup / Completed List")]
    [SerializeField] private GameObject resultPopup;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultListTMP;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button returnHubButton;

    [Header("Audio References")]
    [SerializeField] private AudioClip ariaIntroAudio;
    [SerializeField] private AudioClip sfxMagnetSnap;
    [SerializeField] private AudioClip sfxMagnetRepel;

    // Runtime state variables
    private List<G02TileUI> spawnedTiles = new List<G02TileUI>();
    private G02TileUI selectedHubTile = null;
    private G02TileUI selectedPartnerTile = null;

    private int matchedCount = 0;
    private const int TOTAL_PAIRS = 10;
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

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        parentCanvas = GetComponentInParent<Canvas>();

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeCollocationPairs();
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
        InitializeCollocationPairs();
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
    }

    public void InitializeCollocationPairs() {
        string audioDir = "Assets/Audio/2A/7_Collocations/Speaking/SP01/";

        collocationPairs = new G02CollocationPair[] {
            #if UNITY_EDITOR
            new G02CollocationPair { pairId = 1, firstHalf = "GET", secondHalf = "ready", fullCollocation = "GET READY", readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "get ready.mp3") },
            new G02CollocationPair { pairId = 2, firstHalf = "GET", secondHalf = "permission", fullCollocation = "GET PERMISSION", readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "get permission.mp3") },
            new G02CollocationPair { pairId = 3, firstHalf = "GET", secondHalf = "a job", fullCollocation = "GET A JOB", readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "get ready.mp3") },
            new G02CollocationPair { pairId = 4, firstHalf = "CATCH", secondHalf = "a bus", fullCollocation = "CATCH A BUS", readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "catch a train.mp3") },
            new G02CollocationPair { pairId = 5, firstHalf = "CATCH", secondHalf = "a cold", fullCollocation = "CATCH A COLD", readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "catch a cold.mp3") },
            new G02CollocationPair { pairId = 6, firstHalf = "CATCH", secondHalf = "fire", fullCollocation = "CATCH FIRE", readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "catch a cold.mp3") },
            new G02CollocationPair { pairId = 7, firstHalf = "IDEA", secondHalf = "bright idea", fullCollocation = "BRIGHT IDEA", readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "clever idea.mp3") },
            new G02CollocationPair { pairId = 8, firstHalf = "IDEA", secondHalf = "good idea", fullCollocation = "GOOD IDEA", readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "That was a clever idea.mp3") },
            new G02CollocationPair { pairId = 9, firstHalf = "SAVE", secondHalf = "water", fullCollocation = "SAVE WATER", readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "save water.mp3") },
            new G02CollocationPair { pairId = 10, firstHalf = "SAVE", secondHalf = "time", fullCollocation = "SAVE TIME", readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Switch off the fan to save electricity.mp3") }
            #endif
        };
    }

    private void PlayIntroVoiceover() {
        if (ariaIntroAudio == null) {
            #if UNITY_EDITOR
            ariaIntroAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/7_Collocations/Game/G02/Every half is looking for its partner - bring them together.mp3");
            #endif
        }
        if (ariaIntroAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroAudio);
        }
    }

    public void StartNewGame() {
        matchedCount = 0;
        selectedHubTile = null;
        selectedPartnerTile = null;

        UpdateProgressUI();
        ShowFeedback("Tap a hub tile then its matching partner!", true);

        if (resultPopup != null) resultPopup.SetActive(false);

        GenerateFloorTiles();
    }

    private void GenerateFloorTiles() {
        if (floorContainer == null) floorContainer = transform.GetComponent<RectTransform>();

        List<TileSpawnData> spawnList = new List<TileSpawnData>();

        for (int i = 0; i < collocationPairs.Length; i++) {
            var pair = collocationPairs[i];
            spawnList.Add(new TileSpawnData { pairData = pair, type = TileType.HubTile, text = pair.firstHalf, tileName = $"Tile_HubTile_{pair.pairId}" });
            spawnList.Add(new TileSpawnData { pairData = pair, type = TileType.PartnerTile, text = pair.secondHalf, tileName = $"Tile_PartnerTile_{pair.pairId}" });
        }

        // Calculate 4x5 Grid positions across floor container
        List<Vector2> gridPositions = new List<Vector2>();
        float startX = -360f;
        float startY = 160f;
        float stepX = 180f;
        float stepY = -90f;

        for (int i = 0; i < spawnList.Count; i++) {
            int col = i % 5;
            int row = i / 5;
            gridPositions.Add(new Vector2(startX + (col * stepX), startY + (row * stepY)));
        }

        // Shuffle grid positions
        for (int i = 0; i < gridPositions.Count; i++) {
            int r = Random.Range(i, gridPositions.Count);
            var temp = gridPositions[i];
            gridPositions[i] = gridPositions[r];
            gridPositions[r] = temp;
        }

        // Find existing pre-constructed tiles in floorContainer
        G02TileUI[] existingTiles = floorContainer.GetComponentsInChildren<G02TileUI>(true);
        Dictionary<string, G02TileUI> existingMap = new Dictionary<string, G02TileUI>();
        foreach (var t in existingTiles) {
            if (t != null) existingMap[t.name] = t;
        }

        spawnedTiles.Clear();

        for (int i = 0; i < spawnList.Count; i++) {
            var data = spawnList[i];
            Vector2 pos = gridPositions[i];

            G02TileUI tileUI = null;
            if (existingMap.ContainsKey(data.tileName) && existingMap[data.tileName] != null) {
                tileUI = existingMap[data.tileName];
            } else {
                GameObject tileObj = (tilePrefab != null) ? Instantiate(tilePrefab, floorContainer, false) : CreateFallbackTileObject(data.type);
                tileObj.name = data.tileName;
                tileUI = tileObj.GetComponent<G02TileUI>();
                if (tileUI == null) tileUI = tileObj.AddComponent<G02TileUI>();
            }

            tileUI.gameObject.SetActive(true);
            tileUI.transform.DOKill();
            tileUI.transform.localScale = Vector3.one;

            RectTransform rt = tileUI.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = pos;

            tileUI.Setup(this, parentCanvas, data.pairData, data.type, data.text);
            spawnedTiles.Add(tileUI);
        }
    }

    private GameObject CreateFallbackTileObject(TileType type) {
        GameObject tileObj = new GameObject("TileUI", typeof(RectTransform), typeof(Image), typeof(G02TileUI));
        tileObj.transform.SetParent(floorContainer, false);

        RectTransform rt = tileObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(165f, 65f);

        Image img = tileObj.GetComponent<Image>();
        #if UNITY_EDITOR
        Sprite pillSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Textures/UI/RoundedPillCombined.png");
        if (pillSprite != null) {
            img.sprite = pillSprite;
            img.type = Image.Type.Sliced;
        }
        #endif
        img.color = (type == TileType.HubTile) ? new Color(0.12f, 0.45f, 0.85f, 0.95f) : new Color(0.12f, 0.25f, 0.48f, 0.95f);

        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(tileObj.transform, false);

        RectTransform tRect = textObj.GetComponent<RectTransform>();
        tRect.sizeDelta = new Vector2(155f, 60f);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        #if UNITY_EDITOR
        TMP_FontAsset howdybunFont = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Howdybun SDF 1.asset");
        if (howdybunFont != null) tmp.font = howdybunFont;
        #endif
        tmp.fontSize = 24;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return tileObj;
    }

    private struct TileSpawnData {
        public G02CollocationPair pairData;
        public TileType type;
        public string text;
        public string tileName;
    }

    public void OnTileSelected(G02TileUI tile) {
        if (tile == null || tile.IsMatched) return;

        if (tile.Type == TileType.HubTile) {
            if (selectedHubTile != null) selectedHubTile.SetSelected(false);
            selectedHubTile = tile;
            selectedHubTile.SetSelected(true);
            ShowFeedback($"Selected '{tile.Text}'. Now tap its matching partner!", true);
        } else {
            if (selectedPartnerTile != null) selectedPartnerTile.SetSelected(false);
            selectedPartnerTile = tile;
            selectedPartnerTile.SetSelected(true);
            ShowFeedback($"Selected '{tile.Text}'.", true);
        }

        // Evaluate pair match if both hub and partner tiles are selected
        if (selectedHubTile != null && selectedPartnerTile != null) {
            EvaluatePairMatch();
        }
    }

    public void OnTileDroppedOnto(G02TileUI draggedTile, G02TileUI targetTile) {
        if (draggedTile == null || targetTile == null || draggedTile == targetTile) return;

        if (draggedTile.Type == TileType.HubTile && targetTile.Type == TileType.PartnerTile) {
            selectedHubTile = draggedTile;
            selectedPartnerTile = targetTile;
            EvaluatePairMatch();
        } else if (draggedTile.Type == TileType.PartnerTile && targetTile.Type == TileType.HubTile) {
            selectedHubTile = targetTile;
            selectedPartnerTile = draggedTile;
            EvaluatePairMatch();
        }
    }

    private void EvaluatePairMatch() {
        if (selectedHubTile == null || selectedPartnerTile == null) return;

        G02TileUI hub = selectedHubTile;
        G02TileUI partner = selectedPartnerTile;

        if (hub.PairData.pairId == partner.PairData.pairId) {
            OnCorrectMatch(hub, partner);
        } else {
            OnWrongMatch(hub, partner);
        }

        selectedHubTile = null;
        selectedPartnerTile = null;
    }

    private void OnCorrectMatch(G02TileUI hub, G02TileUI partner) {
        matchedCount++;
        UpdateProgressUI();

        if (sfxMagnetSnap != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxMagnetSnap);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        FlashCollocationBanner(hub.PairData.fullCollocation);
        ShowFeedback($"Correct! {hub.PairData.fullCollocation}", true);

        hub.SetMatched();
        partner.SetMatched();

        if (matchedCount >= TOTAL_PAIRS) {
            StartCoroutine(HandleAllPairsCompleted());
        }
    }

    private void OnWrongMatch(G02TileUI hub, G02TileUI partner) {
        if (sfxMagnetRepel != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxMagnetRepel);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        ShowFeedback("Try another partner!", false);

        hub.AnimateRepel();
        partner.AnimateRepel();
    }

    private IEnumerator HandleAllPairsCompleted() {
        yield return new WaitForSeconds(1.0f);

        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Game);
        }

        if (resultPopup != null) {
            resultPopup.SetActive(true);
            resultPopup.transform.DOKill();
            resultPopup.transform.localScale = Vector3.zero;
            resultPopup.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = "PUZZLE COMPLETED!";
            resultTitleTMP.color = new Color(0.13f, 0.77f, 0.36f);
        }

        if (resultListTMP != null) {
            string listText = "All 10 Collocations Locked:\n";
            for (int i = 0; i < collocationPairs.Length; i++) {
                listText += $"✔ {collocationPairs[i].fullCollocation}  ";
                if ((i + 1) % 2 == 0) listText += "\n";
            }
            resultListTMP.text = listText;
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
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

    private void ShowFeedback(string msg, bool isSuccess) {
        if (feedbackBannerTMP != null) {
            feedbackBannerTMP.gameObject.SetActive(true);
            feedbackBannerTMP.text = msg;
            feedbackBannerTMP.color = isSuccess ? new Color(0.9f, 0.95f, 1f) : new Color(0.95f, 0.3f, 0.3f);
        }
    }

    private void UpdateProgressUI() {
        if (progressTMP != null) {
            progressTMP.text = $"Pairs matched: {matchedCount}/{TOTAL_PAIRS}";
        }
    }

    private void UpdateTitleAndUIComponents() {
        if (g02TitleTMP != null) {
            g02TitleTMP.gameObject.SetActive(true);
            g02TitleTMP.text = "G02 Two Halves to Make One";
            g02TitleTMP.color = Color.white;
        }
        if (g02HeaderTMP != null) {
            g02HeaderTMP.gameObject.SetActive(true);
            g02HeaderTMP.text = "GAME BRANCH (Game Bench)";
            g02HeaderTMP.color = new Color(0.13f, 0.77f, 0.36f);
        }
        if (g02InstructionTMP != null) {
            g02InstructionTMP.gameObject.SetActive(true);
            g02InstructionTMP.text = "Tap a hub tile then its matching partner (or drag one onto the other)!";
            g02InstructionTMP.color = new Color(0.9f, 0.95f, 1f);
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
        if (floorContainer == null) {
            Transform t = transform.Find("FloorContainer") ?? transform.Find("TilesContainer");
            if (t != null) floorContainer = t.GetComponent<RectTransform>();
        }

        if (progressTMP == null) {
            Transform t = transform.Find("ProgressText") ?? transform.Find("ProgressIndicator");
            if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (feedbackBannerTMP == null) {
            Transform t = transform.Find("FeedbackText");
            if (t != null) feedbackBannerTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (flashBannerTMP == null) {
            Transform t = transform.Find("FlashBannerText");
            if (t != null) flashBannerTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (g02TitleTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("Title");
            if (t != null) g02TitleTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (g02HeaderTMP == null) {
            Transform t = transform.Find("Heading") ?? transform.Find("Header");
            if (t != null) g02HeaderTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (g02InstructionTMP == null) {
            Transform t = transform.Find("InstructionText") ?? transform.Find("Instruction");
            if (t != null) g02InstructionTMP = t.GetComponent<TextMeshProUGUI>();
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
/// Helper MonoBehaviour attached to each G02 tile GameObject for tap and drag interactions.
/// </summary>
public class G02TileUI : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IDragHandler, IEndDragHandler {

    private Masters_Collocations_Game_LessonTwo manager;
    private Canvas parentCanvas;
    private RectTransform rectTransform;
    private TextMeshProUGUI textComponent;
    private Image bgImage;

    public Masters_Collocations_Game_LessonTwo.G02CollocationPair PairData { get; private set; }
    public Masters_Collocations_Game_LessonTwo.TileType Type { get; private set; }
    public string Text { get; private set; }
    public bool IsMatched { get; private set; }
    public bool IsSelected { get; private set; }

    private Vector2 originalPos;
    private Color defaultColor;

    public void Setup(Masters_Collocations_Game_LessonTwo mgr, Canvas canvas, Masters_Collocations_Game_LessonTwo.G02CollocationPair pair, Masters_Collocations_Game_LessonTwo.TileType tileType, string textVal) {
        manager = mgr;
        parentCanvas = canvas;
        PairData = pair;
        Type = tileType;
        Text = textVal;
        IsMatched = false;
        IsSelected = false;

        rectTransform = GetComponent<RectTransform>();
        textComponent = GetComponentInChildren<TextMeshProUGUI>(true);
        bgImage = GetComponent<Image>();

        if (textComponent != null) textComponent.text = textVal;

        defaultColor = (Type == Masters_Collocations_Game_LessonTwo.TileType.HubTile) ? new Color(0.12f, 0.45f, 0.85f, 0.95f) : new Color(0.12f, 0.25f, 0.48f, 0.95f);
        if (bgImage != null) bgImage.color = defaultColor;

        gameObject.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData) {
        if (IsMatched || manager == null) return;
        manager.OnTileSelected(this);
    }

    public void OnPointerDown(PointerEventData eventData) {
        if (IsMatched || rectTransform == null) return;
        originalPos = rectTransform.anchoredPosition;
        transform.SetAsLastSibling();
    }

    public void OnDrag(PointerEventData eventData) {
        if (IsMatched || rectTransform == null) return;
        Camera cam = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? parentCanvas.worldCamera : null;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform.parent as RectTransform, eventData.position, cam, out localPoint)) {
            rectTransform.anchoredPosition = localPoint;
        }
    }

    public void OnEndDrag(PointerEventData eventData) {
        if (IsMatched) return;

        Camera cam = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? parentCanvas.worldCamera : null;

        // Check if dropped onto another tile
        var raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        G02TileUI targetTile = null;
        foreach (var result in raycastResults) {
            if (result.gameObject != null && result.gameObject != this.gameObject) {
                targetTile = result.gameObject.GetComponentInParent<G02TileUI>();
                if (targetTile != null) break;
            }
        }

        if (targetTile != null && manager != null) {
            manager.OnTileDroppedOnto(this, targetTile);
        } else {
            rectTransform.DOAnchorPos(originalPos, 0.2f);
        }
    }

    public void SetSelected(bool selected) {
        IsSelected = selected;
        if (bgImage != null) {
            bgImage.color = selected ? new Color(0.95f, 0.65f, 0.12f, 1f) : defaultColor;
        }
    }

    public void SetMatched() {
        IsMatched = true;
        IsSelected = false;

        if (bgImage != null) bgImage.color = new Color(0.13f, 0.77f, 0.36f, 1f);

        transform.DOScale(Vector3.zero, 0.4f).SetDelay(0.3f).OnComplete(() => {
            gameObject.SetActive(false);
        });
    }

    public void AnimateRepel() {
        SetSelected(false);
        if (rectTransform != null) {
            rectTransform.DOShakePosition(0.3f, 15f, 10, 90f);
        }
    }
}