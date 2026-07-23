using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class Logic_BB1 : MonoBehaviour, IUnitCompletable
{
    // ── IUnitCompletable ──────────────────────────────────────────────────────
    [HideInInspector] public SharedUnitPanelController panel;
    [HideInInspector] public SharedUnitButton          unitButton;

    public void OnUnitStart(SharedUnitPanelController sharedPanel, SharedUnitButton sharedButton)
    {
        panel      = sharedPanel;
        unitButton = sharedButton;
    }

    // ── Question Data ──────────────────────────────────────────────────────────
    [System.Serializable]
    public class SpeakingQuestion
    {
        [Tooltip("The word or phrase the player must say")]
        public string targetText;

        [Tooltip("Audio clip that reads the target text aloud (optional)")]
        public AudioClip questionAudio;
    }

    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("── Questions ───────────────────")]
    public SpeakingQuestion[] questions;

    [Header("── Speech Bubble ──────────────")]
    [Tooltip("Root RectTransform of the speech bubble that pops in from the character")]
    public RectTransform speechBubble;
    [Tooltip("The question text label inside the speech bubble")]
    public TextMeshProUGUI questionText;
    [Tooltip("Audio play button inside the speech bubble")]
    public Button          playQuestionButton;
    [Tooltip("Question number label (outside bubble, optional)")]
    public TextMeshProUGUI questionNumberText;

    [Header("── Recognized Text ─────────────")]
    [Tooltip("Second bubble / panel that shows what the player said")]
    public RectTransform   recognizedBubble;
    public TextMeshProUGUI recognizedTextLabel;

    [Header("── Speedometer Gauge ───────────")]
    [Tooltip("The needle RectTransform that rotates (0 = orange, +60 = red, -60 = yellow)")]
    public RectTransform   gaugeNeedle;
    [Tooltip("(Optional) CanvasGroup wrapping the whole gauge — fades in on first result")]
    public CanvasGroup     gaugeGroup;
    public TextMeshProUGUI accuracyPercentLabel;

    [Header("── Hidden Slider (logic only) ──")]
    [Tooltip("Keep wired up — slider value drives the gauge but stays invisible")]
    public Slider      accuracySlider;
    [Tooltip("CanvasGroup on the slider — kept at alpha 0 always")]
    public CanvasGroup accuracyGroup;   // left for legacy wiring; we override alpha

    [Header("── UI Controls ─────────────────")]
    public Button nextButton;
    public Button playRecordingButton;
    public Button replayQuestionButton;   // kept for back-compat; optional
    [Tooltip("Shown after 2 failed attempts; lets the player skip to the next question")]
    public Button skipButton;

    [Header("── Audio ───────────────────────")]
    public AudioSource questionAudioSource;
    public AudioSource sfxSource;
    public AudioClip   sfxWordRecognised;   // plays when final transcript arrives
    public AudioClip   sfxNeedleMove;       // plays each time needle animates

    [Header("── Scoring ─────────────────────")]
    [Range(0f, 1f)]
    public float passThreshold = 0.75f;

    [Header("── Completion ──────────────────")]
    public GameObject completedPanel;
    public Button     finishButton;

    [Header("── Toggle Button ───────────────")]
    public ToggleToTalkButton_BB1 toggleToTalkButton;

    // ── Runtime ────────────────────────────────────────────────────────────────
    private int    _currentIndex       = 0;
    private string _lastSeenHypothesis = "";
    private int    _questionsCompleted = 0;
    private bool   _started            = false;
    private int    _attemptCount       = 0;   // failed attempts on the current question

    // Needle rotation limits (degrees Z — Unity CCW positive)
    // 0 = straight up (orange mid)  +60 = full left (red)  -60 = full right (yellow)
    private const float NeedleRed    =  60f;
    private const float NeedleYellow = -60f;

    // ── Unity Lifecycle ────────────────────────────────────────────────────────
    void Start()
    {
        _started = true;

        if (completedPanel != null) completedPanel.SetActive(false);

        if (nextButton != null)
        {
            nextButton.interactable = false;
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextClicked);
        }

        if (playRecordingButton != null)
        {
            playRecordingButton.interactable = false;
            playRecordingButton.onClick.RemoveAllListeners();
            playRecordingButton.onClick.AddListener(OnPlayRecordingClicked);
        }

        if (replayQuestionButton != null)
        {
            replayQuestionButton.interactable = false;
            replayQuestionButton.onClick.RemoveAllListeners();
            replayQuestionButton.onClick.AddListener(OnReplayQuestionClicked);
        }

        if (playQuestionButton != null)
        {
            playQuestionButton.onClick.RemoveAllListeners();
            playQuestionButton.onClick.AddListener(OnPlayQuestionAudioClicked);
        }

        if (finishButton != null)
        {
            finishButton.onClick.RemoveAllListeners();
            finishButton.onClick.AddListener(OnFinishClicked);
        }

        if (skipButton != null)
        {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipClicked);
            skipButton.gameObject.SetActive(false);
        }

        if (accuracySlider != null)
        {
            accuracySlider.minValue = 0f;
            accuracySlider.maxValue = 1f;
            accuracySlider.value    = 0f;
        }

        // Keep legacy slider group invisible — gauge takes over visually
        HideAccuracyGroup();

        ResetToStart();
    }

    void OnEnable()
    {
        CrossPlatformSpeechManager.OnResultStatic         += HandleResult;
        CrossPlatformSpeechManager.OnPartialStatic        += HandlePartial;
        CrossPlatformSpeechManager.OnRecordingReadyStatic += HandleRecordingReady;

        if (_started)
            ResetToStart();
    }

    void OnDisable()
    {
        CrossPlatformSpeechManager.OnResultStatic         -= HandleResult;
        CrossPlatformSpeechManager.OnPartialStatic        -= HandlePartial;
        CrossPlatformSpeechManager.OnRecordingReadyStatic -= HandleRecordingReady;
    }

    // ── Reset ──────────────────────────────────────────────────────────────────
    void ResetToStart()
    {
        _currentIndex       = 0;
        _questionsCompleted = 0;
        _lastSeenHypothesis = "";

        if (completedPanel != null) completedPanel.SetActive(false);

        if (questions != null && questions.Length > 0)
            LoadQuestion(0);
    }

    // ── Finish ─────────────────────────────────────────────────────────────────
    void OnFinishClicked()
    {
        var cachedPanel  = panel;
        var cachedButton = unitButton;

        gameObject.SetActive(false);

        if (cachedPanel != null && cachedButton != null)
            cachedPanel.UnitFinished(cachedButton);
        else
            Debug.LogWarning("[Logic_BB1] panel or unitButton is null on finish.");
    }

    // ── Question Loading ───────────────────────────────────────────────────────
    void LoadQuestion(int index)
    {
        _currentIndex       = index;
        _lastSeenHypothesis = "";
        _attemptCount       = 0;

        var q = questions[index];

        if (questionText       != null) questionText.text       = q.targetText;
        if (questionNumberText != null) questionNumberText.text = $"Question {index + 1} / {questions.Length}";

        // Clear recognized bubble
        if (recognizedTextLabel != null) recognizedTextLabel.text = "";
        HideRecognizedBubble(instant: true);

        ResetAccuracyUI();

        if (nextButton          != null) nextButton.interactable          = false;
        if (playRecordingButton != null) playRecordingButton.interactable = false;
        if (skipButton          != null) skipButton.gameObject.SetActive(false);

        CrossPlatformSpeechManager.Instance?.ClearLastRecording();

        if (replayQuestionButton != null)
            replayQuestionButton.interactable = q.questionAudio != null;

        // Hide mic button until audio finishes
        if (toggleToTalkButton != null)
            SetMicVisible(false, instant: true);

        // Pop-in the speech bubble then auto-play question audio
        StartCoroutine(PopInBubbleThenAudio(q));
    }

    IEnumerator PopInBubbleThenAudio(SpeakingQuestion q)
    {
        // Bubble starts hidden (scale 0)
        if (speechBubble != null)
        {
            speechBubble.gameObject.SetActive(true);
            speechBubble.localScale = Vector3.zero;
            speechBubble.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        yield return new WaitForSeconds(0.4f);

        // Auto-play the question audio
        if (questionAudioSource != null && q.questionAudio != null)
        {
            questionAudioSource.Stop();
            questionAudioSource.clip = q.questionAudio;
            questionAudioSource.Play();
            yield return new WaitForSeconds(q.questionAudio.length + 0.2f);
        }
        else
        {
            yield return new WaitForSeconds(0.3f);
        }

        // Reveal mic button
        SetMicVisible(true, instant: false);
    }

    // ── Speech Callbacks ───────────────────────────────────────────────────────
    void HandleResult(string transcript)
    {
        _lastSeenHypothesis = "";

        PlaySFX(sfxWordRecognised);
        ShowRecognizedBubble(transcript, final: true);
        EvaluateAccuracy(transcript);
    }

    void HandleRecordingReady()
    {
        if (playRecordingButton != null)
            playRecordingButton.interactable = true;
    }

    void HandlePartial(string partial)
    {
        ShowRecognizedBubble(partial, final: false);

        if (!string.IsNullOrWhiteSpace(partial) &&
            !string.Equals(partial, _lastSeenHypothesis, StringComparison.Ordinal))
        {
            _lastSeenHypothesis = partial;

            if (playRecordingButton != null
                && CrossPlatformSpeechManager.Instance != null
                && CrossPlatformSpeechManager.Instance.HasRecording)
                playRecordingButton.interactable = true;

            EvaluateAccuracy(partial);
        }
    }

    // ── Recognized Bubble ─────────────────────────────────────────────────────
    void ShowRecognizedBubble(string text, bool final)
    {
        if (recognizedTextLabel != null)
        {
            recognizedTextLabel.color = final ? new Color32(32, 63, 10, 255) : Color.gray;
            recognizedTextLabel.text  = text;
        }

        if (recognizedBubble != null && recognizedBubble.localScale.x < 0.9f)
        {
            recognizedBubble.gameObject.SetActive(true);
            recognizedBubble.localScale = Vector3.zero;
            recognizedBubble.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }

    void HideRecognizedBubble(bool instant)
    {
        if (recognizedBubble == null) return;
        if (instant)
        {
            recognizedBubble.localScale = Vector3.zero;
            recognizedBubble.gameObject.SetActive(false);
        }
        else
        {
            recognizedBubble.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack)
                .OnComplete(() => recognizedBubble.gameObject.SetActive(false));
        }
    }

    // ── Mic Visibility ────────────────────────────────────────────────────────
    void SetMicVisible(bool visible, bool instant)
    {
        if (toggleToTalkButton == null) return;
        var cg = toggleToTalkButton.GetComponent<CanvasGroup>();
        if (cg == null) cg = toggleToTalkButton.gameObject.AddComponent<CanvasGroup>();

        if (instant)
        {
            cg.alpha          = visible ? 1f : 0f;
            cg.interactable   = visible;
            cg.blocksRaycasts = visible;
        }
        else
        {
            cg.DOFade(visible ? 1f : 0f, 0.3f);
            cg.interactable   = visible;
            cg.blocksRaycasts = visible;
        }
    }

    // ── Accuracy / Gauge ──────────────────────────────────────────────────────
    void EvaluateAccuracy(string hypothesis)
    {
        string reference = questions[_currentIndex].targetText;
        float  score     = SimilarityPercent(reference, hypothesis);   // 0..1

        // Keep slider value in sync (slider stays hidden)
        if (accuracySlider != null) accuracySlider.value = score;

        // Percentage label
        if (accuracyPercentLabel != null)
            accuracyPercentLabel.text = Mathf.RoundToInt(score * 100f) + "%";

        // Drive speedometer needle
        AnimateNeedle(score);

        if (nextButton != null)
            nextButton.interactable = score >= passThreshold;

        // Track failed attempts — show skip button after 2 consecutive fails
        if (score < passThreshold)
        {
            _attemptCount++;
            if (_attemptCount >= 2 && skipButton != null)
                skipButton.gameObject.SetActive(true);
        }

        // Auto-stop listening on 100% accuracy
        if (score >= 1f && toggleToTalkButton != null)
            toggleToTalkButton.ForceIdle();
    }

    /// <summary>
    /// Rotates the needle:
    ///   score 0.0 → NeedleRed   (+60°, leftmost = red)
    ///   score 0.5 → 0°           (centre = orange)
    ///   score 1.0 → NeedleYellow (-60°, rightmost = yellow)
    /// </summary>
    void AnimateNeedle(float score)
    {
        if (gaugeNeedle == null) return;

        // Map 0..1 → +60..−60 (linear)
        float targetZ = Mathf.Lerp(NeedleRed, NeedleYellow, score);

        PlaySFX(sfxNeedleMove);
        gaugeNeedle.DOLocalRotate(new Vector3(0f, 0f, targetZ), 0.5f, RotateMode.Fast)
                   .SetEase(Ease.OutElastic);
    }

    void ResetAccuracyUI()
    {
        if (accuracySlider       != null) accuracySlider.value      = 0f;
        if (accuracyPercentLabel != null) accuracyPercentLabel.text = "";
        if (nextButton           != null) nextButton.interactable   = false;

        // Reset needle instantly back to red (score 0) — gauge stays visible always
        if (gaugeNeedle != null)
        {
            gaugeNeedle.DOKill();
            gaugeNeedle.localRotation = Quaternion.Euler(0f, 0f, NeedleRed);
        }
    }

    // Gauge group is always visible — no alpha toggling
    void HideGaugeGroup(bool instant = false) { HideAccuracyGroup(); }
    void ShowGaugeGroup() { }

    void HideAccuracyGroup()
    {
        if (accuracyGroup == null) return;
        accuracyGroup.alpha = 0f; accuracyGroup.interactable = false; accuracyGroup.blocksRaycasts = false;
    }

    void ShowAccuracyGroup()   // kept for legacy — not called externally
    {
        if (accuracyGroup == null) return;
        accuracyGroup.alpha = 1f; accuracyGroup.interactable = true; accuracyGroup.blocksRaycasts = true;
    }

    // ── Button Handlers ────────────────────────────────────────────────────────
    void OnPlayRecordingClicked()  => CrossPlatformSpeechManager.Instance?.PlayLastRecording();

    void OnPlayQuestionAudioClicked()
    {
        var q = questions[_currentIndex];
        if (questionAudioSource != null && q.questionAudio != null)
        {
            questionAudioSource.Stop();
            questionAudioSource.clip = q.questionAudio;
            questionAudioSource.Play();
        }
    }

    void OnReplayQuestionClicked() => OnPlayQuestionAudioClicked();

    public void OnNextClicked()
    {
        if (nextButton != null) nextButton.interactable = false;

        CrossPlatformSpeechManager.Instance?.StopListening();
        if (toggleToTalkButton != null) toggleToTalkButton.ForceIdle();

        _questionsCompleted++;

        if (recognizedTextLabel  != null) recognizedTextLabel.text  = "";
        if (accuracyPercentLabel != null) accuracyPercentLabel.text = "";

        if (_questionsCompleted >= questions.Length)
        {
            ShowCompleted();
            return;
        }

        LoadQuestion(_currentIndex + 1);
    }

    /// <summary>
    /// Skips the current question after 2 failed attempts.
    /// The question is counted as completed so overall progress advances.
    /// </summary>
    void OnSkipClicked()
    {
        if (skipButton != null) skipButton.gameObject.SetActive(false);
        OnNextClicked();
    }

    // ── Completion ─────────────────────────────────────────────────────────────
    void ShowCompleted()
    {
        if (completedPanel != null)
        {
            completedPanel.SetActive(true);
            var cg = completedPanel.GetComponent<CanvasGroup>();
            if (cg != null) StartCoroutine(FadeIn(cg));
        }
    }

    IEnumerator FadeIn(CanvasGroup cg)
    {
        cg.alpha = 0f;
        float t  = 0f;
        while (t < 0.5f) { t += Time.deltaTime; cg.alpha = Mathf.Clamp01(t / 0.5f); yield return null; }
        cg.alpha = 1f;
    }

    // ── Public API ─────────────────────────────────────────────────────────────
    public void finish()    => OnFinishClicked();
    public void ResetGame() => ResetToStart();

    // ── SFX ───────────────────────────────────────────────────────────────────
    void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);
    }

    // ── Levenshtein ────────────────────────────────────────────────────────────
    float SimilarityPercent(string reference, string hypothesis)
    {
        string a = Normalize(reference);
        string b = Normalize(hypothesis);
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0f;
        if (a == b) return 1f;
        int dist   = Levenshtein(a, b);
        int maxLen = Mathf.Max(a.Length, b.Length);
        return 1f - (float)dist / maxLen;
    }

    string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim().ToLowerInvariant();
        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                sb.Append(c);
        return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    int Levenshtein(string s, string t)
    {
        int n = s.Length, m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;
        for (int i = 1; i <= n; i++)
        {
            char si = s[i - 1];
            for (int j = 1; j <= m; j++)
            {
                int cost = (si == t[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}