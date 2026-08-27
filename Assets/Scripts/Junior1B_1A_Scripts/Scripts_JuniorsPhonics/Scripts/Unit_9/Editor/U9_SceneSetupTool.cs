#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class U9_SceneSetupTool
{
    [MenuItem("Phonics/Setup Unit 9 Hierarchy & Panels")]
    public static void SetupUnit9Scene()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Unit 9 Setup] No Canvas found in scene! Please open your main game scene with a Canvas.");
            return;
        }

        // 1. Find or create Unit_9 root
        Transform unit9Root = null;
        Transform existingU9 = canvas.transform.Find("Unit_9");
        if (existingU9 != null && existingU9)
        {
            unit9Root = existingU9;
        }
        else
        {
            GameObject obj = new GameObject("Unit_9");
            obj.transform.SetParent(canvas.transform, false);
            unit9Root = obj.transform;
            RectTransform rt = obj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            Undo.RegisterCreatedObjectUndo(obj, "Create Unit_9 Root");
        }

        if (unit9Root == null || !unit9Root) return;

        // Attach U9_Manager to Unit_9 root
        U9_Manager manager = unit9Root.GetComponent<U9_Manager>();
        if (manager == null) manager = unit9Root.gameObject.AddComponent<U9_Manager>();

        // Load Master Level Data asset
        Unit9LevelData levelData = AssetDatabase.LoadAssetAtPath<Unit9LevelData>("Assets/Data/unit 9/Unit9Level_Main.asset");
        if (levelData != null) manager.levelData = levelData;

        // 2. Find or create Unit_9_Sections container
        Transform sections = null;
        Transform existingSecs = unit9Root.Find("Unit_9_Sections");
        if (existingSecs != null && existingSecs)
        {
            sections = existingSecs;
        }
        else
        {
            GameObject sObj = new GameObject("Unit_9_Sections");
            sObj.transform.SetParent(unit9Root, false);
            sections = sObj.transform;
            RectTransform rt = sObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            Undo.RegisterCreatedObjectUndo(sObj, "Create Unit_9_Sections");
        }

        if (sections == null || !sections) return;

        // 3. Build/Setup 5 Full Activity Panels
        GameObject chartPanel  = SetupChartPanel(sections);
        GameObject blendPanel  = SetupBlendPanel(sections);
        GameObject huntPanel   = SetupHuntPanel(sections);
        GameObject pickPanel   = SetupPickPanel(sections);
        GameObject rewardPanel = SetupRewardPanel(sections);

        // Wire references into U9_Manager
        if (chartPanel != null)
        {
            manager.introChartPanel = chartPanel;
            manager.introChartController = chartPanel.GetComponent<U9_IntroChartController>();
        }
        if (blendPanel != null)
        {
            manager.arrowBlendPanel = blendPanel;
            manager.arrowBlendController = blendPanel.GetComponent<U9_A1_ArrowBlendController>();
        }
        if (huntPanel != null)
        {
            manager.digraphHuntPanel = huntPanel;
            manager.digraphHuntController = huntPanel.GetComponent<U9_A2_DigraphHuntController>();
        }
        if (pickPanel != null)
        {
            manager.pickDigraphPanel = pickPanel;
            manager.pickDigraphController = pickPanel.GetComponent<U9_A3_PickDigraphController>();
        }
        if (rewardPanel != null)
        {
            manager.rewardPanel = rewardPanel;
            manager.rewardController = rewardPanel.GetComponent<U9_RewardController>();
        }

        // 4. Create Next & Back navigation buttons
        SetupNavigationButtons(unit9Root, manager);

        manager.AutoBindPanels();
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log("[Unit 9 Setup] Complete Unit 9 Hierarchy & Panels generated successfully in scene!");
    }

    private static GameObject SetupChartPanel(Transform parent)
    {
        GameObject panel = CreatePanelObject(parent, "Intro_Chart_Panel");
        if (panel == null) return null;

        U9_IntroChartController ctrl = panel.GetComponent<U9_IntroChartController>();
        if (ctrl == null) ctrl = panel.AddComponent<U9_IntroChartController>();

        CreateTitleText(panel.transform, "Consonant Digraph Chart (Page 32)");

        Transform grid = panel.transform.Find("GridContainer");
        if (grid == null || !grid)
        {
            GameObject gObj = new GameObject("GridContainer");
            gObj.transform.SetParent(panel.transform, false);
            grid = gObj.transform;
            RectTransform rt = gObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.12f);
            rt.anchorMax = new Vector2(0.85f, 0.78f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            // 2 Columns layout (4 Left, 4 Right) matching Page 32 textbook
            GridLayoutGroup glg = gObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(340f, 110f);
            glg.spacing = new Vector2(40f, 15f);
            glg.childAlignment = TextAnchor.MiddleCenter;
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 2; // 2 Columns (4 Left, 4 Right)
            Undo.RegisterCreatedObjectUndo(gObj, "Create GridContainer");
        }
        else
        {
            GridLayoutGroup glg = grid.GetComponent<GridLayoutGroup>();
            if (glg != null)
            {
                glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                glg.constraintCount = 2; // 2 Columns (4 Left, 4 Right)
            }
        }
        ctrl.gridContainer = grid;
        return panel;
    }

    private static GameObject SetupBlendPanel(Transform parent)
    {
        GameObject panel = CreatePanelObject(parent, "Arrow_Blend_Panel");
        if (panel == null) return null;

        U9_A1_ArrowBlendController ctrl = panel.GetComponent<U9_A1_ArrowBlendController>();
        if (ctrl == null) ctrl = panel.AddComponent<U9_A1_ArrowBlendController>();

        CreateTitleText(panel.transform, "Build the Word — Arrow Blend");

        Transform row = panel.transform.Find("ChunkContainer");
        if (row == null || !row)
        {
            GameObject rObj = new GameObject("ChunkContainer");
            rObj.transform.SetParent(panel.transform, false);
            row = rObj.transform;
            RectTransform rt = rObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.45f);
            rt.anchorMax = new Vector2(0.9f, 0.7f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup hlg = rObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            Undo.RegisterCreatedObjectUndo(rObj, "Create ChunkContainer");
        }
        ctrl.chunkContainer = row;

        Transform blendBtnT = panel.transform.Find("BlendButton");
        if (blendBtnT == null || !blendBtnT)
        {
            GameObject bObj = new GameObject("BlendButton");
            bObj.transform.SetParent(panel.transform, false);
            Image img = bObj.AddComponent<Image>();
            img.color = new Color(1f, 0.6f, 0.2f, 1f);
            Button btn = bObj.AddComponent<Button>();

            RectTransform rt = bObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.25f);
            rt.anchorMax = new Vector2(0.5f, 0.25f);
            rt.sizeDelta = new Vector2(220f, 70f);

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(bObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "BLEND! ✨";
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            ctrl.blendButton = btn;
            Undo.RegisterCreatedObjectUndo(bObj, "Create BlendButton");
        }
        else
        {
            ctrl.blendButton = blendBtnT.GetComponent<Button>();
        }

        Transform resT = panel.transform.Find("ResultWordText");
        if (resT == null || !resT)
        {
            GameObject resObj = new GameObject("ResultWordText");
            resObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI tmp = resObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "";
            tmp.fontSize = 52;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            RectTransform rt = resObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.12f);
            rt.anchorMax = new Vector2(0.5f, 0.12f);
            rt.sizeDelta = new Vector2(500f, 80f);
            ctrl.resultWordText = tmp;
            Undo.RegisterCreatedObjectUndo(resObj, "Create ResultWordText");
        }
        else
        {
            ctrl.resultWordText = resT.GetComponent<TextMeshProUGUI>();
        }

        return panel;
    }

    private static GameObject SetupHuntPanel(Transform parent)
    {
        GameObject panel = CreatePanelObject(parent, "Digraph_Hunt_Panel");
        if (panel == null) return null;

        U9_A2_DigraphHuntController ctrl = panel.GetComponent<U9_A2_DigraphHuntController>();
        if (ctrl == null) ctrl = panel.AddComponent<U9_A2_DigraphHuntController>();

        CreateTitleText(panel.transform, "Digraph Hunt — Spot the Digraph!");

        Transform wordRow = panel.transform.Find("WordContainer");
        if (wordRow == null || !wordRow)
        {
            GameObject wObj = new GameObject("WordContainer");
            wObj.transform.SetParent(panel.transform, false);
            wordRow = wObj.transform;
            RectTransform rt = wObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.35f);
            rt.anchorMax = new Vector2(0.9f, 0.65f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup hlg = wObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            Undo.RegisterCreatedObjectUndo(wObj, "Create WordContainer");
        }
        ctrl.wordContainer = wordRow;

        return panel;
    }

    private static GameObject SetupPickPanel(Transform parent)
    {
        GameObject panel = CreatePanelObject(parent, "Pick_Digraph_Panel");
        if (panel == null) return null;

        U9_A3_PickDigraphController ctrl = panel.GetComponent<U9_A3_PickDigraphController>();
        if (ctrl == null) ctrl = panel.AddComponent<U9_A3_PickDigraphController>();

        CreateTitleText(panel.transform, "Pick the Digraph (Page 41)");

        Transform picT = panel.transform.Find("PictureDisplayImage");
        if (picT == null || !picT)
        {
            GameObject picObj = new GameObject("PictureDisplayImage");
            picObj.transform.SetParent(panel.transform, false);
            Image img = picObj.AddComponent<Image>();
            RectTransform rt = picObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.65f);
            rt.anchorMax = new Vector2(0.5f, 0.65f);
            rt.sizeDelta = new Vector2(180f, 180f);
            ctrl.pictureDisplayImage = img;
            Undo.RegisterCreatedObjectUndo(picObj, "Create PictureDisplayImage");
        }
        else
        {
            ctrl.pictureDisplayImage = picT.GetComponent<Image>();
        }

        Transform incT = panel.transform.Find("IncompleteWordText");
        if (incT == null || !incT)
        {
            GameObject incObj = new GameObject("IncompleteWordText");
            incObj.transform.SetParent(panel.transform, false);
            TextMeshProUGUI tmp = incObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "___eese";
            tmp.fontSize = 54;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            RectTransform rt = incObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.42f);
            rt.anchorMax = new Vector2(0.5f, 0.42f);
            rt.sizeDelta = new Vector2(500f, 80f);
            ctrl.incompleteWordText = tmp;
            Undo.RegisterCreatedObjectUndo(incObj, "Create IncompleteWordText");
        }
        else
        {
            ctrl.incompleteWordText = incT.GetComponent<TextMeshProUGUI>();
        }

        Transform tray = panel.transform.Find("DigraphTrayContainer");
        if (tray == null || !tray)
        {
            GameObject tObj = new GameObject("DigraphTrayContainer");
            tObj.transform.SetParent(panel.transform, false);
            tray = tObj.transform;
            RectTransform rt = tObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.15f, 0.12f);
            rt.anchorMax = new Vector2(0.85f, 0.3f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            HorizontalLayoutGroup hlg = tObj.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 25f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            Undo.RegisterCreatedObjectUndo(tObj, "Create DigraphTrayContainer");
        }
        ctrl.digraphTrayContainer = tray;

        return panel;
    }

    private static GameObject SetupRewardPanel(Transform parent)
    {
        GameObject panel = CreatePanelObject(parent, "RewardPanel");
        if (panel == null) return null;

        U9_RewardController ctrl = panel.GetComponent<U9_RewardController>();
        if (ctrl == null) ctrl = panel.AddComponent<U9_RewardController>();

        CreateTitleText(panel.transform, "DIGRAPH DETECTIVE! 🏆");

        Transform tT = panel.transform.Find("TrophyIcon");
        if (tT == null || !tT)
        {
            GameObject tObj = new GameObject("TrophyIcon");
            tObj.transform.SetParent(panel.transform, false);
            Image img = tObj.AddComponent<Image>();
            RectTransform rt = tObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.55f);
            rt.anchorMax = new Vector2(0.5f, 0.55f);
            rt.sizeDelta = new Vector2(240f, 240f);
            ctrl.trophyIcon = img;
            Undo.RegisterCreatedObjectUndo(tObj, "Create TrophyIcon");
        }
        else
        {
            ctrl.trophyIcon = tT.GetComponent<Image>();
        }

        Transform btnT = panel.transform.Find("ContinueButton");
        if (btnT == null || !btnT)
        {
            GameObject cObj = new GameObject("ContinueButton");
            cObj.transform.SetParent(panel.transform, false);
            Image img = cObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.8f, 0.3f, 1f);
            Button btn = cObj.AddComponent<Button>();

            RectTransform rt = cObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.2f);
            rt.anchorMax = new Vector2(0.5f, 0.2f);
            rt.sizeDelta = new Vector2(220f, 70f);

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(cObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "CONTINUE ➔";
            tmp.fontSize = 26;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            ctrl.continueButton = btn;
            Undo.RegisterCreatedObjectUndo(cObj, "Create ContinueButton");
        }
        else
        {
            ctrl.continueButton = btnT.GetComponent<Button>();
        }

        return panel;
    }

    private static GameObject CreatePanelObject(Transform parent, string name)
    {
        if (parent == null || !parent) return null;

        Transform child = parent.Find(name);
        GameObject panelObj;
        if (child == null || !child)
        {
            panelObj = new GameObject(name);
            panelObj.transform.SetParent(parent, false);
            RectTransform rt = panelObj.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            Undo.RegisterCreatedObjectUndo(panelObj, $"Create {name}");
        }
        else
        {
            panelObj = child.gameObject;
        }
        return panelObj;
    }

    private static void CreateTitleText(Transform parent, string titleText)
    {
        if (parent == null || !parent) return;

        Transform titleT = parent.Find("TitleText");
        if (titleT == null || !titleT)
        {
            GameObject obj = new GameObject("TitleText");
            obj.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = titleText;
            tmp.fontSize = 38;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.12f, 0.12f, 0.3f, 1f);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.88f);
            rt.anchorMax = new Vector2(0.5f, 0.88f);
            rt.sizeDelta = new Vector2(700f, 60f);
            Undo.RegisterCreatedObjectUndo(obj, "Create TitleText");
        }
    }

    private static void SetupNavigationButtons(Transform root, U9_Manager manager)
    {
        if (root == null || !root) return;

        Transform nextT = root.Find("Next_Button");
        if (nextT == null || !nextT)
        {
            GameObject nObj = new GameObject("Next_Button");
            nObj.transform.SetParent(root, false);
            Image img = nObj.AddComponent<Image>();
            img.color = new Color(0.2f, 0.8f, 0.3f, 1f);
            Button btn = nObj.AddComponent<Button>();

            RectTransform rt = nObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0f); rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot     = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-40f, 40f);
            rt.sizeDelta = new Vector2(120f, 120f);

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(nObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "NEXT ➔";
            tmp.fontSize = 24;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            manager.nextButton = btn;
            Undo.RegisterCreatedObjectUndo(nObj, "Create Next_Button");
        }

        Transform backT = root.Find("Back_Button");
        if (backT == null || !backT)
        {
            GameObject bObj = new GameObject("Back_Button");
            bObj.transform.SetParent(root, false);
            Image img = bObj.AddComponent<Image>();
            img.color = new Color(0.9f, 0.4f, 0.3f, 1f);
            Button btn = bObj.AddComponent<Button>();

            RectTransform rt = bObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(40f, -40f);
            rt.sizeDelta = new Vector2(80f, 80f);

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(bObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "✖";
            tmp.fontSize = 32;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            manager.backButton = btn;
            Undo.RegisterCreatedObjectUndo(bObj, "Create Back_Button");
        }
    }
}
#endif
