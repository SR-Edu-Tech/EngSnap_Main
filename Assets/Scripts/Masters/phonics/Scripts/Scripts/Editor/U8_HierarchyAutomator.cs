using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics.EditorTools
{
    public class U8_HierarchyAutomator : EditorWindow
    {
        private const string AUDIO_BASE_PATH = "Assets/Audio/U8_audio";
        private const string ART_BASE_PATH = "Assets/Art/unit8_MP";
        private const string SFX_BASE_PATH = "Assets/SFX";

        private static Sprite cachedRoundedSprite;
        private static Dictionary<string, Sprite> spriteCache;
        private static Dictionary<string, AudioClip> audioCache;
        private static List<string> audioList;

        [MenuItem("Phonics Unit 8/Setup Unit 8 Complete Hierarchy & Logic", false, 1)]
        [MenuItem("Masters Phonics/Unit 8/Generate Unit 8 Hierarchy (Clean & Complete)", false, 100)]
        public static void GenerateUnit8Hierarchy()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in the current Scene!", "OK");
                return;
            }

            Transform sourceUnit = canvas.transform.Find("Unit_7") ?? canvas.transform.Find("Unit_6") ?? canvas.transform.Find("Unit_5");

            // Check if Unit_8 already exists
            Transform existingU8 = canvas.transform.Find("Unit_8");
            GameObject unit8Obj;

            if (existingU8 != null)
            {
                unit8Obj = existingU8.gameObject;
            }
            else if (sourceUnit != null)
            {
                // Clone from existing Unit template
                unit8Obj = Object.Instantiate(sourceUnit.gameObject, canvas.transform);
                unit8Obj.name = "Unit_8";
                Undo.RegisterCreatedObjectUndo(unit8Obj, "Generate Clean Unit_8");
            }
            else
            {
                // Create fresh root
                unit8Obj = new GameObject("Unit_8", typeof(RectTransform));
                unit8Obj.transform.SetParent(canvas.transform, false);
                Undo.RegisterCreatedObjectUndo(unit8Obj, "Create Unit_8 Root");
            }

            RectTransform rootRT = unit8Obj.GetComponent<RectTransform>();
            rootRT.anchorMin = Vector2.zero;
            rootRT.anchorMax = Vector2.one;
            rootRT.offsetMin = Vector2.zero;
            rootRT.offsetMax = Vector2.zero;

            ClearCaches();
            StripOldUnitComponents(unit8Obj);

            // Add Managers
            var flowManager = unit8Obj.GetComponent<U8_SA_UnitFlowManager_Masters_Phonics>() ?? unit8Obj.AddComponent<U8_SA_UnitFlowManager_Masters_Phonics>();
            var audioManager = unit8Obj.GetComponent<U8_SA_AudioManager_Masters_Phonics>() ?? unit8Obj.AddComponent<U8_SA_AudioManager_Masters_Phonics>();

            // Setup Section Selection (Signboard with 5 trucks)
            Transform selPanels = unit8Obj.transform.Find("Unit_8_Section_Selection_Panels") 
                               ?? unit8Obj.transform.Find("Unit_7_Section_Selection_Panels") 
                               ?? unit8Obj.transform.Find("Unit_6_Section_Selection_Panels")
                               ?? unit8Obj.transform.Find("Section_Selection_Panels");
            if (selPanels == null)
            {
                GameObject spGo = new GameObject("Unit_8_Section_Selection_Panels", typeof(RectTransform));
                spGo.transform.SetParent(unit8Obj.transform, false);
                selPanels = spGo.transform;
            }
            selPanels.name = "Unit_8_Section_Selection_Panels";
            UpdateSignboardCards(selPanels);

            // Setup Sections Parent
            Transform sectionsParent = unit8Obj.transform.Find("Unit_8_Sections") 
                                    ?? unit8Obj.transform.Find("Unit_7_Sections") 
                                    ?? unit8Obj.transform.Find("Unit_6_Sections")
                                    ?? unit8Obj.transform.Find("Sections");
            if (sectionsParent == null)
            {
                GameObject secGo = new GameObject("Unit_8_Sections", typeof(RectTransform));
                secGo.transform.SetParent(unit8Obj.transform, false);
                sectionsParent = secGo.transform;
            }
            sectionsParent.name = "Unit_8_Sections";

            RectTransform secRT = sectionsParent.GetComponent<RectTransform>();
            secRT.anchorMin = Vector2.zero;
            secRT.anchorMax = Vector2.one;
            secRT.offsetMin = Vector2.zero;
            secRT.offsetMax = Vector2.zero;

            // Clean up any old leftover unit panels (U1_ through U7_)
            for (int i = sectionsParent.childCount - 1; i >= 0; i--)
            {
                Transform child = sectionsParent.GetChild(i);
                string cName = child.name;
                if (cName.StartsWith("U1_") || cName.StartsWith("U2_") || cName.StartsWith("U3_") || 
                    cName.StartsWith("U4_") || cName.StartsWith("U5_") || cName.StartsWith("U6_") || 
                    cName.StartsWith("U7_"))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

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

            Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART_BASE_PATH}/BG_U8_MP.png") ?? LoadSpriteMatching("BG_U8_MP");
            Image bgImg = bgTr.GetComponent<Image>();
            if (bgImg != null)
            {
                if (bgSprite != null) bgImg.sprite = bgSprite;
                bgImg.color = Color.white;
            }

            // Build or Clean Dedicated Panels with Standard U8 Naming
            GameObject learnGo = BuildOrCleanPanel(sectionsParent, "U8_learn_Concept_Cards_Panel");
            SetupGM01ConceptCards(learnGo);

            GameObject act1Go = BuildOrCleanPanel(sectionsParent, "U8_Activity_1_Bossy_R");
            SetupGM04BossyR(act1Go);

            GameObject act2Go = BuildOrCleanPanel(sectionsParent, "U8_Activity_2_Three_Sounds");
            SetupGM03ThreeSounds(act2Go);

            GameObject act3Go = BuildOrCleanPanel(sectionsParent, "U8_Activity_3_Five_Families");
            SetupGM03FiveFamilies(act3Go);

            GameObject act4Go = BuildOrCleanPanel(sectionsParent, "U8_Activity_4_The_Er_Rule");
            SetupGM03sErRule(act4Go);

            GameObject act5Go = BuildOrCleanPanel(sectionsParent, "U8_Activity_5_Spelling_Lab");
            SetupGM05SpellingLab(act5Go);

            GameObject mapGo = BuildOrCleanPanel(sectionsParent, "U8_SevenTypesMap_Panel");
            SetupSevenTypesMap(mapGo);

            GameObject challengeGo = BuildOrCleanPanel(sectionsParent, "U8_UnitChallenge_Panel");
            SetupUnitChallenge(challengeGo);

            GameObject compGo = BuildOrCleanPanel(sectionsParent, "U8_COMPLETE_Panel");
            SetupCompletionPanel(compGo);

            // Wire Managers
            AutoAssignAudioManager(audioManager);
            AutoAssignFlowManager(flowManager);

            EditorUtility.SetDirty(unit8Obj);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(unit8Obj.scene);

            Debug.Log("<color=#22C55E><b>[Unit 8 Automator]</b> Unit 8 Complete Hierarchy, GameObjects, Sprites, and Audio successfully generated and wired!</color>");
            EditorUtility.DisplayDialog("Unit 8 Hierarchy Created!", 
                "Successfully generated clean Unit 8 GameObject hierarchies with all 9 panels and wired all Voice A, Voice B, and SFX audio clips!", 
                "Great!");
        }

        private static GameObject BuildOrCleanPanel(Transform parent, string panelName)
        {
            Transform existing = parent.Find(panelName);
            if (existing != null)
            {
                for (int i = existing.childCount - 1; i >= 0; i--)
                {
                    Undo.DestroyObjectImmediate(existing.GetChild(i).gameObject);
                }
                StripOldUnitComponents(existing.gameObject);

                Image panelImg = existing.GetComponent<Image>();
                if (panelImg != null && panelImg.sprite == null)
                {
                    Undo.DestroyObjectImmediate(panelImg);
                }
                return existing.gameObject;
            }

            GameObject panelObj = new GameObject(panelName, typeof(RectTransform));
            panelObj.transform.SetParent(parent, false);
            RectTransform rt = panelObj.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return panelObj;
        }

        private static void UpdateSignboardCards(Transform signboard)
        {
            RectTransform srt = signboard.GetComponent<RectTransform>();
            srt.anchorMin = Vector2.zero;
            srt.anchorMax = Vector2.one;
            srt.offsetMin = Vector2.zero;
            srt.offsetMax = Vector2.zero;

            Transform trucks = signboard.Find("Trucks") ?? signboard.Find("Cards") ?? CreateContainer(signboard, "Trucks", new Vector2(0, -40f), new Vector2(1100f, 300f)).transform;

            string[] truckNames = { "Truck1", "Truck2", "Truck3", "Truck4", "Truck5" };
            string[] truckLabels = { 
                "<b>1. LEARN & BOSSY R</b>\n<size=70%>Concept Cards & Minimal Pairs</size>", 
                "<b>2. THREE SOUNDS</b>\n<size=70%>/ar/ · /or/ · /er/ Bins</size>", 
                "<b>3. FIVE FAMILIES</b>\n<size=70%>Spelling Family Sort</size>", 
                "<b>4. THE -ER RULE</b>\n<size=70%>Swipe End Quiet vs Middle</size>", 
                "<b>5. SPELLING LAB</b>\n<size=70%>Tile Builder & Challenge</size>" 
            };

            for (int i = 0; i < truckNames.Length; i++)
            {
                Transform t = trucks.Find(truckNames[i]);
                float xPos = -440f + (i * 220f);
                if (t == null)
                {
                    Button b = CreateButton(trucks, truckNames[i], truckLabels[i], new Vector2(xPos, 0f), new Vector2(200f, 130f), 20);
                    b.GetComponent<Image>().color = new Color(0.1f, 0.45f, 0.75f, 0.95f);
                }
                else
                {
                    RectTransform trt = t.GetComponent<RectTransform>();
                    trt.anchoredPosition = new Vector2(xPos, 0f);
                    trt.sizeDelta = new Vector2(200f, 130f);
                    var tmp = t.GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null)
                    {
                        tmp.text = truckLabels[i];
                        tmp.fontSize = 20;
                    }
                }
            }
        }

        // =========================================================================
        // Panel Setup Handlers
        // =========================================================================

        // 1. LEARN: Concept Cards (GM-01)
        private static void SetupGM01ConceptCards(GameObject panel)
        {
            var gm = panel.GetComponent<U8_SA_GM01_ConceptCards_Masters_Phonics>() ?? panel.AddComponent<U8_SA_GM01_ConceptCards_Masters_Phonics>();

            CreateHeader(panel.transform, "CONCEPT CARDS", "R-Controlled Vowels: The Letter That Takes Over");
            CreateReplayButton(panel.transform, new Vector2(860f, 440f));
            CreateProgressHUD(panel.transform, out _, out _);

            Transform cardsContainer = panel.transform.Find("Cards") ?? CreateContainer(panel.transform, "Cards", new Vector2(0f, -20f), new Vector2(1100f, 580f)).transform;

            // Card 1: Bossy R Intro
            Transform c1 = cardsContainer.Find("Card_1") ?? CreateContainer(cardsContainer, "Card_1", Vector2.zero, new Vector2(1000f, 500f)).transform;
            CreateCardBackground(c1);

            // Bossy R Mascot Illustration
            Sprite rPose = LoadSpriteMatching("U8_RRRR_MP_0") ?? LoadSpriteMatching("U8_RRRR_MP");
            if (c1.Find("BossyR_Image") == null && rPose != null)
            {
                GameObject rImgGo = CreateContainer(c1, "BossyR_Image", new Vector2(-340f, 10f), new Vector2(220f, 260f));
                Image rImg = rImgGo.AddComponent<Image>();
                rImg.sprite = rPose;
                rImg.preserveAspect = true;
            }

            if (c1.Find("Title") == null) CreateText(c1, "Title", "<b>The letter that takes over</b>", 38, new Vector2(100f, 180f), new Vector2(760f, 50f), new Color(0.95f, 0.77f, 0.06f, 1f));
            if (c1.Find("Body") == null) CreateText(c1, "Body", "The Bossy R thinks he's royal. He sits in a word and bosses the vowels around.\nHe has so much influence that he won't let a vowel make its own sound at all!\n\n<size=130%><color=#00E5FF><b>c - a - t</b></color>  ->  <color=#FFD700><b>c - a - r</b></color></size>", 26, new Vector2(100f, 10f), new Vector2(760f, 220f), Color.white);

            // Card 2: Five Spellings Grid
            Transform c2 = cardsContainer.Find("Card_2") ?? CreateContainer(cardsContainer, "Card_2", Vector2.zero, new Vector2(1000f, 500f)).transform;
            CreateCardBackground(c2);
            if (c2.Find("Title") == null) CreateText(c2, "Title", "<b>Five Spellings — Tap each to hear</b>", 36, new Vector2(0f, 190f), new Vector2(800f, 50f), new Color(0.95f, 0.77f, 0.06f, 1f));
            string[] patterns = { "ar", "or", "er", "ir", "ur" };
            string[] examples = { "car", "corn", "pepper", "bird", "turtle" };
            string[] tileGems = { "ar_u8", "or_u8", "er_u8", "ir_u8", "ur_u8" };
            for (int i = 0; i < patterns.Length; i++)
            {
                if (c2.Find($"Tile_{patterns[i]}") == null)
                {
                    float x = -360f + (i * 180f);
                    Button b = CreateButton(c2, $"Tile_{patterns[i]}", $"<b><size=140%>{patterns[i]}</size></b>\n<size=75%>{examples[i]}</size>", new Vector2(x, 20f), new Vector2(150f, 150f), 28);
                    Sprite tileSpr = LoadSpriteMatching(tileGems[i]);
                    if (tileSpr != null)
                    {
                        Image bImg = b.GetComponent<Image>();
                        bImg.sprite = tileSpr;
                        bImg.color = Color.white;
                    }
                    else
                    {
                        b.GetComponent<Image>().color = new Color(0.12f, 0.18f, 0.28f, 1f);
                    }

                    // Object Icon
                    Sprite objSpr = LoadSpriteMatching(examples[i]);
                    if (objSpr != null)
                    {
                        GameObject iconGo = CreateContainer(b.transform, "Icon", new Vector2(0f, -40f), new Vector2(50f, 50f));
                        Image iconImg = iconGo.AddComponent<Image>();
                        iconImg.sprite = objSpr;
                        iconImg.preserveAspect = true;
                    }
                }
            }

            // Card 3: Three Sounds Merge
            Transform c3 = cardsContainer.Find("Card_3") ?? CreateContainer(cardsContainer, "Card_3", Vector2.zero, new Vector2(1000f, 500f)).transform;
            CreateCardBackground(c3);
            if (c3.Find("Title") == null) CreateText(c3, "Title", "<b>Five spellings, three sounds</b>", 36, new Vector2(0f, 190f), new Vector2(800f, 50f), new Color(0.95f, 0.77f, 0.06f, 1f));
            if (c3.Find("Body") == null) CreateText(c3, "Body", "<b>ar</b> has its own sound. <b>or</b> has its own sound.\nBut <b>er</b>, <b>ir</b>, and <b>ur</b> all make exactly the SAME sound!\n\n<size=130%><color=#00E5FF><b>her</b></color> · <color=#FFD700><b>bird</b></color> · <color=#A855F7><b>turn</b></color></size>", 28, new Vector2(0f, 30f), new Vector2(900f, 220f), Color.white);

            // Card 4: The -er Rule
            Transform c4 = cardsContainer.Find("Card_4") ?? CreateContainer(cardsContainer, "Card_4", Vector2.zero, new Vector2(1000f, 500f)).transform;
            CreateCardBackground(c4);
            if (c4.Find("Title") == null) CreateText(c4, "Title", "<b>The one rule that helps</b>", 36, new Vector2(0f, 190f), new Vector2(800f, 50f), new Color(0.95f, 0.77f, 0.06f, 1f));
            if (c4.Find("Body") == null) CreateText(c4, "Body", "When the /ər/ sound is at the <b>END of a word</b> and unaccented, it is almost always spelled <b>er</b>.\n\n<size=120%><color=#22C55E><b>tiger · spider · ladder · flower · boxer</b></color></size>", 26, new Vector2(0f, 30f), new Vector2(900f, 200f), Color.white);

            // Navigation Buttons (Using NavButton Helper with Icons)
            if (panel.transform.Find("PrevBtn") == null) CreateNavButton(panel.transform, "PrevBtn", "Assets/Icons/backbutton.png", "PREV", new Vector2(-720f, -420f), new Vector2(220f, 75f));
            if (panel.transform.Find("NextBtn") == null) CreateNavButton(panel.transform, "NextBtn", "Assets/Icons/nextt.png", "NEXT", new Vector2(720f, -420f), new Vector2(220f, 75f));
            if (panel.transform.Find("StartActivity1Btn") == null)
            {
                Button startB = CreateButton(panel.transform, "StartActivity1Btn", "<b>MEET THE BOSS >></b>", new Vector2(720f, -420f), new Vector2(300f, 75f), 26);
                Sprite contSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/continue button.png") ?? LoadSpriteMatching("continue button");
                if (contSpr != null)
                {
                    startB.GetComponent<Image>().sprite = contSpr;
                    startB.GetComponent<Image>().color = Color.white;
                }
                else
                {
                    startB.GetComponent<Image>().color = new Color(0.12f, 0.72f, 0.35f, 1f);
                }
            }

            AutoAssignConceptCards(gm);
        }

        // 2. ACTIVITY 1: Bossy R (GM-04 Minimal Pairs)
        private static void SetupGM04BossyR(GameObject panel)
        {
            var gm = panel.GetComponent<U8_SA_GM04_BossyR_Masters_Phonics>() ?? panel.AddComponent<U8_SA_GM04_BossyR_Masters_Phonics>();

            CreateHeader(panel.transform, "ACTIVITY 1: BOSSY R", "Which word did you hear?");
            CreateReplayButton(panel.transform, new Vector2(860f, 440f));
            CreateProgressHUD(panel.transform, out _, out _);
            CreateScoreHUD(panel.transform, out _, out _);

            Transform options = panel.transform.Find("Options") ?? CreateContainer(panel.transform, "Options", new Vector2(0f, -20f), new Vector2(960f, 360f)).transform;

            if (options.Find("NoR_Button") == null)
            {
                Button b = CreateButton(options, "NoR_Button", "<b>cat</b>\n<size=65%><color=#94A3B8>no bossy R</color></size>", new Vector2(-240f, 0f), new Vector2(360f, 200f), 48);
                b.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.24f, 1f);
                CreateReplayButton(b.transform, new Vector2(130f, 60f));
            }
            if (options.Find("WithR_Button") == null)
            {
                Button b = CreateButton(options, "WithR_Button", "<b>cart</b>\n<size=65%><color=#94A3B8>bossy R</color></size>", new Vector2(240f, 0f), new Vector2(360f, 200f), 48);
                b.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.24f, 1f);
                CreateReplayButton(b.transform, new Vector2(130f, 60f));
            }

            AutoAssignBossyR(gm);
        }

        // 3. ACTIVITY 2: Three Sounds (GM-03 Sound Sorting)
        private static void SetupGM03ThreeSounds(GameObject panel)
        {
            var gm = panel.GetComponent<U8_SA_GM03_ThreeSounds_Masters_Phonics>() ?? panel.AddComponent<U8_SA_GM03_ThreeSounds_Masters_Phonics>();

            CreateHeader(panel.transform, "ACTIVITY 2: THREE SOUNDS", "Sort cards into bins by sound!");
            CreateReplayButton(panel.transform, new Vector2(860f, 440f));
            CreateProgressHUD(panel.transform, out _, out _);
            CreateScoreHUD(panel.transform, out _, out _);

            // Word Card
            if (panel.transform.Find("CardContainer") == null)
            {
                GameObject card = CreateContainer(panel.transform, "CardContainer", new Vector2(0f, 140f), new Vector2(480f, 160f));
                Image img = card.AddComponent<Image>();
                img.sprite = GetOrCreateRoundedSprite();
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                CreateText(card.transform, "WordText", "<b>purple</b>", 56, Vector2.zero, new Vector2(440f, 110f), new Color(0.12f, 0.16f, 0.24f, 1f));
            }

            // 3 Sound Bins (Using chest sprites ar, or, er from U8 ui assets MP)
            Transform bins = panel.transform.Find("Bins") ?? CreateContainer(panel.transform, "Bins", new Vector2(0f, -190f), new Vector2(1160f, 260f)).transform;

            string[] binNames = { "ArBin", "OrBin", "ErBin" };
            string[] titles = { "<b>/ar/</b>\n<size=75%>as in CAR</size>", "<b>/or/</b>\n<size=75%>as in CORN</size>", "<b>/er/</b>\n<size=75%>as in BIRD</size>" };
            string[] chestSprites = { "ar", "or", "er" };

            for (int i = 0; i < 3; i++)
            {
                if (bins.Find(binNames[i]) == null)
                {
                    float x = -380f + (i * 380f);
                    Button b = CreateButton(bins, binNames[i], titles[i], new Vector2(x, 0f), new Vector2(340f, 220f), 32);
                    Sprite cSpr = LoadSpriteMatching(chestSprites[i]);
                    if (cSpr != null)
                    {
                        Image bImg = b.GetComponent<Image>();
                        bImg.sprite = cSpr;
                        bImg.preserveAspect = true;
                        bImg.color = Color.white;
                    }
                    else
                    {
                        b.GetComponent<Image>().color = new Color(0.12f, 0.18f, 0.28f, 1f);
                    }

                    if (i == 2)
                    {
                        // Collector Strip on ErBin
                        GameObject strip = CreateContainer(b.transform, "CollectorStrip", new Vector2(0f, -75f), new Vector2(300f, 45f));
                        Sprite stripSpr = LoadSpriteMatching("jewel collector bar u8");
                        if (stripSpr != null)
                        {
                            Image sImg = strip.AddComponent<Image>();
                            sImg.sprite = stripSpr;
                            sImg.preserveAspect = true;
                        }
                        CreateText(strip.transform, "Text", "Found: er · ir · ur", 20, Vector2.zero, new Vector2(290f, 40f), new Color(0.95f, 0.77f, 0.06f, 1f));
                    }
                }
            }

            AutoAssignThreeSounds(gm);
        }

        // 4. ACTIVITY 3: Five Families (GM-03 Spelling Sorting)
        private static void SetupGM03FiveFamilies(GameObject panel)
        {
            var gm = panel.GetComponent<U8_SA_GM03_FiveFamilies_Masters_Phonics>() ?? panel.AddComponent<U8_SA_GM03_FiveFamilies_Masters_Phonics>();

            CreateHeader(panel.transform, "ACTIVITY 3: FIVE FAMILIES", "Sort words into spelling families!");
            CreateReplayButton(panel.transform, new Vector2(860f, 440f));
            CreateProgressHUD(panel.transform, out _, out _);
            CreateScoreHUD(panel.transform, out _, out _);

            // Card Container
            if (panel.transform.Find("CardContainer") == null)
            {
                GameObject card = CreateContainer(panel.transform, "CardContainer", new Vector2(0f, 140f), new Vector2(480f, 160f));
                Image img = card.AddComponent<Image>();
                img.sprite = GetOrCreateRoundedSprite();
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                CreateText(card.transform, "WordText", "<b>harp</b>", 56, new Vector2(0f, 14f), new Vector2(440f, 85f), new Color(0.12f, 0.16f, 0.24f, 1f));
                CreateText(card.transform, "HintText", "", 22, new Vector2(0f, -44f), new Vector2(440f, 45f), new Color(0.95f, 0.77f, 0.06f, 1f));
            }

            // 3 Family Bins
            Transform bins = panel.transform.Find("Bins") ?? CreateContainer(panel.transform, "Bins", new Vector2(0f, -190f), new Vector2(1160f, 260f)).transform;
            string[] bNames = { "Bin1", "Bin2", "Bin3" };
            string[] bTitles = { "<b>ar</b>\nFAMILY", "<b>or</b>\nFAMILY", "<b>er</b>\nFAMILY" };
            string[] chestSprites = { "ar", "or", "er" };

            for (int i = 0; i < 3; i++)
            {
                if (bins.Find(bNames[i]) == null)
                {
                    float x = -380f + (i * 380f);
                    Button b = CreateButton(bins, bNames[i], bTitles[i], new Vector2(x, 0f), new Vector2(340f, 220f), 32);
                    Sprite cSpr = LoadSpriteMatching(chestSprites[i]);
                    if (cSpr != null)
                    {
                        Image bImg = b.GetComponent<Image>();
                        bImg.sprite = cSpr;
                        bImg.preserveAspect = true;
                        bImg.color = Color.white;
                    }
                    else
                    {
                        b.GetComponent<Image>().color = new Color(0.12f, 0.18f, 0.28f, 1f);
                    }
                }
            }

            AutoAssignFiveFamilies(gm);
        }

        // 5. ACTIVITY 4: The -er Rule (GM-03s Swipe Mechanics)
        private static void SetupGM03sErRule(GameObject panel)
        {
            var gm = panel.GetComponent<U8_SA_GM03s_ErRule_Masters_Phonics>() ?? panel.AddComponent<U8_SA_GM03s_ErRule_Masters_Phonics>();

            CreateHeader(panel.transform, "ACTIVITY 4: THE -ER RULE", "Swipe Left for END quiet (= er) | Right for MIDDLE stressed (= ir/ur)");
            CreateReplayButton(panel.transform, new Vector2(860f, 440f));
            CreateProgressHUD(panel.transform, out _, out _);
            CreateScoreHUD(panel.transform, out _, out _);

            // Rule Banner
            if (panel.transform.Find("RuleBanner") == null)
            {
                GameObject banner = CreateContainer(panel.transform, "RuleBanner", new Vector2(0f, 240f), new Vector2(980f, 58f));
                Image bImg = banner.AddComponent<Image>();
                bImg.sprite = GetOrCreateRoundedSprite();
                bImg.type = Image.Type.Sliced;
                bImg.color = new Color(0.08f, 0.12f, 0.2f, 0.95f);
                CreateText(banner.transform, "Text", "<b>Rule:</b> The quiet <color=#00E5FF>/ər/</color> at the <b>END</b> of a word = <color=#FFD700>\"er\"</color>", 24, Vector2.zero, new Vector2(960f, 50f), Color.white);
            }

            // Central Swipe Card
            if (panel.transform.Find("CardContainer") == null)
            {
                GameObject card = CreateContainer(panel.transform, "CardContainer", new Vector2(0f, 10f), new Vector2(480f, 260f));
                Image img = card.AddComponent<Image>();
                img.sprite = GetOrCreateRoundedSprite();
                img.type = Image.Type.Sliced;
                img.color = Color.white;
                CreateText(card.transform, "WordText", "<b>spider</b>", 58, new Vector2(0f, 30f), new Vector2(440f, 100f), new Color(0.12f, 0.16f, 0.24f, 1f));
                CreateText(card.transform, "SubText", "Is the r-sound at the END and quiet?", 22, new Vector2(0f, -50f), new Vector2(440f, 50f), new Color(0.4f, 0.45f, 0.55f, 1f));
            }

            // Swipe Pods (Using END QUIET and MIDDLE STRESSED sliced sprites from U8 ui assets MP)
            Transform pods = panel.transform.Find("Pods") ?? CreateContainer(panel.transform, "Pods", new Vector2(0f, -220f), new Vector2(1060f, 160f)).transform;
            if (pods.Find("EndQuietPod") == null)
            {
                Button b = CreateButton(pods, "EndQuietPod", "<b><size=110%><< END, QUIET</size></b>\n<color=#00E5FF>= er</color>", new Vector2(-300f, 0f), new Vector2(420f, 130f), 26);
                Sprite endSpr = LoadSpriteMatching("END QUIET");
                if (endSpr != null)
                {
                    Image bImg = b.GetComponent<Image>();
                    bImg.sprite = endSpr;
                    bImg.preserveAspect = true;
                    bImg.color = Color.white;
                }
                else
                {
                    b.GetComponent<Image>().color = new Color(0.08f, 0.45f, 0.7f, 1f);
                }
            }
            if (pods.Find("MidStressedPod") == null)
            {
                Button b = CreateButton(pods, "MidStressedPod", "<b><size=110%>MIDDLE, STRESSED >></size></b>\n<color=#FFD700>= ir or ur</color>", new Vector2(300f, 0f), new Vector2(420f, 130f), 26);
                Sprite midSpr = LoadSpriteMatching("MIDDLE STRESSED");
                if (midSpr != null)
                {
                    Image bImg = b.GetComponent<Image>();
                    bImg.sprite = midSpr;
                    bImg.preserveAspect = true;
                    bImg.color = Color.white;
                }
                else
                {
                    b.GetComponent<Image>().color = new Color(0.7f, 0.45f, 0.08f, 1f);
                }
            }

            AutoAssignErRule(gm);
        }

        // 6. ACTIVITY 5: Spelling Lab (GM-05 Blanked Word Completion)
        private static void SetupGM05SpellingLab(GameObject panel)
        {
            var gm = panel.GetComponent<U8_SA_GM05_SpellingLab_Masters_Phonics>() ?? panel.AddComponent<U8_SA_GM05_SpellingLab_Masters_Phonics>();

            CreateHeader(panel.transform, "ACTIVITY 5: SPELLING LAB", "Pick the missing r-controlled vowel!");
            CreateReplayButton(panel.transform, new Vector2(860f, 440f));
            CreateProgressHUD(panel.transform, out _, out _);
            CreateScoreHUD(panel.transform, out _, out _);

            // Word Container
            Transform wc = panel.transform.Find("WordContainer") ?? CreateContainer(panel.transform, "WordContainer", new Vector2(0f, 80f), new Vector2(880f, 240f)).transform;
            CreateCardBackground(wc);
            if (wc.Find("PromptWordText") == null) CreateText(wc, "PromptWordText", "<b>b <color=#0284C7>_ _</color> d</b>", 68, new Vector2(0f, 30f), new Vector2(780f, 120f), Color.white);
            if (wc.Find("RuleHintText") == null) CreateText(wc, "RuleHintText", "", 24, new Vector2(0f, -50f), new Vector2(780f, 50f), new Color(0.95f, 0.77f, 0.06f, 1f));

            // Choice Tiles (Using er_u8, ir_u8, ur_u8 sliced sprites from U8 ui assets MP)
            Transform tiles = panel.transform.Find("Tiles") ?? CreateContainer(panel.transform, "Tiles", new Vector2(0f, -180f), new Vector2(880f, 160f)).transform;
            string[] tNames = { "Tile_er", "Tile_ir", "Tile_ur" };
            string[] tLabels = { "<b>er</b>", "<b>ir</b>", "<b>ur</b>" };
            string[] tSprites = { "er_u8", "ir_u8", "ur_u8" };

            for (int i = 0; i < 3; i++)
            {
                if (tiles.Find(tNames[i]) == null)
                {
                    float x = -280f + (i * 280f);
                    Button b = CreateButton(tiles, tNames[i], tLabels[i], new Vector2(x, 0f), new Vector2(230f, 130f), 52);
                    Sprite tSpr = LoadSpriteMatching(tSprites[i]);
                    if (tSpr != null)
                    {
                        Image bImg = b.GetComponent<Image>();
                        bImg.sprite = tSpr;
                        bImg.preserveAspect = true;
                        bImg.color = Color.white;
                    }
                    else
                    {
                        b.GetComponent<Image>().color = new Color(0.12f, 0.18f, 0.28f, 1f);
                    }
                }
            }

            AutoAssignSpellingLab(gm);
        }

        // 7. SEVEN SYLLABLE TYPES MAP
        private static void SetupSevenTypesMap(GameObject panel)
        {
            var gm = panel.GetComponent<U8_SA_SevenTypesMap_Masters_Phonics>() ?? panel.AddComponent<U8_SA_SevenTypesMap_Masters_Phonics>();

            CreateHeader(panel.transform, "7 SYLLABLE TYPES MAP", "6 of 7 Syllable Types Mastered! R-Controlled Unlocked!");
            CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            GameObject gridCont = panel.transform.Find("Tiles_Container")?.gameObject 
                               ?? panel.transform.Find("Grid")?.gameObject 
                               ?? CreateContainer(panel.transform, "Tiles_Container", new Vector2(0f, -25f), new Vector2(1650f, 430f));
            gridCont.name = "Tiles_Container";
            RectTransform gridRT = gridCont.GetComponent<RectTransform>();
            gridRT.anchorMin = new Vector2(0.5f, 0.5f);
            gridRT.anchorMax = new Vector2(0.5f, 0.5f);
            gridRT.pivot = new Vector2(0.5f, 0.5f);
            gridRT.anchoredPosition = new Vector2(0f, -25f);
            gridRT.sizeDelta = new Vector2(1650f, 430f);

            var grid = gridCont.GetComponent<GridLayoutGroup>() ?? gridCont.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(390f, 180f);
            grid.spacing = new Vector2(24f, 18f);
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 4;

            Transform contBtnTr = panel.transform.Find("ContinueButton") ?? panel.transform.Find("ContinueBtn");
            if (contBtnTr == null)
            {
                Button contBtn = CreateNavButton(panel.transform, "ContinueButton", "Assets/Icons/nextt.png", "NEXT", new Vector2(720f, -420f), new Vector2(220f, 75f));
                contBtn.transform.SetAsLastSibling();
            }

            AutoAssignSevenTypesMap(gm);
        }

        // 8. UNIT CHALLENGE
        private static void SetupUnitChallenge(GameObject panel)
        {
            var gm = panel.GetComponent<U8_SA_UnitChallenge_Masters_Phonics>() ?? panel.AddComponent<U8_SA_UnitChallenge_Masters_Phonics>();

            CreateHeader(panel.transform, "UNIT 8 CHALLENGE", "10 Comprehensive Assessment Questions");
            CreateProgressHUD(panel.transform, out _, out _);
            CreateScoreHUD(panel.transform, out _, out _);

            Transform qBox = panel.transform.Find("QuestionBox") ?? CreateContainer(panel.transform, "QuestionBox", new Vector2(0f, 140f), new Vector2(1060f, 220f)).transform;
            CreateCardBackground(qBox);
            if (qBox.Find("QuestionNumberText") == null) CreateText(qBox, "QuestionNumberText", "<b>Question 1 of 10</b>", 28, new Vector2(0f, 65f), new Vector2(960f, 40f), new Color(0.00f, 0.90f, 1f, 1f));
            if (qBox.Find("PromptText") == null) CreateText(qBox, "PromptText", "<b>Which word has the Bossy R?</b>", 34, new Vector2(0f, 10f), new Vector2(960f, 70f), Color.white);
            if (qBox.Find("RuleTipText") == null) CreateText(qBox, "RuleTipText", "", 22, new Vector2(0f, -50f), new Vector2(960f, 40f), new Color(0.95f, 0.77f, 0.06f, 1f));
            if (qBox.Find("ReplayAudioBtn") == null) CreateReplayButton(qBox, new Vector2(440f, 60f));

            Transform opts = panel.transform.Find("OptionsContainer") ?? CreateContainer(panel.transform, "OptionsContainer", new Vector2(0f, -160f), new Vector2(960f, 240f)).transform;
            for (int i = 0; i < 3; i++)
            {
                string bName = $"Option_{i + 1}";
                if (opts.Find(bName) == null)
                {
                    float y = 70f - (i * 70f);
                    Button b = CreateButton(opts, bName, $"<b>Option {i + 1}</b>", new Vector2(0f, y), new Vector2(780f, 60f), 28);
                    b.GetComponent<Image>().color = new Color(0.12f, 0.18f, 0.28f, 1f);
                }
            }

            AutoAssignUnitChallenge(gm);
        }

        // 9. COMPLETION PANEL
        private static void SetupCompletionPanel(GameObject panel)
        {
            Transform card = panel.transform.Find("ReportCard") ?? CreateContainer(panel.transform, "ReportCard", new Vector2(0f, -20f), new Vector2(780f, 540f)).transform;
            CreateCardBackground(card);

            if (card.Find("BadgeDisplay") == null)
            {
                GameObject badge = CreateContainer(card, "BadgeDisplay", new Vector2(0f, 110f), new Vector2(200f, 200f));
                Image bImg = badge.AddComponent<Image>();
                Sprite bSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{ART_BASE_PATH}/badge U8 MP.png") ?? LoadSpriteMatching("badge U8 MP");
                if (bSprite != null)
                {
                    bImg.sprite = bSprite;
                    bImg.preserveAspect = true;
                }
            }

            if (card.Find("StatsText") == null)
            {
                CreateText(card, "StatsText", "<b>Words Read: 60  |  Words Spelled: 15</b>\n<color=#22C55E><b>Badge Unlocked: R Wrangler</b></color>", 26, new Vector2(0f, -40f), new Vector2(700f, 80f), Color.white);
            }

            if (card.Find("DoneBtn") == null)
            {
                Button doneB = CreateButton(card, "DoneBtn", "<b>BACK TO LESSONS >></b>", new Vector2(0f, -180f), new Vector2(360f, 70f), 26);
                Sprite contSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/continue button.png") ?? LoadSpriteMatching("continue button");
                if (contSpr != null)
                {
                    doneB.GetComponent<Image>().sprite = contSpr;
                    doneB.GetComponent<Image>().color = Color.white;
                }
                else
                {
                    doneB.GetComponent<Image>().color = new Color(0.12f, 0.72f, 0.35f, 1f);
                }
            }
        }

        // =========================================================================
        // UI Helpers & Reusable Component Factory
        // =========================================================================

        private static void CreateCardBackground(Transform container)
        {
            Image img = container.GetComponent<Image>() ?? container.gameObject.AddComponent<Image>();
            img.sprite = GetOrCreateRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.08f, 0.12f, 0.2f, 0.95f);
        }

        private static GameObject CreateContainer(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }

        private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, float fontSize = 28)
        {
            GameObject go = CreateContainer(parent, name, pos, size);
            Image img = go.AddComponent<Image>();
            img.sprite = GetOrCreateRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.12f, 0.16f, 0.24f, 1f);

            Button btn = go.AddComponent<Button>();

            GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textGO.transform.SetParent(go.transform, false);
            RectTransform trt = textGO.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = CreateContainer(parent, name, pos, size);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            return tmp;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize)
        {
            return CreateText(parent, name, content, fontSize, Vector2.zero, Vector2.zero, Color.white);
        }

        private static void CreateHeader(Transform parent, string title, string subtitle)
        {
            if (parent.Find("HeaderRibbon") != null) return;

            GameObject header = CreateContainer(parent, "HeaderRibbon", new Vector2(0f, 440f), new Vector2(1600f, 100f));
            var titleTmp = CreateText(header.transform, "TitleText", $"<b>{title}</b>", 38);
            titleTmp.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 18f);
            titleTmp.color = new Color(0.95f, 0.77f, 0.06f, 1f);

            var subTmp = CreateText(header.transform, "SubtitleText", $"<color=#CBD5E1>{subtitle}</color>", 24);
            subTmp.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -22f);
        }

        private static Button CreateReplayButton(Transform parent, Vector2 pos)
        {
            Transform existing = parent.Find("ReplayAudioBtn") 
                              ?? parent.Find("HeaderRibbon/ReplayAudioBtn") 
                              ?? parent.Find("Header_Container/ReplayBtn")
                              ?? parent.Find("SpeakerBtn")
                              ?? parent.Find("Speaker_Button")
                              ?? parent.Find("ReplayButton")
                              ?? parent.Find("ReplayAudioButton")
                              ?? parent.Find("AudioButton")
                              ?? parent.Find("Btn_Audio")
                              ?? parent.Find("SpeakerButton");
            if (existing != null) return existing.GetComponent<Button>();

            // Look for existing ReplayAudioBtn template in scene to clone
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Button templateBtn = null;
            if (canvas != null)
            {
                var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var b in buttons)
                {
                    if (b == null || b.transform.IsChildOf(parent)) continue;
                    string bName = b.gameObject.name.ToLower();
                    if (bName.Contains("replayaudio") || bName.Contains("audiobutton") || bName.Contains("speaker") || bName.Contains("btn_audio"))
                    {
                        templateBtn = b;
                        break;
                    }
                }
            }

            GameObject btnGo;
            if (templateBtn != null)
            {
                btnGo = Object.Instantiate(templateBtn.gameObject, parent, false);
                btnGo.name = "ReplayAudioBtn";
                while (btnGo.GetComponent<Button>().onClick.GetPersistentEventCount() > 0)
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(btnGo.GetComponent<Button>().onClick, 0);
            }
            else
            {
                btnGo = CreateContainer(parent, "ReplayAudioBtn", pos, new Vector2(85f, 85f));
                btnGo.AddComponent<Image>();
                btnGo.AddComponent<Button>();
            }

            RectTransform rt = btnGo.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(85f, 85f);

            Image img = btnGo.GetComponent<Image>();
            if (img != null)
            {
                // CRITICAL: NEVER overwrite an existing or template-configured sprite!
                if (img.sprite == null)
                {
                    Sprite speakerSpr = null;
                    // Try to load standard unit 1 stuff sprite sheet audio icon first
                    var allSprites = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/GENERAL/unit 1 stuff.png");
                    if (allSprites != null)
                    {
                        foreach (var a in allSprites)
                        {
                            if (a is Sprite s && s.name.ToLower().Contains("speaker"))
                            {
                                speakerSpr = s;
                                break;
                            }
                        }
                    }
                    if (speakerSpr == null)
                    {
                        speakerSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/speaker_button.png")
                                  ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Icons/audio icon.png")
                                  ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Icons/volume.png")
                                  ?? LoadSpriteMatching("speaker audio")
                                  ?? LoadSpriteMatching("speaker")
                                  ?? LoadSpriteMatching("audio");
                    }
                    if (speakerSpr != null) img.sprite = speakerSpr;
                }
                img.preserveAspect = true;
                img.color = Color.white;
            }

            return btnGo.GetComponent<Button>();
        }

        private static Button CreateNavButton(Transform parent, string name, string iconPath, string label, Vector2 pos, Vector2 size)
        {
            GameObject btnGo = CreateContainer(parent, name, pos, size);
            Image img = btnGo.AddComponent<Image>();
            Sprite iconSprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath) ?? LoadSpriteMatching(Path.GetFileNameWithoutExtension(iconPath));
            if (iconSprite != null)
            {
                img.sprite = iconSprite;
                img.preserveAspect = true;
            }
            else
            {
                img.sprite = GetOrCreateRoundedSprite();
                img.type = Image.Type.Sliced;
                img.color = new Color(0.08f, 0.62f, 0.98f, 1f);
            }

            Button btn = btnGo.AddComponent<Button>();
            var txt = CreateText(btnGo.transform, "Text", $"<b>{label}</b>", 28);
            txt.color = Color.white;
            return btn;
        }

        private static void CreateProgressHUD(Transform parent, out Slider pBar, out TextMeshProUGUI pText)
        {
            Transform existing = parent.Find("ProgressHUD");
            if (existing != null)
            {
                pBar = existing.GetComponentInChildren<Slider>(true);
                pText = existing.GetComponentInChildren<TextMeshProUGUI>(true);
                return;
            }

            GameObject hudGo = CreateContainer(parent, "ProgressHUD", new Vector2(0f, 370f), new Vector2(900f, 60f));

            // Try to find and clone working ProgressBar from other units
            Slider templateSlider = null;
            var sliders = Object.FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (sliders != null && sliders.Length > 0)
            {
                foreach (var s in sliders)
                {
                    if (s != null && s.fillRect != null && !s.transform.IsChildOf(parent))
                    {
                        string sPath = s.gameObject.name.ToLower();
                        if (sPath.Contains("progress") || sPath.Contains("bar"))
                        {
                            templateSlider = s;
                            break;
                        }
                    }
                }
            }

            if (templateSlider != null)
            {
                GameObject cloneObj = Object.Instantiate(templateSlider.gameObject, hudGo.transform, false);
                cloneObj.name = "ProgressBar";
                RectTransform cRt = cloneObj.GetComponent<RectTransform>();
                cRt.anchoredPosition = new Vector2(0f, -10f);
                cRt.sizeDelta = new Vector2(650f, 24f);
                pBar = cloneObj.GetComponent<Slider>();
                while (pBar.onValueChanged.GetPersistentEventCount() > 0)
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(pBar.onValueChanged, 0);
            }
            else
            {
                // Full working 3-layer Slider hierarchy
                GameObject barObj = new GameObject("ProgressBar", typeof(RectTransform), typeof(Slider));
                barObj.transform.SetParent(hudGo.transform, false);
                RectTransform barRt = barObj.GetComponent<RectTransform>();
                barRt.anchoredPosition = new Vector2(0f, -10f);
                barRt.sizeDelta = new Vector2(650f, 24f);

                // Layer 1: Background
                GameObject bgObj = new GameObject("Background", typeof(RectTransform), typeof(Image));
                bgObj.transform.SetParent(barObj.transform, false);
                RectTransform bgRt = bgObj.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;
                Image bgImg = bgObj.GetComponent<Image>();
                bgImg.sprite = GetOrCreateRoundedSprite(128, 12);
                bgImg.type = Image.Type.Sliced;
                bgImg.color = new Color(0.06f, 0.09f, 0.16f, 1f);

                // Layer 2: Fill Area
                GameObject fillAreaObj = new GameObject("Fill Area", typeof(RectTransform));
                fillAreaObj.transform.SetParent(barObj.transform, false);
                RectTransform fillAreaRt = fillAreaObj.GetComponent<RectTransform>();
                fillAreaRt.anchorMin = Vector2.zero;
                fillAreaRt.anchorMax = Vector2.one;
                fillAreaRt.offsetMin = new Vector2(4f, 4f);
                fillAreaRt.offsetMax = new Vector2(-4f, -4f);

                // Layer 3: Fill
                GameObject fillObj = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                fillObj.transform.SetParent(fillAreaObj.transform, false);
                RectTransform fillRt = fillObj.GetComponent<RectTransform>();
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = new Vector2(0f, 1f);
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;
                Image fillImg = fillObj.GetComponent<Image>();
                fillImg.sprite = GetOrCreateRoundedSprite(128, 12);
                fillImg.type = Image.Type.Sliced;
                fillImg.color = new Color(0.08f, 0.62f, 0.98f, 1f);

                pBar = barObj.GetComponent<Slider>();
                pBar.fillRect = fillRt;
                pBar.targetGraphic = fillImg;
                pBar.direction = Slider.Direction.LeftToRight;
                pBar.minValue = 0f;
                pBar.maxValue = 10f;
                pBar.value = 0f;
            }

            U8_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(pBar);

            pText = CreateText(hudGo.transform, "ProgressText", "<b>Item 1 of 12</b>", 24);
            pText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 22f);
            pText.color = Color.white;
        }

        private static void CreateScoreHUD(Transform parent, out TextMeshProUGUI scoreTMP, out TextMeshProUGUI streakTMP)
        {
            Transform existing = parent.Find("ScoreHUD");
            if (existing != null)
            {
                scoreTMP = existing.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
                streakTMP = existing.Find("StreakText")?.GetComponent<TextMeshProUGUI>();
                return;
            }

            GameObject hudGo = CreateContainer(parent, "ScoreHUD", new Vector2(-650f, 440f), new Vector2(300f, 60f));
            scoreTMP = CreateText(hudGo.transform, "ScoreText", "Score: <b>0</b>", 26);
            streakTMP = CreateText(hudGo.transform, "StreakText", "", 22);
        }

        public static Sprite GetOrCreateRoundedSprite(int size = 128, int radius = 28)
        {
            if (cachedRoundedSprite != null) return cachedRoundedSprite;

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

            Vector4 border = new Vector4(radius, radius, radius, radius);
            cachedRoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            return cachedRoundedSprite;
        }

        // =========================================================================
        // Auto-Assignment Methods (Inspector Bindings)
        // =========================================================================

        public static void ClearCaches()
        {
            spriteCache = null;
            audioCache = null;
            audioList = null;
            U8_SA_AudioManager_Masters_Phonics.ClearCache();
        }

        public static void AutoAssignFlowManager(U8_SA_UnitFlowManager_Masters_Phonics mgr)
        {
            if (mgr == null) return;
            Undo.RecordObject(mgr, "Auto-Assign Flow Manager");
            if (mgr.goldenStarSprite == null)
                mgr.goldenStarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png.png");
            if (mgr.emptyStarSprite == null)
                mgr.emptyStarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png (1).png");
            mgr.AutoBindHierarchyElements();
            EditorUtility.SetDirty(mgr);
        }

        public static void AutoAssignAudioManager(U8_SA_AudioManager_Masters_Phonics mgr)
        {
            if (mgr == null) return;
            Undo.RecordObject(mgr, "Auto-Assign Audio Manager");

            mgr.correctClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct", SFX_BASE_PATH);
            mgr.wrongClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect", SFX_BASE_PATH);
            mgr.clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/AB_Pop_boosted_220.mp3");
            mgr.celebrationClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_map_complete.wav");
            mgr.rTakeoverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_chunk_snap.wav");
            mgr.vowelDrainClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_vowel_stretch.wav");
            mgr.mergeClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U07_SFX_team_merge.wav");
            mgr.binCollectClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_grid_found.wav");
            mgr.swipeFlyClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U02_SFX_swipe_left.wav");

            mgr.EnsureMascotBinding();
            EditorUtility.SetDirty(mgr);
        }

        public static void AutoAssignConceptCards(U8_SA_GM01_ConceptCards_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Concept Cards");
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        public static void AutoAssignBossyR(U8_SA_GM04_BossyR_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Bossy R");
            gm.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct", SFX_BASE_PATH);
            gm.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect", SFX_BASE_PATH);

            if (gm.items == null || gm.items.Count == 0)
                gm.items = U8_SA_DataTypes_Masters_Phonics.GetDefaultActivity1Items();

            foreach (var item in gm.items)
            {
                if (item != null)
                {
                    if (item.withoutRAudio == null) item.withoutRAudio = LoadAudioMatching(item.withoutRWord, AUDIO_BASE_PATH);
                    if (item.withRAudio == null) item.withRAudio = LoadAudioMatching(item.withRWord, AUDIO_BASE_PATH);
                }
            }

            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        public static void AutoAssignThreeSounds(U8_SA_GM03_ThreeSounds_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Three Sounds");
            gm.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct", SFX_BASE_PATH);
            gm.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect", SFX_BASE_PATH);

            if (gm.items == null || gm.items.Count == 0)
                gm.items = U8_SA_DataTypes_Masters_Phonics.GetDefaultActivity2Items();

            foreach (var item in gm.items)
            {
                if (item != null && item.wordAudio == null)
                    item.wordAudio = LoadAudioMatching(item.word, AUDIO_BASE_PATH);
            }

            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        public static void AutoAssignFiveFamilies(U8_SA_GM03_FiveFamilies_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Five Families");
            gm.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct", SFX_BASE_PATH);
            gm.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect", SFX_BASE_PATH);

            if (gm.roundAItems == null || gm.roundAItems.Count == 0)
                gm.roundAItems = U8_SA_DataTypes_Masters_Phonics.GetDefaultActivity3ItemsRoundA();
            if (gm.roundBItems == null || gm.roundBItems.Count == 0)
                gm.roundBItems = U8_SA_DataTypes_Masters_Phonics.GetDefaultActivity3ItemsRoundB();

            foreach (var item in gm.roundAItems)
            {
                if (item != null && item.wordAudio == null)
                    item.wordAudio = LoadAudioMatching(item.word, AUDIO_BASE_PATH);
            }
            foreach (var item in gm.roundBItems)
            {
                if (item != null && item.wordAudio == null)
                    item.wordAudio = LoadAudioMatching(item.word, AUDIO_BASE_PATH);
            }

            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        public static void AutoAssignErRule(U8_SA_GM03s_ErRule_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Er Rule");
            gm.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct", SFX_BASE_PATH);
            gm.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect", SFX_BASE_PATH);

            if (gm.items == null || gm.items.Count == 0)
                gm.items = U8_SA_DataTypes_Masters_Phonics.GetDefaultActivity4Items();

            foreach (var item in gm.items)
            {
                if (item != null && item.wordAudio == null)
                    item.wordAudio = LoadAudioMatching(item.word, AUDIO_BASE_PATH);
            }

            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        public static void AutoAssignSpellingLab(U8_SA_GM05_SpellingLab_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Spelling Lab");
            gm.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct", SFX_BASE_PATH);
            gm.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect", SFX_BASE_PATH);

            if (gm.items == null || gm.items.Count == 0)
                gm.items = U8_SA_DataTypes_Masters_Phonics.GetDefaultActivity5Items();

            foreach (var item in gm.items)
            {
                if (item != null && item.wordAudio == null)
                    item.wordAudio = LoadAudioMatching(item.word, AUDIO_BASE_PATH);
            }

            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        public static void AutoAssignSevenTypesMap(U8_SA_SevenTypesMap_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Seven Types Map");
            gm.mapIntroClip = LoadAudioMatching("Seven Syllable Types", "voice A") 
                           ?? LoadAudioMatching("U08_VO_unit_complete", "voice A")
                           ?? LoadAudioMatching("U07_VO_map_unit7", "voice A");
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        public static void AutoAssignUnitChallenge(U8_SA_UnitChallenge_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Unit Challenge");
            gm.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct", SFX_BASE_PATH);
            gm.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect", SFX_BASE_PATH);

            if (gm.questions == null || gm.questions.Count == 0)
                gm.questions = U8_SA_DataTypes_Masters_Phonics.GetDefaultChallengeQuestions();

            foreach (var q in gm.questions)
            {
                if (q != null && q.promptAudio == null && !string.IsNullOrEmpty(q.targetWord))
                    q.promptAudio = LoadAudioMatching(q.targetWord, AUDIO_BASE_PATH);
            }

            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        private static void StripOldUnitComponents(GameObject root)
        {
            var monoBehaviours = root.GetComponents<MonoBehaviour>();
            foreach (var mb in monoBehaviours)
            {
                if (mb == null) continue;
                string typeName = mb.GetType().Name;
                if ((typeName.StartsWith("U1_") || typeName.StartsWith("U2_") || typeName.StartsWith("U3_") ||
                     typeName.StartsWith("U4_") || typeName.StartsWith("U5_") || typeName.StartsWith("U6_") ||
                     typeName.StartsWith("U7_")) && !typeName.StartsWith("U8_"))
                {
                    Undo.DestroyObjectImmediate(mb);
                }
            }
        }

        private static AudioClip LoadAudioMatching(string query, string subFolder = "")
        {
            if (string.IsNullOrEmpty(query)) return null;

            if (audioCache == null || audioCache.Count == 0)
            {
                audioCache = new Dictionary<string, AudioClip>(System.StringComparer.OrdinalIgnoreCase);
                audioList = new List<string>();

                string[] guids = AssetDatabase.FindAssets("t:AudioClip");
                foreach (string g in guids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(p);
                    if (clip != null && !audioCache.ContainsKey(clip.name))
                    {
                        audioCache[clip.name] = clip;
                        audioList.Add(clip.name);
                    }
                }
            }

            if (audioCache.TryGetValue(query, out AudioClip exact)) return exact;

            string qClean = CleanAudioQuery(query);
            foreach (string name in audioList)
            {
                string nClean = CleanAudioQuery(name);
                if (nClean == qClean || nClean.Contains(qClean) || qClean.Contains(nClean))
                    return audioCache[name];
            }
            return null;
        }

        private static string CleanAudioQuery(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.ToLower()
                .Replace("'", "")
                .Replace("’", "")
                .Replace("\"", "")
                .Replace(".", "")
                .Replace("!", "")
                .Replace("?", "")
                .Replace(",", "")
                .Replace(" ", "")
                .Replace("-", "")
                .Replace("_", "")
                .Trim();
        }

        private static Sprite LoadSpriteMatching(string query)
        {
            if (string.IsNullOrEmpty(query)) return null;

            if (spriteCache == null)
            {
                spriteCache = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
                string[] guids = AssetDatabase.FindAssets("t:Sprite");
                foreach (string g in guids)
                {
                    string p = AssetDatabase.GUIDToAssetPath(g);
                    Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(p);
                    foreach (var sub in subAssets)
                    {
                        if (sub is Sprite sp && !spriteCache.ContainsKey(sp.name))
                        {
                            spriteCache[sp.name] = sp;
                        }
                    }
                }
            }

            if (spriteCache.TryGetValue(query, out Sprite exact)) return exact;

            foreach (var kv in spriteCache)
            {
                if (kv.Key.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return kv.Value;
            }
            return null;
        }
    }
}
