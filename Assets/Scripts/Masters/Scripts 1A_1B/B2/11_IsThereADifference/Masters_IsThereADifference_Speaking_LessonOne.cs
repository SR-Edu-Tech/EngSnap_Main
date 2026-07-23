using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Speaking Lesson 1 for Unit 11 Is There a Difference?
/// Features full sentence speech recognition and tap-to-reveal missing word hint mechanic.
/// </summary>
public class Masters_IsThereADifference_Speaking_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class SpeakingRound {
        public string sentencePrompt;
        public string targetSpokenSentence;
        public string missingWordHint;
        public string[] acceptedPhrases;
        public AudioClip modelAudioClip;
        public string[] targetSentences;
        public AudioClip[] variationAudioClips;
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
    [SerializeField] private Button skipButton;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Animation Binding")]
    [SerializeField] private RectTransform sliderRect;
    [SerializeField] private RectTransform micRect;
    [SerializeField] private RectTransform fillRect;
    [SerializeField] private RectTransform borderRect;
    [SerializeField] private float animSpeed = 0.5f;

    private int currentRoundIndex = 0;
    private SpeakingRound currentRound;
    private bool roundCompleted = false;

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
        if (skipButton == null) {
            var skipObj = GameObject.Find("SkipButton");
            if (skipObj == null) skipObj = GameObject.Find("Skip");
            if (skipObj != null) skipButton = skipObj.GetComponent<Button>();
        }
        if (skipButton != null) {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }
        if (nextButton != null) {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();

        // Attach click listeners for tap-to-reveal hint mechanic
        if (answerPanel != null) {
            Button hintBtn = answerPanel.GetComponent<Button>();
            if (hintBtn == null) hintBtn = answerPanel.AddComponent<Button>();
            hintBtn.onClick.AddListener(OnHintClicked);
        }
        if (promptTMP != null && promptTMP.transform.parent != null) {
            Button promptBtn = promptTMP.transform.parent.GetComponent<Button>();
            if (promptBtn == null) promptBtn = promptTMP.transform.parent.gameObject.AddComponent<Button>();
            promptBtn.onClick.AddListener(OnHintClicked);
        }

        StartCoroutine(StartAnimCoroutine());
        LoadRound(0);
    }

    private void OnHintClicked() {
        if (currentRound != null && answerTMP != null && !roundCompleted) {
            answerTMP.text = $"Missing Word: <b><color=#FFD700>{currentRound.missingWordHint.ToUpper()}</color></b>";
            if (answerPanel != null) {
                answerPanel.transform.DOKill();
                answerPanel.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        }
    }

    private void OnSkipButtonClicked() {
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        LoadRound(currentRoundIndex + 1);
    }

    private void OnContinueButtonClicked() {
        if (continueButton != null) continueButton.gameObject.SetActive(false);
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        LoadRound(currentRoundIndex + 1);
    }

    private void LoadRound(int index) {
        currentRoundIndex = index;
        roundCompleted = false;

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

        if (promptTMP != null) promptTMP.text = currentRound.sentencePrompt;
        if (answerTMP != null) answerTMP.text = "💡 <i>Tap to reveal missing word</i>";
        if (debugTMP != null) debugTMP.text = "";
        if (answerPanel != null) answerPanel.SetActive(true);

        if (progressCountTMP != null) progressCountTMP.text = $"{currentRoundIndex + 1}/{rounds.Length}";
    }

    protected virtual void OnSpeechResult(string spokenText) {
        if (roundCompleted || (continueButton != null && continueButton.gameObject.activeSelf)) return;

        if (debugTMP != null) {
            debugTMP.text = spokenText;
        }

        string cleanSpoken = spokenText.ToLowerInvariant().Replace(".", " ").Replace(",", " ").Replace("!", " ").Replace("?", " ").Trim();
        string cleanTarget = currentRound.targetSpokenSentence != null ? currentRound.targetSpokenSentence.ToLowerInvariant().Replace(".", " ").Replace(",", " ").Replace("!", " ").Replace("?", " ").Trim() : "";

        bool containsMissingWord = currentRound.missingWordHint != null && cleanSpoken.Contains(currentRound.missingWordHint.ToLowerInvariant());
        bool containsPhrase = false;
        int matchedVariationIndex = -1;

        if (currentRound.acceptedPhrases != null) {
            for (int i = 0; i < currentRound.acceptedPhrases.Length; i++) {
                string ph = currentRound.acceptedPhrases[i];
                if (!string.IsNullOrEmpty(ph) && cleanSpoken.Contains(ph.ToLowerInvariant())) {
                    containsPhrase = true;
                    matchedVariationIndex = i;
                    break;
                }
            }
        }

        float bestSim = SimilarityPercent(cleanTarget, cleanSpoken);
        string bestTargetSentence = currentRound.targetSpokenSentence;
        AudioClip bestAudioClip = currentRound.modelAudioClip;

        if (currentRound.targetSentences != null && currentRound.targetSentences.Length > 0) {
            for (int i = 0; i < currentRound.targetSentences.Length; i++) {
                if (string.IsNullOrEmpty(currentRound.targetSentences[i])) continue;
                string cleanVar = currentRound.targetSentences[i].ToLowerInvariant().Replace(".", " ").Replace(",", " ").Replace("!", " ").Replace("?", " ").Trim();
                float varSim = SimilarityPercent(cleanVar, cleanSpoken);
                if (varSim > bestSim || (matchedVariationIndex == i && varSim > bestSim - 0.2f)) {
                    bestSim = varSim;
                    bestTargetSentence = currentRound.targetSentences[i];
                    if (currentRound.variationAudioClips != null && i < currentRound.variationAudioClips.Length && currentRound.variationAudioClips[i] != null) {
                        bestAudioClip = currentRound.variationAudioClips[i];
                    }
                }
            }
        } else if (matchedVariationIndex >= 0 && currentRound.variationAudioClips != null && matchedVariationIndex < currentRound.variationAudioClips.Length && currentRound.variationAudioClips[matchedVariationIndex] != null) {
            bestAudioClip = currentRound.variationAudioClips[matchedVariationIndex];
        }

        float finalScore = bestSim;
        if (containsMissingWord && containsPhrase) {
            finalScore = Mathf.Max(finalScore, 0.95f);
        } else if (containsPhrase || (containsMissingWord && bestSim > 0.5f)) {
            finalScore = Mathf.Max(finalScore, 0.85f);
        }

        if (progressBar != null) {
            progressBar.value = finalScore;
            if (sliderImage != null) sliderImage.color = Color.Lerp(wrongColor, correctColor, progressBar.value);
        }

        if (finalScore >= 0.7f) {
            roundCompleted = true;
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            if (answerTMP != null) {
                answerTMP.text = $"<b><color=#55FF55>{bestTargetSentence}</color></b>";
            }
            if (bestAudioClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(bestAudioClip);
            }
            if (continueButton != null) {
                continueButton.gameObject.SetActive(true);
                continueButton.transform.DOKill();
                continueButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f);
            }
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            if (promptTMP != null && promptTMP.transform.parent != null) {
                promptTMP.transform.parent.DOPunchPosition(Vector3.right * 10f, 0.3f, 10, 1f);
            }
        }
    }

    private float SimilarityPercent(string s1, string s2) {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0f;
        int stepsToSame = LevenshteinDistance(s1, s2);
        return 1.0f - ((float)stepsToSame / (float)Mathf.Max(s1.Length, s2.Length));
    }

    private int LevenshteinDistance(string s, string t) {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++) {
            for (int j = 1; j <= m; j++) {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Mathf.Min(Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }

    private IEnumerator StartAnimCoroutine() {
        if (sliderRect != null) sliderRect.anchoredPosition = new Vector2(-1000, sliderRect.anchoredPosition.y);
        if (micRect != null) micRect.localScale = Vector3.zero;
        yield return null;
        if (sliderRect != null) sliderRect.DOAnchorPosX(0, animSpeed).SetEase(Ease.OutBack);
        if (micRect != null) micRect.DOScale(1, animSpeed).SetEase(Ease.OutBack).SetDelay(0.2f);
    }

    private IEnumerator CloseAnimCoroutine() {
        if (sliderRect != null) sliderRect.DOAnchorPosX(-1000, animSpeed).SetEase(Ease.InBack);
        if (micRect != null) micRect.DOScale(0, animSpeed).SetEase(Ease.InBack);
        yield return new WaitForSeconds(animSpeed);
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
}
