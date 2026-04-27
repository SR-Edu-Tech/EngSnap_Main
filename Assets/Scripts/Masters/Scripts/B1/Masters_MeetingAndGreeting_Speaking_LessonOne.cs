using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class Masters_MeetingAndGreeting_Speaking_LessonOne : Masters_Lesson {


    private const string SET_PHRASE_CARD_AND_SPEECH_TO_TEXT = "SetPhraseCardAndSpeechToText";


    [System.Serializable]
    public class SpeechToText {

        public string phraseCardText;
        public string speechDetectionText;
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
        Debug.Log($"Spoken: {spoken}");

        if(spoken == currentSpeechToText.speechDetectionText) {
            // Correct
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            Invoke(SET_PHRASE_CARD_AND_SPEECH_TO_TEXT, timeToLoadNextSpeechToText);
        } else {
            // Wrong
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
    }

    private void SetPhraseCardAndSpeechToText() {
        if(currentPhraseCardGameObject != null) {
            Destroy(currentPhraseCardGameObject);
        }

        if(currentSpeechToTextIndex == speechToTextArray.Length) {
            nextButton.interactable = true;
            return;
        }

        currentSpeechToText = speechToTextArray[currentSpeechToTextIndex++];
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


}
