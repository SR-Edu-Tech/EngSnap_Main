using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U6_SA_UnitFlowManager_Masters_Phonics : MonoBehaviour
    {
        public static U6_SA_UnitFlowManager_Masters_Phonics Instance { get; private set; }

        [Header("1. Main Containers (Auto-Bound)")]
        [SerializeField] private GameObject sectionSelectionPanel; // Unit_6_Section_Selection_Panels
        [SerializeField] private GameObject sectionsParentContainer; // Unit_6_Sections

        [Header("2. Unit 6 Activity Panels (Auto-Bound)")]
        [SerializeField] private GameObject learnPanel;       // 0: Learn Concept Cards (4 Cards)
        [SerializeField] private GameObject activity1Panel;   // 1: Activity 1 (Magic Wand - GM-05)
        [SerializeField] private GameObject activity2Panel;   // 2: Activity 2 (Which Long Vowel? - GM-04)
        [SerializeField] private GameObject activity3Panel;   // 3: Activity 3 (Soft or Hard? - GM-03s)
        [SerializeField] private GameObject activity4Panel;   // 4: Activity 4 (Why The E? - GM-03)
        [SerializeField] private GameObject mapPanel;         // 5: Seven Types Map (3/7 Unlocked)
        [SerializeField] private GameObject challengePanel;   // 6: Unit 6 Final Challenge
        [SerializeField] private GameObject completionPanel;  // 7: U6_COMPLETE_Panel

        [Header("3. Global Header & Navigation (Static Back Button)")]
        [SerializeField] private Button globalBackButton;
        [SerializeField] private Button nextActivityButton;
        [SerializeField] private TextMeshProUGUI cumulativeScoreTMP;
        [SerializeField] private Slider overallProgressBar;

        [Header("4. Completion UI Elements")]
        [SerializeField] private TextMeshProUGUI finalScoreTMP;
        [SerializeField] private TextMeshProUGUI finalStatsTMP;
        [SerializeField] private TextMeshProUGUI badgeTitleTMP;
        [SerializeField] private Button continueToSignboardButton;
        [SerializeField] private Button nextUnitButton;

        [Header("5. Narration & Voice Audio")]
        [Tooltip("U06_VO_unit_intro")]
        public AudioClip unitIntroClip;
        [Tooltip("U06_VO_unit_complete")]
        public AudioClip unitCompleteVoiceClip;

        [Header("Runtime State")]
        [SerializeField] private int currentActivityIndex = -1;
        private GameObject[] allPanels;
        private bool isTransitioning = false;
        private int totalCumulativeScore = 0;
        private const int MAX_POSSIBLE_SCORE = 5800;
        private int wordsTransformedCount = 0;
        private int silenteExplainedCount = 0;

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
                Transform s = transform.Find("Unit_6_Section_Selection_Panels") 
                           ?? transform.Find("Unit_6_Section_Selection_Panel")
                           ?? transform.Find("Unit_5_Section_Selection_Panels")
                           ?? transform.Find("Unit_4_Section_Selection_Panels")
                           ?? transform.Find("Unit_3_Section_Selection_Panels")
                           ?? transform.Find("Section_Selection_Panels") 
                           ?? transform.Find("Section_Selection_Panel") 
                           ?? transform.Find("Unit6_Section_Selection_Panels")
                           ?? transform.Find("Signboard")
                           ?? transform.Find("Sign_Board")
                           ?? transform.Find("Level_Selection_Panel");

                if (s == null)
                {
                    foreach (Transform child in transform)
                    {
                        if (child == null) continue;
                        string cName = child.name.ToLower();
                        if ((cName.Contains("section") || cName.Contains("signboard") || cName.Contains("level")) &&
                            (cName.Contains("selection") || cName.Contains("signboard") || cName.Contains("panels")))
                        {
                            s = child;
                            break;
                        }
                    }
                }

                if (s != null) sectionSelectionPanel = s.gameObject;
            }

            if (sectionsParentContainer == null)
            {
                Transform s = transform.Find("Unit_6_Sections") 
                           ?? transform.Find("Sections") 
                           ?? transform.Find("Unit6_Sections")
                           ?? transform.Find("Sections_Container");
                if (s != null) sectionsParentContainer = s.gameObject;
            }

            Transform container = sectionsParentContainer != null ? sectionsParentContainer.transform : transform;

            // Ensure BG_Image inside sections container is active and in the back
            EnsureBGImage();

            if (learnPanel == null)
            {
                Transform t = container.Find("U6_learn_Concept_Cards_Panel") ?? container.Find("U6_Learn_Concept_Cards_Panel") ?? container.Find("Learn_Panel") ?? container.Find("U6_Learn") ?? container.Find("Concept_Cards");
                if (t != null) learnPanel = t.gameObject;
                else learnPanel = container.GetComponentInChildren<U6_SA_GM01_ConceptCards_Masters_Phonics>(true)?.gameObject
                               ?? transform.GetComponentInChildren<U6_SA_GM01_ConceptCards_Masters_Phonics>(true)?.gameObject;
            }

            if (activity1Panel == null)
            {
                Transform t = container.Find("U6_Activity_1_Magic_Wand") ?? container.Find("U6_Activity_1") ?? container.Find("Activity_1") ?? container.Find("Magic_Wand");
                if (t != null) activity1Panel = t.gameObject;
                else activity1Panel = container.GetComponentInChildren<U6_SA_GM05_MagicWand_Masters_Phonics>(true)?.gameObject
                                   ?? transform.GetComponentInChildren<U6_SA_GM05_MagicWand_Masters_Phonics>(true)?.gameObject;
            }

            if (activity2Panel == null)
            {
                Transform t = container.Find("U6_Activity_2_Which_Long_Vowel") ?? container.Find("U6_Activity_2") ?? container.Find("Activity_2") ?? container.Find("Which_Long_Vowel");
                if (t != null) activity2Panel = t.gameObject;
                else activity2Panel = container.GetComponentInChildren<U6_SA_GM04_WhichLongVowel_Masters_Phonics>(true)?.gameObject
                                   ?? transform.GetComponentInChildren<U6_SA_GM04_WhichLongVowel_Masters_Phonics>(true)?.gameObject;
            }

            if (activity3Panel == null)
            {
                Transform t = container.Find("U6_Activity_3_Soft_Or_Hard") ?? container.Find("U6_Activity_3") ?? container.Find("Activity_3") ?? container.Find("Soft_Or_Hard");
                if (t != null) activity3Panel = t.gameObject;
                else activity3Panel = container.GetComponentInChildren<U6_SA_GM03s_SoftOrHard_Masters_Phonics>(true)?.gameObject
                                   ?? transform.GetComponentInChildren<U6_SA_GM03s_SoftOrHard_Masters_Phonics>(true)?.gameObject;
            }

            if (activity4Panel == null)
            {
                Transform t = container.Find("U6_Activity_4_Why_The_E") ?? container.Find("U6_Activity_4") ?? container.Find("Activity_4") ?? container.Find("Why_The_E") ?? container.Find("WhyTheE");
                if (t != null) activity4Panel = t.gameObject;
                else activity4Panel = container.GetComponentInChildren<U6_SA_GM03_WhyTheE_Masters_Phonics>(true)?.gameObject
                                   ?? transform.GetComponentInChildren<U6_SA_GM03_WhyTheE_Masters_Phonics>(true)?.gameObject;
            }

            if (mapPanel == null)
            {
                Transform t = container.Find("U6_SevenTypesMap_Panel") 
                           ?? container.Find("U6_Seven_Types_Map") 
                           ?? container.Find("SevenTypesMap") 
                           ?? container.Find("Map_Panel")
                           ?? container.Find("U6_Map_Panel")
                           ?? transform.Find("U6_SevenTypesMap_Panel")
                           ?? transform.Find("U6_Seven_Types_Map");
                if (t != null) mapPanel = t.gameObject;
                else
                {
                    mapPanel = container.GetComponentInChildren<U6_SA_SevenTypesMap_Masters_Phonics>(true)?.gameObject
                            ?? transform.GetComponentInChildren<U6_SA_SevenTypesMap_Masters_Phonics>(true)?.gameObject;
                }
            }

            if (challengePanel == null)
            {
                Transform t = container.Find("U6_UnitChallenge_Panel") ?? container.Find("U6_Unit_Challenge") ?? container.Find("Challenge_Panel") ?? container.Find("Unit_Challenge") ?? container.Find("Challenge");
                if (t != null) challengePanel = t.gameObject;
                else challengePanel = container.GetComponentInChildren<U6_SA_UnitChallenge_Masters_Phonics>(true)?.gameObject
                                   ?? transform.GetComponentInChildren<U6_SA_UnitChallenge_Masters_Phonics>(true)?.gameObject;
            }

            if (completionPanel == null)
            {
                Transform t = container.Find("U6_COMPLETE_Panel") ?? container.Find("U6_Complete_Panel") ?? container.Find("Complete_Panel") ?? container.Find("Completion_Panel") ?? container.Find("U6_COMPLETE");
                if (t != null) completionPanel = t.gameObject;
            }

            allPanels = new GameObject[]
            {
                learnPanel,
                activity1Panel,
                activity2Panel,
                activity3Panel,
                activity4Panel,
                mapPanel,
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

            // Wire all Back buttons across root, signboard, and sections container
            WireAllBackButtons();

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
                        OpenSevenTypesMap(); // Transitions Report + Badge -> Map Update (3 of 7 filled)
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

            // 4. Style all progress bars across Unit 6 with vibrant game blue & navy contrast
            StyleAllProgressBars();
        }

        public void StyleAllProgressBars()
        {
            if (overallProgressBar != null) StyleProgressBarBlue(overallProgressBar);

            Slider[] allSliders = GetComponentsInChildren<Slider>(true);
            foreach (var s in allSliders)
            {
                if (s != null) StyleProgressBarBlue(s);
            }
        }

        public static void StyleProgressBarBlue(Slider slider)
        {
            if (slider == null) return;

            // Vibrant game blue for filled portion
            Color vibrantBlue = new Color(0.08f, 0.62f, 0.98f, 1.0f); // #149EFA Electric Cyan-Blue
            // High-contrast deep navy slate background
            Color darkNavyBg = new Color(0.08f, 0.12f, 0.22f, 0.75f);

            // 1. Style fillRect
            if (slider.fillRect != null)
            {
                Image fillImg = slider.fillRect.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.color = vibrantBlue;
                }
            }

            // 2. Search children
            Transform fillTr = slider.transform.Find("Fill Area/Fill") ?? slider.transform.Find("Fill");
            if (fillTr != null)
            {
                Image fillImg = fillTr.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.color = vibrantBlue;
                }
            }

            Transform bgTr = slider.transform.Find("Background") ?? slider.transform.Find("BG");
            if (bgTr != null)
            {
                Image bgImg = bgTr.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.color = darkNavyBg;
                }
            }

            Image[] allImgs = slider.GetComponentsInChildren<Image>(true);
            foreach (var img in allImgs)
            {
                if (img == null) continue;
                string n = img.gameObject.name.ToLower();
                if (n.Contains("fill"))
                {
                    img.color = vibrantBlue;
                }
                else if (n.Contains("background") || n.Contains("bg"))
                {
                    img.color = darkNavyBg;
                }
            }
        }

        public void EnsureBGImage()
        {
            if (sectionsParentContainer != null)
            {
                Transform bgTr = sectionsParentContainer.transform.Find("BG_Image")
                              ?? sectionsParentContainer.transform.Find("Background")
                              ?? sectionsParentContainer.transform.Find("BG");
                if (bgTr != null)
                {
                    bgTr.gameObject.SetActive(true);
                    bgTr.SetAsFirstSibling();
                }
            }
        }

        private void WireAllBackButtons()
        {
            List<Button> backBtns = new List<Button>();

            if (globalBackButton != null && !backBtns.Contains(globalBackButton))
                backBtns.Add(globalBackButton);

            Button[] rootBtns = GetComponentsInChildren<Button>(true);
            foreach (var b in rootBtns)
            {
                if (b == null) continue;
                string bName = b.name.ToLower();

                // Skip Prev button on Concept Cards pagination
                if (bName.Contains("prev") || bName.Contains("previous")) continue;

                // Skip buttons on completionPanel and mapPanel (they have dedicated handlers)
                if (completionPanel != null && b.transform.IsChildOf(completionPanel.transform)) continue;
                if (mapPanel != null && b.transform.IsChildOf(mapPanel.transform)) continue;

                if (bName.Contains("back") || bName == "btn_back" || bName.Contains("return"))
                {
                    if (!backBtns.Contains(b)) backBtns.Add(b);
                }
            }

            foreach (var b in backBtns)
            {
                if (b != null)
                {
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(OnGlobalBackTapped);
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
                    b.onClick.RemoveListener(OnGlobalBackTapped);
                    b.onClick.AddListener(OnGlobalBackTapped);
                }
                else
                {
                    secButtons.Add(b);
                }
            }

            // 4-Truck Signboard Setup (Standard Unit 6 Signboard)
            // Truck 1: Section 1 (Learn Concept Cards -> launches Activity 1 Magic Wand)
            // Truck 2: Section 2 (Activity 2: Which Long Vowel?)
            // Truck 3: Section 3 (Activity 3: Soft or Hard?)
            // Truck 4: Section 4 (Activity 4: Why The E? -> launches Unit Challenge)
            if (secButtons.Count == 4)
            {
                int[] fourSectionIndices = new int[] { 0, 2, 3, 4 };
                for (int i = 0; i < secButtons.Count; i++)
                {
                    Button b = secButtons[i];
                    int targetIdx = fourSectionIndices[i];
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => OpenSection(targetIdx));
                }
            }
            // 5-Truck Signboard Setup
            // Truck 1: Section 1 (Learn -> Act 1)
            // Truck 2: Section 2 (Act 2: Which Long Vowel)
            // Truck 3: Section 3 (Act 3: Soft or Hard)
            // Truck 4: Section 4 (Act 4: Why The E)
            // Truck 5: Section 5 (Unit Challenge -> Badge -> Map Update)
            else if (secButtons.Count == 5)
            {
                int[] fiveSectionIndices = new int[] { 0, 2, 3, 4, 6 };

                for (int i = 0; i < secButtons.Count; i++)
                {
                    Button b = secButtons[i];
                    string bName = b.name.ToLower();

                    if (bName.Contains("learn") || bName.Contains("concept") || bName.Contains("wand") || bName.Contains("section_1") || bName.Contains("section1"))
                    {
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(OpenSection1);
                    }
                    else if (bName.Contains("act2") || bName.Contains("vowel") || bName.Contains("section_2") || bName.Contains("section2"))
                    {
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(OpenSection2);
                    }
                    else if (bName.Contains("act3") || bName.Contains("soft") || bName.Contains("hard") || bName.Contains("section_3") || bName.Contains("section3"))
                    {
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(OpenSection3);
                    }
                    else if (bName.Contains("act4") || bName.Contains("why") || bName.Contains("section_4") || bName.Contains("section4"))
                    {
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(OpenSection4);
                    }
                    else if (bName.Contains("act5") || bName.Contains("map") || bName.Contains("challenge") || bName.Contains("section_5") || bName.Contains("section5"))
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
                for (int i = 0; i < secButtons.Count; i++)
                {
                    Button b = secButtons[i];
                    string bName = b.name.ToLower();

                    if (bName.Contains("learn") || bName.Contains("concept"))
                    {
                        b.onClick.RemoveListener(OpenSectionA);
                        b.onClick.AddListener(OpenSectionA);
                    }
                    else if (bName.Contains("act1") || bName.Contains("wand"))
                    {
                        b.onClick.RemoveListener(OpenSectionB);
                        b.onClick.AddListener(OpenSectionB);
                    }
                    else if (bName.Contains("act2") || bName.Contains("vowel"))
                    {
                        b.onClick.RemoveListener(OpenSectionC);
                        b.onClick.AddListener(OpenSectionC);
                    }
                    else if (bName.Contains("act3") || bName.Contains("soft") || bName.Contains("hard"))
                    {
                        b.onClick.RemoveListener(OpenSectionD);
                        b.onClick.AddListener(OpenSectionD);
                    }
                    else if (bName.Contains("act4") || bName.Contains("why"))
                    {
                        b.onClick.RemoveListener(OpenSectionE);
                        b.onClick.AddListener(OpenSectionE);
                    }
                    else if (bName.Contains("map") || bName.Contains("types"))
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
                        if (capturedIndex < 8)
                        {
                            b.onClick.RemoveAllListeners();
                            b.onClick.AddListener(() => OpenSection(capturedIndex));
                        }
                    }
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

        public void RecordWordTransformed() => wordsTransformedCount++;
        public void RecordSilentEExplained() => silenteExplainedCount++;

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

            if (U6_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U6_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
            }

            if (!gameObject.activeSelf) gameObject.SetActive(true);

            // 1. Close all activity panels
            CloseAllActivityPanels();

            // 2. Keep sectionsParentContainer active so the background image stays visible
            if (sectionsParentContainer != null)
            {
                sectionsParentContainer.SetActive(true);
                EnsureBGImage();
            }

            // 3. Show and activate the signboard section selection panel
            if (sectionSelectionPanel != null)
            {
                sectionSelectionPanel.SetActive(true);
                foreach (Transform child in sectionSelectionPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null) child.gameObject.SetActive(true);
                }
                sectionSelectionPanel.transform.SetAsLastSibling();
            }
            else
            {
                foreach (Transform child in transform)
                {
                    if (child != null)
                    {
                        string cName = child.name.ToLower();
                        if (cName.Contains("section_selection") || cName.Contains("signboard"))
                        {
                            child.gameObject.SetActive(true);
                            child.SetAsLastSibling();
                        }
                    }
                }
            }

            // 4. Wire up all back buttons and signboard truck buttons
            WireSectionSelectionButtons();
            WireAllBackButtons();

            if (globalBackButton != null)
            {
                globalBackButton.gameObject.SetActive(true);
                globalBackButton.transform.SetAsLastSibling();
            }

            SetNextButtonVisible(false);

            if (U6_SA_AudioManager_Masters_Phonics.Instance != null && unitIntroClip != null)
            {
                U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(unitIntroClip);
            }
        }

        public void OpenSectionSelection() => ShowSectionSelection();

        public void OpenSection(int sectionIndex)
        {
            EnsureAutoBind();
            currentActivityIndex = sectionIndex;

            // Deactivate all section selection / signboard panels under Unit_6
            if (sectionSelectionPanel != null)
            {
                sectionSelectionPanel.SetActive(false);
            }
            foreach (Transform child in transform)
            {
                if (child != null && (child.name.ToLower().Contains("section_selection") || child.name.ToLower().Contains("signboard")))
                {
                    child.gameObject.SetActive(false);
                }
            }

            // Activate sections container
            if (sectionsParentContainer != null)
            {
                sectionsParentContainer.SetActive(true);
            }

            // Ensure BG_Image inside sections container stays active at all times
            EnsureBGImage();

            CloseAllActivityPanels();

            GameObject targetPanel = GetPanelByIndex(sectionIndex);

            // Dynamic fallback for Map Panel if not present in scene
            if (targetPanel == null && sectionIndex == 5)
            {
                Transform container = sectionsParentContainer != null ? sectionsParentContainer.transform : transform;
                GameObject newMapObj = new GameObject("U6_SevenTypesMap_Panel", typeof(RectTransform));
                newMapObj.transform.SetParent(container, false);
                RectTransform rt = newMapObj.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                newMapObj.AddComponent<U6_SA_SevenTypesMap_Masters_Phonics>();
                mapPanel = newMapObj;
                targetPanel = newMapObj;
            }

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

            WireAllBackButtons();

            if (globalBackButton != null)
            {
                globalBackButton.gameObject.SetActive(true);
            }

            SetNextButtonVisible(false);
        }

        // Section Shortcuts
        public void OpenSectionA() => OpenSection(0); // Learn: Concept Cards
        public void OpenSectionB() => OpenSection(1); // Activity 1: Magic Wand
        public void OpenSectionC() => OpenSection(2); // Activity 2: Which Long Vowel?
        public void OpenSectionD() => OpenSection(3); // Activity 3: Soft or Hard?
        public void OpenSectionE() => OpenSection(4); // Activity 4: Why The E?
        public void OpenSectionF() => OpenSection(5); // Seven Types Map
        public void OpenSectionG() => OpenSection(6); // Unit Challenge
        public void OpenSectionH() => OpenSection(7); // Completion Panel

        // 5-Section Signboard Mapping
        public void OpenSection1() => OpenSection(0); // Section 1: Learn -> launches Act 1
        public void OpenSection2() => OpenSection(2); // Section 2: Act 2 (Which Long Vowel)
        public void OpenSection3() => OpenSection(3); // Section 3: Act 3 (Soft or Hard)
        public void OpenSection4() => OpenSection(4); // Section 4: Act 4 (Why The E)
        public void OpenSection5() => OpenSection(6); // Section 5: Unit Challenge -> Badge -> Map Update

        public void OpenLearn() => OpenSection(0);
        public void OpenActivity1() => OpenSection(1);
        public void OpenActivity2() => OpenSection(2);
        public void OpenActivity3() => OpenSection(3);
        public void OpenActivity4() => OpenSection(4);
        public void OpenSevenTypesMap() => OpenSection(5);
        public void OpenUnitChallenge() => OpenSection(6);
        public void OpenCompletionPanel() => OpenSection(7);

        public void OpenActivity(Unit6ActivityState targetState)
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
                case 0: return container.GetComponentInChildren<U6_SA_GM01_ConceptCards_Masters_Phonics>(true)?.gameObject;
                case 1: return container.GetComponentInChildren<U6_SA_GM05_MagicWand_Masters_Phonics>(true)?.gameObject;
                case 2: return container.GetComponentInChildren<U6_SA_GM04_WhichLongVowel_Masters_Phonics>(true)?.gameObject;
                case 3: return container.GetComponentInChildren<U6_SA_GM03s_SoftOrHard_Masters_Phonics>(true)?.gameObject;
                case 4: return container.GetComponentInChildren<U6_SA_GM03_WhyTheE_Masters_Phonics>(true)?.gameObject;
                case 5: return mapPanel != null ? mapPanel : container.GetComponentInChildren<U6_SA_SevenTypesMap_Masters_Phonics>(true)?.gameObject;
                case 6: return container.GetComponentInChildren<U6_SA_UnitChallenge_Masters_Phonics>(true)?.gameObject;
                case 7: return completionPanel != null ? completionPanel : container.Find("U6_COMPLETE_Panel")?.gameObject;
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

            if (learnPanel != null) learnPanel.SetActive(false);
            if (activity1Panel != null) activity1Panel.SetActive(false);
            if (activity2Panel != null) activity2Panel.SetActive(false);
            if (activity3Panel != null) activity3Panel.SetActive(false);
            if (activity4Panel != null) activity4Panel.SetActive(false);
            if (mapPanel != null) mapPanel.SetActive(false);
            if (challengePanel != null) challengePanel.SetActive(false);
            if (completionPanel != null) completionPanel.SetActive(false);

            Transform container = sectionsParentContainer != null ? sectionsParentContainer.transform : transform;
            foreach (Transform child in container)
            {
                if (child != null && child.name != "BG_Image" && child.name != "Background" && !child.name.ToLower().Contains("section_selection") && !child.name.ToLower().Contains("signboard"))
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        // =========================================================================
        // Next Button & Activity Completion
        // =========================================================================

        public void SetNextButtonVisible(bool visible)
        {
            List<Button> nextButtons = new List<Button>();

            if (nextActivityButton != null)
            {
                nextButtons.Add(nextActivityButton);
            }

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
                case 0: // Section A: Learn -> Section B: Act 1 (Magic Wand)
                    OpenSection(1);
                    break;
                case 1: // Section B: Act 1 -> Section C: Act 2 (Which Long Vowel)
                    OpenSection(2);
                    break;
                case 2: // Section C: Act 2 -> Section D: Act 3 (Soft or Hard)
                    OpenSection(3);
                    break;
                case 3: // Section D: Act 3 -> Section E: Act 4 (Why The E)
                    OpenSection(4);
                    break;
                case 4: // Section E: Act 4 -> Section G: Unit Challenge
                    OpenSection(6);
                    break;
                case 6: // Section G: Unit Challenge -> Section H: Report + Badge (Completion Panel)
                    ShowUnitCompletion();
                    break;
                case 7: // Section H: Report + Badge -> Section F: Map Update (Seven Types Map)
                    OpenSevenTypesMap();
                    break;
                case 5: // Section F: Map Update -> Return to Unit Hub / Selection
                    ShowSectionSelection();
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

                if (finalScoreTMP == null)
                {
                    Transform scoreT = completionPanel.transform.Find("Score_Text") 
                                    ?? completionPanel.transform.Find("FinalScoreText")
                                    ?? completionPanel.transform.Find("ScoreText")
                                    ?? completionPanel.transform.Find("Score");
                    if (scoreT != null) finalScoreTMP = scoreT.GetComponent<TextMeshProUGUI>();
                }
                if (finalScoreTMP != null)
                {
                    finalScoreTMP.text = $"<b>Total Unit Score: <color=#FFE57F>{totalCumulativeScore}</color> / {MAX_POSSIBLE_SCORE}</b>";
                }

                if (finalStatsTMP == null)
                {
                    Transform statsT = completionPanel.transform.Find("Stats_Text") 
                                    ?? completionPanel.transform.Find("FinalStatsText")
                                    ?? completionPanel.transform.Find("StatsText")
                                    ?? completionPanel.transform.Find("Subtitle");
                    if (statsT != null) finalStatsTMP = statsT.GetComponent<TextMeshProUGUI>();
                }
                if (finalStatsTMP != null)
                {
                    finalStatsTMP.text = "<b>Words Transformed: 12  -  Silent e's Explained: 18\nSyllable Types Mastered: 3 of 7</b>";
                }

                if (badgeTitleTMP == null)
                {
                    Transform bT = completionPanel.transform.Find("Badge_Title") 
                                ?? completionPanel.transform.Find("BadgeText")
                                ?? completionPanel.transform.Find("Badge_Text");
                    if (bT != null) badgeTitleTMP = bT.GetComponent<TextMeshProUGUI>();
                }
                if (badgeTitleTMP != null)
                {
                    badgeTitleTMP.text = "<b><color=#FFD700>BADGE UNLOCKED: WAND BEARER</color></b>";
                }

                // Explicitly wire all buttons on completion panel
                Button[] cBtns = completionPanel.GetComponentsInChildren<Button>(true);
                foreach (var b in cBtns)
                {
                    if (b == null) continue;
                    Button currentBtn = b;
                    string bName = currentBtn.name.ToLower();
                    var tmp = currentBtn.GetComponentInChildren<TextMeshProUGUI>(true);
                    string bText = tmp != null ? tmp.text.ToLower() : "";

                    currentBtn.onClick.RemoveAllListeners();

                    if (bName.Contains("nextunit") || bName.Contains("next_unit") || bText.Contains("next unit") || bName.Contains("home"))
                    {
                        currentBtn.onClick.AddListener(() =>
                        {
                            StartCoroutine(PulseButton(currentBtn.transform));
                            BackToLessonsMenu();
                        });
                    }
                    else
                    {
                        // Direct link to the 7 Syllable Types Map!
                        currentBtn.onClick.AddListener(() =>
                        {
                            StartCoroutine(PulseButton(currentBtn.transform));
                            OpenSevenTypesMap();
                        });
                        if (tmp != null && !bText.Contains("map"))
                        {
                            tmp.text = "<b>VIEW SYLLABLE MAP</b>";
                        }
                    }
                }
            }

            if (U6_SA_AudioManager_Masters_Phonics.Instance != null && unitCompleteVoiceClip != null)
            {
                U6_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(unitCompleteVoiceClip);
            }
            else
            {
                U6_SA_AudioManager_Masters_Phonics.Instance?.PlayVoicePrompt("U06_VO_unit_complete", "Unit Six, done. Twelve words transformed, eighteen silent e's explained. Badge unlocked - Wand Bearer.");
            }
        }

        // =========================================================================
        // Navigation Handlers
        // =========================================================================

        public void OnGlobalBackTapped()
        {
            if (currentActivityIndex >= 0)
            {
                ShowSectionSelection();
            }
            else
            {
                BackToLessonsMenu();
            }
        }

        public void BackToLessonsMenu()
        {
            if (U6_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U6_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
            }

            CloseAllActivityPanels();

            // 1. Disable this Unit GameObject completely
            gameObject.SetActive(false);

            // 2. Activate and show the global Unit Selection / Lessons menu
            if (Unit_Selection_Panel_Masters_Phonics.Instance != null)
            {
                Unit_Selection_Panel_Masters_Phonics.Instance.gameObject.SetActive(true);
                Unit_Selection_Panel_Masters_Phonics.Instance.BackToUnitSelection();
            }
            else
            {
                var unitSel = FindFirstObjectByType<Unit_Selection_Panel_Masters_Phonics>(FindObjectsInactive.Include);
                if (unitSel != null)
                {
                    unitSel.gameObject.SetActive(true);
                    unitSel.BackToUnitSelection();
                    return;
                }

                Canvas canvas = GetComponentInParent<Canvas>(true);
                if (canvas != null)
                {
                    Transform uSel = canvas.transform.Find("Unit_Selection_Panel_Masters_Phonics")
                                  ?? canvas.transform.Find("Unit_Selection_Panel")
                                  ?? canvas.transform.Find("Unit_Selection")
                                  ?? canvas.transform.Find("Lessons_Panel")
                                  ?? canvas.transform.Find("Lessons");
                    if (uSel != null)
                    {
                        uSel.gameObject.SetActive(true);
                    }
                }
            }
        }

        private IEnumerator PulseButton(Transform btnTransform)
        {
            if (btnTransform == null) yield break;
            Vector3 orig = Vector3.one;
            float elapsed = 0f;
            float dur = 0.25f;

            while (elapsed < dur)
            {
                if (btnTransform == null) yield break;
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                btnTransform.localScale = Vector3.Lerp(orig * 1.15f, orig, t);
                yield return null;
            }
            if (btnTransform != null) btnTransform.localScale = orig;
        }
    }
}
