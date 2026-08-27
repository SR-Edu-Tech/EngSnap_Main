using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Unit5Manager : MonoBehaviour
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
        // Unit 5 only opens when explicitly selected by user from Unit Selection Panel (OpenUnit(5))
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

        StartActivity1();
    }

    private void EnsureInitPanels()
    {
        Transform searchRoot = transform;

        if (levelSelectionPanel == null)
        {
            Transform t = searchRoot.Find("Unit_5_Section_Selection_Panels");
            if (t == null) t = searchRoot.Find("Unit_5_Section_Selection_Panel");
            if (t == null) t = searchRoot.Find("Level_Selection_Panel");
            if (t != null) levelSelectionPanel = t.gameObject;
        }

        Transform sectionsParent = searchRoot.Find("Unit_5_Sections");
        if (sectionsParent == null) sectionsParent = searchRoot.Find("Unit5_Sections");
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

            if (n.Contains("short_i") || n.Contains("shorti") || n.Contains("sectiona") || n.Contains("section_a") || (validBtns.Count == 3 && i == 0))
            {
                b.onClick.RemoveListener(OpenShortIFamily);
                b.onClick.AddListener(OpenShortIFamily);
            }
            else if (n.Contains("short_o") || n.Contains("shorto") || n.Contains("sectionb") || n.Contains("section_b") || (validBtns.Count == 3 && i == 1))
            {
                b.onClick.RemoveListener(OpenShortOFamily);
                b.onClick.AddListener(OpenShortOFamily);
            }
            else if (n.Contains("short_u") || n.Contains("shortu") || n.Contains("sectionc") || n.Contains("section_c") || (validBtns.Count == 3 && i == 2))
            {
                b.onClick.RemoveListener(OpenShortUFamily);
                b.onClick.AddListener(OpenShortUFamily);
            }
        }
    }

    private void SetSectionBackButtonActive(bool active)
    {
        Transform sectionBack = transform.Find("Unit_5_Sections/Back_Button");
        if (sectionBack == null) sectionBack = transform.Find("Back_Button");

        if (sectionBack == null && transform.parent != null)
        {
            sectionBack = transform.parent.Find("Unit_5_Sections/Back_Button");
            if (sectionBack == null) sectionBack = transform.parent.Find("Back_Button");
        }

        if (sectionBack == null)
        {
            GameObject u5Sec = GameObject.Find("Unit_5_Sections");
            if (u5Sec != null)
            {
                Transform b = u5Sec.transform.Find("Back_Button");
                if (b != null) sectionBack = b;
            }
        }

        if (sectionBack != null)
        {
            sectionBack.gameObject.SetActive(active);
        }
    }

    private void EnsureLevelsLoaded()
    {
        if (levels == null) levels = new List<Unit4LevelData>();
        if (levels.Count == 0 || levels[0] == null)
        {
            levels.Clear();
#if UNITY_EDITOR
            string[] g0 = UnityEditor.AssetDatabase.FindAssets("Level_Short_i_families t:Unit4LevelData");
            if (g0.Length > 0) levels.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<Unit4LevelData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g0[0])));

            string[] g1 = UnityEditor.AssetDatabase.FindAssets("Level_Short_o_families t:Unit4LevelData");
            if (g1.Length > 0) levels.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<Unit4LevelData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g1[0])));

            string[] g2 = UnityEditor.AssetDatabase.FindAssets("Level_Short_u_families t:Unit4LevelData");
            if (g2.Length > 0) levels.Add(UnityEditor.AssetDatabase.LoadAssetAtPath<Unit4LevelData>(UnityEditor.AssetDatabase.GUIDToAssetPath(g2[0])));
#endif
        }
    }

    public void StartLevel(int levelIndex)
    {
        EnsureInitPanels();
        EnsureLevelsLoaded();
        if (levelIndex >= 0 && (levels.Count == 0 || levelIndex < levels.Count))
        {
            currentLevelIndex = levelIndex;

            if (levelIndex == 0)
            {
                StartCoroutine(ShowIntroRoutine());
            }
            else
            {
                StartActivity1();
            }
        }
    }

    public void OpenShortIFamily() { StartLevel(0); }
    public void OpenShortOFamily() { StartLevel(1); }
    public void OpenShortUFamily() { StartLevel(2); }

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

    private void EnsureInitControllers()
    {
        if (meetFamilyController == null && activity1Panel != null)
            meetFamilyController = activity1Panel.GetComponentInChildren<Activity1_MeetFamilyController>(true);
        if (meetFamilyController == null)
            meetFamilyController = FindFirstObjectByType<Activity1_MeetFamilyController>(FindObjectsInactive.Include);

        if (swapSoundController == null && activity2Panel != null)
            swapSoundController = activity2Panel.GetComponentInChildren<Activity2_SwapSoundController>(true);
        if (swapSoundController == null)
            swapSoundController = FindFirstObjectByType<Activity2_SwapSoundController>(FindObjectsInactive.Include);

        if (familySortController == null && activity3Panel != null)
            familySortController = activity3Panel.GetComponentInChildren<Activity3_FamilySortController>(true);
        if (familySortController == null)
            familySortController = FindFirstObjectByType<Activity3_FamilySortController>(FindObjectsInactive.Include);

        if (rewardController == null && rewardPanel != null)
            rewardController = rewardPanel.GetComponentInChildren<SillySentenceRewardController>(true);
        if (rewardController == null)
            rewardController = FindFirstObjectByType<SillySentenceRewardController>(FindObjectsInactive.Include);
    }

    private void StartActivity1()
    {
        CloseAllPanels();
        SetSectionBackButtonActive(true);
        EnsureInitControllers();

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null) mascot.HideMascot();

        if (activity1Panel != null)
        {
            EnablePanelAndParents(activity1Panel);
            BindNextButtons(activity1Panel);
            if (meetFamilyController != null && levels != null && levels.Count > currentLevelIndex && levels[currentLevelIndex] != null)
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
        EnsureInitControllers();

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null) mascot.HideMascot();

        if (activity2Panel != null)
        {
            EnablePanelAndParents(activity2Panel);
            BindNextButtons(activity2Panel);
            if (swapSoundController != null && levels != null && levels.Count > currentLevelIndex && levels[currentLevelIndex] != null)
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
        EnsureInitControllers();

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>();
        if (mascot != null) mascot.HideMascot();

        if (activity3Panel != null)
        {
            EnablePanelAndParents(activity3Panel);
            BindNextButtons(activity3Panel);
            if (familySortController != null && levels != null && levels.Count > currentLevelIndex && levels[currentLevelIndex] != null)
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
        EnsureInitControllers();

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

            // Enforce large readable typography on all TextMeshProUGUI components in Unit 5 Reward Panel!
            TextMeshProUGUI[] rewardTexts = rewardPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in rewardTexts)
            {
                if (t != null)
                {
                    t.enableAutoSizing = true;
                    t.fontSizeMin = 45;
                    t.fontSizeMax = 84;
                    t.fontSize = 78;
                    t.fontStyle = FontStyles.Bold;
                }
            }

            Transform mainBack = transform.Find("Section_Selection_Back");
            if (mainBack == null) mainBack = transform.Find("Back_Button");
            Button[] rewardBackBtns = rewardPanel.GetComponentsInChildren<Button>(true);
            if (mainBack != null && rewardBackBtns.Length > 0)
            {
                foreach (var b in rewardBackBtns)
                {
                    if (b.name.ToLower().Contains("back"))
                    {
                        b.gameObject.SetActive(false);
                    }
                }
            }

            if (rewardController != null && levels != null && levels.Count > currentLevelIndex && levels[currentLevelIndex] != null)
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
            // All sections of Unit 5 completed -> Open Unit 6 Section Selection Panel!
            Unit_Selection_Panel_Phonics_Junior unitSelector = FindFirstObjectByType<Unit_Selection_Panel_Phonics_Junior>(FindObjectsInactive.Include);
            if (unitSelector != null)
            {
                unitSelector.OpenUnit(6);
            }
            else
            {
                ShowLevelSelection();
            }
        }
    }

    public void CloseAllPanels()
    {
        SetSectionBackButtonActive(false);
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
