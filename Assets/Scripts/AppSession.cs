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


    // ── User Profile ──────────────────────────────────────────────────────────
 
    /// <summary>
    /// Display name of the logged-in student.
    /// Populated from the login API response field "user_name".
    /// </summary>
    public static string UserName { get; set; }
 
    /// <summary>Clears stored session data (call on logout).</summary>
    public static void Clear()
    {
        PendingBundleUrl = null;
        PendingSceneName = null;
        // NOTE: UserName is intentionally NOT cleared here so the home screen
        // can still display the name while the sub-panel is open.
        // Call ClearAll() on logout instead.
    }
 
    /// <summary>Clears everything including user profile (call on logout).</summary>
    public static void ClearAll()
    {
        PendingBundleUrl = null;
        PendingSceneName = null;
        UserName         = null;
    }
}
