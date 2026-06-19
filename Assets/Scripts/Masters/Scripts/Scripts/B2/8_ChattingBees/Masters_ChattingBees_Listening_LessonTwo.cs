using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Masters_ChattingBees_Listening_LessonTwo : Masters_Lesson {

    private const string SET_SORT_PUZZLE = "SetSortPuzzle";

    public enum SortType {
        Ask,
        State,
        Clarify,
        Add
    }

    [System.Serializable]
    public class SortTypeMap {
        public SortType sortType;
        public int sortId;
    }

    [System.Serializable]
    public class SortPuzzle {
        public string expression;
        public SortType sortType;
        public AudioClip audioClip;
    }

    [SerializeField]
    private Masters_UniversalSortPhraseCard sortPhraseCard;
    [SerializeField]
    private SortTypeMap[] sortTypeMapArray;
    [SerializeField]
    private SortPuzzle[] sortPuzzleArray;
    [SerializeField]
    private Masters_UniversalSortBin[] sortBinArray;
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

        foreach (Masters_UniversalSortBin sortBin in sortBinArray) {
            sortBin.GetButton().onClick.AddListener(() => {
                OnSortBinClicked(sortBin);
            });
        }
    }

    protected override void Start() {
        base.Start();

        SetSortPuzzle();
    }

    private int GetSortIdForType(SortType type) {
        if (sortTypeMapArray != null) {
            foreach (var map in sortTypeMapArray) {
                if (map.sortType == type) {
                    return map.sortId;
                }
            }
        }
        return (int)type; // Fallback
    }

    private void OnSortBinClicked(Masters_UniversalSortBin sortBin) {
        if (!canClick) {
            return;
        }

        int expectedSortId = GetSortIdForType(currentSortPuzzle.sortType);

        if (sortBin.GetSortId() == expectedSortId) {
            // Correct
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            puzzleCountTMP.text = $"{currentSortPuzzleIndex}/{sortPuzzleArray.Length}";
            canClick = false;

            RectTransform sortPhraseCardRectTransform = sortPhraseCard.GetComponent<RectTransform>();
            sortPhraseCard.transform.SetParent(sortBin.GetPhraseTargetPointRectTransform(), true);
            sortPhraseCardRectTransform.DOAnchorPos(Vector2.zero, animationSpeed).SetEase(Ease.InOutSine);
            sortPhraseCardRectTransform.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.InBack).OnComplete(() => {
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

        int sortId = GetSortIdForType(currentSortPuzzle.sortType);
        sortPhraseCard.SetSortIdAndExpression(sortId, currentSortPuzzle.expression);

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
