using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics.EditorTools
{
    public class U7_HierarchyAutomator : EditorWindow
    {
        private const string AUDIO_BASE_PATH = "Assets/Audio/U7_audio";
        private const string ART_BASE_PATH = "Assets/Art/Unit7_MP";

        [MenuItem("Masters Phonics/Unit 7/Generate Unit 7 Hierarchy (Clean & Complete)", false, 100)]
        public static void GenerateUnit7Hierarchy()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in the current Scene!", "OK");
                return;
            }

            Transform sourceUnit = canvas.transform.Find("Unit_6") ?? canvas.transform.Find("Unit_5") ?? canvas.transform.Find("Unit_4");
            if (sourceUnit == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find 'Unit_6', 'Unit_5', or 'Unit_4' under Canvas as a base template!", "OK");
                return;
            }

            // Check if Unit_7 already exists
            Transform existingU7 = canvas.transform.Find("Unit_7");
            if (existingU7 != null)
            {
                bool overwrite = EditorUtility.DisplayDialog("Unit_7 Exists", "Unit_7 already exists in Canvas. Do you want to replace it with a clean Unit 7 hierarchy?", "Yes, Replace", "Cancel");
                if (!overwrite) return;
                Undo.DestroyObjectImmediate(existingU7.gameObject);
            }

            spriteCache = null;
            audioCache = null;
            audioList = null;

            // 1. Duplicate Template Root
            GameObject unit7Obj = Object.Instantiate(sourceUnit.gameObject, canvas.transform);
            unit7Obj.name = "Unit_7";
            Undo.RegisterCreatedObjectUndo(unit7Obj, "Generate Clean Unit_7");

            // Strip Old Root Components
            StripOldUnitComponents(unit7Obj);

            // Add Unit 7 Managers
            var flowManager = unit7Obj.AddComponent<U7_SA_UnitFlowManager_Masters_Phonics>();
            var audioManager = unit7Obj.AddComponent<U7_SA_AudioManager_Masters_Phonics>();

            // Assign Root Audio Manager & Flow Manager Clips
            AssignRootAudioClips(audioManager, flowManager);

            // Setup Section Selection Panels (Signboard)
            Transform selPanels = unit7Obj.transform.Find("Unit_6_Section_Selection_Panels") ?? unit7Obj.transform.Find("Unit_5_Section_Selection_Panels") ?? unit7Obj.transform.Find("Unit_7_Section_Selection_Panels");
            if (selPanels != null)
            {
                selPanels.name = "Unit_7_Section_Selection_Panels";
                UpdateSignboardCards(selPanels, flowManager);
            }

            // Setup Sections Parent
            Transform sectionsParent = unit7Obj.transform.Find("Unit_6_Sections") ?? unit7Obj.transform.Find("Unit_5_Sections") ?? unit7Obj.transform.Find("Unit_7_Sections");
            if (sectionsParent == null)
            {
                GameObject secGo = new GameObject("Unit_7_Sections", typeof(RectTransform));
                secGo.transform.SetParent(unit7Obj.transform, false);
                sectionsParent = secGo.transform;
            }
            sectionsParent.name = "Unit_7_Sections";

            // Clean up old leftover unit panels
            for (int i = sectionsParent.childCount - 1; i >= 0; i--)
            {
                Transform child = sectionsParent.GetChild(i);
                string cName = child.name;
                if (cName.StartsWith("U1_") || cName.StartsWith("U2_") || cName.StartsWith("U3_") || cName.StartsWith("U4_") || cName.StartsWith("U5_") || cName.StartsWith("U6_"))
                {
                    Undo.DestroyObjectImmediate(child.gameObject);
                }
            }

            // Ensure BG_Image exists, is active, and is the first sibling (behind all activities)
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
                Image bgImg = bgObj.GetComponent<Image>();
                Sprite bgSprite = LoadSpriteMatching("U07_BG_Part1_Spelling") ?? LoadSpriteMatching("BG");
                if (bgSprite != null) bgImg.sprite = bgSprite;
                bgImg.color = Color.white;
            }
            bgTr.gameObject.name = "BG_Image";
            bgTr.gameObject.SetActive(true);
            bgTr.SetAsFirstSibling();

            // Clean & Build dedicated Panels for each Unit 7 section with Audio Wired
            GameObject learnGo = BuildOrCleanPanel(sectionsParent, "U7_learn_Concept_Cards_Panel");
            SetupGM01ConceptCards(learnGo);

            GameObject act1Go = BuildOrCleanPanel(sectionsParent, "U7_Activity_1_Team_Up");
            SetupGM04TeamUp(act1Go);

            GameObject act2Go = BuildOrCleanPanel(sectionsParent, "U7_Activity_2_Word_Families");
            SetupGM03WordFamilies(act2Go);

            GameObject act3Go = BuildOrCleanPanel(sectionsParent, "U7_Activity_3_Same_Sound");
            SetupGM03SameSound(act3Go);

            GameObject act4Go = BuildOrCleanPanel(sectionsParent, "U7_Activity_4_Glide_Or_Hold");
            SetupGM04GlideOrHold(act4Go);

            GameObject act5Go = BuildOrCleanPanel(sectionsParent, "U7_Activity_5_Glide_Families");
            SetupGM03sGlideFamilies(act5Go);

            GameObject act6Go = BuildOrCleanPanel(sectionsParent, "U7_Activity_6_Team_Rush");
            SetupGM08TeamRush(act6Go);

            GameObject mapGo = BuildOrCleanPanel(sectionsParent, "U7_SevenTypesMap_Panel");
            SetupSevenTypesMap(mapGo);

            GameObject challengeGo = BuildOrCleanPanel(sectionsParent, "U7_UnitChallenge_Panel");
            SetupUnitChallenge(challengeGo);

            GameObject compGo = BuildOrCleanPanel(sectionsParent, "U7_COMPLETE_Panel");
            SetupCompletionPanel(compGo);

            // Wire Flow Manager
            WireFlowManager(flowManager, selPanels, sectionsParent, learnGo, act1Go, act2Go, act3Go, act4Go, act5Go, act6Go, mapGo, challengeGo, compGo);

            EditorUtility.SetDirty(unit7Obj);
            Selection.activeGameObject = unit7Obj;

            EditorUtility.DisplayDialog("Unit 7 Hierarchy Created!", 
                "Successfully generated clean Unit 7 GameObject hierarchies with all 10 activities/panels and wired all Voice A, Voice B, and SFX audio clips!", 
                "Great!");
        }

        private static GameObject BuildOrCleanPanel(Transform parent, string panelName)
        {
            Transform existing = parent.Find(panelName);
            if (existing != null)
            {
                for (int i = existing.childCount - 1; i >= 0; i--)
                {
                    DestroyImmediate(existing.GetChild(i).gameObject);
                }
                StripOldUnitComponents(existing.gameObject);

                Image panelImg = existing.GetComponent<Image>();
                if (panelImg != null && panelImg.sprite == null)
                {
                    DestroyImmediate(panelImg);
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

        private static void StripOldUnitComponents(GameObject go)
        {
            var monoBehaviours = go.GetComponents<MonoBehaviour>();
            foreach (var mb in monoBehaviours)
            {
                if (mb == null) continue;
                string typeName = mb.GetType().Name;
                if (typeName.StartsWith("U1_") || typeName.StartsWith("U2_") || typeName.StartsWith("U3_") || typeName.StartsWith("U4_") || typeName.StartsWith("U5_") || typeName.StartsWith("U6_"))
                {
                    DestroyImmediate(mb);
                }
            }
        }

        // =========================================================================
        // Root Audio & Flow Manager Assignment
        // =========================================================================

        private static void AssignRootAudioClips(U7_SA_AudioManager_Masters_Phonics audioMgr, U7_SA_UnitFlowManager_Masters_Phonics flowMgr)
        {
            audioMgr.correctClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX") ?? LoadAudioMatching("Correct", "SFX");
            audioMgr.wrongClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX") ?? LoadAudioMatching("Incorrect", "SFX");
            audioMgr.clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/AB_Pop_boosted_220.mp3");
            audioMgr.celebrationClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_map_complete.wav") ?? LoadAudioMatching("celebration") ?? LoadAudioMatching("complete");
            audioMgr.teamMergeClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_chunk_snap.wav");
            audioMgr.waveGlideClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_vowel_stretch.wav");
            audioMgr.waveHoldClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_vowel_snap.wav");
            audioMgr.gapFillClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U02_SFX_tile_land.wav");
            audioMgr.binCollectClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_grid_found.wav");
            audioMgr.wordFlyClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U02_SFX_swipe_left.wav");

            flowMgr.unitIntroClip = LoadAudioMatching("Unit Seven is the big one Two ideas") ?? LoadAudioMatching("Two ideas");
            flowMgr.unitCompleteVoiceClip = LoadAudioMatching("Unit Seven done the longest one in the") ?? LoadAudioMatching("longest one in the");

            Transform mascotTr = audioMgr.transform.Find("City theme boy Front view") 
                              ?? audioMgr.transform.Find("Mascot") 
                              ?? audioMgr.transform.Find("CityThemeBoy");
            if (mascotTr != null)
            {
                SerializedObject soAudio = new SerializedObject(audioMgr);
                SetProp(soAudio, "mascotObject", mascotTr.gameObject);
                soAudio.ApplyModifiedProperties();
            }
        }

        // =========================================================================
        // Panel Setup Handlers
        // =========================================================================

        // 1. LEARN: Concept Cards (GM-01)
        private static void SetupGM01ConceptCards(GameObject panel)
        {
            var gm01 = panel.AddComponent<U7_SA_GM01_ConceptCards_Masters_Phonics>();

            gm01.card1IntroClip = LoadAudioMatching("A vowel team is two vowel letters sitting") ?? LoadAudioMatching("vowel team is two");
            gm01.card2IntroClip = LoadAudioMatching("Heres the annoying part One sound can be") ?? LoadAudioMatching("annoying part");
            gm01.card3IntroClip = LoadAudioMatching("Same two letters Different sound English does this") ?? LoadAudioMatching("Same two letters");
            gm01.card4IntroClip = LoadAudioMatching("Coooiiin Hear that It started in one place") ?? LoadAudioMatching("Coooiiin") ?? LoadAudioMatching("Eight gliding sounds");
            gm01.card5IntroClip = LoadAudioMatching("Careful here these two things arent opposites Vowel") ?? LoadAudioMatching("Careful here");

            CreateHeader(panel.transform, "CONCEPT CARDS", "Vowel Teams & Diphthongs: Letters That Work Together", out _, out _);
            
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));
            GameObject cardContainer = CreateContainer(panel.transform, "CardContainer", new Vector2(0, -30f), new Vector2(1200f, 620f));

            Button prevBtn = CreateNavButton(panel.transform, "PrevButton", "Assets/Icons/backbutton.png", "PREV", new Vector2(-720f, -420f), new Vector2(220f, 75f));
            Button nextBtn = CreateNavButton(panel.transform, "NextButton", "Assets/Icons/nextt.png", "NEXT", new Vector2(720f, -420f), new Vector2(220f, 75f));
            prevBtn.transform.SetAsLastSibling();
            nextBtn.transform.SetAsLastSibling();

            CreateProgressHUD(panel.transform, out Slider pBar, out TextMeshProUGUI pText);

            SerializedObject so = new SerializedObject(gm01);
            SetProp(so, "cardsContainer", cardContainer.transform);
            SetProp(so, "nextCardBtn", nextBtn);
            SetProp(so, "prevCardBtn", prevBtn);
            SetProp(so, "cardCounterText", pText);
            SetProp(so, "card1IntroClip", gm01.card1IntroClip);
            SetProp(so, "card2IntroClip", gm01.card2IntroClip);
            SetProp(so, "card3IntroClip", gm01.card3IntroClip);
            SetProp(so, "card4IntroClip", gm01.card4IntroClip);
            SetProp(so, "card5IntroClip", gm01.card5IntroClip);
            so.ApplyModifiedProperties();
        }

        // 2. ACTIVITY 1: Team Up (GM-04 Picture Gap Fill)
        private static void SetupGM04TeamUp(GameObject panel)
        {
            var gm04 = panel.AddComponent<U7_SA_GM04_TeamUp_Masters_Phonics>();

            gm04.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX");
            gm04.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX");

            CreateHeader(panel.transform, "ACTIVITY 1: TEAM UP", "Choose the right vowel team to complete each word!", out _, out _);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            GameObject cardCont = CreateContainer(panel.transform, "PictureWordCard", new Vector2(0f, 80f), new Vector2(1050f, 440f));
            GameObject pictureObj = CreateContainer(cardCont.transform, "PictureDisplay", new Vector2(0f, 85f), new Vector2(300f, 200f));
            Image picImg = pictureObj.AddComponent<Image>();
            picImg.preserveAspect = true;

            GameObject wordRow = CreateContainer(cardCont.transform, "WordDisplayRow", new Vector2(0f, -80f), new Vector2(750f, 100f));
            TextMeshProUGUI wordTMP = CreateText(wordRow.transform, "GappedWordText", "r _ _ n", 60);

            GameObject tilesRow = CreateContainer(panel.transform, "TileOptionsContainer", new Vector2(0f, -220f), new Vector2(1000f, 130f));
            var grid = tilesRow.AddComponent<HorizontalLayoutGroup>();
            grid.spacing = 30f;
            grid.childAlignment = TextAnchor.MiddleCenter;
            grid.childControlWidth = false;
            grid.childControlHeight = false;

            string[] teamLabels = { "ai", "oa", "ee", "i_e" };
            for (int i = 0; i < teamLabels.Length; i++)
            {
                GameObject tileBtn = CreateContainer(tilesRow.transform, $"Tile_{teamLabels[i]}", Vector2.zero, new Vector2(200f, 100f));
                Image tImg = tileBtn.AddComponent<Image>();
                tImg.sprite = GetOrCreateRoundedSprite();
                tImg.type = Image.Type.Sliced;
                tImg.color = Color.white;
                tileBtn.AddComponent<Button>();
                var txt = CreateText(tileBtn.transform, "Text", $"<b>{teamLabels[i]}</b>", 42);
                txt.color = new Color(0.06f, 0.09f, 0.16f, 1f); // Solid dark black
            }

            CreateProgressHUD(panel.transform, out Slider pBar, out TextMeshProUGUI pText);
            CreateScoreHUD(panel.transform, out TextMeshProUGUI scoreTMP, out TextMeshProUGUI streakTMP);
        }

        // 3. ACTIVITY 2: Word Families (GM-03 4-Bin Sort)
        private static void SetupGM03WordFamilies(GameObject panel)
        {
            var gm03 = panel.AddComponent<U7_SA_GM03_WordFamilies_Masters_Phonics>();

            gm03.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX");
            gm03.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX");

            CreateHeader(panel.transform, "ACTIVITY 2: WORD FAMILIES", "Sort words into the matching vowel team family bins!", out _, out _);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            GameObject binsRow = CreateContainer(panel.transform, "BinsContainer", new Vector2(0f, -170f), new Vector2(1300f, 220f));
            var hlg = binsRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 25f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            for (int i = 0; i < 4; i++)
            {
                GameObject bin = CreateContainer(binsRow.transform, $"Bin_{i + 1}", Vector2.zero, new Vector2(280f, 190f));
                Image bImg = bin.AddComponent<Image>();
                bImg.sprite = GetOrCreateRoundedSprite();
                bImg.type = Image.Type.Sliced;
                bImg.color = new Color(0.15f, 0.22f, 0.32f, 1f);
                bin.AddComponent<Button>();
                CreateText(bin.transform, "BinTitle", $"<b>Bin {i + 1}</b>", 34);
            }

            GameObject cardCont = CreateContainer(panel.transform, "CurrentWordCard", new Vector2(0f, 120f), new Vector2(480f, 180f));
            Image cImg = cardCont.AddComponent<Image>();
            cImg.sprite = GetOrCreateRoundedSprite();
            cImg.type = Image.Type.Sliced;
            cImg.color = Color.white;
            CreateText(cardCont.transform, "WordText", "<b>rain</b>", 56);

            CreateProgressHUD(panel.transform, out Slider pBar, out TextMeshProUGUI pText);
            CreateScoreHUD(panel.transform, out TextMeshProUGUI scoreTMP, out TextMeshProUGUI streakTMP);
        }

        // 4. ACTIVITY 3: Same Sound (GM-03 Auditory Sound Sort)
        private static void SetupGM03SameSound(GameObject panel)
        {
            var gm03s = panel.AddComponent<U7_SA_GM03_SameSound_Masters_Phonics>();

            gm03s.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX");
            gm03s.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX");

            CreateHeader(panel.transform, "ACTIVITY 3: SAME SOUND", "Listen carefully! Match different spellings that make the EXACT SAME sound!", out _, out _);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            GameObject binsRow = CreateContainer(panel.transform, "SoundBinsContainer", new Vector2(0f, -140f), new Vector2(1200f, 200f));
            var hlg = binsRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 35f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            string[] sounds = { "Long A /eɪ/", "Long E /iː/", "Long O /əʊ/" };
            for (int i = 0; i < sounds.Length; i++)
            {
                GameObject bin = CreateContainer(binsRow.transform, $"SoundBin_{i + 1}", Vector2.zero, new Vector2(340f, 180f));
                Image bImg = bin.AddComponent<Image>();
                bImg.sprite = GetOrCreateRoundedSprite();
                bImg.type = Image.Type.Sliced;
                bImg.color = new Color(0.14f, 0.2f, 0.3f, 1f);
                bin.AddComponent<Button>();
                CreateText(bin.transform, "Title", $"<b>{sounds[i]}</b>", 30);
            }

            GameObject cardCont = CreateContainer(panel.transform, "AuditoryWordCard", new Vector2(0f, 130f), new Vector2(500f, 180f));
            Image cImg = cardCont.AddComponent<Image>();
            cImg.sprite = GetOrCreateRoundedSprite();
            cImg.type = Image.Type.Sliced;
            cImg.color = Color.white;
            CreateText(cardCont.transform, "WordText", "<b>day</b>", 54);

            CreateProgressHUD(panel.transform, out Slider pBar, out TextMeshProUGUI pText);
            CreateScoreHUD(panel.transform, out TextMeshProUGUI scoreTMP, out TextMeshProUGUI streakTMP);
        }

        // 5. ACTIVITY 4: Glide or Hold? (GM-04 Waveform Discrimination)
        private static void SetupGM04GlideOrHold(GameObject panel)
        {
            var gm04 = panel.AddComponent<U7_SA_GM04_GlideOrHold_Masters_Phonics>();

            gm04.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX");
            gm04.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX");

            CreateHeader(panel.transform, "ACTIVITY 4: GLIDE OR HOLD?", "Does the vowel GLIDE toward a second sound (Diphthong) or HOLD steady?", out _, out _);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            GameObject waveCont = CreateContainer(panel.transform, "WaveformVisual", new Vector2(0f, 110f), new Vector2(560f, 190f));
            Image wImg = waveCont.AddComponent<Image>();
            wImg.sprite = GetOrCreateRoundedSprite(128, 24);
            wImg.type = Image.Type.Sliced;
            wImg.color = new Color(1f, 1f, 1f, 0.96f);
            var wTmp = CreateText(waveCont.transform, "WaveformText", "<b>day</b>", 58);
            wTmp.color = new Color(0.06f, 0.09f, 0.16f, 1f);

            GameObject btnsRow = CreateContainer(panel.transform, "ChoiceButtons", new Vector2(0f, -180f), new Vector2(800f, 140f));
            var hlg = btnsRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 50f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            GameObject glideBtn = CreateContainer(btnsRow.transform, "GlideChoiceBtn", Vector2.zero, new Vector2(340f, 110f));
            Image gImg = glideBtn.AddComponent<Image>();
            gImg.sprite = GetOrCreateRoundedSprite(128, 16);
            gImg.type = Image.Type.Sliced;
            gImg.color = Color.white;
            glideBtn.AddComponent<Button>();
            var gTmp = CreateText(glideBtn.transform, "Text", "<b>GLIDE</b> <size=80%>(Diphthong)</size>", 28);
            gTmp.color = new Color(0.06f, 0.09f, 0.16f, 1f);

            GameObject holdBtn = CreateContainer(btnsRow.transform, "HoldChoiceBtn", Vector2.zero, new Vector2(340f, 110f));
            Image hImg = holdBtn.AddComponent<Image>();
            hImg.sprite = GetOrCreateRoundedSprite(128, 16);
            hImg.type = Image.Type.Sliced;
            hImg.color = Color.white;
            holdBtn.AddComponent<Button>();
            var hTmp = CreateText(holdBtn.transform, "Text", "<b>HOLD</b> <size=80%>(Steady Vowel)</size>", 28);
            hTmp.color = new Color(0.06f, 0.09f, 0.16f, 1f);

            CreateProgressHUD(panel.transform, out Slider pBar, out TextMeshProUGUI pText);
            CreateScoreHUD(panel.transform, out TextMeshProUGUI scoreTMP, out TextMeshProUGUI streakTMP);
        }

        // 6. ACTIVITY 5: Glide Families (GM-03s Fast Swipe Sort)
        private static void SetupGM03sGlideFamilies(GameObject panel)
        {
            var gm03s = panel.AddComponent<U7_SA_GM03s_GlideFamilies_Masters_Phonics>();

            gm03s.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX");
            gm03s.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX");

            CreateHeader(panel.transform, "ACTIVITY 5: GLIDE FAMILIES", "Swipe Left for Middle spelling, Swipe Right for End spelling!", out _, out _);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            GameObject bannerObj = CreateContainer(panel.transform, "RuleBanner", new Vector2(0f, 260f), new Vector2(1000f, 60f));
            Image bImg = bannerObj.AddComponent<Image>();
            bImg.sprite = GetOrCreateRoundedSprite();
            bImg.type = Image.Type.Sliced;
            bImg.color = new Color(0.1f, 0.15f, 0.25f, 0.9f);
            CreateText(bannerObj.transform, "RuleBannerText", "<b>Rule: oi in middle | oy at end</b>", 24);

            GameObject midBin = CreateContainer(panel.transform, "MiddleLeftBin", new Vector2(-480f, 0f), new Vector2(280f, 360f));
            Image mbImg = midBin.AddComponent<Image>();
            mbImg.sprite = GetOrCreateRoundedSprite();
            mbImg.type = Image.Type.Sliced;
            mbImg.color = new Color(0.08f, 0.45f, 0.6f, 0.85f);
            midBin.AddComponent<Button>();
            CreateText(midBin.transform, "Title", "<b>MIDDLE\n<size=120%>oi / ou</size></b>", 30);

            GameObject endBin = CreateContainer(panel.transform, "EndRightBin", new Vector2(480f, 0f), new Vector2(280f, 360f));
            Image ebImg = endBin.AddComponent<Image>();
            ebImg.sprite = GetOrCreateRoundedSprite();
            ebImg.type = Image.Type.Sliced;
            ebImg.color = new Color(0.7f, 0.45f, 0.1f, 0.85f);
            endBin.AddComponent<Button>();
            CreateText(endBin.transform, "Title", "<b>END\n<size=120%>oy / ow</size></b>", 30);

            GameObject cardCont = CreateContainer(panel.transform, "CardContainer", new Vector2(0f, 0f), new Vector2(440f, 260f));
            Image cImg = cardCont.AddComponent<Image>();
            cImg.sprite = GetOrCreateRoundedSprite();
            cImg.type = Image.Type.Sliced;
            cImg.color = Color.white;
            CreateText(cardCont.transform, "WordText", "<b>c _ _ n</b>", 52);

            CreateProgressHUD(panel.transform, out Slider pBar, out TextMeshProUGUI pText);
            CreateScoreHUD(panel.transform, out TextMeshProUGUI scoreTMP, out TextMeshProUGUI streakTMP);
        }

        // 7. ACTIVITY 6: Team Rush (GM-08 60s Speed Round)
        private static void SetupGM08TeamRush(GameObject panel)
        {
            var gm08 = panel.AddComponent<U7_SA_GM08_TeamRush_Masters_Phonics>();

            gm08.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX");
            gm08.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX");

            CreateHeader(panel.transform, "ACTIVITY 6: TEAM RUSH", "Sort fast into the 4 sound chutes before time runs out!", out _, out _);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            GameObject cardCont = CreateContainer(panel.transform, "CurrentCard", new Vector2(0f, 100f), new Vector2(460f, 180f));
            Image cImg = cardCont.AddComponent<Image>();
            cImg.sprite = GetOrCreateRoundedSprite();
            cImg.type = Image.Type.Sliced;
            cImg.color = Color.white;
            CreateText(cardCont.transform, "WordText", "<b>rain</b>", 54);

            GameObject chutesRow = CreateContainer(panel.transform, "Chutes", new Vector2(0f, -180f), new Vector2(1300f, 180f));
            var hlg = chutesRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20f;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            string[] chuteNames = { "Chute1_LongA", "Chute2_LongE", "Chute3_LongO", "Chute4_Diphthong" };
            string[] chuteLabels = { "/eɪ/ Long A", "/iː/ Long E", "/əʊ/ Long O", "Diphthong" };
            for (int i = 0; i < 4; i++)
            {
                GameObject chute = CreateContainer(chutesRow.transform, chuteNames[i], Vector2.zero, new Vector2(290f, 150f));
                Image chImg = chute.AddComponent<Image>();
                chImg.sprite = GetOrCreateRoundedSprite();
                chImg.type = Image.Type.Sliced;
                chImg.color = new Color(0.18f, 0.24f, 0.35f, 1f);
                chute.AddComponent<Button>();
                CreateText(chute.transform, "Label", $"<b>{chuteLabels[i]}</b>", 28);
            }

            CreateProgressHUD(panel.transform, out Slider pBar, out TextMeshProUGUI pText);
            CreateScoreHUD(panel.transform, out TextMeshProUGUI scoreTMP, out TextMeshProUGUI streakTMP);
        }

        // 8. SEVEN TYPES MAP
        private static void SetupSevenTypesMap(GameObject panel)
        {
            var map = panel.AddComponent<U7_SA_SevenTypesMap_Masters_Phonics>();

            map.mapIntroClip = LoadAudioMatching("Seven Syllable Types", "voice A") ?? LoadAudioMatching("U07_VO_map_unit7", "voice A");

            CreateHeader(panel.transform, "7 SYLLABLE TYPES MAP", "5 of 7 Syllable Types Mastered! Vowel Teams & Diphthongs Unlocked!", out _, out _);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            GameObject gridCont = CreateContainer(panel.transform, "Tiles_Container", new Vector2(0f, -20f), new Vector2(1100f, 580f));
            var grid = gridCont.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(340f, 130f);
            grid.spacing = new Vector2(20f, 20f);
            grid.childAlignment = TextAnchor.MiddleCenter;

            Button contBtn = CreateNavButton(panel.transform, "ContinueButton", "Assets/Icons/nextt.png", "NEXT", new Vector2(720f, -420f), new Vector2(220f, 75f));
            contBtn.transform.SetAsLastSibling();
        }

        // 9. UNIT CHALLENGE
        private static void SetupUnitChallenge(GameObject panel)
        {
            var ch = panel.AddComponent<U7_SA_UnitChallenge_Masters_Phonics>();

            ch.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX");
            ch.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX");

            CreateHeader(panel.transform, "UNIT 7 CHALLENGE", "Show what you know about vowel teams and diphthongs!", out _, out _);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            GameObject qBox = CreateContainer(panel.transform, "QuestionBox", new Vector2(0f, 140f), new Vector2(1100f, 160f));
            Image qImg = qBox.AddComponent<Image>();
            qImg.sprite = GetOrCreateRoundedSprite();
            qImg.type = Image.Type.Sliced;
            qImg.color = new Color(0.1f, 0.16f, 0.25f, 0.95f);
            CreateText(qBox.transform, "PromptText", "<b>Question prompt goes here...</b>", 34);

            GameObject optsCont = CreateContainer(panel.transform, "OptionsContainer", new Vector2(0f, -120f), new Vector2(1100f, 260f));
            var vlg = optsCont.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = false;

            for (int i = 0; i < 3; i++)
            {
                GameObject optBtn = CreateContainer(optsCont.transform, $"Option_{i + 1}", Vector2.zero, new Vector2(1000f, 65f));
                Image oImg = optBtn.AddComponent<Image>();
                oImg.sprite = GetOrCreateRoundedSprite();
                oImg.type = Image.Type.Sliced;
                oImg.color = new Color(0.92f, 0.94f, 0.98f, 1f);
                optBtn.AddComponent<Button>();
                CreateText(optBtn.transform, "Text", $"<b>Option {i + 1}</b>", 26);
            }

            CreateProgressHUD(panel.transform, out Slider pBar, out TextMeshProUGUI pText);
            CreateScoreHUD(panel.transform, out TextMeshProUGUI scoreTMP, out TextMeshProUGUI streakTMP);
        }

        // 10. COMPLETION PANEL
        private static void SetupCompletionPanel(GameObject panel)
        {
            CreateHeader(panel.transform, "UNIT 7 COMPLETE!", "Congratulations on mastering Vowel Teams & Diphthongs!", out _, out _);

            GameObject modal = CreateContainer(panel.transform, "CompletionModal", new Vector2(0f, 0f), new Vector2(900f, 540f));
            Image mImg = modal.AddComponent<Image>();
            mImg.sprite = GetOrCreateRoundedSprite();
            mImg.type = Image.Type.Sliced;
            mImg.color = new Color(0.12f, 0.18f, 0.28f, 0.98f);

            CreateText(modal.transform, "BadgeTitle", "<b><color=#FFD700>BADGE UNLOCKED!</color></b>\n<size=120%>Sound Glider</size>", 36);

            Button homeBtn = CreateNavButton(modal.transform, "ReturnButton", "Assets/Icons/home.png", "LESSONS", new Vector2(0f, -190f), new Vector2(260f, 75f));
            homeBtn.onClick.AddListener(() =>
            {
                if (U7_SA_UnitFlowManager_Masters_Phonics.Instance != null)
                    U7_SA_UnitFlowManager_Masters_Phonics.Instance.ReturnToLessonsMenu();
            });
        }

        // =========================================================================
        // Flow Manager Wiring
        // =========================================================================

        private static void WireFlowManager(U7_SA_UnitFlowManager_Masters_Phonics flowMgr, 
            Transform selPanels, Transform sectionsParent,
            GameObject learn, GameObject act1, GameObject act2, GameObject act3, 
            GameObject act4, GameObject act5, GameObject act6, GameObject map, 
            GameObject challenge, GameObject comp)
        {
            SerializedObject so = new SerializedObject(flowMgr);
            SetProp(so, "sectionSelectionPanel", selPanels != null ? selPanels.gameObject : null);
            SetProp(so, "sectionsParent", sectionsParent != null ? sectionsParent.gameObject : null);

            Transform bgTr = sectionsParent != null ? (sectionsParent.Find("BG_Image") ?? sectionsParent.Find("Background")) : null;
            if (bgTr != null)
            {
                SetProp(so, "bgImageComponent", bgTr.GetComponent<Image>());
            }

            Sprite sp1 = LoadSpriteMatching("U07_BG_Part1_Spelling");
            Sprite sp2 = LoadSpriteMatching("U07_BG_Part2_Sound");
            if (sp1 != null) SetProp(so, "part1SpellingBGSprite", sp1);
            if (sp2 != null) SetProp(so, "part2SoundBGSprite", sp2);

            Sprite goldStar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png.png");
            Sprite greyStar = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ENGSNAP_ASSETS/Everyday Greetings/Quiz/mobile-game-golden-star-clipart-design-illustration-free-png (1).png");
            if (goldStar != null) SetProp(so, "goldenStarSprite", goldStar);
            if (greyStar != null) SetProp(so, "emptyStarSprite", greyStar);

            SetProp(so, "learnConceptCardsPanel", learn);
            SetProp(so, "activity1TeamUpPanel", act1);
            SetProp(so, "activity2WordFamiliesPanel", act2);
            SetProp(so, "activity3SameSoundPanel", act3);
            SetProp(so, "activity4GlideOrHoldPanel", act4);
            SetProp(so, "activity5GlideFamiliesPanel", act5);
            SetProp(so, "activity6TeamRushPanel", act6);
            SetProp(so, "sevenTypesMapPanel", map);
            SetProp(so, "unitChallengePanel", challenge);
            SetProp(so, "completionPanel", comp);

            so.ApplyModifiedProperties();

            if (selPanels != null)
            {
                UpdateSignboardCards(selPanels, flowMgr);
            }
        }

        [MenuItem("Masters Phonics/Unit 7/Setup Section Selection Signboard Cards & Buttons", false, 102)]
        public static void MenuSetupSignboardCards()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas == null) return;
            Transform u7 = canvas.transform.Find("Unit_7") ?? canvas.transform.Find("Unit7");
            if (u7 == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find 'Unit_7' under Canvas!", "OK");
                return;
            }
            var flow = u7.GetComponent<U7_SA_UnitFlowManager_Masters_Phonics>() ?? u7.GetComponentInChildren<U7_SA_UnitFlowManager_Masters_Phonics>(true);
            Transform selPanels = u7.Find("Unit_7_Section_Selection_Panels") ?? u7.Find("Section_Selection_Panels") ?? u7.Find("Unit_6_Section_Selection_Panels");
            if (selPanels == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find Section Selection Panels in Unit 7!", "OK");
                return;
            }
            UpdateSignboardCards(selPanels, flow);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(u7.gameObject.scene);
            EditorUtility.DisplayDialog("Unit 7 Section Selection Updated!", 
                "Successfully updated all 6 activity card titles inside 'Unit_7_Section_Selection_Panels' and wired persistent button listeners to Unit 7 Flow Manager!", "Great!");
        }

        public static void UpdateSignboardCards(Transform selPanels, U7_SA_UnitFlowManager_Masters_Phonics flow)
        {
            if (selPanels == null) return;
            Undo.RecordObject(selPanels.gameObject, "Update Signboard Cards");

            // 1. Update Signboard Header Title
            Transform titleTr = selPanels.Find("Header/Title") 
                             ?? selPanels.Find("Header/Title_Text")
                             ?? selPanels.Find("Title_Text") 
                             ?? selPanels.Find("TitleText") 
                             ?? selPanels.Find("Title");
            if (titleTr != null)
            {
                var tmp = titleTr.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "<b>UNIT 7: VOWEL TEAMS & DIPHTHONGS</b>";
                    EditorUtility.SetDirty(tmp);
                }
                var txt = titleTr.GetComponent<Text>();
                if (txt != null)
                {
                    txt.text = "UNIT 7: VOWEL TEAMS & DIPHTHONGS";
                    EditorUtility.SetDirty(txt);
                }
            }

            // 2. Find Card Container
            Transform content = selPanels.Find("Viewport/Content")
                             ?? selPanels.Find("Content")
                             ?? selPanels.Find("Buttons_Container")
                             ?? selPanels.Find("Trucks_Container")
                             ?? selPanels.Find("Cards_Container")
                             ?? selPanels.Find("Scroll View/Viewport/Content")
                             ?? selPanels;

            // 3. Find Cards / Section Panels
            List<Transform> cards = new List<Transform>();
            foreach (Transform child in content)
            {
                if (child == null) continue;
                string cName = child.name.ToLower();
                if (cName.Contains("bg") || cName.Contains("back") || cName.Contains("title") || cName.Contains("header") || cName.Contains("topbar")) continue;
                if (child.Find("Unlocked") != null || child.Find("unlocked") != null || cName.Contains("section") || cName.Contains("card") || cName.Contains("truck") || cName.Contains("panel"))
                {
                    cards.Add(child);
                }
            }

            // If we have fewer than 6 cards, duplicate the last card so all 6 activities are present
            if (cards.Count > 0 && cards.Count < 6)
            {
                while (cards.Count < 6)
                {
                    Transform lastCard = cards[cards.Count - 1];
                    GameObject newCard = Object.Instantiate(lastCard.gameObject, content);
                    newCard.name = $"Section {((char)('A' + cards.Count))} Panel";
                    Undo.RegisterCreatedObjectUndo(newCard, "Add Section Panel Card");
                    cards.Add(newCard.transform);
                }
            }

            string[] activityTitles = new string[]
            {
                "ACTIVITY 1 — TEAM UP",
                "ACTIVITY 2 — WORD FAMILIES",
                "ACTIVITY 3 — SAME SOUND",
                "ACTIVITY 4 — GLIDE OR HOLD?",
                "ACTIVITY 5 — GLIDE FAMILIES",
                "ACTIVITY 6 — TEAM RUSH (bonus)"
            };

            string[] methodNames = new string[]
            {
                "OpenActivity1",
                "OpenActivity2",
                "OpenActivity3",
                "OpenActivity4",
                "OpenActivity5",
                "OpenActivity6"
            };

            for (int i = 0; i < cards.Count && i < activityTitles.Length; i++)
            {
                Transform card = cards[i];
                Transform unlocked = card.Find("Unlocked") ?? card.Find("unlocked") ?? card.Find("Unlocked_Parent") ?? card;
                Transform locked = card.Find("Locked") ?? card.Find("locked");

                if (unlocked != null) unlocked.gameObject.SetActive(true);
                if (locked != null) locked.gameObject.SetActive(false);

                // Update text inside Unlocked / Card only if empty or placeholder
                TextMeshProUGUI[] tmps = unlocked.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (tmps.Length == 0) tmps = card.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmps)
                {
                    if (tmp == null) continue;
                    if (string.IsNullOrEmpty(tmp.text) || tmp.text.Contains("UNIT 6") || tmp.text.Contains("MAGIC") || tmp.text.Contains("Section") || tmp.text.Contains("UNIT 7: VOWEL TEAMS"))
                    {
                        tmp.text = $"<b>{activityTitles[i]}</b>";
                        tmp.fontStyle = FontStyles.Bold;
                        EditorUtility.SetDirty(tmp);
                    }
                }

                Text[] txts = unlocked.GetComponentsInChildren<Text>(true);
                if (txts.Length == 0) txts = card.GetComponentsInChildren<Text>(true);
                foreach (var txt in txts)
                {
                    if (txt == null) continue;
                    if (string.IsNullOrEmpty(txt.text) || txt.text.Contains("UNIT 6") || txt.text.Contains("MAGIC") || txt.text.Contains("Section"))
                    {
                        txt.text = activityTitles[i];
                        txt.fontStyle = FontStyle.Bold;
                        EditorUtility.SetDirty(txt);
                    }
                }

                // Wire Button on Unlocked / Card to Unit 7 Flow Manager on Unit 7 parent
                Button btn = unlocked.GetComponent<Button>() ?? unlocked.GetComponentInChildren<Button>(true)
                          ?? card.GetComponent<Button>() ?? card.GetComponentInChildren<Button>(true);

                if (btn != null && flow != null)
                {
                    // Clear previous persistent listeners (which pointed to Unit 6)
                    while (btn.onClick.GetPersistentEventCount() > 0)
                    {
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(btn.onClick, 0);
                    }

                    // Add persistent listener pointing to Unit 7 FlowManager method on Unit 7 GameObject
                    var method = typeof(U7_SA_UnitFlowManager_Masters_Phonics).GetMethod(methodNames[i]);
                    if (method != null)
                    {
                        var action = (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), flow, method);
                        UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
                    }

                    // Runtime dynamic listener fallback
                    int actIdx = i + 1;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => {
                        U7_SA_AudioManager_Masters_Phonics.Instance?.PlaySFX("click");
                        flow.OpenSection(actIdx);
                    });

                    EditorUtility.SetDirty(btn);
                }
            }

            // Wire Signboard Back Button to Return to Main Lessons Menu
            Transform backBtnT = selPanels.Find("Back_Button") ?? selPanels.Find("BackButton") ?? selPanels.Find("TopBar/Back_Button") ?? selPanels.Find("Back");
            if (backBtnT != null)
            {
                Button backBtn = backBtnT.GetComponent<Button>();
                if (backBtn != null && flow != null)
                {
                    while (backBtn.onClick.GetPersistentEventCount() > 0)
                    {
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(backBtn.onClick, 0);
                    }
                    var backMethod = typeof(U7_SA_UnitFlowManager_Masters_Phonics).GetMethod("BackToLessonsMenu");
                    if (backMethod != null)
                    {
                        var backAction = (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), flow, backMethod);
                        UnityEditor.Events.UnityEventTools.AddPersistentListener(backBtn.onClick, backAction);
                    }
                    backBtn.onClick.RemoveAllListeners();
                    backBtn.onClick.AddListener(flow.BackToLessonsMenu);
                    EditorUtility.SetDirty(backBtn);
                }
            }

            EditorUtility.SetDirty(selPanels.gameObject);
        }

        // =========================================================================
        // UI Helpers
        // =========================================================================

        private static Sprite proceduralRoundedSprite = null;

        public static Sprite GetOrCreateRoundedSprite(int size = 128, int radius = 28)
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

            Vector4 border = new Vector4(radius, radius, radius, radius);
            proceduralRoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            return proceduralRoundedSprite;
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

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return tmp;
        }

        private static void CreateHeader(Transform parent, string title, string subtitle, out TextMeshProUGUI titleOut, out TextMeshProUGUI subOut)
        {
            GameObject headerGo = CreateContainer(parent, "HeaderRibbon", new Vector2(0f, 440f), new Vector2(1600f, 100f));
            titleOut = CreateText(headerGo.transform, "TitleText", $"<b>{title}</b>", 36);
            titleOut.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 18f);

            subOut = CreateText(headerGo.transform, "SubtitleText", $"<color=#CBD5E1>{subtitle}</color>", 22);
            subOut.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -22f);
        }

        private static Button CreateReplayButton(Transform parent, Vector2 pos)
        {
            Sprite speakerSpr = LoadSpriteMatching("speaker audio") 
                             ?? LoadSpriteMatching("speaker")
                             ?? LoadSpriteMatching("audio");

            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            Button templateBtn = null;
            if (canvas != null)
            {
                var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var b in buttons)
                {
                    if (b == null || b.transform.IsChildOf(parent)) continue;
                    string bName = b.gameObject.name.ToLower();
                    if (bName.Contains("speaker") || bName.Contains("replayaudio") || bName.Contains("audiobutton") || bName.Contains("btn_audio"))
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
                if (speakerSpr != null)
                {
                    img.sprite = speakerSpr;
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
            img.sprite = GetOrCreateRoundedSprite();
            img.type = Image.Type.Sliced;
            img.color = new Color(0.08f, 0.62f, 0.98f, 1f);
            Button btn = btnGo.AddComponent<Button>();
            CreateText(btnGo.transform, "Text", $"<b>{label}</b>", 28);
            return btn;
        }

        private static void CreateProgressHUD(Transform parent, out Slider pBar, out TextMeshProUGUI pText)
        {
            GameObject hudGo = CreateContainer(parent, "ProgressHUD", new Vector2(0f, 370f), new Vector2(900f, 60f));

            // 1. Try to find and clone working ProgressBar from other units (Unit 1..6)
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
                if (templateSlider == null)
                {
                    foreach (var s in sliders)
                    {
                        if (s != null && s.fillRect != null && !s.transform.IsChildOf(parent))
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
                // Build full working 3-layer Slider hierarchy
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
                bgImg.color = new Color(0.06f, 0.09f, 0.16f, 1f); // #0F172A

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
                fillImg.color = new Color(0.08f, 0.62f, 0.98f, 1f); // #149EFA

                pBar = barObj.GetComponent<Slider>();
                pBar.fillRect = fillRt;
                pBar.targetGraphic = fillImg;
                pBar.direction = Slider.Direction.LeftToRight;
                pBar.minValue = 0f;
                pBar.maxValue = 10f;
                pBar.value = 0f;
            }

            U7_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(pBar);

            pText = CreateText(hudGo.transform, "ProgressText", "<b>Item 1 of 10</b>", 22);
            pText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 20f);
            pText.color = Color.white;
        }

        private static void CreateScoreHUD(Transform parent, out TextMeshProUGUI scoreTMP, out TextMeshProUGUI streakTMP)
        {
            GameObject hudGo = CreateContainer(parent, "ScoreHUD", new Vector2(-650f, 440f), new Vector2(300f, 60f));
            scoreTMP = CreateText(hudGo.transform, "ScoreText", "Score: <b>0</b>", 24);
            streakTMP = CreateText(hudGo.transform, "StreakText", "", 20);
        }

        private static void SetProp(SerializedObject so, string propName, Object value)
        {
            SerializedProperty p = so.FindProperty(propName);
            if (p != null) p.objectReferenceValue = value;
        }

        // =========================================================================
        // Resource Resolvers & Caching
        // =========================================================================

        private static Dictionary<string, Sprite> spriteCache;
        private static Dictionary<string, AudioClip> audioCache;
        private static List<string> audioList;

        public static void ClearCaches()
        {
            spriteCache = null;
            audioCache = null;
            audioList = null;
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

        // =========================================================================
        // 1-Click Auto-Assigners for Inspector & Menu
        // =========================================================================

        [MenuItem("Masters Phonics/Unit 7/Auto-Assign All Assets & References", false, 101)]
        public static void AutoAssignAllUnit7()
        {
            ClearCaches();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in Scene!", "OK");
                return;
            }

            Transform u7 = canvas.transform.Find("Unit_7") ?? canvas.transform.Find("Unit7");
            if (u7 == null)
            {
                var mgr = Object.FindFirstObjectByType<U7_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include);
                if (mgr != null) u7 = mgr.transform;
            }

            if (u7 == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find Unit_7 in Scene. Run 'Generate Unit 7 Hierarchy' first!", "OK");
                return;
            }

            var flow = u7.GetComponentInChildren<U7_SA_UnitFlowManager_Masters_Phonics>(true);
            if (flow != null) AutoAssignFlowManager(flow);

            var audioMgr = u7.GetComponentInChildren<U7_SA_AudioManager_Masters_Phonics>(true);
            if (audioMgr != null) AutoAssignAudioManager(audioMgr);

            var gm01 = u7.GetComponentInChildren<U7_SA_GM01_ConceptCards_Masters_Phonics>(true);
            if (gm01 != null) AutoAssignConceptCards(gm01);

            var gm04 = u7.GetComponentInChildren<U7_SA_GM04_TeamUp_Masters_Phonics>(true);
            if (gm04 != null) AutoAssignTeamUp(gm04);

            var gm03 = u7.GetComponentInChildren<U7_SA_GM03_WordFamilies_Masters_Phonics>(true);
            if (gm03 != null) AutoAssignWordFamilies(gm03);

            var gm03s = u7.GetComponentInChildren<U7_SA_GM03_SameSound_Masters_Phonics>(true);
            if (gm03s != null) AutoAssignSameSound(gm03s);

            var gm04g = u7.GetComponentInChildren<U7_SA_GM04_GlideOrHold_Masters_Phonics>(true);
            if (gm04g != null) AutoAssignGlideOrHold(gm04g);

            var gm03g = u7.GetComponentInChildren<U7_SA_GM03s_GlideFamilies_Masters_Phonics>(true);
            if (gm03g != null) AutoAssignGlideFamilies(gm03g);

            var gm08 = u7.GetComponentInChildren<U7_SA_GM08_TeamRush_Masters_Phonics>(true);
            if (gm08 != null) AutoAssignTeamRush(gm08);

            var map = u7.GetComponentInChildren<U7_SA_SevenTypesMap_Masters_Phonics>(true);
            if (map != null) AutoAssignSevenTypesMap(map);

            var challenge = u7.GetComponentInChildren<U7_SA_UnitChallenge_Masters_Phonics>(true);
            if (challenge != null) AutoAssignUnitChallenge(challenge);

            EditorUtility.SetDirty(u7.gameObject);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(u7.gameObject.scene);

            EditorUtility.DisplayDialog("Unit 7 Assets Auto-Assigned!", 
                "Successfully resolved and auto-assigned all audio clips, sprites, and UI elements across all Unit 7 panels!", "Great!");
        }

        public static void AutoAssignAudioManager(U7_SA_AudioManager_Masters_Phonics audioMgr)
        {
            if (audioMgr == null) return;
            ClearCaches();
            Undo.RecordObject(audioMgr, "Auto-Assign Audio Manager");
            AssignRootAudioClips(audioMgr, audioMgr.GetComponent<U7_SA_UnitFlowManager_Masters_Phonics>() ?? Object.FindFirstObjectByType<U7_SA_UnitFlowManager_Masters_Phonics>(FindObjectsInactive.Include));
            EditorUtility.SetDirty(audioMgr);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(audioMgr.gameObject.scene);
        }

        public static void AutoAssignFlowManager(U7_SA_UnitFlowManager_Masters_Phonics flowMgr)
        {
            if (flowMgr == null) return;
            ClearCaches();
            Undo.RecordObject(flowMgr, "Auto-Assign Flow Manager");
            Transform root = flowMgr.transform;
            Transform selPanels = root.Find("Unit_7_Section_Selection_Panels") ?? root.Find("Section_Selection_Panels") ?? root.Find("Unit_6_Section_Selection_Panels");
            Transform secParent = root.Find("Unit_7_Sections") ?? root.Find("Sections") ?? root.Find("Unit_6_Sections");

            GameObject learn = secParent != null ? secParent.Find("U7_learn_Concept_Cards_Panel")?.gameObject : null;
            GameObject act1 = secParent != null ? secParent.Find("U7_Activity_1_Team_Up")?.gameObject : null;
            GameObject act2 = secParent != null ? secParent.Find("U7_Activity_2_Word_Families")?.gameObject : null;
            GameObject act3 = secParent != null ? secParent.Find("U7_Activity_3_Same_Sound")?.gameObject : null;
            GameObject act4 = secParent != null ? secParent.Find("U7_Activity_4_Glide_Or_Hold")?.gameObject : null;
            GameObject act5 = secParent != null ? secParent.Find("U7_Activity_5_Glide_Families")?.gameObject : null;
            GameObject act6 = secParent != null ? secParent.Find("U7_Activity_6_Team_Rush")?.gameObject : null;
            GameObject map = secParent != null ? secParent.Find("U7_SevenTypesMap_Panel")?.gameObject : null;
            GameObject challenge = secParent != null ? secParent.Find("U7_UnitChallenge_Panel")?.gameObject : null;
            GameObject comp = secParent != null ? secParent.Find("U7_COMPLETE_Panel")?.gameObject : null;

            flowMgr.unitIntroClip = LoadAudioMatching("Unit Seven is the big one Two ideas") ?? LoadAudioMatching("Two ideas");
            flowMgr.unitCompleteVoiceClip = LoadAudioMatching("Unit Seven done the longest one in the") ?? LoadAudioMatching("longest one in the");

            WireFlowManager(flowMgr, selPanels, secParent, learn, act1, act2, act3, act4, act5, act6, map, challenge, comp);
            EditorUtility.SetDirty(flowMgr);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(flowMgr.gameObject.scene);
        }

        public static void AutoAssignConceptCards(U7_SA_GM01_ConceptCards_Masters_Phonics gm)
        {
            if (gm == null) return;
            ClearCaches();
            Undo.RecordObject(gm, "Auto-Assign Concept Cards");
            gm.card1IntroClip = LoadAudioMatching("A vowel team is two vowel letters sitting") ?? LoadAudioMatching("vowel team is two");
            gm.card2IntroClip = LoadAudioMatching("Heres the annoying part One sound can be") ?? LoadAudioMatching("annoying part");
            gm.card3IntroClip = LoadAudioMatching("Same two letters Different sound English does this") ?? LoadAudioMatching("Same two letters");
            gm.card4IntroClip = LoadAudioMatching("Coooiiin Hear that It started in one place") ?? LoadAudioMatching("Coooiiin") ?? LoadAudioMatching("Eight gliding sounds");
            gm.card5IntroClip = LoadAudioMatching("Careful here these two things arent opposites Vowel") ?? LoadAudioMatching("Careful here");

            SerializedObject so = new SerializedObject(gm);
            so.Update();
            Transform cCont = gm.transform.Find("CardContainer") ?? gm.transform.Find("Cards_Container") ?? gm.transform.Find("CardsContainer");
            SetProp(so, "cardsContainer", cCont);
            SetProp(so, "nextCardBtn", (gm.transform.Find("NextButton") ?? gm.transform.Find("NextCardButton") ?? gm.transform.Find("NextBtn"))?.GetComponent<Button>());
            SetProp(so, "prevCardBtn", (gm.transform.Find("PrevButton") ?? gm.transform.Find("PrevCardButton") ?? gm.transform.Find("PrevBtn"))?.GetComponent<Button>());
            SetProp(so, "cardCounterText", (gm.transform.Find("ProgressHUD/ProgressText") ?? gm.transform.Find("ProgressHUD/Progress_Text") ?? gm.transform.Find("ProgressText"))?.GetComponent<TextMeshProUGUI>());
            SetProp(so, "card1IntroClip", gm.card1IntroClip);
            SetProp(so, "card2IntroClip", gm.card2IntroClip);
            SetProp(so, "card3IntroClip", gm.card3IntroClip);
            SetProp(so, "card4IntroClip", gm.card4IntroClip);
            SetProp(so, "card5IntroClip", gm.card5IntroClip);

            SerializedProperty cpProp = so.FindProperty("cardPanels");
            if (cpProp != null)
            {
                List<GameObject> foundPanels = new List<GameObject>();
                for (int i = 1; i <= 5; i++)
                {
                    Transform p = cCont != null ? (cCont.Find($"Card_{i}") ?? cCont.Find($"Card{i}") ?? cCont.Find($"Card {i}") ?? cCont.Find($"Card_{i}_Panel") ?? cCont.Find($"CardPanel_{i}")) : null;
                    if (p == null) p = gm.transform.Find($"Card_{i}") ?? gm.transform.Find($"Card{i}") ?? gm.transform.Find($"Card {i}") ?? gm.transform.Find($"Card_{i}_Panel") ?? gm.transform.Find($"CardPanel_{i}");
                    if (p != null) foundPanels.Add(p.gameObject);
                }
                if (foundPanels.Count == 5)
                {
                    cpProp.arraySize = 5;
                    for (int i = 0; i < 5; i++)
                    {
                        cpProp.GetArrayElementAtIndex(i).objectReferenceValue = foundPanels[i];
                    }
                }
            }

            so.ApplyModifiedProperties();
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gm.gameObject.scene);
        }

        public static void AutoAssignTeamUp(U7_SA_GM04_TeamUp_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Team Up");
            gm.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX");
            gm.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX");

            if (gm.teamUpItems == null || gm.teamUpItems.Count == 0)
            {
                gm.teamUpItems = U7_SA_DataTypes_Masters_Phonics.GetDefaultActivity1Items();
            }

            foreach (var it in gm.teamUpItems)
            {
                if (it == null) continue;
                if (it.pictureSprite == null) it.pictureSprite = LoadSpriteMatching($"U07_IMG_{it.fullWord}") ?? LoadSpriteMatching(it.fullWord);
                if (it.wordAudio == null) it.wordAudio = LoadAudioMatching(it.fullWord, "voice B");
            }

            Transform tCont = gm.transform.Find("TileOptionsContainer") 
                           ?? gm.transform.Find("TilesContainer") 
                           ?? gm.transform.Find("ChoiceButtonsContainer") 
                           ?? gm.transform.Find("ButtonsContainer");
            if (tCont != null)
            {
                Button[] btns = tCont.GetComponentsInChildren<Button>(true);
                foreach (var b in btns)
                {
                    if (b == null) continue;
                    Image img = b.GetComponent<Image>();
                    if (img != null)
                    {
                        img.color = Color.white;
                        EditorUtility.SetDirty(img);
                    }
                    TextMeshProUGUI tmp = b.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (tmp != null)
                    {
                        tmp.color = new Color(0.06f, 0.09f, 0.16f, 1f);
                        EditorUtility.SetDirty(tmp);
                    }
                }
            }

            SerializedObject so = new SerializedObject(gm);
            SetProp(so, "correctSFX", gm.correctSFX);
            SetProp(so, "wrongSFX", gm.wrongSFX);

            Transform picTr = gm.transform.Find("PictureWordCard/PictureDisplay") ?? gm.transform.Find("PictureWordCard/PictureImage");
            if (picTr != null) SetProp(so, "illustrationImage", picTr.GetComponent<Image>());

            Transform wordTr = gm.transform.Find("PictureWordCard/WordDisplayRow/GappedWordText") ?? gm.transform.Find("PictureWordCard/GappedWordText");
            if (wordTr != null) SetProp(so, "gappedWordText", wordTr.GetComponent<TextMeshProUGUI>());

            if (tCont != null) SetProp(so, "tilesContainer", tCont);

            Transform replayTr = gm.transform.Find("ReplayAudioBtn") ?? gm.transform.Find("ReplayButton");
            if (replayTr != null) SetProp(so, "replayAudioBtn", replayTr.GetComponent<Button>());

            Transform pText = gm.transform.Find("ProgressHUD/Progress_Text") ?? gm.transform.Find("ProgressHUD/ProgressText");
            if (pText != null) SetProp(so, "progressText", pText.GetComponent<TextMeshProUGUI>());

            Transform sText = gm.transform.Find("HUD_Container/Score_Text") ?? gm.transform.Find("ProgressHUD/Score_Text");
            if (sText != null) SetProp(so, "scoreText", sText.GetComponent<TextMeshProUGUI>());

            Slider pBar = gm.GetComponentInChildren<Slider>(true);
            if (pBar != null) SetProp(so, "progressBar", pBar);

            so.ApplyModifiedProperties();
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        public static void AutoAssignWordFamilies(U7_SA_GM03_WordFamilies_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Word Families");
            gm.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX");
            gm.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX");

            if (gm.rounds == null || gm.rounds.Count == 0)
            {
                gm.rounds = U7_SA_DataTypes_Masters_Phonics.GetDefaultActivity2Rounds();
            }

            foreach (var r in gm.rounds)
            {
                if (r == null || r.items == null) continue;
                foreach (var it in r.items)
                {
                    if (it == null) continue;
                    if (it.wordAudio == null) it.wordAudio = LoadAudioMatching(it.word, "voice B");
                }
            }

            SerializedObject so = new SerializedObject(gm);
            SetProp(so, "correctSFX", gm.correctSFX);
            SetProp(so, "wrongSFX", gm.wrongSFX);

            Transform cardTr = gm.transform.Find("CurrentWordCard") ?? gm.transform.Find("CardContainer") ?? gm.transform.Find("WordCardContainer");
            if (cardTr != null) SetProp(so, "cardContainer", cardTr.GetComponent<RectTransform>());

            Transform binsGroup = gm.transform.Find("BinsContainer") ?? gm.transform.Find("FourBinsGroup");
            if (binsGroup != null)
            {
                SetProp(so, "bin1Transform", (binsGroup.Find("Bin_1") ?? binsGroup.Find("Bin1"))?.GetComponent<RectTransform>());
                SetProp(so, "bin2Transform", (binsGroup.Find("Bin_2") ?? binsGroup.Find("Bin2"))?.GetComponent<RectTransform>());
                SetProp(so, "bin3Transform", (binsGroup.Find("Bin_3") ?? binsGroup.Find("Bin3"))?.GetComponent<RectTransform>());
                SetProp(so, "bin4Transform", (binsGroup.Find("Bin_4") ?? binsGroup.Find("Bin4"))?.GetComponent<RectTransform>());
            }

            Transform replayTr = gm.transform.Find("ReplayAudioBtn") ?? gm.transform.Find("ReplayButton") ?? gm.transform.Find("ReplayAudioButton") ?? gm.transform.Find("Header_Container/ReplayBtn") ?? gm.transform.Find("Speaker_Button");
            if (replayTr != null) SetProp(so, "replayAudioButton", replayTr.GetComponent<Button>());

            Transform pText = gm.transform.Find("ProgressHUD/ProgressText") ?? gm.transform.Find("ProgressHUD/Progress_Text") ?? gm.transform.Find("ProgressHUD/ItemCounterText") ?? gm.transform.Find("ItemCounterText") ?? gm.transform.Find("ProgressText") ?? gm.transform.Find("Progress_Text");
            if (pText != null) SetProp(so, "itemCounterText", pText.GetComponent<TextMeshProUGUI>());

            Transform sText = gm.transform.Find("ScoreHUD/ScoreText") ?? gm.transform.Find("ScoreHUD/Score_Text") ?? gm.transform.Find("HUD_Container/Score_Text") ?? gm.transform.Find("ProgressHUD/Score_Text") ?? gm.transform.Find("Score_HUD/Score_Text") ?? gm.transform.Find("ScoreText");
            if (sText != null) SetProp(so, "scoreText", sText.GetComponent<TextMeshProUGUI>());

            Slider pBar = gm.GetComponentInChildren<Slider>(true);
            if (pBar != null) SetProp(so, "progressBar", pBar);

            so.ApplyModifiedProperties();
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        public static void AutoAssignSameSound(U7_SA_GM03_SameSound_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Same Sound");
            gm.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX");
            gm.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX");

            if (gm.sessionItems == null || gm.sessionItems.Count == 0)
            {
                gm.sessionItems = U7_SA_DataTypes_Masters_Phonics.GetDefaultActivity3Items();
            }

            foreach (var it in gm.sessionItems)
            {
                if (it == null) continue;
                if (it.wordAudio == null) it.wordAudio = LoadAudioMatching(it.word, "voice B");
            }

            SerializedObject so = new SerializedObject(gm);
            SetProp(so, "correctSFX", gm.correctSFX);
            SetProp(so, "wrongSFX", gm.wrongSFX);

            Transform cardTr = gm.transform.Find("AuditoryWordCard") ?? gm.transform.Find("CardContainer") ?? gm.transform.Find("WordCardContainer");
            if (cardTr != null) SetProp(so, "cardContainer", cardTr.GetComponent<RectTransform>());

            Transform binsGroup = gm.transform.Find("SoundBinsContainer") ?? gm.transform.Find("BinsContainer");
            if (binsGroup != null)
            {
                SetProp(so, "binAYTransform", (binsGroup.Find("SoundBin_1") ?? binsGroup.Find("BinAY"))?.GetComponent<RectTransform>());
                SetProp(so, "binEETransform", (binsGroup.Find("SoundBin_2") ?? binsGroup.Find("BinEE"))?.GetComponent<RectTransform>());
                SetProp(so, "binOHTransform", (binsGroup.Find("SoundBin_3") ?? binsGroup.Find("BinOH"))?.GetComponent<RectTransform>());
            }

            Transform replayTr = gm.transform.Find("ReplayAudioBtn") ?? gm.transform.Find("ReplayButton") ?? gm.transform.Find("ReplayAudioButton") ?? gm.transform.Find("Header_Container/ReplayBtn") ?? gm.transform.Find("Speaker_Button");
            if (replayTr != null) SetProp(so, "replayAudioButton", replayTr.GetComponent<Button>());

            Transform pText = gm.transform.Find("ProgressHUD/ProgressText") ?? gm.transform.Find("ProgressHUD/Progress_Text") ?? gm.transform.Find("ProgressHUD/ItemCounterText") ?? gm.transform.Find("ItemCounterText") ?? gm.transform.Find("ProgressText") ?? gm.transform.Find("Progress_Text");
            if (pText != null) SetProp(so, "itemCounterText", pText.GetComponent<TextMeshProUGUI>());

            Transform sText = gm.transform.Find("ScoreHUD/ScoreText") ?? gm.transform.Find("ScoreHUD/Score_Text") ?? gm.transform.Find("HUD_Container/Score_Text") ?? gm.transform.Find("ProgressHUD/Score_Text") ?? gm.transform.Find("Score_HUD/Score_Text") ?? gm.transform.Find("ScoreText");
            if (sText != null) SetProp(so, "scoreText", sText.GetComponent<TextMeshProUGUI>());

            Slider pBar = gm.GetComponentInChildren<Slider>(true);
            if (pBar != null) SetProp(so, "progressBar", pBar);

            so.ApplyModifiedProperties();
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        public static void AutoAssignGlideOrHold(U7_SA_GM04_GlideOrHold_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Glide Or Hold");
            gm.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX");
            gm.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX");
            gm.waveGlideSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_vowel_stretch.wav") ?? LoadAudioMatching("vowel_stretch", "SFX");
            gm.waveHoldSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U05_SFX_vowel_snap.wav") ?? LoadAudioMatching("vowel_snap", "SFX");

            if (gm.glideOrHoldItems == null || gm.glideOrHoldItems.Count == 0)
            {
                gm.glideOrHoldItems = U7_SA_DataTypes_Masters_Phonics.GetDefaultActivity4Items();
            }

            foreach (var it in gm.glideOrHoldItems)
            {
                if (it == null) continue;
                if (it.naturalAudio == null) it.naturalAudio = LoadAudioMatching(it.word, "voice B");
                if (it.stretchedAudio == null) it.stretchedAudio = LoadAudioMatching($"U07_WRD_{it.word}_stretch", "voice B") ?? it.naturalAudio;
            }

            SerializedObject so = new SerializedObject(gm);
            SetProp(so, "correctSFX", gm.correctSFX);
            SetProp(so, "wrongSFX", gm.wrongSFX);
            SetProp(so, "waveGlideSFX", gm.waveGlideSFX);
            SetProp(so, "waveHoldSFX", gm.waveHoldSFX);

            Sprite gBin = LoadSpriteMatching("U07_UI_Bin_Glide");
            Sprite hBin = LoadSpriteMatching("U07_UI_Bin_Hold");
            Sprite gWave = LoadSpriteMatching("U07_UI_Wave_Glide");
            Sprite hWave = LoadSpriteMatching("U07_UI_Wave_Hold");
            if (gBin != null) SetProp(so, "glideBinSprite", gBin);
            if (hBin != null) SetProp(so, "holdBinSprite", hBin);
            if (gWave != null) SetProp(so, "glideWaveSprite", gWave);
            if (hWave != null) SetProp(so, "holdWaveSprite", hWave);

            Transform buttonsGroup = gm.transform.Find("ChoiceButtons") ?? gm.transform.Find("ChoiceButtonsContainer");
            if (buttonsGroup != null)
            {
                SetProp(so, "glideButton", (buttonsGroup.Find("GlideChoiceBtn") ?? buttonsGroup.Find("GlideButton"))?.GetComponent<Button>());
                SetProp(so, "holdButton", (buttonsGroup.Find("HoldChoiceBtn") ?? buttonsGroup.Find("HoldButton"))?.GetComponent<Button>());
            }

            Transform gbTr = gm.transform.Find("GlideBin") ?? gm.transform.Find("Bin_Glide") ?? (buttonsGroup != null ? buttonsGroup.Find("GlideBin") : null);
            if (gbTr != null) SetProp(so, "glideBinGraphicButton", gbTr.GetComponent<Button>());

            Transform hbTr = gm.transform.Find("HoldBin") ?? gm.transform.Find("Bin_Hold") ?? (buttonsGroup != null ? buttonsGroup.Find("HoldBin") : null);
            if (hbTr != null) SetProp(so, "holdBinGraphicButton", hbTr.GetComponent<Button>());

            Transform waveTr = gm.transform.Find("WaveformVisual") ?? gm.transform.Find("CardContainer/WaveformImage");
            if (waveTr != null) SetProp(so, "waveformImage", waveTr.GetComponent<Image>());

            Transform wText = gm.transform.Find("WaveformVisual/WaveformText") 
                           ?? gm.transform.Find("WaveformVisual/WordText")
                           ?? gm.transform.Find("CardContainer/WordText")
                           ?? gm.transform.Find("WordPromptText");
            if (wText != null) SetProp(so, "wordPromptText", wText.GetComponent<TextMeshProUGUI>());

            Transform replayTr = gm.transform.Find("PlayNaturalAudioBtn") ?? gm.transform.Find("ReplayAudioBtn") ?? gm.transform.Find("ReplayButton") ?? gm.transform.Find("Speaker_Button");
            if (replayTr != null) SetProp(so, "playNaturalAudioBtn", replayTr.GetComponent<Button>());

            Transform pText = gm.transform.Find("ProgressHUD/ProgressText") ?? gm.transform.Find("ProgressHUD/Progress_Text") ?? gm.transform.Find("ProgressHUD/ItemCounterText") ?? gm.transform.Find("ProgressText") ?? gm.transform.Find("Progress_Text");
            if (pText != null) SetProp(so, "progressText", pText.GetComponent<TextMeshProUGUI>());

            Transform sText = gm.transform.Find("ScoreHUD/ScoreText") ?? gm.transform.Find("ScoreHUD/Score_Text") ?? gm.transform.Find("HUD_Container/Score_Text") ?? gm.transform.Find("ProgressHUD/Score_Text") ?? gm.transform.Find("Score_HUD/Score_Text") ?? gm.transform.Find("ScoreText");
            if (sText != null) SetProp(so, "scoreText", sText.GetComponent<TextMeshProUGUI>());

            Slider pBar = gm.GetComponentInChildren<Slider>(true);
            if (pBar != null) SetProp(so, "progressBar", pBar);

            so.ApplyModifiedProperties();
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        public static void AutoAssignGlideFamilies(U7_SA_GM03s_GlideFamilies_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Glide Families");
            gm.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX");
            gm.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX");

            if (gm.roundAItems == null || gm.roundAItems.Count == 0)
                gm.roundAItems = U7_SA_DataTypes_Masters_Phonics.GetDefaultActivity5ItemsRoundA();
            if (gm.roundBItems == null || gm.roundBItems.Count == 0)
                gm.roundBItems = U7_SA_DataTypes_Masters_Phonics.GetDefaultActivity5ItemsRoundB();

            foreach (var it in gm.roundAItems)
            {
                if (it != null && it.wordAudio == null) it.wordAudio = LoadAudioMatching(it.word, "voice B");
            }
            foreach (var it in gm.roundBItems)
            {
                if (it != null && it.wordAudio == null) it.wordAudio = LoadAudioMatching(it.word, "voice B");
            }

            SerializedObject so = new SerializedObject(gm);
            SetProp(so, "correctSFX", gm.correctSFX);
            SetProp(so, "wrongSFX", gm.wrongSFX);

            Transform midTr = gm.transform.Find("MiddleLeftBin") ?? gm.transform.Find("LeftBin") ?? gm.transform.Find("MiddleBin");
            if (midTr != null) SetProp(so, "middleLeftBin", midTr.GetComponent<RectTransform>());

            Transform endTr = gm.transform.Find("EndRightBin") ?? gm.transform.Find("RightBin") ?? gm.transform.Find("EndBin");
            if (endTr != null) SetProp(so, "endRightBin", endTr.GetComponent<RectTransform>());

            Transform cardTr = gm.transform.Find("CardContainer") ?? gm.transform.Find("SwipeCardContainer") ?? gm.transform.Find("SwipeCard") ?? gm.transform.Find("Card");
            if (cardTr != null) SetProp(so, "cardContainer", cardTr.GetComponent<RectTransform>());

            Transform replayTr = gm.transform.Find("ReplayAudioBtn") ?? gm.transform.Find("ReplayButton") ?? gm.transform.Find("ReplayAudioButton") ?? gm.transform.Find("Header_Container/ReplayBtn") ?? gm.transform.Find("HeaderRibbon/ReplayAudioBtn") ?? gm.transform.Find("Speaker_Button");
            if (replayTr != null) SetProp(so, "replayAudioBtn", replayTr.GetComponent<Button>());

            Slider pBar = gm.GetComponentInChildren<Slider>(true);
            if (pBar != null) SetProp(so, "progressBar", pBar);

            Transform pText = gm.transform.Find("ProgressHUD/ProgressText") ?? gm.transform.Find("ProgressHUD/Progress_Text") ?? gm.transform.Find("ProgressHUD/ItemCounterText") ?? gm.transform.Find("ProgressText") ?? gm.transform.Find("Progress_Text") ?? gm.transform.Find("ItemCounterText");
            if (pText != null) SetProp(so, "progressText", pText.GetComponent<TextMeshProUGUI>());

            Transform sText = gm.transform.Find("ScoreHUD/ScoreText") ?? gm.transform.Find("ScoreHUD/Score_Text") ?? gm.transform.Find("HUD_Container/Score_Text") ?? gm.transform.Find("ProgressHUD/Score_Text") ?? gm.transform.Find("Score_HUD/Score_Text") ?? gm.transform.Find("ScoreText") ?? gm.transform.Find("Score_Text");
            if (sText != null) SetProp(so, "scoreText", sText.GetComponent<TextMeshProUGUI>());

            Transform stText = gm.transform.Find("ScoreHUD/StreakText") ?? gm.transform.Find("ScoreHUD/Streak_Text") ?? gm.transform.Find("HUD_Container/Streak_Text") ?? gm.transform.Find("Score_HUD/Streak_Text") ?? gm.transform.Find("StreakText") ?? gm.transform.Find("Streak_Text");
            if (stText != null) SetProp(so, "streakText", stText.GetComponent<TextMeshProUGUI>());

            so.ApplyModifiedProperties();
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        public static void AutoAssignTeamRush(U7_SA_GM08_TeamRush_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Team Rush");
            gm.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX");
            gm.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX");

            if (gm.wordPool == null || gm.wordPool.Count == 0)
            {
                gm.wordPool = new List<TeamRushItem>
                {
                    new TeamRushItem("rain", SoundCategoryType.LongA_AY),
                    new TeamRushItem("day", SoundCategoryType.LongA_AY),
                    new TeamRushItem("eight", SoundCategoryType.LongA_AY),
                    new TeamRushItem("paint", SoundCategoryType.LongA_AY),
                    new TeamRushItem("see", SoundCategoryType.LongE_EE),
                    new TeamRushItem("team", SoundCategoryType.LongE_EE),
                    new TeamRushItem("queen", SoundCategoryType.LongE_EE),
                    new TeamRushItem("key", SoundCategoryType.LongE_EE),
                    new TeamRushItem("boat", SoundCategoryType.LongO_OH),
                    new TeamRushItem("road", SoundCategoryType.LongO_OH),
                    new TeamRushItem("snow", SoundCategoryType.LongO_OH),
                    new TeamRushItem("toe", SoundCategoryType.LongO_OH),
                    new TeamRushItem("coin", SoundCategoryType.Diphthong_OY),
                    new TeamRushItem("boy", SoundCategoryType.Diphthong_OY),
                    new TeamRushItem("cloud", SoundCategoryType.Diphthong_OW),
                    new TeamRushItem("town", SoundCategoryType.Diphthong_OW),
                    new TeamRushItem("train", SoundCategoryType.LongA_AY),
                    new TeamRushItem("green", SoundCategoryType.LongE_EE),
                    new TeamRushItem("soap", SoundCategoryType.LongO_OH),
                    new TeamRushItem("toy", SoundCategoryType.Diphthong_OY)
                };
            }

            foreach (var it in gm.wordPool)
            {
                if (it != null && it.wordAudio == null) it.wordAudio = LoadAudioMatching(it.word, "voice B");
            }

            SerializedObject so = new SerializedObject(gm);
            SetProp(so, "correctSFX", gm.correctSFX);
            SetProp(so, "wrongSFX", gm.wrongSFX);

            Transform chutes = gm.transform.Find("Chutes");
            if (chutes != null)
            {
                SetProp(so, "chute1Btn_LongA", (chutes.Find("Chute1_LongA") ?? chutes.Find("Chute1"))?.GetComponentInChildren<Button>(true));
                SetProp(so, "chute2Btn_LongE", (chutes.Find("Chute2_LongE") ?? chutes.Find("Chute2"))?.GetComponentInChildren<Button>(true));
                SetProp(so, "chute3Btn_LongO", (chutes.Find("Chute3_LongO") ?? chutes.Find("Chute3"))?.GetComponentInChildren<Button>(true));
                SetProp(so, "chute4Btn_Diphthong", (chutes.Find("Chute4_Diphthong") ?? chutes.Find("Chute4"))?.GetComponentInChildren<Button>(true));
            }

            Transform cardTr = gm.transform.Find("CurrentCard") ?? gm.transform.Find("CurrentWordCard") ?? gm.transform.Find("Card");
            if (cardTr != null) SetProp(so, "currentCardRect", cardTr.GetComponent<RectTransform>());

            Transform pBar = gm.transform.Find("ProgressHUD/ProgressBar") ?? gm.transform.Find("ProgressBar") ?? gm.transform.Find("TimerBar");
            if (pBar != null) SetProp(so, "timerBar", pBar.GetComponent<Slider>());
            if (pBar == null)
            {
                Slider s = gm.GetComponentInChildren<Slider>(true);
                if (s != null) SetProp(so, "timerBar", s);
            }

            Transform pText = gm.transform.Find("ProgressHUD/ProgressText") ?? gm.transform.Find("ProgressHUD/Progress_Text") ?? gm.transform.Find("ProgressText") ?? gm.transform.Find("TimerText");
            if (pText != null) SetProp(so, "timerText", pText.GetComponent<TextMeshProUGUI>());

            Transform sText = gm.transform.Find("ScoreHUD/ScoreText") ?? gm.transform.Find("ScoreHUD/Score_Text") ?? gm.transform.Find("Score_HUD/Score_Text") ?? gm.transform.Find("Score_HUD/ScoreText") ?? gm.transform.Find("ScoreText");
            if (sText != null) SetProp(so, "scoreText", sText.GetComponent<TextMeshProUGUI>());

            Transform cText = gm.transform.Find("ScoreHUD/StreakText") ?? gm.transform.Find("ScoreHUD/Streak_Text") ?? gm.transform.Find("Score_HUD/Streak_Text") ?? gm.transform.Find("Score_HUD/StreakText") ?? gm.transform.Find("StreakText") ?? gm.transform.Find("ComboText");
            if (cText != null) SetProp(so, "comboText", cText.GetComponent<TextMeshProUGUI>());

            so.ApplyModifiedProperties();
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        public static void AutoAssignSevenTypesMap(U7_SA_SevenTypesMap_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Seven Types Map");
            gm.mapIntroClip = LoadAudioMatching("Seven Syllable Types", "voice A") ?? LoadAudioMatching("U07_VO_map_unit7", "voice A");

            SerializedObject so = new SerializedObject(gm);
            SetProp(so, "mapIntroClip", gm.mapIntroClip);
            Transform tilesTr = gm.transform.Find("Tiles_Container") ?? gm.transform.Find("TilesContainer");
            if (tilesTr != null) SetProp(so, "tilesContainer", tilesTr);
            Transform contTr = gm.transform.Find("ContinueButton") ?? gm.transform.Find("NextButton");
            if (contTr != null) SetProp(so, "continueBtn", contTr.GetComponent<Button>());
            Transform replayTr = gm.transform.Find("ReplayAudioBtn") ?? gm.transform.Find("ReplayButton") ?? gm.transform.Find("ReplayAudioButton") ?? gm.transform.Find("Header_Container/ReplayBtn") ?? gm.transform.Find("Speaker_Button");
            if (replayTr != null) SetProp(so, "replayAudioButton", replayTr.GetComponent<Button>());

            so.ApplyModifiedProperties();
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }

        public static void AutoAssignUnitChallenge(U7_SA_UnitChallenge_Masters_Phonics gm)
        {
            if (gm == null) return;
            Undo.RecordObject(gm, "Auto-Assign Unit Challenge");
            gm.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX");
            gm.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX");
            gm.unitCompletedFanfare = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/U04_SFX_map_complete.wav") ?? LoadAudioMatching("celebration") ?? LoadAudioMatching("complete");

            if (gm.questions == null || gm.questions.Count == 0)
            {
                gm.questions = U7_SA_DataTypes_Masters_Phonics.GetDefaultChallengeQuestions();
            }

            foreach (var q in gm.questions)
            {
                if (q != null && q.customAudio == null && !string.IsNullOrEmpty(q.audioClipName))
                {
                    q.customAudio = LoadAudioMatching(q.audioClipName, "voice A") ?? LoadAudioMatching(q.audioClipName, "voice B");
                }
            }

            SerializedObject so = new SerializedObject(gm);
            SetProp(so, "correctSFX", gm.correctSFX);
            SetProp(so, "wrongSFX", gm.wrongSFX);
            SetProp(so, "unitCompletedFanfare", gm.unitCompletedFanfare);

            Transform qBox = gm.transform.Find("QuestionBox");
            if (qBox != null) SetProp(so, "questionBoxRect", qBox.GetComponent<RectTransform>());

            Transform optsTr = gm.transform.Find("OptionsContainer");
            if (optsTr != null) SetProp(so, "optionsContainer", optsTr.GetComponent<RectTransform>());

            Transform replayTr = gm.transform.Find("ReplayAudioBtn") ?? gm.transform.Find("ReplayButton") ?? gm.transform.Find("ReplayAudioButton") ?? gm.transform.Find("Header_Container/ReplayBtn") ?? gm.transform.Find("QuestionBox/ReplayAudioBtn") ?? gm.transform.Find("Speaker_Button");
            if (replayTr != null) SetProp(so, "replayBtn", replayTr.GetComponent<Button>());

            Transform pText = gm.transform.Find("ProgressHUD/ProgressText") ?? gm.transform.Find("ProgressHUD/Progress_Text") ?? gm.transform.Find("ProgressHUD/ItemCounterText") ?? gm.transform.Find("ProgressText") ?? gm.transform.Find("QuestionNumberText");
            if (pText != null) SetProp(so, "questionNumberText", pText.GetComponent<TextMeshProUGUI>());

            Transform sText = gm.transform.Find("ScoreHUD/ScoreText") ?? gm.transform.Find("ScoreHUD/Score_Text") ?? gm.transform.Find("Score_HUD/Score_Text") ?? gm.transform.Find("Score_HUD/ScoreText") ?? gm.transform.Find("ScoreText");
            if (sText != null) SetProp(so, "scoreText", sText.GetComponent<TextMeshProUGUI>());

            Transform stText = gm.transform.Find("ScoreHUD/StreakText") ?? gm.transform.Find("ScoreHUD/Streak_Text") ?? gm.transform.Find("Score_HUD/Streak_Text") ?? gm.transform.Find("Score_HUD/StreakText") ?? gm.transform.Find("StreakText");
            if (stText != null) SetProp(so, "streakText", stText.GetComponent<TextMeshProUGUI>());

            Slider pBar = gm.GetComponentInChildren<Slider>(true);
            if (pBar != null) SetProp(so, "progressBar", pBar);

            so.ApplyModifiedProperties();
            gm.AutoBindHierarchyElements();
            EditorUtility.SetDirty(gm);
        }
    }
}
