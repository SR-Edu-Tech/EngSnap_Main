using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class U6_A1_MeetTeamsController : MonoBehaviour
{
    [Header("UI Containers")]
    public Transform column1Container; // Main single column container (Col_1)
    public TextMeshProUGUI column1Header;

    public AudioSource audioSource;
    public U6_Manager manager;

    [Header("Word Row Prefab")]
    public GameObject wordRowPrefab; // Single word row item (Text + Picture)

    [Header("Magic E Animation Settings")]
    public GameObject magicEEffectPrefab; // Optional glowing particle / curve effect

    [Header("Table Navigation")]
    public Button tablePrevButton;
    public Button tableNextButton;

    [Header("Header Tab Pills (Assign in Inspector)")]
    public GameObject tab1PillObject; // Tab 1 (a_e / ee)
    public GameObject tab2PillObject; // Tab 2 (ai / ea)
    public GameObject tab3PillObject; // Tab 3 (ay / ey)

    [Header("Card Sizing & Layout (Inspector Customization)")]
    public Vector2 gridCellSize = new Vector2(210f, 200f);
    public Vector2 gridSpacing = new Vector2(25f, 25f);
    public float cardPrefabScale = 1.30f;
    public int gridColumnCount = 4;

    private U6_LevelData currentLevel;
    private int activeTableIndex = 0;

    private void Awake()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void EnsureContainersAssigned()
    {
        if (column1Container == null)
        {
            Transform colContainer = transform.Find("Columns container");
            if (colContainer == null) colContainer = transform.Find("Columns_container");
            if (colContainer != null && colContainer.childCount > 0)
            {
                Transform mainCol = colContainer.GetChild(0);
                if (mainCol != null)
                {
                    Transform inner = mainCol.Find("Column_1");
                    if (inner == null) inner = mainCol.Find("Column 1");
                    if (inner == null) inner = mainCol.Find("Content");
                    column1Container = inner != null ? inner : mainCol;
                }
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureContainersAssigned();
        if (column1Container != null && !Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
        }

        if (gridCellSize.x <= 0) gridCellSize.x = 210f;
        if (gridCellSize.y <= 0) gridCellSize.y = 200f;
        if (cardPrefabScale <= 0) cardPrefabScale = 1.30f;
        if (gridColumnCount <= 0) gridColumnCount = 4;

        Transform container = column1Container;
        if (container != null)
        {
            GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
            if (grid != null)
            {
                grid.cellSize = gridCellSize;
                grid.spacing = gridSpacing;
                grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                grid.constraintCount = gridColumnCount;
                UnityEditor.EditorUtility.SetDirty(grid);
            }

            foreach (Transform child in container)
            {
                if (child != null)
                {
                    child.localScale = Vector3.one * cardPrefabScale;
                }
            }
        }
    }
#endif

    private List<Transform> tabObjectsCache = new List<Transform>();

    private bool tabsInitialized = false;

    public void SetupActivity(U6_LevelData levelData)
    {
        currentLevel = levelData;
        if (manager == null) manager = FindFirstObjectByType<U6_Manager>(FindObjectsInactive.Include);
        if (currentLevel == null && manager != null) currentLevel = manager.levelLongA;
        if (currentLevel == null)
        {
            currentLevel = Resources.Load<U6_LevelData>("Level_Long_A_teams");
#if UNITY_EDITOR
            if (currentLevel == null) currentLevel = UnityEditor.AssetDatabase.LoadAssetAtPath<U6_LevelData>("Assets/Data/Unit6/Levels/Level_Long_A_teams.asset");
#endif
        }

        if (currentLevel == null || currentLevel.teams == null || currentLevel.teams.Count == 0) return;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        // Ensure column1Container is assigned
        EnsureContainersAssigned();

        tabsInitialized = false;
        SetupHeaderTabs();

        ShowTable(0);
    }

    public void OnTabClicked(int tabIndex)
    {
        Debug.Log($"[Activity 1] OnTabClicked called for Tab {tabIndex}!");
        ShowTable(tabIndex);
    }

    public void ShowTable(int tableIndex)
    {
        EnsureContainersAssigned();
        if (manager == null) manager = FindFirstObjectByType<U6_Manager>(FindObjectsInactive.Include);

        // Resolve currentLevel from manager if not already set
        if (currentLevel == null && manager != null)
            currentLevel = manager.activeLevel != null ? manager.activeLevel : manager.levelLongA;

        // Last resort: try loading from Resources or AssetDatabase
        if (currentLevel == null)
        {
            currentLevel = Resources.Load<U6_LevelData>("Level_Long_A_teams");
#if UNITY_EDITOR
            if (currentLevel == null)
                currentLevel = UnityEditor.AssetDatabase.LoadAssetAtPath<U6_LevelData>("Assets/Data/Unit6/Levels/Level_Long_A_teams.asset");
#endif
        }

        if (currentLevel == null || currentLevel.teams == null || currentLevel.teams.Count == 0)
        {
            Debug.LogError("[Activity 1] Cannot switch table! currentLevel is null or has 0 teams.");
            return;
        }


        activeTableIndex = Mathf.Clamp(tableIndex, 0, currentLevel.teams.Count - 1);
        U6_LongVowelTeamData activeTeam = currentLevel.teams[activeTableIndex];
        if (activeTeam == null) return;

        string levelTitle = currentLevel.levelTitle;
        string headerSpelling = GetExpectedHeaderSpelling(levelTitle, activeTableIndex, activeTeam.teamSpelling);

        Debug.Log($"<color=yellow>[Activity 1] Switching to Table {activeTableIndex} ({headerSpelling}) - Spawning {activeTeam.teamWords.Count} cards...</color>");

        // Ensure main container board is active
        Transform col1 = GetTableParent(column1Container);
        if (col1 != null) col1.gameObject.SetActive(true);

        // Update header text & audio for active team
        SetupHeader(column1Header, column1Container, activeTeam, headerSpelling);

        // Populate cards for active team dynamically into single main container
        PopulateColumn(column1Container, activeTeam, headerSpelling);

        UpdateTabVisualStates(activeTableIndex);

        // Hide smaller side arrows for clean Method 1 tab navigation
        if (tablePrevButton != null) tablePrevButton.gameObject.SetActive(false);
        if (tableNextButton != null) tableNextButton.gameObject.SetActive(false);

        // Main Bigger Next Button: active ONLY when on the last table (Table 3)!
        if (manager != null)
        {
            manager.SetNextButtonState(activeTableIndex == currentLevel.teams.Count - 1);
        }
    }

    private void SetupHeaderTabs()
    {
        if (tabsInitialized) return;

        tabObjectsCache.Clear();
        if (tab1PillObject != null) tabObjectsCache.Add(tab1PillObject.transform);
        if (tab2PillObject != null) tabObjectsCache.Add(tab2PillObject.transform);
        if (tab3PillObject != null) tabObjectsCache.Add(tab3PillObject.transform);

        // Auto-find by name if Inspector fields not assigned
        if (tabObjectsCache.Count == 0)
        {
            string[] tabNames = { "heading a_e", "heading ai", "heading ay" };
            foreach (string tName in tabNames)
            {
                // Search current activity panel first, then all scene objects
                Transform found = FindChildByName(transform, tName);
                if (found == null && transform.parent != null) found = FindChildByName(transform.parent, tName);
                if (found == null)
                {
                    // Last resort: search all root GameObjects in scene
                    foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                    {
                        found = FindChildByName(root.transform, tName);
                        if (found != null) break;
                    }
                }
                if (found != null)
                {
                    tabObjectsCache.Add(found);
                    Debug.Log($"[Activity 1] Auto-found tab by name: '{tName}' at {found.gameObject.scene.name}/{GetTransformPath(found)}");
                }
                else
                {
                    Debug.LogWarning($"[Activity 1] Could not find tab GameObject named '{tName}' in scene!");
                }
            }
        }

        Debug.Log($"[Activity 1] SetupHeaderTabs: wiring {tabObjectsCache.Count} tabs...");

        for (int i = 0; i < tabObjectsCache.Count; i++)
        {
            int capturedIndex = i; // CRITICAL: capture by value for lambda closure
            Transform tab = tabObjectsCache[i];
            if (tab == null) continue;

            tab.gameObject.SetActive(true);

            // --- Ensure Image exists & raycasts ---
            Image tabImg = tab.GetComponent<Image>();
            if (tabImg == null)
            {
                tabImg = tab.gameObject.AddComponent<Image>();
                tabImg.color = new Color(1, 1, 1, 0.01f); // near-transparent so it's invisible but clickable
            }
            tabImg.raycastTarget = true;

            // --- Disable raycast on child TEXT so text doesn't block button ---
            foreach (var g in tab.GetComponentsInChildren<Graphic>(true))
            {
                if (g is TextMeshProUGUI || g is Text)
                    g.raycastTarget = false;
            }

            // --- Wire Button.onClick ---
            Button btn = tab.GetComponent<Button>();
            if (btn == null)
            {
                btn = tab.gameObject.AddComponent<Button>();
                Debug.Log($"[Activity 1] Added Button component to tab {capturedIndex} ('{tab.name}')");
            }
            if (btn.targetGraphic == null) btn.targetGraphic = tabImg;
            btn.interactable = true;
            btn.transition = Selectable.Transition.ColorTint;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"<color=cyan>[Activity 1] Tab Button.onClick fired: index {capturedIndex} ('{tab.name}')</color>");
                ShowTable(capturedIndex);
            });

            // --- Also wire IPointerClickHandler as belt-and-suspenders ---
            U6_TabButton tabHandler = tab.GetComponent<U6_TabButton>();
            if (tabHandler == null) tabHandler = tab.gameObject.AddComponent<U6_TabButton>();
            tabHandler.tabIndex = capturedIndex;
            tabHandler.controller = this;

            Debug.Log($"[Activity 1] Tab {capturedIndex} wired: '{tab.name}' — Button ok, U6_TabButton ok");

            // --- CRITICAL FIX: Move tab to last sibling so it renders ON TOP of Columns container ---
            // In Unity UI, later siblings = drawn last = rendered on top = receive raycasts first.
            // heading a_e/ai/ay were BEFORE Columns container, so Columns container's Image was
            // intercepting all clicks! Moving tabs to last sibling fixes this permanently.
            tab.SetAsLastSibling();

            // --- Add Canvas override + GraphicRaycaster so this tab's clicks are handled independently ---
            Canvas tabCanvas = tab.GetComponent<Canvas>();
            if (tabCanvas == null) tabCanvas = tab.gameObject.AddComponent<Canvas>();
            tabCanvas.overrideSorting = true;
            tabCanvas.sortingOrder = 10; // Above panel content (default 0)

            UnityEngine.UI.GraphicRaycaster tabRaycaster = tab.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (tabRaycaster == null) tab.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        tabsInitialized = true; // Only set TRUE after successful wiring!
    }


    private void UpdateTabVisualStates(int selectedIndex)
    {
        for (int i = 0; i < tabObjectsCache.Count; i++)
        {
            Transform tab = tabObjectsCache[i];
            if (tab == null) continue;

            bool isSelected = (i == selectedIndex);

            // Scale selected tab up (1.15x) and keep unselected clean (0.95x) - NO PULSE!
            tab.localScale = isSelected ? Vector3.one * 1.15f : Vector3.one * 0.95f;

            Image img = tab.GetComponent<Image>();
            if (img != null)
            {
                img.color = isSelected ? Color.white : new Color(0.80f, 0.80f, 0.80f, 0.9f);
            }

            TextMeshProUGUI tmp = tab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.color = isSelected ? new Color(1f, 0.85f, 0f, 1f) : Color.white;
                tmp.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
            }
        }
    }

    private void AddTabParent(Transform t, List<Transform> list)
    {
        if (t == null || list == null) return;
        Transform parentObj = (t.parent != null && t.parent.name.ToLower().Contains("gameobject")) ? t.parent : t;
        if (!list.Contains(parentObj)) list.Add(parentObj);
    }

    /// <summary>Recursively searches all children for a Transform with the exact given name.</summary>
    private Transform FindChildByName(Transform root, string targetName)
    {
        if (root == null) return null;
        if (root.name == targetName) return root;
        foreach (Transform child in root)
        {
            Transform found = FindChildByName(child, targetName);
            if (found != null) return found;
        }
        return null;
    }

    private string GetTransformPath(Transform t)
    {
        if (t == null) return "null";
        string path = t.name;
        Transform p = t.parent;
        while (p != null) { path = p.name + "/" + path; p = p.parent; }
        return path;
    }

    private IEnumerator AnimateTabPop(GameObject tabObj)
    {
        if (tabObj == null) yield break;
        Vector3 orig = Vector3.one;
        float elapsed = 0f;
        float duration = 0.25f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = 1f + 0.15f * Mathf.Sin((elapsed / duration) * Mathf.PI);
            tabObj.transform.localScale = orig * scale;
            yield return null;
        }
        tabObj.transform.localScale = orig;
    }

    private Transform GetTableParent(Transform rawContainer)
    {
        if (rawContainer == null) return null;
        if (rawContainer.parent != null && rawContainer.parent.name.ToLower().Contains("column"))
        {
            return rawContainer.parent;
        }
        return rawContainer;
    }

    public void OnPrevTableClicked()
    {
        if (activeTableIndex > 0)
        {
            ShowTable(activeTableIndex - 1);
        }
    }

    public void OnNextTableClicked()
    {
        if (activeTableIndex < 2)
        {
            ShowTable(activeTableIndex + 1);
        }
        else
        {
            // Reached last table (Table 3) -> Advance to Activity 2!
            if (manager != null)
            {
                manager.StartActivity2();
            }
            else
            {
                U6_Manager mgr = FindFirstObjectByType<U6_Manager>(FindObjectsInactive.Include);
                if (mgr != null) mgr.StartActivity2();
            }
        }
    }

    private string GetExpectedHeaderSpelling(string levelTitle, int teamIndex, string fallbackSpelling)
    {
        string title = levelTitle != null ? levelTitle.ToLower().Trim() : "";

        if (title.Contains("long e") || title.Contains("long_e") || title.Contains("e teams") || title.EndsWith(" e") || title.Contains("section e") || title.Contains("vowel e"))
        {
            if (teamIndex == 0) return "ee";
            if (teamIndex == 1) return "ea";
            if (teamIndex == 2) return "ey";
        }
        else if (title.Contains("long a") || title.Contains("long_a") || title.Contains("a teams") || title.EndsWith(" a") || title.Contains("section a") || title.Contains("vowel a"))
        {
            if (teamIndex == 0) return "a_e";
            if (teamIndex == 1) return "ai";
            if (teamIndex == 2) return "ay";
        }
        else if (title.Contains("long i") || title.Contains("long_i") || title.Contains("i teams") || title.EndsWith(" i") || title.Contains("section i") || title.Contains("vowel i"))
        {
            if (teamIndex == 0) return "i_e";
            if (teamIndex == 1) return "ie";
            if (teamIndex == 2) return "igh";
        }
        else if (title.Contains("long o") || title.Contains("long_o") || title.Contains("o teams") || title.EndsWith(" o") || title.Contains("section o") || title.Contains("vowel o"))
        {
            if (teamIndex == 0) return "o_e";
            if (teamIndex == 1) return "oa";
            if (teamIndex == 2) return "ow";
        }
        else if (title.Contains("long u") || title.Contains("long_u") || title.Contains("u teams") || title.EndsWith(" u") || title.Contains("section u") || title.Contains("vowel u"))
        {
            if (teamIndex == 0) return "u_e";
            if (teamIndex == 1) return "ue";
            if (teamIndex == 2) return "ui";
        }

        if (!string.IsNullOrEmpty(fallbackSpelling)) return fallbackSpelling;
        return teamIndex == 0 ? "ee" : (teamIndex == 1 ? "ea" : "ey");
    }

    private void SetupHeader(TextMeshProUGUI headerText, Transform container, U6_LongVowelTeamData teamData, string headerSpelling)
    {
        if (teamData == null) return;
        if (string.IsNullOrEmpty(headerSpelling)) headerSpelling = teamData.teamSpelling;

        if (headerText != null)
        {
            headerText.text = headerSpelling;
        }

        Button headerBtn = null;
        if (headerText != null) headerBtn = headerText.GetComponent<Button>();
        if (headerBtn == null && container != null && container.parent != null)
        {
            Transform t = container.parent.Find("Header");
            if (t != null) headerBtn = t.GetComponent<Button>();
        }

        if (headerBtn != null)
        {
            headerBtn.onClick.RemoveAllListeners();
            headerBtn.onClick.AddListener(() =>
            {
                if (audioSource == null) audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

                if (teamData.spellingAudio != null)
                {
                    audioSource.spatialBlend = 0f;
                    audioSource.volume = 1f;
                    audioSource.mute = false;
                    audioSource.PlayOneShot(teamData.spellingAudio);
                }
                MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
                if (mascot != null) mascot.PlayHiAnimation();
            });
        }
    }

    public void HideNavButtons()
    {
        if (tablePrevButton != null) tablePrevButton.gameObject.SetActive(false);
        if (tableNextButton != null) tableNextButton.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        HideNavButtons();
    }

    private void PopulateColumn(Transform rawContainer, U6_LongVowelTeamData teamData, string headerSpelling)
    {
        if (rawContainer == null || teamData == null) return;
        if (string.IsNullOrEmpty(headerSpelling)) headerSpelling = teamData.teamSpelling;

        Transform container = column1Container != null ? column1Container : rawContainer;

        if (wordRowPrefab == null)
        {
            wordRowPrefab = Resources.Load<GameObject>("U6_WordRowPrefab");
            if (wordRowPrefab == null) wordRowPrefab = Resources.Load<GameObject>("Prefabs/U6_WordRowPrefab");
            if (wordRowPrefab == null) wordRowPrefab = Resources.Load<GameObject>("WordCard");
#if UNITY_EDITOR
            if (wordRowPrefab == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("U6_WordRowPrefab t:Prefab");
                if (guids.Length == 0) guids = UnityEditor.AssetDatabase.FindAssets("WordCard t:Prefab");
                if (guids.Length > 0)
                {
                    wordRowPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
#endif
        }

        // Configure 2-Row Grid Layout inside table container (4 columns x 2 rows)
        HorizontalLayoutGroup hlg = container.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) DestroyImmediate(hlg);
        VerticalLayoutGroup vlg = container.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) DestroyImmediate(vlg);

        GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = container.gameObject.AddComponent<GridLayoutGroup>();

        if (grid != null)
        {
            grid.cellSize = gridCellSize;
            grid.spacing = gridSpacing;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = gridColumnCount > 0 ? gridColumnCount : 4;
        }

        // If wordRowPrefab is provided, clear existing and spawn dynamic word rows matching Page 24/25!
        if (wordRowPrefab != null)
        {
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }

            foreach (var wordData in teamData.teamWords)
            {
                if (wordData == null) continue;

                GameObject item = Instantiate(wordRowPrefab, container);
                float sc = cardPrefabScale > 0f ? cardPrefabScale : 1.30f;
                item.transform.localScale = Vector3.one * sc;
                item.SetActive(true);

                // Set Text on all TMP labels inside prefab
                string formattedWord = HighlightTeamLetters(wordData.word, headerSpelling);
                TextMeshProUGUI[] tmpLabels = item.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmpLabel in tmpLabels)
                {
                    tmpLabel.color = Color.black;
                    tmpLabel.text = formattedWord;
                    tmpLabel.enableAutoSizing = true;
                    tmpLabel.fontSizeMin = 28;
                    tmpLabel.fontSizeMax = 60;
                    tmpLabel.fontStyle = FontStyles.Bold;
                    tmpLabel.raycastTarget = false;
                }

                Text[] uiTexts = item.GetComponentsInChildren<Text>(true);
                foreach (var uiText in uiTexts)
                {
                    uiText.color = Color.black;
                    uiText.text = wordData.word;
                    uiText.raycastTarget = false;
                }

                // Set Image (Check WordPicture, Image, or child Image)
                Image img = null;
                Transform picTrans = item.transform.Find("WordPicture");
                if (picTrans == null) picTrans = item.transform.Find("WordPicture ");
                if (picTrans == null) picTrans = item.transform.Find("Image");

                if (picTrans != null)
                {
                    picTrans.gameObject.SetActive(true);
                    img = picTrans.GetComponent<Image>();
                }
                else
                {
                    Image[] images = item.GetComponentsInChildren<Image>(true);
                    foreach (var i in images)
                    {
                        if (i.gameObject != item)
                        {
                            img = i;
                            break;
                        }
                    }
                }

                if (img != null)
                {
                    img.gameObject.SetActive(true);
                    if (wordData.wordPicture != null)
                    {
                        img.sprite = wordData.wordPicture;
                    }
                    img.raycastTarget = false;
                }

                // Disable raycastTarget ONLY on text components so labels don't block clicks!
                Graphic[] childGraphics = item.GetComponentsInChildren<Graphic>(true);
                foreach (var g in childGraphics)
                {
                    if (g is TextMeshProUGUI || g is Text)
                    {
                        g.raycastTarget = false;
                    }
                }

                // Enable raycastTarget on all Button target graphics and bind OnWordCardClicked!
                Button[] allButtons = item.GetComponentsInChildren<Button>(true);
                foreach (var b in allButtons)
                {
                    if (b.targetGraphic != null)
                    {
                        b.targetGraphic.raycastTarget = true;
                    }
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => OnWordCardClicked(wordData, teamData.teamSpelling, item));
                }

                Button rootBtn = item.GetComponent<Button>();
                if (rootBtn == null) rootBtn = item.AddComponent<Button>();
                Image rootImg = item.GetComponent<Image>();
                if (rootImg != null) rootImg.raycastTarget = true;
                rootBtn.onClick.RemoveAllListeners();
                rootBtn.onClick.AddListener(() => OnWordCardClicked(wordData, teamData.teamSpelling, item));
            }
            return;
        }

        // Fallback: Reuse pre-placed child objects inside container
        for (int i = 0; i < teamData.teamWords.Count; i++)
        {
            CVCWordData wordData = teamData.teamWords[i];
            if (wordData == null) continue;

            Transform item = i < container.childCount ? container.GetChild(i) : null;
            if (item != null)
            {
                item.gameObject.SetActive(true);
                TextMeshProUGUI label = item.GetComponentInChildren<TextMeshProUGUI>();
                Image img = item.GetComponentInChildren<Image>();

                if (label != null)
                {
                    string formattedWord = HighlightTeamLetters(wordData.word, teamData.teamSpelling);
                    label.text = formattedWord;
                }

                if (img != null && wordData.wordPicture != null)
                {
                    img.sprite = wordData.wordPicture;
                    img.gameObject.SetActive(true);
                }

                Button btn = item.GetComponent<Button>();
                if (btn == null) btn = item.gameObject.AddComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnWordCardClicked(wordData, teamData.teamSpelling, item.gameObject));
            }
        }
    }

    private string HighlightTeamLetters(string word, string teamSpelling)
    {
        if (string.IsNullOrEmpty(word)) return "";
        string cleanSpelling = teamSpelling.Replace("_", "").ToLower().Trim();

        if (teamSpelling == "a_e" || teamSpelling == "i_e" || teamSpelling == "o_e" || teamSpelling == "u_e")
        {
            // Magic E format: first vowel & last 'e' highlighted in RED!
            if (word.Length >= 3 && word.EndsWith("e"))
            {
                char firstChar = word[0];
                char vowelChar = word[1];
                string middleChars = word.Substring(1, word.Length - 2);
                return $"{firstChar}<color=#FF0000>{vowelChar}</color>{middleChars.Substring(1)}<color=#FF0000>e</color>";
            }
        }

        if (word.Contains(cleanSpelling))
        {
            return word.Replace(cleanSpelling, $"<color=#FF0000>{cleanSpelling}</color>");
        }

        return word;
    }

    private void OnWordCardClicked(CVCWordData wordData, string teamSpelling, GameObject cardObj)
    {
        if (wordData == null) return;
        Debug.Log($"[Activity 1] Word clicked: '{wordData.word}' (Team: {teamSpelling})");

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.spatialBlend = 0f; // Force 2D
            audioSource.volume = 1f;
            audioSource.mute = false;
        }

        if (wordData.fullWordAudio != null && audioSource != null)
        {
            audioSource.PlayOneShot(wordData.fullWordAudio);
        }
        else if (wordData.fullWordAudio == null)
        {
            Debug.LogWarning($"[Activity 1] fullWordAudio is null on CVCWordData asset for word '{wordData.word}' — please assign it in the Inspector.");
        }

        if (cardObj != null) StartCoroutine(AnimateCard(cardObj));

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null) mascot.PlayHiAnimation();
    }


    private IEnumerator AnimateCard(GameObject cardObj)
    {
        // Smooth scale pulse effect for clicked card!
        Vector3 initialScale = cardObj.transform.localScale;
        float elapsed = 0f;
        float duration = 0.35f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float scale = 1f + 0.15f * Mathf.Sin((elapsed / duration) * Mathf.PI);
            cardObj.transform.localScale = initialScale * scale;
            yield return null;
        }

        cardObj.transform.localScale = initialScale;
    }
}

public class U6_TabButton : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
{
    public int tabIndex;
    public U6_A1_MeetTeamsController controller;

    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        Debug.Log($"<color=green>[Activity 1] Tab Clicked via PointerClick: Index {tabIndex} on '{gameObject.name}'</color>");
        if (controller != null)
        {
            controller.ShowTable(tabIndex);
        }
    }
}
