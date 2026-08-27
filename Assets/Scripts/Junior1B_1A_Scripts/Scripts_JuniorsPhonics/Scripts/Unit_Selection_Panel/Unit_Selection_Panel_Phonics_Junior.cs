using UnityEngine;

public class Unit_Selection_Panel_Phonics_Junior : MonoBehaviour
{
    [Header("Main Panels")]
    [SerializeField] private GameObject lessonsPanel; // "Lessons" signboard panel

    [Header("Unit 1")]
    [SerializeField] private GameObject unit1Parent; // "Unit_1" parent object in Hierarchy
    [SerializeField] private GameObject unit1SectionSelectionPanel; // "Unit_1_Section_Selection_Panels"
    [SerializeField] private GameObject unit1SectionsParent; // "Unit_1_Sections" parent container
    [SerializeField] private GameObject[] sectionPanels; // Unit 1 Sections (A, B, C, D)

    [Header("Unit 2")]
    [SerializeField] private GameObject unit2Parent; // "Unit_2" parent object in Hierarchy
    [SerializeField] private GameObject unit2SectionSelectionPanel; // "Unit_2_Section_Selection_Panels"
    [SerializeField] private GameObject unit2SectionsParent; // "Unit_2_Sections" parent container
    [SerializeField] private GameObject[] unit2SectionPanelsArray; // Unit 2 Sections (A, B, C, D)


    [Header("Unit 3")]
    [SerializeField] private GameObject unit3Parent; // "Unit_3" parent object in Hierarchy
    [SerializeField] private GameObject unit3LevelSelectionPanel; // "Unit_3_Level_Selection_Panel"

    [Header("Unit 4")]
    [SerializeField] private GameObject unit4Parent; // "Unit_4" parent object in Hierarchy
    [SerializeField] private GameObject unit4SectionSelectionPanel; // "Unit_4_Section_Selection_Panels"

    [Header("Unit 5")]
    [SerializeField] private GameObject unit5Parent;
    [SerializeField] private GameObject unit5SectionSelectionPanel;

    [Header("Unit 6")]
    [SerializeField] private GameObject unit6Parent;
    [SerializeField] private GameObject unit6SectionSelectionPanel;

    [Header("Unit 7")]
    [SerializeField] private GameObject unit7Parent;
    [SerializeField] private GameObject unit7SectionSelectionPanel;

    [Header("Unit 8")]
    [SerializeField] private GameObject unit8Parent;
    [SerializeField] private GameObject unit8SectionSelectionPanel;

    [Header("Unit 9")]
    [SerializeField] private GameObject unit9Parent;
    [SerializeField] private GameObject unit9SectionSelectionPanel;

    [Header("Unit 10")]
    [SerializeField] private GameObject unit10Parent;
    [SerializeField] private GameObject unit10SectionSelectionPanel;

    [Header("Audio")]
    [SerializeField] private AudioClip selectUnitClip;
    [SerializeField] private AudioClip selectLessonClip;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip popClip;

    private void Start()
    {
        EnsureInitUnitParents();
        if (lessonsPanel != null) lessonsPanel.SetActive(true);

        CloseAllUnits();

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(PlayGuideAfterDelay(selectUnitClip, 0.1f));
        }
    }

    private void EnsureInitUnitParents()
    {
        Transform canvasTransform = transform.parent;
        if (canvasTransform == null)
        {
            Canvas c = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (c != null) canvasTransform = c.transform;
        }

        if (canvasTransform == null) return;

        if (lessonsPanel == null)
        {
            Transform lObj = canvasTransform.Find("Lessons");
            if (lObj != null) lessonsPanel = lObj.gameObject;
        }

        // 2. Find Unit_1 to Unit_5 top-level containers under Canvas (including inactive!)
        Transform u1 = canvasTransform.Find("Unit_1");
        if (u1 == null) u1 = canvasTransform.Find("Unit1");
        if (u1 != null && (unit1Parent == null || (lessonsPanel != null && unit1Parent.transform.IsChildOf(lessonsPanel.transform)))) unit1Parent = u1.gameObject;

        Transform u2 = canvasTransform.Find("Unit_2");
        if (u2 == null) u2 = canvasTransform.Find("Unit2");
        if (u2 != null && (unit2Parent == null || (lessonsPanel != null && unit2Parent.transform.IsChildOf(lessonsPanel.transform)))) unit2Parent = u2.gameObject;

        Transform u3 = canvasTransform.Find("Unit_3");
        if (u3 == null) u3 = canvasTransform.Find("Unit3");
        if (u3 != null && (unit3Parent == null || (lessonsPanel != null && unit3Parent.transform.IsChildOf(lessonsPanel.transform)))) unit3Parent = u3.gameObject;

        Transform u4 = canvasTransform.Find("Unit_4");
        if (u4 == null) u4 = canvasTransform.Find("Unit4");
        if (u4 != null && (unit4Parent == null || (lessonsPanel != null && unit4Parent.transform.IsChildOf(lessonsPanel.transform)))) unit4Parent = u4.gameObject;

        Transform u5 = canvasTransform.Find("Unit_5");
        if (u5 == null) u5 = canvasTransform.Find("Unit5");
        if (u5 != null && (unit5Parent == null || (lessonsPanel != null && unit5Parent.transform.IsChildOf(lessonsPanel.transform)))) unit5Parent = u5.gameObject;

        Transform u6 = canvasTransform.Find("Unit_6");
        if (u6 == null) u6 = canvasTransform.Find("Unit 6");
        if (u6 == null) u6 = canvasTransform.Find("Unit6");
        if (u6 == null) u6 = canvasTransform.Find("UNIT_6");
        if (u6 == null) u6 = canvasTransform.Find("UNIT 6");
        if (u6 != null && (unit6Parent == null || (lessonsPanel != null && unit6Parent.transform.IsChildOf(lessonsPanel.transform)))) unit6Parent = u6.gameObject;

        Transform u7 = canvasTransform.Find("Unit_7");
        if (u7 == null) u7 = canvasTransform.Find("Unit 7");
        if (u7 == null) u7 = canvasTransform.Find("Unit7");
        if (u7 == null) u7 = canvasTransform.Find("UNIT_7");
        if (u7 == null) u7 = canvasTransform.Find("UNIT 7");
        if (u7 != null && (unit7Parent == null || (lessonsPanel != null && unit7Parent.transform.IsChildOf(lessonsPanel.transform)))) unit7Parent = u7.gameObject;

        Transform u8 = canvasTransform.Find("Unit_8");
        if (u8 == null) u8 = canvasTransform.Find("Unit8");
        if (u8 != null && (unit8Parent == null || (lessonsPanel != null && unit8Parent.transform.IsChildOf(lessonsPanel.transform)))) unit8Parent = u8.gameObject;

        Transform u9 = canvasTransform.Find("Unit_9");
        if (u9 == null) u9 = canvasTransform.Find("Unit9");
        if (u9 != null && (unit9Parent == null || (lessonsPanel != null && unit9Parent.transform.IsChildOf(lessonsPanel.transform)))) unit9Parent = u9.gameObject;

        Transform u10 = canvasTransform.Find("Unit_10");
        if (u10 == null) u10 = canvasTransform.Find("Unit10");
        if (u10 != null && (unit10Parent == null || (lessonsPanel != null && unit10Parent.transform.IsChildOf(lessonsPanel.transform)))) unit10Parent = u10.gameObject;

        // 3. Auto-bind section selection panels for each unit (including inactive!)
        if (unit1Parent != null && unit1SectionSelectionPanel == null)
        {
            Transform t = unit1Parent.transform.Find("Unit_1_Section_Selection_Panels");
            if (t == null) t = unit1Parent.transform.Find("Unit_1_Section_Selection_Panel");
            if (t == null) t = unit1Parent.transform.Find("Section_Selection_Panels");
            if (t != null) unit1SectionSelectionPanel = t.gameObject;
        }

        if (unit2Parent != null && unit2SectionSelectionPanel == null)
        {
            Transform t = unit2Parent.transform.Find("Unit_2_Section_Selection_Panels");
            if (t == null) t = unit2Parent.transform.Find("Unit_2_Section_Selection_Panel");
            if (t == null) t = unit2Parent.transform.Find("Section_Selection_Panels");
            if (t != null) unit2SectionSelectionPanel = t.gameObject;
        }

        if (unit3Parent != null && unit3LevelSelectionPanel == null)
        {
            Transform t = unit3Parent.transform.Find("Unit_3_Level_Selection_Panel");
            if (t == null) t = unit3Parent.transform.Find("Unit_3_Section_Selection_Panels");
            if (t == null) t = unit3Parent.transform.Find("Level_Selection_Panel");
            if (t != null) unit3LevelSelectionPanel = t.gameObject;
        }

        if (unit4Parent != null && unit4SectionSelectionPanel == null)
        {
            Transform t = unit4Parent.transform.Find("Unit_4_Section_Selection_Panels");
            if (t == null) t = unit4Parent.transform.Find("Unit_4_Section_Selection_Panel");
            if (t == null) t = unit4Parent.transform.Find("Level_Selection_Panel");
            if (t != null) unit4SectionSelectionPanel = t.gameObject;
        }

        if (unit5Parent != null && unit5SectionSelectionPanel == null)
        {
            Transform t = unit5Parent.transform.Find("Unit_5_Section_Selection_Panels");
            if (t == null) t = unit5Parent.transform.Find("Unit_5_Section_Selection_Panel");
            if (t == null) t = unit5Parent.transform.Find("Level_Selection_Panel");
            if (t != null) unit5SectionSelectionPanel = t.gameObject;
        }

        if (unit6Parent != null && unit6SectionSelectionPanel == null)
        {
            Transform t = unit6Parent.transform.Find("Unit_6_Section_Selection_Panels");
            if (t == null) t = unit6Parent.transform.Find("Unit_6_Section_Selection_Panel");
            if (t == null) t = unit6Parent.transform.Find("Section_Selection_Panels");
            if (t == null) t = unit6Parent.transform.Find("Section_Selection_Panel");
            if (t == null) t = unit6Parent.transform.Find("Level_Selection_Panel");
            if (t == null) t = unit6Parent.transform.Find("Level_Selection_Panels");
            if (t != null) unit6SectionSelectionPanel = t.gameObject;
        }

        if (unit7Parent != null && unit7SectionSelectionPanel == null)
        {
            Transform t = unit7Parent.transform.Find("Unit_7_Section_Selection_Panels");
            if (t == null) t = unit7Parent.transform.Find("Unit_7_Section_Selection_Panel");
            if (t == null) t = unit7Parent.transform.Find("Section_Selection_Panels");
            if (t == null) t = unit7Parent.transform.Find("Section_Selection_Panel");
            if (t == null) t = unit7Parent.transform.Find("Level_Selection_Panel");
            if (t == null) t = unit7Parent.transform.Find("Level_Selection_Panels");
            if (t != null) unit7SectionSelectionPanel = t.gameObject;
        }

        if (unit8Parent != null && unit8SectionSelectionPanel == null)
        {
            Transform t = unit8Parent.transform.Find("Unit_8_Section_Selection_Panels");
            if (t == null) t = unit8Parent.transform.Find("Unit_8_Section_Selection_Panel");
            if (t == null) t = unit8Parent.transform.Find("Section_Selection_Panels");
            if (t == null) t = unit8Parent.transform.Find("Section_Selection_Panel");
            if (t != null) unit8SectionSelectionPanel = t.gameObject;
        }

        if (unit9Parent != null && unit9SectionSelectionPanel == null)
        {
            Transform t = unit9Parent.transform.Find("Unit_9_Section_Selection_Panels");
            if (t == null) t = unit9Parent.transform.Find("Unit_9_Section_Selection_Panel");
            if (t == null) t = unit9Parent.transform.Find("Section_Selection_Panels");
            if (t == null) t = unit9Parent.transform.Find("Section_Selection_Panel");
            if (t != null) unit9SectionSelectionPanel = t.gameObject;
        }

        if (unit10Parent != null && unit10SectionSelectionPanel == null)
        {
            Transform t = unit10Parent.transform.Find("Unit_10_Section_Selection_Panels");
            if (t == null) t = unit10Parent.transform.Find("Unit_10_Section_Selection_Panel");
            if (t == null) t = unit10Parent.transform.Find("Section_Selection_Panels");
            if (t == null) t = unit10Parent.transform.Find("Section_Selection_Panel");
            if (t != null) unit10SectionSelectionPanel = t.gameObject;
        }
    }
     private void PlayPopSound()
    {
        if (popClip != null && audioSource != null && audioSource.enabled && audioSource.gameObject.activeInHierarchy)
        {
            audioSource.PlayOneShot(popClip);
        }
    }

    // ------------------ Open Unit 1 ------------------

    public void Open_Unit_1_Lessons()
    {
        OpenUnit(1);
    }

    public void Open_Unit_2_Lessons()
    {
        OpenUnit(2);
    }

    public void Open_Unit_3_Lessons()
    {
        OpenUnit(3);
    }

        public void Open_Unit_4_Lessons()
        {
            OpenUnit(4);
        }

        public void Open_Unit_5_Lessons()
        {
            OpenUnit(5);
        }

        public void Open_Unit_6_Lessons()
        {
            OpenUnit(6);
        }

    // ------------------ Back to Unit Selection ------------------

    public void BackToUnitSelection()
    {
        StopAllUnitAudio();
        CloseAllUnits();

        if (lessonsPanel != null)
        {
            lessonsPanel.SetActive(true);
            foreach (Transform child in lessonsPanel.GetComponentsInChildren<Transform>(true))
            {
                if (child != null && !child.gameObject.activeSelf)
                {
                    child.gameObject.SetActive(true);
                }
            }
        }

        if (audioSource != null && selectUnitClip != null)
        {
            audioSource.clip = selectUnitClip;
            audioSource.Play();
        }
    }

    // ------------------ Unit 1 Sections ------------------

    public void HideSelectionPanels()
    {
        if (unit1SectionSelectionPanel != null) unit1SectionSelectionPanel.SetActive(false);
        if (unit2SectionSelectionPanel != null) unit2SectionSelectionPanel.SetActive(false);
    }

    private void EnsureSingleBackButton(GameObject activeSectionPanel)
    {
        if (activeSectionPanel == null) return;

        Transform innerBack = activeSectionPanel.transform.Find("Back_Button");
        if (innerBack == null) innerBack = activeSectionPanel.transform.Find("Back_Button_Unit_1_Panel");

        Transform outerBack = unit1Parent != null ? unit1Parent.transform.Find("Back_Button") : null;
        if (outerBack == null && unit1SectionsParent != null) outerBack = unit1SectionsParent.transform.Find("Back_Button");

        if (innerBack != null && outerBack != null && innerBack != outerBack)
        {
            outerBack.gameObject.SetActive(false);
            innerBack.gameObject.SetActive(true);
        }
    }

    public void OpenSection(int index)
    {
        CloseAllUnit1Sections();
        CloseAllUnit2Sections();

        // 1. Ensure Unit 1 parent is active
        if (unit1Parent != null) unit1Parent.SetActive(true);

        // 2. Hide section selection panel
        HideSelectionPanels();

        // 3. Activate Unit_1_Sections parent container
        if (unit1SectionsParent != null)
        {
            unit1SectionsParent.SetActive(true);
        }

        if (sectionPanels != null && index >= 0 && index < sectionPanels.Length)
        {
            PlayPopSound();
            GameObject targetPanel = sectionPanels[index];
            if (targetPanel != null)
            {
                // Ensure ALL parent objects up to Canvas/Root are active
                Transform curr = targetPanel.transform;
                while (curr != null && curr.gameObject.name != "Canvas")
                {
                    if (!curr.gameObject.activeSelf)
                    {
                        curr.gameObject.SetActive(true);
                    }
                    curr = curr.parent;
                }
                targetPanel.SetActive(true);
                EnsureSingleBackButton(targetPanel);
            }

            // Call manager start method for selected section
            if (index == 0)
            {
                SA_LetterManager_Phonics_Junior manager = unit1SectionsParent != null ? unit1SectionsParent.GetComponentInChildren<SA_LetterManager_Phonics_Junior>(true) : null;
                if (manager == null && targetPanel != null) manager = targetPanel.GetComponentInParent<SA_LetterManager_Phonics_Junior>(true);
                if (manager == null && targetPanel != null) manager = targetPanel.GetComponentInChildren<SA_LetterManager_Phonics_Junior>(true);
                if (manager == null) manager = FindFirstObjectByType<SA_LetterManager_Phonics_Junior>(FindObjectsInactive.Include);
                if (manager != null && manager.gameObject.name != "Game Manager")
                {
                    manager.gameObject.SetActive(true);
                    manager.OpenMeetTheLetters();
                }
                else
                {
                    SA_LetterManager_Phonics_Junior[] all = FindObjectsByType<SA_LetterManager_Phonics_Junior>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    foreach (var m in all)
                    {
                        if (m.gameObject.name != "Game Manager")
                        {
                            m.gameObject.SetActive(true);
                            m.OpenMeetTheLetters();
                            break;
                        }
                    }
                }
            }
            else if (index == 1)
            {
                SB_LetterSoundManager_Phonics_Junior manager = unit1SectionsParent != null ? unit1SectionsParent.GetComponentInChildren<SB_LetterSoundManager_Phonics_Junior>(true) : null;
                if (manager == null && targetPanel != null) manager = targetPanel.GetComponentInParent<SB_LetterSoundManager_Phonics_Junior>(true);
                if (manager == null && targetPanel != null) manager = targetPanel.GetComponentInChildren<SB_LetterSoundManager_Phonics_Junior>(true);
                if (manager == null) manager = FindFirstObjectByType<SB_LetterSoundManager_Phonics_Junior>(FindObjectsInactive.Include);
                if (manager != null && manager.gameObject.name != "Game Manager")
                {
                    manager.gameObject.SetActive(true);
                    manager.OpenLetterSounds();
                }
                else
                {
                    SB_LetterSoundManager_Phonics_Junior[] all = FindObjectsByType<SB_LetterSoundManager_Phonics_Junior>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    foreach (var m in all)
                    {
                        if (m.gameObject.name != "Game Manager")
                        {
                            m.gameObject.SetActive(true);
                            m.OpenLetterSounds();
                            break;
                        }
                    }
                }
            }
            else if (index == 2)
            {
                SC_VowelHandManager_Phonics_Junior manager = unit1SectionsParent != null ? unit1SectionsParent.GetComponentInChildren<SC_VowelHandManager_Phonics_Junior>(true) : null;
                if (manager == null && targetPanel != null) manager = targetPanel.GetComponentInParent<SC_VowelHandManager_Phonics_Junior>(true);
                if (manager == null && targetPanel != null) manager = targetPanel.GetComponentInChildren<SC_VowelHandManager_Phonics_Junior>(true);
                if (manager == null) manager = FindFirstObjectByType<SC_VowelHandManager_Phonics_Junior>(FindObjectsInactive.Include);
                if (manager != null)
                {
                    manager.gameObject.SetActive(true);
                    manager.OpenVowelHand();
                }
            }
            else if (index == 3)
            {
                SD_SoundWallManager_Phonics_Junior manager = unit1SectionsParent != null ? unit1SectionsParent.GetComponentInChildren<SD_SoundWallManager_Phonics_Junior>(true) : null;
                if (manager == null && targetPanel != null) manager = targetPanel.GetComponentInParent<SD_SoundWallManager_Phonics_Junior>(true);
                if (manager == null && targetPanel != null) manager = targetPanel.GetComponentInChildren<SD_SoundWallManager_Phonics_Junior>(true);
                if (manager == null) manager = FindFirstObjectByType<SD_SoundWallManager_Phonics_Junior>(FindObjectsInactive.Include);
                if (manager != null)
                {
                    manager.gameObject.SetActive(true);
                    manager.OpenSoundWall();
                }
            }
        }
    }
    
    public void OpenSectionA() { OpenSection(0); }
    public void OpenSectionB() { OpenSection(1); }
    public void OpenSectionC() { OpenSection(2); }
    public void OpenSectionD() { OpenSection(3); }

    public void CloseAllUnit1Sections()
    {
        if (sectionPanels != null)
        {
            foreach (GameObject panel in sectionPanels)
            {
                if (panel != null) panel.SetActive(false);
            }
        }

        if (unit1SectionsParent != null) unit1SectionsParent.SetActive(false);

        U1_RewardController reward = FindFirstObjectByType<U1_RewardController>(FindObjectsInactive.Include);
        if (reward != null)
        {
            reward.gameObject.SetActive(false);
        }

        SA_LetterManager_Phonics_Junior saManager = FindFirstObjectByType<SA_LetterManager_Phonics_Junior>(FindObjectsInactive.Include);
        if (saManager != null)
        {
            saManager.CloseSection();
        }

        SB_LetterSoundManager_Phonics_Junior sbManager = FindFirstObjectByType<SB_LetterSoundManager_Phonics_Junior>(FindObjectsInactive.Include);
        if (sbManager != null)
        {
            sbManager.CloseSection();
        }
    }

    private void SetUnit2SectionBackButtonActive(bool inActivitySection)
    {
        // 1. Find section selection panel back button (Back_Button_Unit_2_Panel)
        Transform selectionPanelBack = null;
        if (unit2SectionSelectionPanel != null)
        {
            selectionPanelBack = unit2SectionSelectionPanel.transform.Find("Back_Button_Unit_2_Panel");
            if (selectionPanelBack == null) selectionPanelBack = unit2SectionSelectionPanel.transform.Find("Back_Button");
        }
        if (selectionPanelBack == null)
        {
            GameObject selPanel = GameObject.Find("Unit_2_Section_Selection_Panels");
            if (selPanel != null)
            {
                selectionPanelBack = selPanel.transform.Find("Back_Button_Unit_2_Panel");
                if (selectionPanelBack == null) selectionPanelBack = selPanel.transform.Find("Back_Button");
            }
        }

        // 2. Find activity section back button (Back_Button under Unit_2 or Unit_2_Sections)
        Transform activitySectionBack = null;
        if (unit2Parent != null)
        {
            activitySectionBack = unit2Parent.transform.Find("Back_Button");
        }
        if (activitySectionBack == null && unit2SectionsParent != null)
        {
            activitySectionBack = unit2SectionsParent.transform.Find("Back_Button");
            if (activitySectionBack == null) activitySectionBack = unit2SectionsParent.transform.Find("Section_Selection_Back");
        }
        if (activitySectionBack == null)
        {
            GameObject u2Obj = GameObject.Find("Unit_2");
            if (u2Obj != null)
            {
                activitySectionBack = u2Obj.transform.Find("Back_Button");
            }
            if (activitySectionBack == null)
            {
                GameObject u2Sec = GameObject.Find("Unit_2_Sections");
                if (u2Sec != null)
                {
                    activitySectionBack = u2Sec.transform.Find("Back_Button");
                    if (activitySectionBack == null) activitySectionBack = u2Sec.transform.Find("Section_Selection_Back");
                }
            }
        }

        // Apply mutually exclusive visibility toggle:
        if (inActivitySection)
        {
            // Inside activity sections: show activity back button, hide selection panel back button
            if (activitySectionBack != null) activitySectionBack.gameObject.SetActive(true);
            if (selectionPanelBack != null) selectionPanelBack.gameObject.SetActive(false);
        }
        else
        {
            // On section selection panel: hide activity back button, show selection panel back button
            if (activitySectionBack != null) activitySectionBack.gameObject.SetActive(false);
            if (selectionPanelBack != null) selectionPanelBack.gameObject.SetActive(true);
        }
    }

    public void OpenUnit2Section(int index)
    {
        CloseAllUnit1Sections();
        CloseAllUnit2Sections();

        // 1. Hide Unit 2 section selection panel
        if (unit2SectionSelectionPanel != null) unit2SectionSelectionPanel.SetActive(false);

        // 2. Activate Unit 2 sections parent container & deactivate section children (except BG)
        if (unit2SectionsParent != null)
        {
            unit2SectionsParent.SetActive(true);
            foreach (Transform child in unit2SectionsParent.transform)
            {
                if (child.gameObject.name.ToUpper().Contains("BG") || child.gameObject.name.ToUpper().Contains("BACKGROUND"))
                {
                    child.gameObject.SetActive(true);
                    continue;
                }
                child.gameObject.SetActive(false);
                foreach (Transform sub in child)
                {
                    sub.gameObject.SetActive(false);
                }
            }
        }

        // Enable section back button while inside Section A, B, C, or D
        SetUnit2SectionBackButtonActive(true);

        if (unit2SectionPanelsArray != null && index >= 0 && index < unit2SectionPanelsArray.Length)
        {
            PlayPopSound();
            if (unit2SectionPanelsArray[index] != null)
                unit2SectionPanelsArray[index].SetActive(true);

            
    if (index == 0)
    {
        U2_SA_VowelPairsManager_Phonics_Junior manager = unit2SectionPanelsArray[0].GetComponent<U2_SA_VowelPairsManager_Phonics_Junior>();
        if (manager == null) manager = FindFirstObjectByType<U2_SA_VowelPairsManager_Phonics_Junior>();
        if (manager != null) manager.OpenSectionA();
    }
    else if (index == 1)
    {
        U2_SB_ShortVowelManager_Phonics_Junior manager =  FindFirstObjectByType<U2_SB_ShortVowelManager_Phonics_Junior>();
        if (manager != null) manager.OpenSectionB();
    }
    else if (index == 2)
    {
        U2_SC_LongVowelManager_Phonics_Junior manager =FindFirstObjectByType<U2_SC_LongVowelManager_Phonics_Junior>();
        if (manager != null) manager.OpenSectionC();
    } else if (index == 3)
    {
        U2_SD_ShortOrLongManager_Phonics_Junior manager =  FindFirstObjectByType<U2_SD_ShortOrLongManager_Phonics_Junior>();
        if (manager != null) manager.OpenSectionD();
    }
        }
    }

    public void OpenUnit2SectionA() { OpenUnit2Section(0); }
    public void OpenUnit2SectionB() { OpenUnit2Section(1); }
    public void OpenUnit2SectionC() { OpenUnit2Section(2); }
    public void OpenUnit2SectionD() { OpenUnit2Section(3); }


    public void CloseAllUnit2Sections()
    {
        SetUnit2SectionBackButtonActive(false);

        U2_SA_VowelPairsManager_Phonics_Junior sa = FindFirstObjectByType<U2_SA_VowelPairsManager_Phonics_Junior>(FindObjectsInactive.Include);
        if (sa != null) sa.CloseSectionA();

        U2_SB_ShortVowelManager_Phonics_Junior sb = FindFirstObjectByType<U2_SB_ShortVowelManager_Phonics_Junior>(FindObjectsInactive.Include);
        if (sb != null) sb.CloseSectionB();

        U2_SC_LongVowelManager_Phonics_Junior sc = FindFirstObjectByType<U2_SC_LongVowelManager_Phonics_Junior>(FindObjectsInactive.Include);
        if (sc != null) sc.CloseSectionC();

        U2_SD_ShortOrLongManager_Phonics_Junior sd = FindFirstObjectByType<U2_SD_ShortOrLongManager_Phonics_Junior>(FindObjectsInactive.Include);
        if (sd != null) sd.CloseSectionD();

        if (unit2SectionPanelsArray != null)
        {
            foreach (GameObject panel in unit2SectionPanelsArray)
            {
                if (panel != null) panel.SetActive(false);
            }
        }

        if (unit2SectionsParent != null)
        {
            foreach (Transform child in unit2SectionsParent.transform)
            {
                child.gameObject.SetActive(false);
            }
            unit2SectionsParent.SetActive(false);
        }
    }

    // ------------------ Units 1 to 10 Universal Selector ------------------

    private GameObject GetUnitParent(int index)
    {
        switch (index)
        {
            case 1: return unit1Parent;
            case 2: return unit2Parent;
            case 3: return unit3Parent;
            case 4: return unit4Parent;
            case 5: return unit5Parent;
            case 6: return unit6Parent;
            case 7: return unit7Parent;
            case 8: return unit8Parent;
            case 9: return unit9Parent;
            case 10: return unit10Parent;
            default: return null;
        }
    }

    private GameObject GetUnitSectionSelectionPanel(int index)
    {
        switch (index)
        {
            case 1: return unit1SectionSelectionPanel;
            case 2: return unit2SectionSelectionPanel;
            case 3: return unit3LevelSelectionPanel;
            case 4: return unit4SectionSelectionPanel;
            case 5: return unit5SectionSelectionPanel;
            case 6: return unit6SectionSelectionPanel;
            case 7: return unit7SectionSelectionPanel;
            case 8: return unit8SectionSelectionPanel;
            case 9: return unit9SectionSelectionPanel;
            case 10: return unit10SectionSelectionPanel;
            default: return null;
        }
    }

    private Transform GetUnitSections(int index)
    {
        GameObject parent = GetUnitParent(index);
        if (parent == null) return null;
        Transform s = parent.transform.Find($"Unit_{index}_Sections");
        if (s == null) s = parent.transform.Find("Sections");
        if (s == null) s = parent.transform.Find($"Unit{index}_Sections");
        return s;
    }

    public void OpenUnit(int unitIndex)
    {
        EnsureInitUnitParents();
        CloseAllUnits();
        PlayPopSound();

        // Mutually exclusive unit isolation: Deactivate ALL unit parent GameObjects except target unit index!
        Transform canvasTransform = transform.parent;
        if (canvasTransform == null)
        {
            Canvas c = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (c != null) canvasTransform = c.transform;
        }

        if (canvasTransform != null)
        {
            for (int i = 1; i <= 10; i++)
            {
                if (i == unitIndex) continue;

                Transform uParent = canvasTransform.Find($"Unit_{i}");
                if (uParent == null) uParent = canvasTransform.Find($"Unit{i}");
                if (uParent != null) uParent.gameObject.SetActive(false);
            }
        }

        GameObject parentObj = GetUnitParent(unitIndex);
        GameObject panelObj = GetUnitSectionSelectionPanel(unitIndex);

        bool isInsideLessons = (parentObj != null && lessonsPanel != null && parentObj.transform.IsChildOf(lessonsPanel.transform))
                            || (panelObj != null && lessonsPanel != null && panelObj.transform.IsChildOf(lessonsPanel.transform));

        if (!isInsideLessons && lessonsPanel != null)
        {
            lessonsPanel.SetActive(false);
        }
        else if (isInsideLessons && lessonsPanel != null)
        {
            lessonsPanel.SetActive(true);
        }

        if (parentObj != null) EnableObjectAndParents(parentObj);
        if (panelObj != null) EnableObjectAndParents(panelObj);

        Transform sections = GetUnitSections(unitIndex);
        if (sections != null) sections.gameObject.SetActive(false);

        switch (unitIndex)
        {
            case 1:
                if (gameObject.activeInHierarchy) StartCoroutine(PlayGuideAfterDelay(selectLessonClip, 0.3f));
                break;
            case 2:
                SetUnit2SectionBackButtonActive(false);
                if (gameObject.activeInHierarchy) StartCoroutine(PlayGuideAfterDelay(selectLessonClip, 0.3f));
                break;
            case 3:
                Unit3Manager u3Manager = FindFirstObjectByType<Unit3Manager>(FindObjectsInactive.Include);
                if (u3Manager != null) u3Manager.ShowLevelSelection();
                if (gameObject.activeInHierarchy) StartCoroutine(PlayGuideAfterDelay(selectLessonClip, 0.3f));
                break;
            case 4:
                Unit4Manager u4Manager = FindFirstObjectByType<Unit4Manager>(FindObjectsInactive.Include);
                if (u4Manager != null) u4Manager.ShowLevelSelection();
                break;
            case 5:
                Unit5Manager u5Manager = FindFirstObjectByType<Unit5Manager>(FindObjectsInactive.Include);
                if (u5Manager != null) u5Manager.ShowLevelSelection();
                break;
            case 6:
                U6_Manager u6Manager = FindFirstObjectByType<U6_Manager>(FindObjectsInactive.Include);
                if (u6Manager != null)
                {
                    u6Manager.AutoBindPanels();
                    if (u6Manager.levelSelectionPanel == null && panelObj != null)
                    {
                        u6Manager.levelSelectionPanel = panelObj;
                    }
                    u6Manager.ShowLevelSelection();
                }
                break;
            case 7:
                U7_Manager u7Manager = FindFirstObjectByType<U7_Manager>(FindObjectsInactive.Include);
                if (u7Manager != null)
                {
                    u7Manager.AutoBindPanels();
                    if (u7Manager.levelSelectionPanel == null && panelObj != null)
                    {
                        u7Manager.levelSelectionPanel = panelObj;
                    }
                    u7Manager.ShowLevelSelection();
                }
                break;
            case 8:
                U8_Manager u8Manager = FindFirstObjectByType<U8_Manager>(FindObjectsInactive.Include);
                if (u8Manager != null)
                {
                    u8Manager.AutoBindPanels();
                    if (u8Manager.levelSelectionPanel == null && panelObj != null)
                    {
                        u8Manager.levelSelectionPanel = panelObj;
                    }
                    u8Manager.ShowLevelSelection();
                }
                break;
            case 9:
                U9_Manager u9Manager = FindFirstObjectByType<U9_Manager>(FindObjectsInactive.Include);
                if (u9Manager != null)
                {
                    u9Manager.AutoBindPanels();
                    if (u9Manager.levelSelectionPanel == null && panelObj != null)
                    {
                        u9Manager.levelSelectionPanel = panelObj;
                    }
                    u9Manager.ShowLevelSelection();
                }
                break;
            case 10:
                U10_Manager u10Manager = FindFirstObjectByType<U10_Manager>(FindObjectsInactive.Include);
                if (u10Manager != null)
                {
                    u10Manager.AutoBindPanels();
                    if (u10Manager.levelSelectionPanel == null && panelObj != null)
                    {
                        u10Manager.levelSelectionPanel = panelObj;
                    }
                    u10Manager.ShowLevelSelection();
                }
                break;
        }
    }

    private void EnableObjectAndParents(GameObject obj)
    {
        if (obj == null) return;

        obj.SetActive(true);

        Transform current = obj.transform.parent;
        while (current != null)
        {
            if (!current.gameObject.activeSelf)
            {
                current.gameObject.SetActive(true);
            }
            current = current.parent;
        }
    }

    public void OpenUnit1() { OpenUnit(1); }
    public void OpenUnit2() { OpenUnit(2); }
    public void OpenUnit3() { OpenUnit(3); }
    public void OpenUnit4() { OpenUnit(4); }
    public void OpenUnit5() { OpenUnit(5); }
    public void OpenUnit6() { OpenUnit(6); }
    public void OpenUnit7() { OpenUnit(7); }
    public void OpenUnit8() { OpenUnit(8); }
    public void OpenUnit9() { OpenUnit(9); }
    public void OpenUnit10() { OpenUnit(10); }

    public void OpenUnit4ShortA()
    {
        OpenUnit(4);
        Unit4Manager u4Manager = FindFirstObjectByType<Unit4Manager>(FindObjectsInactive.Include);
        if (u4Manager != null) u4Manager.StartLevel(0);
    }

    public void OpenUnit4ShortE()
    {
        OpenUnit(4);
        Unit4Manager u4Manager = FindFirstObjectByType<Unit4Manager>(FindObjectsInactive.Include);
        if (u4Manager != null) u4Manager.StartLevel(1);
    }

    public void OpenUnit5ShortI()
    {
        OpenUnit(5);
        Unit5Manager u5Manager = FindFirstObjectByType<Unit5Manager>(FindObjectsInactive.Include);
        if (u5Manager != null) u5Manager.StartLevel(0);
    }

    public void OpenUnit5ShortO()
    {
        OpenUnit(5);
        Unit5Manager u5Manager = FindFirstObjectByType<Unit5Manager>(FindObjectsInactive.Include);
        if (u5Manager != null) u5Manager.StartLevel(1);
    }

    public void OpenUnit5ShortU()
    {
        OpenUnit(5);
        Unit5Manager u5Manager = FindFirstObjectByType<Unit5Manager>(FindObjectsInactive.Include);
        if (u5Manager != null) u5Manager.StartLevel(2);
    }

    public void OpenUnit7LongI()
    {
        OpenUnit(7);
        U7_Manager u7Manager = FindFirstObjectByType<U7_Manager>(FindObjectsInactive.Include);
        if (u7Manager != null) u7Manager.StartLevel1LongI();
    }

    public void OpenUnit7LongO()
    {
        OpenUnit(7);
        U7_Manager u7Manager = FindFirstObjectByType<U7_Manager>(FindObjectsInactive.Include);
        if (u7Manager != null) u7Manager.StartLevel2LongO();
    }

    public void OpenUnit7LongU()
    {
        OpenUnit(7);
        U7_Manager u7Manager = FindFirstObjectByType<U7_Manager>(FindObjectsInactive.Include);
        if (u7Manager != null) u7Manager.StartLevel3LongU();
    }

    public void OpenUnit6LongA()
    {
        OpenUnit(6);
        U6_Manager u6Manager = FindFirstObjectByType<U6_Manager>(FindObjectsInactive.Include);
        if (u6Manager != null) u6Manager.StartLevel1LongA();
    }

    public void OpenUnit6LongE()
    {
        OpenUnit(6);
        U6_Manager u6Manager = FindFirstObjectByType<U6_Manager>(FindObjectsInactive.Include);
        if (u6Manager != null) u6Manager.StartLevel2LongE();
    }

    public void Open_Unit_9_Lessons()
    {
        OpenUnit(9);
    }

    public void OpenUnit9SectionA()
    {
        OpenUnit(9);
        U9_Manager u9Manager = FindFirstObjectByType<U9_Manager>(FindObjectsInactive.Include);
        if (u9Manager != null) u9Manager.StartSectionA();
    }

    public void OpenUnit9SectionB()
    {
        OpenUnit(9);
        U9_Manager u9Manager = FindFirstObjectByType<U9_Manager>(FindObjectsInactive.Include);
        if (u9Manager != null) u9Manager.StartSectionB();
    }

    public void OpenUnit9SectionC()
    {
        OpenUnit(9);
        U9_Manager u9Manager = FindFirstObjectByType<U9_Manager>(FindObjectsInactive.Include);
        if (u9Manager != null) u9Manager.StartSectionC();
    }

    public void OpenUnit9SectionD()
    {
        OpenUnit(9);
        U9_Manager u9Manager = FindFirstObjectByType<U9_Manager>(FindObjectsInactive.Include);
        if (u9Manager != null) u9Manager.StartSectionD();
    }

    public void Open_Unit_10_Lessons()
    {
        OpenUnit(10);
    }

    public void OpenUnit10SectionA()
    {
        OpenUnit(10);
        U10_Manager u10Manager = FindFirstObjectByType<U10_Manager>(FindObjectsInactive.Include);
        if (u10Manager != null) u10Manager.StartSectionA();
    }

    public void OpenUnit10SectionB()
    {
        OpenUnit(10);
        U10_Manager u10Manager = FindFirstObjectByType<U10_Manager>(FindObjectsInactive.Include);
        if (u10Manager != null) u10Manager.StartSectionB();
    }

    public void OpenUnit10SectionC()
    {
        OpenUnit(10);
        U10_Manager u10Manager = FindFirstObjectByType<U10_Manager>(FindObjectsInactive.Include);
        if (u10Manager != null) u10Manager.StartSectionC();
    }

    public void OpenUnit10SectionD()
    {
        OpenUnit(10);
        U10_Manager u10Manager = FindFirstObjectByType<U10_Manager>(FindObjectsInactive.Include);
        if (u10Manager != null) u10Manager.StartSectionD();
    }

    public void CloseAllUnits()
    {
        StopAllUnitAudio();
        EnsureInitUnitParents();

        void SafeDeactivate(GameObject obj)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        SafeDeactivate(unit1Parent);
        SafeDeactivate(unit2Parent);
        SafeDeactivate(unit3Parent);
        SafeDeactivate(unit4Parent);
        SafeDeactivate(unit5Parent);
        SafeDeactivate(unit6Parent);
        SafeDeactivate(unit7Parent);
        SafeDeactivate(unit8Parent);
        SafeDeactivate(unit9Parent);
        SafeDeactivate(unit10Parent);

        SafeDeactivate(unit1SectionSelectionPanel);
        SafeDeactivate(unit2SectionSelectionPanel);
        SafeDeactivate(unit3LevelSelectionPanel);
        SafeDeactivate(unit4SectionSelectionPanel);
        SafeDeactivate(unit5SectionSelectionPanel);
        SafeDeactivate(unit6SectionSelectionPanel);
        SafeDeactivate(unit7SectionSelectionPanel);
        SafeDeactivate(unit8SectionSelectionPanel);
        SafeDeactivate(unit9SectionSelectionPanel);
        SafeDeactivate(unit10SectionSelectionPanel);

        CloseAllUnit1Sections();
        CloseAllUnit2Sections();

        Unit3Manager u3 = FindFirstObjectByType<Unit3Manager>(FindObjectsInactive.Include);
        // NOTE: Do NOT call u3.ShowLevelSelection() here — that re-enables the Unit 3 panel
        // while we are trying to close everything. It will be shown when the user opens Unit 3.
        if (u3 != null && unit3Parent != null && !unit3Parent.activeSelf)
        {
            // Unit 3 is already deactivated by SafeDeactivate above — just reset its internal state
            u3.gameObject.BroadcastMessage("CloseAllPanels", SendMessageOptions.DontRequireReceiver);
        }

        Unit5Manager u5 = FindFirstObjectByType<Unit5Manager>(FindObjectsInactive.Include);
        if (u5 != null) u5.CloseAllPanels();

        Unit4Manager u4 = FindFirstObjectByType<Unit4Manager>(FindObjectsInactive.Include);
        if (u4 != null) u4.CloseAllPanels();

        U6_Manager u6 = FindFirstObjectByType<U6_Manager>(FindObjectsInactive.Include);
        if (u6 != null) u6.CloseAllPanels();

        U7_Manager u7 = FindFirstObjectByType<U7_Manager>(FindObjectsInactive.Include);
        if (u7 != null) u7.CloseAllPanels();

        U8_Manager u8 = FindFirstObjectByType<U8_Manager>(FindObjectsInactive.Include);
        if (u8 != null) u8.CloseAllPanels();

        U9_Manager u9 = FindFirstObjectByType<U9_Manager>(FindObjectsInactive.Include);
        if (u9 != null) u9.CloseAllPanels();

        // Universal safeguard: Deactivate all Unit_X_Sections activity containers
        Transform canvasTransform = transform.parent;
        if (canvasTransform == null)
        {
            Canvas c = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (c != null) canvasTransform = c.transform;
        }

        if (canvasTransform != null)
        {
            for (int i = 1; i <= 10; i++)
            {
                Transform uSec = canvasTransform.Find($"Unit_{i}/Unit_{i}_Sections");
                if (uSec == null) uSec = canvasTransform.Find($"Unit_{i}/Sections");
                if (uSec == null) uSec = canvasTransform.Find($"Unit{i}/Unit_{i}_Sections");
                if (uSec == null) uSec = canvasTransform.Find($"Unit{i}/Sections");
                if (uSec != null) uSec.gameObject.SetActive(false);
            }
        }
    }

    public void StopAllUnitAudio()
    {
        if (audioSource != null) audioSource.Stop();

        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var src in allAudioSources)
        {
            if (src != null)
            {
                // Preserve background music (BGM)! Do not stop looping music or BGM sources
                string n = src.gameObject.name.ToLower();
                if (src.loop || n.Contains("bg") || n.Contains("music") || n.Contains("background") || n.Contains("theme"))
                {
                    continue;
                }

                try { src.Stop(); } catch (System.Exception) { }
            }
        }

        try { FindFirstObjectByType<SA_LetterManager_Phonics_Junior>(FindObjectsInactive.Include)?.StopSection(); } catch (System.Exception) { }
        try { FindFirstObjectByType<SB_LetterSoundManager_Phonics_Junior>(FindObjectsInactive.Include)?.StopIdleReminder(); } catch (System.Exception) { }
        try { FindFirstObjectByType<SC_VowelHandManager_Phonics_Junior>(FindObjectsInactive.Include)?.StopSection(); } catch (System.Exception) { }
        try { FindFirstObjectByType<SD_SoundWallManager_Phonics_Junior>(FindObjectsInactive.Include)?.StopSection(); } catch (System.Exception) { }

        MonoBehaviour[] allManagers = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var m in allManagers)
        {
            if (m != null && m.GetType().Name.ToLower().Contains("manager"))
            {
                try { m.StopAllCoroutines(); } catch (System.Exception) { }
            }
        }
    }

    private System.Collections.IEnumerator PlayGuideAfterDelay(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (clip != null && audioSource != null && audioSource.enabled && audioSource.gameObject.activeInHierarchy)
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();
        }
    }

    private void OnValidate()
    {
        SanitizeArray(ref sectionPanels);
        SanitizeArray(ref unit2SectionPanelsArray);
    }

    private void SanitizeArray(ref GameObject[] arr)
    {
        if (arr == null) return;
        System.Collections.Generic.List<GameObject> valid = new System.Collections.Generic.List<GameObject>();
        foreach (var item in arr)
        {
            if (item != null) valid.Add(item);
        }
        if (valid.Count != arr.Length)
        {
            arr = valid.ToArray();
        }
    }
}