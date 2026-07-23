using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Game Manager for Tricky Three - Roleplay Lesson 1.
/// Simulates a dialogue sequence where an NPC asks a question and the player picks one of three multiple-choice responses.
/// </summary>
public class Masters_TrickyThree_Roleplay_LessonOne : Masters_Lesson {

    private const string LOAD_NEXT_ROLEPLAY = "LoadNextRoleplay";

    [System.Serializable]
    public class RoleplayTurn {
        [Header("NPC Settings")]
        public string npcDialogueText;
        public AudioClip npcAudioClip;
        
        [Header("Player Settings")]
        [Tooltip("The 3 text options displayed on the buttons for the player.")]
        public string[] studentOptions;
        [Tooltip("The index (0, 1, or 2) of the correct option.")]
        public int correctOptionIndex;
        public AudioClip correctOptionAudioClip;
    }

    [Header("Roleplay Data")]
    [SerializeField] private RoleplayTurn[] roleplayTurns;
    [SerializeField] private AudioClip wrongOptionAudioClip;

    [Header("UI Dialogue Elements")]
    [SerializeField] private TextMeshProUGUI npcDialogueTMP;
    [SerializeField] private TextMeshProUGUI studentDialogueTMP;
    [SerializeField] private GameObject npcCloud;
    [SerializeField] private GameObject studentCloud;
    [SerializeField] private GameObject npcAndStudentGameObject;
    
    [Header("UI Options Elements")]
    [SerializeField] private GameObject optionsContainer;
    [SerializeField] private GameObject optionsPrompt;
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TextMeshProUGUI[] optionTexts;

    [Header("HUD Elements")]
    [SerializeField] private TextMeshProUGUI progressCountTMP;
    [SerializeField] private Button skipButton;
    
    [Header("Animation & Timing")]
    [SerializeField] private float timeBetweenRoleplay;
    [SerializeField] private float animationSpeed;
    [SerializeField] private float timeBetweenEachAnimation;
    [SerializeField] private RectTransform optionsRectTransform;
    
    [Header("Navigation")]
    [SerializeField] private Masters_LessonSO nextLessonSO;

    private int dialogueIndex;
    private RoleplayTurn currentTurn;

    protected override void Awake() {
        base.Awake();
        
        if (skipButton != null) {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }

        // Bind the 3 option buttons to their respective index
        for (int i = 0; i < optionButtons.Length; i++) {
            int index = i;
            if (optionButtons[i] != null) {
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
            }
        }
    }

    private void OnSkipButtonClicked() {
        progressCountTMP.text = $"{++dialogueIndex}/{roleplayTurns.Length}";
        LoadNextRoleplay();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    protected override void Start() {
        base.Start();

        // Hide UI initially
        npcCloud.SetActive(false);
        studentCloud.SetActive(false);
        optionsContainer.SetActive(false);
        if (optionsPrompt != null) optionsPrompt.SetActive(false);

        // Wait for the introductory voiceover to finish before showing the first question
        StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(LoadNextRoleplay));
        StartCoroutine(StartingAnimationCoroutine());
    }

    /// <summary>
    /// Slides the options panel up from the bottom of the screen smoothly.
    /// </summary>
    private IEnumerator StartingAnimationCoroutine() {
        Vector2 optionsStartPosition = new Vector2(0f, -600f);
        if (optionsRectTransform != null) {
            optionsRectTransform.anchoredPosition = optionsStartPosition;
            Vector2 optionsTargetPosition = new Vector2(0f, -160f);
            optionsRectTransform.DOAnchorPos(optionsTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        }
        yield return null;
    }

    /// <summary>
    /// Loads the next turn in the roleplay array. Ends the lesson if all turns are complete.
    /// </summary>
    private void LoadNextRoleplay() {
        if (dialogueIndex >= roleplayTurns.Length) {
            // Roleplay sequence is complete
            npcAndStudentGameObject.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo);
            if (skipButton != null) skipButton.interactable = false;
            
            optionsContainer.SetActive(false);
            if (optionsPrompt != null) optionsPrompt.SetActive(false);

            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            return;
        }

        // Reset UI for the new question
        npcCloud.SetActive(false);
        studentCloud.SetActive(false);
        optionsContainer.SetActive(false);
        if (optionsPrompt != null) optionsPrompt.SetActive(false);

        currentTurn = roleplayTurns[dialogueIndex];

        // Setup NPC Dialogue
        npcDialogueTMP.text = currentTurn.npcDialogueText;
        npcCloud.SetActive(true);
        studentDialogueTMP.text = "";

        // Show Multiple Choice Options
        optionsContainer.SetActive(true);
        if (optionsPrompt != null) optionsPrompt.SetActive(true);
        
        for (int i = 0; i < optionButtons.Length; i++) {
            if (i < currentTurn.studentOptions.Length) {
                optionButtons[i].gameObject.SetActive(true);
                optionTexts[i].text = currentTurn.studentOptions[i];
            } else {
                optionButtons[i].gameObject.SetActive(false);
            }
        }

        // Play the NPC's audio question
        if (currentTurn.npcAudioClip != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(currentTurn.npcAudioClip);
        }
    }

    /// <summary>
    /// Validates the player's button selection against the correct option index.
    /// </summary>
    private void OnOptionSelected(int selectedIndex) {
        optionsContainer.SetActive(false);
        if (optionsPrompt != null) optionsPrompt.SetActive(false);

        if (selectedIndex == currentTurn.correctOptionIndex) {
            // Correct Answer Logic
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            
            float delay = timeBetweenRoleplay;
            if (currentTurn.correctOptionAudioClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(currentTurn.correctOptionAudioClip);
                delay += currentTurn.correctOptionAudioClip.length;
            }
            
            // Update HUD and show player's text bubble
            if (progressCountTMP != null) {
                progressCountTMP.text = $"{++dialogueIndex}/{roleplayTurns.Length}";
            } else {
                dialogueIndex++;
            }
            
            studentDialogueTMP.text = currentTurn.studentOptions[selectedIndex];
            studentCloud.SetActive(true);

            // Wait before loading the next interaction
            Invoke(LOAD_NEXT_ROLEPLAY, delay);
        } else {
            // Wrong Answer Logic
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            if (wrongOptionAudioClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(wrongOptionAudioClip);
            }
            // Re-show the options so the player can try again
            optionsContainer.SetActive(true); 
            if (optionsPrompt != null) optionsPrompt.SetActive(true);
        }
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        
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
