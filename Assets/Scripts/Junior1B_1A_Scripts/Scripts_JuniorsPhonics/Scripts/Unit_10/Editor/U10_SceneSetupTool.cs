#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class U10_SceneSetupTool
{
    [MenuItem("Phonics/Setup Unit 10 Hierarchy")]
    public static void SetupUnit10Hierarchy()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Unit 10 Setup] Canvas not found in active scene!");
            return;
        }

        // 1. Root Container: Unit_10
        Transform unit10Root = null;
        Transform existingU10 = canvas.transform.Find("Unit_10");
        if (existingU10 != null && existingU10)
        {
            unit10Root = existingU10;
        }
        else
        {
            GameObject u10Obj = new GameObject("Unit_10");
            u10Obj.transform.SetParent(canvas.transform, false);
            unit10Root = u10Obj.transform;

            RectTransform rt = u10Obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            Undo.RegisterCreatedObjectUndo(u10Obj, "Create Unit_10 Root");
        }

        if (unit10Root == null || !unit10Root) return;

        // Add U10_Manager to Unit_10
        U10_Manager manager = unit10Root.GetComponent<U10_Manager>();
        if (manager == null) manager = unit10Root.gameObject.AddComponent<U10_Manager>();

        // Load Master Level Data
        Unit10LevelData masterData = AssetDatabase.LoadAssetAtPath<Unit10LevelData>("Assets/Data/unit 10/Unit10LevelData.asset");
        if (masterData != null) manager.levelData = masterData;

        // 2. Sections Container: Unit_10_Sections
        Transform sectionsRoot = unit10Root.Find("Unit_10_Sections");
        if (sectionsRoot == null)
        {
            GameObject secObj = new GameObject("Unit_10_Sections");
            secObj.transform.SetParent(unit10Root, false);
            sectionsRoot = secObj.transform;

            RectTransform rt = secObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            Undo.RegisterCreatedObjectUndo(secObj, "Create Unit_10_Sections");
        }

        if (sectionsRoot == null || !sectionsRoot) return;

        // 3. Section Selection Panels Signboard
        Transform selPanel = unit10Root.Find("Unit_10_Section_Selection_Panels");
        if (selPanel == null) selPanel = CreateSectionSelectionPanel(unit10Root);
        manager.levelSelectionPanel = selPanel.gameObject;

        // 4. Create Activity Panels inside Unit_10_Sections
        GameObject introPanel   = CreateOrGetPanel(sectionsRoot, "Intro_Demo_Panel",             typeof(U10_IntroDemoController));
        GameObject begBuilder   = CreateOrGetPanel(sectionsRoot, "Beginning_Builder_Panel_A",    typeof(U10_A1_BeginningBuilderController));
        GameObject startRight   = CreateOrGetPanel(sectionsRoot, "Start_It_Right_Panel_B",       typeof(U10_A2_StartItRightController));
        GameObject endBuilder   = CreateOrGetPanel(sectionsRoot, "Ending_Builder_Panel_C",       typeof(U10_A3_EndingBuilderController));
        GameObject finishRight  = CreateOrGetPanel(sectionsRoot, "Finish_It_Right_Panel_D",      typeof(U10_A4_FinishItRightController));
        GameObject rewardPanel  = CreateOrGetPanel(sectionsRoot, "RewardPanel",                 typeof(U10_RewardController));

        manager.introDemoPanel         = introPanel;
        manager.beginningBuilderPanel  = begBuilder;
        manager.startItRightPanel      = startRight;
        manager.endingBuilderPanel     = endBuilder;
        manager.finishItRightPanel     = finishRight;
        manager.rewardPanel            = rewardPanel;

        manager.introDemoController        = introPanel.GetComponent<U10_IntroDemoController>();
        manager.beginningBuilderController = begBuilder.GetComponent<U10_A1_BeginningBuilderController>();
        manager.startItRightController     = startRight.GetComponent<U10_A2_StartItRightController>();
        manager.endingBuilderController    = endBuilder.GetComponent<U10_A3_EndingBuilderController>();
        manager.finishItRightController    = finishRight.GetComponent<U10_A4_FinishItRightController>();
        manager.rewardController           = rewardPanel.GetComponent<U10_RewardController>();

        manager.AutoBindPanels();

        // 5. Clean persistent inspector listeners on Next buttons leftover from duplicating Unit 9
        Button[] allBtns = unit10Root.GetComponentsInChildren<Button>(true);
        foreach (Button b in allBtns)
        {
            if (b != null && b.name.Contains("Next"))
            {
                while (b.onClick.GetPersistentEventCount() > 0)
                {
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(b.onClick, 0);
                }
                UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick, manager.OnNextButtonClicked);
            }
        }

        EditorUtility.SetDirty(manager);
        EditorUtility.SetDirty(unit10Root.gameObject);
        Debug.Log("[Unit 10 Setup] Unit 10 Hierarchy fully configured under Canvas/Unit_10! Persistent Next_Button listeners reset.");
    }

    private static Transform CreateSectionSelectionPanel(Transform parent)
    {
        GameObject panelObj = new GameObject("Unit_10_Section_Selection_Panels");
        panelObj.transform.SetParent(parent, false);

        RectTransform rt = panelObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Content layout container
        GameObject contentObj = new GameObject("Content");
        contentObj.transform.SetParent(panelObj.transform, false);
        HorizontalLayoutGroup hlg = contentObj.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 30f;
        hlg.childControlWidth = hlg.childControlHeight = true;

        RectTransform crt = contentObj.GetComponent<RectTransform>();
        crt.sizeDelta = new Vector2(1400f, 600f);

        string[] cardTitles = { "Beginning Blends", "Start it Right", "Ending Blends", "Finish it Right" };
        string[] cardNames  = { "Section_A_Panel", "Section_B_Panel", "Section_C_Panel", "Section_D_Panel" };

        for (int i = 0; i < 4; i++)
        {
            GameObject card = new GameObject(cardNames[i]);
            card.transform.SetParent(contentObj.transform, false);

            Image img = card.AddComponent<Image>();
            img.color = new Color(0.95f, 0.95f, 1f, 1f);

            Button btn = card.AddComponent<Button>();

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(card.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = cardTitles[i];
            tmp.fontSize = 42;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.08f, 0.18f, 0.4f, 1f);

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
        }

        return panelObj.transform;
    }

    private static GameObject CreateOrGetPanel(Transform parent, string panelName, System.Type controllerType)
    {
        Transform t = parent.Find(panelName);
        GameObject panelObj;

        if (t == null)
        {
            panelObj = new GameObject(panelName);
            panelObj.transform.SetParent(parent, false);

            RectTransform rt = panelObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            panelObj.SetActive(false);
        }
        else
        {
            panelObj = t.gameObject;
        }

        if (controllerType != null && panelObj.GetComponent(controllerType) == null)
        {
            panelObj.AddComponent(controllerType);
        }

        return panelObj;
    }
}
#endif
