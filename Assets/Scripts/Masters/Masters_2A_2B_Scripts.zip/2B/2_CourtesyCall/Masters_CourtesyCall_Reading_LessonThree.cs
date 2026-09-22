using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class Masters_CourtesyCall_Reading_LessonThree : Masters_Lesson {

[System.Serializable]
public class ReadingR03ClueRound {
    public string clueText;             // e.g. "You have worked so hard that you cannot do any more."
    public string correctFeeling;       // e.g. "I'm exhausted."
    public string[] distractorFeelings; // 3 other feeling options from p.12
    public AudioClip clueAudio;
}

    [Header("R03 10 Verbatim Clue Rounds (p.12)")]
    [SerializeField]
    private ReadingR03ClueRound[] rounds;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI clueTextTMP;

    [Header("4 Feeling Option Chips")]
    [SerializeField]
    private Button[] optionButtons; // 4 Chips

    [Header("Audio Controls")]
    [SerializeField]
    private Button replayAudioBtn;

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
    [Range(0, 9)]
    public int editorPreviewRound = 0;

    private int currentRoundIndex = 0;
    private int score = 0;
    private bool isProcessingInput = false;

    private Image[] optionImages;
    private Color defaultChipColor = new Color(0.14f, 0.38f, 0.58f, 1f);
    private Color correctColor = new Color(0.14f, 0.53f, 0.22f, 1f);
    private Color wrongColor = new Color(0.71f, 0.15f, 0.15f, 1f);

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;

        PurgeLegacyChildren();
        AutoBindReferences();

        if (optionButtons != null && optionButtons.Length > 0) {
            optionImages = new Image[optionButtons.Length];
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    int index = i;
                    optionImages[i] = optionButtons[i].GetComponent<Image>();
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionButtonClicked(index));
                }
            }
        }

        if (replayAudioBtn != null) {
            replayAudioBtn.onClick.RemoveAllListeners();
            replayAudioBtn.onClick.AddListener(OnReplayAudioClicked);
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
        topic = Masters_Topic.Reading;

        PurgeLegacyChildren();
        AutoBindReferences();
        EnsureHeaderAndTitle();

        if (rounds == null || rounds.Length == 0) {
            PopulateFailsafeRounds();
        }

#if UNITY_EDITOR
        if (narratorSpeech == null) {
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R03_ARIA.mp3");
        }
#endif

        StartCoroutine(StartWithIntroAudioRoutine());
    }

    private IEnumerator StartWithIntroAudioRoutine() {
        isProcessingInput = true;

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) optionButtons[i].interactable = false;
            }
        }

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            yield return new WaitForSeconds(Mathf.Max(2.5f, narratorSpeech.length));
        } else {
            yield return new WaitForSeconds(0.5f);
        }

        StartLesson();
    }

    public void StartLesson() {
        currentRoundIndex = 0;
        score = 0;
        isProcessingInput = false;

        if (resultPanel != null) {
            resultPanel.SetActive(false);
        }

        LoadRound(currentRoundIndex);
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
        ReadingR03ClueRound r = rounds[idx];

        if (headerTMP != null) headerTMP.text = "READING BRANCH (Feelings Fountain)";
        if (titleTMP != null) titleTMP.text = "R03 Name That Feeling";
        if (progressTMP != null) progressTMP.text = $"{idx + 1}/{rounds.Length}";
        if (clueTextTMP != null) clueTextTMP.text = r.clueText;
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements", "Words",
            "FillInTheBlanksGameObjectPrefab", "FillInTheBlanksGameObjectPosition", "OptionButtonContainer",
            "DialoguePanel", "QuizPanel"
        };

        foreach (string lName in legacyNames) {
            Transform lTrans = transform.Find(lName);
            if (lTrans != null) {
                lTrans.gameObject.SetActive(false);
                Destroy(lTrans.gameObject);
            }
        }
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch");
            if (hTrans != null) headerTMP = hTrans.GetComponent<TextMeshProUGUI>();
        }
        if (headerTMP != null) {
            headerTMP.text = "READING BRANCH (Feelings Fountain)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null) {
            titleTMP.text = "R03 Name That Feeling";
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
        }

        if (opts.Count >= 4 && (optionButtons == null || optionButtons.Length == 0)) {
            optionButtons = opts.ToArray();
        }

        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (progressTMP == null && (n.Contains("progresstext") || n.Contains("progress") || n.Contains("counter"))) progressTMP = tmp;
            else if (clueTextTMP == null && (n.Contains("clue") || n.Contains("prompt") || n.Contains("question") || n.Contains("speech") || n.Contains("text"))) clueTextTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    private void PopulateFailsafeRounds() {
        rounds = new ReadingR03ClueRound[] {
            new ReadingR03ClueRound {
                clueText = "You have worked so hard that you cannot do any more.",
                correctFeeling = "I'm exhausted.",
                distractorFeelings = new string[] { "I'm angry.", "I'm happy.", "I'm nervous." },
#if UNITY_EDITOR
                clueAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R03_C1.mp3")
#endif
            },
            new ReadingR03ClueRound {
                clueText = "Someone spoiled your work on purpose.",
                correctFeeling = "I'm angry.",
                distractorFeelings = new string[] { "I'm sad.", "I'm surprised.", "I'm lonely." },
#if UNITY_EDITOR
                clueAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R03_C2.mp3")
#endif
            },
            new ReadingR03ClueRound {
                clueText = "You got the best news of the week.",
                correctFeeling = "I'm happy.",
                distractorFeelings = new string[] { "I'm relaxed.", "I'm shy.", "I'm frightened." },
#if UNITY_EDITOR
                clueAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R03_C3.mp3")
#endif
            },
            new ReadingR03ClueRound {
                clueText = "Your best friend is moving to another city.",
                correctFeeling = "I'm sad.",
                distractorFeelings = new string[] { "I'm angry.", "I'm nervous.", "I'm lonely." },
#if UNITY_EDITOR
                clueAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R03_C4.mp3")
#endif
            },
            new ReadingR03ClueRound {
                clueText = "A quiet Sunday with nothing to worry about.",
                correctFeeling = "I'm relaxed.",
                distractorFeelings = new string[] { "I'm exhausted.", "I'm happy.", "I'm surprised." },
#if UNITY_EDITOR
                clueAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R03_C5.mp3")
#endif
            },
            new ReadingR03ClueRound {
                clueText = "Your turn to speak on stage is next.",
                correctFeeling = "I'm nervous.",
                distractorFeelings = new string[] { "I'm shy.", "I'm frightened.", "I'm angry." },
#if UNITY_EDITOR
                clueAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R03_C6.mp3")
#endif
            },
            new ReadingR03ClueRound {
                clueText = "Something unexpected just happened!",
                correctFeeling = "I'm surprised.",
                distractorFeelings = new string[] { "I'm happy.", "I'm relaxed.", "I'm nervous." },
#if UNITY_EDITOR
                clueAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R03_C7.mp3")
#endif
            },
            new ReadingR03ClueRound {
                clueText = "There is a strange noise in a dark room.",
                correctFeeling = "I'm frightened.",
                distractorFeelings = new string[] { "I'm shy.", "I'm lonely.", "I'm nervous." },
#if UNITY_EDITOR
                clueAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R03_C8.mp3")
#endif
            },
            new ReadingR03ClueRound {
                clueText = "Nobody is around to play with you.",
                correctFeeling = "I'm lonely.",
                distractorFeelings = new string[] { "I'm sad.", "I'm exhausted.", "I'm shy." },
#if UNITY_EDITOR
                clueAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R03_C9.mp3")
#endif
            },
            new ReadingR03ClueRound {
                clueText = "You don't like speaking in front of new people.",
                correctFeeling = "I'm shy.",
                distractorFeelings = new string[] { "I'm nervous.", "I'm frightened.", "I'm lonely." },
#if UNITY_EDITOR
                clueAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R03_C10.mp3")
#endif
            }
        };

#if UNITY_EDITOR
        if (narratorSpeech == null) {
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R03_ARIA.mp3");
        }
#endif
    }

    private void LoadRound(int roundIndex) {
        if (rounds == null || roundIndex < 0 || roundIndex >= rounds.Length) return;

        isProcessingInput = false;
        ReadingR03ClueRound round = rounds[roundIndex];

        if (progressTMP != null) progressTMP.text = $"{roundIndex + 1}/{rounds.Length}";
        if (clueTextTMP != null) clueTextTMP.text = round.clueText;

        // Shuffle 4 options (1 correct + 3 distractors)
        List<string> options = new List<string>();
        options.Add(round.correctFeeling);
        if (round.distractorFeelings != null) {
            foreach (var d in round.distractorFeelings) {
                if (options.Count < 4) options.Add(d);
            }
        }

        // Shuffle
        for (int i = options.Count - 1; i > 0; i--) {
            int r = Random.Range(0, i + 1);
            string tmp = options[i];
            options[i] = options[r];
            options[r] = tmp;
        }

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    bool hasOpt = (i < options.Count);
                    optionButtons[i].gameObject.SetActive(hasOpt);
                    if (hasOpt) {
                        optionButtons[i].interactable = true;
                        if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                            optionImages[i].color = defaultChipColor;
                        }
                        TextMeshProUGUI tmpText = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                        if (tmpText != null) {
                            tmpText.text = options[i];
                        }
                    }
                }
            }
        }

        PlayCurrentRoundAudio();
    }

    private void PlayCurrentRoundAudio() {
        if (rounds != null && currentRoundIndex >= 0 && currentRoundIndex < rounds.Length) {
            AudioClip clip = rounds[currentRoundIndex].clueAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }
    }

    private void OnOptionButtonClicked(int optionIndex) {
        if (isProcessingInput || rounds == null || currentRoundIndex >= rounds.Length) return;
        isProcessingInput = true;

        ReadingR03ClueRound round = rounds[currentRoundIndex];

        string selectedText = "";
        if (optionButtons != null && optionIndex >= 0 && optionIndex < optionButtons.Length && optionButtons[optionIndex] != null) {
            TextMeshProUGUI tmpText = optionButtons[optionIndex].GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null) selectedText = tmpText.text;
        }

        bool isCorrect = (selectedText.Equals(round.correctFeeling, System.StringComparison.OrdinalIgnoreCase));

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
        bool passed = (score >= 8); // Pass condition: 8 of 10

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
        PlayCurrentRoundAudio();
    }

    private void OnRetryButtonClicked() {
        if (resultPanel != null) resultPanel.SetActive(false);
        currentRoundIndex = 0;
        score = 0;
        LoadRound(currentRoundIndex);
    }

    protected override void OnNextButtonClicked() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}
