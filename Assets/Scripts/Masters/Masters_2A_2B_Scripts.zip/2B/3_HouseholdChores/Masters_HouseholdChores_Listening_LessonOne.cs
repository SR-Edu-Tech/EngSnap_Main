using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


/// <summary>
/// Unit 3: Household Chores — Listening Lesson One (L01 Hear It — Which Chore?).
/// 10 Rounds of Audio-to-Chore Recognition:
/// Introduction voiceover plays first.
/// Once introduction completes, gameplay is enabled.
/// ARIA reads a chore sentence aloud.
/// 4 chore options appear. Student taps the matching chore option.
/// Pass threshold: >= 8 / 10 correct.
/// </summary>
public class Masters_HouseholdChores_Listening_LessonOne : Masters_Lesson {

[System.Serializable]
public class HouseholdChores_ListeningL01RoundData {
    public string choreSentence;
    public string[] optionTexts = new string[4];
    public Sprite[] optionIcons = new Sprite[4];
    public int correctOptionIndex = 0;
    public AudioClip normalAudio;
    public AudioClip slowAudio;
}

    [Header("10 Configurable Rounds")]
    [SerializeField] public HouseholdChores_ListeningL01RoundData[] rounds;

    [Header("UI Display References")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI promptTMP;

    [Header("4 Chore Option Buttons")]
    [SerializeField] private Button[] optionButtons = new Button[4];
    [SerializeField] private TextMeshProUGUI[] optionTexts = new TextMeshProUGUI[4];
    [SerializeField] private Image[] optionImages = new Image[4];

    [Header("Audio & Radio Shelf Controls")]
    [SerializeField] private Button replayAudioBtn;
    [SerializeField] private GameObject radioShelfGO;
    [SerializeField] private Toggle slowAudioToggle;
    [SerializeField] private Toggle repeatThisToggle;

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
    private bool isSlowMode = false;
    private Coroutine introRoutine;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Listening;

        AutoBindReferences();
        WireButtons();
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Listening;

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

        if (promptTMP != null && promptTMP != optionTexts[0]) {
            promptTMP.text = "\"Listen to ARIA and choose the matching chore!\"";
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
        if (titleTMP != null) titleTMP.text = "L01 Hear It — Which Chore?";
        if (subtitleTMP != null) subtitleTMP.text = "Listen carefully to ARIA and pick the matching chore!";
    }

    private void SetControlsInteractable(bool interactable) {
        for (int i = 0; i < optionButtons.Length; i++) {
            if (optionButtons[i] != null) {
                optionButtons[i].interactable = interactable;
            }
        }

        if (replayAudioBtn != null) replayAudioBtn.interactable = interactable;
        if (slowAudioToggle != null) slowAudioToggle.interactable = interactable;
        if (repeatThisToggle != null) repeatThisToggle.interactable = interactable;
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
            "AskToRepeatBtn", "CheckMeBtn", "CheckThemBtn", "AnotherWayBtn"
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

        if (promptTMP == null) {
            Transform t = transform.Find("PromptTMP") ?? transform.Find("SpeechBubble/PromptTMP") ?? transform.Find("SpeechBubbleTMP") ?? transform.Find("SpeechBubble/Text (TMP)") ?? transform.Find("SpeechBubble") ?? transform.Find("QuestionTMP");
            if (t != null) promptTMP = t.GetComponent<TextMeshProUGUI>() ?? t.GetComponentInChildren<TextMeshProUGUI>(true);
            if (promptTMP == null) {
                var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmps) {
                    string tName = tmp.name.ToLower();
                    if ((tName.Contains("speech") || tName.Contains("prompt") || tName.Contains("question")) && tmp != optionTexts[0] && tmp != optionTexts[1] && tmp != optionTexts[2] && tmp != optionTexts[3]) {
                        promptTMP = tmp;
                        break;
                    }
                }
            }
        }

        if (replayAudioBtn == null) {
            Transform t = transform.Find("ReplayBtn") ?? transform.Find("ReplayAudioBtn") ?? transform.Find("SpeakerIcon") ?? transform.Find("CommonHUD/ReplayBtn") ?? transform.Find("RadioShelf/ReplayBtn");
            if (t != null) replayAudioBtn = t.GetComponent<Button>();
        }

        if (slowAudioToggle == null) {
            Transform t = transform.Find("SlowToggle") ?? transform.Find("Slow") ?? transform.Find("CommonHUD/SlowToggle");
            if (t != null) slowAudioToggle = t.GetComponent<Toggle>();
        }

        if (repeatThisToggle == null) {
            Transform t = transform.Find("RepeatThisToggle") ?? transform.Find("RepeatThis") ?? transform.Find("CommonHUD/RepeatThisToggle");
            if (t != null) repeatThisToggle = t.GetComponent<Toggle>();
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

        if (slowAudioToggle != null) {
            slowAudioToggle.onValueChanged.RemoveAllListeners();
            slowAudioToggle.onValueChanged.AddListener(OnSlowToggleChanged);
        }

        if (repeatThisToggle != null) {
            repeatThisToggle.onValueChanged.RemoveAllListeners();
            repeatThisToggle.onValueChanged.AddListener((isOn) => {
                ReplayCurrentAudio();
            });
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
        isProcessingInput = false;
        SetControlsInteractable(true);

        HouseholdChores_ListeningL01RoundData round = rounds[currentRoundIndex];

        if (progressTMP != null) {
            progressTMP.text = $"Round {currentRoundIndex + 1} of {rounds.Length}";
        }

        if (promptTMP != null && promptTMP != optionTexts[0]) {
            promptTMP.text = "\"Listen carefully and choose the right chore!\"";
        }

        // Setup option buttons
        for (int i = 0; i < 4; i++) {
            if (optionButtons[i] == null) continue;

            bool hasOption = (round.optionTexts != null && i < round.optionTexts.Length && !string.IsNullOrEmpty(round.optionTexts[i]));
            optionButtons[i].gameObject.SetActive(hasOption);
            optionButtons[i].interactable = true;

            if (hasOption) {
                if (optionTexts[i] != null) {
                    optionTexts[i].text = round.optionTexts[i];
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

    private void PlayRoundAudio(HouseholdChores_ListeningL01RoundData round) {
        AudioClip clip = (isSlowMode && round.slowAudio != null) ? round.slowAudio : round.normalAudio;
        if (clip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(clip);
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

    public void OnSlowToggleChanged(bool slow) {
        isSlowMode = slow;
        if (!isIntroPhase) {
            ReplayCurrentAudio();
        }
    }

    public void OnOptionSelected(int optionIndex) {
        if (isProcessingInput || isIntroPhase || rounds == null || currentRoundIndex >= rounds.Length) return;

        HouseholdChores_ListeningL01RoundData round = rounds[currentRoundIndex];
        bool isCorrect = (optionIndex == round.correctOptionIndex);

        if (isCorrect) {
            isProcessingInput = true;
            score++;

            if (optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = correctColor;
            }
            if (optionButtons[optionIndex] != null) {
                optionButtons[optionIndex].transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }

            if (promptTMP != null && promptTMP != optionTexts[0]) {
                promptTMP.text = $"<color=#55FF88>\"{round.choreSentence}\"</color>";
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            StartCoroutine(AdvanceAfterDelay(1.2f));
        } else {
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

    private IEnumerator AdvanceAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);

        if (currentRoundIndex + 1 < rounds.Length) {
            LoadRound(currentRoundIndex + 1);
        } else {
            EndLesson();
        }
    }

    private void EndLesson() {
        bool passed = (score >= 8);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            if (resultScoreTMP != null) resultScoreTMP.text = $"Score: {score} / {rounds.Length}";
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "Great listening! You identified the chores!" : "Keep practicing! Tap Retry to try again.";
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
            Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Speaking);
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(Masters_Topic.Listening);
        } else {
            base.OnNextButtonClicked();
        }
    }

    protected override void OnNextButtonClicked() {
        if (isIntroPhase) {
            // If user taps Next during introduction, immediately skip intro and allow play
            if (introRoutine != null) StopCoroutine(introRoutine);
            if (Masters_AudioManager.Instance != null) Masters_AudioManager.Instance.StopVoiceOver();
            StartLesson();
            return;
        }

        base.OnNextButtonClicked();
    }

    public void PopulateDefaultRounds() {
        string aDir = "Assets/Audio/2B/3_HouseholdChores/Listening/";
        string slowDir = aDir + "Slow/";

        rounds = new HouseholdChores_ListeningL01RoundData[] {
            new HouseholdChores_ListeningL01RoundData {
                choreSentence = "Sweep the floor.",
                optionTexts = new string[] { "Sweep the floor.", "Wash up the dishes.", "Hang out the clothes.", "Wash the car." },
                correctOptionIndex = 0,
#if UNITY_EDITOR
                normalAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l01_r01.mp3"),
                slowAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l01_r01.mp3")
#endif
            },
            new HouseholdChores_ListeningL01RoundData {
                choreSentence = "Wash up the dishes.",
                optionTexts = new string[] { "Make the bed.", "Wash up the dishes.", "Take the trash out.", "Do the gardening." },
                correctOptionIndex = 1,
#if UNITY_EDITOR
                normalAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l01_r02.mp3"),
                slowAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l01_r02.mp3")
#endif
            },
            new HouseholdChores_ListeningL01RoundData {
                choreSentence = "Hang out the clothes.",
                optionTexts = new string[] { "Feed the dog.", "Set the table.", "Hang out the clothes.", "Dry the dishes." },
                correctOptionIndex = 2,
#if UNITY_EDITOR
                normalAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l01_r03.mp3"),
                slowAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l01_r03.mp3")
#endif
            },
            new HouseholdChores_ListeningL01RoundData {
                choreSentence = "Do the gardening.",
                optionTexts = new string[] { "Clean the window.", "Iron the clothes.", "Do the shopping.", "Do the gardening." },
                correctOptionIndex = 3,
#if UNITY_EDITOR
                normalAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l01_r04.mp3"),
                slowAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l01_r04.mp3")
#endif
            },
            new HouseholdChores_ListeningL01RoundData {
                choreSentence = "Wash the car.",
                optionTexts = new string[] { "Wash the car.", "Mop the floor.", "Sweep the floor.", "Feed the dog." },
                correctOptionIndex = 0,
#if UNITY_EDITOR
                normalAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l01_r05.mp3"),
                slowAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l01_r05.mp3")
#endif
            },
            new HouseholdChores_ListeningL01RoundData {
                choreSentence = "Iron the clothes.",
                optionTexts = new string[] { "Do the washing.", "Iron the clothes.", "Set the table.", "Take the trash out." },
                correctOptionIndex = 1,
#if UNITY_EDITOR
                normalAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l01_r06.mp3"),
                slowAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l01_r06.mp3")
#endif
            },
            new HouseholdChores_ListeningL01RoundData {
                choreSentence = "Make the bed.",
                optionTexts = new string[] { "Dry the dishes.", "Clean the window.", "Make the bed.", "Scrub the floor." },
                correctOptionIndex = 2,
#if UNITY_EDITOR
                normalAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l01_r07.mp3"),
                slowAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l01_r07.mp3")
#endif
            },
            new HouseholdChores_ListeningL01RoundData {
                choreSentence = "Set the table.",
                optionTexts = new string[] { "Do the gardening.", "Sweep the floor.", "Hang out the clothes.", "Set the table." },
                correctOptionIndex = 3,
#if UNITY_EDITOR
                normalAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l01_r08.mp3"),
                slowAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l01_r08.mp3")
#endif
            },
            new HouseholdChores_ListeningL01RoundData {
                choreSentence = "Take the trash out.",
                optionTexts = new string[] { "Take the trash out.", "Wash the car.", "Make the bed.", "Do the shopping." },
                correctOptionIndex = 0,
#if UNITY_EDITOR
                normalAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l01_r09.mp3"),
                slowAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l01_r09.mp3")
#endif
            },
            new HouseholdChores_ListeningL01RoundData {
                choreSentence = "Feed the dog.",
                optionTexts = new string[] { "Wash up the dishes.", "Feed the dog.", "Iron the clothes.", "Mop the floor." },
                correctOptionIndex = 1,
#if UNITY_EDITOR
                normalAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l01_r10.mp3"),
                slowAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l01_r10.mp3")
#endif
            }
        };
    }
}
