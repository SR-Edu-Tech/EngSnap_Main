using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Core controller for Unit 1: Polished Communication - Writing Lesson Two (W02: Rewrite the Message / Change the Tone).
/// Standalone implementation independent of older book scripts.
/// Manages starter chip insertion, typewriter NPC dialogue, keyword input validation, and progression.
/// </summary>
public class Masters_PolishedCommunication_Writing_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public struct WritingPrompt {
        public string npcOfferText;
        public AudioClip npcOfferAudioClip;
        public string[] starterChipsText;
        public string[] validKeywords;
    }

    [Header("Writing Prompts")]
    [SerializeField]
    protected WritingPrompt[] writingPromptArray;

    [Header("Time Delay")]
    [SerializeField]
    private float timeBeforeFirstPrompt = 1f;
    [SerializeField]
    private float timeBetweenPrompts = 2f;

    [Header("Game UI Reference")]
    [SerializeField]
    private TextMeshProUGUI npcSpeechBubbleTMP;
    [SerializeField]
    private Masters_TextTypeWriter npcSpeechTypeWriter;
    [SerializeField]
    private TMP_InputField studentInputField;
    [SerializeField]
    private Image studentInputFieldBackgroundImage;
    [SerializeField]
    private Button[] starterChipButtons;
    [SerializeField]
    private TextMeshProUGUI[] starterChipTMPs;
    [SerializeField]
    private Button submitButton;

    [Header("Color")]
    [SerializeField]
    private Color defaultInputColor = Color.white;
    [SerializeField]
    private Color correctInputColor = Color.green;
    [SerializeField]
    private Color incorrectInputColor = Color.red;

    [Header("Polished Communication Routing")]
    [SerializeField]
    private Masters_LessonSO nextLessonSO;

    private int promptIndex = 0;
    private WritingPrompt currentPrompt;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Writing;

        if (submitButton != null) {
            submitButton.onClick.RemoveAllListeners();
            submitButton.onClick.AddListener(OnSubmitClicked);
        }

        if (starterChipButtons != null) {
            for (int i = 0; i < starterChipButtons.Length; i++) {
                int idx = i;
                if (starterChipButtons[idx] != null) {
                    starterChipButtons[idx].onClick.RemoveAllListeners();
                    starterChipButtons[idx].onClick.AddListener(() => OnChipClicked(idx));
                }
            }
        }

        if (studentInputField != null) {
            studentInputField.onValueChanged.RemoveAllListeners();
            studentInputField.onValueChanged.AddListener(OnInputFieldValueChanged);
        }
    }

    protected override void Start() {
        base.Start();
        if (studentInputField != null) studentInputField.interactable = false;
        if (submitButton != null) submitButton.interactable = false;

        if (starterChipButtons != null) {
            foreach (var btn in starterChipButtons) {
                if (btn != null) btn.gameObject.SetActive(false);
            }
        }

        Invoke(nameof(StartFirstPrompt), timeBeforeFirstPrompt);
    }

    private void StartFirstPrompt() {
        promptIndex = 0;
        LoadPrompt(promptIndex);
    }

    private void LoadPrompt(int index) {
        if (writingPromptArray == null || index < 0 || index >= writingPromptArray.Length) {
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            return;
        }

        currentPrompt = writingPromptArray[index];

        if (studentInputFieldBackgroundImage != null) studentInputFieldBackgroundImage.color = defaultInputColor;
        if (studentInputField != null) {
            studentInputField.text = "";
            studentInputField.interactable = true;
            studentInputField.Select();
            studentInputField.ActivateInputField();
        }

        if (submitButton != null) submitButton.interactable = true;

        // Setup starter chips
        if (starterChipButtons != null && starterChipTMPs != null) {
            for (int i = 0; i < starterChipButtons.Length; i++) {
                if (starterChipButtons[i] == null) continue;

                if (currentPrompt.starterChipsText != null && i < currentPrompt.starterChipsText.Length && !string.IsNullOrEmpty(currentPrompt.starterChipsText[i])) {
                    if (starterChipTMPs.Length > i && starterChipTMPs[i] != null) {
                        starterChipTMPs[i].text = currentPrompt.starterChipsText[i];
                    }
                    starterChipButtons[i].gameObject.SetActive(true);
                } else {
                    starterChipButtons[i].gameObject.SetActive(false);
                }
            }
        }

        // Display NPC speech
        if (npcSpeechBubbleTMP != null) {
            npcSpeechBubbleTMP.text = currentPrompt.npcOfferText;
            if (npcSpeechTypeWriter != null) {
                npcSpeechTypeWriter.TriggerAnimation(currentPrompt.npcOfferText.Length);
            }
        }

        if (currentPrompt.npcOfferAudioClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(currentPrompt.npcOfferAudioClip);
        }
    }

    private void OnChipClicked(int chipIndex) {
        if (currentPrompt.starterChipsText == null || chipIndex < 0 || chipIndex >= currentPrompt.starterChipsText.Length || studentInputField == null) return;

        string chipText = currentPrompt.starterChipsText[chipIndex];
        if (string.IsNullOrEmpty(chipText)) return;

        if (string.IsNullOrEmpty(studentInputField.text)) {
            studentInputField.text = chipText + " ";
        } else {
            studentInputField.text += " " + chipText + " ";
        }

        studentInputField.caretPosition = studentInputField.text.Length;
        studentInputField.Select();
        studentInputField.ActivateInputField();
    }

    private void OnInputFieldValueChanged(string newText) {
        if (studentInputFieldBackgroundImage != null && studentInputFieldBackgroundImage.color == incorrectInputColor) {
            studentInputFieldBackgroundImage.color = defaultInputColor;
        }
    }

    private void OnSubmitClicked() {
        if (studentInputField == null || currentPrompt.validKeywords == null) return;

        string userInput = studentInputField.text.ToLowerInvariant().Trim();
        if (string.IsNullOrEmpty(userInput)) return;

        bool isCorrect = true;
        foreach (var keyword in currentPrompt.validKeywords) {
            if (!string.IsNullOrEmpty(keyword) && !userInput.Contains(keyword.ToLowerInvariant().Trim())) {
                isCorrect = false;
                break;
            }
        }

        if (isCorrect) {
            if (studentInputFieldBackgroundImage != null) studentInputFieldBackgroundImage.color = correctInputColor;
            if (studentInputField != null) studentInputField.interactable = false;
            if (submitButton != null) submitButton.interactable = false;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            Invoke(nameof(LoadNextPrompt), timeBetweenPrompts);
        } else {
            if (studentInputFieldBackgroundImage != null) studentInputFieldBackgroundImage.color = incorrectInputColor;
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            }
        }
    }

    private void LoadNextPrompt() {
        promptIndex++;
        if (writingPromptArray != null && promptIndex < writingPromptArray.Length) {
            LoadPrompt(promptIndex);
        } else {
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
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
}
