using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Masters_SortBin_AnnounceAndRespondToUnfortunateNews : MonoBehaviour {


    [SerializeField]
    private Button button;
    [SerializeField]
    private RectTransform phraseTargetPointRectTransform;
    [SerializeField]
    private Masters_AnnounceAndRespondToUnfortunateNews_Game_LessonTwo.SortType sortType;


    public Button GetButton() {
        return button;
    }

    public RectTransform GetPhraseTargetPointRectTransform() {
        return phraseTargetPointRectTransform;
    }

    public Masters_AnnounceAndRespondToUnfortunateNews_Game_LessonTwo.SortType GetSortType() {
        return sortType;
    }


}
