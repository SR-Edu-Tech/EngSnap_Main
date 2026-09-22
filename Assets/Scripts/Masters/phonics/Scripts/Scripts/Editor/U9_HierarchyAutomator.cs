using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

namespace MastersPhonics.EditorTools
{
    public class U9_HierarchyAutomator : EditorWindow
    {
        private const string AUDIO_BASE_PATH = "Assets/Audio/U9_audio";
        private const string ART_BASE_PATH = "Assets/Art/unit9_MP";
        private const string SFX_BASE_PATH = "Assets/SFX";

        private static Dictionary<string, AudioClip> audioCache;

        [MenuItem("Masters Phonics/Unit 9/Generate Unit 9 Hierarchy (Clean & Complete)", false, 100)]
        public static void GenerateUnit9Hierarchy()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in the current Scene!", "OK");
                return;
            }

            Transform sourceUnit = canvas.transform.Find("Unit_8") 
                                ?? canvas.transform.Find("Unit_7") 
                                ?? canvas.transform.Find("Unit_6")
                                ?? canvas.transform.Find("Unit_5")
                                ?? canvas.transform.Find("Unit_4")
                                ?? canvas.transform.Find("Unit_3")
                                ?? canvas.transform.Find("Unit_2")
                                ?? canvas.transform.Find("Unit_1");

            // Check if Unit_9 already exists
            Transform existingU9 = canvas.transform.Find("Unit_9");
            GameObject unit9Obj;

            if (existingU9 != null)
            {
                unit9Obj = existingU9.gameObject;
            }
            else if (sourceUnit != null)
            {
                // Clone from existing Unit template
                unit9Obj = Object.Instantiate(sourceUnit.gameObject, canvas.transform);
                unit9Obj.name = "Unit_9";
                Undo.RegisterCreatedObjectUndo(unit9Obj, "Generate Clean Unit_9");
            }
            else
            {
                // Create fresh root
                unit9Obj = new GameObject("Unit_9", typeof(RectTransform));
                unit9Obj.transform.SetParent(canvas.transform, false);
                Undo.RegisterCreatedObjectUndo(unit9Obj, "Create Unit_9 Root");
            }

            RectTransform rootRT = unit9Obj.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;

            ClearCaches();
            RemoveMissingScriptsRecursively(unit9Obj);
            StripOldUnitComponents(unit9Obj);

            // Add Managers
            var flowManager = unit9Obj.GetComponent<U9_SA_UnitFlowManager_Masters_Phonics>() ?? unit9Obj.AddComponent<U9_SA_UnitFlowManager_Masters_Phonics>();
            var audioManager = unit9Obj.GetComponent<U9_SA_AudioManager_Masters_Phonics>() ?? unit9Obj.AddComponent<U9_SA_AudioManager_Masters_Phonics>();

            // Setup Section Selection (Signboard with 5 Wagons)
            Transform selPanels = unit9Obj.transform.Find("Unit_9_Section_Selection_Panels") 
                               ?? unit9Obj.transform.Find("Unit_8_Section_Selection_Panels") 
                               ?? unit9Obj.transform.Find("Unit_7_Section_Selection_Panels")
                               ?? unit9Obj.transform.Find("Section_Selection_Panels");
            if (selPanels == null)
            {
                GameObject spGo = new GameObject("Unit_9_Section_Selection_Panels", typeof(RectTransform));
                spGo.transform.SetParent(unit9Obj.transform, false);
                selPanels = spGo.transform;
            }
            selPanels.name = "Unit_9_Section_Selection_Panels";
            UpdateSignboardCards(selPanels);

            // Setup Sections Parent
            Transform sectionsParent = unit9Obj.transform.Find("Unit_9_Sections") 
                                    ?? unit9Obj.transform.Find("Unit_8_Sections") 
                                    ?? unit9Obj.transform.Find("Unit_7_Sections")
                                    ?? unit9Obj.transform.Find("Sections");
            if (sectionsParent == null)
            {
                GameObject secGo = new GameObject("Unit_9_Sections", typeof(RectTransform));
                secGo.transform.SetParent(unit9Obj.transform, false);
                sectionsParent = secGo.transform;
            }
            sectionsParent.name = "Unit_9_Sections";

            RectTransform secRT = sectionsParent.GetComponent<RectTransform>();
            secRT.anchorMin = Vector2.zero;
            secRT.anchorMax = Vector2.one;
            secRT.offsetMin = Vector2.zero;
            secRT.offsetMax = Vector2.zero;

            // Ensure BG_Image exists, is active, and is the first sibling
            Transform bgTr = sectionsParent.Find("BG_Image") ?? sectionsParent.Find("Background") ?? sectionsParent.Find("BG");
            if (bgTr == null)
            {
                GameObject bgObj = new GameObject("BG_Image", typeof(RectTransform), typeof(Image));
                bgObj.transform.SetParent(sectionsParent, false);
                bgTr = bgObj.transform;
                RectTransform bgRt = bgObj.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
            }
            bgTr.gameObject.name = "BG_Image";
            bgTr.gameObject.SetActive(true);
            bgTr.SetAsFirstSibling();

            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART_BASE_PATH}/BG_U9_MP.png") ?? LoadSpriteMatching("BG_U9_MP");
            Image bgImg = bgTr.GetComponent<Image>();
            if (bgImg != null)
            {
                if (bgSprite != null) bgImg.sprite = bgSprite;
                bgImg.color = Color.white;
            }

            // Build or Clean Dedicated Panels (Wipes all legacy Unit 8 child GameObjects)
            GameObject learnGo = BuildOrCleanPanel(sectionsParent, "U9_learn_Concept_Cards_Panel", "learn_Concept_Cards_Panel");
            SetupGM01ConceptCards(learnGo);

            GameObject act1Go = BuildOrCleanPanel(sectionsParent, "U9_Activity_1_Turtle_Rule", "Activity_1");
            SetupGM02sTurtleRule(act1Go);

            GameObject act2Go = BuildOrCleanPanel(sectionsParent, "U9_Activity_2_One_Or_Two", "Activity_2");
            SetupGM03sOneOrTwo(act2Go);

            GameObject act3Go = BuildOrCleanPanel(sectionsParent, "U9_Activity_3_Build_Ending", "Activity_3");
            SetupGM02BuildEnding(act3Go);

            GameObject act4Go = BuildOrCleanPanel(sectionsParent, "U9_Activity_4_Sentence_Hunt", "Activity_4");
            SetupGM06SentenceHunt(act4Go);

            GameObject challengeGo = BuildOrCleanPanel(sectionsParent, "U9_UnitChallenge_Panel", "UnitChallenge");
            SetupUnitChallenge(challengeGo);

            GameObject mapGo = BuildOrCleanPanel(sectionsParent, "U9_SevenTypesMap_Panel", "SevenTypesMap");
            SetupSevenTypesMap(mapGo);

            GameObject compGo = BuildOrCleanPanel(sectionsParent, "U9_COMPLETE_Panel", "COMPLETE");
            SetupCompletionPanel(compGo);

            // Clean up any remaining unadopted legacy activity panels (e.g. Activity 5 or 6 from older units)
            for (int i = sectionsParent.childCount - 1; i >= 0; i--)
            {
                Transform child = sectionsParent.GetChild(i);
                string cName = child.name;
                if (!cName.StartsWith("U9_") && cName != "BG_Image" && cName != "Background" && cName != "BG" && !cName.Contains("Back") && !cName.Contains("Next"))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

            // Ensure Global Navigation (Static Back Button + Dynamic Next Section Button)
            EnsureGlobalNavigation(unit9Obj.transform, sectionsParent);

            // Wire All Managers & Inspectors (Stars, Badges, SFX, Bindings)
            AutoAssignAllInspectors(unit9Obj);

            EditorUtility.SetDirty(unit9Obj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(unit9Obj.scene);

            Debug.Log("<color=#10B981><b>[Unit 9 Automator]</b> Unit 9 Complete Hierarchy, GameObjects, Sprites, and Audio successfully generated and wired!</color>");
            EditorUtility.DisplayDialog("Unit 9 Hierarchy Created!", 
                "Successfully generated clean Unit 9 GameObject hierarchies with all 8 panels and wired all Voice A, Voice B, and SFX audio clips!", 
                "Great!");
        }

        // =====================================================================
        // Individual Panel Menu Actions (Panel-Wise Workflow)
        // =====================================================================

        private static Transform GetSectionsParent()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return null;
            Transform u9 = canvas.transform.Find("Unit_9");
            if (u9 == null) return null;
            return u9.Find("Unit_9_Sections") ?? u9.Find("Unit_8_Sections") ?? u9.Find("Sections");
        }

        [MenuItem("Masters Phonics/Unit 9/Panels/1. Clean & Setup Learn Concept Cards", false, 201)]
        public static void MenuSetupLearnConceptCards()
        {
            Transform sp = GetSectionsParent();
            if (sp == null) { EditorUtility.DisplayDialog("Error", "Unit_9/Unit_9_Sections not found!", "OK"); return; }
            GameObject go = BuildOrCleanPanel(sp, "U9_learn_Concept_Cards_Panel", "learn_Concept_Cards_Panel");
            SetupGM01ConceptCards(go);
            var comp = go.GetComponent<U9_SA_GM01_ConceptCards_Masters_Phonics>();
            if (comp != null) AutoAssignConceptCards(comp);
            EnsureGlobalNavigation(sp.parent, sp);
            var flow = sp.GetComponentInParent<U9_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) AutoAssignFlowManager(flow);
            EditorUtility.SetDirty(go);
            Debug.Log("<color=#10B981><b>[Unit 9]</b> Cleaned & Rebuilt Learn Concept Cards Panel!</color>");
        }

        [MenuItem("Masters Phonics/Unit 9/Panels/2. Clean & Setup Activity 1 (Turtle Rule)", false, 202)]
        public static void MenuSetupActivity1()
        {
            Transform sp = GetSectionsParent();
            if (sp == null) { EditorUtility.DisplayDialog("Error", "Unit_9/Unit_9_Sections not found!", "OK"); return; }
            GameObject go = BuildOrCleanPanel(sp, "U9_Activity_1_Turtle_Rule", "Activity_1");
            SetupGM02sTurtleRule(go);
            var comp = go.GetComponent<U9_SA_GM02s_TurtleRule_Masters_Phonics>();
            if (comp != null) AutoAssignTurtleRule(comp);
            EnsureGlobalNavigation(sp.parent, sp);
            var flow = sp.GetComponentInParent<U9_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) AutoAssignFlowManager(flow);
            EditorUtility.SetDirty(go);
            Debug.Log("<color=#10B981><b>[Unit 9]</b> Cleaned & Rebuilt Activity 1 (Turtle Rule)!</color>");
        }

        [MenuItem("Masters Phonics/Unit 9/Panels/3. Clean & Setup Activity 2 (One or Two)", false, 203)]
        public static void MenuSetupActivity2()
        {
            Transform sp = GetSectionsParent();
            if (sp == null) { EditorUtility.DisplayDialog("Error", "Unit_9/Unit_9_Sections not found!", "OK"); return; }
            GameObject go = BuildOrCleanPanel(sp, "U9_Activity_2_One_Or_Two", "Activity_2");
            SetupGM03sOneOrTwo(go);
            var comp = go.GetComponent<U9_SA_GM03s_OneOrTwo_Masters_Phonics>();
            if (comp != null) AutoAssignOneOrTwo(comp);
            EnsureGlobalNavigation(sp.parent, sp);
            var flow = sp.GetComponentInParent<U9_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) AutoAssignFlowManager(flow);
            EditorUtility.SetDirty(go);
            Debug.Log("<color=#10B981><b>[Unit 9]</b> Cleaned & Rebuilt Activity 2 (One Or Two)!</color>");
        }

        [MenuItem("Masters Phonics/Unit 9/Panels/4. Clean & Setup Activity 3 (Build Ending)", false, 204)]
        public static void MenuSetupActivity3()
        {
            Transform sp = GetSectionsParent();
            if (sp == null) { EditorUtility.DisplayDialog("Error", "Unit_9/Unit_9_Sections not found!", "OK"); return; }
            GameObject go = BuildOrCleanPanel(sp, "U9_Activity_3_Build_Ending", "Activity_3");
            SetupGM02BuildEnding(go);
            var comp = go.GetComponent<U9_SA_GM02_BuildEnding_Masters_Phonics>();
            if (comp != null) AutoAssignBuildEnding(comp);
            EnsureGlobalNavigation(sp.parent, sp);
            var flow = sp.GetComponentInParent<U9_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) AutoAssignFlowManager(flow);
            EditorUtility.SetDirty(go);
            Debug.Log("<color=#10B981><b>[Unit 9]</b> Cleaned & Rebuilt Activity 3 (Build Ending)!</color>");
        }

        [MenuItem("Masters Phonics/Unit 9/Panels/5. Clean & Setup Activity 4 (Sentence Hunt)", false, 205)]
        public static void MenuSetupActivity4()
        {
            Transform sp = GetSectionsParent();
            if (sp == null) { EditorUtility.DisplayDialog("Error", "Unit_9/Unit_9_Sections not found!", "OK"); return; }
            GameObject go = BuildOrCleanPanel(sp, "U9_Activity_4_Sentence_Hunt", "Activity_4");
            SetupGM06SentenceHunt(go);
            var comp = go.GetComponent<U9_SA_GM06_SentenceHunt_Masters_Phonics>();
            if (comp != null) AutoAssignSentenceHunt(comp);
            EnsureGlobalNavigation(sp.parent, sp);
            var flow = sp.GetComponentInParent<U9_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) AutoAssignFlowManager(flow);
            EditorUtility.SetDirty(go);
            Debug.Log("<color=#10B981><b>[Unit 9]</b> Cleaned & Rebuilt Activity 4 (Sentence Hunt)!</color>");
        }

        [MenuItem("Masters Phonics/Unit 9/Panels/6. Clean & Setup Unit Challenge", false, 206)]
        public static void MenuSetupChallenge()
        {
            Transform sp = GetSectionsParent();
            if (sp == null) { EditorUtility.DisplayDialog("Error", "Unit_9/Unit_9_Sections not found!", "OK"); return; }
            GameObject go = BuildOrCleanPanel(sp, "U9_UnitChallenge_Panel", "UnitChallenge");
            SetupUnitChallenge(go);
            var comp = go.GetComponent<U9_SA_UnitChallenge_Masters_Phonics>();
            if (comp != null) AutoAssignUnitChallenge(comp);
            EnsureGlobalNavigation(sp.parent, sp);
            var flow = sp.GetComponentInParent<U9_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) AutoAssignFlowManager(flow);
            EditorUtility.SetDirty(go);
            Debug.Log("<color=#10B981><b>[Unit 9]</b> Cleaned & Rebuilt Unit Challenge Panel!</color>");
        }

        [MenuItem("Masters Phonics/Unit 9/Panels/7. Clean & Setup Seven Types Map", false, 207)]
        public static void MenuSetupMap()
        {
            Transform sp = GetSectionsParent();
            if (sp == null) { EditorUtility.DisplayDialog("Error", "Unit_9/Unit_9_Sections not found!", "OK"); return; }
            GameObject go = BuildOrCleanPanel(sp, "U9_SevenTypesMap_Panel", "SevenTypesMap");
            SetupSevenTypesMap(go);
            var comp = go.GetComponent<U9_SA_SevenTypesMap_Masters_Phonics>();
            if (comp != null) AutoAssignSevenTypesMap(comp);
            EnsureGlobalNavigation(sp.parent, sp);
            var flow = sp.GetComponentInParent<U9_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) AutoAssignFlowManager(flow);
            EditorUtility.SetDirty(go);
            Debug.Log("<color=#10B981><b>[Unit 9]</b> Cleaned & Rebuilt Seven Types Map Panel!</color>");
        }

        [MenuItem("Masters Phonics/Unit 9/Panels/8. Clean & Setup Completion Panel", false, 208)]
        public static void MenuSetupCompletion()
        {
            Transform sp = GetSectionsParent();
            if (sp == null) { EditorUtility.DisplayDialog("Error", "Unit_9/Unit_9_Sections not found!", "OK"); return; }
            GameObject go = BuildOrCleanPanel(sp, "U9_COMPLETE_Panel", "COMPLETE");
            SetupCompletionPanel(go);
            var comp = go.GetComponent<U9_SA_CompletionPanel_Masters_Phonics>();
            if (comp != null) AutoAssignCompletionPanel(comp);
            EnsureGlobalNavigation(sp.parent, sp);
            var flow = sp.GetComponentInParent<U9_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) AutoAssignFlowManager(flow);
            EditorUtility.SetDirty(go);
            Debug.Log("<color=#10B981><b>[Unit 9]</b> Cleaned & Rebuilt Completion Panel!</color>");
        }

        // =====================================================================
        // Section Selection Signboard (5 Wagons)
        // =====================================================================

        private static void UpdateSignboardCards(Transform selPanels)
        {
            Transform content = selPanels.Find("Content") ?? selPanels;
            string[] wagonTitles = new string[]
            {
                "1. Learn & Turtle Rule",
                "2. One or Two?",
                "3. Build the Ending",
                "4. Sentence Hunt",
                "5. Challenge & All Seven"
            };

            for (int i = 1; i <= 5; i++)
            {
                Transform wagon = content.Find($"Wagon{i}") 
                               ?? content.Find($"Cart{i}") 
                               ?? content.Find($"Truck{i}") 
                               ?? content.Find($"Section{i}_Btn");
                if (wagon != null)
                {
                    var txt = wagon.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (txt != null && i <= wagonTitles.Length)
                    {
                        txt.text = $"<b>{wagonTitles[i - 1]}</b>";
                        txt.color = Color.white;
                    }
                }
            }
        }

        // =====================================================================
        // Panel Setup Methods
        // =====================================================================

        private static GameObject BuildOrCleanPanel(Transform parent, string targetPanelName, string searchKeyword = "")
        {
            Transform existing = parent.Find(targetPanelName);
            if (existing == null && !string.IsNullOrEmpty(searchKeyword))
            {
                for (int i = 0; i < parent.childCount; i++)
                {
                    Transform child = parent.GetChild(i);
                    if (child.name.IndexOf(searchKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        existing = child;
                        existing.name = targetPanelName;
                        break;
                    }
                }
            }

            if (existing != null)
            {
                // Thoroughly wipe all legacy child GameObjects from donor unit
                for (int i = existing.childCount - 1; i >= 0; i--)
                {
                    Undo.DestroyObjectImmediate(existing.GetChild(i).gameObject);
                }
                StripOldActivityComponents(existing.gameObject);

                Image panelImg = existing.GetComponent<Image>();
                if (panelImg != null && panelImg.sprite == null)
                {
                    Undo.DestroyObjectImmediate(panelImg);
                }

                RectTransform ert = existing.GetComponent<RectTransform>();
                if (ert != null)
                {
                    ert.anchorMin = Vector2.zero;
                    ert.anchorMax = Vector2.one;
                    ert.offsetMin = Vector2.zero;
                    ert.offsetMax = Vector2.zero;
                }
                return existing.gameObject;
            }

            // Fallback: Create fresh panel container
            GameObject go = new GameObject(targetPanelName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        private static void SetupGM01ConceptCards(GameObject go)
        {
            StripOldActivityComponents(go);
            var comp = go.GetComponent<U9_SA_GM01_ConceptCards_Masters_Phonics>() ?? go.AddComponent<U9_SA_GM01_ConceptCards_Masters_Phonics>();

            EnsureStandardHUD(go.transform, "CONCEPT CARDS — CONSONANT + LE", "Consonant + le Syllables");

            // Setup Cards container
            GameObject cardsRootGo = new GameObject("Cards", typeof(RectTransform));
            cardsRootGo.transform.SetParent(go.transform, false);
            RectTransform crt = cardsRootGo.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero;
            crt.anchorMax = Vector2.one;
            crt.offsetMin = Vector2.zero;
            crt.offsetMax = Vector2.zero;
            Transform cardsRoot = cardsRootGo.transform;

            Transform c1 = CreateCardView(cardsRoot, "Card_1", "A Syllable with No Vowel Sound", "A consonant + le syllable is the last syllable of a word.\nThe 'e' is silent because every syllable needs a vowel letter.");
            Transform c2 = CreateCardView(cardsRoot, "Card_2", "The Turtle Rule", "If a word ends in consonant + le:\ncount back three letters and split there.\nTurtle -> Tur | tle.");
            Transform c3 = CreateCardView(cardsRoot, "Card_3", "One Consonant or Two?", "One consonant before -le -> Open syllable -> Long vowel (ta-ble).\nTwo consonants before -le -> Closed syllable -> Short vowel (cat-tle).");

            // Card 1 Interactive Schwa Aside
            if (c1 != null)
            {
                GameObject sabGo = new GameObject("SchwaAsideBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                sabGo.transform.SetParent(c1, false);
                RectTransform sabRt = sabGo.GetComponent<RectTransform>();
                sabRt.anchoredPosition = new Vector2(0, -135);
                sabRt.sizeDelta = new Vector2(380, 55);
                U9_UI_Utils.ApplyRoundedButtonStyle(sabGo.GetComponent<Image>(), new Color(0.2f, 0.45f, 0.75f));

                GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGO.transform.SetParent(sabGo.transform, false);
                txtGO.GetComponent<RectTransform>().sizeDelta = sabRt.sizeDelta;
                var txt = txtGO.GetComponent<TextMeshProUGUI>();
                txt.text = "<b><size=22>TURTLE: HEAR THE SCHWA</size></b>";
                txt.fontSize = 22;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.white;
            }

            // Card 2 Interactive Examples (Puzzle, Pickle, Bubble)
            if (c2 != null)
            {
                GameObject exGo = new GameObject("ExamplesContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                exGo.transform.SetParent(c2, false);
                RectTransform exRt = exGo.GetComponent<RectTransform>();
                exRt.anchoredPosition = new Vector2(0, -135);
                exRt.sizeDelta = new Vector2(680, 55);
                var hlg = exGo.GetComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 16;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;

                CreateCardExampleButton(exGo.transform, "PuzzleBtn", "puz · zle", new Color(0.15f, 0.55f, 0.35f));
                CreateCardExampleButton(exGo.transform, "PickleBtn", "pick · le", new Color(0.15f, 0.55f, 0.35f));
                CreateCardExampleButton(exGo.transform, "BubbleBtn", "bub · ble", new Color(0.15f, 0.55f, 0.35f));
            }

            // Card 3 Interactive Pairs (Maple vs Apple, Noble vs Bottle, Bugle vs Puzzle)
            if (c3 != null)
            {
                GameObject pGo = new GameObject("PairsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                pGo.transform.SetParent(c3, false);
                RectTransform pRt = pGo.GetComponent<RectTransform>();
                pRt.anchoredPosition = new Vector2(0, -135);
                pRt.sizeDelta = new Vector2(720, 55);
                var hlg = pGo.GetComponent<HorizontalLayoutGroup>();
                hlg.childAlignment = TextAnchor.MiddleCenter;
                hlg.spacing = 14;
                hlg.childControlWidth = false;
                hlg.childControlHeight = false;

                CreateCardExampleButton(pGo.transform, "MapleAppleBtn", "ma-ple vs ap-ple", new Color(0.3f, 0.35f, 0.65f), 220);
                CreateCardExampleButton(pGo.transform, "NobleBottleBtn", "no-ble vs bot-tle", new Color(0.3f, 0.35f, 0.65f), 220);
                CreateCardExampleButton(pGo.transform, "BuglePuzzleBtn", "bu-gle vs puz-zle", new Color(0.3f, 0.35f, 0.65f), 220);
            }

            // Navigation Buttons (Preserve existing if manually assigned)
            Transform nextBtnTr = go.transform.Find("NextBtn") ?? go.transform.Find("NextCardBtn");
            if (nextBtnTr == null)
            {
                GameObject nbGo = new GameObject("NextBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                nbGo.transform.SetParent(go.transform, false);
                RectTransform nbRt = nbGo.GetComponent<RectTransform>();
                nbRt.anchoredPosition = new Vector2(350, -280);
                nbRt.sizeDelta = new Vector2(200, 60);

                GameObject nTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                nTxtGO.transform.SetParent(nbGo.transform, false);
                nTxtGO.GetComponent<RectTransform>().sizeDelta = nbRt.sizeDelta;
                var nTxt = nTxtGO.GetComponent<TextMeshProUGUI>();
                nTxt.text = "<b>NEXT >></b>";
                nTxt.fontSize = 28;
                nTxt.fontStyle = FontStyles.Bold;
                nTxt.alignment = TextAlignmentOptions.Center;
                nTxt.color = Color.white;
            }

            Transform prevBtnTr = go.transform.Find("PrevBtn") ?? go.transform.Find("PrevCardBtn");
            if (prevBtnTr == null)
            {
                GameObject pbGo = new GameObject("PrevBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                pbGo.transform.SetParent(go.transform, false);
                RectTransform pbRt = pbGo.GetComponent<RectTransform>();
                pbRt.anchoredPosition = new Vector2(-350, -280);
                pbRt.sizeDelta = new Vector2(200, 60);

                GameObject pTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                pTxtGO.transform.SetParent(pbGo.transform, false);
                pTxtGO.GetComponent<RectTransform>().sizeDelta = pbRt.sizeDelta;
                var pTxt = pTxtGO.GetComponent<TextMeshProUGUI>();
                pTxt.text = "<b><< BACK</b>";
                pTxt.fontSize = 28;
                pTxt.fontStyle = FontStyles.Bold;
                pTxt.alignment = TextAlignmentOptions.Center;
                pTxt.color = Color.white;
            }

            comp.AutoBindHierarchyElements();
        }

        private static void SetupGM02sTurtleRule(GameObject go)
        {
            StripOldActivityComponents(go);
            var comp = go.GetComponent<U9_SA_GM02s_TurtleRule_Masters_Phonics>() ?? go.AddComponent<U9_SA_GM02s_TurtleRule_Masters_Phonics>();

            EnsureStandardHUD(go.transform, "ACTIVITY 1: THE TURTLE RULE", "Count Back Three & Split");

            // Countback Badge
            GameObject badgeGo = new GameObject("CountbackBadge", typeof(RectTransform), typeof(Image));
            badgeGo.transform.SetParent(go.transform, false);
            RectTransform bRt = badgeGo.GetComponent<RectTransform>();
            bRt.anchoredPosition = new Vector2(0, 140);
            bRt.sizeDelta = new Vector2(340, 50);
            U9_UI_Utils.ApplyRoundedButtonStyle(badgeGo.GetComponent<Image>(), new Color(0.2f, 0.3f, 0.45f));

            GameObject bTxtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            bTxtGo.transform.SetParent(badgeGo.transform, false);
            bTxtGo.GetComponent<RectTransform>().sizeDelta = bRt.sizeDelta;
            var bTxt = bTxtGo.GetComponent<TextMeshProUGUI>();
            bTxt.text = "<b>COUNT: 3 - 2 - 1</b>";
            bTxt.fontSize = 26;
            bTxt.fontStyle = FontStyles.Bold;
            bTxt.alignment = TextAlignmentOptions.Center;
            bTxt.color = new Color(1f, 0.85f, 0.3f);

            // Word Container
            GameObject cGo = new GameObject("WordContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cGo.transform.SetParent(go.transform, false);
            RectTransform rt = cGo.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 10);
            rt.sizeDelta = new Vector2(1200, 180);
            var hlg = cGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 10;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // Split Action Button
            GameObject sbGo = new GameObject("SplitBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            sbGo.transform.SetParent(go.transform, false);
            RectTransform srt = sbGo.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(0, -140);
            srt.sizeDelta = new Vector2(280, 70);
            U9_UI_Utils.ApplyRoundedButtonStyle(sbGo.GetComponent<Image>(), new Color(0.12f, 0.75f, 0.38f));

            GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(sbGo.transform, false);
            txtGO.GetComponent<RectTransform>().sizeDelta = srt.sizeDelta;
            var txt = txtGO.GetComponent<TextMeshProUGUI>();
            txt.text = "<b>SPLIT</b>";
            txt.fontSize = 32;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;

            comp.AutoBindHierarchyElements();
        }

        private static void SetupGM03sOneOrTwo(GameObject go)
        {
            StripOldActivityComponents(go);
            var comp = go.GetComponent<U9_SA_GM03s_OneOrTwo_Masters_Phonics>() ?? go.AddComponent<U9_SA_GM03s_OneOrTwo_Masters_Phonics>();

            EnsureStandardHUD(go.transform, "ACTIVITY 2: ONE OR TWO?", "Open vs Closed Syllables");

            Sprite gateOpenSprite = null;
            Sprite doorClosedSprite = null;
            var moreSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/unit9_MP/more u9 mp.png");
            if (moreSprites != null)
            {
                foreach (var obj in moreSprites)
                {
                    if (obj is Sprite s)
                    {
                        if (s.name == "U09_UI_Gate_Open_Icon") gateOpenSprite = s;
                        else if (s.name == "U09_UI_Door_Closed_Icon") doorClosedSprite = s;
                    }
                }
            }

            // Long Bin (Left)
            CreateSortingBin(go.transform, "LongBin", new Vector2(-380, -30), "LONG VOWEL (OPEN)", new Color(0.1f, 0.55f, 0.9f));
            Transform longBin = go.transform.Find("LongBin");
            if (longBin != null && longBin.Find("GateIcon") == null)
            {
                GameObject iconGo = new GameObject("GateIcon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(longBin, false);
                RectTransform irt = iconGo.GetComponent<RectTransform>();
                irt.anchoredPosition = new Vector2(0, -20);
                irt.sizeDelta = new Vector2(160, 160);
                var img = iconGo.GetComponent<Image>();
                img.preserveAspect = true;
                if (gateOpenSprite != null) img.sprite = gateOpenSprite;
            }

            // Short Bin (Right)
            CreateSortingBin(go.transform, "ShortBin", new Vector2(380, -30), "SHORT VOWEL (CLOSED)", new Color(0.85f, 0.3f, 0.25f));
            Transform shortBin = go.transform.Find("ShortBin");
            if (shortBin != null && shortBin.Find("DoorIcon") == null)
            {
                GameObject iconGo = new GameObject("DoorIcon", typeof(RectTransform), typeof(Image));
                iconGo.transform.SetParent(shortBin, false);
                RectTransform irt = iconGo.GetComponent<RectTransform>();
                irt.anchoredPosition = new Vector2(0, -20);
                irt.sizeDelta = new Vector2(160, 160);
                var img = iconGo.GetComponent<Image>();
                img.preserveAspect = true;
                if (doorClosedSprite != null) img.sprite = doorClosedSprite;
            }

            // Center Card
            GameObject cardGo = new GameObject("CenterCard", typeof(RectTransform), typeof(Image));
            cardGo.transform.SetParent(go.transform, false);
            RectTransform rt = cardGo.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -30);
            rt.sizeDelta = new Vector2(360, 200);
            U9_UI_Utils.ApplyRoundedCardStyle(cardGo.GetComponent<Image>(), new Color(0.96f, 0.98f, 1f));

            GameObject txtGO = new GameObject("WordText", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(cardGo.transform, false);
            txtGO.GetComponent<RectTransform>().sizeDelta = rt.sizeDelta;
            var txt = txtGO.GetComponent<TextMeshProUGUI>();
            txt.text = "<b>table</b>";
            txt.fontSize = 44;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.black;

            // Explanation / Feedback Text
            Transform expTr = go.transform.Find("ExplanationText");
            if (expTr == null)
            {
                GameObject expGo = new GameObject("ExplanationText", typeof(RectTransform), typeof(TextMeshProUGUI));
                expGo.transform.SetParent(go.transform, false);
                RectTransform ert = expGo.GetComponent<RectTransform>();
                ert.anchoredPosition = new Vector2(0, -180);
                ert.sizeDelta = new Vector2(900, 60);
                var etxt = expGo.GetComponent<TextMeshProUGUI>();
                etxt.fontSize = 26;
                etxt.fontStyle = FontStyles.Bold;
                etxt.alignment = TextAlignmentOptions.Center;
                etxt.color = Color.white;
            }

            comp.AutoBindHierarchyElements();
        }

        private static void SetupGM02BuildEnding(GameObject go)
        {
            StripOldActivityComponents(go);
            var comp = go.GetComponent<U9_SA_GM02_BuildEnding_Masters_Phonics>() ?? go.AddComponent<U9_SA_GM02_BuildEnding_Masters_Phonics>();

            EnsureStandardHUD(go.transform, "ACTIVITY 3: BUILD THE ENDING", "Doubling & -le Suffixes");

            // Base Slot
            GameObject bsGo = new GameObject("BaseSlot", typeof(RectTransform), typeof(Image));
            bsGo.transform.SetParent(go.transform, false);
            RectTransform rt = bsGo.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(-120, 90);
            rt.sizeDelta = new Vector2(200, 100);
            U9_UI_Utils.ApplyRoundedCardStyle(bsGo.GetComponent<Image>(), new Color(0.9f, 0.95f, 1f));

            GameObject bsTxtGO = new GameObject("BaseText", typeof(RectTransform), typeof(TextMeshProUGUI));
            bsTxtGO.transform.SetParent(bsGo.transform, false);
            bsTxtGO.GetComponent<RectTransform>().sizeDelta = rt.sizeDelta;
            var bsTxt = bsTxtGO.GetComponent<TextMeshProUGUI>();
            bsTxt.text = "<b>jug</b>";
            bsTxt.fontSize = 42;
            bsTxt.fontStyle = FontStyles.Bold;
            bsTxt.alignment = TextAlignmentOptions.Center;
            bsTxt.color = Color.black;

            // Ending Slot
            GameObject esGo = new GameObject("EndingSlot", typeof(RectTransform), typeof(Image));
            esGo.transform.SetParent(go.transform, false);
            RectTransform ert = esGo.GetComponent<RectTransform>();
            ert.anchoredPosition = new Vector2(120, 90);
            ert.sizeDelta = new Vector2(200, 100);
            U9_UI_Utils.ApplyRoundedCardStyle(esGo.GetComponent<Image>(), new Color(0.9f, 0.95f, 1f));

            GameObject esTxtGO = new GameObject("EndingText", typeof(RectTransform), typeof(TextMeshProUGUI));
            esTxtGO.transform.SetParent(esGo.transform, false);
            esTxtGO.GetComponent<RectTransform>().sizeDelta = ert.sizeDelta;
            var esTxt = esTxtGO.GetComponent<TextMeshProUGUI>();
            esTxt.text = "<b>[ ? ]</b>";
            esTxt.fontSize = 42;
            esTxt.fontStyle = FontStyles.Bold;
            esTxt.alignment = TextAlignmentOptions.Center;
            esTxt.color = Color.black;

            // Live Word Preview / Equation Text
            GameObject resGo = new GameObject("ResultText", typeof(RectTransform), typeof(TextMeshProUGUI));
            resGo.transform.SetParent(go.transform, false);
            RectTransform resRt = resGo.GetComponent<RectTransform>();
            resRt.anchoredPosition = new Vector2(0, 20);
            resRt.sizeDelta = new Vector2(750, 50);
            var resTxt = resGo.GetComponent<TextMeshProUGUI>();
            resTxt.text = "<b>jug + [ ? ] = jug...</b>";
            resTxt.fontSize = 32;
            resTxt.fontStyle = FontStyles.Bold;
            resTxt.alignment = TextAlignmentOptions.Center;
            resTxt.color = new Color(0.1f, 0.15f, 0.25f);

            // Endings Tray
            GameObject trayGo = new GameObject("EndingsTray", typeof(RectTransform), typeof(GridLayoutGroup));
            trayGo.transform.SetParent(go.transform, false);
            RectTransform trt = trayGo.GetComponent<RectTransform>();
            trt.anchoredPosition = new Vector2(0, -30);
            trt.sizeDelta = new Vector2(820, 100);
            var glg = trayGo.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(90, 65);
            glg.spacing = new Vector2(12, 10);
            glg.childAlignment = TextAnchor.MiddleCenter;

            string[] endings = new string[] { "ble", "dle", "gle", "tle", "cle", "fle", "ple", "zle" };
            foreach (var e in endings)
            {
                GameObject btnGo = new GameObject($"Btn_{e}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(trayGo.transform, false);
                U9_UI_Utils.ApplyRoundedButtonStyle(btnGo.GetComponent<Image>(), new Color(0.15f, 0.2f, 0.3f));

                GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGO.transform.SetParent(btnGo.transform, false);
                txtGO.GetComponent<RectTransform>().sizeDelta = new Vector2(90, 65);
                var txt = txtGO.GetComponent<TextMeshProUGUI>();
                txt.text = $"<b>{e}</b>";
                txt.fontSize = 28;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.white;
            }

            // Doubling Switch Label
            GameObject lblGo = new GameObject("DoublingLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblGo.transform.SetParent(go.transform, false);
            RectTransform lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchoredPosition = new Vector2(0, -68);
            lrt.sizeDelta = new Vector2(300, 35);
            var lTxt = lblGo.GetComponent<TextMeshProUGUI>();
            lTxt.text = "<b>Double the Letter?</b>";
            lTxt.fontSize = 20;
            lTxt.fontStyle = FontStyles.Bold;
            lTxt.alignment = TextAlignmentOptions.Center;
            lTxt.color = new Color(0.85f, 0.92f, 1f);

            // Doubling Switch
            GameObject swGo = new GameObject("DoublingSwitch", typeof(RectTransform), typeof(Image), typeof(Button));
            swGo.transform.SetParent(go.transform, false);
            RectTransform srt = swGo.GetComponent<RectTransform>();
            srt.anchoredPosition = new Vector2(0, -125);
            srt.sizeDelta = new Vector2(200, 90);
            var swImg = swGo.GetComponent<Image>();
            swImg.preserveAspect = true;

            Sprite switchOff = null;
            var sheetAssets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath("Assets/Art/unit9_MP/sprite U9 MP.png");
            if (sheetAssets != null)
            {
                foreach (var a in sheetAssets)
                {
                    if (a is Sprite s && (s.name == "U09_UI_Doubling_Switch_Off" || s.name.IndexOf("Switch_Off", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        switchOff = s;
                        break;
                    }
                }
            }

            if (switchOff != null)
            {
                swImg.sprite = switchOff;
                swImg.color = Color.white;
            }
            else
            {
                U9_UI_Utils.ApplyRoundedButtonStyle(swImg, new Color(0.4f, 0.45f, 0.55f));
            }

            GameObject swTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            swTxtGO.transform.SetParent(swGo.transform, false);
            swTxtGO.GetComponent<RectTransform>().sizeDelta = srt.sizeDelta;
            var swTxt = swTxtGO.GetComponent<TextMeshProUGUI>();
            swTxt.text = "<b>DOUBLE: NO</b>";
            swTxt.fontSize = 24;
            swTxt.fontStyle = FontStyles.Bold;
            swTxt.alignment = TextAlignmentOptions.Center;
            swTxt.color = Color.white;
            if (switchOff != null) swTxtGO.SetActive(false);

            // Build Action Button
            GameObject bbGo = new GameObject("BuildBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            bbGo.transform.SetParent(go.transform, false);
            RectTransform brt = bbGo.GetComponent<RectTransform>();
            brt.anchoredPosition = new Vector2(0, -200);
            brt.sizeDelta = new Vector2(240, 60);
            U9_UI_Utils.ApplyRoundedButtonStyle(bbGo.GetComponent<Image>(), new Color(0.12f, 0.75f, 0.38f));

            GameObject bTxtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            bTxtGO.transform.SetParent(bbGo.transform, false);
            bTxtGO.GetComponent<RectTransform>().sizeDelta = brt.sizeDelta;
            var bTxt = bTxtGO.GetComponent<TextMeshProUGUI>();
            bTxt.text = "<b>BUILD</b>";
            bTxt.fontSize = 30;
            bTxt.fontStyle = FontStyles.Bold;
            bTxt.alignment = TextAlignmentOptions.Center;
            bTxt.color = Color.white;

            comp.AutoBindHierarchyElements();
        }

        private static void SetupGM06SentenceHunt(GameObject go)
        {
            StripOldActivityComponents(go);
            var comp = go.GetComponent<U9_SA_GM06_SentenceHunt_Masters_Phonics>() ?? go.AddComponent<U9_SA_GM06_SentenceHunt_Masters_Phonics>();

            EnsureStandardHUD(go.transform, "ACTIVITY 4: SENTENCE HUNT", "Find Consonant + le Words");

            // Sentence Panel
            GameObject pGo = new GameObject("SentencePanel", typeof(RectTransform), typeof(Image));
            pGo.transform.SetParent(go.transform, false);
            RectTransform rt = pGo.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 40);
            rt.sizeDelta = new Vector2(960, 200);
            U9_UI_Utils.ApplyRoundedCardStyle(pGo.GetComponent<Image>(), new Color(0.1f, 0.15f, 0.25f, 0.95f));

            // Words Container
            GameObject wcGo = new GameObject("WordsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            wcGo.transform.SetParent(pGo.transform, false);
            RectTransform wcRt = wcGo.GetComponent<RectTransform>();
            wcRt.anchorMin = Vector2.zero;
            wcRt.anchorMax = Vector2.one;
            wcRt.offsetMin = new Vector2(20, 20);
            wcRt.offsetMax = new Vector2(-20, -20);
            var hlg = wcGo.GetComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 14;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            // Split Result Text
            GameObject rGo = new GameObject("FoundSyllablesText", typeof(RectTransform), typeof(TextMeshProUGUI));
            rGo.transform.SetParent(go.transform, false);
            RectTransform rrt = rGo.GetComponent<RectTransform>();
            rrt.anchoredPosition = new Vector2(0, -100);
            rrt.sizeDelta = new Vector2(700, 60);
            var txt = rGo.GetComponent<TextMeshProUGUI>();
            txt.text = "";
            txt.fontSize = 36;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = new Color(0.2f, 0.9f, 0.45f);

            comp.AutoBindHierarchyElements();
        }

        private static void SetupUnitChallenge(GameObject go)
        {
            StripOldActivityComponents(go);
            var comp = go.GetComponent<U9_SA_UnitChallenge_Masters_Phonics>() ?? go.AddComponent<U9_SA_UnitChallenge_Masters_Phonics>();

            EnsureStandardHUD(go.transform, "UNIT CHALLENGE — CONSONANT + LE", "Mastery Evaluation");

            // Prompt Text
            GameObject prGo = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
            prGo.transform.SetParent(go.transform, false);
            RectTransform rt = prGo.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 100);
            rt.sizeDelta = new Vector2(900, 90);
            var txt = prGo.GetComponent<TextMeshProUGUI>();
            txt.text = "<b>Question Prompt</b>";
            txt.fontSize = 34;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;

            // Replay Audio Speaker Button
            GameObject abGo = new GameObject("ReplayBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            abGo.transform.SetParent(go.transform, false);
            RectTransform abRt = abGo.GetComponent<RectTransform>();
            abRt.anchoredPosition = new Vector2(0, 35);
            abRt.sizeDelta = new Vector2(230, 48);
            U9_UI_Utils.ApplyRoundedButtonStyle(abGo.GetComponent<Image>(), new Color(0.15f, 0.4f, 0.8f));

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

            for (int i = 1; i <= 3; i++)
            {
                GameObject btnGo = new GameObject($"Option_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(optGo.transform, false);
                btnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(650, 58);
                U9_UI_Utils.ApplyRoundedButtonStyle(btnGo.GetComponent<Image>(), new Color(0.15f, 0.22f, 0.35f, 0.95f));

                GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                txtGO.transform.SetParent(btnGo.transform, false);
                txtGO.GetComponent<RectTransform>().sizeDelta = new Vector2(650, 58);
                var oTxt = txtGO.GetComponent<TextMeshProUGUI>();
                oTxt.text = $"<b>Option {i}</b>";
                oTxt.fontSize = 26;
                oTxt.fontStyle = FontStyles.Bold;
                oTxt.alignment = TextAlignmentOptions.Center;
                oTxt.color = Color.white;
            }

            comp.AutoBindHierarchyElements();
        }

        private static void SetupSevenTypesMap(GameObject go)
        {
            StripOldActivityComponents(go);
            var comp = go.GetComponent<U9_SA_SevenTypesMap_Masters_Phonics>() ?? go.AddComponent<U9_SA_SevenTypesMap_Masters_Phonics>();

            EnsureStandardHUD(go.transform, "THE SEVEN SYLLABLE TYPES", "Grand Master Syllable Map");

            GameObject gGo = new GameObject("TilesContainer", typeof(RectTransform), typeof(GridLayoutGroup));
            gGo.transform.SetParent(go.transform, false);
            RectTransform rt = gGo.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(1100, 520);
            var glg = gGo.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(340, 180);
            glg.spacing = new Vector2(20, 20);
            glg.childAlignment = TextAnchor.MiddleCenter;

            GameObject cbGo = new GameObject("ContinueBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            cbGo.transform.SetParent(go.transform, false);
            RectTransform crt = cbGo.GetComponent<RectTransform>();
            crt.anchoredPosition = new Vector2(0, -260);
            crt.sizeDelta = new Vector2(300, 65);
            U9_UI_Utils.ApplyRoundedButtonStyle(cbGo.GetComponent<Image>(), new Color(0.12f, 0.75f, 0.38f));

            GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(cbGo.transform, false);
            txtGO.GetComponent<RectTransform>().sizeDelta = crt.sizeDelta;
            var txt = txtGO.GetComponent<TextMeshProUGUI>();
            txt.text = "<b>CLAIM FINAL BADGES >></b>";
            txt.fontSize = 26;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;

            comp.AutoBindHierarchyElements();
        }

        private static void SetupCompletionPanel(GameObject go)
        {
            StripOldActivityComponents(go);
            var comp = go.GetComponent<U9_SA_CompletionPanel_Masters_Phonics>() ?? go.AddComponent<U9_SA_CompletionPanel_Masters_Phonics>();

            EnsureStandardHUD(go.transform, "UNIT 9 COMPLETE!", "Consonant + le Mastery");

            GameObject cGo = new GameObject("Card", typeof(RectTransform), typeof(Image));
            cGo.transform.SetParent(go.transform, false);
            RectTransform rt = cGo.GetComponent<RectTransform>();
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(760, 520);
            U9_UI_Utils.ApplyRoundedCardStyle(cGo.GetComponent<Image>(), new Color(0.12f, 0.18f, 0.28f, 0.96f));
            Transform card = cGo.transform;

            // Load Sliced Badge Sprites
            Sprite turtleBadge = null;
            Sprite allSevenBadge = null;
            var sheetAssets = AssetDatabase.LoadAllAssetsAtPath($"{ART_BASE_PATH}/sprite U9 MP.png");
            if (sheetAssets != null)
            {
                foreach (var a in sheetAssets)
                {
                    if (a is Sprite s)
                    {
                        if (s.name == "U09_Badge_TurtleKeeper" || s.name.IndexOf("TurtleKeeper", StringComparison.OrdinalIgnoreCase) >= 0)
                            turtleBadge = s;
                        else if (s.name == "U09_Badge_AllSevenMaster" || s.name.IndexOf("AllSeven", StringComparison.OrdinalIgnoreCase) >= 0)
                            allSevenBadge = s;
                    }
                }
            }

            // Left Badge: Turtle Keeper
            GameObject biGo = new GameObject("BadgeIcon", typeof(RectTransform), typeof(Image));
            biGo.transform.SetParent(card, false);
            RectTransform biRt = biGo.GetComponent<RectTransform>();
            biRt.anchoredPosition = new Vector2(-150, 110);
            biRt.sizeDelta = new Vector2(140, 140);
            var biImg = biGo.GetComponent<Image>();
            biImg.preserveAspect = true;
            if (turtleBadge != null) biImg.sprite = turtleBadge;

            GameObject blGo = new GameObject("BadgeLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            blGo.transform.SetParent(card, false);
            RectTransform blRt = blGo.GetComponent<RectTransform>();
            blRt.anchoredPosition = new Vector2(-150, 25);
            blRt.sizeDelta = new Vector2(220, 35);
            var blTxt = blGo.GetComponent<TextMeshProUGUI>();
            blTxt.text = "<b>Turtle Keeper</b>";
            blTxt.fontSize = 20;
            blTxt.fontStyle = FontStyles.Bold;
            blTxt.alignment = TextAlignmentOptions.Center;
            blTxt.color = new Color(1f, 0.85f, 0.3f);

            // Right Badge: All Seven Master
            GameObject asbGo = new GameObject("AllSevenBadge", typeof(RectTransform), typeof(Image));
            asbGo.transform.SetParent(card, false);
            RectTransform asbRt = asbGo.GetComponent<RectTransform>();
            asbRt.anchoredPosition = new Vector2(150, 110);
            asbRt.sizeDelta = new Vector2(140, 140);
            var asbImg = asbGo.GetComponent<Image>();
            asbImg.preserveAspect = true;
            if (allSevenBadge != null) asbImg.sprite = allSevenBadge;

            GameObject asblGo = new GameObject("AllSevenLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
            asblGo.transform.SetParent(card, false);
            RectTransform asblRt = asblGo.GetComponent<RectTransform>();
            asblRt.anchoredPosition = new Vector2(150, 25);
            asblRt.sizeDelta = new Vector2(220, 35);
            var asblTxt = asblGo.GetComponent<TextMeshProUGUI>();
            asblTxt.text = "<b>All Seven Master</b>";
            asblTxt.fontSize = 20;
            asblTxt.fontStyle = FontStyles.Bold;
            asblTxt.alignment = TextAlignmentOptions.Center;
            asblTxt.color = new Color(0.25f, 0.8f, 1f);

            // Final Score Text
            GameObject scoreGO = new GameObject("ScoreText", typeof(RectTransform), typeof(TextMeshProUGUI));
            scoreGO.transform.SetParent(card, false);
            RectTransform sRt = scoreGO.GetComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0, -30);
            sRt.sizeDelta = new Vector2(500, 50);
            var sTxt = scoreGO.GetComponent<TextMeshProUGUI>();
            sTxt.text = "<b>Score: 6,400</b>";
            sTxt.fontSize = 36;
            sTxt.fontStyle = FontStyles.Bold;
            sTxt.alignment = TextAlignmentOptions.Center;
            sTxt.color = new Color(1f, 0.85f, 0.3f);

            // Stats: Words Split & Accuracy
            GameObject wsGO = new GameObject("WordsSplitText", typeof(RectTransform), typeof(TextMeshProUGUI));
            wsGO.transform.SetParent(card, false);
            RectTransform wsRt = wsGO.GetComponent<RectTransform>();
            wsRt.anchoredPosition = new Vector2(-150, -85);
            wsRt.sizeDelta = new Vector2(280, 40);
            var wsTxt = wsGO.GetComponent<TextMeshProUGUI>();
            wsTxt.text = "<b>Words Split: 32</b>";
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

            // Action Buttons: Continue & Replay
            GameObject cbGo = new GameObject("ContinueBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            cbGo.transform.SetParent(card, false);
            RectTransform cbRt = cbGo.GetComponent<RectTransform>();
            cbRt.anchoredPosition = new Vector2(160, -170);
            cbRt.sizeDelta = new Vector2(280, 60);
            U9_UI_Utils.ApplyRoundedButtonStyle(cbGo.GetComponent<Image>(), new Color(0.12f, 0.75f, 0.38f));

            GameObject btnTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTxt.transform.SetParent(cbGo.transform, false);
            btnTxt.GetComponent<RectTransform>().sizeDelta = cbRt.sizeDelta;
            var bt = btnTxt.GetComponent<TextMeshProUGUI>();
            bt.text = "<b>CONTINUE >></b>";
            bt.fontSize = 24;
            bt.fontStyle = FontStyles.Bold;
            bt.alignment = TextAlignmentOptions.Center;
            bt.color = Color.white;

            GameObject rbGo = new GameObject("ReplayUnitBtn", typeof(RectTransform), typeof(Image), typeof(Button));
            rbGo.transform.SetParent(card, false);
            RectTransform rbRt = rbGo.GetComponent<RectTransform>();
            rbRt.anchoredPosition = new Vector2(-160, -170);
            rbRt.sizeDelta = new Vector2(280, 60);
            U9_UI_Utils.ApplyRoundedButtonStyle(rbGo.GetComponent<Image>(), new Color(0.2f, 0.45f, 0.75f));

            GameObject rBtnTxt = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            rBtnTxt.transform.SetParent(rbGo.transform, false);
            rBtnTxt.GetComponent<RectTransform>().sizeDelta = rbRt.sizeDelta;
            var rbt = rBtnTxt.GetComponent<TextMeshProUGUI>();
            rbt.text = "<b>REPLAY UNIT</b>";
            rbt.fontSize = 24;
            rbt.fontStyle = FontStyles.Bold;
            rbt.alignment = TextAlignmentOptions.Center;
            rbt.color = Color.white;

            comp.AutoBindHierarchyElements();
        }

        // =====================================================================
        // UI Helpers
        // =====================================================================

        private static void EnsureStandardHUD(Transform root, string titleText, string subtitleText = "Consonant + le Syllables")
        {
            Transform hud = root.Find("ProgressHUD")
                         ?? root.Find("HUD")
                         ?? root.Find("Header_Container")
                         ?? root.Find("HeaderRibbon")
                         ?? root.Find("Header");
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

            // Title Text (Size 45, Bold)
            Transform tTr = hud.Find("Title_Text") ?? hud.Find("TitleText") ?? hud.Find("Title") ?? root.Find("Title_Text") ?? root.Find("Title");
            TextMeshProUGUI txt = null;
            if (tTr == null)
            {
                GameObject tGo = new GameObject("Title_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                tGo.transform.SetParent(hud, false);
                RectTransform tRt = tGo.GetComponent<RectTransform>();
                tRt.anchoredPosition = new Vector2(0, 10);
                tRt.sizeDelta = new Vector2(950, 60);
                txt = tGo.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                txt = tTr.GetComponent<TextMeshProUGUI>();
                RectTransform tRt = tTr.GetComponent<RectTransform>();
                if (tRt != null && tRt.sizeDelta.y < 55) tRt.sizeDelta = new Vector2(tRt.sizeDelta.x > 0 ? tRt.sizeDelta.x : 950, 60);
            }

            if (txt != null)
            {
                txt.text = $"<b>{titleText}</b>";
                txt.fontSize = 45;
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                txt.color = Color.white;
            }

            // Subtitle Text (Size 30, Bold)
            Transform sTr = hud.Find("Subtitle_Text") ?? hud.Find("SubtitleText") ?? hud.Find("Subtitle") ?? hud.Find("Prompt_Text") ?? root.Find("Subtitle_Text") ?? root.Find("Subtitle");
            TextMeshProUGUI sTxt = null;
            if (sTr == null)
            {
                GameObject sGo = new GameObject("Subtitle_Text", typeof(RectTransform), typeof(TextMeshProUGUI));
                sGo.transform.SetParent(hud, false);
                RectTransform sRt = sGo.GetComponent<RectTransform>();
                sRt.anchoredPosition = new Vector2(0, -35);
                sRt.sizeDelta = new Vector2(950, 45);
                sTxt = sGo.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                sTxt = sTr.GetComponent<TextMeshProUGUI>();
                RectTransform sRt = sTr.GetComponent<RectTransform>();
                if (sRt != null && sRt.sizeDelta.y < 40) sRt.sizeDelta = new Vector2(sRt.sizeDelta.x > 0 ? sRt.sizeDelta.x : 950, 45);
            }

            if (sTxt != null)
            {
                sTxt.text = $"<b>{subtitleText}</b>";
                sTxt.fontSize = 30;
                sTxt.fontStyle = FontStyles.Bold;
                sTxt.alignment = TextAlignmentOptions.Center;
                sTxt.color = new Color(0.75f, 0.9f, 1f);
            }
        }

        private static Transform CreateCardView(Transform parent, string cardName, string title, string body)
        {
            GameObject cGo = new GameObject(cardName, typeof(RectTransform), typeof(Image));
            cGo.transform.SetParent(parent, false);
            RectTransform rt = cGo.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -20);
            rt.sizeDelta = new Vector2(820, 440);
            U9_UI_Utils.ApplyRoundedCardStyle(cGo.GetComponent<Image>(), new Color(0.96f, 0.98f, 1f));
            Transform card = cGo.transform;

            // Card Title (Size 45, Bold)
            GameObject titleGO = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleGO.transform.SetParent(card, false);
            RectTransform tRt = titleGO.GetComponent<RectTransform>();
            tRt.anchoredPosition = new Vector2(0, 150);
            tRt.sizeDelta = new Vector2(760, 60);
            var tTxt = titleGO.GetComponent<TextMeshProUGUI>();
            tTxt.text = $"<b>{title}</b>";
            tTxt.fontSize = 45;
            tTxt.fontStyle = FontStyles.Bold;
            tTxt.alignment = TextAlignmentOptions.Center;
            tTxt.color = Color.black;

            // Subtitle / Guideline (Size 30, Bold)
            GameObject subGO = new GameObject("Subtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subGO.transform.SetParent(card, false);
            RectTransform subRt = subGO.GetComponent<RectTransform>();
            subRt.anchoredPosition = new Vector2(0, 95);
            subRt.sizeDelta = new Vector2(760, 45);
            var subTxt = subGO.GetComponent<TextMeshProUGUI>();
            subTxt.text = "<b>Phonics Concept & Rule</b>";
            subTxt.fontSize = 30;
            subTxt.fontStyle = FontStyles.Bold;
            subTxt.alignment = TextAlignmentOptions.Center;
            subTxt.color = new Color(0.12f, 0.38f, 0.75f);

            // Body Content (Size 28, Readable)
            GameObject bodyGO = new GameObject("Body", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyGO.transform.SetParent(card, false);
            RectTransform bRt = bodyGO.GetComponent<RectTransform>();
            bRt.anchoredPosition = new Vector2(0, 10);
            bRt.sizeDelta = new Vector2(760, 130);
            var bTxt = bodyGO.GetComponent<TextMeshProUGUI>();
            bTxt.text = body;
            bTxt.fontSize = 28;
            bTxt.alignment = TextAlignmentOptions.Center;
            bTxt.color = new Color(0.15f, 0.2f, 0.3f);

            return card;
        }

        private static void CreateCardExampleButton(Transform parent, string btnName, string label, Color bgColor, float width = 190)
        {
            if (parent.Find(btnName) != null) return;

            GameObject btnGo = new GameObject(btnName, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, 50);
            U9_UI_Utils.ApplyRoundedButtonStyle(btnGo.GetComponent<Image>(), bgColor);

            GameObject txtGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(btnGo.transform, false);
            txtGO.GetComponent<RectTransform>().sizeDelta = rt.sizeDelta;
            var txt = txtGO.GetComponent<TextMeshProUGUI>();
            txt.text = $"<b>{label}</b>";
            txt.fontSize = 22;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
        }

        private static void CreateSortingBin(Transform parent, string binName, Vector2 pos, string label, Color color)
        {
            GameObject bGo = new GameObject(binName, typeof(RectTransform), typeof(Image), typeof(Button));
            bGo.transform.SetParent(parent, false);
            RectTransform rt = bGo.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(320, 360);
            U9_UI_Utils.ApplyRoundedCardStyle(bGo.GetComponent<Image>(), color);
            Transform bin = bGo.transform;

            // Bin Title (Size 30, Bold)
            GameObject txtGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(bin, false);
            txtGO.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 60);
            txtGO.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 130);
            var txt = txtGO.GetComponent<TextMeshProUGUI>();
            txt.text = $"<b>{label}</b>";
            txt.fontSize = 30;
            txt.fontStyle = FontStyles.Bold;
            txt.alignment = TextAlignmentOptions.Center;
            txt.color = Color.white;
        }

        private static void EnsureGlobalNavigation(Transform root, Transform sectionsParent)
        {
            if (sectionsParent == null) return;

            // 1. Static Global Back Button (Top-Left of Sections, persistent and visible all the time in sections)
            Transform backTr = sectionsParent.Find("GlobalBack_Btn") 
                            ?? root.Find("GlobalBack_Btn") 
                            ?? sectionsParent.Find("Back_Button main")
                            ?? sectionsParent.Find("Back_Btn") 
                            ?? root.Find("Back_Btn");
            if (backTr == null)
            {
                GameObject backGo = new GameObject("GlobalBack_Btn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                backGo.transform.SetParent(sectionsParent, false);
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
                backTr = backGo.transform;
            }
            backTr.gameObject.SetActive(true);
            backTr.SetAsLastSibling();

            // 2. Dynamic Next Section Button (Bottom-Right of Sections, shown when section ends)
            Transform nextTr = sectionsParent.Find("NextActivity_Btn") 
                            ?? root.Find("NextActivity_Btn") 
                            ?? sectionsParent.Find("Next_Button main")
                            ?? sectionsParent.Find("Next_Btn") 
                            ?? root.Find("Next_Btn");
            if (nextTr == null)
            {
                GameObject nextGo = new GameObject("NextActivity_Btn", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                nextGo.transform.SetParent(sectionsParent, false);
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
                nextTr = nextGo.transform;
            }
            nextTr.gameObject.SetActive(false); // Hidden during initial gameplay, enabled when section/card completes
            nextTr.SetAsLastSibling();
        }

        // =========================================================================
        // Auto-Assignment Methods (Inspector Bindings & Star / SFX Wiring)
        // =========================================================================

        [MenuItem("Masters Phonics/Unit 9/Auto-Assign All Unit 9 Inspectors", false, 101)]
        public static void AutoAssignAllUnit9Inspectors()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in Scene!", "OK");
                return;
            }

            Transform u9 = canvas.transform.Find("Unit_9");
            if (u9 == null)
            {
                EditorUtility.DisplayDialog("Error", "Unit_9 GameObject not found under Canvas! Please run 'Generate Unit 9 Hierarchy' first.", "OK");
                return;
            }

            ClearCaches();
            AutoAssignAllInspectors(u9.gameObject);

            EditorUtility.SetDirty(u9.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(u9.gameObject.scene);

            Debug.Log("<color=#10B981><b>[Unit 9 Automator]</b> Auto-Assigned all Unit 9 Inspector components, star sprites, badges, and audio clips successfully!</color>");
            EditorUtility.DisplayDialog("Unit 9 Inspectors Auto-Assigned!", 
                "Successfully auto-assigned Star rating sprites, Badge sprites, all SFX clips, and hierarchy bindings across all Unit 9 inspectors!", 
                "Awesome!");
        }

        public static void AutoAssignAllInspectors(GameObject unit9Obj)
        {
            if (unit9Obj == null) return;
            RemoveMissingScriptsRecursively(unit9Obj);

            // 1. Flow Manager
            var flow = unit9Obj.GetComponent<U9_SA_UnitFlowManager_Masters_Phonics>();
            if (flow != null) AutoAssignFlowManager(flow);

            // 2. Audio Manager
            var audio = unit9Obj.GetComponent<U9_SA_AudioManager_Masters_Phonics>();
            if (audio != null) AutoAssignAudioManager(audio);

            // 3. Game Mode Panels
            var gm01 = unit9Obj.GetComponentInChildren<U9_SA_GM01_ConceptCards_Masters_Phonics>(true);
            if (gm01 != null) AutoAssignConceptCards(gm01);

            var gm02s = unit9Obj.GetComponentInChildren<U9_SA_GM02s_TurtleRule_Masters_Phonics>(true);
            if (gm02s != null) AutoAssignTurtleRule(gm02s);

            var gm03s = unit9Obj.GetComponentInChildren<U9_SA_GM03s_OneOrTwo_Masters_Phonics>(true);
            if (gm03s != null) AutoAssignOneOrTwo(gm03s);

            var gm02 = unit9Obj.GetComponentInChildren<U9_SA_GM02_BuildEnding_Masters_Phonics>(true);
            if (gm02 != null) AutoAssignBuildEnding(gm02);

            var gm06 = unit9Obj.GetComponentInChildren<U9_SA_GM06_SentenceHunt_Masters_Phonics>(true);
            if (gm06 != null) AutoAssignSentenceHunt(gm06);

            var challenge = unit9Obj.GetComponentInChildren<U9_SA_UnitChallenge_Masters_Phonics>(true);
            if (challenge != null) AutoAssignUnitChallenge(challenge);

            var map = unit9Obj.GetComponentInChildren<U9_SA_SevenTypesMap_Masters_Phonics>(true);
            if (map != null) AutoAssignSevenTypesMap(map);

            var comp = unit9Obj.GetComponentInChildren<U9_SA_CompletionPanel_Masters_Phonics>(true);
            if (comp != null) AutoAssignCompletionPanel(comp);

            // 4. Main Unit Selection Router
            var router = Object.FindFirstObjectByType<Unit_Selection_Panel_Masters_Phonics>();
            if (router != null)
            {
                var so = new SerializedObject(router);
                var u9p = so.FindProperty("unit9Parent");
                if (u9p != null) u9p.objectReferenceValue = unit9Obj;

                var u9sp = so.FindProperty("unit9SectionSelectionPanel");
                if (u9sp != null)
                {
                    Transform sel = unit9Obj.transform.Find("Unit_9_Section_Selection_Panels") 
                                 ?? unit9Obj.transform.Find("Section_Selection_Panels");
                    if (sel != null) u9sp.objectReferenceValue = sel.gameObject;
                }
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(router);
            }
        }

        [MenuItem("CONTEXT/U9_SA_UnitFlowManager_Masters_Phonics/Auto-Assign Flow Manager & Stars", false, 1)]
        public static void ContextAutoAssignFlowManager(MenuCommand command)
        {
            AutoAssignFlowManager(command.context as U9_SA_UnitFlowManager_Masters_Phonics);
        }

        public static void AutoAssignFlowManager(U9_SA_UnitFlowManager_Masters_Phonics mgr)
        {
            if (mgr == null) return;
            Undo.RecordObject(mgr, "Auto-Assign Flow Manager");

            if (mgr.goldenStarSprite == null)
                mgr.goldenStarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png.png")
                                    ?? LoadSpriteMatching("golden-star");

            if (mgr.emptyStarSprite == null)
                mgr.emptyStarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png (1).png")
                                   ?? LoadSpriteMatching("empty-star");

            var so = new SerializedObject(mgr);
            Transform root = mgr.transform;
            Transform secParent = root.Find("Unit_9_Sections") ?? root.Find("Sections");

            if (secParent != null)
            {
                var spProp = so.FindProperty("sectionsParent");
                if (spProp != null) spProp.objectReferenceValue = secParent.gameObject;

                var lProp = so.FindProperty("learnPanel");
                if (lProp != null)
                {
                    var comp = secParent.GetComponentInChildren<U9_SA_GM01_ConceptCards_Masters_Phonics>(true);
                    if (comp != null) lProp.objectReferenceValue = comp.gameObject;
                }

                var a1Prop = so.FindProperty("activity1Panel");
                if (a1Prop != null)
                {
                    var comp = secParent.GetComponentInChildren<U9_SA_GM02s_TurtleRule_Masters_Phonics>(true);
                    if (comp != null) a1Prop.objectReferenceValue = comp.gameObject;
                }

                var a2Prop = so.FindProperty("activity2Panel");
                if (a2Prop != null)
                {
                    var comp = secParent.GetComponentInChildren<U9_SA_GM03s_OneOrTwo_Masters_Phonics>(true);
                    if (comp != null) a2Prop.objectReferenceValue = comp.gameObject;
                }

                var a3Prop = so.FindProperty("activity3Panel");
                if (a3Prop != null)
                {
                    var comp = secParent.GetComponentInChildren<U9_SA_GM02_BuildEnding_Masters_Phonics>(true);
                    if (comp != null) a3Prop.objectReferenceValue = comp.gameObject;
                }

                var a4Prop = so.FindProperty("activity4Panel");
                if (a4Prop != null)
                {
                    var comp = secParent.GetComponentInChildren<U9_SA_GM06_SentenceHunt_Masters_Phonics>(true);
                    if (comp != null) a4Prop.objectReferenceValue = comp.gameObject;
                }

                var ucProp = so.FindProperty("unitChallengePanel");
                if (ucProp != null)
                {
                    var comp = secParent.GetComponentInChildren<U9_SA_UnitChallenge_Masters_Phonics>(true);
                    if (comp != null) ucProp.objectReferenceValue = comp.gameObject;
                }

                var mapProp = so.FindProperty("mapUpdatePanel");
                if (mapProp != null)
                {
                    var comp = secParent.GetComponentInChildren<U9_SA_SevenTypesMap_Masters_Phonics>(true);
                    if (comp != null) mapProp.objectReferenceValue = comp.gameObject;
                }

                var crProp = so.FindProperty("completionReportPanel");
                if (crProp != null)
                {
                    var comp = secParent.GetComponentInChildren<U9_SA_CompletionPanel_Masters_Phonics>(true);
                    if (comp != null) crProp.objectReferenceValue = comp.gameObject;
                }

                var backProp = so.FindProperty("globalBackBtn");
                if (backProp != null)
                {
                    var b = secParent.Find("GlobalBack_Btn") ?? root.Find("GlobalBack_Btn") ?? secParent.Find("Back_Btn") ?? root.Find("Back_Btn");
                    if (b != null) backProp.objectReferenceValue = b.GetComponent<Button>();
                }

                var nextProp = so.FindProperty("nextActivityBtn");
                if (nextProp != null)
                {
                    var n = secParent.Find("NextActivity_Btn") ?? root.Find("NextActivity_Btn") ?? secParent.Find("Next_Btn") ?? root.Find("Next_Btn");
                    if (n != null) nextProp.objectReferenceValue = n.GetComponent<Button>();
                }
            }

            var ssbProp = so.FindProperty("selectionSignboardPanel");
            if (ssbProp != null)
            {
                Transform ssb = root.Find("Unit_9_Section_Selection_Panels") ?? root.Find("Section_Selection_Panels");
                if (ssb != null) ssbProp.objectReferenceValue = ssb.gameObject;
            }

            so.ApplyModifiedProperties();
            mgr.AutoBindHierarchyElements();
            EditorUtility.SetDirty(mgr);
        }

        [MenuItem("CONTEXT/U9_SA_AudioManager_Masters_Phonics/Auto-Assign Audio Manager Clips & Mascot", false, 1)]
        public static void ContextAutoAssignAudioManager(MenuCommand command)
        {
            AutoAssignAudioManager(command.context as U9_SA_AudioManager_Masters_Phonics);
        }

        public static void AutoAssignAudioManager(U9_SA_AudioManager_Masters_Phonics mgr)
        {
            if (mgr == null) return;
            Undo.RecordObject(mgr, "Auto-Assign Audio Manager");

            string vAPath = "Assets/Audio/U9_audio/u9_MP_voiceA";
            mgr.unitIntroClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Unit Nine The last syllable type and the.mp3") ?? mgr.unitIntroClip;
            mgr.card1Clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Consonant plus le at the end of a.mp3") ?? mgr.card1Clip;
            mgr.card1bClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Although say turtle slowly Turtul Theres a tiny.mp3") ?? mgr.card1bClip;
            mgr.card2Clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Heres the rule If a word ends in.mp3") ?? mgr.card2Clip;
            mgr.card3Clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Count the consonants sitting just before the le.mp3") ?? mgr.card3Clip;
            mgr.act1IntroClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Tap the gap where the word splits Count.mp3") ?? mgr.act1IntroClip;
            mgr.act1PickleClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Careful with this one The c and k.mp3") ?? mgr.act1PickleClip;
            mgr.act1SprinkleClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Sprinkle Start at the end and count back.mp3") ?? mgr.act1SprinkleClip;
            mgr.act1LongerClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Longer ones Two splits this time.mp3") ?? mgr.act1LongerClip;
            mgr.act2OneConsonantClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/One consonant The syllable ends in a vowel.mp3") ?? mgr.act2OneConsonantClip;
            mgr.act2TwoConsonantsClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Two consonants Door shut vowel short Same as.mp3") ?? mgr.act2TwoConsonantsClip;
            mgr.act3IntroClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Pick the ending then decide whether the last.mp3") ?? mgr.act3IntroClip;
            mgr.act3Round2Clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Notice anything Every base in this round already.mp3") ?? mgr.act3Round2Clip;
            mgr.act4IntroClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Now find them in real sentences Listen then.mp3") ?? mgr.act4IntroClip;
            mgr.act4MultiClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/More than one hiding in these Find them.mp3") ?? mgr.act4MultiClip;
            mgr.challengeIntroClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Ten questions The last ones a word youve.mp3") ?? mgr.challengeIntroClip;
            mgr.mapIntroClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/And thats all seven Closed open magic e.mp3") ?? mgr.mapIntroClip;
            mgr.mapSummaryClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/And heres something the book doesnt tell you.mp3") ?? mgr.mapSummaryClip;
            mgr.unitCompleteClip = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Unit Nine done Thirtytwo words split Badge unlocked.mp3") ?? mgr.unitCompleteClip;

            mgr.correctClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") 
                           ?? AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/Correct Answer 1.mp3")
                           ?? LoadAudioMatching("Correct", SFX_BASE_PATH);

            mgr.wrongClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") 
                         ?? LoadAudioMatching("Incorrect", SFX_BASE_PATH);

            mgr.clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/AB_Pop_boosted_220.mp3")
                         ?? LoadAudioMatching("AB_Pop", SFX_BASE_PATH);

            mgr.celebrationClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_map_complete.wav")
                               ?? LoadAudioMatching("map_complete", SFX_BASE_PATH);

            mgr.countbackTickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_beat_strong.wav")
                                 ?? LoadAudioMatching("beat_strong", SFX_BASE_PATH);

            mgr.splitClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_chunk_snap.wav")
                              ?? LoadAudioMatching("chunk_snap", SFX_BASE_PATH);

            mgr.doorSlamClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_door_slam.wav")
                            ?? LoadAudioMatching("door_slam", SFX_BASE_PATH);

            mgr.gateOpenClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_gate_open.wav")
                            ?? LoadAudioMatching("gate_open", SFX_BASE_PATH);

            mgr.dialClickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U02_SFX_dial_click.wav")
                             ?? LoadAudioMatching("dial_click", SFX_BASE_PATH);

            mgr.wordLiftClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_vowel_stretch.wav")
                            ?? LoadAudioMatching("vowel_stretch", SFX_BASE_PATH);

            mgr.mapCompleteFanfareClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_map_complete.wav")
                                       ?? LoadAudioMatching("map_complete", SFX_BASE_PATH);

            mgr.EnsureMascotBinding();
            mgr.CacheAllU9Audio();
            EditorUtility.SetDirty(mgr);
        }

        [MenuItem("CONTEXT/U9_SA_GM01_ConceptCards_Masters_Phonics/Auto-Assign Concept Cards", false, 1)]
        public static void ContextAutoAssignConceptCards(MenuCommand command)
        {
            AutoAssignConceptCards(command.context as U9_SA_GM01_ConceptCards_Masters_Phonics);
        }

        public static void AutoAssignConceptCards(U9_SA_GM01_ConceptCards_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Concept Cards");

            string vAPath = "Assets/Audio/U9_audio/u9_MP_voiceA";
            var so = new SerializedObject(gm);
            var c1 = so.FindProperty("card1VoiceClip");
            if (c1 != null && c1.objectReferenceValue == null)
                c1.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Consonant plus le at the end of a.mp3");

            var c1b = so.FindProperty("card1SchwaAsideClip");
            if (c1b != null && c1b.objectReferenceValue == null)
                c1b.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Although say turtle slowly Turtul Theres a tiny.mp3");

            var c2 = so.FindProperty("card2VoiceClip");
            if (c2 != null && c2.objectReferenceValue == null)
                c2.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Heres the rule If a word ends in.mp3");

            var c3 = so.FindProperty("card3VoiceClip");
            if (c3 != null && c3.objectReferenceValue == null)
                c3.objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>($"{vAPath}/Count the consonants sitting just before the le.mp3");

            so.ApplyModifiedProperties();
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        [MenuItem("CONTEXT/U9_SA_GM02s_TurtleRule_Masters_Phonics/Auto-Assign Turtle Rule", false, 1)]
        public static void ContextAutoAssignTurtleRule(MenuCommand command)
        {
            AutoAssignTurtleRule(command.context as U9_SA_GM02s_TurtleRule_Masters_Phonics);
        }

        public static void AutoAssignTurtleRule(U9_SA_GM02s_TurtleRule_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Turtle Rule");
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        [MenuItem("CONTEXT/U9_SA_GM03s_OneOrTwo_Masters_Phonics/Auto-Assign One Or Two", false, 1)]
        public static void ContextAutoAssignOneOrTwo(MenuCommand command)
        {
            AutoAssignOneOrTwo(command.context as U9_SA_GM03s_OneOrTwo_Masters_Phonics);
        }

        public static void AutoAssignOneOrTwo(U9_SA_GM03s_OneOrTwo_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign One Or Two");

            Sprite gateOpenSprite = null;
            Sprite doorClosedSprite = null;
            var moreSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/unit9_MP/more u9 mp.png");
            if (moreSprites != null)
            {
                foreach (var obj in moreSprites)
                {
                    if (obj is Sprite s)
                    {
                        if (s.name == "U09_UI_Gate_Open_Icon") gateOpenSprite = s;
                        else if (s.name == "U09_UI_Door_Closed_Icon") doorClosedSprite = s;
                    }
                }
            }

            Transform longBin = gm.transform.Find("LongBin");
            if (longBin != null)
            {
                Transform iconTr = longBin.Find("GateIcon");
                if (iconTr != null && gateOpenSprite != null)
                {
                    var img = iconTr.GetComponent<Image>();
                    if (img != null) { img.sprite = gateOpenSprite; img.preserveAspect = true; }
                }
            }

            Transform shortBin = gm.transform.Find("ShortBin");
            if (shortBin != null)
            {
                Transform iconTr = shortBin.Find("DoorIcon");
                if (iconTr != null && doorClosedSprite != null)
                {
                    var img = iconTr.GetComponent<Image>();
                    if (img != null) { img.sprite = doorClosedSprite; img.preserveAspect = true; }
                }
            }

            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        [MenuItem("CONTEXT/U9_SA_GM02_BuildEnding_Masters_Phonics/Auto-Assign Build Ending", false, 1)]
        public static void ContextAutoAssignBuildEnding(MenuCommand command)
        {
            AutoAssignBuildEnding(command.context as U9_SA_GM02_BuildEnding_Masters_Phonics);
        }

        public static void AutoAssignBuildEnding(U9_SA_GM02_BuildEnding_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Build Ending");
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        [MenuItem("CONTEXT/U9_SA_GM06_SentenceHunt_Masters_Phonics/Auto-Assign Sentence Hunt", false, 1)]
        public static void ContextAutoAssignSentenceHunt(MenuCommand command)
        {
            AutoAssignSentenceHunt(command.context as U9_SA_GM06_SentenceHunt_Masters_Phonics);
        }

        public static void AutoAssignSentenceHunt(U9_SA_GM06_SentenceHunt_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Sentence Hunt");
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        [MenuItem("CONTEXT/U9_SA_UnitChallenge_Masters_Phonics/Auto-Assign Unit Challenge", false, 1)]
        public static void ContextAutoAssignUnitChallenge(MenuCommand command)
        {
            AutoAssignUnitChallenge(command.context as U9_SA_UnitChallenge_Masters_Phonics);
        }

        public static void AutoAssignUnitChallenge(U9_SA_UnitChallenge_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Unit Challenge");
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        [MenuItem("CONTEXT/U9_SA_SevenTypesMap_Masters_Phonics/Auto-Assign Seven Types Map", false, 1)]
        public static void ContextAutoAssignSevenTypesMap(MenuCommand command)
        {
            AutoAssignSevenTypesMap(command.context as U9_SA_SevenTypesMap_Masters_Phonics);
        }

        public static void AutoAssignSevenTypesMap(U9_SA_SevenTypesMap_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Seven Types Map");
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        [MenuItem("CONTEXT/U9_SA_CompletionPanel_Masters_Phonics/Auto-Assign Completion Panel", false, 1)]
        public static void ContextAutoAssignCompletionPanel(MenuCommand command)
        {
            AutoAssignCompletionPanel(command.context as U9_SA_CompletionPanel_Masters_Phonics);
        }

        public static void AutoAssignCompletionPanel(U9_SA_CompletionPanel_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Completion Panel");

            var serializedObj = new SerializedObject(gm);
            var badgeProp = serializedObj.FindProperty("turtleKeeperBadgeSprite");
            var allSevenProp = serializedObj.FindProperty("allSevenBadgeSprite");

            var sheetAssets = AssetDatabase.LoadAllAssetsAtPath($"{ART_BASE_PATH}/sprite U9 MP.png");
            if (sheetAssets != null)
            {
                foreach (var a in sheetAssets)
                {
                    if (a is Sprite s)
                    {
                        if (badgeProp != null && (s.name == "U09_Badge_TurtleKeeper" || s.name.IndexOf("TurtleKeeper", System.StringComparison.OrdinalIgnoreCase) >= 0 || s.name.IndexOf("Turtle_Keeper", System.StringComparison.OrdinalIgnoreCase) >= 0))
                            badgeProp.objectReferenceValue = s;
                        else if (allSevenProp != null && (s.name == "U09_Badge_AllSevenMaster" || s.name.IndexOf("AllSeven", System.StringComparison.OrdinalIgnoreCase) >= 0 || s.name.IndexOf("All_Seven", System.StringComparison.OrdinalIgnoreCase) >= 0))
                            allSevenProp.objectReferenceValue = s;
                    }
                }
            }

            serializedObj.ApplyModifiedProperties();
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        private static Sprite LoadSpriteMatching(string pattern)
        {
            string[] guids = AssetDatabase.FindAssets($"{pattern} t:Sprite");
            if (guids != null && guids.Length > 0)
            {
                string p = AssetDatabase.GUIDToAssetPath(guids[0]);
                return AssetDatabase.LoadAssetAtPath<Sprite>(p);
            }
            return null;
        }

        private static AudioClip LoadAudioMatching(string query, string subFolder = "")
        {
            if (string.IsNullOrEmpty(query)) return null;

            if (audioCache == null || audioCache.Count == 0)
            {
                audioCache = new Dictionary<string, AudioClip>(System.StringComparer.OrdinalIgnoreCase);
                string[] guids = AssetDatabase.FindAssets("t:AudioClip");
                foreach (string g in guids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                    if (clip != null && !audioCache.ContainsKey(clip.name))
                    {
                        audioCache[clip.name] = clip;
                    }
                }
            }

            if (audioCache.TryGetValue(query, out AudioClip directClip))
                return directClip;

            foreach (var kvp in audioCache)
            {
                if (kvp.Key.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return kvp.Value;
            }

            return null;
        }

        private static void StripOldUnitComponents(GameObject unitObj)
        {
            var comps = unitObj.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (var c in comps)
            {
                if (c == null) continue;
                string typeName = c.GetType().Name;
                if ((typeName.StartsWith("U") && typeName.Contains("_SA_") && !typeName.StartsWith("U9_")) ||
                    typeName.StartsWith("U1_") || typeName.StartsWith("U2_") || typeName.StartsWith("U3_") ||
                    typeName.StartsWith("U4_") || typeName.StartsWith("U5_") || typeName.StartsWith("U6_") ||
                    typeName.StartsWith("U7_") || typeName.StartsWith("U8_"))
                {
                    DestroyImmediate(c);
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
                if ((typeName.StartsWith("U") && typeName.Contains("_SA_") && !typeName.StartsWith("U9_")) ||
                    typeName.StartsWith("U1_") || typeName.StartsWith("U2_") || typeName.StartsWith("U3_") ||
                    typeName.StartsWith("U4_") || typeName.StartsWith("U5_") || typeName.StartsWith("U6_") ||
                    typeName.StartsWith("U7_") || typeName.StartsWith("U8_"))
                {
                    DestroyImmediate(c);
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

        [MenuItem("Masters Phonics/Unit 9/Remove Missing Scripts in Unit 9", false, 102)]
        public static void MenuRemoveMissingScriptsInUnit9()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null) return;
            Transform u9 = canvas.transform.Find("Unit_9");
            if (u9 != null)
            {
                int removed = RemoveMissingScriptsRecursively(u9.gameObject);
                Debug.Log($"<color=#10B981><b>[Unit 9 Automator]</b> Cleaned {removed} missing script component(s) from Unit_9 hierarchy.</color>");
            }
        }

        private static void ClearCaches()
        {
            audioCache = null;
        }
    }
}

