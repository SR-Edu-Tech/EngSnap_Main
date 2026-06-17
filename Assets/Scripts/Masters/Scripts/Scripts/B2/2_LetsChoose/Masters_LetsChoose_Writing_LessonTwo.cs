using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class Masters_LetsChoose_Writing_LessonTwo : Masters_Lesson {

    [System.Serializable]
    public struct WritingPrompt {
        public string npcOfferText;
        public AudioClip npcOfferAudioClip;
        public string[] starterChipsText;
        public string[] validKeywords;
    }

    [Header("Writing Lesson Settings")]
    [SerializeField] private WritingPrompt[] writingPromptArray;
    [SerializeField] private float timeBeforeFirstPrompt = 1f;
    [SerializeField] private float timeBetweenPrompts = 2f;

    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI npcSpeechBubbleTMP;
    [SerializeField] private Masters_TextTypeWriter npcSpeechTypeWriter;
    [SerializeField] private TMP_InputField studentInputField;
    [SerializeField] private Image studentInputFieldBackgroundImage;
    
    [Header("Starter Chips")]
    [SerializeField] private Button[] starterChipButtons;
    [SerializeField] private TextMeshProUGUI[] starterChipTMPs;

    [Header("Actions")]
    [SerializeField] private Button submitButton;

    [Header("Feedback Colors")]
    [SerializeField] private Color defaultInputColor = Color.white;
    [SerializeField] private Color correctInputColor = Color.green;
    [SerializeField] private Color incorrectInputColor = Color.red;

    private int currentPromptIndex = 0;
    private bool isCheckingAnswer = false;

    protected override void Awake() {
        base.Awake();

        if (submitButton != null) {
            submitButton.onClick.AddListener(OnSubmitClicked);
        }

        for (int i = 0; i < starterChipButtons.Length; i++) {
            int index = i; // local copy for closure
            if (starterChipButtons[index] != null) {
                starterChipButtons[index].onClick.AddListener(() => OnChipClicked(index));
            }
        }
        
        // Ensure student can type and it clears red/green color when they type
        if (studentInputField != null) {
            studentInputField.onValueChanged.AddListener(OnInputFieldValueChanged);
        }
    }

    protected override void Start() {
        base.Start();

        // Start Lesson Flow
        Invoke(nameof(StartFirstPrompt), timeBeforeFirstPrompt);
    }
    
    private void StartFirstPrompt() {
        LoadPrompt(currentPromptIndex);
    }

    private void LoadPrompt(int index) {
        if (index >= writingPromptArray.Length) {
            // Lesson Over
            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            return;
        }

        isCheckingAnswer = false;
        WritingPrompt prompt = writingPromptArray[index];

        // Reset UI
        if (studentInputField != null) {
            studentInputField.text = "";
            studentInputField.interactable = true;
        }
        if (studentInputFieldBackgroundImage != null) {
            studentInputFieldBackgroundImage.color = defaultInputColor;
        }
        if (submitButton != null) {
            submitButton.interactable = true;
        }

        // Set NPC Text and Audio
        if (npcSpeechBubbleTMP != null) {
            npcSpeechBubbleTMP.text = prompt.npcOfferText;
            if (npcSpeechTypeWriter != null) {
                npcSpeechTypeWriter.TriggerAnimation(prompt.npcOfferText.Length);
            }
        }

        if (prompt.npcOfferAudioClip != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(prompt.npcOfferAudioClip);
        }

        // Set up Starter Chips
        for (int i = 0; i < starterChipButtons.Length; i++) {
            if (i < prompt.starterChipsText.Length) {
                starterChipButtons[i].gameObject.SetActive(true);
                if (starterChipTMPs[i] != null) {
                    starterChipTMPs[i].text = prompt.starterChipsText[i];
                }
            } else {
                starterChipButtons[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnChipClicked(int chipIndex) {
        if (isCheckingAnswer || studentInputField == null) return;
        
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        // Append text (with a space if it's not empty)
        string chipText = starterChipTMPs[chipIndex].text;
        
        if (string.IsNullOrEmpty(studentInputField.text)) {
            studentInputField.text = chipText;
        } else {
            // Append with a space
            studentInputField.text += " " + chipText;
        }
        
        // Move caret to the end so they can continue typing
        studentInputField.caretPosition = studentInputField.text.Length;
    }

    private void OnInputFieldValueChanged(string newValue) {
        // If they start typing after getting it wrong, reset the color
        if (studentInputFieldBackgroundImage != null && studentInputFieldBackgroundImage.color == incorrectInputColor) {
            studentInputFieldBackgroundImage.color = defaultInputColor;
        }
    }

    private void OnSubmitClicked() {
        if (isCheckingAnswer) return;

        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);

        string playerInput = studentInputField.text.ToLower().Trim();
        if (string.IsNullOrEmpty(playerInput)) {
            // Don't accept empty
            return;
        }

        WritingPrompt currentPrompt = writingPromptArray[currentPromptIndex];
        bool isValid = false;
        string feedback = "";

        // Check if player just used the chip
        bool justUsedChip = false;
        foreach (string chipText in currentPrompt.starterChipsText) {
            if (playerInput == chipText.ToLower().Trim()) {
                justUsedChip = true;
                break;
            }
        }

        if (!justUsedChip) {
            isValid = Masters_SentenceValidator.Validate(studentInputField.text, currentPrompt.validKeywords, out feedback);
        }

        if (isValid) {
            isCheckingAnswer = true;
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            
            if (studentInputFieldBackgroundImage != null) {
                studentInputFieldBackgroundImage.color = correctInputColor;
            }
            if (studentInputField != null) {
                studentInputField.interactable = false;
            }
            if (submitButton != null) {
                submitButton.interactable = false;
            }

            // Move to next prompt
            currentPromptIndex++;
            Invoke(nameof(LoadNextPrompt), timeBetweenPrompts);

        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            
            if (studentInputFieldBackgroundImage != null) {
                studentInputFieldBackgroundImage.color = incorrectInputColor;
                // Shake the input field to show error
                studentInputFieldBackgroundImage.transform.DOShakePosition(0.5f, 10f, 10, 90f, false, true);
            }
        }
    }

    private void LoadNextPrompt() {
        LoadPrompt(currentPromptIndex);
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) {
            Debug.Log($"Topic not set for {this.name}!");
            return;
        }
        Masters_TopicSelectionManager.Instance.UnlockButton((Masters_Topic)((int)topic + 1));
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.OnLessonComplete(topic);
    }
}

