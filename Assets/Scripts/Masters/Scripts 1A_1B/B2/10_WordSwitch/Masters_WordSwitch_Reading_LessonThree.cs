using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_WordSwitch_Reading_LessonThree : Masters_Lesson {

    [System.Serializable]
    public class QuestionData {
        public string sentenceText;
        public string correctSynonym;
        public string[] wrongSynonyms;
    }

    [Header("Reading MCQ Settings")]
    [SerializeField] private QuestionData[] questions;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TextMeshProUGUI sentenceTMP;
    [SerializeField] private TextMeshProUGUI progressionTMP;
    [SerializeField] private Masters_LessonSO nextLessonSO;
    [SerializeField] private float timeBetweenEachAnimation = 0.1f;
    [SerializeField] private float animationSpeed = 0.4f;

    private int currentQuestionIndex = 0;
    private List<string> currentOptions = new List<string>();

    protected override void Awake() {
        base.Awake();

        if (optionButtons != null) {
            foreach (Button optionButton in optionButtons) {
                if (optionButton != null) {
                    optionButton.onClick.AddListener(() => {
                        OnOptionButtonClicked(optionButton);
                    });
                }
            }
        }

        if (nextButton != null) {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }
    }

    private void OnEnable() {
        currentQuestionIndex = 0;
        StartCoroutine(PopUpAnimationAndLoadFirstQuestion());
    }

    private IEnumerator PopUpAnimationAndLoadFirstQuestion() {
        if (optionButtons != null) {
            foreach (Button optionButton in optionButtons) {
                if (optionButton != null) optionButton.transform.localScale = Vector3.zero;
            }

            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    yield return new WaitForSeconds(timeBetweenEachAnimation);
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
                    optionButtons[i].transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
                }
            }
        }

        if (questions != null && questions.Length > 0) {
            LoadQuestion(0);
        }
    }

    private void LoadQuestion(int index) {
        currentQuestionIndex = index;
        if (progressionTMP != null && questions != null) {
            progressionTMP.text = $"{currentQuestionIndex + 1}/{questions.Length}";
        }

        if (questions == null || currentQuestionIndex >= questions.Length) return;

        QuestionData question = questions[currentQuestionIndex];

        if (sentenceTMP != null) {
            sentenceTMP.text = question.sentenceText;
        }

        currentOptions.Clear();
        currentOptions.Add(question.correctSynonym);
        if (question.wrongSynonyms != null) {
            currentOptions.AddRange(question.wrongSynonyms);
        }

        // Shuffle options evenly across buttons
        currentOptions = currentOptions.OrderBy(x => Guid.NewGuid()).ToList();

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    if (i < currentOptions.Count) {
                        optionButtons[i].gameObject.SetActive(false);

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
        }
    }

    private void OnOptionButtonClicked(Button clickedButton) {
        TextMeshProUGUI optionText = clickedButton.GetComponentInChildren<TextMeshProUGUI>();
        if (optionText == null || questions == null || currentQuestionIndex >= questions.Length) return;

        QuestionData question = questions[currentQuestionIndex];

        if (optionText.text == question.correctSynonym) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

            if (currentQuestionIndex + 1 < questions.Length) {
                LoadQuestion(currentQuestionIndex + 1);
            } else {
                if (nextButton != null) {
                    nextButton.gameObject.SetActive(true);
                    nextButton.interactable = true;
                }
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
}
