using System;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WordMatchEvaluator : MonoBehaviour
{
    [Header("UI")]
    public TextMeshProUGUI targetWordLabel;       // set by SlideController
    public TextMeshProUGUI recognizedTextLabel;   // ASR output
    public Slider accuracySlider;                 // 0..1
    public TextMeshProUGUI accuracyPercentLabel;  // "75%"
    public CanvasGroup accuracyGroup;

    [Header("Controls")]
    public Button nextButton;   // optional (not used in speaking)

    [Header("Scoring")]
    [Range(0.5f, 1.0f)]
    public float passThreshold = 0.75f;

    private string lastSeenHypothesis = "";
    //private ListeningGameplay gameplay;
    void OnEnable()
    {
        SpeechRecognitionTest.OnRecognitionStart += HandleRecognitionStart;
        SpeechRecognitionTest.OnRecognitionFinished += HandleRecognitionFinished;
    }

    void OnDisable()
    {
        SpeechRecognitionTest.OnRecognitionStart -= HandleRecognitionStart;
        SpeechRecognitionTest.OnRecognitionFinished -= HandleRecognitionFinished;
    }

    void Start()
    {
        if (accuracySlider != null)
        {
            accuracySlider.minValue = 0f;
            accuracySlider.maxValue = 1f;
            accuracySlider.value = 0f;
        }

        if (accuracyPercentLabel != null)
            accuracyPercentLabel.text = "";

        HideAccuracyGroup();

        if (nextButton != null)
            nextButton.interactable = false;

        //gameplay = ListeningGameplay.Instance;

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextPressed);
        }
    }

    void Update()
    {
        string hypothesis = recognizedTextLabel != null ? recognizedTextLabel.text : "";

        if (!string.Equals(hypothesis, lastSeenHypothesis, StringComparison.Ordinal))
        {
            lastSeenHypothesis = hypothesis;

            if (!string.IsNullOrWhiteSpace(hypothesis) && hypothesis != "Recognizing...")
            {
                EvaluateNow();
            }
        }
    }
    private void OnNextPressed()
    {
        //if (gameplay != null)
           // gameplay.GoToNextSlide();
    }
    private void HandleRecognitionStart()
    {
        if (recognizedTextLabel)
        {
            recognizedTextLabel.color = Color.yellow;
            recognizedTextLabel.text = "Recognizing...";
        }

        ResetAccuracyUI();
    }

    private void HandleRecognitionFinished(string transcript)
    {
        if (recognizedTextLabel)
        {
            recognizedTextLabel.color = new Color32(32, 63, 10, 255);
            recognizedTextLabel.text = transcript;
        }

        lastSeenHypothesis = "";
    }
  
    private void EvaluateNow()
    {
        string reference = targetWordLabel != null ? targetWordLabel.text : "";
        string hypothesis = recognizedTextLabel != null ? recognizedTextLabel.text : "";

        float score = SimilarityPercent(reference, hypothesis);

        if (accuracySlider)
            accuracySlider.value = score;

        if (accuracyPercentLabel)
            accuracyPercentLabel.text = Mathf.RoundToInt(score * 100f) + "%";

        ShowAccuracyGroup();

        bool passed = score >= passThreshold;

        if (nextButton != null)
            nextButton.interactable = passed;
    }

    public float CurrentScore => accuracySlider != null ? accuracySlider.value : 0f;

    private void ResetAccuracyUI()
    {
        if (accuracySlider) accuracySlider.value = 0f;
        if (accuracyPercentLabel) accuracyPercentLabel.text = "";
        HideAccuracyGroup();

        if (nextButton) nextButton.interactable = false;
    }

    private void HideAccuracyGroup()
    {
        if (accuracyGroup)
        {
            accuracyGroup.alpha = 0f;
            accuracyGroup.interactable = false;
            accuracyGroup.blocksRaycasts = false;
        }
    }

    private void ShowAccuracyGroup()
    {
        if (accuracyGroup)
        {
            accuracyGroup.alpha = 1f;
            accuracyGroup.interactable = true;
            accuracyGroup.blocksRaycasts = true;
        }
    }

    // ------------------ SCORING ------------------

    private float SimilarityPercent(string reference, string hypothesis)
    {
        string a = Normalize(reference);
        string b = Normalize(hypothesis);

        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return 0f;

        if (a == b)
            return 1f;

        int dist = Levenshtein(a, b);
        int maxLen = Mathf.Max(a.Length, b.Length);
        return 1f - (float)dist / maxLen;
    }

    private string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "";

        s = s.Trim().ToLowerInvariant();

        var sb = new StringBuilder(s.Length);
        foreach (char c in s)
        {
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                sb.Append(c);
        }

        return System.Text.RegularExpressions.Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }
public void ResetAllUI()
{
    // Clear recognized text
    if (recognizedTextLabel != null)
    {
        recognizedTextLabel.text = "";
    }

    // Reset accuracy
    if (accuracySlider != null)
        accuracySlider.value = 0f;

    if (accuracyPercentLabel != null)
        accuracyPercentLabel.text = "";

    // Hide UI
    HideAccuracyGroup();

    // Disable next button again
    if (nextButton != null)
        nextButton.interactable = false;

    lastSeenHypothesis = "";
}
    private int Levenshtein(string s, string t)
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

                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost
                );
            }
        }

        return d[n, m];
    }
}
