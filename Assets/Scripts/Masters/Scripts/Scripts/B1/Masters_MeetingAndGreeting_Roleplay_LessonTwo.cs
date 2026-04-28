using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Masters_MeetingAndGreeting_Roleplay_LessonTwo : Masters_Lesson {


    [SerializeField]
    private Masters_RoleplayGoodbyeCard[] roleplayGoodbyeCardArray;
    [SerializeField]
    private int numberOfSuccessfulDetectionsToComplete;


    private int numberOfSuccesfulDetections;


    protected override void Awake() {
        base.Awake();

        foreach(Masters_RoleplayGoodbyeCard roleplayGoodbyeCard in roleplayGoodbyeCardArray) {
            roleplayGoodbyeCard.OnSuccessfulDetection += RoleplayGoodbyeCard_OnSuccessfulDetection;
        }
    }

    private void RoleplayGoodbyeCard_OnSuccessfulDetection(object sender, System.EventArgs e) {
        numberOfSuccesfulDetections++;

        if(numberOfSuccesfulDetections == numberOfSuccessfulDetectionsToComplete) {
            // Over
            nextButton.interactable = true;
        }
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
