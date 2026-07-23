using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Masters_WordSwitch_RolePlay_LessonOne : Masters_StartingConversationWithAStranger_Roleplay_LessonOne {

    [System.Serializable]
    public class SubstitutedBeat {
        public string completeSentenceText;
        public AudioClip completeSentenceClip;
    }

    [SerializeField] protected SubstitutedBeat[] substitutedBeats;

    protected override void Start() {
        base.Start();

        if (npcCloud != null) npcCloud.SetActive(false);
        if (studentCloud != null) studentCloud.SetActive(false);
        if (optionsContainer != null) optionsContainer.SetActive(false);
        if (narratorContainer != null) narratorContainer.SetActive(false);
        if (optionsPrompt != null) optionsPrompt.SetActive(false);

        StartCoroutine(Masters_AudioManager.Instance.WaitForVoiceOverEnd(LoadNextRoleplay));
    }

    protected override void LoadNextRoleplay() {
        if (roleplayTurns == null || dialogueIndex >= roleplayTurns.Length) {
            if (npcAndStudentGameObject != null) npcAndStudentGameObject.transform.DOScale(Vector2.zero, 0.5f).SetEase(Ease.OutExpo);
            if (skipButton != null) skipButton.interactable = false;
            if (optionsContainer != null) optionsContainer.SetActive(false);
            if (narratorContainer != null) narratorContainer.SetActive(false);
            if (optionsPrompt != null) optionsPrompt.SetActive(false);

            if (nextButton != null) {
                nextButton.gameObject.SetActive(true);
                nextButton.interactable = true;
            }
            NextButtonAnimation();
            return;
        }

        if (npcCloud != null) npcCloud.SetActive(false);
        if (studentCloud != null) studentCloud.SetActive(false);
        if (optionsContainer != null) optionsContainer.SetActive(false);
        if (optionsPrompt != null) optionsPrompt.SetActive(false);

        currentTurn = roleplayTurns[dialogueIndex];

        // Show silent narrator prompt
        if (narratorContainer != null) narratorContainer.SetActive(true);
        if (narratorTMP != null) narratorTMP.text = currentTurn.npcDialogueText;

        if (studentDialogueTMP != null) studentDialogueTMP.text = "";

        if (optionsContainer != null) optionsContainer.SetActive(true);
        if (optionsPrompt != null) optionsPrompt.SetActive(true);

        if (optionButtons != null && currentTurn.studentOptions != null) {
            for (int i = 0; i < optionButtons.Length; i++) {
                if (optionButtons[i] != null) {
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
        }

        // Silent prompt rule: NO narrator audio is played here!
    }

    protected override void OnOptionSelected(int selectedIndex) {
        if (optionsContainer != null) optionsContainer.SetActive(false);
        if (optionsPrompt != null) optionsPrompt.SetActive(false);

        if (currentTurn != null && selectedIndex == currentTurn.correctOptionIndex) {
            Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);

            float delay = 2.0f;
            if (substitutedBeats != null && dialogueIndex < substitutedBeats.Length && substitutedBeats[dialogueIndex] != null) {
                SubstitutedBeat beat = substitutedBeats[dialogueIndex];
                
                // Display new complete sentence
                if (narratorTMP != null) narratorTMP.text = beat.completeSentenceText;
                if (studentDialogueTMP != null) studentDialogueTMP.text = currentTurn.studentOptions[selectedIndex];
                if (studentCloud != null) studentCloud.SetActive(true);

                if (beat.completeSentenceClip != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(beat.completeSentenceClip);
                    delay = beat.completeSentenceClip.length + 1.0f;
                }
            }

            dialogueIndex++;
            if (progressCountTMP != null) progressCountTMP.text = $"{dialogueIndex}/{roleplayTurns.Length}";

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
