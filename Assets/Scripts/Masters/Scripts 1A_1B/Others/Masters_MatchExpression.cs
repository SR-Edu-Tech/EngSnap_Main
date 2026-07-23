using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Masters_MatchExpression : MonoBehaviour {


    [SerializeField]
    private int expressionIndex;


    private Button button;


    private void Awake() {
        button = GetComponent<Button>();
    }

    public Button GetButton() {
        return button;
    }

    public int GetExpressionIndex() {
        return expressionIndex;
    }


}
