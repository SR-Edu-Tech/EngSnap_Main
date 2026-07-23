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
}