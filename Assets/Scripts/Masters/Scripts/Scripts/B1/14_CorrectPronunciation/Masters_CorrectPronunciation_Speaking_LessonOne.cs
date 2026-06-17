using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_CorrectPronunciation_Speaking_LessonOne : Masters_Lesson {


    private const string SET_PHRASE_CARD_AND_SPEECH_TO_TEXT = "SetPhraseCardAndSpeechToText";


    [System.Serializable]
    public class SpeechToText {

        public string phraseCardText;
        public string[] speechDetectionText;
        public AudioClip statementAudioClip;

    }


    [SerializeField]
    private SpeechToText[] speechToTextArray;
    [SerializeField]
    private GameObject phraseCardReferenceGameObject;
    [SerializeField]
    private RectTransform phraseCardSpawnPointRectTransform;
    [SerializeField]
    private float timeToLoadNextSpeechToText;
    [SerializeField]
    private TextMeshProUGUI debugTMP;
    [SerializeField]
    private TextMeshProUGUI progressCountTMP;
    [SerializeField]
    private Slider progressBar;
    [SerializeField]
    private Color correctColor, wrongColor, defaultColor;
    [SerializeField]
    private Image sliderImage;
    [SerializeField]
    private RectTransform sliderRectTransform, micRectTransform, fillRectTransform, borderRectTransform, debugRectTransform;
    [SerializeField]
    private float animationSpeed, timeBetweenEachAnimation;
    [SerializeField]
    private Button skipButton;
    [SerializeField]
    private Button continueButton;


    private SpeechToText currentSpeechToText;
    private int currentSpeechToTextIndex;
    private GameObject currentPhraseCardGameObject;


    private void OnEnable() {
        CrossPlatformSpeechManager.OnResultStatic += OnSpeechResult;
    }

    private void OnDisable() {
        CrossPlatformSpeechManager.OnResultStatic -= OnSpeechResult;
    }

    protected override void Awake() {
        base.Awake();

        SetPhraseCardAndSpeechToText();
        skipButton.onClick.AddListener(OnSkipButtonClicked);
        
        if (continueButton != null) {
            continueButton.onClick.AddListener(OnContinueButtonClicked);
            continueButton.gameObject.SetActive(false);
        }
    }

    private void OnContinueButtonClicked() {
        if (continueButton != null) {
            continueButton.gameObject.SetActive(false);
        }
        SetPhraseCardAndSpeechToText();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    protected override void Start() {
        base.Start();

        StartCoroutine(StartingAnimationCoroutine());
    }

    private void OnSkipButtonClicked() {
        progressCountTMP.text = $"{++currentSpeechToTextIndex}/{speechToTextArray.Length}";
        SetPhraseCardAndSpeechToText();
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
        fillRectTransform.anchoredPosition = fillStartPosition;
        borderRectTransform.anchoredPosition = borderStartPosition;
        debugRectTransform.anchoredPosition = debugStartPosition;

        Vector2 sliderTargetPosition = new Vector2(0f, 50f);
        Vector2 micTargetPosition = new Vector2(0f, 25f);
        Vector2 fillTargetPosition = Vector2.zero;
        Vector2 borderTargetPosition = Vector2.zero;
        Vector2 debugTargetPosition = new Vector2(0f, 150f);

        sliderRectTransform.DOAnchorPos(sliderTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(timeBetweenEachAnimation);
        fillRectTransform.DOAnchorPos(fillTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        borderRectTransform.DOAnchorPos(borderTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(timeBetweenEachAnimation);
        micRectTransform.DOAnchorPos(micTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        debugRectTransform.DOAnchorPos(debugTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
    }

    private IEnumerator ClosingAnimationCoroutine() {
        Vector2 sliderStartPosition = new Vector2(0f, 50f);
        Vector2 micStartPosition = new Vector2(0f, 25f);
        Vector2 fillStartPosition = Vector2.zero;
        Vector2 borderStartPosition = Vector2.zero;
        Vector2 debugStartPosition = new Vector2(0f, 150f);

        sliderRectTransform.anchoredPosition = sliderStartPosition;
        micRectTransform.anchoredPosition = micStartPosition;
        fillRectTransform.anchoredPosition = fillStartPosition;
        borderRectTransform.anchoredPosition = borderStartPosition;
        debugRectTransform.anchoredPosition = debugStartPosition;

        Vector2 sliderTargetPosition = new Vector2(0f, -600f);
        Vector2 micTargetPosition = new Vector2(0f, -550f);
        Vector2 fillTargetPosition = new Vector2(0f, -600f);
        Vector2 borderTargetPosition = new Vector2(0f, -600f);
        Vector2 debugTargetPosition = new Vector2(0f, -400f);

        micRectTransform.DOAnchorPos(micTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        debugRectTransform.DOAnchorPos(debugTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(timeBetweenEachAnimation);
        fillRectTransform.DOAnchorPos(fillTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        borderRectTransform.DOAnchorPos(borderTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(timeBetweenEachAnimation);
        sliderRectTransform.DOAnchorPos(sliderTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
    }

    private void OnSpeechResult(string spokenText) {
        if (IsInvoking(SET_PHRASE_CARD_AND_SPEECH_TO_TEXT) || (continueButton != null && continueButton.gameObject.activeSelf)) return;

        string spoken = spokenText.ToLower().Trim();
        debugTMP.text = $"\"{spoken}\"";

        float maxSimilarity = 0f;
        if (currentSpeechToText.speechDetectionText != null) {
            foreach (var text in currentSpeechToText.speechDetectionText) {
                float similarity = SimilarityPercent(text, spokenText);
                if (similarity > maxSimilarity) {
                    maxSimilarity = similarity;
                }
            }
        }

        progressBar.value = maxSimilarity;
        sliderImage.color = Color.Lerp(wrongColor, correctColor, progressBar.value);

        if (progressBar.value > 0.75) {
            // Similarity greater than 75%
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            progressCountTMP.text = $"{++currentSpeechToTextIndex}/{speechToTextArray.Length}";
            
            FindObjectOfType<Masters_ToggleToTalkButton>()?.ResetButton();

            if (continueButton != null) {
                continueButton.gameObject.SetActive(true);
            } else {
                Invoke(SET_PHRASE_CARD_AND_SPEECH_TO_TEXT, timeToLoadNextSpeechToText);
            }
            return;

        }

        //if (spoken == currentStudentRoleplayDialogue.dialogueDetectionText) {
        //    // Correct
        //    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        //    progressCountTMP.text = $"{++dialogueIndex}/5";
        //    studentDialogueTMP.text = currentStudentRoleplayDialogue.dialogueButtonText;
        //    studentCloud.SetActive(true);

        //    Invoke(LOAD_NEXT_ROLEPLAY, timeBetweenRoleplay);
        //    return;

        //}

        // Wrong
        FindObjectOfType<Masters_ToggleToTalkButton>()?.ResetButton();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
    }

    private void SetPhraseCardAndSpeechToText() {
        FindObjectOfType<Masters_ToggleToTalkButton>()?.ResetButton();

        progressBar.value = 0f;
        sliderImage.color = defaultColor;

        if (currentPhraseCardGameObject != null) {
            Destroy(currentPhraseCardGameObject);
        }

        if (currentSpeechToTextIndex == speechToTextArray.Length) {
            // Over
            StartCoroutine(ClosingAnimationCoroutine());
            nextButton.interactable = true;
            skipButton.interactable = false;
            NextButtonAnimation();
            return;
        }

        debugTMP.text = "";

        currentSpeechToText = speechToTextArray[currentSpeechToTextIndex];
        currentPhraseCardGameObject = Instantiate(phraseCardReferenceGameObject);
        currentPhraseCardGameObject.transform.SetParent(phraseCardSpawnPointRectTransform, false);

        if (currentPhraseCardGameObject.TryGetComponent(out Masters_SpeakingPhraseCard speakingPhraseCard)) {
            speakingPhraseCard.SetText(currentSpeechToText.phraseCardText);

            Button phraseCardButton = currentPhraseCardGameObject.GetComponent<Button>();
            phraseCardButton.onClick.AddListener(OnPhraseCardButtonClicked);
        }

        currentPhraseCardGameObject.SetActive(true);
    }

    private void OnPhraseCardButtonClicked() {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        Masters_AudioManager.Instance.PlayVoiceOver(currentSpeechToText.statementAudioClip);
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) {
            Debug.Log($"Topic not set for {this.name}!");
            return;
        }
        Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.OnLessonComplete(topic);
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

