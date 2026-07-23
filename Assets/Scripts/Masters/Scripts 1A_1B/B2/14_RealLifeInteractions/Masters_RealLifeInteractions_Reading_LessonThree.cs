using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Reading Lesson 3 for Unit 14 Real Life Interactions.
/// Implements straight Multiple Choice Cloze mechanics without sentence move-up animations or wrong word click triggers.
/// </summary>
public class Masters_RealLifeInteractions_Reading_LessonThree : Masters_Lesson {

    [System.Serializable]
    public class QuestionData {
        public string sentenceText;
        public string targetWrongWord;
        public string correctWord;
        public string[] wrongWords;
        public string ruleText;
        public AudioClip sentenceAudioClip;
        public AudioClip ruleAudioClip;
    }

    [Header("Reading MCQ Settings")]
    [SerializeField] private QuestionData[] questions;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TextMeshProUGUI sentenceTMP;
    [SerializeField] private TextMeshProUGUI progressionTMP;
    [SerializeField] private GameObject ruleTMP;
    [SerializeField] private Masters_LessonSO nextLessonSO;
    [SerializeField] private float delayAfterCorrectAnswer = 2.0f;

    private int currentQuestionIndex = 0;

    protected override void Awake() {
        base.Awake();

        if (optionButtons != null) {
            foreach (Button optionButton in optionButtons) {
                if (optionButton != null) {
                    Button btnRef = optionButton;
                    btnRef.onClick.AddListener(() => OnOptionButtonClicked(btnRef));
                }
            }
        }

        if (nextButton != null) {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();
        StartCoroutine(StartLessonCoroutine());
    }

    private IEnumerator StartLessonCoroutine() {
        if (sentenceTMP != null) sentenceTMP.text = "";
        if (ruleTMP != null) ruleTMP.SetActive(false);
        if (optionButtons != null) {
            foreach (var btn in optionButtons) {
                if (btn != null) btn.gameObject.SetActive(false);
            }
        }

        // Wait brief moment for intro audio if any
        yield return new WaitForSeconds(4.0f);

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
            sentenceTMP.enableWordWrapping = true;
            sentenceTMP.fontSize = 38;
            sentenceTMP.text = question.sentenceText.Replace("\\n", "\n");
        }

        if (ruleTMP != null) {
            ruleTMP.SetActive(false);
        }

        List<string> currentOptions = new List<string>();
        currentOptions.Add(question.correctWord);
        if (question.wrongWords != null) {
            currentOptions.AddRange(question.wrongWords);
        }

        // Shuffling options evenly across buttons A, B, C, D
        currentOptions = currentOptions.OrderBy(x => Guid.NewGuid()).ToList();

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    if (i < currentOptions.Count) {
                        optionButtons[i].gameObject.SetActive(true);
                        optionButtons[i].interactable = true;
                        optionButtons[i].transform.localScale = Vector3.one;

                        TextMeshProUGUI optionText = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (optionText != null) {
                            optionText.text = currentOptions[i];
                        }
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

        if (optionText.text == question.correctWord) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

            if (optionButtons != null) {
                foreach (var btn in optionButtons) {
                    if (btn != null) btn.interactable = false;
                }
            }

            if (sentenceTMP != null && question.sentenceText.Contains("___")) {
                sentenceTMP.text = question.sentenceText.Replace("\\n", "\n").Replace("___", $"<color=#38BF43>{question.correctWord}</color>");
            }

            float waitTime = delayAfterCorrectAnswer;
            if (question.sentenceAudioClip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(question.sentenceAudioClip);
                waitTime = question.sentenceAudioClip.length + 0.5f;
            }

            StartCoroutine(WaitAndLoadNextQuestion(waitTime));
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            clickedButton.transform.DOShakePosition(0.3f, 5f);
        }
    }

    private IEnumerator WaitAndLoadNextQuestion(float delay) {
        yield return new WaitForSeconds(delay);

        if (currentQuestionIndex + 1 < questions.Length) {
            LoadQuestion(currentQuestionIndex + 1);
        } else {
            if (optionButtons != null) {
                foreach (var btn in optionButtons) {
                    if (btn != null) btn.gameObject.SetActive(false);
                }
            }
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }
            NextButtonAnimation();
        }
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        if (nextLessonSO != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            if (topic != Masters_Topic.None && Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
