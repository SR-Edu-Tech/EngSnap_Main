using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton service responsible for:
///   1. Downloading an AssetBundle from a URL (with in-memory cache)
///   2. Loading a named scene out of that bundle
///   3. Reporting download + scene-load progress for UI
/// </summary>
public class AssetBundleLoader : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    public static AssetBundleLoader Instance { get; private set; }

    // ── Cache: url → loaded bundle ────────────────────────────────────────────
    private System.Collections.Generic.Dictionary<string, AssetBundle> _cache
        = new System.Collections.Generic.Dictionary<string, AssetBundle>();

    // ── Events ────────────────────────────────────────────────────────────────
    public event Action<float>  OnDownloadProgress;   // 0..1
    public event Action         OnDownloadComplete;   // fires after scene is fully loaded
    public event Action<string> OnError;

    // ── State ─────────────────────────────────────────────────────────────────
    public bool  IsLoading { get; private set; }
    public float Progress  { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>
    /// Downloads (or uses cached) bundle at <paramref name="url"/> then loads
    /// <paramref name="sceneName"/> as single or additive scene.
    /// </summary>
    public void LoadSceneFromBundle(string url, string sceneName,
                                    LoadSceneMode mode = LoadSceneMode.Single)
    {
        if (IsLoading)
        {
            Debug.LogWarning("[AssetBundleLoader] Already loading — request ignored.");
            return;
        }
        StartCoroutine(LoadRoutine(url, sceneName, mode));
    }

    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator LoadRoutine(string url, string sceneName, LoadSceneMode mode)
    {
        IsLoading = true;
        Progress  = 0f;

        // ── 1. Get the bundle (from cache or download) ────────────────────────
        AssetBundle bundle = null;

        if (_cache.TryGetValue(url, out bundle))
        {
            // FIX #3: A cached bundle can be unloaded externally via UnloadBundle().
            // If the retrieved bundle is null/invalid, remove the stale entry and
            // re-download rather than crashing on GetAllScenePaths().
            if (bundle == null)
            {
                Debug.LogWarning($"[AssetBundleLoader] Stale cache entry removed for: {url}");
                _cache.Remove(url);
                bundle = null;
            }
            else
            {
                Debug.Log($"[AssetBundleLoader] Cache hit: {url}");
                Progress = 1f;
                OnDownloadProgress?.Invoke(1f);
            }
        }

        if (bundle == null)
        {
            Debug.Log($"[AssetBundleLoader] Downloading: {url}");
            using (UnityWebRequest req = UnityWebRequestAssetBundle.GetAssetBundle(url))
            {
                req.SendWebRequest();

                while (!req.isDone)
                {
                    // Download phase: progress 0 → 1, mapped to display range 0 → 0.5
                    // so the bar doesn't appear stuck during the subsequent scene-load phase.
                    Progress = req.downloadProgress * 0.5f;
                    OnDownloadProgress?.Invoke(Progress);
                    yield return null;
                }

                if (req.result != UnityWebRequest.Result.Success)
                {
                    string err = $"Download failed: {req.error}  URL: {url}";
                    Debug.LogError($"[AssetBundleLoader] {err}");
                    OnError?.Invoke(err);
                    IsLoading = false;
                    yield break;
                }

                bundle = DownloadHandlerAssetBundle.GetContent(req);
                if (bundle == null)
                {
                    string err = "Bundle content was null after download.";
                    Debug.LogError($"[AssetBundleLoader] {err}");
                    OnError?.Invoke(err);
                    IsLoading = false;
                    yield break;
                }

                _cache[url] = bundle;
            }
        }

        // ── 2. Validate the scene exists in the bundle ────────────────────────
        string[] scenes = bundle.GetAllScenePaths();
        bool found = false;
        foreach (string path in scenes)
        {
            if (path.Contains(sceneName)) { found = true; break; }
        }

        if (!found)
        {
            string err = $"Scene '{sceneName}' not found in bundle at {url}. " +
                         $"Available: {string.Join(", ", scenes)}";
            Debug.LogError($"[AssetBundleLoader] {err}");
            OnError?.Invoke(err);
            IsLoading = false;
            yield break;
        }

        // ── 3. Load the scene ─────────────────────────────────────────────────
        AsyncOperation sceneOp = SceneManager.LoadSceneAsync(sceneName, mode);
        while (!sceneOp.isDone)
        {
            // FIX #8: SceneManager reports progress 0 → 0.9 then jumps to 1.0,
            // causing the bar to stall at 90%. Remap 0-0.9 → 0.5-1.0 so the full
            // bar represents both download (0-0.5) and scene activation (0.5-1.0).
            float sceneProgress = Mathf.Clamp01(sceneOp.progress / 0.9f);
            Progress = 0.5f + sceneProgress * 0.5f;
            OnDownloadProgress?.Invoke(Progress);
            yield return null;
        }

        // FIX #2: OnDownloadComplete was previously invoked right after the bundle
        // finished downloading, before the scene load even started. HomeScreenManager
        // used it to hide the loading overlay — which disappeared while the scene
        // was still loading. It now fires only after the scene is fully active.
        Debug.Log($"[AssetBundleLoader] Scene '{sceneName}' loaded successfully.");
        OnDownloadComplete?.Invoke();
        IsLoading = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    /// <summary>Unloads a cached bundle (call when you no longer need it).</summary>
    public void UnloadBundle(string url, bool unloadAllObjects = false)
    {
        if (_cache.TryGetValue(url, out AssetBundle b))
        {
            b.Unload(unloadAllObjects);
            _cache.Remove(url);
        }
    }
}