using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


/// <summary>
/// Core Controller for Unit 2 (Lets Choose) Listening Lesson Two:
/// L02 How Are You Feeling? — Hear the Story, Tap the Feeling
/// Handles introduction voice-over, 8 story audio rounds, 6 feeling option chips (from book p.12),
/// audio replay, and complete scoring flow.
/// </summary>
public class Masters_CourtesyCall_Listening_LessonTwo : Masters_Lesson {

[System.Serializable]
public class ListeningL02FeelingRound {
    public string storyPromptText;      // e.g. "I played football all day and then walked home."
    public string correctFeelingSentence; // e.g. "I'm exhausted."
    public string[] distractorFeelings;   // 5 other feeling options
    public AudioClip storyAudio;
}

    // Legacy enum retained for backwards compatibility with OldScripts
    public enum SortType {
        Asking,
        Stating
    }

    [Header("L02 8 Feeling Rounds (p.12)")]
    [SerializeField]
    private ListeningL02FeelingRound[] rounds;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI speechBubbleTMP;

    [Header("6 Feeling Option Chips")]
    [SerializeField]
    private Button[] optionButtons; // 6 Chips for feelings

    [Header("Audio Controls")]
    [SerializeField]
    private Button replayAudioBtn;

    [Header("Navigation")]
    [SerializeField]
    private Button backButton;

    [Header("Results & Retry Panel")]
    [SerializeField]
    private GameObject resultPanel;
    [SerializeField]
    private TextMeshProUGUI resultScoreTMP;
    [SerializeField]
    private TextMeshProUGUI resultStatusTMP;
    [SerializeField]
    private Button retryBtn;

    [Header("Editor Preview")]
    [Range(0, 7)]
    public int editorPreviewRound = 0;

    private int currentRoundIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;
    private bool isIntroPlaying = false;

    private Image[] optionImages;
    private Color defaultChipColor = new Color(0.14f, 0.38f, 0.58f, 1f);
    private Color correctColor = new Color(0.14f, 0.53f, 0.22f, 1f);
    private Color wrongColor = new Color(0.71f, 0.15f, 0.15f, 1f);

    protected override void Awake() {
        topic = Masters_Topic.Listening;
        base.Awake();

        PurgeLegacyChildren();
        AutoBindReferences();
        WireOptionButtons();
        WireReplayAudioButton();
        WireBackButton();

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
        AutoBindReferences();
        EnsureHeaderAndTitle();
        WireOptionButtons();
        WireReplayAudioButton();
        WireBackButton();

        if (rounds == null || rounds.Length == 0) {
            PopulateFailsafeRounds();
        }

#if UNITY_EDITOR
        if (narratorSpeech == null) {
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L02_ARIA.mp3");
        }
#endif

        StartCoroutine(StartWithIntroAudioRoutine());
    }

    private IEnumerator StartWithIntroAudioRoutine() {
        isProcessingInput = true;
        isIntroPlaying = true;

        if (progressTMP != null) progressTMP.text = "1/8";
        if (speechBubbleTMP != null) speechBubbleTMP.text = "Listen to the story line and tap the feeling sentence that fits!";

        // Temporarily disable option buttons during intro
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) optionButtons[i].interactable = false;
            }
        }

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            float delay = narratorSpeech.length > 0 ? narratorSpeech.length + 0.3f : 3.0f;
            yield return new WaitForSeconds(delay);
        } else {
            yield return new WaitForSeconds(0.5f);
        }

        isIntroPlaying = false;
        StartLesson();
    }

    public void StartLesson() {
        currentRoundIndex = 0;
        score = 0;
        isProcessingInput = false;
        isIntroPlaying = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (nextButton != null) nextButton.interactable = false;

        LoadRound(currentRoundIndex);
    }

    protected virtual void OnEnable() {
        topic = Masters_Topic.Listening;
        WireOptionButtons();
        WireReplayAudioButton();
        WireBackButton();
    }

    private void WireOptionButtons() {
        if (optionButtons == null || optionButtons.Length == 0) {
            Transform container = transform.Find("OptionsContainer") ?? transform.Find("OptionButtonContainer") ?? transform.Find("Options");
            if (container != null) {
                Button[] btns = container.GetComponentsInChildren<Button>(true);
                if (btns != null && btns.Length > 0) {
                    optionButtons = btns;
                }
            }
        }

        if (optionButtons == null || optionButtons.Length == 0) {
            List<Button> list = new List<Button>();
            Button[] allBtns = GetComponentsInChildren<Button>(true);
            foreach (var b in allBtns) {
                if (b == null) continue;
                string n = b.name.ToLower();
                if (n.Contains("optionbutton") || n.Contains("chip") || n.Contains("feeling")) {
                    list.Add(b);
                }
            }
            if (list.Count > 0) optionButtons = list.ToArray();
        }

        if (optionButtons != null && optionButtons.Length > 0) {
            optionImages = new Image[optionButtons.Length];
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    int index = i;
                    optionImages[i] = optionButtons[i].GetComponent<Image>();
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionButtonClicked(index));
                    optionButtons[i].interactable = true;
                }
            }
        }
    }

    private void WireReplayAudioButton() {
        if (replayAudioBtn == null) {
            Transform t = transform.Find("Audio/ReplayAudioButton") ?? transform.Find("ReplayAudioButton") ?? transform.Find("ReplayAudio") ?? transform.Find("Audio");
            if (t != null) replayAudioBtn = t.GetComponent<Button>() ?? t.GetComponentInChildren<Button>(true);
        }
        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
        }
    }

    private void WireBackButton() {
        if (backButton == null) {
            Transform t = transform.Find("BackButton") ?? transform.Find("HeaderContainer/BackButton");
            if (t != null) backButton = t.GetComponent<Button>();
        }
        if (backButton != null) {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackButtonClicked);
        }
    }

    private void OnValidate() {
        if (!Application.isPlaying) {
            UpdateEditorPreview();
        }
    }

    [ContextMenu("Update Editor Preview")]
    public void UpdateEditorPreview() {
        AutoBindReferences();

        if (rounds == null || rounds.Length == 0) {
            PopulateFailsafeRounds();
        }

        if (rounds == null || rounds.Length == 0) return;

        int idx = Mathf.Clamp(editorPreviewRound, 0, rounds.Length - 1);
        ListeningL02FeelingRound r = rounds[idx];

        if (headerTMP != null) headerTMP.text = "LISTENING BRANCH (Feelings Fountain)";
        if (titleTMP != null) titleTMP.text = "L02 How Are You Feeling? — Hear the Story, Tap the Feeling";
        if (progressTMP != null) progressTMP.text = $"{idx + 1}/{rounds.Length}";
        if (speechBubbleTMP != null) speechBubbleTMP.text = $"\"{r.storyPromptText}\"";
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
            }
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "LISTENING BRANCH (Feelings Fountain)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "L02 How Are You Feeling? — Hear the Story, Tap the Feeling";
        }
    }

    private void AutoBindReferences() {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> opts = new List<Button>();
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("optionbutton") || n.Contains("chip") || n.Contains("feeling")) opts.Add(btn);
            else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("repeat") || n.Contains("audio") || n.Contains("speaker"))) replayAudioBtn = btn;
            else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
            else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) nextButton = btn;
            else if (backButton == null && n.Contains("back")) backButton = btn;
        }

        if (opts.Count > 0 && (optionButtons == null || optionButtons.Length == 0)) {
            optionButtons = opts.ToArray();
        }

        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (progressTMP == null && (n.Contains("progresstext") || n.Contains("progress") || n.Contains("counter"))) progressTMP = tmp;
            else if (speechBubbleTMP == null && (n.Contains("speech") || n.Contains("prompt") || n.Contains("question") || n.Contains("dialogue"))) speechBubbleTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    private void PopulateFailsafeRounds() {
        rounds = new ListeningL02FeelingRound[] {
            new ListeningL02FeelingRound {
                storyPromptText = "I played football all day and then walked home.",
                correctFeelingSentence = "I'm exhausted.",
                distractorFeelings = new string[] { "I'm nervous.", "I'm surprised.", "I'm frightened.", "I'm lonely.", "I'm happy." },
#if UNITY_EDITOR
                storyAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L02_P1.mp3")
#endif
            },
            new ListeningL02FeelingRound {
                storyPromptText = "My exam starts in five minutes.",
                correctFeelingSentence = "I'm nervous.",
                distractorFeelings = new string[] { "I'm exhausted.", "I'm relaxed.", "I'm angry.", "I'm happy.", "I'm surprised." },
#if UNITY_EDITOR
                storyAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L02_P2.mp3")
#endif
            },
            new ListeningL02FeelingRound {
                storyPromptText = "There's a surprise party waiting for me!",
                correctFeelingSentence = "I'm surprised.",
                distractorFeelings = new string[] { "I'm frightened.", "I'm lonely.", "I'm nervous.", "I'm angry.", "I'm happy." },
#if UNITY_EDITOR
                storyAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L02_P3.mp3")
#endif
            },
            new ListeningL02FeelingRound {
                storyPromptText = "I heard a loud noise in the dark room.",
                correctFeelingSentence = "I'm frightened.",
                distractorFeelings = new string[] { "I'm surprised.", "I'm lonely.", "I'm nervous.", "I'm exhausted.", "I'm relaxed." },
#if UNITY_EDITOR
                storyAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L02_P4.mp3")
#endif
            },
            new ListeningL02FeelingRound {
                storyPromptText = "All my friends are away on holiday.",
                correctFeelingSentence = "I'm lonely.",
                distractorFeelings = new string[] { "I'm happy.", "I'm angry.", "I'm relaxed.", "I'm frightened.", "I'm nervous." },
#if UNITY_EDITOR
                storyAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L02_P5.mp3")
#endif
            },
            new ListeningL02FeelingRound {
                storyPromptText = "I won the drawing competition!",
                correctFeelingSentence = "I'm happy.",
                distractorFeelings = new string[] { "I'm lonely.", "I'm angry.", "I'm exhausted.", "I'm nervous.", "I'm surprised." },
#if UNITY_EDITOR
                storyAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L02_P6.mp3")
#endif
            },
            new ListeningL02FeelingRound {
                storyPromptText = "Someone broke my new pencil on purpose.",
                correctFeelingSentence = "I'm angry.",
                distractorFeelings = new string[] { "I'm happy.", "I'm relaxed.", "I'm lonely.", "I'm frightened.", "I'm nervous." },
#if UNITY_EDITOR
                storyAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L02_P7.mp3")
#endif
            },
            new ListeningL02FeelingRound {
                storyPromptText = "It's Sunday morning and I'm reading in the garden.",
                correctFeelingSentence = "I'm relaxed.",
                distractorFeelings = new string[] { "I'm angry.", "I'm exhausted.", "I'm nervous.", "I'm lonely.", "I'm surprised." },
#if UNITY_EDITOR
                storyAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L02_P8.mp3")
#endif
            }
        };

#if UNITY_EDITOR
        if (narratorSpeech == null) {
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L02_ARIA.mp3");
        }
#endif
    }

    private void LoadRound(int roundIndex) {
        if (rounds == null || roundIndex < 0 || roundIndex >= rounds.Length) return;

        isProcessingInput = false;
        ListeningL02FeelingRound round = rounds[roundIndex];

        if (progressTMP != null) progressTMP.text = $"{roundIndex + 1}/{rounds.Length}";
        if (speechBubbleTMP != null) speechBubbleTMP.text = $"\"{round.storyPromptText}\"";

        // Build list of 6 choices (1 correct + 5 distractors)
        List<string> choices = new List<string>();
        choices.Add(round.correctFeelingSentence);
        if (round.distractorFeelings != null) {
            foreach (var d in round.distractorFeelings) {
                if (choices.Count < 6) choices.Add(d);
            }
        }

        // Shuffle choices deterministically per round
        int targetCorrectIndex = roundIndex % 6;
        string correctVal = choices[0];

        System.Random rng = new System.Random(roundIndex + 42);
        for (int i = choices.Count - 1; i > 0; i--) {
            int k = rng.Next(i + 1);
            string tmpStr = choices[i];
            choices[i] = choices[k];
            choices[k] = tmpStr;
        }

        int actualCorrectIdx = choices.IndexOf(correctVal);
        if (actualCorrectIdx != -1 && actualCorrectIdx != targetCorrectIndex && targetCorrectIndex < choices.Count) {
            string tmpStr = choices[targetCorrectIndex];
            choices[targetCorrectIndex] = correctVal;
            choices[actualCorrectIdx] = tmpStr;
        }

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    bool hasChoice = (i < choices.Count);
                    optionButtons[i].gameObject.SetActive(hasChoice);
                    if (hasChoice) {
                        optionButtons[i].interactable = true;
                        if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                            optionImages[i].color = defaultChipColor;
                        }
                        TextMeshProUGUI tmp = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (tmp != null) {
                            tmp.text = choices[i];
                            tmp.enableAutoSizing = true;
                            tmp.fontSizeMin = 16f;
                            tmp.fontSizeMax = 24f;
                            tmp.alignment = TextAlignmentOptions.Center;
                        }
                    }
                }
            }
        }

        PlayCurrentRoundAudio();
    }

    private void PlayCurrentRoundAudio() {
        if (rounds != null && currentRoundIndex >= 0 && currentRoundIndex < rounds.Length) {
            AudioClip clip = rounds[currentRoundIndex].storyAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }
    }

    private void OnOptionButtonClicked(int optionIndex) {
        if (isProcessingInput || isIntroPlaying || rounds == null || currentRoundIndex >= rounds.Length) return;
        isProcessingInput = true;

        ListeningL02FeelingRound round = rounds[currentRoundIndex];

        string selectedText = "";
        if (optionButtons != null && optionIndex >= 0 && optionIndex < optionButtons.Length && optionButtons[optionIndex] != null) {
            TextMeshProUGUI tmp = optionButtons[optionIndex].GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) selectedText = tmp.text;
        }

        bool isCorrect = (selectedText.Equals(round.correctFeelingSentence, System.StringComparison.OrdinalIgnoreCase));

        if (isCorrect) {
            score++;
            if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = correctColor;
            }
            if (optionButtons[optionIndex] != null) {
                optionButtons[optionIndex].transform.DOPunchScale(Vector3.one * 0.15f, 0.4f);
            }
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
        } else {
            if (optionImages != null && optionIndex < optionImages.Length && optionImages[optionIndex] != null) {
                optionImages[optionIndex].color = wrongColor;
            }
            if (optionButtons[optionIndex] != null) {
                optionButtons[optionIndex].transform.DOShakePosition(0.4f, 10f);
            }
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }

        StartCoroutine(AdvanceRoundRoutine());
    }

    private IEnumerator AdvanceRoundRoutine() {
        yield return new WaitForSeconds(1.2f);

        currentRoundIndex++;
        if (currentRoundIndex < rounds.Length) {
            LoadRound(currentRoundIndex);
        } else {
            CompleteLesson();
        }
    }

    private void CompleteLesson() {
        bool passed = (score >= 6); // Pass condition: 6 of 8

        if (resultPanel != null) {
            resultPanel.SetActive(true);
            if (resultScoreTMP != null) resultScoreTMP.text = $"Score: {score}/{rounds.Length}";
            if (resultStatusTMP != null) {
                resultStatusTMP.text = passed ? "GREAT JOB! LESSON COMPLETE!" : "TRY AGAIN TO PASS!";
                resultStatusTMP.color = passed ? Color.green : Color.red;
            }
        }

        if (passed && nextButton != null) {
            nextButton.interactable = true;
            NextButtonAnimation();
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(passed ? Masters_SFX.Correct : Masters_SFX.Incorrect);
        }
    }

    private void OnReplayAudioClicked() {
        if (isIntroPlaying && narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        } else {
            PlayCurrentRoundAudio();
        }
    }

    private void OnRetryButtonClicked() {
        if (resultPanel != null) resultPanel.SetActive(false);
        StartLesson();
    }

    private void OnBackButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}
