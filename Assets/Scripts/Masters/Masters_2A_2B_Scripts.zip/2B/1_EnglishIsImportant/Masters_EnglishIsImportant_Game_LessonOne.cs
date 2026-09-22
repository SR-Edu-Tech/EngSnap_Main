using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class Masters_EnglishIsImportant_Game_LessonOne : Masters_Lesson {

[System.Serializable]
public class G01PhraseItem {
    public string phraseText; // e.g. "Could you say that again?"
    public int doorIndex;    // 0 = ASK TO REPEAT, 1 = CHECK THEM, 2 = CHECK ME, 3 = ANOTHER WAY
}
    [Header("G01 Phrase Items (pp. 8-9)")]
    [SerializeField]
    private G01PhraseItem[] phraseItems;

    [Header("UI References")]
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

    [Header("Arcade Door Buttons (4 Doors)")]
    [SerializeField]
    private Button[] doorButtons; // 0 = ASK TO REPEAT, 1 = CHECK THEM, 2 = CHECK ME, 3 = ANOTHER WAY

    [Header("Falling Phrase Tile")]
    [SerializeField]
    private RectTransform phraseTileTransform;
    [SerializeField]
    private TextMeshProUGUI phraseTileTextTMP;

    [Header("Audio Controls")]
    [SerializeField]
    private Button replayAudioBtn;

    [Header("Results & Retry Panel")]
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultScoreTMP;
    [SerializeField]
    private TextMeshProUGUI resultStatusTMP;
    [SerializeField]
    private Button retryBtn;

    [Header("Editor Preview")]
    [Range(0, 3)]
    public int editorPreviewDoor = 0;

    private int currentScore = 0;
    private int remainingLives = 3;
    private float remainingTime = 60f;
    private bool isGameActive = false;
    private bool isProcessingInput = false;

    private G01PhraseItem currentFallingPhrase;
    private Coroutine gameTimerCoroutine;
    private Coroutine tileFallCoroutine;

    // Door Colors matching GDD spec
    private Color[] doorColors = new Color[] {
        new Color(0.54f, 0.17f, 0.89f, 1f), // ASK TO REPEAT (Purple #8A2BE2)
        new Color(0.12f, 0.56f, 1.00f, 1f), // CHECK THEM (Blue #1E90FF)
        new Color(0.18f, 0.55f, 0.34f, 1f), // CHECK ME (Green #2E8B57)
        new Color(1.00f, 0.55f, 0.00f, 1f)  // ANOTHER WAY (Orange #FF8C00)
    };

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Game;

        PurgeLegacyChildren();
        AutoBindReferences();

        if (doorButtons != null && doorButtons.Length > 0) {
            for (int i = 0; i < doorButtons.Length; i++) {
                if (doorButtons[i] != null) {
                    int doorIdx = i;
                    doorButtons[i].onClick.RemoveAllListeners();
                    doorButtons[i].onClick.AddListener(() => OnDoorClicked(doorIdx));
                }
            }
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
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

        if (phraseItems == null || phraseItems.Length == 0) {
            PopulateFailsafePhrases();
        }

        StartCoroutine(StartWithIntroAudioRoutine());
    }

    private void OnValidate() {
        if (!Application.isPlaying) {
            UpdateEditorPreview();
        }
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        AutoBindReferences();

        if (phraseItems == null || phraseItems.Length == 0) {
            PopulateFailsafePhrases();
        }

        if (headerTMP != null) headerTMP.text = "🎮 GAME BRANCH (Arcade)";
        if (titleTMP != null) titleTMP.text = "G01 Phrase Sort — Four Doors, Fast";
        if (timerTMP != null) timerTMP.text = "60s";
        if (livesTMP != null) livesTMP.text = "❤❤❤";
        if (scoreTMP != null) scoreTMP.text = "0/16";

        if (doorButtons != null && doorButtons.Length >= 4) {
            string[] doorTitles = new string[] { "ASK TO REPEAT", "CHECK THEM", "CHECK ME", "ANOTHER WAY" };
            for (int i = 0; i < 4; i++) {
                if (doorButtons[i] != null) {
                    doorButtons[i].gameObject.SetActive(true);
                    SetButtonText(doorButtons[i], doorTitles[i]);
                }
            }
        }

        if (phraseTileTextTMP != null && phraseItems != null && phraseItems.Length > 0) {
            phraseTileTextTMP.text = phraseItems[0].phraseText;
        }

        if (phraseTileTransform != null) {
            phraseTileTransform.anchoredPosition = new Vector2(0f, 100f);
        }
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                Destroy(lTrans.gameObject);
            }
        }

        for (int i = transform.childCount - 1; i >= 0; i--) {
            Transform child = transform.GetChild(i);
            string cName = child.name.ToLower();
            if (cName.Contains("statement") || cName.Contains("words") || cName.Contains("fillin")) {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "🎮 GAME BRANCH (Arcade)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "G01 Phrase Sort — Four Doors, Fast";
        }
    }

    private void AutoBindReferences() {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> dBtns = new List<Button>();

        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("door") || n.Contains("optionbutton") || n.Contains("chip")) {
                dBtns.Add(btn);
            } else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("repeat") || n.Contains("audio"))) {
                replayAudioBtn = btn;
            } else if (retryBtn == null && n.Contains("retry")) {
                retryBtn = btn;
            } else if (nextButton == null && n.Contains("next")) {
                nextButton = btn;
            }
        }

        if ((doorButtons == null || doorButtons.Length == 0 || System.Array.Exists(doorButtons, b => b == null)) && dBtns.Count >= 4) {
            doorButtons = new Button[4];
            for (int i = 0; i < 4; i++) {
                doorButtons[i] = dBtns[i];
            }
        }

        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (timerTMP == null && n.Contains("timer")) timerTMP = tmp;
            else if (livesTMP == null && n.Contains("lives")) livesTMP = tmp;
            else if (scoreTMP == null && (n.Contains("score") || n.Contains("progress"))) scoreTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
            else if (phraseTileTextTMP == null && (n.Contains("phrasetile") || n.Contains("tiletext"))) phraseTileTextTMP = tmp;
        }

        if (phraseTileTransform == null) {
            Transform ptTr = transform.Find("PhraseTile") ?? transform.Find("ArcadeLane/PhraseTile");
            if (ptTr != null) phraseTileTransform = ptTr.GetComponent<RectTransform>();
        }
    }

    private void PopulateFailsafePhrases() {
        List<G01PhraseItem> list = new List<G01PhraseItem>();

        // Category 0: ASK TO REPEAT (Purple)
        list.Add(new G01PhraseItem { phraseText = "Could you say that again?", doorIndex = 0 });
        list.Add(new G01PhraseItem { phraseText = "Pardon me?", doorIndex = 0 });
        list.Add(new G01PhraseItem { phraseText = "What did you say?", doorIndex = 0 });
        list.Add(new G01PhraseItem { phraseText = "Can you repeat that, please?", doorIndex = 0 });
        list.Add(new G01PhraseItem { phraseText = "I didn't quite catch that.", doorIndex = 0 });

        // Category 1: CHECK THEM (Blue)
        list.Add(new G01PhraseItem { phraseText = "Do you mean...?", doorIndex = 1 });
        list.Add(new G01PhraseItem { phraseText = "Are you saying that...?", doorIndex = 1 });
        list.Add(new G01PhraseItem { phraseText = "So what you're saying is...?", doorIndex = 1 });
        list.Add(new G01PhraseItem { phraseText = "Did you mean...?", doorIndex = 1 });
        list.Add(new G01PhraseItem { phraseText = "If I understand correctly, you mean...?", doorIndex = 1 });

        // Category 2: CHECK ME (Green)
        list.Add(new G01PhraseItem { phraseText = "Does that make sense?", doorIndex = 2 });
        list.Add(new G01PhraseItem { phraseText = "Do you know what I mean?", doorIndex = 2 });
        list.Add(new G01PhraseItem { phraseText = "Is that clear?", doorIndex = 2 });
        list.Add(new G01PhraseItem { phraseText = "Are you with me so far?", doorIndex = 2 });
        list.Add(new G01PhraseItem { phraseText = "Am I making sense?", doorIndex = 2 });

        // Category 3: ANOTHER WAY (Orange)
        list.Add(new G01PhraseItem { phraseText = "In other words...", doorIndex = 3 });
        list.Add(new G01PhraseItem { phraseText = "Let me rephrase that...", doorIndex = 3 });
        list.Add(new G01PhraseItem { phraseText = "To put it another way...", doorIndex = 3 });
        list.Add(new G01PhraseItem { phraseText = "What I mean is...", doorIndex = 3 });
        list.Add(new G01PhraseItem { phraseText = "Simply put...", doorIndex = 3 });

        phraseItems = list.ToArray();
    }

    private IEnumerator StartWithIntroAudioRoutine() {
        isProcessingInput = true;

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            float delay = narratorSpeech.length > 0 ? narratorSpeech.length : 2.5f;
            yield return new WaitForSeconds(delay);
        }

        StartLesson();
    }

    public void StartLesson() {
        currentScore = 0;
        remainingLives = 3;
        remainingTime = 60f;
        isGameActive = true;
        isProcessingInput = false;

        if (resultPanel != null) resultPanel.SetActive(false);

        UpdateUI();

        if (gameTimerCoroutine != null) StopCoroutine(gameTimerCoroutine);
        gameTimerCoroutine = StartCoroutine(GameTimerRoutine());

        SpawnNextPhraseTile();
    }

    private IEnumerator GameTimerRoutine() {
        while (isGameActive && remainingTime > 0f) {
            yield return new WaitForSeconds(1f);
            remainingTime -= 1f;
            if (timerTMP != null) timerTMP.text = $"{Mathf.Max(0, (int)remainingTime)}s";

            if (remainingTime <= 0f) {
                EndGame();
            }
        }
    }

    private void UpdateUI() {
        if (scoreTMP != null) scoreTMP.text = $"{currentScore}/16";
        if (livesTMP != null) {
            string hearts = "";
            for (int i = 0; i < remainingLives; i++) hearts += "❤";
            livesTMP.text = hearts;
        }
        if (timerTMP != null) timerTMP.text = $"{Mathf.Max(0, (int)remainingTime)}s";
    }

    private void SpawnNextPhraseTile() {
        if (!isGameActive) return;

        if (phraseItems == null || phraseItems.Length == 0) PopulateFailsafePhrases();

        int randIndex = Random.Range(0, phraseItems.Length);
        currentFallingPhrase = phraseItems[randIndex];

        if (phraseTileTextTMP != null) {
            phraseTileTextTMP.text = currentFallingPhrase.phraseText;
        }

        if (phraseTileTransform != null) {
            phraseTileTransform.anchoredPosition = new Vector2(0f, 220f);
            phraseTileTransform.localScale = Vector3.one;
        }

        isProcessingInput = false;

        if (tileFallCoroutine != null) StopCoroutine(tileFallCoroutine);
        tileFallCoroutine = StartCoroutine(TileFallRoutine());
    }

    private IEnumerator TileFallRoutine() {
        float startY = 220f;
        float endY = -220f;
        float duration = Mathf.Max(3f, 6f - (currentScore * 0.15f)); // Speed increases gradually
        float elapsed = 0f;

        while (elapsed < duration && isGameActive && !isProcessingInput) {
            elapsed += Time.deltaTime;
            float newY = Mathf.Lerp(startY, endY, elapsed / duration);
            if (phraseTileTransform != null) {
                phraseTileTransform.anchoredPosition = new Vector2(0f, newY);
            }
            yield return null;
        }

        if (!isProcessingInput && isGameActive) {
            // Tile reached bottom unsorted -> lose life
            OnTileMissed();
        }
    }

    private void OnDoorClicked(int doorIndex) {
        if (!isGameActive || isProcessingInput || currentFallingPhrase == null) return;

        isProcessingInput = true;
        if (tileFallCoroutine != null) StopCoroutine(tileFallCoroutine);

        if (doorIndex == currentFallingPhrase.doorIndex) {
            // Correct door
            currentScore++;
            UpdateUI();

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (phraseTileTransform != null) {
                phraseTileTransform.DOPunchScale(Vector3.one * 0.25f, 0.3f).OnComplete(() => {
                    SpawnNextPhraseTile();
                });
            } else {
                SpawnNextPhraseTile();
            }
        } else {
            // Wrong door
            remainingLives--;
            UpdateUI();

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (phraseTileTransform != null) {
                phraseTileTransform.DOShakePosition(0.4f, new Vector3(20f, 0f, 0f), 10, 90, false, true).OnComplete(() => {
                    if (remainingLives <= 0) {
                        EndGame();
                    } else {
                        SpawnNextPhraseTile();
                    }
                });
            } else {
                if (remainingLives <= 0) EndGame();
                else SpawnNextPhraseTile();
            }
        }
    }

    private void OnTileMissed() {
        isProcessingInput = true;
        remainingLives--;
        UpdateUI();

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        if (remainingLives <= 0) {
            EndGame();
        } else {
            SpawnNextPhraseTile();
        }
    }

    private void EndGame() {
        isGameActive = false;
        if (gameTimerCoroutine != null) StopCoroutine(gameTimerCoroutine);
        if (tileFallCoroutine != null) StopCoroutine(tileFallCoroutine);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"{currentScore}/16";
        }

        bool passed = currentScore >= 16; // 16 correct sorted threshold

        if (resultStatusTMP != null) {
            resultStatusTMP.text = passed ? "AWESOME! YOU SORTED 16+ PHRASES!" : "TIME / LIVES EXPIRED! TRY AGAIN (16 REQUIRED)";
        }

        if (retryBtn != null) {
            retryBtn.gameObject.SetActive(!passed);
        }

        if (passed) {
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            Debug.Log($"[G01 Game] Passed with score {currentScore}/16!");
        } else {
            Debug.Log($"[G01 Game] Failed with score {currentScore}/16. Retry available.");
        }
    }

    private void SetButtonText(Button btn, string textValue) {
        if (btn == null) return;
        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null) {
            tmp.text = textValue;
            tmp.enabled = true;
            tmp.gameObject.SetActive(true);
        }
    }

    private void OnReplayAudioClicked() {
        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        }
    }

    private void OnRetryButtonClicked() {
        StartLesson();
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}
