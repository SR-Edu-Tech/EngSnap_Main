using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


/// <summary>
/// R01 Which Idiom Fits? (in-context)
/// Library Noticeboard reading lesson controller for Book 2B Unit 4 (Colour Your Speech).
/// Displays 10 situation scenes and 4 idiom answer chips.
/// Success condition: Student picks the correct idiom for at least 8 of 10 situations.
/// </summary>
public class Masters_2B_ColourYourSpeech_Reading_LessonOne : Masters_Lesson {

[System.Serializable]
public class ColourYourSpeech_ReadingR01Round {
    public int roundId;
    public string situationText;       // Situation card text
    public string correctIdiom;        // The verbatim idiom
    public string idiomMeaning;        // Book definition of the idiom
    public string[] optionIdioms;      // 4 idiom option chips
    public int correctOptionIndex;     // Index 0..3
    public AudioClip situationAudio;
    public AudioClip meaningAudio;
}

    [Header("R01 10 Situation Rounds")]
    [SerializeField]
    private ColourYourSpeech_ReadingR01Round[] rounds;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI subtitleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI scoreTMP;

    [Header("Library Noticeboard Situation Card")]
    [SerializeField]
    private GameObject noticeboardCardObject;
    [SerializeField]
    private Image noticeboardCardBg;
    [SerializeField]
    private TextMeshProUGUI noticeboardHeaderTMP;
    [SerializeField]
    private TextMeshProUGUI situationTextTMP;
    [SerializeField]
    private Button replayAudioBtn;

    [Header("4 Answer Option Chips")]
    [SerializeField]
    private Button[] optionButtons;       // 4 Option Buttons
    [SerializeField]
    private TextMeshProUGUI[] optionTexts;// 4 Option Texts
    [SerializeField]
    private Image[] optionImages;         // 4 Option Button Images

    [Header("Feedback / Explanation Banner")]
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

    [Header("Navigation")]
    [SerializeField]
    private Button backButton;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;

    private int currentRoundIndex = 0;
    private int score = 0;
    private bool isHandlingAnswer = false;
    private bool isIntroPlaying = false;

    private Color defaultChipColor = new Color(0.14f, 0.45f, 0.75f, 0.95f);
    private Color correctChipColor = new Color(0.15f, 0.75f, 0.35f, 1f);
    private Color wrongChipColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Reading;
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
        topic = Masters_Topic.Reading;
        base.Start();

        PurgeLegacyChildren();
        EnsureHeaderAndTitle();
        AutoBindReferences();
        WireButtonEvents();

        if (rounds == null || rounds.Length == 0) {
            PopulateFailsafeRounds();
        }

        StartCoroutine(PlayIntroAndStartGame());
    }

    private void WireButtonEvents() {
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                int index = i;
                if (optionButtons[i] != null) {
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
                }
            }
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(StartNewGame);
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

    public void SetRounds(ColourYourSpeech_ReadingR01Round[] newRounds) {
        rounds = newRounds;
    }

    private IEnumerator PlayIntroAndStartGame() {
        isIntroPlaying = true;
        isHandlingAnswer = true;
        SetOptionButtonsInteractable(false);
        if (replayAudioBtn != null) replayAudioBtn.interactable = false;

        UpdateScoreUI();
        if (progressTMP != null) progressTMP.text = "Scene 1/10";

        if (situationTextTMP != null && rounds != null && rounds.Length > 0) {
            situationTextTMP.text = $"\"{rounds[0].situationText}\"";
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

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        AutoBindReferences();

        if (rounds == null || rounds.Length == 0) {
            PopulateFailsafeRounds();
        }

        if (headerTMP != null) headerTMP.text = "READING BRANCH (Library Noticeboard)";
        if (titleTMP != null) titleTMP.text = "R01 Which Idiom Fits? (in-context)";
        if (subtitleTMP != null) subtitleTMP.text = "Read the situation on the noticeboard and tap the idiom that fits it!";
        if (progressTMP != null) progressTMP.text = "Scene 1/10";
        if (scoreTMP != null) scoreTMP.text = "Score: 0/10";

        if (situationTextTMP != null && rounds != null && rounds.Length > 0) {
            situationTextTMP.text = $"\"{rounds[0].situationText}\"";
        }
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer",
            "SpawnArea", "GamePlayGameObject", "FlashCardsGrid", "MiniQuizGameObject", "StartMiniQuizButton"
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
            headerTMP.text = "READING BRANCH (Library Noticeboard)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "R01 Which Idiom Fits? (in-context)";
        }

        if (subtitleTMP == null) {
            Transform sTrans = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Instruction") ?? transform.Find("Subtitle");
            if (sTrans != null) subtitleTMP = sTrans.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP != null) {
            subtitleTMP.text = "Read the situation on the noticeboard and tap the idiom that fits it!";
        }
    }

    private void AutoBindReferences() {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading"))) headerTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (subtitleTMP == null && (n.Contains("subtitle") || n.Contains("instruction"))) subtitleTMP = tmp;
            else if (progressTMP == null && (n.Contains("progress") || n.Contains("count") || n.Contains("scenecount"))) progressTMP = tmp;
            else if (scoreTMP == null && n.Contains("score")) scoreTMP = tmp;
            else if (situationTextTMP == null && (n.Contains("situation") || n.Contains("prompt") || n.Contains("cardtext"))) situationTextTMP = tmp;
            else if (noticeboardHeaderTMP == null && (n.Contains("noticeheader") || n.Contains("boardtitle"))) noticeboardHeaderTMP = tmp;
            else if (feedbackTextTMP == null && n.Contains("feedback")) feedbackTextTMP = tmp;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> chips = new List<Button>();
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("option") || n.Contains("chip")) {
                chips.Add(btn);
            } else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("speaker") || n.Contains("audio"))) {
                replayAudioBtn = btn;
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

        if (chips.Count >= 4) {
            optionButtons = chips.ToArray();
            optionTexts = new TextMeshProUGUI[optionButtons.Length];
            optionImages = new Image[optionButtons.Length];
            for (int i = 0; i < optionButtons.Length; i++) {
                optionTexts[i] = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                optionImages[i] = optionButtons[i].GetComponent<Image>();
            }
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("ResultPopup");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    public void PopulateFailsafeRounds() {
        rounds = new ColourYourSpeech_ReadingR01Round[] {
            // Round 1: Bite off more than you can chew
            new ColourYourSpeech_ReadingR01Round {
                roundId = 1,
                situationText = "You promised four people help and still have your worksheets to finish.",
                correctIdiom = "Bite off more than you can chew",
                idiomMeaning = "To try to do something that is too difficult or too much for you.",
                optionIdioms = new string[] { "Bite off more than you can chew", "Piece of cake", "Have a blast", "Miss the boat" },
                correctOptionIndex = 0
            },
            // Round 2: Piece of cake
            new ColourYourSpeech_ReadingR01Round {
                roundId = 2,
                situationText = "Your friend can make a paper boat in a minute.",
                correctIdiom = "Piece of cake",
                idiomMeaning = "A job, task or activity that is easy or simple to do.",
                optionIdioms = new string[] { "A close shave", "Piece of cake", "Bite off more than you can chew", "Bury the hatchet" },
                correctOptionIndex = 1
            },
            // Round 3: Have a blast
            new ColourYourSpeech_ReadingR01Round {
                roundId = 3,
                situationText = "The field trip was amazing - there was never a dull moment.",
                correctIdiom = "Have a blast",
                idiomMeaning = "To enjoy a lot and have great fun.",
                optionIdioms = new string[] { "Miss the boat", "Raining cats and dogs", "Have a blast", "The cat is out of the bag" },
                correctOptionIndex = 2
            },
            // Round 4: Miss the boat
            new ColourYourSpeech_ReadingR01Round {
                roundId = 4,
                situationText = "The dance auditions were held yesterday and you only heard today.",
                correctIdiom = "Miss the boat",
                idiomMeaning = "To miss a chance or opportunity.",
                optionIdioms = new string[] { "Piece of cake", "Have a blast", "You scratch my back", "Miss the boat" },
                correctOptionIndex = 3
            },
            // Round 5: The cat is out of the bag
            new ColourYourSpeech_ReadingR01Round {
                roundId = 5,
                situationText = "Ravi accidentally told Rita about the secret party.",
                correctIdiom = "The cat is out of the bag",
                idiomMeaning = "To reveal a secret carelessly or by mistake.",
                optionIdioms = new string[] { "The cat is out of the bag", "As you make your bed", "Bury the hatchet", "A close shave" },
                correctOptionIndex = 0
            },
            // Round 6: Raining cats and dogs
            new ColourYourSpeech_ReadingR01Round {
                roundId = 6,
                situationText = "Rohan reached school completely drenched.",
                correctIdiom = "Raining cats and dogs",
                idiomMeaning = "Raining extremely heavily.",
                optionIdioms = new string[] { "Miss the boat", "Raining cats and dogs", "Piece of cake", "Bite off more than you can chew" },
                correctOptionIndex = 1
            },
            // Round 7: Bury the hatchet
            new ColourYourSpeech_ReadingR01Round {
                roundId = 7,
                situationText = "After the fight, Simran and Karan are friends again.",
                correctIdiom = "Bury the hatchet",
                idiomMeaning = "To end a quarrel or conflict and become friendly again.",
                optionIdioms = new string[] { "A close shave", "The cat is out of the bag", "Bury the hatchet", "Have a blast" },
                correctOptionIndex = 2
            },
            // Round 8: A close shave
            new ColourYourSpeech_ReadingR01Round {
                roundId = 8,
                situationText = "Mohan's cycle almost fell in the drains — but he is fine.",
                correctIdiom = "A close shave",
                idiomMeaning = "A narrow escape from danger or trouble.",
                optionIdioms = new string[] { "Raining cats and dogs", "Bury the hatchet", "As you make your bed", "A close shave" },
                correctOptionIndex = 3
            },
            // Round 9: You scratch my back I will scratch yours
            new ColourYourSpeech_ReadingR01Round {
                roundId = 9,
                situationText = "Harvey helps with maths, Mike helps with English.",
                correctIdiom = "You scratch my back I will scratch yours",
                idiomMeaning = "Help each other mutually to get benefits.",
                optionIdioms = new string[] { "You scratch my back I will scratch yours", "Piece of cake", "Bite off more than you can chew", "Miss the boat" },
                correctOptionIndex = 0
            },
            // Round 10: As you make your bed so you must lie in it
            new ColourYourSpeech_ReadingR01Round {
                roundId = 10,
                situationText = "Renu broke her sister's new pencil and now Riya won't talk to her.",
                correctIdiom = "As you make your bed, so you must lie in it too.",
                idiomMeaning = "You must accept the consequences of your own actions.",
                optionIdioms = new string[] { "Have a blast", "As you make your bed, so you must lie in it too.", "The cat is out of the bag", "A close shave" },
                correctOptionIndex = 1
            }
        };
    }

    public void StartNewGame() {
        currentRoundIndex = 0;
        score = 0;
        isHandlingAnswer = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        UpdateScoreUI();
        LoadRound(currentRoundIndex);
    }

    private void SetOptionButtonsInteractable(bool interactable) {
        if (optionButtons != null) {
            foreach (var btn in optionButtons) {
                if (btn != null) btn.interactable = interactable;
            }
        }
    }

    private void LoadRound(int roundIdx) {
        if (rounds == null || rounds.Length == 0) return;

        if (roundIdx >= rounds.Length || roundIdx >= 10) {
            EndGame();
            return;
        }

        var round = rounds[roundIdx];
        isHandlingAnswer = false;

        if (progressTMP != null) progressTMP.text = $"Scene {roundIdx + 1}/10";

        if (noticeboardHeaderTMP != null) {
            noticeboardHeaderTMP.text = $"NOTICEBOARD SCENE #{roundIdx + 1}";
        }

        if (situationTextTMP != null) {
            situationTextTMP.text = $"\"{round.situationText}\"";
            situationTextTMP.transform.DOKill();
            situationTextTMP.transform.localScale = Vector3.zero;
            situationTextTMP.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }

        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        PlaySituationAudio(round);

        // Setup Option Chips
        for (int i = 0; i < optionButtons.Length; i++) {
            if (i < round.optionIdioms.Length && optionButtons[i] != null) {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].interactable = true;
                optionButtons[i].transform.DOKill();
                optionButtons[i].transform.localScale = Vector3.one;

                if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) {
                    optionTexts[i].text = round.optionIdioms[i];
                }

                if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                    optionImages[i].color = defaultChipColor;
                }
            } else if (optionButtons[i] != null) {
                optionButtons[i].gameObject.SetActive(false);
            }
        }

        if (replayAudioBtn != null) replayAudioBtn.interactable = true;
    }

    private void PlaySituationAudio(ColourYourSpeech_ReadingR01Round round) {
        if (round != null && round.situationAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(round.situationAudio);
        }
    }

    private void OnOptionSelected(int optionIndex) {
        if (isHandlingAnswer || isIntroPlaying || currentRoundIndex >= rounds.Length) return;

        isHandlingAnswer = true;
        SetOptionButtonsInteractable(false);
        if (replayAudioBtn != null) replayAudioBtn.interactable = false;

        var round = rounds[currentRoundIndex];
        bool isCorrect = (optionIndex == round.correctOptionIndex);

        if (isCorrect) {
            score++;
            UpdateScoreUI();

            if (sfxCorrect != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxCorrect);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = correctChipColor;
            }

            if (optionButtons != null && optionIndex < optionButtons.Length && optionButtons[optionIndex] != null) {
                optionButtons[optionIndex].transform.DOKill();
                optionButtons[optionIndex].transform.DOScale(Vector3.one * 1.08f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }

            ShowFeedback($"Correct! <b>{round.correctIdiom}</b>: {round.idiomMeaning}", true);

            float delay = 2.0f;
            if (round.meaningAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(round.meaningAudio);
                delay = Mathf.Max(delay, round.meaningAudio.length + 0.5f);
            }

            StartCoroutine(AdvanceAfterDelay(delay));
        } else {
            if (sfxWrong != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxWrong);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = wrongChipColor;
            }

            // Highlight correct answer in green
            if (optionImages != null && round.correctOptionIndex < optionImages.Length && optionImages[round.correctOptionIndex] != null) {
                optionImages[round.correctOptionIndex].color = correctChipColor;
            }

            if (optionButtons != null && optionIndex < optionButtons.Length && optionButtons[optionIndex] != null) {
                optionButtons[optionIndex].transform.DOShakePosition(0.25f, new Vector3(10f, 0f, 0f), 10, 90, false, true);
            }

            ShowFeedback($"Hint: Think about what fits: <b>{round.correctIdiom}</b>!", false);
            StartCoroutine(AdvanceAfterDelay(2.2f));
        }
    }

    private void ShowFeedback(string msg, bool isSuccess) {
        if (feedbackBanner != null) {
            feedbackBanner.SetActive(true);
            if (feedbackTextTMP != null) {
                feedbackTextTMP.text = msg;
                feedbackTextTMP.color = isSuccess ? new Color(0.2f, 1f, 0.4f) : new Color(1f, 0.85f, 0.35f);
            }
            feedbackBanner.transform.DOKill();
            feedbackBanner.transform.localScale = Vector3.zero;
            feedbackBanner.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }
    }

    private void OnReplayAudioClicked() {
        if (isIntroPlaying || currentRoundIndex >= rounds.Length) return;
        PlaySituationAudio(rounds[currentRoundIndex]);
        if (noticeboardCardObject != null) {
            noticeboardCardObject.transform.DOKill();
            noticeboardCardObject.transform.DOScale(Vector3.one * 1.04f, 0.15f).SetLoops(2, LoopType.Yoyo);
        }
    }

    private IEnumerator AdvanceAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        currentRoundIndex++;
        LoadRound(currentRoundIndex);
    }

    private void EndGame() {
        isHandlingAnswer = true;
        SetOptionButtonsInteractable(false);
        if (replayAudioBtn != null) replayAudioBtn.interactable = false;

        bool isPassed = (score >= 8);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = isPassed ? "READING CHALLENGE COMPLETED!" : "CHALLENGE NOT PASSED";
            resultTitleTMP.color = isPassed ? new Color(0.15f, 0.85f, 0.4f) : new Color(0.95f, 0.35f, 0.35f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Matched: {score} / 10 Situations (Target: 8)";
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = isPassed
                ? "Great job! You matched the situations with their correct idioms!"
                : "Try again! Match at least 8 of 10 situations with their idioms to pass!";
        }

        if (isPassed) {
            if (sfxCelebration != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxCelebration);
            }
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Speaking);
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Reading);
            }
        }
    }

    private void UpdateScoreUI() {
        if (scoreTMP != null) {
            scoreTMP.text = $"Score: {score}/10";
        }
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
                Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Speaking);
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
