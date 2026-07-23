using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_StartingConversationWithAStranger_Reading_LessonThree : Masters_Lesson {

    private const string GO_TO_NEXT_QUESTION = "GoToNextQuestion";

    [System.Serializable]
    public class DialogueSet {
        public Button button;
        public GameObject gameObject;
        public AudioClip[] dialogueAudioClipArray;
    }

    [System.Serializable]
    public enum PhraseContext {
        Option1,
        Option2,
        Option3
    }

    [System.Serializable]
    public struct PhraseAndContext {
        public string phrase;
        public string option1Text;
        public string option2Text;
        public string option3Text;
        public PhraseContext context;
    }

    [Header("Dialogue Settings")]
    [SerializeField] private DialogueSet[] dialogueSetArray;
    [SerializeField] private float timeBetweenDialogues;
    [SerializeField] private RectTransform dialogueSetsRectTransform;
    [SerializeField] private float dialogueAnimationSpeed;
    [SerializeField] private CanvasGroup fillCanvasGroup;
    [SerializeField] private CanvasGroup borderCanvasGroup;
    [SerializeField] private TextMeshProUGUI dialogueCountTMP;
    [SerializeField] private Button startQuizButton;
    [SerializeField] private GameObject dialoguePanel;

    [Header("Mini Quiz Settings")]
    [SerializeField] private GameObject quizPanel;
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
    [SerializeField] private Color defaultButtonColor;
    [SerializeField] private Color correctButtonColor;
    [SerializeField] private Color wrongButtonColor;
    [SerializeField] private float timeToNextQuestion;
    [SerializeField] private float timeBetweenEachAnimation;
    [SerializeField] private float quizAnimationSpeed;
    [SerializeField] private Button retryQuizButton;
    [SerializeField] private Button closeButton;

    [Header("Quiz Complete Screen Settings")]
    [SerializeField] private GameObject quizCompleteGameObject;
    [SerializeField] private GameObject quizQuestionGameObject;
    [SerializeField] private Image[] starImageArray;
    [SerializeField] private Color goldStarColor = Color.yellow;

    [Header("Optional Typewriters (If present on prefab)")]
    [SerializeField] private Masters_TextTypeWriter questionTextTypeWriter;
    [SerializeField] private Masters_TextTypeWriter phraseTextTypeWriter;

    private HashSet<DialogueSet> dialogueSetHashSet = new HashSet<DialogueSet>();
    private bool doOnce;
    private Coroutine highlightCoroutine;

    private HashSet<PhraseAndContext> phraseAndContextHashSet = new HashSet<PhraseAndContext>();
    private PhraseAndContext currentPhraseAndContext;
    private int numberOfCorrectAnswers;
    private bool canSelectOption;
    private int numberOfQuestions;

    protected override void Awake() {
        base.Awake();

        // Dialogue setup
        for (int i = 0; i < dialogueSetArray.Length; i++) {
            DialogueSet dialogueSet = dialogueSetArray[i];
            RectTransform dialogueButtonRectTransform = dialogueSetArray[i].button.GetComponent<RectTransform>();  
            dialogueSetArray[i].button.onClick.AddListener(() => {
                OnDialogueSetButtonClicked(dialogueButtonRectTransform, dialogueSet);
            });
        }

        if (startQuizButton != null) {
            startQuizButton.interactable = false;
            startQuizButton.onClick.AddListener(StartQuizPhase);
        }

        // Quiz setup
        if (option1Button != null) {
            option1Button.onClick.AddListener(() => OnContextButtonClicked(PhraseContext.Option1));
        }
        if (option2Button != null) {
            option2Button.onClick.AddListener(() => OnContextButtonClicked(PhraseContext.Option2));
        }
        if (option3Button != null) {
            option3Button.onClick.AddListener(() => OnContextButtonClicked(PhraseContext.Option3));
        }

        if (retryQuizButton != null) {
            retryQuizButton.onClick.AddListener(RetryQuiz);
        }

        if (closeButton != null) {
            closeButton.onClick.AddListener(CloseQuiz);
        }
    }

    protected override void Start() {
        base.Start();

        if (quizPanel != null) quizPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        if (retryQuizButton != null) retryQuizButton.gameObject.SetActive(false);
    }

    // --- DIALOGUE LOGIC ---
    private void StartDialogueBoxAnimation(DialogueSet dialogueSet) {
        if (dialogueSetsRectTransform != null) {
            dialogueSetsRectTransform.DOAnchorPos(Vector3.zero, dialogueAnimationSpeed).SetEase(Ease.OutExpo).OnComplete(() => {
                PlayDialogueLineByLine(dialogueSet);
            });
        } else {
            PlayDialogueLineByLine(dialogueSet);
        }

        if (fillCanvasGroup != null) fillCanvasGroup.DOFade(1f, dialogueAnimationSpeed);
        if (borderCanvasGroup != null) borderCanvasGroup.DOFade(1f, dialogueAnimationSpeed);
    }

    private void PlayDialogueLineByLine(DialogueSet dialogueSet) {
        if (!dialogueSetHashSet.Contains(dialogueSet)) {
            // New
            dialogueSetHashSet.Add(dialogueSet);
            dialogueCountTMP.text = $"{dialogueSetHashSet.Count}/3";
            if (dialogueSetHashSet.Count == 3) {
                if (startQuizButton != null) {
                    startQuizButton.interactable = true;
                    Masters_StartMiniQuizButtonAnimator startMiniQuizButtonAnimator = startQuizButton.GetComponent<Masters_StartMiniQuizButtonAnimator>();
                    if (startMiniQuizButtonAnimator != null) {
                        startMiniQuizButtonAnimator.StartMiniQuizButtonAnimation();
                    } else {
                        startQuizButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.5f);
                    }
                }
            }
        }

        Masters_AudioManager.Instance.StopVoiceOver();

        if (highlightCoroutine != null) {
            StopCoroutine(highlightCoroutine);
            highlightCoroutine = null;
        }

        for (int i = 0; i < dialogueSetArray.Length; i++) {
            if (dialogueSet == dialogueSetArray[i]) {
                dialogueSet.gameObject.SetActive(true);
                Masters_AudioManager.Instance.PlayAudioClipsArray(dialogueSet.dialogueAudioClipArray, timeBetweenDialogues);
                
                // Start coroutine to highlight the next button
                if (i + 1 < dialogueSetArray.Length) {
                    highlightCoroutine = StartCoroutine(HighlightNextButton(dialogueSet.dialogueAudioClipArray, dialogueSetArray[i + 1].button));
                }

                continue;
            }
            dialogueSetArray[i].gameObject.SetActive(false);
        }
    }

    private IEnumerator HighlightNextButton(AudioClip[] audioClipArray, Button nextButton) {
        float totalWaitTime = 0f;
        if (audioClipArray != null) {
            for (int j = 0; j < audioClipArray.Length; j++) {
                if (audioClipArray[j] != null) {
                    totalWaitTime += audioClipArray[j].length;
                }
            }
            totalWaitTime += timeBetweenDialogues * Mathf.Max(0, audioClipArray.Length - 1);
        }
        
        yield return new WaitForSeconds(totalWaitTime);
        
        if (nextButton != null) {
            RectTransform nextBtnRect = nextButton.GetComponent<RectTransform>();
            // Subtle but noticeable expanding and contracting
            nextBtnRect.DOScale(1.05f, 0.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
        }
    }

    private void OnDialogueSetButtonClicked(RectTransform rectTransform, DialogueSet dialogueSet) {
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        // Stop any looping animation on all buttons and reset scale
        foreach (DialogueSet ds in dialogueSetArray) {
            if (ds.button != null) {
                RectTransform rt = ds.button.GetComponent<RectTransform>();
                rt.DOKill(true);
                rt.localScale = Vector3.one;
            }
        }

        rectTransform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);

        if (!doOnce) {
            doOnce = true;
            StartDialogueBoxAnimation(dialogueSet);
            return;
        }

        PlayDialogueLineByLine(dialogueSet);
    }

    // --- TRANSITION LOGIC ---
    private void StartQuizPhase() {
        Masters_AudioManager.Instance.StopVoiceOver();
        if (dialoguePanel != null) dialoguePanel.SetActive(false);
        if (quizPanel != null) quizPanel.SetActive(true);

        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (quizQuestionGameObject != null) quizQuestionGameObject.SetActive(true);
        
        if (startQuizButton != null) {
            Masters_StartMiniQuizButtonAnimator anim = startQuizButton.GetComponent<Masters_StartMiniQuizButtonAnimator>();
            if (anim != null) anim.ResetAnimation();
            startQuizButton.interactable = false;
        }

        phraseAndContextHashSet.Clear();
        numberOfQuestions = 0;
        numberOfCorrectAnswers = 0;
        GoToNextQuestion();
    }

    private void RetryQuiz() {
        if (retryQuizButton != null) retryQuizButton.gameObject.SetActive(false);

        if (starImageArray != null) {
            foreach (Image image in starImageArray) {
                if (image != null) image.color = Color.white;
            }
        }

        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (quizQuestionGameObject != null) quizQuestionGameObject.SetActive(true);

        phraseAndContextHashSet.Clear();
        numberOfQuestions = 0;
        numberOfCorrectAnswers = 0;
        GoToNextQuestion();
    }

    private void CloseQuiz() {
        if (starImageArray != null) {
            foreach (Image image in starImageArray) {
                if (image != null) image.color = Color.white;
            }
        }

        if (quizCompleteGameObject != null) quizCompleteGameObject.SetActive(false);
        if (quizQuestionGameObject != null) quizQuestionGameObject.SetActive(true);
        if (retryQuizButton != null) retryQuizButton.gameObject.SetActive(false);

        if (quizPanel != null) quizPanel.SetActive(false);
        if (dialoguePanel != null) dialoguePanel.SetActive(true);

        if (startQuizButton != null) {
            startQuizButton.interactable = true;
        }
    }

    // --- MINI QUIZ LOGIC ---
    private void OnContextButtonClicked(PhraseContext phraseContext) {
        if (!canSelectOption) {
            return;
        }

        switch (phraseContext) {
            case PhraseContext.Option1:
                if (currentPhraseAndContext.context == phraseContext) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                    if (option1ButtonTMP != null) option1ButtonTMP.color = correctButtonColor;
                    numberOfCorrectAnswers++;
                } else {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                    if (option1ButtonTMP != null) option1ButtonTMP.color = wrongButtonColor;
                }
                canSelectOption = false;
                Invoke(GO_TO_NEXT_QUESTION, timeToNextQuestion);
                break;
            case PhraseContext.Option2:
                if (currentPhraseAndContext.context == phraseContext) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                    if (option2ButtonTMP != null) option2ButtonTMP.color = correctButtonColor;
                    numberOfCorrectAnswers++;
                } else {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                    if (option2ButtonTMP != null) option2ButtonTMP.color = wrongButtonColor;
                }
                canSelectOption = false;
                Invoke(GO_TO_NEXT_QUESTION, timeToNextQuestion);
                break;
            case PhraseContext.Option3:
                if (currentPhraseAndContext.context == phraseContext) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                    if (option3ButtonTMP != null) option3ButtonTMP.color = correctButtonColor;
                    numberOfCorrectAnswers++;
                } else {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                    if (option3ButtonTMP != null) option3ButtonTMP.color = wrongButtonColor;
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

        if (phraseAndContextArray.Length > 0) {
            PhraseAndContext randomPhraseAndContext = phraseAndContextArray[Random.Range(0, phraseAndContextArray.Length)];
            
            // Safety check to prevent infinite loop if array is smaller than 3
            if (phraseAndContextArray.Length >= 3) {
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

    private IEnumerator ContextButtonAnimations() {
        if (option1Button != null && option2Button != null && option3Button != null) {
            RectTransform option1RectTransform = option1Button.GetComponent<RectTransform>();
            RectTransform option2RectTransform = option2Button.GetComponent<RectTransform>();
            RectTransform option3RectTransform = option3Button.GetComponent<RectTransform>();

            option1RectTransform.localScale = Vector3.zero;
            option2RectTransform.localScale = Vector3.zero;
            option3RectTransform.localScale = Vector3.zero;

            yield return new WaitForSeconds(timeBetweenEachAnimation);
            option1RectTransform.DOScale(Vector3.one, quizAnimationSpeed).SetEase(Ease.OutExpo);
            yield return new WaitForSeconds(timeBetweenEachAnimation * 2);
            option2RectTransform.DOScale(Vector3.one, quizAnimationSpeed).SetEase(Ease.OutExpo);
            yield return new WaitForSeconds(timeBetweenEachAnimation * 3);
            option3RectTransform.DOScale(Vector3.one, quizAnimationSpeed).SetEase(Ease.OutExpo);
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


