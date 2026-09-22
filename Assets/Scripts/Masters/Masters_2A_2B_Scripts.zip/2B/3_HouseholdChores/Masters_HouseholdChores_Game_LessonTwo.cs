using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Unit 3: Household Chores — Game Lesson Two (G02 Guess My Action).
/// Mime recognition against the clock:
/// - LEO picks a chit and mimes the chore in silence.
/// - 4 chore-sentence options appear with a 10-second timer bar.
/// - Correct -> green highlight, cheers, correct SFX, ARIA reads sentence aloud.
/// - Wrong or timeout -> red shake, highlights correct answer, re-reads sentence.
/// - Pass threshold: Identify at least 8 of 10 mimed chores.
/// </summary>
public class Masters_HouseholdChores_Game_LessonTwo : Masters_Lesson {

[System.Serializable]
public class HouseholdChores_GameG02Item {
    public int itemId;
    public string mimeDescription;     // e.g. "[LEO holds an imaginary broom and sweeps side to side across the floor]"
    public string[] options;           // 4 Options A, B, C, D
    public int correctOptionIndex;     // 0..3
    public AudioClip readbackAudio;    // ARIA model reading the chore sentence
}

    [Header("G02 10 Mime Items")]
    [SerializeField] public HouseholdChores_GameG02Item[] items;

    [Header("UI Display References")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI timerTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI scoreTMP;

    [Header("Question Timer Bar")]
    [SerializeField] private Image timerBarFillImage;

    [Header("Mime Card Object")]
    [SerializeField] private GameObject mimeCardObject;
    [SerializeField] private TextMeshProUGUI mimeTextTMP;

    [Header("4 Option Buttons")]
    [SerializeField] private Button[] optionButtons; // 4 Option Cards
    [SerializeField] private Image[] optionImages;
    [SerializeField] private TextMeshProUGUI[] optionTexts;

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
    [SerializeField] private float questionDuration = 10.0f;
    [SerializeField] private int passScore = 8;

    private int currentItemIndex = 0;
    private int score = 0;
    private float questionTimeRemaining = 10.0f;
    private bool isGameActive = false;
    private bool isHandlingAnswer = false;
    private bool isIntroPhase = true;
    private Coroutine introRoutine;

    private readonly Color defaultOptionColor = new Color(0.12f, 0.35f, 0.65f, 0.95f);
    private readonly Color correctOptionColor = new Color(0.15f, 0.75f, 0.35f, 1f);
    private readonly Color wrongOptionColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Game;
        base.Awake();

        AutoBindReferences();
        InitItemsIfEmpty();
        WireEventListeners();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Game;

        AutoBindReferences();
        WireEventListeners();
        EnsureNextAndBackButtonWired();
        EnsureHeaderAndTitle();

        if (items == null || items.Length == 0) {
            InitItemsIfEmpty();
        }

        // Lock controls during introduction
        isIntroPhase = true;
        isGameActive = false;
        EnableOptions(false);

        introRoutine = StartCoroutine(StartGameAfterIntroRoutine());
    }

    private IEnumerator StartGameAfterIntroRoutine() {
        currentItemIndex = 0;
        score = 0;
        isHandlingAnswer = false;

        if (headerTMP != null) headerTMP.gameObject.SetActive(true);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (mimeCardObject != null) mimeCardObject.SetActive(true);

        Transform optionsContainer = transform.Find("OptionsContainer") ?? transform.Find("DoorsGrid") ?? transform.Find("OptionsGrid");
        if (optionsContainer != null) optionsContainer.gameObject.SetActive(true);

        UpdateHUD();

        if (timerBarFillImage != null) timerBarFillImage.fillAmount = 1f;
        if (timerTMP != null) timerTMP.text = $"Time: {Mathf.CeilToInt(questionDuration)}s";
        if (items != null && items.Length > 0 && mimeTextTMP != null) {
            mimeTextTMP.text = items[0].mimeDescription;
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
            titleTMP.text = "G02 Guess My Action";
            titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f);
        }
        if (subtitleTMP != null) subtitleTMP.text = "Watch LEO mime the chore, then tap the matching chore sentence!";
    }

    private void Update() {
        if (!isGameActive || isHandlingAnswer || isIntroPhase) return;

        questionTimeRemaining -= Time.deltaTime;
        if (timerTMP != null) {
            timerTMP.text = $"Time: {Mathf.Max(0f, Mathf.Ceil(questionTimeRemaining))}s";
        }

        if (timerBarFillImage != null && questionDuration > 0f) {
            timerBarFillImage.fillAmount = Mathf.Clamp01(questionTimeRemaining / questionDuration);
        }

        if (questionTimeRemaining <= 0f) {
            StartCoroutine(HandleAnswer(false, -1));
        }
    }

    private void WireEventListeners() {
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                int optionIdx = i;
                if (optionButtons[i] != null) {
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionClicked(optionIdx));
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

        currentItemIndex = 0;
        score = 0;
        isHandlingAnswer = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (mimeCardObject != null) mimeCardObject.SetActive(true);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(true);

        Transform optionsContainer = transform.Find("OptionsContainer") ?? transform.Find("DoorsGrid") ?? transform.Find("OptionsGrid");
        if (optionsContainer != null) optionsContainer.gameObject.SetActive(true);

        isGameActive = true;
        ShowItem(0);
    }

    public void ShowItem(int index) {
        if (items == null || items.Length == 0) return;
        currentItemIndex = Mathf.Clamp(index, 0, items.Length - 1);
        isHandlingAnswer = false;

        HouseholdChores_GameG02Item item = items[currentItemIndex];
        questionTimeRemaining = questionDuration;

        UpdateHUD();
        EnsureHeaderAndTitle();

        if (mimeTextTMP != null) {
            mimeTextTMP.text = item.mimeDescription;
            mimeTextTMP.color = Color.white;
        }

        if (mimeCardObject != null) {
            mimeCardObject.transform.DOKill();
            mimeCardObject.transform.localScale = Vector3.one * 0.7f;
            mimeCardObject.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }

        // Set Option Texts
        if (optionTexts != null && item.options != null) {
            for (int i = 0; i < optionTexts.Length; i++) {
                if (optionTexts[i] != null && i < item.options.Length) {
                    optionTexts[i].text = item.options[i];
                }
            }
        }

        ResetOptionColors();
        EnableOptions(true);
    }

    private void OnOptionClicked(int optionIndex) {
        if (!isGameActive || isHandlingAnswer || isIntroPhase) return;
        if (items == null || currentItemIndex >= items.Length) return;

        HouseholdChores_GameG02Item item = items[currentItemIndex];
        bool isCorrect = (optionIndex == item.correctOptionIndex);

        StartCoroutine(HandleAnswer(isCorrect, optionIndex));
    }

    private IEnumerator HandleAnswer(bool isCorrect, int clickedIndex) {
        isHandlingAnswer = true;
        EnableOptions(false);

        HouseholdChores_GameG02Item item = items[currentItemIndex];
        int correctIndex = item.correctOptionIndex;

        if (isCorrect) {
            score++;
            UpdateHUD();

            if (clickedIndex >= 0 && clickedIndex < optionImages.Length && optionImages[clickedIndex] != null) {
                optionImages[clickedIndex].color = correctOptionColor;
                optionImages[clickedIndex].transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }

            if (mimeTextTMP != null) {
                mimeTextTMP.text = $"<color=#33D866><b>✓ {item.options[correctIndex]}</b></color>";
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                if (item.readbackAudio != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(item.readbackAudio);
                }
            }

            yield return new WaitForSeconds(2.0f);
        } else {
            if (clickedIndex >= 0 && clickedIndex < optionImages.Length && optionImages[clickedIndex] != null) {
                optionImages[clickedIndex].color = wrongOptionColor;
                optionImages[clickedIndex].transform.DOShakePosition(0.4f, 8f, 15, 90, false, true);
            }

            // Reveal correct answer in green
            if (correctIndex >= 0 && correctIndex < optionImages.Length && optionImages[correctIndex] != null) {
                optionImages[correctIndex].color = correctOptionColor;
            }

            if (mimeCardObject != null) {
                mimeCardObject.transform.DOShakePosition(0.4f, 10f, 15, 90, false, true);
            }

            if (mimeTextTMP != null) {
                mimeTextTMP.text = $"<color=#FFD80D><b>Answer: {item.options[correctIndex]}</b></color>";
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                if (item.readbackAudio != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(item.readbackAudio);
                }
            }

            yield return new WaitForSeconds(2.2f);
        }

        if (currentItemIndex < items.Length - 1) {
            ShowItem(currentItemIndex + 1);
        } else {
            EndGame();
        }
    }

    private void EndGame() {
        isGameActive = false;
        isHandlingAnswer = false;

        EnableOptions(false);
        if (mimeCardObject != null) mimeCardObject.SetActive(false);
        if (subtitleTMP != null) subtitleTMP.gameObject.SetActive(false);

        Transform optionsContainer = transform.Find("OptionsContainer") ?? transform.Find("DoorsGrid") ?? transform.Find("OptionsGrid");
        if (optionsContainer != null) optionsContainer.gameObject.SetActive(false);

        bool passed = (score >= passScore);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

            if (resultTitleTMP != null) {
                resultTitleTMP.text = passed ? "GUESS MY ACTION COMPLETE! 🎭" : "KEEP PRACTICING!";
                resultTitleTMP.color = passed ? new Color(0.2f, 0.85f, 0.35f, 1f) : new Color(0.95f, 0.4f, 0.2f, 1f);
            }

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"You identified {score} of {items.Length} mimed chores correctly!";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "Success! You correctly recognised LEO's chore actions." : $"You need at least {passScore}/{items.Length} correct to complete this game.";
            }

            if (returnHubBtn != null) {
                returnHubBtn.gameObject.SetActive(passed);
            }

            if (retryBtn != null) {
                retryBtn.gameObject.SetActive(!passed || score < items.Length);
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

    private void UpdateHUD() {
        if (progressTMP != null) progressTMP.text = $"Mime: {currentItemIndex + 1}/{items.Length}";
        if (scoreTMP != null) scoreTMP.text = $"Score: {score}";
    }

    private void ResetOptionColors() {
        if (optionImages == null) return;
        for (int i = 0; i < optionImages.Length; i++) {
            if (optionImages[i] != null) {
                optionImages[i].color = defaultOptionColor;
                optionImages[i].transform.localScale = Vector3.one;
            }
        }
    }

    private void EnableOptions(bool enable) {
        if (optionButtons == null) return;
        for (int i = 0; i < optionButtons.Length; i++) {
            if (optionButtons[i] != null) optionButtons[i].interactable = enable;
        }
    }

    public void InitItemsIfEmpty() {
        if (items != null && items.Length > 0) return;

        string aDir = "Assets/Audio/2B/3_HouseholdChores/Game/";

        items = new HouseholdChores_GameG02Item[] {
            new HouseholdChores_GameG02Item {
                itemId = 1,
                mimeDescription = "[LEO holds an imaginary broom and sweeps side to side across the floor]",
                options = new string[] { "Sweep the floor", "Wash the car", "Make the bed", "Take the trash out" },
                correctOptionIndex = 0
#if UNITY_EDITOR
                , readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_g02_m01_sweep.mp3")
#endif
            },
            new HouseholdChores_GameG02Item {
                itemId = 2,
                mimeDescription = "[LEO scrubs a plate with a sponge under running water, then dries it]",
                options = new string[] { "Clean the window", "Wash up the dishes", "Iron the clothes", "Feed the dog" },
                correctOptionIndex = 1
#if UNITY_EDITOR
                , readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_g02_m02_dishes.mp3")
#endif
            },
            new HouseholdChores_GameG02Item {
                itemId = 3,
                mimeDescription = "[LEO pulls and smooths out an imaginary bedsheet, then fluffs a pillow]",
                options = new string[] { "Do the gardening", "Set the table", "Make the bed", "Hang out the clothes" },
                correctOptionIndex = 2
#if UNITY_EDITOR
                , readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_g02_m03_bed.mp3")
#endif
            },
            new HouseholdChores_GameG02Item {
                itemId = 4,
                mimeDescription = "[LEO places plates, forks, spoons, and glasses neatly on a dining table]",
                options = new string[] { "Dust the floor", "Do the shopping", "Dry the dishes", "Set the table" },
                correctOptionIndex = 3
#if UNITY_EDITOR
                , readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_g02_m04_table.mp3")
#endif
            },
            new HouseholdChores_GameG02Item {
                itemId = 5,
                mimeDescription = "[LEO presses an imaginary warm iron smoothly back and forth over a shirt]",
                options = new string[] { "Iron the clothes", "Feed the dog", "Wash the car", "Mop the floor" },
                correctOptionIndex = 0
#if UNITY_EDITOR
                , readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_g02_m05_iron.mp3")
#endif
            },
            new HouseholdChores_GameG02Item {
                itemId = 6,
                mimeDescription = "[LEO shakes out wet clothes from a basket and pegs them onto a washing line]",
                options = new string[] { "Take the trash out", "Hang out the clothes", "Clean the window", "Do the gardening" },
                correctOptionIndex = 1
#if UNITY_EDITOR
                , readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_g02_m06_hang.mp3")
#endif
            },
            new HouseholdChores_GameG02Item {
                itemId = 7,
                mimeDescription = "[LEO ties a heavy garbage bag, lifts it up, and carries it outside]",
                options = new string[] { "Wash up the dishes", "Iron the clothes", "Take the trash out", "Sweep the floor" },
                correctOptionIndex = 2
#if UNITY_EDITOR
                , readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_g02_m07_trash.mp3")
#endif
            },
            new HouseholdChores_GameG02Item {
                itemId = 8,
                mimeDescription = "[LEO opens a dog food can, pours biscuits into a bowl, and pets a puppy]",
                options = new string[] { "Make the bed", "Clean the window", "Do the shopping", "Feed the dog" },
                correctOptionIndex = 3
#if UNITY_EDITOR
                , readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_g02_m08_feed.mp3")
#endif
            },
            new HouseholdChores_GameG02Item {
                itemId = 9,
                mimeDescription = "[LEO dips a large sponge into soapy water and scrubs the windshield of a car]",
                options = new string[] { "Wash the car", "Set the table", "Hang out the clothes", "Sweep the floor" },
                correctOptionIndex = 0
#if UNITY_EDITOR
                , readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_g02_m09_car.mp3")
#endif
            },
            new HouseholdChores_GameG02Item {
                itemId = 10,
                mimeDescription = "[LEO sprays a glass bottle and wipes a window pane in circular motions]",
                options = new string[] { "Iron the clothes", "Clean the window", "Take the trash out", "Dry the dishes" },
                correctOptionIndex = 1
#if UNITY_EDITOR
                , readbackAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_g02_m10_window.mp3")
#endif
            }
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

        // Mime corridor card
        if (mimeCardObject == null) {
            Transform t = transform.Find("SymptomCard") ?? transform.Find("GoodsCard") ?? transform.Find("CardObject") ?? transform.Find("DeskCard") ?? transform.Find("IdiomCard") ?? transform.Find("ChoreCard");
            if (t != null) mimeCardObject = t.gameObject;
        }
        if (mimeTextTMP == null && mimeCardObject != null) {
            Transform t = mimeCardObject.transform.Find("SymptomTextTMP") ?? mimeCardObject.transform.Find("GoodsTextTMP") ?? mimeCardObject.transform.Find("Text") ?? mimeCardObject.transform.Find("IdiomTextTMP") ?? mimeCardObject.transform.Find("ChoreTextTMP");
            if (t != null) mimeTextTMP = t.GetComponent<TextMeshProUGUI>();
        }

        // 4 Options
        Transform optionsContainer = transform.Find("OptionsContainer") ?? transform.Find("DoorsGrid") ?? transform.Find("OptionsGrid");
        if (optionsContainer != null) {
            List<Button> btns = new List<Button>();
            List<Image> imgs = new List<Image>();
            List<TextMeshProUGUI> txts = new List<TextMeshProUGUI>();

            for (int i = 0; i < 4; i++) {
                Transform opTr = optionsContainer.Find($"OptionCard_{i+1}") ?? optionsContainer.Find($"Door_{i}") ?? optionsContainer.Find($"Option_{i}") ?? optionsContainer.Find($"DoorButton_{i}") ?? ((i < optionsContainer.childCount) ? optionsContainer.GetChild(i) : null);
                if (opTr != null) {
                    Button b = opTr.GetComponent<Button>();
                    Image im = opTr.GetComponent<Image>();
                    TextMeshProUGUI tx = opTr.GetComponentInChildren<TextMeshProUGUI>(true);

                    if (b != null) btns.Add(b);
                    if (im != null) imgs.Add(im);
                    if (tx != null) txts.Add(tx);
                }
            }

            if (btns.Count == 4 && (optionButtons == null || optionButtons.Length != 4 || optionButtons[0] == null)) {
                optionButtons = btns.ToArray();
            }
            if (imgs.Count == 4 && (optionImages == null || optionImages.Length != 4 || optionImages[0] == null)) {
                optionImages = imgs.ToArray();
            }
            if (txts.Count == 4 && (optionTexts == null || optionTexts.Length != 4 || optionTexts[0] == null)) {
                optionTexts = txts.ToArray();
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
