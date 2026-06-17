using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_LetMeQuestion_Roleplay_LessonTwo : Masters_Lesson {


    private const string LOAD_NEXT_ROLEPLAY = "LoadNextRoleplay";


    [System.Serializable]
    public class RoleplayDialogues {

        public string dialogueButtonText;
        public string dialogueDetectionText;
        public AudioClip dialogueAudioClip;

    }


    [SerializeField]
    private RoleplayDialogues[] npcRoleplayDialogueArray;
    [SerializeField]
    private RoleplayDialogues[] studentRoleplayDialogueArray;
    [SerializeField]
    private TextMeshProUGUI npcDialogueTMP;
    [SerializeField]
    private TextMeshProUGUI studentDialogueTMP;
    [SerializeField]
    private TextMeshProUGUI micPromptTMP;
    [SerializeField]
    private float timeBetweenRoleplay;
    [SerializeField]
    private TextMeshProUGUI debugTMP, progressCountTMP;
    [SerializeField]
    private Slider progressBar;
    [SerializeField]
    private Image sliderImage;
    [SerializeField]
    private Color wrongColor, correctColor, defaultColor;
    [SerializeField]
    private GameObject npcCloud, studentCloud, npcAndStudentGameObject;
    [SerializeField]
    private Button skipButton;
    [SerializeField]
    private float animationSpeed, timeBetweenEachAnimation;
    [SerializeField]
    private RectTransform sliderRectTransform, micRectTransform, fillRectTransform, borderRectTransform, debugRectTransform;


    private int dialogueIndex;
    private RoleplayDialogues currentNpcRoleplayDialogue;
    private RoleplayDialogues currentStudentRoleplayDialogue;


    protected override void Awake() {
        base.Awake();
        skipButton.onClick.AddListener(OnSkipButtonClicked);
    }

    private void OnSkipButtonClicked() {
        progressCountTMP.text = $"{++dialogueIndex}/5";
        LoadNextRoleplay();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    protected override void Start() {
        base.Start();

        npcCloud.SetActive(false);
        studentCloud.SetActive(false);
        debugTMP.text = "";
        micPromptTMP.gameObject.SetActive(false);
        StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(LoadNextRoleplay));
        StartCoroutine(StartingAnimationCoroutine());
    }

    private void OnEnable() {
        CrossPlatformSpeechManager.OnResultStatic += OnSpeechResult;
    }

    private void OnDisable() {
        CrossPlatformSpeechManager.OnResultStatic -= OnSpeechResult;
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

    private void OnSpeechResult(string spokenText) {
        string spoken = spokenText.ToLower().Trim();
        debugTMP.text = $"\"{spoken}\"";

        progressBar.value = SimilarityPercent(currentStudentRoleplayDialogue.dialogueDetectionText, spokenText);
        sliderImage.color = Color.Lerp(wrongColor, correctColor, progressBar.value);

        if (progressBar.value > 0.5f) {
            // Similarity greater than 50%
            FindObjectOfType<Masters_ToggleToTalkButton>()?.ResetButton();
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            progressCountTMP.text = $"{++dialogueIndex}/5";
            studentDialogueTMP.text = currentStudentRoleplayDialogue.dialogueButtonText;
            studentCloud.SetActive(true);

            Invoke(LOAD_NEXT_ROLEPLAY, timeBetweenRoleplay);
            return;
        }

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

    private void LoadNextRoleplay() {
        FindObjectOfType<Masters_ToggleToTalkButton>()?.ResetButton();
        debugTMP.text = "";

        if (dialogueIndex >= studentRoleplayDialogueArray.Length) {
            if (dialogueIndex == npcRoleplayDialogueArray.Length - 1) {
                currentNpcRoleplayDialogue = npcRoleplayDialogueArray[dialogueIndex++];
                npcCloud.SetActive(false);
                studentCloud.SetActive(false);
                npcDialogueTMP.text = currentNpcRoleplayDialogue.dialogueButtonText;
                sliderImage.color = defaultColor;
                npcCloud.SetActive(true);
                studentDialogueTMP.text = "";
                micPromptTMP.gameObject.SetActive(false);
                skipButton.interactable = false;
                Masters_AudioManager.Instance.PlayVoiceOver(currentNpcRoleplayDialogue.dialogueAudioClip);
                Invoke(LOAD_NEXT_ROLEPLAY, timeBetweenRoleplay);
                return;
            }
            // Over

            npcAndStudentGameObject.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo);
            debugTMP.text = "";
            skipButton.interactable = false;

            nextButton.interactable = true;
            NextButtonAnimation();
            return;
        }

        npcCloud.SetActive(false);
        studentCloud.SetActive(false);

        currentNpcRoleplayDialogue = npcRoleplayDialogueArray[dialogueIndex];
        currentStudentRoleplayDialogue = studentRoleplayDialogueArray[dialogueIndex];

        npcDialogueTMP.text = currentNpcRoleplayDialogue.dialogueButtonText;
        sliderImage.color = defaultColor;
        npcCloud.SetActive(true);
        studentDialogueTMP.text = "";
        micPromptTMP.gameObject.SetActive(false);
        micPromptTMP.text = $"Talk into the mic: {currentStudentRoleplayDialogue.dialogueButtonText}";
        micPromptTMP.gameObject.SetActive(true);
        Masters_AudioManager.Instance.PlayVoiceOver(currentNpcRoleplayDialogue.dialogueAudioClip);
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


