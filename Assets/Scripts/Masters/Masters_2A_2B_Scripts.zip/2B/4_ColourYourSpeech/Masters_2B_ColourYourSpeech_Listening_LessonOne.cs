using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// L01 Hear the Story — Which Idiom?
/// Radio Shelf listening lesson controller for Book 2B Unit 4 (Colour Your Speech).
/// Plays short situational stories and presents 4 idiom answer chips.
/// Success condition: Student picks the correct idiom for at least 8 of 10 stories.
/// </summary>
public class Masters_2B_ColourYourSpeech_Listening_LessonOne : Masters_Lesson {

[System.Serializable]
public class ColourYourSpeech_ListeningL01Round {
    public int roundId;
    public string storyText;           // The story played on the Radio Shelf
    public string correctIdiom;        // The verbatim idiom
    public string idiomMeaning;        // Book definition of the idiom
    public string[] optionIdioms;      // 4 idiom option chips
    public int correctOptionIndex;     // Index 0..3
    public AudioClip storyAudio;
    public AudioClip meaningAudio;
}

    [Header("L01 10 Idiom Story Rounds")]
    [SerializeField]
    private ColourYourSpeech_ListeningL01Round[] rounds;

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

    [Header("Radio Shelf & Story Display")]
    [SerializeField]
    private GameObject radioCardObject;
    [SerializeField]
    private Image radioCardBg;
    [SerializeField]
    private TextMeshProUGUI radioHeaderTMP;
    [SerializeField]
    private TextMeshProUGUI storyTextTMP;
    [SerializeField]
    private Button replayAudioBtn;

    [Header("4 Answer Option Chips")]
    [SerializeField]
    private Button[] optionButtons;       // 4 Option Buttons
    [SerializeField]
    private TextMeshProUGUI[] optionTexts;// 4 Option Texts
    [SerializeField]
    private Image[] optionImages;         // 4 Option Button Images

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

    private readonly Color defaultChipColor = new Color(0.14f, 0.45f, 0.75f, 0.95f);
    private readonly Color correctChipColor = new Color(0.15f, 0.75f, 0.35f, 1f);
    private readonly Color wrongChipColor = new Color(0.85f, 0.25f, 0.25f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Listening;
        base.Awake();

        PurgeLegacyChildren();
        AutoBindReferences();
        WireButtonEvents();

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }
    }

    protected override void Start() {
        topic = Masters_Topic.Listening;
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

    private IEnumerator PlayIntroAndStartGame() {
        isIntroPlaying = true;
        isHandlingAnswer = true;
        SetOptionsInteractable(false);
        if (replayAudioBtn != null) replayAudioBtn.interactable = false;

        UpdateScoreUI();
        if (progressTMP != null) progressTMP.text = "Story 1/10";
        if (radioHeaderTMP != null) radioHeaderTMP.text = "RADIO STORY #1";

        if (storyTextTMP != null && rounds != null && rounds.Length > 0) {
            storyTextTMP.text = $"\"{rounds[0].storyText}\"";
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

        if (headerTMP != null) headerTMP.text = "LISTENING BRANCH (Radio Shelf)";
        if (titleTMP != null) titleTMP.text = "L01 Hear the Story — Which Idiom?";
        if (subtitleTMP != null) subtitleTMP.text = "Listen to the radio story and tap the idiom that fits what happened!";
        if (progressTMP != null) progressTMP.text = "Story 1/10";
        if (scoreTMP != null) scoreTMP.text = "Score: 0/10";

        if (storyTextTMP != null && rounds != null && rounds.Length > 0) {
            storyTextTMP.text = $"\"{rounds[0].storyText}\"";
        }
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer",
            "SpawnArea", "GamePlayGameObject", "SlowToggle", "RepeatToggle"
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
            headerTMP.text = "LISTENING BRANCH (Radio Shelf)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "L01 Hear the Story — Which Idiom?";
        }

        if (subtitleTMP == null) {
            Transform sTrans = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Instruction") ?? transform.Find("Subtitle");
            if (sTrans != null) subtitleTMP = sTrans.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP != null) {
            subtitleTMP.text = "Listen to the radio story and tap the idiom that fits what happened!";
        }
    }

    private void AutoBindReferences() {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading"))) headerTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (subtitleTMP == null && (n.Contains("subtitle") || n.Contains("instruction"))) subtitleTMP = tmp;
            else if (progressTMP == null && (n.Contains("progress") || n.Contains("count") || n.Contains("storycount"))) progressTMP = tmp;
            else if (scoreTMP == null && n.Contains("score")) scoreTMP = tmp;
            else if (storyTextTMP == null && (n.Contains("story") || n.Contains("prompt") || n.Contains("radiotext"))) storyTextTMP = tmp;
            else if (radioHeaderTMP == null && (n.Contains("radioheader") || n.Contains("cardtitle"))) radioHeaderTMP = tmp;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> chips = new List<Button>();
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("option") || n.Contains("chip")) {
                chips.Add(btn);
            } else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("speaker") || n.Contains("audio") || n.Contains("radio"))) {
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
        rounds = new ColourYourSpeech_ListeningL01Round[] {
            // Round 1: Bite off more than you can chew
            new ColourYourSpeech_ListeningL01Round {
                roundId = 1,
                storyText = "She promised to help her dad, her mum and her sister, and still had a party and worksheets.",
                correctIdiom = "Bite off more than you can chew",
                idiomMeaning = "To try to do something that is too difficult or too much for you.",
                optionIdioms = new string[] { "Bite off more than you can chew", "Piece of cake", "Have a blast", "Miss the boat" },
                correctOptionIndex = 0
            },
            // Round 2: Piece of cake
            new ColourYourSpeech_ListeningL01Round {
                roundId = 2,
                storyText = "Making a paper boat? He can do it in a minute.",
                correctIdiom = "Piece of cake",
                idiomMeaning = "A job, task or activity that is easy or simple to do.",
                optionIdioms = new string[] { "A close shave", "Piece of cake", "Bite off more than you can chew", "Bury the hatchet" },
                correctOptionIndex = 1
            },
            // Round 3: Have a blast
            new ColourYourSpeech_ListeningL01Round {
                roundId = 3,
                storyText = "The whole class loved the field trip — there was never a dull moment.",
                correctIdiom = "Have a blast",
                idiomMeaning = "To enjoy a lot and have great fun.",
                optionIdioms = new string[] { "Miss the boat", "Raining cats and dogs", "Have a blast", "The cat is out of the bag" },
                correctOptionIndex = 2
            },
            // Round 4: Miss the boat
            new ColourYourSpeech_ListeningL01Round {
                roundId = 4,
                storyText = "The auditions were held yesterday, and he only found out today.",
                correctIdiom = "Miss the boat",
                idiomMeaning = "To miss a chance or opportunity.",
                optionIdioms = new string[] { "Piece of cake", "Have a blast", "You scratch my back", "Miss the boat" },
                correctOptionIndex = 3
            },
            // Round 5: The cat is out of the bag
            new ColourYourSpeech_ListeningL01Round {
                roundId = 5,
                storyText = "He let the secret party slip while talking to her.",
                correctIdiom = "The cat is out of the bag",
                idiomMeaning = "To reveal a secret carelessly or by mistake.",
                optionIdioms = new string[] { "The cat is out of the bag", "As you make your bed", "Bury the hatchet", "A close shave" },
                correctOptionIndex = 0
            },
            // Round 6: Raining cats and dogs
            new ColourYourSpeech_ListeningL01Round {
                roundId = 6,
                storyText = "He came in completely drenched.",
                correctIdiom = "Raining cats and dogs",
                idiomMeaning = "Raining extremely heavily.",
                optionIdioms = new string[] { "Miss the boat", "Raining cats and dogs", "Piece of cake", "Bite off more than you can chew" },
                correctOptionIndex = 1
            },
            // Round 7: Bury the hatchet
            new ColourYourSpeech_ListeningL01Round {
                roundId = 7,
                storyText = "They fought last week; today they are friends again.",
                correctIdiom = "Bury the hatchet",
                idiomMeaning = "To end a quarrel or conflict and become friendly again.",
                optionIdioms = new string[] { "A close shave", "The cat is out of the bag", "Bury the hatchet", "Have a blast" },
                correctOptionIndex = 2
            },
            // Round 8: A close shave
            new ColourYourSpeech_ListeningL01Round {
                roundId = 8,
                storyText = "His cycle almost fell in the drains but he is fine.",
                correctIdiom = "A close shave",
                idiomMeaning = "A narrow escape from danger or trouble.",
                optionIdioms = new string[] { "Raining cats and dogs", "Bury the hatchet", "As you make your bed", "A close shave" },
                correctOptionIndex = 3
            },
            // Round 9: You scratch my back I will scratch yours
            new ColourYourSpeech_ListeningL01Round {
                roundId = 9,
                storyText = "I'll help with your maths if you help with my English.",
                correctIdiom = "You scratch my back I will scratch yours",
                idiomMeaning = "Help each other mutually to get benefits.",
                optionIdioms = new string[] { "You scratch my back I will scratch yours", "Piece of cake", "Bite off more than you can chew", "Miss the boat" },
                correctOptionIndex = 0
            },
            // Round 10: As you make your bed so you must lie in it
            new ColourYourSpeech_ListeningL01Round {
                roundId = 10,
                storyText = "He broke his sister's new pencil and now she won't talk to him.",
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

        UpdateScoreUI();
        LoadRound(currentRoundIndex);
    }

    private void SetOptionsInteractable(bool interactable) {
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    optionButtons[i].interactable = interactable;
                }
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

        if (progressTMP != null) progressTMP.text = $"Story {roundIdx + 1}/10";

        if (radioHeaderTMP != null) {
            radioHeaderTMP.text = $"RADIO STORY #{roundIdx + 1}";
        }

        if (storyTextTMP != null) {
            storyTextTMP.text = $"\"{round.storyText}\"";
            storyTextTMP.transform.DOKill();
            storyTextTMP.transform.localScale = Vector3.zero;
            storyTextTMP.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }

        PlayStoryAudio(round);

        // Setup Option Chips
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (i < round.optionIdioms.Length && optionButtons[i] != null) {
                    optionButtons[i].gameObject.SetActive(true);
                    optionButtons[i].interactable = true;

                    if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) {
                        optionTexts[i].text = round.optionIdioms[i];
                    }

                    if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                        optionImages[i].color = defaultChipColor;
                    }

                    optionButtons[i].transform.DOKill();
                    optionButtons[i].transform.localScale = Vector3.one;
                } else if (optionButtons[i] != null) {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }

        if (replayAudioBtn != null) replayAudioBtn.interactable = true;
    }

    private void PlayStoryAudio(ColourYourSpeech_ListeningL01Round round) {
        if (round != null && round.storyAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(round.storyAudio);
        }
    }

    private void OnOptionSelected(int optionIndex) {
        if (isHandlingAnswer || isIntroPlaying || currentRoundIndex >= rounds.Length) return;

        isHandlingAnswer = true;
        SetOptionsInteractable(false);
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

            // Splash open meaning in story area
            if (storyTextTMP != null) {
                storyTextTMP.text = $"<color=#F1C40F><b>{round.correctIdiom}</b></color>\n{round.idiomMeaning}";
            }

            // Play meaning audio if available
            float delay = 1.8f;
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

            StartCoroutine(AdvanceAfterDelay(2.0f));
        }
    }

    private void OnReplayAudioClicked() {
        if (isIntroPlaying || currentRoundIndex >= rounds.Length) return;
        PlayStoryAudio(rounds[currentRoundIndex]);
        if (radioCardObject != null) {
            radioCardObject.transform.DOKill();
            radioCardObject.transform.DOScale(Vector3.one * 1.04f, 0.15f).SetLoops(2, LoopType.Yoyo);
        }
    }

    private IEnumerator AdvanceAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        currentRoundIndex++;
        LoadRound(currentRoundIndex);
    }

    private void EndGame() {
        isHandlingAnswer = true;
        SetOptionsInteractable(false);
        if (replayAudioBtn != null) replayAudioBtn.interactable = false;

        bool isPassed = (score >= 8);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = isPassed ? "LISTENING CHALLENGE COMPLETED!" : "CHALLENGE NOT PASSED";
            resultTitleTMP.color = isPassed ? new Color(0.15f, 0.85f, 0.4f) : new Color(0.95f, 0.35f, 0.35f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Identified: {score} / 10 Stories (Target: 8)";
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = isPassed
                ? "Great job! You identified the idioms from the radio stories!"
                : "Try again! Match at least 8 of 10 stories with their idioms to pass!";
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
                Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Listening);
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
