using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Listening 2 controller for Unit 3: Beyond the Horizon (Book 2A).
/// Uses base Listening 2 sort bins with THREE bins labelled 1, 2, 3 (Step 1, Step 2, Step 3).
/// Spawns 3 jumbled phrase cards SIMULTANEOUSLY inside a dedicated container (`PhraseCardsContainer`).
/// Tapping a card or directly tapping a bin sorts the card into `bin.GetPhraseTargetPointRectTransform()` using exact `DOAnchorPos(Vector2.zero)`.
/// </summary>
public class Masters_BeyondTheHorizon_Listening_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class SortStepCardData {
        public string expressionText;
        public AudioClip expressionAudio;
        public int targetStepId; // 0 = Step 1 (Bin 1), 1 = Step 2 (Bin 2), 2 = Step 3 (Bin 3)
    }

    [System.Serializable]
    public class RouteRoundData {
        public SortStepCardData[] stepCards; // Exactly 3 cards per route, in jumbled order
    }

    [Header("Listening L2 Data")]
    [SerializeField] private RouteRoundData[] routes;
    [SerializeField] private Masters_UniversalSortBin[] sortBinArray;
    [SerializeField] private Masters_UniversalSortPhraseCard sortPhraseCard; // Base card template
    [SerializeField] private RectTransform sortPhraseRestPointRectTransform;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private float animationSpeed = 0.4f;

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private List<Masters_UniversalSortPhraseCard> phraseCardsPool = new List<Masters_UniversalSortPhraseCard>();
    private RectTransform phraseCardsContainerRT = null;
    private Masters_UniversalSortPhraseCard selectedCard = null;
    private int currentRouteIndex = 0;
    private int cardsRemainingInRoute = 0;
    private bool isAnimating = false;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Listening;

        if (sortBinArray == null || sortBinArray.Length == 0) {
            AutoFindSortBins();
        }

        if (sortPhraseCard == null) {
            sortPhraseCard = GetComponentInChildren<Masters_UniversalSortPhraseCard>(true);
        }

        ConfigureSortBins();
        EnsurePhraseCardsPool();
    }

    protected override void Start() {
        base.Start();

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        currentRouteIndex = 0;
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
                        sortBinArray[i].SetSortId(0);
                        SetBinLabelText(sortBinArray[i], "1");
                    } else if (i == 1) {
                        sortBinArray[i].gameObject.SetActive(true);
                        sortBinArray[i].SetSortId(1);
                        SetBinLabelText(sortBinArray[i], "2");
                    } else if (i == 2) {
                        sortBinArray[i].gameObject.SetActive(true);
                        sortBinArray[i].SetSortId(2);
                        SetBinLabelText(sortBinArray[i], "3");
                    } else {
                        sortBinArray[i].gameObject.SetActive(false);
                    }

                    if (i < 3) {
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

    private void EnsurePhraseCardsPool() {
        phraseCardsPool.Clear();

        if (sortPhraseCard != null) {
            Transform originalParent = sortPhraseCard.transform.parent;

            if (phraseCardsContainerRT == null) {
                GameObject containerGO = new GameObject("PhraseCardsContainer", typeof(RectTransform));
                phraseCardsContainerRT = containerGO.GetComponent<RectTransform>();
                phraseCardsContainerRT.SetParent(originalParent, false);

                RectTransform baseRT = sortPhraseRestPointRectTransform != null ? sortPhraseRestPointRectTransform : sortPhraseCard.GetComponent<RectTransform>();
                if (baseRT != null) {
                    phraseCardsContainerRT.anchorMin = baseRT.anchorMin;
                    phraseCardsContainerRT.anchorMax = baseRT.anchorMax;
                    phraseCardsContainerRT.pivot = baseRT.pivot;
                    phraseCardsContainerRT.anchoredPosition = baseRT.anchoredPosition;
                    phraseCardsContainerRT.sizeDelta = new Vector2(baseRT.sizeDelta.x > 100 ? baseRT.sizeDelta.x : 550f, 260f);
                }

                VerticalLayoutGroup vlg = containerGO.AddComponent<VerticalLayoutGroup>();
                vlg.spacing = 14f;
                vlg.childAlignment = TextAnchor.MiddleCenter;
                vlg.childControlHeight = false;
                vlg.childControlWidth = false;
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = false;
            }

            sortPhraseCard.transform.SetParent(phraseCardsContainerRT, false);
            phraseCardsPool.Add(sortPhraseCard);

            while (phraseCardsPool.Count < 3) {
                Masters_UniversalSortPhraseCard newCard = Instantiate(sortPhraseCard, phraseCardsContainerRT);
                newCard.name = $"SortPhraseCard_{phraseCardsPool.Count + 1}";
                phraseCardsPool.Add(newCard);
            }
        }
    }

    private IEnumerator InitializeLessonRoutine() {
        foreach (var card in phraseCardsPool) {
            if (card != null) card.gameObject.SetActive(false);
        }

        if (Masters_AudioManager.Instance != null) {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(1.5f);
        }

        LoadRoute(0);
    }

    private void LoadRoute(int routeIdx) {
        if (routes == null || routeIdx >= routes.Length) {
            OnAllRoutesCompleted();
            return;
        }

        currentRouteIndex = routeIdx;
        selectedCard = null;
        isAnimating = false;

        if (progressTMP != null) {
            progressTMP.text = $"{currentRouteIndex + 1}/{routes.Length}";
        }

        RouteRoundData route = routes[currentRouteIndex];
        if (route == null || route.stepCards == null || route.stepCards.Length == 0) {
            StartCoroutine(AdvanceToNextRouteAfterDelay(1.0f));
            return;
        }

        int countToSpawn = Mathf.Min(route.stepCards.Length, phraseCardsPool.Count);
        cardsRemainingInRoute = countToSpawn;

        for (int i = 0; i < phraseCardsPool.Count; i++) {
            Masters_UniversalSortPhraseCard card = phraseCardsPool[i];
            if (card == null) continue;

            if (i < countToSpawn) {
                SortStepCardData cardData = route.stepCards[i];
                card.gameObject.SetActive(true);
                card.transform.DOKill();
                card.transform.SetParent(phraseCardsContainerRT, false);
                card.transform.localScale = Vector3.one;

                LayoutElement le = card.GetComponent<LayoutElement>();
                if (le != null) le.ignoreLayout = false;

                card.SetSortIdAndExpression(cardData.targetStepId, cardData.expressionText);

                Button btn = card.GetButton();
                if (btn != null) {
                    btn.interactable = true;
                    btn.onClick.RemoveAllListeners();
                    Masters_UniversalSortPhraseCard currentCardRef = card;
                    SortStepCardData currentDataRef = cardData;
                    btn.onClick.AddListener(() => OnPhraseCardClicked(currentCardRef, currentDataRef));
                }
            } else {
                card.gameObject.SetActive(false);
            }
        }
    }

    private void OnPhraseCardClicked(Masters_UniversalSortPhraseCard clickedCard, SortStepCardData cardData) {
        if (isAnimating || clickedCard == null) return;

        selectedCard = clickedCard;

        foreach (var c in phraseCardsPool) {
            if (c != null && c.gameObject.activeSelf) {
                c.transform.DOKill();
                if (c == selectedCard) {
                    c.transform.DOScale(Vector3.one * 1.08f, 0.2f);
                } else {
                    c.transform.DOScale(Vector3.one, 0.2f);
                }
            }
        }

        if (cardData != null && cardData.expressionAudio != null) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(cardData.expressionAudio);
            } else {
                AudioSource source = GetComponent<AudioSource>();
                if (source == null) source = gameObject.AddComponent<AudioSource>();
                source.PlayOneShot(cardData.expressionAudio);
            }
        }
    }

    private void OnSortBinClicked(Masters_UniversalSortBin bin) {
        if (isAnimating || bin == null) return;

        // If no card was explicitly clicked, grab the top-most active card right now
        Masters_UniversalSortPhraseCard cardToSort = selectedCard;
        if (cardToSort == null || !cardToSort.gameObject.activeSelf) {
            foreach (var c in phraseCardsPool) {
                if (c != null && c.gameObject.activeSelf) {
                    cardToSort = c;
                    break;
                }
            }
        }

        if (cardToSort == null) return;

        int binId = bin.GetSortId();
        int targetId = cardToSort.GetSortId();

        if (binId == targetId) {
            // Correct Sort!
            isAnimating = true;
            selectedCard = null;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            Button btn = cardToSort.GetButton();
            if (btn != null) btn.interactable = false;

            RectTransform cardRT = cardToSort.GetComponent<RectTransform>();
            if (cardRT != null && bin.GetPhraseTargetPointRectTransform() != null) {
                // Exact pattern from PolishedCommunication_Listening_LessonTwo:
                // Set parent to bin target point with worldPositionStays = true, then DOAnchorPos & DOScale to Vector2.zero!
                cardToSort.transform.SetParent(bin.GetPhraseTargetPointRectTransform(), true);
                cardRT.DOAnchorPos(Vector2.zero, animationSpeed).SetEase(Ease.InOutSine);
                cardRT.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.InBack).OnComplete(() => {
                    cardToSort.gameObject.SetActive(false);
                    isAnimating = false;
                    cardsRemainingInRoute--;

                    if (cardsRemainingInRoute <= 0) {
                        StartCoroutine(AdvanceToNextRouteAfterDelay(1.0f));
                    }
                });
            } else {
                cardToSort.gameObject.SetActive(false);
                isAnimating = false;
                cardsRemainingInRoute--;
                if (cardsRemainingInRoute <= 0) {
                    StartCoroutine(AdvanceToNextRouteAfterDelay(1.0f));
                }
            }
        } else {
            // Wrong Sort!
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            cardToSort.transform.DOShakePosition(animationSpeed, 15f, 20);
        }
    }

    private IEnumerator AdvanceToNextRouteAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        LoadRoute(currentRouteIndex + 1);
    }

    private void OnAllRoutesCompleted() {
        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) {
            Debug.LogWarning($"Topic not set for {this.name}!");
            return;
        }
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

    public void SetListeningData(RouteRoundData[] data) {
        routes = data;
    }
}
