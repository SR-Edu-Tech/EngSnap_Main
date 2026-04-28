using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_MeetingAndGreeting_Reading_LessonOne : Masters_Lesson {


    private const string  GO_TO_NEXT_QUESTION = "GoToNextQuestion";


    [System.Serializable]
    public enum PhraseContext {

        Casual,
        Formal,
        Friendly

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
    private Button casualContextButton;
    [SerializeField]
    private TextMeshProUGUI casualContextButtonTMP;
    [SerializeField]
    private Button formalContextButton;
    [SerializeField]
    private TextMeshProUGUI formalContextButtonTMP;
    [SerializeField]
    private Button friendlyContextButton;
    [SerializeField]
    private TextMeshProUGUI friendlyContextButtonTMP;
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
    private Masters_LessonSO nextLessonSO;
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

        foreach(Button button in phraseCardButtonArray) {
            button.onClick.AddListener(() => {
                OnPhraseCardButtonClicked(button);
            });
        }

        startMiniQuizButton.onClick.AddListener(OnStartMiniQuizButtonClicked);
        casualContextButton.onClick.AddListener(() => {
            OnContextButtonClicked(PhraseContext.Casual);
        });
        formalContextButton.onClick.AddListener(() => {
            OnContextButtonClicked(PhraseContext.Formal);
        });
        friendlyContextButton.onClick.AddListener(() => {
            OnContextButtonClicked(PhraseContext.Friendly);
        });

        retryButton.onClick.AddListener(OnRetryButtonClicked);
        closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    private void OnCloseButtonClicked() {
        foreach(Image image in starImageArray) {
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
            case PhraseContext.Casual:
                // Correct Answer
                if (currentPhraseAndContext.context == phraseContext) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                    casualContextButtonTMP.color = correctButtonColor;
                    numberOfCorrectAnswers++;
                } else {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                    casualContextButtonTMP.color = wrongButtonColor;
                }

                canSelectOption = false;
                Invoke(GO_TO_NEXT_QUESTION, timeToNextQuestion);
                break;
            case PhraseContext.Formal:
                // Correct Answer
                if (currentPhraseAndContext.context == phraseContext) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                    formalContextButtonTMP.color = correctButtonColor;
                    numberOfCorrectAnswers++;
                } else {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                    formalContextButtonTMP.color = wrongButtonColor;
                }

                canSelectOption = false;
                Invoke(GO_TO_NEXT_QUESTION, timeToNextQuestion);
                break;
            case PhraseContext.Friendly:
                // Correct Answer
                if (currentPhraseAndContext.context == phraseContext) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                    friendlyContextButtonTMP.color = correctButtonColor;
                    numberOfCorrectAnswers++;
                } else {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                    friendlyContextButtonTMP.color = wrongButtonColor;
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

        casualContextButtonTMP.color = defaultButtonColor;
        formalContextButtonTMP.color = defaultButtonColor;
        friendlyContextButtonTMP.color = defaultButtonColor;

        SetMiniQuizQuestion();
    }

    private IEnumerator ContextButtonAnimations() {
        RectTransform casualContextRectTransform = casualContextButton.GetComponent<RectTransform>();
        RectTransform formalContextRectTransform = formalContextButton.GetComponent<RectTransform>();
        RectTransform friendlyContextRectTransform = friendlyContextButton.GetComponent<RectTransform>();

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

        for(int i = 0; i < numberOfCorrectAnswers; i++) {
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
        foreach(Button goodbyePhraseCardButton in phraseCardButtonArray) {
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
        quizFillCanvasGroup.DOFade(1f, fadeAnimationSpeed).SetEase(Ease.OutExpo);
        quizBorderCanvasGroup.DOFade(1f, fadeAnimationSpeed).SetEase(Ease.OutExpo);
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
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
    }


}
