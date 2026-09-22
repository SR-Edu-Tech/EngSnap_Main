using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Reading R02 controller for Unit 10: Stop Apologizing.
/// Adapted from Unit 1 Reading L2, with labels fixed for Unit 10 bins.
/// </summary>
public class Masters_StopApologizingStartThanking_Reading_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class SortTileData {
        public string expressionText;
        public int categorySortId; // 0: MAKE A COMPLAINT, 1: RECEIVE, 2: RESPOND
        public AudioClip expressionAudio;
        public AudioClip slowAudio;
    }

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Reading R02 Data")]
    [SerializeField] private SortTileData[] sortTiles;
    [SerializeField] private Masters_UniversalSortBin[] sortBinArray;
    [SerializeField] private Masters_UniversalSortPhraseCard sortPhraseCard;
    [SerializeField] private RectTransform sortPhraseRestPointRectTransform;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private float animationSpeed = 0.4f;
    [SerializeField] private int passThreshold = 9;

    private int currentTileIndex = 0;
    private int correctSorts = 0;
    private bool canClick = false;

    protected override void Awake() {
        base.Awake();

        if (sortBinArray == null || sortBinArray.Length == 0) {
            AutoFindSortBins();
        }
        if (sortPhraseCard == null) {
            sortPhraseCard = GetComponentInChildren<Masters_UniversalSortPhraseCard>(true);
        }

        ConfigureSortBins();
    }

    protected override void Start() {
        base.Start();

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        currentTileIndex = 0;
        correctSorts = 0;
        StartCoroutine(InitializeLessonRoutine());
    }

    private void AutoFindSortBins() {
        Masters_UniversalSortBin[] allBins = GetComponentsInChildren<Masters_UniversalSortBin>(true);
        if (allBins != null && allBins.Length > 0) {
            sortBinArray = allBins;
        }
    }

    private void ConfigureSortBins() {
        if (sortBinArray != null) {
            // UNIT 10 SPECIFIC LABELS!
            string[] labels = new string[] { "MAKE A COMPLAINT", "RECEIVE", "RESPOND" };
            for (int i = 0; i < sortBinArray.Length; i++) {
                if (sortBinArray[i] != null) {
                    if (i < labels.Length) {
                        sortBinArray[i].gameObject.SetActive(true);
                        sortBinArray[i].SetSortId(i);
                        SetBinLabelText(sortBinArray[i], labels[i]);
                    } else {
                        sortBinArray[i].gameObject.SetActive(false);
                    }

                    if (i < labels.Length) {
                        Button binBtn = sortBinArray[i].GetButton();
                        if (binBtn != null) {
                            Masters_UniversalSortBin currentBin = sortBinArray[i];
                            binBtn.onClick.RemoveAllListeners();
                            binBtn.onClick.AddListener(() => OnSortBinClicked(currentBin));
                        }
                    }
                }
            }
        }
    }

    private void SetBinLabelText(Masters_UniversalSortBin bin, string text) {
        if (bin == null) return;
        TMP_Text tmp = bin.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) {
            tmp.text = text;
        } else {
            Text legacy = bin.GetComponentInChildren<Text>(true);
            if (legacy != null) legacy.text = text;
        }
    }

    private IEnumerator InitializeLessonRoutine() {
        if (sortPhraseCard != null) sortPhraseCard.gameObject.SetActive(false);

        if (Masters_AudioManager.Instance != null) {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd((System.Action)null);
        } else {
            yield return new WaitForSeconds(1f);
        }

        LoadTile(0);
    }

    private void LoadTile(int index) {
        if (sortTiles == null || index >= sortTiles.Length) {
            OnAllTilesCompleted();
            return;
        }

        currentTileIndex = index;
        canClick = false;

        if (progressTMP != null) {
            progressTMP.text = $"{currentTileIndex + 1}/{sortTiles.Length}";
        }

        SortTileData tile = sortTiles[currentTileIndex];
        if (tile == null) return;

        if (sortPhraseCard != null) {
            int sortId = tile.categorySortId;
            sortPhraseCard.SetSortIdAndExpression(sortId, tile.expressionText);

            Button phraseBtn = sortPhraseCard.GetButton();
            if (phraseBtn != null) {
                phraseBtn.onClick.RemoveAllListeners(); // Optional: remove click audio for reading focus
            }

            RectTransform cardRect = sortPhraseCard.GetComponent<RectTransform>();
            if (sortPhraseRestPointRectTransform != null && cardRect != null) {
                cardRect.SetParent(sortPhraseRestPointRectTransform, true);
                cardRect.anchoredPosition = Vector2.zero;
            } else if (cardRect != null && sortPhraseCard.transform.parent != null) {
                cardRect.anchoredPosition = new Vector2(0, 150f);
            }

            sortPhraseCard.gameObject.SetActive(true);
            sortPhraseCard.transform.DOKill();
            sortPhraseCard.transform.localScale = Vector3.zero;
            sortPhraseCard.transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack).OnComplete(() => {
                canClick = true;
            });
        } else {
            canClick = true;
        }
    }

    private void OnSortBinClicked(Masters_UniversalSortBin clickedBin) {
        if (!canClick || clickedBin == null || sortPhraseCard == null) return;

        canClick = false;

        if (sortPhraseCard.GetSortId() == clickedBin.GetSortId()) {
            StartCoroutine(CorrectSortRoutine(clickedBin));
        } else {
            StartCoroutine(IncorrectSortRoutine(clickedBin));
        }
    }

    private IEnumerator CorrectSortRoutine(Masters_UniversalSortBin correctBin) {
        correctSorts++;
        
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }

        RectTransform cardRect = sortPhraseCard.GetComponent<RectTransform>();
        if (cardRect != null) {
            cardRect.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.InBack);
        }

        yield return new WaitForSeconds(animationSpeed + 0.1f);
        sortPhraseCard.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.2f);
        LoadTile(currentTileIndex + 1);
    }

    private IEnumerator IncorrectSortRoutine(Masters_UniversalSortBin incorrectBin) {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        yield return new WaitForSeconds(0.6f);
        canClick = true;
    }

    private void OnAllTilesCompleted() {
        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            NextButtonAnimation();
        }
    }

    protected override void OnNextButtonClicked() {
        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}
