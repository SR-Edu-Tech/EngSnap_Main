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
    private RoleplayTurn[] roleplayTurns;

    [SerializeField]
    private AudioClip wrongOptionAudioClip;

    [SerializeField]
    private TextMeshProUGUI npcDialogueTMP;
    [SerializeField]
    private TextMeshProUGUI studentDialogueTMP;
    [SerializeField]
    private GameObject npcCloud, studentCloud, npcAndStudentGameObject;
    [SerializeField]
    private GameObject narratorContainer;
    [SerializeField]
    private TextMeshProUGUI narratorTMP;
    
    [SerializeField]
    private GameObject optionsContainer;
    [SerializeField]
    private GameObject optionsPrompt;
    [SerializeField]
    private Button[] optionButtons;
    [SerializeField]
    private TextMeshProUGUI[] optionTexts;

    [SerializeField]
    private TextMeshProUGUI progressCountTMP;
    
    [SerializeField]
    private float timeBetweenRoleplay;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;
    [SerializeField]
    private Button skipButton;
    [SerializeField]
    private float animationSpeed, timeBetweenEachAnimation;
    
    [SerializeField]
    private RectTransform optionsRectTransform;

    private int dialogueIndex;
    private RoleplayTurn currentTurn;

    protected override void Awake() {
        base.Awake();
        skipButton.onClick.AddListener(OnSkipButtonClicked);

        for (int i = 0; i < optionButtons.Length; i++) {
            int index = i;
            optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
        }
    }

    private void OnSkipButtonClicked() {
        progressCountTMP.text = $"{++dialogueIndex}/{roleplayTurns.Length}";
        LoadNextRoleplay();
        Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.SelectPositive);
    }

    protected override void Start() {
        base.Start();

        npcCloud.SetActive(false);
        studentCloud.SetActive(false);
        optionsContainer.SetActive(false);
        if (narratorContainer != null) narratorContainer.SetActive(false);
        if (optionsPrompt != null) optionsPrompt.SetActive(false);

        StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(LoadNextRoleplay));
        StartCoroutine(StartingAnimationCoroutine());
    }

    private IEnumerator StartingAnimationCoroutine() {
        Vector2 optionsStartPosition = new Vector2(0f, -600f);
        if (optionsRectTransform != null) {
            optionsRectTransform.anchoredPosition = optionsStartPosition;
            Vector2 optionsTargetPosition = new Vector2(0f, -160f);
            optionsRectTransform.DOAnchorPos(optionsTargetPosition, animationSpeed).SetEase(Ease.OutExpo);
        }
        yield return null;
    }

    private void LoadNextRoleplay() {
        if (dialogueIndex >= roleplayTurns.Length) {
            // Over
            npcAndStudentGameObject.transform.DOScale(Vector2.zero, animationSpeed).SetEase(Ease.OutExpo);
            skipButton.interactable = false;
            optionsContainer.SetActive(false);
            if (narratorContainer != null) narratorContainer.SetActive(false);
            if (optionsPrompt != null) optionsPrompt.SetActive(false);

            nextButton.interactable = true;
            NextButtonAnimation();
            return;
        }

        npcCloud.SetActive(false);
        studentCloud.SetActive(false);
        optionsContainer.SetActive(false);
        if (narratorContainer != null) narratorContainer.SetActive(false);
        if (optionsPrompt != null) optionsPrompt.SetActive(false);

        currentTurn = roleplayTurns[dialogueIndex];

        if (currentTurn.isNarrator) {
            if (narratorContainer != null) narratorContainer.SetActive(true);
            if (narratorTMP != null) narratorTMP.text = currentTurn.npcDialogueText;
        } else {
            npcCloud.SetActive(true);
            npcDialogueTMP.text = currentTurn.npcDialogueText;
        }
        
        studentDialogueTMP.text = "";

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

        Masters_AudioManager.Instance.PlayVoiceOver(currentTurn.npcAudioClip);
    }

    private void OnOptionSelected(int selectedIndex) {
        optionsContainer.SetActive(false);
        if (optionsPrompt != null) optionsPrompt.SetActive(false);

        if (selectedIndex == currentTurn.correctOptionIndex) {
            // Correct
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            
            float delay = timeBetweenRoleplay;
            if (currentTurn.correctOptionAudioClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(currentTurn.correctOptionAudioClip);
                delay += currentTurn.correctOptionAudioClip.length;
            }
            
            progressCountTMP.text = $"{++dialogueIndex}/{roleplayTurns.Length}";
            studentDialogueTMP.text = currentTurn.studentOptions[selectedIndex];
            studentCloud.SetActive(true);

            Invoke(LOAD_NEXT_ROLEPLAY, delay);
        } else {
            // Wrong
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            if (wrongOptionAudioClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(wrongOptionAudioClip);
            }
            // Re-show the options so they can try again
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


