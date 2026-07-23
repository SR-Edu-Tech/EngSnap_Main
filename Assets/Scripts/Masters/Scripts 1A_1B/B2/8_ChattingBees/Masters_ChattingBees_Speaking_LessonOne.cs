using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Game Manager for Chatting Bees - Speaking Lesson One.
/// </summary>
public class Masters_ChattingBees_Speaking_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class SpeakingQuestion {
        public string promptText; 
        public AudioClip promptAudioClip;

        public string expectedAnswerText;
        public string[] acceptableSpeechVariations;
        public AudioClip expectedAnswerAudioClip;
    }

    [SerializeField]
    private SpeakingQuestion[] questions;
    
    [Header("Speaking UI")]
    [SerializeField] private TextMeshProUGUI questionTMP; 
    [SerializeField] private TextMeshProUGUI answerTMP; 
    [SerializeField] private GameObject answerPanel; 
    [SerializeField] private Button hintButton;

    [Header("Standard Speaking UI")]
    [SerializeField] private float timeToLoadNextQuestion = 2f;
    [SerializeField] private TextMeshProUGUI debugTMP;
    [SerializeField] private TextMeshProUGUI progressCountTMP;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Color correctColor, wrongColor, defaultColor;
    [SerializeField] private Image sliderImage;
    
    [Header("Animation Rects")]
    [SerializeField] private RectTransform sliderRectTransform;
    [SerializeField] private RectTransform micRectTransform;
    [SerializeField] private RectTransform fillRectTransform;
    [SerializeField] private RectTransform borderRectTransform;
    [SerializeField] private RectTransform debugRectTransform;
    [SerializeField] private float animationSpeed = 0.5f, timeBetweenEachAnimation = 0.1f;
    [SerializeField] private Button skipButton;
    [SerializeField] private Button continueButton;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private SpeakingQuestion currentQuestion;
    private int currentQuestionIndex;

    private void OnEnable() {
        CrossPlatformSpeechManager.OnResultStatic += OnSpeechResult;
    }

    private void OnDisable() {
        CrossPlatformSpeechManager.OnResultStatic -= OnSpeechResult;
    }

    protected override void Awake() {
        base.Awake();

        if (progressBar == null) Debug.LogWarning("Progress Bar is missing in the Inspector!", this);
        if (sliderImage == null) Debug.LogWarning("Slider Image is missing in the Inspector!", this);

        skipButton.onClick.AddListener(OnSkipButtonClicked);
        if (continueButton != null) {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            continueButton.gameObject.SetActive(false);
        }

        if (hintButton != null) {
            hintButton.onClick.AddListener(OnHintButtonClicked);
        }

        SetQuestionState();
    }

    private void OnHintButtonClicked() {
        // Hint system removed - answer is shown immediately.
    }

    private void OnContinueButtonClicked() {
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        SetQuestionState();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    protected override void Start() {
        base.Start();
        StartCoroutine(StartingAnimationCoroutine());
    }

    private void OnSkipButtonClicked() {
        progressCountTMP.text = $"{++currentQuestionIndex}/{questions.Length}";
        SetQuestionState();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    private IEnumerator StartingAnimationCoroutine() {
        Vector2 sliderStartPosition = new Vector2(0f, -600f);
        Vector2 micStartPosition = new Vector2(0f, -550f);
        Vector2 fillStartPosition = new Vector2(0f, -600f);
        Vector2 borderStartPosition = new Vector2(0f, -600f);
        Vector2 debugStartPosition = new Vector2(0f, -400f);

        sliderRectTransform.anchoredPosition = sliderStartPosition;
        micRectTransform.anchoredPosition = micStartPosition;
        if(fillRectTransform) fillRectTransform.anchoredPosition = fillStartPosition;
        if(borderRectTransform) borderRectTransform.anchoredPosition = borderStartPosition;
        debugRectTransform.anchoredPosition = debugStartPosition;

        Vector2 sliderTargetPosition = new Vector2(0f, 50f);
        Vector2 micTargetPosition = new Vector2(0f, 25f);
        Vector2 fillTargetPosition = Vector2.zero;
        Vector2 borderTargetPosition = Vector2.zero;
        Vector2 debugTargetPosition = new Vector2(0f, 150f);

        sliderRectTransform.DOAnchorPos(sliderTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(timeBetweenEachAnimation);
        if(fillRectTransform) fillRectTransform.DOAnchorPos(fillTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        if(borderRectTransform) borderRectTransform.DOAnchorPos(borderTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(timeBetweenEachAnimation);
        micRectTransform.DOAnchorPos(micTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        debugRectTransform.DOAnchorPos(debugTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
    }

    private IEnumerator ClosingAnimationCoroutine() {
        Vector2 sliderTargetPosition = new Vector2(0f, -600f);
        Vector2 micTargetPosition = new Vector2(0f, -550f);
        Vector2 fillTargetPosition = new Vector2(0f, -600f);
        Vector2 borderTargetPosition = new Vector2(0f, -600f);
        Vector2 debugTargetPosition = new Vector2(0f, -400f);

        micRectTransform.DOAnchorPos(micTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        debugRectTransform.DOAnchorPos(debugTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(timeBetweenEachAnimation);
        if(fillRectTransform) fillRectTransform.DOAnchorPos(fillTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        if(borderRectTransform) borderRectTransform.DOAnchorPos(borderTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(timeBetweenEachAnimation);
        sliderRectTransform.DOAnchorPos(sliderTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
    }

    private void SetQuestionState() {
        var toggleToTalk = FindObjectOfType<Masters_ToggleToTalkButton>();
        if (toggleToTalk != null) toggleToTalk.ResetButton();

        if (progressBar != null) progressBar.value = 0f;
        if (sliderImage != null) sliderImage.color = defaultColor;
        
        // Show answer immediately and hide hint button
        if (answerPanel != null) answerPanel.SetActive(true);
        if (hintButton != null) hintButton.gameObject.SetActive(false);

        if (currentQuestionIndex >= questions.Length) {
            StartCoroutine(ClosingAnimationCoroutine());
            nextButton.interactable = true;
            skipButton.interactable = false;
            if (hintButton != null) hintButton.interactable = false;
            NextButtonAnimation();
            return;
        }

        debugTMP.text = "";
        currentQuestion = questions[currentQuestionIndex];
        
        if (questionTMP != null) questionTMP.text = currentQuestion.promptText;
        if (answerTMP != null) answerTMP.text = currentQuestion.expectedAnswerText;

        if (currentQuestion.promptAudioClip != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(currentQuestion.promptAudioClip);
        }
    }

    private void OnSpeechResult(string spokenText) {
        if (continueButton != null && continueButton.gameObject.activeSelf) return;

        string spoken = spokenText.ToLower().Trim();
        debugTMP.text = $"\"{spoken}\"";

        float maxSimilarity = 0f;
        string[] expectedSpeech = currentQuestion.acceptableSpeechVariations;

        if (expectedSpeech != null) {
            foreach (var text in expectedSpeech) {
                float similarity = SimilarityPercent(text, spokenText);
                if (similarity > maxSimilarity) {
                    maxSimilarity = similarity;
                }
            }
        }

        if (progressBar != null) {
            progressBar.value = maxSimilarity;
            if (sliderImage != null) sliderImage.color = Color.Lerp(wrongColor, correctColor, progressBar.value);
        }

        if (maxSimilarity > 0.75) {
            // Correct!
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            progressCountTMP.text = $"{currentQuestionIndex + 1}/{questions.Length}";
            
            var toggleToTalk = FindObjectOfType<Masters_ToggleToTalkButton>();
            if (toggleToTalk != null) toggleToTalk.ResetButton();

            // Hide hint button just in case
            if (hintButton != null) hintButton.gameObject.SetActive(false);

            if (continueButton != null) {
                continueButton.gameObject.SetActive(true);
            } else {
                Invoke(nameof(LoadNextAfterDelay), timeToLoadNextQuestion);
            }
            
            currentQuestionIndex++;
            return;
        }

        // Wrong
        var toggleToTalkWrong = FindObjectOfType<Masters_ToggleToTalkButton>();
        if (toggleToTalkWrong != null) toggleToTalkWrong.ResetButton();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
    }

    private void LoadNextAfterDelay() {
        SetQuestionState();
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) {
            Debug.Log($"Topic not set for {this.name}!");
            return;
        }
        
        if (nextLessonSO != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            Masters_AudioManager.Instance.StopVoiceOver();
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
