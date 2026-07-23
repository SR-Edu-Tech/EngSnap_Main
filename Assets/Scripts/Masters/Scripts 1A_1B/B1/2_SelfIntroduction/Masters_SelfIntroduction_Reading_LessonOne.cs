using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Masters_SelfIntroduction_Reading_LessonOne : Masters_Lesson {


    [System.Serializable]
    public class QuestionsAndAnswers {

        public string questionText, answerText;
        public AudioClip questionAudioClip, answerAudioClip;

    }


    [SerializeField]
    private Masters_QuestionAndAnswerButton[] questionAndAnswerButtonArray;
    [SerializeField]
    private TextMeshProUGUI progressCounterTMP;
    [SerializeField]
    private RectTransform questionsHeadingRectTransform, answersHeadingRectTransform;
    [SerializeField]
    private float animationSpeed;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;
    [SerializeField]
    private QuestionsAndAnswers[] questionsAndAnswersArray;
    [SerializeField]
    private TextMeshProUGUI questionTMP, answerTMP;
    [SerializeField]
    private GameObject questionCloudGameObject, answerCloudGameObject;
    [SerializeField]
    private float timeBetweenEachQuestionAndAnswer;
    [SerializeField]
    private GameObject scrollViewGameObject, npcGameObject;
    [SerializeField]
    private Button continueDialogueButton;


    private HashSet<Masters_QuestionAndAnswerButton> questionAndAnswerButtonHashSet = new HashSet<Masters_QuestionAndAnswerButton>();
    private Masters_QuestionAndAnswerButton latestQuestionAndAnswerButton;
    private int questionsAndAnswersIndex;


    protected override void Awake() {
        base.Awake();

        foreach(Masters_QuestionAndAnswerButton questionAndAnswerButton in questionAndAnswerButtonArray) {
            foreach(Button button in questionAndAnswerButton.GetQuestionAndAnswerButtonArray()) {
                Masters_QuestionAndAnswerButton questionAndAnswer = questionAndAnswerButton;
                button.onClick.AddListener(() => {
                    OnQuestionAndAnswerButtonClicked(questionAndAnswer);
                });
            }
        }

        Vector2 questionTargetPosition = new Vector2(-322.5f, 50f);
        Vector2 answerTargetPosition = new Vector2(322.5f, 50f);
        Vector2 questionStartingPosition = new Vector2(-322.5f, 500f);
        Vector2 answerStartingPosition = new Vector2(322.5f, 500f);

        questionsHeadingRectTransform.anchoredPosition = questionStartingPosition;
        answersHeadingRectTransform.anchoredPosition = answerStartingPosition;

        questionsHeadingRectTransform.DOAnchorPos(questionTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        answersHeadingRectTransform.DOAnchorPos(answerTargetPosition, animationSpeed).SetEase(Ease.OutExpo);

        continueDialogueButton.onClick.AddListener(OnContinueDialogueButtonClicked);
    }

    private void OnContinueDialogueButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        StopAllCoroutines();
        questionCloudGameObject.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo);
        answerCloudGameObject.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo).OnComplete(() => {
            questionCloudGameObject.SetActive(false);
            answerCloudGameObject.SetActive(false);
            StartCoroutine(StartDialoguesSequenceCoroutine());
        });
    }

    protected override void Start() {
        base.Start();

        questionCloudGameObject.SetActive(false);
        answerCloudGameObject.SetActive(false);

        StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
            StartCoroutine(StartDialoguesSequenceCoroutine());
        }));
    }

    private IEnumerator StartDialoguesSequenceCoroutine() {
        if (questionsAndAnswersIndex == questionsAndAnswersArray.Length) {
            // Over

            continueDialogueButton.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo);
            npcGameObject.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo).OnComplete(() => {
                npcGameObject.SetActive(true);
                scrollViewGameObject.SetActive(true);
                continueDialogueButton.gameObject.SetActive(false);
                continueDialogueButton.transform.localScale = Vector2.one;
            });
            yield break;
        }

        QuestionsAndAnswers questionsAndAnswers = questionsAndAnswersArray[questionsAndAnswersIndex++];

        questionTMP.text = questionsAndAnswers.questionText;
        questionCloudGameObject.SetActive(true);
        Masters_AudioManager.Instance.PlayVoiceOver(questionsAndAnswers.questionAudioClip);
        yield return new WaitForSeconds(questionsAndAnswers.questionAudioClip.length);

        

        answerTMP.text = questionsAndAnswers.answerText;
        answerCloudGameObject.SetActive(true);

        Masters_AudioManager.Instance.PlayVoiceOver(questionsAndAnswers.answerAudioClip);
        yield return new WaitForSeconds(questionsAndAnswers.answerAudioClip.length);

        yield return new WaitForSeconds(timeBetweenEachQuestionAndAnswer);
    }

    private void OnQuestionAndAnswerButtonClicked(Masters_QuestionAndAnswerButton questionAndAnswerButton) {
        if(latestQuestionAndAnswerButton != null) {
            latestQuestionAndAnswerButton.StopCoroutine();
        }
        latestQuestionAndAnswerButton = questionAndAnswerButton;

        questionAndAnswerButton.PlayQuestionAndAnswerAudioClip();

        if (!questionAndAnswerButtonHashSet.Contains(questionAndAnswerButton)) {
            // New one
            questionAndAnswerButtonHashSet.Add(questionAndAnswerButton);
            progressCounterTMP.text = $"{questionAndAnswerButtonHashSet.Count}/12";

            if(questionAndAnswerButtonHashSet.Count == 12) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
        }
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
    }


}
