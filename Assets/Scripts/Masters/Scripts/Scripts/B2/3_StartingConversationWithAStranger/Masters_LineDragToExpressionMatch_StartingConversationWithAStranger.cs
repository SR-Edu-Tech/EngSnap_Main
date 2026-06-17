using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Masters_LineDragToExpressionMatch_StartingConversationWithAStranger : MonoBehaviour {


    [SerializeField]
    private Masters_StartingConversationWithAStranger_Reading_LessonTwo.LeftOrRightPhrase leftOrRightPhrase;
    [SerializeField]
    private Transform lineRendererPointTransform;
    [SerializeField]
    private string correctMatch;
    [SerializeField]
    private LineRenderer lineRenderer;
    [SerializeField]
    private Image fillImage, borderImage;
    [SerializeField]
    private Button button;
    [SerializeField]
    private Color completedFillColor, completedBorderColor;


    private bool isSolved;


    public Masters_StartingConversationWithAStranger_Reading_LessonTwo.LeftOrRightPhrase GetLeftOrRightPhrase() {
        return leftOrRightPhrase;
    }

    public Transform GetLineRendererPointTransform() {
        if (!isSolved) {
            return lineRendererPointTransform;
        }
        return null;
    }

    public LineRenderer GetLineRenderer() {
        if (!isSolved) {
            return lineRenderer;
        }
        return null;
    }

    public string GetCorrectMatch() {
        if (!isSolved) {
            return correctMatch;
        }
        return "";
    }

    public void Solved() {
        isSolved = true;

        //fillImage.color = completedFillColor;
        //borderImage.color = completedBorderColor;
        button.interactable = false;
    }

    public bool GetIsSolved() {
        return isSolved;
    }


}


