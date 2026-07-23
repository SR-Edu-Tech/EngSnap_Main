using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum Masters_Unit2_SortCategory {
    Polite = 0,
    Rude = 1
}

/// <summary>
/// Core Listening 2 controller for Unit 2: Clear Confusion (Book 2A).
/// Uses Masters_UniversalSortPhraseCard and Masters_UniversalSortBin to sort heard lines into `POLITE` vs `RUDE`.
/// When a RUDE tile lands correctly in the RUDE bin, ARIA voices the polite verbatim version to contrast.
/// Leverages the clean Unit 1 Listening L2 sorting bin architecture (`1_PolishedCommunication`).
/// </summary>
public class Masters_ClearConfusion_Listening_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class SortTileData {
        public string expressionText;
        public AudioClip expressionAudio;
        public Masters_Unit2_SortCategory category;
        [Tooltip("If this tile is Rude, provide the polite verbatim audio so ARIA can offer the polite version upon correct sort.")]
        public AudioClip politeVerbatimAudio;
    }

    [Header("Listening L2 Data")]
    [SerializeField] private SortTileData[] sortTiles;
    [SerializeField] private Masters_UniversalSortBin[] sortBinArray;
    [SerializeField] private Masters_UniversalSortPhraseCard sortPhraseCard;
    [SerializeField] private RectTransform sortPhraseRestPointRectTransform;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private float animationSpeed = 0.4f;
    [SerializeField] private int passThreshold = 6;

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
            for (int i = 0; i < sortBinArray.Length; i++) {
                if (sortBinArray[i] != null) {
                    if (i == 0) {
                        sortBinArray[i].gameObject.SetActive(true);
                        sortBinArray[i].SetSortId(0); // POLITE = 0
                        SetBinLabelText(sortBinArray[i], "POLITE");
                    } else if (i == 1) {
                        sortBinArray[i].gameObject.SetActive(true);
                        sortBinArray[i].SetSortId(1); // RUDE = 1
                        SetBinLabelText(sortBinArray[i], "RUDE");
                    } else {
                        sortBinArray[i].gameObject.SetActive(false);
                    }

                    if (i < 2) {
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
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
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
            int sortId = (int)tile.category; // POLITE = 0, RUDE = 1
            sortPhraseCard.SetSortIdAndExpression(sortId, tile.expressionText);

            Button phraseBtn = sortPhraseCard.GetButton();
            if (phraseBtn != null) {
                phraseBtn.onClick.RemoveAllListeners();
                if (tile.expressionAudio != null) {
                    phraseBtn.onClick.AddListener(() => {
                        if (Masters_AudioManager.Instance != null) {
                            Masters_AudioManager.Instance.PlayVoiceOver(tile.expressionAudio);
                        }
                    });
                }
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

            if (tile.expressionAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(tile.expressionAudio);
            }
        } else {
            canClick = true;
        }
    }

    private void OnSortBinClicked(Masters_UniversalSortBin sortBin) {
        if (!canClick || sortBin == null || sortTiles == null || currentTileIndex >= sortTiles.Length) return;

        SortTileData tile = sortTiles[currentTileIndex];
        if (tile == null || sortPhraseCard == null || !sortPhraseCard.gameObject.activeInHierarchy) return;

        int expectedSortId = (int)tile.category; // POLITE = 0, RUDE = 1

        if (sortBin.GetSortId() == expectedSortId) {
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
                    if (expectedSortId == 1 && tile.politeVerbatimAudio != null && Masters_AudioManager.Instance != null) {
                        StartCoroutine(PlayPoliteAndLoadNext(tile.politeVerbatimAudio));
                    } else {
                        StartCoroutine(NextTileRoutine());
                    }
                });
            } else {
                StartCoroutine(NextTileRoutine());
            }
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            sortPhraseCard.transform.DOKill(true);
            sortPhraseCard.transform.DOShakePosition(0.4f, new Vector3(15f, 0, 0));
        }
    }

    private IEnumerator PlayPoliteAndLoadNext(AudioClip politeClip) {
        if (Masters_AudioManager.Instance != null && politeClip != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(politeClip);
            yield return new WaitForSeconds(politeClip.length + 0.3f);
        } else {
            yield return new WaitForSeconds(0.3f);
        }
        LoadTile(currentTileIndex + 1);
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
        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
