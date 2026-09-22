using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


/// <summary>
/// Unit 6: Travel Fun - Intro Lesson Controller (The Journey Begins).
/// 5-Step Cinematic Journey Intro (All text sanitized with standard font characters - no emojis).
/// </summary>
public class Masters_TravelFun_Intro_LessonOne : Masters_Lesson {

[System.Serializable]
public class TravelFun_IntroStepData {
    public string speakerName;         // e.g. "LEO", "ARIA", "NARRATOR"
    public string dialogueLine;        // Spoken dialogue / subtitle
    public string actionDescription;   // Stage action description
    public string cardHeadline;        // Center card headline
    public string cardDetail;          // Center card detail text
    public Color cardThemeColor;       // Card highlight theme color
    public AudioClip stepAudio;        // Voiceover clip
}

    [Header("5-Step Travel Fun Intro Flow")]
    [SerializeField] private TravelFun_IntroStepData[] introSteps;

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

    [Header("HUD Controls")]
    [SerializeField] private Button hudBackButton;
    [SerializeField] private Button hudNextButton;

    [Header("Navigation & Call-To-Action")]
    [SerializeField] private Button startUnitBtn;
    [SerializeField] private Button nextStepBtn;
    [SerializeField] private Button prevStepBtn;
    [SerializeField] private Button replayBtn;
    [SerializeField] private Button ariaReplayBtn;

    private int currentStepIndex = 0;
    private AudioSource localAudioSource;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Intro;

        localAudioSource = GetComponent<AudioSource>();
        if (localAudioSource == null) {
            localAudioSource = gameObject.AddComponent<AudioSource>();
            localAudioSource.playOnAwake = false;
        }

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

    public override void EnsureNextAndBackButtonWired() {
        base.EnsureNextAndBackButtonWired();
        WireAllNavigationButtons();
    }

    private void WireAllNavigationButtons() {
        // Find HUD Back button
        if (hudBackButton == null) {
            Transform bt = transform.Find("CommonHUD/BackButton") ?? transform.Find("BackButton") ?? transform.Find("HeaderContainer/BackButton");
            if (bt != null) hudBackButton = bt.GetComponent<Button>();
        }

        if (hudBackButton != null) {
            hudBackButton.gameObject.SetActive(true);
            hudBackButton.interactable = true;
            hudBackButton.onClick.RemoveAllListeners();
            hudBackButton.onClick.AddListener(OnHudBackButtonClicked);
        }

        // Find HUD Next button
        if (hudNextButton == null) {
            Transform nt = transform.Find("CommonHUD/NextButton") ?? transform.Find("NextButton") ?? transform.Find("HeaderContainer/NextButton");
            if (nt != null) hudNextButton = nt.GetComponent<Button>();
        }

        if (hudNextButton != null) {
            hudNextButton.gameObject.SetActive(true);
            hudNextButton.interactable = true;
            hudNextButton.onClick.RemoveAllListeners();
            hudNextButton.onClick.AddListener(OnHudNextButtonClicked);
        }

        if (nextButton == null && hudNextButton != null) {
            nextButton = hudNextButton;
        }

        // On-screen CTA buttons
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
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("HeaderContainer/Header") ?? transform.Find("Header") ?? transform.Find("UnitHeading") ?? transform.Find("Branch");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "TRAVEL FUN";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle/TMP") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "INTRO - The Journey Begins";
            titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
        }

        if (subtitleTMP == null) {
            Transform sTrans = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Instruction") ?? transform.Find("Subtitle");
            if (sTrans != null) subtitleTMP = sTrans.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP != null) {
            subtitleTMP.text = "Watch Leo and Aria embark on an exciting travel journey!";
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
            if (n == "backbutton") hudBackButton = btn;
            else if (n == "nextbutton") hudNextButton = btn;
            else if (startUnitBtn == null && (n.Contains("start") || n.Contains("enter") || n.Contains("hub"))) startUnitBtn = btn;
            else if (nextStepBtn == null && (n.Contains("nextstep") || n.Contains("stepnext") || n.Contains("forward"))) nextStepBtn = btn;
            else if (prevStepBtn == null && (n.Contains("prevstep") || n.Contains("stepprev") || n.Contains("prev") || n.Contains("backstep"))) prevStepBtn = btn;
            else if (replayBtn == null && (n == "replaybtn" || n.Contains("replay"))) replayBtn = btn;
            else if (ariaReplayBtn == null && (n.Contains("aria") || n.Contains("owl") || n.Contains("character") || n == "image")) ariaReplayBtn = btn;
        }

        if (stageCardObject == null) {
            Transform cardTrans = transform.Find("StudioCard") ?? transform.Find("CenterCard") ?? transform.Find("StageCard") ?? transform.Find("ClinicCard") ?? transform.Find("Card");
            if (cardTrans != null) {
                stageCardObject = cardTrans.gameObject;
                stageCardBg = cardTrans.GetComponent<Image>();
            }
        }
    }

    public void PopulateDefaultSteps() {
        string audioDir = "Assets/Audio/2B/6_TravelFun/Intro/";

        introSteps = new TravelFun_IntroStepData[] {
            new TravelFun_IntroStepData {
                speakerName = "NARRATOR",
                dialogueLine = "\"A sunny morning at Leo's front door! The suitcase is packed and the journey map unrolls across the screen.\"",
                actionDescription = "Scene fades in on the front door; LEO zips up his suitcase as the journey map unrolls.",
                cardHeadline = "THE JOURNEY BEGINS",
                cardDetail = "House -> Station Platform -> Hotel Desk -> Stop-over City -> Runway -> Beach",
                cardThemeColor = new Color(0.12f, 0.28f, 0.54f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_intro_step1.mp3")
#endif
            },
            new TravelFun_IntroStepData {
                speakerName = "ARIA",
                dialogueLine = "\"The family waves at the platform! ARIA labels the moment: SEE OFF. 'They have gone to the airport to see their son off.'\"",
                actionDescription = "The family waves at the platform - ARIA labels the moment SEE OFF, and the book's example plays.",
                cardHeadline = "SEE OFF - STATION PLATFORM",
                cardDetail = "\"They have gone to the airport to see their son off.\"\nSaying goodbye to someone as they begin their trip.",
                cardThemeColor = new Color(0.11f, 0.45f, 0.35f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_intro_step2.mp3")
#endif
            },
            new TravelFun_IntroStepData {
                speakerName = "ARIA",
                dialogueLine = "\"The map lights up the next stops in turn: SET OFF, GET ON, HOLD UP, GET IN, and CHECK IN!\"",
                actionDescription = "The map lights up the next stops in turn: SET OFF, GET ON, HOLD UP, GET IN, CHECK IN - each with its example sentence.",
                cardHeadline = "JOURNEY PHRASAL VERBS",
                cardDetail = "• SET OFF (Begin a journey)\n• GET ON (Board a train/plane)\n• HOLD UP (Delay)\n• GET IN (Arrive)\n• CHECK IN (Register at desk)",
                cardThemeColor = new Color(0.85f, 0.45f, 0.12f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_intro_step3.mp3")
#endif
            },
            new TravelFun_IntroStepData {
                speakerName = "ARIA",
                dialogueLine = "\"The runway lights flash for TAKE OFF and TOUCH DOWN, and the beach at the end glows for GET AWAY!\"",
                actionDescription = "The runway lights flash: TAKE OFF, then TOUCH DOWN, and the beach at the end of the map glows for GET AWAY.",
                cardHeadline = "FLIGHT & DESTINATION",
                cardDetail = "• TAKE OFF (Leave the ground)\n• TOUCH DOWN (Land smoothly)\n• GET AWAY (Go away on holiday / escape)",
                cardThemeColor = new Color(0.0f, 0.52f, 0.65f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_intro_step4.mp3")
#endif
            },
            new TravelFun_IntroStepData {
                speakerName = "ARIA",
                dialogueLine = "\"Fourteen little phrases and you can talk about any journey in the world! Tap START to begin!\"",
                actionDescription = "ARIA previews the goal: 'Fourteen little phrases and you can talk about any journey in the world!' A large START button appears.",
                cardHeadline = "TALK ABOUT ANY JOURNEY!",
                cardDetail = "\"Fourteen little phrases and you can talk about any journey in the world!\"\n\nTap START to enter the Unit Hub!",
                cardThemeColor = new Color(0.55f, 0.18f, 0.65f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_intro_step5.mp3")
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

    private void DisplayStepData(TravelFun_IntroStepData step, int index) {
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
            startUnitBtn.gameObject.SetActive(isLastStep);
            if (isLastStep) {
                startUnitBtn.transform.DOKill();
                startUnitBtn.transform.localScale = Vector3.one * 0.9f;
                startUnitBtn.transform.DOScale(Vector3.one * 1.05f, 0.5f).SetLoops(-1, LoopType.Yoyo);
            } else {
                startUnitBtn.transform.DOKill();
                startUnitBtn.transform.localScale = Vector3.one;
            }
        }

        if (prevStepBtn != null) {
            prevStepBtn.gameObject.SetActive(index > 0);
            prevStepBtn.interactable = (index > 0);
        }

        if (nextStepBtn != null) {
            nextStepBtn.gameObject.SetActive(!isLastStep);
        }

        PlayStepAudio(step);
    }

    private void PlayStepAudio(TravelFun_IntroStepData step) {
        if (!Application.isPlaying) return;

        if (step != null && step.stepAudio != null) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(step.stepAudio);
            } else {
                if (localAudioSource == null) localAudioSource = GetComponent<AudioSource>();
                if (localAudioSource != null) {
                    localAudioSource.Stop();
                    localAudioSource.clip = step.stepAudio;
                    localAudioSource.Play();
                }
            }
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }
    }

    public void OnNextStepClicked() {
        if (currentStepIndex < introSteps.Length - 1) {
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            LoadStep(currentStepIndex + 1);
        } else {
            OnStartUnitClicked();
        }
    }

    public void OnPrevStepClicked() {
        if (currentStepIndex > 0) {
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            LoadStep(currentStepIndex - 1);
        }
    }

    public void OnHudBackButtonClicked() {
        if (currentStepIndex > 0) {
            OnPrevStepClicked();
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                Masters_AudioManager.Instance.StopVoiceOver();
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnBackButtonClicked();
            }
        }
    }

    public void OnHudNextButtonClicked() {
        OnNextStepClicked();
    }

    public void ReplayCurrentStep() {
        if (introSteps != null && currentStepIndex >= 0 && currentStepIndex < introSteps.Length) {
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            PlayStepAudio(introSteps[currentStepIndex]);
            if (stageCardObject != null) {
                stageCardObject.transform.DOKill();
                stageCardObject.transform.localScale = Vector3.one * 0.92f;
                stageCardObject.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
            }
        }
    }

    public void OnStartUnitClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Listening);
        }

        topic = Masters_Topic.Intro;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Intro);
        } else {
            OnNextButtonClicked();
        }
    }

    protected override void OnNextButtonClicked() {
        OnHudNextButtonClicked();
    }
}
