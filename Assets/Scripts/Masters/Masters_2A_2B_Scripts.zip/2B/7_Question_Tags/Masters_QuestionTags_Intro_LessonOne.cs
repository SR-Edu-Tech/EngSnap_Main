using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// INTRO: The Valley That Answers Back - Lesson Controller for Book 2B Unit 7 (Question Tags).
/// 5-Step Cinematic Valley Intro with robust audio playback and failsafe fallbacks.
/// </summary>
public class Masters_QuestionTags_Intro_LessonOne : Masters_Lesson {

[System.Serializable]
public class QuestionTags_IntroStepData {
    public string speakerName;         // "LEO", "CLIFF ECHO", "ARIA", "NARRATOR"
    public string dialogueLine;        // Spoken dialogue
    public string actionDescription;   // Action description
    public string cardHeadline;        // Headline on center card
    public string cardDetail;          // Subtext or explanation
    public Color cardThemeColor;       // Card background color
    public AudioClip stepAudio;        // Voiceover clip for this step
}

    [Header("5-Step Valley Intro Flow")]
    [SerializeField]
    private QuestionTags_IntroStepData[] introSteps;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI subtitleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;

    [Header("Speaker & Dialogue Box")]
    [SerializeField]
    private GameObject dialogueContainer;
    [SerializeField]
    private TextMeshProUGUI speakerNameTMP;
    [SerializeField]
    private TextMeshProUGUI dialogueTextTMP;
    [SerializeField]
    private TextMeshProUGUI actionTextTMP;

    [Header("Center Stage Display Card")]
    [SerializeField]
    private GameObject stageCardObject;
    [SerializeField]
    private Image stageCardBg;
    [SerializeField]
    private TextMeshProUGUI cardHeadlineTMP;
    [SerializeField]
    private TextMeshProUGUI cardDetailTMP;

    [Header("Positive / Negative Tag Arches (Visual Indicators)")]
    [SerializeField]
    private GameObject positiveTagArch;
    [SerializeField]
    private GameObject negativeTagArch;

    [Header("HUD Controls")]
    [SerializeField]
    private Button hudBackButton;
    [SerializeField]
    private Button hudNextButton;

    [Header("Navigation & Call-To-Action")]
    [SerializeField]
    private Button startUnitBtn;
    [SerializeField]
    private Button nextStepBtn;
    [SerializeField]
    private Button prevStepBtn;
    [SerializeField]
    private Button replayBtn;
    [SerializeField]
    private Button ariaReplayBtn;

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
        WireIntroButtons();
    }

    protected override void Start() {
        topic = Masters_Topic.Intro;

        PurgeLegacyChildren();
        AutoBindReferences();
        EnsureHeaderAndTitle();
        WireIntroButtons();

        if (introSteps == null || introSteps.Length == 0) {
            PopulateDefaultSteps();
        }

        StartIntroSequence();
    }

    public override void EnsureNextAndBackButtonWired() {
        base.EnsureNextAndBackButtonWired();
        WireIntroButtons();
    }

    private void WireIntroButtons() {
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

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
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

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch") ?? transform.Find("Heading");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "QUESTION TAGS";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "INTRO - The Valley That Answers Back";
            titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
        }

        if (subtitleTMP == null) {
            Transform sTrans = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Instruction") ?? transform.Find("Subtitle");
            if (sTrans != null) subtitleTMP = sTrans.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP != null) {
            subtitleTMP.text = "Say it one way and the echo says it the other. Learn the tags and you will never guess again!";
        }
    }

    public void AutoBindReferences() {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading"))) headerTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
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
            Transform cardTrans = transform.Find("StudioCard") ?? transform.Find("CenterCard") ?? transform.Find("StageCard");
            if (cardTrans != null) {
                stageCardObject = cardTrans.gameObject;
                stageCardBg = cardTrans.GetComponent<Image>();
            }
        }
    }

    public void PopulateDefaultSteps() {
        string audioDir = "Assets/Audio/2B/7_Question_Tags/Intro/";

#if UNITY_EDITOR
        narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_QT_INTRO_WELCOME.mp3")
                      ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Welcome to Unit 7 Question Tags The Valley That Answers Back Say it one way and the echo says it the other Learn the tags and you will never guess again.mp3");
#endif

        introSteps = new QuestionTags_IntroStepData[] {
            // Step 1: LEO positive statement with welcome audio
            new QuestionTags_IntroStepData {
                speakerName = "NARRATOR & LEO",
                dialogueLine = "\"Welcome to Question Tags: The Valley That Answers Back! Leo calls out: 'You are a student...'\"",
                actionDescription = "Scene fades in on the valley; LEO cups his hands and calls out: 'You are a student...'",
                cardHeadline = "LOOKOUT LEDGE: Positive Call\n\"You are a student...\"",
                cardDetail = "Leo calls out a positive statement across the green valley.\nSay it one way and the echo says it the other!",
                cardThemeColor = new Color(0.12f, 0.45f, 0.85f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_QT_INTRO_WELCOME.mp3")
                         ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_QT_INTRO_STEP1_LEO.mp3")
                         ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "You are a student.mp3")
#endif
            },
            // Step 2: Cliff answers with negative tag
            new QuestionTags_IntroStepData {
                speakerName = "ECHO",
                dialogueLine = "\"...aren't you?\"",
                actionDescription = "The cliff answers with the tag: '...aren't you?' - and the NEGATIVE TAG arch lights up.",
                cardHeadline = "NEGATIVE TAG ARCH\n\"...aren't you?\"",
                cardDetail = "Positive Statement (+) -> Negative Tag (-) [Arch Lights Up!]",
                cardThemeColor = new Color(0.85f, 0.35f, 0.15f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_QT_INTRO_STEP2_ECHO.mp3")
                         ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "aren't you.mp3")
#endif
            },
            // Step 3: ARIA explains see-saw
            new QuestionTags_IntroStepData {
                speakerName = "ARIA",
                dialogueLine = "\"Think of it like a see-saw! When the statement tips one way, the tag tips the other!\"",
                actionDescription = "ARIA explains the see-saw with a simple animation: the statement tips one way, the tag tips the other.",
                cardHeadline = "THE SEE-SAW RULE",
                cardDetail = "Statement is Positive (+) | Tag is Negative (-)\nStatement is Negative (-) | Tag is Positive (+)",
                cardThemeColor = new Color(0.55f, 0.25f, 0.75f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_QT_INTRO_STEP3_ARIA.mp3")
                         ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Think of it like a see-saw When the statement tips one way the tag tips the other.mp3")
#endif
            },
            // Step 4: LEO calls negative statement, echo returns positive tag
            new QuestionTags_IntroStepData {
                speakerName = "LEO & ECHO",
                dialogueLine = "LEO: \"You aren't a teacher...\"\nECHO: \"...are you?\"",
                actionDescription = "LEO calls the other way: 'You aren't a teacher...' and the echo comes back '...are you?' - the POSITIVE TAG arch lights.",
                cardHeadline = "POSITIVE TAG ARCH\n\"You aren't a teacher... are you?\"",
                cardDetail = "Negative Statement (-) -> Positive Tag (+) [Positive Arch Lights Up!]",
                cardThemeColor = new Color(0.15f, 0.65f, 0.55f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_QT_INTRO_STEP4_LEO_ECHO.mp3")
                         ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "You aren't a teacher.mp3")
#endif
            },
            // Step 5: ARIA previews goal + START
            new QuestionTags_IntroStepData {
                speakerName = "ARIA",
                dialogueLine = "\"Say it one way and the echo says it the other. Learn the tags and you'll never guess again! Tap START to enter!\"",
                actionDescription = "ARIA previews the goal: 'Say it one way and the echo says it the other. Learn the tags and you'll never guess again!' A large START button appears; tapping it transitions to the Hub.",
                cardHeadline = "MASTER THE VALLEY OF QUESTION TAGS!",
                cardDetail = "Say it one way and the echo says it the other.\nTap START to enter the Unit Hub!",
                cardThemeColor = new Color(0.13f, 0.77f, 0.36f, 0.95f),
#if UNITY_EDITOR
                stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_QT_INTRO_STEP5_ARIA.mp3")
                         ?? UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Say it one way and the echo says it the other Learn the tags and you will never guess again.mp3")
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

    private void DisplayStepData(QuestionTags_IntroStepData step, int index) {
        if (progressTMP != null) {
            progressTMP.text = $"Step {index + 1}/{introSteps.Length}";
        }

        if (speakerNameTMP != null) {
            speakerNameTMP.text = step.speakerName;
            if (step.speakerName.Contains("ARIA")) {
                speakerNameTMP.color = new Color(1f, 0.88f, 0.25f);
            } else if (step.speakerName.Contains("ECHO")) {
                speakerNameTMP.color = new Color(0.9f, 0.5f, 1f);
            } else {
                speakerNameTMP.color = new Color(0.2f, 0.85f, 1f);
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

        // Arch highlights based on step
        if (negativeTagArch != null) {
            negativeTagArch.SetActive(index == 1 || index == 4);
        }
        if (positiveTagArch != null) {
            positiveTagArch.SetActive(index == 3 || index == 4);
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

    private void PlayStepAudio(QuestionTags_IntroStepData step) {
        if (!Application.isPlaying) return;

        AudioClip clipToPlay = (step != null && step.stepAudio != null) ? step.stepAudio : narratorSpeech;

        if (clipToPlay != null) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            } else {
                if (localAudioSource == null) localAudioSource = GetComponent<AudioSource>();
                if (localAudioSource != null) {
                    localAudioSource.Stop();
                    localAudioSource.clip = clipToPlay;
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

    public void ReplaySequence() {
        StartIntroSequence();
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
