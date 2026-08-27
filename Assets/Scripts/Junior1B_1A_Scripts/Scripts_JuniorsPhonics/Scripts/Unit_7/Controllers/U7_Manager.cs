using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class U7_Manager : MonoBehaviour
{
    [Header("Level Data Assets")]
    public U7_LevelData levelLongI;
    public U7_LevelData levelLongO;
    public U7_LevelData levelLongU;

    [Header("UI Panels")]
    public GameObject levelSelectionPanel;
    public GameObject galleryPanel;
    public GameObject activity1Panel;
    public GameObject activity2Panel;
    public GameObject activity3Panel;
    public GameObject rewardPanel;

    [Header("Controllers")]
    public U7_GalleryController galleryController;
    public U7_A1_MeetTeamsController a1Controller;
    public U7_A2_PictureMatchController a2Controller;
    public U7_A3_TeamSortController a3Controller;
    public U7_RewardController rewardController;

    [Header("Navigation Buttons")]
    public Button backToUnitSelectionBtn;
    public Button nextActivityBtn;
    public Button skipActivityBtn;

    [HideInInspector] public U7_LevelData activeLevel;

    private void Awake()
    {
        AutoBindPanels();
        if (backToUnitSelectionBtn != null) backToUnitSelectionBtn.onClick.AddListener(OnBackButtonClicked);
        if (nextActivityBtn != null) nextActivityBtn.onClick.AddListener(OnNextActivityClicked);
        if (skipActivityBtn != null) skipActivityBtn.onClick.AddListener(OnNextActivityClicked);
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

        Transform sections = unitRoot.Find("Unit_7_Sections");
        if (sections == null) sections = transform.Find("Unit_7_Sections");
        Transform searchRoot = sections != null ? sections : unitRoot;

        if (levelSelectionPanel == null)
        {
            string[] names = {
                "Unit_7_Section_Selection_Panels", "Unit_7_Section_Selection_Panel",
                "Unit 7 Section Selection Panels", "Unit 7 Section Selection Panel",
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

        if (galleryController == null && galleryPanel != null) galleryController = galleryPanel.GetComponent<U7_GalleryController>();
        if (a1Controller == null && activity1Panel != null) a1Controller = activity1Panel.GetComponent<U7_A1_MeetTeamsController>();
        if (a2Controller == null && activity2Panel != null) a2Controller = activity2Panel.GetComponent<U7_A2_PictureMatchController>();
        if (a3Controller == null && activity3Panel != null) a3Controller = activity3Panel.GetComponent<U7_A3_TeamSortController>();
        if (rewardController == null && rewardPanel != null) rewardController = rewardPanel.GetComponent<U7_RewardController>();
    }

    private void EnablePanel(GameObject panelObj)
    {
        if (panelObj == null) return;

        panelObj.SetActive(true);

        foreach (Transform child in panelObj.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && !child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(true);
            }
        }

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
                        
                        // Fail-safe: ensure it is wired to advance the activity if missed in inspector
                        b.onClick.RemoveListener(OnNextActivityClicked);
                        b.onClick.AddListener(OnNextActivityClicked);
                    }
                }
            }
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

        if (a1Controller != null) a1Controller.HideNavButtons();
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

        Transform sections = transform.Find("Unit_7_Sections");
        if (sections == null && transform.parent != null) sections = transform.parent.Find("Unit_7_Sections");
        if (sections != null) sections.gameObject.SetActive(false);
    }

    public void ShowIntroGallery()
    {
        CloseAllPanels();
        EnablePanel(galleryPanel);
        if (galleryController != null)
            galleryController.SetupGalleryAll(levelLongI, levelLongO, levelLongU);
        SetNextButtonState(true); // Gallery always allows advancing
    }

    public void StartLevel1LongI()
    {
        activeLevel = levelLongI;
        StartActivity1();
    }

    public void StartLevel2LongO()
    {
        activeLevel = levelLongO;
        StartActivity1();
    }

    public void StartLevel3LongU()
    {
        activeLevel = levelLongU;
        StartActivity1();
    }

    public void StartActivity1()
    {
        CloseAllPanels();
        EnablePanel(activity1Panel);
        if (a1Controller != null && activeLevel != null) a1Controller.SetupActivity(activeLevel);
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
        else ShowLevelSelection();
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
