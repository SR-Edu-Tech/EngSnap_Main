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
}