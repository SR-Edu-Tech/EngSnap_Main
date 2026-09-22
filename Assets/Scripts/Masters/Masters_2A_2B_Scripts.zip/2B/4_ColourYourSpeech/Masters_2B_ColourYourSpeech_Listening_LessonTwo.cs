using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// L02 Real or Literal?
/// Two big canvases side by side: the LITERAL canvas (funny picture of what words say)
/// and the REAL canvas (what the idiom truly means) for Book 2B Unit 4 (Colour Your Speech).
/// Student listens to the sentence and taps the canvas that shows what it truly means.
/// Success condition: Student picks the real meaning in at least 6 of 8 rounds.
/// </summary>
public class Masters_2B_ColourYourSpeech_Listening_LessonTwo : Masters_Lesson {

[System.Serializable]
public class ColourYourSpeech_ListeningL02Round {
    public int roundId;
    public string spokenSentence;         // e.g. "It was raining cats and dogs."
    public string idiomName;              // e.g. "Raining cats and dogs"
    public string literalDescription;     // Clean text (NO emojis, e.g. "Animals falling from a rain cloud")
    public string realDescription;        // Clean text (NO emojis, e.g. "Extremely heavy rainfall")
    public string realMeaningExplanation; // Book definition
    public bool isLeftCanvasReal;         // If true: Left = REAL, Right = LITERAL; If false: Left = LITERAL, Right = REAL
    public AudioClip spokenAudio;
    public AudioClip meaningAudio;
}

    [Header("L02 8 Real vs Literal Rounds")]
    [SerializeField]
    private ColourYourSpeech_ListeningL02Round[] rounds;

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

    [Header("Spoken Sentence Prompt Card")]
    [SerializeField]
    private GameObject promptCardObject;
    [SerializeField]
    private TextMeshProUGUI spokenSentenceTMP;
    [SerializeField]
    private Button replayAudioBtn;

    [Header("Two Side-by-Side Canvases")]
    [SerializeField]
    private Button leftCanvasBtn;
    [SerializeField]
    private Image leftCanvasBg;
    [SerializeField]
    private TextMeshProUGUI leftCanvasBadgeTMP;
    [SerializeField]
    private TextMeshProUGUI leftCanvasContentTMP;

    [SerializeField]
    private Button rightCanvasBtn;
    [SerializeField]
    private Image rightCanvasBg;
    [SerializeField]
    private TextMeshProUGUI rightCanvasBadgeTMP;
    [SerializeField]
    private TextMeshProUGUI rightCanvasContentTMP;

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

    private readonly Color defaultCanvasColor = new Color(0.12f, 0.22f, 0.42f, 0.95f);
    private readonly Color correctCanvasColor = new Color(0.13f, 0.65f, 0.32f, 1f);
    private readonly Color literalCanvasColor = new Color(0.75f, 0.35f, 0.15f, 1f);
    private readonly Color wrongCanvasColor = new Color(0.82f, 0.2f, 0.2f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Listening;
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
        if (leftCanvasBtn != null) {
            leftCanvasBtn.onClick.RemoveAllListeners();
            leftCanvasBtn.onClick.AddListener(() => OnCanvasSelected(true));
        }

        if (rightCanvasBtn != null) {
            rightCanvasBtn.onClick.RemoveAllListeners();
            rightCanvasBtn.onClick.AddListener(() => OnCanvasSelected(false));
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
        SetCanvasesInteractable(false);
        if (replayAudioBtn != null) replayAudioBtn.interactable = false;

        UpdateScoreUI();
        if (progressTMP != null) progressTMP.text = "Round 1/8";

        if (spokenSentenceTMP != null && rounds != null && rounds.Length > 0) {
            spokenSentenceTMP.text = $"\"{rounds[0].spokenSentence}\"";
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

        if (headerTMP != null) headerTMP.text = "LISTENING BRANCH (Paint Studio)";
        if (titleTMP != null) titleTMP.text = "L02 Real or Literal?";
        if (subtitleTMP != null) subtitleTMP.text = "Listen to the idiom sentence and tap the canvas that shows what it truly means!";
        if (progressTMP != null) progressTMP.text = "Round 1/8";
        if (scoreTMP != null) scoreTMP.text = "Score: 0/8";

        if (spokenSentenceTMP != null && rounds != null && rounds.Length > 0) {
            spokenSentenceTMP.text = $"\"{rounds[0].spokenSentence}\"";
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
            headerTMP.text = "LISTENING BRANCH (Paint Studio)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "L02 Real or Literal?";
        }

        if (subtitleTMP == null) {
            Transform sTrans = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Instruction") ?? transform.Find("Subtitle");
            if (sTrans != null) subtitleTMP = sTrans.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP != null) {
            subtitleTMP.text = "Listen to the idiom sentence and tap the canvas that shows what it truly means!";
        }
    }

    private void AutoBindReferences() {
        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (headerTMP == null && (n.Contains("header") || n.Contains("branch") || n.Contains("heading"))) headerTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (subtitleTMP == null && (n.Contains("subtitle") || n.Contains("instruction"))) subtitleTMP = tmp;
            else if (progressTMP == null && (n.Contains("progress") || n.Contains("count") || n.Contains("roundcount"))) progressTMP = tmp;
            else if (scoreTMP == null && n.Contains("score")) scoreTMP = tmp;
            else if (spokenSentenceTMP == null && (n.Contains("spoken") || n.Contains("sentence") || n.Contains("prompt"))) spokenSentenceTMP = tmp;
            else if (feedbackTextTMP == null && n.Contains("feedback")) feedbackTextTMP = tmp;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (leftCanvasBtn == null && (n.Contains("left") || n.Contains("canvasa") || n.Contains("canvas1"))) {
                leftCanvasBtn = btn;
                leftCanvasBg = btn.GetComponent<Image>();
            } else if (rightCanvasBtn == null && (n.Contains("right") || n.Contains("canvasb") || n.Contains("canvas2"))) {
                rightCanvasBtn = btn;
                rightCanvasBg = btn.GetComponent<Image>();
            } else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("audio") || n.Contains("speaker"))) {
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

        if (leftCanvasBtn != null) {
            TextMeshProUGUI[] lTmps = leftCanvasBtn.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (lTmps.Length > 0) leftCanvasBadgeTMP = lTmps[0];
            if (lTmps.Length > 1) leftCanvasContentTMP = lTmps[1];
        }

        if (rightCanvasBtn != null) {
            TextMeshProUGUI[] rTmps = rightCanvasBtn.GetComponentsInChildren<TextMeshProUGUI>(true);
            if (rTmps.Length > 0) rightCanvasBadgeTMP = rTmps[0];
            if (rTmps.Length > 1) rightCanvasContentTMP = rTmps[1];
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("ResultPopup");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    public void PopulateFailsafeRounds() {
        rounds = new ColourYourSpeech_ListeningL02Round[] {
            // Round 1: Raining cats and dogs
            new ColourYourSpeech_ListeningL02Round {
                roundId = 1,
                spokenSentence = "It was raining cats and dogs.",
                idiomName = "Raining cats and dogs",
                literalDescription = "Animals falling from a rain cloud",
                realDescription = "Extremely heavy rainfall",
                realMeaningExplanation = "'Raining cats and dogs' means raining very hard!",
                isLeftCanvasReal = false
            },
            // Round 2: The cat is out of the bag
            new ColourYourSpeech_ListeningL02Round {
                roundId = 2,
                spokenSentence = "The cat is out of the bag!",
                idiomName = "The cat is out of the bag",
                literalDescription = "A cat climbing out of a school bag",
                realDescription = "A secret being revealed",
                realMeaningExplanation = "'The cat is out of the bag' means a secret was let out!",
                isLeftCanvasReal = true
            },
            // Round 3: Piece of cake
            new ColourYourSpeech_ListeningL02Round {
                roundId = 3,
                spokenSentence = "It's a piece of cake.",
                idiomName = "Piece of cake",
                literalDescription = "A slice of sweet cake on a plate",
                realDescription = "An easy job done quickly",
                realMeaningExplanation = "'Piece of cake' means an activity that is simple and easy!",
                isLeftCanvasReal = false
            },
            // Round 4: Buried the hatchet
            new ColourYourSpeech_ListeningL02Round {
                roundId = 4,
                spokenSentence = "We buried the hatchet.",
                idiomName = "Bury the hatchet",
                literalDescription = "Digging a hole in the ground for an axe",
                realDescription = "Two friends making up and ending conflict",
                realMeaningExplanation = "'Bury the hatchet' means making peace and becoming friends again!",
                isLeftCanvasReal = true
            },
            // Round 5: A close shave
            new ColourYourSpeech_ListeningL02Round {
                roundId = 5,
                spokenSentence = "I had a close shave.",
                idiomName = "A close shave",
                literalDescription = "Shaving a beard in a bathroom mirror",
                realDescription = "Narrowly escaping danger or an accident",
                realMeaningExplanation = "'A close shave' means a very narrow escape!",
                isLeftCanvasReal = false
            },
            // Round 6: You scratch my back
            new ColourYourSpeech_ListeningL02Round {
                roundId = 6,
                spokenSentence = "You scratch my back I will scratch yours.",
                idiomName = "You scratch my back I will scratch yours",
                literalDescription = "Two people scratching each other's backs",
                realDescription = "Helping each other out with work",
                realMeaningExplanation = "'You scratch my back' means helping each other mutually!",
                isLeftCanvasReal = true
            },
            // Round 7: Bite off more than you can chew
            new ColourYourSpeech_ListeningL02Round {
                roundId = 7,
                spokenSentence = "She bit off more than she could chew.",
                idiomName = "Bite off more than you can chew",
                literalDescription = "Taking an impossibly gigantic bite of a burger",
                realDescription = "Taking on more tasks than one can handle",
                realMeaningExplanation = "'Bite off more than you can chew' means taking on too much work!",
                isLeftCanvasReal = false
            },
            // Round 8: Have a blast
            new ColourYourSpeech_ListeningL02Round {
                roundId = 8,
                spokenSentence = "We had a blast at the amusement park.",
                idiomName = "Have a blast",
                literalDescription = "A fiery explosion of rockets in the sky",
                realDescription = "Having immense fun and a wonderful time",
                realMeaningExplanation = "'Have a blast' means enjoying yourself immensely!",
                isLeftCanvasReal = true
            }
        };
    }

    public void SetRounds(ColourYourSpeech_ListeningL02Round[] newRounds) {
        rounds = newRounds;
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

    private void SetCanvasesInteractable(bool interactable) {
        if (leftCanvasBtn != null) leftCanvasBtn.interactable = interactable;
        if (rightCanvasBtn != null) rightCanvasBtn.interactable = interactable;
    }

    private void LoadRound(int roundIdx) {
        if (rounds == null || rounds.Length == 0) return;

        if (roundIdx >= rounds.Length || roundIdx >= 8) {
            EndGame();
            return;
        }

        var round = rounds[roundIdx];
        isHandlingAnswer = false;

        if (progressTMP != null) progressTMP.text = $"Round {roundIdx + 1}/8";

        if (spokenSentenceTMP != null) {
            spokenSentenceTMP.text = $"\"{round.spokenSentence}\"";
            spokenSentenceTMP.transform.DOKill();
            spokenSentenceTMP.transform.localScale = Vector3.zero;
            spokenSentenceTMP.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }

        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        // Setup Canvases cleanly with NO emoji prefixes
        if (round.isLeftCanvasReal) {
            if (leftCanvasBadgeTMP != null) leftCanvasBadgeTMP.text = "CANVAS A (Real Meaning)";
            if (leftCanvasContentTMP != null) leftCanvasContentTMP.text = round.realDescription;

            if (rightCanvasBadgeTMP != null) rightCanvasBadgeTMP.text = "CANVAS B (Literal Picture)";
            if (rightCanvasContentTMP != null) rightCanvasContentTMP.text = round.literalDescription;
        } else {
            if (leftCanvasBadgeTMP != null) leftCanvasBadgeTMP.text = "CANVAS A (Literal Picture)";
            if (leftCanvasContentTMP != null) leftCanvasContentTMP.text = round.literalDescription;

            if (rightCanvasBadgeTMP != null) rightCanvasBadgeTMP.text = "CANVAS B (Real Meaning)";
            if (rightCanvasContentTMP != null) rightCanvasContentTMP.text = round.realDescription;
        }

        if (leftCanvasBg != null) leftCanvasBg.color = defaultCanvasColor;
        if (rightCanvasBg != null) rightCanvasBg.color = defaultCanvasColor;

        if (leftCanvasBtn != null) {
            leftCanvasBtn.interactable = true;
            leftCanvasBtn.transform.DOKill();
            leftCanvasBtn.transform.localScale = Vector3.one;
        }
        if (rightCanvasBtn != null) {
            rightCanvasBtn.interactable = true;
            rightCanvasBtn.transform.DOKill();
            rightCanvasBtn.transform.localScale = Vector3.one;
        }

        if (replayAudioBtn != null) replayAudioBtn.interactable = true;

        PlaySpokenAudio(round);
    }

    private void PlaySpokenAudio(ColourYourSpeech_ListeningL02Round round) {
        if (round != null && round.spokenAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(round.spokenAudio);
        }
    }

    private void OnCanvasSelected(bool isLeft) {
        if (isHandlingAnswer || isIntroPlaying || currentRoundIndex >= rounds.Length) return;

        isHandlingAnswer = true;
        SetCanvasesInteractable(false);
        var round = rounds[currentRoundIndex];
        bool isRealSelected = (isLeft == round.isLeftCanvasReal);

        if (isRealSelected) {
            // Correct - Selected Real Meaning
            score++;
            UpdateScoreUI();

            if (sfxCorrect != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxCorrect);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            Image pickedBg = isLeft ? leftCanvasBg : rightCanvasBg;
            Button pickedBtn = isLeft ? leftCanvasBtn : rightCanvasBtn;

            if (pickedBg != null) pickedBg.color = correctCanvasColor;
            if (pickedBtn != null) {
                pickedBtn.transform.DOKill();
                pickedBtn.transform.DOScale(Vector3.one * 1.05f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }

            ShowFeedback($"Exactly! {round.realMeaningExplanation}", true);

            float delay = 2.0f;
            if (round.meaningAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(round.meaningAudio);
                delay = Mathf.Max(delay, round.meaningAudio.length + 0.5f);
            }

            StartCoroutine(AdvanceAfterDelay(delay));
        } else {
            // Picked Literal joke picture
            if (sfxWrong != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxWrong);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            Image pickedBg = isLeft ? leftCanvasBg : rightCanvasBg;
            Image realBg = isLeft ? rightCanvasBg : leftCanvasBg;
            Button pickedBtn = isLeft ? leftCanvasBtn : rightCanvasBtn;

            if (pickedBg != null) pickedBg.color = literalCanvasColor;
            if (realBg != null) realBg.color = correctCanvasColor;

            if (pickedBtn != null) {
                pickedBtn.transform.DOShakePosition(0.25f, new Vector3(10f, 0f, 0f), 10, 90, false, true);
            }

            ShowFeedback($"That's the literal picture — the idiom means what's on the other canvas! {round.realMeaningExplanation}", false);
            StartCoroutine(AdvanceAfterDelay(2.4f));
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
        PlaySpokenAudio(rounds[currentRoundIndex]);
        if (promptCardObject != null) {
            promptCardObject.transform.DOKill();
            promptCardObject.transform.DOScale(Vector3.one * 1.04f, 0.15f).SetLoops(2, LoopType.Yoyo);
        }
    }

    private IEnumerator AdvanceAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        currentRoundIndex++;
        LoadRound(currentRoundIndex);
    }

    private void EndGame() {
        isHandlingAnswer = true;
        SetCanvasesInteractable(false);
        if (replayAudioBtn != null) replayAudioBtn.interactable = false;

        bool isPassed = (score >= 6);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = isPassed ? "REAL OR LITERAL CHALLENGE COMPLETED!" : "CHALLENGE NOT PASSED";
            resultTitleTMP.color = isPassed ? new Color(0.15f, 0.85f, 0.4f) : new Color(0.95f, 0.35f, 0.35f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Identified: {score} / 8 Rounds (Target: 6)";
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = isPassed
                ? "Great job! You mastered distinguishing true idiom meanings from funny literal pictures!"
                : "Try again! Pick the real meaning in at least 6 of 8 rounds to pass!";
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
            scoreTMP.text = $"Score: {score}/8";
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
