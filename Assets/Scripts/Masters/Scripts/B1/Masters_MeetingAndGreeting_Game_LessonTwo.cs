using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Masters_MeetingAndGreeting_Game_LessonTwo : Masters_Lesson {


    private const string SET_SORT_PUZZLE = "SetSortPuzzle";

    public enum SortType {

        Greeting,
        Response,
        Farewell

    }


    [System.Serializable]
    public class SortPuzzle {

        public string expression;
        public SortType sortType;
        public AudioClip audioClip;

    }



    [SerializeField]
    private Masters_SortPhraseCard sortPhraseCard;
    [SerializeField]
    private SortPuzzle[] sortPuzzleArray;
    [SerializeField]
    private Masters_SortBin[] sortBinArray;
    [SerializeField]
    private RectTransform sortPhraseRestPointRectTransform;
    [SerializeField]
    private RectTransform sortPuzzleOutPointRectTransform;
    [SerializeField]
    private float timeBetweenSortPuzzle;
    [SerializeField]
    private TextMeshProUGUI puzzleCountTMP;


    private SortPuzzle currentSortPuzzle;
    private int currentSortPuzzleIndex;
    private bool canClick;


    protected override void Awake() {
        base.Awake();

        foreach(Masters_SortBin sortBin in sortBinArray) {
            sortBin.GetButton().onClick.AddListener(() => {
                OnSortBinClicked(sortBin);
            });
        }
    }

    private void Start() {
        SetSortPuzzle();
    }

    private void OnSortBinClicked(Masters_SortBin sortBin) {
        if (!canClick) {
            return;
        }

        if (sortBin.GetSortType() == currentSortPuzzle.sortType) {
            // Correct
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            puzzleCountTMP.text = $"{currentSortPuzzleIndex}/9";
            canClick = false;

            RectTransform sortPhraseCardRectTransform = sortPhraseCard.GetComponent<RectTransform>();
            sortPhraseCard.transform.SetParent(sortBin.GetPhraseTargetPointRectTransform(), true);
            sortPhraseCardRectTransform.DOAnchorPos(Vector2.zero, 1f).SetEase(Ease.OutExpo);
            sortPhraseCardRectTransform.DOScale(Vector3.zero, 0.75f).SetEase(Ease.OutSine).OnComplete(() => {
                Invoke(SET_SORT_PUZZLE, timeBetweenSortPuzzle);
            });
        } else {
            // Wrong
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
    }

    private void SetSortPuzzle() {
        if(currentSortPuzzle != null) {
            sortPhraseCard.GetButton().onClick.RemoveListener(() => {
                OnSortPhraseClicked(currentSortPuzzle.audioClip);
            });
        }

        if(currentSortPuzzleIndex == sortPuzzleArray.Length) {
            // Over
            nextButton.interactable = true;
            return;
        }

        currentSortPuzzle = sortPuzzleArray[currentSortPuzzleIndex++];
        sortPhraseCard.GetButton().onClick.AddListener(() => {
            OnSortPhraseClicked(currentSortPuzzle.audioClip);
        });

        sortPhraseCard.SetSortTypeAndExpression(currentSortPuzzle.sortType, currentSortPuzzle.expression);
        sortPhraseCard.transform.SetParent(sortPhraseRestPointRectTransform, false);
        sortPhraseCard.gameObject.SetActive(true);
        
        RectTransform sortPhraseCardRectTransform = sortPhraseCard.GetComponent<RectTransform>();
        sortPhraseCardRectTransform.anchoredPosition = sortPuzzleOutPointRectTransform.anchoredPosition;
        sortPhraseCardRectTransform.localScale = Vector3.zero;

        sortPhraseCardRectTransform.DOMoveX(sortPhraseRestPointRectTransform.anchoredPosition.x, 1f).SetEase(Ease.OutExpo);
        sortPhraseCardRectTransform.DOScale(Vector3.one, 0.75f).SetEase(Ease.OutSine).OnComplete(() => {
            canClick = true;
        });
    }

    private void OnSortPhraseClicked(AudioClip audioClip) {
        Masters_AudioManager.Instance.PlayVoiceOver(audioClip);
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
