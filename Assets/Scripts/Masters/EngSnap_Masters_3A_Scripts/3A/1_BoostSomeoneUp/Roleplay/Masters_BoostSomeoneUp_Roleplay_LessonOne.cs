using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Core Game Manager for Unit 3A: Boost Someone Up - Roleplay Lesson One (RP01: On Stage - Cheer the Friend).
/// Standalone Book 3A base controller that simulates a dialogue sequence where an NPC speaks a line and the player picks one of multiple response chips.
/// </summary>
public class Masters_BoostSomeoneUp_Roleplay_LessonOne : Masters_Lesson {

    private const string LOAD_NEXT_ROLEPLAY = "LoadNextRoleplay";

    [System.Serializable]
    public class RoleplayTurn {
        [Header("NPC Settings")]
        [Tooltip("Displays who is speaking inside the options prompt text field.")]
        public string speakerTitle;
        public string npcDialogueText;
        public AudioClip npcAudioClip;
        
        [Header("Player Settings")]
        [Tooltip("The text options displayed on the buttons for the player.")]
        public string[] studentOptions;
        [Tooltip("The index (0, 1, or 2) of the correct option.")]
        public int correctOptionIndex;
        public AudioClip correctOptionAudioClip;
    }

    [Header("Roleplay Data")]
    [SerializeField] protected RoleplayTurn[] roleplayTurns;
    [SerializeField] protected AudioClip wrongOptionAudioClip;
    [SerializeField] protected AudioClip ariaCoachingAudioClip;

    [Header("UI Dialogue Elements")]
    [SerializeField] protected TextMeshProUGUI npcDialogueTMP;
    [SerializeField] protected TextMeshProUGUI studentDialogueTMP;
    [SerializeField] protected GameObject npcCloud;
    [SerializeField] protected GameObject studentCloud;
    [SerializeField] protected GameObject npcAndStudentGameObject;
    
    [Header("UI Options Elements")]
    [SerializeField] protected GameObject optionsContainer;
    [SerializeField] protected GameObject optionsPrompt;
    [SerializeField] protected Button[] optionButtons;
    [SerializeField] protected TextMeshProUGUI[] optionTexts;

    [Header("HUD Elements")]
    [SerializeField] protected TextMeshProUGUI progressCountTMP;
    [SerializeField] protected Button skipButton;
    
    [Header("Animation & Timing")]
    [SerializeField] protected float timeBetweenRoleplay = 1.5f;
    [SerializeField] protected float animationSpeed = 0.5f;
    [SerializeField] protected float timeBetweenEachAnimation = 0.1f;
    [SerializeField] protected RectTransform optionsRectTransform;
    
    [Header("Navigation")]
    [SerializeField] protected Masters_LessonSO nextLessonSO;

    protected int dialogueIndex;
    protected RoleplayTurn currentTurn;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Roleplay;
        
        if (skipButton != null) {
            skipButton.onClick.AddListener(OnSkipButtonClicked);
        }

        if (optionButtons != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                int index = i;
                if (optionButtons[i] != null) {
                    optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
                }
            }
        }
    }

    protected virtual void OnSkipButtonClicked() {
        if (progressCountTMP != null) progressCountTMP.text = $"{++dialogueIndex}/{roleplayTurns.Length}";
        LoadNextRoleplay();
        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
        }
    }

    protected override void Start() {
        // Do not call base.Start() here to prevent immediate overlapping playback.
        
        if (npcCloud != null) npcCloud.SetActive(false);
        if (studentCloud != null) studentCloud.SetActive(false);
        if (optionsContainer != null) optionsContainer.SetActive(false);
        if (optionsPrompt != null) optionsPrompt.SetActive(false);

        LoadNextRoleplay();
        StartCoroutine(StartingAnimationCoroutine());
        StartCoroutine(PlayInitialAudioSequence());
    }

    protected virtual IEnumerator PlayInitialAudioSequence() {
        if (Masters_AudioManager.Instance != null && narratorSpeech != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(narratorSpeech);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd((System.Action)null);
        }
        
        if (roleplayTurns != null && dialogueIndex == 0 && roleplayTurns.Length > 0) {
            RoleplayTurn firstTurn = roleplayTurns[0];
            if (firstTurn.npcAudioClip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(firstTurn.npcAudioClip);
            }
        }
    }

    protected virtual IEnumerator StartingAnimationCoroutine() {
        Vector2 optionsStartPosition = new Vector2(0f, -600f);
        if (optionsRectTransform != null) {
            optionsRectTransform.anchoredPosition = optionsStartPosition;
            Vector2 optionsTargetPosition = new Vector2(0f, -160f);
            optionsRectTransform.DOAnchorPos(optionsTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        }
        yield return null;
    }

    protected virtual void LoadNextRoleplay() {
        if (roleplayTurns == null || dialogueIndex >= roleplayTurns.Length) {
            if (npcAndStudentGameObject != null) {
                npcAndStudentGameObject.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo);
            }
            if (skipButton != null) skipButton.interactable = false;
            
            if (optionsContainer != null) optionsContainer.SetActive(false);
            if (optionsPrompt != null) optionsPrompt.SetActive(false);

            if (nextButton != null) {
                nextButton.interactable = true;
                NextButtonAnimation();
            }
            return;
        }

        if (npcCloud != null) npcCloud.SetActive(false);
        if (studentCloud != null) studentCloud.SetActive(false);
        if (optionsContainer != null) optionsContainer.SetActive(false);
        if (optionsPrompt != null) optionsPrompt.SetActive(false);

        currentTurn = roleplayTurns[dialogueIndex];

        if (npcDialogueTMP != null) npcDialogueTMP.text = currentTurn.npcDialogueText;
        if (npcCloud != null) npcCloud.SetActive(true);
        if (studentDialogueTMP != null) studentDialogueTMP.text = "";

        if (optionsContainer != null) optionsContainer.SetActive(true);
        if (optionsPrompt != null) {
            optionsPrompt.SetActive(true);
            TextMeshProUGUI promptTMP = optionsPrompt.GetComponent<TextMeshProUGUI>();
            if (promptTMP == null) promptTMP = optionsPrompt.GetComponentInChildren<TextMeshProUGUI>();
            if (promptTMP != null && !string.IsNullOrEmpty(currentTurn.speakerTitle)) {
                promptTMP.text = currentTurn.speakerTitle;
            }
        }
        
        if (optionButtons != null && currentTurn.studentOptions != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] == null) continue;
                if (i < currentTurn.studentOptions.Length) {
                    optionButtons[i].gameObject.SetActive(true);
                    if (optionTexts != null && i < optionTexts.Length && optionTexts[i] != null) {
                        optionTexts[i].text = currentTurn.studentOptions[i];
                    }
                } else {
                    optionButtons[i].gameObject.SetActive(false);
                }
            }
        }

        if (currentTurn.npcAudioClip != null && Masters_AudioManager.Instance != null && dialogueIndex > 0) {
            Masters_AudioManager.Instance.PlayVoiceOver(currentTurn.npcAudioClip);
        }
    }

    protected virtual void OnOptionSelected(int selectedIndex) {
        if (optionsContainer != null) optionsContainer.SetActive(false);
        if (optionsPrompt != null) optionsPrompt.SetActive(false);

        if (currentTurn != null && selectedIndex == currentTurn.correctOptionIndex) {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }
            
            float delay = timeBetweenRoleplay;
            if (currentTurn.correctOptionAudioClip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(currentTurn.correctOptionAudioClip);
                delay += currentTurn.correctOptionAudioClip.length;
            }
            
            if (progressCountTMP != null) {
                progressCountTMP.text = $"{++dialogueIndex}/{roleplayTurns.Length}";
            } else {
                dialogueIndex++;
            }
            
            if (studentDialogueTMP != null && currentTurn.studentOptions != null && selectedIndex < currentTurn.studentOptions.Length) {
                studentDialogueTMP.text = currentTurn.studentOptions[selectedIndex];
            }
            if (studentCloud != null) studentCloud.SetActive(true);

            Invoke(LOAD_NEXT_ROLEPLAY, delay);
        } else {
            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                if (wrongOptionAudioClip != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(wrongOptionAudioClip);
                }
            }
            if (optionsContainer != null) optionsContainer.SetActive(true); 
            if (optionsPrompt != null) optionsPrompt.SetActive(true);
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
