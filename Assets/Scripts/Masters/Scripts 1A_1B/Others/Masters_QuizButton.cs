using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_QuizButton : MonoBehaviour {


    [SerializeField]
    private TextMeshProUGUI buttonTMP;


    private Button button;
    private int buttonIndex;
    private Image buttonImage;


    private void Awake() {
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();
    }

    public void SetText(string text) {
        buttonTMP.text = text;
    }

    public Button GetButton() {
        return button;
    }

    public void SetButtonIndex(int value) {
        buttonIndex = value;
    }

    public int GetButtonIndex() {
        return buttonIndex;
    }

    public Image GetButtonImage() {
        return buttonImage;
    }

    
}
