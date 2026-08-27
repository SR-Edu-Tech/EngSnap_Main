using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Controller for Unit 7 (Collocations) Speaking Branch - Stage SP01: Say the Pair — Use It in a Sentence.
/// Manages 6 speaking prompts across 4 hubs (GET, CATCH, SAVE, IDEA).
/// Features safe Unity microphone recording, playback of student's own recording, ARIA model sentence comparison,
/// word bank rail, live score/progress indicators, non-blocking advisory speech check, and Hub return navigation.
/// </summary>
public class Masters_Collocations_Speaking_LessonOne : Masters_PolishedCommunication_Speaking_LessonOne {

    [System.Serializable]
    public class SP01PromptItem {
        public int promptId;
        public string category;            // e.g. "GET", "CATCH", "SAVE", "IDEA"
        public string collocationText;     // e.g. "get ready"
        public string requiredFirstHalf;   // e.g. "get"
        public string requiredSecondHalf;  // e.g. "ready"
        public string modelSentence;       // e.g. "I'm ready for school every morning."
        public AudioClip promptAudio;      // VO_SP01_PROMPT_1..6 clip
        public AudioClip modelAudio;       // VO_SP01_MODEL_1..6 clip
        public Sprite pictureCue;          // Visual cue image
        public AudioClip studentRecording; // Stored student recording for teacher review
        public bool isRecorded;
    }

    [Header("SP01 Data Bank (6 Speaking Prompts)")]
    [SerializeField] private SP01PromptItem[] sp01Prompts;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI sp01TitleTMP;
    [SerializeField] private TextMeshProUGUI sp01HeaderTMP;
    [SerializeField] private TextMeshProUGUI sp01InstructionTMP;
    [SerializeField] private TextMeshProUGUI sp01CategoryBadgeTMP;
    [SerializeField] private TextMeshProUGUI sp01CollocationTMP;
    [SerializeField] private TextMeshProUGUI sp01ModelSentenceTMP;
    [SerializeField] private TextMeshProUGUI sp01ProgressTMP;
    [SerializeField] private TextMeshProUGUI sp01ScoreTMP;
    [SerializeField] private TextMeshProUGUI sp01FeedbackTMP;
    [SerializeField] private TextMeshProUGUI micStatusTMP;
    [SerializeField] private Image pictureCueImage;

    [Header("Control Buttons")]
    [SerializeField] private Button micRecordButton;
    [SerializeField] private Button micStopButton;
    [SerializeField] private Button playMyRecordingButton;
    [SerializeField] private Button playModelAudioButton;
    [SerializeField] private Button reRecordButton;

    [Header("Word Bank UI")]
    [SerializeField] private Button[] wordBankChipButtons;
    [SerializeField] private TextMeshProUGUI[] wordBankChipTMPs;

    [Header("Audio SFX & Intros")]
    [SerializeField] private AudioClip ariaIntroAudio;
    [SerializeField] private AudioClip sfxRecStart;
    [SerializeField] private AudioClip sfxRecStop;

    [Header("Result Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTMP;
    [SerializeField] private Button retryButton;

    // Runtime state variables
    private int currentPromptIndex = 0;
    private int recordedCount = 0;
    private bool isRecording = false;
    private float recordingTimer = 0f;
    private string selectedMicrophoneDevice = null;
    private const float MAX_RECORDING_TIME = 10f;
    private AudioSource playbackAudioSource;

    protected override void OnEnable() {
        // DO NOT call base.OnEnable() to prevent base STT listener from subscribing
    }

    protected override void OnDisable() {
        // DO NOT call base.OnDisable()
    }

    protected override void Awake() {
        // DO NOT call base.Awake() to prevent base class phrase card coroutine or base audio from running
        topic = Masters_Topic.Speaking;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        playbackAudioSource = GetComponent<AudioSource>();
        if (playbackAudioSource == null) playbackAudioSource = gameObject.AddComponent<AudioSource>();

        AutoFindUIReferences();
        Initialize6SpeakingPrompts();
        UpdateTitleAndUIComponents();
    }

    protected override void Start() {
        // DO NOT call base.Start() to prevent base class phrase card coroutine
        topic = Masters_Topic.Speaking;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        AutoFindUIReferences();
        Initialize6SpeakingPrompts();
        UpdateTitleAndUIComponents();
        SetupButtonListeners();

        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);

        // Hide obsolete Unit 1 / STT buttons
        DeactivateObsoleteBaseUI();

        // Initialize Microphone device safely
        if (Microphone.devices != null && Microphone.devices.Length > 0) {
            selectedMicrophoneDevice = Microphone.devices[0];
        }

        currentPromptIndex = 0;
        recordedCount = 0;
        UpdateScoreUI();

        // Play intro ARIA voiceover
        PlayIntroVoiceover();

        LoadPrompt(0);
    }

    private void DeactivateObsoleteBaseUI() {
        Transform skipTrans = transform.Find("SkipButton");
        if (skipTrans != null) skipTrans.gameObject.SetActive(false);

        Transform contTrans = transform.Find("Continue");
        if (contTrans != null) contTrans.gameObject.SetActive(false);

        Transform debugTrans = transform.Find("DebugText");
        if (debugTrans != null) debugTrans.gameObject.SetActive(false);
    }

    public void Initialize6SpeakingPrompts() {
        string audioDir = "Assets/Audio/2A/7_Collocations/Speaking/SP01/";

        sp01Prompts = new SP01PromptItem[] {
            new SP01PromptItem {
                promptId = 1,
                category = "GET",
                collocationText = "get ready",
                requiredFirstHalf = "get",
                requiredSecondHalf = "ready",
                modelSentence = "I'm ready for school every morning.",
                #if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "get ready.mp3"),
                #endif
                #if UNITY_EDITOR
                modelAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "I'm ready for school every morning.mp3")
                #endif
            },
            new SP01PromptItem {
                promptId = 2,
                category = "GET",
                collocationText = "get permission",
                requiredFirstHalf = "get",
                requiredSecondHalf = "permission",
                modelSentence = "I must get permission from my teacher.",
                #if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "get permission.mp3"),
                #endif
                #if UNITY_EDITOR
                modelAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "I must get permission from my teacher.mp3")
                #endif
            },
            new SP01PromptItem {
                promptId = 3,
                category = "CATCH",
                collocationText = "catch a train",
                requiredFirstHalf = "catch",
                requiredSecondHalf = "train",
                modelSentence = "We will catch a train to Delhi.",
                #if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "catch a train.mp3"),
                #endif
                #if UNITY_EDITOR
                modelAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "We will catch a train to Delhi.mp3")
                #endif
            },
            new SP01PromptItem {
                promptId = 4,
                category = "CATCH",
                collocationText = "catch a cold",
                requiredFirstHalf = "catch",
                requiredSecondHalf = "cold",
                modelSentence = "Wear a sweater or you will catch a cold.",
                #if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "catch a cold.mp3"),
                #endif
                #if UNITY_EDITOR
                modelAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Wear a sweater or you will catch a cold.mp3")
                #endif
            },
            new SP01PromptItem {
                promptId = 5,
                category = "SAVE",
                collocationText = "save water",
                requiredFirstHalf = "save",
                requiredSecondHalf = "water",
                modelSentence = "Switch off the fan to save electricity!",
                #if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "save water.mp3"),
                #endif
                #if UNITY_EDITOR
                modelAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Switch off the fan to save electricity.mp3")
                #endif
            },
            new SP01PromptItem {
                promptId = 6,
                category = "IDEA",
                collocationText = "clever idea",
                requiredFirstHalf = "clever",
                requiredSecondHalf = "idea",
                modelSentence = "That was a clever idea!",
                #if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "clever idea.mp3"),
                #endif
                #if UNITY_EDITOR
                modelAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "That was a clever idea.mp3")
                #endif
            }
        };
    }

    private void PlayIntroVoiceover() {
        if (ariaIntroAudio == null) {
            #if UNITY_EDITOR
            ariaIntroAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/7_Collocations/Speaking/SP01/Say the pair then use it in a sentence.mp3");
            #endif
        }
        if (ariaIntroAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroAudio);
        }
    }

    private void AutoFindUIReferences() {
        if (sp01TitleTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("Title");
            if (t != null) sp01TitleTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (sp01HeaderTMP == null) {
            Transform t = transform.Find("Heading") ?? transform.Find("Header");
            if (t != null) sp01HeaderTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (sp01InstructionTMP == null) {
            Transform t = transform.Find("InstructionText") ?? transform.Find("Instruction");
            if (t != null) sp01InstructionTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (sp01CategoryBadgeTMP == null) {
            Transform t = transform.Find("CollocationCardPanel/CategoryBadge") ?? transform.Find("CategoryBadge") ?? transform.Find("BadgeText");
            if (t != null) sp01CategoryBadgeTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (sp01CollocationTMP == null) {
            Transform t = transform.Find("CollocationCardPanel/CollocationText") ?? transform.Find("CollocationText") ?? transform.Find("PromptText") ?? transform.Find("Card/Text");
            if (t != null) sp01CollocationTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (sp01ModelSentenceTMP == null) {
            Transform t = transform.Find("CollocationCardPanel/ModelSentenceText") ?? transform.Find("ModelSentenceText") ?? transform.Find("ExampleText") ?? transform.Find("Card/ExampleText");
            if (t != null) sp01ModelSentenceTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (sp01ProgressTMP == null) {
            Transform t = transform.Find("ProgressIndicator");
            if (t != null) sp01ProgressTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (sp01ScoreTMP == null) {
            Transform t = transform.Find("ScoreIndicator");
            if (t != null) sp01ScoreTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (sp01FeedbackTMP == null) {
            Transform t = transform.Find("FeedbackText");
            if (t != null) sp01FeedbackTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (micStatusTMP == null) {
            Transform t = transform.Find("MicStatusText");
            if (t != null) micStatusTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (pictureCueImage == null) {
            Transform t = transform.Find("PictureCue") ?? transform.Find("Card/Image");
            if (t != null) pictureCueImage = t.GetComponent<Image>();
        }

        Button[] btns = GetComponentsInChildren<Button>(true);
        foreach (var b in btns) {
            if (b == null) continue;
            string bName = b.name.ToLower();
            if (micRecordButton == null && (bName.Contains("mic") || bName.Contains("record") || bName.Contains("speak"))) {
                micRecordButton = b;
            }
            if (micStopButton == null && bName.Contains("stop")) {
                micStopButton = b;
            }
            if (playMyRecordingButton == null && (bName.Contains("myrec") || bName.Contains("playmy") || bName.Contains("listen"))) {
                playMyRecordingButton = b;
            }
            if (playModelAudioButton == null && (bName.Contains("model") || bName.Contains("speaker") || bName.Contains("aria"))) {
                playModelAudioButton = b;
            }
            if (reRecordButton == null && bName.Contains("rerecord")) {
                reRecordButton = b;
            }
        }

        if (resultPanel == null) {
            Transform res = transform.Find("ResultPanel");
            if (res != null) resultPanel = res.gameObject;
        }

        if (retryButton == null && resultPanel != null) {
            retryButton = resultPanel.GetComponentInChildren<Button>(true);
        }
    }

    private void UpdateScoreUI() {
        int total = (sp01Prompts != null && sp01Prompts.Length > 0) ? sp01Prompts.Length : 6;
        string scoreStr = $"Score: {recordedCount}/{total}";
        if (sp01ScoreTMP != null) {
            sp01ScoreTMP.gameObject.SetActive(true);
            sp01ScoreTMP.text = scoreStr;
        }
    }

    private void UpdateTitleAndUIComponents() {
        if (sp01TitleTMP != null) {
            sp01TitleTMP.gameObject.SetActive(true);
            sp01TitleTMP.text = "SP01 Say the Pair — Use It in a Sentence";
            sp01TitleTMP.color = Color.white;
        }

        if (sp01HeaderTMP != null) {
            sp01HeaderTMP.gameObject.SetActive(true);
            sp01HeaderTMP.text = "SPEAKING BRANCH (Speaking Bench)";
            sp01HeaderTMP.color = new Color(0.13f, 0.77f, 0.36f);
        }

        if (sp01InstructionTMP != null) {
            sp01InstructionTMP.gameObject.SetActive(true);
            sp01InstructionTMP.text = "Say the collocation, then use it in a sentence of your own.";
            sp01InstructionTMP.color = new Color(0.9f, 0.95f, 1f);
        }

        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string lowerName = tmp.name.ToLower();

            if (lowerName == "title" || lowerName == "lessontitle") {
                tmp.gameObject.SetActive(true);
                tmp.text = "SP01 Say the Pair — Use It in a Sentence";
                tmp.color = Color.white;
            }
        }
    }

    private void SetupButtonListeners() {
        if (nextButton == null) {
            Transform t = transform.Find("NextButton") ?? transform.Find("Next") ?? transform.Find("Header/NextButton") ?? transform.Find("Canvas/NextButton");
            if (t != null) nextButton = t.GetComponent<Button>();
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        if (micRecordButton != null) {
            micRecordButton.onClick.RemoveAllListeners();
            micRecordButton.onClick.AddListener(ToggleRecording);
        }

        if (micStopButton != null) {
            micStopButton.onClick.RemoveAllListeners();
            micStopButton.onClick.AddListener(StopRecording);
        }

        if (playMyRecordingButton != null) {
            playMyRecordingButton.onClick.RemoveAllListeners();
            playMyRecordingButton.onClick.AddListener(PlayMyRecording);
        }

        if (playModelAudioButton != null) {
            playModelAudioButton.onClick.RemoveAllListeners();
            playModelAudioButton.onClick.AddListener(PlayModelAudio);
        }

        if (reRecordButton != null) {
            reRecordButton.onClick.RemoveAllListeners();
            reRecordButton.onClick.AddListener(StartRecording);
        }
    }

    private void LoadPrompt(int index) {
        if (sp01Prompts == null || index < 0 || index >= sp01Prompts.Length) {
            EvaluateLessonCompletion();
            return;
        }

        currentPromptIndex = index;
        isRecording = false;

        SP01PromptItem prompt = sp01Prompts[index];

        UpdateScoreUI();

        // Category Badge
        if (sp01CategoryBadgeTMP != null) {
            sp01CategoryBadgeTMP.gameObject.SetActive(true);
            sp01CategoryBadgeTMP.text = $"Web: {prompt.category}";
        }

        // Collocation Text
        if (sp01CollocationTMP != null) {
            sp01CollocationTMP.gameObject.SetActive(true);
            sp01CollocationTMP.text = $"Collocation: <b>{prompt.collocationText}</b>";
        }

        // Model Sentence Text
        if (sp01ModelSentenceTMP != null) {
            sp01ModelSentenceTMP.gameObject.SetActive(true);
            sp01ModelSentenceTMP.text = $"Model: \"{prompt.modelSentence}\"";
        }

        // Progress Text
        if (sp01ProgressTMP != null) {
            sp01ProgressTMP.gameObject.SetActive(true);
            sp01ProgressTMP.text = $"Prompt {index + 1}/{sp01Prompts.Length}";
        }

        // Picture Cue Image
        if (pictureCueImage != null) {
            if (prompt.pictureCue != null) {
                pictureCueImage.gameObject.SetActive(true);
                pictureCueImage.sprite = prompt.pictureCue;
            } else {
                pictureCueImage.gameObject.SetActive(true);
            }
        }

        // Reset Feedback UI
        if (sp01FeedbackTMP != null) {
            sp01FeedbackTMP.text = "";
            sp01FeedbackTMP.gameObject.SetActive(false);
        }

        // Reset Record Controls
        if (micRecordButton != null) {
            micRecordButton.gameObject.SetActive(true);
            micRecordButton.interactable = true;
        }
        if (micStopButton != null) micStopButton.gameObject.SetActive(false);
        if (playMyRecordingButton != null) playMyRecordingButton.gameObject.SetActive(prompt.isRecorded);
        if (playModelAudioButton != null) playModelAudioButton.gameObject.SetActive(true);

        if (micStatusTMP != null) {
            micStatusTMP.gameObject.SetActive(true);
            micStatusTMP.text = prompt.isRecorded ? "Recording saved! Tap mic to re-record." : "Tap Microphone to Speak";
        }

        // Play Prompt Voiceover ONLY for subsequent prompts (index > 0) to avoid double audio overlap with intro voiceover on start
        if (index > 0 && prompt.promptAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(prompt.promptAudio);
        }

        // Update Word Bank Rail
        SetupWordBankRail(prompt);
    }

    private void SetupWordBankRail(SP01PromptItem prompt) {
        string[] railChips = new string[] {
            "get ready", "get permission", "catch a train",
            "catch a cold", "save water", "clever idea"
        };

        if (wordBankChipButtons != null && wordBankChipButtons.Length > 0) {
            for (int i = 0; i < wordBankChipButtons.Length; i++) {
                if (wordBankChipButtons[i] == null) continue;
                int idx = i;
                if (i < railChips.Length) {
                    wordBankChipButtons[i].gameObject.SetActive(true);
                    if (wordBankChipTMPs != null && i < wordBankChipTMPs.Length && wordBankChipTMPs[i] != null) {
                        wordBankChipTMPs[i].text = railChips[i];
                    }

                    Image img = wordBankChipButtons[i].GetComponent<Image>();
                    if (img != null) {
                        bool isCurrent = (railChips[i] == prompt.collocationText);
                        img.color = isCurrent ? new Color(0.13f, 0.77f, 0.36f, 1f) : new Color(0.12f, 0.25f, 0.48f, 1f);
                    }

                    wordBankChipButtons[i].onClick.RemoveAllListeners();
                    wordBankChipButtons[i].onClick.AddListener(() => OnWordBankChipTapped(railChips[idx]));
                } else {
                    wordBankChipButtons[i].gameObject.SetActive(false);
                }
            }
        }
    }

    public void OnWordBankChipTapped(string chipText) {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
        if (sp01FeedbackTMP != null) {
            sp01FeedbackTMP.gameObject.SetActive(true);
            sp01FeedbackTMP.text = $"Collocation: \"{chipText}\"";
            sp01FeedbackTMP.color = new Color(0.9f, 0.95f, 1f);
        }
    }

    public void ToggleRecording() {
        if (isRecording) {
            StopRecording();
        } else {
            StartRecording();
        }
    }

    public void StartRecording() {
        if (isRecording) return;
        isRecording = true;

        if (sfxRecStart != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxRecStart);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        if (!string.IsNullOrEmpty(selectedMicrophoneDevice)) {
            AudioClip recording = Microphone.Start(selectedMicrophoneDevice, false, (int)MAX_RECORDING_TIME, 44100);
            sp01Prompts[currentPromptIndex].studentRecording = recording;
        }

        if (micRecordButton != null) micRecordButton.gameObject.SetActive(false);
        if (micStopButton != null) {
            micStopButton.gameObject.SetActive(true);
            micStopButton.interactable = true;
        }

        if (micStatusTMP != null) micStatusTMP.text = "Recording... Tap Stop when finished.";
        ShowFeedback("Listening to your sentence...", true);

        StartCoroutine(RecordingTimerCoroutine());
    }

    private IEnumerator RecordingTimerCoroutine() {
        recordingTimer = 0f;
        while (isRecording && recordingTimer < MAX_RECORDING_TIME) {
            recordingTimer += Time.deltaTime;
            yield return null;
        }

        if (isRecording) {
            StopRecording();
        }
    }

    public void StopRecording() {
        if (!isRecording) return;
        isRecording = false;

        if (!string.IsNullOrEmpty(selectedMicrophoneDevice) && Microphone.IsRecording(selectedMicrophoneDevice)) {
            Microphone.End(selectedMicrophoneDevice);
        }

        if (sfxRecStop != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxRecStop);
        }

        SP01PromptItem prompt = sp01Prompts[currentPromptIndex];
        if (!prompt.isRecorded) {
            prompt.isRecorded = true;
            recordedCount++;
            UpdateScoreUI();
        }

        if (micStopButton != null) micStopButton.gameObject.SetActive(false);
        if (micRecordButton != null) {
            micRecordButton.gameObject.SetActive(true);
            micRecordButton.interactable = true;
        }

        if (playMyRecordingButton != null) playMyRecordingButton.gameObject.SetActive(true);
        if (playModelAudioButton != null) playModelAudioButton.gameObject.SetActive(true);

        if (micStatusTMP != null) micStatusTMP.text = "Recording Saved! Play or tap Mic to re-record.";

        ShowFeedback("Recording saved! Listen back or compare with ARIA's model.", true);

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        // Enable Next Button for progression
        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
            NextButtonAnimation();
        }
    }

    public void PlayMyRecording() {
        SP01PromptItem prompt = sp01Prompts[currentPromptIndex];
        if (prompt.studentRecording != null && playbackAudioSource != null) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
            }
            playbackAudioSource.Stop();
            playbackAudioSource.clip = prompt.studentRecording;
            playbackAudioSource.Play();
            ShowFeedback("Playing your recording...", true);
        } else {
            ShowFeedback("No recording found. Tap Mic to record!", false);
        }
    }

    public void PlayModelAudio() {
        SP01PromptItem prompt = sp01Prompts[currentPromptIndex];
        if (prompt.modelAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(prompt.modelAudio);
            ShowFeedback($"ARIA: \"{prompt.modelSentence}\"", true);
        }
    }

    private void ShowFeedback(string message, bool isSuccess) {
        if (sp01FeedbackTMP != null) {
            sp01FeedbackTMP.gameObject.SetActive(true);
            sp01FeedbackTMP.text = message;
            sp01FeedbackTMP.color = isSuccess ? new Color(0.12f, 0.65f, 0.28f) : new Color(0.85f, 0.2f, 0.2f);
        }
    }

    private void EvaluateLessonCompletion() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        UpdateScoreUI();

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
        }

        bool passed = (recordedCount >= 6);

        if (resultTMP != null) {
            resultTMP.text = $"GREAT JOB! Score: {recordedCount}/{sp01Prompts.Length}\nYou recorded all 6 speaking prompts!";
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
            NextButtonAnimation();
        }
    }

    public void RestartActivity() {
        if (resultPanel != null) resultPanel.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        currentPromptIndex = 0;
        recordedCount = 0;
        isRecording = false;
        if (sp01Prompts != null) {
            foreach (var p in sp01Prompts) {
                if (p != null) p.isRecorded = false;
            }
        }
        UpdateScoreUI();
        LoadPrompt(0);
    }

    protected override void OnNextButtonClicked() {
        currentPromptIndex++;
        if (sp01Prompts != null && currentPromptIndex < sp01Prompts.Length) {
            LoadPrompt(currentPromptIndex);
        } else {
            topic = Masters_Topic.Speaking;
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}