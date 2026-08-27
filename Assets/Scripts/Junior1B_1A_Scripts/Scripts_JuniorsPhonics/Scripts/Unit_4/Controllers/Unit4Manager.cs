using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Unit4Manager : MonoBehaviour
{
    [Header("Panels")]
    public GameObject introPanel;
    public GameObject levelSelectionPanel;
    public GameObject activity1Panel; // Meet the Family
    public GameObject activity2Panel; // Swap the Sound
    public GameObject activity3Panel; // Family Sort
    public GameObject rewardPanel;    // Silly Sentence Reward

    [Header("Level Data")]
    public List<Unit4LevelData> levels = new List<Unit4LevelData>();
    private int currentLevelIndex = 0;

    [Header("Controllers")]
    public Activity1_MeetFamilyController meetFamilyController;
    public Activity2_SwapSoundController swapSoundController;
    public Activity3_FamilySortController familySortController;
    public SillySentenceRewardController rewardController;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip introAudio;

    private void Start()
    {
        // Unit 4 only opens when explicitly selected by user from Unit Selection Panel (OpenUnit(4))
    }

    public void ShowIntro()
    {
        StartCoroutine(ShowIntroRoutine());
    }

    private IEnumerator ShowIntroRoutine()
    {
        EnsureInitPanels();
        CloseAllPanels();
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null)
        {
            mascot.ShowMascot();
            mascot.PlayHiAnimation();
        }

        if (introPanel != null)
        {
            EnablePanelAndParents(introPanel);
            BindNextButtons(introPanel);
        }

        float waitTime = 3.5f;
        if (audioSource != null && introAudio != null)
        {
            audioSource.PlayOneShot(introAudio);
            waitTime = introAudio.length + 0.5f;
        }

        yield return new WaitForSeconds(waitTime);

        // After intro instruction finishes, transition to Activity 1
        StartActivity1();
    }

    private void EnsureInitPanels()
    {
        Transform searchRoot = transform;

        if (levelSelectionPanel == null)
        {
            Transform t = searchRoot.Find("Unit_4_Section_Selection_Panels");
            if (t == null) t = searchRoot.Find("Unit_4_Section_Selection_Panel");
            if (t == null) t = searchRoot.Find("Level_Selection_Panel");
            if (t != null) levelSelectionPanel = t.gameObject;
        }

        Transform sectionsParent = searchRoot.Find("Unit_4_Sections");
        if (sectionsParent == null) sectionsParent = searchRoot.Find("Unit4_Sections");
        if (sectionsParent == null) sectionsParent = searchRoot;

        if (introPanel == null)
        {
            Transform t = sectionsParent.Find("Instruction Panel");
            if (t == null) t = sectionsParent.Find("Instruction_Panel");
            if (t == null) t = sectionsParent.Find("Gallery_Panel_Instruction_Panel");
            if (t == null) t = sectionsParent.Find("IntroPanel");
            if (t == null) t = sectionsParent.Find("Instruction");
            if (t != null) introPanel = t.gameObject;
        }

        if (activity1Panel == null)
        {
            Transform t = sectionsParent.Find("SectionA_Activity1_MeetFamilyPanel");
            if (t == null) t = sectionsParent.Find("Activity1Panel");
            if (t == null) t = sectionsParent.Find("Activity1");
            if (t != null) activity1Panel = t.gameObject;
        }

        if (activity2Panel == null)
        {
            Transform t = sectionsParent.Find("Section_B_Activity2_SwapSoundPanel");
            if (t == null) t = sectionsParent.Find("Activity2Panel");
            if (t == null) t = sectionsParent.Find("Activity2");
            if (t != null) activity2Panel = t.gameObject;
        }

        if (activity3Panel == null)
        {
            Transform t = sectionsParent.Find("Section_C_Activity3_FamilySortPanel");
            if (t == null) t = sectionsParent.Find("Activity3Panel");
            if (t == null) t = sectionsParent.Find("Activity3");
            if (t != null) activity3Panel = t.gameObject;
        }

        if (rewardPanel == null)
        {
            Transform t = sectionsParent.Find("Section_D_RewardPanel");
            if (t == null) t = sectionsParent.Find("RewardPanel");
            if (t == null) t = sectionsParent.Find("Reward");
            if (t != null) rewardPanel = t.gameObject;
        }
    }

    public void ShowLevelSelection()
    {
        EnsureInitPanels();
        CloseAllPanels();
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null) mascot.HideMascot();

        // Disable section back button while viewing Section Selection Panel
        SetSectionBackButtonActive(false);

        if (levelSelectionPanel != null)
        {
            EnablePanelAndParents(levelSelectionPanel);
            BindSectionButtons();
        }
        else StartLevel(0);
    }

    private void BindSectionButtons()
    {
        if (levelSelectionPanel == null) return;
        Button[] btns = levelSelectionPanel.GetComponentsInChildren<Button>(true);
        List<Button> validBtns = new List<Button>();
        foreach (var b in btns)
        {
            if (b != null && !b.name.ToLower().Contains("back"))
            {
                validBtns.Add(b);
            }
        }

        for (int i = 0; i < validBtns.Count; i++)
        {
            Button b = validBtns[i];
            string n = b.name.ToLower();

            if (n.Contains("short_a") || n.Contains("shorta") || n.Contains("sectiona") || n.Contains("section_a") || (validBtns.Count >= 2 && i == 0))
            {
                b.onClick.RemoveListener(OpenShortAFamily);
                b.onClick.AddListener(OpenShortAFamily);
            }
            else if (n.Contains("short_e") || n.Contains("shorte") || n.Contains("sectionb") || n.Contains("section_b") || (validBtns.Count >= 2 && i == 1))
            {
                b.onClick.RemoveListener(OpenShortEFamily);
                b.onClick.AddListener(OpenShortEFamily);
            }
        }
    }

    private void SetSectionBackButtonActive(bool active)
    {
        Transform sectionBack = transform.Find("Unit_4_Sections/Back_Button");
        if (sectionBack == null) sectionBack = transform.Find("Back_Button");

        if (sectionBack == null && transform.parent != null)
        {
            sectionBack = transform.parent.Find("Unit_4_Sections/Back_Button");
            if (sectionBack == null) sectionBack = transform.parent.Find("Back_Button");
        }

        if (sectionBack == null)
        {
            GameObject u4Sec = GameObject.Find("Unit_4_Sections");
            if (u4Sec != null)
            {
                Transform b = u4Sec.transform.Find("Back_Button");
                if (b != null) sectionBack = b;
            }
        }

        if (sectionBack != null)
        {
            sectionBack.gameObject.SetActive(active);
        }
    }

    public void OpenShortAFamily() { StartLevel(0); }
    public void OpenShortEFamily() { StartLevel(1); }

    public void StartLevel(int levelIndex)
    {
        if (levelIndex >= 0 && levelIndex < levels.Count)
        {
            currentLevelIndex = levelIndex;

            // Only play intro instruction panel for the first section (Level 0 / Short A)!
            if (levelIndex == 0 && introPanel != null)
            {
                StartCoroutine(ShowIntroRoutine());
            }
            else
            {
                StartActivity1();
            }
        }
    }

    public void SkipToNextActivity()
    {
        if (activity1Panel != null && activity1Panel.activeSelf)
        {
            StartActivity2();
        }
        else if (activity2Panel != null && activity2Panel.activeSelf)
        {
            StartActivity3();
        }
        else if (activity3Panel != null && activity3Panel.activeSelf)
        {
            ShowReward();
        }
    }

    private void BindNextButtons(GameObject panel)
    {
        if (panel == null) return;
        Button[] btns = panel.GetComponentsInChildren<Button>(true);
        foreach (var b in btns)
        {
            if (b == null) continue;
            string n = b.name.ToLower();
            if (n.Contains("next") || n.Contains("skip") || n.Contains("forward"))
            {
                if (panel == rewardPanel)
                {
                    // Completion / Reward Panel Next Button ONLY -> advances section / unit!
                    b.onClick.RemoveListener(SkipToNextActivity);
                    b.onClick.RemoveListener(OnRewardFinished);
                    b.onClick.AddListener(OnRewardFinished);
                }
                else
                {
                    // Activity Skip/Next buttons -> skips to next activity inside current section!
                    b.onClick.RemoveListener(OnRewardFinished);
                    b.onClick.RemoveListener(SkipToNextActivity);
                    b.onClick.AddListener(SkipToNextActivity);
                }
            }
        }
    }

    private void EnablePanelAndParents(GameObject panel)
    {
        EnsureMainBGActive();
        if (panel == null) return;
        panel.SetActive(true);

        // Ensure all child panels, viewports, content containers, and buttons inside panel are activated!
        foreach (Transform child in panel.GetComponentsInChildren<Transform>(true))
        {
            if (child != null && !child.gameObject.activeSelf)
            {
                child.gameObject.SetActive(true);
            }
        }

        Transform p = panel.transform.parent;
        while (p != null)
        {
            if (!p.gameObject.activeSelf)
            {
                p.gameObject.SetActive(true);
            }
            p = p.parent;
        }
    }

    private void StartActivity1()
    {
        CloseAllPanels();
        SetSectionBackButtonActive(true);
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null) mascot.HideMascot();

        if (activity1Panel != null)
        {
            EnablePanelAndParents(activity1Panel);
            BindNextButtons(activity1Panel);
            if (meetFamilyController != null)
            {
                meetFamilyController.OnActivityComplete = OnActivity1Finished;
                meetFamilyController.Setup(levels[currentLevelIndex]);
            }
        }
        else StartActivity2();
    }

    private void StartActivity2()
    {
        CloseAllPanels();
        SetSectionBackButtonActive(true);
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null) mascot.HideMascot();

        if (activity2Panel != null)
        {
            EnablePanelAndParents(activity2Panel);
            BindNextButtons(activity2Panel);
            if (swapSoundController != null)
            {
                swapSoundController.OnActivityComplete = OnActivity2Finished;
                swapSoundController.Setup(levels[currentLevelIndex]);
            }
        }
        else StartActivity3();
    }

    private void StartActivity3()
    {
        CloseAllPanels();
        SetSectionBackButtonActive(true);
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null) mascot.HideMascot();

        if (activity3Panel != null)
        {
            EnablePanelAndParents(activity3Panel);
            BindNextButtons(activity3Panel);
            if (familySortController != null)
            {
                familySortController.OnActivityComplete = OnActivity3Finished;
                familySortController.Setup(levels[currentLevelIndex]);
            }
        }
        else ShowReward();
    }

    private void OnActivity1Finished() { StartActivity2(); }
    private void OnActivity2Finished() { StartActivity3(); }
    private void OnActivity3Finished() { ShowReward(); }

    public void ShowReward()
    {
        CloseAllPanels();
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null)
        {
            mascot.ShowMascot();
            mascot.PlayCelebrationAnimation();
        }

        if (rewardPanel != null)
        {
            EnablePanelAndParents(rewardPanel);
            BindNextButtons(rewardPanel);

            // Hide extra duplicate back buttons if rewardPanel has its own
            Transform mainBack = transform.Find("Section_Selection_Back");
            if (mainBack == null) mainBack = transform.Find("Back_Button");
            Button[] rewardBackBtns = rewardPanel.GetComponentsInChildren<Button>(true);
            if (mainBack != null && rewardBackBtns.Length > 0)
            {
                foreach (var b in rewardBackBtns)
                {
                    if (b.name.ToLower().Contains("back"))
                    {
                        b.gameObject.SetActive(false); // Hide duplicate inside rewardPanel so main back button handles navigation
                    }
                }
            }

            if (rewardController != null && levels.Count > currentLevelIndex)
            {
                rewardController.OnRewardComplete = OnRewardFinished;
                rewardController.Setup(levels[currentLevelIndex]);
            }
        }
    }

    private void OnRewardFinished()
    {
        currentLevelIndex++;
        if (levels != null && currentLevelIndex < levels.Count)
        {
            StartLevel(currentLevelIndex);
        }
        else
        {
            // All sections of Unit 4 completed -> Open Unit 5 Section Selection Panel!
            Unit_Selection_Panel_Phonics_Junior unitSelector = FindFirstObjectByType<Unit_Selection_Panel_Phonics_Junior>(FindObjectsInactive.Include);
            if (unitSelector != null)
            {
                unitSelector.OpenUnit(5);
            }
            else
            {
                ShowLevelSelection();
            }
        }
    }

    public void CloseAllPanels()
    {
        if (introPanel != null) introPanel.SetActive(false);
        if (levelSelectionPanel != null) levelSelectionPanel.SetActive(false);
        if (activity1Panel != null) activity1Panel.SetActive(false);
        if (activity2Panel != null) activity2Panel.SetActive(false);
        if (activity3Panel != null) activity3Panel.SetActive(false);
        if (rewardPanel != null) rewardPanel.SetActive(false);

        EnsureMainBGActive();
    }

    private void EnsureMainBGActive()
    {
        GameObject bgObj = GameObject.Find("Main_BG");
        if (bgObj == null) bgObj = GameObject.Find("Main_Back");
        if (bgObj != null && !bgObj.activeSelf)
        {
            bgObj.SetActive(true);
        }
    }
}
