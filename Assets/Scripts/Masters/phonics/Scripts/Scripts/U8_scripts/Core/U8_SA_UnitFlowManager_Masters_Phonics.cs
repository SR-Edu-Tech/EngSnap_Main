using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U8_SA_UnitFlowManager_Masters_Phonics : MonoBehaviour
    {
        public static U8_SA_UnitFlowManager_Masters_Phonics Instance { get; private set; }

        [Header("Unit Signboard & Panels Root")]
        [SerializeField] private GameObject selectionSignboardPanel;
        [SerializeField] private GameObject sectionsParent;
        [SerializeField] private GameObject learnPanel;
        [SerializeField] private GameObject activity1Panel;
        [SerializeField] private GameObject activity2Panel;
        [SerializeField] private GameObject activity3Panel;
        [SerializeField] private GameObject activity4Panel;
        [SerializeField] private GameObject activity5Panel;
        [SerializeField] private GameObject unitChallengePanel;
        [SerializeField] private GameObject mapUpdatePanel;
        [SerializeField] private GameObject completionReportPanel;

        [Header("Section Panels Array (0 to 8)")]
        public GameObject[] sectionPanels;

        [Header("Signboard Section Buttons (5 Wagons / Trucks)")]
        [SerializeField] private Button truck1Btn; // Wagon 1: Learn -> Act 1
        [SerializeField] private Button truck2Btn; // Wagon 2: Act 2
        [SerializeField] private Button truck3Btn; // Wagon 3: Act 3
        [SerializeField] private Button truck4Btn; // Wagon 4: Act 4
        [SerializeField] private Button truck5Btn; // Wagon 5: Act 5 / Challenge
        public Button[] allWagonButtons;

        [Header("Global Navigation")]
        [SerializeField] private Button globalBackBtn;
        [SerializeField] private Button nextActivityBtn;

        [Header("Star Rating Sprites (Completion Modal)")]
        public Sprite goldenStarSprite;
        public Sprite emptyStarSprite;

        [Header("Cumulative Progress & Stats")]
        public int totalUnitScore = 0;
        public int wordsReadCount = 0;
        public int wordsSpelledCount = 0;
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
                Transform p = root.Find("Unit_8_Section_Selection_Panels") 
                           ?? root.Find("Section_Selection_Panel") 
                           ?? root.Find("Section_Selection_Panels")
                           ?? root.Find("Signboard")
                           ?? root.Find("Unit_8_Section_Selection_Panel");
                if (p != null) selectionSignboardPanel = p.gameObject;
            }

            if (sectionsParent == null)
            {
                Transform sec = root.Find("Unit_8_Sections") 
                             ?? root.Find("Sections") 
                             ?? root.Find("Unit_8_Section_Panels");
                if (sec != null) sectionsParent = sec.gameObject;
            }

            Transform secRoot = sectionsParent != null ? sectionsParent.transform : root;

            if (learnPanel == null && secRoot != null)
            {
                Transform p = secRoot.Find("U8_learn_Concept_Cards_Panel") ?? secRoot.Find("Learn_ConceptCards") ?? secRoot.Find("Learn") ?? secRoot.Find("ConceptCards");
                if (p != null) learnPanel = p.gameObject;
                else
                {
                    var gm = GetComponentInChildren<U8_SA_GM01_ConceptCards_Masters_Phonics>(true);
                    if (gm != null) learnPanel = gm.gameObject;
                }
            }
            if (activity1Panel == null && secRoot != null)
            {
                Transform p = secRoot.Find("U8_Activity_1_Bossy_R") ?? secRoot.Find("Activity_1_BossyR") ?? secRoot.Find("Activity1") ?? secRoot.Find("Act1");
                if (p != null) activity1Panel = p.gameObject;
                else
                {
                    var gm = GetComponentInChildren<U8_SA_GM04_BossyR_Masters_Phonics>(true);
                    if (gm != null) activity1Panel = gm.gameObject;
                }
            }
            if (activity2Panel == null && secRoot != null)
            {
                Transform p = secRoot.Find("U8_Activity_2_Three_Sounds") ?? secRoot.Find("Activity_2_ThreeSounds") ?? secRoot.Find("Activity2") ?? secRoot.Find("Act2");
                if (p != null) activity2Panel = p.gameObject;
                else
                {
                    var gm = GetComponentInChildren<U8_SA_GM03_ThreeSounds_Masters_Phonics>(true);
                    if (gm != null) activity2Panel = gm.gameObject;
                }
            }
            if (activity3Panel == null && secRoot != null)
            {
                Transform p = secRoot.Find("U8_Activity_3_Five_Families") ?? secRoot.Find("Activity_3_FiveFamilies") ?? secRoot.Find("Activity3") ?? secRoot.Find("Act3");
                if (p != null) activity3Panel = p.gameObject;
                else
                {
                    var gm = GetComponentInChildren<U8_SA_GM03_FiveFamilies_Masters_Phonics>(true);
                    if (gm != null) activity3Panel = gm.gameObject;
                }
            }
            if (activity4Panel == null && secRoot != null)
            {
                Transform p = secRoot.Find("U8_Activity_4_The_Er_Rule") ?? secRoot.Find("Activity_4_ErRule") ?? secRoot.Find("Activity4") ?? secRoot.Find("Act4");
                if (p != null) activity4Panel = p.gameObject;
                else
                {
                    var gm = GetComponentInChildren<U8_SA_GM03s_ErRule_Masters_Phonics>(true);
                    if (gm != null) activity4Panel = gm.gameObject;
                }
            }
            if (activity5Panel == null && secRoot != null)
            {
                Transform p = secRoot.Find("U8_Activity_5_Spelling_Lab") ?? secRoot.Find("Activity_5_SpellingLab") ?? secRoot.Find("Activity5") ?? secRoot.Find("Act5");
                if (p != null) activity5Panel = p.gameObject;
                else
                {
                    var gm = GetComponentInChildren<U8_SA_GM05_SpellingLab_Masters_Phonics>(true);
                    if (gm != null) activity5Panel = gm.gameObject;
                }
            }
            if (unitChallengePanel == null && secRoot != null)
            {
                Transform p = secRoot.Find("U8_UnitChallenge_Panel") ?? secRoot.Find("Unit_Challenge") ?? secRoot.Find("Challenge") ?? secRoot.Find("UnitChallenge");
                if (p != null) unitChallengePanel = p.gameObject;
                else
                {
                    var gm = GetComponentInChildren<U8_SA_UnitChallenge_Masters_Phonics>(true);
                    if (gm != null) unitChallengePanel = gm.gameObject;
                }
            }
            if (mapUpdatePanel == null && secRoot != null)
            {
                Transform p = secRoot.Find("U8_SevenTypesMap_Panel") ?? secRoot.Find("Seven_Types_Map") ?? secRoot.Find("MapUpdate") ?? secRoot.Find("SyllableMap");
                if (p != null) mapUpdatePanel = p.gameObject;
                else
                {
                    var gm = GetComponentInChildren<U8_SA_SevenTypesMap_Masters_Phonics>(true);
                    if (gm != null) mapUpdatePanel = gm.gameObject;
                }
            }
            if (completionReportPanel == null && secRoot != null)
            {
                Transform p = secRoot.Find("U8_COMPLETE_Panel") ?? secRoot.Find("Completion_Report") ?? secRoot.Find("CompletionPanel") ?? secRoot.Find("Report");
                if (p != null) completionReportPanel = p.gameObject;
            }

            sectionPanels = new GameObject[]
            {
                learnPanel,
                activity1Panel,
                activity2Panel,
                activity3Panel,
                activity4Panel,
                activity5Panel,
                unitChallengePanel,
                mapUpdatePanel,
                completionReportPanel
            };

            // Bind Wagon / Truck buttons on signboard
            if (selectionSignboardPanel != null)
            {
                if (truck1Btn == null) truck1Btn = FindButton(selectionSignboardPanel.transform, "Wagon1", "Wagon_1", "Wagon 1", "Cart1", "Cart_1", "Truck1", "Section1_Btn", "Btn_1");
                if (truck2Btn == null) truck2Btn = FindButton(selectionSignboardPanel.transform, "Wagon2", "Wagon_2", "Wagon 2", "Cart2", "Cart_2", "Truck2", "Section2_Btn", "Btn_2");
                if (truck3Btn == null) truck3Btn = FindButton(selectionSignboardPanel.transform, "Wagon3", "Wagon_3", "Wagon 3", "Cart3", "Cart_3", "Truck3", "Section3_Btn", "Btn_3");
                if (truck4Btn == null) truck4Btn = FindButton(selectionSignboardPanel.transform, "Wagon4", "Wagon_4", "Wagon 4", "Cart4", "Cart_4", "Truck4", "Section4_Btn", "Btn_4");
                if (truck5Btn == null) truck5Btn = FindButton(selectionSignboardPanel.transform, "Wagon5", "Wagon_5", "Wagon 5", "Cart5", "Cart_5", "Truck5", "Section5_Btn", "Btn_5");

                // Fallback: search all buttons in signboard
                Button[] allBtns = selectionSignboardPanel.GetComponentsInChildren<Button>(true);
                List<Button> validWagons = new List<Button>();
                foreach (var b in allBtns)
                {
                    if (b == null) continue;
                    string n = b.gameObject.name.ToLower();
                    if (n.Contains("back") || n.Contains("home") || n.Contains("replay") || n.Contains("speaker") || n.Contains("audio")) continue;
                    validWagons.Add(b);
                }
                allWagonButtons = validWagons.ToArray();

                if (truck1Btn == null && validWagons.Count > 0) truck1Btn = validWagons[0];
                if (truck2Btn == null && validWagons.Count > 1) truck2Btn = validWagons[1];
                if (truck3Btn == null && validWagons.Count > 2) truck3Btn = validWagons[2];
                if (truck4Btn == null && validWagons.Count > 3) truck4Btn = validWagons[3];
                if (truck5Btn == null && validWagons.Count > 4) truck5Btn = validWagons[4];
            }

            AttachTruckListeners();

            // Bind Global Next & Back Buttons (ONLY at Sections level or Root level, never inside child panels)
            if (globalBackBtn == null)
            {
                globalBackBtn = FindMainButton(sectionsParent != null ? sectionsParent.transform : null, "Back_Button main", "Back_Button", "BackButton", "Back Button", "GlobalBackBtn")
                             ?? FindMainButton(root, "Back_Button main", "Back_Button", "BackButton", "Back Button", "GlobalBackBtn");
            }
            if (globalBackBtn != null)
            {
                globalBackBtn.gameObject.SetActive(true);
                globalBackBtn.onClick.RemoveAllListeners();
                globalBackBtn.onClick.AddListener(OnGlobalBackTapped);
            }

            // SAFETY: If nextActivityBtn was serialized to a button INSIDE a section panel
            // (e.g., the ConceptCards' NextBtn), it collides — HideNextActivityButton()
            // would disable the card navigation button. Detect and clear this collision.
            if (nextActivityBtn != null)
            {
                GameObject[] panels = { learnPanel, activity1Panel, activity2Panel, activity3Panel, activity4Panel, activity5Panel, unitChallengePanel, mapUpdatePanel, completionReportPanel };
                foreach (var panel in panels)
                {
                    if (panel != null && nextActivityBtn.transform.IsChildOf(panel.transform))
                    {
                        Debug.LogWarning($"[UnitFlowManager] nextActivityBtn '{nextActivityBtn.name}' is INSIDE panel '{panel.name}' — clearing to avoid collision with internal nav buttons.");
                        nextActivityBtn = null;
                        break;
                    }
                }
            }

            if (nextActivityBtn == null)
            {
                nextActivityBtn = FindMainButton(sectionsParent != null ? sectionsParent.transform : null, "Next_Button main", "Next_Button", "NextButton", "Next Button", "NextActivity_Button", "GlobalNextBtn")
                               ?? FindMainButton(root, "Next_Button main", "Next_Button", "NextButton", "Next Button", "NextActivity_Button", "GlobalNextBtn");
            }

            // If still null, create a standalone global Next button at the sectionsParent level
            if (nextActivityBtn == null)
            {
                Transform btnParent = sectionsParent != null ? sectionsParent.transform : root;
                GameObject nextGO = new GameObject("Next_Button main", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                nextGO.transform.SetParent(btnParent, false);
                RectTransform nrt = nextGO.GetComponent<RectTransform>();
                nrt.anchorMin = new Vector2(1f, 0f);
                nrt.anchorMax = new Vector2(1f, 0f);
                nrt.pivot = new Vector2(1f, 0f);
                nrt.anchoredPosition = new Vector2(-30f, 30f);
                nrt.sizeDelta = new Vector2(200f, 80f);

                Image nImg = nextGO.GetComponent<Image>();
                nImg.color = new Color(0.12f, 0.72f, 0.35f, 1f); // Green

                // Add label
                GameObject lblGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                lblGO.transform.SetParent(nextGO.transform, false);
                RectTransform lblRT = lblGO.GetComponent<RectTransform>();
                lblRT.anchorMin = Vector2.zero;
                lblRT.anchorMax = Vector2.one;
                lblRT.offsetMin = Vector2.zero;
                lblRT.offsetMax = Vector2.zero;
                TextMeshProUGUI lbl = lblGO.GetComponent<TextMeshProUGUI>();
                lbl.text = "<b>NEXT >></b>";
                lbl.fontSize = 28f;
                lbl.alignment = TextAlignmentOptions.Center;
                lbl.color = Color.white;

                nextActivityBtn = nextGO.GetComponent<Button>();
                nextGO.SetActive(false); // Start hidden; shown by ShowNextActivityButton()
                Debug.Log("[UnitFlowManager] Created standalone global Next button 'Next_Button main'.");
            }

            if (nextActivityBtn != null)
            {
                nextActivityBtn.onClick.RemoveAllListeners();
                nextActivityBtn.onClick.AddListener(AdvanceNextSection);
            }

#if UNITY_EDITOR
            if (goldenStarSprite == null)
                goldenStarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png.png");
            if (emptyStarSprite == null)
                emptyStarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png (1).png");
#endif
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

        private Button FindButton(Transform parent, params string[] names)
        {
            if (parent == null) return null;

            foreach (var n in names)
            {
                Transform t = parent.Find(n) ?? parent.Find($"Buttons/{n}") ?? parent.Find($"Trucks/{n}") ?? parent.Find($"Wagons/{n}") ?? parent.Find($"Carts/{n}");
                if (t != null)
                {
                    Button b = t.GetComponent<Button>();
                    if (b != null) return b;
                }
            }

            foreach (var b in parent.GetComponentsInChildren<Button>(true))
            {
                foreach (var n in names)
                {
                    if (b.gameObject.name.Equals(n, StringComparison.OrdinalIgnoreCase))
                        return b;
                }
            }
            return null;
        }

        private void AttachTruckListeners()
        {
            if (truck1Btn != null && truck1Btn.onClick.GetPersistentEventCount() == 0)
            {
                truck1Btn.onClick.RemoveAllListeners();
                truck1Btn.onClick.AddListener(() => {
                    U8_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("click");
                    OpenLearn();
                });
            }
            if (truck2Btn != null && truck2Btn.onClick.GetPersistentEventCount() == 0)
            {
                truck2Btn.onClick.RemoveAllListeners();
                truck2Btn.onClick.AddListener(() => {
                    U8_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("click");
                    OpenActivity2();
                });
            }
            if (truck3Btn != null && truck3Btn.onClick.GetPersistentEventCount() == 0)
            {
                truck3Btn.onClick.RemoveAllListeners();
                truck3Btn.onClick.AddListener(() => {
                    U8_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("click");
                    OpenActivity3();
                });
            }
            if (truck4Btn != null && truck4Btn.onClick.GetPersistentEventCount() == 0)
            {
                truck4Btn.onClick.RemoveAllListeners();
                truck4Btn.onClick.AddListener(() => {
                    U8_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("click");
                    OpenActivity4();
                });
            }
            if (truck5Btn != null && truck5Btn.onClick.GetPersistentEventCount() == 0)
            {
                truck5Btn.onClick.RemoveAllListeners();
                truck5Btn.onClick.AddListener(() => {
                    U8_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("click");
                    OpenActivity5();
                });
            }
        }

        // =====================================================================
        // Navigation & Section Opening
        // =====================================================================

        private void HideAllPanels()
        {
            if (selectionSignboardPanel != null) selectionSignboardPanel.SetActive(false);
            if (learnPanel != null) learnPanel.SetActive(false);
            if (activity1Panel != null) activity1Panel.SetActive(false);
            if (activity2Panel != null) activity2Panel.SetActive(false);
            if (activity3Panel != null) activity3Panel.SetActive(false);
            if (activity4Panel != null) activity4Panel.SetActive(false);
            if (activity5Panel != null) activity5Panel.SetActive(false);
            if (unitChallengePanel != null) unitChallengePanel.SetActive(false);
            if (mapUpdatePanel != null) mapUpdatePanel.SetActive(false);
            if (completionReportPanel != null) completionReportPanel.SetActive(false);

            if (sectionsParent != null)
            {
                foreach (Transform child in sectionsParent.transform)
                {
                    if (child == null) continue;
                    string n = child.name.ToLower();
                    // Never disable background, global back button, or global next button
                    if (n.Contains("bg") || n.Contains("background") || n.Contains("back") || n.Contains("next"))
                    {
                        continue;
                    }
                    child.gameObject.SetActive(false);
                }
            }

            U8_SA_AudioManager_Masters_Phonics.Instance?.StopAllSpeech();
        }

        private void ShowSingleSectionPanel(GameObject targetPanel)
        {
            HideAllPanels();

            if (selectionSignboardPanel != null)
            {
                selectionSignboardPanel.SetActive(false);
            }

            if (sectionsParent != null)
            {
                sectionsParent.SetActive(true);
                Transform bgTr = sectionsParent.transform.Find("BG_Image") 
                              ?? sectionsParent.transform.Find("Background") 
                              ?? sectionsParent.transform.Find("BG");
                if (bgTr != null)
                {
                    bgTr.gameObject.SetActive(true);
                    bgTr.SetAsFirstSibling();
                }
            }

            if (targetPanel != null)
            {
                targetPanel.SetActive(true);
                Transform cur = targetPanel.transform.parent;
                while (cur != null && cur != transform)
                {
                    cur.gameObject.SetActive(true);
                    cur = cur.parent;
                }
                targetPanel.transform.SetAsLastSibling();
            }

            // Ensure Back and Next Buttons render on top of the active activity panel
            if (globalBackBtn != null)
            {
                globalBackBtn.gameObject.SetActive(true);
                globalBackBtn.transform.SetAsLastSibling();
            }
            if (nextActivityBtn != null)
            {
                nextActivityBtn.transform.SetAsLastSibling();
            }
        }

        public void OpenSignboard()
        {
            HideAllPanels();
            currentActiveSection = 0;

            if (sectionsParent != null)
            {
                sectionsParent.SetActive(false);
            }

            if (selectionSignboardPanel != null)
            {
                selectionSignboardPanel.SetActive(true);
                selectionSignboardPanel.transform.SetAsLastSibling();
                foreach (Transform child in selectionSignboardPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null) child.gameObject.SetActive(true);
                }
            }

            AudioClip introClip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_unit_intro");
            if (introClip != null)
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(introClip);
        }

        public void OpenSectionSelection() => OpenSignboard();

        public void OpenLearn()
        {
            currentActiveSection = 100;
            ShowSingleSectionPanel(learnPanel);
        }

        public void OpenActivity1()
        {
            currentActiveSection = 1;
            ShowSingleSectionPanel(activity1Panel);
        }

        public void OpenActivity2()
        {
            currentActiveSection = 2;
            ShowSingleSectionPanel(activity2Panel);
        }

        public void OpenActivity3()
        {
            currentActiveSection = 3;
            ShowSingleSectionPanel(activity3Panel);
        }

        public void OpenActivity4()
        {
            currentActiveSection = 4;
            ShowSingleSectionPanel(activity4Panel);
        }

        public void OpenActivity5()
        {
            currentActiveSection = 5;
            ShowSingleSectionPanel(activity5Panel);
        }

        public void OpenUnitChallenge()
        {
            currentActiveSection = 6;
            ShowSingleSectionPanel(unitChallengePanel);
        }

        public void OpenMapUpdate()
        {
            currentActiveSection = 7;
            ShowSingleSectionPanel(mapUpdatePanel);
        }

        public void OpenCompletionReport()
        {
            currentActiveSection = 8;
            EnsureCompletionReportUI();
            ShowSingleSectionPanel(completionReportPanel);

            U8_SA_AudioManager_Masters_Phonics.Instance?.PlayCelebration();
            AudioClip compClip = U8_SA_AudioManager_Masters_Phonics.ResolveAudio("U08_VO_unit_complete");
            if (compClip != null)
            {
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(compClip);
            }
        }

        public void EnsureCompletionReportUI()
        {
            if (completionReportPanel == null)
            {
                Transform secRoot = sectionsParent != null ? sectionsParent.transform : transform;
                Transform p = secRoot.Find("U8_COMPLETE_Panel") ?? secRoot.Find("Completion_Report") ?? secRoot.Find("CompletionPanel") ?? secRoot.Find("Report");
                if (p != null)
                {
                    completionReportPanel = p.gameObject;
                }
                else
                {
                    completionReportPanel = new GameObject("U8_COMPLETE_Panel", typeof(RectTransform));
                    completionReportPanel.transform.SetParent(secRoot, false);
                    RectTransform prt = completionReportPanel.GetComponent<RectTransform>();
                    prt.anchorMin = Vector2.zero;
                    prt.anchorMax = Vector2.one;
                    prt.offsetMin = Vector2.zero;
                    prt.offsetMax = Vector2.zero;
                }
            }

            Transform panelTr = completionReportPanel.transform;

            // Check / Create ReportCard container
            Transform card = panelTr.Find("ReportCard");
            if (card == null)
            {
                GameObject cardGo = new GameObject("ReportCard", typeof(RectTransform), typeof(Image));
                cardGo.transform.SetParent(panelTr, false);
                RectTransform crt = cardGo.GetComponent<RectTransform>();
                crt.anchoredPosition = new Vector2(0f, -10f);
                crt.sizeDelta = new Vector2(880f, 560f);

                Image cImg = cardGo.GetComponent<Image>();
                cImg.sprite = GetOrCreateRoundedSprite(128, 28);
                cImg.type = Image.Type.Sliced;
                cImg.color = new Color(0.08f, 0.14f, 0.28f, 0.98f);

                card = cardGo.transform;
            }

            // Dynamic Stats Text (Final Score, Words, Stars, Badge) - updates only if present or finds in card
            Transform statsTr = card.Find("StatsText") ?? card.Find("ScoreText") ?? card.Find("FinalScoreText");
            if (statsTr == null)
            {
                GameObject sGo = new GameObject("StatsText", typeof(RectTransform), typeof(TextMeshProUGUI));
                sGo.transform.SetParent(card, false);
                RectTransform srt = sGo.GetComponent<RectTransform>();
                srt.anchoredPosition = new Vector2(0f, -40f);
                srt.sizeDelta = new Vector2(800f, 110f);
                statsTr = sGo.transform;
            }
            TextMeshProUGUI statsTMP = statsTr.GetComponent<TextMeshProUGUI>();
            if (statsTMP != null)
            {
                int totalStars = 0;
                foreach (var kv in activityStars) totalStars += kv.Value;

                statsTMP.text = $"<b>Total Unit Score: <color=#FBBF24>{totalUnitScore} pts</color></b>\n" +
                               $"<b>Words Read: {Mathf.Max(60, wordsReadCount)}  |  Words Spelled: {Mathf.Max(15, wordsSpelledCount)}</b>\n" +
                               $"<color=#22C55E><b>Badge Unlocked: R-Wrangler Master</b></color>";
                statsTMP.fontSize = 28;
                statsTMP.alignment = TextAlignmentOptions.Center;
                statsTMP.lineSpacing = 15f;
            }

            // Continue / Return Button
            Transform contBtnTr = card.Find("DoneBtn") ?? card.Find("ContinueButton") ?? card.Find("ContinueBtn") ?? card.Find("NextBtn");
            if (contBtnTr == null)
            {
                GameObject btnGo = new GameObject("DoneBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(card, false);
                RectTransform brt = btnGo.GetComponent<RectTransform>();
                brt.anchoredPosition = new Vector2(0f, -190f);
                brt.sizeDelta = new Vector2(460f, 75f);

                Image btnImg = btnGo.GetComponent<Image>();
                btnImg.sprite = GetOrCreateRoundedSprite(128, 18);
                btnImg.type = Image.Type.Sliced;
                btnImg.color = new Color(0.1f, 0.72f, 0.42f, 1f);

                GameObject lbl = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                lbl.transform.SetParent(btnGo.transform, false);
                RectTransform lrt = lbl.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;

                TextMeshProUGUI ltmp = lbl.GetComponent<TextMeshProUGUI>();
                ltmp.text = "<b>RETURN TO LESSONS >></b>";
                ltmp.fontSize = 28;
                ltmp.color = Color.white;
                ltmp.alignment = TextAlignmentOptions.Center;

                contBtnTr = btnGo.transform;
            }

            // Wire all buttons on completion panel
            Button[] cBtns = completionReportPanel.GetComponentsInChildren<Button>(true);
            foreach (var b in cBtns)
            {
                if (b == null) continue;
                string bName = b.name.ToLower();
                b.onClick.RemoveAllListeners();
                if (bName.Contains("signboard") || bName.Contains("select"))
                {
                    b.onClick.AddListener(() => {
                        U8_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("click");
                        OpenSignboard();
                    });
                }
                else
                {
                    b.onClick.AddListener(ReturnToLessonsMenu);
                }
            }
        }

        public void ReturnToLessonsMenu()
        {
            U8_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("click");
            U8_SA_AudioManager_Masters_Phonics.Instance?.StopAllSpeech();

            gameObject.SetActive(false);

            if (Unit_Selection_Panel_Masters_Phonics.Instance != null)
            {
                Unit_Selection_Panel_Masters_Phonics.Instance.gameObject.SetActive(true);
                Unit_Selection_Panel_Masters_Phonics.Instance.BackToUnitSelection();
            }
            else
            {
                var mainSelect = FindObjectOfType<Unit_Selection_Panel_Masters_Phonics>(true);
                if (mainSelect != null)
                {
                    mainSelect.gameObject.SetActive(true);
                    mainSelect.BackToUnitSelection();
                }
                else
                {
                    OpenSignboard();
                }
            }
        }

        public void OpenSection(int index)
        {
            switch (index)
            {
                case 0:
                case 100:
                    OpenLearn();
                    break;
                case 1:
                    OpenActivity1();
                    break;
                case 2:
                    OpenActivity2();
                    break;
                case 3:
                    OpenActivity3();
                    break;
                case 4:
                    OpenActivity4();
                    break;
                case 5:
                    OpenActivity5();
                    break;
                case 6:
                    OpenUnitChallenge();
                    break;
                case 7:
                    OpenMapUpdate();
                    break;
                case 8:
                    OpenCompletionReport();
                    break;
                default:
                    OpenSignboard();
                    break;
            }
        }

        public void SetNextButtonVisible(bool visible)
        {
            if (nextActivityBtn != null)
            {
                nextActivityBtn.gameObject.SetActive(visible);
                if (visible)
                {
                    nextActivityBtn.transform.SetAsLastSibling();
                    nextActivityBtn.onClick.RemoveListener(AdvanceNextSection);
                    nextActivityBtn.onClick.AddListener(AdvanceNextSection);
                }
            }
        }

        public void ShowNextActivityButton() => SetNextButtonVisible(true);
        public void HideNextActivityButton() => SetNextButtonVisible(false);

        public void AdvanceNextSection()
        {
            SetNextButtonVisible(false);
            if (currentActiveSection == 100) // Learn (Concept Cards)
            {
                var gm01 = learnPanel != null ? learnPanel.GetComponentInChildren<U8_SA_GM01_ConceptCards_Masters_Phonics>(true) : null;
                if (gm01 != null && gm01.GetCurrentCardIndex() < 4)
                {
                    gm01.OnNextCardTapped();
                    return;
                }
            }

            if (nextActivityBtn != null) nextActivityBtn.gameObject.SetActive(false);
            switch (currentActiveSection)
            {
                case 100: OpenActivity1(); break;
                case 1: OpenActivity2(); break;
                case 2: OpenActivity3(); break;
                case 3: OpenActivity4(); break;
                case 4: OpenActivity5(); break;
                case 5: OpenUnitChallenge(); break;
                case 6: OpenMapUpdate(); break;
                case 7: OpenCompletionReport(); break;
                default: OpenSignboard(); break;
            }
        }

        public void OnGlobalBackTapped()
        {
            if (currentActiveSection != 0)
            {
                OpenSignboard();
            }
            else
            {
                // Return to Main Unit Selection
                ReturnToLessonsMenu();
            }
        }

        // =====================================================================
        // Scoring & Statistics Tracking
        // =====================================================================

        public void AddScore(int points)
        {
            totalUnitScore += points;
        }

        public void RecordWordsRead(int count = 1)
        {
            wordsReadCount += count;
        }

        public void RecordWordsSpelled(int count = 1)
        {
            wordsSpelledCount += count;
        }

        public void RecordSectionCompleted(int sectionIndex, int stars)
        {
            activityStars[sectionIndex] = Mathf.Max(activityStars.ContainsKey(sectionIndex) ? activityStars[sectionIndex] : 0, stars);
        }

        public void RecordMistake(string word)
        {
            if (!string.IsNullOrEmpty(word) && !mistakeQueue.Contains(word))
            {
                mistakeQueue.Add(word);
            }
        }

        // =====================================================================
        // Standardized Graphical Star Activity Completion Modal
        // =====================================================================

        private static Sprite proceduralRoundedSprite;
        public static Sprite GetOrCreateRoundedSprite(int size = 128, int radius = 24)
        {
            if (proceduralRoundedSprite != null) return proceduralRoundedSprite;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] colors = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int dx = Mathf.Max(0, Mathf.Max(radius - x, x - (size - 1 - radius)));
                    int dy = Mathf.Max(0, Mathf.Max(radius - y, y - (size - 1 - radius)));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    if (dist <= radius - 1.5f)
                    {
                        colors[y * size + x] = Color.white;
                    }
                    else if (dist <= radius)
                    {
                        float alpha = Mathf.Clamp01(radius - dist);
                        colors[y * size + x] = new Color(1f, 1f, 1f, alpha);
                    }
                    else
                    {
                        colors[y * size + x] = Color.clear;
                    }
                }
            }
            tex.SetPixels(colors);
            tex.Apply();

            proceduralRoundedSprite = Sprite.Create(
                tex,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius)
            );
            return proceduralRoundedSprite;
        }

        public static GameObject ShowActivityCompletionDialog(Transform parent, string activityTitle, int starsEarned, int scoreEarned, Action onNextTapped)
        {
            if (parent == null) return null;

            Transform existing = parent.Find("ActivityCompletionModal");
            if (existing != null) Destroy(existing.gameObject);

            GameObject modal = new GameObject("ActivityCompletionModal", typeof(RectTransform), typeof(Image));
            modal.transform.SetParent(parent, false);
            modal.transform.SetAsLastSibling();

            RectTransform mrt = modal.GetComponent<RectTransform>();
            mrt.anchorMin = Vector2.zero;
            mrt.anchorMax = Vector2.one;
            mrt.offsetMin = Vector2.zero;
            mrt.offsetMax = Vector2.zero;

            Image mImg = modal.GetComponent<Image>();
            mImg.color = new Color(0.04f, 0.07f, 0.14f, 0.94f);

            GameObject win = new GameObject("DialogWindow", typeof(RectTransform), typeof(Image));
            win.transform.SetParent(modal.transform, false);
            RectTransform wrt = win.GetComponent<RectTransform>();
            wrt.anchorMin = new Vector2(0.5f, 0.5f);
            wrt.anchorMax = new Vector2(0.5f, 0.5f);
            wrt.anchoredPosition = Vector2.zero;
            wrt.sizeDelta = new Vector2(860f, 520f);

            Image wImg = win.GetComponent<Image>();
            wImg.sprite = GetOrCreateRoundedSprite(128, 28);
            wImg.type = Image.Type.Sliced;
            wImg.color = new Color(0.08f, 0.14f, 0.28f, 0.98f);

            Outline wo = win.AddComponent<Outline>();
            wo.effectColor = new Color(0.22f, 0.72f, 0.98f, 0.85f);
            wo.effectDistance = new Vector2(4, -4);

            // Activity Header Title
            GameObject tObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            tObj.transform.SetParent(win.transform, false);
            RectTransform trt = tObj.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0f, 185f);
            trt.sizeDelta = new Vector2(800f, 75f);
            var ttmp = tObj.GetComponent<TextMeshProUGUI>();
            ttmp.text = $"<b><color=#38BDF8>{activityTitle.ToUpper()}</color></b>\n<size=75%><color=#FBBF24>ACTIVITY CLEARED!</color></size>";
            ttmp.fontSize = 36;
            ttmp.alignment = TextAlignmentOptions.Center;

            // Load star sprites from Instance or Editor AssetDatabase
            Sprite goldStar = Instance != null ? Instance.goldenStarSprite : null;
            Sprite greyStar = Instance != null ? Instance.emptyStarSprite : null;
#if UNITY_EDITOR
            if (goldStar == null)
                goldStar = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png.png");
            if (greyStar == null)
                greyStar = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png (1).png");
#endif

            // Star Rating Container (3 Stars with center star elevated for game trophy aesthetic)
            GameObject starsContainer = new GameObject("StarsContainer", typeof(RectTransform));
            starsContainer.transform.SetParent(win.transform, false);
            RectTransform scRt = starsContainer.GetComponent<RectTransform>();
            scRt.anchoredPosition = new Vector2(0f, 65f);
            scRt.sizeDelta = new Vector2(480f, 135f);

            float[] starXPositions = new float[] { -135f, 0f, 135f };
            float[] starYOffsets = new float[] { -6f, 12f, -6f };
            float[] starSizes = new float[] { 110f, 130f, 110f };

            for (int i = 0; i < 3; i++)
            {
                bool isGold = (i < starsEarned);
                GameObject starGo = new GameObject($"Star_{i + 1}", typeof(RectTransform), typeof(Image));
                starGo.transform.SetParent(starsContainer.transform, false);
                RectTransform srt = starGo.GetComponent<RectTransform>();
                srt.anchoredPosition = new Vector2(starXPositions[i], starYOffsets[i]);
                srt.sizeDelta = new Vector2(starSizes[i], starSizes[i]);

                Image sImg = starGo.GetComponent<Image>();
                sImg.sprite = isGold ? goldStar : greyStar;
                sImg.preserveAspect = true;
                if (sImg.sprite == null)
                {
                    sImg.color = isGold ? new Color(1f, 0.8f, 0.1f) : new Color(0.4f, 0.45f, 0.55f);
                }
            }

            // Score Earned Badge
            GameObject sObj = new GameObject("ScoreBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
            sObj.transform.SetParent(win.transform, false);
            RectTransform scoreRt = sObj.GetComponent<RectTransform>();
            scoreRt.anchoredPosition = new Vector2(0f, -40f);
            scoreRt.sizeDelta = new Vector2(760f, 55f);
            var stmp = sObj.GetComponent<TextMeshProUGUI>();
            stmp.text = $"<color=#BAE6FD>Score Earned:</color> <b><color=#FBBF24>+{scoreEarned} pts</color></b>";
            stmp.fontSize = 32;
            stmp.alignment = TextAlignmentOptions.Center;

            // Next Activity Button
            GameObject nextBtnObj = new GameObject("NextActivityBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            nextBtnObj.transform.SetParent(win.transform, false);
            RectTransform nrt = nextBtnObj.GetComponent<RectTransform>();
            nrt.anchoredPosition = new Vector2(0f, -170f);
            nrt.sizeDelta = new Vector2(460f, 80f);

            Image nImg = nextBtnObj.GetComponent<Image>();
            nImg.sprite = GetOrCreateRoundedSprite(128, 18);
            nImg.type = Image.Type.Sliced;
            nImg.color = new Color(0.1f, 0.72f, 0.42f, 1f);

            GameObject ntxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            ntxt.transform.SetParent(nextBtnObj.transform, false);
            RectTransform ntxtRt = ntxt.GetComponent<RectTransform>();
            ntxtRt.anchorMin = Vector2.zero;
            ntxtRt.anchorMax = Vector2.one;
            ntxtRt.offsetMin = Vector2.zero;
            ntxtRt.offsetMax = Vector2.zero;

            var ntmp = ntxt.GetComponent<TextMeshProUGUI>();
            ntmp.text = "<b>NEXT ACTIVITY  >></b>";
            ntmp.fontSize = 30;
            ntmp.color = Color.white;
            ntmp.alignment = TextAlignmentOptions.Center;

            Button nb = nextBtnObj.GetComponent<Button>();
            nb.onClick.AddListener(() => {
                U8_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("click");
                Destroy(modal);
                onNextTapped?.Invoke();
            });

            U8_SA_AudioManager_Masters_Phonics.Instance?.PlayCelebration();

            return modal;
        }

        public static void StyleProgressBarBlue(Slider progressBar)
        {
            if (progressBar == null) return;

            // Zero out Fill Area and Fill rect offsets so full progress fills the entire bar without visual gaps
            Transform fillAreaParent = progressBar.transform.Find("Fill Area");
            if (fillAreaParent != null && fillAreaParent is RectTransform faRt)
            {
                faRt.anchorMin = Vector2.zero;
                faRt.anchorMax = Vector2.one;
                faRt.offsetMin = Vector2.zero;
                faRt.offsetMax = Vector2.zero;
            }

            if (progressBar.fillRect != null)
            {
                progressBar.fillRect.offsetMin = Vector2.zero;
                progressBar.fillRect.offsetMax = Vector2.zero;
            }

            Transform fillArea = progressBar.transform.Find("Fill Area/Fill") ?? progressBar.transform.Find("Fill");
            if (fillArea != null)
            {
                Image fillImg = fillArea.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.color = new Color(0.08f, 0.62f, 0.98f, 1f); // Electric Blue #149EFA
                }
                if (fillArea is RectTransform fRt)
                {
                    fRt.offsetMin = Vector2.zero;
                    fRt.offsetMax = Vector2.zero;
                }
            }
            Transform bg = progressBar.transform.Find("Background");
            if (bg != null)
            {
                Image bgImg = bg.GetComponent<Image>();
                if (bgImg != null)
                {
                    bgImg.color = new Color(0.06f, 0.09f, 0.16f, 1f);
                }
            }
        }
    }
}
