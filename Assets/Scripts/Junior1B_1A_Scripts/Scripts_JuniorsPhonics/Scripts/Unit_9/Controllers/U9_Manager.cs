using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Master Manager for Unit 9 — Consonant Digraphs (Pages 32–41).
/// Manages complete step-by-step game flow & rewards:
/// Intro Chart (Page 32) -> Stage 1 (Blend & Hunt) -> Stage 1 Badge ->
/// Stage 2 (Blend & Hunt) -> Stage 2 Badge -> Stage 3 (Blend & Hunt) ->
/// Stage 3 Badge -> Page 41 Pick the Digraph -> Digraph Detective Trophy 🏆!
/// </summary>
public class U9_Manager : MonoBehaviour
{
    [Header("Level Data")]
    public Unit9LevelData levelData;

    [Header("UI Panels")]
    public GameObject levelSelectionPanel;   // Unit_9_Section_Selection_Panels
    public GameObject introChartPanel;       // Intro_Chart_Panel_A (Page 32)
    public GameObject arrowBlendPanel;       // Arrow_Blend_Panel_B (Activity 1)
    public GameObject digraphHuntPanel;      // Digraph_Hunt_Panel_C (Activity 2)
    public GameObject pickDigraphPanel;      // Pick_Digraph_Panel_D (Final Game Page 41)
    public GameObject rewardPanel;           // RewardPanel (Badges & Trophy)

    [Header("Controllers")]
    public U9_IntroChartController introChartController;
    public U9_A1_ArrowBlendController arrowBlendController;
    public U9_A2_DigraphHuntController digraphHuntController;
    public U9_A3_PickDigraphController pickDigraphController;
    public U9_RewardController rewardController;

    [Header("Navigation Buttons")]
    public Button backButton;
    public Button nextButton;

    // Flow State: 0 = Intro, 1 = Stage1 Blend, 2 = Stage1 Hunt, 3 = Stage2 Blend, 4 = Stage2 Hunt, 5 = Stage3 Blend, 6 = Stage3 Hunt, 7 = Pick Game, 8 = Reward, -1 = Map
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

    public void AutoBindPanels()
    {
        Transform unitRoot = transform;
        while (unitRoot.parent != null && unitRoot.parent.name != "Canvas")
            unitRoot = unitRoot.parent;

        Transform sections = unitRoot.Find("Unit_9_Sections");
        if (sections == null) sections = transform.Find("Unit_9_Sections");
        Transform searchRoot = sections != null ? sections : unitRoot;

        // Auto-bind ALL Next buttons in scene
        allNextButtons.Clear();
        Button[] sceneButtons = unitRoot.GetComponentsInChildren<Button>(true);
        foreach (Button btn in sceneButtons)
        {
            if (btn != null && (btn.name.Contains("Next_Button") || btn.name.Contains("NextButton") || btn.name == "Next Button"))
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(OnNextButtonClicked);
                if (!allNextButtons.Contains(btn)) allNextButtons.Add(btn);
                if (nextButton == null) nextButton = btn;
            }
        }

        // Auto-find Back Button
        if (backButton == null)
        {
            string[] backNames = { "Back_Button", "BackButton", "Back Button", "SeaShell", "Main_Back" };
            foreach (string n in backNames)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = unitRoot.Find(n);
                if (t != null) { backButton = t.GetComponent<Button>(); break; }
            }
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }

        // ── Level selection panel ──
        if (levelSelectionPanel == null)
        {
            string[] selNames = { "Unit_9_Section_Selection_Panels", "Unit_9_Section_Selection_Panel", "Section_Selection_Panels" };
            foreach (string n in selNames)
            {
                Transform t = unitRoot.Find(n);
                if (t == null) t = transform.Find(n);
                if (t != null) { levelSelectionPanel = t.gameObject; break; }
            }
        }

        // Auto-wire Section Selection Cards inside Unit_9_Section_Selection_Panels
        if (levelSelectionPanel != null)
        {
            Button[] selBtns = levelSelectionPanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in selBtns)
            {
                if (btn == null) continue;
                string bName = btn.name.ToLower();

                if (bName.Contains("section_a") || bName.Contains("section a") || bName.Contains("stage1") || bName.Contains("item_0") || bName.Contains("card_a"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(StartSectionA);
                }
                else if (bName.Contains("section_b") || bName.Contains("section b") || bName.Contains("stage2") || bName.Contains("item_1") || bName.Contains("card_b"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(StartSectionB);
                }
                else if (bName.Contains("section_c") || bName.Contains("section c") || bName.Contains("stage3") || bName.Contains("item_2") || bName.Contains("card_c"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(StartSectionC);
                }
                else if (bName.Contains("section_d") || bName.Contains("section d") || bName.Contains("final") || bName.Contains("item_3") || bName.Contains("card_d"))
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(StartSectionD);
                }
            }
        }

        // ── Activity Panels (matching exact Hierarchy names) ──
        if (introChartPanel == null)
        {
            string[] names = { "Intro_Chart_Panel_A", "Intro_Chart_Panel", "Chart_Panel", "Page32_Panel" };
            foreach (string n in names)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = unitRoot.Find(n);
                if (t != null) { introChartPanel = t.gameObject; break; }
            }
        }

        if (arrowBlendPanel == null)
        {
            string[] names = { "Arrow_Blend_Panel_B", "Arrow_Blend_Panel", "BuildWord_Panel", "Activity1_Panel" };
            foreach (string n in names)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = unitRoot.Find(n);
                if (t != null) { arrowBlendPanel = t.gameObject; break; }
            }
        }

        if (digraphHuntPanel == null)
        {
            string[] names = { "Digraph_Hunt_Panel_C", "Digraph_Hunt_Panel", "SpotDigraph_Panel", "Activity2_Panel" };
            foreach (string n in names)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = unitRoot.Find(n);
                if (t != null) { digraphHuntPanel = t.gameObject; break; }
            }
        }

        if (pickDigraphPanel == null)
        {
            string[] names = { "Pick_Digraph_Panel_D", "Pick_Digraph_Panel", "Page41_Panel", "FinalGame_Panel" };
            foreach (string n in names)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = unitRoot.Find(n);
                if (t != null) { pickDigraphPanel = t.gameObject; break; }
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

        // ── Controllers ──
        if (introChartController == null && introChartPanel != null)   introChartController = introChartPanel.GetComponent<U9_IntroChartController>();
        if (arrowBlendController == null && arrowBlendPanel != null)   arrowBlendController = arrowBlendPanel.GetComponent<U9_A1_ArrowBlendController>();
        if (digraphHuntController == null && digraphHuntPanel != null) digraphHuntController = digraphHuntPanel.GetComponent<U9_A2_DigraphHuntController>();
        if (pickDigraphController == null && pickDigraphPanel != null) pickDigraphController = pickDigraphPanel.GetComponent<U9_A3_PickDigraphController>();
        if (rewardController == null && rewardPanel != null)           rewardController = rewardPanel.GetComponent<U9_RewardController>();

        // Wire back-references
        if (introChartController != null)   introChartController.manager = this;
        if (arrowBlendController != null)   arrowBlendController.manager = this;
        if (digraphHuntController != null)  digraphHuntController.manager = this;
        if (pickDigraphController != null)  pickDigraphController.manager = this;
        if (rewardController != null)        rewardController.manager = this;
    }

    public void HideNextButton()
    {
        foreach (Button btn in allNextButtons) if (btn != null) btn.gameObject.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);
    }

    public void ShowNextButton()
    {
        foreach (Button btn in allNextButtons)
        {
            if (btn != null) { btn.gameObject.SetActive(true); btn.transform.SetAsLastSibling(); }
        }
        if (nextButton != null) { nextButton.gameObject.SetActive(true); nextButton.transform.SetAsLastSibling(); }
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
    }

    public void CloseAllPanels()
    {
        if (levelSelectionPanel != null) levelSelectionPanel.SetActive(false);
        if (introChartPanel     != null) introChartPanel.SetActive(false);
        if (arrowBlendPanel     != null) arrowBlendPanel.SetActive(false);
        if (digraphHuntPanel    != null) digraphHuntPanel.SetActive(false);
        if (pickDigraphPanel    != null) pickDigraphPanel.SetActive(false);
        if (rewardPanel         != null) rewardPanel.SetActive(false);
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

    // ── Section Selection Button Aliases (Section A, B, C, D) ──

    public void StartSectionA()
    {
        StartIntroChart();
    }

    public void StartSectionB()
    {
        StartStage2Blend();
    }

    public void StartSectionC()
    {
        StartStage3Blend();
    }

    public void StartSectionD()
    {
        StartPickDigraphGame();
    }

    // ── Flow Step Methods ──

    public void StartIntroChart()
    {
        currentFlowIndex = 0; // Page 32 Chart & Stage 1
        CloseAllPanels();
        EnablePanel(introChartPanel);
        if (introChartController != null) introChartController.SetupActivity(levelData);
    }

    public void StartStage1Blend()
    {
        currentFlowIndex = 1; // Stage 1 (ch, sh) Arrow Blend
        CloseAllPanels();
        EnablePanel(arrowBlendPanel);
        if (arrowBlendController != null)
            arrowBlendController.SetupActivity(levelData != null ? levelData.stage1Words : null);
    }

    public void StartStage1Hunt()
    {
        currentFlowIndex = 2; // Stage 1 (ch, sh) Digraph Hunt
        CloseAllPanels();
        EnablePanel(digraphHuntPanel);
        if (digraphHuntController != null)
            digraphHuntController.SetupActivity(levelData != null ? levelData.stage1Words : null);
    }

    public void StartStage2Blend()
    {
        currentFlowIndex = 3; // Stage 2 (th, wh) Arrow Blend
        CloseAllPanels();
        EnablePanel(arrowBlendPanel);
        if (arrowBlendController != null)
            arrowBlendController.SetupActivity(levelData != null ? levelData.stage2Words : null);
    }

    public void StartStage2Hunt()
    {
        currentFlowIndex = 4; // Stage 2 (th, wh) Digraph Hunt
        CloseAllPanels();
        EnablePanel(digraphHuntPanel);
        if (digraphHuntController != null)
            digraphHuntController.SetupActivity(levelData != null ? levelData.stage2Words : null);
    }

    public void StartStage3Blend()
    {
        currentFlowIndex = 5; // Stage 3 (ck, nk, ng) Arrow Blend
        CloseAllPanels();
        EnablePanel(arrowBlendPanel);
        if (arrowBlendController != null)
            arrowBlendController.SetupActivity(levelData != null ? levelData.stage3Words : null);
    }

    public void StartStage3Hunt()
    {
        currentFlowIndex = 6; // Stage 3 (ck, nk, ng) Digraph Hunt
        CloseAllPanels();
        EnablePanel(digraphHuntPanel);
        if (digraphHuntController != null)
            digraphHuntController.SetupActivity(levelData != null ? levelData.stage3Words : null);
    }

    public void StartPickDigraphGame()
    {
        currentFlowIndex = 7; // Page 41 Pick the Digraph Game
        CloseAllPanels();
        EnablePanel(pickDigraphPanel);
        if (pickDigraphController != null)
            pickDigraphController.SetupActivity(levelData != null ? levelData.pickDigraphWords : null);
    }

    // ── Reward Methods ──

    public void ShowStage1Reward()
    {
        CloseAllPanels();
        EnablePanel(rewardPanel);
        if (rewardController != null) rewardController.ShowStage1Badge();
    }

    public void ShowStage2Reward()
    {
        CloseAllPanels();
        EnablePanel(rewardPanel);
        if (rewardController != null) rewardController.ShowStage2Badge();
    }

    public void ShowStage3Reward()
    {
        CloseAllPanels();
        EnablePanel(rewardPanel);
        if (rewardController != null) rewardController.ShowStage3Badge();
    }

    public void ShowReward()
    {
        currentFlowIndex = 8;
        CloseAllPanels();
        EnablePanel(rewardPanel);
        if (rewardController != null) rewardController.ShowTrophy();
    }

    public void OnRewardContinueClicked(int rewardType)
    {
        switch (rewardType)
        {
            case 1: StartStage2Blend();     break; // Stage 1 Badge -> Stage 2
            case 2: StartStage3Blend();     break; // Stage 2 Badge -> Stage 3
            case 3: StartPickDigraphGame(); break; // Stage 3 Badge -> Page 41 Pick Game
            case 4: ShowLevelSelection();   break; // Trophy -> Signboard Menu
            default: ShowLevelSelection();  break;
        }
    }

    public void OnNextButtonClicked()
    {
        switch (currentFlowIndex)
        {
            case 0: StartStage1Blend();  break;
            case 1: StartStage1Hunt();   break;
            case 2: ShowStage1Reward();  break; // Stage 1 Hunt done -> Stage 1 Badge
            case 3: StartStage2Hunt();   break;
            case 4: ShowStage2Reward();  break; // Stage 2 Hunt done -> Stage 2 Badge
            case 5: StartStage3Hunt();   break;
            case 6: ShowStage3Reward();  break; // Stage 3 Hunt done -> Stage 3 Badge
            case 7: ShowReward();        break; // Pick Game done -> Trophy 🏆
            case 8: ShowLevelSelection(); break;
            default: ShowLevelSelection(); break;
        }
    }

    public void OnBackButtonClicked()
    {
        if (currentFlowIndex >= 0)
        {
            ShowLevelSelection();
        }
        else
        {
            // Close unit selection back to main menu
            Transform unitRoot = transform;
            while (unitRoot.parent != null && unitRoot.parent.name != "Canvas")
                unitRoot = unitRoot.parent;

            unitRoot.gameObject.SetActive(false);
        }
    }
}
