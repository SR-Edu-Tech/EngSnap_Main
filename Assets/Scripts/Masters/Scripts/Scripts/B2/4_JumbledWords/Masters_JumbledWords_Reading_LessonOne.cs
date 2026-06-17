using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_JumbledWords_Reading_LessonOne : Masters_Lesson {

    private const string SET_QUIZ = "SetQuiz";

    [System.Serializable]
    public class Quiz {
        public string question;
        public string[] options;
        public int correctOptionIndex;
        public AudioClip correctAudioClip;
    }

    [SerializeField]
    private Quiz[] quizArray;
    [SerializeField]
    private TextMeshProUGUI questionTMP;
    [SerializeField]
    private Masters_QuizButton[] quizButtonArray;
    
    [SerializeField]
    private Color defaultColor;
    [SerializeField]
    private Color correctColor;
    [SerializeField]
    private Color incorrectColor;
    [SerializeField]
    private float timeBetweenEachQuizQuestion;
    [SerializeField]
    private TextMeshProUGUI quizCountTMP;
    [SerializeField]
    private float timeBetweenEachAnimation, animationSpeed;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;

    private int currentQuizIndex;
    private Quiz currentQuiz;
    private bool canClickOptionButton;

    protected override void Awake() {
        base.Awake();
    }

    protected override void Start() {
        base.Start();

        SetQuiz();

        for (int i = 0; i < quizButtonArray.Length; i++) {
            int buttonIndex = i;

            quizButtonArray[i].GetButton().onClick.AddListener(() => {
                OnQuizButtonClicked(quizButtonArray[buttonIndex], buttonIndex);
            });
        }
    }

    private void OnQuizButtonClicked(Masters_QuizButton quizButton, int buttonIndex) {
        if (!canClickOptionButton) {
            return;
        }

        if (currentQuiz.correctOptionIndex == buttonIndex) {
            // Correct
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            quizButton.GetButtonImage().color = correctColor;
            quizCountTMP.text = $"{currentQuizIndex + 1}/{quizArray.Length}";
            
            currentQuizIndex++;

            canClickOptionButton = false;

            if (currentQuiz.correctAudioClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(currentQuiz.correctAudioClip);
                StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(SetQuiz));
            } else {
                Invoke(SET_QUIZ, timeBetweenEachQuizQuestion);
            }
        } else {
            // Incorrect
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            quizButton.GetButton().interactable = false;
            quizButton.GetButtonImage().color = incorrectColor;
            
            // Shake effect for incorrect choice
            quizButton.transform.DOPunchPosition(Vector3.right * 10f, 0.3f, 10, 1f);
        }
    }

    private void SetQuiz() {
        if (currentQuizIndex == quizArray.Length) {
            // Over
            nextButton.interactable = true;
            NextButtonAnimation();
            return;
        }

        foreach (Masters_QuizButton quizButton in quizButtonArray) {
            quizButton.GetButton().interactable = true;
            quizButton.GetButtonImage().color = defaultColor;
        }

        canClickOptionButton = true;
        currentQuiz = quizArray[currentQuizIndex];

        StartCoroutine(AnimationCoroutine());
    }

    private IEnumerator AnimationCoroutine() {
        foreach (Masters_QuizButton quizButton in quizButtonArray) {
            quizButton.gameObject.SetActive(false);
            RectTransform quizButtonRectTransform = quizButton.GetComponent<RectTransform>();
            quizButtonRectTransform.localScale = Vector3.zero;
        }

        questionTMP.gameObject.SetActive(false);

        yield return new WaitForSeconds(timeBetweenEachAnimation);
        questionTMP.text = currentQuiz.question;
        Masters_TextTypeWriter questionTextTypeWriter = questionTMP.GetComponent<Masters_TextTypeWriter>();
        questionTMP.gameObject.SetActive(true);
        questionTextTypeWriter.TriggerAnimation(questionTMP.text.Length);

        for (int i = 0; i < currentQuiz.options.Length; i++) {
            if (i >= quizButtonArray.Length) break;
            
            quizButtonArray[i].gameObject.SetActive(true);
            yield return new WaitForSeconds(timeBetweenEachAnimation);
            quizButtonArray[i].SetText(currentQuiz.options[i]);
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
            RectTransform quizButtonRectTransform = quizButtonArray[i].GetComponent<RectTransform>();
            quizButtonRectTransform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutExpo);
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
