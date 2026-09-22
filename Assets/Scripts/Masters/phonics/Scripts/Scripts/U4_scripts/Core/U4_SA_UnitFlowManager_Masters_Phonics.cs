using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U4_SA_UnitFlowManager_Masters_Phonics : MonoBehaviour
    {
        public static U4_SA_UnitFlowManager_Masters_Phonics Instance { get; private set; }

        [Header("Unit 4 Parent Containers")]
        [SerializeField] private GameObject sectionSelectionPanel; // Unit_4_Section_Selection_Panels
        [SerializeField] private GameObject sectionsParentContainer; // Unit_4_Sections

        [Header("Unit 4 Activity Panels (Auto-Bound)")]
        [SerializeField] private GameObject learnPanel;       // 0: Learn Concept Cards (5 Cards)
        [SerializeField] private GameObject activity1Panel;   // 1: Seven Doors (GM-03)
        [SerializeField] private GameObject activity2Panel;   // 2: Strong Beat (GM-04)
        [SerializeField] private GameObject activity3Panel;   // 3: Schwa Hunt (GM-06)
        [SerializeField] private GameObject mapPanel;         // 4: Seven Types Map
        [SerializeField] private GameObject challengePanel;   // 5: Unit Challenge
        [SerializeField] private GameObject completionPanel;  // 6: U4_COMPLETE_Panel

        [Header("Unit 4 Navigation & Back Button (Static)")]
        [SerializeField] private Button globalBackButton;

        [Header("Unit 4 Completion Narration (Official Doc)")]
        [Tooltip("U04_VO_unit_complete: 'Unit Four, done. Eighteen schwas found. Badge unlocked — Sound Detective.' (7s)")]
        public AudioClip unitCompleteVoiceClip;

        [Header("Runtime State")]
        [SerializeField] private int currentActivityIndex = -1;
        private GameObject[] allPanels;
        private bool isTransitioning = false;
        private int totalCumulativeScore = 0;

        public int CurrentActivityIndex => currentActivityIndex;
        public int TotalCumulativeScore => totalCumulativeScore;

        public void AddScore(int amount)
        {
            totalCumulativeScore += amount;
        }

        public void ResetTotalScore()
        {
            totalCumulativeScore = 0;
        }

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
        }

        private void OnEnable()
        {
            EnsureAutoBind();
        }

        public void EnsureAutoBind()
        {
            if (sectionSelectionPanel == null)
            {
                Transform s = transform.Find("Unit_4_Section_Selection_Panels") 
                           ?? transform.Find("Section_Selection_Panels") 
                           ?? transform.Find("Unit4_Section_Selection_Panels")
                           ?? transform.Find("Signboard");
                if (s != null) sectionSelectionPanel = s.gameObject;
            }

            if (sectionsParentContainer == null)
            {
                Transform s = transform.Find("Unit_4_Sections") 
                           ?? transform.Find("Sections") 
                           ?? transform.Find("Unit4_Sections")
                           ?? transform.Find("Sections_Container");
                if (s != null) sectionsParentContainer = s.gameObject;
            }

            Transform container = sectionsParentContainer != null ? sectionsParentContainer.transform : transform;

            if (learnPanel == null)
            {
                Transform t = container.Find("U4_learn_Concept_Cards_Panel") ?? container.Find("U4_Learn_Concept_Cards_Panel") ?? container.Find("Learn_Panel");
                if (t != null) learnPanel = t.gameObject;
            }

            if (activity1Panel == null)
            {
                Transform t = container.Find("U4_Activity_1_Seven_Doors") ?? container.Find("Activity_1") ?? container.Find("Seven_Doors");
                if (t != null) activity1Panel = t.gameObject;
            }

            if (activity2Panel == null)
            {
                Transform t = container.Find("U4_Activity_2_Strong_Beat") ?? container.Find("Activity_2") ?? container.Find("Strong_Beat");
                if (t != null) activity2Panel = t.gameObject;
            }

            if (activity3Panel == null)
            {
                Transform t = container.Find("U4_Activity_3_Schwa_Hunt") ?? container.Find("Activity_3") ?? container.Find("Schwa_Hunt");
                if (t != null) activity3Panel = t.gameObject;
            }

            if (mapPanel == null)
            {
                Transform t = container.Find("U4_Seven_Types_Map") ?? container.Find("Map_Panel") ?? container.Find("Seven_Types_Map");
                if (t != null) mapPanel = t.gameObject;
            }

            if (challengePanel == null)
            {
                Transform t = container.Find("U4_Unit_Challenge") ?? container.Find("Challenge_Panel") ?? container.Find("Unit_Challenge");
                if (t != null) challengePanel = t.gameObject;
            }

            if (completionPanel == null)
            {
                Transform t = container.Find("U4_COMPLETE_Panel") ?? container.Find("Complete_Panel") ?? container.Find("Completion_Panel");
                if (t != null) completionPanel = t.gameObject;
            }

            allPanels = new GameObject[]
            {
                learnPanel,
                activity1Panel,
                activity2Panel,
                activity3Panel,
                mapPanel,
                challengePanel,
                completionPanel
            };

            // Auto-bind persistent Back Button
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

            WireChildBackButtons(sectionSelectionPanel);
            WireChildBackButtons(sectionsParentContainer);
        }

        private void WireChildBackButtons(GameObject root)
        {
            if (root == null) return;
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                if (b != null && (b.name.ToLower().Contains("back") || b.name.ToLower().Contains("return")))
                {
                    b.onClick.RemoveListener(OnGlobalBackTapped);
                    b.onClick.AddListener(OnGlobalBackTapped);
                }
            }
        }

        public void OpenSectionSelection()
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

            if (globalBackButton != null)
            {
                globalBackButton.gameObject.SetActive(true);
            }

            SetNextButtonVisible(false);
        }

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
                Transform secBack = sectionsParentContainer.transform.Find("Back_Button") 
                                 ?? sectionsParentContainer.transform.Find("BackButton")
                                 ?? sectionsParentContainer.transform.Find("Section_Selection_Back");
                if (secBack != null) secBack.gameObject.SetActive(true);
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
                        if (cName.Contains("nextactivity") || cName.Contains("next_section") || (sectionIndex > 0 && (cName.Contains("next") || cName.Contains("continue"))))
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

            if (globalBackButton != null)
            {
                globalBackButton.gameObject.SetActive(true);
            }

            if (sectionIndex > 0)
            {
                SetNextButtonVisible(false);
            }
        }

        // Inspector Button Shortcuts for Unit_4_Section_Selection_Panels
        public void OpenSectionA() => OpenSection(0); // Learn (5 Concept Cards)
        public void OpenSectionB() => OpenSection(1); // Activity 1 (Seven Doors)
        public void OpenSectionC() => OpenSection(2); // Activity 2 (Strong Beat)
        public void OpenSectionD() => OpenSection(3); // Activity 3 (Schwa Hunt)
        public void OpenSectionE() => OpenSection(4); // Activity 4 (Seven Types Map)
        public void OpenSectionF() => OpenSection(5); // Activity 5 (Unit Challenge)

        private GameObject GetPanelByIndex(int index)
        {
            if (allPanels != null && index >= 0 && index < allPanels.Length)
            {
                return allPanels[index];
            }
            return null;
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

        public void OnGlobalBackTapped()
        {
            if (U4_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
            }

            // If inside any activity section (index 0..6 or sections container active) -> return to Section Selection
            if ((sectionsParentContainer != null && sectionsParentContainer.activeSelf) || currentActivityIndex >= 0)
            {
                OpenSectionSelection();
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
            if (U4_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
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
                                   ?? GameObject.Find("Section_Selection_Panels")
                                   ?? GameObject.Find("Lessons");
                if (globalSelection != null)
                {
                    globalSelection.SetActive(true);
                }
            }

            gameObject.SetActive(false);
        }

        public void SetNextButtonVisible(bool visible)
        {
            List<Button> nextButtons = new List<Button>();

            // 1. Search in current panel
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

            // 2. Search in sectionsParentContainer
            if (sectionsParentContainer != null)
            {
                Button[] containerBtns = sectionsParentContainer.GetComponentsInChildren<Button>(true);
                foreach (var b in containerBtns)
                {
                    if (b != null && (b.name.ToLower().Contains("next") || b.name.ToLower().Contains("continue")))
                    {
                        if (!nextButtons.Contains(b)) nextButtons.Add(b);
                    }
                }
            }

            // 3. Search in root unit transform or parent canvas
            Transform p = transform;
            while (p != null)
            {
                foreach (Transform child in p)
                {
                    if (child.name.ToLower().Contains("next") || child.name.ToLower().Contains("continue"))
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

        public void OnActivityComplete()
        {
            SetNextButtonVisible(true);
        }

        public void OpenNextSection()
        {
            if (isTransitioning) return;
            isTransitioning = true;

            int fromIndex = currentActivityIndex;
            SetNextButtonVisible(false);

            switch (fromIndex)
            {
                case 0: // Section A: Learn -> Section B: Act 1 (Seven Doors)
                    OpenSection(1);
                    break;
                case 1: // Section B: Act 1 -> Section C: Act 2 (Strong Beat)
                    OpenSection(2);
                    break;
                case 2: // Section C: Act 2 -> Section D: Act 3 (Schwa Hunt)
                    OpenSection(3);
                    break;
                case 3: // Section D: Act 3 -> Section E: Map
                    OpenSection(4);
                    break;
                case 4: // Section E: Map -> Section F: Challenge
                    OpenSection(5);
                    break;
                case 5: // Section F: Challenge -> Complete Panel
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

        public void ShowUnitCompletion()
        {
            currentActivityIndex = 6;
            CloseAllActivityPanels();

            if (completionPanel != null)
            {
                completionPanel.SetActive(true);
                foreach (Transform child in completionPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null) child.gameObject.SetActive(true);
                }

                // 1. Display total cumulative score across all activities in Unit 4
                Transform scoreT = completionPanel.transform.Find("Score_Text") 
                                ?? completionPanel.transform.Find("ScoreText")
                                ?? completionPanel.transform.Find("FinalScoreText")
                                ?? completionPanel.transform.Find("Score");
                if (scoreT != null)
                {
                    var tmp = scoreT.GetComponent<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        tmp.text = $"Total Score: <b><color=#FFD54F>{totalCumulativeScore}</color></b>";
                        tmp.fontSize = 38;
                        tmp.alignment = TextAlignmentOptions.Center;
                    }
                }

                // 2. Continue Button -> Returns to Unit 4 Section Selection Panel
                Transform contBtn = completionPanel.transform.Find("Btn_Continue") 
                                 ?? completionPanel.transform.Find("Continue_Button")
                                 ?? completionPanel.transform.Find("ContinueButton");
                if (contBtn != null)
                {
                    Button b = contBtn.GetComponent<Button>();
                    if (b != null)
                    {
                        b.gameObject.SetActive(true);
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(() =>
                        {
                            StartCoroutine(PulseButton(b.transform));
                            OpenSectionSelection();
                        });
                    }
                }

                // 3. Next / Home Button -> Returns to the Global Lessons Unit Selection Panel
                Transform nextBtn = completionPanel.transform.Find("Next_Button") 
                                 ?? completionPanel.transform.Find("NextButton")
                                 ?? completionPanel.transform.Find("Btn_Next")
                                 ?? completionPanel.transform.Find("Btn_Home")
                                 ?? completionPanel.transform.Find("Home_Button");
                if (nextBtn != null)
                {
                    Button b = nextBtn.GetComponent<Button>();
                    if (b != null)
                    {
                        b.gameObject.SetActive(true);
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(() =>
                        {
                            StartCoroutine(PulseButton(b.transform));
                            BackToLessonsMenu();
                        });
                    }
                }
            }

            if (U4_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U4_SA_AudioManager_Masters_Phonics.Instance.PlayCelebration();
                if (unitCompleteVoiceClip != null)
                {
                    U4_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(unitCompleteVoiceClip);
                }
            }
        }
    }
}
