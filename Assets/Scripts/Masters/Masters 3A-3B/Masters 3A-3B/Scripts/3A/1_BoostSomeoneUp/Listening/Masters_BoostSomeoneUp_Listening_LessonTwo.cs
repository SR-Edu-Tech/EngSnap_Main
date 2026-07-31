using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Listening 2 controller for Book 3A Unit 1: Boost Someone Up! (Cheer the Pal — Situation + 3 Replies).
/// Exact copy of Book 2 PolishedCommunication_Listening_LessonOne adapted for 3 multiple choice buttons.
/// Includes full Slow Toggle and Repeat Toggle functionality.
/// </summary>
public class Masters_BoostSomeoneUp_Listening_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class SituationQuestionData {
        public AudioClip expressionAudio;
        public AudioClip slowAudio;
        public string expressionText;
        public string[] options; // The 3 reply choices A, B, C
        public int correctOptionIndex; // 0, 1, or 2
    }

    [Header("Listening L2 Data")]
    [SerializeField] private SituationQuestionData[] questions;
    [SerializeField] private Button[] optionButtons; // Index 0: Reply A, Index 1: Reply B, Index 2: Reply C
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private float animationSpeed = 0.4f;
    [SerializeField] private int passThreshold = 5;

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

        if (optionButtons == null || optionButtons.Length == 0 || optionButtons[0] == null) {
            AutoFindOptionButtons();
        }

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                int index = i;
                if (optionButtons[i] != null) {
                    optionButtons[i].onClick.RemoveAllListeners();
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
                }
            }
        }

        AutoFindToggles();

        if (slowToggle != null) {
            slowToggle.onValueChanged.RemoveAllListeners();
            slowToggle.onValueChanged.AddListener(OnSlowToggle);
        }
        if (repeatToggle != null) {
            repeatToggle.onValueChanged.RemoveAllListeners();
            repeatToggle.onValueChanged.AddListener(OnRepeatToggle);
        }

#if UNITY_EDITOR
        AutoLoadSlowAudioInEditor();
#endif
    }

    protected override void Start() {
        base.Start();

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

    private void AutoFindToggles() {
        if (slowToggle == null) {
            Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
            foreach (Toggle t in toggles) {
                if (t.name.IndexOf("slow", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    t.name.IndexOf("turtle", System.StringComparison.OrdinalIgnoreCase) >= 0) {
                    slowToggle = t;
                    break;
                }
            }
        }

        if (repeatToggle == null) {
            Toggle[] toggles = GetComponentsInChildren<Toggle>(true);
            foreach (Toggle t in toggles) {
                if (t.name.IndexOf("repeat", System.StringComparison.OrdinalIgnoreCase) >= 0) {
                    repeatToggle = t;
                    break;
                }
            }
        }
    }

#if UNITY_EDITOR
    private void AutoLoadSlowAudioInEditor() {
        if (questions == null) return;
        for (int i = 0; i < questions.Length; i++) {
            if (questions[i] != null && questions[i].slowAudio == null && questions[i].expressionAudio != null) {
                string slowPath = $"Assets/Audio/3A/1_BoostSomeoneUp/Slow/{questions[i].expressionAudio.name}.mp3";
                AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(slowPath);
                if (clip != null) {
                    questions[i].slowAudio = clip;
                }
            }
        }
    }
#endif

    private void ConfigureOptionButtonsForQuestion(SituationQuestionData q) {
        if (optionButtons != null && q != null) {
            int optionsCount = (q.options != null && q.options.Length > 0) ? q.options.Length : optionButtons.Length;
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    if (i < optionsCount) {
                        optionButtons[i].gameObject.SetActive(true);
                        if (q.options != null && i < q.options.Length) {
                            SetButtonText(optionButtons[i], q.options[i]);
                        }
                    } else {
                        optionButtons[i].gameObject.SetActive(false);
                    }
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

    private new Transform FindChildRecursive(Transform parent, string targetName) {
        foreach (Transform child in parent) {
            if (child.name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase)) {
                return child;
            }
            Transform found = FindChildRecursive(child, targetName);
            if (found != null) return found;
        }
        return null;
    }

    private IEnumerator InitializeLessonRoutine() {
        if (optionButtons != null) {
            foreach (Button btn in optionButtons) {
                if (btn != null && btn.gameObject.activeSelf) btn.transform.localScale = Vector3.zero;
            }
        }

        if (Masters_AudioManager.Instance != null) {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd((System.Action)null);
        } else {
            yield return new WaitForSeconds(2f);
        }

        if (optionButtons != null) {
            foreach (Button btn in optionButtons) {
                if (btn != null && btn.gameObject.activeSelf) {
                    if (Masters_AudioManager.Instance != null) {
                        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Pop);
                    }
                    btn.transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
                    yield return new WaitForSeconds(0.2f);
                }
            }
        }

        LoadQuestion(0);
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

        SituationQuestionData q = questions[currentQuestionIndex];
        ConfigureOptionButtonsForQuestion(q);

        PlayCurrentQuestionAudio();
    }

    private void PlayCurrentQuestionAudio() {
        if (audioCoroutine != null) {
            StopCoroutine(audioCoroutine);
            audioCoroutine = null;
        }

        if (questions == null || currentQuestionIndex >= questions.Length) return;

        SituationQuestionData question = questions[currentQuestionIndex];
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

    private void OnSlowToggle(bool value) {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        if (slowToggle != null) {
            slowToggle.DOKill(true);
            slowToggle.transform.localScale = Vector3.one;
            slowToggle.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);
        }

        isSlowed = value;

        if (questions != null && questions.Length > 0 && currentQuestionIndex < questions.Length && !isAnswering) {
            PlayCurrentQuestionAudio();
        }
    }

    private void OnRepeatToggle(bool value) {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }

        if (repeatToggle != null) {
            repeatToggle.DOKill(true);
            repeatToggle.transform.localScale = Vector3.one;
            repeatToggle.transform.DOPunchScale(Vector3.one * 0.2f, 0.2f, 8, 0.8f);
        }

        isRepeatOn = value;

        if (value == false) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.StopVoiceOver();
            }
            if (audioCoroutine != null) {
                StopCoroutine(audioCoroutine);
                audioCoroutine = null;
            }
        } else {
            if (questions != null && questions.Length > 0 && currentQuestionIndex < questions.Length && !isAnswering) {
                PlayCurrentQuestionAudio();
            }
        }
    }

    private void OnOptionSelected(int buttonIndex) {
        if (isAnswering || questions == null || currentQuestionIndex >= questions.Length) return;

        SituationQuestionData q = questions[currentQuestionIndex];
        if (q == null) return;

        if (buttonIndex == q.correctOptionIndex) {
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
                optionButtons[buttonIndex].transform.DOPunchScale(Vector3.one * 0.2f, 0.3f);
            }
            StartCoroutine(NextQuestionRoutine());
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            if (optionButtons != null && buttonIndex < optionButtons.Length && optionButtons[buttonIndex] != null) {
                optionButtons[buttonIndex].transform.DOShakePosition(0.4f, new Vector3(10f, 0, 0));
            }
        }
    }

    private IEnumerator NextQuestionRoutine() {
        yield return new WaitForSeconds(1.2f);
        LoadQuestion(currentQuestionIndex + 1);
    }

    private void OnAllQuestionsCompleted() {
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
}
