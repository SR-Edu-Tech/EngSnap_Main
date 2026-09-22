using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using Object = UnityEngine.Object;

namespace MastersPhonics.EditorTools
{
    public static class U10_HierarchyAutomator
    {
        private const string ART_BASE_PATH = "Assets/Art";
        private const string TOPIC_SEL_PATH = "Assets/Art/TOPIC SELECTION";

        private static void ClearCaches()
        {
        }

        // =========================================================================
        // Master Menu Item: Generate Entire Unit 10 Hierarchy
        // =========================================================================

        [MenuItem("Masters Phonics/Unit 10/Generate Unit 10 Complete Hierarchy", false, 100)]
        public static void GenerateUnit10CompleteHierarchy()
        {
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in Scene! Please open a scene with a Canvas.", "OK");
                return;
            }

            ClearCaches();

            Transform sourceUnit = canvas.transform.Find("Unit_9") 
                                ?? canvas.transform.Find("Unit_8") 
                                ?? canvas.transform.Find("Unit_7")
                                ?? canvas.transform.Find("Unit_6");

            // 1. Root Unit 10 GameObject
            Transform u10Tr = canvas.transform.Find("Unit_10");
            GameObject u10Go;
            if (u10Tr != null)
            {
                u10Go = u10Tr.gameObject;
            }
            else if (sourceUnit != null)
            {
                u10Go = UnityEngine.Object.Instantiate(sourceUnit.gameObject, canvas.transform);
                u10Go.name = "Unit_10";
                Undo.RegisterCreatedObjectUndo(u10Go, "Clone Unit_10 from " + sourceUnit.name);
            }
            else
            {
                u10Go = new GameObject("Unit_10", typeof(RectTransform));
                u10Go.transform.SetParent(canvas.transform, false);
                RectTransform rt = u10Go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            StripOldUnitComponents(u10Go);

            var flow = u10Go.GetComponent<U10_SA_UnitFlowManager_Masters_Phonics>() ?? u10Go.AddComponent<U10_SA_UnitFlowManager_Masters_Phonics>();
            var audio = u10Go.GetComponent<U10_SA_AudioManager_Masters_Phonics>() ?? u10Go.AddComponent<U10_SA_AudioManager_Masters_Phonics>();

            // 2. Tier 1: Section Selection Panels (Clone from Unit 9 / Unit 8)
            Transform selPanels = DuplicateAndSetupUnit10Signboard(u10Go, canvas);

            // 3. Tier 2: Sections Container
            Transform sectionsParent = u10Go.transform.Find("Unit_10_Sections");
            if (sectionsParent == null)
            {
                GameObject secGo = new GameObject("Unit_10_Sections", typeof(RectTransform));
                secGo.transform.SetParent(u10Go.transform, false);
                RectTransform rt = secGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                sectionsParent = secGo.transform;
            }

            // 4. Background Image
            Transform bgTr = sectionsParent.Find("BG_Image");
            if (bgTr == null)
            {
                GameObject bgGo = new GameObject("BG_Image", typeof(RectTransform), typeof(Image));
                bgGo.transform.SetParent(sectionsParent, false);
                RectTransform rt = bgGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                var img = bgGo.GetComponent<Image>();
                img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART_BASE_PATH}/BG_Bright.png");
                img.color = Color.white;
                bgTr = bgGo.transform;
            }
            bgTr.SetAsFirstSibling();

            // 5. Build Individual Activity Panels
            GameObject pLearn = BuildOrCleanPanel(sectionsParent, "U10_learn_Concept_Cards_Panel", "Learn_Concept_Cards");
            SetupGM01ConceptCards(pLearn);

            GameObject pAct1 = BuildOrCleanPanel(sectionsParent, "U10_Activity_1_Sound_Twins", "Activity_1_Sound_Twins");
            SetupGM03SoundTwins(pAct1);

            GameObject pAct2 = BuildOrCleanPanel(sectionsParent, "U10_Activity_2_Which_One_Fits", "Activity_2_Which_One_Fits");
            SetupGM04WhichOneFits(pAct2);

            GameObject pAct3 = BuildOrCleanPanel(sectionsParent, "U10_Activity_3_Two_Meanings", "Activity_3_Two_Meanings");
            SetupGM06TwoMeanings(pAct3);

            GameObject pAct4 = BuildOrCleanPanel(sectionsParent, "U10_Activity_4_Say_It_Two_Ways", "Activity_4_Say_It_Two_Ways");
            SetupGM04sSayItTwoWays(pAct4);

            GameObject pChallenge = BuildOrCleanPanel(sectionsParent, "U10_UnitChallenge_Panel", "UnitChallenge");
            SetupUnitChallenge(pChallenge);

            GameObject pComplete = BuildOrCleanPanel(sectionsParent, "U10_COMPLETE_Panel", "COMPLETE");
            SetupCompletionPanel(pComplete);

            GameObject pFinale = BuildOrCleanPanel(sectionsParent, "U10_CourseFinale_Panel", "CourseFinale");
            SetupCourseFinale(pFinale);

            EnsureGlobalNavigation(u10Go.transform, sectionsParent);
            EnsureActivityCompletionDialog(u10Go.transform);

            // 6. Ensure HUD & Prompts, Sanitize TextMeshPro Unicode, Auto-Assign All Inspectors
            EnsureAllHUDAndPromptGameObjects(u10Go);
            SanitizeAllTextMeshProInHierarchy(u10Go);
            AutoAssignAllInspectors(u10Go);

            // 7. Initial Visibility States
            selPanels.gameObject.SetActive(true);
            sectionsParent.gameObject.SetActive(false);

            EditorUtility.SetDirty(u10Go);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(u10Go.scene);

            Debug.Log("<color=#10B981><b>[Unit 10 Automator]</b> Generated complete Unit 10 hierarchy, 4 Activities, Capstone Challenge, and Course Finale successfully!</color>");
            EditorUtility.DisplayDialog("Unit 10 Generation Complete!", 
                "Successfully generated all Unit 10 Panels (Learn, 4 Activities, Unit Challenge, Completion Report, and Course Finale)!\n\nAll scripts and Inspectors have been auto-assigned.", 
                "Awesome!");
        }

        // =========================================================================
        // Non-Destructive In-Place Update (Preserves Existing GameObjects & Visual Layouts)
        // =========================================================================

        [MenuItem("Masters Phonics/Unit 10/Non-Destructive In-Place Update (Preserve Existing Objects)", false, 102)]
        public static void NonDestructiveInPlaceUpdateUnit10()
        {
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in Scene! Please open a scene with a Canvas.", "OK");
                return;
            }

            Transform u10Tr = canvas.transform.Find("Unit_10");
            if (u10Tr == null)
            {
                bool create = EditorUtility.DisplayDialog("Unit 10 Not Found", 
                    "No 'Unit_10' GameObject was found under the Canvas.\n\nWould you like to run the Complete Generator instead?", "Generate Now", "Cancel");
                if (create) GenerateUnit10CompleteHierarchy();
                return;
            }

            GameObject u10Go = u10Tr.gameObject;
            RemoveMissingScriptsRecursively(u10Go);
            StripOldUnitComponents(u10Go);

            // 1. Root Managers
            var flow = u10Go.GetComponent<U10_SA_UnitFlowManager_Masters_Phonics>() ?? u10Go.AddComponent<U10_SA_UnitFlowManager_Masters_Phonics>();
            var audio = u10Go.GetComponent<U10_SA_AudioManager_Masters_Phonics>() ?? u10Go.AddComponent<U10_SA_AudioManager_Masters_Phonics>();

            // 2. Tier 1: Section Selection Panels (Signboard with 5 Wagons)
            Transform selPanels = u10Go.transform.Find("Unit_10_Section_Selection_Panels") 
                               ?? u10Go.transform.Find("Section_Selection_Panels") 
                               ?? u10Go.transform.Find("Signboard");
            if (selPanels != null)
            {
                UpdateSignboardLabelsInPlace(selPanels);
            }
            else
            {
                selPanels = DuplicateAndSetupUnit10Signboard(u10Go, canvas);
            }

            // 3. Tier 2: Sections Container
            Transform sectionsParent = u10Go.transform.Find("Unit_10_Sections") ?? u10Go.transform.Find("Sections");
            if (sectionsParent == null)
            {
                GameObject secGo = new GameObject("Unit_10_Sections", typeof(RectTransform));
                secGo.transform.SetParent(u10Go.transform, false);
                RectTransform rt = secGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                sectionsParent = secGo.transform;
            }

            // 4. In-Place Panel Component Attachment (Preserves all children if panel already exists)
            EnsurePanelNonDestructive<U10_SA_GM01_ConceptCards_Masters_Phonics>(sectionsParent, "U10_learn_Concept_Cards_Panel", "Learn_Concept_Cards", SetupGM01ConceptCards);
            EnsurePanelNonDestructive<U10_SA_GM03_SoundTwins_Masters_Phonics>(sectionsParent, "U10_Activity_1_Sound_Twins", "Activity_1_Sound_Twins", SetupGM03SoundTwins);
            EnsurePanelNonDestructive<U10_SA_GM04_WhichOneFits_Masters_Phonics>(sectionsParent, "U10_Activity_2_Which_One_Fits", "Activity_2_Which_One_Fits", SetupGM04WhichOneFits);
            EnsurePanelNonDestructive<U10_SA_GM06_TwoMeanings_Masters_Phonics>(sectionsParent, "U10_Activity_3_Two_Meanings", "Activity_3_Two_Meanings", SetupGM06TwoMeanings);
            EnsurePanelNonDestructive<U10_SA_GM04s_SayItTwoWays_Masters_Phonics>(sectionsParent, "U10_Activity_4_Say_It_Two_Ways", "Activity_4_Say_It_Two_Ways", SetupGM04sSayItTwoWays);
            EnsurePanelNonDestructive<U10_SA_UnitChallenge_Masters_Phonics>(sectionsParent, "U10_UnitChallenge_Panel", "UnitChallenge", SetupUnitChallenge);
            EnsurePanelNonDestructive<U10_SA_CompletionPanel_Masters_Phonics>(sectionsParent, "U10_COMPLETE_Panel", "COMPLETE", SetupCompletionPanel);
            EnsurePanelNonDestructive<U10_SA_CourseFinale_Masters_Phonics>(sectionsParent, "U10_CourseFinale_Panel", "CourseFinale", SetupCourseFinale);

            EnsureGlobalNavigation(u10Go.transform, sectionsParent);
            EnsureActivityCompletionDialog(u10Go.transform);

            // 5. In-Place Auto-Assign & Sanitize
            EnsureAllHUDAndPromptGameObjects(u10Go);
            SanitizeAllTextMeshProInHierarchy(u10Go);
            AutoAssignAllInspectors(u10Go);

            EditorUtility.SetDirty(u10Go);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(u10Go.scene);

            Debug.Log("<color=#10B981><b>[Unit 10 Automator]</b> Non-Destructive In-Place Update completed! All existing GameObjects, layouts, and art were preserved, and Unit 10 scripts and Inspector bindings were updated in-place.</color>");
            EditorUtility.DisplayDialog("Unit 10 In-Place Update Complete!", 
                "Successfully updated Unit 10 in-place!\n\n• All existing GameObjects, positions, and art were preserved.\n• Unit 10 script components were attached/verified.\n• All Inspectors, star ratings, audio clips, and badges were auto-bound.", 
                "Awesome!");
        }

        // =========================================================================
        // Auto-Fix HUD & Auto-Assign All Inspectors Menu Item
        // =========================================================================

        [MenuItem("Masters Phonics/Unit 10/Auto-Fix HUD & Auto-Assign All Inspectors", false, 105)]
        public static void AutoFixHUDAndAssignMenuAction()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Transform u10Tr = canvas != null ? canvas.transform.Find("Unit_10") : null;
            if (u10Tr == null)
            {
                EditorUtility.DisplayDialog("Not Found", "No 'Unit_10' GameObject found under the Canvas in this scene.", "OK");
                return;
            }

            EnsureAllHUDAndPromptGameObjects(u10Tr.gameObject);
            SanitizeAllTextMeshProInHierarchy(u10Tr.gameObject);
            AutoAssignAllInspectors(u10Tr.gameObject);

            EditorUtility.SetDirty(u10Tr.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(u10Tr.gameObject.scene);
            EditorUtility.DisplayDialog("Done!", "Auto-fixed all activity HUDs (Progress_Text, Score_Text, ProgressBar, Prompts) and auto-assigned all Inspector fields across Unit 10!", "Great!");
        }

        /// <summary>
        /// Ensures that each activity panel in Unit 10 has a ProgressHUD with Progress_Text, Score_Text,
        /// and required Prompt/Instruction text objects so Inspector fields are never unassigned.
        /// </summary>
        private static void EnsureAllHUDAndPromptGameObjects(GameObject root)
        {
            if (root == null) return;

            // 0. Learn Concept Cards: Remove or deactivate redundant "Start Activity 1" button
            var learnPanel = root.GetComponentInChildren<U10_SA_GM01_ConceptCards_Masters_Phonics>(true);
            if (learnPanel != null)
            {
                var buttons = learnPanel.GetComponentsInChildren<Button>(true);
                foreach (var b in buttons)
                {
                    if (b == null) continue;
                    string bName = b.name.ToLower();
                    var tmp = b.GetComponentInChildren<TextMeshProUGUI>(true);
                    string bText = tmp != null ? tmp.text.ToLower() : "";
                    if (bName.Contains("startactivity") || bName.Contains("start_activity") || bName.Contains("start activity") ||
                        bText.Contains("start activity") || bText.Contains("start activity 1"))
                    {
                        b.gameObject.SetActive(false);
                    }
                }
            }

            // 1. GM03 Sound Twins
            var gm03 = root.GetComponentInChildren<U10_SA_GM03_SoundTwins_Masters_Phonics>(true);
            if (gm03 != null)
            {
                EnsureStandardHUD(gm03.transform, "SOUND TWINS — HOMOPHONE MATCH", "Tap two words that sound the same");
                Transform insTr = gm03.transform.Find("InstructionText") ?? gm03.transform.Find("PromptText");
                if (insTr == null)
                {
                    GameObject insGo = new GameObject("InstructionText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    insGo.transform.SetParent(gm03.transform, false);
                    RectTransform rt = insGo.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0, 195);
                    rt.sizeDelta = new Vector2(900, 45);
                    var t = insGo.GetComponent<TextMeshProUGUI>();
                    t.text = "<b>TAP TWO CARDS WITH THE SAME SOUND</b>";
                    t.fontSize = 32;
                    t.fontStyle = FontStyles.Bold;
                    t.alignment = TextAlignmentOptions.Center;
                    t.color = new Color(0.75f, 0.9f, 1f);
                }
            }

            // 2. GM04 Which One Fits
            var gm04 = root.GetComponentInChildren<U10_SA_GM04_WhichOneFits_Masters_Phonics>(true);
            if (gm04 != null)
            {
                EnsureStandardHUD(gm04.transform, "WHICH ONE FITS? — HOMOPHONE SENTENCES", "Choose the right spelling to complete the sentence");
                Transform pt = gm04.transform.Find("PromptText") ?? gm04.transform.Find("InstructionText");
                if (pt == null)
                {
                    GameObject pGo = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    pGo.transform.SetParent(gm04.transform, false);
                    RectTransform rt = pGo.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0, 195);
                    rt.sizeDelta = new Vector2(900, 45);
                    var t = pGo.GetComponent<TextMeshProUGUI>();
                    t.text = "<b>Which word fits the sentence?</b>";
                    t.fontSize = 32;
                    t.fontStyle = FontStyles.Bold;
                    t.alignment = TextAlignmentOptions.Center;
                    t.color = new Color(0.75f, 0.9f, 1f);
                }
            }

            // 3. GM06 Dual Meaning Duel
            var gm06 = root.GetComponentInChildren<U10_SA_GM06_TwoMeanings_Masters_Phonics>(true);
            if (gm06 != null)
            {
                EnsureStandardHUD(gm06.transform, "DUAL MEANING DUEL — HOMONYM MATCH", "Choose the picture that matches the sentence");
                Transform pt = gm06.transform.Find("PromptText") ?? gm06.transform.Find("InstructionText");
                if (pt == null)
                {
                    GameObject pGo = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    pGo.transform.SetParent(gm06.transform, false);
                    RectTransform rt = pGo.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0, 195);
                    rt.sizeDelta = new Vector2(900, 45);
                    var t = pGo.GetComponent<TextMeshProUGUI>();
                    t.text = "<b>Choose the picture that matches the sentence</b>";
                    t.fontSize = 32;
                    t.fontStyle = FontStyles.Bold;
                    t.alignment = TextAlignmentOptions.Center;
                    t.color = new Color(0.75f, 0.9f, 1f);
                }
            }

            // 4. GM04s Say It Two Ways
            var gm04s = root.GetComponentInChildren<U10_SA_GM04s_SayItTwoWays_Masters_Phonics>(true);
            if (gm04s != null)
            {
                EnsureStandardHUD(gm04s.transform, "SAY IT TWO WAYS — HOMOGRAPH SELECTOR", "How do you pronounce this word in the sentence?");
                Transform pt = gm04s.transform.Find("PromptText") ?? gm04s.transform.Find("InstructionText");
                if (pt == null)
                {
                    GameObject pGo = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    pGo.transform.SetParent(gm04s.transform, false);
                    RectTransform rt = pGo.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0, 15);
                    rt.sizeDelta = new Vector2(800, 40);
                    var t = pGo.GetComponent<TextMeshProUGUI>();
                    t.text = "<b>How do you say \"read\" here?</b>";
                    t.fontSize = 32;
                    t.fontStyle = FontStyles.Bold;
                    t.alignment = TextAlignmentOptions.Center;
                    t.color = new Color(0.85f, 0.92f, 1f);
                }
            }

            // 5. Unit Challenge
            var chal = root.GetComponentInChildren<U10_SA_UnitChallenge_Masters_Phonics>(true);
            if (chal != null)
            {
                EnsureStandardHUD(chal.transform, "UNIT 10 CHALLENGE — TRIPLE BADGE QUEST", "Answer correctly to earn badges");
                Transform pt = chal.transform.Find("PromptText") ?? chal.transform.Find("InstructionText");
                if (pt == null)
                {
                    GameObject pGo = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
                    pGo.transform.SetParent(chal.transform, false);
                    RectTransform rt = pGo.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(0, 195);
                    rt.sizeDelta = new Vector2(900, 45);
                    var t = pGo.GetComponent<TextMeshProUGUI>();
                    t.text = "<b>Answer the question to earn the badge!</b>";
                    t.fontSize = 32;
                    t.fontStyle = FontStyles.Bold;
                    t.alignment = TextAlignmentOptions.Center;
                    t.color = new Color(0.75f, 0.9f, 1f);
                }
            }
        }

        // =========================================================================
        // Undo / Revert Mobile Auto-Sizing
        // =========================================================================

        [MenuItem("Masters Phonics/Unit 10/Undo Mobile Font Sizes (Disable Auto-Sizing)", false, 110)]
        public static void UndoMobileFontSizesMenuAction()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Transform u10Tr = canvas != null ? canvas.transform.Find("Unit_10") : null;
            if (u10Tr == null)
            {
                EditorUtility.DisplayDialog("Not Found", "No 'Unit_10' GameObject found under the Canvas in this scene.", "OK");
                return;
            }

            var allTMP = u10Tr.GetComponentsInChildren<TextMeshProUGUI>(true);
            int count = 0;
            Undo.RecordObjects(allTMP, "Undo Mobile Font Sizes");

            foreach (var tmp in allTMP)
            {
                if (tmp.enableAutoSizing)
                {
                    tmp.enableAutoSizing = false;
                    tmp.fontSizeMin = 18f;
                    EditorUtility.SetDirty(tmp);
                    count++;
                }
            }

            EditorUtility.SetDirty(u10Tr.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(u10Tr.gameObject.scene);
            EditorUtility.DisplayDialog("Done!", $"Disabled Auto-Sizing on {count} TextMeshPro component(s).\n\nText components now use their natural, fixed font sizes without min 30 auto-scaling.", "Great!");
        }

        /// <summary>
        /// Sweeps all TextMeshProUGUI under the given root and enforces:
        ///   enableAutoSizing = true, fontSizeMin = 30, fontSizeMax = max(existing size, 30)
        /// Any text that was smaller than 30 is bumped up. Larger values are preserved.
        /// </summary>
        private static int EnforceMobileFontSizes(GameObject root)
        {
            if (root == null) return 0;
            const float MIN_SIZE = 30f;

            var allTMP = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            int count = 0;
            foreach (var tmp in allTMP)
            {
                bool changed = false;

                // Enable auto-sizing so text scales down gracefully inside its container
                if (!tmp.enableAutoSizing)
                {
                    tmp.enableAutoSizing = true;
                    changed = true;
                }

                // Ensure the minimum is at least 30
                if (tmp.fontSizeMin < MIN_SIZE)
                {
                    tmp.fontSizeMin = MIN_SIZE;
                    changed = true;
                }

                // Ensure the max is at least 30 (preserve larger values)
                if (tmp.fontSizeMax < MIN_SIZE)
                {
                    tmp.fontSizeMax = MIN_SIZE;
                    changed = true;
                }

                // If not using auto-size, also bump the fixed fontSize if it's too small
                if (tmp.fontSize < MIN_SIZE)
                {
                    tmp.fontSize = MIN_SIZE;
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(tmp);
                    count++;
                }
            }

            Debug.Log($"<color=#10B981><b>[Unit 10 Automator]</b> Enforced mobile font sizes on {count}/{allTMP.Length} TMP components (min={MIN_SIZE}).</color>");
            return count;
        }

        private static GameObject EnsurePanelNonDestructive<T>(Transform sectionsParent, string primaryName, string altName, Action<GameObject> setupIfNew) where T : MonoBehaviour
        {
            Transform t = sectionsParent.Find(primaryName) ?? sectionsParent.Find(altName);
            if (t != null)
            {
                // Existing GameObject found! Preserve all its children, just ensure correct script component
                GameObject go = t.gameObject;
                go.name = primaryName;
                StripOldActivityComponents(go);
                var comp = go.GetComponent<T>() ?? go.AddComponent<T>();
                return go;
            }
            else
            {
                // Not found, build new using setup action
                GameObject go = BuildOrCleanPanel(sectionsParent, primaryName, altName);
                setupIfNew(go);
                return go;
            }
        }

        private static void UpdateSignboardLabelsInPlace(Transform selGo)
        {
            // Update Header Typography
            Transform titleTr = selGo.Find("Signboard/Title") 
                             ?? selGo.Find("Title") 
                             ?? selGo.Find("Header/Title") 
                             ?? selGo.Find("Signboard_Title")
                             ?? selGo.Find("Signboard/Header/Title");
            if (titleTr != null)
            {
                var tmp = titleTr.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = "<b>HOMONYMS, HOMOPHONES & HOMOGRAPHS</b>";
            }

            Transform unitNumTr = selGo.Find("Signboard/UnitText") 
                               ?? selGo.Find("UnitText") 
                               ?? selGo.Find("Header/UnitText") 
                               ?? selGo.Find("Unit_Number")
                               ?? selGo.Find("Signboard/Header/UnitText");
            if (unitNumTr != null)
            {
                var tmp = unitNumTr.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = "<b>UNIT 10</b>";
            }

            string[] u10WagonTitles = new string[] {
                "Learn & Concepts",
                "Activity 1\nSound Twins",
                "Activity 2\nWhich One Fits?",
                "Activity 3\nTwo Meanings",
                "Activity 4\nSay It Two Ways"
            };

            for (int i = 1; i <= 5; i++)
            {
                Transform wTr = selGo.Find($"Content/Wagon{i}") 
                             ?? selGo.Find($"Wagons/Wagon{i}") 
                             ?? selGo.Find($"Wagon{i}")
                             ?? selGo.Find($"Content/Cart{i}")
                             ?? selGo.Find($"Cart{i}")
                             ?? selGo.Find($"Signboard/Content/Wagon{i}");
                if (wTr != null)
                {
                    var txt = wTr.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null)
                    {
                        txt.text = $"<b>{u10WagonTitles[i - 1]}</b>";
                    }
                }
            }

            Transform content = selGo.Find("Content") ?? selGo.Find("Wagons") ?? selGo.Find("Signboard/Content");
            if (content != null)
            {
                var btns = content.GetComponentsInChildren<Button>(true);
                for (int i = 0; i < btns.Length && i < u10WagonTitles.Length; i++)
                {
                    var txt = btns[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null) txt.text = $"<b>{u10WagonTitles[i]}</b>";
                }
            }
        }

        // =========================================================================
        // Panel Setup & Rebuilding Helpers
        // =========================================================================

        private static GameObject BuildOrCleanPanel(Transform parent, string primaryName, string altName)
        {
            Transform t = parent.Find(primaryName) ?? parent.Find(altName);
            GameObject go;
            if (t == null)
            {
                go = new GameObject(primaryName, typeof(RectTransform));
                go.transform.SetParent(parent, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else
            {
                go = t.gameObject;
                go.name = primaryName;
                int childCount = go.transform.childCount;
                for (int i = childCount - 1; i >= 0; i--)
                {
                    UnityEngine.Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
                }
            }
            return go;
        }

        // =====================================================================
        // Individual Panel Menu Actions (Panel-Wise Rebuilding)
        // =====================================================================

        private static Transform GetSectionsParent()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return null;
            Transform u10 = canvas.transform.Find("Unit_10");
            if (u10 == null) return null;
            return u10.Find("Unit_10_Sections") ?? u10.Find("Sections");
        }

        [MenuItem("Masters Phonics/Unit 10/Panels/1. Clean & Setup Learn Concept Cards", false, 201)]
        public static void MenuSetupLearnConceptCards()
        {
            Transform sp = GetSectionsParent();
            if (sp == null) { EditorUtility.DisplayDialog("Error", "Unit_10/Unit_10_Sections not found! Run Generate Unit 10 Complete Hierarchy first.", "OK"); return; }
            GameObject go = BuildOrCleanPanel(sp, "U10_learn_Concept_Cards_Panel", "learn_Concept_Cards_Panel");
            SetupGM01ConceptCards(go);
            var comp = go.GetComponent<U10_SA_GM01_ConceptCards_Masters_Phonics>();
            if (comp != null) { comp.AutoBindHierarchyElements(); EditorUtility.SetDirty(comp); }
            EnsureGlobalNavigation(sp.parent, sp);
            var flow = sp.GetComponentInParent<U10_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) { flow.AutoBindHierarchyElements(); EditorUtility.SetDirty(flow); }
            EditorUtility.SetDirty(go);
            Debug.Log("<color=#10B981><b>[Unit 10]</b> Cleaned & Rebuilt Learn Concept Cards Panel!</color>");
        }

        [MenuItem("Masters Phonics/Unit 10/Panels/2. Clean & Setup Activity 1 (Sound Twins)", false, 202)]
        public static void MenuSetupActivity1()
        {
            Transform sp = GetSectionsParent();
            if (sp == null) { EditorUtility.DisplayDialog("Error", "Unit_10/Unit_10_Sections not found!", "OK"); return; }
            GameObject go = BuildOrCleanPanel(sp, "U10_Activity_1_Sound_Twins", "Activity_1_Sound_Twins");
            SetupGM03SoundTwins(go);
            var comp = go.GetComponent<U10_SA_GM03_SoundTwins_Masters_Phonics>();
            if (comp != null) { comp.AutoBindHierarchyElements(); EditorUtility.SetDirty(comp); }
            EnsureGlobalNavigation(sp.parent, sp);
            var flow = sp.GetComponentInParent<U10_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) { flow.AutoBindHierarchyElements(); EditorUtility.SetDirty(flow); }
            EditorUtility.SetDirty(go);
            Debug.Log("<color=#10B981><b>[Unit 10]</b> Cleaned & Rebuilt Activity 1 (Sound Twins)!</color>");
        }

        [MenuItem("Masters Phonics/Unit 10/Panels/3. Clean & Setup Activity 2 (Which One Fits?)", false, 203)]
        public static void MenuSetupActivity2()
        {
            Transform sp = GetSectionsParent();
            if (sp == null) { EditorUtility.DisplayDialog("Error", "Unit_10/Unit_10_Sections not found!", "OK"); return; }
            GameObject go = BuildOrCleanPanel(sp, "U10_Activity_2_Which_One_Fits", "Activity_2_Which_One_Fits");
            SetupGM04WhichOneFits(go);
            var comp = go.GetComponent<U10_SA_GM04_WhichOneFits_Masters_Phonics>();
            if (comp != null) { comp.AutoBindHierarchyElements(); EditorUtility.SetDirty(comp); }
            EnsureGlobalNavigation(sp.parent, sp);
            var flow = sp.GetComponentInParent<U10_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) { flow.AutoBindHierarchyElements(); EditorUtility.SetDirty(flow); }
            EditorUtility.SetDirty(go);
            Debug.Log("<color=#10B981><b>[Unit 10]</b> Cleaned & Rebuilt Activity 2 (Which One Fits?)!</color>");
        }

        [MenuItem("Masters Phonics/Unit 10/Panels/4. Clean & Setup Activity 3 (Two Meanings)", false, 204)]
        public static void MenuSetupActivity3()
        {
            Transform sp = GetSectionsParent();
            if (sp == null) { EditorUtility.DisplayDialog("Error", "Unit_10/Unit_10_Sections not found!", "OK"); return; }
            GameObject go = BuildOrCleanPanel(sp, "U10_Activity_3_Two_Meanings", "Activity_3_Two_Meanings");
            SetupGM06TwoMeanings(go);
            var comp = go.GetComponent<U10_SA_GM06_TwoMeanings_Masters_Phonics>();
            if (comp != null) { comp.AutoBindHierarchyElements(); EditorUtility.SetDirty(comp); }
            EnsureGlobalNavigation(sp.parent, sp);
            var flow = sp.GetComponentInParent<U10_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) { flow.AutoBindHierarchyElements(); EditorUtility.SetDirty(flow); }
            EditorUtility.SetDirty(go);
            Debug.Log("<color=#10B981><b>[Unit 10]</b> Cleaned & Rebuilt Activity 3 (Two Meanings)!</color>");
        }

        [MenuItem("Masters Phonics/Unit 10/Panels/5. Clean & Setup Activity 4 (Say It Two Ways)", false, 205)]
        public static void MenuSetupActivity4()
        {
            Transform sp = GetSectionsParent();
            if (sp == null) { EditorUtility.DisplayDialog("Error", "Unit_10/Unit_10_Sections not found!", "OK"); return; }
            GameObject go = BuildOrCleanPanel(sp, "U10_Activity_4_Say_It_Two_Ways", "Activity_4_Say_It_Two_Ways");
            SetupGM04sSayItTwoWays(go);
            var comp = go.GetComponent<U10_SA_GM04s_SayItTwoWays_Masters_Phonics>();
            if (comp != null) { comp.AutoBindHierarchyElements(); EditorUtility.SetDirty(comp); }
            EnsureGlobalNavigation(sp.parent, sp);
            var flow = sp.GetComponentInParent<U10_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) { flow.AutoBindHierarchyElements(); EditorUtility.SetDirty(flow); }
            EditorUtility.SetDirty(go);
            Debug.Log("<color=#10B981><b>[Unit 10]</b> Cleaned & Rebuilt Activity 4 (Say It Two Ways)!</color>");
        }

        [MenuItem("Masters Phonics/Unit 10/Panels/6. Clean & Setup Unit Challenge", false, 206)]
        public static void MenuSetupChallenge()
        {
            Transform sp = GetSectionsParent();
            if (sp == null) { EditorUtility.DisplayDialog("Error", "Unit_10/Unit_10_Sections not found!", "OK"); return; }
            GameObject go = BuildOrCleanPanel(sp, "U10_UnitChallenge_Panel", "UnitChallenge");
            SetupUnitChallenge(go);
            var comp = go.GetComponent<U10_SA_UnitChallenge_Masters_Phonics>();
            if (comp != null) { comp.AutoBindHierarchyElements(); EditorUtility.SetDirty(comp); }
            EnsureGlobalNavigation(sp.parent, sp);
            var flow = sp.GetComponentInParent<U10_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) { flow.AutoBindHierarchyElements(); EditorUtility.SetDirty(flow); }
            EditorUtility.SetDirty(go);
            Debug.Log("<color=#10B981><b>[Unit 10]</b> Cleaned & Rebuilt Unit Challenge Panel!</color>");
        }

        [MenuItem("Masters Phonics/Unit 10/Panels/7. Clean & Setup Completion Panel", false, 207)]
        public static void MenuSetupCompletion()
        {
            Transform sp = GetSectionsParent();
            if (sp == null) { EditorUtility.DisplayDialog("Error", "Unit_10/Unit_10_Sections not found!", "OK"); return; }
            GameObject go = BuildOrCleanPanel(sp, "U10_COMPLETE_Panel", "COMPLETE");
            SetupCompletionPanel(go);
            var comp = go.GetComponent<U10_SA_CompletionPanel_Masters_Phonics>();
            if (comp != null) { comp.AutoBindHierarchyElements(); EditorUtility.SetDirty(comp); }
            EnsureGlobalNavigation(sp.parent, sp);
            var flow = sp.GetComponentInParent<U10_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) { flow.AutoBindHierarchyElements(); EditorUtility.SetDirty(flow); }
            EditorUtility.SetDirty(go);
            Debug.Log("<color=#10B981><b>[Unit 10]</b> Cleaned & Rebuilt Completion Panel!</color>");
        }

        [MenuItem("Masters Phonics/Unit 10/Panels/8. Clean & Setup Course Finale", false, 208)]
        public static void MenuSetupCourseFinale()
        {
            Transform sp = GetSectionsParent();
            if (sp == null) { EditorUtility.DisplayDialog("Error", "Unit_10/Unit_10_Sections not found!", "OK"); return; }
            GameObject go = BuildOrCleanPanel(sp, "U10_CourseFinale_Panel", "CourseFinale");
            SetupCourseFinale(go);
            var comp = go.GetComponent<U10_SA_CourseFinale_Masters_Phonics>();
            if (comp != null) { comp.AutoBindHierarchyElements(); EditorUtility.SetDirty(comp); }
            EnsureGlobalNavigation(sp.parent, sp);
            var flow = sp.GetComponentInParent<U10_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) { flow.AutoBindHierarchyElements(); EditorUtility.SetDirty(flow); }
            EditorUtility.SetDirty(go);
            Debug.Log("<color=#10B981><b>[Unit 10]</b> Cleaned & Rebuilt Course Finale Panel!</color>");
        }

        // =========================================================================
        // Specific Panel Builders (Full-Fidelity GameObject Hierarchies)
        // =========================================================================

        private static void SetupGM01ConceptCards(GameObject go)
        {
            StripOldActivityComponents(go);
            var comp = go.GetComponent<U10_SA_GM01_ConceptCards_Masters_Phonics>() ?? go.AddComponent<U10_SA_GM01_ConceptCards_Masters_Phonics>();

            EnsureStandardHUD(go.transform, "LEARN — MEANING & ROOTS", "Homonyms, Homophones & Homographs");

            // Cards Container
            GameObject cardsContainer = new GameObject("Cards", typeof(RectTransform));
            cardsContainer.transform.SetParent(go.transform, false);
            RectTransform crt = cardsContainer.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;

            // Main Active Card Display
            GameObject cGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cGo.transform.SetParent(cardsContainer.transform, false);
            RectTransform rt = cGo.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -10);
            rt.sizeDelta = new Vector2(880, 480);
            U10_UI_Utils.ApplyRoundedCardStyle(cGo.GetComponent<Image>(), new Color(0.96f, 0.98f, 1f));
            Transform card = cGo.transform;

            // Heading (Size 38, Bold)
            GameObject hGo = new GameObject("Heading", typeof(RectTransform), typeof(TextMeshProUGUI));
            hGo.transform.SetParent(card, false);
            RectTransform hRt = hGo.GetComponent<RectTransform>();
            hRt.anchoredPosition = new Vector2(0, 185);
            hRt.sizeDelta = new Vector2(800, 50);
            var hTxt = hGo.GetComponent<TextMeshProUGUI>();
            hTxt.text = "<b>Break the Word Apart</b>";
            hTxt.fontSize = 38;
            hTxt.fontStyle = FontStyles.Bold;
            hTxt.alignment = TextAlignmentOptions.Center;
            hTxt.color = new Color(0.08f, 0.15f, 0.28f);

            // SubHeading (Size 24, Bold)
            GameObject shGo = new GameObject("SubHeading", typeof(RectTransform), typeof(TextMeshProUGUI));
            shGo.transform.SetParent(card, false);
            RectTransform shRt = shGo.GetComponent<RectTransform>();
            shRt.anchoredPosition = new Vector2(0, 140);
            shRt.sizeDelta = new Vector2(800, 35);
            var shTxt = shGo.GetComponent<TextMeshProUGUI>();
            shTxt.text = "<b>The words tell you what they mean!</b>";
            shTxt.fontSize = 24;
            shTxt.fontStyle = FontStyles.Bold;
            shTxt.alignment = TextAlignmentOptions.Center;
            shTxt.color = new Color(0.12f, 0.38f, 0.75f);

            // Body (Size 26, Readable)
            GameObject bGo = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bGo.transform.SetParent(card, false);
            RectTransform bRt = bGo.GetComponent<RectTransform>();
            bRt.anchoredPosition = new Vector2(0, 30);
            bRt.sizeDelta = new Vector2(800, 160);
            var bTxt = bGo.GetComponent<TextMeshProUGUI>();
            bTxt.text = "<b>homo-</b> is a prefix meaning <b>same</b>.\n\n" +
                        "• <b>homo + nym</b> = same + name (same word)\n" +
                        "• <b>homo + phone</b> = same + sound (same sound)\n" +
                        "• <b>homo + graph</b> = same + writing (same spelling)";
            bTxt.fontSize = 26;
            bTxt.alignment = TextAlignmentOptions.Center;
            bTxt.color = new Color(0.15f, 0.2f, 0.3f);

            // Example Rows Container
            GameObject exGo = new GameObject("ExampleRows", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            exGo.transform.SetParent(card, false);
            RectTransform exRt = exGo.GetComponent<RectTransform>();
            exRt.anchoredPosition = new Vector2(0, -90);
            exRt.sizeDelta = new Vector2(800, 55);
            var hlg = exGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 15;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            for (int i = 0; i < 4; i++)
            {
                CreateCardExampleButton(exGo.transform, $"Example_{i}", $"Example {i+1}", new Color(0.12f, 0.45f, 0.85f), 170);
            }

            // Poem Player Group (Interactive verse player for Card 4)
            GameObject ppGo = new GameObject("PoemPlayer", typeof(RectTransform));
            ppGo.transform.SetParent(card, false);
            RectTransform ppRt = ppGo.GetComponent<RectTransform>();
            ppRt.anchoredPosition = new Vector2(0, -170);
            ppRt.sizeDelta = new Vector2(800, 80);

            GameObject p1Go = new GameObject("Line1", typeof(RectTransform), typeof(TextMeshProUGUI));
            p1Go.transform.SetParent(ppGo.transform, false);
            p1Go.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 18);
            p1Go.GetComponent<RectTransform>().sizeDelta = new Vector2(760, 30);
            var p1Txt = p1Go.GetComponent<TextMeshProUGUI>();
            p1Txt.text = "I have such a <color=#10B981><b>fit</b></color> (tantrum)";
            p1Txt.fontSize = 22;
            p1Txt.fontStyle = FontStyles.Bold;
            p1Txt.alignment = TextAlignmentOptions.Center;
            p1Txt.color = new Color(0.15f, 0.25f, 0.4f);

            GameObject p2Go = new GameObject("Line2", typeof(RectTransform), typeof(TextMeshProUGUI));
            p2Go.transform.SetParent(ppGo.transform, false);
            p2Go.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -14);
            p2Go.GetComponent<RectTransform>().sizeDelta = new Vector2(760, 30);
            var p2Txt = p2Go.GetComponent<TextMeshProUGUI>();
            p2Txt.text = "When these words don't <color=#10B981><b>fit</b></color> (match)";
            p2Txt.fontSize = 22;
            p2Txt.fontStyle = FontStyles.Bold;
            p2Txt.alignment = TextAlignmentOptions.Center;
            p2Txt.color = new Color(0.15f, 0.25f, 0.4f);

            // Replay Button (Using project art/icon or cloned button)
            Button abBtn = CreateOrCloneNavButton(card, "ReplayAudioBtn", "Assets/Icons/audio icon.png", "LISTEN", new Vector2(-280, 185), new Vector2(160, 48), new Color(0.2f, 0.4f, 0.7f));

            // Navigation Buttons (Prev & Next with project art sprites)
            Button nbBtn = CreateOrCloneNavButton(go.transform, "NextCard_Btn", "Assets/Icons/nextt.png", "NEXT >>", new Vector2(340, -265), new Vector2(240, 65), new Color(0.12f, 0.75f, 0.38f));
            Button pbBtn = CreateOrCloneNavButton(go.transform, "PrevCard_Btn", "Assets/Icons/backbutton.png", "<< PREV", new Vector2(-340, -265), new Vector2(200, 65), new Color(0.25f, 0.35f, 0.5f));

            comp.AutoBindHierarchyElements();
        }

        private static void SetupGM03SoundTwins(GameObject go)
        {
            StripOldActivityComponents(go);
            var comp = go.GetComponent<U10_SA_GM03_SoundTwins_Masters_Phonics>() ?? go.AddComponent<U10_SA_GM03_SoundTwins_Masters_Phonics>();

            EnsureStandardHUD(go.transform, "SOUND TWINS — HOMOPHONE MATCH", "Tap two words that sound the same");

            // HUD Badges: Round Banner & Match Counter
            GameObject rbgGo = new GameObject("RoundBadge", typeof(RectTransform), typeof(Image));
            rbgGo.transform.SetParent(go.transform, false);
            RectTransform rbgRt = rbgGo.GetComponent<RectTransform>();
            rbgRt.anchoredPosition = new Vector2(-280, 175);
            rbgRt.sizeDelta = new Vector2(200, 42);
            U10_UI_Utils.ApplyRoundedButtonStyle(rbgGo.GetComponent<Image>(), new Color(0.18f, 0.35f, 0.65f));

            GameObject rbgTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            rbgTxt.transform.SetParent(rbgGo.transform, false);
            rbgTxt.GetComponent<RectTransform>().sizeDelta = rbgRt.sizeDelta;
            var rbt = rbgTxt.GetComponent<TextMeshProUGUI>();
            rbt.text = "<b>ROUND A</b>";
            rbt.fontSize = 20;
            rbt.fontStyle = FontStyles.Bold;
            rbt.alignment = TextAlignmentOptions.Center;
            rbt.color = Color.white;

            // 16-Card Grid Container (4x4)
            GameObject gridGo = new GameObject("GridContainer", typeof(RectTransform), typeof(GridLayoutGroup));
            gridGo.transform.SetParent(go.transform, false);
            RectTransform rt = gridGo.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -35);
            rt.sizeDelta = new Vector2(780, 360);
            var glg = gridGo.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(175, 76);
            glg.spacing = new Vector2(16, 14);
            glg.childAlignment = TextAnchor.MiddleCenter;

            // Pre-instantiate all 16 Card Tiles in Editor Hierarchy
            string[] sampleWords = new[]
            {
                "way", "weigh", "bare", "bear",
                "knight", "night", "piece", "peace",
                "flour", "flower", "stair", "stare",
                "tale", "tail", "sun", "son"
            };

            for (int i = 1; i <= 16; i++)
            {
                GameObject tileGo = new GameObject($"CardTile_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                tileGo.transform.SetParent(gridGo.transform, false);
                tileGo.GetComponent<RectTransform>().sizeDelta = new Vector2(175, 76);
                U10_UI_Utils.ApplyRoundedCardStyle(tileGo.GetComponent<Image>(), new Color(0.96f, 0.98f, 1f));

                GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGo.transform.SetParent(tileGo.transform, false);
                txtGo.GetComponent<RectTransform>().sizeDelta = new Vector2(165, 66);
                var txt = txtGo.GetComponent<TextMeshProUGUI>();
                txt.text = $"<b>{sampleWords[i - 1]}</b>";
                txt.fontSize = 28;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = new Color(0.08f, 0.15f, 0.28f);

                GameObject outlineGo = new GameObject("SelectedOutline", typeof(RectTransform), typeof(Image));
                outlineGo.transform.SetParent(tileGo.transform, false);
                RectTransform ort = outlineGo.GetComponent<RectTransform>();
                ort.anchorMin = Vector2.zero;
                ort.anchorMax = Vector2.one;
                ort.offsetMin = new Vector2(-4, -4);
                ort.offsetMax = new Vector2(4, 4);
                var oImg = outlineGo.GetComponent<Image>();
                oImg.color = new Color(0.12f, 0.75f, 0.38f, 0.85f);
                outlineGo.SetActive(false);
            }

            // Match Feedback Banner
            GameObject fbGo = new GameObject("FeedbackBanner", typeof(RectTransform), typeof(TextMeshProUGUI));
            fbGo.transform.SetParent(go.transform, false);
            RectTransform fbRt = fbGo.GetComponent<RectTransform>();
            fbRt.anchoredPosition = new Vector2(0, -245);
            fbRt.sizeDelta = new Vector2(700, 45);
            var fbTxt = fbGo.GetComponent<TextMeshProUGUI>();
            fbTxt.text = "<b>MATCH FOUND! <color=#10B981>way</color> = <color=#10B981>weigh</color></b>";
            fbTxt.fontSize = 24;
            fbTxt.fontStyle = FontStyles.Bold;
            fbTxt.alignment = TextAlignmentOptions.Center;
            fbTxt.color = new Color(1f, 0.85f, 0.3f);
            fbGo.SetActive(false);

            comp.AutoBindHierarchyElements();
        }

        private static void SetupGM04WhichOneFits(GameObject go)
        {
            StripOldActivityComponents(go);
            var comp = go.GetComponent<U10_SA_GM04_WhichOneFits_Masters_Phonics>() ?? go.AddComponent<U10_SA_GM04_WhichOneFits_Masters_Phonics>();

            EnsureStandardHUD(go.transform, "WHICH ONE FITS? — MEANING IN CONTEXT", "Pick the word that fits the sentence");

            // Sentence Card
            GameObject cGo = new GameObject("SentenceCard", typeof(RectTransform), typeof(Image));
            cGo.transform.SetParent(go.transform, false);
            RectTransform rt = cGo.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 95);
            rt.sizeDelta = new Vector2(880, 150);
            U10_UI_Utils.ApplyRoundedCardStyle(cGo.GetComponent<Image>(), new Color(0.12f, 0.18f, 0.28f, 0.95f));

            GameObject stGo = new GameObject("SentenceText", typeof(RectTransform), typeof(TextMeshProUGUI));
            stGo.transform.SetParent(cGo.transform, false);
            stGo.GetComponent<RectTransform>().sizeDelta = new Vector2(820, 90);
            var stTxt = stGo.GetComponent<TextMeshProUGUI>();
            stTxt.text = "<b>I ______ fruit at the supermarket.</b>";
            stTxt.fontSize = 32;
            stTxt.fontStyle = FontStyles.Bold;
            stTxt.alignment = TextAlignmentOptions.Center;
            stTxt.color = Color.white;

            // "Both sound the same [audio]" Button
            GameObject abGo = new GameObject("PlayBothBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            abGo.transform.SetParent(go.transform, false);
            RectTransform abRt = abGo.GetComponent<RectTransform>();
            abRt.anchoredPosition = new Vector2(0, 2);
            abRt.sizeDelta = new Vector2(320, 48);
            U10_UI_Utils.ApplyRoundedButtonStyle(abGo.GetComponent<Image>(), new Color(0.15f, 0.4f, 0.8f));

            GameObject abTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            abTxt.transform.SetParent(abGo.transform, false);
            abTxt.GetComponent<RectTransform>().sizeDelta = abRt.sizeDelta;
            var at = abTxt.GetComponent<TextMeshProUGUI>();
            at.text = "<b>BOTH SOUND THE SAME</b>";
            at.fontSize = 20;
            at.fontStyle = FontStyles.Bold;
            at.alignment = TextAlignmentOptions.Center;
            at.color = Color.white;

            // Options Container
            GameObject optGo = new GameObject("OptionsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            optGo.transform.SetParent(go.transform, false);
            RectTransform ort = optGo.GetComponent<RectTransform>();
            ort.anchoredPosition = new Vector2(0, -95);
            ort.sizeDelta = new Vector2(880, 100);
            var hlg = optGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 25;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            string[] optionLabels = new[] { "way", "weigh", "whey" };
            for (int i = 1; i <= 3; i++)
            {
                GameObject btnGo = new GameObject($"Option_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(optGo.transform, false);
                btnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 75);
                U10_UI_Utils.ApplyRoundedButtonStyle(btnGo.GetComponent<Image>(), new Color(0.15f, 0.22f, 0.35f, 0.95f));

                GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGO.transform.SetParent(btnGo.transform, false);
                txtGO.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 75);
                var oTxt = txtGO.GetComponent<TextMeshProUGUI>();
                oTxt.text = $"<b>{optionLabels[i - 1]}</b>";
                oTxt.fontSize = 28;
                oTxt.fontStyle = FontStyles.Bold;
                oTxt.alignment = TextAlignmentOptions.Center;
                oTxt.color = Color.white;
            }

            // Wrong Comparison Panel (Hidden by default)
            GameObject cpGo = new GameObject("ComparisonPanel", typeof(RectTransform), typeof(Image));
            cpGo.transform.SetParent(go.transform, false);
            RectTransform cpRt = cpGo.GetComponent<RectTransform>();
            cpRt.anchoredPosition = new Vector2(0, -220);
            cpRt.sizeDelta = new Vector2(880, 95);
            U10_UI_Utils.ApplyRoundedCardStyle(cpGo.GetComponent<Image>(), new Color(0.08f, 0.12f, 0.2f, 0.98f));

            GameObject wtGo = new GameObject("WrongText", typeof(RectTransform), typeof(TextMeshProUGUI));
            wtGo.transform.SetParent(cpGo.transform, false);
            wtGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 18);
            wtGo.GetComponent<RectTransform>().sizeDelta = new Vector2(820, 40);
            var wt = wtGo.GetComponent<TextMeshProUGUI>();
            wt.text = "<s>I way fruit at the supermarket.</s>";
            wt.fontSize = 22;
            wt.alignment = TextAlignmentOptions.Center;
            wt.color = new Color(0.95f, 0.4f, 0.4f);

            GameObject ctGo = new GameObject("CorrectText", typeof(RectTransform), typeof(TextMeshProUGUI));
            ctGo.transform.SetParent(cpGo.transform, false);
            ctGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -18);
            ctGo.GetComponent<RectTransform>().sizeDelta = new Vector2(820, 40);
            var ct = ctGo.GetComponent<TextMeshProUGUI>();
            ct.text = "<b>I weigh fruit at the supermarket.</b>";
            ct.fontSize = 24;
            ct.alignment = TextAlignmentOptions.Center;
            ct.color = new Color(0.12f, 0.75f, 0.38f);

            cpGo.SetActive(false);

            comp.AutoBindHierarchyElements();
        }

        private static void SetupGM06TwoMeanings(GameObject go)
        {
            StripOldActivityComponents(go);
            var comp = go.GetComponent<U10_SA_GM06_TwoMeanings_Masters_Phonics>() ?? go.AddComponent<U10_SA_GM06_TwoMeanings_Masters_Phonics>();

            EnsureStandardHUD(go.transform, "TWO MEANINGS — HOMONYM EXPLORER", "Which meaning is the sentence using?");

            // Sentence Card
            GameObject cGo = new GameObject("SentenceCard", typeof(RectTransform), typeof(Image));
            cGo.transform.SetParent(go.transform, false);
            RectTransform rt = cGo.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 110);
            rt.sizeDelta = new Vector2(860, 140);
            U10_UI_Utils.ApplyRoundedCardStyle(cGo.GetComponent<Image>(), new Color(0.12f, 0.18f, 0.28f, 0.95f));

            GameObject stGo = new GameObject("SentenceText", typeof(RectTransform), typeof(TextMeshProUGUI));
            stGo.transform.SetParent(cGo.transform, false);
            stGo.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 80);
            var stTxt = stGo.GetComponent<TextMeshProUGUI>();
            stTxt.text = "<b>The bat flew out of the cave.</b>";
            stTxt.fontSize = 32;
            stTxt.fontStyle = FontStyles.Bold;
            stTxt.alignment = TextAlignmentOptions.Center;
            stTxt.color = Color.white;

            // Prompt
            GameObject qpGo = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
            qpGo.transform.SetParent(go.transform, false);
            RectTransform qpRt = qpGo.GetComponent<RectTransform>();
            qpRt.anchoredPosition = new Vector2(0, 15);
            qpRt.sizeDelta = new Vector2(800, 40);
            var qpTxt = qpGo.GetComponent<TextMeshProUGUI>();
            qpTxt.text = "<b>Which \"bat\" is this?</b>";
            qpTxt.fontSize = 26;
            qpTxt.fontStyle = FontStyles.Bold;
            qpTxt.alignment = TextAlignmentOptions.Center;
            qpTxt.color = new Color(1f, 0.85f, 0.3f);

            // Meanings Container (2 Choice Cards)
            GameObject mGo = new GameObject("MeaningsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            mGo.transform.SetParent(go.transform, false);
            RectTransform mrt = mGo.GetComponent<RectTransform>();
            mrt.anchoredPosition = new Vector2(0, -110);
            mrt.sizeDelta = new Vector2(860, 160);
            var hlg = mGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 30;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // Meaning Card A
            GameObject maGo = new GameObject("MeaningCard_A", typeof(RectTransform), typeof(Image), typeof(Button));
            maGo.transform.SetParent(mGo.transform, false);
            maGo.GetComponent<RectTransform>().sizeDelta = new Vector2(380, 140);
            U10_UI_Utils.ApplyRoundedCardStyle(maGo.GetComponent<Image>(), new Color(0.15f, 0.22f, 0.35f, 0.95f));

            GameObject maIcon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            maIcon.transform.SetParent(maGo.transform, false);
            maIcon.GetComponent<RectTransform>().anchoredPosition = new Vector2(-120, 0);
            maIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 80);
            maIcon.GetComponent<Image>().preserveAspect = true;

            GameObject maTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            maTxt.transform.SetParent(maGo.transform, false);
            maTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(50, 0);
            maTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 100);
            var mat = maTxt.GetComponent<TextMeshProUGUI>();
            mat.text = "<b>A nocturnal flying mammal with wings</b>";
            mat.fontSize = 20;
            mat.alignment = TextAlignmentOptions.Center;
            mat.color = Color.white;

            // Meaning Card B
            GameObject mbGo = new GameObject("MeaningCard_B", typeof(RectTransform), typeof(Image), typeof(Button));
            mbGo.transform.SetParent(mGo.transform, false);
            mbGo.GetComponent<RectTransform>().sizeDelta = new Vector2(380, 140);
            U10_UI_Utils.ApplyRoundedCardStyle(mbGo.GetComponent<Image>(), new Color(0.15f, 0.22f, 0.35f, 0.95f));

            GameObject mbIcon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            mbIcon.transform.SetParent(mbGo.transform, false);
            mbIcon.GetComponent<RectTransform>().anchoredPosition = new Vector2(-120, 0);
            mbIcon.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 80);
            mbIcon.GetComponent<Image>().preserveAspect = true;

            GameObject mbTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            mbTxt.transform.SetParent(mbGo.transform, false);
            mbTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(50, 0);
            mbTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(240, 100);
            var mbt = mbTxt.GetComponent<TextMeshProUGUI>();
            mbt.text = "<b>A wooden club used to hit a baseball</b>";
            mbt.fontSize = 20;
            mbt.alignment = TextAlignmentOptions.Center;
            mbt.color = Color.white;

            comp.AutoBindHierarchyElements();
        }

        private static void SetupGM04sSayItTwoWays(GameObject go)
        {
            StripOldActivityComponents(go);
            var comp = go.GetComponent<U10_SA_GM04s_SayItTwoWays_Masters_Phonics>() ?? go.AddComponent<U10_SA_GM04s_SayItTwoWays_Masters_Phonics>();

            EnsureStandardHUD(go.transform, "SAY IT TWO WAYS — HOMOGRAPH SELECTOR", "How do you pronounce this word in the sentence?");

            // Sentence Card
            GameObject cGo = new GameObject("SentenceCard", typeof(RectTransform), typeof(Image));
            cGo.transform.SetParent(go.transform, false);
            RectTransform rt = cGo.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 110);
            rt.sizeDelta = new Vector2(860, 140);
            U10_UI_Utils.ApplyRoundedCardStyle(cGo.GetComponent<Image>(), new Color(0.12f, 0.18f, 0.28f, 0.95f));

            GameObject stGo = new GameObject("SentenceText", typeof(RectTransform), typeof(TextMeshProUGUI));
            stGo.transform.SetParent(cGo.transform, false);
            stGo.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 80);
            var stTxt = stGo.GetComponent<TextMeshProUGUI>();
            stTxt.text = "<b>I read that book last year.</b>";
            stTxt.fontSize = 32;
            stTxt.fontStyle = FontStyles.Bold;
            stTxt.alignment = TextAlignmentOptions.Center;
            stTxt.color = Color.white;

            // Prompt
            GameObject qpGo = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
            qpGo.transform.SetParent(go.transform, false);
            RectTransform qpRt = qpGo.GetComponent<RectTransform>();
            qpRt.anchoredPosition = new Vector2(0, 15);
            qpRt.sizeDelta = new Vector2(800, 40);
            var qpTxt = qpGo.GetComponent<TextMeshProUGUI>();
            qpTxt.text = "<b>How do you say \"read\" here?</b>";
            qpTxt.fontSize = 26;
            qpTxt.fontStyle = FontStyles.Bold;
            qpTxt.alignment = TextAlignmentOptions.Center;
            qpTxt.color = new Color(1f, 0.85f, 0.3f);

            // Dual Pronunciation Buttons Container
            GameObject optGo = new GameObject("OptionsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            optGo.transform.SetParent(go.transform, false);
            RectTransform ort = optGo.GetComponent<RectTransform>();
            ort.anchoredPosition = new Vector2(0, -90);
            ort.sizeDelta = new Vector2(860, 120);
            var hlg = optGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 30;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // Button A
            GameObject baGo = new GameObject("PronounceBtn_A", typeof(RectTransform), typeof(Image), typeof(Button));
            baGo.transform.SetParent(optGo.transform, false);
            baGo.GetComponent<RectTransform>().sizeDelta = new Vector2(380, 90);
            U10_UI_Utils.ApplyRoundedButtonStyle(baGo.GetComponent<Image>(), new Color(0.15f, 0.22f, 0.35f, 0.95f));

            GameObject baTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            baTxt.transform.SetParent(baGo.transform, false);
            baTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(340, 80);
            var bat = baTxt.GetComponent<TextMeshProUGUI>();
            bat.text = "<b>((( \"red\" (past) )))</b>";
            bat.fontSize = 24;
            bat.fontStyle = FontStyles.Bold;
            bat.alignment = TextAlignmentOptions.Center;
            bat.color = Color.white;

            // Button B
            GameObject bbGo = new GameObject("PronounceBtn_B", typeof(RectTransform), typeof(Image), typeof(Button));
            bbGo.transform.SetParent(optGo.transform, false);
            bbGo.GetComponent<RectTransform>().sizeDelta = new Vector2(380, 90);
            U10_UI_Utils.ApplyRoundedButtonStyle(bbGo.GetComponent<Image>(), new Color(0.15f, 0.22f, 0.35f, 0.95f));

            GameObject bbTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            bbTxt.transform.SetParent(bbGo.transform, false);
            bbTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(340, 80);
            var bbt = bbTxt.GetComponent<TextMeshProUGUI>();
            bbt.text = "<b>((( \"reed\" (present) )))</b>";
            bbt.fontSize = 24;
            bbt.fontStyle = FontStyles.Bold;
            bbt.alignment = TextAlignmentOptions.Center;
            bbt.color = Color.white;

            comp.AutoBindHierarchyElements();
        }

        private static void SetupUnitChallenge(GameObject go)
        {
            StripOldActivityComponents(go);
            var comp = go.GetComponent<U10_SA_UnitChallenge_Masters_Phonics>() ?? go.AddComponent<U10_SA_UnitChallenge_Masters_Phonics>();

            EnsureStandardHUD(go.transform, "UNIT CHALLENGE — HOMONYMS & HOMOPHONES", "Course Final Mastery Challenge");

            // Prompt Text
            GameObject prGo = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
            prGo.transform.SetParent(go.transform, false);
            RectTransform rt = prGo.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 100);
            rt.sizeDelta = new Vector2(900, 90);
            var txt = prGo.GetComponent<TextMeshProUGUI>();
            txt.text = "<b>Which word completes the sentence correctly?</b>";
            txt.fontSize = 32;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;

            // Replay Audio Speaker Button
            GameObject abGo = new GameObject("ReplayBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            abGo.transform.SetParent(go.transform, false);
            RectTransform abRt = abGo.GetComponent<RectTransform>();
            abRt.anchoredPosition = new Vector2(0, 35);
            abRt.sizeDelta = new Vector2(230, 48);
            U10_UI_Utils.ApplyRoundedButtonStyle(abGo.GetComponent<Image>(), new Color(0.15f, 0.4f, 0.8f));

            GameObject abTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            abTxt.transform.SetParent(abGo.transform, false);
            abTxt.GetComponent<RectTransform>().sizeDelta = abRt.sizeDelta;
            var at = abTxt.GetComponent<TextMeshProUGUI>();
            at.text = "<b>PLAY AUDIO</b>";
            at.fontSize = 22;
            at.fontStyle = FontStyles.Bold;
            at.alignment = TextAlignmentOptions.Center;
            at.color = Color.white;

            // Options Container
            GameObject optGo = new GameObject("OptionsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            optGo.transform.SetParent(go.transform, false);
            RectTransform ort = optGo.GetComponent<RectTransform>();
            ort.anchoredPosition = new Vector2(0, -75);
            ort.sizeDelta = new Vector2(650, 210);
            var vlg = optGo.GetComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 12;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            string[] optDefaults = new[] { "Option A", "Option B", "Option C" };
            for (int i = 1; i <= 3; i++)
            {
                GameObject btnGo = new GameObject($"Option_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(optGo.transform, false);
                btnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(650, 58);
                U10_UI_Utils.ApplyRoundedButtonStyle(btnGo.GetComponent<Image>(), new Color(0.15f, 0.22f, 0.35f, 0.95f));

                GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGO.transform.SetParent(btnGo.transform, false);
                txtGO.GetComponent<RectTransform>().sizeDelta = new Vector2(650, 58);
                var oTxt = txtGO.GetComponent<TextMeshProUGUI>();
                oTxt.text = $"<b>{optDefaults[i - 1]}</b>";
                oTxt.fontSize = 26;
                oTxt.fontStyle = FontStyles.Bold;
                oTxt.alignment = TextAlignmentOptions.Center;
                oTxt.color = Color.white;
            }

            comp.AutoBindHierarchyElements();
        }

        private static void SetupCompletionPanel(GameObject go)
        {
            StripOldActivityComponents(go);
            var comp = go.GetComponent<U10_SA_CompletionPanel_Masters_Phonics>() ?? go.AddComponent<U10_SA_CompletionPanel_Masters_Phonics>();

            EnsureStandardHUD(go.transform, "UNIT 10 COMPLETE!", "Meaning & Morphology Mastery");

            GameObject cGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cGo.transform.SetParent(go.transform, false);
            RectTransform rt = cGo.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(760, 520);
            U10_UI_Utils.ApplyRoundedCardStyle(cGo.GetComponent<Image>(), new Color(0.12f, 0.18f, 0.28f, 0.96f));
            Transform card = cGo.transform;

            // Word Twin Badge
            GameObject biGo = new GameObject("BadgeIcon", typeof(RectTransform), typeof(Image));
            biGo.transform.SetParent(card, false);
            RectTransform biRt = biGo.GetComponent<RectTransform>();
            biRt.anchoredPosition = new Vector2(0, 115);
            biRt.sizeDelta = new Vector2(140, 140);
            var biImg = biGo.GetComponent<Image>();
            biImg.preserveAspect = true;

            GameObject blGo = new GameObject("BadgeLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            blGo.transform.SetParent(card, false);
            RectTransform blRt = blGo.GetComponent<RectTransform>();
            blRt.anchoredPosition = new Vector2(0, 30);
            blRt.sizeDelta = new Vector2(300, 35);
            var blTxt = blGo.GetComponent<TextMeshProUGUI>();
            blTxt.text = "<b>Word Twin Master</b>";
            blTxt.fontSize = 22;
            blTxt.fontStyle = FontStyles.Bold;
            blTxt.alignment = TextAlignmentOptions.Center;
            blTxt.color = new Color(1f, 0.85f, 0.3f);

            // Score Display
            GameObject scGO = new GameObject("ScoreText", typeof(RectTransform), typeof(TextMeshProUGUI));
            scGO.transform.SetParent(card, false);
            RectTransform scRt = scGO.GetComponent<RectTransform>();
            scRt.anchoredPosition = new Vector2(0, -30);
            scRt.sizeDelta = new Vector2(400, 45);
            var scTxt = scGO.GetComponent<TextMeshProUGUI>();
            scTxt.text = "<b>+450 pts</b>";
            scTxt.fontSize = 32;
            scTxt.fontStyle = FontStyles.Bold;
            scTxt.alignment = TextAlignmentOptions.Center;
            scTxt.color = new Color(0.2f, 0.8f, 1f);

            // Stats
            GameObject wsGO = new GameObject("WordsPairedText", typeof(RectTransform), typeof(TextMeshProUGUI));
            wsGO.transform.SetParent(card, false);
            RectTransform wsRt = wsGO.GetComponent<RectTransform>();
            wsRt.anchoredPosition = new Vector2(-150, -85);
            wsRt.sizeDelta = new Vector2(280, 40);
            var wsTxt = wsGO.GetComponent<TextMeshProUGUI>();
            wsTxt.text = "<b>Word Twins: 30</b>";
            wsTxt.fontSize = 22;
            wsTxt.fontStyle = FontStyles.Bold;
            wsTxt.alignment = TextAlignmentOptions.Center;
            wsTxt.color = new Color(0.9f, 0.93f, 0.98f);

            GameObject accGO = new GameObject("AccuracyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            accGO.transform.SetParent(card, false);
            RectTransform accRt = accGO.GetComponent<RectTransform>();
            accRt.anchoredPosition = new Vector2(150, -85);
            accRt.sizeDelta = new Vector2(280, 40);
            var accTxt = accGO.GetComponent<TextMeshProUGUI>();
            accTxt.text = "<b>Accuracy: 100%</b>";
            accTxt.fontSize = 22;
            accTxt.fontStyle = FontStyles.Bold;
            accTxt.alignment = TextAlignmentOptions.Center;
            accTxt.color = new Color(0.9f, 0.93f, 0.98f);

            // Action Buttons
            Button cbBtn = CreateOrCloneNavButton(card, "ContinueBtn", "Assets/Art/continue button.png", "COURSE FINALE >>", new Vector2(160, -170), new Vector2(280, 65), new Color(0.12f, 0.75f, 0.38f));
            Button rbBtn = CreateOrCloneNavButton(card, "ReplayUnitBtn", "Assets/Icons/backbutton.png", "<< REPLAY UNIT", new Vector2(-160, -170), new Vector2(280, 65), new Color(0.2f, 0.45f, 0.75f));

            comp.AutoBindHierarchyElements();
        }

        private static void SetupCourseFinale(GameObject go)
        {
            StripOldActivityComponents(go);
            var comp = go.GetComponent<U10_SA_CourseFinale_Masters_Phonics>() ?? go.AddComponent<U10_SA_CourseFinale_Masters_Phonics>();

            EnsureStandardHUD(go.transform, "COURSE COMPLETE — MASTER PHONICS GRADUATION", "All 10 Units & 7 Syllable Types Mastered!");

            // 1. Badge Wall Card (Left Side)
            GameObject bwGo = new GameObject("BadgeWallCard", typeof(RectTransform), typeof(Image));
            bwGo.transform.SetParent(go.transform, false);
            RectTransform bwRt = bwGo.GetComponent<RectTransform>();
            bwRt.anchoredPosition = new Vector2(-280, 20);
            bwRt.sizeDelta = new Vector2(460, 380);
            U10_UI_Utils.ApplyRoundedCardStyle(bwGo.GetComponent<Image>(), new Color(0.12f, 0.18f, 0.28f, 0.96f));

            GameObject bwTitle = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            bwTitle.transform.SetParent(bwGo.transform, false);
            bwTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 155);
            bwTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(420, 40);
            var bwt = bwTitle.GetComponent<TextMeshProUGUI>();
            bwt.text = "<b>ALL 11 COURSE BADGES</b>";
            bwt.fontSize = 24;
            bwt.fontStyle = FontStyles.Bold;
            bwt.alignment = TextAlignmentOptions.Center;
            bwt.color = new Color(1f, 0.85f, 0.3f);

            // 11-Badge Grid Container
            GameObject bgGrid = new GameObject("BadgeWall", typeof(RectTransform), typeof(GridLayoutGroup));
            bgGrid.transform.SetParent(bwGo.transform, false);
            RectTransform bgRt = bgGrid.GetComponent<RectTransform>();
            bgRt.anchoredPosition = new Vector2(0, -15);
            bgRt.sizeDelta = new Vector2(420, 270);
            var glg = bgGrid.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(64, 64);
            glg.spacing = new Vector2(16, 12);
            glg.childAlignment = TextAnchor.MiddleCenter;

            // Pre-create all 11 Badge slots
            string[] badgeNames = new[]
            {
                "Unit 1: Affix Ace", "Unit 2: Plural Pro", "Unit 3: Syllable Scout", "Unit 4: Stress Star",
                "Unit 5: Open Master", "Unit 6: Magic E", "Unit 7: Team Titan", "Unit 8: Bossy R",
                "Unit 9: Turtle Whiz", "Unit 10: Word Twin", "Grand Scholar"
            };

            for (int i = 1; i <= 11; i++)
            {
                GameObject slotGo = new GameObject($"Badge_{i}", typeof(RectTransform), typeof(Image));
                slotGo.transform.SetParent(bgGrid.transform, false);
                slotGo.GetComponent<RectTransform>().sizeDelta = new Vector2(64, 64);
                var img = slotGo.GetComponent<Image>();
                img.preserveAspect = true;
                U10_UI_Utils.ApplyRoundedButtonStyle(img, new Color(0.2f, 0.35f, 0.6f, 0.9f));
            }

            // 2. Stats & Certificate Container (Right Side)
            GameObject certGo = new GameObject("CertificatePanel", typeof(RectTransform), typeof(Image));
            certGo.transform.SetParent(go.transform, false);
            RectTransform crt = certGo.GetComponent<RectTransform>();
            crt.anchoredPosition = new Vector2(240, 20);
            crt.sizeDelta = new Vector2(500, 380);
            U10_UI_Utils.ApplyRoundedCardStyle(certGo.GetComponent<Image>(), new Color(0.96f, 0.98f, 1f));

            GameObject cTitle = new GameObject("CertTitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            cTitle.transform.SetParent(certGo.transform, false);
            cTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 145);
            cTitle.GetComponent<RectTransform>().sizeDelta = new Vector2(460, 45);
            var ct = cTitle.GetComponent<TextMeshProUGUI>();
            ct.text = "<b>CERTIFICATE OF MASTERY</b>";
            ct.fontSize = 26;
            ct.fontStyle = FontStyles.Bold;
            ct.alignment = TextAlignmentOptions.Center;
            ct.color = new Color(0.12f, 0.38f, 0.75f);

            GameObject snGo = new GameObject("StudentName", typeof(RectTransform), typeof(TextMeshProUGUI));
            snGo.transform.SetParent(certGo.transform, false);
            snGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 80);
            snGo.GetComponent<RectTransform>().sizeDelta = new Vector2(460, 50);
            var snt = snGo.GetComponent<TextMeshProUGUI>();
            snt.text = "<b>MASTER SCHOLAR</b>";
            snt.fontSize = 32;
            snt.fontStyle = FontStyles.Bold;
            snt.alignment = TextAlignmentOptions.Center;
            snt.color = new Color(0.1f, 0.15f, 0.25f);

            GameObject bdGo = new GameObject("BodyText", typeof(RectTransform), typeof(TextMeshProUGUI));
            bdGo.transform.SetParent(certGo.transform, false);
            bdGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 15);
            bdGo.GetComponent<RectTransform>().sizeDelta = new Vector2(460, 45);
            var bdt = bdGo.GetComponent<TextMeshProUGUI>();
            bdt.text = "Has mastered all 10 Phonics Units and 7 Syllable Types!";
            bdt.fontSize = 18;
            bdt.alignment = TextAlignmentOptions.Center;
            bdt.color = new Color(0.2f, 0.3f, 0.45f);

            GameObject dtGo = new GameObject("DateText", typeof(RectTransform), typeof(TextMeshProUGUI));
            dtGo.transform.SetParent(certGo.transform, false);
            dtGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -40);
            dtGo.GetComponent<RectTransform>().sizeDelta = new Vector2(460, 35);
            var dtt = dtGo.GetComponent<TextMeshProUGUI>();
            dtt.text = "Awarded: Course Completion";
            dtt.fontSize = 18;
            dtt.alignment = TextAlignmentOptions.Center;
            dtt.color = new Color(0.4f, 0.5f, 0.65f);

            // Lifetime Stats Container (Under Certificate)
            GameObject statsGo = new GameObject("StatsCard", typeof(RectTransform));
            statsGo.transform.SetParent(certGo.transform, false);
            RectTransform srt = statsGo.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(0, -110);
            srt.sizeDelta = new Vector2(460, 60);

            string[] statNames = new string[] { "WordsRead_Text", "WordsBuilt_Text", "WordsSplit_Text", "SyllableTypes_Text", "Badges_Text" };
            string[] statDefaults = new string[] { "Read: 1,240", "Built: 310", "Split: 75", "7/7 Types", "11/11 Badges" };
            for (int i = 0; i < statNames.Length; i++)
            {
                GameObject sGO = new GameObject(statNames[i], typeof(RectTransform), typeof(TextMeshProUGUI));
                sGO.transform.SetParent(statsGo.transform, false);
                RectTransform stRt = sGO.GetComponent<RectTransform>();
                stRt.anchoredPosition = new Vector2(-180 + (i * 90), 0);
                stRt.sizeDelta = new Vector2(85, 30);
                var txt = sGO.GetComponent<TextMeshProUGUI>();
                txt.text = $"<b>{statDefaults[i]}</b>";
                txt.fontSize = 14;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = new Color(0.15f, 0.35f, 0.6f);
            }

            // Return to Lessons Button
            Button jbBtn = CreateOrCloneNavButton(go.transform, "JourneyMapBtn", "Assets/Art/continue button.png", "RETURN TO LESSONS >>", new Vector2(0, -250), new Vector2(380, 65), new Color(0.12f, 0.75f, 0.38f));

            comp.AutoBindHierarchyElements();
        }

        // =========================================================================
        // UI Helpers
        // =========================================================================

        private static void EnsureStandardHUD(Transform root, string titleText, string subtitleText = "Homonyms, Homophones & Homographs")
        {
            Transform hud = root.Find("ProgressHUD") ?? root.Find("HUD") ?? root.Find("Header_Container");
            if (hud == null)
            {
                GameObject hudGo = new GameObject("ProgressHUD", typeof(RectTransform));
                hudGo.transform.SetParent(root, false);
                RectTransform rt = hudGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.anchoredPosition = new Vector2(0, -50);
                rt.sizeDelta = new Vector2(0, 100);
                hud = hudGo.transform;
            }

            // Title Text
            Transform tTr = hud.Find("Title_Text") ?? hud.Find("TitleText") ?? hud.Find("Title");
            TextMeshProUGUI txt = null;
            if (tTr == null)
            {
                GameObject tGo = new GameObject("Title_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                tGo.transform.SetParent(hud, false);
                RectTransform tRt = tGo.GetComponent<RectTransform>();
                tRt.anchoredPosition = new Vector2(0, 15);
                tRt.sizeDelta = new Vector2(950, 50);
                txt = tGo.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                txt = tTr.GetComponent<TextMeshProUGUI>();
            }

            if (txt != null)
            {
                txt.text = $"<b>{titleText}</b>";
                txt.fontSize = 38;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.white;
            }

            // Subtitle Text
            Transform sTr = hud.Find("Subtitle_Text") ?? hud.Find("SubtitleText") ?? hud.Find("Subtitle");
            TextMeshProUGUI sTxt = null;
            if (sTr == null)
            {
                GameObject sGo = new GameObject("Subtitle_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                sGo.transform.SetParent(hud, false);
                RectTransform sRt = sGo.GetComponent<RectTransform>();
                sRt.anchoredPosition = new Vector2(0, -22);
                sRt.sizeDelta = new Vector2(950, 35);
                sTxt = sGo.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                sTxt = sTr.GetComponent<TextMeshProUGUI>();
            }

            if (sTxt != null)
            {
                sTxt.text = $"<b>{subtitleText}</b>";
                sTxt.fontSize = 22;
                sTxt.fontStyle = FontStyles.Bold;
                sTxt.alignment = TextAlignmentOptions.Center;
                sTxt.color = new Color(0.75f, 0.9f, 1f);
            }

            // Ensure Electric Cyan Progress Bar Slider
            Transform pbTr = hud.Find("ProgressBar") ?? hud.Find("Progress_Slider") ?? root.Find("ProgressBar") ?? root.Find("Progress_Slider");
            if (pbTr == null)
            {
                GameObject pbGo = new GameObject("ProgressBar", typeof(RectTransform), typeof(Slider));
                pbGo.transform.SetParent(hud, false);
                RectTransform pbRt = pbGo.GetComponent<RectTransform>();
                pbRt.anchoredPosition = new Vector2(0, -52);
                pbRt.sizeDelta = new Vector2(500, 14);

                var slider = pbGo.GetComponent<Slider>();
                slider.interactable = false;
                slider.transition = Selectable.Transition.None;

                // Background
                GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bgGo.transform.SetParent(pbGo.transform, false);
                RectTransform bgRt = bgGo.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.sizeDelta = Vector2.zero;
                U10_UI_Utils.ApplyRoundedCardStyle(bgGo.GetComponent<Image>(), new Color(0.1f, 0.15f, 0.25f, 0.8f));

                // Fill Area
                GameObject faGo = new GameObject("Fill Area", typeof(RectTransform));
                faGo.transform.SetParent(pbGo.transform, false);
                RectTransform faRt = faGo.GetComponent<RectTransform>();
                faRt.anchorMin = Vector2.zero;
                faRt.anchorMax = Vector2.one;
                faRt.sizeDelta = new Vector2(-6, 0);

                // Fill
                GameObject fGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fGo.transform.SetParent(faGo.transform, false);
                RectTransform fRt = fGo.GetComponent<RectTransform>();
                fRt.anchorMin = Vector2.zero;
                fRt.anchorMax = new Vector2(1, 1);
                fRt.sizeDelta = Vector2.zero;
                var fImg = fGo.GetComponent<Image>();
                U10_UI_Utils.ApplyRoundedButtonStyle(fImg, new Color(0.12f, 0.85f, 0.95f)); // Electric cyan

                slider.fillRect = fRt;
                slider.targetGraphic = fImg;
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.value = 0f;
            }

            // Progress Text (Left side of HUD: e.g. "Pairs: 0/8", "Item 1 of 16")
            Transform pTr = hud.Find("Progress_Text") ?? hud.Find("ProgressText") ?? root.Find("Progress_Text") ?? root.Find("ProgressText");
            if (pTr == null)
            {
                GameObject pGo = new GameObject("Progress_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                pGo.transform.SetParent(hud, false);
                RectTransform pRt = pGo.GetComponent<RectTransform>();
                pRt.anchoredPosition = new Vector2(-380, 15);
                pRt.sizeDelta = new Vector2(240, 45);
                var pTxt = pGo.GetComponent<TextMeshProUGUI>();
                pTxt.text = "<b>Item 1 of 10</b>";
                pTxt.fontSize = 28;
                pTxt.fontStyle = FontStyles.Bold;
                pTxt.alignment = TextAlignmentOptions.Left;
                pTxt.color = new Color(0.75f, 0.9f, 1f);
            }

            // Score Text (Right side of HUD: e.g. "Score: 0")
            Transform scTr = hud.Find("Score_Text") ?? hud.Find("ScoreText") ?? root.Find("Score_Text") ?? root.Find("ScoreText");
            if (scTr == null)
            {
                GameObject scGo = new GameObject("Score_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                scGo.transform.SetParent(hud, false);
                RectTransform scRt = scGo.GetComponent<RectTransform>();
                scRt.anchoredPosition = new Vector2(380, 15);
                scRt.sizeDelta = new Vector2(240, 45);
                var scTxt = scGo.GetComponent<TextMeshProUGUI>();
                scTxt.text = "<b>Score: 0</b>";
                scTxt.fontSize = 28;
                scTxt.fontStyle = FontStyles.Bold;
                scTxt.alignment = TextAlignmentOptions.Right;
                scTxt.color = new Color(1f, 0.85f, 0.25f);
            }
        }

        private static void CreateCardExampleButton(Transform parent, string btnName, string label, Color bgColor, float width = 170)
        {
            if (parent.Find(btnName) != null) return;

            GameObject btnGo = new GameObject(btnName, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 50);
            U10_UI_Utils.ApplyRoundedButtonStyle(btnGo.GetComponent<Image>(), bgColor);

            GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(btnGo.transform, false);
            txtGO.GetComponent<RectTransform>().sizeDelta = rt.sizeDelta;
            var txt = txtGO.GetComponent<TextMeshProUGUI>();
            txt.text = $"<b>{label}</b>";
            txt.fontSize = 20;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
        }

        private static Button CreateOrCloneNavButton(Transform parent, string name, string iconPath, string label, Vector2 pos, Vector2 size, Color? fallbackColor = null)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<Button>();
            }

            // Try to find a working button template from other units to clone if possible
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Transform sourceUnit = canvas != null ? (canvas.transform.Find("Unit_9") ?? canvas.transform.Find("Unit_8") ?? canvas.transform.Find("Unit_7")) : null;
            Transform templateBtn = null;
            if (sourceUnit != null)
            {
                templateBtn = sourceUnit.Find($"Unit_9_Sections/{name}") 
                           ?? sourceUnit.Find($"Unit_8_Sections/{name}")
                           ?? sourceUnit.Find($"Unit_9_Sections/Activity_1_ConceptCards/{name}")
                           ?? sourceUnit.Find($"Unit_8_Sections/Activity_1_ConceptCards/{name}")
                           ?? sourceUnit.Find(name);
            }

            GameObject btnGo;
            if (templateBtn != null)
            {
                btnGo = Object.Instantiate(templateBtn.gameObject, parent, false);
                btnGo.name = name;
                RectTransform rt = btnGo.GetComponent<RectTransform>();
                if (pos != Vector2.zero) rt.anchoredPosition = pos;
                if (size != Vector2.zero) rt.sizeDelta = size;
            }
            else
            {
                btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(parent, false);
                RectTransform rt = btnGo.GetComponent<RectTransform>();
                rt.anchoredPosition = pos;
                rt.sizeDelta = size;

                Image img = btnGo.GetComponent<Image>();
                Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath)
                                 ?? AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/{iconPath}")
                                 ?? AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Icons/{iconPath}");
                if (iconSprite != null)
                {
                    img.sprite = iconSprite;
                    img.preserveAspect = true;
                }
                else
                {
                    Color col = fallbackColor ?? new Color(0.12f, 0.72f, 0.35f, 1f);
                    U10_UI_Utils.ApplyRoundedButtonStyle(img, col);
                }

                if (!string.IsNullOrEmpty(label))
                {
                    GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                    txtGO.transform.SetParent(btnGo.transform, false);
                    RectTransform trt = txtGO.GetComponent<RectTransform>();
                    trt.anchorMin = Vector2.zero;
                    trt.anchorMax = Vector2.one;
                    trt.offsetMin = Vector2.zero;
                    trt.offsetMax = Vector2.zero;
                    var txt = txtGO.GetComponent<TextMeshProUGUI>();
                    txt.text = $"<b>{label}</b>";
                    txt.fontSize = 24;
                    txt.fontStyle = FontStyles.Bold;
                    txt.alignment = TextAlignmentOptions.Center;
                    txt.color = Color.white;
                }
            }

            return btnGo.GetComponent<Button>();
        }

        private static void EnsureGlobalNavigation(Transform root, Transform sectionsParent)
        {
            if (sectionsParent == null) return;

            // Find source unit (Unit_9, Unit_8, Unit_7, etc.) for button cloning
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Transform sourceUnit = canvas != null ? (canvas.transform.Find("Unit_9") ?? canvas.transform.Find("Unit_8") ?? canvas.transform.Find("Unit_7") ?? canvas.transform.Find("Unit_6")) : null;
            Transform sourceSections = sourceUnit != null ? (sourceUnit.Find("Unit_9_Sections") ?? sourceUnit.Find("Unit_8_Sections") ?? sourceUnit.Find("Unit_7_Sections") ?? sourceUnit.Find("Sections")) : null;

            // Global Back Button (Small Back on HUD)
            Transform backTr = sectionsParent.Find("GlobalBack_Btn") ?? sectionsParent.Find("Btn_Back") ?? sectionsParent.Find("Back_Btn") ?? root.Find("GlobalBack_Btn");
            if (backTr == null)
            {
                Transform sourceBack = sourceSections != null ? (sourceSections.Find("GlobalBack_Btn") ?? sourceSections.Find("Btn_Back") ?? sourceSections.Find("Back_Button") ?? sourceSections.Find("Back_Btn") ?? sourceSections.Find("Back")) : null;
                if (sourceBack != null)
                {
                    GameObject backGo = Object.Instantiate(sourceBack.gameObject, sectionsParent);
                    backGo.name = "GlobalBack_Btn";
                    backTr = backGo.transform;
                }
                else
                {
                    Button b = CreateOrCloneNavButton(sectionsParent, "GlobalBack_Btn", "Assets/Icons/backbutton.png", "<< BACK", new Vector2(40, -30), new Vector2(160, 55), new Color(0.2f, 0.3f, 0.45f));
                    RectTransform rt = b.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0, 1);
                    rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1);
                    rt.anchoredPosition = new Vector2(40, -30);
                    backTr = b.transform;
                }
            }
            backTr.gameObject.SetActive(true);
            backTr.SetAsLastSibling();

            // Dynamic Next Section Button
            Transform nextTr = sectionsParent.Find("NextActivity_Btn") ?? sectionsParent.Find("Btn_Next") ?? sectionsParent.Find("Next_Button main") ?? sectionsParent.Find("Next_Btn") ?? root.Find("NextActivity_Btn");
            if (nextTr == null)
            {
                Transform sourceNext = sourceSections != null ? (sourceSections.Find("NextActivity_Btn") ?? sourceSections.Find("Btn_Next") ?? sourceSections.Find("Next_Button main") ?? sourceSections.Find("Next_Button") ?? sourceSections.Find("Next_Btn")) : null;
                if (sourceNext != null)
                {
                    GameObject nextGo = Object.Instantiate(sourceNext.gameObject, sectionsParent);
                    nextGo.name = "NextActivity_Btn";
                    nextTr = nextGo.transform;
                }
                else
                {
                    Button nb = CreateOrCloneNavButton(sectionsParent, "NextActivity_Btn", "Assets/Icons/nextt.png", "NEXT >>", new Vector2(-40, 30), new Vector2(240, 65), new Color(0.12f, 0.75f, 0.38f));
                    RectTransform rt = nb.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(1, 0);
                    rt.anchorMax = new Vector2(1, 0);
                    rt.pivot = new Vector2(1, 0);
                    rt.anchoredPosition = new Vector2(-40, 30);
                    nextTr = nb.transform;
                }
            }
            nextTr.gameObject.SetActive(false);
            nextTr.SetAsLastSibling();
        }

        private static void EnsureActivityCompletionDialog(Transform root)
        {
            Transform dTr = root.Find("ActivityCompletionDialog");
            if (dTr != null) return;

            // Try to clone from previous unit
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Transform sourceUnit = canvas != null ? (canvas.transform.Find("Unit_9") ?? canvas.transform.Find("Unit_8") ?? canvas.transform.Find("Unit_7")) : null;
            Transform sourceDialog = sourceUnit != null ? (sourceUnit.Find("ActivityCompletionDialog") ?? sourceUnit.Find("Unit_9_Sections/ActivityCompletionDialog") ?? sourceUnit.Find("Unit_8_Sections/ActivityCompletionDialog")) : null;

            if (sourceDialog != null)
            {
                GameObject dGo = Object.Instantiate(sourceDialog.gameObject, root);
                dGo.name = "ActivityCompletionDialog";
                dGo.SetActive(false);
                return;
            }

            GameObject dialogGo = new GameObject("ActivityCompletionDialog", typeof(RectTransform), typeof(Image));
            dialogGo.transform.SetParent(root, false);
            RectTransform rt = dialogGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            U10_UI_Utils.ApplyRoundedCardStyle(dialogGo.GetComponent<Image>(), new Color(0.04f, 0.08f, 0.16f, 0.88f));

            // Dialog Card
            GameObject cGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cGo.transform.SetParent(dialogGo.transform, false);
            RectTransform crt = cGo.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(650, 420);
            U10_UI_Utils.ApplyRoundedCardStyle(cGo.GetComponent<Image>(), new Color(0.12f, 0.18f, 0.28f, 0.98f));

            // Title
            GameObject tGo = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            tGo.transform.SetParent(cGo.transform, false);
            tGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 135);
            tGo.GetComponent<RectTransform>().sizeDelta = new Vector2(580, 50);
            var txt = tGo.GetComponent<TextMeshProUGUI>();
            txt.text = "<b>ACTIVITY COMPLETE!</b>";
            txt.fontSize = 36;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(1f, 0.85f, 0.3f);

            // Stars Row
            GameObject sGo = new GameObject("Stars", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            sGo.transform.SetParent(cGo.transform, false);
            sGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30);
            sGo.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 110);
            var hlg = sGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 20;

            Sprite goldStar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png.png")
                           ?? AssetDatabase.LoadAssetAtPath<Sprite>($"{ART_BASE_PATH}/StarFish.png");
            for (int i = 1; i <= 3; i++)
            {
                GameObject starGo = new GameObject($"Star{i}", typeof(RectTransform), typeof(Image));
                starGo.transform.SetParent(sGo.transform, false);
                starGo.GetComponent<RectTransform>().sizeDelta = new Vector2(90, 90);
                var img = starGo.GetComponent<Image>();
                img.sprite = goldStar;
                img.preserveAspect = true;
            }

            // Score
            GameObject scGo = new GameObject("ScoreText", typeof(RectTransform), typeof(TextMeshProUGUI));
            scGo.transform.SetParent(cGo.transform, false);
            scGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -60);
            scGo.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 45);
            var scTxt = scGo.GetComponent<TextMeshProUGUI>();
            scTxt.text = "<b>+450 pts</b>";
            scTxt.fontSize = 32;
            scTxt.fontStyle = FontStyles.Bold;
            scTxt.alignment = TextAlignmentOptions.Center;
            scTxt.color = new Color(0.2f, 0.8f, 1f);

            // Next Button
            Button nbBtn = CreateOrCloneNavButton(cGo.transform, "NextBtn", "Assets/Art/continue button.png", "NEXT ACTIVITY >>", new Vector2(0, -135), new Vector2(280, 65), new Color(0.12f, 0.75f, 0.38f));

            dialogGo.SetActive(false);
        }

        private static Transform DuplicateAndSetupUnit10Signboard(GameObject u10Go, Canvas canvas)
        {
            Transform existingSel = u10Go.transform.Find("Unit_10_Section_Selection_Panels") 
                                 ?? u10Go.transform.Find("Section_Selection_Panels")
                                 ?? u10Go.transform.Find("Signboard");

            // CRITICAL: If the user has already created or customized the Section Selection Panel, DO NOT destroy or alter it!
            if (existingSel != null)
            {
                Debug.Log("<color=#10B981><b>[Unit 10 Automator]</b> Preserving user's custom Unit_10_Section_Selection_Panels untouched.</color>");
                return existingSel;
            }

            Transform sourceUnit = canvas != null ? (canvas.transform.Find("Unit_9") ?? canvas.transform.Find("Unit_8") ?? canvas.transform.Find("Unit_7") ?? canvas.transform.Find("Unit_6")) : null;
            Transform sourceSel = sourceUnit != null ? (sourceUnit.Find($"{sourceUnit.name}_Section_Selection_Panels") ?? sourceUnit.Find("Section_Selection_Panels") ?? sourceUnit.Find("Signboard")) : null;

            GameObject selGo;
            if (sourceSel != null)
            {
                selGo = Object.Instantiate(sourceSel.gameObject, u10Go.transform);
                selGo.name = "Unit_10_Section_Selection_Panels";
                Undo.RegisterCreatedObjectUndo(selGo, "Duplicate Unit 9 Signboard for Unit 10");
            }
            else
            {
                selGo = new GameObject("Unit_10_Section_Selection_Panels", typeof(RectTransform));
                selGo.transform.SetParent(u10Go.transform, false);
            }

            RectTransform rt = selGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Update Header Typography
            Transform titleTr = selGo.transform.Find("Signboard/Title") 
                             ?? selGo.transform.Find("Title") 
                             ?? selGo.transform.Find("Header/Title") 
                             ?? selGo.transform.Find("Signboard_Title")
                             ?? selGo.transform.Find("Signboard/Header/Title");
            if (titleTr != null)
            {
                var tmp = titleTr.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = "<b>HOMONYMS, HOMOPHONES & HOMOGRAPHS</b>";
            }

            Transform unitNumTr = selGo.transform.Find("Signboard/UnitText") 
                               ?? selGo.transform.Find("UnitText") 
                               ?? selGo.transform.Find("Header/UnitText") 
                               ?? selGo.transform.Find("Unit_Number")
                               ?? selGo.transform.Find("Signboard/Header/UnitText");
            if (unitNumTr != null)
            {
                var tmp = unitNumTr.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = "<b>UNIT 10</b>";
            }

            // Find wagons and update labels
            string[] u10WagonTitles = new string[] {
                "Learn & Concepts",
                "Activity 1\nSound Twins",
                "Activity 2\nWhich One Fits?",
                "Activity 3\nTwo Meanings",
                "Activity 4\nSay It Two Ways"
            };

            for (int i = 1; i <= 5; i++)
            {
                Transform wTr = selGo.transform.Find($"Content/Wagon{i}") 
                             ?? selGo.transform.Find($"Wagons/Wagon{i}") 
                             ?? selGo.transform.Find($"Wagon{i}")
                             ?? selGo.transform.Find($"Content/Cart{i}")
                             ?? selGo.transform.Find($"Cart{i}")
                             ?? selGo.transform.Find($"Signboard/Content/Wagon{i}");
                if (wTr != null)
                {
                    var txt = wTr.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null)
                    {
                        txt.text = $"<b>{u10WagonTitles[i - 1]}</b>";
                    }
                }
            }

            // If wagons not found by specific name, find all wagon buttons under Content
            Transform content = selGo.transform.Find("Content") ?? selGo.transform.Find("Wagons") ?? selGo.transform.Find("Signboard/Content");
            if (content != null)
            {
                var btns = content.GetComponentsInChildren<Button>(true);
                for (int i = 0; i < btns.Length && i < u10WagonTitles.Length; i++)
                {
                    var txt = btns[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null) txt.text = $"<b>{u10WagonTitles[i]}</b>";
                }
            }

            // Ensure Main Back Button on Signboard is present
            Transform mainBack = selGo.transform.Find("Btn_Back") ?? selGo.transform.Find("Back_Btn") ?? selGo.transform.Find("Back");
            if (mainBack == null && sourceSel != null)
            {
                Transform srcBack = sourceSel.Find("Btn_Back") ?? sourceSel.Find("Back_Btn") ?? sourceSel.Find("Back");
                if (srcBack != null)
                {
                    GameObject bClone = Object.Instantiate(srcBack.gameObject, selGo.transform);
                    bClone.name = "Btn_Back";
                    mainBack = bClone.transform;
                }
            }
            if (mainBack != null)
            {
                mainBack.gameObject.SetActive(true);
                mainBack.SetAsLastSibling();
            }

            return selGo.transform;
        }

        // =========================================================================
        // Auto-Assignment Methods
        // =========================================================================

        [MenuItem("Masters Phonics/Unit 10/Auto-Assign All Unit 10 Inspectors", false, 101)]
        public static void AutoAssignAllUnit10Inspectors()
        {
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in Scene!", "OK");
                return;
            }

            Transform u10 = canvas.transform.Find("Unit_10");
            if (u10 == null)
            {
                EditorUtility.DisplayDialog("Error", "Unit_10 GameObject not found under Canvas! Please run 'Generate Unit 10 Complete Hierarchy' first.", "OK");
                return;
            }

            ClearCaches();
            AutoAssignAllInspectors(u10.gameObject);

            EditorUtility.SetDirty(u10.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(u10.gameObject.scene);

            Debug.Log("<color=#10B981><b>[Unit 10 Automator]</b> Auto-Assigned all Unit 10 Inspectors, Star Sprites, Badges, and Audio Clips successfully!</color>");
            EditorUtility.DisplayDialog("Unit 10 Inspectors Auto-Assigned!", 
                "Successfully auto-assigned Star rating sprites, Badge sprites, all SFX clips, and hierarchy bindings across all Unit 10 inspectors!", 
                "Awesome!");
        }

        [MenuItem("Masters Phonics/Unit 10/Clean Activity 3 Homonym Sprites (12 Only)", false, 102)]
        public static void CleanActivity3HomonymSprites()
        {
            var gm06 = UnityEngine.Object.FindFirstObjectByType<U10_SA_GM06_TwoMeanings_Masters_Phonics>();
            if (gm06 == null)
            {
                EditorUtility.DisplayDialog("Error", "U10_SA_GM06_TwoMeanings_Masters_Phonics component not found in active scene!", "OK");
                return;
            }

            var hSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/unit10_MP/sprites u10 mp.png");
            List<Sprite> homonymList = new List<Sprite>();
            if (hSprites != null)
            {
                foreach (var a in hSprites)
                {
                    if (a is Sprite s && s.name.StartsWith("u10_")) homonymList.Add(s);
                }
            }
            Undo.RecordObject(gm06, "Clean Activity 3 Sprites");
            gm06.homonymSprites = homonymList.ToArray();
            gm06.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm06);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gm06.gameObject.scene);

            Debug.Log($"<color=#10B981><b>[Unit 10 Automator]</b> Cleaned Activity 3 Homonym Sprites! Reduced to {gm06.homonymSprites.Length} exact sprites.</color>");
            EditorUtility.DisplayDialog("Activity 3 Sprites Cleaned!", 
                $"Successfully cleaned Sprite Bank! It now contains exactly {gm06.homonymSprites.Length} dedicated Unit 10 homonym sprites.", 
                "Great!");
        }

        [MenuItem("Masters Phonics/Tools/Remove Missing Scripts in Scene", false, 200)]
        public static void RemoveMissingScriptsInActiveScene()
        {
            int totalRemoved = 0;
            GameObject[] rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var root in rootObjects)
            {
                totalRemoved += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    totalRemoved += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
                }
            }
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            Debug.Log($"<color=#10B981><b>[Clean Missing Scripts]</b> Removed {totalRemoved} missing script component(s) from scene!</color>");
            EditorUtility.DisplayDialog("Missing Scripts Cleaned", $"Removed {totalRemoved} missing script component(s) from the scene!", "OK");
        }

        public static void AutoAssignAllInspectors(GameObject unit10Obj)
        {
            if (unit10Obj == null) return;
            RemoveMissingScriptsRecursively(unit10Obj);
            EnsureAllHUDAndPromptGameObjects(unit10Obj);
            SanitizeAllTextMeshProInHierarchy(unit10Obj);

            // 1. Load All Sprites from Assets/Art/unit10_MP and global shared textures
            List<Sprite> u10Sprites = LoadAllU10Sprites();
            Sprite FindSprite(params string[] keywords)
            {
                foreach (var s in u10Sprites)
                {
                    if (s == null) continue;
                    bool match = true;
                    foreach (var kw in keywords)
                    {
                        if (s.name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            match = false;
                            break;
                        }
                    }
                    if (match) return s;
                }
                return null;
            }

            // Star Sprites
            Sprite goldStar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png.png")
                           ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/TOPIC SELECTION/STAR.png")
                           ?? FindSprite("star", "gold");
            Sprite greyStar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png (1).png")
                           ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/TOPIC SELECTION/STAR_GREY.png")
                           ?? FindSprite("star", "grey");

            var flow = unit10Obj.GetComponent<U10_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null)
            {
                if (goldStar != null) flow.earnedGoldStarSprite = goldStar;
                if (greyStar != null) flow.emptyGreyStarSprite = greyStar;
                flow.AutoBindHierarchyElements();
                EditorUtility.SetDirty(flow);
            }

            var audio = unit10Obj.GetComponent<U10_SA_AudioManager_Masters_Phonics>();
            if (audio != null)
            {
                audio.EditorAutoAssignAudioClipsAndMascot();
                EditorUtility.SetDirty(audio);
            }

            var gm01 = unit10Obj.GetComponentInChildren<U10_SA_GM01_ConceptCards_Masters_Phonics>(true);
            if (gm01 != null) { gm01.AutoBindHierarchyElements(); EditorUtility.SetDirty(gm01); }

            var gm03 = unit10Obj.GetComponentInChildren<U10_SA_GM03_SoundTwins_Masters_Phonics>(true);
            if (gm03 != null) { gm03.AutoBindHierarchyElements(); EditorUtility.SetDirty(gm03); }

            var gm04 = unit10Obj.GetComponentInChildren<U10_SA_GM04_WhichOneFits_Masters_Phonics>(true);
            if (gm04 != null) { gm04.AutoBindHierarchyElements(); EditorUtility.SetDirty(gm04); }

            var gm06 = unit10Obj.GetComponentInChildren<U10_SA_GM06_TwoMeanings_Masters_Phonics>(true);
            if (gm06 != null)
            {
                var hSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/unit10_MP/sprites u10 mp.png");
                List<Sprite> homonymList = new List<Sprite>();
                if (hSprites != null)
                {
                    foreach (var a in hSprites)
                    {
                        if (a is Sprite s && s.name.StartsWith("u10_")) homonymList.Add(s);
                    }
                }
                gm06.homonymSprites = homonymList.ToArray();
                gm06.AutoBindHierarchyElements();
                EditorUtility.SetDirty(gm06);
            }

            var gm04s = unit10Obj.GetComponentInChildren<U10_SA_GM04s_SayItTwoWays_Masters_Phonics>(true);
            if (gm04s != null) { gm04s.AutoBindHierarchyElements(); EditorUtility.SetDirty(gm04s); }

            var challenge = unit10Obj.GetComponentInChildren<U10_SA_UnitChallenge_Masters_Phonics>(true);
            if (challenge != null) { challenge.AutoBindHierarchyElements(); EditorUtility.SetDirty(challenge); }

            var comp = unit10Obj.GetComponentInChildren<U10_SA_CompletionPanel_Masters_Phonics>(true);
            if (comp != null)
            {
                Sprite wordTwin = FindSprite("Word_Twin") ?? FindSprite("WordTwin") ?? FindSprite("twin") ?? FindSprite("badge");
                if (wordTwin != null)
                {
                    comp.wordTwinBadgeSprite = wordTwin;
                    if (comp.wordTwinBadgeImage != null) comp.wordTwinBadgeImage.sprite = wordTwin;
                }
                comp.AutoBindHierarchyElements();
                EditorUtility.SetDirty(comp);
            }

            var finale = unit10Obj.GetComponentInChildren<U10_SA_CourseFinale_Masters_Phonics>(true);
            if (finale != null) { finale.AutoBindHierarchyElements(); EditorUtility.SetDirty(finale); }

            // 8. Auto-Sync Master Train Router (Unit_Selection_Panel_Masters_Phonics)
            var router = Object.FindFirstObjectByType<Unit_Selection_Panel_Masters_Phonics>(FindObjectsInactive.Include);
            if (router != null)
            {
                router.EnsureInitUnitParents();
                EditorUtility.SetDirty(router);

                // Wire Bullet Train Carriage 10 Button under Lessons Signboard
                Canvas parentCanvas = unit10Obj.GetComponentInParent<Canvas>(true) ?? Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
                if (parentCanvas != null)
                {
                    Transform lessons = parentCanvas.transform.Find("Lessons") ?? parentCanvas.transform.Find("Lessons_Panel") ?? parentCanvas.transform.Find("LessonsPanel");
                    if (lessons != null)
                    {
                        Transform c10 = lessons.Find("Content/Unit_10") 
                                     ?? lessons.Find("Content/Unit10") 
                                     ?? lessons.Find("Unit_10") 
                                     ?? lessons.Find("Unit10") 
                                     ?? lessons.Find("Wagons/Wagon10") 
                                     ?? lessons.Find("Content/Wagon10") 
                                     ?? lessons.Find("Content/Cart10") 
                                     ?? lessons.Find("Cart10")
                                     ?? lessons.Find("Signboard/Content/Wagon10");
                        if (c10 != null)
                        {
                            var btn = c10.GetComponent<Button>() ?? c10.GetComponentInChildren<Button>(true);
                            if (btn != null)
                            {
                                btn.onClick.RemoveAllListeners();
                                btn.onClick.AddListener(router.OpenUnit10);
                                EditorUtility.SetDirty(btn);
                            }
                        }
                    }
                }
            }

            // Wire Section Selection Wagon Buttons to Router and Flow Manager
            Transform selPanels = unit10Obj.transform.Find("Unit_10_Section_Selection_Panels") 
                               ?? unit10Obj.transform.Find("Section_Selection_Panels");
            if (selPanels != null)
            {
                // Wagon 1: Learn Concept Cards -> Activity 1
                Transform sA = selPanels.Find("Viewport/Content/Section A Panel") ?? selPanels.Find("Content/Section A Panel");
                var btnA = sA != null ? (sA.GetComponent<Button>() ?? sA.GetComponentInChildren<Button>(true)) : null;
                if (btnA != null)
                {
                    btnA.onClick.RemoveAllListeners();
                    if (router != null) btnA.onClick.AddListener(router.OpenUnit10SectionA);
                    else btnA.onClick.AddListener(flow.OpenLearnSection);
                    EditorUtility.SetDirty(btnA);
                }

                // Wagon 2: Activity 2 (Which One Fits?)
                Transform sB = selPanels.Find("Viewport/Content/Section B Panel") ?? selPanels.Find("Content/Section B Panel");
                var btnB = sB != null ? (sB.GetComponent<Button>() ?? sB.GetComponentInChildren<Button>(true)) : null;
                if (btnB != null)
                {
                    btnB.onClick.RemoveAllListeners();
                    if (router != null) btnB.onClick.AddListener(router.OpenUnit10SectionB);
                    else btnB.onClick.AddListener(flow.OpenActivity2);
                    EditorUtility.SetDirty(btnB);
                }

                // Wagon 3: Activity 3 (Two Meanings)
                Transform sC = selPanels.Find("Viewport/Content/Section C Panel") ?? selPanels.Find("Content/Section C Panel");
                var btnC = sC != null ? (sC.GetComponent<Button>() ?? sC.GetComponentInChildren<Button>(true)) : null;
                if (btnC != null)
                {
                    btnC.onClick.RemoveAllListeners();
                    if (router != null) btnC.onClick.AddListener(router.OpenUnit10SectionC);
                    else btnC.onClick.AddListener(flow.OpenActivity3);
                    EditorUtility.SetDirty(btnC);
                }

                // Wagon 4: Activity 4 (Say It Two Ways)
                Transform sD = selPanels.Find("Viewport/Content/Section D Panel") ?? selPanels.Find("Content/Section D Panel");
                var btnD = sD != null ? (sD.GetComponent<Button>() ?? sD.GetComponentInChildren<Button>(true)) : null;
                if (btnD != null)
                {
                    btnD.onClick.RemoveAllListeners();
                    if (router != null) btnD.onClick.AddListener(router.OpenUnit10SectionD);
                    else btnD.onClick.AddListener(flow.OpenActivity4);
                    EditorUtility.SetDirty(btnD);
                }

                // Wagon 5: Unit Challenge & Course Finale
                Transform sE = selPanels.Find("Viewport/Content/Section E Panel") ?? selPanels.Find("Content/Section E Panel");
                var btnE = sE != null ? (sE.GetComponent<Button>() ?? sE.GetComponentInChildren<Button>(true)) : null;
                if (btnE != null)
                {
                    btnE.onClick.RemoveAllListeners();
                    if (router != null) btnE.onClick.AddListener(router.OpenUnit10SectionE);
                    else btnE.onClick.AddListener(flow.OpenUnitChallenge);
                    EditorUtility.SetDirty(btnE);
                }

                // Back Button: Returns to Train / Lessons Panel
                Transform backBtnTr = selPanels.Find("Viewport/Back_Button_Unit_10_Panel") 
                                   ?? selPanels.Find("Back_Button_Unit_10_Panel") 
                                   ?? selPanels.Find("Back_Button");
                var backBtn = backBtnTr != null ? (backBtnTr.GetComponent<Button>() ?? backBtnTr.GetComponentInChildren<Button>(true)) : null;
                if (backBtn != null)
                {
                    backBtn.onClick.RemoveAllListeners();
                    if (router != null) backBtn.onClick.AddListener(router.CloseAllUnit10Sections);
                    else backBtn.onClick.AddListener(() => selPanels.gameObject.SetActive(false));
                    EditorUtility.SetDirty(backBtn);
                }
            }

            Debug.Log("<color=#10B981><b>[Unit 10 Automator]</b> Auto-Assigned all Unit 10 Inspector components, Unit Selection Router, and Audio Clips successfully!</color>");
        }

        private static List<Sprite> LoadAllU10Sprites()
        {
            List<Sprite> list = new List<Sprite>();
            string[] paths = new[]
            {
                "Assets/Art/unit10_MP/badges and ui sprties u10 mp.png",
                "Assets/Art/unit10_MP/mp u10 spritess.png",
                "Assets/Art/unit10_MP/sprites u10 mp.png"
            };

            foreach (string p in paths)
            {
                var assets = AssetDatabase.LoadAllAssetsAtPath(p);
                if (assets != null)
                {
                    foreach (var a in assets)
                    {
                        if (a is Sprite s && !list.Contains(s)) list.Add(s);
                    }
                }
            }

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Art/unit10_MP", "Assets/Art" });
            foreach (var g in guids)
            {
                string p = AssetDatabase.GUIDToAssetPath(g);
                var assets = AssetDatabase.LoadAllAssetsAtPath(p);
                if (assets != null)
                {
                    foreach (var a in assets)
                    {
                        if (a is Sprite s && !list.Contains(s)) list.Add(s);
                    }
                }
            }
            return list;
        }

        private static void StripOldUnitComponents(GameObject unitObj)
        {
            var comps = unitObj.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var c in comps)
            {
                if (c == null) continue;
                string typeName = c.GetType().Name;
                if ((typeName.StartsWith("U") && typeName.Contains("_SA_") && !typeName.StartsWith("U10_")) ||
                    typeName.StartsWith("U1_") || typeName.StartsWith("U2_") || typeName.StartsWith("U3_") ||
                    typeName.StartsWith("U4_") || typeName.StartsWith("U5_") || typeName.StartsWith("U6_") ||
                    typeName.StartsWith("U7_") || typeName.StartsWith("U8_") || typeName.StartsWith("U9_"))
                {
                    UnityEngine.Object.DestroyImmediate(c);
                }
            }
        }

        private static void StripOldActivityComponents(GameObject target)
        {
            var comps = target.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var c in comps)
            {
                if (c == null) continue;
                string typeName = c.GetType().Name;
                if ((typeName.StartsWith("U") && typeName.Contains("_SA_") && !typeName.StartsWith("U10_")) ||
                    typeName.StartsWith("U1_") || typeName.StartsWith("U2_") || typeName.StartsWith("U3_") ||
                    typeName.StartsWith("U4_") || typeName.StartsWith("U5_") || typeName.StartsWith("U6_") ||
                    typeName.StartsWith("U7_") || typeName.StartsWith("U8_") || typeName.StartsWith("U9_"))
                {
                    UnityEngine.Object.DestroyImmediate(c);
                }
            }
        }

        public static int RemoveMissingScriptsRecursively(GameObject root)
        {
            if (root == null) return 0;
            int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);
            foreach (Transform child in root.transform)
            {
                count += RemoveMissingScriptsRecursively(child.gameObject);
            }
            return count;
        }

        public static void SanitizeAllTextMeshProInHierarchy(GameObject root)
        {
            if (root == null) return;
            var tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in tmps)
            {
                if (t == null || string.IsNullOrEmpty(t.text)) continue;
                if (t.text.Contains("\U0001F50A") || t.text.Contains("🔊") || t.text.Contains("🔈") || t.text.Contains("🔉"))
                {
                    t.text = t.text.Replace("\U0001F50A", "")
                                   .Replace("🔊", "")
                                   .Replace("🔈", "")
                                   .Replace("🔉", "")
                                   .Trim();
                    EditorUtility.SetDirty(t);
                }
            }
        }

        [MenuItem("Masters Phonics/Unit 10/Debug/Dump Unit 10 State to File", false, 300)]
        public static void DumpUnit10HierarchyState()
        {
            Canvas canvas = UnityEngine.Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in active Scene!", "OK");
                return;
            }

            Transform u10 = canvas.transform.Find("Unit_10");
            if (u10 == null)
            {
                EditorUtility.DisplayDialog("Unit 10 Not Found", "No 'Unit_10' GameObject found under Canvas!", "OK");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("=== UNIT 10 SCENE HIERARCHY STATE DUMP ===");
            sb.AppendLine($"Timestamp: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Root: {u10.name} (Active: {u10.gameObject.activeSelf})");
            sb.AppendLine("--------------------------------------------");

            DumpTransformRecursive(u10, 0, sb);

            string outPath = "Assets/Documents/Unit_10_Hierarchy_Dump.txt";
            System.IO.File.WriteAllText(outPath, sb.ToString());
            AssetDatabase.Refresh();

            Debug.Log($"<color=#10B981><b>[Unit 10 Debug]</b> Dumped complete Unit 10 hierarchy to: {outPath}</color>");
            EditorUtility.DisplayDialog("Hierarchy Dump Complete!", 
                $"Hierarchy dump successfully written to:\n{outPath}\n\nAntigravity can now inspect your exact scene hierarchy!", 
                "OK");
        }

        private static void DumpTransformRecursive(Transform t, int indent, System.Text.StringBuilder sb)
        {
            string pad = new string(' ', indent * 2);
            var rect = t.GetComponent<RectTransform>();
            string posInfo = rect != null ? $" [Pos: {rect.anchoredPosition}, Size: {rect.sizeDelta}]" : "";
            string activeStr = t.gameObject.activeSelf ? "Active" : "Hidden";

            var comps = t.GetComponents<Component>();
            var compNames = new List<string>();
            foreach (var c in comps)
            {
                if (c == null) compNames.Add("MISSING_SCRIPT");
                else if (c is Transform || c is RectTransform) continue;
                else compNames.Add(c.GetType().Name);
            }
            string compStr = compNames.Count > 0 ? $" -> [{string.Join(", ", compNames)}]" : "";

            sb.AppendLine($"{pad}[{activeStr}] {t.name}{posInfo}{compStr}");

            for (int i = 0; i < t.childCount; i++)
            {
                DumpTransformRecursive(t.GetChild(i), indent + 1, sb);
            }
        }
    }
}
