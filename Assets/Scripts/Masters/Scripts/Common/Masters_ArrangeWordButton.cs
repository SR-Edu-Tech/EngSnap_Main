using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Masters_ArrangeWordButton : MonoBehaviour {


    private const string RESET_COLOR = "ResetColor";


    [SerializeField]
    private TextMeshProUGUI buttonTMP;
    [SerializeField]
    private Color defaultColor;
    [SerializeField]
    private float colorResetTime;


    private int buttonIndex;
    private string buttonString;


    public int GetButtonIndex() {
        return buttonIndex;
    }

    public void SetButtonIndexAndWord(int index, string word) {
        buttonIndex = index;
        buttonString = word;
        buttonTMP.text = buttonString;
    }

    public void SetTMPColor(Color color) {
        buttonTMP.color = color;
        Invoke(RESET_COLOR, colorResetTime);
    }

    public void ResetColor() {
        buttonTMP.color = defaultColor;
    }

    
}
