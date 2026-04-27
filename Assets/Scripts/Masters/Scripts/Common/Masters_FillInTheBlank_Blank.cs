using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Masters_FillInTheBlank_Blank : MonoBehaviour {


    [SerializeField]
    private string correctWord;
    [SerializeField]
    private TextMeshProUGUI blankTMP;


    private Button button;


    private void Awake() {
        button = GetComponent<Button>();
    }

    public string GetCorrectWord() {
        return correctWord;
    }

    public void SetWordToBlank(string word) {
        blankTMP.text = word;
    }

    public void SetWordAndColorToBlank(string word, Color color) {
        blankTMP.text = word;
        blankTMP.color = color;

        button.interactable = false;
    }

    
}
