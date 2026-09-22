using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// G01 Shop Dash — Four Doors, Fast
/// Timed sorting reaction game for Book 2B Unit 8 (Shopping Time).
/// Goods tiles slide down the arcade street; 4 shop doors at the bottom:
/// Door 0: BAKER'S 🥖
/// Door 1: CHEMIST 💊
/// Door 2: FLORIST 💐
/// Door 3: TOY SHOP 🧸
/// 3 lives, 60s total timer, speeds up over time.
/// Success condition: Student sorts at least 16 items correctly within time/lives.
/// </summary>
public class Masters_ShoppingTime_Game_LessonOne : Masters_Lesson {

[System.Serializable]
public class ShoppingTime_GameG01Goods {
    public int goodsId;
    public string goodsName;
    public int correctDoorIndex; // 0: BAKER'S, 1: CHEMIST, 2: FLORIST, 3: TOY SHOP
}

    [Header("G01 32 Shopping Goods Items")]
    [SerializeField] private ShoppingTime_GameG01Goods[] goodsItems;

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

    [Header("Goods Corridor Card")]
    [SerializeField] private GameObject goodsCardObject;
    [SerializeField] private TextMeshProUGUI goodsTextTMP;

    [Header("4 Shop Doors")]
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
    [SerializeField] private float initialQuestionDuration = 6.0f;

    private float remainingGameTime;
    private int currentLives;
    private int totalSorted = 0;
    private bool isGameActive = false;
    private bool isHandlingAnswer = false;
    private int currentGoodsIndex = 0;
    private float currentQuestionDuration = 6.0f;
    private float questionTimeRemaining = 6.0f;
    private List<int> shuffledIndices = new List<int>();

    private readonly string[] shopDoorNames = new string[] {
        "BAKER'S",
        "CHEMIST",
        "FLORIST",
        "TOY SHOP"
    };

    private readonly Color defaultDoorColor = new Color(0.12f, 0.35f, 0.65f, 0.95f);
    private readonly Color correctDoorColor = new Color(0.15f, 0.75f, 0.35f, 1f);
    private readonly Color wrongDoorColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Game;
        base.Awake();

        AutoBindReferences();
        InitGoodsIfEmpty();
        WireEventListeners();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Game;
        AutoBindReferences();
        WireEventListeners();
        EnsureNextAndBackButtonWired();

        if (introAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(introAudio);
        }

        float introDuration = (introAudio != null && introAudio.length > 0) ? introAudio.length + 0.3f : 1.0f;
        StartCoroutine(StartGameAfterIntro(introDuration));
    }

    private IEnumerator StartGameAfterIntro(float delay) {
        isGameActive = false;
        remainingGameTime = totalGameTime;
        currentLives = maxLives;
        totalSorted = 0;
        isHandlingAnswer = false;

        if (headerTMP != null) headerTMP.gameObject.SetActive(true);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (goodsCardObject != null) goodsCardObject.SetActive(true);

        Transform doorsGrid = transform.Find("DoorsGrid") ?? transform.Find("OptionsContainer") ?? transform.Find("OptionsGrid");
        if (doorsGrid != null) doorsGrid.gameObject.SetActive(true);

        UpdateScoreAndHUD();
        SetDoorLabels();

        if (doorButtons != null) {
            for (int i = 0; i < doorButtons.Length; i++) {
                if (doorButtons[i] != null) doorButtons[i].interactable = false;
            }
        }
        if (timerBarFillImage != null) timerBarFillImage.fillAmount = 1f;
        if (timerTMP != null) timerTMP.text = $"Time: {Mathf.CeilToInt(totalGameTime)}s";
        if (goodsItems != null && goodsItems.Length > 0 && goodsTextTMP != null) {
            goodsTextTMP.text = $"\"{goodsItems[0].goodsName}\"";
        }

        yield return new WaitForSeconds(delay);

        RestartGame();
    }

    private void Update() {
        if (!isGameActive || isHandlingAnswer) return;

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
        remainingGameTime = totalGameTime;
        currentLives = maxLives;
        totalSorted = 0;
        currentQuestionDuration = initialQuestionDuration;
        isHandlingAnswer = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (goodsCardObject != null) goodsCardObject.SetActive(true);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);

        Transform doorsGrid = transform.Find("DoorsGrid") ?? transform.Find("OptionsContainer") ?? transform.Find("OptionsGrid");
        if (doorsGrid != null) doorsGrid.gameObject.SetActive(true);

        ShuffleGoodsPool();
        currentGoodsIndex = 0;

        SetDoorLabels();
        UpdateScoreAndHUD();

        isGameActive = true;
        ShowCurrentGoods();
    }

    private void ShuffleGoodsPool() {
        shuffledIndices.Clear();
        if (goodsItems == null || goodsItems.Length == 0) return;

        for (int i = 0; i < goodsItems.Length; i++) {
            shuffledIndices.Add(i);
        }

        for (int i = 0; i < shuffledIndices.Count; i++) {
            int temp = shuffledIndices[i];
            int rand = Random.Range(i, shuffledIndices.Count);
            shuffledIndices[i] = shuffledIndices[rand];
            shuffledIndices[rand] = temp;
        }
    }

    private void ShowCurrentGoods() {
        if (!isGameActive || goodsItems == null || goodsItems.Length == 0) return;

        if (currentGoodsIndex >= shuffledIndices.Count) {
            ShuffleGoodsPool();
            currentGoodsIndex = 0;
        }

        int idx = shuffledIndices[currentGoodsIndex];
        ShoppingTime_GameG01Goods item = goodsItems[idx];

        // Speed increases gradually as player sorts more items
        currentQuestionDuration = Mathf.Max(2.5f, initialQuestionDuration - (totalSorted * 0.15f));
        questionTimeRemaining = currentQuestionDuration;

        if (goodsTextTMP != null) {
            goodsTextTMP.text = $"\"{item.goodsName}\"";
            goodsTextTMP.color = Color.white;
        }

        if (goodsCardObject != null) {
            goodsCardObject.transform.DOKill();
            goodsCardObject.transform.localScale = Vector3.one * 0.7f;
            goodsCardObject.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }

        ResetDoorColors();
        EnableDoors(true);
    }

    private void OnDoorClicked(int doorIndex) {
        if (!isGameActive || isHandlingAnswer) return;
        if (goodsItems == null || goodsItems.Length == 0) return;

        int idx = shuffledIndices[currentGoodsIndex];
        ShoppingTime_GameG01Goods currentItem = goodsItems[idx];

        bool isCorrect = (doorIndex == currentItem.correctDoorIndex);
        StartCoroutine(HandleAnswer(isCorrect, doorIndex));
    }

    private IEnumerator HandleAnswer(bool isCorrect, int clickedDoorIndex) {
        isHandlingAnswer = true;
        EnableDoors(false);

        int idx = shuffledIndices[currentGoodsIndex];
        ShoppingTime_GameG01Goods currentItem = goodsItems[idx];
        int correctDoor = currentItem.correctDoorIndex;

        if (isCorrect) {
            totalSorted++;
            UpdateScoreAndHUD();

            if (clickedDoorIndex >= 0 && clickedDoorIndex < doorImages.Length && doorImages[clickedDoorIndex] != null) {
                doorImages[clickedDoorIndex].color = correctDoorColor;
                doorImages[clickedDoorIndex].transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }

            if (goodsTextTMP != null) goodsTextTMP.color = correctDoorColor;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            yield return new WaitForSeconds(0.4f);
        } else {
            currentLives--;
            UpdateScoreAndHUD();

            if (clickedDoorIndex >= 0 && clickedDoorIndex < doorImages.Length && doorImages[clickedDoorIndex] != null) {
                doorImages[clickedDoorIndex].color = wrongDoorColor;
                doorImages[clickedDoorIndex].transform.DOShakePosition(0.4f, 8f, 15, 90, false, true);
            }

            // Highlight the correct door in green
            if (correctDoor >= 0 && correctDoor < doorImages.Length && doorImages[correctDoor] != null) {
                doorImages[correctDoor].color = correctDoorColor;
            }

            if (goodsCardObject != null) {
                goodsCardObject.transform.DOShakePosition(0.4f, 10f, 15, 90, false, true);
            }

            if (goodsTextTMP != null) goodsTextTMP.color = wrongDoorColor;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            yield return new WaitForSeconds(0.75f);
        }

        if (currentLives <= 0 || remainingGameTime <= 0f) {
            EndGame();
        } else {
            currentGoodsIndex++;
            isHandlingAnswer = false;
            ShowCurrentGoods();
        }
    }

    private void EndGame() {
        isGameActive = false;
        isHandlingAnswer = false;

        EnableDoors(false);
        if (goodsCardObject != null) goodsCardObject.SetActive(false);
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
                resultTitleTMP.text = passed ? "SHOP DASH COMPLETE!" : "TIME'S UP!";
                resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"You sorted {totalSorted} goods correctly! (Target: {targetMatchesToWin})";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "Success! All goods safely delivered to their rightful stores." : $"You need at least {targetMatchesToWin} correct sorts to complete this challenge.";
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
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Game;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
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
            if (doorTexts[i] != null && i < shopDoorNames.Length) {
                doorTexts[i].text = shopDoorNames[i];
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

    public void InitGoodsIfEmpty() {
        if (goodsItems != null && goodsItems.Length > 0) return;

        goodsItems = new ShoppingTime_GameG01Goods[] {
            // Door 0: BAKER'S 🥖
            new ShoppingTime_GameG01Goods { goodsId = 1, goodsName = "Fresh Bread", correctDoorIndex = 0 },
            new ShoppingTime_GameG01Goods { goodsId = 2, goodsName = "Chocolate Cake", correctDoorIndex = 0 },
            new ShoppingTime_GameG01Goods { goodsId = 3, goodsName = "Butter Croissants", correctDoorIndex = 0 },
            new ShoppingTime_GameG01Goods { goodsId = 4, goodsName = "Warm Baguette", correctDoorIndex = 0 },
            new ShoppingTime_GameG01Goods { goodsId = 5, goodsName = "Blueberry Muffins", correctDoorIndex = 0 },
            new ShoppingTime_GameG01Goods { goodsId = 6, goodsName = "Cinnamon Buns", correctDoorIndex = 0 },
            new ShoppingTime_GameG01Goods { goodsId = 7, goodsName = "Pastries", correctDoorIndex = 0 },
            new ShoppingTime_GameG01Goods { goodsId = 8, goodsName = "Butter Cookies", correctDoorIndex = 0 },

            // Door 1: CHEMIST 💊
            new ShoppingTime_GameG01Goods { goodsId = 9, goodsName = "Cough Medicine", correctDoorIndex = 1 },
            new ShoppingTime_GameG01Goods { goodsId = 10, goodsName = "Bandages & Cotton", correctDoorIndex = 1 },
            new ShoppingTime_GameG01Goods { goodsId = 11, goodsName = "Vitamin Tablets", correctDoorIndex = 1 },
            new ShoppingTime_GameG01Goods { goodsId = 12, goodsName = "Painkiller Pills", correctDoorIndex = 1 },
            new ShoppingTime_GameG01Goods { goodsId = 13, goodsName = "Antiseptic Ointment", correctDoorIndex = 1 },
            new ShoppingTime_GameG01Goods { goodsId = 14, goodsName = "First Aid Kit", correctDoorIndex = 1 },
            new ShoppingTime_GameG01Goods { goodsId = 15, goodsName = "Digital Thermometer", correctDoorIndex = 1 },
            new ShoppingTime_GameG01Goods { goodsId = 16, goodsName = "Eye Drops", correctDoorIndex = 1 },

            // Door 2: FLORIST 💐
            new ShoppingTime_GameG01Goods { goodsId = 17, goodsName = "Red Roses", correctDoorIndex = 2 },
            new ShoppingTime_GameG01Goods { goodsId = 18, goodsName = "Sunflowers Bouquet", correctDoorIndex = 2 },
            new ShoppingTime_GameG01Goods { goodsId = 19, goodsName = "Pink Tulips", correctDoorIndex = 2 },
            new ShoppingTime_GameG01Goods { goodsId = 20, goodsName = "White Lilies", correctDoorIndex = 2 },
            new ShoppingTime_GameG01Goods { goodsId = 21, goodsName = "Potted Orchid", correctDoorIndex = 2 },
            new ShoppingTime_GameG01Goods { goodsId = 22, goodsName = "Flower Garland", correctDoorIndex = 2 },
            new ShoppingTime_GameG01Goods { goodsId = 23, goodsName = "Fresh Carnations", correctDoorIndex = 2 },
            new ShoppingTime_GameG01Goods { goodsId = 24, goodsName = "Floral Wreath", correctDoorIndex = 2 },

            // Door 3: TOY SHOP 🧸
            new ShoppingTime_GameG01Goods { goodsId = 25, goodsName = "Teddy Bear", correctDoorIndex = 3 },
            new ShoppingTime_GameG01Goods { goodsId = 26, goodsName = "Board Game", correctDoorIndex = 3 },
            new ShoppingTime_GameG01Goods { goodsId = 27, goodsName = "Remote Toy Car", correctDoorIndex = 3 },
            new ShoppingTime_GameG01Goods { goodsId = 28, goodsName = "Building Blocks", correctDoorIndex = 3 },
            new ShoppingTime_GameG01Goods { goodsId = 29, goodsName = "Jigsaw Puzzle", correctDoorIndex = 3 },
            new ShoppingTime_GameG01Goods { goodsId = 30, goodsName = "Action Hero Figure", correctDoorIndex = 3 },
            new ShoppingTime_GameG01Goods { goodsId = 31, goodsName = "Wooden Train Set", correctDoorIndex = 3 },
            new ShoppingTime_GameG01Goods { goodsId = 32, goodsName = "Spinning Yo-yo", correctDoorIndex = 3 }
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

        // Goods corridor card
        if (goodsCardObject == null) {
            Transform t = transform.Find("SymptomCard") ?? transform.Find("GoodsCard") ?? transform.Find("CardObject") ?? transform.Find("DeskCard");
            if (t != null) goodsCardObject = t.gameObject;
        }
        if (goodsTextTMP == null && goodsCardObject != null) {
            Transform t = goodsCardObject.transform.Find("SymptomTextTMP") ?? goodsCardObject.transform.Find("GoodsTextTMP") ?? goodsCardObject.transform.Find("Text");
            if (t != null) goodsTextTMP = t.GetComponent<TextMeshProUGUI>();
        }

        // 4 Shop Doors
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
