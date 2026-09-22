using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


/// <summary>
/// R03 Finish the Conversation
/// Comic strip / library reading desk format for Book 2B Unit 4 (Colour Your Speech).
/// Shows Speaker A's dialogue and an empty speech bubble for Speaker B.
/// Student selects which of 3 closing lines uses the correct idiom to finish the conversation.
/// Success condition: Student finishes at least 6 of 8 conversations correctly.
/// </summary>
public class Masters_2B_ColourYourSpeech_Reading_LessonThree : Masters_Lesson {

[System.Serializable]
public class ColourYourSpeech_ReadingR03ConversationRound {
    public int roundId;
    public string conversationTitle;      // e.g. "Zara & Stella - The Busy Weekend"
    public string speakerAName;           // e.g. "Stella"
    public string speakerASpeech;         // e.g. "Zara, did you really promise to help four people and still finish all your homework?"
    public string speakerBName;           // e.g. "Zara"
    public string correctClosingLine;     // e.g. "Yes. I think I bit off more than I could chew."
    public string idiomName;              // e.g. "Bite off more than you can chew"
    public string idiomMeaning;           // e.g. "To take on a task that is too big"
    public string[] distractorLines;      // 2 wrong closing lines
    public int correctOptionIndex;        // 0, 1, or 2 (shuffled)
    public string[] optionLines;          // 3 option choices
    public AudioClip conversationAudio;
    public AudioClip closingAudio;
}

    [Header("R03 8 Verbatim Conversation Rounds")]
    [SerializeField]
    private ColourYourSpeech_ReadingR03ConversationRound[] rounds;

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

    [Header("Comic Conversation Stage")]
    [SerializeField]
    private GameObject conversationCardObject;
    [SerializeField]
    private TextMeshProUGUI conversationTitleTMP;
    [SerializeField]
    private TextMeshProUGUI speakerATitleTMP;
    [SerializeField]
    private TextMeshProUGUI speakerASpeechTMP;
    [SerializeField]
    private TextMeshProUGUI speakerBTitleTMP;
    [SerializeField]
    private TextMeshProUGUI speakerBSpeechTMP;
    [SerializeField]
    private Button replayAudioBtn;

    [Header("3 Closing Line Option Chips")]
    [SerializeField]
    private Button[] optionButtons; // 3 Chips
    [SerializeField]
    private Image[] optionImages;
    [SerializeField]
    private TextMeshProUGUI[] optionTexts;

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

    [Header("Colors")]
    [SerializeField]
    private Color defaultChipColor = new Color(0.12f, 0.22f, 0.42f, 0.95f);
    [SerializeField]
    private Color correctChipColor = new Color(0.13f, 0.65f, 0.32f, 1f);
    [SerializeField]
    private Color wrongChipColor = new Color(0.82f, 0.2f, 0.2f, 1f);

    private int currentRoundIndex = 0;
    private int score = 0;
    private bool isHandlingAnswer = false;
    private bool isIntroPlaying = false;
    private int retriesRemaining = 1;

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
        if (optionButtons != null && optionButtons.Length > 0) {
            optionImages = new Image[optionButtons.Length];
            optionTexts = new TextMeshProUGUI[optionButtons.Length];
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    int index = i;
                    optionImages[i] = optionButtons[i].GetComponent<Image>();
                    optionTexts[i] = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
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

    public void SetRounds(ColourYourSpeech_ReadingR03ConversationRound[] newRounds) {
        rounds = newRounds;
    }

    private IEnumerator PlayIntroAndStartGame() {
        isIntroPlaying = true;
        isHandlingAnswer = true;
        SetOptionButtonsInteractable(false);
        if (replayAudioBtn != null) replayAudioBtn.interactable = false;

        UpdateScoreUI();
        if (progressTMP != null) progressTMP.text = "Conversation 1/8";

        if (rounds != null && rounds.Length > 0) {
            if (conversationTitleTMP != null) conversationTitleTMP.text = rounds[0].conversationTitle;
            if (speakerATitleTMP != null) speakerATitleTMP.text = $"{rounds[0].speakerAName}:";
            if (speakerASpeechTMP != null) speakerASpeechTMP.text = $"\"{rounds[0].speakerASpeech}\"";
            if (speakerBTitleTMP != null) speakerBTitleTMP.text = $"{rounds[0].speakerBName}:";
            if (speakerBSpeechTMP != null) speakerBSpeechTMP.text = "[ Tap the closing line below... ]";
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

        if (headerTMP != null) headerTMP.text = "READING BRANCH (Library Reading Desk)";
        if (titleTMP != null) titleTMP.text = "R03 Finish the Conversation";
        if (subtitleTMP != null) subtitleTMP.text = "Read the conversation and tap the line that finishes it, using the right idiom!";
        if (progressTMP != null) progressTMP.text = "Conversation 1/8";
        if (scoreTMP != null) scoreTMP.text = "Score: 0/8";

        if (rounds != null && rounds.Length > 0) {
            if (conversationTitleTMP != null) conversationTitleTMP.text = rounds[0].conversationTitle;
            if (speakerATitleTMP != null) speakerATitleTMP.text = $"{rounds[0].speakerAName}:";
            if (speakerASpeechTMP != null) speakerASpeechTMP.text = $"\"{rounds[0].speakerASpeech}\"";
            if (speakerBTitleTMP != null) speakerBTitleTMP.text = $"{rounds[0].speakerBName}:";
            if (speakerBSpeechTMP != null) speakerBSpeechTMP.text = "[ Tap the closing line below... ]";
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
            headerTMP.text = "READING BRANCH (Library Reading Desk)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "R03 Finish the Conversation";
        }

        if (subtitleTMP == null) {
            Transform sTrans = transform.Find("HeaderContainer/Subtitle") ?? transform.Find("Instruction") ?? transform.Find("Subtitle");
            if (sTrans != null) subtitleTMP = sTrans.GetComponent<TextMeshProUGUI>();
        }
        if (subtitleTMP != null) {
            subtitleTMP.text = "Read the conversation and tap the line that finishes it, using the right idiom!";
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
            else if (conversationTitleTMP == null && n.Contains("convtitle")) conversationTitleTMP = tmp;
            else if (speakerATitleTMP == null && n.Contains("speakeratitle")) speakerATitleTMP = tmp;
            else if (speakerASpeechTMP == null && n.Contains("speakeraspeech")) speakerASpeechTMP = tmp;
            else if (speakerBTitleTMP == null && n.Contains("speakerbtitle")) speakerBTitleTMP = tmp;
            else if (speakerBSpeechTMP == null && n.Contains("speakerbspeech")) speakerBSpeechTMP = tmp;
            else if (feedbackTextTMP == null && n.Contains("feedback")) feedbackTextTMP = tmp;
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> optBtns = new List<Button>();
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("opt") || n.Contains("chip") || n.Contains("choice")) {
                optBtns.Add(btn);
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

        if (optBtns.Count > 0) {
            optionButtons = optBtns.ToArray();
            optionImages = new Image[optionButtons.Length];
            optionTexts = new TextMeshProUGUI[optionButtons.Length];
            for (int i = 0; i < optionButtons.Length; i++) {
                optionImages[i] = optionButtons[i].GetComponent<Image>();
                optionTexts[i] = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        if (conversationCardObject == null) {
            Transform ccTrans = transform.Find("ConversationStage") ?? transform.Find("DialogueContainer") ?? transform.Find("StageCard");
            if (ccTrans != null) conversationCardObject = ccTrans.gameObject;
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel") ?? transform.Find("ResultPopup");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    public void PopulateFailsafeRounds() {
        rounds = new ColourYourSpeech_ReadingR03ConversationRound[] {
            // Round 1: Zara & Stella
            new ColourYourSpeech_ReadingR03ConversationRound {
                roundId = 1,
                conversationTitle = "Zara & Stella - The Busy Weekend",
                speakerAName = "Stella",
                speakerASpeech = "Zara, did you really promise to help four people and still finish all your homework?",
                speakerBName = "Zara",
                correctClosingLine = "Yes. I think I bit off more than I could chew.",
                idiomName = "Bite off more than you can chew",
                idiomMeaning = "To take on a task that is too big",
                distractorLines = new string[] {
                    "Yes. I think it's a piece of cake.",
                    "Yes. I think we had a blast."
                }
            },
            // Round 2: Granny & Clintan
            new ColourYourSpeech_ReadingR03ConversationRound {
                roundId = 2,
                conversationTitle = "Granny & Clintan - The Field Trip",
                speakerAName = "Granny",
                speakerASpeech = "How was the science museum field trip today, Clintan?",
                speakerBName = "Clintan",
                correctClosingLine = "We had a blast, Granny!",
                idiomName = "Have a blast",
                idiomMeaning = "To enjoy a lot",
                distractorLines = new string[] {
                    "We buried the hatchet, Granny!",
                    "We missed the boat, Granny!"
                }
            },
            // Round 3: Naina & Veer
            new ColourYourSpeech_ReadingR03ConversationRound {
                roundId = 3,
                conversationTitle = "Naina & Veer - The Dance Auditions",
                speakerAName = "Veer",
                speakerASpeech = "Naina, are the dance auditions still open today?",
                speakerBName = "Naina",
                correctClosingLine = "I'm sorry. You missed the boat. But you can try next year.",
                idiomName = "Miss the boat",
                idiomMeaning = "To miss a chance",
                distractorLines = new string[] {
                    "I'm sorry. The cat is out of the bag.",
                    "I'm sorry. It was raining cats and dogs."
                }
            },
            // Round 4: Rita & Ravi
            new ColourYourSpeech_ReadingR03ConversationRound {
                roundId = 4,
                conversationTitle = "Rita & Ravi - The Secret Party",
                speakerAName = "Rita",
                speakerASpeech = "Ravi! You told everyone about my surprise birthday party!",
                speakerBName = "Ravi",
                correctClosingLine = "Haha! The cat is out of the bag!",
                idiomName = "The cat is out of the bag",
                idiomMeaning = "The secret is given away",
                distractorLines = new string[] {
                    "Haha! I had a close shave!",
                    "Haha! It's a piece of cake!"
                }
            },
            // Round 5: Sonu & Rohan
            new ColourYourSpeech_ReadingR03ConversationRound {
                roundId = 5,
                conversationTitle = "Sonu & Rohan - Why So Drenched?",
                speakerAName = "Sonu",
                speakerASpeech = "Rohan, your uniform is dripping wet! What happened?",
                speakerBName = "Rohan",
                correctClosingLine = "I got drenched as it was raining cats and dogs.",
                idiomName = "Raining cats and dogs",
                idiomMeaning = "Raining heavily",
                distractorLines = new string[] {
                    "I got drenched as I bit off more than I could chew.",
                    "I got drenched as we buried the hatchet."
                }
            },
            // Round 6: Simran & Mom
            new ColourYourSpeech_ReadingR03ConversationRound {
                roundId = 6,
                conversationTitle = "Simran & Mom - Friends Again",
                speakerAName = "Mom",
                speakerASpeech = "Simran, aren't you and Karan not talking to each other?",
                speakerBName = "Simran",
                correctClosingLine = "We buried the hatchet, mom. We are friends again.",
                idiomName = "Bury the hatchet",
                idiomMeaning = "To make up with someone after an argument",
                distractorLines = new string[] {
                    "We missed the boat, mom. We are friends again.",
                    "We had a close shave, mom. We are friends again."
                }
            },
            // Round 7: Radha & Mohan
            new ColourYourSpeech_ReadingR03ConversationRound {
                roundId = 7,
                conversationTitle = "Radha & Mohan - Near the Drain",
                speakerAName = "Radha",
                speakerASpeech = "Mohan! I saw your cycle slip right on the edge of the open drain! Are you hurt?",
                speakerBName = "Mohan",
                correctClosingLine = "No, I am ok. I had a close shave.",
                idiomName = "A close shave",
                idiomMeaning = "Narrowly escape a disaster",
                distractorLines = new string[] {
                    "No, I am ok. The cat is out of the bag.",
                    "No, I am ok. It was a piece of cake."
                }
            },
            // Round 8: Harvey & Mike
            new ColourYourSpeech_ReadingR03ConversationRound {
                roundId = 8,
                conversationTitle = "Harvey & Mike - Mutual Help",
                speakerAName = "Harvey",
                speakerASpeech = "Mike, if you help me solve these tough maths problems, I will help you with English grammar.",
                speakerBName = "Mike",
                correctClosingLine = "Ok Harvey. You scratch my back I will scratch yours.",
                idiomName = "You scratch my back I will scratch yours",
                idiomMeaning = "You help me and I will help you",
                distractorLines = new string[] {
                    "Ok Harvey. It is raining cats and dogs.",
                    "Ok Harvey. I bit off more than I could chew."
                }
            }
        };

        // Prepare 3 options per round with even distribution
        for (int r = 0; r < rounds.Length; r++) {
            var round = rounds[r];
            int correctIndex = r % 3; // Distribute across buttons 0, 1, 2
            round.correctOptionIndex = correctIndex;

            List<string> options = new List<string>();
            int distractorIdx = 0;

            for (int i = 0; i < 3; i++) {
                if (i == correctIndex) {
                    options.Add(round.correctClosingLine);
                } else if (distractorIdx < round.distractorLines.Length) {
                    options.Add(round.distractorLines[distractorIdx]);
                    distractorIdx++;
                } else {
                    options.Add("It's a piece of cake.");
                }
            }

            round.optionLines = options.ToArray();
        }
    }

    public void StartNewGame() {
        currentRoundIndex = 0;
        score = 0;
        isHandlingAnswer = false;

        if (optionButtons == null || optionButtons.Length == 0) {
            AutoBindReferences();
        }

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

        if (roundIdx >= rounds.Length || roundIdx >= 8) {
            EndGame();
            return;
        }

        var round = rounds[roundIdx];
        isHandlingAnswer = false;
        retriesRemaining = 1;

        if (progressTMP != null) progressTMP.text = $"Conversation {roundIdx + 1}/8";

        if (conversationTitleTMP != null) conversationTitleTMP.text = round.conversationTitle;

        if (speakerATitleTMP != null) speakerATitleTMP.text = $"{round.speakerAName}:";
        if (speakerASpeechTMP != null) {
            speakerASpeechTMP.text = $"\"{round.speakerASpeech}\"";
            speakerASpeechTMP.transform.DOKill();
            speakerASpeechTMP.transform.localScale = Vector3.zero;
            speakerASpeechTMP.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
        }

        if (speakerBTitleTMP != null) speakerBTitleTMP.text = $"{round.speakerBName}:";
        if (speakerBSpeechTMP != null) {
            speakerBSpeechTMP.text = "[ Tap the closing line below... ]";
            speakerBSpeechTMP.color = new Color(0.8f, 0.85f, 0.95f, 0.7f);
        }

        if (feedbackBanner != null) feedbackBanner.SetActive(false);

        PlayConversationAudio(round);

        // Setup 3 Option Chips
        for (int i = 0; i < optionButtons.Length; i++) {
            if (i < round.optionLines.Length && optionButtons[i] != null) {
                optionButtons[i].gameObject.SetActive(true);
                optionButtons[i].interactable = true;

                if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) {
                    optionTexts[i].text = round.optionLines[i];
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

        if (replayAudioBtn != null) replayAudioBtn.interactable = true;
    }

    private void PlayConversationAudio(ColourYourSpeech_ReadingR03ConversationRound round) {
        if (round != null && round.conversationAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(round.conversationAudio);
        }
    }

    private void OnOptionSelected(int optionIndex) {
        if (isHandlingAnswer || isIntroPlaying || currentRoundIndex >= rounds.Length) return;

        var round = rounds[currentRoundIndex];
        bool isCorrect = (optionIndex == round.correctOptionIndex);

        if (isCorrect) {
            isHandlingAnswer = true;
            SetOptionButtonsInteractable(false);
            if (replayAudioBtn != null) replayAudioBtn.interactable = false;

            score++;
            UpdateScoreUI();

            if (sfxCorrect != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(sfxCorrect);
            } else if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            // Fill Speaker B speech bubble with correct line
            if (speakerBSpeechTMP != null) {
                speakerBSpeechTMP.text = $"\"{round.correctClosingLine}\"";
                speakerBSpeechTMP.color = new Color(0.2f, 1f, 0.4f, 1f);
                speakerBSpeechTMP.transform.DOKill();
                speakerBSpeechTMP.transform.DOScale(Vector3.one * 1.05f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }

            if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = correctChipColor;
            }

            if (optionButtons != null && optionIndex < optionButtons.Length && optionButtons[optionIndex] != null) {
                optionButtons[optionIndex].transform.DOKill();
                optionButtons[optionIndex].transform.DOScale(Vector3.one * 1.08f, 0.15f).SetLoops(2, LoopType.Yoyo);
            }

            ShowFeedback($"Correct! {round.idiomName}: {round.idiomMeaning}", true);

            float delay = 2.0f;
            if (round.closingAudio != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(round.closingAudio);
                delay = Mathf.Max(delay, round.closingAudio.length + 0.4f);
            }

            StartCoroutine(AdvanceAfterDelay(delay));
        } else {
            if (retriesRemaining > 0) {
                retriesRemaining--;
                if (sfxWrong != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(sfxWrong);
                } else if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                    optionImages[optionIndex].color = wrongChipColor;
                }

                if (optionButtons != null && optionIndex < optionButtons.Length && optionButtons[optionIndex] != null) {
                    optionButtons[optionIndex].interactable = false;
                    optionButtons[optionIndex].transform.DOShakePosition(0.25f, new Vector3(8f, 0f, 0f), 10, 90, false, true);
                }

                ShowFeedback($"Meaning hint: Think about '{round.idiomMeaning}'. Try another closing line!", false);
            } else {
                isHandlingAnswer = true;
                SetOptionButtonsInteractable(false);
                if (replayAudioBtn != null) replayAudioBtn.interactable = false;

                if (sfxWrong != null && Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(sfxWrong);
                } else if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                }

                if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                    optionImages[optionIndex].color = wrongChipColor;
                }

                // Reveal correct answer in green
                if (optionImages != null && round.correctOptionIndex < optionImages.Length && optionImages[round.correctOptionIndex] != null) {
                    optionImages[round.correctOptionIndex].color = correctChipColor;
                }

                if (speakerBSpeechTMP != null) {
                    speakerBSpeechTMP.text = $"\"{round.correctClosingLine}\"";
                    speakerBSpeechTMP.color = new Color(0.2f, 1f, 0.4f, 1f);
                }

                ShowFeedback($"The correct closing line is: \"{round.correctClosingLine}\" ({round.idiomName})", false);
                StartCoroutine(AdvanceAfterDelay(2.4f));
            }
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
            feedbackBanner.transform.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack);
        }
    }

    private void OnReplayAudioClicked() {
        if (isIntroPlaying || currentRoundIndex >= rounds.Length) return;
        PlayConversationAudio(rounds[currentRoundIndex]);
        if (conversationCardObject != null) {
            conversationCardObject.transform.DOKill();
            conversationCardObject.transform.DOScale(Vector3.one * 1.03f, 0.15f).SetLoops(2, LoopType.Yoyo);
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

        bool isPassed = (score >= 6);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (resultTitleTMP != null) {
            resultTitleTMP.text = isPassed ? "CONVERSATION CHALLENGE COMPLETED!" : "CHALLENGE NOT PASSED";
            resultTitleTMP.color = isPassed ? new Color(0.15f, 0.85f, 0.4f) : new Color(0.95f, 0.35f, 0.35f);
        }

        if (resultScoreTMP != null) {
            resultScoreTMP.text = $"Completed: {score} / 8 Conversations (Target: 6)";
        }

        if (resultStatusTMP != null) {
            resultStatusTMP.text = isPassed
                ? "Brilliant! You finished the conversations using the right idioms!"
                : "Try again! Finish at least 6 of 8 conversations to pass!";
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
