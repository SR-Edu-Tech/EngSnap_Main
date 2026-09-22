using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Core Game Manager for Unit 8 (Source of Inspiration / Tongue Twisters) Listening Lesson One:
/// L01 — Hear the Twister, Line by Line.
/// Features:
/// - 3 Speed Control Options (SLOW 0.75x, NORMAL 1.0x, FAST 1.25x).
/// - 3 Line Cards with Line text, Play buttons, Heard status checkmarks, and audio progress sliders.
/// - Unlimited replays for any line at any speed.
/// - Automatic Full Twister playback once all 3 lines have been heard at least once.
/// - No scoring / no fail state. Unlocks topic completion upon hearing all lines and full twister.
/// </summary>
public class Masters_SourceOfInspiration_Listening_LessonOne : Masters_Lesson {

    public enum PlaybackSpeed {
        SLOW = 0,
        NORMAL = 1,
        FAST = 2
    }

    [System.Serializable]
    public class LineAudioSet {
        public AudioClip slowClip;
        public AudioClip normalClip;
        public AudioClip fastClip;
    }

    [Header("Header & Instructions")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI instructionTMP;

    [Header("Speed Control Bar")]
    [SerializeField] private Button slowSpeedButton;
    [SerializeField] private Button normalSpeedButton;
    [SerializeField] private Button fastSpeedButton;
    [SerializeField] private Image slowSpeedImage;
    [SerializeField] private Image normalSpeedImage;
    [SerializeField] private Image fastSpeedImage;

    [Header("Line Cards (3 Lines)")]
    [SerializeField] private RectTransform[] lineCardRects = new RectTransform[3];
    [SerializeField] private TextMeshProUGUI[] lineTitleTMPs = new TextMeshProUGUI[3];
    [SerializeField] private TextMeshProUGUI[] lineTextTMPs = new TextMeshProUGUI[3];
    [SerializeField] private Button[] linePlayButtons = new Button[3];
    [SerializeField] private TextMeshProUGUI[] linePlayBtnTMPs = new TextMeshProUGUI[3];
    [SerializeField] private TextMeshProUGUI[] lineStatusTMPs = new TextMeshProUGUI[3];
    [SerializeField] private Image[] lineProgressFillImages = new Image[3];
    [SerializeField] private Image[] lineCardBorderImages = new Image[3];

    [Header("Editable Line Box Texts (Change Box Text Here)")]
    [TextArea(2, 4)] [SerializeField] private string customLine1Text = "\"I wish to wish the wish you wish to wish\"";
    [TextArea(2, 4)] [SerializeField] private string customLine2Text = "\"but if you wish the wish the witch wishes\"";
    [TextArea(2, 4)] [SerializeField] private string customLine3Text = "\"I won't wish the wish you wish to wish\"";

    [Header("Full Twister Card & Banner")]
    [SerializeField] private RectTransform fullTwisterCardRect;
    [SerializeField] private TextMeshProUGUI fullTwisterTitleTMP;
    [SerializeField] private TextMeshProUGUI fullTwisterTextTMP;
    [SerializeField] private TextMeshProUGUI fullTwisterStatusTMP;
    [SerializeField] private Button fullTwisterPlayButton;
    [SerializeField] private TextMeshProUGUI fullTwisterPlayBtnTMP;
    [SerializeField] private Image fullTwisterProgressFillImage;
    [SerializeField] private TextMeshProUGUI completionBannerTMP;

    [Header("Audio Data Assets")]
    [SerializeField] private LineAudioSet line1Audio;
    [SerializeField] private LineAudioSet line2Audio;
    [SerializeField] private LineAudioSet line3Audio;
    [SerializeField] private LineAudioSet fullTwisterAudio;
    [SerializeField] private AudioClip ariaIntroAudio;

    private PlaybackSpeed currentSpeed = PlaybackSpeed.NORMAL;
    private bool[] lineHeardState = new bool[3] { false, false, false };
    private int currentlyPlayingIndex = -1; // -1: none, 0-2: lines, 3: full twister
    private Coroutine playbackCoroutine;

    private Color activeSpeedColor = new Color(0.13f, 0.77f, 0.36f); // Green
    private Color inactiveSpeedColor = new Color(0.18f, 0.25f, 0.40f); // Dark Blue
    private Color cardNormalBorderColor = new Color(0.25f, 0.35f, 0.55f, 0.6f);
    private Color cardActiveBorderColor = new Color(0.13f, 0.77f, 0.36f, 1f);

    protected virtual void OnEnable() {
        // Prevent STT subscriptions
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Listening;
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
        topic = Masters_Topic.Listening;
        narratorSpeech = null;
        CancelInvoke();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        DeactivateObsoleteBaseUI();
        AutoFindUIReferences();
        InitializeAudioReferences();
        UpdateTitleAndUIComponents();
        SetupButtonListeners();

        if (nextButton != null) nextButton.gameObject.SetActive(false);

        SetPlaybackSpeed(PlaybackSpeed.NORMAL);
        HighlightActiveLineCard(0);

        StartCoroutine(InitializeL01LessonRoutine());
    }

    private void DeactivateObsoleteBaseUI() {
        string[] obsoleteNames = new string[] {
            "UnitHeading", "Character", "Sign Board left", "Sign Board Right", "Formal", "Informal",
            "MagnetStations", "WordTiles", "CompletedPhraseGlow", "ARIA", "SkipButton", "Continue", "Heading", "Header",
            "SpeechBubble", "SoundBench", "ScoreIndicator", "ScoreText", "ProgressIndicator", "ProgressText",
            "ResultPanel", "ResultPopup", "HubButtons", "GET", "CATCH", "IDEA", "SAVE", "HubButton", "OptionButton"
        };

        foreach (Transform child in transform) {
            if (child == null) continue;
            string cName = child.name;
            bool isObsolete = false;
            foreach (var obs in obsoleteNames) {
                if (cName.Equals(obs, System.StringComparison.OrdinalIgnoreCase) || cName.StartsWith("Hub") || cName.StartsWith("Option") || cName.StartsWith("Button_")) {
                    isObsolete = true;
                    break;
                }
            }

            if (isObsolete) {
                child.gameObject.SetActive(false);
            } else {
                TMP_Text[] tmps = child.GetComponentsInChildren<TMP_Text>(true);
                foreach (var tmp in tmps) {
                    if (tmp == null) continue;
                    string txt = tmp.text ?? "";
                    if (txt.Contains("HEAR IT") || txt.Contains("FORMAL") || txt.Contains("INFORMAL") || txt.Contains("Score:") || txt.Contains("0/8") || txt.Contains("0/12") || txt.Contains("Question")) {
                        tmp.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void InitializeAudioReferences() {
#if UNITY_EDITOR
        string audioDir = "Assets/Audio/2A/8_SourceOfInspiration/Listening/L01/";

        if (line1Audio == null) line1Audio = new LineAudioSet();
        line1Audio.slowClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_L01_LINE1_SLOW.mp3");
        line1Audio.normalClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_L01_LINE1_NORMAL.mp3");
        line1Audio.fastClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_L01_LINE1_FAST.mp3");

        if (line2Audio == null) line2Audio = new LineAudioSet();
        line2Audio.slowClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_L01_LINE2_SLOW.mp3");
        line2Audio.normalClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_L01_LINE2_NORMAL.mp3");
        line2Audio.fastClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_L01_LINE2_FAST.mp3");

        if (line3Audio == null) line3Audio = new LineAudioSet();
        line3Audio.slowClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_L01_LINE3_SLOW.mp3");
        line3Audio.normalClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_L01_LINE3_NORMAL.mp3");
        line3Audio.fastClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_L01_LINE3_FAST.mp3");

        if (fullTwisterAudio == null) fullTwisterAudio = new LineAudioSet();
        fullTwisterAudio.slowClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_L01_FULL_SLOW.mp3");
        fullTwisterAudio.normalClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_L01_FULL_NORMAL.mp3");
        fullTwisterAudio.fastClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_L01_FULL_FAST.mp3");

        ariaIntroAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "VO_L01_ARIA.mp3");
#endif
    }

    private IEnumerator InitializeL01LessonRoutine() {
        if (ariaIntroAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(ariaIntroAudio);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(0.4f);
        }
    }

    public void SetPlaybackSpeed(PlaybackSpeed speed) {
        currentSpeed = speed;

        if (slowSpeedImage != null) slowSpeedImage.color = (speed == PlaybackSpeed.SLOW) ? activeSpeedColor : inactiveSpeedColor;
        if (normalSpeedImage != null) normalSpeedImage.color = (speed == PlaybackSpeed.NORMAL) ? activeSpeedColor : inactiveSpeedColor;
        if (fastSpeedImage != null) fastSpeedImage.color = (speed == PlaybackSpeed.FAST) ? activeSpeedColor : inactiveSpeedColor;

        // If a line is currently playing, restart it at the new speed!
        if (currentlyPlayingIndex >= 0) {
            int activeIdx = currentlyPlayingIndex;
            StopCurrentPlayback();
            if (activeIdx >= 0 && activeIdx <= 2) {
                PlayLine(activeIdx);
            } else if (activeIdx == 3) {
                PlayFullTwister();
            }
        }
    }

    public void PlayLine(int lineIndex) {
        if (lineIndex < 0 || lineIndex > 2) return;

        StopCurrentPlayback();
        currentlyPlayingIndex = lineIndex;
        HighlightActiveLineCard(lineIndex);

        AudioClip clipToPlay = GetClipForLine(lineIndex, currentSpeed);

        playbackCoroutine = StartCoroutine(LinePlaybackRoutine(lineIndex, clipToPlay));
    }

    private IEnumerator LinePlaybackRoutine(int lineIndex, AudioClip clip) {
        if (lineIndex >= 0 && lineIndex < 3) {
            if (linePlayBtnTMPs[lineIndex] != null) linePlayBtnTMPs[lineIndex].text = "PLAYING...";
            if (lineProgressFillImages[lineIndex] != null) lineProgressFillImages[lineIndex].fillAmount = 0f;
        }

        float duration = (clip != null) ? clip.length : 2.5f;

        if (clip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(clip);
        }

        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float fill = Mathf.Clamp01(elapsed / duration);
            if (lineIndex >= 0 && lineIndex < 3 && lineProgressFillImages[lineIndex] != null) {
                lineProgressFillImages[lineIndex].fillAmount = fill;
            }
            yield return null;
        }

        if (lineIndex >= 0 && lineIndex < 3 && lineProgressFillImages[lineIndex] != null) {
            lineProgressFillImages[lineIndex].fillAmount = 1f;
        }

        // Mark line as heard
        lineHeardState[lineIndex] = true;
        currentlyPlayingIndex = -1;

        UpdateLineCardStatusUI(lineIndex);

        CheckStageCompletion();
    }

    public void PlayFullTwister() {
        StopCurrentPlayback();
        currentlyPlayingIndex = 3;

        AudioClip clipToPlay = GetClipForFull(currentSpeed);
        playbackCoroutine = StartCoroutine(FullTwisterPlaybackRoutine(clipToPlay));
    }

    private IEnumerator FullTwisterPlaybackRoutine(AudioClip clip) {
        if (fullTwisterPlayBtnTMP != null) fullTwisterPlayBtnTMP.text = "PLAYING...";
        if (fullTwisterProgressFillImage != null) fullTwisterProgressFillImage.fillAmount = 0f;
        if (fullTwisterStatusTMP != null) fullTwisterStatusTMP.text = "FULL TWISTER PLAYING...";

        float duration = (clip != null) ? clip.length : 4.5f;

        if (clip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(clip);
        }

        float elapsed = 0f;
        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float fill = Mathf.Clamp01(elapsed / duration);
            if (fullTwisterProgressFillImage != null) fullTwisterProgressFillImage.fillAmount = fill;
            yield return null;
        }

        if (fullTwisterProgressFillImage != null) fullTwisterProgressFillImage.fillAmount = 1f;
        if (fullTwisterPlayBtnTMP != null) fullTwisterPlayBtnTMP.text = "REPLAY FULL TWISTER";
        if (fullTwisterStatusTMP != null) fullTwisterStatusTMP.text = "You've completed this listening stage!";

        currentlyPlayingIndex = -1;

        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            NextButtonAnimation();
        }
    }

    private void StopCurrentPlayback() {
        if (playbackCoroutine != null) {
            StopCoroutine(playbackCoroutine);
            playbackCoroutine = null;
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (currentlyPlayingIndex >= 0 && currentlyPlayingIndex <= 2) {
            UpdateLineCardStatusUI(currentlyPlayingIndex);
        } else if (currentlyPlayingIndex == 3) {
            if (fullTwisterPlayBtnTMP != null) fullTwisterPlayBtnTMP.text = "PLAY FULL TWISTER";
        }

        currentlyPlayingIndex = -1;
    }

    private void CheckStageCompletion() {
        if (lineHeardState[0] && lineHeardState[1] && lineHeardState[2]) {
            if (completionBannerTMP != null) {
                completionBannerTMP.text = "Great! You've heard all three lines.";
                completionBannerTMP.gameObject.SetActive(true);
                completionBannerTMP.transform.DOKill();
                completionBannerTMP.transform.localScale = Vector3.zero;
                completionBannerTMP.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
            }

            if (fullTwisterCardRect != null) {
                fullTwisterCardRect.gameObject.SetActive(true);
                fullTwisterCardRect.DOKill();
                fullTwisterCardRect.localScale = Vector3.zero;
                fullTwisterCardRect.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
            }

            // Automatically play full twister end-to-end
            PlayFullTwister();
        }
    }

    private AudioClip GetClipForLine(int lineIndex, PlaybackSpeed speed) {
        LineAudioSet audioSet = (lineIndex == 0) ? line1Audio : (lineIndex == 1) ? line2Audio : line3Audio;
        switch (speed) {
            case PlaybackSpeed.SLOW: return audioSet.slowClip;
            case PlaybackSpeed.FAST: return audioSet.fastClip;
            default: return audioSet.normalClip;
        }
    }

    private AudioClip GetClipForFull(PlaybackSpeed speed) {
        switch (speed) {
            case PlaybackSpeed.SLOW: return fullTwisterAudio.slowClip;
            case PlaybackSpeed.FAST: return fullTwisterAudio.fastClip;
            default: return fullTwisterAudio.normalClip;
        }
    }

    private void HighlightActiveLineCard(int activeIndex) {
        for (int i = 0; i < 3; i++) {
            if (lineCardBorderImages[i] != null) {
                lineCardBorderImages[i].color = (i == activeIndex) ? cardActiveBorderColor : cardNormalBorderColor;
            }
        }
    }

    private void UpdateLineCardStatusUI(int index) {
        if (index < 0 || index >= 3) return;

        bool heard = lineHeardState[index];
        bool isPlaying = (currentlyPlayingIndex == index);

        // Ensure constant dark blue card container color for all 3 cards
        if (lineCardRects[index] != null) {
            Image cardBg = lineCardRects[index].GetComponent<Image>();
            if (cardBg != null) {
                cardBg.color = new Color(0.08f, 0.14f, 0.28f, 0.95f);
            }
        }

        if (lineStatusTMPs[index] != null) {
            lineStatusTMPs[index].text = heard ? "HEARD" : "";
            lineStatusTMPs[index].color = new Color(0.13f, 0.77f, 0.36f);
        }

        if (linePlayButtons[index] != null) {
            Image btnImg = linePlayButtons[index].GetComponent<Image>();
            if (btnImg != null) {
                btnImg.color = isPlaying ? new Color(0.13f, 0.77f, 0.36f, 1.0f) : new Color(0.12f, 0.40f, 0.85f, 1.0f);
            }
        }

        if (linePlayBtnTMPs[index] != null) {
            linePlayBtnTMPs[index].text = isPlaying ? "PLAYING..." : (heard ? "HEARD — REPLAY" : "PLAY");
        }
    }

    private void UpdateTitleAndUIComponents() {
        if (headerTMP != null) {
            headerTMP.text = "L01 — Hear the Twister, Line by Line";
        }

        if (instructionTMP != null) {
            instructionTMP.text = "Listen to each line at any speed. Replay as many times as you like.";
        }

        string[] titles = new string[] { "LINE 1", "LINE 2", "LINE 3" };
        string[] customLines = new string[] { customLine1Text, customLine2Text, customLine3Text };
        string[] defaultLines = new string[] {
            "\"I wish to wish the wish you wish to wish\"",
            "\"but if you wish the wish the witch wishes\"",
            "\"I won't wish the wish you wish to wish\""
        };

        for (int i = 0; i < 3; i++) {
            if (lineTitleTMPs[i] != null) lineTitleTMPs[i].text = titles[i];
            if (lineTextTMPs[i] != null) {
                // If user entered custom text in Inspector box, use it! Otherwise preserve text or fallback
                if (!string.IsNullOrEmpty(customLines[i])) {
                    lineTextTMPs[i].text = customLines[i];
                } else if (string.IsNullOrEmpty(lineTextTMPs[i].text)) {
                    lineTextTMPs[i].text = defaultLines[i];
                }
            }
            UpdateLineCardStatusUI(i);
        }

        if (fullTwisterTitleTMP != null) fullTwisterTitleTMP.text = "FULL TONGUE TWISTER";
        if (fullTwisterTextTMP != null) fullTwisterTextTMP.text = "\"I wish to wish the wish you wish to wish, but if you wish the wish the witch wishes, I won't wish the wish you wish to wish.\"";
    }

#if UNITY_EDITOR
    private void OnValidate() {
        if (Application.isPlaying) return;
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)) return;
        string[] customLines = new string[] { customLine1Text, customLine2Text, customLine3Text };
        for (int i = 0; i < 3; i++) {
            if (lineTextTMPs != null && i < lineTextTMPs.Length && lineTextTMPs[i] != null && !string.IsNullOrEmpty(customLines[i])) {
                lineTextTMPs[i].text = customLines[i];
            }
        }
    }
#endif

    private void SetupButtonListeners() {
        if (slowSpeedButton != null) {
            slowSpeedButton.interactable = true;
            Image img = slowSpeedButton.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
            TextMeshProUGUI txt = slowSpeedButton.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.raycastTarget = false;

            slowSpeedButton.onClick.RemoveAllListeners();
            slowSpeedButton.onClick.AddListener(() => SetPlaybackSpeed(PlaybackSpeed.SLOW));
        }

        if (normalSpeedButton != null) {
            normalSpeedButton.interactable = true;
            Image img = normalSpeedButton.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
            TextMeshProUGUI txt = normalSpeedButton.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.raycastTarget = false;

            normalSpeedButton.onClick.RemoveAllListeners();
            normalSpeedButton.onClick.AddListener(() => SetPlaybackSpeed(PlaybackSpeed.NORMAL));
        }

        if (fastSpeedButton != null) {
            fastSpeedButton.interactable = true;
            Image img = fastSpeedButton.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
            TextMeshProUGUI txt = fastSpeedButton.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.raycastTarget = false;

            fastSpeedButton.onClick.RemoveAllListeners();
            fastSpeedButton.onClick.AddListener(() => SetPlaybackSpeed(PlaybackSpeed.FAST));
        }

        for (int i = 0; i < 3; i++) {
            int lineIdx = i;
            if (linePlayButtons[i] != null) {
                linePlayButtons[i].interactable = true;
                Image img = linePlayButtons[i].GetComponent<Image>();
                if (img != null) img.raycastTarget = true;
                TextMeshProUGUI txt = linePlayButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (txt != null) txt.raycastTarget = false;

                linePlayButtons[i].onClick.RemoveAllListeners();
                linePlayButtons[i].onClick.AddListener(() => PlayLine(lineIdx));
            }
        }

        if (fullTwisterPlayButton != null) {
            fullTwisterPlayButton.interactable = true;
            Image img = fullTwisterPlayButton.GetComponent<Image>();
            if (img != null) img.raycastTarget = true;
            TextMeshProUGUI txt = fullTwisterPlayButton.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null) txt.raycastTarget = false;

            fullTwisterPlayButton.onClick.RemoveAllListeners();
            fullTwisterPlayButton.onClick.AddListener(PlayFullTwister);
        }
    }

    protected override void OnNextButtonClicked() {
        ReturnToHub();
    }

    public void ReturnToHub() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Listening);
        }
    }

    private void AutoFindUIReferences() {
        if (headerTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("Title");
            if (t != null) headerTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (instructionTMP == null) {
            Transform t = transform.Find("InstructionText") ?? transform.Find("Instruction");
            if (t != null) instructionTMP = t.GetComponent<TextMeshProUGUI>();
        }

        if (slowSpeedButton == null) {
            Transform t = transform.Find("SpeedBar/SlowButton");
            if (t != null) {
                slowSpeedButton = t.GetComponent<Button>();
                slowSpeedImage = t.GetComponent<Image>();
            }
        }
        if (normalSpeedButton == null) {
            Transform t = transform.Find("SpeedBar/NormalButton");
            if (t != null) {
                normalSpeedButton = t.GetComponent<Button>();
                normalSpeedImage = t.GetComponent<Image>();
            }
        }
        if (fastSpeedButton == null) {
            Transform t = transform.Find("SpeedBar/FastButton");
            if (t != null) {
                fastSpeedButton = t.GetComponent<Button>();
                fastSpeedImage = t.GetComponent<Image>();
            }
        }

        for (int i = 0; i < 3; i++) {
            Transform card = transform.Find($"LineCard_{i + 1}");
            if (card != null) {
                lineCardRects[i] = card.GetComponent<RectTransform>();
                lineCardBorderImages[i] = card.GetComponent<Image>();
                lineTitleTMPs[i] = card.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                lineTextTMPs[i] = card.Find("LineText")?.GetComponent<TextMeshProUGUI>();
                lineStatusTMPs[i] = card.Find("StatusText")?.GetComponent<TextMeshProUGUI>();

                Transform playBtn = card.Find("PlayButton");
                if (playBtn != null) {
                    linePlayButtons[i] = playBtn.GetComponent<Button>();
                    linePlayBtnTMPs[i] = playBtn.GetComponentInChildren<TextMeshProUGUI>();
                }
                lineProgressFillImages[i] = card.Find("ProgressBar/Fill")?.GetComponent<Image>();
            }
        }

        if (fullTwisterCardRect == null) {
            Transform t = transform.Find("FullTwisterCard");
            if (t != null) {
                fullTwisterCardRect = t.GetComponent<RectTransform>();
                fullTwisterTitleTMP = t.Find("TitleText")?.GetComponent<TextMeshProUGUI>();
                fullTwisterTextTMP = t.Find("LineText")?.GetComponent<TextMeshProUGUI>();
                fullTwisterStatusTMP = t.Find("StatusText")?.GetComponent<TextMeshProUGUI>();

                Transform playBtn = t.Find("PlayButton");
                if (playBtn != null) {
                    fullTwisterPlayButton = playBtn.GetComponent<Button>();
                    fullTwisterPlayBtnTMP = playBtn.GetComponentInChildren<TextMeshProUGUI>();
                }
                fullTwisterProgressFillImage = t.Find("ProgressBar/Fill")?.GetComponent<Image>();
            }
        }

        if (completionBannerTMP == null) {
            Transform t = transform.Find("CompletionBanner");
            if (t != null) completionBannerTMP = t.GetComponent<TextMeshProUGUI>();
        }
    }
}