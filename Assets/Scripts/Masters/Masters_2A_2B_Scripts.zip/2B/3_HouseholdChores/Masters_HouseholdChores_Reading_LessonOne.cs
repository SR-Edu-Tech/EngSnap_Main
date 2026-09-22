using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// Unit 3: Household Chores — Reading Lesson One (R01 Which Job Does This Need?).
/// 12 in-context situation matching rounds across the household chores:
/// 1. Introduction voiceover plays first with controls locked.
/// 2. Once introduction completes, the reading game starts and allows play.
/// 3. Student reads the messy situation and taps the chore sentence that fixes it.
/// 4. Correct -> situation highlights clean/green + ARIA confirms; Wrong -> gentle shake + retry.
/// Pass threshold: >= 10 / 12 correct.
/// </summary>
public class Masters_HouseholdChores_Reading_LessonOne : Masters_Lesson {

[System.Serializable]
public class HouseholdChores_ReadingR01RoundData {
    public string situationText;
    public string correctChoreText;
    public string[] distractorChoreTexts = new string[3];
    public AudioClip situationAudio;
    public AudioClip ariaConfirmationAudio;
}

    [Header("12 Configurable Situation Rounds")]
    [SerializeField] public HouseholdChores_ReadingR01RoundData[] rounds;

    [Header("UI Display References")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI situationCardTMP;

    [Header("4 Chore Option Buttons")]
    [SerializeField] private Button[] optionButtons = new Button[4];
    [SerializeField] private TextMeshProUGUI[] optionTexts = new TextMeshProUGUI[4];
    [SerializeField] private Image[] optionImages = new Image[4];

    [Header("Audio Controls")]
    [SerializeField] private Button replayAudioBtn;

    [Header("Results Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private TextMeshProUGUI resultStatusTMP;
    [SerializeField] private Button retryBtn;
    [SerializeField] private Button continueBtn;

    [Header("Colors & Animation")]
    [SerializeField] private Color normalBtnColor = new Color(0.18f, 0.24f, 0.36f, 1f);
    [SerializeField] private Color correctColor = new Color(0.2f, 0.8f, 0.3f, 1f);
    [SerializeField] private Color wrongColor = new Color(0.9f, 0.25f, 0.2f, 1f);

    private int currentRoundIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;
    private bool isIntroPhase = true;
    private bool currentRoundRetried = false;
    private Coroutine introRoutine;

    private string[] currentRoundShuffledOptions;
    private int currentRoundCorrectChoiceIndex = 0;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;

        PurgeLegacyChildren();
        AutoBindReferences();
        WireButtons();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Reading;

        PurgeLegacyChildren();
        AutoBindReferences();
        EnsureHeaderAndTitle();

        if (rounds == null || rounds.Length == 0) {
            PopulateDefaultRounds();
        }

        // Lock controls during introduction
        isIntroPhase = true;
        isProcessingInput = true;
        SetControlsInteractable(false);

        introRoutine = StartCoroutine(BeginLessonRoutine());
    }

    private IEnumerator BeginLessonRoutine() {
        if (progressTMP != null) {
            progressTMP.text = "Introduction";
        }

        if (situationCardTMP != null) {
            situationCardTMP.text = "\"Read each messy situation and choose the chore that fixes it!\"";
        }

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            yield return new WaitForSeconds(narratorSpeech.length + 0.3f);
        } else {
            yield return new WaitForSeconds(0.6f);
        }

        // Introduction finished — allow gameplay
        StartLesson();
    }

    public override void EnsureNextAndBackButtonWired() {
        base.EnsureNextAndBackButtonWired();
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP != null) headerTMP.text = "HOUSEHOLD CHORES";
        if (titleTMP != null) titleTMP.text = "R01 Which Job Does This Need?";
        if (subtitleTMP != null) subtitleTMP.text = "Read the situation and tap the chore that fixes it!";
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "Cloud", "Cloud (1)", "Cloud (2)", "Cloud (3)",
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer"
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

    private void SetControlsInteractable(bool interactable) {
        for (int i = 0; i < optionButtons.Length; i++) {
            if (optionButtons[i] != null) {
                optionButtons[i].interactable = interactable;
            }
        }

        if (replayAudioBtn != null) replayAudioBtn.interactable = interactable;
    }

    private void AutoBindReferences() {
        if (headerTMP == null) {
            Transform t = transform.Find("UnitHeading") ?? transform.Find("HeaderTMP") ?? transform.Find("CommonHUD/HeaderTMP") ?? transform.Find("CommonHUD/UnitHeading");
            if (t != null) headerTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP == null) {
            Transform t = transform.Find("LessonTitle") ?? transform.Find("TitleTMP") ?? transform.Find("CommonHUD/TitleTMP") ?? transform.Find("CommonHUD/LessonTitle");
            if (t != null) titleTMP = t.GetComponent<TextMeshProUGUI>();
        }
        if (progressTMP == null) {
            Transform t = transform.Find("ProgressTMP") ?? transform.Find("ExpressionCountTMP") ?? transform.Find("CommonHUD/ProgressTMP");
            if (t != null) progressTMP = t.GetComponent<TextMeshProUGUI>();
        }

        // 4 Option buttons auto-binding
        List<Button> foundOptionBtns = new List<Button>();
        string[] candidateNames = new string[] {
            "OptionButton", "OptionButton (1)", "OptionButton (2)", "OptionButton (3)",
            "OptionBtn_1", "OptionBtn_2", "OptionBtn_3", "OptionBtn_4",
            "PhraseCard_1", "PhraseCard_2", "PhraseCard_3", "PhraseCard_4"
        };
        foreach (var name in candidateNames) {
            Transform t = transform.Find(name) ?? transform.Find($"PhraseCardsGrid/{name}") ?? transform.Find($"Controls/{name}") ?? transform.Find($"OptionsContainer/{name}") ?? transform.Find($"Options/{name}");
            if (t != null) {
                Button b = t.GetComponent<Button>();
                if (b != null && !foundOptionBtns.Contains(b)) {
                    foundOptionBtns.Add(b);
                }
            }
        }

        if (foundOptionBtns.Count < 4) {
            Button[] allBtns = GetComponentsInChildren<Button>(true);
            foreach (var b in allBtns) {
                if (b == null) continue;
                string bName = b.name.ToLower();
                if (bName.Contains("back") || bName.Contains("next") || bName.Contains("replay") || bName.Contains("retry") || bName.Contains("continue") || bName.Contains("start")) continue;
                if (!foundOptionBtns.Contains(b)) {
                    foundOptionBtns.Add(b);
                    if (foundOptionBtns.Count == 4) break;
                }
            }
        }

        for (int i = 0; i < 4 && i < foundOptionBtns.Count; i++) {
            if (optionButtons[i] == null) optionButtons[i] = foundOptionBtns[i];
            if (optionButtons[i] != null) {
                if (optionTexts[i] == null) optionTexts[i] = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (optionImages[i] == null) optionImages[i] = optionButtons[i].GetComponent<Image>();
            }
        }

        if (situationCardTMP == null) {
            Transform t = transform.Find("SituationCard/Text (TMP)") ?? transform.Find("SpeechBubble/Text (TMP)") ?? transform.Find("SpeechBubble/PromptTMP") ?? transform.Find("PromptTMP") ?? transform.Find("SituationTMP");
            if (t != null) situationCardTMP = t.GetComponent<TextMeshProUGUI>() ?? t.GetComponentInChildren<TextMeshProUGUI>(true);
            if (situationCardTMP == null) {
                var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmps) {
                    string tName = tmp.name.ToLower();
                    bool isOptionText = false;
                    for (int j = 0; j < 4; j++) {
                        if (optionTexts[j] == tmp) { isOptionText = true; break; }
                    }
                    if ((tName.Contains("situation") || tName.Contains("speech") || tName.Contains("prompt") || tName.Contains("question") || tName.Contains("bubble")) && !isOptionText) {
                        situationCardTMP = tmp;
                        break;
                    }
                }
            }
        }

        if (replayAudioBtn == null) {
            Transform t = transform.Find("ReplayBtn") ?? transform.Find("ReplayAudioBtn") ?? transform.Find("SpeakerIcon") ?? transform.Find("CommonHUD/ReplayBtn") ?? transform.Find("SituationCard/SpeakerIcon") ?? transform.Find("PhraseCardsGrid/SpeakerIcon");
            if (t != null) replayAudioBtn = t.GetComponent<Button>();
        }

        if (resultPanel == null) {
            Transform t = transform.Find("ResultPanel") ?? transform.Find("ResultsContainer");
            if (t != null) {
                resultPanel = t.gameObject;
                resultScoreTMP = t.Find("ScoreTMP")?.GetComponent<TextMeshProUGUI>();
                resultStatusTMP = t.Find("StatusTMP")?.GetComponent<TextMeshProUGUI>();
                retryBtn = t.Find("RetryBtn")?.GetComponent<Button>();
                continueBtn = t.Find("ContinueBtn")?.GetComponent<Button>();
            }
        }
    }

    private void WireButtons() {
        for (int i = 0; i < 4; i++) {
            int idx = i;
            if (optionButtons[i] != null) {
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(idx));
            }
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(ReplayCurrentAudio);
        }

        if (retryBtn != null) {
            retryBtn.onClick.RemoveAllListeners();
            retryBtn.onClick.AddListener(RestartLesson);
        }

        if (continueBtn != null) {
            continueBtn.onClick.RemoveAllListeners();
            continueBtn.onClick.AddListener(OnContinueClicked);
        }
    }

    public void StartLesson() {
        isIntroPhase = false;
        currentRoundIndex = 0;
        score = 0;
        isProcessingInput = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        SetControlsInteractable(true);
        LoadRound(0);
    }

    public void RestartLesson() {
        StartLesson();
    }

    public void LoadRound(int roundIndex) {
        if (rounds == null || rounds.Length == 0) return;
        currentRoundIndex = Mathf.Clamp(roundIndex, 0, rounds.Length - 1);
        currentRoundRetried = false;
        isProcessingInput = false;
        SetControlsInteractable(true);

        HouseholdChores_ReadingR01RoundData round = rounds[currentRoundIndex];

        if (progressTMP != null) {
            progressTMP.text = $"Round {currentRoundIndex + 1} of {rounds.Length}";
        }

        if (situationCardTMP != null) {
            situationCardTMP.text = $"\"{round.situationText}\"";
        }

        // Build options: 1 correct + 3 distractors
        List<string> options = new List<string>();
        options.Add(round.correctChoreText);
        if (round.distractorChoreTexts != null) {
            foreach (var d in round.distractorChoreTexts) {
                if (!string.IsNullOrEmpty(d) && !options.Contains(d)) {
                    options.Add(d);
                }
            }
        }

        // Shuffle options
        for (int i = 0; i < options.Count; i++) {
            int rnd = Random.Range(i, options.Count);
            string tmp = options[i];
            options[i] = options[rnd];
            options[rnd] = tmp;
        }

        currentRoundShuffledOptions = options.ToArray();
        currentRoundCorrectChoiceIndex = options.IndexOf(round.correctChoreText);

        // Setup option buttons
        for (int i = 0; i < 4; i++) {
            if (optionButtons[i] == null) continue;

            bool hasOption = (i < currentRoundShuffledOptions.Length);
            optionButtons[i].gameObject.SetActive(hasOption);
            optionButtons[i].interactable = true;

            if (hasOption) {
                if (optionTexts[i] != null) {
                    optionTexts[i].text = currentRoundShuffledOptions[i];
                    optionTexts[i].maxVisibleCharacters = 99999;
                    optionTexts[i].ForceMeshUpdate();
                }

                if (optionImages[i] != null) {
                    optionImages[i].color = normalBtnColor;
                }

                optionButtons[i].transform.DOKill();
                optionButtons[i].transform.localScale = Vector3.one * 0.9f;
                optionButtons[i].transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
            }
        }

        PlayRoundAudio(round);
    }

    private void PlayRoundAudio(HouseholdChores_ReadingR01RoundData round) {
        if (round.situationAudio != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(round.situationAudio);
        }
    }

    public void ReplayCurrentAudio() {
        if (isIntroPhase) {
            if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            }
            return;
        }

        if (rounds != null && currentRoundIndex >= 0 && currentRoundIndex < rounds.Length) {
            PlayRoundAudio(rounds[currentRoundIndex]);
            if (replayAudioBtn != null) replayAudioBtn.transform.DOPunchScale(Vector3.one * 0.2f, 0.25f);
        }
    }

    public void OnOptionSelected(int optionIndex) {
        if (isProcessingInput || isIntroPhase || rounds == null || currentRoundIndex >= rounds.Length) return;

        HouseholdChores_ReadingR01RoundData round = rounds[currentRoundIndex];
        bool isCorrect = (optionIndex == currentRoundCorrectChoiceIndex);

        if (isCorrect) {
            isProcessingInput = true;
            if (!currentRoundRetried) {
                score++;
            }

            if (optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = correctColor;
            }
            if (optionButtons[optionIndex] != null) {
                optionButtons[optionIndex].transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }

            if (situationCardTMP != null) {
                situationCardTMP.text = $"<color=#55FF88>\"{round.correctChoreText}\"</color>";
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            StartCoroutine(HandleCorrectAnswerRoutine(round));
        } else {
            currentRoundRetried = true;
            if (optionImages[optionIndex] != null) {
                optionImages[optionIndex].DOColor(wrongColor, 0.2f).OnComplete(() => {
                    if (optionImages[optionIndex] != null) optionImages[optionIndex].DOColor(normalBtnColor, 0.3f);
                });
            }
            if (optionButtons[optionIndex] != null) {
                optionButtons[optionIndex].transform.DOShakePosition(0.35f, 10f, 15);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }
    }

    private IEnumerator HandleCorrectAnswerRoutine(HouseholdChores_ReadingR01RoundData round) {
        if (round.ariaConfirmationAudio != null && Masters_AudioManager.Instance != null) {
            yield return new WaitForSeconds(0.3f);
            Masters_AudioManager.Instance.PlayVoiceOver(round.ariaConfirmationAudio);
            yield return new WaitForSeconds(round.ariaConfirmationAudio.length + 0.4f);
        } else {
            yield return new WaitForSeconds(1.2f);
        }

        if (currentRoundIndex + 1 < rounds.Length) {
            LoadRound(currentRoundIndex + 1);
        } else {
            EndLesson();
        }
    }

    private void EndLesson() {
        bool passed = (score >= 10);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            if (resultScoreTMP != null) resultScoreTMP.text = $"Score: {score} / {rounds.Length}";
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "Great reading! You fixed all the messy situations!" : "Keep practicing! Tap Retry to try again.";
                resultStatusTMP.color = passed ? Color.green : Color.yellow;
            }
            if (retryBtn != null) retryBtn.gameObject.SetActive(!passed);
            if (continueBtn != null) continueBtn.gameObject.SetActive(passed);
        } else {
            OnContinueClicked();
        }
    }

    public void OnContinueClicked() {
        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Writing);
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Reading);
        } else {
            base.OnNextButtonClicked();
        }
    }

    protected override void OnNextButtonClicked() {
        if (isIntroPhase) {
            // Skip introduction directly into round 1
            if (introRoutine != null) StopCoroutine(introRoutine);
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.StopVoiceOver();
            StartLesson();
            return;
        }

        base.OnNextButtonClicked();
    }

    public void PopulateDefaultRounds() {
        string aDir = "Assets/Audio/2B/3_HouseholdChores/Reading/";

        rounds = new HouseholdChores_ReadingR01RoundData[] {
            // Round 1
            new HouseholdChores_ReadingR01RoundData {
                situationText = "Crumbs and dust are all over the floor.",
                correctChoreText = "Sweep the floor.",
                distractorChoreTexts = new string[] { "Set the table.", "Wash the car.", "Do the washing." },
#if UNITY_EDITOR
                situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r01_situation.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r01_aria.mp3")
#endif
            },
            // Round 2
            new HouseholdChores_ReadingR01RoundData {
                situationText = "Your school shirt is full of creases.",
                correctChoreText = "Iron the clothes.",
                distractorChoreTexts = new string[] { "Hang out the clothes.", "Clean the window.", "Feed the dog." },
#if UNITY_EDITOR
                situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r02_situation.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r02_aria.mp3")
#endif
            },
            // Round 3
            new HouseholdChores_ReadingR01RoundData {
                situationText = "The sheets are crumpled after last night.",
                correctChoreText = "Make the bed.",
                distractorChoreTexts = new string[] { "Wash up the dishes.", "Do the shopping.", "Take the trash out." },
#if UNITY_EDITOR
                situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r03_situation.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r03_aria.mp3")
#endif
            },
            // Round 4
            new HouseholdChores_ReadingR01RoundData {
                situationText = "Guests are coming for dinner in ten minutes.",
                correctChoreText = "Set the table.",
                distractorChoreTexts = new string[] { "Do the gardening.", "Sweep the floor.", "Iron the clothes." },
#if UNITY_EDITOR
                situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r04_situation.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r04_aria.mp3")
#endif
            },
            // Round 5
            new HouseholdChores_ReadingR01RoundData {
                situationText = "The washed plates are still wet.",
                correctChoreText = "Dry the dishes.",
                distractorChoreTexts = new string[] { "Make the bed.", "Wash the car.", "Hang out the clothes." },
#if UNITY_EDITOR
                situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r05_situation.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r05_aria.mp3")
#endif
            },
            // Round 6
            new HouseholdChores_ReadingR01RoundData {
                situationText = "The fridge is empty before the festival.",
                correctChoreText = "Do the shopping.",
                distractorChoreTexts = new string[] { "Clean the window.", "Take the trash out.", "Set the table." },
#if UNITY_EDITOR
                situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r06_situation.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r06_aria.mp3")
#endif
            },
            // Round 7
            new HouseholdChores_ReadingR01RoundData {
                situationText = "The laundry basket is overflowing.",
                correctChoreText = "Do the washing.",
                distractorChoreTexts = new string[] { "Dry the dishes.", "Feed the dog.", "Sweep the floor." },
#if UNITY_EDITOR
                situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r07_situation.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r07_aria.mp3")
#endif
            },
            // Round 8
            new HouseholdChores_ReadingR01RoundData {
                situationText = "The plants are dry and the weeds are growing.",
                correctChoreText = "Do the gardening.",
                distractorChoreTexts = new string[] { "Wash up the dishes.", "Iron the clothes.", "Make the bed." },
#if UNITY_EDITOR
                situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r08_situation.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r08_aria.mp3")
#endif
            },
            // Round 9
            new HouseholdChores_ReadingR01RoundData {
                situationText = "The bin in the kitchen is full.",
                correctChoreText = "Take the trash out.",
                distractorChoreTexts = new string[] { "Do the shopping.", "Clean the window.", "Set the table." },
#if UNITY_EDITOR
                situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r09_situation.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r09_aria.mp3")
#endif
            },
            // Round 10
            new HouseholdChores_ReadingR01RoundData {
                situationText = "The puppy is waiting by his empty bowl.",
                correctChoreText = "Feed the dog.",
                distractorChoreTexts = new string[] { "Wash the car.", "Do the washing.", "Hang out the clothes." },
#if UNITY_EDITOR
                situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r10_situation.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r10_aria.mp3")
#endif
            },
            // Round 11
            new HouseholdChores_ReadingR01RoundData {
                situationText = "The car is covered in dust after the trip.",
                correctChoreText = "Wash the car.",
                distractorChoreTexts = new string[] { "Sweep the floor.", "Dry the dishes.", "Make the bed." },
#if UNITY_EDITOR
                situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r11_situation.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r11_aria.mp3")
#endif
            },
            // Round 12
            new HouseholdChores_ReadingR01RoundData {
                situationText = "You cannot see out of the glass any more.",
                correctChoreText = "Clean the window.",
                distractorChoreTexts = new string[] { "Do the gardening.", "Take the trash out.", "Iron the clothes." },
#if UNITY_EDITOR
                situationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r12_situation.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_r01_r12_aria.mp3")
#endif
            }
        };
    }
}
