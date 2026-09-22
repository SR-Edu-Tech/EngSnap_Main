using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;



/// <summary>
/// Unit 3: Household Chores — Listening Lesson Two (L02 Hear It — Which Verb?).
/// 8 Rounds of Audio Cloze Verb Recognition:
/// 1. Introduction voiceover plays first with buttons locked.
/// 2. Once introduction completes, the listening game starts and allows play.
/// 3. ARIA voices a chore sentence with the verb hummed over / cloze prompt.
/// 4. 5 verb chips appear. Student taps the verb that belongs with that object.
/// 5. Correct -> ARIA reads the full verbatim sentence; Wrong -> gentle shake + retry.
/// Pass threshold: >= 6 / 8 correct.
/// </summary>
public class Masters_HouseholdChores_Listening_LessonTwo : Masters_Lesson {

[System.Serializable]
public class HouseholdChores_ListeningL02RoundData {
    public string promptClozeText;      // e.g. "_____ the bed."
    public string fullChoreSentence;    // e.g. "Make the bed."
    public string correctVerb;          // e.g. "MAKE"
    public string[] distractorVerbs;    // 4 distractors e.g. ["SWEEP", "IRON", "FEED", "WASH"]
    public AudioClip promptAudio;
    public AudioClip slowPromptAudio;
    public AudioClip ariaConfirmationAudio;
}

    [Header("8 Configurable Cloze Rounds")]
    [SerializeField] public HouseholdChores_ListeningL02RoundData[] rounds;

    [Header("UI Display References")]
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI subtitleTMP;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private TextMeshProUGUI promptTMP;

    [Header("5 Verb Option Buttons")]
    [SerializeField] private Button[] verbButtons = new Button[5];
    [SerializeField] private TextMeshProUGUI[] verbTexts = new TextMeshProUGUI[5];
    [SerializeField] private Image[] verbImages = new Image[5];

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
    private bool currentRoundRetried = false;
    private Coroutine introRoutine;

    private string[] currentRoundShuffledVerbs;
    private int currentRoundCorrectChoiceIndex = 0;

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

        if (promptTMP != null) {
            promptTMP.text = "\"Listen carefully to the chore and choose the missing verb!\"";
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
        if (titleTMP != null) titleTMP.text = "L02 Hear It — Which Verb?";
        if (subtitleTMP != null) subtitleTMP.text = "Listen to the sentence and tap the verb that completes it correctly!";
    }

    private void SetControlsInteractable(bool interactable) {
        for (int i = 0; i < verbButtons.Length; i++) {
            if (verbButtons[i] != null) {
                verbButtons[i].interactable = interactable;
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

        // 5 Verb buttons auto-binding
        List<Button> foundOptionBtns = new List<Button>();
        string[] candidateNames = new string[] {
            "OptionButton", "OptionButton (1)", "OptionButton (2)", "OptionButton (3)", "OptionButton (4)",
            "VerbBtn_1", "VerbBtn_2", "VerbBtn_3", "VerbBtn_4", "VerbBtn_5",
            "OptionBtn_1", "OptionBtn_2", "OptionBtn_3", "OptionBtn_4", "OptionBtn_5"
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

        if (foundOptionBtns.Count < 5) {
            Button[] allBtns = GetComponentsInChildren<Button>(true);
            foreach (var b in allBtns) {
                if (b == null) continue;
                string bName = b.name.ToLower();
                if (bName.Contains("back") || bName.Contains("next") || bName.Contains("replay") || bName.Contains("retry") || bName.Contains("continue") || bName.Contains("start")) continue;
                if (!foundOptionBtns.Contains(b)) {
                    foundOptionBtns.Add(b);
                    if (foundOptionBtns.Count == 5) break;
                }
            }
        }

        for (int i = 0; i < 5 && i < foundOptionBtns.Count; i++) {
            if (verbButtons[i] == null) verbButtons[i] = foundOptionBtns[i];
            if (verbButtons[i] != null) {
                if (verbTexts[i] == null) verbTexts[i] = verbButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                if (verbImages[i] == null) verbImages[i] = verbButtons[i].GetComponent<Image>();
            }
        }

        if (promptTMP == null) {
            Transform t = transform.Find("PromptTMP") ?? transform.Find("SpeechBubble/PromptTMP") ?? transform.Find("SpeechBubbleTMP") ?? transform.Find("SpeechBubble/Text (TMP)") ?? transform.Find("SpeechBubble") ?? transform.Find("QuestionTMP");
            if (t != null) promptTMP = t.GetComponent<TextMeshProUGUI>() ?? t.GetComponentInChildren<TextMeshProUGUI>(true);
            if (promptTMP == null) {
                var tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmps) {
                    string tName = tmp.name.ToLower();
                    bool isOptionText = false;
                    for (int j = 0; j < 5; j++) {
                        if (verbTexts[j] == tmp) { isOptionText = true; break; }
                    }
                    if ((tName.Contains("speech") || tName.Contains("prompt") || tName.Contains("question")) && !isOptionText) {
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
        for (int i = 0; i < 5; i++) {
            int idx = i;
            if (verbButtons[i] != null) {
                verbButtons[i].onClick.RemoveAllListeners();
                verbButtons[i].onClick.AddListener(() => OnVerbSelected(idx));
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
        currentRoundRetried = false;
        isProcessingInput = false;
        SetControlsInteractable(true);

        HouseholdChores_ListeningL02RoundData round = rounds[currentRoundIndex];

        if (progressTMP != null) {
            progressTMP.text = $"Round {currentRoundIndex + 1} of {rounds.Length}";
        }

        if (promptTMP != null) {
            promptTMP.text = $"\"{round.promptClozeText}\"";
        }

        // Build list with 1 correct + up to 4 distractors
        List<string> options = new List<string>();
        options.Add(round.correctVerb);
        if (round.distractorVerbs != null) {
            foreach (var d in round.distractorVerbs) {
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

        currentRoundShuffledVerbs = options.ToArray();
        currentRoundCorrectChoiceIndex = options.IndexOf(round.correctVerb);

        // Setup verb buttons
        for (int i = 0; i < 5; i++) {
            if (verbButtons[i] == null) continue;

            bool hasOption = (i < currentRoundShuffledVerbs.Length);
            verbButtons[i].gameObject.SetActive(hasOption);
            verbButtons[i].interactable = true;

            if (hasOption) {
                if (verbTexts[i] != null) {
                    verbTexts[i].text = currentRoundShuffledVerbs[i];
                    verbTexts[i].maxVisibleCharacters = 99999;
                    verbTexts[i].ForceMeshUpdate();
                }

                if (verbImages[i] != null) {
                    verbImages[i].color = normalBtnColor;
                }

                verbButtons[i].transform.DOKill();
                verbButtons[i].transform.localScale = Vector3.one * 0.9f;
                verbButtons[i].transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
            }
        }

        PlayRoundAudio(round);
    }

    private void PlayRoundAudio(HouseholdChores_ListeningL02RoundData round) {
        AudioClip clip = (isSlowMode && round.slowPromptAudio != null) ? round.slowPromptAudio : round.promptAudio;
        if (clip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
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

    public void OnVerbSelected(int buttonIndex) {
        if (isProcessingInput || isIntroPhase || rounds == null || currentRoundIndex >= rounds.Length) return;

        HouseholdChores_ListeningL02RoundData round = rounds[currentRoundIndex];
        bool isCorrect = (buttonIndex == currentRoundCorrectChoiceIndex);

        if (isCorrect) {
            isProcessingInput = true;
            if (!currentRoundRetried) {
                score++;
            }

            if (verbImages[buttonIndex] != null) {
                verbImages[buttonIndex].color = correctColor;
            }
            if (verbButtons[buttonIndex] != null) {
                verbButtons[buttonIndex].transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }

            if (promptTMP != null) {
                promptTMP.text = $"<color=#55FF88>\"{round.fullChoreSentence}\"</color>";
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            StartCoroutine(HandleCorrectAnswerRoutine(round));
        } else {
            currentRoundRetried = true;
            if (verbImages[buttonIndex] != null) {
                verbImages[buttonIndex].DOColor(wrongColor, 0.2f).OnComplete(() => {
                    if (verbImages[buttonIndex] != null) verbImages[buttonIndex].DOColor(normalBtnColor, 0.3f);
                });
            }
            if (verbButtons[buttonIndex] != null) {
                verbButtons[buttonIndex].transform.DOShakePosition(0.35f, 10f, 15);
            }

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }
    }

    private IEnumerator HandleCorrectAnswerRoutine(HouseholdChores_ListeningL02RoundData round) {
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
        bool passed = (score >= 6);

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            if (resultScoreTMP != null) resultScoreTMP.text = $"Score: {score} / {rounds.Length}";
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "Excellent work! You mastered the chore verbs!" : "Keep practicing! Tap Retry to try again.";
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
            // Skip introduction directly into round 1
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

        rounds = new HouseholdChores_ListeningL02RoundData[] {
            // Round 1: MAKE the bed
            new HouseholdChores_ListeningL02RoundData {
                promptClozeText = "_____ the bed.",
                fullChoreSentence = "Make the bed.",
                correctVerb = "MAKE",
                distractorVerbs = new string[] { "SWEEP", "IRON", "FEED", "WASH" },
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l02_r01_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l02_r01_prompt.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l02_r01_aria.mp3")
#endif
            },
            // Round 2: SET the table
            new HouseholdChores_ListeningL02RoundData {
                promptClozeText = "_____ the table.",
                fullChoreSentence = "Set the table.",
                correctVerb = "SET",
                distractorVerbs = new string[] { "MAKE", "CLEAN", "TAKE", "DO" },
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l02_r02_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l02_r02_prompt.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l02_r02_aria.mp3")
#endif
            },
            // Round 3: IRON the clothes
            new HouseholdChores_ListeningL02RoundData {
                promptClozeText = "_____ the clothes.",
                fullChoreSentence = "Iron the clothes.",
                correctVerb = "IRON",
                distractorVerbs = new string[] { "HANG", "FEED", "SET", "WASH" },
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l02_r03_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l02_r03_prompt.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l02_r03_aria.mp3")
#endif
            },
            // Round 4: FEED the dog
            new HouseholdChores_ListeningL02RoundData {
                promptClozeText = "_____ the dog.",
                fullChoreSentence = "Feed the dog.",
                correctVerb = "FEED",
                distractorVerbs = new string[] { "MAKE", "CLEAN", "IRON", "TAKE" },
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l02_r04_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l02_r04_prompt.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l02_r04_aria.mp3")
#endif
            },
            // Round 5: WASH the car
            new HouseholdChores_ListeningL02RoundData {
                promptClozeText = "_____ the car.",
                fullChoreSentence = "Wash the car.",
                correctVerb = "WASH",
                distractorVerbs = new string[] { "DO", "SWEEP", "SET", "FEED" },
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l02_r05_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l02_r05_prompt.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l02_r05_aria.mp3")
#endif
            },
            // Round 6: CLEAN the window
            new HouseholdChores_ListeningL02RoundData {
                promptClozeText = "_____ the window.",
                fullChoreSentence = "Clean the window.",
                correctVerb = "CLEAN",
                distractorVerbs = new string[] { "HANG", "MAKE", "IRON", "TAKE" },
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l02_r06_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l02_r06_prompt.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l02_r06_aria.mp3")
#endif
            },
            // Round 7: TAKE the trash out
            new HouseholdChores_ListeningL02RoundData {
                promptClozeText = "_____ the trash out.",
                fullChoreSentence = "Take the trash out.",
                correctVerb = "TAKE",
                distractorVerbs = new string[] { "SET", "FEED", "WASH", "CLEAN" },
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l02_r07_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l02_r07_prompt.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l02_r07_aria.mp3")
#endif
            },
            // Round 8: HANG out the clothes
            new HouseholdChores_ListeningL02RoundData {
                promptClozeText = "_____ out the clothes.",
                fullChoreSentence = "Hang out the clothes.",
                correctVerb = "HANG",
                distractorVerbs = new string[] { "SWEEP", "DO", "MAKE", "IRON" },
#if UNITY_EDITOR
                promptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l02_r08_prompt.mp3"),
                slowPromptAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowDir + "hc_l02_r08_prompt.mp3"),
                ariaConfirmationAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(aDir + "hc_l02_r08_aria.mp3")
#endif
            }
        };
    }
}
