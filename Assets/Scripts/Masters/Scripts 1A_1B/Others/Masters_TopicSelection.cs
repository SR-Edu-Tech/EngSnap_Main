using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Masters_TopicSelection : MonoBehaviour {


    [SerializeField]
    private Masters_TopicButton[] topicButtonArray;
    [SerializeField]
    private RectTransform scrollRectContentRectTransform;


    private Masters_Unit unit;


    public Masters_TopicButton[] GetTopicButtonArray() {
        return topicButtonArray;
    }

    public Masters_Unit GetUnit() {
        return unit;
    }

    public void SetUnit(Masters_Unit unit) {
        this.unit = unit;
    }

    public RectTransform GetScrollRectContentRectTransform() {
        return scrollRectContentRectTransform;
    }



}
