using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Writing Lesson Two controller for Unit 4: Code of Conduct (Book 2A).
/// W02 Rewrite the Message / 4 Visual Validators Engine:
/// Smartly locates and reuses the existing "Validation Checks" panel from the legacy prefab,
/// displaying relevant keyword requirements directly inside the checkmark lines!
/// </summary>
public class Masters_CodeOfConduct_Writing_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class RuleStep {
        public string ruleTitle;
        [TextArea(3, 6)]
        public string rulePromptAndExample;
        public AudioClip promptAudioClip;
        public string[] keywordOptions;
        public string[] starterChips;
        public int minTotalWords = 3;
        [TextArea]
        public string hintMessage;
    }

    [Header("4 Visual Validators Setup")]
    [SerializeField] private RuleStep[] ruleSteps;

    [Header("UI Binding")]
    [SerializeField] private TextMeshProUGUI sourceMessageTMP;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button checkButton;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Checklist / Visual Validators UI")]
    [SerializeField] private TextMeshProUGUI line1CheckTMP;
    [SerializeField] private TextMeshProUGUI line2CheckTMP;
    [SerializeField] private TextMeshProUGUI line3CheckTMP;
    [SerializeField] private TextMeshProUGUI line4CheckTMP;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color activeColor = new Color(1f, 0.85f, 0.2f); // Gold
    [SerializeField] private Color passColor = Color.green;
    [SerializeField] private Color failColor = Color.red;

    [Header("Starter Chips Binding")]
    [SerializeField] private Button[] starterChipButtons;
    [SerializeField] private TextMeshProUGUI[] starterChipTMPs;

    [Header("Hint Panel UI")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TextMeshProUGUI hintTMP;

    [Header("Input Feedback")]
    [SerializeField] private Image inputFieldBackground;
    [SerializeField] private Color defaultInputBgColor = Color.white;

    private int currentRuleIndex = 0;
    private int failedAttemptsThisRule = 0;
    private bool isCompleted = false;

    public void SetRuleSteps(RuleStep[] steps) {
        ruleSteps = steps;
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Writing;

        if (checkButton != null) {
            checkButton.onClick.RemoveAllListeners();
            checkButton.onClick.AddListener(OnCheckButtonClicked);
        } else {
            AutoFindCheckButton();
        }

        if (inputField != null) {
            inputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
            inputField.onValueChanged.AddListener(OnInputFieldValueChanged);
        } else {
            AutoFindInputField();
        }

        if (nextButton != null) {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }

        AutoFindStarterChips();
    }

    protected override void Start() {
        base.Start();

        EnsureVisualValidatorsExist();

        if (ruleSteps != null && ruleSteps.Length > 0) {
            StartCoroutine(InitializeLessonRoutine());
        } else {
            EndLesson();
        }
    }

    private IEnumerator InitializeLessonRoutine() {
        if (Masters_AudioManager.Instance != null && narratorSpeech != null) {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(narratorSpeech != null ? narratorSpeech.length + 0.5f : 2.5f);
        }

        LoadRuleStep(0);
    }

    private void EnsureVisualValidatorsExist() {
        // Destroy any duplicate/overlapping panel left behind by previous dynamic generation scripts
        Transform dupPanel = FindChildRecursive(transform, "VisualValidatorsPanel");
        if (dupPanel != null) {
            Destroy(dupPanel.gameObject);
        }

        // Search for existing TMP items on the legacy "Validation Checks" box
        List<TextMeshProUGUI> allTMPs = new List<TextMeshProUGUI>(GetComponentsInChildren<TextMeshProUGUI>(true));

        // Update title from "Validation Checks" to "Etiquette Validators" if present
        foreach (TextMeshProUGUI t in allTMPs) {
            if (t != null && (t.text.Contains("Validation Checks") || t.name.ToLower().Contains("title") || t.text.Contains("Etiquette Validators"))) {
                if (t != sourceMessageTMP && t != hintTMP) {
                    t.text = "<b>Etiquette Validators</b>";
                }
            }
        }

        // Try binding line1..line4 if null by searching names or text patterns (Line 1:, Line 2:, etc.)
        if (line1CheckTMP == null) line1CheckTMP = FindTMPByPattern(allTMPs, "line1", "line 1");
        if (line2CheckTMP == null) line2CheckTMP = FindTMPByPattern(allTMPs, "line2", "line 2");
        if (line3CheckTMP == null) line3CheckTMP = FindTMPByPattern(allTMPs, "line3", "line 3");
        if (line4CheckTMP == null) line4CheckTMP = FindTMPByPattern(allTMPs, "line4", "line 4");

        // If line4 is missing on the base prefab but line3 exists, clone line3's row inside the existing box layout!
        if (line4CheckTMP == null && line3CheckTMP != null) {
            Transform parentRow = line3CheckTMP.transform.parent != null ? line3CheckTMP.transform.parent : line3CheckTMP.transform;
            GameObject rowClone = Instantiate(parentRow.gameObject, parentRow.parent);
            rowClone.name = "Line4CheckRow";
            line4CheckTMP = rowClone.GetComponentInChildren<TextMeshProUGUI>(true);
            if (line4CheckTMP == null) line4CheckTMP = rowClone.GetComponent<TextMeshProUGUI>();
        }

        // If even after deep search line1..line3 are null (completely absent UI), fallback to single clean panel
        if (line1CheckTMP == null || line2CheckTMP == null || line3CheckTMP == null || line4CheckTMP == null) {
            CreateDynamicVisualValidatorsPanel();
        }

        // Format all 4 lines with word wrapping so the relevant keywords display cleanly
        for (int i = 0; i <= 3; i++) {
            TextMeshProUGUI tmp = GetCheckTMP(i);
            if (tmp != null) {
                if (ruleSteps != null && i < ruleSteps.Length) {
                    Transform rowTrans = tmp.transform.parent != null ? tmp.transform.parent : tmp.transform;
                    if (rowTrans.gameObject != tmp.gameObject) rowTrans.gameObject.SetActive(true);
                    tmp.gameObject.SetActive(true);
                    tmp.enableWordWrapping = true;
                    if (tmp.fontSize > 18) tmp.fontSize = 18;
                    tmp.text = $"[   ] {ruleSteps[i].ruleTitle}";
                    tmp.color = defaultColor;
                } else {
                    Transform rowTrans = tmp.transform.parent != null ? tmp.transform.parent : tmp.transform;
                    rowTrans.gameObject.SetActive(false);
                }
            }
        }
    }

    private TextMeshProUGUI FindTMPByPattern(List<TextMeshProUGUI> list, string namePattern, string textPattern) {
        foreach (TextMeshProUGUI t in list) {
            if (t == null || t == sourceMessageTMP || t == hintTMP || (starterChipTMPs != null && starterChipTMPs.Contains(t))) continue;
            if (t.name.ToLower().Contains(namePattern) || t.text.ToLower().Contains(textPattern)) {
                return t;
            }
        }
        return null;
    }

    private void CreateDynamicVisualValidatorsPanel() {
        Canvas canvas = GetComponentInChildren<Canvas>(true);
        Transform parentTrans = canvas != null ? canvas.transform : transform;

        GameObject panelObj = new GameObject("VisualValidatorsPanel");
        panelObj.transform.SetParent(parentTrans, false);

        RectTransform rect = panelObj.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.66f, 0.28f);
        rect.anchorMax = new Vector2(0.96f, 0.88f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image panelImg = panelObj.AddComponent<Image>();
        panelImg.color = new Color(0.08f, 0.12f, 0.2f, 0.88f);

        VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(16, 16, 16, 16);
        vlg.spacing = 12;
        vlg.childAlignment = TextAnchor.MiddleLeft;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;

        GameObject titleObj = new GameObject("ValidatorsTitle");
        titleObj.transform.SetParent(panelObj.transform, false);
        TextMeshProUGUI titleTMP = titleObj.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "<b>Etiquette Validators</b>";
        titleTMP.fontSize = 24;
        titleTMP.color = new Color(1f, 0.85f, 0.2f);
        titleTMP.alignment = TextAlignmentOptions.Center;

        line1CheckTMP = CreateValidatorRow(panelObj.transform, "Line1Check");
        line2CheckTMP = CreateValidatorRow(panelObj.transform, "Line2Check");
        line3CheckTMP = CreateValidatorRow(panelObj.transform, "Line3Check");
        line4CheckTMP = CreateValidatorRow(panelObj.transform, "Line4Check");
    }

    private TextMeshProUGUI CreateValidatorRow(Transform parent, string name) {
        GameObject rowObj = new GameObject(name);
        rowObj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = rowObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "[   ] Rule";
        tmp.fontSize = 18;
        tmp.color = defaultColor;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private TextMeshProUGUI GetCheckTMP(int index) {
        if (index == 0) return line1CheckTMP;
        if (index == 1) return line2CheckTMP;
        if (index == 2) return line3CheckTMP;
        if (index == 3) return line4CheckTMP;
        return null;
    }

    private void AutoFindCheckButton() {
        Transform trans = FindChildRecursive(transform, "CheckButton");
        if (trans == null) trans = FindChildRecursive(transform, "SubmitButton");
        if (trans == null) trans = FindChildRecursive(transform, "Button_Check");
        if (trans != null) {
            checkButton = trans.GetComponent<Button>();
            if (checkButton != null) {
                checkButton.onClick.RemoveAllListeners();
                checkButton.onClick.AddListener(OnCheckButtonClicked);
            }
        }
    }

    private void AutoFindInputField() {
        Transform trans = FindChildRecursive(transform, "InputField");
        if (trans == null) trans = FindChildRecursive(transform, "StudentInputField");
        if (trans != null) {
            inputField = trans.GetComponent<TMP_InputField>();
            if (inputField != null) {
                inputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
                inputField.onValueChanged.AddListener(OnInputFieldValueChanged);
                if (inputFieldBackground == null) inputFieldBackground = inputField.GetComponent<Image>();
            }
        }
        if (sourceMessageTMP == null) {
            Transform sourceTrans = FindChildRecursive(transform, "PromptTMP");
            if (sourceTrans == null) sourceTrans = FindChildRecursive(transform, "NPCSpeechBubble");
            if (sourceTrans == null) sourceTrans = FindChildRecursive(transform, "PromptText");
            if (sourceTrans != null) sourceMessageTMP = sourceTrans.GetComponent<TextMeshProUGUI>();
        }
    }

    private void AutoFindStarterChips() {
        Transform chipsContainer = FindChildRecursive(transform, "StarterChips");
        if (chipsContainer == null) chipsContainer = FindChildRecursive(transform, "Chips");
        if (chipsContainer == null) chipsContainer = FindChildRecursive(transform, "StarterChipButtons");

        if (chipsContainer != null && (starterChipButtons == null || starterChipButtons.Length == 0)) {
            List<Button> btns = new List<Button>();
            foreach (Button b in chipsContainer.GetComponentsInChildren<Button>(true)) {
                if (b != nextButton && b != checkButton && !b.name.ToLower().Contains("next") && !b.name.ToLower().Contains("check")) {
                    btns.Add(b);
                }
            }
            if (btns.Count > 0) {
                starterChipButtons = btns.ToArray();
                starterChipTMPs = new TextMeshProUGUI[starterChipButtons.Length];
                for (int i = 0; i < starterChipButtons.Length; i++) {
                    starterChipTMPs[i] = starterChipButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
                    int idx = i;
                    starterChipButtons[i].onClick.RemoveAllListeners();
                    starterChipButtons[i].onClick.AddListener(() => OnStarterChipClicked(idx));
                }
            }
        } else if (starterChipButtons != null) {
            for (int i = 0; i < starterChipButtons.Length; i++) {
                if (starterChipButtons[i] != null) {
                    int idx = i;
                    starterChipButtons[i].onClick.RemoveAllListeners();
                    starterChipButtons[i].onClick.AddListener(() => OnStarterChipClicked(idx));
                }
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string targetName) {
        foreach (Transform child in parent) {
            if (child.name.Equals(targetName, StringComparison.OrdinalIgnoreCase)) {
                return child;
            }
            Transform found = FindChildRecursive(child, targetName);
            if (found != null) return found;
        }
        return null;
    }

    private void LoadRuleStep(int index) {
        if (ruleSteps == null || index >= ruleSteps.Length) return;

        currentRuleIndex = index;
        failedAttemptsThisRule = 0;

        if (hintPanel != null) hintPanel.SetActive(false);

        RuleStep step = ruleSteps[currentRuleIndex];
        if (sourceMessageTMP != null) {
            sourceMessageTMP.text = step.rulePromptAndExample;
            sourceMessageTMP.transform.DOKill();
            sourceMessageTMP.transform.localScale = Vector3.zero;
            sourceMessageTMP.gameObject.SetActive(true);
            sourceMessageTMP.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        if (step.promptAudioClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(step.promptAudioClip);
        }

        if (inputField != null) {
            inputField.interactable = true;
            inputField.text = "";
            inputField.ActivateInputField();
        }

        if (inputFieldBackground != null) {
            inputFieldBackground.color = defaultInputBgColor;
        }

        // Setup Starter Chips if available for this rule
        if (starterChipButtons != null) {
            for (int i = 0; i < starterChipButtons.Length; i++) {
                if (starterChipButtons[i] != null) {
                    if (step.starterChips != null && i < step.starterChips.Length) {
                        starterChipButtons[i].gameObject.SetActive(true);
                        if (starterChipTMPs != null && i < starterChipTMPs.Length && starterChipTMPs[i] != null) {
                            starterChipTMPs[i].text = step.starterChips[i];
                        } else {
                            TMP_Text tmp = starterChipButtons[i].GetComponentInChildren<TMP_Text>(true);
                            if (tmp != null) tmp.text = step.starterChips[i];
                        }
                    } else {
                        starterChipButtons[i].gameObject.SetActive(false);
                    }
                }
            }
        }

        // Highlight active Visual Validator row in gold/bold while keeping completed ones green
        for (int i = 0; i < ruleSteps.Length; i++) {
            TextMeshProUGUI tmp = GetCheckTMP(i);
            if (tmp != null) {
                tmp.enableWordWrapping = true;
                if (tmp.fontSize > 18) tmp.fontSize = 18;

                if (i < currentRuleIndex) {
                    tmp.text = $"<color=#00FF00><b>[ ✓ ] {ruleSteps[i].ruleTitle}</b></color>";
                    tmp.color = passColor;
                } else if (i == currentRuleIndex) {
                    tmp.text = $"<b>[ • ] {ruleSteps[i].ruleTitle}</b>";
                    tmp.color = activeColor;
                    tmp.transform.DOPunchScale(Vector3.one * 0.12f, 0.3f);
                } else {
                    tmp.text = $"[   ] {ruleSteps[i].ruleTitle}";
                    tmp.color = defaultColor;
                }
            }
        }
    }

    private void OnStarterChipClicked(int index) {
        if (isCompleted || inputField == null || ruleSteps == null || currentRuleIndex >= ruleSteps.Length) return;

        RuleStep step = ruleSteps[currentRuleIndex];
        if (step.starterChips != null && index < step.starterChips.Length) {
            string chipText = step.starterChips[index];
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
            }
            if (string.IsNullOrEmpty(inputField.text)) {
                inputField.text = chipText;
            } else {
                inputField.text = inputField.text.TrimEnd() + " " + chipText;
            }
            inputField.caretPosition = inputField.text.Length;
        }
    }

    private void OnInputFieldValueChanged(string newValue) {
        if (inputFieldBackground != null && inputFieldBackground.color == failColor) {
            inputFieldBackground.color = defaultInputBgColor;
        }
    }

    private void OnCheckButtonClicked() {
        if (isCompleted || inputField == null || ruleSteps == null || currentRuleIndex >= ruleSteps.Length) return;

        string rawInput = inputField.text.Trim();
        if (string.IsNullOrEmpty(rawInput)) {
            inputField.transform.DOPunchPosition(Vector3.right * 5f, 0.3f, 10, 1f);
            return;
        }

        RuleStep currentStep = ruleSteps[currentRuleIndex];
        string failReason = "";

        if (ValidateSentence(rawInput, currentStep, out failReason)) {
            // Passed!
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            TextMeshProUGUI checkTmp = GetCheckTMP(currentRuleIndex);
            if (checkTmp != null) {
                checkTmp.text = $"<color=#00FF00><b>[ ✓ ] {currentStep.ruleTitle}</b></color>";
                checkTmp.color = passColor;
                checkTmp.transform.DOKill(true);
                checkTmp.transform.localScale = Vector3.one;
                checkTmp.transform.DOPunchScale(Vector3.one * 0.22f, 0.4f);
            }

            if (inputFieldBackground != null) {
                inputFieldBackground.color = passColor;
            }

            if (hintPanel != null) hintPanel.SetActive(false);

            StartCoroutine(AdvanceToNextRuleRoutine());
        } else {
            // Failed!
            failedAttemptsThisRule++;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            inputField.transform.DOPunchPosition(Vector3.right * 10f, 0.3f, 10, 1f);

            if (inputFieldBackground != null) {
                inputFieldBackground.DOKill();
                inputFieldBackground.DOColor(failColor, 0.2f).OnComplete(() => {
                    inputFieldBackground.DOColor(defaultInputBgColor, 0.3f);
                });
            }

            if (failedAttemptsThisRule >= 1) {
                ShowHint(failReason);
            }
        }
    }

    private void ShowHint(string reason) {
        if (hintPanel != null && hintTMP != null) {
            hintPanel.SetActive(true);
            hintTMP.text = reason;
            hintPanel.transform.DOKill();
            hintPanel.transform.localScale = Vector3.zero;
            hintPanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        }
    }

    private bool ValidateSentence(string rawInput, RuleStep step, out string failReason) {
        failReason = "";

        if (Masters_SentenceValidator.IsProfanity(rawInput)) {
            failReason = "Inappropriate language is not permitted!";
            return false;
        }

        if (!Masters_SentenceValidator.Validate(rawInput, step.keywordOptions, out string dictFeedback)) {
            failReason = dictFeedback;
            return false;
        }

        string[] words = rawInput.Split(new char[] { ' ', '.', ',', '!', '?', '\n', '\r', '\t', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length < step.minTotalWords) {
            failReason = $"Please write a complete sentence with at least {step.minTotalWords} words.";
            return false;
        }

        bool hasKeyword = false;
        if (step.keywordOptions != null && step.keywordOptions.Length > 0) {
            foreach (string kw in step.keywordOptions) {
                if (string.IsNullOrEmpty(kw)) continue;
                if (rawInput.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0) {
                    hasKeyword = true;
                    break;
                }
            }
        } else {
            hasKeyword = true;
        }

        if (!hasKeyword) {
            failReason = $"Your sentence must include one of these expressions: <b>{string.Join(" / ", step.keywordOptions)}</b>.\n\nHint: {step.hintMessage}";
            return false;
        }

        return true;
    }

    private IEnumerator AdvanceToNextRuleRoutine() {
        if (inputField != null) inputField.interactable = false;
        if (checkButton != null) checkButton.interactable = false;

        yield return new WaitForSeconds(1.3f);

        if (currentRuleIndex + 1 < ruleSteps.Length) {
            if (checkButton != null) checkButton.interactable = true;
            LoadRuleStep(currentRuleIndex + 1);
        } else {
            isCompleted = true;
            if (sourceMessageTMP != null) {
                sourceMessageTMP.text = "<b><color=#00FF00>All 4 Etiquette Validators Lit Up!</color></b>\n\nYou mastered polite apologies, gratitude, replies, and praise!";
                sourceMessageTMP.transform.DOPunchScale(Vector3.one * 0.15f, 0.4f);
            }
            if (inputField != null) inputField.gameObject.SetActive(false);
            if (checkButton != null) checkButton.gameObject.SetActive(false);
            if (starterChipButtons != null) {
                foreach (var b in starterChipButtons) if (b != null) b.gameObject.SetActive(false);
            }

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
                NextButtonAnimation();
            } else {
                OnNextButtonClicked();
            }
        }
    }

    private void EndLesson() {
        if (sourceMessageTMP != null) sourceMessageTMP.gameObject.SetActive(false);
        if (inputField != null) inputField.gameObject.SetActive(false);
        if (checkButton != null) checkButton.gameObject.SetActive(false);

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
