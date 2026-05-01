using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_ArrangeWordButton : MonoBehaviour {


    private const string RESET_COLOR = "ResetColor";


    [SerializeField]
    private TextMeshProUGUI buttonTMP;
    [SerializeField]
    private Image buttonImage;


    private string buttonString;
    private bool isInBox;


    public void SetButtonTextAndStringTMP(string tmpText) {
        buttonTMP.text = tmpText;
        buttonString = tmpText;
    }

    public void SetButtonTextColor(Color color) {
        buttonTMP.color = color;
    }

    public void SetIsInBox(bool value) {
        isInBox = value;
    }

    public bool GetIsInBox() {
        return isInBox;
    }

    public string GetButtonString() {
        return buttonString;
    }

    
}
