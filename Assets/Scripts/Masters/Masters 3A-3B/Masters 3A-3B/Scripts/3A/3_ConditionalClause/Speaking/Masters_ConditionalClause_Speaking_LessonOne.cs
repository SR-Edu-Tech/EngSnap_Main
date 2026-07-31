using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Core controller for Unit 1: Boost Someone Up! - Speaking Lesson One (SP01).
/// Adapted from Book 2A PolishedCommunication Speaking L1.
/// Manages phrase card spawning, speech recognition via Levenshtein similarity, UI animations, and progression.
/// </summary>
public class Masters_ConditionalClause_Speaking_LessonOne : Masters_Lesson {

    private const string SET_PHRASE_CARD_AND_SPEECH_TO_TEXT = "SetPhraseCardAndSpeechToText";

    [System.Serializable]
    public class SpeechToText {
        public string phraseCardText;
        public string[] speechDetectionText;
        public AudioClip statementAudioClip;
    }

    [Header("Speech To Text Data")]
    [SerializeField] protected SpeechToText[] speechToTextArray;
    [SerializeField] private GameObject phraseCardReferenceGameObject;
    [SerializeField] private RectTransform phraseCardSpawnPointRectTransform;
    [SerializeField] private float timeToLoadNextSpeechToText = 2f;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI debugTMP;
    [SerializeField] private TextMeshProUGUI progressCountTMP;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color wrongColor = Color.red;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Image sliderImage;
    [SerializeField] private RectTransform sliderRectTransform;
    [SerializeField] private RectTransform micRectTransform;
    [SerializeField] private RectTransform fillRectTransform;
    [SerializeField] private RectTransform borderRectTransform;
    [SerializeField] private RectTransform debugRectTransform;
    
    [Header("Animation Settings")]
    [SerializeField] private float animationSpeed = 0.5f;
    [SerializeField] private float timeBetweenEachAnimation = 0.2f;

    [Header("Buttons")]
    [SerializeField] private Button skipButton;
    [SerializeField] private Button continueButton;

    [Header("Routing")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private SpeechToText currentSpeechToText;
    private int currentSpeechToTextIndex;
    private GameObject currentPhraseCardGameObject;

    protected virtual void OnEnable() {
        CrossPlatformSpeechManager.OnResultStatic += OnSpeechResult;
    }

    protected virtual void OnDisable() {
        CrossPlatformSpeechManager.OnResultStatic -= OnSpeechResult;
    }

    protected override void Awake() {
        base.Awake();

        if (skipButton != null) {
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }

        if (continueButton != null) {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            continueButton.gameObject.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        SetPhraseCardAndSpeechToText();
        StartCoroutine(StartingAnimationCoroutine());
    }

    private void OnContinueButtonClicked() {
        if (continueButton != null) {
            continueButton.gameObject.SetActive(false);
        }
        SetPhraseCardAndSpeechToText();
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    private void OnSkipButtonClicked() {
        if (progressCountTMP != null && speechToTextArray != null) {
            progressCountTMP.text = $"{++currentSpeechToTextIndex}/{speechToTextArray.Length}";
        }
        SetPhraseCardAndSpeechToText();
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    private IEnumerator StartingAnimationCoroutine() {
        Vector2 sliderStartPosition = new Vector2(0f, -600f);
        Vector2 micStartPosition = new Vector2(0f, -550f);
        Vector2 fillStartPosition = new Vector2(0f, -600f);
        Vector2 borderStartPosition = new Vector2(0f, -600f);
        Vector2 debugStartPosition = new Vector2(0f, -400f);

        if (sliderRectTransform != null) sliderRectTransform.anchoredPosition = sliderStartPosition;
        if (micRectTransform != null) micRectTransform.anchoredPosition = micStartPosition;
        if (fillRectTransform != null) fillRectTransform.anchoredPosition = fillStartPosition;
        if (borderRectTransform != null) borderRectTransform.anchoredPosition = borderStartPosition;
        if (debugRectTransform != null) debugRectTransform.anchoredPosition = debugStartPosition;

        Vector2 sliderTargetPosition = new Vector2(0f, 50f);
        Vector2 micTargetPosition = new Vector2(0f, 25f);
        Vector2 fillTargetPosition = Vector2.zero;
        Vector2 borderTargetPosition = Vector2.zero;
        Vector2 debugTargetPosition = new Vector2(0f, 150f);

        if (sliderRectTransform != null) sliderRectTransform.DOAnchorPos(sliderTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(timeBetweenEachAnimation);
        if (fillRectTransform != null) fillRectTransform.DOAnchorPos(fillTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        if (borderRectTransform != null) borderRectTransform.DOAnchorPos(borderTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(timeBetweenEachAnimation);
        if (micRectTransform != null) micRectTransform.DOAnchorPos(micTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        if (debugRectTransform != null) debugRectTransform.DOAnchorPos(debugTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
    }

    private IEnumerator ClosingAnimationCoroutine() {
        Vector2 sliderStartPosition = new Vector2(0f, 50f);
        Vector2 micStartPosition = new Vector2(0f, 25f);
        Vector2 fillStartPosition = Vector2.zero;
        Vector2 borderStartPosition = Vector2.zero;
        Vector2 debugStartPosition = new Vector2(0f, 150f);

        if (sliderRectTransform != null) sliderRectTransform.anchoredPosition = sliderStartPosition;
        if (micRectTransform != null) micRectTransform.anchoredPosition = micStartPosition;
        if (fillRectTransform != null) fillRectTransform.anchoredPosition = fillStartPosition;
        if (borderRectTransform != null) borderRectTransform.anchoredPosition = borderStartPosition;
        if (debugRectTransform != null) debugRectTransform.anchoredPosition = debugStartPosition;

        Vector2 sliderTargetPosition = new Vector2(0f, -600f);
        Vector2 micTargetPosition = new Vector2(0f, -550f);
        Vector2 fillTargetPosition = new Vector2(0f, -600f);
        Vector2 borderTargetPosition = new Vector2(0f, -600f);
        Vector2 debugTargetPosition = new Vector2(0f, -400f);

        if (micRectTransform != null) micRectTransform.DOAnchorPos(micTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        if (debugRectTransform != null) debugRectTransform.DOAnchorPos(debugTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(timeBetweenEachAnimation);
        if (fillRectTransform != null) fillRectTransform.DOAnchorPos(fillTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        if (borderRectTransform != null) borderRectTransform.DOAnchorPos(borderTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(timeBetweenEachAnimation);
        if (sliderRectTransform != null) sliderRectTransform.DOAnchorPos(sliderTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
    }

    private void OnSpeechResult(string spokenText) {
        if (IsInvoking(SET_PHRASE_CARD_AND_SPEECH_TO_TEXT) || (continueButton != null && continueButton.gameObject.activeSelf)) return;

        string spoken = spokenText.ToLower().Trim();
        if (debugTMP != null) debugTMP.text = $"\"{spoken}\"";

        float maxSimilarity = 0f;
        if (currentSpeechToText != null && currentSpeechToText.speechDetectionText != null) {
            foreach (var text in currentSpeechToText.speechDetectionText) {
                float similarity = SimilarityPercent(text, spokenText);
                if (similarity > maxSimilarity) {
                    maxSimilarity = similarity;
                }
            }
        }

        if (progressBar != null) progressBar.value = maxSimilarity;
        if (sliderImage != null && progressBar != null) sliderImage.color = Color.Lerp(wrongColor, correctColor, progressBar.value);

        if (progressBar != null && progressBar.value > 0.75f) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                if (currentSpeechToText != null && currentSpeechToText.statementAudioClip != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(currentSpeechToText.statementAudioClip);
                }
            }
            if (progressCountTMP != null && speechToTextArray != null) {
                progressCountTMP.text = $"{++currentSpeechToTextIndex}/{speechToTextArray.Length}";
            }

            var toggleButton = FindObjectOfType<Masters_ToggleToTalkButton>();
            if (toggleButton != null) toggleButton.ResetButton();

            if (continueButton != null) {
                continueButton.gameObject.SetActive(true);
            } else {
                Invoke(SET_PHRASE_CARD_AND_SPEECH_TO_TEXT, timeToLoadNextSpeechToText);
            }
            return;
        }

        var btn = FindObjectOfType<Masters_ToggleToTalkButton>();
        if (btn != null) btn.ResetButton();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            if (currentSpeechToText != null && currentSpeechToText.statementAudioClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(currentSpeechToText.statementAudioClip);
            }
        }
    }

    private void SetPhraseCardAndSpeechToText() {
        var toggleButton = FindObjectOfType<Masters_ToggleToTalkButton>();
        if (toggleButton != null) toggleButton.ResetButton();

        if (progressBar != null) progressBar.value = 0f;
        if (sliderImage != null) sliderImage.color = defaultColor;

        if (currentPhraseCardGameObject != null) {
            Destroy(currentPhraseCardGameObject);
        }

        if (speechToTextArray == null || currentSpeechToTextIndex >= speechToTextArray.Length) {
            StartCoroutine(ClosingAnimationCoroutine());
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                NextButtonAnimation();
            }
            if (skipButton != null) skipButton.interactable = false;
            return;
        }

        if (debugTMP != null) debugTMP.text = "";

        currentSpeechToText = speechToTextArray[currentSpeechToTextIndex];
        if (phraseCardReferenceGameObject != null && phraseCardSpawnPointRectTransform != null) {
            currentPhraseCardGameObject = Instantiate(phraseCardReferenceGameObject);
            currentPhraseCardGameObject.transform.SetParent(phraseCardSpawnPointRectTransform, false);

            if (currentPhraseCardGameObject.TryGetComponent(out Masters_SpeakingPhraseCard speakingPhraseCard)) {
                speakingPhraseCard.SetText(currentSpeechToText.phraseCardText);

                Button phraseCardButton = currentPhraseCardGameObject.GetComponent<Button>();
                if (phraseCardButton != null) {
                    phraseCardButton.onClick.RemoveAllListeners();
                    phraseCardButton.onClick.AddListener(OnPhraseCardButtonClicked);
                }
            }

            currentPhraseCardGameObject.SetActive(true);
        }
    }

    private void OnPhraseCardButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            if (currentSpeechToText != null && currentSpeechToText.statementAudioClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(currentSpeechToText.statementAudioClip);
            }
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

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) return;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}

