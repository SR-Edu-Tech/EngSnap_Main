using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MastersPhonics;

public class Unit_Selection_Panel_Masters_Phonics : MonoBehaviour
{
    public static Unit_Selection_Panel_Masters_Phonics Instance { get; private set; }

    [Header("Main Panels")]
    [SerializeField] private GameObject lessonsPanel; // "Lessons" signboard panel

    [Header("Unit 1 (Prefixes & Suffixes)")]
    [SerializeField] private GameObject unit1Parent; // "Unit_1" parent object in Hierarchy
    [SerializeField] private GameObject unit1SectionSelectionPanel; // "Unit_1_Section_Selection_Panels"
    [SerializeField] private GameObject unit1SectionsParent; // "Unit_1_Sections" parent container
    [SerializeField] private GameObject[] unit1SectionPanels; // Sections: 0=Learn, 1=Prefix Machine, 2=Family Sort, 3=Suffix Party, 4=Word Lab, 5=Challenge

    [Header("Unit 2")]
    [SerializeField] private GameObject unit2Parent;
    [SerializeField] private GameObject unit2SectionSelectionPanel;
    [SerializeField] private GameObject unit2SectionsParent;
    [SerializeField] private GameObject[] unit2SectionPanelsArray;

    [Header("Unit 3 to 10 Parents & Panels")]
    [SerializeField] private GameObject unit3Parent;
    [SerializeField] private GameObject unit3SectionSelectionPanel;
    [SerializeField] private GameObject unit4Parent;
    [SerializeField] private GameObject unit4SectionSelectionPanel;
    [SerializeField] private GameObject unit5Parent;
    [SerializeField] private GameObject unit5SectionSelectionPanel;
    [SerializeField] private GameObject unit6Parent;
    [SerializeField] private GameObject unit6SectionSelectionPanel;
    [SerializeField] private GameObject unit7Parent;
    [SerializeField] private GameObject unit7SectionSelectionPanel;
    [SerializeField] private GameObject unit8Parent;
    [SerializeField] private GameObject unit8SectionSelectionPanel;
    [SerializeField] private GameObject unit9Parent;
    [SerializeField] private GameObject unit9SectionSelectionPanel;
    [SerializeField] private GameObject unit10Parent;
    [SerializeField] private GameObject unit10SectionSelectionPanel;

    [Header("Audio")]
    [SerializeField] private AudioClip selectUnitClip;
    [SerializeField] private AudioClip selectLessonClip;
    [SerializeField] private AudioClip popClip;
    [SerializeField] private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

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

    public void EnsureInitUnitParents()
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

        // Auto-find Units 1 to 10 under Canvas
        FindUnitParent(ref unit1Parent, canvasTransform, 1);
        FindUnitParent(ref unit2Parent, canvasTransform, 2);
        FindUnitParent(ref unit3Parent, canvasTransform, 3);
        FindUnitParent(ref unit4Parent, canvasTransform, 4);
        FindUnitParent(ref unit5Parent, canvasTransform, 5);
        FindUnitParent(ref unit6Parent, canvasTransform, 6);
        FindUnitParent(ref unit7Parent, canvasTransform, 7);
        FindUnitParent(ref unit8Parent, canvasTransform, 8);
        FindUnitParent(ref unit9Parent, canvasTransform, 9);
        FindUnitParent(ref unit10Parent, canvasTransform, 10);

        // Auto-bind selection panels
        BindSelectionPanel(ref unit1SectionSelectionPanel, unit1Parent, 1);
        BindSelectionPanel(ref unit2SectionSelectionPanel, unit2Parent, 2);
        BindSelectionPanel(ref unit3SectionSelectionPanel, unit3Parent, 3);
        BindSelectionPanel(ref unit4SectionSelectionPanel, unit4Parent, 4);
        BindSelectionPanel(ref unit5SectionSelectionPanel, unit5Parent, 5);
        BindSelectionPanel(ref unit6SectionSelectionPanel, unit6Parent, 6);
        BindSelectionPanel(ref unit7SectionSelectionPanel, unit7Parent, 7);
        BindSelectionPanel(ref unit8SectionSelectionPanel, unit8Parent, 8);
        BindSelectionPanel(ref unit9SectionSelectionPanel, unit9Parent, 9);
        BindSelectionPanel(ref unit10SectionSelectionPanel, unit10Parent, 10);
    }

    private void FindUnitParent(ref GameObject target, Transform canvasTransform, int index)
    {
        if (target != null && (lessonsPanel == null || !target.transform.IsChildOf(lessonsPanel.transform))) return;
        Transform t = canvasTransform.Find($"Unit_{index}") ?? canvasTransform.Find($"Unit{index}") ?? canvasTransform.Find($"UNIT_{index}") ?? canvasTransform.Find($"UNIT {index}");
        if (t != null) target = t.gameObject;
    }

    private void BindSelectionPanel(ref GameObject target, GameObject parent, int index)
    {
        if (parent == null || target != null) return;
        Transform t = parent.transform.Find($"Unit_{index}_Section_Selection_Panels") 
                   ?? parent.transform.Find($"Unit_{index}_Section_Selection_Panel")
                   ?? parent.transform.Find($"Unit_{index}_Level_Selection_Panel")
                   ?? parent.transform.Find("Section_Selection_Panels")
                   ?? parent.transform.Find("Section_Selection_Panel")
                   ?? parent.transform.Find("Level_Selection_Panel");
        if (t != null) target = t.gameObject;
    }

    public void PlayPopSound()
    {
        if (popClip != null && audioSource != null && audioSource.enabled && audioSource.gameObject.activeInHierarchy)
        {
            audioSource.PlayOneShot(popClip);
        }
    }

    // ------------------ Universal Unit Opener ------------------

    public void OpenUnit(int unitIndex)
    {
        EnsureInitUnitParents();
        CloseAllUnits();
        PlayPopSound();

        // Isolate target unit GameObject
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
                Transform uParent = canvasTransform.Find($"Unit_{i}") ?? canvasTransform.Find($"Unit{i}");
                if (uParent != null) uParent.gameObject.SetActive(false);
            }
        }

        GameObject parentObj = GetUnitParent(unitIndex);
        GameObject panelObj = GetUnitSectionSelectionPanel(unitIndex);

        if (lessonsPanel != null) lessonsPanel.SetActive(false);

        if (parentObj != null) EnableObjectAndParents(parentObj);
        if (panelObj != null) EnableObjectAndParents(panelObj);

        Transform sections = GetUnitSections(unitIndex);
        if (sections != null) sections.gameObject.SetActive(false);

        if (gameObject.activeInHierarchy)
        {
            StartCoroutine(PlayGuideAfterDelay(selectLessonClip, 0.3f));
        }

        // Initialize UnitFlowManager if present
        if (unitIndex == 1)
        {
            var flowManager = FindFirstObjectByType<U1_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);
            if (flowManager != null) flowManager.OpenSectionSelection();
        }
        else if (unitIndex == 2)
        {
            var flowManager = FindFirstObjectByType<U2_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);
            if (flowManager != null) flowManager.OpenSectionSelection();
        }
        else if (unitIndex == 3)
        {
            var flowManager = FindFirstObjectByType<U3_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);
            if (flowManager != null) flowManager.OpenSectionSelection();
        }
        else if (unitIndex == 4)
        {
            var flowManager = FindFirstObjectByType<U4_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);
            if (flowManager != null) flowManager.OpenSectionSelection();
        }
        else if (unitIndex == 5)
        {
            var flowManager = FindFirstObjectByType<U5_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);
            if (flowManager != null) flowManager.OpenSectionSelection();
        }
        else if (unitIndex == 6)
        {
            var flowManager = FindFirstObjectByType<U6_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);
            if (flowManager != null) flowManager.OpenSectionSelection();
        }
        else if (unitIndex == 7)
        {
            var flowManager = FindFirstObjectByType<U7_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);
            if (flowManager != null) flowManager.OpenSectionSelection();
        }
        else if (unitIndex == 8)
        {
            var flowManager = FindFirstObjectByType<U8_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);
            if (flowManager != null) flowManager.OpenSectionSelection();
        }
        else if (unitIndex == 9)
        {
            var flowManager = FindFirstObjectByType<U9_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);
            if (flowManager != null) flowManager.OpenSignboard();
        }
        else if (unitIndex == 10)
        {
            var flowManager = FindFirstObjectByType<U10_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);
            if (flowManager != null) flowManager.OpenSignboard();
        }
    }

    public void OpenUnit1() => OpenUnit(1);
    public void OpenUnit2() => OpenUnit(2);
    public void OpenUnit3() => OpenUnit(3);
    public void OpenUnit4() => OpenUnit(4);
    public void OpenUnit5() => OpenUnit(5);
    public void OpenUnit6() => OpenUnit(6);
    public void OpenUnit7() => OpenUnit(7);
    public void OpenUnit8() => OpenUnit(8);
    public void OpenUnit9() => OpenUnit(9);
    public void OpenUnit10() => OpenUnit(10);

    // ------------------ Unit 1 Sections ------------------

    public void OpenUnit1Section(int index)
    {
        CloseAllUnit1Sections();
        CloseAllUnit2Sections();

        if (unit1Parent != null) unit1Parent.SetActive(true);
        if (unit1SectionSelectionPanel != null) unit1SectionSelectionPanel.SetActive(false);
        if (unit1SectionsParent != null) unit1SectionsParent.SetActive(true);

        // Delegate directly to U1_SA_UnitFlowManager for full synchronization
        var ufm = U1_SA_UnitFlowManager_Masters_Phonics.Instance 
               ?? FindFirstObjectByType<U1_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);

        if (ufm != null)
        {
            PlayPopSound();
            ufm.gameObject.SetActive(true);
            ufm.OpenSection(index);
            return;
        }

        if (unit1SectionPanels != null && index >= 0 && index < unit1SectionPanels.Length)
        {
            PlayPopSound();
            GameObject targetPanel = unit1SectionPanels[index];
            if (targetPanel != null)
            {
                EnableObjectAndParents(targetPanel);
                targetPanel.SetActive(true);
            }

            // Initialize corresponding GameMode component
            if (index == 0) // Learn
            {
                var gm01 = targetPanel != null ? targetPanel.GetComponentInChildren<U1_SA_GM01_ConceptCards_Masters_Phonics>(true) : null;
                if (gm01 != null && gm01.activityData != null) gm01.Initialize(gm01.activityData);
            }
            else if (index == 1) // Prefix Machine
            {
                var gm02 = targetPanel != null ? targetPanel.GetComponentInChildren<U1_SA_GM02_PrefixMachine_Masters_Phonics>(true) : null;
                if (gm02 != null && gm02.activityData != null) gm02.Initialize(gm02.activityData);
            }
            else if (index == 2) // Family Sort
            {
                var gm03 = targetPanel != null ? targetPanel.GetComponentInChildren<U1_SA_GM03_SortingBins_Masters_Phonics>(true) : null;
                if (gm03 != null && gm03.activityData != null) gm03.Initialize(gm03.activityData);
            }
            else if (index == 3) // Suffix Party
            {
                var gm04 = targetPanel != null ? targetPanel.GetComponentInChildren<U1_SA_GM04_SuffixParty_Masters_Phonics>(true) : null;
                if (gm04 != null && gm04.activityData != null) gm04.Initialize(gm04.activityData);
            }
            else if (index == 4) // Word Lab
            {
                var gm05 = targetPanel != null ? targetPanel.GetComponentInChildren<U1_SA_GM05_WordBuilder_Masters_Phonics>(true) : null;
                if (gm05 != null && gm05.currentActivityData != null) gm05.Initialize(gm05.currentActivityData);
            }
        }
    }

    public void OpenUnit1SectionA() => OpenUnit1Section(0); // Learn
    public void OpenUnit1SectionB() => OpenUnit1Section(1); // Activity 1 (Prefix Machine)
    public void OpenUnit1SectionC() => OpenUnit1Section(2); // Activity 2 (Family Sort)
    public void OpenUnit1SectionD() => OpenUnit1Section(3); // Activity 3 (Suffix Party)
    public void OpenUnit1SectionE() => OpenUnit1Section(4); // Activity 4 (Word Lab)

    // ------------------ Unit 2 Sections ------------------

    public void OpenUnit2Section(int index)
    {
        CloseAllUnit1Sections();
        CloseAllUnit2Sections();

        if (unit2Parent != null) unit2Parent.SetActive(true);
        if (unit2SectionSelectionPanel != null) unit2SectionSelectionPanel.SetActive(false);
        if (unit2SectionsParent != null) unit2SectionsParent.SetActive(true);

        var ufm2 = U2_SA_UnitFlowManager_Masters_Phonics.Instance 
                ?? FindFirstObjectByType<U2_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);

        if (ufm2 != null)
        {
            PlayPopSound();
            ufm2.gameObject.SetActive(true);
            ufm2.OpenSection(index);
            return;
        }

        if (unit2SectionPanelsArray != null && index >= 0 && index < unit2SectionPanelsArray.Length)
        {
            PlayPopSound();
            GameObject targetPanel = unit2SectionPanelsArray[index];
            if (targetPanel != null)
            {
                EnableObjectAndParents(targetPanel);
                targetPanel.SetActive(true);
            }
        }
    }

    public void OpenUnit2SectionA() => OpenUnit2Section(0); // Learn
    public void OpenUnit2SectionB() => OpenUnit2Section(1); // Activity 1 (Job Board)
    public void OpenUnit2SectionC() => OpenUnit2Section(2); // Activity 2 (Plural Factory)
    public void OpenUnit2SectionD() => OpenUnit2Section(3); // Activity 3 (Double Trouble)
    public void OpenUnit2SectionE() => OpenUnit2Section(4); // Activity 4 (Compare Ladder)
    public void OpenUnit2SectionF() => OpenUnit2Section(5); // Activity 5 (Y-to-I Lab)

    // ------------------ Unit 3 Sections ------------------

    public void OpenUnit3Section(int index)
    {
        CloseAllUnit1Sections();
        CloseAllUnit2Sections();
        CloseAllUnit3Sections();

        if (unit3Parent != null) unit3Parent.SetActive(true);
        if (unit3SectionSelectionPanel != null) unit3SectionSelectionPanel.SetActive(false);

        var ufm3 = U3_SA_UnitFlowManager_Masters_Phonics.Instance 
                ?? FindFirstObjectByType<U3_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);

        if (ufm3 != null)
        {
            PlayPopSound();
            ufm3.gameObject.SetActive(true);
            ufm3.OpenSection(index);
            return;
        }

        Transform sections = GetUnitSections(3);
        if (sections != null)
        {
            PlayPopSound();
            sections.gameObject.SetActive(true);
            if (index >= 0 && index < sections.childCount)
            {
                sections.GetChild(index).gameObject.SetActive(true);
            }
        }
    }

    public void OpenUnit3SectionA() => OpenUnit3Section(0); // Learn (Concept Cards)
    public void OpenUnit3SectionB() => OpenUnit3Section(1); // Activity 1 (Chin Tap)
    public void OpenUnit3SectionC() => OpenUnit3Section(2); // Activity 2 (Split It)
    public void OpenUnit3SectionD() => OpenUnit3Section(3); // Activity 3 (Syllable Sort)
    public void OpenUnit3SectionE() => OpenUnit3Section(4); // Activity 4 (Syllable Builder)
    public void OpenUnit3SectionF() => OpenUnit3Section(5); // Activity 5 (Pattern Detective)

    // ------------------ Unit 4 Sections ------------------

    public void OpenUnit4Section(int index)
    {
        CloseAllUnit1Sections();
        CloseAllUnit2Sections();
        CloseAllUnit3Sections();
        CloseAllUnit4Sections();

        if (unit4Parent != null) unit4Parent.SetActive(true);
        if (unit4SectionSelectionPanel != null) unit4SectionSelectionPanel.SetActive(false);

        var ufm4 = U4_SA_UnitFlowManager_Masters_Phonics.Instance 
                ?? FindFirstObjectByType<U4_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);

        if (ufm4 != null)
        {
            PlayPopSound();
            ufm4.gameObject.SetActive(true);
            ufm4.OpenSection(index);
            return;
        }

        Transform sections = GetUnitSections(4);
        if (sections != null)
        {
            PlayPopSound();
            sections.gameObject.SetActive(true);
            if (index >= 0 && index < sections.childCount)
            {
                sections.GetChild(index).gameObject.SetActive(true);
            }
        }
    }

    public void OpenUnit4SectionA() => OpenUnit4Section(0); // Learn (Concept Cards)
    public void OpenUnit4SectionB() => OpenUnit4Section(1); // Activity 1 (Seven Doors)
    public void OpenUnit4SectionC() => OpenUnit4Section(2); // Activity 2 (Strong Beat)
    public void OpenUnit4SectionD() => OpenUnit4Section(3); // Activity 3 (Schwa Hunt)
    public void OpenUnit4SectionE() => OpenUnit4Section(4); // Activity 4 (Seven Types Map)
    public void OpenUnit4SectionF() => OpenUnit4Section(5); // Activity 5 (Unit Challenge)

    // ------------------ Unit 5 Sections ------------------

    public void OpenUnit5Section(int index)
    {
        CloseAllUnit1Sections();
        CloseAllUnit2Sections();
        CloseAllUnit3Sections();
        CloseAllUnit4Sections();
        CloseAllUnit5Sections();

        if (unit5Parent != null) unit5Parent.SetActive(true);
        if (unit5SectionSelectionPanel != null) unit5SectionSelectionPanel.SetActive(false);

        var ufm5 = U5_SA_UnitFlowManager_Masters_Phonics.Instance 
                ?? (unit5Parent != null ? unit5Parent.GetComponentInChildren<U5_SA_UnitFlowManager_Masters_Phonics>(true) : null)
                ?? FindFirstObjectByType<U5_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);

        if (ufm5 != null)
        {
            PlayPopSound();
            ufm5.gameObject.SetActive(true);
            ufm5.OpenSection(index);
            return;
        }

        Transform sections = GetUnitSections(5);
        if (sections != null)
        {
            PlayPopSound();
            sections.gameObject.SetActive(true);
            List<Transform> panels = new List<Transform>();
            foreach (Transform child in sections)
            {
                if (child != null && !child.name.ToLower().Contains("bg") && !child.name.ToLower().Contains("background"))
                {
                    panels.Add(child);
                }
            }

            if (index >= 0 && index < panels.Count)
            {
                panels[index].gameObject.SetActive(true);
            }
            else if (index >= 0 && index < sections.childCount)
            {
                sections.GetChild(index).gameObject.SetActive(true);
            }
        }
    }

    public void OpenUnit5SectionA() => OpenUnit5Section(0); // Learn (Concept Cards)
    public void OpenUnit5SectionB() => OpenUnit5Section(1); // Activity 1 (Door or Gate)
    public void OpenUnit5SectionC() => OpenUnit5Section(2); // Activity 2 (Long or Short)
    public void OpenUnit5SectionD() => OpenUnit5Section(3); // Activity 3 (The First Door)
    public void OpenUnit5SectionE() => OpenUnit5Section(4); // Activity 4 (Closed-Word Hunt)
    public void OpenUnit5SectionF() => OpenUnit5Section(5); // Activity 5 (Big Word Reader)
    public void OpenUnit5SectionG() => OpenUnit5Section(6); // Unit Challenge
    public void OpenUnit5SectionH() => OpenUnit5Section(7); // Completion Panel

    // ------------------ Unit 6 Sections ------------------

    public void OpenUnit6Section(int index)
    {
        CloseAllUnit1Sections();
        CloseAllUnit2Sections();
        CloseAllUnit3Sections();
        CloseAllUnit4Sections();
        CloseAllUnit5Sections();
        CloseAllUnit6Sections();

        if (unit6Parent != null) unit6Parent.SetActive(true);
        if (unit6SectionSelectionPanel != null) unit6SectionSelectionPanel.SetActive(false);

        var ufm6 = U6_SA_UnitFlowManager_Masters_Phonics.Instance 
                ?? (unit6Parent != null ? unit6Parent.GetComponentInChildren<U6_SA_UnitFlowManager_Masters_Phonics>(true) : null)
                ?? FindFirstObjectByType<U6_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);

        if (ufm6 != null)
        {
            PlayPopSound();
            ufm6.gameObject.SetActive(true);
            ufm6.OpenSection(index);
            return;
        }

        Transform sections = GetUnitSections(6);
        if (sections != null)
        {
            PlayPopSound();
            sections.gameObject.SetActive(true);
            List<Transform> panels = new List<Transform>();
            foreach (Transform child in sections)
            {
                if (child != null && !child.name.ToLower().Contains("bg") && !child.name.ToLower().Contains("background"))
                {
                    panels.Add(child);
                }
            }

            if (index >= 0 && index < panels.Count)
            {
                panels[index].gameObject.SetActive(true);
            }
            else if (index >= 0 && index < sections.childCount)
            {
                sections.GetChild(index).gameObject.SetActive(true);
            }
        }
    }

    public void OpenUnit6SectionA() => OpenUnit6Section(0); // Learn (Concept Cards)
    public void OpenUnit6SectionB() => OpenUnit6Section(1); // Activity 1 (Magic Wand)
    public void OpenUnit6SectionC() => OpenUnit6Section(2); // Activity 2 (Which Long Vowel)
    public void OpenUnit6SectionD() => OpenUnit6Section(3); // Activity 3 (Soft or Hard)
    public void OpenUnit6SectionE() => OpenUnit6Section(4); // Activity 4 (Why The E)
    public void OpenUnit6SectionF() => OpenUnit6Section(5); // Seven Types Map
    public void OpenUnit6SectionG() => OpenUnit6Section(6); // Unit Challenge
    public void OpenUnit6SectionH() => OpenUnit6Section(7); // Completion Panel

    // ------------------ Unit 7 Sections ------------------

    public void OpenUnit7Section(int index)
    {
        CloseAllUnit1Sections();
        CloseAllUnit2Sections();
        CloseAllUnit3Sections();
        CloseAllUnit4Sections();
        CloseAllUnit5Sections();
        CloseAllUnit6Sections();
        CloseAllUnit7Sections();

        if (unit7Parent != null) unit7Parent.SetActive(true);
        if (unit7SectionSelectionPanel != null) unit7SectionSelectionPanel.SetActive(false);

        var ufm7 = U7_SA_UnitFlowManager_Masters_Phonics.Instance 
                ?? (unit7Parent != null ? unit7Parent.GetComponentInChildren<U7_SA_UnitFlowManager_Masters_Phonics>(true) : null)
                ?? FindFirstObjectByType<U7_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);

        if (ufm7 != null)
        {
            PlayPopSound();
            ufm7.gameObject.SetActive(true);
            ufm7.OpenSection(index);
            return;
        }

        Transform sections = GetUnitSections(7);
        if (sections != null)
        {
            PlayPopSound();
            sections.gameObject.SetActive(true);
            List<Transform> panels = new List<Transform>();
            foreach (Transform child in sections)
            {
                if (child != null && !child.name.ToLower().Contains("bg") && !child.name.ToLower().Contains("background"))
                {
                    panels.Add(child);
                }
            }

            if (index >= 0 && index < panels.Count)
            {
                panels[index].gameObject.SetActive(true);
            }
            else if (index >= 0 && index < sections.childCount)
            {
                sections.GetChild(index).gameObject.SetActive(true);
            }
        }
    }

    public void OpenUnit7SectionA() => OpenUnit7Section(0); // Learn (Concept Cards)
    public void OpenUnit7SectionB() => OpenUnit7Section(1); // Activity 1 (Team Up)
    public void OpenUnit7SectionC() => OpenUnit7Section(2); // Activity 2 (Word Families)
    public void OpenUnit7SectionD() => OpenUnit7Section(3); // Activity 3 (Same Sound)
    public void OpenUnit7SectionE() => OpenUnit7Section(4); // Activity 4 (Glide or Hold?)
    public void OpenUnit7SectionF() => OpenUnit7Section(5); // Activity 5 (Glide Families)
    public void OpenUnit7SectionG() => OpenUnit7Section(6); // Activity 6 (Team Rush)
    public void OpenUnit7SectionH() => OpenUnit7Section(7); // Seven Types Map
    public void OpenUnit7SectionI() => OpenUnit7Section(8); // Unit Challenge

    public void CloseAllUnit7Sections()
    {
        Transform sections = GetUnitSections(7);
        if (sections != null)
        {
            foreach (Transform child in sections)
            {
                if (child != null) child.gameObject.SetActive(false);
            }
            sections.gameObject.SetActive(false);
        }
    }

    // ------------------ Unit 8 Sections ------------------

    public void OpenUnit8Section(int index)
    {
        CloseAllUnit1Sections();
        CloseAllUnit2Sections();
        CloseAllUnit3Sections();
        CloseAllUnit4Sections();
        CloseAllUnit5Sections();
        CloseAllUnit6Sections();
        CloseAllUnit7Sections();
        CloseAllUnit8Sections();

        if (unit8Parent != null) unit8Parent.SetActive(true);
        if (unit8SectionSelectionPanel != null) unit8SectionSelectionPanel.SetActive(false);

        var ufm8 = U8_SA_UnitFlowManager_Masters_Phonics.Instance 
                ?? (unit8Parent != null ? unit8Parent.GetComponentInChildren<U8_SA_UnitFlowManager_Masters_Phonics>(true) : null)
                ?? FindFirstObjectByType<U8_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);

        if (ufm8 != null)
        {
            PlayPopSound();
            ufm8.gameObject.SetActive(true);
            switch (index)
            {
                case 0: ufm8.OpenLearn(); break;
                case 1: ufm8.OpenActivity1(); break;
                case 2: ufm8.OpenActivity2(); break;
                case 3: ufm8.OpenActivity3(); break;
                case 4: ufm8.OpenActivity4(); break;
                case 5: ufm8.OpenActivity5(); break;
                case 6: ufm8.OpenUnitChallenge(); break;
                case 7: ufm8.OpenMapUpdate(); break;
                case 8: ufm8.OpenCompletionReport(); break;
                default: ufm8.OpenSignboard(); break;
            }
            return;
        }

        Transform sections = GetUnitSections(8);
        if (sections != null)
        {
            PlayPopSound();
            sections.gameObject.SetActive(true);
            List<Transform> panels = new List<Transform>();
            foreach (Transform child in sections)
            {
                if (child != null && !child.name.ToLower().Contains("bg") && !child.name.ToLower().Contains("background"))
                {
                    panels.Add(child);
                }
            }

            if (index >= 0 && index < panels.Count)
            {
                panels[index].gameObject.SetActive(true);
            }
            else if (index >= 0 && index < sections.childCount)
            {
                sections.GetChild(index).gameObject.SetActive(true);
            }
        }
    }

    public void OpenUnit8SectionA() => OpenUnit8Section(0); // Learn (Concept Cards)
    public void OpenUnit8SectionB() => OpenUnit8Section(1); // Activity 1 (Bossy R)
    public void OpenUnit8SectionC() => OpenUnit8Section(2); // Activity 2 (Three Sounds)
    public void OpenUnit8SectionD() => OpenUnit8Section(3); // Activity 3 (Five Families)
    public void OpenUnit8SectionE() => OpenUnit8Section(4); // Activity 4 (The -er Rule)
    public void OpenUnit8SectionF() => OpenUnit8Section(5); // Activity 5 (Spelling Lab)
    public void OpenUnit8SectionG() => OpenUnit8Section(6); // Unit Challenge
    public void OpenUnit8SectionH() => OpenUnit8Section(7); // Seven Types Map
    public void OpenUnit8SectionI() => OpenUnit8Section(8); // Completion Report

    public void CloseAllUnit8Sections()
    {
        Transform sections = GetUnitSections(8);
        if (sections != null)
        {
            foreach (Transform child in sections)
            {
                if (child != null) child.gameObject.SetActive(false);
            }
            sections.gameObject.SetActive(false);
        }
    }

    // ------------------ Unit 9 Sections ------------------

    public void OpenUnit9Section(int index)
    {
        CloseAllUnit1Sections();
        CloseAllUnit2Sections();
        CloseAllUnit3Sections();
        CloseAllUnit4Sections();
        CloseAllUnit5Sections();
        CloseAllUnit6Sections();
        CloseAllUnit7Sections();
        CloseAllUnit8Sections();
        CloseAllUnit9Sections();

        if (unit9Parent != null) unit9Parent.SetActive(true);
        if (unit9SectionSelectionPanel != null) unit9SectionSelectionPanel.SetActive(false);

        var ufm9 = U9_SA_UnitFlowManager_Masters_Phonics.Instance 
                ?? (unit9Parent != null ? unit9Parent.GetComponentInChildren<U9_SA_UnitFlowManager_Masters_Phonics>(true) : null)
                ?? FindFirstObjectByType<U9_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);

        if (ufm9 != null)
        {
            PlayPopSound();
            ufm9.gameObject.SetActive(true);
            switch (index)
            {
                case 0: ufm9.OpenLearn(); break;
                case 1: ufm9.OpenActivity1(); break;
                case 2: ufm9.OpenActivity2(); break;
                case 3: ufm9.OpenActivity3(); break;
                case 4: ufm9.OpenActivity4(); break;
                case 5: ufm9.OpenChallenge(); break;
                case 6: ufm9.OpenSevenTypesMap(); break;
                case 7: ufm9.OpenCompletionPanel(); break;
                default: ufm9.OpenSignboard(); break;
            }
            return;
        }

        Transform sections = GetUnitSections(9);
        if (sections != null)
        {
            PlayPopSound();
            sections.gameObject.SetActive(true);
            List<Transform> panels = new List<Transform>();
            foreach (Transform child in sections)
            {
                if (child != null && !child.name.ToLower().Contains("bg") && !child.name.ToLower().Contains("background"))
                {
                    panels.Add(child);
                }
            }

            if (index >= 0 && index < panels.Count)
            {
                panels[index].gameObject.SetActive(true);
            }
            else if (index >= 0 && index < sections.childCount)
            {
                sections.GetChild(index).gameObject.SetActive(true);
            }
        }
    }

    public void OpenUnit9SectionA() => OpenUnit9Section(0); // Learn (Concept Cards)
    public void OpenUnit9SectionB() => OpenUnit9Section(1); // Activity 1 (Turtle Rule Splitter)
    public void OpenUnit9SectionC() => OpenUnit9Section(2); // Activity 2 (One or Two?)
    public void OpenUnit9SectionD() => OpenUnit9Section(3); // Activity 3 (Build Ending)
    public void OpenUnit9SectionE() => OpenUnit9Section(4); // Activity 4 (Sentence Hunt)
    public void OpenUnit9SectionF() => OpenUnit9Section(5); // Unit Challenge
    public void OpenUnit9SectionG() => OpenUnit9Section(6); // Seven Types Map
    public void OpenUnit9SectionH() => OpenUnit9Section(7); // Completion Report

    public void CloseAllUnit9Sections()
    {
        Transform sections = GetUnitSections(9);
        if (sections != null)
        {
            foreach (Transform child in sections)
            {
                if (child != null) child.gameObject.SetActive(false);
            }
            sections.gameObject.SetActive(false);
        }
    }

    // ------------------ Unit 10 Sections ------------------

    public void OpenUnit10Section(int index)
    {
        CloseAllUnit1Sections();
        CloseAllUnit2Sections();
        CloseAllUnit3Sections();
        CloseAllUnit4Sections();
        CloseAllUnit5Sections();
        CloseAllUnit6Sections();
        CloseAllUnit7Sections();
        CloseAllUnit8Sections();
        CloseAllUnit9Sections();
        CloseAllUnit10Sections();

        if (unit10Parent != null) unit10Parent.SetActive(true);
        if (unit10SectionSelectionPanel != null) unit10SectionSelectionPanel.SetActive(false);

        var ufm10 = U10_SA_UnitFlowManager_Masters_Phonics.Instance 
                 ?? (unit10Parent != null ? unit10Parent.GetComponentInChildren<U10_SA_UnitFlowManager_Masters_Phonics>(true) : null)
                 ?? FindFirstObjectByType<U10_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);

        if (ufm10 != null)
        {
            PlayPopSound();
            ufm10.gameObject.SetActive(true);
            switch (index)
            {
                case 0: ufm10.OpenLearnSection(); break;  // Wagon 1 (Learn -> Act 1)
                case 1: ufm10.OpenActivity2(); break;     // Wagon 2 (Act 2)
                case 2: ufm10.OpenActivity3(); break;     // Wagon 3 (Act 3)
                case 3: ufm10.OpenActivity4(); break;     // Wagon 4 (Act 4)
                case 4: ufm10.OpenUnitChallenge(); break; // Wagon 5 (Challenge & Finale)
                case 5: ufm10.OpenActivity1(); break;
                case 6: ufm10.OpenCompletionPanel(); break;
                case 7: ufm10.OpenCourseFinale(); break;
                default: ufm10.OpenSignboard(); break;
            }
            return;
        }

        Transform sections = GetUnitSections(10);
        if (sections != null)
        {
            PlayPopSound();
            sections.gameObject.SetActive(true);
            List<Transform> panels = new List<Transform>();
            foreach (Transform child in sections)
            {
                if (child != null && !child.name.ToLower().Contains("bg") && !child.name.ToLower().Contains("background"))
                {
                    panels.Add(child);
                }
            }

            if (index >= 0 && index < panels.Count)
            {
                panels[index].gameObject.SetActive(true);
            }
            else if (index >= 0 && index < sections.childCount)
            {
                sections.GetChild(index).gameObject.SetActive(true);
            }
        }
    }

    public void OpenUnit10SectionA() => OpenUnit10Section(0); // Wagon 1: Learn Concept Cards -> Activity 1
    public void OpenUnit10SectionB() => OpenUnit10Section(1); // Wagon 2: Activity 2 (Which One Fits?)
    public void OpenUnit10SectionC() => OpenUnit10Section(2); // Wagon 3: Activity 3 (Two Meanings)
    public void OpenUnit10SectionD() => OpenUnit10Section(3); // Wagon 4: Activity 4 (Say It Two Ways)
    public void OpenUnit10SectionE() => OpenUnit10Section(4); // Wagon 5: Unit Challenge & Course Finale
    public void OpenUnit10SectionF() => OpenUnit10Section(5); // Activity 1 Direct
    public void OpenUnit10SectionG() => OpenUnit10Section(6); // Completion Report Direct
    public void OpenUnit10SectionH() => OpenUnit10Section(7); // Course Finale Direct

    public void CloseAllUnit10Sections()
    {
        Transform sections = GetUnitSections(10);
        if (sections != null)
        {
            foreach (Transform child in sections)
            {
                if (child != null) child.gameObject.SetActive(false);
            }
            sections.gameObject.SetActive(false);
        }
    }

    public void CloseAllUnit6Sections()
    {
        Transform sections = GetUnitSections(6);
        if (sections != null)
        {
            foreach (Transform child in sections)
            {
                if (child != null) child.gameObject.SetActive(false);
            }
            sections.gameObject.SetActive(false);
        }
    }

    public void CloseAllUnit3Sections()
    {
        Transform sections = GetUnitSections(3);
        if (sections != null)
        {
            foreach (Transform child in sections)
            {
                if (child != null) child.gameObject.SetActive(false);
            }
            sections.gameObject.SetActive(false);
        }
    }

    public void CloseAllUnit4Sections()
    {
        Transform sections = GetUnitSections(4);
        if (sections != null)
        {
            foreach (Transform child in sections)
            {
                if (child != null) child.gameObject.SetActive(false);
            }
            sections.gameObject.SetActive(false);
        }
    }

    public void CloseAllUnit5Sections()
    {
        Transform sections = GetUnitSections(5);
        if (sections != null)
        {
            foreach (Transform child in sections)
            {
                if (child != null) child.gameObject.SetActive(false);
            }
            sections.gameObject.SetActive(false);
        }
    }

    public void CloseAllUnit1Sections()
    {
        if (unit1SectionPanels != null)
        {
            foreach (GameObject panel in unit1SectionPanels)
            {
                if (panel != null) panel.SetActive(false);
            }
        }
        if (unit1SectionsParent != null) unit1SectionsParent.SetActive(false);
    }

    public void CloseAllUnit2Sections()
    {
        if (unit2SectionPanelsArray != null)
        {
            foreach (GameObject panel in unit2SectionPanelsArray)
            {
                if (panel != null) panel.SetActive(false);
            }
        }
        if (unit2SectionsParent != null) unit2SectionsParent.SetActive(false);
    }

    // ------------------ Back Navigation ------------------

    public void BackToUnitSelection()
    {
        StopAllUnitAudio();
        EnsureInitUnitParents();
        CloseAllUnits();

        if (lessonsPanel != null)
        {
            EnableObjectAndParents(lessonsPanel);
            lessonsPanel.SetActive(true);
            foreach (Transform child in lessonsPanel.GetComponentsInChildren<Transform>(true))
            {
                if (child != null)
                {
                    child.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            Canvas c = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (c != null)
            {
                Transform l = c.transform.Find("Lessons") ?? c.transform.Find("Lessons_Panel") ?? c.transform.Find("LessonsPanel");
                if (l != null)
                {
                    lessonsPanel = l.gameObject;
                    EnableObjectAndParents(lessonsPanel);
                    lessonsPanel.SetActive(true);
                }
            }
        }

        if (audioSource != null && selectUnitClip != null)
        {
            audioSource.clip = selectUnitClip;
            audioSource.Play();
        }
    }

    public void BackToSectionSelection()
    {
        BackToSectionSelection(1);
    }

    public void BackToUnit1SectionSelection()
    {
        BackToSectionSelection(1);
    }

    public void BackToSectionSelection(int unitIndex)
    {
        if (unitIndex <= 0) unitIndex = 1;

        StopAllUnitAudio();
        EnsureInitUnitParents();
        CloseAllUnits();
        PlayPopSound();

        GameObject parentObj = GetUnitParent(unitIndex);
        GameObject panelObj = GetUnitSectionSelectionPanel(unitIndex);

        if (parentObj != null)
        {
            EnableObjectAndParents(parentObj);
            parentObj.SetActive(true);
        }
        
        if (panelObj != null)
        {
            EnableObjectAndParents(panelObj);
            panelObj.SetActive(true);
            foreach (Transform child in panelObj.GetComponentsInChildren<Transform>(true))
            {
                if (child != null) child.gameObject.SetActive(true);
            }
        }
        else if (parentObj != null)
        {
            // Fallback search for section selection panel under parent
            Transform p = parentObj.transform.Find($"Unit_{unitIndex}_Section_Selection_Panels")
                       ?? parentObj.transform.Find("Unit_1_Section_Selection_Panels")
                       ?? parentObj.transform.Find("Section_Selection_Panels");
            if (p != null)
            {
                p.gameObject.SetActive(true);
                foreach (Transform child in p.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null) child.gameObject.SetActive(true);
                }
            }
        }
    }

    public void CloseAllUnits()
    {
        StopAllUnitAudio();
        EnsureInitUnitParents();

        void SafeDeactivate(GameObject obj)
        {
            if (obj != null) obj.SetActive(false);
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
        SafeDeactivate(unit3SectionSelectionPanel);
        SafeDeactivate(unit4SectionSelectionPanel);
        SafeDeactivate(unit5SectionSelectionPanel);
        SafeDeactivate(unit6SectionSelectionPanel);
        SafeDeactivate(unit7SectionSelectionPanel);
        SafeDeactivate(unit8SectionSelectionPanel);
        SafeDeactivate(unit9SectionSelectionPanel);
        SafeDeactivate(unit10SectionSelectionPanel);

        CloseAllUnit1Sections();
        CloseAllUnit2Sections();
        CloseAllUnit3Sections();
        CloseAllUnit4Sections();
        CloseAllUnit5Sections();
        CloseAllUnit6Sections();
        CloseAllUnit7Sections();
        CloseAllUnit8Sections();
        CloseAllUnit9Sections();
        CloseAllUnit10Sections();

        // Deactivate all Unit_X_Sections activity containers
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
                Transform uSec = canvasTransform.Find($"Unit_{i}/Unit_{i}_Sections") 
                              ?? canvasTransform.Find($"Unit_{i}/Sections") 
                              ?? canvasTransform.Find($"Unit{i}/Unit_{i}_Sections") 
                              ?? canvasTransform.Find($"Unit{i}/Sections");
                if (uSec != null) uSec.gameObject.SetActive(false);
            }
        }
    }

    public void StopAllUnitAudio()
    {
        if (audioSource != null) audioSource.Stop();

        if (U1_SA_AudioManager_Masters_Phonics.Instance != null)
        {
            U1_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
        }

        AudioSource[] allAudioSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var src in allAudioSources)
        {
            if (src != null)
            {
                string n = src.gameObject.name.ToLower();
                if (src.loop || n.Contains("bg") || n.Contains("music") || n.Contains("background") || n.Contains("theme"))
                {
                    continue;
                }
                try { src.Stop(); } catch (System.Exception) { }
            }
        }
    }

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
            case 3: return unit3SectionSelectionPanel;
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
        return parent.transform.Find($"Unit_{index}_Sections") 
            ?? parent.transform.Find("Sections") 
            ?? parent.transform.Find($"Unit{index}_Sections");
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

    private IEnumerator PlayGuideAfterDelay(AudioClip clip, float delay)
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
        SanitizeArray(ref unit1SectionPanels);
        SanitizeArray(ref unit2SectionPanelsArray);
    }

    private void SanitizeArray(ref GameObject[] arr)
    {
        if (arr == null) return;
        List<GameObject> valid = new List<GameObject>();
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
