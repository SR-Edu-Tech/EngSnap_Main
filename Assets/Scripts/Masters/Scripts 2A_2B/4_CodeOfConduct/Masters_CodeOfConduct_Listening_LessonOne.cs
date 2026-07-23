using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum Masters_Unit4_EtiquetteFamily {
    ThankYou = 0,
    YoureWelcome = 1,
    Sorry = 2,
    GoodJob = 3,
    Beautiful = 4
}

/// <summary>
/// Core Listening 1 controller for Unit 4: Code of Conduct (Book 2A).
/// Audio-to-family recognition across 10 rounds: student listens to a voiced etiquette phrase
/// and matches it to 1 of 5 family chips (`THANK YOU`, `YOU'RE WELCOME`, `SORRY`, `GOOD JOB`, `BEAUTIFUL`).
/// Leverages slow & repeat toggle architecture from Unit 1/2 Listening.
/// </summary>
public class Masters_CodeOfConduct_Listening_LessonOne : Masters_Lesson {

    [System.Serializable]
    public class EtiquetteQuestionData {
        public AudioClip expressionAudio;
        public AudioClip slowAudio;
        public string expressionText;
        public Masters_Unit4_EtiquetteFamily correctFamily;
    }

    [Header("Listening L1 Data")]
    [SerializeField] private EtiquetteQuestionData[] questions;
    [SerializeField] private Button[] optionButtons; // 0: THANK YOU, 1: YOU'RE WELCOME, 2: SORRY, 3: GOOD JOB, 4: BEAUTIFUL
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private float animationSpeed = 0.4f;
    [SerializeField] private int passThreshold = 8;

    [Header("Audio Toggles")]
    [SerializeField] private Toggle slowToggle;
    [SerializeField] private Toggle repeatToggle;

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private int currentQuestionIndex = 0;
    private int correctScore = 0;
    private bool isAnswering = false;
    private bool isSlowed = false;
    private bool isRepeatOn = false;
    private Coroutine audioCoroutine;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Listening;

        if (optionButtons == null || optionButtons.Length == 0 || optionButtons[0] == null) {
            AutoFindOptionButtons();
        }

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                int index = i;
                if (optionButtons[i] != null) {
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
                }
            }
        }

        AutoFindToggles();

        if (slowToggle != null) {
            slowToggle.onValueChanged.AddListener(OnSlowToggle);
        }
        if (repeatToggle != null) {
            repeatToggle.onValueChanged.AddListener(OnRepeatToggle);
        }
    }

    protected override void Start() {
        base.Start();

        ConfigureOptionButtons();

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        currentQuestionIndex = 0;
        correctScore = 0;
        StartCoroutine(InitializeLessonRoutine());
    }

    private void AutoFindOptionButtons() {
        Transform container = transform.Find("Options");
        if (container == null) container = transform.Find("OptionsContainer");
        if (container == null) container = FindChildRecursive(transform, "Options");
        if (container == null) container = FindChildRecursive(transform, "OptionsContainer");
        if (container == null) container = FindChildRecursive(transform, "Buttons");

        List<Button> foundButtons = new List<Button>();

        if (container != null) {
            Button[] btns = container.GetComponentsInChildren<Button>(true);
            foreach (Button b in btns) {
                if (b != nextButton && !b.name.ToLower().Contains("next") && !b.name.ToLower().Contains("back")) {
                    foundButtons.Add(b);
                }
            }
        }

        if (foundButtons.Count > 0) {
            optionButtons = foundButtons.ToArray();
        }
    }

    private void AutoFindToggles() {
        if (slowToggle == null) {
            Transform slowTrans = FindChildRecursive(transform, "SlowToggle");
            if (slowTrans == null) slowTrans = FindChildRecursive(transform, "Toggle_Slow");
            if (slowTrans != null) slowToggle = slowTrans.GetComponent<Toggle>();
        }

        if (repeatToggle == null) {
            Transform repeatTrans = FindChildRecursive(transform, "RepeatToggle");
            if (repeatTrans == null) repeatTrans = FindChildRecursive(transform, "Toggle_Repeat");
            if (repeatTrans != null) repeatToggle = repeatTrans.GetComponent<Toggle>();
        }
    }

    private Transform FindChildRecursive(Transform parent, string targetName) {
        foreach (Transform child in parent) {
            if (child.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase)) {
                return child;
            }
            Transform found = FindChildRecursive(child, targetName);
            if (found != null) return found;
        }
        return null;
    }

    private void ConfigureOptionButtons() {
        if (optionButtons == null) return;
        string[] labels = { "THANK YOU", "YOU'RE WELCOME", "SORRY", "GOOD JOB", "BEAUTIFUL" };
        for (int i = 0; i < optionButtons.Length; i++) {
            if (optionButtons[i] != null) {
                optionButtons[i].interactable = true;
                if (i < labels.Length) {
                    SetButtonText(optionButtons[i], labels[i]);
                }
            }
        }
    }

    private void SetButtonText(Button btn, string text) {
        if (btn == null) return;
        TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
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
            yield return new WaitForSeconds(3f);
        }

        if (questions != null && questions.Length > 0) {
            LoadQuestion(0);
        }
    }

    private void LoadQuestion(int index) {
        if (questions == null || index >= questions.Length) {
            OnAllQuestionsCompleted();
            return;
        }

        currentQuestionIndex = index;
        isAnswering = false;

        if (progressTMP != null) {
            progressTMP.text = $"{currentQuestionIndex + 1}/{questions.Length}";
        }

        PlayCurrentQuestionAudio();
    }

    private void PlayCurrentQuestionAudio() {
        if (audioCoroutine != null) {
            StopCoroutine(audioCoroutine);
            audioCoroutine = null;
        }

        if (questions == null || currentQuestionIndex >= questions.Length) return;

        EtiquetteQuestionData question = questions[currentQuestionIndex];
        if (question == null) return;

        AudioClip clipToPlay = (isSlowed && question.slowAudio != null) ? question.slowAudio : question.expressionAudio;
        if (clipToPlay == null) clipToPlay = question.expressionAudio;

        if (clipToPlay != null && Masters_AudioManager.Instance != null) {
            if (isRepeatOn) {
                audioCoroutine = StartCoroutine(PlayInRepeatCoroutine(clipToPlay));
            } else {
                Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
            }
        }
    }

    private IEnumerator PlayInRepeatCoroutine(AudioClip clip) {
        while (isRepeatOn && !isAnswering) {
            if (Masters_AudioManager.Instance != null && clip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clip);
            }
            float delay = (clip != null) ? clip.length + 2f : 3f;
            yield return new WaitForSeconds(delay);
        }
    }

    private void OnOptionSelected(int buttonIndex) {
        if (isAnswering || questions == null || currentQuestionIndex >= questions.Length) return;

        EtiquetteQuestionData q = questions[currentQuestionIndex];
        if (q == null) return;

        Masters_Unit4_EtiquetteFamily selectedFamily = (Masters_Unit4_EtiquetteFamily)buttonIndex;

        if (selectedFamily == q.correctFamily) {
            isAnswering = true;
            if (audioCoroutine != null) {
                StopCoroutine(audioCoroutine);
                audioCoroutine = null;
            }
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
            }
            correctScore++;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (optionButtons != null && buttonIndex < optionButtons.Length && optionButtons[buttonIndex] != null) {
                StartCoroutine(ButtonFeedbackCoroutine(optionButtons[buttonIndex], true));
            } else {
                StartCoroutine(WaitAndLoadNextQuestion(1.0f));
            }
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            if (optionButtons != null && buttonIndex < optionButtons.Length && optionButtons[buttonIndex] != null) {
                StartCoroutine(ButtonFeedbackCoroutine(optionButtons[buttonIndex], false));
            }
        }
    }

    private IEnumerator ButtonFeedbackCoroutine(Button btn, bool isCorrect) {
        Image img = btn.GetComponent<Image>();
        Color originalColor = img != null ? img.color : Color.white;

        if (img != null) {
            img.color = isCorrect ? Color.green : new Color(1f, 0.4f, 0.4f);
        }

        if (isCorrect) {
            btn.transform.DOScale(Vector3.one * 1.1f, animationSpeed).SetLoops(2, LoopType.Yoyo);
            yield return new WaitForSeconds(1.2f);
            if (img != null) img.color = originalColor;
            LoadQuestion(currentQuestionIndex + 1);
        } else {
            btn.transform.DOShakePosition(animationSpeed, new Vector3(15f, 0, 0), 20);
            yield return new WaitForSeconds(animationSpeed);
            if (img != null) img.color = originalColor;
        }
    }

    private IEnumerator WaitAndLoadNextQuestion(float delay) {
        yield return new WaitForSeconds(delay);
        LoadQuestion(currentQuestionIndex + 1);
    }

    private void OnSlowToggle(bool isOn) {
        isSlowed = isOn;
        PlayCurrentQuestionAudio();
    }

    private void OnRepeatToggle(bool isOn) {
        isRepeatOn = isOn;
        PlayCurrentQuestionAudio();
    }

    private void OnAllQuestionsCompleted() {
        if (audioCoroutine != null) {
            StopCoroutine(audioCoroutine);
            audioCoroutine = null;
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (correctScore >= passThreshold) {
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                NextButtonAnimation();
            }
        } else {
            currentQuestionIndex = 0;
            correctScore = 0;
            LoadQuestion(0);
        }
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) {
            return;
        }
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
}
