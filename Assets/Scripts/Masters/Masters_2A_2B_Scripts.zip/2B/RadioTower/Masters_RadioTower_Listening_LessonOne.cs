using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class Masters_RadioTower_Listening_LessonOne : Masters_Lesson {

    public enum FunctionCategory {
        AskToRepeat,
        CheckThem,
        CheckMe,
        AnotherWay
    }

    [System.Serializable]
    public class ListeningRoundData {
        public AudioClip audioClip;
        public AudioClip slowedAudioClip;
        public string spokenPhrase;
        public FunctionCategory correctCategory;
        public AudioClip ariaPraiseAudio;
    }

    [Header("Radio Tower Listening L01 Configuration")]
    [SerializeField] private ListeningRoundData[] rounds;
    [SerializeField] private TextMeshProUGUI headerTMP;
    [SerializeField] private TextMeshProUGUI titleTMP;
    [SerializeField] private TextMeshProUGUI roundProgressTMP;
    [SerializeField] private TextMeshProUGUI spokenPhraseBubbleTMP;
    [SerializeField] private GameObject speechBubblePanel;

    [Header("4 Function Category Chips")]
    [SerializeField] private Button askToRepeatChip;
    [SerializeField] private Button checkThemChip;
    [SerializeField] private Button checkMeChip;
    [SerializeField] private Button anotherWayChip;

    [Header("Control Buttons & Toggles")]
    [SerializeField] private Button replayButton;
    [SerializeField] private Toggle slowToggle;
    [SerializeField] private GameObject ariaOwlObject;

    [Header("Results Panel")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultScoreTMP;
    [SerializeField] private TextMeshProUGUI resultAccuracyTMP;
    [SerializeField] private TextMeshProUGUI resultMessageTMP;
    [SerializeField] private Button resultContinueButton;
    [SerializeField] private Button resultRetryButton;

    [Header("Next Lesson Integration")]
    [SerializeField] protected Masters_LessonSO nextLessonSO;

    private int currentRoundIndex = 0;
    private int correctFirstAttemptCount = 0;
    private bool isFirstAttemptForCurrentRound = true;
    private bool isSlowed = false;
    private bool isProcessingAnswer = false;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Listening;

        AutoFindUIReferences();
        EnsureDefaultRoundsData();
        BindChipListeners();

        if (replayButton != null) {
            replayButton.onClick.RemoveAllListeners();
            replayButton.onClick.AddListener(PlayCurrentRoundAudio);
        }

        if (slowToggle != null) {
            slowToggle.onValueChanged.RemoveAllListeners();
            slowToggle.onValueChanged.AddListener(OnSlowToggleChanged);
        }

        if (resultContinueButton != null) {
            resultContinueButton.onClick.RemoveAllListeners();
            resultContinueButton.onClick.AddListener(OnContinueButtonClicked);
        }

        if (resultRetryButton != null) {
            resultRetryButton.onClick.RemoveAllListeners();
            resultRetryButton.onClick.AddListener(RestartActivity);
        }
    }

    private void OnEnable() {
        if (Application.isPlaying) {
            RestartActivity();
        }
    }

    protected override void Start() {
        base.Start();
        topic = Masters_Topic.Listening;
        if (Application.isPlaying) {
            StartCoroutine(InitializeRadioTowerRoutine());
        }
    }

    private IEnumerator InitializeRadioTowerRoutine() {
        if (resultPanel != null) resultPanel.SetActive(false);

        UpdateHeaderAndTitle();

        // 1. Play Introduction Audio first
        if (narratorSpeech == null) {
#if UNITY_EDITOR
            narratorSpeech = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/RadioTower/Listening/Intro.mp3");
#endif
        }

        if (narratorSpeech != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        }

        // 2. Start Round 1
        LoadRound(0);
    }

    private void UpdateHeaderAndTitle() {
        if (headerTMP != null) headerTMP.text = "LISTENING BRANCH (Radio Tower)";
        if (titleTMP != null) titleTMP.text = "L01 Hear It — What Is the Speaker Doing?";
    }

    private void BindChipListeners() {
        if (askToRepeatChip != null) {
            askToRepeatChip.onClick.RemoveAllListeners();
            askToRepeatChip.onClick.AddListener(() => OnCategoryChipClicked(FunctionCategory.AskToRepeat, askToRepeatChip));
        }
        if (checkThemChip != null) {
            checkThemChip.onClick.RemoveAllListeners();
            checkThemChip.onClick.AddListener(() => OnCategoryChipClicked(FunctionCategory.CheckThem, checkThemChip));
        }
        if (checkMeChip != null) {
            checkMeChip.onClick.RemoveAllListeners();
            checkMeChip.onClick.AddListener(() => OnCategoryChipClicked(FunctionCategory.CheckMe, checkMeChip));
        }
        if (anotherWayChip != null) {
            anotherWayChip.onClick.RemoveAllListeners();
            anotherWayChip.onClick.AddListener(() => OnCategoryChipClicked(FunctionCategory.AnotherWay, anotherWayChip));
        }
    }

    public void LoadRound(int roundIndex) {
        if (rounds == null || rounds.Length == 0) return;

        currentRoundIndex = roundIndex;
        isFirstAttemptForCurrentRound = true;
        isProcessingAnswer = false;

        if (roundProgressTMP != null) {
            roundProgressTMP.text = $"ROUND {currentRoundIndex + 1} / {rounds.Length}";
        }

        ListeningRoundData data = rounds[currentRoundIndex];

        if (spokenPhraseBubbleTMP != null) {
            spokenPhraseBubbleTMP.text = $"\"{data.spokenPhrase}\"";
        }
        if (speechBubblePanel != null) {
            speechBubblePanel.SetActive(true);
            speechBubblePanel.transform.DOKill();
            speechBubblePanel.transform.localScale = Vector3.zero;
            speechBubblePanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        ResetChipsState();
        PlayCurrentRoundAudio();
    }

    private void ResetChipsState() {
        ResetChipVisual(askToRepeatChip);
        ResetChipVisual(checkThemChip);
        ResetChipVisual(checkMeChip);
        ResetChipVisual(anotherWayChip);
    }

    private void ResetChipVisual(Button chip) {
        if (chip == null) return;
        chip.interactable = true;
        chip.transform.DOKill();
        chip.transform.localScale = Vector3.one;

        Image img = chip.GetComponent<Image>();
        if (img != null) {
            img.color = Color.white;
        }
    }

    public void PlayCurrentRoundAudio() {
        if (rounds == null || currentRoundIndex >= rounds.Length) return;

        ListeningRoundData data = rounds[currentRoundIndex];
        AudioClip clipToPlay = isSlowed ? (data.slowedAudioClip != null ? data.slowedAudioClip : data.audioClip) : data.audioClip;

        if (clipToPlay == null) return;

        if (ariaOwlObject != null) {
            ariaOwlObject.SetActive(true);
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(() => {
                if (ariaOwlObject != null) {
                    ariaOwlObject.SetActive(false);
                }
            }));
        }
    }

    private void OnCategoryChipClicked(FunctionCategory category, Button clickedChip) {
        if (isProcessingAnswer || rounds == null || currentRoundIndex >= rounds.Length) return;

        ListeningRoundData data = rounds[currentRoundIndex];

        if (category == data.correctCategory) {
            isProcessingAnswer = true;
            if (isFirstAttemptForCurrentRound) {
                correctFirstAttemptCount++;
            }

            // Green glow + success scale animation
            Image img = clickedChip.GetComponent<Image>();
            if (img != null) img.color = new Color(0.2f, 0.85f, 0.3f, 1f);

            clickedChip.transform.DOKill();
            clickedChip.transform.DOPunchScale(Vector3.one * 0.2f, 0.4f, 6, 0.5f);

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                if (data.ariaPraiseAudio != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(data.ariaPraiseAudio);
                }
            }

            StartCoroutine(AdvanceAfterDelayCoroutine());
        } else {
            isFirstAttemptForCurrentRound = false;

            // Red shake animation
            Image img = clickedChip.GetComponent<Image>();
            if (img != null) img.color = new Color(0.95f, 0.4f, 0.4f, 1f);

            clickedChip.transform.DOKill();
            clickedChip.transform.DOShakePosition(0.4f, 10f, 15, 90f);

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }

            if (replayButton != null) {
                replayButton.transform.DOKill();
                replayButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }
        }
    }

    private IEnumerator AdvanceAfterDelayCoroutine() {
        yield return new WaitForSeconds(1.2f);
        if (currentRoundIndex + 1 < rounds.Length) {
            LoadRound(currentRoundIndex + 1);
        } else {
            ShowResults();
        }
    }

    private void ShowResults() {
        if (resultPanel == null) return;

        resultPanel.SetActive(true);
        int total = rounds != null ? rounds.Length : 10;
        int pct = Mathf.RoundToInt(((float)correctFirstAttemptCount / total) * 100f);
        bool passed = correctFirstAttemptCount >= 8;

        if (resultScoreTMP != null) resultScoreTMP.text = $"SCORE: {correctFirstAttemptCount} / {total}";
        if (resultAccuracyTMP != null) resultAccuracyTMP.text = $"ACCURACY: {pct}%";
        if (resultMessageTMP != null) {
            resultMessageTMP.text = passed ? "GREAT JOB! LESSON COMPLETED!" : "KEEP PRACTICING! TRY AGAIN TO REACH 8/10.";
            resultMessageTMP.color = passed ? new Color(0.1f, 0.75f, 0.2f) : new Color(0.9f, 0.25f, 0.2f);
        }

        if (resultContinueButton != null) resultContinueButton.gameObject.SetActive(passed);
        if (resultRetryButton != null) resultRetryButton.gameObject.SetActive(!passed);
    }

    private void OnSlowToggleChanged(bool val) {
        isSlowed = val;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
        PlayCurrentRoundAudio();
    }

    public void RestartActivity() {
        currentRoundIndex = 0;
        correctFirstAttemptCount = 0;
        isProcessingAnswer = false;

        if (resultPanel != null) resultPanel.SetActive(false);
        if (Application.isPlaying) {
            StartCoroutine(InitializeRadioTowerRoutine());
        }
    }

    protected override void OnNextButtonClicked() {
        OnContinueButtonClicked();
    }

    private void OnContinueButtonClicked() {
        topic = Masters_Topic.Listening;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        if (nextLessonSO == null) {
#if UNITY_EDITOR
            nextLessonSO = AssetDatabase.LoadAssetAtPath<Masters_LessonSO>("Assets/ScriptableObjects/2B/RadioTower/Listening/RadioTower_Listening_LessonTwo.asset");
#endif
        }

        if (nextLessonSO != null && Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            return;
        }

        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton(Masters_Topic.Reading);
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }

    private void AutoFindUIReferences() {
        if (headerTMP == null) headerTMP = transform.Find("Header/TMP")?.GetComponent<TextMeshProUGUI>();
        if (titleTMP == null) titleTMP = transform.Find("Title/TMP")?.GetComponent<TextMeshProUGUI>();
        if (roundProgressTMP == null) roundProgressTMP = transform.Find("RoundProgressTMP")?.GetComponent<TextMeshProUGUI>();
        if (spokenPhraseBubbleTMP == null) spokenPhraseBubbleTMP = transform.Find("SpeechBubble/Text")?.GetComponent<TextMeshProUGUI>();
        if (speechBubblePanel == null) speechBubblePanel = transform.Find("SpeechBubble")?.gameObject;

        Transform chipsTrans = transform.Find("FunctionChipsGrid") ?? transform.Find("Chips");
        if (chipsTrans != null) {
            if (askToRepeatChip == null) askToRepeatChip = chipsTrans.Find("AskToRepeatChip")?.GetComponent<Button>() ?? chipsTrans.GetChild(0)?.GetComponent<Button>();
            if (checkThemChip == null) checkThemChip = chipsTrans.Find("CheckThemChip")?.GetComponent<Button>() ?? (chipsTrans.childCount > 1 ? chipsTrans.GetChild(1)?.GetComponent<Button>() : null);
            if (checkMeChip == null) checkMeChip = chipsTrans.Find("CheckMeChip")?.GetComponent<Button>() ?? (chipsTrans.childCount > 2 ? chipsTrans.GetChild(2)?.GetComponent<Button>() : null);
            if (anotherWayChip == null) anotherWayChip = chipsTrans.Find("AnotherWayChip")?.GetComponent<Button>() ?? (chipsTrans.childCount > 3 ? chipsTrans.GetChild(3)?.GetComponent<Button>() : null);
        }

        if (replayButton == null) replayButton = transform.Find("ReplayButton")?.GetComponent<Button>();
        if (slowToggle == null) slowToggle = transform.Find("SlowToggle")?.GetComponent<Toggle>();
        if (ariaOwlObject == null) ariaOwlObject = transform.Find("AriaOwl")?.gameObject;
        if (resultPanel == null) resultPanel = transform.Find("ResultPanel")?.gameObject;
    }

    private void EnsureDefaultRoundsData() {
        if (narratorSpeech == null) {
#if UNITY_EDITOR
            narratorSpeech = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/2B/RadioTower/Listening/Intro.mp3");
#endif
        }

        if (rounds == null || rounds.Length == 0) {
#if UNITY_EDITOR
            string baseAudio = "Assets/Audio/2B/RadioTower/Listening/";
            rounds = new ListeningRoundData[] {
                new ListeningRoundData {
                    spokenPhrase = "Would you say that again, please?",
                    correctCategory = FunctionCategory.AskToRepeat,
                    audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "R01_Normal.mp3"),
                    slowedAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "Slow/R01_Slow.mp3")
                },
                new ListeningRoundData {
                    spokenPhrase = "Can you speak louder, please?",
                    correctCategory = FunctionCategory.AskToRepeat,
                    audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "R02_Normal.mp3"),
                    slowedAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "Slow/R02_Slow.mp3")
                },
                new ListeningRoundData {
                    spokenPhrase = "What does the word mean?",
                    correctCategory = FunctionCategory.AskToRepeat,
                    audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "R03_Normal.mp3"),
                    slowedAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "Slow/R03_Slow.mp3")
                },
                new ListeningRoundData {
                    spokenPhrase = "I am sorry, I did not hear what you said.",
                    correctCategory = FunctionCategory.AskToRepeat,
                    audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "R04_Normal.mp3"),
                    slowedAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "Slow/R04_Slow.mp3")
                },
                new ListeningRoundData {
                    spokenPhrase = "Do you know what I mean?",
                    correctCategory = FunctionCategory.CheckThem,
                    audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "R05_Normal.mp3"),
                    slowedAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "Slow/R05_Slow.mp3")
                },
                new ListeningRoundData {
                    spokenPhrase = "Do I make myself clear?",
                    correctCategory = FunctionCategory.CheckThem,
                    audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "R06_Normal.mp3"),
                    slowedAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "Slow/R06_Slow.mp3")
                },
                new ListeningRoundData {
                    spokenPhrase = "Got the message?",
                    correctCategory = FunctionCategory.CheckThem,
                    audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "R07_Normal.mp3"),
                    slowedAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "Slow/R07_Slow.mp3")
                },
                new ListeningRoundData {
                    spokenPhrase = "Do you mean...?",
                    correctCategory = FunctionCategory.CheckMe,
                    audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "R08_Normal.mp3"),
                    slowedAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "Slow/R08_Slow.mp3")
                },
                new ListeningRoundData {
                    spokenPhrase = "Does that mean...?",
                    correctCategory = FunctionCategory.CheckMe,
                    audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "R09_Normal.mp3"),
                    slowedAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "Slow/R09_Slow.mp3")
                },
                new ListeningRoundData {
                    spokenPhrase = "What I am trying to say is...",
                    correctCategory = FunctionCategory.AnotherWay,
                    audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "R10_Normal.mp3"),
                    slowedAudioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(baseAudio + "Slow/R10_Slow.mp3")
                }
            };
#endif
        }
    }
}
