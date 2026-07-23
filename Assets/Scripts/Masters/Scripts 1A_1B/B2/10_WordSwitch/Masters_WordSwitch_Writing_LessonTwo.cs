using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_WordSwitch_Writing_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public class ParagraphLineRequirement {
        public string checklistLabel;
        public string[] acceptedSynonyms;
        public string requiredContextPhrase; // e.g. "morning was"
        [TextArea] public string hintMessage;
    }

    [TextArea(3, 6)]
    [SerializeField] private string displayedSourceParagraph;
    [SerializeField] private ParagraphLineRequirement[] lineRequirements;

    [Header("UI Binding")]
    [SerializeField] private TextMeshProUGUI sourceMessageTMP;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button checkButton;
    [SerializeField] private Masters_LessonSO nextLessonSO;

    [Header("Checklist Feedback UI")]
    [SerializeField] private TextMeshProUGUI line1CheckTMP;
    [SerializeField] private TextMeshProUGUI line2CheckTMP;
    [SerializeField] private TextMeshProUGUI line3CheckTMP;
    [SerializeField] private Color defaultColor = Color.white;
    [SerializeField] private Color passColor = Color.green;
    [SerializeField] private Color failColor = Color.red;

    [Header("Hint Panel UI")]
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TextMeshProUGUI hintTMP;

    [Header("Input Feedback")]
    [SerializeField] private Image inputFieldBackground;
    [SerializeField] private Color defaultInputBgColor = Color.white;

    private int failedAttempts = 0;
    private bool isCompleted = false;

    protected override void Awake() {
        base.Awake();
        if (checkButton != null) checkButton.onClick.AddListener(OnCheckButtonClicked);
        if (nextButton != null) {
            nextButton.interactable = false;
            nextButton.gameObject.SetActive(false);
        }
    }

    protected override void Start() {
        base.Start();
        if (sourceMessageTMP != null) sourceMessageTMP.text = displayedSourceParagraph;

        if (inputField != null) {
            inputField.lineType = TMP_InputField.LineType.MultiLineSubmit;
            if (inputField.textComponent != null) {
                inputField.textComponent.enableAutoSizing = false;
                inputField.textComponent.enableWordWrapping = true;
            }
        }

        if (lineRequirements != null && lineRequirements.Length >= 3) {
            if (line1CheckTMP != null) line1CheckTMP.text = lineRequirements[0].checklistLabel;
            if (line2CheckTMP != null) line2CheckTMP.text = lineRequirements[1].checklistLabel;
            if (line3CheckTMP != null) line3CheckTMP.text = lineRequirements[2].checklistLabel;
        }

        ResetChecklistUI();
        if (hintPanel != null) hintPanel.SetActive(false);
    }

    private void ResetChecklistUI() {
        if (line1CheckTMP != null) line1CheckTMP.color = defaultColor;
        if (line2CheckTMP != null) line2CheckTMP.color = defaultColor;
        if (line3CheckTMP != null) line3CheckTMP.color = defaultColor;
    }

    private TextMeshProUGUI GetCheckTMP(int index) {
        if (index == 0) return line1CheckTMP;
        if (index == 1) return line2CheckTMP;
        if (index == 2) return line3CheckTMP;
        return null;
    }

    private void OnCheckButtonClicked() {
        if (isCompleted || inputField == null || lineRequirements == null) return;

        string rawInput = inputField.text.Trim();
        if (string.IsNullOrEmpty(rawInput)) {
            inputField.transform.DOPunchPosition(Vector3.right * 5f, 0.3f, 10, 1f);
            return;
        }

        string normalizedInput = rawInput.ToLowerInvariant().Replace(".", " ").Replace("!", " ").Replace("?", " ").Replace(",", " ");

        bool allPassed = true;
        int firstFailedIndex = -1;

        for (int i = 0; i < lineRequirements.Length; i++) {
            var req = lineRequirements[i];
            bool linePass = false;

            if (req.acceptedSynonyms != null) {
                foreach (string syn in req.acceptedSynonyms) {
                    if (string.IsNullOrEmpty(syn)) continue;
                    string synClean = syn.ToLowerInvariant().Trim();

                    // Verify both context phrase presence and synonym presence
                    if (!string.IsNullOrEmpty(req.requiredContextPhrase)) {
                        string ctxClean = req.requiredContextPhrase.ToLowerInvariant().Trim();
                        if (normalizedInput.Contains(ctxClean) && normalizedInput.Contains(synClean)) {
                            linePass = true;
                            break;
                        }
                    } else if (normalizedInput.Contains(synClean)) {
                        linePass = true;
                        break;
                    }
                }
            }

            TextMeshProUGUI tmp = GetCheckTMP(i);
            if (linePass) {
                if (tmp != null) tmp.color = passColor;
            } else {
                allPassed = false;
                if (firstFailedIndex == -1) firstFailedIndex = i;
                if (tmp != null) {
                    tmp.color = failColor;
                    tmp.transform.DOPunchPosition(Vector3.right * 4f, 0.3f, 10, 1f);
                }
            }
        }

        if (allPassed) {
            isCompleted = true;
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            if (inputFieldBackground != null) inputFieldBackground.color = passColor;
            if (hintPanel != null) hintPanel.SetActive(false);

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }
            NextButtonAnimation();
        } else {
            failedAttempts++;
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            inputField.transform.DOPunchPosition(Vector3.right * 10f, 0.3f, 10, 1f);

            if (inputFieldBackground != null) {
                inputFieldBackground.DOKill();
                inputFieldBackground.DOColor(failColor, 0.2f).OnComplete(() => {
                    inputFieldBackground.DOColor(defaultInputBgColor, 0.3f);
                });
            }

            if (failedAttempts >= 1 && firstFailedIndex != -1) {
                ShowHint(firstFailedIndex);
            }
        }
    }

    private void ShowHint(int index) {
        if (hintPanel != null) {
            hintPanel.SetActive(true);
            hintPanel.transform.DOKill();
            hintPanel.transform.DOPunchScale(Vector3.one * 0.1f, 0.3f);
        }
        if (hintTMP != null && index < lineRequirements.Length) {
            hintTMP.text = lineRequirements[index].hintMessage;
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
