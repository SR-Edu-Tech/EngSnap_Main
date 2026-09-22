using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Core Game Manager for Unit 8 (Source of Inspiration / Tongue Twisters) Memory Lesson One:
/// M01 — Learn It by Heart.
/// Features:
/// 1. 5-Round Progressive Word-Hiding Memory Engine (Round 1: 0%, Round 2: 25%, Round 3: 50%, Round 4: First Word Only, Round 5: 100% Blank).
/// 2. Interactive HINT System allowing temporary word reveal without penalty.
/// 3. Real Device Microphone Recording via Unity Microphone API (Microphone.Start / Microphone.End).
/// 4. Live Microphone Waveform Visualization & Elapsed Timer.
/// 5. Self-Check Confirmation (✓ Yes, I Said It / ↻ Try Again).
/// 6. Post-Round 5 Full Text Reveal & Memory Master Celebration.
/// 7. Inspector [TextArea] customization for tongue-twister text.
/// </summary>
public class Masters_SourceOfInspiration_Memory_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class MemoryWordData {
        public int id;
        public string rawWord;
        public string cleanWord;
        public int lineIndex;
        public bool isFirstInLine;
        public bool isHiddenInRound2;
        public bool isHiddenInRound3;
    }

    [Header("UI Header & Titles")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI roundIndicatorTMP;
    [SerializeField] private TextMeshProUGUI instructionTMP;

    [Header("Memory Desk & Card Display")]
    [SerializeField] private RectTransform memoryDeskCardRect;
    [SerializeField] private TextMeshProUGUI deskTextTMP;

    [Header("Control & Action Buttons")]
    [SerializeField] private Button recordButton;
    [SerializeField] private TextMeshProUGUI recordBtnTMP;
    [SerializeField] private Button playMyAttemptButton;
    [SerializeField] private TextMeshProUGUI playMyAttemptBtnTMP;
    [SerializeField] private Button hintButton;
    [SerializeField] private TextMeshProUGUI hintBtnTMP;

    [Header("Self Check Confirmation UI")]
    [SerializeField] private GameObject selfCheckContainerObj;
    [SerializeField] private Button confirmSaidItButton;
    [SerializeField] private Button tryAgainButton;

    [Header("Live Waveform & Timer Display")]
    [SerializeField] private GameObject waveformContainer;
    [SerializeField] private RectTransform[] waveformBars = new RectTransform[8];
    [SerializeField] private TextMeshProUGUI timerDisplayTMP;
    [SerializeField] private TextMeshProUGUI statusTMP;

    [Header("Round Progress Indicator Badges")]
    [SerializeField] private TextMeshProUGUI[] roundBadgeTMPs = new TextMeshProUGUI[5];

    [Header("Completion & Final Celebration UI")]
    [SerializeField] private GameObject completionPanelObj;
    [SerializeField] private TextMeshProUGUI completionPanelTMP;

    [Header("Customizable Twister Text")]
    [TextArea(3, 6)]
    [SerializeField]
    private string customTwisterText = "\"I wish to wish the wish you wish to wish, but if you wish the wish wishes, I won't wish the wish you wish to wish.\"";

    [Header("Audio Data References")]
    [SerializeField] private AudioClip voM01AriaIntro;
    [SerializeField] private AudioClip voM01AriaRound1;
    [SerializeField] private AudioClip voM01AriaRound2;
    [SerializeField] private AudioClip voM01AriaRound3;
    [SerializeField] private AudioClip voM01AriaRound4;
    [SerializeField] private AudioClip voM01AriaRound5;
    [SerializeField] private AudioClip voM01AriaCelebration;
    [SerializeField] private AudioClip sfxRecordBeep;

    private List<MemoryWordData> wordList = new List<MemoryWordData>();
    private int currentRound = 1; // 1 to 5
    private bool[] roundCompletedState = new bool[5] { false, false, false, false, false };
    private bool isHintActive = false;

    // Microphone State
    private string selectedMicrophoneDevice = null;
    private AudioClip micRecordingClip = null;
    private AudioClip userRecordedClip = null;
    private bool isRecording = false;
    private float recordingStartTime = 0f;
    private float recordingDuration = 0f;
#pragma warning disable 0414
    private bool isPlayingUserRecording = false;
#pragma warning restore 0414
    private Coroutine liveTimerCoroutine;
    private Coroutine liveWaveformCoroutine;

    // Visual Style Colors
    private Color colorBtnRecordNormal = new Color(0.9f, 0.3f, 0.3f, 1f);
    private Color colorBtnRecordingActive = new Color(0.95f, 0.15f, 0.15f, 1f);

    protected virtual void OnEnable() {
        // Prevent STT subscriptions
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Writing;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeAudioReferences();
        UpdateTitleAndUIComponents();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Writing;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeAudioReferences();
        ParseTwisterWordData();
        UpdateTitleAndUIComponents();
        SetupButtonListeners();
        InitializeMicrophoneDevice();

        if (nextButton != null) nextButton.gameObject.SetActive(false);
        if (selfCheckContainerObj != null) selfCheckContainerObj.SetActive(false);
        if (completionPanelObj != null) completionPanelObj.SetActive(false);

        LoadMemoryRound(1);
    }

    private void DeactivateObsoleteBaseUI() {
        string[] obsoleteNames = new string[] {
            "sentence", "ExpressionCountTMP", "UnitHeading", "Sign Board left", "Sign Board Right",
            "Formal", "Informal", "MagnetStations", "WordTiles", "CompletedPhraseGlow",
            "ARIA", "SkipButton", "Continue", "Heading", "Header", "SpeechBubble",
            "DialogueBox", "HubButtons", "Hubs", "ResultPanel",
            "Mic", "ProgressCountTMP", "PhraseCardSpawnPoint", "PhraseCardReference",
            "SpeedLadder", "TwisterCard", "TimerDisplay", "ListenAriaButton", "TargetTimer", "StageCompletePanel",
            "LineCard", "PlayModelButton", "PlayMyRecordingButton", "WaveformContainer",
            "NpcAndStudent", "NPCCharacter", "StudentCharacter", "Cloud", "Character", "options promt TMP", "optionscontainer"
        };
        foreach (string name in obsoleteNames) {
            Transform t = transform.Find(name);
            if (t != null) Destroy(t.gameObject);
        }
    }

    private void InitializeMicrophoneDevice() {
        if (Microphone.devices.Length > 0) {
            selectedMicrophoneDevice = Microphone.devices[0];
        }
    }

    private void AutoFindUIReferences() {
        if (headerTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("Title") ?? transform.Find("Header");
            if (t != null) headerTMP = t.GetComponent<TextMeshProUGUI>() ?? t.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (subtitleTMP == null) {
            Transform t = transform.Find("Subtitle") ?? transform.Find("SubtitleText") ?? transform.Find("Instruction") ?? transform.Find("options promt TMP");
            if (t != null) subtitleTMP = t.GetComponent<TextMeshProUGUI>() ?? t.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (roundIndicatorTMP == null) {
            Transform t = transform.Find("RoundIndicator") ?? transform.Find("StepIndicator");
            if (t != null) roundIndicatorTMP = t.GetComponent<TextMeshProUGUI>() ?? t.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (instructionTMP == null) {
            Transform t = transform.Find("InstructionText") ?? transform.Find("Instruction") ?? transform.Find("Subtitle");
            if (t != null) instructionTMP = t.GetComponent<TextMeshProUGUI>() ?? t.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (memoryDeskCardRect == null) {
            Transform t = transform.Find("MemoryDeskCard") ?? transform.Find("LineCard") ?? transform.Find("ReadingContainer");
            if (t != null) memoryDeskCardRect = t.GetComponent<RectTransform>();
        }

        if (deskTextTMP == null && memoryDeskCardRect != null) {
            deskTextTMP = memoryDeskCardRect.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (recordButton == null) {
            Transform t = transform.Find("RecordButton") ?? transform.Find("MicrophoneButton");
            if (t != null) {
                recordButton = t.GetComponent<Button>();
                if (recordBtnTMP == null) recordBtnTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        if (hintButton == null) {
            Transform t = transform.Find("HintButton");
            if (t != null) {
                hintButton = t.GetComponent<Button>();
                if (hintBtnTMP == null) hintBtnTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        if (playMyAttemptButton == null) {
            Transform t = transform.Find("PlayMyAttemptButton") ?? transform.Find("PlayUserButton");
            if (t != null) {
                playMyAttemptButton = t.GetComponent<Button>();
                if (playMyAttemptBtnTMP == null) playMyAttemptBtnTMP = t.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }
    }

    private void InitializeAudioReferences() {
#if UNITY_EDITOR
        string audioDir = "Assets/Audio/2A/8_SourceOfInspiration/Memory/";
        if (voM01AriaIntro == null) voM01AriaIntro = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "M01_AriaIntro.mp3");
        if (voM01AriaRound1 == null) voM01AriaRound1 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "M01_AriaRound1.mp3");
        if (voM01AriaRound2 == null) voM01AriaRound2 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "M01_AriaRound2.mp3");
        if (voM01AriaRound3 == null) voM01AriaRound3 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "M01_AriaRound3.mp3");
        if (voM01AriaRound4 == null) voM01AriaRound4 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "M01_AriaRound4.mp3");
        if (voM01AriaRound5 == null) voM01AriaRound5 = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "M01_AriaRound5.mp3");
        if (voM01AriaCelebration == null) voM01AriaCelebration = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "M01_AriaCelebration.mp3");
        if (sfxRecordBeep == null) sfxRecordBeep = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/Pop.mp3");
#endif
    }

    private void ParseTwisterWordData() {
        wordList.Clear();
        string textToParse = string.IsNullOrEmpty(customTwisterText) ?
            "\"I wish to wish the wish you wish to wish, but if you wish the wish wishes, I won't wish the wish you wish to wish.\"" : customTwisterText;

        string[] rawTokens = textToParse.Split(' ');
        int lineIdx = 0;

        // Dynamic Fisher-Yates Shuffling of hidden word indices
        List<int> validIndices = new List<int>();
        for (int i = 0; i < rawTokens.Length; i++) validIndices.Add(i);

        ShuffleList(validIndices);
        HashSet<int> round2HiddenIndices = new HashSet<int>();
        int countR2 = Mathf.CeilToInt(rawTokens.Length * 0.25f);
        for (int i = 0; i < countR2 && i < validIndices.Count; i++) {
            round2HiddenIndices.Add(validIndices[i]);
        }

        ShuffleList(validIndices);
        HashSet<int> round3HiddenIndices = new HashSet<int>();
        int countR3 = Mathf.CeilToInt(rawTokens.Length * 0.50f);
        for (int i = 0; i < countR3 && i < validIndices.Count; i++) {
            round3HiddenIndices.Add(validIndices[i]);
        }

        for (int i = 0; i < rawTokens.Length; i++) {
            string token = rawTokens[i];
            string clean = token.ToLower().Replace("\"", "").Replace(".", "").Replace(",", "");

            bool isFirst = (i == 0 || rawTokens[i - 1].Contains(",") || lineIdx == 0 && i == 0);
            if (i > 0 && rawTokens[i - 1].Contains(",")) lineIdx++;

            wordList.Add(new MemoryWordData {
                id = i,
                rawWord = token,
                cleanWord = clean,
                lineIndex = lineIdx,
                isFirstInLine = isFirst,
                isHiddenInRound2 = round2HiddenIndices.Contains(i),
                isHiddenInRound3 = round3HiddenIndices.Contains(i)
            });
        }
    }

    private void ShuffleList<T>(List<T> list) {
        for (int i = 0; i < list.Count; i++) {
            T temp = list[i];
            int randomIndex = Random.Range(i, list.Count);
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private void UpdateTitleAndUIComponents() {
        if (headerTMP != null) headerTMP.text = "M01 — Learn It by Heart";
        if (subtitleTMP != null) subtitleTMP.text = "Remember the twister. One step at a time.";
    }

    private void LoadMemoryRound(int roundIndex) {
        if (roundIndex < 1 || roundIndex > 5) return;
        currentRound = roundIndex;
        isHintActive = false;

        StopAllPlaybackAndRecording();

        if (roundIndicatorTMP != null) roundIndicatorTMP.text = $"ROUND {roundIndex} OF 5";
        if (selfCheckContainerObj != null) selfCheckContainerObj.SetActive(false);
        if (playMyAttemptButton != null) playMyAttemptButton.gameObject.SetActive(userRecordedClip != null);
        if (recordBtnTMP != null) recordBtnTMP.text = "START SPEAKING";
        if (hintBtnTMP != null) hintBtnTMP.text = "HINT";

        UpdateRoundBadgesUI();
        UpdateDeskTextDisplay();

        AudioClip ariaClip = GetAriaClipForRound(roundIndex);
        StartCoroutine(PlayAriaRoundIntroRoutine(ariaClip));
    }

    private void UpdateDeskTextDisplay() {
        if (deskTextTMP == null) return;

        if (isHintActive) {
            deskTextTMP.text = string.IsNullOrEmpty(customTwisterText) ?
                "\"I wish to wish the wish you wish to wish, but if you wish the wish wishes, I won't wish the wish you wish to wish.\"" : customTwisterText;
            if (instructionTMP != null) instructionTMP.text = "HINT REVEALED: Full text temporarily shown!";
            return;
        }

        switch (currentRound) {
            case 1:
                deskTextTMP.text = string.IsNullOrEmpty(customTwisterText) ?
                    "\"I wish to wish the wish you wish to wish, but if you wish the wish wishes, I won't wish the wish you wish to wish.\"" : customTwisterText;
                if (instructionTMP != null) instructionTMP.text = "Read the whole twister aloud once.";
                break;

            case 2:
                deskTextTMP.text = BuildFormattedMemoryText(2);
                if (instructionTMP != null) instructionTMP.text = "A quarter of the words are gone. Say the line from memory!";
                break;

            case 3:
                deskTextTMP.text = BuildFormattedMemoryText(3);
                if (instructionTMP != null) instructionTMP.text = "Half the words are gone. Can you remember the rest?";
                break;

            case 4:
                deskTextTMP.text = "\"I ______________________________\nbut ____________________________\nI ______________________________\"";
                if (instructionTMP != null) instructionTMP.text = "Only the first word of each line remains!";
                break;

            case 5:
                deskTextTMP.text = "\n--- MEMORY CHALLENGE ---\n\nThe twister is completely hidden!\nSay the whole thing from memory.";
                if (instructionTMP != null) instructionTMP.text = "The twister is hidden. Say the whole thing from memory!";
                break;
        }
    }

    private string BuildFormattedMemoryText(int round) {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < wordList.Count; i++) {
            MemoryWordData data = wordList[i];
            bool hide = (round == 2) ? data.isHiddenInRound2 : data.isHiddenInRound3;

            if (hide) {
                sb.Append("____ ");
            } else {
                sb.Append(data.rawWord + " ");
            }
        }
        return sb.ToString().Trim();
    }

    private void UpdateRoundBadgesUI() {
        for (int i = 0; i < 5; i++) {
            if (roundBadgeTMPs != null && i < roundBadgeTMPs.Length && roundBadgeTMPs[i] != null) {
                if (roundCompletedState[i]) {
                    roundBadgeTMPs[i].text = $"✓ R{i + 1}";
                    roundBadgeTMPs[i].color = new Color(0.13f, 0.77f, 0.36f);
                } else if (i + 1 == currentRound) {
                    roundBadgeTMPs[i].text = $"● R{i + 1}";
                    roundBadgeTMPs[i].color = new Color(0.2f, 0.8f, 1.0f);
                } else {
                    roundBadgeTMPs[i].text = $"LOCKED R{i + 1}";
                    roundBadgeTMPs[i].color = new Color(0.6f, 0.65f, 0.8f);
                }
            }
        }
    }

    private IEnumerator PlayAriaRoundIntroRoutine(AudioClip clip) {
        yield return new WaitForSeconds(0.4f);
        if (clip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(clip);
        }
    }

    public void OnHintButtonClicked() {
        isHintActive = !isHintActive;
        if (hintBtnTMP != null) {
            hintBtnTMP.text = isHintActive ? "HIDE AGAIN" : "HINT";
        }

        if (memoryDeskCardRect != null) {
            memoryDeskCardRect.DOKill(true);
            memoryDeskCardRect.DOPunchScale(Vector3.one * 0.05f, 0.25f);
        }

        UpdateDeskTextDisplay();
    }

    public void OnRecordButtonClicked() {
        if (isRecording) {
            StopRecording();
        } else {
            StartRecording();
        }
    }

    private void StartRecording() {
        StopAllPlaybackAndRecording();

        isRecording = true;
        recordingStartTime = Time.time;
        recordingDuration = 0f;

        if (sfxRecordBeep != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }

        if (recordBtnTMP != null) recordBtnTMP.text = "STOP RECORDING";
        if (recordButton != null) {
            Image img = recordButton.GetComponent<Image>();
            if (img != null) img.color = colorBtnRecordingActive;
            recordButton.transform.DOKill();
            recordButton.transform.DOScale(Vector3.one * 1.08f, 0.4f).SetLoops(-1, LoopType.Yoyo);
        }

        if (statusTMP != null) statusTMP.text = "● Recording... Recite the twister from memory";

        if (Microphone.devices.Length > 0) {
            micRecordingClip = Microphone.Start(selectedMicrophoneDevice, false, 20, 44100);
        }

        liveTimerCoroutine = StartCoroutine(LiveTimerRoutine());
        liveWaveformCoroutine = StartCoroutine(LiveMicrophoneWaveformRoutine());
    }

    private void StopRecording() {
        if (!isRecording) return;
        isRecording = false;

        recordingDuration = Time.time - recordingStartTime;

        if (recordButton != null) {
            recordButton.transform.DOKill();
            recordButton.transform.localScale = Vector3.one;
            Image img = recordButton.GetComponent<Image>();
            if (img != null) img.color = colorBtnRecordNormal;
        }

        if (liveTimerCoroutine != null) {
            StopCoroutine(liveTimerCoroutine);
            liveTimerCoroutine = null;
        }
        if (liveWaveformCoroutine != null) {
            StopCoroutine(liveWaveformCoroutine);
            liveWaveformCoroutine = null;
        }

        if (Microphone.devices.Length > 0 && Microphone.IsRecording(selectedMicrophoneDevice)) {
            int pos = Microphone.GetPosition(selectedMicrophoneDevice);
            Microphone.End(selectedMicrophoneDevice);

            if (pos > 0 && micRecordingClip != null) {
                float[] samples = new float[pos * micRecordingClip.channels];
                micRecordingClip.GetData(samples, 0);
                userRecordedClip = AudioClip.Create($"UserRec_Round_{currentRound}", pos, micRecordingClip.channels, 44100, false);
                userRecordedClip.SetData(samples, 0);
            }
        }

        if (recordBtnTMP != null) recordBtnTMP.text = "START SPEAKING";
        if (statusTMP != null) statusTMP.text = "Recording saved!";

        if (playMyAttemptButton != null) playMyAttemptButton.gameObject.SetActive(userRecordedClip != null);

        if (selfCheckContainerObj != null) {
            selfCheckContainerObj.SetActive(true);
        } else {
            // Auto advance safeguard to next round
            OnConfirmSaidItButtonClicked();
        }
    }

    private IEnumerator LiveTimerRoutine() {
        while (isRecording) {
            recordingDuration = Time.time - recordingStartTime;
            if (timerDisplayTMP != null) {
                timerDisplayTMP.text = $"{recordingDuration:00.0}s";
            }
            yield return null;
        }
    }

    private IEnumerator LiveMicrophoneWaveformRoutine() {
        while (isRecording) {
            float volume = 0.2f;
            if (Microphone.devices.Length > 0 && micRecordingClip != null && Microphone.IsRecording(selectedMicrophoneDevice)) {
                int pos = Microphone.GetPosition(selectedMicrophoneDevice);
                if (pos > 128) {
                    float[] waveData = new float[128];
                    micRecordingClip.GetData(waveData, Mathf.Max(0, pos - 128));
                    float sum = 0f;
                    for (int i = 0; i < waveData.Length; i++) {
                        sum += Mathf.Abs(waveData[i]);
                    }
                    volume = (sum / 128f) * 8.0f;
                }
            } else {
                volume = Random.Range(0.2f, 0.75f);
            }

            for (int i = 0; i < waveformBars.Length; i++) {
                if (waveformBars[i] != null) {
                    float targetH = Mathf.Clamp(12f + (volume * Random.Range(20f, 50f)), 10f, 55f);
                    waveformBars[i].sizeDelta = new Vector2(16f, targetH);
                }
            }

            yield return new WaitForSeconds(0.06f);
        }
    }

    public void OnConfirmSaidItButtonClicked() {
        roundCompletedState[currentRound - 1] = true;
        if (selfCheckContainerObj != null) selfCheckContainerObj.SetActive(false);

        if (sfxRecordBeep != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }

        UpdateRoundBadgesUI();

        if (currentRound < 5) {
            StartCoroutine(AdvanceNextMemoryRoundRoutine(currentRound + 1));
        } else {
            StartCoroutine(FinalMemoryMasterCelebrationRoutine());
        }
    }

    public void OnTryAgainButtonClicked() {
        if (selfCheckContainerObj != null) selfCheckContainerObj.SetActive(false);
        StartRecording();
    }

    public void OnPlayMyAttemptButtonClicked() {
        if (isRecording) return;
        StopAllPlaybackAndRecording();

        if (userRecordedClip != null && Masters_AudioManager.Instance != null) {
            isPlayingUserRecording = true;
            if (playMyAttemptBtnTMP != null) playMyAttemptBtnTMP.text = "PLAYING MY ATTEMPT...";
            Masters_AudioManager.Instance.PlayVoiceOver(userRecordedClip);
            StartCoroutine(ResetPlayUserAttemptButtonState(userRecordedClip.length));
        }
    }

    private IEnumerator ResetPlayUserAttemptButtonState(float duration) {
        yield return new WaitForSeconds(duration + 0.2f);
        if (playMyAttemptBtnTMP != null) playMyAttemptBtnTMP.text = "PLAY MY ATTEMPT";
        isPlayingUserRecording = false;
    }

    private IEnumerator AdvanceNextMemoryRoundRoutine(int nextRound) {
        yield return new WaitForSeconds(0.6f);
        LoadMemoryRound(nextRound);
    }

    private IEnumerator FinalMemoryMasterCelebrationRoutine() {
        if (voM01AriaCelebration != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(voM01AriaCelebration);
        }

        yield return new WaitForSeconds(1.0f);

        if (deskTextTMP != null) {
            deskTextTMP.text = string.IsNullOrEmpty(customTwisterText) ?
                "\"I wish to wish the wish you wish to wish, but if you wish the wish wishes, I won't wish the wish you wish to wish.\"" : customTwisterText;
        }

        if (completionPanelObj != null && completionPanelTMP != null) {
            completionPanelTMP.text = "MEMORY MASTER! You learned the whole twister by heart!";
            completionPanelObj.SetActive(true);
            completionPanelObj.transform.DOKill(true);
            completionPanelObj.transform.localScale = Vector3.zero;
            completionPanelObj.transform.DOScale(Vector3.one, 0.45f).SetEase(Ease.OutBack);
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            NextButtonAnimation();
        }
    }

    private void StopAllPlaybackAndRecording() {
        if (isRecording) {
            StopRecording();
        }

        if (liveTimerCoroutine != null) {
            StopCoroutine(liveTimerCoroutine);
            liveTimerCoroutine = null;
        }

        if (liveWaveformCoroutine != null) {
            StopCoroutine(liveWaveformCoroutine);
            liveWaveformCoroutine = null;
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        isPlayingUserRecording = false;
        if (playMyAttemptBtnTMP != null) playMyAttemptBtnTMP.text = "PLAY MY ATTEMPT";
    }

    private AudioClip GetAriaClipForRound(int round) {
        switch (round) {
            case 1: return voM01AriaRound1;
            case 2: return voM01AriaRound2;
            case 3: return voM01AriaRound3;
            case 4: return voM01AriaRound4;
            case 5: return voM01AriaRound5;
            default: return null;
        }
    }

    private void SetupButtonListeners() {
        if (recordButton != null) {
            recordButton.onClick.RemoveAllListeners();
            recordButton.onClick.AddListener(OnRecordButtonClicked);
        }

        if (hintButton != null) {
            hintButton.onClick.RemoveAllListeners();
            hintButton.onClick.AddListener(OnHintButtonClicked);
        }

        if (playMyAttemptButton != null) {
            playMyAttemptButton.onClick.RemoveAllListeners();
            playMyAttemptButton.onClick.AddListener(OnPlayMyAttemptButtonClicked);
        }

        if (confirmSaidItButton != null) {
            confirmSaidItButton.onClick.RemoveAllListeners();
            confirmSaidItButton.onClick.AddListener(OnConfirmSaidItButtonClicked);
        }

        if (tryAgainButton != null) {
            tryAgainButton.onClick.RemoveAllListeners();
            tryAgainButton.onClick.AddListener(OnTryAgainButtonClicked);
        }
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Writing;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

#if UNITY_EDITOR
    private void OnValidate() {
        if (Application.isPlaying) return;
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)) return;
        if (deskTextTMP != null) {
            deskTextTMP.text = customTwisterText;
        }
    }
#endif
}
