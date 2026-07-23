using UnityEngine;
using UnityEngine.UI;

public class Masters_SortBin_RealLifeInteractions : MonoBehaviour {

    [SerializeField]
    private Button button;
    [SerializeField]
    private RectTransform phraseTargetPointRectTransform;
    [SerializeField]
    private Masters_RealLifeInteractions_Listening_LessonTwo.SortType sortType;

    public Button GetButton() {
        return button;
    }

    public RectTransform GetPhraseTargetPointRectTransform() {
        return phraseTargetPointRectTransform;
    }

    public Masters_RealLifeInteractions_Listening_LessonTwo.SortType GetSortType() {
        return sortType;
    }

    public void SetSortType(Masters_RealLifeInteractions_Listening_LessonTwo.SortType newType) {
        sortType = newType;
    }
}
