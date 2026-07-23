using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Reading 3 controller for Unit 1: Polished Communication (Book 2A Reference Base).
/// R03 Spot the Wrong-Register Line:
/// Phase 1: Displays two text fields: one narrating the scene (situationTMP) and another displaying the spoken line (sentenceTMP).
///          Player taps KEEP or FIX to decide if the register fits the situation.
///          If isCorrectLine is true (e.g. #3 and #5), player must tap KEEP to continue directly.
///          If isCorrectLine is false, tapping FIX reveals Phase 2 replacement options.
/// Phase 2: If player correctly taps FIX on an incorrect line, 4 option buttons pop up with correct replacement and distractors.
/// </summary>
public class Masters_PolishedCommunication_Reading_LessonThree : Masters_Lesson {

    [System.Serializable]
    public class RegisterCheckQuestion {
        public string situation;      // e.g. "To the Principal:"
        public string spokenLine;     // e.g. "'Hey! What's up?' (too casual)" or "'How is it going with you?' (correct register)"
        public bool isCorrectLine;    // True if spokenLine fits (tap KEEP). False if wrong register (tap FIX).
        public string correctFix;     // e.g. "Hello! How are you?"
        public string[] distractors;  // e.g. new string[] { "Chill", "Shades", "Hey!" }
    }

    [Header("Reading L3 Setup")]
    [SerializeField] private RegisterCheckQuestion[] questions;
    [SerializeField] private TextMeshProUGUI situationTMP;   // Narrating scene/situation
    [SerializeField] private TextMeshProUGUI sentenceTMP;    // Displaying spoken line
    [SerializeField] private Button keepButton;              // Option 1: KEEP
    [SerializeField] private Button fixButton;               // Option 2: FIX
    [SerializeField] private Button[] optionButtons;         // 4 replacement options prompted when FIX is tapped
    [SerializeField] private TextMeshProUGUI progressTMP;
    [SerializeField] private float animationSpeed = 0.35f;
    [SerializeField] private int passThreshold = 5;

    private int currentQuestionIndex = 0;
    private int correctScore = 0;
    private bool canClick = false;
    private bool inPhaseTwo = false;

#if UNITY_EDITOR
    private void Reset() {
        InitializeQuestionsIfEmpty();
        AutoFindUI();
    }

    private void OnValidate() {
        if (questions == null || questions.Length == 0) {
            InitializeQuestionsIfEmpty();
        }
    }
#endif

    protected override void Awake() {
        base.Awake();
        InitializeQuestionsIfEmpty();
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

        // Immediately hide all interactive buttons on frame 0 during initial voiceover
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
            if (foundBtns.Count == 0 && allButtons.Length >= 4) {
                for (int i = 0; i < Mathf.Min(4, allButtons.Length); i++) {
                    if (allButtons[i] != nextButton && allButtons[i] != keepButton && allButtons[i] != fixButton) {
                        foundBtns.Add(allButtons[i]);
                    }
                }
            }
            optionButtons = foundBtns.ToArray();
        }

        TMP_Text[] allTMPs = GetComponentsInChildren<TMP_Text>(true);
        foreach (var tmp in allTMPs) {
            if (tmp == null) continue;
            if (tmp.name.ToLower().Contains("title") || tmp.name.ToLower().Contains("lessontitle")) {
                tmp.gameObject.SetActive(true);
                if (tmp.text.Contains("Hear") || string.IsNullOrWhiteSpace(tmp.text)) {
                    tmp.text = "R03 Spot the Wrong-Register Line";
                }
            } else if (sentenceTMP == null && (tmp.name.ToLower().Contains("sentence") || tmp.name == "TMP" || tmp.text.Contains("Principal"))) {
                sentenceTMP = tmp as TextMeshProUGUI;
            } else if (situationTMP == null && tmp != sentenceTMP && tmp != progressTMP && !tmp.name.ToLower().Contains("title") && (tmp.name.ToLower().Contains("situation") || tmp.name.ToLower().Contains("scene"))) {
                situationTMP = tmp as TextMeshProUGUI;
            }
        }
    }

    private void EnsureTwoStageUIElements() {
        AutoFindUI();

        if (sentenceTMP != null && situationTMP == null) {
            GameObject sitGo = Instantiate(sentenceTMP.gameObject, sentenceTMP.transform.parent);
            sitGo.name = "SituationTMP";
            situationTMP = sitGo.GetComponent<TextMeshProUGUI>();
            if (situationTMP != null) {
                situationTMP.fontSize = Mathf.Min(sentenceTMP.fontSize * 0.9f, 36f);
                situationTMP.color = new Color(1f, 0.9f, 0.3f); // Gold tone for scene narration
                situationTMP.alignment = TextAlignmentOptions.Center;
                RectTransform sitRect = situationTMP.rectTransform;
                if (sitRect != null) {
                    sitRect.anchoredPosition = new Vector2(sitRect.anchoredPosition.x, sitRect.anchoredPosition.y + 110f);
                }
            }
        }

        if (optionButtons != null && optionButtons.Length > 0 && optionButtons[0] != null) {
            if (keepButton == null) {
                GameObject keepGo = Instantiate(optionButtons[0].gameObject, optionButtons[0].transform.parent);
                keepGo.name = "KeepButton";
                keepButton = keepGo.GetComponent<Button>();
                SetButtonText(keepButton, "KEEP (Looks Fine)");
                SetButtonColor(keepButton, new Color(0.3f, 0.5f, 0.8f));
                RectTransform rect = keepButton.GetComponent<RectTransform>();
                if (rect != null) rect.anchoredPosition = new Vector2(-260f, -140f);
            }
            if (fixButton == null) {
                GameObject fixGo = Instantiate(optionButtons[0].gameObject, optionButtons[0].transform.parent);
                fixGo.name = "FixButton";
                fixButton = fixGo.GetComponent<Button>();
                SetButtonText(fixButton, "FIX (Wrong Register)");
                SetButtonColor(fixButton, new Color(0.85f, 0.45f, 0.2f));
                RectTransform rect = fixButton.GetComponent<RectTransform>();
                if (rect != null) rect.anchoredPosition = new Vector2(260f, -140f);
            }
        }
    }

    public void InitializeQuestionsIfEmpty() {
        if (questions != null && questions.Length > 0) return;

        questions = new RegisterCheckQuestion[] {
            new RegisterCheckQuestion { situation = "To the Principal:", spokenLine = "'Hey! What's up?' (too casual)", isCorrectLine = false, correctFix = "Hello! How are you?", distractors = new string[] { "Chill", "Shades", "Hey!" } },
            new RegisterCheckQuestion { situation = "To your best friend at the park:", spokenLine = "'I would like to introduce myself.' (too stiff)", isCorrectLine = false, correctFix = "Hi! I'm... / Hey!", distractors = new string[] { "Nice to meet you, sir.", "How is it going with you?", "Good morning." } },
            new RegisterCheckQuestion { situation = "Meeting a new teacher:", spokenLine = "'How is it going with you?' (correct register)", isCorrectLine = true, correctFix = "", distractors = new string[] { "What's up?", "Take it easy!", "Hey buddy!" } },
            new RegisterCheckQuestion { situation = "Greeting your cousin:", spokenLine = "'I'm delighted to meet you!' (too formal)", isCorrectLine = false, correctFix = "Hi! Nice to meet you!", distractors = new string[] { "I would like to introduce myself.", "Good day to you.", "To understand." } },
            new RegisterCheckQuestion { situation = "To a shopkeeper (stranger):", spokenLine = "'Take it easy!' (correct register)", isCorrectLine = true, correctFix = "", distractors = new string[] { "Buddy", "Shades", "Goof up" } },
            new RegisterCheckQuestion { situation = "Telling a friend you erred:", spokenLine = "'I made a mistake.' (fine but stiff for a buddy)", isCorrectLine = false, correctFix = "I goofed up.", distractors = new string[] { "I would like to introduce myself.", "I beg your pardon.", "How is it going with you?" } }
        };
    }

    private IEnumerator InitializeLessonRoutine() {
        yield return new WaitForSeconds(1.0f);
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

        // Hide Phase 2 replacement options initially
        if (optionButtons != null) {
            foreach (var btn in optionButtons) {
                if (btn != null) btn.gameObject.SetActive(false);
            }
        }

        // Show two textfields: situationTMP (narrating scene) and sentenceTMP (spoken line)
        if (situationTMP != null) {
            situationTMP.text = q.situation;
            situationTMP.transform.DOKill();
            situationTMP.transform.localScale = Vector3.zero;
            situationTMP.gameObject.SetActive(true);
            situationTMP.transform.DOScale(Vector3.one, animationSpeed).SetEase(Ease.OutBack);
        }

        if (sentenceTMP != null) {
            sentenceTMP.text = q.spokenLine;
            sentenceTMP.transform.DOKill();
            sentenceTMP.transform.localScale = Vector3.zero;
            sentenceTMP.gameObject.SetActive(true);
            sentenceTMP.transform.DOScale(Vector3.one, animationSpeed).SetDelay(0.08f).SetEase(Ease.OutBack);
        }

        // Phase 1: Show KEEP and FIX buttons
        if (keepButton != null) {
            keepButton.onClick.RemoveAllListeners();
            keepButton.onClick.AddListener(OnKeepButtonClicked);
            keepButton.transform.DOKill();
            keepButton.transform.localScale = Vector3.zero;
            keepButton.gameObject.SetActive(true);
            keepButton.transform.DOScale(Vector3.one, animationSpeed).SetDelay(0.16f).SetEase(Ease.OutBack);
        }

        if (fixButton != null) {
            fixButton.onClick.RemoveAllListeners();
            fixButton.onClick.AddListener(OnFixButtonClicked);
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
            // Correct! The line is indeed correct (e.g. #3 or #5), so KEEP is the right choice!
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

            StartCoroutine(NextQuestionRoutine());
        } else {
            // Wrong! The line uses the wrong register, so KEEP is incorrect!
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
            // Correct! The student recognized that the line needs fixing!
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

            // Hide Phase 1 KEEP/FIX buttons and prompt Phase 2 replacement option buttons
            if (keepButton != null) keepButton.transform.DOScale(Vector3.zero, 0.25f).OnComplete(() => keepButton.gameObject.SetActive(false));
            if (fixButton != null) fixButton.transform.DOScale(Vector3.zero, 0.25f).OnComplete(() => {
                fixButton.gameObject.SetActive(false);
                RevealReplacementOptions();
            });
        } else {
            // Wrong! The line is ALREADY correct (e.g. #3 or #5), so clicking FIX is incorrect!
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
                if (options.Count < (optionButtons != null ? optionButtons.Length : 4)) {
                    options.Add(dist);
                }
            }
        }

        // Shuffle replacement options across buttons A, B, C, D
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
                        currentBtn.onClick.AddListener(() => OnReplacementOptionSelected(currentBtn, optionTextStr, q.correctFix));

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

    private void OnReplacementOptionSelected(Button selectedBtn, string chosenText, string correctFix) {
        if (!canClick || selectedBtn == null) return;

        if (chosenText == correctFix) {
            canClick = false;
            correctScore++;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            if (selectedBtn.TryGetComponent(out Masters_ButtonPunchAnimator punch)) {
                punch.Punch();
            } else {
                selectedBtn.transform.DOKill();
                selectedBtn.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f);
            }

            StartCoroutine(NextQuestionRoutine());
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            selectedBtn.transform.DOKill(true);
            selectedBtn.transform.DOShakePosition(0.4f, new Vector3(12f, 0, 0));
        }
    }

    private IEnumerator NextQuestionRoutine() {
        yield return new WaitForSeconds(0.7f);
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
        if (Masters_TopicSelectionManager.Instance != null) {
            Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
        }
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }
        if (Masters_LevelManager.Instance != null) {
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
