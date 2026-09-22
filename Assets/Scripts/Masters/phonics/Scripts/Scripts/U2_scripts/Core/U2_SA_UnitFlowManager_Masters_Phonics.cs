using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U2_SA_UnitFlowManager_Masters_Phonics : MonoBehaviour
    {
        public static U2_SA_UnitFlowManager_Masters_Phonics Instance { get; private set; }

        [Header("Unit 2 Parent Containers")]
        [SerializeField] private GameObject sectionSelectionPanel; // Unit_2_Section_Selection_Panels
        [SerializeField] private GameObject sectionsParentContainer; // Unit_2_Sections

        [Header("Unit 2 Activity Panels (Auto-Bound)")]
        [SerializeField] private GameObject learnPanel;       // 0: Learn Concept Cards
        [SerializeField] private GameObject activity1Panel;   // 1: Job Board (4-Chute Sort)
        [SerializeField] private GameObject activity2Panel;   // 2: Plural Factory (+s vs +es)
        [SerializeField] private GameObject activity3Panel;   // 3: Double Trouble (Spelling Dial)
        [SerializeField] private GameObject activity4Panel;   // 4: Compare Ladder (-er vs -est)
        [SerializeField] private GameObject activity5Panel;   // 5: Y-to-I Lab (3-Round Tile Builder)
        [SerializeField] private GameObject completionPanel;  // 6: U2_COMPLETE_Panel

        [Header("Unit 2 Completion Narration (Official Doc)")]
        [Tooltip("U02_VO_unit_complete: 'Unit Two, done. Sixty-two word forms built. That’s a lot of spelling. Badge unlocked — Word Changer.' (9s)")]
        public AudioClip unitCompleteVoiceClip; // U02_VO_unit_complete

        [Header("Runtime State")]
        [SerializeField] private int currentActivityIndex = 0;
        private GameObject[] allPanels;
        private bool isTransitioning = false;

        public int CurrentActivityIndex => currentActivityIndex;

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

        public void EnsureAutoBind()
        {
            if (sectionSelectionPanel == null)
            {
                Transform s = transform.Find("Unit_2_Section_Selection_Panels") 
                           ?? transform.Find("Section_Selection_Panels") 
                           ?? transform.Find("Unit2_Section_Selection_Panels");
                if (s != null) sectionSelectionPanel = s.gameObject;
            }

            if (sectionsParentContainer == null)
            {
                Transform s = transform.Find("Unit_2_Sections") 
                           ?? transform.Find("Sections") 
                           ?? transform.Find("Unit2_Sections");
                if (s != null) sectionsParentContainer = s.gameObject;
            }

            Transform container = sectionsParentContainer != null ? sectionsParentContainer.transform : transform;

            if (learnPanel == null)
            {
                Transform t = container.Find("U2_learn_Concept_Cards_Panel") ?? container.Find("U2_Learn_Concept_Cards_Panel") ?? container.Find("Learn_Panel");
                if (t != null) learnPanel = t.gameObject;
            }

            if (activity1Panel == null)
            {
                Transform t = container.Find("U2_Activity_1_Job_Board") ?? container.Find("Activity_1") ?? container.Find("Job_Board");
                if (t != null) activity1Panel = t.gameObject;
            }

            if (activity2Panel == null)
            {
                Transform t = container.Find("U2_Activity_2_Plural_Factory") ?? container.Find("Activity_2") ?? container.Find("Plural_Factory");
                if (t != null) activity2Panel = t.gameObject;
            }

            if (activity3Panel == null)
            {
                Transform t = container.Find("U2_Activity_3_Double_Trouble") ?? container.Find("Activity_3") ?? container.Find("Double_Trouble");
                if (t != null) activity3Panel = t.gameObject;
            }

            if (activity4Panel == null)
            {
                Transform t = container.Find("U2_Activity_4_Compare_Ladder") ?? container.Find("Activity_4") ?? container.Find("Compare_Ladder");
                if (t != null) activity4Panel = t.gameObject;
            }

            if (activity5Panel == null)
            {
                Transform t = container.Find("U2_Activity_5_Y_To_I_Lab") ?? container.Find("Activity_5") ?? container.Find("Y_To_I_Lab");
                if (t != null) activity5Panel = t.gameObject;
            }

            if (completionPanel == null)
            {
                Transform t = container.Find("U2_COMPLETE_Panel") ?? container.Find("Complete_Panel") ?? container.Find("Completion_Panel");
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
                completionPanel
            };
        }

        public void OpenSectionSelection()
        {
            EnsureAutoBind();
            CloseAllActivityPanels();

            if (!gameObject.activeSelf) gameObject.SetActive(true);

            if (sectionSelectionPanel != null)
            {
                sectionSelectionPanel.SetActive(true);
                foreach (Transform child in sectionSelectionPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null) child.gameObject.SetActive(true);
                }

                Transform backBtn = sectionSelectionPanel.transform.Find("Back_Button") 
                                 ?? sectionSelectionPanel.transform.Find("BackButton")
                                 ?? sectionSelectionPanel.transform.Find("Section_Selection_Back");
                if (backBtn != null)
                {
                    Button b = backBtn.GetComponent<Button>();
                    if (b != null)
                    {
                        b.onClick.RemoveListener(BackToLessonsMenu);
                        b.onClick.AddListener(BackToLessonsMenu);
                    }
                }
            }

            if (sectionsParentContainer != null)
                sectionsParentContainer.SetActive(false);
        }

        public void OpenSection(int index)
        {
            EnsureAutoBind();
            CloseAllActivityPanels();

            if (sectionSelectionPanel != null)
                sectionSelectionPanel.SetActive(false);

            if (sectionsParentContainer != null)
            {
                sectionsParentContainer.SetActive(true);
                Transform secBack = sectionsParentContainer.transform.Find("Back_Button") 
                                 ?? sectionsParentContainer.transform.Find("Section_Selection_Back");
                if (secBack != null) secBack.gameObject.SetActive(true);
            }

            currentActivityIndex = index;
            GameObject targetPanel = GetPanelByIndex(index);

            if (targetPanel != null)
            {
                Debug.Log($"<color=cyan>[Unit2Flow] OpenSection({index}) -> Activating: '{targetPanel.name}'</color>");

                if (completionPanel != null && completionPanel.activeSelf)
                    completionPanel.SetActive(false);

                targetPanel.SetActive(true);

                foreach (Transform child in targetPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null)
                    {
                        string cName = child.name.ToLower();
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

                if (index > 0)
                {
                    SetNextButtonVisible(false);
                }

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
        }

        public void OpenLearn() => OpenSection(0);
        public void OpenActivity1() => OpenSection(1);
        public void OpenActivity2() => OpenSection(2);
        public void OpenActivity3() => OpenSection(3);
        public void OpenActivity4() => OpenSection(4);
        public void OpenActivity5() => OpenSection(5);

        private GameObject GetPanelByIndex(int index)
        {
            EnsureAutoBind();
            Transform container = sectionsParentContainer != null ? sectionsParentContainer.transform : transform;

            switch (index)
            {
                case 0:
                    if (learnPanel == null) learnPanel = container.Find("U2_learn_Concept_Cards_Panel")?.gameObject ?? container.Find("U2_Learn_Concept_Cards_Panel")?.gameObject;
                    return learnPanel;
                case 1:
                    if (activity1Panel == null) activity1Panel = container.Find("U2_Activity_1_Job_Board")?.gameObject;
                    return activity1Panel;
                case 2:
                    if (activity2Panel == null) activity2Panel = container.Find("U2_Activity_2_Plural_Factory")?.gameObject;
                    return activity2Panel;
                case 3:
                    if (activity3Panel == null) activity3Panel = container.Find("U2_Activity_3_Double_Trouble")?.gameObject;
                    return activity3Panel;
                case 4:
                    if (activity4Panel == null) activity4Panel = container.Find("U2_Activity_4_Compare_Ladder")?.gameObject;
                    return activity4Panel;
                case 5:
                    if (activity5Panel == null) activity5Panel = container.Find("U2_Activity_5_Y_To_I_Lab")?.gameObject;
                    return activity5Panel;
                case 6:
                    if (completionPanel == null) completionPanel = container.Find("U2_COMPLETE_Panel")?.gameObject;
                    return completionPanel;
                default:
                    return null;
            }
        }

        public void CloseAllActivityPanels()
        {
            if (allPanels == null) EnsureAutoBind();
            if (allPanels != null)
            {
                foreach (var p in allPanels)
                {
                    if (p != null) p.SetActive(false);
                }
            }
        }

        public void BackToSectionSelection()
        {
            if (U2_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
            }
            OpenSectionSelection();
        }

        public void BackToLessonsMenu()
        {
            if (U2_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
            }

            if (Unit_Selection_Panel_Masters_Phonics.Instance != null)
            {
                Unit_Selection_Panel_Masters_Phonics.Instance.BackToUnitSelection();
            }
            else
            {
                Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var c in allCanvases)
                {
                    if (c == null) continue;
                    Transform l = c.transform.Find("Lessons") ?? c.transform.Find("Unit_Selection_Panel");
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

        public void SetNextButtonVisible(bool visible)
        {
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

        private IEnumerator PulseButton(Transform tr)
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

        public void OnActivityComplete()
        {
            Debug.Log($"<color=green>[Unit2Flow] Activity {currentActivityIndex} completed! Revealing Next Button...</color>");
            SetNextButtonVisible(true);
        }

        public void OpenNextSection()
        {
            if (isTransitioning) return;
            isTransitioning = true;

            int fromIndex = currentActivityIndex;
            SetNextButtonVisible(false);

            Debug.Log($"<color=cyan>[Unit2NextButton] Transitioning from Section {fromIndex}</color>");

            switch (fromIndex)
            {
                case 0: // Section A: Learn -> Section B: Activity 1 (Job Board)
                    OpenSection(1);
                    break;
                case 1: // Section B: Activity 1 (Job Board) -> Section C: Activity 2 (Plural Factory)
                    OpenSection(2);
                    break;
                case 2: // Section C: Activity 2 (Plural Factory) -> Section D: Activity 3 (Double Trouble)
                    OpenSection(3);
                    break;
                case 3: // Section D: Activity 3 (Double Trouble) -> Section E: Activity 4 (Compare Ladder)
                    OpenSection(4);
                    break;
                case 4: // Section E: Activity 4 (Compare Ladder) -> Section F: Activity 5 (Y-to-I Lab)
                    OpenSection(5);
                    break;
                case 5: // Section F: Activity 5 (Y-to-I Lab) -> Unit 2 Complete!
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
            CloseAllActivityPanels();

            if (completionPanel != null)
            {
                completionPanel.SetActive(true);
                foreach (Transform child in completionPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null) child.gameObject.SetActive(true);
                }

                Transform contBtn = completionPanel.transform.Find("Btn_Continue") 
                                 ?? completionPanel.transform.Find("ContinueButton")
                                 ?? completionPanel.transform.Find("Next_Button");
                if (contBtn != null)
                {
                    Button b = contBtn.GetComponent<Button>();
                    if (b != null)
                    {
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(BackToLessonsMenu);
                    }
                }
            }

            if (U2_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U2_SA_AudioManager_Masters_Phonics.Instance.PlayCelebration();
                if (unitCompleteVoiceClip != null)
                {
                    U2_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(unitCompleteVoiceClip);
                }
            }
        }
    }
}
