using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public enum Unit5ActivityState
    {
        SectionSelection = -1,
        LearnConceptCards = 0,
        Activity1DoorOrGate = 1,
        Activity2LongOrShort = 2,
        Activity3TheFirstDoor = 3,
        Activity4ClosedWordHunt = 4,
        Activity5BigWordReader = 5,
        UnitChallenge = 6,
        CompletePanel = 7
    }

    public class U5_SA_UnitFlowManager_Masters_Phonics : MonoBehaviour
    {
        public static U5_SA_UnitFlowManager_Masters_Phonics Instance { get; private set; }

        [Header("1. Main Containers (Auto-Bound)")]
        [SerializeField] private GameObject sectionSelectionPanel; // Unit_5_Section_Selection_Panels
        [SerializeField] private GameObject sectionsParentContainer; // Unit_5_Sections

        [Header("2. Unit 5 Activity Panels (Auto-Bound)")]
        [SerializeField] private GameObject learnPanel;       // 0: Learn Concept Cards (4 Cards)
        [SerializeField] private GameObject activity1Panel;   // 1: Activity 1 (Door or Gate - GM-03s)
        [SerializeField] private GameObject activity2Panel;   // 2: Activity 2 (Long or Short? - GM-04)
        [SerializeField] private GameObject activity3Panel;   // 3: Activity 3 (The First Door - GM-03)
        [SerializeField] private GameObject activity4Panel;   // 4: Activity 4 (Closed-Word Hunt - GM-07)
        [SerializeField] private GameObject activity5Panel;   // 5: Activity 5 (Big Word Reader - GM-05)
        [SerializeField] private GameObject challengePanel;   // 6: Unit 5 Final Challenge
        [SerializeField] private GameObject completionPanel;  // 7: U5_COMPLETE_Panel

        [Header("3. Global Header & Navigation (Static Back Button)")]
        [SerializeField] private Button globalBackButton;
        [SerializeField] private Button nextActivityButton;
        [SerializeField] private TextMeshProUGUI cumulativeScoreTMP;
        [SerializeField] private Slider overallProgressBar;

        [Header("4. Completion UI Elements")]
        [SerializeField] private TextMeshProUGUI finalScoreTMP;
        [SerializeField] private TextMeshProUGUI finalStatsTMP;
        [SerializeField] private Button continueToSignboardButton;
        [SerializeField] private Button nextUnitButton;

        [Header("5. Narration & Voice Audio")]
        [Tooltip("U05_VO_unit_intro: 'Unit Five: Two of the seven types...'")]
        public AudioClip unitIntroClip;
        [Tooltip("U05_VO_unit_complete: 'Unit Five, done. Thirty-four doors opened...'")]
        public AudioClip unitCompleteVoiceClip;
        public AudioClip mapUpdateVoiceClip;

        [Header("Runtime State")]
        [SerializeField] private int currentActivityIndex = -1;
        private GameObject[] allPanels;
        private bool isTransitioning = false;
        private int totalCumulativeScore = 0;
        private const int MAX_POSSIBLE_SCORE = 5600;
        private int doorsOpenedCount = 0;
        private int doorsClosedCount = 0;

        public int CurrentActivityIndex => currentActivityIndex;
        public int TotalCumulativeScore => totalCumulativeScore;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            EnsureAutoBind();
        }

        private void Start()
        {
            EnsureAutoBind();
            ShowSectionSelection();
        }

        private void OnEnable()
        {
            EnsureAutoBind();
        }

        public void EnsureAutoBind()
        {
            if (sectionSelectionPanel == null)
            {
                Transform s = transform.Find("Unit_5_Section_Selection_Panels") 
                           ?? transform.Find("Unit_5_Section_Selection_Panel")
                           ?? transform.Find("Section_Selection_Panels") 
                           ?? transform.Find("Section_Selection_Panel")
                           ?? transform.Find("Unit5_Section_Selection_Panels")
                           ?? transform.Find("Signboard");
                if (s != null) sectionSelectionPanel = s.gameObject;
            }

            if (sectionsParentContainer == null)
            {
                Transform s = transform.Find("Unit_5_Sections") 
                           ?? transform.Find("Sections") 
                           ?? transform.Find("Unit5_Sections")
                           ?? transform.Find("Sections_Container");
                if (s != null) sectionsParentContainer = s.gameObject;
            }

            Transform container = sectionsParentContainer != null ? sectionsParentContainer.transform : transform;

            if (learnPanel == null)
            {
                Transform t = container.Find("U5_learn_Concept_Cards_Panel") ?? container.Find("U5_Learn_Concept_Cards_Panel") ?? container.Find("Learn_Panel") ?? container.Find("U5_Learn") ?? container.Find("Concept_Cards");
                if (t != null) learnPanel = t.gameObject;
                else learnPanel = container.GetComponentInChildren<U5_SA_GM01_ConceptCards_Masters_Phonics>(true)?.gameObject
                               ?? transform.GetComponentInChildren<U5_SA_GM01_ConceptCards_Masters_Phonics>(true)?.gameObject;
            }

            if (activity1Panel == null)
            {
                Transform t = container.Find("U5_Activity_1_Door_Or_Gate") ?? container.Find("U5_Activity_1") ?? container.Find("Activity_1") ?? container.Find("Door_Or_Gate");
                if (t != null) activity1Panel = t.gameObject;
                else activity1Panel = container.GetComponentInChildren<U5_SA_GM03s_DoorOrGate_Masters_Phonics>(true)?.gameObject
                                   ?? transform.GetComponentInChildren<U5_SA_GM03s_DoorOrGate_Masters_Phonics>(true)?.gameObject;
            }

            if (activity2Panel == null)
            {
                Transform t = container.Find("U5_Activity_2_Long_Or_Short") ?? container.Find("U5_Activity_2") ?? container.Find("Activity_2") ?? container.Find("Long_Or_Short");
                if (t != null) activity2Panel = t.gameObject;
                else activity2Panel = container.GetComponentInChildren<U5_SA_GM04_LongOrShort_Masters_Phonics>(true)?.gameObject
                                   ?? transform.GetComponentInChildren<U5_SA_GM04_LongOrShort_Masters_Phonics>(true)?.gameObject;
            }

            if (activity3Panel == null)
            {
                Transform t = container.Find("U5_Activity_3_The_First_Door") ?? container.Find("U5_Activity_3") ?? container.Find("Activity_3") ?? container.Find("The_First_Door") ?? container.Find("First_Door");
                if (t != null) activity3Panel = t.gameObject;
                else activity3Panel = container.GetComponentInChildren<U5_SA_GM03_TheFirstDoor_Masters_Phonics>(true)?.gameObject
                                   ?? transform.GetComponentInChildren<U5_SA_GM03_TheFirstDoor_Masters_Phonics>(true)?.gameObject;
            }

            if (activity4Panel == null)
            {
                Transform t = container.Find("U5_Activity_4_Closed_Word_Hunt") ?? container.Find("U5_Activity_4") ?? container.Find("Activity_4") ?? container.Find("Closed_Word_Hunt") ?? container.Find("ClosedWordHunt") ?? container.Find("Word_Hunt");
                if (t != null) activity4Panel = t.gameObject;
                else activity4Panel = container.GetComponentInChildren<U5_SA_GM07_ClosedWordHunt_Masters_Phonics>(true)?.gameObject
                                   ?? transform.GetComponentInChildren<U5_SA_GM07_ClosedWordHunt_Masters_Phonics>(true)?.gameObject;

                if (activity4Panel == null)
                {
                    GameObject act4Obj = new GameObject("U5_Activity_4_Closed_Word_Hunt", typeof(RectTransform));
                    act4Obj.transform.SetParent(container, false);
                    RectTransform rt = act4Obj.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    act4Obj.AddComponent<U5_SA_GM07_ClosedWordHunt_Masters_Phonics>();
                    activity4Panel = act4Obj;
                }
            }

            if (activity5Panel == null)
            {
                Transform t = container.Find("U5_Activity_5_Big_Word_Reader") ?? container.Find("U5_Activity_5_BigWordReader") ?? container.Find("U5_Activity_5") ?? container.Find("Activity_5") ?? container.Find("Big_Word_Reader") ?? container.Find("BigWordReader");
                if (t != null) activity5Panel = t.gameObject;
                else activity5Panel = container.GetComponentInChildren<U5_SA_GM05_BigWordReader_Masters_Phonics>(true)?.gameObject 
                                   ?? container.GetComponentInChildren<U5_SA_SevenTypesMap_Masters_Phonics>(true)?.gameObject
                                   ?? transform.GetComponentInChildren<U5_SA_GM05_BigWordReader_Masters_Phonics>(true)?.gameObject
                                   ?? transform.GetComponentInChildren<U5_SA_SevenTypesMap_Masters_Phonics>(true)?.gameObject;

                if (activity5Panel == null)
                {
                    GameObject act5Obj = new GameObject("U5_Activity_5_Big_Word_Reader", typeof(RectTransform));
                    act5Obj.transform.SetParent(container, false);
                    RectTransform rt = act5Obj.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    act5Obj.AddComponent<U5_SA_GM05_BigWordReader_Masters_Phonics>();
                    activity5Panel = act5Obj;
                }
            }

            if (challengePanel == null)
            {
                Transform t = container.Find("U5_UnitChallenge_Panel") ?? container.Find("U5_Unit_Challenge") ?? container.Find("Challenge_Panel") ?? container.Find("Unit_Challenge") ?? container.Find("Challenge");
                if (t != null) challengePanel = t.gameObject;
                else challengePanel = container.GetComponentInChildren<U5_SA_UnitChallenge_Masters_Phonics>(true)?.gameObject
                                   ?? transform.GetComponentInChildren<U5_SA_UnitChallenge_Masters_Phonics>(true)?.gameObject;

                if (challengePanel == null)
                {
                    GameObject chalObj = new GameObject("U5_UnitChallenge_Panel", typeof(RectTransform));
                    chalObj.transform.SetParent(container, false);
                    RectTransform rt = chalObj.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = Vector2.zero;
                    rt.offsetMax = Vector2.zero;
                    chalObj.AddComponent<U5_SA_UnitChallenge_Masters_Phonics>();
                    challengePanel = chalObj;
                }
            }

            if (completionPanel == null)
            {
                Transform t = container.Find("U5_COMPLETE_Panel") ?? container.Find("U5_Complete_Panel") ?? container.Find("Complete_Panel") ?? container.Find("Completion_Panel") ?? container.Find("U5_COMPLETE");
                if (t != null) completionPanel = t.gameObject;
            }

            allPanels = new GameObject[]
            {
                learnPanel,
                activity1Panel,
                activity2Panel,
                activity3Panel,
                activity4Panel,
                activity5Panel,
                challengePanel,
                completionPanel
            };

            // 1. Auto-bind Global Static Back Button
            if (globalBackButton == null)
            {
                Transform t = transform.Find("TopBar/Back_Button") 
                           ?? transform.Find("BackButton") 
                           ?? transform.Find("Back_Button")
                           ?? transform.Find("TopBar/btn_back")
                           ?? (sectionsParentContainer != null ? sectionsParentContainer.transform.Find("Back_Button") : null)
                           ?? (sectionSelectionPanel != null ? sectionSelectionPanel.transform.Find("Back_Button") : null);
                if (t != null) globalBackButton = t.GetComponent<Button>();
            }
            if (globalBackButton != null)
            {
                globalBackButton.gameObject.SetActive(true);
                globalBackButton.onClick.RemoveListener(OnGlobalBackTapped);
                globalBackButton.onClick.AddListener(OnGlobalBackTapped);
            }

            // Also search and hook any Back buttons inside section selection panel & sections parent
            WireChildBackButtons(sectionSelectionPanel);
            WireChildBackButtons(sectionsParentContainer);

            // Wire Signboard Section Selection Buttons
            WireSectionSelectionButtons();

            // 2. Auto-bind Next Activity Button
            if (nextActivityButton == null)
            {
                Transform t = transform.Find("NextActivity_Button") 
                           ?? transform.Find("NextButton") 
                           ?? transform.Find("Btn_NextActivity")
                           ?? transform.Find("Next_Button");
                if (t != null) nextActivityButton = t.GetComponent<Button>();
            }
            if (nextActivityButton != null)
            {
                nextActivityButton.onClick.RemoveListener(OpenNextSection);
                nextActivityButton.onClick.AddListener(OpenNextSection);
                nextActivityButton.gameObject.SetActive(false);
            }

            // 3. Auto-bind Completion panel buttons
            if (completionPanel != null)
            {
                if (continueToSignboardButton == null)
                {
                    Transform t = completionPanel.transform.Find("ContinueButton") 
                               ?? completionPanel.transform.Find("Btn_Continue") 
                               ?? completionPanel.transform.Find("ContinueToSignboardButton")
                               ?? completionPanel.transform.Find("ReturnButton");
                    if (t != null) continueToSignboardButton = t.GetComponent<Button>();
                }
                if (continueToSignboardButton != null)
                {
                    continueToSignboardButton.onClick.RemoveAllListeners();
                    continueToSignboardButton.onClick.AddListener(() =>
                    {
                        StartCoroutine(PulseButton(continueToSignboardButton.transform));
                        ShowSectionSelection();
                    });
                }

                if (nextUnitButton == null)
                {
                    Transform t = completionPanel.transform.Find("NextUnitButton") 
                               ?? completionPanel.transform.Find("Btn_NextUnit") 
                               ?? completionPanel.transform.Find("NextButton")
                               ?? completionPanel.transform.Find("Btn_Home")
                               ?? completionPanel.transform.Find("Home_Button");
                    if (t != null) nextUnitButton = t.GetComponent<Button>();
                }
                if (nextUnitButton != null)
                {
                    nextUnitButton.onClick.RemoveAllListeners();
                    nextUnitButton.onClick.AddListener(() =>
                    {
                        StartCoroutine(PulseButton(nextUnitButton.transform));
                        BackToLessonsMenu();
                    });
                }
            }
        }

        private void WireSectionSelectionButtons()
        {
            if (sectionSelectionPanel == null) return;

            Button[] allButtons = sectionSelectionPanel.GetComponentsInChildren<Button>(true);
            List<Button> secButtons = new List<Button>();

            foreach (var b in allButtons)
            {
                if (b == null) continue;
                string bName = b.name.ToLower();

                if (bName.Contains("back") || bName.Contains("return") || bName.Contains("home"))
                {
                    b.onClick.RemoveListener(BackToLessonsMenu);
                    b.onClick.AddListener(BackToLessonsMenu);
                }
                else
                {
                    secButtons.Add(b);
                }
            }

            // Check if 5-button signboard setup (Truck 1=Learn->Act1, Truck 2=Act2, Truck 3=Act3, Truck 4=Act4, Truck 5=Act5)
            if (secButtons.Count == 5)
            {
                int[] fiveSectionIndices = new int[] { 0, 2, 3, 4, 5 }; // 0: Learn->Act1, 2: Act2, 3: Act3, 4: Act4, 5: Act5

                for (int i = 0; i < secButtons.Count; i++)
                {
                    Button b = secButtons[i];
                    string bName = b.name.ToLower();

                    if (bName.Contains("learn") || bName.Contains("concept") || bName.Contains("door_or_gate") || bName.Contains("section_1") || bName.Contains("section1"))
                    {
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(OpenSection1);
                    }
                    else if (bName.Contains("act2") || bName.Contains("long_or_short") || bName.Contains("section_2") || bName.Contains("section2"))
                    {
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(OpenSection2);
                    }
                    else if (bName.Contains("act3") || bName.Contains("first_door") || bName.Contains("section_3") || bName.Contains("section3"))
                    {
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(OpenSection3);
                    }
                    else if (bName.Contains("act4") || bName.Contains("closed") || bName.Contains("word_hunt") || bName.Contains("section_4") || bName.Contains("section4"))
                    {
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(OpenSection4);
                    }
                    else if (bName.Contains("act5") || bName.Contains("big_word") || bName.Contains("challenge") || bName.Contains("section_5") || bName.Contains("section5"))
                    {
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(OpenSection5);
                    }
                    else
                    {
                        int targetIdx = fiveSectionIndices[i];
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(() => OpenSection(targetIdx));
                    }
                }
            }
            else
            {
                // Standard keyword or sequential fallback for other button counts
                for (int i = 0; i < secButtons.Count; i++)
                {
                    Button b = secButtons[i];
                    string bName = b.name.ToLower();

                    if (bName.Contains("learn") || bName.Contains("concept"))
                    {
                        b.onClick.RemoveListener(OpenSectionA);
                        b.onClick.AddListener(OpenSectionA);
                    }
                    else if (bName.Contains("act1") || bName.Contains("door_or_gate"))
                    {
                        b.onClick.RemoveListener(OpenSectionB);
                        b.onClick.AddListener(OpenSectionB);
                    }
                    else if (bName.Contains("act2") || bName.Contains("long_or_short"))
                    {
                        b.onClick.RemoveListener(OpenSectionC);
                        b.onClick.AddListener(OpenSectionC);
                    }
                    else if (bName.Contains("act3") || bName.Contains("first_door"))
                    {
                        b.onClick.RemoveListener(OpenSectionD);
                        b.onClick.AddListener(OpenSectionD);
                    }
                    else if (bName.Contains("act4") || bName.Contains("closed") || bName.Contains("word_hunt"))
                    {
                        b.onClick.RemoveListener(OpenSectionE);
                        b.onClick.AddListener(OpenSectionE);
                    }
                    else if (bName.Contains("act5") || bName.Contains("big_word"))
                    {
                        b.onClick.RemoveListener(OpenSectionF);
                        b.onClick.AddListener(OpenSectionF);
                    }
                    else if (bName.Contains("challenge") || bName.Contains("act6"))
                    {
                        b.onClick.RemoveListener(OpenSectionG);
                        b.onClick.AddListener(OpenSectionG);
                    }
                    else
                    {
                        int capturedIndex = i;
                        if (capturedIndex < 7)
                        {
                            b.onClick.RemoveAllListeners();
                            b.onClick.AddListener(() => OpenSection(capturedIndex));
                        }
                    }
                }
            }
        }

        private void WireChildBackButtons(GameObject root)
        {
            if (root == null) return;
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                if (b != null && (b.name.ToLower().Contains("back") || b.name.ToLower().Contains("return")))
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(OnGlobalBackTapped);
                }
            }
        }

        public void AddScore(int points)
        {
            totalCumulativeScore += points;
            UpdateScoreUI();
        }

        public void ResetTotalScore()
        {
            totalCumulativeScore = 0;
            UpdateScoreUI();
        }

        public void RecordDoorOpened() => doorsOpenedCount++;
        public void RecordDoorClosed() => doorsClosedCount++;

        private void UpdateScoreUI()
        {
            if (cumulativeScoreTMP != null)
            {
                cumulativeScoreTMP.text = $"Score: <b>{totalCumulativeScore}</b>";
            }
            if (overallProgressBar != null)
            {
                overallProgressBar.value = Mathf.Clamp01((float)totalCumulativeScore / MAX_POSSIBLE_SCORE);
            }
        }

        // =========================================================================
        // Section Navigation
        // =========================================================================

        public void ShowSectionSelection()
        {
            EnsureAutoBind();
            currentActivityIndex = -1;
            CloseAllActivityPanels();

            if (!gameObject.activeSelf) gameObject.SetActive(true);

            if (sectionSelectionPanel != null)
            {
                sectionSelectionPanel.SetActive(true);
                foreach (Transform child in sectionSelectionPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null) child.gameObject.SetActive(true);
                }
            }
            if (sectionsParentContainer != null)
            {
                sectionsParentContainer.SetActive(false);
            }

            // Ensure Static Back Button is visible
            if (globalBackButton != null)
            {
                globalBackButton.gameObject.SetActive(true);
            }

            // Ensure Next Button is hidden on Section Selection
            SetNextButtonVisible(false);

            if (U5_SA_AudioManager_Masters_Phonics.Instance != null && unitIntroClip != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(unitIntroClip);
            }
        }

        public void OpenSectionSelection() => ShowSectionSelection();

        public void OpenSection(int sectionIndex)
        {
            EnsureAutoBind();
            currentActivityIndex = sectionIndex;

            if (sectionSelectionPanel != null)
            {
                sectionSelectionPanel.SetActive(false);
            }
            if (sectionsParentContainer != null)
            {
                sectionsParentContainer.SetActive(true);
            }

            CloseAllActivityPanels();

            GameObject targetPanel = GetPanelByIndex(sectionIndex);
            if (targetPanel != null)
            {
                targetPanel.SetActive(true);
                foreach (Transform child in targetPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null)
                    {
                        string cName = child.name.ToLower();
                        if (cName.Contains("nextactivity") || cName.Contains("next_section") || cName.Contains("btn_nextactivity"))
                        {
                            child.gameObject.SetActive(false);
                        }
                        else
                        {
                            child.gameObject.SetActive(true);
                        }
                    }
                }
            }

            // Ensure Static Back Button stays visible during activity
            if (globalBackButton != null)
            {
                globalBackButton.gameObject.SetActive(true);
            }

            // Hide Next button until activity ends
            SetNextButtonVisible(false);
        }

        // Section shortcuts for Inspector button OnClick events
        public void OpenSectionA() => OpenSection(0); // Learn: Concept Cards
        public void OpenSectionB() => OpenSection(1); // Activity 1: Door or Gate
        public void OpenSectionC() => OpenSection(2); // Activity 2: Long or Short?
        public void OpenSectionD() => OpenSection(3); // Activity 3: The First Door
        public void OpenSectionE() => OpenSection(4); // Activity 4: Closed-Word Hunt
        public void OpenSectionF() => OpenSection(5); // Activity 5: Big Word Reader
        public void OpenSectionG() => OpenSection(6); // Unit Challenge
        public void OpenSectionH() => OpenSection(7); // Completion Panel

        // 5-Section Signboard Specific Shortcuts (Matches the 5 Trucks)
        public void OpenSection1() => OpenSection(0); // Section 1 Truck: Learn (Concept Cards) -> Next opens Activity 1 (Door or Gate)
        public void OpenSection2() => OpenSection(2); // Section 2 Truck: Activity 2 (Long or Short)
        public void OpenSection3() => OpenSection(3); // Section 3 Truck: Activity 3 (The First Door)
        public void OpenSection4() => OpenSection(4); // Section 4 Truck: Activity 4 (Closed-Word Hunt)
        public void OpenSection5() => OpenSection(5); // Section 5 Truck: Activity 5 (Big Word Reader)

        public void OpenLearn() => OpenSection(0);
        public void OpenActivity1() => OpenSection(1);
        public void OpenActivity2() => OpenSection(2);
        public void OpenActivity3() => OpenSection(3);
        public void OpenActivity4() => OpenSection(4);
        public void OpenActivity5() => OpenSection(5);
        public void OpenUnitChallenge() => OpenSection(6);
        public void OpenCompletionPanel() => OpenSection(7);

        public void OpenActivity(Unit5ActivityState targetState)
        {
            OpenSection((int)targetState);
        }

        private GameObject GetPanelByIndex(int index)
        {
            if (allPanels != null && index >= 0 && index < allPanels.Length && allPanels[index] != null)
            {
                return allPanels[index];
            }

            EnsureAutoBind();
            if (allPanels != null && index >= 0 && index < allPanels.Length && allPanels[index] != null)
            {
                return allPanels[index];
            }

            Transform container = sectionsParentContainer != null ? sectionsParentContainer.transform : transform;
            switch (index)
            {
                case 0: return container.GetComponentInChildren<U5_SA_GM01_ConceptCards_Masters_Phonics>(true)?.gameObject;
                case 1: return container.GetComponentInChildren<U5_SA_GM03s_DoorOrGate_Masters_Phonics>(true)?.gameObject;
                case 2: return container.GetComponentInChildren<U5_SA_GM04_LongOrShort_Masters_Phonics>(true)?.gameObject;
                case 3: return container.GetComponentInChildren<U5_SA_GM03_TheFirstDoor_Masters_Phonics>(true)?.gameObject;
                case 4: return container.GetComponentInChildren<U5_SA_GM07_ClosedWordHunt_Masters_Phonics>(true)?.gameObject;
                case 5: return container.GetComponentInChildren<U5_SA_GM05_BigWordReader_Masters_Phonics>(true)?.gameObject 
                            ?? container.GetComponentInChildren<U5_SA_SevenTypesMap_Masters_Phonics>(true)?.gameObject;
                case 6: return container.GetComponentInChildren<U5_SA_UnitChallenge_Masters_Phonics>(true)?.gameObject;
                default: return null;
            }
        }

        public void CloseAllActivityPanels()
        {
            EnsureAutoBind();
            if (allPanels != null)
            {
                foreach (var p in allPanels)
                {
                    if (p != null) p.SetActive(false);
                }
            }
        }

        // =========================================================================
        // Next Button & Activity Completion
        // =========================================================================

        public void SetNextButtonVisible(bool visible)
        {
            List<Button> nextButtons = new List<Button>();

            // 1. Direct serialized reference
            if (nextActivityButton != null)
            {
                nextButtons.Add(nextActivityButton);
            }

            // 2. Search in current active panel
            GameObject curPanel = GetPanelByIndex(currentActivityIndex);
            if (curPanel != null)
            {
                Button[] panelBtns = curPanel.GetComponentsInChildren<Button>(true);
                foreach (var b in panelBtns)
                {
                    if (b != null)
                    {
                        string bName = b.name.ToLower();
                        if (bName.Contains("nextactivity") || bName.Contains("next_section") || bName.Contains("btn_nextactivity") || bName.Contains("nextsection") || (bName.Contains("next") && !bName.Contains("unit") && !bName.Contains("card") && !bName.Contains("option")))
                        {
                            if (!nextButtons.Contains(b)) nextButtons.Add(b);
                        }
                    }
                }
            }

            // 3. Search in sectionsParentContainer
            if (sectionsParentContainer != null)
            {
                Button[] containerBtns = sectionsParentContainer.GetComponentsInChildren<Button>(true);
                foreach (var b in containerBtns)
                {
                    if (b != null)
                    {
                        string bName = b.name.ToLower();
                        if (bName.Contains("nextactivity") || bName.Contains("next_section") || bName.Contains("btn_nextactivity") || bName.Contains("nextsection") || (bName.Contains("next") && !bName.Contains("unit") && !bName.Contains("card") && !bName.Contains("option")))
                        {
                            if (!nextButtons.Contains(b)) nextButtons.Add(b);
                        }
                    }
                }
            }

            // 4. Search in unit root / TopBar
            Transform p = transform;
            while (p != null)
            {
                foreach (Transform child in p)
                {
                    string cName = child.name.ToLower();
                    if (cName.Contains("nextactivity") || cName.Contains("next_section") || cName.Contains("btn_nextactivity"))
                    {
                        Button b = child.GetComponent<Button>();
                        if (b != null && !nextButtons.Contains(b)) nextButtons.Add(b);
                    }
                }
                if (p.GetComponent<Canvas>() != null) break;
                p = p.parent;
            }

            foreach (var btn in nextButtons)
            {
                if (btn != null)
                {
                    btn.gameObject.SetActive(visible);
                    if (visible)
                    {
                        btn.transform.localScale = Vector3.one;
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(OpenNextSection);
                        StartCoroutine(PulseButton(btn.transform));
                    }
                }
            }
        }

        public void OnActivityComplete()
        {
            SetNextButtonVisible(true);
        }

        public void CompleteCurrentActivity() => OnActivityComplete();

        public void OpenNextSection()
        {
            if (isTransitioning) return;
            isTransitioning = true;

            int fromIndex = currentActivityIndex;
            SetNextButtonVisible(false);

            switch (fromIndex)
            {
                case 0: // Section A: Learn -> Section B: Act 1 (Door or Gate)
                    OpenSection(1);
                    break;
                case 1: // Section B: Act 1 -> Section C: Act 2 (Long or Short)
                    OpenSection(2);
                    break;
                case 2: // Section C: Act 2 -> Section D: Act 3 (The First Door)
                    OpenSection(3);
                    break;
                case 3: // Section D: Act 3 -> Section E: Act 4 (Closed-Word Hunt)
                    OpenSection(4);
                    break;
                case 4: // Section E: Act 4 -> Section F: Act 5 (Big Word Reader)
                    OpenSection(5);
                    break;
                case 5: // Section F: Act 5 -> Section G: Unit Challenge
                    OpenSection(6);
                    break;
                case 6: // Section G: Unit Challenge -> Section H: Complete Panel
                    ShowUnitCompletion();
                    break;
                default:
                    ShowUnitCompletion();
                    break;
            }

            StartCoroutine(UnlockTransitionAfterDelay(0.6f));
        }

        private IEnumerator UnlockTransitionAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            isTransitioning = false;
        }

        // =========================================================================
        // Unit Completion Panel
        // =========================================================================

        public void ShowUnitCompletion()
        {
            currentActivityIndex = 7;
            CloseAllActivityPanels();

            if (completionPanel != null)
            {
                completionPanel.SetActive(true);
                foreach (Transform child in completionPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null) child.gameObject.SetActive(true);
                }

                // 1. Total Score Display
                Transform scoreT = completionPanel.transform.Find("Score_Text") 
                                ?? completionPanel.transform.Find("FinalScoreText")
                                ?? completionPanel.transform.Find("ScoreText")
                                ?? completionPanel.transform.Find("Score");
                if (scoreT != null)
                {
                    var tmp = scoreT.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        tmp.text = $"Total Score: <b><color=#FFD54F>{totalCumulativeScore} / {MAX_POSSIBLE_SCORE}</color></b>";
                        tmp.fontSize = 38;
                        tmp.alignment = TextAlignmentOptions.Center;
                    }
                }

                // 2. Stats / Badge Display
                Transform statsT = completionPanel.transform.Find("FinalStatsText")
                                ?? completionPanel.transform.Find("Stats_Text")
                                ?? completionPanel.transform.Find("RewardSubtitle");
                if (statsT != null)
                {
                    var tmp = statsT.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        tmp.text = $"<b>Doors Opened:</b> {doorsOpenedCount}  |  <b>Doors Closed:</b> {doorsClosedCount}\n<color=#2E7D32><b>Badge Unlocked: Doorkeeper!</b></color>";
                    }
                }

                // 3. Continue / Return Button -> Returns to Unit 5 Section Selection
                if (continueToSignboardButton != null)
                {
                    continueToSignboardButton.gameObject.SetActive(true);
                    continueToSignboardButton.onClick.RemoveAllListeners();
                    continueToSignboardButton.onClick.AddListener(() =>
                    {
                        StartCoroutine(PulseButton(continueToSignboardButton.transform));
                        ShowSectionSelection();
                    });
                }

                // 4. Next / Home Button -> Returns to Global Lessons Menu
                if (nextUnitButton != null)
                {
                    nextUnitButton.gameObject.SetActive(true);
                    nextUnitButton.onClick.RemoveAllListeners();
                    nextUnitButton.onClick.AddListener(() =>
                    {
                        StartCoroutine(PulseButton(nextUnitButton.transform));
                        BackToLessonsMenu();
                    });
                }
            }

            if (U5_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance.PlayCelebration();
                if (unitCompleteVoiceClip != null)
                {
                    U5_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(unitCompleteVoiceClip);
                }
            }
        }

        // =========================================================================
        // Global Back Navigation
        // =========================================================================

        public void OnGlobalBackTapped()
        {
            if (U5_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
            }

            // If inside any activity section (index 0..7 or sections container active) -> return to Section Selection
            if ((sectionsParentContainer != null && sectionsParentContainer.activeSelf) || currentActivityIndex >= 0)
            {
                ShowSectionSelection();
            }
            else
            {
                // If already on Section Selection -> return to global Lessons Unit Selection menu
                BackToLessonsMenu();
            }
        }

        public void BackToSectionSelection() => OnGlobalBackTapped();

        public void BackToLessonsMenu()
        {
            if (U5_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U5_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
            }

            CloseAllActivityPanels();

            if (Unit_Selection_Panel_Masters_Phonics.Instance != null)
            {
                Unit_Selection_Panel_Masters_Phonics.Instance.BackToUnitSelection();
            }
            else
            {
                var globalSelection = GameObject.Find("Unit_Selection_Panel") 
                                   ?? GameObject.Find("Unit_Selection_Panels")
                                   ?? GameObject.Find("Lessons");
                if (globalSelection != null)
                {
                    globalSelection.SetActive(true);
                }
            }

            gameObject.SetActive(false);
        }

        private IEnumerator PulseButton(Transform tr)
        {
            if (tr == null) yield break;
            Vector3 orig = Vector3.one;
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
    }
}

