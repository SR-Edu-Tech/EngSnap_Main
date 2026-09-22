using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class Masters_CourtesyCall_Game_LessonOne : Masters_Lesson {

[System.Serializable]
public class GameG01CourtesyTileData {
    public string courtesyPhrase;       // e.g. "Thanks a lot."
    public string correctDoorCategory;  // "THANK YOU", "EXCUSE ME", "SORRY"
}

    [Header("G01 15 Verbatim Phrase Tiles (p.11)")]
    [SerializeField]
    private GameG01CourtesyTileData[] courtesyTiles;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI timerTMP;
    [SerializeField]
    private TextMeshProUGUI livesTMP;
    [SerializeField]
    private TextMeshProUGUI scoreTMP;

    [Header("Falling Tile References")]
    [SerializeField]
    private RectTransform activeTileRectTransform;
    [SerializeField]
    private TextMeshProUGUI activeTileText;
    [SerializeField]
    private Image activeTileImage;

    [Header("3 Category Door Buttons")]
    [SerializeField]
    private Button thankYouDoorBtn;  // Blue Door
    [SerializeField]
    private Button excuseMeDoorBtn;  // Green Door
    [SerializeField]
    private Button sorryDoorBtn;     // Orange Door

    [Header("Results & Retry Panel")]
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultScoreTMP;
    [SerializeField]
    private TextMeshProUGUI resultStatusTMP;
    [SerializeField]
    private Button retryBtn;

    [Header("Speed & Difficulty Controls")]
    [SerializeField]
    private float initialFallSpeed = 55f; // Slower falling speed for easy reading
    [SerializeField]
    private float speedAcceleration = 0.5f;

    private float roundTimeRemaining = 60f;
    private int livesRemaining = 3;
    private int sortedCount = 0;
    private bool isGameActive = false;

    private int currentTileIndex = 0;
    private float tileFallSpeed = 55f;
    private Vector2 tileStartPos = new Vector2(0f, 250f);
    private Vector2 tileEndPos = new Vector2(0f, -220f);

    private Coroutine gameLoopCoroutine;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;

        PurgeLegacyChildren();
        AutoBindReferences();

        if (thankYouDoorBtn != null) {
            thankYouDoorBtn.onClick.RemoveAllListeners();
            thankYouDoorBtn.onClick.AddListener(() => OnDoorButtonClicked("THANK YOU"));
        }
        if (excuseMeDoorBtn != null) {
            excuseMeDoorBtn.onClick.RemoveAllListeners();
            excuseMeDoorBtn.onClick.AddListener(() => OnDoorButtonClicked("EXCUSE ME"));
        }
        if (sorryDoorBtn != null) {
            sorryDoorBtn.onClick.RemoveAllListeners();
            sorryDoorBtn.onClick.AddListener(() => OnDoorButtonClicked("SORRY"));
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(OnRetryButtonClicked);
        }

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();

        PurgeLegacyChildren();
        EnsureHeaderAndTitle();

        if (courtesyTiles == null || courtesyTiles.Length == 0) {
            PopulateFailsafeTiles();
        }

        StartNewGame();
    }

    private void OnValidate() {
        if (!Application.isPlaying) {
            UpdateEditorPreview();
        }
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        AutoBindReferences();

        if (courtesyTiles == null || courtesyTiles.Length == 0) {
            PopulateFailsafeTiles();
        }

        if (headerTMP != null) headerTMP.text = "GAME BRANCH (Arcade Lane)";
        if (titleTMP != null) titleTMP.text = "G01 Courtesy Sort — Three Doors, Fast";
        if (timerTMP != null) timerTMP.text = "00:60";
        if (livesTMP != null) livesTMP.text = "❤️ ❤️ ❤️";
        if (scoreTMP != null) scoreTMP.text = "0 Sorted";

        if (activeTileText != null && courtesyTiles != null && courtesyTiles.Length > 0) {
            activeTileText.text = courtesyTiles[0].courtesyPhrase;
        }
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer",
            "SpawnArea", "GamePlayGameObject"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                Destroy(lTrans.gameObject);
            }
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "GAME BRANCH (Arcade Lane)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "G01 Courtesy Sort — Three Doors, Fast";
        }
    }

    private void AutoBindReferences() {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (thankYouDoorBtn == null && (n.Contains("thank") || n.Contains("blue"))) thankYouDoorBtn = btn;
            else if (excuseMeDoorBtn == null && (n.Contains("excuse") || n.Contains("green"))) excuseMeDoorBtn = btn;
            else if (sorryDoorBtn == null && (n.Contains("sorry") || n.Contains("orange"))) sorryDoorBtn = btn;
            else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
            else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) nextButton = btn;
        }

        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (timerTMP == null && (n.Contains("timer") || n.Contains("time"))) timerTMP = tmp;
            else if (livesTMP == null && (n.Contains("lives") || n.Contains("heart"))) livesTMP = tmp;
            else if (scoreTMP == null && (n.Contains("score") || n.Contains("sorted"))) scoreTMP = tmp;
            else if (activeTileText == null && (n.Contains("tiletext") || n.Contains("phrase") || n.Contains("active"))) activeTileText = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
        }

        if (activeTileRectTransform == null) {
            Transform tileTrans = transform.Find("ArcadeLane/ActiveTile") ?? transform.Find("ActiveTile");
            if (tileTrans != null) {
                activeTileRectTransform = tileTrans.GetComponent<RectTransform>();
                activeTileImage = tileTrans.GetComponent<Image>();
            }
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    private void PopulateFailsafeTiles() {
        courtesyTiles = new GameG01CourtesyTileData[] {
            new GameG01CourtesyTileData { courtesyPhrase = "Thanks a lot.", correctDoorCategory = "THANK YOU" },
            new GameG01CourtesyTileData { courtesyPhrase = "Excuse me.", correctDoorCategory = "EXCUSE ME" },
            new GameG01CourtesyTileData { courtesyPhrase = "I'm sorry for being so late.", correctDoorCategory = "SORRY" },
            new GameG01CourtesyTileData { courtesyPhrase = "Thank you so much for the birthday gift.", correctDoorCategory = "THANK YOU" },
            new GameG01CourtesyTileData { courtesyPhrase = "Excuse me, do you know what time it is?", correctDoorCategory = "EXCUSE ME" },
            new GameG01CourtesyTileData { courtesyPhrase = "I'm sorry for the mess.", correctDoorCategory = "SORRY" },
            new GameG01CourtesyTileData { courtesyPhrase = "Thank you so much for driving me home.", correctDoorCategory = "THANK YOU" },
            new GameG01CourtesyTileData { courtesyPhrase = "Excuse me sir, you dropped your wallet.", correctDoorCategory = "EXCUSE ME" },
            new GameG01CourtesyTileData { courtesyPhrase = "I'm really sorry, I didn't invite you to the party.", correctDoorCategory = "SORRY" },
            new GameG01CourtesyTileData { courtesyPhrase = "I really appreciate your help.", correctDoorCategory = "THANK YOU" },
            new GameG01CourtesyTileData { courtesyPhrase = "I'm sorry.", correctDoorCategory = "SORRY" },
            new GameG01CourtesyTileData { courtesyPhrase = "Thank you so much for helping me with the science project work.", correctDoorCategory = "THANK YOU" },
            new GameG01CourtesyTileData { courtesyPhrase = "Excuse me.", correctDoorCategory = "EXCUSE ME" },
            new GameG01CourtesyTileData { courtesyPhrase = "Thanks so much. I really appreciate you helping me out with math test.", correctDoorCategory = "THANK YOU" },
            new GameG01CourtesyTileData { courtesyPhrase = "I really appreciate it.", correctDoorCategory = "THANK YOU" }
        };
    }

    private void StartNewGame() {
        roundTimeRemaining = 60f;
        livesRemaining = 3;
        sortedCount = 0;
        currentTileIndex = 0;
        tileFallSpeed = initialFallSpeed;
        isGameActive = true;

        UpdateGameUI();
        SpawnNextTile();

        if (gameLoopCoroutine != null) StopCoroutine(gameLoopCoroutine);
        gameLoopCoroutine = StartCoroutine(GameTimerAndFallLoop());
    }

    private void SpawnNextTile() {
        if (courtesyTiles == null || courtesyTiles.Length == 0) return;

        currentTileIndex = Random.Range(0, courtesyTiles.Length);
        GameG01CourtesyTileData tileData = courtesyTiles[currentTileIndex];

        if (activeTileText != null) activeTileText.text = tileData.courtesyPhrase;
        if (activeTileRectTransform != null) {
            activeTileRectTransform.anchoredPosition = tileStartPos;
            activeTileRectTransform.gameObject.SetActive(true);
        }
    }

    private IEnumerator GameTimerAndFallLoop() {
        while (isGameActive && roundTimeRemaining > 0 && livesRemaining > 0) {
            yield return null;

            float dt = Time.deltaTime;
            roundTimeRemaining -= dt;
            tileFallSpeed += dt * speedAcceleration; // Gradually and gently increase speed

            if (roundTimeRemaining <= 0) {
                roundTimeRemaining = 0;
                EndGame();
                yield break;
            }

            UpdateGameUI();

            // Move tile down
            if (activeTileRectTransform != null) {
                Vector2 pos = activeTileRectTransform.anchoredPosition;
                pos.y -= tileFallSpeed * dt;
                activeTileRectTransform.anchoredPosition = pos;

                if (pos.y <= tileEndPos.y) {
                    OnTileMissed();
                }
            }
        }
    }

    public bool IsGameActive() {
        return isGameActive;
    }

    public void HandleCorrectItemMissed() {
        OnTileMissed();
    }

    public void HandleItemClicked(bool isCorrect) {
        if (isCorrect) sortedCount++;
        else livesRemaining--;
        UpdateGameUI();
    }

    private void OnDoorButtonClicked(string chosenDoorCategory) {
        if (!isGameActive || courtesyTiles == null || currentTileIndex >= courtesyTiles.Length) return;

        GameG01CourtesyTileData tileData = courtesyTiles[currentTileIndex];
        bool isCorrect = chosenDoorCategory.Equals(tileData.correctDoorCategory, System.StringComparison.OrdinalIgnoreCase);

        if (isCorrect) {
            sortedCount++;
            if (activeTileRectTransform != null) {
                activeTileRectTransform.DOPunchScale(Vector3.one * 0.2f, 0.25f);
            }
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
        } else {
            livesRemaining--;
            if (activeTileRectTransform != null) {
                activeTileRectTransform.DOShakePosition(0.3f, 10f);
            }
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }

        UpdateGameUI();

        if (livesRemaining <= 0) {
            EndGame();
        } else {
            SpawnNextTile();
        }
    }

    private void OnTileMissed() {
        livesRemaining--;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
        UpdateGameUI();

        if (livesRemaining <= 0) {
            EndGame();
        } else {
            SpawnNextTile();
        }
    }

    private void UpdateGameUI() {
        if (timerTMP != null) {
            int secs = Mathf.CeilToInt(roundTimeRemaining);
            timerTMP.text = $"00:{secs:D2}";
        }

        if (livesTMP != null) {
            string hearts = "";
            for (int i = 0; i < livesRemaining; i++) hearts += "❤️ ";
            livesTMP.text = string.IsNullOrEmpty(hearts) ? "💔" : hearts.Trim();
        }

        if (scoreTMP != null) {
            scoreTMP.text = $"{sortedCount} Sorted";
        }
    }

    private void EndGame() {
        isGameActive = false;
        if (gameLoopCoroutine != null) StopCoroutine(gameLoopCoroutine);

        if (activeTileRectTransform != null) {
            activeTileRectTransform.gameObject.SetActive(false);
        }

        bool passed = (livesRemaining > 0 && sortedCount >= 8);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            if (resultScoreTMP != null) resultScoreTMP.text = $"Phrases Sorted: {sortedCount}";
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "VICTORY! ARCADE COMPLETE!" : "GAME OVER!";
                resultStatusTMP.color = passed ? Color.green : Color.red;
            }
        }

        if (retryBtn != null) {
            retryBtn.gameObject.SetActive(true);
            retryBtn.interactable = true;
            retryBtn.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f);
        }

        if (passed && nextButton != null) {
            nextButton.interactable = true;
            NextButtonAnimation();
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(passed ? Masters_SFX.Correct : Masters_SFX.Incorrect);
        }
    }

    private void OnRetryButtonClicked() {
        if (resultPanel != null) resultPanel.SetActive(false);
        StartNewGame();
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}
