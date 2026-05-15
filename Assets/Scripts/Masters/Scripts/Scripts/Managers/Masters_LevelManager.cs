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
    Rewards,
    FindWords
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
    private GameObject topicCanvasGameObject;
    [SerializeField]
    private Masters_LessonSO[] lessonSOArray;
    [SerializeField]
    private AudioClip selectAUnitAudioClip, selectATopicAudioClip;
    [SerializeField]
    private RectTransform unitSelectionScrollRectContentRectTransform;
    [SerializeField]
    private float unitContentSize, topicContentSize;


    private Masters_TopicButton[] topicButtonArray;
    private Masters_CurrentScreen currentScreen;
    private Masters_Unit currentlySelectedUnit;
    private Masters_Topic currentlySelectedTopic;
    private GameObject currentLessonGameObject;
    private List<Dictionary<Masters_Unit, int>> topicCompletedPerUnitList = new List<Dictionary<Masters_Unit, int>>();
    private Dictionary<Masters_Unit, int> topicCompletedPerUnitDictionary = new Dictionary<Masters_Unit, int>();
    private GameObject currentTopicGameObject;
    private List<Masters_TopicSelection> topicSelectionList = new List<Masters_TopicSelection>();
    private RectTransform topicSelectionScrollRectContentRectTransform;


    protected override void Awake() {
        base.Awake();

        //ChangeToUnitSelectionScreen();
        Application.targetFrameRate = 120;
    }

    public void OnUnitButtonClicked(Masters_Unit selectedUnit) {
        // Store selected unit
        currentlySelectedUnit = selectedUnit;

        Masters_TopicSelection currentTopicSelection;

        bool found = false;
        foreach (Dictionary<Masters_Unit, int> topicCompletedPerUnitDictionary in topicCompletedPerUnitList) {
            if (topicCompletedPerUnitDictionary.ContainsKey(currentlySelectedUnit)) {
                // Already has a dictionary for this unit
                this.topicCompletedPerUnitDictionary = topicCompletedPerUnitDictionary;
                found = true;
            }
        }
        if (!found) {
            // Not found a dictionary as selected is a new unit
            Dictionary<Masters_Unit, int> newTopicCompletedPerUnitDictionary = new Dictionary<Masters_Unit, int>();
            newTopicCompletedPerUnitDictionary[currentlySelectedUnit] = 0;
            topicCompletedPerUnitList.Add(newTopicCompletedPerUnitDictionary);
            topicCompletedPerUnitDictionary = newTopicCompletedPerUnitDictionary;
        }

        // CONDITION: If player goes back and clicks the same unit
        if (currentTopicGameObject != null) {
            // There is a Topic Selection
            if(selectedUnit == currentTopicGameObject.GetComponent<Masters_TopicSelection>().GetUnit()) {
                // Current Topic Selection is of another unit, so spawn a new one
                currentTopicSelection = currentTopicGameObject.GetComponent<Masters_TopicSelection>();
                topicSelectionScrollRectContentRectTransform = currentTopicSelection.GetScrollRectContentRectTransform();
                ChangeToTopicSelectionScreen();
                return;
            }
        }

        // CONDITION: If players goes back and clicks the same unit after selecting other units in the middle
        foreach (Masters_TopicSelection topicSelection in topicSelectionList) {
            if(topicSelection.GetUnit() == currentlySelectedUnit) {
                currentTopicGameObject = topicSelection.gameObject;
                topicButtonArray = topicSelection.GetTopicButtonArray();
                currentTopicSelection = currentTopicGameObject.GetComponent<Masters_TopicSelection>();
                topicSelectionScrollRectContentRectTransform = currentTopicSelection.GetScrollRectContentRectTransform();
                ChangeToTopicSelectionScreen();
                return;
            }
        }

        // CONDITION: If players clicks a unit for the very first time
        switch (selectedUnit) {
            case Masters_Unit.MeetingAndGreeting:
                currentTopicGameObject = Instantiate(topicSelectionGameObject, topicCanvasGameObject.transform);
                currentTopicGameObject.name = "MeetingAndGreeting_TopicSelection";
                break;
            case Masters_Unit.SelfIntroduction:
                currentTopicGameObject = Instantiate(topicSelectionGameObject, topicCanvasGameObject.transform);
                currentTopicGameObject.name = "SelfIntroduction_TopicSelection";
                break;
            case Masters_Unit.MyLearningHub:
                currentTopicGameObject = Instantiate(topicSelectionGameObject, topicCanvasGameObject.transform);
                currentTopicGameObject.name = "MyLearningHub_TopicSelection";
                break;
            case Masters_Unit.YouAreInvited:
                currentTopicGameObject = Instantiate(topicSelectionGameObject, topicCanvasGameObject.transform);
                currentTopicGameObject.name = "YouAreInvited_TopicSelection";
                break;
            case Masters_Unit.YeahAndNah:
                currentTopicGameObject = Instantiate(topicSelectionGameObject, topicCanvasGameObject.transform);
                currentTopicGameObject.name = "YeahAndNah_TopicSelection";
                break;
        }

        currentTopicSelection = currentTopicGameObject.GetComponent<Masters_TopicSelection>();
        topicSelectionScrollRectContentRectTransform = currentTopicSelection.GetScrollRectContentRectTransform();
        topicSelectionList.Add(currentTopicSelection);
        currentTopicSelection.SetUnit(selectedUnit);
        topicButtonArray = currentTopicSelection.GetTopicButtonArray();

        ChangeToTopicSelectionScreen();
    }

    public void OnTopicButtonClicked(Masters_Topic selectedTopic) {
        // Store selected topic
        currentlySelectedTopic = selectedTopic;

        SpawnLessonScreenToLessonCanvas();
    }

    private void ChangeToUnitSelectionScreen() {
        Masters_AudioManager.Instance.PlayVoiceOver(selectAUnitAudioClip);

        unitSelectionScrollRectContentRectTransform.offsetMin = new Vector2(0f, 0f);
        unitSelectionScrollRectContentRectTransform.offsetMax = new Vector2(-unitContentSize, 0f);
        unitSelectionGameObject.SetActive(true);
        currentTopicGameObject.SetActive(false);
        currentScreen = Masters_CurrentScreen.UnitSelection;
    }

    private void ChangeToTopicSelectionScreen() {
        Masters_AudioManager.Instance.PlayVoiceOver(selectATopicAudioClip);

        // Destroy Loaded Lesson if coming back to Topic Selection Screen
        if (currentLessonGameObject != null) {
            Destroy(currentLessonGameObject);
        }

        topicSelectionScrollRectContentRectTransform.offsetMin = new Vector2(0f, 0f);
        topicSelectionScrollRectContentRectTransform.offsetMax = new Vector2(-topicContentSize, 0f);
        currentTopicGameObject.SetActive(true);
        unitSelectionGameObject.SetActive(false);
        currentScreen = Masters_CurrentScreen.TopicSelection;
    }

    public void CompleteTopic(Masters_Topic topic) {
        if(topic == Masters_Topic.Rewards) {
            return;
        }

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
        topicCompletedPerUnitDictionary[currentlySelectedUnit]++;
        Debug.Log($"{currentlySelectedUnit}: {topicCompletedPerUnitDictionary[currentlySelectedUnit]}");
        if(topicCompletedPerUnitDictionary[currentlySelectedUnit] == 8) {
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

        currentTopicGameObject.SetActive(false);
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
