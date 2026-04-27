using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_MeetingAndGreeting_Quiz_LessonOne : Masters_Lesson {


    private const string SET_QUIZ = "SetQuiz";


    [System.Serializable]
    public class Quiz {

        public string question;
        public string[] fourOptions = new string[4];
        [Range(0, 3)]
        public int correctOptionIndex;

    }


    [SerializeField]
    private Quiz[] quizArray;
    [SerializeField]
    private TextMeshProUGUI questionTMP;
    [SerializeField]
    private Masters_QuizButton[] quizButtonArray;
    [SerializeField]
    private Button confirmButton;
    [SerializeField]
    private Color selectedColor;
    [SerializeField]
    private Color defaultColor;
    [SerializeField]
    private Color correctColor;
    [SerializeField]
    private Color incorrectColor;
    [SerializeField]
    private float timeBetweenEachQuizQuestion;


    private int currentQuizIndex;
    private Quiz currentQuiz;
    private Masters_QuizButton currentlyPressedQuizButton;


    protected override void Awake() {
        base.Awake();

        confirmButton.onClick.AddListener(OnConfirmButtonClicked);
    }

    private void Start() {
        SetQuiz();

        for (int i = 0; i < quizButtonArray.Length; i++) {
            int buttonIndex = i;

            quizButtonArray[i].GetButton().onClick.AddListener(() => {
                OnQuizButtonClicked(quizButtonArray[buttonIndex], buttonIndex);
            });
        }
    }

    private void OnConfirmButtonClicked() {
        if (currentQuiz.correctOptionIndex == currentlyPressedQuizButton.GetButtonIndex()) {
            // Correct
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            currentlyPressedQuizButton.GetButtonImage().color = correctColor;

            Invoke(SET_QUIZ, timeBetweenEachQuizQuestion);
        } else {
            // Incorrect
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            currentlyPressedQuizButton.GetButton().interactable = false;
            currentlyPressedQuizButton.GetButtonImage().color = incorrectColor;
            currentlyPressedQuizButton = null;
        }
    }

    private void OnQuizButtonClicked(Masters_QuizButton quizButton, int buttonIndex) {
        if(currentlyPressedQuizButton != null) {
            currentlyPressedQuizButton.GetButton().transition = Selectable.Transition.ColorTint;
            currentlyPressedQuizButton.GetButtonImage().color = defaultColor;
        }

        quizButton.SetButtonIndex(buttonIndex);
        currentlyPressedQuizButton = quizButton;

        currentlyPressedQuizButton.GetButton().transition = Selectable.Transition.None;
        currentlyPressedQuizButton.GetButtonImage().color = selectedColor;
    }

    private void SetQuiz() {
        if(currentQuizIndex == quizArray.Length) {
            // Over
            nextButton.interactable = true;
            return;
        }

        foreach(Masters_QuizButton quizButton in quizButtonArray) {
            quizButton.GetButton().transition = Selectable.Transition.ColorTint;
            quizButton.GetButton().interactable = true;
            quizButton.GetButtonImage().color = defaultColor;
        }

        currentQuiz = quizArray[currentQuizIndex++];

        questionTMP.text = currentQuiz.question;
        for(int i = 0; i < currentQuiz.fourOptions.Length; i++) {
            quizButtonArray[i].SetText(currentQuiz.fourOptions[i]);
        }
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
