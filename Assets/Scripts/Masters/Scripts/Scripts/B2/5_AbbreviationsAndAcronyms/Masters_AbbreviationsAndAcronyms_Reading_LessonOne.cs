using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_AbbreviationsAndAcronyms_Reading_LessonOne : Masters_Lesson {
    private const string GO_TO_NEXT_QUESTION = "GoToNextQuestion";

    [System.Serializable]
    public struct FlashCardData {
        [TextArea(2, 5)] public string frontText;
        [TextArea(2, 5)] public string backText;
        public AudioClip backTextAudio;
    }

    private class FlashCardInstance {
        public Button button;
        public TextMeshProUGUI textComponent;
        public FlashCardData data;
    }

    [System.Serializable]
    public enum PhraseContext {
        Option1,
        Option2,
        Option3
    }

    [System.Serializable]
    public struct PhraseAndContext {
        [TextArea(2, 5)] public string phrase;
        public string option1Text;
        public string option2Text;
        public string option3Text;
        public PhraseContext context;
    }

    [Header("Flash Cards Settings")]
    [SerializeField] private FlashCardData[] flashCardsDataArray;
    [SerializeField] private GameObject flashCardPrefab;
    [SerializeField] private Transform flashCardsGridTransform;
    [SerializeField] private float flipAnimationSpeed = 0.3f;
    [SerializeField] private TextMeshProUGUI cardsViewedCountTMP;

    [Header("Mini Quiz Settings")]
    [SerializeField] private GameObject miniQuizGameObject;
    [SerializeField] private Button startMiniQuizButton;
    [SerializeField] private int maxQuestionsToAsk = 3;
    [SerializeField] private int requiredCorrectAnswers = 2;
    [SerializeField] private PhraseAndContext[] phraseAndContextArray;
    [SerializeField] private TextMeshProUGUI questionTMP;
    [SerializeField] private TextMeshProUGUI phraseTMP;
    [SerializeField] private Button option1Button;
    [SerializeField] private TextMeshProUGUI option1ButtonTMP;
    [SerializeField] private Button option2Button;
    [SerializeField] private TextMeshProUGUI option2ButtonTMP;
    [SerializeField] private Button option3Button;
    [SerializeField] private TextMeshProUGUI option3ButtonTMP;
    [SerializeField] private Color defaultButtonColor = Color.white;
    [SerializeField] private Color correctButtonColor = Color.green;
    [SerializeField] private Color wrongButtonColor = Color.red;
    [SerializeField] private float timeToNextQuestion = 1.5f;
    [SerializeField] private float timeBetweenEachAnimation = 0.2f;
    [SerializeField] private float animationSpeed = 0.5f;
    [SerializeField] private Button retryQuizButton;
    [SerializeField] private Button closeButton;

    [Header("Quiz Complete Screen Settings")]
    [SerializeField] private GameObject quizCompleteGameObject;
    [SerializeField] private GameObject quizQuestionGameObject;
    [SerializeField] private Image[] starImageArray;
    [SerializeField] private Color goldStarColor = Color.yellow;

    [Header("Optional Typewriters")]
    [SerializeField] private Masters_TextTypeWriter questionTextTypeWriter;
    [SerializeField] private Masters_TextTypeWriter phraseTextTypeWriter;

    [Header("Progression Settings")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private HashSet<Button> openedCardsHashSet = new HashSet<Button>();
    private FlashCardInstance currentlyFlippedCard = null;
    private List<FlashCardInstance> flashCardInstances = new List<FlashCardInstance>();
    private bool canStartMiniQuiz;

    private HashSet<PhraseAndContext> phraseAndContextHashSet = new HashSet<PhraseAndContext>();
    private PhraseAndContext currentPhraseAndContext;
    private int numberOfCorrectAnswers;
    private bool canSelectOption;
    private int numberOfQuestions;

    protected override void Awake() {
        base.Awake();

        if (flashCardPrefab != null && flashCardsGridTransform != null) {
            for (int i = 0; i < flashCardsDataArray.Length; i++) {
                FlashCardData data = flashCardsDataArray[i];
                GameObject cardGO = Instantiate(flashCardPrefab, flashCardsGridTransform);
                
                Button btn = cardGO.GetComponent<Button>();
                TextMeshProUGUI txt = cardGO.GetComponentInChildren<TextMeshProUGUI>();

                if (btn != null && txt != null) {
                    txt.text = data.frontText;
                    
                    FlashCardInstance instance = new FlashCardInstance {
                        button = btn,
                        textComponent = txt,
                        data = data
                    };

                    btn.onClick.AddListener(() => {
                        OnFlashCardClicked(instance);
                    });

                    flashCardInstances.Add(instance);
                } else {
                    Debug.LogWarning("Flash card prefab requires both a Button component and a TextMeshProUGUI component in its children.");
                }
            }
        }

        if (startMiniQuizButton != null) {
            startMiniQuizButton.interactable = false;
            startMiniQuizButton.onClick.AddListener(OnStartMiniQuizButtonClicked);
        }

        if (option1Button != null) option1Button.onClick.AddListener(() => OnContextButtonClicked(PhraseContext.Option1));
        if (option2Button != null) option2Button.onClick.AddListener(() => OnContextButtonClicked(PhraseContext.Option2));
        if (option3Button != null) option3Button.onClick.AddListener(() => OnContextButtonClicked(PhraseContext.Option3));

        if (retryQuizButton != null) retryQuizButton.onClick.AddListener(OnRetryButtonClicked);
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    protected override void Start() {
        base.Start();

        if (miniQuizGameObject != null) miniQuizGameObject.SetActive(false);
        if (flashCardsGridTransform != null) flashCardsGridTransform.gameObject.SetActive(true);
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (retryQuizButton != null) retryQuizButton.gameObject.SetActive(false);

        UpdateCardsViewedCount();
    }

    private void UpdateCardsViewedCount() {
        if (cardsViewedCountTMP != null) {
            cardsViewedCountTMP.text = $"{openedCardsHashSet.Count}/{flashCardsDataArray.Length}";
        }
    }

    private void OnFlashCardClicked(FlashCardInstance card) {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        // Revert the currently flipped card if it's different from the clicked card
        if (currentlyFlippedCard != null && currentlyFlippedCard.button != card.button) {
            FlashCardInstance prevCard = currentlyFlippedCard;
            RevertCard(prevCard);
        }

        if (currentlyFlippedCard != null && currentlyFlippedCard.button == card.button) {
            // Clicking the same card again reverts it
            RevertCard(card);
            currentlyFlippedCard = null;
        } else {
            // Flip the new card
            FlipCard(card);
            currentlyFlippedCard = card;

            if (!openedCardsHashSet.Contains(card.button)) {
                openedCardsHashSet.Add(card.button);
                UpdateCardsViewedCount();

                if (openedCardsHashSet.Count == flashCardsDataArray.Length) {
                    canStartMiniQuiz = true;
                    if (startMiniQuizButton != null) {
                        startMiniQuizButton.interactable = true;
                        Masters_StartMiniQuizButtonAnimator startMiniQuizButtonAnimator = startMiniQuizButton.GetComponent<Masters_StartMiniQuizButtonAnimator>();
                        if (startMiniQuizButtonAnimator != null) {
                            startMiniQuizButtonAnimator.StartMiniQuizButtonAnimation();
                        } else {
                            startMiniQuizButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f);
                        }
                    }
                }
            }
        }
    }

    private void FlipCard(FlashCardInstance card) {
        if (card.button != null && card.textComponent != null) {
            card.button.transform.DORotate(new Vector3(0, 90, 0), flipAnimationSpeed / 2f).OnComplete(() => {
                card.textComponent.text = card.data.backText;
                card.button.transform.DORotate(new Vector3(0, 0, 0), flipAnimationSpeed / 2f);
                
                if (card.data.backTextAudio != null) {
                    Masters_AudioManager.Instance.StopVoiceOver();
                    Masters_AudioManager.Instance.PlayVoiceOver(card.data.backTextAudio);
                }
            });
        }
    }

    private void RevertCard(FlashCardInstance card) {
        if (card.button != null && card.textComponent != null) {
            card.button.transform.DORotate(new Vector3(0, 90, 0), flipAnimationSpeed / 2f).OnComplete(() => {
                card.textComponent.text = card.data.frontText;
                card.button.transform.DORotate(new Vector3(0, 0, 0), flipAnimationSpeed / 2f);
            });
        }
    }

    private void OnStartMiniQuizButtonClicked() {
        if (canStartMiniQuiz) {
            StartMiniQuizPhase();
        }
    }

    private void StartMiniQuizPhase() {
        Masters_AudioManager.Instance.StopVoiceOver();
        
        if (startMiniQuizButton != null) {
            Masters_StartMiniQuizButtonAnimator startMiniQuizButtonAnimator = startMiniQuizButton.GetComponent<Masters_StartMiniQuizButtonAnimator>();
            if (startMiniQuizButtonAnimator != null) {
                startMiniQuizButtonAnimator.ResetAnimation();
            }
            startMiniQuizButton.interactable = false;
        }

        if (flashCardsGridTransform != null) {
            flashCardsGridTransform.gameObject.SetActive(false);
        }

        StartMiniQuiz();
    }

    private void StartMiniQuiz() {
        canStartMiniQuiz = false;

        if (miniQuizGameObject != null) miniQuizGameObject.SetActive(true);
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (quizQuestionGameObject != null) quizQuestionGameObject.SetActive(true);

        phraseAndContextHashSet.Clear();
        numberOfQuestions = 0;
        numberOfCorrectAnswers = 0;
        GoToNextQuestion();
    }

    private void OnRetryButtonClicked() {
        if (retryQuizButton != null) retryQuizButton.gameObject.SetActive(false);

        if (starImageArray != null) {
            foreach (Image image in starImageArray) {
                if (image != null) image.color = Color.white;
            }
        }

        StartMiniQuiz();
    }

    private void OnCloseButtonClicked() {
        if (starImageArray != null) {
            foreach (Image image in starImageArray) {
                if (image != null) image.color = Color.white;
            }
        }

        if (miniQuizGameObject != null) {
            miniQuizGameObject.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo).OnComplete(() => {
                miniQuizGameObject.SetActive(false);
                if (flashCardsGridTransform != null) flashCardsGridTransform.gameObject.SetActive(true);
                
                // Ensure all cards are visible and reverted
                foreach (FlashCardInstance card in flashCardInstances) {
                    if (card.button != null) {
                        card.button.gameObject.SetActive(true);
                    }
                    RevertCard(card);
                }
                currentlyFlippedCard = null;
            });
        }

        canStartMiniQuiz = true;
        if (startMiniQuizButton != null) {
            startMiniQuizButton.interactable = true;
        }
    }

    private void OnContextButtonClicked(PhraseContext phraseContext) {
        if (!canSelectOption) return;

        bool isCorrect = (currentPhraseAndContext.context == phraseContext);

        if (isCorrect) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            numberOfCorrectAnswers++;
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        switch (phraseContext) {
            case PhraseContext.Option1:
                if (option1ButtonTMP != null) option1ButtonTMP.color = isCorrect ? correctButtonColor : wrongButtonColor;
                break;
            case PhraseContext.Option2:
                if (option2ButtonTMP != null) option2ButtonTMP.color = isCorrect ? correctButtonColor : wrongButtonColor;
                break;
            case PhraseContext.Option3:
                if (option3ButtonTMP != null) option3ButtonTMP.color = isCorrect ? correctButtonColor : wrongButtonColor;
                break;
        }

        canSelectOption = false;
        Invoke(GO_TO_NEXT_QUESTION, timeToNextQuestion);
    }

    private void GoToNextQuestion() {
        int totalQuestions = Mathf.Min(maxQuestionsToAsk, phraseAndContextArray.Length);
        
        if (numberOfQuestions >= totalQuestions) {
            ShowQuizCompleteScreen();
            return;
        } else {
            numberOfQuestions++;
        }

        if (phraseAndContextArray.Length > 0) {
            PhraseAndContext randomPhraseAndContext = phraseAndContextArray[Random.Range(0, phraseAndContextArray.Length)];
            
            if (phraseAndContextArray.Length >= totalQuestions) {
                while (phraseAndContextHashSet.Contains(randomPhraseAndContext)) {
                    randomPhraseAndContext = phraseAndContextArray[Random.Range(0, phraseAndContextArray.Length)];
                }
            }
            
            phraseAndContextHashSet.Add(randomPhraseAndContext);
            currentPhraseAndContext = randomPhraseAndContext;
        }

        StartCoroutine(ContextButtonAnimations());

        if (option1ButtonTMP != null) option1ButtonTMP.color = defaultButtonColor;
        if (option2ButtonTMP != null) option2ButtonTMP.color = defaultButtonColor;
        if (option3ButtonTMP != null) option3ButtonTMP.color = defaultButtonColor;

        SetMiniQuizQuestion();
    }

    private IEnumerator ContextButtonAnimations() {
        if (option1Button != null && option2Button != null && option3Button != null) {
            RectTransform option1RectTransform = option1Button.GetComponent<RectTransform>();
            RectTransform option2RectTransform = option2Button.GetComponent<RectTransform>();
            RectTransform option3RectTransform = option3Button.GetComponent<RectTransform>();

            option1RectTransform.localScale = Vector3.zero;
            option2RectTransform.localScale = Vector3.zero;
            option3RectTransform.localScale = Vector3.zero;

            yield return new WaitForSeconds(timeBetweenEachAnimation);
            option1RectTransform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutExpo);
            yield return new WaitForSeconds(timeBetweenEachAnimation * 2);
            option2RectTransform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutExpo);
            yield return new WaitForSeconds(timeBetweenEachAnimation * 3);
            option3RectTransform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutExpo);
        }
    }

    private void SetMiniQuizQuestion() {
        canSelectOption = true;

        if (questionTMP != null) {
            questionTMP.text = $"Question - {numberOfQuestions}: Choose the correct option!";
            if (questionTextTypeWriter != null) {
                questionTextTypeWriter.TriggerAnimation(questionTMP.text.Length);
            }
        }
        
        if (phraseTMP != null) {
            phraseTMP.text = currentPhraseAndContext.phrase;
            if (phraseTextTypeWriter != null) {
                phraseTextTypeWriter.TriggerAnimation(phraseTMP.text.Length);
            }
        }

        if (option1ButtonTMP != null) option1ButtonTMP.text = currentPhraseAndContext.option1Text;
        if (option2ButtonTMP != null) option2ButtonTMP.text = currentPhraseAndContext.option2Text;
        if (option3ButtonTMP != null) option3ButtonTMP.text = currentPhraseAndContext.option3Text;
    }

    private void ShowQuizCompleteScreen() {
        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(true);
        if (quizQuestionGameObject != null) quizQuestionGameObject.SetActive(false);

        if (starImageArray != null) {
            for (int i = 0; i < numberOfCorrectAnswers; i++) {
                if (i < starImageArray.Length && starImageArray[i] != null) {
                    starImageArray[i].color = goldStarColor;
                }
            }
        }

        if (numberOfCorrectAnswers >= requiredCorrectAnswers) {
            nextButton.interactable = true;
            if (retryQuizButton != null) retryQuizButton.gameObject.SetActive(true);
            NextButtonAnimation();
        } else {
            if (retryQuizButton != null) {
                retryQuizButton.gameObject.SetActive(true);
            }
        }
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        if (nextLessonSO != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            if (topic != Masters_Topic.None) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}
