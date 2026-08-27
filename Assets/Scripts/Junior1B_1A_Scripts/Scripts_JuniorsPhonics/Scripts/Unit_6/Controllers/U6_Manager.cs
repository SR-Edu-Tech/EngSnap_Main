using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class U6_Manager : MonoBehaviour
{
    [Header("Level Data Assets")]
    public U6_LevelData levelLongA;
    public U6_LevelData levelLongE;

    [Header("UI Panels")]
    public GameObject levelSelectionPanel;
    public GameObject galleryPanel;
    public GameObject activity1Panel;
    public GameObject activity2Panel;
    public GameObject activity3Panel;
    public GameObject rewardPanel;

    [Header("Controllers")]
    public U6_GalleryController galleryController;
    public U6_A1_MeetTeamsController a1Controller;
    public U6_A2_PictureMatchController a2Controller;
    public U6_A3_TeamSortController a3Controller;
    public U6_RewardController rewardController;

    [Header("Navigation Buttons")]
    public Button backToUnitSelectionBtn;
    public Button nextActivityBtn;
    public Button skipActivityBtn;

    [HideInInspector] public U6_LevelData activeLevel;

    private void Awake()
    {
        AutoBindPanels();
        if (backToUnitSelectionBtn != null) backToUnitSelectionBtn.onClick.AddListener(OnBackButtonClicked);
        if (nextActivityBtn != null) nextActivityBtn.onClick.AddListener(OnNextActivityClicked);
        if (skipActivityBtn != null) skipActivityBtn.onClick.AddListener(OnNextActivityClicked);
    }

    public void SetNextButtonState(bool enabled)
    {
        if (nextActivityBtn != null)
        {
            nextActivityBtn.gameObject.SetActive(enabled);
            nextActivityBtn.interactable = enabled;
        }

        if (skipActivityBtn != null)
        {
            skipActivityBtn.gameObject.SetActive(enabled);
            skipActivityBtn.interactable = enabled;
        }

        GameObject[] activePanels = { activity1Panel, activity2Panel, activity3Panel, galleryPanel };
        foreach (var panel in activePanels)
        {
            if (panel != null && panel.activeSelf)
            {
                Button[] btns = panel.GetComponentsInChildren<Button>(true);
                foreach (var b in btns)
                {
                    if (b != null && b.name.ToLower().Contains("next"))
                    {
                        b.gameObject.SetActive(enabled);
                        b.interactable = enabled;
                    }
                }
            }
        }
    }

    private void Start()
    {
        AutoBindPanels();
    }

    public void AutoBindPanels()
    {
        Transform unitRoot = transform;
        while (unitRoot.parent != null && unitRoot.parent.name != "Canvas")
        {
            unitRoot = unitRoot.parent;
        }

        Transform sections = unitRoot.Find("Unit_6_Sections");
        if (sections == null) sections = transform.Find("Unit_6_Sections");
        Transform searchRoot = sections != null ? sections : unitRoot;

        if (levelSelectionPanel == null)
        {
            string[] names = {
                "Unit_6_Section_Selection_Panels", "Unit_6_Section_Selection_Panel",
                "Unit 6 Section Selection Panels", "Unit 6 Section Selection Panel",
                "Section_Selection_Panels", "Section_Selection_Panel",
                "Section Selection Panels", "Section Selection Panel",
                "Level_Selection_Panel", "Level Selection Panel", "Level_Selection_Panels"
            };

            foreach (string n in names)
            {
                Transform t = unitRoot.Find(n);
                if (t == null) t = transform.Find(n);
                if (t != null)
                {
                    levelSelectionPanel = t.gameObject;
                    break;
                }
            }

            if (levelSelectionPanel == null)
            {
                foreach (Transform child in unitRoot)
                {
                    if (child != null && !child.name.ToLower().Contains("sections"))
                    {
                        levelSelectionPanel = child.gameObject;
                        break;
                    }
                }
            }
        }

        if (galleryPanel == null)
        {
            Transform t = searchRoot.Find("Gallery_Panel_Instruction_Panel");
            if (t == null) t = searchRoot.Find("Instruction_Panel_Gallery");
            if (t == null) t = searchRoot.Find("Instruction Panel");
            if (t == null) t = searchRoot.Find("Gallery_Panel");
            if (t != null) galleryPanel = t.gameObject;
        }

        if (activity1Panel == null)
        {
            Transform t = searchRoot.Find("Activity1_Panel");
            if (t == null) t = searchRoot.Find("Activity_1_Panel");
            if (t != null) activity1Panel = t.gameObject;
        }

        if (activity2Panel == null)
        {
            Transform t = searchRoot.Find("Section_B_Activity2_SwapSoundPanel");
            if (t == null) t = searchRoot.Find("Activity_2_Panel");
            if (t != null) activity2Panel = t.gameObject;
        }

        if (activity3Panel == null)
        {
            Transform t = searchRoot.Find("Section_C_Activity3_FamilySortPanel");
            if (t == null) t = searchRoot.Find("Activity_3_Panel");
            if (t != null) activity3Panel = t.gameObject;
        }

        if (rewardPanel == null)
        {
            Transform t = searchRoot.Find("Section_D_RewardPanel");
            if (t == null) t = searchRoot.Find("Reward_Panel");
            if (t != null) rewardPanel = t.gameObject;
        }

        if (galleryController == null && galleryPanel != null) galleryController = galleryPanel.GetComponent<U6_GalleryController>();
        if (a1Controller == null && activity1Panel != null) a1Controller = activity1Panel.GetComponent<U6_A1_MeetTeamsController>();
        if (a2Controller == null && activity2Panel != null) a2Controller = activity2Panel.GetComponent<U6_A2_PictureMatchController>();
        if (a3Controller == null && activity3Panel != null) a3Controller = activity3Panel.GetComponent<U6_A3_TeamSortController>();
        if (rewardController == null && rewardPanel != null) rewardController = rewardPanel.GetComponent<U6_RewardController>();
    }

    private void EnablePanel(GameObject panelObj)
    {
        if (panelObj == null) return;

        // 1. Enable target panel
        panelObj.SetActive(true);

        // 2. Ensure child graphics, text, and buttons inside panelObj are ALSO enabled!
        foreach (Transform child in panelObj.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && !child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(true);
            }
        }

        // 3. Ensure parent containers are ALSO enabled up to Canvas
        Transform current = panelObj.transform.parent;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }
            current = current.parent;
        }
    }

    public void CloseAllPanels()
    {
        if (levelSelectionPanel != null) levelSelectionPanel.SetActive(false);
        if (galleryPanel != null) galleryPanel.SetActive(false);
        if (activity1Panel != null) activity1Panel.SetActive(false);
        if (activity2Panel != null) activity2Panel.SetActive(false);
        if (activity3Panel != null) activity3Panel.SetActive(false);
        if (rewardPanel != null) rewardPanel.SetActive(false);

        if (a1Controller != null)
        {
            a1Controller.HideNavButtons();
        }
    }

    public void ShowLevelSelection()
    {
        CloseAllPanels();
        EnablePanel(levelSelectionPanel);

        if (levelSelectionPanel != null)
        {
            Transform content = levelSelectionPanel.transform.Find("Viewport/Content");
            if (content == null) content = levelSelectionPanel.transform.Find("Content");
            if (content != null)
            {
                foreach (Transform child in content)
                {
                    if (child != null && !child.gameObject.activeSelf)
                    {
                        child.gameObject.SetActive(true);
                    }
                }
            }
        }

        // Disable Unit_6_Sections so activity sub-panels hide cleanly!
        Transform sections = transform.Find("Unit_6_Sections");
        if (sections == null && transform.parent != null) sections = transform.parent.Find("Unit_6_Sections");
        if (sections != null) sections.gameObject.SetActive(false);
    }

    public void ShowIntroGallery()
    {
        CloseAllPanels();
        EnablePanel(galleryPanel);
        if (galleryController != null)
        {
            galleryController.SetupGalleryAll(levelLongA, levelLongE);
        }
        SetNextButtonState(true);
    }

    public void StartLevel1LongA()
    {
        activeLevel = levelLongA;
        ShowIntroGallery(); // Section A starts with Intro Gallery warm-up!
    }

    public void StartLevel2LongE()
    {
        activeLevel = levelLongE;
        StartActivity1();   // Section E skips Gallery and starts directly with Activity 1!
    }

    public void StartActivity1()
    {
        CloseAllPanels();
        EnablePanel(activity1Panel);
        if (a1Controller != null && activeLevel != null) a1Controller.SetupActivity(activeLevel);
    }

    public void OnActivity1Complete()
    {
        StartActivity2();
    }

    public void StartActivity2()
    {
        CloseAllPanels();
        EnablePanel(activity2Panel);
        if (a2Controller != null && activeLevel != null) a2Controller.SetupActivity(activeLevel);
        SetNextButtonState(false); // Disabled until all picture matches complete!
    }

    public void StartActivity3()
    {
        CloseAllPanels();
        EnablePanel(activity3Panel);
        if (a3Controller != null && activeLevel != null) a3Controller.SetupActivity(activeLevel);
        SetNextButtonState(false); // Disabled until all word sorting completes!
    }

    public void ShowReward()
    {
        CloseAllPanels();
        EnablePanel(rewardPanel);
        if (rewardController != null && activeLevel != null) rewardController.SetupReward(activeLevel);
    }

    public void OnNextActivityClicked()
    {
        if (galleryPanel != null && galleryPanel.activeSelf) StartActivity1();
        else if (activity1Panel != null && activity1Panel.activeSelf) StartActivity2();
        else if (activity2Panel != null && activity2Panel.activeSelf) StartActivity3();
        else if (activity3Panel != null && activity3Panel.activeSelf) ShowReward();
        else if (rewardPanel != null && rewardPanel.activeSelf) OnRewardFinished();
        else ShowLevelSelection();
    }

    public void OnRewardFinished()
    {
        if (activeLevel == levelLongA)
        {
            // Completed Section A (Long A) -> Return to Unit 6 Section Selection Panel!
            ShowLevelSelection();
        }
        else
        {
            // All sections of Unit 6 completed (Long E) -> Open Unit 7 Section Selection Panel!
            Unit_Selection_Panel_Phonics_Junior unitSelector = FindFirstObjectByType<Unit_Selection_Panel_Phonics_Junior>(FindObjectsInactive.Include);
            if (unitSelector != null)
            {
                unitSelector.OpenUnit(7);
            }
            else
            {
                ShowLevelSelection();
            }
        }
    }

    public void OnBackButtonClicked()
    {
        // If inside an activity section, return to section selection panel
        if ((activity1Panel != null && activity1Panel.activeSelf) ||
            (activity2Panel != null && activity2Panel.activeSelf) ||
            (activity3Panel != null && activity3Panel.activeSelf) ||
            (galleryPanel != null && galleryPanel.activeSelf) ||
            (rewardPanel != null && rewardPanel.activeSelf))
        {
            ShowLevelSelection();
        }
        else
        {
            // If on section selection board, return to main unit selection
            Unit_Selection_Panel_Phonics_Junior selectionPanel = FindFirstObjectByType<Unit_Selection_Panel_Phonics_Junior>(FindObjectsInactive.Include);
            if (selectionPanel != null)
            {
                selectionPanel.BackToUnitSelection();
            }
            else
            {
                CloseAllPanels();
            }
        }
    }
}
