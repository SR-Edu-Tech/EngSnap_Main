using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Masters_FillInTheBlank_Word : MonoBehaviour {


    [SerializeField]
    private string word;


    private Button button;


    private void Awake() {
        button = GetComponent<Button>();
    }

    public string GetWord() {
        button.interactable = false;
        return word;
    }

    
}
