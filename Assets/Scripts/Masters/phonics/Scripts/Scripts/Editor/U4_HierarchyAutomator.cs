using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

namespace MastersPhonics.EditorTools
{
    public class U4_HierarchyAutomator : EditorWindow
    {
        private const string SPRITESHEET_PATH = "Assets/Art/unit4_MP/U4 all sprites MP.png";
        private const string AUDIO_BASE_PATH = "Assets/Audio/U4_audio";

        [MenuItem("Masters Phonics/Unit 4/Duplicate Unit 3 -> Generate Unit 4 Hierarchy", false, 100)]
        public static void GenerateUnit4Hierarchy()
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                EditorUtility.DisplayDialog("Error", "No Canvas found in the current Scene!", "OK");
                return;
            }

            Transform unit3Transform = canvas.transform.Find("Unit_3");
            if (unit3Transform == null)
            {
                EditorUtility.DisplayDialog("Error", "Could not find 'Unit_3' under Canvas to duplicate!", "OK");
                return;
            }

            // Check if Unit_4 already exists
            Transform existingU4 = canvas.transform.Find("Unit_4");
            if (existingU4 != null)
            {
                bool overwrite = EditorUtility.DisplayDialog("Unit_4 Exists", "Unit_4 already exists in Canvas. Do you want to delete it and regenerate?", "Yes, Replace", "Cancel");
                if (!overwrite) return;
                Undo.DestroyObjectImmediate(existingU4.gameObject);
            }

            // 1. Duplicate Unit_3
            GameObject unit4Obj = Object.Instantiate(unit3Transform.gameObject, canvas.transform);
            unit4Obj.name = "Unit_4";
            Undo.RegisterCreatedObjectUndo(unit4Obj, "Generate Unit_4");

            // Strip out Unit 3 Components on Root
            Component[] oldRootComponents = unit4Obj.GetComponents<Component>();
            foreach (var comp in oldRootComponents)
            {
                if (comp is U3_SA_UnitFlowManager_Masters_Phonics || comp is U3_SA_AudioManager_Masters_Phonics)
                {
                    DestroyImmediate(comp);
                }
            }

            // Add Unit 4 Root Managers
            var flowManager = unit4Obj.AddComponent<U4_SA_UnitFlowManager_Masters_Phonics>();
            var audioManager = unit4Obj.AddComponent<U4_SA_AudioManager_Masters_Phonics>();

            // Assign Audio Manager Voice & SFX clips
            AssignAudioManagerClips(audioManager);

            // Assign Unit Completion voice
            flowManager.unitCompleteVoiceClip = LoadAudioMatching("Unit Four done Eighteen schwas");

            // 2. Handle Section Selection Panels
            Transform selPanels = unit4Obj.transform.Find("Unit_3_Section_Selection_Panels");
            if (selPanels != null)
            {
                selPanels.name = "Unit_4_Section_Selection_Panels";
            }

            // 3. Handle Sections Container
            Transform sectionsParent = unit4Obj.transform.Find("Unit_3_Sections");
            if (sectionsParent != null)
            {
                sectionsParent.name = "Unit_4_Sections";

                // Rename and wire child activities
                Transform learnPanel = sectionsParent.Find("U3_learn_Concept_Cards_Panel");
                if (learnPanel != null)
                {
                    learnPanel.name = "U4_learn_Concept_Cards_Panel";
                    StripComponents(learnPanel.gameObject);
                    var gm01 = learnPanel.gameObject.AddComponent<U4_SA_GM01_ConceptCards_Masters_Phonics>();
                    SetupConceptCards(gm01, learnPanel);
                }

                // Activity 1: Seven Doors
                Transform act1Panel = sectionsParent.Find("U3_Activity_1_Chin_Tap");
                if (act1Panel != null)
                {
                    act1Panel.name = "U4_Activity_1_Seven_Doors";
                    StripComponents(act1Panel.gameObject);
                    var gm03 = act1Panel.gameObject.AddComponent<U4_SA_GM03_SevenDoors_Masters_Phonics>();
                    SetupSevenDoors(gm03, act1Panel);
                }

                // Activity 2: Strong Beat
                Transform act2Panel = sectionsParent.Find("U3_Activity_2_Split_It");
                if (act2Panel != null)
                {
                    act2Panel.name = "U4_Activity_2_Strong_Beat";
                    StripComponents(act2Panel.gameObject);
                    var gm04 = act2Panel.gameObject.AddComponent<U4_SA_GM04_StrongBeat_Masters_Phonics>();
                    SetupStrongBeat(gm04, act2Panel);
                }

                // Activity 3: Schwa Hunt
                Transform act3Panel = sectionsParent.Find("U3_Activity_3_Syllable_Sort");
                if (act3Panel != null)
                {
                    act3Panel.name = "U4_Activity_3_Schwa_Hunt";
                    StripComponents(act3Panel.gameObject);
                    var gm06 = act3Panel.gameObject.AddComponent<U4_SA_GM06_SchwaHunt_Masters_Phonics>();
                    SetupSchwaHunt(gm06, act3Panel);
                }

                // Activity 4: Seven Types Map
                Transform act4Panel = sectionsParent.Find("U3_Activity_4_Syllable_Builder");
                if (act4Panel != null)
                {
                    act4Panel.name = "U4_Seven_Types_Map";
                    StripComponents(act4Panel.gameObject);
                    var map = act4Panel.gameObject.AddComponent<U4_SA_SevenTypesMap_Masters_Phonics>();
                    SetupSevenTypesMap(map, act4Panel);
                }

                // Activity 5: Unit Challenge
                Transform act5Panel = sectionsParent.Find("U3_Activity_5_Pattern_Detective");
                if (act5Panel != null)
                {
                    act5Panel.name = "U4_Unit_Challenge";
                    StripComponents(act5Panel.gameObject);
                    var challenge = act5Panel.gameObject.AddComponent<U4_SA_UnitChallenge_Masters_Phonics>();
                    SetupUnitChallenge(challenge, act5Panel);
                }

                // Completion Panel
                Transform compPanel = sectionsParent.Find("U3_COMPLETE_Panel");
                if (compPanel != null)
                {
                    compPanel.name = "U4_COMPLETE_Panel";
                    var rewardTitle = compPanel.Find("RewardTitle");
                    if (rewardTitle != null)
                    {
                        var tmp = rewardTitle.GetComponent<TextMeshProUGUI>();
                        if (tmp != null) tmp.text = "<b>COMPLETED UNIT - 4</b>";
                    }

                    // Assign Sound Detective Badge sprite if image exists
                    Sprite badgeSprite = LoadSlicedSprite("Badge_SoundDetective");
                    if (badgeSprite != null)
                    {
                        Transform badgeObj = compPanel.Find("Medal") ?? compPanel.Find("Badge") ?? compPanel.Find("Trophy") ?? compPanel.Find("Certificate");
                        if (badgeObj != null)
                        {
                            var img = badgeObj.GetComponent<Image>();
                            if (img != null) img.sprite = badgeSprite;
                        }
                    }
                }
            }

            // Explicitly serialize and wire all Inspector fields across Flow Manager and all child game modes
            WireSerializedFieldsExplicitly(flowManager, unit4Obj.transform);

            EditorUtility.SetDirty(unit4Obj);
            Selection.activeGameObject = unit4Obj;

            EditorUtility.DisplayDialog("Success!", "Unit_4 hierarchy generated, all Inspector GameObject/Component fields explicitly assigned, and Audio/Sprites wired!", "Awesome!");
        }

        private static void WireSerializedFieldsExplicitly(U4_SA_UnitFlowManager_Masters_Phonics flow, Transform root)
        {
            SerializedObject soFlow = new SerializedObject(flow);
            Transform selPanel = root.Find("Unit_4_Section_Selection_Panels");
            Transform secParent = root.Find("Unit_4_Sections");

            soFlow.FindProperty("sectionSelectionPanel").objectReferenceValue = selPanel != null ? selPanel.gameObject : null;
            soFlow.FindProperty("sectionsParentContainer").objectReferenceValue = secParent != null ? secParent.gameObject : null;

            if (secParent != null)
            {
                soFlow.FindProperty("learnPanel").objectReferenceValue = GetChildGo(secParent, "U4_learn_Concept_Cards_Panel");
                soFlow.FindProperty("activity1Panel").objectReferenceValue = GetChildGo(secParent, "U4_Activity_1_Seven_Doors");
                soFlow.FindProperty("activity2Panel").objectReferenceValue = GetChildGo(secParent, "U4_Activity_2_Strong_Beat");
                soFlow.FindProperty("activity3Panel").objectReferenceValue = GetChildGo(secParent, "U4_Activity_3_Schwa_Hunt");
                soFlow.FindProperty("mapPanel").objectReferenceValue = GetChildGo(secParent, "U4_Seven_Types_Map");
                soFlow.FindProperty("challengePanel").objectReferenceValue = GetChildGo(secParent, "U4_Unit_Challenge");
                soFlow.FindProperty("completionPanel").objectReferenceValue = GetChildGo(secParent, "U4_COMPLETE_Panel");
            }

            soFlow.ApplyModifiedProperties();

            // Auto-wire Section Selection buttons on the signboard
            if (selPanel != null)
            {
                Button[] secBtns = selPanel.GetComponentsInChildren<Button>(true);
                for (int i = 0; i < secBtns.Length && i < 6; i++)
                {
                    int secIdx = i;
                    Button b = secBtns[i];
                    UnityEditor.Events.UnityEventTools.RemovePersistentListener(b.onClick, 0);
                    // Wire to OpenSection on flow
                    var targetMethod = typeof(U4_SA_UnitFlowManager_Masters_Phonics).GetMethod($"OpenSection{((char)('A' + secIdx))}");
                    if (targetMethod != null)
                    {
                        var action = (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), flow, targetMethod);
                        UnityEditor.Events.UnityEventTools.AddPersistentListener(b.onClick, action);
                    }
                }
            }
        }

        private static GameObject GetChildGo(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            return t != null ? t.gameObject : null;
        }

        private static void AssignAudioManagerClips(U4_SA_AudioManager_Masters_Phonics am)
        {
            am.schwaUhClip = LoadAudioMatching("uh.mp3") ?? LoadAudioMatching("uh");
        }

        private static void SetupConceptCards(U4_SA_GM01_ConceptCards_Masters_Phonics gm01, Transform panel)
        {
            gm01.card1VoiceClip = LoadAudioMatching("You know this one already");
            gm01.card2VoiceClip = LoadAudioMatching("There are seven kinds of syllable");
            gm01.card3VoiceClip = LoadAudioMatching("Say rabbit RABbit");
            gm01.card4VoiceClip = LoadAudioMatching("Heres the secret When a beat is quiet");
            gm01.card5VoiceClip = LoadAudioMatching("Two clues for finding it");

            SerializedObject so = new SerializedObject(gm01);
            so.FindProperty("titleTMP").objectReferenceValue = FindTMP(panel, "Title BG/TitleText", "TitleText", "Title");
            so.FindProperty("descriptionTMP").objectReferenceValue = FindTMP(panel, "DescriptionText", "Prompt_Text", "Subtitle");
            so.FindProperty("dynamicCardContainer").objectReferenceValue = panel.Find("DynamicCardContainer") ?? panel.Find("Cards_Container");
            so.FindProperty("nextCardButton").objectReferenceValue = FindBtn(panel, "NextButton", "Next_Button", "Btn_Next");
            so.FindProperty("replayVoiceAButton").objectReferenceValue = FindBtn(panel, "ReplayButton", "Audio_Button", "Speaker_Button");
            so.ApplyModifiedProperties();
        }

        private static void SetupSevenDoors(U4_SA_GM03_SevenDoors_Masters_Phonics gm03, Transform panel)
        {
            gm03.introInstructionClip = LoadAudioMatching("Seven types four bins at a time");
            gm03.outroClip = LoadAudioMatching("Names learned The real work");

            Sprite doorClosed = LoadSlicedSprite("Door_Closed");
            Sprite doorOpen = LoadSlicedSprite("Door_Open");
            Sprite doorMagicE = LoadSlicedSprite("Door_MagicE");
            Sprite doorVowelTeam = LoadSlicedSprite("Door_VowelTeam");

            AssignSpriteToBin(panel, "1", doorClosed);
            AssignSpriteToBin(panel, "2", doorOpen);
            AssignSpriteToBin(panel, "3", doorMagicE);
            AssignSpriteToBin(panel, "4", doorVowelTeam);

            SerializedObject so = new SerializedObject(gm03);
            so.FindProperty("titleTMP").objectReferenceValue = FindTMP(panel, "Title BG/TitleText", "TitleText", "Title");
            so.FindProperty("promptTMP").objectReferenceValue = FindTMP(panel, "prompt bg/Prompt_Text", "Prompt_Text", "PromptText");
            so.FindProperty("scoreTMP").objectReferenceValue = FindTMP(panel, "ScoreText", "Score_Text", "Score");
            so.FindProperty("deckRemainingTMP").objectReferenceValue = FindTMP(panel, "DeckRemaining_Text", "RemainingText");
            so.FindProperty("progressBar").objectReferenceValue = panel.GetComponentInChildren<Slider>(true);
            so.FindProperty("cardSpawnAnchor").objectReferenceValue = panel.Find("CardSpawnAnchor") ?? panel.Find("Deck_Anchor");
            so.FindProperty("replayAudioButton").objectReferenceValue = FindBtn(panel, "ReplayButton", "Audio_Button", "Speaker_Button", "Replay_Button", "SpeakerButton");

            Transform binsGroup = panel.Find("Bins_Row") ?? panel.Find("Doors_Row") ?? panel.Find("Sort_Bins") ?? panel;
            so.FindProperty("bin1").objectReferenceValue = FindRT(binsGroup, "door1", "bin1", "1");
            so.FindProperty("bin2").objectReferenceValue = FindRT(binsGroup, "door2", "bin2", "2");
            so.FindProperty("bin3").objectReferenceValue = FindRT(binsGroup, "door3", "bin3", "3");
            so.FindProperty("bin4").objectReferenceValue = FindRT(binsGroup, "door4", "bin4", "4");
            so.ApplyModifiedProperties();
        }

        private static void SetupStrongBeat(U4_SA_GM04_StrongBeat_Masters_Phonics gm04, Transform panel)
        {
            gm04.introInstructionClip = LoadAudioMatching("Two beats One is stronger");
            gm04.outroClip = LoadAudioMatching("Did you notice The quiet beat");

            Sprite drumSprite = LoadSlicedSprite("Tile_StrongBeat_Drum");
            Sprite pebbleSprite = LoadSlicedSprite("Tile_WeakBeat_Pebble");

            Button b1 = FindBtn(panel, "Tile_1", "Syllable_1", "Btn_Syllable1");
            Button b2 = FindBtn(panel, "Tile_2", "Syllable_2", "Btn_Syllable2");

            if (b1 != null && drumSprite != null)
            {
                var img = b1.GetComponent<Image>();
                if (img != null) img.sprite = drumSprite;
            }
            if (b2 != null && pebbleSprite != null)
            {
                var img = b2.GetComponent<Image>();
                if (img != null) img.sprite = pebbleSprite;
            }

            SerializedObject so = new SerializedObject(gm04);
            so.FindProperty("titleTMP").objectReferenceValue = FindTMP(panel, "Title BG/TitleText", "TitleText", "Title");
            so.FindProperty("promptTMP").objectReferenceValue = FindTMP(panel, "prompt bg/Prompt_Text", "Prompt_Text", "PromptText");
            so.FindProperty("scoreTMP").objectReferenceValue = FindTMP(panel, "ScoreText", "Score_Text", "Score");
            so.FindProperty("progressBar").objectReferenceValue = panel.GetComponentInChildren<Slider>(true);
            so.FindProperty("tile1Button").objectReferenceValue = b1;
            so.FindProperty("tile2Button").objectReferenceValue = b2;
            so.FindProperty("tile1TextTMP").objectReferenceValue = b1 != null ? b1.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            so.FindProperty("tile2TextTMP").objectReferenceValue = b2 != null ? b2.GetComponentInChildren<TextMeshProUGUI>(true) : null;
            so.FindProperty("replayAudioButton").objectReferenceValue = FindBtn(panel, "ReplayButton", "Audio_Button", "Speaker_Button", "Replay_Button", "SpeakerButton");
            so.ApplyModifiedProperties();
        }

        private static void SetupSchwaHunt(U4_SA_GM06_SchwaHunt_Masters_Phonics gm06, Transform panel)
        {
            gm06.introInstructionClip = LoadAudioMatching("Now find the lazy one");

            SerializedObject so = new SerializedObject(gm06);
            so.FindProperty("titleTMP").objectReferenceValue = FindTMP(panel, "Title BG/TitleText", "TitleText", "Title");
            so.FindProperty("promptTMP").objectReferenceValue = FindTMP(panel, "prompt bg/Prompt_Text", "Prompt_Text", "PromptText");
            so.FindProperty("scoreTMP").objectReferenceValue = FindTMP(panel, "ScoreText", "Score_Text", "Score");
            so.FindProperty("progressBar").objectReferenceValue = panel.GetComponentInChildren<Slider>(true);
            so.FindProperty("letterTilesContainer").objectReferenceValue = panel.Find("LetterTiles_Container") ?? panel.Find("Letters_Row") ?? panel.Find("WordContainer");
            so.FindProperty("respellingTextTMP").objectReferenceValue = FindTMP(panel, "Respelling_Text", "RespellingText", "Phonetic_Text");
            so.FindProperty("clueBannerTMP").objectReferenceValue = FindTMP(panel, "Clue_Banner", "ClueBanner", "RuleText");
            so.FindProperty("replayAudioButton").objectReferenceValue = FindBtn(panel, "ReplayButton", "Audio_Button", "Speaker_Button", "Replay_Button", "SpeakerButton");
            so.ApplyModifiedProperties();
        }

        private static void SetupSevenTypesMap(U4_SA_SevenTypesMap_Masters_Phonics map, Transform panel)
        {
            map.mapIntroClip = LoadAudioMatching("Seven types Two done five to go");

            SerializedObject so = new SerializedObject(map);
            so.FindProperty("titleTMP").objectReferenceValue = FindTMP(panel, "Title BG/TitleText", "TitleText", "Title");
            so.FindProperty("subtitleTMP").objectReferenceValue = FindTMP(panel, "SubtitleText", "Prompt_Text", "Subtitle");
            so.FindProperty("backButton").objectReferenceValue = FindBtn(panel, "BackButton", "Back_Button", "Btn_Back");
            so.FindProperty("tilesContainer").objectReferenceValue = panel.Find("Tiles_Container") ?? panel.Find("MapGrid") ?? panel.Find("Grid");
            so.FindProperty("replayAudioButton").objectReferenceValue = FindBtn(panel, "ReplayButton", "Audio_Button", "Speaker_Button", "Replay_Button", "SpeakerButton");
            so.ApplyModifiedProperties();
        }

        private static void SetupUnitChallenge(U4_SA_UnitChallenge_Masters_Phonics challenge, Transform panel)
        {
            challenge.challengeIntroClip = LoadAudioMatching("Ten questions Ears first");

            SerializedObject so = new SerializedObject(challenge);
            so.FindProperty("titleTMP").objectReferenceValue = FindTMP(panel, "Title BG/TitleText", "TitleText", "Title");
            so.FindProperty("promptTMP").objectReferenceValue = FindTMP(panel, "prompt bg/Prompt_Text", "Prompt_Text", "PromptText");
            so.FindProperty("scoreTMP").objectReferenceValue = FindTMP(panel, "ScoreText", "Score_Text", "Score");
            so.FindProperty("progressBar").objectReferenceValue = panel.GetComponentInChildren<Slider>(true);
            so.FindProperty("questionTextTMP").objectReferenceValue = FindTMP(panel, "Question_Text", "QuestionText", "Prompt");
            so.FindProperty("optionsContainer").objectReferenceValue = panel.Find("Options_Container") ?? panel.Find("Options_Row") ?? panel.Find("AnswersContainer");
            so.FindProperty("replayAudioButton").objectReferenceValue = FindBtn(panel, "ReplayButton", "Audio_Button", "Speaker_Button", "Replay_Button", "SpeakerButton");
            so.ApplyModifiedProperties();
        }

        private static TextMeshProUGUI FindTMP(Transform panel, params string[] paths)
        {
            foreach (var p in paths)
            {
                Transform t = panel.Find(p);
                if (t != null)
                {
                    var tmp = t.GetComponentInChildren<TextMeshProUGUI>(true);
                    if (tmp != null) return tmp;
                }
            }
            return panel.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private static Button FindBtn(Transform panel, params string[] paths)
        {
            foreach (var p in paths)
            {
                Transform t = panel.Find(p);
                if (t != null)
                {
                    var btn = t.GetComponent<Button>();
                    if (btn != null) return btn;
                }
            }
            return null;
        }

        private static RectTransform FindRT(Transform parent, params string[] keywords)
        {
            RectTransform[] all = parent.GetComponentsInChildren<RectTransform>(true);
            foreach (var r in all)
            {
                string n = r.name.ToLower();
                foreach (var kw in keywords)
                {
                    if (n.Contains(kw)) return r;
                }
            }
            return null;
        }

        private static void AssignSpriteToBin(Transform panel, string binIndexStr, Sprite sprite)
        {
            if (sprite == null) return;
            Transform bin = panel.Find($"Bin_{binIndexStr}") ?? panel.Find($"Door_{binIndexStr}") ?? panel.Find($"Bin{binIndexStr}");
            if (bin != null)
            {
                Image img = bin.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = sprite;
                    img.color = Color.white;
                }
            }
        }

        private static Sprite LoadSlicedSprite(string sliceNamePrefix)
        {
            Object[] allSprites = AssetDatabase.LoadAllAssetsAtPath(SPRITESHEET_PATH);
            if (allSprites == null) return null;

            foreach (var obj in allSprites)
            {
                if (obj is Sprite s && s.name.StartsWith(sliceNamePrefix))
                {
                    return s;
                }
            }
            return null;
        }

        private static AudioClip LoadAudioMatching(string keyword)
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AUDIO_BASE_PATH });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string filename = Path.GetFileNameWithoutExtension(path);
                if (filename.ToLower().Contains(keyword.ToLower()))
                {
                    return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                }
            }
            return null;
        }

        private static void StripComponents(GameObject go)
        {
            Component[] comps = go.GetComponents<Component>();
            foreach (var c in comps)
            {
                if (c is MonoBehaviour && !(c is RectTransform) && !(c is CanvasRenderer) && !(c is Image) && !(c is Button) && !(c is TextMeshProUGUI) && !(c is Slider))
                {
                    DestroyImmediate(c);
                }
            }
        }
    }
}
