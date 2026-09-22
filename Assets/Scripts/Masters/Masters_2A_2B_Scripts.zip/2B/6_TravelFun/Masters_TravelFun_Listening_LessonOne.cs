using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// Unit 6: Travel Fun — Listening Lesson One (L01: Hear It — Which Moment of the Journey?)
/// 10 audio-to-picture recognition rounds with instant repeat & slow speed support.
/// Pass mark = 8 / 10.
/// </summary>
public class Masters_TravelFun_Listening_LessonOne : Masters_Lesson {

[System.Serializable]
public class TravelMomentRoundData {
    public string phrasePromptText;
    public AudioClip promptAudio;
    public AudioClip slowPromptAudio;
    public string correctMomentTitle;
    public string[] distractorMomentTitles; // 3 distractors
    public string exampleSentenceText;
    public AudioClip exampleAudio;
}

    [Header("10 Travel Moment Rounds")]
    [SerializeField]
    private TravelMomentRoundData[] rounds;

    [Header("UI References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI promptTextTMP;
    [SerializeField]
    private TextMeshProUGUI exampleSentenceTMP;

    [Header("4 Moment Card Buttons")]
    [SerializeField]
    private Button[] momentCardButtons; // CardButton_01 .. CardButton_04

    [Header("Audio Controls")]
    [SerializeField]
    private Button replayAudioBtn;
    [SerializeField]
    private Toggle repeatThisToggle;
    [SerializeField]
    private Toggle slowToggle;

    [Header("Results & Retry Panel")]
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultScoreTMP;
    [SerializeField]
    private TextMeshProUGUI resultStatusTMP;
    [SerializeField]
    private Button retryBtn;

    [Header("Colors & Styling")]
    [SerializeField]
    private Color defaultCardColor = new Color(0.14f, 0.38f, 0.58f, 1f); // Medium Blue #235E8A
    [SerializeField]
    private Color correctColor = new Color(0.14f, 0.53f, 0.22f, 1f);     // Emerald Green #248838
    [SerializeField]
    private Color wrongColor = new Color(0.71f, 0.15f, 0.15f, 1f);       // Crimson Red #B52626

    private int currentRoundIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;
    private bool currentRoundHasRetried = false;
    private bool isSlowMode = false;

    private Image[] cardImages;
    private Color[] originalCardColors;
    private string[] currentRoundShuffledMoments;
    private int currentRoundCorrectChoiceIndex;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Listening;

        PurgeLegacyChildren();
        AutoBindReferences();

        if (rounds == null || rounds.Length == 0 || rounds[0] == null || string.IsNullOrEmpty(rounds[0].phrasePromptText) || !rounds[0].phrasePromptText.Contains("Set off") || rounds[0].promptAudio == null) {
            PopulateFailsafeRounds();
        }

        InitCardButtons();

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
        }

        if (repeatThisToggle != null) {
            repeatThisToggle.onValueChanged.RemoveAllListeners();
            repeatThisToggle.onValueChanged.AddListener((isOn) => {
                OnReplayAudioClicked();
            });
        }

        if (slowToggle != null) {
            slowToggle.onValueChanged.RemoveAllListeners();
            slowToggle.onValueChanged.AddListener((isOn) => {
                isSlowMode = isOn;
                OnReplayAudioClicked();
            });
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
        topic = Masters_Topic.Listening;

        PurgeLegacyChildren();
        EnsureHeaderAndTitle();

        if (rounds == null || rounds.Length == 0 || rounds[0] == null || string.IsNullOrEmpty(rounds[0].phrasePromptText) || !rounds[0].phrasePromptText.Contains("Set off") || rounds[0].promptAudio == null) {
            PopulateFailsafeRounds();
        }

        currentRoundIndex = 0;
        score = 0;
        isProcessingInput = false;

        if (resultPanel != null) resultPanel.SetActive(false);

        // Populate Round 1 UI immediately on frame 1 so cards are never blank or showing old text
        SetupRoundUI(currentRoundIndex);

        StartCoroutine(StartWithIntroAudioRoutine());
    }

    private void InitCardButtons() {
        if (momentCardButtons != null && momentCardButtons.Length > 0) {
            cardImages = new Image[momentCardButtons.Length];
            originalCardColors = new Color[momentCardButtons.Length];

            for (int i = 0; i < momentCardButtons.Length; i++) {
                if (momentCardButtons[i] != null) {
                    int index = i;
                    cardImages[i] = momentCardButtons[i].GetComponent<Image>();
                    if (cardImages[i] != null) {
                        originalCardColors[i] = cardImages[i].color;
                    } else {
                        originalCardColors[i] = defaultCardColor;
                    }

                    momentCardButtons[i].onClick.RemoveAllListeners();
                    momentCardButtons[i].onClick.AddListener(() => OnMomentCardSelected(index));
                }
            }
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

        var legacyOpts = GetComponentsInChildren<Masters_ListeningOptionButton>(true);
        foreach (var lo in legacyOpts) {
            Destroy(lo);
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "🎧 LISTENING BRANCH (Radio Kiosk)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "L01 Hear It — Which Moment of the Journey?";
            titleTMP.color = new Color(1f, 0.85f, 0.05f, 1f); // Yellow
        }
    }

    private void AutoBindReferences() {
        Transform grid = transform.Find("PhraseCardsGrid") ?? transform.Find("Controls");
        List<Button> chipBtns = new List<Button>();

        string[] exactBtnNames = new string[] { "OptionButton", "OptionButton (1)", "OptionButton (2)", "OptionButton (3)" };
        foreach (string bName in exactBtnNames) {
            Transform bt = (grid != null) ? grid.Find(bName) : null;
            if (bt == null) bt = transform.Find(bName) ?? transform.Find($"PhraseCardsGrid/{bName}") ?? transform.Find($"Controls/{bName}");
            if (bt != null) {
                Button b = bt.GetComponent<Button>();
                if (b != null) chipBtns.Add(b);
            }
        }

        if (chipBtns.Count < 4) {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (Button btn in buttons) {
                string n = btn.gameObject.name.ToLower();
                if ((n.Contains("optionbutton") || n.Contains("card") || n.Contains("chip")) && !n.Contains("next") && !n.Contains("back") && !n.Contains("audio") && !n.Contains("slow") && !n.Contains("repeat")) {
                    if (!chipBtns.Contains(btn)) chipBtns.Add(btn);
                }
            }
        }

        if (momentCardButtons == null || momentCardButtons.Length < 4 || System.Array.Exists(momentCardButtons, b => b == null)) {
            if (chipBtns.Count >= 4) {
                momentCardButtons = new Button[4];
                for (int i = 0; i < 4; i++) {
                    momentCardButtons[i] = chipBtns[i];
                }
            }
        }

        // Bind Repeat This and Slow Controls
        Transform repToggleTr = transform.Find("Controls/RepeatThisToggle") ?? transform.Find("RepeatThisToggle") ?? transform.Find("Controls/RepeatThis");
        if (repToggleTr != null) {
            repeatThisToggle = repToggleTr.GetComponent<Toggle>();
            if (repeatThisToggle == null) replayAudioBtn = repToggleTr.GetComponent<Button>();
        }

        Transform slowToggleTr = transform.Find("Controls/SlowToggle") ?? transform.Find("SlowToggle");
        if (slowToggleTr != null) {
            slowToggle = slowToggleTr.GetComponent<Toggle>();
        }

        if (replayAudioBtn == null) {
            Transform ab = transform.Find("Audio") ?? transform.Find("Controls/SpeakerIcon") ?? transform.Find("SpeakerIcon") ?? transform.Find("ReplayAudioBtn");
            if (ab != null) replayAudioBtn = ab.GetComponent<Button>();
        }

        if (nextButton == null) {
            Transform nb = transform.Find("NextButton") ?? transform.Find("Next");
            if (nb != null) nextButton = nb.GetComponent<Button>();
        }

        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (progressTMP == null && (n.Contains("progresstext") || n.Contains("progress") || n.Contains("counter") || n.Contains("count") || n.Contains("expressioncount"))) {
                progressTMP = tmp;
            } else if (promptTextTMP == null && (n.Contains("prompt") || n.Contains("statement") || n.Contains("question"))) {
                promptTextTMP = tmp;
            } else if (exampleSentenceTMP == null && (n.Contains("example") || n.Contains("sentence") || n.Contains("feedback"))) {
                exampleSentenceTMP = tmp;
            } else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) {
                titleTMP = tmp;
            } else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) {
                headerTMP = tmp;
            }
        }
    }

    public void PopulateFailsafeRounds() {
        string audioDir = "Assets/Audio/2B/6_TravelFun/Listening/";

        rounds = new TravelMomentRoundData[] {
            new TravelMomentRoundData {
                phrasePromptText = "“Set off” — to start a journey.",
                correctMomentTitle = "A family waving goodbye at the gate",
                distractorMomentTitles = new string[] { "Plane landing on runway", "Hotel check-in desk", "Bus arriving at station" },
                exampleSentenceText = "“We set off early in the morning to beat the traffic.”",
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r01_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/tf_l01_r01_prompt.mp3"),
                exampleAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r01_example.mp3")
#endif
            },
            new TravelMomentRoundData {
                phrasePromptText = "“Check in” — to arrive and register at a hotel or airport.",
                correctMomentTitle = "Suitcase being wheeled to a hotel reception desk",
                distractorMomentTitles = new string[] { "Boarding a train", "Waving goodbye", "Unpacking bags at home" },
                exampleSentenceText = "“Please check in at the desk two hours before departure.”",
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r02_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/tf_l01_r02_prompt.mp3"),
                exampleAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r02_example.mp3")
#endif
            },
            new TravelMomentRoundData {
                phrasePromptText = "“Take off” — when an aircraft leaves the ground and begins to fly.",
                correctMomentTitle = "A plane lifting off into the sky",
                distractorMomentTitles = new string[] { "Plane landing on runway", "Train departing platform", "Waiting in lounge" },
                exampleSentenceText = "“The plane took off smoothly on time.”",
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r03_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/tf_l01_r03_prompt.mp3"),
                exampleAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r03_example.mp3")
#endif
            },
            new TravelMomentRoundData {
                phrasePromptText = "“Get on” — to board a bus, train, or plane.",
                correctMomentTitle = "A passenger stepping onto a bus",
                distractorMomentTitles = new string[] { "Stepping off a train", "Sitting in taxi", "Driving a car" },
                exampleSentenceText = "“Get on the bus before the doors close.”",
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r04_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/tf_l01_r04_prompt.mp3"),
                exampleAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r04_example.mp3")
#endif
            },
            new TravelMomentRoundData {
                phrasePromptText = "“Get off” — to leave a bus, train, or plane.",
                correctMomentTitle = "Passengers stepping down at a bus stop",
                distractorMomentTitles = new string[] { "Stepping onto an aircraft", "Waiting at station", "Checking luggage" },
                exampleSentenceText = "“Make sure you get off at the central station.”",
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r05_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/tf_l01_r05_prompt.mp3"),
                exampleAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r05_example.mp3")
#endif
            },
            new TravelMomentRoundData {
                phrasePromptText = "“See off” — to go to the station or airport to say goodbye.",
                correctMomentTitle = "Friends hugging and saying farewell on a platform",
                distractorMomentTitles = new string[] { "Arriving at hotel", "Buying tickets", "Boarding a ferry" },
                exampleSentenceText = "“My grandparents came to the station to see us off.”",
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r06_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/tf_l01_r06_prompt.mp3"),
                exampleAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r06_example.mp3")
#endif
            },
            new TravelMomentRoundData {
                phrasePromptText = "“Touch down” — when an aircraft lands on the runway.",
                correctMomentTitle = "Airplane wheels touching down on the tarmac",
                distractorMomentTitles = new string[] { "Plane taking off", "Cruising at high altitude", "Loading cargo" },
                exampleSentenceText = "“The flight touched down safely in London.”",
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r07_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/tf_l01_r07_prompt.mp3"),
                exampleAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r07_example.mp3")
#endif
            },
            new TravelMomentRoundData {
                phrasePromptText = "“Stop over” — to stay somewhere briefly during a long journey.",
                correctMomentTitle = "Traveller resting overnight at a transit lounge",
                distractorMomentTitles = new string[] { "Walking onto a plane", "Fast driving on highway", "Calling a cab" },
                exampleSentenceText = "“We stopped over in Dubai for one night on our way to Tokyo.”",
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r08_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/tf_l01_r08_prompt.mp3"),
                exampleAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r08_example.mp3")
#endif
            },
            new TravelMomentRoundData {
                phrasePromptText = "“Look around” — to explore and visit things in a new place.",
                correctMomentTitle = "Tourists taking photos of historic monuments",
                distractorMomentTitles = new string[] { "Sleeping on a bus", "Packing a suitcase", "Checking passport" },
                exampleSentenceText = "“We spent the afternoon looking around the old city.”",
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r09_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/tf_l01_r09_prompt.mp3"),
                exampleAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r09_example.mp3")
#endif
            },
            new TravelMomentRoundData {
                phrasePromptText = "“Head back” — to return to where you started.",
                correctMomentTitle = "Travellers walking towards the return shuttle bus",
                distractorMomentTitles = new string[] { "Starting the journey", "Checking into hotel", "Taking off" },
                exampleSentenceText = "“It was getting dark, so we decided to head back.”",
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r10_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "Slow/tf_l01_r10_prompt.mp3"),
                exampleAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(audioDir + "tf_l01_r10_example.mp3")
#endif
            }
        };
    }

    private IEnumerator StartWithIntroAudioRoutine() {
        isProcessingInput = true;

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            float delay = narratorSpeech.length > 0 ? narratorSpeech.length : 2.5f;
            yield return new WaitForSeconds(delay);
        }

        isProcessingInput = false;

        if (rounds != null && currentRoundIndex < rounds.Length) {
            PlayPromptAudio(rounds[currentRoundIndex]);
        }
    }

    public void StartLesson() {
        currentRoundIndex = 0;
        score = 0;
        isProcessingInput = false;

        if (resultPanel != null) resultPanel.SetActive(false);

        SetupRoundUI(currentRoundIndex);
        PlayPromptAudio(rounds[currentRoundIndex]);
    }

    private void SetupRoundUI(int roundIdx) {
        if (rounds == null || rounds.Length == 0 || roundIdx >= rounds.Length) return;

        isProcessingInput = false;
        currentRoundHasRetried = false;
        EnsureHeaderAndTitle();
        ResetCardVisuals();

        TravelMomentRoundData r = rounds[roundIdx];

        if (progressTMP != null) {
            progressTMP.text = $"Round {roundIdx + 1}/{rounds.Length}";
        }

        if (promptTextTMP != null) {
            promptTextTMP.text = r.phrasePromptText;
        }

        if (exampleSentenceTMP != null) {
            exampleSentenceTMP.text = "";
        }

        // Build 4 choices (1 correct + 3 distractors)
        List<string> choices = new List<string> { r.correctMomentTitle };
        if (r.distractorMomentTitles != null) {
            foreach (var d in r.distractorMomentTitles) {
                if (!string.IsNullOrEmpty(d)) choices.Add(d);
            }
        }

        while (choices.Count < 4) {
            choices.Add("Moment");
        }

        // Distribute correct option across buttons 0, 1, 2, 3
        int targetCorrectIndex = roundIdx % 4;
        string correctVal = choices[0];

        System.Random rng = new System.Random(roundIdx * 23 + 51);
        for (int i = choices.Count - 1; i > 0; i--) {
            int k = rng.Next(i + 1);
            string tmpStr = choices[i];
            choices[i] = choices[k];
            choices[k] = tmpStr;
        }

        int actualCorrectIdx = choices.IndexOf(correctVal);
        if (actualCorrectIdx != -1 && actualCorrectIdx != targetCorrectIndex) {
            string tmpStr = choices[targetCorrectIndex];
            choices[targetCorrectIndex] = correctVal;
            choices[actualCorrectIdx] = tmpStr;
        }

        currentRoundShuffledMoments = choices.ToArray();
        currentRoundCorrectChoiceIndex = targetCorrectIndex;

        if (momentCardButtons != null) {
            for (int i = 0; i < momentCardButtons.Length; i++) {
                if (momentCardButtons[i] != null) {
                    if (i < currentRoundShuffledMoments.Length) {
                        momentCardButtons[i].gameObject.SetActive(true);
                        SetButtonText(momentCardButtons[i], currentRoundShuffledMoments[i]);
                    } else {
                        momentCardButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    private void PlayPromptAudio(TravelMomentRoundData r) {
        AudioClip clipToPlay = (isSlowMode && r.slowPromptAudio != null) ? r.slowPromptAudio : r.promptAudio;

        if (clipToPlay != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
        }
    }

    private void SetButtonText(Button btn, string textValue) {
        if (btn == null) return;
        TextMeshProUGUI tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null) {
            tmp.text = textValue;
            tmp.enabled = true;
            tmp.gameObject.SetActive(true);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 18;
            tmp.fontSizeMax = 28;
            tmp.margin = new Vector4(15, 10, 15, 10);
        } else {
            UnityEngine.UI.Text txt = btn.GetComponentInChildren<UnityEngine.UI.Text>(true);
            if (txt != null) {
                txt.text = textValue;
                txt.enabled = true;
                txt.gameObject.SetActive(true);
                txt.alignment = TextAnchor.MiddleCenter;
            }
        }
    }

    private void ResetCardVisuals() {
        if (momentCardButtons != null) {
            for (int i = 0; i < momentCardButtons.Length; i++) {
                if (momentCardButtons[i] != null) {
                    momentCardButtons[i].transform.DOKill(true);
                    momentCardButtons[i].interactable = true;

                    if (cardImages != null && i < cardImages.Length && cardImages[i] != null) {
                        cardImages[i].color = defaultCardColor;
                    }
                }
            }
        }
    }

    private void OnMomentCardSelected(int index) {
        if (isProcessingInput) return;
        if (momentCardButtons == null || index >= momentCardButtons.Length || momentCardButtons[index] == null) return;

        bool isCorrect = (index == currentRoundCorrectChoiceIndex);
        Button btn = momentCardButtons[index];
        Image img = cardImages != null && index < cardImages.Length ? cardImages[index] : null;
        TravelMomentRoundData r = rounds[currentRoundIndex];

        if (isCorrect) {
            isProcessingInput = true;
            score++;

            if (img != null) img.color = correctColor;
            if (btn != null) btn.transform.DOPunchScale(Vector3.one * 0.15f, 0.35f);

            if (exampleSentenceTMP != null) {
                exampleSentenceTMP.text = r.exampleSentenceText;
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                if (r.exampleAudio != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(r.exampleAudio);
                }
            }

            float waitDelay = (r.exampleAudio != null && r.exampleAudio.length > 0) ? r.exampleAudio.length + 0.8f : 2.5f;
            StartCoroutine(NextRoundRoutine(waitDelay));
        } else {
            if (img != null) img.color = wrongColor;
            if (btn != null) {
                btn.transform.DOShakePosition(0.4f, new Vector3(15f, 0f, 0f), 10, 90, false, true);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (!currentRoundHasRetried) {
                currentRoundHasRetried = true;
                btn.interactable = false;
            } else {
                if (currentRoundCorrectChoiceIndex >= 0 && currentRoundCorrectChoiceIndex < cardImages.Length) {
                    if (cardImages[currentRoundCorrectChoiceIndex] != null) {
                        cardImages[currentRoundCorrectChoiceIndex].color = correctColor;
                    }
                }
                isProcessingInput = true;
                StartCoroutine(NextRoundRoutine(2.0f));
            }
        }
    }

    private IEnumerator NextRoundRoutine(float delay) {
        yield return new WaitForSeconds(delay);
        currentRoundIndex++;
        if (currentRoundIndex >= rounds.Length) {
            CompleteLesson();
        } else {
            SetupRoundUI(currentRoundIndex);
            PlayPromptAudio(rounds[currentRoundIndex]);
        }
    }

    public void OnReplayAudioClicked() {
        if (rounds == null || currentRoundIndex >= rounds.Length) return;
        TravelMomentRoundData r = rounds[currentRoundIndex];
        PlayPromptAudio(r);
    }

    private void CompleteLesson() {
        isProcessingInput = true;

        bool passed = (score >= 8);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            resultPanel.transform.DOKill();
            resultPanel.transform.localScale = Vector3.zero;
            resultPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);

            if (resultScoreTMP != null) {
                resultScoreTMP.text = $"Score: {score} / {rounds.Length}";
            }

            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed
                    ? "<color=#248838><b>SUPERB LISTENING!</b>\nYou mastered all 10 moments of the travel journey!</color>"
                    : "<color=#B52626><b>KEEP PRACTICING!</b>\nScore 8/10 to unlock your station badge.</color>";
            }
        }

        if (passed) {
            if (nextButton != null) nextButton.gameObject.SetActive(true);
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }
    }

    private void OnRetryButtonClicked() {
        StartLesson();
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Listening;
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
