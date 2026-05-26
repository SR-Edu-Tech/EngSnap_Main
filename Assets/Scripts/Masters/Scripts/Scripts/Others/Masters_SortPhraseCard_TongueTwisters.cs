using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_SortPhraseCard_TongueTwisters : MonoBehaviour {


    [SerializeField]
    private Masters_TongueTwisters_Game_LessonTwo.SortType sortType;
    [SerializeField]
    private TextMeshProUGUI expressionTMP;


    private Button button;


    private void Awake() {
        button = GetComponent<Button>();
    }

    public Button GetButton() {
        return button;
    }

    public void SetSortTypeAndExpression(Masters_TongueTwisters_Game_LessonTwo.SortType sortType, string expression) {
        this.sortType = sortType;
        expressionTMP.text = expression;
    }

    public Masters_TongueTwisters_Game_LessonTwo.SortType GetSortType() {
        return sortType;
    }

}
