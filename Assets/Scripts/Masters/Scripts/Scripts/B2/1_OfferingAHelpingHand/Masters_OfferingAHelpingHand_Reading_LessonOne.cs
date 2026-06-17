using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_OfferingAHelpingHand_Reading_LessonOne : Masters_Lesson {


    private const string LOAD_NEXT_SET_OF_STATEMENTS_AND_BUTTONS = "LoadNextSetOfStatementsAndButtons";


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
    [SerializeField]
    private Masters_FillInTheBlanks fillInTheBlanks;


    private int setIndex;
    private GameObject currentStatementGameObject;
    private GameObject currentWordsGameObject;
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
            completedRectTransform.localScale = Vector3.one;

            // BULLETPROOF FIX: We no longer Destroy and Instantiate the Prefab.
            // If the spawn position (fillInTheBlanksGameObjectPosition) is ordered behind your background image in the Hierarchy,
            // any newly instantiated prefab spawns invisible behind the background! (Which is why you hear it but can't see it).
            // Resetting the existing object in-place completely prevents all Hierarchy Z-order bugs.

            // 1. Un-shrink the original puzzle (it was scaled to 0 in CompleteScreenAnimation)
            fillInTheBlanksRectTransform.localScale = Vector3.one;

            // 2. Loop through all statements/words and reset them
            for (int i = 0; i < fillInTheBlanks.GetBlanksAndWordsArray().Length; i++) {
                
                // Turn off GameObjects so they can restart their PopUp animations cleanly
                fillInTheBlanks.GetStatementGameObjectArray()[i].SetActive(false);
                fillInTheBlanks.GetWordsGameObjectArray()[i].SetActive(false);
                
                var blanksAndWords = fillInTheBlanks.GetBlanksAndWordsArray()[i];

                // Reset all Blanks
                foreach (Button blankBtn in blanksAndWords.blankButtonArray) {
                    blankBtn.onClick.RemoveAllListeners();
                    blankBtn.interactable = true;
                    if (blankBtn.TryGetComponent(out Masters_FillInTheBlank_Blank blank)) {
                        blank.SetWordToBlank(tapHereText); 
                        blank.GetComponentInChildren<TextMeshProUGUI>().color = Color.white; // Reset to white text
                    }
                }

                // Reset all Words
                foreach (Button wordBtn in blanksAndWords.wordButtonArray) {
                    wordBtn.onClick.RemoveAllListeners();
                    wordBtn.interactable = true;
                }
            }

            // Clear old references
            currentStatementGameObject = null;
            currentWordsGameObject = null;
            currentSelectedBlank = null;
            numberOfWordsInteracted = 0;

            setIndex = 0;
            LoadNextSetOfStatementsAndButtons();

            correctlyFilled = 0;
            correctlyFilledTMP.text = $"{correctlyFilled}/6";
        });
    }

    private void LoadNextSetOfStatementsAndButtons() {
        if (setIndex == 1) {
            nextButton.interactable = true;
            NextButtonAnimation();
            CompleteScreenAnimation();
            return;
        }

        if (currentStatementGameObject != null) {
            currentStatementGameObject.SetActive(false);
        }

        if (currentWordsGameObject != null) {
            currentWordsGameObject.SetActive(false);
        }

        GameObject[] statementGameObjectArray = fillInTheBlanks.GetStatementGameObjectArray();
        GameObject[] wordsGameObjectArray = fillInTheBlanks.GetWordsGameObjectArray();
        Masters_FillInTheBlanks.BlanksAndWords[] blanksAndWordsArray = fillInTheBlanks.GetBlanksAndWordsArray();

        currentStatementGameObject = statementGameObjectArray[setIndex];
        currentWordsGameObject = wordsGameObjectArray[setIndex];
        fillInTheBlanks.SetCurrentBlanksAndWords(blanksAndWordsArray[setIndex]);

        foreach (Button blankButton in fillInTheBlanks.GetCurrentBlanksAndWords().blankButtonArray) {
            blankButton.onClick.AddListener(() => {
                OnBlankButtonClicked(blankButton);
            });
        }

        foreach (Button wordButton in fillInTheBlanks.GetCurrentBlanksAndWords().wordButtonArray) {
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
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        if (button.TryGetComponent(out Masters_FillInTheBlank_Blank fillInTheBlank_Blank)) {
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
        if (currentSelectedBlank == null) {
            return;
        }

        if (button.TryGetComponent(out Masters_FillInTheBlank_Word fillInTheBlank_Word)) {
            string wordText = fillInTheBlank_Word.GetWord();
            if (currentSelectedBlank.GetCorrectWord() == wordText) {
                // Correct word
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                currentSelectedBlank.SetWordAndColorToBlank(wordText, correctColor);
                correctlyFilledTMP.text = $"{++correctlyFilled}/6";
            } else {
                // Incorrect word
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                currentSelectedBlank.SetWordAndColorToBlank(wordText, wrongColor);
                button.interactable = true; // Re-enable if wrong so it can be used for another blank
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
        
        if (nextLessonSO != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            // No next lesson provided, complete the topic and go back to topic selection screen
            if (topic != Masters_Topic.None) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }


}
