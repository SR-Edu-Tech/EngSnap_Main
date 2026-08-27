#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Menu: Phonics → Setup Unit 8 Scene & UI
/// Builds the complete Unit 8 hierarchy under the Scene Canvas:
///   Unit_8
///     ├── Unit_8_Section_Selection_Panels   (signboard with 4 buttons)
///     └── Unit_8_Sections                   (starts inactive)
///          ├── SectionA_Panel  + U8_A1_SoundWallController
///          ├── SectionB_Panel  + U8_A2_BuzzWhisperController
///          ├── SectionC_Panel  + U8_A3_ConnectSoundController
///          ├── SectionD_Panel  + U8_A4_ConsonantSafariController
///          ├── RewardPanel     + U8_RewardController
///          ├── Back_Button
///          └── Next_Button
/// </summary>
public class U8_EditorSetup : EditorWindow
{
    // ──────────────────────────────────────────────────────────
    //  Menu entry
    // ──────────────────────────────────────────────────────────

    [MenuItem("Phonics/Setup Unit 8 Scene & UI")]
    public static void SetupUnit8UI()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[Unit 8 Setup] No Canvas found in the current scene!");
            return;
        }

        // ── 1. Unit_8 root ──────────────────────────────────────
        Transform unit8Trans = canvas.transform.Find("Unit_8");
        if (unit8Trans == null)
        {
            GameObject u8Obj = new GameObject("Unit_8", typeof(RectTransform));
            u8Obj.transform.SetParent(canvas.transform, false);
            StretchFull(u8Obj.GetComponent<RectTransform>());
            unit8Trans = u8Obj.transform;
        }

        GameObject unit8Root = unit8Trans.gameObject;

        // ── 2. U8_Manager on root ───────────────────────────────
        U8_Manager manager = unit8Root.GetComponent<U8_Manager>();
        if (manager == null) manager = unit8Root.AddComponent<U8_Manager>();

        // Try to load the Unit8LevelData asset if it exists already
        manager.levelData = AssetDatabase.LoadAssetAtPath<Unit8LevelData>(
            "Assets/Data/Unit8/Unit8Level_Main.asset");

        // ── 3. Section Selection Panel (signboard) ───────────────
        GameObject selPanel = GetOrCreate(unit8Trans, "Unit_8_Section_Selection_Panels");
        selPanel.SetActive(true);
        manager.levelSelectionPanel = selPanel;
        BuildSelectionButtons(selPanel.transform, manager);

        // ── 4. Unit_8_Sections container (starts INACTIVE) ──────
        GameObject sectionsContainer = GetOrCreate(unit8Trans, "Unit_8_Sections");
        sectionsContainer.SetActive(false);   // deactivated at design time

        Transform secRoot = sectionsContainer.transform;

        // ── 5. SectionA — Consonant Sound Wall ──────────────────
        GameObject secA = GetOrCreate(secRoot, "SectionA_Panel");
        secA.SetActive(false);
        manager.sectionAPanel = secA;

        U8_A1_SoundWallController a1 = GetOrAddComponent<U8_A1_SoundWallController>(secA);
        manager.a1Controller = a1;
        a1.manager = manager;

        // AudioSource for Section A
        if (secA.GetComponent<AudioSource>() == null)
            secA.AddComponent<AudioSource>();
        a1.audioSource = secA.GetComponent<AudioSource>();

        // TilesContainer (GridLayoutGroup)
        GameObject tilesContainer = GetOrCreate(secA.transform, "TilesContainer");
        GridLayoutGroup grid = GetOrAddComponent<GridLayoutGroup>(tilesContainer);
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.cellSize        = new Vector2(160f, 180f);
        grid.spacing         = new Vector2(12f, 12f);
        grid.childAlignment  = TextAnchor.UpperCenter;
        a1.tilesContainer    = tilesContainer.transform;

        // Title + instruction labels
        CreateLabel(secA.transform, "TitleText",       "Consonant Sounds 🔊", 48, TextAlignmentOptions.Center,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -130f), new Vector2(0f, -60f));
        CreateLabel(secA.transform, "InstructionText", "Tap a letter to hear its sound!", 28, TextAlignmentOptions.Center,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -180f), new Vector2(0f, -130f));

        // ── 6. SectionB — Buzz or Whisper ───────────────────────
        GameObject secB = GetOrCreate(secRoot, "SectionB_Panel");
        secB.SetActive(false);
        manager.sectionBPanel = secB;

        U8_A2_BuzzWhisperController a2 = GetOrAddComponent<U8_A2_BuzzWhisperController>(secB);
        manager.a2Controller = a2;
        a2.manager = manager;

        if (secB.GetComponent<AudioSource>() == null)
            secB.AddComponent<AudioSource>();
        a2.audioSource = secB.GetComponent<AudioSource>();

        // PhonemeDisplay — big centre label
        GameObject phonemeObj = GetOrCreate(secB.transform, "PhonemeDisplay");
        TextMeshProUGUI phonemeTMP = GetOrAddComponent<TextMeshProUGUI>(phonemeObj);
        phonemeTMP.text      = "/z/";
        phonemeTMP.fontSize   = 96;
        phonemeTMP.alignment  = TextAlignmentOptions.Center;
        phonemeTMP.color      = new Color(0.15f, 0.15f, 0.6f);
        SetAnchored(phonemeObj.GetComponent<RectTransform>(),
                    new Vector2(0.15f, 0.45f), new Vector2(0.85f, 0.75f));
        a2.phonemeDisplayText = phonemeTMP;

        // ThroatGraphic placeholder image
        GameObject throatObj = GetOrCreate(secB.transform, "ThroatGraphic");
        GetOrAddComponent<Image>(throatObj).color = new Color(1f, 0.85f, 0.85f, 0.6f);
        SetAnchored(throatObj.GetComponent<RectTransform>(),
                    new Vector2(0.3f, 0.25f), new Vector2(0.7f, 0.45f));
        a2.throatGraphic = throatObj.GetComponent<RectTransform>();

        // FeedbackText
        GameObject feedbackObj = GetOrCreate(secB.transform, "FeedbackText");
        TextMeshProUGUI feedbackTMP = GetOrAddComponent<TextMeshProUGUI>(feedbackObj);
        feedbackTMP.text      = "";
        feedbackTMP.fontSize   = 36;
        feedbackTMP.alignment  = TextAlignmentOptions.Center;
        feedbackTMP.color      = new Color(0.1f, 0.6f, 0.1f);
        SetAnchored(feedbackObj.GetComponent<RectTransform>(),
                    new Vector2(0.05f, 0.12f), new Vector2(0.95f, 0.25f));
        a2.feedbackText = feedbackTMP;

        // BuzzButton
        GameObject buzzBtn = GetOrCreate(secB.transform, "BuzzButton");
        GetOrAddComponent<Image>(buzzBtn).color    = new Color(1f, 0.85f, 0.1f);
        Button buzzBtnComp = GetOrAddComponent<Button>(buzzBtn);
        SetAnchored(buzzBtn.GetComponent<RectTransform>(),
                    new Vector2(0.05f, 0.62f), new Vector2(0.45f, 0.78f));
        CreateChildLabel(buzzBtn.transform, "Label", "BUZZ 🐝", 34);
        a2.buzzButton = buzzBtnComp;

        // WhisperButton
        GameObject whisBtn = GetOrCreate(secB.transform, "WhisperButton");
        GetOrAddComponent<Image>(whisBtn).color    = new Color(0.7f, 0.85f, 1f);
        Button whisBtnComp = GetOrAddComponent<Button>(whisBtn);
        SetAnchored(whisBtn.GetComponent<RectTransform>(),
                    new Vector2(0.55f, 0.62f), new Vector2(0.95f, 0.78f));
        CreateChildLabel(whisBtn.transform, "Label", "WHISPER 🤫", 34);
        a2.whisperButton = whisBtnComp;

        // Instruction label
        CreateLabel(secB.transform, "TitleText", "Buzz or Whisper? 🐝", 48, TextAlignmentOptions.Center,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -130f), new Vector2(0f, -60f));
        CreateLabel(secB.transform, "InstructionText", "Feel your throat as you say the sound!", 28, TextAlignmentOptions.Center,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -180f), new Vector2(0f, -130f));

        // ── 7. SectionC — Connect the Sound ─────────────────────
        GameObject secC = GetOrCreate(secRoot, "SectionC_Panel");
        secC.SetActive(false);
        manager.sectionCPanel = secC;

        U8_A3_ConnectSoundController a3 = GetOrAddComponent<U8_A3_ConnectSoundController>(secC);
        manager.a3Controller = a3;
        a3.manager = manager;

        if (secC.GetComponent<AudioSource>() == null)
            secC.AddComponent<AudioSource>();
        a3.audioSource = secC.GetComponent<AudioSource>();
        a3.correctChime = LoadClip("Assets/Audio Clips/unit 8/Great listening.mp3");
        a3.wrongShake   = LoadClip("Assets/Audio Clips/unit 8/Almost Feel it again.mp3");

        // LinesContainer
        GameObject linesContainer = GetOrCreate(secC.transform, "LinesContainer");
        StretchFull(linesContainer.GetComponent<RectTransform>());
        a3.linesContainer = linesContainer.transform;

        // Left column — 5 letter buttons (p, d, b, t, m)
        string[] letters  = { "P", "D", "B", "T", "M" };
        GameObject letCol = GetOrCreate(secC.transform, "LettersColumn");
        SetAnchored(letCol.GetComponent<RectTransform>(),
                    new Vector2(0.05f, 0.1f), new Vector2(0.3f, 0.9f));
        GetOrAddComponent<VerticalLayoutGroup>(letCol).spacing = 14f;

        Button[] letterBtns   = new Button[5];
        TextMeshProUGUI[] letterLbls = new TextMeshProUGUI[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject btnObj = GetOrCreate(letCol.transform, $"LetterButton_{i}");
            GetOrAddComponent<Image>(btnObj).color = new Color(0.95f, 0.95f, 1f);
            letterBtns[i] = GetOrAddComponent<Button>(btnObj);
            TextMeshProUGUI lbl = CreateChildLabel(btnObj.transform, "Label", letters[i], 52);
            letterLbls[i] = lbl;
        }
        a3.letterButtons = letterBtns;
        a3.letterLabels  = letterLbls;

        // Right column — 5 picture buttons (bicycle, telescope, door, pumpkin, matchbox)
        string[] picNames   = { "bicycle", "telescope", "door", "pumpkin", "matchbox" };
        GameObject picCol   = GetOrCreate(secC.transform, "PicturesColumn");
        SetAnchored(picCol.GetComponent<RectTransform>(),
                    new Vector2(0.7f, 0.1f), new Vector2(0.95f, 0.9f));
        GetOrAddComponent<VerticalLayoutGroup>(picCol).spacing = 14f;

        Button[] picBtns  = new Button[5];
        Image[]  picImgs  = new Image[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject btnObj = GetOrCreate(picCol.transform, $"PictureButton_{i}");
            Image img = GetOrAddComponent<Image>(btnObj);
            img.color     = new Color(1f, 1f, 0.95f);
            picImgs[i]    = img;
            picBtns[i]    = GetOrAddComponent<Button>(btnObj);
            CreateChildLabel(btnObj.transform, "Label", picNames[i], 18);
        }
        a3.pictureButtons = picBtns;
        a3.pictureImages  = picImgs;

        CreateLabel(secC.transform, "TitleText",       "Connect the Sound 🔗", 48, TextAlignmentOptions.Center,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -130f), new Vector2(0f, -60f));
        CreateLabel(secC.transform, "InstructionText", "Draw a line from each letter to its picture", 28, TextAlignmentOptions.Center,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -180f), new Vector2(0f, -130f));

        // ── 8. SectionD — Consonant Safari ──────────────────────
        GameObject secD = GetOrCreate(secRoot, "SectionD_Panel");
        secD.SetActive(false);
        manager.sectionDPanel = secD;

        U8_A4_ConsonantSafariController a4 = GetOrAddComponent<U8_A4_ConsonantSafariController>(secD);
        manager.a4Controller = a4;
        a4.manager = manager;

        if (secD.GetComponent<AudioSource>() == null)
            secD.AddComponent<AudioSource>();
        a4.audioSource    = secD.GetComponent<AudioSource>();
        a4.catchClip      = LoadClip("Assets/Audio Clips/Great Thats Correct.mp3");
        a4.completionClip = LoadClip("Assets/Audio Clips/unit 8/Great listening.mp3");
        a4.wrongClip      = LoadClip("Assets/Audio Clips/unit 8/Almost Feel it again.mp3");

        // SafariArea — full panel RectTransform for spawning
        GameObject safariArea = GetOrCreate(secD.transform, "SafariArea");
        StretchFull(safariArea.GetComponent<RectTransform>());
        a4.spawnArea = safariArea.GetComponent<RectTransform>();

        CreateLabel(secD.transform, "TitleText",       "Consonant Safari 🦁", 48, TextAlignmentOptions.Center,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -130f), new Vector2(0f, -60f));
        CreateLabel(secD.transform, "InstructionText", "Tap all the consonants!", 28, TextAlignmentOptions.Center,
                    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -180f), new Vector2(0f, -130f));

        // ── 9. Reward Panel ──────────────────────────────────────
        GameObject rwdPanel = GetOrCreate(secRoot, "RewardPanel");
        rwdPanel.SetActive(false);
        manager.rewardPanel = rwdPanel;

        U8_RewardController rwd = GetOrAddComponent<U8_RewardController>(rwdPanel);
        manager.rewardController = rwd;
        rwd.manager = manager;

        if (rwdPanel.GetComponent<AudioSource>() == null)
            rwdPanel.AddComponent<AudioSource>();
        rwd.audioSource = rwdPanel.GetComponent<AudioSource>();
        rwd.victoryClip = LoadClip("Assets/Audio Clips/unit 8/You're a Consonant Explorer now.mp3");

        // RewardTitle
        GameObject rwdTitle = GetOrCreate(rwdPanel.transform, "RewardTitle");
        TextMeshProUGUI rwdTitleTMP = GetOrAddComponent<TextMeshProUGUI>(rwdTitle);
        rwdTitleTMP.text      = "CONSONANT EXPLORER!";
        rwdTitleTMP.fontSize   = 64;
        rwdTitleTMP.alignment  = TextAlignmentOptions.Center;
        rwdTitleTMP.color      = new Color(0.9f, 0.6f, 0.05f);
        SetAnchored(rwdTitle.GetComponent<RectTransform>(),
                    new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.85f));
        rwd.rewardTitleLabel = rwdTitleTMP;

        // RewardDescription
        GameObject rwdDesc = GetOrCreate(rwdPanel.transform, "RewardDescription");
        TextMeshProUGUI rwdDescTMP = GetOrAddComponent<TextMeshProUGUI>(rwdDesc);
        rwdDescTMP.text      = "You know all the consonant sounds!\nYou earned the Consonant Explorer badge! 🏅";
        rwdDescTMP.fontSize   = 30;
        rwdDescTMP.alignment  = TextAlignmentOptions.Center;
        rwdDescTMP.color      = new Color(0.2f, 0.2f, 0.2f);
        SetAnchored(rwdDesc.GetComponent<RectTransform>(),
                    new Vector2(0.05f, 0.47f), new Vector2(0.95f, 0.65f));
        rwd.rewardDescriptionLabel = rwdDescTMP;

        // BadgeIcon placeholder
        GameObject badgeObj = GetOrCreate(rwdPanel.transform, "BadgeIcon");
        Image badgeImg = GetOrAddComponent<Image>(badgeObj);
        badgeImg.color = new Color(1f, 0.85f, 0.1f, 0.85f);
        SetAnchored(badgeObj.GetComponent<RectTransform>(),
                    new Vector2(0.3f, 0.2f), new Vector2(0.7f, 0.47f));
        rwd.badgeIcon = badgeImg;

        // Continue button
        GameObject contBtn = GetOrCreate(rwdPanel.transform, "ContinueButton");
        GetOrAddComponent<Image>(contBtn).color = new Color(0.15f, 0.7f, 0.35f);
        Button contBtnComp = GetOrAddComponent<Button>(contBtn);
        SetAnchored(contBtn.GetComponent<RectTransform>(),
                    new Vector2(0.3f, 0.06f), new Vector2(0.7f, 0.17f));
        CreateChildLabel(contBtn.transform, "Label", "Continue →", 32);
        rwd.continueButton = contBtnComp;

        // ── 10. Back + Next buttons in Unit_8_Sections ──────────
        GameObject backBtn = CreateNavigationButton(secRoot, "Back_Button", "← Back",
                                                    new Vector2(0.02f, 0.01f), new Vector2(0.22f, 0.08f),
                                                    new Color(0.7f, 0.25f, 0.15f));
        manager.backButton = backBtn.GetComponent<Button>();

        GameObject nextBtn = CreateNavigationButton(secRoot, "Next_Button", "Next →",
                                                    new Vector2(0.78f, 0.01f), new Vector2(0.98f, 0.08f),
                                                    new Color(0.15f, 0.55f, 0.75f));
        manager.nextButton = nextBtn.GetComponent<Button>();

        // ── 11. Mark dirty & report ─────────────────────────────
        EditorUtility.SetDirty(unit8Root);
        EditorUtility.SetDirty(manager);
        Undo.RegisterCreatedObjectUndo(unit8Root, "Setup Unit 8 UI");

        Debug.Log("[Unit 8 Setup] ✅ All panels, controllers, and navigation wired successfully! " +
                  "Assign your ConsonantTilePrefab and ScriptableObject data in the Inspector, then hit Play.");
    }

    // ──────────────────────────────────────────────────────────
    //  Helper — Build the 4 section-selection buttons
    // ──────────────────────────────────────────────────────────

    private static void BuildSelectionButtons(Transform selParent, U8_Manager manager)
    {
        // Ensure Viewport/Content chain exists
        Transform viewport = selParent.Find("Viewport");
        if (viewport == null)
        {
            GameObject vpObj = new GameObject("Viewport", typeof(RectTransform));
            vpObj.transform.SetParent(selParent, false);
            StretchFull(vpObj.GetComponent<RectTransform>());
            viewport = vpObj.transform;
        }

        Transform content = viewport.Find("Content");
        if (content == null)
        {
            GameObject ctObj = new GameObject("Content", typeof(RectTransform));
            ctObj.transform.SetParent(viewport, false);
            RectTransform ctRT = ctObj.GetComponent<RectTransform>();
            ctRT.anchorMin = new Vector2(0f, 0f);
            ctRT.anchorMax = new Vector2(1f, 1f);
            ctRT.offsetMin = ctRT.offsetMax = Vector2.zero;

            VerticalLayoutGroup vlg = ctObj.AddComponent<VerticalLayoutGroup>();
            vlg.spacing           = 20f;
            vlg.childAlignment    = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.padding           = new RectOffset(30, 30, 30, 30);

            ContentSizeFitter csf = ctObj.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            content = ctObj.transform;
        }

        // Section button definitions
        var sections = new (string name, string label, string method)[]
        {
            ("SectionA_Button", "🔊  Consonant Sounds",   "StartSectionA"),
            ("SectionB_Button", "🐝  Buzz or Whisper?",   "StartSectionB"),
            ("SectionC_Button", "🔗  Connect the Sound",  "StartSectionC"),
            ("SectionD_Button", "🦁  Consonant Safari",   "StartSectionD"),
        };

        Color[] btnColors =
        {
            new Color(0.4f, 0.72f, 1f),
            new Color(1f, 0.82f, 0.2f),
            new Color(0.5f, 0.88f, 0.55f),
            new Color(1f, 0.58f, 0.3f),
        };

        for (int i = 0; i < sections.Length; i++)
        {
            var (name, label, method) = sections[i];

            Transform existing = content.Find(name);
            if (existing != null) continue;   // don't recreate

            GameObject btnObj = new GameObject(name, typeof(RectTransform));
            btnObj.transform.SetParent(content, false);

            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, 90f);

            Image img = btnObj.AddComponent<Image>();
            img.color = btnColors[i];

            Button btn = btnObj.AddComponent<Button>();

            // Wire the onClick via persistent listener to U8_Manager
            UnityEditor.Events.UnityEventTools.AddVoidPersistentListener(
                btn.onClick,
                (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                    typeof(UnityEngine.Events.UnityAction), manager,
                    typeof(U8_Manager).GetMethod(method)));

            TextMeshProUGUI tmp = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            tmp.transform.SetParent(btnObj.transform, false);
            RectTransform tmpRT = tmp.GetComponent<RectTransform>();
            tmpRT.anchorMin = Vector2.zero;
            tmpRT.anchorMax = Vector2.one;
            tmpRT.offsetMin = tmpRT.offsetMax = Vector2.zero;
            tmp.text      = label;
            tmp.fontSize   = 38;
            tmp.alignment  = TextAlignmentOptions.Center;
            tmp.color      = Color.white;
        }
    }

    // ──────────────────────────────────────────────────────────
    //  Utility helpers
    // ──────────────────────────────────────────────────────────

    private static GameObject GetOrCreate(Transform parent, string childName)
    {
        Transform t = parent.Find(childName);
        if (t != null) return t.gameObject;

        GameObject go = new GameObject(childName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        StretchFull(go.GetComponent<RectTransform>());
        return go;
    }

    private static T GetOrAddComponent<T>(GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        return c != null ? c : go.AddComponent<T>();
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    // Set anchors in 0-1 space (no pixel offsets)
    private static void SetAnchored(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI CreateLabel(Transform parent, string name, string text,
                                               float fontSize, TextAlignmentOptions align,
                                               Vector2 anchorMin, Vector2 anchorMax,
                                               Vector2 offsetMin, Vector2 offsetMax)
    {
        Transform existing = parent.Find(name);
        GameObject labelObj = existing != null ? existing.gameObject
                                               : new GameObject(name, typeof(RectTransform));
        if (existing == null) labelObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = GetOrAddComponent<TextMeshProUGUI>(labelObj);
        tmp.text      = text;
        tmp.fontSize   = fontSize;
        tmp.alignment  = align;
        tmp.color      = Color.black;

        RectTransform rt = labelObj.GetComponent<RectTransform>();
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = offsetMin;
        rt.offsetMax  = offsetMax;
        return tmp;
    }

    private static TextMeshProUGUI CreateChildLabel(Transform parent, string name, string text, float fontSize)
    {
        Transform existing = parent.Find(name);
        GameObject labelObj = existing != null ? existing.gameObject
                                               : new GameObject(name, typeof(RectTransform));
        if (existing == null) labelObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = GetOrAddComponent<TextMeshProUGUI>(labelObj);
        tmp.text     = text;
        tmp.fontSize  = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        RectTransform rt = labelObj.GetComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = rt.offsetMax = Vector2.zero;
        return tmp;
    }

    private static GameObject CreateNavigationButton(Transform parent, string name, string label,
                                                     Vector2 anchorMin, Vector2 anchorMax, Color colour)
    {
        GameObject btnObj = GetOrCreate(parent, name);
        Image img = GetOrAddComponent<Image>(btnObj);
        img.color = colour;
        GetOrAddComponent<Button>(btnObj);
        SetAnchored(btnObj.GetComponent<RectTransform>(), anchorMin, anchorMax);
        CreateChildLabel(btnObj.transform, "Label", label, 30);
        return btnObj;
    }

    private static AudioClip LoadClip(string path)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) Debug.LogWarning($"[Unit 8 Setup] Audio clip not found at: {path}");
        return clip;
    }
}
#endif
