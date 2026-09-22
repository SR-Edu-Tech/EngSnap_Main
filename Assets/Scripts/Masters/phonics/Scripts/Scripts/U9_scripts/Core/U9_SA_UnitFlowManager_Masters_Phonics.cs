using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U9_SA_UnitFlowManager_Masters_Phonics : MonoBehaviour
    {
        public static U9_SA_UnitFlowManager_Masters_Phonics Instance { get; private set; }

        [Header("Unit Signboard & Panels Root")]
        [SerializeField] private GameObject selectionSignboardPanel;
        [SerializeField] private GameObject sectionsParent;
        [SerializeField] private GameObject learnPanel;
        [SerializeField] private GameObject activity1Panel;
        [SerializeField] private GameObject activity2Panel;
        [SerializeField] private GameObject activity3Panel;
        [SerializeField] private GameObject activity4Panel;
        [SerializeField] private GameObject unitChallengePanel;
        [SerializeField] private GameObject mapUpdatePanel;
        [SerializeField] private GameObject completionReportPanel;

        [Header("Section Panels Array (0 to 7)")]
        public GameObject[] sectionPanels;

        [Header("Signboard Section Buttons (5 Wagons)")]
        [SerializeField] private Button wagon1Btn; // Wagon 1: Learn -> Act 1
        [SerializeField] private Button wagon2Btn; // Wagon 2: Act 2 (One or Two?)
        [SerializeField] private Button wagon3Btn; // Wagon 3: Act 3 (Build Ending)
        [SerializeField] private Button wagon4Btn; // Wagon 4: Act 4 (Sentence Hunt)
        [SerializeField] private Button wagon5Btn; // Wagon 5: Challenge / Map / Complete
        public Button[] allWagonButtons;

        [Header("Global Navigation")]
        [SerializeField] private Button globalBackBtn;
        [SerializeField] private Button nextActivityBtn;

        [Header("Star Rating Sprites (Completion Modal)")]
        public Sprite goldenStarSprite;
        public Sprite emptyStarSprite;

        [Header("Cumulative Progress & Stats")]
        public int totalUnitScore = 0;
        public int wordsSplitCount = 0;
        public int sentencesSolvedCount = 0;
        public Dictionary<int, int> activityStars = new Dictionary<int, int>();
        public List<string> mistakeQueue = new List<string>();

        private int currentActiveSection = 0;

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

            AutoBindHierarchyElements();
        }

        private void Start()
        {
            AutoBindHierarchyElements();
            OpenSignboard();
        }

        public void AutoBindHierarchyElements()
        {
            Transform root = transform;

            if (selectionSignboardPanel == null)
            {
                Transform p = root.Find("Unit_9_Section_Selection_Panels")
                           ?? root.Find("Section_Selection_Panel")
                           ?? root.Find("Section_Selection_Panels")
                           ?? root.Find("Signboard")
                           ?? root.Find("Unit_9_Section_Selection_Panel");
                if (p != null) selectionSignboardPanel = p.gameObject;
            }

            if (sectionsParent == null)
            {
                Transform sec = root.Find("Unit_9_Sections")
                             ?? root.Find("Sections")
                             ?? root.Find("Unit_9_Section_Panels");
                if (sec != null) sectionsParent = sec.gameObject;
            }

            Transform secRoot = sectionsParent != null ? sectionsParent.transform : root;

            if (learnPanel == null && secRoot != null)
            {
                var comp = secRoot.GetComponentInChildren<U9_SA_GM01_ConceptCards_Masters_Phonics>(true);
                if (comp != null) learnPanel = comp.gameObject;
                else
                {
                    Transform l = secRoot.Find("U9_learn_Concept_Cards_Panel")
                               ?? secRoot.Find("UX_learn_Concept_Cards_Panel")
                               ?? secRoot.Find("Learn_Panel")
                               ?? secRoot.Find("Concept_Cards_Panel");
                    if (l != null) learnPanel = l.gameObject;
                }
            }

            if (activity1Panel == null && secRoot != null)
            {
                var comp = secRoot.GetComponentInChildren<U9_SA_GM02s_TurtleRule_Masters_Phonics>(true);
                if (comp != null) activity1Panel = comp.gameObject;
                else
                {
                    Transform a1 = secRoot.Find("U9_Activity_1_Turtle_Rule")
                                ?? secRoot.Find("U9_Activity_1_TurtleRule_Panel")
                                ?? secRoot.Find("UX_Activity_1_TurtleRule_Panel")
                                ?? secRoot.Find("Activity_1_Panel")
                                ?? secRoot.Find("TurtleRule_Panel");
                    if (a1 != null) activity1Panel = a1.gameObject;
                }
            }

            if (activity2Panel == null && secRoot != null)
            {
                var comp = secRoot.GetComponentInChildren<U9_SA_GM03s_OneOrTwo_Masters_Phonics>(true);
                if (comp != null) activity2Panel = comp.gameObject;
                else
                {
                    Transform a2 = secRoot.Find("U9_Activity_2_One_Or_Two")
                                ?? secRoot.Find("U9_Activity_2_OneOrTwo_Panel")
                                ?? secRoot.Find("UX_Activity_2_OneOrTwo_Panel")
                                ?? secRoot.Find("Activity_2_Panel")
                                ?? secRoot.Find("OneOrTwo_Panel");
                    if (a2 != null) activity2Panel = a2.gameObject;
                }
            }

            if (activity3Panel == null && secRoot != null)
            {
                var comp = secRoot.GetComponentInChildren<U9_SA_GM02_BuildEnding_Masters_Phonics>(true);
                if (comp != null) activity3Panel = comp.gameObject;
                else
                {
                    Transform a3 = secRoot.Find("U9_Activity_3_Build_Ending")
                                ?? secRoot.Find("U9_Activity_3_BuildEnding_Panel")
                                ?? secRoot.Find("UX_Activity_3_BuildEnding_Panel")
                                ?? secRoot.Find("Activity_3_Panel")
                                ?? secRoot.Find("BuildEnding_Panel");
                    if (a3 != null) activity3Panel = a3.gameObject;
                }
            }

            if (activity4Panel == null && secRoot != null)
            {
                var comp = secRoot.GetComponentInChildren<U9_SA_GM06_SentenceHunt_Masters_Phonics>(true);
                if (comp != null) activity4Panel = comp.gameObject;
                else
                {
                    Transform a4 = secRoot.Find("U9_Activity_4_Sentence_Hunt")
                                ?? secRoot.Find("U9_Activity_4_SentenceHunt_Panel")
                                ?? secRoot.Find("UX_Activity_4_SentenceHunt_Panel")
                                ?? secRoot.Find("Activity_4_Panel")
                                ?? secRoot.Find("SentenceHunt_Panel");
                    if (a4 != null) activity4Panel = a4.gameObject;
                }
            }

            if (unitChallengePanel == null && secRoot != null)
            {
                var comp = secRoot.GetComponentInChildren<U9_SA_UnitChallenge_Masters_Phonics>(true);
                if (comp != null) unitChallengePanel = comp.gameObject;
                else
                {
                    Transform uc = secRoot.Find("U9_UnitChallenge_Panel")
                                ?? secRoot.Find("UX_UnitChallenge_Panel")
                                ?? secRoot.Find("UnitChallenge_Panel")
                                ?? secRoot.Find("Challenge_Panel");
                    if (uc != null) unitChallengePanel = uc.gameObject;
                }
            }

            if (mapUpdatePanel == null && secRoot != null)
            {
                var comp = secRoot.GetComponentInChildren<U9_SA_SevenTypesMap_Masters_Phonics>(true);
                if (comp != null) mapUpdatePanel = comp.gameObject;
                else
                {
                    Transform map = secRoot.Find("U9_SevenTypesMap_Panel")
                                 ?? secRoot.Find("UX_SevenTypesMap_Panel")
                                 ?? secRoot.Find("SevenTypesMap_Panel")
                                 ?? secRoot.Find("Map_Panel");
                    if (map != null) mapUpdatePanel = map.gameObject;
                }
            }

            if (completionReportPanel == null && secRoot != null)
            {
                var comp = secRoot.GetComponentInChildren<U9_SA_CompletionPanel_Masters_Phonics>(true);
                if (comp != null) completionReportPanel = comp.gameObject;
                else
                {
                    Transform cr = secRoot.Find("U9_COMPLETE_Panel")
                                ?? secRoot.Find("UX_COMPLETE_Panel")
                                ?? secRoot.Find("Complete_Panel")
                                ?? secRoot.Find("Completion_Panel");
                    if (cr != null) completionReportPanel = cr.gameObject;
                }
            }

            // Sync array
            sectionPanels = new GameObject[]
            {
                learnPanel,             // 0
                activity1Panel,         // 1
                activity2Panel,         // 2
                activity3Panel,         // 3
                activity4Panel,         // 4
                unitChallengePanel,     // 5
                mapUpdatePanel,         // 6
                completionReportPanel   // 7
            };

            // Bind wagons
            BindWagonButtons();
            BindGlobalNavigation();
            EnsureStarSprites();
        }

        private void BindWagonButtons()
        {
            if (selectionSignboardPanel == null) return;

            Transform sp = selectionSignboardPanel.transform;
            Transform content = sp.Find("Content") ?? sp;

            if (wagon1Btn == null) wagon1Btn = FindButton(content, "Wagon1", "Cart1", "Truck1", "Section1_Btn", "Btn_1");
            if (wagon2Btn == null) wagon2Btn = FindButton(content, "Wagon2", "Cart2", "Truck2", "Section2_Btn", "Btn_2");
            if (wagon3Btn == null) wagon3Btn = FindButton(content, "Wagon3", "Cart3", "Truck3", "Section3_Btn", "Btn_3");
            if (wagon4Btn == null) wagon4Btn = FindButton(content, "Wagon4", "Cart4", "Truck4", "Section4_Btn", "Btn_4");
            if (wagon5Btn == null) wagon5Btn = FindButton(content, "Wagon5", "Cart5", "Truck5", "Section5_Btn", "Btn_5");

            // Attach listeners unconditionally
            if (wagon1Btn != null)
            {
                wagon1Btn.onClick.RemoveAllListeners();
                wagon1Btn.onClick.AddListener(OpenLearn);
            }

            if (wagon2Btn != null)
            {
                wagon2Btn.onClick.RemoveAllListeners();
                wagon2Btn.onClick.AddListener(OpenActivity2);
            }

            if (wagon3Btn != null)
            {
                wagon3Btn.onClick.RemoveAllListeners();
                wagon3Btn.onClick.AddListener(OpenActivity3);
            }

            if (wagon4Btn != null)
            {
                wagon4Btn.onClick.RemoveAllListeners();
                wagon4Btn.onClick.AddListener(OpenActivity4);
            }

            if (wagon5Btn != null)
            {
                wagon5Btn.onClick.RemoveAllListeners();
                wagon5Btn.onClick.AddListener(OpenChallenge);
            }
        }

        private void BindGlobalNavigation()
        {
            Transform root = transform;
            Transform secRoot = sectionsParent != null ? sectionsParent.transform : root;

            // SAFETY: Clear collisions if serialized to buttons inside child panels
            if (globalBackBtn != null)
            {
                if (sectionPanels != null)
                {
                    foreach (var panel in sectionPanels)
                    {
                        if (panel != null && globalBackBtn.transform.IsChildOf(panel.transform))
                        {
                            globalBackBtn = null;
                            break;
                        }
                    }
                }
            }

            if (nextActivityBtn != null)
            {
                if (sectionPanels != null)
                {
                    foreach (var panel in sectionPanels)
                    {
                        if (panel != null && nextActivityBtn.transform.IsChildOf(panel.transform))
                        {
                            nextActivityBtn = null;
                            break;
                        }
                    }
                }
            }

            // 1. Static Global Back Button (Top-Left)
            if (globalBackBtn == null)
            {
                globalBackBtn = FindMainButton(secRoot, "GlobalBack_Btn", "Back_Button main", "Back_Btn", "Back_Button", "BackButton", "Back Button", "GlobalBackBtn", "Return_Btn", "Back")
                             ?? FindMainButton(root, "GlobalBack_Btn", "Back_Button main", "Back_Btn", "Back_Button", "BackButton", "Back Button", "GlobalBackBtn", "Return_Btn", "Back");
            }

            if (globalBackBtn == null)
            {
                Transform btnParent = secRoot;
                GameObject backGo = new GameObject("GlobalBack_Btn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                backGo.transform.SetParent(btnParent, false);
                RectTransform rt = backGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(40, -30);
                rt.sizeDelta = new Vector2(160, 55);
                U9_UI_Utils.ApplyRoundedButtonStyle(backGo.GetComponent<Image>(), new Color(0.2f, 0.3f, 0.45f));

                GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGO.transform.SetParent(backGo.transform, false);
                RectTransform trt = txtGO.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;
                var txt = txtGO.GetComponent<TextMeshProUGUI>();
                txt.text = "<b><< BACK</b>";
                txt.fontSize = 24;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.white;

                globalBackBtn = backGo.GetComponent<Button>();
            }

            if (globalBackBtn != null)
            {
                globalBackBtn.gameObject.SetActive(true);
                globalBackBtn.onClick.RemoveAllListeners();
                globalBackBtn.onClick.AddListener(OnGlobalBackTapped);
            }

            // 2. Dynamic Next Section Button (Bottom-Right)
            if (nextActivityBtn == null)
            {
                nextActivityBtn = FindMainButton(secRoot, "NextActivity_Btn", "Next_Button main", "Next_Btn", "Next_Button", "NextButton", "Next Button", "NextSection_Btn", "NextActivity_Button", "GlobalNextBtn", "Next")
                               ?? FindMainButton(root, "NextActivity_Btn", "Next_Button main", "Next_Btn", "Next_Button", "NextButton", "Next Button", "NextSection_Btn", "NextActivity_Button", "GlobalNextBtn", "Next");
            }

            if (nextActivityBtn == null)
            {
                Transform btnParent = secRoot;
                GameObject nextGo = new GameObject("NextActivity_Btn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                nextGo.transform.SetParent(btnParent, false);
                RectTransform rt = nextGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot = new Vector2(1, 0);
                rt.anchoredPosition = new Vector2(-40, 30);
                rt.sizeDelta = new Vector2(240, 65);
                U9_UI_Utils.ApplyRoundedButtonStyle(nextGo.GetComponent<Image>(), new Color(0.12f, 0.75f, 0.38f));

                GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGO.transform.SetParent(nextGo.transform, false);
                RectTransform trt = txtGO.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero;
                trt.anchorMax = Vector2.one;
                trt.offsetMin = Vector2.zero;
                trt.offsetMax = Vector2.zero;
                var txt = txtGO.GetComponent<TextMeshProUGUI>();
                txt.text = "<b>NEXT >></b>";
                txt.fontSize = 28;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.white;

                nextActivityBtn = nextGo.GetComponent<Button>();
            }

            if (nextActivityBtn != null)
            {
                nextActivityBtn.gameObject.SetActive(false); // Hidden during gameplay, enabled on section finish
                nextActivityBtn.onClick.RemoveAllListeners();
                nextActivityBtn.onClick.AddListener(AdvanceNextSection);
            }
        }

        private Button FindMainButton(Transform parent, params string[] names)
        {
            if (parent == null) return null;
            foreach (var n in names)
            {
                Transform t = parent.Find(n);
                if (t != null)
                {
                    Button b = t.GetComponent<Button>();
                    if (b != null) return b;
                }
            }
            return null;
        }

        private void EnsureStarSprites()
        {
#if UNITY_EDITOR
            if (goldenStarSprite == null) goldenStarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/unit9_MP/sprite U9 MP.png") ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/unit8_MP/sprite U8 MP.png");
            if (emptyStarSprite == null) emptyStarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/unit8_MP/sprite U8 MP.png");
#endif
        }

        // =====================================================================
        // Navigation & Section Switching
        // =====================================================================

        public void OpenSignboard()
        {
            currentActiveSection = 0;
            if (selectionSignboardPanel != null) selectionSignboardPanel.SetActive(true);
            if (sectionsParent != null) sectionsParent.SetActive(false);

            HideAllSectionPanels();
            if (nextActivityBtn != null) nextActivityBtn.gameObject.SetActive(false);

            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayVoiceA("U09_VO_unit_intro");
            }
        }

        public void ShowSingleSectionPanel(GameObject targetPanel, int sectionIndex)
        {
            currentActiveSection = sectionIndex;

            if (selectionSignboardPanel != null) selectionSignboardPanel.SetActive(false);
            if (sectionsParent != null) sectionsParent.SetActive(true);

            HideAllSectionPanels();

            if (targetPanel != null)
            {
                targetPanel.SetActive(true);
                EnsureParentChainActive(targetPanel.transform);
            }

            // Always bring static Back button to front & make active
            if (globalBackBtn != null)
            {
                globalBackBtn.gameObject.SetActive(true);
                globalBackBtn.transform.SetAsLastSibling();
            }

            // Next button is hidden during gameplay, shown when activity/card completes
            if (nextActivityBtn != null)
            {
                nextActivityBtn.gameObject.SetActive(false);
                nextActivityBtn.transform.SetAsLastSibling();
            }
        }

        public void SetNextActivityButtonActive(bool active)
        {
            if (nextActivityBtn != null)
            {
                nextActivityBtn.gameObject.SetActive(active);
                if (active) nextActivityBtn.transform.SetAsLastSibling();
            }
        }

        public void SetGlobalBackButtonActive(bool active)
        {
            if (globalBackBtn != null)
            {
                globalBackBtn.gameObject.SetActive(active);
                if (active) globalBackBtn.transform.SetAsLastSibling();
            }
        }

        private void HideAllSectionPanels()
        {
            if (sectionPanels != null)
            {
                foreach (var p in sectionPanels)
                {
                    if (p != null) p.SetActive(false);
                }
            }

            if (sectionsParent != null)
            {
                foreach (Transform child in sectionsParent.transform)
                {
                    if (child != null && child.gameObject != globalBackBtn?.gameObject && child.gameObject != nextActivityBtn?.gameObject)
                    {
                        child.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void EnsureParentChainActive(Transform t)
        {
            while (t != null && t != transform)
            {
                t.gameObject.SetActive(true);
                t = t.parent;
            }
        }

        public void OpenLearn() => ShowSingleSectionPanel(learnPanel, 0);
        public void OpenActivity1() => ShowSingleSectionPanel(activity1Panel, 1);
        public void OpenActivity2() => ShowSingleSectionPanel(activity2Panel, 2);
        public void OpenActivity3() => ShowSingleSectionPanel(activity3Panel, 3);
        public void OpenActivity4() => ShowSingleSectionPanel(activity4Panel, 4);
        public void OpenChallenge() => ShowSingleSectionPanel(unitChallengePanel, 5);
        public void OpenSevenTypesMap() => ShowSingleSectionPanel(mapUpdatePanel, 6);
        public void OpenCompletionPanel() => ShowSingleSectionPanel(completionReportPanel, 7);

        public void AdvanceNextSection()
        {
            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayClick();

            switch (currentActiveSection)
            {
                case 0: OpenActivity1(); break;
                case 1: OpenActivity2(); break;
                case 2: OpenActivity3(); break;
                case 3: OpenActivity4(); break;
                case 4: OpenChallenge(); break;
                case 5: OpenSevenTypesMap(); break;
                case 6: OpenCompletionPanel(); break;
                case 7: BackToLessonsMenu(); break;
                default: OpenSignboard(); break;
            }
        }

        public void OnGlobalBackTapped()
        {
            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayClick();
                U9_SA_AudioManager_Masters_Phonics.Instance.StopAll();
            }

            if (currentActiveSection != 0 || (sectionsParent != null && sectionsParent.activeSelf))
            {
                OpenSignboard();
            }
            else
            {
                BackToLessonsMenu();
            }
        }

        public void BackToLessonsMenu()
        {
            if (Unit_Selection_Panel_Masters_Phonics.Instance != null)
            {
                Unit_Selection_Panel_Masters_Phonics.Instance.BackToUnitSelection();
            }
        }

        // =====================================================================
        // Scoring & Modals
        // =====================================================================

        public void AddScore(int points)
        {
            totalUnitScore += points;
        }

        public int GetCumulativeScore() => totalUnitScore;

        public void RecordMistake(string item)
        {
            if (!string.IsNullOrEmpty(item) && !mistakeQueue.Contains(item))
            {
                mistakeQueue.Add(item);
            }
        }

        public void ShowActivityCompletionDialog(Transform parentContainer, int activityNumber, int earnedStars, int scoreEarned, System.Action onNextTapped)
        {
            activityStars[activityNumber] = earnedStars;
            AddScore(scoreEarned);

            if (U9_SA_AudioManager_Masters_Phonics.Instance != null)
            {
                U9_SA_AudioManager_Masters_Phonics.Instance.PlayCelebration();
            }

            // Look for pre-existing or instantiate clean modal
            GameObject modalGO = new GameObject($"Activity_{activityNumber}_StarCompletionModal", typeof(RectTransform), typeof(Image));
            modalGO.transform.SetParent(parentContainer, false);

            RectTransform rt = modalGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Image bg = modalGO.GetComponent<Image>();
            bg.color = new Color(0.05f, 0.08f, 0.15f, 0.92f); // Dark semi-transparent overlay

            // Card Panel
            GameObject card = new GameObject("ModalCard", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(modalGO.transform, false);
            RectTransform cardRt = card.GetComponent<RectTransform>();
            cardRt.sizeDelta = new Vector2(660, 490);
            Image cardImg = card.GetComponent<Image>();
            U9_UI_Utils.ApplyRoundedCardStyle(cardImg, new Color(0.12f, 0.18f, 0.28f, 0.98f));

            // Title (Bold, Size 45)
            GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(card.transform, false);
            RectTransform titleRt = titleGO.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 165);
            titleRt.sizeDelta = new Vector2(620, 60);
            TextMeshProUGUI titleText = titleGO.GetComponent<TextMeshProUGUI>();
            titleText.text = "<b>ACTIVITY COMPLETE!</b>";
            titleText.fontSize = 45;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = new Color(1f, 0.85f, 0.3f);

            // Subtitle (Bold, Size 30)
            GameObject subGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGO.transform.SetParent(card.transform, false);
            RectTransform subRt = subGO.GetComponent<RectTransform>();
            subRt.anchoredPosition = new Vector2(0, 115);
            subRt.sizeDelta = new Vector2(620, 45);
            TextMeshProUGUI subText = subGO.GetComponent<TextMeshProUGUI>();
            subText.text = "<b>Consonant + le Mastery</b>";
            subText.fontSize = 30;
            subText.fontStyle = FontStyles.Bold;
            subText.alignment = TextAlignmentOptions.Center;
            subText.color = new Color(0.75f, 0.9f, 1f);

            // Star layout container
            GameObject starContainer = new GameObject("StarContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            starContainer.transform.SetParent(card.transform, false);
            RectTransform scRt = starContainer.GetComponent<RectTransform>();
            scRt.anchoredPosition = new Vector2(0, 20);
            scRt.sizeDelta = new Vector2(400, 120);
            HorizontalLayoutGroup hlg = starContainer.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 18;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            for (int i = 1; i <= 3; i++)
            {
                GameObject starGO = new GameObject($"Star_{i}", typeof(RectTransform), typeof(Image));
                starGO.transform.SetParent(starContainer.transform, false);
                RectTransform sRt = starGO.GetComponent<RectTransform>();
                bool isCenter = (i == 2);
                float size = isCenter ? 110f : 90f;
                sRt.sizeDelta = new Vector2(size, size);
                if (isCenter) sRt.anchoredPosition = new Vector2(0, 10);

                Image sImg = starGO.GetComponent<Image>();
                bool isEarned = (i <= earnedStars);
                sImg.color = isEarned ? Color.white : new Color(0.4f, 0.4f, 0.4f, 0.8f);
                if (goldenStarSprite != null) sImg.sprite = goldenStarSprite;
            }

            // Score text
            GameObject scoreGO = new GameObject("ScoreText", typeof(RectTransform), typeof(TextMeshProUGUI));
            scoreGO.transform.SetParent(card.transform, false);
            RectTransform scoreRt = scoreGO.GetComponent<RectTransform>();
            scoreRt.anchoredPosition = new Vector2(0, -75);
            scoreRt.sizeDelta = new Vector2(500, 50);
            TextMeshProUGUI scoreTxt = scoreGO.GetComponent<TextMeshProUGUI>();
            scoreTxt.text = $"<b>Score: +{scoreEarned}</b>";
            scoreTxt.fontSize = 32;
            scoreTxt.fontStyle = FontStyles.Bold;
            scoreTxt.alignment = TextAlignmentOptions.Center;
            scoreTxt.color = new Color(0.2f, 0.9f, 0.5f);

            // Next Activity Button
            GameObject nextBtnGO = new GameObject("NextBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            nextBtnGO.transform.SetParent(card.transform, false);
            RectTransform nbRt = nextBtnGO.GetComponent<RectTransform>();
            nbRt.anchoredPosition = new Vector2(0, -165);
            nbRt.sizeDelta = new Vector2(340, 65);
            Image nbImg = nextBtnGO.GetComponent<Image>();
            U9_UI_Utils.ApplyRoundedButtonStyle(nbImg, new Color(0.12f, 0.65f, 0.35f, 1f));

            Button nbBtn = nextBtnGO.GetComponent<Button>();
            GameObject btnTxtGO = new GameObject("BtnText", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTxtGO.transform.SetParent(nextBtnGO.transform, false);
            RectTransform btRt = btnTxtGO.GetComponent<RectTransform>();
            btRt.sizeDelta = nbRt.sizeDelta;
            TextMeshProUGUI btText = btnTxtGO.GetComponent<TextMeshProUGUI>();
            btText.text = "<b>CONTINUE >></b>";
            btText.fontSize = 30;
            btText.fontStyle = FontStyles.Bold;
            btText.alignment = TextAlignmentOptions.Center;
            btText.color = Color.white;

            nbBtn.onClick.AddListener(() =>
            {
                Destroy(modalGO);
                onNextTapped?.Invoke();
            });
        }

        // =====================================================================
        // Helpers & Styling
        // =====================================================================

        public static void StyleProgressBarBlue(Slider slider)
        {
            if (slider == null) return;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            if (slider.fillRect != null)
            {
                var fillImg = slider.fillRect.GetComponent<Image>();
                if (fillImg != null) fillImg.color = new Color(0.08f, 0.62f, 0.98f); // #149EFA Electric Cyan-Blue

                slider.fillRect.offsetMin = Vector2.zero;
                slider.fillRect.offsetMax = Vector2.zero;
            }

            Transform bg = slider.transform.Find("Background");
            if (bg != null)
            {
                var bgImg = bg.GetComponent<Image>();
                if (bgImg != null) bgImg.color = new Color(0.06f, 0.09f, 0.16f); // Dark Navy
            }
        }

        private Button FindButton(Transform root, params string[] names)
        {
            if (root == null) return null;
            foreach (var n in names)
            {
                Transform t = root.Find(n);
                if (t != null)
                {
                    Button b = t.GetComponent<Button>();
                    if (b != null) return b;
                }
            }
            return null;
        }

#if UNITY_EDITOR
        [ContextMenu("Auto-Assign Hierarchy & Stars")]
        public void EditorAutoAssignHierarchyAndStars()
        {
            if (goldenStarSprite == null)
                goldenStarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png.png");
            if (emptyStarSprite == null)
                emptyStarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png (1).png");

            AutoBindHierarchyElements();
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(gameObject);
            Debug.Log("<color=#10B981><b>[Unit 9 Flow Manager] Auto-Assigned Hierarchy Panels & Star Sprites!</b></color>");
        }
#endif
    }
}
