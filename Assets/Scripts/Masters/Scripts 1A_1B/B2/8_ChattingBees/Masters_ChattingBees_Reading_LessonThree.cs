using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_ChattingBees_Reading_LessonThree : Masters_Lesson {

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

    [Header("Discussion Playback")]
    [SerializeField]
    private AudioClip completedDiscussionAudio;

    [SerializeField]
    private AudioClip[] correctWordAudioClips;

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

            fillInTheBlanksRectTransform.localScale = Vector3.one;

            for (int i = 0; i < fillInTheBlanks.GetBlanksAndWordsArray().Length; i++) {
                
                fillInTheBlanks.GetStatementGameObjectArray()[i].SetActive(false);
                fillInTheBlanks.GetWordsGameObjectArray()[i].SetActive(false);
                
                var blanksAndWords = fillInTheBlanks.GetBlanksAndWordsArray()[i];

                foreach (Button blankBtn in blanksAndWords.blankButtonArray) {
                    blankBtn.onClick.RemoveAllListeners();
                    blankBtn.interactable = true;
                    if (blankBtn.TryGetComponent(out Masters_FillInTheBlank_Blank blank)) {
                        blank.SetWordToBlank(tapHereText); 
                        blank.GetComponentInChildren<TextMeshProUGUI>().color = Color.white;
                    }
                }

                foreach (Button wordBtn in blanksAndWords.wordButtonArray) {
                    wordBtn.onClick.RemoveAllListeners();
                    wordBtn.interactable = true;
                }
            }

            currentStatementGameObject = null;
            currentWordsGameObject = null;
            currentSelectedBlank = null;
            numberOfWordsInteracted = 0;

            setIndex = 0;
            LoadNextSetOfStatementsAndButtons();

            correctlyFilled = 0;
            int totalWordsToFill = numberOfWordsPerSet * fillInTheBlanks.GetBlanksAndWordsArray().Length;
            correctlyFilledTMP.text = $"{correctlyFilled}/{totalWordsToFill}";
        });
    }

    private void LoadNextSetOfStatementsAndButtons() {
        if (setIndex == 1) { // Note: Currently ends at index 1? Wait, if 3 rounds, shouldn't this be 3?
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
        if (completedDiscussionAudio != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(completedDiscussionAudio);
        }

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
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

                // Play per-round dialogue audio mapped to the specific blank index
                int blankIndex = -1;
                var blanks = fillInTheBlanks.GetBlanksAndWordsArray()[setIndex - 1].blankButtonArray;
                for (int i = 0; i < blanks.Length; i++) {
                    if (blanks[i].GetComponent<Masters_FillInTheBlank_Blank>() == currentSelectedBlank) {
                        blankIndex = i;
                        break;
                    }
                }

                if (blankIndex != -1 && correctWordAudioClips != null && blankIndex < correctWordAudioClips.Length && correctWordAudioClips[blankIndex] != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(correctWordAudioClips[blankIndex]);
                }

                currentSelectedBlank.SetWordAndColorToBlank(wordText, correctColor);
                int totalWordsToFill = numberOfWordsPerSet * fillInTheBlanks.GetBlanksAndWordsArray().Length;
                correctlyFilledTMP.text = $"{++correctlyFilled}/{totalWordsToFill}";
            } else {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                currentSelectedBlank.SetWordAndColorToBlank(wordText, wrongColor);
                button.interactable = true;
            }
        } else {
            Debug.Log($"{button} does not have FillInTheBlank_Word component!");
        }

        numberOfWordsInteracted++;
        if (numberOfWordsInteracted == numberOfWordsPerSet) {
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
