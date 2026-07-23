using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Game Manager for Unit 1: Polished Communication - Quiz Lesson One (Q01).
/// Standalone Book 2A controller written from scratch.
/// Supports flexible option count (e.g. 2 options for Formal/Informal or True/False, and 4 options for standard MCQ),
/// with complete voiceover integration and UI animations.
/// </summary>
public class Masters_PolishedCommunication_Quiz_LessonOne : Masters_Lesson {

    private const string SET_QUIZ = "SetQuiz";

    [System.Serializable]
    public class Quiz {
        [Tooltip("The question text displayed at the top.")]
        public string question;
        [Tooltip("Voiceover clip read when the question pops up.")]
        public AudioClip questionAudioClip;
        [Tooltip("The choice options displayed on the buttons (e.g., 2 options or 4 options).")]
        public string[] options;
        [Tooltip("The index (0-based) of the correct option.")]
        public int correctOptionIndex;
    }

    [Header("Quiz Data")]
    [SerializeField] protected Quiz[] quizArray;
    
    [Header("UI Elements")]
    [SerializeField] protected TextMeshProUGUI questionTMP;
    [SerializeField] protected Masters_QuizButton[] quizButtonArray;
    [SerializeField] protected Button confirmButton;
    [SerializeField] protected Button skipButton;
    [SerializeField] protected TextMeshProUGUI quizCountTMP;
    
    [Header("Colors")]
    [SerializeField] protected Color selectedColor = new Color(0.9f, 0.8f, 0.2f, 1f);
    [SerializeField] protected Color defaultColor = Color.white;
    [SerializeField] protected Color correctColor = new Color(0.2f, 0.8f, 0.3f, 1f);
    [SerializeField] protected Color incorrectColor = new Color(0.9f, 0.3f, 0.3f, 1f);
    
    [Header("Timing & Animation")]
    [SerializeField] protected float timeBetweenEachQuizQuestion = 1.5f;
    [SerializeField] protected float timeBetweenEachAnimation = 0.1f;
    [SerializeField] protected float animationSpeed = 0.5f;

    protected int currentQuizIndex;
    protected Quiz currentQuiz;
    protected Masters_QuizButton currentlyPressedQuizButton;
    protected bool canClickCheckButton;
    protected bool canClickOptionButton;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Quiz;

        if (confirmButton != null) {
            confirmButton.onClick.AddListener(OnConfirmButtonClicked);
        }
        if (skipButton != null) {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }
    }

    protected override void Start() {
        base.Start();

        SetQuiz();

        // Bind quiz buttons
        if (quizButtonArray != null) {
            for (int i = 0; i < quizButtonArray.Length; i++) {
                int buttonIndex = i;
                if (quizButtonArray[i] != null && quizButtonArray[i].GetButton() != null) {
                    quizButtonArray[i].GetButton().onClick.AddListener(() => {
                        OnQuizButtonClicked(quizButtonArray[buttonIndex], buttonIndex);
                    });
                }
            }
        }
    }

    protected virtual void OnSkipButtonClicked() {
        if (quizCountTMP != null) {
            quizCountTMP.text = $"{++currentQuizIndex}/{quizArray.Length}";
        } else {
            currentQuizIndex++;
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
        SetQuiz();
    }

    protected virtual void OnConfirmButtonClicked() {
        if (!canClickCheckButton || currentlyPressedQuizButton == null) {
            return;
        }

        if (currentQuiz != null && currentQuiz.correctOptionIndex == currentlyPressedQuizButton.GetButtonIndex()) {
            // Correct Answer
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
            if (currentlyPressedQuizButton.GetButtonImage() != null) {
                currentlyPressedQuizButton.GetButtonImage().color = correctColor;
            }
            
            if (quizCountTMP != null) {
                quizCountTMP.text = $"{++currentQuizIndex}/{quizArray.Length}";
            } else {
                currentQuizIndex++;
            }

            canClickCheckButton = false;
            canClickOptionButton = false;

            Invoke(SET_QUIZ, timeBetweenEachQuizQuestion);
        } else {
            // Incorrect Answer
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            if (currentlyPressedQuizButton.GetButton() != null) {
                currentlyPressedQuizButton.GetButton().interactable = false;
            }
            if (currentlyPressedQuizButton.GetButtonImage() != null) {
                currentlyPressedQuizButton.GetButtonImage().color = incorrectColor;
            }
            currentlyPressedQuizButton = null;

            canClickOptionButton = true;
            canClickCheckButton = false;
        }
    }

    protected virtual void OnQuizButtonClicked(Masters_QuizButton quizButton, int buttonIndex) {
        if (!canClickOptionButton) {
            return;
        }

        // Reset previous selection
        if (currentlyPressedQuizButton != null && currentlyPressedQuizButton.GetButtonImage() != null) {
            currentlyPressedQuizButton.GetButtonImage().color = defaultColor;
        }

        canClickCheckButton = true;

        if (quizButton != null) {
            quizButton.SetButtonIndex(buttonIndex);
            currentlyPressedQuizButton = quizButton;

            if (currentlyPressedQuizButton.GetButtonImage() != null) {
                currentlyPressedQuizButton.GetButtonImage().color = selectedColor;
            }
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
        }
    }

    protected virtual void SetQuiz() {
        if (quizArray == null || currentQuizIndex >= quizArray.Length) {
            // Lesson Over
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            return;
        }

        // Reset Buttons
        if (quizButtonArray != null) {
            foreach (Masters_QuizButton quizButton in quizButtonArray) {
                if (quizButton != null) {
                    if (quizButton.GetButton() != null) {
                        quizButton.GetButton().interactable = true;
                    }
                    if (quizButton.GetButtonImage() != null) {
                        quizButton.GetButtonImage().color = defaultColor;
                    }
                }
            }
        }

        canClickCheckButton = false;
        canClickOptionButton = true;
        currentQuiz = quizArray[currentQuizIndex];

        StartCoroutine(AnimationCoroutine());
    }

    protected virtual IEnumerator AnimationCoroutine() {
        // Hide all buttons initially
        if (quizButtonArray != null) {
            foreach (Masters_QuizButton quizButton in quizButtonArray) {
                if (quizButton != null) {
                    quizButton.gameObject.SetActive(false);
                    RectTransform quizButtonRectTransform = quizButton.GetComponent<RectTransform>();
                    if (quizButtonRectTransform != null) quizButtonRectTransform.localScale = Vector3.zero;
                }
            }
        }

        if (questionTMP != null) questionTMP.gameObject.SetActive(false);

        yield return new WaitForSeconds(timeBetweenEachAnimation);
        
        if (questionTMP != null && currentQuiz != null) {
            questionTMP.text = currentQuiz.question;
            questionTMP.gameObject.SetActive(true);
            Masters_TextTypeWriter questionTextTypeWriter = questionTMP.GetComponent<Masters_TextTypeWriter>();
            if (questionTextTypeWriter != null) {
                questionTextTypeWriter.TriggerAnimation(questionTMP.text.Length);
            }
        }
        // Reveal buttons one by one (only up to currentQuiz.options.Length)
        if (quizButtonArray != null && currentQuiz != null && currentQuiz.options != null) {
            for (int i = 0; i < currentQuiz.options.Length; i++) {
                if (i >= quizButtonArray.Length || quizButtonArray[i] == null) break;
                
                quizButtonArray[i].gameObject.SetActive(true);
                yield return new WaitForSeconds(timeBetweenEachAnimation);
                
                quizButtonArray[i].SetText(currentQuiz.options[i]);
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
                }
                
                RectTransform quizButtonRectTransform = quizButtonArray[i].GetComponent<RectTransform>();
                if (quizButtonRectTransform != null) {
                    quizButtonRectTransform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutExpo);
                }
            }

            // Ensure extra buttons beyond options.Length stay completely hidden & disabled
            for (int i = currentQuiz.options.Length; i < quizButtonArray.Length; i++) {
                if (quizButtonArray[i] != null) {
                    quizButtonArray[i].gameObject.SetActive(false);
                }
            }
        }
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Quiz;
        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
