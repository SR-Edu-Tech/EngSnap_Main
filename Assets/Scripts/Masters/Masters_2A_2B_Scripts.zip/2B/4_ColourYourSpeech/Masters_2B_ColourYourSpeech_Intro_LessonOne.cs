using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// INTRO Paint Your Words — Studio Intro Lesson Controller for Book 2B Unit 4 (Colour Your Speech).
/// Displays the 5-step cinematic paint studio flow:
/// Step 1: LEO holds up plain grey sentence 'It was raining a lot.'
/// Step 2: Dips brush in paint pot, turns sentence bright: 'It was raining cats and dogs.'
/// Step 3: ARIA shows two walls (literal pictures vs real meaning — an idiom does not mean what its words say).
/// Step 4: Three paint pots splash open with idioms ('Piece of cake', 'Have a blast', 'Miss the boat').
/// Step 5: ARIA previews goal + START call-to-action to enter the Unit Hub.
/// </summary>
public class Masters_2B_ColourYourSpeech_Intro_LessonOne : Masters_Lesson {

[System.Serializable]
public class ColourYourSpeech_IntroPaintStepData {
    public string speakerName;         // "LEO", "ARIA", "NARRATOR"
    public string dialogueLine;        // Dialogue spoken
    public string actionDescription;   // Action taking place
    public string cardHeadline;        // Top headline on studio card
    public string cardDetail;          // Subtext or explanation
    public Color cardThemeColor;       // Color theme for current step
    public AudioClip stepAudio;        // Step voiceover audio clip
}

    [Header("5-Step Studio Paint Flow")]
    [SerializeField]
    private ColourYourSpeech_IntroPaintStepData[] introSteps;

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

    [Header("Studio Center Stage Card")]
    [SerializeField]
    private GameObject studioCardObject;
    [SerializeField]
    private Image studioCardBg;
    [SerializeField]
    private TextMeshProUGUI cardHeadlineTMP;
    [SerializeField]
    private TextMeshProUGUI cardDetailTMP;

    [Header("Three Paint Pots Container (Step 4)")]
    [SerializeField]
    private GameObject paintPotsContainer;
    [SerializeField]
    private TextMeshProUGUI[] potItemTexts;

    [Header("HUD Controls")]
    [SerializeField]
    private Button hudBackButton;
    [SerializeField]
    private Button hudNextButton;

    [Header("Call To Action & Navigation Controls")]
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
            PopulateFailsafeSteps();
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
            PopulateFailsafeSteps();
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
            headerTMP.text = "INTRO BRANCH (Paint Studio)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "INTRO Paint Your Words";
            titleTMP.color = new Color(1f, 0.85f, 0.15f, 1f);
        }

        if (subtitleTMP == null) {
            Transform sTrans = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Instruction") ?? transform.Find("Subtitle");
            if (sTrans != null) subtitleTMP = sTrans.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP != null) {
            subtitleTMP.text = "Watch LEO turn plain sentences into colorful idioms in the Paint Studio!";
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

        if (studioCardObject == null) {
            Transform cardTrans = transform.Find("StudioCard") ?? transform.Find("CenterCard");
            if (cardTrans != null) {
                studioCardObject = cardTrans.gameObject;
                studioCardBg = cardTrans.GetComponent<Image>();
            }
        }

        if (paintPotsContainer == null) {
            Transform potsTrans = transform.Find("PaintPotsContainer") ?? transform.Find("PotsContainer");
            if (potsTrans != null) {
                paintPotsContainer = potsTrans.gameObject;
                potItemTexts = potsTrans.GetComponentsInChildren<TextMeshProUGUI>(true);
            }
        }
    }

    public void PopulateFailsafeSteps() {
        string audioDir = "Assets/Audio/2B/4_ColourYourSpeech/Intro/";

        introSteps = new ColourYourSpeech_IntroPaintStepData[] {
            // Step 1: Plain grey sentence
            new ColourYourSpeech_IntroPaintStepData {
                speakerName = "LEO",
                dialogueLine = "\"Look at this plain grey sentence: 'It was raining a lot.' Let's give it some color!\"",
                actionDescription = "Scene fades in on the studio; LEO holds up a plain grey sentence: 'It was raining a lot.'",
                cardHeadline = "PLAIN SENTENCE:\n'It was raining a lot.'",
                cardDetail = "Grey and ordinary — it tells the facts, but has no color or vivid imagination.",
                cardThemeColor = new Color(0.35f, 0.40f, 0.48f, 0.95f)
#if UNITY_EDITOR
                , stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cys_intro_01_leo_plain_sentence.mp3")
#endif
            },
            // Step 2: Dipping brush and bright idiom
            new ColourYourSpeech_IntroPaintStepData {
                speakerName = "LEO",
                dialogueLine = "\"I dip my brush in a pot and the sentence turns bright: 'It was raining cats and dogs!'\"",
                actionDescription = "He dips his brush in a pot and the sentence turns bright: 'It was raining cats and dogs.'",
                cardHeadline = "BRIGHT IDIOM:\n'It was raining cats and dogs.'",
                cardDetail = "Vibrant and lively — painting a vivid picture in the listener's mind!",
                cardThemeColor = new Color(0.12f, 0.45f, 0.85f, 0.95f)
#if UNITY_EDITOR
                , stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cys_intro_02_leo_bright_idiom.mp3")
#endif
            },
            // Step 3: ARIA explains the two walls
            new ColourYourSpeech_IntroPaintStepData {
                speakerName = "ARIA",
                dialogueLine = "\"Look at the two walls: the funny literal picture on the left, and the real meaning on the right! An idiom does not mean what its words say.\"",
                actionDescription = "ARIA shows the two walls: the funny literal picture on the left, the real meaning on the right — an idiom does not mean what its words say.",
                cardHeadline = "THE TWO WALLS OF IDIOMS",
                cardDetail = "Left Wall: Funny Literal Pictures (Cats and dogs tumbling out of a cloud)\nRight Wall: Real Meaning (Extremely heavy rainfall!)",
                cardThemeColor = new Color(0.55f, 0.25f, 0.75f, 0.95f)
#if UNITY_EDITOR
                , stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cys_intro_03_aria_two_walls.mp3")
#endif
            },
            // Step 4: Three more paint pots splash open
            new ColourYourSpeech_IntroPaintStepData {
                speakerName = "ARIA",
                dialogueLine = "\"Three more pots splash open with their book meanings! Piece of cake, Have a blast, and Miss the boat!\"",
                actionDescription = "Three more pots splash open with their book meanings: Piece of cake = A job task or activity that is easy or simple to do · Have a blast = To enjoy a lot · Miss the boat = To miss a chance.",
                cardHeadline = "THREE ESSENTIAL IDIOM POTS",
                cardDetail = "• Piece of cake = A job task or activity that is easy or simple to do\n• Have a blast = To enjoy a lot\n• Miss the boat = To miss a chance",
                cardThemeColor = new Color(0.85f, 0.45f, 0.15f, 0.95f)
#if UNITY_EDITOR
                , stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cys_intro_04_aria_three_pots.mp3")
#endif
            },
            // Step 5: ARIA goal preview + START call to action
            new ColourYourSpeech_IntroPaintStepData {
                speakerName = "ARIA",
                dialogueLine = "\"Polish your conversations with these idioms and be a master of conversation! Tap START to begin!\"",
                actionDescription = "ARIA previews the goal: 'Polish your conversations with these idioms and be a master of conversation!' A large START button appears; tapping it transitions to the Hub.",
                cardHeadline = "BE A MASTER OF CONVERSATION!",
                cardDetail = "\"Polish your conversations with these idioms and be a master of conversation!\"\n\nTap START to enter the Unit Hub!",
                cardThemeColor = new Color(0.13f, 0.77f, 0.36f, 0.95f)
#if UNITY_EDITOR
                , stepAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "cys_intro_05_aria_preview_goal.mp3")
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

    private void DisplayStepData(ColourYourSpeech_IntroPaintStepData step, int index) {
        if (progressTMP != null) {
            progressTMP.text = $"Step {index + 1}/{introSteps.Length}";
        }

        if (speakerNameTMP != null) {
            speakerNameTMP.text = step.speakerName;
            speakerNameTMP.color = (step.speakerName == "ARIA") ? new Color(1f, 0.88f, 0.25f) : new Color(0.2f, 0.85f, 1f);
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

        if (studioCardBg != null) {
            studioCardBg.DOColor(step.cardThemeColor, 0.25f);
        }

        if (studioCardObject != null) {
            studioCardObject.transform.DOKill();
            studioCardObject.transform.localScale = Vector3.one * 0.95f;
            studioCardObject.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }

        if (paintPotsContainer != null) {
            paintPotsContainer.SetActive(index == 3);
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

    private void PlayStepAudio(ColourYourSpeech_IntroPaintStepData step) {
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

    public void ReplayCurrentStep() {
        if (introSteps != null && currentStepIndex >= 0 && currentStepIndex < introSteps.Length) {
            PlayStepAudio(introSteps[currentStepIndex]);
            if (studioCardObject != null) {
                studioCardObject.transform.DOKill();
                studioCardObject.transform.localScale = Vector3.one * 0.92f;
                studioCardObject.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
            }
        }
    }

    public void ReplaySequence() {
        StartIntroSequence();
    }

    public void OnNextStepClicked() {
        if (currentStepIndex < introSteps.Length - 1) {
            LoadStep(currentStepIndex + 1);
        } else {
            OnStartUnitClicked();
        }
    }

    public void OnPrevStepClicked() {
        if (currentStepIndex > 0) {
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

    protected override void OnNextButtonClicked() {
        OnHudNextButtonClicked();
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
            base.OnNextButtonClicked();
        }
    }
}
