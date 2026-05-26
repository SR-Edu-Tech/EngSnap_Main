using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    // ── Sub-Panel ─────────────────────────────────────────────────────────────
    [Header("Sub-Panel")]
    [SerializeField] private TextMeshProUGUI subPanelHeading;
    [SerializeField] private Transform       subButtonContainer;
    [SerializeField] private Button          subButtonPrefab;
    [SerializeField] private Button          backButton;

    [SerializeField] private AudioSource splashscreenAudio;

    // ── Sub-Panel Background ──────────────────────────────────────────────────
    [Header("Sub-Panel Background")]
    [Tooltip("The single shared Image used as the sub-panel background. " +
             "Its sprite is swapped per sub-button selection.")]
    [SerializeField] private Image subPanelBackground;

    // ── Home Screen ───────────────────────────────────────────────────────────
    [Header("Home Screen")]
    [SerializeField] private Button playButton;

    [Header("User Greeting")]
    [Tooltip("Text field that shows 'Hi, <Name>!' or similar on the home screen.")]
    [SerializeField] private TextMeshProUGUI greetingLabel;

    [Tooltip("Format string for the greeting. Use {0} as the name placeholder.\n" +
             "Example: \"Hi, {0}!\" → \"Hi, Pramod!\"")]
    [SerializeField] private string greetingFormat = "Hi, {0}!";

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
[SerializeField] private Camera mainCamera; // drag your main scene camera here in Inspector

    [System.Serializable]
    public class HomeScreenEntry
    {
        public string id;
        public GameObject screen;
    }

    [SerializeField] private List<HomeScreenEntry> homeScreens;

    private Dictionary<string, GameObject> _homeScreenMap;

    // ── Runtime state ─────────────────────────────────────────────────────────
    private readonly List<Button>     _spawnedButtons = new List<Button>();
    private readonly List<GameObject> _allHomeScreens = new List<GameObject>();
    private GameObject                _activeHomeScreen = null;
    private Button                    _activeLearnButton = null;

    private static GameObject         _rememberedHomeScreen = null; // ← ADD THIS

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
        // Subscribe to AssetBundleLoader events (with retry in case it's not ready yet)
        if (AssetBundleLoader.Instance != null)
            SubscribeToLoader();
        else
            StartCoroutine(WaitForLoader());

        // Show the username from AppSession (set by GameAuthManager after login)
        RefreshGreeting();
    }

    private IEnumerator WaitForLoader()
    {
        float timeout = 5f;
        while (AssetBundleLoader.Instance == null && timeout > 0f)
        {
            timeout -= UnityEngine.Time.deltaTime;
            yield return null;
        }

        if (AssetBundleLoader.Instance != null)
            SubscribeToLoader();
        else
            Debug.LogError("[HomeScreenManager] AssetBundleLoader not found after waiting 5 s.");
    }

    private void SubscribeToLoader()
    {
        AssetBundleLoader.Instance.OnDownloadProgress += HandleProgress;
        AssetBundleLoader.Instance.OnDownloadComplete += HandleComplete;
        AssetBundleLoader.Instance.OnError            += HandleError;
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
    //  Greeting
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Updates the greeting label from AppSession.UserName.
    /// Call this from GameAuthManager right after login, or let Start() call it.
    /// </summary>
    public void RefreshGreeting()
    {
        if (greetingLabel == null) return;

        string name = AppSession.UserName;
        if (!string.IsNullOrEmpty(name))
            greetingLabel.text = string.Format(greetingFormat, name);
        else
            greetingLabel.text = string.Empty;   // hide until name is known
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Screen navigation
    // ─────────────────────────────────────────────────────────────────────────

    private void OnPlay()   => ShowScreen(selectionPanel);

    private void OnCategoryClicked(int index)
    {
        CategoryData data = panelConfig.categories[index];
        BuildSubPanel(data);
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
                if (btnImage != null)
                    btnImage.sprite = subData.buttonSprite;
                else
                    Debug.LogWarning($"[HomeScreenManager] Sub-button prefab has no Image on root for '{subData.buttonLabel}'.");
            }

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

        AppSession.PendingBundleUrl = data.assetBundleUrl;
        AppSession.PendingSceneName = data.sceneName;

        Debug.Log($"[HomeScreen] Selected → URL: {data.assetBundleUrl} | Scene: {data.sceneName}");

        HideActiveHomeScreen();
        homeScreen.SetActive(false);

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
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void SetSubPanelBackground(Sprite sprite)
    {
        if (subPanelBackground == null)
        {
            Debug.LogWarning("[HomeScreenManager] subPanelBackground is not assigned.");
            return;
        }
        if (sprite == null)
        {
            Debug.LogWarning("[HomeScreenManager] No backgroundSprite assigned for this sub-button.");
            return;
        }
        subPanelBackground.sprite = sprite;
    }

// In OnLearnClicked — remove the camera disable from here
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

    _activeLearnButton = sourceButton;
    if (_activeLearnButton != null) _activeLearnButton.interactable = false;

    _rememberedHomeScreen = _activeHomeScreen;

    // ❌ REMOVE: if (mainCamera != null) mainCamera.gameObject.SetActive(false);
    // Camera stays ON during loading so the overlay is visible

    ShowLoadingOverlay(true);
    AssetBundleLoader.Instance.LoadSceneFromBundle(url, scene);
}
// ── Called by MainSceneReceiver when back button is pressed in bundle scene ──
public void RestoreAfterBundle()
{
    Debug.Log($"[HomeScreenManager] RestoreAfterBundle called. " +
              $"Remembered: {(_rememberedHomeScreen != null ? _rememberedHomeScreen.name : "NULL")}");

    if (mainCamera != null) mainCamera.gameObject.SetActive(true); // ← ADD THIS

    if (_rememberedHomeScreen != null)
    {
        _rememberedHomeScreen.SetActive(true);
        _activeHomeScreen     = _rememberedHomeScreen;
        _rememberedHomeScreen = null;
        Debug.Log("[HomeScreenManager] Home screen restored successfully.");
    }
    else
    {
        Debug.LogWarning("[HomeScreenManager] _rememberedHomeScreen was null, showing default.");
        homeScreen.SetActive(true);
    }

    ShowLoadingOverlay(false);
    ReEnableLearnButton();
}
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
        if (!show && errorLabel) errorLabel.gameObject.SetActive(false);
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