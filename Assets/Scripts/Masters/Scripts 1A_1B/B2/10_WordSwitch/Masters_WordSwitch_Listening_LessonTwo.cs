using DG.Tweening;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>
/// Listening Lesson 2 for Unit 10 Word Switch.
/// Tap-to-Bin sorting puzzle across 11 master word banks.
/// </summary>
public class Masters_WordSwitch_Listening_LessonTwo : Masters_Lesson {

    private const string SET_SORT_PUZZLE = "SetSortPuzzle";

    public enum SortType {
        Saw,
        Said,
        Nice,
        Little,
        Funny,
        Good,
        Pretty,
        Happy,
        Sad,
        Smart,
        Laughed
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
    private float animationSpeed = 0.4f;
    [SerializeField]
    private TextMeshProUGUI puzzleCountTMP;

    private SortPuzzle currentSortPuzzle;
    private int currentSortPuzzleIndex;
    private bool canClick;

    protected override void Awake() {
        base.Awake();

        if (sortBinArray != null) {
            foreach (Masters_UniversalSortBin sortBin in sortBinArray) {
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

        if (sortPuzzleArray != null && sortPuzzleArray.Length > 0) {
            sortPuzzleArray = sortPuzzleArray.OrderBy(x => System.Guid.NewGuid()).ToArray();
        }

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
        return (int)type; // Fallback to enum int
    }

    private void OnSortBinClicked(Masters_UniversalSortBin sortBin) {
        if (!canClick || sortBin == null || currentSortPuzzle == null) {
            return;
        }

        int expectedSortId = GetSortIdForType(currentSortPuzzle.sortType);

        if (sortBin.GetSortId() == expectedSortId) {
            // Correct
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            if (puzzleCountTMP != null && sortPuzzleArray != null) {
                puzzleCountTMP.text = $"{currentSortPuzzleIndex}/{sortPuzzleArray.Length}";
            }
            canClick = false;

            if (sortPhraseCard != null) {
                RectTransform cardRT = sortPhraseCard.GetComponent<RectTransform>();
                if (sortBin.GetPhraseTargetPointRectTransform() != null) {
                    sortPhraseCard.transform.SetParent(sortBin.GetPhraseTargetPointRectTransform(), true);
                }
                cardRT.DOAnchorPos(Vector2.zero, animationSpeed).SetEase(Ease.InOutSine);
                cardRT.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.InBack).OnComplete(() => {
                    sortPhraseCard.gameObject.SetActive(false);
                });
            }

            Masters_AudioManager.Instance.PlayVoiceOver(currentSortPuzzle.audioClip);
            StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(SetSortPuzzle));
        } else {
            // Wrong
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            sortBin.transform.DOShakePosition(0.3f, 6f);
        }
    }

    private void SetSortPuzzle() {
        if (currentSortPuzzle != null && sortPhraseCard != null && sortPhraseCard.GetButton() != null) {
            sortPhraseCard.GetButton().onClick.RemoveAllListeners();
        }

        if (sortPuzzleArray == null || currentSortPuzzleIndex >= sortPuzzleArray.Length) {
            // Level Completed
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            return;
        }

        currentSortPuzzle = sortPuzzleArray[currentSortPuzzleIndex++];
        if (sortPhraseCard != null) {
            if (sortPhraseCard.GetButton() != null) {
                sortPhraseCard.GetButton().onClick.AddListener(() => {
                    OnSortPhraseClicked(currentSortPuzzle.audioClip);
                });
            }

            int sortId = GetSortIdForType(currentSortPuzzle.sortType);
            sortPhraseCard.SetSortIdAndExpression(sortId, currentSortPuzzle.expression);

            RectTransform cardRT = sortPhraseCard.GetComponent<RectTransform>();
            if (sortPhraseRestPointRectTransform != null) {
                cardRT.transform.SetParent(sortPhraseRestPointRectTransform, true);
            }
            cardRT.anchoredPosition = Vector3.zero;
            cardRT.localScale = Vector3.one;
            sortPhraseCard.gameObject.SetActive(true);
        }

        if (puzzleCountTMP != null && sortPuzzleArray != null) {
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
        if (topic != Masters_Topic.None) {
            Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
        }
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}
