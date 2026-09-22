using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U7_SA_UnitFlowManager_Masters_Phonics : MonoBehaviour
    {
        public static U7_SA_UnitFlowManager_Masters_Phonics Instance { get; private set; }

        [Header("Unit Root & Containers")]
        [SerializeField] private GameObject unit7Parent;
        [SerializeField] private GameObject sectionSelectionPanel;
        [SerializeField] private GameObject sectionsParent;

        [Header("Explicit Panel References")]
        [SerializeField] private GameObject learnConceptCardsPanel;
        [SerializeField] private GameObject activity1TeamUpPanel;
        [SerializeField] private GameObject activity2WordFamiliesPanel;
        [SerializeField] private GameObject activity3SameSoundPanel;
        [SerializeField] private GameObject activity4GlideOrHoldPanel;
        [SerializeField] private GameObject activity5GlideFamiliesPanel;
        [SerializeField] private GameObject activity6TeamRushPanel;
        [SerializeField] private GameObject sevenTypesMapPanel;
        [SerializeField] private GameObject unitChallengePanel;
        [SerializeField] private GameObject completionPanel;

        [Header("Section Panels Array (0 to 9)")]
        // 0: Learn (Concept Cards)
        // 1: Activity 1 (Team Up - GM-04 Picture Gap Fill)
        // 2: Activity 2 (Word Families - GM-03 4-Bin Sort + Open Round)
        // 3: Activity 3 (Same Sound - GM-03 3-Bin Sound Sort)
        // 4: Activity 4 (Glide or Hold? - GM-04 Waveform Listen & Choose)
        // 5: Activity 5 (Glide Families - GM-03s Fast Positional Swipe)
        // 6: Activity 6 (Team Rush - GM-08 60s Speed Round Bonus)
        // 7: Seven Types Map (Section 7)
        // 8: Unit Challenge (Section 8)
        // 9: Completion Panel (Section 9)
        public GameObject[] sectionPanels;

        [Header("Unit Narration Audio")]
        public AudioClip unitIntroClip;
        public AudioClip partOneCompleteVoiceClip;
        public AudioClip unitCompleteVoiceClip;

        [Header("Signboard Section Buttons (5 Trucks)")]
        public Button[] sectionButtons;

        [Header("Cumulative Score & Metrics")]
        public int totalUnitScore = 0;
        public int totalWordsLearned = 0;
        public int totalGlidesIdentified = 0;
        public int currentSectionIndex = -1;

        public void CompleteCurrentActivity() => AdvanceNextSection();
        public void ReturnToLessonsMenu() => BackToLessonsMenu();

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

            EnsureComponentBindings();
        }

        private void Start()
        {
            WireSignboardButtons();
            WireGlobalBackButtons();
        }

        [Header("Background Sprites")]
        public Sprite part1SpellingBGSprite;
        public Sprite part2SoundBGSprite;
        [SerializeField] private Image bgImageComponent;

        [Header("Star Rating Sprites (Completion Modal)")]
        public Sprite goldenStarSprite;
        public Sprite emptyStarSprite;

        public void EnsureComponentBindings()
        {
            if (unit7Parent == null) unit7Parent = gameObject;

            if (sectionSelectionPanel == null)
            {
                Transform sel = transform.Find("Unit_7_Section_Selection_Panels") 
                             ?? transform.Find("Section_Selection_Panels")
                             ?? transform.Find("Unit_7_Section_Selection_Panel")
                             ?? transform.Find("SectionSelectionPanel");
                if (sel != null) sectionSelectionPanel = sel.gameObject;
            }

            if (sectionsParent == null)
            {
                Transform sec = transform.Find("Unit_7_Sections") 
                             ?? transform.Find("Sections") 
                             ?? transform.Find("Unit_7_Section_Panels");
                if (sec != null) sectionsParent = sec.gameObject;
            }

            Transform container = sectionsParent != null ? sectionsParent.transform : transform;

            // Find or bind BG image
            if (bgImageComponent == null && container != null)
            {
                Transform bgTr = container.Find("BG_Image") ?? container.Find("Background") ?? container.Find("BG");
                if (bgTr != null) bgImageComponent = bgTr.GetComponent<Image>();
            }

            // Auto-load backgrounds & star sprites if missing in editor
#if UNITY_EDITOR
            if (part1SpellingBGSprite == null)
                part1SpellingBGSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Unit7_MP/U07_BG_Part1_Spelling.jpg");
            if (part2SoundBGSprite == null)
                part2SoundBGSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Unit7_MP/U07_BG_Part2_Sound.jpg");
            if (goldenStarSprite == null)
                goldenStarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png.png");
            if (emptyStarSprite == null)
                emptyStarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png (1).png");
#endif

            // Auto-find panels if unassigned
            if (learnConceptCardsPanel == null)
                learnConceptCardsPanel = container.Find("U7_learn_Concept_Cards_Panel")?.gameObject ?? container.GetComponentInChildren<U7_SA_GM01_ConceptCards_Masters_Phonics>(true)?.gameObject;
            if (activity1TeamUpPanel == null)
                activity1TeamUpPanel = container.Find("U7_Activity_1_Team_Up")?.gameObject ?? container.GetComponentInChildren<U7_SA_GM04_TeamUp_Masters_Phonics>(true)?.gameObject;
            if (activity2WordFamiliesPanel == null)
                activity2WordFamiliesPanel = container.Find("U7_Activity_2_Word_Families")?.gameObject ?? container.GetComponentInChildren<U7_SA_GM03_WordFamilies_Masters_Phonics>(true)?.gameObject;
            if (activity3SameSoundPanel == null)
                activity3SameSoundPanel = container.Find("U7_Activity_3_Same_Sound")?.gameObject ?? container.GetComponentInChildren<U7_SA_GM03_SameSound_Masters_Phonics>(true)?.gameObject;
            if (activity4GlideOrHoldPanel == null)
                activity4GlideOrHoldPanel = container.Find("U7_Activity_4_Glide_Or_Hold")?.gameObject ?? container.GetComponentInChildren<U7_SA_GM04_GlideOrHold_Masters_Phonics>(true)?.gameObject;
            if (activity5GlideFamiliesPanel == null)
                activity5GlideFamiliesPanel = container.Find("U7_Activity_5_Glide_Families")?.gameObject ?? container.GetComponentInChildren<U7_SA_GM03s_GlideFamilies_Masters_Phonics>(true)?.gameObject;
            if (activity6TeamRushPanel == null)
                activity6TeamRushPanel = container.Find("U7_Activity_6_Team_Rush")?.gameObject ?? container.GetComponentInChildren<U7_SA_GM08_TeamRush_Masters_Phonics>(true)?.gameObject;
            if (sevenTypesMapPanel == null)
                sevenTypesMapPanel = container.Find("U7_SevenTypesMap_Panel")?.gameObject ?? container.GetComponentInChildren<U7_SA_SevenTypesMap_Masters_Phonics>(true)?.gameObject;
            if (unitChallengePanel == null)
                unitChallengePanel = container.Find("U7_UnitChallenge_Panel")?.gameObject ?? container.GetComponentInChildren<U7_SA_UnitChallenge_Masters_Phonics>(true)?.gameObject;
            if (completionPanel == null)
                completionPanel = container.Find("U7_COMPLETE_Panel")?.gameObject;

            if (learnConceptCardsPanel != null)
            {
                sectionPanels = new GameObject[]
                {
                    learnConceptCardsPanel,
                    activity1TeamUpPanel,
                    activity2WordFamiliesPanel,
                    activity3SameSoundPanel,
                    activity4GlideOrHoldPanel,
                    activity5GlideFamiliesPanel,
                    activity6TeamRushPanel,
                    sevenTypesMapPanel,
                    unitChallengePanel,
                    completionPanel
                };
            }
            else if ((sectionPanels == null || sectionPanels.Length == 0) && sectionsParent != null)
            {
                List<GameObject> list = new List<GameObject>();
                foreach (Transform child in sectionsParent.transform)
                {
                    if (child != null && !child.name.ToLower().Contains("bg") && !child.name.ToLower().Contains("background"))
                    {
                        list.Add(child.gameObject);
                    }
                }
                sectionPanels = list.ToArray();
            }
        }

        [Header("Global Navigation (Inspector Overrides)")]
        [Tooltip("Global Back Button (Top Left / Static across entire unit)")]
        [SerializeField] private Button globalBackButton;
        [Tooltip("Global Next Activity Button (Bottom Right)")]
        [SerializeField] private Button nextActivityButton;

        private void WireSignboardButtons()
        {
            if (sectionSelectionPanel == null) return;

            Transform cont = sectionSelectionPanel.transform.Find("Buttons_Container") 
                          ?? sectionSelectionPanel.transform.Find("Trucks_Container") 
                          ?? sectionSelectionPanel.transform.Find("Viewport/Content")
                          ?? sectionSelectionPanel.transform.Find("Content")
                          ?? sectionSelectionPanel.transform;

            Button[] btns = cont.GetComponentsInChildren<Button>(true);
            List<Button> validTrucks = new List<Button>();
            foreach (var b in btns)
            {
                if (b == null) continue;
                string n = b.gameObject.name.ToLower();
                if (n.Contains("back") || n.Contains("home") || n.Contains("replay")) continue;
                validTrucks.Add(b);
            }

            sectionButtons = validTrucks.ToArray();

            for (int i = 0; i < sectionButtons.Length; i++)
            {
                Button b = sectionButtons[i];
                if (b == null) continue;

                // If user has already configured persistent listeners in Unity Editor, do NOT clear them!
                if (b.onClick.GetPersistentEventCount() == 0)
                {
                    int index = (sectionButtons.Length <= 6) ? (i + 1) : i;
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(() => {
                        U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("click");
                        OpenSection(index);
                    });
                }
            }
        }

        public void WireGlobalBackButtons()
        {
            Button[] allButtons = GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons)
            {
                if (b == null) continue;
                string n = b.gameObject.name.ToLower();
                if (n.Contains("back") || n.Contains("backbutton") || n.Contains("btn_back") || n.Contains("return"))
                {
                    b.gameObject.SetActive(true);
                    b.onClick.RemoveAllListeners();
                    b.onClick.AddListener(OnGlobalBackTapped);
                }
            }

            if (globalBackButton != null)
            {
                globalBackButton.gameObject.SetActive(true);
                globalBackButton.onClick.RemoveAllListeners();
                globalBackButton.onClick.AddListener(OnGlobalBackTapped);
            }
        }

        public void OnGlobalBackTapped()
        {
            U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("click");
            U7_SA_AudioManager_Masters_Phonics.Instance?.StopAllAudio();

            // Check if user is inside an activity
            bool isInsideSection = (sectionsParent != null && sectionsParent.activeSelf && currentSectionIndex >= 0);

            if (isInsideSection)
            {
                // Return to Signboard
                OpenSectionSelection();
            }
            else
            {
                // Return to Main Lessons Menu
                BackToLessonsMenu();
            }
        }

        public void BackToLessonsMenu()
        {
            U7_SA_AudioManager_Masters_Phonics.Instance?.StopAllAudio();

            if (unit7Parent != null)
            {
                unit7Parent.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            if (Unit_Selection_Panel_Masters_Phonics.Instance != null)
            {
                Unit_Selection_Panel_Masters_Phonics.Instance.BackToUnitSelection();
            }
            else
            {
                Canvas c = GetComponentInParent<Canvas>();
                if (c != null)
                {
                    Transform lessons = c.transform.Find("Lessons") ?? c.transform.Find("Lessons_Panel");
                    if (lessons != null)
                    {
                        lessons.gameObject.SetActive(true);
                        foreach (Transform child in lessons.GetComponentsInChildren<Transform>(true))
                        {
                            if (child != null) child.gameObject.SetActive(true);
                        }
                    }
                }
            }
        }

        public void OpenSectionSelection()
        {
            currentSectionIndex = -1;
            U7_SA_AudioManager_Masters_Phonics.Instance?.StopAllAudio();

            if (sectionsParent != null)
            {
                foreach (Transform child in sectionsParent.transform)
                {
                    if (child != null && !child.name.ToLower().Contains("bg") && !child.name.ToLower().Contains("background"))
                    {
                        child.gameObject.SetActive(false);
                    }
                }
                sectionsParent.SetActive(false);
            }

            if (sectionSelectionPanel != null)
            {
                sectionSelectionPanel.SetActive(true);
                sectionSelectionPanel.transform.SetAsLastSibling();
                foreach (Transform child in sectionSelectionPanel.GetComponentsInChildren<Transform>(true))
                {
                    if (child != null) child.gameObject.SetActive(true);
                }
            }

            if (unitIntroClip != null)
            {
                U7_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA(unitIntroClip);
            }
            else
            {
                U7_SA_AudioManager_Masters_Phonics.Instance?.PlayVoicePrompt("U07_VO_unit_intro");
            }
        }

        public void OpenSection(int index)
        {
            EnsureComponentBindings();
            currentSectionIndex = index;
            U7_SA_AudioManager_Masters_Phonics.Instance?.StopAllAudio();

            if (sectionSelectionPanel != null)
            {
                sectionSelectionPanel.SetActive(false);
            }

            if (sectionsParent != null)
            {
                sectionsParent.SetActive(true);

                // Ensure BG_Image is always active and placed behind all panels, while preserving user's assigned sprite and color/transparency
                Transform bgTr = sectionsParent.transform.Find("BG_Image") 
                              ?? sectionsParent.transform.Find("Background") 
                              ?? sectionsParent.transform.Find("BG");
                if (bgTr != null)
                {
                    bgTr.gameObject.SetActive(true);
                    bgTr.SetAsFirstSibling();
                }

                if (bgImageComponent != null)
                {
                    bgImageComponent.gameObject.SetActive(true);
                    // Only assign fallback background if no sprite is assigned on the component
                    if (bgImageComponent.sprite == null)
                    {
                        if (index < 4 && part1SpellingBGSprite != null)
                        {
                            bgImageComponent.sprite = part1SpellingBGSprite;
                        }
                        else if (index >= 4 && part2SoundBGSprite != null)
                        {
                            bgImageComponent.sprite = part2SoundBGSprite;
                        }
                    }
                    // Preserves user's transparent shade/color without forcing Color.white
                }

                for (int i = 0; i < sectionPanels.Length; i++)
                {
                    if (sectionPanels[i] != null)
                    {
                        sectionPanels[i].SetActive(i == index);
                        if (i == index)
                        {
                            sectionPanels[i].transform.SetAsLastSibling();
                        }
                    }
                }

                // If opening Completion Panel (Section 9 / completionPanel), initialize its stats & buttons
                if ((index == 9 || (completionPanel != null && completionPanel.activeSelf)) && completionPanel != null)
                {
                    Transform scoreT = completionPanel.transform.Find("Score_Text") 
                                    ?? completionPanel.transform.Find("FinalScoreText")
                                    ?? completionPanel.transform.Find("ScoreText")
                                    ?? completionPanel.transform.Find("CompletionModal/FinalScore")
                                    ?? completionPanel.transform.Find("CompletionModal/FinalScoreText");
                    if (scoreT != null)
                    {
                        var tmp = scoreT.GetComponent<TextMeshProUGUI>();
                        if (tmp != null) tmp.text = $"<b>Total Unit Score: <color=#FFE57F>{totalUnitScore} pts</color></b>";
                    }

                    Button[] cBtns = completionPanel.GetComponentsInChildren<Button>(true);
                    foreach (var b in cBtns)
                    {
                        if (b == null) continue;
                        string bName = b.name.ToLower();
                        if (bName.Contains("home") || bName.Contains("lesson") || bName.Contains("return") || bName.Contains("finish"))
                        {
                            b.onClick.RemoveAllListeners();
                            b.onClick.AddListener(ReturnToLessonsMenu);
                        }
                        else if (bName.Contains("signboard") || bName.Contains("select"))
                        {
                            b.onClick.RemoveAllListeners();
                            b.onClick.AddListener(OpenSectionSelection);
                        }
                    }
                }
            }

            WireGlobalBackButtons();
        }

        private bool learnCompleted = false;

        public void MarkLearnCompleted()
        {
            learnCompleted = true;
        }

        // ------------------ Signboard / Inspector Section Shortcuts ------------------
        public void OpenLearn() => OpenSection(0);               // Learn (Concept Cards)
        public void OpenActivity1() 
        {
            // Open Learn (Section 0) first; upon completing Card 5, it seamlessly transitions to Activity 1
            if (!learnCompleted)
            {
                OpenSection(0);
            }
            else
            {
                OpenSection(1);
            }
        }
        public void OpenActivity1Direct() => OpenSection(1);     // Directly opens Activity 1: Team Up
        public void OpenActivity2() => OpenSection(2);           // Activity 2: Word Families
        public void OpenActivity3() => OpenSection(3);           // Activity 3: Same Sound
        public void OpenActivity4() => OpenSection(4);           // Activity 4: Glide or Hold?
        public void OpenActivity5() => OpenSection(5);           // Activity 5: Glide Families
        public void OpenActivity6() => OpenSection(6);           // Activity 6: Team Rush (bonus)
        public void OpenSevenTypesMap() => OpenSection(7);       // Seven Types Map
        public void OpenUnitChallenge() => OpenSection(8);       // Unit Challenge
        public void OpenCompletion() => OpenSection(9);          // Completion Panel

        public void OpenSection0() => OpenSection(0);
        public void OpenSection1() => OpenSection(1);
        public void OpenSection2() => OpenSection(2);
        public void OpenSection3() => OpenSection(3);
        public void OpenSection4() => OpenSection(4);
        public void OpenSection5() => OpenSection(5);
        public void OpenSection6() => OpenSection(6);
        public void OpenSection7() => OpenSection(7);
        public void OpenSection8() => OpenSection(8);
        public void OpenSection9() => OpenSection(9);

        public void OpenSectionA() => OpenSection(0);
        public void OpenSectionB() => OpenSection(1);
        public void OpenSectionC() => OpenSection(2);
        public void OpenSectionD() => OpenSection(3);
        public void OpenSectionE() => OpenSection(4);
        public void OpenSectionF() => OpenSection(5);
        public void OpenSectionG() => OpenSection(6);

        public void AdvanceNextSection()
        {
            if (currentSectionIndex >= 0 && currentSectionIndex + 1 < sectionPanels.Length)
            {
                OpenSection(currentSectionIndex + 1);
            }
            else
            {
                OpenSectionSelection();
            }
        }

        private int[] sectionEarnedStars = new int[10];

        public void RecordSectionCompleted(int sectionIndex, int stars)
        {
            if (sectionIndex >= 0 && sectionIndex < sectionEarnedStars.Length)
            {
                sectionEarnedStars[sectionIndex] = Mathf.Max(sectionEarnedStars[sectionIndex], stars);
            }
        }

        public int GetSectionStars(int sectionIndex)
        {
            if (sectionIndex >= 0 && sectionIndex < sectionEarnedStars.Length)
                return sectionEarnedStars[sectionIndex];
            return 0;
        }

        public void AddScore(int points)
        {
            totalUnitScore += points;
        }

        public void RecordWordLearned()
        {
            totalWordsLearned++;
        }

        public void RecordGlideIdentified()
        {
            totalGlidesIdentified++;
        }

        // =========================================================================
        // High-Contrast UI Utilities
        // =========================================================================

        public static void StyleProgressBarBlue(Slider slider)
        {
            if (slider == null) return;

            Color darkNavyBg = new Color(0.06f, 0.09f, 0.16f, 1f); // #0F172A
            Color electricCyan = new Color(0.08f, 0.62f, 0.98f, 1f); // #149EFA

            Transform bg = slider.transform.Find("Background");
            if (bg != null)
            {
                Image bgImg = bg.GetComponent<Image>();
                if (bgImg != null) bgImg.color = darkNavyBg;
            }

            Transform fillArea = slider.transform.Find("Fill Area");
            if (fillArea != null)
            {
                Transform fill = fillArea.Find("Fill");
                if (fill != null)
                {
                    Image fillImg = fill.GetComponent<Image>();
                    if (fillImg != null) fillImg.color = electricCyan;
                }
            }
        }

        public static GameObject ShowActivityCompletionDialog(Transform parent, string activityTitle, int stars, int score, System.Action onNextActivity)
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
            wImg.sprite = U7_SA_GM01_ConceptCards_Masters_Phonics.GetOrCreateRoundedSprite(128, 28);
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
                bool isGold = (i < stars);
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
            stmp.text = $"<color=#BAE6FD>Score Earned:</color> <b><color=#FBBF24>+{score} pts</color></b>";
            stmp.fontSize = 32;
            stmp.alignment = TextAlignmentOptions.Center;

            // Next Activity Button
            GameObject nextBtnObj = new GameObject("NextActivityBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            nextBtnObj.transform.SetParent(win.transform, false);
            RectTransform nrt = nextBtnObj.GetComponent<RectTransform>();
            nrt.anchoredPosition = new Vector2(0f, -170f);
            nrt.sizeDelta = new Vector2(460f, 80f);

            Image nImg = nextBtnObj.GetComponent<Image>();
            nImg.sprite = U7_SA_GM01_ConceptCards_Masters_Phonics.GetOrCreateRoundedSprite(128, 18);
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
                U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("click");
                Destroy(modal);
                onNextActivity?.Invoke();
            });

            return modal;
        }
    }
}
