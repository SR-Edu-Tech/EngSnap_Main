using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Reading Lesson 1 for Unit 11 Is There a Difference?
/// Implements Fill in the Blanks mechanic supporting dynamic multi-round sets (e.g., 6/6 split).
/// </summary>
public class Masters_IsThereADifference_Reading_LessonOne : Masters_Lesson {

    private const string LOAD_NEXT_SET_OF_STATEMENTS_AND_BUTTONS = "LoadNextSetOfStatementsAndButtons";

    [SerializeField]
    private Color correctColor = Color.green;
    [SerializeField]
    private Color wrongColor = Color.red;
    [SerializeField]
    private int numberOfWordsPerSet = 6;
    [SerializeField]
    private float timeBetweenEachSet = 1.5f;
    [SerializeField]
    private string selectWordText = "Select Word";
    [SerializeField]
    private string tapHereText = "Tap Here";
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
    private float animationSpeed = 0.4f;
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

        if (fillInTheBlanks == null && currentFillInTheBlanksGameObject != null) {
            fillInTheBlanks = currentFillInTheBlanksGameObject.GetComponent<Masters_FillInTheBlanks>();
        }
        if (fillInTheBlanks == null) {
            fillInTheBlanks = GetComponentInChildren<Masters_FillInTheBlanks>(true);
        }

        LoadNextSetOfStatementsAndButtons();
        if (retryButton != null) {
            retryButton.onClick.AddListener(OnRetryButtonClicked);
        }
    }

    private int GetTotalWordsToFill() {
        if (fillInTheBlanks != null && fillInTheBlanks.GetBlanksAndWordsArray() != null) {
            int total = 0;
            foreach (var bw in fillInTheBlanks.GetBlanksAndWordsArray()) {
                if (bw.blankButtonArray != null) total += bw.blankButtonArray.Length;
            }
            if (total > 0) return total;
        }
        int sets = fillInTheBlanks != null && fillInTheBlanks.GetBlanksAndWordsArray() != null ? fillInTheBlanks.GetBlanksAndWordsArray().Length : 1;
        return numberOfWordsPerSet * sets;
    }

    private void OnRetryButtonClicked() {
        if (completedRectTransform != null) {
            completedRectTransform.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.OutExpo).OnComplete(() => {
                ResetAndRestart();
            });
        } else {
            ResetAndRestart();
        }
    }

    private void ResetAndRestart() {
        if (completedGameObject != null) completedGameObject.SetActive(false);
        if (completedRectTransform != null) completedRectTransform.localScale = Vector3.one;
        if (fillInTheBlanksRectTransform != null) fillInTheBlanksRectTransform.localScale = Vector3.one;

        if (fillInTheBlanks != null && fillInTheBlanks.GetBlanksAndWordsArray() != null) {
            for (int i = 0; i < fillInTheBlanks.GetBlanksAndWordsArray().Length; i++) {
                if (fillInTheBlanks.GetStatementGameObjectArray() != null && i < fillInTheBlanks.GetStatementGameObjectArray().Length) {
                    fillInTheBlanks.GetStatementGameObjectArray()[i].SetActive(false);
                }
                if (fillInTheBlanks.GetWordsGameObjectArray() != null && i < fillInTheBlanks.GetWordsGameObjectArray().Length) {
                    fillInTheBlanks.GetWordsGameObjectArray()[i].SetActive(false);
                }

                var blanksAndWords = fillInTheBlanks.GetBlanksAndWordsArray()[i];
                if (blanksAndWords.blankButtonArray != null) {
                    foreach (Button blankBtn in blanksAndWords.blankButtonArray) {
                        if (blankBtn != null) {
                            blankBtn.onClick.RemoveAllListeners();
                            blankBtn.interactable = true;
                            if (blankBtn.TryGetComponent(out Masters_FillInTheBlank_Blank blank)) {
                                blank.SetWordToBlank(tapHereText);
                                var tmp = blank.GetComponentInChildren<TextMeshProUGUI>();
                                if (tmp != null) tmp.color = Color.white;
                            }
                        }
                    }
                }

                if (blanksAndWords.wordButtonArray != null) {
                    foreach (Button wordBtn in blanksAndWords.wordButtonArray) {
                        if (wordBtn != null) {
                            wordBtn.onClick.RemoveAllListeners();
                            wordBtn.interactable = true;
                        }
                    }
                }
            }
        }

        currentStatementGameObject = null;
        currentWordsGameObject = null;
        currentSelectedBlank = null;
        numberOfWordsInteracted = 0;
        setIndex = 0;
        correctlyFilled = 0;

        LoadNextSetOfStatementsAndButtons();

        if (correctlyFilledTMP != null) {
            correctlyFilledTMP.text = $"{correctlyFilled}/{GetTotalWordsToFill()}";
        }
    }

    private void LoadNextSetOfStatementsAndButtons() {
        if (fillInTheBlanks == null || fillInTheBlanks.GetStatementGameObjectArray() == null) {
            return;
        }

        int totalSets = fillInTheBlanks.GetStatementGameObjectArray().Length;

        // Dynamic multi-round check: Ends only when ALL sets have been completed!
        if (setIndex >= totalSets) {
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
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

        if (setIndex < statementGameObjectArray.Length) currentStatementGameObject = statementGameObjectArray[setIndex];
        if (setIndex < wordsGameObjectArray.Length) currentWordsGameObject = wordsGameObjectArray[setIndex];
        if (setIndex < blanksAndWordsArray.Length) fillInTheBlanks.SetCurrentBlanksAndWords(blanksAndWordsArray[setIndex]);

        if (fillInTheBlanks.GetCurrentBlanksAndWords().blankButtonArray != null) {
            foreach (Button blankButton in fillInTheBlanks.GetCurrentBlanksAndWords().blankButtonArray) {
                if (blankButton != null) {
                    blankButton.onClick.AddListener(() => {
                        OnBlankButtonClicked(blankButton);
                    });
                }
            }
        }

        if (fillInTheBlanks.GetCurrentBlanksAndWords().wordButtonArray != null) {
            foreach (Button wordButton in fillInTheBlanks.GetCurrentBlanksAndWords().wordButtonArray) {
                if (wordButton != null) {
                    wordButton.onClick.AddListener(() => {
                        OnWordButtonClicked(wordButton);
                    });
                }
            }
        }

        if (currentStatementGameObject != null) currentStatementGameObject.SetActive(true);
        if (currentWordsGameObject != null) currentWordsGameObject.SetActive(true);

        setIndex++;
    }

    private void CompleteScreenAnimation() {
        if (fillInTheBlanksRectTransform != null && completedGameObject != null) {
            fillInTheBlanksRectTransform.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.OutExpo).OnComplete(() => {
                completedGameObject.SetActive(true);
            });
        } else if (completedGameObject != null) {
            completedGameObject.SetActive(true);
        }
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
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                currentSelectedBlank.SetWordAndColorToBlank(wordText, correctColor);
                if (correctlyFilledTMP != null) {
                    correctlyFilledTMP.text = $"{++correctlyFilled}/{GetTotalWordsToFill()}";
                }
            } else {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                currentSelectedBlank.SetWordAndColorToBlank(wordText, wrongColor);
                button.interactable = true;
            }
        } else {
            Debug.Log($"{button} does not have FillInTheBlank_Word component!");
        }

        numberOfWordsInteracted++;
        
        int wordsInCurrentSet = fillInTheBlanks.GetCurrentBlanksAndWords().blankButtonArray != null ? fillInTheBlanks.GetCurrentBlanksAndWords().blankButtonArray.Length : numberOfWordsPerSet;
        if (numberOfWordsInteracted >= wordsInCurrentSet) {
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
            if (topic != Masters_Topic.None) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
