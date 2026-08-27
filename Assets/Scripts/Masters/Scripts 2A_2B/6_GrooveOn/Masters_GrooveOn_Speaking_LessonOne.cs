using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Data structure for Unit 6 SP01 Speaking Prompts.
/// </summary>
[System.Serializable]
public class SP01PromptData {
    [Tooltip("Category badge (e.g. BIRTHDAY, PARTY QUESTION, FESTIVAL, PREPARATION)")]
    public string category;
    [Tooltip("Instruction / occasion card prompt text.")]
    public string instruction;
    [Tooltip("ARIA's verbatim model greeting / example sentence.")]
    public string exampleText;
    [Tooltip("Useful greeting phrases for the word bank.")]
    public string[] wordBank;
    [Tooltip("ARIA prompt audio clip (VO_SP01_PROMPT_X).")]
    public AudioClip promptAudio;
    [Tooltip("ARIA model audio clip (VO_SP01_MODEL_X).")]
    public AudioClip modelAudio;
}

/// <summary>
/// Controller for Unit 6 (Groove On) Speaking Branch - Stage SP01: Say It with Cheer - Wish Aloud.
/// Manages 6 speaking prompts across 4 kinds:
/// 1. BIRTHDAY
/// 2. BIRTHDAY (Belated)
/// 3. PARTY QUESTION
/// 4. FESTIVAL (Diwali)
/// 5. FESTIVAL (Open-ended)
/// 6. PREPARATION
/// Features safe microphone capture, model audio comparison, word bank chips, and generous speech checks.
/// </summary>
public class Masters_GrooveOn_Speaking_LessonOne : Masters_PolishedCommunication_Speaking_LessonOne {

    [Header("SP01 Prompt Data Bank (6 Prompts)")]
    [SerializeField] private SP01PromptData[] sp01Prompts;

    [Header("SP01 UI References")]
    [SerializeField] private TextMeshProUGUI categoryBadgeTMP;
    [SerializeField] private TextMeshProUGUI instructionTMP;
    [SerializeField] private TextMeshProUGUI exampleTMP;
    [SerializeField] private TextMeshProUGUI speechProgressTMP;
    [SerializeField] private TextMeshProUGUI feedbackBannerTMP;
    [SerializeField] private RectTransform wordBankContainer;
    [SerializeField] private GameObject wordBankChipPrefab;

    [Header("Audio Tones")]
    [SerializeField] private AudioClip sfxRecStart;
    [SerializeField] private AudioClip sfxRecStop;

    [Header("Control Buttons")]
    [SerializeField] private Button recordButton;
    [SerializeField] private Button stopButton;
    [SerializeField] private Button playModelButton;
    [SerializeField] private Button reRecordButton;
    [SerializeField] private TextMeshProUGUI recordButtonStatusTMP;

    private int currentPromptIndex = 0;
    private bool isRecording = false;
    private AudioClip recordedClip = null;
    private string selectedMicrophoneDevice = null;
    private float recordingTimer = 0f;
    private const float MAX_RECORDING_TIME = 10f;

    protected override void Awake() {
        var baseType = typeof(Masters_PolishedCommunication_Speaking_LessonOne);

        // 1. Phrase Card Reference Game Object
        var refField = baseType.GetField("phraseCardReferenceGameObject", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (refField != null && refField.GetValue(this) == null) {
#if UNITY_EDITOR
            var cardPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/2A/1_PolishedCommunication/Speaking/SpeakingPhraseCard.prefab");
            if (cardPrefab != null) refField.SetValue(this, cardPrefab);
#endif
        }

        // 2. Phrase Card Spawn Point Rect Transform
        var spawnField = baseType.GetField("phraseCardSpawnPointRectTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (spawnField != null && spawnField.GetValue(this) == null) {
            Transform sp = transform.Find("PhraseCardSpawnPoint") ?? transform.Find("SpawnPoint") ?? FindChildRecursiveGrooveOn(transform, "PhraseCardSpawnPoint");
            if (sp != null) spawnField.SetValue(this, sp.GetComponent<RectTransform>());
        }

        // 3. Progress Bar (Slider)
        var pbField = baseType.GetField("progressBar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (pbField != null && pbField.GetValue(this) == null) {
            Slider s = GetComponentInChildren<Slider>(true);
            if (s != null) pbField.SetValue(this, s);
        }

        // 4. Progress Count TMP
        var pcField = baseType.GetField("progressCountTMP", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (pcField != null && pcField.GetValue(this) == null) {
            TMP_Text[] tmps = GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tmps) {
                if (t != null && (t.name.ToLower().Contains("count") || (t.text != null && (t.text.Contains("/6") || t.text.Contains("1/"))))) {
                    pcField.SetValue(this, t as TextMeshProUGUI);
                    break;
                }
            }
        }

        // 5. Slider Image & Rect Transforms
        var sImgField = baseType.GetField("sliderImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (sImgField != null && sImgField.GetValue(this) == null) {
            Slider s = GetComponentInChildren<Slider>(true);
            if (s != null && s.fillRect != null) sImgField.SetValue(this, s.fillRect.GetComponent<Image>());
        }

        var sRectField = baseType.GetField("sliderRectTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (sRectField != null && sRectField.GetValue(this) == null) {
            Slider s = GetComponentInChildren<Slider>(true);
            if (s != null) sRectField.SetValue(this, s.GetComponent<RectTransform>());
        }

        base.Awake();
        topic = Masters_Topic.Speaking;

        EnsureSP01PromptsInitialized();
        AutoWireUIReferences();
        SetupButtonListeners();
        InitMicrophone();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Speaking;

        EnsureSP01PromptsInitialized();
        AutoWireUIReferences();
        SetupButtonListeners();
        InitMicrophone();

        if (narratorSpeech == null) {
#if UNITY_EDITOR
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2A/6_GrooveOn/Speaking/Wish your friend a happy birthday today.mp3");
#endif
        }

        if (Masters_AudioManager.Instance != null && narratorSpeech != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        }

        UpdateTitleAndUIComponents();

        if (sp01Prompts != null && sp01Prompts.Length > 0) {
            LoadPrompt(0);
        }
    }

    private void EnsureSP01PromptsInitialized() {
        if (sp01Prompts != null && sp01Prompts.Length >= 6) return;

        List<SP01PromptData> list = new List<SP01PromptData>();

        // 1. Birthday Wish
        list.Add(CreateSP01Prompt("BIRTHDAY", "Wish your friend a happy birthday today.", "Wish you a very happy birthday! Have fun!", new string[] { "Happy Birthday!", "Wish you a happy birthday!", "Have fun!" }));
        // 2. Birthday Wish (Belated)
        list.Add(CreateSP01Prompt("BIRTHDAY", "You forgot — the birthday was yesterday.", "Belated happy birthday! Hope you had a great day!", new string[] { "Belated wishes!", "Hope you had fun!", "Happy belated birthday!" }));
        // 3. Party Question
        list.Add(CreateSP01Prompt("PARTY QUESTION", "You want to know where the party is.", "Where is the party happening tonight?", new string[] { "Where's the party?", "Tell me the venue!", "What's the location?" }));
        // 4. Festival Greeting
        list.Add(CreateSP01Prompt("FESTIVAL", "It's Diwali at your neighbour's house.", "Wish you and your family a Happy Diwali!", new string[] { "Happy Diwali!", "Wish you a Happy Diwali!", "Enjoy the festival!" }));
        // 5. Festival Greeting
        list.Add(CreateSP01Prompt("FESTIVAL", "Your Christian friend celebrates Christmas.", "Merry Christmas to you and your family!", new string[] { "Merry Christmas!", "Season's greetings!", "Happy holidays!" }));
        // 6. Preparation
        list.Add(CreateSP01Prompt("PREPARATION", "The family is preparing for the event.", "We need to clean the house before guests arrive.", new string[] { "Clean the house!", "Get ready!", "Prepare for guests!" }));

        sp01Prompts = list.ToArray();
    }

    private SP01PromptData CreateSP01Prompt(string cat, string inst, string ex, string[] bank) {
        return new SP01PromptData {
            category = cat,
            instruction = inst,
            exampleText = ex,
            wordBank = bank
        };
    }

    private void UpdateTitleAndUIComponents() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;

            // Do NOT overwrite text inside the phrase card box!
            if (tmp.GetComponentInParent<Masters_SpeakingPhraseCard>() != null || tmp.name.ToLower().Contains("statement")) {
                continue;
            }

            string lowerName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";
            if (lowerName.Contains("lessontitle") || lowerName.Contains("title") || textVal.Contains("Polished") || textVal.Contains("SP01") || textVal.Contains("INTRODUCE") || textVal.Contains("TWO WAYS") || textVal.Contains("WAYS")) {
                tmp.gameObject.SetActive(true);
                tmp.text = "SAY IT WITH CHEER — WISH ALOUD";
            }
            if (lowerName.Contains("heading") || textVal.Contains("GROOVE") || textVal.Contains("SPEAKING")) {
                tmp.text = "SPEAKING BRANCH (Stage SP01)";
            }
        }
    }

    private void InitMicrophone() {
        if (Microphone.devices != null && Microphone.devices.Length > 0) {
            selectedMicrophoneDevice = Microphone.devices[0];
            Debug.Log($"[SP01] Selected microphone device: {selectedMicrophoneDevice}");
        } else {
            selectedMicrophoneDevice = null;
            Debug.Log("[SP01] Using default microphone device.");
        }
    }

    private void AutoWireUIReferences() {
        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            string tName = tmp.name.ToLower();
            string textVal = tmp.text ?? "";

            if (categoryBadgeTMP == null && (tName.Contains("badge") || tName.Contains("category") || textVal.Contains("BIRTHDAY") || textVal.Contains("PARTY"))) {
                categoryBadgeTMP = tmp as TextMeshProUGUI;
            }
            if (instructionTMP == null && (tName.Contains("instruction") || tName.Contains("prompt") || tName.Contains("sentence") || tName.Contains("statement") || tName == "tmp")) {
                instructionTMP = tmp as TextMeshProUGUI;
            }
            if (exampleTMP == null && (tName.Contains("example") || tName.Contains("model") || textVal.Contains("Say wish") || textVal.Contains("Wish you"))) {
                exampleTMP = tmp as TextMeshProUGUI;
            }
            if (speechProgressTMP == null && (tName.Contains("progress") || tName.Contains("count") || tName.Contains("expression"))) {
                speechProgressTMP = tmp as TextMeshProUGUI;
            }
            if (feedbackBannerTMP == null && (tName.Contains("feedback") || tName.Contains("aria") || tName.Contains("banner"))) {
                feedbackBannerTMP = tmp as TextMeshProUGUI;
            }
            if (recordButtonStatusTMP == null && (tName.Contains("status") || tName.Contains("speak") || tName.Contains("talk") || textVal.Contains("Click to talk") || textVal.Contains("Tap to speak"))) {
                recordButtonStatusTMP = tmp as TextMeshProUGUI;
            }
        }

        Button[] allBtns = GetComponentsInChildren<Button>(true);
        foreach (var b in allBtns) {
            if (b == null) continue;
            string bName = b.name.ToLower();

            if (nextButton == null && bName.Contains("next")) {
                nextButton = b;
            }
            if (recordButton == null && (bName.Contains("mic") || bName.Contains("speak") || bName.Contains("record") || bName.Contains("talk") || bName.Contains("keep") || bName.Contains("fix") || bName.Contains("button_0") || bName.Contains("main"))) {
                recordButton = b;
            }
            if (stopButton == null && bName.Contains("stop")) {
                stopButton = b;
            }
            if (playModelButton == null && (bName.Contains("model") || bName.Contains("speaker") || bName.Contains("listen") || bName.Contains("audio"))) {
                playModelButton = b;
            }
        }

        // Fallbacks for recordButton & playModelButton
        if (recordButton == null) {
            foreach (var b in allBtns) {
                if (b == null || b == nextButton) continue;
                string bName = b.name.ToLower();
                if (!bName.Contains("skip") && !bName.Contains("back") && !bName.Contains("speaker") && !bName.Contains("audio")) {
                    recordButton = b;
                    break;
                }
            }
        }

        if (playModelButton == null) {
            foreach (var b in allBtns) {
                if (b == null || b == nextButton || b == recordButton) continue;
                string bName = b.name.ToLower();
                if (bName.Contains("speaker") || bName.Contains("icon") || bName.Contains("listen") || bName.Contains("audio")) {
                    playModelButton = b;
                    break;
                }
            }
        }
    }

    private void SetupButtonListeners() {
        if (recordButton != null) {
            recordButton.onClick.RemoveAllListeners();
            recordButton.onClick.AddListener(ToggleRecording);
        }

        if (stopButton != null) {
            stopButton.onClick.RemoveAllListeners();
            stopButton.onClick.AddListener(StopRecording);
        }

        if (playModelButton != null) {
            playModelButton.onClick.RemoveAllListeners();
            playModelButton.onClick.AddListener(PlayModelAudio);
        }

        if (reRecordButton != null) {
            reRecordButton.onClick.RemoveAllListeners();
            reRecordButton.onClick.AddListener(ReRecord);
        }
    }

    public void ToggleRecording() {
        if (isRecording) {
            StopRecording();
        } else {
            StartRecording();
        }
    }

    /// <summary>
    /// Loads the specified prompt index (0 to 5) into the UI.
    /// </summary>
    public void LoadPrompt(int index) {
        if (sp01Prompts == null || index < 0 || index >= sp01Prompts.Length) {
            OnAllPromptsCompleted();
            return;
        }

        currentPromptIndex = index;
        SP01PromptData data = sp01Prompts[currentPromptIndex];

        // Update Text Display
        if (categoryBadgeTMP != null) categoryBadgeTMP.text = data.category;
        if (instructionTMP != null) instructionTMP.text = data.instruction;
        if (exampleTMP != null) exampleTMP.text = $"Example: \"{data.exampleText}\"";
        if (speechProgressTMP != null) speechProgressTMP.text = $"{currentPromptIndex + 1} / {sp01Prompts.Length}";
        if (feedbackBannerTMP != null) feedbackBannerTMP.text = "";

        // Reset Button States
        if (recordButton != null) {
            recordButton.gameObject.SetActive(true);
            recordButton.interactable = true;
        }
        if (stopButton != null) stopButton.gameObject.SetActive(false);
        if (recordButtonStatusTMP != null) recordButtonStatusTMP.text = "Tap to Speak";

        // Play Prompt Voiceover
        if (data.promptAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(data.promptAudio);
        }

        PopulateWordBank(data.wordBank);
    }

    private void PopulateWordBank(string[] phrases) {
        if (wordBankContainer == null) return;

        // Clear existing chips
        foreach (Transform child in wordBankContainer) {
            Destroy(child.gameObject);
        }

        if (phrases == null || phrases.Length == 0 || wordBankChipPrefab == null) return;

        foreach (string phrase in phrases) {
            GameObject chip = Instantiate(wordBankChipPrefab, wordBankContainer);
            chip.SetActive(true);

            Button chipBtn = chip.GetComponent<Button>();
            if (chipBtn != null) {
                chipBtn.transition = Selectable.Transition.None;
                Image img = chipBtn.GetComponent<Image>();
                if (img != null) {
                    img.raycastTarget = true;
                    img.color = new Color(0.12f, 0.25f, 0.48f, 1f); // Royal Blue (#1E40AF)
                }
            }

            TextMeshProUGUI chipTMP = chip.GetComponentInChildren<TextMeshProUGUI>();
            if (chipTMP != null) {
                chipTMP.raycastTarget = false;
                chipTMP.text = phrase;
                chipTMP.color = Color.white;
            }

            if (chipBtn != null) {
                string copyPhrase = phrase;
                chipBtn.onClick.AddListener(() => {
                    Image img = chipBtn.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.13f, 0.77f, 0.36f, 1f); // Emerald Green
                    chipBtn.transform.DOPunchScale(Vector3.one * 0.1f, 0.25f);

                    if (Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                    }
                    if (feedbackBannerTMP != null) {
                        feedbackBannerTMP.text = $"Selected: \"{copyPhrase}\"";
                    }
                });
            }
        }
    }

    public void StartRecording() {
        if (isRecording) return;
        isRecording = true;

        if (string.IsNullOrEmpty(selectedMicrophoneDevice)) {
            InitMicrophone();
        }

        try {
            recordedClip = Microphone.Start(selectedMicrophoneDevice, false, (int)MAX_RECORDING_TIME, 44100);
            Debug.Log($"[SP01] Started microphone recording on device: {(string.IsNullOrEmpty(selectedMicrophoneDevice) ? "DEFAULT" : selectedMicrophoneDevice)}");
        } catch (System.Exception ex) {
            Debug.LogWarning($"[SP01] Microphone.Start exception: {ex.Message}");
        }

        if (sfxRecStart != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxRecStart);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        if (recordButton != null) {
            recordButton.gameObject.SetActive(true);
            recordButton.interactable = true;
            recordButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
        }
        if (stopButton != null) {
            stopButton.gameObject.SetActive(true);
            stopButton.interactable = true;
        }

        if (recordButtonStatusTMP != null) recordButtonStatusTMP.text = "Recording... Tap Stop";
        if (feedbackBannerTMP != null) feedbackBannerTMP.text = "Listening to your greeting...";

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

        if (stopButton != null) stopButton.gameObject.SetActive(false);
        if (recordButton != null) {
            recordButton.gameObject.SetActive(true);
            recordButton.interactable = true;
        }
        if (recordButtonStatusTMP != null) recordButtonStatusTMP.text = "Tap to Re-record";

        EvaluateSpeechAndProvideFeedback();
    }

    private void EvaluateSpeechAndProvideFeedback() {
        // Generous Speech Check: Encourage correct occasion without strict grading
        string[] encouragingMessages = new string[] {
            "Great speaking!",
            "Nice greeting!",
            "Well done!",
            "Good job!"
        };

        string randomMsg = encouragingMessages[Random.Range(0, encouragingMessages.Length)];
        if (feedbackBannerTMP != null) {
            feedbackBannerTMP.text = $"{randomMsg} Compare with ARIA's model below.";
            feedbackBannerTMP.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f);
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        // Enable Next Button for progression
        if (nextButton != null) {
            nextButton.interactable = true;
        }

        // Auto play model audio for comparison
        Invoke(nameof(PlayModelAudio), 0.8f);
    }

    public void PlayModelAudio() {
        if (sp01Prompts != null && currentPromptIndex < sp01Prompts.Length) {
            AudioClip modelClip = sp01Prompts[currentPromptIndex].modelAudio;
            if (modelClip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(modelClip);
            }
        }
    }

    public void ReRecord() {
        StartRecording();
    }

    protected override void OnNextButtonClicked() {
        if (sp01Prompts != null && currentPromptIndex < sp01Prompts.Length - 1) {
            currentPromptIndex++;
            LoadPrompt(currentPromptIndex);
        } else {
            OnAllPromptsCompleted();
        }
    }

    private void OnAllPromptsCompleted() {
        if (feedbackBannerTMP != null) {
            feedbackBannerTMP.text = "Awesome! You completed all 6 Speaking Prompts!";
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(ProceedToNextLevel);
            NextButtonAnimation();
        }
    }

    private void ProceedToNextLevel() {
        topic = Masters_Topic.Speaking;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (nextLessonSO == null) {
#if UNITY_EDITOR
            nextLessonSO = UnityEditor.AssetDatabase.LoadAssetAtPath<Masters_LessonSO>("Assets/ScriptableObjects/2A/6_GrooveOn/Roleplay/GrooveOn_Roleplay_LessonOne.asset");
            if (nextLessonSO == null) {
                nextLessonSO = UnityEditor.AssetDatabase.LoadAssetAtPath<Masters_LessonSO>("Assets/ScriptableObjects/2A/6_GrooveOn/Game/GrooveOn_Game_LessonOne.asset");
            }
#endif
        }

        if (nextLessonSO != null && Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Roleplay);
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }

    private Transform FindChildRecursiveGrooveOn(Transform parent, string childName) {
        foreach (Transform child in parent) {
            if (child == null) continue;
            if (child.name.Equals(childName, System.StringComparison.OrdinalIgnoreCase)) return child;
            Transform result = FindChildRecursiveGrooveOn(child, childName);
            if (result != null) return result;
        }
        return null;
    }
}