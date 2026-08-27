using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Master Manager for Unit 10 — Consonant Blends (Pages 42–48).
/// Manages complete step-by-step game flow & rewards:
/// Intro Demo (Blend vs Digraph) -> Section A (Beginning Builder) -> Section B (Start it Right Game) ->
/// Section C (Ending Builder) -> Section D (Finish it Right Game) ->
/// Grand Finale Reading Star Trophy 🏆 + All 10 Units Badge Recap Wall!
/// </summary>
public class U10_Manager : MonoBehaviour
{
    [Header("Level Data")]
    public Unit10LevelData levelData;

    [Header("UI Panels")]
    public GameObject levelSelectionPanel;      // Unit_10_Section_Selection_Panels
    public GameObject introDemoPanel;           // Intro_Demo_Panel or Intro_Chart_Panel
    public GameObject beginningBuilderPanel;     // Beginning_Builder_Panel_A or Arrow_Blend_Panel_A
    public GameObject startItRightPanel;        // Start_It_Right_Panel_B or Digraph_Hunt_Panel_B
    public GameObject endingBuilderPanel;        // Ending_Builder_Panel_C or Pick_Digraph_Panel_C
    public GameObject finishItRightPanel;       // Finish_It_Right_Panel_D
    public GameObject rewardPanel;              // RewardPanel (Badges & Grand Trophy)

    [Header("Controllers")]
    public U10_IntroDemoController introDemoController;
    public U10_A1_BeginningBuilderController beginningBuilderController;
    public U10_A2_StartItRightController startItRightController;
    public U10_A3_EndingBuilderController endingBuilderController;
    public U10_A4_FinishItRightController finishItRightController;
    public U10_RewardController rewardController;

    [Header("Navigation Buttons")]
    public Button backButton;
    public Button nextButton;

    // Flow State: 0 = Intro Demo, 1 = Section A, 2 = Section B, 3 = Section C, 4 = Section D, 5 = Grand Finale Reward, -1 = Map
    private int currentFlowIndex = -1;
    private List<Button> allNextButtons = new List<Button>();

    private void Awake()
    {
        AutoBindPanels();
    }

    private void Start()
    {
        AutoBindPanels();
    }

    private void Update()
    {
        // Enforce: Back button MUST stay visible at all times!
        if (backButton != null && !backButton.gameObject.activeSelf && gameObject.activeInHierarchy)
        {
            backButton.gameObject.SetActive(true);
        }
    }

    public void AutoBindPanels()
    {
        Transform unitRoot = transform;
        while (unitRoot.parent != null && unitRoot.parent.name != "Canvas")
            unitRoot = unitRoot.parent;

        Transform sections = unitRoot.Find("Unit_10_Sections");
        if (sections == null) sections = unitRoot.Find("Unit_9_Sections");
        if (sections == null) sections = transform.Find("Unit_10_Sections");

        // 1. Recursive scan for Back Button across all buttons in hierarchy
        if (backButton == null)
        {
            Button[] allBtns = unitRoot.GetComponentsInChildren<Button>(true);
            foreach (Button b in allBtns)
            {
                if (b == null) continue;
                string bName = b.name.ToLower();
                if (bName.Contains("back") || bName.Contains("seashell") || bName.Contains("main_back") || bName.Contains("return") || bName.Contains("home"))
                {
                    backButton = b;
                    break;
                }
            }
        }

        // If still null, check Canvas parent
        if (backButton == null && unitRoot.parent != null)
        {
            Button[] allBtns = unitRoot.parent.GetComponentsInChildren<Button>(true);
            foreach (Button b in allBtns)
            {
                if (b == null) continue;
                string bName = b.name.ToLower();
                if (bName.Contains("back") || bName.Contains("seashell") || bName.Contains("main_back"))
                {
                    backButton = b;
                    break;
                }
            }
        }

        // Fallback: If no Back Button exists in scene, create one!
        if (backButton == null)
        {
            GameObject bObj = new GameObject("Back_Button");
            bObj.transform.SetParent(unitRoot, false);

            Image img = bObj.AddComponent<Image>();
            img.color = new Color(0.95f, 0.95f, 1f, 1f);

            backButton = bObj.AddComponent<Button>();

            RectTransform rt = bObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(30f, -30f);
            rt.sizeDelta        = new Vector2(100f, 60f);

            GameObject textObj = new GameObject("Label");
            textObj.transform.SetParent(bObj.transform, false);
            TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = "← Back";
            tmp.fontSize = 28;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.08f, 0.18f, 0.4f, 1f);

            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
            backButton.gameObject.SetActive(true); // Back button stays visible!
            backButton.transform.SetAsLastSibling();
        }

        // 2. Auto-bind ALL Next buttons in scene (excluding backButton!)
        allNextButtons.Clear();
        Button[] sceneButtons = unitRoot.GetComponentsInChildren<Button>(true);
        foreach (Button btn in sceneButtons)
        {
            if (btn == null || btn == backButton) continue;
            string bName = btn.name.ToLower();
            if (bName.Contains("back") || bName.Contains("seashell") || bName.Contains("home")) continue;

            if (bName.Contains("next_button") || bName.Contains("nextbutton") || bName == "next button" || bName.Contains("next"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnNextButtonClicked);
                if (!allNextButtons.Contains(btn)) allNextButtons.Add(btn);
                if (nextButton == null) nextButton = btn;
            }
        }

        // ── Level selection panel ──
        if (levelSelectionPanel == null)
        {
            string[] selNames = { "Unit_10_Section_Selection_Panels", "Unit_10_Section_Selection_Panel", "Unit_9_Section_Selection_Panels", "Section_Selection_Panels" };
            foreach (string n in selNames)
            {
                Transform t = unitRoot.Find(n);
                if (t == null) t = transform.Find(n);
                if (t != null) { levelSelectionPanel = t.gameObject; break; }
            }
        }

        // Auto-wire Section Selection Cards inside levelSelectionPanel
        if (levelSelectionPanel != null)
        {
            Button[] selBtns = levelSelectionPanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in selBtns)
            {
                if (btn == null) continue;
                string bName = btn.name.ToLower();

                if (bName.Contains("section_a") || bName.Contains("section a") || bName.Contains("beginning") || bName.Contains("item_0") || bName.Contains("card_a"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(StartSectionA);
                }
                else if (bName.Contains("section_b") || bName.Contains("section b") || bName.Contains("start_game") || bName.Contains("item_1") || bName.Contains("card_b"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(StartSectionB);
                }
                else if (bName.Contains("section_c") || bName.Contains("section c") || bName.Contains("ending") || bName.Contains("item_2") || bName.Contains("card_c"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(StartSectionC);
                }
                else if (bName.Contains("section_d") || bName.Contains("section d") || bName.Contains("finish_game") || bName.Contains("item_3") || bName.Contains("card_d"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(StartSectionD);
                }
            }
        }

        Transform searchRoot = sections != null ? sections : unitRoot;

        // ── Activity Panels (supports duplicated Unit 9 names!) ──
        if (introDemoPanel == null)
        {
            string[] names = { "Intro_Demo_Panel", "Concept_Demo_Panel", "Intro_Panel", "Intro_Chart_Panel" };
            foreach (string n in names)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = unitRoot.Find(n);
                if (t != null) { introDemoPanel = t.gameObject; break; }
            }
        }

        if (beginningBuilderPanel == null)
        {
            string[] names = { "Beginning_Builder_Panel_A", "Beginning_Builder_Panel", "Section_A_Panel", "Arrow_Blend_Panel_A", "Arrow_Blend_Panel" };
            foreach (string n in names)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = unitRoot.Find(n);
                if (t != null) { beginningBuilderPanel = t.gameObject; break; }
            }
        }

        if (startItRightPanel == null)
        {
            string[] names = { "Start_It_Right_Panel_B", "Start_It_Right_Panel", "Section_B_Panel", "Digraph_Hunt_Panel_B", "Digraph_Hunt_Panel" };
            foreach (string n in names)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = unitRoot.Find(n);
                if (t != null) { startItRightPanel = t.gameObject; break; }
            }
        }

        if (endingBuilderPanel == null)
        {
            string[] names = { "Ending_Builder_Panel_C", "Ending_Builder_Panel", "Section_C_Panel", "Pick_Digraph_Panel_C", "Pick_Digraph_Panel" };
            foreach (string n in names)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = unitRoot.Find(n);
                if (t != null) { endingBuilderPanel = t.gameObject; break; }
            }
        }

        if (finishItRightPanel == null)
        {
            string[] names = { "Finish_It_Right_Panel_D", "Finish_It_Right_Panel", "Section_D_Panel" };
            foreach (string n in names)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = unitRoot.Find(n);
                if (t != null) { finishItRightPanel = t.gameObject; break; }
            }
        }

        if (rewardPanel == null)
        {
            string[] names = { "RewardPanel", "Reward_Panel", "Trophy_Panel" };
            foreach (string n in names)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = unitRoot.Find(n);
                if (t != null) { rewardPanel = t.gameObject; break; }
            }
        }

        // ── Auto-attach Unit 10 Controllers if duplicated from Unit 9! ──
        if (introDemoPanel != null)
        {
            U9_IntroChartController oldComp = introDemoPanel.GetComponent<U9_IntroChartController>();
            if (oldComp != null) DestroyImmediate(oldComp);
            introDemoController = introDemoPanel.GetComponent<U10_IntroDemoController>();
            if (introDemoController == null) introDemoController = introDemoPanel.AddComponent<U10_IntroDemoController>();
        }

        if (beginningBuilderPanel != null)
        {
            U9_A1_ArrowBlendController oldComp = beginningBuilderPanel.GetComponent<U9_A1_ArrowBlendController>();
            if (oldComp != null) DestroyImmediate(oldComp);
            beginningBuilderController = beginningBuilderPanel.GetComponent<U10_A1_BeginningBuilderController>();
            if (beginningBuilderController == null) beginningBuilderController = beginningBuilderPanel.AddComponent<U10_A1_BeginningBuilderController>();
        }

        if (startItRightPanel != null)
        {
            U9_A2_DigraphHuntController oldComp = startItRightPanel.GetComponent<U9_A2_DigraphHuntController>();
            if (oldComp != null) DestroyImmediate(oldComp);
            startItRightController = startItRightPanel.GetComponent<U10_A2_StartItRightController>();
            if (startItRightController == null) startItRightController = startItRightPanel.AddComponent<U10_A2_StartItRightController>();
        }

        if (endingBuilderPanel != null)
        {
            U9_A3_PickDigraphController oldComp = endingBuilderPanel.GetComponent<U9_A3_PickDigraphController>();
            if (oldComp != null) DestroyImmediate(oldComp);
            endingBuilderController = endingBuilderPanel.GetComponent<U10_A3_EndingBuilderController>();
            if (endingBuilderController == null) endingBuilderController = endingBuilderPanel.AddComponent<U10_A3_EndingBuilderController>();
        }

        if (finishItRightPanel != null)
        {
            finishItRightController = finishItRightPanel.GetComponent<U10_A4_FinishItRightController>();
            if (finishItRightController == null) finishItRightController = finishItRightPanel.AddComponent<U10_A4_FinishItRightController>();
        }

        if (rewardPanel != null)
        {
            U9_RewardController oldComp = rewardPanel.GetComponent<U9_RewardController>();
            if (oldComp != null) DestroyImmediate(oldComp);
            rewardController = rewardPanel.GetComponent<U10_RewardController>();
            if (rewardController == null) rewardController = rewardPanel.AddComponent<U10_RewardController>();
        }

        // Wire back-references
        if (introDemoController        != null) introDemoController.manager        = this;
        if (beginningBuilderController != null) beginningBuilderController.manager = this;
        if (startItRightController     != null) startItRightController.manager     = this;
        if (endingBuilderController    != null) endingBuilderController.manager    = this;
        if (finishItRightController    != null) finishItRightController.manager    = this;
        if (rewardController           != null) rewardController.manager           = this;
    }

    public void HideNextButton()
    {
        // Hide ONLY Next buttons
        foreach (Button btn in allNextButtons)
        {
            if (btn != null && btn != backButton) btn.gameObject.SetActive(false);
        }
        if (nextButton != null && nextButton != backButton) nextButton.gameObject.SetActive(false);

        // ALWAYS keep Back Button active!
        if (backButton != null) backButton.gameObject.SetActive(true);
    }

    public void ShowNextButton()
    {
        foreach (Button btn in allNextButtons)
        {
            if (btn != null && btn != backButton)
            {
                btn.gameObject.SetActive(true);
                btn.transform.SetAsLastSibling();
            }
        }
        if (nextButton != null && nextButton != backButton)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.transform.SetAsLastSibling();
        }

        // Keep Back Button active!
        if (backButton != null) backButton.gameObject.SetActive(true);
    }

    private void EnablePanel(GameObject panel)
    {
        if (panel == null) { AutoBindPanels(); return; }
        Transform current = panel.transform.parent;
        while (current != null)
        {
            if (!current.gameObject.activeSelf) current.gameObject.SetActive(true);
            current = current.parent;
        }
        panel.SetActive(true);

        // Keep Back button active at all times!
        if (backButton != null) backButton.gameObject.SetActive(true);
    }

    public void CloseAllPanels()
    {
        if (levelSelectionPanel    != null) levelSelectionPanel.SetActive(false);
        if (introDemoPanel         != null) introDemoPanel.SetActive(false);
        if (beginningBuilderPanel  != null) beginningBuilderPanel.SetActive(false);
        if (startItRightPanel      != null) startItRightPanel.SetActive(false);
        if (endingBuilderPanel     != null) endingBuilderPanel.SetActive(false);
        if (finishItRightPanel     != null) finishItRightPanel.SetActive(false);
        if (rewardPanel            != null) rewardPanel.SetActive(false);
        HideNextButton();
    }

    public void ShowLevelSelection()
    {
        currentFlowIndex = -1;
        CloseAllPanels();
        EnablePanel(levelSelectionPanel);
        HideNextButton();
    }

    public void StartLevel()
    {
        AutoBindPanels();
        StartSectionA();
    }

    // ── Section Selection Button Aliases ──

    public void StartSectionA()
    {
        StartIntroDemo();
    }

    public void StartSectionB()
    {
        StartStartItRightGame();
    }

    public void StartSectionC()
    {
        StartEndingBuilder();
    }

    public void StartSectionD()
    {
        StartFinishItRightGame();
    }

    // ── Flow Step Methods ──

    public void StartIntroDemo()
    {
        currentFlowIndex = 0;
        CloseAllPanels();
        EnablePanel(introDemoPanel);
        if (introDemoController != null) introDemoController.SetupActivity();
    }

    public void StartBeginningBuilder()
    {
        currentFlowIndex = 1;
        CloseAllPanels();
        EnablePanel(beginningBuilderPanel);
        if (beginningBuilderController != null)
            beginningBuilderController.SetupActivity(levelData != null ? levelData.beginningBuilderWords : null);
    }

    public void StartStartItRightGame()
    {
        currentFlowIndex = 2;
        CloseAllPanels();
        EnablePanel(startItRightPanel);
        if (startItRightController != null)
            startItRightController.SetupActivity(levelData != null ? levelData.startItRightGameWords : null);
    }

    public void StartEndingBuilder()
    {
        currentFlowIndex = 3;
        CloseAllPanels();
        EnablePanel(endingBuilderPanel);
        if (endingBuilderController != null)
            endingBuilderController.SetupActivity(levelData != null ? levelData.endingBuilderWords : null);
    }

    public void StartFinishItRightGame()
    {
        currentFlowIndex = 4;
        CloseAllPanels();
        EnablePanel(finishItRightPanel);
        if (finishItRightController != null)
            finishItRightController.SetupActivity(levelData != null ? levelData.finishItRightGameWords : null);
    }

    // ── Reward Methods ──

    public void ShowStage1Reward()
    {
        CloseAllPanels();
        EnablePanel(rewardPanel);
        if (rewardController != null) rewardController.ShowBeginningBlendsBadge();
    }

    public void ShowStage2Reward()
    {
        CloseAllPanels();
        EnablePanel(rewardPanel);
        if (rewardController != null) rewardController.ShowStartItRightBadge();
    }

    public void ShowStage3Reward()
    {
        CloseAllPanels();
        EnablePanel(rewardPanel);
        if (rewardController != null) rewardController.ShowEndingBlendsBadge();
    }

    public void ShowFinalBookReward()
    {
        currentFlowIndex = 5;
        CloseAllPanels();
        EnablePanel(rewardPanel);
        if (rewardController != null) rewardController.ShowGrandFinaleTrophy();
    }

    public void OnRewardContinueClicked(int rewardType)
    {
        ShowLevelSelection();
    }

    public void OnNextButtonClicked()
    {
        // If currently on the Reward Panel (Badge Screen)
        if (rewardPanel != null && rewardPanel.activeSelf && rewardController != null)
        {
            switch (rewardController.currentRewardType)
            {
                case 1: // Stage 1 Badge completed -> Go to Section B!
                    StartStartItRightGame();
                    break;
                case 2: // Stage 2 Badge completed -> Go to Section C!
                    StartEndingBuilder();
                    break;
                case 3: // Stage 3 Badge completed -> Go to Section D!
                    StartFinishItRightGame();
                    break;
                case 4: // Stage 4 Badge completed -> Show Grand Finale Trophy!
                    ShowFinalBookReward();
                    break;
                case 5: // Grand Finale Trophy -> Return to Level Selection!
                    ShowLevelSelection();
                    break;
                default:
                    ShowLevelSelection();
                    break;
            }
            return;
        }

        switch (currentFlowIndex)
        {
            case 0: // Intro Demo completed -> Go to Section A!
                StartBeginningBuilder();
                break;

            case 1: // Section A completed -> Show Stage 1 Badge!
                ShowStage1Reward();
                break;

            case 2: // Section B completed -> Show Stage 2 Badge!
                ShowStage2Reward();
                break;

            case 3: // Section C completed -> Show Stage 3 Badge!
                ShowStage3Reward();
                break;

            case 4: // Section D completed -> Show Grand Finale Trophy!
                ShowFinalBookReward();
                break;

            case 5: // Grand Finale -> Return to Level Selection!
                ShowLevelSelection();
                break;

            default: // Safety fallback -> Go to Section A!
                StartBeginningBuilder();
                break;
        }
    }

    public void OnBackButtonClicked()
    {
        // Check if player is currently inside ANY section activity panel or reward panel
        bool isInsideActivity = (introDemoPanel != null && introDemoPanel.activeSelf) ||
                                (beginningBuilderPanel != null && beginningBuilderPanel.activeSelf) ||
                                (startItRightPanel != null && startItRightPanel.activeSelf) ||
                                (endingBuilderPanel != null && endingBuilderPanel.activeSelf) ||
                                (finishItRightPanel != null && finishItRightPanel.activeSelf) ||
                                (rewardPanel != null && rewardPanel.activeSelf) ||
                                (currentFlowIndex >= 0);

        if (isInsideActivity)
        {
            // Back button inside any section/activity -> Returns to Section Selection Signboard Panel!
            ShowLevelSelection();
        }
        else
        {
            // Back button on Section Selection Signboard -> Returns to Main Unit Selection Lessons Menu!
            Transform unitRoot = transform;
            while (unitRoot.parent != null && unitRoot.parent.name != "Canvas")
                unitRoot = unitRoot.parent;

            unitRoot.gameObject.SetActive(false);

            Unit_Selection_Panel_Phonics_Junior sel = FindFirstObjectByType<Unit_Selection_Panel_Phonics_Junior>(FindObjectsInactive.Include);
            if (sel != null) sel.gameObject.SetActive(true);
        }
    }
}
