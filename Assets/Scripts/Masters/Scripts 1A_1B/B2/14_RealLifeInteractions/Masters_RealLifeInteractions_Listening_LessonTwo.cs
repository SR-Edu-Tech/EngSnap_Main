using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;

public class Masters_RealLifeInteractions_Listening_LessonTwo : Masters_Lesson {

    private const string SET_SORT_PUZZLE = "SetSortPuzzle";

    public enum SortType {
        OrderingFood = 0,
        SchoolTime = 1,
        AtTheClinic = 2
    }

    [System.Serializable]
    public class SortPuzzle {
        public string expression;
        public SortType sortType;
        public AudioClip audioClip;
    }

    [SerializeField]
    private Masters_SortPhraseCard_RealLifeInteractions sortPhraseCard;
    [SerializeField]
    private SortPuzzle[] sortPuzzleArray;
    [SerializeField]
    private Masters_SortBin_RealLifeInteractions[] sortBinArray;
    [SerializeField]
    private RectTransform sortPhraseRestPointRectTransform;
    [SerializeField]
    private float timeBetweenSortPuzzle = 0.5f, animationSpeed = 0.25f;
    [SerializeField]
    private TextMeshProUGUI puzzleCountTMP;

    private SortPuzzle currentSortPuzzle;
    private int currentSortPuzzleIndex;
    private bool canClick;

    protected override void Awake() {
        base.Awake();

        if (sortBinArray != null) {
            foreach (Masters_SortBin_RealLifeInteractions sortBin in sortBinArray) {
                if (sortBin != null && sortBin.GetButton() != null) {
                    sortBin.GetButton().onClick.AddListener(() => {
                        OnSortBinClicked(sortBin);
                    });
                }
            }
        }
    }

    protected override void Start() {
        base.Start();
        SetSortPuzzle();
    }

    private void OnSortBinClicked(Masters_SortBin_RealLifeInteractions sortBin) {
        if (!canClick || currentSortPuzzle == null) {
            return;
        }

        if (sortBin.GetSortType() == currentSortPuzzle.sortType) {
            // Correct
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            if (puzzleCountTMP != null) {
                puzzleCountTMP.text = $"{currentSortPuzzleIndex}/{sortPuzzleArray.Length}";
            }
            canClick = false;

            if (sortPhraseCard != null && sortBin.GetPhraseTargetPointRectTransform() != null) {
                RectTransform sortPhraseCardRectTransform = sortPhraseCard.GetComponent<RectTransform>();
                sortPhraseCard.transform.SetParent(sortBin.GetPhraseTargetPointRectTransform(), true);
                sortPhraseCardRectTransform.DOAnchorPos(Vector2.zero, animationSpeed).SetEase(Ease.InOutSine);
                sortPhraseCardRectTransform.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.InBack).OnComplete(() => {
                    sortPhraseCard.gameObject.SetActive(false);
                });
            } else if (sortPhraseCard != null) {
                sortPhraseCard.gameObject.SetActive(false);
            }

            if (currentSortPuzzle.audioClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(currentSortPuzzle.audioClip);
                StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(SetSortPuzzle));
            } else {
                Invoke(SET_SORT_PUZZLE, timeBetweenSortPuzzle);
            }
        } else {
            // Wrong
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            if (sortPhraseCard != null) {
                sortPhraseCard.GetComponent<RectTransform>().DOShakeAnchorPos(0.5f, 20f);
            }
        }
    }

    private void SetSortPuzzle() {
        if (currentSortPuzzle != null && sortPhraseCard != null && sortPhraseCard.GetButton() != null) {
            sortPhraseCard.GetButton().onClick.RemoveAllListeners();
        }

        if (sortPuzzleArray == null || currentSortPuzzleIndex >= sortPuzzleArray.Length) {
            if (nextButton != null) {
                nextButton.interactable = true;
            }
            NextButtonAnimation();
            return;
        }

        currentSortPuzzle = sortPuzzleArray[currentSortPuzzleIndex++];

        if (sortPhraseCard != null) {
            if (sortPhraseCard.GetButton() != null) {
                sortPhraseCard.GetButton().onClick.AddListener(() => {
                    OnSortPhraseClicked(currentSortPuzzle.audioClip);
                });
            }

            if (sortPhraseRestPointRectTransform != null) {
                RectTransform sortPhraseCardRectTransform = sortPhraseCard.GetComponent<RectTransform>();
                sortPhraseCardRectTransform.transform.SetParent(sortPhraseRestPointRectTransform, true);
                sortPhraseCardRectTransform.anchoredPosition = Vector3.zero;
            }
            sortPhraseCard.transform.localScale = Vector3.one;
            sortPhraseCard.SetSortTypeAndExpression(currentSortPuzzle.sortType, currentSortPuzzle.expression);
            sortPhraseCard.gameObject.SetActive(true);
        }

        if (puzzleCountTMP != null) {
            puzzleCountTMP.text = $"{currentSortPuzzleIndex}/{sortPuzzleArray.Length}";
        }
        canClick = true;
    }

    private void OnSortPhraseClicked(AudioClip audioClip) {
        if (audioClip != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(audioClip);
        }
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
