using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


/// <summary>
/// Unit 3: Household Chores — Game Lesson One (G01 Chore Dash — Four Rooms, Fast).
/// Timed sorting reaction game with 4 room doors:
/// Door 0: FLOOR & WINDOW JOBS 🧹
/// Door 1: KITCHEN & TABLE JOBS 🍽️
/// Door 2: CLOTHES JOBS 👕
/// Door 3: OUTSIDE & PET JOBS 🐶
/// 
/// Game Mechanics:
/// - Controls locked while intro audio is speaking; auto-unlocked once intro finishes.
/// - Chore tiles slide down in a shuffled stream.
/// - 4 doors, 3 lives, 60 seconds total countdown.
/// - Speed increases gradually.
/// - Success condition: Sort at least 16 chores into the correct group within time / lives.
/// </summary>
public class Masters_HouseholdChores_Game_LessonOne : Masters_Lesson {

[System.Serializable]
public class HouseholdChores_GameG01Chore {
    public int choreId;
    public string choreName;
    public int correctDoorIndex; // 0: FLOOR & WINDOW, 1: KITCHEN & TABLE, 2: CLOTHES, 3: OUTSIDE & PET
}

    [Header("G01 32 Household Chore Items")]
    [SerializeField] public HouseholdChores_GameG01Chore[] choreItems;

    [Header("UI Display References")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI livesTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;

    [Header("Question Timer Bar")]
    [SerializeField] private Image timerBarFillImage;

    [Header("Chore Corridor Card")]
    [SerializeField] private GameObject choreCardObject;
    [SerializeField] private TextMeshProUGUI choreTextTMP;

    [Header("4 Room Doors")]
    [SerializeField] private Button[] doorButtons; // 4 Doors
    [SerializeField] private Image[] doorImages;
    [SerializeField] private TextMeshProUGUI[] doorTexts;

    [Header("Results & Retry Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitleTMP;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private TextMeshProUGUI resultStatusTMP;
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button returnHubBtn;

    [Header("Audio References")]
    [SerializeField] private AudioClip introAudio;
    [SerializeField] private AudioClip recapAudio;

    [Header("Game Settings")]
    [SerializeField] private float totalGameTime = 60f;
    [SerializeField] private int maxLives = 3;
    [SerializeField] private int targetMatchesToWin = 16;
    [SerializeField] private float initialQuestionDuration = 10.0f;

    private float remainingGameTime;
    private int currentLives;
    private int totalSorted = 0;
    private bool isGameActive = false;
    private bool isHandlingAnswer = false;
    private bool isIntroPhase = true;
    private int currentChoreIndex = 0;
    private float currentQuestionDuration = 10.0f;
    private float questionTimeRemaining = 10.0f;
    private List<int> shuffledIndices = new List<int>();
    private Coroutine introRoutine;

    private readonly string[] roomDoorNames = new string[] {
        "FLOOR & WINDOW",
        "KITCHEN & TABLE",
        "CLOTHES JOBS",
        "OUTSIDE & PET"
    };

    private readonly Color defaultDoorColor = new Color(0.12f, 0.35f, 0.65f, 0.95f);
    private readonly Color correctDoorColor = new Color(0.15f, 0.75f, 0.35f, 1f);
    private readonly Color wrongDoorColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Game;
        base.Awake();

        AutoBindReferences();
        InitChoresIfEmpty();
        WireEventListeners();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Game;

        AutoBindReferences();
        WireEventListeners();
        EnsureNextAndBackButtonWired();
        EnsureHeaderAndTitle();

        if (choreItems == null || choreItems.Length == 0) {
            InitChoresIfEmpty();
        }

        // Lock controls during introduction
        isIntroPhase = true;
        isGameActive = false;
        EnableDoors(false);

        introRoutine = StartCoroutine(StartGameAfterIntroRoutine());
    }

    private IEnumerator StartGameAfterIntroRoutine() {
        remainingGameTime = totalGameTime;
        currentLives = maxLives;
        totalSorted = 0;
        isHandlingAnswer = false;

        if (headerTMP != null) headerTMP.gameObject.SetActive(true);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (choreCardObject != null) choreCardObject.SetActive(true);

        Transform doorsGrid = transform.Find("DoorsGrid") ?? transform.Find("OptionsContainer") ?? transform.Find("OptionsGrid");
        if (doorsGrid != null) doorsGrid.gameObject.SetActive(true);

        UpdateScoreAndHUD();
        SetDoorLabels();

        if (timerBarFillImage != null) timerBarFillImage.fillAmount = 1f;
        if (timerTMP != null) timerTMP.text = $"Time: {Mathf.CeilToInt(totalGameTime)}s";
        if (choreItems != null && choreItems.Length > 0 && choreTextTMP != null) {
            choreTextTMP.text = $"\"{choreItems[0].choreName}\"";
        }

        AudioClip clipToPlay = introAudio != null ? introAudio : narratorSpeech;
        if (clipToPlay != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            yield return new WaitForSeconds(clipToPlay.length + 0.3f);
        } else {
            yield return new WaitForSeconds(1.0f);
        }

        isIntroPhase = false;
        RestartGame();
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP != null) headerTMP.text = "HOUSEHOLD CHORES 🧹";
        if (titleTMP != null) {
            titleTMP.text = "G01 Chore Dash — Four Rooms, Fast";
            titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f);
        }
        if (subtitleTMP != null) subtitleTMP.text = "Sort each chore into the room door where it belongs before time runs out!";
    }

    private void Update() {
        if (!isGameActive || isHandlingAnswer || isIntroPhase) return;

        // Total game countdown
        remainingGameTime -= Time.deltaTime;
        if (timerTMP != null) {
            timerTMP.text = $"Time: {Mathf.Max(0f, Mathf.Ceil(remainingGameTime))}s";
        }

        // Per-item timer bar countdown
        questionTimeRemaining -= Time.deltaTime;
        if (timerBarFillImage != null && currentQuestionDuration > 0f) {
            timerBarFillImage.fillAmount = Mathf.Clamp01(questionTimeRemaining / currentQuestionDuration);
        }

        // Check timeout triggers
        if (questionTimeRemaining <= 0f) {
            StartCoroutine(HandleAnswer(false, -1));
        } else if (remainingGameTime <= 0f || currentLives <= 0) {
            EndGame();
        }
    }

    private void WireEventListeners() {
        if (doorButtons != null) {
            for (int i = 0; i < doorButtons.Length; i++) {
                int doorIdx = i;
                if (doorButtons[i] != null) {
                    doorButtons[i].onClick.RemoveAllListeners();
                    doorButtons[i].onClick.AddListener(() => OnDoorClicked(doorIdx));
                }
            }
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(RestartGame);
        }

        if (returnHubBtn != null) {
            returnHubBtn.onClick.RemoveAllListeners();
            returnHubBtn.onClick.AddListener(OnReturnHubClicked);
        }

        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
    }

    public void RestartGame() {
        if (introRoutine != null) StopCoroutine(introRoutine);
        isIntroPhase = false;

        remainingGameTime = totalGameTime;
        currentLives = maxLives;
        totalSorted = 0;
        currentQuestionDuration = initialQuestionDuration;
        isHandlingAnswer = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (choreCardObject != null) choreCardObject.SetActive(true);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);

        Transform doorsGrid = transform.Find("DoorsGrid") ?? transform.Find("OptionsContainer") ?? transform.Find("OptionsGrid");
        if (doorsGrid != null) doorsGrid.gameObject.SetActive(true);

        ShuffleChoresPool();
        currentChoreIndex = 0;

        SetDoorLabels();
        UpdateScoreAndHUD();

        isGameActive = true;
        ShowCurrentChore();
    }

    private void ShuffleChoresPool() {
        shuffledIndices.Clear();
        if (choreItems == null || choreItems.Length == 0) return;

        for (int i = 0; i < choreItems.Length; i++) {
            shuffledIndices.Add(i);
        }

        for (int i = 0; i < shuffledIndices.Count; i++) {
            int temp = shuffledIndices[i];
            int rand = Random.Range(i, shuffledIndices.Count);
            shuffledIndices[i] = shuffledIndices[rand];
            shuffledIndices[rand] = temp;
        }
    }

    private void ShowCurrentChore() {
        if (!isGameActive || choreItems == null || choreItems.Length == 0) return;

        if (currentChoreIndex >= shuffledIndices.Count) {
            ShuffleChoresPool();
            currentChoreIndex = 0;
        }

        int idx = shuffledIndices[currentChoreIndex];
        HouseholdChores_GameG01Chore item = choreItems[idx];

        // Speed increases gradually as player sorts more items
        currentQuestionDuration = Mathf.Max(2.2f, initialQuestionDuration - (totalSorted * 0.15f));
        questionTimeRemaining = currentQuestionDuration;

        if (choreTextTMP != null) {
            choreTextTMP.text = $"\"{item.choreName}\"";
            choreTextTMP.color = Color.white;
        }

        if (choreCardObject != null) {
            choreCardObject.transform.DOKill();
            choreCardObject.transform.localScale = Vector3.one * 0.7f;
            choreCardObject.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }

        ResetDoorColors();
        EnableDoors(true);
    }

    private void OnDoorClicked(int doorIndex) {
        if (!isGameActive || isHandlingAnswer || isIntroPhase) return;
        if (choreItems == null || choreItems.Length == 0) return;

        int idx = shuffledIndices[currentChoreIndex];
        HouseholdChores_GameG01Chore currentItem = choreItems[idx];

        bool isCorrect = (doorIndex == currentItem.correctDoorIndex);
        StartCoroutine(HandleAnswer(isCorrect, doorIndex));
    }

    private IEnumerator HandleAnswer(bool isCorrect, int clickedDoorIndex) {
        isHandlingAnswer = true;
        EnableDoors(false);

        int idx = shuffledIndices[currentChoreIndex];
        HouseholdChores_GameG01Chore currentItem = choreItems[idx];
        int correctDoor = currentItem.correctDoorIndex;

        if (isCorrect) {
            totalSorted++;
            UpdateScoreAndHUD();

            if (clickedDoorIndex >= 0 && clickedDoorIndex < doorImages.Length && doorImages[clickedDoorIndex] != null) {
                doorImages[clickedDoorIndex].color = correctDoorColor;
                doorImages[clickedDoorIndex].transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }

            if (choreTextTMP != null) choreTextTMP.color = correctDoorColor;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            yield return new WaitForSeconds(0.35f);
        } else {
            currentLives--;
            UpdateScoreAndHUD();

            if (clickedDoorIndex >= 0 && clickedDoorIndex < doorImages.Length && doorImages[clickedDoorIndex] != null) {
                doorImages[clickedDoorIndex].color = wrongDoorColor;
                doorImages[clickedDoorIndex].transform.DOShakePosition(0.4f, 8f, 15, 90, false, true);
            }

            // Highlight the correct room door in green
            if (correctDoor >= 0 && correctDoor < doorImages.Length && doorImages[correctDoor] != null) {
                doorImages[correctDoor].color = correctDoorColor;
            }

            if (choreCardObject != null) {
                choreCardObject.transform.DOShakePosition(0.4f, 10f, 15, 90, false, true);
            }

            if (choreTextTMP != null) choreTextTMP.color = wrongDoorColor;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            yield return new WaitForSeconds(0.65f);
        }

        if (currentLives <= 0 || remainingGameTime <= 0f) {
            EndGame();
        } else {
            currentChoreIndex++;
            isHandlingAnswer = false;
            ShowCurrentChore();
        }
    }

    private void EndGame() {
        isGameActive = false;
        isHandlingAnswer = false;

        EnableDoors(false);
        if (choreCardObject != null) choreCardObject.SetActive(false);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);

        Transform doorsGrid = transform.Find("DoorsGrid") ?? transform.Find("OptionsContainer") ?? transform.Find("OptionsGrid");
        if (doorsGrid != null) doorsGrid.gameObject.SetActive(false);

        bool passed = (totalSorted >= targetMatchesToWin);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

            if (resultTitleTMP != null) {
                resultTitleTMP.text = passed ? "CHORE DASH COMPLETE! 🧹" : "TIME'S UP!";
                resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"You sorted {totalSorted} chores correctly! (Target: {targetMatchesToWin})";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "Success! All household chores safely delivered to their rightful rooms." : $"You need at least {targetMatchesToWin} correct sorts to complete this challenge.";
            }

            if (returnHubBtn != null) {
                returnHubBtn.gameObject.SetActive(passed);
            }

            if (retryBtn != null) {
                retryBtn.gameObject.SetActive(!passed || totalSorted < targetMatchesToWin);
            }
        }

        if (recapAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(recapAudio);
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(passed);
        }

        if (passed && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    public void OnReturnHubClicked() {
        topic = Masters_Topic.Game;
        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Rewards);
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    protected override void OnNextButtonClicked() {
        if (isIntroPhase) {
            if (introRoutine != null) StopCoroutine(introRoutine);
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.StopVoiceOver();
            RestartGame();
            return;
        }

        OnReturnHubClicked();
    }

    private void UpdateScoreAndHUD() {
        if (scoreTMP != null) scoreTMP.text = $"Score: {totalSorted}";
        if (progressTMP != null) progressTMP.text = $"Sorted: {totalSorted}/{targetMatchesToWin}";
        if (livesTMP != null) {
            livesTMP.text = $"Lives: {currentLives}";
        }
    }

    private void SetDoorLabels() {
        if (doorTexts == null) return;
        for (int i = 0; i < doorTexts.Length; i++) {
            if (doorTexts[i] != null && i < roomDoorNames.Length) {
                doorTexts[i].text = roomDoorNames[i];
            }
        }
    }

    private void ResetDoorColors() {
        if (doorImages == null) return;
        for (int i = 0; i < doorImages.Length; i++) {
            if (doorImages[i] != null) {
                doorImages[i].color = defaultDoorColor;
                doorImages[i].transform.localScale = Vector3.one;
            }
        }
    }

    private void EnableDoors(bool enable) {
        if (doorButtons == null) return;
        for (int i = 0; i < doorButtons.Length; i++) {
            if (doorButtons[i] != null) doorButtons[i].interactable = enable;
        }
    }

    public void InitChoresIfEmpty() {
        if (choreItems != null && choreItems.Length > 0) return;

        choreItems = new HouseholdChores_GameG01Chore[] {
            // Door 0: FLOOR & WINDOW JOBS
            new HouseholdChores_GameG01Chore { choreId = 1, choreName = "Sweep the floor", correctDoorIndex = 0 },
            new HouseholdChores_GameG01Chore { choreId = 2, choreName = "Dust the floor", correctDoorIndex = 0 },
            new HouseholdChores_GameG01Chore { choreId = 3, choreName = "Mop the floor", correctDoorIndex = 0 },
            new HouseholdChores_GameG01Chore { choreId = 4, choreName = "Scrub the floor", correctDoorIndex = 0 },
            new HouseholdChores_GameG01Chore { choreId = 5, choreName = "Clean the window", correctDoorIndex = 0 },
            new HouseholdChores_GameG01Chore { choreId = 6, choreName = "Vacuum the carpet", correctDoorIndex = 0 },
            new HouseholdChores_GameG01Chore { choreId = 7, choreName = "Dust the furniture", correctDoorIndex = 0 },
            new HouseholdChores_GameG01Chore { choreId = 8, choreName = "Polish the floor", correctDoorIndex = 0 },

            // Door 1: KITCHEN & TABLE JOBS
            new HouseholdChores_GameG01Chore { choreId = 9, choreName = "Set the table", correctDoorIndex = 1 },
            new HouseholdChores_GameG01Chore { choreId = 10, choreName = "Dry the dishes", correctDoorIndex = 1 },
            new HouseholdChores_GameG01Chore { choreId = 11, choreName = "Wash up the dishes", correctDoorIndex = 1 },
            new HouseholdChores_GameG01Chore { choreId = 12, choreName = "Clear the table", correctDoorIndex = 1 },
            new HouseholdChores_GameG01Chore { choreId = 13, choreName = "Wipe the kitchen counter", correctDoorIndex = 1 },
            new HouseholdChores_GameG01Chore { choreId = 14, choreName = "Load the dishwasher", correctDoorIndex = 1 },
            new HouseholdChores_GameG01Chore { choreId = 15, choreName = "Clean the sink", correctDoorIndex = 1 },
            new HouseholdChores_GameG01Chore { choreId = 16, choreName = "Put away the plates", correctDoorIndex = 1 },

            // Door 2: CLOTHES JOBS
            new HouseholdChores_GameG01Chore { choreId = 17, choreName = "Iron the clothes", correctDoorIndex = 2 },
            new HouseholdChores_GameG01Chore { choreId = 18, choreName = "Hang out the clothes", correctDoorIndex = 2 },
            new HouseholdChores_GameG01Chore { choreId = 19, choreName = "Make the bed", correctDoorIndex = 2 },
            new HouseholdChores_GameG01Chore { choreId = 20, choreName = "Fold the laundry", correctDoorIndex = 2 },
            new HouseholdChores_GameG01Chore { choreId = 21, choreName = "Wash the clothes", correctDoorIndex = 2 },
            new HouseholdChores_GameG01Chore { choreId = 22, choreName = "Put clothes in the wardrobe", correctDoorIndex = 2 },
            new HouseholdChores_GameG01Chore { choreId = 23, choreName = "Tidy up the bedroom", correctDoorIndex = 2 },
            new HouseholdChores_GameG01Chore { choreId = 24, choreName = "Change the bedsheets", correctDoorIndex = 2 },

            // Door 3: OUTSIDE & PET JOBS
            new HouseholdChores_GameG01Chore { choreId = 25, choreName = "Take the trash out", correctDoorIndex = 3 },
            new HouseholdChores_GameG01Chore { choreId = 26, choreName = "Feed the dog", correctDoorIndex = 3 },
            new HouseholdChores_GameG01Chore { choreId = 27, choreName = "Wash the car", correctDoorIndex = 3 },
            new HouseholdChores_GameG01Chore { choreId = 28, choreName = "Do the gardening", correctDoorIndex = 3 },
            new HouseholdChores_GameG01Chore { choreId = 29, choreName = "Do the shopping", correctDoorIndex = 3 },
            new HouseholdChores_GameG01Chore { choreId = 30, choreName = "Water the plants", correctDoorIndex = 3 },
            new HouseholdChores_GameG01Chore { choreId = 31, choreName = "Walk the dog", correctDoorIndex = 3 },
            new HouseholdChores_GameG01Chore { choreId = 32, choreName = "Clean the garden path", correctDoorIndex = 3 }
        };
    }

    public void AutoBindReferences() {
        if (titleTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("Title") ?? transform.Find("HeaderContainer/LessonTitle");
            if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP == null) {
            Transform t = transform.Find("Subtitle") ?? transform.Find("Instruction") ?? transform.Find("HeaderContainer/Subtitle");
            if (t != null) subtitleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP == null) {
            Transform t = transform.Find("Header") ?? transform.Find("BranchHeader") ?? transform.Find("HeaderContainer/Header");
            if (t != null) headerTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (timerTMP == null) {
            Transform t = transform.Find("TimerTMP") ?? transform.Find("Timer") ?? transform.Find("HeaderContainer/TimerTMP");
            if (t != null) timerTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (livesTMP == null) {
            Transform t = transform.Find("LivesTMP") ?? transform.Find("Lives") ?? transform.Find("HeaderContainer/LivesTMP");
            if (t != null) livesTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (scoreTMP == null) {
            Transform t = transform.Find("ScoreTMP") ?? transform.Find("Score") ?? transform.Find("HeaderContainer/ScoreTMP");
            if (t != null) scoreTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (progressTMP == null) {
            Transform t = transform.Find("ProgressTMP") ?? transform.Find("progression count") ?? transform.Find("Progress");
            if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (timerBarFillImage == null) {
            Transform t = transform.Find("TimerBarFill") ?? transform.Find("TimerBar/Fill") ?? transform.Find("QuestionTimerBar/Fill");
            if (t != null) timerBarFillImage = t.GetComponent<Image>();
        }

        // Chore corridor card
        if (choreCardObject == null) {
            Transform t = transform.Find("SymptomCard") ?? transform.Find("GoodsCard") ?? transform.Find("CardObject") ?? transform.Find("DeskCard") ?? transform.Find("ChoreCard");
            if (t != null) choreCardObject = t.gameObject;
        }
        if (choreTextTMP == null && choreCardObject != null) {
            Transform t = choreCardObject.transform.Find("SymptomTextTMP") ?? choreCardObject.transform.Find("GoodsTextTMP") ?? choreCardObject.transform.Find("Text") ?? choreCardObject.transform.Find("ChoreTextTMP");
            if (t != null) choreTextTMP = t.GetComponent<TextMeshProUGUI>();
        }

        // 4 Room Doors
        Transform doorsGrid = transform.Find("DoorsGrid") ?? transform.Find("OptionsContainer") ?? transform.Find("OptionsGrid");
        if (doorsGrid != null) {
            List<Button> btns = new List<Button>();
            List<Image> imgs = new List<Image>();
            List<TextMeshProUGUI> txts = new List<TextMeshProUGUI>();

            for (int i = 0; i < 4; i++) {
                Transform doorTr = doorsGrid.Find($"Door_{i}") ?? doorsGrid.Find($"Option_{i}") ?? doorsGrid.Find($"DoorButton_{i}") ?? ((i < doorsGrid.childCount) ? doorsGrid.GetChild(i) : null);
                if (doorTr != null) {
                    Button b = doorTr.GetComponent<Button>();
                    Image im = doorTr.GetComponent<Image>();
                    TextMeshProUGUI tx = doorTr.GetComponentInChildren<TextMeshProUGUI>(true);

                    if (b != null) btns.Add(b);
                    if (im != null) imgs.Add(im);
                    if (tx != null) txts.Add(tx);
                }
            }

            if (btns.Count == 4 && (doorButtons == null || doorButtons.Length != 4 || doorButtons[0] == null)) {
                doorButtons = btns.ToArray();
            }
            if (imgs.Count == 4 && (doorImages == null || doorImages.Length != 4 || doorImages[0] == null)) {
                doorImages = imgs.ToArray();
            }
            if (txts.Count == 4 && (doorTexts == null || doorTexts.Length != 4 || doorTexts[0] == null)) {
                doorTexts = txts.ToArray();
            }
        }

        // Result panel
        if (resultPanel == null) {
            Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("CompletionPanel");
            if (t != null) resultPanel = t.gameObject;
        }
        if (resultTitleTMP == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("ResultTitle") ?? resultPanel.transform.Find("Title");
            if (t != null) resultTitleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (resultScoreTMP == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("ResultScore") ?? resultPanel.transform.Find("Score");
            if (t != null) resultScoreTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (resultStatusTMP == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("ResultStatus") ?? resultPanel.transform.Find("Status");
            if (t != null) resultStatusTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (retryBtn == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("RetryButton") ?? resultPanel.transform.Find("RetryBtn");
            if (t != null) retryBtn = t.GetComponent<Button>();
        }
        if (returnHubBtn == null && resultPanel != null) {
            Transform t = resultPanel.transform.Find("ReturnHubButton") ?? resultPanel.transform.Find("ReturnHubBtn") ?? resultPanel.transform.Find("NextButton");
            if (t != null) returnHubBtn = t.GetComponent<Button>();
        }
    }
}
