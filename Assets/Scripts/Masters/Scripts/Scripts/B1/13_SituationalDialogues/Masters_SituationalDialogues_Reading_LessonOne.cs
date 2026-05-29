using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_SituationalDialogues_Reading_LessonOne : Masters_Lesson {


    private const string GO_TO_NEXT_QUESTION = "GoToNextQuestion";


    [System.Serializable]
    public enum PhraseContext {

        School,
        Doctor,
        Cafe

    }


    [System.Serializable]
    public struct PhraseAndContext {

        public string phrase;
        public PhraseContext context;

    }


    [SerializeField]
    private Button[] phraseCardButtonArray;
    [SerializeField]
    private GameObject phraseCardsGridGameObject;
    [SerializeField]
    private GameObject miniQuizGameObject;
    [SerializeField]
    private Button startMiniQuizButton;
    [SerializeField]
    private Button schoolContextButton;
    [SerializeField]
    private TextMeshProUGUI schoolContextButtonTMP;
    [SerializeField]
    private Button doctorContextButton;
    [SerializeField]
    private TextMeshProUGUI doctorContextButtonTMP;
    [SerializeField]
    private Button cafeContextButton;
    [SerializeField]
    private TextMeshProUGUI cafeContextButtonTMP;
    [SerializeField]
    private PhraseAndContext[] phraseAndContextArray;
    [SerializeField]
    private TextMeshProUGUI questionTMP;
    [SerializeField]
    private TextMeshProUGUI phraseTMP;
    [SerializeField]
    private Color defaultButtonColor;
    [SerializeField]
    private Color correctButtonColor;
    [SerializeField]
    private Color wrongButtonColor;
    [SerializeField]
    private float timeToNextQuestion;
    [SerializeField]
    private GameObject quizCompleteGameObject;
    [SerializeField]
    private GameObject quizQuestionGameObject;
    [SerializeField]
    private Image[] starImageArray;
    [SerializeField]
    private Color goldStarColor;
    [SerializeField]
    private Button retryButton;
    [SerializeField]
    private Button closeButton;
    [SerializeField]
    private TextMeshProUGUI goodbyePhraseCountTMP;
    [SerializeField]
    private float animationSpeed;
    [SerializeField]
    private float timeBetweenEachAnimation;
    [SerializeField]
    private float fadeAnimationSpeed;
    [SerializeField]
    private RectTransform miniQuizHeadingRectTransform;
    [SerializeField]
    private RectTransform miniQuizRectTransform;
    [SerializeField]
    private CanvasGroup quizFillCanvasGroup;
    [SerializeField]
    private CanvasGroup quizBorderCanvasGroup;
    [SerializeField]
    private Masters_TextTypeWriter questiontextTypeWriter;
    [SerializeField]
    private Masters_TextTypeWriter phrasetextTypeWriter;


    private HashSet<PhraseAndContext> phraseAndContextHashSet = new HashSet<PhraseAndContext>();
    private HashSet<Button> phraseCardButtonHashSet = new HashSet<Button>();
    private bool canStartMiniQuiz;
    private PhraseAndContext currentPhraseAndContext;
    private int numberOfCorrectAnswers;
    private bool canSelectOption;
    private int numberOfQuestions;


    protected override void Awake() {
        base.Awake();

        foreach (Button button in phraseCardButtonArray) {
            button.onClick.AddListener(() => {
                OnPhraseCardButtonClicked(button);
            });
        }

        startMiniQuizButton.onClick.AddListener(OnStartMiniQuizButtonClicked);
        schoolContextButton.onClick.AddListener(() => {
            OnContextButtonClicked(PhraseContext.School);
        });
        doctorContextButton.onClick.AddListener(() => {
            OnContextButtonClicked(PhraseContext.Doctor);
        });
        cafeContextButton.onClick.AddListener(() => {
            OnContextButtonClicked(PhraseContext.Cafe);
        });

        retryButton.onClick.AddListener(OnRetryButtonClicked);
        closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    private void OnCloseButtonClicked() {
        foreach (Image image in starImageArray) {
            image.color = Color.white;
        }

        miniQuizRectTransform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo).OnComplete(() => {
            miniQuizGameObject.SetActive(false);
            phraseCardsGridGameObject.SetActive(true);

            foreach (Button goodbyePhraseCardButton in phraseCardButtonArray) {
                goodbyePhraseCardButton.gameObject.SetActive(true);
            }
        });

        canStartMiniQuiz = true;
    }

    private void OnRetryButtonClicked() {
        foreach (Image image in starImageArray) {
            image.color = Color.white;
        }

        quizQuestionGameObject.SetActive(true);
        StartMiniQuiz();
    }

    private void OnContextButtonClicked(PhraseContext phraseContext) {
        if (!canSelectOption) {
            return;
        }

        switch (phraseContext) {
            case PhraseContext.School:
                // Correct Answer
                if (currentPhraseAndContext.context == phraseContext) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                    schoolContextButtonTMP.color = correctButtonColor;
                    numberOfCorrectAnswers++;
                } else {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                    schoolContextButtonTMP.color = wrongButtonColor;
                }

                canSelectOption = false;
                Invoke(GO_TO_NEXT_QUESTION, timeToNextQuestion);
                break;
            case PhraseContext.Doctor:
                // Correct Answer
                if (currentPhraseAndContext.context == phraseContext) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                    doctorContextButtonTMP.color = correctButtonColor;
                    numberOfCorrectAnswers++;
                } else {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                    doctorContextButtonTMP.color = wrongButtonColor;
                }

                canSelectOption = false;
                Invoke(GO_TO_NEXT_QUESTION, timeToNextQuestion);
                break;
            case PhraseContext.Cafe:
                // Correct Answer
                if (currentPhraseAndContext.context == phraseContext) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                    cafeContextButtonTMP.color = correctButtonColor;
                    numberOfCorrectAnswers++;
                } else {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                    cafeContextButtonTMP.color = wrongButtonColor;
                }

                canSelectOption = false;
                Invoke(GO_TO_NEXT_QUESTION, timeToNextQuestion);
                break;
        }
    }

    private void GoToNextQuestion() {
        if (numberOfQuestions == 3) {
            // All questions over
            ShowQuizCompleteScreen();
            return;
        } else {
            numberOfQuestions++;
        }
        PhraseAndContext randomPhraseAndContext = phraseAndContextArray[Random.Range(0, phraseAndContextArray.Length)];
        while (phraseAndContextHashSet.Contains(randomPhraseAndContext)) {
            randomPhraseAndContext = phraseAndContextArray[Random.Range(0, phraseAndContextArray.Length)];
        }
        phraseAndContextHashSet.Add(randomPhraseAndContext);
        currentPhraseAndContext = randomPhraseAndContext;

        StartCoroutine(ContextButtonAnimations());

        schoolContextButtonTMP.color = defaultButtonColor;
        doctorContextButtonTMP.color = defaultButtonColor;
        cafeContextButtonTMP.color = defaultButtonColor;

        SetMiniQuizQuestion();
    }

    private IEnumerator ContextButtonAnimations() {
        RectTransform casualContextRectTransform = schoolContextButton.GetComponent<RectTransform>();
        RectTransform formalContextRectTransform = doctorContextButton.GetComponent<RectTransform>();
        RectTransform friendlyContextRectTransform = cafeContextButton.GetComponent<RectTransform>();

        casualContextRectTransform.localScale = Vector3.zero;
        formalContextRectTransform.localScale = Vector3.zero;
        friendlyContextRectTransform.localScale = Vector3.zero;

        yield return new WaitForSeconds(timeBetweenEachAnimation);
        casualContextRectTransform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(timeBetweenEachAnimation * 2);
        formalContextRectTransform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutExpo);
        yield return new WaitForSeconds(timeBetweenEachAnimation * 3);
        friendlyContextRectTransform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutExpo);
    }

    private void ShowQuizCompleteScreen() {
        quizCompleteGameObject.SetActive(true);
        quizQuestionGameObject.SetActive(false);

        for (int i = 0; i < numberOfCorrectAnswers; i++) {
            starImageArray[i].color = goldStarColor;
        }

        nextButton.interactable = true;
        NextButtonAnimation();
    }

    private void OnPhraseCardButtonClicked(Button button) {
        if (!phraseCardButtonHashSet.Contains(button)) {
            phraseCardButtonHashSet.Add(button);

            goodbyePhraseCountTMP.text = $"{phraseCardButtonHashSet.Count}/9";
            if (phraseCardButtonHashSet.Count == phraseCardButtonArray.Length) {
                // All buttons clicked at least once.
                canStartMiniQuiz = true;
                startMiniQuizButton.interactable = true;
                Masters_StartMiniQuizButtonAnimator startMiniQuizButtonAnimator =
                    startMiniQuizButton.GetComponent<Masters_StartMiniQuizButtonAnimator>();
                startMiniQuizButtonAnimator.StartMiniQuizButtonAnimation();
            }
        }
    }

    private void OnStartMiniQuizButtonClicked() {
        if (canStartMiniQuiz) {
            MiniQuizPopUpAnimation();
        }
    }

    private void MiniQuizPopUpAnimation() {
        foreach (Button goodbyePhraseCardButton in phraseCardButtonArray) {
            goodbyePhraseCardButton.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo).OnComplete(() => {
                goodbyePhraseCardButton.gameObject.SetActive(false);
            });
        }

        StartMiniQuiz();

        startMiniQuizButton.interactable = false;
        Masters_StartMiniQuizButtonAnimator startMiniQuizButtonAnimator =
                startMiniQuizButton.GetComponent<Masters_StartMiniQuizButtonAnimator>();
        startMiniQuizButtonAnimator.ResetAnimation();


        miniQuizHeadingRectTransform.anchoredPosition = new Vector2(0f, 600f);
        quizFillCanvasGroup.alpha = 0f;
        quizBorderCanvasGroup.alpha = 0f;

        miniQuizHeadingRectTransform.DOAnchorPos(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo);
        quizFillCanvasGroup.DOFade(1f, fadeAnimationSpeed);
        quizBorderCanvasGroup.DOFade(1f, fadeAnimationSpeed);
    }

    private void StartMiniQuiz() {
        canStartMiniQuiz = false;

        phraseAndContextHashSet = new HashSet<PhraseAndContext>();

        quizCompleteGameObject.SetActive(false);
        phraseCardsGridGameObject.SetActive(false);
        miniQuizGameObject.SetActive(true);

        numberOfQuestions = 0;
        numberOfCorrectAnswers = 0;
        GoToNextQuestion();
    }

    private void SetMiniQuizQuestion() {
        canSelectOption = true;

        questionTMP.text = $"Question - {numberOfQuestions}: Match phrase to context!";
        phraseTMP.text = currentPhraseAndContext.phrase;

        questiontextTypeWriter.TriggerAnimation(questionTMP.text.Length);
        phrasetextTypeWriter.TriggerAnimation(phraseTMP.text.Length);
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
