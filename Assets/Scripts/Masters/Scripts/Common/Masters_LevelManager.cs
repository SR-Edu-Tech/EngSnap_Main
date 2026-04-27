using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Masters_Unit {
    // Book One - 15 Units
    None,
    MeetingAndGreeting,
    SelfIntroduction,
    MyLearningHub,
    YouAreInvited,
    YeahAndNah,
    WowCompliments,
    IDoAndIMake,
    AnnounceAndRespondToUnfortunateNews,
    GoalsAndPlans,
    AreYouConfused,
    TongueTwisters,
    LetMeQuestion,
    SituationalDialogues,
    CorrectPronunciation,
    JustAMinuteSession
}

public enum Masters_Topic {
    None,
    Intro,
    Listening,
    Reading,
    Writing,
    Speaking,
    Game,
    Roleplay,
    Quiz,
    Rewards
}

public enum Masters_CurrentScreen {
    None,
    UnitSelection,
    TopicSelection,
    Lesson
}

public class Masters_LevelManager : Masters_Singleton<Masters_LevelManager> {


    [SerializeField]
    private GameObject unitSelectionGameObject;
    [SerializeField]
    private GameObject topicSelectionGameObject;
    [SerializeField]
    private GameObject lessonCanvasGameObject;
    [SerializeField]
    private Masters_LessonSO[] lessonSOArray;
    [SerializeField]
    private Masters_TopicButton[] topicButtonArray;
    [SerializeField]
    private AudioClip selectAUnitAudioClip, selectATopicAudioClip;


    private Masters_CurrentScreen currentScreen;
    private Masters_Unit currentlySelectedUnit;
    private Masters_Topic currentlySelectedTopic;
    private GameObject currentLessonGameObject;
    private int numberOfTopicCompleted;


    protected override void Awake() {
        base.Awake();

        //ChangeToUnitSelectionScreen();
        Application.targetFrameRate = 120;
    }

    public void OnUnitButtonClicked(Masters_Unit selectedUnit) {
        // Store selected unit
        currentlySelectedUnit = selectedUnit;
        Debug.Log($"Selected Unit: {currentlySelectedUnit}");

        ChangeToTopicSelectionScreen();
    }

    public void OnTopicButtonClicked(Masters_Topic selectedTopic) {
        // Store selected topic
        currentlySelectedTopic = selectedTopic;
        Debug.Log($"Selected Topic: {currentlySelectedTopic}");

        SpawnLessonScreenToLessonCanvas();
    }

    private void ChangeToUnitSelectionScreen() {
        Masters_AudioManager.Instance.PlayVoiceOver(selectAUnitAudioClip);

        unitSelectionGameObject.SetActive(true);
        topicSelectionGameObject.SetActive(false);
        currentScreen = Masters_CurrentScreen.UnitSelection;
    }

    private void ChangeToTopicSelectionScreen() {
        Masters_AudioManager.Instance.PlayVoiceOver(selectATopicAudioClip);

        // Destroy Loaded Lesson if coming back to Topic Selection Screen
        if (currentLessonGameObject != null) {
            Destroy(currentLessonGameObject);
        }

        topicSelectionGameObject.SetActive(true);
        unitSelectionGameObject.SetActive(false);
        currentScreen = Masters_CurrentScreen.TopicSelection;
    }

    public void CompleteTopic(Masters_Topic topic) {
        Masters_Topic nextTopicIndex = (Masters_Topic)((int)topic + 1);

        foreach(Masters_TopicButton topicButton in topicButtonArray) {
            if(topicButton.GetTopic() == topic) {
                topicButton.AddLock();
            }

            if(topicButton.GetTopic() == nextTopicIndex) {
                topicButton.RemoveLock();
            }
        }
    }

    public void OnLessonComplete(Masters_Topic topic) {
        numberOfTopicCompleted++;
        Debug.Log(numberOfTopicCompleted);
        if(numberOfTopicCompleted == 8) {
            // Rewards unlocked
            Destroy(currentLessonGameObject);
            OnTopicButtonClicked(Masters_Topic.Rewards);
            CompleteTopic(topic);
            return;
        }

        CompleteTopic(topic);
        ChangeToTopicSelectionScreen();
    }

    public void LoadLessonToLessonCanvas(Masters_LessonSO lessonSO) {
        // Destroy Loaded Lesson if coming back to Topic Selection Screen
        if (currentLessonGameObject != null) {
            Destroy(currentLessonGameObject);
        }

        currentlySelectedUnit = lessonSO.Unit;
        currentlySelectedTopic = lessonSO.Topic;
        currentLessonGameObject = ChangeToLessonScreen(lessonSO.LessonPrefab);
    }

    private void SpawnLessonScreenToLessonCanvas() {
        foreach(Masters_LessonSO lessonSO in lessonSOArray) {
            if(lessonSO.Unit == currentlySelectedUnit && lessonSO.Topic == currentlySelectedTopic) {
                currentLessonGameObject = ChangeToLessonScreen(lessonSO.LessonPrefab);
                return;
            } 
        }

        Debug.Log($"Lesson for {currentlySelectedUnit} and {currentlySelectedTopic} not found!");
    }

    private GameObject ChangeToLessonScreen(GameObject lessonGameObject) {
        GameObject spawnedLessonGameObejct = Instantiate(lessonGameObject, lessonCanvasGameObject.transform);

        topicSelectionGameObject.SetActive(false);
        currentScreen = Masters_CurrentScreen.Lesson;

        return spawnedLessonGameObejct;
    }

    public void OnBackButtonClicked() {
        switch (currentScreen) {
            case Masters_CurrentScreen.UnitSelection:
                Resources.UnloadUnusedAssets();
                SceneManager.LoadSceneAsync("mainScene");
                break;
            case Masters_CurrentScreen.TopicSelection:
                ChangeToUnitSelectionScreen();
                break;
            case Masters_CurrentScreen.Lesson:
                ChangeToTopicSelectionScreen();
                break;
        }
    }


}
