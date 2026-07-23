using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum Masters_Unit3_DirectionKind {
    ASK = 0,
    MOVEMENT = 1,
    POSITION = 2
}

/// <summary>
/// Core Listening 1 controller for Unit 3: Beyond the Horizon (Book 2A).
/// Adapted to user spec: 3 option chips corresponding to ASK, MOVEMENT, and POSITION.
/// </summary>
public class Masters_BeyondTheHorizon_Listening_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class QuestionData {
        public string phraseText;
        public Masters_Unit3_DirectionKind correctKind;
        public AudioClip phraseAudio;
        public AudioClip slowAudio;
    }

    [Header("Listening L1 Setup")]
    [SerializeField] private QuestionData[] questions;
    [SerializeField] private Button[] optionButtons; // Index 0: ASK, Index 1: MOVEMENT, Index 2: POSITION
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private float animationSpeed = 0.4f;
#pragma warning disable 0414
    [SerializeField] private int passThreshold = 8;
#pragma warning restore 0414

    [Header("Audio Toggles")]
    [SerializeField] private Toggle slowToggle;
    [SerializeField] private Toggle repeatToggle;

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private int currentQuestionIndex = 0;
    private bool isWaitingForSelection = false;
    private int correctCount = 0;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Listening;

        if (optionButtons == null || optionButtons.Length == 0) {
            AutoFindOptionButtons();
        }

        ConfigureOptionButtons();
    }

    protected override void Start() {
        base.Start();

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        if (repeatToggle != null) {
            repeatToggle.onValueChanged.AddListener(OnRepeatToggleChanged);
        }

        currentQuestionIndex = 0;
        correctCount = 0;
        StartCoroutine(InitializeLessonRoutine());
    }

    private void AutoFindOptionButtons() {
        Transform container = transform.Find("Options");
        if (container == null) container = transform.Find("OptionsContainer");
        if (container == null) container = FindChildRecursive(transform, "Options");
        if (container == null) container = FindChildRecursive(transform, "OptionsContainer");

        List<Button> foundButtons = new List<Button>();
        if (container != null) {
            Button[] btns = container.GetComponentsInChildren<Button>(true);
            foreach (Button b in btns) {
                if (b != nextButton && !b.name.ToLower().Contains("next") && !b.name.ToLower().Contains("back")) {
                    foundButtons.Add(b);
                }
            }
        } else {
            Button[] allBtns = GetComponentsInChildren<Button>(true);
            foreach (Button b in allBtns) {
                if (b != nextButton && !b.name.ToLower().Contains("next") && !b.name.ToLower().Contains("back")) {
                    foundButtons.Add(b);
                }
            }
        }

        if (foundButtons.Count > 0) {
            optionButtons = foundButtons.ToArray();
        }
    }

    private Transform FindChildRecursive(Transform parent, string name) {
        foreach (Transform child in parent) {
            if (string.Equals(child.name, name, System.StringComparison.OrdinalIgnoreCase)) return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null) return found;
        }
        return null;
    }

    private void ConfigureOptionButtons() {
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                int optionIndex = i;
                if (optionButtons[i] != null) {
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionButtonClicked(optionIndex));
                    SetOptionButtonText(optionButtons[i], (Masters_Unit3_DirectionKind)i);
                }
            }
        }
    }

    private void SetOptionButtonText(Button btn, Masters_Unit3_DirectionKind kind) {
        if (btn == null) return;
        TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
        string text = kind == Masters_Unit3_DirectionKind.ASK ? "ASK" : (kind == Masters_Unit3_DirectionKind.MOVEMENT ? "MOVEMENT" : "POSITION");
        if (tmp != null) {
            tmp.text = text;
        } else {
            Text legacy = btn.GetComponentInChildren<Text>(true);
            if (legacy != null) legacy.text = text;
        }
    }

    private IEnumerator InitializeLessonRoutine() {
        if (Masters_AudioManager.Instance != null) {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(1.5f);
        }

        LoadQuestion(0);
    }

    private void LoadQuestion(int index) {
        if (questions == null || index >= questions.Length) {
            OnAllQuestionsCompleted();
            return;
        }

        currentQuestionIndex = index;
        isWaitingForSelection = false;

        if (progressTMP != null) {
            progressTMP.text = $"{currentQuestionIndex + 1}/{questions.Length}";
        }

        SetOptionButtonsInteractable(false);
        StartCoroutine(PlayQuestionAudioRoutine());
    }

    private IEnumerator PlayQuestionAudioRoutine() {
        QuestionData currentQ = questions[currentQuestionIndex];
        if (currentQ != null) {
            AudioClip clipToPlay = currentQ.phraseAudio;
            if (slowToggle != null && slowToggle.isOn && currentQ.slowAudio != null) {
                clipToPlay = currentQ.slowAudio;
            }

            if (clipToPlay != null) {
                if (Masters_AudioManager.Instance != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
                    yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
                } else {
                    AudioSource source = GetComponent<AudioSource>();
                    if (source == null) source = gameObject.AddComponent<AudioSource>();
                    source.PlayOneShot(clipToPlay);
                    yield return new WaitForSeconds(clipToPlay.length);
                }
            } else {
                yield return new WaitForSeconds(1.0f);
            }
        }

        isWaitingForSelection = true;
        SetOptionButtonsInteractable(true);
    }

    private void SetOptionButtonsInteractable(bool interactable) {
        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null && i < 3) {
                    optionButtons[i].interactable = interactable;
                }
            }
        }
    }

    private void OnOptionButtonClicked(int selectedIndex) {
        if (!isWaitingForSelection || questions == null || currentQuestionIndex >= questions.Length) return;

        QuestionData currentQ = questions[currentQuestionIndex];
        if (currentQ == null) return;

        isWaitingForSelection = false;
        SetOptionButtonsInteractable(false);

        bool isCorrect = (int)currentQ.correctKind == selectedIndex;
        Button selectedBtn = (optionButtons != null && selectedIndex < optionButtons.Length) ? optionButtons[selectedIndex] : null;

        if (isCorrect) {
            correctCount++;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
            StartCoroutine(ButtonFeedbackCoroutine(selectedBtn, true));
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            StartCoroutine(ButtonFeedbackCoroutine(selectedBtn, false));
        }
    }

    private IEnumerator ButtonFeedbackCoroutine(Button btn, bool isCorrect) {
        if (btn != null) {
            Image btnImage = btn.GetComponent<Image>();
            Color originalColor = btnImage != null ? btnImage.color : Color.white;

            if (btnImage != null) {
                btnImage.color = isCorrect ? Color.green : new Color(1f, 0.4f, 0.4f);
            }

            if (isCorrect) {
                btn.transform.DOScale(Vector3.one * 1.1f, animationSpeed).SetLoops(2, LoopType.Yoyo);
                yield return new WaitForSeconds(1.2f);
                if (btnImage != null) btnImage.color = originalColor;
                LoadQuestion(currentQuestionIndex + 1);
            } else {
                btn.transform.DOShakePosition(animationSpeed, new Vector3(15f, 0, 0), 20);
                yield return new WaitForSeconds(animationSpeed);
                if (btnImage != null) btnImage.color = originalColor;
                isWaitingForSelection = true;
                SetOptionButtonsInteractable(true);
            }
        } else {
            yield return new WaitForSeconds(0.5f);
            if (isCorrect) {
                LoadQuestion(currentQuestionIndex + 1);
            } else {
                isWaitingForSelection = true;
                SetOptionButtonsInteractable(true);
            }
        }
    }

    private void OnRepeatToggleChanged(bool isOn) {
        if (isOn && isWaitingForSelection) {
            StartCoroutine(PlayQuestionAudioRoutine());
        }
    }

    private void OnAllQuestionsCompleted() {
        if (correctCount >= passThreshold || correctCount >= 0) {
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClicked);
            }
        }

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
        }
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) {
            Debug.LogWarning($"Topic not set for {this.name}!");
            return;
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
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

    public void SetListeningData(QuestionData[] data) {
        questions = data;
    }
}
