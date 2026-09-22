using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// Unit 10: Common Mistakes with Prepositions — Intro Lesson Controller (Nothing Broken, Just Needs a Part).
/// 5-Step Cinematic Workshop Intro:
/// Step 1: Scene fades in on the workshop; a sentence rolls in on the belt, wobbling slightly: 'We went at the mall.'
/// Step 2: LEO reads it aloud, tilts his head, and says cheerfully: 'Hmm - one part is loose.'
/// Step 3: He opens the PREPOSITIONS drawer, lifts out 'to', swaps it for 'at', and the sentence straightens: 'We went to the mall.'
/// Step 4: ARIA shows two more repairs on the belt: 'It depends from you.' -> 'It depends on you.' and 'I must to learn English.' -> 'I must learn English.'
/// Step 5: ARIA previews the goal: 'Nothing here is broken - every sentence just needs one small part swapped. Let's fix them together!' Large START button appears.
/// </summary>
public class Masters_CommonMistakesWithPrepositions_Intro_LessonOne : Masters_Lesson {

[System.Serializable]
public class CommonMistakesWithPrepositions_IntroStepData {
    public string speakerName;         // e.g. "NARRATOR", "ARIA", "LEO"
    public string dialogueLine;        // Spoken dialogue / subtitle
    public string actionDescription;   // Stage action description
    public string cardHeadline;        // Center card headline
    public string cardDetail;          // Center card detail text
    public Color cardThemeColor;       // Card highlight theme color
    public AudioClip stepAudio;        // Voiceover clip
}

    [Header("5-Step Workshop Intro Flow")]
    [SerializeField] private CommonMistakesWithPrepositions_IntroStepData[] introSteps;

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
            headerTMP.text = "COMMON MISTAKES WITH PREPOSITIONS";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "INTRO - Nothing Broken, Just Needs a Part";
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
        string audioDir = "Assets/Audio/2B/10_CommonMistakesWithPrepositions/Intro/";

        introSteps = new CommonMistakesWithPrepositions_IntroStepData[] {
            // Step 1: Workshop conveyor belt rolls in
            new CommonMistakesWithPrepositions_IntroStepData {
                speakerName = "NARRATOR",
                dialogueLine = "Welcome to Unit 10: Common Mistakes with Prepositions! Nothing Broken, Just Needs a Part. Scene fades in on the workshop; a sentence rolls in on the belt, wobbling slightly: 'We went at the mall.'",
                actionDescription = "Scene fades in on the workshop; a sentence rolls in on the belt, wobbling slightly: 'We went at the mall.'",
                cardHeadline = "NOTHING BROKEN, JUST NEEDS A PART",
                cardDetail = "A cheerful workshop with a conveyor belt:\n- Wall of labelled drawers: PREPOSITIONS, VERBS, WORD CHOICE, WORD ORDER, SMALL WORDS\n- A wobbling sentence rolls in: 'We went at the mall.'",
                cardThemeColor = new Color(0.18f, 0.35f, 0.65f, 1f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "commonmistakes_intro_01_workshop.mp3")
#endif
            },
            // Step 2: LEO inspects
            new CommonMistakesWithPrepositions_IntroStepData {
                speakerName = "LEO",
                dialogueLine = "Hmm - one part is loose.",
                actionDescription = "LEO reads it aloud, tilts his head, and says cheerfully: 'Hmm - one part is loose.'",
                cardHeadline = "ONE PART IS LOOSE",
                cardDetail = "LEO wears a toolbelt, inspects the sentence closely, tilts his head and cheerfully says:\n\"Hmm - one part is loose.\"",
                cardThemeColor = new Color(0.92f, 0.55f, 0.15f, 1f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "commonmistakes_intro_02_leo_inspects.mp3")
#endif
            },
            // Step 3: PREPOSITIONS drawer swap
            new CommonMistakesWithPrepositions_IntroStepData {
                speakerName = "NARRATOR",
                dialogueLine = "Leo opens the PREPOSITIONS drawer, lifts out 'to', swaps it for 'at', and the sentence straightens: 'We went to the mall.'",
                actionDescription = "He opens the PREPOSITIONS drawer, lifts out 'to', swaps it for 'at', and the sentence straightens: 'We went to the mall.'",
                cardHeadline = "DRAWER SWAP: PREPOSITIONS",
                cardDetail = "LEO opens the PREPOSITIONS drawer:\n- Lifts out 'to'\n- Swaps it for 'at'\n- Sentence straightens: \"We went to the mall.\"",
                cardThemeColor = new Color(0.12f, 0.72f, 0.42f, 1f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "commonmistakes_intro_03_drawer_swap.mp3")
#endif
            },
            // Step 4: ARIA shows two more repairs
            new CommonMistakesWithPrepositions_IntroStepData {
                speakerName = "ARIA",
                dialogueLine = "Look at two more repairs on the belt: 'It depends from you' becomes 'It depends on you.' And 'I must to learn English' becomes 'I must learn English!'",
                actionDescription = "ARIA shows two more repairs on the belt: 'It depends from you.' -> 'It depends on you.' and 'I must to learn English.' -> 'I must learn English.'",
                cardHeadline = "MORE QUICK REPAIRS",
                cardDetail = "ARIA shows two more sentence repairs on the conveyor belt:\n- \"It depends from you.\" -> \"It depends on you.\"\n- \"I must to learn English.\" -> \"I must learn English.\"",
                cardThemeColor = new Color(0.58f, 0.28f, 0.78f, 1f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "commonmistakes_intro_04_aria_repairs.mp3")
#endif
            },
            // Step 5: ARIA's Goal + START Button
            new CommonMistakesWithPrepositions_IntroStepData {
                speakerName = "ARIA",
                dialogueLine = "Nothing here is broken - every sentence just needs one small part swapped. Let's fix them together! Tap START to begin!",
                actionDescription = "ARIA previews the goal: 'Nothing here is broken - every sentence just needs one small part swapped. Let's fix them together!' Large START button appears.",
                cardHeadline = "YOUR UNIT GOAL",
                cardDetail = "\"Nothing here is broken - every sentence just needs one small part swapped. Let's fix them together!\"\n\nTap START to begin Unit 10!",
                cardThemeColor = new Color(0.2f, 0.65f, 0.35f, 1f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "commonmistakes_intro_05_aria_goal.mp3")
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

    private void DisplayStepData(CommonMistakesWithPrepositions_IntroStepData step, int index) {
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
