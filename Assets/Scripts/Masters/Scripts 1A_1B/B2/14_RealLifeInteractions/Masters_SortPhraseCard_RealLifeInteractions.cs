using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_SortPhraseCard_RealLifeInteractions : MonoBehaviour {

    [SerializeField]
    private Masters_RealLifeInteractions_Listening_LessonTwo.SortType sortType;
    [SerializeField]
    private TextMeshProUGUI expressionTMP;

    private Button button;

    private void Awake() {
        button = GetComponent<Button>();
    }

    public Button GetButton() {
        return button;
    }

    public void SetSortTypeAndExpression(Masters_RealLifeInteractions_Listening_LessonTwo.SortType sortType, string expression) {
        this.sortType = sortType;
        if (expressionTMP != null) {
            expressionTMP.text = expression;
        }
    }

    public Masters_RealLifeInteractions_Listening_LessonTwo.SortType GetSortType() {
        return sortType;
    }
}
