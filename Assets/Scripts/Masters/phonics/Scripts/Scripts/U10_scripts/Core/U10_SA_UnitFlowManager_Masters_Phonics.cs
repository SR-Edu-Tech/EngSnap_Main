using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics
{
    public class U10_SA_UnitFlowManager_Masters_Phonics : MonoBehaviour
    {
        public static U10_SA_UnitFlowManager_Masters_Phonics Instance { get; private set; }

        [Header("Containers (2-Tier Architecture)")]
        [Tooltip("Tier 1: Section Selection Hub containing the 5 purple wagons")]
        public GameObject signboardPanel;
        [Tooltip("Tier 2: Sections parent containing the individual activity panels and persistent background")]
        public GameObject sectionsContainer;
        public GameObject unitBG;

        [Header("5 Wagons / Section Selection Buttons")]
        [SerializeField] private Button wagon1LearnBtn;
        [SerializeField] private Button wagon2Act1Btn;
        [SerializeField] private Button wagon3Act2Btn;
        [SerializeField] private Button wagon4Act3Btn;
        [SerializeField] private Button wagon5Act4Btn;
        public Button[] wagonButtons;

        [Header("Section Panels")]
        public GameObject learnConceptCardsPanel;
        public GameObject activity1SoundTwinsPanel;
        public GameObject activity2WhichOneFitsPanel;
        public GameObject activity3TwoMeaningsPanel;
        public GameObject activity4SayItTwoWaysPanel;
        public GameObject unitChallengePanel;
        public GameObject completionReportPanel;
        public GameObject courseFinalePanel;

        [Header("Global Navigation (Persistent Back & Conditional Next)")]
        [SerializeField] private Button globalBackButton;
        [SerializeField] private Button nextActivityButton;

        [Header("Shared Star Activity Completion Dialog")]
        public GameObject activityCompletionDialog;
        public Image star1Image;
        public Image star2Image;
        public Image star3Image;
        public Sprite earnedGoldStarSprite;
        public Sprite emptyGreyStarSprite;
        public TextMeshProUGUI completionScoreText;
        public Button nextActivityModalBtn;

        [Header("Scoring & Statistics")]
        public int cumulativeScore = 0;
        public int wordsPairedCount = 0;
        public List<string> mistakeQueue = new List<string>();

        [Header("State Tracking")]
        public int currentActiveSection = 0; // 0 = Hub, 1 = Learn, 2 = Act1, 3 = Act2, 4 = Act3, 5 = Act4, 6 = Challenge, 7 = Complete, 8 = Finale

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

            if (signboardPanel == null)
            {
                Transform sb = root.Find("Unit_10_Section_Selection_Panels") 
                            ?? root.Find("Section_Selection_Panels") 
                            ?? root.Find("Signboard") 
                            ?? root.Find("Hub");
                if (sb != null) signboardPanel = sb.gameObject;
            }

            if (sectionsContainer == null)
            {
                Transform sec = root.Find("Unit_10_Sections") 
                             ?? root.Find("Sections") 
                             ?? root.Find("Activities");
                if (sec != null) sectionsContainer = sec.gameObject;
            }

            if (unitBG == null && sectionsContainer != null)
            {
                Transform bg = sectionsContainer.transform.Find("BG_Image") 
                            ?? sectionsContainer.transform.Find("BG") 
                            ?? root.Find("BG_Image");
                if (bg != null) unitBG = bg.gameObject;
            }

            // Bind Activity Panels
            if (sectionsContainer != null)
            {
                Transform secRoot = sectionsContainer.transform;
                if (learnConceptCardsPanel == null) learnConceptCardsPanel = FindChildPanel(secRoot, "U10_learn_Concept_Cards_Panel", "Learn_Concept_Cards", "ConceptCards", "Learn");
                if (activity1SoundTwinsPanel == null) activity1SoundTwinsPanel = FindChildPanel(secRoot, "U10_Activity_1_Sound_Twins", "Activity_1_Sound_Twins", "Activity_1", "SoundTwins");
                if (activity2WhichOneFitsPanel == null) activity2WhichOneFitsPanel = FindChildPanel(secRoot, "U10_Activity_2_Which_One_Fits", "Activity_2_Which_One_Fits", "Activity_2", "WhichOneFits");
                if (activity3TwoMeaningsPanel == null) activity3TwoMeaningsPanel = FindChildPanel(secRoot, "U10_Activity_3_Two_Meanings", "Activity_3_Two_Meanings", "Activity_3", "TwoMeanings");
                if (activity4SayItTwoWaysPanel == null) activity4SayItTwoWaysPanel = FindChildPanel(secRoot, "U10_Activity_4_Say_It_Two_Ways", "Activity_4_Say_It_Two_Ways", "Activity_4", "SayItTwoWays");
                if (unitChallengePanel == null) unitChallengePanel = FindChildPanel(secRoot, "U10_UnitChallenge_Panel", "UnitChallenge", "Challenge");
                if (completionReportPanel == null) completionReportPanel = FindChildPanel(secRoot, "U10_COMPLETE_Panel", "CompletionReport", "Complete", "Report");
                if (courseFinalePanel == null) courseFinalePanel = FindChildPanel(secRoot, "U10_CourseFinale_Panel", "CourseFinale", "Finale", "Certificate");
            }

            // Bind Wagons
            if (signboardPanel != null)
            {
                Transform hubRoot = signboardPanel.transform;
                if (wagon1LearnBtn == null) wagon1LearnBtn = FindWagonButton(hubRoot, 1, "Wagon1", "Cart1", "Btn_1", "Section1");
                if (wagon2Act1Btn == null) wagon2Act1Btn = FindWagonButton(hubRoot, 2, "Wagon2", "Cart2", "Btn_2", "Section2");
                if (wagon3Act2Btn == null) wagon3Act2Btn = FindWagonButton(hubRoot, 3, "Wagon3", "Cart3", "Btn_3", "Section3");
                if (wagon4Act3Btn == null) wagon4Act3Btn = FindWagonButton(hubRoot, 4, "Wagon4", "Cart4", "Btn_4", "Section4");
                if (wagon5Act4Btn == null) wagon5Act4Btn = FindWagonButton(hubRoot, 5, "Wagon5", "Cart5", "Btn_5", "Section5");

                wagonButtons = new Button[] { wagon1LearnBtn, wagon2Act1Btn, wagon3Act2Btn, wagon4Act3Btn, wagon5Act4Btn };
                WireWagonButtons();
            }

            // Bind Global Navigation
            Transform secTr = sectionsContainer != null ? sectionsContainer.transform : root;

            if (globalBackButton == null)
            {
                Transform gbTr = secTr.Find("GlobalBack_Btn") ?? secTr.Find("Btn_Back") ?? secTr.Find("Back_Button main") ?? root.Find("GlobalBack_Btn") ?? FindButton(root, "GlobalBack_Btn", "Btn_Back")?.transform;
                if (gbTr != null) globalBackButton = gbTr.GetComponent<Button>();
            }
            if (globalBackButton != null)
            {
                globalBackButton.onClick.RemoveAllListeners();
                globalBackButton.onClick.AddListener(OnGlobalBackTapped);
            }

            if (nextActivityButton == null)
            {
                Transform nbTr = secTr.Find("NextActivity_Btn") ?? secTr.Find("Next_Button main") ?? secTr.Find("Next_Btn") ?? root.Find("NextActivity_Btn") ?? FindButton(root, "NextActivity_Btn", "Next_Button main")?.transform;
                if (nbTr != null) nextActivityButton = nbTr.GetComponent<Button>();
            }

            // SAFETY: Ensure nextActivityButton is not accidentally inside an inner activity panel or completion dialog
            if (nextActivityButton != null)
            {
                if (activityCompletionDialog != null && nextActivityButton.transform.IsChildOf(activityCompletionDialog.transform))
                {
                    nextActivityButton = null;
                }
                else if (learnConceptCardsPanel != null && nextActivityButton.transform.IsChildOf(learnConceptCardsPanel.transform))
                {
                    nextActivityButton = null;
                }
            }

            if (nextActivityButton != null)
            {
                nextActivityButton.onClick.RemoveAllListeners();
                nextActivityButton.onClick.AddListener(AdvanceNextSection);
                nextActivityButton.gameObject.SetActive(false);
            }

            // Bind Completion Modal
            if (activityCompletionDialog == null)
            {
                Transform mod = FindDeepChild(root, "ActivityCompletionDialog") 
                             ?? FindDeepChild(root, "CompletionDialog");
                if (mod != null) activityCompletionDialog = mod.gameObject;
            }

            if (activityCompletionDialog != null)
            {
                Transform mTr = activityCompletionDialog.transform;
                if (star1Image == null) star1Image = (FindDeepChild(mTr, "Star1") ?? FindDeepChild(mTr, "Star_1"))?.GetComponent<Image>();
                if (star2Image == null) star2Image = (FindDeepChild(mTr, "Star2") ?? FindDeepChild(mTr, "Star_2"))?.GetComponent<Image>();
                if (star3Image == null) star3Image = (FindDeepChild(mTr, "Star3") ?? FindDeepChild(mTr, "Star_3"))?.GetComponent<Image>();
                if (completionScoreText == null) completionScoreText = (FindDeepChild(mTr, "ScoreText") ?? FindDeepChild(mTr, "Score_Text") ?? FindDeepChild(mTr, "Score"))?.GetComponent<TextMeshProUGUI>();
                if (nextActivityModalBtn == null) nextActivityModalBtn = (FindDeepChild(mTr, "NextBtn") ?? FindDeepChild(mTr, "ContinueBtn") ?? FindDeepChild(mTr, "Btn_Next"))?.GetComponent<Button>();

                if (nextActivityModalBtn != null)
                {
                    nextActivityModalBtn.onClick.RemoveAllListeners();
                    nextActivityModalBtn.onClick.AddListener(OnModalNextTapped);
                }
            }

            EnsureDefaultStarSprites();
        }

        private void EnsureDefaultStarSprites()
        {
#if UNITY_EDITOR
            if (earnedGoldStarSprite == null)
            {
                earnedGoldStarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png.png")
                                    ?? UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/StarFish.png");
            }
            if (emptyGreyStarSprite == null)
            {
                emptyGreyStarSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png (1).png")
                                   ?? earnedGoldStarSprite;
            }
#endif
        }

        private GameObject FindChildPanel(Transform parent, params string[] names)
        {
            if (parent == null) return null;
            foreach (string name in names)
            {
                Transform t = parent.Find(name) ?? FindDeepChild(parent, name);
                if (t != null) return t.gameObject;
            }
            return null;
        }

        private Button FindWagonButton(Transform root, int index, params string[] names)
        {
            char letter = (char)('A' + index - 1);
            List<string> searchList = new List<string>(names);
            searchList.Add($"Section {letter} Panel/Unlocked");
            searchList.Add($"Section {letter} Panel");
            searchList.Add($"Section_{letter}_Panel/Unlocked");
            searchList.Add($"Section_{letter}_Panel");
            searchList.Add($"Section {letter}/Unlocked");
            searchList.Add($"Section {letter}");
            searchList.Add($"Wagon{index}");
            searchList.Add($"Cart{index}");

            foreach (string name in searchList)
            {
                Transform t = root.Find($"Content/{name}") 
                           ?? root.Find($"Viewport/Content/{name}") 
                           ?? root.Find(name) 
                           ?? FindDeepChild(root, name);
                if (t != null)
                {
                    Button b = t.GetComponent<Button>() ?? t.GetComponentInChildren<Button>(true);
                    if (b != null) return b;
                }
            }
            return null;
        }

        private Button FindButton(Transform root, params string[] names)
        {
            if (root == null) return null;
            foreach (string name in names)
            {
                Transform t = root.Find(name) 
                           ?? root.Find($"Unit_10_Sections/{name}") 
                           ?? root.Find($"Unit_10_Section_Selection_Panels/{name}") 
                           ?? FindDeepChild(root, name);
                if (t != null)
                {
                    Button b = t.GetComponent<Button>();
                    if (b != null) return b;
                }
            }
            return null;
        }

        private Transform FindDeepChild(Transform parent, string name)
        {
            if (parent == null) return null;
            foreach (Transform child in parent)
            {
                if (child.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return child;
                Transform found = FindDeepChild(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private void WireWagonButtons()
        {
            if (wagon1LearnBtn != null) { wagon1LearnBtn.onClick.RemoveAllListeners(); wagon1LearnBtn.onClick.AddListener(OpenLearnSection); }
            if (wagon2Act1Btn != null) { wagon2Act1Btn.onClick.RemoveAllListeners(); wagon2Act1Btn.onClick.AddListener(OpenActivity2); }
            if (wagon3Act2Btn != null) { wagon3Act2Btn.onClick.RemoveAllListeners(); wagon3Act2Btn.onClick.AddListener(OpenActivity3); }
            if (wagon4Act3Btn != null) { wagon4Act3Btn.onClick.RemoveAllListeners(); wagon4Act3Btn.onClick.AddListener(OpenActivity4); }
            if (wagon5Act4Btn != null) { wagon5Act4Btn.onClick.RemoveAllListeners(); wagon5Act4Btn.onClick.AddListener(OpenUnitChallenge); }
        }

        // =====================================================================
        // Navigation & Section Visibility Invariants
        // =====================================================================

        public void OpenSignboard()
        {
            currentActiveSection = 0;

            if (signboardPanel != null) signboardPanel.SetActive(true);
            if (sectionsContainer != null) sectionsContainer.SetActive(false);

            if (activityCompletionDialog != null) activityCompletionDialog.SetActive(false);

            // Hide both nav buttons on the hub — the hub has its own wagon buttons
            if (globalBackButton != null) globalBackButton.gameObject.SetActive(false);
            if (nextActivityButton != null) nextActivityButton.gameObject.SetActive(false);

            U10_SA_AudioManager_Masters_Phonics.Instance?.PlayVoiceA("U10_VO_unit_intro");
        }


        public void OpenSectionSelection() => OpenSignboard();

        private void ShowSingleSectionPanel(GameObject targetPanel, int sectionIndex)
        {
            currentActiveSection = sectionIndex;

            if (signboardPanel != null) signboardPanel.SetActive(false);
            if (sectionsContainer != null)
            {
                sectionsContainer.SetActive(true);

                if (unitBG != null)
                {
                    unitBG.SetActive(true);
                    unitBG.transform.SetAsFirstSibling();
                }

                // Hide all sibling activity panels
                if (learnConceptCardsPanel != null) learnConceptCardsPanel.SetActive(false);
                if (activity1SoundTwinsPanel != null) activity1SoundTwinsPanel.SetActive(false);
                if (activity2WhichOneFitsPanel != null) activity2WhichOneFitsPanel.SetActive(false);
                if (activity3TwoMeaningsPanel != null) activity3TwoMeaningsPanel.SetActive(false);
                if (activity4SayItTwoWaysPanel != null) activity4SayItTwoWaysPanel.SetActive(false);
                if (unitChallengePanel != null) unitChallengePanel.SetActive(false);
                if (completionReportPanel != null) completionReportPanel.SetActive(false);
                if (courseFinalePanel != null) courseFinalePanel.SetActive(false);
            }

            if (targetPanel != null)
            {
                targetPanel.SetActive(true);
                targetPanel.transform.SetAsLastSibling();
            }

            if (activityCompletionDialog != null) activityCompletionDialog.SetActive(false);

            // Back button is ALWAYS visible inside any section (tapping it returns to the hub/signboard)
            if (globalBackButton != null)
            {
                globalBackButton.gameObject.SetActive(true);
                globalBackButton.transform.SetAsLastSibling();
            }

            // Next button is HIDDEN at the start of every section — only shown when the activity calls ShowActivityCompletionDialog()
            if (nextActivityButton != null)
            {
                nextActivityButton.gameObject.SetActive(false);
            }
        }

        public void SetNextActivityButtonActive(bool active)
        {
            if (nextActivityButton != null)
            {
                nextActivityButton.gameObject.SetActive(active);
                if (active) nextActivityButton.transform.SetAsLastSibling();
            }
        }


        public void OpenLearnSection() => ShowSingleSectionPanel(learnConceptCardsPanel, 1);
        public void OpenActivity1() => ShowSingleSectionPanel(activity1SoundTwinsPanel, 2);
        public void OpenActivity2() => ShowSingleSectionPanel(activity2WhichOneFitsPanel, 3);
        public void OpenActivity3() => ShowSingleSectionPanel(activity3TwoMeaningsPanel, 4);
        public void OpenActivity4() => ShowSingleSectionPanel(activity4SayItTwoWaysPanel, 5);
        public void OpenUnitChallenge() => ShowSingleSectionPanel(unitChallengePanel, 6);
        public void OpenCompletionPanel() => ShowSingleSectionPanel(completionReportPanel, 7);
        public void OpenCourseFinale() => ShowSingleSectionPanel(courseFinalePanel, 8);

        public void AdvanceNextSection()
        {
            switch (currentActiveSection)
            {
                case 1: OpenActivity1(); break;
                case 2: OpenActivity2(); break;
                case 3: OpenActivity3(); break;
                case 4: OpenActivity4(); break;
                case 5: OpenUnitChallenge(); break;
                case 6: OpenCompletionPanel(); break;
                case 7: OpenCourseFinale(); break;
                case 8: BackToLessonsMenu(); break;
                default: OpenSignboard(); break;
            }
        }

        public void OnGlobalBackTapped()
        {
            U10_SA_AudioManager_Masters_Phonics.Instance?.PlayClick();
            U10_SA_AudioManager_Masters_Phonics.Instance?.StopAll();

            if (currentActiveSection != 0)
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
        // Star Completion Modal
        // =====================================================================

        public void ShowActivityCompletionDialog(int earnedStars, int pointsEarned)
        {
            AddScore(pointsEarned);

            if (activityCompletionDialog != null)
            {
                activityCompletionDialog.SetActive(true);
                activityCompletionDialog.transform.SetAsLastSibling();

                if (star1Image != null) star1Image.sprite = (earnedStars >= 1) ? earnedGoldStarSprite : emptyGreyStarSprite;
                if (star2Image != null) star2Image.sprite = (earnedStars >= 2) ? earnedGoldStarSprite : emptyGreyStarSprite;
                if (star3Image != null) star3Image.sprite = (earnedStars >= 3) ? earnedGoldStarSprite : emptyGreyStarSprite;

                if (completionScoreText != null)
                {
                    completionScoreText.text = $"<b>+{pointsEarned} pts</b>";
                }
            }

            if (nextActivityButton != null)
            {
                nextActivityButton.gameObject.SetActive(true);
                nextActivityButton.transform.SetAsLastSibling();
            }

            U10_SA_AudioManager_Masters_Phonics.Instance?.PlayCelebration();
        }

        private void OnModalNextTapped()
        {
            U10_SA_AudioManager_Masters_Phonics.Instance?.PlayClick();
            if (activityCompletionDialog != null) activityCompletionDialog.SetActive(false);
            AdvanceNextSection();
        }

        // =====================================================================
        // Scoring & Mistake Tracking
        // =====================================================================

        public void AddScore(int score)
        {
            cumulativeScore += score;
        }

        public int GetCumulativeScore() => cumulativeScore;

        public void RecordMistake(string item, bool isWeighted = false)
        {
            mistakeQueue.Add(item);
            if (isWeighted) mistakeQueue.Add(item); // 2x weighting for Round B high frequency items
        }

        public void IncrementWordsPaired(int count = 1)
        {
            wordsPairedCount += count;
        }

        public static void StyleProgressBarBlue(Slider slider)
        {
            if (slider == null) return;
            slider.interactable = false;

            var bgImg = slider.GetComponentInChildren<Image>(true);
            if (bgImg != null && bgImg.gameObject != slider.fillRect?.gameObject)
            {
                bgImg.color = new Color(0.06f, 0.09f, 0.16f, 0.95f); // Dark navy
            }

            if (slider.fillRect != null)
            {
                var fillImg = slider.fillRect.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.color = new Color(0.08f, 0.62f, 0.98f); // Electric cyan blue
                }

                slider.fillRect.offsetMin = Vector2.zero;
                slider.fillRect.offsetMax = Vector2.zero;
            }
        }
    }
}
