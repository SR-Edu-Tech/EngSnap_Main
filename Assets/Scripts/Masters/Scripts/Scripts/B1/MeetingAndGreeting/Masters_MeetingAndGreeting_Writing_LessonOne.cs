using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_MeetingAndGreeting_Writing_LessonOne : Masters_Lesson {


    private const string LOAD_NEXT_SET_OF_STATEMENTS_AND_BUTTONS = "LoadNextSetOfStatementsAndButtons";


    [System.Serializable]
    public struct BlanksAndWords {

        public Button[] blankButtonArray;
        public Button[] wordButtonArray;

    }


    [SerializeField]
    private GameObject[] statementGameObjectArray;
    [SerializeField]
    private GameObject[] wordsGameObjectArray;
    [SerializeField]
    private BlanksAndWords[] blanksAndWordsArray;
    [SerializeField]
    private Color correctColor;
    [SerializeField]
    private Color wrongColor;
    [SerializeField]
    private int numberOfWordsPerSet;
    [SerializeField]
    private float timeBetweenEachSet;
    [SerializeField]
    private string selectWordText;
    [SerializeField]
    private string tapHereText;
    [SerializeField]
    private TextMeshProUGUI correctlyFilledTMP;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;
    [SerializeField]
    private RectTransform fillInTheBlanksRectTransform;
    [SerializeField]
    private RectTransform completedRectTransform;
    [SerializeField]
    private GameObject completedGameObject;
    [SerializeField]
    private float animationSpeed;
    [SerializeField]
    private Transform fillInTheBlanksGameObjectPosition;
    [SerializeField]
    private GameObject fillInTheBlanksGameObjectPrefab;
    [SerializeField]
    private GameObject currentFillInTheBlanksGameObject;
    [SerializeField]
    private Button retryButton;


    private int setIndex;
    private GameObject currentStatementGameObject;
    private GameObject currentWordsGameObject;
    private BlanksAndWords currentBlanksAndWords;
    private Masters_FillInTheBlank_Blank currentSelectedBlank;
    private int numberOfWordsInteracted;
    private int correctlyFilled;


    protected override void Awake() {
        base.Awake();

        LoadNextSetOfStatementsAndButtons();
        retryButton.onClick.AddListener(OnRetryButtonClicked);
    }

    private void OnRetryButtonClicked() {
        completedRectTransform.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.OutExpo).OnComplete(() => {
            completedGameObject.SetActive(false);

            Destroy(currentFillInTheBlanksGameObject);
            currentFillInTheBlanksGameObject = Instantiate(fillInTheBlanksGameObjectPrefab);
            currentFillInTheBlanksGameObject.transform.SetParent(fillInTheBlanksGameObjectPosition, true);
            currentFillInTheBlanksGameObject.transform.localScale = Vector3.one;

            correctlyFilled = 0;
            correctlyFilledTMP.text = $"Correctly Filled: {correctlyFilled}/12";
        });
    }

    private void LoadNextSetOfStatementsAndButtons() {
        if(setIndex == 3) {
            nextButton.interactable = true;
            NextButtonAnimation();
            CompleteScreenAnimation();
            return;
        }

        if(currentStatementGameObject != null) {
            currentStatementGameObject.SetActive(false);
        }

        if(currentWordsGameObject != null) {
            currentWordsGameObject.SetActive(false);
        }

        currentStatementGameObject = statementGameObjectArray[setIndex];
        currentWordsGameObject = wordsGameObjectArray[setIndex];
        currentBlanksAndWords = blanksAndWordsArray[setIndex];

        foreach(Button blankButton in currentBlanksAndWords.blankButtonArray) {
            blankButton.onClick.AddListener(() => {
                OnBlankButtonClicked(blankButton);
            });
        }

        foreach(Button wordButton in currentBlanksAndWords.wordButtonArray) {
            wordButton.onClick.AddListener(() => {
                OnWordButtonClicked(wordButton);
            });
        }

        currentStatementGameObject.SetActive(true);
        currentWordsGameObject.SetActive(true);

        setIndex++;
    }

    private void CompleteScreenAnimation() {
        fillInTheBlanksRectTransform.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.OutExpo).OnComplete(() => {
            completedGameObject.SetActive(true);
        });
    }

    private void OnBlankButtonClicked(Button button) {
        if(button.TryGetComponent(out Masters_FillInTheBlank_Blank fillInTheBlank_Blank)) {
            if (currentSelectedBlank) {
                currentSelectedBlank.SetWordToBlank(tapHereText);
            }

            currentSelectedBlank = fillInTheBlank_Blank;
            currentSelectedBlank.SetWordToBlank(selectWordText);
        } else {
            Debug.Log($"{button} does not have FillInTheBlank_Blank component!");
        }
    }

    private void OnWordButtonClicked(Button button) {
        if(currentSelectedBlank == null) {
            return;
        }

        if (button.TryGetComponent(out Masters_FillInTheBlank_Word fillInTheBlank_Word)) {
            string wordText = fillInTheBlank_Word.GetWord();
            if (currentSelectedBlank.GetCorrectWord() == wordText) {
                // Correct word
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                currentSelectedBlank.SetWordAndColorToBlank(wordText, correctColor);
                correctlyFilledTMP.text = $"Correctly Filled: {++correctlyFilled}/12";
            } else {
                // Incorrect word
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                currentSelectedBlank.SetWordAndColorToBlank(wordText, wrongColor);
            }
        } else {
            Debug.Log($"{button} does not have FillInTheBlank_Word component!");
        }

        numberOfWordsInteracted++;
        if (numberOfWordsInteracted == numberOfWordsPerSet) {
            // Completed a set
            numberOfWordsInteracted = 0;
            Invoke(LOAD_NEXT_SET_OF_STATEMENTS_AND_BUTTONS, timeBetweenEachSet);
        }

        currentSelectedBlank = null;
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
    }


}
