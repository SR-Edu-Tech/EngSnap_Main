/// <summary>
/// Lightweight static session store.
/// Holds the AssetBundle URL and Scene Name selected by the user
/// so the Learn button can access them regardless of which
/// GameObject calls it.
/// </summary>
public static class AppSession
{
    /// <summary>URL of the AssetBundle the user last selected.</summary>
    public static string PendingBundleUrl { get; set; }

    /// <summary>Scene name inside that AssetBundle to load on Learn.</summary>
    public static string PendingSceneName { get; set; }

    /// <summary>Returns true if both URL and scene name are set and non-empty.</summary>
    public static bool IsReady =>
        !string.IsNullOrEmpty(PendingBundleUrl) &&
        !string.IsNullOrEmpty(PendingSceneName);

    /// <summary>Clears stored session data.</summary>
    public static void Clear()
    {
        PendingBundleUrl = null;
        PendingSceneName = null;
    }
}
