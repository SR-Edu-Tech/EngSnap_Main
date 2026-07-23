using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Reading 3 controller for Unit 4: Code of Conduct (Book 2A).
/// R03 Say It Better — Swap the Plain Word: 6 rounds.
/// Displays a sentence with a plain/basic word underlined on card. A rail of 4 option chips is shown.
/// Student taps the correct richer verbatim word to swap; the sentence updates live and ARIA voices the polished sentence.
/// </summary>
public class Masters_CodeOfConduct_Reading_LessonThree : Masters_Lesson {

    [System.Serializable]
    public class SwapQuestionData {
        [TextArea(2, 3)] public string sentenceText;      // e.g. "What a <u>nice</u> painting!"
        public string plainWord;                          // e.g. "nice"
        public string correctRicherWord;                  // e.g. "Gorgeous"
        public AudioClip improvedSentenceAudio;
        public string[] wrongDistractorChips;
    }

    [Header("Reading R03 Setup")]
    [SerializeField] private SwapQuestionData[] questions;
    [SerializeField] private TextMeshProUGUI sentenceTMP;
    [SerializeField] private Button[] optionButtons; // 4 chip buttons
    [SerializeField] private Button keepButton;      // Disabled phase-1 button
    [SerializeField] private Button fixButton;       // Disabled phase-1 button
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private float animationSpeed = 0.35f;
    [SerializeField] private int passThreshold = 5;

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private int currentQuestionIndex = 0;
    private int correctScore = 0;
    private bool canClick = false;
    private Dictionary<Button, Color> defaultButtonColors = new Dictionary<Button, Color>();

    private void CacheButtonColor(Button btn) {
        if (btn == null) return;
        if (!defaultButtonColors.ContainsKey(btn)) {
            Image img = btn.GetComponent<Image>();
            if (img != null) defaultButtonColors[btn] = img.color;
        }
    }

    private void RestoreButtonColor(Button btn) {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null && defaultButtonColors.ContainsKey(btn)) {
            img.color = defaultButtonColors[btn];
        }
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Reading;

        DisableKeepAndFixButtons();

        if (optionButtons == null || optionButtons.Length == 0) {
            AutoFindOptionButtons();
        }
        if (sentenceTMP == null) {
            AutoFindSentenceTMP();
        }
    }

    protected override void Start() {
        base.Start();

        DisableKeepAndFixButtons();

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        currentQuestionIndex = 0;
        correctScore = 0;

        if (optionButtons != null) {
            foreach (var btn in optionButtons) {
                if (btn != null) {
                    btn.transform.DOKill();
                    btn.transform.localScale = Vector3.zero;
                    btn.gameObject.SetActive(false);
                }
            }
        }
        if (sentenceTMP != null) {
            sentenceTMP.transform.DOKill();
            sentenceTMP.transform.localScale = Vector3.zero;
        }

        StartCoroutine(InitializeLessonRoutine());
    }

    private void DisableKeepAndFixButtons() {
        if (keepButton != null) keepButton.gameObject.SetActive(false);
        if (fixButton != null) fixButton.gameObject.SetActive(false);

        string[] namesToHide = { "KeepButton", "FixButton", "Button_Keep", "Button_Fix", "Keep", "Fix" };
        foreach (string n in namesToHide) {
            Transform found = FindChildRecursive(transform, n);
            if (found != null && found.GetComponent<Button>() != null) {
                found.gameObject.SetActive(false);
            }
        }
    }

    private void AutoFindOptionButtons() {
        Transform container = transform.Find("Options");
        if (container == null) container = transform.Find("OptionsContainer");
        if (container == null) container = transform.Find("Chips");
        if (container == null) container = FindChildRecursive(transform, "Options");
        if (container == null) container = FindChildRecursive(transform, "OptionsContainer");

        if (container != null) {
            List<Button> btns = new List<Button>();
            foreach (Button b in container.GetComponentsInChildren<Button>(true)) {
                if (b != nextButton && !b.name.ToLower().Contains("next") && !b.name.ToLower().Contains("back") &&
                    !b.name.ToLower().Contains("keep") && !b.name.ToLower().Contains("fix")) {
                    btns.Add(b);
                }
            }
            if (btns.Count > 0) optionButtons = btns.ToArray();
        }
    }

    private void AutoFindSentenceTMP() {
        Transform trans = FindChildRecursive(transform, "Sentence");
        if (trans == null) trans = FindChildRecursive(transform, "SentenceText");
        if (trans == null) trans = FindChildRecursive(transform, "CardText");
        if (trans != null) {
            sentenceTMP = trans.GetComponent<TextMeshProUGUI>();
            if (sentenceTMP == null) sentenceTMP = trans.GetComponentInChildren<TextMeshProUGUI>();
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

    private IEnumerator InitializeLessonRoutine() {
        if (Masters_AudioManager.Instance != null && narratorSpeech != null) {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(narratorSpeech != null ? narratorSpeech.length + 0.5f : 2.5f);
        }

        if (questions != null && questions.Length > 0) {
            LoadQuestion(0);
        } else {
            EndLesson();
        }
    }

    private void LoadQuestion(int index) {
        if (questions == null || index >= questions.Length) {
            OnAllQuestionsCompleted();
            return;
        }

        currentQuestionIndex = index;
        canClick = false;

        if (progressTMP != null) {
            progressTMP.text = $"{currentQuestionIndex + 1}/{questions.Length}";
        }

        SwapQuestionData q = questions[currentQuestionIndex];
        if (q == null) {
            LoadQuestion(currentQuestionIndex + 1);
            return;
        }

        if (sentenceTMP != null) {
            sentenceTMP.text = q.sentenceText;
            sentenceTMP.transform.DOKill();
            sentenceTMP.transform.localScale = Vector3.zero;
            sentenceTMP.gameObject.SetActive(true);
            sentenceTMP.transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        }

        List<string> options = new List<string>();
        options.Add(q.correctRicherWord);
        if (q.wrongDistractorChips != null) {
            foreach (string dist in q.wrongDistractorChips) {
                if (!string.IsNullOrEmpty(dist) && !options.Contains(dist)) {
                    options.Add(dist);
                }
            }
        }

        for (int i = 0; i < options.Count; i++) {
            string temp = options[i];
            int randIndex = Random.Range(i, options.Count);
            options[i] = options[randIndex];
            options[randIndex] = temp;
        }

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    if (i < options.Count) {
                        string optionText = options[i];
                        optionButtons[i].gameObject.SetActive(true);
                        optionButtons[i].interactable = true;
                        SetButtonText(optionButtons[i], optionText);

                        CacheButtonColor(optionButtons[i]);
                        RestoreButtonColor(optionButtons[i]);

                        optionButtons[i].transform.DOKill();
                        optionButtons[i].transform.localScale = Vector3.zero;

                        int btnIndex = i;
                        optionButtons[i].onClick.RemoveAllListeners();
                        optionButtons[i].onClick.AddListener(() => OnOptionClicked(btnIndex, optionText, q));

                        optionButtons[i].transform.DOScale(Vector3.one, animationSpeed)
                            .SetDelay(i * 0.08f)
                            .SetEase(Ease.OutBack)
                            .OnComplete(() => {
                                if (btnIndex == Mathf.Min(options.Count - 1, optionButtons.Length - 1)) {
                                    canClick = true;
                                }
                            });
                    } else {
                        optionButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        } else {
            canClick = true;
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

    protected virtual void OnOptionClicked(int buttonIndex, string selectedText, SwapQuestionData q) {
        if (!canClick) return;

        bool isCorrect = (selectedText == q.correctRicherWord);

        if (isCorrect) {
            canClick = false;
            correctScore++;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (sentenceTMP != null) {
                // Live swap the underlined plain word with the richer word in gold/bold!
                string updated = q.sentenceText;
                if (!string.IsNullOrEmpty(q.plainWord)) {
                    updated = q.sentenceText.Replace($"<u>{q.plainWord}</u>", $"<color=#FFD700><b>{q.correctRicherWord}</b></color>");
                    if (updated == q.sentenceText) {
                        updated = q.sentenceText.Replace(q.plainWord, $"<color=#FFD700><b>{q.correctRicherWord}</b></color>");
                    }
                } else {
                    updated = $"{q.sentenceText}\n<color=#FFD700><b>➔ {q.correctRicherWord}</b></color>";
                }
                sentenceTMP.text = updated;
                sentenceTMP.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }

            if (buttonIndex < optionButtons.Length && optionButtons[buttonIndex] != null) {
                StartCoroutine(ButtonFeedbackCoroutine(optionButtons[buttonIndex], true, q.improvedSentenceAudio));
            } else {
                StartCoroutine(WaitAndLoadNextRound(1.5f));
            }
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            if (buttonIndex < optionButtons.Length && optionButtons[buttonIndex] != null) {
                StartCoroutine(ButtonFeedbackCoroutine(optionButtons[buttonIndex], false, null));
            }
        }
    }

    private IEnumerator ButtonFeedbackCoroutine(Button btn, bool isCorrect, AudioClip clipToPlay) {
        Image img = btn.GetComponent<Image>();
        if (img != null) {
            img.color = isCorrect ? Color.green : new Color(1f, 0.4f, 0.4f);
        }

        if (isCorrect) {
            btn.transform.DOScale(Vector3.one * 1.08f, 0.25f).SetLoops(2, LoopType.Yoyo);
            if (clipToPlay != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(clipToPlay);
                yield return new WaitForSeconds(clipToPlay.length + 0.3f);
            } else {
                yield return new WaitForSeconds(1.2f);
            }
            LoadQuestion(currentQuestionIndex + 1);
        } else {
            btn.transform.DOShakePosition(animationSpeed, new Vector3(15f, 0, 0), 20);
            yield return new WaitForSeconds(animationSpeed);
            RestoreButtonColor(btn);
        }
    }

    private IEnumerator WaitAndLoadNextRound(float delay) {
        yield return new WaitForSeconds(delay);
        LoadQuestion(currentQuestionIndex + 1);
    }

    private void OnAllQuestionsCompleted() {
        if (correctScore >= passThreshold) {
            EndLesson();
        } else {
            currentQuestionIndex = 0;
            correctScore = 0;
            LoadQuestion(0);
        }
    }

    private void EndLesson() {
        if (sentenceTMP != null) sentenceTMP.gameObject.SetActive(false);
        if (optionButtons != null) {
            foreach (var b in optionButtons) if (b != null) b.gameObject.SetActive(false);
        }

        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            NextButtonAnimation();
        } else {
            OnNextButtonClicked();
        }
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) return;

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
