#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class U1_SectionD_LayoutFixer
{
    [MenuItem("Phonics/Fix Unit 1 Section D Layout & Spacing")]
    public static void FixSectionDLayout()
    {
        // 0. Auto-generate the 44 assets with sliced U1_SEC_D card sprites
        U1_SoundWallDataAssetGenerator.GenerateDataAssets();

        // 1. Find Section D Panel
        GameObject sectionDObj = GameObject.Find("Section D Panel");
        if (sectionDObj == null) sectionDObj = GameObject.Find("Section_D");
        
        SD_SoundWallManager_Phonics_Junior mgr = Object.FindFirstObjectByType<SD_SoundWallManager_Phonics_Junior>(FindObjectsInactive.Include);
        if (sectionDObj == null && mgr != null) sectionDObj = mgr.gameObject;

        if (sectionDObj == null)
        {
            Debug.LogError("[Unit 1 Section D Fixer] Could not find 'Section D Panel'!");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(sectionDObj, "Fix Section D Table Layout");

        int uiLayer = LayerMask.NameToLayer("UI");
        
        // Ensure all parent objects up to Canvas are ACTIVE and set to UI Layer
        Transform parentCurr = sectionDObj.transform;
        while (parentCurr != null && parentCurr.gameObject.name != "Canvas")
        {
            parentCurr.gameObject.SetActive(true);
            if (uiLayer >= 0) parentCurr.gameObject.layer = uiLayer;
            parentCurr = parentCurr.parent;
        }

        // Ensure Section D Panel has normal scale and position
        RectTransform secRt = sectionDObj.GetComponent<RectTransform>();
        if (secRt != null)
        {
            secRt.localScale = Vector3.one;
            secRt.localPosition = Vector3.zero;
        }

        CanvasGroup cg = sectionDObj.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.blocksRaycasts = true;
            cg.interactable = true;
        }

        // Bind newly generated assets from Assets/Data/unit 1 to manager and reset prefab clutter
        if (mgr != null)
        {
            mgr.soundTilePrefab = null;
            string[] guids = AssetDatabase.FindAssets("t:SD_SoundTileData_Phonics_Junior", new[] { "Assets/Data/unit 1" });
            if (guids.Length > 0)
            {
                List<SD_SoundTileData_Phonics_Junior> list = new List<SD_SoundTileData_Phonics_Junior>();
                foreach (string guid in guids)
                {
                    SD_SoundTileData_Phonics_Junior asset = AssetDatabase.LoadAssetAtPath<SD_SoundTileData_Phonics_Junior>(AssetDatabase.GUIDToAssetPath(guid));
                    if (asset != null) list.Add(asset);
                }
                mgr.soundTiles = list.ToArray();
                EditorUtility.SetDirty(mgr);
            }
        }

        // Find Scroll View
        Transform scrollView = sectionDObj.transform.Find("Scroll View");
        if (scrollView == null) scrollView = sectionDObj.transform.Find("ScrollView");
        if (scrollView != null)
        {
            if (uiLayer >= 0) scrollView.gameObject.layer = uiLayer;
            RectTransform svRt = scrollView.GetComponent<RectTransform>();
            if (svRt != null)
            {
                svRt.anchorMin = Vector2.zero;
                svRt.anchorMax = Vector2.one;
                svRt.offsetMin = new Vector2(40f, 40f);
                svRt.offsetMax = new Vector2(-40f, -40f);
                svRt.localScale = Vector3.one;
                svRt.localPosition = Vector3.zero;
            }
        }

        // Find Viewport
        Transform viewport = scrollView != null ? scrollView.Find("Viewport") : sectionDObj.transform.Find("Viewport");
        if (viewport != null)
        {
            if (uiLayer >= 0) viewport.gameObject.layer = uiLayer;
            RectTransform vpRt = viewport.GetComponent<RectTransform>();
            if (vpRt != null)
            {
                vpRt.anchorMin = Vector2.zero;
                vpRt.anchorMax = Vector2.one;
                vpRt.offsetMin = Vector2.zero;
                vpRt.offsetMax = Vector2.zero;
                vpRt.localScale = Vector3.one;
                vpRt.localPosition = Vector3.zero;
            }
        }

        // Find Scroll View content
        Transform scrollContent = viewport != null ? viewport.Find("content") : sectionDObj.transform.Find("Scroll View/Viewport/content");
        if (scrollContent == null)
        {
            ScrollRect sr = sectionDObj.GetComponentInChildren<ScrollRect>(true);
            if (sr != null && sr.content != null) scrollContent = sr.content;
        }

        if (scrollContent == null)
        {
            Debug.LogError("[Unit 1 Section D Fixer] Could not find Scroll View 'content'!");
            return;
        }

        if (uiLayer >= 0) scrollContent.gameObject.layer = uiLayer;

        // Reset content RectTransform to CENTER anchor & centered position
        RectTransform cRt = scrollContent.GetComponent<RectTransform>();
        if (cRt != null)
        {
            cRt.anchorMin = new Vector2(0.5f, 0.5f);
            cRt.anchorMax = new Vector2(0.5f, 0.5f);
            cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.anchoredPosition = new Vector2(0f, -20f);
            cRt.sizeDelta = new Vector2(1400f, 900f);
            cRt.localScale = Vector3.one;
            cRt.localPosition = Vector3.zero;
        }

        // Deactivate all old scene objects inside content so ONLY the table cards render
        foreach (Transform child in scrollContent)
        {
            if (child != null && !child.name.StartsWith("Table_Cell") && !child.name.StartsWith("Blank_Cell") && !child.name.StartsWith("Blank_Space"))
            {
                child.gameObject.SetActive(false);
            }
        }

        // Remove any conflicting VerticalLayoutGroup / HorizontalLayoutGroup
        LayoutGroup oldGroup = scrollContent.GetComponent<LayoutGroup>();
        if (oldGroup != null && !(oldGroup is GridLayoutGroup))
        {
            Object.DestroyImmediate(oldGroup);
        }

        GridLayoutGroup grid = GetOrAddComponent<GridLayoutGroup>(scrollContent.gameObject);
        grid.padding = new RectOffset(10, 10, 10, 10);
        grid.cellSize = new Vector2(160f, 140f);
        grid.spacing = new Vector2(8f, 8f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.MiddleCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 8;

        ContentSizeFitter csf = GetOrAddComponent<ContentSizeFitter>(scrollContent.gameObject);
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Force rebuild of table in scene right now!
        if (mgr != null)
        {
            mgr.OpenSoundWall();
        }

        EditorUtility.SetDirty(sectionDObj);
        Debug.Log("[Unit 1 Section D] Successfully auto-generated U1_SEC_D card assets and rebuilt the 8-column table matrix in scene!");
    }

    private static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null) comp = go.AddComponent<T>();
        return comp;
    }
}
#endif
