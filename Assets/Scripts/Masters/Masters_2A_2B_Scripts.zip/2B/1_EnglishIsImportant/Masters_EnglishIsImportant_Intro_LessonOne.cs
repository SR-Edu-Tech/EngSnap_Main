using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


/// <summary>
/// INTRO: The World Is Calling — Lesson Controller for Book 2B Unit 1 (English Is Important).
/// 5-Step Cinematic Station Intro:
/// Step 1: A bright Global English Station — globe spins and departure board clatters through destinations (CAREER · FRIENDS · BOOKS · MOVIES · INTERNET · THE WHOLE WORLD).
/// Step 2: LEO reads his ticket aloud: 'I want to learn English for a bright future.' — the board flips to THE WHOLE WORLD.
/// Step 3: ARIA lights the three platforms in turn: REASONS (blue), SHORT & SMART (green) and TALK REPAIR (purple).
/// Step 4: ARIA previews the goal: 'Know why you learn English, say it the short smart way, and never get stuck in a conversation!'
/// Step 5: A large START button appears; tapping it transitions to the Hub.
/// </summary>
public class Masters_EnglishIsImportant_Intro_LessonOne : Masters_Lesson {

[System.Serializable]
public class EnglishIsImportant_IntroStepData {
    public string speakerName;         // e.g. "NARRATOR", "LEO", "ARIA"
    public string dialogueLine;        // Spoken dialogue / subtitle
    public string actionDescription;   // Stage action description
    public string cardHeadline;        // Center card headline
    public string cardDetail;          // Center card detail text
    public Color cardThemeColor;       // Card highlight theme color
    public AudioClip stepAudio;        // Voiceover clip
}

    [Header("5-Step The World Is Calling Intro Flow")]
    [SerializeField] private EnglishIsImportant_IntroStepData[] introSteps;

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
        WireIntroButtons();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Intro;

        PurgeLegacyChildren();
        EnsureHeaderAndTitle();
        WireIntroButtons();

        if (introSteps == null || introSteps.Length == 0) {
            PopulateDefaultSteps();
        }

        StartIntroSequence();
    }

    public override void EnsureNextAndBackButtonWired() {
        // 1. Wire BackButton safely
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (var b in allButtons) {
            if (b != null && b.name.ToLower().Contains("back")) {
                b.gameObject.SetActive(true);
                b.interactable = true;
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => {
                    if (Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                        Masters_AudioManager.Instance.StopVoiceOver();
                    }
                    if (Masters_LevelManager.Instance != null) {
                        Masters_LevelManager.Instance.OnBackButtonClicked();
                    }
                });
            }
        }

        // 2. Prevent base class from hijacking NextStepBtn as nextButton
        nextButton = null;

        // 3. Explicitly wire intro navigation buttons
        WireIntroButtons();
    }

    private void WireIntroButtons() {
        AutoBindReferences();

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

    public void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("UnitHeading") ?? transform.Find("Branch");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "ENGLISH IS IMPORTANT";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "The World Is Calling";
        }

        if (subtitleTMP != null) {
            subtitleTMP.text = "Know why you learn English, say it the short smart way, and never get stuck in a conversation!";
        }

        if (progressTMP != null) {
            progressTMP.text = "Step 1/5";
        }
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        WireIntroButtons();
        EnsureHeaderAndTitle();

        if (introSteps == null || introSteps.Length == 0) {
            PopulateDefaultSteps();
        }

        if (introSteps != null && introSteps.Length > 0) {
            DisplayStepData(introSteps[0], 0);
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
            else if (nextStepBtn == null && (n.Contains("nextstep") || n.Contains("stepnext") || n == "nextstepbtn")) nextStepBtn = btn;
            else if (prevStepBtn == null && (n.Contains("prevstep") || n.Contains("stepprev") || n.Contains("backstep"))) prevStepBtn = btn;
            else if (replayBtn == null && n.Contains("replay")) replayBtn = btn;
            else if (ariaReplayBtn == null && (n.Contains("aria") || n.Contains("character"))) ariaReplayBtn = btn;
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
        string audioDir = "Assets/Audio/2B/1_EnglishIsImportant/Intro/";

        introSteps = new EnglishIsImportant_IntroStepData[] {
            // Step 1: Global English Station
            new EnglishIsImportant_IntroStepData {
                speakerName = "NARRATOR",
                dialogueLine = "\"Welcome to Unit 1 English is Important! The World Is Calling. The globe spins and the departure board clatters through destinations: Career, Friends, Books, Movies, Internet, and The Whole World!\"",
                actionDescription = "Scene fades in on the station; the globe spins and the departure board clatters through destinations.",
                cardHeadline = "GLOBAL ENGLISH STATION",
                cardDetail = "Welcome to the Global English Station:\nCAREER | FRIENDS | BOOKS | MOVIES | INTERNET | THE WHOLE WORLD\nThree glowing platforms await!",
                cardThemeColor = new Color(0.12f, 0.42f, 0.78f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "eii_intro_01_station.mp3")
#endif
            },
            // Step 2: LEO's Ticket
            new EnglishIsImportant_IntroStepData {
                speakerName = "LEO",
                dialogueLine = "\"I want to learn English for a bright future!\"",
                actionDescription = "LEO reads his ticket aloud: 'I want to learn English for a bright future.' - the board flips to THE WHOLE WORLD.",
                cardHeadline = "TICKET TO THE WORLD",
                cardDetail = "\"I want to learn English for a bright future!\"\nThe departure board flips to: THE WHOLE WORLD!",
                cardThemeColor = new Color(0.18f, 0.55f, 0.88f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "eii_intro_02_leo_ticket.mp3")
#endif
            },
            // Step 3: Three Glowing Platforms
            new EnglishIsImportant_IntroStepData {
                speakerName = "ARIA",
                dialogueLine = "\"Look at the three glowing platforms! Blue is for Reasons, green is for Short and Smart, and purple is for Talk Repair!\"",
                actionDescription = "ARIA lights the three platforms in turn: REASONS (blue), SHORT AND SMART (green) and TALK REPAIR (purple), with sample lines playing.",
                cardHeadline = "THREE GLOWING PLATFORMS",
                cardDetail = "Three glowing platforms:\n1. REASONS (Blue Platform - p.5)\n2. SHORT AND SMART (Green Platform - pp.6-7)\n3. TALK REPAIR (Purple Platform - pp.8-9)",
                cardThemeColor = new Color(0.58f, 0.28f, 0.78f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "eii_intro_03_three_platforms.mp3")
#endif
            },
            // Step 4: Unit Goal
            new EnglishIsImportant_IntroStepData {
                speakerName = "ARIA",
                dialogueLine = "\"Know why you learn English, say it the short smart way, and never get stuck in a conversation!\"",
                actionDescription = "ARIA previews the goal: 'Know why you learn English, say it the short smart way, and never get stuck in a conversation!'",
                cardHeadline = "UNIT MISSION",
                cardDetail = "Know why you learn English, say it the short smart way, and never get stuck in a conversation!",
                cardThemeColor = new Color(0.12f, 0.75f, 0.42f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "eii_intro_04_unit_goal.mp3")
#endif
            },
            // Step 5: START Call to Action
            new EnglishIsImportant_IntroStepData {
                speakerName = "ARIA",
                dialogueLine = "\"Your ticket is ready! Tap START to begin your English journey!\"",
                actionDescription = "A large START button appears; tapping it transitions to the Hub.",
                cardHeadline = "THE WORLD IS CALLING!",
                cardDetail = "The World Is Calling!\nTap START to begin your English journey at the Hub!",
                cardThemeColor = new Color(0.95f, 0.55f, 0.12f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "eii_intro_05_start.mp3")
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

    private void DisplayStepData(EnglishIsImportant_IntroStepData step, int index) {
        if (progressTMP != null) {
            progressTMP.text = $"Step {index + 1}/{introSteps.Length}";
            progressTMP.enableAutoSizing = false;
            progressTMP.fontSize = 24f;
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
            prevStepBtn.gameObject.SetActive(true);
            prevStepBtn.interactable = (index > 0);
        }

        if (replayBtn != null) {
            replayBtn.gameObject.SetActive(true);
        }

        if (nextStepBtn != null) {
            nextStepBtn.gameObject.SetActive(!isLastStep);
        }

        PlayStepAudio(step);
    }

    private void PlayStepAudio(EnglishIsImportant_IntroStepData step) {
        if (step.stepAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(step.stepAudio);
        }
    }

    public void ReplayCurrentStep() {
        if (introSteps != null && currentStepIndex >= 0 && currentStepIndex < introSteps.Length) {
            PlayStepAudio(introSteps[currentStepIndex]);
        }
    }

    public void OnNextStepClicked() {
        if (currentStepIndex < introSteps.Length - 1) {
            LoadStep(currentStepIndex + 1);
        }
    }

    public void OnPrevStepClicked() {
        if (currentStepIndex > 0) {
            LoadStep(currentStepIndex - 1);
        }
    }

    public void OnStartUnitClicked() {
        topic = Masters_Topic.Intro;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    protected override void OnNextButtonClicked() {
        OnStartUnitClicked();
    }
}
