using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Reading 3 controller for Unit 2: Clear Confusion (Book 2A).
/// R03 Fix the Rude Line — Make It Polite:
/// Phase 1: Displays a blunt/rude classroom card. Player taps KEEP or FIX. Since all 6 items are rude contrasts, player must tap FIX.
/// Phase 2: When FIX is tapped, 4 replacement options appear containing the polite verbatim fix and 3 distractors.
/// </summary>
public class Masters_ClearConfusion_Reading_LessonThree : Masters_Lesson {

    [System.Serializable]
    public class RegisterCheckQuestion {
        public string situation;      // e.g. "Rude contrast line shown:"
        public string spokenLine;     // e.g. "'Say it again!'"
        public bool isCorrectLine;    // False (since all items in R03 need fixing)
        public string correctFix;     // e.g. "Pardon ma'am, could you say that again?"
        public AudioClip correctFixAudio;
        public string[] distractors;
    }

    [Header("Reading R03 Setup")]
    [SerializeField] private RegisterCheckQuestion[] questions;
    [SerializeField] private TextMeshProUGUI situationTMP;   // Narrating scene/situation
    [SerializeField] private TextMeshProUGUI sentenceTMP;    // Displaying rude spoken line
    [SerializeField] private Button keepButton;              // Option 1: KEEP
    [SerializeField] private Button fixButton;               // Option 2: FIX
    [SerializeField] private Button[] optionButtons;         // 4 replacement options prompted when FIX is tapped
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private float animationSpeed = 0.35f;
    [SerializeField] private int passThreshold = 5;

    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private int currentQuestionIndex = 0;
    private int correctScore = 0;
    private bool canClick = false;
    private bool inPhaseTwo = false;
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

#if UNITY_EDITOR
    private void Reset() {
        AutoFindUI();
    }

    private void OnValidate() {
    }
#endif

    protected override void Awake() {
        base.Awake();
        AutoFindUI();
    }

    protected override void Start() {
        base.Start();

        if (nextButton != null) {
            nextButton.gameObject.SetActive(false);
        }

        currentQuestionIndex = 0;
        correctScore = 0;

        EnsureTwoStageUIElements();

        if (keepButton != null) {
            keepButton.transform.localScale = Vector3.zero;
            keepButton.gameObject.SetActive(false);
        }
        if (fixButton != null) {
            fixButton.transform.localScale = Vector3.zero;
            fixButton.gameObject.SetActive(false);
        }
        if (optionButtons != null) {
            foreach (var btn in optionButtons) {
                if (btn != null) {
                    btn.transform.localScale = Vector3.zero;
                    btn.gameObject.SetActive(false);
                }
            }
        }
        if (situationTMP != null) {
            situationTMP.transform.localScale = Vector3.zero;
            situationTMP.gameObject.SetActive(false);
        }
        if (sentenceTMP != null) {
            sentenceTMP.transform.localScale = Vector3.zero;
            sentenceTMP.gameObject.SetActive(false);
        }

        StartCoroutine(InitializeLessonRoutine());
    }

    private void AutoFindUI() {
        if (optionButtons == null || optionButtons.Length == 0) {
            List<Button> foundBtns = new List<Button>();
            Button[] allButtons = GetComponentsInChildren<Button>(true);
            foreach (Button btn in allButtons) {
                if (btn != null && btn != nextButton && btn != keepButton && btn != fixButton && btn.name.ToLower().Contains("option")) {
                    foundBtns.Add(btn);
                }
            }
            if (foundBtns.Count > 0) optionButtons = foundBtns.ToArray();
        }
    }

    private void EnsureTwoStageUIElements() {
        AutoFindUI();

        if (sentenceTMP != null && situationTMP == null) {
            Transform sitTrans = FindChildRecursive(transform, "SituationTMP");
            if (sitTrans == null) sitTrans = FindChildRecursive(transform, "Situation");
            if (sitTrans != null) situationTMP = sitTrans.GetComponent<TextMeshProUGUI>();

            if (situationTMP == null) {
                GameObject sitGo = Instantiate(sentenceTMP.gameObject, sentenceTMP.transform.parent);
                sitGo.name = "SituationTMP";
                situationTMP = sitGo.GetComponent<TextMeshProUGUI>();
                if (situationTMP != null) {
                    situationTMP.fontSize = Mathf.Min(sentenceTMP.fontSize * 0.9f, 32f);
                    situationTMP.color = new Color(1f, 0.9f, 0.3f);
                    situationTMP.alignment = TextAlignmentOptions.Center;
                    RectTransform sitRect = situationTMP.rectTransform;
                    if (sitRect != null) {
                        sitRect.anchoredPosition = new Vector2(sitRect.anchoredPosition.x, sitRect.anchoredPosition.y + 110f);
                    }
                }
            }
        }

        if (optionButtons != null && optionButtons.Length > 0 && optionButtons[0] != null) {
            if (keepButton == null) {
                Transform kTrans = FindChildRecursive(transform, "KeepButton");
                if (kTrans != null) keepButton = kTrans.GetComponent<Button>();
                if (keepButton == null) {
                    GameObject keepGo = Instantiate(optionButtons[0].gameObject, optionButtons[0].transform.parent);
                    keepGo.name = "KeepButton";
                    keepButton = keepGo.GetComponent<Button>();
                    SetButtonText(keepButton, "KEEP (Looks Polite)");
                    SetButtonColor(keepButton, new Color(0.3f, 0.5f, 0.8f));
                    RectTransform rect = keepButton.GetComponent<RectTransform>();
                    if (rect != null) rect.anchoredPosition = new Vector2(-260f, -140f);
                }
            }
            if (fixButton == null) {
                Transform fTrans = FindChildRecursive(transform, "FixButton");
                if (fTrans != null) fixButton = fTrans.GetComponent<Button>();
                if (fixButton == null) {
                    GameObject fixGo = Instantiate(optionButtons[0].gameObject, optionButtons[0].transform.parent);
                    fixGo.name = "FixButton";
                    fixButton = fixGo.GetComponent<Button>();
                    SetButtonText(fixButton, "FIX (Too Rude)");
                    SetButtonColor(fixButton, new Color(0.85f, 0.45f, 0.2f));
                    RectTransform rect = fixButton.GetComponent<RectTransform>();
                    if (rect != null) rect.anchoredPosition = new Vector2(260f, -140f);
                }
            }
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
        LoadQuestion(0);
    }

    private void LoadQuestion(int questionIdx) {
        if (questions == null || questionIdx >= questions.Length) {
            OnAllQuestionsCompleted();
            return;
        }

        currentQuestionIndex = questionIdx;
        canClick = false;
        inPhaseTwo = false;

        if (progressTMP != null) {
            progressTMP.text = $"{currentQuestionIndex + 1}/{questions.Length}";
        }

        RegisterCheckQuestion q = questions[currentQuestionIndex];
        if (q == null) return;

        if (optionButtons != null) {
            foreach (var btn in optionButtons) {
                if (btn != null) {
                    btn.interactable = true;
                    btn.gameObject.SetActive(false);
                }
            }
        }

        if (situationTMP != null) {
            if (!string.IsNullOrEmpty(q.situation)) {
                situationTMP.text = q.situation;
                situationTMP.transform.DOKill();
                situationTMP.transform.localScale = Vector3.zero;
                situationTMP.gameObject.SetActive(true);
                situationTMP.transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
            } else {
                situationTMP.gameObject.SetActive(false);
            }
        }

        if (sentenceTMP != null) {
            sentenceTMP.text = q.spokenLine;
            sentenceTMP.transform.DOKill();
            sentenceTMP.transform.localScale = Vector3.zero;
            sentenceTMP.gameObject.SetActive(true);
            sentenceTMP.transform.DOScale(Vector3.one, animationSpeed).SetDelay(0.08f).SetEase(Ease.OutBack);
        }

        if (keepButton != null) {
            keepButton.onClick.RemoveAllListeners();
            keepButton.onClick.AddListener(OnKeepButtonClicked);
            keepButton.interactable = true;
            keepButton.transform.DOKill();
            keepButton.transform.localScale = Vector3.zero;
            keepButton.gameObject.SetActive(true);
            keepButton.transform.DOScale(Vector3.one, animationSpeed).SetDelay(0.16f).SetEase(Ease.OutBack);
        }

        if (fixButton != null) {
            fixButton.onClick.RemoveAllListeners();
            fixButton.onClick.AddListener(OnFixButtonClicked);
            fixButton.interactable = true;
            fixButton.transform.DOKill();
            fixButton.transform.localScale = Vector3.zero;
            fixButton.gameObject.SetActive(true);
            fixButton.transform.DOScale(Vector3.one, animationSpeed).SetDelay(0.24f).SetEase(Ease.OutBack);
        }

        StartCoroutine(EnableClickAfterDelay(animationSpeed + 0.3f));
    }

    private IEnumerator EnableClickAfterDelay(float delay) {
        yield return new WaitForSeconds(delay);
        canClick = true;
    }

    private void OnKeepButtonClicked() {
        if (!canClick || keepButton == null || inPhaseTwo) return;

        if (questions == null || currentQuestionIndex >= questions.Length) return;
        RegisterCheckQuestion q = questions[currentQuestionIndex];

        if (q.isCorrectLine) {
            canClick = false;
            correctScore++;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (keepButton.TryGetComponent(out Masters_ButtonPunchAnimator punch)) {
                punch.Punch();
            } else {
                keepButton.transform.DOKill();
                keepButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }

            StartCoroutine(NextQuestionRoutine(1.0f));
        } else {
            // Wrong choice: this line is rude and needs FIX
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            keepButton.transform.DOKill(true);
            keepButton.transform.DOShakePosition(0.4f, new Vector3(14f, 0, 0));
        }
    }

    private void OnFixButtonClicked() {
        if (!canClick || fixButton == null || inPhaseTwo) return;

        if (questions == null || currentQuestionIndex >= questions.Length) return;
        RegisterCheckQuestion q = questions[currentQuestionIndex];

        if (!q.isCorrectLine) {
            inPhaseTwo = true;
            canClick = false;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }

            if (fixButton.TryGetComponent(out Masters_ButtonPunchAnimator punch)) {
                punch.Punch();
            } else {
                fixButton.transform.DOPunchScale(Vector3.one * 0.15f, 0.25f);
            }

            if (keepButton != null) keepButton.transform.DOScale(Vector3.zero, 0.25f).OnComplete(() => keepButton.gameObject.SetActive(false));
            if (fixButton != null) fixButton.transform.DOScale(Vector3.zero, 0.25f).OnComplete(() => {
                fixButton.gameObject.SetActive(false);
                RevealReplacementOptions();
            });
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            fixButton.transform.DOKill(true);
            fixButton.transform.DOShakePosition(0.4f, new Vector3(14f, 0, 0));
        }
    }

    private void RevealReplacementOptions() {
        if (questions == null || currentQuestionIndex >= questions.Length) return;
        RegisterCheckQuestion q = questions[currentQuestionIndex];

        List<string> options = new List<string>();
        options.Add(q.correctFix);
        if (q.distractors != null) {
            foreach (string dist in q.distractors) {
                if (options.Count < (optionButtons != null ? optionButtons.Length : 4) && !string.IsNullOrEmpty(dist)) {
                    options.Add(dist);
                }
            }
        }

        for (int i = 0; i < options.Count; i++) {
            string temp = options[i];
            int rand = Random.Range(i, options.Count);
            options[i] = options[rand];
            options[rand] = temp;
        }

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
                    Button currentBtn = optionButtons[i];
                    currentBtn.onClick.RemoveAllListeners();

                    if (i < options.Count) {
                        string optionTextStr = options[i];
                        SetButtonText(currentBtn, optionTextStr);
                        currentBtn.onClick.AddListener(() => OnReplacementOptionSelected(currentBtn, optionTextStr, q.correctFix, q.correctFixAudio));

                        currentBtn.interactable = true;
                        CacheButtonColor(currentBtn);
                        RestoreButtonColor(currentBtn);

                        currentBtn.transform.DOKill();
                        currentBtn.transform.localScale = Vector3.zero;
                        currentBtn.gameObject.SetActive(true);
                        currentBtn.transform.DOScale(Vector3.one, animationSpeed).SetDelay(i * 0.08f).SetEase(Ease.OutBack);
                    } else {
                        currentBtn.gameObject.SetActive(false);
                    }
                }
            }
        }

        StartCoroutine(EnableClickAfterDelay(animationSpeed + 0.35f));
    }

    private void OnReplacementOptionSelected(Button selectedBtn, string chosenText, string correctFix, AudioClip revealAudio) {
        if (!canClick || selectedBtn == null) return;

        if (string.Equals(chosenText.Trim(), correctFix.Trim(), System.StringComparison.OrdinalIgnoreCase)) {
            canClick = false;
            correctScore++;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
                if (revealAudio != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(revealAudio);
                }
            }

            Image img = selectedBtn.GetComponent<Image>();
            if (img != null) img.color = Color.green;

            if (selectedBtn.TryGetComponent(out Masters_ButtonPunchAnimator punch)) {
                punch.Punch();
            } else {
                selectedBtn.transform.DOKill();
                selectedBtn.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }

            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null && optionButtons[i] != selectedBtn && optionButtons[i].gameObject.activeSelf) {
                    optionButtons[i].interactable = false;
                    optionButtons[i].transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack);
                }
            }

            StartCoroutine(NextQuestionRoutine(revealAudio != null ? revealAudio.length + 0.6f : 1.5f));
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            Image img = selectedBtn.GetComponent<Image>();
            if (img != null) {
                img.color = new Color(1f, 0.4f, 0.4f);
                selectedBtn.transform.DOShakePosition(0.4f, new Vector3(12f, 0, 0)).OnComplete(() => {
                    RestoreButtonColor(selectedBtn);
                });
            }
        }
    }

    private IEnumerator NextQuestionRoutine(float delay) {
        yield return new WaitForSeconds(delay);
        LoadQuestion(currentQuestionIndex + 1);
    }

    private void OnAllQuestionsCompleted() {
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (correctScore >= passThreshold) {
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                NextButtonAnimation();
            } else {
                OnNextButtonClicked();
            }
        } else {
            currentQuestionIndex = 0;
            correctScore = 0;
            LoadQuestion(0);
        }
    }

    private void SetButtonText(Button btn, string text) {
        if (btn == null) return;
        TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null) {
            tmp.text = text;
            tmp.enableWordWrapping = true;
            tmp.alignment = TextAlignmentOptions.Center;
        } else {
            Text legacy = btn.GetComponentInChildren<Text>(true);
            if (legacy != null) {
                legacy.text = text;
                legacy.alignment = TextAnchor.MiddleCenter;
            }
        }
    }

    private void SetButtonColor(Button btn, Color color) {
        if (btn == null) return;
        Image img = btn.GetComponent<Image>();
        if (img != null && img.sprite != null) {
            img.color = color;
        }
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) return;
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

    private void EndLesson() {
        OnAllQuestionsCompleted();
    }
}
