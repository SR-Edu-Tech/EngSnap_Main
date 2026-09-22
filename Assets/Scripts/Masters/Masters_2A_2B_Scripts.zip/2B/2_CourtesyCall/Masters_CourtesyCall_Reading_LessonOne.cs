using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;


public class Masters_CourtesyCall_Reading_LessonOne : Masters_Lesson {

[System.Serializable]
public class ReadingR01SituationRound {
    public string situationText;        // e.g. "Your friend gives you a present on your birthday. [thank]"
    public string correctOption;       // e.g. "Thank you so much for the birthday gift."
    public string[] distractorOptions;  // 3 other options from p.11
    public AudioClip roundAudio;
}

    [Header("R01 12 Verbatim Situation Rounds (p.11)")]
    [SerializeField]
    private ReadingR01SituationRound[] rounds;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI situationTextTMP;

    [Header("4 Option Choice Chips")]
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
    [Range(0, 11)]
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
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R01_ARIA.mp3");
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
        ReadingR01SituationRound r = rounds[idx];

        if (headerTMP != null) headerTMP.text = "READING BRANCH (Library Noticeboard)";
        if (titleTMP != null) titleTMP.text = "R01 What Would You Say? — Courtesy in Context";
        if (progressTMP != null) progressTMP.text = $"{idx + 1}/{rounds.Length}";
        if (situationTextTMP != null) situationTextTMP.text = r.situationText;
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
    }

    private void EnsureHeaderAndTitle() {
        if (headerTMP == null) {
            Transform hTrans = transform.Find("HeaderContainer/Branch") ?? transform.Find("Header") ?? transform.Find("Branch");
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
            titleTMP.text = "R01 What Would You Say? — Courtesy in Context";
        }
    }

    private void AutoBindReferences() {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> opts = new List<Button>();
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("optionbutton") || n.Contains("chip") || n.Contains("choice")) opts.Add(btn);
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
            else if (situationTextTMP == null && (n.Contains("situation") || n.Contains("prompt") || n.Contains("question") || n.Contains("speech"))) situationTextTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    private void PopulateFailsafeRounds() {
        rounds = new ReadingR01SituationRound[] {
            new ReadingR01SituationRound {
                situationText = "Your friend gives you a present on your birthday. [thank]",
                correctOption = "Thank you so much for the birthday gift.",
                distractorOptions = new string[] { "Excuse me sir, you dropped your wallet.", "I'm sorry for being so late.", "Do you know what time it is?" },
#if UNITY_EDITOR
                roundAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R01_S1.mp3")
#endif
            },
            new ReadingR01SituationRound {
                situationText = "Uncle drops you home after the match. [thank]",
                correctOption = "Thank you so much for driving me home.",
                distractorOptions = new string[] { "I'm sorry for the mess.", "Excuse me.", "I'm really sorry, I didn't invite you to the party." },
#if UNITY_EDITOR
                roundAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R01_S2.mp3")
#endif
            },
            new ReadingR01SituationRound {
                situationText = "A classmate helped you build your science model. [thank]",
                correctOption = "Thank you so much for helping me with the science project work.",
                distractorOptions = new string[] { "I'm sorry.", "Excuse me, do you know what time it is?", "I'm sorry for being so late." },
#if UNITY_EDITOR
                roundAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R01_S3.mp3")
#endif
            },
            new ReadingR01SituationRound {
                situationText = "A friend helped you get ready for the maths test. [thank]",
                correctOption = "Thanks so much. I really appreciate you helping me out with math test.",
                distractorOptions = new string[] { "Excuse me sir, you dropped your wallet.", "I'm sorry for the mess.", "Excuse me." },
#if UNITY_EDITOR
                roundAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R01_S4.mp3")
#endif
            },
            new ReadingR01SituationRound {
                situationText = "Someone carried your bag without being asked. [thank]",
                correctOption = "I really appreciate it.",
                distractorOptions = new string[] { "I'm really sorry, I didn't invite you to the party.", "I'm sorry.", "Excuse me, do you know what time it is?" },
#if UNITY_EDITOR
                roundAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R01_S5.mp3")
#endif
            },
            new ReadingR01SituationRound {
                situationText = "You want to stop a stranger politely. [excuse me]",
                correctOption = "Excuse me.",
                distractorOptions = new string[] { "Thank you so much for the birthday gift.", "I'm sorry for being so late.", "I really appreciate it." },
#if UNITY_EDITOR
                roundAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R01_S6.mp3")
#endif
            },
            new ReadingR01SituationRound {
                situationText = "You need to know the time from a passer-by. [excuse me]",
                correctOption = "Excuse me, do you know what time it is?",
                distractorOptions = new string[] { "Thank you so much for driving me home.", "I'm sorry for the mess.", "Thanks so much." },
#if UNITY_EDITOR
                roundAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R01_S7.mp3")
#endif
            },
            new ReadingR01SituationRound {
                situationText = "A man in front of you drops his wallet. [excuse me]",
                correctOption = "Excuse me sir, you dropped your wallet.",
                distractorOptions = new string[] { "Thank you so much for helping me with the science project work.", "I'm really sorry.", "I'm sorry." },
#if UNITY_EDITOR
                roundAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R01_S8.mp3")
#endif
            },
            new ReadingR01SituationRound {
                situationText = "You reach class after the bell. [sorry]",
                correctOption = "I'm sorry for being so late.",
                distractorOptions = new string[] { "Thank you so much for the birthday gift.", "Excuse me.", "I really appreciate it." },
#if UNITY_EDITOR
                roundAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R01_S9.mp3")
#endif
            },
            new ReadingR01SituationRound {
                situationText = "Your room is untidy when a guest arrives. [sorry]",
                correctOption = "I'm sorry for the mess.",
                distractorOptions = new string[] { "Thank you so much for driving me home.", "Excuse me, do you know what time it is?", "Thanks so much." },
#if UNITY_EDITOR
                roundAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R01_S10.mp3")
#endif
            },
            new ReadingR01SituationRound {
                situationText = "You forgot to invite your friend to your party. [sorry]",
                correctOption = "I'm really sorry, I didn't invite you to the party.",
                distractorOptions = new string[] { "Thank you so much for helping me with the science project work.", "Excuse me sir, you dropped your wallet.", "I really appreciate it." },
#if UNITY_EDITOR
                roundAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R01_S11.mp3")
#endif
            },
            new ReadingR01SituationRound {
                situationText = "You bumped into someone in the corridor. [sorry]",
                correctOption = "I'm sorry.",
                distractorOptions = new string[] { "Thank you so much for the birthday gift.", "Excuse me.", "Thanks so much." },
#if UNITY_EDITOR
                roundAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R01_S12.mp3")
#endif
            }
        };

#if UNITY_EDITOR
        if (narratorSpeech == null) {
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Reading/VO_R01_ARIA.mp3");
        }
#endif
    }

    private void LoadRound(int roundIndex) {
        if (rounds == null || roundIndex < 0 || roundIndex >= rounds.Length) return;

        isProcessingInput = false;
        ReadingR01SituationRound round = rounds[roundIndex];

        if (progressTMP != null) progressTMP.text = $"{roundIndex + 1}/{rounds.Length}";
        if (situationTextTMP != null) situationTextTMP.text = round.situationText;

        // Shuffle 4 options (1 correct + 3 distractors)
        List<string> options = new List<string>();
        options.Add(round.correctOption);
        if (round.distractorOptions != null) {
            foreach (var d in round.distractorOptions) {
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
            AudioClip clip = rounds[currentRoundIndex].roundAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }
    }

    private void OnOptionButtonClicked(int optionIndex) {
        if (isProcessingInput || rounds == null || currentRoundIndex >= rounds.Length) return;
        isProcessingInput = true;

        ReadingR01SituationRound round = rounds[currentRoundIndex];

        string selectedText = "";
        if (optionButtons != null && optionIndex >= 0 && optionIndex < optionButtons.Length && optionButtons[optionIndex] != null) {
            TextMeshProUGUI tmpText = optionButtons[optionIndex].GetComponentInChildren<TextMeshProUGUI>();
            if (tmpText != null) selectedText = tmpText.text;
        }

        bool isCorrect = (selectedText.Equals(round.correctOption, System.StringComparison.OrdinalIgnoreCase));

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
        bool passed = (score >= 10); // Pass condition: 10 of 12

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
