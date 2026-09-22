using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Reading 2 controller for Unit 1: Boost Someone Up! (Sort the Cheer).
/// Adapted from the Listening L1 3-bin sort, but with audio playback removed to focus on reading.
/// Uses Masters_UniversalSortPhraseCard and Masters_UniversalSortBin.
/// </summary>
public class Masters_BoostSomeoneUp_Reading_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class SortTileData {
        public string expressionText;
        public int categorySortId; // 0: COMPLIMENT, 1: ENCOURAGEMENT, 2: TEAM
    }

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Reading L2 Data")]
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
            string[] labels = new string[] { "COMPLIMENT", "ENCOURAGEMENT", "TEAM" };
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
                phraseBtn.onClick.RemoveAllListeners(); // Remove audio playback on click for reading focus
            }

            RectTransform cardRect = sortPhraseCard.GetComponent<RectTransform>();
            if (sortPhraseRestPointRectTransform != null && cardRect != null) {
                cardRect.SetParent(sortPhraseRestPointRectTransform, true);
                cardRect.anchoredPosition = Vector2.zero;
            } else if (cardRect != null && sortPhraseCard.transform.parent != null) {
                cardRect.anchoredPosition = new Vector2(0, 150f);
            }

            sortPhraseCard.transform.localScale = Vector3.zero;
            sortPhraseCard.gameObject.SetActive(true);
            sortPhraseCard.transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack).OnComplete(() => {
                canClick = true;
            });
        } else {
            canClick = true;
        }
    }

    private void OnSortBinClicked(Masters_UniversalSortBin sortBin) {
        if (!canClick || sortBin == null || sortTiles == null || currentTileIndex >= sortTiles.Length) return;

        SortTileData tile = sortTiles[currentTileIndex];
        if (tile == null || sortPhraseCard == null || !sortPhraseCard.gameObject.activeInHierarchy) return;

        int expectedSortId = tile.categorySortId;

        if (sortBin.GetSortId() == expectedSortId) {
            // Correct
            canClick = false;
            correctSorts++;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            RectTransform sortPhraseCardRectTransform = sortPhraseCard.GetComponent<RectTransform>();
            if (sortPhraseCardRectTransform != null) {
                sortPhraseCard.transform.SetParent(sortBin.GetPhraseTargetPointRectTransform(), true);
                sortPhraseCardRectTransform.DOAnchorPos(Vector2.zero, animationSpeed).SetEase(Ease.InOutSine);
                sortPhraseCardRectTransform.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.InBack).OnComplete(() => {
                    sortPhraseCard.gameObject.SetActive(false);
                    StartCoroutine(NextTileRoutine());
                });
            } else {
                StartCoroutine(NextTileRoutine());
            }
        } else {
            // Wrong
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            sortPhraseCard.transform.DOKill(true);
            sortPhraseCard.transform.DOShakePosition(0.4f, new Vector3(15f, 0, 0));
        }
    }

    private IEnumerator NextTileRoutine() {
        yield return new WaitForSeconds(0.3f);
        LoadTile(currentTileIndex + 1);
    }

    private void OnAllTilesCompleted() {
        if (correctSorts >= passThreshold) {
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                NextButtonAnimation();
            }
        } else {
            currentTileIndex = 0;
            correctSorts = 0;
            LoadTile(0);
        }
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) return;
        
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }
}
