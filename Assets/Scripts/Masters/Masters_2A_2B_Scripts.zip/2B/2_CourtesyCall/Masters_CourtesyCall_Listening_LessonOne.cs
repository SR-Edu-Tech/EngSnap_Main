using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Core Controller for Unit 2 (Lets Choose) Listening Lesson One:
/// L01 Hear It — Thank, Excuse Me or Sorry?
/// Handles 10 spoken courtesy audio rounds, 3 option category chips (THANK YOU, EXCUSE ME, SORRY),
/// audio replay, and complete scoring flow.
/// </summary>
public class Masters_CourtesyCall_Listening_LessonOne : Masters_Lesson {

[System.Serializable]
public class ListeningL01CourtesyRound {
    public string spokenPhrase;         // e.g. "Thanks a lot."
    public string correctCourtesyType;  // "THANK YOU", "EXCUSE ME", "SORRY"
    public AudioClip phraseAudio;
}

    [Header("L01 10 Verbatim Courtesy Rounds (p.11)")]
    [SerializeField]
    private ListeningL01CourtesyRound[] rounds;

    [Header("UI Display References")]
    [SerializeField]
    private TextMeshProUGUI headerTMP;
    [SerializeField]
    private TextMeshProUGUI titleTMP;
    [SerializeField]
    private TextMeshProUGUI progressTMP;
    [SerializeField]
    private TextMeshProUGUI speechBubbleTMP;

    [Header("3 Category Option Chips")]
    [SerializeField]
    private Button[] optionButtons; // 3 Chips: THANK YOU, EXCUSE ME, SORRY

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
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L01_ARIA.mp3");
        }
#endif

        StartCoroutine(StartWithIntroAudioRoutine());
    }

    protected virtual void OnEnable() {
        topic = Masters_Topic.Listening;
        WireOptionButtons();
        WireReplayAudioButton();
        WireBackButton();
    }

    private IEnumerator StartWithIntroAudioRoutine() {
        isProcessingInput = true;

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            yield return new WaitForSeconds(Mathf.Max(2.5f, narratorSpeech.length));
        } else {
            yield return new WaitForSeconds(0.3f);
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

        EnsureHeaderAndTitle();
        LoadRound(currentRoundIndex);
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
                if (n.Contains("optionbutton") || n.Contains("chip") || n.Contains("category")) {
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
        GameObject backObj = GameObject.Find("BackButton");
        if (backObj == null) {
            Transform t = transform.Find("BackButton") ?? transform.Find("HeaderContainer/BackButton");
            if (t != null) backObj = t.gameObject;
        }
        if (backObj != null) {
            Button b = backObj.GetComponent<Button>() ?? backObj.AddComponent<Button>();
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(() => {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
                    Masters_AudioManager.Instance.StopVoiceOver();
                }
                if (Masters_LevelManager.Instance != null) {
                    Masters_LevelManager.Instance.OnBackButtonClicked();
                }
            });
        }
    }

    private void PurgeLegacyChildren() {
        string[] legacyNames = new string[] {
            "QuestionStatements", "WordsOne", "FillInTheBlanks", "Statements",
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
        if (headerTMP != null && string.IsNullOrEmpty(headerTMP.text)) {
            headerTMP.text = "LISTENING BRANCH (Radio Bench)";
        }

        if (titleTMP == null) {
            Transform tTrans = transform.Find("HeaderContainer/Title") ?? transform.Find("Title") ?? transform.Find("LessonTitle");
            if (tTrans != null) titleTMP = tTrans.GetComponent<TextMeshProUGUI>();
        }
        if (titleTMP != null && string.IsNullOrEmpty(titleTMP.text)) {
            titleTMP.text = "L01 Hear It — Thank, Excuse Me or Sorry?";
        }
    }

    private void AutoBindReferences() {
        Button[] buttons = GetComponentsInChildren<Button>(true);
        List<Button> opts = new List<Button>();
        foreach (Button btn in buttons) {
            string n = btn.gameObject.name.ToLower();
            if (n.Contains("optionbutton") || n.Contains("chip") || n.Contains("category")) opts.Add(btn);
            else if (replayAudioBtn == null && (n.Contains("replay") || n.Contains("repeat") || n.Contains("audio") || n.Contains("speaker"))) replayAudioBtn = btn;
            else if (retryBtn == null && n.Contains("retry")) retryBtn = btn;
            else if (nextButton == null && (n.Contains("next") || n.Contains("continue"))) nextButton = btn;
        }

        if (opts.Count > 0 && (optionButtons == null || optionButtons.Length == 0)) {
            optionButtons = opts.ToArray();
        }

        TextMeshProUGUI[] tmps = GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (TextMeshProUGUI tmp in tmps) {
            string n = tmp.gameObject.name.ToLower();
            if (progressTMP == null && (n.Contains("progresstext") || n.Contains("progress") || n.Contains("counter"))) progressTMP = tmp;
            else if (speechBubbleTMP == null && (n.Contains("speech") || n.Contains("phrase") || n.Contains("question") || n.Contains("dialogue"))) speechBubbleTMP = tmp;
            else if (titleTMP == null && (n.Contains("lessontitle") || n.Contains("title"))) titleTMP = tmp;
            else if (headerTMP == null && (n.Contains("header") || n.Contains("branch"))) headerTMP = tmp;
        }

        if (resultPanel == null) {
            Transform rpTrans = transform.Find("ResultPanel") ?? transform.Find("ResultsPanel");
            if (rpTrans != null) resultPanel = rpTrans.gameObject;
        }
    }

    private void PopulateFailsafeRounds() {
        rounds = new ListeningL01CourtesyRound[] {
            new ListeningL01CourtesyRound { 
                spokenPhrase = "Thanks a lot.", 
                correctCourtesyType = "THANK YOU",
#if UNITY_EDITOR
                phraseAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L01_P1.mp3")
#endif
            },
            new ListeningL01CourtesyRound { 
                spokenPhrase = "Thank you so much for driving me home.", 
                correctCourtesyType = "THANK YOU",
#if UNITY_EDITOR
                phraseAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L01_P2.mp3")
#endif
            },
            new ListeningL01CourtesyRound { 
                spokenPhrase = "I really appreciate your help.", 
                correctCourtesyType = "THANK YOU",
#if UNITY_EDITOR
                phraseAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L01_P3.mp3")
#endif
            },
            new ListeningL01CourtesyRound { 
                spokenPhrase = "Thanks so much. I really appreciate you helping me out with math test.", 
                correctCourtesyType = "THANK YOU",
#if UNITY_EDITOR
                phraseAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L01_P4.mp3")
#endif
            },
            new ListeningL01CourtesyRound { 
                spokenPhrase = "Excuse me.", 
                correctCourtesyType = "EXCUSE ME",
#if UNITY_EDITOR
                phraseAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L01_P5.mp3")
#endif
            },
            new ListeningL01CourtesyRound { 
                spokenPhrase = "Excuse me, do you know what time it is?", 
                correctCourtesyType = "EXCUSE ME",
#if UNITY_EDITOR
                phraseAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L01_P6.mp3")
#endif
            },
            new ListeningL01CourtesyRound { 
                spokenPhrase = "Excuse me sir, you dropped your wallet.", 
                correctCourtesyType = "EXCUSE ME",
#if UNITY_EDITOR
                phraseAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L01_P7.mp3")
#endif
            },
            new ListeningL01CourtesyRound { 
                spokenPhrase = "I'm sorry for being so late.", 
                correctCourtesyType = "SORRY",
#if UNITY_EDITOR
                phraseAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L01_P8.mp3")
#endif
            },
            new ListeningL01CourtesyRound { 
                spokenPhrase = "I'm sorry for the mess.", 
                correctCourtesyType = "SORRY",
#if UNITY_EDITOR
                phraseAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L01_P9.mp3")
#endif
            },
            new ListeningL01CourtesyRound { 
                spokenPhrase = "I'm really sorry, I didn't invite you to the party.", 
                correctCourtesyType = "SORRY",
#if UNITY_EDITOR
                phraseAudio = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L01_P10.mp3")
#endif
            }
        };

#if UNITY_EDITOR
        if (narratorSpeech == null) {
            narratorSpeech = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/2_CourtesyCall/Listening/VO_L01_ARIA.mp3");
        }
#endif
    }

    private void LoadRound(int roundIndex) {
        if (rounds == null || roundIndex < 0 || roundIndex >= rounds.Length) return;

        isProcessingInput = false;
        ListeningL01CourtesyRound round = rounds[roundIndex];

        if (progressTMP != null) progressTMP.text = $"{roundIndex + 1}/{rounds.Length}";
        if (speechBubbleTMP != null) speechBubbleTMP.text = $"\"{round.spokenPhrase}\"";

        string[] categoryNames = new string[] { "THANK YOU", "EXCUSE ME", "SORRY" };
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    optionButtons[i].interactable = true;
                    if (optionImages != null && i < optionImages.Length && optionImages[i] != null) {
                        optionImages[i].color = defaultChipColor;
                    }
                    TextMeshProUGUI tmp = optionButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                    if (tmp != null && i < categoryNames.Length) {
                        tmp.text = categoryNames[i];
                    }
                }
            }
        }

        PlayCurrentRoundAudio();
    }

    private void PlayCurrentRoundAudio() {
        if (rounds != null && currentRoundIndex >= 0 && currentRoundIndex < rounds.Length) {
            AudioClip clip = rounds[currentRoundIndex].phraseAudio;
            if (clip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
        }
    }

    private void OnOptionButtonClicked(int optionIndex) {
        if (isProcessingInput || rounds == null || currentRoundIndex >= rounds.Length) return;
        isProcessingInput = true;

        string[] categoryNames = new string[] { "THANK YOU", "EXCUSE ME", "SORRY" };
        if (optionIndex < 0 || optionIndex >= categoryNames.Length) return;

        string selectedCategory = categoryNames[optionIndex];
        ListeningL01CourtesyRound round = rounds[currentRoundIndex];
        bool isCorrect = (selectedCategory.Equals(round.correctCourtesyType, System.StringComparison.OrdinalIgnoreCase));

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
        StartLesson();
    }

    protected override void OnNextButtonClicked() {
        topic = Masters_Topic.Listening;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
