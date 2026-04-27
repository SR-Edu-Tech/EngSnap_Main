using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Masters_TopicSelectionManager : Masters_Singleton<Masters_TopicSelectionManager> {


    [System.Serializable]
    public class PerUnitTopicProgress {


        [SerializeField]
        private Masters_Unit unit;
        [SerializeField]
        private Masters_Topic topic;
        [SerializeField]
        private int maxNumberOfLessons;

        [HideInInspector]
        public int numberOfLessonsFinished;


    }


    [SerializeField]
    private Masters_TopicButton[] topicButtonArray;
    [SerializeField]
    private PerUnitTopicProgress[] perUnitTopicProgressArray;


    public void UnlockButton(Masters_Topic buttonTopic) {
        //if (buttonTopic == Masters_Topic.None || buttonTopic == Masters_Topic.Intro) {
        //    Debug.Log($"Can't pass None or Intro as buttonTopic in UnlockButton(), {this.name}");
        //    return;
        //}

        //foreach(Masters_TopicButton topicButton in topicButtonArray) {
        //    if(topicButton.GetTopic() == buttonTopic) {
        //        topicButton.RemoveLock();
        //    }
        //}
    }

    
}
