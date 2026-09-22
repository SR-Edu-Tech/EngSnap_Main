using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


/// <summary>
/// Unit 3: Household Chores — Intro Lesson Controller (Diwali Clean-up).
/// 5-Step Cinematic House Intro:
/// Step 1: Three Days to Diwali — Calendar flips and a festive lamp lights on the porch.
/// Step 2: Mom looks around the rooms — So much to do and such little time!
/// Step 3: Leo steps up with broom & bucket — Kitchen, Bedroom, Washing Corner, Garden, Driveway.
/// Step 4: ARIA shows essential chores — Sweep, Wash up, Hang out, Gardening, Wash car.
/// Step 5: ARIA previews unit goal + START button appears to enter Hub.
/// </summary>
public class Masters_HouseholdChores_Intro_LessonOne : Masters_Lesson {


[System.Serializable]
public class HouseholdChores_IntroStepData {
    public string speakerName;         // e.g. "NARRATOR", "MOM", "LEO", "ARIA"
    public string dialogueLine;        // Spoken dialogue / subtitle
    public string actionDescription;   // Stage action description
    public string cardHeadline;        // Center card headline
    public string cardDetail;          // Center card detail text
    public Color cardThemeColor;       // Card highlight theme color
    public AudioClip stepAudio;        // Voiceover clip
}

    [Header("5-Step Household Chores Intro Flow")]
    [SerializeField] private HouseholdChores_IntroStepData[] introSteps;

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
            headerTMP.text = "HOUSEHOLD CHORES";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "INTRO \u2014 Three Days to Diwali";
            titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
        }
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "Cloud", "Cloud (1)", "Cloud (2)", "Cloud (3)", "Cloud (4)",
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
        string audioDir = "Assets/Audio/2B/3_HouseholdChores/Intro/";

        introSteps = new HouseholdChores_IntroStepData[] {
            new HouseholdChores_IntroStepData {
                speakerName = "NARRATOR",
                dialogueLine = "Welcome to Unit 3 Household Chores! In a warm family home, the calendar flips—Diwali is just three days away, and a festive lamp lights on the porch.",
                actionDescription = "Scene fades in on the house; the calendar flips and a Diwali lamp lights on the porch.",
                cardHeadline = "Three Days to Diwali",
                cardDetail = "Diwali is coming! The family home needs a sparkling clean-up before the festival.",
                cardThemeColor = new Color(0.18f, 0.35f, 0.65f, 1f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_intro_step1.mp3")
#endif
            },
            new HouseholdChores_IntroStepData {
                speakerName = "MOM",
                dialogueLine = "Oh dear, look around the rooms! There is so much to do and such little time before Diwali!",
                actionDescription = "Mom looks around the rooms: there's so much to do and such a little time.",
                cardHeadline = "So Much To Do!",
                cardDetail = "Kitchen, bedroom, washing corner, garden, and driveway all need attention.",
                cardThemeColor = new Color(0.65f, 0.25f, 0.45f, 1f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_intro_step2.mp3")
#endif
            },
            new HouseholdChores_IntroStepData {
                speakerName = "LEO",
                dialogueLine = "Don't worry, Mom! Today I can help you with the cleaning of our house!",
                actionDescription = "LEO steps up with a broom and bucket — the five areas glow one by one.",
                cardHeadline = "LEO Steps Up to Help",
                cardDetail = "5 Areas: 1. Kitchen · 2. Bedroom · 3. Washing Corner · 4. Garden · 5. Driveway",
                cardThemeColor = new Color(0.2f, 0.6f, 0.35f, 1f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_intro_step3.mp3")
#endif
            },
            new HouseholdChores_IntroStepData {
                speakerName = "ARIA",
                dialogueLine = "Let's see the jobs: Sweep the floor, wash up the dishes, hang out the clothes, do the gardening, and wash the car!",
                actionDescription = "ARIA shows sample chores in each area.",
                cardHeadline = "Essential Household Chores",
                cardDetail = "• Sweep the floor\n• Wash up the dishes\n• Hang out the clothes\n• Do the gardening\n• Wash the car",
                cardThemeColor = new Color(0.7f, 0.5f, 0.15f, 1f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_intro_step4.mp3")
#endif
            },
            new HouseholdChores_IntroStepData {
                speakerName = "ARIA",
                dialogueLine = "Learn every chore, say it the right way, and help your family before the festival! Tap START to begin!",
                actionDescription = "ARIA previews the goal: Large START button appears.",
                cardHeadline = "Your Unit Goal",
                cardDetail = "Master all 17 household chores, perfect your English, and help your family get ready for Diwali!",
                cardThemeColor = new Color(0.55f, 0.2f, 0.75f, 1f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "hc_intro_step5.mp3")
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

    private void DisplayStepData(HouseholdChores_IntroStepData step, int index) {
        if (progressTMP != null) {
            progressTMP.text = $"Step {index + 1}/{introSteps.Length}";
        }

        if (speakerNameTMP != null) {
            speakerNameTMP.text = step.speakerName;
            if (step.speakerName.Contains("ARIA")) {
                speakerNameTMP.color = new Color(1f, 0.88f, 0.25f);
            } else if (step.speakerName.Contains("LEO")) {
                speakerNameTMP.color = new Color(0.3f, 0.85f, 1f);
            } else if (step.speakerName.Contains("MOM")) {
                speakerNameTMP.color = new Color(0.95f, 0.55f, 0.95f);
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
