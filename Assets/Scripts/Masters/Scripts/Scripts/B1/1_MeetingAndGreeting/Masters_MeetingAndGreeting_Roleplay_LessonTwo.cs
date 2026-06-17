using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_MeetingAndGreeting_Roleplay_LessonTwo : Masters_Lesson {


    [System.Serializable]
    public class RoleplayPhrase {

        public string[] detectionText;
        public string buttonText;
        public AudioClip phraseAudioClip;

    }


    [SerializeField]
    private RoleplayPhrase[] roleplayPhraseArray;
    [SerializeField]
    private RectTransform sliderRectTransform, micRectTransform, fillRectTransform, borderRectTransform, debugRectTransform;
    [SerializeField]
    private float animationSpeed, timeBetweenEachAnimation, timeBetweenRoleplay;
    [SerializeField]
    private Slider progressBar;
    [SerializeField]
    private Image sliderImage;
    [SerializeField]
    private TextMeshProUGUI debugTMP, micPromptTMP, progressCountTMP, npcCloudTMP;
    [SerializeField]
    private Button skipButton;
    [SerializeField]
    private Color wrongColor, correctColor, defaultColor;
    [SerializeField]
    private GameObject npcCloudGameObject, npcAndMicGameObject;


    private int currentRoleplayPhraseIndex;
    private RoleplayPhrase currentRoleplayPhrase;


    protected override void Awake() {
        base.Awake();

        skipButton.onClick.AddListener(OnSkipButtonClicked);
    }

    private void OnSkipButtonClicked() {
        progressCountTMP.text = $"{++currentRoleplayPhraseIndex}/19";
        micPromptTMP.gameObject.SetActive(false);
        LoadNextRoleplayPhrase();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    protected override void Start() {
        base.Start();

        npcCloudGameObject.SetActive(false);
        micPromptTMP.gameObject.SetActive(false);
        debugTMP.text = "";

        StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(LoadNextRoleplayPhrase));
        StartCoroutine(StartingAnimationCoroutine());
    }

    private void OnEnable() {
        CrossPlatformSpeechManager.OnResultStatic += OnSpeechResult;
    }

    private void OnDisable() {
        CrossPlatformSpeechManager.OnResultStatic -= OnSpeechResult;
    }

    private void OnSpeechResult(string spokenText) {
        string spoken = spokenText.ToLower().Trim();
        debugTMP.text = $"\"{spoken}\"";

        progressBar.value = SimilarityPercent(currentRoleplayPhrase.detectionText[0], spokenText);
        sliderImage.color = Color.Lerp(wrongColor, correctColor, progressBar.value);

        if (progressBar.value > 0.5f) {
            // Similarity greater than 50%
            FindObjectOfType<Masters_ToggleToTalkButton>()?.ResetButton();
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            Masters_AudioManager.Instance.PlayVoiceOver(currentRoleplayPhrase.phraseAudioClip);
            StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(LoadNextRoleplayPhrase));
            progressCountTMP.text = $"{++currentRoleplayPhraseIndex}/19";
            npcCloudTMP.text = currentRoleplayPhrase.buttonText;
            npcCloudGameObject.SetActive(true);

            if (currentRoleplayPhraseIndex == roleplayPhraseArray.Length) {
                // Over

                npcAndMicGameObject.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo);
                debugTMP.text = "";
                skipButton.interactable = false;

                nextButton.interactable = true;
                NextButtonAnimation();
                return;
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

    private void LoadNextRoleplayPhrase() {
        if(currentRoleplayPhraseIndex == roleplayPhraseArray.Length) {
            // Over
            skipButton.interactable = false;
            nextButton.interactable = true;
            NextButtonAnimation();
            npcAndMicGameObject.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo);
            return;
        }

        progressBar.value = 0f;
        sliderImage.color = defaultColor;
        npcCloudGameObject.SetActive(false);
        currentRoleplayPhrase = roleplayPhraseArray[currentRoleplayPhraseIndex];
        micPromptTMP.text = $"Talk into the mic: {currentRoleplayPhrase.buttonText}";
        micPromptTMP.gameObject.SetActive(true);
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

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) {
            Debug.Log($"Topic not set for {this.name}!");
            return;
        }
        Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }


}



//[SerializeField]
//private Masters_RoleplayGoodbyeCard[] roleplayGoodbyeCardArray;
//[SerializeField]
//private int numberOfSuccessfulDetectionsToComplete;


//private int numberOfSuccesfulDetections;


//protected override void Awake() {
//    base.Awake();

//    foreach (Masters_RoleplayGoodbyeCard roleplayGoodbyeCard in roleplayGoodbyeCardArray) {
//        roleplayGoodbyeCard.OnSuccessfulDetection += RoleplayGoodbyeCard_OnSuccessfulDetection;
//    }
//}

//private void RoleplayGoodbyeCard_OnSuccessfulDetection(object sender, System.EventArgs e) {
//    numberOfSuccesfulDetections++;

//    if (numberOfSuccesfulDetections == numberOfSuccessfulDetectionsToComplete) {
//        // Over
//        nextButton.interactable = true;
//    }
//}

//protected override void OnNextButtonClicked() {
//    if (topic == Masters_Topic.None) {
//        Debug.Log($"Topic not set for {this.name}!");
//        return;
//    }
//    Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
//    Masters_AudioManager.Instance.StopVoiceOver();
//    Masters_LevelManager.Instance.OnLessonComplete(topic);
//}


