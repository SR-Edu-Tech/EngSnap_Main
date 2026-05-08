using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class Logic_BB1 : MonoBehaviour
{
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

    [Header("── UI Labels ──────────────────")]
    public TextMeshProUGUI questionText;
    public TextMeshProUGUI questionNumberText;
    public TextMeshProUGUI recognizedTextLabel;
    public TextMeshProUGUI accuracyPercentLabel;

    [Header("── UI Controls ─────────────────")]
    public Slider accuracySlider;
    public CanvasGroup accuracyGroup;
    public Button nextButton;
    public Button playRecordingButton;
    public Button replayQuestionButton;

    [Header("── Audio ───────────────────────")]
    public AudioSource questionAudioSource;

    [Header("── Scoring ─────────────────────")]
    [Range(0f, 1f)]
    public float passThreshold = 0.75f;

    [Header("── Completion ──────────────────")]
    public GameObject completedPanel;
    public Button finishButton;

    [Header("── Navigation ──────────────────")]
    public GameObject speechGamePanel;
    public GameObject unitPanel;

    // ── Runtime ────────────────────────────────────────────────────────────────

    private int    _currentIndex       = 0;
    private string _lastSeenHypothesis = "";
    private int    _questionsCompleted = 0;
    private bool   _started            = false;

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

        if (finishButton != null)
        {
            finishButton.onClick.RemoveAllListeners();
            finishButton.onClick.AddListener(GoBackToUnitPanel);
        }

        if (accuracySlider != null)
        {
            accuracySlider.minValue = 0f;
            accuracySlider.maxValue = 1f;
            accuracySlider.value    = 0f;
        }

        ResetToStart();
    }

    void OnEnable()
    {
        CrossPlatformSpeechManager_BB1.OnResultStatic         += HandleResult;
        CrossPlatformSpeechManager_BB1.OnPartialStatic        += HandlePartial;
        CrossPlatformSpeechManager_BB1.OnRecordingReadyStatic += HandleRecordingReady;

        if (_started)
            ResetToStart();
    }

    void OnDisable()
    {
        CrossPlatformSpeechManager_BB1.OnResultStatic         -= HandleResult;
        CrossPlatformSpeechManager_BB1.OnPartialStatic        -= HandlePartial;
        CrossPlatformSpeechManager_BB1.OnRecordingReadyStatic -= HandleRecordingReady;
    }

    // ── Reset / Navigation ─────────────────────────────────────────────────────

    void ResetToStart()
    {
        _currentIndex       = 0;
        _questionsCompleted = 0;
        _lastSeenHypothesis = "";

        Debug.Log($"[Logic_BB1] ResetToStart — totalQuestions={questions?.Length}");

        if (completedPanel != null) completedPanel.SetActive(false);

        if (questions != null && questions.Length > 0)
            LoadQuestion(0);
    }

    void GoBackToUnitPanel()
    {
        if (unitPanel != null) unitPanel.SetActive(true);
        GameObject panelToHide = speechGamePanel != null ? speechGamePanel : gameObject;
        panelToHide.SetActive(false);
    }

    // ── Question Loading ───────────────────────────────────────────────────────

    void LoadQuestion(int index)
    {
        _currentIndex       = index;
        _lastSeenHypothesis = "";

        Debug.Log($"[Logic_BB1] LoadQuestion({index}) — _questionsCompleted={_questionsCompleted} totalQuestions={questions.Length}");

        var q = questions[index];

        if (questionText != null)        questionText.text        = q.targetText;
        if (questionNumberText != null)  questionNumberText.text  = $"Question {index + 1} / {questions.Length}";
        if (recognizedTextLabel != null) recognizedTextLabel.text = "";

        ResetAccuracyUI();

        if (nextButton != null)          nextButton.interactable          = false;
        if (playRecordingButton != null)  playRecordingButton.interactable = false;

        CrossPlatformSpeechManager_BB1.Instance?.ClearLastRecording();

        if (replayQuestionButton != null)
            replayQuestionButton.interactable = q.questionAudio != null;

        if (questionAudioSource != null && q.questionAudio != null)
        {
            questionAudioSource.Stop();
            questionAudioSource.clip = q.questionAudio;
            questionAudioSource.Play();
        }
    }

    // ── Speech Callbacks ───────────────────────────────────────────────────────

    void HandleResult(string transcript)
    {
        _lastSeenHypothesis = "";

        if (recognizedTextLabel != null)
        {
            recognizedTextLabel.color = new Color32(32, 63, 10, 255);
            recognizedTextLabel.text  = transcript;
        }

        EvaluateAccuracy(transcript);
    }

    void HandleRecordingReady()
    {
        if (playRecordingButton != null)
            playRecordingButton.interactable = true;
    }

    void HandlePartial(string partial)
    {
        if (recognizedTextLabel != null)
        {
            recognizedTextLabel.color = Color.yellow;
            recognizedTextLabel.text  = partial;
        }

        if (!string.IsNullOrWhiteSpace(partial) &&
            !string.Equals(partial, _lastSeenHypothesis, StringComparison.Ordinal))
        {
            _lastSeenHypothesis = partial;

            if (playRecordingButton != null
                && CrossPlatformSpeechManager_BB1.Instance != null
                && CrossPlatformSpeechManager_BB1.Instance.HasRecording)
                playRecordingButton.interactable = true;

            EvaluateAccuracy(partial);
        }
    }

    // ── Accuracy ───────────────────────────────────────────────────────────────

    void EvaluateAccuracy(string hypothesis)
    {
        string reference = questions[_currentIndex].targetText;
        float  score     = SimilarityPercent(reference, hypothesis);

        if (accuracySlider != null)       accuracySlider.value      = score;
        if (accuracyPercentLabel != null)  accuracyPercentLabel.text = Mathf.RoundToInt(score * 100f) + "%";

        ShowAccuracyGroup();

        if (nextButton != null)
            nextButton.interactable = score >= passThreshold;
    }

    void ResetAccuracyUI()
    {
        if (accuracySlider != null)       accuracySlider.value      = 0f;
        if (accuracyPercentLabel != null)  accuracyPercentLabel.text = "";
        HideAccuracyGroup();
        if (nextButton != null) nextButton.interactable = false;
    }

    void HideAccuracyGroup()
    {
        if (accuracyGroup == null) return;
        accuracyGroup.alpha          = 0f;
        accuracyGroup.interactable   = false;
        accuracyGroup.blocksRaycasts = false;
    }

    void ShowAccuracyGroup()
    {
        if (accuracyGroup == null) return;
        accuracyGroup.alpha          = 1f;
        accuracyGroup.interactable   = true;
        accuracyGroup.blocksRaycasts = true;
    }

    // ── Button Handlers ────────────────────────────────────────────────────────

    void OnPlayRecordingClicked()  => CrossPlatformSpeechManager_BB1.Instance?.PlayLastRecording();

    void OnReplayQuestionClicked()
    {
        var q = questions[_currentIndex];
        if (questionAudioSource != null && q.questionAudio != null)
        {
            questionAudioSource.Stop();
            questionAudioSource.clip = q.questionAudio;
            questionAudioSource.Play();
        }
    }

    public void OnNextClicked()
    {
        if (nextButton != null) nextButton.interactable = false;

        _questionsCompleted++;

        Debug.Log($"[Logic_BB1] OnNextClicked — _questionsCompleted={_questionsCompleted} totalQuestions={questions.Length} _currentIndex={_currentIndex}");
        Debug.Log($"[Logic_BB1] OnNextClicked ENTRY — stack: {new System.Diagnostics.StackTrace()}");
        if (_questionsCompleted >= questions.Length)
        {
            Debug.Log("[Logic_BB1] All questions done → ShowCompleted");
            ShowCompleted();
            return;
        }

        LoadQuestion(_currentIndex + 1);
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
        while (t < 0.5f)
        {
            t        += Time.deltaTime;
            cg.alpha  = Mathf.Clamp01(t / 0.5f);
            yield return null;
        }
        cg.alpha = 1f;
    }

    // ── Legacy / Public API ────────────────────────────────────────────────────

    public void finish()    => GoBackToUnitPanel();
    public void ResetGame() => ResetToStart();

    // ── Levenshtein Similarity ─────────────────────────────────────────────────

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