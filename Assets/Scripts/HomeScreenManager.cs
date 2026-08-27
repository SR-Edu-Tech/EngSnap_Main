using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Changes vs previous version
/// ────────────────────────────
/// 1. SetCategoryLockStates(bool[]) — call this from GameAuthManager after the
///    backend returns the student's unlock flags.  Locked buttons are disabled
///    (greyed out + non-interactable).  Unlocked buttons are fully enabled.
///
/// 2. The LockedButtonHandler component is NOT used for category buttons any
///    more.  Lock state comes from the backend, not a static Inspector field.
///
/// 3. Everything else is unchanged.
/// </summary>
public class HomeScreenManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
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

    [Tooltip("(Optional) Sprites to swap on locked/unlocked state per category button. " +
             "Leave empty to rely on the button's built-in disabled colour tint instead.")]
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;

    // ── Sub-Panel ─────────────────────────────────────────────────────────────
    [Header("Sub-Panel")]
    [SerializeField] private TextMeshProUGUI subPanelHeading;
    [SerializeField] private Transform       subButtonContainer;
    [SerializeField] private Button          subButtonPrefab;
    [SerializeField] private Button          backButton;
    [SerializeField] private AudioSource     splashscreenAudio;

    // ── Sub-Panel Background ──────────────────────────────────────────────────
    [Header("Sub-Panel Background")]
    [SerializeField] private Image subPanelBackground;

    // ── Home Screen ───────────────────────────────────────────────────────────
    [Header("Home Screen")]
    [SerializeField] private Button playButton;

    [Header("User Greeting")]
    [SerializeField] private TextMeshProUGUI greetingLabel;
    [SerializeField] private string          greetingFormat = "Hi, {0}!";

    // ── Loading UI ────────────────────────────────────────────────────────────
    [Header("Loading UI")]
    [SerializeField] private GameObject      loadingOverlay;
    [SerializeField] private Slider          progressBar;
    [SerializeField] private TextMeshProUGUI progressLabel;
    [SerializeField] private TextMeshProUGUI errorLabel;

    // ── Data ──────────────────────────────────────────────────────────────────
    [Header("Data")]
    [SerializeField] private PanelConfig panelConfig;

    [Header("Main Camera")]
    [SerializeField] private Camera mainCamera;

    // ── Home Screen entries ───────────────────────────────────────────────────
    [System.Serializable]
    public class HomeScreenEntry
    {
        public string     id;
        public GameObject screen;
    }

    [SerializeField] private List<HomeScreenEntry> homeScreens;
    private Dictionary<string, GameObject> _homeScreenMap;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private readonly List<Button>     _spawnedButtons   = new List<Button>();
    private readonly List<GameObject> _allHomeScreens   = new List<GameObject>();
    private GameObject                _activeHomeScreen = null;
    private Button                    _activeLearnButton = null;
    private static GameObject         _rememberedHomeScreen = null;

    // Course IDs unlocked per the backend's assigned_courses (per-level, not just
    // per-category). Null = not yet known (treated as "don't restrict" so sub-panels
    // aren't accidentally locked before login data arrives).
    private HashSet<int> _unlockedCourseIds = null;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        ValidateConfig();

        _homeScreenMap = new Dictionary<string, GameObject>();
        foreach (var entry in homeScreens)
        {
            if (!string.IsNullOrEmpty(entry.id) && entry.screen != null)
            {
                _homeScreenMap[entry.id] = entry.screen;
                entry.screen.SetActive(false);
            }
        }

        foreach (var go in _allHomeScreens) go.SetActive(false);

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
        if (CachedBundleLoader.Instance != null)
            SubscribeToLoader();
        else
            StartCoroutine(WaitForLoader());

        RefreshGreeting();
    }

    private IEnumerator WaitForLoader()
    {
        float timeout = 5f;
        while (CachedBundleLoader.Instance == null && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }
        if (CachedBundleLoader.Instance != null)
            SubscribeToLoader();
        else
            Debug.LogError("[HomeScreenManager] CachedBundleLoader not found after 5 s.");
    }

    private void SubscribeToLoader()
    {
        CachedBundleLoader.Instance.OnLoadProgress += HandleProgress;
        CachedBundleLoader.Instance.OnLoadComplete += HandleComplete;
        CachedBundleLoader.Instance.OnError        += HandleError;
    }

    private void OnDestroy()
    {
        if (CachedBundleLoader.Instance != null)
        {
            CachedBundleLoader.Instance.OnLoadProgress -= HandleProgress;
            CachedBundleLoader.Instance.OnLoadComplete -= HandleComplete;
            CachedBundleLoader.Instance.OnError        -= HandleError;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Lock / Unlock API
    //  Call from GameAuthManager after the backend returns the student's data.
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sets the locked/unlocked state of each category button from backend data.
    ///
    /// USAGE — call this from GameAuthManager once you have the student's
    /// accessible categories, e.g.:
    ///
    ///   bool[] unlocked = { true, false, false, false }; // only Beginners open
    ///   HomeScreenManager.Instance.SetCategoryLockStates(unlocked);
    ///
    /// The array must be the same length as the number of category buttons.
    /// Index 0 = Beginners, 1 = Juniors, 2 = Seniors, 3 = Masters (or whatever
    /// order your PanelConfig defines).
    /// </summary>
    public void SetCategoryLockStates(bool[] unlockedFlags)
    {
        if (unlockedFlags == null)
        {
            Debug.LogWarning("[HomeScreenManager] SetCategoryLockStates: null array passed.");
            return;
        }

        for (int i = 0; i < categoryButtons.Length; i++)
        {
            bool unlocked = (i < unlockedFlags.Length) && unlockedFlags[i];
            ApplyLockState(categoryButtons[i], unlocked);
        }
    }

    /// <summary>
    /// Convenience overload that accepts a HashSet of unlocked category indices.
    /// </summary>
    public void SetCategoryLockStates(HashSet<int> unlockedIndices)
    {
        for (int i = 0; i < categoryButtons.Length; i++)
            ApplyLockState(categoryButtons[i], unlockedIndices.Contains(i));
    }

    /// <summary>
    /// Sets the full set of unlocked course IDs (per level, e.g. "Beginners Level 1" = 12,
    /// "Beginners Level 2" = 13), used to lock/unlock individual sub-buttons inside a
    /// sub-panel. Call this from GameAuthManager right alongside SetCategoryLockStates.
    /// Pass null to clear (e.g. on logout) — this makes BuildSubPanel treat everything
    /// as unrestricted until fresh data arrives.
    /// </summary>
    public void SetUnlockedCourseIds(HashSet<int> unlockedCourseIds)
    {
        _unlockedCourseIds = unlockedCourseIds;
    }

    private void ApplyLockState(Button btn, bool unlocked)
    {
        btn.interactable = unlocked;

        // Optional: swap sprites if provided
        if (lockedSprite != null && unlockedSprite != null)
        {
            Image img = btn.GetComponent<Image>();
            if (img != null)
                img.sprite = unlocked ? unlockedSprite : lockedSprite;
        }

        // Dim the label text to reinforce locked state
        TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.alpha = unlocked ? 1f : 0.4f;
    }

    /// <summary>
    /// Locks/unlocks a spawned sub-button based on:
    ///   1. courseId == 0  → never API-locked (always unlocked, unless manuallyLocked)
    ///   2. courseId != 0  → unlocked only if present in _unlockedCourseIds
    ///   3. manuallyLocked → always wins, forces locked regardless of the above
    /// _unlockedCourseIds == null means backend data hasn't arrived yet — in that
    /// case courseId-based buttons are left unrestricted rather than locked, so a
    /// slow API response can't accidentally block everything.
    /// </summary>
    private void ApplySubButtonLockState(Button btn, SubButtonData data)
    {
        bool unlocked = data.courseId == 0
            || _unlockedCourseIds == null
            || _unlockedCourseIds.Contains(data.courseId);

        if (data.manuallyLocked)
            unlocked = false;

        btn.interactable = unlocked;

        TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.alpha = unlocked ? 1f : 0.4f;

        Image img = btn.GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = unlocked ? 1f : 0.5f;
            img.color = c;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Greeting
    // ─────────────────────────────────────────────────────────────────────────
    public void RefreshGreeting()
    {
        if (greetingLabel == null) return;
        string name = AppSession.UserName;
        greetingLabel.text = !string.IsNullOrEmpty(name)
            ? string.Format(greetingFormat, name)
            : string.Empty;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Screen navigation
    // ─────────────────────────────────────────────────────────────────────────
    private void OnPlay()
    {
        if (FirstRunDownloader.Instance != null)
            FirstRunDownloader.Instance.StartFlow();
        else
        {
            Debug.LogWarning("[HomeScreenManager] FirstRunDownloader not found — opening selection directly.");
            ShowScreen(selectionPanel);
        }
    }

    public void ShowSelectionPanel() => ShowScreen(selectionPanel);

    private void OnCategoryClicked(int index)
    {
        BuildSubPanel(panelConfig.categories[index]);
        ShowScreen(subPanel);
    }

    private void OnBack()
    {
        HideActiveHomeScreen();
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

        if (data.subButtons != null && data.subButtons.Count > 0)
            SetSubPanelBackground(data.subButtons[0].backgroundSprite);

        foreach (var subData in data.subButtons)
        {
            Button btn = Instantiate(subButtonPrefab, subButtonContainer);

            var label = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (label) label.text = subData.buttonLabel;

            if (subData.buttonSprite != null)
            {
                Image btnImage = btn.GetComponent<Image>();
                if (btnImage != null) btnImage.sprite = subData.buttonSprite;
            }

            ApplySubButtonLockState(btn, subData);

            SubButtonData captured = subData;
            btn.onClick.AddListener(() => OnSubButtonClicked(captured));
            _spawnedButtons.Add(btn);
        }

        homeScreen.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Sub-button click
    // ─────────────────────────────────────────────────────────────────────────
    private void OnSubButtonClicked(SubButtonData data)
    {
        SetSubPanelBackground(data.backgroundSprite);

        string resolvedUrl = data.GetAssetBundleUrl();

        AppSession.PendingBundleUrl = resolvedUrl;
        AppSession.PendingSceneName = data.sceneName;

        Debug.Log($"[HomeScreen] Selected → Platform: {Application.platform} | " +
                  $"URL: {resolvedUrl} | Scene: {data.sceneName}");

        HideActiveHomeScreen();
        homeScreen.SetActive(false);

        if (splashscreenAudio != null)
            splashscreenAudio.gameObject.SetActive(false);

        if (_homeScreenMap.TryGetValue(data.homeScreenId, out GameObject screen))
        {
            screen.SetActive(true);
            _activeHomeScreen = screen;
            Debug.Log($"[HomeScreen] _activeHomeScreen set to: {screen.name}");
        }
        else
        {
            Debug.LogError($"[HomeScreen] homeScreenId '{data.homeScreenId}' not found in map. " +
                           $"Available IDs: {string.Join(", ", _homeScreenMap.Keys)}");
        }

        ShowScreen(null);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Learn click
    // ─────────────────────────────────────────────────────────────────────────
    public void OnLearnClicked(Button sourceButton = null)
    {
        string url   = AppSession.PendingBundleUrl;
        string scene = AppSession.PendingSceneName;

        if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(scene))
        {
            Debug.LogError("[HomeScreen] Learn clicked but no URL/Scene in AppSession.");
            return;
        }
        if (FirstRunDownloader.Instance == null && CachedBundleLoader.Instance == null)
        {
            Debug.LogError("[HomeScreen] No lesson loader is available.");
            return;
        }

        _activeLearnButton = sourceButton;
        if (_activeLearnButton != null) _activeLearnButton.interactable = false;

        _rememberedHomeScreen = _activeHomeScreen;

        ShowLoadingOverlay(true);
        if (FirstRunDownloader.Instance != null)
            FirstRunDownloader.Instance.DownloadBundleAndLoadScene(url, scene);
        else
            CachedBundleLoader.Instance.LoadSceneFromDisk(url, scene);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Restore after bundle scene exits
    // ─────────────────────────────────────────────────────────────────────────
    public void RestoreAfterBundle()
    {
        Debug.Log($"[HomeScreenManager] RestoreAfterBundle called. " +
                  $"Remembered: {(_rememberedHomeScreen != null ? _rememberedHomeScreen.name : "NULL")}");

        if (mainCamera != null) mainCamera.gameObject.SetActive(true);

        if (_rememberedHomeScreen != null)
        {
            _rememberedHomeScreen.SetActive(true);
            _activeHomeScreen     = _rememberedHomeScreen;
            _rememberedHomeScreen = null;
        }
        else
        {
            homeScreen.SetActive(true);
        }

        ShowLoadingOverlay(false);
        ReEnableLearnButton();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Restore directly to the main home screen (clearing active/remembered screens)
    // ─────────────────────────────────────────────────────────────────────────
    public void RestoreToHomeScreen()
    {
        Debug.Log("[HomeScreenManager] RestoreToHomeScreen called.");

        if (mainCamera != null) mainCamera.gameObject.SetActive(true);

        HideActiveHomeScreen();
        _rememberedHomeScreen = null;

        ShowLoadingOverlay(false);
        ReEnableLearnButton();

        ShowScreen(homeScreen);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Progress / error handlers
    // ─────────────────────────────────────────────────────────────────────────
    private void HandleProgress(float t)
    {
        if (progressBar)   progressBar.value  = t;
        if (progressLabel) progressLabel.text = $"{Mathf.RoundToInt(t * 100)}%";
    }

    private void HandleComplete()
    {
        ShowLoadingOverlay(false);
        ReEnableLearnButton();
        if (mainCamera != null) mainCamera.gameObject.SetActive(false);
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

    public void ShowLoadError(string msg)
    {
        HandleError(msg);
    }

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

    private void ShowScreen(GameObject target)
    {
        homeScreen.SetActive(target == homeScreen);
        selectionPanel.SetActive(target == selectionPanel);
        subPanel.SetActive(target == subPanel);
    }

    private void ShowLoadingOverlay(bool show)
    {
        if (loadingOverlay) loadingOverlay.SetActive(show);
        if (errorLabel) errorLabel.gameObject.SetActive(false);
        if (show)
        {
            if (progressBar) progressBar.value = 0f;
            if (progressLabel) progressLabel.text = "0%";
        }
    }

    private void SetSubPanelBackground(Sprite sprite)
    {
        if (subPanelBackground == null || sprite == null) return;
        subPanelBackground.sprite = sprite;
    }

    private void ValidateConfig()
    {
        if (panelConfig == null)
        {
            Debug.LogError("[HomeScreenManager] PanelConfig is not assigned!");
            return;
        }
        if (panelConfig.categories == null ||
            panelConfig.categories.Count != categoryButtons.Length)
        {
            Debug.LogError($"[HomeScreenManager] PanelConfig has " +
                           $"{panelConfig.categories?.Count ?? 0} categories but " +
                           $"{categoryButtons.Length} buttons are wired. They must match.");
        }
    }
}