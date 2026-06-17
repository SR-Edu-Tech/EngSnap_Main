using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Masters_SortBin_JumbledWords : MonoBehaviour {


    [SerializeField]
    private Button button;
    [SerializeField]
    private RectTransform phraseTargetPointRectTransform;
    [SerializeField]
    private Masters_JumbledWords_Listening_LessonTwo.SortType sortType;


    public Button GetButton() {
        return button;
    }

    public RectTransform GetPhraseTargetPointRectTransform() {
        return phraseTargetPointRectTransform;
    }

    public Masters_JumbledWords_Listening_LessonTwo.SortType GetSortType() {
        return sortType;
    }


}
