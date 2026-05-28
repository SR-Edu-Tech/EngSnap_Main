using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads AssetBundle scenes from local disk (pre-downloaded by FirstRunDownloader).
///
/// KEY CHANGES vs old version
/// ───────────────────────────
/// 1. Multi-bundle cache: ALL bundles can be resident in memory at once.
///    Switching from Beginners → Juniors is instant if both were pre-warmed.
///    The old single-bundle design forced an unload+reload on every switch.
///
/// 2. PrewarmBundle(): called by FirstRunDownloader right after download so
///    the very first Learn click requires zero disk I/O.
///
/// 3. Scene-name matching is done with OrdinalIgnoreCase against the filename
///    part only (not the full path), which is more robust.
/// </summary>
public class CachedBundleLoader : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static CachedBundleLoader Instance { get; private set; }

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action<float>  OnLoadProgress;
    public event Action         OnLoadComplete;
    public event Action<string> OnError;

    // ── State ─────────────────────────────────────────────────────────────────
    public bool  IsLoading { get; private set; }
    public float Progress  { get; private set; }

    [Header("Settings")]
    [Tooltip("Minimum time (in seconds) the loading screen should remain active to avoid flickering/jerkiness on fast cache hits.")]
    public float minLoadDuration = 40f;

    // ── Multi-bundle in-memory cache: url → AssetBundle ───────────────────────
    private readonly Dictionary<string, AssetBundle> _cache
        = new Dictionary<string, AssetBundle>();
    private readonly HashSet<string> _prewarming
        = new HashSet<string>();
    private readonly Dictionary<string, float> _prewarmProgress
        = new Dictionary<string, float>();

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Pre-load a bundle from disk into the in-memory cache without loading a scene.
    /// Called after a lesson bundle finishes downloading so the first scene
    /// load can reuse the in-memory bundle instead of hitting disk again.
    /// </summary>
    public IEnumerator PrewarmBundle(string bundleUrl, string localPath,
                                     Action<float> onProgress = null)
    {
        // Already cached — nothing to do
        if (_cache.TryGetValue(bundleUrl, out AssetBundle existing) && existing != null)
        {
            Debug.Log($"[CachedBundleLoader] Prewarm: already cached → {bundleUrl}");
            onProgress?.Invoke(1f);
            yield break;
        }

        // Unload any stale null entry
        if (_cache.ContainsKey(bundleUrl))
            _cache.Remove(bundleUrl);

        if (_prewarming.Contains(bundleUrl))
        {
            Debug.Log($"[CachedBundleLoader] Prewarm already in progress, waiting: {bundleUrl}");
            while (_prewarming.Contains(bundleUrl))
            {
                onProgress?.Invoke(GetPrewarmProgress(bundleUrl));
                yield return null;
            }

            if (_cache.TryGetValue(bundleUrl, out AssetBundle warmed) && warmed != null)
                onProgress?.Invoke(1f);

            yield break;
        }

        // Unload any Unity-level collision (same files loaded under a different key)
        UnloadStaleUnityBundles(bundleUrl, localPath);

        _prewarming.Add(bundleUrl);
        _prewarmProgress[bundleUrl] = 0f;

        try
        {
            Debug.Log($"[CachedBundleLoader] Prewarm loading from disk: {localPath}");
            AssetBundleCreateRequest req = AssetBundle.LoadFromFileAsync(localPath);

            while (!req.isDone)
            {
                _prewarmProgress[bundleUrl] = req.progress;
                onProgress?.Invoke(req.progress);
                yield return null;
            }

            if (req.assetBundle == null)
            {
                Debug.LogError($"[CachedBundleLoader] Prewarm failed (null bundle): {localPath}");
                yield break;
            }

            _cache[bundleUrl] = req.assetBundle;
            _prewarmProgress[bundleUrl] = 1f;
            onProgress?.Invoke(1f);
            Debug.Log($"[CachedBundleLoader] Prewarm complete: '{req.assetBundle.name}'  " +
                      $"Scenes: {string.Join(", ", req.assetBundle.GetAllScenePaths())}");
        }
        finally
        {
            _prewarming.Remove(bundleUrl);
            _prewarmProgress.Remove(bundleUrl);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Main entry point — load a scene from a pre-downloaded bundle.
    /// If the bundle is already in the in-memory cache (pre-warmed or reused)
    /// the scene loads immediately with no disk I/O.
    /// </summary>
    public void LoadSceneFromDisk(string bundleUrl, string sceneName,
                                  LoadSceneMode mode = LoadSceneMode.Additive)
    {
        if (IsLoading)
        {
            Debug.LogWarning("[CachedBundleLoader] Already loading — request ignored.");
            return;
        }
        StartCoroutine(LoadRoutine(bundleUrl, sceneName, mode));
    }

    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator LoadRoutine(string bundleUrl, string sceneName, LoadSceneMode mode)
    {
        float startTime = Time.time;
        IsLoading = true;
        Progress  = 0f;
        OnLoadProgress?.Invoke(0f);

        // ── 1. Verify file exists on disk (as a fallback reference) ──────────
        string localPath = FirstRunDownloader.GetLocalPath(bundleUrl);
        if (!File.Exists(localPath))
        {
            Fail($"Bundle not found on disk: {localPath}\n" +
                 "Tap Learn to download the lesson bundle first.");
            yield break;
        }

        // ── 2. Get bundle from cache or load from disk ────────────────────────
        AssetBundle bundle = null;

        if (_prewarming.Contains(bundleUrl))
        {
            Debug.Log($"[CachedBundleLoader] Waiting for prewarm to finish: {bundleUrl}");
            while (_prewarming.Contains(bundleUrl))
            {
                float actualProgress = GetPrewarmProgress(bundleUrl) * 0.5f;
                Progress = Mathf.Min(actualProgress, (Time.time - startTime) / minLoadDuration);
                OnLoadProgress?.Invoke(Progress);
                yield return null;
            }
        }

        if (_cache.TryGetValue(bundleUrl, out AssetBundle cached) && cached != null)
        {
            // ── Fast path: already in memory ──────────────────────────────────
            Debug.Log($"[CachedBundleLoader] Cache hit → instant load for: {bundleUrl}");
            bundle   = cached;
            Progress = Mathf.Min(0.5f, (Time.time - startTime) / minLoadDuration);
            OnLoadProgress?.Invoke(Progress);
        }
        else
        {
            // ── Slow path: load from disk (only if not pre-warmed) ────────────
            if (_cache.ContainsKey(bundleUrl))
                _cache.Remove(bundleUrl); // remove stale null entry

            // Safety: unload any Unity-level collision from a previous session
            UnloadStaleUnityBundles(bundleUrl, localPath);

            Debug.Log($"[CachedBundleLoader] Loading from disk: {localPath}");
            AssetBundleCreateRequest req = AssetBundle.LoadFromFileAsync(localPath);

            while (!req.isDone)
            {
                float actualProgress = req.progress * 0.5f;
                Progress = Mathf.Min(actualProgress, (Time.time - startTime) / minLoadDuration);
                OnLoadProgress?.Invoke(Progress);
                yield return null;
            }

            if (req.assetBundle == null)
            {
                Fail($"LoadFromFileAsync returned null for: {localPath}\n" +
                     "File may be corrupt. Call FirstRunDownloader.ResetCache() and relaunch.");
                yield break;
            }

            bundle              = req.assetBundle;
            _cache[bundleUrl]   = bundle;

            Debug.Log($"[CachedBundleLoader] Bundle loaded. Name='{bundle.name}'  " +
                      $"Scenes: {string.Join(", ", bundle.GetAllScenePaths())}");
        }

        // ── 3. Validate the scene exists ──────────────────────────────────────
        string[] scenePaths = bundle.GetAllScenePaths();
        bool     found      = false;

        foreach (string p in scenePaths)
        {
            // Match on filename-without-extension so the caller doesn't need to
            // include the full "Assets/Scenes/" prefix or ".unity" extension.
            string fileNameOnly = Path.GetFileNameWithoutExtension(p);
            if (string.Equals(fileNameOnly, sceneName, StringComparison.OrdinalIgnoreCase))
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            // Build a helpful list of valid names
            var validNames = new System.Text.StringBuilder();
            foreach (string p in scenePaths)
                validNames.Append(Path.GetFileNameWithoutExtension(p)).Append(", ");

            Fail($"Scene '{sceneName}' not found in bundle.\n" +
                 $"Available scenes: {string.Join(", ", scenePaths)}\n" +
                 $"Valid scene names to put in PanelConfig: {validNames}\n" +
                 $"Check that the Scene Name in PanelConfig exactly matches " +
                 $"the scene filename inside the bundle (case-sensitive on Android).");
            yield break;
        }

        // ── 4. Load the scene ─────────────────────────────────────────────────
        AsyncOperation sceneOp = SceneManager.LoadSceneAsync(sceneName, mode);

        while (!sceneOp.isDone)
        {
            float sceneProgress = Mathf.Clamp01(sceneOp.progress / 0.9f);
            float actualProgress = 0.5f + sceneProgress * 0.5f;
            Progress = Mathf.Min(actualProgress, (Time.time - startTime) / minLoadDuration);
            OnLoadProgress?.Invoke(Progress);
            yield return null;
        }

        // Enforce minimum load duration to prevent visual flicker/jerkiness on fast loads
        float elapsed = Time.time - startTime;
        if (elapsed < minLoadDuration)
        {
            float remaining = minLoadDuration - elapsed;
            float holdStartTime = Time.time;
            float startProgress = Progress;
            while (Time.time - holdStartTime < remaining)
            {
                float t = (Time.time - holdStartTime) / remaining;
                Progress = Mathf.Lerp(startProgress, 1f, t);
                OnLoadProgress?.Invoke(Progress);
                yield return null;
            }
        }

        Progress = 1f;
        OnLoadProgress?.Invoke(1f);

        Debug.Log($"[CachedBundleLoader] Load completed. Actual Load Time: {elapsed:F2}s, Enforced Min Time: {minLoadDuration:F2}s, Total Time Spent: {(Time.time - startTime):F2}s");
        Debug.Log($"[CachedBundleLoader] Scene '{sceneName}' loaded successfully.");
        OnLoadComplete?.Invoke();
        IsLoading = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Unload any AssetBundle already loaded in Unity that references the same
    /// file path, to avoid the "already loaded" collision error.
    /// We collect into a List first to avoid modifying the iterator mid-loop.
    /// </summary>
    private void UnloadStaleUnityBundles(string bundleUrl, string localPath)
    {
        // Unload AssetBundleLoader's cache too
        if (AssetBundleLoader.Instance != null)
            AssetBundleLoader.Instance.UnloadAllBundles();

        // Unload any Unity-tracked bundles not in our cache
        var stale = new List<AssetBundle>(AssetBundle.GetAllLoadedAssetBundles());
        foreach (AssetBundle ab in stale)
        {
            // Only unload if it's not one of our known-good cached bundles
            bool isCached = false;
            foreach (var kvp in _cache)
            {
                if (kvp.Value == ab) { isCached = true; break; }
            }
            if (!isCached)
            {
                Debug.Log($"[CachedBundleLoader] Unloading stale bundle: '{ab.name}'");
                ab.Unload(false);
            }
        }
    }

    private void Fail(string msg)
    {
        Debug.LogError($"[CachedBundleLoader] {msg}");
        OnError?.Invoke(msg);
        IsLoading = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public unload helpers (called by HomeScreenManager on back/logout)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Unloads one bundle from the cache and from memory.
    /// unloadAllObjects=false keeps active scene objects alive.
    /// </summary>
    public void UnloadBundle(string bundleUrl, bool unloadAllObjects = false)
    {
        if (_cache.TryGetValue(bundleUrl, out AssetBundle b) && b != null)
        {
            Debug.Log($"[CachedBundleLoader] UnloadBundle: '{bundleUrl}'");
            b.Unload(unloadAllObjects);
        }
        _cache.Remove(bundleUrl);
    }

    /// <summary>
    /// Unloads ALL cached bundles (call on logout or low-memory warning).
    /// </summary>
    public void UnloadAllBundles(bool unloadAllObjects = false)
    {
        foreach (var kvp in _cache)
        {
            if (kvp.Value != null)
            {
                Debug.Log($"[CachedBundleLoader] UnloadAllBundles: releasing '{kvp.Key}'");
                kvp.Value.Unload(unloadAllObjects);
            }
        }
        _cache.Clear();
    }

    // ─────────────────────────────────────────────────────────────────────────
    private float GetPrewarmProgress(string bundleUrl)
    {
        return _prewarmProgress.TryGetValue(bundleUrl, out float progress)
            ? Mathf.Clamp01(progress)
            : 0f;
    }

    /// <summary>Async unload of a loaded scene, then free unused assets.</summary>
    public void UnloadBundleScene(string sceneName, Action onComplete = null)
        => StartCoroutine(UnloadRoutine(sceneName, onComplete));

    private IEnumerator UnloadRoutine(string sceneName, Action onComplete)
    {
        Scene bundleScene = SceneManager.GetSceneByName(sceneName);

        if (bundleScene.isLoaded)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s.name != sceneName) { SceneManager.SetActiveScene(s); break; }
            }
            yield return SceneManager.UnloadSceneAsync(bundleScene);
        }

        yield return Resources.UnloadUnusedAssets();
        onComplete?.Invoke();
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>Disk-cache diagnostic — logs which URLs have files on disk.</summary>
    public static void LogDiskCacheStatus(IEnumerable<string> urls)
    {
        foreach (string url in urls)
        {
            string path   = FirstRunDownloader.GetLocalPath(url);
            bool   exists = File.Exists(path);
            long   size   = exists ? new FileInfo(path).Length / 1024 : 0;
            Debug.Log($"[CachedBundleLoader] {(exists ? "✓" : "✗")} " +
                      $"{Path.GetFileName(path)}  {(exists ? size + " KB" : "NOT FOUND")}");
        }
    }
}
