using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// Unit 9: Prepositions of Time — Intro Lesson Controller (Three Rings of Time).
/// 5-Step Cinematic Time Intro:
/// Step 1: Clock Town seen from above - three glowing rings drawn exactly like the book's diagram.
/// Step 2: The outer ring lights first - January, 1947, Spring, The 1980s, The future - and the word IN carves itself above it.
/// Step 3: The middle ring lights next - Tuesday, Christmas Day, 10th of August, Fourth of July - and ON appears.
/// Step 4: The clock tower at the centre lights last - 2.30 pm, 4 o'clock, Noon, Sunrise, Dinner time - and AT appears.
/// Step 5: ARIA gives the rule in one line: 'The wider the time, the wider the ring. IN is a long stretch, ON is a day, AT is a moment.' Large START button appears.
/// </summary>
public class Masters_PrepositionsOfTime_Intro_LessonOne : Masters_Lesson {

[System.Serializable]
public class PrepositionsOfTime_IntroStepData {
    public string speakerName;         // e.g. "NARRATOR", "ARIA", "LEO"
    public string dialogueLine;        // Spoken dialogue / subtitle
    public string actionDescription;   // Stage action description
    public string cardHeadline;        // Center card headline
    public string cardDetail;          // Center card detail text
    public Color cardThemeColor;       // Card highlight theme color
    public AudioClip stepAudio;        // Voiceover clip
}

    [Header("5-Step Prepositions of Time Intro Flow")]
    [SerializeField] private PrepositionsOfTime_IntroStepData[] introSteps;

    [Header("UI Display References")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;

    [Header("Speaker & Dialogue Box")]
    [SerializeField] private GameObject dialogueContainer;
    [SerializeField] private TextMeshProUGUI speakerNameTMP;
    [SerializeField] private TextMeshProUGUI dialogueTextTMP;
    [SerializeField] private TextMeshProUGUI actionTextTMP;

    [Header("Center Stage Display Card")]
    [SerializeField] private GameObject stageCardObject;
    [SerializeField] private Image stageCardBg;
    [SerializeField] private TextMeshProUGUI cardHeadlineTMP;
    [SerializeField] private TextMeshProUGUI cardDetailTMP;

    [Header("Navigation & Call-To-Action")]
    [SerializeField] private Button startUnitBtn;
    [SerializeField] private Button nextStepBtn;
    [SerializeField] private Button prevStepBtn;
    [SerializeField] private Button replayBtn;
    [SerializeField] private Button ariaReplayBtn;

    private int currentStepIndex = 0;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Intro;

        PurgeLegacyChildren();
        AutoBindReferences();
        WireAllNavigationButtons();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Intro;

        PurgeLegacyChildren();
        AutoBindReferences();
        WireAllNavigationButtons();
        EnsureHeaderAndTitle();

        if (introSteps == null || introSteps.Length == 0) {
            PopulateDefaultSteps();
        }

        StartIntroSequence();
    }

    private void WireAllNavigationButtons() {
        if (startUnitBtn != null) {
            startUnitBtn.onClick.RemoveAllListeners();
            startUnitBtn.onClick.AddListener(OnStartUnitClicked);
        }

        if (nextStepBtn != null) {
            nextStepBtn.onClick.RemoveAllListeners();
            nextStepBtn.onClick.AddListener(OnNextStepClicked);
        }

        if (prevStepBtn != null) {
            prevStepBtn.onClick.RemoveAllListeners();
            prevStepBtn.onClick.AddListener(OnPrevStepClicked);
        }

        if (replayBtn != null) {
            replayBtn.onClick.RemoveAllListeners();
            replayBtn.onClick.AddListener(ReplayCurrentStep);
        }

        if (ariaReplayBtn != null) {
            ariaReplayBtn.onClick.RemoveAllListeners();
            ariaReplayBtn.onClick.AddListener(ReplayCurrentStep);
        }
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        AutoBindReferences();
        EnsureHeaderAndTitle();

        if (introSteps == null || introSteps.Length == 0) {
            PopulateDefaultSteps();
        }

        if (introSteps != null && introSteps.Length > 0) {
            DisplayStepData(introSteps[0], 0);
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("UnitHeading") ?? transform.Find("Branch");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "PREPOSITIONS OF TIME";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "INTRO - Three Rings of Time";
            titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
        }
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "Cloud", "Cloud (1)", "Cloud (2)", "Cloud (3)", "Cloud (4)", "Cloud (5)", "Cloud (6)", "Cloud (7)",
            "Cloud (8)", "Cloud (9)", "Cloud (10)", "Cloud (11)", "Cloud (12)", "Cloud (13)", "Cloud (14)",
            "Cloud (15)", "Cloud (16)", "Cloud (17)", "Cloud (18)", "Cloud (19)", "Cloud (20)", "Cloud (21)",
            "cloud grid", "Objects scroll view",
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer",
            "SpawnArea", "GamePlayGameObject"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(lTrans.gameObject);
                else DestroyImmediate(lTrans.gameObject);
            }
        }
    }

    public void AutoBindReferences() {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading") || n.Contains("unitheading"))) headerTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("unittmp") || n == "title")) titleTMP = tmp;
            else if (subtitleTMP == null && (n.Contains("subtitle") || n.Contains("instruction"))) subtitleTMP = tmp;
            else if (progressTMP == null && (n.Contains("progress") || n.Contains("counter") || n.Contains("stepcount"))) progressTMP = tmp;
            else if (speakerNameTMP == null && n.Contains("speaker")) speakerNameTMP = tmp;
            else if (dialogueTextTMP == null && (n.Contains("dialogue") || n.Contains("line"))) dialogueTextTMP = tmp;
            else if (actionTextTMP == null && (n.Contains("action") || n.Contains("description"))) actionTextTMP = tmp;
            else if (cardHeadlineTMP == null && (n.Contains("headline") || n.Contains("cardtitle"))) cardHeadlineTMP = tmp;
            else if (cardDetailTMP == null && (n.Contains("carddetail") || n.Contains("cardsub"))) cardDetailTMP = tmp;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (startUnitBtn == null && (n.Contains("start") || n.Contains("enter") || n.Contains("hub"))) startUnitBtn = btn;
            else if (nextStepBtn == null && (n.Contains("nextstep") || n.Contains("stepnext") || n.Contains("forward"))) nextStepBtn = btn;
            else if (prevStepBtn == null && (n.Contains("prevstep") || n.Contains("stepprev") || n.Contains("prev") || n.Contains("backstep"))) prevStepBtn = btn;
            else if (replayBtn == null && (n == "replaybtn" || n.Contains("replay"))) replayBtn = btn;
            else if (ariaReplayBtn == null && (n.Contains("aria") || n.Contains("character") || n == "image")) ariaReplayBtn = btn;
            else if (nextButton == null && n == "nextbutton") nextButton = btn;
        }

        if (stageCardObject == null) {
            Transform cardTrans = transform.Find("StudioCard") ?? transform.Find("CenterCard") ?? transform.Find("StageCard") ?? transform.Find("Card");
            if (cardTrans != null) {
                stageCardObject = cardTrans.gameObject;
                stageCardBg = cardTrans.GetComponent<Image>();
            }
        }
    }

    public void PopulateDefaultSteps() {
        string audioDir = "Assets/Audio/2B/9_PrepositionsOfTime/Intro/";

        introSteps = new PrepositionsOfTime_IntroStepData[] {
            // Step 1: Three Rings of Time Intro
            new PrepositionsOfTime_IntroStepData {
                speakerName = "NARRATOR",
                dialogueLine = "Welcome to Unit 9: Prepositions of Time! Three Rings of Time. Scene fades in on the three rings, dark and still.",
                actionDescription = "Clock Town seen from above - three glowing rings drawn exactly like the book's diagram.",
                cardHeadline = "THREE RINGS OF TIME",
                cardDetail = "Clock Town from above - Three glowing rings of time:\n- Wide Outer Ring (Seasons, Years and Long Stretches)\n- Middle Ring (Calendar Days and Dates)\n- Small Centre Clock Tower (Exact Moments)",
                cardThemeColor = new Color(0.18f, 0.35f, 0.65f, 1f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "prepositionsoftime_intro_01_three_rings.mp3")
#endif
            },
            // Step 2: Outer ring - IN
            new PrepositionsOfTime_IntroStepData {
                speakerName = "NARRATOR",
                dialogueLine = "The outer ring lights first: January, 1947, Spring, the 1980s, the future - and the word IN carves itself above it!",
                actionDescription = "The wide outer ring lights first, holding seasons, years and long stretches, and IN carves above it.",
                cardHeadline = "OUTER RING: IN",
                cardDetail = "Wide Outer Ring:\n- January, 1947, Spring, The 1980s, The future\nWord IN lights up above the ring!",
                cardThemeColor = new Color(0.15f, 0.45f, 0.85f, 1f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "prepositionsoftime_intro_02_outer_ring_in.mp3")
#endif
            },
            // Step 3: Middle ring - ON
            new PrepositionsOfTime_IntroStepData {
                speakerName = "NARRATOR",
                dialogueLine = "The middle ring lights next: Tuesday, Christmas Day, 10th of August, Fourth of July - and the word ON appears!",
                actionDescription = "The middle ring lights next, holding calendar days and dates, and ON appears.",
                cardHeadline = "MIDDLE RING: ON",
                cardDetail = "Middle Ring:\n- Tuesday, Christmas Day, 10th of August, Fourth of July\nWord ON lights up!",
                cardThemeColor = new Color(0.85f, 0.45f, 0.15f, 1f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "prepositionsoftime_intro_03_middle_ring_on.mp3")
#endif
            },
            // Step 4: Centre clock tower - AT
            new PrepositionsOfTime_IntroStepData {
                speakerName = "NARRATOR",
                dialogueLine = "The clock tower at the centre lights last: 2.30 pm, 4 o'clock, Noon, Sunrise, Dinner time - and the word AT appears!",
                actionDescription = "The clock tower at the small centre lights last with hands on 4 o'clock, and AT appears.",
                cardHeadline = "CENTRE TOWER: AT",
                cardDetail = "Centre Clock Tower:\n- 2.30 pm, 4 o'clock, Noon, Sunrise, Dinner time\nWord AT lights up!",
                cardThemeColor = new Color(0.55f, 0.25f, 0.75f, 1f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "prepositionsoftime_intro_04_centre_clock_at.mp3")
#endif
            },
            // Step 5: ARIA's Rule + START
            new PrepositionsOfTime_IntroStepData {
                speakerName = "ARIA",
                dialogueLine = "The wider the time, the wider the ring. IN is a long stretch, ON is a day, AT is a moment! Tap START to begin!",
                actionDescription = "ARIA gives the rule in one line. A large START button appears!",
                cardHeadline = "ARIA'S TIME RULE",
                cardDetail = "\"The wider the time, the wider the ring!\n- IN is a long stretch\n- ON is a day\n- AT is a moment\"\nTap START to enter the unit!",
                cardThemeColor = new Color(0.2f, 0.65f, 0.35f, 1f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "prepositionsoftime_intro_05_aria_rule.mp3")
#endif
            }
        };
    }

    public void StartIntroSequence() {
        currentStepIndex = 0;
        LoadStep(currentStepIndex);
    }

    public void LoadStep(int stepIndex) {
        if (introSteps == null || introSteps.Length == 0) return;

        currentStepIndex = Mathf.Clamp(stepIndex, 0, introSteps.Length - 1);
        var step = introSteps[currentStepIndex];

        DisplayStepData(step, currentStepIndex);
    }

    private void DisplayStepData(PrepositionsOfTime_IntroStepData step, int index) {
        if (progressTMP != null) {
            progressTMP.text = $"Step {index + 1}/{introSteps.Length}";
        }

        if (speakerNameTMP != null) {
            speakerNameTMP.text = step.speakerName;
            if (step.speakerName.Contains("ARIA")) {
                speakerNameTMP.color = new Color(1f, 0.88f, 0.25f);
            } else if (step.speakerName.Contains("LEO")) {
                speakerNameTMP.color = new Color(0.3f, 0.85f, 1f);
            } else {
                speakerNameTMP.color = new Color(1f, 1f, 1f);
            }
        }

        if (dialogueTextTMP != null) {
            dialogueTextTMP.text = step.dialogueLine;
        }

        if (actionTextTMP != null) {
            actionTextTMP.text = step.actionDescription;
        }

        if (cardHeadlineTMP != null) {
            cardHeadlineTMP.text = step.cardHeadline;
        }

        if (cardDetailTMP != null) {
            cardDetailTMP.text = step.cardDetail;
        }

        if (stageCardBg != null) {
            stageCardBg.DOColor(step.cardThemeColor, 0.25f);
        }

        if (stageCardObject != null) {
            stageCardObject.transform.DOKill();
            stageCardObject.transform.localScale = Vector3.one * 0.95f;
            stageCardObject.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }

        bool isLastStep = (index == introSteps.Length - 1);

        if (startUnitBtn != null) {
            startUnitBtn.gameObject.SetActive(true);
            if (isLastStep) {
                startUnitBtn.transform.DOKill();
                startUnitBtn.transform.DOScale(Vector3.one * 1.1f, 0.5f).SetLoops(-1, LoopType.Yoyo);
            } else {
                startUnitBtn.transform.DOKill();
                startUnitBtn.transform.localScale = Vector3.one;
            }
        }

        if (prevStepBtn != null) {
            prevStepBtn.interactable = (index > 0);
        }

        if (nextStepBtn != null) {
            nextStepBtn.gameObject.SetActive(!isLastStep);
        }

        if (Application.isPlaying) {
            if (step.stepAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(step.stepAudio);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            }
        }
    }

    private void OnNextStepClicked() {
        if (currentStepIndex < introSteps.Length - 1) {
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            LoadStep(currentStepIndex + 1);
        }
    }

    private void OnPrevStepClicked() {
        if (currentStepIndex > 0) {
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            LoadStep(currentStepIndex - 1);
        }
    }

    public void ReplayCurrentStep() {
        if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        LoadStep(currentStepIndex);
    }

    public void OnStartUnitClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Listening);
        }

        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Intro);
        } else {
            OnNextButtonClicked();
        }
    }

    protected override void OnNextButtonClicked() {
        OnStartUnitClicked();
    }
}
