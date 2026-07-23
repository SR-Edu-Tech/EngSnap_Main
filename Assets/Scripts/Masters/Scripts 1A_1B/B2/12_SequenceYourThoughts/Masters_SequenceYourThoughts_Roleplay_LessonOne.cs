using UnityEngine;

/// <summary>
/// Roleplay Lesson 1 for Unit 12 Sequence Your Thoughts.
/// Inherits from StartingConversationWithAStranger_Roleplay_LessonOne.
/// Displays complete sentences and plays student audio upon correct answer without narrator.
/// </summary>
public class Masters_SequenceYourThoughts_Roleplay_LessonOne : Masters_StartingConversationWithAStranger_Roleplay_LessonOne {

    [Header("Unit 12 Complete Sentences Text")]
    public string[] completeSentences;

    protected override void Start() {
        base.Start();
        if (narratorContainer != null) narratorContainer.SetActive(false);
    }

    protected override void OnOptionSelected(int selectedIndex) {
        if (optionsContainer != null) optionsContainer.SetActive(false);
        if (optionsPrompt != null) optionsPrompt.SetActive(false);

        if (currentTurn != null && selectedIndex == currentTurn.correctOptionIndex) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

            float delay = timeBetweenRoleplay;
            if (currentTurn.correctOptionAudioClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(currentTurn.correctOptionAudioClip);
                delay += currentTurn.correctOptionAudioClip.length;
            }

            if (progressCountTMP != null) progressCountTMP.text = $"{++dialogueIndex}/{roleplayTurns.Length}";

            string textToShow = (completeSentences != null && (dialogueIndex - 1) >= 0 && (dialogueIndex - 1) < completeSentences.Length && !string.IsNullOrEmpty(completeSentences[dialogueIndex - 1]))
                ? completeSentences[dialogueIndex - 1]
                : currentTurn.studentOptions[selectedIndex];

            if (studentDialogueTMP != null) studentDialogueTMP.text = textToShow;
            if (studentCloud != null) studentCloud.SetActive(true);

            Invoke("LoadNextRoleplay", delay);
        } else {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
            if (wrongOptionAudioClip != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(wrongOptionAudioClip);
            }
            if (optionsContainer != null) optionsContainer.SetActive(true);
            if (optionsPrompt != null) optionsPrompt.SetActive(true);
        }
    }
}
