using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Listening 2 controller for Unit 5: Over the Phone Call (Book 2A).
/// Say It Louder — Match the Heard Action to a Phrasal Verb (8 rounds).
/// ARIA voices a short action line (`expressionAudio` / `slowAudio`); phrasal-verb chips appear (`HANG ON`, `PUT THROUGH`, `SPEAK UP`, `CALL BACK`, `BREAK UP`, etc.).
/// Student listens to the line and taps the phrasal verb chip that names what's happening.
/// Upon correct selection, ARIA reads the phrasal verb + verbatim p.32 meaning (`meaningAudio`).
/// </summary>
public class Masters_OverThePhoneCall_Listening_LessonTwo : Masters_Lesson {

    public enum PhrasalVerbOption {
        HangOn = 0,
        PutThrough = 1,
        SpeakUp = 2,
        CallBack = 3,
        BreakUp = 4,
        PickUp = 5,
        CutOff = 6,
        GetThrough = 7
    }

    [System.Serializable]
    public class PhrasalVerbRoundData {
        [Tooltip("Voiced dev-team action line (normal speed)")]
        public AudioClip expressionAudio;
        [Tooltip("Voiced dev-team action line (-30% slow speed from Slow/ subdirectory)")]
        public AudioClip slowAudio;
        [Tooltip("ARIA readback of phrasal verb + verbatim p.32 meaning upon correct answer")]
        public AudioClip meaningAudio;
        [Tooltip("Action line text (for dev reference/display if enabled)")]
        public string actionLineText;
        [Tooltip("The correct phrasal verb option for this round")]
        public PhrasalVerbOption correctVerb;
        [Tooltip("The button indices (or chip labels) displayed this round")]
        public int correctButtonIndex;
    }

    [Header("Listening L2 Setup")]
    [SerializeField] private PhrasalVerbRoundData[] rounds;
    [SerializeField] private Button[] optionButtons; // Phrasal verb chips
    [SerializeField] private TextMeshProUGUI[] optionButtonLabels;
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private float animationSpeed = 0.4f;
    [SerializeField] private int passThreshold = 6;

    [Header("Audio Toggles")]
    [SerializeField] private Toggle slowToggle;
    [SerializeField] private Toggle repeatToggle;

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private int currentRoundIndex = 0;
    private int correctScore = 0;
    private bool isAnswering = false;
    private bool isSlowed = false;
    private bool isRepeatOn = false;
    private Coroutine audioCoroutine;

    protected override void Awake() {
        if (nextButton != null) {
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }
        topic = Masters_Topic.Listening;

        if (optionButtons == null || optionButtons.Length == 0) {
            AutoFindOptionButtons();
        }
    }

    protected override void Start() {
        if (Masters_AudioManager.Instance != null && narratorSpeech != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        if (slowToggle != null) {
            slowToggle.onValueChanged.AddListener(OnSlowToggleChanged);
        }
        if (repeatToggle != null) {
            repeatToggle.onValueChanged.AddListener(OnRepeatToggleChanged);
        }

        SetupOptionButtonListeners();
        currentRoundIndex = 0;
        correctScore = 0;
        UpdateProgressUI();

        StartCoroutine(InitializeLessonRoutine());
    }

    private void AutoFindOptionButtons() {
        List<Button> foundButtons = new List<Button>();
        Button[] allButtons = GetComponentsInChildren<Button>(true);
        foreach (var btn in allButtons) {
            if (btn != nextButton && (btn.name.Contains("Option") || btn.name.Contains("Chip") || btn.name.Contains("Verb"))) {
                foundButtons.Add(btn);
            }
        }
        if (foundButtons.Count > 0) {
            optionButtons = foundButtons.ToArray();
        }
    }

    private void SetupOptionButtonListeners() {
        if (optionButtons == null) return;
        for (int i = 0; i < optionButtons.Length; i++) {
            int index = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnOptionButtonClicked(index));
        }
    }

    private void OnSlowToggleChanged(bool isOn) {
        isSlowed = isOn;
    }

    private void OnRepeatToggleChanged(bool isOn) {
        isRepeatOn = isOn;
        if (isRepeatOn && !isAnswering && rounds != null && currentRoundIndex < rounds.Length) {
            PlayCurrentRoundAudio();
        }
    }

    private IEnumerator InitializeLessonRoutine() {
        SetButtonsInteractable(false);
        if (narratorSpeech != null) {
            yield return new WaitForSeconds(narratorSpeech.length + 0.5f);
        } else {
            yield return new WaitForSeconds(1.0f);
        }
        LoadRound(currentRoundIndex);
    }

    private void LoadRound(int index) {
        if (rounds == null || index >= rounds.Length) {
            CompleteLesson();
            return;
        }

        isAnswering = false;
        UpdateProgressUI();
        AnimateOptionsIn();
        SetButtonsInteractable(true);
        PlayCurrentRoundAudio();
    }

    private void PlayCurrentRoundAudio() {
        if (audioCoroutine != null) {
            StopCoroutine(audioCoroutine);
        }
        audioCoroutine = StartCoroutine(AudioPlaybackRoutine());
    }

    private IEnumerator AudioPlaybackRoutine() {
        if (rounds == null || currentRoundIndex >= rounds.Length) yield break;
        var roundData = rounds[currentRoundIndex];

        AudioClip clipToPlay = isSlowed && roundData.slowAudio != null ? roundData.slowAudio : roundData.expressionAudio;
        if (clipToPlay != null) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
                Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            }
            yield return new WaitForSeconds(clipToPlay.length);
        }

        if (isRepeatOn && !isAnswering) {
            yield return new WaitForSeconds(1.5f);
            if (!isAnswering) {
                PlayCurrentRoundAudio();
            }
        }
    }

    private void OnOptionButtonClicked(int buttonIndex) {
        if (isAnswering || rounds == null || currentRoundIndex >= rounds.Length) return;
        isAnswering = true;
        SetButtonsInteractable(false);

        if (audioCoroutine != null) {
            StopCoroutine(audioCoroutine);
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        var roundData = rounds[currentRoundIndex];
        bool isCorrect = (buttonIndex == roundData.correctButtonIndex);

        if (isCorrect) {
            correctScore++;
            UpdateProgressUI();
            AnimateButtonFeedback(optionButtons[buttonIndex], true);
            StartCoroutine(CorrectAnswerRoutine(roundData));
        } else {
            AnimateButtonFeedback(optionButtons[buttonIndex], false);
            StartCoroutine(WrongAnswerRoutine());
        }
    }

    private IEnumerator CorrectAnswerRoutine(PhrasalVerbRoundData roundData) {
        if (roundData.meaningAudio != null) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(roundData.meaningAudio);
            }
            yield return new WaitForSeconds(roundData.meaningAudio.length + 0.3f);
        } else {
            yield return new WaitForSeconds(1.2f);
        }

        AnimateOptionsOut();
        yield return new WaitForSeconds(animationSpeed);

        currentRoundIndex++;
        if (currentRoundIndex < rounds.Length) {
            LoadRound(currentRoundIndex);
        } else {
            CompleteLesson();
        }
    }

    private IEnumerator WrongAnswerRoutine() {
        yield return new WaitForSeconds(1.0f);
        isAnswering = false;
        SetButtonsInteractable(true);
        PlayCurrentRoundAudio();
    }

    private void AnimateButtonFeedback(Button btn, bool isCorrect) {
        if (btn == null) return;
        btn.transform.DOKill();
        if (isCorrect) {
            btn.transform.DOPunchScale(Vector3.one * 0.2f, 0.4f, 10, 1f);
        } else {
            btn.transform.DOShakePosition(0.5f, new Vector3(15f, 0f, 0f), 20, 90f, false, true);
        }
    }

    private void AnimateOptionsIn() {
        if (optionButtons == null) return;
        foreach (var btn in optionButtons) {
            if (btn != null) {
                btn.transform.localScale = Vector3.zero;
                btn.transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
            }
        }
    }

    private void AnimateOptionsOut() {
        if (optionButtons == null) return;
        foreach (var btn in optionButtons) {
            if (btn != null) {
                btn.transform.DOScale(Vector3.zero, animationSpeed).SetEase(Ease.InBack);
            }
        }
    }

    private void SetButtonsInteractable(bool state) {
        if (optionButtons == null) return;
        foreach (var btn in optionButtons) {
            if (btn != null) btn.interactable = state;
        }
    }

    private void UpdateProgressUI() {
        if (progressTMP != null && rounds != null) {
            progressTMP.text = $"{Mathf.Min(currentRoundIndex + 1, rounds.Length)} / {rounds.Length}";
        }
    }

    private void CompleteLesson() {
        SetButtonsInteractable(false);
        if (correctScore < passThreshold) {
            // Optional: allow retry or still unlock next per threshold
            Debug.Log($"Score {correctScore} / {passThreshold}");
        }
        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            NextButtonAnimation();
        }
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) return;
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        if (audioCoroutine != null) {
            StopCoroutine(audioCoroutine);
            audioCoroutine = null;
        }

        if (nextLessonSO != null) {
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
            }
        } else {
            if (Masters_TopicSelectionManager.Instance != null) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            if (Masters_LevelManager.Instance != null) {
                Masters_LevelManager.Instance.OnLessonComplete(topic);
            }
        }
    }

    private void OnDestroy() {
        if (optionButtons != null) {
            foreach (var btn in optionButtons) {
                if (btn != null) btn.transform.DOKill();
            }
        }
        if (nextButton != null) nextButton.transform.DOKill();
    }
}
