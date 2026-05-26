using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Masters_IDoAndIMake_Game_LessonTwo : Masters_Lesson {


    private const string SET_SORT_PUZZLE = "SetSortPuzzle";

    public enum SortType {

        Do,
        Make

    }


    [System.Serializable]
    public class SortPuzzle {

        public string expression;
        public SortType sortType;
        public AudioClip audioClip;

    }



    [SerializeField]
    private Masters_SortPhraseCard_IDoAndIMake sortPhraseCard;
    [SerializeField]
    private SortPuzzle[] sortPuzzleArray;
    [SerializeField]
    private Masters_SortBin_IDoAndIMake[] sortBinArray;
    [SerializeField]
    private RectTransform sortPhraseRestPointRectTransform;
    [SerializeField]
    private float timeBetweenSortPuzzle, animationSpeed;
    [SerializeField]
    private TextMeshProUGUI puzzleCountTMP;


    private SortPuzzle currentSortPuzzle;
    private int currentSortPuzzleIndex;
    private bool canClick;


    protected override void Awake() {
        base.Awake();

        foreach (Masters_SortBin_IDoAndIMake sortBin in sortBinArray) {
            sortBin.GetButton().onClick.AddListener(() => {
                OnSortBinClicked(sortBin);
            });
        }
    }

    protected override void Start() {
        base.Start();

        SetSortPuzzle();
    }

    private void OnSortBinClicked(Masters_SortBin_IDoAndIMake sortBin) {
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
            sortPhraseCardRectTransform.DOAnchorPos(Vector2.zero, animationSpeed).SetEase(Ease.InOutSine);
            sortPhraseCardRectTransform.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.InBack).OnComplete(() => {
                //Invoke(SET_SORT_PUZZLE, timeBetweenSortPuzzle);
                sortPhraseCard.gameObject.SetActive(false);
            });

            Masters_AudioManager.Instance.PlayVoiceOver(currentSortPuzzle.audioClip);
            StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(SetSortPuzzle));
        } else {
            // Wrong
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
    }

    private void SetSortPuzzle() {
        if (currentSortPuzzle != null) {
            sortPhraseCard.GetButton().onClick.RemoveListener(() => {
                OnSortPhraseClicked(currentSortPuzzle.audioClip);
            });
        }

        if (currentSortPuzzleIndex == sortPuzzleArray.Length) {
            // Over
            nextButton.interactable = true;
            NextButtonAnimation();
            return;
        }

        currentSortPuzzle = sortPuzzleArray[currentSortPuzzleIndex++];
        sortPhraseCard.GetButton().onClick.AddListener(() => {
            OnSortPhraseClicked(currentSortPuzzle.audioClip);
        });

        sortPhraseCard.SetSortTypeAndExpression(currentSortPuzzle.sortType, currentSortPuzzle.expression);


        RectTransform sortPhraseCardRectTransform = sortPhraseCard.GetComponent<RectTransform>();
        sortPhraseCardRectTransform.transform.SetParent(sortPhraseRestPointRectTransform, true);
        sortPhraseCardRectTransform.anchoredPosition = Vector3.zero;
        sortPhraseCard.gameObject.SetActive(true);
        canClick = true;
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
