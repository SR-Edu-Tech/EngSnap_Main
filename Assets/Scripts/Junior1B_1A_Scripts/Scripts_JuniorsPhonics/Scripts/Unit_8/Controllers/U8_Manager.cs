using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class U8_Manager : MonoBehaviour
{
    [Header("Level Data")]
    public Unit8LevelData levelData;

    [Header("UI Panels")]
    public GameObject levelSelectionPanel;   // Unit_8_Section_Selection_Panels
    public GameObject sectionAPanel;         // SectionA_Panel  (Sound Wall)
    public GameObject sectionBPanel;         // SectionB_Panel  (Buzz or Whisper)
    public GameObject sectionCPanel;         // SectionC_Panel  (Connect the Sound)
    public GameObject sectionDPanel;         // SectionD_Panel  (Consonant Safari)
    public GameObject rewardPanel;           // RewardPanel

    [Header("Controllers")]
    public U8_A1_SoundWallController a1Controller;
    public U8_A2_BuzzWhisperController a2Controller;
    public U8_A3_ConnectSoundController a3Controller;
    public U8_A4_ConsonantSafariController a4Controller;
    public U8_RewardController rewardController;

    [Header("Navigation Buttons")]
    public Button backButton;
    public Button nextButton;

    // Direct Section State Tracking (0 = Sec A, 1 = Sec B, 2 = Sec C, 3 = Sec D, 4 = Reward, -1 = LevelSelection)
    private int currentSectionIndex = -1;
    private List<Button> allNextButtons = new List<Button>();

    // ──────────────────────────────────────────────────────────
    //  Lifecycle
    // ──────────────────────────────────────────────────────────

    private void Awake()
    {
        AutoBindPanels();
    }

    private void Start()
    {
        AutoBindPanels();
    }

    // ──────────────────────────────────────────────────────────
    //  Auto-Discovery
    // ──────────────────────────────────────────────────────────

    public void AutoBindPanels()
    {
        // Walk up to the Unit_8 root
        Transform unitRoot = transform;
        while (unitRoot.parent != null && unitRoot.parent.name != "Canvas")
            unitRoot = unitRoot.parent;

        Transform sections = unitRoot.Find("Unit_8_Sections");
        if (sections == null) sections = transform.Find("Unit_8_Sections");
        Transform searchRoot = sections != null ? sections : unitRoot;

        // Auto-find ALL Next Buttons in scene (root or panel children)
        allNextButtons.Clear();
        Button[] sceneButtons = unitRoot.GetComponentsInChildren<Button>(true);
        foreach (Button btn in sceneButtons)
        {
            if (btn != null && btn.name.ToLower().Contains("next"))
            {
                btn.onClick.RemoveListener(OnNextButtonClicked);
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
            string[] selectionNames = {
                "Unit_8_Section_Selection_Panels", "Unit_8_Section_Selection_Panel",
                "Section_Selection_Panels", "Section_Selection_Panel",
                "Level_Selection_Panel", "Level_Selection_Panels"
            };
            foreach (string n in selectionNames)
            {
                Transform t = unitRoot.Find(n);
                if (t == null) t = transform.Find(n);
                if (t != null) { levelSelectionPanel = t.gameObject; break; }
            }
        }

        if (levelSelectionPanel != null)
        {
            Button[] sectionBtns = levelSelectionPanel.GetComponentsInChildren<Button>(true);
            foreach (Button btn in sectionBtns)
            {
                string lowerName = btn.name.ToLower();
                if (lowerName.Contains("sectiona") || lowerName.Contains("section_a") || lowerName.Contains("activity1"))
                {
                    btn.onClick.RemoveListener(StartSectionA);
                    btn.onClick.AddListener(StartSectionA);
                }
                else if (lowerName.Contains("sectionb") || lowerName.Contains("section_b") || lowerName.Contains("activity2"))
                {
                    btn.onClick.RemoveListener(StartSectionB);
                    btn.onClick.AddListener(StartSectionB);
                }
                else if (lowerName.Contains("sectionc") || lowerName.Contains("section_c") || lowerName.Contains("activity3"))
                {
                    btn.onClick.RemoveListener(StartSectionC);
                    btn.onClick.AddListener(StartSectionC);
                }
                else if (lowerName.Contains("sectiond") || lowerName.Contains("section_d") || lowerName.Contains("activity4") || lowerName.Contains("safari"))
                {
                    btn.onClick.RemoveListener(StartSectionD);
                    btn.onClick.AddListener(StartSectionD);
                }
            }
        }

        // ── Activity panels ──
        if (sectionAPanel == null)
        {
            string[] aNames = { "SectionA_Panel", "Section_A_Panel", "Section A Panel", "Activity1_Panel" };
            foreach (string n in aNames)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = unitRoot.Find(n);
                if (t != null) { sectionAPanel = t.gameObject; break; }
            }
        }

        if (sectionBPanel == null)
        {
            string[] bNames = { "SectionB_Panel", "Section_B_Panel", "Section B Panel", "Activity2_Panel" };
            foreach (string n in bNames)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = unitRoot.Find(n);
                if (t != null) { sectionBPanel = t.gameObject; break; }
            }
        }

        if (sectionCPanel == null)
        {
            string[] cNames = { "SectionC_Panel", "Section_C_Panel", "Section C Panel", "Activity3_Panel" };
            foreach (string n in cNames)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = unitRoot.Find(n);
                if (t != null) { sectionCPanel = t.gameObject; break; }
            }
        }

        if (sectionDPanel == null)
        {
            string[] dNames = { "SectionD_Panel", "Section_D_Panel", "Section D Panel", "SectionD", "ConsonantSafari", "Consonant Safari", "SafariPanel", "Activity4_Panel" };
            foreach (string n in dNames)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = unitRoot.Find(n);
                if (t != null) { sectionDPanel = t.gameObject; break; }
            }

            if (sectionDPanel == null)
            {
                foreach (Transform child in searchRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (child.name.Contains("SectionD") || child.name.Contains("Section_D") || child.name.Contains("Safari"))
                    {
                        sectionDPanel = child.gameObject;
                        break;
                    }
                }
            }
        }

        if (rewardPanel == null)
        {
            string[] rNames = { "RewardPanel", "Reward_Panel", "Reward Panel" };
            foreach (string n in rNames)
            {
                Transform t = searchRoot.Find(n);
                if (t == null) t = unitRoot.Find(n);
                if (t != null) { rewardPanel = t.gameObject; break; }
            }
        }

        // ── Controllers ──
        if (a1Controller == null && sectionAPanel != null) a1Controller = sectionAPanel.GetComponent<U8_A1_SoundWallController>();
        if (a2Controller == null && sectionBPanel != null) a2Controller = sectionBPanel.GetComponent<U8_A2_BuzzWhisperController>();
        if (a3Controller == null && sectionCPanel != null) a3Controller = sectionCPanel.GetComponent<U8_A3_ConnectSoundController>();
        if (a4Controller == null && sectionDPanel != null) a4Controller = sectionDPanel.GetComponent<U8_A4_ConsonantSafariController>();
        if (rewardController == null && rewardPanel != null) rewardController = rewardPanel.GetComponent<U8_RewardController>();

        // ── Back-reference into each controller ──
        if (a1Controller != null) a1Controller.manager = this;
        if (a2Controller != null) a2Controller.manager = this;
        if (a3Controller != null) a3Controller.manager = this;
        if (a4Controller != null) a4Controller.manager = this;
        if (rewardController != null) rewardController.manager = this;
    }

    // ──────────────────────────────────────────────────────────
    //  Global Next Button Controls
    // ──────────────────────────────────────────────────────────

    public void HideNextButton()
    {
        foreach (Button btn in allNextButtons)
        {
            if (btn != null) btn.gameObject.SetActive(false);
        }
        if (nextButton != null) nextButton.gameObject.SetActive(false);
    }

    public void ShowNextButton()
    {
        foreach (Button btn in allNextButtons)
        {
            if (btn != null)
            {
                btn.gameObject.SetActive(true);
                btn.transform.SetAsLastSibling();
                
                // Fail-safe wiring
                btn.onClick.RemoveListener(OnNextButtonClicked);
                btn.onClick.AddListener(OnNextButtonClicked);
            }
        }
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(true);
            nextButton.transform.SetAsLastSibling();
        }
    }

    // ──────────────────────────────────────────────────────────
    //  Panel Utilities
    // ──────────────────────────────────────────────────────────

    private void EnablePanel(GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogWarning("[U8_Manager] EnablePanel called on null panel! Attempting AutoBindPanels...");
            AutoBindPanels();
            return;
        }

        // Make sure parent container (e.g. Unit_8_Sections) is active
        Transform current = panel.transform.parent;
        while (current != null)
        {
            if (!current.gameObject.activeSelf) current.gameObject.SetActive(true);
            current = current.parent;
        }

        panel.SetActive(true);

        // Ensure every child of target panel is active
        foreach (Transform child in panel.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && !child.gameObject.activeSelf)
                child.gameObject.SetActive(true);
        }
    }

    public void CloseAllPanels()
    {
        if (levelSelectionPanel != null) levelSelectionPanel.SetActive(false);
        if (sectionAPanel  != null) sectionAPanel.SetActive(false);
        if (sectionBPanel  != null) sectionBPanel.SetActive(false);
        if (sectionCPanel  != null) sectionCPanel.SetActive(false);
        if (sectionDPanel  != null) sectionDPanel.SetActive(false);
        if (rewardPanel    != null) rewardPanel.SetActive(false);
        HideNextButton();
    }

    // ──────────────────────────────────────────────────────────
    //  Navigation
    // ──────────────────────────────────────────────────────────

    public void ShowLevelSelection()
    {
        currentSectionIndex = -1;
        CloseAllPanels();
        EnablePanel(levelSelectionPanel);

        // Keep Viewport/Content children visible (section buttons)
        if (levelSelectionPanel != null)
        {
            Transform content = levelSelectionPanel.transform.Find("Viewport/Content");
            if (content == null) content = levelSelectionPanel.transform.Find("Content");
            if (content != null)
            {
                foreach (Transform child in content)
                    if (child != null && !child.gameObject.activeSelf)
                        child.gameObject.SetActive(true);
            }
        }

        // Hide the Sections container so no activity bleeds through
        Transform unitRoot = transform;
        while (unitRoot.parent != null && unitRoot.parent.name != "Canvas")
            unitRoot = unitRoot.parent;
        Transform secs = unitRoot.Find("Unit_8_Sections");
        if (secs != null) secs.gameObject.SetActive(false);

        HideNextButton();
    }

    // Entry point called by Unit_Selection_Panel
    public void StartLevel()
    {
        AutoBindPanels();
        StartSectionA();
    }

    // ── Section entry points ──

    public void StartSectionA()
    {
        currentSectionIndex = 0; // State 0 = Section A
        CloseAllPanels();
        HideNextButton();
        EnablePanel(sectionAPanel);
        if (a1Controller != null) a1Controller.SetupActivity(levelData);
    }

    public void StartSectionB()
    {
        currentSectionIndex = 1; // State 1 = Section B
        CloseAllPanels();
        HideNextButton();
        EnablePanel(sectionBPanel);
        if (a2Controller != null)
        {
            a2Controller.SetupActivity(levelData);
            a2Controller.OnActivityComplete = ShowNextButton;
        }
    }

    public void StartSectionC()
    {
        currentSectionIndex = 2; // State 2 = Section C
        CloseAllPanels();
        HideNextButton();
        EnablePanel(sectionCPanel);
        if (a3Controller != null)
        {
            a3Controller.SetupActivity(levelData);
            a3Controller.OnActivityComplete = ShowNextButton;
        }
    }

    public void StartSectionD()
    {
        currentSectionIndex = 3; // State 3 = Section D
        CloseAllPanels();
        HideNextButton();

        if (sectionDPanel == null) AutoBindPanels();
        EnablePanel(sectionDPanel);

        if (a4Controller != null)
        {
            a4Controller.SetupActivity(levelData);
            a4Controller.OnActivityComplete = ShowReward;
        }
    }

    public void ShowReward()
    {
        currentSectionIndex = 4; // State 4 = Reward
        CloseAllPanels();
        HideNextButton();
        EnablePanel(rewardPanel);
        if (rewardController != null) rewardController.SetupReward();
    }

    // ── Next/Back buttons ──

    public void OnNextButtonClicked()
    {
        HideNextButton(); // Disappear immediately when user clicks Next!

        // Guarantee transition from Section C to Section D!
        if (currentSectionIndex == 2 || (sectionCPanel != null && sectionCPanel.activeSelf))
        {
            StartSectionD();
            return;
        }

        switch (currentSectionIndex)
        {
            case 0: // Currently in Section A -> Advance to Section B!
                StartSectionB();
                break;
            case 1: // Currently in Section B -> Advance to Section C!
                StartSectionC();
                break;
            case 2: // Currently in Section C -> Advance to Section D!
                StartSectionD();
                break;
            case 3: // Currently in Section D -> Advance to Reward Panel!
                ShowReward();
                break;
            default:
                ShowLevelSelection();
                break;
        }
    }

    public void OnBackButtonClicked()
    {
        if (currentSectionIndex >= 0)
        {
            ShowLevelSelection();
        }
        else
        {
            // On the section selection board — go all the way back to the main map
            Unit_Selection_Panel_Phonics_Junior selPanel =
                FindFirstObjectByType<Unit_Selection_Panel_Phonics_Junior>(FindObjectsInactive.Include);
            if (selPanel != null) selPanel.BackToUnitSelection();
            else CloseAllPanels();
        }
    }
}
