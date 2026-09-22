using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics.EditorTools
{
    public class U6_HierarchyAutomator : EditorWindow
    {
        private const string AUDIO_BASE_PATH = "Assets/Audio/U6_audio";

        [MenuItem("Masters Phonics/Unit 6/Generate Unit 6 Hierarchy (Clean & Complete)", false, 100)]
        public static void GenerateUnit6Hierarchy()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in the current Scene!", "OK");
                return;
            }

            Transform sourceUnit = canvas.transform.Find("Unit_5") ?? canvas.transform.Find("Unit_4") ?? canvas.transform.Find("Unit_3");
            if (sourceUnit == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find 'Unit_5', 'Unit_4', or 'Unit_3' under Canvas as a base template!", "OK");
                return;
            }

            // Check if Unit_6 already exists
            Transform existingU6 = canvas.transform.Find("Unit_6");
            if (existingU6 != null)
            {
                bool overwrite = EditorUtility.DisplayDialog("Unit_6 Exists", "Unit_6 already exists in Canvas. Do you want to replace it with a clean Unit 6 hierarchy?", "Yes, Replace", "Cancel");
                if (!overwrite) return;
                Undo.DestroyObjectImmediate(existingU6.gameObject);
            }

            spriteCache = null;
            audioCache = null;
            audioList = null;

            // 1. Duplicate Template Root
            GameObject unit6Obj = Object.Instantiate(sourceUnit.gameObject, canvas.transform);
            unit6Obj.name = "Unit_6";
            Undo.RegisterCreatedObjectUndo(unit6Obj, "Generate Clean Unit_6");

            // Strip Old Root Components
            StripOldUnitComponents(unit6Obj);

            // Add Unit 6 Managers
            var flowManager = unit6Obj.AddComponent<U6_SA_UnitFlowManager_Masters_Phonics>();
            var audioManager = unit6Obj.AddComponent<U6_SA_AudioManager_Masters_Phonics>();

            // Assign Root Audio Manager & Flow Manager Clips
            AssignRootAudioClips(audioManager, flowManager);

            // Setup Section Selection Panels (Signboard)
            Transform selPanels = unit6Obj.transform.Find("Unit_5_Section_Selection_Panels") ?? unit6Obj.transform.Find("Unit_4_Section_Selection_Panels") ?? unit6Obj.transform.Find("Unit_3_Section_Selection_Panels") ?? unit6Obj.transform.Find("Unit_6_Section_Selection_Panels");
            if (selPanels != null)
            {
                selPanels.name = "Unit_6_Section_Selection_Panels";
                UpdateSignboardTitles(selPanels);
            }

            // Setup Sections Parent
            Transform sectionsParent = unit6Obj.transform.Find("Unit_5_Sections") ?? unit6Obj.transform.Find("Unit_4_Sections") ?? unit6Obj.transform.Find("Unit_3_Sections") ?? unit6Obj.transform.Find("Unit_6_Sections");
            if (sectionsParent == null)
            {
                GameObject secGo = new GameObject("Unit_6_Sections", typeof(RectTransform));
                secGo.transform.SetParent(unit6Obj.transform, false);
                sectionsParent = secGo.transform;
            }
            sectionsParent.name = "Unit_6_Sections";

            // Clean up any old leftover unit panels from previous unit templates
            for (int i = sectionsParent.childCount - 1; i >= 0; i--)
            {
                Transform child = sectionsParent.GetChild(i);
                string cName = child.name;
                if (cName.StartsWith("U1_") || cName.StartsWith("U2_") || cName.StartsWith("U3_") || cName.StartsWith("U4_") || cName.StartsWith("U5_"))
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
                Sprite bgSprite = LoadSpriteMatching("U6_BG") ?? LoadSpriteMatching("U3_MP_BG") ?? LoadSpriteMatching("BG");
                if (bgSprite != null) bgImg.sprite = bgSprite;
                bgImg.color = Color.white;
            }
            bgTr.gameObject.name = "BG_Image";
            bgTr.gameObject.SetActive(true);
            bgTr.SetAsFirstSibling();

            // Clean & Build dedicated Panels for each Unit 6 section with Audio Wired
            GameObject learnGo = BuildOrCleanPanel(sectionsParent, "U6_learn_Concept_Cards_Panel");
            SetupGM01ConceptCards(learnGo);

            GameObject act1Go = BuildOrCleanPanel(sectionsParent, "U6_Activity_1_Magic_Wand");
            SetupGM05MagicWand(act1Go);

            GameObject act2Go = BuildOrCleanPanel(sectionsParent, "U6_Activity_2_Which_Long_Vowel");
            SetupGM04WhichLongVowel(act2Go);

            GameObject act3Go = BuildOrCleanPanel(sectionsParent, "U6_Activity_3_Soft_Or_Hard");
            SetupGM03sSoftOrHard(act3Go);

            GameObject act4Go = BuildOrCleanPanel(sectionsParent, "U6_Activity_4_Why_The_E");
            SetupGM03WhyTheE(act4Go);

            GameObject mapGo = BuildOrCleanPanel(sectionsParent, "U6_SevenTypesMap_Panel");
            SetupSevenTypesMap(mapGo);

            GameObject challengeGo = BuildOrCleanPanel(sectionsParent, "U6_UnitChallenge_Panel");
            SetupUnitChallenge(challengeGo);

            GameObject compGo = BuildOrCleanPanel(sectionsParent, "U6_COMPLETE_Panel");
            SetupCompletionPanel(compGo);

            // Wire Flow Manager
            WireFlowManager(flowManager, selPanels, sectionsParent, learnGo, act1Go, act2Go, act3Go, act4Go, mapGo, challengeGo, compGo);

            EditorUtility.SetDirty(unit6Obj);
            Selection.activeGameObject = unit6Obj;

            EditorUtility.DisplayDialog("Unit 6 Hierarchy Created!", 
                "Successfully generated clean Unit 6 GameObject hierarchies and assigned all Voice A, Voice B, and SFX audio clips to Inspector fields!", 
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
                if (typeName.StartsWith("U1_") || typeName.StartsWith("U2_") || typeName.StartsWith("U3_") || typeName.StartsWith("U4_") || typeName.StartsWith("U5_"))
                {
                    DestroyImmediate(mb);
                }
            }
        }

        // =========================================================================
        // Root Audio & Flow Manager Assignment
        // =========================================================================

        private static void AssignRootAudioClips(U6_SA_AudioManager_Masters_Phonics audioMgr, U6_SA_UnitFlowManager_Masters_Phonics flowMgr)
        {
            audioMgr.wandSparkleClip = LoadAudioMatching("U06_SFX_wand", "SFX") ?? LoadAudioMatching("wand", "SFX");
            audioMgr.vowelStretchClip = LoadAudioMatching("U06_SFX_vowel_stretch", "SFX") ?? LoadAudioMatching("vowel_stretch", "SFX");
            audioMgr.softenClip = LoadAudioMatching("U06_SFX_soften", "SFX") ?? LoadAudioMatching("soften", "SFX");
            audioMgr.pictureMorphClip = LoadAudioMatching("U06_SFX_picture_morph", "SFX") ?? LoadAudioMatching("picture_morph", "SFX");
            audioMgr.correctClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX") ?? LoadAudioMatching("Correct", "SFX");
            audioMgr.wrongClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX") ?? LoadAudioMatching("Incorrect", "SFX");
            audioMgr.celebrationClip = LoadAudioMatching("celebration") ?? LoadAudioMatching("complete");

            flowMgr.unitIntroClip = LoadAudioMatching("Unit Six One silent letter", "voice A") ?? LoadAudioMatching("U06_VO_unit_intro", "voice A");
            flowMgr.unitCompleteVoiceClip = LoadAudioMatching("Unit Six done Twelve words transformed", "voice A") ?? LoadAudioMatching("U06_VO_unit_complete", "voice A");

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
        // Panel Builders & Audio Inspectors
        // =========================================================================

        // 1. LEARN: Concept Cards (GM-01)
        private static void SetupGM01ConceptCards(GameObject panel)
        {
            var gm01 = panel.AddComponent<U6_SA_GM01_ConceptCards_Masters_Phonics>();

            gm01.card1IntroClip = LoadAudioMatching("Bossy e seems a bit rude", "voice A") ?? LoadAudioMatching("U06_VO_card1", "voice A");
            gm01.card2IntroClip = LoadAudioMatching("Seven jobs The first one is the big one", "voice A") ?? LoadAudioMatching("U06_VO_card2", "voice A");
            gm01.card3IntroClip = LoadAudioMatching("c usually says k Cat Cup", "voice A") ?? LoadAudioMatching("U06_VO_card3", "voice A");
            gm01.card4IntroClip = LoadAudioMatching("Now the rulebreakers English words dont end", "voice A") ?? LoadAudioMatching("U06_VO_card4", "voice A");

            CreateHeader(panel.transform, "CONCEPT CARDS", "Magic 'e' & Bossy 'e': The Letter That Changes Everything", out _, out _);
            
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
            so.ApplyModifiedProperties();
        }

        // 2. ACTIVITY 1: Magic Wand (GM-05 Tile Drop & Picture Morph)
        private static void SetupGM05MagicWand(GameObject panel)
        {
            var gm05 = panel.AddComponent<U6_SA_GM05_MagicWand_Masters_Phonics>();

            gm05.wandSparkleSFX = LoadAudioMatching("U06_SFX_wand", "SFX") ?? LoadAudioMatching("wand", "SFX");
            gm05.vowelStretchSFX = LoadAudioMatching("U06_SFX_vowel_stretch", "SFX") ?? LoadAudioMatching("vowel_stretch", "SFX");
            gm05.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX") ?? LoadAudioMatching("Correct", "SFX");
            gm05.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX") ?? LoadAudioMatching("Incorrect", "SFX");

            CreateHeader(panel.transform, "ACTIVITY 1: MAGIC WAND", "Drag the Magic 'e' to transform the word and picture!", out _, out _);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            GameObject cardCont = CreateContainer(panel.transform, "TransformationCard", new Vector2(0f, 80f), new Vector2(1100f, 460f));
            GameObject pictureObj = CreateContainer(cardCont.transform, "MorphPicture", new Vector2(0f, 90f), new Vector2(320f, 220f));
            Image picImg = pictureObj.AddComponent<Image>();
            picImg.preserveAspect = true;
            Sprite defaultPic = LoadSpriteMatching("U6_pic_tub") ?? LoadSpriteMatching("tub");
            if (defaultPic != null) picImg.sprite = defaultPic;

            GameObject wordRow = CreateContainer(cardCont.transform, "WordRow", new Vector2(0f, -70f), new Vector2(850f, 110f));
            TextMeshProUGUI beforeWordTMP = CreateText(wordRow.transform, "BeforeWordText", "t u b", 64);
            beforeWordTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 90f);

            GameObject slotObj = CreateContainer(wordRow.transform, "DropSlot", new Vector2(220f, 0f), new Vector2(130f, 110f));
            Image slotImg = slotObj.AddComponent<Image>();
            Sprite slotSprite = LoadSpriteMatching("U6_ui_slot_drop") ?? LoadSpriteMatching("slot_drop");
            if (slotSprite != null)
            {
                slotImg.sprite = slotSprite;
            }
            else
            {
                slotImg.sprite = U6_SA_GM05_MagicWand_Masters_Phonics.GetOrCreateRoundedSprite();
                slotImg.type = Image.Type.Sliced;
                slotImg.color = new Color(1f, 1f, 1f, 0.25f);
            }

            GameObject tileSpawn = CreateContainer(panel.transform, "TileSpawnZone", new Vector2(0f, -220f), new Vector2(320f, 130f));
            GameObject eTile = CreateContainer(tileSpawn.transform, "MagicETile", Vector2.zero, new Vector2(130f, 110f));
            Image tileImg = eTile.AddComponent<Image>();
            Sprite tileSprite = LoadSpriteMatching("U6_ui_tile_magic_e") ?? LoadSpriteMatching("tile_magic_e");
            if (tileSprite != null)
            {
                tileImg.sprite = tileSprite;
            }
            else
            {
                tileImg.sprite = U6_SA_GM05_MagicWand_Masters_Phonics.GetOrCreateRoundedSprite();
                tileImg.type = Image.Type.Sliced;
                tileImg.color = new Color(1f, 0.85f, 0.25f, 1f);
            }
            eTile.AddComponent<CanvasGroup>();
            TextMeshProUGUI eTileText = CreateText(eTile.transform, "EText", "<b>e</b>", 60);
            eTileText.raycastTarget = false;
            eTileText.color = new Color(0.12f, 0.16f, 0.28f, 1f);

            GameObject checkCont = CreateContainer(panel.transform, "VowelSoundCheckContainer", new Vector2(0f, -360f), new Vector2(950f, 110f));
            checkCont.SetActive(false);
            Button shortBtn = CreateActionButton(checkCont.transform, "ShortVowelBtn", "SHORT VOWEL", new Vector2(-240f, 0f), new Vector2(300f, 80f));
            Button longBtn = CreateActionButton(checkCont.transform, "LongVowelBtn", "LONG VOWEL", new Vector2(240f, 0f), new Vector2(300f, 80f));

            CreateHUD(panel.transform, out TextMeshProUGUI scoreTxt, out TextMeshProUGUI streakTxt, out Slider pBar, out TextMeshProUGUI pText);

            SerializedObject so = new SerializedObject(gm05);
            SetProp(so, "progressText", pText);
            SetProp(so, "scoreText", scoreTxt);
            SetProp(so, "progressBar", pBar);
            SetProp(so, "replayAudioBtn", replayBtn);
            SetProp(so, "illustrationImage", picImg);
            SetProp(so, "wordDisplayText", beforeWordTMP);
            SetProp(so, "dropSlot", slotObj.GetComponent<RectTransform>());
            SetProp(so, "draggableETile", eTile.GetComponent<RectTransform>());
            SetProp(so, "draggableCanvasGroup", eTile.GetComponent<CanvasGroup>());
            SetProp(so, "soundCheckContainer", checkCont);
            SetProp(so, "shortSoundButton", shortBtn);
            SetProp(so, "longSoundButton", longBtn);
            SetProp(so, "shortSoundButtonText", shortBtn.GetComponentInChildren<TextMeshProUGUI>());
            SetProp(so, "longSoundButtonText", longBtn.GetComponentInChildren<TextMeshProUGUI>());

            // Wire transformationItems audio & sprites
            SerializedProperty transItemsProp = so.FindProperty("transformationItems");
            if (transItemsProp != null)
            {
                for (int i = 0; i < transItemsProp.arraySize; i++)
                {
                    SerializedProperty itemProp = transItemsProp.GetArrayElementAtIndex(i);
                    string beforeWord = itemProp.FindPropertyRelative("beforeWord").stringValue;
                    string afterWord = itemProp.FindPropertyRelative("afterWord").stringValue;

                    AudioClip beforeClip = LoadAudioMatching(beforeWord, "voice B");
                    AudioClip afterClip = LoadAudioMatching(afterWord, "voice B");
                    if (beforeClip != null) itemProp.FindPropertyRelative("beforeAudio").objectReferenceValue = beforeClip;
                    if (afterClip != null) itemProp.FindPropertyRelative("afterAudio").objectReferenceValue = afterClip;

                    Sprite bSprite = LoadSpriteMatching($"U6_pic_{beforeWord}") ?? LoadSpriteMatching(beforeWord);
                    Sprite aSprite = LoadSpriteMatching($"U6_pic_{afterWord}") ?? LoadSpriteMatching(afterWord);
                    if (bSprite != null) itemProp.FindPropertyRelative("beforeSprite").objectReferenceValue = bSprite;
                    if (aSprite != null) itemProp.FindPropertyRelative("afterSprite").objectReferenceValue = aSprite;
                }
            }

            so.ApplyModifiedProperties();
        }

        // 3. ACTIVITY 2: Which Long Vowel? (GM-04 5-Button Comparison)
        private static void SetupGM04WhichLongVowel(GameObject panel)
        {
            var gm04 = panel.AddComponent<U6_SA_GM04_WhichLongVowel_Masters_Phonics>();

            gm04.longAAudio = LoadAudioMatching("U06_WRD_cake", "voice B") ?? LoadAudioMatching("cake", "voice B");
            gm04.longEAudio = LoadAudioMatching("U06_WRD_these", "voice B") ?? LoadAudioMatching("these", "voice B");
            gm04.longIAudio = LoadAudioMatching("U06_WRD_kite", "voice B") ?? LoadAudioMatching("kite", "voice B");
            gm04.longOAudio = LoadAudioMatching("U06_WRD_home", "voice B") ?? LoadAudioMatching("home", "voice B");
            gm04.longUAudio = LoadAudioMatching("U06_WRD_mule", "voice B") ?? LoadAudioMatching("mule", "voice B");
            gm04.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX") ?? LoadAudioMatching("Correct", "SFX");
            gm04.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX") ?? LoadAudioMatching("Incorrect", "SFX");

            CreateHeader(panel.transform, "ACTIVITY 2: WHICH LONG VOWEL?", "Listen to the word! Tap the matching long vowel sound.", out _, out _);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(0f, 180f));

            GameObject buttonsContainer = CreateContainer(panel.transform, "FiveButtonsContainer", new Vector2(0f, -60f), new Vector2(1300f, 220f));
            var hlg = buttonsContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 24f;
            hlg.childAlignment = TextAnchor.MiddleCenter;

            Button[] vowelBtns = new Button[5];
            TextMeshProUGUI[] vowelLabels = new TextMeshProUGUI[5];
            string[] vowelLabelsStr = { "Long a\n/ay/", "Long e\n/ee/", "Long i\n/eye/", "Long o\n/oh/", "Long u\n/you/" };
            for (int i = 0; i < 5; i++)
            {
                vowelBtns[i] = CreateActionButton(buttonsContainer.transform, $"VowelBtn_{i}", vowelLabelsStr[i], Vector2.zero, new Vector2(230f, 140f));
                vowelLabels[i] = vowelBtns[i].GetComponentInChildren<TextMeshProUGUI>();
            }

            GameObject revealObj = CreateContainer(panel.transform, "WordRevealContainer", new Vector2(0f, -300f), new Vector2(900f, 130f));
            TextMeshProUGUI revealTMP = CreateText(revealObj.transform, "RevealedWordText", "", 48);

            CreateHUD(panel.transform, out TextMeshProUGUI scoreTxt, out _, out Slider pBar, out TextMeshProUGUI pText);

            SerializedObject so = new SerializedObject(gm04);
            SetProp(so, "progressText", pText);
            SetProp(so, "scoreText", scoreTxt);
            SetProp(so, "progressBar", pBar);
            SetProp(so, "playPromptWordBtn", replayBtn);
            SetProp(so, "wordRevealText", revealTMP);

            SerializedProperty btnArr = so.FindProperty("vowelButtons");
            if (btnArr != null)
            {
                btnArr.arraySize = 5;
                for (int i = 0; i < 5; i++) btnArr.GetArrayElementAtIndex(i).objectReferenceValue = vowelBtns[i];
            }

            SerializedProperty lblArr = so.FindProperty("vowelButtonLabels");
            if (lblArr != null)
            {
                lblArr.arraySize = 5;
                for (int i = 0; i < 5; i++) lblArr.GetArrayElementAtIndex(i).objectReferenceValue = vowelLabels[i];
            }

            // Wire vowelItems audio
            SerializedProperty vowelItemsProp = so.FindProperty("vowelItems");
            if (vowelItemsProp != null)
            {
                for (int i = 0; i < vowelItemsProp.arraySize; i++)
                {
                    SerializedProperty itemProp = vowelItemsProp.GetArrayElementAtIndex(i);
                    string word = itemProp.FindPropertyRelative("word").stringValue;
                    AudioClip clip = LoadAudioMatching(word, "voice B");
                    if (clip != null) itemProp.FindPropertyRelative("wordAudio").objectReferenceValue = clip;
                }
            }

            so.ApplyModifiedProperties();
        }

        // 4. ACTIVITY 3: Soft or Hard? (GM-03s Fast Swiper)
        private static void SetupGM03sSoftOrHard(GameObject panel)
        {
            var gm03s = panel.AddComponent<U6_SA_GM03s_SoftOrHard_Masters_Phonics>();

            gm03s.softSoundSFX = LoadAudioMatching("U06_SFX_soften", "SFX") ?? LoadAudioMatching("soften", "SFX");
            gm03s.hardSoundSFX = LoadAudioMatching("Click", "SFX");
            gm03s.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX") ?? LoadAudioMatching("Correct", "SFX");
            gm03s.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX") ?? LoadAudioMatching("Incorrect", "SFX");

            CreateHeader(panel.transform, "ACTIVITY 3: SOFT OR HARD?", "c and g go SOFT before e, i, y! Swipe LEFT for Hard, RIGHT for Soft.", out _, out _);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            GameObject leftInd = CreateSwipeIndicator(panel.transform, "HardIndicator", "<< HARD (/k/, /g/)", new Vector2(-540f, 0f));
            Sprite hardBinSprite = LoadSpriteMatching("U6_ui_bin_hard") ?? LoadSpriteMatching("bin_hard");
            if (hardBinSprite != null) leftInd.GetComponent<Image>().sprite = hardBinSprite;

            GameObject rightInd = CreateSwipeIndicator(panel.transform, "SoftIndicator", "SOFT (/s/, /j/) >>", new Vector2(540f, 0f));
            Sprite softBinSprite = LoadSpriteMatching("U6_ui_bin_soft") ?? LoadSpriteMatching("bin_soft");
            if (softBinSprite != null) rightInd.GetComponent<Image>().sprite = softBinSprite;

            GameObject cardSpawn = CreateContainer(panel.transform, "CardSpawnZone", Vector2.zero, new Vector2(540f, 360f));
            Image cardBg = cardSpawn.AddComponent<Image>();
            cardBg.sprite = U6_SA_GM05_MagicWand_Masters_Phonics.GetOrCreateRoundedSprite();
            cardBg.type = Image.Type.Sliced;
            cardBg.color = new Color(0.96f, 0.98f, 1f, 1f);
            cardSpawn.AddComponent<CanvasGroup>();

            TextMeshProUGUI cardWordTxt = CreateText(cardSpawn.transform, "WordText", "<b>city</b>", 56);
            cardWordTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 20f);
            cardWordTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(480f, 100f);
            cardWordTxt.color = new Color(0.1f, 0.15f, 0.28f, 1f);

            TextMeshProUGUI hintTxt = CreateText(cardSpawn.transform, "HintText", "Letter: <b>C</b>", 24);
            hintTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -50f);
            hintTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(480f, 50f);
            hintTxt.color = new Color(0.4f, 0.45f, 0.55f, 1f);

            CreateHUD(panel.transform, out TextMeshProUGUI scoreTxt, out TextMeshProUGUI streakTxt, out Slider pBar, out TextMeshProUGUI pText);

            SerializedObject so = new SerializedObject(gm03s);
            SetProp(so, "progressText", pText);
            SetProp(so, "scoreText", scoreTxt);
            SetProp(so, "streakText", streakTxt);
            SetProp(so, "progressBar", pBar);
            SetProp(so, "cardContainer", cardSpawn.GetComponent<RectTransform>());
            SetProp(so, "cardWordText", cardWordTxt);
            SetProp(so, "cardPhoneticHintText", hintTxt);
            SetProp(so, "cardCanvasGroup", cardSpawn.GetComponent<CanvasGroup>());
            SetProp(so, "hardLeftBin", leftInd.GetComponent<RectTransform>());
            SetProp(so, "softRightBin", rightInd.GetComponent<RectTransform>());
            SetProp(so, "tapHardLeftBtn", leftInd.GetComponent<Button>());
            SetProp(so, "tapSoftRightBtn", rightInd.GetComponent<Button>());

            // Wire swipeItems audio
            SerializedProperty swipeItemsProp = so.FindProperty("swipeItems");
            if (swipeItemsProp != null)
            {
                for (int i = 0; i < swipeItemsProp.arraySize; i++)
                {
                    SerializedProperty itemProp = swipeItemsProp.GetArrayElementAtIndex(i);
                    string word = itemProp.FindPropertyRelative("word").stringValue;
                    AudioClip clip = LoadAudioMatching(word, "voice B");
                    if (clip != null) itemProp.FindPropertyRelative("wordAudio").objectReferenceValue = clip;
                }
            }

            so.ApplyModifiedProperties();
        }

        // 5. ACTIVITY 4: Why The E? (GM-03 3-Bin Sorter)
        private static void SetupGM03WhyTheE(GameObject panel)
        {
            var gm03 = panel.AddComponent<U6_SA_GM03_WhyTheE_Masters_Phonics>();

            gm03.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX") ?? LoadAudioMatching("Correct", "SFX");
            gm03.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX") ?? LoadAudioMatching("Incorrect", "SFX");

            CreateHeader(panel.transform, "ACTIVITY 4: WHY THE E?", "Why is the silent 'e' at the end of the word? Drag to the correct job!", out _, out _);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            GameObject cardCont = CreateContainer(panel.transform, "CardContainer", new Vector2(0f, 140f), new Vector2(560f, 260f));
            Image cardBg = cardCont.AddComponent<Image>();
            cardBg.sprite = U6_SA_GM05_MagicWand_Masters_Phonics.GetOrCreateRoundedSprite();
            cardBg.type = Image.Type.Sliced;
            cardBg.color = new Color(0.96f, 0.98f, 1f, 1f);
            cardCont.AddComponent<CanvasGroup>();

            TextMeshProUGUI wordCardTMP = CreateText(cardCont.transform, "WordText", "<b>cake</b>", 56);
            wordCardTMP.color = new Color(0.1f, 0.15f, 0.28f, 1f);

            RectTransform bin1 = CreateDropBin(panel.transform, "Bin1_MakesVowelLong", "MAKES VOWEL LONG\n(cake, hide, cube)", new Vector2(-500f, -200f));
            Sprite b1Spr = LoadSpriteMatching("U6_ui_bin_vowel_long") ?? LoadSpriteMatching("bin_vowel_long");
            if (b1Spr != null) bin1.GetComponent<Image>().sprite = b1Spr;

            RectTransform bin2 = CreateDropBin(panel.transform, "Bin2_HoldsAPlace", "HOLDS A PLACE\n(give, have, blue)", new Vector2(0f, -200f));
            Sprite b2Spr = LoadSpriteMatching("U6_ui_bin_holds_place") ?? LoadSpriteMatching("bin_holds_place");
            if (b2Spr != null) bin2.GetComponent<Image>().sprite = b2Spr;

            RectTransform bin3 = CreateDropBin(panel.transform, "Bin3_NotAPlural", "NOT A PLURAL\n(house, mouse, nurse)", new Vector2(500f, -200f));
            Sprite b3Spr = LoadSpriteMatching("U6_ui_bin_not_plural") ?? LoadSpriteMatching("bin_not_plural");
            if (b3Spr != null) bin3.GetComponent<Image>().sprite = b3Spr;

            CreateHUD(panel.transform, out TextMeshProUGUI scoreTxt, out TextMeshProUGUI streakTxt, out Slider pBar, out TextMeshProUGUI pText);

            SerializedObject so = new SerializedObject(gm03);
            SetProp(so, "itemCounterText", pText);
            SetProp(so, "scoreText", scoreTxt);
            SetProp(so, "streakText", streakTxt);
            SetProp(so, "progressBar", pBar);
            SetProp(so, "replayAudioButton", replayBtn);
            SetProp(so, "cardContainer", cardCont.GetComponent<RectTransform>());
            SetProp(so, "wordCardTMP", wordCardTMP);
            SetProp(so, "cardCanvasGroup", cardCont.GetComponent<CanvasGroup>());
            SetProp(so, "bin1Transform", bin1);
            SetProp(so, "bin2Transform", bin2);
            SetProp(so, "bin3Transform", bin3);
            SetProp(so, "bin1LabelTMP", bin1.GetComponentInChildren<TextMeshProUGUI>());
            SetProp(so, "bin2LabelTMP", bin2.GetComponentInChildren<TextMeshProUGUI>());
            SetProp(so, "bin3LabelTMP", bin3.GetComponentInChildren<TextMeshProUGUI>());

            // Wire sessionPool audio
            SerializedProperty sessionPoolProp = so.FindProperty("sessionPool");
            if (sessionPoolProp != null)
            {
                for (int i = 0; i < sessionPoolProp.arraySize; i++)
                {
                    SerializedProperty itemProp = sessionPoolProp.GetArrayElementAtIndex(i);
                    string word = itemProp.FindPropertyRelative("word").stringValue;
                    AudioClip clip = LoadAudioMatching(word, "voice B");
                    if (clip != null) itemProp.FindPropertyRelative("wordAudio").objectReferenceValue = clip;
                }
            }

            so.ApplyModifiedProperties();
        }

        // 6. SEVEN TYPES MAP (Section F)
        private static void SetupSevenTypesMap(GameObject panel)
        {
            var map = panel.AddComponent<U6_SA_SevenTypesMap_Masters_Phonics>();

            map.mapIntroClip = LoadAudioMatching("Seven Syllable Types", "voice A") ?? LoadAudioMatching("U06_VO_map_unit6", "voice A");

            CreateHeader(panel.transform, "THE 7 SYLLABLE TYPES MAP", "3 of 7 Syllable Types Mastered: Closed, Open, and Magic 'e'!", out TextMeshProUGUI titleTMP, out TextMeshProUGUI subTMP);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            GameObject tilesContainer = CreateContainer(panel.transform, "Tiles_Container", new Vector2(50f, -60f), new Vector2(1600f, 460f));

            SerializedObject so = new SerializedObject(map);
            SetProp(so, "titleTMP", titleTMP);
            SetProp(so, "subtitleTMP", subTMP);
            SetProp(so, "tilesContainer", tilesContainer.transform);
            SetProp(so, "replayAudioButton", replayBtn);
            so.ApplyModifiedProperties();
        }

        // 7. UNIT CHALLENGE (Section G)
        private static void SetupUnitChallenge(GameObject panel)
        {
            var challenge = panel.AddComponent<U6_SA_UnitChallenge_Masters_Phonics>();
            challenge.correctSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Correct.mp3") ?? LoadAudioMatching("Correct.mp3", "SFX") ?? LoadAudioMatching("Correct", "SFX");
            challenge.wrongSFX = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/SFX/SFX/Incorrect.mp3") ?? LoadAudioMatching("Incorrect.mp3", "SFX") ?? LoadAudioMatching("Incorrect", "SFX");

            CreateHeader(panel.transform, "UNIT 6 FINAL CHALLENGE", "Mastery Challenge: The 7 Jobs of Magic 'e'!", out _, out _);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            TextMeshProUGUI qNumTxt = CreateText(panel.transform, "QuestionNumberText", "Question 1 of 10", 28);
            qNumTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 260f);
            qNumTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 50f);

            TextMeshProUGUI promptTxt = CreateText(panel.transform, "PromptText", "Question Prompt", 34);
            promptTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 180f);
            promptTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 110f);

            GameObject optionsContainer = CreateContainer(panel.transform, "OptionsContainer", new Vector2(0f, -100f), new Vector2(1200f, 380f));
            var vlg = optionsContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20f;
            vlg.childAlignment = TextAnchor.MiddleCenter;

            Button[] optionBtns = new Button[3];
            TextMeshProUGUI[] optionTxts = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                optionBtns[i] = CreateActionButton(optionsContainer.transform, $"Option_{i}", $"Option {i+1}", Vector2.zero, new Vector2(960f, 90f));
                optionTxts[i] = optionBtns[i].GetComponentInChildren<TextMeshProUGUI>();
            }

            CreateHUD(panel.transform, out TextMeshProUGUI scoreTxt, out TextMeshProUGUI streakTxt, out Slider pBar, out _);

            SerializedObject so = new SerializedObject(challenge);
            SetProp(so, "questionNumberText", qNumTxt);
            SetProp(so, "promptText", promptTxt);
            SetProp(so, "replayAudioBtn", replayBtn);
            SetProp(so, "optionsContainer", optionsContainer.transform);
            SetProp(so, "progressBar", pBar);
            SetProp(so, "scoreText", scoreTxt);
            SetProp(so, "streakText", streakTxt);
            SetProp(so, "correctSFX", challenge.correctSFX);
            SetProp(so, "wrongSFX", challenge.wrongSFX);

            SerializedProperty btnArr = so.FindProperty("optionButtons");
            if (btnArr != null)
            {
                btnArr.arraySize = 3;
                for (int i = 0; i < 3; i++) btnArr.GetArrayElementAtIndex(i).objectReferenceValue = optionBtns[i];
            }

            SerializedProperty txtArr = so.FindProperty("optionTexts");
            if (txtArr != null)
            {
                txtArr.arraySize = 3;
                for (int i = 0; i < 3; i++) txtArr.GetArrayElementAtIndex(i).objectReferenceValue = optionTxts[i];
            }

            so.ApplyModifiedProperties();
        }

        // 8. COMPLETION PANEL
        private static void SetupCompletionPanel(GameObject panel)
        {
            CreateHeader(panel.transform, "COMPLETED UNIT - 6", "Magic 'e' Mastered!", out _, out _);

            GameObject medalObj = CreateContainer(panel.transform, "Medal", new Vector2(0f, 80f), new Vector2(300f, 300f));
            Image img = medalObj.AddComponent<Image>();
            img.preserveAspect = true;
            Sprite badgeSprite = LoadSpriteMatching("U6_badge_wand_bearer") ?? LoadSpriteMatching("badge_wand_bearer");
            if (badgeSprite != null)
            {
                img.sprite = badgeSprite;
                img.color = Color.white;
            }
            else
            {
                img.color = new Color(1f, 0.84f, 0f, 1f);
            }

            TextMeshProUGUI badgeTxt = CreateText(panel.transform, "Badge_Title", "BADGE UNLOCKED: WAND BEARER", 34);
            badgeTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -90f);
            badgeTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 60f);

            TextMeshProUGUI scoreTxt = CreateText(panel.transform, "FinalScoreText", "Total Unit Score: 5800 / 5800", 38);
            scoreTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -160f);
            scoreTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 60f);

            TextMeshProUGUI statsTxt = CreateText(panel.transform, "FinalStatsText", "Words Transformed: 12  -  Silent e's Explained: 18\nSyllable Types Mastered: 3 of 7", 26);
            statsTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -230f);
            statsTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(1400f, 70f);

            Button contBtn = CreateCompletionButton(panel.transform, "ContinueToSignboardButton", "BACK TO UNIT MAP", new Vector2(-260f, -360f), new Vector2(340f, 85f));
            Button nextUnitBtn = CreateCompletionButton(panel.transform, "NextUnitButton", "NEXT UNIT", new Vector2(260f, -360f), new Vector2(340f, 85f));
        }

        // =========================================================================
        // UI Helpers & Button Duplicators
        // =========================================================================

        private static Button FindTemplateButton(params string[] candidateNames)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                // Search in preceding units first (Unit_5 down to Unit_1)
                for (int u = 5; u >= 1; u--)
                {
                    Transform uTr = canvas.transform.Find($"Unit_{u}") ?? canvas.transform.Find($"Unit{u}");
                    if (uTr != null)
                    {
                        Button[] btns = uTr.GetComponentsInChildren<Button>(true);
                        foreach (var name in candidateNames)
                        {
                            foreach (var b in btns)
                            {
                                if (b != null && b.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                                    return b;
                            }
                        }
                        foreach (var name in candidateNames)
                        {
                            foreach (var b in btns)
                            {
                                if (b != null && b.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                                    return b;
                            }
                        }
                    }
                }

                // Search across whole Canvas
                Button[] allCanvasBtns = canvas.GetComponentsInChildren<Button>(true);
                foreach (var name in candidateNames)
                {
                    foreach (var b in allCanvasBtns)
                    {
                        if (b != null && b.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                            return b;
                    }
                }
                foreach (var name in candidateNames)
                {
                    foreach (var b in allCanvasBtns)
                    {
                        if (b != null && b.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                            return b;
                    }
                }
            }

            // Search active scene hierarchy
            Button[] allSceneBtns = Resources.FindObjectsOfTypeAll<Button>();
            foreach (var name in candidateNames)
            {
                foreach (var b in allSceneBtns)
                {
                    if (b != null && !EditorUtility.IsPersistent(b.gameObject) && b.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                        return b;
                }
            }

            return null;
        }

        private static void CreateHeader(Transform parent, string title, string subtitle, out TextMeshProUGUI titleTMP, out TextMeshProUGUI subTMP)
        {
            titleTMP = CreateText(parent, "TitleText", $"<b>{title}</b>", 42);
            titleTMP.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 440f);
            titleTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(1600f, 70f);
            titleTMP.color = Color.white;
            titleTMP.fontStyle = FontStyles.Bold;

            subTMP = CreateText(parent, "SubtitleText", $"<b>{subtitle}</b>", 28);
            subTMP.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 385f);
            subTMP.GetComponent<RectTransform>().sizeDelta = new Vector2(1600f, 55f);
            subTMP.color = new Color(0.92f, 0.96f, 1f, 1f);
            subTMP.fontStyle = FontStyles.Bold;
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

        private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float fontSize)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = content.StartsWith("<b>") ? content : $"<b>{content}</b>";
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(800f, 60f);
            return tmp;
        }

        private static Button CreateActionButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            Button template = FindTemplateButton(name, "WaterButton", "ActionButton", "Btn_Action", "Option_0", "Option_1");
            if (template != null)
            {
                GameObject clone = Object.Instantiate(template.gameObject, parent, false);
                clone.name = name;
                RectTransform cRt = clone.GetComponent<RectTransform>();
                cRt.anchoredPosition = pos;
                if (size != Vector2.zero) cRt.sizeDelta = size;

                Button cloneBtn = clone.GetComponent<Button>();
                if (cloneBtn != null)
                {
                    while (cloneBtn.onClick.GetPersistentEventCount() > 0)
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(cloneBtn.onClick, 0);
                }

                var tmp = clone.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = $"<b>{label}</b>";
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = Color.white;
                }
                return cloneBtn;
            }

            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            Sprite btnSpr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Icons/waterButton.png")
                         ?? LoadSpriteMatching("waterButton")
                         ?? LoadSpriteMatching("button")
                         ?? U6_SA_GM05_MagicWand_Masters_Phonics.GetOrCreateRoundedSprite();
            if (btnSpr != null)
            {
                img.sprite = btnSpr;
                img.type = Image.Type.Sliced;
            }
            img.color = Color.white;

            TextMeshProUGUI textComp = CreateText(go.transform, "Text", $"<b>{label}</b>", 24);
            textComp.color = Color.white;
            textComp.fontStyle = FontStyles.Bold;
            RectTransform textRt = textComp.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            return go.GetComponent<Button>();
        }

        private static Button CreateCompletionButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            string[] searchTerms = name.ToLower().Contains("next")
                ? new string[] { "NextUnitButton", "Btn_NextUnit", "NextButton", "Btn_Next" }
                : new string[] { "ContinueToSignboardButton", "ContinueButton", "Btn_Continue", "Continue_Button" };

            Button template = FindTemplateButton(searchTerms);
            if (template != null)
            {
                GameObject clone = Object.Instantiate(template.gameObject, parent, false);
                clone.name = name;
                RectTransform cRt = clone.GetComponent<RectTransform>();
                cRt.anchoredPosition = pos;
                if (size != Vector2.zero) cRt.sizeDelta = size;

                Button cloneBtn = clone.GetComponent<Button>();
                if (cloneBtn != null)
                {
                    while (cloneBtn.onClick.GetPersistentEventCount() > 0)
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(cloneBtn.onClick, 0);
                }

                var tmp = clone.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = $"<b>{label}</b>";
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = Color.white;
                }
                return cloneBtn;
            }

            return CreateActionButton(parent, name, label, pos, size);
        }

        private static Button CreateNavButton(Transform parent, string name, string iconPath, string fallbackLabel, Vector2 pos, Vector2 size)
        {
            string[] searchTerms = name.ToLower().Contains("prev") || name.ToLower().Contains("back")
                ? new string[] { "PrevButton", "BackButton", "Back_Button", "Btn_Back", "Btn_Prev", "PrevCardBtn", "Prev_Button" }
                : new string[] { "NextButton", "Next_Button", "Btn_Next", "NextCardBtn", "NextActivity_Button", "Btn_NextActivity" };

            Button template = FindTemplateButton(searchTerms);
            if (template != null)
            {
                GameObject clone = Object.Instantiate(template.gameObject, parent, false);
                clone.name = name;
                RectTransform cRt = clone.GetComponent<RectTransform>();
                cRt.anchoredPosition = pos;
                if (size != Vector2.zero) cRt.sizeDelta = size;

                Button cloneBtn = clone.GetComponent<Button>();
                if (cloneBtn != null)
                {
                    while (cloneBtn.onClick.GetPersistentEventCount() > 0)
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(cloneBtn.onClick, 0);
                }

                var tmp = clone.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null && !string.IsNullOrEmpty(fallbackLabel))
                {
                    tmp.text = $"<b>{fallbackLabel}</b>";
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = Color.white;
                }
                return cloneBtn;
            }

            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            Sprite iconSpr = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath) ?? LoadSpriteMatching(fallbackLabel);
            if (iconSpr != null)
            {
                img.sprite = iconSpr;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                img.color = Color.white;
                TextMeshProUGUI tmp = CreateText(go.transform, "Text", $"<b>{fallbackLabel}</b>", 24);
                tmp.color = Color.white;
                tmp.fontStyle = FontStyles.Bold;
            }

            return go.GetComponent<Button>();
        }

        private static Button CreateReplayButton(Transform parent, Vector2 pos)
        {
            Button template = FindTemplateButton("ReplayAudioButton", "Btn_ReplayAudio", "AudioButton", "SpeakerButton", "Audio_Button", "Speaker_Button", "Btn_Speaker");
            if (template != null)
            {
                GameObject clone = Object.Instantiate(template.gameObject, parent, false);
                clone.name = "ReplayAudioButton";
                RectTransform cRt = clone.GetComponent<RectTransform>();
                cRt.anchoredPosition = pos;

                Button cloneBtn = clone.GetComponent<Button>();
                if (cloneBtn != null)
                {
                    while (cloneBtn.onClick.GetPersistentEventCount() > 0)
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(cloneBtn.onClick, 0);
                }
                return cloneBtn;
            }

            GameObject go = new GameObject("ReplayAudioButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(85f, 85f);

            Image img = go.GetComponent<Image>();
            Sprite speakerSpr = LoadSpriteMatching("speaker")
                             ?? LoadSpriteMatching("audio")
                             ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Icons/audio icon.png")
                             ?? AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Icons/volume.png");
            if (speakerSpr != null)
            {
                img.sprite = speakerSpr;
                img.preserveAspect = true;
                img.color = Color.white;
            }
            else
            {
                img.color = Color.white;
                TextMeshProUGUI tmp = CreateText(go.transform, "SpeakerIcon", "LISTEN", 18);
                tmp.color = Color.white;
                tmp.fontStyle = FontStyles.Bold;
            }
            return go.GetComponent<Button>();
        }

        private static GameObject CreateSwipeIndicator(Transform parent, string name, string label, Vector2 pos)
        {
            GameObject go = CreateContainer(parent, name, pos, new Vector2(320f, 140f));
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.2f);
            img.preserveAspect = true;
            TextMeshProUGUI tmp = CreateText(go.transform, "Label", $"<b>{label}</b>", 24);
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            return go;
        }

        private static RectTransform CreateDropBin(Transform parent, string name, string label, Vector2 pos)
        {
            GameObject go = CreateContainer(parent, name, pos, new Vector2(460f, 240f));
            Image img = go.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.18f);
            img.preserveAspect = true;
            TextMeshProUGUI tmp = CreateText(go.transform, "BinLabel", $"<b>{label}</b>", 22);
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            return go.GetComponent<RectTransform>();
        }

        private static void CreateHUD(Transform parent, out TextMeshProUGUI scoreTxt, out TextMeshProUGUI streakTxt, out Slider pBar, out TextMeshProUGUI pText)
        {
            CreateProgressHUD(parent, out pBar, out pText);
            scoreTxt = CreateText(parent, "Score_Text", "<b>Score: 0</b>", 28);
            scoreTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(700f, 440f);
            scoreTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 50f);
            scoreTxt.color = Color.white;
            scoreTxt.fontStyle = FontStyles.Bold;

            streakTxt = CreateText(parent, "Streak_Text", "", 26);
            streakTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(700f, 390f);
            streakTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(300f, 50f);
            streakTxt.color = new Color(1f, 0.92f, 0.35f, 1f);
            streakTxt.fontStyle = FontStyles.Bold;
        }

        private static void CreateProgressHUD(Transform parent, out Slider pBar, out TextMeshProUGUI pText)
        {
            GameObject barObj = CreateContainer(parent, "ProgressBar", new Vector2(0f, 470f), new Vector2(650f, 24f));
            pBar = barObj.AddComponent<Slider>();
            U6_SA_UnitFlowManager_Masters_Phonics.StyleProgressBarBlue(pBar);

            pText = CreateText(parent, "Progress_Text", "<b>Item 1 of 12</b>", 22);
            pText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 440f);
            pText.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 40f);
            pText.color = Color.white;
            pText.fontStyle = FontStyles.Bold;
        }

        // =========================================================================
        // Sprite & Audio Loaders (Cached & Validated)
        // =========================================================================

        private static Dictionary<string, Sprite> spriteCache = null;
        private static Dictionary<string, AudioClip> audioCache = null;
        private static List<KeyValuePair<string, AudioClip>> audioList = null;

        private static string[] FilterExistingFolders(string[] candidateFolders)
        {
            var valid = new List<string>();
            foreach (var folder in candidateFolders)
            {
                if (string.IsNullOrEmpty(folder)) continue;
                if (AssetDatabase.IsValidFolder(folder) || Directory.Exists(folder))
                {
                    valid.Add(folder);
                }
            }
            return valid.ToArray();
        }

        private static void BuildSpriteCache()
        {
            spriteCache = new Dictionary<string, Sprite>(System.StringComparer.OrdinalIgnoreCase);
            string[] rawPaths = new string[] { "Assets/Art/unit6_MP", "Assets/Art", "Assets/Icons" };
            string[] validPaths = FilterExistingFolders(rawPaths);

            if (validPaths.Length == 0) return;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", validPaths);

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var sub in subAssets)
                {
                    if (sub is Sprite spr)
                    {
                        string normName = NormalizeString(spr.name);
                        if (!spriteCache.ContainsKey(normName))
                        {
                            spriteCache[normName] = spr;
                        }
                        string shortName = normName.Replace("u6pic", "").Replace("u6ui", "").Replace("u6badge", "").Trim();
                        if (!string.IsNullOrEmpty(shortName) && !spriteCache.ContainsKey(shortName))
                        {
                            spriteCache[shortName] = spr;
                        }
                    }
                }
            }
        }

        public static Sprite LoadSpriteMatching(string keyword)
        {
            if (string.IsNullOrEmpty(keyword)) return null;
            if (spriteCache == null) BuildSpriteCache();

            string norm = NormalizeString(keyword);
            if (spriteCache.TryGetValue(norm, out Sprite exact)) return exact;

            string shortKeyword = norm.Replace("u6pic", "").Replace("u6ui", "").Replace("u6badge", "").Trim();
            if (!string.IsNullOrEmpty(shortKeyword) && spriteCache.TryGetValue(shortKeyword, out Sprite shortExact)) return shortExact;

            foreach (var kvp in spriteCache)
            {
                if (kvp.Key.Contains(norm) || norm.Contains(kvp.Key) || (!string.IsNullOrEmpty(shortKeyword) && kvp.Key.Contains(shortKeyword)))
                    return kvp.Value;
            }
            return null;
        }

        private static void BuildAudioCache()
        {
            audioCache = new Dictionary<string, AudioClip>(System.StringComparer.OrdinalIgnoreCase);
            audioList = new List<KeyValuePair<string, AudioClip>>();

            string[] rawPaths = new string[] 
            { 
                AUDIO_BASE_PATH, 
                $"{AUDIO_BASE_PATH}/U6_voiceA", 
                $"{AUDIO_BASE_PATH}/U6_voiceB", 
                "Assets/SFX",
                "Assets/Resources"
            };

            string[] validPaths = FilterExistingFolders(rawPaths);
            if (validPaths.Length == 0) return;

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", validPaths);
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string filename = Path.GetFileNameWithoutExtension(path);
                string normName = NormalizeString(filename);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                {
                    if (!audioCache.ContainsKey(normName))
                    {
                        audioCache[normName] = clip;
                    }
                    audioList.Add(new KeyValuePair<string, AudioClip>(normName, clip));
                }
            }
        }

        public static AudioClip LoadAudioMatching(string keyword, string preferredSubdir = "")
        {
            if (string.IsNullOrEmpty(keyword)) return null;
            if (audioCache == null) BuildAudioCache();

            string cleanKeyword = NormalizeString(keyword);

            // 1. Exact match
            if (audioCache.TryGetValue(cleanKeyword, out AudioClip exactClip))
            {
                return exactClip;
            }

            // 2. Substring match
            foreach (var kvp in audioList)
            {
                if (kvp.Key.Contains(cleanKeyword) || cleanKeyword.Contains(kvp.Key))
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        private static string NormalizeString(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (char c in input.ToLower())
            {
                if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                    sb.Append(c);
            }
            return sb.ToString().Trim();
        }

        private static void SetProp(SerializedObject so, string propName, Object value)
        {
            if (so == null || string.IsNullOrEmpty(propName)) return;
            var prop = so.FindProperty(propName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
            }
        }

        private static void UpdateSignboardTitles(Transform selPanels)
        {
            var title = selPanels.Find("Title_Text") ?? selPanels.Find("TitleText");
            if (title != null)
            {
                var tmp = title.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = "<b>UNIT 6: MAGIC 'E' / BOSSY 'E'</b>";
                    tmp.color = Color.white;
                    tmp.fontStyle = FontStyles.Bold;
                }
            }
        }

        private static void WireFlowManager(U6_SA_UnitFlowManager_Masters_Phonics flow, Transform selPanel, Transform secParent,
            GameObject learn, GameObject act1, GameObject act2, GameObject act3, GameObject act4, GameObject map, GameObject challenge, GameObject comp)
        {
            Transform unitRoot = flow.transform;

            Transform backBtnT = unitRoot.Find("TopBar/Back_Button") 
                              ?? unitRoot.Find("BackButton") 
                              ?? unitRoot.Find("Back_Button")
                              ?? (secParent != null ? secParent.Find("Back_Button") : null)
                              ?? (selPanel != null ? selPanel.Find("Back_Button") : null);

            Transform nextBtnT = unitRoot.Find("NextActivity_Button") 
                              ?? unitRoot.Find("NextButton") 
                              ?? unitRoot.Find("Btn_NextActivity")
                              ?? unitRoot.Find("Next_Button");

            Transform contBtnT = comp != null ? (comp.transform.Find("ContinueToSignboardButton") ?? comp.transform.Find("ContinueButton") ?? comp.transform.Find("Btn_Continue")) : null;
            Transform nextUnitBtnT = comp != null ? (comp.transform.Find("NextUnitButton") ?? comp.transform.Find("Btn_NextUnit") ?? comp.transform.Find("NextButton")) : null;

            SerializedObject so = new SerializedObject(flow);
            SetProp(so, "sectionSelectionPanel", selPanel != null ? selPanel.gameObject : null);
            SetProp(so, "sectionsParentContainer", secParent != null ? secParent.gameObject : null);
            SetProp(so, "learnPanel", learn);
            SetProp(so, "activity1Panel", act1);
            SetProp(so, "activity2Panel", act2);
            SetProp(so, "activity3Panel", act3);
            SetProp(so, "activity4Panel", act4);
            SetProp(so, "mapPanel", map);
            SetProp(so, "challengePanel", challenge);
            SetProp(so, "completionPanel", comp);

            if (backBtnT != null) SetProp(so, "globalBackButton", backBtnT.GetComponent<Button>());
            if (nextBtnT != null) SetProp(so, "nextActivityButton", nextBtnT.GetComponent<Button>());
            if (contBtnT != null) SetProp(so, "continueToSignboardButton", contBtnT.GetComponent<Button>());
            if (nextUnitBtnT != null) SetProp(so, "nextUnitButton", nextUnitBtnT.GetComponent<Button>());

            so.ApplyModifiedProperties();
        }
    }
}
