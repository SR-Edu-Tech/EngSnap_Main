using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_StartingConversationWithAStranger_Roleplay_LessonOne : Masters_Lesson {

    private const string LOAD_NEXT_ROLEPLAY = "LoadNextRoleplay";

    [System.Serializable]
    public class RoleplayTurn {
        public bool isNarrator;
        public string npcDialogueText;
        public AudioClip npcAudioClip;
        public string[] studentOptions;
        public int correctOptionIndex;
        public AudioClip correctOptionAudioClip;
    }

    [SerializeField]
    protected RoleplayTurn[] roleplayTurns;

    [SerializeField]
    protected AudioClip wrongOptionAudioClip;

    [SerializeField]
    protected TextMeshProUGUI npcDialogueTMP;
    [SerializeField]
    protected TextMeshProUGUI studentDialogueTMP;
    [SerializeField]
    protected GameObject npcCloud, studentCloud, npcAndStudentGameObject;
    [SerializeField]
    protected GameObject narratorContainer;
    [SerializeField]
    protected TextMeshProUGUI narratorTMP;
    
    [SerializeField]
    protected GameObject optionsContainer;
    [SerializeField]
    protected GameObject optionsPrompt;
    [SerializeField]
    protected Button[] optionButtons;
    [SerializeField]
    protected TextMeshProUGUI[] optionTexts;

    [SerializeField]
    protected TextMeshProUGUI progressCountTMP;
    
    [SerializeField]
    protected float timeBetweenRoleplay;
    [SerializeField]
    protected Masters_LessonSO nextLessonSO;
    [SerializeField]
    protected Button skipButton;
    [SerializeField]
    protected float animationSpeed, timeBetweenEachAnimation;
    
    [SerializeField]
    protected RectTransform optionsRectTransform;

    protected int dialogueIndex;
    protected RoleplayTurn currentTurn;
    protected Vector2 initialOptionsPosition;

    protected override void Awake() {
        base.Awake();
        if (skipButton != null) skipButton.onClick.AddListener(OnSkipButtonClicked);

        if (optionsRectTransform != null) {
            initialOptionsPosition = optionsRectTransform.anchoredPosition;
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

    private void OnSkipButtonClicked() {
        if (progressCountTMP != null) progressCountTMP.text = $"{++dialogueIndex}/{roleplayTurns.Length}";
        LoadNextRoleplay();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    protected override void Start() {
        base.Start();

        if (npcCloud != null) npcCloud.SetActive(false);
        if (studentCloud != null) studentCloud.SetActive(false);
        if (optionsContainer != null) optionsContainer.SetActive(false);
        if (narratorContainer != null) narratorContainer.SetActive(false);
        if (optionsPrompt != null) optionsPrompt.SetActive(false);

        StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(LoadNextRoleplay));
        StartCoroutine(StartingAnimationCoroutine());
    }

    private IEnumerator StartingAnimationCoroutine() {
        if (optionsRectTransform != null) {
            Vector2 optionsStartPosition = new Vector2(initialOptionsPosition.x, initialOptionsPosition.y - 440f);
            optionsRectTransform.anchoredPosition = optionsStartPosition;
            optionsRectTransform.DOAnchorPos(initialOptionsPosition, animationSpeed).SetEase(Ease.OutExpo);
        }
        yield return null;
    }

    protected virtual void LoadNextRoleplay() {
        if (dialogueIndex >= roleplayTurns.Length) {
            // Over
            if (npcAndStudentGameObject != null) npcAndStudentGameObject.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo);
            if (skipButton != null) skipButton.interactable = false;
            if (optionsContainer != null) optionsContainer.SetActive(false);
            if (narratorContainer != null) narratorContainer.SetActive(false);
            if (optionsPrompt != null) optionsPrompt.SetActive(false);

            if (nextButton != null) nextButton.interactable = true;
            NextButtonAnimation();
            return;
        }

        if (npcCloud != null) npcCloud.SetActive(false);
        if (studentCloud != null) studentCloud.SetActive(false);
        if (optionsContainer != null) optionsContainer.SetActive(false);
        if (narratorContainer != null) narratorContainer.SetActive(false);
        if (optionsPrompt != null) optionsPrompt.SetActive(false);

        currentTurn = roleplayTurns[dialogueIndex];

        if (currentTurn.isNarrator) {
            if (narratorContainer != null) narratorContainer.SetActive(true);
            if (narratorTMP != null) narratorTMP.text = currentTurn.npcDialogueText;
        } else {
            if (npcCloud != null) npcCloud.SetActive(true);
            if (npcDialogueTMP != null) npcDialogueTMP.text = currentTurn.npcDialogueText;
        }
        
        if (studentDialogueTMP != null) studentDialogueTMP.text = "";

        if (optionsContainer != null) optionsContainer.SetActive(true);
        if (optionsPrompt != null) optionsPrompt.SetActive(true);
        
        for (int i = 0; i < optionButtons.Length; i++) {
            if (i < currentTurn.studentOptions.Length) {
                optionButtons[i].gameObject.SetActive(true);
                optionTexts[i].text = currentTurn.studentOptions[i];
            } else {
                optionButtons[i].gameObject.SetActive(false);
            }
        }

        Masters_AudioManager.Instance.PlayVoiceOver(currentTurn.npcAudioClip);
    }

    protected virtual void OnOptionSelected(int selectedIndex) {
        if (optionsContainer != null) optionsContainer.SetActive(false);
        if (optionsPrompt != null) optionsPrompt.SetActive(false);

        if (selectedIndex == currentTurn.correctOptionIndex) {
            // Correct
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            
            float delay = timeBetweenRoleplay;
            if (currentTurn.correctOptionAudioClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(currentTurn.correctOptionAudioClip);
                delay += currentTurn.correctOptionAudioClip.length;
            }
            
            if (progressCountTMP != null) progressCountTMP.text = $"{++dialogueIndex}/{roleplayTurns.Length}";
            if (studentDialogueTMP != null) studentDialogueTMP.text = currentTurn.studentOptions[selectedIndex];
            if (studentCloud != null) studentCloud.SetActive(true);

            Invoke(LOAD_NEXT_ROLEPLAY, delay);
        } else {
            // Wrong
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            if (wrongOptionAudioClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(wrongOptionAudioClip);
            }
            // Re-show the options so they can try again
            if (optionsContainer != null) optionsContainer.SetActive(true); 
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


