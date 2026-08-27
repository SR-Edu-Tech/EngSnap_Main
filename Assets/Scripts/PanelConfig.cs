using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PanelConfig", menuName = "HomeScreen/Panel Config")]
public class PanelConfig : ScriptableObject
{
    public List<CategoryData> categories = new List<CategoryData>();
}

[System.Serializable]
public class CategoryData
{
    public string buttonLabel;
    public string panelHeading;
    public List<SubButtonData> subButtons;
}

[System.Serializable]
public class SubButtonData
{
    public string buttonLabel;

    [Header("AssetBundle URL (per platform)")]
    [Tooltip("Used when building/running on Android. Falls back to Legacy URL below if left empty.")]
    public string assetBundleUrlAndroid;

    [Tooltip("Used when building/running on iOS. Falls back to Legacy URL below if left empty.")]
    public string assetBundleUrlIOS;

    [Tooltip("Used when building/running on Windows (Standalone). Falls back to Legacy URL below if left empty.")]
    public string assetBundleUrlWindows;

    [Tooltip("Old single-field URL. Kept for backward compatibility with existing configs, and " +
             "used as a fallback for any platform whose field above is left empty.")]
    public string assetBundleUrl;

    public string sceneName;
    public string homeScreenId;

    [Tooltip("Background sprite shown when this sub-button is selected")]
    public Sprite backgroundSprite;   // swapped on the shared BG Image

    [Tooltip("Sprite shown on the button itself (the Button's own Image)")]
    public Sprite buttonSprite;       // ← NEW: set directly on the instantiated button

    [Header("Lock State")]
    [Tooltip("Must match the 'id' field returned by the /student-courses-with-lock " +
             "API for this exact level (e.g. Beginners Level 1 = 12, Level 2 = 13). " +
             "Set to 0 if this sub-button should never be API-locked.")]
    public int courseId;

    [Tooltip("Force this button locked regardless of what the API says. " +
             "Lets you manually disable a level without touching the backend.")]
    public bool manuallyLocked;

    /// <summary>
    /// Returns the AssetBundle URL for whatever platform this build is currently running on.
    /// Falls back to the legacy single "assetBundleUrl" field if the platform-specific
    /// field is empty, so older configs keep working without edits.
    /// </summary>
    public string GetAssetBundleUrl()
    {
        string platformUrl;

        switch (Application.platform)
        {
            case RuntimePlatform.Android:
                platformUrl = assetBundleUrlAndroid;
                break;

            case RuntimePlatform.IPhonePlayer:
                platformUrl = assetBundleUrlIOS;
                break;

            case RuntimePlatform.WindowsPlayer:
            case RuntimePlatform.WindowsEditor:
                platformUrl = assetBundleUrlWindows;
                break;

            default:
                platformUrl = null;
                break;
        }

        if (!string.IsNullOrEmpty(platformUrl))
            return platformUrl;

        // Fallback for platforms without a dedicated field (e.g. macOS/Linux editor),
        // or if the platform-specific field was left blank.
        return assetBundleUrl;
    }
}