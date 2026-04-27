using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Masters_MeetingAndGreeting_Roleplay_LessonOne : Masters_Lesson {


    private const string LOAD_NEXT_ROLEPLAY = "LoadNextRoleplay";


    [System.Serializable]
    public class RoleplayDialogues {

        public string dialogueButtonText;
        public string dialogueDetectionText;
        public AudioClip dialogueAudioClip;

    }


    [SerializeField]
    private RoleplayDialogues[] npcRoleplayDialogueArray;
    [SerializeField]
    private RoleplayDialogues[] studentRoleplayDialogueArray;
    [SerializeField]
    private TextMeshProUGUI npcDialogueTMP;
    [SerializeField]
    private TextMeshProUGUI studentDialogueTMP;
    [SerializeField]
    private TextMeshProUGUI micPromptTMP;
    [SerializeField]
    private float timeBetweenRoleplay;
    [SerializeField]
    private Masters_LessonSO nextLessonSO;


    private int dialogueIndex;
    private RoleplayDialogues currentNpcRoleplayDialogue;
    private RoleplayDialogues currentStudentRoleplayDialogue;


    protected override void Awake() {
        base.Awake();
    }

    private void Start() {
        LoadNextRoleplay();
    }

    private void OnEnable() {
        CrossPlatformSpeechManager.OnResultStatic += OnSpeechResult;
    }

    private void OnDisable() {
        CrossPlatformSpeechManager.OnResultStatic -= OnSpeechResult;
    }

    private void OnSpeechResult(string spokenText) {
        string spoken = spokenText.ToLower().Trim();
        Debug.Log($"Spoken: {spoken}");

        if (spoken == currentStudentRoleplayDialogue.dialogueDetectionText) {
            // Correct
            studentDialogueTMP.text = currentStudentRoleplayDialogue.dialogueButtonText;
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            Invoke(LOAD_NEXT_ROLEPLAY, timeBetweenRoleplay);
        } else {
            // Wrong
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
        }
    }

    private void LoadNextRoleplay() {
        if(dialogueIndex == studentRoleplayDialogueArray.Length) {
            // Over
            nextButton.interactable = true;
            return;
        }

        currentNpcRoleplayDialogue = npcRoleplayDialogueArray[dialogueIndex];
        currentStudentRoleplayDialogue = studentRoleplayDialogueArray[dialogueIndex++];

        npcDialogueTMP.text = currentNpcRoleplayDialogue.dialogueButtonText;
        studentDialogueTMP.text = "";
        micPromptTMP.text = $"Talk into the mic: {currentStudentRoleplayDialogue.dialogueButtonText}";
        Masters_AudioManager.Instance.PlayVoiceOver(currentNpcRoleplayDialogue.dialogueAudioClip);
    }

    protected override void OnNextButtonClicked() {
        Masters_AudioManager.Instance.StopVoiceOver();
        Masters_LevelManager.Instance.LoadLessonToLessonCanvas(nextLessonSO);
    }


}
