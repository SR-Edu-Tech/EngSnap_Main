using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics.EditorTools
{
    public class U5_HierarchyAutomator : EditorWindow
    {
        private const string SPRITESHEET_PATH = "Assets/Art/unit5_MP/U5 all sprites MP.png";
        private const string AUDIO_BASE_PATH = "Assets/Audio/U5_audio";

        [MenuItem("Masters Phonics/Unit 5/Generate Unit 5 Hierarchy (Clean & Complete)", false, 100)]
        public static void GenerateUnit5Hierarchy()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in the current Scene!", "OK");
                return;
            }

            Transform sourceUnit = canvas.transform.Find("Unit_4") ?? canvas.transform.Find("Unit_3");
            if (sourceUnit == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find 'Unit_4' or 'Unit_3' under Canvas as a base template!", "OK");
                return;
            }

            // Check if Unit_5 already exists
            Transform existingU5 = canvas.transform.Find("Unit_5");
            if (existingU5 != null)
            {
                bool overwrite = EditorUtility.DisplayDialog("Unit_5 Exists", "Unit_5 already exists in Canvas. Do you want to replace it with a clean Unit 5 hierarchy?", "Yes, Replace", "Cancel");
                if (!overwrite) return;
                Undo.DestroyObjectImmediate(existingU5.gameObject);
            }

            // 1. Duplicate Template Root
            GameObject unit5Obj = Object.Instantiate(sourceUnit.gameObject, canvas.transform);
            unit5Obj.name = "Unit_5";
            Undo.RegisterCreatedObjectUndo(unit5Obj, "Generate Clean Unit_5");

            // Strip Old Root Components
            StripOldUnitComponents(unit5Obj);

            // Add Unit 5 Managers
            var flowManager = unit5Obj.AddComponent<U5_SA_UnitFlowManager_Masters_Phonics>();
            var audioManager = unit5Obj.AddComponent<U5_SA_AudioManager_Masters_Phonics>();

            // Assign Root Audio Manager & Flow Manager Clips
            AssignRootAudioClips(audioManager, flowManager);

            // Setup Section Selection Panels (Signboard)
            Transform selPanels = unit5Obj.transform.Find("Unit_4_Section_Selection_Panels") ?? unit5Obj.transform.Find("Unit_3_Section_Selection_Panels") ?? unit5Obj.transform.Find("Unit_5_Section_Selection_Panels");
            if (selPanels != null)
            {
                selPanels.name = "Unit_5_Section_Selection_Panels";
                UpdateSignboardTitles(selPanels);
            }

            // Setup Sections Parent
            Transform sectionsParent = unit5Obj.transform.Find("Unit_4_Sections") ?? unit5Obj.transform.Find("Unit_3_Sections") ?? unit5Obj.transform.Find("Unit_5_Sections");
            if (sectionsParent == null)
            {
                GameObject secGo = new GameObject("Unit_5_Sections", typeof(RectTransform));
                secGo.transform.SetParent(unit5Obj.transform, false);
                sectionsParent = secGo.transform;
            }
            sectionsParent.name = "Unit_5_Sections";

            // Clean & Build dedicated Panels for each Unit 5 section with Audio Wired
            GameObject learnGo = BuildOrCleanPanel(sectionsParent, "U5_learn_Concept_Cards_Panel");
            SetupGM01ConceptCards(learnGo);

            GameObject act1Go = BuildOrCleanPanel(sectionsParent, "U5_Activity_1_Door_Or_Gate");
            SetupGM03sDoorOrGate(act1Go);

            GameObject act2Go = BuildOrCleanPanel(sectionsParent, "U5_Activity_2_Long_Or_Short");
            SetupGM04LongOrShort(act2Go);

            GameObject act3Go = BuildOrCleanPanel(sectionsParent, "U5_Activity_3_The_First_Door");
            SetupGM03TheFirstDoor(act3Go);

            GameObject act4Go = BuildOrCleanPanel(sectionsParent, "U5_Activity_4_Closed_Word_Hunt");
            SetupGM07ClosedWordHunt(act4Go);

            GameObject act5Go = BuildOrCleanPanel(sectionsParent, "U5_Activity_5_Big_Word_Reader");
            SetupGM05BigWordReader(act5Go);

            GameObject challengeGo = BuildOrCleanPanel(sectionsParent, "U5_UnitChallenge_Panel");
            SetupUnitChallenge(challengeGo);

            GameObject compGo = BuildOrCleanPanel(sectionsParent, "U5_COMPLETE_Panel");
            SetupCompletionPanel(compGo);

            // Wire Flow Manager
            WireFlowManager(flowManager, selPanels, sectionsParent, learnGo, act1Go, act2Go, act3Go, act4Go, act5Go, challengeGo, compGo);

            EditorUtility.SetDirty(unit5Obj);
            Selection.activeGameObject = unit5Obj;

            EditorUtility.DisplayDialog("Unit 5 Hierarchy Created!", 
                "Successfully generated clean Unit 5 GameObject hierarchies and assigned all Voice A, Voice B, and SFX audio clips to Inspector fields!", 
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

                // Remove placeholder background image if it has no sprite
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

        // =========================================================================
        // Root Audio & Flow Manager Assignment
        // =========================================================================

        private static void AssignRootAudioClips(U5_SA_AudioManager_Masters_Phonics audioMgr, U5_SA_UnitFlowManager_Masters_Phonics flowMgr)
        {
            audioMgr.doorSlamClip = LoadAudioMatching("U05_SFX_door_slam", "SFX") ?? LoadAudioMatching("door_slam", "SFX");
            audioMgr.gateOpenClip = LoadAudioMatching("U05_SFX_gate_open", "SFX") ?? LoadAudioMatching("gate_open", "SFX");
            audioMgr.vowelStretchClip = LoadAudioMatching("U05_SFX_vowel_stretch", "SFX") ?? LoadAudioMatching("vowel_stretch", "SFX");
            audioMgr.vowelSnapClip = LoadAudioMatching("U05_SFX_vowel_snap", "SFX") ?? LoadAudioMatching("vowel_snap", "SFX");
            audioMgr.gridSelectClip = LoadAudioMatching("U05_SFX_grid_select", "SFX") ?? LoadAudioMatching("grid_select", "SFX");
            audioMgr.gridFoundClip = LoadAudioMatching("U05_SFX_grid_found", "SFX") ?? LoadAudioMatching("grid_found", "SFX");
            audioMgr.chunkSnapClip = LoadAudioMatching("U05_SFX_chunk_snap", "SFX") ?? LoadAudioMatching("chunk_snap", "SFX");
            audioMgr.correctClip = LoadAudioMatching("Correct Answer 1", "SFX") ?? LoadAudioMatching("Correct", "SFX");
            audioMgr.wrongClip = LoadAudioMatching("Incorrect", "SFX") ?? LoadAudioMatching("life_lost", "SFX") ?? LoadAudioMatching("SelectNegative", "SFX");
            audioMgr.celebrationClip = LoadAudioMatching("celebration") ?? LoadAudioMatching("complete");

            flowMgr.unitIntroClip = LoadAudioMatching("Unit Five Two of the seven types", "voice A");
            flowMgr.unitCompleteVoiceClip = LoadAudioMatching("Unit Five done Thirtyfour doors opened", "voice A");
            flowMgr.mapUpdateVoiceClip = LoadAudioMatching("Two of the seven types done Five to", "voice A");
        }

        // =========================================================================
        // Panel Builders & Audio Inspectors
        // =========================================================================

        // 1. LEARN: Concept Cards (GM-01)
        private static void SetupGM01ConceptCards(GameObject panel)
        {
            var gm01 = panel.AddComponent<U5_SA_GM01_ConceptCards_Masters_Phonics>();

            gm01.card1VoiceClip = LoadAudioMatching("Watch Go The gates open", "voice A") ?? LoadAudioMatching("Open on the left", "voice A");
            gm01.card2VoiceClip = LoadAudioMatching("Take the t away and the gate swings", "voice A") ?? LoadAudioMatching("Last letter of me is an e", "voice A");
            gm01.card3VoiceClip = LoadAudioMatching("Open on the left closed on the right", "voice A") ?? LoadAudioMatching("Now bed Last letter is a d", "voice A");
            gm01.card4VoiceClip = LoadAudioMatching("Heres the useful bit In a long word", "voice A") ?? LoadAudioMatching("Longer words now Find the first vowel", "voice A");

            CreateHeader(panel.transform, "CONCEPT CARDS", "Open & Closed Syllables: Learn the Door Metaphor", out TextMeshProUGUI titleTMP, out TextMeshProUGUI descTMP);
            
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));
            GameObject cardContainer = CreateContainer(panel.transform, "CardContainer", new Vector2(0, -30f), new Vector2(1200f, 620f));

            Button prevBtn = CreateActionButton(panel.transform, "PrevButton", "PREV", new Vector2(-480f, -440f), new Vector2(220f, 70f));
            Button nextBtn = CreateActionButton(panel.transform, "NextButton", "NEXT", new Vector2(480f, -440f), new Vector2(220f, 70f));

            CreateProgressHUD(panel.transform, out Slider pBar, out TextMeshProUGUI pText);

            SerializedObject so = new SerializedObject(gm01);
            SetProp(so, "titleTMP", titleTMP);
            SetProp(so, "descriptionTMP", descTMP);
            SetProp(so, "dynamicCardContainer", cardContainer.transform);
            SetProp(so, "nextCardButton", nextBtn);
            SetProp(so, "replayVoiceAButton", replayBtn);
            SetProp(so, "cardRoundedSprite", LoadSlicedSprite("U5_Sprite_Card_Swipe_Normal"));
            so.ApplyModifiedProperties();
        }

        // 2. ACTIVITY 1: Door or Gate (GM-03s Swipe)
        private static void SetupGM03sDoorOrGate(GameObject panel)
        {
            var gm03s = panel.AddComponent<U5_SA_GM03s_DoorOrGate_Masters_Phonics>();

            gm03s.introInstructionClip = LoadAudioMatching("Quick round Swipe left if the gates open", "voice A");
            gm03s.yVowelAsideClip = LoadAudioMatching("Careful that y is acting as a vowel", "voice A");
            gm03s.correctSFX = LoadAudioMatching("Correct Answer 1", "SFX") ?? LoadAudioMatching("Correct", "SFX");
            gm03s.wrongSFX = LoadAudioMatching("Incorrect", "SFX") ?? LoadAudioMatching("life_lost", "SFX") ?? LoadAudioMatching("SelectNegative", "SFX");
            gm03s.gateOpenSFX = LoadAudioMatching("U05_SFX_gate_open", "SFX") ?? LoadAudioMatching("gate_open", "SFX");
            gm03s.doorSlamSFX = LoadAudioMatching("U05_SFX_door_slam", "SFX") ?? LoadAudioMatching("door_slam", "SFX");

            CreateHeader(panel.transform, "ACTIVITY 1: DOOR OR GATE", "Swipe LEFT for Open Gate (Long Vowel) | Swipe RIGHT for Closed Door (Short Vowel)", out TextMeshProUGUI titleTMP, out TextMeshProUGUI promptTMP);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));
            GameObject openGateInd = CreateSwipeIndicator(panel.transform, "OpenGateIndicator", "<< OPEN GATE (Long Vowel)", new Vector2(-540f, 0f));
            GameObject closedDoorInd = CreateSwipeIndicator(panel.transform, "ClosedDoorIndicator", "CLOSED DOOR (Short Vowel) >>", new Vector2(540f, 0f));

            GameObject cardSpawnAnchor = CreateContainer(panel.transform, "CardSpawnAnchor", Vector2.zero, new Vector2(480f, 320f));

            CreateHUD(panel.transform, out TextMeshProUGUI scoreTxt, out TextMeshProUGUI streakTxt, out Slider pBar, out TextMeshProUGUI pText);

            SerializedObject so = new SerializedObject(gm03s);
            SetProp(so, "titleTMP", titleTMP);
            SetProp(so, "promptTMP", promptTMP);
            SetProp(so, "scoreTMP", scoreTxt);
            SetProp(so, "streakTMP", streakTxt);
            SetProp(so, "progressTMP", pText);
            SetProp(so, "progressBar", pBar);
            SetProp(so, "replayAudioButton", replayBtn);
            SetProp(so, "cardSpawnAnchor", cardSpawnAnchor.GetComponent<RectTransform>());
            SetProp(so, "openGateBin", openGateInd.GetComponent<RectTransform>());
            SetProp(so, "closedDoorBin", closedDoorInd.GetComponent<RectTransform>());
            SetProp(so, "correctSFX", gm03s.correctSFX);
            SetProp(so, "wrongSFX", gm03s.wrongSFX);
            SetProp(so, "gateOpenSFX", gm03s.gateOpenSFX);
            SetProp(so, "doorSlamSFX", gm03s.doorSlamSFX);
            so.ApplyModifiedProperties();
        }

        // 3. ACTIVITY 2: Long or Short? (GM-04 Minimal Pairs)
        private static void SetupGM04LongOrShort(GameObject panel)
        {
            var gm04 = panel.AddComponent<U5_SA_GM04_LongOrShort_Masters_Phonics>();

            gm04.introInstructionClip = LoadAudioMatching("Ears only this time No word on screen", "voice A");
            gm04.pairContrastClip = LoadAudioMatching("Listen to both Hear the door close", "voice A");
            gm04.correctSFX = LoadAudioMatching("Correct Answer 1", "SFX") ?? LoadAudioMatching("Correct", "SFX");
            gm04.wrongSFX = LoadAudioMatching("Incorrect", "SFX") ?? LoadAudioMatching("life_lost", "SFX") ?? LoadAudioMatching("SelectNegative", "SFX");
            gm04.vowelStretchSFX = LoadAudioMatching("U05_SFX_vowel_stretch", "SFX") ?? LoadAudioMatching("vowel_stretch", "SFX");
            gm04.vowelSnapSFX = LoadAudioMatching("U05_SFX_vowel_snap", "SFX") ?? LoadAudioMatching("vowel_snap", "SFX");

            CreateHeader(panel.transform, "ACTIVITY 2: LONG OR SHORT?", "Listen to the word! Tap the vowel sound you hear.", out TextMeshProUGUI titleTMP, out TextMeshProUGUI promptTMP);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(0f, 200f));

            GameObject optionsContainer = CreateContainer(panel.transform, "VowelOptionsContainer", new Vector2(0f, -50f), new Vector2(1000f, 200f));
            var glg = optionsContainer.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(230f, 100f);
            glg.spacing = new Vector2(20f, 20f);
            glg.childAlignment = TextAnchor.MiddleCenter;

            Button[] optionBtns = new Button[4];
            Sprite vowelBtnSprite = LoadSlicedSprite("U5_Sprite_Vowel_Pill_Btn");
            for (int i = 0; i < 4; i++)
            {
                optionBtns[i] = CreateActionButton(optionsContainer.transform, $"Option_{i}", $"Option {i+1}", Vector2.zero, new Vector2(230f, 100f));
                if (vowelBtnSprite != null)
                {
                    Image btnImg = optionBtns[i].GetComponent<Image>();
                    if (btnImg != null) btnImg.sprite = vowelBtnSprite;
                }
            }

            GameObject wordRevealObj = CreateContainer(panel.transform, "RevealedWordContainer", new Vector2(0f, -320f), new Vector2(900f, 160f));
            TextMeshProUGUI revealedWordTxt = CreateText(wordRevealObj.transform, "RevealedWordText", "", 48);

            CreateHUD(panel.transform, out TextMeshProUGUI scoreTxt, out _, out Slider pBar, out TextMeshProUGUI pText);

            SerializedObject so = new SerializedObject(gm04);
            SetProp(so, "titleTMP", titleTMP);
            SetProp(so, "promptTMP", promptTMP);
            SetProp(so, "scoreTMP", scoreTxt);
            SetProp(so, "progressTMP", pText);
            SetProp(so, "progressBar", pBar);
            SetProp(so, "replayAudioButton", replayBtn);
            SetProp(so, "optionBtn1", optionBtns[0]);
            SetProp(so, "optionBtn2", optionBtns[1]);
            SetProp(so, "optionBtn3", optionBtns[2]);
            SetProp(so, "optionBtn4", optionBtns[3]);
            SetProp(so, "revealedWordTMP", revealedWordTxt);
            SetProp(so, "correctSFX", gm04.correctSFX);
            SetProp(so, "wrongSFX", gm04.wrongSFX);
            SetProp(so, "vowelStretchSFX", gm04.vowelStretchSFX);
            SetProp(so, "vowelSnapSFX", gm04.vowelSnapSFX);
            so.ApplyModifiedProperties();
        }

        // 4. ACTIVITY 3: The First Door (GM-03 2-Bin Drag Sort)
        private static void SetupGM03TheFirstDoor(GameObject panel)
        {
            var gm03 = panel.AddComponent<U5_SA_GM03_TheFirstDoor_Masters_Phonics>();

            gm03.correctSFX = LoadAudioMatching("Correct Answer 1", "SFX") ?? LoadAudioMatching("Correct", "SFX");
            gm03.wrongSFX = LoadAudioMatching("Incorrect", "SFX") ?? LoadAudioMatching("life_lost", "SFX") ?? LoadAudioMatching("SelectNegative", "SFX");
            gm03.gateOpenSFX = LoadAudioMatching("U05_SFX_gate_open", "SFX") ?? LoadAudioMatching("gate_open", "SFX");
            gm03.doorSlamSFX = LoadAudioMatching("U05_SFX_door_slam", "SFX") ?? LoadAudioMatching("door_slam", "SFX");

            CreateHeader(panel.transform, "ACTIVITY 3: THE FIRST DOOR", "Look at the first syllable! Drag card to Open or Closed bin.", out _, out _);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            RectTransform openBin = CreateDropBin(panel.transform, "OpenBin", "OPEN FIRST SYLLABLE\n(Gate Open)", new Vector2(-460f, -120f));
            RectTransform closedBin = CreateDropBin(panel.transform, "ClosedBin", "CLOSED FIRST SYLLABLE\n(Door Closed)", new Vector2(460f, -120f));

            Sprite openBinSprite = LoadSlicedSprite("U5_Sprite_Bin_Open");
            if (openBinSprite != null) openBin.GetComponent<Image>().sprite = openBinSprite;

            Sprite closedBinSprite = LoadSlicedSprite("U5_Sprite_Bin_Closed");
            if (closedBinSprite != null) closedBin.GetComponent<Image>().sprite = closedBinSprite;

            GameObject wordCard = CreateSwipeCard(panel.transform, "DraggableWordCard", out RectTransform cardRt, out CanvasGroup cg, out TextMeshProUGUI wordTxt, out TextMeshProUGUI ruleTxt, out Image bg, out Image doorIcon);

            CreateHUD(panel.transform, out TextMeshProUGUI scoreTxt, out TextMeshProUGUI streakTxt, out Slider pBar, out TextMeshProUGUI pText);

            SerializedObject so = new SerializedObject(gm03);
            SetProp(so, "wordCard", wordCard);
            SetProp(so, "cardRect", cardRt);
            SetProp(so, "cardCanvasGroup", cg);
            SetProp(so, "wordText", wordTxt);
            SetProp(so, "ruleNoteText", ruleTxt);
            SetProp(so, "cardBackground", bg);
            SetProp(so, "cardDoorIcon", doorIcon);
            SetProp(so, "openBinRect", openBin);
            SetProp(so, "closedBinRect", closedBin);
            SetProp(so, "replayAudioBtn", replayBtn);
            SetProp(so, "progressText", pText);
            SetProp(so, "progressBar", pBar);
            SetProp(so, "scoreText", scoreTxt);
            SetProp(so, "streakText", streakTxt);
            SetProp(so, "correctSFX", gm03.correctSFX);
            SetProp(so, "wrongSFX", gm03.wrongSFX);
            SetProp(so, "gateOpenSFX", gm03.gateOpenSFX);
            SetProp(so, "doorSlamSFX", gm03.doorSlamSFX);

            SetProp(so, "openDoorSprite", LoadSlicedSprite("U5_Sprite_Gate_Open"));
            SetProp(so, "closedDoorSprite", LoadSlicedSprite("U5_Sprite_Door_Closed"));
            SetProp(so, "normalCardSprite", LoadSlicedSprite("U5_Sprite_Card_Swipe_Normal"));
            SetProp(so, "correctCardSprite", LoadSlicedSprite("U5_Sprite_Card_Swipe_Correct"));
            SetProp(so, "incorrectCardSprite", LoadSlicedSprite("U5_Sprite_Card_Swipe_Incorrect"));

            // Wire Items Audio in allItems list
            SerializedProperty allItemsProp = so.FindProperty("allItems");
            if (allItemsProp != null)
            {
                for (int i = 0; i < allItemsProp.arraySize; i++)
                {
                    SerializedProperty itemProp = allItemsProp.GetArrayElementAtIndex(i);
                    string wordName = itemProp.FindPropertyRelative("word").stringValue;
                    AudioClip clip = LoadAudioMatching(wordName, "voice B");
                    if (clip != null)
                    {
                        itemProp.FindPropertyRelative("wordAudio").objectReferenceValue = clip;
                    }
                }
            }

            so.ApplyModifiedProperties();
        }

        // 5. ACTIVITY 4: Closed-Word Hunt (GM-07 Word Grid)
        private static void SetupGM07ClosedWordHunt(GameObject panel)
        {
            var gm07 = panel.AddComponent<U5_SA_GM07_ClosedWordHunt_Masters_Phonics>();

            CreateHeader(panel.transform, "ACTIVITY 4: CLOSED-WORD HUNT", "Find all 10 closed-syllable words in the letter grid!", out _, out _);

            GameObject gridObj = CreateContainer(panel.transform, "LetterGridContainer", new Vector2(-280f, -40f), new Vector2(780f, 650f));
            var glg = gridObj.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(65f, 60f);
            glg.spacing = new Vector2(5f, 5f);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 11;

            GameObject bankObj = CreateContainer(panel.transform, "WordBankContainer", new Vector2(460f, -20f), new Vector2(420f, 580f));
            var bankGlg = bankObj.AddComponent<GridLayoutGroup>();
            bankGlg.cellSize = new Vector2(180f, 50f);
            bankGlg.spacing = new Vector2(15f, 15f);
            bankGlg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            bankGlg.constraintCount = 2;

            GameObject mascotBubble = CreateContainer(panel.transform, "MascotBubble", new Vector2(460f, 320f), new Vector2(420f, 100f));
            TextMeshProUGUI mascotTxt = CreateText(mascotBubble.transform, "MascotText", "Find all 10 closed words!", 22);
            Button hintBtn = CreateActionButton(panel.transform, "HintButton", "HINT", new Vector2(460f, -380f), new Vector2(220f, 60f));

            CreateHUD(panel.transform, out TextMeshProUGUI scoreTxt, out _, out Slider pBar, out TextMeshProUGUI pText);
            CreateFeedbackPanel(panel.transform, out GameObject fbPanel, out TextMeshProUGUI fbText);

            SerializedObject so = new SerializedObject(gm07);
            SetProp(so, "gridContainer", gridObj.GetComponent<RectTransform>());
            SetProp(so, "gridLayout", glg);
            SetProp(so, "wordBankContainer", bankObj.transform);
            SetProp(so, "foundCountText", pText);
            SetProp(so, "progressBar", pBar);
            SetProp(so, "scoreText", scoreTxt);
            SetProp(so, "mascotBubble", mascotBubble);
            SetProp(so, "mascotCommentaryText", mascotTxt);
            SetProp(so, "hintButton", hintBtn);
            SetProp(so, "feedbackPanel", fbPanel);
            SetProp(so, "feedbackText", fbText);
            so.ApplyModifiedProperties();
        }

        // 6. ACTIVITY 5: Big Word Reader (GM-05 Syllable Builder)
        private static void SetupGM05BigWordReader(GameObject panel)
        {
            var gm05 = panel.AddComponent<U5_SA_GM05_BigWordReader_Masters_Phonics>();

            CreateHeader(panel.transform, "ACTIVITY 5: BIG WORD READER", "Assemble syllable tiles into multisyllable words!", out TextMeshProUGUI roundTitle, out _);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            GameObject slotContainer = CreateContainer(panel.transform, "TargetSlotContainer", new Vector2(0f, 100f), new Vector2(950f, 140f));
            var slotHlg = slotContainer.AddComponent<HorizontalLayoutGroup>();
            slotHlg.spacing = 24f;
            slotHlg.childAlignment = TextAnchor.MiddleCenter;

            GameObject bankContainer = CreateContainer(panel.transform, "BankTileContainer", new Vector2(0f, -80f), new Vector2(950f, 140f));
            var bankHlg = bankContainer.AddComponent<HorizontalLayoutGroup>();
            bankHlg.spacing = 24f;
            bankHlg.childAlignment = TextAnchor.MiddleCenter;

            GameObject showcasePanel = CreateContainer(panel.transform, "ShowcaseAnalysisPanel", new Vector2(0f, -280f), new Vector2(900f, 150f));
            showcasePanel.SetActive(false);

            Button clearBtn = CreateActionButton(panel.transform, "ClearButton", "RESET TILES", new Vector2(0f, -420f), new Vector2(240f, 65f));

            CreateHUD(panel.transform, out TextMeshProUGUI scoreTxt, out _, out Slider pBar, out TextMeshProUGUI pText);
            CreateFeedbackPanel(panel.transform, out GameObject fbPanel, out TextMeshProUGUI fbText);

            SerializedObject so = new SerializedObject(gm05);
            SetProp(so, "slotContainer", slotContainer.transform);
            SetProp(so, "bankContainer", bankContainer.transform);
            SetProp(so, "showcasePanel", showcasePanel);
            SetProp(so, "roundTitleText", roundTitle);
            SetProp(so, "progressText", pText);
            SetProp(so, "progressBar", pBar);
            SetProp(so, "scoreText", scoreTxt);
            SetProp(so, "replayWholeWordBtn", replayBtn);
            SetProp(so, "clearBtn", clearBtn);
            SetProp(so, "feedbackPanel", fbPanel);
            SetProp(so, "feedbackText", fbText);

            SetProp(so, "openDoorSprite", LoadSlicedSprite("U5_Sprite_Gate_Open"));
            SetProp(so, "closedDoorSprite", LoadSlicedSprite("U5_Sprite_Door_Closed"));
            SetProp(so, "defaultTileSprite", LoadSlicedSprite("U5_Sprite_Syllable_Tile"));
            SetProp(so, "placedTileSprite", LoadSlicedSprite("U5_Sprite_Syllable_Slot"));

            // Wire allWords audio clips
            SerializedProperty allWordsProp = so.FindProperty("allWords");
            if (allWordsProp != null)
            {
                for (int i = 0; i < allWordsProp.arraySize; i++)
                {
                    SerializedProperty wordProp = allWordsProp.GetArrayElementAtIndex(i);
                    string word = wordProp.FindPropertyRelative("word").stringValue;
                    AudioClip wholeClip = LoadAudioMatching(word, "voice B");
                    if (wholeClip != null)
                    {
                        wordProp.FindPropertyRelative("wholeWordAudio").objectReferenceValue = wholeClip;
                    }
                }
            }

            so.ApplyModifiedProperties();
        }

        // 7. SEVEN TYPES MAP (Section E)
        private static void SetupSevenTypesMap(GameObject panel)
        {
            var map = panel.AddComponent<U5_SA_SevenTypesMap_Masters_Phonics>();

            map.mapIntroClip = LoadAudioMatching("Two of the seven types done Five to", "voice A");

            CreateHeader(panel.transform, "THE 7 SYLLABLE TYPES MAP", "Closed & Open Syllables Mastered! Tap any card to preview!", out TextMeshProUGUI titleTMP, out TextMeshProUGUI subTMP);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            GameObject tilesContainer = CreateContainer(panel.transform, "Tiles_Container", new Vector2(50f, -60f), new Vector2(1580f, 440f));

            SerializedObject so = new SerializedObject(map);
            SetProp(so, "titleTMP", titleTMP);
            SetProp(so, "subtitleTMP", subTMP);
            SetProp(so, "tilesContainer", tilesContainer.transform);
            SetProp(so, "replayAudioButton", replayBtn);
            so.ApplyModifiedProperties();
        }

        // 8. UNIT CHALLENGE (Section F)
        private static void SetupUnitChallenge(GameObject panel)
        {
            var challenge = panel.AddComponent<U5_SA_UnitChallenge_Masters_Phonics>();

            CreateHeader(panel.transform, "UNIT 5 FINAL CHALLENGE", "Test your mastery of Open & Closed Syllables!", out _, out _);
            Button replayBtn = CreateReplayButton(panel.transform, new Vector2(860f, 440f));

            TextMeshProUGUI qNumTxt = CreateText(panel.transform, "QuestionNumberText", "Question 1 of 10", 26);
            qNumTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 260f);

            TextMeshProUGUI promptTxt = CreateText(panel.transform, "PromptText", "Question Prompt", 32);
            promptTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 180f);
            promptTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(1200f, 100f);

            GameObject optionsContainer = CreateContainer(panel.transform, "OptionsContainer", new Vector2(0f, -100f), new Vector2(1100f, 360f));
            var vlg = optionsContainer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20f;
            vlg.childAlignment = TextAnchor.MiddleCenter;

            Button[] optionBtns = new Button[3];
            TextMeshProUGUI[] optionTxts = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                optionBtns[i] = CreateActionButton(optionsContainer.transform, $"Option_{i}", $"Option {i+1}", Vector2.zero, new Vector2(900f, 85f));
                optionTxts[i] = optionBtns[i].GetComponentInChildren<TextMeshProUGUI>();
            }

            CreateHUD(panel.transform, out TextMeshProUGUI scoreTxt, out TextMeshProUGUI streakTxt, out Slider pBar, out _);
            CreateFeedbackPanel(panel.transform, out GameObject fbPanel, out TextMeshProUGUI fbText);

            SerializedObject so = new SerializedObject(challenge);
            SetProp(so, "questionNumberText", qNumTxt);
            SetProp(so, "promptText", promptTxt);
            SetProp(so, "replayAudioBtn", replayBtn);
            SetProp(so, "optionsContainer", optionsContainer.transform);
            SetProp(so, "progressBar", pBar);
            SetProp(so, "scoreText", scoreTxt);
            SetProp(so, "streakText", streakTxt);
            SetProp(so, "correctSFX", LoadAudioMatching("correct", ""));
            SetProp(so, "wrongSFX", LoadAudioMatching("wrong", ""));
            SetProp(so, "feedbackPanel", fbPanel);
            SetProp(so, "feedbackText", fbText);

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

        // 9. COMPLETION PANEL
        private static void SetupCompletionPanel(GameObject panel)
        {
            CreateHeader(panel.transform, "COMPLETED UNIT - 5", "Open & Closed Syllables Mastered!", out _, out _);

            GameObject medalObj = CreateContainer(panel.transform, "Medal", new Vector2(0f, 80f), new Vector2(280f, 280f));
            Image img = medalObj.AddComponent<Image>();
            img.color = Color.white;
            Sprite badgeSprite = LoadSlicedSprite("U5_Sprite_Badge_Doorkeeper");
            if (badgeSprite != null) img.sprite = badgeSprite;
            else img.color = new Color(1f, 0.84f, 0f, 1f);

            TextMeshProUGUI scoreTxt = CreateText(panel.transform, "FinalScoreText", "Final Score: 5600", 38);
            scoreTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -140f);

            TextMeshProUGUI statsTxt = CreateText(panel.transform, "FinalStatsText", "Master Doorkeeper Badge Awarded!", 28);
            statsTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -210f);

            CreateActionButton(panel.transform, "ContinueToSignboardButton", "BACK TO UNIT MAP", new Vector2(-260f, -360f), new Vector2(340f, 85f));
            CreateActionButton(panel.transform, "NextUnitButton", "NEXT UNIT", new Vector2(260f, -360f), new Vector2(340f, 85f));
        }

        // =========================================================================
        // Audio & Sprite Utilities
        // =========================================================================

        public static Sprite LoadSlicedSprite(string sliceNamePrefix)
        {
            Object[] allSprites = AssetDatabase.LoadAllAssetsAtPath(SPRITESHEET_PATH);
            if (allSprites == null) return null;

            foreach (var obj in allSprites)
            {
                if (obj is Sprite s && s.name.ToLower().StartsWith(sliceNamePrefix.ToLower()))
                {
                    return s;
                }
            }
            return null;
        }

        public static AudioClip LoadAudioMatching(string keyword, string preferredSubdir = "")
        {
            if (string.IsNullOrEmpty(keyword)) return null;

            string[] searchPaths = string.IsNullOrEmpty(preferredSubdir)
                ? new[] { AUDIO_BASE_PATH, $"{AUDIO_BASE_PATH}/voice A", $"{AUDIO_BASE_PATH}/voice B" }
                : new[] { $"{AUDIO_BASE_PATH}/{preferredSubdir}", AUDIO_BASE_PATH };

            string[] guids = AssetDatabase.FindAssets("t:AudioClip", searchPaths);
            
            string cleanKeyword = NormalizeString(keyword);

            // 1. Exact match (cleaned)
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string filename = NormalizeString(Path.GetFileNameWithoutExtension(path));
                if (filename == cleanKeyword)
                {
                    return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }

            // 2. Starts with / contains match (cleaned)
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string filename = NormalizeString(Path.GetFileNameWithoutExtension(path));
                if (filename.Contains(cleanKeyword) || cleanKeyword.Contains(filename))
                {
                    return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
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
                if (tmp != null) tmp.text = "<b>UNIT 5: OPEN & CLOSED SYLLABLES</b>";
            }
        }

        private static void WireFlowManager(U5_SA_UnitFlowManager_Masters_Phonics flow, Transform selPanel, Transform secParent,
            GameObject learn, GameObject act1, GameObject act2, GameObject act3, GameObject act4, GameObject act5, GameObject challenge, GameObject comp)
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
            SetProp(so, "activity5Panel", act5);
            SetProp(so, "challengePanel", challenge);
            SetProp(so, "completionPanel", comp);

            if (backBtnT != null) SetProp(so, "globalBackButton", backBtnT.GetComponent<Button>());
            if (nextBtnT != null) SetProp(so, "nextActivityButton", nextBtnT.GetComponent<Button>());
            if (contBtnT != null) SetProp(so, "continueToSignboardButton", contBtnT.GetComponent<Button>());
            if (nextUnitBtnT != null) SetProp(so, "nextUnitButton", nextUnitBtnT.GetComponent<Button>());

            so.ApplyModifiedProperties();

            // Auto-wire persistent Section Selection buttons on the signboard
            if (selPanel != null)
            {
                Button[] allBtns = selPanel.GetComponentsInChildren<Button>(true);
                List<Button> secBtns = new List<Button>();
                foreach (var b in allBtns)
                {
                    if (b == null) continue;
                    if (b.name.ToLower().Contains("back") || b.name.ToLower().Contains("return") || b.name.ToLower().Contains("home"))
                    {
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(b.onClick, 0);
                        var backAction = (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), flow, typeof(U5_SA_UnitFlowManager_Masters_Phonics).GetMethod("BackToLessonsMenu"));
                        if (backAction != null) UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick, backAction);
                    }
                    else
                    {
                        secBtns.Add(b);
                    }
                }

                if (secBtns.Count == 5)
                {
                    string[] methodNames = new string[] { "OpenSection1", "OpenSection2", "OpenSection3", "OpenSection4", "OpenSection5" };
                    for (int i = 0; i < secBtns.Count; i++)
                    {
                        Button b = secBtns[i];
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(b.onClick, 0);
                        var targetMethod = typeof(U5_SA_UnitFlowManager_Masters_Phonics).GetMethod(methodNames[i]);
                        if (targetMethod != null)
                        {
                            var action = (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), flow, targetMethod);
                            UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick, action);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < secBtns.Count && i < 7; i++)
                    {
                        Button b = secBtns[i];
                        UnityEditor.Events.UnityEventTools.RemovePersistentListener(b.onClick, 0);
                        var targetMethod = typeof(U5_SA_UnitFlowManager_Masters_Phonics).GetMethod($"OpenSection{((char)('A' + i))}");
                        if (targetMethod != null)
                        {
                            var action = (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), flow, targetMethod);
                            UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick, action);
                        }
                    }
                }
            }
        }

        private static void CreateHeader(Transform parent, string title, string subtitle, out TextMeshProUGUI titleTMP, out TextMeshProUGUI subTMP)
        {
            titleTMP = CreateText(parent, "TitleText", $"<b>{title}</b>", 38);
            titleTMP.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 440f);
            titleTMP.color = new Color(0.12f, 0.2f, 0.35f, 1f);

            subTMP = CreateText(parent, "SubtitleText", subtitle, 24);
            subTMP.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 380f);
            subTMP.color = new Color(0.35f, 0.45f, 0.55f, 1f);
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
            tmp.text = content;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            return tmp;
        }

        private static Button CreateActionButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            Image img = go.GetComponent<Image>();
            img.color = Color.white;

            TextMeshProUGUI tmp = CreateText(go.transform, "Text", $"<b>{label}</b>", 24);
            tmp.color = new Color(0.15f, 0.2f, 0.3f, 1f);
            RectTransform textRt = tmp.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.sizeDelta = Vector2.zero;

            return go.GetComponent<Button>();
        }

        private static Button CreateReplayButton(Transform parent, Vector2 pos)
        {
            GameObject go = new GameObject("ReplayAudioButton", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(80f, 80f);

            Image img = go.GetComponent<Image>();
            Sprite speakerSprite = LoadSlicedSprite("U5_Sprite_Speaker_Icon");
            if (speakerSprite != null)
            {
                img.sprite = speakerSprite;
                img.color = Color.white;
            }
            else
            {
                img.color = Color.white;
                TextMeshProUGUI tmp = CreateText(go.transform, "Label", "AUDIO", 18);
                tmp.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            }

            return go.GetComponent<Button>();
        }

        private static GameObject CreateSwipeCard(Transform parent, string name, out RectTransform cardRt, out CanvasGroup cg, out TextMeshProUGUI wordTxt, out TextMeshProUGUI subTxt, out Image bg, out Image icon)
        {
            GameObject card = new GameObject(name, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            card.transform.SetParent(parent, false);

            cardRt = card.GetComponent<RectTransform>();
            cardRt.anchoredPosition = new Vector2(0f, 0f);
            cardRt.sizeDelta = new Vector2(480f, 320f);

            cg = card.GetComponent<CanvasGroup>();
            bg = card.GetComponent<Image>();
            bg.color = Color.white;

            wordTxt = CreateText(card.transform, "WordText", "WORD", 56);
            wordTxt.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
            wordTxt.alignment = TextAlignmentOptions.Center;
            wordTxt.color = new Color(0.12f, 0.18f, 0.28f, 1f);

            subTxt = CreateText(card.transform, "SubText", "", 24);
            subTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -60f);
            subTxt.color = new Color(0.4f, 0.5f, 0.6f, 1f);
            subTxt.gameObject.SetActive(false);

            icon = null;
            return card;
        }

        private static GameObject CreateSwipeIndicator(Transform parent, string name, string text, Vector2 pos)
        {
            GameObject ind = CreateContainer(parent, name, pos, new Vector2(340f, 240f));
            Image img = ind.AddComponent<Image>();
            img.color = Color.white;

            TextMeshProUGUI tmp = CreateText(ind.transform, "Text", $"<b>{text}</b>", 24);
            tmp.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            return ind;
        }

        private static RectTransform CreateDropBin(Transform parent, string name, string label, Vector2 pos)
        {
            GameObject bin = CreateContainer(parent, name, pos, new Vector2(380f, 420f));
            Image img = bin.AddComponent<Image>();
            img.color = Color.white;

            TextMeshProUGUI tmp = CreateText(bin.transform, "Label", $"<b>{label}</b>", 24);
            tmp.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            tmp.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 160f);

            return bin.GetComponent<RectTransform>();
        }

        private static void CreateHUD(Transform parent, out TextMeshProUGUI scoreTxt, out TextMeshProUGUI streakTxt, out Slider pBar, out TextMeshProUGUI pText)
        {
            scoreTxt = CreateText(parent, "ScoreText", "Score: 0", 26);
            scoreTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(-760f, 440f);
            scoreTxt.color = new Color(0.15f, 0.45f, 0.85f, 1f);

            streakTxt = CreateText(parent, "StreakText", "", 24);
            streakTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(-760f, 390f);
            streakTxt.color = new Color(1f, 0.5f, 0f, 1f);

            CreateProgressHUD(parent, out pBar, out pText);
        }

        private static void CreateProgressHUD(Transform parent, out Slider pBar, out TextMeshProUGUI pText)
        {
            GameObject sliderObj = new GameObject("ProgressBar", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(parent, false);
            RectTransform sRt = sliderObj.GetComponent<RectTransform>();
            sRt.anchoredPosition = new Vector2(0f, -440f);
            sRt.sizeDelta = new Vector2(600f, 20f);

            pBar = sliderObj.GetComponent<Slider>();
            pBar.value = 0f;

            pText = CreateText(parent, "ProgressText", "1 / 10", 22);
            pText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -470f);
        }

        private static void CreateFeedbackPanel(Transform parent, out GameObject fbPanel, out TextMeshProUGUI fbText)
        {
            fbPanel = CreateContainer(parent, "FeedbackPanel", Vector2.zero, new Vector2(700f, 350f));
            Image img = fbPanel.AddComponent<Image>();
            img.color = new Color(0.1f, 0.12f, 0.18f, 0.95f);

            fbText = CreateText(fbPanel.transform, "FeedbackText", "Activity Complete!", 32);
            fbText.color = Color.white;
            fbPanel.SetActive(false);
        }

        private static void StripOldUnitComponents(GameObject go)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            Component[] comps = go.GetComponents<Component>();
            foreach (var c in comps)
            {
                if (c == null) continue;
                if (c is Transform || c is RectTransform || c is CanvasRenderer || c is CanvasGroup ||
                    c is Image || c is Button || c is Slider || c is LayoutGroup || c is TextMeshProUGUI)
                {
                    continue;
                }

                DestroyImmediate(c);
            }
        }
    }
}
