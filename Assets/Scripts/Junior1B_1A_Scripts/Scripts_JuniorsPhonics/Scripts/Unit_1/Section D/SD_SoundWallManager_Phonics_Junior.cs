using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class SD_SoundWallManager_Phonics_Junior : MonoBehaviour
{
    [Header("Data")]
    public SD_SoundTileData_Phonics_Junior[] soundTiles;

    [Header("Prefab")]
    public GameObject soundTilePrefab;

    [Header("Table Container")]
    [SerializeField] private GameObject topBar;
    [SerializeField] private GameObject headingsParent;
    [SerializeField] private Transform tableContent;
    [SerializeField] private Transform monophthongContent;
    [SerializeField] private Transform diphthongContent;
    [SerializeField] private Transform vowelContent;
    [SerializeField] private Transform consonantContent;
    [SerializeField] private Transform moreSoundContent;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Instruction")]
    [SerializeField] private GameObject instructionPanel;
    [SerializeField] private TMP_Text instructionText;
    [SerializeField] private AudioClip instructionAudio;
    [SerializeField] private CanvasGroup instructionCanvas;
    [SerializeField] private RectTransform instructionRect;
    [SerializeField] private float typingSpeed = 0.03f;
    private bool isAudioPlaying = false;

    private bool tilesCreated = false;
    private Coroutine instructionCoroutine;

    [Header("Navigation")]
    [SerializeField] private Button nextButton;
    [SerializeField] private GameObject backButtonObj;
    [SerializeField] private GameObject unitCompletionPanel;
    [SerializeField] private GameObject sectionSelectionPanel;
    [SerializeField] private GameObject sectionDPanel;
    private int visitedCount = 0;

    private static readonly Color DefaultCellColor = new Color(1f, 1f, 1f, 1f);
    private static readonly Color BlankCellColor = new Color(1f, 1f, 1f, 0.15f);

    private struct Page9CellDef
    {
        public string symbol;
        public string keyword;
        public Page9CellDef(string s, string k) { symbol = s; keyword = k; }
    }

    private void Awake()
    {
        EnsureInit();
    }

    private void OnEnable()
    {
        EnsureInit();
        OpenSoundWall();
    }

    [Header("Tabbed Sound Wall")]
    [SerializeField] private Button vowelsTabBtn;
    [SerializeField] private Button consonantsTabBtn;
    private List<GameObject> vowelCellObjects = new List<GameObject>();
    private List<GameObject> consonantCellObjects = new List<GameObject>();
    private bool isVowelsTabActive = true;

    private void EnsureInit()
    {
        if (sectionDPanel == null) sectionDPanel = gameObject;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.enabled = true;
        audioSource.mute = false;
        audioSource.volume = 1f;

        if (instructionAudio == null)
        {
            instructionAudio = Resources.Load<AudioClip>("Unit1_SD_Instruction");
            if (instructionAudio == null) instructionAudio = Resources.Load<AudioClip>("SectionD_Instruction");
            if (instructionAudio == null) instructionAudio = Resources.Load<AudioClip>("Audio/Unit1_SD_Instruction");
            if (instructionAudio == null) instructionAudio = Resources.Load<AudioClip>("Audio/SectionD_Instruction");
        }

        Transform searchRoot = transform;
        if (topBar == null && searchRoot.Find("TopBar") != null)
            topBar = searchRoot.Find("TopBar").gameObject;

        if (headingsParent == null && searchRoot.Find("headings parent") != null)
            headingsParent = searchRoot.Find("headings parent").gameObject;

        if (instructionPanel == null)
        {
            Transform t = searchRoot.Find("Instruction Panel (1)");
            if (t == null) t = searchRoot.Find("Instruction Panel");
            if (t != null) instructionPanel = t.gameObject;
        }

        if (unitCompletionPanel == null)
        {
            Transform t = searchRoot.Find("Unit Completion Panel");
            if (t == null) t = searchRoot.Find("Completion Panel");
            if (t != null) unitCompletionPanel = t.gameObject;
        }

        if (backButtonObj == null)
        {
            Transform t = searchRoot.Find("Back Button");
            if (t == null) t = searchRoot.Find("Back_Button");
            if (t != null) backButtonObj = t.gameObject;
        }

        if (tableContent == null)
        {
            Transform t = searchRoot.Find("Scroll View/Viewport/content");
            if (t == null) t = searchRoot.Find("Viewport/content");
            if (t == null) t = searchRoot.Find("Table Content");
            if (t == null) t = searchRoot.Find("Vowel Content");
            if (t != null) tableContent = t;
        }

        if (monophthongContent == null) monophthongContent = tableContent;
        if (diphthongContent == null) diphthongContent = tableContent;
        if (vowelContent == null) vowelContent = tableContent;
        if (consonantContent == null) consonantContent = tableContent;
        if (moreSoundContent == null) moreSoundContent = tableContent;

        // Auto-resolve Vowels and Consonants sidebar tab buttons (adds Button component automatically if it's currently an Image!)
        Transform vowelsTr = searchRoot.Find("Vowels");
        if (vowelsTr == null) vowelsTr = searchRoot.Find("Vowel");
        if (vowelsTr == null)
        {
            foreach (Transform child in searchRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.ToLower().Contains("vowel") && !child.name.ToLower().Contains("content") && !child.name.ToLower().Contains("matrix"))
                {
                    vowelsTr = child;
                    break;
                }
            }
        }

        Transform consonantsTr = searchRoot.Find("Consonants");
        if (consonantsTr == null) consonantsTr = searchRoot.Find("Consonant");
        if (consonantsTr == null)
        {
            foreach (Transform child in searchRoot.GetComponentsInChildren<Transform>(true))
            {
                if (child.name.ToLower().Contains("consonant") && !child.name.ToLower().Contains("content") && !child.name.ToLower().Contains("matrix"))
                {
                    consonantsTr = child;
                    break;
                }
            }
        }

        if (vowelsTr != null)
        {
            vowelsTabBtn = vowelsTr.GetComponent<Button>();
            if (vowelsTabBtn == null) vowelsTabBtn = vowelsTr.gameObject.AddComponent<Button>();
            vowelsTabBtn.onClick.RemoveAllListeners();
            vowelsTabBtn.onClick.AddListener(() => SwitchTab(true));
        }

        if (consonantsTr != null)
        {
            consonantsTabBtn = consonantsTr.GetComponent<Button>();
            if (consonantsTabBtn == null) consonantsTabBtn = consonantsTr.gameObject.AddComponent<Button>();
            consonantsTabBtn.onClick.RemoveAllListeners();
            consonantsTabBtn.onClick.AddListener(() => SwitchTab(false));
        }

        if (soundTiles == null || soundTiles.Length == 0)
        {
            soundTiles = Resources.LoadAll<SD_SoundTileData_Phonics_Junior>("Unit1_SD_Data");
            if (soundTiles == null || soundTiles.Length == 0)
                soundTiles = Resources.LoadAll<SD_SoundTileData_Phonics_Junior>("");

#if UNITY_EDITOR
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:SD_SoundTileData_Phonics_Junior", new[] { "Assets/Data/unit 1" });
        if (guids.Length == 0)
            guids = UnityEditor.AssetDatabase.FindAssets("t:SD_SoundTileData_Phonics_Junior", new[] { "Assets/Data/Unit1_SD_Data" });
        if (guids.Length == 0)
            guids = UnityEditor.AssetDatabase.FindAssets("t:SD_SoundTileData_Phonics_Junior");

        List<SD_SoundTileData_Phonics_Junior> list = new List<SD_SoundTileData_Phonics_Junior>();
        foreach (string guid in guids)
        {
            SD_SoundTileData_Phonics_Junior asset = UnityEditor.AssetDatabase.LoadAssetAtPath<SD_SoundTileData_Phonics_Junior>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
            if (asset != null) list.Add(asset);
        }
        soundTiles = list.ToArray();
#endif
        }
    }

    private void HideSectionSelectionPanels()
    {
        Unit_Selection_Panel_Phonics_Junior unitSel = FindFirstObjectByType<Unit_Selection_Panel_Phonics_Junior>(FindObjectsInactive.Include);
        if (unitSel != null)
        {
            unitSel.HideSelectionPanels();
        }

        GameObject panels = GameObject.Find("Unit_1_Section_Selection_Panels");
        if (panels != null)
        {
            panels.SetActive(false);
        }
    }

    public void OpenSoundWall()
    {
        EnsureInit();
        HideSectionSelectionPanels();

        if (sectionDPanel != null)
        {
            Transform curr = sectionDPanel.transform;
            while (curr != null && curr.gameObject.name != "Canvas")
            {
                if (!curr.gameObject.activeSelf)
                {
                    curr.gameObject.SetActive(true);
                }
                curr = curr.parent;
            }
            sectionDPanel.SetActive(true);
        }

        if (backButtonObj != null)
        {
            backButtonObj.SetActive(true);
            backButtonObj.transform.SetAsLastSibling();
        }

        if (tableContent != null)
        {
            RectTransform cRt = tableContent.GetComponent<RectTransform>();
            if (cRt != null)
            {
                cRt.anchorMin = new Vector2(0.5f, 0.5f);
                cRt.anchorMax = new Vector2(0.5f, 0.5f);
                cRt.pivot = new Vector2(0.5f, 0.5f);
                cRt.anchoredPosition = new Vector2(0f, -20f);
                cRt.localScale = Vector3.one;
            }
        }

        visitedCount = 0;

        int childTileCount = 0;
        if (tableContent != null) childTileCount += tableContent.childCount;

        if (!tilesCreated || childTileCount == 0)
        {
            CreatePage9Table();
            tilesCreated = true;
        }

        if (tableContent != null)
        {
            foreach (SD_SoundTile_Phonics_Junior tile in tableContent.GetComponentsInChildren<SD_SoundTile_Phonics_Junior>(true))
            {
                if (tile != null) tile.ResetTile();
            }
        }

        // Hide table initially while instruction plays
        HideTableContent();

        if (instructionCoroutine != null)
        {
            StopCoroutine(instructionCoroutine);
            instructionCoroutine = null;
        }

        if (gameObject.activeInHierarchy)
        {
            instructionCoroutine = StartCoroutine(ShowInstruction());
        }
        else
        {
            ShowTableContent();
        }
    }

    private void HideTableContent()
    {
        if (topBar != null) topBar.SetActive(false);
        if (headingsParent != null) headingsParent.SetActive(false);

        if (tableContent == null) return;

        Transform scrollParent = tableContent.parent;
        while (scrollParent != null && scrollParent.name != "Scroll View" && scrollParent.name != "ScrollView" && scrollParent != transform)
        {
            scrollParent = scrollParent.parent;
        }
        if (scrollParent != null && (scrollParent.name == "Scroll View" || scrollParent.name == "ScrollView"))
        {
            scrollParent.gameObject.SetActive(false);
        }
        else
        {
            tableContent.gameObject.SetActive(false);
        }
    }

    private void ShowTableContent()
    {
        if (topBar != null) topBar.SetActive(true);
        if (headingsParent != null) headingsParent.SetActive(true);

        if (tableContent == null) return;

        Transform scrollParent = tableContent.parent;
        while (scrollParent != null && scrollParent.name != "Scroll View" && scrollParent.name != "ScrollView" && scrollParent != transform)
        {
            scrollParent = scrollParent.parent;
        }
        if (scrollParent != null && (scrollParent.name == "Scroll View" || scrollParent.name == "ScrollView"))
        {
            scrollParent.gameObject.SetActive(true);
        }
        tableContent.gameObject.SetActive(true);
        SwitchTab(isVowelsTabActive);
    }

    private void ClearExistingTiles()
    {
        if (tableContent == null) return;

        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in tableContent)
        {
            if (child != null) children.Add(child.gameObject);
        }
        for (int i = children.Count - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
                Destroy(children[i]);
            else
                DestroyImmediate(children[i]);
        }
    }

    private SD_SoundTile_Phonics_Junior CreateProceduralCell(Transform parent, SD_SoundTileData_Phonics_Junior tileData)
    {
        int targetLayer = parent != null ? parent.gameObject.layer : LayerMask.NameToLayer("UI");

        GameObject tileObj = new GameObject("Table_Cell", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tileObj.layer = targetLayer;
        tileObj.transform.SetParent(parent, false);

        RectTransform rt = tileObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160f, 140f);
        rt.localScale = Vector3.one;
        rt.localPosition = Vector3.zero;

        Image bgImage = tileObj.GetComponent<Image>();
        bgImage.color = DefaultCellColor;

#if UNITY_EDITOR
        if (tileData != null && tileData.image == null && !string.IsNullOrEmpty(tileData.keyword))
        {
            string key = tileData.keyword.ToLower().Trim();
            string[] secDGuids = UnityEditor.AssetDatabase.FindAssets("U1_SEC_D t:Texture2D");
            foreach (string guid in secDGuids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                UnityEngine.Object[] sheetAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                if (sheetAssets != null)
                {
                    foreach (UnityEngine.Object obj in sheetAssets)
                    {
                        if (obj is Sprite sp && sp.name.ToLower().Trim() == key)
                        {
                            tileData.image = sp;
                            UnityEditor.EditorUtility.SetDirty(tileData);
                            break;
                        }
                    }
                }
                if (tileData.image != null) break;
            }
        }
#endif

        if (tileData != null && tileData.image != null)
        {
            bgImage.sprite = tileData.image;
            bgImage.color = Color.white;
            bgImage.preserveAspect = false;
            bgImage.type = Image.Type.Simple;
        }

        Button btn = tileObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.selectedColor = Color.white;
        btn.colors = colors;

        // Text overlays (only visible if image is null as fallback)
        GameObject graphemeObj = new GameObject("GraphemeText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        graphemeObj.layer = targetLayer;
        graphemeObj.transform.SetParent(tileObj.transform, false);

        RectTransform gRt = graphemeObj.GetComponent<RectTransform>();
        gRt.anchorMin = new Vector2(0f, 0.45f);
        gRt.anchorMax = new Vector2(1f, 1f);
        gRt.offsetMin = new Vector2(2f, 0f);
        gRt.offsetMax = new Vector2(-2f, 0f);
        gRt.localScale = Vector3.one;
        gRt.localPosition = Vector3.zero;

        TextMeshProUGUI gTmp = graphemeObj.GetComponent<TextMeshProUGUI>();
        gTmp.alignment = TextAlignmentOptions.Center;
        gTmp.color = new Color(0.05f, 0.18f, 0.55f, 1f);
        gTmp.fontSize = 28f;
        gTmp.enableAutoSizing = false;
        gTmp.raycastTarget = false;

        GameObject keywordObj = new GameObject("KeywordText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        keywordObj.layer = targetLayer;
        keywordObj.transform.SetParent(tileObj.transform, false);

        RectTransform kRt = keywordObj.GetComponent<RectTransform>();
        kRt.anchorMin = new Vector2(0f, 0f);
        kRt.anchorMax = new Vector2(1f, 0.45f);
        kRt.offsetMin = new Vector2(2f, 2f);
        kRt.offsetMax = new Vector2(-2f, 0f);
        kRt.localScale = Vector3.one;
        kRt.localPosition = Vector3.zero;

        TextMeshProUGUI kTmp = keywordObj.GetComponent<TextMeshProUGUI>();
        kTmp.alignment = TextAlignmentOptions.Center;
        kTmp.color = new Color(0.20f, 0.20f, 0.25f, 1f);
        kTmp.fontSize = 16f;
        kTmp.enableAutoSizing = false;
        kTmp.raycastTarget = false;

        if (tileData != null && tileData.image != null)
        {
            graphemeObj.SetActive(false);
            keywordObj.SetActive(false);
        }

        SD_SoundTile_Phonics_Junior tileComp = tileObj.AddComponent<SD_SoundTile_Phonics_Junior>();
        tileComp.SetUIElements(gTmp, kTmp, bgImage, null);

        return tileComp;
    }

    private GameObject CreateBlankCell(Transform parent, Color bgCol)
    {
        int targetLayer = parent != null ? parent.gameObject.layer : LayerMask.NameToLayer("UI");

        GameObject blankObj = new GameObject("Blank_Space", typeof(RectTransform));
        blankObj.layer = targetLayer;
        blankObj.transform.SetParent(parent, false);

        RectTransform rt = blankObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(195f, 185f);
        rt.localScale = Vector3.one;
        rt.localPosition = Vector3.zero;

        return blankObj;
    }

    private Coroutine tabPulseCoroutine;

    private void StartTabPulse()
    {
        if (tabPulseCoroutine != null) StopCoroutine(tabPulseCoroutine);
        if (gameObject.activeInHierarchy)
        {
            tabPulseCoroutine = StartCoroutine(IdlePulseTabsRoutine());
        }
    }

    private IEnumerator IdlePulseTabsRoutine()
    {
        while (true)
        {
            float t = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f; // Smooth 0..1 sine pulse
            float inactiveScale = Mathf.Lerp(0.92f, 1.05f, t);

            if (vowelsTabBtn != null)
            {
                if (isVowelsTabActive)
                {
                    vowelsTabBtn.transform.localScale = Vector3.one * 1.15f;
                }
                else
                {
                    vowelsTabBtn.transform.localScale = Vector3.one * inactiveScale;
                }
            }

            if (consonantsTabBtn != null)
            {
                if (!isVowelsTabActive)
                {
                    consonantsTabBtn.transform.localScale = Vector3.one * 1.15f;
                }
                else
                {
                    consonantsTabBtn.transform.localScale = Vector3.one * inactiveScale;
                }
            }

            yield return null;
        }
    }

    public void SwitchTab(bool showVowels)
    {
        isVowelsTabActive = showVowels;

        foreach (var obj in vowelCellObjects)
        {
            if (obj != null) obj.SetActive(showVowels);
        }

        foreach (var obj in consonantCellObjects)
        {
            if (obj != null) obj.SetActive(!showVowels);
        }

        // ALWAYS keep headings, top bar, and back button fully active & intact on BOTH tabs!
        if (topBar != null) topBar.SetActive(true);
        if (headingsParent != null) headingsParent.SetActive(true);

        if (backButtonObj != null && gameObject.activeInHierarchy)
        {
            backButtonObj.SetActive(true);
            try { backButtonObj.transform.SetAsLastSibling(); } catch (System.Exception) { }
        }

        if (tableContent != null)
        {
            GridLayoutGroup grid = tableContent.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.padding = new RectOffset(5, 5, 5, 5);
                grid.cellSize = new Vector2(195f, 185f);
                grid.spacing = new Vector2(10f, 10f);
            }
        }

        if (vowelsTabBtn != null)
        {
            Image img = vowelsTabBtn.GetComponent<Image>();
            if (img != null) img.color = showVowels ? Color.white : new Color(0.70f, 0.70f, 0.75f, 0.85f);
        }

        if (consonantsTabBtn != null)
        {
            Image img = consonantsTabBtn.GetComponent<Image>();
            if (img != null) img.color = !showVowels ? Color.white : new Color(0.70f, 0.70f, 0.75f, 0.85f);
        }

        Transform targetTr = showVowels ? (vowelsTabBtn != null ? vowelsTabBtn.transform : null) : (consonantsTabBtn != null ? consonantsTabBtn.transform : null);
        if (targetTr != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(TabPopAnimation(targetTr));
        }

        StartTabPulse();
    }

    private IEnumerator TabPopAnimation(Transform tr)
    {
        float timer = 0f;
        float duration = 0.2f;
        Vector3 startScale = Vector3.one * 1.30f;
        Vector3 endScale = Vector3.one * 1.15f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            if (tr != null)
            {
                tr.localScale = Vector3.Lerp(startScale, endScale, t);
            }
            yield return null;
        }

        if (tr != null) tr.localScale = endScale;
    }

    private void CreatePage9Table()
    {
        if (tableContent == null) return;

        ClearExistingTiles();
        vowelCellObjects.Clear();
        consonantCellObjects.Clear();

        // Build 8-Column Grid Layout on tableContent
        GridLayoutGroup grid = tableContent.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = tableContent.gameObject.AddComponent<GridLayoutGroup>();

        grid.padding = new RectOffset(5, 5, 5, 5);
        grid.cellSize = new Vector2(195f, 185f);
        grid.spacing = new Vector2(10f, 10f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperLeft;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;

        ContentSizeFitter csf = tableContent.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = tableContent.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Map soundTiles by keyword for audio and sprite matching
        Dictionary<string, SD_SoundTileData_Phonics_Junior> tileMap = new Dictionary<string, SD_SoundTileData_Phonics_Junior>();
        if (soundTiles != null)
        {
            foreach (var t in soundTiles)
            {
                if (t != null && !string.IsNullOrEmpty(t.keyword))
                {
                    tileMap[t.keyword.ToLower().Trim()] = t;
                }
            }
        }

        // --- Exact Page 9 Table Matrix (8 Columns x 6 Rows) ---
        Page9CellDef?[,] tableMatrix = new Page9CellDef?[6, 8]
        {
            // Row 0 (Vowels 1)
            { new Page9CellDef("i:", "sheep"), new Page9CellDef("ɪ", "ship"), new Page9CellDef("ʊ", "good"), new Page9CellDef("u:", "shoot"), new Page9CellDef("ɪə", "here"), new Page9CellDef("eɪ", "wait"), null, null },
            // Row 1 (Vowels 2)
            { new Page9CellDef("e", "bed"), new Page9CellDef("ə", "teacher"), new Page9CellDef("3:", "bird"), new Page9CellDef("ɔ:", "door"), new Page9CellDef("ʊə", "tourist"), new Page9CellDef("ɔɪ", "boy"), new Page9CellDef("əʊ", "show"), null },
            // Row 2 (Vowels 3)
            { new Page9CellDef("æ", "cat"), new Page9CellDef("ʌ", "up"), new Page9CellDef("ɑ:", "far"), new Page9CellDef("ɒ", "on"), new Page9CellDef("eə", "hair"), new Page9CellDef("aɪ", "my"), new Page9CellDef("aʊ", "cow"), null },
            // Row 3 (Consonants 1)
            { new Page9CellDef("p", "pea"), new Page9CellDef("b", "boat"), new Page9CellDef("t", "tea"), new Page9CellDef("d", "dog"), new Page9CellDef("tʃ", "cheese"), new Page9CellDef("dʒ", "june"), new Page9CellDef("k", "car"), new Page9CellDef("g", "go") },
            // Row 4 (Consonants 2)
            { new Page9CellDef("f", "fly"), new Page9CellDef("v", "video"), new Page9CellDef("θ", "think"), new Page9CellDef("ð", "this"), new Page9CellDef("s", "see"), new Page9CellDef("z", "zoo"), new Page9CellDef("ʃ", "shall"), new Page9CellDef("ʒ", "television") },
            // Row 5 (Consonants 3)
            { new Page9CellDef("m", "man"), new Page9CellDef("n", "now"), new Page9CellDef("ŋ", "sing"), new Page9CellDef("h", "hat"), new Page9CellDef("l", "love"), new Page9CellDef("r", "red"), new Page9CellDef("w", "wet"), new Page9CellDef("j", "yes") }
        };

        for (int r = 0; r < 6; r++)
        {
            for (int c = 0; c < 8; c++)
            {
                Page9CellDef? cellDef = tableMatrix[r, c];
                GameObject cellObj = null;

                Color sectionBgColor = Color.white;
                if (r < 3)
                {
                    if (c <= 3) sectionBgColor = Color.white; // Monophthongs (White)
                    else sectionBgColor = new Color(0.90f, 0.91f, 0.93f, 1f); // Diphthongs (Soft Gray)
                }
                else
                {
                    sectionBgColor = new Color(0.99f, 0.96f, 0.70f, 1f); // Consonants (Soft Yellow)
                }

                if (!cellDef.HasValue)
                {
                    cellObj = CreateBlankCell(tableContent, sectionBgColor);
                }
                else
                {
                    Page9CellDef def = cellDef.Value;
                    tileMap.TryGetValue(def.keyword.ToLower().Trim(), out SD_SoundTileData_Phonics_Junior data);

                    if (data == null)
                    {
                        data = ScriptableObject.CreateInstance<SD_SoundTileData_Phonics_Junior>();
                        data.grapheme = def.symbol;
                        data.keyword = def.keyword;
                    }
                    else if (string.IsNullOrEmpty(data.grapheme))
                    {
                        data.grapheme = def.symbol;
                    }

                    SD_SoundTile_Phonics_Junior tile = null;
                    if (soundTilePrefab != null)
                    {
                        GameObject tileObj = Instantiate(soundTilePrefab, tableContent);
                        tile = tileObj.GetComponent<SD_SoundTile_Phonics_Junior>();
                        if (tile == null) tile = tileObj.AddComponent<SD_SoundTile_Phonics_Junior>();
                    }
                    else
                    {
                        tile = CreateProceduralCell(tableContent, data);
                    }

                    if (tile != null)
                    {
                        tile.Initialize(data, audioSource);

                        Image tileBg = tile.GetComponent<Image>();
                        if (tileBg != null && (data == null || data.image == null))
                        {
                            tileBg.color = sectionBgColor;
                        }
                    }

                    Button button = tile.GetComponent<Button>();
                    if (button != null)
                    {
                        SD_SoundTile_Phonics_Junior currentTile = tile;
                        button.onClick.RemoveAllListeners();
                        button.onClick.AddListener(() =>
                        {
                            if (UnityEngine.EventSystems.EventSystem.current != null)
                            {
                                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
                            }

                            if (instructionCoroutine != null)
                            {
                                StopCoroutine(instructionCoroutine);
                                instructionCoroutine = null;

                                if (audioSource != null) audioSource.Stop();

                                if (instructionPanel != null) instructionPanel.SetActive(false);
                                if (instructionCanvas != null) instructionCanvas.alpha = 1f;
                                if (instructionRect != null) instructionRect.localScale = Vector3.one;

                                ShowTableContent();
                                isAudioPlaying = false;
                            }

                            if (isAudioPlaying)
                                return;

                            StartCoroutine(PlayTile(currentTile));
                        });
                    }

                    if (tile != null) cellObj = tile.gameObject;
                }

                if (cellObj != null)
                {
                    if (r < 3) vowelCellObjects.Add(cellObj);
                    else consonantCellObjects.Add(cellObj);
                }
            }
        }

        SwitchTab(isVowelsTabActive);
    }

    private IEnumerator PlayTile(SD_SoundTile_Phonics_Junior tile)
    {
        isAudioPlaying = true;

        bool firstVisit = tile.MarkVisited();

        if (firstVisit)
        {
            visitedCount++;

            if (visitedCount >= soundTiles.Length)
            {
                if (nextButton != null)
                {
                    nextButton.gameObject.SetActive(true);
                    nextButton.interactable = true;
                }
                if (unitCompletionPanel != null)
                {
                    unitCompletionPanel.SetActive(true);
                }
                if (backButtonObj != null)
                {
                    backButtonObj.SetActive(true);
                    backButtonObj.transform.SetAsLastSibling();
                }
            }
        }

        tile.PlaySound();

        yield return new WaitForSeconds(tile.GetTotalDuration());

        isAudioPlaying = false;
    }

    private IEnumerator ShowInstruction()
    {
        isAudioPlaying = true;
        if (instructionPanel != null) instructionPanel.SetActive(true);

        if (instructionCanvas != null) instructionCanvas.alpha = 1f;
        if (instructionRect != null) instructionRect.localScale = Vector3.one;

        if (instructionText != null)
        {
            instructionText.text = "";
            string message = "Tap on sound tiles to play the sound";

            foreach (char letter in message)
            {
                instructionText.text += letter;
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        if (audioSource != null)
        {
            audioSource.enabled = true;
            audioSource.mute = false;
            audioSource.volume = 1f;
        }

        if (instructionAudio != null && audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = instructionAudio;
            audioSource.Play();
            yield return new WaitForSeconds(instructionAudio.length);
        }
        else
        {
            yield return new WaitForSeconds(1.5f);
        }

        if (instructionCanvas != null && instructionRect != null)
        {
            Vector3 startScale = instructionRect.localScale;
            Vector3 endScale = Vector3.zero;

            float timer = 0f;
            float duration = 0.4f;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;

                instructionCanvas.alpha = Mathf.Lerp(1f, 0f, t);
                instructionRect.localScale = Vector3.Lerp(startScale, endScale, t);

                yield return null;
            }

            instructionCanvas.alpha = 0f;
            instructionRect.localScale = endScale;
        }

        if (instructionPanel != null)
            instructionPanel.SetActive(false);

        ShowTableContent();
        isAudioPlaying = false;
    }

    public void StopSection()
    {
        StopAllCoroutines();

        instructionCoroutine = null;
        isAudioPlaying = false;
        visitedCount = 0;

        if (audioSource != null)
            audioSource.Stop();

        if (instructionPanel != null)
            instructionPanel.SetActive(false);

        ShowTableContent();

        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(false);
            nextButton.interactable = false;
        }
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        instructionCoroutine = null;
        isAudioPlaying = false;

        if (audioSource != null)
            audioSource.Stop();

        if (instructionPanel != null)
            instructionPanel.SetActive(false);
    }

    public void OnNextButton()
    {
        StopSection();

        if (sectionDPanel != null) sectionDPanel.SetActive(false);

        U1_RewardController reward = FindFirstObjectByType<U1_RewardController>(FindObjectsInactive.Include);
        if (reward != null)
        {
            reward.gameObject.SetActive(true);
            reward.ShowReward();
        }
        else if (sectionSelectionPanel != null)
        {
            sectionSelectionPanel.SetActive(true);
        }
    }
}