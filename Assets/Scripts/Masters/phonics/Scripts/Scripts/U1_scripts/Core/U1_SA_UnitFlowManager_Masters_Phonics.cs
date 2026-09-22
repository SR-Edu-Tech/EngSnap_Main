using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace MastersPhonics
{
    public class U1_SA_UnitFlowManager_Masters_Phonics : MonoBehaviour
    {
        public static U1_SA_UnitFlowManager_Masters_Phonics Instance { get; private set; }

        [Header("Unit Data")]
        public U1_SA_UnitDataSO_Masters_Phonics currentUnitData;

        [Header("Unit 1 Hub & Section Panels")]
        [Tooltip("The panel inside Unit 1 showing the section icons/buttons")]
        public GameObject sectionSelectionPanel;

        [Tooltip("Container holding all section panels (e.g., Unit_1_Sections)")]
        public GameObject sectionsParentContainer;

        [Header("Individual Activity Panels")]
        public GameObject learnPanel;       // Section A: Concept Cards (GM-01)
        public GameObject activity1Panel;   // Section B: Prefix Machine (GM-02 2-slot)
        public GameObject activity2Panel;   // Section C: Family Sort (GM-03 Sort Bins)
        public GameObject activity3Panel;   // Section D: Suffix Party (GM-02 2-slot flipped)
        public GameObject activity4Panel;   // Section E: Word Lab (GM-02 3-slot)
        public GameObject challengePanel;   // Unit Challenge
        public GameObject completionPanel;  // Badge & Summary Report

        [Header("Audio Guide Clips")]
        public AudioClip unitIntroClip;
        public AudioClip selectSectionClip;

        [Header("Events")]
        public UnityEvent OnUnitStarted;
        public UnityEvent<int> OnSectionOpened;
        public UnityEvent OnUnitCompleted;

        private GameObject[] allPanels;
        private int currentActivityIndex = 0;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);

            EnsureAutoBind();
        }

        private void OnEnable()
        {
            EnsureAutoBind();
            OpenSectionSelection();
        }

        /// <summary>
        /// Auto-locates all panels in the hierarchy if they haven't been manually assigned in the inspector.
        /// </summary>
        public void EnsureAutoBind()
        {
            // 1. Find Section Selection Panel
            if (sectionSelectionPanel == null)
            {
                Transform t = transform.Find("Unit_1_Section_Selection_Panels") 
                           ?? transform.Find("Unit_1_Section_Selection_Panel") 
                           ?? transform.Find("Section_Selection_Panels");
                if (t != null) sectionSelectionPanel = t.gameObject;
            }

            // 2. Find Sections Parent Container
            if (sectionsParentContainer == null)
            {
                Transform s = transform.Find("Unit_1_Sections") 
                           ?? transform.Find("Sections") 
                           ?? transform.Find("Unit1_Sections");
                if (s != null) sectionsParentContainer = s.gameObject;
            }

            // 3. Find individual activity panels under sections parent
            Transform container = sectionsParentContainer != null ? sectionsParentContainer.transform : transform;

            if (learnPanel == null)
            {
                Transform t = container.Find("U1_Learn_Concept_Cards_Panel") ?? container.Find("Learn_Panel") ?? container.Find("Concept_Cards_Panel");
                if (t != null) learnPanel = t.gameObject;
            }

            if (activity1Panel == null)
            {
                Transform t = container.Find("U1_Activity_1_Prefix_Machine") ?? container.Find("Activity_1") ?? container.Find("Activity 1");
                if (t != null) activity1Panel = t.gameObject;
            }

            if (activity2Panel == null)
            {
                Transform t = container.Find("U1_Activity_2_Family_Sort") ?? container.Find("Activity_2") ?? container.Find("Activity 2");
                if (t != null) activity2Panel = t.gameObject;
            }

            if (activity3Panel == null)
            {
                Transform t = container.Find("U1_Activity_3_Suffix_Party") ?? container.Find("Activity_3") ?? container.Find("Activity 3");
                if (t != null) activity3Panel = t.gameObject;
            }

            if (activity4Panel == null)
            {
                Transform t = container.Find("U1_Activity_4_Word_Lab") ?? container.Find("Activity_4") ?? container.Find("Activity 4");
                if (t != null) activity4Panel = t.gameObject;
            }

            if (completionPanel == null)
            {
                Transform t = container.Find("U1_COMPLETE_Panel") ?? container.Find("Complete_Panel") ?? container.Find("Completion_Panel");
                if (t != null) completionPanel = t.gameObject;
            }

            allPanels = new GameObject[]
            {
                learnPanel,
                activity1Panel,
                activity2Panel,
                activity3Panel,
                activity4Panel,
                challengePanel,
                completionPanel
            };
        }

        // ------------------ Hub & Navigation ------------------

        public void OpenSectionSelection()
        {
            EnsureAutoBind();
            CloseAllActivityPanels();

            // Ensure Unit_1 parent itself is active
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            if (sectionSelectionPanel != null)
            {
                sectionSelectionPanel.SetActive(true);

                // Ensure all children inside sectionSelectionPanel (Viewport, Content, Section Cards) are active
                foreach (Transform child in sectionSelectionPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null)
                    {
                        child.gameObject.SetActive(true);
                    }
                }

                // Auto-hook the Back Button inside Section Selection to return to Lessons Menu
                Button[] buttons = sectionSelectionPanel.GetComponentsInChildren<Button>(true);
                foreach (var b in buttons)
                {
                    if (b != null && (b.name.ToLower().Contains("back") || b.name.ToLower().Contains("return") || b.name.ToLower().Contains("unit_selection_back")))
                    {
                        b.onClick.RemoveListener(BackToMainLessonsMenu);
                        b.onClick.AddListener(BackToMainLessonsMenu);
                    }
                }
            }

            if (sectionsParentContainer != null)
            {
                sectionsParentContainer.SetActive(false);
            }

            if (U1_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                if (unitIntroClip != null)
                    U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(unitIntroClip);
                else if (selectSectionClip != null)
                    U1_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(selectSectionClip);
            }
        }

        public void OpenSection(int index)
        {
            EnsureAutoBind();
            CloseAllActivityPanels();

            // Hide section selection
            if (sectionSelectionPanel != null)
                sectionSelectionPanel.SetActive(false);

            // Show sections container
            if (sectionsParentContainer != null)
            {
                sectionsParentContainer.SetActive(true);

                // Also make sure common elements inside Unit_1_Sections (like Back_Button) are visible
                Transform secBack = sectionsParentContainer.transform.Find("Back_Button") 
                                 ?? sectionsParentContainer.transform.Find("Section_Selection_Back");
                if (secBack != null) secBack.gameObject.SetActive(true);
            }

            currentActivityIndex = index;

            GameObject targetPanel = GetPanelByIndex(index);
            if (targetPanel != null)
            {
                Debug.Log($"<color=cyan>[UnitFlow] OpenSection({index}) -> Activating Target Panel: '{targetPanel.name}'</color>");
                
                // Explicitly ensure completion panel is closed when opening any activity 0-4
                if (completionPanel != null && completionPanel.activeSelf)
                {
                    completionPanel.SetActive(false);
                }

                targetPanel.SetActive(true);

                // Enable all child UI elements of the active panel so they are fully visible
                foreach (Transform child in targetPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null)
                    {
                        string cName = child.name.ToLower();
                        // For gameplay activities (index > 0), hide the completion next button until rounds are finished.
                        // For Learn Panel (index == 0), the Next Button is required to advance between the 4 cards!
                        if (index > 0 && (cName.Contains("next") || cName.Contains("continue")))
                        {
                            child.gameObject.SetActive(false); 
                        }
                        else
                        {
                            child.gameObject.SetActive(true);
                        }
                    }
                }

                // If gameplay activity (index > 0), ensure container-level Next Button is hidden during gameplay
                if (index > 0)
                {
                    SetNextButtonVisible(false);
                }

                // Auto-hook any back button inside the active panel to safely return to Section Selection
                Button[] buttons = targetPanel.GetComponentsInChildren<Button>(true);
                foreach (var b in buttons)
                {
                    if (b != null && (b.name.ToLower().Contains("back") || b.name.ToLower().Contains("return")))
                    {
                        b.onClick.RemoveListener(BackToSectionSelection);
                        b.onClick.AddListener(BackToSectionSelection);
                    }
                }
            }

            // Initialize the game mode with its data
            InitializeGameMode(index, targetPanel);

            OnSectionOpened?.Invoke(index);
        }

        public void OpenLearn() => OpenSection(0);
        public void OpenActivity1() => OpenSection(1);
        public void OpenActivity2() => OpenSection(2);
        public void OpenActivity3() => OpenSection(3);
        public void OpenActivity4() => OpenSection(4);
        public void OpenChallenge() => OpenSection(5);

        private GameObject GetPanelByIndex(int index)
        {
            EnsureAutoBind();
            Transform container = sectionsParentContainer != null ? sectionsParentContainer.transform : transform;

            switch (index)
            {
                case 0: 
                    if (learnPanel == null) learnPanel = container.Find("U1_learn_Concept_Cards_Panel")?.gameObject ?? container.Find("U1_Learn_Concept_Cards_Panel")?.gameObject;
                    return learnPanel;
                case 1: 
                    if (activity1Panel == null) activity1Panel = container.Find("U1_Activity_1_Prefix_Machine")?.gameObject;
                    return activity1Panel;
                case 2: 
                    if (activity2Panel == null) activity2Panel = container.Find("U1_Activity_2_Family_Sort")?.gameObject;
                    return activity2Panel;
                case 3: 
                    if (activity3Panel == null) activity3Panel = container.Find("U1_Activity_3_Suffix_Party")?.gameObject;
                    return activity3Panel;
                case 4: 
                    // Guarantee that index 4 is strictly U1_Activity_4_Word_Lab
                    if (activity4Panel == null || activity4Panel.name.ToLower().Contains("complete"))
                    {
                        Transform t = container.Find("U1_Activity_4_Word_Lab") ?? container.Find("Activity_4") ?? container.Find("Word_Lab");
                        if (t != null) activity4Panel = t.gameObject;
                    }
                    return activity4Panel;
                case 5: return challengePanel;
                case 6: 
                    if (completionPanel == null) completionPanel = container.Find("U1_COMPLETE_Panel")?.gameObject;
                    return completionPanel;
                default: return null;
            }
        }

        private void InitializeGameMode(int index, GameObject panel)
        {
            if (panel == null || currentUnitData == null || currentUnitData.activities == null) return;

            if (index < currentUnitData.activities.Count)
            {
                var actData = currentUnitData.activities[index];

                if (index == 0) // Learn (GM-01)
                {
                    var gm01 = panel.GetComponentInChildren<U1_SA_GM01_ConceptCards_Masters_Phonics>(true);
                    if (gm01 != null) gm01.Initialize(actData);
                }
                else if (index == 1) // Prefix Machine (GM-02)
                {
                    var gm02 = panel.GetComponentInChildren<U1_SA_GM02_PrefixMachine_Masters_Phonics>(true);
                    if (gm02 != null) gm02.Initialize(actData);
                }
                else if (index == 2) // Family Sort (GM-03)
                {
                    var gm03 = panel.GetComponentInChildren<U1_SA_GM03_SortingBins_Masters_Phonics>(true);
                    if (gm03 != null) gm03.Initialize(actData);
                }
                else if (index == 3) // Suffix Party (GM-04)
                {
                    var gm04 = panel.GetComponentInChildren<U1_SA_GM04_SuffixParty_Masters_Phonics>(true);
                    if (gm04 != null) gm04.Initialize(actData);
                }
                else if (index == 4) // Word Lab (GM-05)
                {
                    var gm05 = panel.GetComponentInChildren<U1_SA_GM05_WordBuilder_Masters_Phonics>(true);
                    if (gm05 != null)
                    {
                        gm05.Initialize(actData);
                    }
                }
            }
        }

        public void CloseAllActivityPanels()
        {
            if (allPanels == null)
            {
                allPanels = new GameObject[]
                {
                    learnPanel, activity1Panel, activity2Panel, activity3Panel, activity4Panel, challengePanel, completionPanel
                };
            }

            foreach (var p in allPanels)
            {
                if (p != null) p.SetActive(false);
            }
        }

        // ------------------ Back Buttons ------------------

        public void BackToSectionSelection()
        {
            if (U1_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
            }
            SetNextButtonVisible(false);
            OpenSectionSelection();
        }

        public void BackToMainLessonsMenu()
        {
            if (U1_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
            }

            SetNextButtonVisible(false);
            if (sectionsParentContainer != null) sectionsParentContainer.SetActive(false);
            if (sectionSelectionPanel != null) sectionSelectionPanel.SetActive(false);

            var usp = Unit_Selection_Panel_Masters_Phonics.Instance 
                   ?? FindFirstObjectByType<Unit_Selection_Panel_Masters_Phonics>(FindObjectsInactive.Include);

            if (usp != null)
            {
                usp.gameObject.SetActive(true);
                usp.BackToUnitSelection();
            }
            else
            {
                Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var c in canvases)
                {
                    if (c == null) continue;
                    Transform l = c.transform.Find("Lessons") 
                               ?? c.transform.Find("Unit_Selection_Panel")
                               ?? c.transform.Find("LessonsPanel")
                               ?? c.transform.Find("Unit_Selection_Panels");
                    if (l != null)
                    {
                        l.gameObject.SetActive(true);
                        foreach (Transform child in l.GetComponentsInChildren<Transform>(true))
                        {
                            if (child != null) child.gameObject.SetActive(true);
                        }
                    }
                }
            }

            gameObject.SetActive(false);
        }

        // ------------------ Activity Completion & Sequential Flow ------------------

        public void SetNextButtonVisible(bool visible)
        {
            // Search in active panel and container
            List<Button> nextButtons = new List<Button>();

            if (sectionsParentContainer != null)
            {
                Button[] containerBtns = sectionsParentContainer.GetComponentsInChildren<Button>(true);
                foreach (var b in containerBtns)
                {
                    if (b != null && (b.name.ToLower().Contains("next") || b.name.ToLower().Contains("continue")))
                    {
                        nextButtons.Add(b);
                    }
                }
            }

            GameObject curPanel = GetPanelByIndex(currentActivityIndex);
            if (curPanel != null)
            {
                Button[] panelBtns = curPanel.GetComponentsInChildren<Button>(true);
                foreach (var b in panelBtns)
                {
                    if (b != null && (b.name.ToLower().Contains("next") || b.name.ToLower().Contains("continue")))
                    {
                        if (!nextButtons.Contains(b)) nextButtons.Add(b);
                    }
                }
            }

            foreach (var btn in nextButtons)
            {
                if (btn != null)
                {
                    btn.gameObject.SetActive(visible);
                    if (visible)
                    {
                        // Enforce full button scale (ensure it is not shrunken or microscopic)
                        if (btn.transform.localScale.x < 0.8f || btn.transform.localScale.x > 1.8f || btn.transform.localScale == Vector3.zero)
                        {
                            btn.transform.localScale = Vector3.one;
                        }

                        RectTransform rt = btn.GetComponent<RectTransform>();
                        if (rt != null && (rt.sizeDelta.x < 100f || rt.sizeDelta.y < 100f))
                        {
                            rt.sizeDelta = new Vector2(150f, 150f);
                        }

                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(OpenNextSection);
                        StartCoroutine(PulseButton(btn.transform));
                    }
                }
            }
        }

        private System.Collections.IEnumerator PulseButton(Transform tr)
        {
            if (tr == null) yield break;
            Vector3 orig = (tr.localScale.x < 0.8f) ? Vector3.one : tr.localScale;
            tr.localScale = orig;
            for (int i = 0; i < 3; i++)
            {
                tr.localScale = orig * 1.18f;
                yield return new WaitForSeconds(0.18f);
                tr.localScale = orig;
                yield return new WaitForSeconds(0.18f);
            }
            if (tr != null) tr.localScale = orig;
        }

        private bool isTransitioning = false;

        public void OpenNextSection()
        {
            if (isTransitioning)
            {
                Debug.LogWarning("[NextButton] Transition already in progress. Ignoring duplicate click.");
                return;
            }

            isTransitioning = true;
            int fromIndex = currentActivityIndex;

            SetNextButtonVisible(false);

            Debug.Log($"<color=cyan>[NextButton] Transitioning from Section {fromIndex}</color>");

            switch (fromIndex)
            {
                case 0: // Section A: Learn -> Section B: Activity 1 (Prefix Machine)
                    Debug.Log("<color=green>[NextButton] Advancing: Learn -> Prefix Machine (Activity 1)</color>");
                    OpenSection(1);
                    break;

                case 1: // Section B: Activity 1 (Prefix Machine) -> Section C: Activity 2 (Family Sort)
                    Debug.Log("<color=green>[NextButton] Advancing: Prefix Machine -> Family Sort (Activity 2)</color>");
                    OpenSection(2);
                    break;

                case 2: // Section C: Activity 2 (Family Sort) -> Section D: Activity 3 (Suffix Party)
                    Debug.Log("<color=green>[NextButton] Advancing: Family Sort -> Suffix Party (Activity 3)</color>");
                    OpenSection(3);
                    break;

                case 3: // Section D: Activity 3 (Suffix Party) -> Section E: Activity 4 (Word Lab)
                    Debug.Log("<color=green>[NextButton] Advancing: Suffix Party -> Word Lab (Activity 4)</color>");
                    OpenSection(4);
                    break;

                case 4: // Section E: Activity 4 (Word Lab) -> Unit Completion
                    Debug.Log("<color=green>[NextButton] Advancing: Word Lab -> Unit 1 Complete!</color>");
                    ShowUnitCompletion();
                    break;

                default:
                    ShowUnitCompletion();
                    break;
            }

            StartCoroutine(UnlockTransitionAfterDelay(0.6f));
        }

        private System.Collections.IEnumerator UnlockTransitionAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            isTransitioning = false;
        }

        public void OpenNextUnit()
        {
            if (U1_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U1_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
            }

            if (Unit_Selection_Panel_Masters_Phonics.Instance != null)
            {
                Unit_Selection_Panel_Masters_Phonics.Instance.OpenUnit(2);
            }
        }

        public void OnActivityComplete()
        {
            Debug.Log($"<color=green>Activity {currentActivityIndex} completed! Revealing Next Button...</color>");
            SetNextButtonVisible(true);
        }

        public void ShowUnitCompletion()
        {
            CloseAllActivityPanels();
            if (completionPanel != null)
            {
                completionPanel.SetActive(true);
            }
            OnUnitCompleted?.Invoke();
        }
    }
}
