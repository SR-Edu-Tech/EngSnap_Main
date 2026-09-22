using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class Masters_TravelFun_Game_LessonOne : Masters_Lesson {

public enum TravelGateCategory {
    OnTheWay = 0,
    GettingOnOff = 1,
    HotelsAndStops = 2,
    Planes = 3
}

[System.Serializable]
public class TravelFun_SortTileData {
    public string phraseText;
    public TravelGateCategory targetGate;
}

    [Header("G01 Gate Rush Settings")]
    [SerializeField]
    private float roundDuration = 60f;
    [SerializeField]
    private int maxLives = 3;
    [SerializeField]
    private int targetScore = 16;
    [SerializeField]
    private float initialSlideDuration = 12.0f; // Extra slow, kid-friendly
    [SerializeField]
    private float minSlideDuration = 5.0f;

    [Header("UI Header & Stats")]
    [SerializeField]
    private TextMeshProUGUI branchTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI livesTMP;
    [SerializeField]
    private TextMeshProUGUI timerTMP;
    [SerializeField]
    private TextMeshProUGUI scoreTMP;
    [SerializeField]
    private TextMeshProUGUI targetTMP;

    [Header("Phrase Tile (Falling/Sliding)")]
    [SerializeField]
    private GameObject phraseTileGO;
    [SerializeField]
    private TextMeshProUGUI phraseTileTMP;
    [SerializeField]
    private RectTransform tileSpawnPoint;
    [SerializeField]
    private RectTransform tileBottomLimit;

    [Header("Four Departure Gates")]
    [SerializeField]
    private Button[] gateButtons = new Button[4];
    [SerializeField]
    private Image[] gateImages = new Image[4];
    [SerializeField]
    private TextMeshProUGUI[] gateTexts = new TextMeshProUGUI[4];

    [Header("Game Over / Results Panel")]
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultTitleTMP;
    [SerializeField]
    private TextMeshProUGUI resultScoreTMP;
    [SerializeField]
    private TextMeshProUGUI resultStatusTMP;
    [SerializeField]
    private Button retryBtn;

    [Header("Phrase Database")]
    [SerializeField]
    private TravelFun_SortTileData[] allPhrases = new TravelFun_SortTileData[] {
        // Gate 0: ON THE WAY
        new TravelFun_SortTileData { phraseText = "set off", targetGate = TravelGateCategory.OnTheWay },
        new TravelFun_SortTileData { phraseText = "see off", targetGate = TravelGateCategory.OnTheWay },
        new TravelFun_SortTileData { phraseText = "hold up", targetGate = TravelGateCategory.OnTheWay },
        new TravelFun_SortTileData { phraseText = "held up", targetGate = TravelGateCategory.OnTheWay },
        new TravelFun_SortTileData { phraseText = "get away", targetGate = TravelGateCategory.OnTheWay },
        new TravelFun_SortTileData { phraseText = "got away", targetGate = TravelGateCategory.OnTheWay },
        new TravelFun_SortTileData { phraseText = "hurry up", targetGate = TravelGateCategory.OnTheWay },

        // Gate 1: GETTING ON & OFF
        new TravelFun_SortTileData { phraseText = "get on", targetGate = TravelGateCategory.GettingOnOff },
        new TravelFun_SortTileData { phraseText = "got on", targetGate = TravelGateCategory.GettingOnOff },
        new TravelFun_SortTileData { phraseText = "get off", targetGate = TravelGateCategory.GettingOnOff },

        // Gate 2: HOTELS & STOPS
        new TravelFun_SortTileData { phraseText = "check in", targetGate = TravelGateCategory.HotelsAndStops },
        new TravelFun_SortTileData { phraseText = "checked in", targetGate = TravelGateCategory.HotelsAndStops },
        new TravelFun_SortTileData { phraseText = "check out", targetGate = TravelGateCategory.HotelsAndStops },
        new TravelFun_SortTileData { phraseText = "checked out", targetGate = TravelGateCategory.HotelsAndStops },
        new TravelFun_SortTileData { phraseText = "stop over", targetGate = TravelGateCategory.HotelsAndStops },

        // Gate 3: PLANES
        new TravelFun_SortTileData { phraseText = "take off", targetGate = TravelGateCategory.Planes },
        new TravelFun_SortTileData { phraseText = "took off", targetGate = TravelGateCategory.Planes },
        new TravelFun_SortTileData { phraseText = "touch down", targetGate = TravelGateCategory.Planes },
        new TravelFun_SortTileData { phraseText = "touched down", targetGate = TravelGateCategory.Planes }
    };

    private int currentLives;
    private int sortedCount;
    private float timeRemaining;
    private bool isGameRunning;
    private TravelFun_SortTileData currentTile;
    private Tweener slideTween;
    private RectTransform phraseTileRt;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;

        AutoBindReferences();

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(OnRetryButtonClicked);
        }

        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(HandleNextButtonClicked);
            nextButton.gameObject.SetActive(false);
        }

        if (resultPanel != null) resultPanel.SetActive(false);
        if (phraseTileGO != null) {
            phraseTileRt = phraseTileGO.GetComponent<RectTransform>();
            phraseTileGO.SetActive(false);
        }

        for (int i = 0; i < gateButtons.Length; i++) {
            if (gateButtons[i] != null) {
                int gateIdx = i;
                gateButtons[i].onClick.RemoveAllListeners();
                gateButtons[i].onClick.AddListener(() => OnGateClicked((TravelGateCategory)gateIdx));
            }
        }
    }

    protected override void Start() {
        base.Start();
        AutoBindReferences();
        StartCoroutine(StartWithIntroRoutine());
    }

    private void AutoBindReferences() {
        if (branchTMP == null) {
            Transform b = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header");
            if (b != null) branchTMP = b.GetComponent<TextMeshProUGUI>();
        }
        if (branchTMP != null) branchTMP.text = "GAME BRANCH (Departure Hall)";

        if (titleTMP == null) {
            Transform t = transform.Find("HeaderContainer/Title") ?? transform.Find("LessonTitle");
            if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) titleTMP.text = "G01 Gate Rush — Four Gates, Fast";

        // Stats Container
        Transform stats = transform.Find("TopStatsContainer") ?? transform.Find("Stats");
        if (stats != null) {
            if (livesTMP == null) {
                Transform l = stats.Find("Lives") ?? stats.Find("LivesTMP");
                if (l != null) livesTMP = l.GetComponent<TextMeshProUGUI>();
            }
            if (timerTMP == null) {
                Transform tm = stats.Find("Time") ?? stats.Find("TimerTMP");
                if (tm != null) timerTMP = tm.GetComponent<TextMeshProUGUI>();
            }
            if (scoreTMP == null) {
                Transform sc = stats.Find("Score") ?? stats.Find("ScoreTMP");
                if (sc != null) scoreTMP = sc.GetComponent<TextMeshProUGUI>();
            }
        }

        // Gates
        Transform gatesTr = transform.Find("GatesContainer") ?? transform.Find("Gates");
        if (gatesTr != null) {
            for (int i = 0; i < 4; i++) {
                string gName = $"Gate_{i + 1}";
                Transform gChild = gatesTr.Find(gName);
                if (gChild != null) {
                    gateButtons[i] = gChild.GetComponent<Button>();
                    gateImages[i] = gChild.GetComponent<Image>();
                    gateTexts[i] = gChild.GetComponentInChildren<TextMeshProUGUI>(true);
                }
            }
        }

        // Phrase Tile
        if (phraseTileGO == null) {
            Transform pt = transform.Find("PhraseTile");
            if (pt != null) phraseTileGO = pt.gameObject;
        }
        if (phraseTileGO != null) {
            phraseTileRt = phraseTileGO.GetComponent<RectTransform>();
            if (phraseTileTMP == null) phraseTileTMP = phraseTileGO.GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }

    private IEnumerator StartWithIntroRoutine() {
        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            float delay = narratorSpeech.length > 0 ? narratorSpeech.length : 3.0f;
            yield return new WaitForSeconds(delay);
        } else {
            yield return new WaitForSeconds(0.4f);
        }

        StartGame();
    }

    private void StartGame() {
        currentLives = maxLives;
        sortedCount = 0;
        timeRemaining = roundDuration;
        isGameRunning = true;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (nextButton != null) nextButton.gameObject.SetActive(false);

        UpdateStatsDisplay();
        StartCoroutine(GameTimerRoutine());
        SpawnNextTile();
    }

    private IEnumerator GameTimerRoutine() {
        while (isGameRunning && timeRemaining > 0) {
            yield return new WaitForSeconds(1.0f);
            if (!isGameRunning) break;
            timeRemaining--;
            UpdateStatsDisplay();

            if (timeRemaining <= 0) {
                EndGame(false, "Time's up!");
                break;
            }
        }
    }

    private void UpdateStatsDisplay() {
        if (livesTMP != null) {
            livesTMP.text = $"Lives: {Mathf.Max(0, currentLives)}";
        }

        if (timerTMP != null) {
            int mins = (int)timeRemaining / 60;
            int secs = (int)timeRemaining % 60;
            timerTMP.text = $"Time: {mins:D2}:{secs:D2}";
        }

        if (scoreTMP != null) {
            scoreTMP.text = $"Sorted: {sortedCount} / {targetScore}";
        }
    }

    private void SpawnNextTile() {
        if (!isGameRunning || allPhrases == null || allPhrases.Length == 0) return;

        currentTile = allPhrases[Random.Range(0, allPhrases.Length)];

        if (phraseTileGO != null && phraseTileRt != null) {
            phraseTileGO.SetActive(true);
            if (phraseTileTMP != null) phraseTileTMP.text = currentTile.phraseText;

            // Start at top center
            phraseTileRt.anchoredPosition = new Vector2(0f, 280f);
            phraseTileRt.localScale = Vector3.one;

            // Calculate progress-based slide speed
            float progress = Mathf.Clamp01((roundDuration - timeRemaining) / roundDuration);
            float duration = Mathf.Lerp(initialSlideDuration, minSlideDuration, progress);

            if (slideTween != null) slideTween.Kill();
            slideTween = phraseTileRt.DOAnchorPosY(-140f, duration).SetEase(Ease.Linear).OnComplete(OnTileMissed);
        }
    }

    private void OnGateClicked(TravelGateCategory chosenGate) {
        if (!isGameRunning || currentTile == null) return;

        if (chosenGate == currentTile.targetGate) {
            // Correct Gate!
            if (slideTween != null) slideTween.Kill();

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            sortedCount++;
            UpdateStatsDisplay();

            // Gate Visual Pulse
            int gateIdx = (int)chosenGate;
            if (gateIdx >= 0 && gateIdx < gateButtons.Length && gateButtons[gateIdx] != null) {
                gateButtons[gateIdx].transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.3f, 5, 0.5f);
            }

            // Burst tile into gate
            if (phraseTileRt != null) {
                phraseTileRt.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack).OnComplete(() => {
                    if (phraseTileGO != null) phraseTileGO.SetActive(false);
                    if (sortedCount >= targetScore) {
                        EndGame(true, "Goal Reached!");
                    } else {
                        SpawnNextTile();
                    }
                });
            }
        } else {
            // Wrong Gate!
            OnWrongGateChosen(chosenGate);
        }
    }

    private void OnWrongGateChosen(TravelGateCategory chosenGate) {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        int gateIdx = (int)chosenGate;
        if (gateIdx >= 0 && gateIdx < gateButtons.Length && gateButtons[gateIdx] != null) {
            gateButtons[gateIdx].transform.DOShakePosition(0.3f, new Vector3(10f, 0f, 0f), 10, 90f);
        }

        currentLives--;
        UpdateStatsDisplay();

        if (currentLives <= 0) {
            if (slideTween != null) slideTween.Kill();
            if (phraseTileGO != null) phraseTileGO.SetActive(false);
            EndGame(false, "Out of Lives!");
        }
    }

    private void OnTileMissed() {
        if (!isGameRunning) return;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        currentLives--;
        UpdateStatsDisplay();

        if (currentLives <= 0) {
            if (phraseTileGO != null) phraseTileGO.SetActive(false);
            EndGame(false, "Out of Lives!");
        } else {
            SpawnNextTile();
        }
    }

    private void EndGame(bool won, string reason) {
        isGameRunning = false;
        if (slideTween != null) slideTween.Kill();
        if (phraseTileGO != null) phraseTileGO.SetActive(false);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        bool passed = sortedCount >= targetScore;

        if (resultTitleTMP != null) {
            if (passed) {
                resultTitleTMP.text = "GATE RUSH COMPLETE! 🌟";
                resultTitleTMP.color = new Color(0.2f, 0.95f, 0.4f);
            } else if (currentLives <= 0) {
                resultTitleTMP.text = "OUT OF LIVES! 💔";
                resultTitleTMP.color = new Color(1f, 0.35f, 0.35f);
            } else {
                resultTitleTMP.text = "TIME'S UP! ⏰";
                resultTitleTMP.color = new Color(1f, 0.55f, 0.2f);
            }
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Phrases Sorted: {sortedCount} / {targetScore}";
            resultScoreTMP.color = passed ? new Color(1f, 0.9f, 0.2f) : Color.white;
        }

        if (resultStatusTMP != null) {
            if (passed) {
                resultStatusTMP.text = "“Incredible speed! You sorted all travel phrases to their correct gates!”";
            } else if (currentLives <= 0) {
                resultStatusTMP.text = "“You ran out of lives! Tap Retry below to try again!”";
            } else {
                resultStatusTMP.text = "“Time ran out! Try again to sort at least 16 phrases into the right gates!”";
            }
        }

        if (retryBtn != null) {
            retryBtn.gameObject.SetActive(!passed);
            retryBtn.interactable = !passed;
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(passed);
            nextButton.interactable = passed;
        }
    }

    private void OnRetryButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        if (resultPanel != null) {
            resultPanel.transform.DOScale(Vector3.zero, 0.2f).OnComplete(() => {
                resultPanel.SetActive(false);
                StartGame();
            });
        } else {
            StartGame();
        }
    }

    private void HandleNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    protected override void OnNextButtonClicked() {
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}

