using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Writing Lesson 2 for Unit 11 Is There a Difference?
/// Implements step-by-step single rule sentence validation with anti-gibberish and structural checks.
/// </summary>
public class Masters_IsThereADifference_Writing_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class RuleStep {
        public string ruleTitle;
        [TextArea(3, 6)]
        public string rulePromptAndExample;
        public string[] keywordOptions;
        public string forbiddenKeyword;
        public int minWordsAfterKeyword;
        [TextArea]
        public string hintMessage;
    }

    [SerializeField] private RuleStep[] ruleSteps;

    [Header("UI Binding")]
    [SerializeField] private TextMeshProUGUI sourceMessageTMP;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button checkButton;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Checklist Feedback UI")]
    [SerializeField] private TextMeshProUGUI line1CheckTMP;
    [SerializeField] private TextMeshProUGUI line2CheckTMP;
    [SerializeField] private TextMeshProUGUI line3CheckTMP;
    [SerializeField] private TextMeshProUGUI line4CheckTMP;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color passColor = Color.green;
    [SerializeField] private Color failColor = Color.red;

    [Header("Hint Panel UI")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TextMeshProUGUI hintTMP;

    [Header("Input Feedback")]
    [SerializeField] private Image inputFieldBackground;
    [SerializeField] private Color defaultInputBgColor = Color.white;

    private int currentRuleIndex = 0;
    private int failedAttemptsThisRule = 0;
    private bool isCompleted = false;

    protected override void Awake() {
        base.Awake();
        if (checkButton != null) checkButton.onClick.AddListener(OnCheckButtonClicked);
        if (inputField != null) {
            inputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
        }
        if (nextButton != null) {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();

        // Safely spawn a 4th checklist slot if needed
        if (ruleSteps != null && ruleSteps.Length > 3 && line4CheckTMP == null && line3CheckTMP != null && line3CheckTMP.transform.parent != null) {
            Transform parentRow = line3CheckTMP.transform.parent;
            GameObject rowClone = Instantiate(parentRow.gameObject, parentRow.parent);
            rowClone.name = "Line4CheckRow";
            line4CheckTMP = rowClone.GetComponentInChildren<TextMeshProUGUI>();
        }

        for (int i = 0; i <= 3; i++) {
            TextMeshProUGUI tmp = GetCheckTMP(i);
            if (tmp != null) {
                GameObject rowObj = tmp.transform.parent != null ? tmp.transform.parent.gameObject : tmp.gameObject;
                if (ruleSteps != null && i < ruleSteps.Length) {
                    rowObj.SetActive(true);
                    tmp.text = ruleSteps[i].ruleTitle;
                    tmp.color = defaultColor;
                } else {
                    rowObj.SetActive(false);
                }
            }
        }

        if (ruleSteps != null && ruleSteps.Length > 0) {
            LoadRuleStep(0);
        }
    }

    private TextMeshProUGUI GetCheckTMP(int index) {
        if (index == 0) return line1CheckTMP;
        if (index == 1) return line2CheckTMP;
        if (index == 2) return line3CheckTMP;
        if (index == 3) return line4CheckTMP;
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
        }

        if (inputField != null) {
            inputField.interactable = true;
            inputField.text = "";
            inputField.ActivateInputField();
        }

        if (inputFieldBackground != null) {
            inputFieldBackground.color = defaultInputBgColor;
        }

        // Highlight current checklist item slightly or reset
        for (int i = 0; i < ruleSteps.Length; i++) {
            TextMeshProUGUI tmp = GetCheckTMP(i);
            if (tmp != null && i >= currentRuleIndex) {
                tmp.color = defaultColor;
            }
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
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            TextMeshProUGUI checkTmp = GetCheckTMP(currentRuleIndex);
            if (checkTmp != null) {
                checkTmp.color = passColor;
                checkTmp.transform.DOPunchScale(Vector3.one * 0.2f, 0.4f);
            }

            if (inputFieldBackground != null) {
                inputFieldBackground.color = passColor;
            }

            if (hintPanel != null) hintPanel.SetActive(false);

            StartCoroutine(AdvanceToNextRuleRoutine());
        } else {
            // Failed!
            failedAttemptsThisRule++;
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
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

    private IEnumerator AdvanceToNextRuleRoutine() {
        if (inputField != null) inputField.interactable = false;
        yield return new WaitForSeconds(1.2f);

        if (currentRuleIndex + 1 < ruleSteps.Length) {
            LoadRuleStep(currentRuleIndex + 1);
        } else {
            // Completed all 4 rules!
            isCompleted = true;
            if (sourceMessageTMP != null) {
                sourceMessageTMP.text = "<b><color=#55FF55>Excellent job!</color></b>\n\nYou successfully wrote clean sentences demonstrating all 4 grammar rules!";
            }
            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }
            NextButtonAnimation();
        }
    }

    protected virtual bool ValidateSentence(string rawInput, RuleStep step, out string failReason) {
        failReason = "";
        
        // 1. Run dictionary & keyword heuristic check against words.txt
        if (!Masters_SentenceValidator.Validate(rawInput, step.keywordOptions, out string dictFeedback)) {
            failReason = dictFeedback;
            return false;
        }

        string[] words = rawInput.Split(new char[] { ' ', '.', ',', '!', '?', '\n', '\r', '\t', ';', ':' }, StringSplitOptions.RemoveEmptyEntries);

        if (words.Length < 4) {
            failReason = "Please write a complete sentence (at least 4 words).";
            return false;
        }

        // Anti-gibberish checks
        foreach (string w in words) {
            // Check for flood of repeating characters (e.g., bbbbb or xxxx)
            for (int i = 0; i < w.Length - 2; i++) {
                if (char.IsLetter(w[i]) && w[i] == w[i + 1] && w[i] == w[i + 2]) {
                    failReason = $"Please write natural English words (avoid repeating characters like '{w}').";
                    return false;
                }
            }

            // Check if longer words contain at least one vowel/y or digits/common abbreviations
            if (w.Length >= 3) {
                bool hasVowelOrDigit = false;
                foreach (char c in w.ToLowerInvariant()) {
                    if ("aeiouy0123456789".Contains(c)) {
                        hasVowelOrDigit = true;
                        break;
                    }
                }
                if (!hasVowelOrDigit) {
                    failReason = $"The word '{w}' does not appear to be a valid English word.";
                    return false;
                }
            }
        }

        // Check for forbidden keyword
        if (!string.IsNullOrEmpty(step.forbiddenKeyword)) {
            foreach (string w in words) {
                if (w.Equals(step.forbiddenKeyword, StringComparison.OrdinalIgnoreCase) || w.Equals(step.forbiddenKeyword + "ed", StringComparison.OrdinalIgnoreCase)) {
                    failReason = $"Remember the rule: please use '{step.keywordOptions[0]}' instead of '{step.forbiddenKeyword}'.";
                    return false;
                }
            }
        }

        // Check for required keyword presence and position
        int keywordIdx = -1;
        foreach (string kw in step.keywordOptions) {
            for (int i = 0; i < words.Length; i++) {
                if (words[i].Equals(kw, StringComparison.OrdinalIgnoreCase)) {
                    keywordIdx = i;
                    break;
                }
            }
            if (keywordIdx != -1) break;
        }

        if (keywordIdx == -1) {
            failReason = $"Your sentence must include the target word: <b>{string.Join(" / ", step.keywordOptions)}</b>.\n\nHint: {step.hintMessage}";
            return false;
        }

        // Check structural words after keyword
        int wordsAfter = words.Length - 1 - keywordIdx;
        if (wordsAfter < step.minWordsAfterKeyword) {
            failReason = $"Please add more details after '{words[keywordIdx]}' to complete the structure.\n\nHint: {step.hintMessage}";
            return false;
        }

        return true;
    }

    private void ShowHint(string customMessage) {
        if (hintPanel != null) {
            hintPanel.SetActive(true);
            hintPanel.transform.DOKill();
            hintPanel.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f);
        }
        if (hintTMP != null) {
            hintTMP.text = customMessage;
        }
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        if (nextLessonSO != null) {
            Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
        } else {
            if (topic != Masters_Topic.None) {
                Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
            }
            Masters_LevelManager.Instance.OnLessonComplete(topic);
        }
    }
}
