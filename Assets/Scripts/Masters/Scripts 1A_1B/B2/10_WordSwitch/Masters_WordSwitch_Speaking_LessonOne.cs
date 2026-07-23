using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_WordSwitch_Speaking_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class SpeakingRound {
        public string targetWord; // LAUGHED
        public string modelSynonymsText; // giggled · chuckled · roared
        public string[] acceptedSynonymsBank; // All 12 items
        public AudioClip modelAudioClip;
    }

    [SerializeField] private SpeakingRound[] rounds;

    [Header("UI Binding")]
    [SerializeField] private TextMeshProUGUI promptTMP;
    [SerializeField] private TextMeshProUGUI answerTMP;
    [SerializeField] private TextMeshProUGUI debugTMP;
    [SerializeField] private GameObject answerPanel;
    [SerializeField] private TextMeshProUGUI progressCountTMP;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Image sliderImage;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color wrongColor = Color.red;
    [SerializeField] private Color defaultSliderColor = Color.white;

    [Header("Buttons & Navigation")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Animation Binding")]
    [SerializeField] private RectTransform sliderRect;
    [SerializeField] private RectTransform micRect;
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private RectTransform borderRect;
    [SerializeField] private float animSpeed = 0.5f;

    private int currentRoundIndex = 0;
    private SpeakingRound currentRound;

    private void OnEnable() {
        CrossPlatformSpeechManager.OnResultStatic += OnSpeechResult;
    }

    private void OnDisable() {
        CrossPlatformSpeechManager.OnResultStatic -= OnSpeechResult;
    }

    protected override void Awake() {
        base.Awake();
        if (continueButton != null) {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            continueButton.gameObject.SetActive(false);
        }
        if (nextButton != null) {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();
        StartCoroutine(StartAnimCoroutine());
        LoadRound(0);
    }

    private void OnContinueButtonClicked() {
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        LoadRound(currentRoundIndex + 1);
    }

    private void LoadRound(int index) {
        currentRoundIndex = index;

        var toggleToTalk = FindObjectOfType<Masters_ToggleToTalkButton>();
        if (toggleToTalk != null) toggleToTalk.ResetButton();

        if (progressBar != null) progressBar.value = 0f;
        if (sliderImage != null) sliderImage.color = defaultSliderColor;

        if (currentRoundIndex >= rounds.Length) {
            StartCoroutine(CloseAnimCoroutine());
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }
            NextButtonAnimation();
            return;
        }

        currentRound = rounds[currentRoundIndex];

        if (promptTMP != null) promptTMP.text = currentRound.targetWord;
        if (answerTMP != null) answerTMP.text = currentRound.modelSynonymsText;
        if (debugTMP != null) debugTMP.text = "";
        if (answerPanel != null) answerPanel.SetActive(true);

        if (progressCountTMP != null) progressCountTMP.text = $"{currentRoundIndex + 1}/{rounds.Length}";
    }

    private void OnSpeechResult(string spokenText) {
        if (continueButton != null && continueButton.gameObject.activeSelf) return;

        if (debugTMP != null) {
            debugTMP.text = spokenText;
        }

        string cleanSpoken = spokenText.ToLowerInvariant().Replace(".", " ").Replace(",", " ").Replace("!", " ").Replace("and", " ");

        int detectedMatches = 0;
        if (currentRound.acceptedSynonymsBank != null) {
            foreach (string syn in currentRound.acceptedSynonymsBank) {
                if (string.IsNullOrEmpty(syn)) continue;
                string cleanSyn = syn.ToLowerInvariant().Trim();
                if (cleanSpoken.Contains(cleanSyn)) {
                    detectedMatches++;
                }
            }
        }

        float simPercent = SimilarityPercent(currentRound.modelSynonymsText, spokenText);
        
        float finalScore = detectedMatches >= 2 ? 0.9f : (detectedMatches == 1 ? 0.5f : simPercent);
        if (simPercent > finalScore) finalScore = simPercent;

        if (progressBar != null) {
            progressBar.value = finalScore;
            if (sliderImage != null) sliderImage.color = Color.Lerp(wrongColor, correctColor, progressBar.value);
        }

        if (currentRound.modelAudioClip != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(currentRound.modelAudioClip);
        }

        float delayTime = (currentRound.modelAudioClip != null ? currentRound.modelAudioClip.length : 1.2f) + 0.8f;

        if (finalScore > 0.72f) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            if (continueButton != null) {
                continueButton.gameObject.SetActive(true);
            } else {
                Invoke(nameof(AutoNextRound), delayTime);
            }
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
    }

    private void AutoNextRound() {
        LoadRound(currentRoundIndex + 1);
    }

    private IEnumerator StartAnimCoroutine() {
        if (sliderRect) sliderRect.anchoredPosition = new Vector2(0f, -600f);
        if (micRect) micRect.anchoredPosition = new Vector2(0f, -550f);
        if (fillRect) fillRect.anchoredPosition = new Vector2(0f, -600f);
        if (borderRect) borderRect.anchoredPosition = new Vector2(0f, -600f);

        if (sliderRect) sliderRect.DOAnchorPos(new Vector2(0f, 50f), animSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(0.1f);
        if (fillRect) fillRect.DOAnchorPos(Vector2.zero, animSpeed).SetEase(Ease.OutExpo);
        if (borderRect) borderRect.DOAnchorPos(Vector2.zero, animSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(0.1f);
        if (micRect) micRect.DOAnchorPos(new Vector2(0f, 25f), animSpeed).SetEase(Ease.OutExpo);
    }

    private IEnumerator CloseAnimCoroutine() {
        if (micRect) micRect.DOAnchorPos(new Vector2(0f, -550f), animSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(0.1f);
        if (fillRect) fillRect.DOAnchorPos(new Vector2(0f, -600f), animSpeed).SetEase(Ease.OutExpo);
        if (borderRect) borderRect.DOAnchorPos(new Vector2(0f, -600f), animSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(0.1f);
        if (sliderRect) sliderRect.DOAnchorPos(new Vector2(0f, -600f), animSpeed).SetEase(Ease.OutExpo);
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        if (nextLessonSO != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            if (topic != Masters_Topic.None) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    private float SimilarityPercent(string reference, string hypothesis) {
        string a = Normalize(reference);
        string b = Normalize(hypothesis);
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0f;
        int dist = Levenshtein(a, b);
        return 1f - (float)dist / Mathf.Max(a.Length, b.Length);
    }

    private string Normalize(string s) {
        return System.Text.RegularExpressions.Regex.Replace(s.Trim().ToLowerInvariant(), @"[^a-z0-9\s]", "");
    }

    private int Levenshtein(string s, string t) {
        int n = s.Length, m = t.Length;
        int[,] d = new int[n + 1, m + 1];
        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;
        for (int i = 1; i <= n; i++) {
            for (int j = 1; j <= m; j++) {
                int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}
