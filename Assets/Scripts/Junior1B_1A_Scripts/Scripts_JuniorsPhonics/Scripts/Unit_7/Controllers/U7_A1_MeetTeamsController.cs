using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class U7_A1_MeetTeamsController : MonoBehaviour
{
    [Header("UI Containers")]
    public Transform column1Container; // Main single column container (Col_1)
    public TextMeshProUGUI column1Header;

    public AudioSource audioSource;
    public U7_Manager manager;

    [Header("Word Row Prefab")]
    public GameObject wordRowPrefab;

    [Header("Magic E Animation Settings")]
    public GameObject magicEEffectPrefab;

    [Header("Table Navigation")]
    public Button tablePrevButton;
    public Button tableNextButton;

    [Header("Header Tab Pills (Assign in Inspector)")]
    public GameObject tab1PillObject; // Tab 1 (i_e / ee / o_e / u_e)
    public GameObject tab2PillObject; // Tab 2 (ie / ea / oa / ue)
    public GameObject tab3PillObject; // Tab 3 (igh / ey / ow / ui)

    [Header("Card Sizing & Layout")]
    public Vector2 gridCellSize = new Vector2(210f, 200f);
    public Vector2 gridSpacing = new Vector2(25f, 25f);
    public float cardPrefabScale = 1.30f;
    public int gridColumnCount = 4;

    private U7_LevelData currentLevel;
    private int activeTableIndex = 0;
    private List<Transform> tabObjectsCache = new List<Transform>();
    private bool tabsInitialized = false;

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
            UnityEditor.EditorUtility.SetDirty(this);

        if (gridCellSize.x <= 0) gridCellSize.x = 210f;
        if (gridCellSize.y <= 0) gridCellSize.y = 200f;
        if (cardPrefabScale <= 0) cardPrefabScale = 1.30f;
        if (gridColumnCount <= 0) gridColumnCount = 4;
    }
#endif

    public void SetupActivity(U7_LevelData levelData)
    {
        currentLevel = levelData;
        if (manager == null) manager = FindFirstObjectByType<U7_Manager>(FindObjectsInactive.Include);
        if (currentLevel == null && manager != null) currentLevel = manager.levelLongI;
        if (currentLevel == null || currentLevel.teams == null || currentLevel.teams.Count == 0) return;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();

        EnsureContainersAssigned();

        tabsInitialized = false;
        SetupHeaderTabs();

        ShowTable(0);
    }

    public void OnTabClicked(int tabIndex)
    {
        Debug.Log($"[Activity 1 U7] OnTabClicked called for Tab {tabIndex}!");
        ShowTable(tabIndex);
    }

    public void ShowTable(int tableIndex)
    {
        EnsureContainersAssigned();
        if (manager == null) manager = FindFirstObjectByType<U7_Manager>(FindObjectsInactive.Include);

        if (currentLevel == null && manager != null)
            currentLevel = manager.activeLevel != null ? manager.activeLevel : manager.levelLongI;

        if (currentLevel == null)
        {
            currentLevel = Resources.Load<U7_LevelData>("Level_Long_I_teams");
#if UNITY_EDITOR
            if (currentLevel == null)
                currentLevel = UnityEditor.AssetDatabase.LoadAssetAtPath<U7_LevelData>("Assets/Data/Unit7/Levels/Level_Long_I_teams.asset");
#endif
        }

        if (currentLevel == null || currentLevel.teams == null || currentLevel.teams.Count == 0)
        {
            Debug.LogError("[Activity 1 U7] Cannot switch table! currentLevel is null or has 0 teams.");
            return;
        }

        activeTableIndex = Mathf.Clamp(tableIndex, 0, currentLevel.teams.Count - 1);
        U7_LongVowelTeamData activeTeam = currentLevel.teams[activeTableIndex];
        if (activeTeam == null) return;

        string levelTitle = currentLevel.levelTitle;
        string headerSpelling = GetExpectedHeaderSpelling(levelTitle, activeTableIndex, activeTeam.teamSpelling);

        Debug.Log($"<color=yellow>[Activity 1 U7] Switching to Table {activeTableIndex} ({headerSpelling}) - Spawning {activeTeam.teamWords.Count} cards...</color>");

        Transform col1 = GetTableParent(column1Container);
        if (col1 != null) col1.gameObject.SetActive(true);

        SetupHeader(column1Header, column1Container, activeTeam, headerSpelling);
        PopulateColumn(column1Container, activeTeam, headerSpelling);

        UpdateTabVisualStates(activeTableIndex);

        if (tablePrevButton != null) tablePrevButton.gameObject.SetActive(false);
        if (tableNextButton != null) tableNextButton.gameObject.SetActive(false);

        if (manager != null)
            manager.SetNextButtonState(activeTableIndex == currentLevel.teams.Count - 1);
    }

    // ─── Tab Setup ────────────────────────────────────────────────────────────

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
            // Determine tab names based on current level vowel
            string[] tabNames = GetTabNamesForLevel();
            string[] fallbackNames = new[] { "heading i_e", "heading ie", "heading igh" }; // Default copied names
            
            for (int j = 0; j < 3; j++)
            {
                string tName = tabNames[j];
                Transform found = FindChildByName(transform, tName);
                if (found == null && transform.parent != null) found = FindChildByName(transform.parent, tName);
                
                if (found == null) found = FindChildByName(transform, fallbackNames[j]);
                if (found == null && transform.parent != null) found = FindChildByName(transform.parent, fallbackNames[j]);

                if (found == null)
                {
                    foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
                    {
                        found = FindChildByName(root.transform, tName);
                        if (found == null) found = FindChildByName(root.transform, fallbackNames[j]);
                        if (found != null) break;
                    }
                }
                if (found != null)
                {
                    tabObjectsCache.Add(found);
                    Debug.Log($"[Activity 1 U7] Auto-found tab: '{tName}' at {GetTransformPath(found)}");
                }
                else
                {
                    Debug.LogWarning($"[Activity 1 U7] Could not find tab named '{tName}' or '{fallbackNames[j]}' in scene!");
                }
            }
        }

        Debug.Log($"[Activity 1 U7] SetupHeaderTabs: wiring {tabObjectsCache.Count} tabs...");

        for (int i = 0; i < tabObjectsCache.Count; i++)
        {
            int capturedIndex = i;
            Transform tab = tabObjectsCache[i];
            if (tab == null) continue;

            tab.gameObject.SetActive(true);


            // Ensure Image exists & raycasts
            Image tabImg = tab.GetComponent<Image>();
            if (tabImg == null)
            {
                tabImg = tab.gameObject.AddComponent<Image>();
                tabImg.color = new Color(1, 1, 1, 0.01f);
            }
            tabImg.raycastTarget = true;

            // Disable raycast on child TEXT
            foreach (var g in tab.GetComponentsInChildren<Graphic>(true))
            {
                if (g is TextMeshProUGUI || g is Text)
                    g.raycastTarget = false;
            }

            // Wire Button.onClick
            Button btn = tab.GetComponent<Button>();
            if (btn == null)
            {
                btn = tab.gameObject.AddComponent<Button>();
                Debug.Log($"[Activity 1 U7] Added Button to tab {capturedIndex} ('{tab.name}')");
            }
            if (btn.targetGraphic == null) btn.targetGraphic = tabImg;
            btn.interactable = true;
            btn.transition = Selectable.Transition.ColorTint;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() =>
            {
                Debug.Log($"<color=cyan>[Activity 1 U7] Tab clicked: index {capturedIndex} ('{tab.name}')</color>");
                ShowTable(capturedIndex);
            });

            // Belt-and-suspenders: IPointerClickHandler via U7_TabButton
            U7_TabButton tabHandler = tab.GetComponent<U7_TabButton>();
            if (tabHandler == null) tabHandler = tab.gameObject.AddComponent<U7_TabButton>();
            tabHandler.tabIndex = capturedIndex;
            tabHandler.controller = this;

            Debug.Log($"[Activity 1 U7] Tab {capturedIndex} wired: '{tab.name}'");

            // CRITICAL FIX: Move to last sibling so it renders ON TOP of Columns container
            tab.SetAsLastSibling();

            // Add Canvas override so tab always receives raycasts first
            Canvas tabCanvas = tab.GetComponent<Canvas>();
            if (tabCanvas == null) tabCanvas = tab.gameObject.AddComponent<Canvas>();
            tabCanvas.overrideSorting = true;
            tabCanvas.sortingOrder = 10;

            GraphicRaycaster tabRaycaster = tab.GetComponent<GraphicRaycaster>();
            if (tabRaycaster == null) tab.gameObject.AddComponent<GraphicRaycaster>();
        }

        tabsInitialized = true;
    }

    private string[] GetTabNamesForLevel()
    {
        string title = currentLevel != null && currentLevel.levelTitle != null
            ? currentLevel.levelTitle.ToLower() : "";

        if (title.Contains("long o") || title.Contains("long_o"))
            return new[] { "heading o_e", "heading oa", "heading ow" };
        if (title.Contains("long u") || title.Contains("long_u"))
            return new[] { "heading u_e", "heading ue", "heading ui" };
        // Default: Long I
        return new[] { "heading i_e", "heading ie", "heading igh" };
    }

    private void UpdateTabVisualStates(int selectedIndex)
    {
        for (int i = 0; i < tabObjectsCache.Count; i++)
        {
            Transform tab = tabObjectsCache[i];
            if (tab == null) continue;

            bool isSelected = (i == selectedIndex);
            tab.localScale = isSelected ? Vector3.one * 1.15f : Vector3.one * 0.95f;

            Image img = tab.GetComponent<Image>();
            if (img != null)
                img.color = isSelected ? Color.white : new Color(0.80f, 0.80f, 0.80f, 0.9f);

            TextMeshProUGUI tmp = tab.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmp != null)
            {
                tmp.color = isSelected ? new Color(1f, 0.85f, 0f, 1f) : Color.white;
                tmp.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                
                // Dynamically update the text to the correct team spelling
                if (currentLevel != null && i < currentLevel.teams.Count)
                {
                    tmp.text = GetExpectedHeaderSpelling(currentLevel.levelTitle, i, currentLevel.teams[i].teamSpelling);
                }
            }
        }
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

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

    private Transform GetTableParent(Transform rawContainer)
    {
        if (rawContainer == null) return null;
        if (rawContainer.parent != null && rawContainer.parent.name.ToLower().Contains("column"))
            return rawContainer.parent;
        return rawContainer;
    }

    private string GetExpectedHeaderSpelling(string levelTitle, int teamIndex, string fallbackSpelling)
    {
        string title = levelTitle != null ? levelTitle.ToLower().Trim() : "";

        if (title.Contains("long i") || title.Contains("long_i") || title.Contains("i teams"))
        {
            if (teamIndex == 0) return "i_e";
            if (teamIndex == 1) return "ie";
            if (teamIndex == 2) return "igh";
        }
        else if (title.Contains("long o") || title.Contains("long_o") || title.Contains("o teams"))
        {
            if (teamIndex == 0) return "o_e";
            if (teamIndex == 1) return "oa";
            if (teamIndex == 2) return "ow";
        }
        else if (title.Contains("long u") || title.Contains("long_u") || title.Contains("u teams"))
        {
            if (teamIndex == 0) return "u_e";
            if (teamIndex == 1) return "ue";
            if (teamIndex == 2) return "ui";
        }

        if (!string.IsNullOrEmpty(fallbackSpelling)) return fallbackSpelling;
        return teamIndex == 0 ? "i_e" : (teamIndex == 1 ? "ie" : "igh");
    }

    private void SetupHeader(TextMeshProUGUI headerText, Transform container, U7_LongVowelTeamData teamData, string headerSpelling)
    {
        if (teamData == null) return;
        if (string.IsNullOrEmpty(headerSpelling)) headerSpelling = teamData.teamSpelling;
        if (headerText != null) headerText.text = headerSpelling;
    }

    private void PopulateColumn(Transform container, U7_LongVowelTeamData teamData, string headerSpelling)
    {
        if (container == null || teamData == null) return;
        if (string.IsNullOrEmpty(headerSpelling)) headerSpelling = teamData.teamSpelling;

        // Remove any existing layout groups before adding GridLayoutGroup (matches U6)
        HorizontalLayoutGroup hlg = container.GetComponent<HorizontalLayoutGroup>();
        if (hlg != null) DestroyImmediate(hlg);
        VerticalLayoutGroup vlg = container.GetComponent<VerticalLayoutGroup>();
        if (vlg != null) DestroyImmediate(vlg);

        // Configure Grid Layout
        GridLayoutGroup grid = container.GetComponent<GridLayoutGroup>();
        if (grid == null) grid = container.gameObject.AddComponent<GridLayoutGroup>();
        grid.cellSize = gridCellSize;
        grid.spacing = gridSpacing;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = gridColumnCount > 0 ? gridColumnCount : 4;
        // Clear existing cards
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            if (child != null)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }

        // Find prefab
        if (wordRowPrefab == null)
        {
            wordRowPrefab = Resources.Load<GameObject>("U6_WordRowPrefab");
            if (wordRowPrefab == null) wordRowPrefab = Resources.Load<GameObject>("Prefabs/U6_WordRowPrefab");
#if UNITY_EDITOR
            if (wordRowPrefab == null)
            {
                string[] guids = UnityEditor.AssetDatabase.FindAssets("U6_WordRowPrefab t:Prefab");
                if (guids.Length == 0) guids = UnityEditor.AssetDatabase.FindAssets("WordCard t:Prefab");
                if (guids.Length > 0)
                    wordRowPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            }
#endif
        }

        if (wordRowPrefab == null)
        {
            Debug.LogError("[Activity 1 U7] wordRowPrefab is null! Assign it in the Inspector.");
            return;
        }

        // Spawn cards
        foreach (CVCWordData wordData in teamData.teamWords)
        {
            if (wordData == null) continue;

            GameObject item = Instantiate(wordRowPrefab, container);
            float sc = cardPrefabScale > 0f ? cardPrefabScale : 1.30f;
            item.transform.localScale = Vector3.one * sc;
            item.transform.localPosition = Vector3.zero;
            item.SetActive(true);

            // Set Text — BLACK base colour, RED team-letter highlight (matches U6)
            string formattedWord = HighlightTeamLetters(wordData.word, headerSpelling);

            foreach (var tmp in item.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                tmp.color = Color.black;
                tmp.text = formattedWord;
                tmp.enableAutoSizing = true;
                tmp.fontSizeMin = 28;
                tmp.fontSizeMax = 60;
                tmp.fontStyle = FontStyles.Bold;
                tmp.raycastTarget = false;
            }
            foreach (var uiText in item.GetComponentsInChildren<Text>(true))
            {
                uiText.color = Color.black;
                uiText.text = wordData.word;
                uiText.raycastTarget = false;
            }

            // Set Picture
            Image img = null;
            Transform picTrans = item.transform.Find("WordPicture");
            if (picTrans == null) picTrans = item.transform.Find("WordPicture ");
            if (picTrans == null) picTrans = item.transform.Find("Image");

            if (picTrans != null)
                img = picTrans.GetComponent<Image>();
            else
            {
                foreach (var childImg in item.GetComponentsInChildren<Image>(true))
                {
                    if (childImg.gameObject != item) { img = childImg; break; }
                }
            }

            if (img != null && wordData.wordPicture != null)
            {
                img.sprite = wordData.wordPicture;
                img.preserveAspect = true;
                img.raycastTarget = false;
                img.gameObject.SetActive(true);
            }

            // Hide blank image boxes
            foreach (var childImg in item.GetComponentsInChildren<Image>(true))
            {
                if (childImg.gameObject != item && childImg.sprite == null)
                    childImg.gameObject.SetActive(false);
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
            if (allButtons.Length == 0)
            {
                // Fallback: add button to root if none exist
                Button rootBtn = item.AddComponent<Button>();
                Image rootImg = item.GetComponent<Image>();
                if (rootImg != null) rootImg.raycastTarget = true;
                if (rootBtn.targetGraphic == null && rootImg != null) rootBtn.targetGraphic = rootImg;
                allButtons = new Button[] { rootBtn };
            }

            foreach (var b in allButtons)
            {
                if (b.targetGraphic != null)
                {
                    b.targetGraphic.raycastTarget = true;
                }

                CVCWordData capturedWord = wordData;
                GameObject capturedItem = item;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => OnWordCardClicked(capturedWord, teamData.teamSpelling, capturedItem));
            }

            StartCoroutine(AnimateCardSpawn(item));
        }
    }

    // ─── Word Card ────────────────────────────────────────────────────────────

    private string HighlightTeamLetters(string word, string teamSpelling)
    {
        if (string.IsNullOrEmpty(word)) return "";
        string cleanSpelling = teamSpelling.Replace("_", "").ToLower().Trim();

        // Magic-E: vowel + consonants + e → highlight vowel and final e in RED
        if (teamSpelling == "a_e" || teamSpelling == "i_e" || teamSpelling == "o_e" || teamSpelling == "u_e")
        {
            if (word.Length >= 3 && word.EndsWith("e"))
            {
                char firstChar = word[0];
                char vowelChar = word[1];
                string middleChars = word.Substring(1, word.Length - 2);
                return $"{firstChar}<color=#FF0000>{vowelChar}</color>{middleChars.Substring(1)}<color=#FF0000>e</color>";
            }
        }

        // All other teams: highlight the team spelling in RED
        if (word.Contains(cleanSpelling))
            return word.Replace(cleanSpelling, $"<color=#FF0000>{cleanSpelling}</color>");

        return word;
    }

    private void OnWordCardClicked(CVCWordData wordData, string teamSpelling, GameObject cardObj)
    {
        if (wordData == null) return;
        Debug.Log($"[Activity 1 U7] Word clicked: '{wordData.word}' (Team: {teamSpelling})");

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
            Debug.LogWarning($"[Activity 1 U7] fullWordAudio is null on CVCWordData for '{wordData.word}' — assign it in the Inspector.");
        }

        if (cardObj != null) StartCoroutine(AnimateCard(cardObj));

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null) mascot.PlayHiAnimation();
    }


    // ─── Navigation ──────────────────────────────────────────────────────────

    public void OnPrevTableClicked()
    {
        if (activeTableIndex > 0) ShowTable(activeTableIndex - 1);
    }

    public void OnNextTableClicked()
    {
        if (currentLevel != null && activeTableIndex < currentLevel.teams.Count - 1)
            ShowTable(activeTableIndex + 1);
        else if (manager != null)
            manager.StartActivity2();
    }

    public void HideNavButtons()
    {
        if (tablePrevButton != null) tablePrevButton.gameObject.SetActive(false);
        if (tableNextButton != null) tableNextButton.gameObject.SetActive(false);
    }

    // ─── Coroutines ───────────────────────────────────────────────────────────

    private IEnumerator AnimateCardSpawn(GameObject cardObj)
    {
        if (cardObj == null) yield break;
        Vector3 targetScale = cardObj.transform.localScale;
        cardObj.transform.localScale = Vector3.zero;
        float elapsed = 0f;
        float duration = 0.20f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cardObj.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, elapsed / duration);
            yield return null;
        }
        cardObj.transform.localScale = targetScale;
    }

    private IEnumerator AnimateCard(GameObject cardObj)
    {
        if (cardObj == null) yield break;
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

// ─── Tab Click Handler ────────────────────────────────────────────────────────

public class U7_TabButton : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
{
    public int tabIndex;
    public U7_A1_MeetTeamsController controller;

    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        Debug.Log($"<color=green>[Activity 1 U7] Tab PointerClick: Index {tabIndex} on '{gameObject.name}'</color>");
        if (controller != null) controller.ShowTable(tabIndex);
    }
}
