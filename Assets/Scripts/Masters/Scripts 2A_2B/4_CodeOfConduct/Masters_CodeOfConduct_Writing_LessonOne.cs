using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Writing Lesson One controller for Unit 4: Code of Conduct (Book 2A).
/// W01 Complete the Etiquette Phrase: 8 situations. Student reads a situation prompt and types
/// the exact verbatim etiquette expression into the input box.
/// Enhanced with Family-Verbatim acceptance so that ANY sentence in the same etiquette family
/// (e.g. "Thank you very much" instead of just "Thanks so much") is accepted as correct!
/// </summary>
public class Masters_CodeOfConduct_Writing_LessonOne : Masters_PolishedCommunication_Writing_LessonOne {

    [Header("Enhanced Local Bindings")]
    [SerializeField] private TextMeshProUGUI localPromptTMP;
    [SerializeField] private TMP_InputField localInputField;
    [SerializeField] private Button localCheckButton;
    [SerializeField] private TextMeshProUGUI localProgressCountTMP;
    [SerializeField] private GameObject localHintPanel;
    [SerializeField] private TextMeshProUGUI localHintTMP;
    [SerializeField] private Image localInputBackground;

    private int myQuestionIndex = 0;
    private bool myLessonComplete = false;

    public void SetQuestions(WritingQuestion[] newQuestions) {
        questions = newQuestions;
    }

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Writing;

        AutoBindLocalUI();

        if (localCheckButton != null) {
            localCheckButton.onClick.RemoveAllListeners();
            localCheckButton.onClick.AddListener(OnFamilyVerbatimCheckClicked);
        }

        if (localInputField != null) {
            localInputField.onValueChanged.AddListener(OnInputChanged);
        }

        if (nextButton != null) {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();

        if (questions != null && questions.Length > 0) {
            StartCoroutine(InitLessonRoutine());
        } else {
            EndMyLesson();
        }
    }

    private void AutoBindLocalUI() {
        Transform trans = FindChildRecursive(transform, "CheckButton");
        if (trans == null) trans = FindChildRecursive(transform, "SubmitButton");
        if (trans != null) localCheckButton = trans.GetComponent<Button>();

        trans = FindChildRecursive(transform, "InputField");
        if (trans == null) trans = FindChildRecursive(transform, "StudentInputField");
        if (trans != null) {
            localInputField = trans.GetComponent<TMP_InputField>();
            if (localInputField != null && localInputBackground == null) {
                localInputBackground = localInputField.GetComponent<Image>();
            }
        }

        trans = FindChildRecursive(transform, "PromptTMP");
        if (trans == null) trans = FindChildRecursive(transform, "NPCSpeechBubble");
        if (trans == null) trans = FindChildRecursive(transform, "PromptText");
        if (trans != null) localPromptTMP = trans.GetComponent<TextMeshProUGUI>();

        trans = FindChildRecursive(transform, "ProgressCountTMP");
        if (trans == null) trans = FindChildRecursive(transform, "ProgressCount");
        if (trans != null) localProgressCountTMP = trans.GetComponent<TextMeshProUGUI>();

        trans = FindChildRecursive(transform, "HintPanel");
        if (trans != null) {
            localHintPanel = trans.gameObject;
            localHintTMP = trans.GetComponentInChildren<TextMeshProUGUI>(true);
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

    private IEnumerator InitLessonRoutine() {
        if (Masters_AudioManager.Instance != null && narratorSpeech != null) {
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd(null);
        } else {
            yield return new WaitForSeconds(narratorSpeech != null ? narratorSpeech.length + 0.5f : 2.0f);
        }

        LoadMyQuestion(0);
    }

    private void LoadMyQuestion(int index) {
        if (questions == null || index >= questions.Length) {
            EndMyLesson();
            return;
        }

        myQuestionIndex = index;
        WritingQuestion q = questions[myQuestionIndex];

        if (localPromptTMP != null) {
            localPromptTMP.text = q.incomingMessageText;
        }

        if (localProgressCountTMP != null) {
            localProgressCountTMP.text = $"{myQuestionIndex + 1}/{questions.Length}";
        }

        if (localInputField != null) {
            localInputField.interactable = true;
            localInputField.text = "";
            localInputField.ActivateInputField();
        }

        if (localInputBackground != null) {
            localInputBackground.color = Color.white;
        }

        if (localHintPanel != null) {
            localHintPanel.SetActive(false);
        }

        if (localCheckButton != null) {
            localCheckButton.interactable = true;
        }
    }

    private void OnInputChanged(string val) {
        if (localInputBackground != null && localInputBackground.color == Color.red) {
            localInputBackground.color = Color.white;
        }
    }

    private void OnFamilyVerbatimCheckClicked() {
        if (myLessonComplete || localInputField == null || questions == null || myQuestionIndex >= questions.Length) return;

        string rawInput = localInputField.text.Trim();
        if (string.IsNullOrEmpty(rawInput)) {
            return;
        }

        WritingQuestion q = questions[myQuestionIndex];

        bool isCorrect = IsFamilyVerbatimMatch(rawInput, q);

        if (isCorrect) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
            if (localInputBackground != null) localInputBackground.color = Color.green;
            if (localInputField != null) localInputField.interactable = false;
            if (localCheckButton != null) localCheckButton.interactable = false;
            if (localHintPanel != null) localHintPanel.SetActive(false);

            StartCoroutine(AdvanceToNextRoutine());
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
            if (localInputBackground != null) {
                localInputBackground.color = Color.red;
            }
            if (localHintPanel != null && localHintTMP != null && !string.IsNullOrEmpty(q.hintText)) {
                localHintTMP.text = q.hintText;
                localHintPanel.SetActive(true);
            }
        }
    }

    private bool IsFamilyVerbatimMatch(string userInput, WritingQuestion question) {
        if (question.acceptableExactMatches == null || question.acceptableExactMatches.Length == 0) return false;

        string cleanInput = StripPunctuationAndSpace(userInput);

        foreach (string exact in question.acceptableExactMatches) {
            if (string.IsNullOrEmpty(exact)) continue;
            string cleanExact = StripPunctuationAndSpace(exact);

            if (cleanInput.Equals(cleanExact, StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
            if (cleanExact.Length >= 4 && cleanInput.Contains(cleanExact)) {
                return true;
            }
            if (cleanInput.Length >= 4 && cleanExact.Contains(cleanInput)) {
                return true;
            }
        }

        // Fallback keyword check if any keywords are assigned
        if (question.requiredKeywords != null && question.requiredKeywords.Length > 0) {
            bool allFound = true;
            foreach (string kw in question.requiredKeywords) {
                if (string.IsNullOrEmpty(kw)) continue;
                string cleanKw = StripPunctuationAndSpace(kw);
                if (!cleanInput.Contains(cleanKw)) {
                    allFound = false;
                    break;
                }
            }
            if (allFound) return true;
        }

        return false;
    }

    private string StripPunctuationAndSpace(string text) {
        if (string.IsNullOrEmpty(text)) return "";
        string lower = text.ToLowerInvariant();
        return System.Text.RegularExpressions.Regex.Replace(lower, @"[^a-z0-9]", "");
    }

    private IEnumerator AdvanceToNextRoutine() {
        yield return new WaitForSeconds(1.3f);
        if (myQuestionIndex + 1 < questions.Length) {
            LoadMyQuestion(myQuestionIndex + 1);
        } else {
            EndMyLesson();
        }
    }

    private void EndMyLesson() {
        myLessonComplete = true;
        if (localInputField != null) localInputField.gameObject.SetActive(false);
        if (localCheckButton != null) localCheckButton.gameObject.SetActive(false);
        if (localPromptTMP != null) {
            localPromptTMP.text = "<b><color=#00FF00>Great Job!</color></b>\n\nYou completed all etiquette expressions!";
        }
        if (nextButton != null) {
            nextButton.gameObject.SetActive(true);
            nextButton.interactable = true;
            NextButtonAnimation();
        }
    }
}
