using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Full home-screen flow:
///
///   [Home]  → Play →  [Selection Panel: N category buttons]
///                              ↓  click any category
///                      [Sub-Panel: dynamic buttons]
///                              ↓  click a sub-button
///                      • All home-screen GameObjects hidden
///                      • Linked GameObject enabled  (shows its home screen UI)
///                      • URL + SceneName stored in AppSession
///                              ↓  click Learn on that home screen
///                      AssetBundleLoader downloads bundle → loads scene
/// </summary>
public class HomeScreenManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    // Exposed so LearnButton can cache the reference once in Start() instead of
    // calling FindObjectOfType on every click.
    public static HomeScreenManager Instance { get; private set; }

    // ── Screens ───────────────────────────────────────────────────────────────
    [Header("Screens")]
    [SerializeField] private GameObject homeScreen;
    [SerializeField] private GameObject selectionPanel;
    [SerializeField] private GameObject subPanel;

   

    // ── Selection Panel ───────────────────────────────────────────────────────
    [Header("Selection Panel")]
    [SerializeField] private Button[]          categoryButtons;
    [SerializeField] private TextMeshProUGUI[] categoryLabels;

    // ── Sub-Panel ─────────────────────────────────────────────────────────────
    [Header("Sub-Panel")]
    [SerializeField] private TextMeshProUGUI subPanelHeading;
    [SerializeField] private Transform       subButtonContainer;
    [SerializeField] private Button          subButtonPrefab;
    [SerializeField] private Button          backButton;

    // ── Home Screen ───────────────────────────────────────────────────────────
    [Header("Home Screen")]
    [SerializeField] private Button playButton;

    // ── Loading UI ────────────────────────────────────────────────────────────
    [Header("Loading UI")]
    [SerializeField] private GameObject      loadingOverlay;  // full-screen overlay
    [SerializeField] private Slider          progressBar;     // optional
    [SerializeField] private TextMeshProUGUI progressLabel;   // optional "47%"
    [SerializeField] private TextMeshProUGUI errorLabel;      // shown on failure

    // ── Data ──────────────────────────────────────────────────────────────────
    [Header("Data")]
    [SerializeField] private PanelConfig panelConfig;

    [System.Serializable]
public class HomeScreenEntry
{
    public string id;
    public GameObject screen;
}

[SerializeField] private List<HomeScreenEntry> homeScreens;

private Dictionary<string, GameObject> _homeScreenMap;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private readonly List<Button>     _spawnedButtons   = new List<Button>();
    private readonly List<GameObject> _allHomeScreens   = new List<GameObject>();
    private GameObject                _activeHomeScreen = null;

    // Tracked so we can re-enable it after a load completes or errors.
    private Button _activeLearnButton = null;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        ValidateConfig();

      _homeScreenMap = new Dictionary<string, GameObject>();

foreach (var entry in homeScreens)
{
    if (!string.IsNullOrEmpty(entry.id) && entry.screen != null)
    {
        _homeScreenMap[entry.id] = entry.screen;
        entry.screen.SetActive(false); // hide all at start
    }
}
        foreach (var go in _allHomeScreens) go.SetActive(false);

        // Wire category buttons — use categoryButtons.Length, not a hard-coded 4,
        // so the config size check in ValidateConfig() is the only place to change.
        for (int i = 0; i < categoryButtons.Length; i++)
        {
            int idx = i;
            categoryLabels[i].text = panelConfig.categories[i].buttonLabel;
            categoryButtons[i].onClick.AddListener(() => OnCategoryClicked(idx));
        }

        backButton.onClick.AddListener(OnBack);
        playButton.onClick.AddListener(OnPlay);

        if (loadingOverlay) loadingOverlay.SetActive(false);
        if (errorLabel)     errorLabel.gameObject.SetActive(false);

        ShowScreen(homeScreen);
    }

    private void Start()
    {
        // FIX #1: Event subscription moved from Awake() to Start().
        // Unity does not guarantee Awake() execution order across MonoBehaviours,
        // so AssetBundleLoader.Instance could still be null during our Awake().
        // By Start() all Awake() calls in the scene are complete, so Instance is
        // guaranteed to be set.
        if (AssetBundleLoader.Instance != null)
        {
            AssetBundleLoader.Instance.OnDownloadProgress += HandleProgress;
            AssetBundleLoader.Instance.OnDownloadComplete += HandleComplete;
            AssetBundleLoader.Instance.OnError            += HandleError;
        }
        else
        {
            Debug.LogError("[HomeScreenManager] AssetBundleLoader singleton not found. " +
                           "Make sure AssetBundleLoader GameObject is in the scene.");
        }
    }

    private void OnDestroy()
    {
        if (AssetBundleLoader.Instance != null)
        {
            AssetBundleLoader.Instance.OnDownloadProgress -= HandleProgress;
            AssetBundleLoader.Instance.OnDownloadComplete -= HandleComplete;
            AssetBundleLoader.Instance.OnError            -= HandleError;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Screen navigation
    // ─────────────────────────────────────────────────────────────────────────

    private void OnPlay()
    {
        ShowScreen(selectionPanel);
    }

    private void OnCategoryClicked(int index)
    {
        CategoryData data = panelConfig.categories[index];
        BuildSubPanel(data);
        ShowScreen(subPanel);
    }

    private void OnBack()
    {
        HideActiveHomeScreen();

        // FIX #6: Clear stale session data when the user navigates back.
        // Without this, a fresh sub-button selection overwrites correctly, but
        // if the user somehow triggers Learn without making a new selection the
        // old URL/scene would still fire.
        AppSession.Clear();

        ShowScreen(selectionPanel);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Sub-panel construction
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildSubPanel(CategoryData data)
    {
        subPanelHeading.text = data.panelHeading;

        foreach (var btn in _spawnedButtons) Destroy(btn.gameObject);
        _spawnedButtons.Clear();

        foreach (var subData in data.subButtons)
        {
            Button btn   = Instantiate(subButtonPrefab, subButtonContainer);
            var label    = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label) label.text = subData.buttonLabel;

            SubButtonData captured = subData;
            btn.onClick.AddListener(() => OnSubButtonClicked(captured));

            _spawnedButtons.Add(btn);

            homeScreen.SetActive(false);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Sub-button click → store data, enable linked home screen
    // ─────────────────────────────────────────────────────────────────────────

    private void OnSubButtonClicked(SubButtonData data)
    {
        AppSession.PendingBundleUrl = data.assetBundleUrl;
        AppSession.PendingSceneName = data.sceneName;

        Debug.Log($"[HomeScreen] Selected → URL: {data.assetBundleUrl} | Scene: {data.sceneName}");

        HideActiveHomeScreen();

        homeScreen.SetActive(false);

    if (_homeScreenMap.TryGetValue(data.homeScreenId, out GameObject screen))
{
    screen.SetActive(true);
    _activeHomeScreen = screen;
}
else
{
    Debug.LogError($"[HomeScreen] No HomeScreen found for ID: {data.homeScreenId}");
}

        ShowScreen(null);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Learn button entry point (called by LearnButton.cs)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by LearnButton. Pass the Button reference so it can be
    /// disabled during loading and re-enabled when the load finishes or errors.
    /// </summary>
    public void OnLearnClicked(Button sourceButton = null)
    {
        string url   = AppSession.PendingBundleUrl;
        string scene = AppSession.PendingSceneName;

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(scene))
        {
            Debug.LogError("[HomeScreen] Learn clicked but no URL/Scene in AppSession.");
            return;
        }

        if (AssetBundleLoader.Instance == null)
        {
            Debug.LogError("[HomeScreen] AssetBundleLoader singleton not found.");
            return;
        }

        // FIX #5: Disable the Learn button for the duration of the load so a
        // double-tap can't fire a second request or desync the overlay state.
        _activeLearnButton = sourceButton;
        if (_activeLearnButton != null) _activeLearnButton.interactable = false;

        ShowLoadingOverlay(true);
        AssetBundleLoader.Instance.LoadSceneFromBundle(url, scene);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  AssetBundleLoader callbacks
    // ─────────────────────────────────────────────────────────────────────────

    private void HandleProgress(float t)
    {
        if (progressBar)   progressBar.value  = t;
        if (progressLabel) progressLabel.text = $"{Mathf.RoundToInt(t * 100)}%";
    }

    private void HandleComplete()
    {
        // FIX #2 (consequence): OnDownloadComplete now fires only after the scene
        // is fully loaded (fixed in AssetBundleLoader), so hiding the overlay here
        // is safe — the scene is already active.
        ShowLoadingOverlay(false);
        ReEnableLearnButton();
    }

    private void HandleError(string msg)
    {
        ShowLoadingOverlay(false);
        ReEnableLearnButton();

        if (errorLabel)
        {
            errorLabel.gameObject.SetActive(true);
            errorLabel.text = $"Error: {msg}";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void ReEnableLearnButton()
    {
        if (_activeLearnButton != null)
        {
            _activeLearnButton.interactable = true;
            _activeLearnButton = null;
        }
    }

    private void HideActiveHomeScreen()
    {
        if (_activeHomeScreen != null)
        {
            _activeHomeScreen.SetActive(false);
            _activeHomeScreen = null;
        }
    }

    /// <summary>Pass null to hide all navigation panels (when a linked home screen takes over).</summary>
    private void ShowScreen(GameObject target)
    {
        homeScreen.SetActive(target == homeScreen);
        selectionPanel.SetActive(target == selectionPanel);
        subPanel.SetActive(target == subPanel);
    }

    private void ShowLoadingOverlay(bool show)
    {
        if (loadingOverlay) loadingOverlay.SetActive(show);
        if (!show && errorLabel) errorLabel.gameObject.SetActive(false);
    }

    private void ValidateConfig()
    {
        if (panelConfig == null)
        {
            Debug.LogError("[HomeScreenManager] PanelConfig is not assigned!");
            return;
        }

        // FIX #7: The old check tested categories.Count < 4 (hard-coded).
        // The real constraint is that the count matches the number of wired
        // category buttons, otherwise the for-loop in Awake() throws an
        // IndexOutOfRangeException.
        if (panelConfig.categories == null ||
            panelConfig.categories.Count != categoryButtons.Length)
        {
            Debug.LogError($"[HomeScreenManager] PanelConfig has " +
                           $"{panelConfig.categories?.Count ?? 0} categories but " +
                           $"{categoryButtons.Length} buttons are wired. They must match.");
        }
    }
}