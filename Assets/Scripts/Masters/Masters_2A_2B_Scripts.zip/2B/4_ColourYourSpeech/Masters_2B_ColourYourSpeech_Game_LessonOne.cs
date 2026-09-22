using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// G01 Meaning Rush
/// Arcade easel reaction game for Book 2B Unit 4 (Colour Your Speech).
/// An idiom appears at the top; 3 meaning cards sit at the bottom.
/// A paint-drip timer bar ticks down (6s shrinking to 4s as rounds speed up).
/// 3 lives, 60s total timer, speeds up over time.
/// Success condition: Student matches at least 16 idioms to the correct meaning within the time/lives.
/// </summary>
public class Masters_2B_ColourYourSpeech_Game_LessonOne : Masters_Lesson {

[System.Serializable]
public class ColourYourSpeech_GameG01IdiomPair {
    public int pairId;
    public string idiomText;              // e.g. "Bite off more than you can chew"
    public string correctMeaning;         // e.g. "To take on a task that is too big"
    public string[] distractorMeanings;   // 2 distractors from other idioms
    public AudioClip idiomAudioClip;      // Spoken voiceover of the idiom
}

    [Header("G01 10 Verbatim Idiom Pairs")]
    [SerializeField]
    private ColourYourSpeech_GameG01IdiomPair[] idiomPairs;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI subtitleTMP;
    [SerializeField]
    private TextMeshProUGUI timerTMP;
    [SerializeField]
    private TextMeshProUGUI livesTMP;
    [SerializeField]
    private TextMeshProUGUI scoreTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;

    [Header("Paint-Drip Timer Bar")]
    [SerializeField]
    private Image timerBarFillImage;

    [Header("Idiom Easel Card")]
    [SerializeField]
    private GameObject idiomCardObject;
    [SerializeField]
    private TextMeshProUGUI idiomTextTMP;

    [Header("3 Meaning Option Cards")]
    [SerializeField]
    private Button[] meaningButtons; // 3 Chips
    [SerializeField]
    private Image[] meaningButtonImages;
    [SerializeField]
    private TextMeshProUGUI[] meaningButtonTexts;

    [Header("Feedback Banner")]
    [SerializeField]
    private GameObject feedbackBanner;
    [SerializeField]
    private TextMeshProUGUI feedbackTextTMP;

    [Header("Results & Retry Panel")]
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
    [SerializeField]
    private Button returnHubBtn;

    [Header("Audio References")]
    [SerializeField]
    private AudioClip introAudio;
    [SerializeField]
    private AudioClip sfxCorrect;
    [SerializeField]
    private AudioClip sfxWrong;
    [SerializeField]
    private AudioClip sfxCelebration;

    [Header("Game Settings")]
    [SerializeField]
    private float totalGameTime = 60f;
    [SerializeField]
    private int maxLives = 3;
    [SerializeField]
    private int targetMatchesToWin = 16;
    [SerializeField]
    private float initialQuestionDuration = 6.0f;
    [SerializeField]
    private float minimumQuestionDuration = 4.0f;

    [Header("Navigation")]
    [SerializeField]
    private Button backButton;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;

    private float totalTimeRemaining = 60f;
    private float questionTimeRemaining = 6.0f;
    private float currentQuestionDuration = 6.0f;
    private int livesRemaining = 3;
    private int score = 0;
    private int matchesCompleted = 0;
    private bool isGameRunning = false;
    private bool isAnswering = false;
    private bool isIntroPlaying = false;

    private int currentPairIndex = 0;
    private int correctOptionIndex = 0;
    private string[] currentOptions = new string[3];

    private readonly Color defaultChipColor = new Color(0.12f, 0.22f, 0.42f, 0.95f);
    private readonly Color correctColor = new Color(0.13f, 0.65f, 0.32f, 1f);
    private readonly Color wrongColor = new Color(0.82f, 0.2f, 0.2f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Game;
        base.Awake();

        PurgeLegacyChildren();
        AutoBindReferences();
        WireButtonEvents();

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }

        if (feedbackBanner != null) {
            feedbackBanner.SetActive(false);
        }
    }

    protected override void Start() {
        topic = Masters_Topic.Game;
        base.Start();

        PurgeLegacyChildren();
        EnsureHeaderAndTitle();
        AutoBindReferences();
        WireButtonEvents();

        if (idiomPairs == null || idiomPairs.Length == 0) {
            PopulateFailsafePairs();
        }

        StartCoroutine(PlayIntroAndStartGame());
    }

    private void WireButtonEvents() {
        if (meaningButtons != null && meaningButtons.Length > 0) {
            meaningButtonImages = new Image[meaningButtons.Length];
            meaningButtonTexts = new TextMeshProUGUI[meaningButtons.Length];
            for (int i = 0; i < meaningButtons.Length; i++) {
                if (meaningButtons[i] != null) {
                    int index = i;
                    meaningButtonImages[i] = meaningButtons[i].GetComponent<Image>();
                    meaningButtonTexts[i] = meaningButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    meaningButtons[i].onClick.RemoveAllListeners();
                    meaningButtons[i].onClick.AddListener(() => OnOptionClicked(index));
                }
            }
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(() => StartNewGame());
        }

        if (returnHubBtn != null) {
            returnHubBtn.onClick.RemoveAllListeners();
            returnHubBtn.onClick.AddListener(OnReturnHubClicked);
        }

        if (nextButton != null) {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        if (backButton != null) {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
    }

    private IEnumerator PlayIntroAndStartGame() {
        isIntroPlaying = true;
        isGameRunning = false;
        isAnswering = true;

        SetOptionsInteractable(false);
        UpdateHUD();

        if (idiomPairs != null && idiomPairs.Length > 0 && idiomTextTMP != null) {
            idiomTextTMP.text = $"\"{idiomPairs[0].idiomText}\"";
        }

        AudioClip introClip = introAudio != null ? introAudio : narratorSpeech;
        if (introClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(introClip);
            yield return new WaitForSeconds(introClip.length + 0.3f);
        } else {
            yield return new WaitForSeconds(0.4f);
        }

        isIntroPlaying = false;
        StartNewGame();
    }

    private void Update() {
        if (!isGameRunning || isAnswering || isIntroPlaying) return;

        // Total game countdown
        totalTimeRemaining -= Time.deltaTime;
        if (timerTMP != null) {
            timerTMP.text = $"Time: {Mathf.Max(0f, Mathf.Ceil(totalTimeRemaining))}s";
        }

        // Per-question paint drip countdown
        questionTimeRemaining -= Time.deltaTime;
        if (timerBarFillImage != null) {
            timerBarFillImage.fillAmount = Mathf.Clamp01(questionTimeRemaining / currentQuestionDuration);
        }

        if (questionTimeRemaining <= 0f) {
            OnTimeOut();
        } else if (totalTimeRemaining <= 0f) {
            EndGame(matchesCompleted >= targetMatchesToWin);
        }
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        AutoBindReferences();

        if (idiomPairs == null || idiomPairs.Length == 0) {
            PopulateFailsafePairs();
        }

        if (headerTMP != null) headerTMP.text = "GAME BRANCH (Arcade Easel)";
        if (titleTMP != null) titleTMP.text = "G01 Meaning Rush";
        if (subtitleTMP != null) subtitleTMP.text = "Tap the correct book meaning before the paint drips to the bottom!";
        if (timerTMP != null) timerTMP.text = "Time: 60s";
        if (livesTMP != null) livesTMP.text = "Lives: 3";
        if (scoreTMP != null) scoreTMP.text = "Score: 0";
        if (progressTMP != null) progressTMP.text = "Matches: 0/16";

        if (idiomPairs != null && idiomPairs.Length > 0 && idiomTextTMP != null) {
            idiomTextTMP.text = $"\"{idiomPairs[0].idiomText}\"";
        }
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer",
            "SpawnArea", "GamePlayGameObject", "LeftSpawnArea", "RightSpawnArea"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                if (Application.isPlaying) Destroy(lTrans.gameObject);
                else DestroyImmediate(lTrans.gameObject);
            }
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch") ?? transform.Find("Heading");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "GAME BRANCH (Arcade Easel)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "G01 Meaning Rush";
        }

        if (subtitleTMP == null) {
            Transform sTrans = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Instruction") ?? transform.Find("Subtitle");
            if (sTrans != null) subtitleTMP = sTrans.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP != null) {
            subtitleTMP.text = "Tap the correct book meaning before the paint drips to the bottom!";
        }
    }

    private void AutoBindReferences() {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading"))) headerTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (subtitleTMP == null && (n.Contains("subtitle") || n.Contains("instruction"))) subtitleTMP = tmp;
            else if (timerTMP == null && n.Contains("timer")) timerTMP = tmp;
            else if (livesTMP == null && n.Contains("lives")) livesTMP = tmp;
            else if (scoreTMP == null && n.Contains("score")) scoreTMP = tmp;
            else if (progressTMP == null && (n.Contains("progress") || n.Contains("matchcount"))) progressTMP = tmp;
            else if (idiomTextTMP == null && (n.Contains("idiomtext") || n.Contains("prompttext"))) idiomTextTMP = tmp;
            else if (feedbackTextTMP == null && n.Contains("feedback")) feedbackTextTMP = tmp;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> optBtns = new List<Button>();
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("option") || n.Contains("card") || n.Contains("chip")) {
                optBtns.Add(btn);
            } else if (retryBtn == null && n.Contains("retry")) {
                retryBtn = btn;
            } else if (returnHubBtn == null && (n.Contains("hub") || n.Contains("home"))) {
                returnHubBtn = btn;
            } else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) {
                nextButton = btn;
            } else if (backButton == null && (n.Contains("back") || n.Contains("prev"))) {
                backButton = btn;
            }
        }

        if (optBtns.Count > 0) {
            meaningButtons = optBtns.ToArray();
            meaningButtonImages = new Image[meaningButtons.Length];
            meaningButtonTexts = new TextMeshProUGUI[meaningButtons.Length];
            for (int i = 0; i < meaningButtons.Length; i++) {
                meaningButtonImages[i] = meaningButtons[i].GetComponent<Image>();
                meaningButtonTexts[i] = meaningButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        if (timerBarFillImage == null) {
            Transform tbTrans = transform.Find("TimerBar/Fill") ?? transform.Find("HeaderContainer/TimerBar/Fill");
            if (tbTrans != null) timerBarFillImage = tbTrans.GetComponent<Image>();
        }

        if (idiomCardObject == null) {
            Transform icTrans = transform.Find("IdiomCard") ?? transform.Find("PromptCard") ?? transform.Find("EaselCard");
            if (icTrans != null) idiomCardObject = icTrans.gameObject;
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("ResultPopup");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    public void PopulateFailsafePairs() {
        idiomPairs = new ColourYourSpeech_GameG01IdiomPair[] {
            new ColourYourSpeech_GameG01IdiomPair {
                pairId = 1,
                idiomText = "Bite off more than you can chew",
                correctMeaning = "To take on a task that is too big",
                distractorMeanings = new string[] { "To enjoy a lot", "A job task that is easy" }
            },
            new ColourYourSpeech_GameG01IdiomPair {
                pairId = 2,
                idiomText = "Piece of cake",
                correctMeaning = "A job task or activity that is easy or simple to do",
                distractorMeanings = new string[] { "Narrowly escape a disaster", "Raining heavily" }
            },
            new ColourYourSpeech_GameG01IdiomPair {
                pairId = 3,
                idiomText = "Have a blast",
                correctMeaning = "To enjoy a lot",
                distractorMeanings = new string[] { "To miss a chance", "The secret is given away" }
            },
            new ColourYourSpeech_GameG01IdiomPair {
                pairId = 4,
                idiomText = "Miss the boat",
                correctMeaning = "To miss a chance",
                distractorMeanings = new string[] { "To make up with someone after an argument", "You help me and I help you" }
            },
            new ColourYourSpeech_GameG01IdiomPair {
                pairId = 5,
                idiomText = "The cat is out of the bag",
                correctMeaning = "the secret is given away",
                distractorMeanings = new string[] { "To take on a task that is too big", "To enjoy a lot" }
            },
            new ColourYourSpeech_GameG01IdiomPair {
                pairId = 6,
                idiomText = "Raining cats and dogs",
                correctMeaning = "raining heavily",
                distractorMeanings = new string[] { "A simple activity to do", "Narrowly escape a disaster" }
            },
            new ColourYourSpeech_GameG01IdiomPair {
                pairId = 7,
                idiomText = "Bury the hatchet",
                correctMeaning = "To make up with someone after an argument",
                distractorMeanings = new string[] { "The secret is given away", "To miss a chance" }
            },
            new ColourYourSpeech_GameG01IdiomPair {
                pairId = 8,
                idiomText = "A close shave",
                correctMeaning = "narrowly escape a disaster",
                distractorMeanings = new string[] { "To enjoy a lot", "Raining heavily" }
            },
            new ColourYourSpeech_GameG01IdiomPair {
                pairId = 9,
                idiomText = "You scratch my back I will scratch yours",
                correctMeaning = "You help me and I will help you",
                distractorMeanings = new string[] { "A task that is too big", "To make up after an argument" }
            },
            new ColourYourSpeech_GameG01IdiomPair {
                pairId = 10,
                idiomText = "As you make your bed, so you must lie in it too.",
                correctMeaning = "Be prepared to face the consequences of your wrong actions",
                distractorMeanings = new string[] { "To miss an opportunity", "To enjoy a lot" }
            }
        };
    }

    public void StartNewGame() {
        totalTimeRemaining = totalGameTime;
        livesRemaining = maxLives;
        score = 0;
        matchesCompleted = 0;
        isGameRunning = true;
        isAnswering = false;

        if (meaningButtons == null || meaningButtons.Length == 0) {
            AutoBindReferences();
            WireButtonEvents();
        }

        if (resultPanel != null) resultPanel.SetActive(false);
        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        UpdateHUD();
        LoadNextRound();
    }

    private void SetOptionsInteractable(bool interactable) {
        if (meaningButtons != null) {
            for (int i = 0; i < meaningButtons.Length; i++) {
                if (meaningButtons[i] != null) {
                    meaningButtons[i].interactable = interactable;
                }
            }
        }
    }

    private void LoadNextRound() {
        if (livesRemaining <= 0 || totalTimeRemaining <= 0f) {
            EndGame(matchesCompleted >= targetMatchesToWin);
            return;
        }

        if (idiomPairs == null || idiomPairs.Length == 0) return;

        isAnswering = false;

        // Dynamic speed up: shrinks from 6s down to 4s as matches progress
        float speedFactor = Mathf.Clamp01(matchesCompleted / 16f);
        currentQuestionDuration = Mathf.Lerp(initialQuestionDuration, minimumQuestionDuration, speedFactor);
        questionTimeRemaining = currentQuestionDuration;

        // Pick pair (sequential cycle)
        currentPairIndex = matchesCompleted % idiomPairs.Length;
        var pair = idiomPairs[currentPairIndex];

        if (idiomTextTMP != null) {
            idiomTextTMP.text = $"\"{pair.idiomText}\"";
            idiomTextTMP.transform.DOKill();
            idiomTextTMP.transform.localScale = Vector3.zero;
            idiomTextTMP.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        }

        // Play idiom voiceover
        if (pair.idiomAudioClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(pair.idiomAudioClip);
        }

        // Setup 3 Options
        correctOptionIndex = Random.Range(0, 3);
        int distractorIdx = 0;

        for (int i = 0; i < 3; i++) {
            if (i == correctOptionIndex) {
                currentOptions[i] = pair.correctMeaning;
            } else if (distractorIdx < pair.distractorMeanings.Length) {
                currentOptions[i] = pair.distractorMeanings[distractorIdx];
                distractorIdx++;
            } else {
                currentOptions[i] = "A simple activity to do";
            }

            if (meaningButtons != null && i < meaningButtons.Length && meaningButtons[i] != null) {
                meaningButtons[i].gameObject.SetActive(true);
                meaningButtons[i].interactable = true;

                if (meaningButtonTexts != null && i < meaningButtonTexts.Length && meaningButtonTexts[i] != null) {
                    meaningButtonTexts[i].text = currentOptions[i];
                }

                if (meaningButtonImages != null && i < meaningButtonImages.Length && meaningButtonImages[i] != null) {
                    meaningButtonImages[i].color = defaultChipColor;
                }

                meaningButtons[i].transform.DOKill();
                meaningButtons[i].transform.localScale = Vector3.one;
            }
        }

        if (feedbackBanner != null) feedbackBanner.SetActive(false);
    }

    private void OnOptionClicked(int optionIndex) {
        if (!isGameRunning || isAnswering || isIntroPlaying) return;

        isAnswering = true;
        SetOptionsInteractable(false);
        bool isCorrect = (optionIndex == correctOptionIndex);

        if (isCorrect) {
            score += 100;
            matchesCompleted++;
            UpdateHUD();

            if (sfxCorrect != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxCorrect);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (meaningButtonImages != null && optionIndex < meaningButtonImages.Length && meaningButtonImages[optionIndex] != null) {
                meaningButtonImages[optionIndex].color = correctColor;
            }

            if (meaningButtons != null && optionIndex < meaningButtons.Length && meaningButtons[optionIndex] != null) {
                meaningButtons[optionIndex].transform.DOKill();
                meaningButtons[optionIndex].transform.DOScale(Vector3.one * 1.08f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }

            if (idiomCardObject != null) {
                idiomCardObject.transform.DOKill();
                idiomCardObject.transform.DOScale(Vector3.one * 1.05f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }

            ShowFeedback("+100 PTS!", true);
            StartCoroutine(AdvanceAfterDelay(0.6f));
        } else {
            livesRemaining--;
            UpdateHUD();

            if (sfxWrong != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxWrong);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (meaningButtonImages != null && optionIndex < meaningButtonImages.Length && meaningButtonImages[optionIndex] != null) {
                meaningButtonImages[optionIndex].color = wrongColor;
            }

            if (meaningButtons != null && optionIndex < meaningButtons.Length && meaningButtons[optionIndex] != null) {
                meaningButtons[optionIndex].transform.DOShakePosition(0.25f, new Vector3(8f, 0f, 0f), 10, 90, false, true);
            }

            // Reveal correct
            if (meaningButtonImages != null && correctOptionIndex < meaningButtonImages.Length && meaningButtonImages[correctOptionIndex] != null) {
                meaningButtonImages[correctOptionIndex].color = correctColor;
            }

            ShowFeedback("WRONG MEANING! -1 LIFE", false);

            if (livesRemaining <= 0) {
                StartCoroutine(AdvanceToEndAfterDelay(1.2f));
            } else {
                StartCoroutine(AdvanceAfterDelay(1.2f));
            }
        }
    }

    private void OnTimeOut() {
        if (!isGameRunning || isAnswering || isIntroPlaying) return;

        isAnswering = true;
        SetOptionsInteractable(false);
        livesRemaining--;
        UpdateHUD();

        if (sfxWrong != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(sfxWrong);
        } else if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }

        // Reveal correct answer
        if (meaningButtonImages != null && correctOptionIndex < meaningButtonImages.Length && meaningButtonImages[correctOptionIndex] != null) {
            meaningButtonImages[correctOptionIndex].color = correctColor;
        }

        ShowFeedback("TIME OUT! -1 LIFE", false);

        if (livesRemaining <= 0) {
            StartCoroutine(AdvanceToEndAfterDelay(1.2f));
        } else {
            StartCoroutine(AdvanceAfterDelay(1.2f));
        }
    }

    private void ShowFeedback(string msg, bool isSuccess) {
        if (feedbackBanner != null) {
            feedbackBanner.SetActive(true);
            if (feedbackTextTMP != null) {
                feedbackTextTMP.text = msg;
                feedbackTextTMP.color = isSuccess ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.35f, 0.35f);
            }
            feedbackBanner.transform.DOKill();
            feedbackBanner.transform.localScale = Vector3.zero;
            feedbackBanner.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack);
        }
    }

    private IEnumerator AdvanceAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        LoadNextRound();
    }

    private IEnumerator AdvanceToEndAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        EndGame(matchesCompleted >= targetMatchesToWin);
    }

    private void EndGame(bool isPassed) {
        isGameRunning = false;
        SetOptionsInteractable(false);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = isPassed ? "MEANING RUSH COMPLETED!" : "RUSH CHALLENGE OVER";
            resultTitleTMP.color = isPassed ? new Color(0.15f, 0.85f, 0.4f) : new Color(0.95f, 0.35f, 0.35f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Matches: {matchesCompleted} / 16 (Target: 16) | Score: {score} pts";
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = isPassed
                ? "Lightning Fast! You matched the idioms to their meanings in record time!"
                : "Good attempt! Match at least 16 idioms to pass the rush challenge!";
        }

        if (isPassed) {
            if (sfxCelebration != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxCelebration);
            }
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Speaking);
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Game);
            }
        }
    }

    private void UpdateHUD() {
        if (livesTMP != null) livesTMP.text = $"Lives: {livesRemaining}";
        if (scoreTMP != null) scoreTMP.text = $"Score: {score}";
        if (progressTMP != null) progressTMP.text = $"Matches: {matchesCompleted}/16";
        if (timerTMP != null && !isGameRunning) timerTMP.text = "Time: 60s";
    }

    private void OnReturnHubClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnBackButtonClicked();
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

    protected virtual void OnBackButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnBackButtonClicked();
        }
    }
}
