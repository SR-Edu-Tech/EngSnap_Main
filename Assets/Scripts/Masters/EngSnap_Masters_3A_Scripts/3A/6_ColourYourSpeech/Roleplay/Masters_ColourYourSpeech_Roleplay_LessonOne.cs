using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

/// <summary>
/// Controller for Unit 6: Colour Your Speech - RP01 (On Stage: The Bananas & Fruitcake Chats).
/// Inherits from Masters_BoostSomeoneUp_Roleplay_LessonOne.
/// Features 2 scenes (4 steps), verbatim book dialogue, Aria idiom notes, and Hub progression tracking.
/// </summary>
public class Masters_ColourYourSpeech_Roleplay_LessonOne : Masters_BoostSomeoneUp_Roleplay_LessonOne {

    [Header("Unit 6 RP01 Scoring & Progression")]
    [Tooltip("Minimum correct first-attempt steps required to award RP01 completion (3 out of 4).")]
    [SerializeField] private int passThreshold = 3;

    [Header("Unit 6 RP01 Title & UI Elements")]
    [SerializeField] protected TextMeshProUGUI lessonTitleTMP;

    [Header("Unit 6 RP01 Audio & Transitions")]
    [Tooltip("Curtain / Scene transition SFX.")]
    [SerializeField] private AudioClip sfxCurtain;

    [Tooltip("Aria idiom note audio clips corresponding to conversation steps.")]
    [SerializeField] private AudioClip[] ariaIdiomNotes;

    // Internal scoring & attempt tracking
    private int firstAttemptCorrectCount = 0;
    private bool hasAttemptedCurrentStep = false;
    private bool isStepAdvancing = false;

    protected override void Awake() {
        base.Awake();
        topic = Masters_Topic.Roleplay;

        EnsureUIBindings();
    }

    private void EnsureUIBindings() {
        if (lessonTitleTMP == null) {
            var texts = GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in texts) {
                string n = t.gameObject.name.ToLower();
                if (n.Contains("title") || n.Contains("lessontitle")) {
                    lessonTitleTMP = t;
                    break;
                }
            }
        }
    }

    protected override void Start() {
        firstAttemptCorrectCount = 0;
        hasAttemptedCurrentStep = false;
        isStepAdvancing = false;

        EnsureUIBindings();

        if (lessonTitleTMP != null) {
            lessonTitleTMP.text = "On Stage — The Bananas & Fruitcake Chats";
        }

        base.Start();

        if (progressCountTMP != null && roleplayTurns != null && roleplayTurns.Length > 0) {
            progressCountTMP.text = $"1/{roleplayTurns.Length}";
        }
    }

    protected override void LoadNextRoleplay() {
        hasAttemptedCurrentStep = false;
        isStepAdvancing = false;

        // Scene transition between Step 2 (Turn 1) and Step 3 (Turn 2)
        if (dialogueIndex == 2 && sfxCurtain != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlaySoundEffect(sfxCurtain);
        }

        base.LoadNextRoleplay();

        // Check if all steps completed
        if (roleplayTurns != null && dialogueIndex >= roleplayTurns.Length) {
            if (firstAttemptCorrectCount >= passThreshold) {
                M3A_U6_HubProgress.MarkRP01Complete();
            }
        }
    }

    protected override void OnOptionSelected(int selectedIndex) {
        if (isStepAdvancing) return;

        if (currentTurn != null && selectedIndex == currentTurn.correctOptionIndex) {
            isStepAdvancing = true;

            // Award point only if answered correctly on the first attempt
            if (!hasAttemptedCurrentStep) {
                firstAttemptCorrectCount++;
            }

            if (optionsContainer != null) optionsContainer.SetActive(false);
            if (optionsPrompt != null) optionsPrompt.SetActive(false);

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Correct);
            }

            float delay = timeBetweenRoleplay;
            if (currentTurn.correctOptionAudioClip != null && Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlayVoiceOver(currentTurn.correctOptionAudioClip);
                delay += currentTurn.correctOptionAudioClip.length;
            }

            if (progressCountTMP != null && roleplayTurns != null) {
                progressCountTMP.text = $"{dialogueIndex + 1}/{roleplayTurns.Length}";
            }

            if (studentDialogueTMP != null && currentTurn.studentOptions != null && selectedIndex < currentTurn.studentOptions.Length) {
                studentDialogueTMP.text = currentTurn.studentOptions[selectedIndex];
            }
            if (studentCloud != null) studentCloud.SetActive(true);

            // Optional Aria idiom note playback
            if (ariaIdiomNotes != null && dialogueIndex < ariaIdiomNotes.Length && ariaIdiomNotes[dialogueIndex] != null) {
                StartCoroutine(PlayAriaNoteAndAdvance(ariaIdiomNotes[dialogueIndex], delay));
            } else {
                dialogueIndex++;
                Invoke("LoadNextRoleplay", delay);
            }
        } else {
            hasAttemptedCurrentStep = true;

            if (Masters_AudioManager.Instance != null) {
                Masters_AudioManager.Instance.PlaySoundEffect(Masters_SFX.Incorrect);
                if (wrongOptionAudioClip != null) {
                    Masters_AudioManager.Instance.PlayVoiceOver(wrongOptionAudioClip);
                }
            }

            // Keep choices active for retry
            if (optionsContainer != null) optionsContainer.SetActive(true);
            if (optionsPrompt != null) optionsPrompt.SetActive(true);
        }
    }

    private IEnumerator PlayAriaNoteAndAdvance(AudioClip ariaClip, float initialDelay) {
        yield return new WaitForSeconds(initialDelay);

        if (ariaClip != null && Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.PlayVoiceOver(ariaClip);
            yield return Masters_AudioManager.Instance.WaitForVoiceOverEnd((System.Action)null);
        }

        dialogueIndex++;
        LoadNextRoleplay();
    }

    protected override void OnNextButtonClicked() {
        if (topic == Masters_Topic.None) topic = Masters_Topic.Roleplay;

        if (Masters_AudioManager.Instance != null) {
            Masters_AudioManager.Instance.StopVoiceOver();
        }

        if (firstAttemptCorrectCount >= passThreshold) {
            M3A_U6_HubProgress.MarkRP01Complete();
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

    private void OnDestroy() {
        CancelInvoke();
        StopAllCoroutines();
    }
}
