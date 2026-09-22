using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


/// <summary>
/// INTRO: A Thank-You for Everyone — Lesson Controller for Book 2B Unit 2 (Courtesy Call).
/// 5-Step Cinematic Intro:
/// Step 1: Scene fades in on Kindness Corner; the Thank-You tree lights up tag by tag.
/// Step 2: LEO turns to a neighbour: 'Thank you so much for the birthday gift.' — a gift tag glows on the tree.
/// Step 3: A passer-by drops a wallet; LEO calls 'Excuse me sir, you dropped your wallet.' — the crossing lights green.
/// Step 4: LEO arrives late to the bench: 'I'm sorry for being so late.' — the fountain turns from grey to bright.
/// Step 5: ARIA previews the goal: 'Say thank you, say sorry, say excuse me — and say how you really feel!' A large START button appears; tapping it transitions to the Hub.
/// </summary>
public class Masters_CourtesyCall_Intro_LessonOne : Masters_Lesson {

[System.Serializable]
public class CourtesyCall_IntroStepData {
    public string speakerName;         // e.g. "NARRATOR", "LEO", "ARIA"
    public string dialogueLine;        // Spoken dialogue / subtitle
    public string actionDescription;   // Stage action description
    public string cardHeadline;        // Center card headline
    public string cardDetail;          // Center card detail text
    public Color cardThemeColor;       // Card highlight theme color
    public AudioClip stepAudio;        // Voiceover clip
}

    [Header("5-Step A Thank-You for Everyone Intro Flow")]
    [SerializeField] private CourtesyCall_IntroStepData[] introSteps;

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
            headerTMP.text = "COURTESY CALL";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "A Thank-You for Everyone";
        }

        if (subtitleTMP != null) {
            subtitleTMP.text = "Say thank you, say sorry, say excuse me — and say how you really feel!";
        }

        if (progressTMP != null) {
            progressTMP.text = "Step 1/5";
        }
    }

    private void OnValidate() {
        if (!Application.isPlaying) {
            UpdateEditorPreview();
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
        string audioDir = "Assets/Audio/2B/2_CourtesyCall/Intro/";

        introSteps = new CourtesyCall_IntroStepData[] {
            // Step 1: Kindness Corner
            new CourtesyCall_IntroStepData {
                speakerName = "NARRATOR",
                dialogueLine = "\"Welcome to Kindness Corner! A warm neighbourhood square where the Thank-You tree lights up tag by tag.\"",
                actionDescription = "Scene fades in on Kindness Corner; the Thank-You tree lights up tag by tag.",
                cardHeadline = "KINDNESS CORNER",
                cardDetail = "Welcome to Kindness Corner!\nThank-You Tree | Sorry Bench | Excuse-Me Crossing | Feelings Fountain\nA warm neighbourhood of polite words!",
                cardThemeColor = new Color(0.12f, 0.45f, 0.78f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cc_intro_01_kindness_corner.mp3")
#endif
            },
            // Step 2: Thank-You Tree
            new CourtesyCall_IntroStepData {
                speakerName = "LEO",
                dialogueLine = "\"Thank you so much for the birthday gift.\"",
                actionDescription = "LEO turns to a neighbour: 'Thank you so much for the birthday gift.' — a gift tag glows on the tree.",
                cardHeadline = "THE THANK-YOU TREE",
                cardDetail = "LEO turns to a neighbour:\n\"Thank you so much for the birthday gift.\"\nA glowing gift tag lights up the Thank-You tree!",
                cardThemeColor = new Color(0.85f, 0.55f, 0.12f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cc_intro_02_leo_thankyou.mp3")
#endif
            },
            // Step 3: Excuse-Me Crossing
            new CourtesyCall_IntroStepData {
                speakerName = "LEO",
                dialogueLine = "\"Excuse me sir, you dropped your wallet.\"",
                actionDescription = "A passer-by drops a wallet; LEO calls 'Excuse me sir, you dropped your wallet.' — the crossing lights green.",
                cardHeadline = "EXCUSE-ME CROSSING",
                cardDetail = "A passer-by drops a wallet:\n\"Excuse me sir, you dropped your wallet.\"\nThe crossing lights up bright green!",
                cardThemeColor = new Color(0.12f, 0.72f, 0.38f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cc_intro_03_leo_excuseme.mp3")
#endif
            },
            // Step 4: The Sorry Bench
            new CourtesyCall_IntroStepData {
                speakerName = "LEO",
                dialogueLine = "\"I'm sorry for being so late.\"",
                actionDescription = "LEO arrives late to the bench: 'I'm sorry for being so late.' — the fountain turns from grey to bright.",
                cardHeadline = "THE SORRY BENCH",
                cardDetail = "LEO arrives at the bench:\n\"I'm sorry for being so late.\"\nThe Feelings Fountain turns from grey to bright colours!",
                cardThemeColor = new Color(0.55f, 0.25f, 0.78f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cc_intro_04_leo_sorry.mp3")
#endif
            },
            // Step 5: Unit Goal
            new CourtesyCall_IntroStepData {
                speakerName = "ARIA",
                dialogueLine = "\"Say thank you, say sorry, say excuse me — and say how you really feel! Tap START to enter the unit.\"",
                actionDescription = "ARIA previews the goal: 'Say thank you, say sorry, say excuse me — and say how you really feel!' A large START button appears; tapping it transitions to the Hub.",
                cardHeadline = "UNIT GOAL",
                cardDetail = "Say thank you, say sorry, say excuse me — and say how you really feel!\nTap START to enter the unit!",
                cardThemeColor = new Color(0.95f, 0.45f, 0.15f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cc_intro_05_aria_goal.mp3")
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

    private void DisplayStepData(CourtesyCall_IntroStepData step, int index) {
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

    private void PlayStepAudio(CourtesyCall_IntroStepData step) {
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
