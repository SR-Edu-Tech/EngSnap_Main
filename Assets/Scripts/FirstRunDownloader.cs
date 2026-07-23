using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

/// <summary>
/// Opens the login/class-selection flow from Play, and downloads a lesson
/// bundle only when the player taps Learn for that lesson.
/// </summary>
public class FirstRunDownloader : MonoBehaviour
{
    public static FirstRunDownloader Instance { get; private set; }

    private const string PREF_BUNDLE_VERSION = "BundlesDownloadedVersion_v1";
    private const string PREF_LEGACY_DOWNLOADED = "BundlesDownloaded_v1";
    private const string PREF_TOKEN = "ACCESS_TOKEN";

    private bool _isFlowRunning;
    private Coroutine _lessonLoadRoutine;

    [Header("Config Reference")]
    [Tooltip("Reference to the PanelConfig ScriptableObject containing categories and bundle URLs.")]
    [SerializeField] private PanelConfig panelConfig;

    /// <summary>
    /// Dynamically retrieves all unique asset bundle URLs configured in PanelConfig.
    /// This removes the need to manually maintain a duplicate list in the Inspector.
    /// </summary>
    public List<string> BundleUrls
    {
        get
        {
            List<string> urls = new List<string>();
            if (panelConfig != null && panelConfig.categories != null)
            {
                foreach (var cat in panelConfig.categories)
                {
                    if (cat.subButtons != null)
                    {
                        foreach (var sub in cat.subButtons)
                        {
                            if (!string.IsNullOrEmpty(sub.assetBundleUrl) && !urls.Contains(sub.assetBundleUrl))
                            {
                                urls.Add(sub.assetBundleUrl);
                            }
                        }
                    }
                }
            }
            return urls;
        }
    }

    [Header("Download Panel")]
    public GameObject downloadPanel;
    public TextMeshProUGUI downloadingText;
    public Slider progressBar;
    public TextMeshProUGUI percentText;

    [Header("Next Panels")]
    public GameObject classSelectionPanel;
    public GameObject loginPanel;

    [Header("No Internet Popup")]
    [Tooltip("Popup panel shown when trying to download without internet.")]
    public GameObject noInternetPopup;
    [Tooltip("Retry button inside the no internet popup.")]
    public Button noInternetRetryButton;
    [Tooltip("Home button inside the no internet popup.")]
    public Button noInternetHomeButton;
    [Tooltip("Text component inside the no internet popup to display status message.")]
    public TextMeshProUGUI noInternetText;

    private enum UserChoice
    {
        Pending,
        Retry,
        GoToHomeScreen
    }
    private UserChoice _userChoice = UserChoice.Pending;

    private Coroutine _animationRoutine;
    private int _noInternetTextIndex = 0;
    private readonly string[] _noInternetSentences = new string[]
    {
        "Internet connection lost.",
        "Connect to an active internet connection and try again.",
        "Please check your Wi-Fi or mobile data network settings.",
        "Ensure your device is online before retrying."
    };

    [Header("Settings")]
    public int maxRetries = 3;
    public float completionDelay = 0.2f;

    [Tooltip("Pre-load the lesson bundle into memory right after downloading it.")]
    public bool prewarmBundles = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (noInternetRetryButton != null)
            noInternetRetryButton.onClick.AddListener(OnRetryButtonClicked);
        if (noInternetHomeButton != null)
            noInternetHomeButton.onClick.AddListener(OnNoInternetHomeClicked);

        if (noInternetPopup != null)
        {
            noInternetPopup.SetActive(false);

            if (noInternetText == null)
            {
                var texts = noInternetPopup.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in texts)
                {
                    if (noInternetRetryButton != null && t.transform.IsChildOf(noInternetRetryButton.transform))
                        continue;
                    if (noInternetHomeButton != null && t.transform.IsChildOf(noInternetHomeButton.transform))
                        continue;
                    if (t.GetComponentInParent<Button>() != null)
                        continue;

                    noInternetText = t;
                    break;
                }
            }
        }
    }

    private void StartAnimation(IEnumerator routine)
    {
        if (_animationRoutine != null)
        {
            StopCoroutine(_animationRoutine);
        }
        _animationRoutine = StartCoroutine(routine);
    }

    private IEnumerator PopInRoutine(Transform target)
    {
        target.localScale = Vector3.zero;
        float duration = 0.4f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float scaleValue;
            if (t < 0.7f)
            {
                scaleValue = Mathf.Lerp(0f, 1.15f, t / 0.7f);
            }
            else
            {
                scaleValue = Mathf.Lerp(1.15f, 1.0f, (t - 0.7f) / 0.3f);
            }
            target.localScale = new Vector3(scaleValue, scaleValue, scaleValue);
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    private IEnumerator BounceRoutine(Transform target, Action onComplete = null)
    {
        float duration = 0.3f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            float scaleValue;
            if (t < 0.3f)
            {
                scaleValue = Mathf.Lerp(1f, 0.85f, t / 0.3f);
            }
            else if (t < 0.7f)
            {
                scaleValue = Mathf.Lerp(0.85f, 1.15f, (t - 0.3f) / 0.4f);
            }
            else
            {
                scaleValue = Mathf.Lerp(1.15f, 1.0f, (t - 0.7f) / 0.3f);
            }
            target.localScale = new Vector3(scaleValue, scaleValue, scaleValue);
            yield return null;
        }
        target.localScale = Vector3.one;
        onComplete?.Invoke();
    }

    private void OnRetryButtonClicked()
    {
        if (Application.internetReachability != NetworkReachability.NotReachable)
        {
            Debug.Log("[FirstRunDownloader] Internet connection restored. Retrying download...");
            StartAnimation(BounceRoutine(noInternetPopup.transform, () =>
            {
                if (noInternetPopup != null)
                    noInternetPopup.SetActive(false);
                _userChoice = UserChoice.Retry;
            }));
        }
        else
        {
            Debug.LogWarning("[FirstRunDownloader] Retry clicked, but still no internet.");
            _noInternetTextIndex = (_noInternetTextIndex + 1) % _noInternetSentences.Length;
            if (noInternetText != null)
            {
                noInternetText.text = _noInternetSentences[_noInternetTextIndex];
            }
            StartAnimation(BounceRoutine(noInternetPopup.transform));
        }
    }

    private void OnNoInternetHomeClicked()
    {
        Debug.Log("[FirstRunDownloader] Home clicked on No Internet popup.");
        
        StartAnimation(BounceRoutine(noInternetPopup.transform, () =>
        {
            _userChoice = UserChoice.GoToHomeScreen;

            if (noInternetPopup != null)
                noInternetPopup.SetActive(false);

            HideDownloadPanel();

            if (HomeScreenManager.Instance != null)
            {
                HomeScreenManager.Instance.RestoreAfterBundle();
            }
        }));
    }

    public void StartFlow()
    {
        if (_isFlowRunning)
        {
            Debug.LogWarning("[FirstRunDownloader] StartFlow ignored because a flow is already running.");
            return;
        }

        _isFlowRunning = true;
        StartCoroutine(RunFlow());
    }

    public void DownloadBundleAndLoadScene(string bundleUrl, string sceneName)
    {
        if (string.IsNullOrEmpty(bundleUrl) || string.IsNullOrEmpty(sceneName))
        {
            NotifyLearnLoadError("Lesson details are missing. Please select the lesson again.");
            return;
        }

        if (_lessonLoadRoutine != null)
        {
            Debug.LogWarning("[FirstRunDownloader] Lesson load ignored because another lesson is already being prepared.");
            return;
        }

        _lessonLoadRoutine = StartCoroutine(DownloadBundleAndLoadSceneRoutine(bundleUrl, sceneName));
    }

    private IEnumerator RunFlow()
    {
        try
        {
            PrepareBundleCacheForCurrentVersion();
            HideDownloadPanel();
            OpenNextPanel();
        }
        finally
        {
            _isFlowRunning = false;
        }

        yield break;
    }

    private IEnumerator DownloadBundleAndLoadSceneRoutine(string bundleUrl, string sceneName)
    {
        try
        {
            PrepareBundleCacheForCurrentVersion();

            if (CachedBundleLoader.Instance == null)
            {
                NotifyLearnLoadError("Lesson loader is not ready yet. Please try again.");
                yield break;
            }

            string localPath = GetLocalPath(bundleUrl);
            bool needsDownload = !File.Exists(localPath);

            if (needsDownload)
            {
                if (Application.internetReachability == NetworkReachability.NotReachable)
                {
                    // Show "No Internet" popup
                    if (noInternetPopup != null)
                    {
                        if (noInternetText != null)
                        {
                            noInternetText.text = "Internet connection lost.";
                        }
                        noInternetPopup.SetActive(true);
                        StartAnimation(PopInRoutine(noInternetPopup.transform));
                    }
                    else
                    {
                        Debug.LogError("[FirstRunDownloader] noInternetPopup is not assigned!");
                    }

                    // Hide normal download/loading panel so only the popup shows
                    HideDownloadPanel();

                    _userChoice = UserChoice.Pending;

                    // Wait until user makes a choice (Retry or Home Screen)
                    while (_userChoice == UserChoice.Pending)
                    {
                        yield return null;
                    }

                    if (_userChoice == UserChoice.GoToHomeScreen)
                    {
                        yield break;
                    }
                }

                ShowDownloadPanel("Downloading lesson...", 0f);

                bool success = false;
                for (int attempt = 1; attempt <= maxRetries && !success; attempt++)
                {
                    yield return StartCoroutine(DownloadOne(
                        bundleUrl,
                        localPath,
                        SetProgress,
                        () => success = true));

                    if (!success && attempt < maxRetries)
                    {
                        SetText("Retrying download...");
                        yield return new WaitForSeconds(1f);
                    }
                }

                if (!success)
                {
                    HideDownloadPanel();
                    NotifyLearnLoadError("Download failed. Please check your connection and try again.");
                    yield break;
                }

                RememberCurrentVersion();

                if (prewarmBundles)
                {
                    SetText("Preparing lesson...");
                    SetProgress(0f);
                    yield return StartCoroutine(PrewarmBundle(bundleUrl, localPath));
                }

                SetProgress(1f);
                yield return new WaitForSeconds(completionDelay);
                HideDownloadPanel();
            }
            else
            {
                HideDownloadPanel();
            }

            CachedBundleLoader.Instance.LoadSceneFromDisk(bundleUrl, sceneName);
        }
        finally
        {
            _lessonLoadRoutine = null;
        }
    }

    private IEnumerator PrewarmBundle(string bundleUrl, string localPath)
    {
        if (CachedBundleLoader.Instance == null)
            yield break;

        yield return CachedBundleLoader.Instance.PrewarmBundle(
            bundleUrl,
            localPath,
            SetProgress);
    }

    private IEnumerator DownloadOne(string url, string localPath,
                                    Action<float> onProgress,
                                    Action onDone)
    {
        using (UnityWebRequest req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SendWebRequest();

            while (!req.isDone)
            {
                onProgress?.Invoke(req.downloadProgress);
                yield return null;
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[FirstRunDownloader] Download error: {req.error}  URL: {url}");
                yield break;
            }

            try
            {
                string dir = Path.GetDirectoryName(localPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllBytes(localPath, req.downloadHandler.data);
                Debug.Log($"[FirstRunDownloader] Saved: {Path.GetFileName(localPath)} ({req.downloadHandler.data.Length / 1024} KB)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[FirstRunDownloader] Disk write failed: {ex.Message}");
                yield break;
            }
        }

        onProgress?.Invoke(1f);
        onDone?.Invoke();
    }

    private void OpenNextPanel()
    {
        SetPanel(loginPanel, false);
        SetPanel(classSelectionPanel, false);

        bool isLoggedIn = !string.IsNullOrEmpty(PlayerPrefs.GetString(PREF_TOKEN, string.Empty));
        if (isLoggedIn)
        {
            Debug.Log("[FirstRunDownloader] Token found -> opening class selection.");

            if (HomeScreenManager.Instance != null)
                HomeScreenManager.Instance.ShowSelectionPanel();
            else
                SetPanel(classSelectionPanel, true);

            if (GameAuthManager.Instance != null)
                GameAuthManager.Instance.ApplySessionState();
            else
                Debug.LogWarning("[FirstRunDownloader] GameAuthManager not found - session state not applied.");
        }
        else
        {
            Debug.Log("[FirstRunDownloader] No token -> opening login.");
            SetPanel(loginPanel, true);
        }
    }

    public static string GetLocalPath(string url)
    {
        string fileName = ExtractStableFilename(url);
        return Path.Combine(Application.persistentDataPath, "bundles", fileName);
    }

    private static string ExtractStableFilename(string url)
    {
        try
        {
            Uri uri = new Uri(url);
            string query = uri.Query.TrimStart('?');

            foreach (string part in query.Split('&'))
            {
                string[] kv = part.Split('=');
                if (kv.Length == 2 &&
                    string.Equals(kv[0], "id", StringComparison.OrdinalIgnoreCase))
                {
                    return $"bundle_{Uri.UnescapeDataString(kv[1])}.bundle";
                }
            }

            string name = Path.GetFileName(uri.AbsolutePath);
            return string.IsNullOrEmpty(name) ? "bundle_unknown.bundle" : name;
        }
        catch
        {
            return $"bundle_{Mathf.Abs(url.GetHashCode())}.bundle";
        }
    }

    private void PrepareBundleCacheForCurrentVersion()
    {
        string storedVersion = PlayerPrefs.GetString(PREF_BUNDLE_VERSION, string.Empty);
        if (string.IsNullOrEmpty(storedVersion) ||
            string.Equals(storedVersion, Application.version, StringComparison.Ordinal))
        {
            return;
        }

        InvalidateCachedBundles(
            $"[FirstRunDownloader] App version changed from '{storedVersion}' to '{Application.version}'. Clearing old lesson bundles.");
    }

    private void RememberCurrentVersion()
    {
        PlayerPrefs.SetString(PREF_BUNDLE_VERSION, Application.version);
        PlayerPrefs.DeleteKey(PREF_LEGACY_DOWNLOADED);
        PlayerPrefs.Save();
    }

    public static void ResetCache()
    {
        if (CachedBundleLoader.Instance != null)
            CachedBundleLoader.Instance.UnloadAllBundles();

        if (AssetBundleLoader.Instance != null)
            AssetBundleLoader.Instance.UnloadAllBundles();

        PlayerPrefs.DeleteKey(PREF_BUNDLE_VERSION);
        PlayerPrefs.DeleteKey(PREF_LEGACY_DOWNLOADED);
        PlayerPrefs.DeleteKey("CACHED_COURSES");
        PlayerPrefs.Save();

        string dir = Path.Combine(Application.persistentDataPath, "bundles");
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
            Debug.Log("[FirstRunDownloader] Cache cleared.");
        }
    }

    private void InvalidateCachedBundles(string reason)
    {
        if (CachedBundleLoader.Instance != null)
            CachedBundleLoader.Instance.UnloadAllBundles();

        if (AssetBundleLoader.Instance != null)
            AssetBundleLoader.Instance.UnloadAllBundles();

        PlayerPrefs.DeleteKey(PREF_BUNDLE_VERSION);
        PlayerPrefs.DeleteKey(PREF_LEGACY_DOWNLOADED);
        PlayerPrefs.Save();

        string dir = Path.Combine(Application.persistentDataPath, "bundles");
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);

        Debug.Log(reason);
    }

    private void NotifyLearnLoadError(string msg)
    {
        HideDownloadPanel();

        if (HomeScreenManager.Instance != null)
            HomeScreenManager.Instance.ShowLoadError(msg);
        else
            Debug.LogError($"[FirstRunDownloader] {msg}");
    }

    private void SetPanel(GameObject panel, bool show)
    {
        if (panel == null)
            return;

        if (show)
            panel.transform.SetAsLastSibling();

        panel.SetActive(show);
    }

    private void ShowDownloadPanel(string message, float progress)
    {
        SetPanel(downloadPanel, true);
        SetText(message);
        SetProgress(progress);
    }

    private void HideDownloadPanel()
    {
        SetPanel(downloadPanel, false);
        SetText(string.Empty);
        SetProgress(0f);
    }

    private void SetText(string msg)
    {
        if (downloadingText != null)
            downloadingText.text = msg;
    }

    private void SetProgress(float value)
    {
        value = Mathf.Clamp01(value);

        if (progressBar != null)
            progressBar.value = value;

        if (percentText != null)
            percentText.text = $"{Mathf.RoundToInt(value * 100)}%";
    }
}
