using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Unit3Manager : MonoBehaviour
{
    [Header("Level Data Set (5 Vowels)")]
    public List<Unit3LevelData> levels = new List<Unit3LevelData>();
    private int currentLevelIndex = 0;

    [Header("Panels")]
    public GameObject introPanel;
    public GameObject levelSelectionPanel;
    public GameObject activity1Panel;
    public GameObject activity2Panel;
    public GameObject activity3Panel;
    public GameObject rewardPanel;
    public GameObject mainBG; // Reference to Main_BG object in Hierarchy

    [Header("Controllers")]
    public Activity1_BlendReadController blendReadController;
    public Activity2_WordHuntController wordHuntController;
    public Activity3_SpellPictureController spellPictureController;

    [Header("Reward UI")]
    public Image badgeDisplayImage;
    public TextMeshProUGUI rewardTitleText;
    public GameObject unitTrophyObject;

    [Header("Intro Audio")]
    public AudioSource audioSource;
    public AudioClip introClip;

    private Coroutine introCoroutine;

    private void Awake()
    {
        AutoLoadLevels();
    }

    private void OnEnable()
    {
        AutoLoadLevels();
        EnsureMainBGActive();
        if (blendReadController != null) blendReadController.OnActivityComplete += OnActivity1Finished;
        if (wordHuntController != null) wordHuntController.OnActivityComplete += OnActivity2Finished;
        if (spellPictureController != null) spellPictureController.OnActivityComplete += OnActivity3Finished;

        bool anyActivityActive = (activity1Panel != null && activity1Panel.activeSelf) ||
                                 (activity2Panel != null && activity2Panel.activeSelf) ||
                                 (activity3Panel != null && activity3Panel.activeSelf) ||
                                 (introPanel != null && introPanel.activeSelf) ||
                                 (rewardPanel != null && rewardPanel.activeSelf);

        if (!anyActivityActive)
        {
            ShowLevelSelection();
        }
    }

    public void AutoLoadLevels()
    {
        if (levels == null) levels = new List<Unit3LevelData>();
        if (levels.Count == 0)
        {
#if UNITY_EDITOR
            string[] levelPaths = new string[]
            {
                "Assets/Data/Unit3/Levels/Level_ShortA.asset",
                "Assets/Data/Unit3/Levels/Level_ShortE.asset",
                "Assets/Data/Unit3/Levels/Level_ShortI.asset",
                "Assets/Data/Unit3/Levels/Level_ShortO.asset",
                "Assets/Data/Unit3/Levels/Level_ShortU.asset"
            };

            foreach (string path in levelPaths)
            {
                Unit3LevelData l = UnityEditor.AssetDatabase.LoadAssetAtPath<Unit3LevelData>(path);
                if (l != null && !levels.Contains(l)) levels.Add(l);
            }
#endif
        }
    }

    private void OnDisable()
    {
        if (blendReadController != null) blendReadController.OnActivityComplete -= OnActivity1Finished;
        if (wordHuntController != null) wordHuntController.OnActivityComplete -= OnActivity2Finished;
        if (spellPictureController != null) spellPictureController.OnActivityComplete -= OnActivity3Finished;

        if (introCoroutine != null) StopCoroutine(introCoroutine);
    }

    public void ShowIntro()
    {
        CloseAllPanels();
        SetSectionBackButtonActive(false);

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null)
        {
            mascot.ShowMascot();
            mascot.PlayHiAnimation();
        }

        if (introPanel != null)
        {
            EnablePanelAndParents(introPanel);
            if (introCoroutine != null) StopCoroutine(introCoroutine);
            introCoroutine = StartCoroutine(AutoIntroRoutine());
        }
        else
        {
            StartActivity1();
        }
    }

    private IEnumerator AutoIntroRoutine()
    {
        float duration = 3.5f;
        if (audioSource != null && introClip != null)
        {
            audioSource.Stop();
            audioSource.PlayOneShot(introClip);
            duration = introClip.length + 0.5f;
        }

        yield return new WaitForSeconds(duration);

        StartActivity1(); // Launch Activity 1!
    }

    public void ShowLevelSelection()
    {
        if (introCoroutine != null) StopCoroutine(introCoroutine);
        CloseAllPanels();
        SetSectionBackButtonActive(false);

        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null) mascot.HideMascot();
        if (levelSelectionPanel != null) EnablePanelAndParents(levelSelectionPanel);
    }

    private void SetSectionBackButtonActive(bool active)
    {
        Transform sectionBack = transform.Find("Unit_3_Sections/Back_Button");
        if (sectionBack == null) sectionBack = transform.Find("Back_Button");

        if (sectionBack == null && transform.parent != null)
        {
            sectionBack = transform.parent.Find("Unit_3_Sections/Back_Button");
            if (sectionBack == null) sectionBack = transform.parent.Find("Back_Button");
        }

        if (sectionBack == null)
        {
            GameObject u3Sec = GameObject.Find("Unit_3_Sections");
            if (u3Sec != null)
            {
                Transform b = u3Sec.transform.Find("Back_Button");
                if (b != null) sectionBack = b;
            }
        }

        if (sectionBack != null)
        {
            sectionBack.gameObject.SetActive(active);
        }
    }

    public void StartLevel(int levelIndex)
    {
        if (introCoroutine != null) StopCoroutine(introCoroutine);
        currentLevelIndex = Mathf.Clamp(levelIndex, 0, Mathf.Max(0, levels.Count - 1));

        // Play intro instruction panel only for the first section (Level 0 / Section A)!
        if (currentLevelIndex == 0)
        {
            ShowIntro();
        }
        else
        {
            StartActivity1();
        }
    }

    public void OpenSectionA() { ShowIntro(); } // Trigger Intro speech, then auto-launch Activity 1
    public void OpenSectionB() { StartActivity2(); }
    public void OpenSectionC() { StartActivity3(); }
    public void OpenSectionD() { ShowReward(); }

    public void StartActivity1()
    {
        CloseAllPanels();
        SetSectionBackButtonActive(true);
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null) mascot.HideMascot();

        if (activity1Panel != null)
        {
            EnablePanelAndParents(activity1Panel);
            if (blendReadController != null && levels.Count > currentLevelIndex && levels[currentLevelIndex] != null)
            {
                blendReadController.Setup(levels[currentLevelIndex]);
            }
        }
    }

    public void StartActivity2()
    {
        CloseAllPanels();
        SetSectionBackButtonActive(true);
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null) mascot.HideMascot();

        if (activity2Panel != null)
        {
            EnablePanelAndParents(activity2Panel);
            if (wordHuntController != null && levels.Count > currentLevelIndex && levels[currentLevelIndex] != null)
            {
                wordHuntController.Setup(levels[currentLevelIndex]);
            }
        }
    }

    public void StartActivity3()
    {
        CloseAllPanels();
        SetSectionBackButtonActive(true);
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null) mascot.HideMascot();

        if (activity3Panel != null)
        {
            EnablePanelAndParents(activity3Panel);
            if (spellPictureController != null && levels.Count > currentLevelIndex && levels[currentLevelIndex] != null)
            {
                spellPictureController.Setup(levels[currentLevelIndex]);
            }
        }
        else
        {
            ShowReward();
        }
    }

    private void OnActivity1Finished() { StartActivity2(); }
    private void OnActivity2Finished() { StartActivity3(); }
    private void OnActivity3Finished() { ShowReward(); }

    public void OnRewardFinished()
    {
        currentLevelIndex++;
        if (levels != null && currentLevelIndex < levels.Count)
        {
            StartLevel(currentLevelIndex);
        }
        else
        {
            // All sections of Unit 3 completed -> Open Unit 4 Section Selection Panel!
            Unit_Selection_Panel_Phonics_Junior unitSelector = FindFirstObjectByType<Unit_Selection_Panel_Phonics_Junior>(FindObjectsInactive.Include);
            if (unitSelector != null)
            {
                unitSelector.OpenUnit(4);
            }
            else
            {
                ShowLevelSelection();
            }
        }
    }

    public void SkipToNextActivity()
    {
        if (activity1Panel != null && activity1Panel.activeSelf) StartActivity2();
        else if (activity2Panel != null && activity2Panel.activeSelf) StartActivity3();
        else if (activity3Panel != null && activity3Panel.activeSelf) ShowReward();
    }

    public void ShowReward()
    {
        CloseAllPanels();
        MascotController_Phonics_Junior mascot = FindFirstObjectByType<MascotController_Phonics_Junior>(FindObjectsInactive.Include);
        if (mascot != null)
        {
            mascot.ShowMascot();
            mascot.PlayCelebrationAnimation(); // Mascot celebrates on reward panel!
        }

        if (rewardPanel != null)
        {
            EnablePanelAndParents(rewardPanel);

            // Check if final level (Short U - Level 5) is completed!
            bool isUnitComplete = (currentLevelIndex >= levels.Count - 1);

            if (isUnitComplete)
            {
                // Final Level (Short U Completion): Show Grand Trophy Image & "Short a, e, i, o, u Completed!"
                if (badgeDisplayImage != null) badgeDisplayImage.gameObject.SetActive(false);
                if (unitTrophyObject != null) unitTrophyObject.SetActive(true);

                if (rewardTitleText != null)
                {
                    rewardTitleText.gameObject.SetActive(true);
                    rewardTitleText.text = "Short a, e, i, o, u Completed!";
                }
            }
            else
            {
                // Level Completion (Short A, E, I, O): Show Star Badge & Level Title Text
                if (unitTrophyObject != null) unitTrophyObject.SetActive(false);

                if (levels.Count > currentLevelIndex && levels[currentLevelIndex] != null)
                {
                    Unit3LevelData currentLevel = levels[currentLevelIndex];
                    if (badgeDisplayImage != null)
                    {
                        badgeDisplayImage.gameObject.SetActive(true);
                        if (currentLevel.vowelBadge != null)
                            badgeDisplayImage.sprite = currentLevel.vowelBadge;
                    }

                    if (rewardTitleText != null)
                    {
                        rewardTitleText.gameObject.SetActive(true);
                        rewardTitleText.text = $"{currentLevel.vowelName} Badge Earned!";
                    }
                }
            }
        }
    }

    private void CloseAllPanels()
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

    private void EnablePanelAndParents(GameObject panel)
    {
        EnsureMainBGActive();
        if (panel == null) return;
        panel.SetActive(true);

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

    private void EnsureMainBGActive()
    {
        if (mainBG != null && !mainBG.activeSelf)
        {
            mainBG.SetActive(true);
        }
        else if (mainBG == null)
        {
            GameObject bgObj = GameObject.Find("Main_BG");
            if (bgObj == null) bgObj = GameObject.Find("Main_Back");
            if (bgObj != null)
            {
                mainBG = bgObj;
                mainBG.SetActive(true);
            }
        }
    }
}