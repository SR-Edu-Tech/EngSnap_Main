using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_JumbledWords_Listening_LessonOne : Masters_Lesson {


    [System.Serializable]
    private class QuestionData {
        public AudioClip normalAudioClip;
        public AudioClip slowedAudioClip;
        public string correctPhrase;
        public string[] wrongPhrases;
    }

    [SerializeField]
    private QuestionData[] questions;
    [SerializeField]
    private Button[] optionButtons;
    [SerializeField]
    private GameObject speakerGameObject;
    [SerializeField]
    private float timeBetweenAudioInPlayAll = 2f;
    [SerializeField]
    private Toggle slowToggle;
    [SerializeField]
    private Toggle repeatToggle;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;
    [SerializeField]
    private float timeBetweenEachAnimation, animationSpeed;
    [SerializeField]
    private TextMeshProUGUI expressionCountTMP;
    
    private int currentQuestionIndex = 0;
    private bool isSlowed;
    private bool isRepeatOn;
    private Coroutine audioCoroutine;
    private List<string> currentOptions = new List<string>();


    protected override void Awake() {
        base.Awake();

        foreach(Button optionButton in optionButtons) {
            optionButton.onClick.AddListener(() => {
                OnOptionButtonClicked(optionButton);
            });
        }

        slowToggle.onValueChanged.AddListener(OnSlowToggle);
        repeatToggle.onValueChanged.AddListener(OnRepeatToggle);
    }

    private void OnEnable() {
        currentQuestionIndex = 0;
        StartCoroutine(PopUpAnimationAndLoadFirstQuestion());
    }

    protected override void Start() {
        base.Start();
    }

    private IEnumerator PopUpAnimationAndLoadFirstQuestion() {
        foreach(Button optionButton in optionButtons) {
            optionButton.transform.localScale = Vector3.zero;
        }
        slowToggle.transform.localScale = Vector3.zero;
        repeatToggle.transform.localScale = Vector3.zero;

        for(int i = 0; i < optionButtons.Length; i++) {
            yield return new WaitForSeconds(timeBetweenEachAnimation);
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            optionButtons[i].transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        }
        yield return new WaitForSeconds(timeBetweenEachAnimation);
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
        slowToggle.transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        repeatToggle.transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);

        if (questions != null && questions.Length > 0) {
            // Load the UI for the first question, but don't play its audio yet
            LoadQuestion(0, false);
        }

        // Wait for the base Masters_Lesson's narrator audio (triggered in Start) to finish
        yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);

        // Now play the question audio
        PlayCurrentQuestionAudio();
    }

    private void LoadQuestion(int index, bool playAudio = true) {
        currentQuestionIndex = index;
        expressionCountTMP.text = $"{currentQuestionIndex + 1}/{questions.Length}";

        QuestionData question = questions[currentQuestionIndex];

        currentOptions.Clear();
        currentOptions.Add(question.correctPhrase);
        if (question.wrongPhrases != null) {
            foreach (string wrongPhrase in question.wrongPhrases) {
                currentOptions.Add(wrongPhrase);
            }
        }
        
        // Shuffle options
        currentOptions = currentOptions.OrderBy(x => Guid.NewGuid()).ToList();

        for (int i = 0; i < optionButtons.Length; i++) {
            if (i < currentOptions.Count) {
                // Temporarily disable to stop any old typewriter animations from previous questions
                optionButtons[i].gameObject.SetActive(false);

                Masters_ListeningOptionButton cardBtn = optionButtons[i].GetComponent<Masters_ListeningOptionButton>();
                if (cardBtn != null) {
                    cardBtn.ResetButton();
                }

                TextMeshProUGUI optionText = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (optionText != null) {
                    optionText.text = currentOptions[i];
                }

                // Re-enabling the button triggers the typewriter animation via OnEnable
                optionButtons[i].gameObject.SetActive(true);
            } else {
                optionButtons[i].gameObject.SetActive(false);
            }
        }

        if (playAudio) {
            PlayCurrentQuestionAudio();
        }
    }

    private void PlayCurrentQuestionAudio() {
        if(audioCoroutine != null) {
            StopCoroutine(audioCoroutine);
        }

        if (questions == null || currentQuestionIndex >= questions.Length) return;

        QuestionData question = questions[currentQuestionIndex];
        AudioClip audioClip = isSlowed ? question.slowedAudioClip : question.normalAudioClip;
        
        if (speakerGameObject != null) {
            speakerGameObject.SetActive(true);
        }

        if (isRepeatOn) {
            audioCoroutine = StartCoroutine(PlayInRepeatCoroutine(audioClip));
        } else {
            Masters_AudioManager.Instance.PlayVoiceOver(audioClip);
            StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
                if (speakerGameObject != null) {
                    speakerGameObject.SetActive(false);
                }
            }));
        }
    }

    private void OnOptionButtonClicked(Button clickedButton) {
        TextMeshProUGUI optionText = clickedButton.GetComponentInChildren<TextMeshProUGUI>();
        if (optionText == null) return;

        QuestionData question = questions[currentQuestionIndex];

        if (optionText.text == question.correctPhrase) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

            if (currentQuestionIndex + 1 < questions.Length) {
                LoadQuestion(currentQuestionIndex + 1);
            } else {
                if (speakerGameObject != null) {
                    speakerGameObject.SetActive(false);
                }
                Masters_AudioManager.Instance.StopVoiceOver();
                if(audioCoroutine != null) {
                    StopCoroutine(audioCoroutine);
                }
                
                nextButton.interactable = true;
                NextButtonAnimation();
            }
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            clickedButton.transform.DOShakePosition(0.3f, 5f);
        }
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

    private void OnSlowToggle(bool value) {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        slowToggle.DOKill(true);
        slowToggle.transform.localScale = Vector3.one;

        slowToggle.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);

        isSlowed = value;
        
        if (questions != null && questions.Length > 0 && currentQuestionIndex < questions.Length) {
            PlayCurrentQuestionAudio();
        }
    }

    private void OnRepeatToggle(bool value) {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        repeatToggle.DOKill(true);
        repeatToggle.transform.localScale = Vector3.one;

        repeatToggle.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);

        isRepeatOn = value;

        if (value == false) {
            Masters_AudioManager.Instance.StopVoiceOver();

            if(audioCoroutine != null) {
                StopCoroutine(audioCoroutine);
            }
            
            if (speakerGameObject != null) {
                speakerGameObject.SetActive(false);
            }
        } else {
            if (questions != null && questions.Length > 0 && currentQuestionIndex < questions.Length) {
                PlayCurrentQuestionAudio();
            }
        }
    }

    private IEnumerator PlayInRepeatCoroutine(AudioClip audioClip) {
        while (true) {
            Masters_AudioManager.Instance.PlayVoiceOver(audioClip);
            if (speakerGameObject != null) {
                speakerGameObject.SetActive(true);
            }
            yield return new WaitForSeconds(audioClip.length);
            if (speakerGameObject != null) {
                speakerGameObject.SetActive(false);
            }
            yield return new WaitForSeconds(timeBetweenAudioInPlayAll);
        }
    }


}
