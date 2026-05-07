using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class Masters_MeetingAndGreeting_Speaking_LessonOne : Masters_Lesson {


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
    }

    private void OnSpeechResult(string spokenText) {
        string spoken = spokenText.ToLower().Trim();
        debugTMP.text = $"\"{spoken}\"";

        progressBar.value = SimilarityPercent(currentSpeechToText.speechDetectionText[0], spokenText);
        sliderImage.color = Color.Lerp(wrongColor, correctColor, progressBar.value);

        foreach (string speechDetection in currentSpeechToText.speechDetectionText){
            if (spoken == speechDetection) {
                // Correct
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                progressCountTMP.text = $"{++currentSpeechToTextIndex}/3";
                Invoke(SET_PHRASE_CARD_AND_SPEECH_TO_TEXT, timeToLoadNextSpeechToText);
                return;
            }
        }

        // Wrong
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
    }

    private void SetPhraseCardAndSpeechToText() {
        progressBar.value = 0f;

        if (currentPhraseCardGameObject != null) {
            Destroy(currentPhraseCardGameObject);
        }

        if(currentSpeechToTextIndex == speechToTextArray.Length) {
            nextButton.interactable = true;
            return;
        }

        debugTMP.text = "";

        currentSpeechToText = speechToTextArray[currentSpeechToTextIndex];
        currentPhraseCardGameObject = Instantiate(phraseCardReferenceGameObject);
        currentPhraseCardGameObject.transform.SetParent(phraseCardSpawnPointRectTransform, false);
        currentPhraseCardGameObject.SetActive(true);

        if(currentPhraseCardGameObject.TryGetComponent(out Masters_SpeakingPhraseCard speakingPhraseCard)) {
            speakingPhraseCard.SetText(currentSpeechToText.phraseCardText);

            Button phraseCardButton = currentPhraseCardGameObject.GetComponent<Button>();
            phraseCardButton.onClick.AddListener(OnPhraseCardButtonClicked);
        }
    }

    private void OnPhraseCardButtonClicked() {
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
