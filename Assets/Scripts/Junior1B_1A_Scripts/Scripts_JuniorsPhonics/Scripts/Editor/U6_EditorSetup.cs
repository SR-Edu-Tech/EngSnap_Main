#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class U6_EditorSetup : EditorWindow
{
    [MenuItem("Phonics/Setup Unit 6 Scene & UI")]
    public static void SetupUnit6UI()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Unit 6 Setup] No Canvas found in the current scene!");
            return;
        }

        // 1. Find or create top-level Unit_6 GameObject under Canvas
        Transform unit6Trans = canvas.transform.Find("Unit_6");
        if (unit6Trans == null)
        {
            GameObject u6Obj = new GameObject("Unit_6", typeof(RectTransform));
            u6Obj.transform.SetParent(canvas.transform, false);
            unit6Trans = u6Obj.transform;

            RectTransform rt = u6Obj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        GameObject unit6Parent = unit6Trans.gameObject;
        U6_Manager manager = unit6Parent.GetComponent<U6_Manager>();
        if (manager == null) manager = unit6Parent.AddComponent<U6_Manager>();

        // Load Level Assets
        manager.levelLongA = AssetDatabase.LoadAssetAtPath<U6_LevelData>("Assets/Data/Unit6/Levels/Level_Long_A_teams.asset");
        manager.levelLongE = AssetDatabase.LoadAssetAtPath<U6_LevelData>("Assets/Data/Unit6/Levels/Level_Long_E_teams.asset");

        // 2. Setup Level Selection Panel
        GameObject selPanel = GetOrCreateChildPanel(unit6Trans, "Level_Selection_Panel");
        manager.levelSelectionPanel = selPanel;

        // 3. Setup Gallery Panel
        GameObject galPanel = GetOrCreateChildPanel(unit6Trans, "Gallery_Panel");
        manager.galleryPanel = galPanel;
        if (galPanel.GetComponent<U6_GalleryController>() == null) galPanel.AddComponent<U6_GalleryController>();
        manager.galleryController = galPanel.GetComponent<U6_GalleryController>();

        // 4. Setup Activity 1 Panel
        GameObject act1Panel = GetOrCreateChildPanel(unit6Trans, "Activity_1_Panel");
        manager.activity1Panel = act1Panel;
        U6_A1_MeetTeamsController a1Controller = act1Panel.GetComponent<U6_A1_MeetTeamsController>();
        if (a1Controller == null) a1Controller = act1Panel.AddComponent<U6_A1_MeetTeamsController>();
        manager.a1Controller = a1Controller;
        a1Controller.manager = manager;

        Transform col1 = act1Panel.transform.Find("Column1");
        if (col1 == null) col1 = act1Panel.transform.Find("Columns container/Col_1/Column_1");
        if (col1 == null) col1 = CreateColumn(act1Panel.transform, "Column1", "a_e");
        a1Controller.column1Container = col1;

        // Search for heading tabs — check direct children, then whole scene
        Transform t1 = FindInHierarchy(unit6Trans, "heading a_e");
        Transform t2 = FindInHierarchy(unit6Trans, "heading ai");
        Transform t3 = FindInHierarchy(unit6Trans, "heading ay");
        if (t1 != null) { a1Controller.tab1PillObject = t1.gameObject; Debug.Log("[Unit 6 Setup] Found 'heading a_e' at: " + GetPath(t1)); }
        else Debug.LogWarning("[Unit 6 Setup] Could not find 'heading a_e' in scene hierarchy!");
        if (t2 != null) { a1Controller.tab2PillObject = t2.gameObject; Debug.Log("[Unit 6 Setup] Found 'heading ai' at: " + GetPath(t2)); }
        else Debug.LogWarning("[Unit 6 Setup] Could not find 'heading ai' in scene hierarchy!");
        if (t3 != null) { a1Controller.tab3PillObject = t3.gameObject; Debug.Log("[Unit 6 Setup] Found 'heading ay' at: " + GetPath(t3)); }
        else Debug.LogWarning("[Unit 6 Setup] Could not find 'heading ay' in scene hierarchy!");

        // 5. Setup Activity 2 Panel
        GameObject act2Panel = GetOrCreateChildPanel(unit6Trans, "Activity_2_Panel");
        manager.activity2Panel = act2Panel;
        U6_A2_PictureMatchController a2Controller = act2Panel.GetComponent<U6_A2_PictureMatchController>();
        if (a2Controller == null) a2Controller = act2Panel.AddComponent<U6_A2_PictureMatchController>();
        manager.a2Controller = a2Controller;
        a2Controller.manager = manager;

        // 6. Setup Activity 3 Panel
        GameObject act3Panel = GetOrCreateChildPanel(unit6Trans, "Activity_3_Panel");
        manager.activity3Panel = act3Panel;
        U6_A3_TeamSortController a3Controller = act3Panel.GetComponent<U6_A3_TeamSortController>();
        if (a3Controller == null) a3Controller = act3Panel.AddComponent<U6_A3_TeamSortController>();
        manager.a3Controller = a3Controller;
        a3Controller.manager = manager;

        // 7. Setup Reward Panel
        GameObject rwdPanel = GetOrCreateChildPanel(unit6Trans, "Reward_Panel");
        manager.rewardPanel = rwdPanel;
        U6_RewardController rwdController = rwdPanel.GetComponent<U6_RewardController>();
        if (rwdController == null) rwdController = rwdPanel.AddComponent<U6_RewardController>();
        manager.rewardController = rwdController;
        rwdController.manager = manager;

        EditorUtility.SetDirty(unit6Parent);
        EditorUtility.SetDirty(manager);
        Undo.RegisterCreatedObjectUndo(unit6Parent, "Setup Unit 6 UI");

        Debug.Log("[Unit 6 Setup] Successfully generated and configured Unit 6 UI GameObjects & Manager in Scene!");
    }

    /// <summary>Recursively finds a child Transform by name anywhere in the hierarchy.</summary>
    private static Transform FindInHierarchy(Transform root, string targetName)
    {
        if (root == null) return null;
        if (root.name == targetName) return root;
        foreach (Transform child in root)
        {
            Transform found = FindInHierarchy(child, targetName);
            if (found != null) return found;
        }
        return null;
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        Transform p = t.parent;
        while (p != null) { path = p.name + "/" + path; p = p.parent; }
        return path;
    }

    private static GameObject GetOrCreateChildPanel(Transform parent, string name)
    {
        Transform t = parent.Find(name);
        if (t != null) return t.gameObject;

        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        panel.SetActive(false);
        return panel;
    }

    private static Transform CreateColumn(Transform parent, string colName, string headerText)
    {
        GameObject colObj = new GameObject(colName, typeof(RectTransform), typeof(VerticalLayoutGroup));
        colObj.transform.SetParent(parent, false);

        GameObject headerObj = new GameObject("Header", typeof(RectTransform), typeof(TextMeshProUGUI));
        headerObj.transform.SetParent(colObj.transform, false);
        TextMeshProUGUI tmp = headerObj.GetComponent<TextMeshProUGUI>();
        tmp.text = headerText;
        tmp.fontSize = 32;
        tmp.alignment = TextAlignmentOptions.Center;

        return colObj.transform;
    }
}
#endif
