using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_JumbledWords_Reading_LessonThree : Masters_Lesson {

    [System.Serializable]
    private class QuestionData {
        public string correctPhrase;
        public string[] wrongPhrases;
    }

    [SerializeField]
    private QuestionData[] questions;
    [SerializeField]
    private Button[] optionButtons;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;
    [SerializeField]
    private float timeBetweenEachAnimation, animationSpeed;
    [SerializeField]
    private TextMeshProUGUI expressionCountTMP;
    
    private int currentQuestionIndex = 0;
    private List<string> currentOptions = new List<string>();

    protected override void Awake() {
        base.Awake();

        foreach(Button optionButton in optionButtons) {
            optionButton.onClick.AddListener(() => {
                OnOptionButtonClicked(optionButton);
            });
        }
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

        for(int i = 0; i < optionButtons.Length; i++) {
            yield return new WaitForSeconds(timeBetweenEachAnimation);
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            optionButtons[i].transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        }

        if (questions != null && questions.Length > 0) {
            LoadQuestion(0);
        }

        yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
    }

    private void LoadQuestion(int index) {
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
                optionButtons[i].gameObject.SetActive(false);

                Masters_ListeningOptionButton cardBtn = optionButtons[i].GetComponent<Masters_ListeningOptionButton>();
                if (cardBtn != null) {
                    cardBtn.ResetButton();
                }

                TextMeshProUGUI optionText = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (optionText != null) {
                    optionText.text = currentOptions[i];
                }

                optionButtons[i].gameObject.SetActive(true);
            } else {
                optionButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnOptionButtonClicked(Button clickedButton) {
        TextMeshProUGUI optionText = clickedButton.GetComponentInChildren<TextMeshProUGUI>();
        if (optionText == null) return;

        QuestionData question = questions[currentQuestionIndex];

        if (optionText.text == question.correctPhrase) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            ProceedToNextQuestion();
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            clickedButton.transform.DOPunchPosition(Vector3.right * 10f, 0.3f, 10, 1f);
        }
    }

    private void ProceedToNextQuestion() {
        if (currentQuestionIndex + 1 < questions.Length) {
            LoadQuestion(currentQuestionIndex + 1);
        } else {
            Masters_AudioManager.Instance.StopVoiceOver();
            
            nextButton.interactable = true;
            NextButtonAnimation();
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
}
