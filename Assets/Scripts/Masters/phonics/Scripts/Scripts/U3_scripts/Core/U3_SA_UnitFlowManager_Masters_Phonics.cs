using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U3_SA_UnitFlowManager_Masters_Phonics : MonoBehaviour
    {
        public static U3_SA_UnitFlowManager_Masters_Phonics Instance { get; private set; }

        [Header("Unit 3 Parent Containers")]
        [SerializeField] private GameObject sectionSelectionPanel; // Unit_3_Section_Selection_Panels
        [SerializeField] private GameObject sectionsParentContainer; // Unit_3_Sections

        [Header("Unit 3 Activity Panels (Auto-Bound)")]
        [SerializeField] private GameObject learnPanel;       // 0: Learn Concept Cards
        [SerializeField] private GameObject activity1Panel;   // 1: Chin Tap (Beat Pad)
        [SerializeField] private GameObject activity2Panel;   // 2: Split It (GM-02s Letter Gap Splitter)
        [SerializeField] private GameObject activity3Panel;   // 3: Syllable Sort (4 Bins)
        [SerializeField] private GameObject activity4Panel;   // 4: Syllable Builder (GM-05 Chunk Blending)
        [SerializeField] private GameObject activity5Panel;   // 5: Pattern Detective (C/V Pattern)
        [SerializeField] private GameObject completionPanel;  // 6: U3_COMPLETE_Panel

        [Header("Global Navigation (Auto-Bound)")]
        [SerializeField] private Button backButton;           // Back_Button under Unit_3_Sections
        [SerializeField] private Button nextButton;           // Next Button under Unit_3_Sections

        [Header("Unit 3 Completion Narration (Official Doc)")]
        [Tooltip("U03_VO_unit_complete: 'Unit Three, done. Forty-three words split. Badge unlocked — Syllable Splitter.' (7s)")]
        public AudioClip unitCompleteVoiceClip;

        [Header("Runtime State")]
        [SerializeField] private int currentActivityIndex = 0;
        private GameObject[] allPanels;

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
                Transform s = transform.Find("Unit_3_Section_Selection_Panels") 
                           ?? transform.Find("Section_Selection_Panels") 
                           ?? transform.Find("Unit3_Section_Selection_Panels");
                if (s != null) sectionSelectionPanel = s.gameObject;
            }

            if (sectionsParentContainer == null)
            {
                Transform s = transform.Find("Unit_3_Sections") 
                           ?? transform.Find("Sections") 
                           ?? transform.Find("Unit3_Sections");
                if (s != null) sectionsParentContainer = s.gameObject;
            }

            Transform container = sectionsParentContainer != null ? sectionsParentContainer.transform : transform;

            if (learnPanel == null)
            {
                Transform t = container.Find("U3_learn_Concept_Cards_Panel") ?? container.Find("U3_Learn_Concept_Cards_Panel") ?? container.Find("Learn_Panel");
                if (t != null) learnPanel = t.gameObject;
            }

            if (activity1Panel == null)
            {
                Transform t = container.Find("U3_Activity_1_Chin_Tap") ?? container.Find("Activity_1") ?? container.Find("Chin_Tap");
                if (t != null) activity1Panel = t.gameObject;
            }

            if (activity2Panel == null)
            {
                Transform t = container.Find("U3_Activity_2_Split_It") ?? container.Find("Activity_2") ?? container.Find("Split_It");
                if (t != null) activity2Panel = t.gameObject;
            }

            if (activity3Panel == null)
            {
                Transform t = container.Find("U3_Activity_3_Syllable_Sort") ?? container.Find("Activity_3") ?? container.Find("Syllable_Sort");
                if (t != null) activity3Panel = t.gameObject;
            }

            if (activity4Panel == null)
            {
                Transform t = container.Find("U3_Activity_4_Syllable_Builder") ?? container.Find("Activity_4") ?? container.Find("Syllable_Builder");
                if (t != null) activity4Panel = t.gameObject;
            }

            if (activity5Panel == null)
            {
                Transform t = container.Find("U3_Activity_5_Pattern_Detective") 
                           ?? container.Find("U3_Activity_5") 
                           ?? container.Find("U3_Optional_Challenge") 
                           ?? container.Find("U3_Unit_Challenge") 
                           ?? container.Find("U3_Challenge_Panel")
                           ?? container.Find("Activity_5") 
                           ?? container.Find("Pattern_Detective")
                           ?? container.Find("Challenge");
                if (t != null) activity5Panel = t.gameObject;
            }

            if (completionPanel == null)
            {
                Transform t = container.Find("U3_COMPLETE_Panel") ?? container.Find("Complete_Panel") ?? container.Find("Completion_Panel");
                if (t != null) completionPanel = t.gameObject;
            }

            if (backButton == null && sectionsParentContainer != null)
            {
                Transform t = sectionsParentContainer.transform.Find("Back_Button") 
                           ?? sectionsParentContainer.transform.Find("BackButton") 
                           ?? sectionsParentContainer.transform.Find("Btn_Back");
                if (t != null) backButton = t.GetComponent<Button>();
            }
            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OpenSectionSelection);
            }

            if (nextButton == null && sectionsParentContainer != null)
            {
                Transform t = sectionsParentContainer.transform.Find("Next Button") 
                           ?? sectionsParentContainer.transform.Find("Next_Button") 
                           ?? sectionsParentContainer.transform.Find("Btn_Next");
                if (t != null) nextButton = t.GetComponent<Button>();
            }
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OpenNextSection);
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
            currentActivityIndex = -1;

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
            }

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

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(false);
            }
        }

        public void BackToSectionSelection() => OpenSectionSelection();
        public void ShowSectionSelection() => OpenSectionSelection();

        public void OpenSection(int sectionIndex)
        {
            EnsureAutoBind();
            currentActivityIndex = sectionIndex;

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.StopAllAudio();
            }

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
                targetPanel.transform.SetAsLastSibling();
            }
            else
            {
                Debug.LogWarning($"[Unit3 FlowManager] Target panel for section index {sectionIndex} could not be found!");
            }

            // Static persistent back button on top of any active section
            if (backButton != null)
            {
                backButton.gameObject.SetActive(true);
                backButton.transform.SetAsLastSibling();
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(OpenSectionSelection);
            }

            // Next button behavior:
            // For Section 5 (Pattern Detective), keep Next Button visible with SKIP >> label so user can skip directly to completion
            if (sectionIndex == 5)
            {
                SetNextButtonVisible(true, "SKIP >>");
            }
            else if (nextButton != null)
            {
                nextButton.gameObject.SetActive(false);
            }
        }

        private GameObject GetPanelByIndex(int index)
        {
            switch (index)
            {
                case 0:
                    if (learnPanel == null) learnPanel = FindSectionChild("U3_learn_Concept_Cards_Panel");
                    return learnPanel;
                case 1:
                    if (activity1Panel == null) activity1Panel = FindSectionChild("U3_Activity_1_Chin_Tap");
                    return activity1Panel;
                case 2:
                    if (activity2Panel == null) activity2Panel = FindSectionChild("U3_Activity_2_Split_It");
                    return activity2Panel;
                case 3:
                    if (activity3Panel == null) activity3Panel = FindSectionChild("U3_Activity_3_Syllable_Sort");
                    return activity3Panel;
                case 4:
                    if (activity4Panel == null) activity4Panel = FindSectionChild("U3_Activity_4_Syllable_Builder");
                    return activity4Panel;
                case 5:
                    if (activity5Panel == null) activity5Panel = FindSectionChild("U3_Activity_5_Pattern_Detective");
                    return activity5Panel;
                case 6:
                    if (completionPanel == null) completionPanel = FindSectionChild("U3_COMPLETE_Panel");
                    return completionPanel;
                default:
                    return null;
            }
        }

        private GameObject FindSectionChild(string childName)
        {
            Transform container = sectionsParentContainer != null ? sectionsParentContainer.transform : transform;
            Transform t = container.Find(childName);
            if (t != null) return t.gameObject;

            foreach (Transform c in container.GetComponentsInChildren<Transform>(true))
            {
                if (c.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                {
                    return c.gameObject;
                }
            }
            return null;
        }

        public void CloseAllActivityPanels()
        {
            if (learnPanel != null) learnPanel.SetActive(false);
            if (activity1Panel != null) activity1Panel.SetActive(false);
            if (activity2Panel != null) activity2Panel.SetActive(false);
            if (activity3Panel != null) activity3Panel.SetActive(false);
            if (activity4Panel != null) activity4Panel.SetActive(false);
            if (activity5Panel != null) activity5Panel.SetActive(false);
            if (completionPanel != null) completionPanel.SetActive(false);

            if (allPanels != null)
            {
                foreach (var p in allPanels)
                {
                    if (p != null) p.SetActive(false);
                }
            }
        }

        public void BackToLessonsMenu()
        {
            CloseAllActivityPanels();

            if (Unit_Selection_Panel_Masters_Phonics.Instance != null)
            {
                Unit_Selection_Panel_Masters_Phonics.Instance.BackToUnitSelection();
            }
            else
            {
                var globalSelection = GameObject.Find("Unit_Selection_Panel") 
                                   ?? GameObject.Find("Unit_Selection_Panels")
                                   ?? GameObject.Find("Section_Selection_Panels");
                if (globalSelection != null)
                {
                    globalSelection.SetActive(true);
                }

                gameObject.SetActive(false);
            }
        }

        public void SetNextButtonVisible(bool visible, string customLabel = null)
        {
            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(visible);
                if (visible)
                {
                    nextButton.transform.SetAsLastSibling();
                    nextButton.transform.localScale = Vector3.one;
                    nextButton.onClick.RemoveAllListeners();
                    nextButton.onClick.AddListener(OpenNextSection);

                    string labelText = !string.IsNullOrEmpty(customLabel) ? customLabel : "NEXT >>";
                    var tmp = nextButton.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (tmp != null) tmp.text = $"<b>{labelText}</b>";
                    else
                    {
                        var txt = nextButton.GetComponentInChildren<Text>(true);
                        if (txt != null) txt.text = labelText;
                    }

                    if (isActiveAndEnabled && gameObject.activeInHierarchy)
                    {
                        StartCoroutine(PulseButton(nextButton.transform));
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

        private float lastTransitionTime = 0f;

        public int GetActiveSectionIndex()
        {
            if (activity5Panel != null && activity5Panel.activeInHierarchy) return 5;
            if (activity4Panel != null && activity4Panel.activeInHierarchy) return 4;
            if (activity3Panel != null && activity3Panel.activeInHierarchy) return 3;
            if (activity2Panel != null && activity2Panel.activeInHierarchy) return 2;
            if (activity1Panel != null && activity1Panel.activeInHierarchy) return 1;
            if (learnPanel != null && learnPanel.activeInHierarchy) return 0;
            return currentActivityIndex;
        }

        public void SetCurrentActivityIndex(int index)
        {
            currentActivityIndex = index;
        }

        public void OpenNextSection()
        {
            // Debounce guard: Prevent double execution from multiple listeners or rapid taps
            if (Time.unscaledTime - lastTransitionTime < 0.6f) return;
            lastTransitionTime = Time.unscaledTime;

            int fromIndex = GetActiveSectionIndex();
            Debug.Log($"[Unit3 FlowManager] Advancing from section {fromIndex}");

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(false);
            }

            switch (fromIndex)
            {
                case 0: // Section A: Learn (Concept Cards) -> Section B: Act 1 (Chin Tap)
                    OpenSection(1);
                    break;
                case 1: // Section B: Act 1 (Chin Tap) -> Section C: Act 2 (Split It)
                    OpenSection(2);
                    break;
                case 2: // Section C: Act 2 (Split It) -> Section D: Act 3 (Syllable Sort)
                    OpenSection(3);
                    break;
                case 3: // Section D: Act 3 (Syllable Sort) -> Section E: Act 4 (Syllable Builder)
                    OpenSection(4);
                    break;
                case 4: // Section E: Act 4 (Syllable Builder) -> Section F: Act 5 (Pattern Detective)
                    OpenSection(5);
                    break;
                case 5: // Section F: Act 5 (Pattern Detective) -> Unit Complete
                    ShowUnitCompletion();
                    break;
                default:
                    ShowUnitCompletion();
                    break;
            }
        }

        public void ShowUnitCompletion()
        {
            CloseAllActivityPanels();

            if (nextButton != null)
            {
                nextButton.gameObject.SetActive(false);
            }

            if (completionPanel != null)
            {
                completionPanel.SetActive(true);
                completionPanel.transform.SetAsLastSibling();

                if (backButton != null)
                {
                    backButton.gameObject.SetActive(true);
                    backButton.transform.SetAsLastSibling();
                }

                Transform contBtn = completionPanel.transform.Find("Btn_Continue") 
                                 ?? completionPanel.transform.Find("Continue_Button")
                                 ?? completionPanel.transform.Find("ContinueButton")
                                 ?? completionPanel.transform.Find("Btn_Home")
                                 ?? completionPanel.transform.Find("Next_Button");
                if (contBtn != null)
                {
                    Button b = contBtn.GetComponent<Button>();
                    if (b != null)
                    {
                        b.onClick.RemoveAllListeners();
                        b.onClick.AddListener(OnContinueTapped);
                    }
                }
            }

            if (U3_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U3_SA_AudioManager_Masters_Phonics.Instance.PlayCelebration();
                if (unitCompleteVoiceClip != null)
                {
                    U3_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA(unitCompleteVoiceClip);
                }
            }
        }

        private void OnContinueTapped()
        {
            if (isActiveAndEnabled && gameObject.activeInHierarchy)
            {
                StartCoroutine(CoContinueToLessons());
            }
            else
            {
                BackToLessonsMenu();
            }
        }

        private IEnumerator CoContinueToLessons()
        {
            yield return new WaitForSeconds(0.12f);
            BackToLessonsMenu();
        }
    }
}
