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

    // 🔥 IMPORTANT CHANGE
    public string homeScreenId; // instead of GameObject
}